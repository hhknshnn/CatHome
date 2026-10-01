using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Transient EditMode geometry only. No scene/asset/save writes or Build() call.
/// Brute force enumerates ORIGINAL triangle order and shares only the unchanged
/// closest-triangle kernel: this tests the new hierarchy/pruning/cache exactly.
/// </summary>
public sealed class CatMeshContactBvhTests
{
    const BindingFlags HiddenStatic = BindingFlags.Static | BindingFlags.NonPublic;
    const BindingFlags HiddenInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly FieldInfo Loaded = typeof(CatMeshContactSurface).GetField("loaded", HiddenStatic);
    static readonly FieldInfo Geometries = typeof(CatMeshContactSurface).GetField("geometries", HiddenInstance);
    static readonly MethodInfo Bake = typeof(CatMeshContactSurface).GetMethod("BuildHierarchy", HiddenStatic);
    static readonly Func<Vector3, Vector3, Vector3, Vector3, Vector3> Closest =
        (Func<Vector3, Vector3, Vector3, Vector3, Vector3>)Delegate.CreateDelegate(
            typeof(Func<Vector3, Vector3, Vector3, Vector3, Vector3>),
            typeof(CatMeshContactSurface).GetMethod("ClosestTriangle", HiddenStatic));

    [Test]
    public void ExactClosest_MatchesOriginalBruteForce_For500QueriesAndChangingAffineTransforms()
    {
        using (var fixture = new Fixture())
        {
            var set = new CatMeshContactSurface.TargetSet(fixture.root.transform);
            int totalTests = 0, count = 0;
            for (int pose = 0; pose < 5; pose++)
            {
                // Last two create shear through a rotated child below a
                // non-uniform parent; the last also reflects one axis.
                fixture.root.transform.SetPositionAndRotation(new Vector3(3.1f * pose, -.2f * pose, -2.7f * pose), Quaternion.Euler(13 * pose, 31 * pose, -9 * pose));
                fixture.root.transform.localScale = pose < 2 ? Vector3.one : new Vector3(pose == 4 ? -1.9f : 1.9f, .37f, 2.7f);
                fixture.part.transform.localRotation = pose >= 3 ? Quaternion.Euler(38, 17, -26) : Quaternion.identity;
                fixture.part.transform.localScale = pose == 1 ? new Vector3(.31f, 2.2f, 1.6f) : Vector3.one;
                Matrix4x4 matrix = fixture.part.transform.localToWorldMatrix;
                var random = new System.Random(771 + pose);
                for (int query = 0; query < 100; query++)
                {
                    Vector3 local = query == 0 ? fixture.geometry.vertices[0] : new Vector3(
                        (float)random.NextDouble() * 3.2f - 1.6f,
                        (float)random.NextDouble() * 2f - 1f,
                        (float)random.NextDouble() * 3.2f - 1.6f);
                    Vector3 point = matrix.MultiplyPoint3x4(local);
                    Assert.That(set.TryClosest(point, out var hit), Is.True);
                    Brute(fixture.geometry, matrix, point, out Vector3 expected, out int triangle);
                    Assert.That(hit.Triangle, Is.EqualTo(triangle), "Original-ID tie rule, pose/query " + pose + "/" + query);
                    Assert.That(Vector3.Distance(hit.Point, expected), Is.LessThan(.00002f), "Exact world contact, pose/query " + pose + "/" + query);
                    Assert.That(hit.IsValid, Is.True);
                    Assert.That(set.LastNodeVisits, Is.GreaterThan(0));
                    totalTests += set.LastTriangleTests; count++;
                }
            }
            Assert.That(count, Is.EqualTo(500));
            // A work bound, not a hardware timing assumption. Performance
            // cannot pass merely because the implementation skipped geometry.
            int bruteWork = count * fixture.geometry.triangles.Length / 3;
            Assert.That(totalTests, Is.LessThan(bruteWork / 3), "BVH must reduce exact triangle work by > 3x on this nontrivial mesh.");
            TestContext.WriteLine("BVH exact triangles " + totalTests + " / brute " + bruteWork);
        }
    }

    [Test]
    public void Cache_RebindsReplacementMesh_FollowsMovement_AndHonoursVisibility()
    {
        using (var fixture = new Fixture())
        {
            var set = new CatMeshContactSurface.TargetSet(fixture.root.transform);
            Assert.That(set.TryClosest(new Vector3(.25f, .5f, .1f), out var first), Is.True);
            Vector3 original = first.Point;
            fixture.part.transform.position += new Vector3(1, .2f, -.3f);
            Assert.That(Vector3.Distance(first.Point - original, new Vector3(1, .2f, -.3f)), Is.LessThan(.000001f));
            Vector3 query = new Vector3(1.2f, .5f, -.1f);
            Assert.That(set.TryClosest(query, out var moved), Is.True);
            Brute(fixture.geometry, fixture.part.transform.localToWorldMatrix, query, out var expected, out _);
            Assert.That(Vector3.Distance(moved.Point, expected), Is.LessThan(.00001f));
            fixture.filter.sharedMesh = fixture.replacement.mesh;
            Assert.That(first.IsValid, Is.False);
            Assert.That(set.TryClosest(query, out var rebound), Is.True);
            Brute(fixture.replacement, fixture.part.transform.localToWorldMatrix, query, out expected, out _);
            Assert.That(rebound.Mesh, Is.SameAs(fixture.replacement.mesh));
            Assert.That(Vector3.Distance(rebound.Point, expected), Is.LessThan(.00001f));
            fixture.renderer.enabled = false;
            Assert.That(set.TryClosest(query, out _), Is.False);
            fixture.renderer.enabled = true; fixture.part.SetActive(false);
            Assert.That(set.TryClosest(query, out _), Is.False);
        }
    }

