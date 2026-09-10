using UnityEngine;

/// <summary>A single closed tray prevents narrow pockets between the two bowls.</summary>
[DisallowMultipleComponent]
public sealed class CatCareStationObstacle : MonoBehaviour
{
    [SerializeField] private BoxCollider body;
    [SerializeField] private Transform frontExit;
    public BoxCollider Body=>body;
    public Transform FrontExit=>frontExit;

    public static Vector3 ResolveSavedPosition(CatMovement cat,Vector3 position)
    {
        if(cat==null)return position;
        Physics.SyncTransforms();
        foreach(var station in FindObjectsByType<CatCareStationObstacle>(FindObjectsSortMode.None))
        {
            if(station.gameObject.scene!=cat.gameObject.scene||station.body==null||!station.body.enabled||station.frontExit==null)continue;
            var bounds=station.body.bounds;bounds.Expand(new Vector3(.66f,0,.66f));
            if(position.x<bounds.min.x||position.x>bounds.max.x||position.z<bounds.min.z||position.z>bounds.max.z)continue;
            var exit=station.frontExit.position;exit.y=.05f;
            if(CatActivityMotion.IsFloorClear(exit,.30f))return exit;
            foreach(var point in CatActivityMotion.ReachableFloor(exit))
                if(CatActivityMotion.IsFloorClear(point,.30f))return point;
        }
        return position;
    }
#if UNITY_EDITOR
    public void EditorConfigure(BoxCollider collider,Transform exit){body=collider;frontExit=exit;}
#endif
}
