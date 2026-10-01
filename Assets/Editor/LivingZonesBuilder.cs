using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>REF A room zoning and REF B warm interior presentation.</summary>
public static class LivingZonesBuilder
{
    public const string Qa="Docs/QA/LIVING_ZONES_2026-09-30";
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        CatHomeEditPreview.Clear();
        var room=LivingCompositionBuilder.Room;
        var all=room.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        // Translate the complete care assembly and its external approach
        // anchors together. Bound feeding/contact children retain their poses.
        var care=all.First(t=>t.name=="PairedCareStation");
        var oldCarePosition=care.position;
        var careTurn=LivingRoomReferenceLayout.CareStationRotation*Quaternion.Inverse(care.rotation);
        foreach(var t in all.Where(t=>new[]{"PairedCareStation","FoodBowl","WaterBowl","FoodInteractionPoint","WaterInteractionPoint"}.Contains(t.name))){
            t.SetPositionAndRotation(LivingRoomReferenceLayout.CareStationPosition+careTurn*(t.position-oldCarePosition),careTurn*t.rotation);
            EditorUtility.SetDirty(t);
        }
        foreach(var p in all.Select(t=>t.GetComponent<HomeProductPlacement>()).Where(p=>p!=null))
            if(HomeStoreService.IsLivingRoomCollectionProduct(p.ProductId)&&StoreCatalogAssets.TryGet(p.ProductId,out var d)){
                p.MovableRoot.SetPositionAndRotation(d.DefaultPosition,Quaternion.Euler(0,d.DefaultYaw,0));
                EditorUtility.SetDirty(p.MovableRoot);
            }
        var window=all.Select(t=>t.GetComponent<WindowDayNightController>()).First(w=>w!=null);
        var windowData=new SerializedObject(window);
        foreach(string name in new[]{"dawnLightIntensity","dayLightIntensity","sunsetLightIntensity","nightLightIntensity"})windowData.FindProperty(name).floatValue=0f;
        windowData.FindProperty("sunBeamRenderer").objectReferenceValue=null;
        // An opaque decorative inset replaces the artificial sky. Detach the
        // colour animation so dawn/night cannot restore the blue backdrop.
        var sky=windowData.FindProperty("skyRenderer").objectReferenceValue as Renderer;
        if(sky!=null){sky.sharedMaterial=LivingCompositionBuilder.Material("LC_Cream");sky.SetPropertyBlock(null);EditorUtility.SetDirty(sky);}
        var glass=windowData.FindProperty("glassRenderer").objectReferenceValue as Renderer;
        if(glass!=null){glass.enabled=false;glass.SetPropertyBlock(null);EditorUtility.SetDirty(glass);}
        windowData.FindProperty("skyRenderer").objectReferenceValue=null;
        windowData.FindProperty("glassRenderer").objectReferenceValue=null;
        windowData.ApplyModifiedPropertiesWithoutUndo();
        all.First(t=>t.name=="SunBeam").gameObject.SetActive(false);
        var lights=all.Select(t=>t.GetComponent<Light>()).Where(l=>l!=null).ToArray();
        foreach(var l in lights){
            switch(l.name){
                case "WindowLight":l.enabled=false;l.intensity=0;break;
                case "Directional Light":l.transform.rotation=Quaternion.Euler(62,330,0);l.intensity=1.30f;l.shadowStrength=.45f;break;
                case "CeilingLight":l.transform.position=new Vector3(-.25f,3.52f,-.35f);l.spotAngle=125;l.innerSpotAngle=85;l.range=9;l.intensity=1.4f;l.shadows=LightShadows.None;break;
                case "PremiumWindowBounce":l.transform.position=new Vector3(0,3.20f,-.65f);l.color=new Color(1,.93f,.83f);l.intensity=.45f;break;
                case "PremiumPeachFill":l.intensity=.28f;l.color=new Color(1,.94f,.88f);break;
                case "ReferenceSoftFill":l.transform.rotation=Quaternion.Euler(38,75,0);l.intensity=.50f;l.color=new Color(1,.97f,.91f);break;
            }
            EditorUtility.SetDirty(l);EditorUtility.SetDirty(l.transform);
        }
        // Keep the existing clock/brightness setting, but make interior light
        // the source at every hour. No additional realtime lights are created.
        var controller=all.Select(t=>t.GetComponent<RoomLightingController>()).First(c=>c!=null);
        var data=new SerializedObject(controller);var keys=data.FindProperty("keys");
        for(int i=0;i<keys.arraySize;i++){
            var key=keys.GetArrayElementAtIndex(i);float hour=key.FindPropertyRelative("hour").floatValue;
            float daytime=hour>=9&&hour<=18?1f:hour==6?.5f:0f;
            key.FindPropertyRelative("directionalIntensity").floatValue=Mathf.Lerp(.90f,1.30f,daytime);
            key.FindPropertyRelative("ceilingLightIntensity").floatValue=Mathf.Lerp(1.8f,1.4f,daytime);
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        // Restore the authored viewport before serialization; Edit Mode HUD
        // preview temporarily adapts its rect to the Game View panel.
        var camera=all.Select(t=>t.GetComponent<Camera>()).First(c=>c!=null);
        camera.rect=new Rect(0,.074074075f,1,.9259259f);
        Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(room);EditorSceneManager.SaveScene(room);
        return "Zoned ROOM poses and window-independent interior light saved.";
    }
    public static string Capture(string name)=>LivingCompositionBuilder.Capture("../LIVING_ZONES_2026-09-30/"+name);
}
