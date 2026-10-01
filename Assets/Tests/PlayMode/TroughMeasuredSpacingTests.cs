using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Transient Play Mode placement measurement. No scene/prefab is saved.</summary>
public sealed class TroughMeasuredSpacingTests
{
    const string Root="Docs/QA/INTERACTION_POLISH_2026-09-16";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    PreparedInteractionStartTests fixture;
    CatMovement cat; CatActivity activity; Transform product;
    Vector3 originalPosition,openNormal; Quaternion originalRotation; Vector3 originalScale;
    GameObject inputHost; object previousJoystick,previousCamera;
    readonly List<string> scan=new List<string>();
    readonly List<string> failures=new List<string>();
    readonly List<string> cycleRows=new List<string>();
    bool cyclePassed;
    static string F(float v)=>v.ToString("R",CultureInfo.InvariantCulture);
    static string V(Vector3 v)=>F(v.x)+";"+F(v.y)+";"+F(v.z);
    static string Q(string s)=>"\""+s.Replace("\"","\"\"")+"\"";
    object Invoke(string name,params object[] args)=>typeof(PreparedInteractionStartTests).GetMethod(name,Private).Invoke(fixture,args);
    void Check(bool condition,string message){if(!condition&&!failures.Contains(message))failures.Add(message);}

    [SetUp] public void Before()
    {
        fixture=new PreparedInteractionStartTests();fixture.Before();fixture.CaptureSkinEvidence=true;
        scan.Clear();failures.Clear();cycleRows.Clear();cyclePassed=false;
        cycleRows.Add("offset,passed,completed,nativeFrames,maximumSkinDepth,floorClear,walkTravel,walkDepth,skinDetail,errors");
        scan.Add("offset,ready,source63Samples,position,yaw,landing,reason");
    }
    [TearDown] public void After()
    {
        if(activity!=null&&activity.IsRunning)activity.CancelForTransition();
        if(cat!=null&&inputHost!=null)
        {
            typeof(CatMovement).GetField("mobileJoystick",Private).SetValue(cat,previousJoystick);
            typeof(CatMovement).GetField("cameraTransform",Private).SetValue(cat,previousCamera);
        }
        if(inputHost!=null)Object.DestroyImmediate(inputHost);
        if(product!=null){product.SetPositionAndRotation(originalPosition,originalRotation);product.localScale=originalScale;Physics.SyncTransforms();}
        Directory.CreateDirectory(Root);File.WriteAllLines(Root+"/trough-measured-spacing.csv",scan);
        File.WriteAllLines(Root+"/trough-measured-spacing-cycles.csv",cycleRows);
        fixture?.After();
    }

