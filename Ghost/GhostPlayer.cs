﻿using ScoreAttackGhostSystem.Patches;
using Reptile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Reptile.UserInputHandler;
using MonoMod.Cil;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using ScoreAttack;
using System.Reflection;

namespace ScoreAttackGhostSystem
{
    public class GhostPlayer : GhostState
    {
        public float CurrentTime => _currentTime;
        public Ghost Replay;
        public Action OnFrameSkip;
        private bool _paused = false;
        private float _playbackSpeed = 1f;
        private float _currentTime = 0f;

        public bool ReplayEnded { get; private set; } = false;

        public static Player ghostPlayerCharacter;
        //public int FrameIndex;

        bool developmentMode = false;

        // State Tracker
        private bool _isSpraying = false;

        // Ghost Settings
        //public bool GhostAudioEnabled = true;

        public float BaseScore;
        public float ScoreMultiplier;
        public float LastAppliedOngoingScore;

        public bool Active { get; private set; } = false;
        private Player.SpraycanState _lastAppliedSpraycanState = Player.SpraycanState.NONE;

        public override void Start()
        {
            _currentTime = 0f;
            _paused = false;
            _playbackSpeed = 1f;
            ReplayEnded = false;
            ghostPlayerCharacter = SetupAIPlayer();
            Active = true;

            /*
            // Destroy old ghost player if exists
            if (ghostPlayerCharacter != null)
            {
                UnityEngine.Object.Destroy(ghostPlayerCharacter.gameObject);
                ghostPlayerCharacter = null;
            }
            */

            // Align _currentTime to the next tick
            float fixedDelta = Replay.TickDelta; // or ScoreAttackPlugin.ghostTickInterval
            float remainder = _currentTime % fixedDelta;
            if (remainder != 0)
            {
                float delay = fixedDelta - remainder;
                _currentTime += delay;
            }

            ScoreAttackPlugin.Instance.ResetCurrentTime();

            LastAppliedOngoingScore = 0;
        }

        // Set up the AI Player for the Ghost, this pulls the model and everything from the current player
        private Player SetupAIPlayer()
        {
            WorldHandler worldHandler = WorldHandler.instance;
            PlayerSpawner playerSpawnPoint = worldHandler.GetDefaultPlayerSpawnPoint();
            Player currentPlayer = WorldHandler.instance.GetCurrentPlayer();

            int currentPlayerOutfit = Core.Instance.SaveManager.CurrentSaveSlot.GetCharacterProgress(currentPlayer.character).outfit;

            // If the model isn't the player model, force outfit to default (0)
            bool overrideModel = GhostSaveData.Instance.GhostModel != GhostModel.Self;
            if (overrideModel)
            {
                switch (GhostSaveData.Instance.GhostModel)
                {
                    case GhostModel.DJCyber:
                        Replay.Character = (int)Characters.dj;
                        break;
                    case GhostModel.Faux:
                        Replay.Character = (int)Characters.headMan;
                        break;
                }

                // Use default outfit when character is overridden
                Replay.Outfit = 0;
            }
            else
            {
                if (Replay.Character > (int)Characters.MAX && Replay.CharacterGUID != null && Replay.CharacterGUID != Guid.Empty)
                {
                    try { Replay.Character = CrewBoomSupport.GetCharacter(Replay.CharacterGUID); }
                    catch (System.Exception) { Replay.Character = -1; }
                }

                if (Replay.Character == -1)
                {
                    Replay.Character = (int)currentPlayer.character;
                }

                if (Replay.Outfit == -1)
                {
                    Replay.Outfit = currentPlayerOutfit;
                }
            }


            MoveStyle ghostMovestyle = Replay.Frames[0].moveStyle;
            Player returnPlayer = worldHandler.SetupAIPlayerAt(playerSpawnPoint.transform, (Reptile.Characters)Replay.Character, PlayerType.NONE, Replay.Outfit, ghostMovestyle);

            returnPlayer.motor.gravity = 0;
            returnPlayer.motor.SetKinematic(true);
            returnPlayer.motor.enabled = false;
            returnPlayer.moveStyleEquipped = Replay.Frames[0].equippedMoveStyle;

            // Drop any existing combo and reset animation state
            returnPlayer.DropCombo();
            returnPlayer.StopCurrentAbility();

            // Optional: Apply Ghost Effect
            if (GhostSaveData.Instance.GhostEffect == GhostEffect.Transparent)
            {
                MakePlayerGhostLOD(returnPlayer, 0.3f);
            }


            // Optional: Disable Doppler Effect for all AudioSources on the ghost
            if (AppGhostSettings.SoundMode != GhostSoundMode.Doppler)
            {
                var audioDopplerSources = returnPlayer.GetComponentsInChildren<AudioSource>(true);
                foreach (var audio in audioDopplerSources)
                {
                    audio.dopplerLevel = 0f;
                }
            }
            else
            {
                var audioDopplerSources = returnPlayer.GetComponentsInChildren<AudioSource>(true);
                foreach (var audio in audioDopplerSources)
                {
                    audio.dopplerLevel = 1f;
                }
            }

            // Optional: Disable all sound from ghost
            if (AppGhostSettings.SoundMode == GhostSoundMode.Off)
            {
                var audioSources = returnPlayer.GetComponentsInChildren<AudioSource>(true);
                foreach (var audio in audioSources)
                {
                    audio.mute = true;
                }
            }
            else
            {
                var audioSources = returnPlayer.GetComponentsInChildren<AudioSource>(true);
                foreach (var audio in audioSources)
                {
                    audio.mute = false;
                }
            }

            return returnPlayer;
        }

