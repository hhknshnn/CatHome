using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum AudioCue
{
    UIClick, UIToggle, UIOpen, UIClose, UIError, Purchase, Coin, Diamond, Success, LevelUp, RoomChange, NewGame,
    Countdown, Start, Shield, PowerUp, Result, Catch, Combo,
    PawWood, PawTile, PawGrass, PawFabric, LandWood, LandTile, LandSoft, BallTap, FruitLand, BookLand, WoodTap, Hit, Miss,
    ScratchWood, ScratchRope, DigSand, DigSoil, Cloth, PaperTear, PaperRoll, Ribbon, Groom, Sniff, Slide, Leaves,
    GlassTap, GlassLand, CeramicTap, CeramicLand, Bell, MetalRattle, Jump, ToySqueak, Bird, Creak,
    Shake, WaterDrop, Feeder, BallRoll, WheelRoll, Record, Meow, Purr, Eat, Drink, Shower, GardenAir, Fountain, FireCrackle, Television,
    PropLand
}

public enum AudioBus { UI, World, MiniGame }

/// <summary>One bounded effect pool, shared preferences, authored variations, and no gameplay side effects.</summary>
[DefaultExecutionOrder(-800)]
[DisallowMultipleComponent]
public sealed class GameAudio : MonoBehaviour
{
    public const int VoiceLimit = 12;
    private static GameAudio instance;
    private readonly Dictionary<AudioCue, AudioClip[]> bank = new Dictionary<AudioCue, AudioClip[]>();
    private readonly Dictionary<AudioCue, int> variation = new Dictionary<AudioCue, int>();
    private readonly Dictionary<AudioCue, float> nextAllowed = new Dictionary<AudioCue, float>();
    private readonly AudioSource[] voices = new AudioSource[VoiceLimit];
    private readonly AudioBus[] voiceBuses = new AudioBus[VoiceLimit];
    private readonly float[] began = new float[VoiceLimit];
    private readonly float[] gains = new float[VoiceLimit];
    private bool focused = true, suspended;
    private float nextBind, nextAccent;
    private int lastUIClickFrame = -1;
    public static event Action<AudioCue, AudioBus> Played;
    public static bool IsSuspended => instance != null && (instance.suspended || !instance.focused);
    public static int LoadedCueCount => instance == null ? 0 : instance.bank.Count;
    public static int PlayingVoiceCount
    {
        get { int n=0; if(instance!=null)foreach(var s in instance.voices)if(s!=null&&s.isPlaying)n++; return n; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance=null; Played=null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => Ensure();

    private static void Ensure()
    {
        if(instance!=null || !Application.isPlaying)return;
        var host=new GameObject("Game Audio"); DontDestroyOnLoad(host); instance=host.AddComponent<GameAudio>();
    }

    private void Awake()
    {
        if(instance!=null&&instance!=this){Destroy(gameObject);return;} instance=this;
        var clips=Resources.LoadAll<AudioClip>("GameAudio/Sfx");
        Array.Sort(clips,(a,b)=>string.CompareOrdinal(a.name,b.name));
        foreach(AudioCue cue in Enum.GetValues(typeof(AudioCue)))
        {
            var matches=new List<AudioClip>();
            foreach(var clip in clips)if(clip.name.StartsWith(cue+"_",StringComparison.Ordinal))matches.Add(clip);
            bank[cue]=matches.ToArray();
            if(matches.Count==0)Debug.LogError("Missing game audio cue: "+cue,this);
        }
        for(int i=0;i<voices.Length;i++)
        {
            var child=new GameObject("Effect "+(i+1));child.transform.SetParent(transform,false);
            voices[i]=child.AddComponent<AudioSource>();Configure(voices[i],false,110);
        }
        gameObject.AddComponent<GameSoundscape>();
    }

    public static void Configure(AudioSource source,bool loop,int priority)
    {
        source.playOnAwake=false;source.loop=loop;source.spatialBlend=0;source.dopplerLevel=0;
        source.pitch=1;source.volume=0;source.priority=priority;
    }

    private void OnEnable()
    {
        HomeAudioService.Changed+=ApplyPreferences;
        CatRunnerProgressService.PreferencesChanged+=ApplyPreferences;
        SceneManager.sceneLoaded+=SceneLoaded;
    }
    private void OnDisable()
    {
        HomeAudioService.Changed-=ApplyPreferences;
        CatRunnerProgressService.PreferencesChanged-=ApplyPreferences;
        SceneManager.sceneLoaded-=SceneLoaded; Silence();
    }
    private void OnDestroy(){if(instance==this)instance=null;}
    private void SceneLoaded(Scene scene,LoadSceneMode mode){nextBind=0;}
    private void Update()
    {
        ApplyPreferences();
        if(Time.unscaledTime<nextBind)return;
        nextBind=Time.unscaledTime+.35f;
        GameAudioButton.BindVisibleControls();
        foreach(var cat in FindObjectsByType<CatMovement>(FindObjectsInactive.Exclude))
        {
            if(cat.GetComponent<CatFoley>()==null)cat.gameObject.AddComponent<CatFoley>();
            CatVoice.EnsureOn(cat);
        }
        foreach(var runner in FindObjectsByType<CatRunnerPlayer>(FindObjectsInactive.Exclude))
            if(runner.GetComponent<MiniGameFoley>()==null)runner.gameObject.AddComponent<MiniGameFoley>();
        foreach(var hunter in FindObjectsByType<CatCatchPlayer>(FindObjectsInactive.Exclude))
            if(hunter.GetComponent<MiniGameFoley>()==null)hunter.gameObject.AddComponent<MiniGameFoley>();
    }
    private void ApplyPreferences()
    {
        for(int i=0;i<voices.Length;i++)if(voices[i]!=null&&!Allowed(voiceBuses[i]))voices[i].Stop();
        BalanceVoices();
    }
    private void BalanceVoices()
    {
        float sum=0;for(int i=0;i<voices.Length;i++)if(voices[i]!=null&&voices[i].isPlaying)sum+=gains[i];
        float trim=sum>.78f?.78f/sum:1f;
        for(int i=0;i<voices.Length;i++)if(voices[i]!=null&&voices[i].isPlaying)voices[i].volume=gains[i]*trim;
    }
    private void OnApplicationPause(bool value){suspended=value;if(value)Silence();}
    private void OnApplicationFocus(bool value){focused=value;if(!value)Silence();}
    private void Silence(){foreach(var s in voices)if(s!=null)s.Stop();}

    public static bool Allowed(AudioBus bus)
    {
        if(!Application.isPlaying||IsSuspended||!HomeAudioService.SoundEnabled)return false;
        bool runner=SceneManager.GetSceneByName(CatRunnerLauncher.RunnerSceneName).isLoaded;
        if(bus==AudioBus.World)return !TitleScreen.IsShowing&&!HomeUiFlow.IsMiniGameVisible&&Time.timeScale>0;
        if(bus==AudioBus.MiniGame&&Time.timeScale<=0)return false;
        return !runner||CatRunnerProgressService.SoundEnabled;
    }
    public static AudioClip Clip(AudioCue cue,int take=0)
    {
        Ensure();if(instance==null||!instance.bank.TryGetValue(cue,out var clips)||clips.Length==0)return null;
        return clips[Math.Abs(take)%clips.Length];
    }
    public static bool Play(AudioCue cue,float strength=1f,AudioBus bus=AudioBus.World)
    {
        Ensure();return instance!=null&&instance.PlayInternal(cue,strength,bus);
    }
    public static bool UI(AudioCue cue=AudioCue.UIClick)=>Play(cue,1,AudioBus.UI);

    private bool PlayInternal(AudioCue cue,float strength,AudioBus bus)
    {
        if(!Allowed(bus))return false;
        float now=Time.unscaledTime;
        if(cue==AudioCue.UIClick&&lastUIClickFrame==Time.frameCount)return false;
        if(nextAllowed.TryGetValue(cue,out float next)&&now<next)return false;
        bool accent=cue==AudioCue.LevelUp||cue==AudioCue.Result||cue==AudioCue.NewGame||cue==AudioCue.Success;
        if(accent&&now<nextAccent)return false;
        int take=variation.TryGetValue(cue,out int index)?index:0;
        AudioClip clip=Clip(cue,take);if(clip==null)return false;
        int slot=-1;
        for(int i=0;i<voices.Length;i++)if(!voices[i].isPlaying){slot=i;break;}
        int priority=accent?35:bus==AudioBus.UI?65:IsFootstep(cue)?180:105;
        if(slot<0)
        {
            float oldest=float.MaxValue;
            for(int i=0;i<voices.Length;i++)if(voices[i].priority>=priority&&began[i]<oldest){slot=i;oldest=began[i];}
            if(slot<0)return false;
        }
        var source=voices[slot];source.Stop();source.clip=clip;source.pitch=1;source.mute=false;
        source.priority=priority;gains[slot]=Gain(cue)*Mathf.Clamp(strength,0,1.5f);source.volume=gains[slot];source.Play();
        voiceBuses[slot]=bus;began[slot]=now;variation[cue]=take+1;
        BalanceVoices();
        nextAllowed[cue]=now+(IsFootstep(cue)?.085f:cue==AudioCue.Coin?.055f:cue==AudioCue.Meow?3f:accent?1f:.075f);
        if(accent)nextAccent=now+.85f;
        if(cue==AudioCue.UIClick)lastUIClickFrame=Time.frameCount;
        Played?.Invoke(cue,bus);return true;
    }
    private static bool IsFootstep(AudioCue cue)=>cue>=AudioCue.PawWood&&cue<=AudioCue.PawFabric;
    private static float Gain(AudioCue cue)
    {
        if(IsFootstep(cue))return .09f;
        if(cue==AudioCue.Meow)return .32f;
        if(cue==AudioCue.LevelUp||cue==AudioCue.Result||cue==AudioCue.NewGame)return .36f;
        if(cue<=AudioCue.UIError)return .19f;
        if(cue==AudioCue.Coin||cue==AudioCue.Diamond||cue==AudioCue.Catch||cue==AudioCue.Combo)return .23f;
        if(cue==AudioCue.Sniff||cue==AudioCue.Cloth||cue==AudioCue.Groom||cue==AudioCue.Creak)return .12f;
        return .25f;
    }
}
