using UnityEngine;

/// <summary>Small leaf movement inside the existing architecture envelope.</summary>
public sealed class RoomFoliageMotion : MonoBehaviour
{
    Quaternion rest;
    void Awake(){rest=transform.localRotation;}
    void Update()
    {
        if(CatRunnerProgressService.ReducedMotion){transform.localRotation=rest;return;}
        float phase=Time.time*.85f+transform.position.x*1.7f+transform.position.z;
        transform.localRotation=rest*Quaternion.Euler(0,Mathf.Sin(phase)*1.2f,Mathf.Sin(phase*.73f)*.65f);
    }
    void OnDisable(){transform.localRotation=rest;}
}
