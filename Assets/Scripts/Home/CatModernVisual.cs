using UnityEngine;

/// <summary>One-time shared asset assignment. Never changes bones, bounds, scale, or contact geometry.</summary>
[DisallowMultipleComponent]
public sealed class CatModernVisual : MonoBehaviour
{
    private CatModernVisualCatalog appliedCatalog;
    private int appliedRevision;

    public static bool Apply(GameObject visualRoot)
    {
        if (visualRoot == null) return false;
        var catalog = CatModernVisualCatalog.Load();
        if (catalog == null) return false;
        var stamp = visualRoot.GetComponent<CatModernVisual>();
        if (stamp != null && stamp.appliedCatalog == catalog && stamp.appliedRevision == catalog.Revision)
            return false;

        bool changed = false;
        foreach (var skin in visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = catalog.Resolve(skin.sharedMesh);
            if (mesh != skin.sharedMesh) { skin.sharedMesh = mesh; changed = true; }
            var materials = skin.sharedMaterials;
            bool materialChanged = false;
            for (int i = 0; i < materials.Length; i++)
            {
                var modern = catalog.Resolve(materials[i]);
                if (modern == materials[i]) continue;
                materials[i] = modern;
                materialChanged = true;
            }
            if (materialChanged) { skin.sharedMaterials = materials; changed = true; }
        }
        if (stamp == null) stamp = visualRoot.AddComponent<CatModernVisual>();
        stamp.appliedCatalog = catalog;
        stamp.appliedRevision = catalog.Revision;
        return changed;
    }
}
