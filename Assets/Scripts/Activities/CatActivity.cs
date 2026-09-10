using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CatActivityKind
{
    BallChase = 0,
    ScratchPost = 1,
    MouseHunt = 2,
    TunnelPlay = 3,
    WindowWatch = 4,
    FeatherPlay = 5,
    BirdWatch = 6,
    CanopyNap = 7,
    SwingRide = 8,
    ShowerRinse = 9,
    SinkSip = 10,
    PaperSpin = 11,
    TowelNest = 12,
    LitterDig = 13,
    GroomBrush = 14,
    HamperDive = 15,
    MirrorGaze = 16,
    MatKnead = 17,
    TubEdgeWalk = 18,
    IslandPerch = 19,
    StoolPerch = 20,
    FridgeStare = 21,
    FruitSwat = 22,
    PantryClimb = 23,
    OvenWarmth = 24,
    CartNudge = 25,
    MealTime = 26,
    KitchenSip = 27,
    KitchenMatKnead = 28,
    BedNap = 29,
    WardrobeScratch = 30,
    DaybedWatch = 31,
    KnockOff = 32,
    VanityStoolNap = 33,
    YarnSwat = 34,
    NightLightGaze = 35,
    ArtGaze = 36,
    BedroomMatKnead = 37,
    PergolaClimb = 38,
    TreeScratch = 39,
    BistroPerch = 40,
    HammockSway = 41,
    SunBask = 42,
    GrillWatch = 43,
    BirdBathSip = 44,
    PotDig = 45,
    DaisyRoll = 46,
    YarnBallChase = 47,
    AwningGaze = 48,
    HerbShelfClimb = 49,
    FeederShake = 50,
    EggChairNap = 51,
    BenchNap = 52,
    TableKnockOff = 53,
    SunMatBask = 54,
    PlanterDig = 55,
    RailingSwat = 56,
    LanternGaze = 57,
    ArchClimb = 58,
    ParasolScratch = 59,
    DiningPerch = 60,
    FirePitBask = 61,
    FountainSip = 62,
    FernWatch = 63,
    HerbTroughDig = 64,
    FestoonGaze = 65,
    StoneRugKnead = 66,
    RunnerKnead = 67,
    CushionNest = 68,
    BookKnockOff = 69,
    LampGlowBask = 70,
    BeanBagNap = 71,
    RecordSpin = 72,
    DeskPerch = 73,
    GalleryGaze = 74,
    BookcaseClimb = 75,
    ChaiseNap = 76,
    ArmchairNap = 77,
    LampWatch = 78,
    BookshelfSniff = 79,
    BookSetSniff = 80,
    PlantSniff = 81,
    PaintingWatch = 82,
    TvUnitPaw = 83,
    TelevisionWatch = 84,
    ConsolePaw = 85,
    SpeakerListen = 86,
    PodNap = 87,
    ToyMousePlay = 88,
    CeramicMeal = 89,
    CloudNap = 90,
    PetCanopyNap = 91,
    TreatPuzzle = 92,
    BallTrack = 93,
    RibbonPlay = 94,
    BellRoller = 95,
    FoodDispenser = 96,
    PillowNap = 97,
    CatGrassPlay = 98,
    BoxHide = 99,
    SofaLounge = 100,
    CoffeeTablePlay = 101,
    CompanionCommand = 102
}

public abstract class CatActivity : MonoBehaviour
{
    private static readonly List<CatActivity> registered = new List<CatActivity>();

    [Header("Identity")]
    [SerializeField] private string activityId;
    [SerializeField] private string displayName;
    [SerializeField] private CatActivityKind kind;
    [SerializeField] private QuestType questType;

    [Header("Unlock")]
    [SerializeField] private long requiredBondXp;
    [SerializeField] private string storeProductId;

    [Header("Interaction")]
    [SerializeField] private string actionText = "PLAY";
    [SerializeField, Min(0.2f)] private float interactionRadius = 1.25f;
    [SerializeField, Min(0f)] private float energyCost = 8f;
    [SerializeField] private Transform interactionAnchor;
    [SerializeField] private Transform routineEntryPoint;
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedContent;

    private CatActivityAnimation catAnimation;
    private readonly CatActivityApproach nearbyApproach = new CatActivityApproach();
    private bool hasNearbyApproach;
    private Vector3 nearbyFloor;
    protected virtual bool AllowsPerimeterApproach => true;
    protected virtual bool UsesNearbyRoutineEntry => false;
    protected bool HasNearbyApproach => hasNearbyApproach && UsesNearbyRoutineEntry;
    public Vector3 RoutineFloorPosition => HasNearbyApproach ? nearbyFloor :
        RoutineEntryPoint != null ? RoutineEntryPoint.position : transform.position;
    private Vector3 entryFloor;
    private Vector3 originalCatPosition;
    private bool restStopRequested,restWaiting;
    private float restStarted=-1f;
    protected CatMovement Cat { get; private set; }
    protected bool HasBegunActivity { get; private set; }
    private CharacterController ownedController;
    private bool originalControllerEnabled;
    protected EnergySystem Energy { get; private set; }
    protected virtual bool UsesFloorApproach => HomeStoreService.IsFixedRoomProduct(storeProductId);
    protected virtual bool RecordsQuestProgress => true;

