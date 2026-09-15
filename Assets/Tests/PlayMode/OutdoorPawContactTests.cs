using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class OutdoorPawContactTests
{
    HomeStoreSaveState store;string breed;float capture,scale;
    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive,Is.True);
        store=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;
        capture=Time.captureDeltaTime;scale=Time.timeScale;Time.timeScale=1;Time.captureFramerate=60;
    }
    [TearDown] public void After()
    {
        CatActionState.CancelForTransition(Object.FindAnyObjectByType<CatMovement>());
        RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(store);CatBreedService.Select(breed);
        Time.captureDeltaTime=capture;Time.timeScale=scale;
    }
    static bool Tapping(CatActivity a)=>a is BirdFeederShakeActivity bird?bird.IsTapping:a is PaperSpinActivity record?record.IsRecordTapping:((KnockOffActivity)a).IsTapping;
    static int Strokes(CatActivity a)=>a is BirdFeederShakeActivity bird?bird.ContactStrokes:a is PaperSpinActivity record?record.RecordContactCount:((KnockOffActivity)a).ContactStrokes;
    static float Distance(CatActivity a)=>a is BirdFeederShakeActivity bird?bird.MinimumPawDistance:a is PaperSpinActivity record?record.RecordContactDistance:((KnockOffActivity)a).MinimumPawDistance;
    static void Place(CatMovement cat,CatActivity a)
    {
        var cc=cat.GetComponent<CharacterController>();cc.enabled=false;
        var point=a.RoutineEntryPoint.position;point.y=.05f;
        cat.transform.SetPositionAndRotation(point,Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();
        RoomPlayModeSupport.ProvisionNeeds();
    }
    [UnityTest] public IEnumerator Balcony_PauseAndCancel_RestorePropsAndReleasePaws() =>
        PauseRoom("Balcony_Level01",HomeRoomService.BalconyId,new[]{HomeStoreService.BalconyBirdFeederId,HomeStoreService.BalconySideTableId});
    [UnityTest] public IEnumerator Loft_PauseAndCancel_RestorePropsAndReleasePaws() =>
        PauseRoom("SecondFloor_Level01",HomeRoomService.SecondFloorId,new[]{HomeStoreService.LoftBookStackId,HomeStoreService.LoftRecordPlayerId});
    private IEnumerator PauseRoom(string scene,string room,string[] ids)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(scene);
        var owned=HomeStoreSaveState.CreateDefault();owned.ownedProductIds=HomeStoreService.GetRoomCollection(room).ToArray();
        HomeStoreService.ApplySavedState(owned);yield return null;
        var cat=Object.FindAnyObjectByType<CatMovement>();
        foreach(var a in CatActivity.Registered.Where(a=>ids.Contains(a.StoreProductId)).ToArray())
        {
            var pivot=a is BirdFeederShakeActivity bird?bird.FeederPivot:a is PaperSpinActivity record?record.RollPivot:((KnockOffActivity)a).GlassPivot;
            Vector3 home=pivot.localPosition;Quaternion rotation=pivot.localRotation;
            Place(cat,a);yield return null;Assert.That(a.TryStart(cat),Is.True);
            float deadline=Time.realtimeSinceStartup+15;
            while(a.IsRunning&&!Tapping(a)&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Tapping(a),Is.True);yield return new WaitForEndOfFrame();
            Time.timeScale=0;Vector3 stopped=cat.transform.position,prop=pivot.position;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(Vector3.Distance(stopped,cat.transform.position),Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(prop,pivot.position),Is.LessThan(.0001f));
            a.CancelForTransition();Time.timeScale=1;yield return null;
            Assert.That(Vector3.Distance(home,pivot.localPosition),Is.LessThan(.0001f));
            Assert.That(Quaternion.Angle(rotation,pivot.localRotation),Is.LessThan(.01f));
            Assert.That(float.IsPositiveInfinity(cat.GetComponent<CatToyContactMotion>().Distance),Is.True);
            Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
            Assert.That(cat.GetComponent<CharacterController>().enabled,Is.True);
        }
    }
    [UnityTest,Timeout(180000)] public IEnumerator Balcony_TenBreeds_TouchPropsWithoutSlidingTheirRoot() =>
        ContactRoom("Balcony_Level01",HomeRoomService.BalconyId,new[]{HomeStoreService.BalconyBirdFeederId,HomeStoreService.BalconySideTableId});
    [UnityTest,Timeout(180000)] public IEnumerator Loft_TenBreeds_TouchBookAndRecordWithoutSliding() =>
        ContactRoom("SecondFloor_Level01",HomeRoomService.SecondFloorId,new[]{HomeStoreService.LoftBookStackId,HomeStoreService.LoftRecordPlayerId});
    private IEnumerator ContactRoom(string scene,string room,string[] ids)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(scene);
        var owned=HomeStoreSaveState.CreateDefault();owned.ownedProductIds=HomeStoreService.GetRoomCollection(room).ToArray();
        HomeStoreService.ApplySavedState(owned);yield return null;
        var cat=Object.FindAnyObjectByType<CatMovement>();cat.GetComponent<CatIdleBehavior>().enabled=false;
        var activities=CatActivity.Registered.Where(a=>ids.Contains(a.StoreProductId)).ToArray();
        Assert.That(activities.Length,Is.EqualTo(2));var breeds=CatBreedCatalog.Load();
        for(int index=0;index<breeds.Count;index++)
        {
            CatBreedService.Select(breeds.Get(index).Id);yield return null;yield return null;
            var hips=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine");
            var shoulders=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine.003");
            var head=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine.006");
            foreach(var a in activities)
            {
                Place(cat,a);yield return null;Assert.That(a.TryStart(cat),Is.True,a.StoreProductId);
                float deadline=Time.realtimeSinceStartup+25;Vector3 previous=cat.transform.position;bool wasTapping=false;int frames=0;
                while(a.IsRunning&&Time.realtimeSinceStartup<deadline)
                {
                    yield return new WaitForEndOfFrame();bool tapping=Tapping(a);
                    if(tapping&&wasTapping)Assert.That(Vector3.Distance(cat.transform.position,previous),Is.LessThan(.0001f),a.StoreProductId+" root slides during paw contact");
                    if(tapping&&wasTapping)Assert.That(CatActivityFacing.FacingDot(shoulders.position-hips.position,(shoulders.position+hips.position)*.5f,CatActivityFacing.CameraPosition(cat)),Is.GreaterThanOrEqualTo(-.01f),a.StoreProductId+" torso faces away");
                    if(tapping && a is BirdFeederShakeActivity)
                    {
                        Vector3 toPole=a.transform.position-shoulders.position;toPole.y=0;
                        Assert.That(Vector3.Dot(toPole,cat.transform.forward),Is.GreaterThan(.08f),
                            a.StoreProductId+" pole must remain in front of the shoulder, not inside the torso");
                        Vector3 headToPole=a.transform.position-head.position;headToPole.y=0;
                        Assert.That(headToPole.magnitude,Is.GreaterThan(.14f),
                            a.StoreProductId+" head must remain beside and clear of the pole");
                    }
                    if(tapping)frames++;previous=cat.transform.position;wasTapping=tapping;
                }
                Assert.That(a.IsRunning,Is.False,a.StoreProductId+" timeout");
                Assert.That(Strokes(a),Is.EqualTo(3),a.StoreProductId+" "+breeds.Get(index).Id+" nearest paw="+Distance(a).ToString("F4"));
                Assert.That(Distance(a),Is.LessThan(.025f),a.StoreProductId);
                Assert.That(frames,Is.GreaterThan(20));
                Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
                Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True,a.StoreProductId);
            }
        }
    }
}
