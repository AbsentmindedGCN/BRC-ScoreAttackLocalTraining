using CommonAPI.Phone;
using Reptile;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using TMPro;
using UnityEngine;

namespace ScoreAttack
{
    public enum SFXToggle
    {
        Default,            // Default
        DefaultPlus,        // Default Plus
        LLB,                // Lethal League Blaze
        FZero               // F-Zero GX
    }

    public class AppExtras : CustomApp
    {
        public override bool Available => false;

        public static SFXToggle SFXMode { get; private set; } = SFXToggle.Default;

        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppExtras>("extras");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();

            LoadState();

            CreateIconlessTitleBar(GetExtrasTitle());

            ScrollView = PhoneScrollView.Create(this);

            // Create toggle button
            var toggle = PhoneUIUtility.CreateSimpleButton(GetViewerLabel());
            toggle.OnConfirm += () =>
            {
                // Cycle between Default and LLB and More
                SFXMode = GetNextMode(SFXMode);

                // Update UI
                toggle.Label.SetText(GetViewerLabel());
                CreateIconlessTitleBar(GetExtrasTitle());

                // Save the new setting
                SaveState();
            };
            ScrollView.AddButton(toggle);

            // Add button to open URL in browser
            var spreadsheetButton = PhoneUIUtility.CreateSimpleButton("Open Leaderboard\n<size=50%>Opens in your default browser!</size>");
            spreadsheetButton.OnConfirm += () =>
            {
                OpenSpreadsheetInBrowser();
            };
            ScrollView.AddButton(spreadsheetButton);

            // Export Save Data
            var exportButton = PhoneUIUtility.CreateSimpleButton("Export Save Data\n<size=50%>Back up your settings and records</size>");
            exportButton.OnConfirm += () =>
            {
                if (ScoreAttackEncounter.IsScoreAttackActive())
                {
                    Debug.LogError("Can't export save data during a battle!");
                    Core.Instance.UIManager.ShowNotification("You are in a run! Cancel your current run before exporting your save file.");
                }
                else
                {
                    string path = SaveManager.ExportSaveData();
                    Core.Instance.UIManager.ShowNotification($"Save Data Exported!\n<size=50%>{path}</size>");
                }
            };
            exportButton.LabelUnselectedColor = UnityEngine.Color.black;
            exportButton.LabelSelectedColor = UnityEngine.Color.red;
            ScrollView.AddButton(exportButton);

            // Import Save Data
            var importButton = PhoneUIUtility.CreateSimpleButton("Import Save Data\n<size=50%>Load settings config and records</size>");
            importButton.OnConfirm += () =>
            {
                if (ScoreAttackEncounter.IsScoreAttackActive())
                {
                    Debug.LogError("Can't import save data during a battle!");
                    Core.Instance.UIManager.ShowNotification("You are in a run! Cancel your current run before importing a new save file.");
                }
                else
                {
                    string path = SaveManager.ImportSaveData();
                    if (string.IsNullOrEmpty(path))
                    {
                        Core.Instance.UIManager.ShowNotification("No save file found to import.");
                    }
                    else
                    {
                        Core.Instance.UIManager.ShowNotification($"Save Data Imported!\n<size=50%>{path}</size>");
                    }
                }
            };
            importButton.LabelUnselectedColor = UnityEngine.Color.black;
            importButton.LabelSelectedColor = UnityEngine.Color.red;
            ScrollView.AddButton(importButton);

        }

        private static string GetViewerLabel()
        {
            string color = SFXMode == SFXToggle.Default ? "white" : "yellow";
            //return $"Grind Debt Viewer \n<size=50%><color={color}>{GetFormattedEnumName(SFXMode)}</color></size>";
            return $"Score Attack SFX \n<size=50%><color={color}>{GetFormattedEnumName(SFXMode)}</color></size>";
        }

        private static string GetFormattedEnumName(SFXToggle mode)
        {
            // Convert enum value to a user-friendly string
            switch (mode)
            {
                case SFXToggle.Default: return "Default SFX";
                case SFXToggle.DefaultPlus: return "Default Plus SFX";
                case SFXToggle.LLB: return "Lethal League Blaze SFX";
                case SFXToggle.FZero: return "F-Zero GX SFX";
                default: return mode.ToString();
            }
        }

        private static SFXToggle GetNextMode(SFXToggle current)
        {
            return (SFXToggle)(((int)current + 1) % System.Enum.GetValues(typeof(SFXToggle)).Length);
        }

        private void LoadState()
        {
            try
            {
                SFXMode = ScoreAttackSaveData.Instance.ExtraSFXMode;
            }
            catch
            {
                SFXMode = SFXToggle.Default;
            }
        }

        private void SaveState()
        {
            ScoreAttackSaveData.Instance.ExtraSFXMode = SFXMode;
        }

        private static string GetExtrasTitle()
        {
            return "Extras\n<size=50%>Experimental!</size>";
        }

        private void OpenSpreadsheetInBrowser()
        {
            string spreadsheetUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQPLU_jkIHBLmvsMqjotXKyeUZ2veHfseqzD_aMRI29b6Mb92c0o5l8WUdDPCp1s4xVwyLwgYHBiYa7/pubhtml";
            Application.OpenURL(spreadsheetUrl);
            Core.Instance.UIManager.ShowNotification("<size=75%><color=yellow>Vanilla Score Attack Leaderboard</color> opened in your default web browser! </size>\n<size=50%>You can also go to <color=yellow>sloppers.club/leaderboard</color> to view/submit vanilla score attack runs!</size>");
        }

    }
}
