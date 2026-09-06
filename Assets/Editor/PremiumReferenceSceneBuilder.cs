using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Clean contact shading and a measured, warm indoor fill.</summary>
public static class PremiumReferenceSceneBuilder
{
    public static void Apply()
    {
        foreach(string name in new[]{"Mobile","PC"})
        {
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/"+name+"_Renderer.asset");
            foreach(var feature in renderer.rendererFeatures)
            {
                if(!(feature is ScreenSpaceAmbientOcclusion))continue;
                var so=new SerializedObject(feature);var p=so.FindProperty("m_Settings");
                // Depth-derived low-sample normals produced a repeating dirty
                // pattern on flat walls. Use the authored normals and bilateral blur.
                p.FindPropertyRelative("AOMethod").intValue=0;
                p.FindPropertyRelative("Source").intValue=1;
                p.FindPropertyRelative("Samples").intValue=1;
                p.FindPropertyRelative("BlurQuality").intValue=0;
                p.FindPropertyRelative("Downsample").boolValue=false;
                p.FindPropertyRelative("Intensity").floatValue=.32f;
                p.FindPropertyRelative("Radius").floatValue=.18f;
                p.FindPropertyRelative("DirectLightingStrength").floatValue=.12f;
                so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(feature);
            }
        }
        Scene originalActive=SceneManager.GetActiveScene();
        foreach(string path in new[]{HomeRoomService.LivingRoomScenePath,HomeRoomService.BathroomScenePath,HomeRoomService.KitchenScenePath,HomeRoomService.BedroomScenePath,HomeRoomService.GardenScenePath,HomeRoomService.BalconyScenePath,HomeRoomService.PatioScenePath,HomeRoomService.SecondFloorScenePath})
        {
            var room=SceneManager.GetSceneByPath(path);bool opened=!room.IsValid()||!room.isLoaded;
            if(opened)room=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            foreach(var root in room.GetRootGameObjects())foreach(var view in root.GetComponentsInChildren<Camera>(true))
            {view.fieldOfView=44.5f;view.transform.rotation=Quaternion.Euler(25,10,0);EditorUtility.SetDirty(view);}
            EditorSceneManager.MarkSceneDirty(room);EditorSceneManager.SaveScene(room);
            if(opened)EditorSceneManager.CloseScene(room,true);
        }
        if(originalActive.IsValid()&&originalActive.isLoaded)SceneManager.SetActiveScene(originalActive);
        var scene=SceneManager.GetSceneByName("LivingRoom_Level01");
        if(!scene.IsValid()||!scene.isLoaded)return;
        Transform presentation=null;Camera camera=null;
        foreach(var root in scene.GetRootGameObjects())
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name=="05 Presentation")presentation=t;
            if(camera==null)camera=root.GetComponentInChildren<Camera>(true);
        }
        if(presentation==null)presentation=scene.GetRootGameObjects()[0].transform;
        var child=presentation.Find("ReferenceSoftFill");
        if(child==null){child=new GameObject("ReferenceSoftFill").transform;child.SetParent(presentation,false);}
        var light=child.GetComponent<Light>();if(light==null)light=child.gameObject.AddComponent<Light>();
        light.type=LightType.Directional;light.color=new Color(1,.96f,.88f);light.intensity=.38f;
        light.shadows=LightShadows.None;light.cullingMask=~(1<<TitleCatShowcase.StageLayer);
        child.rotation=Quaternion.Euler(22,12,0);
        if(camera!=null){camera.fieldOfView=44.5f;camera.transform.rotation=Quaternion.Euler(25,10,0);EditorUtility.SetDirty(camera);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
}
