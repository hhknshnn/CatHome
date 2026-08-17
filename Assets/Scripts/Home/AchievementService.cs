using System;
using System.Collections.Generic;
using CatHome.Economy;
using UnityEngine;

[Serializable]
public sealed class AchievementSaveState
{
    public int achievementVersion = AchievementService.SaveVersion;
    public string[] unlockedIds = Array.Empty<string>();

    public static AchievementSaveState CreateDefault()
    {
        return new AchievementSaveState
        {
            achievementVersion = AchievementService.SaveVersion,
            unlockedIds = Array.Empty<string>()
        };
    }
}

public readonly struct AchievementDefinition
{
    public AchievementDefinition(
        string id,
        string title,
        long coins,
        long diamonds)
    {
        Id = id;
        Title = title;
        Coins = Math.Max(0L, coins);
        Diamonds = Math.Max(0L, diamonds);
    }

    public string Id { get; }
    public string Title { get; }
    public long Coins { get; }
    public long Diamonds { get; }
}

/// <summary>
/// One-time milestone rewards. Unlocks are idempotent through economy
/// transaction ids; existing wallets and quests are never rewritten.
/// </summary>
public static class AchievementService
{
    public const int SaveVersion = 1;
    public const string FirstRunId = "ach.first-run";
    public const string FirstShopId = "ach.first-shop";
    public const string HomeLevel3Id = "ach.home-level-3";
    public const string HomeLevel5Id = "ach.home-level-5";
    public const string Bond80Id = "ach.bond-80";
    public const string Bond250Id = "ach.bond-250";
    public const string LoginStreak7Id = "ach.login-streak-7";

    private static readonly AchievementDefinition[] CatalogInternal =
    {
        new AchievementDefinition(FirstRunId, "First Dash", 20L, 0L),
        new AchievementDefinition(FirstShopId, "First Decor", 25L, 0L),
        new AchievementDefinition(HomeLevel3Id, "Cozy Home", 40L, 0L),
        new AchievementDefinition(HomeLevel5Id, "Garden Ready", 0L, 1L),
        new AchievementDefinition(Bond80Id, "Window Friend", 30L, 0L),
        new AchievementDefinition(Bond250Id, "Bird Friend", 0L, 1L),
        new AchievementDefinition(LoginStreak7Id, "Week Together", 0L, 1L)
    };

    private static readonly HashSet<string> Unlocked =
        new HashSet<string>(StringComparer.Ordinal);
    private static bool initialized;

    public static event Action StateChanged;
    public static IReadOnlyList<AchievementDefinition> Catalog => CatalogInternal;
    public static int UnlockedCount => Unlocked.Count;

    public static bool IsUnlocked(string id)
    {
        EnsureInitialized();
        return !string.IsNullOrWhiteSpace(id) && Unlocked.Contains(id);
    }

    public static int Evaluate()
    {
        EnsureInitialized();
        int granted = 0;
        if (CatRunnerProgressService.TutorialCompleted ||
            CatRunnerProgressService.BestScore > 0)
        {
            granted += TryUnlock(FirstRunId) ? 1 : 0;
        }

        if (CountOwnedProducts() > 0)
            granted += TryUnlock(FirstShopId) ? 1 : 0;

        if (HomeProgressionService.HomeLevel >= 3)
            granted += TryUnlock(HomeLevel3Id) ? 1 : 0;
        if (HomeProgressionService.HomeLevel >= 5)
            granted += TryUnlock(HomeLevel5Id) ? 1 : 0;

        if (ProgressionService.BondXp >= BondMilestoneService.WindowWatchBond)
            granted += TryUnlock(Bond80Id) ? 1 : 0;
        if (ProgressionService.BondXp >= BondMilestoneService.BirdWatchBond)
            granted += TryUnlock(Bond250Id) ? 1 : 0;

        if (DailyRetentionService.LoginStreak >= 7)
            granted += TryUnlock(LoginStreak7Id) ? 1 : 0;

        return granted;
    }

    public static AchievementSaveState CaptureState()
    {
        EnsureInitialized();
        string[] ids = new string[Unlocked.Count];
        Unlocked.CopyTo(ids);
        Array.Sort(ids, StringComparer.Ordinal);
        return new AchievementSaveState
        {
            achievementVersion = SaveVersion,
            unlockedIds = ids
        };
    }

    public static void ApplySavedState(AchievementSaveState saved)
    {
        Unlocked.Clear();
        if (saved?.unlockedIds != null)
        {
            for (int i = 0; i < saved.unlockedIds.Length; i++)
            {
                string id = saved.unlockedIds[i];
                if (!string.IsNullOrWhiteSpace(id))
                    Unlocked.Add(id);
            }
        }

        initialized = true;
        StateChanged?.Invoke();
    }

    private static bool TryUnlock(string id)
    {
        if (!TryGet(id, out AchievementDefinition definition) || Unlocked.Contains(id))
            return false;

        RewardBundle bundle = RewardBundle.FromQuestRewards(
            definition.Coins,
            0L,
            definition.Diamonds);
        EconomyTransactionResult grant = EconomyService.GrantReward(
            bundle,
            EconomySource.Achievement,
            "achievement:" + id,
            EconomyPersistence.DeferToCaller);
        if (!grant.IsSettled)
            return false;

        Unlocked.Add(id);
        StateChanged?.Invoke();
        return true;
    }

    private static bool TryGet(string id, out AchievementDefinition definition)
    {
        for (int i = 0; i < CatalogInternal.Length; i++)
        {
            if (string.Equals(CatalogInternal[i].Id, id, StringComparison.Ordinal))
            {
                definition = CatalogInternal[i];
                return true;
            }
        }

        definition = default;
        return false;
    }

    private static int CountOwnedProducts()
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

    private static void EnsureInitialized()
    {
        if (initialized)
            return;
        initialized = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        Unlocked.Clear();
        initialized = false;
        StateChanged = null;
    }
}
