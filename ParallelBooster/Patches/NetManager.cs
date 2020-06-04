using ColossalFramework;
using ColossalFramework.Threading;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using UnityEngine;
using static RenderManager;

namespace ParallelBooster.Patches
{
    public static class NetManagerPatch
    {
        private static int TaskCount => 3;
        public static void Patch(Harmony harmony)
        {
            var originalMethod = AccessTools.Method(typeof(NetManager), "EndRenderingImpl");

            var extractedMethod = AccessTools.Method(typeof(NetManagerPatch), nameof(EndRenderingImplExtracted));
            Patcher.PatchReverse(harmony, originalMethod, extractedMethod);

            var transpilerMethod = AccessTools.Method(typeof(NetManagerPatch), nameof(EndRenderingImplPatch)); 
            Patcher.PatchTranspiler(harmony, originalMethod, transpilerMethod);
        }

        public static void RenderGroups(NetManager instance, CameraInfo cameraInfo, FastList<RenderGroup> renderedGroups)
        {
#if Debug
            Logger.Debug($"Start {nameof(renderedGroups)}={renderedGroups.m_size} (thread={Thread.CurrentThread.ManagedThreadId})");
            var sw = Stopwatch.StartNew();
            var dipsSw = new Stopwatch();
            Patcher.Stopwatch.Reset();
#endif

#if UseTask
            Patcher.Dispatcher.Clear();

            var tasks = new Task[TaskCount];
            for (var i = 0; i < TaskCount; i += 1)
            {
                var taskNum = i;
                var task = Task.Create(() =>
                {
#if Debug
                    Logger.Debug($"Start task #{taskNum} (thread={Thread.CurrentThread.ManagedThreadId})");
                    var tasksw = Stopwatch.StartNew();
#endif
                    EndRenderingImplExtracted(instance, cameraInfo, renderedGroups, taskNum, TaskCount);
#if Debug
                    tasksw.Stop();
                    Logger.Debug($"End task #{taskNum} {tasksw.ElapsedTicks}");
#endif
                });
                task.Run();
                tasks[i] = task;
            }


            while (tasks.Any(t => !t.hasEnded) || !Patcher.Dispatcher.IsDone)
            {
#if Debug
                dipsSw.Start();
#endif
                Patcher.Dispatcher.Execute();
#if Debug
                dipsSw.Stop();
#endif
            }
#else
                EndRenderingImplExtracted(instance, cameraInfo, renderedGroups);
#endif

#if Debug
            sw.Stop();
            Logger.Debug($"Actions duration {Patcher.Stopwatch.ElapsedTicks}");
            Logger.Debug($"Dispatcher duration {dipsSw.ElapsedTicks}");
            Logger.Debug($"End {sw.ElapsedTicks}");
#endif
        }

#if Debug
        [HarmonyDebug]
#endif
        private static IEnumerable<CodeInstruction> EndRenderingImplPatch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var replaceInstructions = new CodeInstruction[]
            {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetManagerPatch), nameof(RenderGroups))),
            };

            var newInstructions = instructions.ReplaceFor(Patcher.GetIVarInstruction(1), replaceInstructions);
#if Debug && Trace
                Logger.AddDebugInstructions(newInstructions, nameof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplPatch));
#endif
#if Debug && IL
                Logger.Debug(nameof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplPatch), newInstructions);
#endif
            return newInstructions;
        }

#if Debug
        [HarmonyDebug]
#endif
        private static void EndRenderingImplExtracted(NetManager instance, CameraInfo cameraInfo, FastList<RenderGroup> renderedGroups, int taskNum, int taskCount)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var newInstructions = new List<CodeInstruction>();

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_2));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_0));

                var forInstructions = instructions.GetFor(Patcher.GetIVarInstruction(1));
                forInstructions[0] = new CodeInstruction(OpCodes.Ldarg, 3);
                forInstructions[forInstructions.Count - 7] = new CodeInstruction(OpCodes.Ldarg, 4);

                newInstructions.AddRange(forInstructions);

                newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Trace
                Logger.AddDebugInstructions(newInstructions, nameof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplExtracted));
#endif
#if Debug && IL
                Logger.Debug(nameof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplExtracted), newInstructions);
#endif
                return newInstructions;
            }

            _ = Transpiler(null);
        }
    }
}
