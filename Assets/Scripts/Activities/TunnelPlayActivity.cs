using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TunnelPlayActivity : CatActivity
{
    [Header("Tunnel")]
    [SerializeField] private Transform entrancePoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField, Min(0.4f)] private float crawlDuration = 1.35f;

    private CatActivityReaction reaction;
    private CharacterController characterController;
    private Vector3 originalScale;

    public override string ProgressLabel => IsRunning ? "ZOOMING THROUGH!" : string.Empty;
    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        start = default;
        return entrancePoint != null && exitPoint != null && CatActivityStartResolver.Facing(actor,
            entrancePoint.position, .18f, exitPoint.position, 20f, out start);
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (entrancePoint == null || exitPoint == null)
        {
            failureReason = "TUNNEL IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        reaction = Cat.GetComponent<CatActivityReaction>() ??
                   Cat.gameObject.AddComponent<CatActivityReaction>();
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        StartCoroutine(CrawlRoutine());
        return true;
    }

    private IEnumerator CrawlRoutine()
    {
        Cat.SetMovementLocked(this, true);
        Vector3 startPosition = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 enter = startPosition;
        Vector3 exit = Flatten(exitPoint.position, startPosition.y);
        Vector3 through = exit - enter;
        through.y = 0f;
        Quaternion crawlRotation = AcceptedStart.Rotation;

        if (characterController != null)
            characterController.enabled = false;

        float elapsed = 0f;

        reaction.PlayTunnelReaction(crawlDuration);
        elapsed = 0f;
        Vector3 crouched = originalScale;
        crouched.y *= 0.72f;
        crouched.x *= 1.08f;
        crouched.z *= 1.08f;
        while (elapsed < crawlDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / crawlDuration);
            Cat.transform.position = Vector3.Lerp(enter, exit, t);
            Cat.transform.rotation = crawlRotation;
            float squash = t < 0.5f ? t * 2f : (1f - t) * 2f;
            Cat.transform.localScale = Vector3.Lerp(originalScale, crouched, squash);
            yield return null;
        }

        Cat.transform.localScale = originalScale;
        if (characterController != null)
            characterController.enabled = true;
        Cat.SetMovementLocked(this, false);
        CompleteActivity("TUNNEL CHAMPION!");
    }

    private static Vector3 Flatten(Vector3 point, float y)
    {
        point.y = y;
        return point;
    }

    protected override void CancelActivity()
    {
        if (!IsRunning) return;
        StopAllCoroutines();
        if (!HasBegunActivity) { base.CancelActivity(); return; }
        if (reaction != null)
            reaction.CancelReaction();
        if (Cat != null)
        {
            if (originalScale.sqrMagnitude > 0.0001f)
                Cat.transform.localScale = originalScale;
            Cat.SetMovementLocked(this, false);
        }
        if (characterController != null)
            characterController.enabled = true;
        base.CancelActivity();
    }

#if UNITY_EDITOR
    public void EditorConfigureTunnel(Transform entrance, Transform exit, float duration)
    {
        entrancePoint = entrance;
        exitPoint = exit;
        crawlDuration = Mathf.Max(0.4f, duration);
    }
#endif
}
