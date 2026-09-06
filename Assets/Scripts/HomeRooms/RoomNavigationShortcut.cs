using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent, RequireComponent(typeof(Button))]
public sealed class RoomNavigationShortcut : MonoBehaviour
{
    private void OnEnable() => GetComponent<Button>().onClick.AddListener(Open);
    private void OnDisable() => GetComponent<Button>().onClick.RemoveListener(Open);
    private void Open()
    {
        var selector = FindAnyObjectByType<RoomSelectorPanel>(FindObjectsInactive.Include);
        if (selector != null) selector.RequestOpen();
    }
}
