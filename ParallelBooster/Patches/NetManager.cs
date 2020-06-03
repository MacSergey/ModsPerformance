using ColossalFramework;
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
        private static Stopwatch Stopwatch { get; } = new Stopwatch();

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
        public static void Start() => Stopwatch.Start();
        public static void Stop() => Stopwatch.Stop();


        public static class NetManagerPatch
        {
            public static void Patch(Harmony harmony)
            {
                var originalMethod = AccessTools.Method(typeof(NetManager), "EndRenderingImpl");

                var extractedMethod = AccessTools.Method(typeof(NetManagerPatch), nameof(EndRenderingImplExtracted));
                Patcher.PatchReverse(harmony, originalMethod, extractedMethod);

                var transpilerMethod = AccessTools.Method(typeof(NetManagerPatch), nameof(EndRenderingImplPatch));
                Patcher.PatchTranspiler(harmony, originalMethod, transpilerMethod);
            }

            public static void RenderGroups(NetManager instance, CameraInfo cameraInfo, FastList<RenderGroup> renderedGroups)
            {
#if Debug
                Logger.Debug($"Start {nameof(renderedGroups)}={renderedGroups.m_size} (thread={Thread.CurrentThread.ManagedThreadId})");
                var sw = Stopwatch.StartNew();
                var dipsSw = new Stopwatch();
                Stopwatch.Reset();
#endif

#if UseTask
                CustomDispatcher.Clear();

                var task = Task.Create(() =>
                {
#if Debug
                    Logger.Debug($"Start task (thread={Thread.CurrentThread.ManagedThreadId})");
                    var tasksw = Stopwatch.StartNew();
#endif
                    EndRenderingImplExtracted(instance, cameraInfo, renderedGroups);
#if Debug
                    tasksw.Stop();
                    Logger.Debug($"Task end {tasksw.ElapsedTicks}");
#endif
                });
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
#else
                EndRenderingImplExtracted(instance, cameraInfo, renderedGroups);
#endif

#if Debug
                sw.Stop();
                Logger.Debug($"Actions duration {Stopwatch.ElapsedTicks}");
                Logger.Debug($"Dispatcher duration {dipsSw.ElapsedTicks}");
                Logger.Debug($"End {sw.ElapsedTicks}");
#endif
            }

#if Debug
            [HarmonyDebug]
#endif
            private static IEnumerable<CodeInstruction> EndRenderingImplPatch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var replaceInstructions = new CodeInstruction[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetManagerPatch), nameof(RenderGroups))),
                };

                var newInstructions = instructions.ReplaceFor(Patcher.GetIVarInstruction(1), replaceInstructions);
#if Debug && IL
#if Debug && Trace
                Logger.AddDebugInstructions(newInstructions, nameof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplPatch));
#endif
                Logger.Debug(nameof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplPatch), newInstructions);
#endif
                return newInstructions;
            }

#if Debug
            [HarmonyDebug]
#endif
            private static void EndRenderingImplExtracted(NetManager instance, CameraInfo cameraInfo, FastList<RenderGroup> renderedGroups)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var newInstructions = new List<CodeInstruction>();

                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_2));
                    newInstructions.Add(new CodeInstruction(OpCodes.Stloc_0));

                    newInstructions.AddRange(instructions.GetFor(Patcher.GetIVarInstruction(1)));

                    newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Debug && IL
#if Trace
                    Logger.AddDebugInstructions(newInstructions, nameof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplExtracted));
