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

            var prefixMethod = AccessTools.Method(typeof(NetManagerPatch), nameof(EndRenderingImplPrefix));
            Patcher.PatchPrefix(harmony, originalMethod, prefixMethod);
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


            while (tasks.Any(t => !t.hasEnded) || !Patcher.Dispatcher.NothingExecute)
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
                EndRenderingImplExtracted(instance, cameraInfo, renderedGroups, 0, 1);
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

        public static bool EndRenderingImplPrefix(NetManager __instance, CameraInfo cameraInfo)
        {
            FastList<RenderGroup> renderedGroups = Singleton<RenderManager>.instance.m_renderedGroups;
            __instance.m_nameInstanceBuffer.Clear();
            __instance.m_visibleRoadNameSegment = 0;
            __instance.m_visibleTrafficLightNode = 0;
            for (int i = 0; i < renderedGroups.m_size; i++)
            {
                RenderGroup renderGroup = renderedGroups.m_buffer[i];
                if (renderGroup.m_instanceMask == 0)
                {
                    continue;
                }
                int num = renderGroup.m_x * 270 / 45;
                int num2 = renderGroup.m_z * 270 / 45;
                int num3 = (renderGroup.m_x + 1) * 270 / 45 - 1;
                int num4 = (renderGroup.m_z + 1) * 270 / 45 - 1;
                for (int j = num2; j <= num4; j++)
                {
                    for (int k = num; k <= num3; k++)
                    {
                        int num5 = j * 270 + k;
                        ushort num6 = __instance.m_nodeGrid[num5];
                        int num7 = 0;
                        while (num6 != 0)
                        {
                            __instance.m_nodes.m_buffer[num6].RenderInstance(cameraInfo, num6, renderGroup.m_instanceMask);
                            num6 = __instance.m_nodes.m_buffer[num6].m_nextGridNode;
                            if (++num7 >= 32768)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                    }
                }
                for (int l = num2; l <= num4; l++)
                {
                    for (int m = num; m <= num3; m++)
                    {
                        int num8 = l * 270 + m;
                        ushort num9 = __instance.m_segmentGrid[num8];
                        int num10 = 0;
                        while (num9 != 0)
                        {
                            __instance.m_segments.m_buffer[num9].RenderInstance(cameraInfo, num9, renderGroup.m_instanceMask);
                            num9 = __instance.m_segments.m_buffer[num9].m_nextGridSegment;
                            if (++num10 >= 36864)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                    }
                }
            }
            __instance.m_lastVisibleRoadNameSegment = __instance.m_visibleRoadNameSegment;
            __instance.m_lastVisibleTrafficLightNode = __instance.m_visibleTrafficLightNode;
            int num11 = PrefabCollection<NetInfo>.PrefabCount();

            var action = new Action(() =>
            {
                for (int n = 0; n < num11; n++)
                {
                    NetInfo prefab = PrefabCollection<NetInfo>.GetPrefab((uint)n);
                    if ((object)prefab == null)
                    {
                        continue;
                    }
                    if (prefab.m_segments != null)
                    {
                        for (int num12 = 0; num12 < prefab.m_segments.Length; num12++)
                        {
                            NetInfo.Segment segment = prefab.m_segments[num12];
                            NetInfo.LodValue combinedLod = segment.m_combinedLod;
                            if (combinedLod != null && combinedLod.m_lodCount != 0)
                            {
                                NetSegment.RenderLod(cameraInfo, combinedLod);
                            }
                        }
                    }
                    if (prefab.m_nodes == null)
                    {
                        continue;
                    }
                    for (int num13 = 0; num13 < prefab.m_nodes.Length; num13++)
                    {
                        NetInfo.Node node = prefab.m_nodes[num13];
                        NetInfo.LodValue combinedLod2 = node.m_combinedLod;
                        if (combinedLod2 != null && combinedLod2.m_lodCount != 0)
                        {
                            if (node.m_directConnect)
                            {
                                NetSegment.RenderLod(cameraInfo, combinedLod2);
                            }
                            else
                            {
                                NetNode.RenderLod(cameraInfo, combinedLod2);
                            }
                        }
                    }
                }
            });
#if UseTask
            Patcher.Dispatcher.Add(action);
#else
            action.Invoke();
#endif

            return false;
        }
    }
}
