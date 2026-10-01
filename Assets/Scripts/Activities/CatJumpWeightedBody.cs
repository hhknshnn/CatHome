using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Optional exact weighted NativeJump body geometry. Source and root
/// mapping are inputs; this never samples an Animator or moves a live actor.</summary>
public static class CatJumpWeightedBody
{
    // Match the five-millimetre rendered-body acceptance. Limb support keeps
    // its tighter two-millimetre gate; this replaces the former 15 mm body gate.
    const float Tolerance=.005f, Shell=.001f;
    static readonly string[] Names={"pelvis","chest","neck","head"};
    static readonly Dictionary<CatJumpClearanceCatalog.Sample,Region[]> frames=new Dictionary<CatJumpClearanceCatalog.Sample,Region[]>();
    static CatJumpClearanceCatalog.Entry cachedEntry;
    static int preparingSample, preparingRegion, preparedFrame=-1;
    static readonly bool[] topologyChecked=new bool[4],topologyValid=new bool[4];
    public static int RegionQueries { get; private set; }
    public static int RefinedTriangles { get; private set; }
    public static int CachedRegions { get; private set; }

    // Build immutable source poses during the existing room/breed readiness
    // window. Bounded small batches keep first-use skin binding out of walking
    // without stretching 252 tiny regions across 252 low-FPS loading frames.
    // This never caches a scene collision result or changes an actor pose.
    public static bool PrepareSourceGeometry(string breed)
    {
        var entry=CatJumpClearanceCatalog.Load()?.Find(breed);
        if(entry?.samples==null)return false;
        SelectEntry(entry);
        if(preparingSample>=entry.samples.Length)return true;
        if(preparedFrame==Time.frameCount)return false;
        preparedFrame=Time.frameCount;
        long began=System.Diagnostics.Stopwatch.GetTimestamp();int remaining=8;
        while(preparingSample<entry.samples.Length)
        {
            var sample=entry.samples[preparingSample];
            if(sample==null||(sample.phase>CatJumpMotion.Takeoff+.000001f&&sample.phase<CatJumpMotion.Touchdown-.000001f)||!HasData(entry,sample))
            {preparingSample++;preparingRegion=0;continue;}
            GetRegion(entry,sample,preparingRegion);
            if(++preparingRegion==4)
            {
                if(!CatJumpLimbClearance.Prepare(entry,sample)){preparingRegion=3;return false;}
                preparingRegion=0;preparingSample++;
            }
            if(--remaining==0||(System.Diagnostics.Stopwatch.GetTimestamp()-began)*1000d/System.Diagnostics.Stopwatch.Frequency>=1d)break;
        }
        return preparingSample>=entry.samples.Length;
    }

    static void SelectEntry(CatJumpClearanceCatalog.Entry entry)
    {
        if(ReferenceEquals(cachedEntry,entry))return;
        frames.Clear();cachedEntry=entry;Array.Clear(topologyChecked,0,4);Array.Clear(topologyValid,0,4);CachedRegions=0;
        preparingSample=preparingRegion=0;preparedFrame=-1;
    }

    static Region GetRegion(CatJumpClearanceCatalog.Entry entry,CatJumpClearanceCatalog.Sample sample,int regionIndex)
    {
        SelectEntry(entry);
        if(!topologyChecked[regionIndex])
        {topologyValid[regionIndex]=TopologyValid(entry.bodySurface[regionIndex],regionIndex);topologyChecked[regionIndex]=true;}
        if(!topologyValid[regionIndex])return null;
        if(!frames.TryGetValue(sample,out var regions))
        {
            if(frames.Count>=128){frames.Clear();CachedRegions=0;}
            regions=new Region[4];frames.Add(sample,regions);
        }
        if(regions[regionIndex]==null)
        {regions[regionIndex]=BuildRegion(entry,sample,regionIndex);if(regions[regionIndex]!=null)CachedRegions++;}
        return regions[regionIndex];
    }

