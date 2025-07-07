using HarmonyLib;
using Reptile;
using UnityEngine;

namespace ScoreAttack.Patches
{
    [HarmonyPatch(typeof(ScoreEncounter))]
    internal static class UpdateMainEventPatch
    {
        public static float customDeltaTime
        {
            get
            {
                return Time.deltaTime; // Just do it like TR I guess
            }
            set
            {
            }
        }

        // Prefix to modify UpdateMainEvent behavior
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ScoreEncounter.UpdateMainEvent))]
        private static bool UpdateMainEvent_Prefix(ScoreEncounter __instance)
        {
            // Clone Core.dt to a custom variable to prevent interference
            //float customDeltaTime = Time.deltaTime;

            // If you need to manipulate customDeltaTime, do it here
            // For example, if you need to make time pass at a fixed rate, modify customDeltaTime

            // Update the time limit with the custom delta time, bypassing IsBusyWithSequence()
            __instance.timeLimitTimer -= customDeltaTime;
            if (__instance.timeLimitTimer < 0f)
            {
                __instance.timeLimitTimer = 0f;
            }

            // Continue with the rest of the method logic
            __instance.ScoreGot = __instance.player.score + __instance.player.baseScore * __instance.player.scoreMultiplier;

            if (__instance.hasContestants)
            {
                __instance.UpdateContestants();
            }

            __instance.SetScoreUI();

            if (__instance.ScoreGot >= (float)__instance.opponentsScore && !__instance.hasOpponents)
            {
                __instance.SetEncounterState(Encounter.EncounterState.MAIN_EVENT_SUCCES_DECAY);
                return false; // Prevent the original method from running since we've already handled logic
            }

            if (__instance.timeLimitTimer > 0f)
            {
                //ScoreAttackEncounter.UpdateMainEvent();
                var EncounterTemp = WorldHandler.instance.currentEncounter;
                EncounterTemp.UpdateMainEvent();
                return false; // Prevent original method from running if the timer is still ticking
            }

            if (__instance.ScoreGot >= (float)__instance.opponentsScore)
            {
                __instance.SetEncounterState(Encounter.EncounterState.MAIN_EVENT_SUCCES_DECAY);
                return false; // Prevent the original method from running if success state is reached
            }

            __instance.SetEncounterState(Encounter.EncounterState.MAIN_EVENT_FAILED_DECAY);
            return false; // Prevent original method from running since we've handled all state updates
        }
    }
}


namespace ScoreAttack.Patches
{
    [HarmonyPatch(typeof(Player))]
    internal static class StartGraffitiModePatch
    {
        // Flag to skip PauseCore during Graffiti Mode
        private static bool skipPauseCore = false;

        // Flag for when Core.PauseCore is called due to Graffiti Mode
        public static bool inGraffitiMode { get; private set; } = false;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.StartGraffitiMode))]
        private static void StartGraffitiMode_Prefix()
        {
            // Set flag to skip PauseCore
            skipPauseCore = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.StartGraffitiMode))]
        private static void StartGraffitiMode_Postfix(Player __instance)
        {
            // Reset flags after method runs
            skipPauseCore = false;
        }

        // Patch Core.PauseCore so it only works when not in graffiti mode override
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Core), nameof(Core.PauseCore))]
        private static bool PauseCore_Prefix(PauseType pauseType)
        {
            // Detect if we're skipping PauseCore and it's trying to pause for graffiti
            if (skipPauseCore && (pauseType == PauseType.Graffiti || pauseType == PauseType.ForceOff || pauseType == PauseType.Forced))
            {
                if (pauseType == PauseType.Graffiti)
                {
                    inGraffitiMode = true;
                    // (Clover) removed the GameObject.SetActive() code since as far as I can tell, it isn't needed anymore
                }
            }
            return true;
        }

        // Patch Core.UnPauseCore to ensure proper UI and camera handling when unpausing
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Core), nameof(Core.UnPauseCore))]
        private static bool UnPauseCore_Prefix(PauseType pauseType)
        {
            // Ensure proper camera and UI reset when Graffiti Mode ends
            if (pauseType == PauseType.Graffiti)
            {
                inGraffitiMode = false;

                // Handle UI and camera reset on unpause
                var playerUnpauseInstance = WorldHandler.instance.GetCurrentPlayer();
                if (playerUnpauseInstance != null)
                {
                    playerUnpauseInstance.cam.gameObject.SetActive(true);  // Reactivate camera
                    playerUnpauseInstance.ui.gameplayScreen.gameObject.SetActive(true);  // Reactivate gameplay screen
                    playerUnpauseInstance.ui.graffitiScreen.gameObject.SetActive(false); // Hide graffiti screen
                    playerUnpauseInstance.phone.dynamicGameplayScreen.gameObject.SetActive(true); // Reactivate dynamic phone screen
                }
            }
            return true;
        }
    }
}


namespace ScoreAttack.Patches
{
    [HarmonyPatch(typeof(GraffitiGame))]
    internal static class GraffitiGamePatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(GraffitiGame.Play))]
        private static void Play_Postfix(string anim)
        {
            ScoreAttackGhostSystem.GhostRecorder ghostRecorder = ScoreAttackGhostSystem.GhostManager.Instance.GetGhostRecorder();
            var currentGhostState = ScoreAttackGhostSystem.GhostManager.Instance.GetGhostState();
            if (ghostRecorder == currentGhostState)
            {
                ghostRecorder.Replay.Frames[ghostRecorder.Replay.Frames.Count - 1].Animation.ID = Animator.StringToHash(anim);
                ghostRecorder.Replay.Frames[ghostRecorder.Replay.Frames.Count - 1].Animation.Time = 0f;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(GraffitiGame.PlayPuppetSlashAnim))]
        private static void PlayPuppetSlashAnim_Postfix(Side side, bool outAnim)
        {
            ScoreAttackGhostSystem.GhostRecorder ghostRecorder = ScoreAttackGhostSystem.GhostManager.Instance.GetGhostRecorder();
            var currentGhostState = ScoreAttackGhostSystem.GhostManager.Instance.GetGhostState();
            if (ghostRecorder == currentGhostState)
            {
                ghostRecorder.Replay.Frames[ghostRecorder.Replay.Frames.Count - 1].Animation.ID = Animator.StringToHash("grafSlash" + side.ToString() + (outAnim ? "_Out" : ""));
                ghostRecorder.Replay.Frames[ghostRecorder.Replay.Frames.Count - 1].Animation.Time = 0f;
            }
        }
    }
} 