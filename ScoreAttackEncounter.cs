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
using System.Collections;

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

        private bool playCustomSounds = false; // default to false

        // SFX
        private AudioClip announcerThree;
        private AudioClip announcerTwo;
        private AudioClip announcerOne;
        private AudioClip announcerStart;
        private AudioClip announcerEnd;
        private AudioClip announcerBest;
        private AudioSource audioSource;
        private bool announcerReady = false;
        private bool hasPlayedThree = false;
        private bool hasPlayedTwo = false;
        private bool hasPlayedOne = false;
        private bool hasPlayedStart = false;
        private bool hasPlayedEnd = false;
        private bool hasPlayedBest = false;

        private IEnumerator LoadAnnouncerClips()
        {
            string pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (AppExtras.SFXMode == SFXToggle.LLB)
            {
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/announcer_three.ogg"), clip => announcerThree = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/announcer_two.ogg"), clip => announcerTwo = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/announcer_one.ogg"), clip => announcerOne = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/lobby_start_game.ogg"), clip => announcerStart = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/announcer_time_up.ogg"), clip => announcerEnd = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/express.ogg"), clip => announcerBest = clip);
            }
            else if (AppExtras.SFXMode == SFXToggle.FZero)
            {
                yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_three.ogg"), clip => announcerThree = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_two.ogg"), clip => announcerTwo = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_one.ogg"), clip => announcerOne = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_go.ogg"), clip => announcerStart = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_finish.ogg"), clip => announcerEnd = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_courserecord.ogg"), clip => announcerBest = clip);
                //yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_goal.ogg"), clip => announcerBest = clip);
            }
            else
            {
                // Just play LLB or something
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/announcer_three.ogg"), clip => announcerThree = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/announcer_two.ogg"), clip => announcerTwo = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/announcer_one.ogg"), clip => announcerOne = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/lobby_start_game.ogg"), clip => announcerStart = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/announcer_time_up.ogg"), clip => announcerEnd = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/express.ogg"), clip => announcerBest = clip);
            }

                if (audioSource == null)
            {
                GameObject go = new GameObject("AnnouncerAudioSource");
                audioSource = go.AddComponent<AudioSource>();
                UnityEngine.Object.DontDestroyOnLoad(go);
            }

            announcerReady = true;
        }

        private IEnumerator LoadOgg(string filePath, Action<AudioClip> onLoaded)
        {
            var uri = new System.Uri(filePath).AbsoluteUri;
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error loading OGG: " + www.error);
                }
                else
                {
                    AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    onLoaded?.Invoke(clip);
                }
            }
        }

        public void SetPlayOggSounds(bool value)
        {
            playCustomSounds = value;
        }

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

            // Sync settings with whatever the player chose in AppExtras
            SetPlayOggSounds(AppExtras.SFXMode != SFXToggle.Default);

            // Initialize currentStage with the current stage
            currentStage = Core.Instance.BaseModule.CurrentStage;

            // Remove Grind Debt
            player.grindAbility.trickTimer = 0f;

            // Refresh Stale Moves
            //player.RefreshAirTricks();
            player.RefreshAllDegrade();

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

            // Load SFX
            Core.Instance.StartCoroutine(LoadAnnouncerClips());
            hasPlayedThree = hasPlayedTwo = hasPlayedOne = false;

        }



        // Update Score as the player gets it
        public override void UpdateMainEvent()
        {

            // Update the personal best score if the current score is higher
            float currentScore = ScoreGot;


            if (player.IsBusyWithSequence())
                return;

            /*
            if (player.IsBusyWithSequence())
            {
                this.timeLimitTimer -= Core.dt;
            }
            */

            if (!isCountdownFinished)
            {
                if (!announcerReady)
                {
                    Debug.Log("Announcer loading...");
                    return;
                }

                // Countdown logic
                countdownTimer -= Core.dt;
                //var text = NiceTimerString(countdownTimer);
                var text = Mathf.CeilToInt(countdownTimer).ToString(); // Display only 3, 2, 1
                GameplayUI gameplay = Core.Instance.UIManager.gameplay;
                gameplay.timeLimitLabel.text = text + "...";

                if (playCustomSounds)
                {
                    if (Mathf.CeilToInt(countdownTimer) == 3 && !hasPlayedThree)
                    {
                        if (AppExtras.SFXMode == SFXToggle.LLB || AppExtras.SFXMode == SFXToggle.FZero)
                        {
                            player.AudioManager.PlayOneShotSfx(player.AudioManager.mixerGroups[3], announcerThree, player.AudioManager.audioSources[3], 0f);

                            //player.AudioManager.PlayOneShotSfx(Reptile.AudioManager.mixerGroups[3], announcerThree, Reptile.AudioManager.audioSources[3], 0f);
                            //float sfxVolume = Core.Instance.AudioManager.SfxVolume;
                            //audioSource?.PlayOneShot(announcerThree, sfxVolume * 0.5f);
                        }
                        else
                        {
                            player.AudioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.jump_special, player.playerOneShotAudioSource, 0f);
                            //Core.Instance.audioManager.PlaySfxGameplay(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Select);
                            //player.audioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.siren,
                            //player.playerOneShotAudioSource, 0f);
                        }

                        hasPlayedThree = true;
                    }
                    else if (Mathf.CeilToInt(countdownTimer) == 2 && !hasPlayedTwo)
                    {
                        if (AppExtras.SFXMode == SFXToggle.LLB || AppExtras.SFXMode == SFXToggle.FZero)
                        {
                            player.AudioManager.PlayOneShotSfx(player.AudioManager.mixerGroups[3], announcerTwo, player.AudioManager.audioSources[3], 0f);
                            //float sfxVolume = Core.Instance.AudioManager.SfxVolume;
                            //audioSource?.PlayOneShot(announcerTwo, sfxVolume * 0.5f);
                        }
                        else
                        {
                            player.AudioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.jump_special, player.playerOneShotAudioSource, 0f);
                            //Core.Instance.audioManager.PlaySfxGameplay(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Select);
                        }
                        
                        hasPlayedTwo = true;
                    }
                    else if (Mathf.CeilToInt(countdownTimer) == 1 && !hasPlayedOne)
                    {
                        if (AppExtras.SFXMode == SFXToggle.LLB || AppExtras.SFXMode == SFXToggle.FZero)
                        {
                            player.AudioManager.PlayOneShotSfx(player.AudioManager.mixerGroups[3], announcerOne, player.AudioManager.audioSources[3], 0f);
                            //float sfxVolume = Core.Instance.AudioManager.SfxVolume;
                            //audioSource?.PlayOneShot(announcerOne, sfxVolume * 0.5f);
                        }
                        else
                        {
                            player.AudioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.jump_special, player.playerOneShotAudioSource, 0f);
                            //Core.Instance.audioManager.PlaySfxGameplay(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Select);
                            //Core.Instance.audioManager.PlaySfxGameplay(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Select);
                            //player.audioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.launcher_woosh,
                            //player.playerOneShotAudioSource, 0f);
                        }
                        
                        hasPlayedOne = true;
                    }
                }

                if (countdownTimer <= 0f)
                {
                    isCountdownFinished = true;
                    timeLimitTimer = timeLimit; // Start the main timer after the countdown finishes
                    
                    // reset any score gathered during countdown
                    player.score = 0f; // set score to 0
                    player.baseScore = 0f;
                    player.scoreMultiplier = 0f;

                    if (playCustomSounds)
                    {
                        if (AppExtras.SFXMode == SFXToggle.LLB || AppExtras.SFXMode == SFXToggle.FZero)
                        {
                            player.AudioManager.PlayOneShotSfx(player.AudioManager.mixerGroups[3], announcerStart, player.AudioManager.audioSources[3], 0f);
                            //float sfxVolume = Core.Instance.AudioManager.SfxVolume;
                            //audioSource?.PlayOneShot(announcerStart, sfxVolume * 0.5f);
                        }
                        else
                        {

                            player.audioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.launcher_woosh, player.playerOneShotAudioSource, 0f);

                            /*
                            if (!hasPlayedStart)
                            {
                                player.audioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.launcher_woosh, player.playerOneShotAudioSource, 0f);
                            }
                            */
                            //player.AudioManager.PlaySfxGameplay(SfxCollectionID.VoiceOldhead, AudioClipID.VoiceBoostTrick, player.playerOneShotAudioSource, 0f);

                            //player.audioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.launcher_woosh,
                            //player.playerOneShotAudioSource, 0f);
                        }

                        hasPlayedStart = true;
                    }
                }
            }
            else
            {
                // Main event logic
                timeLimitTimer -= Core.dt;

                if (timeLimitTimer < 0f)
                {
                    if (playCustomSounds)
                    {
                        if (AppExtras.SFXMode == SFXToggle.LLB || AppExtras.SFXMode == SFXToggle.FZero)
                        {
                            // Play LLB SFX
                            player.AudioManager.PlayOneShotSfx(player.AudioManager.mixerGroups[3], announcerEnd, player.AudioManager.audioSources[3], 0f);
                            //float sfxVolume = Core.Instance.AudioManager.SfxVolume;
                            //audioSource?.PlayOneShot(announcerEnd, sfxVolume * 0.5f);
                            hasPlayedEnd = true;
                        }
                        else
                        {
                            // Play the usual SFX when ending battle
                            Core.Instance.AudioManager.PlaySfxUI(
                            SfxCollectionID.EnvironmentSfx,
                            AudioClipID.MascotUnlock);
                        }
                    }
                    else
                    {
                        // Play SFX when ending battle
                        Core.Instance.AudioManager.PlaySfxUI(
                        SfxCollectionID.EnvironmentSfx,
                        AudioClipID.MascotUnlock);
                    }

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

                    // Also play a jingle if custom sounds are enabled
                    if (playCustomSounds && !hasPlayedBest)
                    {
                        if (AppExtras.SFXMode == SFXToggle.LLB || AppExtras.SFXMode == SFXToggle.FZero)
                        {
                            player.AudioManager.PlayOneShotSfx(player.AudioManager.mixerGroups[3], announcerBest, player.AudioManager.audioSources[3], 0f);
                            //float sfxVolume = Core.Instance.AudioManager.SfxVolume;
                            //audioSource?.PlayOneShot(announcerBest, sfxVolume * 0.5f);
                            hasPlayedBest = true;
                        }
                        else
                        {
                            player.audioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.launcher_woosh,
                            player.playerOneShotAudioSource, 0f);
                            hasPlayedBest = true;
                        }
                    }
                }
                else
                {
                    gameplay.targetScoreLabel.text = FormattingUtility.FormatPlayerScore(cultureInfo, personalBestScore);
                    hasPlayedBest = false;
                }

                gameplay.totalScoreLabel.text = FormattingUtility.FormatPlayerScore(cultureInfo, ScoreGot);
                gameplay.targetScoreTitleLabel.text = "Personal Best:";
                gameplay.totalScoreTitleLabel.text = (timeLimit/60)+" "+"Min."+" "+"Score:";
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
                //text = "Finish!";

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
