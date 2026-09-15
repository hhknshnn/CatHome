using System.Collections.Generic;
using UnityEngine;

/// <summary>Reviewed perimeter furniture bays; the shared centre remains a walking area.</summary>
public static class KitchenBedroomArrangementProfile
{
    public static readonly Dictionary<string, Vector4> Kitchen = new Dictionary<string, Vector4>
    {
        { "kitchen.paw-mat", new Vector4(-2.10f, 0, .08f, 270) },
        { "kitchen.fruit-basket", new Vector4(-1.72f, KitchenScatterBuilder.CounterTop, 2.05f, 0) },
        { "kitchen.feeding-station", new Vector4(3.14f, 0, -1.35f, 0) },
        { "kitchen.counter-stool", new Vector4(0f, 0, 2.42f, 0) },
        { "kitchen.pantry-shelf", new Vector4(-3.40f, 0, 1.05f, 270) },
        { "kitchen.dish-cart", new Vector4(-3.20f, 0, -1.62f, 270) },
        { "kitchen.sink-cabinet", new Vector4(3.27f, 0, .38f, 90) },
        { "kitchen.refrigerator", new Vector4(2.97f, 0, 2.3905f, 0) },
        { "kitchen.stove-oven", new Vector4(-3.33f, 0, -.35f, 270) },
        { "kitchen.island", new Vector4(-1.72f, 0, 2.2664f, 0) }
    };

    public static readonly Dictionary<string, Vector4> Bedroom = new Dictionary<string, Vector4>
    {
        { "bedroom.paw-rug", new Vector4(-2.10f, 0, -.05f, 270) },
        { "bedroom.night-light", new Vector4(-2.8f, 0, -1.96f, 0) },
        { "bedroom.dream-art", new Vector4(-3.6145f, 1.62f, 0, 90) },
        { "bedroom.yarn-basket", new Vector4(2.35f, 0, -1.65f, 180) },
        { "bedroom.nightstand", new Vector4(-3.4052f, 0, 0, 90) },
        { "bedroom.vanity-stool", new Vector4(-3.30f, 0, -1.07f, 270) },
        { "bedroom.wardrobe", new Vector4(3.40f, 0, 2.0f, 270) },
        { "bedroom.window-daybed", new Vector4(3.2733f, 0, .05f, 270) },
        { "bedroom.star-canopy", new Vector4(-.28f, 0, 2.18f, 0) },
        { "bedroom.queen-bed", new Vector4(-2.30f, 0, 2.0861f, 0) }
    };
    public static bool IsDecoration(string id) => id == HomeStoreService.BedroomNightLightId || id == HomeStoreService.BedroomDreamArtId;
    public static bool IsReviewedRoom(string roomId) => roomId == HomeRoomService.KitchenId || roomId == HomeRoomService.BedroomId;
    public static bool IsReviewedProduct(string id) => id != null && (id.StartsWith("kitchen.", System.StringComparison.Ordinal) || id.StartsWith("bedroom.", System.StringComparison.Ordinal));
    public static float SideLimit(string id) => IsReviewedProduct(id) ? 3.70f : BathroomArrangementProfile.SideLimit(id);
    public static float FrontLimit(string id) => IsReviewedProduct(id) ? -2.20f : HomeRoomLayoutPlanner.FrontLimit;

    public static List<HomeRoomLayoutEntry> Plan(string roomId, IReadOnlyList<HomeRoomLayoutPlanner.Item> items,
        IReadOnlyList<Vector3> views, IReadOnlyList<Bounds> features, IReadOnlyList<Bounds> obstacles) =>
        HomeRoomLayoutPlanner.PlanAuthored(items, roomId == HomeRoomService.KitchenId ? Kitchen : Bedroom, views, features, obstacles);
}
