using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Build-safe references to the ten Polyperfect cats. The asset is generated at
/// Assets/Resources/CatBreedCatalog.asset by PolyperfectCatIntegrationBuilder,
/// so runtime code never depends on editor-only AssetDatabase paths.
/// </summary>
[CreateAssetMenu(fileName = "CatBreedCatalog", menuName = "Cat Home/Cat Breed Catalog")]
public sealed class CatBreedCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private GameObject sourcePrefab;
        [SerializeField] private Sprite portrait;
        [SerializeField] private int[] contactVertexIndices = Array.Empty<int>();

        public string Id => id;
        public string DisplayName => displayName;
        public GameObject SourcePrefab => sourcePrefab;
        public Sprite Portrait => portrait;
        public IReadOnlyList<int> ContactVertexIndices => contactVertexIndices;

#if UNITY_EDITOR
        internal Entry(string id, string displayName, GameObject sourcePrefab, Sprite portrait)
        {
            this.id = id;
            this.displayName = displayName;
            this.sourcePrefab = sourcePrefab;
            this.portrait = portrait;
            contactVertexIndices = BakeContactVertices(sourcePrefab);
        }

        private static int[] BakeContactVertices(GameObject source)
        {
            var skin = source != null ? source.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            if (skin == null || skin.sharedMesh == null) return Array.Empty<int>();
            var bones = skin.bones;
            var weights = skin.sharedMesh.boneWeights;
            var body = new List<int>(weights.Length);
            for (int i = 0; i < weights.Length; i++)
            {
                var w = weights[i];
                float tail = TailWeight(bones, w.boneIndex0, w.weight0) + TailWeight(bones, w.boneIndex1, w.weight1) +
                    TailWeight(bones, w.boneIndex2, w.weight2) + TailWeight(bones, w.boneIndex3, w.weight3);
                if (tail < .25f) body.Add(i);
            }
            return body.ToArray();
        }

        private static float TailWeight(Transform[] bones, int index, float weight) =>
            index >= 0 && index < bones.Length && bones[index] != null &&
            bones[index].name.StartsWith("DEF-tail", StringComparison.Ordinal) ? weight : 0f;
#endif
    }

    public const string ResourceName = "CatBreedCatalog";
    public const string DefaultBreedId = "domestic-shorthair";
    public const float SourceVisualScale = 3f;

    [SerializeField] private RuntimeAnimatorController gameplayController;
    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    private static CatBreedCatalog cached;

    public RuntimeAnimatorController GameplayController => gameplayController;
    public IReadOnlyList<Entry> Entries => entries;
    public int Count => entries?.Length ?? 0;

    public static CatBreedCatalog Load()
    {
        if (cached == null)
            cached = Resources.Load<CatBreedCatalog>(ResourceName);
        return cached;
    }

    public Entry Get(int index)
    {
        if (entries == null || entries.Length == 0)
            return null;
        return entries[Mathf.Clamp(index, 0, entries.Length - 1)];
    }

    public Entry Find(string id)
    {
        if (entries == null || string.IsNullOrWhiteSpace(id))
            return null;
        for (int i = 0; i < entries.Length; i++)
            if (entries[i] != null && string.Equals(entries[i].Id, id, StringComparison.Ordinal))
                return entries[i];
        return null;
    }

    public int IndexOf(string id)
    {
        if (entries == null)
            return -1;
        for (int i = 0; i < entries.Length; i++)
            if (entries[i] != null && string.Equals(entries[i].Id, id, StringComparison.Ordinal))
                return i;
        return -1;
    }

#if UNITY_EDITOR
    public void EditorConfigure(RuntimeAnimatorController controller, Entry[] configuredEntries)
    {
        gameplayController = controller;
        entries = configuredEntries ?? Array.Empty<Entry>();
    }

    public static Entry EditorCreateEntry(
        string id,
        string displayName,
        GameObject sourcePrefab,
        Sprite portrait)
    {
        return new Entry(id, displayName, sourcePrefab, portrait);
    }
#endif
}
