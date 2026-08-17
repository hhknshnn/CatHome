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
/// This phase keeps Home Level additive, and Home Level is now the progression
/// source used when room and product features apply lock requirements.
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

    /// <summary>
    /// Checks if the current Home Level satisfies the requested minimum.
    /// Minimum level is clamped to 1 so Level 1 is never treated as a lock.
    /// </summary>
    public static bool MeetsHomeLevelRequirement(int requiredLevel)
    {
        int minimum = requiredLevel < 1 ? 1 : requiredLevel;
        return HomeLevel >= minimum;
    }

    /// <summary>Raised after any Home XP or Home Level change. UI/debug only.</summary>
    public static event Action Changed;

    /// <summary>
    /// Raised once per level gained, with the new Home Level, when a
    /// <see cref="GrantHomeXp"/> crosses one or more thresholds. Only real
    /// gameplay grants fire it; <see cref="ApplySavedState"/> and
    /// <see cref="EnsureFloor"/> restore state silently so a load never pops the
    /// celebration.
    /// </summary>
    public static event Action<int> LeveledUp;

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
            RaiseLeveledUp(newLevel);
        }

        RaiseChanged();
    }

    /// <summary>
    /// Raises Home XP to at least <paramref name="minXp"/>, never lowering it.
    /// Used at load to back-fill progression for products acquired before the Home
    /// XP save slice existed: their purchase-time grant predates the field, so the
    /// value is recovered from current ownership. A no-op once the saved XP already
    /// covers what is owned.
    /// </summary>
    public static void EnsureFloor(long minXp)
    {
        if (minXp <= homeXp)
            return;

        homeXp = minXp;
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

    private static void RaiseLeveledUp(int newLevel)
    {
        Action<int> handlers = LeveledUp;
        if (handlers == null)
            return;

        foreach (Action<int> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(newLevel);
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
        LeveledUp = null;
    }
}
