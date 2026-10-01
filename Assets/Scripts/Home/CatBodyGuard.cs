using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>A measured world-space interaction envelope; it never moves the actor.</summary>
public struct CatBodyGuardBox
{
    public Vector3 centre,halfExtents;
    public Quaternion rotation;
}

/// <summary>
/// Additional anatomical sweeps for ordinary walking. The CharacterController
/// still owns grounding/steps. This guard never depenetrates or moves an idle cat.
/// </summary>
public sealed class CatBodyGuard : IDisposable
{
    private const float Skin = .002f;
    private const float ContactTolerance = .0001f;
    private readonly Transform actor;
    private readonly CharacterController controller;
    private readonly Collider[] overlaps = new Collider[64];
    private readonly RaycastHit[] hits = new RaycastHit[64];
    private readonly Contact[] contacts = new Contact[128];
    private readonly float[] initialBoundaryDepth = new float[4];
    private CatBodyGuardCatalog.Probe[] probes = Array.Empty<CatBodyGuardCatalog.Probe>();
    private CapsuleCollider query;
    private BoxCollider boxQuery;
    private readonly System.Collections.Generic.Dictionary<(Collider,Vector3,float),bool> surfaceInsideCache = new System.Collections.Generic.Dictionary<(Collider,Vector3,float),bool>();
    private bool surfaceQuery;
    private readonly System.Collections.Generic.HashSet<Collider> surfaceChecked=new System.Collections.Generic.HashSet<Collider>();
    private readonly System.Collections.Generic.List<GameObject> surfaceRoots=new System.Collections.Generic.List<GameObject>(32);
    private readonly System.Collections.Generic.List<MeshCollider> surfaceMeshScratch=new System.Collections.Generic.List<MeshCollider>(128);
    private struct SurfaceMeshBounds { public MeshCollider mesh; public Bounds bounds; }
    private readonly System.Collections.Generic.List<SurfaceMeshBounds> surfaceMeshes=new System.Collections.Generic.List<SurfaceMeshBounds>(128);
    private static readonly Vector3[] interiorDirections={Vector3.up,Vector3.down,Vector3.right,Vector3.left,Vector3.forward,Vector3.back};
    private int contactCount;
    private float reach;
    private HomeRoomBoundary boundary;
    private float boundaryClearance;

    private struct Contact { public int probe; public Collider collider; public float depth; public Vector3 normal; }
    public int ProbeCount => probes.Length;
    public bool HasProfile => probes.Length > 0;

    public CatBodyGuard(Transform actor, CharacterController controller)
    {
        this.actor = actor;
        this.controller = controller;
    }

    public void Rebind(Animator animator)
    {
        probes = Array.Empty<CatBodyGuardCatalog.Probe>();
        reach = 0f;
        var visual = animator != null ? animator.GetComponentInParent<CatBreedVisualTag>() : null;
        var catalog = Resources.Load<CatBodyGuardCatalog>(CatBodyGuardCatalog.ResourceName);
        var profile = visual != null && catalog != null ? catalog.Find(visual.BreedId) : null;
        if (profile == null) return;
        probes = new CatBodyGuardCatalog.Probe[profile.probes.Length];
        Vector3 scale = visual.transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        Quaternion inverse = Quaternion.Inverse(actor.rotation);
        for (int i = 0; i < probes.Length; i++)
        {
            var source = profile.probes[i];
            var p = new CatBodyGuardCatalog.Probe { region = source.region,
                start = inverse * (visual.transform.TransformPoint(source.start) - actor.position),
                end = inverse * (visual.transform.TransformPoint(source.end) - actor.position),
                radius = source.radius * radiusScale };
            probes[i] = p;
            reach = Mathf.Max(reach, p.start.magnitude + p.radius, p.end.magnitude + p.radius);
        }
        if (query == null)
        {
            var holder = new GameObject("Cat body guard query") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(holder, actor.gameObject.scene);
            // This Unity version requires an enabled primitive for reliable
            // ComputePenetration. A remote trigger has no gameplay collision;
            // all queries below ignore triggers and use explicit probe poses.
            holder.transform.position = new Vector3(0f, -10000f, 0f);
            query = holder.AddComponent<CapsuleCollider>();
            query.direction = 2;
            query.isTrigger = true;
            query.enabled = true;
        }
    }

