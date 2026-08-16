using UnityEngine;

/// <summary>
/// Lightweight runner camera motion without a Cinemachine dependency. The rig
/// follows only a fraction of the cat's movement so the lanes remain readable,
/// while speed, road slope and short feedback impulses keep the shot alive.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class CatRunnerCameraRig : MonoBehaviour
{
    [SerializeField] private CatRunnerGameController game;
    [SerializeField] private CatRunnerPlayer player;
    [SerializeField] private CatRunnerTrackManager track;
    [SerializeField, Range(0f, 1f)] private float laneFollow = 0.28f;
    [SerializeField, Range(0f, 1f)] private float jumpFollow = 0.24f;
    [SerializeField] private float positionSmoothTime = 0.14f;
    [SerializeField] private float rotationResponsiveness = 8f;
    [SerializeField] private float maximumLaneRoll = 4.5f;
    [SerializeField] private float baseFieldOfView = 56f;
    [SerializeField] private float maximumFieldOfView = 64f;

    private Camera targetCamera;
    private Vector3 authoredLocalPosition;
    private Quaternion authoredLocalRotation;
    private Vector3 positionVelocity;
    private float fieldOfViewVelocity;
    private float previousLanePosition;
    private float smoothedLaneVelocity;
    private float trauma;
    private float runKick;
    private float coinKick;
    private bool reducedMotion;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        if (game == null)
            game = FindAnyObjectByType<CatRunnerGameController>(FindObjectsInactive.Include);
        if (player == null)
            player = FindAnyObjectByType<CatRunnerPlayer>(FindObjectsInactive.Include);
        if (track == null)
            track = FindAnyObjectByType<CatRunnerTrackManager>(FindObjectsInactive.Include);

        authoredLocalPosition = transform.localPosition;
        authoredLocalRotation = transform.localRotation;
        if (targetCamera != null)
        {
            if (baseFieldOfView <= 1f)
                baseFieldOfView = targetCamera.fieldOfView;
            targetCamera.fieldOfView = baseFieldOfView;
        }
        previousLanePosition = player != null ? player.LanePosition : 0f;
    }

    private void LateUpdate()
    {
        float delta = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
        float lane = player != null ? player.LanePosition : 0f;
        float laneVelocity = (lane - previousLanePosition) / delta;
        previousLanePosition = lane;
        smoothedLaneVelocity = Mathf.Lerp(
            smoothedLaneVelocity,
            laneVelocity,
            1f - Mathf.Exp(-10f * delta));

        float jumpHeight = player != null ? player.Height : 0f;
        float speed01 = game != null
            ? Mathf.InverseLerp(1f, 1.7f, game.CurrentDifficultyMultiplier)
            : 0f;
        float running = game != null && game.IsRunning ? 1f : 0f;
        float clock = Time.unscaledTime;
        float breathing = reducedMotion ? 0f : Mathf.Sin(clock * 1.65f) * 0.018f;
        float sideSway = reducedMotion
            ? 0f
            : Mathf.Sin(clock * 1.12f + 0.8f) * 0.018f * (0.35f + running);

        runKick = Mathf.MoveTowards(runKick, 0f, delta * 1.8f);
        coinKick = Mathf.MoveTowards(coinKick, 0f, delta * 5f);
        trauma = Mathf.MoveTowards(trauma, 0f, delta * 1.9f);

        float shakeX = reducedMotion
            ? 0f
            : (Mathf.PerlinNoise(clock * 22f, 1.7f) - 0.5f) * trauma * 0.22f;
        float shakeY = reducedMotion
            ? 0f
            : (Mathf.PerlinNoise(2.9f, clock * 25f) - 0.5f) * trauma * 0.16f;
        Vector3 targetPosition = authoredLocalPosition + new Vector3(
            lane * laneFollow + sideSway + shakeX,
            jumpHeight * jumpFollow + breathing + shakeY,
            -runKick * 0.55f);
        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            targetPosition,
            ref positionVelocity,
            Mathf.Max(0.04f, positionSmoothTime),
            Mathf.Infinity,
            delta);

        float roadPitch = track != null
            ? -Mathf.Atan(track.CurrentRoadSlope) * Mathf.Rad2Deg * 0.62f
            : 0f;
        float jumpPitch = Mathf.Clamp(jumpHeight * -1.8f, -2.5f, 0f);
        float laneRoll = Mathf.Clamp(
            -smoothedLaneVelocity * 0.52f,
            -maximumLaneRoll,
            maximumLaneRoll);
        float shakeRoll = reducedMotion
            ? 0f
            : (Mathf.PerlinNoise(clock * 27f, 6.1f) - 0.5f) * trauma * 5f;
        Quaternion targetRotation = authoredLocalRotation * Quaternion.Euler(
            roadPitch + jumpPitch,
            sideSway * 12f,
            laneRoll + shakeRoll);
        float rotationBlend = 1f - Mathf.Exp(-Mathf.Max(1f, rotationResponsiveness) * delta);
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            rotationBlend);

        if (targetCamera != null)
        {
            float desiredFov = Mathf.Lerp(baseFieldOfView, maximumFieldOfView, speed01) +
                               (reducedMotion ? 0f : runKick * 2.2f + coinKick);
            targetCamera.fieldOfView = Mathf.SmoothDamp(
                targetCamera.fieldOfView,
                desiredFov,
                ref fieldOfViewVelocity,
                0.18f,
                Mathf.Infinity,
                delta);
        }
    }

    public void BeginRunKick()
    {
        runKick = 1f;
    }

    public void PulseCoin()
    {
        coinKick = Mathf.Max(coinKick, 0.55f);
    }

    public void AddHitTrauma(float amount = 0.65f)
    {
        trauma = Mathf.Clamp01(Mathf.Max(trauma, amount));
    }

    public void SetReducedMotion(bool value)
    {
        reducedMotion = value;
        if (!reducedMotion)
            return;
        trauma = 0f;
        runKick = 0f;
        coinKick = 0f;
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        CatRunnerGameController controller,
        CatRunnerPlayer runner,
        CatRunnerTrackManager manager)
    {
        game = controller;
        player = runner;
        track = manager;
        laneFollow = 0.28f;
        jumpFollow = 0.24f;
        positionSmoothTime = 0.14f;
        rotationResponsiveness = 8f;
        maximumLaneRoll = 4.5f;
        baseFieldOfView = 56f;
        maximumFieldOfView = 64f;
    }
#endif
}
