using ColossalFramework;
using ColossalFramework.PlatformServices;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParallelBooster.Patches
{
    public static class BuildingManagerPath
    {
        public static void Patch(Harmony harmony)
        {
            var originalMethod = AccessTools.Method(typeof(BuildingManager), "EndRenderingImpl");
            var prefixMethod = AccessTools.Method(typeof(BuildingManagerPath), nameof(EndRenderingImplPrefix));
            Patcher.PatchPrefix(harmony, originalMethod, prefixMethod);
        }

        public static bool EndRenderingImplPrefix(BuildingManager __instance, RenderManager.CameraInfo cameraInfo)
        {
            FastList<RenderGroup> renderedGroups = Singleton<RenderManager>.instance.m_renderedGroups;
            for (int i = 0; i < renderedGroups.m_size; i++)
            {
                RenderGroup renderGroup = renderedGroups.m_buffer[i];
                int num = renderGroup.m_layersRendered & ~(1 << Singleton<NotificationManager>.instance.m_notificationLayer);
                if (renderGroup.m_instanceMask != 0)
                {
                    num &= ~renderGroup.m_instanceMask;
                    int num2 = renderGroup.m_x * 270 / 45;
                    int num3 = renderGroup.m_z * 270 / 45;
                    int num4 = (renderGroup.m_x + 1) * 270 / 45 - 1;
                    int num5 = (renderGroup.m_z + 1) * 270 / 45 - 1;
                    for (int j = num3; j <= num5; j++)
                    {
                        for (int k = num2; k <= num4; k++)
                        {
                            int num6 = j * 270 + k;
                            ushort num7 = __instance.m_buildingGrid[num6];
                            int num8 = 0;
                            while (num7 != 0)
                            {
                                __instance.m_buildings.m_buffer[num7].RenderInstance(cameraInfo, num7, num | renderGroup.m_instanceMask);
                                num7 = __instance.m_buildings.m_buffer[num7].m_nextGridBuilding;
                                if (++num8 >= 49152)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                    break;
                                }
                            }
                        }
                    }
                }
                if (num == 0)
                {
                    continue;
                }
                int num9 = renderGroup.m_z * 45 + renderGroup.m_x;
                ushort num10 = __instance.m_buildingGrid2[num9];
                int num11 = 0;
                while (num10 != 0)
                {
                    __instance.m_buildings.m_buffer[num10].RenderInstance(cameraInfo, num10, num);
                    num10 = __instance.m_buildings.m_buffer[num10].m_nextGridBuilding2;
                    if (++num11 >= 49152)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
            }
#if UseTask
            Patcher.Dispatcher.Add(EndRenderingImplExtractedMethod, __instance, cameraInfo);
#else
                        EndRenderingImplExtractedMethod.Invoke(__instance, cameraInfo);
#endif

            return false;
        }

        private static Action<object[]> EndRenderingImplExtractedMethod { get; } = new Action<object[]>((args) =>
        {
            BuildingManager __instance = (BuildingManager)args[0];
            RenderManager.CameraInfo cameraInfo = (RenderManager.CameraInfo)args[1];

            int num12 = PrefabCollection<BuildingInfo>.PrefabCount();
            for (int l = 0; l < num12; l++)
            {
                BuildingInfo prefab = PrefabCollection<BuildingInfo>.GetPrefab((uint)l);
                if ((object)prefab == null)
                {
                    continue;
                }
                prefab.UpdatePrefabInstances();
                if (!prefab.m_rendered)
                {
                    continue;
                }
                if (prefab.m_lodCount != 0)
                {
                    Building.RenderLod(cameraInfo, prefab);
                }
                if (prefab.m_subMeshes == null)
                {
                    continue;
                }
                for (int m = 0; m < prefab.m_subMeshes.Length; m++)
                {
                    BuildingInfoSub buildingInfoSub = prefab.m_subMeshes[m].m_subInfo as BuildingInfoSub;
                    if (!buildingInfoSub.m_rendered)
                    {
                        continue;
                    }
                    if (buildingInfoSub.m_lodCount != 0)
                    {
                        Building.RenderLod(cameraInfo, buildingInfoSub);
                    }
                    if (buildingInfoSub.m_subMeshes == null)
                    {
                        continue;
                    }
                    for (int n = 0; n < buildingInfoSub.m_subMeshes.Length; n++)
                    {
                        BuildingInfoSub buildingInfoSub2 = buildingInfoSub.m_subMeshes[n].m_subInfo as BuildingInfoSub;
                        if (buildingInfoSub2.m_lodCount != 0)
                        {
                            Building.RenderLod(cameraInfo, buildingInfoSub2);
                        }
                    }
                }
            }
            if (!(__instance.m_common != null) || __instance.m_common.m_subInfos == null)
            {
                return;
            }
            for (int num13 = 0; num13 < __instance.m_common.m_subInfos.m_size; num13++)
            {
                BuildingInfoBase buildingInfoBase = __instance.m_common.m_subInfos.m_buffer[num13];
                if (buildingInfoBase.m_lodCount != 0)
                {
                    Building.RenderLod(cameraInfo, buildingInfoBase);
                }
            }
        });
    }
}
