using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small anatomical envelopes from the measured skin's strongest bone weight.
/// Every measured vertex belongs to one region; no contact vertex is omitted.
/// Partition once per cached source geometry, then fit without allocations.
/// </summary>
public static class CatPawSurfaceRegions
{
    public static CatBodyGuardBox FitBox(Vector3[] points,int[] indices,Vector3 surfaceNormal)
    {
        Vector3 normal=surfaceNormal.normalized;
        if(normal.sqrMagnitude<.5f)normal=Vector3.up;
        Vector3 u=Vector3.ProjectOnPlane(Vector3.up,normal).normalized;
        if(u.sqrMagnitude<.5f)u=Vector3.ProjectOnPlane(Vector3.forward,normal).normalized;
        Vector3 v=Vector3.Cross(normal,u).normalized,mean=Vector3.zero;
        foreach(int i in indices)mean+=points[i];mean/=indices.Length;
        // Principal direction within the actual surface tangent plane. The
        // normal axis stays exact, avoiding capsule bulge through the surface.
        float uu=0,uv=0,vv=0;
        foreach(int i in indices)
        {Vector3 p=points[i]-mean;float a=Vector3.Dot(p,u),b=Vector3.Dot(p,v);uu+=a*a;uv+=a*b;vv+=b*b;}
        float angle=.5f*Mathf.Atan2(2*uv,uu-vv);
        Vector3 tangent=u*Mathf.Cos(angle)+v*Mathf.Sin(angle);
        Quaternion rotation=Quaternion.LookRotation(tangent,normal),inverse=Quaternion.Inverse(rotation);
        var bounds=new Bounds(inverse*points[indices[0]],Vector3.zero);
        foreach(int i in indices)bounds.Encapsulate(inverse*points[i]);
        bounds.Expand(.002f); // 1 mm padding on every face, same envelope margin.
        return new CatBodyGuardBox{centre=rotation*bounds.center,halfExtents=bounds.extents,rotation=rotation};
    }
    public static int[][] Build(CatPawReachCatalog.PawVertex[] definitions, int[] triangles = null, int maximumBoneGroups = 8)
    {
        if(definitions==null||definitions.Length==0)return Array.Empty<int[]>();
        var groups=new Dictionary<string,List<int>>(StringComparer.Ordinal);
        for(int v=0;v<definitions.Length;v++)
        {
            var vertex=definitions[v];if(vertex?.influences==null||vertex.influences.Length==0)return Array.Empty<int[]>();
            string key=null;float maximum=-1;
            foreach(var influence in vertex.influences)
                if(influence.weight>maximum){maximum=influence.weight;key=influence.bonePath;}
            if(string.IsNullOrEmpty(key))return Array.Empty<int[]>();
            if(!groups.TryGetValue(key,out var indices)){indices=new List<int>();groups.Add(key,indices);}
            indices.Add(v);
        }
        // Catalog bindings use four regions per paw. Refuse unexpected data
        // rather than silently omit vertices or allow unbounded physics work.
        if(groups.Count>maximumBoneGroups)return Array.Empty<int[]>();
        // One whole bone region can bridge empty space between splayed toes.
        // Subdivide its measured vertices in that same bone's bind frame. Every
        // vertex remains in exactly one region and every fitted face retains
        // the same 1 mm padding; neither contact tolerance nor skin is reduced.
        var result=new List<int[]>(16);
        foreach(var pair in groups)
        {
            var parts=new List<int[]>{pair.Value.ToArray()};
            for(int split=0;split<3;split++)
            {
                int chosen=-1,axis=0;float spread=0;
                for(int r=0;r<parts.Count;r++)
                {
                    if(parts[r].Length<12)continue;
                    var bounds=new Bounds(BindPoint(parts[r][0],pair.Key),Vector3.zero);
                    foreach(int v in parts[r])bounds.Encapsulate(BindPoint(v,pair.Key));
                    for(int a=0;a<3;a++)if(bounds.size[a]>spread){chosen=r;axis=a;spread=bounds.size[a];}
                }
                if(chosen<0||spread<.004f)break;
                int sortAxis=axis;var indices=parts[chosen];
                Array.Sort(indices,(a,b)=>BindPoint(a,pair.Key)[sortAxis].CompareTo(BindPoint(b,pair.Key)[sortAxis]));
                int half=indices.Length/2;var first=new int[half];var second=new int[indices.Length-half];
                Array.Copy(indices,0,first,0,first.Length);Array.Copy(indices,half,second,0,second.Length);
                parts[chosen]=first;parts.Add(second);
            }
            result.AddRange(parts);
        }
        // Eight source bone groups times four bounded subregions at most.
        var partition=result.ToArray();
        return triangles==null?partition:CoverTriangles(partition,definitions.Length,triangles);
        Vector3 BindPoint(int vertex,string bone)
        {
            foreach(var influence in definitions[vertex].influences)
                if(influence.bonePath==bone)return influence.bindPosition;
            throw new InvalidOperationException("A measured vertex lost its source bone.");
        }
    }


