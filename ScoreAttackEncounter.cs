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
using System.Diagnostics.PerformanceData;
using UnityEngine.SocialPlatforms.Impl;
using System.Globalization;
using UnityEngine.Events;

namespace ScoreAttack
{
    public class ScoreAttackEncounter : Encounter
    {
        public float timeLimit = 40f;
        public float ScoreGot;
        private float timeLimitTimer;
        private CultureInfo cultureInfo;

        // Add a field to store the personal best score for the current area
        private float personalBestScore = 0.0f;
        private float displayBestScore = 0.0f;
        //private readonly Dictionary<Stage, Encounter> getStage= [];
        private float countdownTimer = 3.0f; // 3-second countdown
        private bool isCountdownFinished = false;

        private bool isNewBestDisplayed = false;

        // Set true if battle is active
        private static bool isScoreAttackActive = false;

        // For Saving PBs
        private Stage currentStage; // Add a field to store the current stage

        //private float initialScore = 0.0f;

        public override void InitSceneObject()
        {
            cultureInfo = CultureInfo.CurrentCulture;
            makeUnavailableDuringEncounter = new GameObject[0];
            introSequence = null;
            currentCheckpoint = -1;
            OnIntro = new UnityEvent();
            OnStart = new UnityEvent();
            OnOutro = new UnityEvent();
            OnCompleted = new UnityEvent();
            OnFailed = new UnityEvent();
            stopWantedOnStart = false;

            base.InitSceneObject();
        }


        // Start Score Battle and Refresh the Stuff
        public override void StartMainEvent()
        {
            // Initialize currentStage with the current stage
            currentStage = Core.Instance.BaseModule.CurrentStage;

            player.grindAbility.trickTimer = 0f;

            // Set battle as active
            isScoreAttackActive = true;

            player.score = 0f;
            ScoreGot = 0f;

            // Set the boolean flag to false to start the countdown
            isCountdownFinished = false;
            countdownTimer = 3.0f; // Reset countdown timer

            // Load personal best score for the current stage and time limit
            personalBestScore = ScoreAttackSaveData.Instance.GetOrCreatePersonalBest(currentStage).GetPersonalBest(timeLimit);
            //Core.Instance.SaveManager.SaveCurrentSaveSlot();

            // If personal best score does not exist (is -1), set it to 0
            if (personalBestScore == -1f)
            {
                personalBestScore = 0f;
                ScoreAttackSaveData.Instance.GetOrCreatePersonalBest(currentStage).SetPersonalBest(timeLimit, personalBestScore);
                //Core.Instance.SaveManager.SaveCurrentSaveSlot();
            }

            // Call base StartMainEvent
            base.StartMainEvent();
        }

        
        
        // Update Score as the player gets it
        public override void UpdateMainEvent()
        {

            // Update the personal best score if the current score is higher
            float currentScore = ScoreGot;

            if (player.IsBusyWithSequence())
                return;

            if (!isCountdownFinished)
            {
                // Countdown logic
                countdownTimer -= Core.dt;
                //var text = NiceTimerString(countdownTimer);
                var text = Mathf.CeilToInt(countdownTimer).ToString(); // Display only 3, 2, 1
                GameplayUI gameplay = Core.Instance.UIManager.gameplay;
                gameplay.timeLimitLabel.text = text + "...";

                if (countdownTimer <= 0f)
                {
                    isCountdownFinished = true;
                    timeLimitTimer = timeLimit; // Start the main timer after the countdown finishes
                    
                    // reset any score gathered during countdown
                    player.score = 0f; // set score to 0
                    player.baseScore = 0f;
                    player.scoreMultiplier = 0f;
                }
            }
            else
            {
                // Main event logic
                timeLimitTimer -= Core.dt;

                if (timeLimitTimer < 0f)
                {
                    // Play SFX when ending battle
                    Core.Instance.AudioManager.PlaySfxUI(
                        SfxCollectionID.EnvironmentSfx,
                        AudioClipID.MascotUnlock);

                    // End battle
                    isScoreAttackActive = false;

                    timeLimitTimer = 0f;
                }

                ScoreGot = player.score + player.baseScore * player.scoreMultiplier;
                SetScoreUI();

                // Update personal best score if the current score surpasses it
                if (ScoreGot > personalBestScore)
                {
                    personalBestScore = ScoreGot;

                    // Update Save Data
                    ScoreAttackSaveData.Instance.GetOrCreatePersonalBest(currentStage).SetPersonalBest(timeLimit, personalBestScore);
                    Core.Instance.SaveManager.SaveCurrentSaveSlot();
                }

                // Save personal best after updating
                Core.Instance.SaveManager.SaveCurrentSaveSlot();

                if (personalBestScore != ScoreGot)
                {
                    displayBestScore = personalBestScore;
                }
                else
                {
                    displayBestScore = 0.0f;
                }

                if (timeLimitTimer > 0f)
                {
                    base.UpdateMainEvent();
                    return;
                }
                SetEncounterState(Encounter.EncounterState.MAIN_EVENT_SUCCES_DECAY);
            }
        }


