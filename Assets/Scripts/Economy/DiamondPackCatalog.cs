using System;
using System.Collections.Generic;

namespace CatHome.Economy
{
    /// <summary>
    /// Store-facing definition for a consumable diamond pack. Real-money prices
    /// deliberately stay in Apple/Google store configuration so regional pricing
    /// remains authoritative; this catalog owns stable product ids and rewards.
    /// </summary>
    public readonly struct DiamondPackDefinition
    {
        public DiamondPackDefinition(string productId, long diamondAmount)
        {
            ProductId = productId;
            DiamondAmount = Math.Max(0L, diamondAmount);
        }

        public string ProductId { get; }
        public long DiamondAmount { get; }
        public RewardBundle Reward => RewardBundle.Diamonds(DiamondAmount);
    }

    public static class DiamondPackCatalog
    {
        private static readonly DiamondPackDefinition[] PacksInternal =
        {
            new DiamondPackDefinition("cathome.diamonds.10", 10L),
            new DiamondPackDefinition("cathome.diamonds.20", 20L),
            new DiamondPackDefinition("cathome.diamonds.50", 50L),
            new DiamondPackDefinition("cathome.diamonds.100", 100L),
            new DiamondPackDefinition("cathome.diamonds.500", 500L),
            new DiamondPackDefinition("cathome.diamonds.1000", 1000L)
        };

        public static IReadOnlyList<DiamondPackDefinition> Packs => PacksInternal;

        public static bool TryGet(string productId, out DiamondPackDefinition pack)
        {
            for (int i = 0; i < PacksInternal.Length; i++)
            {
                if (string.Equals(PacksInternal[i].ProductId, productId,
                    StringComparison.Ordinal))
                {
                    pack = PacksInternal[i];
                    return true;
                }
            }

            pack = default;
            return false;
        }

        /// <summary>
        /// Called only after the platform store validates a consumable purchase.
        /// EconomyService uses the transaction id to prevent duplicate grants.
        /// </summary>
        public static EconomyTransactionResult GrantConfirmedPurchase(
            string productId,
            string transactionId)
        {
            if (!TryGet(productId, out DiamondPackDefinition pack))
            {
                return new EconomyTransactionResult(
                    EconomyTransactionStatus.Rejected,
                    transactionId,
                    false,
                    "Unknown diamond pack.");
            }

            return EconomyService.GrantPurchasedProduct(
                pack.ProductId,
                pack.Reward,
                transactionId);
        }
    }
}
