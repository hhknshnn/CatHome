using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Physical openings and measured cat contact areas on the built products.</summary>
public static class RoomProductInteractionBuilder
{
    private const string PrefabFolder = "Assets/Art/StoreProducts/Prefabs/";

    public static string UpgradePrefabs()
    {
        int count = 0;
        var report = new System.Text.StringBuilder();
        foreach (var definition in StoreCatalogAssets.PlaceableProducts)
        {
            bool roomProduct = false;
            foreach (var room in HomeRoomService.Rooms)
                roomProduct |= HomeStoreService.IsProductInRoomCollection(room.Id, definition.ProductId);
            if (!roomProduct) continue;
            UpgradePrefab(definition,report);count++;
        }
        System.IO.Directory.CreateDirectory("Temp/FixedRoomAudit");
        System.IO.File.WriteAllText("Temp/FixedRoomAudit/contact-surfaces.txt", report.ToString());
        return count + " room products: geometry colliders and measured contact surfaces.";
    }

    public static void UpgradePrefab(StoreCatalogAsset definition,System.Text.StringBuilder report)
    {
        string path=PrefabFolder+definition.PrefabName+".prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            HomeRoomArrangementBuilder.ApplyProductScale(root, definition);
            if (definition.ProductId == HomeStoreService.BathroomMirrorId || KitchenBedroomArrangementProfile.IsDecoration(definition.ProductId) || OutdoorArrangementProfile.IsDecoration(definition.ProductId))
                foreach (var retired in root.GetComponentsInChildren<CatActivity>(true))
                    UnityEngine.Object.DestroyImmediate(retired);
            ConfigureLivingRoomActivity(root,definition);
            KitchenScatterBuilder.Configure(root,definition);
            if (definition.ProductId == HomeStoreService.BedroomYarnBasketId)
                BedroomPlayRestBuilder.ConfigureYarn(root, root.transform.Find("VisualContent").gameObject);
            if (definition.ProductId == HomeStoreService.BedroomWindowDaybedId)
            {
                BedroomPlayRestBuilder.ConfigureDaybed(root, root.transform.Find("VisualContent").gameObject);
            }
            if(root.GetComponent<RoomProductFeedback>()==null)root.AddComponent<RoomProductFeedback>();
            BathroomActionPartsBuilder.Configure(root);
            ConfigurePhysicalGeometry(root,definition);ConfigureMeasuredPoints(root,definition);
            SinkSipFacingBuilder.Configure(root, definition);
            SitLookFacingBuilder.Configure(root, definition);
            ConfigureContactSurfaces(root,definition,report);OutdoorPolishBuilder.ConfigureProduct(root,definition);ConfigureEntry(root);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    private static void ConfigurePhysicalGeometry(GameObject root, StoreCatalogAsset definition)
    {
        // The catalogue box describes reserved space, not a solid block. Retain
        // it as a trigger for picking, and collide with the actual authored mesh.
        // In particular a parasol has a pole/base; its canopy is not a floor wall.
        foreach (var box in root.GetComponentsInChildren<BoxCollider>(true))
            box.isTrigger = true;
        bool walkOver = definition.HungHeight < .01f && definition.Height <= .12f;
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            string source = AssetDatabase.GetAssetPath(filter.sharedMesh);
            if (filter.sharedMesh == null || !source.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                continue; // Animated water/particles and procedural decoration are not obstacles.
            if (!filter.name.EndsWith("_PremiumModel") && filter.name != "HammockBed" && filter.name != "SwingSeat")
            {
                var oldCollider = filter.GetComponent<MeshCollider>();
                if (oldCollider != null) UnityEngine.Object.DestroyImmediate(oldCollider);
                continue; // Loose paper, rolling books and falling props remain cosmetic.
            }
            var collider = filter.GetComponent<MeshCollider>();
            if (walkOver)
            {
                if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
                continue;
            }
            if (collider == null) collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
            collider.isTrigger = false;
        }
    }

    private struct Triangle
    {
        public Vector3 a, b, c;
        public bool Height(float x, float z, out float y)
        {
            float den = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            if (Mathf.Abs(den) < .000001f) { y = 0f; return false; }
            float u = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / den;
            float v = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / den;
            y = u * a.y + v * b.y + (1f - u - v) * c.y;
            return u >= -.001f && v >= -.001f && u + v <= 1.001f;
        }
    }

    private static void ConfigureEntry(GameObject root)
    {
        foreach (var activity in root.GetComponentsInChildren<CatActivity>(true))
        {
            var data = new SerializedObject(activity);
            if (activity is PaperSpinActivity paper && paper.UsesPaperTears)
            {
                // Entry spacing must never move the measured paw-contact stand.
                // The paper routine walks from this open entrance to SwatPoint.
                activity.EditorConfigureEntry(data.FindProperty("interactionAnchor").objectReferenceValue as Transform);
                continue;
            }
            Transform entry = null;
            foreach (string field in new[] { "floorPoint", "mountPoint", "mouthPoint", "doorPoint",
                         "scratchPoint", "standPoint", "approachPoint", "reachPoint", "swatPoint", "shovePoint", "interactionAnchor" })
            {
                if (activity is OvenWarmthActivity && field == "doorPoint") continue; // Oven door is a look target.
                var property = data.FindProperty(field);
                entry = property != null ? property.objectReferenceValue as Transform : null;
                if (entry != null) break;
            }
            activity.EditorConfigureEntry(entry);
        }
    }

    private static void SetPoint(GameObject root, string field, Vector3 local)
    {
        var activity = root.GetComponent<CatActivity>();
        var property = activity == null ? null : new SerializedObject(activity).FindProperty(field);
        var point = property != null ? property.objectReferenceValue as Transform : null;
        var stamp = root.GetComponent<RoomProductScaleStamp>();
        if (point != null) point.position = root.transform.TransformPoint(local * (stamp != null ? stamp.AppliedScale : 1f));
    }

    private static void ConfigureMeasuredPoints(GameObject root, StoreCatalogAsset d)
    {
        // These corrections come from horizontal triangle profiles of the BUILT
        // FBXs. Slat gaps and a basin centre are not landing surfaces.
        switch (d.PrefabName)
        {
            case "TallHouseplant":
                SetPoint(root, "interactionAnchor", LivingRoomGazeLayoutBuilder.TallPlantEntryLocal);
                break;
            case "BathroomWallMirror":
                // Decoration: no cat action or approach point is configured.
                break;
            case "BathroomToilet":
                // BathroomActionPartsBuilder keeps the real holder, paw stand
                // and open front entrance together after the wall rotation.
                break;
            case "BathroomShower":
                var outlet = root.transform.Find("RinseWaterOutlet");
                if (outlet == null) { outlet = new GameObject("RinseWaterOutlet").transform; outlet.SetParent(root.transform, false); }
                root.GetComponent<ShowerRinseActivity>().EditorConfigureWaterOutlet(outlet);
                // Blender component measurement: 5 mm beneath the actual nozzle tips,
                // converted through the premium model's import scale and 180-degree turn.
                SetPoint(root, "waterOutlet", new Vector3(0, 1.6975694f, .0262041f));
                // Measured front glass occupies root +X; the walk-in half is -X.
                // The original +.33 entry sent the cat through glass and hid the rinse.
                SetPoint(root, "doorPoint", new Vector3(-.33f, 0, .95f));
                SetPoint(root, "standPoint", new Vector3(-.30f, .125f, .05f));
                SetPoint(root, "interactionAnchor", new Vector3(-.33f, 0, 1.05f));
                break;
            case "ClassicArmchair": SetPoint(root,"perchPoint",new Vector3(0,.34f,-.06f));break;
            case "KitchenSinkCabinet": SetPoint(root, "perchPoint", new Vector3(-.47f, .811f, -.10f)); break;
            case "BedroomQueenBed": SetPoint(root, "perchPoint", new Vector3(-.4787234f, .57888f, .0531915f)); break;
            case "BedroomNightstand":
                var point = Point(root, "KnockPerchPoint", new Vector3(-.03f, .55f, 0f));
                root.GetComponent<KnockOffActivity>().EditorConfigurePerch(point);
                break;
            case "BedroomStarCanopy": SetPoint(root, "nestPoint", new Vector3(0f, .195f, -.10f)); break;
            case "KitchenIsland":
                SetPoint(root, "floorPoint", new Vector3(-.30f, 0f, -.92f));
                SetPoint(root, "perchPoint", new Vector3(-.70f,.8256f,-.11f));
                break;
            case "KitchenFeedingStation":
                SetPoint(root, "standPoint", new Vector3(-.50f, 0, .60f));
                // Vertex on the actual front kibble, measured from the premium
                // Cream submesh; the .82 room scale is applied exactly once.
                SetPoint(root, "bowlPoint", new Vector3(.22252827f, .23703012f, .0704091f));
                var meal = root.GetComponent<MealTimeActivity>();
                var mealData = new SerializedObject(meal);
                meal.EditorConfigureMeal(mealData.FindProperty("standPoint").objectReferenceValue as Transform,
                    meal.BowlPoint, 4.3f, 40f, 96f);
                break;
            case "KitchenRefrigerator":
                var fridge = root.GetComponent<SitLookActivity>();
                fridge.EditorConfigureLook(fridge.LookPoint, SitLookReaction.Sit, 2.8f, "OPEN IT!");
                break;
            case "KitchenDishCart":
                SetPoint(root, "shovePoint", new Vector3(.50f, 0f, -.60f));
                SetPoint(root, "interactionAnchor", new Vector3(.50f, 0f, -.60f));
                var cart = root.GetComponent<CartNudgeActivity>();
                var cartData = new SerializedObject(cart);
                cart.EditorConfigureNudge(cartData.FindProperty("shovePoint").objectReferenceValue as Transform,
                    cart.CartVisual, Vector3.left, .24f, 2);
                break;
            case "BathroomTowelStorage": SetPoint(root, "nestPoint", new Vector3(0f, 1.7472f, 0f)); break;
            case "KitchenPantryShelf":
                SetPoint(root, "upperShelfPoint", new Vector3(0f, 1.7696f, 0f));
                root.GetComponent<PantryClimbActivity>().EditorConfigureDirectClimb(true);
                break;
            case "BalconyHerbShelf":
                SetPoint(root, "upperShelfPoint", new Vector3(0f, 1.27f, .05388889f));
                root.GetComponent<PantryClimbActivity>().EditorConfigureDirectClimb(true);
                break;
            case "LoftTallBookcase":
                SetPoint(root, "upperShelfPoint", new Vector3(0f, 2.1f, 0f));
                root.GetComponent<PantryClimbActivity>().EditorConfigureDirectClimb(true);
                break;
            case "GardenPergola":
                SetPoint(root, "upperShelfPoint", new Vector3(0f, 1.7046f, 0f));
                SetPoint(root, "floorPoint", new Vector3(0f, 0f, -1.25f));
                root.GetComponent<PantryClimbActivity>().EditorConfigureDirectClimb(true);
                break;
            case "BalconyHangingChair": SetPoint(root, "nestPoint", new Vector3(0f, .59f, -.53f)); break;
            case "PatioDiningSet": SetPoint(root, "perchPoint", new Vector3(-.44f, .440f, -.08f)); break;
            case "PatioPergolaArch":
                SetPoint(root, "upperShelfPoint", new Vector3(0f, 1.79f, .11f));
                // The authored arch has no intermediate shelf: the old .78
                // point was inside its vine. Jump to the actual header beam.
                root.GetComponent<PantryClimbActivity>().EditorConfigureDirectClimb(true);
                break;
        }
    }

    private static List<Triangle> ReadTriangles(GameObject root)
    {
        var triangles = new List<Triangle>();
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh = filter.sharedMesh;
            if (mesh == null) continue;
            Vector3[] vertices = mesh.vertices;
            int[] indices = mesh.triangles;
            Matrix4x4 matrix = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int i = 0; i < indices.Length; i += 3)
            {
                var triangle = new Triangle {
                    a = matrix.MultiplyPoint3x4(vertices[indices[i]]),
                    b = matrix.MultiplyPoint3x4(vertices[indices[i + 1]]),
                    c = matrix.MultiplyPoint3x4(vertices[indices[i + 2]]) };
                if (Vector3.Cross(triangle.b - triangle.a, triangle.c - triangle.a).normalized.y >= .6f)
                    triangles.Add(triangle); // An underside or steep backrest cannot support the paws.
            }
        }
        return triangles;
    }

    private static bool HeightNear(List<Triangle> triangles, Vector3 point, float tolerance, out float y)
    {
        float nearest = tolerance;
        bool found = false;
        y = point.y;
        foreach (var triangle in triangles)
        {
            if (!triangle.Height(point.x, point.z, out float candidate)) continue;
            float distance = Mathf.Abs(candidate - point.y);
            if (distance <= nearest)
            {
                nearest = distance;
                y = candidate;
                found = true;
            }
        }
        return found;
    }

    private static void ConfigureContactSurfaces(GameObject root, StoreCatalogAsset definition,
        System.Text.StringBuilder report)
    {
        var triangles = ReadTriangles(root);
        foreach (var activity in root.GetComponentsInChildren<CatActivity>(true))
        {
            if(activity is SurfaceScatterActivity)continue; // Supported by its required counter, not the basket mesh.
            var serialized = new SerializedObject(activity);
            foreach (string name in new[] { "seatPoint", "perchPoint", "nestPoint", "pilePoint",
                         "upperShelfPoint", "lowerShelfPoint", "padPoint", "digPoint" })
            {
                if (name == "lowerShelfPoint" && activity is PantryClimbActivity climb && !climb.UsesIntermediatePerch)
                    continue;
                var property = serialized.FindProperty(name);
                var point = property != null ? property.objectReferenceValue as Transform : null;
                if (point == null) continue;
                Vector3 local = root.transform.InverseTransformPoint(point.position);
                if (!HeightNear(triangles, local, .16f, out float measuredY))
                {
                    report.AppendLine(definition.PrefabName + "/" + name + ": NO SUPPORT at " + local);
                    continue;
                }
                local.y = measuredY;
                point.position = root.transform.TransformPoint(local);
                const float step = .035f;
                bool[,] grid = SupportGrid(triangles, local, step);
                int halfX = 1, halfZ = 1;
                // Grow a rectangle only while its entire sampled face has support.
                for (int iteration = 0; iteration < 27; iteration++)
                {
                    if (SupportsRectangle(grid, halfX + 1, halfZ)) halfX++;
                    if (SupportsRectangle(grid, halfX, halfZ + 1)) halfZ++;
                }
                var surface = point.GetComponent<CatActivitySurface>() ?? point.gameObject.AddComponent<CatActivitySurface>();
                surface.EditorConfigure(new Vector2(halfX * step * 2f, halfZ * step * 2f));
                if (definition.PrefabName == "BalconyHerbShelf")
                    surface.EditorConfigurePose(CatActivityPose.GentleKnead, false);
                else if (definition.PrefabName == "BedroomStarCanopy")
                    surface.EditorConfigurePose(CatActivityPose.Sleep, false);
                else if (definition.PrefabName == "KitchenCounterStool" || definition.PrefabName == "BedroomVanityStool")
                    surface.EditorConfigurePose(CatActivityPose.Sit, false);
                else if (name == "upperShelfPoint" || definition.PrefabName == "GardenHammock" ||
                    definition.PrefabName == "BathroomTowelStorage")
                    surface.EditorConfigurePose(CatActivityPose.Sleep, true);
                report.AppendLine(definition.PrefabName + "/" + name + ": " + local.ToString("F3") +
                    " usable=" + surface.Size.ToString("F3"));
            }
        }
    }

    private static bool[,] SupportGrid(List<Triangle> triangles, Vector3 center, float step)
    {
        var grid = new bool[59, 59];
        foreach (var t in triangles)
        {
            if (Mathf.Min(t.a.y, Mathf.Min(t.b.y, t.c.y)) > center.y + .055f ||
                Mathf.Max(t.a.y, Mathf.Max(t.b.y, t.c.y)) < center.y - .055f) continue;
            int left = Mathf.Clamp(Mathf.CeilToInt((Mathf.Min(t.a.x, Mathf.Min(t.b.x, t.c.x)) - center.x) / step) + 29, 0, 58);
            int right = Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(t.a.x, Mathf.Max(t.b.x, t.c.x)) - center.x) / step) + 29, 0, 58);
            int front = Mathf.Clamp(Mathf.CeilToInt((Mathf.Min(t.a.z, Mathf.Min(t.b.z, t.c.z)) - center.z) / step) + 29, 0, 58);
            int back = Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(t.a.z, Mathf.Max(t.b.z, t.c.z)) - center.z) / step) + 29, 0, 58);
            for (int x = left; x <= right; x++)
                for (int z = front; z <= back; z++)
                    if (t.Height(center.x + (x - 29) * step, center.z + (z - 29) * step, out float y) &&
                        Mathf.Abs(y - center.y) <= .055f) grid[x, z] = true;
        }
        return grid;
    }

    private static bool SupportsRectangle(bool[,] grid, int halfX, int halfZ)
    {
        for (int x = 29 - halfX; x <= 29 + halfX; x++)
            for (int z = 29 - halfZ; z <= 29 + halfZ; z++)
                if (!grid[x, z]) return false;
        return true;
    }

    private static Transform Point(GameObject root, string name, Vector3 position)
    {
        var point = root.transform.Find(name);
        if (point == null) point = new GameObject(name).transform;
        point.SetParent(root.transform, false);
        point.localPosition = position;
        return point;
    }

    private static void ConfigureLivingRoomActivity(GameObject root, StoreCatalogAsset d)
    {
        if (d.ProductId == HomeStoreService.GameConsoleId || d.ProductId == HomeStoreService.StereoId || d.ProductId == HomeStoreService.TvUnitId)
        {
            foreach (var retired in root.GetComponents<CatActivity>()) UnityEngine.Object.DestroyImmediate(retired);
            return;
        }
        if (!HomeStoreService.IsLivingRoomCollectionProduct(d.ProductId) || root.GetComponent<CatActivity>() != null)
            return;
        var visual = root.transform.Find("VisualContent");
        if (visual == null) return;
        bool roomAtPlusZ = d.ProductId == HomeStoreService.BookshelfId ||
            d.ProductId == HomeStoreService.ModernPaintingId || d.ProductId == HomeStoreService.TvUnitId ||
            d.ProductId == HomeStoreService.ModernTelevisionId || d.ProductId == HomeStoreService.BookSetId ||
            d.ProductId == HomeStoreService.GameConsoleId || d.ProductId == HomeStoreService.StereoId;
        float direction = roomAtPlusZ ? 1f : -1f;
        Vector3 approach = new Vector3(0f, 0f, direction * (d.Footprint.y * .5f + .42f));
        // The three media activities share a cabinet but keep distinct floor
        // approaches along its open front, outside the cabinet footprint.
        if(d.ProductId==HomeStoreService.GameConsoleId)approach=new Vector3(-.72f,0,.77f);
        if(d.ProductId==HomeStoreService.StereoId)approach=new Vector3(.85f,0,.77f);
        if(d.ProductId==HomeStoreService.ModernTelevisionId)approach=new Vector3(0,0,.96f);
        // The bed has its own approach at z=1.416; watch the picture from the
        // open gallery aisle, not from the bed's sleep button zone.
        if(d.ProductId==HomeStoreService.ModernPaintingId)approach=new Vector3(0,0,2.04f);
        // Watch the lamp from the open aisle in front of the care-side chair.
        // Its old near-base point is occupied by the angled armchair.
        if(d.ProductId==HomeStoreService.FloorLampId)approach=new Vector3(0,0,-1.04f);
        Transform anchor = Point(root, "InteractionAnchor", approach);
        if (d.ProductId == HomeStoreService.ArmchairId)
        {
            var floor = Point(root, "PerchFloorPoint", approach);
            var seat = Point(root, "PerchPoint", new Vector3(0f, .46f, -.08f));
            var activity = root.AddComponent<PerchNapActivity>();
            activity.EditorConfigure("armchair-nap", "ARMCHAIR", CatActivityKind.ArmchairNap,
                QuestType.Sleep, 0, "NAP", 1f, 6f, anchor, null, visual.gameObject);
            activity.EditorConfigureStoreProduct(d.ProductId);
            activity.EditorConfigurePerch(floor, seat, 3.5f, 18f, 94f, "MY COZY CHAIR!");
            return;
        }
        CatActivityKind kind = CatActivityKind.LampWatch;
        string action = "WATCH";
        var reaction = SitLookReaction.Sit;
        if (d.ProductId == HomeStoreService.BookshelfId) kind = CatActivityKind.BookshelfSniff;
        else if (d.ProductId == HomeStoreService.BookSetId) kind = CatActivityKind.BookSetSniff;
        else if (d.ProductId == HomeStoreService.TallPlantId) kind = CatActivityKind.PlantSniff;
        else if (d.ProductId == HomeStoreService.ModernPaintingId) kind = CatActivityKind.PaintingWatch;
        else if (d.ProductId == HomeStoreService.ModernTelevisionId) kind = CatActivityKind.TelevisionWatch;
        else if (d.ProductId == HomeStoreService.GameConsoleId) { kind = CatActivityKind.ConsolePaw; action = "PLAY"; reaction = SitLookReaction.PawSwat; }
        else if (d.ProductId == HomeStoreService.StereoId) kind = CatActivityKind.SpeakerListen;
        var target = Point(root, "LookPoint", new Vector3(0f, Mathf.Min(.65f, d.Height * .5f), 0f));
        var look = root.AddComponent<SitLookActivity>();
        look.EditorConfigure(d.ProductId + "-interaction", d.PrefabName, kind,
            QuestType.WindowWatch, 0, action, 1f, 4f, anchor, null, visual.gameObject);
        look.EditorConfigureStoreProduct(d.ProductId);
        look.EditorConfigureLook(target, reaction, 3f, "PURRR!");
    }
}
