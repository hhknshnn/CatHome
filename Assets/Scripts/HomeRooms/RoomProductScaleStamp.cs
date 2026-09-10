using UnityEngine;

/// <summary>Records the baked uniform content scale so rebuilding never multiplies it twice.</summary>
[DisallowMultipleComponent]
public sealed class RoomProductScaleStamp : MonoBehaviour
{
    [SerializeField] private float appliedScale = 1f;
    public float AppliedScale => appliedScale;
#if UNITY_EDITOR
    public void EditorSet(float value) => appliedScale = value;
#endif
}
