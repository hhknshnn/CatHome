using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnPointId = "default";

    public string SpawnPointId => string.IsNullOrWhiteSpace(spawnPointId)
        ? "default"
        : spawnPointId;

#if UNITY_EDITOR
    public void EditorSetId(string value)
    {
        spawnPointId = string.IsNullOrWhiteSpace(value) ? "default" : value;
    }
#endif
}
