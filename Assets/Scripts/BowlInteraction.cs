using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BowlInteraction : MonoBehaviour
{
    [Serializable]
    public class BowlSetup
    {
        [SerializeField] private Transform bowl;
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private Transform feedingPoint;
        [SerializeField] private Transform contactPoint;
        [SerializeField] private GameObject content;
        [SerializeField] private bool startsFull = true;
        [SerializeField] public string buttonText;
        [SerializeField] public string animationState;
        [SerializeField] private UnityEvent onInteractionCompleted = new UnityEvent();

        [NonSerialized] private bool isFull;

        public Transform Bowl => bowl;
        public Transform InteractionPoint => interactionPoint;
        public Transform FeedingPoint => feedingPoint;
        public Transform ContactPoint => contactPoint;
        public GameObject Content => content;
        public string ButtonText => buttonText;
        public string AnimationState => animationState;
        public UnityEvent OnInteractionCompleted => onInteractionCompleted;
        public bool IsFull => isFull;

        public void Initialize()
        {
            isFull = startsFull;
            SetContentActive(isFull);
        }

        public void Fill()
        {
            isFull = true;
            SetContentActive(true);
        }

        public void Empty()
        {
            isFull = false;
            SetContentActive(false);
        }

        private void SetContentActive(bool active)
        {
            if (content != null)
                content.SetActive(active);
        }
    }

    [Header("Cat")]
    [SerializeField] private Transform catTransform;
    [SerializeField] private CatMovement catMovement;
    [SerializeField] private HungerSystem hungerSystem;
    [SerializeField] private ThirstSystem thirstSystem;
    [SerializeField] private SleepInteraction sleepInteraction;
    [SerializeField] private CatSpeechBubble speechBubble;
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float modelForwardOffset;

    [Header("UI")]
    [SerializeField] private Button interactionButton;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private TMP_Text buttonShadowText;

    [Header("Button Appearance")]
    [SerializeField] private Sprite eatButtonSprite;
    [SerializeField] private Sprite drinkButtonSprite;
    [Tooltip("Optional. Eat sprite is used when this is not assigned.")]
    [SerializeField] private Sprite sleepButtonSprite;
    [SerializeField] private Color eatButtonTextColor = new Color32(255, 241, 184, 255);
    [SerializeField] private Color eatButtonShadowColor = new Color32(122, 50, 27, 255);
    [SerializeField] private Color drinkButtonTextColor = new Color32(244, 252, 255, 255);
    [SerializeField] private Color drinkButtonShadowColor = new Color32(22, 75, 106, 255);
    [SerializeField] private Color sleepButtonTextColor = new Color32(255, 244, 214, 255);
    [SerializeField] private Color sleepButtonShadowColor = new Color32(76, 55, 108, 255);

    [Header("Bowls")]
    [SerializeField] private BowlSetup food = new BowlSetup
    {
        buttonText = "EAT",
        animationState = "Eat"
    };
    [SerializeField] private BowlSetup water = new BowlSetup
    {
        buttonText = "DRINK",
        animationState = "Drink"
    };

    [Header("Interaction")]
    [SerializeField, Min(0f)] private float interactionDuration = 10f;
    [SerializeField, Min(0f)] private float animationTransitionTime = 0.15f;

    [Header("Animator")]
    [SerializeField] private string speedAnimatorParameter = "Speed";
    [SerializeField] private string idleState = "Idle";

    private const string BaseLayerPrefix = "Base Layer.";

    private BowlSetup currentBowl;
    private Coroutine interactionCoroutine;
    private bool isInteracting;
    private BowlSetup activeBowl;
    private bool needRecoveryCompleted;
    private int speedParameterHash;
    private BowlSetup styledBowl;
    private bool showingSleepStyle;
    private float nextFoodReadiness, nextWaterReadiness;
    private bool foodReady, waterReady;
    private float eatingDuration;
    private float drinkingDuration;
    private float satisfiedActionThreshold = 90f;
    private bool atContact;
    private bool controllerHeld;
    private bool controllerWasEnabled;
    private CatMealHeadMotion headContact;
    private float CareTransitionTime => Mathf.Max(.35f, animationTransitionTime);

    public bool IsInteracting => isInteracting;
    public bool IsAtContact => isInteracting && atContact;
    public string ActiveCareSound { get; private set; }
    public bool HasVisibleAction =>
        interactionButton != null && interactionButton.gameObject.activeSelf;
    public Transform FoodBowl => food != null ? food.Bowl : null;
    public Transform WaterBowl => water != null ? water.Bowl : null;

    public void RebindAnimator(Animator replacement)
    {
        if (replacement != null)
            animator = replacement;
    }

    private void Reset()
    {
        catTransform = transform;
        catMovement = GetComponent<CatMovement>();
        hungerSystem = GetComponent<HungerSystem>();
        thirstSystem = GetComponent<ThirstSystem>();
        sleepInteraction = GetComponent<SleepInteraction>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        eatingDuration = interactionDuration;
        drinkingDuration = interactionDuration;

        if (GameBalanceConfig.TryGetActiveBalance(out GameBalanceConfig.BalanceProfile balance))
        {
            eatingDuration = balance.EatingDuration;
            drinkingDuration = balance.DrinkingDuration;
            satisfiedActionThreshold = balance.SatisfiedActionThreshold;
        }

        ResolveSceneReferences();

        if (speechBubble == null)
            speechBubble = GetComponent<CatSpeechBubble>() ?? gameObject.AddComponent<CatSpeechBubble>();

        speedParameterHash = Animator.StringToHash(speedAnimatorParameter);
        food.Initialize();
        water.Initialize();

        if (interactionButton != null)
        {
            ConfigureNeutralButtonColors();
            interactionButton.onClick.RemoveListener(OnInteractionButtonClicked);
            interactionButton.onClick.AddListener(OnInteractionButtonClicked);
        }

        if (buttonText != null)
            buttonText.raycastTarget = false;

        if (buttonShadowText != null)
            buttonShadowText.raycastTarget = false;

        SetButtonVisible(false);
    }

    public void ResolveSceneReferences()
    {
        if (catTransform == null)
            catTransform = transform;
        if (catMovement == null)
            catMovement = GetComponent<CatMovement>();
        if (hungerSystem == null)
            hungerSystem = FindAnyObjectByType<HungerSystem>(FindObjectsInactive.Include);
        if (thirstSystem == null)
            thirstSystem = FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include);
        if (sleepInteraction == null)
            sleepInteraction = GetComponent<SleepInteraction>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (interactionButton == null)
            interactionButton = FindNamedComponent<Button>("ActionButton");

        if (interactionButton != null)
        {
            TMP_Text[] labels = interactionButton.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (buttonShadowText == null && label.name == "ActionButtonTextShadow")
                    buttonShadowText = label;
                else if (buttonText == null && label.name != "ActionButtonTextShadow")
                    buttonText = label;
            }

            ConfigureNeutralButtonColors();
            interactionButton.onClick.RemoveListener(OnInteractionButtonClicked);
            interactionButton.onClick.AddListener(OnInteractionButtonClicked);
        }

        if (buttonText != null)
            buttonText.raycastTarget = false;
        if (buttonShadowText != null)
            buttonShadowText.raycastTarget = false;
    }

    private static T FindNamedComponent<T>(string objectName) where T : Component
    {
        T[] candidates = FindObjectsByType<T>(FindObjectsInactive.Include);
        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i].name == objectName)
                return candidates[i];
        }

        return null;
    }

    private void Update()
    {
        if (HomeUiFlow.IsHomeControlBlocked || TitleScreen.IsShowing)
        {
            currentBowl = null;
            SetButtonVisible(false);
            return;
        }
        if (isInteracting)
        {
            SetButtonVisible(false);
            return;
        }

        if (catMovement != null && catMovement.AreWorldActionsBlocked)
        {
            currentBowl = null;
            SetButtonVisible(false);
            return;
        }

        if (CatActionState.IsBusy(catMovement) &&
            (sleepInteraction == null || !sleepInteraction.IsSleeping))
        {
            currentBowl = null;
            SetButtonVisible(false);
            return;
        }

        if (sleepInteraction != null && sleepInteraction.WantsActionButton)
        {
            currentBowl = null;
            UpdateSleepButton();
            return;
        }

        currentBowl = FindClosestAvailableBowl();
        UpdateButton(currentBowl);
    }

    private void OnInteractionButtonClicked()
    {
        if (!isActiveAndEnabled)
            return;
        if (HomeUiFlow.IsHomeControlBlocked || TitleScreen.IsShowing)
            return;
        if (isInteracting)
            return;

        bool wakingSleepingCat =
            showingSleepStyle && sleepInteraction != null && sleepInteraction.IsSleeping;
        if (catMovement != null &&
            (catMovement.AreWorldActionsBlocked ||
             (catMovement.IsMovementLocked && !wakingSleepingCat)))
        {
            return;
        }

        if (showingSleepStyle)
        {
            if (sleepInteraction != null && sleepInteraction.WantsActionButton && sleepInteraction.TryHandleActionButton())
                UpdateSleepButton();
            else
                SetButtonVisible(false);

            return;
        }

        if (CatActionState.IsBusy(catMovement))
            return;

        // Revalidate the displayed action silently if the world changed since
        // it was offered. The HUD never offers an instruction to move or turn.
        if (!CanInteractWith(currentBowl)) { currentBowl = null; SetButtonVisible(false); return; }
        if (!CatCareEligibility.CanAccept(catMovement,
            ReferenceEquals(currentBowl, food) ? CatCareNeed.Food : CatCareNeed.Water))
        { currentBowl = null; SetButtonVisible(false); return; }

        // A stale button must neither teleport the cat nor select another item.
        if (float.IsPositiveInfinity(GetAvailableDistanceSquared(currentBowl)))
        {
            currentBowl = null;
            SetButtonVisible(false);
            return;
        }

        if (!ValidateInteraction(currentBowl))
        {
            currentBowl = null;
            SetButtonVisible(false);
            return;
        }

        BowlSetup selectedBowl = currentBowl;
        if (!TryPrepareBowlStart(selectedBowl, out _))
        {
            foodReady = waterReady = false;
            nextFoodReadiness = nextWaterReadiness = 0f;
            currentBowl = null;
            SetButtonVisible(false);
            return;
        }

        speechBubble?.DismissOwned(this);
        interactionCoroutine = StartCoroutine(PerformInteraction(selectedBowl));
    }

    private IEnumerator PerformInteraction(BowlSetup bowl)
    {
        var idleBehavior = GetComponent<CatIdleBehavior>();
        bool finishingIdleBeat = idleBehavior != null && idleBehavior.IsPerformingBeat;
        isInteracting = true;
        activeBowl = bowl;
        needRecoveryCompleted = false;
        SetButtonVisible(false);

        if (catMovement != null)
            catMovement.SetMovementLocked(this, true);

        animator.SetFloat(speedParameterHash, 0f);
        controllerWasEnabled = characterController != null && characterController.enabled;
        controllerHeld = true;
        if (controllerWasEnabled) characterController.enabled = false;

        // A groom/stretch pose is not a standing source for the meal reach.
        // Give its native pose a short, smooth return first. The movement lock
        // makes idle ownership yield, so it cannot overwrite this transition.
        if (finishingIdleBeat)
        {
            animator.CrossFadeInFixedTime(BaseLayerPrefix + idleState, CareTransitionTime, 0);
            yield return new WaitForSeconds(CareTransitionTime + .05f);
            Physics.SyncTransforms();
            if (!TryPrepareBowlStart(bowl, out _))
            {
                FinishInteraction();
                foodReady = waterReady = false;
                yield break;
            }
        }

        // Own the native Idle-to-Eat blend from its first frame. Binding after
        // this blend left the low head unprotected and measured a raised eating
        // forepaw as the permanent sole offset for the later standing pose.
        headContact = GetComponent<CatMealHeadMotion>();
        if (headContact == null) headContact = gameObject.AddComponent<CatMealHeadMotion>();
        if (!headContact.Begin(this, bowl.ContactPoint != null ? bowl.ContactPoint : bowl.Bowl))
        { CancelInteraction(); yield break; }
        string interactionStatePath = BaseLayerPrefix + bowl.AnimationState;
        float reachBlendDuration = CareTransitionTime;
        animator.CrossFadeInFixedTime(interactionStatePath, reachBlendDuration, 0);
        float reachTime = 0f;
        // A native chew/lap cycle may reach its low point after the old .85s
        // timeout, especially from the edge of an accepted stance. Wait for
        // measured contact, with a bounded deadline, rather than cancelling a
        // physically clear reach before the source animation gets there.
        float reachDuration = Mathf.Max(2f, animationTransitionTime + .7f);
        float contactTime = 0f;
        while (reachTime < reachDuration)
        {
            headContact.Sample(this, Mathf.Clamp01(reachTime / reachBlendDuration));
            yield return null;
            reachTime += Time.deltaTime;
            bool touching = headContact.MinimumDistance <= CatCareReachGeometry.ContactDistance && headContact.CareFrameClear &&
                headContact.Distance <= CatCareReachGeometry.ContactDistance;
            contactTime = touching ? contactTime + Time.deltaTime : 0f;
            if (contactTime >= .10f) break;
        }
        if (contactTime < .10f)
        {
            yield return LeaveBowl();
            FinishInteraction();
            foodReady = waterReady = false;
            yield break;
        }
        atContact = true;
        if (!TryBeginNeedRecovery(bowl))
        { yield return LeaveBowl(); FinishInteraction(); yield break; }
        ActiveCareSound=ReferenceEquals(bowl,water)?"CatDrink":"CatEat";

        // Recovery runs on the persistent HUD need system. Keep action ownership
        // through its last tick and distinguish completion from interruption.
        while (!needRecoveryCompleted && (ReferenceEquals(bowl, food)
            ? hungerSystem != null && hungerSystem.IsEatingFor(this)
            : thirstSystem != null && thirstSystem.IsDrinkingFor(this)))
        {
            headContact.Sample(this, 1f);
            yield return null;
        }

        if (!needRecoveryCompleted)
        {
            CancelInteraction();
            yield break;
        }

        // Care audio ends with the last bite/lap, before standing and walking
        // away. Action ownership remains held until the accepted exit finishes.
        ActiveCareSound = null;
        try
        {
            bowl.OnInteractionCompleted?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }

        if (!isActiveAndEnabled)
            yield break;

        yield return LeaveBowl();
        FinishInteraction();
    }

    private IEnumerator LeaveBowl()
    {
        ActiveCareSound = null;
        atContact = false;
        if (!controllerHeld) yield break;
        if (headContact != null) yield return headContact.SettleOut(this,
            () => animator.CrossFadeInFixedTime(BaseLayerPrefix + idleState, CareTransitionTime, 0), CareTransitionTime);
        else
        {
            animator.CrossFadeInFixedTime(BaseLayerPrefix + idleState, CareTransitionTime, 0);
            yield return new WaitForSeconds(CareTransitionTime);
        }
        if (headContact != null) headContact.Stop(this);
        RestoreController();
    }

    private void RestoreController()
    {
        if (!controllerHeld) return;
        controllerHeld = false;
        // The root stayed in the accepted clear stance throughout care.
        if (headContact != null) headContact.Stop(this);
        if (characterController != null) characterController.enabled = controllerWasEnabled;
    }

    private BowlSetup FindClosestAvailableBowl()
    {
        if (catTransform == null)
            return null;

        float foodDistanceSquared = GetReadyDistanceSquared(food, ref nextFoodReadiness, ref foodReady);
        float waterDistanceSquared = GetReadyDistanceSquared(water, ref nextWaterReadiness, ref waterReady);

        bool foodIsNear = !float.IsPositiveInfinity(foodDistanceSquared);
        bool waterIsNear = !float.IsPositiveInfinity(waterDistanceSquared);

        if (foodIsNear && waterIsNear)
            return foodDistanceSquared <= waterDistanceSquared ? food : water;
        if (foodIsNear)
            return food;
        if (waterIsNear)
            return water;

        return null;
    }

    private float GetAvailableDistanceSquared(BowlSetup bowl)
    {
        if (!CanInteractWith(bowl))
            return float.PositiveInfinity;

        if (!CatCareEligibility.CanAccept(catMovement,
            ReferenceEquals(bowl, food) ? CatCareNeed.Food : CatCareNeed.Water))
            return float.PositiveInfinity;

        // Cheap stance checks run every frame. Skin clearance is sampled only
        // for an otherwise usable bowl, at a bounded rate below, and on click.
        Transform target = bowl.ContactPoint != null ? bowl.ContactPoint : bowl.Bowl;
        Vector3 delta = target.position - catTransform.position;
        if (Mathf.Abs(delta.y) > .4f) return float.PositiveInfinity;
        delta.y = 0f;
        float radius = .75f * Mathf.Abs(catTransform.lossyScale.y / .5f);
        return delta.sqrMagnitude <= radius * radius &&
            CatMealHeadMotion.TryPrepareBowlPose(catMovement, target, out _) ? delta.sqrMagnitude : float.PositiveInfinity;
    }

    private float GetReadyDistanceSquared(BowlSetup bowl, ref float nextCheck, ref bool ready)
    {
        float distance = GetAvailableDistanceSquared(bowl);
        if (float.IsPositiveInfinity(distance))
        { ready = false; return distance; }
        if (Time.unscaledTime >= nextCheck)
        {
            nextCheck = Time.unscaledTime + .15f;
            Physics.SyncTransforms();
            ready = TryPrepareBowlStart(bowl, out _);
        }
        return ready ? distance : float.PositiveInfinity;
    }

    private bool TryPrepareBowlStart(BowlSetup bowl, out CatActivityStart start)
    {
        start = default;
        return bowl != null && CareInteractionTarget.IsVisibleInRoom(bowl.Bowl, catTransform) &&
            CatMealHeadMotion.TryPrepareBowlPose(catMovement,
                bowl.ContactPoint != null ? bowl.ContactPoint : bowl.Bowl, out _) &&
            CatMealHeadMotion.TryPrepareCareStart(catMovement,
                bowl.ContactPoint != null ? bowl.ContactPoint : bowl.Bowl, out start);
    }

    private bool CanInteractWith(BowlSetup bowl)
    {
        if (bowl == null || !bowl.IsFull ||
            !CareInteractionTarget.IsVisibleInRoom(bowl.Bowl, catTransform))
            return false;

        if (ReferenceEquals(bowl, food))
            return hungerSystem != null && !hungerSystem.IsEating;

        if (ReferenceEquals(bowl, water))
            return thirstSystem != null && !thirstSystem.IsDrinking;

        return false;
    }

    private bool TryBeginNeedRecovery(BowlSetup bowl)
    {
        if (ReferenceEquals(bowl, food))
        {
            if (hungerSystem == null) return false;
            return hungerSystem.BeginEating(eatingDuration, this, HandleNeedRecoveryCompleted);
        }

        if (ReferenceEquals(bowl, water))
        {
            if (thirstSystem == null) return false;
            return thirstSystem.BeginDrinking(drinkingDuration, this, HandleNeedRecoveryCompleted);
        }

        return false;
    }

    private void HandleNeedRecoveryCompleted() => needRecoveryCompleted = true;

    private void StopNeedRecovery()
    {
        if (hungerSystem != null)
        {
            if (ReferenceEquals(activeBowl, food)) hungerSystem.CancelEating(this);
        }
        if (thirstSystem != null)
        {
            if (ReferenceEquals(activeBowl, water)) thirstSystem.CancelDrinking(this);
        }
        activeBowl = null;
        needRecoveryCompleted = false;
    }

    private bool ValidateInteraction(BowlSetup bowl)
    {
        if (animator == null)
        {
            Debug.LogError("BowlInteraction: Animator referansı atanmamış.", this);
            return false;
        }

        if (bowl.Bowl == null || bowl.InteractionPoint == null)
        {
            Debug.LogError("BowlInteraction: Seçilen kase için Bowl ve Interaction Point referansları atanmalıdır.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(bowl.AnimationState))
        {
            Debug.LogError("BowlInteraction: Seçilen kasenin Animation State alanı boş.", this);
            return false;
        }

        if (!HasAnimatorState(bowl.AnimationState))
        {
            Debug.LogError($"BowlInteraction: Animator içinde '{BaseLayerPrefix + bowl.AnimationState}' state'i bulunamadı. Hareket sistemi açık bırakıldı.", animator);
            return false;
        }

        if (string.IsNullOrWhiteSpace(idleState) || !HasAnimatorState(idleState))
        {
            Debug.LogError($"BowlInteraction: Animator içinde '{BaseLayerPrefix + idleState}' state'i bulunamadı. Hareket sistemi açık bırakıldı.", animator);
            return false;
        }

        if (!HasFloatParameter(speedParameterHash))
        {
            Debug.LogError($"BowlInteraction: Animator içinde Float türünde '{speedAnimatorParameter}' parametresi bulunamadı. Hareket sistemi açık bırakıldı.", animator);
            return false;
        }

        return true;
    }

    private bool HasAnimatorState(string stateName)
    {
        int stateHash = Animator.StringToHash(BaseLayerPrefix + stateName);
        return animator.HasState(0, stateHash);
    }

    private bool HasFloatParameter(int parameterHash)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash && parameter.type == AnimatorControllerParameterType.Float)
                return true;
        }

        return false;
    }

    private void UpdateButton(BowlSetup bowl)
    {
        if (bowl == null)
        {
            SetButtonVisible(false);
            return;
        }

        SetButtonText(bowl.ButtonText);

        bool buttonIsVisible = interactionButton != null && interactionButton.gameObject.activeSelf;
        if (!ReferenceEquals(styledBowl, bowl) || !buttonIsVisible)
        {
            ApplyButtonAppearance(bowl);
            styledBowl = bowl;
            showingSleepStyle = false;
        }

        SetButtonVisible(true);
    }

    private void UpdateSleepButton()
    {
        if (sleepInteraction == null || !sleepInteraction.WantsActionButton)
        {
            SetButtonVisible(false);
            return;
        }

        SetButtonText(sleepInteraction.CurrentButtonText);

        bool buttonIsVisible = interactionButton != null && interactionButton.gameObject.activeSelf;
        if (!showingSleepStyle || !buttonIsVisible)
        {
            ApplySleepButtonAppearance();
            styledBowl = null;
            showingSleepStyle = true;
        }

        SetButtonVisible(true);
    }

    private void SetButtonText(string text)
    {
        text=GameInteractionCopy.Action(text);
        if (buttonText != null && buttonText.text != text)
            buttonText.text = text;

        if (buttonShadowText != null && buttonShadowText.text != text)
            buttonShadowText.text = text;
    }

    private void ApplyButtonAppearance(BowlSetup bowl)
    {
        if(interactionButton!=null && interactionButton.targetGraphic is LowPolyPanelGraphic surface)
        {
            StorybookHudBottomPresentation.StyleButton(interactionButton,true);
            if(buttonText!=null)buttonText.color=StorybookHudBottomPresentation.Cream;
            if(buttonShadowText!=null)buttonShadowText.gameObject.SetActive(false);
            return;
        }

        bool isWater = ReferenceEquals(bowl, water);
        Color textColor = isWater ? drinkButtonTextColor : eatButtonTextColor;
        Color shadowColor = isWater ? drinkButtonShadowColor : eatButtonShadowColor;
        Sprite buttonSprite = isWater ? drinkButtonSprite : eatButtonSprite;

        if (buttonText != null)
            buttonText.color = textColor;

        if (buttonShadowText != null)
            buttonShadowText.color = shadowColor;

        if (interactionButton == null)
            return;

        Image buttonImage = interactionButton.targetGraphic as Image;
        if (buttonImage == null)
            buttonImage = interactionButton.GetComponent<Image>();

        if (buttonImage == null)
            return;

        if (buttonSprite != null)
            buttonImage.sprite = buttonSprite;

        buttonImage.color = Color.white;
    }

    private void ApplySleepButtonAppearance()
    {
        if(interactionButton!=null && interactionButton.targetGraphic is LowPolyPanelGraphic surface)
        {
            StorybookHudBottomPresentation.StyleButton(interactionButton,true);
            if(buttonText!=null)buttonText.color=StorybookHudBottomPresentation.Cream;
            if(buttonShadowText!=null)buttonShadowText.gameObject.SetActive(false);
            return;
        }

        if (buttonText != null)
            buttonText.color = sleepButtonTextColor;

        if (buttonShadowText != null)
            buttonShadowText.color = sleepButtonShadowColor;

        if (interactionButton == null)
            return;

        Image buttonImage = interactionButton.targetGraphic as Image;
        if (buttonImage == null)
            buttonImage = interactionButton.GetComponent<Image>();

        if (buttonImage == null)
            return;

        Sprite sprite = sleepButtonSprite != null ? sleepButtonSprite : eatButtonSprite;
        if (sprite != null)
            buttonImage.sprite = sprite;

        buttonImage.color = Color.white;
    }

    private void ConfigureNeutralButtonColors()
    {
        ColorBlock colors = interactionButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
        colors.pressedColor = new Color32(235, 235, 235, 255);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        interactionButton.colors = colors;
    }

    private void SetButtonVisible(bool visible)
    {
        if (interactionButton != null && interactionButton.gameObject.activeSelf != visible)
            interactionButton.gameObject.SetActive(visible);
    }

    private void FinishInteraction()
    {
        atContact = false;
        RestoreController();
        StopNeedRecovery();
        ActiveCareSound=null;
        interactionCoroutine = null;
        isInteracting = false;
        currentBowl = null;

        if (catMovement != null)
            catMovement.SetMovementLocked(this, false);
    }

    [ContextMenu("Fill Both Bowls")]
    private void FillBothBowls()
    {
        food.Fill();
        water.Fill();
    }

    [ContextMenu("Fill Food Bowl")]
    private void FillFoodBowl()
    {
        food.Fill();
    }

    [ContextMenu("Fill Water Bowl")]
    private void FillWaterBowl()
    {
        water.Fill();
    }

    public void CancelInteraction()
    {
        bool wasInteracting = isInteracting;
        StopAllCoroutines();
        FinishInteraction();
        SetButtonVisible(false);
        if (wasInteracting && animator != null)
        {
            animator.SetFloat(speedParameterHash, 0f);
            int idle = Animator.StringToHash(BaseLayerPrefix + idleState);
            if (animator.HasState(0, idle)) animator.CrossFadeInFixedTime(idle, animationTransitionTime, 0);
        }
    }

    private void OnDisable() => CancelInteraction();

    private void OnDestroy()
    {
        if (interactionButton != null)
            interactionButton.onClick.RemoveListener(OnInteractionButtonClicked);
    }
}
