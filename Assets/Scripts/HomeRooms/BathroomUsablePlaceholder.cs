using UnityEngine;

public enum BathroomUsableKind
{
    Bath = 0,
    Sink = 1,
    Grooming = 2,
    LitterBox = 3
}

/// <summary>
/// Stable, non-economic interaction seam for future Bathroom activities. The
/// authored anchor is usable by animation/gameplay without replacing the prop.
/// </summary>
[DisallowMultipleComponent]
public sealed class BathroomUsablePlaceholder : MonoBehaviour
{
    [SerializeField] private string usableId;
    [SerializeField] private BathroomUsableKind kind;
    [SerializeField] private string prompt = "USE";
    [SerializeField] private Transform interactionAnchor;

    public string UsableId => usableId;
    public BathroomUsableKind Kind => kind;
    public string Prompt => string.IsNullOrWhiteSpace(prompt) ? "USE" : prompt;
    public Transform InteractionAnchor => interactionAnchor;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(usableId) && interactionAnchor != null;

#if UNITY_EDITOR
    public void EditorConfigure(
        string id,
        BathroomUsableKind usableKind,
        string actionPrompt,
        Transform anchor)
    {
        usableId = id;
        kind = usableKind;
        prompt = string.IsNullOrWhiteSpace(actionPrompt) ? "USE" : actionPrompt;
        interactionAnchor = anchor;
    }

    private void OnDrawGizmosSelected()
    {
        if (interactionAnchor == null)
            return;
        Gizmos.color = new Color(0.18f, 0.9f, 0.82f, 0.85f);
        Gizmos.DrawWireSphere(interactionAnchor.position, 0.18f);
        Gizmos.DrawRay(interactionAnchor.position, interactionAnchor.forward * 0.45f);
    }
#endif
}
