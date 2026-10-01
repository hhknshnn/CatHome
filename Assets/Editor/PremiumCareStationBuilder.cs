using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Collections;
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
        // The base remains a closed footprint; the original tray mesh owns
        // the lip/backplate. Empty air above the tray must not block the neck.
        ApplyMeasuredTrayCollision(trayRoot, tray, trayBody);
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
                // Keep the native Persian touchdown clear of the raised backrest.
                Vector3 support=t.position+new Vector3(0,0,-.145f);
                support.y=LivingRoomArrangementBuilder.MeasureTop(visual,support,.242f);
                point.SetPositionAndRotation(support,Quaternion.Euler(0,90,0));
                ApplyBedSupportCollision(blocker, visual, point);
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
        ApplyMeasuredGapNavigationSeal(scene, trayRoot);
    }
    const string TrayMeshPath = "Assets/Art/PremiumFurniture/Models/CareStationTray_Premium.fbx";

    // The footprint seals the base; the original mesh owns the raised back
    // and side cushions. Their bounding height must not fill the sleeping air.
    static void ApplyBedSupportCollision(BoxCollider body, Transform visual, Transform support)
    {
        var filters=visual.GetComponentsInChildren<MeshFilter>(true);
        if(filters.Length==0||visual.GetComponentInParent<Rigidbody>()!=null)
            throw new InvalidOperationException("Static original bed geometry required.");
        foreach(var filter in filters)
        {
            var solid=filter.GetComponent<MeshCollider>();
            if(filter.sharedMesh==null||solid==null||!solid.enabled||solid.isTrigger||solid.convex||
                solid.sharedMesh!=filter.sharedMesh)
                throw new InvalidOperationException("Keep the original solid bed mesh before reducing its base box.");
        }
        float measured=LivingRoomArrangementBuilder.MeasureTop(visual,support.position,support.position.y);
        if(Mathf.Abs(measured-support.position.y)>.001f||Vector3.Dot(body.transform.up,Vector3.up)<.9999f)
            throw new InvalidOperationException("The bed support must match its upright original surface.");
        float bottom=body.center.y-body.size.y*.5f;
        float top=body.transform.InverseTransformPoint(new Vector3(support.position.x,measured,support.position.z)).y;
        if(top<=bottom)throw new InvalidOperationException("Invalid measured bed base height.");
        Vector3 centre=body.center,size=body.size;
        centre.y=(bottom+top)*.5f;size.y=top-bottom;
        body.center=centre;body.size=size;body.enabled=true;body.isTrigger=false;
        EditorUtility.SetDirty(body);
    }

    public static string ApplyBedCollisionAndSave()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Apply bed collision outside Play Mode.");
        var scene=SceneManager.GetSceneByPath(HomeRoomService.LivingRoomScenePath);
        if(!scene.IsValid()||!scene.isLoaded||scene.isDirty)
            throw new InvalidOperationException("The existing clean living room must be open.");
        var root=Find(scene,"Bed5 V3");
        var obstacle=root!=null?root.GetComponent<CatBedObstacle>():null;
        var visual=root!=null?root.Find("PremiumCareVisual"):null;
        var support=root!=null?root.Find("SleepPoint"):null;
        if(obstacle==null||obstacle.Body==null||obstacle.FrontExit==null||visual==null||support==null||obstacle.Body.transform!=visual)
            throw new InvalidOperationException("Existing bed bindings are incomplete.");
        ApplyBedSupportCollision(obstacle.Body,visual,support);
        Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Bed collider scene save failed.");
        return "Only the existing bed base height changed; original mesh, footprint, support and rescue bindings retained.";
    }

    /// <summary>Migration of only the existing living-room care colliders; no artwork/layout rebuild.</summary>
    public static string ApplyPhysicalCollisionAndSave()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Apply care collision outside Play Mode.");
        var scene = SceneManager.GetSceneByPath(HomeRoomService.LivingRoomScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("Open the existing living-room scene first.");
        var root = Find(scene, "PairedCareStation");
        var tray = root != null ? root.Find("StationVisual") : null;
        var safety = root != null ? root.GetComponent<CatCareStationObstacle>() : null;
        if (tray == null || safety == null || safety.Body == null || safety.FrontExit == null ||
            safety.Body.transform != root)
            throw new InvalidOperationException("The bound care station is incomplete; collision migration stopped.");
        string report = ApplyMeasuredTrayCollision(root, tray, safety.Body);
        report += "\n" + ApplyMeasuredGapNavigationSeal(scene, root);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Living-room collision scene could not be saved.");
        return report;
    }

    static string ApplyMeasuredTrayCollision(Transform root, Transform tray, BoxCollider body)
    {
        var filters = tray.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0 || tray.GetComponentInParent<Rigidbody>() != null)
            throw new InvalidOperationException("Static original care tray mesh required.");
        var localVertices = new System.Collections.Generic.List<Vector3>();
        foreach (var filter in filters)
        {
            var mesh = filter.sharedMesh;
            if (mesh == null || AssetDatabase.GetAssetPath(mesh) != TrayMeshPath)
                throw new InvalidOperationException("Unknown care tray mesh; collision left unchanged.");
            // Editor readback preserves the original imported mesh/readability.
            using (var snapshot = MeshUtility.AcquireReadOnlyMeshData(mesh))
            using (var vertices = new NativeArray<Vector3>(snapshot[0].vertexCount, Allocator.Temp))
            {
                snapshot[0].GetVertices(vertices);
                foreach (Vector3 vertex in vertices)
                    localVertices.Add(root.InverseTransformPoint(filter.transform.TransformPoint(vertex)));
            }
        }
        if (localVertices.Count == 0) throw new InvalidOperationException("Empty care tray mesh.");
        var bounds = new Bounds(localVertices[0], Vector3.zero);
        foreach (var point in localVertices) bounds.Encapsulate(point);
        // Only the front half contains the closed base and inset; the separate
        // rear backplate, gold lip and badges are all in local positive Z.
        float floorTop = float.NegativeInfinity;
        foreach (var point in localVertices)
            if (point.z <= bounds.center.z) floorTop = Mathf.Max(floorTop, point.y);
        if (Mathf.Abs(bounds.size.x - 1.40f) > .001f || Mathf.Abs(bounds.size.z - .55f) > .001f ||
            Mathf.Abs(bounds.min.y) > .001f || Mathf.Abs(bounds.max.y - .170f) > .001f ||
            Mathf.Abs(floorTop - .028f) > .001f)
            throw new InvalidOperationException("Care tray shape differs from the measured source; collision left unchanged.");
        var boxes = tray.GetComponentsInChildren<BoxCollider>(true);
        foreach (var box in boxes)
            if (box.transform != tray || Vector3.Distance(box.center, new Vector3(0, .085f, .255f)) > .001f ||
                Vector3.Distance(box.size, new Vector3(1.40f, .17f, .04f)) > .001f)
                throw new InvalidOperationException("Unknown care tray box; collision left unchanged.");

        body.center = new Vector3(bounds.center.x, (bounds.min.y + floorTop) * .5f, bounds.center.z);
        body.size = new Vector3(bounds.size.x, floorTop - bounds.min.y, bounds.size.z);
        body.isTrigger = false; body.enabled = true;
        EditorUtility.SetDirty(body);
        foreach (var box in boxes) { box.enabled = false; EditorUtility.SetDirty(box); }
        foreach (var filter in filters)
        {
            var collider = filter.GetComponent<MeshCollider>();
            if (collider == null) collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh; collider.convex = false;
            collider.isTrigger = false; collider.enabled = true;
            EditorUtility.SetDirty(collider);
            if (PrefabUtility.IsPartOfPrefabInstance(collider)) PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        }
        // Body and FrontExit references remain identical, retaining old-save rescue.
        return FormattableString.Invariant($"Living care only: base {bounds.size.x:F3} x {floorTop-bounds.min.y:F3} x {bounds.size.z:F3} m; {filters.Length} original static tray mesh collider(s); saved-pose rescue unchanged. Pocket regression still requires native validation.");
    }

    const string GapSealName = "CareGapNavigationSeal";

    // A declared navigation seam in the non-walkable space between the bowls,
    // not a physical enclosure of all the empty air above the care tray. Its
    // 2 cm width splits the measured gap into two passages narrower than the
    // existing controller; the food/water approach volumes remain open.
    static string ApplyMeasuredGapNavigationSeal(Scene scene, Transform station)
    {
        var food = Find(scene, "FoodBowl");
        var water = Find(scene, "WaterBowl");
        var safety = station.GetComponent<CatCareStationObstacle>();
        if (food == null || water == null || safety == null || safety.Body == null)
            throw new InvalidOperationException("Bound bowls and tray required for care gap navigation seam.");
        Bounds foodBounds = OriginalBowlBounds(food, station, "MainFoodBowl");
        Bounds waterBounds = OriginalBowlBounds(water, station, "MainWaterBowl");
        Bounds left = foodBounds.center.x < waterBounds.center.x ? foodBounds : waterBounds;
        Bounds right = foodBounds.center.x < waterBounds.center.x ? waterBounds : foodBounds;
        float gapMinimum = left.max.x, gapMaximum = right.min.x;
        float width = gapMaximum - gapMinimum;
        const float seamWidth = .020f;
        const float navigationTop = .200f;
        var baseBox = safety.Body;
        Vector3 low = baseBox.center - baseBox.size * .5f, high = baseBox.center + baseBox.size * .5f;
        // These are source-layout guards, not a request to widen/narrow a
        // controller. An unknown bowl spacing must be reviewed independently.
        if (width < .45f || width > .55f || gapMinimum < low.x || gapMaximum > high.x ||
            high.y < .02f || high.y > .04f || high.z - low.z < .50f)
            throw new InvalidOperationException("Care gap differs from the measured layout; navigation seam stopped.");
        Transform child = station.Find(GapSealName);
        if (child != null && (child.childCount != 0 || child.GetComponents<Component>().Length > 2 ||
            child.GetComponent<BoxCollider>() == null))
            throw new InvalidOperationException("Unexpected care navigation seam object; left unchanged.");
        if (child == null)
        {
            child = new GameObject(GapSealName).transform;
            child.SetParent(station, false);
            child.gameObject.layer = station.gameObject.layer;
        }
        child.localPosition = Vector3.zero; child.localRotation = Quaternion.identity; child.localScale = Vector3.one;
        var collider = child.GetComponent<BoxCollider>();
        if (collider == null) collider = child.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3((gapMinimum + gapMaximum) * .5f, (high.y + navigationTop) * .5f, baseBox.center.z);
        collider.size = new Vector3(seamWidth, navigationTop - high.y, baseBox.size.z);
        collider.enabled = true; collider.isTrigger = false;
        EditorUtility.SetDirty(child); EditorUtility.SetDirty(collider);
        return FormattableString.Invariant($"Explicit care-gap navigation seam: gap [{gapMinimum:F6},{gapMaximum:F6}] m; width {seamWidth:F3} m; each remaining gap {(width-seamWidth)*.5f:F6} m; top {navigationTop:F3} m. Real tray/bowl meshes retained.");
    }

    static Bounds OriginalBowlBounds(Transform bowl, Transform station, string model)
    {
        var visual = bowl.Find("PremiumCareVisual");
        if (visual == null) throw new InvalidOperationException("Missing original bowl visual: " + model);
        bool initialized = false; Bounds bounds = default;
        foreach (var filter in visual.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh = filter.sharedMesh;
            if (mesh == null || AssetDatabase.GetAssetPath(mesh) != "Assets/Art/PremiumFurniture/Models/" + model + "_Premium.fbx")
                throw new InvalidOperationException("Unknown bowl mesh: " + model);
            using (var snapshot = MeshUtility.AcquireReadOnlyMeshData(mesh))
            using (var vertices = new NativeArray<Vector3>(snapshot[0].vertexCount, Allocator.Temp))
            {
                snapshot[0].GetVertices(vertices);
                foreach (Vector3 vertex in vertices)
                {
                    Vector3 point = station.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    if (!initialized) { bounds = new Bounds(point, Vector3.zero); initialized = true; }
                    else bounds.Encapsulate(point);
                }
            }
        }
        if (!initialized) throw new InvalidOperationException("No measured bowl vertices: " + model);
        return bounds;
    }

    static void ConfigureBowlContact(Scene scene, Transform bowl, Transform contents, bool food)
    {
        // Side view from the player camera, with all paws on the open floor.
        var offset = LivingRoomReferenceLayout.CareStationRotation * new Vector3(.1183331f,0,-.2537662f);
        var pose=bowl.Find("FeedingPoint");if(pose==null){pose=new GameObject("FeedingPoint").transform;pose.SetParent(bowl,false);}
        var standing=bowl.position+offset;standing.y=0f;
        pose.SetPositionAndRotation(standing,Quaternion.LookRotation(-offset));
        var contact=bowl.Find("MouthContact");if(contact==null){contact=new GameObject("MouthContact").transform;contact.SetParent(bowl,false);}
        // The front edible surface is reachable from the open floor without
        // asking the walking body to enter the solid bowl or tray.
        Vector3 desired=bowl.position+LivingRoomReferenceLayout.CareStationRotation*new Vector3(0,0,-.085f);
        desired.y=bowl.position.y+.105f;
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
