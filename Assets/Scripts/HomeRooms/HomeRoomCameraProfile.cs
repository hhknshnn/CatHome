using UnityEngine;

/// <summary>The shared front-centred composition for every home room, including new rooms.</summary>
public static class HomeRoomCameraProfile
{
    public static readonly Vector3 Position = new Vector3(0f, 3.6f, -6.5f);
    public static readonly Vector3 Angles = new Vector3(23f, 0f, 0f);
    public const float FieldOfView = 38f;
    // Full-frame catalog photos have no reserved HUD strip; retain the living-room photo framing.
    public const float PreviewFieldOfView = 42f;

    public static void Apply(Camera camera)
    {
        if (camera == null) return;
        camera.transform.SetPositionAndRotation(Position, Quaternion.Euler(Angles));
        camera.orthographic = false;
        camera.fieldOfView = FieldOfView;
        var viewport = camera.GetComponent<HomeWorldViewport>();
        if (viewport == null) viewport = camera.gameObject.AddComponent<HomeWorldViewport>();
        viewport.ConfigureFraming(FieldOfView);
    }
}
