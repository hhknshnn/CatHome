using System.Collections.Generic;
using UnityEngine;

/// <summary>Reviewed outdoor furniture bays, with open approaches facing the room.</summary>
public static class OutdoorArrangementProfile
{
    public static readonly Dictionary<string, Vector4> Garden = new Dictionary<string, Vector4>
    {
        {"garden.sapling", new Vector4(-3.29f,0,1.05f,270)},
        {"garden.flower-pots", new Vector4(-2.66f,0,2.40f,0)},
        {"garden.daisy-bed", new Vector4(-1.33f,0,2.39f,0)},
        {"garden.pergola", new Vector4(1.72f,0,2.05f,0)},
        {"garden.hammock", new Vector4(-3.29f,0,-.28f,90)},
        {"garden.sun-lounger", new Vector4(-2.90f,0,-1.50f,180)},
        {"garden.bistro-set", new Vector4(3.02f,0,.33f,90)},
        {"garden.grill", new Vector4(3.27f,0,-1.25f,270)},
        {"garden.bird-bath", new Vector4(-1.10f,0,-1.70f,270)},
        {"garden.yarn-ball", new Vector4(1.65f,0,-1.90f,180)}
    };
    public static readonly Dictionary<string, Vector4> Balcony = new Dictionary<string, Vector4>
    {
        {"balcony.sun-mat", new Vector4(-1.70f,0,.20f,270)},
        {"balcony.planter-box", new Vector4(-2.20f,0,-1.60f,270)},
        {"balcony.herb-shelf", new Vector4(2.01f,0,2.48f,180)},
        {"balcony.railing-flowers", new Vector4(-3.48f,0,1.0f,90)},
        {"balcony.bird-feeder", new Vector4(3.30f,0,-1.55f,90)},
        {"balcony.lantern-string", new Vector4(-3.52f,1.92f,1.70f,90)},
        {"balcony.cushion-bench", new Vector4(3.37f,0,.15f,90)},
        {"balcony.side-table", new Vector4(-3.18f,0,-.85f,180)},
        {"balcony.hanging-chair", new Vector4(-1.90f,0,2.25f,0)},
        {"balcony.sun-awning", new Vector4(0,2.24f,2.20f,180)}
    };
    public static readonly Dictionary<string, Vector4> Patio = new Dictionary<string, Vector4>
    {
        {"patio.stone-rug", new Vector4(0,0,.60f,0)},
        {"patio.potted-ferns", new Vector4(-1.25f,0,1.75f,270)},
        {"patio.herb-trough", new Vector4(-3.29f,0,-.15f,90)},
        {"patio.string-lights", new Vector4(-.80f,0,2.58f,180)},
        {"patio.water-fountain", new Vector4(-2.36f,0,-1.77f,180)},
        {"patio.fire-pit", new Vector4(-2.13f,0,-.65f,270)},
        {"patio.dining-set", new Vector4(2.56f,0,-1.30f,0)},
        {"patio.parasol", new Vector4(-2.80f,0,1.90f,0)},
        {"patio.porch-swing", new Vector4(3.32f,0,.80f,90)},
        {"patio.pergola-arch", new Vector4(1.464f,0,2.29f,180)}
    };
    public static readonly Dictionary<string, Vector4> Loft = new Dictionary<string, Vector4>
    {
        {"loft.floor-runner", new Vector4(0,0,.50f,0)},
        {"loft.floor-cushions", new Vector4(.65f,0,2.27f,270)},
        {"loft.book-stack", new Vector4(2.15f,0,-.50f,0)},
        {"loft.arc-lamp", new Vector4(2.50f,0,2.39f,180)},
        {"loft.bean-bag", new Vector4(-.95f,0,2.245f,0)},
        {"loft.record-player", new Vector4(-3.05f,0,-1.40f,270)},
        {"loft.study-desk", new Vector4(3.12f,0,.85f,90)},
        {"loft.wall-gallery", new Vector4(-3.57f,1.55f,1.60f,90)},
        {"loft.tall-bookcase", new Vector4(-2.38f,0,2.548f,180)},
        {"loft.chaise-lounge", new Vector4(-3.23f,0,.22f,90)}
    };
    public static bool IsReviewedRoom(string id) => id == HomeRoomService.GardenId || id == HomeRoomService.BalconyId || id == HomeRoomService.PatioId || id == HomeRoomService.SecondFloorId;
    public static bool IsReviewedProduct(string id) => id != null &&
        (id.StartsWith("garden.",System.StringComparison.Ordinal) || id.StartsWith("balcony.",System.StringComparison.Ordinal) || id.StartsWith("patio.",System.StringComparison.Ordinal) || id.StartsWith("loft.",System.StringComparison.Ordinal));
    public static bool IsDecoration(string id) => id == HomeStoreService.GardenGrillId || id == HomeStoreService.BalconySunAwningId || id == HomeStoreService.PatioStringLightsId;
    public static float FurnitureGap(string a,string b)
    {
        bool plantA = a==HomeStoreService.GardenSaplingId||a==HomeStoreService.GardenFlowerPotsId||a==HomeStoreService.GardenDaisyBedId;
        bool plantB = b==HomeStoreService.GardenSaplingId||b==HomeStoreService.GardenFlowerPotsId||b==HomeStoreService.GardenDaisyBedId;
        return plantA&&plantB?.20f:BathroomArrangementProfile.FurnitureGap(a,b);
    }
    public static List<HomeRoomLayoutEntry> Plan(string room,IReadOnlyList<HomeRoomLayoutPlanner.Item> items,
        IReadOnlyList<Vector3> views,IReadOnlyList<Bounds> features,IReadOnlyList<Bounds> obstacles) =>
        HomeRoomLayoutPlanner.PlanAuthored(items,room==HomeRoomService.SecondFloorId?Loft:room==HomeRoomService.PatioId?Patio:room==HomeRoomService.BalconyId?Balcony:Garden,views,features,obstacles);
}
