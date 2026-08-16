using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Self-contained Runner audio/haptic layer. Authored clips can replace every
/// slot; lightweight procedural clips keep feedback complete when no licensed
/// audio pack is installed.
/// </summary>
[DisallowMultipleComponent]
public sealed class CatRunnerAudioController : MonoBehaviour
{
    [SerializeField] private CatRunnerPlayer player;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectsSource;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.16f;
    [SerializeField, Range(0f, 1f)] private float effectsVolume = 0.72f;
    [SerializeField] private AudioClip musicLoop;
    [SerializeField] private AudioClip countdownClip;
    [SerializeField] private AudioClip startClip;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip slideClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip shieldClip;
    [SerializeField] private AudioClip powerUpClip;
    [SerializeField] private AudioClip resultClip;
    [SerializeField] private AudioClip buttonClip;

    private float lastCoinSoundTime = -10f;
    private int coinPitchStep;
    private readonly List<AudioClip> generatedClips = new List<AudioClip>(12);

    private void Awake()
    {
        Resolve();
        BuildFallbackClips();
        ConfigureSources();
        Subscribe();
        ApplyPreferences();
    }

    private void OnEnable()
    {
        CatRunnerProgressService.PreferencesChanged += ApplyPreferences;
    }

    private void OnDisable()
    {
        CatRunnerProgressService.PreferencesChanged -= ApplyPreferences;
    }

