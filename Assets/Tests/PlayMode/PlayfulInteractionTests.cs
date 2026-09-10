using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PlayfulInteractionTests
{
    HomeStoreSaveState saved; string breed; float speed;
    const string Output="Docs/QA/PLAYFUL_INTERACTIONS_2026-09-08";
    [SetUp] public void Before(){saved=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;speed=Time.timeScale;}
    [TearDown] public void After()
    {
        if(CatActivity.Active!=null)CatActivity.Active.enabled=false;
        Time.timeScale=speed;RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(saved);CatBreedService.Select(breed);
    }
    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        var state=HomeStoreSaveState.CreateDefault();state.ownedProductIds=HomeStoreService.Products.Select(p=>p.Id).ToArray();
        state.storedProductIds=state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();HomeStoreService.ApplySavedState(state);
        yield return null;yield return null;Physics.SyncTransforms();
    }
    static void Move(CatMovement cat,Vector3 point)
    {var cc=cat.GetComponent<CharacterController>();cc.enabled=false;point.y=.05f;cat.transform.position=point;cc.enabled=true;Physics.SyncTransforms();}
    static Bounds BoundsOf(Transform root)
    {var rs=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    [UnityTest] public IEnumerator RetiredActionsAreAbsent_AndTelevisionUsesShortName()
    {
        yield return Prepare();
        Assert.That(Object.FindObjectsByType<CatActivity>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(a=>a.IsRetired),Is.False);
        Assert.That(Object.FindObjectsByType<HomeProductPlacement>(FindObjectsSortMode.None).Any(p=>p.ProductId==HomeStoreService.GameConsoleId),Is.True);
        Assert.That(Object.FindObjectsByType<HomeProductPlacement>(FindObjectsSortMode.None).Any(p=>p.ProductId==HomeStoreService.StereoId),Is.True);
        Assert.That(Object.FindObjectsByType<HomeProductPlacement>(FindObjectsSortMode.None).Any(p=>p.ProductId==HomeStoreService.TvUnitId),Is.True);
        Assert.That(CatActivity.Registered.Any(a=>a.StoreProductId==HomeStoreService.TvUnitId),Is.False,"TV unit is decoration only");
        Assert.That(CatActivity.Registered.Any(a=>a.StoreProductId==HomeStoreService.ModernTelevisionId&&a.Kind==CatActivityKind.TelevisionWatch),Is.True,"The television still has Watch");
        Assert.That(GameProductCopy.Title(HomeStoreService.ModernTelevisionId,"TV"),Is.EqualTo("TV"));
    }
    [UnityTest] public IEnumerator ToysOfferFourSides_NearOnly_AndAnimateFromThatSide()
    {
        yield return Prepare();var cat=Object.FindAnyObjectByType<CatMovement>();var report=new List<string>{"toy,side,contacts,exitClear"};
        foreach(string id in new[]{HomeStoreService.FeatherToyId,HomeStoreService.ToyMouseId,HomeStoreService.BellCollarId})
        {
            Assert.That(HomeStoreService.TrySetStored(id,false),Is.True);yield return null;yield return null;
            var activity=CatActivity.Registered.OfType<CatEnrichmentActivity>().First(a=>a.StoreProductId==id);
            activity.transform.position=new Vector3(0,0,-.3f);Physics.SyncTransforms();
            Bounds bounds=BoundsOf(activity.transform);
            for(int side=0;side<4;side++)
            {
                Vector3 outward=Quaternion.Euler(0,side*90,0)*Vector3.forward;
                Vector3 edge=bounds.ClosestPoint(bounds.center+outward*5);Vector3 at=edge+outward*.36f;at.y=0;
                Move(cat,at+outward*1.2f);Assert.That(activity.TryGetPromptDistance(cat,out _),Is.False,id+" far "+side);
                Move(cat,at);Assert.That(CatActivityMotion.IsFloorClear(at),Is.True,id+" test position");
                Assert.That(activity.TryGetPromptDistance(cat,out _),Is.True,id+" near "+side);
                var wall=new GameObject("QA blocked approach",typeof(BoxCollider));wall.transform.position=at-outward*.18f+Vector3.up*.3f;
                wall.transform.rotation=Quaternion.LookRotation(outward);wall.GetComponent<BoxCollider>().size=new Vector3(.8f,.6f,.035f);Physics.SyncTransforms();
                Assert.That(activity.TryGetPromptDistance(cat,out _),Is.False,id+" wall "+side);Object.DestroyImmediate(wall);Physics.SyncTransforms();
                RoomPlayModeSupport.ProvisionNeeds();Assert.That(activity.TryStart(cat),Is.True,id+" start "+side);
                Assert.That(Vector3.Distance(activity.RoutineFloorPosition,at),Is.LessThan(.12f),"chosen entry");
                float deadline=Time.realtimeSinceStartup+14;
                while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(activity.IsRunning,Is.False,id);Assert.That(activity.ContactCount,Is.GreaterThan(0),id+" real contact "+side+" nearest="+activity.ClosestPawDistance+" start="+at+" target="+activity.ContactPosition+" end="+cat.transform.position);
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position,.24f),Is.True,id+" exit");
                report.Add(id+","+side+","+activity.ContactCount+",true");
            }
            HomeStoreService.TrySetStored(id,true);yield return null;
        }
        System.IO.Directory.CreateDirectory(Output);System.IO.File.WriteAllLines(Output+"/toy-sides.csv",report);
    }
    [UnityTest] public IEnumerator SofaRest_AllBreeds_ClearBothCushions()
    {
        yield return Prepare();var cat=Object.FindAnyObjectByType<CatMovement>();var sofa=CatActivity.Registered.OfType<LivingFurnitureActivity>().First(a=>a.Kind==CatActivityKind.SofaLounge);
        var pillows=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.name=="MintCushion"||r.name=="LilacCushion").ToArray();
        Assert.That(pillows.Length,Is.EqualTo(2));var breeds=CatBreedCatalog.Load();var mesh=new Mesh();var vertices=new List<Vector3>();var rows=new List<string>{"breed,poseSamples,pillowPenetrations"};
        try
        {
            for(int i=0;i<breeds.Count;i++)
            {
                CatBreedService.Select(breeds.Get(i).Id);yield return null;yield return null;RoomPlayModeSupport.ProvisionNeeds();Move(cat,sofa.RoutineEntryPoint.position);
                Assert.That(sofa.TryStart(cat),Is.True);int samples=0,penetrations=0;float deadline=Time.realtimeSinceStartup+14;
                while(sofa.IsRunning&&Time.realtimeSinceStartup<deadline)
                {
                    RoomPlayModeSupport.StopObservedRest(sofa);yield return new WaitForEndOfFrame();if(!sofa.IsResting)continue;
                    var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();skin.BakeMesh(mesh,true);mesh.GetVertices(vertices);samples++;
                    foreach(var pillow in pillows)
                    {
                        var box=pillow.bounds;box.Expand(-.006f);
                        foreach(var v in vertices)if(box.Contains(skin.transform.TransformPoint(v)))penetrations++;
                    }
                }
                Assert.That(sofa.IsRunning,Is.False);Assert.That(samples,Is.GreaterThan(10));
                Assert.That(penetrations,Is.Zero,breeds.Get(i).Id+" intersects a cushion");
                rows.Add(breeds.Get(i).Id+","+samples+","+penetrations);yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            }
        }
        finally{Object.Destroy(mesh);System.IO.Directory.CreateDirectory(Output);System.IO.File.WriteAllLines(Output+"/sofa-cushions.csv",rows);}
    }
    [UnityTest] public IEnumerator ScratchPost_FourSides_HasBothPawContacts()
    {
        yield return Prepare();var cat=Object.FindAnyObjectByType<CatMovement>();
        HomeStoreService.TrySetStored(HomeStoreService.ScratchPostId,false);yield return null;yield return null;
        var scratch=CatActivity.Registered.OfType<ScratchPostActivity>().First(a=>a.StoreProductId==HomeStoreService.ScratchPostId);
        // Four clear sides in the foreground; the central coffee table occupies
        // the right side of (0, 0, -.3), which correctly rejects a prompt there.
        scratch.transform.position=new Vector3(0,0,-1.5f);Physics.SyncTransforms();var b=BoundsOf(scratch.transform);
        for(int side=0;side<4;side++)
        {
            var direction=Quaternion.Euler(0,side*90,0)*Vector3.forward;
            var point=b.ClosestPoint(b.center+direction*5)+direction*.36f;point.y=0;Move(cat,point);
            Assert.That(scratch.TryGetPromptDistance(cat,out var measured),Is.True,"near side "+side+" position="+point+" product="+scratch.transform.position+" distance="+measured+" clear="+CatActivityMotion.IsFloorClear(point));RoomPlayModeSupport.ProvisionNeeds();
            bool started=scratch.TryStart(cat);
            var reason=new object[]{null};
            if(!started)typeof(ScratchPostActivity).GetMethod("CanBeginActivity",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(scratch,reason);
            Assert.That(started,Is.True,"start side "+side+" "+reason[0]+" unlocked="+scratch.IsUnlocked+" at="+point+" root="+scratch.transform.position+" floor="+scratch.RoutineFloorPosition);float deadline=Time.realtimeSinceStartup+10;
            while(scratch.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(scratch.IsRunning,Is.False);Assert.That(scratch.LeftStrokes,Is.GreaterThan(0),"left paw "+side);Assert.That(scratch.RightStrokes,Is.GreaterThan(0),"right paw "+side);
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position,.24f),Is.True);
        }
    }
    [UnityTest] public IEnumerator NearbyToyContacts_WorkAcrossAllTenBreeds()
    {
        yield return Prepare();var cat=Object.FindAnyObjectByType<CatMovement>();var breeds=CatBreedCatalog.Load();
        var rows=new List<string>{"breed,toy,side,contacts"};
        for(int i=0;i<breeds.Count;i++)
        {
            string id=new[]{HomeStoreService.FeatherToyId,HomeStoreService.ToyMouseId,HomeStoreService.BellCollarId}[i%3];
            HomeStoreService.TrySetStored(id,false);CatBreedService.Select(breeds.Get(i).Id);yield return null;yield return null;
            var toy=CatActivity.Registered.OfType<CatEnrichmentActivity>().First(a=>a.StoreProductId==id);toy.transform.position=new Vector3(0,0,-.3f);
            Physics.SyncTransforms();var b=BoundsOf(toy.transform);var direction=Quaternion.Euler(0,(i%4)*90,0)*Vector3.forward;
            var point=b.ClosestPoint(b.center+direction*5)+direction*.36f;point.y=0;Move(cat,point);RoomPlayModeSupport.ProvisionNeeds();
            Assert.That(toy.TryStart(cat),Is.True,breeds.Get(i).Id);float deadline=Time.realtimeSinceStartup+14;
            while(toy.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(toy.IsRunning,Is.False);Assert.That(toy.ContactCount,Is.GreaterThan(0),breeds.Get(i).Id+" "+id+" nearest="+toy.ClosestPawDistance);
            rows.Add(breeds.Get(i).Id+","+id+","+i%4+","+toy.ContactCount);
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);HomeStoreService.TrySetStored(id,true);yield return null;
        }
        System.IO.File.WriteAllLines(Output+"/toy-breed-contacts.csv",rows);
    }
}
