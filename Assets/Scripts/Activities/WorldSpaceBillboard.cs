using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldSpaceBillboard : MonoBehaviour
{
    [SerializeField] private bool yawOnly = true;
    private Camera targetCamera;

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        Vector3 direction = transform.position - targetCamera.transform.position;
        if (yawOnly)
            direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }
}
