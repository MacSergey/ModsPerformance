using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.Threading;
using ColossalFramework.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using UnityEngine;
using static RenderManager;

namespace ModsPerformance
{
    public static partial class Patcher
    {
        private static bool RenderManagerLateUpdatePrefix(RenderManager __instance, ref uint ___m_currentFrame, ref FastList<IRenderableManager> ___m_renderables, ref CameraInfo ___m_cameraInfo, ref LightSystem ___m_lightSystem)
        {
            var allSW = new Stopwatch();
            var beginRenderingSW = new Stopwatch();
            var renderCameraInfoSW = new Stopwatch();
            var renderMegaGroupSW = new Stopwatch();
            var renderGroupMaskSW = new Stopwatch();
            var endRenderingSW = new Stopwatch();
            var endRenderingSWdetails = new Dictionary<string, Stopwatch>();

            allSW.Start();
            ___m_currentFrame++;
            __instance.m_outOfInstances = false;
            PrefabPool.m_canCreateInstances = 1;
            ___m_lightSystem.m_lightBuffer.Clear();
            __instance.m_overlayBuffer.Clear();
            Singleton<InfoManager>.instance.UpdateInfoMode();
            if (Singleton<LoadingManager>.instance.m_loadingComplete)
            {
                __instance.UpdateCameraInfo();
                var updateColorMapMethod = AccessTools.Method(typeof(RenderManager), "UpdateColorMap");
                updateColorMapMethod.Invoke(__instance, new object[0]);
                //UpdateColorMap();
                try
                {
                    beginRenderingSW.Start();
                    for (int i = 0; i < ___m_renderables.m_size; i++)
                    {
                        ___m_renderables.m_buffer[i].BeginRendering(___m_cameraInfo);
                    }
                    beginRenderingSW.Stop();
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


                    renderCameraInfoSW.Start();
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
                    renderCameraInfoSW.Stop();

                    renderMegaGroupSW.Start();
                    for (int l = num7; l <= num9; l++)
                    {
                        for (int m = num6; m <= num8; m++)
                        {
                            int num14 = l * 9 + m;
                            __instance.m_megaGroups[num14]?.Render();
                        }
                    }
                    renderMegaGroupSW.Stop();

                    renderGroupMaskSW.Start();
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
                    renderGroupMaskSW.Stop();
                }
                finally
                {
                }
                try
                {
                    endRenderingSW.Start();
                    for (int num18 = 0; num18 < ___m_renderables.m_size; num18++)
                    {
                        var endRenderingSWDetail = Stopwatch.StartNew();
                        ___m_renderables.m_buffer[num18].EndRendering(___m_cameraInfo);
                        endRenderingSWDetail.Stop();
                        endRenderingSWdetails.Add(___m_renderables.m_buffer[num18].GetType().Name, endRenderingSWDetail);
                    }
                    endRenderingSW.Stop();
                }
                finally
                {
                }
                ___m_lightSystem.EndRendering(___m_cameraInfo);
            }

            var swResult = new List<string>()
            {
                SWResult(nameof(allSW), allSW, allSW),
                //SWResult(nameof(beginRenderingSW), beginRenderingSW, allSW),
                //SWResult(nameof(renderCameraInfoSW), renderCameraInfoSW, allSW),
                //SWResult(nameof(renderMegaGroupSW), renderMegaGroupSW, allSW),
                //SWResult(nameof(renderGroupMaskSW), renderGroupMaskSW, allSW),
                SWResult(nameof(endRenderingSW), endRenderingSW, allSW),
            };
            foreach (var endRenderingSWdetail in endRenderingSWdetails)
            {
                swResult.Add(SWResult($"{endRenderingSWdetail.Key}.EndRendering", endRenderingSWdetail.Value, allSW));
            }

            Debug(string.Join("", swResult.ToArray()));

            return false;

            string SWResult(string name, Stopwatch current, Stopwatch all) => $"\n{name}={current.ElapsedTicks}({(int)((double)current.ElapsedTicks * 100 / (double)all.ElapsedTicks)}%)";
        }

        private static int TaskCount => 1;
        private static bool UseTasks => true;
        private static bool UsePatch => UseTasks && true;
        private static CustomDispatcher CustomDispatcher { get; } = new CustomDispatcher();
        public static Stopwatch Stopwatch { get; } = new Stopwatch();
        private static bool EndRenderingImplPrefix(NetManager __instance, CameraInfo cameraInfo)
        {
            __instance.m_nameInstanceBuffer.Clear();
            __instance.m_visibleRoadNameSegment = 0;
            __instance.m_visibleTrafficLightNode = 0;

            RenderGroups(__instance, cameraInfo);

            __instance.m_lastVisibleRoadNameSegment = __instance.m_visibleRoadNameSegment;
            __instance.m_lastVisibleTrafficLightNode = __instance.m_visibleTrafficLightNode;
            int num11 = PrefabCollection<NetInfo>.PrefabCount();
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

            return false;
        }

        private static void RenderGroups(NetManager __instance, CameraInfo cameraInfo)
        {
            int index = -1;
            var lockObject = new object();

            var indexGetter = new Func<int>(() =>
            {
                lock (lockObject)
                {
                    return (index += 1);
                }
            });

            FastList<RenderGroup> renderedGroups = Singleton<RenderManager>.instance.m_renderedGroups;

            var action = new Action<int>((taskNum) => RenderGroupsTask(taskNum, __instance, cameraInfo, renderedGroups, indexGetter));

            Debug($"Start. {nameof(renderedGroups)}={renderedGroups.m_size} (thread={Thread.CurrentThread.ManagedThreadId})");
            var sw = Stopwatch.StartNew();
            Stopwatch.Reset();

            if (UseTasks)
            {
                CustomDispatcher.Clear();

                var tasks = Enumerable.Range(0, TaskCount).Select(taskNum => Task.Create(() => action(taskNum))).ToArray();

                foreach (var task in tasks)
                {
                    task.Run();
                }

                var dipsSw = new Stopwatch();

                while (tasks.Any(t => !t.hasEnded) || !CustomDispatcher.IsDone)
                {
                    dipsSw.Start();
                    CustomDispatcher.Execute();
                    dipsSw.Stop();
                }
                //Task.WaitAll(tasks);
                //dipsSw.Start();
                //CustomDispatcher.Execute();
                //dipsSw.Stop();


                Debug($"Dispatcher duration {dipsSw.ElapsedTicks}");
            }
            else
                action.Invoke(0);

            sw.Stop();
            Debug($"DrawMesh {Stopwatch.ElapsedTicks}");
            Debug($"End {sw.ElapsedTicks}");
        }