    /// <summary>Capture an existing overlap, permitting escape but never a forced reposition.</summary>
    public void BeginStep(HomeRoomBoundary roomBoundary, float extraClearance)
    {
        boundary = roomBoundary;
        boundaryClearance = extraClearance;
        contactCount = 0;
        for (int side = 0; side < initialBoundaryDepth.Length; side++) initialBoundaryDepth[side] = 0f;
        for (int i = 0; i < probes.Length; i++)
        {
            WorldProbe(i, actor.position, actor.rotation, out var a, out var b, out float radius);
            if (boundary != null)
                for (int side = 0; side < 4; side++) initialBoundaryDepth[side] = Mathf.Max(
                    initialBoundaryDepth[side], BoundaryDepth(a, b, radius, side));
            int count = Physics.OverlapCapsuleNonAlloc(a, b, radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            for (int c = 0; c < count && contactCount < contacts.Length; c++)
                if (Solid(overlaps[c]) && Penetration(a, b, radius, overlaps[c], out var normal, out float depth))
                    contacts[contactCount++] = new Contact { probe = i, collider = overlaps[c], normal = normal, depth = depth };
        }
    }

    public Quaternion ConstrainRotation(Vector3 position, Quaternion from, Quaternion target)
    {
        if (!HasProfile) return target;
        float angle = Quaternion.Angle(from, target);
        // At most one degree / 8 mm of the furthest anatomical point per check.
        int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(angle, angle * Mathf.Deg2Rad * reach / .008f)), 1, 64);
        float accepted = 0f;
        for (int step = 1; step <= steps; step++)
        {
            float t = (float)step / steps;
            if (PoseAllowed(position, Quaternion.Slerp(from, target, t))) { accepted = t; continue; }
            float rejected = t;
            for (int refine = 0; refine < 5; refine++)
            {
                float middle = (accepted + rejected) * .5f;
                if (PoseAllowed(position, Quaternion.Slerp(from, target, middle))) accepted = middle;
                else rejected = middle;
            }
            break;
        }
        return Quaternion.Slerp(from, target, accepted);
    }

    public Vector3 ConstrainMove(Vector3 position, Quaternion rotation, Vector3 movement)
    {
        if (!HasProfile) return movement;
        float vertical = movement.y;
        movement.y = 0f;
        Vector3 origin = position, remaining = movement;
        for (int slide = 0; slide < 3 && remaining.sqrMagnitude > .00000001f; slide++)
        {
            float distance = remaining.magnitude;
            Vector3 direction = remaining / distance;
            float allowed = distance;
            Vector3 stopNormal = Vector3.zero;
            for (int i = 0; i < probes.Length; i++)
            {
                WorldProbe(i, position, rotation, out var a, out var b, out float radius);
                int count = Physics.CapsuleCastNonAlloc(a, b, radius, direction, hits,
                    distance + Skin, ~0, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < count; h++)
                {
                    var hit = hits[h];
                    if (!Solid(hit.collider)) continue;
                    Vector3 normal = hit.normal;
                    if (hit.distance <= Skin && FindContact(i, hit.collider, out var contact))
                    {
                        // Casts beginning in overlap can report a synthetic opposite
                        // normal. The measured separation normal allows an exit.
                        normal = contact.normal;
                        if (Vector3.Dot(direction, normal) >= -.0001f) continue;
                    }
                    normal.y = 0f;
                    if (normal.sqrMagnitude < .01f) continue; // ground remains the controller's job
                    normal.Normalize();
                    if (Vector3.Dot(direction, normal) >= -.0001f) continue;
                    float travel = Mathf.Max(0f, hit.distance - Skin);
                    if (travel < allowed) { allowed = travel; stopNormal = normal; }
                }
            }
            Vector3 advance = direction * allowed;
            position += advance;
            if (stopNormal.sqrMagnitude < .01f) break;
            remaining = Vector3.ProjectOnPlane(remaining - advance, stopNormal);
        }
        Vector3 candidate = ClampBoundary(position, rotation);
        Vector3 result = candidate - origin;
        // Overlap checks cover starts inside geometry and concave contact cases
        // where a sweep does not report an entry. Never worsen a pre-existing overlap.
        if (!PoseAllowed(candidate, rotation))
        {
            float accepted = 0f, rejected = 1f;
            for (int i = 0; i < 8; i++)
            {
                float t = (accepted + rejected) * .5f;
                if (PoseAllowed(origin + result * t, rotation)) accepted = t;
                else rejected = t;
            }
            result *= accepted;
        }
        result.y = vertical;
        return result;
    }

    /// <summary>Pure world check for action readiness; never changes movement contacts or the actor.</summary>
    public bool IsPoseClear(Vector3 position, Quaternion rotation, float tolerance = .015f,
        Func<Collider, bool> verifiedSkinClear = null)
    {
        if (!HasProfile || query == null) return false;
        for (int i = 0; i < probes.Length; i++)
        {
            WorldProbe(i, position, rotation, out var a, out var b, out float radius);
            int count = Physics.OverlapCapsuleNonAlloc(a, b, radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int c = 0; c < count; c++)
                if (Solid(overlaps[c]) && Penetration(a, b, radius, overlaps[c], out _, out float depth) &&
                    depth > tolerance && !(verifiedSkinClear?.Invoke(overlaps[c]) ?? false)) return false;
        }
        return PoseInteriorClear(position, rotation, tolerance, verifiedSkinClear);
    }

    // A capsule entirely inside a closed nonconvex mesh can report no overlap
    // or MTD. Check measured body-axis points against its original winding;
    // this is an additional refusal only, never a replacement for surface MTD.
    // Fresh scene enumeration also observes a solid moved/added before a click.
    readonly System.Collections.Generic.List<GameObject> poseSceneRoots = new System.Collections.Generic.List<GameObject>(32);
    readonly System.Collections.Generic.List<MeshCollider> poseMeshScratch = new System.Collections.Generic.List<MeshCollider>(128);
    readonly System.Collections.Generic.List<Vector3> poseAxisPoints = new System.Collections.Generic.List<Vector3>(12);
    bool PoseInteriorClear(Vector3 position, Quaternion rotation, float tolerance, Func<Collider, bool> verifiedSkinClear)
    {
        poseAxisPoints.Clear();
        var bounds = new Bounds(position, Vector3.zero);
        for (int i = 0; i < probes.Length; i++)
        {
            WorldProbe(i, position, rotation, out var a, out var b, out float radius);
            poseAxisPoints.Add(a); poseAxisPoints.Add((a + b) * .5f); poseAxisPoints.Add(b);
            var capsuleBounds = new Bounds(a, Vector3.zero); capsuleBounds.Encapsulate(b); capsuleBounds.Expand(radius * 2f);
            bounds.Encapsulate(capsuleBounds);
        }
        surfaceInsideCache.Clear();
        actor.gameObject.scene.GetRootGameObjects(poseSceneRoots);
        foreach (var root in poseSceneRoots)
        {
            root.GetComponentsInChildren(false, poseMeshScratch);
            foreach (var mesh in poseMeshScratch)
            {
                if (mesh.convex || !Solid(mesh) || mesh.sharedMesh == null || !mesh.bounds.Intersects(bounds)) continue;
                // Unknown unreadable assets retain their existing surface gate.
                // Catalog authoring opts real closed geometry into this check.
                if (!mesh.sharedMesh.isReadable && !CatMeshContactSurface.HasGeometry(mesh.sharedMesh)) continue;
                foreach (var point in poseAxisPoints)
                    if (DeepInside(point, mesh, tolerance) && !(verifiedSkinClear?.Invoke(mesh) ?? false)) return false;
            }
        }
        return true;
    }

    /// <summary>The actual controller capsule must fit before an action disables it.</summary>
    public bool IsControllerClear(Vector3 position, Quaternion rotation)
    {
        if (controller == null || query == null) return false;
        Vector3 scale = actor.lossyScale;
        float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(radius * 2f, controller.height * Mathf.Abs(scale.y));
        Vector3 centre = position + rotation * Vector3.Scale(controller.center, scale);
        Vector3 rise = Vector3.up * (height * .5f - radius);
        int count = Physics.OverlapCapsuleNonAlloc(centre - rise, centre + rise, radius,
            overlaps, ~0, QueryTriggerInteraction.Ignore);
        if (count == overlaps.Length) return false;
        for (int c = 0; c < count; c++)
            if (Solid(overlaps[c]) && Penetration(centre - rise, centre + rise, radius,
                overlaps[c], out var normal, out float depth) && normal.y < .8f && depth > .002f) return false;
        return true;
    }

    /// <summary>Validate a measured gesture envelope without changing movement state.</summary>
    public bool IsWorldPoseClear(CatBodyGuardCatalog.Probe[] worldProbes, float tolerance = .015f)
    {
        if (query == null || worldProbes == null || worldProbes.Length == 0) return false;
        foreach (var probe in worldProbes)
        {
            int count = Physics.OverlapCapsuleNonAlloc(probe.start, probe.end, probe.radius,
                overlaps, ~0, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int c = 0; c < count; c++)
                if (Solid(overlaps[c]) && Penetration(probe.start, probe.end, probe.radius,
                    overlaps[c], out _, out float depth) && depth > tolerance) return false;
        }
        return true;
    }

    public bool IsWorldBoxesClear(CatBodyGuardBox[] boxes,float tolerance=.015f, Func<int,Collider,bool> refine=null)
    {
        if(query==null||boxes==null||boxes.Length==0)return false;
        if(boxQuery==null)
        {
            // Reuse the remote trigger holder and the same solid/layer/owner
            // filter. Enabled primitives are required by this Unity version.
            boxQuery=query.gameObject.AddComponent<BoxCollider>();
            boxQuery.isTrigger=true;boxQuery.enabled=true;
        }
        surfaceInsideCache.Clear();surfaceQuery=true;
        if(refine!=null)
        {
            // Non-convex mesh overlap/MTD can both miss an entirely contained
            // primitive. Collect fresh scene solids, including same-frame
            // additions, with reusable lists rather than caching permission.
            surfaceMeshes.Clear();actor.gameObject.scene.GetRootGameObjects(surfaceRoots);
            var queryBounds=SurfaceWorldBounds(boxes[0]);
            for(int i=1;i<boxes.Length;i++)queryBounds.Encapsulate(SurfaceWorldBounds(boxes[i]));
            foreach(var root in surfaceRoots)
            {
                root.GetComponentsInChildren(false,surfaceMeshScratch);
                foreach(var mesh in surfaceMeshScratch)if(!mesh.convex&&Solid(mesh))
                {
                    // Bounds are read fresh for this synchronous query only.
                    // A distant solid cannot overlap any supplied envelope.
                    var bounds=mesh.bounds;
                    if(bounds.Intersects(queryBounds))surfaceMeshes.Add(new SurfaceMeshBounds{mesh=mesh,bounds=bounds});
                }
            }
        }
        try
        {
        for(int index=0;index<boxes.Length;index++)
        {
            var box=boxes[index];surfaceChecked.Clear();
            if(box.halfExtents.x<=0||box.halfExtents.y<=0||box.halfExtents.z<=0)return false;
            int count=Physics.OverlapBoxNonAlloc(box.centre,box.halfExtents,overlaps,box.rotation,~0,QueryTriggerInteraction.Ignore);
            if(count==overlaps.Length)return false;
            for(int c=0;c<count;c++)
            {
                // Refinement also uses this remote primitive. Reset dimensions
                // before each collider so a previous triangle cannot shrink it.
                boxQuery.size=box.halfExtents*2;
                var solid=overlaps[c];
                if(Solid(solid)&&Physics.ComputePenetration(boxQuery,box.centre,box.rotation,
                    solid,solid.transform.position,solid.transform.rotation,out _,out float depth))
                {
                    surfaceChecked.Add(solid);
                    if(depth>tolerance&&(refine==null||!refine(index,solid)))return false;
                }
            }
            if(refine!=null)
            {
                var boxBounds=SurfaceWorldBounds(box);
                foreach(var item in surfaceMeshes)
                {
                    var mesh=item.mesh;
                    if(surfaceChecked.Contains(mesh)||!item.bounds.Intersects(boxBounds))continue;
                    bool known=CatMeshContactSurface.TryBoxBoundary(mesh.sharedMesh,mesh.transform,box,out bool boundary);
                    if((!known||boundary||DeepInside(box.centre,mesh,0))&&!refine(index,mesh))return false;
                }
            }
        }
        return true;
        }
        finally{surfaceQuery=false;}
    }

    /// <summary>Conservative full-face refinement of an already overlapping
    /// region. Every child encloses its complete triangle with the same 1 mm
    /// shell; subdivision removes empty box corners, never measured skin.</summary>
    public bool IsWorldTriangleClear(Vector3 a,Vector3 b,Vector3 c,Collider solid,float tolerance=.002f,int subdivisions=3,float shell=.001f)
    {
        if(boxQuery==null||solid==null)return false;
        if(subdivisions==3&&!surfaceQuery)surfaceInsideCache.Clear();
        // Exact full-face bounds, before ray/MTD work. The isotropic shell is
        // retained and all three corners participate, including skinny tips.
        var faceBounds=new Bounds(a,Vector3.zero);faceBounds.Encapsulate(b);faceBounds.Encapsulate(c);
        faceBounds.Expand(shell*2);
        if(!solid.bounds.Intersects(faceBounds))return true;
        // PhysX penetration against a non-convex mesh can miss a primitive
        // entirely inside it. Preserve this distinct occupied-volume check.
        if(solid is MeshCollider mesh && !mesh.convex &&
            (DeepInside(a,solid,tolerance)||DeepInside(b,solid,tolerance)||DeepInside(c,solid,tolerance)||
             DeepInside((a+b+c)/3f,solid,tolerance)))return false;
        Vector3 edge=b-a,normal=Vector3.Cross(edge,c-a);
        Quaternion rotation=normal.sqrMagnitude>1e-16f&&edge.sqrMagnitude>1e-16f?
            Quaternion.LookRotation(edge.normalized,normal.normalized):Quaternion.identity;
        Quaternion inverse=Quaternion.Inverse(rotation);
        var bounds=new Bounds(inverse*a,Vector3.zero);bounds.Encapsulate(inverse*b);bounds.Encapsulate(inverse*c);
        bounds.Expand(shell*2);boxQuery.size=bounds.size;
        bool penetration=Physics.ComputePenetration(boxQuery,rotation*bounds.center,rotation,solid,solid.transform.position,
            solid.transform.rotation,out _,out float depth);
        if(penetration&&depth<=tolerance)return true;
        if(!penetration)
        {
            if(!(solid is MeshCollider nonconvex)||nonconvex.convex)return true;
            var envelope=new CatBodyGuardBox{centre=rotation*bounds.center,halfExtents=bounds.extents,rotation=rotation};
            Vector3 ex=rotation*Vector3.right*bounds.extents.x,ey=rotation*Vector3.up*bounds.extents.y,ez=rotation*Vector3.forward*bounds.extents.z;
            Vector3 worldExtent=new Vector3(Mathf.Abs(ex.x)+Mathf.Abs(ey.x)+Mathf.Abs(ez.x),Mathf.Abs(ex.y)+Mathf.Abs(ey.y)+Mathf.Abs(ez.y),Mathf.Abs(ex.z)+Mathf.Abs(ey.z)+Mathf.Abs(ez.z));
            if(!solid.bounds.Intersects(new Bounds(envelope.centre,worldExtent*2)))return true;
            if(!CatMeshContactSurface.TryBoxBoundary(nonconvex.sharedMesh,nonconvex.transform,envelope,out bool boundary))return false;
            bool inside=DeepInside(envelope.centre,solid,0);
            if(!boundary&&!inside)return true;
            // Unsigned distance is 1-Lipschitz. If even the farthest corner
            // stays inside the existing tolerance, the whole envelope is safe.
            if(CatMeshContactSurface.TryMetric(nonconvex.sharedMesh,nonconvex.transform,envelope.centre,out float nearest,out _)&&
                nearest+envelope.halfExtents.magnitude<=tolerance)return true;
        }
        if(subdivisions<=0)return false;
        Vector3 ab=(a+b)*.5f,bc=(b+c)*.5f,ca=(c+a)*.5f;
        int next=subdivisions-1;
        return IsWorldTriangleClear(a,ab,ca,solid,tolerance,next,shell)&&IsWorldTriangleClear(ab,b,bc,solid,tolerance,next,shell)&&
            IsWorldTriangleClear(ca,bc,c,solid,tolerance,next,shell)&&IsWorldTriangleClear(ab,bc,ca,solid,tolerance,next,shell);
    }

    static Bounds SurfaceWorldBounds(CatBodyGuardBox box)
    {
        Vector3 x=box.rotation*Vector3.right*box.halfExtents.x,y=box.rotation*Vector3.up*box.halfExtents.y,z=box.rotation*Vector3.forward*box.halfExtents.z;
        Vector3 extent=new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
        return new Bounds(box.centre,extent*2);
    }

    bool DeepInside(Vector3 point,Collider solid,float tolerance)
    {
        var key=(solid,point,tolerance);
        if(surfaceInsideCache.TryGetValue(key,out bool value))return value;
        if(!solid.bounds.Contains(point)){surfaceInsideCache[key]=false;return false;}
        bool previous=Physics.queriesHitBackfaces;int exits=0;float closest=float.PositiveInfinity;
        var original=solid as MeshCollider;bool unknownWinding=false;
        try
        {
            Physics.queriesHitBackfaces=true;
            float distance=solid.bounds.size.magnitude+.01f;
            foreach(var direction in interiorDirections)
            {
                if(!solid.Raycast(new Ray(point,direction),out var hit,distance))continue;
                Vector3 normal=hit.normal;
                if(original!=null&&!CatMeshContactSurface.TryOriginalTriangleNormal(original.sharedMesh,original.transform,hit.triangleIndex,out normal))
                {unknownWinding=true;continue;}
                if(Vector3.Dot(normal,direction)>.0001f){exits++;closest=Mathf.Min(closest,hit.distance);}
            }
        }
        finally{Physics.queriesHitBackfaces=previous;}
        // A missing original winding cannot certify empty volume. Bake known
        // Paw/care meshes; retain refusal for unsupported unreadable geometry.
        if(unknownWinding){surfaceInsideCache[key]=true;return true;}
        // Exit votes classify volume; axis-ray length is only a conservative
        // fallback metric. Oblique surfaces need the actual Euclidean distance.
        if(exits>=4&&original!=null&&CatMeshContactSurface.TryMetric(original.sharedMesh,original.transform,point,out float exact,out _))closest=exact;
        value=exits>=4&&closest>tolerance;surfaceInsideCache[key]=value;return value;
    }

    private bool PoseAllowed(Vector3 position, Quaternion rotation)
    {
        for (int i = 0; i < probes.Length; i++)
        {
            WorldProbe(i, position, rotation, out var a, out var b, out float radius);
            if (boundary != null)
                for (int side = 0; side < 4; side++)
                    if (BoundaryDepth(a, b, radius, side) > initialBoundaryDepth[side] + ContactTolerance) return false;
            int count = Physics.OverlapCapsuleNonAlloc(a, b, radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int c = 0; c < count; c++)
            {
                var solid = overlaps[c];
                if (!Solid(solid) || !Penetration(a, b, radius, solid, out _, out float depth)) continue;
                float previous = FindContact(i, solid, out var contact) ? contact.depth : 0f;
                if (depth > previous + ContactTolerance) return false;
            }
        }
        return true;
    }

    private Vector3 ClampBoundary(Vector3 position, Quaternion rotation)
    {
        if (boundary == null) return position;
        for (int i = 0; i < probes.Length; i++)
        {
            WorldProbe(i, position, rotation, out var a, out var b, out float radius);
            position.x += Mathf.Max(0f, BoundaryDepth(a, b, radius, 0) - initialBoundaryDepth[0]);
            position.x -= Mathf.Max(0f, BoundaryDepth(a, b, radius, 1) - initialBoundaryDepth[1]);
            position.z += Mathf.Max(0f, BoundaryDepth(a, b, radius, 2) - initialBoundaryDepth[2]);
            position.z -= Mathf.Max(0f, BoundaryDepth(a, b, radius, 3) - initialBoundaryDepth[3]);
        }
        return position;
    }

    private float BoundaryDepth(Vector3 a, Vector3 b, float radius, int side)
    {
        float clearance = boundary.EdgeClearance + boundaryClearance;
        switch (side)
        {
            case 0: return boundary.MinimumXZ.x + clearance - (Mathf.Min(a.x, b.x) - radius);
            case 1: return Mathf.Max(a.x, b.x) + radius - (boundary.MaximumXZ.x - clearance);
            case 2: return boundary.MinimumXZ.y + clearance - (Mathf.Min(a.z, b.z) - radius);
            default: return Mathf.Max(a.z, b.z) + radius - (boundary.MaximumXZ.y - clearance);
        }
    }

    private void WorldProbe(int index, Vector3 position, Quaternion rotation, out Vector3 a, out Vector3 b, out float radius)
    {
        var p = probes[index];
        a = position + rotation * p.start;
        b = position + rotation * p.end;
        radius = p.radius;
    }

    private bool Penetration(Vector3 a, Vector3 b, float radius, Collider solid, out Vector3 normal, out float depth)
    {
        query.radius = radius;
        query.height = Vector3.Distance(a, b) + radius * 2f;
        Quaternion rotation = (b - a).sqrMagnitude > .000001f ? Quaternion.LookRotation(b - a) : Quaternion.identity;
        return Physics.ComputePenetration(query, (a + b) * .5f, rotation,
            solid, solid.transform.position, solid.transform.rotation, out normal, out depth);
    }

    private bool FindContact(int index, Collider solid, out Contact contact)
    {
        for (int i = 0; i < contactCount; i++)
            if (contacts[i].probe == index && contacts[i].collider == solid) { contact = contacts[i]; return true; }
        contact = default;
        return false;
    }

    private bool Solid(Collider solid) => solid != null && solid.enabled && !solid.isTrigger &&
        solid != query && solid != controller && solid.gameObject.scene == actor.gameObject.scene &&
        !solid.transform.IsChildOf(actor) && !Physics.GetIgnoreLayerCollision(actor.gameObject.layer, solid.gameObject.layer) &&
        (controller == null || !Physics.GetIgnoreCollision(controller, solid));

    public void Dispose()
    {
        if (query != null) UnityEngine.Object.Destroy(query.gameObject);
        query = null;
        boxQuery = null;
    }
}
