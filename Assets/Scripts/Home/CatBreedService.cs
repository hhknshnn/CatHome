using System;
using UnityEngine;

/// <summary>Small PlayerPrefs-backed selection seam for the player's cat breed.</summary>
public static class CatBreedService
{
    private const string BreedKey = "cat.identity.breed.v1";

    public static event Action Changed;

    public static string SelectedBreedId
    {
        get
        {
            string stored = PlayerPrefs.GetString(BreedKey, CatBreedCatalog.DefaultBreedId);
            CatBreedCatalog catalog = CatBreedCatalog.Load();
            return catalog == null || catalog.Find(stored) != null
                ? stored
                : CatBreedCatalog.DefaultBreedId;
        }
    }

    public static bool Select(string breedId)
    {
        CatBreedCatalog catalog = CatBreedCatalog.Load();
        if (catalog == null || catalog.Find(breedId) == null)
            return false;
        if (string.Equals(SelectedBreedId, breedId, StringComparison.Ordinal))
            return true;

        PlayerPrefs.SetString(BreedKey, breedId);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }

    public static CatBreedCatalog.Entry SelectedEntry
    {
        get
        {
            CatBreedCatalog catalog = CatBreedCatalog.Load();
            return catalog?.Find(SelectedBreedId) ?? catalog?.Get(0);
        }
    }

    public static void ResetForNewGame()
    {
        PlayerPrefs.DeleteKey(BreedKey);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
