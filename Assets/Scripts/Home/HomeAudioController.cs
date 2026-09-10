using System.Collections.Generic;
using UnityEngine;
using CatHome.Economy;


/// <summary>
/// Event sounds for care, purchases and rewards. The synthetic repeating music,
/// ambient chirps and automatic pointer plops were retired after device feedback.
/// Does not create an AudioListener or change the player's sound preferences.
/// </summary>
[DisallowMultipleComponent]
public sealed class HomeAudioController : MonoBehaviour
{
    private static HomeAudioController instance;

    private AudioSource effectsSource;

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

    private const float EffectsVolume = 0.6f;

    private float sessionReadyTime;
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

        effectsSource = gameObject.AddComponent<AudioSource>();

        BuildClips();
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
        if (effectsSource != null)
            effectsSource.mute = !HomeAudioService.SoundEnabled || ducked;
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
        if(activity is CatCommandActivity || activity!=null&&activity.SupportsContinuousRest)return;
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
        var cat=FindFirstObjectByType<CatVoice>();if(cat!=null)cat.PurrBriefly();
    }

    public static void PlayCelebration()
    {
        if (instance == null)
            return;
        instance.PlayEffect(instance.levelUpClip, 0.9f, 1f);
    }

    public static void PlayCare()
    {
        // CatVoice follows the actual eating pose; completion adds no plop.
    }

    public static void PlayDrink()
    {
        // CatVoice follows the actual drinking pose.
    }

    public static void PlayHearts()
    {
        PlayPurr();
    }

    public static void PlayActivity()
    {
        if (instance == null)
            return;
        instance.PlayEffect(instance.activityClip, 0.62f, 1f);
    }

    public static void PlayRoomChange()
    {
        // Room changes are silent.
    }

    private void PlayEffect(AudioClip clip, float volume, float pitch)
    {
        if (effectsSource == null || clip == null || !HomeAudioService.SoundEnabled || ducked)
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
        purchaseClip = Track(CreateTone("Home_Purchase", 0.4f, 480f, 960f, ToneShape.Chime));
        coinClip = Track(CreateTone("Home_Coin", 0.2f, 820f, 1180f, ToneShape.Chime));
        diamondClip = Track(CreateTone("Home_Diamond", 0.36f, 900f, 1500f, ToneShape.Chime));
        levelUpClip = Track(CreateTone("Home_LevelUp", 0.62f, 420f, 1180f, ToneShape.Chime));
        activityClip = Track(CreateTone("Home_Activity", 0.32f, 400f, 860f, ToneShape.Chime));
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
