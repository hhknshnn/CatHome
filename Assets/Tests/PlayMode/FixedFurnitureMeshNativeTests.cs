#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class FixedFurnitureMeshNativeTests
{
    const string Root = "Docs/QA/INTERACTION_POLISH_2026-09-16";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    PreparedInteractionStartTests fixture;
    CareAlignmentPolishTests home;
    readonly List<string> rows = new List<string>();
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target,args);
    static T Read<T>(object target,string name) => (T)target.GetType().GetField(name,Private).GetValue(target);
    [TearDown] public void After()
    {
        fixture?.After(); home?.After();
        Directory.CreateDirectory(Root);
        File.WriteAllLines(Root + "/fixed-furniture-" + TestContext.CurrentContext.Test.Name + ".csv",rows);
    }
    static LivingFurnitureActivity[] Pair(CatMovement cat) => CatActivity.Registered.OfType<LivingFurnitureActivity>()
        .Where(a=>a.gameObject.scene==cat.gameObject.scene).OrderBy(a=>a.Kind).ToArray();
    static void AssertMeshPolicy(LivingFurnitureActivity activity)
    {
        Assert.That(activity.SelectionVisual,Is.Not.Null);
        var boxes=activity.SelectionVisual.GetComponentsInChildren<BoxCollider>(true);
        Assert.That(boxes,Is.Not.Empty);Assert.That(boxes.All(b=>b.isTrigger),Is.True,"Reservation volumes must be pick-only.");
        var filters=activity.SelectionVisual.GetComponentsInChildren<MeshFilter>(true);
        Assert.That(filters,Is.Not.Empty);
        foreach(var filter in filters)
        {
            var collider=filter.GetComponent<MeshCollider>();Assert.That(collider,Is.Not.Null);
            Assert.That(collider.sharedMesh,Is.SameAs(filter.sharedMesh));
            Assert.That(collider.enabled&&!collider.isTrigger&&!collider.convex,Is.True);
        }
    }
    [UnityTest,Timeout(150000)] public IEnumerator SofaAndCoffeeTable_RealMeshSolids_CompleteSourceCyclesWithClearSkin()
    {
        fixture=new PreparedInteractionStartTests();fixture.Before();
        yield return (IEnumerator)Call(fixture,"Prepare","LivingRoom_Level01");
        var cat=Read<CatMovement>(fixture,"cat");
        var pair=Pair(cat);Assert.That(pair.Length,Is.EqualTo(2));
        rows.Add("activity,complete,nativeFrames,maxExactSkinDepth,initialShift,initialYaw,floorClear,control,detail");
        foreach(var activity in pair)
        {
            AssertMeshPolicy(activity);
            object[] readyArgs={activity,default(CatActivityStart),string.Empty};
            Assert.That((bool)Call(fixture,"FindReadyPose",readyArgs),Is.True,(string)readyArgs[2]);
            yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
            Assert.That(activity.TryGetStartPose(cat,out var accepted),Is.True);
            Assert.That(activity.TryGetPromptDistance(cat,out _),Is.True);
            RoomPlayModeSupport.ProvisionNeeds();
            var animation=cat.GetComponent<CatActivityAnimation>();var animator=cat.GetComponentInChildren<Animator>();
            var bones=(Transform[])Call(fixture,"FullTraceBones");
            var trace=new JumpContinuityTests.Trace{room="living-room",product=activity.Kind.ToString(),breed=CatBreedService.SelectedBreedId};
            Action<CatActivity> complete=a=>{if(a==activity)trace.completed++;};CatActivity.Completed+=complete;
            int native=0,after=0,frame=0;float depth=0,shift=0,turn=0,started=Time.time;
            string detail="";bool floor=false,control=false,stopped=false;
            try
            {
                Assert.That(activity.TryStart(cat),Is.True);
                animation=cat.GetComponent<CatActivityAnimation>();Assert.That(animation,Is.Not.Null);
                float deadline=Time.realtimeSinceStartup+35;
                while((activity.IsRunning||after<15)&&Time.realtimeSinceStartup<deadline)
                {
                    yield return new WaitForEndOfFrame();frame++;
                    if(!activity.IsRunning)after++;
                    if(animation.IsNativeJump)
                    {
                        native++;
                        Assert.That(animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name.EndsWith("|Jump",StringComparison.Ordinal)),Is.True);
                    }
                    if(Time.time-started<=.30f)
                    {
                        shift=Mathf.Max(shift,Vector3.Distance(cat.transform.position,accepted.Position));
                        turn=Mathf.Max(turn,Quaternion.Angle(cat.transform.rotation,accepted.Rotation));
                    }
                    trace.frames.Add((JumpContinuityTests.Frame)Call(fixture,"SampleFullTrace",activity,animation,animator,bones,Time.time-started));
                    if(frame%2==0)
                    {
                        float sample=(float)Call(fixture,"ActualSkinDepth");
                        if(sample>depth){depth=sample;detail=fixture.DeepestExactSkinDetail;}
                    }
                    if(!stopped&&activity.IsWaitingForRestStop&&activity.RestingSeconds>=1.2f)
                    {stopped=true;Assert.That(activity.RequestRestStop(),Is.True);}
                }
                Assert.That(activity.IsRunning,Is.False);Assert.That(trace.completed,Is.EqualTo(1));
                Assert.That(native,Is.GreaterThan(10));Assert.That(shift,Is.LessThan(.001f));Assert.That(turn,Is.LessThan(.2f));
                Assert.That(depth,Is.LessThanOrEqualTo(.025f),"Actual rendered skin uses exact triangle distance, no soft exemption: "+detail);
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                floor=(bool)Call(fixture,"ControllerAndBodyClear",cat.transform.position,cat.transform.rotation);
                control=cat.GetComponent<CharacterController>().enabled&&!cat.IsMovementPhysicallyLocked;
                Assert.That(floor&&control,Is.True);trace.controllerFloorClear=floor;
                typeof(JumpContinuityTests).GetMethod("Verify",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{trace});
            }
            finally
            {
                CatActivity.Completed-=complete;if(activity.IsRunning)activity.CancelForTransition();
                rows.Add(FormattableString.Invariant($"{activity.Kind},{trace.completed},{native},{depth:R},{shift:R},{turn:R},{floor},{control},\"{(detail??"").Replace("\"","'")}\""));
                File.WriteAllText(Root+"/fixed-furniture-"+activity.Kind+".json",JsonUtility.ToJson(trace,true));
            }
        }
    }

    [UnityTest,Timeout(90000)] public IEnumerator FullHomeCamera_PicksBothFixedProductsThroughTheirPreservedTriggers()
    {
        home=new CareAlignmentPolishTests();home.Before();
        yield return (IEnumerator)Call(home,"Home");
        var cat=Read<CatMovement>(home,"cat");yield return QaBreedReadiness.WaitForSelected(cat);
        var camera=Camera.main;Assert.That(camera,Is.Not.Null);
        var pair=Pair(cat);Assert.That(pair.Length,Is.EqualTo(2));
        yield return null;yield return new WaitForEndOfFrame();
        rows.Add("activity,picked,triggerHit,screenX,screenY");
        foreach(var activity in pair)
        {
            AssertMeshPolicy(activity);
            var box=activity.SelectionVisual.GetComponentsInChildren<BoxCollider>(true).First(b=>b.enabled);
            bool found=false,sawTrigger=false;Vector3 selected=Vector3.zero;
            foreach(float y in new[]{.5f,.75f,.25f})
            foreach(float x in new[]{.5f,.25f,.75f})
            foreach(float z in new[]{.5f,.25f,.75f})
            {
                Vector3 local=box.center+Vector3.Scale(box.size,new Vector3(x-.5f,y-.5f,z-.5f));
                Vector3 screen=camera.WorldToScreenPoint(box.transform.TransformPoint(local));
                if(screen.z<=0||!camera.pixelRect.Contains(screen))continue;
                bool trigger=Physics.RaycastAll(camera.ScreenPointToRay(screen),30f,~0,QueryTriggerInteraction.Collide).Any(h=>h.collider==box);
                if(ActivityPromptController.PickWorldActivity(camera,screen)!=activity||!trigger)continue;
                found=true;sawTrigger=true;selected=screen;break;
            }
            rows.Add(FormattableString.Invariant($"{activity.Kind},{found},{sawTrigger},{selected.x:R},{selected.y:R}"));
            Assert.That(found&&sawTrigger,Is.True,"The actual home camera must still select "+activity.Kind+" through its original pick volume.");
        }
    }
}
#endif
