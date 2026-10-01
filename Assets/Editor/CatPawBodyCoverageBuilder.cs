using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Whole-body face ownership and weighted bindings; never changes a source mesh.</summary>
public static class CatPawBodyCoverageBuilder
{
    static readonly string[] Names={"pelvis","chest","neck","head"};
    public static CatPawReachCatalog.BodyRegion[] Build(SkinnedMeshRenderer skin,Transform animationRoot,IReadOnlyList<int> contactMask)
    {
        var mesh=skin.sharedMesh;var positions=mesh.vertices;var weights=mesh.boneWeights;var bones=skin.bones;var bind=mesh.bindposes;
        var groups=Enumerable.Repeat(-1,positions.Length).ToArray();
        foreach(int v in contactMask)groups[v]=CatJumpClearanceBuilder.ClassifyBodyRegion(weights[v],bones);
        var triangles=mesh.triangles;var result=new CatPawReachCatalog.BodyRegion[4];
        for(int r=0;r<4;r++)
        {
            int[] faces=CatJumpSurfaceCoverageBuilder.RegionTriangles(triangles,groups,r);
            var fit=CatJumpSurfaceCoverageBuilder.Fit(positions,faces,Names[r]);
            int[] original=faces.Distinct().OrderBy(v=>v).ToArray();var local=new Dictionary<int,int>();
            var vertices=new CatPawReachCatalog.PawVertex[original.Length];
            for(int i=0;i<original.Length;i++)
            {
                int v=original[i];local[v]=i;var w=weights[v];var influences=new List<CatPawReachCatalog.PawInfluence>(4);
                Add(w.boneIndex0,w.weight0);Add(w.boneIndex1,w.weight1);Add(w.boneIndex2,w.weight2);Add(w.boneIndex3,w.weight3);
                vertices[i]=new CatPawReachCatalog.PawVertex{vertexIndex=v,influences=influences.ToArray()};
                void Add(int bone,float amount){if(amount>0)influences.Add(new CatPawReachCatalog.PawInfluence{
                    bonePath=AnimationUtility.CalculateTransformPath(bones[bone],animationRoot),bindPosition=bind[bone].MultiplyPoint3x4(positions[v]),weight=amount});}
            }
            var partitions=fit.parts.Select(_=>new List<int>()).ToArray();
            for(int t=0;t<faces.Length;t+=3)
            {
                int owner=-1;
                for(int p=0;p<fit.parts.Length;p++)
                    if(Contains(fit.parts[p],positions[faces[t]])>=.0039f&&Contains(fit.parts[p],positions[faces[t+1]])>=.0039f&&Contains(fit.parts[p],positions[faces[t+2]])>=.0039f){owner=p;break;}
                if(owner<0)throw new InvalidOperationException("Uncovered complete body face: "+Names[r]);
                for(int k=0;k<3;k++)partitions[owner].Add(local[faces[t+k]]);
            }
            result[r]=new CatPawReachCatalog.BodyRegion{region=Names[r],skin=vertices,triangles=faces.Select(v=>local[v]).ToArray(),
                parts=partitions.Where(p=>p.Count>0).Select(p=>new CatPawReachCatalog.BodyPart{triangles=p.ToArray(),vertices=p.Distinct().OrderBy(v=>v).ToArray()}).ToArray()};
            if(result[r].parts.Length>32)throw new InvalidOperationException("Body partition exceeded its bound");
        }
        return result;
    }
    static float Contains(CatBodyGuardCatalog.Probe probe,Vector3 point)
    {
        Vector3 axis=probe.end-probe.start;float t=axis.sqrMagnitude>1e-12f?Mathf.Clamp01(Vector3.Dot(point-probe.start,axis)/axis.sqrMagnitude):0;
        return probe.radius-Vector3.Distance(point,probe.start+axis*t);
    }
    public static CatPawReachCatalog.PawVertex[] Clone(CatPawReachCatalog.PawVertex[] source)=>source.Select(v=>new CatPawReachCatalog.PawVertex{
        vertexIndex=v.vertexIndex,distal=v.distal,influences=v.influences.Select(i=>new CatPawReachCatalog.PawInfluence{
            bonePath=i.bonePath,bindPosition=i.bindPosition,weight=i.weight,sourceBone=i.sourceBone}).ToArray()}).ToArray();
}
