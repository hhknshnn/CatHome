using System.Collections.Generic;
using UnityEngine;

/// <summary>Empty legacy care anchors are bindings, not usable furniture.</summary>
public static class CareInteractionTarget
{
    private static readonly List<Renderer> renderers = new List<Renderer>(32);

    public static bool IsVisibleInRoom(Transform target, Transform cat)
    {
        if (!IsInRoom(target, cat))
            return false;

        // Do not use Renderer.isVisible: camera culling must not decide whether
        // a piece of furniture exists. Reuse the list as visuals can be replaced.
        target.GetComponentsInChildren(true, renderers);
        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy)
                continue;
            Mesh mesh = renderer is SkinnedMeshRenderer skin ? skin.sharedMesh :
                renderer is MeshRenderer ? renderer.GetComponent<MeshFilter>()?.sharedMesh : null;
            if (mesh != null && mesh.vertexCount > 0)
                return true;
        }
        return false;
    }

    public static bool IsInRoom(Transform target, Transform cat)
    {
        return target != null && cat != null && target.gameObject.activeInHierarchy &&
            cat.gameObject.activeInHierarchy && target.gameObject.scene.isLoaded &&
            target.gameObject.scene == cat.gameObject.scene;
    }

    public static float NearbyDistanceSquared(Transform entrance, Transform cat, float radius)
    {
        if (!IsInRoom(entrance, cat))
            return float.PositiveInfinity;
        Vector3 from = cat.position, to = entrance.position;
        from.y = to.y = 0f;
        float distance = (to - from).sqrMagnitude;
        return distance <= radius * radius && CatActivityMotion.ClearSegment(from, to)
            ? distance : float.PositiveInfinity;
    }
}
