using System;
using System.Globalization;
using UnityEngine;

[Serializable]
public sealed class RunnerEnergySaveState
{
    public int energy;
    public string regenerationAnchorUtc;
    public string unlimitedUntilUtc;
    public int rewardedAdsClaimedToday;
    public string rewardedAdsDayUtc;

    public static RunnerEnergySaveState CreateDefault(DateTime utcNow)
    {
        DateTime safeNow = RunnerEnergyService.ToSafeUtc(utcNow);
        return new RunnerEnergySaveState
        {
            energy = RunnerEnergyService.MaximumEnergy,
            regenerationAnchorUtc = safeNow.ToString("O", CultureInfo.InvariantCulture),
            unlimitedUntilUtc = string.Empty,
            rewardedAdsClaimedToday = 0,
            rewardedAdsDayUtc = safeNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
    }
}

/// <summary>
/// Owns Cat Runner entry energy. Runtime callers use UTC wrappers while tests
/// can provide a deterministic clock. Energy regenerates offline, is capped at
/// five, and an active unlimited entitlement bypasses spending without making
/// the runner invulnerable.
/// </summary>
public static class RunnerEnergyService
{
    public const int MaximumEnergy = 5;
    public const int RewardedAdEnergy = 2;
    public const int MaximumRewardedAdsPerUtcDay = 3;
    public static readonly TimeSpan RegenerationInterval = TimeSpan.FromMinutes(10d);

    private static int energy;
    private static DateTime regenerationAnchorUtc;
    private static DateTime unlimitedUntilUtc;
    private static DateTime rewardedAdsDayUtc;
    private static int rewardedAdsClaimedToday;
    private static bool initialized;

    public static event Action StateChanged;

    public static int CurrentEnergy
    {
        get
        {
            Refresh(DateTime.UtcNow);
            return energy;
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

    public static bool CanStartRun() => CanStartRun(DateTime.UtcNow);
    public static bool CanClaimRewardedAd() => CanClaimRewardedAd(DateTime.UtcNow);
    public static bool TrySpendRunEnergy() => TrySpendRunEnergy(DateTime.UtcNow);
    public static bool TryGrantRewardedAd() => TryGrantRewardedAd(DateTime.UtcNow);
    public static TimeSpan TimeUntilNextEnergy() => TimeUntilNextEnergy(DateTime.UtcNow);
    public static void Refresh() => Refresh(DateTime.UtcNow);

    public static bool CanStartRun(DateTime utcNow)
    {
        Refresh(utcNow);
        return IsUnlimitedAt(utcNow) || energy > 0;
    }

    public static bool CanClaimRewardedAd(DateTime utcNow)
    {
        Refresh(utcNow);
        return !IsUnlimitedAt(utcNow) &&
               energy <= MaximumEnergy - RewardedAdEnergy &&
               rewardedAdsClaimedToday < MaximumRewardedAdsPerUtcDay;
    }

    public static bool TrySpendRunEnergy(DateTime utcNow)
    {
        utcNow = ToSafeUtc(utcNow);
        Refresh(utcNow);
        if (IsUnlimitedAt(utcNow))
            return true;
        if (energy <= 0)
            return false;

        bool wasFull = energy == MaximumEnergy;
        energy--;
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

        energy = Mathf.Min(MaximumEnergy, energy + RewardedAdEnergy);
        rewardedAdsClaimedToday++;
        if (energy == MaximumEnergy)
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

    public static TimeSpan TimeUntilNextEnergy(DateTime utcNow)
    {
        utcNow = ToSafeUtc(utcNow);
        Refresh(utcNow);
        if (energy >= MaximumEnergy)
            return TimeSpan.Zero;

        TimeSpan elapsed = utcNow - regenerationAnchorUtc;
        if (elapsed < TimeSpan.Zero)
            return (regenerationAnchorUtc - utcNow) + RegenerationInterval;
        TimeSpan remaining = RegenerationInterval - elapsed;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public static RunnerEnergySaveState CaptureState(DateTime utcNow)
    {
        utcNow = ToSafeUtc(utcNow);
        Refresh(utcNow);
        return new RunnerEnergySaveState
        {
            energy = energy,
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

    public static void ApplySavedState(RunnerEnergySaveState state, DateTime utcNow)
    {
        utcNow = ToSafeUtc(utcNow);
        initialized = true;
        energy = Mathf.Clamp(state?.energy ?? MaximumEnergy, 0, MaximumEnergy);
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

        if (energy == MaximumEnergy)
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

        if (energy >= MaximumEnergy)
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

        int before = energy;
        energy = Mathf.Min(MaximumEnergy, energy + (int)Math.Min(int.MaxValue, ticks));
        regenerationAnchorUtc = energy == MaximumEnergy
            ? utcNow
            : regenerationAnchorUtc.AddTicks(RegenerationInterval.Ticks * ticks);
        if (energy != before || changed)
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
        energy = MaximumEnergy;
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
        energy = MaximumEnergy;
        regenerationAnchorUtc = default;
        unlimitedUntilUtc = default;
        rewardedAdsDayUtc = default;
        rewardedAdsClaimedToday = 0;
        initialized = false;
        StateChanged = null;
    }
}
