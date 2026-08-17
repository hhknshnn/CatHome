using UnityEngine;

/// <summary>
/// When the cat is idle in the garden, it glances at the nearest flying bird.
/// It never takes a movement lock, so care and activities stay in charge.
/// </summary>
[DisallowMultipleComponent]
public sealed class GardenBirdAttention : MonoBehaviour
{
    [SerializeField] private CatMovement cat;
    [SerializeField] private GardenBirdFlock flock;
    [SerializeField] private float retargetSeconds = 1.6f;

    private float untilRetarget;
    private Vector3 lookPoint;

    private void Awake()
    {
        if (cat == null)
            cat = FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        if (flock == null)
            flock = FindAnyObjectByType<GardenBirdFlock>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (cat == null || flock == null || !cat.IsIdle)
            return;

        untilRetarget -= Time.deltaTime;
        if (untilRetarget <= 0f)
        {
            if (!flock.TryGetNearestBird(cat.transform.position, out lookPoint))
                return;
            untilRetarget = retargetSeconds;
        }

        cat.SuggestLookDirection(lookPoint);
    }

#if UNITY_EDITOR
    public void EditorConfigure(CatMovement roomCat, GardenBirdFlock birds)
    {
        cat = roomCat;
        flock = birds;
        retargetSeconds = 1.6f;
    }
#endif
}
