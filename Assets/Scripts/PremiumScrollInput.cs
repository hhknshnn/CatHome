using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gives the entire clipped list a hit surface. ScrollRect itself keeps ownership
/// of wheel, mouse and touch dragging, including cancelling a card click on drag.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ScrollRect))]
public sealed class PremiumScrollInput : MonoBehaviour
{
    private ScrollRect scroll;
    private bool authoredInertia;
    private bool capturedInertia;

    public static void Ensure(ScrollRect target)
    {
        if (target == null) return;
        var input = target.GetComponent<PremiumScrollInput>();
        if (input == null) input = target.gameObject.AddComponent<PremiumScrollInput>();
        input.Configure();
    }

    private void OnEnable()
    {
        Configure();
        CatRunnerProgressService.PreferencesChanged += ApplyPreferences;
    }

    private void OnDisable()
    {
        CatRunnerProgressService.PreferencesChanged -= ApplyPreferences;
    }

    private void Configure()
    {
        scroll = GetComponent<ScrollRect>();
        if (scroll == null) return;
        var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        var surface = viewport.GetComponent<Graphic>();
        if (surface == null)
        {
            var image = viewport.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            surface = image;
        }

        // Place the target on the viewport, behind its children: card buttons
        // retain their own clicks and gaps/photos route to the parent ScrollRect.
        surface.raycastTarget = true;
        surface.canvasRenderer.cullTransparentMesh = false;
        scroll.scrollSensitivity = Mathf.Max(48f, scroll.scrollSensitivity);
        if (!capturedInertia)
        {
            authoredInertia = scroll.inertia;
            capturedInertia = true;
        }
        ApplyPreferences();
    }

    private void ApplyPreferences()
    {
        if (scroll == null || !Application.isPlaying) return;
        bool reduced = CatRunnerProgressService.ReducedMotion;
        scroll.inertia = authoredInertia && !reduced;
        if (reduced) scroll.StopMovement();
    }
}
