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
    public class AppGhostList : CustomApp
    {
        public override bool Available => false;

        private TMPro.TMP_Text titleBarText;
        private string normalTitle = "Ghosts\n<size=50%>Load Previous!</size>";

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
            PhoneAPI.RegisterApp<AppGhostList>("AppGhostList");
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
                LoadSavedGhostButtons(currentStage, ScrollView);
                SetTitleBarText(normalTitle);
            }
            else
            {
                SetTitleBarText("Loading ghosts...");
                StartCoroutine(LoadSavedGhostButtonsCoroutine(currentStage));
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
                if (CachedGhostEntries.TryGetValue(file, out GhostEntry entry))
                {
                    ghostEntries.Add(entry);
                }
                else
                {
                    var fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                    string cleanStageName = GetCleanStageName(currentStage).ToLowerInvariant();

                    if (!fileName.StartsWith(cleanStageName + "-"))
                        continue;

                    if (!TryLoadGhostMetadata(file, out Ghost ghost, out float timeLimit, out float score, out DateTime timestamp))
                        continue;

                    entry = new GhostEntry
                    {
                        FilePath = file,
                        Ghost = ghost,
                        TimeLimit = timeLimit,
                        Score = score,
                        Timestamp = timestamp
                    };

                    ghostEntries.Add(entry);
                    CachedGhostEntries[file] = entry;
                }
            }

            ghostEntries = ghostEntries.OrderBy(e => e.TimeLimit).ThenByDescending(e => e.Score).ToList();
            scrollView.RemoveAllButtons();

            foreach (var entry in ghostEntries)
            {
                string timeLabel = $"{Mathf.RoundToInt(entry.TimeLimit / 60f)} Min - {Mathf.FloorToInt(entry.Score):N0}";
                string timestampLabel = entry.Timestamp.ToString("MMMM d, yyyy, 'at' hh:mm:ss tt");
                string buttonLabel = $"{timeLabel}\n<size=50%>{timestampLabel}</size>";

                var button = PhoneUIUtility.CreateSimpleButton(buttonLabel);
                Ghost ghost = entry.Ghost;
                float timeLimit = entry.TimeLimit;
                float score = entry.Score;
                DateTime timestamp = entry.Timestamp;

                button.OnConfirm += () => LaunchScoreAttack(ghost, timeLimit, score, timestamp);
                scrollView.AddButton(button);
            }
        }

        private System.Collections.IEnumerator LoadSavedGhostButtonsCoroutine(Stage currentStage)
        {
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

                return entries.OrderBy(e => e.TimeLimit).ThenByDescending(e => e.Score).ToList();
            });

            while (!task.IsCompleted)
                yield return null;

            ghostEntries = task.Result;
            ScrollView.RemoveAllButtons();
            int processed = 0;

            foreach (var entry in ghostEntries)
            {
                string timeLabel = $"{Mathf.RoundToInt(entry.TimeLimit / 60f)} Min - {Mathf.FloorToInt(entry.Score):N0}";
                string timestampLabel = entry.Timestamp.ToString("MMMM d, yyyy, 'at' hh:mm:ss tt");
                string buttonLabel = $"{timeLabel}\n<size=50%>{timestampLabel}</size>";

                var button = PhoneUIUtility.CreateSimpleButton(buttonLabel);
                Ghost ghost = entry.Ghost;
                float timeLimit = entry.TimeLimit;
                float score = entry.Score;
                DateTime timestamp = entry.Timestamp;

                button.OnConfirm += () => LaunchScoreAttack(ghost, timeLimit, score, timestamp);
                ScrollView.AddButton(button);

                if (++processed % 10 == 0)
                    yield return null;
            }

            SetTitleBarText(normalTitle);
        }

        private void LaunchScoreAttack(Ghost ghost, float timeLimit, float score, DateTime timestamp)
        {
            if (GhostSaveData.Instance.GhostDisplayMode == GhostDisplayMode.Hide)
            {
                Core.Instance.UIManager.ShowNotification("Ghost display mode is set to <color=red>hide</color>.\nPlease enable it to use ghost playback during runs.");
                return;
            }

            if (BannedMods.IsAdvantageousModLoaded())
            {
                Core.Instance.UIManager.ShowNotification("The score attack app is not compatible with TrickGod!");
                return;
            }

            if (GhostSaveData.Instance.GhostWarpMode == GhostWarpMode.ToGhost && ghost.FrameCount > 0)
            {
                var f = ghost.Frames[0];
                WorldHandler.instance.PlacePlayerAt(MyPhone.player, f.PlayerPosition, f.PlayerRotation);
                MyPhone.player.SwitchToEquippedMovestyle(f.UsingEquippedMoveStyle);
            }
            else
            {
                var stage = Core.Instance.BaseModule.CurrentStage;
                var respawnPoint = ScoreAttackSaveData.Instance.GetRespawnPoint(stage);
                if (respawnPoint == null)
                {
                    Core.Instance.UIManager.ShowNotification("Please set a respawn point first!\n<size=50%>It's at the bottom of the app menu!</size>");
                    return;
                }
                respawnPoint.ApplyToPlayer(MyPhone.player);
            }

            if (ScoreAttackEncounter.IsScoreAttackActive())
            {
                var encounter = GameObject.FindObjectOfType<ScoreAttackEncounter>();
                encounter.EndScoreAttack();
                Core.Instance.SaveManager.SaveCurrentSaveSlot();
            }

            ScoreAttackManager.ExternalGhostScore = score;
            ScoreAttackManager.LoadedExternalGhost = ghost;
            ScoreAttackManager.ExternalGhostLoadedFromGhostList = true;

            AppScoreAttack.BattleRefresh();
            ScoreAttackManager.StartScoreAttack(timeLimit);
            MyPhone.CloseCurrentApp();
            MyPhone.TurnOff();
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
                    return false;

                string numericMinutes = new string(parts[1].Where(char.IsDigit).ToArray());
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

            return cleanStageName.Replace("/", ".").Replace("\\", ".");
        }
    }
}
