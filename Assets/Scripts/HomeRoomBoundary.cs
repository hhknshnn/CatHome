using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the home cat's complete visible footprint inside a room's walkable floor.
/// The front of the authored room is intentionally open for the camera, so wall
/// colliders alone cannot provide this guarantee.
/// </summary>
[DisallowMultipleComponent]
public sealed class HomeRoomBoundary : MonoBehaviour
{
    [SerializeField] private Vector2 minimumXZ = new Vector2(-3.8f, -3f);
    [SerializeField] private Vector2 maximumXZ = new Vector2(3.8f, 2.8f);
    [SerializeField, Min(0f)] private float edgeClearance = 0.05f;

    public Vector2 MinimumXZ => minimumXZ;
    public Vector2 MaximumXZ => maximumXZ;
    public float EdgeClearance => edgeClearance;

    public void Configure(Vector2 minimum, Vector2 maximum, float clearance)
    {
        minimumXZ = Vector2.Min(minimum, maximum);
        maximumXZ = Vector2.Max(minimum, maximum);
        edgeClearance = Mathf.Max(0f, clearance);
    }

    public Vector3 ClampPosition(Vector3 worldPosition, float footprintRadius)
    {
        float inset = Mathf.Max(0f, footprintRadius) + edgeClearance;
        float minX = minimumXZ.x + inset;
        float maxX = maximumXZ.x - inset;
        float minZ = minimumXZ.y + inset;
        float maxZ = maximumXZ.y - inset;

        if (minX > maxX)
            minX = maxX = (minimumXZ.x + maximumXZ.x) * 0.5f;
        if (minZ > maxZ)
            minZ = maxZ = (minimumXZ.y + maximumXZ.y) * 0.5f;

        worldPosition.x = Mathf.Clamp(worldPosition.x, minX, maxX);
        worldPosition.z = Mathf.Clamp(worldPosition.z, minZ, maxZ);
        return worldPosition;
    }

    public static HomeRoomBoundary FindFor(Scene scene)
    {
        HomeRoomBoundary[] boundaries =
            FindObjectsByType<HomeRoomBoundary>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < boundaries.Length; i++)
            if (boundaries[i] != null && boundaries[i].gameObject.scene == scene)
                return boundaries[i];
        return null;
    }
}
