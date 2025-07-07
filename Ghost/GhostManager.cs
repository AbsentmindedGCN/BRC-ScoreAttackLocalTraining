using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Reptile;

namespace ScoreAttackGhostSystem
{
    public class GhostManager : MonoBehaviour
    {
        public static GhostManager Instance => _instance;
        public GhostState CurrentGhostState => _currentGhostState;
        public GhostRecorder GhostRecorder => _ghostRecorder;
        public GhostPlayer GhostPlayer => _ghostPlayer;

        private static GhostManager _instance;
        private GhostState _currentGhostState = null;
        private GhostRecorder _ghostRecorder = new GhostRecorder();
        private GhostPlayer _ghostPlayer = new GhostPlayer();

        public static GhostManager Create()
        {
            var replayManagerObject = new GameObject("Ghost Manager");
            return replayManagerObject.AddComponent<GhostManager>();
        }

        private void Awake()
        {
            _instance = this;
        }

        /* 
        // This is not handled by ScoreAttackManager!
        
        private void Update()
        {
            if (Core.instance.IsCorePaused)
                return;
            if (_currentGhostState != null)
                _currentGhostState.OnUpdate();
            if (Input.GetKeyDown(KeyCode.F3)) // Move Logic to Score Attack Implementation
            {
                if (_currentGhostState == null)
                {
                    _currentGhostState?.End();
                    _currentGhostState = _ghostRecorder;
                    _ghostRecorder.Start();
                }
                else if (_currentGhostState == _ghostRecorder)
                {
                    _ghostRecorder.End();
                    _currentGhostState = _ghostPlayer;
                    _ghostPlayer.Replay = _ghostRecorder.Replay;
                    _ghostPlayer.Start();
                }
                else if (_currentGhostState == _ghostPlayer)
                {
                    _ghostPlayer.End();
                    _currentGhostState = null;
                }
            }
        }
        */

        public void OnFixedUpdate()
        {
            if (Core.instance.IsCorePaused && !ScoreAttack.Patches.StartGraffitiModePatch.inGraffitiMode)
                return;
            _currentGhostState?.OnFixedUpdate();
        }

        private void OnDestroy()
        {
            _instance = null; //
        }

        // Methods for Score Attack
        public GhostRecorder GetGhostRecorder()
        {
            return _ghostRecorder;
        }

        public GhostState GetGhostState()
        {
            return _currentGhostState;
        }

        public void RemoveCurrentGhostState()
        {
            _currentGhostState = null;
        }

        public void AddCurrentGhostState()
        {
            _currentGhostState = GetGhostRecorder(); 
        }

    }
}