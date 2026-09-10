using System;
using UnityEngine;

/// <summary>
/// Distances measured from the planted paws of the unmodified home clips.
/// Distances include the breed visual scale, but not the playable owner's scale.
/// Mini games keep their own animation and cadence policy.
/// </summary>
public sealed class CatHomeLocomotionCatalog : ScriptableObject
{
    public const string ResourceName = "Home/CatHomeLocomotionCatalog";

    [Serializable]
    public sealed class Entry
    {
        public string breedId;
        public AnimationClip walkClip, runClip;
        public float walkCycleDistance, runCycleDistance;

        public bool IsValid => walkClip != null && runClip != null &&
                               walkCycleDistance > .01f && runCycleDistance > .01f;

        public float CycleDistance(float runBlend, float ownerScale) =>
            Mathf.Lerp(walkCycleDistance, runCycleDistance, runBlend) * Mathf.Abs(ownerScale);

        public float CycleSeconds(float runBlend) =>
            Mathf.Lerp(walkClip.length, runClip.length, runBlend);

        public float PlaybackRate(float groundSpeed, float runBlend, float ownerScale) =>
            Mathf.Max(0f, groundSpeed) * CycleSeconds(runBlend) /
            Mathf.Max(.001f, CycleDistance(runBlend, ownerScale));
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();
    public System.Collections.Generic.IReadOnlyList<Entry> Entries => entries;

    public Entry Find(string breedId)
    {
        foreach (var entry in entries)
            if (entry != null && entry.breedId == breedId && entry.IsValid) return entry;
        return null;
    }

    public static float RunBlendForSpeed(float metresPerSecond)
    {
        // Hunger/thirst can turn a full joystick deflection into a gentle walk.
        // Gait is chosen by the distance travelled, never by that deflection.
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.05f, 1.4f, metresPerSecond));
    }

#if UNITY_EDITOR
    public void EditorConfigure(Entry[] measuredEntries) => entries = measuredEntries;
#endif
}
