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
        [SerializeField] private int[] leftForeSupport = Array.Empty<int>(), rightForeSupport = Array.Empty<int>(),
            leftRearSupport = Array.Empty<int>(), rightRearSupport = Array.Empty<int>();
        [SerializeField] private int[] leftForeLimb = Array.Empty<int>(), rightForeLimb = Array.Empty<int>(),
            leftRearLimb = Array.Empty<int>(), rightRearLimb = Array.Empty<int>();
        public IReadOnlyList<int> SupportLimbVertices(int limb) => limb == 0 ? leftForeLimb : limb == 1 ? rightForeLimb :
            limb == 2 ? leftRearLimb : rightRearLimb;
        public IReadOnlyList<int> SupportPawVertices(int foot) => foot == 0 ? leftForeSupport : foot == 1 ? rightForeSupport :
            foot == 2 ? leftRearSupport : rightRearSupport;

#if UNITY_EDITOR
        internal Entry(string id, string displayName, GameObject sourcePrefab, Sprite portrait)
        {
            this.id = id;
            this.displayName = displayName;
            this.sourcePrefab = sourcePrefab;
            this.portrait = portrait;
            contactVertexIndices = BakeContactVertices(sourcePrefab);
            EditorBakeSupportPaws();
        }

        public void EditorBakeSupportPaws()
        {
            var skin = sourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var bones = skin.bones; var weights = skin.sharedMesh.boneWeights;
            var groups = new List<int>[] { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
            for (int foot = 0; foot < 4; foot++)
            {
                string name = (foot < 2 ? "DEF-hand." : "DEF-foot.") + ((foot & 1) == 0 ? "L" : "R");
                Transform joint = Array.Find(bones, b => b != null && b.name == name);
                if (joint == null) throw new InvalidOperationException(id + " missing " + name);
                for (int i = 0; i < weights.Length; i++)
                {
                    var w = weights[i];
                    float sum = Weight(w.boneIndex0,w.weight0) + Weight(w.boneIndex1,w.weight1) +
                        Weight(w.boneIndex2,w.weight2) + Weight(w.boneIndex3,w.weight3);
                    if (sum >= .5f) groups[foot].Add(i);
                }
                float Weight(int index,float value) => value > 0f && (bones[index] == joint || bones[index].IsChildOf(joint)) ? value : 0f;
                if (groups[foot].Count < 10) throw new InvalidOperationException(id + " empty distal skin " + name);
            }
            var limbGroups = new List<int>[] { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
            for(int limb=0;limb<4;limb++)
            {
                string name=(limb<2?"DEF-upper_arm.":"DEF-thigh.")+((limb&1)==0?"L":"R");
                var joint=Array.Find(bones,b=>b!=null&&b.name==name);
                if(joint==null)throw new InvalidOperationException(id+" missing "+name);
                for(int i=0;i<weights.Length;i++)
                {
                    var w=weights[i];float sum=Weight(w.boneIndex0,w.weight0)+Weight(w.boneIndex1,w.weight1)+Weight(w.boneIndex2,w.weight2)+Weight(w.boneIndex3,w.weight3);
                    if(sum>=.5f)limbGroups[limb].Add(i);
                }
                float Weight(int index,float value)=>value>0&&(bones[index]==joint||bones[index].IsChildOf(joint))?value:0;
                if(limbGroups[limb].Count<groups[limb].Count)throw new InvalidOperationException(id+" invalid limb mask "+name);
            }
            leftForeLimb=limbGroups[0].ToArray();rightForeLimb=limbGroups[1].ToArray();
            leftRearLimb=limbGroups[2].ToArray();rightRearLimb=limbGroups[3].ToArray();
            leftForeSupport = groups[0].ToArray(); rightForeSupport = groups[1].ToArray();
            leftRearSupport = groups[2].ToArray(); rightRearSupport = groups[3].ToArray();
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
