using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Read-only triangle geometry for real prop contact. Imported meshes remain
/// unreadable in players; the editor copies only the required local geometry.
/// Import as Assets/Scripts/Home/CatMeshContactSurface.cs, then call Build().
/// </summary>
public sealed partial class CatMeshContactSurface : ScriptableObject
{
    public const string ResourceName = "Home/CatMeshContactSurface";
    public const string AssetPath = "Assets/Resources/Home/CatMeshContactSurface.asset";

    [Serializable]
    public struct BvhNode
    {
        public Bounds bounds;
        public int left, right, first, count;
    }

    [Serializable]
    public sealed class Geometry
    {
        public Mesh mesh;
        public Vector3[] vertices = Array.Empty<Vector3>();
        public int[] triangles = Array.Empty<int>();
        // Triangle IDs, never reordered vertex/index data. Old catalogs use the
        // exact brute-force fallback until the next explicit editor Build().
        public BvhNode[] nodes = Array.Empty<BvhNode>();
        public int[] triangleOrder = Array.Empty<int>();
    }

    [SerializeField] Geometry[] geometries = Array.Empty<Geometry>();
    static CatMeshContactSurface loaded;
    static ResourceRequest catalogRequest;
    static bool catalogMissing;
    static CatMeshContactSurface ReadCatalog()
    {
        if(loaded!=null)return loaded.EnsurePacked(!Application.isPlaying)?loaded:null;
        if(catalogMissing)return null;
        if(!Application.isPlaying){loaded=Resources.Load<CatMeshContactSurface>(ResourceName);return loaded!=null&&loaded.EnsurePacked(true)?loaded:null;}
        if(catalogRequest==null)catalogRequest=Resources.LoadAsync<CatMeshContactSurface>(ResourceName);
        if(!catalogRequest.isDone)return null;
        loaded=catalogRequest.asset as CatMeshContactSurface;catalogRequest=null;catalogMissing=loaded==null;
        return loaded!=null&&loaded.EnsurePacked(false)?loaded:null;
    }
    public static void Preload()=>ReadCatalog();
    public static bool IsReady=>ReadCatalog()!=null;
    public static bool HasPendingLoad
    {
        get
        {
            // Callers that only wait on pending work must also publish a
            // completed decode; readiness must not require a second API.
            if(catalogRequest!=null||loaded!=null)ReadCatalog();
            return (catalogRequest!=null&&!catalogRequest.isDone)||(loaded!=null&&loaded.PackedPending);
        }
    }
#if UNITY_EDITOR
    public static void EditorClearResourceCacheForQa()
    {
        if(HasPendingLoad)throw new InvalidOperationException("Cannot unload an unfinished geometry request.");
        if(loaded!=null)Resources.UnloadAsset(loaded);
        loaded=null;catalogRequest=null;catalogMissing=false;
        readableMetricGeometry.Clear();metricWorlds.Clear();
    }
#endif
    Dictionary<Mesh, Geometry> byMesh;

    Geometry Find(Mesh mesh)
    {
        if(!EnsurePacked(true))return null;
        if (byMesh == null)
        {
            byMesh = new Dictionary<Mesh, Geometry>();
            foreach (var data in GeometryData)
                if (data != null && data.mesh != null) byMesh[data.mesh] = data;
        }
        return mesh != null && byMesh.TryGetValue(mesh, out var value) ? value : null;
    }

    public static bool HasGeometry(Mesh mesh)
    {
        // Normal room/breed readiness preloads this data. Keep the synchronous
        // fallback for isolated scenes and callers before that readiness gate:
        // a temporary null must never become a permanently empty target set.
        if(loaded==null)loaded=Resources.Load<CatMeshContactSurface>(ResourceName);
        return loaded!=null&&loaded.Find(mesh)!=null;
    }
    // Original triangle winding is independent of ray-facing hit normals.
    public static bool TryWorldNormal(Mesh mesh,Transform transform,int triangle,out Vector3 normal)
    {
        normal=Vector3.zero;
        if(!HasGeometry(mesh)||transform==null)return false;
        var data=loaded.Find(mesh);int at=triangle*3;
        if(at<0||at+2>=data.triangles.Length)return false;
        Vector3 a=transform.TransformPoint(data.vertices[data.triangles[at]]),
            b=transform.TransformPoint(data.vertices[data.triangles[at+1]]),c=transform.TransformPoint(data.vertices[data.triangles[at+2]]);
        normal=OutwardWorldCross(b-a,c-a,transform.localToWorldMatrix);return normal.sqrMagnitude>1e-18f;
    }

