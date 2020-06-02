using CitiesHarmony.API;
using HarmonyLib;
using ParallelBooster.Patches;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using static RenderManager;

namespace ParallelBooster
{
    public static class Patcher
    {
        private static string HarmonyId { get; } = nameof(ParallelBooster);

        public static void Patch() => HarmonyHelper.DoOnHarmonyReady(() => Begin());
        public static void Unpatch()
        {
            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
        }

        private static void Begin()
        {
            var harmony = new Harmony(HarmonyId);

            NetManagerRenderPatch.Patch(harmony);
        }

        public static void PatchTranspiler(Harmony harmony, MethodInfo originalMethod, MethodInfo transpilerMethod)
        {
            harmony.Patch(originalMethod, transpiler: new HarmonyMethod(transpilerMethod));
            Logger.Debug($"Patched {originalMethod.DeclaringType.Name}.{originalMethod.Name}");
        }
        public static void PatchReverse(Harmony harmony, MethodInfo originalMethod, MethodInfo extractedMethod)
        {
            var reversePatcher = harmony.CreateReversePatcher(originalMethod, new HarmonyMethod(extractedMethod));
            reversePatcher.Patch();
            Logger.Debug($"Created reverse patch {extractedMethod.DeclaringType.Name}.{extractedMethod.Name}");
        }

        public static IEnumerable<CodeInstruction> GetFor(this IEnumerable<CodeInstruction> instructions, CodeInstruction startLoopInstruction)
        {
            var enumerator = instructions.GetEnumerator();

            while (enumerator.MoveNext() && !enumerator.Current.IsForStart(startLoopInstruction)) ;
            enumerator.MoveNext();

            var startLoopLabel = (Label)default;
            while (enumerator.MoveNext())
            {
                var instruction = enumerator.Current;
                yield return instruction;

                if (startLoopLabel == default)
                    startLoopLabel = instruction.labels.First();
                if (instruction.operand is Label label && label == startLoopLabel)
                    break;
            }
        }

