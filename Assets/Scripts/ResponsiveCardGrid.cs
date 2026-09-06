using UnityEngine;
using UnityEngine.UI;

/// <summary>Fits catalog columns to their actual viewport without shrinking text independently.</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(GridLayoutGroup))]
public sealed class ResponsiveCardGrid : MonoBehaviour
{
    [SerializeField] private int maximumColumns = 4;
    [SerializeField] private float minimumCardWidth = 270f;
    [SerializeField] private float cardHeight = 480f;
    private GridLayoutGroup grid;
    private float lastWidth = -1f;
    private void OnEnable() => Apply();
    private void OnRectTransformDimensionsChange() => Apply();
    public void Apply()
    {
        if (grid == null) grid = GetComponent<GridLayoutGroup>();
        float width = ((RectTransform)transform).rect.width;
        if (width <= 1f || Mathf.Abs(width - lastWidth) < .25f) return;
        lastWidth = width;
        float usable = width - grid.padding.horizontal;
        int columns = Mathf.Clamp(Mathf.FloorToInt((usable + grid.spacing.x) /
            (minimumCardWidth + grid.spacing.x)), 1, maximumColumns);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.cellSize = new Vector2((usable - grid.spacing.x * (columns - 1)) / columns, cardHeight);
    }
}
