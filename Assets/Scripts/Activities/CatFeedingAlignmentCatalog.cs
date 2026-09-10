using System;
using UnityEngine;

/// <summary>Placement measured from the unmodified vendor Eating clip.</summary>
public sealed class CatFeedingAlignmentCatalog : ScriptableObject
{
    [Serializable] public sealed class Entry
    {
        public string breedId;
        public Vector3 mouthOffset;
        public float rootHeight;
    }
    public AnimationClip sourceClip;
    public Entry[] entries=Array.Empty<Entry>();
    static CatFeedingAlignmentCatalog cached;
    public static CatFeedingAlignmentCatalog Load()
    {if(cached==null)cached=Resources.Load<CatFeedingAlignmentCatalog>("CatFeedingAlignmentCatalog");return cached;}
    public Entry Find(string id)=>Array.Find(entries,e=>e.breedId==id);
}
