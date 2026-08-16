using UnityEngine;

/// <summary>
/// Canonical room-shell box shared by every home room. Living Room Level 01 is
/// the reference: floor extents, wall placement, wall height and baseboard trim
/// must be identical in every room so the shared camera transform frames all
/// rooms the same way and the cat reads at one size everywhere. Room builders
/// must source their shell geometry here instead of hand-written numbers.
/// </summary>
public static class HomeRoomShellMetrics
{
    public const float FloorWidth = 8f;
    public const float FloorDepth = 6f;
    public const float FloorThickness = .2f;
    public const float WallHeight = 3f;
    public const float WallThickness = .2f;

    /// <summary>Inner face of the left and right walls (+-3.8).</summary>
    public const float InteriorMaxX = FloorWidth * .5f - WallThickness;
    public const float InteriorMinX = -InteriorMaxX;

    /// <summary>Inner face of the back wall (2.8). The front side stays open.</summary>
    public const float InteriorMaxZ = FloorDepth * .5f - WallThickness;
    public const float InteriorMinZ = -(FloorDepth * .5f);

    /// <summary>Wainscot band that matches the Living Room panel line.</summary>
    public const float WainscotCenterY = .57f;
    public const float WainscotHeight = 1.14f;
    public const float WainscotThickness = .08f;

    public const float BaseboardHeight = .18f;
    public const float BaseboardDepth = .28f;
    public const float BaseboardCenterY = .09f;

    public static readonly Vector3 FloorPosition =
        new Vector3(0f, -FloorThickness * .5f, 0f);
    public static readonly Vector3 FloorScale =
        new Vector3(FloorWidth, FloorThickness, FloorDepth);

    public static readonly Vector3 BackWallPosition =
        new Vector3(0f, WallHeight * .5f, FloorDepth * .5f - WallThickness * .5f);
    public static readonly Vector3 BackWallScale =
        new Vector3(FloorWidth, WallHeight, WallThickness);

    public static readonly Vector3 LeftWallPosition =
        new Vector3(-(FloorWidth * .5f - WallThickness * .5f), WallHeight * .5f, 0f);
    public static readonly Vector3 RightWallPosition =
        new Vector3(FloorWidth * .5f - WallThickness * .5f, WallHeight * .5f, 0f);
    public static readonly Vector3 SideWallScale =
        new Vector3(WallThickness, WallHeight, FloorDepth);

    public static readonly Vector3 BackBaseboardPosition =
        new Vector3(0f, BaseboardCenterY, BackWallPosition.z);
    public static readonly Vector3 BackBaseboardScale =
        new Vector3(FloorWidth, BaseboardHeight, BaseboardDepth);
    public static readonly Vector3 LeftBaseboardPosition =
        new Vector3(LeftWallPosition.x, BaseboardCenterY, 0f);
    public static readonly Vector3 RightBaseboardPosition =
        new Vector3(RightWallPosition.x, BaseboardCenterY, 0f);
    public static readonly Vector3 SideBaseboardScale =
        new Vector3(BaseboardDepth, BaseboardHeight, FloorDepth);

    /// <summary>Back-wall wainscot panel, flush against the inner wall face.</summary>
    public static readonly Vector3 BackWainscotPosition =
        new Vector3(0f, WainscotCenterY, InteriorMaxZ - WainscotThickness * .5f);
    public static readonly Vector3 BackWainscotScale =
        new Vector3(FloorWidth - .25f, WainscotHeight, WainscotThickness);
    public static readonly Vector3 LeftWainscotPosition =
        new Vector3(InteriorMinX + WainscotThickness * .5f, WainscotCenterY, 0f);
    public static readonly Vector3 RightWainscotPosition =
        new Vector3(InteriorMaxX - WainscotThickness * .5f, WainscotCenterY, 0f);
    public static readonly Vector3 SideWainscotScale =
        new Vector3(WainscotThickness, WainscotHeight, FloorDepth - .25f);

    /// <summary>Shared checker-floor grid that covers the canonical floor.</summary>
    public const int FloorTileColumns = 8;
    public const int FloorTileRows = 6;
    public const float FloorTileStep = 1f;
    public const float FloorTileSize = .98f;
    public const float FloorTileCenterY = .015f;
    public const float FloorTileThickness = .03f;

    public static Vector3 FloorTilePosition(int column, int row)
    {
        float x = -(FloorWidth * .5f) + FloorTileStep * (column + .5f);
        float z = -(FloorDepth * .5f) + FloorTileStep * (row + .5f);
        return new Vector3(x, FloorTileCenterY, z);
    }

    /// <summary>
    /// Decoration plane that sits just in front of the back wall so wall art
    /// never z-fights with the wall itself.
    /// </summary>
    public static float BackWallDecorZ(float offset)
    {
        return InteriorMaxZ - offset;
    }
}
