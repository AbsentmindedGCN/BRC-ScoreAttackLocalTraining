using Reptile;
using ScoreAttackGhostSystem;
using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;
using System.Linq;
using CommonAPI.Phone;
using Reptile.Phone;
using static ScoreAttack.AppGrindDebt;
using System.Collections.Generic;

namespace ScoreAttack
{
    public class AppGhostList : CustomApp
    {
        //private PhoneScrollView scrollView;

        public override bool Available => false;
        //public override string ID => "AppGhostList";
        //public override string DisplayName => "Ghost Replays";

        private class GhostEntry
        {
            public string FilePath;
            public Ghost Ghost;
            public float TimeLimit;
            public float Score;
            public DateTime Timestamp;
        }

        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppGhostList>("AppGhostList");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();

            //LoadState();

            CreateIconlessTitleBar("Ghosts\n<size=50%>Load Previous!</size>");

            ScrollView = PhoneScrollView.Create(this);
        }

        public override void OnAppEnable()
        {
            base.OnAppEnable();

            //ScrollView.Clear(); // Ensure fresh load every time
            ScrollView.RemoveAllButtons();

            Stage currentStage = Core.Instance.BaseModule.CurrentStage;
            LoadSavedGhostButtons(currentStage, ScrollView);
        }

        private void LoadSavedGhostButtons(Stage currentStage, PhoneScrollView scrollView)
        {
            string folderPath = GhostSaveData.Instance.GetSaveLocation();
            string ghostFolder = Path.Combine(folderPath, "GhostSaveData");

            if (!Directory.Exists(ghostFolder))
            {
                Debug.Log("[ScoreAttack] No ghost save folder found.");
                return;
            }

            var ghostFiles = Directory.GetFiles(ghostFolder, "*.ghost");
            var ghostEntries = new List<GhostEntry>();

            foreach (var file in ghostFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                string cleanStageName = GetCleanStageName(currentStage).ToLowerInvariant();
                /*
                if (!fileName.StartsWith(cleanStageName))
                    continue;
                */
                if (!fileName.StartsWith(cleanStageName + "-"))
                {
                    Debug.Log($"[ScoreAttack] Skipping {fileName} (doesn't start with '{cleanStageName}-')");
                    continue;
                }

                if (!TryLoadGhostMetadata(file, out Ghost ghost, out float timeLimit, out float score, out DateTime timestamp))
                    continue;

                ghostEntries.Add(new GhostEntry
                {
                    FilePath = file,
                    Ghost = ghost,
                    TimeLimit = timeLimit,
                    Score = score,
                    Timestamp = timestamp
                });
            }

            /*
            // Sort from high to low score
            ghostEntries.Sort((a, b) => b.Score.CompareTo(a.Score));
            */

            // Sort by time limit (ascending), then by score (descending)
            ghostEntries = ghostEntries
                .OrderBy(entry => entry.TimeLimit)
                .ThenByDescending(entry => entry.Score)
                .ToList();

            foreach (var entry in ghostEntries)
            {
                string timeLabel = $"{Mathf.RoundToInt(entry.TimeLimit / 60f)} Min - {Mathf.FloorToInt(entry.Score).ToString("N0")}";
                string timestampLabel = entry.Timestamp.ToString("MMMM d, yyyy, 'at' hh:mm:ss tt");
                string buttonLabel = $"{timeLabel}\n<size=50%>{timestampLabel}</size>";

                var button = PhoneUIUtility.CreateSimpleButton(buttonLabel);
                Ghost ghost = entry.Ghost;
                float timeLimit = entry.TimeLimit;
                float score = entry.Score;
                DateTime timestamp = entry.Timestamp;

                button.OnConfirm += () =>
                {

                    // Check if ghost display is disabled, jump ship if it is since the ghost wouldn't appear
                    if (GhostSaveData.Instance.GhostDisplayMode == GhostDisplayMode.Hide)
                    {
                        Debug.Log("[ScoreAttack] Ghost display mode set to Hide. Skipping Score Attack setup.");
                        Core.Instance.UIManager.ShowNotification("Ghost display mode is set to <color=red>hide</color>.\nPlease enable it to use ghost playback during runs.");
                        return;
                    }

                    // ---------

                    Debug.Log($"[ScoreAttack] Starting {Mathf.RoundToInt(timeLimit / 60f)} Min Score Battle with ghost saved at {timestamp}.");

                    if (BannedMods.IsAdvantageousModLoaded())
                    {
                        Core.Instance.UIManager.ShowNotification("The score attack app is not compatible with TrickGod!");
                        return;
                    }

                    var stage = Core.Instance.BaseModule.CurrentStage;
                    var respawnPoint = ScoreAttackSaveData.Instance.GetRespawnPoint(stage);
                    if (respawnPoint == null)
                    {
                        Core.Instance.UIManager.ShowNotification("Please set a respawn point first!\n<size=50%>It's at the bottom of the app menu!</size>");
                        return;
                    }

                    respawnPoint.ApplyToPlayer(MyPhone.player);

                    if (ScoreAttackEncounter.IsScoreAttackActive())
                    {
                        var activeEncounter = GameObject.FindObjectOfType<ScoreAttackEncounter>();
                        activeEncounter.EndScoreAttack();
                        Core.Instance.SaveManager.SaveCurrentSaveSlot();
                    }

                    /*
                    //ScoreAttackManager.LoadedExternalGhost = ghost;
                    ScoreAttackManager.ExternalGhostLoadedFromGhostList = true; // check
                    ghost.Score = score; // Assign external ghost score
                    ScoreAttackManager.LoadedExternalGhost = ghost;
                    */

                    ScoreAttackManager.ExternalGhostScore = score;
                    ScoreAttackManager.LoadedExternalGhost = ghost;
                    ScoreAttackManager.ExternalGhostLoadedFromGhostList = true;

                    AppScoreAttack.BattleRefresh();
                    ScoreAttackManager.StartScoreAttack(timeLimit);
                    MyPhone.CloseCurrentApp();
                    MyPhone.TurnOff();
                };

                scrollView.AddButton(button);
            }
        }

        private static bool TryLoadGhostMetadata(string filePath, out Ghost ghost, out float timeLimit, out float score, out DateTime timestamp)
        {
            ghost = null;
            timeLimit = 0f;
            score = 0f;
            timestamp = DateTime.MinValue;

            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var parts = fileName.Split('-');
                if (parts.Length != 4)
                {
                    Debug.LogWarning($"[ScoreAttack] Invalid ghost filename format: {fileName}");
                    return false;
                }

                string timePart = parts[1]; // "1min"
                string numericMinutes = new string(timePart.Where(char.IsDigit).ToArray());
                if (!int.TryParse(numericMinutes, out int timeMinutes))
                    return false;

                timeLimit = timeMinutes * 60f;

                if (!float.TryParse(parts[2], out score))
                    return false;

                if (!DateTime.TryParseExact(parts[3], "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp))
                    return false;

                using var fs = new FileStream(filePath, FileMode.Open);
                using var br = new BinaryReader(new GZipStream(fs, CompressionMode.Decompress));
                ghost = GhostSaveData.Instance.ReadExportedGhost(br).Ghost;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScoreAttack] Failed to parse ghost file '{filePath}': {ex}");
                return false;
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
                _ => stage.ToString() // fallback
            };
        }

    }
}