        public void SkipTo(float time)
        {
            // Ensure the time doesn't go beyond the replay's total length
            if (time >= Replay.Length)
            {
                time = Replay.Length;
            }

            var frame = Replay.GetFrameForTime(time);
            if (frame >= Replay.FrameCount)
                frame = Replay.FrameCount - 1;
            if (frame < 0)
                frame = 0;

            ApplyFrameToWorld(Replay.Frames[frame], true);
            _currentTime = (float)frame * Replay.TickDelta;
        }

        public void EndReplay()
        {
            // Stop updating and go idle
            ReplayEnded = true;
            if (ghostPlayerCharacter != null)
            {
                if (GhostSaveData.Instance.GhostEffect == GhostEffect.Transparent)
                {
                    MakePlayerGhostLOD(ghostPlayerCharacter, 0.15f); // even more transparent to signal end of replay
                }
                ghostPlayerCharacter.anim.speed = 0f;
            }
            return;
        }

        //Overload
        // fallback overload so all existing calls still work because I'm lazy
        public void ApplyFrameToWorld(GhostFrame frame, bool skip)
        {
            ApplyFrameToWorld(
                frame,
                skip,
                frame.PlayerPosition,      // use the raw position
                frame.PlayerRotation,       // and raw rotation
                frame.Visual.Position,
                frame.Visual.Rotation
            );

        }

