using UnityEngine;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [SerializeField] private RectTransform handle;
    [SerializeField, Range(0.1f, 1f)]
    private float handleRange = 0.65f;

    private RectTransform background;
    private int activePointer=int.MinValue;

    public Vector2 Direction { get; private set; }

    private void Awake()
    {
        background = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(activePointer!=int.MinValue && activePointer!=eventData.pointerId)return;
        activePointer=eventData.pointerId;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(activePointer!=int.MinValue && activePointer!=eventData.pointerId)return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPosition
        );

        Vector2 radius = background.rect.size * 0.5f;

        Direction = new Vector2(
            localPosition.x / radius.x,
            localPosition.y / radius.y
        );

        Direction = Vector2.ClampMagnitude(Direction, 1f);

        if (handle != null)
        {
            handle.anchoredPosition = new Vector2(
                Direction.x * radius.x * handleRange,
                Direction.y * radius.y * handleRange
            );
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(activePointer!=eventData.pointerId)return;
        CancelInput();
    }

    public void CancelInput()
    {
        activePointer=int.MinValue;
        Direction = Vector2.zero;

        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }
    private void OnDisable()=>CancelInput();
    private void OnApplicationFocus(bool focused){if(!focused)CancelInput();}
}
