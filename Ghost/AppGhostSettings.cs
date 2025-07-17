using CommonAPI.Phone;
using Reptile;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.IO.Compression;
using System;
using System.Linq;
using ScoreAttackGhostSystem;

namespace ScoreAttack
{
    public enum GhostSaveMode { Disabled, Enabled, OnlyPB }
    //public enum GhostDisplayMode { Hide, Show }
    public enum GhostSoundMode { Off, On, Doppler }
    public enum GhostModel { Self, DJCyber, Faux }
    public enum GhostEffect { Transparent, Normal }
    public enum GhostScoreMode { Final, Ongoing }

    public class AppGhostSettings : CustomApp
    {

        public override bool Available => false;

        public static GhostSaveMode SaveMode { get; private set; } = GhostSaveMode.Enabled;
        // public static GhostDisplayMode DisplayMode { get; private set; } = GhostDisplayMode.Show;
        public static GhostSoundMode SoundMode { get; private set; } = GhostSoundMode.On;
        public static GhostModel CurrentModel { get; private set; } = GhostModel.Self;
        public static GhostEffect CurrentEffect { get; private set; } = GhostEffect.Transparent;

        public static GhostScoreMode CurrentScoreMode { get; private set; } = GhostScoreMode.Final;

        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppGhostSettings>("ghostsettings");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();

            LoadState();

            CreateIconlessTitleBar("Ghost Settings</size>");

            ScrollView = PhoneScrollView.Create(this);

            AddEnumButton("Auto Save Ghosts", () => SaveMode, GetNextSaveMode, m => SaveMode = m);
            //AddEnumButton("Display Ghosts", () => DisplayMode, GetNextDisplayMode, m => DisplayMode = m);
            AddEnumButton("Voice & Sound", () => SoundMode, GetNextSoundMode, m => SoundMode = m);
            AddEnumButton("Model Used", () => CurrentModel, GetNextModel, m => CurrentModel = m);
            AddEnumButton("Model Effect", () => CurrentEffect, GetNextEffect, m => CurrentEffect = m);
            AddEnumButton("Score Display", () => CurrentScoreMode, GetNextScoreMode, m => CurrentScoreMode = m);

            // Export Ghost Save Data
            var exportButton = PhoneUIUtility.CreateSimpleButton("Export PB Ghosts\n<size=50%>Back up your personal best ghosts</size>");
            exportButton.OnConfirm += () =>
            {
                if (ScoreAttackEncounter.IsScoreAttackActive())
                {
                    Core.Instance.UIManager.ShowNotification("You are in a run! Cancel your current run before exporting ghosts.");
                    return;
                }

                string exportPath = Path.Combine(Application.persistentDataPath, "PersonalBestGhosts.dat");

                try
                {
                    using (var fs = new FileStream(exportPath, FileMode.Create))
                    using (var writer = new BinaryWriter(new GZipStream(fs, CompressionMode.Compress)))
                    {
                        var allGhosts = GhostSaveData.Instance.BestGhostsByStage;

                        // Write total ghost count
                        int ghostCount = allGhosts.Sum(stagePair => stagePair.Value.GhostByTimeLimit.Count);
                        writer.Write(ghostCount);

                        foreach (var stagePair in allGhosts)
                        {
                            var stage = stagePair.Key;
                            var ghostData = stagePair.Value;

                            foreach (var timePair in ghostData.GhostByTimeLimit)
                            {
                                var ghost = timePair.Value;
                                //GhostSaveData.Instance.WriteExportedGhost(writer, timePair.Value, stage, timePair.Key);
                                GhostSaveData.Instance.WriteExportedGhost(writer, ghost, stage, timePair.Key, ghost.Score);
                            }
                        }
                    }

                    Core.Instance.UIManager.ShowNotification($"Personal best ghost data exported to:\n<size=50%>{exportPath}</size>");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Export failed: {ex}");
                    Core.Instance.UIManager.ShowNotification("Failed to export personal best ghost data.");
                }
            };

