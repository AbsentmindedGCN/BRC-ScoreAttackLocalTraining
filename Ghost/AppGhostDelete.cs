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
using System.Collections.Generic;
using System.Net;
using UnityEngine.UIElements;

namespace ScoreAttack
{

    public class AppGhostDelete : CustomApp
    {
        public override bool Available => false;

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
            PhoneAPI.RegisterApp<AppGhostDelete>("AppGhostDelete");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();
            CreateIconlessTitleBar("Delete Ghosts");
            //CreateIconlessTitleBar("Delete Ghosts\n<size=50%>Select to Delete</size>");
            ScrollView = PhoneScrollView.Create(this);
        }

        public override void OnAppEnable()
        {
            base.OnAppEnable();
            ScrollView.RemoveAllButtons();

            Stage currentStage = Core.Instance.BaseModule.CurrentStage;
            LoadGhostDeleteButtons(currentStage, ScrollView);
        }

        /*
        private void LoadGhostDeleteButtons(Stage currentStage, PhoneScrollView scrollView)
        {
            string folderPath = GhostSaveData.Instance.GetSaveLocation();
            string ghostFolder = Path.Combine(folderPath, "GhostSaveData");

            if (!Directory.Exists(ghostFolder))
            {
                Debug.Log("[ScoreAttack] No ghost save folder found.");
                return;
            }

            var ghostFiles = Directory.GetFiles(ghostFolder, "*.ghost");
            List<GhostEntry> ghostEntries = new List<GhostEntry>();

            foreach (var file in ghostFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                if (!fileName.StartsWith(currentStage.ToString().ToLower()))
                    continue;

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

            // Sort by score (ascending)
            ghostEntries.Sort((a, b) => a.Score.CompareTo(b.Score));

            foreach (var entry in ghostEntries)
            {
                string timeLabel = $"{Mathf.RoundToInt(entry.TimeLimit / 60f)} Min - {Mathf.FloorToInt(entry.Score).ToString("N0")}";
                string timestampLabel = entry.Timestamp.ToString("MMMM d, yyyy, 'at' hh:mm:ss tt");
                string buttonLabel = $"{timeLabel}\n<size=50%>{timestampLabel}</size>";

                string filePath = entry.FilePath;

                var button = PhoneUIUtility.CreateSimpleButton(buttonLabel);
                button.OnConfirm += () =>
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Core.Instance.UIManager.ShowNotification("Ghost deleted.");

                        // Refresh the list after deletion
                        ScrollView.RemoveAllButtons();
                        LoadGhostDeleteButtons(currentStage, scrollView);
                    }
                    else
                    {
                        Core.Instance.UIManager.ShowNotification("Ghost file not found.");
                    }
                };

                button.LabelUnselectedColor = Color.black;
                button.LabelSelectedColor = Color.red;
                scrollView.AddButton(button);
            }
        }
        */


        private void LoadGhostDeleteButtons(Stage currentStage, PhoneScrollView scrollView)
        {
            string folderPath = GhostSaveData.Instance.GetSaveLocation();
            string ghostFolder = Path.Combine(folderPath, "GhostSaveData");

            if (!Directory.Exists(ghostFolder))
            {
                Debug.Log("[ScoreAttack] No ghost save folder found.");
                return;
            }

            var ghostFiles = Directory.GetFiles(ghostFolder, "*.ghost");
            List<GhostEntry> ghostEntries = new List<GhostEntry>();

            foreach (var file in ghostFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
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
            // Sort by score (ascending)
            ghostEntries.Sort((a, b) => a.Score.CompareTo(b.Score));
            */

            // Sort by time limit (ascending), then by score (ascending), that way user can delete the LOWEST SCORES first
            ghostEntries = ghostEntries
                .OrderBy(entry => entry.TimeLimit)
                .ThenBy(entry => entry.Score)
                .ToList();

            // Add "Delete All" button
            if (ghostEntries.Count > 0)
            {
                string buttonLabel = $"<color=red><b>DELETE ALL GHOSTS</b></color>\n<size=50%>Careful! This will delete all ghosts in this area!</size>";
                //string buttonLabel = $"<color=red><b>DELETE ALL GHOSTS</b></color>\n<size=50%>Careful! This will remove all ghosts in {GetCleanStageName(currentStage)}!</size>";
                var deleteAllButton = PhoneUIUtility.CreateSimpleButton(buttonLabel);
                //var deleteAllButton = PhoneUIUtility.CreateSimpleButton("<color=red><b>DELETE ALL GHOSTS</b></color>\n<size=50%>Remove every ghost in {currentStage}</size>");
                //deleteAllButton.LabelUnselectedColor = new Color(0.4f, 0f, 0f); // Dark red
                //deleteAllButton.LabelSelectedColor = new Color(1f, 0.1f, 0.1f); // Bright red
                deleteAllButton.LabelUnselectedColor = Color.black;
                deleteAllButton.LabelSelectedColor = Color.red;

                deleteAllButton.OnConfirm += () =>
                {
                    foreach (var entry in ghostEntries)
                    {
                        if (File.Exists(entry.FilePath))
                            File.Delete(entry.FilePath);
                    }

                    Core.Instance.UIManager.ShowNotification("All ghosts deleted.");
                    ScrollView.RemoveAllButtons();
                    LoadGhostDeleteButtons(currentStage, scrollView);
                };

                scrollView.AddButton(deleteAllButton);
            }

            // Add individual delete buttons
            foreach (var entry in ghostEntries)
            {
                string timeLabel = $"{Mathf.RoundToInt(entry.TimeLimit / 60f)} Min - {Mathf.FloorToInt(entry.Score).ToString("N0")}";
                string timestampLabel = entry.Timestamp.ToString("MMMM d, yyyy, 'at' hh:mm:ss tt");
                string buttonLabel = $"{timeLabel}\n<size=50%>{timestampLabel}</size>";

                string filePath = entry.FilePath;

                var button = PhoneUIUtility.CreateSimpleButton(buttonLabel);
                button.OnConfirm += () =>
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Core.Instance.UIManager.ShowNotification("Ghost deleted.");

                        ScrollView.RemoveAllButtons();
                        LoadGhostDeleteButtons(currentStage, scrollView);
                    }
                    else
                    {
                        Core.Instance.UIManager.ShowNotification("Ghost file not found.");
                    }
                };

                button.LabelUnselectedColor = Color.black;
                button.LabelSelectedColor = Color.red;
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
