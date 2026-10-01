using UnityEngine;

/// <summary>Compact navigation in its own strip, below the rendered room.</summary>
[ExecuteAlways]
public sealed class PremiumHomeDockLayout : MonoBehaviour
{
    void LateUpdate()
    {
        var rect=(RectTransform)transform;var canvas=GetComponentInParent<Canvas>();
        if(canvas==null)return;
        float width=Screen.safeArea.width/Mathf.Max(.01f,canvas.scaleFactor);
        float scale=Mathf.Min(1f,(width-32f)/1080f);
        rect.localScale=Vector3.one*scale;
        float padding=Screen.safeArea.yMin/Mathf.Max(.01f,canvas.scaleFactor);
        rect.anchoredPosition=new Vector2(0,40f+padding);
        var tray=transform.Find("DockEnamelTray") as RectTransform;
        if(tray!=null)
        {
            tray.sizeDelta=new Vector2((Screen.width/canvas.scaleFactor+8)/scale,(80f+padding)/scale);
            tray.anchoredPosition=new Vector2(0,-padding*.5f/scale);
        }
    }

    // The commands button has a persistent modal canvas of its own. Align it
    // with the real dock slot, including asymmetric safe areas and both scales.
    public static void AlignShortcut(RectTransform dock,RectTransform shortcut)
    {
        if(dock==null||shortcut==null||shortcut.parent==null)return;
        shortcut.position=dock.TransformPoint(new Vector3(135f,0,0));
        Vector3 parentScale=shortcut.parent.lossyScale;
        Vector3 dockScale=dock.lossyScale;
        shortcut.localScale=new Vector3(dockScale.x/Mathf.Max(.0001f,parentScale.x),
            dockScale.y/Mathf.Max(.0001f,parentScale.y),1f);
    }
}
