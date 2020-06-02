using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ParallelBooster
{
    public static class Logger
    {
        private static string Source { get; } = nameof(ParallelBooster);
        public static void Debug(string message) => Log(UnityEngine.Debug.Log, message);
        public static void Debug(string typeName, string methodName, IEnumerable<CodeInstruction> instructions) => Debug($"{typeName}.{methodName}{string.Join("", instructions.Select(i => $"\n\t{i}").ToArray())}");
        public static void Error(string message) => Log(UnityEngine.Debug.LogError, message);
        private static void Log(Action<string> logAction, string message) => logAction($"[{Source}] {message}");

        internal static void Error(Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
