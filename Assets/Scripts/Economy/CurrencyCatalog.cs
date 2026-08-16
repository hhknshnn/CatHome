using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace CatHome.Economy
{
    /// <summary>
    /// Static description of one currency. The <see cref="SaveKey"/> is the only
    /// value that ever reaches disk, which is what lets the enum member be
    /// renamed or reordered later without touching player saves.
    /// </summary>
    public readonly struct CurrencyDefinition
    {
        public CurrencyDefinition(
            CurrencyType type,
            string saveKey,
            string displayName,
            bool isPremium)
        {
            Type = type;
            SaveKey = saveKey;
            DisplayName = displayName;
            IsPremium = isPremium;
        }

        public CurrencyType Type { get; }

        /// <summary>Stable, save-facing identifier. Must never change.</summary>
        public string SaveKey { get; }

        public string DisplayName { get; }

        /// <summary>Hard currency (bought with real money) rather than earned.</summary>
        public bool IsPremium { get; }

        public bool IsValid => Type != CurrencyType.None && !string.IsNullOrEmpty(SaveKey);
    }

    /// <summary>
    /// The registry of every currency the game knows about. It is the single
    /// place that turns a <see cref="CurrencyType"/> into something the save file
    /// and the UI can use, so <see cref="EconomyService"/> itself contains no
    /// per-currency special cases.
    /// </summary>
    public static class CurrencyCatalog
    {
        // Registration order is also the natural display order.
        private static readonly CurrencyDefinition[] Definitions =
        {
            new CurrencyDefinition(CurrencyType.Coin, "coin", "Coins", false),
            new CurrencyDefinition(CurrencyType.Diamond, "diamond", "Diamonds", true),
        };

        private static readonly ReadOnlyCollection<CurrencyDefinition> ReadOnlyDefinitions =
            Array.AsReadOnly(Definitions);

        private static readonly Dictionary<CurrencyType, CurrencyDefinition> ByType =
            new Dictionary<CurrencyType, CurrencyDefinition>();

        private static readonly Dictionary<string, CurrencyDefinition> BySaveKey =
            new Dictionary<string, CurrencyDefinition>(StringComparer.Ordinal);

        static CurrencyCatalog()
        {
            foreach (CurrencyDefinition definition in Definitions)
            {
                if (!definition.IsValid)
                {
                    Debug.LogError(
                        "CurrencyCatalog contains an invalid currency definition " +
                        $"({definition.Type}); it is ignored."
                    );
                    continue;
                }

                if (!ByType.TryAdd(definition.Type, definition))
                {
                    Debug.LogError(
                        $"CurrencyCatalog contains duplicate definitions for {definition.Type}; " +
                        "the first one wins."
                    );
                    continue;
                }

                if (!BySaveKey.TryAdd(definition.SaveKey, definition))
                {
                    Debug.LogError(
                        $"CurrencyCatalog reuses the save key '{definition.SaveKey}'. " +
                        "Save keys must be unique or balances will collide on disk."
                    );
                }
            }
        }

        /// <summary>Every registered currency, in display order.</summary>
        public static IReadOnlyList<CurrencyDefinition> All => ReadOnlyDefinitions;

        public static int Count => ReadOnlyDefinitions.Count;

        /// <summary>False for <see cref="CurrencyType.None"/> and for any value that is not registered.</summary>
        public static bool IsSupported(CurrencyType type) => ByType.ContainsKey(type);

        public static bool TryGet(CurrencyType type, out CurrencyDefinition definition)
        {
            return ByType.TryGetValue(type, out definition);
        }

        /// <summary>
        /// Resolves a currency from a saved key. Returns false for an unknown key,
        /// which happens when a save was written by a newer build that already had
        /// a currency this build does not know; the caller keeps the raw entry
        /// instead of discarding the player's balance.
        /// </summary>
        public static bool TryGetBySaveKey(string saveKey, out CurrencyDefinition definition)
        {
            if (string.IsNullOrEmpty(saveKey))
            {
                definition = default;
                return false;
            }

            return BySaveKey.TryGetValue(saveKey, out definition);
        }

        public static string GetSaveKey(CurrencyType type)
        {
            return ByType.TryGetValue(type, out CurrencyDefinition definition)
                ? definition.SaveKey
                : null;
        }

        public static string GetDisplayName(CurrencyType type)
        {
            return ByType.TryGetValue(type, out CurrencyDefinition definition)
                ? definition.DisplayName
                : type.ToString();
        }
    }
}
