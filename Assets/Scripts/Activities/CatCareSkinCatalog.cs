using System;
using UnityEngine;

/// <summary>Immutable source skinning data, with no Mesh/FBX/clip dependency.</summary>
public sealed class CatCareSkinCatalog : ScriptableObject
{
    public const string ResourceName="CatCareSkinCatalog";
    public const string AssetPath="Assets/Resources/CatCareSkinCatalog.asset";
    [Serializable] public sealed class Profile
    {
        public string sourceKey,sourceHash,meshName;
        public int meshVertexCount;
        public string[] bonePaths=Array.Empty<string>();
        public int[] vertexIndices=Array.Empty<int>(),starts=Array.Empty<int>(),bones=Array.Empty<int>();
        public Vector3[] bindPoints=Array.Empty<Vector3>();
        public float[] weights=Array.Empty<float>();
    }
    [Serializable] public sealed class Binding{public string breedId;public int profile;}
    [SerializeField] Profile[] profiles=Array.Empty<Profile>();
    [SerializeField] Binding[] breeds=Array.Empty<Binding>();
    static CatCareSkinCatalog cached;
    public static CatCareSkinCatalog Load(){if(cached==null)cached=Resources.Load<CatCareSkinCatalog>(ResourceName);return cached;}
    public Profile Find(string breedId)
    {
        foreach(var binding in breeds)if(binding.breedId==breedId&&binding.profile>=0&&binding.profile<profiles.Length)return profiles[binding.profile];
        return null;
    }
#if UNITY_EDITOR
    public void EditorConfigure(Profile[] source,Binding[] bindings){profiles=source;breeds=bindings;cached=this;}
#endif
}
