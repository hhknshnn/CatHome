using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PetSoundController : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip purrLoop;
    [SerializeField] private AudioClip[] meowClips;

    [Header("Sources")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource meowAudioSource;

    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float purrVolume = 0.3f;
    [SerializeField, Range(0f, 1f)] private float meowVolume = 0.45f;
    [SerializeField] private Vector2 meowPitchRange = new Vector2(0.95f, 1.05f);

    [Header("Timing")]
    [SerializeField, Range(0f, 1f)] private float meowChance = 0.3f;
    [SerializeField, Min(8f)] private float meowCooldown = 8f;
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.3f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.35f;

    private Coroutine purrFade;
    private float nextMeowTime;
    private bool isPetting;

    public void BeginPettingAudio()
    {
        isPetting = true;
        // Keep old serialized slots for scene compatibility; CatVoice owns the
        // new state-bound purr so petting cannot double up two recordings.
        StopAllAudio();
        var movement = GetComponent<CatMovement>();
        if (movement != null) CatVoice.EnsureOn(movement).PurrBriefly();
    }

    public void EndPettingAudio()
    {
        isPetting = false;
        if (purrFade != null)
            StopCoroutine(purrFade);

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            StopAllAudio();
            return;
        }

        if (audioSource != null && audioSource.isPlaying)
            purrFade = StartCoroutine(FadePurr(0f, fadeOutDuration, true));
        else
            purrFade = null;
    }

    private void Awake()
    {
        ConfigureSources();
    }

    private void ConfigureSources()
    {
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = true;
        }

        if (meowAudioSource != null)
        {
            meowAudioSource.playOnAwake = false;
            meowAudioSource.loop = false;
        }
    }

    private void TryPlayMeow()
    {
        if (meowAudioSource == null ||
            meowClips == null ||
            meowClips.Length == 0 ||
            Time.unscaledTime < nextMeowTime ||
            Random.value > meowChance)
        {
            return;
        }

        AudioClip clip = PickMeowClip();
        if (clip == null)
            return;

        // One dedicated source prevents one-shots from stacking or interrupting the purr loop.
        if (meowAudioSource.isPlaying)
            return;

        float lowPitch = Mathf.Min(meowPitchRange.x, meowPitchRange.y);
        float highPitch = Mathf.Max(meowPitchRange.x, meowPitchRange.y);
        meowAudioSource.pitch = Random.Range(lowPitch, highPitch);
        meowAudioSource.volume = meowVolume;
        meowAudioSource.clip = clip;
        meowAudioSource.Play();
        nextMeowTime = Time.unscaledTime + Mathf.Max(8f, meowCooldown);
    }

    private AudioClip PickMeowClip()
    {
        int start = Random.Range(0, meowClips.Length);
        for (int i = 0; i < meowClips.Length; i++)
        {
            AudioClip candidate = meowClips[(start + i) % meowClips.Length];
            if (candidate != null)
                return candidate;
        }

        return null;
    }

    private IEnumerator FadePurr(float targetVolume, float duration, bool stopAtEnd)
    {
        float startVolume = audioSource != null ? audioSource.volume : 0f;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (audioSource != null && elapsed < duration)
        {
            if (!isPetting && !stopAtEnd)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        if (audioSource != null)
        {
            audioSource.volume = targetVolume;
            if (stopAtEnd)
            {
                audioSource.Stop();
                audioSource.clip = null;
            }
        }

        purrFade = null;
    }

    private void StopAllAudio()
    {
        isPetting = false;
        if (purrFade != null)
        {
            StopCoroutine(purrFade);
            purrFade = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.volume = 0f;
        }

        if (meowAudioSource != null)
        {
            meowAudioSource.Stop();
            meowAudioSource.clip = null;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            StopAllAudio();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            StopAllAudio();
    }

    private void OnDisable()
    {
        StopAllAudio();
    }

    private void OnDestroy()
    {
        StopAllAudio();
    }
}
