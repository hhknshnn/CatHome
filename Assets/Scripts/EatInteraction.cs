using UnityEngine;

// Legacy compatibility component. BowlInteraction is the single interaction manager.
[AddComponentMenu("")]
public sealed class EatInteraction : MonoBehaviour
{
    private void Awake()
    {
        Debug.LogWarning(
            "EatInteraction is deprecated. Use BowlInteraction for food and water interactions.",
            this
        );
        enabled = false;
    }
}
