using ColossalFramework.Threading;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using UnityEngine;
using static RenderManager;

namespace ParallelBooster.Patches
{
    public static class NetManagerRenderPatch
    {
        private static CustomDispatcher CustomDispatcher { get; } = new CustomDispatcher();

        public static void Patch(Harmony harmony)
        {
            try
            {
                NetManagerPatch.Patch(harmony);
                NetNodePatch.Patch(harmony);
                NetSegmentPatch.Patch(harmony);
            }
            catch (Exception error)
            {
                //Logger.Error(error);
                throw;
            }
        }

        public static class NetManagerPatch
        {
            public static void Patch(Harmony harmony)
            {
                var originalMethod = AccessTools.Method(typeof(NetManager), "EndRenderingImpl");
                var extractedMethod = AccessTools.Method(typeof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplExtractedDummy));
                var transpilerMethod = AccessTools.Method(typeof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplPatch));

                Patcher.PatchReverse(harmony, originalMethod, extractedMethod);
                Patcher.PatchTranspiler(harmony, originalMethod, transpilerMethod);
            }

            public static void RenderGroups(NetManager instance, CameraInfo cameraInfo, FastList<RenderGroup> renderedGroups)
            {
#if Debug
                Logger.Debug($"Start. {nameof(renderedGroups)}={renderedGroups.m_size} (thread={Thread.CurrentThread.ManagedThreadId})");
                var sw = Stopwatch.StartNew();
                var dipsSw = new Stopwatch();
#endif

                CustomDispatcher.Clear();

                var task = Task.Create(() => EndRenderingImplExtractedDummy(instance, cameraInfo, renderedGroups));
                task.Run();


                while (!task.hasEnded || !CustomDispatcher.IsDone)
                {
#if Debug
                    dipsSw.Start();
#endif
                    CustomDispatcher.Execute();
#if Debug
                    dipsSw.Stop();
#endif
                }

#if Debug
                sw.Stop();
                Logger.Debug($"Dispatcher duration {dipsSw.ElapsedTicks}");
                Logger.Debug($"End {sw.ElapsedTicks}");
#endif
            }

#if Debug
            [HarmonyDebug]
#endif
            private static IEnumerable<CodeInstruction> EndRenderingImplPatch(IEnumerable<CodeInstruction> instructions)
            {
                var replaceInstructions = new CodeInstruction[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetManagerPatch), nameof(NetManagerPatch.RenderGroups))),
                };

                var newInstructions = instructions.ReplaceFor(Patcher.GetIVarInstruction(1), replaceInstructions).ToList();
#if Debug && IL
                Logger.Debug(nameof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplPatch), newInstructions);
#endif
                return newInstructions;
            }

#if Debug
            [HarmonyDebug]
#endif
            private static void EndRenderingImplExtractedDummy(NetManager instance, CameraInfo cameraInfo, FastList<RenderGroup> renderedGroups)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var newInstructions = new List<CodeInstruction>();

                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_2));
                    newInstructions.Add(new CodeInstruction(OpCodes.Stloc_0));
                    newInstructions.AddRange(instructions.GetFor(Patcher.GetIVarInstruction(1)));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Debug && IL
                    Logger.Debug(nameof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplExtractedDummy), newInstructions);
