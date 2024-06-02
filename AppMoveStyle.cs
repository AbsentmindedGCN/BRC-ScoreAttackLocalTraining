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
using HarmonyLib;

namespace ScoreAttack
{
    public class AppMoveStyle : CustomApp
    {
        public override bool Available => false;

        private Player player;
        private WorldHandler world;
        private bool coreHasBeenSetup;

        // This app lets us change our move style on the fly
        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppMoveStyle>("move styles");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();
            CreateIconlessTitleBar("Change Style");
            ScrollView = PhoneScrollView.Create(this);

            // Initialize player and world variables
            player = WorldHandler.instance.GetCurrentPlayer();
            world = WorldHandler.instance;
            coreHasBeenSetup = player != null && world != null;

            var button = PhoneUIUtility.CreateSimpleButton("Skateboard");
            button.OnConfirm += () => {
                if (coreHasBeenSetup)
                {
                    SwapStyle(MoveStyle.SKATEBOARD);
                }
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("Inline Skates");
            button.OnConfirm += () => {
                if (coreHasBeenSetup)
                {
                    SwapStyle(MoveStyle.INLINE);
                }
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("BMX");
            button.OnConfirm += () => {
                if (coreHasBeenSetup)
                {
                    SwapStyle(MoveStyle.BMX);
                }
            };
            ScrollView.AddButton(button);
        }

        //From Yuri's Style Swap Mod, adapted to phone menu for easy access
        private void SwapStyle(MoveStyle newStyle)
        {
            if (player != null)
            {
                Ability currentAbility = (Ability)Traverse.Create(player).Field("ability").GetValue();
                GrindAbility playerGrindAbility = (GrindAbility)Traverse.Create(player).Field("grindAbility").GetValue();
                WallrunLineAbility playerWallrunAbility = (WallrunLineAbility)Traverse.Create(player).Field("wallrunAbility").GetValue();

                player.InitMovement(newStyle);

                if (currentAbility == playerGrindAbility)
                {
                    player.SwitchToEquippedMovestyle(true, false, true, true);
                }
                else if (currentAbility == playerWallrunAbility)
                {
                    player.SwitchToEquippedMovestyle(true, true, true, true);
                }
                else
                {
                    player.SwitchToEquippedMovestyle(true, true, true, true);
                }
            }
        }
    }
}

/*
namespace ScoreAttack
{
    public class AppMoveStyle : CustomApp
    {
        public override bool Available => false;

        public MoveStyle CurrentStyle = MoveStyle.ON_FOOT;

        private Player player;
        private Core core;
        private WorldHandler world;
        private bool coreHasBeenSetup;
        private bool delegateHasBeenSetup = false;

        // This app lets us change our move style on the fly
        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppMoveStyle>("move styles");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();
            CreateIconlessTitleBar("Change Style");
            ScrollView = PhoneScrollView.Create(this);

            // Initialize core, player, and world variables
            core = Core.Instance;
            world = WorldHandler.instance;
            coreHasBeenSetup = core != null && world != null;

            var button = PhoneUIUtility.CreateSimpleButton("Skateboard");
            button.OnConfirm += () => {
                // Set the new movestyle
                if (coreHasBeenSetup)
                {
                    if (player == null)
                    {
                        player = world.GetCurrentPlayer();
                    }
                    else
                    {
                        SwapStyle(MoveStyle.SKATEBOARD);
                    }
                }
                //var player = world.GetCurrentPlayer();
                //player.SetCurrentMoveStyleEquipped(MoveStyle.SKATEBOARD);
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("Inline Skates");
            button.OnConfirm += () => {

                // Set the new movestyle
                if (coreHasBeenSetup)
                {
                    if (player == null)
                    {
                        player = world.GetCurrentPlayer();
                    }
                    else
                    {
                        SwapStyle(MoveStyle.INLINE);
                    }
                }
                //var player = world.GetCurrentPlayer();
                //player.SetCurrentMoveStyleEquipped(MoveStyle.INLINE);
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("BMX");
            button.OnConfirm += () => {
                // Set the new movestyle
                if (coreHasBeenSetup)
                {
                    if (player == null)
                    {
                        player = world.GetCurrentPlayer();
                    }
                    else
                    {
                        SwapStyle(MoveStyle.BMX);
                    }
                }
                //var player = world.GetCurrentPlayer();
                //player.SetCurrentMoveStyleEquipped(MoveStyle.BMX);
            };
            ScrollView.AddButton(button);

        }

        private void Update()
        {
            if (!coreHasBeenSetup)
            {
                core = Core.Instance;
                if (core != null)
                {
                    // Attempt to set up core, player, and world variables if not already set up
                    core = Core.Instance;
                    world = WorldHandler.instance;
                    coreHasBeenSetup = core != null && world != null;

                    if (!delegateHasBeenSetup)
                    {
                        StageManager.OnStageInitialized += () =>
                        {
                            coreHasBeenSetup = false;
                        };
                        delegateHasBeenSetup = true;
                    }
                }
            }
        }

        //From Yuri's Style Swap Mod, adapted to phone menu for easy access
        private void SwapStyle(MoveStyle NewStyle)
        {

            Ability currentAbility = (Ability)Traverse.Create(player).Field("ability").GetValue();
            GrindAbility playerGrindAbility = (GrindAbility)Traverse.Create(player).Field("grindAbility").GetValue();
            WallrunLineAbility playerWallrunAbility = (WallrunLineAbility)Traverse.Create(player).Field("wallrunAbility").GetValue();

            player.InitMovement(NewStyle);

            if (currentAbility == playerGrindAbility)
            {
                player.SwitchToEquippedMovestyle(true, false, true, true);
            }

            else if (currentAbility == playerWallrunAbility)
            {
                player.SwitchToEquippedMovestyle(true, true, true, true);
            }

            else
            {
                player.SwitchToEquippedMovestyle(true, true, true, true);
            }
        }

    }
}
*/