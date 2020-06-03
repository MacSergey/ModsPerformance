using HarmonyLib;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ParallelBooster
{
    public class Mod : LoadingExtensionBase, IUserMod
    {
        public string Version => Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).OfType<AssemblyFileVersionAttribute>().FirstOrDefault() is AssemblyFileVersionAttribute versionAttribute ? versionAttribute.Version : string.Empty;
        public string Name => $"{nameof(ParallelBooster)} {Version} [BETA]";
        public string Description => "Increases performance by parallelizing render calculations";

        public Mod()
        {
#if Debug
            Harmony.DEBUG = true;
#endif
        }

        public void OnEnabled()
        {
            Patcher.Patch();
        }
    }
}
