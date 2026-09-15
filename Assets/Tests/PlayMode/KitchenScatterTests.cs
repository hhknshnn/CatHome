using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class KitchenScatterTests
{
    HomeStoreSaveState store;string breed;float speed,capture;CatMovement cat;
    readonly List<string> rows=new List<string>();
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Temp/KitchenScatter");
    [SetUp]public void Before(){rows.Clear();Assert.That(EditorQaSession.IsActive,Is.True);store=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;speed=Time.timeScale;capture=Time.captureDeltaTime;Time.timeScale=1;Time.captureFramerate=24;}
    [TearDown]public void After(){if(cat!=null)CatActionState.CancelForTransition(cat);RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(store);CatBreedService.Select(breed);Time.timeScale=speed;Time.captureDeltaTime=capture;Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/"+TestContext.CurrentContext.Test.Name+".txt",rows);}
    IEnumerator Prepare(){yield return RoomPlayModeSupport.LoadRoomAlone("Kitchen_Level01");var state=HomeStoreSaveState.CreateDefault();state.ownedProductIds=HomeStoreService.KitchenCollection.ToArray();HomeStoreService.ApplySavedState(state);yield return null;yield return null;cat=Object.FindAnyObjectByType<CatMovement>();cat.GetComponent<CatIdleBehavior>().enabled=false;}
    void Start(SurfaceScatterActivity a){CatActionState.CancelForTransition(cat);var cc=cat.GetComponent<CharacterController>();cc.enabled=false;var p=a.RoutineEntryPoint.position;p.y=0;cat.transform.SetPositionAndRotation(p,Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();RoomPlayModeSupport.ProvisionNeeds();Assert.That(a.TryStart(cat),Is.True,a.DisplayName);}
    void CheckPaws(SurfaceScatterActivity a,string breedId)
    {
        var hips=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine");var shoulders=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine.003");
        Assert.That(CatActivityFacing.FacingDot(shoulders.position-hips.position,hips.position,CatActivityFacing.CameraPosition(cat)),Is.GreaterThanOrEqualTo(-.01f),breedId+" hidden body");
        var source=CatBreedCatalog.Load().Find(breedId).SourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);var weights=source.sharedMesh.boneWeights;
        var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh,true);var vertices=mesh.vertices;
        try{foreach(string name in new[]{"DEF-hand.L","DEF-hand.R","DEF-foot.L","DEF-foot.R"}){var paw=CatBreedVisualFactory.FindDescendant(cat.transform,name);var sourcePaw=source.bones.First(b=>b.name==name);float sole=float.PositiveInfinity;for(int i=0;i<weights.Length;i++){var w=weights[i];int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] ws={w.weight0,w.weight1,w.weight2,w.weight3};float influence=0;for(int j=0;j<4;j++)if(source.bones[ids[j]]==sourcePaw||source.bones[ids[j]].IsChildOf(sourcePaw))influence+=ws[j];if(influence>.5f)sole=Mathf.Min(sole,skin.transform.TransformPoint(vertices[i]).y);}var hits=Physics.RaycastAll(paw.position+Vector3.up*.12f,Vector3.down,.4f,~0,QueryTriggerInteraction.Ignore);Assert.That(hits.Any(h=>h.normal.y>.7f&&Mathf.Abs(h.point.y-a.PerchPoint.position.y)<.025f&&Mathf.Abs(sole-h.point.y)<.045f),Is.True,breedId+"/"+name+" unsupported sole="+sole+" anchor="+a.PerchPoint.position);}}
        finally{Object.Destroy(mesh);}
    }
    [UnityTest]public IEnumerator Fruit_TenBreeds_ClimbTouchSpillAndReturn()
    {yield return FullRoutine(HomeStoreService.KitchenFruitBasketId,10,"fruit");}
    [UnityTest]public IEnumerator Dining_TenBreeds_ClimbTouchTossAndReturn()
    {yield return FullRoutine(string.Empty,3,"dining");}
    [UnityTest]public IEnumerator Scatter_15_30_60Fps_AllContactsCompleteForBlackCat()
    {
        foreach(int fps in new[]{15,30,60})
        {Time.captureFramerate=fps;yield return FullRoutine(HomeStoreService.KitchenFruitBasketId,10,"fruit-fps"+fps,"oriental-shorthair");yield return FullRoutine(string.Empty,3,"dining-fps"+fps,"oriental-shorthair");}
    }
    IEnumerator FullRoutine(string id,int count,string label,string onlyBreed=null)
    {
        yield return Prepare();var a=CatActivity.Registered.OfType<SurfaceScatterActivity>().Single(s=>string.IsNullOrEmpty(id)?s.Kind==CatActivityKind.DiningScatter:s.StoreProductId==id);
        if(!string.IsNullOrEmpty(id))
        {
            var frame=a.PropPivot.GetComponentsInChildren<Renderer>().Single(r=>r.name=="BasketFrame");
            Assert.That(frame.bounds.min.y,Is.EqualTo(a.PerchPoint.position.y).Within(.006f),"Basket base must sit on the counter, upright.");
            Assert.That(a.LooseParts.Max(p=>p.GetComponent<Renderer>().bounds.max.y),Is.GreaterThan(a.PerchPoint.position.y+.31f),"Fruit tiers must rise above the counter.");
        }
        foreach(var b in CatBreedCatalog.Load().Entries.Where(b=>onlyBreed==null||b.Id==onlyBreed)){CatBreedService.Select(b.Id);yield return null;yield return null;var parts=a.LooseParts.ToArray();var home=parts.Select(t=>t.localPosition).ToArray();var parents=parts.Select(t=>t.parent).ToArray();var rotations=parts.Select(t=>t.localRotation).ToArray();Vector3 scale=cat.transform.localScale;int events=0;Action<CatActivity> handler=x=>{if(x==a)events++;};CatActivity.Completed+=handler;Start(a);bool held=false,spilled=false,shot=false;float low=1;float deadline=Time.realtimeSinceStartup+20;
            try{while(a.IsRunning&&Time.realtimeSinceStartup<deadline){yield return new WaitForEndOfFrame();Assert.That(cat.transform.localScale,Is.EqualTo(scale));if(!held&&a.IsPerched&&!a.IsPawing&&!a.IsScattering&&a.ContactStrokes>0){CheckPaws(a,b.Id);held=true;if(b.Id=="oriental-shorthair")ScreenCapture.CaptureScreenshot(Output+"/"+label+"-touch.png");}if(a.IsScattering){spilled=true;foreach(var p in parts)foreach(var r in p.GetComponentsInChildren<Renderer>())low=Mathf.Min(low,r.bounds.min.y);if(b.Id=="oriental-shorthair"&&!shot&&parts.Any(p=>p.position.y<.30f)){shot=true;ScreenCapture.CaptureScreenshot(Output+"/"+label+"-spill.png");}}}}
            finally{CatActivity.Completed-=handler;}
            string row=b.Id+",contacts="+a.ContactStrokes+",parts="+a.ScatteredParts+",events="+events+",held="+held+",spilled="+spilled+",minY="+low+",paw="+a.MinimumPawDistance+",lastPaw="+a.LastPawDistance+",target="+a.CurrentContact;rows.Add(row);
            Assert.That(a.IsRunning,Is.False,row);Assert.That(events,Is.EqualTo(1),row);Assert.That(a.ContactStrokes,Is.EqualTo(3),row);Assert.That(a.ScatteredParts,Is.EqualTo(count),row);Assert.That(held&&spilled,Is.True,row);Assert.That(low,Is.GreaterThanOrEqualTo(-.002f),row);
            for(int i=0;i<parts.Length;i++){Assert.That(parts[i].parent,Is.SameAs(parents[i]));Assert.That(Vector3.Distance(parts[i].localPosition,home[i]),Is.LessThan(.0001f));Assert.That(Quaternion.Angle(parts[i].localRotation,rotations[i]),Is.LessThan(.001f));}
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True,row);
        }
    }
    [UnityTest]public IEnumerator Fruit_PauseCancelAndHostOwnershipRestoreEverything()
    {
        yield return Prepare();var a=CatActivity.Registered.OfType<SurfaceScatterActivity>().Single(s=>s.StoreProductId==HomeStoreService.KitchenFruitBasketId);var parts=a.LooseParts.ToArray();var homes=parts.Select(t=>t.localPosition).ToArray();var parents=parts.Select(t=>t.parent).ToArray();
        foreach(bool spilling in new[]{false,true}){Start(a);float deadline=Time.realtimeSinceStartup+15;while(a.IsRunning&&(spilling?!parts.Any(p=>p.parent==a.transform):!a.IsPawing)&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(a.IsRunning,Is.True);Time.timeScale=0;yield return new WaitForEndOfFrame();var root=cat.transform.position;var positions=parts.Select(p=>p.position).ToArray();for(int f=0;f<8;f++)yield return null;Assert.That(cat.transform.position,Is.EqualTo(root));for(int i=0;i<parts.Length;i++)Assert.That(Vector3.Distance(parts[i].position,positions[i]),Is.LessThan(.0001f));a.CancelForTransition();Time.timeScale=1;yield return RoomPlayModeSupport.WaitForMovementRelease(cat);for(int i=0;i<parts.Length;i++){Assert.That(parts[i].parent,Is.SameAs(parents[i]));Assert.That(Vector3.Distance(parts[i].localPosition,homes[i]),Is.LessThan(.0001f));}Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True);}
        var state=HomeStoreSaveState.CreateDefault();state.ownedProductIds=HomeStoreService.KitchenCollection.Where(id=>id!=HomeStoreService.KitchenIslandId).ToArray();HomeStoreService.ApplySavedState(state);yield return null;Assert.That(a.IsUnlocked,Is.False);Assert.That(a.IsContentVisible,Is.False);rows.Add("Pause/tap, pause/spill, cancel/reset, missing host: pass");
    }
    [UnityTest]public IEnumerator Dining_PauseAndCancelDuringJumpTouchAndFlightRestorePropsAndCat()
    {
        yield return Prepare();var a=CatActivity.Registered.OfType<SurfaceScatterActivity>().Single(s=>s.Kind==CatActivityKind.DiningScatter);
        var parts=a.LooseParts.ToArray();var homes=parts.Select(t=>t.localPosition).ToArray();var parents=parts.Select(t=>t.parent).ToArray();
        for(int stage=0;stage<3;stage++)
        {
            Start(a);float deadline=Time.realtimeSinceStartup+15;
            while(a.IsRunning&&Time.realtimeSinceStartup<deadline)
            {bool reached=stage==0?cat.transform.position.y>.12f&&!a.IsPerched:stage==1?a.IsPawing:a.IsScattering&&parts.Any(p=>p.position.y<.50f);if(reached)break;yield return null;}
            Assert.That(a.IsRunning,Is.True,"stage "+stage);Time.timeScale=0;yield return new WaitForEndOfFrame();
            var position=cat.transform.position;var points=parts.Select(p=>p.position).ToArray();for(int i=0;i<8;i++)yield return null;
            Assert.That(cat.transform.position,Is.EqualTo(position));for(int i=0;i<parts.Length;i++)Assert.That(parts[i].position,Is.EqualTo(points[i]));
            a.CancelForTransition();Time.timeScale=1;yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            for(int i=0;i<parts.Length;i++){Assert.That(parts[i].parent,Is.SameAs(parents[i]));Assert.That(Vector3.Distance(parts[i].localPosition,homes[i]),Is.LessThan(.0001f));}
            Assert.That(cat.transform.position.y,Is.LessThan(.065f),"Grounded CharacterController keeps its normal 5 cm root offset.");Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True);
        }
        rows.Add("Jump/touch/flight pause and cancellation: pass");
    }
    [UnityTest]public IEnumerator KitchenRoutes_AllElevenActionsAccessibleAroundDiningFurniture()
    {
        yield return Prepare();Physics.SyncTransforms();var actions=CatActivity.Registered.Where(a=>a.gameObject.scene==cat.gameObject.scene&&!a.IsRetired&&(HomeStoreService.IsProductInRoomCollection(HomeRoomService.KitchenId,a.StoreProductId)||a.Kind==CatActivityKind.DiningScatter)).ToArray();
        Assert.That(actions.Length,Is.EqualTo(11));
        foreach(var from in new[]{new Vector3(0,0,-1.8f),new Vector3(-1.25f,0,-.8f),new Vector3(1.5f,0,.7f)})
        {
            Assert.That(CatActivityMotion.IsControllerFloorClear(cat,from),Is.True,"Start "+from);
            foreach(var a in actions)
            {var target=a.RoutineEntryPoint.position;target.y=0;Assert.That(CatActivityMotion.TryFloorPath(cat,from,target,out var path),Is.True,a.DisplayName+" from "+from);rows.Add(from+" to "+a.DisplayName+": "+path.Count+" points");}
        }
        yield return null;
    }
}
