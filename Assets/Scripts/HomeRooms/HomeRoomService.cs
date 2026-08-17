using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Immutable metadata for one visitable room. Quest chapters deliberately do not
/// live here: rooms are home navigation, while LevelDefinition remains quest data.
/// </summary>
public readonly struct HomeRoomDefinition
{
    public HomeRoomDefinition(
        string id,
        string displayName,
        string scenePath,
        string spawnPointId,
        string requiredOwnershipId,
        int requiredHomeLevel)
    {
        Id = id;
        DisplayName = displayName;
        ScenePath = scenePath;
        SpawnPointId = string.IsNullOrWhiteSpace(spawnPointId) ? "default" : spawnPointId;
        RequiredOwnershipId = requiredOwnershipId;
        RequiredHomeLevel = Math.Max(1, requiredHomeLevel);
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string ScenePath { get; }
    public string SceneName => Path.GetFileNameWithoutExtension(ScenePath);
    public string SpawnPointId { get; }
    public string RequiredOwnershipId { get; }
    public int RequiredHomeLevel { get; }
    public bool IsAlwaysUnlocked => string.IsNullOrWhiteSpace(RequiredOwnershipId);
}

/// <summary>
/// Canonical room catalog and the current-room state shared by bootstrap, UI,
/// saving and Cat Runner return navigation. It never spends currency and never
/// grants ownership; room access is read from HomeStoreService only.
/// </summary>
public static class HomeRoomService
{
    public const string LivingRoomId = "living-room-01";
    public const string BathroomId = "bathroom-01";
    public const string KitchenId = "kitchen-01";
    public const string BedroomId = "bedroom-01";
    public const string GardenId = "garden-01";
    public const string LivingRoomScenePath =
        "Assets/Scenes/Levels/LivingRoom_Level01.unity";
    public const string BathroomScenePath =
        "Assets/Scenes/Levels/Bathroom_Level01.unity";
    public const string KitchenScenePath =
        "Assets/Scenes/Levels/Kitchen_Level01.unity";
    public const string BedroomScenePath =
        "Assets/Scenes/Levels/Bedroom_Level01.unity";
    public const string GardenScenePath =
        "Assets/Scenes/Levels/Garden_Level01.unity";

    private static readonly HomeRoomDefinition[] RoomsInternal =
    {
        new HomeRoomDefinition(
            LivingRoomId,
            "LIVING ROOM",
            LivingRoomScenePath,
            "default",
            null,
            1),
        new HomeRoomDefinition(
            BathroomId,
            "BATHROOM",
            BathroomScenePath,
            "default",
            HomeStoreService.HomeBathroomPreviewId,
            GetHomeLevelRequirement(HomeStoreService.HomeBathroomPreviewId)),
        new HomeRoomDefinition(
            KitchenId,
            "KITCHEN",
            KitchenScenePath,
            "default",
            HomeStoreService.HomeKitchenPreviewId,
            GetHomeLevelRequirement(HomeStoreService.HomeKitchenPreviewId)),
        new HomeRoomDefinition(
            BedroomId,
            "BEDROOM",
            BedroomScenePath,
            "default",
            HomeStoreService.HomeBedroomPreviewId,
            GetHomeLevelRequirement(HomeStoreService.HomeBedroomPreviewId)),
        new HomeRoomDefinition(
            GardenId,
            "GARDEN",
            GardenScenePath,
            "default",
            HomeStoreService.HomeGardenPreviewId,
            GetHomeLevelRequirement(HomeStoreService.HomeGardenPreviewId))
    };

    private static readonly Dictionary<string, HomeRoomDefinition> RoomsById =
        new Dictionary<string, HomeRoomDefinition>(StringComparer.Ordinal);

    private static string currentRoomId = LivingRoomId;

    static HomeRoomService()
    {
        for (int i = 0; i < RoomsInternal.Length; i++)
            RoomsById[RoomsInternal[i].Id] = RoomsInternal[i];
    }

    public static IReadOnlyList<HomeRoomDefinition> Rooms => RoomsInternal;
    public static string CurrentRoomId => currentRoomId;
    public static HomeRoomDefinition CurrentRoom => GetOrLivingRoom(currentRoomId);
    public static string CurrentRoomScenePath => CurrentRoom.ScenePath;
    public static string CurrentRoomSceneName => CurrentRoom.SceneName;
    public static event Action<string> CurrentRoomChanged;

    public static bool TryGetRoom(string roomId, out HomeRoomDefinition room)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            room = default;
            return false;
        }

        return RoomsById.TryGetValue(roomId, out room);
    }

    public static HomeRoomDefinition GetOrLivingRoom(string roomId)
    {
        return TryGetRoom(roomId, out HomeRoomDefinition room)
            ? room
            : RoomsInternal[0];
    }

    public static bool IsRoomUnlocked(string roomId)
    {
        if (!TryGetRoom(roomId, out HomeRoomDefinition room))
            return false;

        // Room access is ownership + dependency only. The Home Level gate is
        // enforced once, when the preview product is purchased (HomeStoreService),
        // and Home Level never regresses, so owning the preview already proves the
        // requirement was met. Re-checking it here would wrongly re-lock a room the
        // player owns after the save migration seeds a default (zero) Home XP for
        // the progression slice added in save version 10.
        return room.IsAlwaysUnlocked ||
               (HomeStoreService.IsOwned(room.RequiredOwnershipId) &&
                HomeStoreService.IsProductDependencyMet(room.RequiredOwnershipId));
    }

    public static int GetRequiredHomeLevel(string roomId)
    {
        return TryGetRoom(roomId, out HomeRoomDefinition room)
            ? room.RequiredHomeLevel
            : 1;
    }

    /// <summary>
    /// Applies save/bootstrap state without inventing access. Unknown or locked
    /// room ids safely return to the Living Room.
    /// </summary>
    public static string ApplySavedRoomId(string savedRoomId)
    {
        string resolved = TryGetRoom(savedRoomId, out HomeRoomDefinition room) &&
                          IsRoomUnlocked(room.Id)
            ? room.Id
            : LivingRoomId;
        SetCurrentRoomInternal(resolved, false);
        return resolved;
    }

    /// <summary>
    /// Commits a successfully activated room. The loader validates ownership and
    /// scene readiness before calling this; this method performs no economy work.
    /// </summary>
    public static bool TryMarkRoomActive(string roomId)
    {
        return TryMarkRoomActive(roomId, false);
    }

    internal static bool TryMarkRoomActive(string roomId, bool authoringOverride)
    {
        if (!TryGetRoom(roomId, out HomeRoomDefinition room))
            return false;

        bool unlocked = IsRoomUnlocked(room.Id);
#if UNITY_EDITOR
        unlocked |= authoringOverride && Application.isEditor;
#endif
        if (!unlocked)
            return false;

        SetCurrentRoomInternal(room.Id, true);
        return true;
    }

    public static bool IsKnownScenePath(string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
            return false;

        for (int i = 0; i < RoomsInternal.Length; i++)
        {
            if (string.Equals(RoomsInternal[i].ScenePath, scenePath, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static void SetCurrentRoomInternal(string roomId, bool notify)
    {
        if (string.Equals(currentRoomId, roomId, StringComparison.Ordinal))
            return;

        currentRoomId = roomId;
        if (notify)
            CurrentRoomChanged?.Invoke(currentRoomId);
    }

    private static int GetHomeLevelRequirement(string previewProductId)
    {
        return HomeStoreService.TryGetProduct(previewProductId, out HomeStoreProduct product)
            ? product.RequiredLevel
            : 1;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        currentRoomId = LivingRoomId;
        CurrentRoomChanged = null;
    }
}