    public static event System.Action<CatActivity> Completed;
    public static IReadOnlyList<CatActivity> Registered => registered;
    public static CatActivity Active { get; private set; }

    public string ActivityId => activityId;
    public virtual string DisplayName => HomeStoreService.TryGetProduct(storeProductId, out var product) ? product.Title :
        Kind == CatActivityKind.SofaLounge ? GameContentCopy.Text("Koltuk", "Sofa") :
        Kind == CatActivityKind.CoffeeTablePlay ? GameContentCopy.Text("Sehpa", "Coffee table") : displayName;
    public virtual CatActivityKind Kind => kind;
    public QuestType QuestType => questType;
    public long RequiredBondXp => requiredBondXp < 0 ? 0 : requiredBondXp;
    public string ActionText => string.IsNullOrWhiteSpace(actionText) ? "PLAY" : actionText;
    public float InteractionRadius => Mathf.Max(0.2f, interactionRadius);
    public virtual float EnergyCost => Mathf.Max(0f, energyCost);
    protected Transform InteractionAnchor => interactionAnchor;
    public bool IsContentVisible => unlockedContent == null || unlockedContent.activeSelf;
    public bool IsRunning { get; private set; }
    public virtual bool SupportsContinuousRest=>false;
    public bool IsWaitingForRestStop=>IsRunning && restWaiting && !restStopRequested;
    public float RestingSeconds=>restStarted<0?0:Mathf.Max(0,Time.time-restStarted);
    protected bool KeepResting
    {
        get
        {
            restWaiting=true;
            if(restStarted<0)restStarted=Time.time;
            return !restStopRequested;
        }
    }
    public bool RequestRestStop()
    {
        if(!IsRunning || !SupportsContinuousRest)return false;
        restStopRequested=true;NotifyChanged();return true;
    }
    public bool IsRestingOnFurniture => IsRunning && catAnimation != null && catAnimation.ContactSurface != null &&
        (catAnimation.CurrentPose == CatActivityPose.Sleep ||
         (catAnimation.CurrentPose == CatActivityPose.Sit && SupportsContinuousRest));
    public string StoreProductId => storeProductId;
    public Transform RoutineEntryPoint => routineEntryPoint != null ? routineEntryPoint : interactionAnchor;
    public bool RequiresStoreOwnership => !string.IsNullOrWhiteSpace(storeProductId);
    public bool IsRetired => Kind == CatActivityKind.MouseHunt || Kind == CatActivityKind.ConsolePaw || Kind == CatActivityKind.SpeakerListen || Kind == CatActivityKind.TvUnitPaw || Kind == CatActivityKind.WindowWatch;
    public bool IsUnlocked =>
        ProgressionService.BondXp >= RequiredBondXp &&
        (!RequiresStoreOwnership || (HomeStoreService.IsOwned(storeProductId)&&!HomeStoreService.IsStored(storeProductId)));
    public virtual string ProgressLabel => IsRunning ? DisplayName : string.Empty;

    protected virtual void Awake()
    {
        ResolveReferences();
        RefreshUnlockPresentation();
    }

    protected virtual void OnEnable()
    {
        if (!registered.Contains(this))
            registered.Add(this);
        ProgressionService.StateChanged += RefreshUnlockPresentation;
        HomeStoreService.OwnershipChanged += HandleStoreOwnershipChanged;
        HomeStoreService.StorageChanged += HandleStoreOwnershipChanged;
        RefreshUnlockPresentation();
    }

    protected virtual void OnDisable()
    {
        registered.Remove(this);
        ProgressionService.StateChanged -= RefreshUnlockPresentation;
        HomeStoreService.OwnershipChanged -= HandleStoreOwnershipChanged;
        HomeStoreService.StorageChanged -= HandleStoreOwnershipChanged;
        if (IsRunning)
            CancelActivity();
    }

    // Fixed openings retain their doorway check; open products use their visible perimeter.
    public const float PromptRadius = .46f;
    public virtual bool TryGetPromptDistance(CatMovement cat, out float distance)
    {
        if (IsRetired) { distance = float.PositiveInfinity; return false; }
        if (AllowsPerimeterApproach && nearbyApproach.HasGeometry(this))
            return nearbyApproach.TryResolve(this, cat, out _, out distance);
        return TryPromptAt(cat, RoutineEntryPoint, out distance);
    }

