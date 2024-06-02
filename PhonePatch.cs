using Reptile.Phone;
using HarmonyLib;
using Reptile;

namespace ScoreAttack.Patches
{
    [HarmonyPatch(typeof(Phone))]
    internal static class PhonePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Phone.PushNotification))]
        private static bool PushNotification_Prefix(App app)
        {
            var currentEncounter = WorldHandler.instance.currentEncounter;
            if (app.GetType().Name == "AppEncounters" && currentEncounter != null && currentEncounter is ScoreAttackEncounter)
                return false;
            return true;
        }
    }
}
