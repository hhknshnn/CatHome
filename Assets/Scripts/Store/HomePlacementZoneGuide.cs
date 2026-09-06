using UnityEngine;

/// <summary>
/// Shows the selected item's legal placement family without covering the room.
/// </summary>
[DisallowMultipleComponent]
public sealed class HomePlacementZoneGuide : MonoBehaviour
{
    private GameObject guideRoot;
    private Material guideMaterial;

    public void Show(HomeProductPlacement placement)
    {
        Hide();
        if (placement == null)
            return;

        guideRoot = new GameObject("PlacementZoneGuide_Runtime");
        guideRoot.transform.SetParent(transform, false);

        switch (placement.PlacementKind)
        {
            case HomeProductPlacementKind.Floor:
                BuildFloorPerimeter(new Color(0.18f, 0.93f, 0.67f, 0.48f));
                break;
            case HomeProductPlacementKind.WallEdge:
                BuildWallRails(new Color(0.72f, 0.48f, 1f, 0.58f));
                break;
            case HomeProductPlacementKind.BookshelfOnly:
            case HomeProductPlacementKind.ProductSurfaceOnly:
                BuildTargetOutline(
                    placement.RequiredPlacementTarget,
                    new Color(1f, 0.72f, 0.18f, 0.75f));
                break;
        }
    }

    public void Hide()
    {
        if (guideRoot != null)
            Destroy(guideRoot);
        if (guideMaterial != null)
            Destroy(guideMaterial);
        guideRoot = null;
        guideMaterial = null;
    }

    private void OnDisable() => Hide();

    private void BuildFloorPerimeter(Color color)
    {
        guideMaterial = CreateGuideMaterial(color);
        const float left = HomeProductPlacement.DefaultRoomLeft + 0.14f;
        const float right = HomeProductPlacement.DefaultRoomRight - 0.14f;
        const float front = HomeProductPlacement.DefaultRoomFront + 0.14f;
        const float back = HomeProductPlacement.DefaultRoomBack - 0.14f;
        const float thickness = 0.045f;
        float width = right - left;
        float depth = back - front;
        CreateBar("FloorGuide_Back", new Vector3(0f, 0.035f, back),
            new Vector3(width, 0.018f, thickness));
        CreateBar("FloorGuide_Front", new Vector3(0f, 0.035f, front),
            new Vector3(width, 0.018f, thickness));
        CreateBar("FloorGuide_Left", new Vector3(left, 0.035f, 0f),
            new Vector3(thickness, 0.018f, depth));
        CreateBar("FloorGuide_Right", new Vector3(right, 0.035f, 0f),
            new Vector3(thickness, 0.018f, depth));
    }

    private void BuildWallRails(Color color)
    {
        guideMaterial = CreateGuideMaterial(color);
        const float y = 1.36f;
        const float thickness = 0.045f;
        float width = HomeProductPlacement.DefaultRoomRight -
                      HomeProductPlacement.DefaultRoomLeft - 0.3f;
        float depth = HomeProductPlacement.DefaultRoomBack -
                      HomeProductPlacement.DefaultRoomFront - 0.3f;
        CreateBar("WallGuide_Back", new Vector3(0f, y, HomeProductPlacement.DefaultRoomBack - 0.035f),
            new Vector3(width, 0.075f, thickness));
        CreateBar("WallGuide_Left", new Vector3(HomeProductPlacement.DefaultRoomLeft + 0.035f, y, 0f),
            new Vector3(thickness, 0.075f, depth));
        CreateBar("WallGuide_Right", new Vector3(HomeProductPlacement.DefaultRoomRight - 0.035f, y, 0f),
            new Vector3(thickness, 0.075f, depth));
    }

    private void BuildTargetOutline(Transform target, Color color)
    {
        if (target == null || !TryGetBounds(target, out Bounds bounds))
            return;
        guideMaterial = CreateGuideMaterial(color);
        Vector3 center = bounds.center;
        Vector3 size = bounds.size + new Vector3(0.12f, 0.12f, 0.12f);
        float t = Mathf.Clamp(Mathf.Min(size.x, size.z) * 0.06f, 0.035f, 0.075f);
        float y = bounds.max.y + 0.055f;
        CreateBar("TargetGuide_Front", new Vector3(center.x, y, center.z - size.z * 0.5f),
            new Vector3(size.x, t, t));
        CreateBar("TargetGuide_Back", new Vector3(center.x, y, center.z + size.z * 0.5f),
            new Vector3(size.x, t, t));
        CreateBar("TargetGuide_Left", new Vector3(center.x - size.x * 0.5f, y, center.z),
            new Vector3(t, t, size.z));
        CreateBar("TargetGuide_Right", new Vector3(center.x + size.x * 0.5f, y, center.z),
            new Vector3(t, t, size.z));
    }

    private void CreateBar(string name, Vector3 position, Vector3 scale)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = name;
        bar.layer = 2;
        bar.transform.SetParent(guideRoot.transform, false);
        bar.transform.position = position;
        bar.transform.localScale = scale;
        Collider collider = bar.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        Renderer renderer = bar.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = guideMaterial;
    }

    private static Material CreateGuideMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                        Shader.Find("Unlit/Color");
        Material material = new Material(shader) { name = "Runtime_PlacementZoneGuide" };
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        material.renderQueue = 3000;
        return material;
    }

    private static bool TryGetBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        bounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.gameObject.name == "PlacementValidityIndicator")
                continue;
            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return found;
    }
}
