using CommonAPI.Phone;
using Reptile;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;

namespace ScoreAttack
{
    public enum GrindDebtDisplayMode
    {
        Disabled,           // Disabled
        Bar,                // Boost Bar
        Value,              // Numerical Value
        Both,               // Bar + Value
        SoftGoat            // Softgoat Bar + Value
    }

    public enum GrindDebtColorMode
    {
        Flashing,
        Red,
        White
    }

    public class AppGrindDebt : CustomApp
    {
        public override bool Available => false;

        public static GrindDebtDisplayMode ViewerMode { get; private set; } = GrindDebtDisplayMode.Disabled;
        public static GrindDebtColorMode ColorMode { get; private set; } = GrindDebtColorMode.Flashing;

        //private float refreshTimer = 0f;
        //private float refreshCooldown = 0.25f; // Faster check, less frequent redraws
        //private string lastGrindDebtString = "";

        private float refreshCooldown = 0.25f;
        private float refreshTimer = 0f;
        private bool hadDebt = false;

        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppGrindDebt>("grind debt");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();

            LoadState();

            CreateIconlessTitleBar(GetGrindDebtString());
            //InvokeRepeating(nameof(RefreshGrindDebtTitle), 0f, 0.5f); // This slows down the game when the app is open...

            // Monitor Changes
            gameObject.AddComponent<GrindDebtMonitor>().Init(this);

            ScrollView = PhoneScrollView.Create(this);

            var clear = PhoneUIUtility.CreateSimpleButton("Clear Grind Debt");
            clear.OnConfirm += () => {
                var p = WorldHandler.instance.GetCurrentPlayer();
                p.grindAbility.trickTimer = 0f;
                p.RefreshAllDegrade();
                CreateIconlessTitleBar(GetGrindDebtString());
                hadDebt = false; // Reset internal state
            };
            ScrollView.AddButton(clear);

            var toggle = PhoneUIUtility.CreateSimpleButton(GetViewerLabel());
            toggle.OnConfirm += () => {
                ViewerMode = GetNextMode(ViewerMode);
                toggle.Label.SetText(GetViewerLabel());
                CreateIconlessTitleBar(GetGrindDebtString());
                SaveState();
            };
            ScrollView.AddButton(toggle);

            var colorToggle = PhoneUIUtility.CreateSimpleButton(GetColorLabel());
            colorToggle.OnConfirm += () => {
                ColorMode = GetNextColorMode(ColorMode);
                colorToggle.Label.SetText(GetColorLabel());
                SaveState();
            };
            ScrollView.AddButton(colorToggle);
        }

        /*
        public override void Update()
        {
            base.Update();

            if (!IsActive) return;

            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = refreshCooldown;

                float debt = GetTrickTimer();
                bool hasDebtNow = debt > 0.2f;

                if (hasDebtNow != hadDebt)
                {
                    hadDebt = hasDebtNow;
                    CreateIconlessTitleBar(GetGrindDebtString());
                }
            }
        }
        */

        private static string GetViewerLabel()
        {
            string color = ViewerMode == GrindDebtDisplayMode.Disabled ? "red" : "green";
            return $"Grind Debt Viewer \n<size=50%><color={color}>{GetFormattedEnumName(ViewerMode)}</color></size>";
        }

        /*
        private static string GetColorLabel()
        {
            return $"Grind Debt Color \n<size=50%><color=orange>{ColorMode}</color></size>";
        }
        */

        private static string GetFormattedEnumName(GrindDebtDisplayMode mode)
        {
            // Convert enum value to a user-friendly string
            switch (mode)
            {
                case GrindDebtDisplayMode.Disabled: return "Disabled";
                case GrindDebtDisplayMode.Bar: return "Boost Bar";
                case GrindDebtDisplayMode.Value: return "Numerical Value";
                case GrindDebtDisplayMode.Both: return "Bar + Value";
                case GrindDebtDisplayMode.SoftGoat: return "SoftGoat Speedometer Preset";
                default: return mode.ToString();
            }
        }

        private static string GetColorLabel()
        {
            return $"Viewer Behavior \n<size=50%><color=orange>{GetFormattedEnumName(ColorMode)}</color></size>";
        }

        private static string GetFormattedEnumName(GrindDebtColorMode mode)
        {
            switch (mode)
            {
                case GrindDebtColorMode.Flashing: return "Flash When You Have Debt";
                case GrindDebtColorMode.Red: return "Solid Colors";
                case GrindDebtColorMode.White: return "All White";
                default: return mode.ToString();
            }
        }


        private static GrindDebtDisplayMode GetNextMode(GrindDebtDisplayMode current)
        {
            int next = ((int)current + 1) % 5; // was 4 for main 3, set to 6 for Dragsun and SoftGoat
            return (GrindDebtDisplayMode)next;
        }

        private static GrindDebtColorMode GetNextColorMode(GrindDebtColorMode current)
        {
            int next = ((int)current + 1) % 3;
            return (GrindDebtColorMode)next;
        }

        public static float GetTrickTimer()
        {
            var player = WorldHandler.instance.GetCurrentPlayer();
            if (player == null || player.grindAbility == null)
            {
                return 0f;  // Default to 0 if no player or grindAbility is found
            }
            return player.grindAbility.trickTimer;
        }


        private void LoadState()
        {
            ViewerMode = ScoreAttackSaveData.Instance.GrindDebtViewerMode;
            ColorMode = ScoreAttackSaveData.Instance.GrindDebtColorMode;
        }

        private void SaveState()
        {
            ScoreAttackSaveData.Instance.GrindDebtViewerMode = ViewerMode;
            ScoreAttackSaveData.Instance.GrindDebtColorMode = ColorMode;
        }

        private static string GetGrindDebtString()
        {
            var player = WorldHandler.instance.GetCurrentPlayer();

            if (player == null || player.grindAbility == null)
                return "Grind Debt\n<size=50%>Checking...</size>";

            float debt = player.grindAbility.trickTimer;

            // Truncate the value to 1 decimal place (same as before)
            float truncatedDebt = Mathf.Floor(debt * 10) / 10f;

            if (truncatedDebt >= 0.2f)
                return "Grind Debt\n<size=50%><color=red>Pay your debts...</color></size>";
            else
                return "Grind Debt\n<size=50%>No debt here!</size>";
        }

        public class GrindDebtMonitor : MonoBehaviour
        {
            private AppGrindDebt app;
            private float refreshCooldown = 0.25f;
            private float refreshTimer = 0f;
            private bool hadDebt = false;

            public void Init(AppGrindDebt appRef)
            {
                app = appRef;
            }

            void Update()
            {
                if (app == null)
                    return;

                refreshTimer -= Time.deltaTime;
                if (refreshTimer <= 0f)
                {
                    refreshTimer = refreshCooldown;

                    float debt = AppGrindDebt.GetTrickTimer();
                    bool hasDebtNow = debt >= 0.2f;

                    if (hasDebtNow != hadDebt)
                    {
                        hadDebt = hasDebtNow;
                        app.CreateIconlessTitleBar(AppGrindDebt.GetGrindDebtString());
                    }
                }
            }
        }

        /*
        private void RefreshGrindDebtTitle()
        {
            CreateIconlessTitleBar(GetGrindDebtString());
        }
        */

    }
}
