using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Project-owned shading assets; the vendor cats and their animation data stay untouched.</summary>
public sealed class CatModernVisualCatalog : ScriptableObject
{
    public const string ResourceName = "CatModernVisualCatalog";

    [Serializable]
    public sealed class MeshPair
    {
        public Mesh source;
        public Mesh modern;
        public int softenedVertices;
        public int protectedFaceVertices;
    }

    [Serializable]
    public sealed class MaterialPair
    {
        public Material source;
        public Material modern;
    }

    [Serializable]
    public sealed class PortraitPair
    {
        public string breedId;
        public string originalGuid;
        public Sprite identity;
        public Sprite fullBody;
    }

    [SerializeField] private MeshPair[] meshes = Array.Empty<MeshPair>();
    [SerializeField] private MaterialPair[] materials = Array.Empty<MaterialPair>();
    [SerializeField] private PortraitPair[] portraits = Array.Empty<PortraitPair>();
    [SerializeField] private int revision = 1;
    private Dictionary<Mesh, Mesh> meshLookup;
    private Dictionary<Material, Material> materialLookup;
    private static CatModernVisualCatalog cached;

    public IReadOnlyList<MeshPair> Meshes => meshes;
    public IReadOnlyList<MaterialPair> Materials => materials;
    public IReadOnlyList<PortraitPair> Portraits => portraits;
    public int Revision => revision;

    public static CatModernVisualCatalog Load()
    {
        if (cached == null) cached = Resources.Load<CatModernVisualCatalog>(ResourceName);
        return cached;
    }

    public Mesh Resolve(Mesh source)
    {
        if (source == null) return null;
        if (meshLookup == null)
        {
            meshLookup = new Dictionary<Mesh, Mesh>();
            foreach (var pair in meshes)
                if (pair != null && pair.source != null && pair.modern != null)
                { meshLookup[pair.source] = pair.modern; meshLookup[pair.modern] = pair.modern; }
        }
        return meshLookup.TryGetValue(source, out var modern) ? modern : source;
    }

    public Material Resolve(Material source)
    {
        if (source == null) return null;
        if (materialLookup == null)
        {
            materialLookup = new Dictionary<Material, Material>();
            foreach (var pair in materials)
                if (pair != null && pair.source != null && pair.modern != null)
                { materialLookup[pair.source] = pair.modern; materialLookup[pair.modern] = pair.modern; }
        }
        return materialLookup.TryGetValue(source, out var modern) ? modern : source;
    }

#if UNITY_EDITOR
    public void EditorConfigurePortraits(PortraitPair[] portraitPairs)
    {
        portraits = portraitPairs ?? Array.Empty<PortraitPair>();
        cached = this;
    }

    public void EditorConfigure(MeshPair[] meshPairs, MaterialPair[] materialPairs)
    {
        meshes = meshPairs ?? Array.Empty<MeshPair>();
        materials = materialPairs ?? Array.Empty<MaterialPair>();
        revision = revision == int.MaxValue ? 1 : revision + 1;
        meshLookup = null;
        materialLookup = null;
        cached = this;
    }
#endif
}
