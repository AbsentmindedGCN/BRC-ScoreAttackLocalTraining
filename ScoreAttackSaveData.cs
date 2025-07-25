﻿using CommonAPI;
using Reptile;
using ScoreAttackGhostSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScoreAttack
{
    public class ScoreAttackSaveData : CustomSaveData
    {
        public static ScoreAttackSaveData Instance { get; private set; }
        private readonly Dictionary<Stage, RespawnPoint> respawnPoints = [];

        //private readonly Dictionary<Stage, float> personalBests = new Dictionary<Stage, float>();
        public Dictionary<Stage, PersonalBest> PersonalBestByStage = [];

        // Ghosts
        public Dictionary<Stage, GhostData> BestGhostsByStage = [];

        // Grind Debt Timer Visible
        public bool GrindDebtTimerVisible { get; set; }

        // Grind Debt Display Mode
        public GrindDebtDisplayMode GrindDebtViewerMode = GrindDebtDisplayMode.Disabled; // Default to Disabled
        public GrindDebtColorMode GrindDebtColorMode = GrindDebtColorMode.Flashing; // Default to Flashing

        // Extra Modes
        public SFXToggle ExtraSFXMode = SFXToggle.Default; // Default to no custom SFX

        public class PersonalBest
        {
            public Dictionary<float, float> PersonalBestByTimeLimit = [];

            public float GetPersonalBest(float timeLimit)
            {
                if (PersonalBestByTimeLimit.TryGetValue(timeLimit, out var result))
                    return result;
                return -1f;
            }

            public void SetPersonalBest(float timeLimit, float best)
            {
                PersonalBestByTimeLimit[timeLimit] = best;
            }
        }
        public class GhostData
        {
            public Dictionary<float, Ghost> GhostByTimeLimit = new Dictionary<float, Ghost>();

            public Ghost GetGhost(float timeLimit)
            {
                if (GhostByTimeLimit.TryGetValue(timeLimit, out var result))
                    return result;
                return null;
            }

            public void SetGhost(float timeLimit, Ghost ghost)
            {
                GhostByTimeLimit[timeLimit] = ghost;
            }
        }

        public PersonalBest GetOrCreatePersonalBest(Stage stage)
        {
            if (PersonalBestByStage.TryGetValue(stage, out var personalBest))
            { return personalBest; }
            personalBest = new PersonalBest();
            PersonalBestByStage[stage] = personalBest;
            return personalBest;
        }

        // Get or create Ghost data for a stage
        public GhostData GetOrCreateGhostData(Stage stage) //, float fps)
        {
            if (BestGhostsByStage.TryGetValue(stage, out var ghostData))
            { return ghostData; }
            ghostData = new GhostData();
            BestGhostsByStage[stage] = ghostData;
            return ghostData;
        }

        // Property to store and retrieve target frame rate
        public int TargetFrameRate { get; set; }

        public ScoreAttackSaveData() : base(PluginInfo.PLUGIN_NAME, "{0}.data")
        {
            Instance = this;
        }

        public void SetRespawnPoint(Stage stage, Vector3 position, Quaternion rotation, bool gear)
        {
            var respawnPoint = new RespawnPoint(position, rotation, gear);
            respawnPoints[stage] = respawnPoint;
            // Custom save data gets saved alongside the base savedata, so it makes sense to call this right after changing custom data.
            Core.Instance.SaveManager.SaveCurrentSaveSlot();
        }

        public RespawnPoint GetRespawnPoint(Stage stage)
        {
            if (respawnPoints.TryGetValue(stage, out var respawnPoint)) return respawnPoint;
            return null;
        }

        // Starting a new save - start from zero.
        public override void Initialize()
        {
            respawnPoints.Clear();
        }

        public override void Read(BinaryReader reader)
        {
            var version = reader.ReadByte();
            var respawns = reader.ReadInt32();
            for(var i = 0; i < respawns; i++)
            {
                var stage = (Stage)reader.ReadInt32();

                var positionX = reader.ReadSingle();
                var positionY = reader.ReadSingle();
                var positionZ = reader.ReadSingle();

                var rotationX = reader.ReadSingle();
                var rotationY = reader.ReadSingle();
                var rotationZ = reader.ReadSingle();
                var rotationW = reader.ReadSingle();

                var gear = reader.ReadBoolean();

                var position = new Vector3(positionX, positionY, positionZ);
                var rotation = new Quaternion(rotationX, rotationY, rotationZ, rotationW);

                var respawnPoint = new RespawnPoint(position, rotation, gear);
                respawnPoints[stage] = respawnPoint;
            }

            // Read personal bests
            var personalBestCount = reader.ReadInt32();
            for (int i = 0; i < personalBestCount; i++)
            {
                var stage = (Stage)reader.ReadInt32();
                var personalBest = new PersonalBest();
                var timeLimitCount = reader.ReadInt32();
                for (int j = 0; j < timeLimitCount; j++)
                {
                    var timeLimit = reader.ReadSingle();
                    var best = reader.ReadSingle();
                    personalBest.SetPersonalBest(timeLimit, best);
                }
                PersonalBestByStage[stage] = personalBest;
            }

            // Read ghosts
            var ghostCount = reader.ReadInt32();
            for (int i = 0; i < ghostCount; i++)
            {
                var stage = (Stage)reader.ReadInt32();
                var ghostData = new GhostData();
                var timeLimitCount = reader.ReadInt32();
                for (int j = 0; j < timeLimitCount; j++)
                {
                    var timeLimit = reader.ReadSingle();
                    //var ghost = reader.ReadSingle();
                    var ghost = new Ghost(240f); // Default to 60 FPS, changed to 240
                    //ghost.ReadGhostData(reader);
                    ghostData.SetGhost(timeLimit, ghost);
                    //ghostData.SetGhost(timeLimit, best);
                }
                BestGhostsByStage[stage] = ghostData;
            }

            // Read and set target frame rate
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                TargetFrameRate = reader.ReadInt32();
                if (TargetFrameRate != -1) // Don't set frame rate to -1 (unlimited) as it causes issues
                {
                    Application.targetFrameRate = TargetFrameRate;
                }
            }
            else
            {
                // Set a default frame rate if TargetFrameRate is not present in the save data
                TargetFrameRate = 60; // Set to default frame rate (60 fps)
                Application.targetFrameRate = TargetFrameRate;
            }

            // Read GrindDebtTimerVisible if it exists
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                GrindDebtTimerVisible = reader.ReadBoolean();
            }
            else
            {
                GrindDebtTimerVisible = false; // default
            }

            // Read GrindDebtViewerMode (Default to Disabled if not found)
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                GrindDebtViewerMode = (GrindDebtDisplayMode)reader.ReadInt32();
            }
            else
            {
                GrindDebtViewerMode = GrindDebtDisplayMode.Disabled; // default
            }

            // Read GrindDebtColorMode (Default to Flashing if not found)
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                GrindDebtColorMode = (GrindDebtColorMode)reader.ReadInt32();
            }
            else
            {
                GrindDebtColorMode = GrindDebtColorMode.Flashing; // default
            }

            // Read ExtraSFXMode (Default to No SFX if not found)
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                ExtraSFXMode = (SFXToggle)reader.ReadInt32();
            }
            else
            {
                ExtraSFXMode = SFXToggle.Default; // default
            }

        }

        public override void Write(BinaryWriter writer)
        {
            // Version
            writer.Write((byte)0);
            writer.Write(respawnPoints.Count);
            foreach(var respawnPair in respawnPoints)
            {
                writer.Write((int)respawnPair.Key);
                var respawnPoint = respawnPair.Value;

                writer.Write(respawnPoint.Position.x);
                writer.Write(respawnPoint.Position.y);
                writer.Write(respawnPoint.Position.z);

                writer.Write(respawnPoint.Rotation.x);
                writer.Write(respawnPoint.Rotation.y);
                writer.Write(respawnPoint.Rotation.z);
                writer.Write(respawnPoint.Rotation.w);

                writer.Write(respawnPoint.Gear);
            }

            // Write personal bests
            writer.Write(PersonalBestByStage.Count);
            foreach (var personalBestPair in PersonalBestByStage)
            {
                writer.Write((int)personalBestPair.Key);
                var personalBest = personalBestPair.Value;

                writer.Write(personalBest.PersonalBestByTimeLimit.Count);
                foreach (var timeLimitPair in personalBest.PersonalBestByTimeLimit)
                {
                    writer.Write(timeLimitPair.Key);
                    writer.Write(timeLimitPair.Value);
                }
            }

            // Write ghosts
            writer.Write(BestGhostsByStage.Count);
            foreach (var ghostPair in BestGhostsByStage)
            {
                writer.Write((int)ghostPair.Key); // Write stage ID
                var ghostData = ghostPair.Value;
                writer.Write(ghostData.GhostByTimeLimit.Count); // Write count of ghosts

                foreach (var ghostTimePair in ghostData.GhostByTimeLimit)
                {
                    writer.Write(ghostTimePair.Key); // Write time limit
                    //ghostTimePair.Value.WriteGhostData(writer);
                }
            }

            // Save current frame rate
            writer.Write(TargetFrameRate);

            // Save user Grind Debt Display preference
            writer.Write(GrindDebtTimerVisible);

            // Save GrindDebtViewerMode
            writer.Write((int)GrindDebtViewerMode);

            // Save GrindDebtColorMode
            writer.Write((int)GrindDebtColorMode);

            // Save Extra Toggles
            writer.Write((int)ExtraSFXMode);
        }
    }
}