    [UnityTest,Timeout(180000)]
    public IEnumerator SmallestFiveCentimetreSpacing_HasRealCycleSkinClearanceAndOrdinaryExitMovement()
    {
        yield return (IEnumerator)Invoke("Prepare",HomeRoomService.Rooms.First(r=>r.Id==HomeRoomService.PatioId).SceneName);
        cat=(CatMovement)typeof(PreparedInteractionStartTests).GetField("cat",Private).GetValue(fixture);
        activity=CatActivity.Registered.Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId==HomeStoreService.PatioHerbTroughId);
        product=activity.transform;originalPosition=product.position;originalRotation=product.rotation;originalScale=product.localScale;
        activity.TryGetStartPose(cat,out var originalStart);
        openNormal=Vector3.ProjectOnPlane(originalStart.ZoneCentre-originalStart.ActionTarget,Vector3.up).normalized;
        // Remove the small authored along-axis displacement: spacing changes
        // only the measured open side, not the product's position along the wall.
        var surface=activity.GetComponentsInChildren<CatActivitySurface>(true).OrderBy(s=>Vector3.Distance(s.transform.position,originalStart.ActionTarget)).First();
        var axis=Vector3.ProjectOnPlane(surface.Size.x>=surface.Size.y?surface.transform.right:surface.transform.forward,Vector3.up).normalized;
        openNormal-=axis*Vector3.Dot(openNormal,axis);openNormal.Normalize();
        Assert.That(openNormal.sqrMagnitude,Is.GreaterThan(.99f));
        var relatives=product.GetComponentsInChildren<Transform>(true).Where(t=>t!=product).ToArray();
        var relativePositions=relatives.Select(t=>t.localPosition).ToArray();
        var relativeRotations=relatives.Select(t=>t.localRotation).ToArray();
        var relativeScales=relatives.Select(t=>t.localScale).ToArray();
        var candidates=new List<KeyValuePair<float,CatActivityStart>>();
        var tag=cat.GetComponentInChildren<CatBreedVisualTag>();var entry=CatJumpClearanceCatalog.Load().Find(tag.BreedId);
        Assert.That(entry,Is.Not.Null);Assert.That(entry.samples.Length,Is.GreaterThanOrEqualTo(60));
        foreach(float offset in new[]{0f,.05f,.10f,.15f,.20f,.25f,.30f})
        {
            product.position=originalPosition+openNormal*offset;Physics.SyncTransforms();
            object[] args={activity,default(CatActivityStart),string.Empty};
            bool ready=(bool)Invoke("FindReadyPose",args);
            var found=(CatActivityStart)args[1];string reason=(string)args[2];
            scan.Add(string.Join(",",F(offset),ready,entry.samples.Length,Q(V(found.Position)),F(found.Rotation.eulerAngles.y),Q(V(found.ActionTarget)),Q(reason)));
            File.WriteAllLines(Root+"/trough-measured-spacing.csv",scan);
            if(ready)candidates.Add(new KeyValuePair<float,CatActivityStart>(offset,found));
            for(int i=0;i<relatives.Length;i++)
            {
                Check(Vector3.Distance(relativePositions[i],relatives[i].localPosition)<.000001f,"Spacing changed a relative anchor/model position");
                Check(Quaternion.Angle(relativeRotations[i],relatives[i].localRotation)<.001f,"Spacing changed a relative anchor/model rotation");
                Check(Vector3.Distance(relativeScales[i],relatives[i].localScale)<.000001f,"Spacing changed a relative model scale");
            }
            yield return null;
        }
        Assert.That(failures,Is.Empty,"Relative placement invariants failed: "+string.Join("\n",failures));
        Assert.That(candidates,Is.Not.Empty,"No valid source/body/controller/local-approach stance through30cm. Stop; do not invent a ready offset.");
        float smallestPassed=-1;
        foreach(var candidate in candidates)
        {
            yield return FullCycle(candidate.Key,candidate.Value,entry,tag);
            if(cyclePassed){smallestPassed=candidate.Key;break;}
        }
        Assert.That(smallestPassed,Is.GreaterThanOrEqualTo(0),"No full-cycle/25mm-skin/continuity/movement passing spacing through30cm. See per-offset cycles CSV.");
    }

    IEnumerator FullCycle(float smallest,CatActivityStart first,CatJumpClearanceCatalog.Entry entry,CatBreedVisualTag tag)
    {
        failures.Clear();cyclePassed=false;
        product.position=originalPosition+openNormal*smallest;Physics.SyncTransforms();
        Invoke("Place",first.Position,first.Rotation);
        yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
        Assert.That(activity.TryGetStartPose(cat,out var accepted),Is.True,"Minimum candidate must remain ready after the real controller settles");
        Assert.That(activity.TryGetPromptDistance(cat,out _),Is.True,"Minimum candidate must have a real prompt");
        RoomPlayModeSupport.ProvisionNeeds();
        var trace=new JumpContinuityTests.Trace{room=HomeRoomService.PatioId,product=activity.StoreProductId,breed=CatBreedService.SelectedBreedId};
        var bones=(Transform[])Invoke("FullTraceBones");var animator=cat.GetComponentInChildren<Animator>();
        Action<CatActivity> completed=a=>{if(a==activity)trace.completed++;};
        int frame=0,nativeFrames=0,after=0;float maxDepth=0,walkDepth=0,walkTravel=0,startTime=Time.time;string skinDetail="",continuityError="";
        CatActivity.Completed+=completed;
        try
        {
            Assert.That(activity.TryStart(cat),Is.True,"Measured public prompt must start through the real API");
            var animation=cat.GetComponent<CatActivityAnimation>();
            float deadline=Time.realtimeSinceStartup+35;
            while((activity.IsRunning||after<15)&&Time.realtimeSinceStartup<deadline)
            {
                yield return new WaitForEndOfFrame();frame++;if(!activity.IsRunning)after++;
                trace.frames.Add((JumpContinuityTests.Frame)Invoke("SampleFullTrace",activity,animation,animator,bones,Time.time-startTime));
                if(animation.IsNativeJump)
                {
                    nativeFrames++;
                    Vector3 zero=(cat.transform.worldToLocalMatrix*tag.transform.localToWorldMatrix).MultiplyPoint3x4(entry.sourceZeroCentre);
                    Vector3 expected=animation.CurrentPose==CatActivityPose.TowelJumpUp?new Vector3(-zero.x,0,-zero.z):Vector3.zero;
                    Check(cat.transform.TransformVector(animation.NativeJumpEndOffset-expected).magnitude<=.002f,"Source/native end centering differs by more than2mm");
                    if(Time.time-startTime<=.30f)Check(cat.transform.TransformVector(animation.NativeJumpStartOffset).magnitude<=.002f,"Prepared first launch retains a source offset");
                }
                if(Time.time-startTime<=.30f)
                {
                    Check(Vector3.Distance(accepted.Position,cat.transform.position)<.001f,"Initial preparation moves the accepted root");
                    Check(Quaternion.Angle(accepted.Rotation,cat.transform.rotation)<.2f,"Initial preparation rotates the accepted root");
                }
                if(frame%2==0)
                {
                    float depth=(float)Invoke("ActualSkinDepth");
                    if(depth>maxDepth){maxDepth=depth;skinDetail=fixture.DeepestSkinCollider+";vertex="+fixture.DeepestSkinVertex+";point="+V(fixture.DeepestSkinPoint)+";pose="+animation.CurrentPose+";phase="+F(animation.NativeJumpPhase);}
                }
                if(activity.IsWaitingForRestStop&&activity.RestingSeconds>=1.2f)Check(activity.RequestRestStop(),"Rest release refused");
                Check(Vector3.Distance(product.position,originalPosition+openNormal*smallest)<.000001f,"Product placement changed during the actual cycle");
            }
            Check(!activity.IsRunning,"Full cycle exceeded35seconds");Check(trace.completed==1,"Expected exactly one natural completion");
            Check(nativeFrames>10,"Both native jumps must be observed");Check(maxDepth<=.025f,"Actual full-cycle skin exceeds25mm: "+F(maxDepth));
            if(activity.IsRunning)activity.CancelForTransition();
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            trace.controllerFloorClear=(bool)Invoke("ControllerAndBodyClear",cat.transform.position,cat.transform.rotation)&&cat.IsInteractionPoseClear(cat.transform.position,cat.transform.rotation);
            Check(trace.controllerFloorClear,"Completion floor is not body/controller clear");
            Check(cat.GetComponent<CharacterController>().enabled&&!cat.IsMovementPhysicallyLocked,"Completion did not return ordinary movement control");
            try{typeof(JumpContinuityTests).GetMethod("Verify",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{trace});}
            catch(TargetInvocationException e){continuityError=e.InnerException?.Message??e.Message;Check(false,"Native continuity: "+continuityError);}
            // Real movement input after the action; no position/yaw writes.
            previousJoystick=typeof(CatMovement).GetField("mobileJoystick",Private).GetValue(cat);
            previousCamera=typeof(CatMovement).GetField("cameraTransform",Private).GetValue(cat);
            inputHost=new GameObject("QA measured trough exit input",typeof(RectTransform));var joystick=inputHost.AddComponent<MobileJoystick>();
            typeof(CatMovement).GetField("mobileJoystick",Private).SetValue(cat,joystick);typeof(CatMovement).GetField("cameraTransform",Private).SetValue(cat,null);
            Vector3 before=cat.transform.position;typeof(MobileJoystick).GetProperty("Direction").SetValue(joystick,new Vector2(openNormal.x,openNormal.z)*.65f);
            for(int i=0;i<35;i++){yield return new WaitForEndOfFrame();if(i%2==0)walkDepth=Mathf.Max(walkDepth,(float)Invoke("ActualSkinDepth"));}
            typeof(MobileJoystick).GetProperty("Direction").SetValue(joystick,Vector2.zero);
            walkTravel=Vector3.ProjectOnPlane(cat.transform.position-before,Vector3.up).magnitude;
            Check(walkTravel>.15f,"Ordinary input did not leave the measured placement naturally");Check(walkDepth<=.025f,"Ordinary movement skin exceeds25mm");
        }
        finally
        {
            CatActivity.Completed-=completed;if(activity.IsRunning)activity.CancelForTransition();
            cyclePassed=failures.Count==0&&trace.completed==1;
            File.WriteAllText(Root+"/trough-measured-spacing-cycle-"+Mathf.RoundToInt(smallest*100)+"cm.json",JsonUtility.ToJson(trace,true));
            cycleRows.Add(string.Join(",",F(smallest),cyclePassed,trace.completed,nativeFrames,F(maxDepth),trace.controllerFloorClear,F(walkTravel),F(walkDepth),Q(skinDetail),Q(string.Join(";",failures))));
            File.WriteAllLines(Root+"/trough-measured-spacing-cycles.csv",cycleRows);
            if(inputHost!=null)
            {
                typeof(CatMovement).GetField("mobileJoystick",Private).SetValue(cat,previousJoystick);
                typeof(CatMovement).GetField("cameraTransform",Private).SetValue(cat,previousCamera);
                Object.DestroyImmediate(inputHost);inputHost=null;
            }
            File.WriteAllText(Root+"/trough-measured-spacing-result.txt","FullCyclePassed="+cyclePassed+"\nTestedGridOffset="+F(smallest)+"\nOriginalRoot="+V(originalPosition)+"\nCandidateRoot="+V(originalPosition+openNormal*smallest)+"\nCompleted="+trace.completed+"\nNativeFrames="+nativeFrames+"\nMaximumSkinDepth="+F(maxDepth)+"\nSkinDetail="+skinDetail+"\nFloorClear="+trace.controllerFloorClear+"\nOrdinaryWalkTravel="+F(walkTravel)+"\nOrdinaryWalkDepth="+F(walkDepth)+"\nContinuity="+continuityError+"\n"+string.Join("\n",failures));
        }
    }
}
