using System;
using System.Globalization;
using UnityEngine;

[Serializable]
public sealed class MiniGameLivesSaveState
{
    public int lives;
    public string regenerationAnchorUtc;
    public string unlimitedUntilUtc;
    public int rewardedAdsClaimedToday;
    public string rewardedAdsDayUtc;

    public static MiniGameLivesSaveState CreateDefault(DateTime utcNow)
    {
        DateTime safeNow = MiniGameLivesService.ToSafeUtc(utcNow);
        return new MiniGameLivesSaveState
        {
            lives = MiniGameLivesService.MaximumLives,
            regenerationAnchorUtc = safeNow.ToString("O", CultureInfo.InvariantCulture),
            unlimitedUntilUtc = string.Empty,
            rewardedAdsClaimedToday = 0,
            rewardedAdsDayUtc = safeNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
    }
}

/// <summary>One persisted, offline-regenerating entry pool shared by all four mini-games.</summary>
public static class MiniGameLivesService
{
    public const int MaximumLives = 20;
    public const int RewardedAdLives = 2;
    public const int MaximumRewardedAdsPerUtcDay = 3;
    public static readonly TimeSpan RegenerationInterval = TimeSpan.FromMinutes(10d);

    private static int lives;
    private static DateTime regenerationAnchorUtc;
    private static DateTime unlimitedUntilUtc;
    private static DateTime rewardedAdsDayUtc;
    private static int rewardedAdsClaimedToday;
    private static bool initialized;

    public static MiniGameLivesSaveState FromLegacy(RunnerEnergySaveState runner, CatchLivesSaveState catcher, DateTime utcNow)
    {
        utcNow = ToSafeUtc(utcNow);
        var state = MiniGameLivesSaveState.CreateDefault(utcNow);
        DateTime runnerEnd, catchEnd;
        TryParseUtc(runner?.unlimitedUntilUtc, out runnerEnd);
        TryParseUtc(catcher?.unlimitedUntilUtc, out catchEnd);
        DateTime end = runnerEnd > catchEnd ? runnerEnd : catchEnd;
        if (end > utcNow) state.unlimitedUntilUtc = end.ToString("O", CultureInfo.InvariantCulture);
        string today = utcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        int runnerAds = runner?.rewardedAdsDayUtc == today ? Mathf.Clamp(runner.rewardedAdsClaimedToday, 0, MaximumRewardedAdsPerUtcDay) : 0;
        int catchAds = catcher?.rewardedAdsDayUtc == today ? Mathf.Clamp(catcher.rewardedAdsClaimedToday, 0, MaximumRewardedAdsPerUtcDay) : 0;
        state.rewardedAdsClaimedToday = Mathf.Min(MaximumRewardedAdsPerUtcDay, runnerAds + catchAds);
        return state;
    }

    public static event Action StateChanged;

    public static int CurrentLives
    {
        get
        {
            Refresh(DateTime.UtcNow);
            return lives;
        }
    }

    public static bool IsUnlimited => IsUnlimitedAt(DateTime.UtcNow);
    public static int RewardedAdsClaimedToday
    {
        get
        {
            Refresh(DateTime.UtcNow);
            return rewardedAdsClaimedToday;
        }
    }

    public static void EnsureInitialized() => EnsureInitialized(DateTime.UtcNow);

    public static bool CanStartRound() => CanStartRound(DateTime.UtcNow);
    public static bool CanClaimRewardedAd() => CanClaimRewardedAd(DateTime.UtcNow);
    public static bool TrySpendLife() => TrySpendLife(DateTime.UtcNow);
    public static bool TryGrantRewardedAd() => TryGrantRewardedAd(DateTime.UtcNow);
    public static TimeSpan TimeUntilNextLife() => TimeUntilNextLife(DateTime.UtcNow);
    public static void Refresh() => Refresh(DateTime.UtcNow);

    public static bool CanStartRound(DateTime utcNow)
    {
        Refresh(utcNow);
        return IsUnlimitedAt(utcNow) || lives > 0;
    }

    public static bool CanClaimRewardedAd(DateTime utcNow)
    {
        Refresh(utcNow);
        return !IsUnlimitedAt(utcNow) &&
               lives <= MaximumLives - RewardedAdLives &&
               rewardedAdsClaimedToday < MaximumRewardedAdsPerUtcDay;
    }

    public static bool TrySpendLife(DateTime utcNow)
    {
        utcNow = ToSafeUtc(utcNow);
        Refresh(utcNow);
        if (IsUnlimitedAt(utcNow))
            return true;
        if (lives <= 0)
            return false;

        bool wasFull = lives == MaximumLives;
        lives--;
        if (wasFull)
            regenerationAnchorUtc = utcNow;
        StateChanged?.Invoke();
        return true;
    }

