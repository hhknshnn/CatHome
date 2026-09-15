using UnityEngine;

/// <summary>Recorded care foley and stylized voice, tied to the actual care/rest state.</summary>
[DefaultExecutionOrder(600),DisallowMultipleComponent]
public sealed class CatVoice : MonoBehaviour
{
    private AudioSource loop,voice;
    private AudioClip purr,eat,drink;
    private BowlInteraction bowls;
    private SleepInteraction sleep;
    private PetInteraction pet;
    private CatActivityAnimation activity;
    private float nextMeow,briefPurrUntil;
    private int meowTake;
    public string PlayingLoop=>loop!=null&&loop.isPlaying&&loop.clip!=null?loop.clip.name:string.Empty;
    public bool IsVocalizing=>voice!=null&&voice.isPlaying;
    public float MeowDuration=>voice!=null&&voice.clip!=null?voice.clip.length:1;
    public static CatVoice EnsureOn(CatMovement cat)=>cat.GetComponent<CatVoice>()??cat.gameObject.AddComponent<CatVoice>();
    private void Awake()
    {
        purr=GameAudio.Clip(AudioCue.Purr);eat=GameAudio.Clip(AudioCue.Eat);drink=GameAudio.Clip(AudioCue.Drink);
        loop=gameObject.AddComponent<AudioSource>();voice=gameObject.AddComponent<AudioSource>();
        GameAudio.Configure(loop,true,100);GameAudio.Configure(voice,false,65);voice.volume=.30f;
        bowls=GetComponent<BowlInteraction>();sleep=GetComponent<SleepInteraction>();pet=GetComponent<PetInteraction>();
    }
    private void OnEnable()=>HomeAudioService.Changed+=ApplyPreferences;
    private void ApplyPreferences(){if(!GameAudio.Allowed(AudioBus.World))Silence();}
    public bool Meow()
    {
        if(!isActiveAndEnabled||!GameAudio.Allowed(AudioBus.World)||Time.unscaledTime<nextMeow)return false;
        var clip=GameAudio.Clip(AudioCue.Meow,meowTake++);if(clip==null)return false;
        nextMeow=Time.unscaledTime+3;voice.pitch=1;voice.clip=clip;voice.Play();return true;
    }
    public void PurrBriefly()=>briefPurrUntil=Time.time+2.5f;
    private void Update()
    {
        if(!GameAudio.Allowed(AudioBus.World)){Silence();return;}
        if(activity==null)activity=GetComponent<CatActivityAnimation>();
        AudioClip desired=null;float volume=.11f;
        if(bowls!=null&&bowls.IsInteracting)
        {if(bowls.ActiveCareSound=="CatEat"){desired=eat;volume=.34f;}else if(bowls.ActiveCareSound=="CatDrink"){desired=drink;volume=.42f;}}
        else if(activity!=null&&activity.IsActive)
        {
            if(activity.CurrentPose==CatActivityPose.Eat){desired=eat;volume=.34f;}
            else if(activity.CurrentPose==CatActivityPose.Drink){desired=drink;volume=.42f;}
            else if(activity.CurrentPose==CatActivityPose.Sleep||CatActivity.Active!=null&&CatActivity.Active.IsWaitingForRestStop)desired=purr;
        }
        else if(sleep!=null&&sleep.IsSleeping)desired=purr;
        else if(pet!=null&&pet.IsPetting||Time.time<briefPurrUntil){desired=purr;volume=.16f;}
        if(desired!=loop.clip)
        {
            loop.volume=Mathf.MoveTowards(loop.volume,0,Time.unscaledDeltaTime*1.4f);
            if(loop.volume>.001f)return;loop.Stop();loop.clip=desired;
        }
        if(desired==null)return;
        if(!loop.isPlaying)loop.Play();loop.volume=Mathf.MoveTowards(loop.volume,volume,Time.unscaledDeltaTime*.8f);
    }
    private void OnApplicationPause(bool value){if(value)Silence();}
    private void OnApplicationFocus(bool value){if(!value)Silence();}
    private void OnDisable(){HomeAudioService.Changed-=ApplyPreferences;Silence();}
    private void Silence(){if(loop!=null){loop.Stop();loop.volume=0;}if(voice!=null)voice.Stop();briefPurrUntil=0;}
}
