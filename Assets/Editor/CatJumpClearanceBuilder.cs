using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Preview-scene measurement only; never modifies clips, models or room scenes.</summary>
public static class CatJumpClearanceBuilder
{
    const string Path = "Assets/Resources/Home/CatJumpClearanceCatalog.asset";
    static readonly string[] Names = { "pelvis", "chest", "neck", "head" };
    // Keep the existing fitting/source contract. Jump trunk coverage additionally
    // includes its real pelvis/belly deform bones; paw-reach profiles stay unchanged.
    static readonly MethodInfo LegacyRegion = typeof(CatPawReachBuilder).GetMethod("Region", BindingFlags.Static | BindingFlags.NonPublic);
    static readonly MethodInfo Fit = typeof(CatPawReachBuilder).GetMethod("FitBody", BindingFlags.Static | BindingFlags.NonPublic);
    static readonly MethodInfo Clip = typeof(CatPawReachBuilder).GetMethod("StateClip", BindingFlags.Static | BindingFlags.NonPublic);
    static readonly MethodInfo BindBones = typeof(CatPawReachBuilder).GetMethod("BindSourceBones", BindingFlags.Static | BindingFlags.NonPublic);
    static readonly MethodInfo BoneMatrices = typeof(CatPawReachBuilder).GetMethod("SourceMatrices", BindingFlags.Static | BindingFlags.NonPublic);
    public static string Measure() => Build(false);
    public static string Build(bool save = true) => BuildInternal(save, false, false, false);
    // Preview-only before/after measurement, including the installed catalog's
    // containment of the expanded vertex set. Never saves or changes a scene.
    public static string MeasureCoverage() => BuildInternal(false, true, false, false);
    public static string MeasureHeadCoverage() => BuildInternal(false, false, true, false);
    public static string MeasureBodyTriangleCoverage() => BuildInternal(false, false, false, true);
    static string BuildInternal(bool save, bool compareCoverage, bool compareHead, bool compareBody)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Measure jump clearance outside Play Mode.");
        if (LegacyRegion == null || Fit == null || Clip == null || BindBones == null || BoneMatrices == null) throw new InvalidOperationException("Measured source-pose fit contract changed.");
        var catalog = CatBreedCatalog.Load();
        var entries = new List<CatJumpClearanceCatalog.Entry>();
        var report = new StringBuilder(compareBody
            ? "breed,phase,region,triangles,parts,broadRadius_home,maxPartRadius_home,minTriangleContainment_home,installedTriangleContainment_home\n"
            : compareHead
            ? "breed,phase,triangles,parts,broadRadius_home,maxPartRadius_home,minTriangleContainment_home,installedTriangleContainment_home\n"
            : compareCoverage
            ? "breed,phase,region,legacyVertices,vertices,legacyRadius_home,radius_home,legacyStart,legacyEnd,start,end,minContainment_home,installedMinContainment_home\n"
            : "breed,phase,region,radius_home,start,end,vertices,minContainment_home\n");
        var phases = Enumerable.Range(0, 97).Select(i => i / 96f)
            .Concat(new[] { CatJumpMotion.Takeoff, CatJumpMotion.Touchdown })
            .Where(p => p <= CatJumpMotion.Takeoff || p >= CatJumpMotion.Touchdown).Distinct().OrderBy(p => p).ToArray();
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (var breed in catalog.Entries)
            {
                var visual = CatBreedVisualFactory.Create(breed, catalog.GameplayController, null, "Native jump envelope measurement");
                SceneManager.MoveGameObjectToScene(visual, scene); var baked = new Mesh();
                try
                {
                    var animator = visual.GetComponentInChildren<Animator>(); animator.enabled = false;
                    var skin = visual.GetComponentInChildren<SkinnedMeshRenderer>();
                    var source = (AnimationClip)Clip.Invoke(null, new object[] { catalog.GameplayController, "NativeJump" });
                    if (source == null) throw new InvalidOperationException("NativeJump clip missing for " + breed.Id);
                    var transforms = visual.GetComponentsInChildren<Transform>(true);
                    var positions = transforms.Select(t => t.localPosition).ToArray();
                    var rotations = transforms.Select(t => t.localRotation).ToArray();
                    var scales = transforms.Select(t => t.localScale).ToArray();
                    Vector3 basePosition = animator.transform.localPosition, baseScale = animator.transform.localScale;
                    Quaternion baseRotation = animator.transform.localRotation;
                    var weights = skin.sharedMesh.boneWeights; var bones = skin.bones;
                    var weightedBody = CatPawBodyCoverageBuilder.Build(skin,animator.transform,breed.ContactVertexIndices);
                    var weightedBones = (CatPawReachCatalog.SkinBone[])BindBones.Invoke(null,new object[]{animator.transform,weightedBody.Select(r=>r.skin).ToArray()});
                    var groups = new int[weights.Length]; for (int i = 0; i < groups.Length; i++) groups[i] = -1;
                    var legacyGroups = compareCoverage ? new int[weights.Length] : null;
                    foreach (int index in breed.ContactVertexIndices)
                    {
                        groups[index] = ClassifyBodyRegion(weights[index], bones);
                        if (compareCoverage) legacyGroups[index] = (int)LegacyRegion.Invoke(null, new object[] { weights[index], bones });
                    }
                    int[] triangles = skin.sharedMesh.triangles;
                    var regionTriangles = Enumerable.Range(0,4).Select(r => CatJumpSurfaceCoverageBuilder.RegionTriangles(triangles, groups, r)).ToArray();
                    var headTriangles = regionTriangles[3];
                    var installed = compareCoverage || compareHead || compareBody ? CatJumpClearanceCatalog.Load()?.Find(breed.Id) : null;
                    var result = new CatJumpClearanceCatalog.Entry { breedId = breed.Id, sourceClip = source,
                        samples = new CatJumpClearanceCatalog.Sample[phases.Length],
                        weightedBodyVersion=CatJumpClearanceCatalog.WeightedBodyVersion,bodySurface=weightedBody,bodyBones=weightedBones };
                    for (int sampleIndex = 0; sampleIndex < phases.Length; sampleIndex++)
                    {
                        for (int i = 0; i < transforms.Length; i++)
                        { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
                        float phase = phases[sampleIndex]; source.SampleAnimation(animator.gameObject, source.length * phase);
                        animator.transform.localPosition = basePosition; animator.transform.localRotation = baseRotation; animator.transform.localScale = baseScale;
                        skin.BakeMesh(baked, true); var vertices = baked.vertices;
                        var points = new Vector3[vertices.Length]; float minY = float.PositiveInfinity;
                        // Boundary triangles may include neck-side vertices outside
                        // the body contact mask. Their real coordinates must be baked too.
                        for (int i = 0; i < vertices.Length; i++)
                            points[i] = visual.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));
                        Bounds bounds = default; bool first = true;
                        foreach (int index in breed.ContactVertexIndices)
                        {
                            Vector3 point = visual.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[index]));
                            points[index] = point; minY = Mathf.Min(minY, point.y);
                            if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
                        }
                        if (first) throw new InvalidOperationException("Body skin mask is empty for " + breed.Id);
                        if (sampleIndex == 0) result.sourceZeroCentre = bounds.center;
                        var regionPoints = Names.Select(_ => new List<Vector3>()).ToArray();
                        foreach (int index in breed.ContactVertexIndices)
                            if (groups[index] >= 0) regionPoints[groups[index]].Add(points[index] - Vector3.up * minY);
                        var sample = new CatJumpClearanceCatalog.Sample { phase = phase, probes = new CatBodyGuardCatalog.Probe[4] };
                        for (int region = 0; region < 4; region++)
                        {
                            var values = regionPoints[region]; if (values.Count == 0) throw new InvalidOperationException(breed.Id + "/" + Names[region] + " has no samples.");
                            var p = (CatBodyGuardCatalog.Probe)Fit.Invoke(null, new object[] { Names[region], values }); sample.probes[region] = p;
                            Vector3 d = p.end - p.start; float minimum = float.PositiveInfinity;
                            foreach (var point in values)
                            {
                                float t = d.sqrMagnitude > 1e-12f ? Mathf.Clamp01(Vector3.Dot(point - p.start, d) / d.sqrMagnitude) : 0;
                                minimum = Mathf.Min(minimum, p.radius - Vector3.Distance(point, p.start + t * d));
                            }
                            if (minimum < .00399f) throw new InvalidOperationException(breed.Id + " native skin escaped " + Names[region]);
                            result.sampledVertices += values.Count;
                            if (compareCoverage)
                            {
                                var legacyValues = new List<Vector3>();
                                foreach (int vertex in breed.ContactVertexIndices)
                                    if (legacyGroups[vertex] == region) legacyValues.Add(points[vertex] - Vector3.up * minY);
                                var old = (CatBodyGuardCatalog.Probe)Fit.Invoke(null, new object[] { Names[region], legacyValues });
                                var stored = installed?.samples.FirstOrDefault(s => Mathf.Abs(s.phase - phase) < .00001f);
                                float installedMinimum = stored != null ? MinContainment(stored.probes[region], values) : float.NegativeInfinity;
                                report.AppendLine(string.Join(",", breed.Id, F(phase), Names[region], legacyValues.Count, values.Count,
                                    F(old.radius * .5f), F(p.radius * .5f), P(old.start * .5f), P(old.end * .5f),
                                    P(p.start * .5f), P(p.end * .5f), F(minimum * .5f), F(installedMinimum * .5f)));
                            }
                            else if (!compareHead && !compareBody) report.AppendLine(string.Join(",", breed.Id, F(phase), Names[region], F(p.radius * .5f), P(p.start * .5f), P(p.end * .5f), values.Count, F(minimum * .5f)));
                        }
                        var grounded = points.Select(p => p - Vector3.up * minY).ToArray();
                        sample.bodySkinMatrices=(Matrix4x4[])BoneMatrices.Invoke(null,new object[]{weightedBones,animator.transform,visual.transform,-Vector3.up*minY});
                        // Independent actual BakeMesh coordinates already exist above.
                        // Verify every weighted boundary corner before saving this catalog.
                        foreach(var region in weightedBody)
                        {
                            var frame=CatPawWeightedSkin.Build(weightedBones,sample.bodySkinMatrices,Matrix4x4.identity,region.skin,Vector3.zero);
                            if(frame==null)throw new InvalidOperationException("Invalid weighted jump bindings "+breed.Id);
                            var predicted=new Vector3[region.skin.Length];
                            CatPawWeightedSkin.Fill(frame,Vector3.zero,Quaternion.identity,false,true,default,default,default,predicted);
                            for(int v=0;v<predicted.Length;v++)
                                if(Vector3.Distance(predicted[v],grounded[region.skin[v].vertexIndex])>.0001f)
                                    throw new InvalidOperationException("Weighted NativeJump differs from actual source "+breed.Id+"/"+phase+"/"+region.region+"/"+v);
                        }
                        sample.bodyCoverageVersion = CatJumpClearanceCatalog.BodyCoverageVersion;
                        sample.bodyCoverage = new CatJumpClearanceCatalog.RegionCoverage[4];
                        var fitted = new CatJumpSurfaceCoverageBuilder.Result[4];
                        for (int r = 0; r < 4; r++)
                        {
                            fitted[r] = CatJumpSurfaceCoverageBuilder.Fit(grounded, regionTriangles[r], Names[r]);
                            sample.bodyCoverage[r] = new CatJumpClearanceCatalog.RegionCoverage {
                                region = Names[r], triangleCount = regionTriangles[r].Length / 3,
                                envelope = fitted[r].envelope, parts = fitted[r].parts };
                            if (compareBody)
                            {
                                var stored = installed?.samples.FirstOrDefault(s => Mathf.Abs(s.phase - phase) < .00001f);
                                var region = stored != null && stored.bodyCoverageVersion == CatJumpClearanceCatalog.BodyCoverageVersion &&
                                    stored.bodyCoverage != null && stored.bodyCoverage.Length == 4 ? stored.bodyCoverage[r] : null;
                                float minimum = region != null && region.region == Names[r] ? Mathf.Min(
                                    CatJumpSurfaceCoverageBuilder.TriangleContainment(grounded, regionTriangles[r], region.parts),
                                    CatJumpSurfaceCoverageBuilder.TriangleContainment(grounded, regionTriangles[r], new[] { region.envelope })) : float.NegativeInfinity;
                                report.AppendLine(string.Join(",", breed.Id, F(phase), Names[r], regionTriangles[r].Length / 3, fitted[r].parts.Length,
                                    F(fitted[r].envelope.radius * .5f), F(fitted[r].parts.Max(p => p.radius) * .5f),
                                    F(fitted[r].minimumContainment * .5f), F(minimum * .5f)));
                            }
                        }
                        // Preserve the already installed head diagnostic contract.
                        var refinement = fitted[3];
                        sample.headCoverageVersion = CatJumpClearanceCatalog.HeadCoverageVersion;
                        sample.headTriangleCount = headTriangles.Length / 3;
                        sample.headEnvelope = refinement.envelope;
                        sample.headProbes = refinement.parts;
                        if (compareHead)
                        {
                            var stored = installed?.samples.FirstOrDefault(s => Mathf.Abs(s.phase - phase) < .00001f);
                            float storedContainment = stored != null && stored.headCoverageVersion == CatJumpClearanceCatalog.HeadCoverageVersion
                                ? CatJumpHeadCoverageBuilder.TriangleContainment(grounded, headTriangles, stored.headProbes)
                                : float.NegativeInfinity;
                            if (stored != null && stored.headCoverageVersion == CatJumpClearanceCatalog.HeadCoverageVersion)
                                storedContainment = Mathf.Min(storedContainment,
                                    CatJumpHeadCoverageBuilder.TriangleContainment(grounded, headTriangles, new[] { stored.headEnvelope }));
                            report.AppendLine(string.Join(",", breed.Id, F(phase), sample.headTriangleCount, sample.headProbes.Length,
                                F(sample.headEnvelope.radius * .5f), F(sample.headProbes.Max(p => p.radius) * .5f),
                                F(refinement.minimumContainment * .5f), F(storedContainment * .5f)));
                        }
                        result.samples[sampleIndex] = sample;
                    }
                    // Most discriminating samples first; all remaining sampled
                    // preparations/recoveries must still pass the same query.
                    result.samples = result.samples.OrderBy(s => Mathf.Abs(s.phase - CatJumpMotion.Touchdown) < .00001f ? 0 :
                        Mathf.Abs(s.phase - CatJumpMotion.Takeoff) < .00001f ? 1 : 2).ThenBy(s => s.phase).ToArray();
                    entries.Add(result);
                }
                finally { Object.DestroyImmediate(baked); Object.DestroyImmediate(visual); }
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        if (save)
        {
            var asset = AssetDatabase.LoadAssetAtPath<CatJumpClearanceCatalog>(Path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<CatJumpClearanceCatalog>(); AssetDatabase.CreateAsset(asset, Path); }
            asset.EditorConfigure(entries.ToArray()); EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
        }
        return report.ToString();
    }
    // Preserve every previously assigned vertex's region. Only skin omitted by
    // the older classifier can enter through these four measured deform bones.
    // Never walk pelvis descendants: doing so would classify thighs/feet as body.
    public static int ClassifyBodyRegion(BoneWeight weight, Transform[] bones)
    {
        int previous = (int)LegacyRegion.Invoke(null, new object[] { weight, bones });
        if (previous >= 0) return previous;
        var totals = new float[4];
        AddTrunkWeight(totals, bones, weight.boneIndex0, weight.weight0);
        AddTrunkWeight(totals, bones, weight.boneIndex1, weight.weight1);
        AddTrunkWeight(totals, bones, weight.boneIndex2, weight.weight2);
        AddTrunkWeight(totals, bones, weight.boneIndex3, weight.weight3);
        if (totals.Sum() < .5f) return -1;
        int best = 0;
        for (int i = 1; i < totals.Length; i++) if (totals[i] > totals[best]) best = i;
        return best;
    }
    static void AddTrunkWeight(float[] totals, Transform[] bones, int index, float weight)
    {
        if (weight <= 0f || index < 0 || index >= bones.Length || bones[index] == null) return;
        var bone = bones[index]; string name = bone.name;
        int region = name == "DEF-spine" || name == "DEF-pelvis.C" || name == "DEF-pelvis.L" || name == "DEF-pelvis.R" ? 0 :
            name == "DEF-spine.001" || name == "DEF-spine.002" || name == "DEF-spine.003" || name == "DEF-belly.C" ? 1 :
            name == "DEF-spine.004" || name == "DEF-spine.005" ? 2 : -1;
        if (region < 0)
            for (var parent = bone; parent != null; parent = parent.parent)
                if (parent.name == "DEF-spine.006") { region = 3; break; }
        if (region >= 0) totals[region] += weight;
    }
    static float MinContainment(CatBodyGuardCatalog.Probe probe, List<Vector3> points)
    {
        Vector3 axis = probe.end - probe.start; float minimum = float.PositiveInfinity;
        foreach (var point in points)
        {
            float t = axis.sqrMagnitude > 1e-12f ? Mathf.Clamp01(Vector3.Dot(point - probe.start, axis) / axis.sqrMagnitude) : 0f;
            minimum = Mathf.Min(minimum, probe.radius - Vector3.Distance(point, probe.start + t * axis));
        }
        return minimum;
    }
    static string F(float value) => value.ToString("F6", CultureInfo.InvariantCulture);
    static string P(Vector3 value) => F(value.x) + ";" + F(value.y) + ";" + F(value.z);
}