    public void BeginMusic()
    {
        if (musicSource == null || musicLoop == null)
            return;
        musicSource.clip = musicLoop;
        musicSource.loop = true;
        if (CatRunnerProgressService.SoundEnabled && !musicSource.isPlaying)
            musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void SetPaused(bool value)
    {
        if (musicSource != null)
            musicSource.volume = musicVolume * (value ? .42f : 1f);
    }

    public void PlayCountdown() => Play(countdownClip, 0.78f, 1f);

    public void PlayStart() => Play(startClip, 0.95f, 1f);

    public void PlayCoin()
    {
        // Dense coin lines remain musical without producing a harsh wall of
        // overlapping transients.
        if (Time.unscaledTime - lastCoinSoundTime < 0.035f)
            return;
        lastCoinSoundTime = Time.unscaledTime;
        float pitch = 0.96f + (coinPitchStep++ % 5) * 0.055f;
        Play(coinClip, 0.64f, pitch);
    }

    public void PlayHit()
    {
        Play(hitClip, 1f, 0.96f);
        Vibrate();
    }

    public void PlayShieldBlock()
    {
        Play(shieldClip, 0.9f, 1.04f);
        Vibrate();
    }

    public void PlayPowerUp() => Play(powerUpClip, 0.92f, 1f);

    public void PlayResult() => Play(resultClip, 0.92f, 1f);

    public void PlayButton() => Play(buttonClip, 0.7f, 1f);

    private void PlayJump()
    {
        Play(jumpClip, 0.72f, 1f);
    }

    private void PlaySlide()
    {
        Play(slideClip, 0.58f, 1f);
    }

    private void Play(AudioClip clip, float volumeScale, float pitch)
    {
        if (effectsSource == null || clip == null ||
            !CatRunnerProgressService.SoundEnabled)
        {
            return;
        }

        effectsSource.pitch = Mathf.Clamp(pitch, 0.7f, 1.4f);
        effectsSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    private void Resolve()
    {
        if (player == null)
            player = FindAnyObjectByType<CatRunnerPlayer>(FindObjectsInactive.Include);
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();
        if (effectsSource == null)
            effectsSource = gameObject.AddComponent<AudioSource>();
        if (ReferenceEquals(musicSource, effectsSource))
            effectsSource = gameObject.AddComponent<AudioSource>();
    }

    private void ConfigureSources()
    {
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = musicVolume;
            musicSource.priority = 180;
        }
        if (effectsSource != null)
        {
            effectsSource.playOnAwake = false;
            effectsSource.loop = false;
            effectsSource.spatialBlend = 0f;
            effectsSource.volume = effectsVolume;
            effectsSource.priority = 80;
        }
    }

    private void Subscribe()
    {
        if (player == null)
            return;
        player.JumpStarted += PlayJump;
        player.SlideStarted += PlaySlide;
    }

    private void ApplyPreferences()
    {
        bool enabled = CatRunnerProgressService.SoundEnabled;
        if (musicSource != null)
        {
            musicSource.mute = !enabled;
            if (enabled && musicSource.clip != null && !musicSource.isPlaying)
                musicSource.Play();
            else if (!enabled && musicSource.isPlaying)
                musicSource.Pause();
        }
        if (effectsSource != null)
            effectsSource.mute = !enabled;
    }

    private void BuildFallbackClips()
    {
        musicLoop ??= TrackGenerated(CreateMusicLoop());
        countdownClip ??= TrackGenerated(
            CreateTone("Runner_Countdown", 0.13f, 520f, 650f, ToneShape.Plop));
        startClip ??= TrackGenerated(
            CreateTone("Runner_Start", 0.42f, 420f, 960f, ToneShape.Chime));
        coinClip ??= TrackGenerated(
            CreateTone("Runner_Coin", 0.16f, 880f, 1220f, ToneShape.Chime));
        jumpClip ??= TrackGenerated(
            CreateTone("Runner_Jump", 0.24f, 310f, 690f, ToneShape.Plop));
        slideClip ??= TrackGenerated(
            CreateTone("Runner_Slide", 0.22f, 260f, 130f, ToneShape.Noise));
        hitClip ??= TrackGenerated(
            CreateTone("Runner_Hit", 0.34f, 135f, 72f, ToneShape.Noise));
        shieldClip ??= TrackGenerated(
            CreateTone("Runner_Shield", 0.34f, 480f, 940f, ToneShape.Chime));
        powerUpClip ??= TrackGenerated(
            CreateTone("Runner_PowerUp", 0.46f, 430f, 1120f, ToneShape.Chime));
        resultClip ??= TrackGenerated(
            CreateTone("Runner_Result", 0.7f, 390f, 980f, ToneShape.Chime));
        buttonClip ??= TrackGenerated(
            CreateTone("Runner_Button", 0.1f, 420f, 510f, ToneShape.Plop));
    }

    private AudioClip TrackGenerated(AudioClip clip)
    {
        if (clip != null)
            generatedClips.Add(clip);
        return clip;
    }

    private enum ToneShape
    {
        Plop,
        Chime,
        Noise
    }

    private static AudioClip CreateTone(
        string name,
        float seconds,
        float startFrequency,
        float endFrequency,
        ToneShape shape)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.Max(32, Mathf.RoundToInt(seconds * sampleRate));
        var samples = new float[sampleCount];
        float phase = 0f;
        uint noise = 0xA341316Cu;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t * t);
            phase += frequency / sampleRate * Mathf.PI * 2f;
            float attack = Mathf.Clamp01(t / 0.035f);
            float release = Mathf.Pow(1f - t, shape == ToneShape.Chime ? 2.2f : 1.45f);
            float wave;
            if (shape == ToneShape.Noise)
            {
                noise = noise * 1664525u + 1013904223u;
                float random = ((noise >> 9) & 0x7FFFFF) / 4194303.5f - 1f;
                wave = Mathf.Sin(phase) * 0.48f + random * 0.38f;
            }
            else
            {
                float harmonic = shape == ToneShape.Chime ? 0.34f : 0.16f;
                wave = Mathf.Sin(phase) + Mathf.Sin(phase * 2.01f) * harmonic;
            }
            samples[i] = Mathf.Clamp(wave * attack * release * 0.38f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateMusicLoop()
    {
        const int sampleRate = 22050;
        const float seconds = 8f;
        int sampleCount = Mathf.RoundToInt(seconds * sampleRate);
        var samples = new float[sampleCount];
        float[] roots = { 261.63f, 329.63f, 392f, 293.66f };
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            int beat = Mathf.FloorToInt(time * 2f);
            float beatPhase = Mathf.Repeat(time * 2f, 1f);
            float root = roots[(beat / 4) % roots.Length];
            float melody = root * (beat % 4 == 0 ? 2f : beat % 4 == 2 ? 2.5f : 3f);
            float pluckEnvelope = Mathf.Exp(-beatPhase * 5.2f);
            float bass = Mathf.Sin(time * Mathf.PI * 2f * root * 0.5f) * 0.1f;
            float pluck = (
                Mathf.Sin(time * Mathf.PI * 2f * melody) +
                Mathf.Sin(time * Mathf.PI * 2f * melody * 2.01f) * 0.28f) *
                pluckEnvelope * 0.12f;
            float pad = (
                Mathf.Sin(time * Mathf.PI * 2f * root) +
                Mathf.Sin(time * Mathf.PI * 2f * root * 1.25f) +
                Mathf.Sin(time * Mathf.PI * 2f * root * 1.5f)) * 0.022f;
            float loopFade = Mathf.Min(
                1f,
                Mathf.Min(time / 0.04f, (seconds - time) / 0.04f));
            samples[i] = Mathf.Clamp((bass + pluck + pad) * loopFade, -0.5f, 0.5f);
        }
        AudioClip clip = AudioClip.Create(
            "Runner_CandyTownLoop",
            sampleCount,
            1,
            sampleRate,
            false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static void Vibrate()
    {
        if (!CatRunnerProgressService.HapticsEnabled)
            return;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.JumpStarted -= PlayJump;
            player.SlideStarted -= PlaySlide;
        }
        for (int i = 0; i < generatedClips.Count; i++)
            if (generatedClips[i] != null)
                Destroy(generatedClips[i]);
        generatedClips.Clear();
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        CatRunnerPlayer runner,
        AudioSource music,
        AudioSource effects)
    {
        player = runner;
        musicSource = music;
        effectsSource = effects;
        musicVolume = 0.16f;
        effectsVolume = 0.72f;
    }
#endif
}
