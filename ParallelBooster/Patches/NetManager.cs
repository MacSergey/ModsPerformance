using ColossalFramework.Threading;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using static RenderManager;

namespace ParallelBooster.Patches
{
    public static class NetManagerPatch
    {
        private static CustomDispatcher CustomDispatcher { get; } = new CustomDispatcher();

        public static void Patch(Harmony harmony)
        {
            var originalMethod = AccessTools.Method(typeof(NetManager), "EndRenderingImpl");
            var extractedMethod = AccessTools.Method(typeof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplExtractedDummy));

            var reversePatcher = harmony.CreateReversePatcher(originalMethod, new HarmonyMethod(extractedMethod));
            reversePatcher.Patch();
            Logger.Debug($"Created reverse patch NetManagerPatch.EndRenderingImplExtractedDummy");

            var transpilerMethod = AccessTools.Method(typeof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplPatch));
            harmony.Patch(originalMethod, transpiler: new HarmonyMethod(transpilerMethod));
            Logger.Debug($"Patched NetManager.EndRenderingImpl");
        }

        #region NetManager.EndRenderingImpl

#if Debug
        [HarmonyDebug]
#endif
        private static IEnumerable<CodeInstruction> EndRenderingImplPatch(IEnumerable<CodeInstruction> instructions)
        {
            var replaceInstructions = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetManagerPatch), nameof(NetManagerPatch.RenderGroups))),
            };

            return instructions.ReplaceFor(1, new CodeInstruction(OpCodes.Ldarg_0), replaceInstructions);
            //var instructionsEnumerator = instructions.GetEnumerator();
            //var startLoopFinded = false;
            //var endLoopLable = (Label)default;
            //CodeInstruction instruction;

            //while (instructionsEnumerator.MoveNext())
            //{
            //    instruction = instructionsEnumerator.Current;

            //    if (startLoopFinded)
            //    {
            //        endLoopLable = (Label)instruction.operand;
            //        break;
            //    }

            //    yield return instruction;

            //    if (instruction.opcode == OpCodes.Stloc_1)
            //        startLoopFinded = true;
            //}

            //yield return new CodeInstruction(OpCodes.Ldarg_0);
            //yield return new CodeInstruction(OpCodes.Ldarg_1);
            //yield return new CodeInstruction(OpCodes.Ldloc_0);
            //yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetManagerPatch), nameof(NetManagerPatch.RenderGroups)));

            //var loopСonditionFinded = false;
            //var endLoopFinded = false;
            //while(instructionsEnumerator.MoveNext())
            //{
            //    instruction = instructionsEnumerator.Current;

            //    if (!endLoopFinded)
            //    {
            //        if (!loopСonditionFinded)
            //        {
            //            if (instruction.labels.Contains(endLoopLable))
            //                loopСonditionFinded = true;
            //            continue;
            //        }
            //        else if (instruction.opcode != OpCodes.Ldarg_0)
            //            continue;
            //        else
            //            endLoopFinded = true;
            //    }

            //    yield return instruction;
            //}
        }

        private static void RenderGroups(NetManager instance, CameraInfo cameraInfo, FastList<RenderGroup> renderedGroups)
        {
#if Debug
            Logger.Debug($"Start. {nameof(renderedGroups)}={renderedGroups.m_size} (thread={Thread.CurrentThread.ManagedThreadId})");
            var sw = Stopwatch.StartNew();
            var dipsSw = new Stopwatch();
#endif

            CustomDispatcher.Clear();

            var task = Task.Create(() => EndRenderingImplExtractedDummy(instance, cameraInfo, renderedGroups));
            task.Run();


            while (!task.hasEnded || !CustomDispatcher.IsDone)
            {
#if Debug
                dipsSw.Start();
#endif
                CustomDispatcher.Execute();
#if Debug
                dipsSw.Stop();
#endif
            }

#if Debug
            sw.Stop();
            Logger.Debug($"Dispatcher duration {dipsSw.ElapsedTicks}");
            Logger.Debug($"End {sw.ElapsedTicks}");
#endif
        }

#if Debug
        [HarmonyDebug]
#endif
        private static void EndRenderingImplExtractedDummy(NetManager instance, CameraInfo cameraInfo, FastList<RenderGroup> renderedGroups)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Stloc_0);

                foreach (var instruction in instructions.GetFor(1, new CodeInstruction(OpCodes.Ldarg_0)))
                    yield return instruction;
                //var instructionsEnumerator = instructions.GetEnumerator();
                //CodeInstruction instruction;

                //for (var prevInstruction = (CodeInstruction)null; instructionsEnumerator.MoveNext(); prevInstruction = instructionsEnumerator.Current)
                //{
                //    instruction = instructionsEnumerator.Current;

                //    if(instruction.opcode == OpCodes.Stloc_1)
                //    {
                //        Logger.Debug(prevInstruction.ToString());
                //        yield return prevInstruction;
                //        Logger.Debug(instruction.ToString());
                //        yield return instruction;
                //        break;
                //    }
                //}

                //instructionsEnumerator.MoveNext();
                //instruction = instructionsEnumerator.Current;
                //Logger.Debug(instruction.ToString());
                //yield return instruction;               
                //var endLoopLable = (Label)instruction.operand;

                //var loopСonditionFinded = false;
                //while (instructionsEnumerator.MoveNext())
                //{
                //    instruction = instructionsEnumerator.Current;
                //    if (!loopСonditionFinded)
                //        loopСonditionFinded = instruction.labels.Contains(endLoopLable);
                //    else if (instruction.opcode == OpCodes.Ldarg_0)
                //        break;

                //    Logger.Debug(instruction.ToString());
                //    yield return instruction;
                //}

                yield return new CodeInstruction(OpCodes.Ret);
            }

            _ = Transpiler(null);
        }

        #endregion;
    }
}
