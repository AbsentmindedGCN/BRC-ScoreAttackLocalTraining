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
    public enum GhostDisplayMode { Hide, Show }
    public enum GhostWarpMode { Respawn, ToGhost }

    public class AppGhost : CustomApp
    {
        public override bool Available => false;

        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppGhost>("ghost");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();

            CreateIconlessTitleBar("Ghosts\n<size=50%>Challenge Ghosts!</size>");

            ScrollView = PhoneScrollView.Create(this);

            // Launch GhostList
            var button = PhoneUIUtility.CreateSimpleButton("Load Ghost...");
            button.OnConfirm += () =>
            {
                // Check if ghost display is disabled, jump ship if it is since the ghost wouldn't appear
                if (GhostSaveData.Instance.GhostDisplayMode == GhostDisplayMode.Hide)
                {
                    Debug.Log("[ScoreAttack] Ghost display mode set to Hide. Not letting the player enter this menu.");
                    Core.Instance.UIManager.ShowNotification("Ghost display mode is set to <color=red>hide</color>.\nPlease enable it to use ghost playback during runs.");
                    return;
                }
                else
                {
                    MyPhone.OpenApp(typeof(AppGhostList));
                }
            };
            ScrollView.AddButton(button);

            // Launch GhostListDelete
            button = PhoneUIUtility.CreateSimpleButton("Delete Ghost...");
            button.OnConfirm += () =>
            {
                MyPhone.OpenApp(typeof(AppGhostDelete));
            };
            ScrollView.AddButton(button);

            // Launch GhostSettings
            button = PhoneUIUtility.CreateSimpleButton("Ghost Settings...");
            button.OnConfirm += () =>
            {
                MyPhone.OpenApp(typeof(AppGhostSettings));
            };
            ScrollView.AddButton(button);

            //AddEnumButton("Display Ghosts", () => AppGhostSettings.DisplayMode, GetNextDisplayMode, m => AppGhostSettings.DisplayMode = m);
            AddEnumButton("Display Ghosts", () => GhostSaveData.Instance.GhostDisplayMode, GetNextDisplayMode, m => GhostSaveData.Instance.GhostDisplayMode = m);
            AddEnumButton("Start Position", () => GhostSaveData.Instance.GhostWarpMode, GetNextWarpMode, m => GhostSaveData.Instance.GhostWarpMode = m);

            // Add Cancel Run button, for convenience
            button = PhoneUIUtility.CreateSimpleButton("Cancel Run");
            button.OnConfirm += () =>
            {
                // New Encounter
                Debug.Log("Cancelling Active Battle...");

                ScoreAttackManager.LoadedExternalGhost = null;
                ScoreAttackManager.ExternalGhostLoadedFromGhostList = false; // check

                bool isScoreBattleActive = ScoreAttackEncounter.IsScoreAttackActive();
                if (isScoreBattleActive)
                {
                    // End the Score Battle
                    ScoreAttackEncounter scoreAttackActiveEncounter = FindObjectOfType<ScoreAttackEncounter>();
                    scoreAttackActiveEncounter.EndScoreAttack();

                    //Save on ending..?
                    Core.Instance.SaveManager.SaveCurrentSaveSlot();
                    //ScoreAttack.ScoreAttackEncounter.EndScoreAttack();
                }
                else
                {
                    Core.Instance.UIManager.ShowNotification("There is no ongoing Score Battle run!");
                }

            };
            button.LabelUnselectedColor = UnityEngine.Color.black;
            button.LabelSelectedColor = UnityEngine.Color.red;
            ScrollView.AddButton(button);

        }

            // ------------------

            /*
            // Export Ghost Save Data
            var exportButton = PhoneUIUtility.CreateSimpleButton("Export Ghost Data\n<size=50%>Back up your best ghosts</size>");
            exportButton.OnConfirm += () =>
            {
                if (ScoreAttackEncounter.IsScoreAttackActive())
                {
                    Core.Instance.UIManager.ShowNotification("You are in a run! Cancel your current run before exporting.");
                    return;
                }

                string exportPath = Path.Combine(Application.persistentDataPath, "AllGhosts.ghosts");

                try
                {
                    using (var fs = new FileStream(exportPath, FileMode.Create))
                    using (var writer = new BinaryWriter(fs))
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

                    Core.Instance.UIManager.ShowNotification($"All ghost data exported to:\n<size=50%>{exportPath}</size>");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Export failed: {ex}");
                    Core.Instance.UIManager.ShowNotification("Failed to export all ghost data.");
                }
            };
            ScrollView.AddButton(exportButton);

            // Import Ghost Save Data
            var importButton = PhoneUIUtility.CreateSimpleButton("Import Ghost Data\n<size=50%>Restore your ghost data</size>");
            importButton.OnConfirm += () =>
            {
                if (ScoreAttackEncounter.IsScoreAttackActive())
                {
                    Core.Instance.UIManager.ShowNotification("Cancel your current run before importing.");
                    return;
                }

                string importPath = Path.Combine(Application.persistentDataPath, "AllGhosts.ghosts");

                if (!File.Exists(importPath))
                {
                    Core.Instance.UIManager.ShowNotification("No ghost file found to import.");
                    return;
                }

                try
                {
                    using (var fs = new FileStream(importPath, FileMode.Open))
                    using (var reader = new BinaryReader(fs))
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

                        Core.Instance.UIManager.ShowNotification($"Imported {importedCount} ghost(s) from:\n<size=50%>{importPath}</size>");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Import failed: {ex}");
                    Core.Instance.UIManager.ShowNotification("Failed to import ghost data.");
                }
            };
            ScrollView.AddButton(importButton);

            // Clear All Ghost Data
            var clearButton = PhoneUIUtility.CreateSimpleButton("Clear Ghost Data\n<size=50%>Delete all saved ghosts</size>");
            clearButton.OnConfirm += () =>
            {
                if (ScoreAttackEncounter.IsScoreAttackActive())
                {
                    Core.Instance.UIManager.ShowNotification("Can't clear ghosts during a battle!");
                    return;
                }

                GhostSaveData.Instance.BestGhostsByStage.Clear();
                Core.Instance.UIManager.ShowNotification("All ghost data cleared.");
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
                case GhostSaveMode.Disabled: return "Disabled Recording, Loading, and Saving";
                case GhostSaveMode.Enabled: return "Enabled Ghost Replay System";
                case GhostDisplayMode.Hide: return "Hide Ghost Playback During Runs";
                case GhostDisplayMode.Show: return "Show Ghost Playback During Runs";
                case GhostSoundMode.Off: return "Ghost Sound Disabled";
                case GhostSoundMode.On: return "Ghost Sound Enabled";
                case GhostSoundMode.Doppler: return "Ghost Sound with Doppler Effect";
                case GhostModel.Self: return "Your Current Model";
                case GhostModel.DJCyber: return "DJ Cyber";
                case GhostModel.Felix: return "Felix";
                case GhostEffect.Transparent: return "Translucent";
                case GhostEffect.Normal: return "Normal";
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

        private void LoadState()
        {
            var save = GhostSaveData.Instance;
            SaveMode = save.GhostSaveMode;
            DisplayMode = save.GhostDisplayMode;
            SoundMode = save.GhostSoundMode;
            CurrentModel = save.GhostModel;
            CurrentEffect = save.GhostEffect;
        }

        private void SaveState()
        {
            var save = GhostSaveData.Instance;
            save.GhostSaveMode = SaveMode;
            save.GhostDisplayMode = DisplayMode;
            save.GhostSoundMode = SoundMode;
            save.GhostModel = CurrentModel;
            save.GhostEffect = CurrentEffect;
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
                case GhostSoundMode.Doppler:
                case GhostModel.Self:
                case GhostModel.DJCyber:
                case GhostModel.Felix:
                case GhostEffect.Transparent:
                case GhostEffect.Normal:
                    return "orange";
                default:
                    return "white";
            }
        }

        */

        private void AddEnumButton<T>(string label, Func<T> getCurrent, Func<T, T> getNext, Action<T> setValue)
        {
            var current = getCurrent();
            var button = PhoneUIUtility.CreateSimpleButton($"{label}\n<size=50%><color={GetColorForValue(current)}>{FormatEnum(current)}</color></size>");
            button.OnConfirm += () =>
            {
                var current = getCurrent();
                var nextValue = getNext(current);
                setValue(nextValue);
                button.Label.SetText($"{label}\n<size=50%><color={GetColorForValue(nextValue)}>{FormatEnum(nextValue)}</color></size>");
                
                // already directly set in GhostSaveData.Instance via setValue
                //GhostSaveData.Instance.GhostDisplayMode = (GhostDisplayMode)(object)nextValue; // ensure persistence
            };
            ScrollView.AddButton(button);
        }

        private static string FormatEnum<T>(T val)
        {
            switch (val)
            {
                case GhostDisplayMode.Hide: return "Hide Ghost Playback During Runs";
                case GhostDisplayMode.Show: return "Show Ghost Playback During Runs";
                case GhostWarpMode.Respawn: return "Your Respawn Point";
                case GhostWarpMode.ToGhost: return "Ghost Start Point";
                default: return val.ToString();
            }
        }

        private static string GetColorForValue<T>(T val)
        {
            switch (val)
            {
                case GhostDisplayMode.Hide:
                    return "red";
                case GhostDisplayMode.Show:
                    return "green";
                case GhostWarpMode.Respawn:
                case GhostWarpMode.ToGhost: 
                    return "orange"; 
                default:
                    return "white";
            }
        }

        private static GhostDisplayMode GetNextDisplayMode(GhostDisplayMode mode) => (GhostDisplayMode)(((int)mode + 1) % Enum.GetValues(typeof(GhostDisplayMode)).Length);
        private static GhostWarpMode GetNextWarpMode(GhostWarpMode mode) => (GhostWarpMode)(((int)mode + 1) % Enum.GetValues(typeof(GhostWarpMode)).Length);
    }
}