using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ActivityProximityTests
{
    HomeStoreSaveState saved; string breed;
    const string Output="Docs/QA/PLAYFUL_INTERACTIONS_2026-09-08";
    [SetUp] public void Before(){saved=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;}
    [TearDown] public void After()
    {
        if(CatActivity.Active!=null)CatActivity.Active.enabled=false;
        RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(saved);CatBreedService.Select(breed);
    }
    static void OwnRoom()
    {
        var state=HomeStoreSaveState.CreateDefault();state.ownedProductIds=HomeStoreService.Products.Select(p=>p.Id).ToArray();
        state.storedProductIds=state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();HomeStoreService.ApplySavedState(state);
    }
    static void Move(CatMovement cat,Vector3 p)
    {
        var cc=cat.GetComponent<CharacterController>();cc.enabled=false;p.y=.05f;cat.transform.position=p;cc.enabled=true;Physics.SyncTransforms();
    }
    [UnityTest] public IEnumerator EveryRoomProduct_OffersReachableNearSurfaces_AndRejectsRemoteOrBlockedPoints()
    {
        var failures=new List<string>();var products=new HashSet<string>();var rows=new List<string>{"room,product,activity,near,far,blocked"};int count=0;
        foreach(string room in new[]{"LivingRoom_Level01","Bathroom_Level01","Kitchen_Level01","Bedroom_Level01","Garden_Level01","Balcony_Level01","Patio_Level01","SecondFloor_Level01"})
        {
            yield return RoomPlayModeSupport.LoadRoomAlone(room);OwnRoom();yield return null;yield return null;
            var cat=Object.FindAnyObjectByType<CatMovement>();
            foreach(var a in CatActivity.Registered.Where(a=>a.gameObject.scene==cat.gameObject.scene && HomeStoreService.IsFixedRoomProduct(a.StoreProductId)).ToArray())
            {
                count++;products.Add(a.StoreProductId);var entry=a.RoutineEntryPoint.position;
                var placement=a.GetComponent<HomeProductPlacement>();var visual=placement!=null?placement.MovableRoot:a.transform;
                var renderers=visual.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
                var bounds=new Bounds(entry,Vector3.zero);bool measured=false;
                foreach(var r in renderers){if(!measured){bounds=r.bounds;measured=true;}else bounds.Encapsulate(r.bounds);}
                Move(cat,entry);bool near=a.TryGetPromptDistance(cat,out _);Vector3 chosen=entry;
                for(int i=0;i<16;i++)for(int ring=0;ring<2;ring++)
                {
                    Vector3 direction=Quaternion.Euler(0,i*22.5f,0)*Vector3.forward;
                    Vector3 edge=bounds.ClosestPoint(bounds.center+direction*10);Vector3 point=edge+direction*(.36f+ring*.24f);point.y=0;
                    if(!CatActivityMotion.IsFloorClear(point))continue;Move(cat,point);
                    if(a.TryGetPromptDistance(cat,out _)){near=true;chosen=point;}
                }
                Move(cat,new Vector3(bounds.max.x+2,0,bounds.max.z+2));bool far=a.TryGetPromptDistance(cat,out _);
                // A solid capsule-height obstruction around a chosen close point must hide the action.
                Move(cat,chosen);var wall=new GameObject("QA approach obstruction",typeof(BoxCollider));
                wall.transform.position=chosen+Vector3.up*.3f;wall.GetComponent<BoxCollider>().size=new Vector3(.08f,.6f,.08f);Physics.SyncTransforms();
                bool blocked=a.TryGetPromptDistance(cat,out _);Object.DestroyImmediate(wall);
                rows.Add(room+","+a.StoreProductId+","+a.Kind+","+near+","+far+","+blocked);
                if(!near||far||blocked)failures.Add(a.Kind+" near="+near+" far="+far+" blocked="+blocked+" entry="+entry);
            }
        }
        System.IO.Directory.CreateDirectory(Output);System.IO.File.WriteAllLines(Output+"/room-proximity.csv",rows);
        // Garden has a legacy ball routine as well as its authored yarn routine.
        Assert.That(products.Count,Is.EqualTo(78));Assert.That(count,Is.GreaterThanOrEqualTo(78));
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }
    [UnityTest] public IEnumerator Toys_BothTunnelEnds_AndBedPaintingRemainDistinct()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");OwnRoom();yield return null;
        var cat=Object.FindAnyObjectByType<CatMovement>();var failures=new List<string>();int count=0;
        foreach(string id in HomeStoreService.Products.Where(p=>CatCollectionPolicy.IsCatItem(p.Id)).Select(p=>p.Id))
        {
            Assert.That(HomeStoreService.TrySetStored(id,false),Is.True,id);yield return null;
            var a=CatActivity.Registered.First(x=>x.StoreProductId==id);Move(cat,a.RoutineEntryPoint.position);
            if(!a.TryGetPromptDistance(cat,out _))failures.Add(id+" entrance");count++;
            Move(cat,a.RoutineEntryPoint.position+Vector3.right*2);
            if(a.TryGetPromptDistance(cat,out _))failures.Add(id+" remote");
            if(a is CatEnrichmentActivity tunnel && tunnel.Mode==CatEnrichmentMode.Tunnel)
            {
                Move(cat,tunnel.ExitPoint.position);Assert.That(tunnel.TryGetPromptDistance(cat,out _),Is.True,"opposite mouth");
                Move(cat,tunnel.transform.position+tunnel.transform.right*.6f);Assert.That(tunnel.TryGetPromptDistance(cat,out _),Is.False,"cloth side is not a mouth");
            }
            Assert.That(HomeStoreService.TrySetStored(id,true),Is.True,id);yield return null;
        }
        var painting=CatActivity.Registered.First(a=>a.Kind==CatActivityKind.PaintingWatch);
        var bed=Object.FindObjectsByType<Transform>().First(t=>t.name=="BedInteractionPoint");
        Move(cat,bed.position);Assert.That(painting.TryGetPromptDistance(cat,out _),Is.False,"bed must never offer painting");
        var paintingRoot=painting.GetComponent<HomeProductPlacement>().MovableRoot;
        var paintingBounds=paintingRoot.GetComponentsInChildren<Renderer>().First(r=>r.enabled).bounds;
        bool paintingNear=false;
        for(int side=0;side<16;side++)
        {
            var direction=Quaternion.Euler(0,side*22.5f,0)*Vector3.forward;
            var point=paintingBounds.ClosestPoint(paintingBounds.center+direction*5)+direction*.6f;point.y=0;
            if(!CatActivityMotion.IsFloorClear(point))continue;Move(cat,point);
            if(painting.TryGetPromptDistance(cat,out _))paintingNear=true;
        }
        Assert.That(paintingNear,Is.True,"Painting has a clear nearby viewing side.");
        Assert.That(count,Is.EqualTo(16));Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }
    [UnityTest] public IEnumerator FullFiveToyRoom_BasketContactAndPathsWorkForEveryBreed()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");OwnRoom();yield return null;
        foreach(string id in new[]{HomeStoreService.FeatherToyId,HomeStoreService.BellCollarId,HomeStoreService.PlayTunnelId,HomeStoreService.ScratchPostId,HomeStoreService.BallBasketId})
            Assert.That(HomeStoreService.TrySetStored(id,false),Is.True,id);
        yield return null;var cat=Object.FindAnyObjectByType<CatMovement>();var basket=Object.FindAnyObjectByType<BallChaseActivity>();
        var displayed=Object.FindObjectsByType<HomeProductPlacement>()
            .Where(p=>CatCollectionPolicy.IsCatItem(p.ProductId)&&!HomeStoreService.IsStored(p.ProductId)).ToArray();
        Assert.That(displayed.Length,Is.EqualTo(5));
        Assert.That(displayed.Max(p=>p.MovableRoot.position.x)-displayed.Min(p=>p.MovableRoot.position.x),Is.GreaterThan(3f),"Use the open foreground across the room");
        foreach(var p in displayed)
        {
            foreach(var q in displayed)if(q!=p)
                Assert.That(HomeProductPlacement.FootprintsOverlap(p.MovableRoot.position,p.Footprint,p.MovableRoot.eulerAngles.y,q.MovableRoot.position,q.Footprint,q.MovableRoot.eulerAngles.y,.39f),Is.False,p.ProductId+" crowds "+q.ProductId);
            foreach(var r in p.GetComponentsInChildren<Renderer>())if(r.enabled&&r.gameObject.activeInHierarchy)
            {
                var b=r.bounds;
                foreach(float x in new[]{b.min.x,b.max.x})foreach(float y in new[]{b.min.y,b.max.y})foreach(float z in new[]{b.min.z,b.max.z})
                {
                    var v=Camera.main.WorldToViewportPoint(new Vector3(x,y,z));
                    Assert.That(v.y,Is.GreaterThan(.015f),p.ProductId+" clipped by the bottom of the room");
                    Assert.That(v.x,Is.InRange(.01f,.99f),p.ProductId+" clipped at screen side");
                }
            }
        }
        var host=new GameObject("QA prompt",typeof(ActivityPromptController));var prompt=host.GetComponent<ActivityPromptController>();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var selected=typeof(ActivityPromptController).GetField("selected",flags);
        var candidate=typeof(ActivityPromptController).GetField("candidate",flags);var find=typeof(ActivityPromptController).GetMethod("FindNearestCandidate",flags);
        Move(cat,new Vector3(.50f,.05f,-3.0f));selected.SetValue(prompt,basket);candidate.SetValue(prompt,basket);
        Assert.That(find.Invoke(prompt,null),Is.Not.SameAs(basket),"outside every nearby side");
        Move(cat,basket.RoutineEntryPoint.position);selected.SetValue(prompt,basket);Assert.That(find.Invoke(prompt,null),Is.SameAs(basket));
        candidate.SetValue(prompt,basket);Move(cat,new Vector3(.5f,0,-3.0f));
        typeof(ActivityPromptController).GetMethod("HandleAction",flags).Invoke(prompt,null);Assert.That(basket.IsRunning,Is.False,"stale button click");
        Object.Destroy(host);var breeds=CatBreedCatalog.Load();
        for(int i=0;i<breeds.Count;i++)
        {
            CatBreedService.Select(breeds.Get(i).Id);yield return null;yield return null;RoomPlayModeSupport.ProvisionNeeds();Move(cat,basket.RoutineEntryPoint.position);
            Assert.That(basket.TryStart(cat),Is.True,breeds.Get(i).Id);float deadline=Time.realtimeSinceStartup+18;
            while(basket.IsRunning&&Time.realtimeSinceStartup<deadline){RoomPlayModeSupport.StopObservedRest(basket);yield return null;}
            Assert.That(basket.IsRunning,Is.False);Assert.That(basket.CatchCount,Is.EqualTo(3),breeds.Get(i).Id+" "+basket.InterruptedReason);
            Assert.That(basket.LastHitDistance,Is.LessThan(.095f));Assert.That(basket.RolledDistance,Is.GreaterThan(1.9f));
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position,.24f),Is.True);
        }
    }
}
