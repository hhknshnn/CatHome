using UnityEngine;

/// <summary>Restore legacy poses inside a closed door to its open room-facing side.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
public sealed class RoomDoorObstacle : MonoBehaviour
{
    public static Vector3 ResolveSavedPosition(CatMovement cat, Vector3 position)
    {
        if (cat == null) return position;
        var controller = cat.GetComponent<CharacterController>();
        float radius = controller != null
            ? controller.radius * Mathf.Max(Mathf.Abs(cat.transform.lossyScale.x), Mathf.Abs(cat.transform.lossyScale.z)) + .06f
            : .33f;
        Physics.SyncTransforms();
        foreach (var door in FindObjectsByType<RoomDoorObstacle>())
        {
            if (door.gameObject.scene != cat.gameObject.scene) continue;
            var body = door.GetComponent<BoxCollider>();
            if (!body.enabled || body.isTrigger) continue;
            var local = door.transform.InverseTransformPoint(position);
            var scale = door.transform.lossyScale;
            var half = body.size * .5f;
            var delta = local - body.center;
            if (Mathf.Abs(delta.x) > half.x + radius / Mathf.Max(.001f, Mathf.Abs(scale.x)) ||
                Mathf.Abs(delta.z) > half.z + radius / Mathf.Max(.001f, Mathf.Abs(scale.z))) continue;

            // The canonical door model faces local -Z, including when rotated on a side wall.
            var front = door.transform.TransformPoint(new Vector3(body.center.x, local.y, body.center.z - half.z));
            front -= door.transform.forward * (radius + .04f);
            front.y = position.y;
            if (CatActivityMotion.IsFloorClear(front, radius)) return front;
            float nearest = float.PositiveInfinity;
            foreach (var point in CatActivityMotion.ReachableFloor(front))
                if (CatActivityMotion.IsFloorClear(point, radius) && (point - front).sqrMagnitude < nearest)
                { nearest = (point - front).sqrMagnitude; position = point; }
            return position;
        }
        return position;
    }
}
