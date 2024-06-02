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
using System.Numerics;

namespace ScoreAttack
{
    public class AppPolice : CustomApp
    {
        /// <summary>
        /// Don't show in home screen.
        /// </summary>
        public override bool Available => false;
        private WantedManager wantedManager;

        // This app just lets us remove the police.
        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppPolice>("remove police");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();
            CreateIconlessTitleBar("Wanted Level");
            ScrollView = PhoneScrollView.Create(this);

            var button = PhoneUIUtility.CreateSimpleButton("Remove Police");
            button.OnConfirm += () => {

                //Reset Wanted Status
                if (PlayerIsWanted())
                {
                    wantedManager.StopPlayerWantedStatus(true);
                }

                // Remove Police and Chains
                var player = WorldHandler.instance.GetCurrentPlayer();

                //Restore HP
                player.ResetHP();

                //Remove Cuffs
                if (player.AmountOfCuffs() > 0)
                {
                    player.RemoveAllCuffs();
                }
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("Enable Cops");
            button.OnConfirm += () => {
                // Enables Police

            };
            ScrollView.AddButton(button);
        }

        public void Update()
        {
            wantedManager = WantedManager.instance;
        }

        public bool PlayerIsWanted()
        {
            if (wantedManager != null) return wantedManager.Wanted;
            return false;
        }

        public void StopWantedStatus()
        {
            if (PlayerIsWanted())
            {
                Debug.Log("Stopping wanted status...");
                wantedManager.StopPlayerWantedStatus(true);
            }
        }


        /*
        public void StopWantedStatus()
        {
            if (PlayerIsWanted())
            {
                Debug.Log("Stopping wanted status...");
                wantedManager.StopPlayerWantedStatus(true);
            }
        }
        */

    }
}