    protected bool TryPromptAt(CatMovement cat, Transform entrance, out float distance)
    {
        distance = float.PositiveInfinity;
        if (cat == null || entrance == null) return false;
        Vector3 from = cat.transform.position, to = entrance.position;
        from.y = to.y = 0f;
        distance = Vector3.Distance(from, to);
        return distance <= Mathf.Min(PromptRadius, InteractionRadius) &&
            CatActivityMotion.ClearSegment(from, to);
    }

    public virtual float DistanceTo(CatMovement cat)
    {
        if (cat == null)
            return float.PositiveInfinity;

        Vector3 anchor = interactionAnchor != null
            ? interactionAnchor.position
            : transform.position;
        Vector3 delta = cat.transform.position - anchor;
        delta.y = 0f;
        return delta.magnitude;
    }

    public string GetLockLabel()
    {
        if (RequiresStoreOwnership && !HomeStoreService.IsOwned(storeProductId))
            return "AVAILABLE IN STORE";
        if (ProgressionService.BondXp < RequiredBondXp)
            return $"NEEDS {RequiredBondXp} BOND";
        return string.Empty;
    }

    public bool TryStart(CatMovement cat)
    {
        if (Active != null || IsRunning || IsRetired || !isActiveAndEnabled)
            return false;
        ResolveReferences();
        var actor = cat != null ? cat : Cat;
        if (actor == null || !actor.isActiveAndEnabled || actor.gameObject.scene != gameObject.scene ||
            CatActionState.IsBusy(actor) || actor.AreWorldActionsBlocked || HomeUiFlow.IsMiniGameVisible)
            return false;
        Cat = actor;

        if (!IsUnlocked)
        {
            ShowSpeech(GetLockLabel());
            return false;
        }

        hasNearbyApproach = AllowsPerimeterApproach && UsesNearbyRoutineEntry &&
            nearbyApproach.TryResolve(this, Cat, out nearbyFloor, out _);
        if (!CanBeginActivity(out string failureReason))
        {
            ShowSpeech(failureReason);
            return false;
        }

        List<Vector3> approach = null;
        Physics.SyncTransforms();
        originalCatPosition = Cat.transform.position;
        entryFloor = RoutineFloorPosition;
        entryFloor.y = originalCatPosition.y;
        if (UsesFloorApproach &&
            !CatActivityMotion.TryFloorPath(originalCatPosition, entryFloor, out approach))
        {
            ShowSpeech("LET'S GET A LITTLE CLOSER!");
            return false;
        }

        if (Energy == null || !Energy.TrySpendEnergy(EnergyCost))
        {
            ShowSpeech("I NEED A NAP FIRST!");
            return false;
        }

        Active = this;
        IsRunning = true;
        HasBegunActivity = false;
        ownedController = Cat.GetComponent<CharacterController>();
        originalControllerEnabled = ownedController != null && ownedController.enabled;
        restStopRequested=false;restWaiting=false;restStarted=-1f;
        catAnimation = Cat.GetComponent<CatActivityAnimation>() ?? Cat.gameObject.AddComponent<CatActivityAnimation>();
        catAnimation.Begin(this);
        if (approach != null)
        {
            StartCoroutine(ApproachRoutine(approach));
            NotifyChanged();
            return true;
        }
        if (!BeginRoutineBody())
        {
            Energy.RestoreEnergy(EnergyCost);
            CancelActivity();
            return false;
        }

        NotifyChanged();
        return true;
    }

    private IEnumerator ApproachRoutine(List<Vector3> path)
    {
        var controller = Cat.GetComponent<CharacterController>();
        Cat.SetMovementLocked(this, true);
        if (controller != null) controller.enabled = false;
        foreach (var destination in path)
        {
            Vector3 target = destination; target.y = originalCatPosition.y;
            Vector3 direction = target - Cat.transform.position; direction.y = 0f;
            if (direction.sqrMagnitude <= .0001f) continue;
            Quaternion travel = Quaternion.LookRotation(direction);
            PlayCatPose(CatActivityPose.Sniff);
            yield return CatActivityFacing.Turn(Cat, travel, .18f);
            PlayCatPose(CatActivityPose.Walk);
            while ((Cat.transform.position - target).sqrMagnitude > .0001f)
            {
                Cat.transform.rotation = travel;
                Cat.transform.position = Vector3.MoveTowards(Cat.transform.position, target, 1.5f * Time.deltaTime);
                yield return null;
            }
        }
        if (!BeginRoutineBody())
        {
            Energy?.RestoreEnergy(EnergyCost);
            CancelActivity();
        }
    }

    private bool BeginRoutineBody()
    {
        HasBegunActivity = true;
        try { return BeginActivity(); }
        catch (Exception error) { Debug.LogException(error, this); return false; }
    }

