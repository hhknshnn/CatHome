using System;
using System.Collections;
using System.Collections.Generic;
using CatHome.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CatCatchGameController : MonoBehaviour
{
    public const float HuntDuration = 60f;
    public const int CoinRewardPerCatch = CatchScoring.CoinsPerCatch;
    private const float MouseSpawnClearance = CatchHuntRules.MouseSpawnClearance;
    private const float MouseSpawnGrace = CatchHuntRules.MouseSpawnGrace;
    private const float MaximumTutorialSeconds = 25f;
    private static readonly Color LowTimeColor = new Color32(178, 54, 51, 255);

    [SerializeField] private CatCatchPlayer player;
    [SerializeField] private CatCatchMouse[] mice = Array.Empty<CatCatchMouse>();
    [SerializeField] private Transform spawnRoot;
    [SerializeField] private Camera huntCamera;
    [SerializeField] private AudioListener huntListener;
    [SerializeField] private Canvas huntCanvas;
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private TMP_Text welcomeBestText;
    [SerializeField] private TMP_Text welcomeLivesText;
    [SerializeField] private Button welcomeStartButton;
    [SerializeField] private Button welcomeExitButton;
    [SerializeField] private Button welcomeGamesButton;
    [SerializeField] private Button welcomeRewardedButton;
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private TMP_Text scoreLabel;
    [SerializeField] private TMP_Text comboLabel;
    [SerializeField] private TMP_Text timerLabel;
    [SerializeField] private TMP_Text catchLabel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text resultDetails;
    [SerializeField] private Button collectButton;
    [SerializeField] private Button resultGamesButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private Button tutorialSkipButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseExitButton;
    [SerializeField] private Button pauseGamesButton;

    private bool hunting;
    private bool settling;
    private bool exiting;
    private bool paused;
    private float remaining;
    private int catches;
    private int score;
    private int combo;
    private int comboStepsTotal;
    private string huntId = string.Empty;
    private float lastCatchTime;
    private static int bestScore;
    private Coroutine huntRoutine;
    private readonly List<Canvas> hiddenCanvases = new List<Canvas>();
    private readonly List<Camera> hiddenCameras = new List<Camera>();
    private readonly List<AudioListener> hiddenListeners = new List<AudioListener>();
    private bool presentationCaptured;
    private bool tutorialActive;
    private int tutorialStage;
    private float tutorialHold;
    private float tutorialElapsed;
    private CatchBurstFx[] catchBursts = System.Array.Empty<CatchBurstFx>();
    private int nextBurst;

    public static int BestScore => bestScore;
    public int Catches => catches;
    public int Score => score;
    public bool IsHunting => hunting;
    public bool IsPaused => paused;
    public int StrikesResolved { get; private set; }
    public int StrikesMissed { get; private set; }
    public float LastStrikeGap { get; private set; } = -1f;

    private void Awake()
    {
        CatchLivesService.EnsureInitialized();
        if (mice == null || mice.Length == 0)
            mice = GetComponentsInChildren<CatCatchMouse>(true);
        catchBursts = GetComponentsInChildren<CatchBurstFx>(true);
        if (player == null)
            player = GetComponentInChildren<CatCatchPlayer>(true);
        if (spawnRoot == null)
        {
            Transform found = transform.Find("CatSpawn");
            if (found != null)
                spawnRoot = found;
        }
        if (player != null)
            player.BindCamera(huntCamera);
        Bind(welcomeStartButton, StartHunt);
        Bind(welcomeExitButton, ExitToHome);
        Bind(welcomeGamesButton, ReturnToGamesFromWelcome);
        Bind(pauseGamesButton, ReturnToGamesFromPause);
        Bind(resultGamesButton, ReturnToGamesFromResult);
        Bind(welcomeRewardedButton, RequestRewardedLives);
        Bind(collectButton, ExitToHome);
        Bind(retryButton, StartHunt);
        Bind(tutorialSkipButton, SkipTutorial);
        Bind(pauseButton, PauseHunt);
        Bind(resumeButton, ResumeHunt);
        Bind(pauseExitButton, ExitFromPause);
        ShowWelcome();
        ParkHomePresentation();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        Unbind(welcomeStartButton, StartHunt);
        Unbind(welcomeExitButton, ExitToHome);
        Unbind(welcomeGamesButton, ReturnToGamesFromWelcome);
        Unbind(pauseGamesButton, ReturnToGamesFromPause);
        Unbind(resultGamesButton, ReturnToGamesFromResult);
        Unbind(welcomeRewardedButton, RequestRewardedLives);
        Unbind(collectButton, ExitToHome);
        Unbind(retryButton, StartHunt);
        Unbind(tutorialSkipButton, SkipTutorial);
        Unbind(pauseButton, PauseHunt);
        Unbind(resumeButton, ResumeHunt);
        Unbind(pauseExitButton, ExitFromPause);
        RestoreHomePresentation();
    }

    private void Update()
    {
        if (welcomePanel != null && welcomePanel.activeSelf)
            RefreshWelcome();

        if (!hunting || player == null || paused)
            return;

        if (tutorialActive)
        {
            TickTutorial();
            ResolveStrike();
            KeepMiceOnTheFloor();
            RefreshHud();
            return;
        }

        remaining = Mathf.Max(0f, remaining - Time.deltaTime);
        ResolveStrike();
        KeepMiceOnTheFloor();
        RefreshHud();
        if (remaining <= 0f)
            FinishHunt(true);
    }

    public void PauseHunt()
    {
        if (!hunting || paused || settling || exiting)
            return;
        paused = true;
        Time.timeScale = 0f;
        if (player != null)
            player.SetPaused(true);
        if (tutorialPanel != null && tutorialActive)
            tutorialPanel.SetActive(false);
        SetPanel(pausePanel, true);
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(false);
    }

    public void ResumeHunt()
    {
        if (!hunting || !paused || exiting)
            return;
        paused = false;
        Time.timeScale = 1f;
        SetPanel(pausePanel, false);
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(true);
        if (tutorialPanel != null && tutorialActive)
        {
            tutorialPanel.SetActive(true);
            RefreshTutorialText();
        }
        if (player != null)
            player.SetPaused(false);
    }

    public void ExitFromPause()
    {
        if (!paused || settling || exiting)
            return;
        ExitToHome();
    }

    public void StartHunt()
    {
        if (hunting || settling || exiting)
            return;
        if (!CatchLivesService.TrySpendHuntLife())
        {
            RefreshWelcome();
            return;
        }

        CatHomeSaveSystem.SaveNow();
        catches = 0;
        score = 0;
        combo = 0;
        comboStepsTotal = 0;
        StrikesResolved = 0;
        StrikesMissed = 0;
        LastStrikeGap = -1f;
        huntId = Guid.NewGuid().ToString("N");
        lastCatchTime = float.NegativeInfinity;
        remaining = HuntDuration;
        hunting = true;
        GameAudio.Play(AudioCue.Start, .85f, AudioBus.MiniGame);
        SetPanel(welcomePanel, false);
        SetPanel(resultPanel, false);
        SetPanel(hudRoot, true);
        SetPanel(pausePanel, false);
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(true);
        if (player != null)
        {
            player.BindCamera(huntCamera);
            player.SnapTo(spawnRoot != null ? spawnRoot.position : Vector3.zero);
            player.SetInputEnabled(true);
        }
        LaunchMice();
        RefreshHud();
        if (!CatchLivesService.TutorialCompleted)
            BeginTutorial();
    }

    private Transform ArenaRoot =>
        spawnRoot != null ? spawnRoot.parent : transform;

    public void RequestRewardedLives()
    {
        if (!CatchLivesService.CanClaimRewardedAd())
            return;
#if UNITY_EDITOR
        if (CatchLivesService.TryGrantRewardedAd())
            CatHomeSaveSystem.SaveNow();
#else
        if (CatRunnerRewardedAdBridge.TryShow(verified =>
            {
                if (verified && CatchLivesService.TryGrantRewardedAd())
                    CatHomeSaveSystem.SaveNow();
            }))
        {
            return;
        }
#endif
        RefreshWelcome();
    }

    public void ExitToHome() => ExitToDestination(false);

    public void ReturnToGamesFromWelcome()
    {
        if (!hunting && welcomePanel != null && welcomePanel.activeSelf)
            ExitToDestination(true);
    }

    public void ReturnToGamesFromPause()
    {
        if (paused)
            ExitToDestination(true);
    }

    public void ReturnToGamesFromResult()
    {
        if (!hunting && resultPanel != null && resultPanel.activeSelf)
            ExitToDestination(true);
    }

    private void ExitToDestination(bool games)
    {
        if (exiting || settling)
            return;
        // Lock before stopping the round: another queued click cannot spend a
        // life or start a second unload while the scene is still alive.
        exiting = true;
        CatRunnerSessionContext.SetReturnToGames(games);
        if (hunting)
            FinishHunt(false);
        if (huntCanvas != null)
            huntCanvas.enabled = false;
        StartCoroutine(UnloadRoutine());
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            PauseHunt();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
#if !UNITY_EDITOR
        if (!hasFocus)
            PauseHunt();
#endif
    }

    private void EndHunt()
    {
        FinishHunt(true);
    }

    private void FinishHunt(bool completed)
    {
        if (!hunting || settling)
            return;
        hunting = false;
        settling = true;
        paused = false;
        Time.timeScale = 1f;
        HideTutorial(false);
        if (player != null)
            player.SetInputEnabled(false);
        HideMice();
        SetPanel(pausePanel, false);
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(false);
        SetPanel(hudRoot, false);

        if (!completed)
        {
            SetPanel(resultPanel, false);
            settling = false;
            return;
        }

        bool newBest = score > bestScore;
        GameAudio.Play(AudioCue.Result, .9f, AudioBus.MiniGame);
        if (newBest)
            bestScore = score;
        long coins = CatchScoring.CoinsForCatches(catches);
        // Keep the existing quest/save identity completable after retiring the
        // separate home mouse station. One completed hunt fulfils one quest step.
        if(catches>=3)ProgressionService.RecordProgress(QuestType.MouseHunt);
        if (coins > 0)
        {
            EconomyService.GrantReward(
                RewardBundle.Coins(coins),
                EconomySource.CatCatch,
                "cat-catch:" + huntId);
        }
        _ = CompetitionService.SubmitCatchAsync(
            huntId,
            HuntDuration - remaining,
            catches,
            comboStepsTotal,
            StrikesResolved,
            score);
        CatHomeSaveSystem.SaveNow();
        SetPanel(resultPanel, true);
        if (resultTitle != null)
            resultTitle.text = GameLanguageService.Text(catches>0?"catch.complete":"catch.nice_try");
        if (resultDetails != null)
        {
            var view=resultPanel.GetComponent<MiniGameResultView>(); if(view!=null)view.Present(score,coins);
            resultDetails.text=GameLanguageService.Format("catch.result_stats",catches,bestScore);
            var bestBadge=resultPanel.transform.Find("ResultsSafeArea/ResultsCardLayout/NewBestBadge");
            if(bestBadge!=null)bestBadge.gameObject.SetActive(newBest);

        }
        if (retryButton != null)
            retryButton.interactable = CatchLivesService.CanStartHunt();
        settling = false;
    }

    /// <summary>
    /// A catch can only be produced by a landed pounce or a paw swat, and never
    /// resolves more than one mouse per strike.
    /// </summary>
    private void ResolveStrike()
    {
        if (player == null || mice == null)
            return;
        if (!player.TryConsumeStrike(out Vector3 strikePoint, out CatCatchMouse hunted))
            return;

        StrikesResolved++;
        if (hunted != null)
        {
            Vector3 gap = hunted.transform.position - strikePoint;
            gap.y = 0f;
            LastStrikeGap = gap.magnitude;
        }

        CatCatchMouse target = ResolveHuntedMouse(hunted, strikePoint);
        if (target == null)
        {
            StrikesMissed++;
            GameAudio.Play(AudioCue.Miss, .55f, AudioBus.MiniGame);
            combo = 0;
            PanicNearbyMice(strikePoint);
            return;
        }

        PlayCatchBurst(target.transform.position);
        target.Hide();
        catches++;
        combo = CatchScoring.ComboFor(combo, Time.time - lastCatchTime);
        GameAudio.Play(combo > 1 ? AudioCue.Combo : AudioCue.Catch, .85f, AudioBus.MiniGame);
        comboStepsTotal += Mathf.Clamp(combo - 1, 0, CatchScoring.MaximumComboSteps);
        lastCatchTime = Time.time;
        score += CatchScoring.ScoreForCatch(combo);
        player.PlayCatchReaction();
        LaunchOneMouse();
        if (tutorialActive && tutorialStage == 1)
            AdvanceTutorial();
    }

    /// <summary>
    /// The pounced mouse wins the strike when the landing reaches it. Any other
    /// mouse standing on the landing spot counts too, but nothing else does.
    /// </summary>
    private CatCatchMouse ResolveHuntedMouse(CatCatchMouse hunted, Vector3 strikePoint)
    {
        if (hunted != null && hunted.IsCatchable)
        {
            Vector3 delta = hunted.transform.position - strikePoint;
            delta.y = 0f;
            return CatchHuntRules.IsWithinStrike(delta.magnitude) ? hunted : null;
        }

        return FindStrikeTarget(strikePoint, CatchHuntRules.StrikeRadius);
    }

    private void PlayCatchBurst(Vector3 worldPosition)
    {
        if (catchBursts == null || catchBursts.Length == 0)
            return;
        for (int i = 0; i < catchBursts.Length; i++)
        {
            CatchBurstFx fx = catchBursts[nextBurst % catchBursts.Length];
            nextBurst++;
            if (fx != null)
            {
                fx.Play(worldPosition + Vector3.up * 0.1f);
                return;
            }
        }
    }

    /// <summary>A missed pounce scatters the mice it landed among.</summary>
    private void PanicNearbyMice(Vector3 strikePoint)
    {
        if (mice == null)
            return;
        for (int i = 0; i < mice.Length; i++)
        {
            CatCatchMouse mouse = mice[i];
            if (mouse == null || !mouse.IsActive)
                continue;
            Vector3 delta = mouse.transform.position - strikePoint;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 2.6f * 2.6f)
                mouse.Panic();
        }
    }

    /// <summary>
    /// A tap either picks the prey under the finger or sends the cat running to
    /// that patch of floor. The catch itself is decided later, on the landing.
    /// </summary>
    public void HandleTap(Vector2 screenPosition)
    {
        if (!hunting || paused || settling || player == null)
            return;
        if (!player.TryResolveScreenPoint(screenPosition, out Vector3 world))
            return;

        CatCatchMouse prey = FindStrikeTarget(world, CatchHuntRules.TapTargetRadius);
        if (prey != null)
        {
            player.ChasePrey(prey);
            if (tutorialActive && tutorialStage == 0)
                AdvanceTutorial();
            return;
        }

        player.MoveTo(world);
    }

    private CatCatchMouse FindStrikeTarget(Vector3 strikePoint, float radius)
    {
        if (mice == null)
            return null;
        CatCatchMouse best = null;
        float bestDistance = radius * radius;
        for (int i = 0; i < mice.Length; i++)
        {
            CatCatchMouse mouse = mice[i];
            if (mouse == null || !mouse.IsCatchable)
                continue;
            Vector3 delta = mouse.transform.position - strikePoint;
            delta.y = 0f;
            float distance = delta.sqrMagnitude;
            if (distance > bestDistance)
                continue;
            bestDistance = distance;
            best = mouse;
        }
        return best;
    }

    // Spread anchors across the whole arena — left/right, near/far — so the mice
    // never start bunched in one corner. Each anchor is jittered per hunt.
    private static readonly Vector3[] SpawnZones =
    {
        new Vector3(-2.1f, 0.12f, 1.35f),
        new Vector3(2.1f, 0.12f, 1.15f),
        new Vector3(-1.8f, 0.12f, -0.3f),
        new Vector3(1.9f, 0.12f, -0.1f),
        new Vector3(0f, 0.12f, 1.55f),
        new Vector3(-2.3f, 0.12f, 0.5f),
        new Vector3(2.3f, 0.12f, 0.4f)
    };

    private void LaunchMice()
    {
        if (mice == null)
            return;
        // A gentler clearance than the respawn value so the depth-varied zones
        // (some sit nearer the cat's row) survive instead of all being kicked to
        // the far half, which is what bunched the mice together at the start.
        const float initialClearance = 1.15f;
        Transform arena = ArenaRoot;
        Vector3 catPos = player != null ? player.Position : (arena != null ? arena.position : Vector3.zero);
        for (int i = 0; i < mice.Length; i++)
        {
            if (mice[i] == null)
                continue;
            Vector3 zone = SpawnZones[i % SpawnZones.Length];
            Vector3 local = zone + new Vector3(
                UnityEngine.Random.Range(-0.35f, 0.35f), 0f,
                UnityEngine.Random.Range(-0.3f, 0.3f));
            Vector3 world = arena != null ? arena.TransformPoint(local) : local;
            Vector3 delta = world - catPos;
            delta.y = 0f;
            // If a jittered zone lands on the cat, fall back to a scattered point.
            if (delta.sqrMagnitude < initialClearance * initialClearance)
                world = RandomArenaPoint(initialClearance);
            mice[i].Launch(world, MouseSpawnGrace);
        }
    }

    private void KeepMiceOnTheFloor()
    {
        int active = 0;
        if (mice == null)
            return;
        for (int i = 0; i < mice.Length; i++)
        {
            if (mice[i] != null && mice[i].IsActive)
                active++;
        }
        while (active < Mathf.Min(4, mice.Length))
        {
            LaunchOneMouse();
            active++;
        }
    }

    private void LaunchOneMouse(int index = -1)
    {
        if (mice == null || mice.Length == 0)
            return;
        CatCatchMouse mouse = index >= 0 && index < mice.Length ? mice[index] : FindIdleMouse();
        if (mouse == null)
            return;
        mouse.Launch(RandomArenaPoint(MouseSpawnClearance), MouseSpawnGrace);
    }

    private Vector3 RandomArenaPoint(float minDistanceFromCat)
    {
        const float minSeparation = 1.15f;
        Transform arena = ArenaRoot;
        Vector3 catPos = player != null ? player.Position : arena.position;
        Vector3 best = arena != null
            ? arena.TransformPoint(new Vector3(2.15f, 0.12f, 1.35f))
            : new Vector3(2.15f, 0.12f, 1.35f);
        float bestScore = -1f;
        for (int attempt = 0; attempt < 24; attempt++)
        {
            Vector3 local = new Vector3(
                UnityEngine.Random.Range(-2.4f, 2.4f),
                0.12f,
                UnityEngine.Random.Range(-1.65f, 1.65f));
            Vector3 world = arena != null ? arena.TransformPoint(local) : local;
            Vector3 delta = world - catPos;
            delta.y = 0f;
            float catDistance = delta.magnitude;
            float mouseDistance = NearestActiveMouseDistance(world);
            // Perfect spot: clear of the cat and not stacked on another mouse.
            if (catDistance >= minDistanceFromCat && mouseDistance >= minSeparation)
                return world;
            // Otherwise remember the candidate that best balances both spacings.
            float score = Mathf.Min(catDistance / Mathf.Max(0.01f, minDistanceFromCat), 1f) +
                          Mathf.Min(mouseDistance / minSeparation, 1f);
            if (score > bestScore)
            {
                bestScore = score;
                best = world;
            }
        }

        return best;
    }

    private float NearestActiveMouseDistance(Vector3 world)
    {
        if (mice == null)
            return float.PositiveInfinity;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < mice.Length; i++)
        {
            if (mice[i] == null || !mice[i].IsActive)
                continue;
            Vector3 delta = mice[i].transform.position - world;
            delta.y = 0f;
            nearest = Mathf.Min(nearest, delta.magnitude);
        }
        return nearest;
    }

    private CatCatchMouse FindIdleMouse()
    {
        for (int i = 0; i < mice.Length; i++)
        {
            if (mice[i] != null && !mice[i].IsActive)
                return mice[i];
        }
        return null;
    }

    private void HideMice()
    {
        if (mice == null)
            return;
        for (int i = 0; i < mice.Length; i++)
            mice[i]?.Hide();
    }

    private void ShowWelcome()
    {
        hunting = false;
        paused = false;
        Time.timeScale = 1f;
        HideTutorial(false);
        SetPanel(pausePanel, false);
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(false);
        SetPanel(resultPanel, false);
        SetPanel(hudRoot, false);
        SetPanel(welcomePanel, true);
        if (player != null)
            player.SetInputEnabled(false);
        HideMice();
        RefreshWelcome();
    }

    private void RefreshWelcome()
    {
        CatchLivesService.Refresh();
        // The caption lives in the same label as the value, matching the Cat
        // Runner best-score pill; a separate caption row overlapped the digits.
        if (welcomeBestText != null)
            welcomeBestText.text = GameLanguageService.Format("games.best",bestScore.ToString("N0"));
        bool canStart = CatchLivesService.CanStartHunt();
        if (welcomeStartButton != null)
            welcomeStartButton.interactable = canStart;
        if (welcomeRewardedButton != null)
        {
            bool canReward = CatchLivesService.CanClaimRewardedAd();
            welcomeRewardedButton.gameObject.SetActive(!canStart && canReward);
            if (welcomeStartButton != null)
                welcomeStartButton.gameObject.SetActive(canStart || !canReward);
            welcomeRewardedButton.interactable = canReward;
        }
        if (welcomeLivesText == null)
            return;
        if (CatchLivesService.IsUnlimited)
            welcomeLivesText.text = GameLanguageService.Text("games.unlimited");
        else
        {
            welcomeLivesText.text =
                GameLanguageService.Format("games.lives_ready",CatchLivesService.CurrentLives,CatchLivesService.MaximumLives);
            if (!canStart)
            {
                TimeSpan remainingLife = CatchLivesService.TimeUntilNextLife();
                int seconds = Mathf.Max(0, Mathf.CeilToInt((float)remainingLife.TotalSeconds));
                welcomeLivesText.text = GameLanguageService.Format("games.next_life",$"{seconds / 60:00}:{seconds % 60:00}");
            }
        }
    }

    private void RefreshHud()
    {
        if (scoreLabel != null)
            scoreLabel.text = score.ToString();
        if (timerLabel != null)
        {
            int seconds = Mathf.CeilToInt(remaining);
            timerLabel.text = seconds.ToString();
            // The last ten seconds read as urgent without an extra widget.
            timerLabel.color = seconds <= 10 ? LowTimeColor : PremiumUiStyle.Ink;
        }
        if (catchLabel != null)
            catchLabel.text = $"{catches}  •  {CatchScoring.CoinsForCatches(catches)}";
        if (comboLabel == null)
            return;
        bool comboLive = combo > 1 && Time.time - lastCatchTime <= CatchScoring.ComboWindowSeconds;
        comboLabel.text = comboLive ? GameContentCopy.Text("SERİ","COMBO")+$"  x{Mathf.Min(combo, CatchScoring.MaximumComboSteps + 1)}" : string.Empty;
    }

    private void BeginTutorial()
    {
        tutorialActive = true;
        tutorialStage = 0;
        tutorialHold = 0f;
        tutorialElapsed = 0f;
        SetPanel(tutorialPanel, true);
        RefreshTutorialText();
    }

    public void SkipTutorial()
    {
        if (!tutorialActive)
            return;
        CompleteTutorial();
    }

    private void TickTutorial()
    {
        tutorialElapsed += Time.deltaTime;
        if (tutorialElapsed >= MaximumTutorialSeconds)
        {
            CompleteTutorial();
            return;
        }

        if (player != null && player.ConsumePounceStarted() && tutorialStage == 0)
            AdvanceTutorial();

        if (tutorialStage != 2)
            return;

        tutorialHold += Time.deltaTime;
        if (tutorialHold >= 2.2f)
            CompleteTutorial();
    }

    private void AdvanceTutorial()
    {
        tutorialStage++;
        tutorialHold = 0f;
        RefreshTutorialText();
    }

    private void RefreshTutorialText()
    {
        if (tutorialText == null)
            return;
        switch (tutorialStage)
        {
            case 0:
                tutorialText.text = GameContentCopy.Text("Bir fareye dokun\nKedin peşinden koşsun", "Tap a squeaky mouse\nYour cat chases it");
                break;
            case 1:
                tutorialText.text = GameContentCopy.Text("Yaklaşınca kedin atlar\nİnişte fareyi yakalar", "Get close and your cat pounces\nLand to catch the mouse");
                break;
            default:
                tutorialText.text = GameContentCopy.Text("Altmış saniyen var\nKaç fare yakalayabilirsin?", "You have sixty seconds\nHow many can you catch?");
                break;
        }
    }

    private void CompleteTutorial()
    {
        HideTutorial(true);
    }

    private void HideTutorial(bool persist)
    {
        tutorialActive = false;
        tutorialStage = 0;
        tutorialHold = 0f;
        tutorialElapsed = 0f;
        SetPanel(tutorialPanel, false);
        if (!persist)
            return;
        if (CatchLivesService.CompleteTutorial())
            CatHomeSaveSystem.SaveNow();
    }

    private IEnumerator UnloadRoutine()
    {
        Time.timeScale = 1f;
        RestoreHomePresentation();
        Scene catchScene = SceneManager.GetSceneByName(CatCatchLauncher.CatchSceneName);
        if (catchScene.IsValid() && catchScene.isLoaded)
        {
            CatRunnerSessionContext.RestoreRequestedNavigation();
            AsyncOperation operation = SceneManager.UnloadSceneAsync(catchScene);
            while (operation != null && !operation.isDone)
                yield return null;
        }
    }

    private void ParkHomePresentation()
    {
        hiddenCanvases.Clear();
        hiddenCameras.Clear();
        hiddenListeners.Clear();
        presentationCaptured = true;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas == huntCanvas || !canvas.enabled)
                continue;
            hiddenCanvases.Add(canvas);
            canvas.enabled = false;
        }

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == null || cameras[i] == huntCamera || !cameras[i].enabled)
                continue;
            hiddenCameras.Add(cameras[i]);
            cameras[i].enabled = false;
        }

        AudioListener[] listeners = FindObjectsByType<AudioListener>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] == null || listeners[i] == huntListener || !listeners[i].enabled)
                continue;
            hiddenListeners.Add(listeners[i]);
            listeners[i].enabled = false;
        }

        if (huntCamera != null)
            huntCamera.enabled = true;
        if (huntListener != null)
            huntListener.enabled = true;
        if (huntCanvas != null)
            huntCanvas.enabled = true;
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

        hiddenCanvases.Clear();
        hiddenCameras.Clear();
        hiddenListeners.Clear();

        if (huntCamera != null)
            huntCamera.enabled = false;
        if (huntListener != null)
            huntListener.enabled = false;
    }

    public static void ApplyBestScore(int saved)
    {
        bestScore = Mathf.Max(0, saved);
    }

    public static int CaptureBestScore() => bestScore;

    private static void SetPanel(GameObject panel, bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void Unbind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }

