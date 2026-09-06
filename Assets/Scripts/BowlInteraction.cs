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
        [SerializeField] private GameObject content;
        [SerializeField] private bool startsFull = true;
        [SerializeField] public string buttonText;
        [SerializeField] public string animationState;
        [SerializeField] private UnityEvent onInteractionCompleted = new UnityEvent();

        [NonSerialized] private bool isFull;

        public Transform Bowl => bowl;
        public Transform InteractionPoint => interactionPoint;
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
    [SerializeField, Min(0f)] private float interactionDistance = 0.45f;
    [SerializeField, Min(0f)] private float interactionDuration = 10f;
    [SerializeField, Min(0f)] private float animationTransitionTime = 0.15f;

    [Header("Animator")]
    [SerializeField] private string speedAnimatorParameter = "Speed";
    [SerializeField] private string idleState = "Idle";

    private const string BaseLayerPrefix = "Base Layer.";

    private BowlSetup currentBowl;
    private Coroutine interactionCoroutine;
    private bool isInteracting;
    private int speedParameterHash;
    private BowlSetup styledBowl;
    private bool showingSleepStyle;
    private float eatingDuration;
    private float drinkingDuration;
    private float satisfiedActionThreshold = 90f;

    public bool IsInteracting => isInteracting;
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
        if (isInteracting)
            return;

        if (catMovement != null && catMovement.AreWorldActionsBlocked)
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
        if (isInteracting)
            return;

        bool wakingSleepingCat =
            sleepInteraction != null && sleepInteraction.IsSleeping;
        if (catMovement != null &&
            (catMovement.AreWorldActionsBlocked ||
             (catMovement.IsMovementLocked && !wakingSleepingCat)))
        {
            return;
        }

        if (sleepInteraction != null && sleepInteraction.WantsActionButton)
        {
            if (sleepInteraction.TryHandleActionButton())
                UpdateSleepButton();

            return;
        }

        if (currentBowl == null || !CanInteractWith(currentBowl))
            return;

        if (!ValidateInteraction(currentBowl))
        {
            currentBowl = null;
            SetButtonVisible(false);
            return;
        }

        BowlSetup selectedBowl = currentBowl;
        if (IsSatisfied(selectedBowl))
        {
            speechBubble?.Show(ReferenceEquals(selectedBowl, food)
                ? GameContentCopy.Text("Şu an aç değilim!","I'm not hungry right now!")
                : GameContentCopy.Text("Şu an susamadım!","I'm not thirsty right now!"));
            return;
        }

        if (!TryBeginNeedRecovery(selectedBowl))
        {
            currentBowl = null;
            SetButtonVisible(false);
            return;
        }

        interactionCoroutine = StartCoroutine(PerformInteraction(selectedBowl));
    }

    private IEnumerator PerformInteraction(BowlSetup bowl)
    {
        isInteracting = true;
        SetButtonVisible(false);

        if (catMovement != null)
            catMovement.SetMovementLocked(this, true);

        animator.SetFloat(speedParameterHash, 0f);
        MoveAndFaceCat(bowl);

        string interactionStatePath = BaseLayerPrefix + bowl.AnimationState;
        animator.CrossFadeInFixedTime(interactionStatePath, animationTransitionTime, 0);

        yield return new WaitForSeconds(GetInteractionDuration(bowl));

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

        animator.CrossFadeInFixedTime(BaseLayerPrefix + idleState, animationTransitionTime, 0);
        FinishInteraction();
    }

    private void MoveAndFaceCat(BowlSetup bowl)
    {
        bool controllerWasEnabled = characterController != null && characterController.enabled;
        if (controllerWasEnabled)
            characterController.enabled = false;

        transform.position = bowl.InteractionPoint.position;

        Vector3 direction = bowl.Bowl.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation =
                Quaternion.LookRotation(direction.normalized, Vector3.up) *
                Quaternion.Euler(0f, modelForwardOffset, 0f);
        }

        if (controllerWasEnabled && characterController != null)
            characterController.enabled = true;
    }

    private BowlSetup FindClosestAvailableBowl()
    {
        if (catTransform == null)
            return null;

        float maxDistanceSquared = interactionDistance * interactionDistance;
        float foodDistanceSquared = CanInteractWith(food)
            ? GetHorizontalDistanceSquared(catTransform, food)
            : float.PositiveInfinity;
        float waterDistanceSquared = CanInteractWith(water)
            ? GetHorizontalDistanceSquared(catTransform, water)
            : float.PositiveInfinity;

        bool foodIsNear = foodDistanceSquared <= maxDistanceSquared;
        bool waterIsNear = waterDistanceSquared <= maxDistanceSquared;

        if (foodIsNear && waterIsNear)
            return foodDistanceSquared <= waterDistanceSquared ? food : water;
        if (foodIsNear)
            return food;
        if (waterIsNear)
            return water;

        return null;
    }

    private static float GetHorizontalDistanceSquared(Transform cat, BowlSetup bowl)
    {
        if (cat == null || bowl == null || bowl.Bowl == null)
            return float.PositiveInfinity;

        Vector3 difference = bowl.Bowl.position - cat.position;
        difference.y = 0f;
        return difference.sqrMagnitude;
    }

    private bool CanInteractWith(BowlSetup bowl)
    {
        if (bowl == null || !bowl.IsFull || bowl.Bowl == null)
            return false;

        if (ReferenceEquals(bowl, food))
            return hungerSystem != null && !hungerSystem.IsEating;

        if (ReferenceEquals(bowl, water))
            return thirstSystem != null && !thirstSystem.IsDrinking;

        return false;
    }

    private bool IsSatisfied(BowlSetup bowl)
    {
        if(ReferenceEquals(bowl,food))
            return hungerSystem!=null&&hungerSystem.CurrentHunger>=satisfiedActionThreshold;
        if(ReferenceEquals(bowl,water))
            return thirstSystem!=null&&thirstSystem.CurrentThirst>=satisfiedActionThreshold;
        return false;
    }

    private bool TryBeginNeedRecovery(BowlSetup bowl)
    {
        if (ReferenceEquals(bowl, food))
            return hungerSystem != null && hungerSystem.BeginEating(eatingDuration);

        if (ReferenceEquals(bowl, water))
            return thirstSystem != null && thirstSystem.BeginDrinking(drinkingDuration);

        return false;
    }

    private float GetInteractionDuration(BowlSetup bowl)
    {
        return ReferenceEquals(bowl, water) ? drinkingDuration : eatingDuration;
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
        text=GameInteractionCopy.Text(text);
        if (buttonText != null && buttonText.text != text)
            buttonText.text = text;

        if (buttonShadowText != null && buttonShadowText.text != text)
            buttonShadowText.text = text;
    }

    private void ApplyButtonAppearance(BowlSetup bowl)
    {
        if(interactionButton!=null && interactionButton.targetGraphic is LowPolyPanelGraphic surface)
        {
            surface.SetPremiumBaseColor(PremiumUiStyle.Coral);
            if(buttonText!=null)buttonText.color=PremiumUiStyle.Ink;
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
            surface.SetPremiumBaseColor(PremiumUiStyle.Coral);
            if(buttonText!=null)buttonText.color=PremiumUiStyle.Ink;
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

    private void OnDisable()
    {
        bool wasInteracting = isInteracting;

        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
        }

        isInteracting = false;
        currentBowl = null;
        SetButtonVisible(false);

        if (wasInteracting && catMovement != null)
            catMovement.SetMovementLocked(this, false);
    }

    private void OnDestroy()
    {
        if (interactionButton != null)
            interactionButton.onClick.RemoveListener(OnInteractionButtonClicked);
    }
}