            exportButton.LabelUnselectedColor = UnityEngine.Color.black;
            exportButton.LabelSelectedColor = UnityEngine.Color.red;
            ScrollView.AddButton(exportButton);

            // Import Ghost Save Data
            var importButton = PhoneUIUtility.CreateSimpleButton("Import PB Ghosts\n<size=50%>Restore your personal best ghosts</size>");
            importButton.OnConfirm += () =>
            {
                if (ScoreAttackEncounter.IsScoreAttackActive())
                {
                    Core.Instance.UIManager.ShowNotification("You are in a run! Cancel your current run before importing ghosts.");
                    return;
                }

                string importPath = Path.Combine(Application.persistentDataPath, "PersonalBestGhosts.dat");

                if (!File.Exists(importPath))
                {
                    Core.Instance.UIManager.ShowNotification("No personal best ghost file found to import.");
                    return;
                }

                try
                {
                    using (var fs = new FileStream(importPath, FileMode.Open))
                    using (var reader = new BinaryReader(new GZipStream(fs, CompressionMode.Decompress)))
                    {
                        int ghostCount = reader.ReadInt32();

                        int importedCount = 0;

                        for (int i = 0; i < ghostCount; i++)
                        {
                            var imported = GhostSaveData.Instance.ReadExportedGhost(reader);
                            GhostSaveData.Instance
                                .GetOrCreateGhostData(imported.Stage)
                                .SetGhost(imported.TimeLimit, imported.Ghost);

                            importedCount++;
                        }

                        Core.Instance.UIManager.ShowNotification($"Imported {importedCount} personal best ghost(s) from:\n<size=50%>{importPath}</size>");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Import failed: {ex}");
                    Core.Instance.UIManager.ShowNotification("Failed to import personal best ghost data.");
                }
            };
            importButton.LabelUnselectedColor = UnityEngine.Color.black;
            importButton.LabelSelectedColor = UnityEngine.Color.red;
            ScrollView.AddButton(importButton);

            // Clear All Ghost Data
            var clearButton = PhoneUIUtility.CreateSimpleButton("Clear PB Ghosts\n<size=50%>Delete all personal best ghosts</size>");
            clearButton.OnConfirm += () =>
            {
                if (ScoreAttackEncounter.IsScoreAttackActive())
                {
                    Core.Instance.UIManager.ShowNotification("Can't clear personal best ghosts during a run!");
                    return;
                }

                GhostSaveData.Instance.BestGhostsByStage.Clear();
                Core.Instance.UIManager.ShowNotification("All personal best ghosts cleared.");
            };
            clearButton.LabelUnselectedColor = UnityEngine.Color.black;
            clearButton.LabelSelectedColor = UnityEngine.Color.red;
            ScrollView.AddButton(clearButton);

        }

        private void AddEnumButton<T>(string label, System.Func<T> getCurrent, System.Func<T, T> getNext, System.Action<T> setValue)
        {
            //var button = PhoneUIUtility.CreateSimpleButton($"{label}\n<size=50%><color=orange>{FormatEnum(getCurrent())}</color></size>");

            var current = getCurrent();
            var button = PhoneUIUtility.CreateSimpleButton($"{label}\n<size=50%><color={GetColorForValue(current)}>{FormatEnum(current)}</color></size>");
            button.OnConfirm += () =>
            {
                var current = getCurrent();
                var nextValue = getNext(current);
                setValue(nextValue);
                //button.Label.SetText($"{label}\n<size=50%><color=orange>{FormatEnum(nextValue)}</color></size>");
                button.Label.SetText($"{label}\n<size=50%><color={GetColorForValue(nextValue)}>{FormatEnum(nextValue)}</color></size>");
                SaveState();
            };
            ScrollView.AddButton(button);
        }


