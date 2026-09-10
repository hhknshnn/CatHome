using UnityEngine;

/// <summary>
/// Explicit room-scene contract used by transitions and validation. References
/// are optional because legacy LivingRoom_Level01 still has safe type fallbacks.
/// </summary>
[DisallowMultipleComponent]
public sealed class HomeRoomSceneMarker : MonoBehaviour
{
    [SerializeField] private string roomId = HomeRoomService.LivingRoomId;
    [SerializeField] private Camera levelCamera;
    [SerializeField] private CatMovement cat;

    public string RoomId => roomId;
    public Camera LevelCamera => levelCamera;
    public CatMovement Cat => cat;

    private void OnEnable() => HomeRoomCameraProfile.Apply(levelCamera);

#if UNITY_EDITOR
    public void EditorConfigure(string id, Camera camera, CatMovement roomCat)
    {
        roomId = string.IsNullOrWhiteSpace(id) ? HomeRoomService.LivingRoomId : id;
        levelCamera = camera;
        cat = roomCat;
        HomeRoomCameraProfile.Apply(levelCamera);
    }
#endif
}
