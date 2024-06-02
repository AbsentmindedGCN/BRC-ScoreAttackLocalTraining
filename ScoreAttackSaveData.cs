using CommonAPI;
using Reptile;
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

        public PersonalBest GetOrCreatePersonalBest(Stage stage)
        {
            if (PersonalBestByStage.TryGetValue(stage, out var personalBest))
            { return personalBest; }
            personalBest = new PersonalBest();
            PersonalBestByStage[stage] = personalBest;
            return personalBest;
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

            // Save current frame rate
            writer.Write(TargetFrameRate);
        }
    }
}