        private static string FormatEnum<T>(T val)
        {
            switch (val)
            {
                //case GhostSaveMode.Disabled: return "Disabled Recording, Loading, and Saving";
                case GhostSaveMode.Disabled: return "Disabled Recording and Saving Ghosts";
                case GhostSaveMode.Enabled: return "All Ghosts Automatically Saved in Runs";
                case GhostSaveMode.OnlyPB: return "Only Auto Save Personal Best Ghosts";
                //case GhostDisplayMode.Hide: return "Hide Ghost Playback During Runs";
                //case GhostDisplayMode.Show: return "Show Ghost Playback During Runs";
                case GhostSoundMode.Off: return "Ghost Sound Disabled";
                case GhostSoundMode.On: return "Ghost Sound Enabled";
                case GhostSoundMode.Doppler: return "Ghost Sound with Doppler Effect";
                case GhostModel.Self: return "Recorded/Current Model";
                case GhostModel.DJCyber: return "DJ Cyber";
                case GhostModel.Faux: return "Faux";
                case GhostEffect.Transparent: return "Translucent";
                case GhostEffect.Normal: return "Normal";
                case GhostScoreMode.Final: return "Final Score";
                case GhostScoreMode.Ongoing: return "Real-Time Score";
                default: return val.ToString();
            }
        }

        private static GhostSaveMode GetNextSaveMode(GhostSaveMode mode) =>
            (GhostSaveMode)(((int)mode + 1) % System.Enum.GetValues(typeof(GhostSaveMode)).Length);

        private static GhostDisplayMode GetNextDisplayMode(GhostDisplayMode mode) =>
            (GhostDisplayMode)(((int)mode + 1) % System.Enum.GetValues(typeof(GhostDisplayMode)).Length);

        private static GhostSoundMode GetNextSoundMode(GhostSoundMode mode) =>
            (GhostSoundMode)(((int)mode + 1) % System.Enum.GetValues(typeof(GhostSoundMode)).Length);

        private static GhostModel GetNextModel(GhostModel model) =>
            (GhostModel)(((int)model + 1) % System.Enum.GetValues(typeof(GhostModel)).Length);

        private static GhostEffect GetNextEffect(GhostEffect effect) =>
            (GhostEffect)(((int)effect + 1) % System.Enum.GetValues(typeof(GhostEffect)).Length);

        private static GhostScoreMode GetNextScoreMode(GhostScoreMode scoremode) =>
            (GhostScoreMode)(((int)scoremode + 1) % System.Enum.GetValues(typeof(GhostScoreMode)).Length);

        private void LoadState()
        {
            var save = GhostSaveData.Instance;
            SaveMode = save.GhostSaveMode;
            // DisplayMode = save.GhostDisplayMode; // Moved to AppGhost
            SoundMode = save.GhostSoundMode;
            CurrentModel = save.GhostModel;
            CurrentEffect = save.GhostEffect;
            CurrentScoreMode = save.GhostScoreMode;
        }

        private void SaveState()
        {
            var save = GhostSaveData.Instance;
            save.GhostSaveMode = SaveMode;
            // save.GhostDisplayMode = DisplayMode; // Moved to AppGhost
            save.GhostSoundMode = SoundMode;
            save.GhostModel = CurrentModel;
            save.GhostEffect = CurrentEffect;
            save.GhostScoreMode = CurrentScoreMode;
        }

        private static string GetColorForValue<T>(T val)
        {
            switch (val)
            {
                case GhostSaveMode.Disabled:
                case GhostDisplayMode.Hide:
                case GhostSoundMode.Off:
                    return "red";
                case GhostSaveMode.Enabled:
                case GhostDisplayMode.Show:
                case GhostSoundMode.On:
                    return "green";
                case GhostSaveMode.OnlyPB:
                case GhostSoundMode.Doppler:
                case GhostModel.Self:
                case GhostModel.DJCyber:
                case GhostModel.Faux:
                case GhostEffect.Transparent:
                case GhostEffect.Normal:
                case GhostScoreMode.Final:
                case GhostScoreMode.Ongoing:
                    return "orange";
                default:
                    return "white";
            }
        }

    }
}