#endif
                    return newInstructions;
                }

                _ = Transpiler(null);
            }
        }

        public static class NetNodePatch
        {
            public static List<CodeInstruction> MainIfBlockFind { get; } = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_S, 7),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Instance), nameof(Instance.m_initialized))),
            };
            public static List<CodeInstruction> JunctionIfBlockFind { get; } = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_S, 5),
                new CodeInstruction(OpCodes.Ldc_I4, (int)NetNode.Flags.Junction),
                new CodeInstruction(OpCodes.And)
            };
            public static List<CodeInstruction> JunctionIfIfBlockFind { get; } = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_S, 7),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Instance), nameof(Instance.m_dataInt0))),
                new CodeInstruction(OpCodes.Ldc_I4_8),
                new CodeInstruction(OpCodes.And)
            };
            public static List<CodeInstruction> EndIfBlockFind { get; } = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_S, 5),
                new CodeInstruction(OpCodes.Ldc_I4_S, (int)NetNode.Flags.End),
                new CodeInstruction(OpCodes.And)
            };
            public static List<CodeInstruction> BendIfBlockFind { get; } = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_S, 5),
                new CodeInstruction(OpCodes.Ldc_I4_S, (int)NetNode.Flags.Bend),
                new CodeInstruction(OpCodes.And)
            };

            public static void Patch(Harmony harmony)
            {
                var originalMethod = AccessTools.Method(typeof(NetNode), "RenderInstance", new Type[] { typeof(CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(Instance).MakeByRefType() });

                var extractedJunctionIfIfMethod = AccessTools.Method(typeof(NetNodePatch), nameof(NetNodePatch.RenderInstanceExtractedJunctionIfIfDummy));
                Patcher.PatchReverse(harmony, originalMethod, extractedJunctionIfIfMethod);

                var extractedJunctionIfElseMethod = AccessTools.Method(typeof(NetNodePatch), nameof(NetNodePatch.RenderInstanceExtractedJunctionIfElseDummy));
                Patcher.PatchReverse(harmony, originalMethod, extractedJunctionIfElseMethod);

                var extractedEndIfMethod = AccessTools.Method(typeof(NetNodePatch), nameof(NetNodePatch.RenderInstanceExtractedEndIfDummy));
                Patcher.PatchReverse(harmony, originalMethod, extractedEndIfMethod);

                var extractedBendIfMethod = AccessTools.Method(typeof(NetNodePatch), nameof(NetNodePatch.RenderInstanceExtractedBendIfDummy));
                Patcher.PatchReverse(harmony, originalMethod, extractedBendIfMethod);
            }

#if Debug
            [HarmonyDebug]
#endif
            private static void RenderInstanceExtractedJunctionIfIfDummy(ref NetNode instance, CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, ref uint instanceIndex, ref Instance data)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var newInstructions = new List<CodeInstruction>();

                    var mainIfBlock = instructions.GetIfBlock(MainIfBlockFind);
                    //Logger.Debug(nameof(NetNodePatch), nameof(mainIfBlock), mainIfBlock);

                    var junctionIfBlock = mainIfBlock.GetIfBlock(JunctionIfBlockFind, true);
                    //Logger.Debug(nameof(NetNodePatch), nameof(junctionIfBlock), junctionIfBlock);

                    var junctionIfIfBlock = junctionIfBlock.GetIfBlock(JunctionIfIfBlockFind, true);
                    //Logger.Debug(nameof(NetNodePatch), nameof(junctionIfIfBlock), junctionIfIfBlock);

                    newInstructions.AddRange(junctionIfIfBlock);
                    newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Debug && IL
                    Logger.Debug(nameof(NetNodePatch), nameof(NetNodePatch.RenderInstanceExtractedJunctionIfIfDummy), newInstructions);
#endif
                    return newInstructions;
                }

                _ = Transpiler(null);
            }

#if Debug
            [HarmonyDebug]
#endif
            private static void RenderInstanceExtractedJunctionIfElseDummy(ref NetNode instance, CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, ref uint instanceIndex, ref Instance data)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var newInstructions = new List<CodeInstruction>();

                    var mainIfBlock = instructions.GetIfBlock(MainIfBlockFind);
                    //Logger.Debug(nameof(NetNodePatch), nameof(mainIfBlock), mainIfBlock);

                    var junctionIfBlock = mainIfBlock.GetIfBlock(JunctionIfBlockFind, true);
                    //Logger.Debug(nameof(NetNodePatch), nameof(junctionIfBlock), junctionIfBlock);

                    var junctionIfElseBlock = junctionIfBlock.GetElseBlock(JunctionIfIfBlockFind);
                    //Logger.Debug(nameof(NetNodePatch), nameof(junctionIfElseBlock), junctionIfElseBlock);

                    newInstructions.AddRange(junctionIfElseBlock);
                    newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Debug && IL
                    Logger.Debug(nameof(NetNodePatch), nameof(NetNodePatch.RenderInstanceExtractedJunctionIfElseDummy), newInstructions);
#endif
                    return newInstructions;
                }

                _ = Transpiler(null);
            }

