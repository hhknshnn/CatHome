using UnityEngine;

public enum CatCareNeed { None, Food, Water }

/// <summary>One care refusal policy for every current and future room.</summary>
public static class CatCareEligibility
{
    public static float SatisfiedThreshold => GameBalanceConfig.GetSatisfiedActionThreshold();

    public static bool IsSatisfied(CatMovement actor, CatCareNeed need)
    {
        return TryRead(actor, need, out float value) && value >= SatisfiedThreshold;
    }

    public static bool CanAccept(CatMovement actor, CatCareNeed need) =>
        need == CatCareNeed.None || (TryRead(actor, need, out float value) && value < SatisfiedThreshold);

    // Invoke after target ownership/visibility checks, but before geometry,
    // energy, animation or movement ownership. A refusal has no gameplay cost.
    public static bool TryAccept(CatMovement actor, CatCareNeed need)
    {
        if (need == CatCareNeed.None) return true;
        string failure = !TryRead(actor, need, out float value) ? "care.unavailable" :
            value >= SatisfiedThreshold ? (need == CatCareNeed.Food ? "care.not_hungry" : "care.not_thirsty") : null;
        if (failure == null) return true;
        CatSpeechBubble.EnsureOn(actor)?.ShowKey(failure);
        return false;
    }

    private static CatMovement cachedActor;
    private static HungerSystem cachedHunger;
    private static ThirstSystem cachedThirst;
    private static bool TryRead(CatMovement actor, CatCareNeed need, out float value)
    {
        value = 0f;
        if (actor == null || !actor.isActiveAndEnabled) return false;
        if (cachedActor != actor)
        { cachedActor = actor; cachedHunger = null; cachedThirst = null; }
        if (need == CatCareNeed.Food)
        {
            if (cachedHunger != null && cachedHunger.isActiveAndEnabled &&
                (cachedHunger.CatMovement == actor || cachedHunger.gameObject == actor.gameObject))
            { value = cachedHunger.CurrentHunger; return true; }
            var local = actor.GetComponent<HungerSystem>();
            if (local != null && local.isActiveAndEnabled) { cachedHunger = local; value = local.CurrentHunger; return true; }
            var candidates = Object.FindObjectsByType<HungerSystem>(FindObjectsInactive.Exclude);
            foreach (var candidate in candidates)
                if (candidate.isActiveAndEnabled && candidate.CatMovement == actor) { cachedHunger = candidate; value = candidate.CurrentHunger; return true; }
            if (candidates.Length == 1 && candidates[0].isActiveAndEnabled) { cachedHunger = candidates[0]; value = candidates[0].CurrentHunger; return true; }
        }
        else if (need == CatCareNeed.Water)
        {
            if (cachedThirst != null && cachedThirst.isActiveAndEnabled &&
                (cachedThirst.CatMovement == actor || cachedThirst.gameObject == actor.gameObject))
            { value = cachedThirst.CurrentThirst; return true; }
            var local = actor.GetComponent<ThirstSystem>();
            if (local != null && local.isActiveAndEnabled) { cachedThirst = local; value = local.CurrentThirst; return true; }
            var candidates = Object.FindObjectsByType<ThirstSystem>(FindObjectsInactive.Exclude);
            foreach (var candidate in candidates)
                if (candidate.isActiveAndEnabled && candidate.CatMovement == actor) { cachedThirst = candidate; value = candidate.CurrentThirst; return true; }
            if (candidates.Length == 1 && candidates[0].isActiveAndEnabled) { cachedThirst = candidates[0]; value = candidates[0].CurrentThirst; return true; }
        }
        return false;
    }
}
