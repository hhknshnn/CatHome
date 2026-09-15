using UnityEngine;

/// <summary>Owns the title's music only; uses the existing independent music preference.</summary>
[DisallowMultipleComponent]
public sealed class TitleMusicController : MonoBehaviour
{
    private const string ResourcePath = "Music/CatHomeMenu";
    private const float Volume = 0.36f;
    private const float FadeSeconds = 0.25f;
    [SerializeField] private AudioSource source;
    private bool paused;
    private bool applicationPaused;
    private bool focused = true;

    private void Awake()
    {
        var host = new GameObject("Title Music");
        host.transform.SetParent(transform, false);
        source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.pitch = 1f;
        source.priority = 32;
        source.volume = 0f;
        source.clip = Resources.Load<AudioClip>(ResourcePath);
        if (source.clip == null)
            Debug.LogWarning("Title music clip is missing: " + ResourcePath, this);
    }

    private void OnEnable()
    {
        focused = Application.isFocused;
        applicationPaused = false;
        HomeAudioService.Changed += ApplyPreferences;
        ApplyPreferences();
    }

    private void OnDisable()
    {
        HomeAudioService.Changed -= ApplyPreferences;
        if (source == null) return;
        source.Stop();
        source.volume = 0f;
        paused = false;
        if (source.clip != null && source.clip.loadState == AudioDataLoadState.Loaded) source.clip.UnloadAudioData();
    }

    private void Update()
    {
        if (source == null || source.clip == null) return;
        bool suspended = !HomeAudioService.MusicEnabled || applicationPaused || !focused;
        source.mute = suspended;
        if (suspended)
        {
            PausePlayback();
            return;
        }

        if (TitleScreen.IsShowing)
        {
            // The title is decoded before playback, so import / scene I/O cannot
            // starve a streaming decoder in the middle of its accepted phrase.
            if (source.clip.loadState != AudioDataLoadState.Loaded)
            {
                if (source.clip.loadState == AudioDataLoadState.Unloaded) source.clip.LoadAudioData();
                return;
            }
            if (paused)
            {
                source.UnPause();
                paused = false;
            }
            if (!source.isPlaying) source.Play();
            source.volume = Mathf.MoveTowards(source.volume, Volume,
                Volume * Time.unscaledDeltaTime / FadeSeconds);
        }
        else
        {
            source.volume = Mathf.MoveTowards(source.volume, 0f,
                Volume * Time.unscaledDeltaTime / FadeSeconds);
            if (source.volume <= 0f)
            {
                source.Stop();
                paused = false;
                // Release the 60-second decoded title once home takes over.
                if (source.clip.loadState == AudioDataLoadState.Loaded) source.clip.UnloadAudioData();
            }
        }
    }

    private void ApplyPreferences()
    {
        if (source == null) return;
        bool suspended = !HomeAudioService.MusicEnabled || applicationPaused || !focused;
        source.mute = suspended;
        if (suspended) PausePlayback();
    }

    private void PausePlayback()
    {
        if (source.isPlaying)
        {
            source.Pause();
            paused = true;
        }
        source.volume = 0f;
    }

    private void OnApplicationPause(bool value)
    {
        applicationPaused = value;
        ApplyPreferences();
    }

    private void OnApplicationFocus(bool value)
    {
        focused = value;
        ApplyPreferences();
    }
}
