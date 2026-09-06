using System.Collections;
using UnityEngine;

/// <summary>
/// Eat from the feeding station's food bowl.
///
/// Hunger is topped up with <see cref="HungerSystem.Feed"/> rather than
/// BeginEating, for the same reason the vanity sip uses
/// <see cref="ThirstSystem.RestoreThirst"/>: BeginEating fires Ate, which already
/// records an Eat quest, and <see cref="CatActivity"/> records one again on
/// completion — the pair would count a single meal twice. `Feed` raises no event.
///
/// The bowl rim is only 0.24 up, so the cat walks to it and dips rather than
/// hopping: this is the one Kitchen routine with no arc in it. The controller is
/// still switched off, because the dip pushes the cat past what the capsule
/// allows against the stand.
/// </summary>
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
    private HungerSystem hunger;

    public override string ProgressLabel => IsRunning ? "EATING..." : string.Empty;

    public float MealDuration => Mathf.Max(0.5f, mealDuration);
    public float HungerRestore => Mathf.Max(0f, hungerRestore);
    public float NotHungryAbove => Mathf.Clamp(notHungryAbove, 0f, 100f);

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (standPoint == null || bowlPoint == null)
        {
            failureReason = "THE BOWL IS NOT READY";
            return false;
        }

        if (hunger == null)
            hunger = FindAnyObjectByType<HungerSystem>(FindObjectsInactive.Include);
        if (hunger != null && hunger.CurrentHunger >= NotHungryAbove)
        {
            failureReason = "I AM NOT HUNGRY!";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        StartCoroutine(MealRoutine());
        return true;
    }

    private IEnumerator MealRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 stand = Flatten(standPoint.position, start.y);
        Vector3 bowl = bowlPoint.position;

        Quaternion toStand = LookTowards(stand - start, startRotation);
        yield return Move(start, stand, startRotation, toStand, 0.34f);

        Quaternion inward = LookTowards(
            new Vector3(bowl.x - stand.x, 0f, bowl.z - stand.z), toStand);
        yield return Move(stand, stand, toStand, inward, 0.20f);

        float elapsed = 0f;
        PlayCatPose(CatActivityPose.Eat, standPoint);
        while (elapsed < MealDuration)
        {
            elapsed += Time.deltaTime;
            // Head dips into the bowl, with a pause between mouthfuls.
            float cycle = Mathf.Repeat(elapsed, 0.75f) / 0.75f;
            float dip = cycle < 0.55f ? Mathf.Sin(cycle / 0.55f * Mathf.PI) : 0f;
            Vector3 position = stand;
            position += (inward * Vector3.forward) * (dip * 0.085f);
            position.y = stand.y - dip * 0.030f;
            Cat.transform.position = position;
            Cat.transform.rotation = inward * Quaternion.Euler(dip * 21f, 0f, 0f);
            yield return null;
        }

        Cat.transform.position = stand;
        Cat.transform.rotation = inward;
        // Back off the bowl before physics resumes.
        Quaternion away = LookTowards(start - stand, inward);
        yield return Move(stand, stand, inward, away, 0.22f);

        RestoreCat();
        if (hunger != null)
            hunger.Feed(HungerRestore);
        CompleteActivity("YUM!");
    }

    private IEnumerator Move(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation, float duration)
    {
        PlayCatPose(CatActivityPose.Walk);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Cat.transform.position = Vector3.Lerp(from, to, t);
            Cat.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, t);
            yield return null;
        }

        Cat.transform.position = to;
        Cat.transform.rotation = toRotation;
    }

    private void RestoreCat()
    {
        if (Cat == null)
            return;

        if (characterController != null)
            characterController.enabled = true;
        Cat.SetMovementLocked(this, false);
    }

    private static Quaternion LookTowards(Vector3 direction, Quaternion fallback)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : fallback;
    }

    private static Vector3 Flatten(Vector3 point, float y)
    {
        point.y = y;
        return point;
    }

    protected override void OnDisable()
    {
        StopAllCoroutines();
        RestoreCat();
        base.OnDisable();
    }

#if UNITY_EDITOR
    public void EditorConfigureMeal(
        Transform stand, Transform bowl, float duration, float restore, float hungryBelow)
    {
        standPoint = stand;
        bowlPoint = bowl;
        mealDuration = Mathf.Max(0.5f, duration);
        hungerRestore = Mathf.Max(0f, restore);
        notHungryAbove = Mathf.Clamp(hungryBelow, 0f, 100f);
    }
#endif
}
