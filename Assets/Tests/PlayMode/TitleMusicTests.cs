#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class TitleMusicTests
{
    private TitleScreen title;
    private TitleMusicController music;
    private AudioSource source;
    private bool originalMusic, originalSound;
    private bool originalEditorMute;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Use an isolated save for the real title screen.");
        originalMusic = HomeAudioService.MusicEnabled;
        originalSound = HomeAudioService.SoundEnabled;
        originalEditorMute = UnityEditor.EditorUtility.audioMasterMute;
        title = Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
        if (title == null)
        {
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("GameScene");
            float deadline = Time.realtimeSinceStartup + 15f;
            while (title == null && Time.realtimeSinceStartup < deadline)
            {
                title = Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
                yield return null;
            }
        }
        Assert.That(title, Is.Not.Null);
        float listenerDeadline = Time.realtimeSinceStartup + 20f;
        AudioListener listener = null;
        while (listener == null && Time.realtimeSinceStartup < listenerDeadline)
        {
            foreach (var candidate in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (candidate.isActiveAndEnabled) listener = candidate;
            yield return null;
        }
        Assert.That(listener, Is.Not.Null, "The normal room camera must finish loading before testing audio output.");
        title.RequestShow();
        HomeAudioService.MusicEnabled = true;
        music = title.GetComponent<TitleMusicController>();
        Assert.That(music, Is.Not.Null);
        music.SendMessage("OnApplicationFocus", true);
        music.SendMessage("OnApplicationPause", false);
        UnityEditor.EditorUtility.audioMasterMute = false;
        source = music.transform.Find("Title Music").GetComponent<AudioSource>();
        source.GetOutputData(new float[2048], 0);
        yield return new WaitForSecondsRealtime(.45f);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        HomeAudioService.MusicEnabled = originalMusic;
        HomeAudioService.SoundEnabled = originalSound;
        UnityEditor.EditorUtility.audioMasterMute = originalEditorMute;
        if (title != null) title.RequestShow();
        if (music != null)
        {
            music.SendMessage("OnApplicationPause", false);
            music.SendMessage("OnApplicationFocus", true);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator ImportedMinuteLoopPlaysAndWrapsAtNormalPitch()
    {
        Assert.That(source.clip.length, Is.EqualTo(60f).Within(.02f));
        Assert.That(source.clip.loadState, Is.EqualTo(AudioDataLoadState.Loaded));
        Assert.That(source.clip.loadType, Is.EqualTo(AudioClipLoadType.Streaming));
        Assert.That(source.pitch, Is.EqualTo(1f));
        Assert.That(source.loop && source.isPlaying && !source.mute, Is.True);
        source.time = 59.6f;
        yield return new WaitForSecondsRealtime(1f);
        Assert.That(source.isPlaying, Is.True);
        Assert.That(source.time, Is.InRange(.1f, 2f));
        var data = new float[2048];
        float peak = 0f;
        for (int read=0; read<6 && peak<=.00001f; read++)
        {
            source.GetOutputData(data, 0);
            foreach (float sample in data) peak = Mathf.Max(peak, Mathf.Abs(sample));
            yield return new WaitForSecondsRealtime(.08f);
        }
        Assert.That(peak, Is.GreaterThan(.00001f), "Mixer output after wrapping: playing=" + source.isPlaying +
            ", sourceMute=" + source.mute + ", editorMute=" + UnityEditor.EditorUtility.audioMasterMute +
            ", volume=" + source.volume + ", time=" + source.time);
    }

    [UnityTest]
    public IEnumerator MusicMuteIsIndependentAndResumesFromItsPosition()
    {
        HomeAudioService.SoundEnabled = false;
        yield return null;
        Assert.That(source.isPlaying && !source.mute, Is.True);
        float before = source.time;
        HomeAudioService.MusicEnabled = false;
        yield return new WaitForSecondsRealtime(.3f);
        Assert.That(source.mute && !source.isPlaying, Is.True);
        Assert.That(source.time, Is.EqualTo(before).Within(.15f));
        HomeAudioService.MusicEnabled = true;
        yield return new WaitForSecondsRealtime(.4f);
        Assert.That(source.isPlaying && !source.mute, Is.True);
        Assert.That(source.time, Is.GreaterThan(before));
    }

    [UnityTest]
    public IEnumerator ContinueStopsTheMenuMusicAndReopenHasOneSource()
    {
        var play = (Button)typeof(TitleScreen).GetField("playButton", Private).GetValue(title);
        Assert.That(play.interactable, Is.True);
        play.onClick.Invoke();
        yield return new WaitForSecondsRealtime(.65f);
        Assert.That(TitleScreen.IsShowing, Is.False);
        Assert.That(source.isPlaying, Is.False);
        for (int i=0; i<3; i++) title.RequestShow();
        music.SendMessage("OnApplicationFocus", true);
        yield return new WaitForSecondsRealtime(.4f);
        Assert.That(source.isPlaying, Is.True);
        Assert.That(title.GetComponents<TitleMusicController>().Length, Is.EqualTo(1));
        Assert.That(title.GetComponentsInChildren<AudioSource>(true).Length, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator ApplicationPauseAndFocusLossSilenceAndResumeMusic()
    {
        foreach (string callback in new[]{"OnApplicationPause", "OnApplicationFocus"})
        {
            bool stopValue = callback == "OnApplicationPause";
            music.SendMessage(callback, stopValue);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(source.mute && !source.isPlaying, Is.True, callback);
            music.SendMessage(callback, !stopValue);
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(source.isPlaying && !source.mute, Is.True, callback);
        }
    }
}
#endif
