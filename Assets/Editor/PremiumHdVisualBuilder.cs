using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Shared native-resolution presentation, without altering room art or gameplay.</summary>
public static class PremiumHdVisualBuilder
{
    public static string BuildSilently()
    {
        ConfigurePipeline("Mobile", 1024, 1);
        ConfigurePipeline("PC", 4096, 4);
        var quality = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
        var levels = quality.FindProperty("m_QualitySettings");
        for (int i = 0; i < levels.arraySize; i++)
        {
            var level = levels.GetArrayElementAtIndex(i);
            level.FindPropertyRelative("skinWeights").intValue = 4;
            level.FindPropertyRelative("anisotropicTextures").intValue = 2;
            level.FindPropertyRelative("globalTextureMipmapLimit").intValue = 0;
            level.FindPropertyRelative("resolutionScalingFixedDPIFactor").floatValue = 1f;
        }
        quality.ApplyModifiedPropertiesWithoutUndo();
        Scene active = SceneManager.GetActiveScene();
        int cameras = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes/Levels", "Assets/Scenes/Runner", "Assets/Scenes/Catch" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                    {
                        camera.allowHDR = true; camera.allowMSAA = true; camera.allowDynamicResolution = false;
                        var data = camera.GetUniversalAdditionalCameraData();
                        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                        data.antialiasingQuality = AntialiasingQuality.High;
                        data.dithering = true; data.renderPostProcessing = true;
                        // Presentation cameras render on explicit requests, outside the gameplay view.
                        camera.cullingMask &= ~(1 << TitleCatShowcase.StageLayer);
                        EditorUtility.SetDirty(camera); EditorUtility.SetDirty(data); cameras++;
                    }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
        AssetDatabase.SaveAssets();
        return "PC HD and bounded mobile quality; " + cameras + " room/game cameras configured.";
    }
    private static void ConfigurePipeline(string name, int shadowSize, int cascades)
    {
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/" + name + "_RPAsset.asset");
        bool mobile=name=="Mobile";
        asset.renderScale = mobile ? .85f : 1f; asset.msaaSampleCount = mobile ? 2 : 4; asset.supportsHDR = true;
        if(mobile)asset.shadowDistance=16f;
        asset.mainLightShadowmapResolution = shadowSize; asset.shadowCascadeCount = cascades;
        var serialized = new SerializedObject(asset);
        serialized.FindProperty("m_SoftShadowsSupported").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        if(mobile)
        {
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Mobile_Renderer.asset");
            foreach(var feature in renderer.rendererFeatures)
            {
                if(feature==null||feature.name!="ScreenSpaceAmbientOcclusion")continue;
                var settings=new SerializedObject(feature);
                settings.FindProperty("m_Settings.Downsample").boolValue=true;
                settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(feature);
            }
        }
    }
}
