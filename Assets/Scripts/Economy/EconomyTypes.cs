using System.Globalization;

namespace CatHome.Economy
{
    /// <summary>
    /// Where a transaction came from. Purely descriptive: it drives development
    /// logging and is carried on the change event so analytics or a future reward
    /// popup can tell a quest payout apart from a shop refund. It never changes
    /// how a transaction is validated.
    /// </summary>
    public enum EconomySource
    {
        Unknown = 0,
        Quest = 1,
        DailyQuest = 2,
        Achievement = 3,
        Shop = 4,
        Furniture = 5,
        RewardedAd = 6,
        Purchase = 7,
        PurchaseRestore = 8,
        Event = 9,
        Migration = 10,
        SaveLoad = 11,
        Debug = 12,
        CatRunner = 13,
        Onboarding = 14,
        CatCatch = 15,
        YarnRoute = 16,
        PondPlay = 17,
    }

    /// <summary>Outcome of a single economy transaction.</summary>
    public enum EconomyTransactionStatus
    {
        /// <summary>Balances (and payloads) were applied.</summary>
        Success = 0,

        /// <summary>Valid request that resolved to zero work; nothing changed.</summary>
        NoChange = 1,

        /// <summary>The transaction id was already settled; nothing changed.</summary>
        Duplicate = 2,

        /// <summary>At least one currency could not cover its cost; nothing changed.</summary>
        InsufficientFunds = 3,

        /// <summary>Negative, or otherwise unusable, amount; nothing changed.</summary>
        InvalidAmount = 4,

        /// <summary>A currency that is not registered in <see cref="CurrencyCatalog"/>.</summary>
        UnknownCurrency = 5,

        /// <summary>A non-currency reward line with no registered handler.</summary>
        UnsupportedPayload = 6,

        /// <summary>Nesting guard tripped; the economy refused to recurse further.</summary>
        Rejected = 7,
    }

    /// <summary>
    /// The result of a transaction. A struct so the hot paths allocate nothing,
    /// and detailed enough that a caller can distinguish "already paid" from
    /// "could not pay", which is the difference between repairing state and
    /// aborting an operation.
    /// </summary>
    public readonly struct EconomyTransactionResult
    {
        public EconomyTransactionResult(
            EconomyTransactionStatus status,
            string transactionId,
            bool clamped,
            string message)
        {
            Status = status;
            TransactionId = transactionId;
            Clamped = clamped;
            Message = message;
        }

        public EconomyTransactionStatus Status { get; }

        /// <summary>The idempotency key this transaction ran under, or null.</summary>
        public string TransactionId { get; }

        /// <summary>
        /// A balance hit <see cref="long.MaxValue"/> and was capped instead of
        /// wrapping. The transaction still succeeded.
        /// </summary>
        public bool Clamped { get; }

        public string Message { get; }

        /// <summary>True only when balances actually moved.</summary>
        public bool Succeeded => Status == EconomyTransactionStatus.Success;

        /// <summary>
        /// True when the caller may proceed as if the request is settled: it
        /// either applied now, was already applied under the same transaction id,
        /// or had nothing to apply. This is the check a claim/purchase flow wants
        /// before it flips its own state.
        /// </summary>
        public bool IsSettled =>
            Status == EconomyTransactionStatus.Success ||
            Status == EconomyTransactionStatus.Duplicate ||
            Status == EconomyTransactionStatus.NoChange;

        public override string ToString()
        {
            string id = string.IsNullOrEmpty(TransactionId) ? "-" : TransactionId;
            return string.IsNullOrEmpty(Message)
                ? $"{Status} (txn {id})"
                : $"{Status} (txn {id}): {Message}";
        }
    }

    /// <summary>
    /// One committed balance move, broadcast by
    /// <see cref="EconomyService.BalanceChanged"/>. It carries everything a UI
    /// needs to animate a delta without querying the service back.
    /// </summary>
    public readonly struct CurrencyBalanceChange
    {
        public CurrencyBalanceChange(
            CurrencyType currency,
            long previousBalance,
            long newBalance,
            EconomySource source,
            string transactionId)
        {
            Currency = currency;
            PreviousBalance = previousBalance;
            NewBalance = newBalance;
            Source = source;
            TransactionId = transactionId;
        }

        public CurrencyType Currency { get; }
        public long PreviousBalance { get; }
        public long NewBalance { get; }
        public EconomySource Source { get; }
        public string TransactionId { get; }

        /// <summary>Signed change. Positive for a grant, negative for a spend.</summary>
        public long Delta => NewBalance - PreviousBalance;

        public bool IsIncrease => NewBalance > PreviousBalance;

        public override string ToString()
        {
            string delta = Delta.ToString("+#;-#;0", CultureInfo.InvariantCulture);
            return $"{CurrencyCatalog.GetDisplayName(Currency)} {delta} " +
                   $"-> {NewBalance.ToString(CultureInfo.InvariantCulture)} ({Source})";
        }
    }

    /// <summary>
    /// Read-only answer to "what would this purchase do?". Produced by
    /// <see cref="EconomyService.PreviewPurchase"/> so a shop can render a price
    /// tag, a disabled buy button and a "you need N more" hint without touching a
    /// balance or running a transaction.
    /// </summary>
    public readonly struct PurchasePreview
    {
        public PurchasePreview(
            CurrencyType currency,
            long price,
            long balance,
            bool isValidPrice)
        {
            Currency = currency;
            Price = price;
            Balance = balance;
            IsValidPrice = isValidPrice;
        }

        public CurrencyType Currency { get; }
        public long Price { get; }
        public long Balance { get; }

        /// <summary>False for a negative price or an unregistered currency.</summary>
        public bool IsValidPrice { get; }

        public bool CanAfford => IsValidPrice && Balance >= Price;

        /// <summary>How much more the player needs; 0 when affordable.</summary>
        public long Missing => CanAfford || !IsValidPrice ? 0 : Price - Balance;

        /// <summary>Balance after the purchase, or the unchanged balance when unaffordable.</summary>
        public long BalanceAfter => CanAfford ? Balance - Price : Balance;
    }

    /// <summary>
    /// A product the player already owns, replayed through
    /// <see cref="EconomyService.RestorePurchaseRewards"/>. The transaction id is
    /// what makes a restore safe: an already granted product is skipped instead
    /// of paid a second time.
    /// </summary>
    public readonly struct PurchaseRecord
    {
        public PurchaseRecord(string productId, string transactionId, RewardBundle contents)
        {
            ProductId = productId;
            TransactionId = transactionId;
            Contents = contents;
        }

        public string ProductId { get; }
        public string TransactionId { get; }
        public RewardBundle Contents { get; }
    }

    /// <summary>
    /// Who writes the save after a transaction.
    /// </summary>
    public enum EconomyPersistence
    {
        /// <summary>
        /// The economy persists immediately. The default, and correct for any
        /// standalone transaction (shop, ad, IAP).
        /// </summary>
        Immediate = 0,

        /// <summary>
        /// The caller persists. Used when a currency move is only one half of a
        /// larger change — a quest claim also flips the quest to Claimed — so both
        /// halves land in a single save file write instead of two, and a crash
        /// between them can never persist the payout without the claim.
        /// </summary>
        DeferToCaller = 1,
    }
}