#if Debug
            [HarmonyDebug]
#endif
            private static void RenderInstanceExtractedEndIfDummy(ref NetNode instance, CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, ref uint instanceIndex, ref Instance data)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var newInstructions = new List<CodeInstruction>();

                    var mainIfBlock = instructions.GetIfBlock(MainIfBlockFind);
                    //Logger.Debug(nameof(NetNodePatch), nameof(mainIfBlock), mainIfBlock);

                    var endIfBlock = mainIfBlock.GetIfBlock(EndIfBlockFind, true);
                    //Logger.Debug(nameof(NetNodePatch), nameof(junctionIfBlock), junctionIfBlock);

                    newInstructions.AddRange(endIfBlock);
                    newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Debug && IL
                    Logger.Debug(nameof(NetNodePatch), nameof(NetNodePatch.RenderInstanceExtractedEndIfDummy), newInstructions);
#endif
                    return newInstructions;
                }

                _ = Transpiler(null);
            }

#if Debug
            [HarmonyDebug]
#endif
            private static void RenderInstanceExtractedBendIfDummy(ref NetNode instance, CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, ref uint instanceIndex, ref Instance data)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var newInstructions = new List<CodeInstruction>();

                    var mainIfBlock = instructions.GetIfBlock(MainIfBlockFind);
                    //Logger.Debug(nameof(NetNodePatch), nameof(mainIfBlock), mainIfBlock);

                    var bendIfBlock = mainIfBlock.GetIfBlock(BendIfBlockFind);
                    //Logger.Debug(nameof(NetNodePatch), nameof(junctionIfBlock), junctionIfBlock);

                    newInstructions.AddRange(bendIfBlock);
                    newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Debug && IL
                    Logger.Debug(nameof(NetNodePatch), nameof(NetNodePatch.RenderInstanceExtractedBendIfDummy), newInstructions);
#endif
                    return newInstructions;
                }

                _ = Transpiler(null);
            }
        }

        public static class NetSegmentPatch
        {
            public static void Patch(Harmony harmony)
            {
                var originalMethod = AccessTools.Method(typeof(NetSegment), "RenderInstance", new Type[] { typeof(CameraInfo), typeof(ushort), typeof(int), typeof(NetInfo), typeof(Instance).MakeByRefType() });
                var extractedMethod = AccessTools.Method(typeof(NetSegmentPatch), nameof(NetSegmentPatch.RenderInstanceExtractedDummy));
                var transpilerMethod = AccessTools.Method(typeof(NetSegmentPatch), nameof(NetSegmentPatch.RenderInstancePatch));

                Patcher.PatchReverse(harmony, originalMethod, extractedMethod);
                Patcher.PatchTranspiler(harmony, originalMethod, transpilerMethod);
            }

            public static void AddToDispatcher(NetSegment instance, CameraInfo cameraInfo, ushort segmentID, int layerMask, NetInfo info, Instance data)
            {
                var action = new Action(() => RenderInstanceExtractedDummy(instance, cameraInfo, segmentID, layerMask, info, ref data));
                CustomDispatcher.Add(action);
            }

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
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatch), nameof(NetSegmentPatch.AddToDispatcher))),
                };

                var newInstructions = instructions.ReplaceFor(Patcher.GetIVarInstruction(39), replaceInstructions).ToList();
#if Debug && IL
                Logger.Debug(nameof(NetSegmentPatch), nameof(NetSegmentPatch.RenderInstancePatch), newInstructions);
#endif
                return newInstructions;
            }

#if Debug
            [HarmonyDebug]
#endif
            private static void RenderInstanceExtractedDummy(NetSegment instance, CameraInfo cameraInfo, ushort segmentID, int layerMask, NetInfo info, ref Instance data)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var newInstructions = new List<CodeInstruction>();
                    newInstructions.AddRange(instructions.GetFor(Patcher.GetIVarInstruction(39)));
                    newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Debug && IL
                    Logger.Debug(nameof(NetSegmentPatch), nameof(NetSegmentPatch.RenderInstanceExtractedDummy), newInstructions);
#endif
                    return newInstructions;
                }

                _ = Transpiler(null);
            }
        }
    }
}
