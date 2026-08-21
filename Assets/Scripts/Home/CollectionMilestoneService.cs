using System;
using System.Collections.Generic;
using CatHome.Economy;
using UnityEngine;

/// <summary>
/// One-time rewards for finishing a room's 10-item collection and for filling
/// the whole catalog. Grants happen on <see cref="TryClaim"/> (the celebration
/// COLLECT button), never inside <see cref="HomeStoreService.TryPurchase"/>, so
/// wallet tests keep seeing only the purchase spend.
/// </summary>
public readonly struct CollectionMilestone
{
    public CollectionMilestone(
        string id,
        string title,
        string roomId,
        long coins,
        long diamonds,
        long bondXp)
    {
        Id = id;
        Title = title;
        RoomId = roomId;
        Coins = Math.Max(0L, coins);
        Diamonds = Math.Max(0L, diamonds);
        BondXp = Math.Max(0L, bondXp);
    }

    public string Id { get; }
    public string Title { get; }
    public string RoomId { get; }
    public long Coins { get; }
    public long Diamonds { get; }
    public long BondXp { get; }
    public bool IsCatalogWide => string.IsNullOrEmpty(RoomId);
}

public static class CollectionMilestoneService
{
    public const string LivingRoomId = "col.living-room";
    public const string BathroomId = "col.bathroom";
    public const string KitchenId = "col.kitchen";
    public const string BedroomId = "col.bedroom";
    public const string GardenId = "col.garden";
    public const string BalconyId = "col.balcony";
    public const string PatioId = "col.patio";
    public const string SecondFloorId = "col.second-floor";
    public const string CatalogId = "col.catalog";
    private const string PrefsKey = "CatHome_CollectionMilestones";

    private static readonly CollectionMilestone[] CatalogInternal =
    {
        new CollectionMilestone(LivingRoomId, "LIVING ROOM COMPLETE!", HomeRoomService.LivingRoomId, 500L, 0L, 15L),
        new CollectionMilestone(BathroomId, "BATHROOM COMPLETE!", HomeRoomService.BathroomId, 500L, 0L, 15L),
        new CollectionMilestone(KitchenId, "KITCHEN COMPLETE!", HomeRoomService.KitchenId, 500L, 0L, 15L),
        new CollectionMilestone(BedroomId, "BEDROOM COMPLETE!", HomeRoomService.BedroomId, 500L, 0L, 15L),
        new CollectionMilestone(GardenId, "GARDEN COMPLETE!", HomeRoomService.GardenId, 500L, 0L, 15L),
        new CollectionMilestone(BalconyId, "BALCONY COMPLETE!", HomeRoomService.BalconyId, 500L, 0L, 15L),
        new CollectionMilestone(PatioId, "GARDEN PATIO COMPLETE!", HomeRoomService.PatioId, 500L, 0L, 15L),
        new CollectionMilestone(SecondFloorId, "SECOND FLOOR COMPLETE!", HomeRoomService.SecondFloorId, 500L, 0L, 15L),
        new CollectionMilestone(CatalogId, "EVERYTHING COLLECTED!", null, 1000L, 1L, 40L)
    };

    private static readonly HashSet<string> Claimed =
        new HashSet<string>(StringComparer.Ordinal);
    private static readonly Queue<string> Pending = new Queue<string>();
    private static bool initialized;

    public static event Action<CollectionMilestone> Completed;
    public static IReadOnlyList<CollectionMilestone> Catalog => CatalogInternal;
    public static int CatalogSize => HomeStoreService.Products.Count;
    public static int OwnedCount => CountOwned();

    public static string FormatOwnedLabel()
    {
        return OwnedCount + " OF " + CatalogSize + " • COLLECTED";
    }

    public static bool IsClaimed(string id)
    {
        EnsureInitialized();
        return !string.IsNullOrWhiteSpace(id) && Claimed.Contains(id);
    }

    public static bool TryPeekPending(out CollectionMilestone milestone)
    {
        EnsureInitialized();
        while (Pending.Count > 0)
        {
            string id = Pending.Peek();
            if (TryGet(id, out milestone) && !Claimed.Contains(id))
                return true;
            Pending.Dequeue();
        }

        milestone = default;
        return false;
    }

