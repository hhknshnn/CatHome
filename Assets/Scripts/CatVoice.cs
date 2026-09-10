using UnityEngine;

/// <summary>Natural, state-bound vocal and care audio. No ambient timer or electronic care ticks.</summary>
[DefaultExecutionOrder(600)]
[DisallowMultipleComponent]
public sealed class CatVoice : MonoBehaviour
{
    private AudioSource loop,voice;
    private AudioClip meow,purr,eat,drink;
    private BowlInteraction bowls;
    private SleepInteraction sleep;
    private CatActivityAnimation activity;
    private float nextMeow,briefPurrUntil;
    private bool paused;
    public string PlayingLoop=>loop!=null&&loop.isPlaying&&loop.clip!=null?loop.clip.name:string.Empty;
    public bool IsVocalizing=>voice!=null&&voice.isPlaying;
    public float MeowDuration=>meow!=null?meow.length:3f;
    public static CatVoice EnsureOn(CatMovement cat)=>cat.GetComponent<CatVoice>()??cat.gameObject.AddComponent<CatVoice>();
    private void Awake()
    {
        meow=Resources.Load<AudioClip>("CatAudio/CatMeow");purr=Resources.Load<AudioClip>("CatAudio/CatPurr");
        eat=Resources.Load<AudioClip>("CatAudio/CatEat");drink=Resources.Load<AudioClip>("CatAudio/CatDrink");
        loop=gameObject.AddComponent<AudioSource>();voice=gameObject.AddComponent<AudioSource>();
        foreach(var source in new[]{loop,voice}){source.playOnAwake=false;source.spatialBlend=0;source.priority=70;source.dopplerLevel=0;}
        loop.loop=true;loop.volume=0;voice.volume=.40f;
        bowls=GetComponent<BowlInteraction>();sleep=GetComponent<SleepInteraction>();
    }
    public bool Meow()
    {
        if(!isActiveAndEnabled||paused||!HomeAudioService.SoundEnabled||Time.unscaledTime<nextMeow||meow==null)return false;
        nextMeow=Time.unscaledTime+Mathf.Max(3,meow.length);
        voice.pitch=1;voice.PlayOneShot(meow);return true;
    }
    public void PurrBriefly(){briefPurrUntil=Time.time+2.5f;}
    private void Update()
    {
        if(paused||!HomeAudioService.SoundEnabled||Time.timeScale<=0||TitleScreen.IsShowing||HomeUiFlow.IsMiniGameVisible)
        {loop.Stop();voice.Stop();loop.volume=0;return;}
        if(activity==null)activity=GetComponent<CatActivityAnimation>();
        AudioClip desired=null;float volume=.17f;
        if(bowls!=null&&bowls.IsInteracting)
        {if(bowls.ActiveCareSound=="CatEat")desired=eat;else if(bowls.ActiveCareSound=="CatDrink")desired=drink;volume=.28f;}
        else if(activity!=null&&activity.IsActive)
        {
            if(activity.CurrentPose==CatActivityPose.Eat){desired=eat;volume=.28f;}
            else if(activity.CurrentPose==CatActivityPose.Drink){desired=drink;volume=.25f;}
            else if(activity.CurrentPose==CatActivityPose.Sleep||CatActivity.Active!=null&&CatActivity.Active.IsWaitingForRestStop)desired=purr;
        }
        else if(sleep!=null&&sleep.IsSleeping)desired=purr;
        else if(Time.time<briefPurrUntil)desired=purr;
        if(desired!=loop.clip)
        {
            loop.volume=Mathf.MoveTowards(loop.volume,0,Time.unscaledDeltaTime*1.4f);
            if(loop.volume>.001f)return;
            loop.Stop();loop.clip=desired;
        }
        if(desired==null)return;
        if(!loop.isPlaying)loop.Play();
        loop.volume=Mathf.MoveTowards(loop.volume,volume,Time.unscaledDeltaTime*.8f);
    }
    private void OnApplicationPause(bool value){paused=value;if(value)Silence();}
    private void OnDisable()=>Silence();
    private void Silence(){if(loop!=null){loop.Stop();loop.volume=0;}if(voice!=null)voice.Stop();briefPurrUntil=0;}
}
