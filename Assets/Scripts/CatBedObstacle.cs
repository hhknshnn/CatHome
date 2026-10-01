using UnityEngine;

/// <summary>Solid, wall-backed care bed; restores old obstructed saves to its front.</summary>
[DisallowMultipleComponent]
public sealed class CatBedObstacle : MonoBehaviour
{
    [SerializeField] private BoxCollider body;
    [SerializeField] private Transform frontExit;

    public BoxCollider Body => body;
    public Transform FrontExit => frontExit;

    public static Vector3 ResolveSavedPosition(CatMovement cat,Vector3 position)
    {
        if(cat==null)return position;
        var controller=cat.GetComponent<CharacterController>();
        float radius=controller!=null?controller.radius*Mathf.Max(Mathf.Abs(cat.transform.lossyScale.x),Mathf.Abs(cat.transform.lossyScale.z))+.06f:.31f;
        Physics.SyncTransforms();
        foreach(var bed in FindObjectsByType<CatBedObstacle>())
        {
            if(bed.gameObject.scene!=cat.gameObject.scene || bed.body==null || !bed.body.enabled || bed.frontExit==null)continue;
            var bounds=bed.body.bounds;bounds.Expand(new Vector3(radius*2,0,radius*2));
            // Only XZ matters: older saves can contain the old cushion height.
            if(position.x<bounds.min.x || position.x>bounds.max.x || position.z<bounds.min.z || position.z>bounds.max.z)continue;
            var exit=bed.frontExit.position;
            if(CatActivityMotion.IsFloorClear(exit,radius))return exit;
            var floor=CatActivityMotion.ReachableFloor(exit);float nearest=float.PositiveInfinity;
            foreach(var point in floor)
                if(CatActivityMotion.IsFloorClear(point,radius) && (point-exit).sqrMagnitude<nearest)
                {nearest=(point-exit).sqrMagnitude;position=point;}
            return position;
        }
        return position;
    }
#if UNITY_EDITOR
    public void EditorConfigure(BoxCollider obstacle,Transform exit){body=obstacle;frontExit=exit;}
#endif
}
