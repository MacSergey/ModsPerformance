using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace ParallelBooster
{
    public static class Logger
    {
        private static string Source { get; } = nameof(ParallelBooster);
        public static void Debug(string message) => Log(UnityEngine.Debug.Log, message);
        public static void Start(string typeName, string methodName) => Debug($"start {typeName}.{methodName}");
        public static void Debug(string typeName, string methodName, IEnumerable<CodeInstruction> instructions) => Debug($"{typeName}.{methodName}{string.Join("", instructions.Select(i => $"\n\t{i}").ToArray())}");
        public static void Error(string message) => Log(UnityEngine.Debug.LogError, message);
        public static void Error(Exception error) => Error($"\n{error.Message}\n{error.StackTrace}");
        private static void Log(Action<string> logAction, string message) => logAction($"[{Source}] {message}");

        public static void AddDebugInstructions(List<CodeInstruction> instructions, string typeName, string methodName)
        {
            var debugInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldstr, $"start {typeName}.{methodName}"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Logger), nameof(Debug), new Type[] { typeof(string)}))
            };
            instructions.InsertRange(0, debugInstructions);
        }
    }
}
