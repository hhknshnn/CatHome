using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public sealed class CatRunnerTrackManager : MonoBehaviour
{
    public const int MaximumHazardLanesPerRow = 2;
    [SerializeField] private CatRunnerGameController game;
    [SerializeField] private CatRunnerPlayer player;
    [SerializeField] private Transform[] trackSegments = Array.Empty<Transform>();
    [SerializeField] private float segmentLength = 8f;
    [SerializeField] private float laneWidth = 1.35f;
    [SerializeField, Min(1f)] private float objectApproachSpeedMultiplier = 1.35f;
    [SerializeField, Min(0.5f)] private float firstCurtainObstacleInterval = 1.4f;
    [SerializeField, Min(0.5f)] private float minimumObstacleInterval = 0.8f;
    [SerializeField] private GameObject coinTemplate;
    [SerializeField] private GameObject[] obstacleTemplates = Array.Empty<GameObject>();
    [SerializeField] private GameObject[] overheadObstacleTemplates = Array.Empty<GameObject>();
    [SerializeField] private GameObject[] platformTemplates = Array.Empty<GameObject>();
    [SerializeField] private GameObject[] powerUpTemplates = Array.Empty<GameObject>();
    [SerializeField, Min(3f)] private float minimumElevationInterval = 8.5f;
    [SerializeField, Min(3f)] private float maximumElevationInterval = 12.5f;
    [SerializeField, Min(5f)] private float minimumPowerUpInterval = 18f;
    [SerializeField, Min(5f)] private float maximumPowerUpInterval = 26f;

    private readonly List<CatRunnerTrackObject> activeObjects =
        new List<CatRunnerTrackObject>();
    private readonly Dictionary<GameObject, Stack<CatRunnerTrackObject>> pools =
        new Dictionary<GameObject, Stack<CatRunnerTrackObject>>();
    private readonly Dictionary<CatRunnerTrackObject, GameObject> poolOrigins =
        new Dictionary<CatRunnerTrackObject, GameObject>();
    private Vector3[] initialSegmentPositions = Array.Empty<Vector3>();
    private Quaternion[] initialSegmentRotations = Array.Empty<Quaternion>();
    private System.Random random;
    private float untilNextObstacle;
    private float untilNextElevation;
    private float untilNextPowerUp;
    private int spawnedCoins;
    private int coinFormationRemaining;
    private int coinFormationLane;
    private int coinFormationIndex;
    private bool coinFormationHasJump;
    private int attemptNumber;
    private float travelledDistance;
    private int recycledSegmentCount;
    private bool tutorialSafety;

    public float ObjectApproachSpeedMultiplier =>
        Mathf.Max(1f, objectApproachSpeedMultiplier);
    public int SpawnedCoins => spawnedCoins;
    public int ActiveObjectCount => activeObjects.Count;
    public int PooledObjectCount
    {
        get
        {
            int count = 0;
            foreach (KeyValuePair<GameObject, Stack<CatRunnerTrackObject>> pair in pools)
                count += pair.Value.Count;
            return count;
        }
    }
    public float CurrentRoadSlope => GetRoadSlope(travelledDistance);
    public float CurrentObstacleInterval => GetObstacleInterval(
        game != null ? game.CurrentCurtainNumber : 1,
        firstCurtainObstacleInterval,
        minimumObstacleInterval);

    public static float GetObstacleInterval(
        int curtainNumber,
        float firstInterval = 1.4f,
        float minimumInterval = 0.8f)
    {
        int completedCurtains = Mathf.Max(0, curtainNumber - 1);
        float interval = Mathf.Max(0.5f, firstInterval) * Mathf.Pow(0.86f, completedCurtains);
        return Mathf.Max(Mathf.Max(0.5f, minimumInterval), interval);
    }

    public static float GetDoubleObstacleChance(int curtainNumber)
    {
        return Mathf.Min(0.55f, 0.1f + Mathf.Max(0, curtainNumber - 1) * 0.12f);
    }

    public static float GetOverheadObstacleChance(int curtainNumber)
    {
        return Mathf.Min(.38f, .16f + Mathf.Max(0, curtainNumber - 1) * .045f);
    }

    public static float GetElevationInterval(
        float random01,
        float minimum = 8.5f,
        float maximum = 12.5f)
    {
        float safeMinimum = Mathf.Max(3f, minimum);
        float safeMaximum = Mathf.Max(safeMinimum, maximum);
        return Mathf.Lerp(safeMinimum, safeMaximum, Mathf.Clamp01(random01));
    }

    /// <summary>
    /// Deterministic rolling road profile. The two long waves keep the changes
    /// gentle enough for lane switching while avoiding an endlessly flat track.
    /// </summary>
    public static float GetRoadHeight(float distance)
    {
        distance = Mathf.Max(0f, distance);
        return Mathf.Sin(distance * 0.075f) * 0.82f +
               Mathf.Sin(distance * 0.028f + 0.7f) * 0.32f;
    }

    public static float GetRoadSlope(float distance)
    {
        distance = Mathf.Max(0f, distance);
        return Mathf.Cos(distance * 0.075f) * 0.075f * 0.82f +
               Mathf.Cos(distance * 0.028f + 0.7f) * 0.028f * 0.32f;
    }

    private void Awake()
    {
        if (game == null)
            game = FindAnyObjectByType<CatRunnerGameController>(FindObjectsInactive.Include);
        if (player == null)
            player = FindAnyObjectByType<CatRunnerPlayer>(FindObjectsInactive.Include);

        initialSegmentPositions = new Vector3[trackSegments.Length];
        initialSegmentRotations = new Quaternion[trackSegments.Length];
        for (int i = 0; i < trackSegments.Length; i++)
            if (trackSegments[i] != null)
            {
                initialSegmentPositions[i] = trackSegments[i].localPosition;
                initialSegmentRotations[i] = trackSegments[i].localRotation;
            }

        PrewarmPools();
        ResetRun();
    }

    private void Update()
    {
        if (game == null || !game.IsGameplayActive)
            return;

        float runnerDistance = game.CurrentSpeed * Time.deltaTime;
        ScrollSegments(runnerDistance);

        // The corridor is a visual speed reference. Obstacles and coins need a
        // stronger closing speed so they clearly approach the cat instead of
        // appearing to ride forward with the scrolling floor.
        ScrollObjects(runnerDistance * ObjectApproachSpeedMultiplier);

        int scheduledCoins = CatRunnerGameController.GetScheduledCoinCount(game.ElapsedSeconds);
        while (spawnedCoins < scheduledCoins)
        {
            SpawnCoinPickup();
            spawnedCoins++;
        }

        if (!tutorialSafety)
        {
            untilNextObstacle -= Time.deltaTime;
            if (untilNextObstacle <= 0f)
            {
                SpawnObstacleRow();
                untilNextObstacle += CurrentObstacleInterval;
            }

            untilNextElevation -= Time.deltaTime;
            if (untilNextElevation <= 0f)
            {
                SpawnElevationRoute();
                untilNextElevation += GetElevationInterval(
                    random != null ? (float)random.NextDouble() : .5f,
                    minimumElevationInterval,
                    maximumElevationInterval);
            }

            untilNextPowerUp -= Time.deltaTime;
            if (untilNextPowerUp <= 0f)
            {
                SpawnPowerUp();
                untilNextPowerUp += GetPowerUpInterval();
            }
        }
    }

    public void ResetRun()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            if (activeObjects[i] != null)
                ReturnToPool(activeObjects[i]);
        }
        activeObjects.Clear();

        for (int i = 0; i < trackSegments.Length && i < initialSegmentPositions.Length; i++)
            if (trackSegments[i] != null)
            {
                trackSegments[i].localPosition = initialSegmentPositions[i];
                trackSegments[i].localRotation = i < initialSegmentRotations.Length
                    ? initialSegmentRotations[i]
                    : Quaternion.identity;
                CatRunnerThemeSegment theme = trackSegments[i]
                    .GetComponent<CatRunnerThemeSegment>();
                if (theme != null)
                    theme.ApplyTheme(0);
                CatRunnerScenerySegment scenery = trackSegments[i]
                    .GetComponent<CatRunnerScenerySegment>();
                if (scenery != null)
                    scenery.ApplyVariant(i);
            }

        random = new System.Random(7301 + attemptNumber * 97);
        attemptNumber++;
        spawnedCoins = 0;
        coinFormationRemaining = 0;
        coinFormationLane = 0;
        coinFormationIndex = 0;
        coinFormationHasJump = false;
        untilNextObstacle = 1.6f;
        untilNextElevation = 5.4f;
        untilNextPowerUp = 15f;
        travelledDistance = 0f;
        recycledSegmentCount = trackSegments.Length;
    }

    private void ScrollSegments(float distance)
    {
        travelledDistance += Mathf.Max(0f, distance);
        float totalLength = segmentLength * trackSegments.Length;
        float currentRoadHeight = GetRoadHeight(travelledDistance);
        for (int i = 0; i < trackSegments.Length; i++)
        {
            Transform segment = trackSegments[i];
            if (segment == null)
                continue;

            Vector3 position = segment.localPosition;
            position.z -= distance;
            bool recycled = false;
            if (position.z < -segmentLength)
            {
                position.z += totalLength;
                recycled = true;
            }

            float profileDistance = travelledDistance + position.z;
            position.y = GetRoadHeight(profileDistance) - currentRoadHeight;
            segment.localPosition = position;
            float pitch = -Mathf.Atan(GetRoadSlope(profileDistance)) * Mathf.Rad2Deg;
            segment.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            if (recycled)
            {
                CatRunnerThemeSegment theme = segment.GetComponent<CatRunnerThemeSegment>();
                if (theme != null)
                    theme.ApplyTheme(Mathf.FloorToInt(
                        (game != null ? game.ElapsedSeconds : 0f) / 45f));
                CatRunnerScenerySegment scenery = segment
                    .GetComponent<CatRunnerScenerySegment>();
                if (scenery != null)
                    scenery.ApplyVariant(recycledSegmentCount);
                recycledSegmentCount++;
            }
        }
    }

    private void ScrollObjects(float distance)
    {
        float playerSurfaceHeight = 0f;
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            CatRunnerTrackObject item = activeObjects[i];
            if (item == null)
            {
                activeObjects.RemoveAt(i);
                continue;
            }

            Transform itemTransform = item.transform;
            Vector3 position = itemTransform.localPosition;
            float previousZ = position.z;
            position.z -= distance;
            position.y = item.RuntimeBaseHeight +
                         GetRoadHeight(travelledDistance + position.z) -
                         GetRoadHeight(travelledDistance);

            if (item.Kind == CatRunnerTrackObjectKind.Coin &&
                game != null && game.IsMagnetActive && player != null &&
                position.z >= -1.25f && position.z <= game.MagnetRange)
            {
                float attraction = Mathf.Lerp(5.5f, 13f, Mathf.InverseLerp(
                    game.MagnetRange,
                    -1.25f,
                    position.z));
                position.x = Mathf.MoveTowards(
                    position.x,
                    player.LanePosition,
                    attraction * Time.deltaTime);
                position.y = Mathf.MoveTowards(
                    position.y,
                    player.Height + .42f,
                    attraction * .62f * Time.deltaTime);
            }
            itemTransform.localPosition = position;

            if (item.Kind == CatRunnerTrackObjectKind.Platform && player != null &&
                item.TrySamplePlatformHeight(player.LanePosition, out float sampledHeight))
            {
                playerSurfaceHeight = Mathf.Max(playerSurfaceHeight, sampledHeight);
            }

            if (item.Kind == CatRunnerTrackObjectKind.Coin)
            {
                item.AnimateCoin(Time.time, CatRunnerProgressService.ReducedMotion);
            }
            else if (item.Kind == CatRunnerTrackObjectKind.PowerUp)
            {
                item.AnimatePowerUp(Time.time, CatRunnerProgressService.ReducedMotion);
            }
            else if (item.IsHazard)
            {
                item.AnimateObstacle(
                    Time.time,
                    CatRunnerProgressService.ReducedMotion);
            }

            if (position.z < item.RuntimeDespawnZ)
            {
                bool missedCoin = item.Kind == CatRunnerTrackObjectKind.Coin;
                ReleaseAt(i);
                if (missedCoin && game != null)
                    game.RegisterCoinMissed();
                continue;
            }

            if (item.Kind == CatRunnerTrackObjectKind.Platform ||
                !CrossesInteractionBand(previousZ, position.z, .55f) ||
                player == null)
                continue;

            float lateralDistance = Mathf.Abs(position.x - player.LanePosition);
            float lateralTolerance = item.IsPickup ? .68f : .7f;
            if (lateralDistance > lateralTolerance)
                continue;

            if (item.Kind == CatRunnerTrackObjectKind.Coin)
            {
                if (Mathf.Abs(position.y - player.Height) <= 0.8f)
                {
                    game.RegisterCoin(itemTransform.position);
                    ReleaseAt(i);
                }
            }
            else if (item.Kind == CatRunnerTrackObjectKind.PowerUp)
            {
                if (Mathf.Abs(position.y - player.Height) <= 1.05f)
                {
                    game.RegisterPowerUp(item.PowerUpKind, itemTransform.position);
                    ReleaseAt(i);
                }
            }
            else if (item.IsHazard && item.OverlapsVerticalInterval(
                         player.CollisionLower,
                         player.CollisionUpper))
            {
                game.RegisterObstacleHit();
                ReleaseAt(i);
            }
        }

        if (player != null)
            player.SetTrackSurfaceHeight(playerSurfaceHeight);
    }

    public static bool CrossesInteractionBand(
        float previousZ,
        float currentZ,
        float halfWidth)
    {
        float width = Mathf.Max(0f, halfWidth);
        float minimum = Mathf.Min(previousZ, currentZ);
        float maximum = Mathf.Max(previousZ, currentZ);
        return minimum <= width && maximum >= -width;
    }

    private void SpawnCoinPickup()
    {
        if (random == null)
            random = new System.Random(7301);

        const float spawnZ = 27f;
        if (coinFormationRemaining <= 0)
        {
            coinFormationLane = TryGetPlatformLaneAt(spawnZ, out int platformLane) &&
                                random.NextDouble() < .82d
                ? platformLane
                : PickLaneAvoiding(CatRunnerTrackObjectKind.Obstacle, spawnZ);
            coinFormationRemaining = 5;
            coinFormationIndex = 0;
            coinFormationHasJump = random.NextDouble() < 0.45d;
        }

        float height = GetFormationCoinHeight(coinFormationIndex, coinFormationHasJump) +
                       GetPlatformHeightAt(coinFormationLane, spawnZ);
        SpawnCoin(coinFormationLane, spawnZ, height);

        // The rising coins teach the gesture naturally: the apex coin sits over
        // a real obstacle, so following the arc with an upward swipe both earns
        // the coin and clears the hazard.
        if (!tutorialSafety && coinFormationHasJump && coinFormationIndex == 2)
            SpawnObstacle(coinFormationLane, spawnZ);

        coinFormationIndex++;
        coinFormationRemaining--;
    }

    public static float GetFormationCoinHeight(int index, bool jumpFormation)
    {
        if (!jumpFormation)
            return 0.42f;

        switch (Mathf.Clamp(index, 0, 4))
        {
            case 1:
            case 3:
                return 0.78f;
            case 2:
                return 1.18f;
            default:
                return 0.42f;
        }
    }

    private void SpawnObstacleRow()
    {
        if (random == null)
            random = new System.Random(7301);

        const float spawnZ = 27f;
        // Most rows preserve the visible coin route, but not all of them. A
        // regular obstacle may cross a flat trail and ask for a jump or lane
        // change instead of leaving an always-safe corridor down the track.
        int reservedCoinLane = random.NextDouble() < 0.55d ? coinFormationLane : 99;
        int availableHazardSlots = Mathf.Max(
            0,
            MaximumHazardLanesPerRow - CountHazardsNear(spawnZ, 1.2f));
        if (availableHazardSlots <= 0)
            return;
        int primaryLane = PickAvailableObstacleLane(spawnZ, reservedCoinLane);
        if (primaryLane == 99)
            return;
        SpawnRandomHazard(primaryLane, spawnZ);
        availableHazardSlots--;

        if (availableHazardSlots <= 0 ||
            random.NextDouble() >= GetDoubleObstacleChance(
                game != null ? game.CurrentCurtainNumber : 1))
        {
            return;
        }

        int secondaryLane = PickAvailableObstacleLane(spawnZ, reservedCoinLane);
        if (secondaryLane != 99)
            SpawnRandomHazard(secondaryLane, spawnZ);
    }

    private int CountHazardsNear(float z, float range)
    {
        int count = 0;
        for (int i = 0; i < activeObjects.Count; i++)
        {
            CatRunnerTrackObject item = activeObjects[i];
            if (item != null && item.IsHazard &&
                Mathf.Abs(item.transform.localPosition.z - z) <= range)
            {
                count++;
            }
        }
        return count;
    }

    private void SpawnElevationRoute()
    {
        if (random == null)
            random = new System.Random(7301);
        if (platformTemplates == null || platformTemplates.Length == 0)
            return;

        const float spawnZ = 27f;
        int start = random.Next(0, 3);
        int selectedLane = 99;
        for (int offset = 0; offset < 3; offset++)
        {
            int lane = (start + offset) % 3 - 1;
            if (!HasHazardNearLane(lane, spawnZ, 4.5f))
            {
                selectedLane = lane;
                break;
            }
        }
        if (selectedLane == 99)
            return;

        GameObject template = platformTemplates[random.Next(0, platformTemplates.Length)];
        Spawn(template, selectedLane, spawnZ, 0f);
    }

    private int PickLaneAvoiding(CatRunnerTrackObjectKind kind, float spawnZ)
    {
        bool[] blocked = new bool[3];
        for (int i = 0; i < activeObjects.Count; i++)
        {
            CatRunnerTrackObject item = activeObjects[i];
            bool kindMatches = kind == CatRunnerTrackObjectKind.Obstacle
                ? item != null && item.IsHazard
                : item != null && item.Kind == kind;
            if (!kindMatches || Mathf.Abs(item.transform.localPosition.z - spawnZ) > 1.5f)
                continue;
            int laneIndex = Mathf.RoundToInt(item.transform.localPosition.x / laneWidth) + 1;
            if (laneIndex >= 0 && laneIndex < blocked.Length)
                blocked[laneIndex] = true;
        }

        int start = random.Next(0, 3);
        for (int offset = 0; offset < 3; offset++)
        {
            int index = (start + offset) % 3;
            if (!blocked[index])
                return index - 1;
        }

        return start - 1;
    }

    private int PickAvailableObstacleLane(float spawnZ, int excludedLane)
    {
        int start = random.Next(0, 3);
        for (int offset = 0; offset < 3; offset++)
        {
            int lane = (start + offset) % 3 - 1;
            if (lane == excludedLane || HasObstacleNearLane(lane, spawnZ))
                continue;
            return lane;
        }

        return 99;
    }

    private bool HasObstacleNearLane(int lane, float spawnZ)
    {
        for (int i = 0; i < activeObjects.Count; i++)
        {
            CatRunnerTrackObject item = activeObjects[i];
            if (item == null || !item.IsHazard)
                continue;
            if (Mathf.Abs(item.transform.localPosition.z - spawnZ) > 1.2f)
                continue;
            int occupiedLane = Mathf.RoundToInt(item.transform.localPosition.x / laneWidth);
            if (occupiedLane == lane)
                return true;
        }

        return false;
    }

    private bool HasHazardNearLane(int lane, float z, float longitudinalRange)
    {
        for (int i = 0; i < activeObjects.Count; i++)
        {
            CatRunnerTrackObject item = activeObjects[i];
            if (item == null || !item.IsHazard)
                continue;
            if (Mathf.Abs(item.transform.localPosition.z - z) > longitudinalRange)
                continue;
            int occupiedLane = Mathf.RoundToInt(item.transform.localPosition.x / laneWidth);
            if (occupiedLane == lane)
                return true;
        }
        return false;
    }

    private bool TryGetPlatformLaneAt(float z, out int lane)
    {
        lane = 0;
        float bestHeight = 0f;
        bool found = false;
        for (int i = 0; i < activeObjects.Count; i++)
        {
            CatRunnerTrackObject item = activeObjects[i];
            if (item == null || item.Kind != CatRunnerTrackObjectKind.Platform)
                continue;
            int itemLane = Mathf.RoundToInt(item.transform.localPosition.x / laneWidth);
            float height = GetPlatformHeight(item, z, itemLane);
            if (height <= bestHeight)
                continue;
            bestHeight = height;
            lane = itemLane;
            found = true;
        }
        return found;
    }

    private float GetPlatformHeightAt(int lane, float z)
    {
        float result = 0f;
        for (int i = 0; i < activeObjects.Count; i++)
        {
            CatRunnerTrackObject item = activeObjects[i];
            if (item == null || item.Kind != CatRunnerTrackObjectKind.Platform)
                continue;
            result = Mathf.Max(result, GetPlatformHeight(item, z, lane));
        }
        return result;
    }

    private float GetPlatformHeight(CatRunnerTrackObject item, float z, int lane)
    {
        float laneX = lane * laneWidth;
        return item.TrySamplePlatformRelativeHeightAt(laneX, z, out float height)
            ? height
            : 0f;
    }

    private void SpawnCoin(int lane, float z, float y)
    {
        Spawn(coinTemplate, lane, z, y);
    }

    private void SpawnObstacle(int lane, float z)
    {
        if (obstacleTemplates == null || obstacleTemplates.Length == 0)
            return;
        GameObject template = obstacleTemplates[random.Next(0, obstacleTemplates.Length)];
        Spawn(template, lane, z, GetPlatformHeightAt(lane, z));
    }

    private void SpawnRandomHazard(int lane, float z)
    {
        int curtain = game != null ? game.CurrentCurtainNumber : 1;
        bool useOverhead = overheadObstacleTemplates != null &&
                           overheadObstacleTemplates.Length > 0 &&
                           random.NextDouble() < GetOverheadObstacleChance(curtain);
        if (!useOverhead)
        {
            SpawnObstacle(lane, z);
            return;
        }

        GameObject template = overheadObstacleTemplates[
            random.Next(0, overheadObstacleTemplates.Length)];
        Spawn(template, lane, z, GetPlatformHeightAt(lane, z));
    }

    private void SpawnPowerUp()
    {
        if (powerUpTemplates == null || powerUpTemplates.Length == 0)
            return;
        if (random == null)
            random = new System.Random(7301);

        const float spawnZ = 27f;
        int lane = PickLaneAvoiding(CatRunnerTrackObjectKind.Obstacle, spawnZ);
        GameObject template = powerUpTemplates[random.Next(0, powerUpTemplates.Length)];
        float height = GetPlatformHeightAt(lane, spawnZ) + .52f;
        Spawn(template, lane, spawnZ, height);
    }

    private float GetPowerUpInterval()
    {
        float minimum = Mathf.Max(5f, minimumPowerUpInterval);
        float maximum = Mathf.Max(minimum, maximumPowerUpInterval);
        float random01 = random != null ? (float)random.NextDouble() : .5f;
        return Mathf.Lerp(minimum, maximum, random01);
    }

    private void Spawn(GameObject template, int lane, float z, float y)
    {
        if (template == null)
            return;

        CatRunnerTrackObject item = TakeFromPool(template);
        if (item == null)
            return;
        GameObject instance = item.gameObject;
        instance.name = template.name.Replace("_Template", string.Empty);
        instance.transform.localPosition = new Vector3(lane * laneWidth, y, z);
        instance.transform.localRotation = template.transform.localRotation;
        instance.transform.localScale = template.transform.localScale;
        item.InitializeRuntime(y);
        instance.SetActive(true);
        activeObjects.Add(item);
    }

    public void SetTutorialSafety(bool value)
    {
        tutorialSafety = value;
        if (tutorialSafety)
        {
            // Existing hazards are returned immediately so a first-run prompt
            // can never appear behind an unavoidable obstacle.
            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                CatRunnerTrackObject item = activeObjects[i];
                if (item != null && item.IsHazard)
                    ReleaseAt(i);
            }
        }
    }

    private void PrewarmPools()
    {
        Prewarm(coinTemplate, 24);
        PrewarmArray(obstacleTemplates, 4);
        PrewarmArray(overheadObstacleTemplates, 3);
        PrewarmArray(platformTemplates, 2);
        PrewarmArray(powerUpTemplates, 2);
    }

    private void PrewarmArray(GameObject[] templates, int countEach)
    {
        if (templates == null)
            return;
        for (int i = 0; i < templates.Length; i++)
            Prewarm(templates[i], countEach);
    }

    private void Prewarm(GameObject template, int count)
    {
        if (template == null || count <= 0)
            return;
        EnsurePool(template);
        for (int i = 0; i < count; i++)
        {
            CatRunnerTrackObject item = CreatePooledInstance(template);
            if (item != null)
                pools[template].Push(item);
        }
    }

    private CatRunnerTrackObject TakeFromPool(GameObject template)
    {
        EnsurePool(template);
        Stack<CatRunnerTrackObject> pool = pools[template];
        while (pool.Count > 0)
        {
            CatRunnerTrackObject item = pool.Pop();
            if (item != null)
                return item;
        }
        return CreatePooledInstance(template);
    }

    private CatRunnerTrackObject CreatePooledInstance(GameObject template)
    {
        GameObject instance = Instantiate(template, transform);
        instance.SetActive(false);
        CatRunnerTrackObject item = instance.GetComponent<CatRunnerTrackObject>();
        if (item == null)
        {
            Destroy(instance);
            return null;
        }
        poolOrigins[item] = template;
        return item;
    }

    private void EnsurePool(GameObject template)
    {
        if (!pools.ContainsKey(template))
            pools.Add(template, new Stack<CatRunnerTrackObject>());
    }

    private void ReleaseAt(int index)
    {
        if (index < 0 || index >= activeObjects.Count)
            return;
        CatRunnerTrackObject item = activeObjects[index];
        activeObjects.RemoveAt(index);
        ReturnToPool(item);
    }

    private void ReturnToPool(CatRunnerTrackObject item)
    {
        if (item == null)
            return;
        item.ResetForPool();
        item.gameObject.SetActive(false);
        if (!poolOrigins.TryGetValue(item, out GameObject template) || template == null)
        {
            Destroy(item.gameObject);
            return;
        }
        EnsurePool(template);
        pools[template].Push(item);
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        CatRunnerGameController controller,
        CatRunnerPlayer runnerPlayer,
        Transform[] segments,
        GameObject coin,
        GameObject[] obstacles)
    {
        game = controller;
        player = runnerPlayer;
        trackSegments = segments ?? Array.Empty<Transform>();
        coinTemplate = coin;
        obstacleTemplates = obstacles ?? Array.Empty<GameObject>();
        overheadObstacleTemplates = Array.Empty<GameObject>();
        platformTemplates = Array.Empty<GameObject>();
        objectApproachSpeedMultiplier = 1.35f;
        firstCurtainObstacleInterval = 1.4f;
        minimumObstacleInterval = 0.8f;
    }

    public void EditorConfigurePremiumRoutes(
        GameObject[] overheadObstacles,
        GameObject[] platforms,
        GameObject[] powerUps = null)
    {
        overheadObstacleTemplates = overheadObstacles ?? Array.Empty<GameObject>();
        platformTemplates = platforms ?? Array.Empty<GameObject>();
        powerUpTemplates = powerUps ?? Array.Empty<GameObject>();
        minimumElevationInterval = 8.5f;
        maximumElevationInterval = 12.5f;
        minimumPowerUpInterval = 18f;
        maximumPowerUpInterval = 26f;
    }
#endif
}
