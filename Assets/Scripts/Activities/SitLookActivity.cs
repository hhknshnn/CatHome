using System.Collections;
using UnityEngine;

public enum SitLookReaction
{
    Sit = 0,
    PawSwat = 1,
    Pounce = 2
}

/// <summary>
/// Shared sit-and-look activity used by Bond milestone play: window watching,
/// feather-toy swats and garden bird watching.
/// </summary>
[DisallowMultipleComponent]
public sealed class SitLookActivity : CatActivity
{
    [Header("Look")]
    [SerializeField] private Transform lookPoint;
    [SerializeField] private SitLookReaction reactionKind = SitLookReaction.Sit;
    [SerializeField, Min(0.5f)] private float lookDuration = 2.4f;
    [SerializeField] private string completeMessage = "SO COZY!";

    private CatActivityReaction reaction;
    private CharacterController characterController;

    public override string ProgressLabel => IsRunning ? DisplayName : string.Empty;
    public Transform LookPoint => lookPoint;
    public SitLookReaction ReactionKind => reactionKind;

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (lookPoint == null)
        {
            failureReason = "NOT READY YET";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        PlayCatPose(CatActivityPose.Walk);
        reaction = Cat.GetComponent<CatActivityReaction>() ??
                   Cat.gameObject.AddComponent<CatActivityReaction>();
        characterController = Cat.GetComponent<CharacterController>();
        StartCoroutine(LookRoutine());
        return true;
    }

    private IEnumerator LookRoutine()
    {
        Cat.SetMovementLocked(this, true);
        Vector3 startPosition = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 sitPoint = Flatten(transform.position, startPosition.y);
        if (InteractionAnchor != null)
            sitPoint = Flatten(InteractionAnchor.position, startPosition.y);

        Vector3 lookTarget = lookPoint.position;
        Vector3 lookDirection = lookTarget - sitPoint;
        lookDirection.y = 0f;
        Quaternion sitRotation = lookDirection.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
            : startRotation;

        if (characterController != null)
            characterController.enabled = false;

        const float approachDuration = 0.35f;
        float elapsed = 0f;
        while (elapsed < approachDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / approachDuration);
            Cat.transform.position = Vector3.Lerp(startPosition, sitPoint, t);
            Cat.transform.rotation = Quaternion.Slerp(startRotation, sitRotation, t);
            yield return null;
        }

        if (characterController != null)
            characterController.enabled = true;

        PlayReaction();
        elapsed = 0f;
        while (elapsed < lookDuration)
        {
            elapsed += Time.deltaTime;
            if (Cat != null)
                Cat.SuggestLookDirection(lookTarget);
            yield return null;
        }

        if (Cat != null)
            Cat.SetMovementLocked(this, false);
        CompleteActivity(
            string.IsNullOrWhiteSpace(completeMessage) ? "SO COZY!" : completeMessage);
    }

    private void PlayReaction()
    {
        if (reaction == null)
            return;

        switch (reactionKind)
        {
            case SitLookReaction.PawSwat:
                reaction.PlayPawSwatReaction();
                break;
            case SitLookReaction.Pounce:
                reaction.PlayPounceReaction();
                break;
            default:
                PlayCatPose(CatActivityPose.Sit);
                break;
        }
    }

    private static Vector3 Flatten(Vector3 point, float y)
    {
        point.y = y;
        return point;
    }

    protected override void OnDisable()
    {
        StopAllCoroutines();
        if (characterController != null)
            characterController.enabled = true;
        if (Cat != null)
            Cat.SetMovementLocked(this, false);
        base.OnDisable();
    }

#if UNITY_EDITOR
    public void EditorConfigureLook(
        Transform point,
        SitLookReaction kind,
        float duration,
        string message)
    {
        lookPoint = point;
        reactionKind = kind;
        lookDuration = Mathf.Max(0.5f, duration);
        completeMessage = string.IsNullOrWhiteSpace(message) ? "SO COZY!" : message;
    }
#endif
}