#endif
                    Logger.Debug(nameof(NetManagerPatch), nameof(NetManagerPatch.EndRenderingImplExtracted), newInstructions);
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
            public static List<CodeInstruction> GetReplace(string methodName) => new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_S, 0),
                new CodeInstruction(OpCodes.Ldobj, typeof(NetNode)),
                new CodeInstruction(OpCodes.Ldarg_S, 1),
                new CodeInstruction(OpCodes.Ldarg_S, 2),
                new CodeInstruction(OpCodes.Ldarg_S, 3),
                new CodeInstruction(OpCodes.Ldarg_S, 4),
                new CodeInstruction(OpCodes.Ldarg_S, 5),
                new CodeInstruction(OpCodes.Ldarg_S, 6),
                new CodeInstruction(OpCodes.Ldobj, typeof(uint)),
                new CodeInstruction(OpCodes.Ldarg_S, 7),
                new CodeInstruction(OpCodes.Ldobj, typeof(Instance)),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatch), methodName)),
            };

            public static void Patch(Harmony harmony)
            {
                var originalMethod = AccessTools.Method(typeof(NetNode), "RenderInstance", new Type[] { typeof(CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(Instance).MakeByRefType() });

                var extractedJunctionIfIfMethod = AccessTools.Method(typeof(NetNodePatch), nameof(RenderInstanceJunctionIfIfExtracted));
                Patcher.PatchReverse(harmony, originalMethod, extractedJunctionIfIfMethod);

                var extractedJunctionIfElseMethod = AccessTools.Method(typeof(NetNodePatch), nameof(RenderInstanceJunctionIfElseExtracted));
                Patcher.PatchReverse(harmony, originalMethod, extractedJunctionIfElseMethod);

                var extractedEndIfMethod = AccessTools.Method(typeof(NetNodePatch), nameof(RenderInstanceEndIfExtracted));
                Patcher.PatchReverse(harmony, originalMethod, extractedEndIfMethod);

                var extractedBendIfMethod = AccessTools.Method(typeof(NetNodePatch), nameof(RenderInstanceBendIfExtracted));
                Patcher.PatchReverse(harmony, originalMethod, extractedBendIfMethod);

                var transpilerMethod = AccessTools.Method(typeof(NetNodePatch), nameof(RenderInstancePatch));
                Patcher.PatchTranspiler(harmony, originalMethod, transpilerMethod);
            }

#if Debug
            [HarmonyDebug]
#endif
            private static IEnumerable<CodeInstruction> RenderInstancePatch(IEnumerable<CodeInstruction> instructions)
            {
                var mainIfBlock = instructions.GetIfBlock(MainIfBlockFind);
                //Logger.Debug(nameof(NetNodePatch), nameof(mainIfBlock), mainIfBlock);

                var junctionIfBlock = mainIfBlock.GetIfBlock(JunctionIfBlockFind, true);
                //Logger.Debug(nameof(NetNodePatch), nameof(junctionIfBlock), junctionIfBlock);

                junctionIfBlock = junctionIfBlock.ReplaceIfBlock(JunctionIfIfBlockFind, GetReplace(nameof(AddToDispatcherJunctionIfIf)), true);
                junctionIfBlock = junctionIfBlock.ReplaceElseBlock(JunctionIfIfBlockFind, GetReplace(nameof(AddToDispatcherJunctionIfElse)));
                //Logger.Debug(nameof(NetNodePatch), nameof(junctionIfBlock), junctionIfBlock);

                mainIfBlock = mainIfBlock.ReplaceIfBlock(JunctionIfBlockFind, junctionIfBlock);
                mainIfBlock = mainIfBlock.ReplaceIfBlock(EndIfBlockFind, GetReplace(nameof(AddToDispatcherEndIf)));
                mainIfBlock = mainIfBlock.ReplaceIfBlock(BendIfBlockFind, GetReplace(nameof(AddToDispatcherBendIf)));
                //Logger.Debug(nameof(NetNodePatch), nameof(mainIfBlock), mainIfBlock);

                var newInstructions = instructions.ReplaceIfBlock(MainIfBlockFind, mainIfBlock);

#if Debug && Trace
                Logger.AddDebugInstructions(newInstructions, nameof(NetNodePatch), nameof(RenderInstancePatch));
#endif
#if Debug && IL
                Logger.Debug(nameof(NetNodePatch), nameof(RenderInstancePatch), newInstructions);
#endif
                return newInstructions;
            }

            public static void AddToDispatcherJunctionIfIf(NetNode instance, CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, uint instanceIndex, Instance data)
            {
#if Debug && Trace
                Logger.Start(nameof(NetNodePatch), nameof(AddToDispatcherJunctionIfIf));
#endif
#if UseTask
                CustomDispatcher.Add(() => RenderInstanceJunctionIfIfExtracted(ref instance, cameraInfo, nodeID, info, iter, flags, ref instanceIndex, ref data));
#else
                RenderInstanceJunctionIfIfExtracted(ref instance, cameraInfo, nodeID, info, iter, flags, ref instanceIndex, ref data);
#endif
            }
            public static void AddToDispatcherJunctionIfElse(NetNode instance, CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, uint instanceIndex, Instance data)
            {
#if Debug && Trace
                Logger.Start(nameof(NetNodePatch), nameof(AddToDispatcherJunctionIfElse));
#endif
#if UseTask
                CustomDispatcher.Add(() => RenderInstanceJunctionIfElseExtracted(ref instance, cameraInfo, nodeID, info, iter, flags, ref instanceIndex, ref data));
#else
                RenderInstanceJunctionIfElseExtracted(ref instance, cameraInfo, nodeID, info, iter, flags, ref instanceIndex, ref data);
#endif
            }
            public static void AddToDispatcherEndIf(NetNode instance, CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, uint instanceIndex, Instance data)
            {
#if Debug && Trace
                Logger.Start(nameof(NetNodePatch), nameof(AddToDispatcherEndIf));
#endif
#if UseTask
                CustomDispatcher.Add(() => RenderInstanceEndIfExtracted(ref instance, cameraInfo, nodeID, info, iter, flags, ref instanceIndex, ref data));
#else
                RenderInstanceEndIfExtracted(ref instance, cameraInfo, nodeID, info, iter, flags, ref instanceIndex, ref data);
#endif
            }
            public static void AddToDispatcherBendIf(NetNode instance, CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, uint instanceIndex, Instance data)
            {
#if Debug && Trace
                Logger.Start(nameof(NetNodePatch), nameof(AddToDispatcherBendIf));
#endif
#if UseTask
                CustomDispatcher.Add(() => RenderInstanceBendIfExtracted(ref instance, cameraInfo, nodeID, info, iter, flags, ref instanceIndex, ref data));
#else
                RenderInstanceBendIfExtracted(ref instance, cameraInfo, nodeID, info, iter, flags, ref instanceIndex, ref data);
#endif
            }

#if Debug
            [HarmonyDebug]
#endif
            private static void RenderInstanceJunctionIfIfExtracted(ref NetNode instance, CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, ref uint instanceIndex, ref Instance data)
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
#if Debug
                    Patcher.AddStopWatch(newInstructions);
#endif
                    newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Debug && Trace
                    Logger.AddDebugInstructions(newInstructions, nameof(NetNodePatch), nameof(NetNodePatch.RenderInstanceJunctionIfIfExtracted));
#endif
#if Debug && IL
                    Logger.Debug(nameof(NetNodePatch), nameof(NetNodePatch.RenderInstanceJunctionIfIfExtracted), newInstructions);
#endif
                    return newInstructions;
                }

                _ = Transpiler(null);
            }

