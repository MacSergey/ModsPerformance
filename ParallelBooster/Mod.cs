using HarmonyLib;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParallelBooster
{
    public class Mod : LoadingExtensionBase, IUserMod
    {
        public string Name => nameof(ParallelBooster);

        public string Description => "Increases performance by parallelizing render calculations";

        public void OnEnabled()
        {
            Patcher.Patch();
        }
        public void F()
        {

        }
    }
}
