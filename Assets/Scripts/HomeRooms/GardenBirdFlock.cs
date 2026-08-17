using UnityEngine;

/// <summary>
/// One or two courtyard birds that visit the garden tree, then leave.
/// They never collide and never grant rewards.
/// </summary>
[DisallowMultipleComponent]
public sealed class GardenBirdFlock : MonoBehaviour
{
    private enum Visit
    {
        Away = 0,
        Arrive = 1,
        Perch = 2,
        Leave = 3
    }

    [SerializeField] private Transform[] birds = System.Array.Empty<Transform>();
    [SerializeField] private Transform roost;
    [SerializeField] private float arriveSeconds = 3.4f;
    [SerializeField] private float perchSeconds = 2.6f;
    [SerializeField] private float leaveSeconds = 3.1f;
    [SerializeField] private float awaySeconds = 5.5f;

    private Visit[] phases = System.Array.Empty<Visit>();
    private float[] timers = System.Array.Empty<float>();
    private Vector3[] fromPoints = System.Array.Empty<Vector3>();
    private Vector3[] toPoints = System.Array.Empty<Vector3>();

    public int BirdCount => birds != null ? birds.Length : 0;

    private void Awake()
    {
        EnsureRuntimeBuffers();
        for (int i = 0; i < birds.Length; i++)
            BeginAway(i, i * 2.4f);
    }

    private void Update()
    {
        if (birds == null || roost == null)
            return;

        for (int i = 0; i < birds.Length; i++)
        {
            Transform bird = birds[i];
            if (bird == null)
                continue;

            timers[i] += Time.deltaTime;
            switch (phases[i])
            {
                case Visit.Away:
                    if (timers[i] >= awaySeconds + i * 1.8f)
                        BeginArrive(i);
                    break;
                case Visit.Arrive:
                    Fly(bird, fromPoints[i], PerchPoint(i), timers[i] / arriveSeconds);
                    if (timers[i] >= arriveSeconds)
                        BeginPerch(i);
                    break;
                case Visit.Perch:
                    if (timers[i] >= perchSeconds)
                        BeginLeave(i);
                    break;
                case Visit.Leave:
                    Fly(bird, PerchPoint(i), toPoints[i], timers[i] / leaveSeconds);
                    if (timers[i] >= leaveSeconds)
                        BeginAway(i, 0f);
                    break;
            }
        }
    }

    public bool TryGetNearestBird(Vector3 from, out Vector3 position)
    {
        position = from;
        if (birds == null)
            return false;

        float best = float.MaxValue;
        bool found = false;
        for (int i = 0; i < birds.Length; i++)
        {
            Transform bird = birds[i];
            if (bird == null || !bird.gameObject.activeSelf)
                continue;
            if (i < phases.Length && phases[i] == Visit.Away)
                continue;
            float distance = (bird.position - from).sqrMagnitude;
            if (distance >= best)
                continue;
            best = distance;
            position = bird.position;
            found = true;
        }

        return found;
    }

#if UNITY_EDITOR
    public void EditorConfigure(Transform[] flock, Transform treeRoost)
    {
        birds = flock ?? System.Array.Empty<Transform>();
        roost = treeRoost;
        arriveSeconds = 3.4f;
        perchSeconds = 2.6f;
        leaveSeconds = 3.1f;
        awaySeconds = 5.5f;
        EnsureRuntimeBuffers();
    }
#endif

    private void BeginAway(int index, float elapsed)
    {
        phases[index] = Visit.Away;
        timers[index] = elapsed;
        if (birds[index] != null)
            birds[index].gameObject.SetActive(false);
    }

    private void BeginArrive(int index)
    {
        phases[index] = Visit.Arrive;
        timers[index] = 0f;
        fromPoints[index] = SpawnPoint(index);
        if (birds[index] != null)
        {
            birds[index].gameObject.SetActive(true);
            birds[index].position = fromPoints[index];
        }
    }

    private void BeginPerch(int index)
    {
        phases[index] = Visit.Perch;
        timers[index] = 0f;
        if (birds[index] != null)
            birds[index].position = PerchPoint(index);
    }

    private void BeginLeave(int index)
    {
        phases[index] = Visit.Leave;
        timers[index] = 0f;
        toPoints[index] = SpawnPoint(index + 3);
    }

    private void Fly(Transform bird, Vector3 from, Vector3 to, float raw)
    {
        float t = Mathf.Clamp01(raw);
        float eased = t * t * (3f - 2f * t);
        Vector3 next = Vector3.Lerp(from, to, eased);
        next.y += Mathf.Sin(t * Mathf.PI) * 0.55f;
        Vector3 travel = next - bird.position;
        bird.position = next;
        travel.y = 0f;
        if (travel.sqrMagnitude > 0.0001f)
            bird.rotation = Quaternion.Slerp(
                bird.rotation,
                Quaternion.LookRotation(travel.normalized, Vector3.up),
                Time.deltaTime * 7f);
    }

    private Vector3 PerchPoint(int index)
    {
        Vector3 crown = roost != null ? roost.position : new Vector3(-2.2f, 1.7f, 0.6f);
        float side = index % 2 == 0 ? -0.22f : 0.24f;
        return crown + new Vector3(side, 0.08f + index * 0.05f, 0.06f);
    }

    private static Vector3 SpawnPoint(int index)
    {
        switch (index % 4)
        {
            case 0:
                return new Vector3(7.5f, 4.2f, 11f);
            case 1:
                return new Vector3(-6.8f, 4.6f, 9.5f);
            case 2:
                return new Vector3(5.4f, 5.1f, 14f);
            default:
                return new Vector3(-4.2f, 4.8f, 13.2f);
        }
    }

    private void EnsureRuntimeBuffers()
    {
        int count = birds != null ? birds.Length : 0;
        phases = new Visit[count];
        timers = new float[count];
        fromPoints = new Vector3[count];
        toPoints = new Vector3[count];
    }
}
