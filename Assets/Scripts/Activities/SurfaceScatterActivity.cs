using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Climb onto a real surface, touch a prop, then spill its loose pieces.</summary>
[DisallowMultipleComponent]
public sealed class SurfaceScatterActivity : CatActivity
{
    [SerializeField] Transform perchPoint, propPivot;
    [SerializeField] Transform[] looseParts=Array.Empty<Transform>();
    [SerializeField] Vector3 contactLocal, edgeLocal, landingLocal;
    [SerializeField] bool basket;
    [SerializeField] string requiredProductId;
    CharacterController controller;
    CatSupportedFurnitureMotion support;
    CatToyContactMotion paw;
    Vector3 floor;
    Quaternion facing;
    readonly List<SavedPart> saved=new List<SavedPart>();
    sealed class SavedPart
    {
        public Transform part,parent;public Vector3 position,scale;public Quaternion rotation;
        public SavedPart(Transform value){part=value;parent=value.parent;position=value.localPosition;rotation=value.localRotation;scale=value.localScale;}
        public void Restore(){if(part==null)return;part.SetParent(parent,false);part.localPosition=position;part.localRotation=rotation;part.localScale=scale;}
    }
    public Transform PerchPoint=>perchPoint;
    public Transform PropPivot=>propPivot;
    public IReadOnlyList<Transform> LooseParts=>looseParts;
    public bool IsPerched{get;private set;}
    public bool IsPawing{get;private set;}
    public bool IsScattering{get;private set;}
    public int ContactStrokes{get;private set;}
    public int ScatteredParts{get;private set;}
    public float MinimumPawDistance{get;private set;}
    public float LastPawDistance{get;private set;}
    public Vector3 CurrentContact{get;private set;}
    protected override bool UsesFloorApproach=>true;
    public override string DisplayName=>string.IsNullOrEmpty(StoreProductId)?GameContentCopy.Text("Yemek masası","Dining table"):base.DisplayName;
    public override string ProgressLabel=>IsRunning?GameContentCopy.Text(IsScattering?"Ortalığı dağıtıyor":"Yaramazlık peşinde",IsScattering?"Making a mess":"Up to mischief"):string.Empty;
    protected override bool CanBeginActivity(out string reason)
    {
        bool ready=perchPoint!=null&&propPivot!=null&&looseParts.Length>0&&
            (string.IsNullOrEmpty(requiredProductId)||HomeStoreService.IsOwned(requiredProductId));
        reason=ready?string.Empty:GameContentCopy.Text("Önce mutfak tezgâhı gerekiyor.","The kitchen counter is needed first.");return ready;
    }
    protected override bool BeginActivity()
    {
        controller=Cat.GetComponent<CharacterController>();floor=RoutineFloorPosition;floor.y=Cat.transform.position.y;
        if(!CatActivityMotion.IsControllerFloorClear(Cat,floor))return false;
        saved.Clear();saved.Add(new SavedPart(propPivot));foreach(var part in looseParts)if(part!=null)saved.Add(new SavedPart(part));
        ContactStrokes=0;ScatteredParts=0;MinimumPawDistance=float.PositiveInfinity;StartCoroutine(Routine());return true;
    }
    IEnumerator Routine()
    {
        Cat.SetMovementLocked(this,true);if(controller!=null)controller.enabled=false;
        Vector3 perch=perchPoint.position;
        Vector3 toward=(basket?transform.TransformPoint(contactLocal):looseParts[0].position)-perch;toward.y=0;
        facing=Quaternion.LookRotation(toward.normalized);
        Vector3 travel=perch-floor;travel.y=0;Quaternion launch=Quaternion.LookRotation(travel.normalized);
        yield return CatActivityMotion.WalkAuthoredStep(Cat,floor,launch,.2f);
        support=new CatSupportedFurnitureMotion(this,Cat,perchPoint);
        yield return support.Jump(floor,perch,launch,facing);IsPerched=true;
        paw=Cat.GetComponent<CatToyContactMotion>()??Cat.gameObject.AddComponent<CatToyContactMotion>();
        if(basket)
        {
            for(int i=0;i<3;i++)
            {
                yield return Touch(transform.TransformPoint(contactLocal),i%2==0);
                if(!IsRunning)yield break;
            }
            yield return SpillBasket();
        }
        else
        {
            for(int i=0;i<looseParts.Length;i++)
            {
                var bounds=BoundsOf(looseParts[i]);Vector3 target=bounds.center-facing*Vector3.forward*Mathf.Min(bounds.extents.x,bounds.extents.z)*.9f;
                target.y=bounds.max.y-.012f;
                yield return Touch(target,Vector3.Dot(target-perch,facing*Vector3.right)<0);
                if(!IsRunning)yield break;
                IsScattering=true;
                Vector3 edge=transform.TransformPoint(edgeLocal)+transform.right*(i-1)*.14f;
                yield return Slide(looseParts[i],edge,.32f);
                yield return Toss(new[]{looseParts[i]},transform.TransformPoint(landingLocal)+transform.right*(i-1)*.24f,false);
                IsScattering=false;
            }
        }
        yield return support.Pose(CatActivityPose.SitDown,.48f,perch,facing);
        yield return support.Pose(CatActivityPose.Sit,.55f,perch,facing);
        yield return support.Pose(CatActivityPose.StandUp,.48f,perch,facing);
        Vector3 exitDirection=floor-perch;exitDirection.y=0;
        yield return support.Jump(perch,floor,facing,Quaternion.LookRotation(exitDirection.normalized));
        IsPerched=false;Restore();CompleteActivity(GameContentCopy.Text("Ben yapmadım!","Wasn't me!"));
    }
    IEnumerator Touch(Vector3 target,bool left)
    {
        IsPawing=true;CurrentContact=target;LastPawDistance=float.PositiveInfinity;float elapsed=0;bool touched=false;
        while(elapsed<.72f)
        {
            float phase=elapsed/.72f;Cat.transform.SetPositionAndRotation(perchPoint.position,facing);
            // Hold full contact for .216 seconds so a low frame rate cannot
            // skip the single peak of the reach curve.
            float reachPhase=phase<.32f?.42f*phase/.32f:phase<.62f?.42f:Mathf.Lerp(.42f,1,(phase-.62f)/.38f);
            PlayCatPose(CatActivityPose.GentleKnead,perchPoint);paw.Reach(target,left,reachPhase);
            if(phase>.28f&&phase<.72f){MinimumPawDistance=Mathf.Min(MinimumPawDistance,paw.Distance);LastPawDistance=Mathf.Min(LastPawDistance,paw.Distance);
                if(!touched&&paw.Distance<.028f){touched=true;GameAudio.Play(CatFoley.ContactCue(this),.8f);}}
            yield return null;elapsed+=Time.deltaTime;
        }
        paw.Clear();IsPawing=false;
        if(!touched){CancelForTransition();yield break;}
        ContactStrokes++;yield return support.Pose(CatActivityPose.GentleKnead,.16f,perchPoint.position,facing);
    }
    IEnumerator SpillBasket()
    {
        IsScattering=true;Vector3 edge=transform.TransformPoint(edgeLocal);
        yield return Slide(propPivot,edge,.65f);
        Vector3 land=transform.TransformPoint(landingLocal);
        var parts=new Transform[looseParts.Length+1];parts[0]=propPivot;Array.Copy(looseParts,0,parts,1,looseParts.Length);
        foreach(var part in looseParts)part.SetParent(transform,true);
        yield return Toss(parts,land,true);IsScattering=false;
    }
    IEnumerator Slide(Transform part,Vector3 end,float seconds)
    {
        GameAudio.Play(Kind==CatActivityKind.FruitSwat?AudioCue.Cloth:AudioCue.WoodTap,.45f);
        Vector3 start=part.position;end.y=start.y;float elapsed=0;
        while(elapsed<seconds){part.position=Vector3.Lerp(start,end,Mathf.SmoothStep(0,1,elapsed/seconds));PlayCatPose(CatActivityPose.GentleKnead,perchPoint);yield return null;elapsed+=Time.deltaTime;}
        part.position=end;
    }
    IEnumerator Toss(Transform[] parts,Vector3 landing,bool includesFrame)
    {
        int count=parts.Length;var starts=new Vector3[count];var ends=new Vector3[count];var spins=new Quaternion[count];var rotations=new Quaternion[count];var durations=new float[count];float longest=0;
        Vector3 forward=landing-propPivot.position;forward.y=0;if(forward.sqrMagnitude<.01f)forward=-transform.forward;forward.Normalize();
        Vector3 axis=Vector3.Cross(Vector3.up,forward);
        for(int i=0;i<count;i++)
        {
            var part=parts[i];starts[i]=part.position;rotations[i]=part.rotation;
            bool frame=includesFrame&&i==0;spins[i]=Quaternion.AngleAxis(frame?80:120+i*31,axis)*rotations[i];
            part.rotation=spins[i];float bottom=part.position.y-BoundsOf(part).min.y;part.rotation=rotations[i];
            float angle=i*2.399963f;float radius=frame?0:.17f+.022f*i;
            ends[i]=landing+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;ends[i].y=bottom+.007f;
            float lift=frame?.05f:.7f;durations[i]=(lift+Mathf.Sqrt(lift*lift+19.62f*Mathf.Max(.01f,starts[i].y-ends[i].y)))/9.81f;longest=Mathf.Max(longest,durations[i]);
        }
        float elapsed=0;var sounded=new bool[count];
        while(elapsed<longest+.36f)
        {
            for(int i=0;i<count;i++)
            {
                bool frame=includesFrame&&i==0;float t=Mathf.Clamp01(elapsed/durations[i]);
                parts[i].rotation=Quaternion.Slerp(rotations[i],spins[i],t);
                Vector3 position=Vector3.Lerp(starts[i],ends[i],t);
                if(t<1)position.y=starts[i].y+(frame?.05f:.7f)*elapsed-4.905f*elapsed*elapsed;
                else{float bounce=Mathf.Clamp01((elapsed-durations[i])/.36f);float bottom=parts[i].position.y-BoundsOf(parts[i]).min.y;position.y=bottom+.007f+Mathf.Sin(bounce*Mathf.PI)*(frame?.015f:.06f);position+=forward*(frame?0:.07f)*bounce;}
                float supportHeight=parts[i].position.y-BoundsOf(parts[i]).min.y+.007f;
                position.y=Mathf.Max(position.y,supportHeight);parts[i].position=position;
                if(!sounded[i]&&(t>=1||position.y<=supportHeight+.0001f))
                {
                    sounded[i]=true;
                    GameAudio.Play(includesFrame?(frame?AudioCue.BookLand:AudioCue.FruitLand):
                        parts[i].name.ToLowerInvariant().Contains("salt")?AudioCue.MetalRattle:AudioCue.CeramicLand,includesFrame?.55f:.8f);
                }
            }
            PlayCatPose(CatActivityPose.GentleKnead,perchPoint);yield return null;elapsed+=Time.deltaTime;
        }
        for(int i=0;i<count;i++){parts[i].rotation=spins[i];Vector3 p=parts[i].position;p.y=parts[i].position.y-BoundsOf(parts[i]).min.y+.007f;parts[i].position=p;}
        ScatteredParts+=includesFrame?count-1:count;
    }
    static Bounds BoundsOf(Transform part)
    {
        var renderers=part.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return new Bounds(part.position,Vector3.one*.02f);
        var bounds=renderers[0].bounds;for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);return bounds;
    }
    void Restore()
    {
        if(paw!=null)paw.Clear();if(support!=null){support.End();support=null;}
        foreach(var part in saved)part.Restore();saved.Clear();
        if(Cat!=null){if(IsPerched){Cat.transform.position=floor;}if(controller!=null)controller.enabled=true;Cat.SetMovementLocked(this,false);}
        IsPerched=IsPawing=IsScattering=false;
    }
    protected override void CancelActivity(){if(!IsRunning)return;StopAllCoroutines();Restore();base.CancelActivity();}
#if UNITY_EDITOR
    public void EditorConfigureScatter(Transform perch,Transform prop,Transform[] parts,Vector3 contact,Vector3 edge,Vector3 landing,bool isBasket,string required=null)
    {perchPoint=perch;propPivot=prop;looseParts=parts;contactLocal=contact;edgeLocal=edge;landingLocal=landing;basket=isBasket;requiredProductId=required;}
#endif
}
