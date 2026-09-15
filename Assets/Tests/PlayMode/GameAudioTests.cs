#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed partial class GameAudioTests
{
    private bool sound,music,editorMute;
    private GameAudio audio;
    private readonly List<AudioCue> heard=new List<AudioCue>();
    private CatRunnerProgressSaveState runnerState;
    private CatchLivesSaveState catchState;
    private RunnerEnergySaveState energyState;
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    [UnitySetUp] public IEnumerator Before()
    {
        Assert.That(EditorQaSession.IsActive,Is.True);
        sound=HomeAudioService.SoundEnabled;music=HomeAudioService.MusicEnabled;editorMute=UnityEditor.EditorUtility.audioMasterMute;
        runnerState=CatRunnerProgressService.CaptureState(DateTime.UtcNow);catchState=CatchLivesService.CaptureState(DateTime.UtcNow);
        energyState=RunnerEnergyService.CaptureState(DateTime.UtcNow);
        Time.timeScale=1;HomeAudioService.SoundEnabled=true;HomeAudioService.MusicEnabled=true;
        GameAudio.Clip(AudioCue.UIClick);audio=Object.FindFirstObjectByType<GameAudio>();Assert.That(audio,Is.Not.Null);
        audio.SendMessage("OnApplicationFocus",true);audio.SendMessage("OnApplicationPause",false);
        UnityEditor.EditorUtility.audioMasterMute=false;
        heard.Clear();GameAudio.Played+=Heard;yield return new WaitForSecondsRealtime(.15f);
    }
    private void Heard(AudioCue cue,AudioBus bus)=>heard.Add(cue);
    [UnityTearDown] public IEnumerator After()
    {
        GameAudio.Played-=Heard;Time.timeScale=1;
        if(CatActivity.Active!=null)CatActivity.Active.CancelForTransition();
        RoomPlayModeSupport.ReleaseRoom();
        foreach(string name in new[]{CatRunnerLauncher.RunnerSceneName,CatCatchLauncher.CatchSceneName})
        {var scene=SceneManager.GetSceneByName(name);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);}
        CatRunnerProgressService.ApplySavedState(runnerState,DateTime.UtcNow);CatchLivesService.ApplySavedState(catchState,DateTime.UtcNow);
        RunnerEnergyService.ApplySavedState(energyState,DateTime.UtcNow);
        HomeAudioService.SoundEnabled=sound;HomeAudioService.MusicEnabled=music;UnityEditor.EditorUtility.audioMasterMute=editorMute;
        if(audio!=null){audio.SendMessage("OnApplicationPause",false);audio.SendMessage("OnApplicationFocus",true);}
    }
    private IEnumerator NormalHome()
    {
        DirectLevelPlayBootstrap.RedirectSuppressed=false;
        yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);
        float until=Time.realtimeSinceStartup+20;
        while((Object.FindFirstObjectByType<CatMovement>()==null||!Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Any(a=>a.isActiveAndEnabled))&&Time.realtimeSinceStartup<until)yield return null;
        var title=Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);Assert.That(title,Is.Not.Null);title.RequestShow();
        title.GetComponent<TitleMusicController>().SendMessage("OnApplicationFocus",true);
        audio.SendMessage("OnApplicationFocus",true);yield return new WaitForSecondsRealtime(.7f);
    }
    private IEnumerator EnterHome()
    {
        var title=Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
        ((Button)typeof(TitleScreen).GetField("playButton",Private).GetValue(title)).onClick.Invoke();
        yield return new WaitForSecondsRealtime(1.5f);
        Assert.That(TitleScreen.IsShowing,Is.False);
    }

    [UnityTest] public IEnumerator Bank_AllAuthoredClipsDecodeAndMusicStreams()
    {
        Assert.That(GameAudio.LoadedCueCount,Is.EqualTo(Enum.GetValues(typeof(AudioCue)).Length));
        var clips=Resources.LoadAll<AudioClip>("GameAudio/Sfx");Assert.That(clips.Length,Is.EqualTo(122));
        float highest=0;
        foreach(var clip in clips)
        {
            Assert.That(clip.loadType,Is.EqualTo(AudioClipLoadType.DecompressOnLoad),clip.name);
            Assert.That(clip.frequency,Is.EqualTo(48000));Assert.That(clip.channels,Is.EqualTo(1));
            var data=new float[clip.samples];Assert.That(clip.GetData(data,0),Is.True,clip.name);
            float peak=0;bool finite=true;foreach(float v in data){finite&=!float.IsNaN(v)&&!float.IsInfinity(v);peak=Mathf.Max(peak,Mathf.Abs(v));}
            Assert.That(finite,Is.True,clip.name);
            highest=Mathf.Max(highest,peak);Assert.That(peak,Is.InRange(.1f,.99f),clip.name);
        }
        foreach(string name in new[]{"Home","Outdoor","Rest","Runner","Catch"})
        {
            var clip=Resources.Load<AudioClip>("GameAudio/Music/"+name);Assert.That(clip,Is.Not.Null,name);
            Assert.That(clip.loadType,Is.EqualTo(AudioClipLoadType.Streaming));Assert.That(clip.length,Is.InRange(59f,90f));
        }
        Debug.Log("Audio decoded peak: "+highest+"; 122 effects and 5 streaming music clips.");yield return null;
    }

    [UnityTest] public IEnumerator FirstLaunch_TitleButtonAndHomeMusicUseOneSystem()
    {
        yield return NormalHome();var soundscape=Object.FindFirstObjectByType<GameSoundscape>();
        Assert.That(soundscape.PlayingMusicSources,Is.Zero,"Title owns its accepted theme.");
        Assert.That(GameAudio.Play(AudioCue.PawWood),Is.False,"World sounds must stay behind the title.");
        var title=Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
        var button=(Button)typeof(TitleScreen).GetField("playButton",Private).GetValue(title);
        Assert.That(button.GetComponent<GameAudioButton>(),Is.Not.Null);
        heard.Clear();yield return EnterHome();
        Assert.That(heard.Contains(AudioCue.UIClick),Is.True,"Actual Continue button produces feedback.");
        Assert.That(soundscape.Selection,Is.EqualTo("Home"));Assert.That(soundscape.PlayingMusicSources,Is.EqualTo(1));
        Assert.That(Object.FindObjectsByType<GameAudio>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
        Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.isActiveAndEnabled),Is.EqualTo(1));
    }

    [UnityTest] public IEnumerator PreferencesFocusAndPauseSilenceTheCorrectBuses()
    {
        yield return NormalHome();yield return EnterHome();
        HomeAudioService.SoundEnabled=false;
        Assert.That(GameAudio.UI(),Is.False);Assert.That(GameAudio.Play(AudioCue.Jump),Is.False);
        Assert.That(GameAudio.PlayingVoiceCount,Is.Zero);
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(Object.FindFirstObjectByType<GameSoundscape>().PlayingMusicSources,Is.EqualTo(1));
        HomeAudioService.SoundEnabled=true;HomeAudioService.MusicEnabled=false;yield return null;
        Assert.That(Object.FindFirstObjectByType<GameSoundscape>().PlayingMusicSources,Is.Zero);
        Assert.That(GameAudio.UI(AudioCue.PowerUp),Is.True);
        audio.SendMessage("OnApplicationFocus",false);yield return null;
        Assert.That(GameAudio.PlayingVoiceCount,Is.Zero);Assert.That(GameAudio.UI(),Is.False);
        HomeAudioService.MusicEnabled=true;yield return null;
        Assert.That(Object.FindFirstObjectByType<GameSoundscape>().PlayingMusicSources,Is.Zero);
        audio.SendMessage("OnApplicationFocus",true);yield return new WaitForSecondsRealtime(.3f);
        Assert.That(Object.FindFirstObjectByType<GameSoundscape>().PlayingMusicSources,Is.EqualTo(1));
        audio.SendMessage("OnApplicationPause",true);yield return null;
        Assert.That(GameAudio.UI(),Is.False);Assert.That(Object.FindFirstObjectByType<GameSoundscape>().PlayingMusicSources,Is.Zero);
        audio.SendMessage("OnApplicationPause",false);Time.timeScale=0;
        Assert.That(GameAudio.Play(AudioCue.BallTap),Is.False);Assert.That(GameAudio.Play(AudioCue.Hit,1,AudioBus.MiniGame),Is.False);
        Assert.That(GameAudio.UI(),Is.True,"Pause menus remain audible.");
    }

    [UnityTest] public IEnumerator BurstPoolIsBoundedAndRepeatedActionsDoNotStack()
    {
        yield return NormalHome();yield return EnterHome();heard.Clear();
        Assert.That(GameAudio.UI(),Is.True);Assert.That(GameAudio.UI(),Is.False);
        Assert.That(heard.Count(c=>c==AudioCue.UIClick),Is.EqualTo(1));
        foreach(AudioCue cue in Enum.GetValues(typeof(AudioCue)))GameAudio.Play(cue,1,AudioBus.UI);
        Assert.That(GameAudio.PlayingVoiceCount,Is.LessThanOrEqualTo(GameAudio.VoiceLimit));
        var pool=audio.GetComponentsInChildren<AudioSource>().Where(s=>s.name.StartsWith("Effect ")).ToArray();
        Assert.That(pool.Length,Is.EqualTo(12));Assert.That(pool.Where(s=>s.isPlaying).Sum(s=>s.volume),Is.LessThanOrEqualTo(.781f));
        foreach(var source in pool)Assert.That(source.pitch,Is.EqualTo(1f));
        var samples=new float[2048];foreach(var s in pool)s.GetOutputData(samples,0);
        yield return new WaitForSecondsRealtime(.09f);float peak=0;
        foreach(var s in pool){s.GetOutputData(samples,0);foreach(float v in samples)peak=Mathf.Max(peak,Mathf.Abs(v));}
        Assert.That(peak,Is.GreaterThan(.00001f),"Real mixer output, not only Play flags.");
        HomeAudioService.SoundEnabled=false;Assert.That(GameAudio.PlayingVoiceCount,Is.Zero);
        HomeAudioService.SoundEnabled=true;yield return new WaitForSecondsRealtime(.2f);
        Assert.That(GameAudio.PlayingVoiceCount,Is.Zero,"Muting clears old tails instead of resuming them.");
    }

    [UnityTest] public IEnumerator HomeMusicLoopsAndResumesAtItsPosition()
    {
        yield return NormalHome();yield return EnterHome();
        var musicSource=audio.GetComponentsInChildren<AudioSource>().Single(s=>s.loop&&s.clip!=null&&s.clip.name=="Home"&&s.isPlaying);
        musicSource.time=musicSource.clip.length-.25f;yield return new WaitForSecondsRealtime(.65f);
        Assert.That(musicSource.time,Is.InRange(.1f,1.2f));
        HomeAudioService.MusicEnabled=false;float time=musicSource.time;yield return new WaitForSecondsRealtime(.3f);
        Assert.That(musicSource.time,Is.EqualTo(time).Within(.05f));
        HomeAudioService.MusicEnabled=true;yield return new WaitForSecondsRealtime(.3f);
        Assert.That(musicSource.time,Is.GreaterThan(time+.1f));Assert.That(musicSource.pitch,Is.EqualTo(1));
    }

    [UnityTest] public IEnumerator BasicCareButtonsAndPettingAudioShareOneVoice()
    {
        yield return NormalHome();yield return EnterHome();
        var loader=Object.FindFirstObjectByType<LevelLoader>();Assert.That(loader.LoadRoom(HomeRoomService.LivingRoomId),Is.True);
        float roomDeadline=Time.realtimeSinceStartup+15;
        while((!loader.IsReady||Object.FindFirstObjectByType<CatMovement>()==null||
            Object.FindFirstObjectByType<CatMovement>().gameObject.scene.name!="LivingRoom_Level01")&&Time.realtimeSinceStartup<roomDeadline)yield return null;
        yield return new WaitForSecondsRealtime(.5f);
        var returning=Object.FindAnyObjectByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include);
        Assert.That(returning,Is.Not.Null);
        // Long suites can naturally produce an offline-return popup. Exercise
        // its real close button on every run, including quick standalone runs.
        if(!returning.IsOpen)typeof(WhileYouWereAwayPopup).GetMethod("Show",Private).Invoke(returning,null);
        yield return new WaitForSecondsRealtime(.5f);
        var welcome=(Button)typeof(WhileYouWereAwayPopup).GetField("welcomeBackButton",Private).GetValue(returning);
        Assert.That(welcome.isActiveAndEnabled,Is.True);heard.Clear();welcome.onClick.Invoke();
        yield return new WaitForSecondsRealtime(.6f);Assert.That(WhileYouWereAwayPopup.IsAnyOpen,Is.False);
        Assert.That(heard.Any(c=>c==AudioCue.UIClose||c==AudioCue.UIClick),Is.True,"Actual return popup button sound");
        var cat=Object.FindFirstObjectByType<CatMovement>();var idle=cat.GetComponent<CatIdleBehavior>();if(idle!=null)idle.enabled=false;
        var bowls=cat.GetComponent<BowlInteraction>();var voice=cat.GetComponent<CatVoice>();var cc=cat.GetComponent<CharacterController>();
        foreach(string trial in new[]{"food","water","water-complete"})
        {
            string kind=trial=="food"?"food":"water";
            if(trial=="water-complete")typeof(BowlInteraction).GetField("drinkingDuration",Private).SetValue(bowls,2.5f);
            Object.FindFirstObjectByType<HungerSystem>().ApplySavedValue(30);Object.FindFirstObjectByType<ThirstSystem>().ApplySavedValue(30);
            var setup=(BowlInteraction.BowlSetup)typeof(BowlInteraction).GetField(kind,Private).GetValue(bowls);setup.Fill();
            cc.enabled=false;cat.transform.SetPositionAndRotation(setup.InteractionPoint.position+new Vector3(.12f,.05f,-.12f),Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.5f);
            var button=(Button)typeof(BowlInteraction).GetField("interactionButton",Private).GetValue(bowls);
            Assert.That(button.isActiveAndEnabled&&button.interactable,Is.True,kind+" actual care button; room="+HomeRoomService.CurrentRoomId+
                " blocked="+HomeUiFlow.IsHomeControlBlocked+" away="+WhileYouWereAwayPopup.IsAnyOpen+" actions="+cat.AreWorldActionsBlocked+
                " busy="+CatActionState.IsBusy(cat)+" visible="+CareInteractionTarget.IsVisibleInRoom(setup.Bowl,cat.transform)+
                " distance="+CareInteractionTarget.NearbyDistanceSquared(setup.InteractionPoint,cat.transform,.45f)+" position="+cat.transform.position);
            Assert.That(button.GetComponent<GameAudioButton>(),Is.Not.Null);heard.Clear();button.onClick.Invoke();
            Assert.That(bowls.IsInteracting,Is.True);Assert.That(heard.Contains(AudioCue.UIClick),Is.True);
            float until=Time.realtimeSinceStartup+8;
            while(bowls.IsInteracting&&bowls.ActiveCareSound==null&&Time.realtimeSinceStartup<until)
            {Assert.That(voice.PlayingLoop,Is.Empty,"No chewing or lapping while approaching");yield return null;}
            Assert.That(bowls.ActiveCareSound,Is.Not.Null);yield return new WaitForSecondsRealtime(1f);
            Assert.That(voice.PlayingLoop,Is.EqualTo(kind=="food"?"Eat_1":"Drink_1"));
            var mix=Object.FindFirstObjectByType<GameSoundscape>();
            Assert.That(mix.TargetMusicVolume,Is.EqualTo(.025f).Within(.001f),kind+" music duck");
            Assert.That(mix.CurrentMusicVolume,Is.InRange(.015f,.04f),kind+" audible care foreground");
            Assert.That(cat.GetComponents<AudioSource>().Count(s=>s.loop&&s.isPlaying),Is.EqualTo(1));
            if(trial=="water-complete")
            {
                float completedBy=Time.realtimeSinceStartup+6;
                while(bowls.ActiveCareSound!=null&&Time.realtimeSinceStartup<completedBy)yield return null;
                Assert.That(bowls.ActiveCareSound,Is.Null,"Lapping ends before the natural exit");
                Assert.That(Object.FindFirstObjectByType<ThirstSystem>().CurrentThirst,Is.GreaterThanOrEqualTo(99f),"Actual need recovery completed");
                while(bowls.IsInteracting&&Time.realtimeSinceStartup<completedBy)yield return null;
                Assert.That(bowls.IsInteracting,Is.False,"Natural exit completed");
            }
            else bowls.CancelInteraction();
            yield return new WaitForSecondsRealtime(.35f);Assert.That(voice.PlayingLoop,Is.Empty);
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(mix.CurrentMusicVolume,Is.EqualTo(.11f).Within(.01f),"Music recovers after cancel");
        }
        var pet=cat.GetComponent<PetSoundController>();Assert.That(pet,Is.Not.Null);
        pet.BeginPettingAudio();voice.PurrBriefly();yield return new WaitForSecondsRealtime(.25f);
        Assert.That(voice.PlayingLoop,Is.EqualTo("Purr_1"));
        Assert.That(cat.GetComponents<AudioSource>().Count(s=>s.loop&&s.isPlaying),Is.EqualTo(1),"Petting cannot double the purr");
        HomeAudioService.SoundEnabled=false;yield return null;Assert.That(cat.GetComponents<AudioSource>().Any(s=>s.isPlaying),Is.False);
        pet.EndPettingAudio();HomeAudioService.SoundEnabled=true;yield return null;Assert.That(voice.PlayingLoop,Is.Empty,"Muted pet tails stay cleared");
    }

    [UnityTest] public IEnumerator Runner_ActualCountdownPickupJumpSlideAndSettings()
    {
        yield return NormalHome();yield return EnterHome();
        var progress=CatRunnerProgressSaveState.CreateDefault(DateTime.UtcNow);progress.tutorialCompleted=true;progress.soundEnabled=true;progress.hapticsEnabled=false;
        CatRunnerProgressService.ApplySavedState(progress,DateTime.UtcNow);
        RunnerEnergyService.ApplySavedState(new RunnerEnergySaveState{energy=5,regenerationAnchorUtc=DateTime.UtcNow.ToString("O"),unlimitedUntilUtc="",rewardedAdsDayUtc=DateTime.UtcNow.ToString("yyyy-MM-dd")},DateTime.UtcNow);
        yield return SceneManager.LoadSceneAsync(CatRunnerLauncher.RunnerSceneName,LoadSceneMode.Additive);
        yield return new WaitForSecondsRealtime(.6f);
        var game=Object.FindFirstObjectByType<CatRunnerGameController>();var player=Object.FindFirstObjectByType<CatRunnerPlayer>();
        Assert.That(Object.FindFirstObjectByType<GameSoundscape>().Selection,Is.EqualTo("Runner"));
        heard.Clear();game.StartFromWelcome();yield return new WaitForSecondsRealtime(2.7f);
        Assert.That(game.IsGameplayActive,Is.True);Assert.That(heard.Contains(AudioCue.Countdown)&&heard.Contains(AudioCue.Start),Is.True);
        game.RegisterCoin();Assert.That(heard.Contains(AudioCue.Coin),Is.True);
        player.SendMessage("Jump");yield return new WaitForSecondsRealtime(.7f);
        Assert.That(heard.Contains(AudioCue.Jump),Is.True);
        player.SendMessage("Slide");yield return null;Assert.That(heard.Contains(AudioCue.Slide),Is.True);
        game.PauseRun();yield return null;Assert.That(GameAudio.Play(AudioCue.Jump,1,AudioBus.MiniGame),Is.False);
        game.ResumeRun();CatRunnerProgressService.SetSoundEnabled(false);yield return null;
        Assert.That(GameAudio.Play(AudioCue.Coin,1,AudioBus.MiniGame),Is.False);
        Assert.That(Object.FindFirstObjectByType<GameSoundscape>().PlayingMusicSources,Is.Zero);
        CatRunnerProgressService.SetSoundEnabled(true);HomeAudioService.SoundEnabled=false;yield return null;
        Assert.That(GameAudio.Play(AudioCue.Coin,1,AudioBus.MiniGame),Is.False,"Home Sound is the master effect switch.");
        Assert.That(Object.FindFirstObjectByType<GameSoundscape>().PlayingMusicSources,Is.GreaterThan(0));
    }

    [UnityTest] public IEnumerator Catch_RealHuntProducesPounceLandingAndCatchAudio()
    {
        yield return NormalHome();yield return EnterHome();
        CatchLivesService.ApplySavedState(null,DateTime.UtcNow);CatchLivesService.CompleteTutorial();
        yield return SceneManager.LoadSceneAsync(CatCatchLauncher.CatchSceneName,LoadSceneMode.Additive);
        yield return new WaitForSecondsRealtime(.6f);
        var game=Object.FindFirstObjectByType<CatCatchGameController>();var player=Object.FindFirstObjectByType<CatCatchPlayer>();
        Assert.That(Object.FindFirstObjectByType<GameSoundscape>().Selection,Is.EqualTo("Catch"));
        heard.Clear();game.StartHunt();float until=Time.realtimeSinceStartup+15;
        while(game.Catches<1&&Time.realtimeSinceStartup<until)
        {
            if(!player.IsBusy&&player.Prey==null)
            {
                var prey=Object.FindObjectsByType<CatCatchMouse>(FindObjectsSortMode.None).Where(m=>m.IsCatchable).OrderBy(m=>(m.transform.position-player.Position).sqrMagnitude).FirstOrDefault();
                if(prey!=null)player.ChasePrey(prey);
            }
            yield return null;
        }
        Assert.That(game.Catches,Is.GreaterThan(0));
        Assert.That(heard.Contains(AudioCue.Start)&&heard.Contains(AudioCue.Jump)&&heard.Contains(AudioCue.LandSoft)&&heard.Contains(AudioCue.Catch),Is.True,string.Join(",",heard));
        game.PauseHunt();yield return null;Assert.That(GameAudio.Play(AudioCue.Catch,1,AudioBus.MiniGame),Is.False);
        game.ResumeHunt();typeof(CatCatchGameController).GetMethod("FinishHunt",Private).Invoke(game,new object[]{true});
        Assert.That(heard.Contains(AudioCue.Result),Is.True);
    }
}
#endif
