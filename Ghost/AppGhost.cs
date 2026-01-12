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
                    scoreAttackActiveEncounter.isCancelling = true; // Skip Delay
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