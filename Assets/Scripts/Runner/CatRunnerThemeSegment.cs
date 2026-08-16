using UnityEngine;

/// <summary>
/// Palette receiver for one recyclable runner segment. The track manager changes
/// themes only when a segment wraps to the far end, so colour transitions enter
/// the player's view naturally instead of popping beneath the cat.
/// </summary>
[DisallowMultipleComponent]
public sealed class CatRunnerThemeSegment : MonoBehaviour
{
    [SerializeField] private Renderer[] floorRenderers = new Renderer[0];
    [SerializeField] private Renderer[] detailRenderers = new Renderer[0];
    [SerializeField] private Renderer[] laneRenderers = new Renderer[0];
    [SerializeField] private Renderer[] edgeRenderers = new Renderer[0];
    [SerializeField] private Renderer[] accentRenderers = new Renderer[0];
    [SerializeField] private Material[] floorThemes = new Material[0];
    [SerializeField] private Material[] detailThemes = new Material[0];
    [SerializeField] private Material[] laneThemes = new Material[0];
    [SerializeField] private Material[] edgeThemes = new Material[0];
    [SerializeField] private Material[] accentThemes = new Material[0];

    public int CurrentTheme { get; private set; }

    public void ApplyTheme(int themeIndex)
    {
        int count = Mathf.Max(
            Mathf.Max(Mathf.Max(floorThemes.Length, detailThemes.Length), laneThemes.Length),
            Mathf.Max(edgeThemes.Length, accentThemes.Length));
        if (count <= 0)
            return;

        CurrentTheme = PositiveModulo(themeIndex, count);
        Apply(floorRenderers, floorThemes, CurrentTheme);
        Apply(detailRenderers, detailThemes, CurrentTheme);
        Apply(laneRenderers, laneThemes, CurrentTheme);
        Apply(edgeRenderers, edgeThemes, CurrentTheme);
        Apply(accentRenderers, accentThemes, CurrentTheme);
    }

    private static void Apply(Renderer[] renderers, Material[] themes, int index)
    {
        if (renderers == null || themes == null || themes.Length == 0)
            return;
        Material material = themes[PositiveModulo(index, themes.Length)];
        if (material == null)
            return;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null)
                renderers[i].sharedMaterial = material;
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
        Renderer[] floors,
        Renderer[] details,
        Renderer[] lanes,
        Renderer[] edges,
        Renderer[] accents,
        Material[] floorPalette,
        Material[] detailPalette,
        Material[] lanePalette,
        Material[] edgePalette,
        Material[] accentPalette)
    {
        floorRenderers = floors ?? new Renderer[0];
        detailRenderers = details ?? new Renderer[0];
        laneRenderers = lanes ?? new Renderer[0];
        edgeRenderers = edges ?? new Renderer[0];
        accentRenderers = accents ?? new Renderer[0];
        floorThemes = floorPalette ?? new Material[0];
        detailThemes = detailPalette ?? new Material[0];
        laneThemes = lanePalette ?? new Material[0];
        edgeThemes = edgePalette ?? new Material[0];
        accentThemes = accentPalette ?? new Material[0];
        ApplyTheme(0);
    }
#endif
}
