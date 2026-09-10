using UnityEngine;

/// <summary>A bounded composition shift, never a chase camera that hides arena edges.</summary>
[RequireComponent(typeof(Camera))]
public sealed class CatCatchCameraRig:MonoBehaviour
{
    private CatCatchPlayer cat;private Vector3 origin,velocity;
    private void Awake(){origin=transform.localPosition;cat=transform.root.GetComponentInChildren<CatCatchPlayer>();}
    private void LateUpdate()
    {
        bool reduced=CatRunnerProgressService.ReducedMotion;
        Vector3 focus=cat!=null?cat.transform.localPosition:Vector3.zero;
        Vector3 desired=origin+new Vector3(reduced?0:Mathf.Clamp(focus.x*.035f,-.10f,.10f),0,0);
        transform.localPosition=Vector3.SmoothDamp(transform.localPosition,desired,ref velocity,.6f,Mathf.Infinity,Time.deltaTime);
    }
}