#if UNITY_EDITOR
    public void EditorConfigureNavigation(Button welcomeGames, Button pauseGames, Button resultGames)
    {
        welcomeGamesButton = welcomeGames;
        pauseGamesButton = pauseGames;
        resultGamesButton = resultGames;
    }

    public void EditorConfigure(
        CatCatchPlayer catchPlayer,
        CatCatchMouse[] mouseSet,
        Transform spawn,
        Camera camera,
        AudioListener listener,
        Canvas canvas,
        GameObject welcome,
        TMP_Text welcomeBest,
        TMP_Text welcomeLives,
        Button start,
        Button exit,
        Button rewarded,
        GameObject hud,
        TMP_Text score,
        TMP_Text comboReadout,
        TMP_Text timer,
        TMP_Text caught,
        GameObject results,
        TMP_Text title,
        TMP_Text details,
        Button collect,
        Button retry,
        GameObject tutorialOverlay,
        TMP_Text tutorialMessage,
        Button tutorialSkip,
        Button pause,
        GameObject pauseOverlay,
        Button resume,
        Button pauseExit)
    {
        player = catchPlayer;
        mice = mouseSet ?? Array.Empty<CatCatchMouse>();
        spawnRoot = spawn;
        huntCamera = camera;
        huntListener = listener;
        huntCanvas = canvas;
        welcomePanel = welcome;
        welcomeBestText = welcomeBest;
        welcomeLivesText = welcomeLives;
        welcomeStartButton = start;
        welcomeExitButton = exit;
        welcomeRewardedButton = rewarded;
        hudRoot = hud;
        scoreLabel = score;
        comboLabel = comboReadout;
        timerLabel = timer;
        catchLabel = caught;
        resultPanel = results;
        resultTitle = title;
        resultDetails = details;
        collectButton = collect;
        retryButton = retry;
        tutorialPanel = tutorialOverlay;
        tutorialText = tutorialMessage;
        tutorialSkipButton = tutorialSkip;
        pauseButton = pause;
        pausePanel = pauseOverlay;
        resumeButton = resume;
        pauseExitButton = pauseExit;
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        bestScore = 0;
    }
}
