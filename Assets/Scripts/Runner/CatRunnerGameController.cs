using System;
using System.Collections;
using System.Collections.Generic;
using CatHome.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CatRunnerGameController : MonoBehaviour
{
    public const int MaximumCollisionHits = 3;
    public const float CurtainDurationSeconds = 30f;
    public const int CoinsPerCurtain = 90;
    public const float MagnetDurationSeconds = 9f;
    public const float ShieldDurationSeconds = 12f;
    public const float DoubleCoinsDurationSeconds = 9f;
    public const int MaximumComboMultiplier = 5;

    [Header("Run")]
    [SerializeField] private float baseSpeed = 7f;
    [SerializeField, Min(0f)] private float speedIncreasePerCurtain = 0.1f;
    [SerializeField, Min(1f)] private float maximumSpeedMultiplier = 1.7f;
    [SerializeField] private CatRunnerPlayer player;
    [SerializeField] private CatRunnerTrackManager track;
    [SerializeField] private Camera runnerCamera;
    [SerializeField] private Canvas runnerCanvas;
    [SerializeField] private CatRunnerCameraRig cameraRig;
    [SerializeField] private CatRunnerFeedbackController feedback;
    [SerializeField] private CatRunnerAudioController audioController;

    [Header("HUD")]
    [SerializeField] private TMP_Text timerLabel;
    [SerializeField] private TMP_Text coinLabel;
    [SerializeField] private TMP_Text distanceLabel;
    [SerializeField] private TMP_Text bonusLabel;
    [SerializeField] private TMP_Text chancesLabel;
    [SerializeField] private TMP_Text countdownLabel;
    [SerializeField] private TMP_Text scoreLabel;
    [SerializeField] private TMP_Text comboLabel;
    [SerializeField] private TMP_Text powerUpLabel;
    [SerializeField] private Button pauseButton;

    [Header("Welcome")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private TMP_Text welcomeBestScoreText;
    [SerializeField] private TMP_Text welcomeEnergyText;
    [SerializeField] private Button welcomeStartButton;
    [SerializeField] private Button welcomeExitButton;
    [SerializeField] private TMP_Text welcomeMissionsText;
    [SerializeField] private Button welcomeRewardedEnergyButton;

    [Header("Results")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text resultDetails;
    [SerializeField] private Button collectButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private TMP_Text resultMissionsText;
    [SerializeField] private GameObject newBestBadge;

    [Header("Pause / Accessibility")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseExitButton;
    [SerializeField] private Button reducedMotionButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button hapticsButton;

    [Header("First Run Tutorial")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private Button tutorialSkipButton;

    private readonly List<Canvas> hiddenCanvases = new List<Canvas>();
    private readonly List<Camera> hiddenCameras = new List<Camera>();
    private readonly List<AudioListener> hiddenListeners = new List<AudioListener>();
    private readonly List<AudioSource> hiddenAudioSources = new List<AudioSource>();
    private readonly List<Light> hiddenLights = new List<Light>();
    private float elapsed;
    private float distance;
    private float hitSlowRemaining;
    private int coins;
    private int collisions;
    private int comboStreak;
    private int comboMultiplier = 1;
    private int maximumCombo;
    private int comboScoreBonus;
    private float comboRemaining;
    private float magnetRemaining;
    private float shieldRemaining;
    private float doubleCoinsRemaining;
    private int tutorialStage;
    private bool tutorialActive;
    private bool resultSettled;
    private bool resultWasNewBest;
    private bool resultActionLocked;
    private bool startActionLocked;
    private bool hudDirty = true;
    private float hudRefreshRemaining;
    private int lastEnergyUiSecond = -1;
    private bool presentationCaptured;
    private bool exiting;
    private bool paused;
    private CatRunnerResult latestResult;

    public bool IsRunning { get; private set; }
    public bool IsPaused => paused;
    public bool IsGameplayActive => IsRunning && !paused;
    public int CollisionCount => collisions;
    public int ChancesRemaining => Mathf.Max(0, MaximumCollisionHits - collisions);
    public float ElapsedSeconds => Mathf.Max(0f, elapsed);
    public int CurrentScore => CalculateScore(distance, coins) + comboScoreBonus;
    public int BestScore => CatRunnerProgressService.BestScore;
    public int ComboMultiplier => comboMultiplier;
    public int MaximumCombo => maximumCombo;
    public bool IsMagnetActive => magnetRemaining > 0f;
    public bool IsShieldActive => shieldRemaining > 0f;
    public bool IsDoubleCoinsActive => doubleCoinsRemaining > 0f;
    public float MagnetRange => 7f;
    public int CurrentCurtainNumber => GetCurtainNumber(elapsed);
    public float CurrentDifficultyMultiplier => GetSpeedMultiplier(
        CurrentCurtainNumber,
        speedIncreasePerCurtain,
        maximumSpeedMultiplier);
    public float CurrentSpeed
    {
        get
        {
            float normalSpeed = baseSpeed * CurrentDifficultyMultiplier;
            if (hitSlowRemaining <= 0f)
                return normalSpeed;
            float recovery = 1f - Mathf.Clamp01(hitSlowRemaining / 1.2f);
            return normalSpeed * Mathf.Lerp(0.52f, 1f, recovery);
        }
    }

    public static int GetCurtainNumber(float elapsedSeconds)
    {
        return Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) / CurtainDurationSeconds) + 1;
    }

    public static float GetSpeedMultiplier(
        int curtainNumber,
        float increasePerCurtain = 0.1f,
        float maximumMultiplier = 1.7f)
    {
        int completedCurtains = Mathf.Max(0, curtainNumber - 1);
        float multiplier = 1f + completedCurtains * Mathf.Max(0f, increasePerCurtain);
        return Mathf.Min(Mathf.Max(1f, maximumMultiplier), multiplier);
    }

    public static int GetScheduledCoinCount(float elapsedSeconds)
    {
        if (elapsedSeconds <= 0f)
            return 0;

        float interval = CurtainDurationSeconds / CoinsPerCurtain;
        float firstCoinTime = interval * 0.5f;
        if (elapsedSeconds < firstCoinTime)
            return 0;

        return Mathf.FloorToInt((elapsedSeconds - firstCoinTime) / interval) + 1;
    }

    public static int CalculateScore(float runDistance, int collectedCoins)
    {
        return Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0f, runDistance))) +
               Mathf.Max(0, collectedCoins) * 10;
    }

    public static int GetComboMultiplier(int consecutiveCoins)
    {
        return Mathf.Clamp(
            1 + Mathf.Max(0, consecutiveCoins) / 5,
            1,
            MaximumComboMultiplier);
    }

    public static int GetCollectedCoinValue(bool doubleCoinsActive)
    {
        return doubleCoinsActive ? 2 : 1;
    }

    private void Awake()
    {
        if (player == null)
            player = FindAnyObjectByType<CatRunnerPlayer>(FindObjectsInactive.Include);
        if (track == null)
            track = FindAnyObjectByType<CatRunnerTrackManager>(FindObjectsInactive.Include);
        if (runnerCamera == null)
            runnerCamera = GetComponentInChildren<Camera>(true);
        if (runnerCanvas == null)
            runnerCanvas = GetComponentInChildren<Canvas>(true);
        if (cameraRig == null)
            cameraRig = GetComponentInChildren<CatRunnerCameraRig>(true);
        if (feedback == null)
            feedback = GetComponentInChildren<CatRunnerFeedbackController>(true);
        if (audioController == null)
            audioController = GetComponentInChildren<CatRunnerAudioController>(true);

        if (collectButton != null)
        {
            collectButton.onClick.RemoveListener(CollectAndReturnHome);
            collectButton.onClick.AddListener(CollectAndReturnHome);
        }
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(CollectAndRetry);
            retryButton.onClick.AddListener(CollectAndRetry);
        }
        if (welcomeStartButton != null)
        {
            welcomeStartButton.onClick.RemoveListener(StartFromWelcome);
            welcomeStartButton.onClick.AddListener(StartFromWelcome);
        }
        if (welcomeExitButton != null)
        {
            welcomeExitButton.onClick.RemoveListener(ExitFromWelcome);
            welcomeExitButton.onClick.AddListener(ExitFromWelcome);
        }
        if (welcomeRewardedEnergyButton != null)
        {
            welcomeRewardedEnergyButton.onClick.RemoveListener(RequestRewardedEnergy);
            welcomeRewardedEnergyButton.onClick.AddListener(RequestRewardedEnergy);
        }
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(PauseRun);
            pauseButton.onClick.AddListener(PauseRun);
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeRun);
            resumeButton.onClick.AddListener(ResumeRun);
        }
        if (pauseExitButton != null)
        {
            pauseExitButton.onClick.RemoveListener(ExitFromPause);
            pauseExitButton.onClick.AddListener(ExitFromPause);
        }
        if (reducedMotionButton != null)
        {
            reducedMotionButton.onClick.RemoveListener(ToggleReducedMotion);
            reducedMotionButton.onClick.AddListener(ToggleReducedMotion);
        }
        if (soundButton != null)
        {
            soundButton.onClick.RemoveListener(ToggleSound);
            soundButton.onClick.AddListener(ToggleSound);
        }
        if (hapticsButton != null)
        {
            hapticsButton.onClick.RemoveListener(ToggleHaptics);
            hapticsButton.onClick.AddListener(ToggleHaptics);
        }
        if (tutorialSkipButton != null)
        {
            tutorialSkipButton.onClick.RemoveListener(SkipTutorial);
            tutorialSkipButton.onClick.AddListener(SkipTutorial);
        }
        if (player != null)
        {
            player.LaneChanged += HandleTutorialLaneChanged;
            player.JumpStarted += HandleTutorialJump;
            player.SlideStarted += HandleTutorialSlide;
            player.JumpStarted += CatRunnerProgressService.RecordJump;
        }
    }

    private void Start()
    {
        EnterRunnerPresentation();
        Scene runnerScene = gameObject.scene;
        if (runnerScene.IsValid() && runnerScene.isLoaded)
            SceneManager.SetActiveScene(runnerScene);
        ApplyAccessibilityPreferences();
        CatRunnerProgressService.TrySettlePendingResult(out _);
        if (audioController != null)
            audioController.BeginMusic();
        ShowWelcome();
    }

    private void Update()
    {
        RunnerEnergyService.Refresh();
        int energyUiSecond = Mathf.FloorToInt(Time.unscaledTime);
        if (energyUiSecond != lastEnergyUiSecond)
        {
            lastEnergyUiSecond = energyUiSecond;
            RefreshRetryButton();
            RefreshWelcome();
        }

        if (!IsGameplayActive)
            return;

        if (hitSlowRemaining > 0f)
            hitSlowRemaining -= Time.deltaTime;

        float delta = Time.deltaTime;
        elapsed += delta;
        distance += CurrentSpeed * delta;
        magnetRemaining = Mathf.Max(0f, magnetRemaining - delta);
        shieldRemaining = Mathf.Max(0f, shieldRemaining - delta);
        doubleCoinsRemaining = Mathf.Max(0f, doubleCoinsRemaining - delta);
        if (comboRemaining > 0f)
        {
            comboRemaining -= delta;
            if (comboRemaining <= 0f)
                ResetCombo();
        }

        hudRefreshRemaining -= delta;
        if (hudDirty || hudRefreshRemaining <= 0f)
        {
            RefreshHud();
            hudRefreshRemaining = .1f;
            hudDirty = false;
        }
    }

    public void RegisterCoin()
    {
        RegisterCoinInternal(false, Vector3.zero);
    }

    public void RegisterCoin(Vector3 pickupWorldPosition)
    {
        RegisterCoinInternal(true, pickupWorldPosition);
    }

    private void RegisterCoinInternal(bool hasPickupPosition, Vector3 pickupWorldPosition)
    {
        if (!IsGameplayActive)
            return;

        comboStreak++;
        comboMultiplier = GetComboMultiplier(comboStreak);
        maximumCombo = Mathf.Max(maximumCombo, comboMultiplier);
        comboRemaining = 2.5f;
        int collectedValue = GetCollectedCoinValue(IsDoubleCoinsActive);
        coins = coins > int.MaxValue - collectedValue
            ? int.MaxValue
            : coins + collectedValue;
        comboScoreBonus = comboScoreBonus > int.MaxValue - (comboMultiplier - 1) * 10
            ? int.MaxValue
            : comboScoreBonus + (comboMultiplier - 1) * 10;
        CatRunnerProgressService.RecordCoinPickup();
        if (feedback != null)
        {
            if (hasPickupPosition)
                feedback.PlayCoinAt(pickupWorldPosition);
            else
                feedback.PlayCoin();
        }
        if (audioController != null)
            audioController.PlayCoin();
        RefreshHud();
        hudDirty = false;
    }

    public void RegisterCoinMissed()
    {
        if (IsGameplayActive)
            ResetCombo();
    }

    public void RegisterPowerUp(
        CatRunnerPowerUpKind kind,
        Vector3 pickupWorldPosition)
    {
        if (!IsGameplayActive)
            return;
        switch (kind)
        {
            case CatRunnerPowerUpKind.Magnet:
                magnetRemaining = Mathf.Max(magnetRemaining, MagnetDurationSeconds);
                break;
            case CatRunnerPowerUpKind.Shield:
                shieldRemaining = Mathf.Max(shieldRemaining, ShieldDurationSeconds);
                break;
            case CatRunnerPowerUpKind.DoubleCoins:
                doubleCoinsRemaining = Mathf.Max(
                    doubleCoinsRemaining,
                    DoubleCoinsDurationSeconds);
                break;
        }
        if (feedback != null)
            feedback.PlayPowerUpAt(pickupWorldPosition);
        if (audioController != null)
            audioController.PlayPowerUp();
        RefreshHud();
        hudDirty = false;
    }

    public void RegisterObstacleHit()
    {
        if (!IsGameplayActive || player == null)
            return;
        if (IsShieldActive)
        {
            shieldRemaining = 0f;
            player.TryRegisterHit();
            if (feedback != null)
                feedback.PlayShieldBlock();
            if (audioController != null)
                audioController.PlayShieldBlock();
            hudDirty = true;
            return;
        }
        if (!player.TryRegisterHit())
            return;
        collisions++;
        ResetCombo();
        hitSlowRemaining = 1.2f;
        if (feedback != null)
            feedback.PlayHit();
        if (audioController != null)
            audioController.PlayHit();
        RefreshHud();
        if (collisions >= MaximumCollisionHits)
            CompleteRun();
    }

    public void StartFromWelcome()
    {
        if (exiting || IsRunning || startActionLocked)
            return;

        startActionLocked = true;
        if (!RunnerEnergyService.TrySpendRunEnergy())
        {
            startActionLocked = false;
            RefreshWelcome();
            return;
        }

        CatHomeSaveSystem.SaveNow();
        if (welcomePanel != null)
            welcomePanel.SetActive(false);
        if (audioController != null)
        {
            audioController.SetPaused(false);
            audioController.PlayButton();
        }
        BeginAttempt();
    }

    public void ExitFromWelcome()
    {
        if (!exiting)
        {
            exiting = true;
            if (audioController != null)
                audioController.PlayButton();
            StartCoroutine(ExitWithoutRewardRoutine());
        }
    }

    public void RequestRewardedEnergy()
    {
        CatRunnerLauncher launcher =
            FindAnyObjectByType<CatRunnerLauncher>(FindObjectsInactive.Include);
        if (launcher != null)
            launcher.RequestRewardedEnergy();
        RefreshWelcome();
    }

    public void PauseRun()
    {
        if (!IsRunning || paused || exiting)
            return;
        paused = true;
        Time.timeScale = 0f;
        if (player != null)
            player.SetRunning(false);
        if (feedback != null)
            feedback.SetRunning(false);
        if (tutorialPanel != null && tutorialActive)
            tutorialPanel.SetActive(false);
        if (pausePanel != null)
            pausePanel.SetActive(true);
        if (audioController != null)
        {
            audioController.SetPaused(true);
            audioController.PlayButton();
        }
        RefreshPreferenceButtons();
        hudDirty = true;
    }

    public void ResumeRun()
    {
        if (!IsRunning || !paused || exiting)
            return;
        paused = false;
        Time.timeScale = 1f;
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (tutorialPanel != null && tutorialActive)
        {
            tutorialPanel.SetActive(true);
            RefreshTutorialText();
        }
        if (player != null)
            player.SetRunning(true);
        if (feedback != null)
            feedback.SetRunning(true);
        if (audioController != null)
        {
            audioController.SetPaused(false);
            audioController.PlayButton();
        }
    }

    public void ExitFromPause()
    {
        if (exiting)
            return;
        exiting = true;
        paused = false;
        Time.timeScale = 1f;
        if (audioController != null)
        {
            audioController.SetPaused(false);
            audioController.PlayButton();
        }
        StartCoroutine(ExitWithoutRewardRoutine());
    }

    public void ToggleReducedMotion()
    {
        CatRunnerProgressService.SetReducedMotion(
            !CatRunnerProgressService.ReducedMotion);
        ApplyAccessibilityPreferences();
    }

    public void ToggleSound()
    {
        CatRunnerProgressService.SetSoundEnabled(
            !CatRunnerProgressService.SoundEnabled);
        RefreshPreferenceButtons();
    }

    public void ToggleHaptics()
    {
        CatRunnerProgressService.SetHapticsEnabled(
            !CatRunnerProgressService.HapticsEnabled);
        RefreshPreferenceButtons();
    }

    private void ShowWelcome()
    {
        StopAllCoroutines();
        Time.timeScale = 1f;
        IsRunning = false;
        paused = false;
        startActionLocked = false;
        resultActionLocked = false;
        resultSettled = false;
        tutorialActive = false;
        tutorialStage = 0;
        elapsed = 0f;
        distance = 0f;
        coins = 0;
        collisions = 0;
        comboStreak = 0;
        comboMultiplier = 1;
        maximumCombo = 1;
        comboScoreBonus = 0;
        comboRemaining = 0f;
        magnetRemaining = 0f;
        shieldRemaining = 0f;
        doubleCoinsRemaining = 0f;
        latestResult = default;
        if (resultPanel != null)
            resultPanel.SetActive(false);
        if (countdownLabel != null)
            countdownLabel.gameObject.SetActive(false);
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(false);
        if (newBestBadge != null)
            newBestBadge.SetActive(false);
        if (player != null)
        {
            player.ResetRun();
            player.SetRunning(false);
        }
        if (track != null)
        {
            track.SetTutorialSafety(false);
            track.ResetRun();
        }
        if (feedback != null)
            feedback.SetRunning(false);
        if (welcomePanel != null)
            welcomePanel.SetActive(true);
        RefreshHud();
        RefreshWelcome();
        RefreshPreferenceButtons();
    }

    private void RefreshWelcome()
    {
        if (welcomeBestScoreText != null)
            welcomeBestScoreText.text = $"BEST SCORE   {BestScore:N0}";
        if (welcomeMissionsText != null)
            welcomeMissionsText.text = CatRunnerProgressService.GetDailyMissionSummary();

        bool canStart = RunnerEnergyService.CanStartRun();
        if (welcomeStartButton != null)
            welcomeStartButton.interactable = canStart;
        if (welcomeRewardedEnergyButton != null)
        {
            CatRunnerLauncher launcher =
                FindAnyObjectByType<CatRunnerLauncher>(FindObjectsInactive.Include);
            bool canReward = launcher != null && launcher.CanRequestRewardedEnergy();
            welcomeRewardedEnergyButton.gameObject.SetActive(!canStart && canReward);
            welcomeRewardedEnergyButton.interactable = canReward;
        }
        if (welcomeEnergyText == null)
            return;

        if (RunnerEnergyService.IsUnlimited)
            welcomeEnergyText.text = "UNLIMITED LIVES";
        else if (canStart)
            welcomeEnergyText.text =
                $"1 LIFE PER RUN   •   {RunnerEnergyService.CurrentEnergy}/{RunnerEnergyService.MaximumEnergy} READY";
        else
        {
            TimeSpan remaining = RunnerEnergyService.TimeUntilNextEnergy();
            int seconds = Mathf.Max(0, Mathf.CeilToInt((float)remaining.TotalSeconds));
            welcomeEnergyText.text = $"NEXT LIFE   {seconds / 60:00}:{seconds % 60:00}";
        }
    }

    private void BeginAttempt()
    {
        StopAllCoroutines();
        Time.timeScale = 1f;
        IsRunning = false;
        paused = false;
        startActionLocked = true;
        resultActionLocked = false;
        resultSettled = false;
        resultWasNewBest = false;
        tutorialActive = false;
        tutorialStage = 0;
        elapsed = 0f;
        distance = 0f;
        hitSlowRemaining = 0f;
        coins = 0;
        collisions = 0;
        comboStreak = 0;
        comboMultiplier = 1;
        maximumCombo = 1;
        comboScoreBonus = 0;
        comboRemaining = 0f;
        magnetRemaining = 0f;
        shieldRemaining = 0f;
        doubleCoinsRemaining = 0f;
        latestResult = default;

        if (resultPanel != null)
            resultPanel.SetActive(false);
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
        if (newBestBadge != null)
            newBestBadge.SetActive(false);
        if (player != null)
        {
            player.ResetRun();
            player.SetRunning(false);
        }
        if (track != null)
        {
            track.SetTutorialSafety(false);
            track.ResetRun();
        }
        if (feedback != null)
            feedback.SetRunning(false);
        RefreshHud();
        RefreshRetryButton();
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        if (countdownLabel != null)
            countdownLabel.gameObject.SetActive(true);

        for (int value = 3; value >= 1; value--)
        {
            if (countdownLabel != null)
                countdownLabel.text = value.ToString();
            if (feedback != null)
                feedback.PlayCountdown();
            if (audioController != null)
                audioController.PlayCountdown();
            yield return new WaitForSeconds(0.65f);
        }

        if (countdownLabel != null)
        {
            countdownLabel.text = "RUN!";
            yield return new WaitForSeconds(0.45f);
            countdownLabel.gameObject.SetActive(false);
        }

        IsRunning = true;
        startActionLocked = false;
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(true);
        if (player != null)
            player.SetRunning(true);
        if (feedback != null)
            feedback.BeginRun();
        if (audioController != null)
            audioController.PlayStart();
        if (!CatRunnerProgressService.TutorialCompleted)
            BeginTutorial();
    }

    private void BeginTutorial()
    {
        tutorialActive = true;
        tutorialStage = 0;
        if (track != null)
            track.SetTutorialSafety(true);
        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);
        RefreshTutorialText();
    }

    public void SkipTutorial()
    {
        if (!tutorialActive)
            return;
        CompleteTutorial();
    }

    private void HandleTutorialLaneChanged()
    {
        if (tutorialActive && tutorialStage == 0)
            AdvanceTutorial();
    }

    private void HandleTutorialJump()
    {
        if (tutorialActive && tutorialStage == 1)
            AdvanceTutorial();
    }

    private void HandleTutorialSlide()
    {
        if (tutorialActive && tutorialStage == 2)
            CompleteTutorial();
    }

    private void AdvanceTutorial()
    {
        tutorialStage++;
        RefreshTutorialText();
    }

    private void RefreshTutorialText()
    {
        if (tutorialText == null)
            return;
        switch (tutorialStage)
        {
            case 0:
                tutorialText.text = "DRAG LEFT OR RIGHT\nCHANGE LANES";
                break;
            case 1:
                tutorialText.text = "SWIPE UP\nJUMP OVER OBSTACLES";
                break;
            default:
                tutorialText.text = "SWIPE DOWN\nSLIDE • FAST-DROP IN AIR";
                break;
        }
    }

    private void CompleteTutorial()
    {
        tutorialActive = false;
        if (track != null)
            track.SetTutorialSafety(false);
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
        CatRunnerProgressService.CompleteTutorial();
        CatHomeSaveSystem.SaveNow();
    }

    private void CompleteRun()
    {
        IsRunning = false;
        paused = false;
        Time.timeScale = 1f;
        tutorialActive = false;
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(false);
        if (track != null)
            track.SetTutorialSafety(false);
        if (feedback != null)
            feedback.SetRunning(false);
        float playedDuration = Mathf.Max(0f, elapsed);
        if (player != null)
        {
            player.SetRunning(false);
            player.PlayCelebration();
        }

        latestResult = CatRunnerResult.Create(
            Guid.NewGuid().ToString("N"),
            playedDuration,
            distance,
            coins,
            collisions,
            CatRunnerSessionContext.CareBonusPercent);

        int score = CurrentScore;
        int previousBest = BestScore;
        resultWasNewBest = score > previousBest;
        CatRunnerProgressService.SetPendingResult(latestResult, score);
        CatRunnerProgressService.RecordRunDistance(distance);
        // Persist the recovery record before payout. Settlement happens on the
        // next frame, when EconomyService can atomically save both its processed
        // transaction id and the cleared recovery record.
        CatHomeSaveSystem.SaveNow();
        resultSettled = false;

        if (resultTitle != null)
            resultTitle.text = "RUN COMPLETE!";
        if (resultMissionsText != null)
            resultMissionsText.text = CatRunnerProgressService.GetDailyMissionSummary();
        if (newBestBadge != null)
            newBestBadge.SetActive(resultWasNewBest);
        if (resultPanel != null)
            resultPanel.SetActive(true);
        if (collectButton != null)
            collectButton.interactable = resultSettled;
        if (audioController != null)
            audioController.PlayResult();
        StartCoroutine(AnimateResultRoutine(score));
        StartCoroutine(SettleCompletedRunRoutine());
        RefreshHud();
        RefreshRetryButton();
    }

    private IEnumerator SettleCompletedRunRoutine()
    {
        yield return null;
        resultSettled = CatRunnerProgressService.TrySettlePendingResult(
            out EconomyTransactionResult settlement);
        if (!resultSettled)
        {
            if (resultTitle != null)
                resultTitle.text = "REWARD RETRY NEEDED";
            Debug.LogWarning($"Cat Runner reward could not be settled: {settlement}", this);
        }
        else if (resultTitle != null)
        {
            resultTitle.text = "RUN COMPLETE!";
        }
        CatHomeSaveSystem.SaveNow();
        RefreshRetryButton();
    }

    private IEnumerator AnimateResultRoutine(int score)
    {
        const float duration = .8f;
        float elapsedAnimation = CatRunnerProgressService.ReducedMotion ? duration : 0f;
        while (elapsedAnimation < duration)
        {
            elapsedAnimation += Time.unscaledDeltaTime;
            float amount = Mathf.Clamp01(elapsedAnimation / duration);
            amount = 1f - Mathf.Pow(1f - amount, 3f);
            RefreshResultDetails(score, amount);
            yield return null;
        }
        RefreshResultDetails(score, 1f);
    }

    private void RefreshResultDetails(int score, float amount)
    {
        if (resultDetails == null)
            return;
        int shownDistance = Mathf.RoundToInt(latestResult.Distance * amount);
        int shownCoins = Mathf.RoundToInt(latestResult.CoinsCollected * amount);
        int shownScore = Mathf.RoundToInt(score * amount);
        long shownBonus = (long)Math.Round(latestResult.BonusCoins * (double)amount);
        long shownTotal = (long)Math.Round(latestResult.TotalCoins * (double)amount);
        resultDetails.text =
            $"TIME   {FormatElapsed(latestResult.DurationSeconds)}      " +
            $"STAGE   {GetCurtainNumber(latestResult.DurationSeconds)}\n" +
            $"DISTANCE   {shownDistance} m      COINS   {shownCoins}\n" +
            $"SCORE   {shownScore:N0}      BEST   {BestScore:N0}\n" +
            $"MAX COMBO   x{maximumCombo}      HITS   " +
            $"{latestResult.Collisions}/{MaximumCollisionHits}\n" +
            $"HAPPY BONUS   +{shownBonus}      TOTAL   {shownTotal}";
    }

    private void CollectAndReturnHome()
    {
        if (exiting || resultActionLocked)
            return;
        resultActionLocked = true;
        exiting = true;
        DisableResultActions();
        if (audioController != null)
            audioController.PlayButton();
        StartCoroutine(ReturnHomeRoutine());
    }

    private void CollectAndRetry()
    {
        if (resultActionLocked || exiting)
            return;
        resultActionLocked = true;
        DisableResultActions();
        if (!TrySettleLatestResult())
        {
            resultActionLocked = false;
            RefreshRetryButton();
            return;
        }

        if (!RunnerEnergyService.TrySpendRunEnergy())
        {
            if (resultTitle != null)
                resultTitle.text = "NO ENERGY";
            resultActionLocked = false;
            RefreshRetryButton();
            return;
        }

        if (audioController != null)
            audioController.PlayButton();
        BeginAttempt();
        StartCoroutine(SaveEnergyNextFrame());
    }

    private IEnumerator SaveEnergyNextFrame()
    {
        yield return null;
        CatHomeSaveSystem.SaveNow();
    }

    private bool TrySettleLatestResult()
    {
        if (resultSettled && !CatRunnerProgressService.HasPendingResult)
            return true;

        resultSettled = CatRunnerProgressService.TrySettlePendingResult(
            out EconomyTransactionResult reward);
        if (resultSettled)
        {
            if (collectButton != null)
                collectButton.interactable = true;
            return true;
        }

        if (resultTitle != null)
            resultTitle.text = "REWARD RETRY NEEDED";
        Debug.LogWarning($"Cat Runner reward could not be settled: {reward}", this);
        return false;
    }

    private IEnumerator ReturnHomeRoutine()
    {
        if (!TrySettleLatestResult())
        {
            exiting = false;
            resultActionLocked = false;
            RefreshRetryButton();
            yield break;
        }

        yield return null;
        CatHomeSaveSystem.SaveNow();

        Scene home = SceneManager.GetSceneByName(
            CatRunnerSessionContext.ReturnRoomSceneName);
        if (home.IsValid() && home.isLoaded)
        {
            DisableRunnerPresentation();
            RestoreHomePresentation();
            SceneManager.SetActiveScene(home);
            AsyncOperation unload = SceneManager.UnloadSceneAsync(gameObject.scene);
            if (unload != null)
                while (!unload.isDone)
                    yield return null;
        }
        else
        {
            SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
    }

    private IEnumerator ExitWithoutRewardRoutine()
    {
        exiting = true;
        Time.timeScale = 1f;

        Scene home = SceneManager.GetSceneByName(
            CatRunnerSessionContext.ReturnRoomSceneName);
        if (home.IsValid() && home.isLoaded)
        {
            DisableRunnerPresentation();
            RestoreHomePresentation();
            SceneManager.SetActiveScene(home);
            AsyncOperation unload = SceneManager.UnloadSceneAsync(gameObject.scene);
            if (unload != null)
                while (!unload.isDone)
                    yield return null;
        }
        else
        {
            SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
    }

    private void RefreshHud()
    {
        if (timerLabel != null)
            timerLabel.text = FormatElapsed(elapsed);
        if (coinLabel != null)
            coinLabel.text = coins.ToString();
        if (distanceLabel != null)
            distanceLabel.text = Mathf.RoundToInt(distance) + " m";
        if (bonusLabel != null)
            bonusLabel.text =
                $"STAGE {CurrentCurtainNumber}   •   HAPPY CAT +{CatRunnerSessionContext.CareBonusPercent}%";
        if (chancesLabel != null)
            chancesLabel.text = $"CHANCES  {ChancesRemaining}/{MaximumCollisionHits}";
        if (scoreLabel != null)
            scoreLabel.text = $"SCORE  {CurrentScore:N0}";
        if (comboLabel != null)
        {
            comboLabel.transform.parent.gameObject.SetActive(comboMultiplier > 1);
            comboLabel.text = $"COMBO  x{comboMultiplier}";
        }
        if (powerUpLabel != null)
        {
            string active = BuildPowerUpSummary();
            powerUpLabel.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(active));
            powerUpLabel.text = active;
        }
        if (pauseButton != null)
            pauseButton.interactable = IsRunning && !paused && !exiting;
    }

    private string BuildPowerUpSummary()
    {
        string summary = string.Empty;
        if (IsMagnetActive)
            summary = $"MAGNET {Mathf.CeilToInt(magnetRemaining)}s";
        if (IsShieldActive)
            summary = AppendPowerUp(
                summary,
                $"SHIELD {Mathf.CeilToInt(shieldRemaining)}s");
        if (IsDoubleCoinsActive)
            summary = AppendPowerUp(
                summary,
                $"2x COINS {Mathf.CeilToInt(doubleCoinsRemaining)}s");
        return summary;
    }

    private static string AppendPowerUp(string existing, string value)
    {
        return string.IsNullOrEmpty(existing)
            ? value
            : existing + "   •   " + value;
    }

    private void ResetCombo()
    {
        if (comboStreak == 0 && comboMultiplier == 1)
            return;
        comboStreak = 0;
        comboMultiplier = 1;
        comboRemaining = 0f;
        hudDirty = true;
    }

    private static string FormatElapsed(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

    private void RefreshRetryButton()
    {
        if (retryButton == null)
            return;

        bool canRetry = RunnerEnergyService.CanStartRun();
        if (collectButton != null && resultPanel != null && resultPanel.activeSelf)
            collectButton.interactable = resultSettled && !resultActionLocked && !exiting;
        retryButton.interactable = canRetry && !resultActionLocked && !exiting;
        TMP_Text label = retryButton.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        if (RunnerEnergyService.IsUnlimited)
            label.text = "RUN AGAIN";
        else if (canRetry)
            label.text = $"RUN AGAIN  •  LIVES {RunnerEnergyService.CurrentEnergy}";
        else
        {
            TimeSpan time = RunnerEnergyService.TimeUntilNextEnergy();
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt((float)time.TotalSeconds));
            label.text = $"NEXT ENERGY  {totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }

    private void DisableResultActions()
    {
        if (collectButton != null)
            collectButton.interactable = false;
        if (retryButton != null)
            retryButton.interactable = false;
    }

    private void ApplyAccessibilityPreferences()
    {
        bool reduced = CatRunnerProgressService.ReducedMotion;
        if (player != null)
            player.SetReducedMotion(reduced);
        if (cameraRig != null)
            cameraRig.SetReducedMotion(reduced);
        if (feedback != null)
            feedback.SetReducedMotion(reduced);
        foreach (CatRunnerScenerySegment scenery in
                 GetComponentsInChildren<CatRunnerScenerySegment>(true))
        {
            if (scenery != null)
                scenery.SetReducedMotion(reduced);
        }
        if (runnerCanvas != null)
        {
            foreach (PremiumButtonFx buttonFx in
                     runnerCanvas.GetComponentsInChildren<PremiumButtonFx>(true))
            {
                if (buttonFx != null)
                    buttonFx.SetReducedMotion(reduced);
            }
            foreach (PremiumAmbientSparkle sparkle in
                     runnerCanvas.GetComponentsInChildren<PremiumAmbientSparkle>(true))
            {
                if (sparkle != null)
                    sparkle.SetReducedMotion(reduced);
            }
        }
        RefreshPreferenceButtons();
    }

    private void RefreshPreferenceButtons()
    {
        SetButtonText(
            reducedMotionButton,
            CatRunnerProgressService.ReducedMotion
                ? "REDUCED MOTION  ON"
                : "REDUCED MOTION  OFF");
        SetButtonText(
            soundButton,
            CatRunnerProgressService.SoundEnabled ? "SOUND  ON" : "SOUND  OFF");
        SetButtonText(
            hapticsButton,
            CatRunnerProgressService.HapticsEnabled ? "HAPTICS  ON" : "HAPTICS  OFF");
    }

    private static void SetButtonText(Button button, string value)
    {
        if (button == null)
            return;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = value;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && IsRunning && !paused)
            PauseRun();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
#if !UNITY_EDITOR
        if (!hasFocus && IsRunning && !paused)
            PauseRun();
#endif
    }

    private void EnterRunnerPresentation()
    {
        if (presentationCaptured)
            return;
        presentationCaptured = true;

        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas != null && canvas != runnerCanvas && canvas.enabled)
            {
                hiddenCanvases.Add(canvas);
                canvas.enabled = false;
            }
        }

        foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (camera != null && camera != runnerCamera && camera.enabled)
            {
                hiddenCameras.Add(camera);
                camera.enabled = false;
            }
        }

        AudioListener runnerListener = runnerCamera != null
            ? runnerCamera.GetComponent<AudioListener>()
            : null;
        foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Include))
        {
            if (listener != null && listener != runnerListener && listener.enabled)
            {
                hiddenListeners.Add(listener);
                listener.enabled = false;
            }
        }

        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include))
        {
            if (source != null && source.gameObject.scene != gameObject.scene &&
                source.isPlaying)
            {
                hiddenAudioSources.Add(source);
                source.Pause();
            }
        }

        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include))
        {
            if (light != null && light.gameObject.scene != gameObject.scene && light.enabled)
            {
                hiddenLights.Add(light);
                light.enabled = false;
            }
        }

        // CatHome_UI owns the one shared EventSystem. It remains active while
        // home canvases are hidden and drives the additive Runner canvas too.
    }

    private void RestoreHomePresentation()
    {
        if (!presentationCaptured)
            return;
        presentationCaptured = false;

        for (int i = 0; i < hiddenCanvases.Count; i++)
            if (hiddenCanvases[i] != null)
                hiddenCanvases[i].enabled = true;
        for (int i = 0; i < hiddenCameras.Count; i++)
            if (hiddenCameras[i] != null)
                hiddenCameras[i].enabled = true;
        for (int i = 0; i < hiddenListeners.Count; i++)
            if (hiddenListeners[i] != null)
                hiddenListeners[i].enabled = true;
        for (int i = 0; i < hiddenAudioSources.Count; i++)
            if (hiddenAudioSources[i] != null)
                hiddenAudioSources[i].UnPause();
        for (int i = 0; i < hiddenLights.Count; i++)
            if (hiddenLights[i] != null)
                hiddenLights[i].enabled = true;
        hiddenCanvases.Clear();
        hiddenCameras.Clear();
        hiddenListeners.Clear();
        hiddenAudioSources.Clear();
        hiddenLights.Clear();
    }

    private void DisableRunnerPresentation()
    {
        Scene runnerScene = gameObject.scene;
        CatRunnerThemeController theme =
            GetComponentInChildren<CatRunnerThemeController>(true);
        if (theme != null)
            theme.enabled = false;
        foreach (Camera sceneCamera in
                 FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (sceneCamera != null && sceneCamera.gameObject.scene == runnerScene)
                sceneCamera.enabled = false;
        }

        foreach (AudioListener listener in
                 FindObjectsByType<AudioListener>(FindObjectsInactive.Include))
        {
            if (listener != null && listener.gameObject.scene == runnerScene)
                listener.enabled = false;
        }

        foreach (Canvas sceneCanvas in
                 FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (sceneCanvas != null && sceneCanvas.gameObject.scene == runnerScene)
                sceneCanvas.enabled = false;
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        RestoreHomePresentation();
        if (audioController != null)
            audioController.StopMusic();
        if (collectButton != null)
            collectButton.onClick.RemoveListener(CollectAndReturnHome);
        if (retryButton != null)
            retryButton.onClick.RemoveListener(CollectAndRetry);
        if (welcomeStartButton != null)
            welcomeStartButton.onClick.RemoveListener(StartFromWelcome);
        if (welcomeExitButton != null)
            welcomeExitButton.onClick.RemoveListener(ExitFromWelcome);
        if (welcomeRewardedEnergyButton != null)
            welcomeRewardedEnergyButton.onClick.RemoveListener(RequestRewardedEnergy);
        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(PauseRun);
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(ResumeRun);
        if (pauseExitButton != null)
            pauseExitButton.onClick.RemoveListener(ExitFromPause);
        if (reducedMotionButton != null)
            reducedMotionButton.onClick.RemoveListener(ToggleReducedMotion);
        if (soundButton != null)
            soundButton.onClick.RemoveListener(ToggleSound);
        if (hapticsButton != null)
            hapticsButton.onClick.RemoveListener(ToggleHaptics);
        if (tutorialSkipButton != null)
            tutorialSkipButton.onClick.RemoveListener(SkipTutorial);
        if (player != null)
        {
            player.LaneChanged -= HandleTutorialLaneChanged;
            player.JumpStarted -= HandleTutorialJump;
            player.SlideStarted -= HandleTutorialSlide;
            player.JumpStarted -= CatRunnerProgressService.RecordJump;
        }
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        CatRunnerPlayer runnerPlayer,
        CatRunnerTrackManager trackManager,
        Camera camera,
        Canvas canvas,
        TMP_Text timer,
        TMP_Text coin,
        TMP_Text distanceText,
        TMP_Text bonus,
        TMP_Text chances,
        TMP_Text countdown,
        GameObject welcome,
        TMP_Text welcomeBestScore,
        TMP_Text welcomeEnergy,
        Button welcomeStart,
        Button welcomeExit,
        GameObject results,
        TMP_Text title,
        TMP_Text details,
        Button collect,
        Button retry)
    {
        baseSpeed = 7f;
        speedIncreasePerCurtain = 0.1f;
        maximumSpeedMultiplier = 1.7f;
        player = runnerPlayer;
        track = trackManager;
        runnerCamera = camera;
        runnerCanvas = canvas;
        timerLabel = timer;
        coinLabel = coin;
        distanceLabel = distanceText;
        bonusLabel = bonus;
        chancesLabel = chances;
        countdownLabel = countdown;
        welcomePanel = welcome;
        welcomeBestScoreText = welcomeBestScore;
        welcomeEnergyText = welcomeEnergy;
        welcomeStartButton = welcomeStart;
        welcomeExitButton = welcomeExit;
        resultPanel = results;
        resultTitle = title;
        resultDetails = details;
        collectButton = collect;
        retryButton = retry;
    }

    public void EditorConfigurePremium(
        CatRunnerAudioController audio,
        TMP_Text score,
        TMP_Text combo,
        TMP_Text powerUps,
        Button pause,
        TMP_Text welcomeMissions,
        Button welcomeRewarded,
        TMP_Text resultMissions,
        GameObject bestBadge,
        GameObject pauseOverlay,
        Button resume,
        Button exitPause,
        Button motion,
        Button sound,
        Button haptics,
        GameObject tutorialOverlay,
        TMP_Text tutorialMessage,
        Button tutorialSkip)
    {
        audioController = audio;
        scoreLabel = score;
        comboLabel = combo;
        powerUpLabel = powerUps;
        pauseButton = pause;
        welcomeMissionsText = welcomeMissions;
        welcomeRewardedEnergyButton = welcomeRewarded;
        resultMissionsText = resultMissions;
        newBestBadge = bestBadge;
        pausePanel = pauseOverlay;
        resumeButton = resume;
        pauseExitButton = exitPause;
        reducedMotionButton = motion;
        soundButton = sound;
        hapticsButton = haptics;
        tutorialPanel = tutorialOverlay;
        tutorialText = tutorialMessage;
        tutorialSkipButton = tutorialSkip;
    }
#endif
}
