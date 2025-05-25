using System;
using System.Collections.Generic;
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

        private MoveStyle currentStyle;
        private Dictionary<MoveStyle, int> skinIndices = new Dictionary<MoveStyle, int>();

        private int characterSkinIndex = 0;

        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppMoveStyle>("move styles");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();
            CreateIconlessTitleBar("Change Style");
            ScrollView = PhoneScrollView.Create(this);

            player = WorldHandler.instance.GetCurrentPlayer();
            world = WorldHandler.instance;
            coreHasBeenSetup = player != null && world != null;

            var button = PhoneUIUtility.CreateSimpleButton("Skateboard");
            button.OnConfirm += () =>
            {
                if (coreHasBeenSetup)
                {
                    SwapStyle(MoveStyle.SKATEBOARD);
                }
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("Inline Skates");
            button.OnConfirm += () =>
            {
                if (coreHasBeenSetup)
                {
                    SwapStyle(MoveStyle.INLINE);
                }
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("BMX");
            button.OnConfirm += () =>
            {
                if (coreHasBeenSetup)
                {
                    SwapStyle(MoveStyle.BMX);
                }
            };
            ScrollView.AddButton(button);

            button = PhoneUIUtility.CreateSimpleButton("Swap Style Skin\n<size=50%>Change how your gear looks!</size>");
            button.OnConfirm += () =>
            {
                if (coreHasBeenSetup)
                {
                    CycleSkinForCurrentStyle();
                }
            };
            ScrollView.AddButton(button);

            // Swap character outfit
            button = PhoneUIUtility.CreateSimpleButton("Swap Outfit\n<size=50%>Change your clothes!</size>");
            button.OnConfirm += () =>
            {
                if (coreHasBeenSetup)
                {
                    SwapCharacterOutfit();
                }
            };
            ScrollView.AddButton(button);

            if (player != null)
            {
                currentStyle = player.moveStyle;

                if (!skinIndices.ContainsKey(currentStyle))
                    skinIndices[currentStyle] = 0;

                ApplySkin(currentStyle, skinIndices[currentStyle]);
            }
        }

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

                currentStyle = newStyle;

                if (!skinIndices.ContainsKey(newStyle))
                    skinIndices[newStyle] = 0;

                ApplySkin(newStyle, skinIndices[newStyle]);
            }
        }

        private void CycleSkinForCurrentStyle()
        {
            if (player == null) return;

            currentStyle = player.moveStyle;

            if (!skinIndices.ContainsKey(currentStyle))
            {
                skinIndices[currentStyle] = 1; // was 0
            }

            int currentIndex = skinIndices[currentStyle];
            int nextIndex = (currentIndex + 1) % 10;

            skinIndices[currentStyle] = nextIndex;

            ApplySkin(currentStyle, nextIndex);
        }

        private void ApplySkin(MoveStyle style, int skinIndex)
        {
            if (player == null) return;

            Texture skinTexture = player.CharacterConstructor.GetMoveStyleSkinTexture(style, skinIndex);
            if (skinTexture == null) return;

            Material[] moveStyleMaterials = MoveStyleLoader.GetMoveStyleMaterials(player, style);

            if (moveStyleMaterials == null || moveStyleMaterials.Length == 0)
            {
                Debug.LogWarning($"No materials found for MoveStyle {style}");
                return;
            }

            for (int i = 0; i < moveStyleMaterials.Length; i++)
            {
                moveStyleMaterials[i].mainTexture = skinTexture;
            }

            Debug.Log($"Applied skin index {skinIndex} for style {style}");
        }

        // Swap character outfit
        private void SwapCharacterOutfit()
        {
            if (player == null) return;

            Characters characterEnum = player.character;
            CharacterProgress progress = Core.Instance.SaveManager.CurrentSaveSlot.GetCharacterProgress(characterEnum);

            characterSkinIndex = (characterSkinIndex + 1) % 4; // or use progress.GetAvailableOutfitCount() if available

            player.SetOutfit(characterSkinIndex);

            Debug.Log($"Swapped to outfit index {characterSkinIndex} for character {characterEnum}");
        }
    }
}
