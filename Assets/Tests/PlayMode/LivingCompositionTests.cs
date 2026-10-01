#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class LivingCompositionTests
{
    HomeStoreSaveState before;
    CatMovement cat;
    static readonly string[] Cats={HomeStoreService.BallBasketId,HomeStoreService.ScratchPostId,HomeStoreService.PlayTunnelId,HomeStoreService.BellCollarId,HomeStoreService.FeatherToyId};
    [SetUp] public void Before(){Assert.That(EditorQaSession.IsActive,Is.True);before=HomeStoreService.CaptureState();}
    [TearDown] public void After(){if(cat!=null)CatActionState.CancelForTransition(cat);Time.timeScale=1;RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(before);}
    IEnumerator Prepare(bool full=true)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        var state=HomeStoreSaveState.CreateDefault();
        state.ownedProductIds=full?HomeStoreService.Products.Where(p=>HomeStoreService.IsLivingRoomCollectionProduct(p.Id)).Select(p=>p.Id).Concat(Cats).ToArray():new string[0];
        HomeStoreService.ApplySavedState(state);yield return null;yield return null;
        foreach(var c in Object.FindObjectsByType<HomeLevelUpCelebrationView>(FindObjectsInactive.Include))Object.DestroyImmediate(c.gameObject);
        cat=Object.FindAnyObjectByType<CatMovement>();var idle=cat.GetComponent<CatIdleBehavior>();if(idle!=null)idle.enabled=false;
        yield return QaBreedReadiness.WaitForSelected(cat,CatBreedService.SelectedBreedId);
        Physics.SyncTransforms();
    }
    static GameObject Visual(string id)
    {
        var d=Object.FindObjectsByType<StoreProductDisplay>(FindObjectsInactive.Include).Single(x=>x.ProductId==id);
        return new UnityEditor.SerializedObject(d).FindProperty("visualRoot").objectReferenceValue as GameObject;
    }
    [UnityTest] public IEnumerator ShelfThenBooks_RemainSeparatePurchases_AndReloadRetainsTenBooks()
    {
        yield return Prepare(false);
        Assert.That(GameObject.Find("LivingPermanentDecor"),Is.Not.Null);
        foreach(var p in HomeStoreService.Products.Where(p=>HomeStoreService.IsLivingRoomCollectionProduct(p.Id)))Assert.That(Visual(p.Id).activeInHierarchy,Is.False,p.Id+" starts locked");
        Assert.That(HomeStoreService.TryAcquireForTesting(HomeStoreService.BookshelfId).Succeeded,Is.True);yield return null;
        Assert.That(Visual(HomeStoreService.BookshelfId).activeInHierarchy,Is.True);
        Assert.That(Visual(HomeStoreService.BookSetId).activeInHierarchy,Is.False);
        Assert.That(HomeStoreService.TryAcquireForTesting(HomeStoreService.BookSetId).Succeeded,Is.True);yield return new WaitForSeconds(1.5f);
        var saved=HomeStoreService.CaptureState();
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");HomeStoreService.ApplySavedState(saved);yield return null;yield return null;
        var set=Object.FindAnyObjectByType<HomeBookshelfBookSet>();Assert.That(set.IsInstalled,Is.True);Assert.That(set.BookCount,Is.EqualTo(10));Assert.That(set.ShelfRowCount,Is.EqualTo(3));
        Assert.That(Visual(HomeStoreService.BookSetId).activeInHierarchy,Is.True);
        var data=new UnityEditor.SerializedObject(set);var books=data.FindProperty("books");var positions=data.FindProperty("targetLocalPositions");
        for(int i=0;i<10;i++){var b=books.GetArrayElementAtIndex(i).objectReferenceValue as Transform;Assert.That(Vector3.Distance(b.localPosition,positions.GetArrayElementAtIndex(i).vector3Value),Is.LessThan(.001f));}
    }
    [UnityTest] public IEnumerator FullRoom_AllCareAndProductEntriesRemainReachable()
    {
        yield return Prepare();Assert.That(CatCollectionPolicy.DisplayedCount,Is.EqualTo(5));
        var activities=Object.FindObjectsByType<CatActivity>(FindObjectsInactive.Include).Where(a=>HomeStoreService.IsLivingRoomCollectionProduct(a.StoreProductId)).ToArray();
        foreach(var a in activities){var p=a.RoutineEntryPoint.position;p.y=0;Assert.That(CatActivityMotion.IsFloorClear(p,.24f,true),Is.True,a.StoreProductId+" entry clear");Assert.That(CatActivityMotion.TryFloorPath(Vector3.zero,p,out _),Is.True,a.StoreProductId+" reachable");}
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t=>new[]{"FoodInteractionPoint","WaterInteractionPoint","SofaJumpEntry","TableJumpEntry"}.Contains(t.name)))
            Assert.That(CatActivityMotion.TryFloorPath(Vector3.zero,t.position,out _),Is.True,t.name);
        foreach(var p in Object.FindObjectsByType<HomeProductPlacement>(FindObjectsInactive.Include).Where(p=>Cats.Contains(p.ProductId)))Assert.That(p.IsCurrentPositionValid(),Is.True,p.ProductId);
    }
    [UnityTest] public IEnumerator FullRoom_ChangedObservationMeshesCompleteAndReleaseControl()
        => Observe(false,false,false);
    [UnityTest] public IEnumerator FullRoom_PermanentSofaAndTableCompleteAndReleaseControl()
        => Observe(false,false,true);
    [UnityTest] public IEnumerator CurrentArmchair_DiagnosticControl()
        => Observe(false,true,false);
    [UnityTest] public IEnumerator BaselineArmchairPose_DiagnosticControl()
        => Observe(true,true,false);
    [UnityTest] public IEnumerator ReferenceCollection_KeepsCentralFloorOpenAfterReload()
    {
        yield return Prepare();
        var ids=new[]{HomeStoreService.BallBasketId,HomeStoreService.FeatherToyId,HomeStoreService.ToyMouseId,HomeStoreService.BellCollarId};
        var state=HomeStoreSaveState.CreateDefault();
        state.ownedProductIds=HomeStoreService.Products.Where(p=>HomeStoreService.IsLivingRoomCollectionProduct(p.Id)).Select(p=>p.Id).Concat(ids).ToArray();
        HomeStoreService.ApplySavedState(state);yield return null;yield return null;
        // A formerly saved central display must be replanned, without losing
        // ownership or relaxing the five-item / one-bed rule.
        HomeStoreService.TrySetPlacement(HomeStoreService.BallBasketId,new Vector3(0,0,-.3f),0);
        var saved=HomeStoreService.CaptureState();
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");HomeStoreService.ApplySavedState(saved);
        yield return null;yield return null;
        Assert.That(CatCollectionPolicy.DisplayedCount,Is.EqualTo(4));
        foreach(var product in Object.FindObjectsByType<HomeProductPlacement>(FindObjectsInactive.Include).Where(p=>ids.Contains(p.ProductId))){
            Assert.That(HomeStoreService.IsOwned(product.ProductId),Is.True);
            Assert.That(product.IsCurrentPositionValid(),Is.True,product.ProductId);
            Assert.That(HomeProductPlacement.FootprintsOverlap(product.MovableRoot.position,product.Footprint,product.MovableRoot.eulerAngles.y,new Vector3(0,0,-.05f),new Vector2(1.5f,1.75f),0,0),Is.False,product.ProductId+" blocks the central floor");
        }
        Assert.That(CatActivityMotion.TryFloorPath(new Vector3(0,0,-1),new Vector3(0,0,.8f),out _),Is.True);
    }
    [UnityTest] public IEnumerator DecorativeWindow_InteriorLightingSurvivesDawnDayAndNight()
    {
        yield return Prepare();
        var clock=Object.FindAnyObjectByType<GameTimeService>();Assert.That(clock,Is.Not.Null);
        var data=new UnityEditor.SerializedObject(clock);data.FindProperty("useTestTime").boolValue=true;data.ApplyModifiedPropertiesWithoutUndo();
        var window=Object.FindAnyObjectByType<WindowDayNightController>();
        foreach(float hour in new[]{6f,13f,23f}){
            data.Update();data.FindProperty("testHour").floatValue=hour;data.ApplyModifiedPropertiesWithoutUndo();
            window.RefreshVisuals();yield return new WaitForSeconds(2);
            var lights=Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            var key=lights.First(l=>l.name=="Directional Light");var ceiling=lights.First(l=>l.name=="CeilingLight");var source=lights.First(l=>l.name=="WindowLight");
            Assert.That(source.enabled,Is.False);Assert.That(source.intensity,Is.Zero);
            Assert.That(key.intensity,Is.GreaterThan(.5f));Assert.That(ceiling.intensity,Is.GreaterThan(1f));
            Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).First(t=>t.name=="SunBeam").gameObject.activeSelf,Is.False);
        }
    }
    [UnityTest] public IEnumerator NewCartoon_PreparesAdvancesLoopsAndFitsScreen()
    {
        yield return Prepare();
        var screen=Object.FindAnyObjectByType<CatTelevisionScreen>();Assert.That(screen,Is.Not.Null);
        var player=screen.GetComponent<UnityEngine.Video.VideoPlayer>();
        float deadline=Time.realtimeSinceStartup+25;
        while(!player.isPlaying&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(player.isPlaying,Is.True,"Cartoon must prepare and play");
        Assert.That(player.clip.width,Is.EqualTo(1280));Assert.That(player.clip.height,Is.EqualTo(720));
        Assert.That(player.length,Is.EqualTo(10).Within(.05));
        Assert.That(player.audioOutputMode,Is.EqualTo(UnityEngine.Video.VideoAudioOutputMode.None));
        Assert.That(screen.transform.localScale.x/screen.transform.localScale.y,Is.EqualTo(16f/9f).Within(.001));
        long first=player.frame;deadline=Time.realtimeSinceStartup+10;
        while(player.frame==first&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(player.frame,Is.Not.EqualTo(first),"frame="+player.frame+" time="+player.time+" scale="+Time.timeScale+" audioPause="+AudioListener.pause);
        bool looped=false;UnityEngine.Video.VideoPlayer.EventHandler onLoop=p=>looped=true;
        player.loopPointReached+=onLoop;
        deadline=Time.realtimeSinceStartup+12;
        while(!looped&&Time.realtimeSinceStartup<deadline)yield return null;
        player.loopPointReached-=onLoop;Assert.That(looped,Is.True,"Cartoon loops");
        Assert.That(player.isPlaying,Is.True);
        Assert.That(player.targetTexture.IsCreated(),Is.True);
    }
    IEnumerator Observe(bool originalChair,bool chairOnly,bool permanent)
    {
        yield return Prepare();var report=new List<string>();
        foreach(var a in Object.FindObjectsByType<CatActivity>(FindObjectsInactive.Include).Where(a=>permanent?a is LivingFurnitureActivity:HomeStoreService.IsLivingRoomCollectionProduct(a.StoreProductId)&&(chairOnly?a is PerchNapActivity:a is SitLookActivity)).OrderBy(a=>a.StoreProductId))
        {
            if(originalChair){if(!(a is PerchNapActivity))continue;a.transform.SetPositionAndRotation(new Vector3(2.18f,0,1.95f),Quaternion.Euler(0,50,0));Physics.SyncTransforms();}
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);RoomPlayModeSupport.ProvisionNeeds();
            a.TryGetStartPose(cat,out var initial);
            var p=a is SitLookActivity?a.RoutineEntryPoint.position:initial.ZoneCentre;p.y=.05f;
            var look=a is SitLookActivity?((SitLookActivity)a).LookPoint.position:initial.ActionTarget;var direction=look-p;direction.y=0;
            var cc=cat.GetComponent<CharacterController>();cc.enabled=false;cat.transform.SetPositionAndRotation(p,Quaternion.LookRotation(direction));cc.enabled=true;Physics.SyncTransforms();yield return null;
            // Exercise the existing legal stance region; the runtime never moves
            // the player to make an action available.
            bool ready=a.TryGetStartPose(cat,out _);
            foreach(float radius in new[]{0f,.06f,.12f,.18f,.215f})
            {
                if(ready)break;
                for(int angle=0;angle<12&&!ready;angle++)
                {
                    var candidate=p+Quaternion.Euler(0,angle*30,0)*Vector3.forward*radius;
                    var d=look-candidate;d.y=0;var direct=Quaternion.LookRotation(d);
                    foreach(float yaw in new[]{0f,-15f,15f,-30f,30f}){
                        var rotation=Quaternion.AngleAxis(yaw,Vector3.up)*direct;
                        if(!CatActivityMotion.TryFloorPath(Vector3.zero,candidate,out _)||!cat.IsInteractionPoseClear(candidate,rotation))continue;
                        cc.enabled=false;cat.transform.SetPositionAndRotation(candidate,rotation);cc.enabled=true;Physics.SyncTransforms();ready=a.TryGetStartPose(cat,out _);if(ready)break;
                    }
                }
            }
            if(!ready){
                var detail=new List<string>{a.StoreProductId+",breed="+CatBreedService.SelectedBreedId+",body="+cat.IsInteractionPoseClear(cat.transform.position,cat.transform.rotation)+",pos="+cat.transform.position+",target="+look};
                foreach(var other in Object.FindObjectsByType<HomeProductPlacement>(FindObjectsInactive.Include).Where(x=>x.ProductId!=a.StoreProductId)){
                    var solids=other.GetComponentsInChildren<Collider>().Where(c=>c.enabled).ToArray();foreach(var c in solids)c.enabled=false;Physics.SyncTransforms();
                    detail.Add("without="+other.ProductId+",ready="+a.TryGetStartPose(cat,out _));foreach(var c in solids)c.enabled=true;Physics.SyncTransforms();}
                System.IO.File.WriteAllLines(UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Temp")+"/start-diagnostic.txt",detail);
            }
            Assert.That(ready,Is.True,a.StoreProductId+" has a reachable real start stance");
            int completed=0;System.Action<CatActivity> handler=x=>{if(x==a)completed++;};CatActivity.Completed+=handler;
            try{
                Assert.That(a.TryStart(cat),Is.True,a.StoreProductId+" begins from authored clear entry");
                float deadline=Time.realtimeSinceStartup+22;
                var trace=new List<string>();
                while(a.IsRunning&&Time.realtimeSinceStartup<deadline){var animation=cat.GetComponent<CatActivityAnimation>();trace.Add(Time.time+","+cat.transform.position+","+(animation==null?"none":animation.CurrentPose.ToString())+",rest="+a.IsWaitingForRestStop+",owned="+HomeStoreService.IsOwned(a.StoreProductId));RoomPlayModeSupport.StopObservedRest(a);yield return null;}
                System.IO.File.WriteAllLines(UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Temp")+"/"+a.StoreProductId+"-trace.csv",trace);
                Assert.That(a.IsRunning,Is.False,a.StoreProductId+" completes");Assert.That(completed,Is.EqualTo(1),a.StoreProductId+" one completion");
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);Assert.That(cat.IsMovementPhysicallyLocked,Is.False);Assert.That(cc.enabled,Is.True);
                if(a is SitLookActivity)Assert.That(((SitLookActivity)a).GestureBeats,Is.EqualTo(3),a.StoreProductId);
                if(a is LivingFurnitureActivity furniture&&furniture.Kind==CatActivityKind.CoffeeTablePlay){Assert.That(furniture.DidPush,Is.True);Assert.That(furniture.ContactDistance,Is.LessThan(.1f));}
                report.Add(a.StoreProductId+",complete=1,movementReleased=true");
            }finally{CatActivity.Completed-=handler;System.IO.File.WriteAllLines(UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Temp")+"/room-interactions.csv",report);}
        }
        Assert.That(report.Count,Is.EqualTo(permanent?2:chairOnly?1:6));
    }
}
#endif
