using CitiesHarmony.API;
using HarmonyLib;
using ParallelBooster.Patches;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

        public static void Patch(Harmony harmony, MethodInfo originalMethod, MethodInfo extractedMethod, MethodInfo transpilerMethod)
        {
            var reversePatcher = harmony.CreateReversePatcher(originalMethod, new HarmonyMethod(extractedMethod));
            reversePatcher.Patch();
            Logger.Debug($"Created reverse patch {extractedMethod.DeclaringType.Name}.{extractedMethod.Name}");


            harmony.Patch(originalMethod, transpiler: new HarmonyMethod(transpilerMethod));
            Logger.Debug($"Patched {originalMethod.DeclaringType.Name}.{originalMethod.Name}");
        }

        public static IEnumerable<CodeInstruction> GetFor(this IEnumerable<CodeInstruction> instructions, uint iVarIndex)
        {
            var iVarInstruction = GetIVarInstruction(iVarIndex);

            var enumerator = instructions.GetEnumerator();

            while (enumerator.MoveNext() && !enumerator.Current.IsForStart(iVarInstruction)) ;
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

        public static IEnumerable<CodeInstruction> ReplaceFor(this IEnumerable<CodeInstruction> instructions, uint iVarIndex, IEnumerable<CodeInstruction> replaceInstructions = null)
        {
            var iVarInstruction = GetIVarInstruction(iVarIndex);

            var enumerator = instructions.GetEnumerator();

            for (var prevInstruction = (CodeInstruction)null; enumerator.MoveNext(); prevInstruction = enumerator.Current)
            {
                var instruction = enumerator.Current;
                if (instruction.IsForStart(iVarInstruction))
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

        private static CodeInstruction GetIVarInstruction(uint iVarIndex, bool shortCode = true)
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
    }
}
