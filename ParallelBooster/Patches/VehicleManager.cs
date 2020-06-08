using ColossalFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ParallelBooster.Patches
{
    public static class VehicleManagerPatch
    {
        public static void Patch(Harmony harmony)
        {
            var originalMethod = AccessTools.Method(typeof(VehicleManager), "EndRenderingImpl");
            var prefixMethod = AccessTools.Method(typeof(VehicleManagerPatch), nameof(EndRenderingImplPrefix));
            Patcher.PatchPrefix(harmony, originalMethod, prefixMethod);
        }

        public static bool EndRenderingImplPrefix(VehicleManager __instance, RenderManager.CameraInfo cameraInfo, ulong[] ___m_renderBuffer, ulong[] ___m_renderBuffer2)
        {
            float levelOfDetailFactor = RenderManager.LevelOfDetailFactor;
            float near = cameraInfo.m_near;
            float d = Mathf.Min(levelOfDetailFactor * 5000f, Mathf.Min(levelOfDetailFactor * 2000f + cameraInfo.m_height * 0.6f, cameraInfo.m_far));
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
            int num = Mathf.Max((int)((vector.x - 10f) / 32f + 270f), 0);
            int num2 = Mathf.Max((int)((vector.z - 10f) / 32f + 270f), 0);
            int num3 = Mathf.Min((int)((vector2.x + 10f) / 32f + 270f), 539);
            int num4 = Mathf.Min((int)((vector2.z + 10f) / 32f + 270f), 539);
            for (int i = num2; i <= num4; i++)
            {
                for (int j = num; j <= num3; j++)
                {
                    ushort num5 = __instance.m_vehicleGrid[i * 540 + j];
                    if (num5 != 0)
                    {
                        ___m_renderBuffer[num5 >> 6] |= (ulong)(1L << (int)num5);
                    }
                }
            }
            float near2 = cameraInfo.m_near;
            float d2 = Mathf.Min(2000f, cameraInfo.m_far);
            Vector3 lhs5 = cameraInfo.m_position + cameraInfo.m_directionA * near2;
            Vector3 rhs5 = cameraInfo.m_position + cameraInfo.m_directionB * near2;
            Vector3 lhs6 = cameraInfo.m_position + cameraInfo.m_directionC * near2;
            Vector3 rhs6 = cameraInfo.m_position + cameraInfo.m_directionD * near2;
            Vector3 lhs7 = cameraInfo.m_position + cameraInfo.m_directionA * d2;
            Vector3 rhs7 = cameraInfo.m_position + cameraInfo.m_directionB * d2;
            Vector3 lhs8 = cameraInfo.m_position + cameraInfo.m_directionC * d2;
            Vector3 rhs8 = cameraInfo.m_position + cameraInfo.m_directionD * d2;
            Vector3 vector3 = Vector3.Min(Vector3.Min(Vector3.Min(lhs5, rhs5), Vector3.Min(lhs6, rhs6)), Vector3.Min(Vector3.Min(lhs7, rhs7), Vector3.Min(lhs8, rhs8)));
            Vector3 vector4 = Vector3.Max(Vector3.Max(Vector3.Max(lhs5, rhs5), Vector3.Max(lhs6, rhs6)), Vector3.Max(Vector3.Max(lhs7, rhs7), Vector3.Max(lhs8, rhs8)));
            int num6 = Mathf.Max((int)((vector3.x - 10f) / 32f + 270f), 0);
            int num7 = Mathf.Max((int)((vector3.z - 10f) / 32f + 270f), 0);
            int num8 = Mathf.Min((int)((vector4.x + 10f) / 32f + 270f), 539);
            int num9 = Mathf.Min((int)((vector4.z + 10f) / 32f + 270f), 539);
            for (int k = num7; k <= num9; k++)
            {
                for (int l = num6; l <= num8; l++)
                {
                    ushort num10 = __instance.m_parkedGrid[k * 540 + l];
                    if (num10 != 0)
                    {
                        ___m_renderBuffer2[num10 >> 6] |= (ulong)(1L << (int)num10);
                    }
                }
            }
            float near3 = cameraInfo.m_near;
            float num11 = Mathf.Min(10000f, cameraInfo.m_far);
            Vector3 lhs9 = cameraInfo.m_position + cameraInfo.m_directionA * near3;
            Vector3 rhs9 = cameraInfo.m_position + cameraInfo.m_directionB * near3;
            Vector3 lhs10 = cameraInfo.m_position + cameraInfo.m_directionC * near3;
            Vector3 rhs10 = cameraInfo.m_position + cameraInfo.m_directionD * near3;
            Vector3 lhs11 = cameraInfo.m_position + cameraInfo.m_directionA * num11;
            Vector3 rhs11 = cameraInfo.m_position + cameraInfo.m_directionB * num11;
            Vector3 lhs12 = cameraInfo.m_position + cameraInfo.m_directionC * num11;
            Vector3 rhs12 = cameraInfo.m_position + cameraInfo.m_directionD * num11;
            Vector3 vector5 = Vector3.Min(Vector3.Min(Vector3.Min(lhs9, rhs9), Vector3.Min(lhs10, rhs10)), Vector3.Min(Vector3.Min(lhs11, rhs11), Vector3.Min(lhs12, rhs12)));
            Vector3 vector6 = Vector3.Max(Vector3.Max(Vector3.Max(lhs9, rhs9), Vector3.Max(lhs10, rhs10)), Vector3.Max(Vector3.Max(lhs11, rhs11), Vector3.Max(lhs12, rhs12)));
            if (cameraInfo.m_shadowOffset.x < 0f)
            {
                vector6.x = Mathf.Min(cameraInfo.m_position.x + num11, vector6.x - cameraInfo.m_shadowOffset.x);
            }
            else
            {
                vector5.x = Mathf.Max(cameraInfo.m_position.x - num11, vector5.x - cameraInfo.m_shadowOffset.x);
            }
            if (cameraInfo.m_shadowOffset.z < 0f)
            {
                vector6.z = Mathf.Min(cameraInfo.m_position.z + num11, vector6.z - cameraInfo.m_shadowOffset.z);
            }
            else
            {
                vector5.z = Mathf.Max(cameraInfo.m_position.z - num11, vector5.z - cameraInfo.m_shadowOffset.z);
            }
            int num12 = Mathf.Max((int)((vector5.x - 50f) / 320f + 27f), 0);
            int num13 = Mathf.Max((int)((vector5.z - 50f) / 320f + 27f), 0);
            int num14 = Mathf.Min((int)((vector6.x + 50f) / 320f + 27f), 53);
            int num15 = Mathf.Min((int)((vector6.z + 50f) / 320f + 27f), 53);
            for (int m = num13; m <= num15; m++)
            {
                for (int n = num12; n <= num14; n++)
                {
                    ushort num16 = __instance.m_vehicleGrid2[m * 54 + n];
                    if (num16 != 0)
                    {
                        ___m_renderBuffer[num16 >> 6] |= (ulong)(1L << (int)num16);
                    }
                }
            }
            int num17 = ___m_renderBuffer.Length;
            for (int num18 = 0; num18 < num17; num18++)
            {
                ulong num19 = ___m_renderBuffer[num18];
                if (num19 == 0)
                {
                    continue;
                }
                for (int num20 = 0; num20 < 64; num20++)
                {
                    ulong num21 = (ulong)(1L << num20);
                    if ((num19 & num21) == 0)
                    {
                        continue;
                    }
                    ushort num22 = (ushort)((num18 << 6) | num20);
                    if (!__instance.m_vehicles.m_buffer[num22].RenderInstance(cameraInfo, num22))
                    {
                        num19 &= ~num21;
                    }
                    ushort nextGridVehicle = __instance.m_vehicles.m_buffer[num22].m_nextGridVehicle;
                    int num23 = 0;
                    while (nextGridVehicle != 0)
                    {
                        int num24 = nextGridVehicle >> 6;
                        num21 = (ulong)(1L << (int)nextGridVehicle);
                        if (num24 == num18)
                        {
                            if ((num19 & num21) != 0)
                            {
                                break;
                            }
                            num19 |= num21;
                        }
                        else
                        {
                            ulong num25 = ___m_renderBuffer[num24];
                            if ((num25 & num21) != 0)
                            {
                                break;
                            }
                            ___m_renderBuffer[num24] = (num25 | num21);
                        }
                        if (nextGridVehicle > num22)
                        {
                            break;
                        }
                        nextGridVehicle = __instance.m_vehicles.m_buffer[nextGridVehicle].m_nextGridVehicle;
                        if (++num23 > 16384)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
                ___m_renderBuffer[num18] = num19;
            }
#if UseTask
            Patcher.Dispatcher.Add(EndRenderingImplExtracted, __instance, cameraInfo, ___m_renderBuffer2);
#else
            EndRenderingImplExtracted.Invoke(__instance, cameraInfo, ___m_renderBuffer2);
#endif
            return false;
        }
        private static Action<object[]> EndRenderingImplExtracted { get; } = new Action<object[]>((args) =>
        {
            VehicleManager __instance = (VehicleManager)args[0];
            RenderManager.CameraInfo cameraInfo = (RenderManager.CameraInfo)args[1];
            ulong[] ___m_renderBuffer2 = (ulong[])args[2];

            int num26 = ___m_renderBuffer2.Length;
            for (int num27 = 0; num27 < num26; num27++)
            {
                ulong num28 = ___m_renderBuffer2[num27];
                if (num28 == 0)
                {
                    continue;
                }
                for (int num29 = 0; num29 < 64; num29++)
                {
                    ulong num30 = (ulong)(1L << num29);
                    if ((num28 & num30) == 0)
                    {
                        continue;
                    }
                    ushort num31 = (ushort)((num27 << 6) | num29);
                    if (!__instance.m_parkedVehicles.m_buffer[num31].RenderInstance(cameraInfo, num31))
                    {
                        num28 &= ~num30;
                    }
                    ushort nextGridParked = __instance.m_parkedVehicles.m_buffer[num31].m_nextGridParked;
                    int num32 = 0;
                    while (nextGridParked != 0)
                    {
                        int num33 = nextGridParked >> 6;
                        num30 = (ulong)(1L << (int)nextGridParked);
                        if (num33 == num27)
                        {
                            if ((num28 & num30) != 0)
                            {
                                break;
                            }
                            num28 |= num30;
                        }
                        else
                        {
                            ulong num34 = ___m_renderBuffer2[num33];
                            if ((num34 & num30) != 0)
                            {
                                break;
                            }
                            ___m_renderBuffer2[num33] = (num34 | num30);
                        }
                        if (nextGridParked > num31)
                        {
                            break;
                        }
                        nextGridParked = __instance.m_parkedVehicles.m_buffer[nextGridParked].m_nextGridParked;
                        if (++num32 > 32768)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
                ___m_renderBuffer2[num27] = num28;
            }


            int num35 = PrefabCollection<VehicleInfo>.PrefabCount();
            for (int num36 = 0; num36 < num35; num36++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab((uint)num36);
                if ((object)prefab == null)
                {
                    continue;
                }
                if (prefab.m_lodCount != 0)
                {
                    Vehicle.RenderLod(cameraInfo, prefab);
                }
                if (prefab.m_undergroundLodCount != 0)
                {
                    Vehicle.RenderUndergroundLod(cameraInfo, prefab);
                }
                if (prefab.m_subMeshes == null)
                {
                    continue;
                }
                for (int num37 = 0; num37 < prefab.m_subMeshes.Length; num37++)
                {
                    VehicleInfoBase subInfo = prefab.m_subMeshes[num37].m_subInfo;
                    if (subInfo != null)
                    {
                        if (subInfo.m_lodCount != 0)
                        {
                            Vehicle.RenderLod(cameraInfo, subInfo);
                        }
                        if (subInfo.m_undergroundLodCount != 0)
                        {
                            Vehicle.RenderUndergroundLod(cameraInfo, subInfo);
                        }
                    }
                }
            }
        });
    }
}
