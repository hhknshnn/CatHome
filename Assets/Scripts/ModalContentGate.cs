using UnityEngine;

/// <summary>Stops keyboard and pointer access to content below a nested modal.</summary>
public sealed class ModalContentGate : MonoBehaviour
{
    [SerializeField] private CanvasGroup content;
    [SerializeField] private CanvasGroup[] overlays;
    private void LateUpdate()
    {
        if(content==null)return;
        bool allowed=content.alpha>.01f;
        if(overlays!=null)foreach(var overlay in overlays)
            if(overlay!=null && overlay.gameObject.activeInHierarchy && overlay.alpha>.01f){allowed=false;break;}
        content.interactable=allowed;content.blocksRaycasts=allowed;
    }
    public void Configure(CanvasGroup target,params CanvasGroup[] blockers){content=target;overlays=blockers;}
}
