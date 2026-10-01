#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class MainBedCurrentRoomSafetyTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    ReferenceMotionTests fixture;
    [SetUp]public void Before()
    {
        Assert.That(EditorQaSession.IsActive,Is.True,"Copied QA session required.");
        Assert.That(System.IO.Path.GetFullPath(EditorQaSession.SaveDirectory).TrimEnd('/','\\'),
            Is.Not.EqualTo(System.IO.Path.GetFullPath(Application.persistentDataPath).TrimEnd('/','\\')));
        fixture=new ReferenceMotionTests();fixture.Before();
    }
    [TearDown]public void After(){if(fixture!=null)fixture.After();}
    static IEnumerator Prepare(ReferenceMotionTests f)=>(IEnumerator)typeof(ReferenceMotionTests)
        .GetMethod("Prepare",Private).Invoke(f,new object[]{null});
    static void Place(CatMovement cat,CharacterController cc,Vector3 point,Quaternion heading)
    {
        bool enabled=cc.enabled;cc.enabled=false;cat.transform.SetPositionAndRotation(point,heading);
        cc.enabled=enabled;Physics.SyncTransforms();
    }
    static bool BedFirstOnSweep(CatMovement cat,CharacterController cc,CatBedObstacle bed,Vector3 start,Quaternion heading,Vector3 direction)
    {
        Vector3 scale=cat.transform.lossyScale;
        float radius=cc.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
        float height=Mathf.Max(radius*2f,cc.height*Mathf.Abs(scale.y));
        Vector3 centre=start+heading*Vector3.Scale(cc.center,scale);
        Vector3 rise=Vector3.up*(height*.5f-radius);
        // The first actual controller-sized sweep hit must be the bed itself;
        // a bookshelf/chair stop cannot masquerade as successful bed blocking.
        var hits=Physics.CapsuleCastAll(centre-rise,centre+rise,radius,direction,1.35f,~0,QueryTriggerInteraction.Ignore)
            .Where(h=>h.collider!=cc&&!h.transform.IsChildOf(cat.transform)&&h.collider.gameObject.scene==cat.gameObject.scene)
            .OrderBy(h=>h.distance).ToArray();
        return hits.Length>0&&hits[0].transform.IsChildOf(bed.transform);
    }
    static Vector2 CameraInput(CatMovement cat,Vector3 desired)
    {
        var camera=(Transform)typeof(CatMovement).GetField("cameraTransform",Private).GetValue(cat);
        Vector3 right=camera!=null?camera.right:Vector3.right,forward=camera!=null?camera.forward:Vector3.forward;
        right.y=forward.y=0;right.Normalize();forward.Normalize();
        float det=right.x*forward.z-right.z*forward.x;
        Assert.That(Mathf.Abs(det),Is.GreaterThan(.01f));
        Vector2 input=new Vector2((desired.x*forward.z-desired.z*forward.x)/det,
            (right.x*desired.z-right.z*desired.x)/det).normalized;
        Vector3 actual=(right*input.x+forward*input.y).normalized;
        Assert.That(Vector3.Dot(actual,desired.normalized),Is.GreaterThan(.9999f));
        return input;
    }
    static bool RootOutsideBed(Vector3 root,Bounds box)=>
        root.x<box.min.x||root.x>box.max.x||root.z<box.min.z||root.z>box.max.z;

    static IEnumerator WalkAtBed(CatMovement cat,CharacterController cc,CatBedObstacle bed,
        MobileJoystick stick,PropertyInfo inputProperty,Vector3 start,Vector3 direction,int side,bool authoredFloor)
    {
        RoomPlayModeSupport.ProvisionNeeds();inputProperty.SetValue(stick,Vector2.zero);
        Quaternion heading=Quaternion.LookRotation(direction);
        Assert.That(cat.IsInteractionPoseClear(start,heading),Is.True,"Legal start body/controller clearance.");
        if(authoredFloor)Assert.That(CatActivityMotion.IsControllerFloorClear(cat,start),Is.True);
        Assert.That(BedFirstOnSweep(cat,cc,bed,start,heading,direction),Is.True,
            "The bed must be the first physical obstacle, not adjacent furniture.");
        Place(cat,cc,start,heading);
        for(int frame=0;frame<3;frame++)
        {
            yield return null;
            Vector3 delta=cat.transform.position-start;delta.y=0;
            Assert.That(delta.magnitude,Is.LessThan(.002f),"No zero-input depenetration at the legal start.");
            Assert.That(Quaternion.Angle(heading,cat.transform.rotation),Is.LessThan(.2f));
            Assert.That(Mathf.Abs(cat.transform.position.y-start.y),Is.LessThan(.02f));
        }
        Vector3 stable=cat.transform.position;
        inputProperty.SetValue(stick,CameraInput(cat,direction));
        float until=Time.time+.85f;
        while(Time.time<until)
        {
            yield return null;
            Assert.That(cat.transform.position.y,Is.LessThanOrEqualTo(stable.y+.02f),"Manual input must not climb onto the bed.");
            Assert.That(RootOutsideBed(cat.transform.position,bed.Body.bounds),Is.True,"Root must remain outside the bed XZ footprint.");
        }
        inputProperty.SetValue(stick,Vector2.zero);
        if(side!=0)Assert.That((cat.transform.position.x-bed.transform.position.x)*side,Is.GreaterThan(.74f),
            "Unchanged original side exclusion threshold.");
        else Assert.That(cat.transform.position.z,Is.LessThan(bed.Body.bounds.min.z-.18f),
            "Unchanged original front exclusion threshold.");
        Assert.That(Vector3.Dot(cat.transform.position-stable,direction),Is.GreaterThan(.005f),"Actual nonzero manual approach required.");
        Assert.That(cat.IsMovementPhysicallyLocked,Is.False);Assert.That(cc.enabled,Is.True);
    }

    static IEnumerator IsolatedSides(CatMovement cat,CharacterController cc,CatBedObstacle bed,
        MobileJoystick stick,PropertyInfo inputProperty)
    {
        // Same real bed, meshes and colliders. Only its root translation changes
        // temporarily inside the copied QA scene; authored room access is tested separately.
        Vector3 bedPosition=bed.transform.position,bedScale=bed.transform.localScale;
        Quaternion bedRotation=bed.transform.rotation;
        Vector3 actorPosition=cat.transform.position;Quaternion actorRotation=cat.transform.rotation;
        Vector3 boxCentre=bed.Body.center,boxSize=bed.Body.size;
        float floorY=2f;
        foreach(var collider in Object.FindObjectsByType<Collider>())
            if(collider.enabled&&!collider.isTrigger&&collider.gameObject.scene==cat.gameObject.scene&&
                !collider.transform.IsChildOf(cat.transform))floorY=Mathf.Max(floorY,collider.bounds.max.y+2f);
        var floor=new GameObject("QA isolated bed floor",typeof(BoxCollider));floor.hideFlags=HideFlags.DontSave;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(floor,cat.gameObject.scene);
        var floorBox=floor.GetComponent<BoxCollider>();floorBox.size=new Vector3(6f,.2f,4f);
        floor.transform.position=new Vector3(0,floorY-.1f,0);
        try
        {
            bed.transform.position=new Vector3(0,floorY,0);Physics.SyncTransforms();
            foreach(int side in new[]{-1,1})
            {
                Vector3 start=bed.transform.position+new Vector3(side*1.1f,.05f,.15f);
                Assert.That(Physics.Raycast(start+Vector3.up*.05f,Vector3.down,out var floorHit,.2f,~0,QueryTriggerInteraction.Ignore),Is.True);
                Assert.That(floorHit.collider,Is.SameAs(floorBox),"Isolated start must stand over its real physical floor.");
                yield return WalkAtBed(cat,cc,bed,stick,inputProperty,start,Vector3.left*side,side,false);
            }
        }
        finally
        {
            inputProperty.SetValue(stick,Vector2.zero);
            bed.transform.SetPositionAndRotation(bedPosition,bedRotation);
            Place(cat,cc,actorPosition,actorRotation);Object.DestroyImmediate(floor);Physics.SyncTransforms();
            Assert.That(bed.transform.localScale,Is.EqualTo(bedScale));
            Assert.That(bed.Body.center,Is.EqualTo(boxCentre));Assert.That(bed.Body.size,Is.EqualTo(boxSize));
        }
    }

    [UnityTest,Timeout(90000)]
    public IEnumerator CurrentRoomFrontAndOccupiedLeft_WithIsolatedSides_AndSavedSleepWakes()
        => VerifyCurrentRoomAndIsolatedSides(fixture);

    public static IEnumerator VerifyCurrentRoomAndIsolatedSides(ReferenceMotionTests fixture)
    {
        Assert.That(EditorQaSession.IsActive,Is.True,"Copied QA session required.");
        yield return Prepare(fixture);
        var cat=Object.FindAnyObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();
        yield return QaBreedReadiness.WaitForSelected(cat,CatBreedService.SelectedBreedId);
        var idle=cat.GetComponent<CatIdleBehavior>();if(idle!=null)idle.enabled=false;
        var bed=Object.FindAnyObjectByType<CatBedObstacle>();var sleep=cat.GetComponent<SleepInteraction>();
        Assert.That(bed.Body.bounds.max.z,Is.EqualTo(2.80f).Within(.005f));
        Assert.That(bed.transform.Find("PremiumCareVisual").GetComponentInChildren<Renderer>().bounds.max.z,
            Is.EqualTo(2.72f).Within(.005f));
        Vector3 occupiedLeft=new Vector3(bed.transform.position.x-.98f,.05f,2.48f);
        Assert.That(cat.IsInteractionPoseClear(occupiedLeft,Quaternion.LookRotation(Vector3.right)),Is.False,
            "Current left rear start is occupied; it cannot be used as a legal manual approach.");
        var joystickField=typeof(CatMovement).GetField("mobileJoystick",Private);
        object oldJoystick=joystickField.GetValue(cat);
        var host=new GameObject("QA current bed analog",typeof(RectTransform),typeof(MobileJoystick));
        var stick=host.GetComponent<MobileJoystick>();joystickField.SetValue(cat,stick);
        var inputProperty=typeof(MobileJoystick).GetProperty("Direction");
        try
        {
            Vector3 front=bed.FrontExit.position;front.y=.05f;
            yield return WalkAtBed(cat,cc,bed,stick,inputProperty,front,Vector3.forward,0,true);
            yield return IsolatedSides(cat,cc,bed,stick,inputProperty);
            // Saved points are relative to the CURRENT authored bed.
            foreach(var old in new[]{bed.transform.position,bed.transform.position+Vector3.forward*.28f,
                bed.transform.position+new Vector3(-.30f,.05f,.14f)})
            {
                cat.ApplySavedWorldPose(old,Quaternion.identity);
                Assert.That(Vector3.Distance(cat.transform.position,bed.FrontExit.position),Is.LessThan(.02f));
            }
            Vector3 clear=new Vector3(0,0,-2);cat.ApplySavedWorldPose(clear,Quaternion.identity);
            Assert.That(cat.transform.position,Is.EqualTo(clear));
            RoomPlayModeSupport.ProvisionNeeds();Assert.That(sleep.TryRestoreSleepingState(out var reason),Is.True,reason);
            yield return null;Vector3 rest=cat.transform.position;
            CatRoomArrangement.Request(cat.gameObject.scene).Invalidate();yield return null;yield return null;
            Assert.That(sleep.IsSleeping,Is.True);Assert.That(cat.transform.position,Is.EqualTo(rest));
            Assert.That(sleep.TryHandleActionButton(),Is.True);
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(sleep.IsSleeping,Is.False);Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
            Assert.That(cc.enabled,Is.True);Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position,.30f),Is.True);
        }
        finally
        {
            inputProperty.SetValue(stick,Vector2.zero);
            if(sleep.IsSleeping)sleep.CancelForTransition();
            joystickField.SetValue(cat,oldJoystick);Object.Destroy(host);
        }
    }
}
#endif
