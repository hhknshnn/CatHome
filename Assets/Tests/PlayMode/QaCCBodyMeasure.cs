#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

// Test-only metrology. No collider, asset, import setting or runtime profile is changed.
// All dimensions are metres in the actor's yaw-neutral frame at its actual scale.
internal static class QaCCBodyMeasure
{
    internal const string Header = "breed,clip,phase,group,vertices,bounds,controllerCentre,controllerRadius,controllerHeight,controllerAxisStart,controllerAxisEnd,maxPlanarRadiusAtCCCentre,maxLateralAtCCCentre,maxForwardAtCCCentre,maxBackwardAtCCCentre,maxExistingCCExcess,verticesOutsideExistingCC,furthestPlanarPoint,worstCCPoint";

    internal static void Controller(CharacterController cc, Transform actor, out Vector3 centre,
        out Vector3 a, out Vector3 b, out float radius, out float height)
    {
        Vector3 scale = actor.lossyScale;
        radius = cc.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        height = Mathf.Max(radius * 2f, cc.height * Mathf.Abs(scale.y));
        centre = Vector3.Scale(cc.center, scale);
        Vector3 rise = Vector3.up * (height * .5f - radius);
        a = centre - rise; b = centre + rise;
    }

    internal static IEnumerator Capture(CatMovement cat, CatBodyGuardCatalog.Probe[] probes,
        Mesh bakedMesh, string breedId, List<string> rows)
    {
        var animator = cat.GetComponentInChildren<Animator>();
        var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>();
        var indices = CatBreedCatalog.Load().Find(breedId).ContactVertexIndices;
        var cc = cat.GetComponent<CharacterController>();
        Controller(cc, cat.transform, out var centre, out var a, out var b, out float radius, out float height);
        var transforms = animator.GetComponentsInChildren<Transform>(true);
        var positions = transforms.Select(t => t.localPosition).ToArray();
        var rotations = transforms.Select(t => t.localRotation).ToArray();
        var scales = transforms.Select(t => t.localScale).ToArray();
        Action restore = () =>
        {
            for (int i = 0; i < transforms.Length; i++)
            { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
        };
        var all = NewGroups();
        try
        {
            var current = Measure(cat, skin, bakedMesh, indices, probes, centre, a, b, radius);
            Write(rows, breedId, "current", -1f, current, centre, a, b, radius, height);
            foreach (string kind in new[] { "Idle", "Walk", "Run" })
            {
                var clip = animator.runtimeAnimatorController.animationClips.First(c => c.name.EndsWith("|" + kind, StringComparison.Ordinal));
                var union = NewGroups();
                for (int phase = 0; phase < 32; phase++)
                {
                    restore();
                    clip.SampleAnimation(animator.gameObject, clip.length * phase / 32f);
                    var measured = Measure(cat, skin, bakedMesh, indices, probes, centre, a, b, radius);
                    Write(rows, breedId, kind, phase / 32f, measured, centre, a, b, radius, height);
                    for (int i = 0; i < measured.Length; i++) { union[i].Merge(measured[i]); all[i].Merge(measured[i]); }
                    // Restore before yielding; the game never observes a sampled source pose.
                    restore();
                    if (phase % 8 == 7) yield return null;
                }
                Write(rows, breedId, kind + "_union", -1f, union, centre, a, b, radius, height);
            }
            Write(rows, breedId, "IdleWalkRun_union", -1f, all, centre, a, b, radius, height);
        }
        finally { restore(); }
    }

    static Stats[] NewGroups() => new[] { new Stats(), new Stats(), new Stats(), new Stats() };

    static Stats[] Measure(CatMovement cat, SkinnedMeshRenderer skin, Mesh bakedMesh,
        IReadOnlyList<int> indices, CatBodyGuardCatalog.Probe[] probes, Vector3 centre,
        Vector3 a, Vector3 b, float radius)
    {
        skin.BakeMesh(bakedMesh, true);
        var vertices = bakedMesh.vertices;
        var frame = Matrix4x4.TRS(cat.transform.position, cat.transform.rotation, Vector3.one).inverse * skin.transform.localToWorldMatrix;
        var result = NewGroups();
        foreach (int index in indices)
        {
            Assert.That(index, Is.InRange(0, vertices.Length - 1));
            Vector3 p = frame.MultiplyPoint3x4(vertices[index]);
            bool covered = false;
            foreach (var probe in probes)
                if (SegmentDistance(p, probe.start, probe.end) <= probe.radius + .00001f) { covered = true; break; }
            result[0].Add(p, centre, a, b, radius);
            if (p.y <= centre.y) result[1].Add(p, centre, a, b, radius);
            if (!covered)
            {
                result[2].Add(p, centre, a, b, radius);
                if (p.y <= centre.y) result[3].Add(p, centre, a, b, radius);
            }
        }
        return result;
    }

    static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 line = b - a;
        return Vector3.Distance(p, a + line * (line.sqrMagnitude > 1e-12f ? Mathf.Clamp01(Vector3.Dot(p - a, line) / line.sqrMagnitude) : 0f));
    }

