#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Editor QA metrology only. This class never creates/updates a Unity asset.
public static class QaCareShapeTrialBuilder
{
    [Serializable] public sealed class Report { public Entry[] entries; public string coordinateSpace = "CatBreedVisualTag local; home scale .5"; }
    [Serializable] public sealed class Entry
    {
        public string breed;
        public Vector3 neckMin, neckMax, neckCentre, neckSize, torsoMin, torsoMax;
        public float paddingSource, minimumNeckClearanceAtHome, torsoMaxLateralAtHome, coreRadiusAtHome;
        public int poses, neckVerticesPerPose, sampledNeckVertices, sampledTorsoVertices;
    }
    public static string MeasureTo(string outputPath)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Measure outside Play Mode.");
        string project = Directory.GetParent(Application.dataPath).FullName;
        string qa = Path.GetFullPath(Path.Combine(project, "Docs", "QA")) + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(outputPath);
        if (!path.StartsWith(qa, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("QA report path required.");
        var breeds = CatBreedCatalog.Load();
        var region = typeof(CatBodyGuardBuilder).GetMethod("Region", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(BoneWeight), typeof(Transform[]) }, null);
        var names = typeof(CatBodyGuardBuilder).GetField("Regions", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string[];
        if (region == null || region.ReturnType != typeof(int) || names == null ||
            !names.SequenceEqual(new[] { "pelvis", "chest", "neck", "head" }))
            throw new InvalidOperationException("Expected current source-vertex grouping; do not reinterpret an unfamiliar builder.");
        var entries = new List<Entry>(); var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (var breed in breeds.Entries)
            {
                var visual = CatBreedVisualFactory.Create(breed, breeds.GameplayController, null, "QA exact source care bounds");
                SceneManager.MoveGameObjectToScene(visual, preview); var mesh = new Mesh();
                try
                {
                    var animator = visual.GetComponentInChildren<Animator>(); animator.enabled = false;
                    var skin = visual.GetComponentInChildren<SkinnedMeshRenderer>();
                    var weights = skin.sharedMesh.boneWeights; var bones = skin.bones;
                    var neckIndices = new List<int>(); var torsoIndices = new List<int>();
                    foreach (int index in breed.ContactVertexIndices)
                    {
                        if (index < 0 || index >= weights.Length) continue;
                        int group = (int)region.Invoke(null, new object[] { weights[index], bones });
                        if (group == 2) neckIndices.Add(index);
                        if (group == 0 || group == 1) torsoIndices.Add(index);
                    }
                    if (neckIndices.Count == 0 || torsoIndices.Count == 0) throw new InvalidOperationException("Missing assigned source vertices: " + breed.Id);
                    var neck = new List<Vector3>(neckIndices.Count * 96); var torso = new List<Vector3>(torsoIndices.Count * 96);
                    int poses = 0;
                    foreach (string kind in new[] { "Idle", "Walk", "Run" })
                    {
                        var clip = CatHomeLocomotionBuilder.FindClip(animator, kind);
                        for (int phase = 0; phase < 32; phase++)
                        {
                            clip.SampleAnimation(animator.gameObject, clip.length * phase / 32f);
                            skin.BakeMesh(mesh, true); var vertices = mesh.vertices;
                            foreach (int index in neckIndices) neck.Add(visual.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[index])));
                            foreach (int index in torsoIndices) torso.Add(visual.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[index])));
                            poses++;
                        }
                    }
                    Bounds neckBounds = BoundsOf(neck), torsoBounds = BoundsOf(torso);
                    const float sourcePadding = .01f; // exactly5mm at gameplay half scale
                    Bounds padded = neckBounds; padded.Expand(sourcePadding * 2f);
                    float minimum = float.PositiveInfinity;
                    foreach (Vector3 point in neck)
                    {
                        Vector3 edge = padded.extents - Abs(point - padded.center);
                        minimum = Mathf.Min(minimum, edge.x, edge.y, edge.z);
                        if (edge.x < sourcePadding - .00001f || edge.y < sourcePadding - .00001f || edge.z < sourcePadding - .00001f)
                            throw new InvalidOperationException("A trial box lost assigned neck skin/padding: " + breed.Id);
                    }
                    if (poses != 96 || neck.Count != neckIndices.Count * 96) throw new InvalidOperationException("Incomplete source coverage.");
                    float lateral = Mathf.Max(Mathf.Abs(torsoBounds.min.x), Mathf.Abs(torsoBounds.max.x)) * .5f;
                    entries.Add(new Entry { breed = breed.Id, neckMin = neckBounds.min, neckMax = neckBounds.max,
                        neckCentre = padded.center, neckSize = padded.size, torsoMin = torsoBounds.min, torsoMax = torsoBounds.max,
                        paddingSource = sourcePadding, minimumNeckClearanceAtHome = minimum * .5f,
                        torsoMaxLateralAtHome = lateral, coreRadiusAtHome = lateral + .01f,
                        poses = poses, neckVerticesPerPose = neckIndices.Count, sampledNeckVertices = neck.Count, sampledTorsoVertices = torso.Count });
                }
                finally { Object.DestroyImmediate(mesh); Object.DestroyImmediate(visual); }
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        string json = JsonUtility.ToJson(new Report { entries = entries.ToArray() }, true);
        Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, json);
        return path + " — " + entries.Count + " breeds, 96 poses each, actual source AABB plus5mm neck padding; no asset writes.";
    }
    static Bounds BoundsOf(List<Vector3> points) { var b = new Bounds(points[0], Vector3.zero); foreach (var p in points) b.Encapsulate(p); return b; }
    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
}
#endif