    public static bool TryGrantRewardedAd(DateTime utcNow)
    {
        utcNow = ToSafeUtc(utcNow);
        if (!CanClaimRewardedAd(utcNow))
            return false;

        lives = Mathf.Min(MaximumLives, lives + RewardedAdLives);
        rewardedAdsClaimedToday++;
        if (lives == MaximumLives)
            regenerationAnchorUtc = utcNow;
        StateChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Store verification code may call this after validating a paid entitlement.
    /// Passing an expired entitlement is safe and never removes a longer one.
    /// </summary>
    public static bool ActivateUnlimitedUntil(DateTime entitlementEndUtc, DateTime utcNow)
    {
        utcNow = ToSafeUtc(utcNow);
        entitlementEndUtc = ToSafeUtc(entitlementEndUtc);
        Refresh(utcNow);
        if (entitlementEndUtc <= utcNow || entitlementEndUtc <= unlimitedUntilUtc)
            return false;

        unlimitedUntilUtc = entitlementEndUtc;
        StateChanged?.Invoke();
        return true;
    }

    public static bool ActivateSevenDayUnlimitedPass() =>
        ActivateUnlimitedUntil(DateTime.UtcNow.AddDays(7d), DateTime.UtcNow);

    public static bool IsUnlimitedAt(DateTime utcNow)
    {
        EnsureInitialized(utcNow);
        return unlimitedUntilUtc > ToSafeUtc(utcNow);
    }

    public static TimeSpan TimeUntilNextLife(DateTime utcNow)
    {
        utcNow = ToSafeUtc(utcNow);
        Refresh(utcNow);
        if (lives >= MaximumLives)
            return TimeSpan.Zero;

        TimeSpan elapsed = utcNow - regenerationAnchorUtc;
        if (elapsed < TimeSpan.Zero)
            return (regenerationAnchorUtc - utcNow) + RegenerationInterval;
        TimeSpan remaining = RegenerationInterval - elapsed;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public static MiniGameLivesSaveState CaptureState(DateTime utcNow)
    {
        utcNow = ToSafeUtc(utcNow);
        Refresh(utcNow);
        return new MiniGameLivesSaveState
        {
            lives = lives,
            regenerationAnchorUtc = regenerationAnchorUtc.ToString(
                "O", CultureInfo.InvariantCulture),
            unlimitedUntilUtc = unlimitedUntilUtc == default
                ? string.Empty
                : unlimitedUntilUtc.ToString("O", CultureInfo.InvariantCulture),
            rewardedAdsClaimedToday = rewardedAdsClaimedToday,
            rewardedAdsDayUtc = rewardedAdsDayUtc.ToString(
                "yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
    }

    public static void ApplySavedState(MiniGameLivesSaveState state, DateTime utcNow)
    {
        utcNow = ToSafeUtc(utcNow);
        initialized = true;
        lives = Mathf.Clamp(state?.lives ?? MaximumLives, 0, MaximumLives);
        regenerationAnchorUtc = TryParseUtc(state?.regenerationAnchorUtc, out DateTime anchor)
            ? anchor
            : utcNow;
        unlimitedUntilUtc = TryParseUtc(state?.unlimitedUntilUtc, out DateTime unlimited)
            ? unlimited
            : default;
        rewardedAdsDayUtc = TryParseDay(state?.rewardedAdsDayUtc, out DateTime adDay)
            ? adDay
            : utcNow.Date;
        rewardedAdsClaimedToday = Mathf.Clamp(
            state?.rewardedAdsClaimedToday ?? 0,
            0,
            MaximumRewardedAdsPerUtcDay);

        if (lives == MaximumLives)
            regenerationAnchorUtc = utcNow;
        NormalizeRewardedAdDay(utcNow);
        Refresh(utcNow);
        StateChanged?.Invoke();
    }

    public static void Refresh(DateTime utcNow)
    {
        utcNow = ToSafeUtc(utcNow);
        EnsureInitialized(utcNow);
        bool changed = NormalizeRewardedAdDay(utcNow);

        if (lives >= MaximumLives)
        {
            if (changed)
                StateChanged?.Invoke();
            return;
        }

        if (utcNow < regenerationAnchorUtc)
        {
            if (changed)
                StateChanged?.Invoke();
            return;
        }

        long ticks = (long)Math.Floor(
            (utcNow - regenerationAnchorUtc).TotalSeconds /
            RegenerationInterval.TotalSeconds);
        if (ticks <= 0)
        {
            if (changed)
                StateChanged?.Invoke();
            return;
        }

        int before = lives;
        lives = Mathf.Min(MaximumLives, lives + (int)Math.Min(MaximumLives, ticks));
        regenerationAnchorUtc = lives == MaximumLives
            ? utcNow
            : regenerationAnchorUtc.AddTicks(RegenerationInterval.Ticks * ticks);
        if (lives != before || changed)
            StateChanged?.Invoke();
    }

    internal static DateTime ToSafeUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
            return value;
        if (value.Kind == DateTimeKind.Local)
            return value.ToUniversalTime();
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static void EnsureInitialized(DateTime utcNow)
    {
        if (initialized)
            return;

        utcNow = ToSafeUtc(utcNow);
        initialized = true;
        lives = MaximumLives;
        regenerationAnchorUtc = utcNow;
        unlimitedUntilUtc = default;
        rewardedAdsDayUtc = utcNow.Date;
        rewardedAdsClaimedToday = 0;
    }

    private static bool NormalizeRewardedAdDay(DateTime utcNow)
    {
        if (rewardedAdsDayUtc.Date == utcNow.Date)
            return false;

        rewardedAdsDayUtc = utcNow.Date;
        rewardedAdsClaimedToday = 0;
        return true;
    }

    private static bool TryParseUtc(string value, out DateTime utc)
    {
        if (DateTime.TryParseExact(
                value,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out utc))
        {
            utc = ToSafeUtc(utc);
            return true;
        }

        utc = default;
        return false;
    }

    private static bool TryParseDay(string value, out DateTime day)
    {
        bool parsed = DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out day);
        if (parsed)
            day = ToSafeUtc(day).Date;
        return parsed;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        lives = MaximumLives;
        regenerationAnchorUtc = default;
        unlimitedUntilUtc = default;
        rewardedAdsDayUtc = default;
        rewardedAdsClaimedToday = 0;
        initialized = false;
        StateChanged = null;
    }
}
