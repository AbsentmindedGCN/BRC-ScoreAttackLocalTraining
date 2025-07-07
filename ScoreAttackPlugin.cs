using BepInEx;
using HarmonyLib;
using ScoreAttackGhostSystem;
using System.IO;
using UnityEngine;

namespace ScoreAttack
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(CommonAPIGUID, BepInDependency.DependencyFlags.HardDependency)]
    public class ScoreAttackPlugin : BaseUnityPlugin
    {
        private const string CommonAPIGUID = "CommonAPI";
        public static ScoreAttackPlugin Instance { get; private set; }
        public string Directory => Path.GetDirectoryName(Info.Location);

        public static readonly float ghostTickInterval = 1f / 60f;
        private float ghostTickTimer = 0f;
        private float _currentGhostTime = 0f;

        private void Awake()
        {
            Instance = this;
            // Create the singleton for our custom save data.
            new ScoreAttackSaveData();
            new GhostSaveData();
            AppScoreAttackOffline.Initialize();
            AppScoreAttackStageSelect.Initialize();
            AppScoreAttack.Initialize();
            AppFPSLimit.Initialize();
            ScoreAttackManager.Initialize();
            AppMoveStyle.Initialize();
            AppGrindDebt.Initialize();
            AppGhost.Initialize();
            AppGhostSettings.Initialize();
            AppExtras.Initialize();
            AppGhostList.Initialize();
            AppGhostDelete.Initialize();
            //AppPolice.Initialize(); - No longer needed

            // Create Ghost Manager
            GhostManager.Create();

            // Patch Player, Phone, and NPCs so cops can spawn during battles, taxi appears, and more!
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }


        private void Update()
        {
            // Update ScoreAttackEncounter while in Graffiti Mode
            if (Patches.StartGraffitiModePatch.inGraffitiMode && ScoreAttackManager.Encounter != null)
            {
                if (ScoreAttackManager.Encounter.currentlyActive)
                {
                    ScoreAttackManager.Encounter.UpdateMainEvent();
                }
            }

            ghostTickTimer += Time.deltaTime;
            while (ghostTickTimer >= ghostTickInterval)
            {
                ghostTickTimer -= ghostTickInterval;
                _currentGhostTime += ghostTickInterval;

                try { AdvanceGhostTick(); } catch (System.Exception) {}
            }
            
            try { SmoothGhostMovement(ghostTickTimer); } catch (System.Exception) {}
        }

        private void AdvanceGhostTick()
        {
            ScoreAttackGhostSystem.GhostManager.Instance?.OnFixedUpdate();

            var ghostPlayer = ScoreAttackGhostSystem.GhostManager.Instance?.GhostPlayer;
            var replay = ghostPlayer?.Replay;
            if (ghostPlayer == null || replay == null || !ghostPlayer.Active) { return; }

            int frameIndex = Mathf.FloorToInt(_currentGhostTime / ghostTickInterval);
            if (frameIndex >= replay.FrameCount) { return; } // End or loop playback
            ghostPlayer.ApplyFrameToWorld(replay.Frames[frameIndex], false);
        }

        private void SmoothGhostMovement(float ghostTimeGap)
        {
            var ghostPlayer = ScoreAttackGhostSystem.GhostManager.Instance?.GhostPlayer;
            var replay = ghostPlayer?.Replay;
            if (ghostPlayer == null || replay == null || !ghostPlayer.Active) { return; }

            int frameIndex = Mathf.FloorToInt(_currentGhostTime / ghostTickInterval);
            int nextFrameIndex = frameIndex + 1;

            if (frameIndex >= replay.FrameCount) { // End or loop playback
                if (!ghostPlayer.ReplayEnded) { ghostPlayer.EndReplay(); }
                return; 
            } 

            var frameA = replay.Frames[frameIndex];
            var frameB = nextFrameIndex < replay.FrameCount ? replay.Frames[nextFrameIndex] : frameA;

            float t = Mathf.Clamp01(ghostTimeGap / ghostTickInterval); //(_currentGhostTime % ghostTickInterval) / ghostTickInterval;
            if (t == 0) { return; }

            Vector3 interpPos = Vector3.Lerp(frameA.PlayerPosition, frameB.PlayerPosition, t);
            Quaternion interpRot = Quaternion.Lerp(frameA.PlayerRotation, frameB.PlayerRotation, t);
            Vector3 interpPosVisual = Vector3.Lerp(frameA.Visual.Position, frameB.Visual.Position, t);
            Quaternion interpRotVisual = Quaternion.Lerp(frameA.Visual.Rotation, frameB.Visual.Rotation, t);

            ghostPlayer.ApplyInterpolationToWorld(false, interpPos, interpRot, interpPosVisual, interpRotVisual);
        }

        public void ResetCurrentTime()
        {
            _currentGhostTime = 0f;
            //ghostTickTimer = 0f;
        }
    }
}