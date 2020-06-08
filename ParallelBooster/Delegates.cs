using HarmonyLib;
using ParallelBooster.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ParallelBooster
{
    public static class Delegates
    {
        public static Action<object[]> PropsRenderInstanceMethod1 { get; } = new Action<object[]>((args) => PropInstance.RenderInstance((RenderManager.CameraInfo)args[0], (PropInfo)args[1], (InstanceID)args[2], (Vector3)args[3], (float)args[4], (float)args[5], (Color)args[6], (Vector4)args[7], (bool)args[8], (Texture)args[9], (Vector4)args[10], (Vector4)args[11], (Texture)args[12], (Vector4)args[13], (Vector4)args[14]));
        public static Action<object[]> PropsRenderInstanceMethod2 { get; } = new Action<object[]>((args) => PropInstance.RenderInstance((RenderManager.CameraInfo)args[0], (PropInfo)args[1], (InstanceID)args[2], (Vector3)args[3], (float)args[4], (float)args[5], (Color)args[6], (Vector4)args[7], (bool)args[8], (Texture)args[9], (Vector4)args[10], (Vector4)args[11]));
        public static Action<object[]> PropsRenderInstanceMethod3 { get; } = new Action<object[]>((args) => PropInstance.RenderInstance((RenderManager.CameraInfo)args[0], (PropInfo)args[1], (InstanceID)args[2], (Vector3)args[3], (float)args[4], (float)args[5], (Color)args[6], (Vector4)args[7], (bool)args[8]));
        public static Action<object[]> TreeRenderInstanceMethod { get; } = new Action<object[]>((args) => TreeInstance.RenderInstance((RenderManager.CameraInfo)args[0], (TreeInfo)args[1], (Vector3)args[2], (float)args[3], (float)args[4], (Vector4)args[5]));
    }
}
