using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Read-only source limb coverage for a ground landing. Only immutable
/// source positions are cached; every placement checks the current scene.</summary>
public static class CatJumpLimbClearance
{
    static CatJumpClearanceCatalog.Entry cachedEntry;
    static CatCareSkinCatalog.Profile profile;
    static int[] boneMap;
    static int[][] limbSlots;
    public static string LastRejection {get;private set;}
    static readonly Dictionary<CatJumpClearanceCatalog.Sample,Frame> frames=new Dictionary<CatJumpClearanceCatalog.Sample,Frame>();
    sealed class Frame
    {
        public readonly Vector3[][] local=new Vector3[4][],world=new Vector3[4][];
        public readonly CatBodyGuardBox[] boxes=new CatBodyGuardBox[4];
        public Func<int,Collider,bool> refinement;
        public bool Clear(CatMovement actor,Matrix4x4 matrix,Vector3 correction)
        {
            for(int leg=0;leg<4;leg++)
            {
                var points=world[leg];
                for(int v=0;v<points.Length;v++)points[v]=matrix.MultiplyPoint3x4(local[leg][v])+correction;
                var bounds=new Bounds(points[0],Vector3.zero);
                for(int v=1;v<points.Length;v++)bounds.Encapsulate(points[v]);
                bounds.Expand(.002f);
                boxes[leg]=new CatBodyGuardBox{centre=bounds.center,halfExtents=bounds.extents,rotation=Quaternion.identity};
            }
            bool backfaces=Physics.queriesHitBackfaces;
            try{Physics.queriesHitBackfaces=true;return actor.IsInteractionBoxesClear(boxes,.002f,refinement);}
            finally{Physics.queriesHitBackfaces=backfaces;}
        }
        public bool Refine(int leg,Collider solid)
        {
            if(solid is BoxCollider box)
            {
                Vector3 scale=box.transform.lossyScale,half=box.size*.5f;
                foreach(Vector3 point in world[leg])
                {
                    Vector3 localPoint=box.transform.InverseTransformPoint(point)-box.center;
                    Vector3 remaining=half-new Vector3(Mathf.Abs(localPoint.x),Mathf.Abs(localPoint.y),Mathf.Abs(localPoint.z));
                    float depth=Mathf.Min(remaining.x*Mathf.Abs(scale.x),Mathf.Min(remaining.y*Mathf.Abs(scale.y),remaining.z*Mathf.Abs(scale.z)));
                    if(depth>.002f){LastRejection=box.name+" leg="+leg+" boxDepth="+depth;return false;}
                }
                return true;
            }
            var mesh=solid as MeshCollider;
            if(mesh==null||mesh.sharedMesh==null){LastRejection=solid.name+" unsupported "+solid.GetType().Name;return false;}
            Bounds bounds=mesh.bounds;Matrix4x4 matrix=mesh.transform.localToWorldMatrix;
            float distance=bounds.size.magnitude+.01f;
            foreach(Vector3 point in world[leg])
            {
                if(!bounds.Contains(point))continue;
                int exits=0,entries=0;
                for(int ray=0;ray<3;ray++)
                {
                    Vector3 axis=ray==0?Vector3.up:ray==1?Vector3.right:Vector3.forward;
                    if(!mesh.Raycast(new Ray(point,axis),out var hit,distance)){entries++;continue;}
                    if(!CatMeshContactSurface.TryOriginalTriangleNormal(mesh.sharedMesh,matrix,hit.triangleIndex,out var normal))return false;
                    if(Vector3.Dot(normal,axis)>0)exits++;else entries++;
                    if(exits>=2||entries>=2)break;
                }
                if(exits<2)continue;
                if(!CatMeshContactSurface.TryMetric(mesh.sharedMesh,mesh.transform,matrix,point,out float depth,out _)||depth>.002f)
                {LastRejection=mesh.name+" leg="+leg+" meshDepth="+depth+" point="+point;return false;}
            }
            return true;
        }
    }
    public static bool Prepare(CatJumpClearanceCatalog.Entry entry,CatJumpClearanceCatalog.Sample sample)
    {
        if(entry==null||sample==null||sample.supportSkinMatrices==null||sample.supportSkinMatrices.Length!=entry.supportBonePaths.Length)return false;
        if(!ReferenceEquals(cachedEntry,entry))
        {
            frames.Clear();cachedEntry=entry;profile=CatCareSkinCatalog.Load()?.Find(entry.breedId);boneMap=null;limbSlots=null;
            var breed=CatBreedCatalog.Load()?.Find(entry.breedId);
            if(profile==null||breed==null||profile.sourceKey!=entry.supportProfileKey||profile.sourceHash!=entry.supportProfileHash)return false;
            var paths=new Dictionary<string,int>();
            for(int b=0;b<entry.supportBonePaths.Length;b++)paths[entry.supportBonePaths[b]]=b;
            boneMap=new int[profile.bonePaths.Length];
            for(int b=0;b<boneMap.Length;b++)if(!paths.TryGetValue(profile.bonePaths[b],out boneMap[b])){boneMap=null;return false;}
            var lookup=new Dictionary<int,int>();for(int v=0;v<profile.vertexIndices.Length;v++)lookup[profile.vertexIndices[v]]=v;
            limbSlots=new int[4][];
            for(int leg=0;leg<4;leg++)
            {
                var indices=new HashSet<int>();foreach(int index in breed.SupportLimbVertices(leg))indices.Add(index);
                foreach(int index in breed.SupportPawVertices(leg))indices.Add(index);
                var slots=new List<int>();foreach(int index in indices)if(lookup.TryGetValue(index,out int slot))slots.Add(slot);
                if(slots.Count==0){limbSlots=null;return false;}limbSlots[leg]=slots.ToArray();
            }
        }
        if(boneMap==null||limbSlots==null)return false;
        if(frames.ContainsKey(sample))return true;
        var frame=new Frame();
        for(int leg=0;leg<4;leg++)
        {
            int[] slots=limbSlots[leg];frame.local[leg]=new Vector3[slots.Length];frame.world[leg]=new Vector3[slots.Length];
            for(int v=0;v<slots.Length;v++)
            {
                int slot=slots[v];Vector3 point=Vector3.zero;
                for(int w=profile.starts[slot];w<profile.starts[slot+1];w++)
                    point+=sample.supportSkinMatrices[boneMap[profile.bones[w]]].MultiplyPoint3x4(profile.bindPoints[w])*profile.weights[w];
                frame.local[leg][v]=point;
            }
        }
        frame.refinement=frame.Refine;frames.Add(sample,frame);return true;
    }
    public static bool IsClear(CatMovement actor,CatJumpClearanceCatalog.Entry entry,CatJumpClearanceCatalog.Sample sample,Matrix4x4 matrix,Vector3 correction)
        =>Prepare(entry,sample)&&frames[sample].Clear(actor,matrix,correction);
}
