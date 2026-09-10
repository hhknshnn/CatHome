using System;
using System.Collections.Generic;

/// <summary>
/// Bond XP thresholds that unlock home play. Values are derived from current
/// Bond XP; nothing here is spent or saved as a separate resource.
/// </summary>
public readonly struct BondMilestone
{
    public BondMilestone(
        string id,
        long requiredBondXp,
        string title,
        string description,
        CatActivityKind activityKind)
    {
        Id = id;
        RequiredBondXp = Math.Max(0L, requiredBondXp);
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        ActivityKind = activityKind;
    }

    public string Id { get; }
    public long RequiredBondXp { get; }
    public string Title { get; }
    public string Description { get; }
    public CatActivityKind ActivityKind { get; }

    public bool IsReached(long bondXp) => bondXp >= RequiredBondXp;
}

/// <summary>
/// Canonical Bond milestone catalog. Rooms stay on Home Level; these gifts are
/// the Bond half of the care → play → decorate loop.
/// </summary>
public static class BondMilestoneService
{
    public const long MouseHuntBond = 35;
    public const long WindowWatchBond = 80;
    public const long FeatherPlayBond = 150;
    public const long BirdWatchBond = 250;

    public const string MouseHuntId = "bond.mouse-hunt";
    public const string WindowWatchId = "bond.window-watch";
    public const string FeatherPlayId = "bond.feather-play";
    public const string BirdWatchId = "bond.bird-watch";

    private static readonly BondMilestone[] MilestonesInternal =
    {
        new BondMilestone(
            FeatherPlayId,
            FeatherPlayBond,
            "Feather Frenzy",
            "Play with the bouncy feather toy after it is placed at home.",
            CatActivityKind.FeatherPlay),
        new BondMilestone(
            BirdWatchId,
            BirdWatchBond,
            "Bird Friends",
            "Watch the visiting garden birds from the courtyard tree.",
            CatActivityKind.BirdWatch)
    };

    public static IReadOnlyList<BondMilestone> Milestones => MilestonesInternal;
    public static int Count => MilestonesInternal.Length;

    public static int CountReached(long bondXp)
    {
        int count = 0;
        for (int i = 0; i < MilestonesInternal.Length; i++)
        {
            if (MilestonesInternal[i].IsReached(bondXp))
                count++;
        }

        return count;
    }

    public static bool HasReached(string milestoneId, long bondXp)
    {
        return TryGet(milestoneId, out BondMilestone milestone) &&
               milestone.IsReached(bondXp);
    }

    public static bool TryGet(string milestoneId, out BondMilestone milestone)
    {
        if (!string.IsNullOrWhiteSpace(milestoneId))
        {
            for (int i = 0; i < MilestonesInternal.Length; i++)
            {
                if (string.Equals(MilestonesInternal[i].Id, milestoneId, StringComparison.Ordinal))
                {
                    milestone = MilestonesInternal[i];
                    return true;
                }
            }
        }

        milestone = default;
        return false;
    }

    public static bool TryGetNext(long bondXp, out BondMilestone milestone)
    {
        for (int i = 0; i < MilestonesInternal.Length; i++)
        {
            if (!MilestonesInternal[i].IsReached(bondXp))
            {
                milestone = MilestonesInternal[i];
                return true;
            }
        }

        milestone = default;
        return false;
    }

    public static string FormatNextGiftLabel(long bondXp)
    {
        return TryGetNext(bondXp, out BondMilestone next)
            ? "Next bond gift: " + next.Title + " @ " + next.RequiredBondXp
            : "Bond gifts complete";
    }
}
