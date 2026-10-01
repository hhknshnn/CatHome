using UnityEngine;

/// <summary>Scene-owned record state; an observed paw contact is the only toggle trigger.</summary>
[DisallowMultipleComponent]
public sealed class RecordPlayerMusic : MonoBehaviour
{
    public const string ResourcePath = "GameAudio/Music/Record";
    static RecordPlayerMusic active;
    AudioSource source;
    Transform disc;
    bool on, paused;
    CatMovement cat;
    BowlInteraction bowls;
    SleepInteraction sleep;
    CatActivityAnimation poses;
    public bool IsOn => on;
    public bool IsAudible => source != null && source.isPlaying && source.volume > .001f;
    public int ToggleCount { get; private set; }
    public AudioSource Source => source;
    public static bool HasActiveRecord => active != null && active.isActiveAndEnabled && active.on;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => active = null;

    public void Prepare(Transform platter)
    {
        disc = platter;
        if (source != null) return;
        source = gameObject.AddComponent<AudioSource>();
        GameAudio.Configure(source, true, 150);
        source.clip = Resources.Load<AudioClip>(ResourcePath);
        // Import/preload happens with the owned product, before any paw contact.
        if (source.clip != null) source.clip.LoadAudioData();
    }

    public bool ToggleFromContact(PaperSpinActivity activity, CatMovement actor)
    {
        if (activity == null || activity.gameObject != gameObject || !activity.IsRunning ||
            actor == null || !activity.BelongsTo(actor) ||
            activity.Kind != CatActivityKind.RecordSpin || activity.RecordContactCount != 1 ||
            Time.timeScale <= 0f || Time.deltaTime <= 0f || source == null || source.clip == null) return false;
        ToggleCount++;
        if (on) SwitchOff();
        else
        {
            if (active != null && active != this) active.SwitchOff();
            // The contact already has the authoritative actor. Keep that
            // context instead of rediscovering a cat from an additive scene.
            cat = actor;
            bowls = actor.GetComponent<BowlInteraction>();
            sleep = actor.GetComponent<SleepInteraction>();
            poses = actor.GetComponent<CatActivityAnimation>();
            active = this; on = true; paused = false;
        }
        return true;
    }

    void Update()
    {
        if (!on || source == null) return;
        if (cat == null) { SwitchOff(); return; }
        // The music preference silences the source without changing the switch
        // or platter; an actual pause/focus loss freezes the scene mechanism.
        if (disc != null && !GameAudio.IsSuspended && Time.timeScale > 0)
            disc.localRotation *= Quaternion.AngleAxis(200f * Time.deltaTime, Vector3.up);
        bool allowed = HomeAudioService.MusicEnabled && !GameAudio.IsSuspended &&
            !TitleScreen.IsShowing && !HomeUiFlow.IsMiniGameVisible && Time.timeScale > 0;
        if (!allowed)
        {
            if (source.isPlaying) { source.Pause(); paused = true; }
            source.volume = 0;
            return;
        }
        if (!source.isPlaying)
        {
            if (paused) source.UnPause();
            else source.Play();
            paused = false;
        }
        float volume = .16f;
        var activity = CatActivity.Active;
        if (activity != null && activity.BelongsTo(cat) && activity.IsWaitingForRestStop) volume = .018f;
        if (bowls != null && bowls.IsInteracting || poses != null && poses.IsActive &&
            (poses.CurrentPose == CatActivityPose.Eat || poses.CurrentPose == CatActivityPose.Drink))
            volume = Mathf.Min(volume, .025f);
        if (sleep != null && sleep.IsSettledOnBed || poses != null && poses.IsActive && poses.CurrentPose == CatActivityPose.Sleep)
            volume = Mathf.Min(volume, .012f);
        source.volume = Mathf.MoveTowards(source.volume, volume, Time.unscaledDeltaTime * .35f);
    }

    void SwitchOff()
    {
        on = paused = false;
        if (source != null) { source.Stop(); source.volume = 0; }
        if (active == this) active = null;
        cat = null; bowls = null; sleep = null; poses = null;
    }
    void OnDisable() => SwitchOff();
    void OnDestroy() { if (active == this) active = null; }
}
