using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using CatHome.Economy;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// The persistent home soundscape: a gentle music loop, a soft ambient bed and
/// event stingers (button taps, purchases, rewards, level ups). Fully procedural,
/// so it needs no licensed audio pack; authored clips can replace the generators
/// later. Self-bootstraps into a DontDestroyOnLoad host — no scene or builder edit
/// is required — and ducks itself while a mini game runs so it never fights the
/// Runner/Catch audio. Governed by <see cref="HomeAudioService"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class HomeAudioController : MonoBehaviour
{
    private static HomeAudioController instance;

    private AudioSource musicSource;
    private AudioSource ambientSource;
    private AudioSource effectsSource;

    private AudioClip musicLoop;
    private AudioClip ambientLoop;
    private AudioClip clickClip;
    private AudioClip purchaseClip;
    private AudioClip coinClip;
    private AudioClip diamondClip;
    private AudioClip levelUpClip;
    private AudioClip purrClip;
    private AudioClip eatClip;
    private AudioClip drinkClip;
    private AudioClip heartClip;
    private AudioClip activityClip;
    private AudioClip roomClip;

    private readonly List<AudioClip> generated = new List<AudioClip>(16);

    private const float MusicVolume = 0.12f;
    private const float AmbientVolume = 0.09f;
    private const float EffectsVolume = 0.6f;

    private float sessionReadyTime;
    private float lastClickTime = -1f;
    private float nextDuckPoll;
    private float nextHookScan;
    private bool ducked;
    private HungerSystem hunger;
    private ThirstSystem thirst;
    private PetInteraction pet;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;
        var host = new GameObject("HomeAudioController");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<HomeAudioController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        ambientSource = gameObject.AddComponent<AudioSource>();
        effectsSource = gameObject.AddComponent<AudioSource>();

        BuildClips();
        ConfigureSource(musicSource, musicLoop, MusicVolume, true, 180);
        ConfigureSource(ambientSource, ambientLoop, AmbientVolume, true, 200);
        ConfigureSource(effectsSource, null, EffectsVolume, false, 90);

        // Give the save system a beat to restore balances/ownership before event
        // stingers are allowed, so loading a save is silent.
        sessionReadyTime = Time.unscaledTime + 1.5f;
    }

    private void OnEnable()
    {
        HomeAudioService.Changed += ApplyPreferences;
        HomeProgressionService.LeveledUp += HandleLevelUp;
        HomeStoreService.OwnershipChanged += HandleOwnershipChanged;
        EconomyService.BalanceChanged += HandleBalanceChanged;
        HomeRoomService.CurrentRoomChanged += HandleRoomChanged;
        CatActivity.Completed += HandleActivityCompleted;
        AchievementService.StateChanged += HandleAchievement;
        ApplyPreferences();
        LocalNotificationService.RefreshSchedules(System.DateTime.UtcNow);
    }

    private void OnDisable()
    {
        HomeAudioService.Changed -= ApplyPreferences;
        HomeProgressionService.LeveledUp -= HandleLevelUp;
        HomeStoreService.OwnershipChanged -= HandleOwnershipChanged;
        EconomyService.BalanceChanged -= HandleBalanceChanged;
        HomeRoomService.CurrentRoomChanged -= HandleRoomChanged;
        CatActivity.Completed -= HandleActivityCompleted;
        AchievementService.StateChanged -= HandleAchievement;
        UnbindCareHooks();
    }

    private void Update()
    {
        PollMiniGameDuck();
        DetectUiClick();
        PollCareHooks();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            LocalNotificationService.NotifyAppPaused(System.DateTime.UtcNow);
    }

    // --- Playback control -------------------------------------------------

    private void ApplyPreferences()
    {
        bool music = HomeAudioService.SoundEnabled && HomeAudioService.MusicEnabled && !ducked;
        SetLoop(musicSource, music);
        SetLoop(ambientSource, HomeAudioService.SoundEnabled && !ducked);
        if (effectsSource != null)
            effectsSource.mute = !HomeAudioService.SoundEnabled;
    }

    private static void SetLoop(AudioSource source, bool shouldPlay)
    {
        if (source == null || source.clip == null)
            return;
        if (shouldPlay)
        {
            source.mute = false;
            if (!source.isPlaying)
                source.Play();
        }
        else if (source.isPlaying)
        {
            source.Pause();
        }
    }

    private void PollMiniGameDuck()
    {
        if (Time.unscaledTime < nextDuckPoll)
            return;
        nextDuckPoll = Time.unscaledTime + 0.4f;

        bool miniGameActive =
            FindAnyObjectByType<CatRunnerGameController>(FindObjectsInactive.Exclude) != null ||
            FindAnyObjectByType<CatCatchGameController>(FindObjectsInactive.Exclude) != null;
        if (miniGameActive == ducked)
            return;
        ducked = miniGameActive;
        ApplyPreferences();
    }

    private void DetectUiClick()
    {
        if (!HomeAudioService.SoundEnabled || EventSystem.current == null)
            return;
        if (!PointerPressedThisFrame() || !EventSystem.current.IsPointerOverGameObject())
            return;
        if (Time.unscaledTime - lastClickTime < 0.05f)
            return;
        lastClickTime = Time.unscaledTime;
        PlayEffect(clickClip, 0.5f, 1f);
    }

    private static bool PointerPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    // --- Event stingers ---------------------------------------------------

    private void HandleLevelUp(int level)
    {
        PlayCelebration();
    }

    private void HandleRoomChanged(string roomId)
    {
        if (Time.unscaledTime < sessionReadyTime)
            return;
        PlayRoomChange();
    }

    private void HandleActivityCompleted(CatActivity activity)
    {
        if (Time.unscaledTime < sessionReadyTime)
            return;
        PlayActivity();
    }

    private void HandleAchievement()
    {
        if (Time.unscaledTime < sessionReadyTime)
            return;
        PlayCelebration();
    }

    private void PollCareHooks()
    {
        if (Time.unscaledTime < nextHookScan)
            return;
        nextHookScan = Time.unscaledTime + 0.6f;

        HungerSystem foundHunger = FindAnyObjectByType<HungerSystem>(FindObjectsInactive.Include);
        if (foundHunger != hunger)
        {
            if (hunger != null)
                hunger.Ate -= HandleAte;
            hunger = foundHunger;
            if (hunger != null)
                hunger.Ate += HandleAte;
        }

        ThirstSystem foundThirst = FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include);
        if (foundThirst != thirst)
        {
            if (thirst != null)
                thirst.Drank -= HandleDrank;
            thirst = foundThirst;
            if (thirst != null)
                thirst.Drank += HandleDrank;
        }

        PetInteraction foundPet = FindAnyObjectByType<PetInteraction>(FindObjectsInactive.Include);
        if (foundPet != pet)
        {
            if (pet != null)
                pet.PettingStarted -= HandlePet;
            pet = foundPet;
            if (pet != null)
                pet.PettingStarted += HandlePet;
        }
    }

    private void UnbindCareHooks()
    {
        if (hunger != null)
            hunger.Ate -= HandleAte;
        if (thirst != null)
            thirst.Drank -= HandleDrank;
        if (pet != null)
            pet.PettingStarted -= HandlePet;
        hunger = null;
        thirst = null;
        pet = null;
    }

    private void HandleAte()
    {
        if (Time.unscaledTime < sessionReadyTime)
            return;
        PlayCare();
    }

    private void HandleDrank()
    {
        if (Time.unscaledTime < sessionReadyTime)
            return;
        PlayDrink();
    }

    private void HandlePet()
    {
        if (Time.unscaledTime < sessionReadyTime)
            return;
        PlayHearts();
    }

    private void HandleOwnershipChanged(string productId)
    {
        if (Time.unscaledTime < sessionReadyTime)
            return;
        PlayEffect(purchaseClip, 0.85f, 1f);
    }

    private void HandleBalanceChanged(CurrencyBalanceChange change)
    {
        if (Time.unscaledTime < sessionReadyTime || !change.IsIncrease)
            return;
        if (change.Currency == CurrencyType.Diamond)
            PlayEffect(diamondClip, 0.8f, 1f);
        else if (change.Currency == CurrencyType.Coin)
            PlayEffect(coinClip, 0.7f, 1f);
    }

    /// <summary>
    /// Short attention purr for idle personality. No-ops when sound is off or
    /// the controller has not bootstrapped yet.
    /// </summary>
    public static void PlayPurr()
    {
        if (instance == null)
            return;
        instance.PlayEffect(instance.purrClip, 0.62f, 0.92f);
    }

    public static void PlayCelebration()
    {
        if (instance == null)
            return;
        instance.PlayEffect(instance.levelUpClip, 0.9f, 1f);
    }

    public static void PlayCare()
    {
        if (instance == null)
            return;
        instance.PlayEffect(instance.eatClip, 0.55f, 1.05f);
    }

    public static void PlayDrink()
    {
        if (instance == null)
            return;
        instance.PlayEffect(instance.drinkClip, 0.5f, 1.1f);
    }

    public static void PlayHearts()
    {
        if (instance == null)
            return;
        instance.PlayEffect(instance.heartClip, 0.58f, 1.15f);
    }

    public static void PlayActivity()
    {
        if (instance == null)
            return;
        instance.PlayEffect(instance.activityClip, 0.62f, 1f);
    }

    public static void PlayRoomChange()
    {
        if (instance == null)
            return;
        instance.PlayEffect(instance.roomClip, 0.45f, 0.95f);
    }

    private void PlayEffect(AudioClip clip, float volume, float pitch)
    {
        if (effectsSource == null || clip == null || !HomeAudioService.SoundEnabled)
            return;
        effectsSource.pitch = Mathf.Clamp(pitch, 0.7f, 1.4f);
        effectsSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // --- Setup ------------------------------------------------------------

    private static void ConfigureSource(
        AudioSource source, AudioClip clip, float volume, bool loop, int priority)
    {
        if (source == null)
            return;
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.priority = priority;
    }

    private void BuildClips()
    {
        musicLoop = Track(CreateCozyMusicLoop());
        ambientLoop = Track(CreateAmbientLoop());
        clickClip = Track(CreateTone("Home_Click", 0.09f, 620f, 720f, ToneShape.Plop));
        purchaseClip = Track(CreateTone("Home_Purchase", 0.4f, 480f, 960f, ToneShape.Chime));
        coinClip = Track(CreateTone("Home_Coin", 0.2f, 820f, 1180f, ToneShape.Chime));
        diamondClip = Track(CreateTone("Home_Diamond", 0.36f, 900f, 1500f, ToneShape.Chime));
        levelUpClip = Track(CreateTone("Home_LevelUp", 0.62f, 420f, 1180f, ToneShape.Chime));
        purrClip = Track(CreatePurr());
        eatClip = Track(CreateTone("Home_Eat", 0.18f, 180f, 240f, ToneShape.Plop));
        drinkClip = Track(CreateTone("Home_Drink", 0.16f, 520f, 880f, ToneShape.Plop));
        heartClip = Track(CreateTone("Home_Heart", 0.28f, 640f, 980f, ToneShape.Chime));
        activityClip = Track(CreateTone("Home_Activity", 0.32f, 400f, 860f, ToneShape.Chime));
        roomClip = Track(CreateTone("Home_Room", 0.22f, 300f, 180f, ToneShape.Plop));
    }

    private AudioClip Track(AudioClip clip)
    {
        if (clip != null)
            generated.Add(clip);
        return clip;
    }

    private enum ToneShape
    {
        Plop,
        Chime
    }

    private static AudioClip CreateTone(
        string name, float seconds, float startFrequency, float endFrequency, ToneShape shape)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.Max(32, Mathf.RoundToInt(seconds * sampleRate));
        var samples = new float[sampleCount];
        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t * t);
            phase += frequency / sampleRate * Mathf.PI * 2f;
            float attack = Mathf.Clamp01(t / 0.03f);
            float release = Mathf.Pow(1f - t, shape == ToneShape.Chime ? 2.2f : 1.5f);
            float harmonic = shape == ToneShape.Chime ? 0.34f : 0.16f;
            float wave = Mathf.Sin(phase) + Mathf.Sin(phase * 2.01f) * harmonic;
            samples[i] = Mathf.Clamp(wave * attack * release * 0.36f, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreatePurr()
    {
        const int sampleRate = 22050;
        const float seconds = 0.55f;
        int sampleCount = Mathf.Max(32, Mathf.RoundToInt(seconds * sampleRate));
        var samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Clamp01(t / 0.06f) * Mathf.Pow(1f - t / seconds, 1.4f);
            float pulse = 0.55f + 0.45f * Mathf.Sin(t * Mathf.PI * 2f * 18f);
            float rumble =
                Mathf.Sin(t * Mathf.PI * 2f * 92f) * 0.55f +
                Mathf.Sin(t * Mathf.PI * 2f * 118f) * 0.28f;
            samples[i] = Mathf.Clamp(rumble * pulse * env * 0.32f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Home_Purr", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateCozyMusicLoop()
    {
        const int sampleRate = 22050;
        const float seconds = 12f;
        int sampleCount = Mathf.RoundToInt(seconds * sampleRate);
        var samples = new float[sampleCount];
        // A slow, warm major progression (C - G - Am - F) with a soft music-box
        // pluck and a low pad. Quieter and calmer than the Runner loop so it sits
        // under gameplay as a cozy home bed.
        float[] roots = { 261.63f, 196f, 220f, 174.61f };
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            int bar = Mathf.FloorToInt(time / 3f) % roots.Length;
            float barPhase = Mathf.Repeat(time / 3f, 1f);
            float root = roots[bar];
            int step = Mathf.FloorToInt(barPhase * 4f);
            float note = root * (step == 0 ? 2f : step == 1 ? 2.5f : step == 2 ? 3f : 2.5f);
            float stepPhase = Mathf.Repeat(barPhase * 4f, 1f);
            float pluckEnv = Mathf.Exp(-stepPhase * 3.4f);
            float pluck = (
                Mathf.Sin(time * Mathf.PI * 2f * note) +
                Mathf.Sin(time * Mathf.PI * 2f * note * 2.01f) * 0.22f) * pluckEnv * 0.1f;
            float pad = (
                Mathf.Sin(time * Mathf.PI * 2f * root) +
                Mathf.Sin(time * Mathf.PI * 2f * root * 1.5f) +
                Mathf.Sin(time * Mathf.PI * 2f * root * 2f) * 0.6f) * 0.02f;
            float bass = Mathf.Sin(time * Mathf.PI * 2f * root * 0.5f) * 0.06f;
            float loopFade = Mathf.Min(1f, Mathf.Min(time / 0.1f, (seconds - time) / 0.1f));
            samples[i] = Mathf.Clamp((pluck + pad + bass) * loopFade, -0.5f, 0.5f);
        }
        AudioClip clip = AudioClip.Create("Home_CozyLoop", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateAmbientLoop()
    {
        const int sampleRate = 22050;
        const float seconds = 9f;
        int sampleCount = Mathf.RoundToInt(seconds * sampleRate);
        var samples = new float[sampleCount];
        uint noise = 0x1F2E3D4Cu;
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            // Airy breeze: gently filtered noise swelling in and out.
            noise = noise * 1664525u + 1013904223u;
            float random = ((noise >> 9) & 0x7FFFFF) / 4194303.5f - 1f;
            float breeze = random * 0.05f * (0.5f + 0.5f * Mathf.Sin(time * 0.6f));
            // Occasional soft bird chirp, spaced so it reads as a real garden.
            float chirpPhase = Mathf.Repeat(time, 3.7f);
            float chirp = 0f;
            if (chirpPhase < 0.18f)
            {
                float c = chirpPhase / 0.18f;
                float freq = Mathf.Lerp(2200f, 2800f, c);
                chirp = Mathf.Sin(time * Mathf.PI * 2f * freq) *
                        Mathf.Exp(-c * 6f) * 0.05f;
            }
            float loopFade = Mathf.Min(1f, Mathf.Min(time / 0.2f, (seconds - time) / 0.2f));
            samples[i] = Mathf.Clamp((breeze + chirp) * loopFade, -0.4f, 0.4f);
        }
        AudioClip clip = AudioClip.Create("Home_Ambient", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
        for (int i = 0; i < generated.Count; i++)
            if (generated[i] != null)
                Destroy(generated[i]);
        generated.Clear();
    }
}