    // A reflected transform reverses world triangle winding, but it does not
    // turn occupied volume inside out. det(M)*M^-T maps a cross product;
    // removing det(M)'s sign preserves the original outward orientation.
    // Distance/SAT calculations keep their original world-space vertices.
    static Vector3 OutwardWorldCross(Vector3 firstEdge,Vector3 secondEdge,Matrix4x4 matrix)
    {
        float determinant=matrix.determinant;
        if(determinant==0f||float.IsNaN(determinant)||float.IsInfinity(determinant))return Vector3.zero;
        return Vector3.Cross(firstEdge,secondEdge)*(determinant<0f?-1f:1f);
    }

    public struct Hit
    {
        public MeshFilter Filter;
        public Mesh Mesh;
        public Vector3 LocalPoint, LocalNormal;
        internal TargetSet Owner;
        public Vector3 Normal => Filter != null ? Filter.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(LocalNormal).normalized : Vector3.zero;
        public int Triangle;
        public bool IsValid => Filter != null && Filter.sharedMesh == Mesh && Filter.gameObject.activeInHierarchy &&
            Filter.TryGetComponent<Renderer>(out var renderer) && renderer.enabled;
        public Vector3 Point => Filter != null ? Filter.transform.TransformPoint(LocalPoint) : Vector3.zero;
    }

    /// <summary>A small per-prop view; world vertices update only when its transform changes.</summary>
    public sealed partial class TargetSet
    {
        sealed class Part
        {
            public MeshFilter filter;
            public Renderer renderer;
            public Geometry data;
            public Vector3[] world;
            public Matrix4x4 matrix;
            public Bounds bounds;
            public Bounds[] worldNodes;
            public int[] stack;
            public bool transformed;
            public bool HasHierarchy => data.nodes != null && data.nodes.Length > 0 &&
                data.triangleOrder != null && data.triangleOrder.Length == data.triangles.Length / 3;
        }
        readonly Part[] parts;
        readonly CatMeshContactSurface catalog;
        public readonly Transform Root;
        // Work counters are deterministic diagnostics, not elapsed-time gates.
        public int LastTriangleTests { get; private set; }
        public int LastNodeVisits { get; private set; }

        public TargetSet(Transform root)
        {
            Root = root;
            if (loaded == null) loaded = Resources.Load<CatMeshContactSurface>(ResourceName);
            catalog = loaded;
            if (root == null || catalog == null) { parts = Array.Empty<Part>(); return; }
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            parts = new Part[filters.Length];
            for (int i = 0; i < filters.Length; i++)
                parts[i] = new Part { filter = filters[i], renderer = filters[i].GetComponent<Renderer>() };
        }