        public static IEnumerable<CodeInstruction> ReplaceFor(this IEnumerable<CodeInstruction> instructions, CodeInstruction startLoopInstruction, IEnumerable<CodeInstruction> replaceInstructions)
        {
            var enumerator = instructions.GetEnumerator();

            for (var prevInstruction = (CodeInstruction)null; enumerator.MoveNext(); prevInstruction = enumerator.Current)
            {
                var instruction = enumerator.Current;
                if (instruction.IsForStart(startLoopInstruction))
                    break;
                else if (prevInstruction != null)
                    yield return prevInstruction;
            }

            enumerator.MoveNext();
            enumerator.MoveNext();
            var startLoopLabel = enumerator.Current.labels.First();

            while (enumerator.MoveNext() && !enumerator.Current.IsForEnd(startLoopLabel)) ;

            foreach (var replaceInstruction in replaceInstructions)
                yield return replaceInstruction;

            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        public static CodeInstruction GetIVarInstruction(uint iVarIndex, bool shortCode = true)
        {
            switch (iVarIndex)
            {
                case 0 when shortCode: return new CodeInstruction(OpCodes.Stloc_0);
                case 1 when shortCode: return new CodeInstruction(OpCodes.Stloc_1);
                case 2 when shortCode: return new CodeInstruction(OpCodes.Stloc_2);
                case 3 when shortCode: return new CodeInstruction(OpCodes.Stloc_3);
                default: return new CodeInstruction(OpCodes.Stloc_S, (int)iVarIndex);
            }
        }
        public static bool IsForStart(this CodeInstruction inst1, CodeInstruction inst2)
        {
            return inst1.opcode == inst2.opcode && ((inst1.operand == null && inst2.operand == null) || (inst1.operand is LocalBuilder local && local.LocalIndex == (int)inst2.operand));
        }
        public static bool IsForEnd(this CodeInstruction instruction, Label startLoopLabel) => instruction.operand is Label label && label == startLoopLabel;
        public static bool Is(this CodeInstruction inst, CodeInstruction other) => other.operand == null ? inst.opcode == other.opcode : inst.Is(other.opcode, other.operand);


        public static Label FindIfBegin(IEnumerator<CodeInstruction> enumerator, IEnumerator<CodeInstruction> findEnumerator)
        {
            while (findEnumerator.MoveNext() && enumerator.MoveNext())
            {
                //Logger.Debug($"{enumerator.Current} - {findEnumerator.Current}");
                if (!enumerator.Current.Is(findEnumerator.Current))
                    findEnumerator.Reset();
            }

            enumerator.MoveNext();
            return (Label)enumerator.Current.operand;
        }
        public static IEnumerable<CodeInstruction> GetIfBlock(this IEnumerable<CodeInstruction> instructions, IEnumerable<CodeInstruction> ifCondInsts, bool elseExist = false)
        {
            var result = new List<CodeInstruction>();
            var endLabels = new List<Label>();

            var enumerator = instructions.GetEnumerator();
            var findEnumerator = ifCondInsts.GetEnumerator();

            var endIfLabel = FindIfBegin(enumerator, findEnumerator);

            for (var prev = (CodeInstruction)default; enumerator.MoveNext(); prev = enumerator.Current)
            {
                var instruction = enumerator.Current;

                if (instruction.labels.Contains(endIfLabel))
                {
                    if (elseExist)
                        endLabels = prev.labels;   //точно, не менять                 
                    else
                    {
                        result.Add(prev);
                        endLabels = instruction.labels;
                    }
                    break;
                }
                else if (prev != null)
                    result.Add(prev);
            }

            result.Add(new CodeInstruction(OpCodes.Nop) { labels = endLabels });
            return result;
        }
        public static IEnumerable<CodeInstruction> GetElseBlock(this IEnumerable<CodeInstruction> instructions, IEnumerable<CodeInstruction> ifCondInsts)
        {
            var result = new List<CodeInstruction>();
            var endLabels = new List<Label>();

            var enumerator = instructions.GetEnumerator();
            var findEnumerator = ifCondInsts.GetEnumerator();

            var startElseLabel = FindIfBegin(enumerator, findEnumerator);

            var endElseLabel = (Label)default;
            for (var prev = (CodeInstruction)default; enumerator.MoveNext(); prev = enumerator.Current)
            {
                var instruction = enumerator.Current;
                if (instruction.labels.Contains(startElseLabel))
                {
                    endElseLabel = (Label)prev.operand;
                    result.Add(instruction);
                    break;
                }
            }

            for (var prev = (CodeInstruction)default; enumerator.MoveNext(); prev = enumerator.Current)
            {
                var instruction = enumerator.Current;

                if (prev != null)
                    result.Add(prev);

                if (instruction.labels.Contains(endElseLabel))
                {
                    endLabels = instruction.labels;
                    break;
                }
            }

            result.Add(new CodeInstruction(OpCodes.Nop) { labels = endLabels });
            return result;
        }

        public static IEnumerable<CodeInstruction> ReplaceIfBlock(this IEnumerable<CodeInstruction> instructions, CodeInstruction endIfCondInst, IEnumerable<CodeInstruction> replaceInstructions)
        {
            var result = new List<CodeInstruction>();

            var enumirator = instructions.GetEnumerator();

            var endIfLabel = (Label)default;

            for (CodeInstruction prev = default; enumirator.MoveNext(); prev = enumirator.Current)
            {
                var instruction = enumirator.Current;
                result.Add(instruction);

                if (prev != null && prev.Is(endIfCondInst))
                {
                    endIfLabel = (Label)instruction.operand;
                    break;
                }
            }

            result.AddRange(replaceInstructions);

            while (enumirator.MoveNext())
            {
                if (enumirator.Current.labels.Contains(endIfLabel))
                {
                    result.Add(enumirator.Current);
                    break;
                }
            }

            while (enumirator.MoveNext())
            {
                result.Add(enumirator.Current);
            }

            return result;
        }

        public static IEnumerable<CodeInstruction> ReplaceElseBlock(this IEnumerable<CodeInstruction> instructions, CodeInstruction endIfCondInst, IEnumerable<CodeInstruction> replaceInstructions)
        {
            var result = new List<CodeInstruction>();

            var enumirator = instructions.GetEnumerator();

            while (enumirator.MoveNext() && !enumirator.Current.Is(endIfCondInst)) ;

            enumirator.MoveNext();
            var startElseLabel = (Label)enumirator.Current.operand;

            while (enumirator.MoveNext() && !enumirator.Current.labels.Contains(startElseLabel)) ;

            var endElseLabel = (Label)enumirator.Current.operand;

            result.AddRange(replaceInstructions);

            while (enumirator.MoveNext())
            {
                if (enumirator.Current.labels.Contains(endElseLabel))
                {
                    result.Add(enumirator.Current);
                    break;
                }
            }

            while (enumirator.MoveNext())
            {
                result.Add(enumirator.Current);
            }

            return result;
        }
    }
}
