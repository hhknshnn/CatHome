using System;
using UnityEngine;

/// <summary>
/// Player-owned, persistent brightness preference. The time-of-day controllers remain the
/// source of all authored lighting values; they only multiply those values by this setting.
/// </summary>
public static class PlayerLightingPreference
{
    public enum LightLevel
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public const string PlayerPrefsKey = "Player_LightLevel";
    public static event Action<LightLevel> LevelChanged;

    private static bool loaded;
    private static LightLevel current;

    public static LightLevel Current
    {
        get
        {
            if (!loaded)
            {
                int stored = PlayerPrefs.GetInt(PlayerPrefsKey, (int)LightLevel.Medium);
                current = (LightLevel)Mathf.Clamp(
                    stored,
                    (int)LightLevel.Low,
                    (int)LightLevel.High
                );
                loaded = true;
            }
            return current;
        }
    }

    public static float Multiplier
    {
        get
        {
            switch (Current)
            {
                case LightLevel.Low: return 0.85f;
                case LightLevel.High: return 1.30f;
                default: return 1.10f;
            }
        }
    }

    /// <summary>
    /// Applies and persists an explicit level. Integer enum values are intentionally unchanged,
    /// so every Player_LightLevel value written by earlier builds remains compatible.
    /// </summary>
    public static void SetLevel(LightLevel level)
    {
        LightLevel safeLevel = (LightLevel)Mathf.Clamp(
            (int)level,
            (int)LightLevel.Low,
            (int)LightLevel.High
        );

        bool changed = !loaded || Current != safeLevel;
        current = safeLevel;
        loaded = true;
        PlayerPrefs.SetInt(PlayerPrefsKey, (int)safeLevel);
        PlayerPrefs.Save();

        if (changed)
            LevelChanged?.Invoke(safeLevel);
    }

    /// <summary>Legacy API retained for callers from older scenes.</summary>
    public static LightLevel Cycle()
    {
        LightLevel next = (LightLevel)(((int)Current + 1) % 3);
        SetLevel(next);
        return next;
    }
}
