using UnityEngine;

public sealed partial class CatMeshContactSurface
{
    // A non-convex mesh may return no PhysX MTD for a box which crosses its
    // closed volume. Check original triangles independently before certifying
    // that an MTD-negative envelope is empty. Geometry is cached, permission is not.
    public static bool TryBoxBoundary(Mesh mesh,Transform transform,CatBodyGuardBox box,out bool crosses)
    {
        crosses=true;
        if(transform==null)return false;
        var data=MetricGeometry(mesh);if(data==null)return false;
        var key=(mesh,transform);
        if(!metricWorlds.TryGetValue(key,out var world)||world.data!=data)
        {
            if(metricWorlds.Count>=128)metricWorlds.Clear();
            bool hierarchy=data.nodes!=null&&data.nodes.Length>0&&data.triangleOrder!=null&&data.triangleOrder.Length==data.triangles.Length/3;
            world=new MetricWorld{data=data,points=new Vector3[data.vertices.Length],
                bounds=hierarchy?new Bounds[data.nodes.Length]:null,stack=hierarchy?new int[data.nodes.Length]:null};
            metricWorlds[key]=world;
        }
        Matrix4x4 matrix=transform.localToWorldMatrix;
        if(!world.transformed||!world.matrix.Equals(matrix))
        {
            for(int i=0;i<world.points.Length;i++)world.points[i]=matrix.MultiplyPoint3x4(data.vertices[i]);
            if(world.bounds!=null)for(int i=0;i<world.bounds.Length;i++)world.bounds[i]=MetricBounds(matrix,data.nodes[i].bounds);
            world.matrix=matrix;world.transformed=true;
        }
        Quaternion inverse=Quaternion.Inverse(box.rotation);
        var bounds=MetricBounds(Matrix4x4.TRS(box.centre,box.rotation,Vector3.one),new Bounds(Vector3.zero,box.halfExtents*2));
        bool Crosses(int triangle)
        {
            int at=triangle*3;
            Vector3 a=inverse*(world.points[data.triangles[at]]-box.centre),
                b=inverse*(world.points[data.triangles[at+1]]-box.centre),c=inverse*(world.points[data.triangles[at+2]]-box.centre);
            Vector3 ab=b-a,bc=c-b,ca=a-c;
            if(BoundarySeparated(a,b,c,Vector3.right,box.halfExtents)||BoundarySeparated(a,b,c,Vector3.up,box.halfExtents)||
                BoundarySeparated(a,b,c,Vector3.forward,box.halfExtents)||BoundarySeparated(a,b,c,Vector3.Cross(ab,bc),box.halfExtents))return false;
            for(int axis=0;axis<3;axis++)
            {
                Vector3 basis=axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward;
                if(BoundarySeparated(a,b,c,Vector3.Cross(ab,basis),box.halfExtents)||
                    BoundarySeparated(a,b,c,Vector3.Cross(bc,basis),box.halfExtents)||
                    BoundarySeparated(a,b,c,Vector3.Cross(ca,basis),box.halfExtents))return false;
            }
            return true;
        }
        if(world.bounds==null)
        {
            for(int i=0;i<data.triangles.Length/3;i++)if(Crosses(i))return true;
        }
        else
        {
            int top=0;world.stack[top++]=0;
            while(top>0)
            {
                int at=world.stack[--top];if(!world.bounds[at].Intersects(bounds))continue;
                var node=data.nodes[at];
                if(node.count==0){world.stack[top++]=node.left;world.stack[top++]=node.right;continue;}
                for(int i=node.first;i<node.first+node.count;i++)if(Crosses(data.triangleOrder[i]))return true;
            }
        }
        crosses=false;return true;
    }

    static bool BoundarySeparated(Vector3 a,Vector3 b,Vector3 c,Vector3 axis,Vector3 extent)
    {
        if(axis.sqrMagnitude<1e-16f)return false;
        float x=Vector3.Dot(a,axis),y=Vector3.Dot(b,axis),z=Vector3.Dot(c,axis);
        float radius=Mathf.Abs(axis.x)*extent.x+Mathf.Abs(axis.y)*extent.y+Mathf.Abs(axis.z)*extent.z;
        return Mathf.Min(x,Mathf.Min(y,z))>radius||Mathf.Max(x,Mathf.Max(y,z))<-radius;
    }
}
