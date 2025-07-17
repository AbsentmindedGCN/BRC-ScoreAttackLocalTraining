using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonAPI;
using CommonAPI.Phone;
using Reptile;
using Reptile.Phone;
using UnityEngine;

namespace ScoreAttack
{
    public class AppScoreAttack : CustomApp
    {
        public override bool Available => false;
        public static Encounter globalEncounter;

        private PhoneButton goToRespawnButton = null;
        private WantedManager wantedManager;

        public bool testingPlugin = false; // If testing, allow the use of 1 minute score attacks

        //public readonly Sprite EncounterIcon = TextureUtility.LoadSprite("encounter.png"); // 128x128

        // This phone app allows the player to select between 3, 5, or 10 minute timed score attack sessions
        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppScoreAttack>("score atk");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();
            CreateIconlessTitleBar("Select Length");
            ScrollView = PhoneScrollView.Create(this);

            var button = PhoneUIUtility.CreateSimpleButton("3 Minutes");
            button.OnConfirm += () =>
            {
                // New Encounter
                Debug.Log("Starting 3 Minute Score Battle...");
                
                // Stop Plugin from working if TrickGod is enabled.
                if (BannedMods.IsAdvantageousModLoaded())
                {
                    Core.Instance.UIManager.ShowNotification("The score attack app is not compatible with TrickGod!");
                    return;
                }

                //Force stop previous battle, so partial replays and PBs are saved
                bool isScoreBattleActive = ScoreAttackEncounter.IsScoreAttackActive();
                if (isScoreBattleActive)
                {
                    // End the Score Battle
                    ScoreAttackEncounter scoreAttackActiveEncounter = FindObjectOfType<ScoreAttackEncounter>();
                    scoreAttackActiveEncounter.EndScoreAttack();

                    //Save on ending
                    Core.Instance.SaveManager.SaveCurrentSaveSlot();
                }

                // Refresh Boost, Cops, and More
                BattleRefresh();

                // Let the encounter show player PB and not ghost PB
                ScoreAttackManager.ExternalGhostLoadedFromGhostList = false;

                // Respawn Player, if respawn exists
                var stage = Core.Instance.BaseModule.CurrentStage;
                var respawnPoint = ScoreAttackSaveData.Instance.GetRespawnPoint(stage);
                if (respawnPoint == null)
                {
                    Core.Instance.UIManager.ShowNotification("Please set a respawn point first!\n<size=50%>It's at the bottom of the app menu!</size>");
                    return;
                }
                else
                {
                    respawnPoint.ApplyToPlayer(MyPhone.player);

                    ScoreAttackManager.StartScoreAttack(60f * 3f);
                    MyPhone.CloseCurrentApp();
                    MyPhone.TurnOff();
                }

                ScoreAttackManager.StartScoreAttack(60f * 3f);
                MyPhone.CloseCurrentApp();
                MyPhone.TurnOff();

            };
            
