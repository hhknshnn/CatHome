using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Test-only metric distance to original mesh triangles. An inside vote is a
/// separate classification, never inferred from distance or collider bounds.
/// Uses the independently brute-force-tested contact BVH and triangle kernel;
/// does not replace its runtime catalog, create colliders, or write any asset.
/// </summary>
internal sealed class QaExactMeshContact
{
    internal struct Hit
    {
        internal Vector3 point,normal;
        internal float distance;
        internal int triangle,submesh,triangleTests;
    }
    sealed class World
    {
        internal CatMeshContactSurface.Geometry geometry;
        internal Matrix4x4 matrix;
        internal Vector3[] points;
        internal Bounds[] bounds;
        internal int[] stack;
        internal bool transformed;
    }
    static readonly MethodInfo Build=typeof(CatMeshContactSurface).GetMethod("BuildHierarchy",BindingFlags.NonPublic|BindingFlags.Static);
    static readonly Func<Vector3,Vector3,Vector3,Vector3,Vector3> Closest=
        (Func<Vector3,Vector3,Vector3,Vector3,Vector3>)Delegate.CreateDelegate(typeof(Func<Vector3,Vector3,Vector3,Vector3,Vector3>),
            typeof(CatMeshContactSurface).GetMethod("ClosestTriangle",BindingFlags.NonPublic|BindingFlags.Static));
    readonly QaMeshTopologyCache topology;
    readonly Dictionary<Mesh,CatMeshContactSurface.Geometry> geometry=new Dictionary<Mesh,CatMeshContactSurface.Geometry>();
    readonly Dictionary<Transform,World> worlds=new Dictionary<Transform,World>();
    internal QaExactMeshContact(QaMeshTopologyCache topology){this.topology=topology;}
    internal void Clear(){geometry.Clear();worlds.Clear();}

    internal Hit Measure(Mesh mesh,Transform transform,Vector3 point)
    {
        if(mesh==null||transform==null)throw new ArgumentNullException("An original mesh and its actual transform are required");
        if(!geometry.TryGetValue(mesh,out var data))
        {
            var source=topology.Get(mesh);
            data=new CatMeshContactSurface.Geometry{mesh=mesh,vertices=source.vertices,triangles=source.triangles};
            if(Build==null)throw new InvalidOperationException("Exact QA metrology requires the tested BVH builder");
            Build.Invoke(null,new object[]{data});
            if(data.nodes.Length==0)throw new InvalidOperationException("Empty original collision mesh: "+mesh.name);
            geometry.Add(mesh,data);
        }
        if(!worlds.TryGetValue(transform,out var world)||world.geometry!=data)
        {
            world=new World{geometry=data,points=new Vector3[data.vertices.Length],bounds=new Bounds[data.nodes.Length],stack=new int[data.nodes.Length]};
            worlds[transform]=world;
        }
        Matrix4x4 matrix=transform.localToWorldMatrix;
        if(!world.transformed||!world.matrix.Equals(matrix))
        {
            for(int i=0;i<world.points.Length;i++)world.points[i]=matrix.MultiplyPoint3x4(data.vertices[i]);
            for(int i=0;i<world.bounds.Length;i++)world.bounds[i]=BoundsInWorld(matrix,data.nodes[i].bounds);
            world.matrix=matrix;world.transformed=true;
        }
        float best=float.PositiveInfinity;Vector3 nearest=default;int triangle=-1,tests=0,top=0;
        world.stack[top++]=0;
        while(top>0)
        {
            int n=world.stack[--top];if(world.bounds[n].SqrDistance(point)>best)continue;
            var node=data.nodes[n];
            if(node.count>0)
            {
                for(int i=node.first;i<node.first+node.count;i++)
                {
                    int id=data.triangleOrder[i],index=id*3;tests++;
                    Vector3 candidate=Closest(point,world.points[data.triangles[index]],world.points[data.triangles[index+1]],world.points[data.triangles[index+2]]);
                    float distance=(point-candidate).sqrMagnitude;
                    if(distance>best||distance==best&&triangle>=0&&id>=triangle)continue;
                    triangle=id;best=distance;nearest=candidate;
                }
                continue;
            }
            float left=world.bounds[node.left].SqrDistance(point),right=world.bounds[node.right].SqrDistance(point);
            int near=left<=right?node.left:node.right,far=left<=right?node.right:node.left;
            if(Mathf.Max(left,right)<=best)world.stack[top++]=far;
            if(Mathf.Min(left,right)<=best)world.stack[top++]=near;
        }
        if(triangle<0)throw new InvalidOperationException("No original triangle measured");
        int t=triangle*3;
        Vector3 normal=Vector3.Cross(world.points[data.triangles[t+1]]-world.points[data.triangles[t]],world.points[data.triangles[t+2]]-world.points[data.triangles[t]]).normalized;
        int remaining=triangle*3,submesh=-1;
        for(int sub=0;sub<mesh.subMeshCount;sub++)
        {
            int count=mesh.GetSubMesh(sub).indexCount;
            if(remaining<count){submesh=sub;break;}remaining-=count;
        }
        return new Hit{point=nearest,normal=normal,distance=Mathf.Sqrt(best),triangle=triangle,submesh=submesh,triangleTests=tests};
    }

    static Bounds BoundsInWorld(Matrix4x4 m,Bounds b)
    {
        Vector3 e=b.extents;
        Vector3 size=new Vector3(Mathf.Abs(m.m00)*e.x+Mathf.Abs(m.m01)*e.y+Mathf.Abs(m.m02)*e.z,
            Mathf.Abs(m.m10)*e.x+Mathf.Abs(m.m11)*e.y+Mathf.Abs(m.m12)*e.z,
            Mathf.Abs(m.m20)*e.x+Mathf.Abs(m.m21)*e.y+Mathf.Abs(m.m22)*e.z);
        Vector3 centre=m.MultiplyPoint3x4(b.center);
        Vector3 pad=(new Vector3(Mathf.Abs(centre.x),Mathf.Abs(centre.y),Mathf.Abs(centre.z))+size+Vector3.one)*.000001f;
        return new Bounds(centre,(size+pad)*2);
    }
}
