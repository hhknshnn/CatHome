using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Two measured, transparent photographs per finished cat; only existing portrait bindings are updated.</summary>
public static class CatModernPortraitBuilder
{
    public const string Folder = CatModernVisualBuilder.Folder + "/Portraits";
    public const int Size = 512;
    private const int PreviewLayer = 31;

    [Serializable]
    public sealed class Report
    {
        public int breeds, images, catalogBindings, prefabBindings, sceneBindings;
        public bool valid;
        public List<string> paths = new List<string>();
        public List<string> poseSamples = new List<string>();
        public List<string> errors = new List<string>();
    }

    [MenuItem("Tools/Cat Home/Cat/Capture Modern Breed Portraits", false, 4093)]
    public static void BuildFromMenu() => Debug.Log(Build());

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Capture modern portraits in Edit Mode.");
        var catalog = CatBreedCatalog.Load();
        var finish = CatModernVisualCatalog.Load();
        if (catalog == null || catalog.Count != 10 || finish == null || finish.Meshes.Count == 0)
            throw new InvalidOperationException("Build the complete modern cat surface catalog first.");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(CatModernVisualBuilder.Folder, "Portraits");
        var report = new Report();
        var pairs = new List<CatModernVisualCatalog.PortraitPair>();
        var originalSprites = new Dictionary<Sprite, Sprite>();
        var unchangedController = catalog.GameplayController;
        var unchangedIds = catalog.Entries.Select(e => e.Id).ToArray();
        var unchangedPrefabs = catalog.Entries.Select(e => e.SourcePrefab).ToArray();
        var unchangedContacts = catalog.Entries.Select(e => e.ContactVertexIndices.ToArray()).ToArray();
        AnimationClip pose = catalog.GameplayController.animationClips.FirstOrDefault(c => c.name.EndsWith("|Sitting", StringComparison.Ordinal));
        if (pose == null) throw new InvalidOperationException("The real sitting clip is missing.");