        public void SetScoreUI()
        {
            GameplayUI gameplay = Core.Instance.UIManager.gameplay;
            gameplay.challengeGroup.SetActive(true);

            if (!isCountdownFinished)
            {
                // Display countdown timer
                if (!isScoreAttackActive)
                {
                    gameplay.timeLimitLabel.text = "";
                    gameplay.targetScoreLabel.text = ""; // Clear other labels during cancel
                    gameplay.totalScoreLabel.text = "";
                    gameplay.targetScoreTitleLabel.text = "";
                    gameplay.totalScoreTitleLabel.text = "";
                }
                else
                {
                    gameplay.timeLimitLabel.text = Mathf.CeilToInt(countdownTimer).ToString();
                    gameplay.targetScoreLabel.text = ""; // Clear other labels during countdown
                    gameplay.totalScoreLabel.text = "";
                    gameplay.targetScoreTitleLabel.text = "";
                    gameplay.totalScoreTitleLabel.text = "";
                }

                // Reset the flag when countdown starts
                isNewBestDisplayed = false;
            }
            else
            {
                // Display actual timer and scores
                var text = NiceTimerString(timeLimitTimer);
                gameplay.timeLimitLabel.text = text;

                // Check if ScoreGot is greater than personalBestScore
                if (ScoreGot > personalBestScore)
                {
                    // If ScoreGot is greater than personalBestScore, update personalBestScore and set isNewBestDisplayed to true
                    personalBestScore = ScoreGot;
                    isNewBestDisplayed = true;

                    //This fixes one PB from sticking
                    ScoreAttackSaveData.Instance.GetOrCreatePersonalBest(currentStage).SetPersonalBest(timeLimit, personalBestScore);
                    Core.Instance.SaveManager.SaveCurrentSaveSlot();
                }

                // Display the personal best score or "New Best!" message
                if (isNewBestDisplayed)
                {
                    gameplay.targetScoreLabel.text = "New Best!";
                }
                else
                {
                    gameplay.targetScoreLabel.text = FormattingUtility.FormatPlayerScore(cultureInfo, personalBestScore);
                }

                gameplay.totalScoreLabel.text = FormattingUtility.FormatPlayerScore(cultureInfo, ScoreGot);
                gameplay.targetScoreTitleLabel.text = "Personal Best:";
                gameplay.totalScoreTitleLabel.text = "Score:";
            }
        }


        public void EndScoreAttack()
        {

            // Play SFX when ending battle
            /*
            Core.Instance.AudioManager.PlaySfxUI(
                SfxCollectionID.MenuSfx,
                AudioClipID.cancel);
            */

            // Reload personal bests after battle
            personalBestScore = ScoreAttackSaveData.Instance.GetOrCreatePersonalBest(currentStage).GetPersonalBest(timeLimit);
            //personalBestScore = ScoreAttackSaveData.Instance.GetOrCreatePersonalBest(currentStage).GetPersonalBest(timeLimit);

            // Update personal best score if the current score surpasses it
            if (ScoreGot > personalBestScore)
            {
                personalBestScore = ScoreGot;
                ScoreAttackSaveData.Instance.GetOrCreatePersonalBest(currentStage).SetPersonalBest(timeLimit, personalBestScore);
                //Core.Instance.SaveManager.SaveCurrentSaveSlot();
            }

            // Save personal best after updating
            Core.Instance.SaveManager.SaveCurrentSaveSlot();

            // Deactivate the score attack
            isScoreAttackActive = false;

            // Reset countdown timer and flags
            countdownTimer = 0f;
            isCountdownFinished = false;

            // Turn off any UI related to the score attack
            TurnOffScoreUI();

            // Save personal best after updating
            //Core.Instance.SaveManager.SaveCurrentSaveSlot();

            //SetEncounterState(Encounter.EncounterState.MAIN_EVENT_SUCCES_DECAY);
            SetEncounterState(Encounter.EncounterState.MAIN_EVENT_FAILED_DECAY);
            //This shouldn't matter if it's dailed or succes?
        }


        public void TurnOffScoreUI()
        {
            GameplayUI gameplay = Core.Instance.UIManager.gameplay;
            gameplay.challengeGroup.SetActive(false);
            gameplay.timeLimitLabel.text = "";
            gameplay.targetScoreLabel.text = "";
            gameplay.totalScoreLabel.text = "";
            gameplay.targetScoreTitleLabel.text = "";
            gameplay.totalScoreTitleLabel.text = "";
        }

        private string NiceTimerString(float timer)
        {
            string text = timer.ToString();
            int num = ((int)timer).ToString().Length + 3;
            if (text.Length > num)
            {
                text = text.Remove(num);
            }
            if (timer == 0f)
            {
                text = "0.00";
                
            }
            return text;
        }

