using System.Collections;
using UnityEngine;

/// <summary>
/// Shove the dish cart and watch it roll.
///
/// The only Kitchen product that moves, and unlike the toilet paper roll it needs
/// no second FBX and no pivot: the whole cart rolls, so the routine drives the
/// VisualContent transform directly and springs it back. The cart is left exactly
/// where it started — a product that drifted a little further every time the cat
/// played with it would eventually be somewhere the catalog never placed it.
///
/// The cat stays on its own feet the whole time; the controller is switched off
/// only so the shove can push it through the cart's own collider.
/// </summary>
[DisallowMultipleComponent]
public sealed class CartNudgeActivity : CatActivity
{
    [Header("Cart nudge")]
    [SerializeField] private Transform shovePoint;
    [SerializeField] private Transform cartVisual;
    [SerializeField] private Vector3 rollDirection = Vector3.right;
    [SerializeField, Min(0.02f)] private float rollDistance = 0.32f;
    [SerializeField, Min(1)] private int shoveCount = 2;

    private CharacterController characterController;
    private Vector3 visualHome;
    private bool visualHomeCaptured;

    public override string ProgressLabel => IsRunning ? "PUSHING..." : string.Empty;

    public float RollDistance => Mathf.Max(0.02f, rollDistance);
    public int ShoveCount => Mathf.Max(1, shoveCount);
    public Transform CartVisual => cartVisual;

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (shovePoint == null || cartVisual == null)
        {
            failureReason = "THE CART IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        if (!visualHomeCaptured)
        {
            visualHome = cartVisual.localPosition;
            visualHomeCaptured = true;
        }
        StartCoroutine(NudgeRoutine());
        return true;
    }

    private IEnumerator NudgeRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 shove = Flatten(shovePoint.position, start.y);

        Quaternion toShove = LookTowards(shove - start, startRotation);
        yield return Move(start, shove, startRotation, toShove, 0.34f);

        Vector3 roll = rollDirection.sqrMagnitude > 0.0001f
            ? rollDirection.normalized
            : Vector3.right;
        Quaternion facing = LookTowards(roll, toShove);
        yield return Move(shove, shove, toShove, facing, 0.20f);

        for (int i = 0; i < ShoveCount; i++)
        {
            // Paw reaches out; the cart runs ahead of it and coasts back.
            float push = 0f;
            PlayCatPose(CatActivityPose.Paw);
            while (push < 0.26f)
            {
                push += Time.deltaTime;
                float t = Mathf.Clamp01(push / 0.26f);
                float reach = Mathf.Sin(t * Mathf.PI);
                Cat.transform.position = shove + roll * (reach * 0.070f);
                Cat.transform.rotation = facing * Quaternion.Euler(-reach * 15f, 0f, 0f);
                cartVisual.localPosition = visualHome + roll * (reach * RollDistance);
                yield return null;
            }

            // Coast: the cart keeps going a little, then settles back.
            float coast = 0f;
            PlayCatPose(CatActivityPose.Sit);
            while (coast < 0.55f)
            {
                coast += Time.deltaTime;
                float t = Mathf.Clamp01(coast / 0.55f);
                float damped = Mathf.Cos(t * Mathf.PI * 2.2f) * (1f - t) * 0.34f;
                cartVisual.localPosition = visualHome + roll * (damped * RollDistance);
                Cat.transform.position = shove;
                Cat.transform.rotation = facing;
                yield return null;
            }
        }

        cartVisual.localPosition = visualHome;
        Quaternion away = LookTowards(start - shove, facing);
        yield return Move(shove, shove, facing, away, 0.22f);

        RestoreCat();
        CompleteActivity("IT MOVES!");
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
        if (cartVisual != null && visualHomeCaptured)
            cartVisual.localPosition = visualHome;
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
    public void EditorConfigureNudge(
        Transform shove, Transform visual, Vector3 direction, float distance, int shoves)
    {
        shovePoint = shove;
        cartVisual = visual;
        rollDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
        rollDistance = Mathf.Max(0.02f, distance);
        shoveCount = Mathf.Max(1, shoves);
    }
#endif
}
