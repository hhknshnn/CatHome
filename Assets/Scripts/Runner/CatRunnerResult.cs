using System;
using UnityEngine;

/// <summary>
/// Immutable summary of one Cat Runner attempt. It is the only payload allowed
/// to cross from the runner scene into the persistent Cat Home economy.
/// </summary>
public readonly struct CatRunnerResult
{
    private CatRunnerResult(
        string runId,
        float durationSeconds,
        float distance,
        int coinsCollected,
        int collisions,
        int careBonusPercent,
        long bonusCoins,
        long totalCoins)
    {
        RunId = runId;
        DurationSeconds = durationSeconds;
        Distance = distance;
        CoinsCollected = coinsCollected;
        Collisions = collisions;
        CareBonusPercent = careBonusPercent;
        BonusCoins = bonusCoins;
        TotalCoins = totalCoins;
    }

    public string RunId { get; }
    public float DurationSeconds { get; }
    public float Distance { get; }
    public int CoinsCollected { get; }
    public int Collisions { get; }
    public int CareBonusPercent { get; }
    public long BonusCoins { get; }
    public long TotalCoins { get; }

    public static CatRunnerResult Create(
        string runId,
        float durationSeconds,
        float distance,
        int coinsCollected,
        int collisions,
        int careBonusPercent)
    {
        string safeId = string.IsNullOrWhiteSpace(runId)
            ? Guid.NewGuid().ToString("N")
            : runId.Trim();
        int safeCoins = Mathf.Max(0, coinsCollected);
        int safeBonusPercent = Mathf.Clamp(careBonusPercent, 0, 100);
        long bonus = (long)Math.Round(
            safeCoins * (safeBonusPercent / 100d),
            MidpointRounding.AwayFromZero);
        long total = safeCoins > long.MaxValue - bonus
            ? long.MaxValue
            : safeCoins + bonus;

        return new CatRunnerResult(
            safeId,
            Mathf.Max(0f, durationSeconds),
            Mathf.Max(0f, distance),
            safeCoins,
            Mathf.Max(0, collisions),
            safeBonusPercent,
            bonus,
            total);
    }
}
