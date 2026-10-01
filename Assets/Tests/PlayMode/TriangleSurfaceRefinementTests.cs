using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Production guard queries against synthetic geometry, in the actor's
/// real scene. No actor, bone, animation, collider profile or threshold edits.</summary>
public sealed class TriangleSurfaceRefinementTests
{
    const string Output = "Docs/QA/INTERACTION_POLISH_2026-09-16/triangle-surface-tests";
    readonly List<GameObject> objects = new List<GameObject>();
    readonly List<string> rows = new List<string>();
    readonly CatBodyGuardBox[] coarse = new CatBodyGuardBox[1];
    HomeStoreSaveState saved;
    CatMovement cat;
    Vector3 origin, a, b, c;
    Func<int, Collider, bool> refine;

    [UnitySetUp] public IEnumerator Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True);
        saved = HomeStoreService.CaptureState();
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        cat = Object.FindAnyObjectByType<CatMovement>();
        Assert.That(cat, Is.Not.Null);
        yield return QaBreedReadiness.WaitForSelected(cat, CatBreedService.SelectedBreedId);
        origin = new Vector3(30, 10, 30);
        a = origin + new Vector3(-.2f, 0, -.2f);
        b = origin + new Vector3(.2f, 0, -.2f);
        c = origin + new Vector3(-.2f, 0, .2f);
        coarse[0] = new CatBodyGuardBox { centre = origin, halfExtents = new Vector3(.201f, .001f, .201f), rotation = Quaternion.identity };
        refine = (index, solid) => cat.IsInteractionTriangleClear(a, b, c, solid, .002f);
        Physics.SyncTransforms();
        Assert.That(cat.IsInteractionBoxesClear(coarse, .002f), Is.True, "An empty query also initializes the enabled remote primitive.");
        rows.Clear(); rows.Add("case,directTriangleClear,coarseClear,refinedClear,queryMilliseconds");
    }

    [UnityTearDown] public IEnumerator After()
    {
        foreach (var item in objects) if (item != null) Object.Destroy(item);
        objects.Clear();
        RoomPlayModeSupport.ReleaseRoom();
        if (saved != null) HomeStoreService.ApplySavedState(saved);
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output, TestContext.CurrentContext.Test.Name + ".csv"), rows);
        yield return RoomPlayModeSupport.WaitForPendingContactData();
    }

    Collider Obstacle(PrimitiveType type, Vector3 at, float size, bool closedMesh = false)
    {
        var item = GameObject.CreatePrimitive(type);
        item.name = "QA triangle " + type + (closedMesh ? " closed nonconvex" : "");
        SceneManager.MoveGameObjectToScene(item, cat.gameObject.scene);
        objects.Add(item);
        item.transform.SetPositionAndRotation(at, Quaternion.identity);
        item.transform.localScale = Vector3.one * size;
        item.GetComponent<Renderer>().enabled = false;
        var solid = item.GetComponent<Collider>();
        if (closedMesh)
        {
            solid.enabled = false;
            var mesh = item.AddComponent<MeshCollider>();
            mesh.sharedMesh = item.GetComponent<MeshFilter>().sharedMesh;
            mesh.convex = false; solid = mesh;
        }
        Physics.SyncTransforms();
        return solid;
    }

    (bool direct, bool broad, bool refined) Observe(string label, Collider solid)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        bool direct = cat.IsInteractionTriangleClear(a, b, c, solid, .002f);
        bool broad = cat.IsInteractionBoxesClear(coarse, .002f);
        bool result = cat.IsInteractionBoxesClear(coarse, .002f, refine);
        rows.Add(FormattableString.Invariant($"{label},{direct},{broad},{result},{clock.Elapsed.TotalMilliseconds:F6}"));
        return (direct, broad, result);
    }

    [UnityTest, Timeout(60000)]
    public IEnumerator EmptyBoxCornerRefinesClear_RealSolidsAndClosedMeshRemainBlocked()
    {
        // The triangle is x+z <= 0 in its local plane. This sphere is wholly
        // inside the opposite empty corner of its enclosing rectangle.
        var corner = Obstacle(PrimitiveType.Sphere, origin + new Vector3(.14f, 0, .14f), .06f);
        var state = new PoseSnapshot(cat.transform);
        var empty = Observe("emptyCorner", corner);
        Assert.That(empty.broad, Is.False, "Calibration: coarse box must overlap the empty-corner obstacle.");
        Assert.That(empty.direct && empty.refined, Is.True, "Full triangle refinement should remove only empty box corners.");
        corner.enabled = false;

        foreach (var type in new[] { PrimitiveType.Cube, PrimitiveType.Sphere })
        {
            // The interior contact is away from all three triangle vertices.
            var solid = Obstacle(type, origin + new Vector3(-.06f, 0, -.06f), .06f);
            var contact = Observe("solid" + type, solid);
            Assert.That(contact.direct || contact.refined, Is.False, type + " cutting the full face must be rejected.");
            solid.enabled = false;
        }

        var enclosing = Obstacle(PrimitiveType.Cube, origin, 1f, true);
        var inside = Observe("entireTriangleInsideClosedMesh", enclosing);
        Assert.That(inside.direct, Is.False, "Closed-mesh occupied volume is not empty space when primitive MTD is absent.");
        Assert.That(inside.refined, Is.False, "Production coarse/refine chain must also run the occupied-volume check.");
        state.AssertUnchanged();
        yield return null;
    }

    [UnityTest, Timeout(60000)]
    public IEnumerator WarmQueriesRecheckMovedObstacles_AndNeverMutateActorOrBones()
    {
        var solid = Obstacle(PrimitiveType.Cube, origin + Vector3.up, .6f, true);
        var state = new PoseSnapshot(cat.transform);
        bool previousBackfaces = Physics.queriesHitBackfaces;
        var clear = Observe("warmOutside", solid);
        Assert.That(clear.direct && clear.refined, Is.True);
        solid.transform.position = origin; Physics.SyncTransforms();
        var blocked = Observe("movedInside", solid);
        Assert.That(blocked.direct || blocked.refined, Is.False, "Cached clear geometry is never cached physics permission.");
        solid.transform.position = origin + Vector3.up; Physics.SyncTransforms();
        var recovered = Observe("movedOutsideAgain", solid);
        Assert.That(recovered.direct && recovered.refined, Is.True, "A prior blocked query must not remain cached after motion.");

        // Exercise the normal overlap path too, using one reused callback.
        solid.enabled = false;
        var corner = Obstacle(PrimitiveType.Sphere, origin + new Vector3(.14f, 0, .14f), .06f);
        Assert.That(Observe("warmCorner", corner).refined, Is.True);
        corner.transform.position = origin + new Vector3(-.06f, 0, -.06f); Physics.SyncTransforms();
        Assert.That(Observe("cornerMovedIntoFace", corner).refined, Is.False);
        corner.transform.position = origin + new Vector3(.14f, 0, .14f); Physics.SyncTransforms();
        Assert.That(Observe("cornerMovedBackOut", corner).refined, Is.True);
        Assert.That(Physics.queriesHitBackfaces, Is.EqualTo(previousBackfaces));
        state.AssertUnchanged();
        yield return null;
    }

    sealed class PoseSnapshot
    {
        readonly Transform[] transforms;
        readonly Vector3[] positions, scales;
        readonly Quaternion[] rotations;
        readonly Vector3 rootPosition;
        readonly Quaternion rootRotation;
        public PoseSnapshot(Transform root)
        {
            transforms = root.GetComponentsInChildren<Transform>(true);
            positions = new Vector3[transforms.Length]; scales = new Vector3[transforms.Length]; rotations = new Quaternion[transforms.Length];
            rootPosition = root.position; rootRotation = root.rotation;
            for (int i = 0; i < transforms.Length; i++)
            { positions[i] = transforms[i].localPosition; scales[i] = transforms[i].localScale; rotations[i] = transforms[i].localRotation; }
        }
        public void AssertUnchanged()
        {
            Assert.That(transforms[0].position, Is.EqualTo(rootPosition));
            Assert.That(transforms[0].rotation, Is.EqualTo(rootRotation));
            for (int i = 0; i < transforms.Length; i++)
            {
                Assert.That(transforms[i].localPosition, Is.EqualTo(positions[i]), transforms[i].name + " position");
                Assert.That(transforms[i].localRotation, Is.EqualTo(rotations[i]), transforms[i].name + " rotation");
                Assert.That(transforms[i].localScale, Is.EqualTo(scales[i]), transforms[i].name + " scale");
            }
        }
    }
}
