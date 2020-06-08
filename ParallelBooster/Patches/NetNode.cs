using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using static RenderManager;

namespace ParallelBooster.Patches
{
    public static class NetNodePatch
    {
        private delegate void ExtractedDelegate(NetNode instance, CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, uint instanceIndex, Instance data);

        private static Action<object[]> RenderInstanceJunctionIfIfExtractedMethod { get; } = new Action<object[]>((args) =>
        {
            NetNode __instance = (NetNode)args[0];
            uint instanceIndex = (uint)args[6];
            Instance data = (Instance)args[7];

            RenderInstanceJunctionIfIfExtracted(ref __instance, (CameraInfo)args[1], (ushort)args[2], (NetInfo)args[3], (int)args[4], (NetNode.Flags)args[5], ref instanceIndex, ref data);
        });
        private static Action<object[]> RenderInstanceJunctionIfElseExtractedMethod { get; } = new Action<object[]>((args) =>
        {
            NetNode __instance = (NetNode)args[0];
            uint instanceIndex = (uint)args[6];
            Instance data = (Instance)args[7];

            RenderInstanceJunctionIfElseExtracted(ref __instance, (CameraInfo)args[1], (ushort)args[2], (NetInfo)args[3], (int)args[4], (NetNode.Flags)args[5], ref instanceIndex, ref data);
        });
        private static Action<object[]> RenderInstanceEndIfExtractedMethod { get; } = new Action<object[]>((args) =>
        {
            NetNode __instance = (NetNode)args[0];
            uint instanceIndex = (uint)args[6];
            Instance data = (Instance)args[7];

            RenderInstanceEndIfExtracted(ref __instance, (CameraInfo)args[1], (ushort)args[2], (NetInfo)args[3], (int)args[4], (NetNode.Flags)args[5], ref instanceIndex, ref data);
        });
        private static Action<object[]> RenderInstanceBendIfExtractedMethod { get; } = new Action<object[]>((args) =>
        {
            NetNode __instance = (NetNode)args[0];
            uint instanceIndex = (uint)args[6];
            Instance data = (Instance)args[7];

            RenderInstanceBendIfExtracted(ref __instance, (CameraInfo)args[1], (ushort)args[2], (NetInfo)args[3], (int)args[4], (NetNode.Flags)args[5], ref instanceIndex, ref data);
        });

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
            Patcher.Dispatcher.Add(RenderInstanceJunctionIfIfExtractedMethod, instance, cameraInfo, nodeID, info, iter, flags, instanceIndex, data );
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
            Patcher.Dispatcher.Add(RenderInstanceJunctionIfElseExtractedMethod, instance, cameraInfo, nodeID, info, iter, flags, instanceIndex, data);
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
            Patcher.Dispatcher.Add(RenderInstanceEndIfExtractedMethod,instance, cameraInfo, nodeID, info, iter, flags, instanceIndex, data);
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
            Patcher.Dispatcher.Add(RenderInstanceBendIfExtractedMethod, instance, cameraInfo, nodeID, info, iter, flags, instanceIndex, data);
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
}
