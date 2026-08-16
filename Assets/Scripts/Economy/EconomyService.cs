using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CatHome.Economy
{
    /// <summary>
    /// The single authority for every player currency in Cat Home.
    ///
    /// Nothing outside this class may hold or mutate a balance: quests, the shop,
    /// rewarded ads, IAP and future events all go through the public API below,
    /// which validates, applies atomically, persists through the existing
    /// <c>CatHomeSaveSystem</c> and then broadcasts what changed.
    ///
    /// Shape: a static service with no scene presence, mirroring
    /// <c>CatHomeSaveSystem</c> and <c>ProgressionService</c>. The mutable state
    /// is private — callers only ever see read-only balances and immutable result
    /// structs — and it is wiped on domain reload by the SubsystemRegistration
    /// hook at the bottom, so entering Play Mode always starts from a clean slate.
    ///
    /// Guarantees:
    /// <list type="bullet">
    /// <item>Atomic. A transaction is validated in full against a staged copy and
    /// only then committed, so a multi-currency reward or price can never apply
    /// half-way.</item>
    /// <item>Never negative. A spend that a balance cannot cover is rejected
    /// outright; no path can drive a balance below zero.</item>
    /// <item>Overflow-safe. A grant that would pass <see cref="long.MaxValue"/> is
    /// capped and reported as clamped instead of wrapping into a negative.</item>
    /// <item>Idempotent on request. A transaction carrying an id is settled at
    /// most once, so a retried grant cannot pay twice.</item>
    /// <item>Exception-free at the boundary. Unknown currencies, corrupt saves and
    /// throwing subscribers are all handled and logged, never thrown.</item>
    /// </list>
    ///
    /// It knows nothing about UI. Views subscribe to <see cref="BalanceChanged"/>.
    /// </summary>
    public static class EconomyService
    {
        /// <summary>Version of the economy save section, not of the save file.</summary>
        public const int SaveVersion = 1;

        /// <summary>
        /// How many settled transaction ids are remembered and persisted. Bounded
        /// so the save file cannot grow forever; the oldest id is evicted first.
        /// Duplicate protection therefore covers recent retries (a re-sent ad
        /// callback, a restored purchase, a double-tapped claim), while the real
        /// long-term guard stays the caller's own state — a quest that is already
        /// Claimed is never claimed again regardless of this window.
        /// </summary>
        public const int MaxTrackedTransactionIds = 64;

        /// <summary>
        /// How deep transactions may nest. A subscriber is allowed to react to a
        /// balance change with another transaction (a grant that triggers a
        /// milestone, for example), but a handler that feeds itself is stopped
        /// here instead of overflowing the stack.
        /// </summary>
        private const int MaxTransactionDepth = 8;

        private static readonly Dictionary<CurrencyType, long> Balances =
            new Dictionary<CurrencyType, long>();

        // Insertion-ordered id window: the list keeps eviction order, the set
        // keeps the lookup O(1). They are always mutated together.
        private static readonly List<string> ProcessedIdOrder = new List<string>(MaxTrackedTransactionIds);
        private static readonly HashSet<string> ProcessedIds = new HashSet<string>(StringComparer.Ordinal);

        private static readonly Dictionary<RewardPayloadKind, Action<RewardItem>> PayloadHandlers =
            new Dictionary<RewardPayloadKind, Action<RewardItem>>();

        private static bool seeded;
        private static bool saveLoaded;
        private static bool unloadedSpendWarningLogged;
        private static int transactionDepth;

        /// <summary>
        /// Raised once per currency that moved, after the change is committed and
        /// persisted. Subscribers are invoked individually inside a try/catch, so
        /// a throwing view can never roll back or block a payout.
        /// </summary>
        public static event Action<CurrencyBalanceChange> BalanceChanged;

        /// <summary>
        /// Convenience signal for views that simply repaint everything. Raised
        /// once per transaction, after the per-currency events.
        /// </summary>
        public static event Action AnyBalanceChanged;

        /// <summary>
        /// True once a save (or an explicit legacy restore) has been applied. A
        /// spend attempted before this happens is still safe — balances are 0 —
        /// but it is warned about once, because it almost always means a caller
        /// ran ahead of CatHomeSaveSystem.
        /// </summary>
        public static bool HasLoadedSave => saveLoaded;

        // ----- Reading -----

        public static long Coins => GetBalance(CurrencyType.Coin);

        public static long Diamonds => GetBalance(CurrencyType.Diamond);

        /// <summary>
        /// Current balance, or 0 for a currency this build does not know. Pure
        /// read: it never creates state and never throws.
        /// </summary>
        public static long GetBalance(CurrencyType currency)
        {
            EnsureSeeded();
            return Balances.TryGetValue(currency, out long balance) ? balance : 0L;
        }

        /// <summary>True when the balance covers <paramref name="amount"/>.</summary>
        public static bool HasEnough(CurrencyType currency, long amount)
        {
            if (amount <= 0)
                return true;

            return CurrencyCatalog.IsSupported(currency) && GetBalance(currency) >= amount;
        }

        /// <summary>Shop-facing alias of <see cref="HasEnough"/>.</summary>
        public static bool CanAfford(CurrencyType currency, long price) => HasEnough(currency, price);

        /// <summary>
        /// True when every currency line of <paramref name="price"/> is covered.
        /// Non-currency lines are ignored: a price is currencies only.
        /// </summary>
        public static bool CanAfford(RewardBundle price)
        {
            if (price == null || price.IsEmpty)
                return true;

            IReadOnlyList<RewardItem> lines = price.Items;
            for (int i = 0; i < lines.Count; i++)
            {
                RewardItem line = lines[i];
                if (!line.IsCurrency)
                    continue;

                if (!HasEnough(line.Currency, line.Amount))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Everything a shop row needs to render a price without mutating
        /// anything: affordability, the shortfall and the resulting balance.
        /// </summary>
        public static PurchasePreview PreviewPurchase(CurrencyType currency, long price)
        {
            bool valid = price >= 0 && CurrencyCatalog.IsSupported(currency);
            return new PurchasePreview(currency, price, GetBalance(currency), valid);
        }

        // ----- Granting -----

        /// <summary>
        /// Adds a positive amount of one currency.
        /// </summary>
        /// <param name="transactionId">
        /// Optional idempotency key. When supplied, a repeat of the same id is
        /// reported as <see cref="EconomyTransactionStatus.Duplicate"/> and
        /// changes nothing.
        /// </param>
        public static EconomyTransactionResult AddCurrency(
            CurrencyType currency,
            long amount,
            EconomySource source = EconomySource.Unknown,
            string transactionId = null,
            EconomyPersistence persistence = EconomyPersistence.Immediate)
        {
            if (amount < 0)
            {
                return Reject(
                    EconomyTransactionStatus.InvalidAmount,
                    transactionId,
                    $"AddCurrency was called with a negative amount ({Format(amount)} " +
                    $"{CurrencyCatalog.GetDisplayName(currency)}). Use TrySpend to remove currency."
                );
            }

            return Execute(
                OneDelta(currency, amount),
                null,
                source,
                transactionId,
                persistence,
                "grant"
            );
        }

        /// <summary>
        /// Pays out a whole reward bundle as one transaction: either every line
        /// lands or none does. Non-currency lines (bond XP today; furniture and
        /// items later) are routed to their registered payload handler, and a
        /// line with no handler rejects the whole bundle rather than paying a
        /// partial reward.
        /// </summary>
        public static EconomyTransactionResult GrantReward(
            RewardBundle reward,
            EconomySource source = EconomySource.Unknown,
            string transactionId = null,
            EconomyPersistence persistence = EconomyPersistence.Immediate)
        {
            if (reward == null || reward.IsEmpty)
                return NoChange(transactionId);

            List<CurrencyDelta> deltas = null;
            List<RewardItem> payloads = null;

            IReadOnlyList<RewardItem> lines = reward.Items;
            for (int i = 0; i < lines.Count; i++)
            {
                RewardItem line = lines[i];
                if (line.IsCurrency)
                {
                    deltas ??= new List<CurrencyDelta>(lines.Count);
                    deltas.Add(new CurrencyDelta(line.Currency, line.Amount));
                }
                else
                {
                    payloads ??= new List<RewardItem>(lines.Count);
                    payloads.Add(line);
                }
            }

            return Execute(deltas, payloads, source, transactionId, persistence, "grant");
        }

        /// <summary>
        /// Pays several bundles as a single atomic transaction, so a level-up that
        /// awards a quest reward plus a milestone bonus cannot half-apply. The
        /// bundles are merged first, which also merges repeated currencies.
        /// </summary>
        public static EconomyTransactionResult GrantMultipleRewards(
            IReadOnlyList<RewardBundle> rewards,
            EconomySource source = EconomySource.Unknown,
            string transactionId = null,
            EconomyPersistence persistence = EconomyPersistence.Immediate)
        {
            if (rewards == null || rewards.Count == 0)
                return NoChange(transactionId);

            RewardBundle combined = RewardBundle.Empty;
            for (int i = 0; i < rewards.Count; i++)
                combined = combined.CombinedWith(rewards[i]);

            return GrantReward(combined, source, transactionId, persistence);
        }

        /// <summary>
        /// Overwrites a balance outright. Intended for debug tooling and for
        /// authoritative restores; ordinary gameplay uses
        /// <see cref="AddCurrency"/> or <see cref="TrySpend"/> so the delta stays
        /// meaningful to listeners.
        /// </summary>
        public static EconomyTransactionResult SetBalance(
            CurrencyType currency,
            long value,
            EconomySource source = EconomySource.Debug)
        {
            if (value < 0)
            {
                return Reject(
                    EconomyTransactionStatus.InvalidAmount,
                    null,
                    $"SetBalance refused a negative value ({Format(value)} " +
                    $"{CurrencyCatalog.GetDisplayName(currency)}). Balances can never be negative."
                );
            }

            if (!CurrencyCatalog.IsSupported(currency))
                return RejectUnknownCurrency(currency, null);

            long delta = value - GetBalance(currency);
            if (delta == 0)
                return NoChange(null);

            return Execute(
                OneDelta(currency, delta),
                null,
                source,
                null,
                EconomyPersistence.Immediate,
                "set"
            );
        }

        // ----- Spending -----

        /// <summary>
        /// Removes <paramref name="amount"/> of one currency, but only if the
        /// balance covers it in full. Nothing changes on failure.
        /// </summary>
        public static EconomyTransactionResult TrySpend(
            CurrencyType currency,
            long amount,
            EconomySource source = EconomySource.Shop,
            string transactionId = null,
            EconomyPersistence persistence = EconomyPersistence.Immediate)
        {
            if (amount < 0)
            {
                return Reject(
                    EconomyTransactionStatus.InvalidAmount,
                    transactionId,
                    $"TrySpend was called with a negative amount ({Format(amount)} " +
                    $"{CurrencyCatalog.GetDisplayName(currency)}). Use AddCurrency to grant currency."
                );
            }

            WarnIfSpendingBeforeLoad();
            return Execute(
                OneDelta(currency, -amount),
                null,
                source,
                transactionId,
                persistence,
                "spend"
            );
        }

        /// <summary>
        /// Spends a multi-currency price atomically: either every line is
        /// deducted or none is. Non-currency lines are ignored.
        /// </summary>
        public static EconomyTransactionResult TrySpend(
            RewardBundle price,
            EconomySource source = EconomySource.Shop,
            string transactionId = null,
            EconomyPersistence persistence = EconomyPersistence.Immediate)
        {
            if (price == null || price.IsEmpty)
                return NoChange(transactionId);

            List<CurrencyDelta> deltas = null;
            IReadOnlyList<RewardItem> lines = price.Items;
            for (int i = 0; i < lines.Count; i++)
            {
                RewardItem line = lines[i];
                if (!line.IsCurrency)
                    continue;

                deltas ??= new List<CurrencyDelta>(lines.Count);
                deltas.Add(new CurrencyDelta(line.Currency, -line.Amount));
            }

            WarnIfSpendingBeforeLoad();
            return Execute(deltas, null, source, transactionId, persistence, "spend");
        }

        /// <summary>Shop-facing shorthand. True only when the coins were deducted.</summary>
        public static bool TrySpendCoins(
            long amount,
            EconomySource source = EconomySource.Shop,
            string transactionId = null)
        {
            return TrySpend(CurrencyType.Coin, amount, source, transactionId).Succeeded;
        }

        /// <summary>Shop-facing shorthand. True only when the diamonds were deducted.</summary>
        public static bool TrySpendDiamonds(
            long amount,
            EconomySource source = EconomySource.Shop,
            string transactionId = null)
        {
            return TrySpend(CurrencyType.Diamond, amount, source, transactionId).Succeeded;
        }

        // ----- Rewarded ad hook (no ad SDK, no platform code) -----

        /// <summary>
        /// Entry point a rewarded-ad integration will call once its SDK reports a
        /// completed view. Supplying the ad network's own impression id as
        /// <paramref name="transactionId"/> is what makes a re-delivered callback
        /// safe: the second delivery reports Duplicate and pays nothing.
        /// </summary>
        public static EconomyTransactionResult GrantRewardedAdReward(
            RewardBundle reward,
            string placementId = null,
            string transactionId = null)
        {
            LogDevelopment(
                "EconomyService: rewarded ad payout requested for placement " +
                $"'{placementId ?? "-"}' ({Describe(reward)})."
            );
            return GrantReward(reward, EconomySource.RewardedAd, transactionId);
        }

        // ----- IAP hooks (no Unity IAP, no store code) -----

        /// <summary>
        /// Entry point an IAP integration will call after the store confirms a
        /// purchase. This layer never talks to a store, never validates a receipt
        /// and knows no product catalog; it only settles the contents.
        /// </summary>
        public static EconomyTransactionResult GrantPurchasedProduct(
            string productId,
            RewardBundle contents,
            string transactionId = null)
        {
            LogDevelopment(
                $"EconomyService: purchase payout requested for product '{productId ?? "-"}' " +
                $"({Describe(contents)})."
            );
            return GrantReward(contents, EconomySource.Purchase, transactionId);
        }

        /// <summary>
        /// Generic bundle entry point for any future granting system that does
        /// not fit one of the named hooks.
        /// </summary>
        public static EconomyTransactionResult GrantRewardBundle(
            RewardBundle bundle,
            EconomySource source = EconomySource.Unknown,
            string transactionId = null)
        {
            return GrantReward(bundle, source, transactionId);
        }

        /// <summary>
        /// Replays owned products after a reinstall or a store restore. Each
        /// record settles under its own transaction id, so products that were
        /// already granted are skipped rather than paid twice.
        /// </summary>
        /// <returns>How many records actually moved a balance.</returns>
        public static int RestorePurchaseRewards(IReadOnlyList<PurchaseRecord> records)
        {
            if (records == null || records.Count == 0)
                return 0;

            int granted = 0;
            for (int i = 0; i < records.Count; i++)
            {
                PurchaseRecord record = records[i];
                EconomyTransactionResult result = GrantReward(
                    record.Contents,
                    EconomySource.PurchaseRestore,
                    record.TransactionId
                );

                if (result.Succeeded)
                    granted++;
                else if (result.Status == EconomyTransactionStatus.Duplicate)
                    LogDevelopment($"EconomyService: product '{record.ProductId}' was already restored.");
            }

            LogDevelopment(
                $"EconomyService: restore settled {granted.ToString(CultureInfo.InvariantCulture)} of " +
                $"{records.Count.ToString(CultureInfo.InvariantCulture)} product(s)."
            );
            return granted;
        }

        // ----- Non-currency reward payloads -----

        /// <summary>
        /// Registers the handler for a non-currency reward kind. Bond XP is
        /// registered by ProgressionService; furniture and items will register
        /// theirs the same way. Keeping this a registry rather than a hard
        /// reference is what lets the economy stay unaware of progression,
        /// inventory and UI.
        ///
        /// Replacing an existing handler is allowed but reported, because two
        /// systems claiming the same reward kind is almost always a mistake.
        /// </summary>
        public static void RegisterPayloadHandler(RewardPayloadKind kind, Action<RewardItem> handler)
        {
            if (kind == RewardPayloadKind.Currency)
            {
                Debug.LogWarning(
                    "EconomyService: currency rewards are settled by the economy itself and " +
                    "can not have a payload handler."
                );
                return;
            }

            if (handler == null)
                return;

            if (PayloadHandlers.TryGetValue(kind, out Action<RewardItem> existing) &&
                existing != handler)
            {
                Debug.LogWarning(
                    $"EconomyService replaced the existing {kind} reward handler. " +
                    "Only one system should own a reward kind."
                );
            }

            PayloadHandlers[kind] = handler;
        }

        public static void UnregisterPayloadHandler(RewardPayloadKind kind, Action<RewardItem> handler)
        {
            if (handler != null &&
                PayloadHandlers.TryGetValue(kind, out Action<RewardItem> existing) &&
                existing == handler)
            {
                PayloadHandlers.Remove(kind);
            }
        }

        /// <summary>
        /// True when a bundle containing this kind can currently be granted.
        /// Currency is always supported; everything else needs a handler.
        /// </summary>
        public static bool IsPayloadSupported(RewardPayloadKind kind)
        {
            return kind == RewardPayloadKind.Currency || PayloadHandlers.ContainsKey(kind);
        }

        // ----- Save integration (one save file, owned by CatHomeSaveSystem) -----

        /// <summary>
        /// Snapshot for the save system. Pure read: it never mutates balances and
        /// never triggers a write.
        /// </summary>
        public static EconomySaveState CaptureState()
        {
            EnsureSeeded();

            var state = new EconomySaveState
            {
                economyVersion = SaveVersion,
                balances = new CurrencyBalanceEntry[Balances.Count],
                processedTransactionIds = ProcessedIdOrder.ToArray(),
            };

            int index = 0;
            foreach (KeyValuePair<CurrencyType, long> pair in Balances)
            {
                string key = CurrencyCatalog.GetSaveKey(pair.Key);
                if (key == null)
                    continue;

                state.balances[index++] = new CurrencyBalanceEntry(key, pair.Value);
            }

            // Only if a currency vanished from the catalog between seeding and
            // capture, which cannot happen at runtime but keeps the array tight.
            if (index != state.balances.Length)
                Array.Resize(ref state.balances, index);

            return state;
        }

        /// <summary>
        /// Restores balances from the save. Defensive by design: a null or empty
        /// section leaves the current balances alone instead of zeroing them, so a
        /// missing or partially written economy section can never wipe a wallet.
        /// Negative and unknown entries are repaired, not discarded.
        ///
        /// This is a pure state restore: no transaction is recorded and no save is
        /// written. Change events are raised so any live UI repaints.
        /// </summary>
        public static void ApplySavedState(EconomySaveState state)
        {
            EnsureSeeded();

            if (state == null || !state.HasBalances)
            {
                // Expected on a first run and on a save migrated from a version
                // that predates the economy section; CatHomeSaveSystem has
                // already seeded the legacy coin/diamond values by then.
                saveLoaded = true;
                LogDevelopment(
                    "EconomyService: the save carried no economy section; the current balances " +
                    "were kept and nothing was reset."
                );
                return;
            }

            if (state.economyVersion > SaveVersion)
            {
                Debug.LogWarning(
                    $"EconomyService loaded an economy section from a newer version " +
                    $"({state.economyVersion.ToString(CultureInfo.InvariantCulture)} > " +
                    $"{SaveVersion.ToString(CultureInfo.InvariantCulture)}). Known currencies are " +
                    "read as usual; anything unrecognized is ignored."
                );
            }

            var changes = new List<CurrencyBalanceChange>(state.balances.Length);
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (CurrencyBalanceEntry entry in state.balances)
            {
                if (entry == null || string.IsNullOrEmpty(entry.currencyKey))
                {
                    Debug.LogWarning("EconomyService skipped a saved balance without a currency key.");
                    continue;
                }

                if (!seenKeys.Add(entry.currencyKey))
                {
                    Debug.LogWarning(
                        $"EconomyService skipped a duplicate saved balance for '{entry.currencyKey}'; " +
                        "the first entry wins."
                    );
                    continue;
                }

                if (!CurrencyCatalog.TryGetBySaveKey(entry.currencyKey, out CurrencyDefinition definition))
                {
                    // A save written by a build that knows more currencies than
                    // this one. Dropping it silently would look like theft, so it
                    // is reported; the value stays in the file because the save
                    // system rewrites the whole section from CaptureState only
                    // for currencies this build owns.
                    Debug.LogWarning(
                        $"EconomyService ignored the unknown saved currency '{entry.currencyKey}'. " +
                        "This build does not define it."
                    );
                    continue;
                }

                long amount = entry.amount;
                if (amount < 0)
                {
                    Debug.LogWarning(
                        $"EconomyService repaired a negative saved balance for " +
                        $"{definition.DisplayName} ({Format(amount)} -> 0)."
                    );
                    amount = 0;
                }

                long previous = GetBalance(definition.Type);
                Balances[definition.Type] = amount;

                if (previous != amount)
                {
                    changes.Add(new CurrencyBalanceChange(
                        definition.Type,
                        previous,
                        amount,
                        EconomySource.SaveLoad,
                        null
                    ));
                }
            }

            RestoreProcessedIds(state.processedTransactionIds);
            saveLoaded = true;

            LogDevelopment(
                $"EconomyService loaded balances: {DescribeBalances()} " +
                $"({ProcessedIdOrder.Count.ToString(CultureInfo.InvariantCulture)} tracked transaction id(s))."
            );

            Broadcast(changes);
        }

        /// <summary>
        /// Seeds balances from the pre-economy save fields (save versions 1-4,
        /// which stored coins and diamonds as bare numbers). Called by the
        /// migration path before <see cref="ApplySavedState"/>, so a real economy
        /// section always wins over the legacy mirror.
        ///
        /// Like the load path it is a pure restore: no transaction, no save write.
        /// </summary>
        public static void ApplyLegacyBalances(long coins, long diamonds)
        {
            EnsureSeeded();

            // The settled-transaction window belongs to the same snapshot as the
            // balances it protects. Restoring balances without clearing it would
            // let a stale id block a legitimate payout after a progression reset.
            // ApplySavedState refills the window straight afterwards when the save
            // actually carries one.
            RestoreProcessedIds(null);

            var changes = new List<CurrencyBalanceChange>(2);
            ApplyLegacyBalance(CurrencyType.Coin, coins, changes);
            ApplyLegacyBalance(CurrencyType.Diamond, diamonds, changes);
            saveLoaded = true;

            Broadcast(changes);
        }

        private static void ApplyLegacyBalance(
            CurrencyType currency,
            long amount,
            List<CurrencyBalanceChange> changes)
        {
            if (amount < 0)
                amount = 0;

            long previous = GetBalance(currency);
            if (previous == amount)
                return;

            Balances[currency] = amount;
            changes.Add(new CurrencyBalanceChange(
                currency,
                previous,
                amount,
                EconomySource.SaveLoad,
                null
            ));
        }

        // ----- Transaction core -----

        private readonly struct CurrencyDelta
        {
            public CurrencyDelta(CurrencyType currency, long delta)
            {
                Currency = currency;
                Delta = delta;
            }

            public CurrencyType Currency { get; }
            public long Delta { get; }
        }

        /// <summary>
        /// The one and only mutation path. Validates the whole request against a
        /// staged copy of the balances, and only when every line passes does it
        /// commit, persist and broadcast. Any rejection returns before a single
        /// balance is touched.
        /// </summary>
        private static EconomyTransactionResult Execute(
            List<CurrencyDelta> deltas,
            List<RewardItem> payloads,
            EconomySource source,
            string transactionId,
            EconomyPersistence persistence,
            string verb)
        {
            EnsureSeeded();

            bool hasDeltas = deltas != null && deltas.Count > 0;
            bool hasPayloads = payloads != null && payloads.Count > 0;
            if (!hasDeltas && !hasPayloads)
                return NoChange(transactionId);

            if (transactionDepth >= MaxTransactionDepth)
            {
                // Reported as an error rather than a warning: reaching this means
                // a listener is feeding transactions back into the economy, which
                // is a logic bug that needs fixing, not a runtime condition.
                Debug.LogError(
                    "EconomyService refused a transaction because economy calls are nested " +
                    $"{MaxTransactionDepth.ToString(CultureInfo.InvariantCulture)} deep. " +
                    "A balance-change listener is very likely starting new transactions."
                );
                return new EconomyTransactionResult(
                    EconomyTransactionStatus.Rejected,
                    transactionId,
                    false,
                    "Maximum economy transaction depth reached."
                );
            }

            bool tracked = !string.IsNullOrEmpty(transactionId);
            if (tracked && ProcessedIds.Contains(transactionId))
            {
                LogDevelopment(
                    $"EconomyService prevented a duplicate transaction '{transactionId}' " +
                    $"({source}). Nothing changed."
                );
                return new EconomyTransactionResult(
                    EconomyTransactionStatus.Duplicate,
                    transactionId,
                    false,
                    "This transaction id was already settled."
                );
            }

            // --- Validation phase: nothing is written until all of it passes ---

            if (hasPayloads)
            {
                for (int i = 0; i < payloads.Count; i++)
                {
                    RewardItem payload = payloads[i];
                    if (!payload.IsValid)
                    {
                        return Reject(
                            EconomyTransactionStatus.InvalidAmount,
                            transactionId,
                            $"The reward line '{payload}' is not a valid payout."
                        );
                    }

                    if (!PayloadHandlers.ContainsKey(payload.Kind))
                    {
                        return Reject(
                            EconomyTransactionStatus.UnsupportedPayload,
                            transactionId,
                            $"No handler is registered for {payload.Kind} rewards, so the whole " +
                            "reward was rejected instead of paying out only part of it."
                        );
                    }
                }
            }

            Dictionary<CurrencyType, long> staged = null;
            bool clamped = false;

            if (hasDeltas)
            {
                staged = new Dictionary<CurrencyType, long>(deltas.Count);
                for (int i = 0; i < deltas.Count; i++)
                {
                    CurrencyDelta delta = deltas[i];
                    if (delta.Delta == 0)
                        continue;

                    if (!CurrencyCatalog.IsSupported(delta.Currency))
                        return RejectUnknownCurrency(delta.Currency, transactionId);

                    if (delta.Delta == long.MinValue)
                    {
                        // Cannot be negated safely, and no legitimate caller can
                        // produce it; rejected rather than clamped so the bug is
                        // visible.
                        return Reject(
                            EconomyTransactionStatus.InvalidAmount,
                            transactionId,
                            $"A {CurrencyCatalog.GetDisplayName(delta.Currency)} change of " +
                            "long.MinValue is not representable."
                        );
                    }

                    // Reads the staged value first so several lines targeting the
                    // same currency accumulate correctly inside one transaction.
                    long current = staged.TryGetValue(delta.Currency, out long stagedValue)
                        ? stagedValue
                        : GetBalance(delta.Currency);

                    long next;
                    if (delta.Delta < 0)
                    {
                        long cost = -delta.Delta;
                        if (current < cost)
                        {
                            LogDevelopment(
                                $"EconomyService rejected a {verb}: " +
                                $"{Format(cost)} {CurrencyCatalog.GetDisplayName(delta.Currency)} " +
                                $"needed, {Format(current)} available ({source})."
                            );
                            return new EconomyTransactionResult(
                                EconomyTransactionStatus.InsufficientFunds,
                                transactionId,
                                false,
                                $"{Format(cost - current)} more " +
                                $"{CurrencyCatalog.GetDisplayName(delta.Currency)} needed."
                            );
                        }

                        next = current - cost;
                    }
                    else if (delta.Delta > long.MaxValue - current)
                    {
                        // Capped instead of wrapped: wrapping would turn a huge
                        // balance negative, which every other guarantee here
                        // exists to prevent.
                        next = long.MaxValue;
                        clamped = true;
                        Debug.LogWarning(
                            $"EconomyService capped {CurrencyCatalog.GetDisplayName(delta.Currency)} " +
                            "at long.MaxValue to avoid an overflow."
                        );
                    }
                    else
                    {
                        next = current + delta.Delta;
                    }

                    staged[delta.Currency] = next;
                }

                if (staged.Count == 0 && !hasPayloads)
                    return NoChange(transactionId);
            }

            // --- Commit phase: from here nothing can fail the transaction ---

            transactionDepth++;
            try
            {
                List<CurrencyBalanceChange> changes = null;
                if (staged != null && staged.Count > 0)
                {
                    changes = new List<CurrencyBalanceChange>(staged.Count);
                    foreach (KeyValuePair<CurrencyType, long> pair in staged)
                    {
                        long previous = GetBalance(pair.Key);
                        if (previous == pair.Value)
                            continue;

                        Balances[pair.Key] = pair.Value;
                        changes.Add(new CurrencyBalanceChange(
                            pair.Key,
                            previous,
                            pair.Value,
                            source,
                            transactionId
                        ));
                    }
                }

                if (tracked)
                    RecordTransactionId(transactionId);

                if (hasPayloads)
                    ApplyPayloads(payloads);

                LogTransaction(verb, source, transactionId, changes, payloads);

                // Persisted before the broadcast, so a listener that reads the
                // save (or a listener that throws) can never observe a state the
                // file does not already contain.
                if (persistence == EconomyPersistence.Immediate)
                    CatHomeSaveSystem.SaveNow();

                Broadcast(changes);

                return new EconomyTransactionResult(
                    EconomyTransactionStatus.Success,
                    transactionId,
                    clamped,
                    null
                );
            }
            finally
            {
                transactionDepth--;
            }
        }

        /// <summary>
        /// Hands the non-currency lines to their handlers. Currencies are already
        /// committed at this point, so a handler that throws is logged and the
        /// transaction still stands: the alternative — silently taking a paid
        /// reward back — would be worse for the player and harder to diagnose.
        /// </summary>
        private static void ApplyPayloads(List<RewardItem> payloads)
        {
            for (int i = 0; i < payloads.Count; i++)
            {
                RewardItem payload = payloads[i];
                if (!PayloadHandlers.TryGetValue(payload.Kind, out Action<RewardItem> handler))
                    continue;

                try
                {
                    handler(payload);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"EconomyService: the {payload.Kind} reward handler threw while applying " +
                        $"'{payload}'. The currency part of the transaction stands."
                    );
                    Debug.LogException(exception);
                }
            }
        }

        private static void RecordTransactionId(string transactionId)
        {
            if (!ProcessedIds.Add(transactionId))
                return;

            ProcessedIdOrder.Add(transactionId);

            while (ProcessedIdOrder.Count > MaxTrackedTransactionIds)
            {
                ProcessedIds.Remove(ProcessedIdOrder[0]);
                ProcessedIdOrder.RemoveAt(0);
            }
        }

        private static void RestoreProcessedIds(string[] savedIds)
        {
            ProcessedIds.Clear();
            ProcessedIdOrder.Clear();

            if (savedIds == null)
                return;

            foreach (string id in savedIds)
            {
                if (string.IsNullOrEmpty(id))
                    continue;

                RecordTransactionId(id);
            }
        }

        /// <summary>
        /// Notifies listeners. Every subscriber is invoked in its own try/catch so
        /// one broken view can neither block the rest nor unwind a committed
        /// transaction.
        /// </summary>
        private static void Broadcast(List<CurrencyBalanceChange> changes)
        {
            if (changes == null || changes.Count == 0)
                return;

            Action<CurrencyBalanceChange> perCurrency = BalanceChanged;
            if (perCurrency != null)
            {
                Delegate[] handlers = perCurrency.GetInvocationList();
                for (int i = 0; i < changes.Count; i++)
                {
                    foreach (Delegate handler in handlers)
                    {
                        try
                        {
                            ((Action<CurrencyBalanceChange>)handler)(changes[i]);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception);
                        }
                    }
                }
            }

            Action any = AnyBalanceChanged;
            if (any == null)
                return;

            foreach (Delegate handler in any.GetInvocationList())
            {
                try
                {
                    ((Action)handler)();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        // ----- Helpers -----

        /// <summary>
        /// Puts every catalog currency in the dictionary at 0 exactly once, so a
        /// read never has to decide what a missing key means.
        /// </summary>
        private static void EnsureSeeded()
        {
            if (seeded)
                return;

            seeded = true;
            IReadOnlyList<CurrencyDefinition> definitions = CurrencyCatalog.All;
            for (int i = 0; i < definitions.Count; i++)
                Balances[definitions[i].Type] = 0L;
        }

        private static List<CurrencyDelta> OneDelta(CurrencyType currency, long delta)
        {
            return new List<CurrencyDelta>(1) { new CurrencyDelta(currency, delta) };
        }

        private static EconomyTransactionResult NoChange(string transactionId)
        {
            return new EconomyTransactionResult(
                EconomyTransactionStatus.NoChange,
                transactionId,
                false,
                null
            );
        }

        private static EconomyTransactionResult Reject(
            EconomyTransactionStatus status,
            string transactionId,
            string message)
        {
            Debug.LogWarning("EconomyService rejected a transaction: " + message);
            return new EconomyTransactionResult(status, transactionId, false, message);
        }

        private static EconomyTransactionResult RejectUnknownCurrency(
            CurrencyType currency,
            string transactionId)
        {
            return Reject(
                EconomyTransactionStatus.UnknownCurrency,
                transactionId,
                $"'{currency}' is not registered in CurrencyCatalog, so no balance was touched."
            );
        }

        private static void WarnIfSpendingBeforeLoad()
        {
            if (saveLoaded || unloadedSpendWarningLogged)
                return;

            unloadedSpendWarningLogged = true;
            Debug.LogWarning(
                "EconomyService received a spend before the save was loaded, so every balance is " +
                "still 0. The spend is rejected safely, but the caller is running ahead of " +
                "CatHomeSaveSystem."
            );
        }

        private static string Format(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

        private static string Describe(RewardBundle bundle) =>
            bundle == null ? "nothing" : bundle.Describe();

        private static string DescribeBalances()
        {
            IReadOnlyList<CurrencyDefinition> definitions = CurrencyCatalog.All;
            var parts = new string[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                parts[i] = definitions[i].DisplayName + " " +
                           Format(GetBalance(definitions[i].Type));
            }

            return string.Join(", ", parts);
        }

        // ----- Development logging (never per frame, stripped from release) -----

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private static void LogDevelopment(string message)
        {
            Debug.Log(message);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private static void LogTransaction(
            string verb,
            EconomySource source,
            string transactionId,
            List<CurrencyBalanceChange> changes,
            List<RewardItem> payloads)
        {
            if ((changes == null || changes.Count == 0) &&
                (payloads == null || payloads.Count == 0))
            {
                return;
            }

            var parts = new List<string>(4);
            if (changes != null)
            {
                for (int i = 0; i < changes.Count; i++)
                    parts.Add(changes[i].ToString());
            }

            if (payloads != null)
            {
                for (int i = 0; i < payloads.Count; i++)
                    parts.Add(payloads[i].ToString());
            }

            string id = string.IsNullOrEmpty(transactionId) ? string.Empty : $" [txn {transactionId}]";
            Debug.Log($"EconomyService {verb} ({source}){id}: {string.Join(", ", parts)}.");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            Balances.Clear();
            ProcessedIds.Clear();
            ProcessedIdOrder.Clear();
            PayloadHandlers.Clear();
            seeded = false;
            saveLoaded = false;
            unloadedSpendWarningLogged = false;
            transactionDepth = 0;
            BalanceChanged = null;
            AnyBalanceChanged = null;
        }
    }
}
