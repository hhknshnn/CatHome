using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Home Games overlay. The dock GAMES button opens this picker so Cat Runner
/// and Cat Catch stay selectable without sharing a life pool.
/// </summary>
[DisallowMultipleComponent]
public sealed class GamesHubPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button runnerButton;
    [SerializeField] private Button catchButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private TMP_Text runnerLivesText;
    [SerializeField] private TMP_Text catchLivesText;
    [SerializeField] private CatRunnerLauncher runnerLauncher;
    [SerializeField] private CatCatchLauncher catchLauncher;
    [SerializeField] private LeaderboardPanel leaderboardPanel;

    private static GamesHubPanel activeInstance;
    private bool open;
    private CatMovement movement;

    public static bool IsAnyOpen =>
        activeInstance != null && activeInstance.open;

    private void Awake()
    {
        activeInstance = this;
        StorybookGamesPresentation.ApplyGames(transform);
        Bind(closeButton, Hide);
        Bind(GetComponent<Button>(), Hide);
        Bind(runnerButton, PlayRunner);
        Bind(catchButton, PlayCatch);
        Bind(leaderboardButton, ShowLeaderboards);
        HideImmediate();
    }

    private void OnDestroy()
    {
        if(movement!=null)movement.ReleaseInputBlock(this);
        if (activeInstance == this)
            activeInstance = null;
        Unbind(closeButton, Hide);
        Unbind(GetComponent<Button>(), Hide);
        Unbind(runnerButton, PlayRunner);
        Unbind(catchButton, PlayCatch);
        Unbind(leaderboardButton, ShowLeaderboards);
    }

    private void OnDisable() => HideImmediate();

    private void Update()
    {
        // Fallback when a return had to reload GameScene instead of revealing
        // the already loaded home. Normal additive returns restore immediately.
        if (!HomeUiFlow.IsMiniGameVisible)
            CatRunnerSessionContext.RestoreRequestedNavigation();
        if (!open)
            return;
        RefreshLives();
    }

    public void Show()
    {
        if (HomeUiFlow.IsMiniGameVisible)
            return;
        ShowCore();
    }

    internal void ShowFromMiniGameReturn() => ShowCore();

    private void ShowCore()
    {
        open = true;
        movement=FindAnyObjectByType<CatMovement>();if(movement!=null)movement.AcquireInputBlock(this);
        if (rootGroup != null)
        {
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
        }
        gameObject.SetActive(true);
        RefreshLives();
    }

    public void Hide()
    {
        HideImmediate();
    }

    private void HideImmediate()
    {
        open = false;
        if(movement!=null)movement.ReleaseInputBlock(this);
        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }
    }

    private void PlayRunner()
    {
        if (!open || HomeUiFlow.IsMiniGameVisible)
            return;
        HideImmediate();
        if (runnerLauncher != null)
            runnerLauncher.Launch();
    }

    private void PlayCatch()
    {
        if (!open || HomeUiFlow.IsMiniGameVisible)
            return;
        HideImmediate();
        if (catchLauncher != null)
            catchLauncher.Launch();
    }

    private void ShowLeaderboards()
    {
        if (!open || HomeUiFlow.IsMiniGameVisible)
            return;
        HideImmediate();
        if (leaderboardPanel != null)
            leaderboardPanel.Show();
    }

    private void RefreshLives()
    {
        RunnerEnergyService.Refresh();
        CatchLivesService.Refresh();
        if (runnerLivesText != null)
        {
            runnerLivesText.text = RunnerEnergyService.IsUnlimited
                ? GameLanguageService.Text("games.unlimited")
                : GameLanguageService.Format("games.life_count",RunnerEnergyService.CurrentEnergy,RunnerEnergyService.MaximumEnergy);
        }
        if (catchLivesText != null)
        {
            catchLivesText.text = CatchLivesService.IsUnlimited
                ? GameLanguageService.Text("games.unlimited")
                : GameLanguageService.Format("games.life_count",CatchLivesService.CurrentLives,CatchLivesService.MaximumLives);
        }
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
    public void EditorConfigure(
        CanvasGroup group,
        Button close,
        Button runner,
        Button catchPlay,
        TMP_Text runnerLives,
        TMP_Text catchLives,
        CatRunnerLauncher launcher,
        CatCatchLauncher catchLaunch,
        Button rankings,
        LeaderboardPanel rankingsPanel)
    {
        rootGroup = group;
        closeButton = close;
        runnerButton = runner;
        catchButton = catchPlay;
        runnerLivesText = runnerLives;
        catchLivesText = catchLives;
        runnerLauncher = launcher;
        catchLauncher = catchLaunch;
        leaderboardButton = rankings;
        leaderboardPanel = rankingsPanel;
        HideImmediate();
    }
#endif
}
