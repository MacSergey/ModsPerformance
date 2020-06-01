using ColossalFramework.PlatformServices;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace ParallelBooster
{
    public static class Utility
    {
        public static IEnumerable<CodeInstruction> GetFor(this IEnumerable<CodeInstruction> instructions, uint iVarIndex, CodeInstruction nextInstruction)
        {
            var iVarInstruction = GetIVarInstruction(iVarIndex);

            var instructionsEnumerator = instructions.GetEnumerator();
            CodeInstruction instruction;

            for (var prevInstruction = (CodeInstruction)null; instructionsEnumerator.MoveNext(); prevInstruction = instructionsEnumerator.Current)
            {
                instruction = instructionsEnumerator.Current;

                if (instruction.opcode == iVarInstruction.opcode && instruction.operand == iVarInstruction.operand)
                {
                    Logger.Debug(prevInstruction.ToString());
                    yield return prevInstruction;
                    Logger.Debug(instruction.ToString());
                    yield return instruction;
                    break;
                }
            }

            instructionsEnumerator.MoveNext();
            instruction = instructionsEnumerator.Current;
            Logger.Debug(instruction.ToString());
            yield return instruction;
            var endLoopLable = (Label)instruction.operand;

            var loopСonditionFinded = false;
            while (instructionsEnumerator.MoveNext())
            {
                instruction = instructionsEnumerator.Current;
                if (!loopСonditionFinded)
                    loopСonditionFinded = instruction.labels.Contains(endLoopLable);
                else if (instruction == nextInstruction)
                    break;

                Logger.Debug(instruction.ToString());
                yield return instruction;
            }
        }

        public static IEnumerable<CodeInstruction> ReplaceFor(this IEnumerable<CodeInstruction> instructions, uint iVarIndex, CodeInstruction nextInstruction, IEnumerable<CodeInstruction> replaceInstructions = null)
        {
            var iVarInstruction = GetIVarInstruction(iVarIndex);

            var instructionsEnumerator = instructions.GetEnumerator();
            var startLoopFinded = false;
            var endLoopLable = (Label)default;
            CodeInstruction instruction;

            while (instructionsEnumerator.MoveNext())
            {
                instruction = instructionsEnumerator.Current;

                if (startLoopFinded)
                {
                    endLoopLable = (Label)instruction.operand;
                    break;
                }

                yield return instruction;

                if (instruction.opcode == iVarInstruction.opcode && instruction.operand == iVarInstruction.operand)
                    startLoopFinded = true;
            }

            if (replaceInstructions != null)
            {
                foreach (var replaceInstruction in replaceInstructions)
                {
                    yield return replaceInstruction;
                }
            }

            var loopСonditionFinded = false;
            var endLoopFinded = false;
            while (instructionsEnumerator.MoveNext())
            {
                instruction = instructionsEnumerator.Current;

                if (!endLoopFinded)
                {
                    if (!loopСonditionFinded)
                    {
                        if (instruction.labels.Contains(endLoopLable))
                            loopСonditionFinded = true;
                        continue;
                    }
                    else if (instruction.opcode != nextInstruction.opcode || instruction.operand != nextInstruction.operand)
                        continue;
                    else
                        endLoopFinded = true;
                }

                yield return instruction;
            }
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
    }
}
