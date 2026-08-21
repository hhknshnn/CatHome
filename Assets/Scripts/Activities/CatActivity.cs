using System;
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
    BirdWatch = 6
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
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedContent;

    protected CatMovement Cat { get; private set; }
    protected EnergySystem Energy { get; private set; }

    public static event System.Action<CatActivity> Completed;
    public static IReadOnlyList<CatActivity> Registered => registered;
    public static CatActivity Active { get; private set; }

    public string ActivityId => activityId;
    public string DisplayName => displayName;
    public CatActivityKind Kind => kind;
    public QuestType QuestType => questType;
    public long RequiredBondXp => requiredBondXp < 0 ? 0 : requiredBondXp;
    public string ActionText => string.IsNullOrWhiteSpace(actionText) ? "PLAY" : actionText;
    public float InteractionRadius => Mathf.Max(0.2f, interactionRadius);
    public float EnergyCost => Mathf.Max(0f, energyCost);
    protected Transform InteractionAnchor => interactionAnchor;
    public bool IsContentVisible => unlockedContent == null || unlockedContent.activeSelf;
    public bool IsRunning { get; private set; }
    public string StoreProductId => storeProductId;
    public bool RequiresStoreOwnership => !string.IsNullOrWhiteSpace(storeProductId);
    public bool IsUnlocked =>
        ProgressionService.BondXp >= RequiredBondXp &&
        (!RequiresStoreOwnership || HomeStoreService.IsOwned(storeProductId));
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
        RefreshUnlockPresentation();
    }

    protected virtual void OnDisable()
    {
        registered.Remove(this);
        ProgressionService.StateChanged -= RefreshUnlockPresentation;
        HomeStoreService.OwnershipChanged -= HandleStoreOwnershipChanged;
        if (IsRunning)
            CancelActivity();
    }

    public float DistanceTo(CatMovement cat)
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
        ResolveReferences();
        Cat = cat != null ? cat : Cat;

        if (Cat == null || Active != null || IsRunning)
            return false;

        if (!IsUnlocked)
        {
            ShowSpeech(GetLockLabel());
            return false;
        }

        if (!CanBeginActivity(out string failureReason))
        {
            ShowSpeech(failureReason);
            return false;
        }

        if (Energy == null || !Energy.TrySpendEnergy(EnergyCost))
        {
            ShowSpeech("I NEED A NAP FIRST!");
            return false;
        }

        Active = this;
        IsRunning = true;
        if (!BeginActivity())
        {
            IsRunning = false;
            Active = null;
            return false;
        }

        NotifyChanged();
        return true;
    }

    protected virtual bool CanBeginActivity(out string failureReason)
    {
        failureReason = string.Empty;
        return true;
    }

    protected abstract bool BeginActivity();

    protected void CompleteActivity(string message)
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        if (Active == this)
            Active = null;

        ProgressionService.RecordProgress(questType);
        CatHomeSaveSystem.SaveNow();
        ShowSpeech(string.IsNullOrWhiteSpace(message) ? "GREAT PLAY!" : message);
        RefreshUnlockPresentation();
        NotifyChanged();
        Completed?.Invoke(this);
    }

    protected virtual void CancelActivity()
    {
        IsRunning = false;
        if (Active == this)
            Active = null;
        NotifyChanged();
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
            RefreshUnlockPresentation();
            NotifyChanged();
        }
    }

#if UNITY_EDITOR
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
