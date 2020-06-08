using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.Threading;
using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;
using UnityEngine;
using static RenderManager;

namespace ModsPerformance
{
    public class Mod : LoadingExtensionBase, IUserMod
    {
        public string Name => nameof(ModsPerformance);

        public string Description => "Mods performance";

        public void OnEnabled()
        {
            Patcher.Patch();
        }
    }

    public static partial class Patcher
    {
        private static string HarmonyId { get; } = nameof(ModsPerformance);
        private static double ReportInterval => 1000;
        private static HashSet<MethodBase> Methods { get; set; }
        private static Dictionary<MethodBase, List<long>> Performance { get; } = new Dictionary<MethodBase, List<long>>();
        private static object Lock { get; } = new object();
        private static HashSet<string> IgnoreAssembly { get; } = new HashSet<string>()
        {
            "ColossalManaged",
            //"Assembly-CSharp",
        };
        private static HashSet<string> MethodsName { get; } = new HashSet<string>()
        {
            "SimulationStep",
            "Update",
            "LateUpdate",
            "OnGUI"
        };
        private static HashSet<Type> ExcludeSkip { get; } = new HashSet<Type>
        {
            typeof(RenderManager),
            typeof(CameraController)
        };
        private static Timer ReportTimer { get; set; }

        public static void Patch() => HarmonyHelper.DoOnHarmonyReady(() => Begin());
        private static void Begin()
        {
            Debug($"Start tracking");

            Methods = FindMethods();

            var harmony = new Harmony(HarmonyId);
            var prefix = new HarmonyMethod(AccessTools.Method(typeof(Patcher), nameof(Patcher.Prefix)));
            var postfix = new HarmonyMethod(AccessTools.Method(typeof(Patcher), nameof(Patcher.Postfix)));

            foreach (var method in Methods)
            {
                try
                {
                    harmony.Patch(method, prefix, postfix);
                    Debug($"Start tracking: {method.GetString()}");
                    Performance.Add(method, new List<long>());
                }
                catch (Exception error)
                {
                    Debug($"Start tracking falled: {method.GetString()}\n{error.Message}\n{error.StackTrace}");
                }
            }

            ReportTimer = new Timer(ReportInterval)
            {
                AutoReset = true
            };
            ReportTimer.Elapsed += Report;
            ReportTimer.Start();
        }

        private static void Report(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock (Lock)
                {
                    Debug(string.Join("", Performance.Select(p => $"\n\t{p.Key.GetString()} = {(p.Value.Any() ? (int)p.Value.Average() : -1)}").ToArray()));
                    Performance.Clear();
                }
            }
            catch (Exception error)
            {
                Error($"Report error: {error.Message} {error.StackTrace}");
            }
        }

        private static void Prefix(MethodBase __originalMethod, ref Stopwatch __state)
        {
            __state = Stopwatch.StartNew();
        }
        private static void Postfix(MethodBase __originalMethod, ref Stopwatch __state)
        {
            if (__state != null)
            {
                __state.Stop();
                Set(__originalMethod, __state.ElapsedTicks);
            }
        }

        private static void Set(MethodBase method, long duration)
        {
            lock (Lock)
            {
                if (!Performance.TryGetValue(method, out List<long> list))
                {
                    list = new List<long>();
                    Performance.Add(method, list);
                }

                list.Add(duration);
            }
        }


        private static HashSet<MethodBase> FindMethods()
        {
            var methods = new HashSet<MethodBase>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(i => !IgnoreAssembly.Contains(i.GetName().Name)))
            {
                try
                {
                    foreach (Type type in assembly.GetTypes().Where(i => i.IsSubclassOf(typeof(MonoBehaviour)) && !i.IsGenericType /*&& !i.IsSubclassOf(typeof(UIComponent))*/))
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                        {
                            if (MethodsName.Contains(method.Name) && method.ReturnType == typeof(void) && method.GetParameters().Length == 0 && !IgnoreAssembly.Contains(method.DeclaringType.Assembly.GetName().Name) && !method.IsVirtual)
                                methods.Add(method);
                        }
                    }
                }
                catch { }
            }

            Debug($"{methods.Count} Finded methods:{string.Join("", methods.Select(m => $"\n\t{m.GetString()}").ToArray())}");

            return methods;
        }


        private static void Debug(string message) => Log(UnityEngine.Debug.Log, message);
        private static void Error(string message) => Log(UnityEngine.Debug.LogError, message);
        private static void Log(Action<string> logAction, string message) => logAction($"[{HarmonyId}] {message}");
        public static string GetString(this MethodBase method) => $"{method.DeclaringType.Assembly.GetName().Name}:{method.DeclaringType.Name}.{method.Name}";
    }
}
