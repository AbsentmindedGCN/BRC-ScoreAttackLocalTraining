using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommonAPI;
using CommonAPI.Phone;
using Reptile;
using UnityEngine;

namespace ScoreAttack
{
    // Cheats app from LazyDuchess - Shows up in homescreen. Lets us do misc things for Score Attack.
    public class AppScoreAttackOffline : CustomApp
    {
        private static Sprite IconSprite = null;
        private PhoneButton goToRespawnButton = null;
        private WantedManager wantedManager;

        // Load the icon for this app and register it with the PhoneAPI, so that it shows up on the homescreen.
        public static void Initialize()
        {
            IconSprite = TextureUtility.LoadSprite(Path.Combine(ScoreAttackPlugin.Instance.Directory, "scoreIcon.png"));
            PhoneAPI.RegisterApp<AppScoreAttackOffline>("score atk", IconSprite);
        }

        // Add or remove the Go to respawn button from the menu, depending on if the current stage has a respawn set or not.
        public void UpdateGoToRespawnButton()
        {
            var stage = Core.Instance.BaseModule.CurrentStage;
            var respawnPoint = ScoreAttackSaveData.Instance.GetRespawnPoint(stage);
            if (respawnPoint == null)
            {
                if (ScrollView.HasButton(goToRespawnButton))
                    ScrollView.RemoveButton(goToRespawnButton);
            }
            else
            {
                if (!ScrollView.HasButton(goToRespawnButton))
                    ScrollView.AddButton(goToRespawnButton);
            }
        }

        public override void OnAppEnable()
        {
            base.OnAppEnable();
            UpdateGoToRespawnButton();
        }

        public override void OnAppInit()
        {
            base.OnAppInit();
            CreateTitleBar("Score Attack", IconSprite);
            ScrollView = PhoneScrollView.Create(this);

            var button = PhoneUIUtility.CreateSimpleButton("Start a Run...");
            button.OnConfirm += () => {
                //Launch Score Attack App
                MyPhone.OpenApp(typeof(AppScoreAttack));
                //MyPhone.OpenApp(typeof(AppStageSelect));
            };
            ScrollView.AddButton(button);

            //If score battle is active, add a button to cancel here

            // Enable/Disable new ghost function and settings
            button = PhoneUIUtility.CreateSimpleButton("Ghost Replays...");
            button.OnConfirm += () => {
                // Launch Ghost Settings app.
                MyPhone.OpenApp(typeof(AppGhost));
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("Go to Stage...");
            button.OnConfirm += () => {
                // Launch our stage select app.
                MyPhone.OpenApp(typeof(AppScoreAttackStageSelect));
            };
            ScrollView.AddButton(button);

            //Add an option to switch move styles via the phone
            button = PhoneUIUtility.CreateSimpleButton("Change Style...");
            button.OnConfirm += () => {
                // Launch our move style changer app.
                MyPhone.OpenApp(typeof(AppMoveStyle));
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("Refill Boost");
            button.OnConfirm += () => {
                var player = WorldHandler.instance.GetCurrentPlayer();
                player.boostCharge = player.maxBoostCharge;
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("Reset Capsules");
            button.OnConfirm += () => {

                //Restore boost capsules so they can be used in Score Attack runs
                Debug.Log("Respawning Pickups...");
                UnityEngine.Object.FindObjectsOfType<Pickup>()
                .Where(pickup => pickup.pickupType is Pickup.PickUpType.BOOST_CHARGE or Pickup.PickUpType.BOOST_BIG_CHARGE
                                     && pickup.pickupObject != null)
                .ToList()
                .ForEach(boost => { boost.SetPickupActive(true); });

                //Also restore vending so they can be used in Score Attack runs
                Debug.Log("Restoring Vending Machines...");

                var vendingMachines = UnityEngine.Object.FindObjectsOfType<VendingMachine>()
                .Where(vending => vending.rewardCount > 0)
                .ToList();
                foreach (var vending in vendingMachines)
                {
                    if (vending.dropCount > 0)
                    {
                        vending.dropCount = 0;
                    }
                }
                foreach (var vending in vendingMachines)
                {
                    if (vending.rewardCount > 0)
                    {
                        vending.rewardCount = 0;
                    }
                }
                foreach (var vending in vendingMachines)
                {
                    if (vending.firstHit == true)
                    {
                        vending.firstHit = false;
                    }
                }

            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("Reset Cops");
            button.OnConfirm += () => {
                // Launch FPS Limit app. - This doesn't need to be a phone app
                //MyPhone.OpenApp(typeof(AppPolice));

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

                /*
                // Clear Cuffs
                var playerCuffs = UnityEngine.Object.FindObjectsOfType<PlayerCuff>()
                .Where(cuff => cuff.GetState() != PlayerCuff.CuffState.Hidden);
                foreach (var cuff in playerCuffs)
                {
                    cuff.SetState(PlayerCuff.CuffState.Hidden);
                }
                */

            };
            ScrollView.AddButton(button);

            // Clear or See Current Grind Debt Values
            button = PhoneUIUtility.CreateSimpleButton("Grind Debt...");
            button.OnConfirm += () => {
                // Launch Grind Debt Viewer app.
                MyPhone.OpenApp(typeof(AppGrindDebt));
            };
            ScrollView.AddButton(button);

            // Manually set FPS Limit
            button = PhoneUIUtility.CreateSimpleButton("Set FPS Limit...");
            button.OnConfirm += () => {
                // Launch FPS Limit app.
                MyPhone.OpenApp(typeof(AppFPSLimit));
            };
            ScrollView.AddButton(button);

            // Configure Extras
            button = PhoneUIUtility.CreateSimpleButton("Extras...");
            button.OnConfirm += () => {
                // Launch Extras app.
                MyPhone.OpenApp(typeof(AppExtras));
            };
            ScrollView.AddButton(button);

            // Set up respawn point for Score Attack practice
            button = PhoneUIUtility.CreateSimpleButton("Set Respawn");
            button.OnConfirm += () =>
            {
                var stage = Core.Instance.BaseModule.CurrentStage;
                var position = MyPhone.player.transform.position;
                var rotation = MyPhone.player.transform.rotation;
                var gear = MyPhone.player.usingEquippedMovestyle;
                ScoreAttackSaveData.Instance.SetRespawnPoint(stage, position, rotation, gear);
                UpdateGoToRespawnButton();
            };
            ScrollView.AddButton(button);

            // We conditionally add this button depending on whether the stage has a respawn saved or not.
            goToRespawnButton = PhoneUIUtility.CreateSimpleButton("Go to Respawn");
            goToRespawnButton.OnConfirm += () =>
            {
                var stage = Core.Instance.BaseModule.CurrentStage;
                var respawnPoint = ScoreAttackSaveData.Instance.GetRespawnPoint(stage);
                if (respawnPoint == null) return;
                respawnPoint.ApplyToPlayer(MyPhone.player);
            };

        }

        //Police and Wanted System, removes police that spawn after graffiti is used, that way runs can restart from a normal game state
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
                wantedManager.StopPlayerWantedStatus(true);
            }
        }


    }
}
