using System.Collections.Generic;
using UnityEngine;

/// <summary>Exact current weighted skin against the original care meshes.
/// Reuses cached contact BVHs; never bakes or reads a live mesh.</summary>
public sealed class CatCareSkinClearance
{
    sealed class Part{public MeshCollider collider;}
    readonly CatCareWeightedSkin skin;
    readonly Part[] parts;
    readonly float floorY;
    // Only failed source-vertex indices are retained, never points, matrices,
    // normals, distances or a clear/blocked permission. Most adjacent rejected
    // candidates meet the same head/foot witness late in the ordinary order.
    readonly int[] recentRejected=new int[6];
    int recentRejectedCount;
    public int LastVertices{get;private set;}
    public int LastRays{get;private set;}
    // TryMetric owns its BVH; -1 explicitly means its triangle count is not
    // exposed. Do not report a fabricated zero for performed metric queries.
    public int LastTriangleTests{get;private set;}
    public int LastMetricQueries{get;private set;}
    public int LastRejectedSourceVertex{get;private set;}=-1;
    public MeshCollider LastRejectedCollider{get;private set;}
    public Vector3 LastRejectedPoint{get;private set;}
    public float LastMaximumDepth{get;private set;}
    public bool Available=>parts.Length>0;
    public float FloorHeight=>floorY;
    public bool ContainsVerifiedMesh(Collider collider)
    {
        foreach (var part in parts)
            if (part.collider == collider && part.collider != null && part.collider.enabled &&
                !part.collider.isTrigger && part.collider.gameObject.activeInHierarchy) return true;
        return false;
    }
    static readonly Vector3[] Directions={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,new Vector3(.019f,.023f,1).normalized,
        -new Vector3(.013f,1,.027f).normalized,-new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
    CatCareSkinClearance(CatCareWeightedSkin weighted,Part[] source,float ground){skin=weighted;parts=source;floorY=ground;}
    public static CatCareSkinClearance Bind(CatCareWeightedSkin skin,MonoBehaviour owner,Transform target,CatMovement cat)
    {
        if(skin==null||owner==null||target==null||cat==null)return null;
        Transform root=owner is MealTimeActivity?owner.transform:target.parent;
        if(root==null)return null;var found=new List<Part>();var unique=new HashSet<MeshCollider>();
        foreach(var collider in root.GetComponentsInChildren<MeshCollider>(true))Add(collider);
        // Include known real neighbouring bowl/tray meshes; navigation proxy boxes
        // continue to govern root readiness and are not treated as rendered skin.
        foreach(var collider in Physics.OverlapSphere(target.position,.85f,~0,QueryTriggerInteraction.Ignore))if(collider is MeshCollider mesh)Add(mesh);
        // The controller may stand partly on the care tray. Its root height
        // minus a fixed offset is then not the room floor: treating that as a
        // global plane wrongly rejects the pelvis/rear paws beside the tray.
        // Measure the lowest real upward surface beneath this stance. Raised
        // tray support remains local to each paw through SupportHeight below.
        float ground=float.PositiveInfinity;
        foreach(var hit in Physics.RaycastAll(cat.transform.position+Vector3.up*.20f,Vector3.down,.40f,~0,QueryTriggerInteraction.Ignore))
            if(!hit.collider.transform.IsChildOf(cat.transform)&&hit.normal.y>=.8f&&hit.point.y<=cat.transform.position.y+.02f)
                ground=Mathf.Min(ground,hit.point.y);
        if(float.IsPositiveInfinity(ground))ground=cat.transform.position.y-.05f;
        return found.Count==0?null:new CatCareSkinClearance(skin,found.ToArray(),ground);
        void Add(MeshCollider collider)
        {
            if(collider==null||collider.sharedMesh==null||!collider.enabled||collider.isTrigger||!collider.gameObject.activeInHierarchy||
                collider.transform.IsChildOf(cat.transform)||!unique.Add(collider))return;
            var filter=collider.GetComponent<MeshFilter>();var renderer=collider.GetComponent<Renderer>();
            if(filter==null||renderer==null||!renderer.enabled||filter.sharedMesh!=collider.sharedMesh||!CatMeshContactSurface.HasGeometry(collider.sharedMesh))return;
            found.Add(new Part{collider=collider});
        }
    }
    public float SupportHeight(Vector3 sourceSole)
    {
        // A paw above the real care tray must meet its top, not be pushed
        // through that solid to the room floor. Only original visible meshes
        // already bound for full-skin clearance can supply raised support.
        float support=floorY;
        var ray=new Ray(sourceSole+Vector3.up*.01f,Vector3.down);
        float distance=Mathf.Max(0f,sourceSole.y-floorY)+.02f;
        foreach(var part in parts)
        {
            var mesh=part.collider;
            if(mesh==null||!mesh.enabled||mesh.isTrigger||!mesh.gameObject.activeInHierarchy)continue;
            if(!mesh.Raycast(ray,out var hit,distance)||hit.point.y>sourceSole.y+.002f||hit.point.y<=support)continue;
            // Slope is an angle test: use the unit original normal, not the
            // area-weighted triangle cross product used by inside votes.
            if(!CatMeshContactSurface.TryOriginalTriangleNormal(mesh.sharedMesh,mesh.transform,hit.triangleIndex,out var normal)||normal.y<.8f)continue;
            support=hit.point.y;
        }
        return support;
    }
    Matrix4x4 pointDelta=Matrix4x4.identity;
    public bool IsClear(float maximumDepth=.0029f,Matrix4x4? plannedDelta=null)
    {
        pointDelta=plannedDelta??Matrix4x4.identity;
        LastVertices=LastRays=LastTriangleTests=LastMetricQueries=0;LastMaximumDepth=0;
        LastRejectedSourceVertex=-1;LastRejectedCollider=null;LastRejectedPoint=Vector3.zero;
        if(!skin.CaptureMatrices())return false;
        bool backfaces=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
        try
        {
            for(int index=0;index<recentRejectedCount;index++)
            {
                int vertex=recentRejected[index];
                if(!IsVertexClear(vertex,maximumDepth)){RememberRejected(vertex);return false;}
            }
            for(int vertex=0;vertex<skin.Count;vertex++)
            {
                bool alreadyChecked=false;
                for(int index=0;index<recentRejectedCount;index++)
                    if(recentRejected[index]==vertex){alreadyChecked=true;break;}
                if(alreadyChecked)continue;
                if(!IsVertexClear(vertex,maximumDepth)){RememberRejected(vertex);return false;}
            }
            return true;
        }
        finally{Physics.queriesHitBackfaces=backfaces;}
    }
    bool IsVertexClear(int vertex,float maximumDepth)
    {
        Vector3 point=pointDelta.MultiplyPoint3x4(skin.Point(vertex));LastVertices++;
        if(point.y<floorY-.003f)
        {RejectWitness(vertex,point,null);return false;}
        foreach(var part in parts)
        {
            var mesh=part.collider;
            if(mesh==null||!mesh.enabled||mesh.isTrigger||!mesh.gameObject.activeInHierarchy||!mesh.bounds.Contains(point))continue;
            int votes=0;
            for(int ray=0;ray<Directions.Length;ray++)
            {
                Vector3 direction=Directions[ray];LastRays++;
                if(mesh.Raycast(new Ray(point,direction),out var hit,5f)&&
                    CatMeshContactSurface.TryWorldNormal(mesh.sharedMesh,mesh.transform,hit.triangleIndex,out var normal)&&Vector3.Dot(normal,direction)>0)votes++;
                // Same four-of-six verdict and exact current-world geometry.
                if(votes>=4||votes+Directions.Length-ray-1<4)break;
            }
            if(votes<4)continue;
            // This inside vote belongs to this exact collider instance. A
            // child using the same sharedMesh is still a different surface.
            LastMetricQueries++;LastTriangleTests=-1;
            if(!CatMeshContactSurface.TryMetric(mesh.sharedMesh,mesh.transform,point,out float depth,out _))
            {RejectWitness(vertex,point,mesh);return false;}
            LastMaximumDepth=Mathf.Max(LastMaximumDepth,depth);
            if(depth>maximumDepth){RejectWitness(vertex,point,mesh);return false;}
        }
        return true;
    }
    void RejectWitness(int vertex,Vector3 point,MeshCollider collider)
    {LastRejectedSourceVertex=skin.SourceIndex(vertex);LastRejectedPoint=point;LastRejectedCollider=collider;}
    void RememberRejected(int vertex)
    {
        int at=0;
        while(at<recentRejectedCount&&recentRejected[at]!=vertex)at++;
        if(at==recentRejectedCount)
        {
            if(recentRejectedCount<recentRejected.Length)recentRejectedCount++;
            at=recentRejectedCount-1;
        }
        for(int index=at;index>0;index--)recentRejected[index]=recentRejected[index-1];
        recentRejected[0]=vertex;
    }
}
