using UnityEngine;

/// <summary>
/// Tiny bees and butterflies that hover over the garden flower borders.
/// Presentation only.
/// </summary>
[DisallowMultipleComponent]
public sealed class GardenAmbientCritters : MonoBehaviour
{
    [SerializeField] private Transform[] critters = System.Array.Empty<Transform>();
    [SerializeField] private Transform[] flowers = System.Array.Empty<Transform>();
    [SerializeField] private float hoverSpeed = 2.4f;
    [SerializeField] private float hoverRadius = 0.18f;

    public int CritterCount => critters != null ? critters.Length : 0;

    private void Update()
    {
        if (critters == null || flowers == null || flowers.Length == 0)
            return;

        float time = Time.unscaledTime * hoverSpeed;
        for (int i = 0; i < critters.Length; i++)
        {
            Transform critter = critters[i];
            Transform flower = flowers[i % flowers.Length];
            if (critter == null || flower == null)
                continue;

            float phase = i * 1.3f;
            Vector3 hover = new Vector3(
                Mathf.Cos(time + phase) * hoverRadius,
                0.16f + Mathf.Abs(Mathf.Sin(time * 1.7f + phase)) * 0.14f,
                Mathf.Sin(time * 0.9f + phase) * hoverRadius);
            critter.position = flower.position + hover;
            critter.localRotation = Quaternion.Euler(
                0f,
                time * 70f + i * 40f,
                Mathf.Sin(time * 8f + phase) * 18f);
        }
    }

#if UNITY_EDITOR
    public void EditorConfigure(Transform[] flying, Transform[] flowerAnchors)
    {
        critters = flying ?? System.Array.Empty<Transform>();
        flowers = flowerAnchors ?? System.Array.Empty<Transform>();
        hoverSpeed = 2.4f;
        hoverRadius = 0.18f;
    }
#endif
}
