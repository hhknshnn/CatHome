using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScratchPostActivity : CatActivity
{
    [Header("Scratch Post")]
    [SerializeField] private Transform scratchPoint;
    [SerializeField, Min(0.5f)] private float scratchDuration = 2.4f;

    private CatActivityReaction reaction;
    private CharacterController characterController;

    public override string ProgressLabel => IsRunning ? "SCRATCHING..." : string.Empty;

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (scratchPoint == null)
        {
            failureReason = "SCRATCH POST IS NOT READY";
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
        StartCoroutine(ScratchRoutine());
        return true;
    }

    private IEnumerator ScratchRoutine()
    {
        Cat.SetMovementLocked(this, true);
        Vector3 startPosition = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 destination = scratchPoint.position;
        Vector3 lookDirection = transform.position - destination;
        lookDirection.y = 0f;
        Quaternion destinationRotation = lookDirection.sqrMagnitude > 0.001f
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
            Cat.transform.position = Vector3.Lerp(startPosition, destination, t);
            Cat.transform.rotation = Quaternion.Slerp(startRotation, destinationRotation, t);
            yield return null;
        }

        if (characterController != null)
            characterController.enabled = true;

        reaction.PlayScratchReaction(scratchDuration);
        yield return new WaitForSeconds(scratchDuration + 0.05f);
        Cat.SetMovementLocked(this, false);
        CompleteActivity("CLAWS FEEL GREAT!");
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
    public void EditorConfigureScratch(Transform point, float duration)
    {
        scratchPoint = point;
        scratchDuration = Mathf.Max(0.5f, duration);
    }
#endif
}
