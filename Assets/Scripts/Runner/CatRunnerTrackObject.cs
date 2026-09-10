using UnityEngine;

public enum CatRunnerTrackObjectKind
{
    Coin = 0,
    Obstacle = 1,
    OverheadObstacle = 2,
    Platform = 3,
    PowerUp = 4
}

public enum CatRunnerPowerUpKind
{
    Magnet = 0,
    Shield = 1,
    DoubleCoins = 2,
    ScoreStar = 3,
    MysteryGift = 4
}

[DisallowMultipleComponent]
public sealed class CatRunnerTrackObject : MonoBehaviour
{
    [SerializeField] private CatRunnerTrackObjectKind kind;
    [SerializeField] private CatRunnerPowerUpKind powerUpKind;
    [SerializeField, Min(0.2f)] private float platformHeight = 0.82f;
    [SerializeField, Min(2f)] private float platformLength = 10f;
    [SerializeField, Min(0.25f)] private float rampLength = 2.2f;
    [SerializeField, Min(0.25f)] private float platformHalfWidth = 0.62f;
    [SerializeField] private float collisionBottomOffset;
    [SerializeField] private float collisionTopOffset = 0.48f;
    [SerializeField] private Bounds visualBounds = new Bounds(new Vector3(0,.3f,0),new Vector3(1,.6f,1));

