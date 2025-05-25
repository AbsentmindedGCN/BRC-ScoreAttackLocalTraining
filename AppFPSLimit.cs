using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using CommonAPI;
using CommonAPI.Phone;
using Reptile;
using UnityEngine;
using UnityEngine.Playables;
using System.Threading;

namespace ScoreAttack
{
    public class AppFPSLimit : CustomApp
    {
        public override bool Available => false;
        public bool LimitFramerate = false;
        public int FpsLimit = 60;
        public int CurrentFps = 60;

        private float refreshTimer = 0f;
        private string lastTitle = "";

        public static AppFPSLimit Instance;


        // This app lets us change the FPS limit of the game
        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppFPSLimit>("fps select");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();
            Instance = this;
            CreateIconlessTitleBar(GetFpsTitle());  // Call the function that gets the dynamic FPS title
            //CreateIconlessTitleBar("FPS Limit\n<size=50%>Current Limit = " + Application.targetFrameRate + "</size>");
            //CreateIconlessTitleBar("FPS Limit\n<size=50%>Current Limit = " + Application.targetFrameRate + "</size>");
            //QualitySettings.onVSyncChanged += OnVSyncChanged; // Refresh the title bar whenever V-Sync is changed
            
            // This is slow
            //InvokeRepeating("RefreshVSyncStatus", 0f, 0.5f);  // Refresh every half second because I'm a goober and idk how to do this right

            if (GameObject.Find("VSyncWatcher") == null)
            {
                var watcher = new GameObject("VSyncWatcher");
                GameObject.DontDestroyOnLoad(watcher);
                watcher.AddComponent<VSyncWatcher>();
            }

            ScrollView = PhoneScrollView.Create(this);

            var button = PhoneUIUtility.CreateSimpleButton("30 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(30);
                //Application.targetFrameRate = 30;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(30);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 30! \nMake sure " + GetColoredVSyncText() + " is off for it to go into effect.");
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("40 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(40);
                //Application.targetFrameRate = 40;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(40);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 40! \nMake sure " + GetColoredVSyncText() + " is off for it to go into effect.");
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("60 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(60);
                //Application.targetFrameRate = 60;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(60);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 60! \nMake sure " + GetColoredVSyncText() + " is off for it to go into effect.");
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("90 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(90);
                //Application.targetFrameRate = 90;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(90);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 90! \nMake sure " + GetColoredVSyncText() + " is off for it to go into effect.");
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("120 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(120);
                //Application.targetFrameRate = 120;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(120);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 120! \nMake sure " + GetColoredVSyncText() + " is off for it to go into effect.");
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("144 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(144);
                //Application.targetFrameRate = 144;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(144);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 144! \nMake sure " + GetColoredVSyncText() + " is off for it to go into effect.");
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("240 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(240);
                //Application.targetFrameRate = 240;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(240);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 240! \nMake sure " + GetColoredVSyncText() + " is off for it to go into effect.");

            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("Unlimited");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.targetFrameRate = -1;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(-1);
                Core.Instance.UIManager.ShowNotification("Your FPS has no limits! \nMake sure " + GetColoredVSyncText() + " is off for it to go into effect.");

            };
            ScrollView.AddButton(button);

        }

        void SetTargetFrameRate(int frameRate)
        {
            Application.targetFrameRate = frameRate;
            ScoreAttackSaveData.Instance.TargetFrameRate = frameRate; // Save target frame rate

            // Update the title bar dynamically
            CreateIconlessTitleBar(GetFpsTitle());
        }

        // Periodically checks V-Sync status, useful for refreshing the title bar
        private void RefreshVSyncStatus()
        {
            // Force update of the title bar to reflect any changes in V-Sync status
            //CreateIconlessTitleBar(GetFpsTitle());

            string newTitle = GetFpsTitle();

            if (newTitle != lastTitle)
            {
                lastTitle = newTitle;
                CreateIconlessTitleBar(newTitle);
            }
        }

        private string GetFpsTitle()
        {
            // Get the saved target frame rate (FPS limit)
            int savedLimit = ScoreAttackSaveData.Instance.TargetFrameRate;

            string label;

            // If no value has been saved (0), use Application.targetFrameRate instead
            if (savedLimit == 0)
            {
                savedLimit = Application.targetFrameRate;
                // Save it so it doesn't show 0 next time
                ScoreAttackSaveData.Instance.TargetFrameRate = savedLimit;
            }

            // If savedLimit is -1 (Unlimited), show a string instead of a number
            //string label = savedLimit == -1 ? "None, baby!" : savedLimit + " fps";

            if (savedLimit == 0)
            {
                label = "No Limit Set";
            }
            else if (savedLimit == -1)
            {
                label = "None, baby!";
            }
            else
            {
                label = savedLimit + " fps";
            }

            //return $"FPS Limit\n<size=50%>Limit = {label}</size>";

            // Check V-Sync status
            string vsyncStatus = QualitySettings.vSyncCount > 0 ? "V-Sync = On" : "V-Sync = Off";

            // Return the title with FPS limit on one line and V-Sync status on another
            return $"FPS Limit\n<size=50%>Limit = {label}</size>\n<size=50%>{vsyncStatus}</size>";
        }

        public static string StaticGetFpsTitle()
        {
            int savedLimit = ScoreAttackSaveData.Instance.TargetFrameRate;
            if (savedLimit == 0)
                savedLimit = Application.targetFrameRate;

            string label = savedLimit == -1 ? "None, baby!" :
                           savedLimit == 0 ? "No Limit Set" : savedLimit + " fps";

            string vsyncStatus = QualitySettings.vSyncCount > 0 ? "V-Sync = On" : "V-Sync = Off";
            return $"FPS Limit\n<size=50%>Limit = {label}</size>\n<size=50%>{vsyncStatus}</size>";
        }


        private void OnUpdate()
        {

            //base.OnUpdate();
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= 0.5f)
            {
                refreshTimer = 0f;
                RefreshVSyncStatus();
            }
        }


        public class VSyncWatcher : MonoBehaviour
        {
            private float timer = 0f;
            private string lastTitle = "";

            void Update()
            {
                timer += Time.deltaTime;
                if (timer >= 0.5f)
                {
                    timer = 0f;
                    string newTitle = AppFPSLimit.StaticGetFpsTitle(); // Needs to be made static
                    if (newTitle != lastTitle)
                    {
                        lastTitle = newTitle;
                        if (AppFPSLimit.Instance != null)
                        {
                            AppFPSLimit.Instance.ForceTitleRefresh(newTitle);
                        }
                    }
                }
            }
        }

        public void ForceTitleRefresh(string newTitle)
        {
            CreateIconlessTitleBar(newTitle);
        }

        private string GetColoredVSyncText()
        {
            bool vsyncOn = QualitySettings.vSyncCount > 0;
            string color = vsyncOn ? "red" : "green";
            return $"<color={color}>V-Sync</color>";
        }

    }
}
