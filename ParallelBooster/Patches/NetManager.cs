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
    public static class NetManagerRenderPatch
    {
        private static CustomDispatcher CustomDispatcher { get; } = new CustomDispatcher();

        public static void Patch(Harmony harmony)
        {
            NetManagerPatch.Patch(harmony);
            NetSegmentPatch.Patch(harmony);
        }

        public static class NetManagerPatch
        {
            public static void Patch(Harmony harmony)
            {
                var originalMethod = AccessTools.Method(typeof(NetManager), "EndRenderingImpl");
                var extractedMethod = AccessTools.Method(typeof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplExtractedDummy));
                var transpilerMethod = AccessTools.Method(typeof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplPatch));

                Patcher.Patch(harmony, originalMethod, extractedMethod, transpilerMethod);
            }

            public static void RenderGroups(NetManager instance, CameraInfo cameraInfo, FastList<RenderGroup> renderedGroups)
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
            private static IEnumerable<CodeInstruction> EndRenderingImplPatch(IEnumerable<CodeInstruction> instructions)
            {
                var replaceInstructions = new CodeInstruction[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetManagerPatch), nameof(NetManagerPatch.RenderGroups))),
                };

                var newInstructions = instructions.ReplaceFor(1, replaceInstructions).ToList();
#if Debug
                Logger.Debug(nameof(NetManagerPatch), nameof(EndRenderingImplPatch), newInstructions);
#endif
                return newInstructions;
            }

#if Debug
            [HarmonyDebug]
#endif
            private static void EndRenderingImplExtractedDummy(NetManager instance, CameraInfo cameraInfo, FastList<RenderGroup> renderedGroups)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var newInstructions = new List<CodeInstruction>();

                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_2));
                    newInstructions.Add(new CodeInstruction(OpCodes.Stloc_0));
                    newInstructions.AddRange(instructions.GetFor(1));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Debug
                    Logger.Debug(nameof(NetManagerPatch), nameof(EndRenderingImplExtractedDummy), newInstructions);
#endif
                    return newInstructions;
                }

                _ = Transpiler(null);
            }
        }

        public static class NetSegmentPatch
        {
            public static void Patch(Harmony harmony)
            {
                var originalMethod = AccessTools.Method(typeof(NetSegment), "RenderInstance", new Type[] { typeof(CameraInfo), typeof(ushort), typeof(int), typeof(NetInfo), typeof(Instance).MakeByRefType() });
                var extractedMethod = AccessTools.Method(typeof(NetSegmentPatch), nameof(NetSegmentPatch.RenderInstanceExtractedDummy));
                var transpilerMethod = AccessTools.Method(typeof(NetSegmentPatch), nameof(NetSegmentPatch.RenderInstancePatch));

                Patcher.Patch(harmony, originalMethod, extractedMethod, transpilerMethod);
            }

            public static void AddToDispatcher(NetSegment instance, CameraInfo cameraInfo, ushort segmentID, int layerMask, NetInfo info, Instance data)
            {
                var action = new Action(() => RenderInstanceExtractedDummy(instance, cameraInfo, segmentID, layerMask, info, ref data));
                CustomDispatcher.Add(action);
            }

#if Debug
            [HarmonyDebug]
#endif
            private static IEnumerable<CodeInstruction> RenderInstancePatch(IEnumerable<CodeInstruction> instructions)
            {
                var replaceInstructions = new CodeInstruction[]
                {
                    new CodeInstruction(OpCodes.Ldarg_S, 0),
                    new CodeInstruction(OpCodes.Ldobj, typeof(NetSegment)),
                    new CodeInstruction(OpCodes.Ldarg_S, 1),
                    new CodeInstruction(OpCodes.Ldarg_S, 2),
                    new CodeInstruction(OpCodes.Ldarg_S, 3),
                    new CodeInstruction(OpCodes.Ldarg_S, 4),
                    new CodeInstruction(OpCodes.Ldarg_S, 5),
                    new CodeInstruction(OpCodes.Ldobj, typeof(Instance)),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatch), nameof(NetSegmentPatch.AddToDispatcher))),
                };

                var newInstructions = instructions.ReplaceFor(39, replaceInstructions).ToList();
#if Debug
                Logger.Debug(nameof(NetSegmentPatch), nameof(RenderInstancePatch), newInstructions);
#endif
                return newInstructions;
            }

#if Debug
            [HarmonyDebug]
#endif
            private static void RenderInstanceExtractedDummy(NetSegment instance, CameraInfo cameraInfo, ushort segmentID, int layerMask, NetInfo info, ref Instance data)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    Logger.Debug(nameof(NetSegmentPatch), nameof(RenderInstanceExtractedDummy), instructions);

                    var newInstructions = new List<CodeInstruction>();
                    newInstructions.AddRange(instructions.GetFor(39));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Debug
                    Logger.Debug(nameof(NetSegmentPatch), nameof(RenderInstanceExtractedDummy), newInstructions);
#endif
                    return newInstructions;
                }

                _ = Transpiler(null);
            }
        }
    }
}
