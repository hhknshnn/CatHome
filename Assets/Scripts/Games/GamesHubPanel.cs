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
    [SerializeField] private TMP_Text runnerLivesText;
    [SerializeField] private TMP_Text catchLivesText;
    [SerializeField] private CatRunnerLauncher runnerLauncher;
    [SerializeField] private CatCatchLauncher catchLauncher;

    private static GamesHubPanel activeInstance;
    private bool open;

    public static bool IsAnyOpen =>
        activeInstance != null && activeInstance.open;

    private void Awake()
    {
        activeInstance = this;
        Bind(closeButton, Hide);
        Bind(GetComponent<Button>(), Hide);
        Bind(runnerButton, PlayRunner);
        Bind(catchButton, PlayCatch);
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
            activeInstance = null;
        Unbind(closeButton, Hide);
        Unbind(GetComponent<Button>(), Hide);
        Unbind(runnerButton, PlayRunner);
        Unbind(catchButton, PlayCatch);
    }

    private void Update()
    {
        if (!open)
            return;
        RefreshLives();
    }

    public void Show()
    {
        open = true;
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
        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }
    }

    private void PlayRunner()
    {
        HideImmediate();
        if (runnerLauncher != null)
            runnerLauncher.Launch();
    }

    private void PlayCatch()
    {
        HideImmediate();
        if (catchLauncher != null)
            catchLauncher.Launch();
    }

    private void RefreshLives()
    {
        RunnerEnergyService.Refresh();
        CatchLivesService.Refresh();
        if (runnerLivesText != null)
        {
            runnerLivesText.text = RunnerEnergyService.IsUnlimited
                ? "LIVES  UNLIMITED"
                : $"LIVES  {RunnerEnergyService.CurrentEnergy}/{RunnerEnergyService.MaximumEnergy}";
        }
        if (catchLivesText != null)
        {
            catchLivesText.text = CatchLivesService.IsUnlimited
                ? "LIVES  UNLIMITED"
                : $"LIVES  {CatchLivesService.CurrentLives}/{CatchLivesService.MaximumLives}";
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
        CatCatchLauncher catchLaunch)
    {
        rootGroup = group;
        closeButton = close;
        runnerButton = runner;
        catchButton = catchPlay;
        runnerLivesText = runnerLives;
        catchLivesText = catchLives;
        runnerLauncher = launcher;
        catchLauncher = catchLaunch;
        HideImmediate();
    }
#endif
}
