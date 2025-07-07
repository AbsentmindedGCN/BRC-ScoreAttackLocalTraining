using HarmonyLib;
using Reptile;

namespace ScoreAttack.Patches
{
    [HarmonyPatch(typeof(NPC))]
    internal static class NPCPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(NPC.SetAvailable))]
        private static bool SetAvailable_Prefix(bool set, NPC __instance)
        {
            var currentEncounter = WorldHandler.instance.currentEncounter;
            if (!set && currentEncounter != null && currentEncounter is ScoreAttackEncounter && __instance.available)
            {
                __instance.available = false;
                return false;
            }
            return true;
        }
    }
}
