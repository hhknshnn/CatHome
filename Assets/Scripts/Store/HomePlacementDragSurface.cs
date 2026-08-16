using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class HomePlacementDragSurface : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [SerializeField] private ShopPanelController controller;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (controller != null)
            controller.HandlePlacementDrag(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (controller != null)
            controller.HandlePlacementDrag(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (controller != null)
        {
            controller.HandlePlacementDrag(eventData.position);
            controller.HandlePlacementRelease();
        }
    }

#if UNITY_EDITOR
    public void EditorConfigure(ShopPanelController owner)
    {
        controller = owner;
    }
#endif
}
