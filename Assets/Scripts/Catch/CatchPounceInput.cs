using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class CatchPounceInput : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private CatCatchGameController game;

    public void EditorBind(CatCatchGameController controller)
    {
        game = controller;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (game == null || eventData == null)
            return;
        game.HandleTap(eventData.position);
    }
}