    private Bounds motionBounds;
    private bool motionBoundsReady;
    public Bounds VisualBoundsAt(Vector3 position)
    {
        if(!motionBoundsReady)CacheMotionBounds(transform.localScale,transform.localRotation);
        return new Bounds(position+motionBounds.center,motionBounds.size);
    }
    private void CacheMotionBounds(Vector3 scale,Quaternion rotation)
    {
        bool coin=kind==CatRunnerTrackObjectKind.Coin;
        float yaw=IsPickup?(coin?18f:22f):0,roll=IsPickup?(coin?4.5f:5f):0;
        float pulse=IsPickup?(coin?1.065f:1.08f):1f;
        bool first=true;motionBounds=default;
        for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)for(int corner=0;corner<8;corner++)
        {
            Vector3 sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
            Vector3 vertex=visualBounds.center+Vector3.Scale(visualBounds.extents,sign);
            vertex=rotation*Quaternion.Euler(0,y*yaw,z*roll)*Vector3.Scale(vertex,scale)*pulse;
            if(first){motionBounds=new Bounds(vertex,Vector3.zero);first=false;}else motionBounds.Encapsulate(vertex);
        }
        // Includes the continuous rotation extrema between samples and the full bob.
        motionBounds.Expand(IsPickup?new Vector3(.05f,coin?.20f:.23f,.05f):Vector3.one*.04f);
        motionBoundsReady=true;
    }

    private float runtimeBaseHeight;
    private Vector3 runtimeBaseScale;
    private Quaternion runtimeBaseRotation;
    private float pulsePhase;
    private Transform coinHalo;
    private Transform coinSparkleA;
    private Transform coinSparkleB;

    public CatRunnerTrackObjectKind Kind => kind;
    public CatRunnerPowerUpKind PowerUpKind => powerUpKind;
    public float RuntimeBaseHeight => runtimeBaseHeight;
    public Quaternion RuntimeBaseRotation => runtimeBaseRotation;
    public float RuntimeDespawnZ => kind == CatRunnerTrackObjectKind.Platform
        ? -(Mathf.Max(2f, platformLength) * .5f + 2.5f)
        : -2.5f;
    public bool IsHazard => kind == CatRunnerTrackObjectKind.Obstacle ||
                            kind == CatRunnerTrackObjectKind.OverheadObstacle;
    public bool IsPickup => kind == CatRunnerTrackObjectKind.Coin ||
                            kind == CatRunnerTrackObjectKind.PowerUp;

    public void InitializeRuntime(float baseHeight)
    {
        runtimeBaseHeight = baseHeight;
        runtimeBaseScale = transform.localScale;
        runtimeBaseRotation = transform.localRotation;
        CacheMotionBounds(runtimeBaseScale,runtimeBaseRotation);
        pulsePhase = (EntityId.ToULong(GetEntityId()) % 97UL) * 0.071f;
        coinHalo = transform.Find("CoinHalo");
        coinSparkleA = transform.Find("CoinSparkleA");
        coinSparkleB = transform.Find("CoinSparkleB");
    }

    public void AnimateCoin(float time, bool reducedMotion = false)
    {
        if (kind != CatRunnerTrackObjectKind.Coin)
            return;
        if (reducedMotion)
        {
            transform.localScale = runtimeBaseScale;
            transform.localRotation = runtimeBaseRotation;
            return;
        }
        float pulse = 1f + Mathf.Sin(time * 5.5f + pulsePhase) * 0.065f;
        transform.localScale = runtimeBaseScale * pulse;
        // Keep the embossed paw face readable. A full 360-degree spin exposed
        // the dark rear/rim submeshes for long stretches and made coin trails
        // look like flat brown discs at gameplay distance.
        float showcaseYaw = Mathf.Sin(time * 2.6f + pulsePhase) * 18f;
        float showcaseRoll = Mathf.Sin(time * 3.1f + pulsePhase * .73f) * 4.5f;
        transform.localRotation = runtimeBaseRotation *
                                  Quaternion.Euler(0f, showcaseYaw, showcaseRoll);
        Vector3 position = transform.localPosition;
        position.y += Mathf.Sin(time * 4.2f + pulsePhase) * 0.075f;
        transform.localPosition = position;

        if (coinHalo != null)
        {
            float haloPulse = 1f + Mathf.Sin(time * 3.8f + pulsePhase) * .11f;
            coinHalo.localScale = Vector3.one * haloPulse;
            coinHalo.localRotation = Quaternion.Euler(0f, 0f, -time * 62f - pulsePhase * 24f);
        }
        AnimateSparkle(coinSparkleA, time, pulsePhase, 0f);
        AnimateSparkle(coinSparkleB, time, pulsePhase, Mathf.PI);
    }

    public void AnimatePowerUp(float time, bool reducedMotion = false)
    {
        if (kind != CatRunnerTrackObjectKind.PowerUp)
            return;
        if (reducedMotion)
        {
            transform.localScale = runtimeBaseScale;
            transform.localRotation = runtimeBaseRotation;
            return;
        }

        float pulse = 1f + Mathf.Sin(time * 4.4f + pulsePhase) * .08f;
        transform.localScale = runtimeBaseScale * pulse;
        transform.localRotation = runtimeBaseRotation *
                                  Quaternion.Euler(
                                      0f,
                                      Mathf.Sin(time * 2.25f + pulsePhase) * 22f,
                                      Mathf.Sin(time * 3.1f + pulsePhase) * 5f);
        Vector3 position = transform.localPosition;
        position.y += Mathf.Sin(time * 3.7f + pulsePhase) * .09f;
        transform.localPosition = position;
    }

    public void AnimateObstacle(float time, bool reducedMotion = false)
    {
        // Firm props keep the same silhouette as their collision envelope.
        transform.localScale = runtimeBaseScale;
        transform.localRotation = runtimeBaseRotation;
    }

    public void ResetForPool()
    {
        transform.localScale = runtimeBaseScale == Vector3.zero
            ? Vector3.one
            : runtimeBaseScale;
        transform.localRotation = runtimeBaseRotation;
    }

    public bool TrySamplePlatformHeight(float playerLanePosition, out float height)
    {
        return TrySamplePlatformHeightAt(playerLanePosition, 0f, out height);
    }

    public bool TrySamplePlatformHeightAt(
        float lanePosition,
        float sampleZ,
        out float height)
    {
        if (!TrySamplePlatformRelativeHeightAt(lanePosition, sampleZ, out float relativeHeight))
        {
            height = 0f;
            return false;
        }

        height = transform.localPosition.y + relativeHeight;
        return true;
    }

    /// <summary>
    /// Returns only the authored ramp/deck lift. Spawned coins and hazards use
    /// this value as their runtime base height because the track manager adds
    /// the rolling-road offset independently every frame.
    /// </summary>
    public bool TrySamplePlatformRelativeHeightAt(
        float lanePosition,
        float sampleZ,
        out float height)
    {
        height = 0f;
        if (kind != CatRunnerTrackObjectKind.Platform ||
            Mathf.Abs(transform.localPosition.x - lanePosition) > platformHalfWidth)
        {
            return false;
        }

        height = SamplePlatformHeight(
            transform.localPosition.z - sampleZ,
            platformHeight,
            platformLength,
            rampLength);
        return height > 0f;
    }

    public bool OverlapsVerticalInterval(float lower, float upper)
    {
        float obstacleLower = transform.localPosition.y + collisionBottomOffset;
        float obstacleUpper = transform.localPosition.y + collisionTopOffset;
        return HasVerticalOverlap(lower, upper, obstacleLower, obstacleUpper);
    }

    public static bool HasVerticalOverlap(
        float firstLower,
        float firstUpper,
        float secondLower,
        float secondUpper)
    {
        float firstMin = Mathf.Min(firstLower, firstUpper);
        float firstMax = Mathf.Max(firstLower, firstUpper);
        float secondMin = Mathf.Min(secondLower, secondUpper);
        float secondMax = Mathf.Max(secondLower, secondUpper);
        return firstMax > secondMin && secondMax > firstMin;
    }

    public static float SamplePlatformHeight(
        float platformCenterZ,
        float height = .82f,
        float totalLength = 10f,
        float transitionLength = 2.2f)
    {
        float safeHeight = Mathf.Max(0f, height);
        float safeLength = Mathf.Max(2f, totalLength);
        float halfLength = safeLength * .5f;
        float safeTransition = Mathf.Clamp(transitionLength, .25f, halfLength);
        float localZ = -platformCenterZ;
        if (localZ <= -halfLength || localZ >= halfLength)
            return 0f;

        float factor = 1f;
        if (localZ < -halfLength + safeTransition)
            factor = Mathf.InverseLerp(-halfLength, -halfLength + safeTransition, localZ);
        else if (localZ > halfLength - safeTransition)
            factor = Mathf.InverseLerp(halfLength, halfLength - safeTransition, localZ);

        factor = factor * factor * (3f - 2f * factor);
        return safeHeight * factor;
    }

    private static void AnimateSparkle(
        Transform sparkle,
        float time,
        float phase,
        float phaseOffset)
    {
        if (sparkle == null)
            return;
        float clock = time * 2.7f + phase + phaseOffset;
        float scale = .74f + (Mathf.Sin(clock) * .5f + .5f) * .46f;
        sparkle.localScale = Vector3.one * scale;
        sparkle.localRotation = Quaternion.Euler(0f, 0f, time * 96f + phaseOffset * 24f);
    }

