using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ParallelBooster.Patches
{
    public static class NetLanePatch
    {
        public static void Patch(Harmony harmony)
        {
            var originalRenderDestroyedInstanceMethod = AccessTools.Method(typeof(NetLane), nameof(NetLane.RenderDestroyedInstance));
            var prefixRenderDestroyedInstanceMethod = AccessTools.Method(typeof(NetLanePatch), nameof(RenderDestroyedInstancePrefix));
            Patcher.PatchPrefix(harmony, originalRenderDestroyedInstanceMethod, prefixRenderDestroyedInstanceMethod);

            var originalRenderInstanceMethod = AccessTools.Method(typeof(NetLane), nameof(NetLane.RenderInstance));
            var prefixRenderInstanceMethod = AccessTools.Method(typeof(NetLanePatch), nameof(RenderInstancePrefix));
            Patcher.PatchPrefix(harmony, originalRenderInstanceMethod, prefixRenderInstanceMethod);
        }

        public static bool RenderDestroyedInstancePrefix(NetLane __instance, RenderManager.CameraInfo cameraInfo, ushort segmentID, uint laneID, NetInfo netInfo, NetInfo.Lane laneInfo, NetNode.Flags startFlags, NetNode.Flags endFlags, Color startColor, Color endColor, float startAngle, float endAngle, bool invert, int layerMask, Vector4 objectIndex1, Vector4 objectIndex2, ref RenderManager.Instance data, ref int propIndex)
        {
            NetLaneProps laneProps = laneInfo.m_laneProps;
            if (laneProps != null && laneProps.m_props != null)
            {
                bool flag = (laneInfo.m_finalDirection & NetInfo.Direction.Both) == NetInfo.Direction.Backward || (laneInfo.m_finalDirection & NetInfo.Direction.AvoidBoth) == NetInfo.Direction.AvoidForward;
                bool flag2 = flag != invert;
                if (flag)
                {
                    NetNode.Flags flags = startFlags;
                    startFlags = endFlags;
                    endFlags = flags;
                }
                int num = laneProps.m_props.Length;
                for (int i = 0; i < num; i++)
                {
                    NetLaneProps.Prop prop = laneProps.m_props[i];
                    if (__instance.m_length < prop.m_minLength)
                    {
                        continue;
                    }
                    int num2 = 2;
                    if (prop.m_repeatDistance > 1f)
                    {
                        num2 *= Mathf.Max(1, Mathf.RoundToInt(__instance.m_length / prop.m_repeatDistance));
                    }
                    int num3 = propIndex;
                    if (propIndex != -1)
                    {
                        propIndex = num3 + (num2 + 1 >> 1);
                    }
                    if (!prop.CheckFlags((NetLane.Flags)__instance.m_flags, startFlags, endFlags))
                    {
                        continue;
                    }
                    float num4 = prop.m_segmentOffset * 0.5f;
                    if (__instance.m_length != 0f)
                    {
                        num4 = Mathf.Clamp(num4 + prop.m_position.z / __instance.m_length, -0.5f, 0.5f);
                    }
                    if (flag2)
                    {
                        num4 = 0f - num4;
                    }
                    PropInfo finalProp = prop.m_finalProp;
                    if (!(finalProp != null) || (layerMask & (1 << finalProp.m_prefabDataLayer)) == 0)
                    {
                        continue;
                    }
                    Color color = (prop.m_colorMode != NetLaneProps.ColorMode.EndState) ? startColor : endColor;
                    Randomizer r = new Randomizer((int)laneID + i);
                    for (int j = 1; j <= num2; j += 2)
                    {
                        if (r.Int32(100u) >= prop.m_probability)
                        {
                            continue;
                        }
                        float num5 = num4 + (float)j / (float)num2;
                        PropInfo variation = finalProp.GetVariation(ref r);
                        float scale = variation.m_minScale + (float)r.Int32(10000u) * (variation.m_maxScale - variation.m_minScale) * 0.0001f;
                        if (prop.m_colorMode == NetLaneProps.ColorMode.Default)
                        {
                            color = variation.GetColor(ref r);
                        }
                        if (!variation.m_isDecal && !variation.m_surviveCollapse)
                        {
                            continue;
                        }
                        Vector3 vector = __instance.m_bezier.Position(num5);
                        if (propIndex != -1)
                        {
                            vector.y = (float)(int)data.m_extraData.GetUShort(num3++) * 0.015625f;
                        }
                        vector.y += prop.m_position.y;
                        if (!cameraInfo.CheckRenderDistance(vector, variation.m_maxRenderDistance))
                        {
                            continue;
                        }
                        Vector3 vector2 = __instance.m_bezier.Tangent(num5);
                        if (!(vector2 != Vector3.zero))
                        {
                            continue;
                        }
                        if (flag2)
                        {
                            vector2 = -vector2;
                        }
                        vector2.y = 0f;
                        if (prop.m_position.x != 0f)
                        {
                            vector2 = Vector3.Normalize(vector2);
                            vector.x += vector2.z * prop.m_position.x;
                            vector.z -= vector2.x * prop.m_position.x;
                        }
                        float num6 = Mathf.Atan2(vector2.x, 0f - vector2.z);
                        if (prop.m_cornerAngle != 0f || prop.m_position.x != 0f)
                        {
                            float num7 = endAngle - startAngle;
                            if (num7 > (float)Math.PI)
                            {
                                num7 -= (float)Math.PI * 2f;
                            }
                            if (num7 < -(float)Math.PI)
                            {
                                num7 += (float)Math.PI * 2f;
                            }
                            float num8 = startAngle + num7 * num5;
                            num7 = num8 - num6;
                            if (num7 > (float)Math.PI)
                            {
                                num7 -= (float)Math.PI * 2f;
                            }
                            if (num7 < -(float)Math.PI)
                            {
                                num7 += (float)Math.PI * 2f;
                            }
                            num6 += num7 * prop.m_cornerAngle;
                            if (num7 != 0f && prop.m_position.x != 0f)
                            {
                                float num9 = Mathf.Tan(num7);
                                vector.x += vector2.x * num9 * prop.m_position.x;
                                vector.z += vector2.z * num9 * prop.m_position.x;
                            }
                        }
                        Vector4 objectIndex3 = (!(num5 > 0.5f)) ? objectIndex1 : objectIndex2;
                        num6 += prop.m_angle * ((float)Math.PI / 180f);
                        InstanceID id = default(InstanceID);
                        id.NetSegment = segmentID;
#if UseTask
                        Patcher.Dispatcher.Add(() => PropInstance.RenderInstance(cameraInfo, variation, id, vector, scale, num6, color, objectIndex3, active: true));
#else
                        PropInstance.RenderInstance(cameraInfo, variation, id, vector, scale, num6, color, objectIndex3, active: true);
#endif
                    }
                }
            }
            if ((laneInfo.m_laneType & (NetInfo.LaneType.Vehicle | NetInfo.LaneType.Parking | NetInfo.LaneType.CargoVehicle | NetInfo.LaneType.TransportVehicle)) == 0 && ((laneInfo.m_laneType & NetInfo.LaneType.Pedestrian) == 0 || netInfo.m_vehicleTypes != 0))
            {
                return false;
            }
            bool flag3 = (laneInfo.m_vehicleType & ~VehicleInfo.VehicleType.Bicycle) == 0 || laneInfo.m_verticalOffset >= 0.5f;
            Randomizer r2 = new Randomizer(laneID);
            int num10 = Mathf.RoundToInt(__instance.m_length * 0.07f);
            for (int k = 0; k < num10; k++)
            {
                PropInfo randomPropInfo = Singleton<PropManager>.instance.GetRandomPropInfo(ref r2, ItemClass.Service.Road);
                randomPropInfo = randomPropInfo.GetVariation(ref r2);
                float num11 = randomPropInfo.m_minScale + (float)r2.Int32(10000u) * (randomPropInfo.m_maxScale - randomPropInfo.m_minScale) * 0.0001f;
                Color color2 = randomPropInfo.GetColor(ref r2);
                float num12 = (float)r2.Int32(1000u) * 0.001f;
                float angle = (float)r2.Int32(1000u) * 0.006283186f;
                if (!randomPropInfo.m_isDecal)
                {
                    if (flag3)
                    {
                        continue;
                    }
                    if (netInfo.m_netAI.IsOverground())
                    {
                        float num13 = netInfo.m_halfWidth - Mathf.Abs(laneInfo.m_position);
                        float num14 = Mathf.Max(randomPropInfo.m_generatedInfo.m_size.x, randomPropInfo.m_generatedInfo.m_size.z) * num11 * 0.5f - 0.5f;
                        if (num13 < num14)
                        {
                            continue;
                        }
                    }
                }
                Vector3 position = __instance.m_bezier.Position(num12);
                Vector4 objectIndex4 = (!(num12 > 0.5f)) ? objectIndex1 : objectIndex2;
                InstanceID id2 = default(InstanceID);
                id2.NetSegment = segmentID;
                if (!randomPropInfo.m_requireHeightMap)
                {
#if UseTask
                    Patcher.Dispatcher.Add(() => PropInstance.RenderInstance(cameraInfo, randomPropInfo, id2, position, num11, angle, color2, objectIndex4, active: true));
#else
                    PropInstance.RenderInstance(cameraInfo, randomPropInfo, id2, position, num11, angle, color2, objectIndex4, active: true);
#endif
                }
            }

            return false;
        }

        public static bool RenderInstancePrefix(NetLane __instance, RenderManager.CameraInfo cameraInfo, ushort segmentID, uint laneID, NetInfo.Lane laneInfo, NetNode.Flags startFlags, NetNode.Flags endFlags, Color startColor, Color endColor, float startAngle, float endAngle, bool invert, int layerMask, Vector4 objectIndex1, Vector4 objectIndex2, ref RenderManager.Instance data, ref int propIndex)
        {
            NetLaneProps laneProps = laneInfo.m_laneProps;
            if (!(laneProps != null) || laneProps.m_props == null)
            {
                return false;
            }
            bool flag = (laneInfo.m_finalDirection & NetInfo.Direction.Both) == NetInfo.Direction.Backward || (laneInfo.m_finalDirection & NetInfo.Direction.AvoidBoth) == NetInfo.Direction.AvoidForward;
            bool flag2 = flag != invert;
            if (flag)
            {
                NetNode.Flags flags = startFlags;
                startFlags = endFlags;
                endFlags = flags;
            }
            Texture _HeightMap = null;
            Vector4 _HeightMapping = Vector4.zero;
            Vector4 _SurfaceMapping = Vector4.zero;
            Texture _HeightMap2 = null;
            Vector4 _HeightMapping2 = Vector4.zero;
            Vector4 _SurfaceMapping2 = Vector4.zero;
            int num = laneProps.m_props.Length;
            for (int i = 0; i < num; i++)
            {
                NetLaneProps.Prop prop = laneProps.m_props[i];
                if (__instance.m_length < prop.m_minLength)
                {
                    continue;
                }
                int num2 = 2;
                if (prop.m_repeatDistance > 1f)
                {
                    num2 *= Mathf.Max(1, Mathf.RoundToInt(__instance.m_length / prop.m_repeatDistance));
                }
                int num3 = propIndex;
                if (propIndex != -1)
                {
                    propIndex = num3 + (num2 + 1 >> 1);
                }
                if (!prop.CheckFlags((NetLane.Flags)__instance.m_flags, startFlags, endFlags))
                {
                    continue;
                }
                float num4 = prop.m_segmentOffset * 0.5f;
                if (__instance.m_length != 0f)
                {
                    num4 = Mathf.Clamp(num4 + prop.m_position.z / __instance.m_length, -0.5f, 0.5f);
                }
                if (flag2)
                {
                    num4 = 0f - num4;
                }
                PropInfo finalProp = prop.m_finalProp;
                if (finalProp != null && (layerMask & (1 << finalProp.m_prefabDataLayer)) != 0)
                {
                    Color color = (prop.m_colorMode != NetLaneProps.ColorMode.EndState) ? startColor : endColor;
                    Randomizer r = new Randomizer((int)laneID + i);
                    for (int j = 1; j <= num2; j += 2)
                    {
                        if (r.Int32(100u) >= prop.m_probability)
                        {
                            continue;
                        }
                        float num5 = num4 + (float)j / (float)num2;
                        PropInfo variation = finalProp.GetVariation(ref r);
                        float scale = variation.m_minScale + (float)r.Int32(10000u) * (variation.m_maxScale - variation.m_minScale) * 0.0001f;
                        if (prop.m_colorMode == NetLaneProps.ColorMode.Default)
                        {
                            color = variation.GetColor(ref r);
                        }
                        Vector3 vector = __instance.m_bezier.Position(num5);
                        if (propIndex != -1)
                        {
                            vector.y = (float)(int)data.m_extraData.GetUShort(num3++) * 0.015625f;
                        }
                        vector.y += prop.m_position.y;
                        if (!cameraInfo.CheckRenderDistance(vector, variation.m_maxRenderDistance))
                        {
                            continue;
                        }
                        Vector3 vector2 = __instance.m_bezier.Tangent(num5);
                        if (!(vector2 != Vector3.zero))
                        {
                            continue;
                        }
                        if (flag2)
                        {
                            vector2 = -vector2;
                        }
                        vector2.y = 0f;
                        if (prop.m_position.x != 0f)
                        {
                            vector2 = Vector3.Normalize(vector2);
                            vector.x += vector2.z * prop.m_position.x;
                            vector.z -= vector2.x * prop.m_position.x;
                        }
                        float num6 = Mathf.Atan2(vector2.x, 0f - vector2.z);
                        if (prop.m_cornerAngle != 0f || prop.m_position.x != 0f)
                        {
                            float num7 = endAngle - startAngle;
                            if (num7 > (float)Math.PI)
                            {
                                num7 -= (float)Math.PI * 2f;
                            }
                            if (num7 < -(float)Math.PI)
                            {
                                num7 += (float)Math.PI * 2f;
                            }
                            float num8 = startAngle + num7 * num5;
                            num7 = num8 - num6;
                            if (num7 > (float)Math.PI)
                            {
                                num7 -= (float)Math.PI * 2f;
                            }
                            if (num7 < -(float)Math.PI)
                            {
                                num7 += (float)Math.PI * 2f;
                            }
                            num6 += num7 * prop.m_cornerAngle;
                            if (num7 != 0f && prop.m_position.x != 0f)
                            {
                                float num9 = Mathf.Tan(num7);
                                vector.x += vector2.x * num9 * prop.m_position.x;
                                vector.z += vector2.z * num9 * prop.m_position.x;
                            }
                        }
                        Vector4 objectIndex3 = (!(num5 > 0.5f)) ? objectIndex1 : objectIndex2;
                        num6 += prop.m_angle * ((float)Math.PI / 180f);
                        InstanceID id = default(InstanceID);
                        id.NetSegment = segmentID;
                        if (variation.m_requireWaterMap)
                        {
                            if (_HeightMap == null)
                            {
                                Singleton<TerrainManager>.instance.GetHeightMapping(Singleton<NetManager>.instance.m_segments.m_buffer[segmentID].m_middlePosition, out _HeightMap, out _HeightMapping, out _SurfaceMapping);
                            }
                            if (_HeightMap2 == null)
                            {
                                Singleton<TerrainManager>.instance.GetWaterMapping(Singleton<NetManager>.instance.m_segments.m_buffer[segmentID].m_middlePosition, out _HeightMap2, out _HeightMapping2, out _SurfaceMapping2);
                            }
#if UseTask
                            Patcher.Dispatcher.Add(() => PropInstance.RenderInstance(cameraInfo, variation, id, vector, scale, num6, color, objectIndex3, active: true, _HeightMap, _HeightMapping, _SurfaceMapping, _HeightMap2, _HeightMapping2, _SurfaceMapping2));
#else
                            PropInstance.RenderInstance(cameraInfo, variation, id, vector, scale, num6, color, objectIndex3, active: true, _HeightMap, _HeightMapping, _SurfaceMapping, _HeightMap2, _HeightMapping2, _SurfaceMapping2);
#endif
                        }
                        else if (!variation.m_requireHeightMap)
                        {
#if UseTask
                            Patcher.Dispatcher.Add(() => PropInstance.RenderInstance(cameraInfo, variation, id, vector, scale, num6, color, objectIndex3, active: true));
#else
                            PropInstance.RenderInstance(cameraInfo, variation, id, vector, scale, num6, color, objectIndex3, active: true);
#endif
                        }
                    }
                }
                TreeInfo finalTree = prop.m_finalTree;
                if (!(finalTree != null) || (layerMask & (1 << finalTree.m_prefabDataLayer)) == 0)
                {
                    continue;
                }
                Randomizer r2 = new Randomizer((int)laneID + i);
                for (int k = 1; k <= num2; k += 2)
                {
                    if (r2.Int32(100u) >= prop.m_probability)
                    {
                        continue;
                    }
                    float t = num4 + (float)k / (float)num2;
                    TreeInfo variation2 = finalTree.GetVariation(ref r2);
                    float scale2 = variation2.m_minScale + (float)r2.Int32(10000u) * (variation2.m_maxScale - variation2.m_minScale) * 0.0001f;
                    float brightness = variation2.m_minBrightness + (float)r2.Int32(10000u) * (variation2.m_maxBrightness - variation2.m_minBrightness) * 0.0001f;
                    Vector3 position = __instance.m_bezier.Position(t);
                    if (propIndex != -1)
                    {
                        position.y = (float)(int)data.m_extraData.GetUShort(num3++) * 0.015625f;
                    }
                    position.y += prop.m_position.y;
                    if (prop.m_position.x != 0f)
                    {
                        Vector3 vector3 = __instance.m_bezier.Tangent(t);
                        if (flag2)
                        {
                            vector3 = -vector3;
                        }
                        vector3.y = 0f;
                        vector3 = Vector3.Normalize(vector3);
                        position.x += vector3.z * prop.m_position.x;
                        position.z -= vector3.x * prop.m_position.x;
                    }
#if UseTask
                    Patcher.Dispatcher.Add(() => TreeInstance.RenderInstance(cameraInfo, variation2, position, scale2, brightness, RenderManager.DefaultColorLocation));
#else
                    TreeInstance.RenderInstance(cameraInfo, variation2, position, scale2, brightness, RenderManager.DefaultColorLocation);
#endif
                }
            }
            return false;
        }
    }
}
