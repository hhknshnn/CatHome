using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persisted cat identity: the player's chosen name and coat colour. The name is
/// the SAME value the first-launch onboarding sets (PlayerPrefs key
/// <see cref="PetTutorialHint.CatNameKey"/>), so the intro naming and the CAT
/// JOURNAL panel are one source of truth — renaming in the journal updates the
/// tutorial's name and vice versa. The coat is a separate PlayerPrefs slot. Nothing
/// here touches the game save schema.
///
/// The coat tint multiplies the cat's white base material, so index 0 ("Classic")
/// leaves the authored grey tabby untouched and the others wash it in a soft pastel.
/// </summary>
public static class CatIdentityService
{
    public readonly struct CatCoat
    {
        public CatCoat(string name, Color tint)
        {
            Name = name;
            Tint = tint;
        }

        public string Name { get; }
        public Color Tint { get; }
    }

    // Shared with the onboarding naming flow — do not change without migrating it.
    private static string NameKey => PetTutorialHint.CatNameKey;
    private const string CoatKey = "cat.identity.coat";
    private const string FallbackDisplayName = "MELO";
    public const int MaxNameLength = 14;

    private static readonly CatCoat[] Coats =
    {
        new CatCoat("CLASSIC", new Color(1.00f, 1.00f, 1.00f)),
        new CatCoat("CREAM", new Color(1.00f, 0.93f, 0.80f)),
        new CatCoat("PEACH", new Color(1.00f, 0.82f, 0.66f)),
        new CatCoat("ROSY", new Color(1.00f, 0.78f, 0.83f)),
        new CatCoat("MINT", new Color(0.78f, 1.00f, 0.86f)),
        new CatCoat("SKY", new Color(0.76f, 0.90f, 1.00f)),
        new CatCoat("LILAC", new Color(0.86f, 0.80f, 1.00f)),
        new CatCoat("BUTTER", new Color(1.00f, 0.95f, 0.70f))
    };

    /// <summary>Raised whenever the name or coat changes so live views/appearance react.</summary>
    public static event Action Changed;

    public static IReadOnlyList<CatCoat> Palette => Coats;
    public static int CoatCount => Coats.Length;

    /// <summary>
    /// The stored name, read live so it always matches whatever the onboarding wrote.
    /// Empty means the cat has not been named yet.
    /// </summary>
    public static string CatName
    {
        get => CatDialogueView.NormalizeName(PlayerPrefs.GetString(NameKey, string.Empty));
        set
        {
            string clean = CatDialogueView.NormalizeName(value);
            if (string.Equals(clean, CatName, StringComparison.Ordinal))
                return;
            if (string.IsNullOrEmpty(clean))
                PlayerPrefs.DeleteKey(NameKey);
            else
                PlayerPrefs.SetString(NameKey, clean);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    /// <summary>Name for UI display; falls back to a friendly default before naming.</summary>
    public static string DisplayName
    {
        get
        {
            string name = CatName;
            return string.IsNullOrEmpty(name) ? FallbackDisplayName : name;
        }
    }

    public static int CoatIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(CoatKey, 0), 0, Coats.Length - 1);
        set
        {
            int clamped = Mathf.Clamp(value, 0, Coats.Length - 1);
            if (clamped == CoatIndex)
                return;
            PlayerPrefs.SetInt(CoatKey, clamped);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public static Color CurrentTint => Coats[CoatIndex].Tint;

    public static void ResetForNewGame()
    {
        PlayerPrefs.DeleteKey(NameKey);
        PlayerPrefs.DeleteKey(CoatKey);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
