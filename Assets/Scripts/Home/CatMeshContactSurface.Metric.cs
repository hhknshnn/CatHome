using System.Collections.Generic;
using UnityEngine;

public sealed partial class CatMeshContactSurface
{
    sealed class MetricWorld
    {
        public Geometry data;
        public Vector3[] points;
        public Bounds[] bounds;
        public int[] stack;
        public Matrix4x4 matrix;
        public bool transformed;
    }
    static readonly Dictionary<Mesh,Geometry> readableMetricGeometry=new Dictionary<Mesh,Geometry>();
    static readonly Dictionary<(Mesh,Transform),MetricWorld> metricWorlds=new Dictionary<(Mesh,Transform),MetricWorld>();

    static Geometry MetricGeometry(Mesh mesh)
    {
        if(mesh==null)return null;
        if(loaded==null)loaded=Resources.Load<CatMeshContactSurface>(ResourceName);
        var baked=loaded!=null?loaded.Find(mesh):null;
        if(baked!=null)return baked;
        // Static built-in readable geometry is available in both Editor and
        // player. Never change importer flags or call UploadMeshData here.
        if(!mesh.isReadable)return null;
        if(readableMetricGeometry.TryGetValue(mesh,out var existing))return existing;
        var value=new Geometry{mesh=mesh,vertices=mesh.vertices,triangles=mesh.triangles};
        if(value.vertices.Length==0||value.triangles.Length==0||value.triangles.Length%3!=0)return null;
        foreach(int index in value.triangles)if(index<0||index>=value.vertices.Length)return null;
        if(readableMetricGeometry.Count>=32)readableMetricGeometry.Clear();
        readableMetricGeometry.Add(mesh,value);return value;
    }

    /// <summary>Original outward orientation, including reflection/nonuniform scale.
    /// RaycastHit.normal is not used to infer which side of a mesh was hit.</summary>
    public static bool TryOriginalTriangleNormal(Mesh mesh,Transform transform,int triangle,out Vector3 normal)
    {
        normal=Vector3.zero;if(transform==null)return false;
        return TryOriginalTriangleNormal(mesh,transform.localToWorldMatrix,triangle,out normal);
    }
    // Callers may capture this exact matrix once for an atomic same-frame solve.
    // It is geometry input, never a cached collision permission.
    public static bool TryOriginalTriangleNormal(Mesh mesh,Matrix4x4 matrix,int triangle,out Vector3 normal)
    {
        normal=Vector3.zero;
        var data=MetricGeometry(mesh);int at=triangle*3;
        if(data==null||at<0||at+2>=data.triangles.Length)return false;
        Vector3 a=matrix.MultiplyPoint3x4(data.vertices[data.triangles[at]]),
            b=matrix.MultiplyPoint3x4(data.vertices[data.triangles[at+1]]),c=matrix.MultiplyPoint3x4(data.vertices[data.triangles[at+2]]);
        normal=OutwardWorldCross(b-a,c-a,matrix);if(normal.sqrMagnitude<1e-18f)return false;
        normal.Normalize();return true;
    }

    /// <summary>Exact Euclidean distance to this mesh at this transform only.
    /// This is a metric, not an inside classification. Unknown unreadable data
    /// returns false, never a fabricated zero or a clear-volume answer.</summary>
    public static bool TryMetric(Mesh mesh,Transform transform,Vector3 point,out float distance,out Vector3 normal)
    {
        distance=float.PositiveInfinity;normal=Vector3.zero;
        return transform!=null&&TryMetric(mesh,transform,transform.localToWorldMatrix,point,out distance,out normal);
    }
    // Preserve the identity key and validate the supplied current matrix on
    // every query. A moved/scaled/rotated owner rebuilds transformed geometry.
    public static bool TryMetric(Mesh mesh,Transform transform,Matrix4x4 matrix,Vector3 point,out float distance,out Vector3 normal)
    {
        distance=float.PositiveInfinity;normal=Vector3.zero;
        if(transform==null)return false;var data=MetricGeometry(mesh);if(data==null)return false;
        var key=(mesh,transform);
        if(!metricWorlds.TryGetValue(key,out var world)||world.data!=data)
        {
            if(metricWorlds.Count>=128)metricWorlds.Clear();
            bool hierarchy=data.nodes!=null&&data.nodes.Length>0&&data.triangleOrder!=null&&data.triangleOrder.Length==data.triangles.Length/3;
            world=new MetricWorld{data=data,points=new Vector3[data.vertices.Length],
                bounds=hierarchy?new Bounds[data.nodes.Length]:null,stack=hierarchy?new int[data.nodes.Length]:null};
            metricWorlds[key]=world;
        }
        if(!world.transformed||!world.matrix.Equals(matrix))
        {
            for(int i=0;i<world.points.Length;i++)world.points[i]=matrix.MultiplyPoint3x4(data.vertices[i]);
            if(world.bounds!=null)for(int i=0;i<world.bounds.Length;i++)world.bounds[i]=MetricBounds(matrix,data.nodes[i].bounds);
            world.matrix=matrix;world.transformed=true;
        }
        float best=float.PositiveInfinity;int triangle=-1;
        void Test(int t)
        {
            int at=t*3;Vector3 value=ClosestTriangle(point,world.points[data.triangles[at]],world.points[data.triangles[at+1]],world.points[data.triangles[at+2]]);
            float square=(value-point).sqrMagnitude;
            if(square>best||square==best&&triangle>=0&&t>=triangle)return;
            best=square;triangle=t;
        }
        if(world.bounds==null)
        {for(int t=0;t<data.triangles.Length/3;t++)Test(t);}
        else
        {
            int top=0;world.stack[top++]=0;
            while(top>0)
            {
                int at=world.stack[--top];if(world.bounds[at].SqrDistance(point)>best)continue;
                var node=data.nodes[at];
                if(node.count>0){for(int i=node.first;i<node.first+node.count;i++)Test(data.triangleOrder[i]);continue;}
                float left=world.bounds[node.left].SqrDistance(point),right=world.bounds[node.right].SqrDistance(point);
                int near=left<=right?node.left:node.right,far=left<=right?node.right:node.left;
                if(Mathf.Max(left,right)<=best)world.stack[top++]=far;
                if(Mathf.Min(left,right)<=best)world.stack[top++]=near;
            }
        }
        if(triangle<0)return false;
        int offset=triangle*3;
        normal=OutwardWorldCross(world.points[data.triangles[offset+1]]-world.points[data.triangles[offset]],
            world.points[data.triangles[offset+2]]-world.points[data.triangles[offset]],matrix).normalized;
        distance=Mathf.Sqrt(best);return true;
    }
    static Bounds MetricBounds(Matrix4x4 matrix,Bounds local)
    {
        Vector3 e=local.extents;
        Vector3 world=new Vector3(Mathf.Abs(matrix.m00)*e.x+Mathf.Abs(matrix.m01)*e.y+Mathf.Abs(matrix.m02)*e.z,
            Mathf.Abs(matrix.m10)*e.x+Mathf.Abs(matrix.m11)*e.y+Mathf.Abs(matrix.m12)*e.z,
            Mathf.Abs(matrix.m20)*e.x+Mathf.Abs(matrix.m21)*e.y+Mathf.Abs(matrix.m22)*e.z);
        Vector3 centre=matrix.MultiplyPoint3x4(local.center);
        Vector3 pad=(new Vector3(Mathf.Abs(centre.x),Mathf.Abs(centre.y),Mathf.Abs(centre.z))+world+Vector3.one)*.000001f;
        return new Bounds(centre,(world+pad)*2);
    }
}
