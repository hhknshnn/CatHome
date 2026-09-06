using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScratchPostActivity : CatActivity
{
    [SerializeField] Transform scratchPoint;
    [SerializeField,Min(.5f)] float scratchDuration=2.4f;
    [SerializeField] Transform hangingToy;
    CharacterController controller;
    bool wasEnabled,captured;
    Vector3 start;
    Quaternion rotation,toyRotation;
    List<Vector3> approach;
    public override string ProgressLabel=>IsRunning?"SCRATCHING...":string.Empty;
    protected override bool CanBeginActivity(out string failureReason)
    {
        failureReason="";
        if(scratchPoint==null){failureReason="SCRATCH POST IS NOT READY";return false;}
        if(!CatActivityMotion.TryFloorPath(Cat.transform.position,RoutineEntryPoint.position,out approach))
        {failureReason="LET'S GET A LITTLE CLOSER!";return false;}
        return true;
    }
    protected override bool BeginActivity()
    {
        controller=Cat.GetComponent<CharacterController>();wasEnabled=controller!=null&&controller.enabled;
        start=Cat.transform.position;rotation=Cat.transform.rotation;captured=true;
        if(hangingToy!=null)toyRotation=hangingToy.localRotation;
        Cat.SetMovementLocked(this,true);if(controller!=null)controller.enabled=false;
        StartCoroutine(Routine());return true;
    }
    IEnumerator Routine()
    {
        foreach(var point in approach)yield return Walk(point);
        yield return Walk(scratchPoint.position);
        Vector3 facing=transform.position-Cat.transform.position;facing.y=0;
        if(facing.sqrMagnitude>.001f)Cat.transform.rotation=Quaternion.LookRotation(facing);
        PlayCatPose(CatActivityPose.Scratch);
        float time=0;
        while(time<scratchDuration)
        {
            time+=Time.deltaTime;
            if(hangingToy!=null)hangingToy.localRotation=toyRotation*Quaternion.Euler(0,0,Mathf.Sin(time*10)*9);
            yield return null;
        }
        yield return Walk(RoutineEntryPoint.position);
        Restore(false);CompleteActivity("CLAWS FEEL GREAT!");
    }
    IEnumerator Walk(Vector3 point)
    {
        PlayCatPose(CatActivityPose.Walk);
        Vector3 direction=point-Cat.transform.position;direction.y=0;
        if(direction.sqrMagnitude>.001f)Cat.transform.rotation=Quaternion.LookRotation(direction);
        while(Vector3.Distance(Cat.transform.position,point)>.005f)
        {Cat.transform.position=Vector3.MoveTowards(Cat.transform.position,point,1.5f*Time.deltaTime);yield return null;}
        Cat.transform.position=point;
    }
    void Restore(bool cancel)
    {
        if(!captured)return;captured=false;
        if(hangingToy!=null)hangingToy.localRotation=toyRotation;
        if(Cat!=null){if(cancel){Cat.transform.position=start;Cat.transform.rotation=rotation;}Cat.SetMovementLocked(this,false);}
        if(controller!=null)controller.enabled=wasEnabled;
    }
    protected override void CancelActivity(){StopAllCoroutines();Restore(true);base.CancelActivity();}
    protected override void OnDisable(){StopAllCoroutines();Restore(true);base.OnDisable();}
#if UNITY_EDITOR
    public void EditorConfigureScratch(Transform point,float duration){scratchPoint=point;scratchDuration=Mathf.Max(.5f,duration);}
    public void EditorConfigureToy(Transform toy){hangingToy=toy;}
#endif
}
