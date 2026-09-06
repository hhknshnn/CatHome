using UnityEngine;

/// <summary>Keeps the decorated dock clear of joystick and contextual care.</summary>
[ExecuteAlways]
public sealed class PremiumHomeDockLayout : MonoBehaviour
{
    void LateUpdate()
    {
        var rect=(RectTransform)transform;var canvas=GetComponentInParent<Canvas>();
        if(canvas==null)return;
        float width=Screen.safeArea.width/Mathf.Max(.01f,canvas.scaleFactor);
        float scale=Mathf.Clamp((width-540f)/936f,.60f,1f);
        rect.localScale=Vector3.one*scale;
    }
}
