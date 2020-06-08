using ColossalFramework;
using ColossalFramework.PlatformServices;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static CitizenInstance;

namespace ParallelBooster.Patches
{
    public static class CitizenManagerPatch
    {
        public static void Patch(Harmony harmony)
        {
            var originalMethod = AccessTools.Method(typeof(CitizenManager), "EndRenderingImpl");
            var prefixMethod = AccessTools.Method(typeof(CitizenManagerPatch), nameof(EndRenderingImplPrefix));
            Patcher.PatchPrefix(harmony, originalMethod, prefixMethod);
        }

        public static bool EndRenderingImplPrefix(CitizenManager __instance, RenderManager.CameraInfo cameraInfo, ulong[] ___m_renderBuffer)
        {
            float levelOfDetailFactor = RenderManager.LevelOfDetailFactor;
            float near = cameraInfo.m_near;
            float d = Mathf.Min(Mathf.Min(levelOfDetailFactor * 800f, levelOfDetailFactor * 400f + cameraInfo.m_height * 0.5f), cameraInfo.m_far);
            Vector3 lhs = cameraInfo.m_position + cameraInfo.m_directionA * near;
            Vector3 rhs = cameraInfo.m_position + cameraInfo.m_directionB * near;
            Vector3 lhs2 = cameraInfo.m_position + cameraInfo.m_directionC * near;
            Vector3 rhs2 = cameraInfo.m_position + cameraInfo.m_directionD * near;
            Vector3 lhs3 = cameraInfo.m_position + cameraInfo.m_directionA * d;
            Vector3 rhs3 = cameraInfo.m_position + cameraInfo.m_directionB * d;
            Vector3 lhs4 = cameraInfo.m_position + cameraInfo.m_directionC * d;
            Vector3 rhs4 = cameraInfo.m_position + cameraInfo.m_directionD * d;
            Vector3 vector = Vector3.Min(Vector3.Min(Vector3.Min(lhs, rhs), Vector3.Min(lhs2, rhs2)), Vector3.Min(Vector3.Min(lhs3, rhs3), Vector3.Min(lhs4, rhs4)));
            Vector3 vector2 = Vector3.Max(Vector3.Max(Vector3.Max(lhs, rhs), Vector3.Max(lhs2, rhs2)), Vector3.Max(Vector3.Max(lhs3, rhs3), Vector3.Max(lhs4, rhs4)));
            int num = Mathf.Max((int)((vector.x - 1f) / 8f + 1080f), 0);
            int num2 = Mathf.Max((int)((vector.z - 1f) / 8f + 1080f), 0);
            int num3 = Mathf.Min((int)((vector2.x + 1f) / 8f + 1080f), 2159);
            int num4 = Mathf.Min((int)((vector2.z + 1f) / 8f + 1080f), 2159);
            for (int i = num2; i <= num4; i++)
            {
                for (int j = num; j <= num3; j++)
                {
                    ushort num5 = __instance.m_citizenGrid[i * 2160 + j];
                    if (num5 != 0)
                    {
                        ___m_renderBuffer[num5 >> 6] |= (ulong)(1L << (int)num5);
                    }
                }
            }
            int num6 = ___m_renderBuffer.Length;
            for (int k = 0; k < num6; k++)
            {
                ulong num7 = ___m_renderBuffer[k];
                if (num7 == 0)
                {
                    continue;
                }
                for (int l = 0; l < 64; l++)
                {
                    ulong num8 = (ulong)(1L << l);
                    if ((num7 & num8) == 0)
                    {
                        continue;
                    }
                    ushort num9 = (ushort)((k << 6) | l);
                    if (!__instance.m_instances.m_buffer[num9].RenderInstance(cameraInfo, num9))
                    {
                        num7 &= ~num8;
                    }
                    ushort nextGridInstance = __instance.m_instances.m_buffer[num9].m_nextGridInstance;
                    int num10 = 0;
                    while (nextGridInstance != 0)
                    {
                        int num11 = nextGridInstance >> 6;
                        num8 = (ulong)(1L << (int)nextGridInstance);
                        if (num11 == k)
                        {
                            if ((num7 & num8) != 0)
                            {
                                break;
                            }
                            num7 |= num8;
                        }
                        else
                        {
                            ulong num12 = ___m_renderBuffer[num11];
                            if ((num12 & num8) != 0)
                            {
                                break;
                            }
                            ___m_renderBuffer[num11] = (num12 | num8);
                        }
                        if (nextGridInstance > num9)
                        {
                            break;
                        }
                        nextGridInstance = __instance.m_instances.m_buffer[nextGridInstance].m_nextGridInstance;
                        if (++num10 > 65536)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
                ___m_renderBuffer[k] = num7;
            }
#if UseTask
            Patcher.Dispatcher.Add(EndRenderingImplExtracted, cameraInfo);
#else
                        EndRenderingImplExtracted.Invoke(cameraInfo);
#endif
            return false;
        }
        private static Action<object[]> EndRenderingImplExtracted { get; } = new Action<object[]>((args) =>
        {
            RenderManager.CameraInfo cameraInfo = (RenderManager.CameraInfo)args[0];

            int num13 = PrefabCollection<CitizenInfo>.PrefabCount();
            for (int m = 0; m < num13; m++)
            {
                CitizenInfo prefab = PrefabCollection<CitizenInfo>.GetPrefab((uint)m);
                if ((object)prefab != null)
                {
                    prefab.UpdatePrefabInstances();
                    if (prefab.m_lodCount != 0)
                    {
                        CitizenInstance.RenderLod(cameraInfo, prefab);
                    }
                    if (prefab.m_undergroundLodCount != 0)
                    {
                        CitizenInstance.RenderUndergroundLod(cameraInfo, prefab);
                    }
                }
            }
        });
    }
}