    public static void HandleOwned(string productId)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(productId))
            return;
        EnqueueNewlySatisfied();
    }

    /// <summary>
    /// After a save load, queue any finished collections that were never claimed
    /// (rooms completed before this service existed).
    /// </summary>
    public static void ScanOwned()
    {
        EnsureInitialized();
        EnqueueNewlySatisfied();
    }

    private static void EnqueueNewlySatisfied()
    {
        for (int i = 0; i < CatalogInternal.Length; i++)
        {
            CollectionMilestone milestone = CatalogInternal[i];
            if (Claimed.Contains(milestone.Id) || IsQueued(milestone.Id))
                continue;
            if (!IsSatisfied(milestone))
                continue;
            Pending.Enqueue(milestone.Id);
            Completed?.Invoke(milestone);
        }
    }

    public static bool TryClaim(string id)
    {
        EnsureInitialized();
        ProgressionService.EnsureEconomyBinding();
        if (!TryGet(id, out CollectionMilestone milestone) || Claimed.Contains(id))
            return false;
        if (!IsSatisfied(milestone))
            return false;

        RewardBundle bundle = RewardBundle.FromQuestRewards(
            milestone.Coins,
            milestone.BondXp,
            milestone.Diamonds);
        EconomyTransactionResult grant = EconomyService.GrantReward(
            bundle,
            EconomySource.Achievement,
            "collection:" + id,
            EconomyPersistence.Immediate);
        if (!grant.IsSettled && !bundle.IsEmpty)
            return false;

        Claimed.Add(id);
        Dequeue(id);
        SaveClaimed();
        return true;
    }

    public static void ClearAll()
    {
        Claimed.Clear();
        Pending.Clear();
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.Save();
        initialized = true;
    }

    public static void EnsureInitialized()
    {
        if (initialized)
            return;
        initialized = true;
        LoadClaimed();
    }

    private static bool IsSatisfied(CollectionMilestone milestone)
    {
        if (milestone.IsCatalogWide)
            return OwnedCount >= CatalogSize && CatalogSize > 0;
        IReadOnlyList<string> collection = HomeStoreService.GetRoomCollection(milestone.RoomId);
        return collection.Count > 0 &&
               HomeStoreService.GetRoomOwnedCount(milestone.RoomId) >= collection.Count;
    }

    private static int CountOwned()
    {
        int count = 0;
        IReadOnlyList<HomeStoreProduct> products = HomeStoreService.Products;
        for (int i = 0; i < products.Count; i++)
        {
            if (HomeStoreService.IsOwned(products[i].Id))
                count++;
        }

        return count;
    }

    private static bool IsQueued(string id)
    {
        foreach (string pending in Pending)
            if (string.Equals(pending, id, StringComparison.Ordinal))
                return true;
        return false;
    }

    private static void Dequeue(string id)
    {
        if (Pending.Count == 0)
            return;
        var kept = new Queue<string>();
        while (Pending.Count > 0)
        {
            string next = Pending.Dequeue();
            if (!string.Equals(next, id, StringComparison.Ordinal))
                kept.Enqueue(next);
        }
        while (kept.Count > 0)
            Pending.Enqueue(kept.Dequeue());
    }

    private static bool TryGet(string id, out CollectionMilestone milestone)
    {
        for (int i = 0; i < CatalogInternal.Length; i++)
        {
            if (string.Equals(CatalogInternal[i].Id, id, StringComparison.Ordinal))
            {
                milestone = CatalogInternal[i];
                return true;
            }
        }

        milestone = default;
        return false;
    }

    private static void LoadClaimed()
    {
        Claimed.Clear();
        string raw = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (string.IsNullOrEmpty(raw))
            return;
        string[] parts = raw.Split('|');
        for (int i = 0; i < parts.Length; i++)
            if (!string.IsNullOrWhiteSpace(parts[i]))
                Claimed.Add(parts[i]);
    }

    private static void SaveClaimed()
    {
        var ids = new List<string>(Claimed);
        ids.Sort(StringComparer.Ordinal);
        PlayerPrefs.SetString(PrefsKey, string.Join("|", ids));
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        Claimed.Clear();
        Pending.Clear();
        initialized = false;
        Completed = null;
    }
}