#if Debug
            [HarmonyDebug]
#endif
            private static void RenderInstanceJunctionIfElseExtracted(ref NetNode instance, CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, ref uint instanceIndex, ref Instance data)
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
#if Debug
                    Patcher.AddStopWatch(newInstructions);
#endif
                    newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Debug && Trace
                    Logger.AddDebugInstructions(newInstructions, nameof(NetNodePatch), nameof(NetNodePatch.RenderInstanceJunctionIfElseExtracted));
#endif
#if Debug && IL
                    Logger.Debug(nameof(NetNodePatch), nameof(NetNodePatch.RenderInstanceJunctionIfElseExtracted), newInstructions);
#endif
                    return newInstructions;
                }

                _ = Transpiler(null);
            }

#if Debug
            [HarmonyDebug]
#endif
            private static void RenderInstanceEndIfExtracted(ref NetNode instance, CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, ref uint instanceIndex, ref Instance data)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var newInstructions = new List<CodeInstruction>();

                    var mainIfBlock = instructions.GetIfBlock(MainIfBlockFind);
                    //Logger.Debug(nameof(NetNodePatch), nameof(mainIfBlock), mainIfBlock);

                    var endIfBlock = mainIfBlock.GetIfBlock(EndIfBlockFind, true);
                    //Logger.Debug(nameof(NetNodePatch), nameof(junctionIfBlock), junctionIfBlock);

                    newInstructions.AddRange(endIfBlock);
#if Debug
                    Patcher.AddStopWatch(newInstructions);
#endif
                    newInstructions.Add(new CodeInstruction(OpCodes.Ret));

#if Debug && Trace
                    Logger.AddDebugInstructions(newInstructions, nameof(NetNodePatch), nameof(NetNodePatch.RenderInstanceEndIfExtracted));
#endif
#if Debug && IL
                    Logger.Debug(nameof(NetNodePatch), nameof(NetNodePatch.RenderInstanceEndIfExtracted), newInstructions);
#endif
                    return newInstructions;
                }

                _ = Transpiler(null);
            }

#if Debug
            [HarmonyDebug]
