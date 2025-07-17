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
using ScoreAttackGhostSystem;
using static Reptile.FixedFramerateSequence;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

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
        public static bool isScoreAttackActive { get; private set; } = false;

        // For Saving PBs
        private Stage currentStage; // Add a field to store the current stage

        //private float initialScore = 0.0f;

        // Ghost
        private Ghost currentBestGhost; // Loaded ghost for current stage/time limit
        private object ghostPlayerInstance; // Cached reference to _ghostPlayer
        public object ghostRecorderInstance { get; private set; } // Cached reference to _ghostRecorder
        private float ghostEndTimer = -1f; // Delay for ghost despawn

        // Ghost Score
        private float externalGhostScore = -1f;
        private bool ghostWasExternal = false;

        // Set up WantedManager to clear cops
        private WantedManager wantedManager;

        private bool playCustomSounds = false; // default to false

        // SFX
        private AudioClip announcerThree;
        private AudioClip announcerTwo;
        private AudioClip announcerOne;
        private AudioClip announcerStart;
        private AudioClip announcerEnd;
        private AudioClip announcerBest;
        private AudioClip announcerGhost;
        private AudioSource audioSource;
        private bool announcerReady = false;
        private bool hasPlayedThree = false;
        private bool hasPlayedTwo = false;
        private bool hasPlayedOne = false;
        private bool hasPlayedStart = false;
        private bool hasPlayedEnd = false;
        private bool hasPlayedBest = false;
        private bool hasPlayedGhostBeaten = false;

        // Custom Time
        public static float customDeltaTime
        {
            get
            {
                return Time.deltaTime; // Just do it like TR I guess
                //return Time.unscaledDeltaTime;
            }
            set
            {
            }
        }

        // now accessed by AppExtras
        public IEnumerator LoadAnnouncerClips()
        {
            string pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.LLB)
            {
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/announcer_three.ogg"), clip => announcerThree = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/announcer_two.ogg"), clip => announcerTwo = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/announcer_one.ogg"), clip => announcerOne = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/lobby_start_game.ogg"), clip => announcerStart = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/announcer_time_up.ogg"), clip => announcerEnd = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/reward.ogg"), clip => announcerBest = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sfx/special_activate.ogg"), clip => announcerGhost = clip);
            }
            else if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.FZero)
            {
                yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_three.ogg"), clip => announcerThree = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_two.ogg"), clip => announcerTwo = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_one.ogg"), clip => announcerOne = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_go.ogg"), clip => announcerStart = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_finish.ogg"), clip => announcerEnd = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_courserecord.ogg"), clip => announcerBest = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "fzero/fzero_goal.ogg"), clip => announcerGhost = clip);
            }
            else if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.POPN)
            {
                yield return LoadOgg(Path.Combine(pluginPath, "popn/pnm_areyouready.ogg"), clip => announcerThree = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "popn/null.ogg"), clip => announcerTwo = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "popn/null.ogg"), clip => announcerOne = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "popn/pnm_herewego.ogg"), clip => announcerStart = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "popn/pnm_exit.ogg"), clip => announcerEnd = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "popn/pnm_marvelous.ogg"), clip => announcerBest = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "popn/pnm_yourecool.ogg"), clip => announcerGhost = clip);
            }
            else if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SSBM)
            {
                yield return LoadOgg(Path.Combine(pluginPath, "melee/ssbm_ready.ogg"), clip => announcerThree = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "melee/null.ogg"), clip => announcerTwo = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "melee/null.ogg"), clip => announcerOne = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "melee/ssbm_go.ogg"), clip => announcerStart = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "melee/ssbm_time.ogg"), clip => announcerEnd = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "melee/ssbm_anewrecord.ogg"), clip => announcerBest = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "melee/ssbm_success.ogg"), clip => announcerGhost = clip);
            }
            else if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SF3S)
            {
                // Define valid group IDs
                string[] introGroups = { "i1", "i2", "i3", "i4" };

                // Randomly select a group index
                int randomIndex = UnityEngine.Random.Range(0, introGroups.Length); // 0 to 3
                string selectedGroup = introGroups[randomIndex];

                // Build correct file paths for p1 and p2 using the same group
                string pathP1 = Path.Combine(pluginPath, "sf3", $"sf3_{selectedGroup}p1.ogg");
                string pathP2 = Path.Combine(pluginPath, "sf3", $"sf3_{selectedGroup}p2.ogg");

                // Load paired clips for announcerThree (p1) and announcerStart (p2)
                yield return LoadOgg(pathP1, clip => announcerThree = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sf3/null.ogg"), clip => announcerTwo = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sf3/null.ogg"), clip => announcerOne = clip);
                yield return LoadOgg(pathP2, clip => announcerStart = clip);

                // Load other announcer calls
                yield return LoadOgg(Path.Combine(pluginPath, "sf3/sf3_timeover.ogg"), clip => announcerEnd = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sf3/sf3_letsgo.ogg"), clip => announcerBest = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "sf3/sf3_letsgo.ogg"), clip => announcerGhost = clip);
            }
            else if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.Skullgirls)
            {
                yield return LoadOgg(Path.Combine(pluginPath, "skullgirls/sg_ladiesandgentlemen.ogg"), clip => announcerThree = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "skullgirls/null.ogg"), clip => announcerTwo = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "skullgirls/null.ogg"), clip => announcerOne = clip);

                // Randomly choose one of the showtime variants
                int showtimeVariant = UnityEngine.Random.Range(1, 4); // 1 to 3
                string showtimeFile = $"sg_showtime{(showtimeVariant == 1 ? "" : showtimeVariant.ToString())}.ogg";
                yield return LoadOgg(Path.Combine(pluginPath, "skullgirls", showtimeFile), clip => announcerStart = clip);

                yield return LoadOgg(Path.Combine(pluginPath, "skullgirls/sg_timeout.ogg"), clip => announcerEnd = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "skullgirls/sg_letsgo.ogg"), clip => announcerBest = clip);
                yield return LoadOgg(Path.Combine(pluginPath, "skullgirls/sg_jingle.ogg"), clip => announcerGhost = clip);
            }
            else
            {
                // Don't do anything
            }

            if (audioSource == null)
            {
                GameObject go = new GameObject("AnnouncerAudioSource");
                audioSource = go.AddComponent<AudioSource>();
                UnityEngine.Object.DontDestroyOnLoad(go);
            }

            announcerReady = true;
        }

        /*
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
        */

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

                    if (clip.loadState != AudioDataLoadState.Loaded)
                    {
                        clip.LoadAudioData();

                        // Wait until clip is fully loaded into memory
                        while (clip.loadState == AudioDataLoadState.Loading)
                        {
                            yield return null;
                        }

                        if (clip.loadState != AudioDataLoadState.Loaded)
                        {
                            Debug.LogError("AudioClip failed to load completely: " + filePath);
                            yield break;
                        }
                    }

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
            SetPlayOggSounds(ScoreAttackSaveData.Instance.ExtraSFXMode != SFXToggle.Default);

            // Initialize currentStage with the current stage
            currentStage = Core.Instance.BaseModule.CurrentStage;

            // Remove Grind Debt
            player.grindAbility.trickTimer = 0f;

            // Refresh Stale Moves
            //player.RefreshAirTricks();
            player.RefreshAllDegrade();

            // Clear Police
            ResetPoliceState();

            // Clear any Ghost if one exists
            TryEndGhostPlaybackIfActive();

            // Start a New Ghost Recording
            StartGhostRecording();

            // Play back personal best ghost at the same time
            StartGhostPlaybackAlongsideRecording();

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

            var ghostManager = GhostManager.Instance;
            //var ghostManagerType = typeof(GhostManager);

            // Get ghost player
            ghostPlayerInstance = ghostManager.GhostPlayer;

            // Get ghost recorder
            ghostRecorderInstance = ghostManager.GhostRecorder;

            //Clear Ghost Ongoing Score
            ghostManager.GhostPlayer.LastAppliedOngoingScore = 0f;

            // Load the best ghost for the current stage and time limit
            Ghost bestGhost = GhostSaveData.Instance.GetOrCreateGhostData(currentStage).GetGhost(timeLimit);

            if (ScoreAttackManager.ExternalGhostLoadedFromGhostList && ScoreAttackManager.LoadedExternalGhost != null)
            {
                externalGhostScore = ScoreAttackManager.ExternalGhostScore;
            }
            else
            {
                externalGhostScore = -1f; // no external ghost score loaded
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

            if (!isCountdownFinished)
            {
                if (!announcerReady)
                {
                    Debug.Log("Announcer loading...");
                    return;
                }

                // Countdown logic
                countdownTimer -= customDeltaTime;
                var text = Mathf.CeilToInt(countdownTimer).ToString(); // Display only 3, 2, 1
                GameplayUI gameplay = Core.Instance.UIManager.gameplay;
                gameplay.timeLimitLabel.text = text + "...";

                if (playCustomSounds)
                {
                    if (Mathf.CeilToInt(countdownTimer) == 3 && !hasPlayedThree)
                    {
                        //if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.LLB || ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.FZero)
                        if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.LLB   ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.FZero ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.POPN  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SSBM  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SF3S  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.Skullgirls)
                        {
                            player.AudioManager.PlayOneShotSfx(player.AudioManager.mixerGroups[3], announcerThree, player.AudioManager.audioSources[3], 0f);
                        }
                        else if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.DefaultPlus)
                        {
                            if (ScoreAttackSaveData.Instance.ExtraSFXMode != SFXToggle.Default)
                            {
                                player.AudioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.jump_special, player.playerOneShotAudioSource, 0f);
                            }
                        }
                        else
                        {
                            //Don't play any sound for Default SFX Config
                        }

                        hasPlayedThree = true;
                    }
                    else if (Mathf.CeilToInt(countdownTimer) == 2 && !hasPlayedTwo)
                    {
                        if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.LLB   ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.FZero ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.POPN  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SSBM  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SF3S  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.Skullgirls)
                        {
                            player.AudioManager.PlayOneShotSfx(player.AudioManager.mixerGroups[3], announcerTwo, player.AudioManager.audioSources[3], 0f);
                        }
                        else if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.DefaultPlus)
                        {
                            if (ScoreAttackSaveData.Instance.ExtraSFXMode != SFXToggle.Default)
                            {
                                player.AudioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.jump_special, player.playerOneShotAudioSource, 0f);
                            }
                        }
                        else
                        {
                            //Don't play any sound for Default SFX Config
                        }

                        hasPlayedTwo = true;
                    }
                    else if (Mathf.CeilToInt(countdownTimer) == 1 && !hasPlayedOne)
                    {
                        if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.LLB   ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.FZero ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.POPN  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SSBM  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SF3S  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.Skullgirls)
                        {
                            player.AudioManager.PlayOneShotSfx(player.AudioManager.mixerGroups[3], announcerOne, player.AudioManager.audioSources[3], 0f);
                        }
                        else if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.DefaultPlus)
                        {
                            if (ScoreAttackSaveData.Instance.ExtraSFXMode != SFXToggle.Default)
                            {
                                player.AudioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.jump_special, player.playerOneShotAudioSource, 0f);
                            }
                        }
                        else
                        {
                            //Don't play any sound for Default SFX Config
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
                        //if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.LLB || ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.FZero)
                        if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.LLB   ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.FZero ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.POPN  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SSBM  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SF3S  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.Skullgirls)
                        {
                            player.AudioManager.PlayOneShotSfx(player.AudioManager.mixerGroups[3], announcerStart, player.AudioManager.audioSources[3], 0f);
                        }
                        else if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.DefaultPlus)
                        {
                            if (ScoreAttackSaveData.Instance.ExtraSFXMode != SFXToggle.Default)
                            {
                                player.audioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.launcher_woosh, player.playerOneShotAudioSource, 0f);

                            }
                        }
                        else
                        {
                            //Don't play any sound for Default SFX Config
                        }

                        hasPlayedStart = true;
                    }
                }
            }
            else
            {
                // Main event logic
                //timeLimitTimer -= Core.dt;
                timeLimitTimer -= customDeltaTime;

                if (timeLimitTimer < 0f)
                {
                    if (playCustomSounds)
                    {
                        //if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.LLB || ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.FZero)
                        if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.LLB   ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.FZero ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.POPN  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SSBM  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SF3S  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.Skullgirls)
                        {
                            // Play Custom SFX
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

                    // Reload personal bests after battle
                    personalBestScore = ScoreAttackSaveData.Instance.GetOrCreatePersonalBest(currentStage).GetPersonalBest(timeLimit);

                    // Check if there is a new personal best
                    if (ScoreGot > personalBestScore)
                    {
                        SavePB();

                        // Export ghost here again, it's not working other places
                        Debug.Log("[ScoreAttack] Exporting new personal best as external ghost (OnlyPB mode).");
                        var pbreplay = (ghostRecorderInstance as GhostRecorder)?.Replay;
                        GhostSaveData.Instance.SaveGhostToFile(pbreplay, currentStage, timeLimit, ScoreGot);
                    }

                    // Save External Ghost
                    var replay = (ghostRecorderInstance as GhostRecorder)?.Replay;

                    if (replay != null && replay.Frames.Count > 0 && ScoreGot >= 1f)
                    {
                        if (GhostSaveData.Instance.GhostSaveMode == GhostSaveMode.OnlyPB)
                        {
                            if (ScoreGot < personalBestScore)
                            {
                                Debug.Log("[ScoreAttack] Score was not a personal best. External ghost not saved (OnlyPB mode).");
                            }
                        }
                        else if (GhostSaveData.Instance.GhostSaveMode == GhostSaveMode.Enabled)
                        {
                            Debug.Log("[ScoreAttack] Saving ghost (Enabled mode).");
                            GhostSaveData.Instance.SaveGhostToFile(replay, currentStage, timeLimit, ScoreGot);
                        }
                        else
                        {
                            Debug.Log("[ScoreAttack] Ghost Auto Save is disabled. No ghost saved.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[ScoreAttack] Replay was null or had no frames. Ghost not saved.");
                    }


                    // End Ghost
                    //EndGhostPlayback();
                    TryEndGhostPlaybackIfActive();

                    // End battle
                    isScoreAttackActive = false;

                    // Clear Police (Fix for leftover chains)
                    ResetPoliceState();

                    timeLimitTimer = 0f;
                }

                ScoreGot = player.score + player.baseScore * player.scoreMultiplier;
                SetScoreUI();

                // Update personal best score if the current score surpasses it
                if (ScoreGot > personalBestScore)
                {
                    personalBestScore = ScoreGot;

                    /*// Update Save Data
                    ScoreAttackSaveData.Instance.GetOrCreatePersonalBest(currentStage).SetPersonalBest(timeLimit, personalBestScore);
                    Core.Instance.SaveManager.SaveCurrentSaveSlot();*/
                }

                // Save personal best after updating
                //Core.Instance.SaveManager.SaveCurrentSaveSlot();

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

                // Check if player beat personal best
                if (ScoreGot > personalBestScore)
                {
                    personalBestScore = ScoreGot;
                    isNewBestDisplayed = true;
                }

                // --- Begin Ghost vs PB Display Logic ---
                bool ghostLoaded = ScoreAttackManager.ExternalGhostLoadedFromGhostList;
                float ghostScore = ScoreAttackManager.ExternalGhostScore;
                bool ghostBeaten = ghostLoaded && ScoreGot > ghostScore && ghostScore >= 0f;

                // Show New Best Message, has priority over others
                if (isNewBestDisplayed)
                {
                    gameplay.targetScoreLabel.text = "New Best!";

                    if (playCustomSounds && !hasPlayedBest)
                    {
                        //if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.LLB || ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.FZero)
                        if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.LLB   ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.FZero ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.POPN  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SSBM  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.SF3S  ||
                            ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.Skullgirls)
                        {
                            player.AudioManager.PlayOneShotSfx(player.AudioManager.mixerGroups[3], announcerBest, player.AudioManager.audioSources[3], 0f);
                        }
                        else if (ScoreAttackSaveData.Instance.ExtraSFXMode == SFXToggle.DefaultPlus)
                        {
                            if (ScoreAttackSaveData.Instance.ExtraSFXMode != SFXToggle.Default)
                            {
                                player.audioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.launcher_woosh, player.playerOneShotAudioSource, 0f);

                            }
                        }
                        else
                        {
                            // Don't play any sound for Default SFX
                        }

                        hasPlayedBest = true;
                    }

                    if (ghostBeaten && playCustomSounds && ScoreAttackSaveData.Instance.ExtraSFXMode != SFXToggle.Default && ScoreAttackSaveData.Instance.ExtraSFXMode != SFXToggle.DefaultPlus && !hasPlayedGhostBeaten)
                    {
                        player.AudioManager.PlayOneShotSfx(player.AudioManager.mixerGroups[3], announcerGhost, player.AudioManager.audioSources[3], 0f);
                        hasPlayedGhostBeaten = true;
                    }
                }

                // Show Ghost Beaten if Ghost is Beaten
                else if (ghostBeaten)
                {
                    gameplay.targetScoreLabel.text = "Ghost Beat!";

                    if (playCustomSounds && ScoreAttackSaveData.Instance.ExtraSFXMode != SFXToggle.Default && ScoreAttackSaveData.Instance.ExtraSFXMode != SFXToggle.DefaultPlus && !hasPlayedGhostBeaten)
                    {
                        player.AudioManager.PlayOneShotSfx(player.AudioManager.mixerGroups[3], announcerGhost, player.AudioManager.audioSources[3], 0f);
                        hasPlayedGhostBeaten = true;
                    }
                }

                // Update UI
                else
                {
                    var ghostManager = GhostManager.Instance;
                    var ghostPlayer = ghostManager?.GhostPlayer;
                    var replay = ghostPlayer?.Replay;

                    bool usingOngoing = GhostSaveData.Instance.GhostScoreMode == GhostScoreMode.Ongoing;
                    bool hasReplay = replay != null && replay.Frames != null && replay.Frames.Count > 0;

                    if (usingOngoing && hasReplay && replay.HasOngoingScoreData)
                    {
                        float ongoing = ghostPlayer.LastAppliedOngoingScore;
                        gameplay.targetScoreLabel.text = FormattingUtility.FormatPlayerScore(cultureInfo, ongoing);
                    }
                    else if (ghostLoaded && ghostScore >= 0f)
                    {
                        gameplay.targetScoreLabel.text = FormattingUtility.FormatPlayerScore(cultureInfo, ghostScore);
                    }
                    else
                    {
                        gameplay.targetScoreLabel.text = FormattingUtility.FormatPlayerScore(cultureInfo, personalBestScore);
                    }

                    hasPlayedBest = false;
                    hasPlayedGhostBeaten = false;
                }


                // Always show current score
                gameplay.totalScoreLabel.text = FormattingUtility.FormatPlayerScore(cultureInfo, ScoreGot);

                // Score label title
                gameplay.targetScoreTitleLabel.text = ghostLoaded ? "Ghost Score:" : "Personal Best:";
                gameplay.totalScoreTitleLabel.text = (timeLimit / 60) + " " + "Min." + " " + "Score:";
            }

        }


        public void EndScoreAttack()
        {

            // Reload personal bests after battle
            personalBestScore = ScoreAttackSaveData.Instance.GetOrCreatePersonalBest(currentStage).GetPersonalBest(timeLimit);

            // Reload ghost data after battle
            //currentBestGhost = ScoreAttackGhostSaveData.Instance.GetOrCreateGhostData(currentStage).GetGhost(timeLimit);

            // Update personal best score if the current score surpasses it
            if (ScoreGot > personalBestScore)
            {
                SavePB();

                // Export Ghost Here because it's not working other places
                Debug.Log("[ScoreAttack] Exporting new personal best as external ghost (OnlyPB mode).");
                var pbreplay = (ghostRecorderInstance as GhostRecorder)?.Replay;
                GhostSaveData.Instance.SaveGhostToFile(pbreplay, currentStage, timeLimit, ScoreGot);

            }

            var replay = (ghostRecorderInstance as GhostRecorder)?.Replay;

            if (replay != null && replay.Frames.Count > 0 && ScoreGot >= 1f)
            {
                if (GhostSaveData.Instance.GhostSaveMode == GhostSaveMode.OnlyPB)
                {
                    if (ScoreGot < personalBestScore)
                    {
                        Debug.Log("[ScoreAttack] Score was not a personal best. External ghost not saved (OnlyPB mode).");
                    }
                }
                else if (GhostSaveData.Instance.GhostSaveMode == GhostSaveMode.Enabled)
                {
                    Debug.Log("[ScoreAttack] Saving ghost (Enabled mode).");
                    GhostSaveData.Instance.SaveGhostToFile(replay, currentStage, timeLimit, ScoreGot);
                }
                else
                {
                    Debug.Log("[ScoreAttack] Ghost Auto Save is disabled. No ghost saved.");
                }
            }
            else
            {
                Debug.LogWarning("[ScoreAttack] Replay was null or had no frames. Ghost not saved.");
            }

            // Clear any Ghost if one exists
            TryEndGhostPlaybackIfActive();

            // Save personal best after updating
            //Core.Instance.SaveManager.SaveCurrentSaveSlot();

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
            //This shouldn't matter if it's failed or succes?
        }

        private void SavePB()
        {
            personalBestScore = ScoreGot;
            ScoreAttackSaveData.Instance.GetOrCreatePersonalBest(currentStage).SetPersonalBest(timeLimit, personalBestScore);

            if (GhostSaveData.Instance.GhostSaveMode != GhostSaveMode.Disabled)
            {
                var replay = (ghostRecorderInstance as GhostRecorder)?.Replay;
                if (replay != null && replay.Frames.Count > 0)
                {
                    replay.Score = ScoreGot;

                    GhostSaveData.Instance
                        .GetOrCreateGhostData(currentStage)
                        .SetGhost(timeLimit, replay);

                    Debug.Log($"[ScoreAttack] New PB ghost {timeLimit / 60} min saved with {replay.Frames.Count} frames. Score was {replay.Score}.");

                    // Save external file
                    //GhostSaveData.Instance.SaveGhostToFile(replay, currentStage, timeLimit, ScoreGot);
                }

                Core.Instance.SaveManager.SaveCurrentSaveSlot();
            }
            else
            {
                Debug.Log($"[ScoreAttack] Ghost Save Mode is set to disabled. Not saving player's new PB.");
            }

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

        private void ResetPoliceState()
        {

            wantedManager = WantedManager.instance;
            if (wantedManager == null)
                wantedManager = WantedManager.instance;

            if (wantedManager != null && wantedManager.Wanted)
            {
                wantedManager.StopPlayerWantedStatus(true);
            }

            var player = WorldHandler.instance.GetCurrentPlayer();
            if (player != null)
            {
                // Restore HP
                player.ResetHP();

                // Remove Cuffs
                if (player.AmountOfCuffs() > 0)
                {
                    player.RemoveAllCuffs();
                    //this.cuffs[i].SetState(PlayerCuff.CuffState.Hidden);
                }
            }

        }

        private void StartGhostRecording()
        {
            GhostManager.Instance.CurrentGhostState?.End();
            GhostManager.Instance.RemoveCurrentGhostState();
            GhostManager.Instance.GhostRecorder.Start();
            GhostManager.Instance.AddCurrentGhostState();
        }

        // We need to make a copy of the ghost
        private void StartGhostPlaybackAlongsideRecording()
        {

            // Check if ghost display is disabled
            if (GhostSaveData.Instance.GhostDisplayMode == GhostDisplayMode.Hide)
            {
                Debug.Log("[ScoreAttack] Ghost display mode set to Hide. Skipping ghost playback.");
                return;
            }

            // Get the personal best ghost from save data
            //Ghost bestGhost = GhostSaveData.Instance.GetOrCreateGhostData(currentStage).GetGhost(timeLimit);

            // New Solution - load an external ghost (from file) only when one is set, and fall back to the PB ghost if not.
            Ghost bestGhost;

            if (ScoreAttackManager.LoadedExternalGhost != null)
            {
                bestGhost = ScoreAttackManager.LoadedExternalGhost;
                ghostWasExternal = true;
                ScoreAttackManager.LoadedExternalGhost = null; // Clear it after use
                Debug.Log("[ScoreAttack] Using externally loaded ghost.");
            }
            else
            {
                // Fallback to personal best ghost
                bestGhost = GhostSaveData.Instance.GetOrCreateGhostData(currentStage).GetGhost(timeLimit);
                ghostWasExternal = false;
                Debug.Log("[ScoreAttack] Using personal best ghost.");
            }

            // Safety check
            if (bestGhost == null || bestGhost.Frames == null || bestGhost.Frames.Count == 0)
            {
                Debug.LogWarning("[ScoreAttack] No valid ghost to play.");
                return;
            }

            // Make a deep copy of the ghost to avoid modifying the saved one
            Ghost ghostCopy = new Ghost(bestGhost.TickDelta)
            {
                Character = bestGhost.Character,
                CharacterGUID = bestGhost.CharacterGUID,
                Outfit = bestGhost.Outfit
                //Score = bestGhost.Score; 
            };

            foreach (GhostFrame originalFrame in bestGhost.Frames)
            {
                GhostFrame frameCopy = new GhostFrame(ghostCopy)
                {
                    FrameIndex = originalFrame.FrameIndex,
                    Valid = originalFrame.Valid,

                    PlayerPosition = originalFrame.PlayerPosition,
                    PlayerRotation = originalFrame.PlayerRotation,
                    Velocity = originalFrame.Velocity,

                    BaseScore = originalFrame.BaseScore,
                    ScoreMultiplier = originalFrame.ScoreMultiplier,
                    OngoingScore = originalFrame.OngoingScore,

                    PhoneState = originalFrame.PhoneState,
                    SpraycanState = originalFrame.SpraycanState,

                    moveStyle = originalFrame.moveStyle,
                    equippedMoveStyle = originalFrame.equippedMoveStyle,
                    UsingEquippedMoveStyle = originalFrame.UsingEquippedMoveStyle,

                    Animation = new GhostFrame.GhostFrameAnimation()
                    {
                        ID = originalFrame.Animation.ID,
                        Time = originalFrame.Animation.Time,
                        ForceOverwrite = originalFrame.Animation.ForceOverwrite,
                        Instant = originalFrame.Animation.Instant,
                        AtTime = originalFrame.Animation.AtTime
                    },

                    Visual = new GhostFrame.GhostFrameVisual()
                    {
                        Position = originalFrame.Visual.Position,
                        Rotation = originalFrame.Visual.Rotation,

                        boostpackEffectMode = originalFrame.Visual.boostpackEffectMode,
                        frictionEffectMode = originalFrame.Visual.frictionEffectMode,
                        dustEmission = originalFrame.Visual.dustEmission,
                        spraypaintEmission = originalFrame.Visual.spraypaintEmission,
                        ringEmission = originalFrame.Visual.ringEmission
                    },

                    Effects = new GhostFrame.GhostFrameEffects()
                    {
                        ringParticles = originalFrame.Effects.ringParticles,
                        spraypaintParticles = originalFrame.Effects.spraypaintParticles,

                        JumpEffects = originalFrame.Effects.JumpEffects,
                        HighJumpEffects = originalFrame.Effects.HighJumpEffects,
                        DoJumpEffects = originalFrame.Effects.DoJumpEffects,
                        DoHighJumpEffects = originalFrame.Effects.DoHighJumpEffects
                    }
                };

                foreach (GhostFrame.GhostFrameSFX frameSFX in originalFrame.SFX)
                {
                    frameCopy.SFX.Add(new GhostFrame.GhostFrameSFX()
                    {
                        AudioClipID = frameSFX.AudioClipID,
                        CollectionID = frameSFX.CollectionID,
                        RandomPitchVariance = frameSFX.RandomPitchVariance,
                        Voice = frameSFX.Voice
                    });
                }

                ghostCopy.Frames.Add(frameCopy);
            }

            // Assign the replay to GhostPlayer and start it
            var ghostManager = GhostManager.Instance;
            if (ghostManager == null)
            {
                Debug.LogError("[ScoreAttack] GhostManager is not initialized.");
                return;
            }

            var ghostPlayer = ghostManager.GhostPlayer;
            ghostPlayer.Replay = ghostCopy;
            ghostPlayer.Start();
        }

        private void EndGhostPlayback()
        {
            GhostManager.Instance.RemoveCurrentGhostState();
            GhostManager.Instance.GhostRecorder.End();
            GhostManager.Instance.GhostPlayer.End();
        }

        private void TryEndGhostPlaybackIfActive()
        {
            if (GhostManager.Instance.GhostPlayer.Active) { EndGhostPlayback(); }
        }

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