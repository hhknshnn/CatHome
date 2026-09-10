using System.Linq;
using UnityEngine;

/// <summary>Distance-driven fabric feet; the target root and hit position never bob.</summary>
public sealed class CatchMousePresentation:MonoBehaviour
{
    private CatCatchMouse mouse;private Transform[] feet;private Quaternion[] rest;private float stride;
    private LineRenderer selection;private CatCatchPlayer player;
    private void Awake()
    {
        mouse=GetComponentInParent<CatCatchMouse>();
        feet=GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Foot_")).ToArray();
        rest=feet.Select(t=>t.localRotation).ToArray();
        selection=GetComponentInChildren<LineRenderer>();player=transform.root.GetComponentInChildren<CatCatchPlayer>();
    }
    private void LateUpdate()
    {
        float speed=mouse!=null?mouse.Velocity.magnitude:0;
        if(selection!=null)selection.enabled=player!=null&&player.Prey==mouse;
        stride+=speed*Time.deltaTime/.19f;
        for(int i=0;i<feet.Length;i++)
            feet[i].localRotation=rest[i]*Quaternion.Euler(Mathf.Sin(stride*Mathf.PI*2+i*Mathf.PI)*Mathf.Min(22,speed*14),0,0);
    }
}
