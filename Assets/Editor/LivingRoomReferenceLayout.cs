using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Approved front-centred camera and living-room composition.</summary>
public static class LivingRoomReferenceLayout
{
    public static readonly Vector3 CameraPosition = HomeRoomCameraProfile.Position;
    public static readonly Vector3 CameraAngles = HomeRoomCameraProfile.Angles;
    public const float FieldOfView = HomeRoomCameraProfile.FieldOfView;
    public const float SofaScale = .76f;
    public static readonly Vector3 SofaPosition = new Vector3(2.95f,0f,-.35f);
    public static readonly Vector3 TablePosition = new Vector3(1.30f,0f,-.60f);
    public static readonly Vector3 BedPosition = new Vector3(1f,0f,2.40f);
    public static readonly Vector3 BookshelfPosition = new Vector3(-.47f,0f,2.36f);
    // Backplate follows the TV wall; the supported feeding poses face the
    // open camera side without sending the head through a bowl rim.
    public static readonly Vector3 CareStationPosition = new Vector3(-3.445f,0f,2.02f);
    public static readonly Quaternion CareStationRotation = Quaternion.Euler(0,270,0);
    public static Vector3 CarePoint(Vector3 local) => CareStationPosition + CareStationRotation * local;
    public static void ApplyCareLayout(Scene scene)
    {
        if(scene.path!=HomeRoomService.LivingRoomScenePath)return;
        foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            Vector3 local;
            switch(t.name)
            {
                case "FoodBowl": local=new Vector3(-.52f,.018f,-.12f);break;
                case "WaterBowl": local=new Vector3(.26f,.018f,-.12f);break;
                case "FoodInteractionPoint": local=new Vector3(-.4016669f,0f,-.7737662f);break;
                case "WaterInteractionPoint": local=new Vector3(.3783331f,0f,-.7737662f);break;
                default:continue;
            }
            t.SetPositionAndRotation(CarePoint(local),CareStationRotation);
            EditorUtility.SetDirty(t);
        }
    }
    static readonly Quaternion Turn = Quaternion.Euler(0,90,0);
    public static Vector3 SofaPoint(Vector3 original) => SofaPosition + Turn*((original-new Vector3(0,0,2.22f))*SofaScale);
    public static Vector3 TablePoint(Vector3 original) => TablePosition + Turn*(original-new Vector3(0,0,.56f));
    public static void ApplyCamera(Camera camera)
    {
        HomeRoomCameraProfile.Apply(camera);
        EditorUtility.SetDirty(camera); EditorUtility.SetDirty(camera.transform);
    }
    public static void Apply(Scene scene)
    {
        if(scene.path!=HomeRoomService.LivingRoomScenePath)return;
        foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            var camera=t.GetComponent<Camera>();if(camera!=null)ApplyCamera(camera);
            if(t.name==HomeRoomGameplaySafetyBuilder.LivingSofaName)
            {t.SetPositionAndRotation(SofaPosition,Quaternion.Euler(0,270,0));SetWorldScale(t,SofaScale);}
            else if(t.name==HomeRoomGameplaySafetyBuilder.LivingCoffeeTableName)t.SetPositionAndRotation(TablePosition,Turn);
            else if(t.name=="SofaComfortSet")
            {
                t.SetPositionAndRotation(SofaPoint(Vector3.zero),Turn);SetWorldScale(t,SofaScale);
                // Reserve the left seat for every breed's measured resting pose.
                // Loose cushions form a cluster at the opposite arm, outside that seat.
                var mint=t.Find("MintCushion");if(mint!=null)mint.localPosition=new Vector3(.65f,.77f,2.30f);
                var lilac=t.Find("LilacCushion");if(lilac!=null)lilac.localPosition=new Vector3(.52f,.75f,1.99f);
            }
            else if(t.name=="CoffeeTableStyling")t.SetPositionAndRotation(TablePoint(Vector3.zero),Turn);
            else if(t.name=="Carpet_1")
            {t.position=new Vector3(2.10f,0,-.35f);t.rotation=Turn;t.localScale=new Vector3(1.12f,.20f,1.60f);}
            else if(t.name=="RugGoldStitch")
            {t.SetPositionAndRotation(new Vector3(2.10f,0,-.35f),Turn);t.localScale=new Vector3(.56f,1,.80f);}
        }
    }
    static void SetWorldScale(Transform t,float scale)
    {
        var parent=t.parent!=null?t.parent.lossyScale:Vector3.one;
        t.localScale=new Vector3(scale/parent.x,scale/parent.y,scale/parent.z);
    }
}
