using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TutorialSpotlight : MaskableGraphic
{
    [SerializeField] private Color dimColor = new Color(0.08f, 0.045f, 0.025f, 0.62f);
    [SerializeField] private Color accentColor = new Color32(255, 190, 105, 230);
    private Transform[] targets;
    private Camera gameplayCamera;
    private Rect hole;
    private bool hasHole;
    private Vector4 padding = new Vector4(24f, 24f, 24f, 24f);
    private Coroutine hideRoutine;

    public void Show(Transform[] newTargets, Camera camera, float referencePadding = 24f)
    {
        Show(newTargets, camera, new Vector4(referencePadding, referencePadding, referencePadding, referencePadding));
    }

    public void Show(Transform[] newTargets, Camera camera, Vector4 directionalPadding)
    {
        bool changed = !SameTargets(targets, newTargets);
        targets = newTargets;
        gameplayCamera = camera;
        padding = directionalPadding;
        if (hideRoutine != null) { StopCoroutine(hideRoutine); hideRoutine = null; }
        canvasRenderer.SetAlpha(1f);
        raycastTarget = true;
        gameObject.SetActive(true);
        RefreshHole(changed || !hasHole);
    }

    public void Hide()
    {
        if (hideRoutine != null) { StopCoroutine(hideRoutine); hideRoutine = null; }
        canvasRenderer.SetAlpha(1f);
        raycastTarget = false;
        gameObject.SetActive(false);
    }

    public void HideAnimated(float duration = .18f)
    {
        raycastTarget = false;
        if (!gameObject.activeSelf) return;
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(FadeOut(duration));
    }

    private void LateUpdate()
    {
        RefreshHole(false);
        SetVerticesDirty();
    }

    private void RefreshHole(bool immediate)
    {
        if (targets == null || rectTransform == null) return;
        RectTransform canvasRect = rectTransform;
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera ownCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Rect desired = default;
        bool found = false;
        for (int i=0;i<targets.Length;i++)
        {
            Transform target = targets[i];
            if (target == null) continue;
            Rect candidate;
            if (target is RectTransform rt && target.GetComponentInParent<Canvas>() != null)
            {
                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                Vector2 a = RectTransformUtility.WorldToScreenPoint(GetTargetCamera(rt), corners[0]);
                Vector2 b = RectTransformUtility.WorldToScreenPoint(GetTargetCamera(rt), corners[2]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, a, ownCamera, out Vector2 la);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, b, ownCamera, out Vector2 lb);
                candidate = Rect.MinMaxRect(Mathf.Min(la.x,lb.x),Mathf.Min(la.y,lb.y),Mathf.Max(la.x,lb.x),Mathf.Max(la.y,lb.y));
            }
            else
            {
                Camera cam = gameplayCamera != null ? gameplayCamera : Camera.main;
                if (cam == null) continue;
                if (!TryGetWorldTargetRect(target, cam, canvasRect, ownCamera, out candidate)) continue;
            }
            desired = found ? Union(desired, candidate) : candidate;
            found = true;
        }
        if (found)
        {
            desired = Rect.MinMaxRect(
                desired.xMin-padding.x,
                desired.yMin-padding.w,
                desired.xMax+padding.y,
                desired.yMax+padding.z);
            Rect safeLocal = ScreenSafeAreaLocal(canvasRect, ownCamera);
            desired.xMin = Mathf.Max(desired.xMin, safeLocal.xMin);
            desired.xMax = Mathf.Min(desired.xMax, safeLocal.xMax);
            desired.yMin = Mathf.Max(desired.yMin, safeLocal.yMin);
            desired.yMax = Mathf.Min(desired.yMax, safeLocal.yMax);
            float blend = immediate || !hasHole ? 1f : 1f-Mathf.Exp(-Time.unscaledDeltaTime/0.08f);
            hole = Lerp(hole, desired, blend);
        }
        hasHole = found;
    }

    private static bool TryGetWorldTargetRect(Transform target, Camera camera, RectTransform canvasRect, Camera canvasCamera, out Rect result)
    {
        result = default;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(false);
        bool hasBounds = false;
        Bounds bounds = default;
        for (int i=0;i<renderers.Length;i++)
        {
            Renderer renderer = renderers[i];
            if (!IsVisualRenderer(renderer)) continue;
            if (!hasBounds) { bounds=renderer.bounds; hasBounds=true; } else bounds.Encapsulate(renderer.bounds);
        }
        if (!hasBounds)
        {
            Collider[] colliders=target.GetComponentsInChildren<Collider>(false);
            for(int i=0;i<colliders.Length;i++)
            {
                Collider collider=colliders[i];
                if(!collider.enabled||IsHelperName(collider.name)) continue;
                if(!hasBounds){bounds=collider.bounds;hasBounds=true;}else bounds.Encapsulate(collider.bounds);
            }
        }
        if (!hasBounds)
        {
            Vector3 screen=camera.WorldToScreenPoint(target.position);
            if(screen.z<=0f)return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,canvasCamera,out Vector2 local);
            result=new Rect(local-new Vector2(44f,44f),new Vector2(88f,88f));
            return true;
        }
        Vector3 min=bounds.min,max=bounds.max;
        Vector3[] corners={new(min.x,min.y,min.z),new(min.x,min.y,max.z),new(min.x,max.y,min.z),new(min.x,max.y,max.z),new(max.x,min.y,min.z),new(max.x,min.y,max.z),new(max.x,max.y,min.z),new(max.x,max.y,max.z)};
        bool any=false; Vector2 localMin=default,localMax=default;
        for(int i=0;i<corners.Length;i++)
        {
            Vector3 screen=camera.WorldToScreenPoint(corners[i]); if(screen.z<=0f)continue;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,canvasCamera,out Vector2 local);
            if(!any){localMin=localMax=local;any=true;}else{localMin=Vector2.Min(localMin,local);localMax=Vector2.Max(localMax,local);}
        }
        if(!any)return false; result=Rect.MinMaxRect(localMin.x,localMin.y,localMax.x,localMax.y); return true;
    }

    private static bool IsVisualRenderer(Renderer renderer)
    {
        return renderer!=null&&renderer.enabled&&renderer.gameObject.activeInHierarchy&&
               renderer is not ParticleSystemRenderer&&renderer is not TrailRenderer&&renderer is not LineRenderer&&
               !IsHelperName(renderer.name);
    }
    private static bool IsHelperName(string value)
    {
        string n=value.ToLowerInvariant();
        return n.Contains("trigger")||n.Contains("interaction")||n.Contains("sleep point")||n.Contains("sleeppoint")||
               n.Contains("helper")||n.Contains("gizmo")||n.Contains("particle")||n.Contains("vfx")||n.Contains("effect");
    }
    private static bool SameTargets(Transform[] a,Transform[] b){if(a==null||b==null||a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    private static Rect Lerp(Rect a,Rect b,float t)=>Rect.MinMaxRect(Mathf.Lerp(a.xMin,b.xMin,t),Mathf.Lerp(a.yMin,b.yMin,t),Mathf.Lerp(a.xMax,b.xMax,t),Mathf.Lerp(a.yMax,b.yMax,t));
    private IEnumerator FadeOut(float duration){CrossFadeAlpha(0f,Mathf.Max(.01f,duration),true);yield return new WaitForSecondsRealtime(Mathf.Max(.01f,duration));hideRoutine=null;gameObject.SetActive(false);canvasRenderer.SetAlpha(1f);}

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect full = GetPixelAdjustedRect();
        if (!hasHole) { AddQuad(vh, full, dimColor); return; }
        AddQuad(vh, Rect.MinMaxRect(full.xMin,full.yMin,full.xMax,hole.yMin),dimColor);
        AddQuad(vh, Rect.MinMaxRect(full.xMin,hole.yMax,full.xMax,full.yMax),dimColor);
        AddQuad(vh, Rect.MinMaxRect(full.xMin,hole.yMin,hole.xMin,hole.yMax),dimColor);
        AddQuad(vh, Rect.MinMaxRect(hole.xMax,hole.yMin,full.xMax,hole.yMax),dimColor);
        float pulse = .72f + .28f * (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 1.05f) * .5f + .5f);
        Color border = accentColor; border.a *= pulse;
        float thickness = 5f + pulse * 2f;
        Rect outer = new Rect(hole.xMin-thickness,hole.yMin-thickness,hole.width+thickness*2f,hole.height+thickness*2f);
        AddFrame(vh, outer, thickness, border);
    }

    private static Camera GetTargetCamera(RectTransform rt)
    {
        Canvas c = rt.GetComponentInParent<Canvas>();
        return c == null || c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
    }
    private static Rect ScreenSafeAreaLocal(RectTransform root, Camera camera)
    {
        Rect safe=Screen.safeArea;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,safe.min,camera,out Vector2 min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,safe.max,camera,out Vector2 max);
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    private static Rect Union(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin,b.xMin),Mathf.Min(a.yMin,b.yMin),Mathf.Max(a.xMax,b.xMax),Mathf.Max(a.yMax,b.yMax));
    private static void AddFrame(VertexHelper vh, Rect r, float t, Color c)
    {
        AddQuad(vh,Rect.MinMaxRect(r.xMin,r.yMin,r.xMax,r.yMin+t),c); AddQuad(vh,Rect.MinMaxRect(r.xMin,r.yMax-t,r.xMax,r.yMax),c);
        AddQuad(vh,Rect.MinMaxRect(r.xMin,r.yMin+t,r.xMin+t,r.yMax-t),c); AddQuad(vh,Rect.MinMaxRect(r.xMax-t,r.yMin+t,r.xMax,r.yMax-t),c);
    }
    private static void AddQuad(VertexHelper vh, Rect r, Color c)
    {
        if (r.width<=0f || r.height<=0f) return;
        int s=vh.currentVertCount; vh.AddVert(new Vector2(r.xMin,r.yMin),c,Vector2.zero); vh.AddVert(new Vector2(r.xMin,r.yMax),c,Vector2.zero);
        vh.AddVert(new Vector2(r.xMax,r.yMax),c,Vector2.zero); vh.AddVert(new Vector2(r.xMax,r.yMin),c,Vector2.zero); vh.AddTriangle(s,s+1,s+2); vh.AddTriangle(s,s+2,s+3);
    }
}
