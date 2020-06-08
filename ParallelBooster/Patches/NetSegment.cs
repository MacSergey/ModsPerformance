using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using static RenderManager;

namespace ParallelBooster.Patches
{
    public static class NetSegmentPatch
    {
        private static Action<object[]> RenderInstanceExtractedMethod { get; } = new Action<object[]>((args) =>
        {
            NetSegment __instance = (NetSegment)args[0];
            RenderInstanceExtracted(ref __instance, (CameraInfo)args[1], (ushort)args[2], (int)args[3], (NetInfo)args[4], (Instance)args[5], (NetManager)args[6]);
        });

        public static List<CodeInstruction> CollapsedIfBlockFind { get; } = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(NetSegment), nameof(NetSegment.m_flags))),
                new CodeInstruction(OpCodes.Ldc_I4_8),
                new CodeInstruction(OpCodes.And)
            };
        private static int NetManagerVarIndex => 0;
        private static int PropIndex2VarIndex => 60;
        private static int Num2VarIndex => 61;
        private static int Flags3VarIndex => 46;
        private static int Flags4VarIndex => 47;
        private static int Color3VarIndex => 48;
        private static int Color4VarIndex => 49;
        private static int StartAngle2VarIndex => 55;
        private static int EndAngle2VarIndex => 56;
        private static int Invert2VarIndex => 50;
        private static int ObjectIndexVarIndex => 57;
        private static int ObjectIndex2VarIndex => 58;

        public static void Patch(Harmony harmony)
        {
            var originalMethod = AccessTools.Method(typeof(NetSegment), "RenderInstance", new Type[] { typeof(CameraInfo), typeof(ushort), typeof(int), typeof(NetInfo), typeof(Instance).MakeByRefType() });

            var extractedRenderInstanceMethod = AccessTools.Method(typeof(NetSegmentPatch), nameof(RenderInstanceExtracted));
            Patcher.PatchReverse(harmony, originalMethod, extractedRenderInstanceMethod);

            var extractedRenderLinesMethod = AccessTools.Method(typeof(NetSegmentPatch), nameof(RenderLinesExtracted));
            Patcher.PatchReverse(harmony, originalMethod, extractedRenderLinesMethod);

            var transpilerMethod = AccessTools.Method(typeof(NetSegmentPatch), nameof(RenderInstancePatch));
            Patcher.PatchTranspiler(harmony, originalMethod, transpilerMethod);
        }

        public static void AddToDispatcherInstance(NetSegment instance, CameraInfo cameraInfo, ushort segmentID, int layerMask, NetInfo info, Instance data, NetManager netManager)
        {
#if Debug && Trace
                Logger.Start(nameof(NetSegmentPatch), nameof(AddToDispatcherInstance));
#endif
#if UseTask
            Patcher.Dispatcher.Add(RenderInstanceExtractedMethod,instance, cameraInfo, segmentID, layerMask, info, data, netManager);
#else
                RenderInstanceExtracted(ref instance, cameraInfo, segmentID, layerMask, info, data, netManager);
#endif
        }
        //        public static void AddToDispatcherLanes(NetSegment instance, CameraInfo cameraInfo, ushort segmentID, int layerMask, NetInfo info, Instance data, NetManager netManager, int propIndex2, uint num2, NetNode.Flags flags3, NetNode.Flags flags4, Color color3, Color color4, float startAngle2, float endAngle2, bool invert2, Vector4 objectIndex, Vector4 objectIndex2)
        //        {
        //#if Debug && Trace
        //                Logger.Start(nameof(NetSegmentPatch), nameof(AddToDispatcherLanes));
        //#endif
        //#if UseTask
        //            Patcher.Dispatcher.Add(() => RenderLinesExtracted(ref instance, cameraInfo, segmentID, layerMask, info, ref data, netManager, propIndex2, num2, flags3, flags4, color3, color4, startAngle2, endAngle2, invert2, objectIndex, objectIndex2));
        //#else
        //                RenderLinesExtracted(ref instance, cameraInfo, segmentID, layerMask, info, ref data, netManager, propIndex2, num2, flags3, flags4, color3, color4, startAngle2, endAngle2, invert2, objectIndex, objectIndex2);
        //#endif
        //        }

#if Debug
        [HarmonyDebug]
