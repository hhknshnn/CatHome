using System;
using UnityEngine;

/// <summary>Tiny baked mouth skinning data; gameplay never reads the full cat mesh.</summary>
public sealed class CatSipMouthCatalog : ScriptableObject
{
    public const string ResourceName="CatSipMouthCatalog";
    [Serializable] public sealed class Influence
    {
        public string bonePath;
        public Vector3 bindPosition;
        public float weight;
    }
    [Serializable] public sealed class Vertex
    {
        public int vertexIndex;
        public Influence[] influences=Array.Empty<Influence>();
    }
    [Serializable] public sealed class Profile
    {
        public string sourceKey;
        public Vertex[] vertices=Array.Empty<Vertex>();
    }
    [Serializable] public sealed class BreedBinding
    {
        public string breedId;
        public int profileIndex;
    }
    [SerializeField] Profile[] profiles=Array.Empty<Profile>();
    [SerializeField] BreedBinding[] breeds=Array.Empty<BreedBinding>();
    public int ProfileCount=>profiles.Length;
    public int BreedCount=>breeds.Length;
    static CatSipMouthCatalog cached;
    public static CatSipMouthCatalog Load()
    {if(cached==null)cached=Resources.Load<CatSipMouthCatalog>(ResourceName);return cached;}
    public Profile Find(string breedId)
    {
        foreach(var entry in breeds)
            if(entry.breedId==breedId&&entry.profileIndex>=0&&entry.profileIndex<profiles.Length)return profiles[entry.profileIndex];
        return null;
    }
#if UNITY_EDITOR
    public void EditorConfigure(Profile[] values,BreedBinding[] bindings)
    {profiles=values??Array.Empty<Profile>();breeds=bindings??Array.Empty<BreedBinding>();cached=this;}
#endif
}
