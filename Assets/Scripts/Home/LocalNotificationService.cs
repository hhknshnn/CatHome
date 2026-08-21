using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Local-notification seam. Editor and tests record scheduled toasts; a real
/// mobile provider can subscribe later. Missing permission or no provider is
/// silent — never throws, never grants currency.
/// </summary>
public static class LocalNotificationService
{
    public const string EnergyFullId = "notify.energy-full";
    public const string DailyReadyId = "notify.daily-ready";
    public const string OfflineRewardId = "notify.offline-reward";

    public readonly struct ScheduledNotice
    {
        public ScheduledNotice(string id, string title, DateTime fireUtc)
        {
            Id = id;
            Title = title;
            FireUtc = fireUtc;
        }

        public string Id { get; }
        public string Title { get; }
        public DateTime FireUtc { get; }
    }

    private static readonly List<ScheduledNotice> Pending =
        new List<ScheduledNotice>(4);

    public static event Action<ScheduledNotice> Simulated;
    public static IReadOnlyList<ScheduledNotice> Scheduled => Pending;

    public static void RefreshSchedules(DateTime utcNow)
    {
        utcNow = utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime();
        Pending.Clear();

        RunnerEnergySaveState energy = RunnerEnergyService.CaptureState(utcNow);
        if (!RunnerEnergyService.IsUnlimitedAt(utcNow) &&
            energy.energy < RunnerEnergyService.MaximumEnergy)
        {
            DateTime ready = utcNow + RunnerEnergyService.TimeUntilNextEnergy(utcNow);
            Enqueue(EnergyFullId, "RUNNER ENERGY IS READY!", ready);
        }

        DateTime nextMidnight = utcNow.Date.AddDays(1);
        Enqueue(DailyReadyId, "YOUR DAILY GIFTS ARE WAITING!", nextMidnight);

        Enqueue(OfflineRewardId, "COME BACK — GIFTS ARE PILING UP!", utcNow.AddHours(4));
    }

    public static void NotifyAppPaused(DateTime utcNow)
    {
        RefreshSchedules(utcNow);
    }

    public static void Clear()
    {
        Pending.Clear();
    }

    private static void Enqueue(string id, string title, DateTime fireUtc)
    {
        var notice = new ScheduledNotice(id, title, fireUtc);
        Pending.Add(notice);
        Simulated?.Invoke(notice);
#if UNITY_EDITOR
        Debug.Log("[CatHome Notify] " + id + " @ " + fireUtc.ToString("u") + " — " + title);
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        Pending.Clear();
        Simulated = null;
    }
}
