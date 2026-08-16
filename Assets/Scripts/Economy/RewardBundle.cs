using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace CatHome.Economy
{
    /// <summary>
    /// What a single reward line pays out. Only <see cref="Currency"/> is settled
    /// by the economy itself; every other kind is forwarded to a registered
    /// payload handler (see <see cref="EconomyService.RegisterPayloadHandler"/>),
    /// which is how bond XP already works and how furniture and items will work
    /// later without redesigning the reward model.
    /// </summary>
    public enum RewardPayloadKind
    {
        Currency = 0,
        BondXp = 1,
        PlayerXp = 2,
        Furniture = 3,
        Item = 4,
    }

    /// <summary>
    /// One immutable line of a <see cref="RewardBundle"/>: "+50 coins",
    /// "+5 bond XP", "+1 furniture:sofa_blue". Amounts are always positive; a
    /// cost is expressed by the API that consumes it, never by a negative reward.
    /// </summary>
    public readonly struct RewardItem
    {
        private RewardItem(RewardPayloadKind kind, CurrencyType currency, string itemId, long amount)
        {
            Kind = kind;
            Currency = currency;
            ItemId = itemId;
            Amount = amount;
        }

        public RewardPayloadKind Kind { get; }

        /// <summary>Meaningful only when <see cref="Kind"/> is Currency.</summary>
        public CurrencyType Currency { get; }

        /// <summary>Meaningful only for the item-like kinds (Furniture, Item).</summary>
        public string ItemId { get; }

        public long Amount { get; }

        public bool IsCurrency => Kind == RewardPayloadKind.Currency;

        /// <summary>
        /// A line that the economy is willing to process. Invalid lines are
        /// dropped by <see cref="RewardBundle"/> instead of silently paying out
        /// something unintended.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (Amount <= 0)
                    return false;

                return Kind switch
                {
                    RewardPayloadKind.Currency => CurrencyCatalog.IsSupported(Currency),
                    RewardPayloadKind.Furniture => !string.IsNullOrWhiteSpace(ItemId),
                    RewardPayloadKind.Item => !string.IsNullOrWhiteSpace(ItemId),
                    _ => true,
                };
            }
        }

        public static RewardItem Currencies(CurrencyType currency, long amount) =>
            new RewardItem(RewardPayloadKind.Currency, currency, null, amount);

        public static RewardItem Coins(long amount) => Currencies(CurrencyType.Coin, amount);

        public static RewardItem Diamonds(long amount) => Currencies(CurrencyType.Diamond, amount);

        public static RewardItem BondXp(long amount) =>
            new RewardItem(RewardPayloadKind.BondXp, CurrencyType.None, null, amount);

        public static RewardItem PlayerXp(long amount) =>
            new RewardItem(RewardPayloadKind.PlayerXp, CurrencyType.None, null, amount);

        public static RewardItem Furniture(string furnitureId, long count = 1) =>
            new RewardItem(RewardPayloadKind.Furniture, CurrencyType.None, furnitureId, count);

        public static RewardItem Item(string itemId, long count = 1) =>
            new RewardItem(RewardPayloadKind.Item, CurrencyType.None, itemId, count);

        /// <summary>Same payload target, ignoring the amount. Used to merge lines.</summary>
        internal bool HasSameTarget(RewardItem other)
        {
            if (Kind != other.Kind)
                return false;

            return Kind == RewardPayloadKind.Currency
                ? Currency == other.Currency
                : string.Equals(ItemId, other.ItemId, StringComparison.Ordinal);
        }

        internal RewardItem WithAmount(long amount) =>
            new RewardItem(Kind, Currency, ItemId, amount);

        public override string ToString()
        {
            string amount = "+" + Amount.ToString(CultureInfo.InvariantCulture);
            return Kind switch
            {
                RewardPayloadKind.Currency => amount + " " + CurrencyCatalog.GetDisplayName(Currency),
                RewardPayloadKind.Furniture => amount + " furniture:" + ItemId,
                RewardPayloadKind.Item => amount + " item:" + ItemId,
                _ => amount + " " + Kind,
            };
        }
    }

    /// <summary>
    /// An immutable set of reward lines paid out as one atomic transaction. This
    /// is the reward model every producer builds and every consumer hands to
    /// <see cref="EconomyService.GrantReward"/> — quests today, daily quests,
    /// achievements, rewarded ads, IAP products and events later.
    ///
    /// Construction sanitizes: non-positive and malformed lines are dropped with
    /// a development warning, and lines that target the same currency or item are
    /// merged (with an overflow-safe sum) so a bundle can never contain two
    /// competing entries for the same thing.
    /// </summary>
    public sealed class RewardBundle
    {
        private static readonly RewardItem[] NoItems = Array.Empty<RewardItem>();

        /// <summary>Pays nothing. Granting it is a successful no-op.</summary>
        public static readonly RewardBundle Empty = new RewardBundle(NoItems);

        private readonly RewardItem[] items;
        private readonly ReadOnlyCollection<RewardItem> readOnlyItems;

        private RewardBundle(RewardItem[] sanitizedItems)
        {
            items = sanitizedItems ?? NoItems;
            readOnlyItems = Array.AsReadOnly(items);
        }

        public IReadOnlyList<RewardItem> Items => readOnlyItems;

        public int Count => items.Length;

        public bool IsEmpty => items.Length == 0;

        /// <summary>True when at least one line needs a payload handler.</summary>
        public bool HasNonCurrencyPayload
        {
            get
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (!items[i].IsCurrency)
                        return true;
                }

                return false;
            }
        }

        public static RewardBundle Create(params RewardItem[] lines) => Sanitize(lines);

        public static RewardBundle CreateFrom(IReadOnlyList<RewardItem> lines) => Sanitize(lines);

        public static RewardBundle Coins(long amount) =>
            Sanitize(new[] { RewardItem.Coins(amount) });

        public static RewardBundle Diamonds(long amount) =>
            Sanitize(new[] { RewardItem.Diamonds(amount) });

        public static RewardBundle Currency(CurrencyType currency, long amount) =>
            Sanitize(new[] { RewardItem.Currencies(currency, amount) });

        /// <summary>
        /// The shape every quest definition in the project currently declares.
        /// Zero rewards are dropped by the sanitizer, so a quest with only coins
        /// produces a one-line bundle.
        /// </summary>
        public static RewardBundle FromQuestRewards(long coins, long bondXp, long diamonds)
        {
            return Sanitize(new[]
            {
                RewardItem.Coins(coins),
                RewardItem.BondXp(bondXp),
                RewardItem.Diamonds(diamonds),
            });
        }

        /// <summary>Returns a new bundle; the receiver is never modified.</summary>
        public RewardBundle With(RewardItem line)
        {
            var combined = new RewardItem[items.Length + 1];
            Array.Copy(items, combined, items.Length);
            combined[items.Length] = line;
            return Sanitize(combined);
        }

        /// <summary>Returns a new bundle containing the lines of both.</summary>
        public RewardBundle CombinedWith(RewardBundle other)
        {
            if (other == null || other.IsEmpty)
                return this;

            if (IsEmpty)
                return other;

            var combined = new RewardItem[items.Length + other.items.Length];
            Array.Copy(items, combined, items.Length);
            Array.Copy(other.items, 0, combined, items.Length, other.items.Length);
            return Sanitize(combined);
        }

        /// <summary>Total amount of one currency in this bundle; 0 when absent.</summary>
        public long GetCurrencyAmount(CurrencyType currency)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].IsCurrency && items[i].Currency == currency)
                    return items[i].Amount;
            }

            return 0;
        }

        /// <summary>Human-readable single line for logs and future reward popups.</summary>
        public string Describe()
        {
            if (items.Length == 0)
                return "nothing";

            var builder = new StringBuilder();
            for (int i = 0; i < items.Length; i++)
            {
                if (i > 0)
                    builder.Append(", ");
                builder.Append(items[i].ToString());
            }

            return builder.ToString();
        }

        public override string ToString() => Describe();

        private static RewardBundle Sanitize(IReadOnlyList<RewardItem> lines)
        {
            if (lines == null || lines.Count == 0)
                return Empty;

            List<RewardItem> merged = null;
            for (int i = 0; i < lines.Count; i++)
            {
                RewardItem line = lines[i];

                // A zero line is the normal way a definition says "no reward of
                // this kind", so it is dropped silently. Anything else that fails
                // validation is a real authoring mistake and is reported.
                if (line.Amount == 0)
                    continue;

                if (!line.IsValid)
                {
                    Debug.LogWarning(
                        $"RewardBundle dropped an invalid reward line ({line.Kind}, " +
                        $"amount {line.Amount.ToString(CultureInfo.InvariantCulture)}). " +
                        "Amounts must be positive, currencies must be registered and " +
                        "item rewards need an id."
                    );
                    continue;
                }

                merged ??= new List<RewardItem>(lines.Count);

                bool combinedIntoExisting = false;
                for (int existing = 0; existing < merged.Count; existing++)
                {
                    if (!merged[existing].HasSameTarget(line))
                        continue;

                    long current = merged[existing].Amount;

                    // Clamped rather than wrapped: a bundle can be assembled from
                    // several sources (a bundle plus a bonus), and a wrapped sum
                    // would turn a reward into a tiny or negative payout.
                    long sum = line.Amount > long.MaxValue - current
                        ? long.MaxValue
                        : current + line.Amount;

                    merged[existing] = merged[existing].WithAmount(sum);
                    combinedIntoExisting = true;
                    break;
                }

                if (!combinedIntoExisting)
                    merged.Add(line);
            }

            return merged == null || merged.Count == 0
                ? Empty
                : new RewardBundle(merged.ToArray());
        }
    }
}
