using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Replace care artwork while retaining the bound bowl/content/sleep objects.</summary>
public static class PremiumCareStationBuilder
{
    public static void Apply(Scene scene)
    {
        if(scene.path!=HomeRoomService.LivingRoomScenePath)return;
        LivingRoomReferenceLayout.ApplyCareLayout(scene);
        var trayRoot=Find(scene,"PairedCareStation");
        if(trayRoot==null){trayRoot=new GameObject("PairedCareStation").transform;SceneManager.MoveGameObjectToScene(trayRoot.gameObject,scene);}
        trayRoot.SetPositionAndRotation(LivingRoomReferenceLayout.CareStationPosition,LivingRoomReferenceLayout.CareStationRotation);
        var tray=Install(trayRoot,"StationVisual","CareStationTray",trayRoot.position);
        tray.rotation=LivingRoomReferenceLayout.CareStationRotation;
        tray.localScale=Vector3.one;
        tray.localPosition=Vector3.zero;
        var trayBody=trayRoot.GetComponent<BoxCollider>();if(trayBody==null)trayBody=trayRoot.gameObject.AddComponent<BoxCollider>();
        // Seal the pockets within the existing bowl/backplate assembly. A
        // tray-height collider alone lets the rounded controller foot climb
        // the six-centimetre lip and enter the gap between the two bowls.
        trayBody.center=new Vector3(0,.10f,0);trayBody.size=new Vector3(1.40f,.20f,.55f);trayBody.isTrigger=false;
        var back=tray.gameObject.AddComponent<BoxCollider>();back.center=new Vector3(0,.085f,.255f);back.size=new Vector3(1.40f,.17f,.04f);
        var exit=trayRoot.Find("OpenFront");if(exit==null){exit=new GameObject("OpenFront").transform;exit.SetParent(trayRoot,false);}
        exit.SetPositionAndRotation(LivingRoomReferenceLayout.CarePoint(new Vector3(-.0116669f,.05f,-.7737662f)),LivingRoomReferenceLayout.CareStationRotation);
        var safety=trayRoot.GetComponent<CatCareStationObstacle>();if(safety==null)safety=trayRoot.gameObject.AddComponent<CatCareStationObstacle>();
        safety.EditorConfigure(trayBody,exit);EditorUtility.SetDirty(safety);
        foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            if(t==null)continue;
            string model=t.name=="FoodBowl"?"MainFoodBowl":t.name=="WaterBowl"?"MainWaterBowl":t.name=="Bed5 V3"?"MainCatBed":null;
            if(model==null)continue;
            // Old renderers remain disabled so serialized references and fill events survive.
            foreach(var renderer in t.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            foreach(var collider in t.GetComponentsInChildren<Collider>(true))if(!collider.isTrigger)collider.enabled=false;
            var visual=Install(t,"PremiumCareVisual",model,t.position);
            if(model!="MainCatBed")visual.rotation=LivingRoomReferenceLayout.CareStationRotation;
            foreach(var filter in visual.GetComponentsInChildren<MeshFilter>(true))
            {var c=filter.GetComponent<MeshCollider>();if(c==null)c=filter.gameObject.AddComponent<MeshCollider>();c.sharedMesh=filter.sharedMesh;}
            if(model=="MainCatBed")
            {
                // Legacy Bed5 has a 180-degree root rotation. The installed
                // visual faces world -Z, so measure in that visual's frame.
                var bounds=CatProductContentBuilder.LocalBounds(visual.gameObject,visual);
                float wallTrim=HomeRoomShellMetrics.WainscotThickness;
                t.position=new Vector3(t.position.x,0,HomeRoomShellMetrics.InteriorMaxZ-wallTrim-bounds.max.z);
                var blocker=visual.GetComponent<BoxCollider>();if(blocker==null)blocker=visual.gameObject.AddComponent<BoxCollider>();
                blocker.isTrigger=false;blocker.enabled=true;
                // The visible back touches the panelling; collision also seals
                // the recessed 8 cm between panelling and structural wall.
                blocker.center=new Vector3(bounds.center.x,bounds.max.y*.5f,bounds.center.z+wallTrim*.5f);
                blocker.size=new Vector3(bounds.size.x,bounds.max.y,bounds.size.z+wallTrim);
                var point=t.Find("SleepPoint");
                if(point==null)throw new InvalidOperationException("Missing bound SleepPoint");
                Vector3 support=t.position+new Vector3(0,0,-.065f);
                support.y=LivingRoomArrangementBuilder.MeasureTop(visual,support,.242f);
                point.SetPositionAndRotation(support,Quaternion.Euler(0,90,0));
                var surface=point.GetComponent<CatActivitySurface>();if(surface==null)surface=point.gameObject.AddComponent<CatActivitySurface>();
                surface.EditorConfigure(new Vector2(.56f,.86f));
                Transform entry=null;
                foreach(var sceneRoot in scene.GetRootGameObjects())foreach(var candidate in sceneRoot.GetComponentsInChildren<Transform>(true))
                    if(candidate.name=="BedInteractionPoint")entry=candidate;
                if(entry==null)throw new InvalidOperationException("Missing bound BedInteractionPoint");
                entry.SetPositionAndRotation(t.position+new Vector3(0,.05f,bounds.min.z-.50f),Quaternion.Euler(0,180,0));
                var bedSafety=t.GetComponent<CatBedObstacle>();if(bedSafety==null)bedSafety=t.gameObject.AddComponent<CatBedObstacle>();
                bedSafety.EditorConfigure(blocker,entry);
                EditorUtility.SetDirty(bedSafety);EditorUtility.SetDirty(surface);
            }
            else
            {
                var content=t.Find(model=="MainFoodBowl"?"FoodContent":"WaterContent");
                if(content==null)throw new InvalidOperationException("Missing bound care content on "+t.name);
                var contents=Install(content,"PremiumContents",model+"Content",t.position);
                contents.rotation=LivingRoomReferenceLayout.CareStationRotation;
                // Fill to just below the rim: contact must not bury the head.
                contents.position+=Vector3.up*(model=="MainFoodBowl"?.030f:.028f);
                ConfigureBowlContact(scene, t, contents, model=="MainFoodBowl");
            }
        }
    }
    static void ConfigureBowlContact(Scene scene, Transform bowl, Transform contents, bool food)
    {
        // Side view from the player camera, with all paws on the open floor.
        var offset = LivingRoomReferenceLayout.CareStationRotation * new Vector3(.1183331f,0,-.2537662f);
        var pose=bowl.Find("FeedingPoint");if(pose==null){pose=new GameObject("FeedingPoint").transform;pose.SetParent(bowl,false);}
        var standing=bowl.position+offset;standing.y=0f;
        pose.SetPositionAndRotation(standing,Quaternion.LookRotation(-offset));
        var contact=bowl.Find("MouthContact");if(contact==null){contact=new GameObject("MouthContact").transform;contact.SetParent(bowl,false);}
        Vector3 desired=bowl.position;desired.y=bowl.position.y+.105f;
        Vector3 best=Vector3.zero;float distance=float.PositiveInfinity;
        foreach(var filter in contents.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh=filter.sharedMesh;var vertices=mesh.vertices;var triangles=mesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                Vector3 a=filter.transform.TransformPoint(vertices[triangles[i]]),b=filter.transform.TransformPoint(vertices[triangles[i+1]]),c=filter.transform.TransformPoint(vertices[triangles[i+2]]);
                // Kibble has sloped facets rather than a flat top. Any upward
                // facet is a real reachable surface; vertical undersides are not.
                // Do not Normalize the tiny kibble triangles: Unity's vector
                // epsilon can collapse their valid area vectors to zero.
                if(Vector3.Cross(b-a,c-a).y<=0f)continue;
                Vector3 point=food?(a+b+c)/3f:PointOnSurfaceTriangle(a,b,c,desired);
                float square=(point-desired).sqrMagnitude;
                // Symmetric water triangles can be equally close. Translation
                // rounding must not switch the mouth to the opposite triangle.
                // Keep a stable side in the station's frame for equal distances.
                Vector3 stationRight=LivingRoomReferenceLayout.CareStationRotation*Vector3.right;
                if(square<distance-.0000001f ||
                    (Mathf.Abs(square-distance)<=.0000001f && Vector3.Dot(point-best,stationRight)>.00001f))
                {distance=square;best=point;}
            }
        }
        if(distance>.0036f)throw new InvalidOperationException("No actual food/water contact surface on "+bowl.name);
        contact.position=best;
        foreach(var root in scene.GetRootGameObjects())foreach(var interaction in root.GetComponentsInChildren<BowlInteraction>(true))
        {
            var serialized=new SerializedObject(interaction);var setup=serialized.FindProperty(food?"food":"water");
            if(setup.FindPropertyRelative("bowl").objectReferenceValue!=bowl)continue;
            setup.FindPropertyRelative("feedingPoint").objectReferenceValue=pose;
            setup.FindPropertyRelative("contactPoint").objectReferenceValue=contact;
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(interaction);
        }
    }
    static Vector3 PointOnSurfaceTriangle(Vector3 a,Vector3 b,Vector3 c,Vector3 desired)
    {
        // The water contact is on the actual surface under the muzzle, not
        // an arbitrary triangle centroid offset sideways from the cat.
        Vector3 ab=b-a,ac=c-a,n=Vector3.Cross(ab,ac);
        float normalSquare=n.sqrMagnitude;if(normalSquare<1e-14f)return (a+b+c)/3f;
        Vector3 point=desired-n*(Vector3.Dot(desired-a,n)/normalSquare),ap=point-a;
        float aa=Vector3.Dot(ab,ab),bb=Vector3.Dot(ac,ac),cross=Vector3.Dot(ab,ac);
        float denominator=aa*bb-cross*cross;if(Mathf.Abs(denominator)<1e-14f)return (a+b+c)/3f;
        float u=(bb*Vector3.Dot(ap,ab)-cross*Vector3.Dot(ap,ac))/denominator;
        float v=(aa*Vector3.Dot(ap,ac)-cross*Vector3.Dot(ap,ab))/denominator;
        return u>=-.00001f&&v>=-.00001f&&u+v<=1.00001f?point:(a+b+c)/3f;
    }
    static Transform Find(Scene scene,string name)
    {foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
    static Transform Install(Transform parent,string name,string model,Vector3 position)
    {
        var old=parent.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PremiumFurniture/Models/"+model+"_Premium.fbx");
        if(source==null)throw new InvalidOperationException("Missing care model "+model);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(source,parent);instance.name=name;
        instance.transform.SetPositionAndRotation(position,Quaternion.identity);
        var scale=parent.lossyScale;instance.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
        foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/StoreProducts/Materials/"+materials[i].name+".mat");if(material!=null)materials[i]=material;}
            renderer.sharedMaterials=materials;renderer.enabled=true;
        }
        ModernWorldArtBuilder.ApplyRoot(instance.transform,HomeRoomService.LivingRoomId);
        return instance.transform;
    }
}