        public override void EnterEncounterState(Encounter.EncounterState setState)
        {
            if (setState == Encounter.EncounterState.CLOSED || setState == Encounter.EncounterState.OPEN || setState == Encounter.EncounterState.OPEN_STARTUP)
            {
                TurnOffScoreUI();
            }
            else
            {
                SetScoreUI();
            }
            base.EnterEncounterState(setState);
        }

        // Don't load.
        public override void ReadFromData()
        {

        }

        // Don't save.
        public override void WriteToData()
        {

        }

        public static bool IsScoreAttackActive()
        {
            return isScoreAttackActive;
        }

        public void ClearPersonalBest()
        {
            foreach (var personalBest in ScoreAttackSaveData.Instance.PersonalBestByStage.Values)
            {
                personalBest.PersonalBestByTimeLimit.Clear();
            }

            // Save the cleared personal bests
            Core.Instance.SaveManager.SaveCurrentSaveSlot();

            // Notify the player that personal bests have been erased
            Core.Instance.UIManager.ShowNotification("All personal bests have been erased.");
        }

        /*
        // Only clears current stage?
        public void ClearPersonalBest()
        {
            personalBestScore = 0.0f;
            displayBestScore = 0.0f;
            isNewBestDisplayed = false;

            // Clear the stored personal best score for the current stage
            ScoreAttackSaveData.Instance.GetOrCreatePersonalBest(currentStage).SetPersonalBest(timeLimit, personalBestScore);

            //Save now that the data is cleared
            Core.Instance.SaveManager.SaveCurrentSaveSlot();
        }
        */

        // I'm not sure why i'm getting null refs so I'm overriding this method and taking out some stuff we're not using.
        public override void SetEncounterState(EncounterState setState)
        {
            stateTimer = 0f;
            state = setState;
            switch (state)
            {
                case EncounterState.CLOSED:
                    EnterEncounterState(EncounterState.CLOSED);
                    if (WorldHandler.instance.currentEncounter == this)
                    {
                        WorldHandler.instance.MakeStuffAvailableAgainCauseEncounterOver();
                    }
                    if (player.phone != null)
                    {
                        player.phone.AllowPhone(true, true, false);
                        return;
                    }
                    break;
                case EncounterState.OPEN_STARTUP:
                    EnterEncounterState(EncounterState.OPEN_STARTUP);
                    return;
                case EncounterState.OPEN:
                    if (openOn == OpenOn.REQUIRED_REP && requirement != 0)
                    {
                        WorldHandler.instance.ResetRequiredREP();
                    }
                    requirement = 0;
                    EnterEncounterState(EncounterState.OPEN);
                    shownOpening = true;
                    return;
                case EncounterState.INTRO:
                    WorldHandler.instance.StartEncounter(this);
                    currentlyActive = true;
                    ChangeSkybox(true);
                    player.phone.AllowPhone(allowPhone, true, false);
                    EnterEncounterState(Encounter.EncounterState.INTRO);
                    SetEncounterState(Encounter.EncounterState.MAIN_EVENT);
                    SequenceHandler.instance.LetPlayerExitSequence();
                    WriteToData();
                    return;
                case EncounterState.MAIN_EVENT:
                    EnterEncounterState(Encounter.EncounterState.MAIN_EVENT);
                    StartMainEvent();
                    return;
                case EncounterState.MAIN_EVENT_SUCCES_DECAY:
                    win = true;
                    EnterEncounterState(Encounter.EncounterState.MAIN_EVENT_SUCCES_DECAY);
                    return;
                case EncounterState.MAIN_EVENT_FAILED_DECAY:
                    EnterEncounterState(Encounter.EncounterState.MAIN_EVENT_FAILED_DECAY);
                    return;
                case EncounterState.OUTRO_SUCCES:
                    EnterEncounterState(Encounter.EncounterState.OUTRO_SUCCES);
                    if (WorldHandler.instance.currentEncounter == this)
                    {
                        WorldHandler.instance.MakeStuffAvailableAgainCauseEncounterOver();
                    }
                    SetEncounterState(Encounter.EncounterState.CLOSED);
                    Complete(null);
                    return;
                case Encounter.EncounterState.OUTRO_FAIL:
                    EnterEncounterState(Encounter.EncounterState.OUTRO_FAIL);
                    Fail(null);
                    if (restartImmediatelyOnFail)
                    {
                        ActivateEncounterInstantIntro();
                        return;
                    }
                    if (WorldHandler.instance.currentEncounter == this)
                    {
                        WorldHandler.instance.MakeStuffAvailableAgainCauseEncounterOver();
                    }
                    SetEncounterState(Encounter.EncounterState.CLOSED);
                    break;
                default:
                    return;
            }
        }
    }
}
