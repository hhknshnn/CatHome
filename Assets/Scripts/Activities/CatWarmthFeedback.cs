using UnityEngine;
using UnityEngine.Rendering;

/// <summary>One quiet warmth visual and owned localized bubble per resting cat.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1100)]
public sealed class CatWarmthFeedback : MonoBehaviour
{
    const int Wisps=3,Points=6;
    public const float BubbleInterval=30f;
    readonly Vector3[] vertices=new Vector3[Wisps*Points*3];
    readonly Color[] colours=new Color[Wisps*Points*3];
    readonly int[] triangles=new int[Wisps*(Points-1)*12];
    OvenWarmthActivity owner;
    CatMovement cat;
    CatSpeechBubble bubble;
    Transform visualRoot;
    Mesh mesh;
    Material material;
    bool running;
    float elapsed,nextBubble;
    public bool IsActive=>running&&owner!=null&&owner.IsRunning;
    public float Elapsed=>elapsed;
    public int BubbleCount {get;private set;}
    public Transform VisualRoot=>visualRoot;
    public bool QuietMotion=>CatRunnerProgressService.ReducedMotion;

    public void Begin(OvenWarmthActivity activity,CatMovement actor)
    {
        Stop();
        if(activity==null||actor==null||!activity.IsRunning||!activity.BelongsTo(actor))return;
        owner=activity;cat=actor;
        bubble=actor.GetComponent<CatSpeechBubble>()??actor.gameObject.AddComponent<CatSpeechBubble>();
        elapsed=nextBubble=0;BubbleCount=0;running=true;
        EnsureVisual();Draw();
    }
    void OnEnable()=>GameLanguageService.Changed+=LanguageChanged;
    void OnDisable(){GameLanguageService.Changed-=LanguageChanged;Stop();}
    void LanguageChanged()
    {
        if(IsActive&&bubble!=null&&bubble.IsOwnedBy(this))bubble.ShowOwned(this,"SO WARM!");
    }
    void LateUpdate()
    {
        if(!running)return;
        if(owner==null||cat==null||!owner.isActiveAndEnabled||!owner.IsRunning||!owner.BelongsTo(cat))
        {Stop();return;}
        if(Time.deltaTime<=0f)return;
        elapsed+=Time.deltaTime;
        if(elapsed>=nextBubble)
        {
            nextBubble=elapsed+BubbleInterval;
            if(bubble!=null){bubble.ShowOwned(this,"SO WARM!");BubbleCount++;}
        }
        Draw();
    }
    void EnsureVisual()
    {
        if(visualRoot!=null)return;
        var shader=Shader.Find("CatHome/Window Sun Beam")??Shader.Find("Sprites/Default");
        if(shader==null)return;
        var visual=new GameObject("Warmth wisps",typeof(MeshFilter),typeof(MeshRenderer));
        visual.layer=gameObject.layer;visualRoot=visual.transform;visualRoot.SetParent(transform,false);
        mesh=new Mesh{name="Runtime_WarmthWisps",hideFlags=HideFlags.DontSave};mesh.MarkDynamic();
        material=new Material(shader){name="Runtime_WarmthWisps",hideFlags=HideFlags.DontSave,renderQueue=(int)RenderQueue.Transparent};
        if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",Color.white);
        if(material.HasProperty("_Color"))material.SetColor("_Color",Color.white);
        int n=0;
        for(int w=0;w<Wisps;w++)for(int p=0;p<Points-1;p++)for(int band=0;band<2;band++)
        {
            int a=(w*Points+p)*3+band,b=a+3;
            triangles[n++]=a;triangles[n++]=b;triangles[n++]=a+1;
            triangles[n++]=a+1;triangles[n++]=b;triangles[n++]=b+1;
        }
        mesh.vertices=vertices;mesh.colors=colours;mesh.triangles=triangles;
        visual.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=visual.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
    }
    void Draw()
    {
        if(visualRoot==null||cat==null)return;
        Vector3 right=Camera.main!=null?Camera.main.transform.right:cat.transform.right;
        right.y=0;right.Normalize();
        for(int w=0;w<Wisps;w++)
        {
            float phase=QuietMotion ? .5f : Mathf.Repeat(elapsed/2.6f+w/(float)Wisps,1f);
            Vector3 origin=cat.transform.position+cat.transform.right*((w-1)*.12f)-cat.transform.forward*.05f;
            for(int p=0;p<Points;p++)
            {
                float u=p/(float)(Points-1);
                Vector3 centre=origin+Vector3.up*(.10f+phase*.12f+u*.16f)+
                    right*(Mathf.Sin(u*5f+(QuietMotion?0:elapsed)*1.1f+w)*.009f);
                float alpha=(QuietMotion ? .065f : .11f)*Mathf.Sin(phase*Mathf.PI)*Mathf.Sin(u*Mathf.PI);
                int at=(w*Points+p)*3;
                for(int band=0;band<3;band++)
                {
                    vertices[at+band]=visualRoot.InverseTransformPoint(centre+right*((band-1)*.007f));
                    colours[at+band]=new Color(1f,.75f,.35f,band==1?alpha:0);
                }
            }
        }
        mesh.SetVertices(vertices);mesh.SetColors(colours);mesh.RecalculateBounds();
        visualRoot.gameObject.SetActive(true);
    }
    public void Stop()
    {
        if(bubble!=null)bubble.DismissOwned(this);
        running=false;owner=null;cat=null;bubble=null;elapsed=nextBubble=0;
        if(visualRoot!=null)visualRoot.gameObject.SetActive(false);
    }
    void OnDestroy()
    {
        Stop();
        if(visualRoot!=null)Destroy(visualRoot.gameObject);
        if(mesh!=null)Destroy(mesh);
        if(material!=null)Destroy(material);
    }
}
