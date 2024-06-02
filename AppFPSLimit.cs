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

namespace ScoreAttack
{
    public class AppFPSLimit : CustomApp
    {
        public override bool Available => false;
        public bool LimitFramerate = false;
        public int FpsLimit = 60;
        public int CurrentFps = 60;

        // This app lets us change the FPS limit of the game
        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppFPSLimit>("fps select");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();
            CreateIconlessTitleBar("FPS Limit");
            ScrollView = PhoneScrollView.Create(this);

            var button = PhoneUIUtility.CreateSimpleButton("30 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(30);
                //Application.targetFrameRate = 30;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(30);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 30! Make sure VSync is off for it to go into effect.");
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("40 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(40);
                //Application.targetFrameRate = 40;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(40);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 40! Make sure VSync is off for it to go into effect.");
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("60 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(60);
                //Application.targetFrameRate = 60;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(60);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 60! Make sure VSync is off for it to go into effect.");
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("90 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(90);
                //Application.targetFrameRate = 90;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(90);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 90! Make sure VSync is off for it to go into effect.");
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("120 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(120);
                //Application.targetFrameRate = 120;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(120);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 120! Make sure VSync is off for it to go into effect.");
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("144 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(144);
                //Application.targetFrameRate = 144;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(144);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 144! Make sure VSync is off for it to go into effect.");
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("240 fps");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.SetTargetFrameRate(240);
                //Application.targetFrameRate = 240;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(240);
                Core.Instance.UIManager.ShowNotification("FPS Limit has been set to 240! Make sure VSync is off for it to go into effect.");
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("Unlimited");
            button.OnConfirm += () => {
                // Set the new framerate
                //Application.targetFrameRate = -1;
                //CurrentFps = Application.targetFrameRate;

                SetTargetFrameRate(-1);
                Core.Instance.UIManager.ShowNotification("Your FPS has no limits! Make sure VSync is off for it to go into effect.");

            };
            ScrollView.AddButton(button);

        }

        void SetTargetFrameRate(int frameRate)
        {
            Application.targetFrameRate = frameRate;
            ScoreAttackSaveData.Instance.TargetFrameRate = frameRate; // Save target frame rate
        }

    }
}
