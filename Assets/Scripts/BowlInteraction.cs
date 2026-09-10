using System;
using System.Collections;
using System.Collections.Generic;
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
    private BowlSetup activeBowl;
    private bool needRecoveryCompleted;
    private int speedParameterHash;
    private BowlSetup styledBowl;
    private bool showingSleepStyle;
    private float eatingDuration;
    private float drinkingDuration;
    private float satisfiedActionThreshold = 90f;
    private bool atContact;
    private bool controllerHeld;
    private bool controllerWasEnabled;
    private Vector3 returnPoint;
    private Vector3 feedingPosition;

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
            if (sleepInteraction != null && sleepInteraction.TryHandleActionButton())
                UpdateSleepButton();
            else
                SetButtonVisible(false);

            return;
        }

        if (CatActionState.IsBusy(catMovement))
            return;

        // Revalidate the displayed target at the click, including distance.
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
        if(selectedBowl.FeedingPoint!=null && selectedBowl.ContactPoint!=null)
        {
            var tag=GetComponentInChildren<CatBreedVisualTag>();
            var profile=CatFeedingAlignmentCatalog.Load()?.Find(tag!=null?tag.BreedId:CatBreedService.SelectedBreedId);
            if(profile==null)return;
            float scale=catTransform.lossyScale.y/.5f;
            Vector3 mouth=profile.mouthOffset*scale;mouth.y=0f;
            var rotation=selectedBowl.FeedingPoint.rotation;
            feedingPosition=selectedBowl.ContactPoint.position-rotation*(mouth+Vector3.forward*.035f);
            feedingPosition.y=profile.rootHeight*scale;
        }
        if (IsSatisfied(selectedBowl))
        {
            speechBubble?.Show(ReferenceEquals(selectedBowl, food)
                ? GameContentCopy.Text("Şu an aç değilim!","I'm not hungry right now!")
                : GameContentCopy.Text("Şu an susamadım!","I'm not thirsty right now!"));
            return;
        }

        Vector3 authoredStand = selectedBowl.InteractionPoint.position;
        authoredStand.y = catTransform.position.y;
        Vector3 stand = authoredStand;
        List<Vector3> approach;
        bool measuredContact = selectedBowl.FeedingPoint != null && selectedBowl.ContactPoint != null;
        bool reachable = measuredContact
            ? CatActivityMotion.TryFloorPath(catMovement, catTransform.position, stand, out approach)
            : CatActivityFacing.TryFindContactStand(catMovement, selectedBowl.Bowl.position,
                authoredStand, catTransform.position, out stand, out approach);
        // A nearby cat already on the open side need not visit the fixed
        // entrance and turn back. Preserve its safe starting point for exit.
        if(measuredContact && Vector3.Distance(catTransform.position,feedingPosition)<.85f &&
            CatActivityMotion.IsControllerFloorClear(catMovement,catTransform.position) &&
            CatActivityMotion.ClearSegment(catTransform.position,feedingPosition,.08f))
        {stand=catTransform.position;approach=new List<Vector3>();reachable=true;}
        if (!reachable)
        {
            speechBubble?.Show(GameContentCopy.Text("Kabın yanında biraz yer açalım.", "Let's make room beside the bowl."));
            return;
        }

        interactionCoroutine = StartCoroutine(PerformInteraction(selectedBowl, stand, approach));
    }

    private IEnumerator PerformInteraction(BowlSetup bowl, Vector3 stand, List<Vector3> approach)
    {
        isInteracting = true;
        activeBowl = bowl;
        needRecoveryCompleted = false;
        SetButtonVisible(false);

        if (catMovement != null)
            catMovement.SetMovementLocked(this, true);

        animator.SetFloat(speedParameterHash, 0f);
        yield return ApproachBowl(bowl, approach);
        Vector3 remaining = stand - catTransform.position; remaining.y = 0f;
        if (remaining.magnitude > .04f || !CareInteractionTarget.IsVisibleInRoom(bowl.Bowl, catTransform) ||
            !CatActivityMotion.IsFloorClear(catTransform.position) ||
            (bowl.FeedingPoint == null && CatActivityFacing.FacingDot(bowl.Bowl.position - catTransform.position, catTransform.position,
                CatActivityFacing.CameraPosition(catMovement)) < CatActivityFacing.MinimumViewDot))
        { FinishInteraction();yield break; }

        if (bowl.FeedingPoint != null && bowl.ContactPoint != null)
        {
            // The walking capsule is wider than the planted feeding pose. Only
            // the authored final step uses the measured body/paw clearance.
            returnPoint = catTransform.position;
            controllerWasEnabled = characterController != null && characterController.enabled;
            controllerHeld = true;
            if (controllerWasEnabled) characterController.enabled = false;
            yield return MoveIntoBowl(feedingPosition,bowl.FeedingPoint.rotation);
        }

        string interactionStatePath = BaseLayerPrefix + bowl.AnimationState;
        animator.CrossFadeInFixedTime(interactionStatePath, animationTransitionTime, 0);
        if (controllerHeld)
        {
            // Allow only the animator's original Eat/Drink transition. The
            // measured root pose was reached while walking; no IK or per-frame
            // tracking moves the neck, chest, limbs or body during the clip.
            yield return new WaitForSeconds(animationTransitionTime+.05f);
            atContact = true;
        }
        if (!TryBeginNeedRecovery(bowl))
        { yield return LeaveBowl(); FinishInteraction(); yield break; }
        ActiveCareSound=ReferenceEquals(bowl,water)?"CatDrink":"CatEat";

        // Recovery runs on the persistent HUD need system. Keep action ownership
        // through its last tick and distinguish completion from interruption.
        while (!needRecoveryCompleted && (ReferenceEquals(bowl, food)
            ? hungerSystem != null && hungerSystem.IsEatingFor(this)
            : thirstSystem != null && thirstSystem.IsDrinkingFor(this)))
            yield return null;

        if (!needRecoveryCompleted)
        {
            CancelInteraction();
            yield break;
        }

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
        yield return LeaveBowl();
        FinishInteraction();
    }

    private IEnumerator ApproachBowl(BowlSetup bowl, List<Vector3> approach)
    {
        // Follow the validated side route; turning the cat away from the bowl
        // would improve its silhouette while breaking the actual mouth contact.
        float remaining = 7f;
        foreach (Vector3 point in approach)
        {
            Vector3 target = point; target.y = catTransform.position.y;
            while ((target - catTransform.position).sqrMagnitude > .0016f)
            {
                remaining -= Time.deltaTime;
                if (remaining <= 0f || !CareInteractionTarget.IsVisibleInRoom(bowl.Bowl, catTransform) ||
                    !CatActivityMotion.ClearSegment(catTransform.position, target, CatActivityMotion.ControllerFloorRadius(catMovement)))
                { animator.SetFloat(speedParameterHash, 0f); yield break; }
                Vector3 direction = target - catTransform.position; direction.y = 0f;
                Quaternion turn = Quaternion.LookRotation(direction) * Quaternion.Euler(0, modelForwardOffset, 0);
                catTransform.rotation = Quaternion.RotateTowards(catTransform.rotation, turn, 220f * Time.deltaTime);
                if (Quaternion.Angle(catTransform.rotation, turn) < 45f)
                {
                    Vector3 next = Vector3.MoveTowards(catTransform.position, target, .75f * Time.deltaTime);
                    if (characterController != null && characterController.enabled) characterController.Move(next - catTransform.position);
                    else catTransform.position = next;
                    animator.SetFloat(speedParameterHash, .5f);
                }
                else animator.SetFloat(speedParameterHash, 0f);
                yield return null;
            }
        }
        animator.SetFloat(speedParameterHash, 0f);
        if (bowl.FeedingPoint != null) yield break;
        Vector3 facing = bowl.Bowl.position - catTransform.position; facing.y = 0f;
        if (facing.sqrMagnitude < .0001f) yield break;
        Quaternion rotation = Quaternion.LookRotation(facing) * Quaternion.Euler(0, modelForwardOffset, 0);
        while (Quaternion.Angle(catTransform.rotation, rotation) > 1f)
        { catTransform.rotation = Quaternion.RotateTowards(catTransform.rotation, rotation, 220f * Time.deltaTime); yield return null; }
    }

    private IEnumerator TurnAtBowl(Quaternion target)
    {
        animator.SetFloat(speedParameterHash, 0f);
        while (Quaternion.Angle(catTransform.rotation, target) > .5f)
        { catTransform.rotation = Quaternion.RotateTowards(catTransform.rotation, target, 220f * Time.deltaTime); yield return null; }
    }

    private IEnumerator MoveIntoBowl(Vector3 target,Quaternion finalRotation)
    {
        Vector3 origin=catTransform.position,direction=target-origin;direction.y=0f;
        if(direction.sqrMagnitude<.000025f)
        {catTransform.position=target;yield return TurnAtBowl(finalRotation);yield break;}
        Vector3 first=Vector3.Lerp(origin,target,.35f);
        Vector3 second=target-finalRotation*Vector3.forward*Mathf.Min(.12f,direction.magnitude*.30f);
        float t=0f;
        while(t<1f)
        {
            float u=1f-t;
            Vector3 tangent=3f*u*u*(first-origin)+6f*u*t*(second-first)+3f*t*t*(target-second);
            Vector3 heading=tangent;heading.y=0f;
            Quaternion desired=heading.sqrMagnitude>.000001f?Quaternion.LookRotation(heading):finalRotation;
            catTransform.rotation=Quaternion.RotateTowards(catTransform.rotation,desired,220f*Time.deltaTime);
            if(Quaternion.Angle(catTransform.rotation,desired)<45f)
            {
                t=Mathf.Min(1f,t+.55f*Time.deltaTime/Mathf.Max(.01f,tangent.magnitude));u=1f-t;
                catTransform.position=u*u*u*origin+3f*u*u*t*first+3f*u*t*t*second+t*t*t*target;
                animator.SetFloat(speedParameterHash,.5f);
            }
            else animator.SetFloat(speedParameterHash,0f);
            yield return null;
        }
        catTransform.SetPositionAndRotation(target,finalRotation);
        animator.SetFloat(speedParameterHash,0f);
    }

    private IEnumerator MoveAtBowl(Vector3 target)
    {
        Vector3 direction = target - catTransform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < .0001f) yield break;
        yield return TurnAtBowl(Quaternion.LookRotation(direction));
        while ((target - catTransform.position).sqrMagnitude > .000025f)
        {
            catTransform.position = Vector3.MoveTowards(catTransform.position, target, .55f * Time.deltaTime);
            animator.SetFloat(speedParameterHash, .5f);
            yield return null;
        }
        animator.SetFloat(speedParameterHash, 0f);
    }

    private IEnumerator LeaveBowl()
    {
        ActiveCareSound = null;
        atContact = false;
        if (!controllerHeld) yield break;
        animator.CrossFadeInFixedTime(BaseLayerPrefix + idleState, animationTransitionTime, 0);
        yield return MoveAtBowl(returnPoint);
        RestoreController();
    }

    private void RestoreController()
    {
        if (!controllerHeld) return;
        controllerHeld = false;
        // Cancellation returns to the already validated open entry before the
        // walking capsule becomes active; it must never push through the tray.
        catTransform.position = returnPoint;
        if (characterController != null) characterController.enabled = controllerWasEnabled;
    }

    private BowlSetup FindClosestAvailableBowl()
    {
        if (catTransform == null)
            return null;

        float foodDistanceSquared = GetAvailableDistanceSquared(food);
        float waterDistanceSquared = GetAvailableDistanceSquared(water);

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

        return CareInteractionTarget.NearbyDistanceSquared(bowl.InteractionPoint, catTransform, interactionDistance);
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
            ModernUiArt.Action(interactionButton);
            if(buttonText!=null)buttonText.color=Color.white;
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
            ModernUiArt.Action(interactionButton);
            if(buttonText!=null)buttonText.color=Color.white;
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
