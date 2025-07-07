using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reptile;
using UnityEngine;
using static Reptile.UserInputHandler;

namespace ScoreAttackGhostSystem
{
    public class Ghost(float tickDelta)
    {
        public float TickDelta => _tickDelta;
        public Player Player => _player;
        public int FrameCount => _frames.Count;
        public float Length => (FrameCount - 1) * _tickDelta; //public float Length => (FrameCount - 1) * FPS;
        
        public List<GhostFrame> Frames => _frames;
        private Player _player;
        private float _tickDelta = tickDelta;
        private List<GhostFrame> _frames = new List<GhostFrame>();

        public int Character = -1;
        public Guid CharacterGUID;
        public int Outfit = -1;
        public float Score { get; set; } = 0f;

        public void Init()
        {
            _player = WorldHandler.instance.GetCurrentPlayer();
        }

        public int GetFrameForTime(float time)
        {
            var divTime = Mathf.FloorToInt(time / _tickDelta);
            return divTime;
        }
    }
}