        public bool TryClosest(Vector3 point, out Hit hit)
        {
            hit = default; float best = float.PositiveInfinity; bool found = false;
            LastTriangleTests = LastNodeVisits = 0;
            if (catalog == null || Root == null) return false;
            foreach (var part in parts)
            {
                if (part.filter == null || part.renderer == null || !part.renderer.enabled ||
                    !part.filter.gameObject.activeInHierarchy) continue;
                Mesh mesh = part.filter.sharedMesh;
                if (part.data == null || part.data.mesh != mesh)
                {
                    part.data = catalog.Find(mesh); part.transformed = false;
                    part.world = part.data != null ? new Vector3[part.data.vertices.Length] : null;
                    int nodeCount = part.data != null && part.HasHierarchy ? part.data.nodes.Length : 0;
                    part.worldNodes = nodeCount > 0 ? new Bounds[nodeCount] : null;
                    // One allocation on mesh binding, none during a query.
                    // Size is bounded by the baked tree, so no fixed-depth overflow.
                    part.stack = nodeCount > 0 ? new int[nodeCount] : null;
                }
                if (part.data == null || part.world.Length == 0) continue;
                Matrix4x4 matrix = part.filter.transform.localToWorldMatrix;
                if (!part.transformed || !part.matrix.Equals(matrix))
                {
                    for (int i = 0; i < part.world.Length; i++) part.world[i] = matrix.MultiplyPoint3x4(part.data.vertices[i]);
                    if (part.HasHierarchy)
                    {
                        for (int i = 0; i < part.worldNodes.Length; i++)
                            part.worldNodes[i] = TransformBounds(matrix, part.data.nodes[i].bounds);
                        part.bounds = part.worldNodes[0];
                    }
                    else
                    {
                        part.bounds = new Bounds(part.world[0], Vector3.zero);
                        foreach (Vector3 vertex in part.world) part.bounds.Encapsulate(vertex);
                    }
                    part.matrix = matrix; part.transformed = true;
                }
                if (part.bounds.SqrDistance(point) > best) continue; // broad rejection only
                int bestTriangleInPart = -1;
                if (!part.HasHierarchy)
                {
                    for (int triangle = 0; triangle < part.data.triangles.Length / 3; triangle++)
                        TestTriangle(part, mesh, triangle, point, ref best, ref found, ref bestTriangleInPart, ref hit);
                    continue;
                }
                int top = 0; part.stack[top++] = 0;
                while (top > 0)
                {
                    int nodeIndex = part.stack[--top]; LastNodeVisits++;
                    if (part.worldNodes[nodeIndex].SqrDistance(point) > best) continue;
                    var node = part.data.nodes[nodeIndex];
                    if (node.count > 0)
                    {
                        for (int i = node.first; i < node.first + node.count; i++)
                            TestTriangle(part, mesh, part.data.triangleOrder[i], point,
                                ref best, ref found, ref bestTriangleInPart, ref hit);
                        continue;
                    }
                    float left = part.worldNodes[node.left].SqrDistance(point);
                    float right = part.worldNodes[node.right].SqrDistance(point);
                    // LIFO: visit the nearer child first; bounds are WORLD AABBs.
                    // They remain conservative under rotation, shear, reflection,
                    // and non-uniform scale. Only actual world triangles win.
                    int near = left <= right ? node.left : node.right;
                    int far = left <= right ? node.right : node.left;
                    float nearDistance = Mathf.Min(left, right), farDistance = Mathf.Max(left, right);
                    if (farDistance <= best) part.stack[top++] = far;
                    if (nearDistance <= best) part.stack[top++] = near;
                }
            }
            return found;
        }

        void TestTriangle(Part part, Mesh mesh, int triangle, Vector3 point,
            ref float best, ref bool found, ref int bestTriangleInPart, ref Hit hit)
        {
            LastTriangleTests++;
            int index = triangle * 3; int[] indices = part.data.triangles;
            Vector3 closest = ClosestTriangle(point, part.world[indices[index]], part.world[indices[index + 1]], part.world[indices[index + 2]]);
            float distance = (point - closest).sqrMagnitude;
            // Keep the old brute-force tie rule: earlier part, then lower
            // original triangle ID. Tree ordering must not change a contact.
            if (distance > best || (distance == best && (bestTriangleInPart < 0 || triangle >= bestTriangleInPart))) return;
            best = distance; found = true; bestTriangleInPart = triangle;
            hit = new Hit { Filter = part.filter, Mesh = mesh, Triangle = triangle,
                LocalPoint = part.filter.transform.InverseTransformPoint(closest), Owner = this,
                LocalNormal = Vector3.Cross(part.data.vertices[indices[index + 1]] - part.data.vertices[indices[index]],
                    part.data.vertices[indices[index + 2]] - part.data.vertices[indices[index]]).normalized };
        }

