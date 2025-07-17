using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Reptile.UserInputHandler;
using Reptile.Phone;
using Reptile;

namespace ScoreAttackGhostSystem
{
    public class GhostFrame(Ghost replay)
    {
        public int FrameIndex;
        public bool Valid = false;
        private Ghost _replay = replay;

        public Vector3 PlayerPosition;
        public Quaternion PlayerRotation;
        public Vector3 Velocity;

        //public float OngoingScore = 0f;

        public float BaseScore;
        public float ScoreMultiplier;
        public float OngoingScore;

        public Phone.PhoneState PhoneState = Phone.PhoneState.OFF;
        public Player.SpraycanState SpraycanState = Player.SpraycanState.NONE;

        public MoveStyle moveStyle;
        public MoveStyle equippedMoveStyle;
        public bool UsingEquippedMoveStyle;

        public GhostFrameAnimation Animation = new();
        public GhostFrameVisual Visual = new();
        public GhostFrameEffects Effects = new();
        public List<GhostFrameSFX> SFX = new List<GhostFrameSFX>();

        public class GhostFrameAnimation
        {
            public int ID;
            public float Time;
            public bool ForceOverwrite = false;
            public bool Instant = false;
            public float AtTime = -1f;
        }

        public class GhostFrameVisual
        {
            public Vector3 Position;
            public Quaternion Rotation;

            public BoostpackEffectMode boostpackEffectMode = BoostpackEffectMode.OFF;
            public FrictionEffectMode frictionEffectMode = FrictionEffectMode.OFF;
            public int dustEmission = 0;
            public float dustSize = 1f;
            public int spraypaintEmission = 0;
            public int ringEmission = 0; 
        }

        public class GhostFrameEffects
        {
            public int ringParticles = -1;
            public int spraypaintParticles = -1;

            public bool DoJumpEffects = false;
            public bool DoHighJumpEffects = false;
            
            public Vector3 JumpEffects = Vector3.zero;
            public Vector3 HighJumpEffects = Vector3.zero;
        }

        public class GhostFrameSFX
        {
	        public AudioClipID AudioClipID = AudioClipID.NONE;
	        public SfxCollectionID CollectionID = SfxCollectionID.NONE;
	        public float RandomPitchVariance = 0f;
            public bool Voice = false;
        }
    }
}
