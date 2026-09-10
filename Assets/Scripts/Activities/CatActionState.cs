using UnityEngine;

/// <summary>Physical actions are exclusive; hiding a prompt never releases an action.</summary>
public static class CatActionState
{
    // Scoped modal input blocks are deliberately separate: the commands panel
    // owns one itself, and must still be able to show its real action state.
    public static bool IsBusy(CatMovement cat)
    {
        if (cat == null) return true;
        var bowl = cat.GetComponent<BowlInteraction>();
        var sleep = cat.GetComponent<SleepInteraction>();
        return cat.IsMovementPhysicallyLocked || CatActivity.Active != null ||
            (bowl != null && bowl.IsInteracting) || (sleep != null && sleep.IsSleeping);
    }

    public static void CancelForTransition(CatMovement cat)
    {
        if (cat == null) return;
        var active = CatActivity.Active;
        if (active != null && active.BelongsTo(cat)) active.CancelForTransition();
        cat.GetComponent<BowlInteraction>()?.CancelInteraction();
        cat.GetComponent<SleepInteraction>()?.CancelForTransition();
    }
}
