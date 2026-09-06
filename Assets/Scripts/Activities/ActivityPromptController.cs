using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class ActivityPromptController : MonoBehaviour
{
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionLabel;
    [SerializeField] private TMP_Text actionShadowLabel;
    [SerializeField] private CanvasGroup actionGroup;
    [SerializeField] private CanvasGroup progressGroup;
    [SerializeField] private TMP_Text progressLabel;

    private static ActivityPromptController instance;
    private CatMovement cat;
    private BowlInteraction bowlInteraction;
    private EnergySystem energySystem;
    private CatActivity candidate;

    public static void NotifyActivityChanged()
    {
        if (instance != null)
            instance.RefreshImmediate();
    }

    private void Awake()
    {
        instance = this;
        ResolveReferences();
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleAction);
            actionButton.onClick.AddListener(HandleAction);
        }
        HideAction();
        HideProgress();
    }

    private void Update()
    {
        ResolveReferences();
        RefreshImmediate();

#if ENABLE_INPUT_SYSTEM
        if (candidate != null && Keyboard.current != null &&
            (Keyboard.current.eKey.wasPressedThisFrame ||
             Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            HandleAction();
        }
#endif
    }

    public void ResolveSceneReferences()
    {
        cat = null;
        bowlInteraction = null;
        energySystem = null;
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (cat == null)
            cat = FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        if (bowlInteraction == null)
            bowlInteraction = FindAnyObjectByType<BowlInteraction>(FindObjectsInactive.Include);
        if (energySystem == null)
            energySystem = FindAnyObjectByType<EnergySystem>(FindObjectsInactive.Include);
    }

    private void RefreshImmediate()
    {
        if(HomeUiFlow.IsHomeControlBlocked || TitleScreen.IsShowing || GamesHubPanel.IsAnyOpen || LeaderboardPanel.IsAnyOpen ||
           ShopPanelController.IsAnyOpen || QuestPanelController.IsAnyOpen || CatBreedShopPanel.IsAnyOpen ||
           RoomSelectorPanel.IsAnyOpen || SettingsPanel.IsAnyOpen || PrivacyDataPanel.IsAnyOpen ||
           WhileYouWereAwayPopup.IsAnyOpen || HomeLevelUpCelebrationView.IsAnyOpen || CollectionCompleteCelebrationView.IsAnyOpen)
        {candidate=null;HideAction();HideProgress();return;}

        CatActivity active = CatActivity.Active;
        if (active != null)
        {
            candidate = null;
            HideAction();
            ShowProgress(active.ProgressLabel);
            return;
        }

        HideProgress();
        candidate = FindNearestCandidate();
        bool bowlOwnsContext = bowlInteraction != null && bowlInteraction.HasVisibleAction;
        bool tutorialBlocks = !PetTutorialHint.IsOnboardingCompleted;
        if (candidate == null || bowlOwnsContext || tutorialBlocks ||
            OnboardingCelebrationView.IsAnyOpen || QuestPanelController.IsAnyOpen)
        {
            HideAction();
            return;
        }

        SetActionText(BuildActionText(candidate, energySystem));
        SetGroup(actionGroup, true);
        if (actionButton != null)
        {
            actionButton.gameObject.SetActive(true);
            actionButton.interactable = true;
        }
    }

    private CatActivity FindNearestCandidate()
    {
        if (cat == null || cat.AreWorldActionsBlocked)
            return null;

        CatActivity nearest = null;
        float nearestDistance = float.PositiveInfinity;
        var activities = CatActivity.Registered;
        for (int i = 0; i < activities.Count; i++)
        {
            CatActivity activity = activities[i];
            if (activity == null || !activity.isActiveAndEnabled || !activity.IsUnlocked)
                continue;

            float distance = activity.DistanceTo(cat);
            if (distance <= activity.InteractionRadius && distance < nearestDistance)
            {
                nearest = activity;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private void HandleAction()
    {
        if (candidate != null)
            candidate.TryStart(cat);
    }

    private void ShowProgress(string text)
    {
        if (progressGroup != null && !string.IsNullOrWhiteSpace(text))
            progressGroup.gameObject.SetActive(true);
        if (progressLabel != null)
            progressLabel.text = GameInteractionCopy.Text(text);
        SetGroup(progressGroup, !string.IsNullOrWhiteSpace(text));
    }

    private void HideProgress()
    {
        SetGroup(progressGroup, false);
    }

    private void HideAction()
    {
        if (actionButton != null)
            actionButton.gameObject.SetActive(false);
        SetGroup(actionGroup, false);
    }

    private void SetActionText(string value)
    {
        if (actionLabel != null)
            actionLabel.text = value;
        if (actionShadowLabel != null)
            actionShadowLabel.text = value;
    }

    public static string BuildActionText(CatActivity activity, EnergySystem energy)
    {
        if (activity == null)
            return string.Empty;

        if (activity.EnergyCost > 0f &&
            (energy == null || !energy.CanSpendEnergy(activity.EnergyCost)))
        {
            return GameContentCopy.Text($"{Mathf.CeilToInt(activity.EnergyCost)} enerji gerekli",$"Need {Mathf.CeilToInt(activity.EnergyCost)} energy");
        }

        return GameInteractionCopy.Text(activity.ActionText);
    }

    private static void SetGroup(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        Button button,
        TMP_Text label,
        TMP_Text shadow,
        CanvasGroup buttonGroup,
        CanvasGroup statusGroup,
        TMP_Text statusLabel)
    {
        actionButton = button;
        actionLabel = label;
        actionShadowLabel = shadow;
        actionGroup = buttonGroup;
        progressGroup = statusGroup;
        progressLabel = statusLabel;
    }
#endif
}
