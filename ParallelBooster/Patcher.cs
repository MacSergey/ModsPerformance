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


        public static Label FindIfBegin(IEnumerator<CodeInstruction> enumerator, IEnumerator<CodeInstruction> findEnumerator, out List<CodeInstruction> beforeIfBlock)
        {
            beforeIfBlock = new List<CodeInstruction>();
            while (findEnumerator.MoveNext() && enumerator.MoveNext())
            {
                //Logger.Debug($"{enumerator.Current} - {findEnumerator.Current}");
                if (!enumerator.Current.Is(findEnumerator.Current))
                    findEnumerator.Reset();

                beforeIfBlock.Add(enumerator.Current);
            }

            enumerator.MoveNext();
            beforeIfBlock.Add(enumerator.Current);
            return (Label)enumerator.Current.operand;
        }
        public static IEnumerable<CodeInstruction> GetIfBlock(this IEnumerable<CodeInstruction> instructions, IEnumerable<CodeInstruction> ifCondInsts, bool elseExist = false)
        {
            var result = new List<CodeInstruction>();
            var endLabels = new List<Label>();

            var enumerator = instructions.GetEnumerator();
            var findEnumerator = ifCondInsts.GetEnumerator();

            var endIfLabel = FindIfBegin(enumerator, findEnumerator, out _);

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

            var startElseLabel = FindIfBegin(enumerator, findEnumerator, out _);

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

        public static IEnumerable<CodeInstruction> ReplaceIfBlock(this IEnumerable<CodeInstruction> instructions, IEnumerable<CodeInstruction> ifCondInsts, IEnumerable<CodeInstruction> replaceInstructions, bool elseExist = false)
        {
            var result = new List<CodeInstruction>();

            var enumerator = instructions.GetEnumerator();
            var findEnumerator = ifCondInsts.GetEnumerator();

            var endIfLabel = FindIfBegin(enumerator, findEnumerator, out List<CodeInstruction> beforeIfBlock);

            result.AddRange(beforeIfBlock);
            result.AddRange(replaceInstructions);

            for (var prev = (CodeInstruction)default; enumerator.MoveNext(); prev = enumerator.Current)
            {
                var instruction = enumerator.Current;

                if (instruction.labels.Contains(endIfLabel))
                {
                    if(elseExist)
                        result.Add(prev);
                    result.Add(instruction);
                    break;
                }
            }

            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }

            return result;
        }

        public static IEnumerable<CodeInstruction> ReplaceElseBlock(this IEnumerable<CodeInstruction> instructions, IEnumerable<CodeInstruction> ifCondInsts, IEnumerable<CodeInstruction> replaceInstructions)
        {
            var result = new List<CodeInstruction>();

            var enumerator = instructions.GetEnumerator();
            var findEnumerator = ifCondInsts.GetEnumerator();

            var startElseLabel = FindIfBegin(enumerator, findEnumerator, out List<CodeInstruction> beforeIfBlock);

            result.AddRange(beforeIfBlock);

            var endElseLabel = (Label)default;
            for (var prev = (CodeInstruction)default; enumerator.MoveNext(); prev = enumerator.Current)
            {
                var instruction = enumerator.Current;

                if (prev != null)
                    result.Add(prev);

                if (instruction.labels.Contains(startElseLabel))
                {
                    result.Add(new CodeInstruction(OpCodes.Nop) { labels = new List<Label> { startElseLabel } });
                    endElseLabel = (Label)prev.operand;
                    break;
                }
            }

            result.AddRange(replaceInstructions);

            while (enumerator.MoveNext())
            {
                var instruction = enumerator.Current;

                if (instruction.labels.Contains(endElseLabel))
                {
                    result.Add(instruction);
                    break;
                }
            }

            while(enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }

            return result;
        }
    }
}
