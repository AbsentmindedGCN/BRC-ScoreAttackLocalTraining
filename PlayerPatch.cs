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
    internal static class PlayerPatch
    {

        // Set the current encounter to null if we're on score attack before running the end graffiti mode method then restore it, so that we can get the cops on us.
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.EndGraffitiMode))]
        private static void EndGraffitiMode_Prefix(ref Encounter __state)
        {
            var currentEncounter = WorldHandler.instance.currentEncounter;
            if (currentEncounter is ScoreAttackEncounter)
            {
                __state = currentEncounter;
                WorldHandler.instance.currentEncounter = null;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.EndGraffitiMode))]
        private static void EndGraffitiMode_Postfix(ref Encounter __state)
        {
            if (__state != null)
                WorldHandler.instance.currentEncounter = __state;
        }
    }
}