        foreach (var breed in catalog.Entries)
        {
            string identityPath = Folder + "/" + breed.Id + "_Identity.png";
            string bodyPath = Folder + "/" + breed.Id + "_FullBody.png";
            float sample = Capture(breed, catalog, pose, identityPath, true);
            Capture(breed, catalog, pose, bodyPath, false);
            report.poseSamples.Add(breed.Id + ": Sitting at " + sample.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + " seconds, breed-authored neutral jaw and eyelid transforms");
            Import(identityPath); Import(bodyPath);
            var previous = finish.Portraits.FirstOrDefault(p => p.breedId == breed.Id);
            string oldPath = breed.Portrait != null ? AssetDatabase.GetAssetPath(breed.Portrait) : string.Empty;
            string originalGuid = previous != null && !string.IsNullOrEmpty(previous.originalGuid)
                ? previous.originalGuid : AssetDatabase.AssetPathToGUID(oldPath);
            var pair = new CatModernVisualCatalog.PortraitPair
            {
                breedId = breed.Id, originalGuid = originalGuid,
                identity = AssetDatabase.LoadAssetAtPath<Sprite>(identityPath),
                fullBody = AssetDatabase.LoadAssetAtPath<Sprite>(bodyPath)
            };
            if (pair.identity == null || pair.fullBody == null) throw new InvalidOperationException("Portrait import failed: " + breed.Id);
            if (breed.Portrait != null) originalSprites[breed.Portrait] = pair.identity;
            var vendor = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(originalGuid));
            if (vendor != null) originalSprites[vendor] = pair.identity;
            pairs.Add(pair);
            report.paths.Add(identityPath); report.paths.Add(bodyPath);
        }

        // Updating only the serialized portrait field preserves baked contact
        // indices and leaves the controller, prefab and breed identities intact.
        var data = new SerializedObject(catalog);
        var entries = data.FindProperty("entries");
        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            var pair = pairs.First(p => p.breedId == entry.FindPropertyRelative("id").stringValue);
            entry.FindPropertyRelative("portrait").objectReferenceValue = pair.identity;
            report.catalogBindings++;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        finish.EditorConfigurePortraits(pairs.ToArray());
        EditorUtility.SetDirty(catalog); EditorUtility.SetDirty(finish);
        AssetDatabase.SaveAssets();

        if (catalog.GameplayController != unchangedController || !catalog.Entries.Select(e => e.Id).SequenceEqual(unchangedIds) ||
            !catalog.Entries.Select(e => e.SourcePrefab).SequenceEqual(unchangedPrefabs) ||
            catalog.Entries.Where((e, i) => !e.ContactVertexIndices.SequenceEqual(unchangedContacts[i])).Any())
            throw new InvalidOperationException("Portrait capture changed a breed gameplay binding.");

        var byBreed = pairs.ToDictionary(p => p.breedId, StringComparer.Ordinal);
        var candidateGuids = pairs.Select(p => p.originalGuid).Concat(pairs.Select(p => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p.identity))))
            .Where(g => !string.IsNullOrEmpty(g)).Distinct().ToArray();
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/UI" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            if (!HasPortraitReferences(path, candidateGuids)) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int changed = Rebind(root, originalSprites, byBreed);
                if (changed > 0) { PrefabUtility.SaveAsPrefabAsset(root, path); report.prefabBindings += changed; }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        Scene previousActive = SceneManager.GetActiveScene();
        try
        {
            foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                if (!HasPortraitReferences(path, candidateGuids)) continue;
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    int changed = scene.GetRootGameObjects().Sum(root => Rebind(root, originalSprites, byBreed));
                    if (changed > 0)
                    { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); report.sceneBindings += changed; }
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
        }
        finally { if (previousActive.IsValid() && previousActive.isLoaded) EditorSceneManager.SetActiveScene(previousActive); }
        AssetDatabase.SaveAssets();
        var validation = Validate();
        report.breeds = validation.breeds; report.images = validation.images;
        report.errors = validation.errors; report.valid = validation.valid;
        if (!report.valid) throw new InvalidOperationException(JsonUtility.ToJson(report, true));
        return JsonUtility.ToJson(report, true);
    }

    public static Report Validate()
    {
        var report = new Report();
        var catalog = CatBreedCatalog.Load(); var finish = CatModernVisualCatalog.Load();
        if (catalog == null || finish == null) { report.errors.Add("Catalog missing."); return report; }
        report.breeds = finish.Portraits.Count;
        if (report.breeds != 10) report.errors.Add("Expected ten portrait pairs.");
        foreach (var pair in finish.Portraits)
        {
            var entry = catalog.Find(pair.breedId);
            if (entry == null || entry.Portrait != pair.identity) report.errors.Add("Identity binding missing: " + pair.breedId);
            foreach (var portrait in new[] { pair.identity, pair.fullBody })
            {
                if (portrait == null) { report.errors.Add("Missing image: " + pair.breedId); continue; }
                report.images++;
                string path = AssetDatabase.GetAssetPath(portrait); report.paths.Add(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!path.StartsWith(Folder + "/", StringComparison.Ordinal) || portrait.texture.width != Size || portrait.texture.height != Size ||
                    importer == null || importer.mipmapEnabled || importer.isReadable || !importer.alphaIsTransparency)
                    report.errors.Add("Portrait import contract failed: " + path);
                if (importer != null)
                {
                    var android = importer.GetPlatformTextureSettings("Android");
                    if (!android.overridden || android.maxTextureSize != Size || android.format != TextureImporterFormat.ETC2_RGBA8)
                        report.errors.Add("Portrait mobile format failed: " + path);
                }
            }
        }
        report.valid = report.errors.Count == 0;
        return report;
    }

    private static float Capture(CatBreedCatalog.Entry breed, CatBreedCatalog catalog, AnimationClip pose, string path, bool identity)
    {
        var preview = new PreviewRenderUtility(); var baked = new Mesh();
        Texture2D image = null; RenderTexture resolved = null;
        var active = RenderTexture.active;
        try
        {
            var cat = CatBreedVisualFactory.Create(breed, catalog.GameplayController, null);
            preview.AddSingleGO(cat);
            foreach (var part in cat.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = PreviewLayer;
            var animator = cat.GetComponentInChildren<Animator>(true);
            animator.enabled = false;
            float sampleTime = SamplePortraitPose(animator, breed, pose);
            cat.transform.rotation = Quaternion.Euler(0, 156, 0);
            Transform head = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-spine.006");
            if (head == null) throw new InvalidOperationException("Missing portrait head bone: " + breed.Id);
            var points = new List<Vector3>(); var headPoints = new List<Vector3>();
            foreach (var skin in cat.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.BakeMesh(baked, true);
                var vertices = baked.vertices; var weights = skin.sharedMesh.boneWeights; var bones = skin.bones;
                var headBones = bones.Select(b => b != null && (b == head || b.IsChildOf(head))).ToArray();
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 point = skin.transform.TransformPoint(vertices[i]); points.Add(point);
                    var w = weights[i];
                    float headWeight = HeadWeight(w.boneIndex0, w.weight0, headBones) + HeadWeight(w.boneIndex1, w.weight1, headBones) +
                        HeadWeight(w.boneIndex2, w.weight2, headBones) + HeadWeight(w.boneIndex3, w.weight3, headBones);
                    if (headWeight >= .5f) headPoints.Add(point);
                }
            }
            var measured = identity ? headPoints : points;
            if (measured.Count == 0) throw new InvalidOperationException("Empty measured portrait: " + breed.Id);
            var bounds = new Bounds(measured[0], Vector3.zero);
            foreach (var point in measured) bounds.Encapsulate(point);
            var camera = preview.camera;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << PreviewLayer; camera.orthographic = true;
            camera.nearClipPlane = .01f; camera.farClipPlane = 30; camera.allowHDR = false; camera.useOcclusionCulling = false;
            camera.transform.position = bounds.center + new Vector3(0, identity ? .04f : .18f, -3f);
            camera.transform.LookAt(bounds.center);
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var point in measured)
            {
                var local = camera.transform.InverseTransformPoint(point);
                min = Vector2.Min(min, new Vector2(local.x, local.y)); max = Vector2.Max(max, new Vector2(local.x, local.y));
            }
            // Head and ear bounds determine the bust crop; the lower margin adds
            // measured shoulder space. Full-body cards include every tail/paw vertex.
            if (identity) min.y -= (max.y - min.y) * .24f;
            Vector2 center = (min + max) * .5f, half = (max - min) * .5f;
            camera.transform.position += camera.transform.right * center.x + camera.transform.up * center.y;
            camera.orthographicSize = Mathf.Max(half.x, half.y) * (identity ? 1.10f : 1.12f);
            preview.ambientColor = new Color(.80f, .82f, .86f);
            preview.lights[0].intensity = 1.50f; preview.lights[0].color = new Color(1, .97f, .93f);
            preview.lights[0].transform.rotation = Quaternion.Euler(30, 330, 0);
            preview.lights[1].intensity = 1.05f; preview.lights[1].color = new Color(.89f, .94f, 1);
            preview.lights[1].transform.rotation = Quaternion.Euler(18, 55, 0);
            foreach (var light in preview.lights) { light.cullingMask = 1 << PreviewLayer; light.shadows = LightShadows.None; }
            preview.BeginPreview(new Rect(0, 0, Size, Size), GUIStyle.none); preview.Render(true); preview.EndPreview();
            preview.BeginPreview(new Rect(0, 0, Size, Size), GUIStyle.none); preview.Render(true);
            Texture target = preview.EndPreview();
            resolved = RenderTexture.GetTemporary(Size, Size, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(target, resolved); RenderTexture.active = resolved;
            image = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); image.Apply();
            var pixels = image.GetPixels32();
            int opaque = pixels.Count(p => p.a > 16);
            if (opaque < Size * Size / 40 || opaque > Size * Size * .95f || pixels[pixels.Length - 1].a > 16 || pixels[Size * (Size - 1)].a > 16)
                throw new InvalidOperationException("Portrait is empty, unframed, or lacks transparency: " + breed.Id + (identity ? " identity" : " full body"));
            File.WriteAllBytes(path, image.EncodeToPNG());
            return sampleTime;
        }
        finally
        {
            RenderTexture.active = active;
            if (resolved != null) RenderTexture.ReleaseTemporary(resolved);
            if (image != null) Object.DestroyImmediate(image);
            Object.DestroyImmediate(baked); preview.Cleanup();
        }
    }

    public static float SamplePortraitPose(Animator animator, CatBreedCatalog.Entry breed, AnimationClip sitting)
    {
        var face = animator.GetComponentsInChildren<Transform>(true).Where(t =>
            t.name == "DEF-jaw" || t.name.StartsWith("DEF-eyelids", StringComparison.Ordinal) || t.name == "DEF-spine.006").ToArray();
        if (face.Length < 4) throw new InvalidOperationException("The portrait needs the head, jaw and both eyelids.");
        // The shared Domestic animation can blink or open a differently shaped
        // breed's mouth even at Idle frame zero. Each breed prefab contains its
        // own neutral, open-eyed face, so use those authored transforms rather
        // than inferring neutral from one arbitrary animation sample.
        var sourceBones = breed.SourcePrefab.GetComponentsInChildren<Transform>(true);
        var sourceFace = face.Select(t => sourceBones.First(b => b.name == t.name)).ToArray();
        var neutral = sourceFace.Select(t => t.localRotation).ToArray();
        float bestTime = 0, bestScore = float.PositiveInfinity;
        for (int sample = 0; sample < 48; sample++)
        {
            float time = sitting.length * sample / 48f;
            sitting.SampleAnimation(animator.gameObject, time);
            float score = 0;
            for (int i = 0; i < face.Length; i++)
            {
                float weight = face[i].name.StartsWith("DEF-eyelids", StringComparison.Ordinal) ? 4f : face[i].name == "DEF-jaw" ? 3f : .35f;
                score += Quaternion.Angle(neutral[i], face[i].localRotation) * weight;
            }
            if (score >= bestScore) continue;
            bestTime = time; bestScore = score;
        }
        sitting.SampleAnimation(animator.gameObject, bestTime);
        // Presentation-only: retain the actual sitting body/head/ears/tail pose,
        // restoring just jaw and eyelids from this breed's authored rest pose.
        // Sitting keys translation and scale as well as rotation; retaining
        // Domestic positions on another breed would still narrow its eyes.
        // No source prefab, mesh, gameplay animator or animation clip is edited.
        for (int i = 0; i < face.Length; i++)
        {
            if (face[i].name == "DEF-spine.006") continue;
            face[i].localPosition = sourceFace[i].localPosition;
            face[i].localRotation = sourceFace[i].localRotation;
            face[i].localScale = sourceFace[i].localScale;
        }
        return bestTime;
    }

    private static float HeadWeight(int index, float weight, bool[] bones) =>
        index >= 0 && index < bones.Length && bones[index] ? weight : 0;

    private static void Import(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true;
        importer.sRGBTexture = true; importer.mipmapEnabled = false; importer.isReadable = false;
        importer.maxTextureSize = Size; importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Compressed; importer.compressionQuality = 85;
        var android = importer.GetPlatformTextureSettings("Android"); android.name = "Android"; android.overridden = true;
        android.maxTextureSize = Size; android.format = TextureImporterFormat.ETC2_RGBA8; android.compressionQuality = 85;
        importer.SetPlatformTextureSettings(android); importer.SaveAndReimport();
    }

    private static bool HasPortraitReferences(string path, string[] guids)
    {
        string text = File.ReadAllText(path);
        return guids.Any(g => text.Contains(g)) || text.Contains("RealCatPortrait");
    }

    private static int Rebind(GameObject root, Dictionary<Sprite, Sprite> replacements,
        Dictionary<string, CatModernVisualCatalog.PortraitPair> byBreed)
    {
        int changed = 0;
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            Sprite desired = image.sprite;
            if (image.name == "RealCatPortrait")
            {
                for (Transform parent = image.transform.parent; parent != null; parent = parent.parent)
                    if (parent.name.StartsWith("BreedCard_", StringComparison.Ordinal) &&
                        byBreed.TryGetValue(parent.name.Substring("BreedCard_".Length), out var pair))
                    { desired = pair.fullBody; break; }
            }
            else if (image.sprite != null && replacements.TryGetValue(image.sprite, out var identity)) desired = identity;
            if (desired != image.sprite) { image.sprite = desired; EditorUtility.SetDirty(image); changed++; }
        }
        foreach (var image in root.GetComponentsInChildren<RawImage>(true))
        {
            var pair = replacements.FirstOrDefault(p => p.Key.texture == image.texture);
            if (pair.Key == null || image.texture == pair.Value.texture) continue;
            image.texture = pair.Value.texture; EditorUtility.SetDirty(image); changed++;
        }
        return changed;
    }
}
