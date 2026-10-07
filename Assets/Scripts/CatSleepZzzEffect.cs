using UnityEngine;

/// <summary>A quiet, screen-sized sleep medallion attached to the current breed's head.</summary>
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public sealed class CatSleepZzzEffect : MonoBehaviour
{
    private CatCareFxCanvas presentation;
    private CatCareFxGraphic badge;
    private bool playing;
    private float elapsed;
    private UnityEngine.UI.Image pearl;
    private TMPro.TMP_Text dream;
    private CanvasGroup fade;
    private float stopping;
    private readonly UnityEngine.UI.Image[] sparks=new UnityEngine.UI.Image[2];
    private float stopAlpha;
    private CatSpeechBubble speech;
    public bool IsPlaying => playing;
    public float Elapsed => elapsed;

    public void Begin()
    {
        if (!isActiveAndEnabled) return;
        presentation = CatCareFxCanvas.For(gameObject);
        if (badge == null)
            badge = presentation.CreateGraphic("SleepMedallion", CatCareFxGraphic.Shape.Sleep, 74f);
        elapsed = 0f;
        stopping=0f;
        if(LivingRoomSpeech.IsLiving)
        {
            pearl=LivingFinalVisuals.Image(badge.transform,"PearlMoon",1);
            pearl.rectTransform.anchorMin=pearl.rectTransform.anchorMax=new Vector2(.5f,.5f);
            pearl.rectTransform.sizeDelta=new Vector2(58,61);
            pearl.rectTransform.anchoredPosition=new Vector2(-8,-2);
            pearl.preserveAspect=true;
            for(int i=0;i<2;i++)
            {
                sparks[i]=LivingFinalVisuals.Image(badge.transform,"DreamSpark"+i,i+2);
                sparks[i].rectTransform.anchorMin=sparks[i].rectTransform.anchorMax=new Vector2(.5f,.5f);
                sparks[i].rectTransform.sizeDelta=Vector2.one*(i==0?19:12);sparks[i].preserveAspect=true;sparks[i].gameObject.SetActive(true);
            }
            if(dream==null)
            {
                var go=new GameObject("Dream",typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(badge.transform,false);
                dream=go.AddComponent<TMPro.TextMeshProUGUI>();dream.font=PremiumTypography.Body;dream.text="Zzz";dream.fontSize=25;dream.color=new Color32(36,68,88,255);dream.raycastTarget=false;dream.outlineColor=new Color32(255,248,229,255);dream.outlineWidth=.14f;
                dream.alignment=TMPro.TextAlignmentOptions.Center;dream.rectTransform.sizeDelta=new Vector2(68,34);
            }
            fade=badge.GetComponent<CanvasGroup>();if(fade==null)fade=badge.gameObject.AddComponent<CanvasGroup>();
            fade.alpha=0;badge.enabled=false;pearl.gameObject.SetActive(true);dream.gameObject.SetActive(true);
        }
        else {badge.enabled=true;if(pearl!=null)pearl.gameObject.SetActive(false);if(dream!=null)dream.gameObject.SetActive(false);foreach(var spark in sparks)if(spark!=null)spark.gameObject.SetActive(false);if(fade!=null)fade.alpha=1;}
        playing = true;
        Present();
    }

    public void Stop()
    {
        playing = false;
        stopAlpha=fade!=null?fade.alpha:0;
        stopping=LivingRoomSpeech.IsLiving&&fade!=null&&isActiveAndEnabled?.35f:0f;
        if (badge != null&&stopping<=0) badge.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!playing)
        {if(stopping>0){stopping-=Time.deltaTime;fade.alpha=stopAlpha*Mathf.SmoothStep(0,1,stopping/.35f);if(stopping<=0)badge.gameObject.SetActive(false);}return;}
        elapsed += Time.deltaTime;
        Present();
    }

    private void Present()
    {
        if (presentation == null || badge == null) return;
        if (!presentation.TryHeadPosition(out Vector2 origin))
        {
            badge.gameObject.SetActive(false);
            return;
        }
        float floating = presentation.QuietMotion ? 0f : Mathf.Sin(elapsed * 1.45f) * 3f;
        badge.rectTransform.anchoredPosition = origin + new Vector2(30f, 61f + floating);
        if(LivingRoomSpeech.IsLiving&&pearl!=null)
        {
            bool reduced=CatRunnerProgressService.ReducedMotion;
            float t=Mathf.Repeat(elapsed/4.8f,1);
            fade.alpha=Mathf.SmoothStep(0,1,elapsed/.55f);
            pearl.rectTransform.localScale=Vector3.one*(reduced?1:1+Mathf.Sin(elapsed*1.1f)*.035f);
            badge.rectTransform.anchoredPosition=origin+new Vector2(33,67+(reduced?0:Mathf.Sin(elapsed*.9f)*3));
            dream.rectTransform.anchoredPosition=new Vector2(35,27+(reduced?0:Mathf.Sin(elapsed*.8f)*7));
            dream.alpha=reduced?.9f:.76f+.2f*Mathf.Sin(elapsed*.8f);
            dream.rectTransform.localRotation=Quaternion.Euler(0,0,reduced?0:Mathf.Sin(elapsed*.65f)*5);
            for(int i=0;i<2;i++)
            {
                float phase=Mathf.Repeat(elapsed/4.8f+i*.32f,1);
                var rect=sparks[i].rectTransform;rect.anchoredPosition=new Vector2(i==0?26:-15,-12+i*54+(reduced?0:phase*8));
                rect.localScale=Vector3.one*(reduced?1:.88f+Mathf.Sin(phase*Mathf.PI)*.16f);
                var color=Color.white;color.a=reduced?.8f:.25f+.7f*Mathf.Sin(phase*Mathf.PI);sparks[i].color=color;
            }
        }
        if(LivingRoomSpeech.IsLiving&&pearl!=null)
        {
            if(speech==null)speech=GetComponent<CatSpeechBubble>();
            float scale=Mathf.Max(.01f,presentation.Overlay.scaleFactor);
            Vector3 centre=badge.rectTransform.position;Rect safe=Screen.safeArea;
            float left=58*scale,right=78*scale,top=68*scale,bottom=38*scale,gap=14*scale;
            if(speech!=null&&speech.TryVisibleScreenRect(out var bubbleRect))
            {
                var space=Rect.MinMaxRect(centre.x-left,centre.y-bottom,centre.x+right,centre.y+top);
                if(space.Overlaps(bubbleRect))
                {
                    bool canLeft=bubbleRect.xMin-gap-right-left>=safe.xMin;
                    bool canRight=bubbleRect.xMax+gap+left+right<=safe.xMax;
                    centre.x=canLeft&&(!canRight||bubbleRect.center.x>safe.center.x)?bubbleRect.xMin-gap-right:bubbleRect.xMax+gap+left;
                }
            }
            centre.x=Mathf.Clamp(centre.x,safe.xMin+left+gap,safe.xMax-right-gap);
            centre.y=Mathf.Clamp(centre.y,safe.yMin+bottom+gap,safe.yMax-top-gap);
            badge.rectTransform.position=centre;
        }
        badge.gameObject.SetActive(true);
    }

    private void OnDisable(){playing=false;stopping=0;if(badge!=null)badge.gameObject.SetActive(false);}
    private void OnDestroy()
    {
        if (presentation != null) presentation.Release(badge);
    }
}
