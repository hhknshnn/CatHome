using System.Collections;
using UnityEngine;

/// <summary>
/// Living-room idle personality. When the player leaves the cat alone it
/// grooms, glances at toys, stretches, plays a short pounce, or (after a longer
/// quiet spell) purrs and pops a speech bubble. Mood comes from Bond + needs.
/// Never takes a movement lock — care, activities and joystick stay in charge.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public sealed class CatIdleBehavior : MonoBehaviour
{
    private const string BaseLayerPrefix = "Base Layer.";

    private CatMovement movement;
    private Animator animator;
    private CatSpeechBubble speech;
    private HungerSystem hunger;
    private ThirstSystem thirst;
    private EnergySystem energy;
    private PetInteraction pet;
    private SleepInteraction sleep;
    private Coroutine beatRoutine;
    private Vector3 lookPoint;
    private bool looking;
    private float idleSeconds;
    private float sinceAttention = 999f;
    private CatIdleBeat lastBeat = CatIdleBeat.LookAround;

    public bool IsPerformingBeat => beatRoutine != null;
    public CatIdleMood CurrentMood { get; private set; } = CatIdleMood.Content;

    public static void EnsureOn(CatMovement cat)
    {
        if (cat == null)
            return;
        if (cat.GetComponent<CatIdleBehavior>() == null)
            cat.gameObject.AddComponent<CatIdleBehavior>();
    }

    public void RebindAnimator(Animator replacement)
    {
        if (replacement == null)
            return;
        if (beatRoutine != null)
            StopBeat(false);
        animator = replacement;
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        StopBeat(false);
    }

    private void Update()
    {
        ResolveReferences();
        if (!CanIdle())
        {
            idleSeconds = 0f;
            if (IsPerformingBeat)
                StopBeat(!SomeoneElseOwnsAnimator());
            looking = false;
            return;
        }

        CurrentMood = CatIdlePersonality.Evaluate(
            Need(hunger != null ? hunger.CurrentHunger : 100f),
            Need(thirst != null ? thirst.CurrentThirst : 100f),
            Need(energy != null ? energy.CurrentEnergy : 100f),
            ProgressionService.BondXp);

        idleSeconds += Time.deltaTime;
        sinceAttention += Time.deltaTime;

        if (looking && movement != null)
            movement.SuggestLookDirection(lookPoint);

        if (IsPerformingBeat)
            return;

        if (sinceAttention >= CatIdlePersonality.AttentionDelay(CurrentMood) &&
            idleSeconds >= CatIdlePersonality.AttentionDelay(CurrentMood))
        {
            BeginBeat(CatIdleBeat.Attention);
            return;
        }

        if (idleSeconds >= CatIdlePersonality.QuietDelay(CurrentMood))
            BeginBeat(PickFreshBeat());
    }

    public void BeginBeatForTesting(CatIdleBeat beat)
    {
        BeginBeat(beat);
    }

    private CatIdleBeat PickFreshBeat()
    {
        CatIdleBeat beat = CatIdlePersonality.PickBeat(CurrentMood, Random.Range(0, 100));
        if (beat == lastBeat)
            beat = CatIdlePersonality.PickBeat(CurrentMood, (Random.Range(0, 100) + 37) % 100);
        return beat;
    }

    private void BeginBeat(CatIdleBeat beat)
    {
        if (beatRoutine != null)
            StopCoroutine(beatRoutine);
        lastBeat = beat;
        idleSeconds = 0f;
        beatRoutine = StartCoroutine(BeatRoutine(beat));
    }

    private IEnumerator BeatRoutine(CatIdleBeat beat)
    {
        looking = TryChooseLookPoint(beat, out lookPoint);
        if (beat == CatIdleBeat.Attention)
        {
            sinceAttention = 0f;
            HomeAudioController.PlayPurr();
            ShowAttention();
        }
        else if (!CatRunnerProgressService.ReducedMotion)
        {
            CrossFade(CatIdlePersonality.AnimatorState(beat), 0.1f);
        }

        float duration = CatIdlePersonality.BeatDuration(beat);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!CanIdle())
            {
                FinishBeat(!SomeoneElseOwnsAnimator());
                yield break;
            }

            if (looking && movement != null)
                movement.SuggestLookDirection(lookPoint);
            elapsed += Time.deltaTime;
            yield return null;
        }

        FinishBeat(!CatRunnerProgressService.ReducedMotion &&
                   !string.IsNullOrEmpty(CatIdlePersonality.AnimatorState(beat)));
    }

    private void FinishBeat(bool restoreIdle)
    {
        looking = false;
        beatRoutine = null;
        if (restoreIdle)
            CrossFade(CatIdlePersonality.IdleState, 0.12f);
    }

    private void StopBeat(bool restoreIdle)
    {
        if (beatRoutine != null)
        {
            StopCoroutine(beatRoutine);
            beatRoutine = null;
        }

        looking = false;
        if (restoreIdle)
            CrossFade(CatIdlePersonality.IdleState, 0.08f);
    }

    private bool CanIdle()
    {
        if (movement == null || !movement.isActiveAndEnabled)
            return false;
        if (!movement.IsIdle)
            return false;
        if (CatActivity.Active != null)
            return false;
        if (pet != null && pet.IsPetting)
            return false;
        if (sleep != null && sleep.IsSleeping)
            return false;
        if (hunger != null && hunger.IsEating)
            return false;
        if (TitleScreen.IsShowing)
            return false;
        if (!PetTutorialHint.IsOnboardingCompleted)
            return false;
        return !MiniGameIsActive();
    }

    private bool SomeoneElseOwnsAnimator()
    {
        if (movement != null && movement.IsMovementPhysicallyLocked)
            return true;
        if (pet != null && pet.IsPetting)
            return true;
        if (sleep != null && sleep.IsSleeping)
            return true;
        if (hunger != null && hunger.IsEating)
            return true;
        return CatActivity.Active != null;
    }

    private static bool MiniGameIsActive()
    {
        // Share the transition gate already used by home controls. Repeated
        // global controller scans made an otherwise idle cat expensive.
        return HomeUiFlow.IsMiniGameVisible;
    }

    private bool TryChooseLookPoint(CatIdleBeat beat, out Vector3 point)
    {
        if (beat == CatIdleBeat.Attention && Camera.main != null)
        {
            point = Camera.main.transform.position;
            return true;
        }

        CatActivity nearest = null;
        float nearestDistance = beat == CatIdleBeat.ToyGlance ? 6f : 4.2f;
        for (int i = 0; i < CatActivity.Registered.Count; i++)
        {
            CatActivity activity = CatActivity.Registered[i];
            if (activity == null || !activity.isActiveAndEnabled ||
                !activity.IsUnlocked || !activity.IsContentVisible)
                continue;
            if (movement == null)
                continue;
            float distance = activity.DistanceTo(movement);
            if (distance >= nearestDistance)
                continue;
            nearestDistance = distance;
            nearest = activity;
        }

        if (nearest != null)
        {
            point = nearest.transform.position;
            return true;
        }

        if (movement == null)
        {
            point = Vector3.zero;
            return false;
        }

        float side = lastBeat == CatIdleBeat.LookAround ? -1f : 1f;
        point = movement.transform.position +
                movement.transform.right * side +
                movement.transform.forward * 0.45f;
        return true;
    }

    private void ShowAttention()
    {
        if (speech == null)
            return;
        speech.ShowLocalized(CatIdlePersonality.AttentionLine(
            CurrentMood,
            CatIdentityService.DisplayName,
            Need(hunger != null ? hunger.CurrentHunger : 100f),
            Need(thirst != null ? thirst.CurrentThirst : 100f),
            Need(energy != null ? energy.CurrentEnergy : 100f)));
    }

    private void CrossFade(string state, float duration)
    {
        if (animator == null || !animator.isActiveAndEnabled || string.IsNullOrEmpty(state))
            return;
        int hash = Animator.StringToHash(BaseLayerPrefix + state);
        if (animator.HasState(0, hash))
            animator.CrossFadeInFixedTime(hash, duration, 0);
    }

    private void ResolveReferences()
    {
        if (movement == null)
            movement = GetComponent<CatMovement>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (speech == null)
            speech = GetComponent<CatSpeechBubble>();
        if (hunger == null)
            hunger = FindAnyObjectByType<HungerSystem>(FindObjectsInactive.Include);
        if (thirst == null)
            thirst = FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include);
        if (energy == null)
            energy = FindAnyObjectByType<EnergySystem>(FindObjectsInactive.Include);
        if (pet == null)
            pet = GetComponent<PetInteraction>();
        if (sleep == null)
            sleep = GetComponent<SleepInteraction>();
    }

    private static float Need(float value)
    {
        return Mathf.Clamp(value, 0f, 100f);
    }
}