#endif
            private static void RenderInstanceBendIfExtracted(ref NetNode instance, CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, ref uint instanceIndex, ref Instance data)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var newInstructions = new List<CodeInstruction>();

                    var mainIfBlock = instructions.GetIfBlock(MainIfBlockFind);
                    //Logger.Debug(nameof(NetNodePatch), nameof(mainIfBlock), mainIfBlock);

                    var bendIfBlock = mainIfBlock.GetIfBlock(BendIfBlockFind);
                    //Logger.Debug(nameof(NetNodePatch), nameof(junctionIfBlock), junctionIfBlock);

                    newInstructions.AddRange(bendIfBlock);
#if Debug
                    Patcher.AddStopWatch(newInstructions);
#endif
                    newInstructions.Add(new CodeInstruction(OpCodes.Ret));
#if Debug && Trace
                    Logger.AddDebugInstructions(newInstructions, nameof(NetNodePatch), nameof(NetNodePatch.RenderInstanceBendIfExtracted));
#endif
#if Debug && IL
                    Logger.Debug(nameof(NetNodePatch), nameof(NetNodePatch.RenderInstanceBendIfExtracted), newInstructions);
#endif
                    return newInstructions;
                }

                _ = Transpiler(null);
            }
        }

        public static class NetSegmentPatch
        {
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
                CustomDispatcher.Add(() => RenderInstanceExtracted(ref instance, cameraInfo, segmentID, layerMask, info, data, netManager));
#else
                RenderInstanceExtracted(ref instance, cameraInfo, segmentID, layerMask, info, data, netManager);
#endif
            }
            public static void AddToDispatcherLanes(NetSegment instance, CameraInfo cameraInfo, ushort segmentID, int layerMask, NetInfo info, Instance data, NetManager netManager, int propIndex2, uint num2, NetNode.Flags flags3, NetNode.Flags flags4, Color color3, Color color4, float startAngle2, float endAngle2, bool invert2, Vector4 objectIndex, Vector4 objectIndex2)
            {
#if Debug && Trace
                Logger.Start(nameof(NetSegmentPatch), nameof(AddToDispatcherLanes));
#endif
#if UseTask
                CustomDispatcher.Add(() => RenderLinesExtracted(ref instance, cameraInfo, segmentID, layerMask, info, ref data, netManager, propIndex2, num2, flags3, flags4, color3, color4, startAngle2, endAngle2, invert2, objectIndex, objectIndex2));
#else
                RenderLinesExtracted(ref instance, cameraInfo, segmentID, layerMask, info, ref data, netManager, propIndex2, num2, flags3, flags4, color3, color4, startAngle2, endAngle2, invert2, objectIndex, objectIndex2);
#endif
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
                    new CodeInstruction(OpCodes.Ldloc_S, NetManagerVarIndex),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatch), nameof(AddToDispatcherInstance))),
                };

                var replaceForResult = instructions.ReplaceFor(Patcher.GetIVarInstruction(39), replaceInstructions);

                var enumerator = replaceForResult.GetEnumerator();
                var findEnumerator = (IEnumerator<CodeInstruction>)CollapsedIfBlockFind.GetEnumerator();

                var newInstructions = new List<CodeInstruction>();
                var toAdd = new List<CodeInstruction>();
                while (findEnumerator.MoveNext() && enumerator.MoveNext())
                {
                    //Logger.Debug($"{enumerator.Current} - {findEnumerator.Current}");

                    toAdd.Add(enumerator.Current);

                    if (!enumerator.Current.Is(findEnumerator.Current))
                    {
                        newInstructions.AddRange(toAdd);
                        toAdd.Clear();
                        findEnumerator.Reset();
                    }
                }

                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldobj, typeof(NetSegment)));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 1));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 2));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 3));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 4));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_S, 5));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldobj, typeof(Instance)));

                newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, NetManagerVarIndex));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, PropIndex2VarIndex));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, Num2VarIndex));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, Flags3VarIndex));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, Flags4VarIndex));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, Color3VarIndex));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, Color4VarIndex));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, StartAngle2VarIndex));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, EndAngle2VarIndex));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, Invert2VarIndex));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, ObjectIndexVarIndex));
                newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_S, ObjectIndex2VarIndex));

                newInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatch), nameof(AddToDispatcherLanes))));

                while (enumerator.MoveNext()) ;

                newInstructions.Add(enumerator.Current);
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

                    //while (enumerator.MoveNext())
                    //{
                    //    newInstructions.Add(enumerator.Current);
                    //}
                    for(var prev = (CodeInstruction)null; enumerator.MoveNext(); prev = enumerator.Current)
                    {
                        if (prev != null)
                            newInstructions.Add(prev);
                    }
#if Debug
                    Patcher.AddStopWatch(newInstructions);
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
}