        static Bounds TransformBounds(Matrix4x4 matrix, Bounds local)
        {
            Vector3 e = local.extents;
            Vector3 world = new Vector3(
                Mathf.Abs(matrix.m00) * e.x + Mathf.Abs(matrix.m01) * e.y + Mathf.Abs(matrix.m02) * e.z,
                Mathf.Abs(matrix.m10) * e.x + Mathf.Abs(matrix.m11) * e.y + Mathf.Abs(matrix.m12) * e.z,
                Mathf.Abs(matrix.m20) * e.x + Mathf.Abs(matrix.m21) * e.y + Mathf.Abs(matrix.m22) * e.z);
            Vector3 centre = matrix.MultiplyPoint3x4(local.center);
            // Conservative roundoff padding affects pruning only. It cannot
            // change the selected triangle point or any contact tolerance.
            Vector3 pad = (new Vector3(Mathf.Abs(centre.x), Mathf.Abs(centre.y), Mathf.Abs(centre.z)) + world + Vector3.one) * .000001f;
            return new Bounds(centre, (world + pad) * 2f);
        }
    }

    // Voronoi-region closest point. The computation is in WORLD space, so a
    // rotated or non-uniformly scaled prop still yields a metric closest point.
    static Vector3 ClosestTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a, ac = c - a;
        if (Vector3.Cross(ab, ac).sqrMagnitude < 1e-16f)
        {
            Vector3 first = ClosestSegment(p, a, b), second = ClosestSegment(p, b, c), third = ClosestSegment(p, c, a);
            Vector3 best = (p - first).sqrMagnitude <= (p - second).sqrMagnitude ? first : second;
            return (p - best).sqrMagnitude <= (p - third).sqrMagnitude ? best : third;
        }
        Vector3 ap = p - a;
        float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0 && d2 <= 0) return a;
        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0 && d4 <= d3) return b;
        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0 && d1 >= 0 && d3 <= 0) return a + ab * (d1 / (d1 - d3));
        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0 && d5 <= d6) return c;
        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0 && d2 >= 0 && d6 <= 0) return a + ac * (d2 / (d2 - d6));
        float va = d3 * d6 - d5 * d4;
        if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0) return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
        float denominator = 1f / (va + vb + vc);
        return a + ab * (vb * denominator) + ac * (vc * denominator);
    }

    static Vector3 ClosestSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 delta = b - a;
        return a + delta * (delta.sqrMagnitude > 1e-16f ? Mathf.Clamp01(Vector3.Dot(point - a, delta) / delta.sqrMagnitude) : 0f);
    }

