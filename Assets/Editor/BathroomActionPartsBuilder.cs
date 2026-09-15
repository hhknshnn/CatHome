using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>One measured authoring path for new prefabs and existing bathroom products.</summary>
public static class BathroomActionPartsBuilder
{
    public static void Configure(GameObject root)
    {
        var litter = root.GetComponent<LitterDigActivity>();
        if (litter != null && litter.UsesGentleScraping) ConfigureSand(root, litter);
        else if (litter != null) RemoveMisplacedSand(litter);
        var paper = root.GetComponent<PaperSpinActivity>();
        if (paper != null && paper.UsesPaperTears) ConfigurePaper(root, paper);
    }

    public static bool RemoveMisplacedSand(LitterDigActivity activity)
    {
        if (activity == null || activity.UsesGentleScraping) return false;
        var surface = activity.GetComponent<CatLitterSandSurface>();
        if (surface == null) return false;
        var data = new SerializedObject(activity);
        data.FindProperty("litterSurface").objectReferenceValue = null;
        data.FindProperty("digFacingPoint").objectReferenceValue = null;
        data.ApplyModifiedPropertiesWithoutUndo();
        if (surface.Sand != null && surface.Sand.name == "LitterSandSurface")
            UnityEngine.Object.DestroyImmediate(surface.Sand.gameObject);
        var facing = activity.transform.Find("DigFacingPoint");
        if (facing != null) UnityEngine.Object.DestroyImmediate(facing.gameObject);
        UnityEngine.Object.DestroyImmediate(surface);
        return true;
    }

    static void ConfigureSand(GameObject root, LitterDigActivity activity)
    {
        var body = root.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.name.EndsWith("_PremiumModel"));
        if (body == null) throw new InvalidOperationException("Litter tray needs its measured premium shell.");
        const string path = "Assets/Art/PremiumFurniture/Models/BathroomLitterSand_Premium.fbx";
        var mesh = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault();
        if (mesh == null) throw new InvalidOperationException("Build/import the independent Blender sand surface first.");
        var child = body.transform.parent.Find("LitterSandSurface");
        if (child == null) { child = new GameObject("LitterSandSurface").transform; child.SetParent(body.transform.parent, false); }
        child.localPosition = body.transform.localPosition;
        child.localRotation = body.transform.localRotation;
        child.localScale = body.transform.localScale;
        var filter = child.GetComponent<MeshFilter>();
        if (filter == null) filter = child.gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var renderer = child.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = child.gameObject.AddComponent<MeshRenderer>();
        var bounds = mesh.bounds;
        LitterSandMaterialBuilder.Apply(filter, bounds.center.y);
        activity.EditorConfigureLitterSurface(filter, bounds.center, new Vector2(bounds.size.x, bounds.size.z));
        var facing = root.transform.Find("DigFacingPoint");
        if (facing == null) { facing = new GameObject("DigFacingPoint").transform; facing.SetParent(root.transform, false); }
        var dig = root.transform.Find("DigPoint");
        facing.localPosition = dig.localPosition + Vector3.right * .35f;
        activity.EditorConfigureLitterFacing(facing);
    }

    static void ConfigurePaper(GameObject root, PaperSpinActivity activity)
    {
        var body = root.GetComponentsInChildren<MeshFilter>(true).First(m => m.name.EndsWith("_PremiumModel"));
        // Same authored axis as Blender: the holder is advanced along the real
        // side of the toilet, so the clear floor bay is within normal arm reach.
        Vector3 newAxis = body.transform.TransformPoint(new Vector3(.375f, .760f, -.545f));
        activity.RollPivot.position = newAxis;
        activity.RollPivot.localRotation = Quaternion.identity;
        var roll = activity.RollPivot.Find("ToiletPaperRoll");
        if (roll != null) roll.localPosition = -activity.RollPivot.localPosition;
        var point = root.transform.Find("PaperContactPoint");
        if (point == null) { point = new GameObject("PaperContactPoint").transform; point.SetParent(root.transform, false); }
        Vector3 axis = root.transform.InverseTransformPoint(activity.RollPivot.position);
        float radius = axis.y * (.115f / .760f);
        point.localPosition = axis + new Vector3(0f, -1f, 1f).normalized * radius;
        activity.EditorConfigurePaperContact(point);
        // The toilet now backs onto the right wall. The extended real holder
        // reaches its open front bay, where the cat can show its side and face.
        var swat = root.transform.Find("SwatPoint");
        if (swat != null) swat.localPosition = new Vector3(.02f, 0f, .67f);
        var entry = root.transform.Find("InteractionAnchor");
        if (entry != null) entry.localPosition = new Vector3(.04f, 0f, .80f);
    }
}
