using TMPro;
using UnityEngine;

/// <summary>
/// Coordinates short, non-gameplay feedback beats for the runner. All effects
/// are optional references so the gameplay remains safe if a low-end build strips
/// a particle layer.
/// </summary>
[DisallowMultipleComponent]
public sealed class CatRunnerFeedbackController : MonoBehaviour
{
    [SerializeField] private CatRunnerPlayer player;
    [SerializeField] private CatRunnerCameraRig cameraRig;
    [SerializeField] private TMP_Text coinLabel;
    [SerializeField] private TMP_Text countdownLabel;
    [SerializeField] private CanvasGroup hitFlash;
    [SerializeField] private ParticleSystem coinBurst;
    [SerializeField] private ParticleSystem coinSparkleBurst;
    [SerializeField] private ParticleSystem hitBurst;
    [SerializeField] private ParticleSystem landingDust;
    [SerializeField] private ParticleSystem slideDust;
    [SerializeField] private ParticleSystem speedStreaks;

    private Vector3 coinLabelScale = Vector3.one;
    private Vector3 countdownScale = Vector3.one;
    private float coinPunch;
    private float countdownPunch;
    private bool reducedMotion;

    private void Awake()
    {
        if (player == null)
            player = FindAnyObjectByType<CatRunnerPlayer>(FindObjectsInactive.Include);
        if (cameraRig == null)
            cameraRig = FindAnyObjectByType<CatRunnerCameraRig>(FindObjectsInactive.Include);
        if (coinLabel != null)
            coinLabelScale = coinLabel.rectTransform.localScale;
        if (countdownLabel != null)
            countdownScale = countdownLabel.rectTransform.localScale;
        if (hitFlash != null)
            hitFlash.alpha = 0f;
        if (player != null)
        {
            player.Landed += PlayLanding;
            player.SlideStarted += PlaySlide;
        }
        SetRunning(false);
    }

    private void Update()
    {
        float delta = Time.unscaledDeltaTime;
        coinPunch = reducedMotion ? 0f : Mathf.MoveTowards(coinPunch, 0f, delta * 5.8f);
        countdownPunch = reducedMotion
            ? 0f
            : Mathf.MoveTowards(countdownPunch, 0f, delta * 3.8f);

        if (coinLabel != null)
        {
            float pulse = 1f + coinPunch * 0.26f;
            coinLabel.rectTransform.localScale = coinLabelScale * pulse;
            coinLabel.color = Color.Lerp(
                new Color32(255, 207, 56, 255),
                Color.white,
                coinPunch * 0.72f);
        }

        if (countdownLabel != null)
        {
            float pulse = 1f + countdownPunch * 0.42f;
            countdownLabel.rectTransform.localScale = countdownScale * pulse;
            countdownLabel.rectTransform.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Sin(Time.unscaledTime * 18f) * countdownPunch * 4f);
        }

        if (hitFlash != null)
            hitFlash.alpha = Mathf.MoveTowards(hitFlash.alpha, 0f, delta * 3.5f);
    }

    public void BeginRun()
    {
        if (cameraRig != null)
            cameraRig.BeginRunKick();
        SetRunning(true);
    }

    public void SetRunning(bool value)
    {
        if (speedStreaks == null)
            return;
        ParticleSystem.EmissionModule emission = speedStreaks.emission;
        bool shouldRun = value && !reducedMotion;
        emission.enabled = shouldRun;
        if (shouldRun && !speedStreaks.isPlaying)
            speedStreaks.Play();
        else if (!shouldRun)
            speedStreaks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    public void PlayCountdown()
    {
        countdownPunch = reducedMotion ? 0f : 1f;
    }

    public void PlayCoin()
    {
        Vector3 position = player != null
            ? player.transform.position + new Vector3(0f, .72f, .08f)
            : transform.position;
        PlayCoinAt(position);
    }

    public void PlayCoinAt(Vector3 worldPosition)
    {
        coinPunch = reducedMotion ? 0f : 1f;
        if (cameraRig != null)
            cameraRig.PulseCoin();
        EmitAtWorld(coinBurst, reducedMotion ? 4 : 10, worldPosition);
        EmitAtWorld(coinSparkleBurst, reducedMotion ? 5 : 16, worldPosition);
    }

    public void PlayHit()
    {
        if (cameraRig != null)
            cameraRig.AddHitTrauma();
        if (hitFlash != null)
            hitFlash.alpha = reducedMotion ? 0.24f : 0.58f;
        EmitAtPlayer(hitBurst, reducedMotion ? 7 : 18, new Vector3(0f, 0.62f, 0.05f));
    }

    public void PlayPowerUpAt(Vector3 worldPosition)
    {
        EmitAtWorld(coinSparkleBurst, reducedMotion ? 8 : 24, worldPosition);
        if (cameraRig != null && !reducedMotion)
            cameraRig.PulseCoin();
    }

    public void PlayShieldBlock()
    {
        if (hitFlash != null)
            hitFlash.alpha = reducedMotion ? 0.16f : 0.32f;
        EmitAtPlayer(coinSparkleBurst, reducedMotion ? 8 : 22, new Vector3(0f, 0.65f, 0.08f));
    }

    public void SetReducedMotion(bool value)
    {
        reducedMotion = value;
        if (cameraRig != null)
            cameraRig.SetReducedMotion(value);
        if (reducedMotion)
            SetRunning(false);
        if (!reducedMotion)
            return;
        coinPunch = 0f;
        countdownPunch = 0f;
        if (coinLabel != null)
            coinLabel.rectTransform.localScale = coinLabelScale;
        if (countdownLabel != null)
        {
            countdownLabel.rectTransform.localScale = countdownScale;
            countdownLabel.rectTransform.localRotation = Quaternion.identity;
        }
    }

    private void PlayLanding()
    {
        EmitAtPlayer(landingDust, 8, new Vector3(0f, 0.04f, 0.18f));
    }

    private void PlaySlide()
    {
        if (player != null && player.JumpHeight > .05f)
            return;
        EmitAtPlayer(slideDust, 7, new Vector3(0f, .06f, .26f));
    }

    private void EmitAtPlayer(ParticleSystem system, int amount, Vector3 offset)
    {
        if (system == null || player == null)
            return;
        system.transform.position = player.transform.position + offset;
        system.Emit(amount);
    }

    private static void EmitAtWorld(ParticleSystem system, int amount, Vector3 position)
    {
        if (system == null)
            return;
        system.transform.position = position;
        system.Emit(amount);
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.Landed -= PlayLanding;
            player.SlideStarted -= PlaySlide;
        }
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        CatRunnerPlayer runner,
        CatRunnerCameraRig rig,
        TMP_Text coins,
        TMP_Text countdown,
        CanvasGroup flash,
        ParticleSystem coinParticles,
        ParticleSystem coinSparkles,
        ParticleSystem hitParticles,
        ParticleSystem dustParticles,
        ParticleSystem slideParticles,
        ParticleSystem streakParticles)
    {
        player = runner;
        cameraRig = rig;
        coinLabel = coins;
        countdownLabel = countdown;
        hitFlash = flash;
        coinBurst = coinParticles;
        coinSparkleBurst = coinSparkles;
        hitBurst = hitParticles;
        landingDust = dustParticles;
        slideDust = slideParticles;
        speedStreaks = streakParticles;
    }
#endif
}
