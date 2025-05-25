using CommonAPI;
using Reptile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScoreAttack
{
    public static class ScoreAttackManager
    {
        public static ScoreAttackEncounter Encounter;
        private const string UID = "ea696dc2-28cb-46c4-abfa-4193199b7e98";
        //Testing

        internal static void Initialize()
        {
            StageAPI.OnStagePreInitialization += OnStagePreInitialization;
        }

        // Create the Score Attack encounter before we initialize the new stage.
        private static void OnStagePreInitialization(Stage newStage, Stage previousStage)
        {
            var encounterGameObject = new GameObject("Score Attack Encounter");
            Encounter = encounterGameObject.AddComponent<ScoreAttackEncounter>();
            Encounter.uid = UID;
        }

        public static void StartScoreAttack(float timeLimit)
        {
            Encounter.timeLimit = timeLimit;
            Encounter.ActivateEncounter();
        }
    }
}