    sealed class Region
    {
        public CatPawReachCatalog.BodyRegion source;
        public Vector3[] local,world;
        public Bounds[] parts;
        public CatBodyGuardBox[] query;
        public Func<int,Collider,bool> refinement;
        public CatMovement actor;
        public Matrix4x4 matrix;
        public Vector3 correction;
        public bool worldFilled;
        public bool Clear(CatMovement cat,Matrix4x4 transform,Vector3 offset)
        {
            actor=cat;matrix=transform;correction=offset;worldFilled=false;
            for(int part=0;part<parts.Length;part++)
            {
                var box=Transform(parts[part],matrix,correction);box.Expand(Shell*2);
                query[part]=new CatBodyGuardBox{centre=box.center,halfExtents=box.extents,rotation=Quaternion.identity};
            }
            // The shared box query also checks entirely occupied nonconvex
            // volumes when PhysX primitive MTD/Overlap do not report them.
            return actor.IsInteractionBoxesClear(query,Tolerance,refinement);
        }
        public bool Refine(int index,Collider solid)
        {
            if(!worldFilled)
            {for(int v=0;v<local.Length;v++)world[v]=matrix.MultiplyPoint3x4(local[v])+correction;worldFilled=true;}
            // The shared broad query identifies both the actual overlapping
            // collider and this whole-face part. Do not revisit all parts in
            // the region's much larger, mostly empty overall bounding box.
            if(index<0||index>=source.parts.Length)return false;
            var triangles=source.parts[index].triangles;
            for(int t=0;t<triangles.Length;t+=3)
            {
                RefinedTriangles++;
                if(!actor.IsInteractionTriangleClear(world[triangles[t]],world[triangles[t+1]],world[triangles[t+2]],solid,Tolerance,Shell))
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>False means optional data is absent/invalid: use the existing
    /// body-part gate. True supplies a fresh physics result in clear.</summary>
    public static bool TryClear(CatMovement actor,CatJumpClearanceCatalog.Entry entry,
        CatJumpClearanceCatalog.Sample sample,Matrix4x4 sourceMatrix,Vector3 correction,out bool clear)
    {
        clear=false;
        for(int region=0;region<4;region++)
        {
            if(!TryClearRegion(actor,entry,sample,region,sourceMatrix,correction,out bool regionClear))return false;
            if(!regionClear)return true;
        }
        clear=true;return true;
    }
    // Production calls this ONLY after the matching existing capsule region
    // refuses. Cache immutable source geometry, never a physics permission.
    public static bool TryClearRegion(CatMovement actor,CatJumpClearanceCatalog.Entry entry,
        CatJumpClearanceCatalog.Sample sample,int regionIndex,Matrix4x4 sourceMatrix,Vector3 correction,out bool clear)
    {
        clear=false;if(actor==null||regionIndex<0||regionIndex>=4||!HasData(entry,sample))return false;
        var region=GetRegion(entry,sample,regionIndex);if(region==null)return false;
        RegionQueries++;clear=region.Clear(actor,sourceMatrix,correction);return true;
    }
    public static bool HasData(CatJumpClearanceCatalog.Entry entry,CatJumpClearanceCatalog.Sample sample)
    {
        return entry!=null&&sample!=null&&entry.weightedBodyVersion==CatJumpClearanceCatalog.WeightedBodyVersion&&
            entry.bodySurface!=null&&entry.bodySurface.Length==4&&entry.bodyBones!=null&&entry.bodyBones.Length>0&&
            sample.bodySkinMatrices!=null&&sample.bodySkinMatrices.Length==entry.bodyBones.Length;
    }
    static Region BuildRegion(CatJumpClearanceCatalog.Entry entry,CatJumpClearanceCatalog.Sample sample,int r)
    {
        var source=entry.bodySurface[r];
        var frame=CatPawWeightedSkin.Build(entry.bodyBones,sample.bodySkinMatrices,Matrix4x4.identity,source.skin,Vector3.zero);
        if(frame==null)return null;
        var region=new Region{source=source,local=new Vector3[source.skin.Length],world=new Vector3[source.skin.Length],
            parts=new Bounds[source.parts.Length],query=new CatBodyGuardBox[source.parts.Length]};
        CatPawWeightedSkin.Fill(frame,Vector3.zero,Quaternion.identity,false,true,default,default,default,region.local);
        for(int p=0;p<region.parts.Length;p++)
        {
            var indices=source.parts[p].vertices;var box=new Bounds(region.local[indices[0]],Vector3.zero);
            foreach(int vertex in indices)box.Encapsulate(region.local[vertex]);region.parts[p]=box;
        }
        region.refinement=region.Refine;return region;
    }
    static bool TopologyValid(CatPawReachCatalog.BodyRegion region,int index)
    {
        if(region==null||region.region!=Names[index]||region.skin==null||region.skin.Length==0||region.triangles==null||
            region.triangles.Length==0||region.triangles.Length%3!=0||region.parts==null||region.parts.Length==0||region.parts.Length>32)return false;
        var covered=new HashSet<(int,int,int)>();
        foreach(var part in region.parts)
        {
            if(part?.vertices==null||part.vertices.Length==0||part.triangles==null||part.triangles.Length==0||part.triangles.Length%3!=0)return false;
            var vertices=new HashSet<int>(part.vertices);
            foreach(int v in vertices)if(v<0||v>=region.skin.Length)return false;
            for(int t=0;t<part.triangles.Length;t+=3)
            {
                int a=part.triangles[t],b=part.triangles[t+1],c=part.triangles[t+2];
                if(!vertices.Contains(a)||!vertices.Contains(b)||!vertices.Contains(c))return false;
                covered.Add((a,b,c));
            }
        }
        for(int t=0;t<region.triangles.Length;t+=3)
            if(!covered.Contains((region.triangles[t],region.triangles[t+1],region.triangles[t+2])))return false;
        return true;
    }
    static Bounds Transform(Bounds local,Matrix4x4 matrix,Vector3 correction)
    {
        Vector3 e=local.extents;
        Vector3 world=new Vector3(Mathf.Abs(matrix.m00)*e.x+Mathf.Abs(matrix.m01)*e.y+Mathf.Abs(matrix.m02)*e.z,
            Mathf.Abs(matrix.m10)*e.x+Mathf.Abs(matrix.m11)*e.y+Mathf.Abs(matrix.m12)*e.z,
            Mathf.Abs(matrix.m20)*e.x+Mathf.Abs(matrix.m21)*e.y+Mathf.Abs(matrix.m22)*e.z);
        return new Bounds(matrix.MultiplyPoint3x4(local.center)+correction,world*2);
    }
}
