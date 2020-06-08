using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ParallelBooster.Patches
{
    public static class BuildingAIPatch
    {
        public static Action<object[]> RenderMeshesMethod { get; } = new Action<object[]>((args) =>
        {
            Building data = (Building)args[3];
            RenderManager.Instance instance = (RenderManager.Instance)args[5];
            ((BuildingAI)args[0]).RenderMeshes((RenderManager.CameraInfo)args[1], (ushort)args[2], ref data, (int)args[4], ref instance);
        });
        public static Action<object[]> RenderMeshMethod { get; } = new Action<object[]>((args) =>
        {
            RenderManager.Instance instance = (RenderManager.Instance)args[4];
            BuildingAI.RenderMesh((RenderManager.CameraInfo)args[0], (BuildingInfo)args[1], (BuildingInfoBase)args[2], (Matrix4x4)args[3], ref instance);
        });
        public static Action<object[]> RenderCollapseEffectMethod { get; } = new Action<object[]>((args) =>
        {
            Building data = (Building)args[3];
            ((CommonBuildingAI)args[0]).RenderCollapseEffect((RenderManager.CameraInfo)args[1], (ushort)args[2], ref data, (float)args[4]);
        });
        public static Action<object[]> RenderGarbageBinsMethod { get; } = new Action<object[]>((args) =>
        {
            Building data = (Building)args[3];
            RenderManager.Instance instance = (RenderManager.Instance)args[5];
            ((CommonBuildingAI)args[0]).RenderGarbageBins((RenderManager.CameraInfo)args[1], (ushort)args[2], ref data, (int)args[4], ref instance);
        });
        public static Action<object[]> RenderFireEffectMethod { get; } = new Action<object[]>((args) =>
        {
            Building data = (Building)args[3];
            RenderManager.Instance instance = (RenderManager.Instance)args[4];
            ((CommonBuildingAI)args[0]).RenderFireEffect((RenderManager.CameraInfo)args[1], (ushort)args[2], ref data, instance.m_dataVector0.x);
            RenderFireEffectPropsReverse((CommonBuildingAI)args[0], (RenderManager.CameraInfo)args[1], (ushort)args[2], ref data, ref instance, (float)(int)data.m_fireIntensity * 0.003921569f, instance.m_dataVector0.x, true, true);
        });

        public static void Patch(Harmony harmony)
        {
            var originalRenderInstanceBuildingAIMethod = AccessTools.Method(typeof(BuildingAI), nameof(BuildingAI.RenderInstance));
            var prefixRenderInstanceBuildingAIMethod = AccessTools.Method(typeof(BuildingAIPatch), nameof(RenderInstanceBuildingAIPrefix));
            Patcher.PatchPrefix(harmony, originalRenderInstanceBuildingAIMethod, prefixRenderInstanceBuildingAIMethod);

            var originalRenderInstanceCableCarPylonAIMethod = AccessTools.Method(typeof(CableCarPylonAI), nameof(CableCarPylonAI.RenderInstance));
            var prefixRenderInstanceCableCarPylonAIMethod = AccessTools.Method(typeof(BuildingAIPatch), nameof(RenderInstanceCableCarPylonAIPrefix));
            Patcher.PatchPrefix(harmony, originalRenderInstanceCableCarPylonAIMethod, prefixRenderInstanceCableCarPylonAIMethod);

            var originalRenderInstancePowerPoleAIMethod = AccessTools.Method(typeof(PowerPoleAI), nameof(PowerPoleAI.RenderInstance));
            var prefixRenderInstancePowerPoleAIMethod = AccessTools.Method(typeof(BuildingAIPatch), nameof(RenderInstancePowerPoleAIPrefix));
            Patcher.PatchPrefix(harmony, originalRenderInstancePowerPoleAIMethod, prefixRenderInstancePowerPoleAIMethod);

            var originalRenderInstanceCommonBuildingAIMethod = AccessTools.Method(typeof(CommonBuildingAI), nameof(CommonBuildingAI.RenderInstance));
            var prefixRenderInstanceCommonBuildingAIMethod = AccessTools.Method(typeof(BuildingAIPatch), nameof(RenderInstanceCommonBuildingAIPrefix));
            Patcher.PatchPrefix(harmony, originalRenderInstanceCommonBuildingAIMethod, prefixRenderInstanceCommonBuildingAIMethod);

            var originalRenderDestroyedPropsMethod = AccessTools.Method(typeof(BuildingAI), "RenderDestroyedProps");
            var prefixRenderDestroyedPropsMethod = AccessTools.Method(typeof(BuildingAIPatch), nameof(RenderDestroyedPropsPrefix));
            Patcher.PatchPrefix(harmony, originalRenderDestroyedPropsMethod, prefixRenderDestroyedPropsMethod);

            var originalRenderPropsMethod = AccessTools.Method(typeof(BuildingAI), "RenderProps", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(Building).MakeByRefType(), typeof(int), typeof(RenderManager.Instance).MakeByRefType(), typeof(bool), typeof(bool), typeof(bool) });
            var prefixRenderPropsMethod = AccessTools.Method(typeof(BuildingAIPatch), nameof(RenderPropsPrefix));
            Patcher.PatchPrefix(harmony, originalRenderPropsMethod, prefixRenderPropsMethod);

            var originalGetCollapseTimeMethod = AccessTools.Method(typeof(CommonBuildingAI), "GetCollapseTime");
            var reverseGetCollapseTimeMethod = AccessTools.Method(typeof(BuildingAIPatch), nameof(GetCollapseTimeReverse));
            Patcher.PatchReverse(harmony, originalGetCollapseTimeMethod, reverseGetCollapseTimeMethod);

            var originalGetConstructionTimeMethod = AccessTools.Method(typeof(CommonBuildingAI), "GetConstructionTime");
            var reverseGetConstructionTimeMethod = AccessTools.Method(typeof(BuildingAIPatch), nameof(GetConstructionTimeReverse));
            Patcher.PatchReverse(harmony, originalGetConstructionTimeMethod, reverseGetConstructionTimeMethod);

            var originalRenderFireEffectPropsMethod = AccessTools.Method(typeof(CommonBuildingAI), "RenderFireEffectProps");
            var reverseRenderFireEffectPropsMethod = AccessTools.Method(typeof(BuildingAIPatch), nameof(RenderFireEffectPropsReverse));
            Patcher.PatchReverse(harmony, originalRenderFireEffectPropsMethod, reverseRenderFireEffectPropsMethod);

            var originalGetPropRenderIDMethod = AccessTools.Method(typeof(BuildingAI), "GetPropRenderID");
            var reverseGetPropRenderIDMethod = AccessTools.Method(typeof(BuildingAIPatch), nameof(GetPropRenderIDReverse));
            Patcher.PatchReverse(harmony, originalGetPropRenderIDMethod, reverseGetPropRenderIDMethod);
        }

        public static bool RenderInstanceBuildingAIPrefix(BuildingAI __instance, RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance)
        {
#if UseTask
            Patcher.Dispatcher.Add(RenderMeshesMethod, __instance, cameraInfo, buildingID, data, layerMask, instance);
#else
            __instance.RenderMeshes(cameraInfo, buildingID, ref data, layerMask, ref instance);
#endif
            __instance.RenderProps(cameraInfo, buildingID, ref data, layerMask, ref instance, renderFixed: true, renderNonfixed: true);

            return false;
        }

        public static bool RenderInstanceCableCarPylonAIPrefix(CableCarPylonAI __instance, RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance)
        {
#if UseTask
            Patcher.Dispatcher.Add(RenderMeshesMethod,__instance, cameraInfo, buildingID, data, layerMask, instance );
#else
            __instance.RenderMeshes(cameraInfo, buildingID, ref data, layerMask, ref instance);
#endif
            __instance.RenderProps(cameraInfo, buildingID, ref data, layerMask, ref instance, (data.m_flags & Building.Flags.Collapsed) == 0, renderNonfixed: true);

            return false;
        }

        public static bool RenderInstancePowerPoleAIPrefix(PowerPoleAI __instance, RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance)
        {
#if UseTask
            Patcher.Dispatcher.Add(RenderMeshesMethod, __instance, cameraInfo, buildingID, data, layerMask, instance );
#else
            __instance.RenderMeshes(cameraInfo, buildingID, ref data, layerMask, ref instance);
#endif
            __instance.RenderProps(cameraInfo, buildingID, ref data, layerMask, ref instance, (data.m_flags & Building.Flags.Collapsed) == 0, renderNonfixed: true);

            return false;
        }

        public static bool RenderInstanceCommonBuildingAIPrefix(CommonBuildingAI __instance, RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance)
        {
            if ((data.m_flags & (Building.Flags.Completed | Building.Flags.Collapsed)) != Building.Flags.Completed)
            {
                if ((data.m_flags & Building.Flags.Collapsed) != 0)
                {
                    uint num = (uint)(buildingID << 8) / 49152u;
                    uint num2 = Singleton<SimulationManager>.instance.m_referenceFrameIndex - num;
                    float t = ((float)(double)(num2 & 0xFF) + Singleton<SimulationManager>.instance.m_referenceTimer) * 0.00390625f;
                    Building.Frame frameData = data.GetFrameData(num2 - 512);
                    Building.Frame frameData2 = data.GetFrameData(num2 - 256);
                    instance.m_dataVector0.x = Mathf.Max(0f, (Mathf.Lerp((int)frameData.m_fireDamage, (int)frameData2.m_fireDamage, t) - 127f) * 0.0078125f);
                    instance.m_dataVector0.y = 0f;
                    instance.m_dataVector0.z = (((data.m_flags & Building.Flags.Abandoned) == 0) ? 0f : 1f);
                    float num3 = 0f;
                    Randomizer randomizer = new Randomizer(buildingID);
                    int num4 = randomizer.Int32(4u);
                    if (frameData.m_constructState != 0)
                    {
                        float y = __instance.m_info.m_size.y;
                        float num5 = (float)GetCollapseTimeReverse(__instance);
                        num5 /= Mathf.Max(1f, num5 - 6f);
                        float num6 = Mathf.Lerp((int)frameData.m_constructState, (int)frameData2.m_constructState, t) * 0.003921569f;
                        num3 = 1f - num5 * (1f - num6);
                        instance.m_dataVector0.y = Mathf.Max(instance.m_dataVector0.y, y * num3);
                        if (instance.m_dataVector0.y > 0.1f)
                        {
                            __instance.RenderProps(cameraInfo, buildingID, ref data, layerMask, ref instance, renderFixed: false, renderNonfixed: true);
                            float angle = data.m_angle;
                            Vector3 position = instance.m_position;
                            Quaternion rotation = instance.m_rotation;
                            Matrix4x4 dataMatrix = instance.m_dataMatrix1;
                            float f = (float)randomizer.Int32(1000u) * ((float)Math.PI / 500f);
                            float num7 = randomizer.Int32(10, 45);
                            float angle2 = (1f - num3) * (1f - num3) * num7;
                            Vector3 axis = new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f));
                            float num8 = (y - instance.m_dataVector0.y) * (y - instance.m_dataVector0.y) / y;
                            instance.m_position.x += axis.z * num8 * num7 * 0.01f;
                            instance.m_position.z -= axis.x * num8 * num7 * 0.01f;
                            instance.m_position.y -= num8;
                            instance.m_rotation = Quaternion.AngleAxis(angle2, axis) * Quaternion.AngleAxis(angle * 57.29578f, Vector3.down);
                            instance.m_dataMatrix1.SetTRS(instance.m_position, instance.m_rotation, Vector3.one);
                            instance.m_dataVector0.y = y;
#if UseTask
                            Patcher.Dispatcher.Add(RenderMeshesMethod, __instance, cameraInfo, buildingID, data, layerMask, instance );
#else
							__instance.RenderMeshes(cameraInfo, buildingID, ref data, layerMask, ref instance);
#endif
                            __instance.RenderProps(cameraInfo, buildingID, ref data, layerMask, ref instance, renderFixed: true, renderNonfixed: false);
                            instance.m_dataVector0.y = y - num8;
                            instance.m_position = position;
                            instance.m_rotation = rotation;
                            instance.m_dataMatrix1 = dataMatrix;
                        }
                        else if ((data.m_flags & Building.Flags.Demolishing) == 0)
                        {
                            RenderDestroyedPropsPrefix(__instance, cameraInfo, buildingID, ref data, layerMask, ref instance, renderFixed: false, renderNonfixed: true);
                        }
                    }
                    else if ((data.m_flags & Building.Flags.Demolishing) == 0)
                    {
                        RenderDestroyedPropsPrefix(__instance, cameraInfo, buildingID, ref data, layerMask, ref instance, renderFixed: false, renderNonfixed: true);
                    }
                    float num9 = Mathf.Clamp01(1f - num3);
                    instance.m_dataVector0.x = 0f - instance.m_dataVector0.x;
                    if ((data.m_flags & Building.Flags.Demolishing) == 0 && num9 > 0.01f)
                    {
                        BuildingInfoBase collapsedInfo = __instance.m_info.m_collapsedInfo;
                        if (__instance.m_info.m_mesh != null && collapsedInfo != null)
                        {
                            if (((1 << num4) & __instance.m_info.m_collapsedRotations) == 0)
                            {
                                num4 = ((num4 + 1) & 3);
                            }
                            Vector3 min = __instance.m_info.m_generatedInfo.m_min;
                            Vector3 max = __instance.m_info.m_generatedInfo.m_max;
                            float num10 = (float)data.Width * 4f;
                            float num11 = (float)data.Length * 4f;
                            float num12 = Building.CalculateLocalMeshOffset(__instance.m_info, data.Length);
                            min = Vector3.Max(min - new Vector3(4f, 0f, 4f + num12), new Vector3(0f - num10, 0f, 0f - num11));
                            max = Vector3.Min(max + new Vector3(4f, 0f, 4f - num12), new Vector3(num10, 0f, num11));
                            Vector3 vector = (min + max) * 0.5f;
                            Vector3 vector2 = max - min;
                            float x = (((num4 & 1) != 0) ? vector2.z : vector2.x) * num9 / Mathf.Max(1f, collapsedInfo.m_generatedInfo.m_size.x);
                            float z = (((num4 & 1) != 0) ? vector2.x : vector2.z) * num9 / Mathf.Max(1f, collapsedInfo.m_generatedInfo.m_size.z);
                            Quaternion q = Quaternion.AngleAxis((float)num4 * 90f, Vector3.down);
                            instance.m_dataVector0.y = Mathf.Max(instance.m_dataVector0.y, collapsedInfo.m_generatedInfo.m_size.y);
                            Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(vector.x, 0f, vector.z + num12), q, new Vector3(x, num9, z));
                            collapsedInfo.m_rendered = true;
#if UseTask
                            Patcher.Dispatcher.Add(RenderMeshMethod, cameraInfo, __instance.m_info, collapsedInfo, matrix, instance);
#else
							BuildingAI.RenderMesh(cameraInfo, __instance.m_info, collapsedInfo, matrix, ref instance);
#endif
                        }
                    }
                    if (Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.None)
                    {
#if UseTask
                        Patcher.Dispatcher.Add(RenderCollapseEffectMethod, __instance, cameraInfo, buildingID, data, num3);
#else
							__instance.RenderCollapseEffect(cameraInfo, buildingID, ref data, num3);
#endif
                    }
                    return false;
                }
                uint num13 = (uint)(buildingID << 8) / 49152u;
                uint num14 = Singleton<SimulationManager>.instance.m_referenceFrameIndex - num13;
                float t2 = ((float)(double)(num14 & 0xFF) + Singleton<SimulationManager>.instance.m_referenceTimer) * 0.00390625f;
                Building.Frame frameData3 = data.GetFrameData(num14 - 512);
                Building.Frame frameData4 = data.GetFrameData(num14 - 256);
                float num15 = 0f;
                BuildingInfo buildingInfo;
                BuildingInfo buildingInfo2;
                if ((data.m_flags & Building.Flags.Upgrading) != 0)
                {
                    BuildingInfo upgradeInfo = __instance.GetUpgradeInfo(buildingID, ref data);
                    if (upgradeInfo != null)
                    {
                        buildingInfo = __instance.m_info;
                        buildingInfo2 = upgradeInfo;
                    }
                    else
                    {
                        buildingInfo = null;
                        buildingInfo2 = __instance.m_info;
                    }
                }
                else
                {
                    buildingInfo = null;
                    buildingInfo2 = __instance.m_info;
                }
                float num16 = buildingInfo2.m_size.y;
                if (buildingInfo != null)
                {
                    num16 = Mathf.Max(num16, buildingInfo.m_size.y);
                }
                float num17 = (float)GetConstructionTimeReverse(__instance);
                num17 /= Mathf.Max(1f, num17 - 6f);
                float num18 = Mathf.Max(0.5f, num16 / 60f);
                float num19 = Mathf.Ceil(num16 / num18 / 6f) * 6f;
                float num20 = (num19 * 2f + 6f) * num17 * Mathf.Lerp((int)frameData3.m_constructState, (int)frameData4.m_constructState, t2) * 0.003921569f;
                float num21 = (num20 - 6f) * num18;
                if (num21 >= buildingInfo2.m_size.y && instance.m_dataInt0 != buildingInfo2.m_prefabDataIndex)
                {
                    BuildingAI.RefreshInstance(buildingInfo2, cameraInfo, buildingID, ref data, layerMask, ref instance, requireHeightMap: false);
                }
                float num22 = (!(num20 > num19)) ? num20 : Mathf.Min(num19, num19 * 2f + 6f - num20);
                if (frameData4.m_productionState < frameData3.m_productionState)
                {
                    instance.m_dataVector3.w = Mathf.Lerp((int)frameData3.m_productionState, (float)(int)frameData4.m_productionState + 256f, t2) * 0.00390625f;
                    if (instance.m_dataVector3.w >= 1f)
                    {
                        instance.m_dataVector3.w -= 1f;
                    }
                }
                else
                {
                    instance.m_dataVector3.w = Mathf.Lerp((int)frameData3.m_productionState, (int)frameData4.m_productionState, t2) * 0.00390625f;
                }
                if (buildingInfo != null)
                {
                    instance.m_position = Building.CalculateMeshPosition(buildingInfo, data.m_position, data.m_angle, data.Length);
                    instance.m_rotation = Quaternion.AngleAxis(data.m_angle * 57.29578f, Vector3.down);
                    instance.m_dataMatrix1.SetTRS(instance.m_position, instance.m_rotation, Vector3.one);
                    instance.m_dataColor0 = buildingInfo.m_buildingAI.GetColor(buildingID, ref data, Singleton<InfoManager>.instance.CurrentMode);
                    float num23 = num20 * num18;
                    float num24 = (!(num23 > buildingInfo.m_size.y)) ? buildingInfo.m_size.y : (buildingInfo.m_size.y * 2f - num23);
                    if (num24 > 0f)
                    {
                        instance.m_dataVector0.y = 0f - num24;
                        instance.m_dataVector0.x = num22 * num18;
#if UseTask
                        Patcher.Dispatcher.Add(RenderMeshesMethod, buildingInfo.m_buildingAI, cameraInfo, buildingID, data, layerMask, instance);
#else
							buildingInfo.m_buildingAI.RenderMeshes(cameraInfo, buildingID, ref data, layerMask, ref instance);
#endif
                        num15 = Mathf.Max(num15, instance.m_dataVector0.y);
                        if (instance.m_dataVector0.y >= buildingInfo.m_size.y && instance.m_dataInt0 == buildingInfo.m_prefabDataIndex)
                        {
                            layerMask &= ~(1 << Singleton<TreeManager>.instance.m_treeLayer);
                            buildingInfo.m_buildingAI.RenderProps(cameraInfo, buildingID, ref data, layerMask, ref instance, renderFixed: true, renderNonfixed: true);
                        }
                    }
                }
                float num25 = data.m_angle;
                int length = data.Length;
                int num26 = 0;
                if (buildingInfo != null && buildingInfo2 != null)
                {
                    if (buildingInfo.m_zoningMode == BuildingInfo.ZoningMode.CornerLeft && buildingInfo2.m_zoningMode == BuildingInfo.ZoningMode.CornerRight)
                    {
                        num25 -= (float)Math.PI / 2f;
                        num26 = -1;
                        length = data.Width;
                    }
                    else if (buildingInfo.m_zoningMode == BuildingInfo.ZoningMode.CornerRight && buildingInfo2.m_zoningMode == BuildingInfo.ZoningMode.CornerLeft)
                    {
                        num25 += (float)Math.PI / 2f;
                        num26 = 1;
                        length = data.Width;
                    }
                }
                instance.m_position = Building.CalculateMeshPosition(buildingInfo2, data.m_position, num25, length);
                instance.m_rotation = Quaternion.AngleAxis(num25 * 57.29578f, Vector3.down);
                instance.m_dataMatrix1.SetTRS(instance.m_position, instance.m_rotation, Vector3.one);
                instance.m_dataColor0 = buildingInfo2.m_buildingAI.GetColor(buildingID, ref data, Singleton<InfoManager>.instance.CurrentMode);
                if (num21 > 0f)
                {
                    instance.m_dataVector0.y = 0f - num21;
                    instance.m_dataVector0.x = num22 * num18;
#if UseTask
                    Patcher.Dispatcher.Add(RenderMeshesMethod, buildingInfo2.m_buildingAI, cameraInfo, buildingID, data, layerMask, instance);
#else
				buildingInfo2.m_buildingAI.RenderMeshes(cameraInfo, buildingID, ref data, layerMask, ref instance);
#endif
                    num15 = Mathf.Max(num15, instance.m_dataVector0.y);
                    if (num21 >= buildingInfo2.m_size.y && instance.m_dataInt0 == buildingInfo2.m_prefabDataIndex)
                    {
                        layerMask &= ~(1 << Singleton<TreeManager>.instance.m_treeLayer);
                        buildingInfo2.m_buildingAI.RenderProps(cameraInfo, buildingID, ref data, layerMask, ref instance, renderFixed: true, renderNonfixed: true);
                    }
                }
                BuildingManager instance2 = Singleton<BuildingManager>.instance;
                if (instance2.m_common != null)
                {
                    BuildingInfoBase construction = instance2.m_common.m_construction;
                    Vector3 vector3 = buildingInfo2.m_generatedInfo.m_max;
                    Vector3 vector4 = buildingInfo2.m_generatedInfo.m_min;
                    if (buildingInfo != null)
                    {
                        Vector3 zero = Vector3.zero;
                        zero.z = 0f - Building.CalculateLocalMeshOffset(buildingInfo2, length);
                        switch (num26)
                        {
                            case -1:
                                {
                                    zero.x -= Building.CalculateLocalMeshOffset(buildingInfo, data.Length);
                                    Vector3 max3 = buildingInfo.m_generatedInfo.m_max;
                                    Vector3 min3 = buildingInfo.m_generatedInfo.m_min;
                                    vector3 = Vector3.Max(vector3, new Vector3(max3.z, max3.y, 0f - min3.x) - zero);
                                    vector4 = Vector3.Min(vector4, new Vector3(min3.z, min3.y, 0f - max3.x) - zero);
                                    break;
                                }
                            case 1:
                                {
                                    zero.x += Building.CalculateLocalMeshOffset(buildingInfo, data.Length);
                                    Vector3 max2 = buildingInfo.m_generatedInfo.m_max;
                                    Vector3 min2 = buildingInfo.m_generatedInfo.m_min;
                                    vector3 = Vector3.Max(vector3, new Vector3(max2.z, max2.y, max2.x) - zero);
                                    vector4 = Vector3.Min(vector4, new Vector3(min2.z, min2.y, min2.x) - zero);
                                    break;
                                }
                            default:
                                zero.z += Building.CalculateLocalMeshOffset(buildingInfo, data.Length);
                                vector3 = Vector3.Max(vector3, buildingInfo.m_generatedInfo.m_max - zero);
                                vector4 = Vector3.Min(vector4, buildingInfo.m_generatedInfo.m_min - zero);
                                break;
                        }
                    }
                    Vector3 vector5 = vector3 - vector4;
                    float x2 = (vector5.x + 1f) / Mathf.Max(1f, construction.m_generatedInfo.m_size.x);
                    float z2 = (vector5.z + 1f) / Mathf.Max(1f, construction.m_generatedInfo.m_size.z);
                    Matrix4x4 matrix2 = Matrix4x4.TRS(new Vector3((vector3.x + vector4.x) * 0.5f, 0f, (vector3.z + vector4.z) * 0.5f), s: new Vector3(x2, num18, z2), q: Quaternion.identity);
                    if (num22 > 0f)
                    {
                        instance.m_dataVector0.y = num22;
                        construction.m_rendered = true;
#if UseTask
                        Patcher.Dispatcher.Add(RenderMeshMethod, cameraInfo, buildingInfo2, construction, matrix2, instance );
#else
						BuildingAI.RenderMesh(cameraInfo, buildingInfo2, construction, matrix2, ref instance);
#endif
                        num15 = Mathf.Max(num15, instance.m_dataVector0.y);
                    }
                }
                instance.m_dataVector0.y = num15;
                return false;
            }
            if (!__instance.m_hideGarbageBins)
            {
#if UseTask
                Patcher.Dispatcher.Add(RenderGarbageBinsMethod,  __instance, cameraInfo, buildingID, data, layerMask, instance);
#else
				__instance.RenderGarbageBins(cameraInfo, buildingID, ref data, layerMask, ref instance);
#endif
            }
            uint num27 = (uint)(buildingID << 8) / 49152u;
            uint num28 = Singleton<SimulationManager>.instance.m_referenceFrameIndex - num27;
            float t3 = ((float)(double)(num28 & 0xFF) + Singleton<SimulationManager>.instance.m_referenceTimer) * 0.00390625f;
            Building.Frame frameData5 = data.GetFrameData(num28 - 512);
            Building.Frame frameData6 = data.GetFrameData(num28 - 256);
            instance.m_dataVector0.x = Mathf.Max(0f, (Mathf.Lerp((int)frameData5.m_fireDamage, (int)frameData6.m_fireDamage, t3) - 127f) * 0.0078125f);
            instance.m_dataVector0.z = (((data.m_flags & Building.Flags.Abandoned) == 0) ? 0f : 1f);
            if (frameData6.m_productionState < frameData5.m_productionState)
            {
                instance.m_dataVector3.w = Mathf.Lerp((int)frameData5.m_productionState, (float)(int)frameData6.m_productionState + 256f, t3) * 0.00390625f;
                if (instance.m_dataVector3.w >= 1f)
                {
                    instance.m_dataVector3.w -= 1f;
                }
            }
            else
            {
                instance.m_dataVector3.w = Mathf.Lerp((int)frameData5.m_productionState, (int)frameData6.m_productionState, t3) * 0.00390625f;
            }
            RenderInstanceBuildingAIPrefix(__instance, cameraInfo, buildingID, ref data, layerMask, ref instance);
            if (data.m_fireIntensity != 0 && Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.None)
            {
#if UseTask
                Patcher.Dispatcher.Add(RenderFireEffectMethod, __instance, cameraInfo, buildingID, data, instance);
#else
				__instance.RenderFireEffect(cameraInfo, buildingID, ref data, instance.m_dataVector0.x);
                RenderFireEffectPropsReverse(__instance, cameraInfo, buildingID, ref data, ref instance, (float)(int)data.m_fireIntensity * 0.003921569f, instance.m_dataVector0.x, true, true);
#endif

            }

            return false;
        }

        public static bool RenderDestroyedPropsPrefix(BuildingAI __instance, RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance, bool renderFixed, bool renderNonfixed)
        {
            if (__instance.m_info.m_props == null || !cameraInfo.CheckRenderDistance(instance.m_position, __instance.m_info.m_maxPropDistance + 72f))
            {
                return false;
            }
            int length = data.Length;
            Texture _HeightMap = null;
            Vector4 _HeightMapping = Vector4.zero;
            Vector4 _SurfaceMapping = Vector4.zero;
            BuildingProperties properties = Singleton<BuildingManager>.instance.m_properties;
            Building.Frame lastFrameData = data.GetLastFrameData();
            float num = (float)Mathf.Max(0, lastFrameData.m_fireDamage - 127) * 0.0078125f;
            for (int i = 0; i < __instance.m_info.m_props.Length; i++)
            {
                BuildingInfo.Prop prop = __instance.m_info.m_props[i];
                Randomizer r = new Randomizer((buildingID << 6) | prop.m_index);
                Randomizer r2 = new Randomizer((buildingID << 6) | prop.m_index);
                if (r.Int32(100u) >= prop.m_probability || length < prop.m_requiredLength)
                {
                    continue;
                }
                PropInfo finalProp = prop.m_finalProp;
                if (!(finalProp != null))
                {
                    continue;
                }
                finalProp = finalProp.GetVariation(ref r);
                float scale = finalProp.m_minScale + (float)r.Int32(10000u) * (finalProp.m_maxScale - finalProp.m_minScale) * 0.0001f;
                Color color = finalProp.GetColor(ref r);
                if (!finalProp.m_isDecal)
                {
                    finalProp = Singleton<PropManager>.instance.GetRandomPropInfo(ref r2, ItemClass.Service.Disaster);
                    finalProp = finalProp.GetVariation(ref r2);
                    scale = finalProp.m_minScale + (float)r2.Int32(10000u) * (finalProp.m_maxScale - finalProp.m_minScale) * 0.0001f;
                    color = finalProp.GetColor(ref r2);
                    if (properties != null && num != 0f)
                    {
                        color = Color.Lerp(color, properties.m_burnedColor, num);
                    }
                }
                if ((layerMask & (1 << finalProp.m_prefabDataLayer)) == 0 && !finalProp.m_hasEffects)
                {
                    continue;
                }
                Vector3 vector = instance.m_dataMatrix1.MultiplyPoint(prop.m_position);
                if (!prop.m_fixedHeight || __instance.m_info.m_requireHeightMap)
                {
                    vector.y = (float)(int)instance.m_extraData.GetUShort(i) * 0.015625f;
                }
                if (!cameraInfo.CheckRenderDistance(vector, finalProp.m_maxRenderDistance) || !((!prop.m_fixedHeight) ? renderNonfixed : renderFixed))
                {
                    continue;
                }
                InstanceID propRenderID = GetPropRenderIDReverse(__instance, buildingID, 0, ref data);
                Vector4 dataVector = instance.m_dataVector3;
                if (!prop.m_fixedHeight && (!__instance.m_info.m_colorizeEverything || finalProp.m_isDecal))
                {
                    dataVector.z = 0f;
                }
                if (finalProp.m_requireWaterMap)
                {
                    if (_HeightMap == null)
                    {
                        Singleton<TerrainManager>.instance.GetWaterMapping(data.m_position, out _HeightMap, out _HeightMapping, out _SurfaceMapping);
                    }
#if UseTask
                    Patcher.Dispatcher.Add(Delegates.PropsRenderInstanceMethod1, cameraInfo, finalProp, propRenderID, vector, scale, data.m_angle + prop.m_radAngle, color, dataVector, (data.m_flags & Building.Flags.Active) != 0, instance.m_dataTexture0, instance.m_dataVector1, instance.m_dataVector2, _HeightMap, _HeightMapping, _SurfaceMapping);
#else
                    PropInstance.RenderInstance(cameraInfo, finalProp, propRenderID, vector, scale, data.m_angle + prop.m_radAngle, color, dataVector, (data.m_flags & Building.Flags.Active) != 0, instance.m_dataTexture0, instance.m_dataVector1, instance.m_dataVector2, _HeightMap, _HeightMapping, _SurfaceMapping);
#endif
                }
                else if (finalProp.m_requireHeightMap)
                {
#if UseTask
                    Patcher.Dispatcher.Add(Delegates.PropsRenderInstanceMethod2, cameraInfo, finalProp, propRenderID, vector, scale, data.m_angle + prop.m_radAngle, color, dataVector, (data.m_flags & Building.Flags.Active) != 0, instance.m_dataTexture0, instance.m_dataVector1, instance.m_dataVector2);
#else
                    PropInstance.RenderInstance(cameraInfo, finalProp, propRenderID, vector, scale, data.m_angle + prop.m_radAngle, color, dataVector, (data.m_flags & Building.Flags.Active) != 0, instance.m_dataTexture0, instance.m_dataVector1, instance.m_dataVector2);
#endif
                }
                else
                {
#if UseTask
                    Patcher.Dispatcher.Add(Delegates.PropsRenderInstanceMethod3, cameraInfo, finalProp, propRenderID, vector, scale, data.m_angle + prop.m_radAngle, color, dataVector, (data.m_flags & Building.Flags.Active) != 0 );
#else
				                    PropInstance.RenderInstance(cameraInfo, finalProp, propRenderID, vector, scale, data.m_angle + prop.m_radAngle, color, dataVector, (data.m_flags & Building.Flags.Active) != 0);
#endif
                }
            }

            return false;
        }

        public static bool RenderPropsPrefix(BuildingAI __instance, RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance, bool renderFixed, bool renderNonfixed, bool isActive)
        {
            if (__instance.m_info.m_props == null || ((layerMask & __instance.m_info.m_treeLayers) == 0 && !cameraInfo.CheckRenderDistance(instance.m_position, __instance.m_info.m_maxPropDistance + 72f)))
            {
                return false;
            }
            int length = data.Length;
            Texture _HeightMap = null;
            Vector4 _HeightMapping = Vector4.zero;
            Vector4 _SurfaceMapping = Vector4.zero;
            Matrix4x4 lhs = Matrix4x4.zero;
            bool flag = false;
            DistrictManager instance2 = Singleton<DistrictManager>.instance;
            byte district = instance2.GetDistrict(data.m_position);
            Vector3 position = data.m_position;
            ushort num = Building.FindParentBuilding(buildingID);
            if (num != 0)
            {
                position = Singleton<BuildingManager>.instance.m_buildings.m_buffer[num].m_position;
            }
            byte park = instance2.GetPark(position);
            for (int i = 0; i < __instance.m_info.m_props.Length; i++)
            {
                BuildingInfo.Prop prop = __instance.m_info.m_props[i];
                Randomizer r = new Randomizer((buildingID << 6) | prop.m_index);
                if (r.Int32(100u) >= prop.m_probability || length < prop.m_requiredLength)
                {
                    continue;
                }
                PropInfo finalProp = prop.m_finalProp;
                TreeInfo finalTree = prop.m_finalTree;
                if (finalProp != null)
                {
                    finalProp = finalProp.GetVariation(ref r, ref instance2.m_districts.m_buffer[district], park);
                    float num2 = finalProp.m_minScale + (float)r.Int32(10000u) * (finalProp.m_maxScale - finalProp.m_minScale) * 0.0001f;
                    Color color = finalProp.GetColor(ref r);
                    if ((layerMask & (1 << finalProp.m_prefabDataLayer)) == 0 && !finalProp.m_hasEffects)
                    {
                        continue;
                    }
                    Vector4 dataVector = instance.m_dataVector3;
                    Vector3 vector;
                    if (prop.m_fixedHeight)
                    {
                        if (!renderFixed)
                        {
                            continue;
                        }
                        if (__instance.m_info.m_isFloating)
                        {
                            if (!flag)
                            {
                                Singleton<TerrainManager>.instance.HeightMap_sampleWaterHeightAndNormal(instance.m_position, 0.15f, out float h, out Vector3 normal);
                                Vector3 position2 = instance.m_position;
                                position2.y = h;
                                Quaternion q = Quaternion.FromToRotation(Vector3.up, normal) * instance.m_rotation;
                                lhs = Matrix4x4.TRS(position2, q, Vector3.one);
                                flag = true;
                            }
                            Matrix4x4 rhs = default(Matrix4x4);
                            rhs.SetTRS(prop.m_position, Quaternion.AngleAxis(prop.m_radAngle * 57.29578f, Vector3.down), new Vector3(num2, num2, num2));
                            rhs = lhs * rhs;
                            vector = rhs.MultiplyPoint(Vector3.zero);
                            if (cameraInfo.CheckRenderDistance(vector, finalProp.m_maxRenderDistance))
                            {
                                InstanceID propRenderID = GetPropRenderIDReverse(__instance, buildingID, i, ref data);
                                PropInstance.RenderInstance(cameraInfo, finalProp, propRenderID, rhs, vector, num2, data.m_angle + prop.m_radAngle, color, dataVector, isActive);
                                continue;
                            }
                        }
                        else
                        {
                            vector = instance.m_dataMatrix1.MultiplyPoint(prop.m_position);
                            if (__instance.m_info.m_requireHeightMap)
                            {
                                vector.y = (float)(int)instance.m_extraData.GetUShort(i) * 0.015625f;
                            }
                        }
                    }
                    else
                    {
                        if (!renderNonfixed)
                        {
                            continue;
                        }
                        vector = instance.m_dataMatrix1.MultiplyPoint(prop.m_position);
                        if (!__instance.m_info.m_isFloating)
                        {
                            vector.y = (float)(int)instance.m_extraData.GetUShort(i) * 0.015625f;
                        }
                        if (!__instance.m_info.m_colorizeEverything || finalProp.m_isDecal)
                        {
                            dataVector.z = 0f;
                        }
                    }
                    if (!cameraInfo.CheckRenderDistance(vector, finalProp.m_maxRenderDistance))
                    {
                        continue;
                    }
                    InstanceID propRenderID2 = GetPropRenderIDReverse(__instance, buildingID, i, ref data);
                    if (finalProp.m_requireWaterMap)
                    {
                        if (_HeightMap == null)
                        {
                            Singleton<TerrainManager>.instance.GetWaterMapping(data.m_position, out _HeightMap, out _HeightMapping, out _SurfaceMapping);
                        }
#if UseTask
                        Patcher.Dispatcher.Add(Delegates.PropsRenderInstanceMethod1, cameraInfo, finalProp, propRenderID2, vector, num2, data.m_angle + prop.m_radAngle, color, dataVector, isActive, instance.m_dataTexture0, instance.m_dataVector1, instance.m_dataVector2, _HeightMap, _HeightMapping, _SurfaceMapping);
#else
PropInstance.RenderInstance(cameraInfo, finalProp, propRenderID2, vector, num2, data.m_angle + prop.m_radAngle, color, dataVector, isActive, instance.m_dataTexture0, instance.m_dataVector1, instance.m_dataVector2, _HeightMap, _HeightMapping, _SurfaceMapping);
#endif
                    }
                    else if (finalProp.m_requireHeightMap)
                    {
#if UseTask
                        Patcher.Dispatcher.Add(Delegates.PropsRenderInstanceMethod2, cameraInfo, finalProp, propRenderID2, vector, num2, data.m_angle + prop.m_radAngle, color, dataVector, isActive, instance.m_dataTexture0, instance.m_dataVector1, instance.m_dataVector2);
#else
                        PropInstance.RenderInstance(cameraInfo, finalProp, propRenderID2, vector, num2, data.m_angle + prop.m_radAngle, color, dataVector, isActive, instance.m_dataTexture0, instance.m_dataVector1, instance.m_dataVector2);
#endif
                    }
                    else
                    {
#if UseTask
                        Patcher.Dispatcher.Add(Delegates.PropsRenderInstanceMethod3, cameraInfo, finalProp, propRenderID2, vector, num2, data.m_angle + prop.m_radAngle, color, dataVector, isActive);
#else
                        PropInstance.RenderInstance(cameraInfo, finalProp, propRenderID2, vector, num2, data.m_angle + prop.m_radAngle, color, dataVector, isActive);
#endif
                    }
                }
                else
                {
                    if (!(finalTree != null))
                    {
                        continue;
                    }
                    finalTree = finalTree.GetVariation(ref r);
                    float scale = finalTree.m_minScale + (float)r.Int32(10000u) * (finalTree.m_maxScale - finalTree.m_minScale) * 0.0001f;
                    float brightness = finalTree.m_minBrightness + (float)r.Int32(10000u) * (finalTree.m_maxBrightness - finalTree.m_minBrightness) * 0.0001f;
                    if ((layerMask & (1 << finalTree.m_prefabDataLayer)) != 0 && ((!prop.m_fixedHeight) ? renderNonfixed : renderFixed))
                    {
                        Vector3 position3 = instance.m_dataMatrix1.MultiplyPoint(prop.m_position);
                        if (!prop.m_fixedHeight || __instance.m_info.m_requireHeightMap)
                        {
                            position3.y = (float)(int)instance.m_extraData.GetUShort(i) * 0.015625f;
                        }
                        Vector4 dataVector2 = instance.m_dataVector3;
                        if (!__instance.m_info.m_colorizeEverything)
                        {
                            dataVector2.z = 0f;
                        }
#if UseTask
                        Patcher.Dispatcher.Add(Delegates.TreeRenderInstanceMethod, cameraInfo, finalTree, position3, scale, brightness, dataVector2);
#else
                        TreeInstance.RenderInstance(cameraInfo, finalTree, position3, scale, brightness, dataVector2);
#endif
                    }
                }
            }

            return false;
        }


        public static int GetCollapseTimeReverse(CommonBuildingAI inst)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return instructions;
            }
            _ = Transpiler(null);
            return default;
        }
        public static int GetConstructionTimeReverse(CommonBuildingAI inst)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return instructions;
            }
            _ = Transpiler(null);
            return default;
        }
        public static void RenderFireEffectPropsReverse(CommonBuildingAI inst, RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, ref RenderManager.Instance instance, float fireIntensity, float fireDamage, bool renderFixed, bool renderNonfixed)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return instructions;
            }
            _ = Transpiler(null);
        }
        public static InstanceID GetPropRenderIDReverse(BuildingAI inst, ushort buildingID, int propIndex, ref Building data)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return instructions;
            }
            _ = Transpiler(null);
            return default;
        }
    }
}
