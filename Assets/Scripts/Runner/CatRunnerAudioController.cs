using UnityEngine;

/// <summary>Runner events use the authored palette and shared master preferences.</summary>
[DisallowMultipleComponent]
public sealed class CatRunnerAudioController : MonoBehaviour
{
    [SerializeField] private CatRunnerPlayer player;
    [SerializeField] private AudioSource musicSource,effectsSource;
    private float lastCoinSound=-10;
    private void Awake()
    {
        if(player==null)player=FindFirstObjectByType<CatRunnerPlayer>();
        SilenceLegacySources();
    }
    private void OnEnable(){if(player!=null){player.JumpStarted+=PlayJump;player.SlideStarted+=PlaySlide;player.Landed+=PlayLanding;}}
    private void OnDisable(){if(player!=null){player.JumpStarted-=PlayJump;player.SlideStarted-=PlaySlide;player.Landed-=PlayLanding;}SilenceLegacySources();}
    private void SilenceLegacySources(){if(musicSource!=null){musicSource.Stop();musicSource.playOnAwake=false;}if(effectsSource!=null){effectsSource.Stop();effectsSource.playOnAwake=false;}}
    public void BeginMusic(){ /* GameSoundscape owns welcome, running and result music. */ }
    public void StopMusic()=>SilenceLegacySources();
    public void SetPaused(bool value){ /* GameSoundscape observes the actual paused state. */ }
    private static void Play(AudioCue cue,float gain=1)=>GameAudio.Play(cue,gain,AudioBus.MiniGame);
    public void PlayCountdown()=>Play(AudioCue.Countdown,.85f);
    public void PlayStart()=>Play(AudioCue.Start);
    public void PlayCoin(){if(Time.unscaledTime-lastCoinSound<.055f)return;lastCoinSound=Time.unscaledTime;Play(AudioCue.Coin,.8f);}
    public void PlayHit(){Play(AudioCue.Hit);Vibrate();}
    public void PlayShieldBlock(){Play(AudioCue.Shield);Vibrate();}
    public void PlayPowerUp()=>Play(AudioCue.PowerUp);
    public void PlayResult()=>Play(AudioCue.Result);
    public void PlayButton()=>GameAudio.UI();
    private void PlayJump()=>Play(AudioCue.Jump,.8f);
    private void PlaySlide()=>Play(AudioCue.Slide,.8f);
    private void PlayLanding()=>Play(AudioCue.LandSoft,.65f);
    private static void Vibrate()
    {
        if(!CatRunnerProgressService.HapticsEnabled)return;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
#if UNITY_EDITOR
    public void EditorConfigure(CatRunnerPlayer runner,AudioSource music,AudioSource effects)
    {player=runner;musicSource=music;effectsSource=effects;}
#endif
}
