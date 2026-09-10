using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Bakes only a home stride calibration; does not edit clips, controllers or scenes.</summary>
public static class CatHomeLocomotionBuilder
{
    public const string AssetPath = "Assets/Resources/Home/CatHomeLocomotionCatalog.asset";
    public const string ReportPath = "Docs/QA/HOME_LOCOMOTION_2026-09-09/stride-measurements.csv";
    public static readonly string[] PawNames = { "DEF-hand.L", "DEF-hand.R", "DEF-foot.L", "DEF-foot.R" };

    public sealed class Measurement
    {
        public float cycleDistance, nominalSpeed, relativeMedianResidual;
        public int contactSamples;
    }

    [MenuItem("Tools/Cat Home/Cat/Measure Home Locomotion")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Measure home locomotion outside Play Mode.");
        var breeds = CatBreedCatalog.Load();
        if (breeds == null) throw new InvalidOperationException("Cat breed catalog is missing.");
        var entries = new List<CatHomeLocomotionCatalog.Entry>();
        var report = new StringBuilder("breed,clip,seconds,unit_scale_stride_m,home_half_scale_stride_m,home_nominal_mps,contact_samples,median_relative_residual\n");
        foreach (var breed in breeds.Entries)
        {
            var visual = CatBreedVisualFactory.Create(breed, breeds.GameplayController, null, "Home stride measurement");
            try
            {
                var animator = visual.GetComponentInChildren<Animator>();
                animator.enabled = false;
                var walk = FindClip(animator, "Walk");
                var run = FindClip(animator, "Run");
                var walkMeasurement = Measure(animator, visual.transform, walk);
                var runMeasurement = Measure(animator, visual.transform, run);
                entries.Add(new CatHomeLocomotionCatalog.Entry {
                    breedId = breed.Id, walkClip = walk, runClip = run,
                    walkCycleDistance = walkMeasurement.cycleDistance,
                    runCycleDistance = runMeasurement.cycleDistance
                });
                Append(report, breed.Id, walk, walkMeasurement);
                Append(report, breed.Id, run, runMeasurement);
            }
            finally { Object.DestroyImmediate(visual); }
        }
        var catalog = AssetDatabase.LoadAssetAtPath<CatHomeLocomotionCatalog>(AssetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CatHomeLocomotionCatalog>();
            AssetDatabase.CreateAsset(catalog, AssetPath);
        }
        catalog.EditorConfigure(entries.ToArray());
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, report.ToString());
        Debug.Log("[Home locomotion] Measured " + entries.Count + " breeds; " + ReportPath);
    }

    public static AnimationClip FindClip(Animator animator, string kind) =>
        animator.runtimeAnimatorController.animationClips.First(c => c.name.EndsWith("|" + kind, StringComparison.Ordinal));

    public static Measurement Measure(Animator animator, Transform owner, AnimationClip clip, int samplesPerSecond = 240)
    {
        int count = Mathf.Max(32, Mathf.CeilToInt(clip.length * samplesPerSecond));
        float step = clip.length / count;
        var paws = PawNames.Select(n => CatBreedVisualFactory.FindDescendant(animator.transform, n)).ToArray();
        if (paws.Any(p => p == null)) throw new InvalidOperationException("Missing home locomotion paw.");
        var positions = new Vector3[paws.Length, count + 1];
        var minY = Enumerable.Repeat(float.PositiveInfinity, paws.Length).ToArray();
        var maxY = Enumerable.Repeat(float.NegativeInfinity, paws.Length).ToArray();
        for (int frame = 0; frame <= count; frame++)
        {
            clip.SampleAnimation(animator.gameObject, frame * step);
            for (int p = 0; p < paws.Length; p++)
            {
                var point = owner.InverseTransformPoint(paws[p].position);
                positions[p, frame] = point;
                minY[p] = Mathf.Min(minY[p], point.y); maxY[p] = Mathf.Max(maxY[p], point.y);
            }
        }
        var speeds = new List<float>();
        for (int p = 0; p < paws.Length; p++)
        for (int frame = 1; frame < count; frame++)
        {
            // The middle of stance gives translation evidence. The airborne
            // return and touchdown/lift-off extremes must not determine stride.
            float height = (positions[p, frame].y - minY[p]) / Mathf.Max(.001f, maxY[p] - minY[p]);
            float backwards = -(positions[p, frame + 1].z - positions[p, frame - 1].z) / (2f * step);
            if (height <= .3f && backwards > .02f) speeds.Add(backwards);
        }
        if (speeds.Count < 12) throw new InvalidOperationException(clip.name + " has insufficient planted-paw samples.");
        speeds.Sort();
        float speed = Median(speeds);
        var residuals = speeds.Select(v => Mathf.Abs(v - speed) / speed).OrderBy(v => v).ToList();
        return new Measurement { nominalSpeed = speed, cycleDistance = speed * clip.length,
            contactSamples = speeds.Count, relativeMedianResidual = Median(residuals) };
    }

    static float Median(List<float> values) => (values[(values.Count - 1) / 2] + values[values.Count / 2]) * .5f;

    static void Append(StringBuilder report, string breed, AnimationClip clip, Measurement value)
    {
        report.AppendLine(string.Join(",", breed, clip.name, F(clip.length), F(value.cycleDistance),
            F(value.cycleDistance * .5f), F(value.nominalSpeed * .5f), value.contactSamples, F(value.relativeMedianResidual)));
    }
    static string F(float value) => value.ToString("F6", CultureInfo.InvariantCulture);
}
