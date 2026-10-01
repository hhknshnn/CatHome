using System.Collections;
using UnityEngine;

/// <summary>Measured approach offsets, in the furniture's local frame.</summary>
public static class CatFurnitureJumpClearance
{
    public static float AscentRise(string id,float clearance) => id==HomeStoreService.GardenHammockId||id=="loft.floor-cushions"?Mathf.Max(.30f,clearance):Mathf.Max(.15f,clearance);
    public static float DescentTravelPower(string id) => id == HomeStoreService.BalconyHangingChairId ? 9f : 2.2f;
    public static float DescentRise(string id,float clearance) => id == HomeStoreService.BalconyHangingChairId ? 0f : Mathf.Max(.15f,clearance);
    // These change the launch path, never the placed furniture or its rest point.
    public static Vector3 LaunchOffset(string id)
    {
        switch (id)
        {
            case "bedroom.vanity-stool": return new Vector3(0,0,-.15f);
            case "garden.pergola": return new Vector3(0,0,-.15f);
            case "balcony.herb-shelf": return new Vector3(-.5264f,0,-.1533f);
            case "balcony.hanging-chair": return new Vector3(-.8980f,0,-.3280f);
            case "patio.pergola-arch": return new Vector3(0,0,.30f);
            case "loft.tall-bookcase": return new Vector3(-.4166f,0,.1884f);
            default: return Vector3.zero;
        }
    }

    public static Vector3 DownHeading(string id)
    {
        switch (id)
        {
            case "bedroom.window-daybed": return new Vector3(.7140f,0,.7001f);
            case "balcony.herb-shelf": return Vector3.right;
            case "balcony.hanging-chair": return Vector3.forward;
            case "loft.tall-bookcase": return new Vector3(-.8660f,0,.5000f);
            case "patio.pergola-arch": return Vector3.back;
            case "loft.bean-bag": return new Vector3(.9978f,0,.0670f);
            default: return Vector3.zero;
        }
    }
}
