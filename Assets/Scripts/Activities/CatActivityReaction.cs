using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CatActivityReaction : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string pounceState = "ActivityPounce";
    [SerializeField] private string pawSwatState = "ActivityPawSwat";
    [SerializeField] private string scratchState = "ActivityScratch";
    [SerializeField] private string tunnelState = "ActivityTunnelCrawl";
    [SerializeField] private string idleState = "Idle";

    private CatMovement movement;
    private PetHeartEffect hearts;
    private Coroutine routine;

    public bool IsReacting => routine != null;

    private void Awake()
    {
        ResolveReferences();
    }

    public void PlayCatchReaction()
    {
        PlayPounceReaction();
    }

    public void PlayPounceReaction()
    {
        BeginReaction(OneShotRoutine(pounceState, 0.72f));
    }

    public void PlayPawSwatReaction()
    {
        BeginReaction(OneShotRoutine(pawSwatState, 0.58f));
    }

    public void PlayScratchReaction(float duration)
    {
        BeginReaction(ScratchRoutine(Mathf.Max(0.5f, duration)));
    }

    public void PlayTunnelReaction(float duration)
    {
        BeginReaction(ScratchRoutine(Mathf.Max(0.4f, duration), tunnelState));
    }

    private void BeginReaction(IEnumerator reaction)
    {
        ResolveReferences();
        StopCurrentReaction();
        if (hearts != null)
            hearts.Stop();
        routine = StartCoroutine(reaction);
    }

    private IEnumerator OneShotRoutine(string state, float duration)
    {
        if (movement != null)
            movement.SetMovementLocked(this, true);
        CrossFade(state, 0.06f);
        yield return new WaitForSeconds(duration);
        FinishReaction();
    }

    private IEnumerator ScratchRoutine(float duration, string state = null)
    {
        if (movement != null)
            movement.SetMovementLocked(this, true);
        CrossFade(string.IsNullOrEmpty(state) ? scratchState : state, 0.08f);
        yield return new WaitForSeconds(duration);
        FinishReaction();
    }

    private void FinishReaction()
    {
        CrossFade(idleState, 0.12f);
        if (movement != null)
            movement.SetMovementLocked(this, false);
        routine = null;
    }

    private void StopCurrentReaction()
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = null;
        if (movement != null)
            movement.SetMovementLocked(this, false);
    }

    private void CrossFade(string state, float duration)
    {
        if (animator == null || !animator.isActiveAndEnabled || string.IsNullOrEmpty(state))
            return;
        int hash = Animator.StringToHash("Base Layer." + state);
        if (animator.HasState(0, hash))
            animator.CrossFadeInFixedTime(hash, duration, 0);
    }

    private void ResolveReferences()
    {
        if (movement == null)
            movement = GetComponent<CatMovement>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (hearts == null)
            hearts = GetComponentInChildren<PetHeartEffect>(true);
    }

    private void OnDisable()
    {
        StopCurrentReaction();
        if (hearts != null)
            hearts.Stop();
    }
}
