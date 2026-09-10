using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Softens surface lighting without reshaping the cat. Only normals and restrained
/// material settings change; silhouettes, skinning and contact vertices are exact copies.
/// </summary>
public static class CatModernVisualBuilder
{
    public const string Folder = "Assets/Art/Cat/Modern";
    public const string CatalogPath = "Assets/Resources/CatModernVisualCatalog.asset";
    private const float MinimumNormalDot = .25881905f; // Preserve creases sharper than 75 degrees.

    [Serializable]
    public sealed class MeshCheck
    {
        public string source, modern;
        public int vertices, triangles, bindposes, softenedVertices, protectedFaceVertices;
        public int headVertices, headSoftenedVertices;
        public bool positionsUnchanged, indicesUnchanged, weightsUnchanged, bindposesUnchanged;
        public bool uvsUnchanged, tangentsUnchanged, colorsUnchanged, blendShapesUnchanged, boundsUnchanged;
    }

    [Serializable]
    public sealed class ValidationReport
    {
        public int breeds, meshAssets, materialAssets, vertices, triangles, softenedVertices;
        public int protectedFaceVertices, headVertices, headSoftenedVertices;
        public bool valid;
        public List<MeshCheck> meshes = new List<MeshCheck>();
        public List<string> errors = new List<string>();
    }

    [MenuItem("Tools/Cat Home/Cat/Build Modern Surface Finish", false, 4092)]
    public static void BuildFromMenu() => Debug.Log(Build());

    public static string BuildPortraits() => CatModernPortraitBuilder.Build();

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build cat surface assets in Edit Mode.");
        var breeds = CatBreedCatalog.Load();
        if (breeds == null || breeds.Count != 10)
            throw new InvalidOperationException("The complete ten-breed catalog is required.");
        EnsureFolder(Folder + "/Meshes");
        EnsureFolder(Folder + "/Materials");
        EnsureFolder("Assets/Resources");
        var meshPairs = new List<CatModernVisualCatalog.MeshPair>();
        var materialPairs = new List<CatModernVisualCatalog.MaterialPair>();
        var seenMeshes = new HashSet<Mesh>();
        var seenMaterials = new HashSet<Material>();

        foreach (var breed in breeds.Entries)
        {
            if (breed.SourcePrefab == null) throw new InvalidOperationException("Missing cat: " + breed.Id);
            foreach (var skin in breed.SourcePrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var source = skin.sharedMesh;
                if (source == null) throw new InvalidOperationException("Missing skinned mesh: " + breed.Id);
                if (seenMeshes.Add(source))
                {
                    var mesh = Object.Instantiate(source);
                    try
                    {
                        mesh.name = "Modern_" + source.name;
                        var face = ProtectedFaceVertices(source, skin.bones);
                        var normals = SoftenNormals(source, face, out int softened);
                        mesh.normals = normals;
                        string path = Folder + "/Meshes/" + StableName(source) + ".asset";
                        var saved = SaveCopy(mesh, path);
                        meshPairs.Add(new CatModernVisualCatalog.MeshPair
                        {
                            source = source, modern = saved, softenedVertices = softened,
                            protectedFaceVertices = face.Count(v => v)
                        });
                    }
                    finally { if (mesh != null && !EditorUtility.IsPersistent(mesh)) Object.DestroyImmediate(mesh); }
                }
                foreach (var original in skin.sharedMaterials)
                {
                    if (original == null || !seenMaterials.Add(original)) continue;
                    var material = new Material(original) { name = "Modern_" + original.name };
                    try
                    {
                        bool skinMaterial = original.name.IndexOf("sphynx", StringComparison.OrdinalIgnoreCase) >= 0;
                        SetFloat(material, "_Smoothness", skinMaterial ? .27f : .18f);
                        SetFloat(material, "_Glossiness", skinMaterial ? .27f : .18f);
                        // The vendor AO atlas includes triangle-shaped shading;
                        // a strong contribution would reintroduce facets even
                        // after the lighting normals have been softened.
                        SetFloat(material, "_OcclusionStrength", skinMaterial ? .14f : .08f);
                        SetFloat(material, "_Metallic", 0f);
                        // Keep the original color/eye maps, emission color, shader,
                        // keywords, render queue and opaque surface settings.
                        var saved = SaveCopy(material, Folder + "/Materials/" + StableName(original) + ".mat");
                        materialPairs.Add(new CatModernVisualCatalog.MaterialPair { source = original, modern = saved });
                    }
                    finally { if (material != null && !EditorUtility.IsPersistent(material)) Object.DestroyImmediate(material); }
                }
            }
        }

