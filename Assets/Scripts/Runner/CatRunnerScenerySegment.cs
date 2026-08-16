using System;
using UnityEngine;

/// <summary>
/// Selects one authored scenery silhouette per recyclable road segment and adds
/// inexpensive ambient motion to balloons, signs and toy props. Gameplay lanes
/// remain untouched; this component is presentation-only.
/// </summary>
[DisallowMultipleComponent]
public sealed class CatRunnerScenerySegment : MonoBehaviour
{
    [SerializeField] private GameObject[] variants = Array.Empty<GameObject>();
    [SerializeField] private Transform[] floaters = Array.Empty<Transform>();
    [SerializeField] private Transform[] spinners = Array.Empty<Transform>();
    [SerializeField] private float floatAmplitude = 0.12f;
    [SerializeField] private float floatSpeed = 1.25f;
    [SerializeField] private float spinSpeed = 24f;

    private Vector3[] floaterPositions = Array.Empty<Vector3>();
    private Quaternion[] spinnerRotations = Array.Empty<Quaternion>();
    private float phase;
    private bool reducedMotion;

    public int CurrentVariant { get; private set; }

    private void Awake()
    {
        CacheAuthoredPose();
        phase = (EntityId.ToULong(GetEntityId()) % 113UL) * 0.071f;
    }

    private void Update()
    {
        if (reducedMotion)
            return;
        float time = Time.unscaledTime + phase;
        for (int i = 0; i < floaters.Length && i < floaterPositions.Length; i++)
        {
            Transform item = floaters[i];
            if (item == null || !item.gameObject.activeInHierarchy)
                continue;
            Vector3 position = floaterPositions[i];
            position.y += Mathf.Sin(time * floatSpeed + i * 1.7f) * floatAmplitude;
            position.x += Mathf.Sin(time * floatSpeed * 0.63f + i) * floatAmplitude * 0.22f;
            item.localPosition = position;
        }

        for (int i = 0; i < spinners.Length && i < spinnerRotations.Length; i++)
        {
            Transform item = spinners[i];
            if (item == null || !item.gameObject.activeInHierarchy)
                continue;
            item.localRotation = spinnerRotations[i] *
                                 Quaternion.Euler(0f, time * spinSpeed + i * 31f, 0f);
        }
    }

    public void ApplyVariant(int sequence)
    {
        if (variants == null || variants.Length == 0)
            return;
        CurrentVariant = PositiveModulo(sequence, variants.Length);
        for (int i = 0; i < variants.Length; i++)
            if (variants[i] != null)
                variants[i].SetActive(i == CurrentVariant);
    }

    public void SetReducedMotion(bool value)
    {
        reducedMotion = value;
        if (!reducedMotion)
            return;
        for (int i = 0; i < floaters.Length && i < floaterPositions.Length; i++)
            if (floaters[i] != null)
                floaters[i].localPosition = floaterPositions[i];
        for (int i = 0; i < spinners.Length && i < spinnerRotations.Length; i++)
            if (spinners[i] != null)
                spinners[i].localRotation = spinnerRotations[i];
    }

    private void CacheAuthoredPose()
    {
        floaterPositions = new Vector3[floaters != null ? floaters.Length : 0];
        for (int i = 0; i < floaterPositions.Length; i++)
            if (floaters[i] != null)
                floaterPositions[i] = floaters[i].localPosition;

        spinnerRotations = new Quaternion[spinners != null ? spinners.Length : 0];
        for (int i = 0; i < spinnerRotations.Length; i++)
            if (spinners[i] != null)
                spinnerRotations[i] = spinners[i].localRotation;
    }

    private static int PositiveModulo(int value, int modulus)
    {
        if (modulus <= 0)
            return 0;
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        GameObject[] sceneryVariants,
        Transform[] floatingItems,
        Transform[] spinningItems,
        int initialVariant)
    {
        variants = sceneryVariants ?? Array.Empty<GameObject>();
        floaters = floatingItems ?? Array.Empty<Transform>();
        spinners = spinningItems ?? Array.Empty<Transform>();
        floatAmplitude = 0.12f;
        floatSpeed = 1.25f;
        spinSpeed = 24f;
        CacheAuthoredPose();
        ApplyVariant(initialVariant);
    }
#endif
}
