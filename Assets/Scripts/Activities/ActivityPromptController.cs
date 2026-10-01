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
    private CatActivity selected;
    private ActivitySelectionGraphic selectionGraphic;
    private readonly System.Collections.Generic.Dictionary<CatActivity, (bool ready, float distance)> promptChecks =
        new System.Collections.Generic.Dictionary<CatActivity, (bool, float)>();

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
        ReadWorldSelection();
        RefreshImmediate();
        UpdateSelectionGraphic();

#if ENABLE_INPUT_SYSTEM
        if ((candidate != null || CatActivity.Active!=null && CatActivity.Active.IsWaitingForRestStop) && Keyboard.current != null &&
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
        promptChecks.Clear();
        if(HomeUiFlow.IsHomeControlBlocked || TitleScreen.IsShowing || GamesHubPanel.IsAnyOpen || LeaderboardPanel.IsAnyOpen ||
           ShopPanelController.IsAnyOpen || QuestPanelController.IsAnyOpen || CatBreedShopPanel.IsAnyOpen ||
           RoomSelectorPanel.IsAnyOpen || SettingsPanel.IsAnyOpen || PrivacyDataPanel.IsAnyOpen ||
           WhileYouWereAwayPopup.IsAnyOpen || HomeLevelUpCelebrationView.IsAnyOpen || CollectionCompleteCelebrationView.IsAnyOpen)
        {candidate=null;HideAction();HideProgress();return;}

        CatActivity active = CatActivity.Active;
        if (active != null)
        {
            candidate = null;
            if(active.IsWaitingForRestStop)
            {
                HideProgress();
                SetActionText(GameContentCopy.Text("Kalk","Get up"));
                SetGroup(actionGroup,true);
                if(actionButton!=null){actionButton.gameObject.SetActive(true);actionButton.interactable=true;}
            }
            else {HideAction();ShowProgress(active.ProgressLabel);}
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

        string title=HomeStoreService.TryGetProduct(candidate.StoreProductId,out var product)?product.Title:GameInteractionCopy.Text(candidate.DisplayName);
        string action=BuildActionText(candidate, energySystem, cat);
        bool needsEnergy=!candidate.IsCareSatisfiedFor(cat) && candidate.EnergyCost>0 && (energySystem==null || !energySystem.CanSpendEnergy(candidate.EnergyCost));
        SetActionText(GameInteractionCopy.ProductAction(candidate.StoreProductId, title, action, needsEnergy));
        SetGroup(actionGroup, true);
        if (actionButton != null)
        {
            actionButton.gameObject.SetActive(true);
            actionButton.interactable = true;
        }
    }

    private CatActivity FindNearestCandidate()
    {
        if (cat == null || CatActionState.IsBusy(cat) || cat.AreWorldActionsBlocked)
            return null;

        if(IsNearby(selected))
            return selected;
        selected=null;

        CatActivity nearest = null;
        float nearestDistance = float.PositiveInfinity;
        var activities = CatActivity.Registered;
        for (int i = 0; i < activities.Count; i++)
        {
            CatActivity activity = activities[i];
            if (!IsEligible(activity) || !QueryPrompt(activity, out float distance))
                continue;
            if (distance <= activity.InteractionRadius && distance < nearestDistance)
            {
                nearest = activity;
                nearestDistance = distance;
            }
        }

        // A small hysteresis keeps adjacent media actions from flickering as the cat idles.
        if(IsEligible(candidate) && QueryPrompt(candidate,out float previousDistance) && previousDistance<=nearestDistance+.06f)return candidate;
        return nearest;
    }

    private bool IsEligible(CatActivity activity) => cat!=null && activity!=null &&
        activity.isActiveAndEnabled && activity.IsUnlocked && activity.gameObject.scene==cat.gameObject.scene &&
        !activity.IsRetired;
    private bool IsNearby(CatActivity activity) => IsEligible(activity) && QueryPrompt(activity,out _);
    private bool QueryPrompt(CatActivity activity,out float distance)
    {
        if(!promptChecks.TryGetValue(activity,out var check))
        {
            bool ready=activity.CanStartFromPrompt(cat,out float measured);
            check=(ready,measured);promptChecks.Add(activity,check);
        }
        distance=check.distance;return check.ready;
    }

    private void ReadWorldSelection()
    {
        if(cat==null || CatActionState.IsBusy(cat) || cat.AreWorldActionsBlocked || HomeUiFlow.IsHomeControlBlocked)return;
        Vector2 point;
#if ENABLE_INPUT_SYSTEM
        if(Touchscreen.current!=null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)point=Touchscreen.current.primaryTouch.position.ReadValue();
        else if(Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame)point=Mouse.current.position.ReadValue();
        else return;
#else
        if(!Input.GetMouseButtonDown(0))return;point=Input.mousePosition;
#endif
        var events=UnityEngine.EventSystems.EventSystem.current;
        if(events!=null)
        {
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events){position=point},hits);
            if(hits.Exists(h=>h.module is UnityEngine.UI.GraphicRaycaster))return;
        }
        selected=PickWorldActivity(Camera.main,point);
    }

    public static CatActivity PickWorldActivity(Camera camera,Vector2 point)
    {
        if(camera==null || !camera.pixelRect.Contains(point))return null;
        var ray=camera.ScreenPointToRay(point);
        // Walk-through toys use trigger geometry; invisible room boundaries only constrain movement.
        var worldHits=Physics.RaycastAll(ray,30f,~0,QueryTriggerInteraction.Collide);
        System.Array.Sort(worldHits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in worldHits)
        {
            var activity=hit.collider.GetComponentInParent<CatActivity>();
            if(activity==null)foreach(var option in CatActivity.Registered)
                if(option is LivingFurnitureActivity furniture && furniture.SelectionVisual!=null && hit.transform.IsChildOf(furniture.SelectionVisual))
                {activity=option;break;}
            if(activity!=null && activity.IsUnlocked && activity.isActiveAndEnabled)return activity;
            // The first opaque object hides objects behind it.
            if(!hit.collider.isTrigger)
                foreach(var surface in hit.collider.GetComponentsInChildren<Renderer>())
                    if(surface.enabled && surface.gameObject.activeInHierarchy && surface.bounds.IntersectRay(ray))return null;
        }
        return null;
    }

    private void HandleAction()
    {
        if (!isActiveAndEnabled || cat == null || cat.AreWorldActionsBlocked || HomeUiFlow.IsHomeControlBlocked)
            return;
        if(CatActivity.Active!=null && CatActivity.Active.BelongsTo(cat))
        {CatActivity.Active.RequestRestStop();return;}
        // A prop/need may change between presentation and pointer delivery.
        // Withdraw that stale action silently; never turn a button into advice.
        bool canStart = IsEligible(candidate);
        if (canStart && !CatActionState.IsBusy(cat) && !cat.AreWorldActionsBlocked && !HomeUiFlow.IsHomeControlBlocked)
        { if (!candidate.TryStartFromPrompt(cat)) RefreshImmediate(); }
        else RefreshImmediate();
    }

    void UpdateSelectionGraphic()
    {
        if(selectionGraphic==null && actionButton!=null)
        {
            var canvas=actionButton.GetComponentInParent<Canvas>();
            if(canvas==null)return;
            var host=new GameObject("Selected product",typeof(RectTransform),typeof(CanvasRenderer));
            host.transform.SetParent(canvas.transform,false);host.transform.SetAsFirstSibling();
            selectionGraphic=host.AddComponent<ActivitySelectionGraphic>();selectionGraphic.raycastTarget=false;
            selectionGraphic.color=new Color32(34,157,146,235);
        }
        if(selectionGraphic==null)return;
        bool visible=selected!=null && candidate==selected && actionButton!=null && actionButton.gameObject.activeInHierarchy;
        Transform visual=visible?(selected is LivingFurnitureActivity furniture?furniture.SelectionVisual:selected.transform):null;
        selectionGraphic.SetTarget(visual);
    }

    private void ShowProgress(string text)
    {
        if (progressGroup != null && !string.IsNullOrWhiteSpace(text))
            progressGroup.gameObject.SetActive(true);
        if (progressLabel != null)
            progressLabel.text = GameInteractionCopy.Status(text);
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

    public static string BuildActionText(CatActivity activity, EnergySystem energy, CatMovement actor = null)
    {
        if (activity == null)
            return string.Empty;

        if (!(actor != null ? activity.IsCareSatisfiedFor(actor) : activity.IsCareSatisfied) && activity.EnergyCost > 0f &&
            (energy == null || !energy.CanSpendEnergy(activity.EnergyCost)))
        {
            return GameContentCopy.Text($"{Mathf.CeilToInt(activity.EnergyCost)} enerji gerekli",$"Need {Mathf.CeilToInt(activity.EnergyCost)} energy");
        }

        return activity.Kind == CatActivityKind.FernWatch
            ? GameLanguageService.Text("interaction.fern.action") : GameInteractionCopy.Action(activity.ActionText);
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
        if(selectionGraphic!=null)Destroy(selectionGraphic.gameObject);
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