    // A box is convex. Including all three corners in one fitted box encloses
    // its complete rendered triangle, not just the sampled corner vertices.
    // Neighbours may be duplicated across regions; no vertex is discarded.
    static int[][] CoverTriangles(int[][] partition,int vertexCount,int[] triangles)
    {
        if(triangles.Length==0||triangles.Length%3!=0)return Array.Empty<int[]>();
        var regions=new HashSet<int>[partition.Length];var owner=new int[vertexCount];
        for(int i=0;i<owner.Length;i++)owner[i]=-1;
        for(int r=0;r<partition.Length;r++)
        {
            regions[r]=new HashSet<int>(partition[r]);
            foreach(int v in partition[r])owner[v]=r;
        }
        foreach(int r in owner)if(r<0)return Array.Empty<int[]>();
        for(int t=0;t<triangles.Length;t+=3)
        {
            int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
            if(a<0||a>=vertexCount||b<0||b>=vertexCount||c<0||c>=vertexCount)return Array.Empty<int[]>();
            int chosen=owner[a],missing=Missing(chosen,a,b,c);
            Choose(owner[b]);Choose(owner[c]);
            regions[chosen].Add(a);regions[chosen].Add(b);regions[chosen].Add(c);
            void Choose(int r){int count=Missing(r,a,b,c);if(count<missing||count==missing&&r<chosen){chosen=r;missing=count;}}
        }
        var result=new int[regions.Length][];
        for(int r=0;r<regions.Length;r++)
        {result[r]=new int[regions[r].Count];regions[r].CopyTo(result[r]);Array.Sort(result[r]);}
        return result;
        int Missing(int r,int a,int b,int c)=>
            (regions[r].Contains(a)?0:1)+(regions[r].Contains(b)?0:1)+(regions[r].Contains(c)?0:1);
    }

    // Assign each complete source face to every enclosing anatomical region.
    // At least one region must cover it; malformed/incomplete topology refuses.
    public static int[][] CoveredFaces(int[][] regions,int[] triangles)
    {
        if(regions==null||triangles==null||triangles.Length==0||triangles.Length%3!=0)return null;
        var sets=new HashSet<int>[regions.Length];var lists=new List<int>[regions.Length];
        for(int r=0;r<regions.Length;r++){sets[r]=new HashSet<int>(regions[r]);lists[r]=new List<int>();}
        for(int t=0;t<triangles.Length;t+=3)
        {
            bool covered=false;
            for(int r=0;r<regions.Length;r++)
                if(sets[r].Contains(triangles[t])&&sets[r].Contains(triangles[t+1])&&sets[r].Contains(triangles[t+2]))
                {lists[r].Add(triangles[t]);lists[r].Add(triangles[t+1]);lists[r].Add(triangles[t+2]);covered=true;}
            if(!covered)return null;
        }
        var result=new int[regions.Length][];for(int r=0;r<result.Length;r++)result[r]=lists[r].ToArray();return result;
    }

    public static CatBodyGuardCatalog.Probe Fit(Vector3[] points,int[] indices,string name)
    {
        var bounds=new Bounds(points[indices[0]],Vector3.zero);
        foreach(int i in indices)bounds.Encapsulate(points[i]);
        var best=default(CatBodyGuardCatalog.Probe);float volume=float.PositiveInfinity;
        for(int axis=0;axis<3;axis++)
        {
            Vector3 centre=bounds.center;float radial=0;
            foreach(int i in indices){Vector3 p=points[i]-centre;p[axis]=0;radial=Mathf.Max(radial,p.sqrMagnitude);}
            float first=float.PositiveInfinity,last=float.NegativeInfinity;
            foreach(int i in indices)
            {
                Vector3 p=points[i]-centre;p[axis]=0;float cap=Mathf.Sqrt(Mathf.Max(0,radial-p.sqrMagnitude));
                first=Mathf.Min(first,points[i][axis]+cap);last=Mathf.Max(last,points[i][axis]-cap);
            }
            if(first>last)first=last=(first+last)*.5f;
            Vector3 a=centre,b=centre;a[axis]=first;b[axis]=last;float radius=Mathf.Sqrt(radial)+.001f;
            float candidate=Mathf.PI*radius*radius*(Vector3.Distance(a,b)+4f*radius/3f);
            if(candidate>=volume)continue;
            volume=candidate;best=new CatBodyGuardCatalog.Probe{region=name,start=a,end=b,radius=radius};
        }
        return best;
    }
}
