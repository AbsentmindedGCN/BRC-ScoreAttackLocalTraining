using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reptile;
using HarmonyLib;
using UnityEngine;

namespace ScoreAttackGhostSystem.Patches
{
    [HarmonyPatch]
    internal class GhostPatch
    {
        // Prevent AI from breaking glass
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BreakableObject), "CheckSetPlayerFromHitbox")]
        private static bool CheckSetPlayerFromHitbox_Prefix(BreakableObject __instance, Collider newCollider)
        {
            if (__instance.curCollider == newCollider)
                return false;

            Player foundPlayer = newCollider.transform.GetComponentInParent<Player>();
            if (foundPlayer == GhostPlayer.ghostPlayerCharacter || foundPlayer.isAI)
                return false;

            // Let original method run
            return true;
        }

        // Prevent AI Hitboxes (Not working...)
        /* [HarmonyPrefix]
        [HarmonyPatch(typeof(Junk), nameof(Junk.OnTriggerStay))]
        private static bool OnTriggerStay_Prefix(Junk __instance, Collider other)
        {
            if (__instance.junkBehaviour == null)
                return false;

            if (other.gameObject.layer == 25)
                return false;

            if (JunkBehaviour.IsJunkKicked(__instance.junkBehaviour.kickedJunkIndex, __instance.junkBehaviour.nonupdatingJunkIndex, __instance._index))
                return false;

            if (__instance.rehitTimer != 0f)
                return false;

            int layer = other.gameObject.layer;
            if (layer != 18 && layer != 20)
                return false;

            if (layer == 18)
            {
                Player player = other.transform.GetComponentInParent<Player>();
                if (player == null || player.isAI)
                {
                    return false; // Skip AI (including ghostPlayerCharacter)
                }
            }

            return true; // Allow original method to run
        } */

        // CAR HIBOXES
        [HarmonyPatch(typeof(MoveAlongPoints))]
        public static class Hitbox_HitboxHitPlayer_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch("HitboxHitPlayer")]
            public static bool Prefix_HitboxHitPlayer(Player player)
            {
                // Skip the method if the player is the ghost AI
                return player != GhostPlayer.ghostPlayerCharacter;
            }
        }

        // DON'T PAUSE CAUSE
        [HarmonyPatch(typeof(MoveAlongHandler))]
        public static class MoveAlongHandler_GetCurrentPlayer_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch("GetCurrentPlayer")]
            public static bool Prefix_GetCurrentPlayer(MoveAlongHandler __instance, ref Player __result)
            {
                if (GhostPlayer.ghostPlayerCharacter != null &&
                    WorldHandler.instance.GetCurrentPlayer() == GhostPlayer.ghostPlayerCharacter)
                {
                    __result = null; // or any alternative behavior you want
                    return false;    // skip the original method
                }

                return true; // continue with original method
            }
        }

        // Record particle emissions
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ParticleSystem))]
        [HarmonyPatch(nameof(ParticleSystem.Emit), new Type[] {typeof(int)})]
        private static void SetFrameEmit(int count, ParticleSystem __instance) {
            if (WorldHandler.instance?.GetCurrentPlayer() == null) { return; }
            Player player = WorldHandler.instance?.GetCurrentPlayer();

            if (GhostRecorder.Instance != null && GhostRecorder.Instance.Recording) {
                if (GhostRecorder.Instance.LastFrame == null) { return; }
                
                if (__instance == player.ringParticles) {
                    GhostRecorder.Instance.LastFrame.Effects.ringParticles = count;
                } 

                if (__instance == player.spraypaintParticles) {
                    GhostRecorder.Instance.LastFrame.Effects.spraypaintParticles = count;
                }
            }
        } 

        // Record gameplay SFX
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AudioManager))]
        [HarmonyPatch(nameof(AudioManager.PlaySfxGameplay), new Type[] {typeof(SfxCollectionID), typeof(AudioClipID), typeof(AudioSource), typeof(float)})]
        private static void SetSFXGameplayAlt(SfxCollectionID collectionId, AudioClipID audioClipId, AudioSource audioSource, float randomPitchVariance) {
            if (WorldHandler.instance?.GetCurrentPlayer() == null) { return; }
            if (audioSource != WorldHandler.instance?.GetCurrentPlayer().playerOneShotAudioSource) { return; }
            if (GhostRecorder.Instance != null && GhostRecorder.Instance.Recording) {
                if (GhostRecorder.Instance.LastFrame == null) { return; }
                
                GhostRecorder.Instance.LastFrame.SFX.Add(new GhostFrame.GhostFrameSFX() {
                    CollectionID = collectionId,
                    AudioClipID = audioClipId,
                    RandomPitchVariance = randomPitchVariance
                });
            }
        }

        // Record gameplay SFX
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AudioManager))]
        [HarmonyPatch(nameof(AudioManager.PlaySfxGameplay), new Type[] {typeof(MoveStyle), typeof(AudioClipID), typeof(AudioSource), typeof(float)})]
        private static void SetSFXGameplay(MoveStyle moveStyle, AudioClipID audioClipId, AudioSource audioSource, float randomPitchVariance) {
            if (WorldHandler.instance?.GetCurrentPlayer() == null) { return; }
            if (audioSource != WorldHandler.instance?.GetCurrentPlayer().playerOneShotAudioSource) { return; }
            if (GhostRecorder.Instance != null && GhostRecorder.Instance.Recording) {
                if (GhostRecorder.Instance.LastFrame == null) { return; }
                
                GhostRecorder.Instance.LastFrame.SFX.Add(new GhostFrame.GhostFrameSFX() {
                    AudioClipID = audioClipId,
                    RandomPitchVariance = randomPitchVariance
                });
            }
        }

        // Record gameplay voices
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AudioManager))]
        [HarmonyPatch(nameof(AudioManager.PlayVoice), new Type[] {typeof(VoicePriority), typeof(Characters), typeof(AudioClipID), typeof(AudioSource), typeof(VoicePriority)}, new ArgumentType[] { HarmonyLib.ArgumentType.Ref, HarmonyLib.ArgumentType.Normal, HarmonyLib.ArgumentType.Normal, HarmonyLib.ArgumentType.Normal, HarmonyLib.ArgumentType.Normal})]
        private static void SetSFXGameplay(ref VoicePriority currentPriority, Characters character, AudioClipID audioClipID, AudioSource audioSource, VoicePriority playbackPriority) {
            if (WorldHandler.instance?.GetCurrentPlayer() == null) { return; }
            if (audioSource != WorldHandler.instance?.GetCurrentPlayer().playerGameplayVoicesAudioSource) { return; }
            if (GhostRecorder.Instance != null && GhostRecorder.Instance.Recording) {
                if (GhostRecorder.Instance.LastFrame == null) { return; }
                
                GhostRecorder.Instance.LastFrame.SFX.Add(new GhostFrame.GhostFrameSFX() {
                    AudioClipID = audioClipID,
                    Voice = true
                });
            }
        }
    }

    [HarmonyPatch(typeof(Player))]
    internal class GhostPlayerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.PlayVoice))]
        private static bool PlayVoice_Prefix(Player __instance)
        {
            if (__instance == GhostPlayer.ghostPlayerCharacter)
                return false; // Skip method entirely for ghost AI
            return true; // Allow original method for normal players
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "InitHitboxes")]
        private static bool InitHitboxes_Prefix(Player __instance)
        {
            if (__instance == GhostPlayer.ghostPlayerCharacter)
                return false; // Skip method entirely for ghost AI
            return true; // Allow original method for normal players
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnTriggerStay))]
        public class Patch_Player_OnTriggerStay
        {
            [HarmonyPrefix]
            public static bool OnTriggerStay_Prefix(Collider other, Player __instance)
            {
                // Do Not Run for Ghost AI
                if (__instance != GhostPlayer.ghostPlayerCharacter)
                    return true;

                return false;
            }
        }

        // DO NOT RUN player update methods if GhostPlayer
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.FixedUpdatePlayer))]
        [HarmonyPatch(nameof(Player.UpdatePlayer))]
        private static bool FixedUpdate_Prefix(Player __instance)
        {
            return __instance != GhostPlayer.ghostPlayerCharacter;
        }


        // Ghost Recording Patches 
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.PlayAnim))]
        private static void SetFrameAnim(int newAnim, bool forceOverwrite, bool instant, float atTime, Player __instance) 
        {
            if (__instance != WorldHandler.instance?.GetCurrentPlayer()) { return; }
            if (GhostRecorder.Instance != null && GhostRecorder.Instance.Recording) {
                if (GhostRecorder.Instance.LastFrame == null) { return; }
                //GhostRecorder.LastFrame.Animation.ID = newAnim;
                //GhostRecorder.LastFrame.Animation.Time = atTime; //p.anim.playbackTime;
                GhostRecorder.Instance.LastFrame.Animation.Instant = instant;
                GhostRecorder.Instance.LastFrame.Animation.AtTime = atTime; 
                GhostRecorder.Instance.LastFrame.Animation.ForceOverwrite = forceOverwrite;
            }
        } 

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.DoJumpEffects))]
        private static void SetFrameJumpEffects(Vector3 dir, Player __instance) {
            if (__instance != WorldHandler.instance?.GetCurrentPlayer()) { return; }
            if (GhostRecorder.Instance != null && GhostRecorder.Instance.Recording) {
                if (GhostRecorder.Instance.LastFrame == null) { return; }
                GhostRecorder.Instance.LastFrame.Effects.DoJumpEffects = true;
                GhostRecorder.Instance.LastFrame.Effects.JumpEffects = dir; 
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.DoHighJumpEffects))]
        private static void SetFrameHighJumpEffects(Vector3 dir, Player __instance) {
            if (__instance != WorldHandler.instance?.GetCurrentPlayer()) { return; }
            if (GhostRecorder.Instance != null && GhostRecorder.Instance.Recording) {
                if (GhostRecorder.Instance.LastFrame == null) { return; }
                GhostRecorder.Instance.LastFrame.Effects.DoHighJumpEffects = true;
                GhostRecorder.Instance.LastFrame.Effects.HighJumpEffects = dir; 
            }
        }

        // Spraycan fix
        [HarmonyPatch(typeof(Player), nameof(Player.StopHoldSpraycan))]
        internal class Player_StopHoldSpraycan_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Player __instance)
            {
                // Skip StopHoldSpraycan for the ghost player
                if (__instance == GhostPlayer.ghostPlayerCharacter)
                    return false;

                return true;
            }
        }

    }
}