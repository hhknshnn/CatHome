using System;
using UnityEngine;

/// <summary>
/// Legacy brightness preference retained so older saves and serialized callbacks keep loading.
/// The selector was removed from the home HUD; lighting now uses one canonical multiplier.
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
    public const float CanonicalMultiplier = 1.10f;
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

    public static float Multiplier => CanonicalMultiplier;

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