#if UNITY_EDITOR
    public void EditorSetVisualBounds(Bounds bounds){visualBounds=bounds;motionBoundsReady=false;}
    public void EditorConfigure(
        CatRunnerTrackObjectKind value,
        float elevatedHeight = .82f,
        float elevatedLength = 10f,
        float transition = 2.2f,
        float halfWidth = .62f,
        float hazardBottom = 0f,
        float hazardTop = -1f,
        CatRunnerPowerUpKind pickupKind = CatRunnerPowerUpKind.Magnet)
    {
        kind = value;
        powerUpKind = pickupKind;
        platformHeight = Mathf.Max(.2f, elevatedHeight);
        platformLength = Mathf.Max(2f, elevatedLength);
        rampLength = Mathf.Max(.25f, transition);
        platformHalfWidth = Mathf.Max(.25f, halfWidth);
        if (hazardTop > hazardBottom)
        {
            collisionBottomOffset = hazardBottom;
            collisionTopOffset = hazardTop;
        }
        else if (value == CatRunnerTrackObjectKind.OverheadObstacle)
        {
            collisionBottomOffset = .46f;
            collisionTopOffset = 1.08f;
        }
        else
        {
            collisionBottomOffset = 0f;
            collisionTopOffset = .48f;
        }
    }
#endif
}
