using System;
using UnityEngine;

/// <summary>Unmodified NativeJump skin in grounded CatBreedVisualTag coordinates.</summary>
public sealed class CatJumpClearanceCatalog : ScriptableObject
{
    public const string ResourceName = "Home/CatJumpClearanceCatalog";
    public const int BodyCoverageVersion = 1;
    public const int WeightedBodyVersion = 1;
    public const int SupportedPreparationVersion = 1;
    public const int MaxRegionProbes = 32;
    [Serializable] public sealed class RegionCoverage
    {
        public string region;
        public int triangleCount;
        public CatBodyGuardCatalog.Probe envelope;
        public CatBodyGuardCatalog.Probe[] parts = Array.Empty<CatBodyGuardCatalog.Probe>();
    }
    public const int HeadCoverageVersion = 1;
    public const int MaxHeadProbes = 32;
    [Serializable] public sealed class Sample
    {
        public float phase;
        public Matrix4x4[] bodySkinMatrices = Array.Empty<Matrix4x4>();
        // Optional complete source skeleton for the shared support planner.
        // Existing body envelopes/matrices remain byte-for-byte independent.
        public Matrix4x4[] supportSkinMatrices = Array.Empty<Matrix4x4>();
        public int bodyCoverageVersion;
        public RegionCoverage[] bodyCoverage = Array.Empty<RegionCoverage>();
        // Versioned, full-triangle coverage. Older catalogs keep the original
        // four-probe result until explicitly measured again by the editor baker.
        public int headCoverageVersion;
        public int headTriangleCount;
        public CatBodyGuardCatalog.Probe headEnvelope;
        public CatBodyGuardCatalog.Probe[] headProbes = Array.Empty<CatBodyGuardCatalog.Probe>();
        public CatBodyGuardCatalog.Probe[] probes = Array.Empty<CatBodyGuardCatalog.Probe>();
    }
    [Serializable] public sealed class Entry
    {
        public string breedId;
        // One immutable bind/topology table per breed, not duplicated per phase.
        public int weightedBodyVersion;
        public int supportedPreparationVersion;
        public string supportProfileKey, supportProfileHash;
        public string[] supportBonePaths = Array.Empty<string>();
        public CatPawReachCatalog.BodyRegion[] bodySurface = Array.Empty<CatPawReachCatalog.BodyRegion>();
        public CatPawReachCatalog.SkinBone[] bodyBones = Array.Empty<CatPawReachCatalog.SkinBone>();
        public AnimationClip sourceClip;
        public Vector3 sourceZeroCentre;
        public Sample[] samples = Array.Empty<Sample>();
        public int sampledVertices;
    }
    [SerializeField] Entry[] entries = Array.Empty<Entry>();
    static CatJumpClearanceCatalog loaded;
    static ResourceRequest loading;
    static bool loadFailed;
    public static bool HasPendingLoad => loading != null && !loading.isDone;
    public static bool IsReady => Load() != null;
    public static void Preload() => Load();
    public static CatJumpClearanceCatalog Load()
    {
        if (loaded != null || loadFailed) return loaded;
        if (!Application.isPlaying) return loaded = Resources.Load<CatJumpClearanceCatalog>(ResourceName);
        // The complete source-jump catalog is large. Start it with the room's
        // measured contact data, never synchronously on the first nearby prompt.
        // Reading ResourceRequest.asset before completion would block the frame.
        if (loading == null) loading = Resources.LoadAsync<CatJumpClearanceCatalog>(ResourceName);
        if (!loading.isDone) return null;
        loaded = loading.asset as CatJumpClearanceCatalog;
        loading = null; loadFailed = loaded == null;
        return loaded;
    }
    public Entry Find(string breed)
    {
        foreach (var entry in entries) if (entry != null && entry.breedId == breed) return entry;
        return null;
    }
#if UNITY_EDITOR
    public void EditorConfigure(Entry[] values) { entries = values; loaded = this; loadFailed = false; }
    public static void EditorClearResourceCacheForQa()
    {
        if (loading != null && !loading.isDone) throw new InvalidOperationException("Jump catalog load is still running");
        if (loaded != null) Resources.UnloadAsset(loaded);
        loaded = null; loading = null; loadFailed = false;
    }
#endif
}
