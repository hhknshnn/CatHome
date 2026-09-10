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
        rect.anchoredPosition=new Vector2(0,40f);
        var tray=transform.Find("DockEnamelTray") as RectTransform;
        if(tray!=null)
        {
            float padding=Screen.safeArea.yMin/Mathf.Max(.01f,canvas.scaleFactor);
            tray.sizeDelta=new Vector2((Screen.width/canvas.scaleFactor+8)/scale,(80f+padding)/scale);
            tray.anchoredPosition=new Vector2(0,-padding*.5f/scale);
        }
    }
}
