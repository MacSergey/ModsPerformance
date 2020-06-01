using CitiesHarmony.API;
using HarmonyLib;
using ParallelBooster.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static RenderManager;

namespace ParallelBooster
{
    public static class Patcher
    {
        private static string HarmonyId { get; } = nameof(ParallelBooster);

        public static void Patch() => HarmonyHelper.DoOnHarmonyReady(() => Begin());
        public static void Unpatch()
        {
            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
        }

        private static void Begin()
        {
            var harmony = new Harmony(HarmonyId);

            NetManagerPatch.Patch(harmony);
        }
    }
}
