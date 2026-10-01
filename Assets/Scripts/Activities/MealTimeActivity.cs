using System.Collections;
using UnityEngine;

/// <summary>Eat at the real bowl from a validated player stance, without root alignment travel.</summary>
[DisallowMultipleComponent]
public sealed class MealTimeActivity : CatActivity
{
    [Header("Meal time")]
    [SerializeField] private Transform standPoint;
    [SerializeField] private Transform bowlPoint;
    [SerializeField, Min(0.5f)] private float mealDuration = 3.0f;
    [SerializeField, Min(0f)] private float hungerRestore = 40f;
    [SerializeField, Range(0f, 100f)] private float notHungryAbove = 96f;

    private CharacterController characterController;
    private bool controllerWasEnabled;
    private HungerSystem hunger;
    private CatMealHeadMotion headContact;
    public bool IsEating { get; private set; }
    public bool IsLeavingMeal { get; private set; }
    public Transform BowlPoint => bowlPoint;
    public override CatCareNeed RequiredCareNeed => CatCareNeed.Food;
    public override string ProgressLabel => IsRunning ? "EATING..." : string.Empty;
    public float MealDuration => Mathf.Max(0.5f, mealDuration);
    public float HungerRestore => Mathf.Max(0f, hungerRestore);
    public float NotHungryAbove => Mathf.Clamp(notHungryAbove, 0f, 100f);
    public bool InspectingOnly => false; // Compatibility: full cats now refuse before starting.
    protected override bool RecordsQuestProgress => !InspectingOnly;
    protected override bool UsesPreparedStart => true;

    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start) =>
        CatMealHeadMotion.TryPrepareCareStart(actor, bowlPoint, out start);

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (standPoint == null || bowlPoint == null)
        {
            failureReason = "THE BOWL IS NOT READY";
            return false;
        }
        if (hunger == null)
            hunger = FindAnyObjectByType<HungerSystem>(FindObjectsInactive.Include);

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        controllerWasEnabled = characterController != null && characterController.enabled;
        StartCoroutine(MealRoutine());
        return true;
    }

    private IEnumerator MealRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        PlayCatPose(CatActivityPose.Eat);
        IsEating = true;
        if (IsEating)
        {
            headContact = Cat.GetComponent<CatMealHeadMotion>();
            if (headContact == null) headContact = Cat.gameObject.AddComponent<CatMealHeadMotion>();
            if (!headContact.Begin(this, bowlPoint)) { CancelForTransition(); yield break; }
        }
        float elapsed = 0f;
        while (elapsed < MealDuration)
        {
            if (IsEating) headContact.Sample(this, Mathf.Clamp01(elapsed / .25f));
            yield return null;
            elapsed += Time.deltaTime;
        }
        bool reached = headContact != null && headContact.MinimumDistance <= .045f;
        IsEating = false;
        IsLeavingMeal = true;
        // Return the bounded weight shift before the existing .12s source
        // stand-up blend; the common care owner still protects every frame.
        if (headContact != null) yield return headContact.SettleOut(this, () => PlayCatPose(CatActivityPose.Sniff), .12f);
        else PlayCatPose(CatActivityPose.Sniff);
        if (headContact != null) headContact.Stop(this);
        IsLeavingMeal = false;
        yield return new WaitForSeconds(.20f);
        RestoreCat();
        if (!reached) { CancelForTransition(); yield break; }
        // Feed raises no Ate event: CatActivity records this completed quest once.
        if (hunger != null) hunger.Feed(HungerRestore);
        CompleteActivity("YUM!");
    }

    private void RestoreCat()
    {
        IsEating = false;
        IsLeavingMeal = false;
        if (headContact != null) headContact.Stop(this);
        if (characterController != null) characterController.enabled = controllerWasEnabled;
        if (Cat != null) Cat.SetMovementLocked(this, false);
    }

    protected override void CancelActivity()
    {
        if (!IsRunning) return;
        StopAllCoroutines();
        if (!HasBegunActivity) { base.CancelActivity(); return; }
        RestoreCat();
        base.CancelActivity();
    }

#if UNITY_EDITOR
    public void EditorConfigureMeal(Transform stand, Transform bowl, float duration, float restore, float hungryBelow)
    {
        standPoint = stand;
        bowlPoint = bowl;
        mealDuration = Mathf.Max(0.5f, duration);
        hungerRestore = Mathf.Max(0f, restore);
        notHungryAbove = Mathf.Clamp(hungryBelow, 0f, 100f);
    }
#endif
}
