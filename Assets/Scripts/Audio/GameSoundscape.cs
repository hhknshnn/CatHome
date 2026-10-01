using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-aware music crossfades; title keeps its accepted dedicated arrangement.</summary>
public sealed class GameSoundscape : MonoBehaviour
{
    [SerializeField] private AudioSource[] music=new AudioSource[2];
    private readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
    [SerializeField] private AudioSource air;
    [SerializeField] private string selection="";
    [SerializeField] private int current;
    private float nextResolve;
    private CatMovement cat;
    private SleepInteraction sleep;
    private BowlInteraction bowls;
    private CatActivityAnimation activity;
    private CatRunnerGameController runner;
    private CatCatchGameController hunt;
    private bool outdoor,mini;
    public string Selection=>selection;
    public float TargetMusicVolume { get; private set; }
    public float CurrentMusicVolume { get { float value=0;foreach(var s in music)if(s!=null&&s.isPlaying)value+=s.volume;return value; } }
    public int PlayingMusicSources { get { int n=0;foreach(var s in music)if(s!=null&&s.isPlaying)n++;return n; } }

    private void Awake()
    {
        for(int i=0;i<2;i++)
        {
            var host=new GameObject("Music "+(i+1));host.transform.SetParent(transform,false);
            music[i]=host.AddComponent<AudioSource>();GameAudio.Configure(music[i],true,190);
        }
        air=gameObject.AddComponent<AudioSource>();GameAudio.Configure(air,true,220);air.clip=GameAudio.Clip(AudioCue.GardenAir);
    }
    private void OnEnable(){SceneManager.sceneLoaded+=SceneLoaded;HomeAudioService.Changed+=Preferences;}
    private void OnDisable(){SceneManager.sceneLoaded-=SceneLoaded;HomeAudioService.Changed-=Preferences;foreach(var s in music)if(s!=null)s.Stop();if(air!=null)air.Stop();}
    private void SceneLoaded(Scene scene,LoadSceneMode mode){nextResolve=0;}
    private void Preferences(){if(!HomeAudioService.MusicEnabled)foreach(var s in music)if(s!=null)s.Pause();if(!HomeAudioService.SoundEnabled&&air!=null)air.Stop();}

    private void Update()
    {
        if(Time.unscaledTime>=nextResolve)
        {
            nextResolve=Time.unscaledTime+.2f;
            if(cat==null)cat=FindAnyObjectByType<CatMovement>();
            if(sleep==null&&cat!=null)sleep=cat.GetComponent<SleepInteraction>();
            if(bowls==null&&cat!=null)bowls=cat.GetComponent<BowlInteraction>();
            if(activity==null&&cat!=null)activity=cat.GetComponent<CatActivityAnimation>();
            if(runner==null)runner=FindAnyObjectByType<CatRunnerGameController>();
            if(hunt==null)hunt=FindAnyObjectByType<CatCatchGameController>();
            string room=HomeRoomService.CurrentRoomId;
            outdoor=room==HomeRoomService.GardenId||room==HomeRoomService.BalconyId||room==HomeRoomService.PatioId;
            mini=HomeUiFlow.IsMiniGameVisible;
            string wanted="";
            if(runner!=null)wanted="Runner";
            else if(hunt!=null)wanted="Catch";
            else if(!TitleScreen.IsShowing&&!mini&&cat!=null)
            {
                bool resting=sleep!=null&&sleep.IsSleeping||CatActivity.Active!=null&&CatActivity.Active.IsWaitingForRestStop;
                wanted=resting?"Rest":outdoor?"Outdoor":"Home";
            }
            Select(wanted);
        }
        bool musicAllowed=HomeAudioService.MusicEnabled&&!GameAudio.IsSuspended&&(runner==null||CatRunnerProgressService.SoundEnabled);
        float level=selection=="Runner"?.24f:selection=="Catch"?.22f:selection=="Rest"?.018f:selection=="Outdoor"?.12f:.11f;
        bool home=selection=="Home"||selection=="Outdoor"||selection=="Rest";
        bool care=bowls!=null&&bowls.IsInteracting&&!string.IsNullOrEmpty(bowls.ActiveCareSound)||
            activity!=null&&activity.IsActive&&(activity.CurrentPose==CatActivityPose.Eat||activity.CurrentPose==CatActivityPose.Drink);
        bool sleeping=sleep!=null&&sleep.IsSleeping||activity!=null&&activity.IsActive&&activity.CurrentPose==CatActivityPose.Sleep;
        if(home&&care)level=Mathf.Min(level,.025f);
        if(home&&sleeping)level=Mathf.Min(level,.012f);
        bool paused=Time.timeScale<=0||runner!=null&&runner.IsPaused||hunt!=null&&hunt.IsPaused;
        if(paused)level*=.35f;
        if(runner!=null&&!runner.IsRunning||hunt!=null&&!hunt.IsHunting)level*=.62f;
        if(CatActivity.Active!=null&&CatActivity.Active.IsRunning&&selection!="Rest"&&!care)level*=.72f;
        // The scene record takes the music slot while its switch is ON.
        // Its own source shares music preference, focus and pause ownership.
        if (home && RecordPlayerMusic.HasActiveRecord) level = 0;
        TargetMusicVolume=musicAllowed&&selection!=""?level:0;
        for(int i=0;i<2;i++)
        {
            var s=music[i];if(s==null||s.clip==null)continue;
            if(!musicAllowed){s.Pause();s.volume=0;continue;}
            float target=i==current&&selection!=""?level:0;
            if(target>0&&!s.isPlaying){s.UnPause();if(!s.isPlaying)s.Play();}
            s.volume=Mathf.MoveTowards(s.volume,target,Time.unscaledDeltaTime*(target<s.volume?.18f:.07f));
            if(target==0&&s.volume<.001f)s.Stop();
        }
        bool ambient=outdoor&&!mini&&!TitleScreen.IsShowing&&GameAudio.Allowed(AudioBus.World)&&cat!=null;
        if(!ambient){air.Stop();air.volume=0;}
        else {air.volume=Mathf.MoveTowards(air.volume,.027f,Time.unscaledDeltaTime*.02f);if(!air.isPlaying)air.Play();}
    }
    private void Select(string wanted)
    {
        if(wanted==selection)return;selection=wanted;
        if(wanted=="")return;
        current=1-current;var s=music[current];s.Stop();s.volume=0;
        if(!clips.TryGetValue(wanted,out var clip)){clip=Resources.Load<AudioClip>("GameAudio/Music/"+wanted);clips[wanted]=clip;}
        s.clip=clip;
        if(clip==null)Debug.LogError("Missing music: "+wanted,this);
    }
}