        public void ApplyFrameToWorld(GhostFrame frame, bool skip, Vector3 interpPosition, Quaternion interpRotation, Vector3 interpPositionVisual, Quaternion interpRotationVisual)
        {
            if (!frame.Valid)
                return;

            // Calculate incremental score gained since last frame
            /* float currentBaseScore = frame.BaseScore;
            float currentMultiplier = frame.ScoreMultiplier;

            BaseScore = currentBaseScore;
            ScoreMultiplier = currentMultiplier; */

            var p = ghostPlayerCharacter;

            p.moveStyle = frame.moveStyle;
            p.moveStyleEquipped = frame.equippedMoveStyle;

            p.InitMovement(frame.moveStyle);
            p.SetMoveStyle(frame.moveStyle, true, false);
            p.SwitchToEquippedMovestyle(frame.UsingEquippedMoveStyle, false, true, false);

            if (frame.OngoingScore != 0) { LastAppliedOngoingScore = frame.OngoingScore; }

            if (skip)
            {
                p.SwitchToEquippedMovestyle(frame.UsingEquippedMoveStyle, false, true, false);
                OnFrameSkip?.Invoke();
            }

            ApplyInterpolationToWorld(skip, interpPosition, interpRotation, interpPositionVisual, interpRotationVisual);

            p.SetVelocity(frame.Velocity);
            //p.phone.phoneState = frame.PhoneState;

            p.SetBoostpackAndFrictionEffects(frame.Visual.boostpackEffectMode, frame.Visual.frictionEffectMode);
            p.SetDustEmission(frame.Visual.dustEmission);
            p.SetDustSize(frame.Visual.dustSize);
            p.SetSpraypaintEmission(frame.Visual.spraypaintEmission);
            p.SetRingEmission((int)frame.Visual.ringEmission);

            //Spraycan fix
            if (frame.SpraycanState != _lastAppliedSpraycanState)
            {
                p.SetSpraycanState(frame.SpraycanState);
                _lastAppliedSpraycanState = frame.SpraycanState;
                if (frame.SpraycanState == Player.SpraycanState.SPRAY)
                    p.anim.Play(p.canSprayHash, 1, 0f);
            }
            else { p.spraycanState = frame.SpraycanState; }

            // Only log if changed
            if (frame.SpraycanState != _lastAppliedSpraycanState)
            {
                Debug.Log($"[ScoreAttack] Ghost SpraycanState changed to: {frame.SpraycanState}");
                _lastAppliedSpraycanState = frame.SpraycanState;
            }
            else if (_isSpraying)
            {
                UnityEngine.Debug.Log("[ScoreAttack] Replaying spray animation");
            }

            bool newAnim = frame.Animation.AtTime != -1f;
            float animTime = !newAnim ? frame.Animation.Time : frame.Animation.AtTime;
            if (frame.SpraycanState == Player.SpraycanState.NONE || newAnim)
            {
                p.PlayAnim(frame.Animation.ID, frame.Animation.ForceOverwrite, frame.Animation.Instant, animTime);
            }

            if (frame.Effects.ringParticles > 0) { p.ringParticles.Emit(frame.Effects.ringParticles); }
            if (frame.Effects.spraypaintParticles > 0) { p.spraypaintParticles.Emit(frame.Effects.spraypaintParticles); }

            if (frame.Effects.DoJumpEffects) { p.DoJumpEffects(frame.Effects.JumpEffects); }
            if (frame.Effects.DoHighJumpEffects) { p.DoHighJumpEffects(frame.Effects.HighJumpEffects); }

            if (AppGhostSettings.SoundMode != GhostSoundMode.Off)
            {
                foreach (GhostFrame.GhostFrameSFX frameSFX in frame.SFX)
                {
                    if (frameSFX.AudioClipID != AudioClipID.NONE)
                    {
                        if (frameSFX.Voice)
                        {
                            p.audioManager.PlayVoice(ref p.currentVoicePriority, p.character, frameSFX.AudioClipID, p.playerGameplayVoicesAudioSource, VoicePriority.MOVEMENT);
                        }
                        else
                        {
                            if (frameSFX.CollectionID != SfxCollectionID.NONE)
                            {
                                // Filter out DefaultPlus SFX
                                if (!((frameSFX.AudioClipID == AudioClipID.jump_special || frameSFX.AudioClipID == AudioClipID.launcher_woosh) && !frame.Effects.DoHighJumpEffects))
                                {
                                    p.audioManager.PlaySfxGameplay(frameSFX.CollectionID, frameSFX.AudioClipID, p.playerOneShotAudioSource, frameSFX.RandomPitchVariance);
                                }

                            }
                            else
                            {
                                p.audioManager.PlaySfxGameplay(frame.moveStyle, frameSFX.AudioClipID, p.playerOneShotAudioSource, frameSFX.RandomPitchVariance);
                            }
                        }
                    }
                }
            }

            // Disable Colliders
            foreach (Collider collider in p.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null)
                    continue;
                {
                    collider.enabled = false;
                }
            }
        }

        public void ApplyInterpolationToWorld(bool skip, Vector3 interpPosition, Quaternion interpRotation, Vector3 interpPositionVisual, Quaternion interpRotationVisual)
        {
            var p = ghostPlayerCharacter;
            if (skip)
            {
                WorldHandler.instance.PlacePlayerAt(p, interpPosition, interpRotation, true);
            }
            else
            {
                p.transform.position = interpPosition;
                p.transform.rotation = interpRotation;
            }

            p.characterVisual.transform.position = interpPositionVisual;
            p.characterVisual.transform.rotation = interpRotationVisual;
        }

