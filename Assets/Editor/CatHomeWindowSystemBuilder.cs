#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class CatHomeWindowSystemBuilder
{
    private const string MenuPath = "Tools/Cat Home/Build Selected Window System";
    private const string MaterialFolder = "Assets/Materials/WindowSystem";
    private const string SkyMaterialPath = MaterialFolder + "/WindowSky.mat";
    private const string GlassMaterialPath = MaterialFolder + "/WindowGlass.mat";
    private const string SunBeamMaterialPath = MaterialFolder + "/WindowSunBeam.mat";
    private const string GlassPanelMeshPath = "Assets/Meshes/WindowSystem/WindowGlassPanels.asset";
    private const string SunBeamMeshPath = "Assets/Meshes/WindowSystem/WindowSunBeam.asset";
    private const string SkyShaderName = "CatHome/Window Sky";
    private const string SunBeamShaderName = "CatHome/Window Sun Beam";

    private const float SkyGlassOffset = 0.001f;
    private const float GlassSkyOffset = 0.002f;
    private const float PanelPlaneScale = 1.16f;

    private readonly struct GlassTriangle
    {
        public readonly Vector3 A;
        public readonly Vector3 B;
        public readonly Vector3 C;
        public readonly long KeyA;
        public readonly long KeyB;
        public readonly long KeyC;

        public GlassTriangle(Vector3 a, Vector3 b, Vector3 c, long keyA, long keyB, long keyC)
        {
            A = a;
            B = b;
            C = c;
            KeyA = keyA;
            KeyB = keyB;
            KeyC = keyC;
        }
    }

    private sealed class GlassSurface
    {
        public readonly List<GlassTriangle> Triangles = new List<GlassTriangle>();
        public Bounds Bounds;
        public bool HasBounds;

        public void Add(GlassTriangle triangle)
        {
            Triangles.Add(triangle);
            if (!HasBounds)
            {
                Bounds = new Bounds(triangle.A, Vector3.zero);
                HasBounds = true;
            }
            Bounds.Encapsulate(triangle.A);
            Bounds.Encapsulate(triangle.B);
            Bounds.Encapsulate(triangle.C);
        }
    }

    [MenuItem(MenuPath)]
    private static void BuildSelectedWindowSystem()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null || EditorUtility.IsPersistent(selected))
        {
            ShowInvalidSelection("Hierarchy'de sahnedeki mevcut Window nesnesini seçin.");
            return;
        }

        GameObject root;
        GameObject window;
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Cat Home Window System");

        if (selected.name == "WindowSystem")
        {
            root = selected;
            window = FindDirectChild(root.transform, "Window")?.gameObject;
            if (window == null)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                ShowInvalidSelection("Seçilen WindowSystem altında doğrudan bir Window nesnesi bulunamadı.");
                return;
            }
        }
        else
        {
            window = PrefabUtility.GetNearestPrefabInstanceRoot(selected) ?? selected;
            if (!IsValidWindow(window))
            {
                Undo.RevertAllDownToGroup(undoGroup);
                ShowInvalidSelection(
                    $"'{selected.name}' geçerli bir pencere olarak doğrulanamadı. " +
                    "Window isimli, Renderer içeren pencere prefab instance'ını seçin."
                );
                return;
            }

            if (window.transform.parent != null && window.transform.parent.name == "WindowSystem")
            {
                root = window.transform.parent.gameObject;
            }
            else
            {
                root = CreateRootForWindow(window);
            }
        }

        Renderer[] windowRenderers = GetWindowRenderers(window, root.transform);
        if (windowRenderers.Length == 0 ||
            !TryGetLocalBounds(root.transform, windowRenderers, out Bounds windowBounds))
        {
            Undo.RevertAllDownToGroup(undoGroup);
            ShowInvalidSelection("Pencere geometrisinde kullanılabilir bir Renderer bounds değeri bulunamadı.");
            return;
        }

        Material skyMaterial = GetOrCreateSkyMaterial();
        Material glassMaterial = GetOrCreateGlassMaterial();
        Material sunBeamMaterial = GetOrCreateSunBeamMaterial();
        Mesh sunBeamMesh = GetOrCreateSunBeamMesh();
        if (skyMaterial == null || glassMaterial == null || sunBeamMaterial == null || sunBeamMesh == null)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            EditorUtility.DisplayDialog(
                "Cat Home Window System",
                "URP Unlit shader bulunamadı. Projenin URP paketini ve aktif Render Pipeline ayarını kontrol edin.",
                "Tamam"
            );
            return;
        }

        bool directionCertain = TryDetermineRoomDirection(
            root.transform,
            windowBounds,
            out Vector3 inwardLocal,
            out bool normalUsesLocalZ
        );

        if (!TryGetOrCreateWindowPanelQuad(
            root.transform,
            windowRenderers,
            inwardLocal,
            out Mesh glassPanelMesh,
            out Bounds openingBounds,
            out Bounds upperOpeningBounds,
            out Vector3 panelOrigin,
            out float roomFacingGlassPlaneWorld,
            out int panelTriangleCount))
        {
            Undo.RevertAllDownToGroup(undoGroup);
            ShowInvalidSelection(
                "Window meshinde panel açıklığını belirlemek için kullanılabilir cam sınırları bulunamadı."
            );
            return;
        }

        float thickness = normalUsesLocalZ ? windowBounds.size.z : windowBounds.size.x;
        if (!TryGetPanelDepthPositions(
            root.transform,
            panelOrigin,
            inwardLocal,
            roomFacingGlassPlaneWorld,
            out Vector3 skyPanelPosition,
            out Vector3 glassPanelPosition))
        {
            Undo.RevertAllDownToGroup(undoGroup);
            ShowInvalidSelection(
                "SkyPanel ve GlassPanel depth yerleşimi için Floor Renderer bounds değeri bulunamadı."
            );
            return;
        }

        Renderer skyRenderer = EnsurePanel(
            root.transform,
            "SkyPanel",
            skyPanelPosition,
            glassPanelMesh,
            skyMaterial
        );

        Renderer glassRenderer = EnsurePanel(
            root.transform,
            "GlassPanel",
            glassPanelPosition,
            glassPanelMesh,
            glassMaterial
        );

        Light windowLight = EnsureWindowLight(
            root.transform,
            openingBounds,
            inwardLocal,
            thickness
        );

        MeshRenderer sunBeamRenderer = EnsureSunBeam(
            root.transform,
            upperOpeningBounds,
            inwardLocal,
            normalUsesLocalZ,
            thickness,
            sunBeamMesh,
            sunBeamMaterial
        );

        GameTimeService timeService = GetOrAddComponent<GameTimeService>(root, out bool timeServiceCreated);
        WindowDayNightController controller = GetOrAddComponent<WindowDayNightController>(root, out _);
        if (timeServiceCreated)
            ConfigureTimeService(timeService);
        ConfigureController(
            controller,
            timeService,
            skyRenderer,
            glassRenderer,
            sunBeamRenderer,
            windowLight
        );

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(root.scene);
        AssetDatabase.SaveAssets();
        bool sceneSaved = EditorSceneManager.SaveScene(root.scene);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Undo.CollapseUndoOperations(undoGroup);

        string transformSummary =
            $"Scene save: {(sceneSaved ? "successful" : "failed")}; " +
            $"full opening quad: {glassPanelMesh.vertexCount} vertices / {panelTriangleCount} triangles; " +
            FormatTransform("SkyPanel", skyRenderer.transform) + "; " +
            FormatTransform("GlassPanel", glassRenderer.transform) + "; " +
            FormatTransform("WindowLight", windowLight.transform) + "; " +
            FormatTransform("SunBeam", sunBeamRenderer.transform) + ".";

        if (!directionCertain)
        {
            Debug.LogWarning(
                "Cat Home Window System kuruldu ancak geometri/oda yönü kesin belirlenemedi. " +
                "Inspector'da panellerin pencere açıklığında olduğunu ve WindowLight'ın odaya baktığını kontrol edin. " +
                transformSummary,
                root
            );
        }

        Debug.Log(
            "Cat Home Window System hazır. Kurulum idempotenttir; mevcut Window prefab bağlantısı korunmuştur. " +
            transformSummary,
            root
        );
    }

    public static void ValidateGameScene()
    {
        const string scenePath = "Assets/Scenes/GameScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        WindowDayNightController controller =
            UnityEngine.Object.FindAnyObjectByType<WindowDayNightController>(FindObjectsInactive.Include);
        if (controller == null || controller.gameObject.name != "WindowSystem")
            throw new InvalidOperationException("GameScene WindowSystem controller could not be found.");

        GameObject root = controller.gameObject;
        GameObject window = FindDirectChild(root.transform, "Window")?.gameObject;
        if (window == null)
            throw new InvalidOperationException("WindowSystem does not contain the existing Window prefab.");

        Vector3 rootPosition = root.transform.position;
        Quaternion rootRotation = root.transform.rotation;
        Vector3 rootScale = root.transform.lossyScale;
        Vector3 windowPosition = window.transform.position;
        Quaternion windowRotation = window.transform.rotation;
        Vector3 windowScale = window.transform.lossyScale;

        Selection.activeGameObject = root;
        BuildSelectedWindowSystem();

        controller = root.GetComponent<WindowDayNightController>();
        GameTimeService timeService = root.GetComponent<GameTimeService>();
        Renderer skyRenderer = FindDirectChild(root.transform, "SkyPanel")?.GetComponent<Renderer>();
        Renderer glassRenderer = FindDirectChild(root.transform, "GlassPanel")?.GetComponent<Renderer>();
        MeshRenderer sunBeamRenderer = FindDirectChild(root.transform, "SunBeam")?.GetComponent<MeshRenderer>();
        Light windowLight = FindDirectChild(root.transform, "WindowLight")?.GetComponent<Light>();
        if (controller == null || timeService == null || skyRenderer == null || glassRenderer == null ||
            sunBeamRenderer == null || windowLight == null)
        {
            throw new InvalidOperationException("Builder did not create or bind every WindowSystem component.");
        }

        GameObject skyObject = skyRenderer.gameObject;
        GameObject glassObject = glassRenderer.gameObject;
        GameObject lightObject = windowLight.gameObject;
        GameObject beamObject = sunBeamRenderer.gameObject;
        Selection.activeGameObject = root;
        BuildSelectedWindowSystem();

        controller = root.GetComponent<WindowDayNightController>();
        timeService = root.GetComponent<GameTimeService>();
        skyRenderer = FindDirectChild(root.transform, "SkyPanel")?.GetComponent<Renderer>();
        glassRenderer = FindDirectChild(root.transform, "GlassPanel")?.GetComponent<Renderer>();
        sunBeamRenderer = FindDirectChild(root.transform, "SunBeam")?.GetComponent<MeshRenderer>();
        windowLight = FindDirectChild(root.transform, "WindowLight")?.GetComponent<Light>();

        StringBuilder failures = new StringBuilder();
        Check(Approximately(root.transform.position, rootPosition), "WindowSystem position changed.", failures);
        Check(Quaternion.Angle(root.transform.rotation, rootRotation) < 0.001f, "WindowSystem rotation changed.", failures);
        Check(Approximately(root.transform.lossyScale, rootScale), "WindowSystem scale changed.", failures);
        Check(Approximately(window.transform.position, windowPosition), "Window position changed.", failures);
        Check(Quaternion.Angle(window.transform.rotation, windowRotation) < 0.001f, "Window rotation changed.", failures);
        Check(Approximately(window.transform.lossyScale, windowScale), "Window scale changed.", failures);
        Check(PrefabUtility.IsPartOfPrefabInstance(window), "Window prefab connection was lost.", failures);
        Check(CountDirectChildren(root.transform, "SkyPanel") == 1, "SkyPanel was duplicated.", failures);
        Check(CountDirectChildren(root.transform, "GlassPanel") == 1, "GlassPanel was duplicated.", failures);
        Check(CountDirectChildren(root.transform, "WindowLight") == 1, "WindowLight was duplicated.", failures);
        Check(CountDirectChildren(root.transform, "SunBeam") == 1, "SunBeam was duplicated.", failures);
        Check(skyRenderer.gameObject == skyObject &&
              glassRenderer.gameObject == glassObject &&
              windowLight.gameObject == lightObject &&
              sunBeamRenderer.gameObject == beamObject,
              "Second builder run replaced an existing generated object.", failures);
        Check(sunBeamRenderer.GetComponent<Collider>() == null, "SunBeam contains a collider.", failures);
        Check(sunBeamRenderer.sharedMaterial != null &&
              sunBeamRenderer.sharedMaterial.shader.name == SunBeamShaderName,
              "SunBeam is not using the URP-compatible unlit beam shader.", failures);
        Check(sunBeamRenderer.sharedMaterial != null &&
              sunBeamRenderer.sharedMaterial.HasProperty("_BaseColor"),
              "SunBeam shader does not expose the _BaseColor property used by the controller.", failures);
        Check(sunBeamRenderer.sharedMaterial != null &&
              sunBeamRenderer.sharedMaterial.renderQueue == (int)RenderQueue.Transparent - 10,
              "SunBeam render queue is not Transparent-10.", failures);
        Check(sunBeamRenderer.shadowCastingMode == ShadowCastingMode.Off && !sunBeamRenderer.receiveShadows,
              "SunBeam shadow settings are not disabled.", failures);
        SerializedObject serializedController = new SerializedObject(controller);
        Check(serializedController.FindProperty("sunBeamRenderer").objectReferenceValue == sunBeamRenderer,
              "Controller Sun Beam Renderer serialized reference is None or incorrect.", failures);
        Camera mainCamera = Camera.main;
        Check(mainCamera == null || (mainCamera.cullingMask & (1 << sunBeamRenderer.gameObject.layer)) != 0,
              "Main Camera culling mask excludes the SunBeam layer.", failures);

        Renderer[] windowRenderers = GetWindowRenderers(window, root.transform);
        Check(TryGetLocalBounds(root.transform, windowRenderers, out Bounds windowBounds),
              "Window mesh bounds could not be calculated during validation.", failures);
        Mesh skyMesh = skyRenderer.GetComponent<MeshFilter>()?.sharedMesh;
        Mesh glassMesh = glassRenderer.GetComponent<MeshFilter>()?.sharedMesh;
        Check(skyMesh != null && skyMesh == glassMesh,
              "SkyPanel and GlassPanel are not using the same full-opening quad.", failures);
        Check(skyMesh != null && skyMesh.vertexCount == 4 && skyMesh.triangles.Length / 3 == 2,
              "Panel mesh is not one four-vertex full-opening quad.", failures);
        Check(skyMesh != null && CountMeshConnectedComponents(skyMesh) == 1,
              "Panel mesh is not one connected rectangle.", failures);
        Check(skyRenderer.sharedMaterial != null && skyRenderer.sharedMaterial.GetFloat("_Cull") == (float)CullMode.Off &&
              glassRenderer.sharedMaterial != null && glassRenderer.sharedMaterial.GetFloat("_Cull") == (float)CullMode.Off,
              "SkyPanel or GlassPanel material is not using Cull Off.", failures);
        TryGetLocalBounds(root.transform, new[] { skyRenderer }, out Bounds openingBounds);
        TryDetermineRoomDirection(root.transform, windowBounds, out Vector3 inwardLocal, out bool normalUsesLocalZ);
        Check(skyMesh != null && MeshTrianglesFaceDirection(skyMesh, inwardLocal),
              "The full-opening quad does not use the room-facing winding.", failures);
        Check(Vector3.Dot(glassRenderer.transform.localPosition - skyRenderer.transform.localPosition, inwardLocal) > 0.001f,
              "GlassPanel is not in front of SkyPanel toward the room.", failures);
        Check(Vector3.Dot(windowLight.transform.localPosition - openingBounds.center, inwardLocal) > 0f,
              "WindowLight is not on the room side of the window.", failures);
        Vector3 beamDirection = sunBeamRenderer.transform.localRotation * Vector3.forward;
        Check(Vector3.Dot(beamDirection, inwardLocal) > 0.2f && Vector3.Dot(beamDirection, Vector3.down) > 0.1f,
              "SunBeam is not aimed inward and downward.", failures);
        Mesh beamMesh = sunBeamRenderer.GetComponent<MeshFilter>()?.sharedMesh;
        Check(beamMesh != null && beamMesh.vertexCount == 12 && beamMesh.triangles.Length / 3 == 12 &&
              beamMesh.bounds.size.y < 0.001f && CountMeshConnectedComponents(beamMesh) == 1,
              "SunBeam is not one open 12-vertex tapered ribbon.", failures);
        Check(beamMesh != null && MeshUsesSoftVertexAlpha(beamMesh),
              "SunBeam mesh does not contain transparent edges and a visible center in vertex alpha.", failures);
        Check(TryGetHighestConnectedComponentBounds(skyMesh, skyRenderer.transform.localPosition, out Bounds upperBounds) &&
              PointFitsOpeningPlane(sunBeamRenderer.transform.localPosition, upperBounds, inwardLocal, normalUsesLocalZ),
              "SunBeam does not start inside the upper glass opening.", failures);

        SerializedObject serializedTime = new SerializedObject(timeService);
        SerializedProperty useTestTime = serializedTime.FindProperty("useTestTime");
        SerializedProperty testHour = serializedTime.FindProperty("testHour");
        bool previousUseTestTime = useTestTime.boolValue;
        float previousTestHour = testHour.floatValue;

        float[] hours = { 0f, 5.9f, 6f, 9f, 12f, 13f, 16f, 18f, 19f, 21f, 23f };
        var beamPoseByHour = new Dictionary<float, Vector3>();
        var beamAlphaByHour = new Dictionary<float, float>();
        Vector3 beamOrigin = sunBeamRenderer.transform.localPosition;
        try
        {
            useTestTime.boolValue = true;
            serializedTime.ApplyModifiedPropertiesWithoutUndo();

            foreach (float hour in hours)
            {
                serializedTime.Update();
                serializedTime.FindProperty("testHour").floatValue = hour;
                serializedTime.ApplyModifiedPropertiesWithoutUndo();
                controller.RefreshVisuals();

                MaterialPropertyBlock skyBlock = new MaterialPropertyBlock();
                skyRenderer.GetPropertyBlock(skyBlock);
                Color skyColor = skyBlock.GetColor("_BaseColor");
                MaterialPropertyBlock beamBlock = new MaterialPropertyBlock();
                sunBeamRenderer.GetPropertyBlock(beamBlock);
                Color beamColor = beamBlock.GetColor("_BaseColor");
                // The beam window is 06:00-20:00; it is fully disabled once night begins.
                bool beamShouldBeVisible = hour >= 6f && hour < 20f;
                beamPoseByHour[hour] = sunBeamRenderer.transform.localScale;
                beamAlphaByHour[hour] = beamColor.a;
                Check(sunBeamRenderer.enabled == beamShouldBeVisible,
                      $"SunBeam visibility is incorrect at {FormatHour(hour)}.", failures);
                if (!beamShouldBeVisible)
                {
                    Check(!sunBeamRenderer.enabled,
                          $"SunBeam renderer is not fully disabled at {FormatHour(hour)}.", failures);
                }
                else if (hour == 12f)
                {
                    Check(beamColor.a >= 0.10f && beamColor.a <= 0.16f,
                          "SunBeam final alpha is outside 0.10-0.16 at 12:00.", failures);
                    Check(sunBeamRenderer.gameObject.activeInHierarchy,
                          "SunBeam GameObject is inactive at 12:00.", failures);
                }

                if (hour == 12f)
                {
                    Color expectedNoon = new Color(0.22f, 0.62f, 1f, 1f);
                    Check(MaxRgbDifference(skyColor, expectedNoon) < 0.01f,
                          "12:00 sky is not using the authored bright-blue noon color.", failures);
                }

                if (hour == 19f)
                {
                    Color expectedSunset = new Color(0.91f, 0.49f, 0.39f, 1f);
                    Check(MaxRgbDifference(skyColor, expectedSunset) < 0.01f,
                          "19:00 sky is not using the authored sunset color.", failures);
                    Check(windowLight.color.g > windowLight.color.b && windowLight.color.r >= windowLight.color.g,
                          "19:00 WindowLight is not warm peach/amber.", failures);
                }

                Debug.Log(
                    $"WindowSystem test {FormatHour(hour)} | sky {Format(skyColor)} | " +
                    $"light {Format(windowLight.color)} @ {windowLight.intensity:F3} | " +
                    $"SunBeam enabled={sunBeamRenderer.enabled}, alpha={beamColor.a:F4}, " +
                    $"scale {Format(sunBeamRenderer.transform.localScale)}, " +
                    $"landing {Format(BeamLandingPoint(sunBeamRenderer.transform))}",
                    root
                );

                if (mainCamera != null && (hour == 12f || hour == 19f))
                    CaptureCameraView(mainCamera, $"Logs/WindowSystem_{hour:00}.png");
            }

            // Shape of the day: widest and deepest at 13:00, thin and close to the window at the
            // two ends of the visible window.
            Check(beamPoseByHour[13f].x > beamPoseByHour[6f].x &&
                  beamPoseByHour[13f].x > beamPoseByHour[18f].x,
                  "SunBeam is not at its widest at 13:00.", failures);
            Check(beamPoseByHour[13f].z > beamPoseByHour[6f].z &&
                  beamPoseByHour[13f].z > beamPoseByHour[18f].z,
                  "SunBeam does not reach deepest into the room at 13:00.", failures);
            Check(beamAlphaByHour[13f] > beamAlphaByHour[9f] &&
                  beamAlphaByHour[9f] > beamAlphaByHour[6f] &&
                  beamAlphaByHour[13f] > beamAlphaByHour[18f],
                  "SunBeam intensity does not peak around 13:00.", failures);
            Check(beamPoseByHour[6f].z < beamPoseByHour[9f].z,
                  "SunBeam does not grow from 06:00 towards 09:00.", failures);
            Check(Approximately(sunBeamRenderer.transform.localPosition, beamOrigin),
                  "SunBeam origin drifted away from the window opening.", failures);
        }
        finally
        {
            serializedTime.Update();
            serializedTime.FindProperty("useTestTime").boolValue = previousUseTestTime;
            serializedTime.FindProperty("testHour").floatValue = previousTestHour;
            serializedTime.ApplyModifiedPropertiesWithoutUndo();
            controller.RefreshVisuals();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (failures.Length > 0)
            throw new InvalidOperationException("WindowSystem validation failed:\n" + failures);

        Debug.Log(
            "WindowSystem validation passed on the saved GameScene for 00:00, 05:54, 06:00, 09:00, 12:00, " +
            "13:00, 16:00, 18:00, 19:00, 21:00 and 23:00. " +
            $"Sun Beam Renderer -> {sunBeamRenderer.name}; exact panels -> " +
            $"{skyMesh.vertexCount} vertices / {skyMesh.triangles.Length / 3} triangles; " +
            $"open ribbon -> {beamMesh.vertexCount} vertices / {beamMesh.triangles.Length / 3} triangles.",
            root
        );
    }

    private static GameObject CreateRootForWindow(GameObject window)
    {
        Transform previousParent = window.transform.parent;
        int siblingIndex = window.transform.GetSiblingIndex();
        Vector3 worldPosition = window.transform.position;
        Quaternion worldRotation = window.transform.rotation;
        Vector3 worldScale = window.transform.lossyScale;

        GameObject root = new GameObject("WindowSystem");
        Undo.RegisterCreatedObjectUndo(root, "Create Window System");

        if (previousParent != null)
            Undo.SetTransformParent(root.transform, previousParent, "Parent Window System");

        root.transform.SetPositionAndRotation(worldPosition, worldRotation);
        root.transform.localScale = Vector3.one;
        root.transform.SetSiblingIndex(siblingIndex);

        Undo.SetTransformParent(window.transform, root.transform, "Move Window Into Window System");
        Undo.RecordObject(window.transform, "Preserve Window World Transform");
        window.transform.SetPositionAndRotation(worldPosition, worldRotation);

        if (!Approximately(window.transform.lossyScale, worldScale))
        {
            Vector3 currentScale = window.transform.lossyScale;
            Vector3 correction = new Vector3(
                SafeRatio(worldScale.x, currentScale.x),
                SafeRatio(worldScale.y, currentScale.y),
                SafeRatio(worldScale.z, currentScale.z)
            );
            window.transform.localScale = Vector3.Scale(window.transform.localScale, correction);
        }

        if (!Approximately(window.transform.position, worldPosition) ||
            Quaternion.Angle(window.transform.rotation, worldRotation) > 0.001f ||
            !Approximately(window.transform.lossyScale, worldScale))
        {
            Debug.LogWarning(
                "WindowSystem Builder pencerenin dünya Transform değerlerini tam olarak koruyamadı. " +
                "Undo yapın ve üst parent üzerindeki non-uniform scale değerini kontrol edin.",
                window
            );
        }

        return root;
    }

    private static bool IsValidWindow(GameObject candidate)
    {
        if (candidate == null || candidate.GetComponentsInChildren<Renderer>(true).Length == 0)
            return false;

        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(candidate);
        return candidate.name.IndexOf("window", StringComparison.OrdinalIgnoreCase) >= 0 ||
               prefabPath.IndexOf("window", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Renderer[] GetWindowRenderers(GameObject window, Transform root)
    {
        Renderer[] renderers = window.GetComponentsInChildren<Renderer>(true);
        return Array.FindAll(renderers, renderer =>
            renderer != null &&
            renderer.transform != root.Find("SkyPanel") &&
            renderer.transform != root.Find("GlassPanel")
        );
    }

    private static bool TryGetLocalBounds(Transform root, Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool initialized = false;

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (renderer == null || !TryGetRendererMeshBounds(renderer, out Bounds rendererBounds))
                continue;

            EncapsulateTransformedBounds(
                root,
                renderer.transform,
                rendererBounds,
                ref bounds,
                ref initialized
            );
        }

        return initialized && bounds.size.x > 0.001f && bounds.size.y > 0.001f;
    }

    private static bool TryGetOrCreateWindowPanelQuad(
        Transform root,
        Renderer[] renderers,
        Vector3 inwardLocal,
        out Mesh panelMesh,
        out Bounds openingBounds,
        out Bounds upperOpeningBounds,
        out Vector3 panelOrigin,
        out float roomFacingGlassPlaneWorld,
        out int panelTriangleCount)
    {
        var candidates = new List<GlassTriangle>();

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (renderer == null)
                continue;

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            Mesh sourceMesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (sourceMesh == null)
                continue;

            Vector3[] sourceVertices = sourceMesh.vertices;
            Material[] materials = renderer.sharedMaterials;
            int subMeshCount = Mathf.Min(sourceMesh.subMeshCount, materials.Length);
            for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
            {
                Material material = materials[subMesh];
                if (material == null ||
                    material.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                int[] sourceTriangles = sourceMesh.GetTriangles(subMesh);
                for (int triangle = 0; triangle + 2 < sourceTriangles.Length; triangle += 3)
                {
                    int indexA = sourceTriangles[triangle];
                    int indexB = sourceTriangles[triangle + 1];
                    int indexC = sourceTriangles[triangle + 2];
                    Vector3 a = root.InverseTransformPoint(renderer.transform.TransformPoint(sourceVertices[indexA]));
                    Vector3 b = root.InverseTransformPoint(renderer.transform.TransformPoint(sourceVertices[indexB]));
                    Vector3 c = root.InverseTransformPoint(renderer.transform.TransformPoint(sourceVertices[indexC]));
                    Vector3 cross = Vector3.Cross(b - a, c - a);
                    if (cross.sqrMagnitude < 0.00000001f)
                        continue;

                    float facing = Vector3.Dot(cross.normalized, inwardLocal);
                    if (Mathf.Abs(facing) < 0.5f)
                        continue;

                    long rendererKey = (long)rendererIndex << 32;
                    // Glass panes in this source asset do not consistently share a winding.
                    // Normalize every triangle independently instead of discarding the pane
                    // whose source normal happens to point away from the room.
                    candidates.Add(facing > 0f
                        ? new GlassTriangle(
                            a, b, c,
                            rendererKey ^ (uint)indexA,
                            rendererKey ^ (uint)indexB,
                            rendererKey ^ (uint)indexC)
                        : new GlassTriangle(
                            a, c, b,
                            rendererKey ^ (uint)indexA,
                            rendererKey ^ (uint)indexC,
                            rendererKey ^ (uint)indexB));
                }
            }
        }

        List<GlassTriangle> selected = SelectRoomFacingGlassSurfaces(candidates, inwardLocal);
        if (selected.Count == 0)
        {
            panelMesh = null;
            openingBounds = default;
            upperOpeningBounds = default;
            panelOrigin = default;
            roomFacingGlassPlaneWorld = 0f;
            panelTriangleCount = 0;
            return false;
        }

        Vector3 inwardWorld = root.TransformDirection(inwardLocal).normalized;
        roomFacingGlassPlaneWorld = float.NegativeInfinity;
        foreach (GlassTriangle triangle in candidates)
        {
            roomFacingGlassPlaneWorld = Mathf.Max(
                roomFacingGlassPlaneWorld,
                Vector3.Dot(root.TransformPoint(triangle.A), inwardWorld),
                Vector3.Dot(root.TransformPoint(triangle.B), inwardWorld),
                Vector3.Dot(root.TransformPoint(triangle.C), inwardWorld)
            );
        }

        openingBounds = new Bounds(selected[0].A, Vector3.zero);
        foreach (GlassTriangle triangle in selected)
        {
            openingBounds.Encapsulate(triangle.A);
            openingBounds.Encapsulate(triangle.B);
            openingBounds.Encapsulate(triangle.C);
        }
        panelOrigin = openingBounds.center;

        bool normalUsesLocalZ = Mathf.Abs(inwardLocal.z) > Mathf.Abs(inwardLocal.x);
        Vector3 horizontal = normalUsesLocalZ ? Vector3.right : Vector3.forward;
        float halfWidth = normalUsesLocalZ
            ? openingBounds.extents.x
            : openingBounds.extents.z;
        float halfHeight = openingBounds.extents.y;

        Vector3[] vertices =
        {
            -horizontal * halfWidth - Vector3.up * halfHeight,
            -horizontal * halfWidth + Vector3.up * halfHeight,
             horizontal * halfWidth - Vector3.up * halfHeight,
             horizontal * halfWidth + Vector3.up * halfHeight
        };
        ScalePanelVerticesInPlane(vertices, PanelPlaneScale);
        int[] triangles = { 0, 1, 2, 2, 1, 3 };
        Vector3 faceNormal = Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]);
        if (Vector3.Dot(faceNormal, inwardLocal) < 0f)
            triangles = new[] { 0, 2, 1, 2, 3, 1 };

        panelMesh = AssetDatabase.LoadAssetAtPath<Mesh>(GlassPanelMeshPath);
        bool created = panelMesh == null;
        if (created)
        {
            EnsureSunBeamMeshFolder();
            panelMesh = new Mesh { name = "WindowGlassPanels" };
        }
        else
        {
            Undo.RecordObject(panelMesh, "Update Full Window Opening Panel Mesh");
            panelMesh.Clear();
        }

        panelMesh.indexFormat = IndexFormat.UInt16;
        panelMesh.vertices = vertices;
        panelMesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f)
        };
        panelMesh.SetTriangles(triangles, 0);
        panelMesh.RecalculateNormals();
        panelMesh.RecalculateBounds();
        panelTriangleCount = triangles.Length / 3;
        if (!TryGetHighestGlassSurfaceBounds(selected, out upperOpeningBounds))
            upperOpeningBounds = openingBounds;

        if (created)
            AssetDatabase.CreateAsset(panelMesh, GlassPanelMeshPath);
        else
            EditorUtility.SetDirty(panelMesh);
        return true;
    }

    private static void ScalePanelVerticesInPlane(Vector3[] vertices, float scale)
    {
        if (vertices == null || vertices.Length == 0)
            return;

        Bounds bounds = new Bounds(vertices[0], Vector3.zero);
        for (int i = 1; i < vertices.Length; i++)
            bounds.Encapsulate(vertices[i]);

        Vector3 size = bounds.size;
        int depthAxis = size.x <= size.y && size.x <= size.z
            ? 0
            : size.y <= size.z
                ? 1
                : 2;
        Vector3 center = bounds.center;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            if (depthAxis != 0)
                vertex.x = center.x + (vertex.x - center.x) * scale;
            if (depthAxis != 1)
                vertex.y = center.y + (vertex.y - center.y) * scale;
            if (depthAxis != 2)
                vertex.z = center.z + (vertex.z - center.z) * scale;
            vertices[i] = vertex;
        }
    }

    private static bool TryGetHighestGlassSurfaceBounds(
        List<GlassTriangle> triangles,
        out Bounds highestBounds)
    {
        highestBounds = default;
        if (triangles.Count == 0)
            return false;

        int[] parents = new int[triangles.Count];
        var triangleByVertex = new Dictionary<long, int>();
        for (int i = 0; i < triangles.Count; i++)
        {
            parents[i] = i;
            GlassTriangle triangle = triangles[i];
            ConnectTriangleVertex(triangle.KeyA, i, parents, triangleByVertex);
            ConnectTriangleVertex(triangle.KeyB, i, parents, triangleByVertex);
            ConnectTriangleVertex(triangle.KeyC, i, parents, triangleByVertex);
        }

        var boundsByRoot = new Dictionary<int, Bounds>();
        for (int i = 0; i < triangles.Count; i++)
        {
            int root = FindRoot(parents, i);
            GlassTriangle triangle = triangles[i];
            if (!boundsByRoot.TryGetValue(root, out Bounds bounds))
                bounds = new Bounds(triangle.A, Vector3.zero);
            bounds.Encapsulate(triangle.A);
            bounds.Encapsulate(triangle.B);
            bounds.Encapsulate(triangle.C);
            boundsByRoot[root] = bounds;
        }

        bool found = false;
        foreach (Bounds bounds in boundsByRoot.Values)
        {
            if (!found || bounds.center.y > highestBounds.center.y)
            {
                highestBounds = bounds;
                found = true;
            }
        }
        return found;
    }

    private static List<GlassTriangle> SelectRoomFacingGlassSurfaces(
        List<GlassTriangle> candidates,
        Vector3 inwardLocal)
    {
        if (candidates.Count == 0)
            return candidates;

        int[] parents = new int[candidates.Count];
        var triangleByVertex = new Dictionary<long, int>();
        for (int i = 0; i < candidates.Count; i++)
        {
            parents[i] = i;
            GlassTriangle triangle = candidates[i];
            ConnectTriangleVertex(triangle.KeyA, i, parents, triangleByVertex);
            ConnectTriangleVertex(triangle.KeyB, i, parents, triangleByVertex);
            ConnectTriangleVertex(triangle.KeyC, i, parents, triangleByVertex);
        }

        var surfaceByRoot = new Dictionary<int, GlassSurface>();
        for (int i = 0; i < candidates.Count; i++)
        {
            int root = FindRoot(parents, i);
            if (!surfaceByRoot.TryGetValue(root, out GlassSurface surface))
            {
                surface = new GlassSurface();
                surfaceByRoot.Add(root, surface);
            }
            surface.Add(candidates[i]);
        }

        var surfaces = new List<GlassSurface>(surfaceByRoot.Values);
        if (surfaces.Count <= 2)
            return candidates;

        surfaces.Sort((left, right) => left.Bounds.center.y.CompareTo(right.Bounds.center.y));
        int splitIndex = 1;
        float largestVerticalGap = float.NegativeInfinity;
        for (int i = 1; i < surfaces.Count; i++)
        {
            float gap = surfaces[i].Bounds.center.y - surfaces[i - 1].Bounds.center.y;
            if (gap > largestVerticalGap)
            {
                largestVerticalGap = gap;
                splitIndex = i;
            }
        }

        var selected = new List<GlassTriangle>();
        AppendMostRoomwardSurface(surfaces, 0, splitIndex, inwardLocal, selected);
        AppendMostRoomwardSurface(surfaces, splitIndex, surfaces.Count, inwardLocal, selected);
        return selected;
    }

    private static void ConnectTriangleVertex(
        long vertexKey,
        int triangleIndex,
        int[] parents,
        Dictionary<long, int> triangleByVertex)
    {
        if (triangleByVertex.TryGetValue(vertexKey, out int connectedTriangle))
            Union(parents, triangleIndex, connectedTriangle);
        else
            triangleByVertex.Add(vertexKey, triangleIndex);
    }

    private static void AppendMostRoomwardSurface(
        List<GlassSurface> surfaces,
        int start,
        int end,
        Vector3 inwardLocal,
        List<GlassTriangle> selected)
    {
        GlassSurface best = null;
        float bestRoomCoordinate = float.NegativeInfinity;
        for (int i = start; i < end; i++)
        {
            float roomCoordinate = Vector3.Dot(surfaces[i].Bounds.center, inwardLocal);
            if (best == null || roomCoordinate > bestRoomCoordinate)
            {
                best = surfaces[i];
                bestRoomCoordinate = roomCoordinate;
            }
        }

        if (best != null)
            selected.AddRange(best.Triangles);
    }

    private static bool TryGetHighestConnectedComponentBounds(
        Mesh mesh,
        Vector3 meshOrigin,
        out Bounds highestBounds)
    {
        highestBounds = default;
        if (mesh == null || mesh.vertexCount == 0)
            return false;

        int[] parents = new int[mesh.vertexCount];
        for (int i = 0; i < parents.Length; i++)
            parents[i] = i;

        int[] triangles = mesh.triangles;
        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            Union(parents, triangles[i], triangles[i + 1]);
            Union(parents, triangles[i], triangles[i + 2]);
        }

        Vector3[] vertices = mesh.vertices;
        var componentBounds = new Dictionary<int, Bounds>();
        foreach (int vertexIndex in triangles)
        {
            int root = FindRoot(parents, vertexIndex);
            Vector3 position = vertices[vertexIndex] + meshOrigin;
            if (componentBounds.TryGetValue(root, out Bounds bounds))
            {
                bounds.Encapsulate(position);
                componentBounds[root] = bounds;
            }
            else
            {
                componentBounds.Add(root, new Bounds(position, Vector3.zero));
            }
        }

        bool found = false;
        foreach (Bounds bounds in componentBounds.Values)
        {
            if (!found || bounds.center.y > highestBounds.center.y)
            {
                highestBounds = bounds;
                found = true;
            }
        }
        return found;
    }

    private static bool TryGetRendererMeshBounds(Renderer renderer, out Bounds bounds)
    {
        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            bounds = meshFilter.sharedMesh.bounds;
            return true;
        }

        if (renderer is SkinnedMeshRenderer skinnedRenderer && skinnedRenderer.sharedMesh != null)
        {
            bounds = skinnedRenderer.localBounds;
            return true;
        }

        bounds = default;
        return false;
    }

    private static void EncapsulateTransformedBounds(
        Transform root,
        Transform source,
        Bounds sourceBounds,
        ref Bounds result,
        ref bool initialized)
    {
        Vector3 min = sourceBounds.min;
        Vector3 max = sourceBounds.max;

        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        for (int z = 0; z < 2; z++)
        {
            Vector3 sourceCorner = new Vector3(
                x == 0 ? min.x : max.x,
                y == 0 ? min.y : max.y,
                z == 0 ? min.z : max.z
            );
            Vector3 rootLocalCorner = root.InverseTransformPoint(source.TransformPoint(sourceCorner));

            if (!initialized)
            {
                result = new Bounds(rootLocalCorner, Vector3.zero);
                initialized = true;
            }
            else
            {
                result.Encapsulate(rootLocalCorner);
            }
        }
    }

    private static bool TryDetermineRoomDirection(
        Transform root,
        Bounds bounds,
        out Vector3 inwardLocal,
        out bool normalUsesLocalZ)
    {
        normalUsesLocalZ = bounds.size.z <= bounds.size.x;
        Vector3 normalLocal = normalUsesLocalZ ? Vector3.forward : Vector3.right;
        Vector3 windowCenter = root.TransformPoint(bounds.center);

        bool foundFloor = TryFindSceneRendererBounds(root, "Floor", out Bounds floorBounds);
        Camera sceneCamera = Camera.main;
        if (sceneCamera == null)
            sceneCamera = UnityEngine.Object.FindAnyObjectByType<Camera>();

        Vector3 towardRoom = foundFloor
            ? floorBounds.center - windowCenter
            : sceneCamera != null
                ? sceneCamera.transform.position - windowCenter
                : -windowCenter;
        towardRoom = Vector3.ProjectOnPlane(towardRoom, root.up);

        bool hasDirection = towardRoom.sqrMagnitude > 0.01f;
        if (!hasDirection)
            towardRoom = -root.forward;

        Vector3 normalWorld = root.TransformDirection(normalLocal).normalized;
        if (Vector3.Dot(normalWorld, towardRoom) < 0f)
            normalLocal = -normalLocal;

        inwardLocal = normalLocal;

        float horizontalMax = Mathf.Max(bounds.size.x, bounds.size.z);
        float horizontalMin = Mathf.Min(bounds.size.x, bounds.size.z);
        bool planeIsClear = horizontalMax > 0.001f && horizontalMin / horizontalMax < 0.65f;
        return (foundFloor || sceneCamera != null) && hasDirection && planeIsClear;
    }

    private static bool TryGetPanelDepthPositions(
        Transform root,
        Vector3 windowCenterLocal,
        Vector3 windowNormalLocal,
        float roomFacingGlassPlaneWorld,
        out Vector3 skyPositionLocal,
        out Vector3 glassPositionLocal)
    {
        skyPositionLocal = windowCenterLocal;
        glassPositionLocal = windowCenterLocal;

        if (!TryFindSceneRendererBounds(root, "Floor", out Bounds floorBounds))
        {
            return false;
        }

        Vector3 windowCenterWorld = root.TransformPoint(windowCenterLocal);
        Vector3 towardRoomWorld = Vector3.ProjectOnPlane(
            floorBounds.center - windowCenterWorld,
            Vector3.up
        );
        if (towardRoomWorld.sqrMagnitude < 0.000001f)
            return false;

        towardRoomWorld.Normalize();
        Vector3 depthDirectionWorld = root.TransformDirection(windowNormalLocal).normalized;
        if (Vector3.Dot(depthDirectionWorld, towardRoomWorld) < 0f)
            depthDirectionWorld = -depthDirectionWorld;

        float windowCenterDepth = Vector3.Dot(windowCenterWorld, depthDirectionWorld);

        Vector3 skyPositionWorld = windowCenterWorld + depthDirectionWorld *
            (roomFacingGlassPlaneWorld + SkyGlassOffset - windowCenterDepth);
        Vector3 glassPositionWorld = skyPositionWorld + depthDirectionWorld * GlassSkyOffset;

        skyPositionLocal = root.InverseTransformPoint(skyPositionWorld);
        glassPositionLocal = root.InverseTransformPoint(glassPositionWorld);
        return true;
    }

    private static Renderer EnsurePanel(
        Transform root,
        string panelName,
        Vector3 localPosition,
        Mesh mesh,
        Material material)
    {
        Transform existing = FindDirectChild(root, panelName);
        bool created = existing == null;
        GameObject panel;

        if (created)
        {
            panel = new GameObject(panelName);
            Undo.RegisterCreatedObjectUndo(panel, "Create " + panelName);
            Undo.SetTransformParent(panel.transform, root, "Parent " + panelName);
        }
        else
        {
            panel = existing.gameObject;
        }

        Undo.RecordObject(panel.transform, "Align " + panelName);
        panel.transform.localPosition = localPosition;
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = Vector3.one;

        Collider collider = panel.GetComponent<Collider>();
        if (collider != null)
            Undo.DestroyObjectImmediate(collider);

        MeshFilter meshFilter = panel.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = Undo.AddComponent<MeshFilter>(panel);
        Undo.RecordObject(meshFilter, "Configure " + panelName + " Mesh");
        meshFilter.sharedMesh = mesh;

        Renderer renderer = panel.GetComponent<Renderer>();
        if (renderer == null)
            renderer = Undo.AddComponent<MeshRenderer>(panel);

        Undo.RecordObject(renderer, "Configure " + panelName + " Renderer");
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        return renderer;
    }

    private static Light EnsureWindowLight(
        Transform root,
        Bounds bounds,
        Vector3 inwardLocal,
        float thickness)
    {
        Transform existing = FindDirectChild(root, "WindowLight");
        bool created = existing == null;
        GameObject lightObject;

        if (created)
        {
            lightObject = new GameObject("WindowLight");
            Undo.RegisterCreatedObjectUndo(lightObject, "Create Window Light");
            Undo.SetTransformParent(lightObject.transform, root, "Parent Window Light");
        }
        else
        {
            lightObject = existing.gameObject;
        }

        Vector3 localPosition = bounds.center +
                                inwardLocal * (Mathf.Max(thickness * 0.5f, 0.06f) + 0.12f);
        float targetDistance = Mathf.Max(bounds.size.y * 2.4f, 2f);
        Vector3 target = bounds.center +
                         inwardLocal * targetDistance +
                         Vector3.down * bounds.size.y * 0.55f;
        Undo.RecordObject(lightObject.transform, "Align Window Light");
        lightObject.transform.localPosition = localPosition;
        lightObject.transform.localRotation = Quaternion.LookRotation(target - localPosition, Vector3.up);
        lightObject.transform.localScale = Vector3.one;

        Light light = lightObject.GetComponent<Light>();
        bool lightComponentCreated = light == null;
        if (lightComponentCreated)
            light = Undo.AddComponent<Light>(lightObject);

        Undo.RecordObject(light, "Configure Window Light");
        light.type = LightType.Spot;
        if (created || lightComponentCreated)
        {
            light.range = 8f;
            light.spotAngle = 100f;
            light.innerSpotAngle = 60f;
        }
        light.shadows = LightShadows.None;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        light.renderMode = LightRenderMode.Auto;

        return light;
    }

    private static MeshRenderer EnsureSunBeam(
        Transform root,
        Bounds upperOpeningBounds,
        Vector3 inwardLocal,
        bool normalUsesLocalZ,
        float windowThickness,
        Mesh mesh,
        Material material)
    {
        Transform existing = FindDirectChild(root, "SunBeam");
        GameObject beamObject;
        if (existing == null)
        {
            beamObject = new GameObject("SunBeam");
            Undo.RegisterCreatedObjectUndo(beamObject, "Create Sun Beam");
            Undo.SetTransformParent(beamObject.transform, root, "Parent Sun Beam");
        }
        else
        {
            beamObject = existing.gameObject;
        }

        MeshFilter meshFilter = beamObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = Undo.AddComponent<MeshFilter>(beamObject);
        Undo.RecordObject(meshFilter, "Configure Sun Beam Mesh");
        meshFilter.sharedMesh = mesh;

        MeshRenderer renderer = beamObject.GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = Undo.AddComponent<MeshRenderer>(beamObject);
        Undo.RecordObject(renderer, "Configure Sun Beam Renderer");
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.sortingOrder = -10;

        Collider[] colliders = beamObject.GetComponents<Collider>();
        foreach (Collider collider in colliders)
            Undo.DestroyObjectImmediate(collider);

        beamObject.layer = root.gameObject.layer;

        float openingWidth = normalUsesLocalZ ? upperOpeningBounds.size.x : upperOpeningBounds.size.z;
        float openingHeight = upperOpeningBounds.size.y;
        Vector3 start = upperOpeningBounds.center +
                        inwardLocal * (Mathf.Max(windowThickness * 0.5f, 0.04f) + 0.025f);
        float forwardDistance = Mathf.Max(openingHeight * 2.4f, openingWidth * 3.5f);
        float downwardDistance = openingHeight * 1.7f;
        Vector3 beamVector = inwardLocal * forwardDistance + Vector3.down * downwardDistance;

        if (TryFindSceneRendererBounds(root, "Carpet", out Bounds carpetBounds) ||
            TryFindSceneRendererBounds(root, "Rug", out carpetBounds))
        {
            Vector3 target = root.InverseTransformPoint(carpetBounds.center);
            Vector3 lateral = normalUsesLocalZ ? Vector3.right : Vector3.forward;
            float lateralExtent = normalUsesLocalZ
                ? root.InverseTransformVector(carpetBounds.extents).x
                : root.InverseTransformVector(carpetBounds.extents).z;
            target += lateral * Mathf.Abs(lateralExtent) * 0.3f;
            Vector3 towardCarpet = target - start;
            if (Vector3.Dot(towardCarpet, inwardLocal) > openingWidth * 0.5f &&
                Vector3.Dot(towardCarpet, Vector3.down) > openingHeight * 0.25f)
            {
                beamVector = towardCarpet;
            }
        }

        Undo.RecordObject(beamObject.transform, "Align Sun Beam");
        beamObject.transform.localPosition = start;
        beamObject.transform.localRotation = Quaternion.LookRotation(beamVector.normalized, Vector3.up);
        beamObject.transform.localScale = new Vector3(
            openingWidth * 0.78f,
            1f,
            beamVector.magnitude
        );

        return renderer;
    }

    private static bool TryFindSceneRendererBounds(
        Transform context,
        string nameFragment,
        out Bounds bounds)
    {
        bounds = default;
        bool initialized = false;
        GameObject[] sceneRoots = context.gameObject.scene.GetRootGameObjects();
        foreach (GameObject sceneRoot in sceneRoots)
        {
            Renderer[] renderers = sceneRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    renderer.gameObject.name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }
        return initialized;
    }

    private static T GetOrAddComponent<T>(GameObject target, out bool created) where T : Component
    {
        T component = target.GetComponent<T>();
        created = component == null;
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static void ConfigureTimeService(GameTimeService timeService)
    {
        SerializedObject serialized = new SerializedObject(timeService);
        serialized.FindProperty("useTestTime").boolValue = true;
        serialized.FindProperty("testHour").floatValue = 12f;
        serialized.FindProperty("utcOffsetMinutes").intValue = 180;
        serialized.ApplyModifiedProperties();
    }

    private static void ConfigureController(
        WindowDayNightController controller,
        GameTimeService timeService,
        Renderer skyRenderer,
        Renderer glassRenderer,
        MeshRenderer sunBeamRenderer,
        Light windowLight)
    {
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("timeService").objectReferenceValue = timeService;
        serialized.FindProperty("skyRenderer").objectReferenceValue = skyRenderer;
        serialized.FindProperty("glassRenderer").objectReferenceValue = glassRenderer;
        serialized.FindProperty("sunBeamRenderer").objectReferenceValue = sunBeamRenderer;
        serialized.FindProperty("windowLight").objectReferenceValue = windowLight;
        serialized.FindProperty("dawnSkyColor").colorValue =
            new Color(0.48f, 0.68f, 0.84f, 1f);
        serialized.FindProperty("daySkyColor").colorValue =
            new Color(0.22f, 0.62f, 1f, 1f);
        serialized.FindProperty("sunsetSkyColor").colorValue =
            new Color(0.91f, 0.49f, 0.39f, 1f);
        serialized.FindProperty("nightSkyColor").colorValue =
            new Color(0.06f, 0.11f, 0.27f, 1f);
        serialized.FindProperty("dawnHorizonColor").colorValue =
            new Color(0.96f, 0.77f, 0.62f, 1f);
        serialized.FindProperty("dayHorizonColor").colorValue =
            new Color(0.72f, 0.91f, 1f, 1f);
        serialized.FindProperty("sunsetHorizonColor").colorValue =
            new Color(1f, 0.72f, 0.62f, 1f);
        serialized.FindProperty("nightHorizonColor").colorValue =
            new Color(0.15f, 0.22f, 0.39f, 1f);
        serialized.FindProperty("dawnLightColor").colorValue =
            new Color(1f, 0.84f, 0.68f, 1f);
        serialized.FindProperty("dayLightColor").colorValue =
            new Color(1f, 0.97f, 0.91f, 1f);
        serialized.FindProperty("sunsetLightColor").colorValue =
            new Color(1f, 0.69f, 0.44f, 1f);
        serialized.FindProperty("nightLightColor").colorValue =
            new Color(0.56f, 0.66f, 0.85f, 1f);
        serialized.FindProperty("dawnLightIntensity").floatValue = 1.70f;
        serialized.FindProperty("dayLightIntensity").floatValue = 3.15f;
        serialized.FindProperty("sunsetLightIntensity").floatValue = 1.60f;
        serialized.FindProperty("nightLightIntensity").floatValue = 0.26f;
        serialized.FindProperty("transitionDurationHours").floatValue = 1f;
        serialized.FindProperty("glassTint").colorValue = new Color(0.86f, 0.92f, 1f, 0.1f);
        serialized.FindProperty("glassSkyInfluence").floatValue = 0.18f;
        serialized.FindProperty("sunBeamMaxAlpha").floatValue = 0.12f;
        serialized.FindProperty("sunBeamEndHour").floatValue = 20f;

        // EnsureSunBeam has just rewritten the beam transform to its neutral pose, so reading it
        // back here always captures the base the hour keys are expressed against - even on a
        // rebuild where the controller had already animated the beam away from it.
        Transform beam = sunBeamRenderer.transform;
        serialized.FindProperty("sunBeamBaseLocalPosition").vector3Value = beam.localPosition;
        serialized.FindProperty("sunBeamBaseVector").vector3Value =
            (beam.localRotation * Vector3.forward) * beam.localScale.z;
        serialized.FindProperty("sunBeamBaseWidth").floatValue = Mathf.Max(0.0001f, beam.localScale.x);
        UpgradeMissingBeamColors(serialized.FindProperty("sunBeamKeys"));
        serialized.ApplyModifiedProperties();
    }

    /// <summary>
    /// Beam keys authored before the hourly tint existed deserialize their colour as black.
    /// Fill those in from the shipped ramp at each key's own hour and leave every authored
    /// colour alone, so a second run of the builder changes nothing.
    /// </summary>
    private static void UpgradeMissingBeamColors(SerializedProperty keys)
    {
        if (keys == null || !keys.isArray)
            return;

        for (int i = 0; i < keys.arraySize; i++)
        {
            SerializedProperty beamColor = keys.GetArrayElementAtIndex(i).FindPropertyRelative("beamColor");
            SerializedProperty hour = keys.GetArrayElementAtIndex(i).FindPropertyRelative("hour");
            if (beamColor == null || hour == null)
                continue;

            if (WindowDayNightController.IsUnsetColor(beamColor.colorValue))
                beamColor.colorValue = WindowDayNightController.SampleDefaultBeamColor(hour.floatValue);
        }
    }

    private static Material GetOrCreateSkyMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);

        Shader shader = Shader.Find(SkyShaderName);
        if (shader == null)
            return null;

        if (material == null)
        {
            EnsureMaterialFolder();
            material = new Material(shader) { name = "WindowSky" };
            AssetDatabase.CreateAsset(material, SkyMaterialPath);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        Color dayColor = new Color(0.22f, 0.62f, 1f, 1f);
        material.SetColor("_BaseColor", dayColor);
        material.SetColor("_Color", dayColor);
        material.SetColor("_HorizonColor", new Color(0.72f, 0.91f, 1f, 1f));
        material.SetFloat("_StarStrength", 0f);
        material.doubleSidedGI = true;
        material.renderQueue = (int)RenderQueue.Geometry;
        material.SetOverrideTag("RenderType", "Opaque");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetOrCreateGlassMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(GlassMaterialPath);

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            return null;

        if (material == null)
        {
            EnsureMaterialFolder();
            material = new Material(shader) { name = "WindowGlass" };
            AssetDatabase.CreateAsset(material, GlassMaterialPath);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        Color tint = new Color(0.86f, 0.92f, 1f, 0.1f);
        material.SetColor("_BaseColor", tint);
        material.SetColor("_Color", tint);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", (float)CullMode.Off);
        material.doubleSidedGI = true;
        material.SetFloat("_AlphaClip", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetOrCreateSunBeamMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(SunBeamMaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find(SunBeamShaderName);
            if (shader == null)
                return null;

            EnsureMaterialFolder();
            material = new Material(shader)
            {
                name = "WindowSunBeam"
            };
            AssetDatabase.CreateAsset(material, SunBeamMaterialPath);
        }

        // Step 1 keeps a single constant warm white; the controller drives the alpha per hour.
        Color beamColor = new Color(1f, 0.9490196f, 0.8666667f, 0.12f);
        material.SetColor("_BaseColor", beamColor);
        material.SetColor("_Color", beamColor);
        material.renderQueue = (int)RenderQueue.Transparent - 10;
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetShaderPassEnabled("ShadowCaster", false);
        material.SetShaderPassEnabled("DepthOnly", false);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh GetOrCreateSunBeamMesh()
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SunBeamMeshPath);
        bool created = mesh == null;
        if (created)
        {
            EnsureSunBeamMeshFolder();
            mesh = new Mesh
            {
                name = "WindowSunBeam"
            };
        }
        else
        {
            Undo.RecordObject(mesh, "Update Sun Beam Mesh");
            mesh.Clear();
        }

        float[] rowPositions = { 0f, 0.16f, 0.68f, 1f };
        float[] rowAlpha = { 0f, 0.72f, 1f, 0f };
        float[] columnPositions = { -1f, 0f, 1f };
        float[] columnAlpha = { 0f, 1f, 0f };
        int columns = columnPositions.Length;
        int rows = rowPositions.Length;

        Vector3[] vertices = new Vector3[columns * rows];
        Vector2[] uv = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];

        for (int row = 0; row < rows; row++)
        {
            float distance = rowPositions[row];
            float halfWidth = Mathf.Lerp(0.28f, 1f, distance);
            for (int column = 0; column < columns; column++)
            {
                int index = BeamVertexIndex(row, column, columns);
                vertices[index] = new Vector3(
                    columnPositions[column] * halfWidth,
                    0f,
                    distance
                );
                uv[index] = new Vector2((columnPositions[column] + 1f) * 0.5f, distance);
                colors[index] = new Color(1f, 1f, 1f, rowAlpha[row] * columnAlpha[column]);
            }
        }

        var triangles = new List<int>();
        for (int row = 0; row < rows - 1; row++)
        for (int column = 0; column < columns - 1; column++)
        {
            int nearLeft = BeamVertexIndex(row, column, columns);
            int nearRight = BeamVertexIndex(row, column + 1, columns);
            int farLeft = BeamVertexIndex(row + 1, column, columns);
            int farRight = BeamVertexIndex(row + 1, column + 1, columns);
            AddQuad(triangles, nearLeft, farLeft, nearRight, farRight);
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.colors = colors;
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (created)
            AssetDatabase.CreateAsset(mesh, SunBeamMeshPath);
        else
            EditorUtility.SetDirty(mesh);

        return mesh;
    }

    private static int BeamVertexIndex(int row, int column, int columns)
    {
        return row * columns + column;
    }

    private static void AddQuad(List<int> triangles, int nearLeft, int farLeft, int nearRight, int farRight)
    {
        triangles.Add(nearLeft);
        triangles.Add(farLeft);
        triangles.Add(nearRight);
        triangles.Add(nearRight);
        triangles.Add(farLeft);
        triangles.Add(farRight);
    }

    private static void EnsureMaterialFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "WindowSystem");
    }

    private static void EnsureSunBeamMeshFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Meshes"))
            AssetDatabase.CreateFolder("Assets", "Meshes");
        if (!AssetDatabase.IsValidFolder("Assets/Meshes/WindowSystem"))
            AssetDatabase.CreateFolder("Assets/Meshes", "WindowSystem");
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }
        return null;
    }

    private static void ShowInvalidSelection(string message)
    {
        EditorUtility.DisplayDialog("Cat Home Window System", message, "Tamam");
        Debug.LogWarning("Cat Home Window System: " + message);
    }

    private static bool Approximately(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x) < 0.0001f &&
               Mathf.Abs(a.y - b.y) < 0.0001f &&
               Mathf.Abs(a.z - b.z) < 0.0001f;
    }

    private static float SafeRatio(float desired, float current)
    {
        return Mathf.Abs(current) > 0.00001f ? desired / current : 1f;
    }

    private static string Format(Vector3 value)
    {
        return $"({value.x:F4}, {value.y:F4}, {value.z:F4})";
    }

    private static string FormatHour(float hour)
    {
        int wholeHours = Mathf.FloorToInt(hour);
        int minutes = Mathf.RoundToInt((hour - wholeHours) * 60f);
        return $"{wholeHours:00}:{minutes:00}";
    }

    /// <summary>
    /// Where the beam ribbon meets the floor, in WindowSystem local space.
    /// </summary>
    private static Vector3 BeamLandingPoint(Transform beam)
    {
        return beam.localPosition + (beam.localRotation * Vector3.forward) * beam.localScale.z;
    }

    private static int CountDirectChildren(Transform parent, string childName)
    {
        int count = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).name == childName)
                count++;
        }
        return count;
    }

    private static int CountMeshConnectedComponents(Mesh mesh)
    {
        if (mesh == null || mesh.vertexCount == 0)
            return 0;

        int[] parents = new int[mesh.vertexCount];
        for (int i = 0; i < parents.Length; i++)
            parents[i] = i;

        int[] triangles = mesh.triangles;
        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            Union(parents, triangles[i], triangles[i + 1]);
            Union(parents, triangles[i], triangles[i + 2]);
        }

        var roots = new HashSet<int>();
        foreach (int vertex in triangles)
            roots.Add(FindRoot(parents, vertex));
        return roots.Count;
    }

    private static bool MeshTrianglesFaceDirection(Mesh mesh, Vector3 expectedDirection)
    {
        if (mesh == null)
            return false;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];
            Vector3 cross = Vector3.Cross(b - a, c - a);
            if (cross.sqrMagnitude < 0.00000001f || Vector3.Dot(cross.normalized, expectedDirection) < 0.9f)
                return false;
        }
        return triangles.Length > 0;
    }

    private static bool MeshUsesSoftVertexAlpha(Mesh mesh)
    {
        if (mesh == null || mesh.colors == null || mesh.colors.Length != mesh.vertexCount)
            return false;

        bool hasTransparentEdge = false;
        bool hasVisibleCenter = false;
        foreach (Color color in mesh.colors)
        {
            hasTransparentEdge |= color.a < 0.001f;
            hasVisibleCenter |= color.a > 0.9f;
        }
        return hasTransparentEdge && hasVisibleCenter;
    }

    private static bool PointFitsOpeningPlane(
        Vector3 point,
        Bounds opening,
        Vector3 inwardLocal,
        bool normalUsesLocalZ)
    {
        Vector3 projected = point - inwardLocal * Vector3.Dot(point - opening.center, inwardLocal);
        const float tolerance = 0.001f;
        float horizontal = normalUsesLocalZ ? projected.x : projected.z;
        float minHorizontal = normalUsesLocalZ ? opening.min.x : opening.min.z;
        float maxHorizontal = normalUsesLocalZ ? opening.max.x : opening.max.z;
        return horizontal >= minHorizontal - tolerance && horizontal <= maxHorizontal + tolerance &&
               projected.y >= opening.min.y - tolerance && projected.y <= opening.max.y + tolerance;
    }

    private static int FindRoot(int[] parents, int index)
    {
        while (parents[index] != index)
        {
            parents[index] = parents[parents[index]];
            index = parents[index];
        }
        return index;
    }

    private static void Union(int[] parents, int a, int b)
    {
        int rootA = FindRoot(parents, a);
        int rootB = FindRoot(parents, b);
        if (rootA != rootB)
            parents[rootB] = rootA;
    }

    private static bool PanelFitsOpening(
        Transform root,
        Renderer panel,
        Bounds openingBounds,
        bool normalUsesLocalZ)
    {
        if (!TryGetLocalBounds(root, new[] { panel }, out Bounds panelBounds))
            return false;

        const float tolerance = 0.001f;
        float panelMinHorizontal = normalUsesLocalZ ? panelBounds.min.x : panelBounds.min.z;
        float panelMaxHorizontal = normalUsesLocalZ ? panelBounds.max.x : panelBounds.max.z;
        float openingMinHorizontal = normalUsesLocalZ ? openingBounds.min.x : openingBounds.min.z;
        float openingMaxHorizontal = normalUsesLocalZ ? openingBounds.max.x : openingBounds.max.z;

        return panelMinHorizontal >= openingMinHorizontal - tolerance &&
               panelMaxHorizontal <= openingMaxHorizontal + tolerance &&
               panelBounds.min.y >= openingBounds.min.y - tolerance &&
               panelBounds.max.y <= openingBounds.max.y + tolerance;
    }

    private static void Check(bool condition, string message, StringBuilder failures)
    {
        if (!condition)
            failures.AppendLine("- " + message);
    }

    private static float MaxRgbDifference(Color a, Color b)
    {
        return Mathf.Max(
            Mathf.Abs(a.r - b.r),
            Mathf.Abs(a.g - b.g),
            Mathf.Abs(a.b - b.b)
        );
    }

    private static string Format(Color value)
    {
        return $"({value.r:F4}, {value.g:F4}, {value.b:F4}, {value.a:F4})";
    }

    private static string FormatTransform(string objectName, Transform transform)
    {
        return $"{objectName} local position/rotation/scale: " +
               $"{Format(transform.localPosition)} / " +
               $"{Format(transform.localEulerAngles)} / " +
               $"{Format(transform.localScale)}";
    }

    private static void CaptureCameraView(Camera camera, string path)
    {
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply(false, false);
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
#endif
