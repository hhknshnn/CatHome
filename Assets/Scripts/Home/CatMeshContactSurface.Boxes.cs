using UnityEngine;

public sealed partial class CatMeshContactSurface
{
    public sealed partial class TargetSet
    {
        static readonly Vector3[] insideRays={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,
            new Vector3(.019f,.023f,1).normalized,-new Vector3(.013f,1,.027f).normalized,
            -new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};

        /// <summary>Exact owned triangles versus the interior of world OBBs. No Collider is created.</summary>
        public bool BoxesClear(CatBodyGuardBox[] boxes,float tolerance)
        {
            if(boxes==null||boxes.Length==0)return true;
            // Bind/rebind and update all world vertices/nodes under the same
            // rules as closest-point. Subsequent boxes allocate no geometry.
            if(!TryClosest(boxes[0].centre,out _))return false;
            foreach(var box in boxes)
            {
                Vector3 extent=box.halfExtents-Vector3.one*Mathf.Max(0,tolerance);
                // Thin envelopes still require occupied-volume checks: a thin
                // skin region can be completely inside a thick target slab.
                extent=new Vector3(Mathf.Max(.000001f,extent.x),Mathf.Max(.000001f,extent.y),Mathf.Max(.000001f,extent.z));
                Quaternion inverse=Quaternion.Inverse(box.rotation);
                Matrix4x4 matrix=Matrix4x4.TRS(box.centre,box.rotation,Vector3.one);
                Bounds bounds=TransformBounds(matrix,new Bounds(Vector3.zero,extent*2));
                foreach(var part in parts)
                {
                    if(part.data==null||part.filter==null||part.renderer==null||!part.renderer.enabled||
                        !part.filter.gameObject.activeInHierarchy||!part.bounds.Intersects(bounds))continue;
                    if(BoxCrosses(part,box.centre,inverse,extent,bounds))return false;
                    // A completely contained OBB has no crossing surface.
                    // Four of six nearest back-facing exits identify that case
                    // separately; nearest mesh distance is never an inside vote.
                    int inside=0;
                    foreach(var direction in insideRays)if(NearestExit(part,box.centre,direction))inside++;
                    if(inside>=4&&!NearSurface(part,box.centre,tolerance*tolerance))return false;
                }
            }
            return true;
        }
        static bool NearSurface(Part part,Vector3 point,float square)
        {
            bool Near(int triangle)
            {
                int at=triangle*3;var ids=part.data.triangles;
                return (ClosestTriangle(point,part.world[ids[at]],part.world[ids[at+1]],part.world[ids[at+2]])-point).sqrMagnitude<=square;
            }
            if(!part.HasHierarchy)
            {for(int t=0;t<part.data.triangles.Length/3;t++)if(Near(t))return true;return false;}
            int top=0;part.stack[top++]=0;
            while(top>0)
            {
                int at=part.stack[--top];if(part.worldNodes[at].SqrDistance(point)>square)continue;
                var node=part.data.nodes[at];
                if(node.count==0){part.stack[top++]=node.left;part.stack[top++]=node.right;continue;}
                for(int i=node.first;i<node.first+node.count;i++)if(Near(part.data.triangleOrder[i]))return true;
            }
            return false;
        }
        static bool BoxCrosses(Part part,Vector3 centre,Quaternion inverse,Vector3 extent,Bounds bounds)
        {
            if(!part.HasHierarchy)
            {
                for(int t=0;t<part.data.triangles.Length/3;t++)if(TriangleCrosses(part,t,centre,inverse,extent))return true;
                return false;
            }
            int top=0;part.stack[top++]=0;
            while(top>0)
            {
                int at=part.stack[--top];if(!part.worldNodes[at].Intersects(bounds))continue;
                var node=part.data.nodes[at];
                if(node.count==0){part.stack[top++]=node.left;part.stack[top++]=node.right;continue;}
                for(int i=node.first;i<node.first+node.count;i++)
                    if(TriangleCrosses(part,part.data.triangleOrder[i],centre,inverse,extent))return true;
            }
            return false;
        }
        static bool TriangleCrosses(Part part,int triangle,Vector3 centre,Quaternion inverse,Vector3 extent)
        {
            int at=triangle*3;var indices=part.data.triangles;
            Vector3 a=inverse*(part.world[indices[at]]-centre),b=inverse*(part.world[indices[at+1]]-centre),c=inverse*(part.world[indices[at+2]]-centre);
            Vector3 ab=b-a,bc=c-b,ca=a-c;
            if(Separated(a,b,c,Vector3.right,extent)||Separated(a,b,c,Vector3.up,extent)||Separated(a,b,c,Vector3.forward,extent)||
                Separated(a,b,c,Vector3.Cross(ab,bc),extent))return false;
            for(int axis=0;axis<3;axis++)
            {
                Vector3 basis=axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward;
                if(Separated(a,b,c,Vector3.Cross(ab,basis),extent)||Separated(a,b,c,Vector3.Cross(bc,basis),extent)||
                    Separated(a,b,c,Vector3.Cross(ca,basis),extent))return false;
            }
            return true;
        }
        static bool Separated(Vector3 a,Vector3 b,Vector3 c,Vector3 axis,Vector3 e)
        {
            if(axis.sqrMagnitude<1e-16f)return false;
            float x=Vector3.Dot(a,axis),y=Vector3.Dot(b,axis),z=Vector3.Dot(c,axis);
            float radius=Mathf.Abs(axis.x)*e.x+Mathf.Abs(axis.y)*e.y+Mathf.Abs(axis.z)*e.z;
            return Mathf.Min(x,Mathf.Min(y,z))>radius||Mathf.Max(x,Mathf.Max(y,z))<-radius;
        }
        static bool NearestExit(Part part,Vector3 point,Vector3 direction)
        {
            float nearest=float.PositiveInfinity;bool exit=false;
            if(!part.HasHierarchy)
            {
                for(int t=0;t<part.data.triangles.Length/3;t++)RayTriangle(part,t,point,direction,ref nearest,ref exit);
                return exit;
            }
            int top=0;part.stack[top++]=0;Ray ray=new Ray(point,direction);
            while(top>0)
            {
                int at=part.stack[--top];
                if(!part.worldNodes[at].IntersectRay(ray,out float distance)||distance>nearest)continue;
                var node=part.data.nodes[at];
                if(node.count==0){part.stack[top++]=node.left;part.stack[top++]=node.right;continue;}
                for(int i=node.first;i<node.first+node.count;i++)RayTriangle(part,part.data.triangleOrder[i],point,direction,ref nearest,ref exit);
            }
            return exit;
        }
        static void RayTriangle(Part part,int triangle,Vector3 point,Vector3 direction,ref float nearest,ref bool exit)
        {
            int at=triangle*3;var indices=part.data.triangles;
            Vector3 a=part.world[indices[at]],b=part.world[indices[at+1]],c=part.world[indices[at+2]];
            Vector3 e1=b-a,e2=c-a,h=Vector3.Cross(direction,e2);float determinant=Vector3.Dot(e1,h);
            if(Mathf.Abs(determinant)<1e-10f)return;
            float reciprocal=1/determinant;Vector3 s=point-a;float u=Vector3.Dot(s,h)*reciprocal;
            if(u<0||u>1)return;Vector3 q=Vector3.Cross(s,e1);float v=Vector3.Dot(direction,q)*reciprocal;
            if(v<0||u+v>1)return;float distance=Vector3.Dot(e2,q)*reciprocal;
            if(distance<=.000001f||distance>=nearest)return;
            nearest=distance;exit=Vector3.Dot(OutwardWorldCross(e1,e2,part.matrix),direction)>0;
        }
    }
}
