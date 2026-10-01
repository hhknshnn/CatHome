using UnityEngine;

/// <summary>Room camera composition; unlisted rooms retain the shared overview.</summary>
public static class HomeRoomCameraProfile
{
    public static readonly Vector3 Position = new Vector3(0f, 3.6f, -6.5f);
    public static readonly Vector3 Angles = new Vector3(23f, 0f, 0f);
    public const float FieldOfView = 38f;
    public static readonly Vector3 LivingRoomPosition = new Vector3(0f, 2.7f, -6.5f);
    public static readonly Vector3 LivingRoomAngles = new Vector3(18f, 0f, 0f);
    public const float LivingRoomFieldOfView = 36f;

    public static Vector3 PositionFor(string scenePath) =>
        scenePath == HomeRoomService.LivingRoomScenePath ? LivingRoomPosition : Position;
    public static Vector3 AnglesFor(string scenePath) =>
        scenePath == HomeRoomService.LivingRoomScenePath ? LivingRoomAngles : Angles;
    public static float FieldOfViewFor(string scenePath) =>
        scenePath == HomeRoomService.LivingRoomScenePath ? LivingRoomFieldOfView : FieldOfView;
    // Full-frame catalog photos have no reserved HUD strip; retain the living-room photo framing.
    public const float PreviewFieldOfView = 42f;

    public static void Apply(Camera camera)
    {
        if (camera == null) return;
        string scenePath = camera.gameObject.scene.path;
        float fieldOfView = FieldOfViewFor(scenePath);
        camera.transform.SetPositionAndRotation(PositionFor(scenePath), Quaternion.Euler(AnglesFor(scenePath)));
        camera.orthographic = false;
        camera.fieldOfView = fieldOfView;
        var viewport = camera.GetComponent<HomeWorldViewport>();
        if (viewport == null) viewport = camera.gameObject.AddComponent<HomeWorldViewport>();
        viewport.ConfigureFraming(fieldOfView);
    }
}
