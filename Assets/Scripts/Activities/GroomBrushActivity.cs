using System.Collections;
using UnityEngine;

/// <summary>Play the original paw-grooming clip on the cat's clear nearby floor.</summary>
[DisallowMultipleComponent]
public sealed class GroomBrushActivity : CatActivity
{
    // Retained for prefab/builder compatibility; the former roller route is retired.
    [SerializeField, HideInInspector] private Transform approachPoint;
    [SerializeField, HideInInspector] private Transform rubStartPoint;
    [SerializeField, HideInInspector] private Transform rubEndPoint;
    [SerializeField, HideInInspector] private int passCount = 3;
    [SerializeField, HideInInspector] private float passDuration = .8f;
    private CharacterController controller;
    private Vector3 originalScale;

    protected override bool UsesFloorApproach => false;
    public override string ProgressLabel => IsRunning ? "GROOMING..." : string.Empty;
    public bool IsGrooming { get; private set; }
    public bool IsRubbing => IsGrooming;
    public int PassCount => 1;
    public float PassDuration { get; private set; } = 7f;
    public Vector3 SelectedRubStart { get; private set; }
    public Vector3 SelectedRubEnd => SelectedRubStart;

    protected override bool CanBeginActivity(out string failureReason)
    {
        bool ready = Cat != null && TryGetPromptDistance(Cat, out _) &&
            CatActivityMotion.IsControllerFloorClear(Cat, Cat.transform.position);
        failureReason = ready ? string.Empty : "LET'S GET A LITTLE CLOSER!";
        return ready;
    }

    protected override bool BeginActivity()
    {
        controller = Cat.GetComponent<CharacterController>(); originalScale = Cat.transform.localScale;
        StartCoroutine(GroomRoutine()); return true;
    }

    private IEnumerator GroomRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (controller != null) controller.enabled = false;
        SelectedRubStart = Cat.transform.position;
        yield return CatActivityMotion.TurnForStep(Cat,
            CatActivityFacing.Resolve(Cat, Cat.transform.position, Cat.transform.rotation));
        var animator = Cat.GetComponentInChildren<Animator>();
        PassDuration = 7f;
        if (animator != null && animator.runtimeAnimatorController != null)
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name.EndsWith("|Itching", System.StringComparison.Ordinal))
                { PassDuration = clip.length; break; }
        PlayCatPose(CatActivityPose.Groom);
        yield return new WaitForSeconds(.12f);
        IsGrooming = true;
        float elapsed = 0f;
        while (elapsed < PassDuration)
        {
            yield return null; elapsed += Time.deltaTime;
        }
        RestoreCat(); CompleteActivity("SO FLUFFY!");
    }

    private void RestoreCat()
    {
        IsGrooming = false;
        if (Cat == null) return;
        Cat.transform.localScale = originalScale;
        if (controller != null) controller.enabled = true;
        Cat.SetMovementLocked(this, false);
    }

    protected override void CancelActivity()
    {
        if (!IsRunning) return;
        StopAllCoroutines();
        if (HasBegunActivity) RestoreCat();
        base.CancelActivity();
    }

#if UNITY_EDITOR
    public void EditorConfigureGroom(Transform approach, Transform rubStart, Transform rubEnd, int passes, float duration)
    {
        approachPoint = approach; rubStartPoint = rubStart; rubEndPoint = rubEnd;
        passCount = passes; passDuration = duration;
    }
#endif
}