    public bool BelongsTo(CatMovement cat) => Cat == cat;
    public void CancelForTransition() { if (IsRunning) CancelActivity(); }

    protected virtual bool CanBeginActivity(out string failureReason)
    {
        failureReason = string.Empty;
        return true;
    }

    protected abstract bool BeginActivity();

    protected void PlayCatPose(CatActivityPose pose, Transform surface = null)
    {
        catAnimation?.SetPose(pose, surface);
    }

    protected void CompleteActivity(string message)
    {
        if (!IsRunning)
            return;

        catAnimation?.End();
        RestoreSafeFloor();
        ReleaseActionControl();
        IsRunning = false;
        HasBegunActivity = false;
        restWaiting=false;
        hasNearbyApproach=false;
        if (Active == this)
            Active = null;

        if(RecordsQuestProgress) ProgressionService.RecordProgress(questType);
        CatHomeSaveSystem.SaveNow();
        ShowSpeech(string.IsNullOrWhiteSpace(message) ? "GREAT PLAY!" : message);
        RefreshUnlockPresentation();
        NotifyChanged();
        Completed?.Invoke(this);
    }

    protected virtual void CancelActivity()
    {
        StopAllCoroutines();
        catAnimation?.End();
        RestoreSafeFloor(true);
        ReleaseActionControl();
        IsRunning = false;
        HasBegunActivity = false;
        restWaiting=false;
        hasNearbyApproach=false;
        if (Active == this)
            Active = null;
        NotifyChanged();
    }

    private void ReleaseActionControl()
    {
        if (ownedController != null) ownedController.enabled = originalControllerEnabled;
        ownedController = null;
        if (Cat != null) Cat.SetMovementLocked(this, false);
    }

    private void RestoreSafeFloor(bool cancelled = false)
    {
        if (Cat == null || !UsesFloorApproach) return;
        Physics.SyncTransforms();
        if (cancelled || Cat.transform.position.y > .12f || !CatActivityMotion.IsFloorClear(Cat.transform.position) ||
            !CatActivityMotion.TryFloorPath(Cat.transform.position, entryFloor, out _))
        {
            Vector3 target = CatActivityMotion.IsFloorClear(entryFloor) ? entryFloor : originalCatPosition;
            var controller = Cat.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            Cat.transform.position = target;
            Cat.transform.rotation = Quaternion.Euler(0f, Cat.transform.eulerAngles.y, 0f);
            if (controller != null) controller.enabled = true;
            Physics.SyncTransforms();
        }
        Cat.SetMovementLocked(this, false);
    }

    protected void NotifyChanged()
    {
        ActivityPromptController.NotifyActivityChanged();
    }

    protected void ShowSpeech(string message)
    {
        if (Cat == null)
            return;
        CatSpeechBubble bubble = Cat.GetComponent<CatSpeechBubble>();
        if (bubble != null)
            bubble.Show(message);
    }

    private void ResolveReferences()
    {
        if (Cat == null)
            Cat = FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        if (Energy == null)
            Energy = FindAnyObjectByType<EnergySystem>(FindObjectsInactive.Include);
    }

    public void RefreshUnlockPresentation()
    {
        bool unlocked = IsUnlocked;
        if (lockedVisual != null)
            lockedVisual.SetActive(false);
        if (unlockedContent != null)
            unlockedContent.SetActive(unlocked);
    }

    private void HandleStoreOwnershipChanged(string productId)
    {
        if (string.IsNullOrEmpty(productId) ||
            string.Equals(productId, storeProductId, StringComparison.Ordinal))
        {
            if(IsRunning&&!IsUnlocked)CancelActivity();
            RefreshUnlockPresentation();
            NotifyChanged();
        }
    }

#if UNITY_EDITOR
    public void EditorConfigureEntry(Transform point) => routineEntryPoint = point;

    public void EditorConfigure(
        string id,
        string title,
        CatActivityKind activityKind,
        QuestType progressType,
        long bondRequirement,
        string buttonText,
        float radius,
        float cost,
        Transform anchor,
        GameObject lockObject,
        GameObject contentRoot)
    {
        activityId = id;
        displayName = title;
        kind = activityKind;
        questType = progressType;
        requiredBondXp = Math.Max(0L, bondRequirement);
        actionText = buttonText;
        interactionRadius = Mathf.Max(0.2f, radius);
        energyCost = Mathf.Max(0f, cost);
        interactionAnchor = anchor;
        lockedVisual = lockObject;
        unlockedContent = contentRoot;
        RefreshUnlockPresentation();
    }

    public void EditorConfigureStoreProduct(string productId)
    {
        storeProductId = productId;
        RefreshUnlockPresentation();
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        registered.Clear();
        Active = null;
        Completed = null;
    }
}
