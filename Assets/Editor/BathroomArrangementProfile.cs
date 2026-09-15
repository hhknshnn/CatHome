using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Aligned bathroom bays, kept independent of other rooms' approved plans.</summary>
public static class BathroomArrangementProfile
{
    public static readonly Dictionary<string, Vector4> Poses = new Dictionary<string, Vector4>
    {
        { "bathroom.shower", new Vector4(-2.60f, 0, 2.12f, 180) },
        { "bathroom.vanity-sink", new Vector4(-3.05f, 0, .40f, 90) },
        { "bathroom.wall-mirror", new Vector4(-.75f, 1.25f, 2.65f, 0) },
        { "bathroom.grooming-cart", new Vector4(-.75f, 0, 2.20f, 0) },
        { "bathroom.towel-storage", new Vector4(2.45f, 0, 2.375f, 0) },
        { "bathroom.tub", new Vector4(2.85f, 0, .35f, 270) },
        { "bathroom.toilet", new Vector4(3.29118f, 0, -1.30f, 270) },
        { "bathroom.laundry-hamper", new Vector4(-1.525f, 0, 2.27108f, 0) },
        { "bathroom.litter-box", new Vector4(-3.17891f, 0, -1.35f, 270) },
        { "bathroom.bath-mat", new Vector4(0, 0, .25f, 0) }
    };

    public static List<HomeRoomLayoutEntry> Plan(IReadOnlyList<HomeRoomLayoutPlanner.Item> items,
        IReadOnlyList<Vector3> views, IReadOnlyList<Bounds> features, IReadOnlyList<Bounds> obstacles)
    {
        return HomeRoomLayoutPlanner.PlanAuthored(items, Poses, views, features, obstacles);
    }

    // These two backs are measured against the side-wall panels at X +/-3.6615.
    public static float SideLimit(string id) => id == "bathroom.litter-box" || id == "bathroom.toilet" ? 3.72f : 3.65f;

    // Adjacent cabinets form one furniture bay; their accessible fronts stay open.
    public static float FurnitureGap(string a, string b)
    {
        string neighbor = a == "bathroom.laundry-hamper" ? b : b == "bathroom.laundry-hamper" ? a : null;
        if (neighbor == "bathroom.grooming-cart") return .03f;
        if (neighbor == "bathroom.shower") return .01f;
        if ((a == "bathroom.litter-box" && b == "bathroom.vanity-sink") ||
            (b == "bathroom.litter-box" && a == "bathroom.vanity-sink")) return .27f;
        if ((a == "bathroom.toilet" && b == "bathroom.tub") ||
            (b == "bathroom.toilet" && a == "bathroom.tub")) return .30f;
        return HomeRoomLayoutPlanner.ItemGap;
    }
}