    sealed class Stats
    {
        internal int count, outside;
        internal Bounds bounds;
        internal float planar, lateral, forward, backward, excess;
        internal Vector3 planarPoint, excessPoint;
        internal void Add(Vector3 p, Vector3 centre, Vector3 a, Vector3 b, float radius)
        {
            if (count++ == 0) bounds = new Bounds(p, Vector3.zero); else bounds.Encapsulate(p);
            Vector3 delta = p - centre;
            float horizontal = new Vector2(delta.x, delta.z).magnitude;
            if (horizontal > planar) { planar = horizontal; planarPoint = p; }
            lateral = Mathf.Max(lateral, Mathf.Abs(delta.x));
            forward = Mathf.Max(forward, delta.z); backward = Mathf.Max(backward, -delta.z);
            float distance = SegmentDistance(p, a, b) - radius;
            if (distance > .00001f) outside++;
            if (distance > excess) { excess = distance; excessPoint = p; }
        }
        internal void Merge(Stats other)
        {
            if (other.count == 0) return;
            if (count == 0) bounds = other.bounds; else bounds.Encapsulate(other.bounds);
            count += other.count; outside += other.outside;
            if (other.planar > planar) { planar = other.planar; planarPoint = other.planarPoint; }
            if (other.excess > excess) { excess = other.excess; excessPoint = other.excessPoint; }
            lateral = Mathf.Max(lateral, other.lateral); forward = Mathf.Max(forward, other.forward); backward = Mathf.Max(backward, other.backward);
        }
    }

    static void Write(List<string> rows, string breed, string clip, float phase, Stats[] stats,
        Vector3 centre, Vector3 a, Vector3 b, float radius, float height)
    {
        string[] names = { "allBodyNoTail", "belowCCMid", "outsideFourBodyProbes", "outsideFourBodyProbesBelowCCMid" };
        for (int i = 0; i < stats.Length; i++)
        {
            var s = stats[i];
            rows.Add(Csv(breed, clip, phase, names[i], s.count, s.bounds, centre, radius, height, a, b,
                s.planar, s.lateral, s.forward, s.backward, s.excess, s.outside, s.planarPoint, s.excessPoint));
        }
    }

    static string Csv(params object[] values) => string.Join(",", values.Select(value =>
    {
        string text = value is Vector3 v ? FormattableString.Invariant($"{v.x:F6};{v.y:F6};{v.z:F6}") :
            value is Bounds b ? FormattableString.Invariant($"{b.min.x:F6};{b.min.y:F6};{b.min.z:F6}|{b.max.x:F6};{b.max.y:F6};{b.max.z:F6}") :
            value is IFormattable f ? f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : value?.ToString() ?? "";
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }));
}
#endif
