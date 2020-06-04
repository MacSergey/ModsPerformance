using ColossalFramework;
using ColossalFramework.Threading;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static RenderManager;

namespace ParallelBooster.Patches
{
    public static class RenderManagerPatch
    {
        public static void Patch(Harmony harmony)
        {
            var originalMethod = AccessTools.Method(typeof(RenderManager), "LateUpdate");
            var prefixMethod = AccessTools.Method(typeof(NetLanePatch), nameof(LateUpdate));
            Patcher.PatchPrefix(harmony, originalMethod, prefixMethod);
        }

        private static MethodInfo UpdateColorMapMethod { get; } = AccessTools.Method(typeof(RenderManager), "UpdateColorMap");
        public static bool LateUpdate(RenderManager __instance, CameraInfo ___m_cameraInfo, uint ___m_currentFrame, LightSystem ___m_lightSystem, FastList<IRenderableManager> ___m_renderables)
        {
            ___m_currentFrame++;
            __instance.m_outOfInstances = false;
            PrefabPool.m_canCreateInstances = 1;
            ___m_lightSystem.m_lightBuffer.Clear();
            __instance.m_overlayBuffer.Clear();
            Singleton<InfoManager>.instance.UpdateInfoMode();
            if (Singleton<LoadingManager>.instance.m_loadingComplete)
            {
                __instance.UpdateCameraInfo();
                //__instance.UpdateColorMap();
                UpdateColorMapMethod.Invoke(__instance, new object[0]);
                try
                {
                    for (int i = 0; i < ___m_renderables.m_size; i++)
                    {
                        ___m_renderables.m_buffer[i].BeginRendering(___m_cameraInfo);
                    }
                }
                finally
                {
                }
                try
                {
                    Vector3 min = ___m_cameraInfo.m_bounds.min;
                    Vector3 max = ___m_cameraInfo.m_bounds.max;
                    if (___m_cameraInfo.m_shadowOffset.x < 0f)
                    {
                        max.x -= ___m_cameraInfo.m_shadowOffset.x;
                    }
                    else
                    {
                        min.x -= ___m_cameraInfo.m_shadowOffset.x;
                    }
                    if (___m_cameraInfo.m_shadowOffset.z < 0f)
                    {
                        max.z -= ___m_cameraInfo.m_shadowOffset.z;
                    }
                    else
                    {
                        min.z -= ___m_cameraInfo.m_shadowOffset.z;
                    }
                    int num = Mathf.Max((int)((min.x - 128f) / 384f + 22.5f), 0);
                    int num2 = Mathf.Max((int)((min.z - 128f) / 384f + 22.5f), 0);
                    int num3 = Mathf.Min((int)((max.x + 128f) / 384f + 22.5f), 44);
                    int num4 = Mathf.Min((int)((max.z + 128f) / 384f + 22.5f), 44);
                    int num5 = 5;
                    int num6 = 10000;
                    int num7 = 10000;
                    int num8 = -10000;
                    int num9 = -10000;
                    __instance.m_renderedGroups.Clear();
                    for (int j = num2; j <= num4; j++)
                    {
                        for (int k = num; k <= num3; k++)
                        {
                            int num10 = j * 45 + k;
                            RenderGroup renderGroup = __instance.m_groups[num10];
                            if (renderGroup != null && renderGroup.Render(___m_cameraInfo))
                            {
                                __instance.m_renderedGroups.Add(renderGroup);
                                int num11 = k / num5;
                                int num12 = j / num5;
                                int num13 = num12 * 9 + num11;
                                MegaRenderGroup megaRenderGroup = __instance.m_megaGroups[num13];
                                if (megaRenderGroup != null)
                                {
                                    megaRenderGroup.m_layersRendered2 |= (megaRenderGroup.m_layersRendered1 & renderGroup.m_layersRendered);
                                    megaRenderGroup.m_layersRendered1 |= renderGroup.m_layersRendered;
                                    megaRenderGroup.m_instanceMask |= renderGroup.m_instanceMask;
                                    num6 = Mathf.Min(num6, num11);
                                    num7 = Mathf.Min(num7, num12);
                                    num8 = Mathf.Max(num8, num11);
                                    num9 = Mathf.Max(num9, num12);
                                }
                            }
                        }
                    }
                    for (int l = num7; l <= num9; l++)
                    {
                        for (int m = num6; m <= num8; m++)
                        {
                            int num14 = l * 9 + m;
                            __instance.m_megaGroups[num14]?.Render();
                        }
                    }
                    for (int n = 0; n < __instance.m_renderedGroups.m_size; n++)
                    {
                        RenderGroup renderGroup2 = __instance.m_renderedGroups.m_buffer[n];
                        int num15 = renderGroup2.m_x / num5;
                        int num16 = renderGroup2.m_z / num5;
                        int num17 = num16 * 9 + num15;
                        MegaRenderGroup megaRenderGroup2 = __instance.m_megaGroups[num17];
                        if (megaRenderGroup2 != null && megaRenderGroup2.m_groupMask != 0)
                        {
                            renderGroup2.Render(megaRenderGroup2.m_groupMask);
                        }
                    }
                }
                finally
                {
                }
                try
                {
                    for (int num18 = 0; num18 < ___m_renderables.m_size; num18++)
                    {
                        ___m_renderables.m_buffer[num18].EndRendering(___m_cameraInfo);
                    }


#if Debug
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
                finally
                {
                }
                ___m_lightSystem.EndRendering(___m_cameraInfo);
            }

            return false;
        }
    }
}
