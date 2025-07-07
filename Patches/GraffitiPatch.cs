using HarmonyLib;
using Reptile;

namespace ScoreAttack.Patches
{
    [HarmonyPatch(typeof(Player))]
    internal static class StartGraffitiModePatch
    {
        // Disable the PauseCore for Graffiti so timer appropriately ticks down during a score battle
        // Backup the Core instance's PauseCore delegate, or use a flag if it's not replaceable
        private static bool skipPauseCore = false;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.StartGraffitiMode))]
        private static void StartGraffitiMode_Prefix()
        {
            // Set flag to skip PauseCore
            skipPauseCore = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.StartGraffitiMode))]
        private static void StartGraffitiMode_Postfix()
        {
            // Reset flag after method runs
            skipPauseCore = false;
        }

        // Patch Core.PauseCore so it only works when not in graffiti mode override
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Core), nameof(Core.PauseCore))]
        private static bool PauseCore_Prefix(PauseType pauseType)
        {
            // If we're skipping PauseCore and it's trying to pause for graffiti, cancel it
            if (skipPauseCore && pauseType == PauseType.Graffiti)
            {
                return false;
            }
            return true;
        }
    }
}
