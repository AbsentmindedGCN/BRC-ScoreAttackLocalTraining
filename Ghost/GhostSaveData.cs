using CommonAPI;
using Reptile;
using ScoreAttackGhostSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScoreAttack
{
    public class GhostSaveData : CustomSaveData
    {
        public static GhostSaveData Instance { get; private set; }

        // REMEMBER TO UPDATE THIS - fallback for ReadSingleGhost() 
        // affects both GhostSaveData and ExportedGhostData now
        public static readonly int currentSaveVersion = 4;

        public Dictionary<Stage, GhostData> BestGhostsByStage = new();

        public GhostSaveMode GhostSaveMode = GhostSaveMode.Enabled;
        public GhostDisplayMode GhostDisplayMode = GhostDisplayMode.Show;
        public GhostSoundMode GhostSoundMode = GhostSoundMode.On;
        public GhostModel GhostModel = GhostModel.Self;
        public GhostEffect GhostEffect = GhostEffect.Transparent;
        public GhostWarpMode GhostWarpMode = GhostWarpMode.ToGhost;
        public GhostScoreMode GhostScoreMode = GhostScoreMode.Final;

        public bool CopiedPackagedGhosts = false;

        public class GhostData
        {
            public Dictionary<float, Ghost> GhostByTimeLimit = new();

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

        public GhostData GetOrCreateGhostData(Stage stage)
        {
            if (BestGhostsByStage.TryGetValue(stage, out var ghostData))
                return ghostData;

            ghostData = new GhostData();
            BestGhostsByStage[stage] = ghostData;
            return ghostData;
        }

        public class ExportedGhostData
        {
            public Ghost Ghost;
            public Stage Stage;
            public float TimeLimit;
            public float GhostScore;

            public ExportedGhostData(Ghost ghost, Stage stage, float timeLimit, float ghostScore)
            {
                Ghost = ghost;
                Stage = stage;
                TimeLimit = timeLimit;
                GhostScore = ghostScore;
            }
        }

        public GhostSaveData() : base("GhostSaveData", "{0}_ghosts.data")
        {
            Instance = this;
        }

        public override void Initialize()
        {
            BestGhostsByStage.Clear();
            CopyPackagedGhostsToDocuments();
            CopiedPackagedGhosts = true;
        }

        public override void Read(BinaryReader reader2)
        {
            BestGhostsByStage.Clear();

            GZipStream dcmp = new GZipStream(reader2.BaseStream, CompressionMode.Decompress);
            BinaryReader reader = new BinaryReader(dcmp);

            var version = reader.ReadByte();
            if (version >= 3) { CopiedPackagedGhosts = reader.ReadBoolean(); }

            if (version == 0)
            { // Legacy version
                var ghostCount = reader.ReadInt32();
                for (int i = 0; i < ghostCount; i++)
                {
                    var stage = (Stage)reader.ReadInt32();
                    var ghostData = new GhostData();
                    var timeLimitCount = reader.ReadInt32();
                    for (int j = 0; j < timeLimitCount; j++)
                    {
                        var timeLimit = reader.ReadSingle();
                        var ghost = new Ghost(60f); // FPS
                        ghostData.SetGhost(timeLimit, ghost);
                    }
                    BestGhostsByStage[stage] = ghostData;
                }
            }
            else if (version == 1)
            {

                var ghostCount = reader.ReadInt32();
                for (int i = 0; i < ghostCount; i++)
                {
                    var stage = (Stage)reader.ReadInt32();
                    var ghostData = new GhostData();
                    var timeLimitCount = reader.ReadInt32();
                    for (int j = 0; j < timeLimitCount; j++)
                    {
                        var timeLimit = reader.ReadSingle();
                        var ghost = ReadSingleGhost(reader, version);
                        ghostData.SetGhost(timeLimit, ghost);
                    }
                    BestGhostsByStage[stage] = ghostData;
                }
            }
            else if (version == 2)
            {

                // Read settings
                GhostSaveMode = (GhostSaveMode)reader.ReadInt32();
                GhostDisplayMode = (GhostDisplayMode)reader.ReadInt32();
                GhostSoundMode = (GhostSoundMode)reader.ReadInt32();
                GhostModel = (GhostModel)reader.ReadInt32();
                GhostEffect = (GhostEffect)reader.ReadInt32();

                var ghostCount = reader.ReadInt32();
                for (int i = 0; i < ghostCount; i++)
                {
                    var stage = (Stage)reader.ReadInt32();
                    var ghostData = new GhostData();
                    var timeLimitCount = reader.ReadInt32();
                    for (int j = 0; j < timeLimitCount; j++)
                    {
                        var timeLimit = reader.ReadSingle();
                        var ghost = ReadSingleGhost(reader, version);
                        ghostData.SetGhost(timeLimit, ghost);
                    }
                    BestGhostsByStage[stage] = ghostData;
                }
            }
            else if (version == 3)
            {

                // Read settings
                GhostSaveMode = (GhostSaveMode)reader.ReadInt32();
                GhostDisplayMode = (GhostDisplayMode)reader.ReadInt32();
                GhostSoundMode = (GhostSoundMode)reader.ReadInt32();
                GhostModel = (GhostModel)reader.ReadInt32();
                GhostEffect = (GhostEffect)reader.ReadInt32();
                GhostWarpMode = (GhostWarpMode)reader.ReadInt32();

                var ghostCount = reader.ReadInt32();
                for (int i = 0; i < ghostCount; i++)
                {
                    var stage = (Stage)reader.ReadInt32();
                    var ghostData = new GhostData();
                    var timeLimitCount = reader.ReadInt32();
                    for (int j = 0; j < timeLimitCount; j++)
                    {
                        var timeLimit = reader.ReadSingle();
                        var ghost = ReadSingleGhost(reader, version);
                        ghostData.SetGhost(timeLimit, ghost);
                    }
                    BestGhostsByStage[stage] = ghostData;
                }
            }
            else if (version == 4)
            {

                // Read settings
                GhostSaveMode = (GhostSaveMode)reader.ReadInt32();
                GhostDisplayMode = (GhostDisplayMode)reader.ReadInt32();
                GhostSoundMode = (GhostSoundMode)reader.ReadInt32();
                GhostModel = (GhostModel)reader.ReadInt32();
                GhostEffect = (GhostEffect)reader.ReadInt32();
                GhostWarpMode = (GhostWarpMode)reader.ReadInt32();
                GhostScoreMode = (GhostScoreMode)reader.ReadInt32();

                var ghostCount = reader.ReadInt32();
                for (int i = 0; i < ghostCount; i++)
                {
                    var stage = (Stage)reader.ReadInt32();
                    var ghostData = new GhostData();
                    var timeLimitCount = reader.ReadInt32();
                    for (int j = 0; j < timeLimitCount; j++)
                    {
                        var timeLimit = reader.ReadSingle();
                        var ghost = ReadSingleGhost(reader, version);
                        ghostData.SetGhost(timeLimit, ghost);
                    }
                    BestGhostsByStage[stage] = ghostData;
                }
            }


            reader.Close();

            if (!CopiedPackagedGhosts)
            {
                CopyPackagedGhostsToDocuments();
                CopiedPackagedGhosts = true;
            }
        }

        public override void Write(BinaryWriter writer2)
        {
            GZipStream cmp = new GZipStream(writer2.BaseStream, CompressionMode.Compress);
            BinaryWriter writer = new BinaryWriter(cmp);

            writer.Write((byte)currentSaveVersion); // save file version
            writer.Write((bool)CopiedPackagedGhosts);

            //Ghost Settings for App
            writer.Write((int)GhostSaveMode);
            writer.Write((int)GhostDisplayMode);
            writer.Write((int)GhostSoundMode);
            writer.Write((int)GhostModel);
            writer.Write((int)GhostEffect);
            writer.Write((int)GhostWarpMode);
            writer.Write((int)GhostScoreMode);

            // Write Ghost data
            writer.Write(BestGhostsByStage.Count);
            foreach (var ghostPair in BestGhostsByStage)
            {
                writer.Write((int)ghostPair.Key); // Stage ID
                var ghostData = ghostPair.Value;
                writer.Write(ghostData.GhostByTimeLimit.Count);

                foreach (var ghostTimePair in ghostData.GhostByTimeLimit)
                {
                    writer.Write(ghostTimePair.Key); // Time limit
                    WriteSingleGhost(writer, ghostTimePair.Value);
                }
            }

            writer.Flush();
            writer.Close();
        }

        // EXPECTS COMPRESSION WRITER!!!!!!!!!!!
        public void WriteExportedGhost(BinaryWriter writer, Ghost ghost, Stage stage, float timeLimit, float score)
        {
            writer.Write((byte)currentSaveVersion); // export version
            writer.Write((int)stage);
            writer.Write((Single)timeLimit);
            writer.Write((Single)score);
            WriteSingleGhost(writer, ghost);
        }

        // EXPECTS COMPRESSION WRITER!!!!!!!!!!!
        public ExportedGhostData ReadExportedGhost(BinaryReader reader)
        {
            var version = reader.ReadByte();
            Stage stage = (Stage)reader.ReadInt32();
            float timeLimit = (float)reader.ReadSingle();
            float score = (float)reader.ReadSingle();

            int readGhostVersion = version > 2 ? currentSaveVersion : 3;
            Ghost ghost = ReadSingleGhost(reader, readGhostVersion);
            return new ExportedGhostData(ghost, stage, timeLimit, score);
        }

        private void WriteSingleGhost(BinaryWriter writer, Ghost ghost)
        {
            writer.Write((Single)ghost.TickDelta);
            writer.Write((int)ghost.Character);
            writer.Write((string)ghost.CharacterGUID.ToString());
            writer.Write((int)ghost.Outfit);
            writer.Write((Int32)ghost.Frames.Count);
            foreach (var frame in ghost.Frames)
            {
                writer.Write((Int32)frame.FrameIndex);
                writer.Write((bool)frame.Valid);

                writer.Write((Single)frame.PlayerPosition.x);
                writer.Write((Single)frame.PlayerPosition.y);
                writer.Write((Single)frame.PlayerPosition.z);

                writer.Write((Single)frame.PlayerRotation.x);
                writer.Write((Single)frame.PlayerRotation.y);
                writer.Write((Single)frame.PlayerRotation.z);
                writer.Write((Single)frame.PlayerRotation.w);

                writer.Write((Single)frame.Velocity.x);
                writer.Write((Single)frame.Velocity.y);
                writer.Write((Single)frame.Velocity.z);

                writer.Write((Int32)frame.PhoneState);
                writer.Write((Int32)frame.SpraycanState);

                writer.Write((Int32)frame.moveStyle);
                writer.Write((Int32)frame.equippedMoveStyle);
                writer.Write((bool)frame.UsingEquippedMoveStyle);

                writer.Write((Int32)frame.Animation.ID);
                writer.Write((Single)frame.Animation.Time);
                writer.Write((bool)frame.Animation.ForceOverwrite);
                writer.Write((bool)frame.Animation.Instant);
                writer.Write((Single)frame.Animation.AtTime);

                writer.Write((Single)frame.Visual.Position.x);
                writer.Write((Single)frame.Visual.Position.y);
                writer.Write((Single)frame.Visual.Position.z);

                writer.Write((Single)frame.Visual.Rotation.x);
                writer.Write((Single)frame.Visual.Rotation.y);
                writer.Write((Single)frame.Visual.Rotation.z);
                writer.Write((Single)frame.Visual.Rotation.w);

                writer.Write((Int32)frame.Visual.boostpackEffectMode);
                writer.Write((Int32)frame.Visual.frictionEffectMode);
                writer.Write((Int32)frame.Visual.dustEmission);
                writer.Write((float)frame.Visual.dustSize);
                writer.Write((Int32)frame.Visual.spraypaintEmission);
                writer.Write((Int32)frame.Visual.ringEmission);

                writer.Write((Int32)frame.Effects.ringParticles);
                writer.Write((Int32)frame.Effects.spraypaintParticles);

                writer.Write((bool)frame.Effects.DoJumpEffects);
                writer.Write((bool)frame.Effects.DoHighJumpEffects);

                writer.Write((Single)frame.Effects.JumpEffects.x);
                writer.Write((Single)frame.Effects.JumpEffects.y);
                writer.Write((Single)frame.Effects.JumpEffects.z);

                writer.Write((Single)frame.Effects.HighJumpEffects.x);
                writer.Write((Single)frame.Effects.HighJumpEffects.y);
                writer.Write((Single)frame.Effects.HighJumpEffects.z);

                writer.Write((Int32)frame.SFX.Count);
                foreach (GhostFrame.GhostFrameSFX frameSFX in frame.SFX)
                {
                    writer.Write((Int32)frameSFX.AudioClipID);
                    writer.Write((Int32)frameSFX.CollectionID);
                    writer.Write((Single)frameSFX.RandomPitchVariance);
                    writer.Write((bool)frameSFX.Voice);
                }

                writer.Write((float)frame.BaseScore);
                writer.Write((float)frame.ScoreMultiplier);
                writer.Write((float)frame.OngoingScore);

            }

            writer.Write((Single)ghost.Score);
        }

        private Ghost ReadSingleGhost(BinaryReader reader, int version = -1)
        {
            if (version == -1) { version = currentSaveVersion; }

            Ghost ghost = new Ghost(reader.ReadSingle());
            ghost.Character = reader.ReadInt32();
            ghost.CharacterGUID = new Guid(reader.ReadString());
            ghost.Outfit = reader.ReadInt32();

            int frameCount = reader.ReadInt32();
            for (int i = 0; i < frameCount; i++)
            {
                GhostFrame frame = new GhostFrame(ghost)
                {
                    FrameIndex = reader.ReadInt32(),
                    Valid = reader.ReadBoolean(),

                    PlayerPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                    //PlayerRotation = ShuffledQuaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                    PlayerRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                    Velocity = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),

                    PhoneState = (Reptile.Phone.Phone.PhoneState)reader.ReadInt32(),
                    SpraycanState = (Reptile.Player.SpraycanState)reader.ReadInt32(),

                    moveStyle = (Reptile.MoveStyle)reader.ReadInt32(),
                    equippedMoveStyle = (Reptile.MoveStyle)reader.ReadInt32(),
                    UsingEquippedMoveStyle = reader.ReadBoolean()
                };

                frame.Animation.ID = reader.ReadInt32();
                frame.Animation.Time = reader.ReadSingle();
                frame.Animation.ForceOverwrite = reader.ReadBoolean();
                frame.Animation.Instant = reader.ReadBoolean();
                frame.Animation.AtTime = reader.ReadSingle();

                frame.Visual.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                frame.Visual.Rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                frame.Visual.boostpackEffectMode = (Reptile.BoostpackEffectMode)reader.ReadInt32();
                frame.Visual.frictionEffectMode = (Reptile.FrictionEffectMode)reader.ReadInt32();
                frame.Visual.dustEmission = reader.ReadInt32();
                frame.Visual.dustSize = reader.ReadSingle();
                frame.Visual.spraypaintEmission = reader.ReadInt32();
                frame.Visual.ringEmission = reader.ReadInt32();

                frame.Effects.ringParticles = reader.ReadInt32();
                frame.Effects.spraypaintParticles = reader.ReadInt32();

                frame.Effects.DoJumpEffects = reader.ReadBoolean();
                frame.Effects.DoHighJumpEffects = reader.ReadBoolean();

                frame.Effects.JumpEffects = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                frame.Effects.HighJumpEffects = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                int sfxCount = reader.ReadInt32();
                for (int c = 0; c < sfxCount; c++)
                {
                    frame.SFX.Add(new GhostFrame.GhostFrameSFX()
                    {
                        AudioClipID = (Reptile.AudioClipID)reader.ReadInt32(),
                        CollectionID = (Reptile.SfxCollectionID)reader.ReadInt32(),
                        RandomPitchVariance = reader.ReadSingle(),
                        Voice = reader.ReadBoolean()
                    });
                }

                if (version > 3)
                {
                    frame.BaseScore = reader.ReadSingle();
                    frame.ScoreMultiplier = reader.ReadSingle();
                    frame.OngoingScore = reader.ReadSingle();
                }

                ghost.Frames.Add(frame);
            }

            ghost.Score = reader.ReadSingle();
            return ghost;
        }

        public void SaveGhostToFile(Ghost ghost, Stage stage, float timeLimit, float score)
        {
            try
            {
                string baseFolderPath = GetSaveLocation();
                string folderPath = Path.Combine(baseFolderPath, "GhostSaveData");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                int timeMinutes = Mathf.RoundToInt(timeLimit / 60f);
                //string fileName = $"{stage.ToString().ToLower()}{timeMinutes}min-{score}-{timestamp}.ghost";
                //string stageName = GetStageFileName(stage);
                string stageName = GetCleanStageName(stage);
                //string fileName = $"{stageName}-{timeMinutes}min-{score}-{timestamp}.ghost"; //This causes issues because float limit, eg. 11,272,190 is 1.127219E+07
                string fileName = $"{stageName}-{timeMinutes}min-{score.ToString("F0")}-{timestamp}.ghost";

                string fullPath = Path.Combine(folderPath, fileName);

                using (FileStream fileStream = new FileStream(fullPath, FileMode.Create))
                using (BinaryWriter writer = new BinaryWriter(new GZipStream(fileStream, CompressionMode.Compress)))
                {
                    WriteExportedGhost(writer, ghost, stage, timeLimit, score);
                    writer.Flush();
                }

                Debug.Log($"[ScoreAttack] External ghost saved to: {fullPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScoreAttack] Failed to save external ghost: {ex}");
            }
        }

        public Ghost LoadGhostFromFile(string fullPath, out float timeLimit, out float score, out DateTime timestamp)
        {
            timeLimit = 0f;
            score = 0f;
            timestamp = DateTime.MinValue;

            try
            {
                var fileName = Path.GetFileNameWithoutExtension(fullPath);
                // Expected format: {stage}{timeLimit}min-{score}-{timestamp}.ghost
                // e.g. square3min-19593380-20250704122851.ghost

                // Split by '-'
                var parts = fileName.Split('-');
                if (parts.Length != 3) return null;

                // Parse timeLimit from part 0 (remove stage name and "min")
                // E.g. "square3min" -> "3"
                string stageAndTime = parts[0];
                int minIndex = stageAndTime.IndexOf("min");
                if (minIndex < 0) return null;
                string timeStr = stageAndTime.Substring(stageAndTime.Length - (minIndex + 3), minIndex);

                // More reliable way: extract digits before "min"
                timeStr = new string(stageAndTime.Where(char.IsDigit).ToArray());
                if (!int.TryParse(timeStr, out int timeMinutes)) return null;
                timeLimit = timeMinutes * 60f;

                // Parse score
                if (!float.TryParse(parts[1], out score)) return null;

                // Parse timestamp
                if (!DateTime.TryParseExact(parts[2], "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out timestamp))
                    return null;

                // Read ghost from file
                using var fs = new FileStream(fullPath, FileMode.Open);
                using var br = new BinaryReader(new GZipStream(fs, CompressionMode.Decompress));
                return ReadExportedGhost(br).Ghost;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScoreAttack] Failed to load ghost file {fullPath}: {ex}");
                return null;
            }
        }

        public void CopyPackagedGhostsToDocuments()
        {
            try
            {
                string copyDirectory = Path.Combine(ScoreAttackPlugin.Instance.Directory, "ghost");
                string baseFolderPath = GetSaveLocation();
                string pasteDirectory = Path.Combine(baseFolderPath, "GhostSaveData");

                // Ensure destination folder exists
                if (!Directory.Exists(pasteDirectory))
                {
                    Directory.CreateDirectory(pasteDirectory);
                    Debug.Log($"[ScoreAttack] Created destination directory: {pasteDirectory}");
                }

                // If source folder doesn't exist, optionally create it and exit early
                if (!Directory.Exists(copyDirectory))
                {
                    Directory.CreateDirectory(copyDirectory);
                    Debug.LogWarning($"[ScoreAttack] Ghost source directory not found, created empty directory: {copyDirectory}");
                    return;
                }

                int copiedCount = 0;
                foreach (var file in Directory.GetFiles(copyDirectory, "*.ghost"))
                {
                    string destinationFile = Path.Combine(pasteDirectory, Path.GetFileName(file));
                    if (!File.Exists(destinationFile))
                    {
                        File.Copy(file, destinationFile);
                        copiedCount++;
                    }
                }

                Debug.Log($"[ScoreAttack] Successfully copied {copiedCount} packaged ghost file(s) to: {pasteDirectory}");

                // Only set flag if the operation completed without exception
                CopiedPackagedGhosts = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScoreAttack] Failed to copy packaged ghosts: {ex}");
            }
        }

        private static string GetCleanStageName(Stage stage)
        {
            return stage switch
            {
                Stage.hideout => "hideout",
                Stage.downhill => "versum_hill",
                Stage.square => "millennium_square",
                Stage.tower => "brink_terminal",
                Stage.Mall => "millennium_mall",
                Stage.osaka => "mataan",
                Stage.pyramid => "pyramid_island",
                Stage.Prelude => "police_station",
                _ => stage.ToString().ToLowerInvariant() // fallback
            };
        }

        public string GetSaveLocation()
        {
            //return base.GetSaveLocation(SaveLocations.Documents); 
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify), "Bomb Rush Cyberfunk Modding");
        }
    }
}