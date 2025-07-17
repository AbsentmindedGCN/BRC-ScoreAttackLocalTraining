using Reptile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Reptile.UserInputHandler;
using ScoreAttack;

namespace ScoreAttackGhostSystem
{
    public class GhostRecorder : GhostState
    {
        private float accumulatedScore = 0f;
        private float lastValidBaseScore = 0f;
        private float lastValidMultiplier = 1f;
        private bool comboJustDropped = false;
        private float lastFrameScore = 0f;
        private float comboAccumulated = 0f;


        public static GhostRecorder Instance;
        public bool Recording { get; private set; } = false;

        public Ghost Replay => _currentReplay;
        public InputBuffer CurrentInput;

        public GhostFrame LastFrame
        {
            get
            {
                return _currentReplay.Frames.Count > 1 ? _currentReplay.Frames[_currentReplay.Frames.Count - 1] : null;
            }
        }
        private Ghost _currentReplay;

        public override void Start()
        {
            _currentReplay = new Ghost(ScoreAttackPlugin.ghostTickInterval);
            _currentReplay.Init();
            Instance = this;
            Recording = true;

            accumulatedScore = 0f;
            lastValidBaseScore = 0f;
            lastValidMultiplier = 1f;
            comboJustDropped = false;
        }

        public override void End()
        {
            Instance = null;
            Recording = false;
        }

        public void OnPlayerInput(Player player)
        {
            if (WorldHandler.instance.GetCurrentPlayer() != player)
                return;
            CurrentInput = player.inputBuffer;
        }

        public override void OnFixedUpdate()
        {
            if (!Recording) { return; }

            if (ScoreAttackManager.Encounter == null || !ScoreAttackEncounter.IsScoreAttackActive())
            {
                Debug.Log("[Score Attack] Stopping recording: Score Attack is no longer active.");
                End();
                return;
            }

            if (_currentReplay.Player == null)
            {
                Debug.Log("[GhostRecorder] Stopping recording: Player is no longer valid.");
                End();
                return;
            }

            RecordCurrentFrame();
        }

        private void RecordCurrentFrame()
        {
            var frame = new GhostFrame(_currentReplay) { FrameIndex = _currentReplay.Frames.Count };
            _currentReplay.Frames.Add(frame);
            
            if (_currentReplay.Player == null) { return; }
            var p = _currentReplay.Player;
            frame.Valid = true;

            if (_currentReplay.Character == -1) {
                _currentReplay.Character = (int)p.character; 
                if (p.character > Characters.MAX) _currentReplay.CharacterGUID = CrewBoomSupport.GetGUID((int)p.character);
            }
                
            if (_currentReplay.Outfit == -1) {
                _currentReplay.Outfit = Core.Instance.SaveManager.CurrentSaveSlot.GetCharacterProgress(p.character).outfit;
            }
                
            frame.PlayerPosition = p.transform.position;
            frame.PlayerRotation = p.transform.rotation;
            frame.Velocity = p.GetVelocity();

            /*
            // Ongoing score
            frame.BaseScore = p.baseScore;
            frame.ScoreMultiplier = p.scoreMultiplier;
            frame.OngoingScore = p.baseScore*p.scoreMultiplier;
            */

            // === FINAL SCORE FIX ===
            /*
            float baseScore = p.baseScore;
            float multiplier = p.scoreMultiplier;

            // If baseScore is 0 but we had a combo last frame, it means combo dropped
            if (baseScore == 0f && multiplier == 0f && lastValidBaseScore > 0f)
            {
                if (!comboJustDropped)
                {
                    accumulatedScore += lastValidBaseScore * lastValidMultiplier;
                    //Debug.Log($"[Score Attack] Combo dropped — adding {lastValidBaseScore} x {lastValidMultiplier} = {lastValidBaseScore * lastValidMultiplier}");
                    comboJustDropped = true;
                }

                // Reset stored combo values
                lastValidBaseScore = 0f;
                lastValidMultiplier = 1f;
            }
            else if (baseScore > 0f && multiplier > 0f)
            {
                lastValidBaseScore = baseScore;
                lastValidMultiplier = multiplier;
                comboJustDropped = false;
            }

            // Set to ghost frame
            frame.BaseScore = baseScore;
            frame.ScoreMultiplier = multiplier;
            frame.OngoingScore = accumulatedScore + (baseScore * multiplier);
            */

            /*
            float baseScore = p.baseScore;
            float multiplier = p.scoreMultiplier;

            // Calculate current combo score
            float currentScore = baseScore * multiplier;

            // Only add score if it increased since last frame
            if (currentScore > 0f && currentScore != lastFrameScore)
            {
                float delta = currentScore - lastFrameScore;

                // Only add positive increases
                if (delta > 0f)
                {
                    accumulatedScore += delta;
                }

                lastFrameScore = currentScore;
            }
            else if (currentScore == 0f)
            {
                // Combo dropped or ended
                lastFrameScore = 0f;
            }

            // Set to ghost frame
            frame.BaseScore = baseScore;
            frame.ScoreMultiplier = multiplier;
            frame.OngoingScore = accumulatedScore;
            */

            float baseScore = p.baseScore;
            float multiplier = p.scoreMultiplier;
            float comboScore = baseScore * multiplier;

            // If combo is currently active
            if (comboScore > 0f)
            {
                if (comboJustDropped)
                {
                    // New combo started after drop
                    comboJustDropped = false;
                    comboAccumulated = 0f;
                }

                // Accumulate current combo
                comboAccumulated = comboScore;
            }
            else
            {
                // Combo dropped
                if (!comboJustDropped && comboAccumulated > 0f)
                {
                    accumulatedScore += comboAccumulated;
                    //Debug.Log($"[Score Attack] Combo dropped — added {comboAccumulated}");
                    comboAccumulated = 0f;
                    comboJustDropped = true;
                }
            }

            // Add ongoing combo if it’s the last frame (e.g., replay ends or time expires)
            // (You may do this outside this loop if needed)

            // Assign values to frame
            frame.BaseScore = baseScore;
            frame.ScoreMultiplier = multiplier;
            frame.OngoingScore = accumulatedScore + comboAccumulated;


            // ----------------------

            //frame.PhoneState = p.phone.state;
            frame.SpraycanState = p.spraycanState;

            frame.moveStyle = p.moveStyle;
            frame.equippedMoveStyle = p.moveStyleEquipped;
            frame.UsingEquippedMoveStyle = p.usingEquippedMovestyle;

            frame.Animation.ID = p.curAnim;
            frame.Animation.Time = p.anim.playbackTime;
            // (IMPLEMENTED) set frame.Animation.Instant and frame.Animation.AtTime in a PlayAnim patch

            frame.Visual.Position = p.characterVisual.tf.position;
            frame.Visual.Rotation = p.characterVisual.tf.rotation;

            // prevent null exceptions
            try { frame.Visual.dustEmission = (int)p.characterVisual.dustParticles.emission.rateOverTime.constant; } catch (System.Exception) {}
            try { frame.Visual.dustSize = (float)p.characterVisual.VFX.dust.transform.localScale.x; } catch (System.Exception) {}
            try { frame.Visual.spraypaintEmission = (int)p.spraypaintParticles.emission.rateOverTime.constant; } catch (System.Exception) {}
            try { frame.Visual.ringEmission = (int)p.ringParticles.emission.rateOverTime.constant; } catch (System.Exception) {}

            frame.Visual.boostpackEffectMode = p.characterVisual.boostpackEffectMode;
            frame.Visual.frictionEffectMode = p.characterVisual.frictionEffectMode;

            // frame.Effects set in GhostPatches
        }
    }
}
