using UnityEngine;

/// <summary>
/// A short, pooled catch celebration: an expanding ring plus a spray of confetti
/// beans that arc outward and shrink away. Scale-only so it needs no transparent
/// materials, and self-deactivates so the controller can round-robin a small pool.
/// </summary>
[DisallowMultipleComponent]
public sealed class CatchBurstFx : MonoBehaviour
{
    private const float Lifetime = 0.42f;

    [SerializeField] private Transform ring;
    [SerializeField] private Transform[] bits = System.Array.Empty<Transform>();

    private Vector3[] directions;
    private Vector3[] bitRestScale;
    private Vector3 ringRestScale = Vector3.one;
    private float elapsed;
    private bool playing;

    private void Awake()
    {
        CacheRest();
        gameObject.SetActive(false);
    }

    private void CacheRest()
    {
        if (ring != null)
            ringRestScale = ring.localScale;
        if (bits != null)
        {
            directions = new Vector3[bits.Length];
            bitRestScale = new Vector3[bits.Length];
            for (int i = 0; i < bits.Length; i++)
                bitRestScale[i] = bits[i] != null ? bits[i].localScale : Vector3.one;
        }
    }

    public void Play(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        elapsed = 0f;
        playing = true;
        if (directions == null || bitRestScale == null)
            CacheRest();
        for (int i = 0; i < (bits?.Length ?? 0); i++)
        {
            float angle = (i / Mathf.Max(1f, bits.Length)) * Mathf.PI * 2f +
                          Random.Range(-0.3f, 0.3f);
            directions[i] = new Vector3(Mathf.Cos(angle), Random.Range(1.3f, 2f), Mathf.Sin(angle));
        }
        gameObject.SetActive(true);
        Apply(0f);
    }

    private void Update()
    {
        if (!playing)
            return;
        elapsed += Time.deltaTime;
        float p = Mathf.Clamp01(elapsed / Lifetime);
        Apply(p);
        if (p >= 1f)
        {
            playing = false;
            gameObject.SetActive(false);
        }
    }

    private void Apply(float p)
    {
        if (ring != null)
        {
            // Grow fast, then pop out: a sine envelope peaks near the middle.
            // Kept small so the flat disc reads as a soft flash, not a big plate.
            float ringScale = Mathf.Sin(p * Mathf.PI) * 0.85f + 0.1f;
            ring.localScale = new Vector3(
                ringRestScale.x * ringScale, ringRestScale.y, ringRestScale.z * ringScale);
        }
        if (bits == null)
            return;
        float reach = 0.95f;
        float ease = 1f - (1f - p) * (1f - p);
        for (int i = 0; i < bits.Length; i++)
        {
            if (bits[i] == null)
                continue;
            Vector3 dir = directions[i];
            Vector3 offset = new Vector3(dir.x, 0f, dir.z) * (reach * ease);
            // Small arc: up then settle.
            offset.y = dir.y * 0.35f * Mathf.Sin(p * Mathf.PI);
            bits[i].localPosition = offset;
            bits[i].localScale = bitRestScale[i] * (1f - p);
        }
    }

    public void EditorBind(Transform ringTransform, Transform[] confetti)
    {
        ring = ringTransform;
        bits = confetti ?? System.Array.Empty<Transform>();
        CacheRest();
    }
}
