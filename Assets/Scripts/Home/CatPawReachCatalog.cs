using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Measured source contact poses in CatBreedVisualTag local units. Each sample
/// is grounded at masked skin minY = 0; add GroundClearance in world units only
/// after transforming a point. No gameplay visual scale is baked into the data.
/// </summary>
public sealed class CatPawReachCatalog : ScriptableObject
{
    public const string ResourceName = "Home/CatPawReachCatalog";
    public const float GroundClearance = .008f;
    public const int SurfaceGeometryVersion = 2;
    public const int BodySurfaceVersion = 1;
    [Serializable] public sealed class BodyPart { public int[] vertices=Array.Empty<int>(),triangles=Array.Empty<int>(); }
    [Serializable] public sealed class BodyRegion
    { public string region; public PawVertex[] skin=Array.Empty<PawVertex>(); public int[] triangles=Array.Empty<int>(); public BodyPart[] parts=Array.Empty<BodyPart>(); }
    public const int SurfaceSampleCount = 33;

    [Serializable] public sealed class PawInfluence
    {
        public string bonePath;
        public int sourceBone;
        public Vector3 bindPosition;
        public float weight;
    }
    [Serializable] public sealed class PawVertex
    {
        public int vertexIndex;
        public bool distal;
        public PawInfluence[] influences = Array.Empty<PawInfluence>();
    }

    public enum SkinMotion { Fixed, Chest, LeftUpper, LeftFore, RightUpper, RightFore }
    [Serializable] public sealed class SkinBone
    {
        public string path;
        public SkinMotion motion;
    }

    [Serializable]
    public struct ArmChain
    {
        public Vector3 upper, fore, hand;
        public Quaternion upperRotation, foreRotation, handRotation;
        public Quaternion upperLocalRotation, foreLocalRotation, handLocalRotation;
        public float upperLength, foreLength;
        public float Reach => upperLength + foreLength;
    }

    [Serializable]
    public sealed class Sample
    {
        public float phase;
        public float sourceMinimumY;
        // Source bone-to-tag matrices already include the same minY grounding.
        // Immutable bind positions/weights remain in the entry, not each phase.
        public Matrix4x4[] skinMatrices = Array.Empty<Matrix4x4>();
        public Vector3 torsoPivot, pelvis, head, leftRear, rightRear;
        public Quaternion torsoRotation, torsoLocalRotation;
        public ArmChain left, right;
        public Vector3[] leftPaw = Array.Empty<Vector3>(), rightPaw = Array.Empty<Vector3>();
        public CatBodyGuardCatalog.Probe[] bodyProbes = Array.Empty<CatBodyGuardCatalog.Probe>();
    }

