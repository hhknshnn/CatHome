using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Explicit authoring roles for fixed room geometry. Floors, foliage, loose
/// props, decorative finials and catalog products do not belong to this pass.
/// A solid box uses its own LOCAL mesh bounds; no aggregate renderer bounds,
/// extra padding, root scaling, or character-controller changes are involved.
/// </summary>
public static class HomeEnvironmentCollisionBuilder
{
    public enum SolidRole { Wall, Rail, Post, FixedPlanter }

    public sealed class Report
    {
        public string scene;
        public int inspected, changes, unchanged;
        public readonly List<string> entries = new List<string>();
        public override string ToString() => scene + ": " + inspected + " solid parts; " +
            changes + " changes; " + unchanged + " already correct.\n" + string.Join("\n", entries);
    }

    /// <summary>New room builders opt into this standard at the geometry call site.</summary>
    public static GameObject CreateSolidBlock(string name, Transform parent, Vector3 position,
        Vector3 scale, Material material, SolidRole role)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;
        EnsureSolidBox(block, role);
        return block;
    }

    /// <summary>
    /// Idempotent primitive-solid contract. Arbitrary shapes must retain their
    /// authored shape colliders instead of silently becoming bounding boxes.
    /// </summary>
    public static bool EnsureSolidBox(GameObject geometry, SolidRole role)
    {
        string issue = ValidateSolidBox(geometry);
        if (issue != null) throw new ArgumentException(issue, nameof(geometry));
        if (!Enum.IsDefined(typeof(SolidRole), role)) throw new ArgumentOutOfRangeException(nameof(role));
        var bounds = geometry.GetComponent<MeshFilter>().sharedMesh.bounds;
        var box = geometry.GetComponent<BoxCollider>();
        if (IsCorrect(box, bounds)) return false;
        if (box == null) box = geometry.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = bounds.size;
        box.isTrigger = false;
        box.enabled = true;
        EditorUtility.SetDirty(box);
        return true;
    }

    const string WallPanelMeshPath = "Assets/Art/RoomShellPolish/Models/RoomWallPanel_Premium.fbx";

    /// <summary>Only the verified architectural wall-panel asset; every collider uses its actual mesh.</summary>
    public static void EnsurePremiumWallPanel(Transform panel)
    {
        var result = new Report();
        PremiumWallPanel(panel, result, true);
        foreach (string entry in result.entries)
            if (entry.StartsWith("UNRECOGNIZED", StringComparison.Ordinal))
                throw new InvalidOperationException(entry);
    }

    /// <summary>Shared by the living-room authoring pass and the scene migration.</summary>
    public static void EnsureLivingWallCladding(Transform wainscot)
    {
        var result = new Report();
        LivingWallCladding(wainscot, result, true);
        foreach (string entry in result.entries)
            if (entry.StartsWith("UNRECOGNIZED", StringComparison.Ordinal) || entry.StartsWith("MISSING", StringComparison.Ordinal))
                throw new InvalidOperationException(entry);
    }

    public static Report Inspect(Scene scene) => Process(scene, false);
    /// <summary>Changes only the loaded scene; caller owns saving. Never rebuilds a room.</summary>
    public static Report Apply(Scene scene) => Process(scene, true);

    [MenuItem("Tools/Cat Home/Rooms/Environment Collision/Inspect All Rooms")]
    public static void InspectFromMenu() => Debug.Log(InspectAllRooms());
    public static string InspectAllRooms() => ProcessAll(false);
    /// <summary>For an explicitly requested migration after inspection; saves changed scenes only.</summary>
    public static string MigrateAllRooms() => ProcessAll(true);

    static string ProcessAll(bool apply)
    {
        RequireEditMode();
        // Do not accidentally save an unrelated edit already present in an open room.
        if (apply)
            foreach (var room in HomeRoomService.Rooms)
            {
                var loaded = SceneManager.GetSceneByPath(room.ScenePath);
                if (loaded.IsValid() && loaded.isLoaded && loaded.isDirty)
                    throw new InvalidOperationException("Room already has unsaved changes: " + room.ScenePath);
            }
        var report = new StringBuilder();
        var active = SceneManager.GetActiveScene();
        try
        {
            foreach (var room in HomeRoomService.Rooms)
            {
                var scene = SceneManager.GetSceneByPath(room.ScenePath);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(room.ScenePath, OpenSceneMode.Additive);
                try
                {
                    var result = Process(scene, apply);
                    report.AppendLine(result.ToString());
                    if (apply && result.changes > 0 && !EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException("Could not save " + scene.path);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
        }
        finally { if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
        return report.ToString();
    }

    static Report Process(Scene scene, bool apply)
    {
        RequireEditMode();
        if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("A loaded room is required.");
        var result = new Report { scene = scene.path };
        Transform environment = null;
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "01 Environment")
            {
                if (environment != null) throw new InvalidOperationException("Ambiguous environment root: " + scene.path);
                environment = root.transform;
            }
        if (environment == null) { result.entries.Add("UNRECOGNIZED: no authoring environment root; unchanged."); return result; }

        WallCladding(scene, environment, result, apply);

        if (scene.path == HomeRoomService.PatioScenePath)
        {
            Runs(environment, "Patio Low Wall", new[] { "FrontLeftWall", "FrontRightWall", "BackLeftWall", "BackRightWall", "LeftWall_Stone", "RightWall_Stone" },
                node => node == "WallBody" || node == "WallCap" ? SolidRole.Wall : Indexed(node, "Pilaster_") ? SolidRole.Post : (SolidRole?)null,
                node => Indexed(node, "Urn_"), result, apply);
            Runs(environment, "Patio Planters", new[] { "CornerPot_1", "CornerPot_2" },
                node => node == "Pot" ? SolidRole.FixedPlanter : (SolidRole?)null,
                node => node == "Bush" || node == "Bloom", result, apply);
        }
        else if (scene.path == HomeRoomService.GardenScenePath)
        {
            Runs(environment, "Sunny Garden Fence", new[] { "BackLeftFence", "BackRightFence", "LeftFence", "RightFence", "FrontLeftFence", "FrontRightFence" },
                node => node == "RailLow" || node == "RailHigh" ? SolidRole.Rail : Indexed(node, "Post_") ? SolidRole.Post : (SolidRole?)null,
                node => node == "Hedge" || Indexed(node, "PostCap_"), result, apply);
            Parts(environment.Find("Garden Gate"), node => node == "GateLeft" || node == "GateRight" ? SolidRole.Post : (SolidRole?)null,
                node => node == "GateArch", "Garden Gate", result, apply);
        }
        else if (scene.path == HomeRoomService.BalconyScenePath)
        {
            Runs(environment, "Deck Railing", new[] { "FrontRail", "LeftRail", "RightRail" },
                node => node == "TopRail" || node == "MidRail" ? SolidRole.Rail : Indexed(node, "Post_") || Indexed(node, "Baluster_") ? SolidRole.Post : (SolidRole?)null,
                node => false, result, apply);
            Runs(environment, "Deck Greens", new[] { "PlanterPot_1", "PlanterPot_2" },
                node => node == "Pot" ? SolidRole.FixedPlanter : (SolidRole?)null,
                node => node == "Foliage" || node == "FoliageHigh", result, apply);
        }
        else if (scene.path == HomeRoomService.SecondFloorScenePath)
        {
            Parts(environment.Find("Loft Stair Landing"),
                node => node == "HandRail" ? SolidRole.Rail : node == "Newel" || Indexed(node, "Baluster_") ? SolidRole.Post : (SolidRole?)null,
                node => node == "NewelCap" || node == "LandingPlant", "Loft Stair Landing", result, apply);
        }
        else if (scene.path != HomeRoomService.LivingRoomScenePath && scene.path != HomeRoomService.BathroomScenePath &&
                 scene.path != HomeRoomService.KitchenScenePath && scene.path != HomeRoomService.BedroomScenePath)
            result.entries.Add("NO MIGRATION PROFILE: existing shell/product colliders preserved; other geometry requires an explicit authoring role.");
        if (apply && result.changes > 0) EditorSceneManager.MarkSceneDirty(scene);
        return result;
    }

    static void WallCladding(Scene scene, Transform environment, Report result, bool apply)
    {
        string group = null;
        string[] boxes = null;
        if (scene.path == HomeRoomService.BathroomScenePath)
        { group = "Glossy Walls"; boxes = new[] { "BackWall_AquaWainscot", "LeftWall_MintWainscot", "RightWall_LilacWainscot" }; }
        else if (scene.path == HomeRoomService.KitchenScenePath)
        { group = "Candy Kitchen Walls"; boxes = new[] { "BackWall_AquaBacksplash", "LeftWall_MintWainscot", "RightWall_CoralWainscot" }; }
        else if (scene.path == HomeRoomService.BedroomScenePath)
        { group = "Dreamy Bedroom Walls"; boxes = new[] { "BackWall_LilacWainscot", "LeftWall_PeachWainscot", "RightWall_MintWainscot" }; }
        else if (scene.path == HomeRoomService.SecondFloorScenePath)
        { group = "Loft Walls"; boxes = new[] { "WainscotBack" }; }

        if (group != null)
        {
            foreach (string name in boxes)
                BoxPart(environment.Find(group + "/" + name), SolidRole.Wall, group + "/" + name, result, apply);
            var finish = environment.Find("PremiumRoomFinish");
            if (finish == null) result.entries.Add("MISSING authoring group: PremiumRoomFinish");
            else
                foreach (Transform model in finish)
                    if (model.name == "RoomWallPanel_Premium") PremiumWallPanel(model, result, apply);
                    else if (model.name.StartsWith("RoomWallPanel", StringComparison.Ordinal))
                        result.entries.Add("UNRECOGNIZED wall panel unchanged: " + Path(model));
        }
        else if (scene.path == HomeRoomService.LivingRoomScenePath)
        {
            Transform wainscot = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "05 Presentation")
                    wainscot = root.transform.Find("PremiumWorldPresentation/CandyRoomArchitecture/RoundedWainscot");
            LivingWallCladding(wainscot, result, apply);
        }
    }

    static void LivingWallCladding(Transform wainscot, Report result, bool apply)
    {
        if (wainscot == null) { result.entries.Add("MISSING authoring group: RoundedWainscot"); return; }
        var known = new HashSet<string>(StringComparer.Ordinal);
        foreach (string side in new[] { "Back", "Left", "Right" })
        {
            known.Add(side + "_Base");
            known.Add(side + "_ChairRail");
            known.Add(side + "_ChairRailGold"); // upper decorative trim remains nonblocking
            BoxPart(wainscot.Find(side + "_Base"), SolidRole.Wall, side + "_Base", result, apply);
            for (int index = 1; index <= 4; index++)
            {
                string name = side + "Panel_" + index;
                known.Add(name + "_Inset"); known.Add(name + "_Frame");
                BoxPart(wainscot.Find(name + "_Inset"), SolidRole.Wall, name + "_Inset", result, apply);
                var frame = wainscot.Find(name + "_Frame");
                if (frame == null) { result.entries.Add("MISSING wall-panel frame: " + name); continue; }
                var frameParts = new HashSet<string>(new[] { "Top", "Bottom", "Left", "Right" }, StringComparer.Ordinal);
                foreach (Transform part in frame)
                    if (frameParts.Remove(part.name)) BoxPart(part, SolidRole.Wall, Path(part), result, apply);
                    else result.entries.Add("UNRECOGNIZED wall-frame part unchanged: " + Path(part));
                foreach (string missing in frameParts) result.entries.Add("MISSING wall-frame part: " + name + "/" + missing);
            }
        }
        foreach (Transform child in wainscot)
            if (!known.Contains(child.name)) result.entries.Add("UNRECOGNIZED wall-cladding part unchanged: " + Path(child));
    }

    static void PremiumWallPanel(Transform panel, Report result, bool apply)
    {
        if (panel == null || panel.name != "RoomWallPanel_Premium")
        { result.entries.Add("UNRECOGNIZED architectural wall-panel root; unchanged."); return; }
        var filters = panel.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0) result.entries.Add("UNRECOGNIZED wall panel without mesh: " + Path(panel));
        foreach (var filter in filters)
        {
            var mesh = filter.sharedMesh;
            var colliders = filter.GetComponents<Collider>();
            if (mesh == null || AssetDatabase.GetAssetPath(mesh) != WallPanelMeshPath ||
                filter.GetComponentInParent<HomeProductPlacement>() != null || filter.GetComponentInParent<StoreProductDisplay>() != null ||
                filter.GetComponentInParent<Rigidbody>() != null || filter.GetComponentInParent<CatActivity>() != null ||
                colliders.Length > 1 || (colliders.Length == 1 && !(colliders[0] is MeshCollider)))
            { result.entries.Add("UNRECOGNIZED wall mesh/collider policy unchanged: " + Path(filter.transform)); continue; }
            result.inspected++;
            var collider = filter.GetComponent<MeshCollider>();
            if (collider != null && collider.enabled && !collider.isTrigger && !collider.convex && collider.sharedMesh == mesh)
            { result.unchanged++; continue; }
            result.changes++;
            result.entries.Add((apply ? "APPLIED " : "NEEDS ") + "Wall actual mesh: " + Path(filter.transform));
            if (!apply) continue;
            if (collider == null) collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
            collider.isTrigger = false;
            collider.enabled = true;
            EditorUtility.SetDirty(collider);
            if (PrefabUtility.IsPartOfPrefabInstance(collider)) PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        }
    }

    static void BoxPart(Transform child, SolidRole role, string label, Report result, bool apply)
    {
        if (child == null) { result.entries.Add("MISSING solid part: " + label); return; }
        string issue = ValidateSolidBox(child.gameObject);
        if (issue != null) { result.entries.Add("UNRECOGNIZED unchanged: " + Path(child) + " / " + issue); return; }
        result.inspected++;
        var bounds = child.GetComponent<MeshFilter>().sharedMesh.bounds;
        if (IsCorrect(child.GetComponent<BoxCollider>(), bounds)) { result.unchanged++; return; }
        result.changes++;
        result.entries.Add((apply ? "APPLIED " : "NEEDS ") + role + ": " + Path(child));
        if (apply) EnsureSolidBox(child.gameObject, role);
    }

    static void Runs(Transform environment, string groupName, string[] runNames,
        Func<string, SolidRole?> role, Func<string, bool> decorative, Report result, bool apply)
    {
        var group = environment.Find(groupName);
        if (group == null) { result.entries.Add("MISSING authoring group: " + groupName); return; }
        var expected = new HashSet<string>(runNames, StringComparer.Ordinal);
        foreach (Transform run in group)
        {
            if (!expected.Remove(run.name)) { result.entries.Add("UNRECOGNIZED unchanged: " + Path(run)); continue; }
            Parts(run, role, decorative, Path(run), result, apply);
        }
        foreach (string missing in expected) result.entries.Add("MISSING authoring run: " + groupName + "/" + missing);
    }

    static void Parts(Transform parent, Func<string, SolidRole?> role, Func<string, bool> decorative,
        string label, Report result, bool apply)
    {
        if (parent == null) { result.entries.Add("MISSING authoring group: " + label); return; }
        foreach (Transform child in parent)
        {
            var assignedRole = role(child.name);
            if (!assignedRole.HasValue)
            {
                if (!decorative(child.name)) result.entries.Add("UNRECOGNIZED unchanged: " + Path(child));
                continue;
            }
            BoxPart(child, assignedRole.Value, Path(child), result, apply);
        }
    }

    static string ValidateSolidBox(GameObject geometry)
    {
        if (geometry == null) return "Missing geometry.";
        if (geometry.GetComponentInParent<HomeProductPlacement>() != null || geometry.GetComponentInParent<StoreProductDisplay>() != null ||
            PrefabUtility.IsPartOfPrefabInstance(geometry)) return "Catalog/prefab geometry is owned by its existing collider policy.";
        var mesh = geometry.GetComponent<MeshFilter>()?.sharedMesh;
        if (mesh == null || mesh.name != "Cube" || mesh.vertexCount != 24)
            return "Expected an authored cube primitive; use an explicit mesh/shape policy for other geometry.";
        var colliders = geometry.GetComponents<Collider>();
        if (colliders.Length > 1 || (colliders.Length == 1 && !(colliders[0] is BoxCollider)))
            return "Existing non-box or multiple colliders require review.";
        return null;
    }

    static bool IsCorrect(BoxCollider box, Bounds bounds) => box != null && box.enabled && !box.isTrigger &&
        (box.center - bounds.center).sqrMagnitude < .00000001f && (box.size - bounds.size).sqrMagnitude < .00000001f;
    static bool Indexed(string name, string prefix) => name.StartsWith(prefix, StringComparison.Ordinal) &&
        int.TryParse(name.Substring(prefix.Length), out int index) && index > 0;
    static string Path(Transform node) => node.parent == null ? node.name : Path(node.parent) + "/" + node.name;
    static void RequireEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Environment collision authoring requires Edit Mode.");
    }
}
