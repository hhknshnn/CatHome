namespace CatHome.Economy
{
    /// <summary>
    /// Strongly typed identifier for every player-facing currency. Deliberately
    /// not a string: an unknown identifier can never be typed by accident, and a
    /// switch over currencies is exhaustive at compile time.
    ///
    /// Adding a currency is exactly two edits — a member here and a matching
    /// <see cref="CurrencyDefinition"/> in <see cref="CurrencyCatalog"/>. Nothing
    /// else in the economy layer hardcodes a currency.
    ///
    /// The numeric values are never persisted: saves are keyed by
    /// <see cref="CurrencyDefinition.SaveKey"/>, so members may be renamed or
    /// reordered without breaking an existing save.
    /// </summary>
    public enum CurrencyType
    {
        /// <summary>
        /// The default(CurrencyType) value. Never a valid transaction target; the
        /// economy rejects it with <see cref="EconomyTransactionStatus.UnknownCurrency"/>
        /// so an uninitialized field can not silently move real money.
        /// </summary>
        None = 0,

        Coin = 1,
        Diamond = 2,

        // Future currencies (Ticket, SeasonToken, EventToken, ...) are added
        // here with the next free value and registered in CurrencyCatalog.
    }
}
