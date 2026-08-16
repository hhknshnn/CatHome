using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CatSleepZzzEffect : MonoBehaviour
{
    private const int PoolSize = 4;
    private readonly RectTransform[] items=new RectTransform[PoolSize];
    private readonly TMP_Text[] labels=new TMP_Text[PoolSize];
    private readonly float[] ages=new float[PoolSize];
    private readonly float[] lifetimes=new float[PoolSize];
    private Canvas canvas;
    private Transform head;
    private Camera gameplayCamera;
    private bool playing;
    private float spawnTimer;
    private int nextIndex;

    public void Begin()
    {
        EnsurePool();Stop();playing=true;spawnTimer=0f;Spawn();
    }

    public void Stop()
    {
        playing=false;spawnTimer=0f;
        for(int i=0;i<PoolSize;i++){ages[i]=-1f;if(items[i]!=null)items[i].gameObject.SetActive(false);}
    }

    private void Update()
    {
        if(!playing)return;
        spawnTimer-=Time.unscaledDeltaTime;
        if(spawnTimer<=0f){Spawn();spawnTimer=.62f;}
        Vector2 origin=HeadScreenPosition();
        for(int i=0;i<PoolSize;i++)
        {
            if(ages[i]<0f||items[i]==null)continue;
            ages[i]+=Time.unscaledDeltaTime;float t=ages[i]/lifetimes[i];
            if(t>=1f){ages[i]=-1f;items[i].gameObject.SetActive(false);continue;}
            float side=(i%2==0?1f:-1f)*(18f+10f*t);items[i].position=origin+new Vector2(side,25f+95f*t);
            items[i].localScale=Vector3.one*Mathf.Lerp(.65f,1.2f,t);
            Color color=labels[i].color;color.a=Mathf.Sin(t*Mathf.PI);labels[i].color=color;
        }
    }

    private void Spawn()
    {
        int index=nextIndex++%PoolSize;ages[index]=0f;lifetimes[index]=1.65f+.12f*(index%2);
        labels[index].text=index%3==0?"Z":index%3==1?"Zz":"Zzz";
        Color color=index%2==0?new Color32(210,193,244,255):new Color32(190,224,244,255);labels[index].color=color;
        items[index].gameObject.SetActive(true);
    }

    private Vector2 HeadScreenPosition()
    {
        if(gameplayCamera==null)gameplayCamera=Camera.main;
        if(head==null||gameplayCamera==null)return Vector2.zero;
        Vector3 value=gameplayCamera.WorldToScreenPoint(head.position+Vector3.up*.12f);
        return value.z>0f?(Vector2)value:new Vector2(-1000f,-1000f);
    }

    private void EnsurePool()
    {
        if(canvas!=null)return;
        gameplayCamera=Camera.main;head=FindNamed(transform,"Head")??FindNamed(transform,"Neck")??transform;
        GameObject root=new GameObject("CatSleepZzzCanvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
        canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.overrideSorting=true;canvas.sortingOrder=48;root.GetComponent<GraphicRaycaster>().enabled=false;
        TMP_FontAsset font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        for(int i=0;i<PoolSize;i++)
        {
            GameObject go=new GameObject("Zzz_"+i,typeof(RectTransform));items[i]=go.GetComponent<RectTransform>();items[i].SetParent(root.transform,false);items[i].sizeDelta=new Vector2(120f,55f);
            labels[i]=go.AddComponent<TextMeshProUGUI>();labels[i].font=font;labels[i].fontSize=34f;labels[i].fontStyle=FontStyles.Bold;labels[i].alignment=TextAlignmentOptions.Center;labels[i].raycastTarget=false;labels[i].richText=false;
            ages[i]=-1f;go.SetActive(false);
        }
    }

    private static Transform FindNamed(Transform root,string value){foreach(Transform item in root.GetComponentsInChildren<Transform>(true))if(string.Equals(item.name,value,System.StringComparison.OrdinalIgnoreCase))return item;return null;}
    private void OnDisable()=>Stop();
    private void OnDestroy(){if(canvas!=null)Destroy(canvas.gameObject);}
}
