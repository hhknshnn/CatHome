using UnityEngine;

// Legacy compatibility component. BowlInteraction is the single interaction manager.
[AddComponentMenu("")]
public sealed class DrinkInteraction : MonoBehaviour
{
    private void Awake()
    {
        Debug.LogWarning(
            "DrinkInteraction is deprecated. Use BowlInteraction for food and water interactions.",
            this
        );
        enabled = false;
    }
}
