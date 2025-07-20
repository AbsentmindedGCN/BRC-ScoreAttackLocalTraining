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
using System.Threading.Tasks;

namespace ScoreAttack
{
    public class AppGhostDelete : CustomApp
    {
        public override bool Available => false;

        private TMPro.TMP_Text titleBarText;
        private string normalTitle = "Delete Ghosts";

        private class GhostEntry
        {
            public string FilePath;
            public Ghost Ghost;
            public float TimeLimit;
            public float Score;
            public DateTime Timestamp;
        }

        private Dictionary<string, GhostEntry> CachedGhostEntries = new Dictionary<string, GhostEntry>();

        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppGhostDelete>("AppGhostDelete");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();
            CreateIconlessTitleBar(normalTitle);
            titleBarText = GetComponentInChildren<TMPro.TMP_Text>();
            ScrollView = PhoneScrollView.Create(this);
        }

        public override void OnAppEnable()
        {
            base.OnAppEnable();
            ScrollView.RemoveAllButtons();

            Stage currentStage = Core.Instance.BaseModule.CurrentStage;

            if (HasCachedGhostsForStage(currentStage))
            {
                LoadGhostDeleteButtons(currentStage);
                SetTitleBarText(normalTitle);
            }
            else
            {
                SetTitleBarText("Loading ghosts...");
                StartCoroutine(LoadGhostDeleteButtonsCoroutine(currentStage));
            }
        }

        private bool HasCachedGhostsForStage(Stage stage)
        {
            string cleanStageName = GetCleanStageName(stage).ToLowerInvariant();
            return CachedGhostEntries.Values.Any(entry =>
                Path.GetFileNameWithoutExtension(entry.FilePath).ToLower().StartsWith(cleanStageName + "-"));
        }

        private void SetTitleBarText(string text)
        {
            if (titleBarText != null)
                titleBarText.text = text;
        }

        private void LoadGhostDeleteButtons(Stage currentStage)
        {
            StartCoroutine(LoadGhostDeleteButtonsCoroutine(currentStage));
        }

        private System.Collections.IEnumerator LoadGhostDeleteButtonsCoroutine(Stage currentStage)
        {
            yield return null;

            string folderPath = GhostSaveData.Instance.GetSaveLocation();
            string ghostFolder = Path.Combine(folderPath, "GhostSaveData");

            if (!Directory.Exists(ghostFolder))
            {
                Debug.Log("[ScoreAttack] No ghost save folder found.");
                SetTitleBarText(normalTitle);
                yield break;
            }

            var ghostFiles = Directory.GetFiles(ghostFolder, "*.ghost");
            List<GhostEntry> ghostEntries = null;

            var task = Task.Run(() =>
            {
                var entries = new List<GhostEntry>();
                foreach (var file in ghostFiles)
                {
                    if (CachedGhostEntries.TryGetValue(file, out GhostEntry cachedEntry))
                    {
                        entries.Add(cachedEntry);
                        continue;
                    }

                    var fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                    string cleanStageName = GetCleanStageName(currentStage).ToLowerInvariant();

                    if (!fileName.StartsWith(cleanStageName + "-"))
                        continue;

                    if (!TryLoadGhostMetadata(file, out Ghost ghost, out float timeLimit, out float score, out DateTime timestamp))
                        continue;

                    GhostEntry entry = new()
                    {
                        FilePath = file,
                        Ghost = ghost,
                        TimeLimit = timeLimit,
                        Score = score,
                        Timestamp = timestamp
                    };

                    CachedGhostEntries[file] = entry;
                    entries.Add(entry);
                }

                return entries.OrderBy(e => e.TimeLimit).ThenBy(e => e.Score).ToList();
            });

            while (!task.IsCompleted)
                yield return null;
            ghostEntries = task.Result;

            ScrollView.RemoveAllButtons();

            if (ghostEntries.Count > 0)
            {
                string buttonLabel = $"<color=red><b>DELETE ALL GHOSTS</b></color>\n<size=50%>Careful! This will delete all ghosts in this area!</size>";
                var deleteAllButton = PhoneUIUtility.CreateSimpleButton(buttonLabel);
                deleteAllButton.LabelUnselectedColor = Color.black;
                deleteAllButton.LabelSelectedColor = Color.red;

                deleteAllButton.OnConfirm += () =>
                {
                    foreach (var entry in ghostEntries)
                    {
                        if (File.Exists(entry.FilePath))
                        {
                            CachedGhostEntries.Remove(entry.FilePath);
                            File.Delete(entry.FilePath);
                        }
                    }

                    Core.Instance.UIManager.ShowNotification("All ghosts deleted.");
                    ScrollView.RemoveAllButtons();
                    LoadGhostDeleteButtons(currentStage);
                };

                ScrollView.AddButton(deleteAllButton);
            }

            int processed = 0;
            foreach (var entry in ghostEntries)
            {
                string timeLabel = $"{Mathf.RoundToInt(entry.TimeLimit / 60f)} Min - {Mathf.FloorToInt(entry.Score):N0}";
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
                        LoadGhostDeleteButtons(currentStage);
                    }
                    else
                    {
                        Core.Instance.UIManager.ShowNotification("Ghost file not found.");
                    }
                };

                button.LabelUnselectedColor = Color.black;
                button.LabelSelectedColor = Color.red;
                ScrollView.AddButton(button);

                if (++processed % 10 == 0)
                    yield return null;
            }

            SetTitleBarText(normalTitle);
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
                    return false;
                }

                string timePart = parts[1];
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
            string cleanStageName = stage switch
            {
                Stage.hideout => "hideout",
                Stage.downhill => "versum_hill",
                Stage.square => "millennium_square",
                Stage.tower => "brink_terminal",
                Stage.Mall => "millennium_mall",
                Stage.osaka => "mataan",
                Stage.pyramid => "pyramid_island",
                Stage.Prelude => "police_station",
                _ => stage.ToString().ToLowerInvariant()
            };

            return cleanStageName
            .Replace(@"/", ".")
            .Replace(@"\", ".") // MapStation fix
            .Replace("-", ".") // Replace hyphens with dots
            .Replace("..", "."); // Replace double dot
        }
    }
}