    [Serializable]
    public sealed class Entry
    {
        public string breedId, stateName;
        public CatActivityPose pose;
        public Sample[] samples = Array.Empty<Sample>();
        public PawVertex[] leftPaw = Array.Empty<PawVertex>(), rightPaw = Array.Empty<PawVertex>();
        // Triangle corners index the unchanged per-side PawVertex arrays.
        // No Mesh/FBX/clip dependency is introduced. Empty on legacy Body data.
        public int surfaceGeometryVersion;
        public int bodySurfaceVersion;
        public BodyRegion[] bodySurface=Array.Empty<BodyRegion>();
        // Separate collision skin: the original distal endpoint arrays stay
        // byte-for-byte equivalent in vertex order, path, bind point and weight.
        public SkinBone[] skinBones = Array.Empty<SkinBone>();
        public PawVertex[] leftArmSkin = Array.Empty<PawVertex>(), rightArmSkin = Array.Empty<PawVertex>();
        public int[] leftArmTriangles = Array.Empty<int>(), rightArmTriangles = Array.Empty<int>();
        public int[] leftPawTriangles = Array.Empty<int>(), rightPawTriangles = Array.Empty<int>();
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();
    [SerializeField] private int indexedEntryCount=-1;
    public int Count => indexedEntryCount>=0?indexedEntryCount:entries.Length;
    public static bool SupportsSurface(CatActivityPose pose)=>pose==CatActivityPose.Scratch||pose==CatActivityPose.Paw;
    public static string BodyResourceName(string breedId)=>"Home/PawReach/"+breedId+"/Body";
    public static string SurfaceResourceName(string breedId,CatActivityPose pose)=>"Home/PawReach/"+breedId+"/Surface-"+pose;
    sealed class ResourceSlot
    {
        public readonly string path;
        public ResourceRequest request;
        public CatPawReachCatalog asset;
        public bool failed;
        public ResourceSlot(string path){this.path=path;}
    }
    readonly struct SurfaceKey : IEquatable<SurfaceKey>
    {
        public readonly string breed;public readonly CatActivityPose pose;
        public SurfaceKey(string breed,CatActivityPose pose){this.breed=breed;this.pose=pose;}
        public bool Equals(SurfaceKey other)=>breed==other.breed&&pose==other.pose;
        public override bool Equals(object value)=>value is SurfaceKey key&&Equals(key);
        public override int GetHashCode()=>(breed!=null?breed.GetHashCode():0)*397^(int)pose;
    }
    static ResourceSlot root=new ResourceSlot(ResourceName);
    static readonly Dictionary<string,ResourceSlot> bodies=new Dictionary<string,ResourceSlot>();
    static readonly Dictionary<SurfaceKey,ResourceSlot> surfaces=new Dictionary<SurfaceKey,ResourceSlot>();
    static CatPawReachCatalog Read(ResourceSlot slot,out bool loading)
    {
        loading=false;if(slot.asset!=null)return slot.asset;if(slot.failed)return null;
        if(!Application.isPlaying)
        {slot.asset=Resources.Load<CatPawReachCatalog>(slot.path);slot.failed=slot.asset==null;return slot.asset;}
        if(slot.request==null)slot.request=Resources.LoadAsync<CatPawReachCatalog>(slot.path);
        if(!slot.request.isDone){loading=true;return null;}
        // Accessing asset before isDone can force synchronous completion.
        slot.asset=slot.request.asset as CatPawReachCatalog;slot.request=null;slot.failed=slot.asset==null;
        return slot.asset;
    }
    static ResourceSlot BodySlot(string breedId)
    {
        if(!bodies.TryGetValue(breedId,out var slot))
        {slot=new ResourceSlot(BodyResourceName(breedId));bodies.Add(breedId,slot);}
        return slot;
    }
    static ResourceSlot SurfaceSlot(string breedId,CatActivityPose pose)
    {
        var key=new SurfaceKey(breedId,pose);
        if(!surfaces.TryGetValue(key,out var slot))
        {slot=new ResourceSlot(SurfaceResourceName(breedId,pose));surfaces.Add(key,slot);}
        return slot;
    }
    // A superseded selection may still have a request in the native loader.
    // Do not replace an Animator until all requests from this data family settle.
    public static bool HasPendingLoads
    {
        get
        {
            if(CatMeshContactSurface.HasPendingLoad||CatJumpClearanceCatalog.HasPendingLoad)return true;
            if (root.request != null && !root.request.isDone) return true;
            foreach (var slot in bodies.Values) if (slot.request != null && !slot.request.isDone) return true;
            foreach (var slot in surfaces.Values) if (slot.request != null && !slot.request.isDone) return true;
            return false;
        }
    }
    public static CatPawReachCatalog Load()=>Read(root,out _);
    public static Entry LoadSurface(string breedId,CatActivityPose pose,out bool loading)
    {
        loading=false;
        if(string.IsNullOrEmpty(breedId)||!SupportsSurface(pose))return null;
        var asset=Read(SurfaceSlot(breedId,pose),out loading);
        return asset!=null?asset.FindLocal(breedId,pose):null;
    }
    public static void Preload(string breedId)
    {
        CatMeshContactSurface.Preload();
        CatJumpClearanceCatalog.Preload();
        Read(root,out _);if(string.IsNullOrEmpty(breedId))return;
        Read(BodySlot(breedId),out _);Read(SurfaceSlot(breedId,CatActivityPose.Scratch),out _);
    }
    public static bool IsBodyReady(string breedId)
    {
        if(string.IsNullOrEmpty(breedId))return false;
        var index=Read(root,out _);var body=Read(BodySlot(breedId),out _);
        return index!=null&&body!=null&&body.entries.Length>0;
    }
    public static bool IsReadyFor(string breedId)
    {
        if(!CatMeshContactSurface.IsReady||!CatJumpClearanceCatalog.IsReady||!IsBodyReady(breedId))return false;
        var surface=LoadSurface(breedId,CatActivityPose.Scratch,out bool loading);
        return !loading&&surface?.samples!=null&&surface.samples.Length>0&&
            (!Application.isPlaying||CatJumpWeightedBody.PrepareSourceGeometry(breedId));
    }
    public static bool IsLoadingFor(string breedId)
    {
        Read(root,out bool main);
        if(string.IsNullOrEmpty(breedId))return main;
        Read(BodySlot(breedId),out bool body);Read(SurfaceSlot(breedId,CatActivityPose.Scratch),out bool surface);
        return main||body||surface||!CatMeshContactSurface.IsReady||!CatJumpClearanceCatalog.IsReady;
    }
    public Entry Find(string breedId,CatActivityPose pose)
    {
        if(indexedEntryCount<0)return FindLocal(breedId,pose);
        if(string.IsNullOrEmpty(breedId))return null;
        var shard=Read(BodySlot(breedId),out _);return shard!=null?shard.FindLocal(breedId,pose):null;
    }
    Entry FindLocal(string breedId,CatActivityPose pose)
    {
        foreach(var entry in entries)
            if(entry!=null&&entry.breedId==breedId&&entry.pose==pose)return entry;
        return null;
    }
    public static Vector3 WorldPoint(Transform visual, Vector3 groundedLocalPoint) =>
        visual.TransformPoint(groundedLocalPoint) + Vector3.up * GroundClearance;

#if UNITY_EDITOR
    public void EditorConfigure(Entry[] values,int indexedCount=-1)
    {entries=values??Array.Empty<Entry>();indexedEntryCount=indexedCount;}
    // QA can measure a genuine cold async load in an isolated, idle copied-save
    // session. Only this catalog family's ScriptableObjects are unloaded.
    public static void EditorClearResourceCacheForQa()
    {
        if(root.request!=null&&!root.request.isDone)throw new InvalidOperationException("Catalog load is still running");
        foreach(var slot in bodies.Values)if(slot.request!=null&&!slot.request.isDone)throw new InvalidOperationException("Body load is still running");
        foreach(var slot in surfaces.Values)if(slot.request!=null&&!slot.request.isDone)throw new InvalidOperationException("Surface load is still running");
        UnloadForQa(root);
        foreach(var slot in bodies.Values)UnloadForQa(slot);
        foreach(var slot in surfaces.Values)UnloadForQa(slot);
        root=new ResourceSlot(ResourceName);bodies.Clear();surfaces.Clear();
    }
    static void UnloadForQa(ResourceSlot slot)
    {
        var asset=slot.asset!=null?slot.asset:slot.request!=null&&slot.request.isDone?slot.request.asset as CatPawReachCatalog:null;
        if(asset!=null)Resources.UnloadAsset(asset);
    }
#endif
}
