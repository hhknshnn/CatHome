using System;
using UnityEngine;

/// <summary>
/// Supplies time for the window's visual day/night cycle.
/// Device time is only a visual fallback and must never be treated as authoritative
/// for economy, needs, rewards, or other security-sensitive gameplay.
/// </summary>
public class GameTimeService : MonoBehaviour
{
    [Header("Visual Time Source")]
    [SerializeField] private bool useTestTime;
    [SerializeField, Range(0f, 24f)] private float testHour = 12f;
    [SerializeField] private int utcOffsetMinutes = 180;

    private DateTimeOffset trustedUtcTime;
    private double trustedTimeAppliedAt;
    private bool hasTrustedTime;

    public bool HasTrustedTime => hasTrustedTime;

    public DateTimeOffset CurrentLocalTime
    {
        get
        {
            if (useTestTime)
                return GetTestLocalTime();

            if (hasTrustedTime)
            {
                double elapsedSeconds = Math.Max(
                    0d,
                    Time.realtimeSinceStartupAsDouble - trustedTimeAppliedAt
                );

                return trustedUtcTime
                    .AddSeconds(elapsedSeconds)
                    .ToOffset(TimeSpan.FromMinutes(utcOffsetMinutes));
            }

            // Visual fallback only. Never use this value for authoritative gameplay.
            return DateTimeOffset.Now;
        }
    }

    public float CurrentHour
    {
        get
        {
            DateTimeOffset localTime = CurrentLocalTime;
            return localTime.Hour +
                   localTime.Minute / 60f +
                   localTime.Second / 3600f +
                   localTime.Millisecond / 3600000f;
        }
    }

    /// <summary>
    /// Anchors the service to a trusted UTC timestamp. From this point onward the
    /// timestamp advances using monotonic realtime rather than repeatedly reading
    /// the device clock.
    /// </summary>
    public void ApplyTrustedServerUnixTime(long unixSeconds, int utcOffsetMinutes)
    {
        try
        {
            trustedUtcTime = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToUniversalTime();
        }
        catch (ArgumentOutOfRangeException)
        {
            Debug.LogWarning("GameTimeService ignored an invalid trusted Unix timestamp.", this);
            return;
        }

        this.utcOffsetMinutes = Mathf.Clamp(utcOffsetMinutes, -14 * 60, 14 * 60);
        trustedTimeAppliedAt = Time.realtimeSinceStartupAsDouble;
        hasTrustedTime = true;
    }

    private DateTimeOffset GetTestLocalTime()
    {
        float normalizedHour = Mathf.Repeat(testHour, 24f);
        DateTime date = DateTime.SpecifyKind(
            DateTime.Today.AddHours(normalizedHour),
            DateTimeKind.Unspecified
        );
        int safeOffsetMinutes = Mathf.Clamp(utcOffsetMinutes, -14 * 60, 14 * 60);
        return new DateTimeOffset(date, TimeSpan.FromMinutes(safeOffsetMinutes));
    }

    private void OnValidate()
    {
        testHour = Mathf.Clamp(testHour, 0f, 24f);
        utcOffsetMinutes = Mathf.Clamp(utcOffsetMinutes, -14 * 60, 14 * 60);
    }
}
