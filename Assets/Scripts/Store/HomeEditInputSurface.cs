using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Forwards room-edit taps and drags without teaching the UI about placement.</summary>
[DisallowMultipleComponent]
public sealed class HomeEditInputSurface : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private HomeEditModeController controller;

    public void OnPointerDown(PointerEventData eventData)
    {
        controller?.HandlePointerDown(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        controller?.HandlePointerDrag(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        controller?.HandlePointerUp(eventData.position);
    }

#if UNITY_EDITOR
    public void EditorConfigure(HomeEditModeController owner)
    {
        controller = owner;
    }
#endif
}
