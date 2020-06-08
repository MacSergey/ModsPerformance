using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static Vehicle;

namespace ParallelBooster.Patches
{
    public static class VehiclePatch
    {
        private static Action<object[]> RenderInstanceMethod { get; } = new Action<object[]>((args) => Vehicle.RenderInstance((RenderManager.CameraInfo)args[0], (VehicleInfo)args[1], (Vector3)args[2], (Quaternion)args[3], (Vector3)args[4], (Vector4)args[5], (Vector4)args[6], (Vector3)args[7], (float)args[8], (Color)args[9], (Flags)args[10], (int)args[11], (InstanceID)args[12], (bool)args[13], (bool)args[14]));

        public static void Patch(Harmony harmony)
        {
            var originalMethod = AccessTools.Method(typeof(Vehicle), nameof(Vehicle.RenderInstance), new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort) });
            var prefixMethod = AccessTools.Method(typeof(VehiclePatch), nameof(RenderInstancePrefix));
            Patcher.PatchPrefix(harmony, originalMethod, prefixMethod);
        }

        public static bool RenderInstancePrefix(Vehicle __instance, RenderManager.CameraInfo cameraInfo, ushort vehicleID, ref bool __result)
        {
            if ((__instance.m_flags & Flags.Spawned) == 0)
            {
                __result = false;
                return false;
            }
            VehicleInfo info = __instance.Info;
            if (info == null)
            {
                __result = false;
                return false;
            }
            uint targetFrame = __instance.GetTargetFrame(info, vehicleID);
            Vector3 framePosition = __instance.GetFramePosition(targetFrame - 32);
            float maxDistance = Mathf.Min(Mathf.Max(info.m_maxRenderDistance, RenderManager.LevelOfDetailFactor * 5000f), info.m_maxRenderDistance * (1f + cameraInfo.m_height * 0.0005f) + cameraInfo.m_height * 0.4f);
            if (!cameraInfo.CheckRenderDistance(framePosition, maxDistance))
            {
                __result = false;
                return false;
            }
            if (!cameraInfo.Intersect(framePosition, info.m_generatedInfo.m_size.z * 0.5f + 15f))
            {
                __result = false;
                return false;
            }
            Frame frameData = __instance.GetFrameData(targetFrame - 32);
            Frame frameData2 = __instance.GetFrameData(targetFrame - 16);
            float num = ((float)(double)(targetFrame & 0xF) + Singleton<SimulationManager>.instance.m_referenceTimer) * 0.0625f;
            bool flag = frameData2.m_underground && frameData.m_underground;
            bool flag2 = frameData2.m_insideBuilding && frameData.m_insideBuilding;
            bool flag3 = frameData2.m_transition || frameData.m_transition;
            if (flag2 && !flag3)
            {
                __result = false;
                return false;
            }
            if (flag && !flag3)
            {
                if ((cameraInfo.m_layerMask & (1 << Singleton<VehicleManager>.instance.m_undergroundLayer)) == 0)
                {
                    __result = false;
                    return false;
                }
            }
            else if ((cameraInfo.m_layerMask & (1 << info.m_prefabDataLayer)) == 0)
            {
                __result = false;
                return false;
            }
            Bezier3 bezier = default(Bezier3);
            bezier.a = frameData.m_position;
            bezier.b = frameData.m_position + frameData.m_velocity * 0.333f;
            bezier.c = frameData2.m_position - frameData2.m_velocity * 0.333f;
            bezier.d = frameData2.m_position;
            Vector3 position = bezier.Position(num);
            Bezier3 bezier2 = default(Bezier3);
            bezier2.a = frameData.m_swayPosition;
            bezier2.b = frameData.m_swayPosition + frameData.m_swayVelocity * 0.333f;
            bezier2.c = frameData2.m_swayPosition - frameData2.m_swayVelocity * 0.333f;
            bezier2.d = frameData2.m_swayPosition;
            Vector3 swayPosition = bezier2.Position(num);
            swayPosition.x *= info.m_leanMultiplier / Mathf.Max(1f, info.m_generatedInfo.m_wheelGauge);
            swayPosition.z *= info.m_nodMultiplier / Mathf.Max(1f, info.m_generatedInfo.m_wheelBase);
            Vector4 lightState = (!(num >= 0.5f)) ? frameData.m_lightIntensity : frameData2.m_lightIntensity;
            Quaternion rotation = Quaternion.Lerp(frameData.m_rotation, frameData2.m_rotation, num);
            Color color = info.m_vehicleAI.GetColor(vehicleID, ref __instance, Singleton<InfoManager>.instance.CurrentMode);
            color.a = ((!(num >= 0.5f)) ? frameData.m_blinkState : frameData2.m_blinkState);
            Vector4 tyrePosition = default(Vector4);
            tyrePosition.x = frameData.m_steerAngle + (frameData2.m_steerAngle - frameData.m_steerAngle) * num;
            tyrePosition.y = frameData.m_travelDistance + (frameData2.m_travelDistance - frameData.m_travelDistance) * num;
            tyrePosition.z = 0f;
            tyrePosition.w = 0f;
            Vector3 velocity = Vector3.Lerp(frameData.m_velocity, frameData2.m_velocity, num) * 3.75f;
            float acceleration = frameData2.m_velocity.magnitude - frameData.m_velocity.magnitude;
            InstanceID id = default(InstanceID);
            id.Vehicle = vehicleID;
#if UseTask
            Patcher.Dispatcher.Add(RenderInstanceMethod,cameraInfo, info, position, rotation, swayPosition, lightState, tyrePosition, velocity, acceleration, color, __instance.m_flags, ~(1 << (int)__instance.m_gateIndex), id, flag || flag3, !flag || flag3);
#else
            Vehicle.RenderInstance(cameraInfo, info, position, rotation, swayPosition, lightState, tyrePosition, velocity, acceleration, color, __instance.m_flags, ~(1 << (int)__instance.m_gateIndex), id, flag || flag3, !flag || flag3);
#endif
            __result = true;
            return false;
        }
    }
}
