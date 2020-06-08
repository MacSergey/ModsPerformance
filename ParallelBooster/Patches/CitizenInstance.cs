using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static CitizenInstance;

namespace ParallelBooster.Patches
{
    public static class CitizenInstancePatch
    {
        private delegate void RenderInstanceExtractedDelegate(CitizenInstance __instance, RenderManager.CameraInfo cameraInfo, ushort instanceID, CitizenInfo info, Vector3 vector, Frame frameData, Frame frameData2, float t, Quaternion quaternion, Color color, bool flag, bool flag3);
        public static void Patch(Harmony harmony)
        {
            var originalMethod = AccessTools.Method(typeof(CitizenInstance), nameof(Vehicle.RenderInstance), new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort) });
            var prefixMethod = AccessTools.Method(typeof(CitizenInstancePatch), nameof(RenderInstancePrefix));
            Patcher.PatchPrefix(harmony, originalMethod, prefixMethod);
        }

        public static bool RenderInstancePrefix(CitizenInstance __instance, RenderManager.CameraInfo cameraInfo, ushort instanceID, ref bool __result)
        {
            if ((__instance.m_flags & Flags.Character) == 0)
            {
                __result = false;
                return false;
            }
            CitizenInfo info = __instance.Info;
            if (info == null)
            {
                __result = false;
                return false;
            }
            uint num = (uint)(instanceID << 4) / 65536u;
            uint num2 = Singleton<SimulationManager>.instance.m_referenceFrameIndex - num;
            Frame frameData = __instance.GetFrameData(num2 - 32);
            float maxDistance = Mathf.Min(RenderManager.LevelOfDetailFactor * 800f, info.m_maxRenderDistance + cameraInfo.m_height * 0.5f);
            if (!cameraInfo.CheckRenderDistance(frameData.m_position, maxDistance))
            {
                __result = false;
                return false;
            }
            if (!cameraInfo.Intersect(frameData.m_position, 10f))
            {
                __result = false;
                return false;
            }
            Frame frameData2 = __instance.GetFrameData(num2 - 16);
            float t = ((float)(double)(num2 & 0xF) + Singleton<SimulationManager>.instance.m_referenceTimer) * 0.0625f;
            bool flag = frameData2.m_underground && frameData.m_underground;
            bool flag2 = frameData2.m_insideBuilding && frameData.m_insideBuilding;
            bool flag3 = frameData2.m_transition || frameData.m_transition;
            if ((flag2 && !flag3) || (flag && !flag3 && (cameraInfo.m_layerMask & (1 << Singleton<CitizenManager>.instance.m_undergroundLayer)) == 0))
            {
                __result = false;
                return false;
            }
            Bezier3 bezier = default(Bezier3);
            bezier.a = frameData.m_position;
            bezier.b = frameData.m_position + frameData.m_velocity * 0.333f;
            bezier.c = frameData2.m_position - frameData2.m_velocity * 0.333f;
            bezier.d = frameData2.m_position;
            Vector3 vector = bezier.Position(t);
            Quaternion quaternion = Quaternion.Lerp(frameData.m_rotation, frameData2.m_rotation, t);
            Color color = info.m_citizenAI.GetColor(instanceID, ref __instance, Singleton<InfoManager>.instance.CurrentMode);
#if UseTask
            Patcher.Dispatcher.Add(RenderInstanceExtractedMethod, __instance, cameraInfo, instanceID, info, vector, frameData, frameData2, t, quaternion, color, flag, flag3);
#else
                        RenderInstanceExtractedMethod.Invoke(__instance, cameraInfo, instanceID, info, vector, frameData, frameData2, t, quaternion, color, flag, flag3);
#endif
            __result = true;
            return false;
        }

        private static Action<object[]> RenderInstanceExtractedMethod { get; } = new Action<object[]>((args) =>
        {
            CitizenInstance __instance = (CitizenInstance)args[0];
            RenderManager.CameraInfo cameraInfo = (RenderManager.CameraInfo)args[1];
            ushort instanceID = (ushort)args[2];
            CitizenInfo info = (CitizenInfo)args[3];
            Vector3 vector = (Vector3)args[4];
            Quaternion quaternion = (Quaternion)args[8];
            Color color = (Color)args[9];
            bool flag = (bool)args[10];
            bool flag3 = (bool)args[11];

            if (cameraInfo.CheckRenderDistance(vector, info.m_lodRenderDistance))
            {
                InstanceID empty = InstanceID.Empty;
                empty.CitizenInstance = instanceID;
                CitizenInfo citizenInfo = info.ObtainPrefabInstance<CitizenInfo>(empty, 255);
                if (citizenInfo != null)
                {
                    Vector3 velocity = Vector3.Lerp(((Frame)args[5]).m_velocity, ((Frame)args[6]).m_velocity, (float)args[7]);
                    citizenInfo.m_citizenAI.SetRenderParameters(cameraInfo, instanceID, ref __instance, vector, quaternion, velocity, color, (flag || flag3) && (cameraInfo.m_layerMask & (1 << Singleton<CitizenManager>.instance.m_undergroundLayer)) != 0);
                    return;
                }
            }
            if (flag || flag3)
            {
                info.m_undergroundLodLocations[info.m_undergroundLodCount].SetTRS(vector, quaternion, Vector3.one);
                info.m_undergroundLodColors[info.m_undergroundLodCount] = color.linear;
                info.m_undergroundLodMin = Vector3.Min(info.m_undergroundLodMin, vector);
                info.m_undergroundLodMax = Vector3.Max(info.m_undergroundLodMax, vector);
                if (++info.m_undergroundLodCount == info.m_undergroundLodLocations.Length)
                {
                    RenderUndergroundLod(cameraInfo, info);
                }
            }
            if (!flag || flag3)
            {
                info.m_lodLocations[info.m_lodCount].SetTRS(vector, quaternion, Vector3.one);
                info.m_lodColors[info.m_lodCount] = color.linear;
                info.m_lodMin = Vector3.Min(info.m_lodMin, vector);
                info.m_lodMax = Vector3.Max(info.m_lodMax, vector);
                if (++info.m_lodCount == info.m_lodLocations.Length)
                {
                    RenderLod(cameraInfo, info);
                }
            }
        });
    }
}
