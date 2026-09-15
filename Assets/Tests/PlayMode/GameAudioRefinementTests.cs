#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed partial class GameAudioTests
{
    const string RefinementRoot="Docs/QA/AUDIO_REFINEMENT_2026-09-15";
    [UnityTest] public IEnumerator Title_DecodedPlaybackCrossesPhraseGapsAndLoopWithoutDropouts()
    {
        yield return NormalHome();
        var musicController=Object.FindAnyObjectByType<TitleMusicController>();
        var source=musicController.GetComponentInChildren<AudioSource>();
        Assert.That(source.clip.loadType,Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
        Assert.That(source.clip.loadState,Is.EqualTo(AudioDataLoadState.Loaded));
        Assert.That(source.priority,Is.LessThanOrEqualTo(32));
        var decoded=new float[source.clip.samples*source.clip.channels];
        Assert.That(source.clip.GetData(decoded,0),Is.True);
        foreach(float at in new[]{11.90f,41.90f})
        {
            int offset=(int)(at*source.clip.frequency)*source.clip.channels;double power=0;
            for(int i=0;i<1920;i++)power+=decoded[offset+i]*decoded[offset+i];
            Assert.That(Math.Sqrt(power/1920),Is.GreaterThan(.0006),"Decoded PCM no longer has an artificial phrase gap at "+at);
        }
        var probe=source.gameObject.AddComponent<RefinementAudioProbe>();
        yield return new WaitForSecondsRealtime(.25f);
        foreach(float at in new[]{11.4f,41.4f,58.8f})
        {
            source.time=at;yield return new WaitForSecondsRealtime(.15f);probe.Begin();
            float until=Time.realtimeSinceStartup+2.3f;int frame=0;
            while(Time.realtimeSinceStartup<until)
            {
                Assert.That(source.isPlaying,Is.True);Assert.That(source.pitch,Is.EqualTo(1));
                // Rendering stalls must not restart / stall the predecoded audio voice.
                if(++frame%25==0)System.Threading.Thread.Sleep(70);
                yield return null;
            }
            probe.End();
            Assert.That(probe.Frames,Is.GreaterThan(48000));
            Assert.That(probe.LongestSilence,Is.LessThan(2400),"No 50 ms mixer silence at "+at);
            File.AppendAllText(RefinementRoot+"/title-mixer.txt",at+" frames="+probe.Frames+" maxZeroFrames="+probe.LongestSilence+"\n");
        }
        Assert.That(source.time,Is.InRange(.8f,3f),"Loop resumes at the head without rescheduling");
        HomeAudioService.MusicEnabled=false;float stopped=source.time;
        yield return new WaitForSecondsRealtime(.3f);Assert.That(source.isPlaying,Is.False);
        HomeAudioService.MusicEnabled=true;yield return new WaitForSecondsRealtime(.4f);
        Assert.That(source.time,Is.GreaterThan(stopped+.1f));
        yield return EnterHome();yield return new WaitForSecondsRealtime(.4f);
        Assert.That(source.clip.loadState,Is.EqualTo(AudioDataLoadState.Unloaded),"Decoded title memory is released in home");
        Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include).RequestShow();
        // Re-enabling the title reads the real desktop focus. This native run
        // explicitly simulates foreground, just like NormalHome does on first open.
        musicController.SendMessage("OnApplicationFocus",true);
        yield return new WaitForSecondsRealtime(.7f);Assert.That(source.isPlaying,Is.True,"Focused title can be reopened after unload");
    }

    [UnityTest] public IEnumerator Sleep_MusicFallsBelowCareAndReturnsAfterWake()
    {
        yield return NormalHome();yield return EnterHome();yield return DismissReturnPopup();
        var cat=Object.FindFirstObjectByType<CatMovement>();
        var sleep=cat.GetComponent<SleepInteraction>();var mix=Object.FindFirstObjectByType<GameSoundscape>();
        Assert.That(sleep.TryRestoreSleepingState(out var failure),Is.True,failure);
        yield return new WaitForSecondsRealtime(2f);
        Assert.That(mix.Selection,Is.EqualTo("Rest"));Assert.That(mix.TargetMusicVolume,Is.EqualTo(.012f).Within(.001f));
        Assert.That(mix.CurrentMusicVolume,Is.InRange(.008f,.016f));
        Assert.That(cat.GetComponent<CatVoice>().PlayingLoop,Is.EqualTo("Purr_1"));
        sleep.CancelForTransition();yield return new WaitForSecondsRealtime(2f);
        Assert.That(mix.Selection,Is.EqualTo("Home"));Assert.That(mix.CurrentMusicVolume,Is.EqualTo(.11f).Within(.01f));
    }

    IEnumerator DismissReturnPopup()
    {
        var popup=Object.FindAnyObjectByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include);
        if(popup!=null&&popup.IsOpen)
        {((Button)typeof(WhileYouWereAwayPopup).GetField("welcomeBackButton",Private).GetValue(popup)).onClick.Invoke();yield return new WaitForSecondsRealtime(.6f);}
    }

    [UnityTest] public IEnumerator Paws_ActualJoystickWalkAndRunHaveContactsAndStopQuietly()
    {
        yield return NormalHome();yield return EnterHome();yield return DismissReturnPopup();
        var cat=Object.FindFirstObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();
        var idle=cat.GetComponent<CatIdleBehavior>();if(idle!=null)idle.enabled=false;
        Object.FindFirstObjectByType<HungerSystem>().ApplySavedValue(90);Object.FindFirstObjectByType<ThirstSystem>().ApplySavedValue(90);
        var joy=(MobileJoystick)typeof(CatMovement).GetField("mobileJoystick",Private).GetValue(cat);
        Assert.That(joy,Is.Not.Null);var rect=(RectTransform)joy.transform;
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        Vector2 center=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        float radius=Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null,corners[0]),RectTransformUtility.WorldToScreenPoint(null,corners[3]))*.5f;
        int oldRate=Application.targetFrameRate,oldVSync=QualitySettings.vSyncCount;QualitySettings.vSyncCount=0;
        try
        {
            foreach(int fps in new[]{15,30,60})foreach(float strength in new[]{.5f,1f})
            {
                Application.targetFrameRate=fps;joy.CancelInput();
                cc.enabled=false;cat.transform.SetPositionAndRotation(new Vector3(-1.15f,.05f,-1.55f),Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();
                yield return new WaitForSecondsRealtime(.3f);heard.Clear();
                var pointer=new PointerEventData(EventSystem.current){pointerId=73,position=center+Vector2.right*radius*strength};
                joy.OnPointerDown(pointer);float until=Time.realtimeSinceStartup+1.25f;bool ran=false;
                while(Time.realtimeSinceStartup<until){ran|=cat.IsRunning;yield return null;}
                joy.OnPointerUp(pointer);
                int contacts=heard.Count(c=>c>=AudioCue.PawWood&&c<=AudioCue.PawFabric);
                File.AppendAllText(RefinementRoot+"/paw-cadence.txt",fps+" strength="+strength+" contacts="+contacts+" ran="+ran+"\n");
                Assert.That(contacts,Is.InRange(2,16),fps+" fps actual paw contacts; strength "+strength);
                Assert.That(ran,Is.EqualTo(strength>.9f),"Actual walk/run gait");
                yield return new WaitForSecondsRealtime(.4f);heard.Clear();yield return new WaitForSecondsRealtime(.4f);
                Assert.That(heard.Count(c=>c>=AudioCue.PawWood&&c<=AudioCue.PawFabric),Is.Zero,"Stopped cat is quiet");
            }
        }
        finally{joy.CancelInput();Application.targetFrameRate=oldRate;QualitySettings.vSyncCount=oldVSync;}
    }
}

public sealed class RefinementAudioProbe:MonoBehaviour
{
    volatile bool capture;int frames,run,longest;
    public int Frames=>frames;public int LongestSilence=>longest;
    public void Begin(){frames=run=longest=0;capture=true;}
    public void End(){capture=false;}
    void OnAudioFilterRead(float[] data,int channels)
    {
        if(!capture)return;
        for(int i=0;i<data.Length;i+=channels)
        {
            float peak=0;for(int c=0;c<channels;c++)peak=Mathf.Max(peak,Mathf.Abs(data[i+c]));
            run=peak<.0000001f?run+1:0;if(run>longest)longest=run;frames++;
        }
    }
}
#endif