        private static void RenderGroupsTask(int taskNum, NetManager __instance, CameraInfo cameraInfo, FastList<RenderGroup> renderedGroups, Func<int> indexGetter)
        {
            Debug($"Start task #{taskNum} (thread={Thread.CurrentThread.ManagedThreadId})");
            var tasksw = Stopwatch.StartNew();

            for (int i = indexGetter(); i < renderedGroups.m_size; i = indexGetter())
            {
                //Debug($"task #{taskNum} i={i}");

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
                            //Debug($"task #{taskNum} Render Node #{num6}");
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
                            //Debug($"task #{taskNum} Render Segment #{num9}");
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

            tasksw.Stop();
            Debug($"Task #{taskNum} end {tasksw.ElapsedTicks}");
        }


        #region NetNodeRenderInstance
        public static bool NetNodePublicRenderInstancePrefix(NetNode __instance, RenderManager.CameraInfo cameraInfo, ushort nodeID, int layerMask)
        {
            if (__instance.m_flags == NetNode.Flags.None)
            {
                return false;
            }
            NetInfo info = __instance.Info;
            if (!cameraInfo.Intersect(__instance.m_bounds))
            {
                return false;
            }
            if (__instance.m_problems != Notification.Problem.None && (layerMask & (1 << Singleton<NotificationManager>.instance.m_notificationLayer)) != 0 && (__instance.m_flags & NetNode.Flags.Temporary) == 0)
            {
                Vector3 position = __instance.m_position;
                position.y += Mathf.Max(5f, info.m_maxHeight);
                Notification.RenderInstance(cameraInfo, __instance.m_problems, position, 1f);
            }
            if ((layerMask & info.m_netLayers) == 0 || (__instance.m_flags & (NetNode.Flags.End | NetNode.Flags.Bend | NetNode.Flags.Junction)) == 0)
            {
                return false;
            }
            if ((__instance.m_flags & NetNode.Flags.Bend) != 0)
            {
                if (info.m_segments == null || info.m_segments.Length == 0)
                {
                    return false;
                }
            }
            else if (info.m_nodes == null || info.m_nodes.Length == 0)
            {
                return false;
            }

            var calculateRendererCountMethod = AccessTools.Method(typeof(NetNode), "CalculateRendererCount");
            uint count = (uint)(int)calculateRendererCountMethod.Invoke(__instance, new object[] { info });
            RenderManager instance = Singleton<RenderManager>.instance;

            if (instance.RequireInstance((uint)(86016 + nodeID), count, out uint instanceIndex))
            {
                int num = 0;
                var renderInstanceMethod = AccessTools.Method(typeof(NetNode), "RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() });

                while (instanceIndex != 65535)
                {
                    NetNodePrivateRenderInstancePatch(__instance, cameraInfo, nodeID, info, num, __instance.m_flags, ref instanceIndex, ref instance.m_instances[instanceIndex]);

                    //var args = new object[] { cameraInfo, nodeID, info, num, __instance.m_flags, instanceIndex, instance.m_instances[instanceIndex] };
                    //renderInstanceMethod.Invoke(__instance, args);
                    //instance.m_instances[instanceIndex] = (Instance)args[6];
                    //instanceIndex = (uint)args[5];

                    if (++num > 36)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
            }
            info.m_netAI.RenderNode(nodeID, ref __instance, cameraInfo);


            return false;
        }
        public static bool NetNodePublicRenderInstancePatch(NetNode __instance, RenderManager.CameraInfo cameraInfo, ushort nodeID, int layerMask)
        {
            if (__instance.m_flags == NetNode.Flags.None)
            {
                return false;
            }
            NetInfo info = __instance.Info;
            if (!cameraInfo.Intersect(__instance.m_bounds))
            {
                return false;
            }
            if (__instance.m_problems != Notification.Problem.None && (layerMask & (1 << Singleton<NotificationManager>.instance.m_notificationLayer)) != 0 && (__instance.m_flags & NetNode.Flags.Temporary) == 0)
            {
                Vector3 position = __instance.m_position;
                position.y += Mathf.Max(5f, info.m_maxHeight);
                Notification.RenderInstance(cameraInfo, __instance.m_problems, position, 1f);
            }
            if ((layerMask & info.m_netLayers) == 0 || (__instance.m_flags & (NetNode.Flags.End | NetNode.Flags.Bend | NetNode.Flags.Junction)) == 0)
            {
                return false;
            }
            if ((__instance.m_flags & NetNode.Flags.Bend) != 0)
            {
                if (info.m_segments == null || info.m_segments.Length == 0)
                {
                    return false;
                }
            }
            else if (info.m_nodes == null || info.m_nodes.Length == 0)
            {
                return false;
            }

            var calculateRendererCountMethod = AccessTools.Method(typeof(NetNode), "CalculateRendererCount");
            uint count = (uint)(int)calculateRendererCountMethod.Invoke(__instance, new object[] { info });
            RenderManager instance = Singleton<RenderManager>.instance;

            if (instance.RequireInstance((uint)(86016 + nodeID), count, out uint instanceIndex))
            {
                var action = new Action(() =>
                {
                    var renderInstanceMethod = AccessTools.Method(typeof(NetNode), "RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() });

                    int num = 0;
                    while (instanceIndex != 65535)
                    {
                        var args = new object[] { cameraInfo, nodeID, info, num, __instance.m_flags, instanceIndex, instance.m_instances[instanceIndex] };
                        renderInstanceMethod.Invoke(__instance, args);
                        instance.m_instances[instanceIndex] = (Instance)args[6];
                        instanceIndex = (uint)args[5];

                        if (++num > 36)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                });

                if (UseTasks)
                    CustomDispatcher.Add(action);
                else
                    action();
            }
            info.m_netAI.RenderNode(nodeID, ref __instance, cameraInfo);


            return false;
        }

        private static MethodBase RefreshJunctionDataMethod { get; set; }
        private static MethodBase RefreshBendDataMethod { get; set; }
        private static MethodBase RefreshEndDataMethod { get; set; }


        private static bool NetNodePrivateRenderInstancePatch(NetNode __instance, RenderManager.CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, ref uint instanceIndex, ref RenderManager.Instance data)
        {
            //Debug($"{nameof(NetNodePrivateRenderInstancePatch)} {nameof(nodeID)}={nodeID} (thread={Thread.CurrentThread.ManagedThreadId})");

            if (data.m_dirty)
            {
                data.m_dirty = false;
                if (iter == 0)
                {
                    if ((flags & NetNode.Flags.Junction) != 0)
                    {
                        var args = new object[] { nodeID, info, instanceIndex };
                        RefreshJunctionDataMethod.Invoke(__instance, args);
                    }
                    else if ((flags & NetNode.Flags.Bend) != 0)
                    {
                        var args = new object[] { nodeID, info, instanceIndex, data };
                        RefreshBendDataMethod.Invoke(__instance, args);
                        data = (Instance)args[3];
                    }
                    else if ((flags & NetNode.Flags.End) != 0)
                    {
                        var args = new object[] { nodeID, info, instanceIndex, data };
                        RefreshEndDataMethod.Invoke(__instance, args);
                        data = (Instance)args[3];
                    }
                }
            }
            if (data.m_initialized)
            {
                var localData = data;
                Action renderAction = null;

                if ((flags & NetNode.Flags.Junction) != 0)
                {
                    if ((data.m_dataInt0 & 8) != 0)
                    {
                        ushort segment = __instance.GetSegment(data.m_dataInt0 & 7);
                        ushort segment2 = __instance.GetSegment(data.m_dataInt0 >> 4);
                        if (segment != 0 && segment2 != 0)
                        {
                            //Debug($"{nameof(renderAction)} = {nameof(NetNodeRenderMainThread1)} (thread={Thread.CurrentThread.ManagedThreadId})");
                            renderAction = new Action(() => NetNodeRenderMainThread1(localData, cameraInfo, nodeID, flags, info, segment, segment2));
                        }
                    }
                    else
                    {
                        ushort segment3 = __instance.GetSegment(data.m_dataInt0 & 7);
                        if (segment3 != 0)
                        {
                            //Debug($"{nameof(renderAction)} = {nameof(NetNodeRenderMainThread2)} (thread={Thread.CurrentThread.ManagedThreadId})");
                            renderAction = new Action(() => NetNodeRenderMainThread2(localData, cameraInfo, flags, segment3));
                        }
                    }
                }
                else if ((flags & NetNode.Flags.End) != 0)
                {
                    //Debug($"{nameof(renderAction)} = {nameof(NetNodeRenderMainThread3)} (thread={Thread.CurrentThread.ManagedThreadId})");
                    renderAction = new Action(() => NetNodeRenderMainThread3(localData, cameraInfo, flags, info));
                }
                else if ((flags & NetNode.Flags.Bend) != 0)
                {
                    //Debug($"{nameof(renderAction)} = {nameof(NetNodeRenderMainThread4)} (thread={Thread.CurrentThread.ManagedThreadId})");
                    renderAction = new Action(() => NetNodeRenderMainThread4(__instance, localData, cameraInfo, nodeID, flags, info));
                }

                if (UseTasks)
                    CustomDispatcher.Add(renderAction);
                else
                    renderAction?.Invoke();
            }
            instanceIndex = data.m_nextInstance;

            return false;
        }

        private static void NetNodeRenderMainThread1(Instance data, RenderManager.CameraInfo cameraInfo, ushort nodeID, NetNode.Flags flags, NetInfo info, ushort segment, ushort segment2)
        {
            //Debug($"{nameof(NetNodeRenderMainThread1)} (thread={Thread.CurrentThread.ManagedThreadId})");

            NetManager instance = Singleton<NetManager>.instance;
            info = instance.m_segments.m_buffer[segment].Info;
            NetInfo info2 = instance.m_segments.m_buffer[segment2].Info;
            NetNode.Flags flags2 = flags;
            if (((instance.m_segments.m_buffer[segment].m_flags | instance.m_segments.m_buffer[segment2].m_flags) & NetSegment.Flags.Collapsed) != 0)
            {
                flags2 |= NetNode.Flags.Collapsed;
            }

            for (int i = 0; i < info.m_nodes.Length; i++)
            {
                NetInfo.Node node = info.m_nodes[i];
                if (!node.CheckFlags(flags2) || !node.m_directConnect || (node.m_connectGroup != 0 && (node.m_connectGroup & info2.m_connectGroup & NetInfo.ConnectGroup.AllGroups) == 0))
                {
                    continue;
                }
                Vector4 dataVector = data.m_dataVector3;
                Vector4 dataVector2 = data.m_dataVector0;
                if (node.m_requireWindSpeed)
                {
                    dataVector.w = data.m_dataFloat0;
                }
                if ((node.m_connectGroup & NetInfo.ConnectGroup.Oneway) != 0)
                {
                    bool flag = instance.m_segments.m_buffer[segment].m_startNode == nodeID == ((instance.m_segments.m_buffer[segment].m_flags & NetSegment.Flags.Invert) == 0);
                    if (info2.m_hasBackwardVehicleLanes != info2.m_hasForwardVehicleLanes || (node.m_connectGroup & NetInfo.ConnectGroup.Directional) != 0)
                    {
                        bool flag2 = instance.m_segments.m_buffer[segment2].m_startNode == nodeID == ((instance.m_segments.m_buffer[segment2].m_flags & NetSegment.Flags.Invert) == 0);
                        if (flag == flag2)
                        {
                            continue;
                        }
                    }
                    if (flag)
                    {
                        if ((node.m_connectGroup & NetInfo.ConnectGroup.OnewayStart) == 0)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if ((node.m_connectGroup & NetInfo.ConnectGroup.OnewayEnd) == 0)
                        {
                            continue;
                        }
                        dataVector2.x = 0f - dataVector2.x;
                        dataVector2.y = 0f - dataVector2.y;
                    }
                }
                if (cameraInfo.CheckRenderDistance(data.m_position, node.m_lodRenderDistance))
                {
                    Stopwatch.Start();
                    instance.m_materialBlock.Clear();
                    instance.m_materialBlock.SetMatrix(instance.ID_LeftMatrix, data.m_dataMatrix0);
                    instance.m_materialBlock.SetMatrix(instance.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                    instance.m_materialBlock.SetVector(instance.ID_MeshScale, dataVector2);
                    instance.m_materialBlock.SetVector(instance.ID_ObjectIndex, dataVector);
                    instance.m_materialBlock.SetColor(instance.ID_Color, data.m_dataColor0);
                    if (node.m_requireSurfaceMaps && data.m_dataTexture1 != null)
                    {
                        instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexA, data.m_dataTexture0);
                        instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexB, data.m_dataTexture1);
                        instance.m_materialBlock.SetVector(instance.ID_SurfaceMapping, data.m_dataVector1);
                    }
                    instance.m_drawCallData.m_defaultCalls++;
                    
                    Graphics.DrawMesh(node.m_nodeMesh, data.m_position, data.m_rotation, node.m_nodeMaterial, node.m_layer, null, 0, instance.m_materialBlock);
                    Stopwatch.Stop();
                    continue;
                }
                NetInfo.LodValue combinedLod = node.m_combinedLod;
                if (combinedLod == null)
                {
                    continue;
                }
                if (node.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod.m_surfaceTexA)
                {
                    if (combinedLod.m_lodCount != 0)
                    {
                        NetSegment.RenderLod(cameraInfo, combinedLod);
                    }
                    combinedLod.m_surfaceTexA = data.m_dataTexture0;
                    combinedLod.m_surfaceTexB = data.m_dataTexture1;
                    combinedLod.m_surfaceMapping = data.m_dataVector1;
                }
                combinedLod.m_leftMatrices[combinedLod.m_lodCount] = data.m_dataMatrix0;
                combinedLod.m_rightMatrices[combinedLod.m_lodCount] = data.m_extraData.m_dataMatrix2;
                combinedLod.m_meshScales[combinedLod.m_lodCount] = dataVector2;
                combinedLod.m_objectIndices[combinedLod.m_lodCount] = dataVector;
                combinedLod.m_meshLocations[combinedLod.m_lodCount] = data.m_position;
                combinedLod.m_lodMin = Vector3.Min(combinedLod.m_lodMin, data.m_position);
                combinedLod.m_lodMax = Vector3.Max(combinedLod.m_lodMax, data.m_position);
                if (++combinedLod.m_lodCount == combinedLod.m_leftMatrices.Length)
                {
                    NetSegment.RenderLod(cameraInfo, combinedLod);
                }
            }
        }

        private static void NetNodeRenderMainThread2(Instance data, RenderManager.CameraInfo cameraInfo, NetNode.Flags flags, ushort segment3)
        {
            //Debug($"{nameof(NetNodeRenderMainThread1)} (thread={Thread.CurrentThread.ManagedThreadId})");

            NetManager instance2 = Singleton<NetManager>.instance;
            var info = instance2.m_segments.m_buffer[segment3].Info;

            for (int j = 0; j < info.m_nodes.Length; j++)
            {
                NetInfo.Node node2 = info.m_nodes[j];
                if (!node2.CheckFlags(flags) || node2.m_directConnect)
                {
                    continue;
                }
                Vector4 dataVector3 = data.m_extraData.m_dataVector4;
                if (node2.m_requireWindSpeed)
                {
                    dataVector3.w = data.m_dataFloat0;
                }
                if (cameraInfo.CheckRenderDistance(data.m_position, node2.m_lodRenderDistance))
                {
                    Stopwatch.Start();
                    instance2.m_materialBlock.Clear();
                    instance2.m_materialBlock.SetMatrix(instance2.ID_LeftMatrix, data.m_dataMatrix0);
                    instance2.m_materialBlock.SetMatrix(instance2.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                    instance2.m_materialBlock.SetMatrix(instance2.ID_LeftMatrixB, data.m_extraData.m_dataMatrix3);
                    instance2.m_materialBlock.SetMatrix(instance2.ID_RightMatrixB, data.m_dataMatrix1);
                    instance2.m_materialBlock.SetVector(instance2.ID_MeshScale, data.m_dataVector0);
                    instance2.m_materialBlock.SetVector(instance2.ID_CenterPos, data.m_dataVector1);
                    instance2.m_materialBlock.SetVector(instance2.ID_SideScale, data.m_dataVector2);
                    instance2.m_materialBlock.SetVector(instance2.ID_ObjectIndex, dataVector3);
                    instance2.m_materialBlock.SetColor(instance2.ID_Color, data.m_dataColor0);
                    if (node2.m_requireSurfaceMaps && data.m_dataTexture1 != null)
                    {
                        instance2.m_materialBlock.SetTexture(instance2.ID_SurfaceTexA, data.m_dataTexture0);
                        instance2.m_materialBlock.SetTexture(instance2.ID_SurfaceTexB, data.m_dataTexture1);
                        instance2.m_materialBlock.SetVector(instance2.ID_SurfaceMapping, data.m_dataVector3);
                    }
                    instance2.m_drawCallData.m_defaultCalls++;
                    
                    Graphics.DrawMesh(node2.m_nodeMesh, data.m_position, data.m_rotation, node2.m_nodeMaterial, node2.m_layer, null, 0, instance2.m_materialBlock);
                    Stopwatch.Stop();
                    continue;
                }
                NetInfo.LodValue combinedLod2 = node2.m_combinedLod;
                if (combinedLod2 == null)
                {
                    continue;
                }
                if (node2.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod2.m_surfaceTexA)
                {
                    if (combinedLod2.m_lodCount != 0)
                    {
                        NetNode.RenderLod(cameraInfo, combinedLod2);
                    }
                    combinedLod2.m_surfaceTexA = data.m_dataTexture0;
                    combinedLod2.m_surfaceTexB = data.m_dataTexture1;
                    combinedLod2.m_surfaceMapping = data.m_dataVector3;
                }
                combinedLod2.m_leftMatrices[combinedLod2.m_lodCount] = data.m_dataMatrix0;
                combinedLod2.m_leftMatricesB[combinedLod2.m_lodCount] = data.m_extraData.m_dataMatrix3;
                combinedLod2.m_rightMatrices[combinedLod2.m_lodCount] = data.m_extraData.m_dataMatrix2;
                combinedLod2.m_rightMatricesB[combinedLod2.m_lodCount] = data.m_dataMatrix1;
                combinedLod2.m_meshScales[combinedLod2.m_lodCount] = data.m_dataVector0;
                combinedLod2.m_centerPositions[combinedLod2.m_lodCount] = data.m_dataVector1;
                combinedLod2.m_sideScales[combinedLod2.m_lodCount] = data.m_dataVector2;
                combinedLod2.m_objectIndices[combinedLod2.m_lodCount] = dataVector3;
                combinedLod2.m_meshLocations[combinedLod2.m_lodCount] = data.m_position;
                combinedLod2.m_lodMin = Vector3.Min(combinedLod2.m_lodMin, data.m_position);
                combinedLod2.m_lodMax = Vector3.Max(combinedLod2.m_lodMax, data.m_position);
                if (++combinedLod2.m_lodCount == combinedLod2.m_leftMatrices.Length)
                {
                    NetNode.RenderLod(cameraInfo, combinedLod2);
                }
            }
        }

        private static void NetNodeRenderMainThread3(Instance data, RenderManager.CameraInfo cameraInfo, NetNode.Flags flags, NetInfo info)
        {
            //Debug($"{nameof(NetNodeRenderMainThread1)} (thread={Thread.CurrentThread.ManagedThreadId})");

            NetManager instance3 = Singleton<NetManager>.instance;
            for (int k = 0; k < info.m_nodes.Length; k++)
            {
                NetInfo.Node node3 = info.m_nodes[k];
                if (!node3.CheckFlags(flags) || node3.m_directConnect)
                {
                    continue;
                }
                Vector4 dataVector4 = data.m_extraData.m_dataVector4;
                if (node3.m_requireWindSpeed)
                {
                    dataVector4.w = data.m_dataFloat0;
                }
                if (cameraInfo.CheckRenderDistance(data.m_position, node3.m_lodRenderDistance))
                {
                    Stopwatch.Start();
                    instance3.m_materialBlock.Clear();
                    instance3.m_materialBlock.SetMatrix(instance3.ID_LeftMatrix, data.m_dataMatrix0);
                    instance3.m_materialBlock.SetMatrix(instance3.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                    instance3.m_materialBlock.SetMatrix(instance3.ID_LeftMatrixB, data.m_extraData.m_dataMatrix3);
                    instance3.m_materialBlock.SetMatrix(instance3.ID_RightMatrixB, data.m_dataMatrix1);
                    instance3.m_materialBlock.SetVector(instance3.ID_MeshScale, data.m_dataVector0);
                    instance3.m_materialBlock.SetVector(instance3.ID_CenterPos, data.m_dataVector1);
                    instance3.m_materialBlock.SetVector(instance3.ID_SideScale, data.m_dataVector2);
                    instance3.m_materialBlock.SetVector(instance3.ID_ObjectIndex, dataVector4);
                    instance3.m_materialBlock.SetColor(instance3.ID_Color, data.m_dataColor0);
                    if (node3.m_requireSurfaceMaps && data.m_dataTexture1 != null)
                    {
                        instance3.m_materialBlock.SetTexture(instance3.ID_SurfaceTexA, data.m_dataTexture0);
                        instance3.m_materialBlock.SetTexture(instance3.ID_SurfaceTexB, data.m_dataTexture1);
                        instance3.m_materialBlock.SetVector(instance3.ID_SurfaceMapping, data.m_dataVector3);
                    }
                    instance3.m_drawCallData.m_defaultCalls++;
                    
                    Graphics.DrawMesh(node3.m_nodeMesh, data.m_position, data.m_rotation, node3.m_nodeMaterial, node3.m_layer, null, 0, instance3.m_materialBlock);
                    Stopwatch.Stop();
                    continue;
                }
                NetInfo.LodValue combinedLod3 = node3.m_combinedLod;
                if (combinedLod3 == null)
                {
                    continue;
                }
                if (node3.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod3.m_surfaceTexA)
                {
                    if (combinedLod3.m_lodCount != 0)
                    {
                        NetNode.RenderLod(cameraInfo, combinedLod3);
                    }
                    combinedLod3.m_surfaceTexA = data.m_dataTexture0;
                    combinedLod3.m_surfaceTexB = data.m_dataTexture1;
                    combinedLod3.m_surfaceMapping = data.m_dataVector3;
                }
                combinedLod3.m_leftMatrices[combinedLod3.m_lodCount] = data.m_dataMatrix0;
                combinedLod3.m_leftMatricesB[combinedLod3.m_lodCount] = data.m_extraData.m_dataMatrix3;
                combinedLod3.m_rightMatrices[combinedLod3.m_lodCount] = data.m_extraData.m_dataMatrix2;
                combinedLod3.m_rightMatricesB[combinedLod3.m_lodCount] = data.m_dataMatrix1;
                combinedLod3.m_meshScales[combinedLod3.m_lodCount] = data.m_dataVector0;
                combinedLod3.m_centerPositions[combinedLod3.m_lodCount] = data.m_dataVector1;
                combinedLod3.m_sideScales[combinedLod3.m_lodCount] = data.m_dataVector2;
                combinedLod3.m_objectIndices[combinedLod3.m_lodCount] = dataVector4;
                combinedLod3.m_meshLocations[combinedLod3.m_lodCount] = data.m_position;
                combinedLod3.m_lodMin = Vector3.Min(combinedLod3.m_lodMin, data.m_position);
                combinedLod3.m_lodMax = Vector3.Max(combinedLod3.m_lodMax, data.m_position);
                if (++combinedLod3.m_lodCount == combinedLod3.m_leftMatrices.Length)
                {
                    NetNode.RenderLod(cameraInfo, combinedLod3);
                }
            }
        }

        private static void NetNodeRenderMainThread4(NetNode __instance, Instance data, RenderManager.CameraInfo cameraInfo, ushort nodeID, NetNode.Flags flags, NetInfo info)
        {
            //Debug($"{nameof(NetNodeRenderMainThread1)} (thread={Thread.CurrentThread.ManagedThreadId})");

            NetManager instance4 = Singleton<NetManager>.instance;
            for (int l = 0; l < info.m_segments.Length; l++)
            {
                NetInfo.Segment segment4 = info.m_segments[l];
                if (!segment4.CheckFlags(info.m_netAI.GetBendFlags(nodeID, ref __instance), out bool turnAround) || segment4.m_disableBendNodes)
                {
                    continue;
                }
                Vector4 dataVector5 = data.m_dataVector3;
                Vector4 dataVector6 = data.m_dataVector0;
                if (segment4.m_requireWindSpeed)
                {
                    dataVector5.w = data.m_dataFloat0;
                }
                if (turnAround)
                {
                    dataVector6.x = 0f - dataVector6.x;
                    dataVector6.y = 0f - dataVector6.y;
                }
                if (cameraInfo.CheckRenderDistance(data.m_position, segment4.m_lodRenderDistance))
                {
                    Stopwatch.Start();
                    instance4.m_materialBlock.Clear();
                    instance4.m_materialBlock.SetMatrix(instance4.ID_LeftMatrix, data.m_dataMatrix0);
                    instance4.m_materialBlock.SetMatrix(instance4.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                    instance4.m_materialBlock.SetVector(instance4.ID_MeshScale, dataVector6);
                    instance4.m_materialBlock.SetVector(instance4.ID_ObjectIndex, dataVector5);
                    instance4.m_materialBlock.SetColor(instance4.ID_Color, data.m_dataColor0);
                    if (segment4.m_requireSurfaceMaps && data.m_dataTexture1 != null)
                    {
                        instance4.m_materialBlock.SetTexture(instance4.ID_SurfaceTexA, data.m_dataTexture0);
                        instance4.m_materialBlock.SetTexture(instance4.ID_SurfaceTexB, data.m_dataTexture1);
                        instance4.m_materialBlock.SetVector(instance4.ID_SurfaceMapping, data.m_dataVector1);
                    }
                    instance4.m_drawCallData.m_defaultCalls++;
                    
                    Graphics.DrawMesh(segment4.m_segmentMesh, data.m_position, data.m_rotation, segment4.m_segmentMaterial, segment4.m_layer, null, 0, instance4.m_materialBlock);
                    Stopwatch.Stop();
                    continue;
                }
                NetInfo.LodValue combinedLod4 = segment4.m_combinedLod;
                if (combinedLod4 == null)
                {
                    continue;
                }
                if (segment4.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod4.m_surfaceTexA)
                {
                    if (combinedLod4.m_lodCount != 0)
                    {
                        NetSegment.RenderLod(cameraInfo, combinedLod4);
                    }
                    combinedLod4.m_surfaceTexA = data.m_dataTexture0;
                    combinedLod4.m_surfaceTexB = data.m_dataTexture1;
                    combinedLod4.m_surfaceMapping = data.m_dataVector1;
                }
                combinedLod4.m_leftMatrices[combinedLod4.m_lodCount] = data.m_dataMatrix0;
                combinedLod4.m_rightMatrices[combinedLod4.m_lodCount] = data.m_extraData.m_dataMatrix2;
                combinedLod4.m_meshScales[combinedLod4.m_lodCount] = dataVector6;
                combinedLod4.m_objectIndices[combinedLod4.m_lodCount] = dataVector5;
                combinedLod4.m_meshLocations[combinedLod4.m_lodCount] = data.m_position;
                combinedLod4.m_lodMin = Vector3.Min(combinedLod4.m_lodMin, data.m_position);
                combinedLod4.m_lodMax = Vector3.Max(combinedLod4.m_lodMax, data.m_position);
                if (++combinedLod4.m_lodCount == combinedLod4.m_leftMatrices.Length)
                {
                    NetSegment.RenderLod(cameraInfo, combinedLod4);
                }
            }
            for (int m = 0; m < info.m_nodes.Length; m++)
            {
                ushort segment5 = __instance.GetSegment(data.m_dataInt0 & 7);
                ushort segment6 = __instance.GetSegment(data.m_dataInt0 >> 4);
                NetNode.Flags flags3 = flags;
                if (((instance4.m_segments.m_buffer[segment5].m_flags | instance4.m_segments.m_buffer[segment6].m_flags) & NetSegment.Flags.Collapsed) != 0)
                {
                    flags3 |= NetNode.Flags.Collapsed;
                }
                NetInfo.Node node4 = info.m_nodes[m];
                if (!node4.CheckFlags(flags) || !node4.m_directConnect || (node4.m_connectGroup != 0 && (node4.m_connectGroup & info.m_connectGroup & NetInfo.ConnectGroup.AllGroups) == 0))
                {
                    continue;
                }
                Vector4 dataVector7 = data.m_dataVector3;
                Vector4 dataVector8 = data.m_dataVector0;
                if (node4.m_requireWindSpeed)
                {
                    dataVector7.w = data.m_dataFloat0;
                }
                if ((node4.m_connectGroup & NetInfo.ConnectGroup.Oneway) != 0)
                {
                    bool flag3 = instance4.m_segments.m_buffer[segment5].m_startNode == nodeID == ((instance4.m_segments.m_buffer[segment5].m_flags & NetSegment.Flags.Invert) == 0);
                    bool flag4 = instance4.m_segments.m_buffer[segment6].m_startNode == nodeID == ((instance4.m_segments.m_buffer[segment6].m_flags & NetSegment.Flags.Invert) == 0);
                    if (flag3 == flag4)
                    {
                        continue;
                    }
                    if (flag3)
                    {
                        if ((node4.m_connectGroup & NetInfo.ConnectGroup.OnewayStart) == 0)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if ((node4.m_connectGroup & NetInfo.ConnectGroup.OnewayEnd) == 0)
                        {
                            continue;
                        }
                        dataVector8.x = 0f - dataVector8.x;
                        dataVector8.y = 0f - dataVector8.y;
                    }
                }
                if (cameraInfo.CheckRenderDistance(data.m_position, node4.m_lodRenderDistance))
                {
                    Stopwatch.Start();
                    instance4.m_materialBlock.Clear();
                    instance4.m_materialBlock.SetMatrix(instance4.ID_LeftMatrix, data.m_dataMatrix0);
                    instance4.m_materialBlock.SetMatrix(instance4.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                    instance4.m_materialBlock.SetVector(instance4.ID_MeshScale, dataVector8);
                    instance4.m_materialBlock.SetVector(instance4.ID_ObjectIndex, dataVector7);
                    instance4.m_materialBlock.SetColor(instance4.ID_Color, data.m_dataColor0);
                    if (node4.m_requireSurfaceMaps && data.m_dataTexture1 != null)
                    {
                        instance4.m_materialBlock.SetTexture(instance4.ID_SurfaceTexA, data.m_dataTexture0);
                        instance4.m_materialBlock.SetTexture(instance4.ID_SurfaceTexB, data.m_dataTexture1);
                        instance4.m_materialBlock.SetVector(instance4.ID_SurfaceMapping, data.m_dataVector1);
                    }
                    instance4.m_drawCallData.m_defaultCalls++;
                    
                    Graphics.DrawMesh(node4.m_nodeMesh, data.m_position, data.m_rotation, node4.m_nodeMaterial, node4.m_layer, null, 0, instance4.m_materialBlock);
                    Stopwatch.Stop();
                    continue;
                }
                NetInfo.LodValue combinedLod5 = node4.m_combinedLod;
                if (combinedLod5 == null)
                {
                    continue;
                }
                if (node4.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod5.m_surfaceTexA)
                {
                    if (combinedLod5.m_lodCount != 0)
                    {
                        NetSegment.RenderLod(cameraInfo, combinedLod5);
                    }
                    combinedLod5.m_surfaceTexA = data.m_dataTexture0;
                    combinedLod5.m_surfaceTexB = data.m_dataTexture1;
                    combinedLod5.m_surfaceMapping = data.m_dataVector1;
                }
                combinedLod5.m_leftMatrices[combinedLod5.m_lodCount] = data.m_dataMatrix0;
                combinedLod5.m_rightMatrices[combinedLod5.m_lodCount] = data.m_extraData.m_dataMatrix2;
                combinedLod5.m_meshScales[combinedLod5.m_lodCount] = dataVector8;
                combinedLod5.m_objectIndices[combinedLod5.m_lodCount] = dataVector7;
                combinedLod5.m_meshLocations[combinedLod5.m_lodCount] = data.m_position;
                combinedLod5.m_lodMin = Vector3.Min(combinedLod5.m_lodMin, data.m_position);
                combinedLod5.m_lodMax = Vector3.Max(combinedLod5.m_lodMax, data.m_position);
                if (++combinedLod5.m_lodCount == combinedLod5.m_leftMatrices.Length)
                {
                    NetSegment.RenderLod(cameraInfo, combinedLod5);
                }
            }
        }

        #endregion

        #region NetSegmentRenderInstance

        public static bool NetSegmentPublicRenderInstancePatch(NetSegment __instance, RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask)
        {
            if (__instance.m_flags == NetSegment.Flags.None)
            {
                return false;
            }
            NetInfo info = __instance.Info;
            if (!cameraInfo.Intersect(__instance.m_bounds))
            {
                return false;
            }
            if (__instance.m_problems != Notification.Problem.None && (layerMask & (1 << Singleton<NotificationManager>.instance.m_notificationLayer)) != 0)
            {
                Vector3 middlePosition = __instance.m_middlePosition;
                middlePosition.y += Mathf.Max(5f, info.m_maxHeight);
                Notification.RenderInstance(cameraInfo, __instance.m_problems, middlePosition, 1f);
            }
            if ((layerMask & (info.m_netLayers | info.m_propLayers)) == 0)
            {
                return false;
            }
            RenderManager instance = Singleton<RenderManager>.instance;
            if (instance.RequireInstance((uint)(49152 + segmentID), 1u, out uint instanceIndex))
            {
                CustomDispatcher.Add(() =>
                {
                    var renderInstanceMethod = AccessTools.Method(typeof(NetSegment), "RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int), typeof(NetInfo), typeof(RenderManager.Instance).MakeByRefType() });
                    var args = new object[] { cameraInfo, segmentID, layerMask, info, instance.m_instances[instanceIndex] };
                    renderInstanceMethod.Invoke(__instance, args);
                    instance.m_instances[instanceIndex] = (Instance)args[4];

                    if (instance.m_instances[instanceIndex].m_nameData != null)
                    {
                        NetManager instance2 = Singleton<NetManager>.instance;
                        instance2.m_visibleRoadNameSegment = segmentID;
                        instance2.m_nameInstanceBuffer.Add(instanceIndex);
                    }
                });
            }

            return false;
        }

        public static bool NetSegmentPrivareRenderInstancePatch(NetSegment __instance, RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask, NetInfo info, ref RenderManager.Instance data)
        {
            NetManager instance = Singleton<NetManager>.instance;
            if (data.m_dirty)
            {
                data.m_dirty = false;
                Vector3 position = instance.m_nodes.m_buffer[__instance.m_startNode].m_position;
                Vector3 position2 = instance.m_nodes.m_buffer[__instance.m_endNode].m_position;
                data.m_position = (position + position2) * 0.5f;
                data.m_rotation = Quaternion.identity;
                data.m_dataColor0 = info.m_color;
                data.m_dataColor0.a = 0f;
                data.m_dataFloat0 = Singleton<WeatherManager>.instance.GetWindSpeed(data.m_position);
                data.m_dataVector0 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 1f, 1f);
                Vector4 colorLocation = RenderManager.GetColorLocation((uint)(49152 + segmentID));
                Vector4 vector = colorLocation;
                if (NetNode.BlendJunction(__instance.m_startNode))
                {
                    colorLocation = RenderManager.GetColorLocation((uint)(86016 + __instance.m_startNode));
                }
                if (NetNode.BlendJunction(__instance.m_endNode))
                {
                    vector = RenderManager.GetColorLocation((uint)(86016 + __instance.m_endNode));
                }
                data.m_dataVector3 = new Vector4(colorLocation.x, colorLocation.y, vector.x, vector.y);
                if (info.m_segments == null || info.m_segments.Length == 0)
                {
                    if (info.m_lanes != null)
                    {
                        bool invert;
                        NetNode.Flags flags;
                        Color color;
                        NetNode.Flags flags2;
                        Color color2;
                        if ((__instance.m_flags & NetSegment.Flags.Invert) != 0)
                        {
                            invert = true;
                            NetInfo info2 = instance.m_nodes.m_buffer[__instance.m_endNode].Info;
                            info2.m_netAI.GetNodeState(__instance.m_endNode, ref instance.m_nodes.m_buffer[__instance.m_endNode], segmentID, ref __instance, out flags, out color);
                            NetInfo info3 = instance.m_nodes.m_buffer[__instance.m_startNode].Info;
                            info3.m_netAI.GetNodeState(__instance.m_startNode, ref instance.m_nodes.m_buffer[__instance.m_startNode], segmentID, ref __instance, out flags2, out color2);
                        }
                        else
                        {
                            invert = false;
                            NetInfo info4 = instance.m_nodes.m_buffer[__instance.m_startNode].Info;
                            info4.m_netAI.GetNodeState(__instance.m_startNode, ref instance.m_nodes.m_buffer[__instance.m_startNode], segmentID, ref __instance, out flags, out color);
                            NetInfo info5 = instance.m_nodes.m_buffer[__instance.m_endNode].Info;
                            info5.m_netAI.GetNodeState(__instance.m_endNode, ref instance.m_nodes.m_buffer[__instance.m_endNode], segmentID, ref __instance, out flags2, out color2);
                        }
                        float startAngle = (float)(int)__instance.m_cornerAngleStart * ((float)Math.PI / 128f);
                        float endAngle = (float)(int)__instance.m_cornerAngleEnd * ((float)Math.PI / 128f);
                        int propIndex = 0;
                        uint num = __instance.m_lanes;
                        for (int i = 0; i < info.m_lanes.Length; i++)
                        {
                            if (num == 0)
                            {
                                break;
                            }
                            instance.m_lanes.m_buffer[num].RefreshInstance(num, info.m_lanes[i], startAngle, endAngle, invert, ref data, ref propIndex);
                            num = instance.m_lanes.m_buffer[num].m_nextLane;
                        }
                    }
                }
                else
                {
                    float vScale = info.m_netAI.GetVScale();
                    __instance.CalculateCorner(segmentID, heightOffset: true, start: true, leftSide: true, out Vector3 cornerPos, out Vector3 cornerDirection, out bool smooth);
                    __instance.CalculateCorner(segmentID, heightOffset: true, start: false, leftSide: true, out Vector3 cornerPos2, out Vector3 cornerDirection2, out bool smooth2);
                    __instance.CalculateCorner(segmentID, heightOffset: true, start: true, leftSide: false, out Vector3 cornerPos3, out Vector3 cornerDirection3, out smooth);
                    __instance.CalculateCorner(segmentID, heightOffset: true, start: false, leftSide: false, out Vector3 cornerPos4, out Vector3 cornerDirection4, out smooth2);
                    NetSegment.CalculateMiddlePoints(cornerPos, cornerDirection, cornerPos4, cornerDirection4, smooth, smooth2, out Vector3 middlePos, out Vector3 middlePos2);
                    NetSegment.CalculateMiddlePoints(cornerPos3, cornerDirection3, cornerPos2, cornerDirection2, smooth, smooth2, out Vector3 middlePos3, out Vector3 middlePos4);
                    data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(cornerPos, middlePos, middlePos2, cornerPos4, cornerPos3, middlePos3, middlePos4, cornerPos2, data.m_position, vScale);
                    data.m_dataMatrix1 = NetSegment.CalculateControlMatrix(cornerPos3, middlePos3, middlePos4, cornerPos2, cornerPos, middlePos, middlePos2, cornerPos4, data.m_position, vScale);
                }
                if ((__instance.m_flags & NetSegment.Flags.NameVisible2) != 0)
                {
                    string segmentName = instance.GetSegmentName(segmentID);
                    UIFont nameFont = instance.m_properties.m_nameFont;
                    data.m_nameData = Singleton<InstanceManager>.instance.GetNameData(segmentName, nameFont, canCreate: true);
                    if (data.m_nameData != null)
                    {
                        float snapElevation = info.m_netAI.GetSnapElevation();
                        position.y += snapElevation;
                        position2.y += snapElevation;
                        NetSegment.CalculateMiddlePoints(position, __instance.m_startDirection, position2, __instance.m_endDirection, smoothStart: true, smoothEnd: true, out Vector3 middlePos5, out Vector3 middlePos6);
                        data.m_dataMatrix2 = NetSegment.CalculateControlMatrix(position, middlePos5, middlePos6, position2, data.m_position, 1f);
                    }
                }
                else
                {
                    data.m_nameData = null;
                }
                if (info.m_requireSurfaceMaps)
                {
                    Singleton<TerrainManager>.instance.GetSurfaceMapping(data.m_position, out data.m_dataTexture0, out data.m_dataTexture1, out data.m_dataVector1);
                }
                else if (info.m_requireHeightMap)
                {
                    Singleton<TerrainManager>.instance.GetHeightMapping(data.m_position, out data.m_dataTexture0, out data.m_dataVector1, out data.m_dataVector2);
                }
            }
            if (info.m_segments != null && (layerMask & info.m_netLayers) != 0)
            {
                var dataLocal = data;
                var renderAction = new Action(() => NetSegmentRenderMainThread(__instance, cameraInfo, info, dataLocal, instance));
                if (UseTasks)
                    CustomDispatcher.Add(renderAction);
                else
                    renderAction();
            }
            if (info.m_lanes == null || ((layerMask & info.m_propLayers) == 0 && !cameraInfo.CheckRenderDistance(data.m_position, info.m_maxPropDistance + 128f)))
            {
                return false;
            }
            bool invert2;
            NetNode.Flags flags3;
            Color color3;
            NetNode.Flags flags4;
            Color color4;
            if ((__instance.m_flags & NetSegment.Flags.Invert) != 0)
            {
                invert2 = true;
                NetInfo info6 = instance.m_nodes.m_buffer[__instance.m_endNode].Info;
                info6.m_netAI.GetNodeState(__instance.m_endNode, ref instance.m_nodes.m_buffer[__instance.m_endNode], segmentID, ref __instance, out flags3, out color3);
                NetInfo info7 = instance.m_nodes.m_buffer[__instance.m_startNode].Info;
                info7.m_netAI.GetNodeState(__instance.m_startNode, ref instance.m_nodes.m_buffer[__instance.m_startNode], segmentID, ref __instance, out flags4, out color4);
            }
            else
            {
                invert2 = false;
                NetInfo info8 = instance.m_nodes.m_buffer[__instance.m_startNode].Info;
                info8.m_netAI.GetNodeState(__instance.m_startNode, ref instance.m_nodes.m_buffer[__instance.m_startNode], segmentID, ref __instance, out flags3, out color3);
                NetInfo info9 = instance.m_nodes.m_buffer[__instance.m_endNode].Info;
                info9.m_netAI.GetNodeState(__instance.m_endNode, ref instance.m_nodes.m_buffer[__instance.m_endNode], segmentID, ref __instance, out flags4, out color4);
            }
            float startAngle2 = (float)(int)__instance.m_cornerAngleStart * ((float)Math.PI / 128f);
            float endAngle2 = (float)(int)__instance.m_cornerAngleEnd * ((float)Math.PI / 128f);
            Vector4 objectIndex = new Vector4(data.m_dataVector3.x, data.m_dataVector3.y, 1f, data.m_dataFloat0);
            Vector4 objectIndex2 = new Vector4(data.m_dataVector3.z, data.m_dataVector3.w, 1f, data.m_dataFloat0);
            InfoManager.InfoMode currentMode = Singleton<InfoManager>.instance.CurrentMode;
            if (currentMode != 0 && !info.m_netAI.ColorizeProps(currentMode))
            {
                objectIndex.z = 0f;
                objectIndex2.z = 0f;
            }

            var localData = data;
            var action = new Action(() =>
            {
                int propIndex2 = (info.m_segments != null && info.m_segments.Length != 0) ? (-1) : 0;
                uint num2 = __instance.m_lanes;
                if ((__instance.m_flags & NetSegment.Flags.Collapsed) != 0)
                {
                    for (int k = 0; k < info.m_lanes.Length; k++)
                    {
                        if (num2 == 0)
                        {
                            break;
                        }
                        instance.m_lanes.m_buffer[num2].RenderDestroyedInstance(cameraInfo, segmentID, num2, info, info.m_lanes[k], flags3, flags4, color3, color4, startAngle2, endAngle2, invert2, layerMask, objectIndex, objectIndex2, ref localData, ref propIndex2);
                        num2 = instance.m_lanes.m_buffer[num2].m_nextLane;
                    }
                    return;
                }
                for (int l = 0; l < info.m_lanes.Length; l++)
                {
                    if (num2 == 0)
                    {
                        break;
                    }
                    instance.m_lanes.m_buffer[num2].RenderInstance(cameraInfo, segmentID, num2, info.m_lanes[l], flags3, flags4, color3, color4, startAngle2, endAngle2, invert2, layerMask, objectIndex, objectIndex2, ref localData, ref propIndex2);
                    num2 = instance.m_lanes.m_buffer[num2].m_nextLane;
                }
            });
            if (UseTasks)
                CustomDispatcher.Add(action);
            else
                action();

            return false;
        }

        private static void NetSegmentRenderMainThread(NetSegment __instance, RenderManager.CameraInfo cameraInfo, NetInfo info, RenderManager.Instance dataLocal, NetManager instance)
        {
            //NetManager instance = Singleton<NetManager>.instance;

            for (int j = 0; j < info.m_segments.Length; j++)
            {
                NetInfo.Segment segment = info.m_segments[j];
                if (!segment.CheckFlags(__instance.m_flags, out bool turnAround))
                {
                    continue;
                }
                Vector4 dataVector = dataLocal.m_dataVector3;
                Vector4 dataVector2 = dataLocal.m_dataVector0;
                if (segment.m_requireWindSpeed)
                {
                    dataVector.w = dataLocal.m_dataFloat0;
                }
                if (turnAround)
                {
                    dataVector2.x = 0f - dataVector2.x;
                    dataVector2.y = 0f - dataVector2.y;
                }
                if (cameraInfo.CheckRenderDistance(dataLocal.m_position, segment.m_lodRenderDistance))
                {
                    Stopwatch.Start();
                    instance.m_materialBlock.Clear();
                    instance.m_materialBlock.SetMatrix(instance.ID_LeftMatrix, dataLocal.m_dataMatrix0);
                    instance.m_materialBlock.SetMatrix(instance.ID_RightMatrix, dataLocal.m_dataMatrix1);
                    instance.m_materialBlock.SetVector(instance.ID_MeshScale, dataVector2);
                    instance.m_materialBlock.SetVector(instance.ID_ObjectIndex, dataVector);
                    instance.m_materialBlock.SetColor(instance.ID_Color, dataLocal.m_dataColor0);
                    if (segment.m_requireSurfaceMaps && dataLocal.m_dataTexture0 != null)
                    {
                        instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexA, dataLocal.m_dataTexture0);
                        instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexB, dataLocal.m_dataTexture1);
                        instance.m_materialBlock.SetVector(instance.ID_SurfaceMapping, dataLocal.m_dataVector1);
                    }
                    else if (segment.m_requireHeightMap && dataLocal.m_dataTexture0 != null)
                    {
                        instance.m_materialBlock.SetTexture(instance.ID_HeightMap, dataLocal.m_dataTexture0);
                        instance.m_materialBlock.SetVector(instance.ID_HeightMapping, dataLocal.m_dataVector1);
                        instance.m_materialBlock.SetVector(instance.ID_SurfaceMapping, dataLocal.m_dataVector2);
                    }
                    instance.m_drawCallData.m_defaultCalls++;                   
                    Graphics.DrawMesh(segment.m_segmentMesh, dataLocal.m_position, dataLocal.m_rotation, segment.m_segmentMaterial, segment.m_layer, null, 0, instance.m_materialBlock);
                    Stopwatch.Stop();
                    continue;
                }
                NetInfo.LodValue combinedLod = segment.m_combinedLod;
                if (combinedLod == null)
                {
                    continue;
                }
                if (segment.m_requireSurfaceMaps)
                {
                    if (dataLocal.m_dataTexture0 != combinedLod.m_surfaceTexA)
                    {
                        if (combinedLod.m_lodCount != 0)
                        {
                            NetSegment.RenderLod(cameraInfo, combinedLod);
                        }
                        combinedLod.m_surfaceTexA = dataLocal.m_dataTexture0;
                        combinedLod.m_surfaceTexB = dataLocal.m_dataTexture1;
                        combinedLod.m_surfaceMapping = dataLocal.m_dataVector1;
                    }
                }
                else if (segment.m_requireHeightMap && dataLocal.m_dataTexture0 != combinedLod.m_heightMap)
                {
                    if (combinedLod.m_lodCount != 0)
                    {
                        NetSegment.RenderLod(cameraInfo, combinedLod);
                    }
                    combinedLod.m_heightMap = dataLocal.m_dataTexture0;
                    combinedLod.m_heightMapping = dataLocal.m_dataVector1;
                    combinedLod.m_surfaceMapping = dataLocal.m_dataVector2;
                }
                combinedLod.m_leftMatrices[combinedLod.m_lodCount] = dataLocal.m_dataMatrix0;
                combinedLod.m_rightMatrices[combinedLod.m_lodCount] = dataLocal.m_dataMatrix1;
                combinedLod.m_meshScales[combinedLod.m_lodCount] = dataVector2;
                combinedLod.m_objectIndices[combinedLod.m_lodCount] = dataVector;
                combinedLod.m_meshLocations[combinedLod.m_lodCount] = dataLocal.m_position;
                combinedLod.m_lodMin = Vector3.Min(combinedLod.m_lodMin, dataLocal.m_position);
                combinedLod.m_lodMax = Vector3.Max(combinedLod.m_lodMax, dataLocal.m_position);
                if (++combinedLod.m_lodCount == combinedLod.m_leftMatrices.Length)
                {
                    NetSegment.RenderLod(cameraInfo, combinedLod);
                }
            }
        }

        private static void NetSegmentRenderMainThread2(NetSegment __instance, RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask, NetInfo info, ref RenderManager.Instance data, NetManager instance, int propIndex2, uint num2, NetNode.Flags flags3, NetNode.Flags flags4, Color color3, Color color4, float startAngle2, float endAngle2, bool invert2, Vector4 objectIndex, Vector4 objectIndex2)
        {
            if ((__instance.m_flags & NetSegment.Flags.Collapsed) != 0)
            {
                for (int k = 0; k < info.m_lanes.Length; k++)
                {
                    if (num2 == 0)
                    {
                        break;
                    }
                    instance.m_lanes.m_buffer[num2].RenderDestroyedInstance(cameraInfo, segmentID, num2, info, info.m_lanes[k], flags3, flags4, color3, color4, startAngle2, endAngle2, invert2, layerMask, objectIndex, objectIndex2, ref data, ref propIndex2);
                    num2 = instance.m_lanes.m_buffer[num2].m_nextLane;
                }
                return;
            }
            for (int l = 0; l < info.m_lanes.Length; l++)
            {
                if (num2 == 0)
                {
                    break;
                }
                instance.m_lanes.m_buffer[num2].RenderInstance(cameraInfo, segmentID, num2, info.m_lanes[l], flags3, flags4, color3, color4, startAngle2, endAngle2, invert2, layerMask, objectIndex, objectIndex2, ref data, ref propIndex2);
                num2 = instance.m_lanes.m_buffer[num2].m_nextLane;
            }
        }
        #endregion
    }
}