            //button.SelectedButtonSprite = EncounterIcon;
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("5 Minutes");
            button.OnConfirm += () =>
            {
                // New Encounter
                Debug.Log("Starting 5 Minute Score Battle...");

                //Force stop previous battle, so partial replays and PBs are saved
                bool isScoreBattleActive = ScoreAttackEncounter.IsScoreAttackActive();
                if (isScoreBattleActive)
                {
                    // End the Score Battle
                    ScoreAttackEncounter scoreAttackActiveEncounter = FindObjectOfType<ScoreAttackEncounter>();
                    scoreAttackActiveEncounter.EndScoreAttack();

                    //Save on ending
                    Core.Instance.SaveManager.SaveCurrentSaveSlot();
                }

                // Refresh Boost, Cops, and More
                BattleRefresh();

                // Let the encounter show player PB and not ghost PB
                ScoreAttackManager.ExternalGhostLoadedFromGhostList = false;

                // Stop Plugin from working if TrickGod is enabled.
                if (BannedMods.IsAdvantageousModLoaded())
                {
                    Core.Instance.UIManager.ShowNotification("The score attack app is not compatible with TrickGod!");
                    return;
                }

                // Respawn Player, if respawn exists
                var stage = Core.Instance.BaseModule.CurrentStage;
                var respawnPoint = ScoreAttackSaveData.Instance.GetRespawnPoint(stage);
                if (respawnPoint == null)
                {
                    Core.Instance.UIManager.ShowNotification("Please set a respawn point first!\n<size=50%>It's at the bottom of the app menu!</size>");
                    return;
                }
                else
                {
                    respawnPoint.ApplyToPlayer(MyPhone.player);

                    ScoreAttackManager.StartScoreAttack(60f * 5f);
                    MyPhone.CloseCurrentApp();
                    MyPhone.TurnOff();
                }

                ScoreAttackManager.StartScoreAttack(60f * 5f);
                MyPhone.CloseCurrentApp();
                MyPhone.TurnOff();
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("10 Minutes");
            button.OnConfirm += () =>
            {
                // New Encounter
                Debug.Log("Starting 10 Minute Score Battle...");

                //Force stop previous battle, so partial replays and PBs are saved
                bool isScoreBattleActive = ScoreAttackEncounter.IsScoreAttackActive();
                if (isScoreBattleActive)
                {
                    // End the Score Battle
                    ScoreAttackEncounter scoreAttackActiveEncounter = FindObjectOfType<ScoreAttackEncounter>();
                    scoreAttackActiveEncounter.EndScoreAttack();

                    //Save on ending
                    Core.Instance.SaveManager.SaveCurrentSaveSlot();
                }

                // Refresh Boost, Cops, and More
                BattleRefresh();

                // Let the encounter show player PB and not ghost PB
                ScoreAttackManager.ExternalGhostLoadedFromGhostList = false;

                // Stop Plugin from working if TrickGod is enabled.
                if (BannedMods.IsAdvantageousModLoaded())
                {
                    Core.Instance.UIManager.ShowNotification("The score attack app is not compatible with TrickGod!");
                    return;
                }

                // Respawn Player, if respawn exists
                var stage = Core.Instance.BaseModule.CurrentStage;
                var respawnPoint = ScoreAttackSaveData.Instance.GetRespawnPoint(stage);
                if (respawnPoint == null)
                {
                    Core.Instance.UIManager.ShowNotification("Please set a respawn point first!\n<size=50%>It's at the bottom of the app menu!</size>");
                    return;
                }
                else
                {
                    respawnPoint.ApplyToPlayer(MyPhone.player);

                    ScoreAttackManager.StartScoreAttack(60f * 10f);
                    MyPhone.CloseCurrentApp();
                    MyPhone.TurnOff();
                }

                ScoreAttackManager.StartScoreAttack(60f * 10f);
                MyPhone.CloseCurrentApp();
                MyPhone.TurnOff();

            };
            ScrollView.AddButton(button);

            // --- FOR TESTING PURPOSES ---
            if (testingPlugin == true)
            {
                button = PhoneUIUtility.CreateSimpleButton("1 Minute");
                button.OnConfirm += () =>
                {
                    // New Encounter
                    Debug.Log("Starting 1 Minute Score Battle...");

                    //Force stop previous battle, so partial replays and PBs are saved
                    bool isScoreBattleActive = ScoreAttackEncounter.IsScoreAttackActive();
                    if (isScoreBattleActive)
                    {
                        // End the Score Battle
                        ScoreAttackEncounter scoreAttackActiveEncounter = FindObjectOfType<ScoreAttackEncounter>();
                        scoreAttackActiveEncounter.EndScoreAttack();

                        //Save on ending
                        Core.Instance.SaveManager.SaveCurrentSaveSlot();
                    }

                    // Refresh Boost, Cops, and More
                    BattleRefresh();

                    // Let the encounter show player PB and not ghost PB
                    ScoreAttackManager.ExternalGhostLoadedFromGhostList = false;

                    // Stop Plugin from working if TrickGod is enabled.
                    if (BannedMods.IsAdvantageousModLoaded())
                    {
                        Core.Instance.UIManager.ShowNotification("The score attack app is not compatible with TrickGod!");
                        return;
                    }

                    // Respawn Player, if respawn exists
                    var stage = Core.Instance.BaseModule.CurrentStage;
                    var respawnPoint = ScoreAttackSaveData.Instance.GetRespawnPoint(stage);
                    if (respawnPoint == null)
                    {
                        Core.Instance.UIManager.ShowNotification("Please set a respawn point first!\n<size=50%>It's at the bottom of the app menu!</size>");
                        return;
                    }
                    else
                    {
                        respawnPoint.ApplyToPlayer(MyPhone.player);

                        ScoreAttackManager.StartScoreAttack(60f * 1f);
                        MyPhone.CloseCurrentApp();
                        MyPhone.TurnOff();
                    }

                    ScoreAttackManager.StartScoreAttack(60f * 1f);
                    MyPhone.CloseCurrentApp();
                    MyPhone.TurnOff();

                };
                ScrollView.AddButton(button);
            }
            // --- wooo ---


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

            button = PhoneUIUtility.CreateSimpleButton("Erase All Your Personal Bests");
            button.OnConfirm += () =>
            {
                // New Encounter
                Debug.Log("Clearing Personal Bests...");

                ScoreAttackEncounter scoreAttackEncounter = FindObjectOfType<ScoreAttackEncounter>();
                if (scoreAttackEncounter != null)
                {
                    if (ScoreAttackEncounter.IsScoreAttackActive())
                    {
                        Debug.LogError("Can't clear PB during a run!");
                        Core.Instance.UIManager.ShowNotification("You are in a run! Cancel the run to clear your personal bests.");
                    }
                    else
                    {
                        scoreAttackEncounter.ClearPersonalBest();
                        Core.Instance.UIManager.ShowNotification("Personal bests have been erased.");
                    }
                    
                }
                else
                {
                    Debug.LogError("ScoreAttackEncounter instance not found!");
                }

            };
            button.LabelUnselectedColor = UnityEngine.Color.black;
            button.LabelSelectedColor = UnityEngine.Color.red;
            ScrollView.AddButton(button);

        }


        public static void InitializeEncounter()
    {
        globalEncounter = FindObjectOfType<Encounter>();
        if (globalEncounter == null)
        {
            Debug.LogError("Error: Encounter object not found!");
        }
    }

        public static void BattleRefresh()
        {
            // Let's Refresh Everything so it's an even playing field

            // Give Full Boost
            var player = WorldHandler.instance.GetCurrentPlayer();
            player.boostCharge = player.maxBoostCharge;

            // Capsules
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

        }

    }
}
