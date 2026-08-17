using System;
using UnityEngine;

/// <summary>
/// Serialized Home progression slice (save version 10). Only the cumulative Home
/// XP is stored; the Home Level is always derived from it through
/// <see cref="HomeProgressionService.LevelForXp"/>, so a curve retune never needs
/// a save migration and can never disagree with the stored value.
/// </summary>
[Serializable]
public sealed class HomeProgressionSaveState
{
    public int homeProgressionVersion = HomeProgressionService.SaveVersion;
    public long homeXp;

    public static HomeProgressionSaveState CreateDefault()
    {
        return new HomeProgressionSaveState
        {
            homeProgressionVersion = HomeProgressionService.SaveVersion,
            homeXp = 0L
        };
    }
}

/// <summary>
/// Home XP and the derived Home Level, following the same static, event-driven
/// pattern as <see cref="ProgressionService"/>: no scene object, reset on domain
/// reload, restored by <see cref="CatHomeSaveSystem"/>.
///
/// Home XP is progression earned from home improvements, not a spendable
/// currency, so it never lives in <see cref="CatHome.Economy.EconomyService"/>.
/// It is granted when a home-store product is acquired (see
/// <see cref="HomeStoreService"/>), which is the canonical "improve your home"
/// action from the roadmap.
///
/// This phase deliberately keeps Home Level additive: it is a surfaced
/// progression resource and does NOT yet gate rooms or products. Room access
/// stays owned by <see cref="HomeStoreService"/> ownership and collection
/// completion, exactly as before, so no existing gating behaviour changes.
/// </summary>
public static class HomeProgressionService
{
    public const int SaveVersion = 1;

    /// <summary>
    /// Coin value of home improvements needed to reach one level past the
    /// previous. Cumulative XP for level L is <c>PerLevelStep * (L-1) * L / 2</c>,
    /// i.e. the step grows by one unit each level. Tunable at runtime because the
    /// level is always re-derived from the stored cumulative XP.
    /// </summary>
    public const long PerLevelStep = 1000L;

    /// <summary>Guards the level derivation loop against runaway / overflow input.</summary>
    public const int MaxLevel = 99;

    private static long homeXp;

    public static long HomeXp => homeXp;

    /// <summary>Current Home Level, always derived from <see cref="HomeXp"/>.</summary>
    public static int HomeLevel => LevelForXp(homeXp);

    /// <summary>XP accumulated inside the current level (0 at each level-up).</summary>
    public static long XpIntoCurrentLevel => homeXp - CumulativeXpForLevel(HomeLevel);

    /// <summary>
    /// XP span of the current level. Zero once <see cref="MaxLevel"/> is reached,
    /// where there is no next level to fill toward.
    /// </summary>
    public static long XpForCurrentLevel
    {
        get
        {
            int level = HomeLevel;
            if (level >= MaxLevel)
                return 0L;

            return CumulativeXpForLevel(level + 1) - CumulativeXpForLevel(level);
        }
    }

    /// <summary>Raised after any Home XP or Home Level change. UI/debug only.</summary>
    public static event Action Changed;

    /// <summary>
    /// Adds Home XP earned from a home improvement. Ignores non-positive amounts
    /// (a free preview product grants nothing) and saturates instead of
    /// overflowing. It deliberately does NOT persist: callers such as
    /// <see cref="HomeStoreService"/> already SaveNow immediately after the
    /// acquisition, so the grant and the ownership change land in one file write.
    /// </summary>
    /// <param name="amount">Home XP to add; the acquired product's coin value.</param>
    /// <param name="reasonId">Optional source id for the debug log.</param>
    public static void GrantHomeXp(long amount, string reasonId = null)
    {
        if (amount <= 0L)
            return;

        int previousLevel = LevelForXp(homeXp);
        homeXp = amount > long.MaxValue - homeXp ? long.MaxValue : homeXp + amount;
        int newLevel = LevelForXp(homeXp);

        if (newLevel > previousLevel)
        {
            Debug.Log(
                $"HomeProgressionService: Home Level {previousLevel} -> {newLevel} " +
                $"(Home XP {homeXp})" +
                (string.IsNullOrEmpty(reasonId) ? "." : $" from '{reasonId}'."));
        }

        RaiseChanged();
    }

    /// <summary>
    /// Highest level whose cumulative XP requirement is satisfied by
    /// <paramref name="xp"/>. Level 1 needs 0 XP, so a fresh home is level 1.
    /// </summary>
    public static int LevelForXp(long xp)
    {
        if (xp <= 0L)
            return 1;

        int level = 1;
        while (level < MaxLevel && CumulativeXpForLevel(level + 1) <= xp)
            level++;

        return level;
    }

    /// <summary>
    /// Cumulative Home XP required to have reached <paramref name="level"/>.
    /// Level 1 is the 0-XP floor. Growth is triangular so each level asks for a
    /// slightly larger home investment than the last.
    /// </summary>
    public static long CumulativeXpForLevel(int level)
    {
        if (level <= 1)
            return 0L;

        int clamped = level > MaxLevel ? MaxLevel : level;
        long span = clamped - 1L;
        // PerLevelStep * (1 + 2 + ... + (level-1)); the /2 stays exact because
        // span * (span + 1) is always even.
        return PerLevelStep * (span * (span + 1L) / 2L);
    }

    public static HomeProgressionSaveState CaptureState()
    {
        return new HomeProgressionSaveState
        {
            homeProgressionVersion = SaveVersion,
            homeXp = homeXp
        };
    }

    /// <summary>
    /// Restores saved Home XP. A null or negative value safely resets to zero.
    /// Pure state restore: never grants XP and never raises a level-up log.
    /// </summary>
    public static void ApplySavedState(HomeProgressionSaveState state)
    {
        long loaded = state != null ? state.homeXp : 0L;
        homeXp = loaded < 0L ? 0L : loaded;
        RaiseChanged();
    }

    private static void RaiseChanged()
    {
        Action handlers = Changed;
        if (handlers == null)
            return;

        foreach (Action handler in handlers.GetInvocationList())
        {
            try
            {
                handler();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        homeXp = 0L;
        Changed = null;
    }
}
