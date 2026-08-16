using System;
using System.Globalization;
using UnityEngine;

[Serializable]
public sealed class CatchLivesSaveState
{
    public int lives;
    public string regenerationAnchorUtc;
    public string unlimitedUntilUtc;
    public int rewardedAdsClaimedToday;
    public string rewardedAdsDayUtc;
    public bool tutorialCompleted;

    public static CatchLivesSaveState CreateDefault(DateTime utcNow)
    {
        DateTime safeNow = CatchLivesService.ToSafeUtc(utcNow);
        return new CatchLivesSaveState
        {
            lives = CatchLivesService.MaximumLives,
            regenerationAnchorUtc = safeNow.ToString("O", CultureInfo.InvariantCulture),
            unlimitedUntilUtc = string.Empty,
            rewardedAdsClaimedToday = 0,
            rewardedAdsDayUtc = safeNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            tutorialCompleted = false
        };
    }
}

/// <summary>
/// Independent Cat Catch entry lives. The pool is capped at five, regenerates
/// offline, and never shares state with Cat Runner energy.
/// </summary>
public static class CatchLivesService
{
    public const int MaximumLives = 5;
    public const int RewardedAdLives = 2;
    public const int MaximumRewardedAdsPerUtcDay = 3;
    public static readonly TimeSpan RegenerationInterval = TimeSpan.FromMinutes(10d);

    private static int lives;
    private static DateTime regenerationAnchorUtc;
    private static DateTime unlimitedUntilUtc;
    private static DateTime rewardedAdsDayUtc;
    private static int rewardedAdsClaimedToday;
    private static bool tutorialCompleted;
    private static bool initialized;

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
    public static bool TutorialCompleted
    {
        get
        {
            EnsureInitialized();
            return tutorialCompleted;
        }
    }
    public static int RewardedAdsClaimedToday
    {
        get
        {
            Refresh(DateTime.UtcNow);
            return rewardedAdsClaimedToday;
        }
    }

    public static void EnsureInitialized() => EnsureInitialized(DateTime.UtcNow);
    public static bool CanStartHunt() => CanStartHunt(DateTime.UtcNow);
    public static bool CanClaimRewardedAd() => CanClaimRewardedAd(DateTime.UtcNow);
    public static bool TrySpendHuntLife() => TrySpendHuntLife(DateTime.UtcNow);
    public static bool TryGrantRewardedAd() => TryGrantRewardedAd(DateTime.UtcNow);
    public static TimeSpan TimeUntilNextLife() => TimeUntilNextLife(DateTime.UtcNow);
    public static void Refresh() => Refresh(DateTime.UtcNow);

    public static bool CanStartHunt(DateTime utcNow)
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

    public static bool TrySpendHuntLife(DateTime utcNow)
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

    public static CatchLivesSaveState CaptureState(DateTime utcNow)
    {
        utcNow = ToSafeUtc(utcNow);
        Refresh(utcNow);
        return new CatchLivesSaveState
        {
            lives = lives,
            regenerationAnchorUtc = regenerationAnchorUtc.ToString(
                "O", CultureInfo.InvariantCulture),
            unlimitedUntilUtc = unlimitedUntilUtc == default
                ? string.Empty
                : unlimitedUntilUtc.ToString("O", CultureInfo.InvariantCulture),
            rewardedAdsClaimedToday = rewardedAdsClaimedToday,
            rewardedAdsDayUtc = rewardedAdsDayUtc.ToString(
                "yyyy-MM-dd", CultureInfo.InvariantCulture),
            tutorialCompleted = tutorialCompleted
        };
    }

    public static void ApplySavedState(CatchLivesSaveState state, DateTime utcNow)
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
        tutorialCompleted = state?.tutorialCompleted ?? false;

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
        lives = Mathf.Min(MaximumLives, lives + (int)Math.Min(int.MaxValue, ticks));
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
        tutorialCompleted = false;
    }

    public static bool CompleteTutorial()
    {
        EnsureInitialized();
        if (tutorialCompleted)
            return false;

        tutorialCompleted = true;
        StateChanged?.Invoke();
        return true;
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
        tutorialCompleted = false;
        initialized = false;
        StateChanged = null;
    }
}
