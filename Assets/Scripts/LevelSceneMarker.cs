using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelSceneMarker : MonoBehaviour
{
    [SerializeField] private string sceneId = "living-room-01";

    public string SceneId => sceneId;

#if UNITY_EDITOR
    public void EditorSetId(string value)
    {
        sceneId = value;
    }
#endif
}
