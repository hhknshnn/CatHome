using System;

namespace CatHome.Economy
{
    /// <summary>
    /// One persisted balance. Keyed by <see cref="CurrencyDefinition.SaveKey"/>
    /// rather than by the enum value, so renaming or reordering
    /// <see cref="CurrencyType"/> can never shuffle a player's money.
    /// </summary>
    [Serializable]
    public sealed class CurrencyBalanceEntry
    {
        public string currencyKey;
        public long amount;

        public CurrencyBalanceEntry()
        {
        }

        public CurrencyBalanceEntry(string currencyKey, long amount)
        {
            this.currencyKey = currencyKey;
            this.amount = amount;
        }

        public CurrencyBalanceEntry Clone() => new CurrencyBalanceEntry(currencyKey, amount);
    }

    /// <summary>
    /// The economy section of the existing save file. It is embedded in
    /// CatHomeSaveData — there is no second save file — and carries its own small
    /// version number so the economy can evolve without spending a save version
    /// of the whole game.
    ///
    /// Serialized by JsonUtility, so this is a plain class with public fields.
    /// </summary>
    [Serializable]
    public sealed class EconomySaveState
    {
        public int economyVersion;

        /// <summary>Every known balance. A missing currency loads as 0.</summary>
        public CurrencyBalanceEntry[] balances;

        /// <summary>
        /// Recently settled transaction ids, oldest first. Bounded (see
        /// <see cref="EconomyService.MaxTrackedTransactionIds"/>) so the save can
        /// not grow without limit; it is a second line of defence against a
        /// duplicate payout, never the only one.
        /// </summary>
        public string[] processedTransactionIds;

        public EconomySaveState()
        {
            economyVersion = EconomyService.SaveVersion;
            balances = Array.Empty<CurrencyBalanceEntry>();
            processedTransactionIds = Array.Empty<string>();
        }

        public bool HasBalances => balances != null && balances.Length > 0;

        public EconomySaveState Clone()
        {
            var clone = new EconomySaveState { economyVersion = economyVersion };

            if (balances != null)
            {
                clone.balances = new CurrencyBalanceEntry[balances.Length];
                for (int i = 0; i < balances.Length; i++)
                    clone.balances[i] = balances[i]?.Clone();
            }

            if (processedTransactionIds != null)
                clone.processedTransactionIds = (string[])processedTransactionIds.Clone();

            return clone;
        }
    }
}