#endif
        private static IEnumerable<CodeInstruction> RenderInstancePatch(IEnumerable<CodeInstruction> instructions)
        {
            var replaceInstructions = new CodeInstruction[]
            {
                    new CodeInstruction(OpCodes.Ldarg_S, 0),
                    new CodeInstruction(OpCodes.Ldobj, typeof(NetSegment)),
                    new CodeInstruction(OpCodes.Ldarg_S, 1),
                    new CodeInstruction(OpCodes.Ldarg_S, 2),
                    new CodeInstruction(OpCodes.Ldarg_S, 3),
                    new CodeInstruction(OpCodes.Ldarg_S, 4),
                    new CodeInstruction(OpCodes.Ldarg_S, 5),
                    new CodeInstruction(OpCodes.Ldobj, typeof(Instance)),
                    new CodeInstruction(OpCodes.Ldloc_S, NetManagerVarIndex),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatch), nameof(AddToDispatcherInstance))),
            };

            var replaceForResult = instructions.ReplaceFor(Patcher.GetIVarInstruction(39), replaceInstructions);

            //var enumerator = replaceForResult.GetEnumerator();
            //var findEnumerator = (IEnumerator<CodeInstruction>)CollapsedIfBlockFind.GetEnumerator();

            //var newInstructions = new List<CodeInstruction>();
            //var toAdd = new List<CodeInstruction>();
            //while (findEnumerator.MoveNext() && enumerator.MoveNext())
            //{
            //    //Logger.Debug($"{enumerator.Current} - {findEnumerator.Current}");

            //    toAdd.Add(enumerator.Current);

            //    if (!enumerator.Current.Is(findEnumerator.Current))
            //    {
            //        newInstructions.AddRange(toAdd);
            //        toAdd.Clear();
            //        findEnumerator.Reset();
            //    }
            //}

            //newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldobj, typeof(NetSegment)));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 1));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 2));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 3));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 4));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 5));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldobj, typeof(Instance)));

            //newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, NetManagerVarIndex));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, PropIndex2VarIndex));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, Num2VarIndex));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, Flags3VarIndex));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, Flags4VarIndex));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, Color3VarIndex));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, Color4VarIndex));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, StartAngle2VarIndex));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, EndAngle2VarIndex));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, Invert2VarIndex));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, ObjectIndexVarIndex));
            //newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, ObjectIndex2VarIndex));

            //newInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatch), nameof(AddToDispatcherLanes))));

            //while (enumerator.MoveNext()) ;

            //newInstructions.Add(enumerator.Current);

            var newInstructions = replaceForResult;
#if Debug && Trace
                Logger.AddDebugInstructions(newInstructions, nameof(NetSegmentPatch), nameof(NetSegmentPatch.RenderInstancePatch));
#endif
#if Debug && IL
            Logger.Debug(nameof(NetSegmentPatch), nameof(NetSegmentPatch.RenderInstancePatch), newInstructions);
#endif
            return newInstructions;
        }

#if Debug
        [HarmonyDebug]
#endif
        private static void RenderInstanceExtracted(ref NetSegment instance, CameraInfo cameraInfo, ushort segmentID, int layerMask, NetInfo info, Instance data, NetManager netManager)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var newInstructions = new List<CodeInstruction>();

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 6));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, NetManagerVarIndex));

                newInstructions.AddRange(instructions.GetFor(Patcher.GetIVarInstruction(39)));
#if Debug
                Patcher.AddStopWatch(newInstructions);
#endif
                newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Debug && Trace
                    Logger.AddDebugInstructions(newInstructions, nameof(NetSegmentPatch), nameof(RenderInstanceExtracted));
#endif
#if Debug && IL
                Logger.Debug(nameof(NetSegmentPatch), nameof(RenderInstanceExtracted), newInstructions);
#endif
                return newInstructions;
            }

            _ = Transpiler(null);
        }

        private static void RenderLinesExtracted(ref NetSegment instance, CameraInfo cameraInfo, ushort segmentID, int layerMask, NetInfo info, ref Instance data, NetManager netManager, int propIndex2, uint num2, NetNode.Flags flags3, NetNode.Flags flags4, Color color3, Color color4, float startAngle2, float endAngle2, bool invert2, Vector4 objectIndex, Vector4 objectIndex2)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var newInstructions = new List<CodeInstruction>();

                var argIndex = 5;

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, argIndex += 1));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, NetManagerVarIndex));

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, argIndex += 1));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, PropIndex2VarIndex));

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, argIndex += 1));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, Num2VarIndex));

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, argIndex += 1));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, Flags3VarIndex));

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, argIndex += 1));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, Flags4VarIndex));

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, argIndex += 1));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, Color3VarIndex));

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, argIndex += 1));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, Color4VarIndex));

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, argIndex += 1));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, StartAngle2VarIndex));

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, argIndex += 1));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, EndAngle2VarIndex));

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, argIndex += 1));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, Invert2VarIndex));

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, argIndex += 1));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, ObjectIndexVarIndex));

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, argIndex += 1));
                newInstructions.Add(new CodeInstruction(OpCodes.Stloc_S, ObjectIndex2VarIndex));

                newInstructions.AddRange(CollapsedIfBlockFind);

                var enumerator = instructions.GetEnumerator();
                var findEnumerator = CollapsedIfBlockFind.GetEnumerator();

                Patcher.FindIfBegin(enumerator, findEnumerator, out _);

                newInstructions.Add(enumerator.Current);

                for (var prev = (CodeInstruction)null; enumerator.MoveNext(); prev = enumerator.Current)
                {
                    if (prev != null)
                        newInstructions.Add(prev);
                }
#if Debug
                //newInstructions.Clear();
                //Patcher.AddStopWatch(newInstructions);
#endif
                newInstructions.Add(enumerator.Current);

#if Debug && Trace
                    Logger.AddDebugInstructions(newInstructions, nameof(NetSegmentPatch), nameof(RenderLinesExtracted));
#endif
#if Debug && IL
                Logger.Debug(nameof(NetSegmentPatch), nameof(RenderLinesExtracted), newInstructions);
#endif
                return newInstructions;
            }

            _ = Transpiler(null);
        }
    }
}
