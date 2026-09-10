using System;
using System.Collections.Generic;
using UnityEngine;

public enum HomeRoomZone { RearFurniture, SideFurniture, FrontActivity, FloorAccent, WallAccent }

[Serializable]
public sealed class HomeRoomLayoutEntry
{
    public string roomId, productId;
    public Vector3 position;
    public float yaw, modelScale = 1f, height;
    public Vector2 footprint;
    public HomeRoomZone zone;
}

/// <summary>Authored results of the common room planner, independent of purchase order or saves.</summary>
public sealed class HomeRoomLayoutCatalog : ScriptableObject
{
    public const string ResourcePath = "Home/RoomLayoutCatalog";
    [SerializeField] private int layoutVersion = 1;
    [SerializeField] private HomeRoomLayoutEntry[] entries = Array.Empty<HomeRoomLayoutEntry>();
    private Dictionary<string, HomeRoomLayoutEntry> lookup;
    private static HomeRoomLayoutCatalog loaded;
    public IReadOnlyList<HomeRoomLayoutEntry> Entries => entries;
    public int Version => layoutVersion;

    public static bool TryGet(string productId, out HomeRoomLayoutEntry entry)
    {
        if (loaded == null) loaded = Resources.Load<HomeRoomLayoutCatalog>(ResourcePath);
        entry = null;
        if (loaded == null || string.IsNullOrEmpty(productId)) return false;
        if (loaded.lookup == null) loaded.Reindex();
        return loaded.lookup.TryGetValue(productId, out entry);
    }
    private void OnEnable() => Reindex();
    private void Reindex()
    {
        lookup = new Dictionary<string, HomeRoomLayoutEntry>(StringComparer.Ordinal);
        foreach (var entry in entries)
            if (entry != null && !string.IsNullOrEmpty(entry.productId)) lookup[entry.productId] = entry;
    }
#if UNITY_EDITOR
    public void EditorReplace(IEnumerable<HomeRoomLayoutEntry> values)
    {
        entries = new List<HomeRoomLayoutEntry>(values).ToArray();
        Reindex(); loaded = this;
    }
#endif
}
