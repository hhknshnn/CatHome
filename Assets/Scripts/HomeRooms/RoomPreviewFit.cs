using UnityEngine;

/// <summary>
/// Shared cover-crop math for room camera stills. Shop squares and room-selector
/// wells keep the captured 16:9 proportions instead of stretching the photo.
/// </summary>
public static class RoomPreviewFit
{
    public const float PhotoAspect = 16f / 9f;

    public static Rect CoverUv(
        float textureWidth,
        float textureHeight,
        float viewWidth,
        float viewHeight)
    {
        if (textureWidth <= 1f || textureHeight <= 1f || viewWidth <= 1f || viewHeight <= 1f)
            return new Rect(0f, 0f, 1f, 1f);

        float textureAspect = textureWidth / textureHeight;
        float viewAspect = viewWidth / viewHeight;
        if (Mathf.Abs(textureAspect - viewAspect) < 0.01f)
            return new Rect(0f, 0f, 1f, 1f);

        if (textureAspect > viewAspect)
        {
            float width = viewAspect / textureAspect;
            return new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }

        float height = textureAspect / viewAspect;
        return new Rect(0f, (1f - height) * 0.5f, 1f, height);
    }
}