    [Test]
    public void UnbakedCatalog_UsesUnchangedExactFallback_AndTreeContainsEveryTriangleOnce()
    {
        using (var fixture = new Fixture())
        {
            var ids = (int[])fixture.geometry.triangleOrder.Clone(); Array.Sort(ids);
            Assert.That(ids.Length, Is.EqualTo(fixture.geometry.triangles.Length / 3));
            for (int i = 0; i < ids.Length; i++) Assert.That(ids[i], Is.EqualTo(i));
            Assert.That(fixture.geometry.nodes.Length, Is.GreaterThan(1));
            fixture.geometry.nodes = Array.Empty<CatMeshContactSurface.BvhNode>();
            fixture.geometry.triangleOrder = Array.Empty<int>();
            var set = new CatMeshContactSurface.TargetSet(fixture.root.transform);
            Vector3 point = new Vector3(.3f, .4f, -.12f);
            Assert.That(set.TryClosest(point, out var hit), Is.True);
            Brute(fixture.geometry, fixture.part.transform.localToWorldMatrix, point, out var expected, out int triangle);
            Assert.That(hit.Triangle, Is.EqualTo(triangle));
            Assert.That(Vector3.Distance(hit.Point, expected), Is.LessThan(.000001f));
            Assert.That(set.LastTriangleTests, Is.EqualTo(ids.Length));
            Assert.That(set.LastNodeVisits, Is.Zero);
        }
    }

    static void Brute(CatMeshContactSurface.Geometry data, Matrix4x4 matrix, Vector3 point, out Vector3 closest, out int triangle)
    {
        closest = default; triangle = -1; float best = float.PositiveInfinity;
        var world = new Vector3[data.vertices.Length];
        for (int i = 0; i < world.Length; i++) world[i] = matrix.MultiplyPoint3x4(data.vertices[i]);
        for (int i = 0; i < data.triangles.Length; i += 3)
        {
            Vector3 candidate = Closest(point, world[data.triangles[i]], world[data.triangles[i + 1]], world[data.triangles[i + 2]]);
            float squared = (candidate - point).sqrMagnitude;
            if (squared >= best) continue;
            best = squared; closest = candidate; triangle = i / 3;
        }
    }

    sealed class Fixture : IDisposable
    {
        public readonly GameObject root, part;
        public readonly MeshFilter filter;
        public readonly MeshRenderer renderer;
        public readonly CatMeshContactSurface.Geometry geometry, replacement;
        readonly CatMeshContactSurface catalog;
        readonly object previous;
        public Fixture()
        {
            Assert.That(Bake, Is.Not.Null);
            previous = Loaded.GetValue(null);
            root = new GameObject("Transient BVH test root") { hideFlags = HideFlags.HideAndDontSave };
            part = new GameObject("Transient BVH test mesh") { hideFlags = HideFlags.HideAndDontSave };
            part.transform.SetParent(root.transform, false);
            filter = part.AddComponent<MeshFilter>(); renderer = part.AddComponent<MeshRenderer>();
            geometry = MakeGeometry(30, 0f); replacement = MakeGeometry(12, .38f);
            filter.sharedMesh = geometry.mesh;
            catalog = ScriptableObject.CreateInstance<CatMeshContactSurface>(); catalog.hideFlags = HideFlags.HideAndDontSave;
            Geometries.SetValue(catalog, new[] { geometry, replacement }); Loaded.SetValue(null, catalog);
        }
        public void Dispose()
        {
            Loaded.SetValue(null, previous);
            Object.DestroyImmediate(root); Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(geometry.mesh); Object.DestroyImmediate(replacement.mesh);
        }
        static CatMeshContactSurface.Geometry MakeGeometry(int size, float lift)
        {
            var vertices = new Vector3[(size + 1) * (size + 1)]; var triangles = new List<int>();
            for (int z = 0; z <= size; z++) for (int x = 0; x <= size; x++)
            {
                float xx = (float)x / size * 2 - 1, zz = (float)z / size * 2 - 1;
                vertices[z * (size + 1) + x] = new Vector3(xx, Mathf.Sin(xx * 4) * Mathf.Cos(zz * 3) * .13f + lift, zz);
                if (x == size || z == size) continue;
                int a = z * (size + 1) + x, b = a + 1, c = a + size + 1, d = c + 1;
                triangles.AddRange(new[] { a, b, c, b, d, c });
            }
            // Equal-distance duplicate + collapsed triangle exercise exact tie
            // ordering and the pre-existing degenerate-triangle kernel.
            triangles.AddRange(new[] { 0, 1, size + 1, 0, 0, 0 });
            var mesh = new Mesh { name = "Transient contact terrain", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = vertices; mesh.triangles = triangles.ToArray(); mesh.RecalculateBounds();
            var geometry = new CatMeshContactSurface.Geometry { mesh = mesh, vertices = vertices, triangles = triangles.ToArray() };
            Bake.Invoke(null, new object[] { geometry }); return geometry;
        }
    }
}
