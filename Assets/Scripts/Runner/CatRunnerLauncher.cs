using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CatRunnerLauncher : MonoBehaviour
{
    public const string RunnerScenePath = "Assets/Scenes/Runner/CatRunner.unity";
    public const string RunnerSceneName = "CatRunner";

    [SerializeField] private Button playButton;
    [SerializeField] private Button roomsButton;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text energyLabel;
    [SerializeField] private TMP_Text roomProgressLabel;
    [SerializeField] private Button rewardedAdButton;
    [SerializeField] private GamesHubPanel gamesHub;

    private LevelLoader levelLoader;
    private bool loading;

    private void Awake()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OpenGames);
            playButton.onClick.AddListener(OpenGames);
        }
        if (rewardedAdButton != null)
        {
            rewardedAdButton.onClick.RemoveListener(RequestRewardedEnergy);
            rewardedAdButton.onClick.AddListener(RequestRewardedEnergy);
        }
        if (roomsButton != null)
        {
            roomsButton.onClick.RemoveListener(OpenShop);
            roomsButton.onClick.AddListener(OpenShop);
        }
        RunnerEnergyService.EnsureInitialized();
        CatchLivesService.EnsureInitialized();
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();
        RunnerEnergyService.Refresh();
        bool baseReady = !loading && !HomeUiFlow.IsHomeControlBlocked &&
                         !GamesHubPanel.IsAnyOpen && !LeaderboardPanel.IsAnyOpen &&
                         !SettingsPanel.IsAnyOpen && !PrivacyDataPanel.IsAnyOpen &&
                         !CatBreedShopPanel.IsAnyOpen && !TitleScreen.IsShowing &&
                         PetTutorialHint.IsOnboardingCompleted &&
                         levelLoader != null && levelLoader.IsReady &&
                         !ShopPanelController.IsAnyOpen &&
                         !QuestPanelController.IsAnyOpen &&
                         !RoomSelectorPanel.IsAnyOpen &&
                         !OnboardingCelebrationView.IsAnyOpen;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = baseReady ? 1f : 0f;
            canvasGroup.interactable = baseReady;
            canvasGroup.blocksRaycasts = baseReady;
        }
        if (playButton != null)
            // The welcome screen is available even at zero energy. Only START
            // RUN spends energy, so players can inspect missions/settings or
            // request a verified rewarded-energy grant before leaving.
            playButton.interactable = baseReady && !GamesHubPanel.IsAnyOpen;
        if (rewardedAdButton != null)
        {
            rewardedAdButton.gameObject.SetActive(
                baseReady && CatActivity.Active == null && CanRequestRewardedEnergy());
            SetButtonText(rewardedAdButton,GameContentCopy.Text("İzle · +2 can","Watch · +2 lives"));
        }
        RefreshEnergyUi();
        RefreshRoomUi();
    }

    public void OpenShop()
    {
        ShopPanelController shop =
            FindAnyObjectByType<ShopPanelController>(FindObjectsInactive.Include);
        if (shop != null)
            shop.RequestOpen(HomeStoreCategory.Cat);
    }

    public void OpenGames()
    {
        if (gamesHub != null)
            gamesHub.Show();
        else
            Launch();
    }

    public void Launch()
    {
        if (!isActiveAndEnabled || loading ||
            !CatRunnerSessionContext.TryBeginLaunch(RunnerSceneName))
            return;
        loading = true;
        try
        {
            if (StartCoroutine(LaunchRoutine()) == null)
            {
                loading = false;
                CatRunnerSessionContext.CompleteLaunch(RunnerSceneName);
            }
        }
        catch
        {
            loading = false;
            CatRunnerSessionContext.CompleteLaunch(RunnerSceneName);
            throw;
        }
    }

    private IEnumerator LaunchRoutine()
    {
        try
        {
            CatActionState.CancelForTransition(
                FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include));
            // Viewing the welcome screen is free; START spends the life.
            CatRunnerSessionContext.CaptureFromHome();
            AsyncOperation operation = SceneManager.LoadSceneAsync(
                RunnerScenePath, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError("Cat Runner scene could not be queued for loading.", this);
                yield break;
            }
            while (!operation.isDone)
                yield return null;
        }
        finally
        {
            loading = false;
            CatRunnerSessionContext.CompleteLaunch(RunnerSceneName);
        }
    }

    public void RequestRewardedEnergy()
    {
        if (!CanRequestRewardedEnergy())
            return;

        if (CatRunnerRewardedAdBridge.TryShow(
                verified =>
                {
                    if (verified)
                        CompleteRewardedAd();
                }))
        {
            return;
        }

#if UNITY_EDITOR
        // Editor-only deterministic simulation. Release builds require a
        // registered provider and grant only from its verified completion.
        CompleteRewardedAd();
#else
        Debug.LogWarning(
            "Rewarded energy was requested, but no rewarded-ad provider is connected.",
            this);
#endif
    }

    private void CompleteRewardedAd()
    {
        if (!RunnerEnergyService.TryGrantRewardedAd())
            return;

        CatHomeSaveSystem.SaveNow();
        RefreshEnergyUi();
    }

    public bool CanRequestRewardedEnergy()
    {
        if (!RunnerEnergyService.CanClaimRewardedAd())
            return false;
#if UNITY_EDITOR
        return true;
#else
        return CatRunnerRewardedAdBridge.HasReadyProvider;
#endif
    }

    private void RefreshEnergyUi()
    {
        if (energyLabel == null)
            return;

        if (RunnerEnergyService.IsUnlimited)
        {
            energyLabel.text = GameLanguageService.Text("games.unlimited");
            SetButtonText(playButton, GameLanguageService.Text("title.games"));
            return;
        }

        int value = RunnerEnergyService.CurrentEnergy;
        if (value > 0)
        {
            energyLabel.text =
                $"Runner {value}/{RunnerEnergyService.MaximumEnergy}  ·  Catch {DescribeCatchLives()}";
            SetButtonText(playButton, GameLanguageService.Text("title.games"));
            return;
        }

        TimeSpan remaining = RunnerEnergyService.TimeUntilNextEnergy();
        energyLabel.text = $"Runner 0/{RunnerEnergyService.MaximumEnergy}  ·  " +
                           GameContentCopy.Text($"Yeni can {FormatCountdown(remaining)}",$"Next life {FormatCountdown(remaining)}");
        SetButtonText(playButton, GameLanguageService.Text("title.games"));
    }

    /// <summary>
    /// The dock summary has to agree with the Games hub card, which prints
    /// UNLIMITED while the Catch entitlement is active instead of a live count.
    /// </summary>
    private static string DescribeCatchLives()
    {
        return CatchLivesService.IsUnlimited
            ? "UNLIMITED"
            : $"{CatchLivesService.CurrentLives}/{CatchLivesService.MaximumLives}";
    }

    private void RefreshRoomUi()
    {
        if (roomProgressLabel == null)
            return;

        if (HomeRoomService.TryGetRoom(HomeRoomService.CurrentRoomId, out HomeRoomDefinition room))
            roomProgressLabel.text = room.DisplayName + "  ·  " +
                HomeStoreService.GetRoomOwnedCount(room.Id) + "/" + HomeStoreService.GetRoomCollection(room.Id).Count;
    }

    private static string FormatCountdown(TimeSpan value)
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt((float)value.TotalSeconds));
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

    private static void SetButtonText(Button button, string value)
    {
        if (button == null)
            return;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = value;
    }

    private void ResolveReferences()
    {
        if (levelLoader == null)
            levelLoader = FindAnyObjectByType<LevelLoader>(FindObjectsInactive.Include);
    }

    private void OnDestroy()
    {
        if (loading)
            CatRunnerSessionContext.CompleteLaunch(RunnerSceneName);
        if (playButton != null)
            playButton.onClick.RemoveListener(OpenGames);
        if (rewardedAdButton != null)
            rewardedAdButton.onClick.RemoveListener(RequestRewardedEnergy);
        if (roomsButton != null)
            roomsButton.onClick.RemoveListener(OpenShop);
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        Button button,
        Button roomButton,
        CanvasGroup group,
        TMP_Text energy,
        TMP_Text roomProgress,
        Button rewardedAd,
        GamesHubPanel hub = null)
    {
        playButton = button;
        roomsButton = roomButton;
        canvasGroup = group;
        energyLabel = energy;
        roomProgressLabel = roomProgress;
        rewardedAdButton = rewardedAd;
        gamesHub = hub;
    }
#endif
}