        public override void End()
        {
            Active = false;
            /* var lastFrame = Replay.Frames[Replay.Frames.Count - 1];
            ApplyFrameToWorld(lastFrame, true); */

            //Excluded ControllerMap
            Core.instance.UIManager.gameObject.SetActive(true);
            //_replayCamera.Destroy();
            Time.timeScale = 1f;

            if (ghostPlayerCharacter != null)
            {
                WorldHandler worldHandler = WorldHandler.instance;
                if (worldHandler is { SceneObjectsRegister.players: not null })
                {
                    worldHandler.SceneObjectsRegister.players.Remove(ghostPlayerCharacter);
                }

                UnityEngine.Object.Destroy(ghostPlayerCharacter.gameObject);
            }

            //LastAppliedOngoingScore = 0;
        }

        private void RefreshTimeScale()
        {
            if (_paused)
                Time.timeScale = 0f;
            else
                Time.timeScale = _playbackSpeed;
        }

        public override void OnUpdate()
        {
            UpdatePlaybackInput();
            RefreshTimeScale();
        }

        private void UpdatePlaybackInput()
        {
            // Dev Commands

            if (developmentMode == true)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    _paused = !_paused;
                }
                _playbackSpeed += Input.mouseScrollDelta.y * 0.05f;
                if (_playbackSpeed <= 0f)
                    _playbackSpeed = 0f;
                if (Input.GetKeyDown(KeyCode.R))
                {
                    _playbackSpeed = 1f;
                    _paused = false;
                }

                if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    _currentTime = 0f;
                }
                if (Input.GetKeyDown(KeyCode.H))
                {
                    Core.instance.UIManager.gameObject.SetActive(!Core.instance.UIManager.gameObject.activeSelf);
                }
                if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    SkipTo(0f);
                }
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    SkipTo(Replay.Length * 0.25f);
                }
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    SkipTo(Replay.Length * 0.5f);
                }
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    SkipTo(Replay.Length * 0.75f);
                }
                if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    SkipTo(Replay.Length);
                }
            }

        }

        // Use Unity's LOD system to create a ghost effect
        private static void MakePlayerGhostLOD(Player player, float alpha)
        {
            // Create a simple ghost material
            Material ghostMaterial = new Material(Shader.Find("Sprites/Default"));
            ghostMaterial.color = new Color(1f, 1f, 1f, alpha);

            // Fix depth/rendering issues
            ghostMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            ghostMaterial.SetInt("_ZWrite", 0);
            ghostMaterial.renderQueue = 4000;

            // Apply to all renderers
            PlayerVisualEffects vfx = player.characterVisual.VFX;
            PlayerMoveStyleProps props = player.characterVisual.moveStyleProps;
            GameObject[] safeRenderers = new GameObject[] {
                vfx.circleBoostEffect, vfx.jumpEffect, vfx.frictionEffect, vfx.frictionBlueEffect,
                vfx.boostpackEffect, vfx.boostpackBlueEffect, vfx.boostpackTrail, vfx.spraycan, vfx.spraycanCap,
                vfx.spraypaint, vfx.graffitiSlash, vfx.graffitiFinishEffect, vfx.graffitiFinishLargeEffect,
                vfx.phone, vfx.dust, vfx.ring, vfx.hitEffect, vfx.getHitEffect, vfx.highJumpEffect,
                props.bmxFrame, props.bmxHandlebars, props.bmxWheelF, props.bmxWheelR,
                props.bmxGear, props.bmxPedalL, props.bmxPedalR, props.skateboard,
                props.skateR, props.skateL, props.specialSkateBoard
            };

            Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    if (renderer is TrailRenderer && !safeRenderers.Contains(renderer.gameObject))
                    {
                        renderer.gameObject.SetActive(false);
                    }
                    Material[] materials = new Material[renderer.materials.Length];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = ghostMaterial;
                    }
                    renderer.materials = materials;
                }
            }
        }


    }
}