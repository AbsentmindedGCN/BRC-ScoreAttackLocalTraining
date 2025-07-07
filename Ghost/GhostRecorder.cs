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
