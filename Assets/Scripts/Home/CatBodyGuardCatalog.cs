using System;
using UnityEngine;

/// <summary>Source-skin envelopes in CatBreedVisualTag local space; baked once in the editor.</summary>
public sealed class CatBodyGuardCatalog : ScriptableObject
{
    public const string ResourceName = "Home/CatBodyGuardCatalog";

    [Serializable]
    public struct Probe
    {
        public string region;
        public Vector3 start, end;
        public float radius;
    }

    [Serializable]
    public sealed class Entry
    {
        public string breedId;
        public Probe[] probes = Array.Empty<Probe>();
        public int sampledPoses, sampledVertices;
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();
    public int Count => entries.Length;
    public Entry Find(string breedId)
    {
        foreach (var entry in entries)
            if (entry != null && entry.breedId == breedId) return entry;
        return null;
    }

#if UNITY_EDITOR
    public void EditorConfigure(Entry[] values) => entries = values ?? Array.Empty<Entry>();
#endif
}
