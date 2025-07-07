using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reptile;
using HarmonyLib;

namespace ScoreAttack.Patches
{
    [HarmonyPatch(typeof(Player))]
    internal static class CornerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.LandCombo))]
        private static void LandCombo_Prefix(Player __instance)
        {
            var currentEncounter = WorldHandler.instance.currentEncounter;
            if (__instance.IsComboing() && currentEncounter != null && currentEncounter is ScoreAttackEncounter)
                __instance.ClearMultipliersDone();
        }
    }
}