        var catalog = AssetDatabase.LoadAssetAtPath<CatModernVisualCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CatModernVisualCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        catalog.EditorConfigure(meshPairs.ToArray(), materialPairs.ToArray());
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        CatSipMouthBuilder.Build();
        var report = Validate();
        if (!report.valid) throw new InvalidOperationException(JsonUtility.ToJson(report, true));
        return JsonUtility.ToJson(report, true);
    }

    public static ValidationReport Validate()
    {
        var report = new ValidationReport();
        var catalog = AssetDatabase.LoadAssetAtPath<CatModernVisualCatalog>(CatalogPath);
        var breeds = CatBreedCatalog.Load();
        if (catalog == null || breeds == null)
        { report.errors.Add("Modern or breed catalog is missing."); return report; }
        report.breeds = breeds.Count;
        report.meshAssets = catalog.Meshes.Count;
        report.materialAssets = catalog.Materials.Count;
        if (report.breeds != 10) report.errors.Add("Expected ten breeds.");
        if (report.meshAssets == 0 || report.materialAssets == 0) report.errors.Add("Modern assets are empty.");
        foreach (var pair in catalog.Meshes)
        {
            if (pair == null || pair.source == null || pair.modern == null)
            { report.errors.Add("Incomplete mesh mapping."); continue; }
            var check = CheckMesh(pair.source, pair.modern);
            check.softenedVertices = pair.softenedVertices;
            check.protectedFaceVertices = pair.protectedFaceVertices;
            var sourceSkin = breeds.Entries.SelectMany(b => b.SourcePrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                .First(s => s.sharedMesh == pair.source);
            CountHeadSmoothing(sourceSkin, pair.modern, check);
            report.meshes.Add(check);
            report.vertices += check.vertices;
            report.triangles += check.triangles;
            report.softenedVertices += check.softenedVertices;
            report.protectedFaceVertices += check.protectedFaceVertices;
            report.headVertices += check.headVertices;
            report.headSoftenedVertices += check.headSoftenedVertices;
            if (!check.positionsUnchanged || !check.indicesUnchanged || !check.weightsUnchanged ||
                !check.bindposesUnchanged || !check.uvsUnchanged || !check.tangentsUnchanged ||
                !check.colorsUnchanged || !check.blendShapesUnchanged || !check.boundsUnchanged)
                report.errors.Add("Geometry/skinning invariant failed: " + pair.source.name);
            if (pair.source == pair.modern || !AssetDatabase.GetAssetPath(pair.modern).StartsWith(Folder + "/", StringComparison.Ordinal))
                report.errors.Add("Mesh is not a project-owned copy: " + pair.source.name);
            var normals = pair.modern.normals;
            if (normals.Length != pair.modern.vertexCount || normals.Any(n =>
                float.IsNaN(n.x) || float.IsNaN(n.y) || float.IsNaN(n.z) ||
                float.IsInfinity(n.x) || float.IsInfinity(n.y) || float.IsInfinity(n.z) || n.sqrMagnitude < .5f))
                report.errors.Add("Invalid normals: " + pair.source.name);
        }
        foreach (var pair in catalog.Materials)
        {
            if (pair == null || pair.source == null || pair.modern == null)
            { report.errors.Add("Incomplete material mapping."); continue; }
            if (pair.source == pair.modern || !AssetDatabase.GetAssetPath(pair.modern).StartsWith(Folder + "/", StringComparison.Ordinal))
                report.errors.Add("Material is not a project-owned copy: " + pair.source.name);
            if (pair.source.shader != pair.modern.shader || pair.source.renderQueue != pair.modern.renderQueue ||
                !pair.source.shaderKeywords.OrderBy(k => k).SequenceEqual(pair.modern.shaderKeywords.OrderBy(k => k)))
                report.errors.Add("Shader contract changed: " + pair.source.name);
            foreach (string property in pair.source.GetTexturePropertyNames())
                if (pair.source.GetTexture(property) != pair.modern.GetTexture(property) ||
                    pair.source.GetTextureOffset(property) != pair.modern.GetTextureOffset(property) ||
                    pair.source.GetTextureScale(property) != pair.modern.GetTextureScale(property))
                    report.errors.Add("Texture mapping changed: " + pair.source.name + "/" + property);
            foreach (string property in new[] { "_BaseColor", "_Color", "_EmissionColor" })
                if (pair.source.HasProperty(property) && pair.source.GetColor(property) != pair.modern.GetColor(property))
                    report.errors.Add("Breed/eye color changed: " + pair.source.name + "/" + property);
        }
        foreach (var breed in breeds.Entries)
        foreach (var skin in breed.SourcePrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Mesh modern = catalog.Resolve(skin.sharedMesh);
            if (modern == skin.sharedMesh) { report.errors.Add("Unmapped breed mesh: " + breed.Id); continue; }
            bool[] face = ProtectedFaceVertices(skin.sharedMesh, skin.bones);
            var before = skin.sharedMesh.normals;
            var after = modern.normals;
            for (int i = 0; i < face.Length; i++)
                if (face[i] && !before[i].Equals(after[i]))
                { report.errors.Add("Facial surface normal changed: " + breed.Id + "/" + i); break; }
            foreach (var material in skin.sharedMaterials)
                if (material != null && catalog.Resolve(material) == material)
                    report.errors.Add("Unmapped breed material: " + breed.Id + "/" + material.name);
        }
        report.valid = report.errors.Count == 0;
        return report;
    }

    private static Vector3[] SoftenNormals(Mesh source, bool[] protectedFace, out int changed)
    {
        Vector3[] positions = source.vertices, normals = source.normals;
        if (normals.Length != positions.Length) throw new InvalidOperationException("Source normals missing: " + source.name);
        var weights = source.boneWeights;
        if (weights.Length != positions.Length) throw new InvalidOperationException("Expected four-weight cat skin: " + source.name);
        var result = (Vector3[])normals.Clone();
        var groups = new Dictionary<Vector3, List<int>>();
        var areas = new float[positions.Length];
        for (int sub = 0; sub < source.subMeshCount; sub++)
        {
            if (source.GetTopology(sub) != MeshTopology.Triangles)
                throw new InvalidOperationException("Expected triangle cat mesh: " + source.name);
            int[] indices = source.GetIndices(sub);
            for (int i = 0; i < indices.Length; i += 3)
            {
                int a = indices[i], b = indices[i + 1], c = indices[i + 2];
                float area = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]).magnitude;
                areas[a] += area; areas[b] += area; areas[c] += area;
            }
        }
        for (int i = 0; i < positions.Length; i++)
        {
            if (protectedFace[i]) continue;
            // Exact coincident positions only: no spatial welding, no vertex movement.
            if (!groups.TryGetValue(positions[i], out var group)) groups.Add(positions[i], group = new List<int>());
            group.Add(i);
        }
        changed = 0;
        foreach (var group in groups.Values)
        foreach (int index in group)
        {
            Vector3 sum = Vector3.zero;
            foreach (int other in group)
            {
                // UV islands may split every low-poly face. They are texture
                // coordinates, not surface creases: average normals across them
                // while preserving the UV values and all vertices exactly.
                // Distinct skin influences remain separate moving surfaces.
                if (!weights[index].Equals(weights[other]) ||
                    Vector3.Dot(normals[index].normalized, normals[other].normalized) < MinimumNormalDot) continue;
                sum += normals[other] * Mathf.Max(areas[other], .00000001f);
            }
            if (sum.sqrMagnitude < 1e-18f) continue;
            Vector3 softened = sum.normalized;
            if (Vector3.Angle(softened, normals[index]) <= .01f) continue;
            result[index] = softened;
            changed++;
        }
        return result;
    }

    private static bool[] ProtectedFaceVertices(Mesh mesh, Transform[] bones)
    {
        var protectedBones = new bool[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            string name = bones[i] != null ? bones[i].name.ToLowerInvariant() : string.Empty;
            // Eyes and mouth interiors are separate surfaces. Tiny jaw/eyelid
            // influences also extend over the cheeks and forehead, so freezing
            // every affected vertex preserves the entire faceted face. Keep
            // only the actual eyeball/tongue surfaces unchanged; distinct skin
            // influences and the crease limit preserve lip and lid edges.
            protectedBones[i] = (name.Contains("eye") && !name.Contains("eyelid")) || name.Contains("tongue");
        }
        var weights = mesh.boneWeights;
        var result = new bool[mesh.vertexCount];
        for (int i = 0; i < weights.Length; i++)
        {
            var w = weights[i];
            result[i] = ProtectedWeight(w.boneIndex0, w.weight0, protectedBones) ||
                ProtectedWeight(w.boneIndex1, w.weight1, protectedBones) ||
                ProtectedWeight(w.boneIndex2, w.weight2, protectedBones) ||
                ProtectedWeight(w.boneIndex3, w.weight3, protectedBones);
        }
        return result;
    }

    private static bool ProtectedWeight(int index, float weight, bool[] bones) =>
        weight >= .5f && index >= 0 && index < bones.Length && bones[index];

    private static void CountHeadSmoothing(SkinnedMeshRenderer source, Mesh modern, MeshCheck check)
    {
        Transform head = source.bones.FirstOrDefault(b => b != null && b.name == "DEF-spine.006");
        if (head == null) return;
        var headBones = source.bones.Select(b => b != null && (b == head || b.IsChildOf(head))).ToArray();
        var weights = source.sharedMesh.boneWeights;
        var before = source.sharedMesh.normals; var after = modern.normals;
        for (int i = 0; i < weights.Length; i++)
        {
            var w = weights[i];
            float headWeight = BoneWeight(w.boneIndex0, w.weight0, headBones) + BoneWeight(w.boneIndex1, w.weight1, headBones) +
                BoneWeight(w.boneIndex2, w.weight2, headBones) + BoneWeight(w.boneIndex3, w.weight3, headBones);
            if (headWeight < .5f) continue;
            check.headVertices++;
            if (Vector3.Angle(before[i], after[i]) > .01f) check.headSoftenedVertices++;
        }
    }

    private static float BoneWeight(int index, float weight, bool[] selected) =>
        index >= 0 && index < selected.Length && selected[index] ? weight : 0;

    private static MeshCheck CheckMesh(Mesh source, Mesh modern)
    {
        var check = new MeshCheck
        {
            source = AssetDatabase.GetAssetPath(source), modern = AssetDatabase.GetAssetPath(modern),
            vertices = source.vertexCount, bindposes = source.bindposes.Length,
            positionsUnchanged = source.vertices.SequenceEqual(modern.vertices),
            weightsUnchanged = source.boneWeights.SequenceEqual(modern.boneWeights),
            bindposesUnchanged = source.bindposes.SequenceEqual(modern.bindposes),
            tangentsUnchanged = source.tangents.SequenceEqual(modern.tangents),
            colorsUnchanged = source.colors.SequenceEqual(modern.colors),
            boundsUnchanged = source.bounds.Equals(modern.bounds),
            indicesUnchanged = source.subMeshCount == modern.subMeshCount && source.indexFormat == modern.indexFormat,
            uvsUnchanged = true, blendShapesUnchanged = SameBlendShapes(source, modern)
        };
        for (int sub = 0; sub < source.subMeshCount; sub++)
        {
            var indices = source.GetIndices(sub);
            check.triangles += indices.Length / 3;
            if (sub >= modern.subMeshCount || source.GetTopology(sub) != modern.GetTopology(sub) ||
                !indices.SequenceEqual(modern.GetIndices(sub)) || source.GetBaseVertex(sub) != modern.GetBaseVertex(sub))
                check.indicesUnchanged = false;
        }
        var sourceUvs = new List<Vector4>();
        var modernUvs = new List<Vector4>();
        for (int channel = 0; channel < 8; channel++)
        {
            source.GetUVs(channel, sourceUvs); modern.GetUVs(channel, modernUvs);
            if (!sourceUvs.SequenceEqual(modernUvs)) check.uvsUnchanged = false;
        }
        return check;
    }

    private static bool SameBlendShapes(Mesh source, Mesh modern)
    {
        if (source.blendShapeCount != modern.blendShapeCount || source.vertexCount != modern.vertexCount) return false;
        if (source.blendShapeCount == 0) return true;
        var ap = new Vector3[source.vertexCount]; var an = new Vector3[source.vertexCount]; var at = new Vector3[source.vertexCount];
        var bp = new Vector3[source.vertexCount]; var bn = new Vector3[source.vertexCount]; var bt = new Vector3[source.vertexCount];
        for (int shape = 0; shape < source.blendShapeCount; shape++)
        {
            if (source.GetBlendShapeName(shape) != modern.GetBlendShapeName(shape) ||
                source.GetBlendShapeFrameCount(shape) != modern.GetBlendShapeFrameCount(shape)) return false;
            for (int frame = 0; frame < source.GetBlendShapeFrameCount(shape); frame++)
            {
                if (source.GetBlendShapeFrameWeight(shape, frame) != modern.GetBlendShapeFrameWeight(shape, frame)) return false;
                source.GetBlendShapeFrameVertices(shape, frame, ap, an, at);
                modern.GetBlendShapeFrameVertices(shape, frame, bp, bn, bt);
                if (!ap.SequenceEqual(bp) || !an.SequenceEqual(bn) || !at.SequenceEqual(bt)) return false;
            }
        }
        return true;
    }

    private static T SaveCopy<T>(T fresh, string path) where T : Object
    {
        var saved = AssetDatabase.LoadAssetAtPath<T>(path);
        if (saved == null) { AssetDatabase.CreateAsset(fresh, path); saved = fresh; }
        else EditorUtility.CopySerialized(fresh, saved);
        EditorUtility.SetDirty(saved);
        return saved;
    }

    private static string StableName(Object source)
    {
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long fileId))
            throw new InvalidOperationException("Cat source must be a persistent asset: " + source.name);
        string name = new string(source.name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray());
        return name + "_" + guid.Substring(0, 8) + "_" + fileId.ToString().Replace("-", "m");
    }

    private static void SetFloat(Material material, string name, float value)
    { if (material.HasProperty(name)) material.SetFloat(name, value); }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