#if UNITY_EDITOR
    static void BuildHierarchy(Geometry data)
    {
        const int leafSize = 12;
        int count = data.triangles.Length / 3;
        data.triangleOrder = new int[count];
        var bounds = new Bounds[count]; var centres = new Vector3[count];
        for (int triangle = 0; triangle < count; triangle++)
        {
            int start = triangle * 3; Vector3 a = data.vertices[data.triangles[start]];
            Vector3 b = data.vertices[data.triangles[start + 1]], c = data.vertices[data.triangles[start + 2]];
            bounds[triangle] = new Bounds(a, Vector3.zero); bounds[triangle].Encapsulate(b); bounds[triangle].Encapsulate(c);
            centres[triangle] = (a + b + c) / 3f; data.triangleOrder[triangle] = triangle;
        }
        var nodes = new List<BvhNode>(Mathf.Max(1, count / leafSize * 3));
        int BuildNode(int first, int length)
        {
            int index = nodes.Count; nodes.Add(default);
            Bounds bound = bounds[data.triangleOrder[first]], centroid = new Bounds(centres[data.triangleOrder[first]], Vector3.zero);
            for (int i = first + 1; i < first + length; i++)
            { bound.Encapsulate(bounds[data.triangleOrder[i]]); centroid.Encapsulate(centres[data.triangleOrder[i]]); }
            if (length <= leafSize)
            { nodes[index] = new BvhNode { bounds = bound, first = first, count = length, left = -1, right = -1 }; return index; }
            Vector3 size = centroid.size;
            int axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
            Array.Sort(data.triangleOrder, first, length, Comparer<int>.Create((a, b) =>
            { int result = centres[a][axis].CompareTo(centres[b][axis]); return result != 0 ? result : a.CompareTo(b); }));
            int middle = length / 2;
            int left = BuildNode(first, middle), right = BuildNode(first + middle, length - middle);
            nodes[index] = new BvhNode { bounds = bound, left = left, right = right };
            return index;
        }
        if (count > 0) BuildNode(0, count);
        data.nodes = nodes.ToArray();
    }

    /// <summary>Copies required contact/support triangles; preserves unchanged baked geometry; saves only this catalog. Never changes prefab/FBX/import settings.</summary>
    public static string Build()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Bake contact geometry outside Play Mode.");
        var meshes = new HashSet<Mesh>();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Art/StoreProducts/Prefabs" }))
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if (prefab == null) continue;
            // Shared jump/body checks can encounter every placed solid, not
            // only props with a paw-contact activity. Bake their real triangles
            // once so an unreadable imported model is never an unknown hole.
            foreach(var solid in prefab.GetComponentsInChildren<MeshCollider>(true))
                if(solid.enabled&&!solid.isTrigger&&solid.sharedMesh!=null)meshes.Add(solid.sharedMesh);
            foreach(var care in prefab.GetComponentsInChildren<MealTimeActivity>(true))
                foreach(var filter in care.GetComponentsInChildren<MeshFilter>(true))
                    if(filter.sharedMesh!=null&&filter.GetComponent<Renderer>()!=null)meshes.Add(filter.sharedMesh);
            foreach (var knock in prefab.GetComponentsInChildren<KnockOffActivity>(true))
            {
                if (knock.GlassPivot == null) continue;
                foreach (var filter in knock.GlassPivot.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh != null && filter.GetComponent<Renderer>() != null) meshes.Add(filter.sharedMesh);
            }
            foreach(var watch in prefab.GetComponentsInChildren<SitLookActivity>(true))
            {
                if(watch.Kind!=CatActivityKind.RailingSwat)continue;
                foreach(var solid in watch.GetComponentsInChildren<MeshCollider>(true))
                    if(solid.enabled&&!solid.isTrigger&&solid.sharedMesh!=null)meshes.Add(solid.sharedMesh);
            }
            foreach(var knock in prefab.GetComponentsInChildren<KnockOffActivity>(true))
                foreach(var solid in knock.GetComponentsInChildren<MeshCollider>(true))
                    if(solid.enabled&&!solid.isTrigger&&solid.sharedMesh!=null)meshes.Add(solid.sharedMesh);
            foreach (var cart in prefab.GetComponentsInChildren<CartNudgeActivity>(true))
            {
                if (cart.CartVisual == null) continue;
                foreach (var filter in cart.CartVisual.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh != null && filter.GetComponent<Renderer>() != null) meshes.Add(filter.sharedMesh);
            }
            foreach (var activity in prefab.GetComponentsInChildren<CatEnrichmentActivity>(true))
            {
                if (activity.Mode == CatEnrichmentMode.Nap || activity.Mode == CatEnrichmentMode.Hide || activity.Mode == CatEnrichmentMode.Tunnel) continue;
                if (activity.Mode == CatEnrichmentMode.Feed && activity.MovingPart == null) continue;
                Transform root = activity.MovingPart != null ? activity.MovingPart : activity.transform;
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh != null && filter.GetComponent<Renderer>() != null) meshes.Add(filter.sharedMesh);
            }
        }
        foreach(string name in new[]{"MainFoodBowl","MainWaterBowl","CareStationTray",
            "GardenHammockBed","LoftFloorCushions","GardenHammock","KitchenIsland","KitchenCounterStool",
            "BathroomTub","BedroomStarCanopy","LoftChaiseLounge","LoftBeanBag","BalconyHangingChair"})
        {
            var care=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PremiumFurniture/Models/"+name+"_Premium.fbx");
            if(care!=null)foreach(var filter in care.GetComponentsInChildren<MeshFilter>(true))
                if(filter.sharedMesh!=null)meshes.Add(filter.sharedMesh);
        }
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<CatMeshContactSurface>(AssetPath);
        // Keep existing entries and their order. Adding required support
        // meshes must not discard unrelated, already baked contact geometry.
        var previous = new Dictionary<Mesh, Geometry>();
        var ordered = new List<Mesh>();
        if(asset!=null&&!asset.EnsurePacked(true))throw new InvalidOperationException("Existing contact geometry could not load.");
        if (asset != null && asset.GeometryData != null)
            foreach (var existing in asset.GeometryData)
                if (existing != null && existing.mesh != null && !previous.ContainsKey(existing.mesh))
                { previous.Add(existing.mesh, existing); ordered.Add(existing.mesh); meshes.Remove(existing.mesh); }
        var additions = new List<Mesh>(meshes);
        additions.Sort((a,b) => string.CompareOrdinal(UnityEditor.AssetDatabase.GetAssetPath(a),
            UnityEditor.AssetDatabase.GetAssetPath(b)));
        ordered.AddRange(additions);
        var data = new List<Geometry>(); int vertexCount = 0, triangleCount = 0;
        foreach (Mesh mesh in ordered)
        {
            using (var source = UnityEditor.MeshUtility.AcquireReadOnlyMeshData(mesh))
            using (var vertices = new Unity.Collections.NativeArray<Vector3>(source[0].vertexCount, Unity.Collections.Allocator.Temp))
            {
                source[0].GetVertices(vertices);
                var triangles = new List<int>();
                for (int sub = 0; sub < source[0].subMeshCount; sub++)
                {
                    var descriptor = source[0].GetSubMesh(sub);
                    if (descriptor.topology != MeshTopology.Triangles) continue;
                    using (var indices = new Unity.Collections.NativeArray<int>(descriptor.indexCount, Unity.Collections.Allocator.Temp))
                    { source[0].GetIndices(indices, sub); triangles.AddRange(indices.ToArray()); }
                }
                if (vertices.Length == 0 || triangles.Count == 0) throw new InvalidOperationException("No contact triangles: " + mesh.name);
                foreach (int index in triangles)
                    if (index < 0 || index >= vertices.Length) throw new InvalidOperationException("Invalid contact index: " + mesh.name);
                var geometry = new Geometry { mesh = mesh, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
                // Preserve all arrays of unchanged existing entries exactly;
                // still refresh genuinely changed source geometry on a later bake.
                if (previous.TryGetValue(mesh, out var existing) && SameBakedGeometry(existing, geometry)) geometry = existing;
                else BuildHierarchy(geometry);
                data.Add(geometry);
                vertexCount += vertices.Length; triangleCount += triangles.Count / 3;
            }
        }
        if (data.Count == 0) throw new InvalidOperationException("No enrichment prop meshes found.");
        if (asset == null)
        {
            asset = CreateInstance<CatMeshContactSurface>();
            UnityEditor.AssetDatabase.CreateAsset(asset, AssetPath);
        }
        asset.WritePacked(data.ToArray());
        UnityEditor.EditorUtility.SetDirty(asset);
        UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);
        loaded = asset;
        return "Contact surfaces: " + data.Count + " meshes, " + vertexCount + " vertices, " + triangleCount + " triangles.";
    }
    static bool SameBakedGeometry(Geometry existing, Geometry source)
    {
        if (existing.vertices == null || existing.triangles == null || existing.nodes == null || existing.nodes.Length == 0 ||
            existing.triangleOrder == null || existing.triangleOrder.Length != source.triangles.Length / 3 ||
            existing.vertices.Length != source.vertices.Length || existing.triangles.Length != source.triangles.Length) return false;
        for (int i = 0; i < source.vertices.Length; i++) if (!existing.vertices[i].Equals(source.vertices[i])) return false;
        for (int i = 0; i < source.triangles.Length; i++) if (existing.triangles[i] != source.triangles[i]) return false;
        return true;
    }
#endif
}
