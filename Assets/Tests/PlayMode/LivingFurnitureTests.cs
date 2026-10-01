using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class LivingFurnitureTests
{
    HomeStoreSaveState store;string breed;float speed,capture;
    [SetUp] public void Before(){store=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;speed=Time.timeScale;capture=Time.captureDeltaTime;Time.captureFramerate=60;}
    [TearDown] public void After()
    {
        if(CatActivity.Active!=null)CatActivity.Active.enabled=false;
        Time.timeScale=speed;Time.captureDeltaTime=capture;RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(store);CatBreedService.Select(breed);
    }
    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        typeof(CatHomeSaveSystem).GetField("initialized",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,false);
        var state=HomeStoreSaveState.CreateDefault();var owned=new List<string>();var stored=new List<string>();
        foreach(var p in HomeStoreService.Products)
            if(HomeStoreService.IsLivingRoomCollectionProduct(p.Id)||CatCollectionPolicy.IsCatItem(p.Id))
            {owned.Add(p.Id);if(CatCollectionPolicy.IsCatItem(p.Id))stored.Add(p.Id);}
        state.ownedProductIds=owned.ToArray();state.storedProductIds=stored.ToArray();HomeStoreService.ApplySavedState(state);
        yield return null;Physics.SyncTransforms();CatRoomArrangement.Request(catScene()).Invalidate();
        foreach(var id in new[]{HomeStoreService.CloudBedId,HomeStoreService.FeatherToyId,HomeStoreService.PlayTunnelId,HomeStoreService.BallBasketId,HomeStoreService.ToyMouseId})
            Assert.That(HomeStoreService.TrySetStored(id,false),Is.True,id);
        yield return null;Physics.SyncTransforms();
    }
    static UnityEngine.SceneManagement.Scene catScene()=>UnityEngine.SceneManagement.SceneManager.GetSceneByName("LivingRoom_Level01");
    [UnityTest] public IEnumerator SofaAndTable_AllTenBreeds_JumpTouchAndReturnToOpenFloor()
    {
        yield return Prepare();var cat=Object.FindAnyObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();
        var parent=cat.transform.parent;var scale=cat.transform.localScale;var breeds=CatBreedCatalog.Load();Time.timeScale=1;
        var activities=Object.FindObjectsByType<LivingFurnitureActivity>();
        Assert.That(activities.Length,Is.EqualTo(2));
        var report=new System.Text.StringBuilder("breed,activity,supportSamples,maxContactGap,pushed\n");
        var mesh=new Mesh();
        try
        {
            for(int i=0;i<breeds.Count;i++)
            {
                CatBreedService.Select(breeds.Get(i).Id);yield return null;yield return null;
                foreach(var activity in activities)
                {
                    RoomPlayModeSupport.ProvisionNeeds();cc.enabled=false;cat.transform.position=new Vector3(1.5f,0,1.1f);cc.enabled=true;
                    Assert.That(activity.TryStart(cat),Is.True,breeds.Get(i).Id+"/"+activity.Kind);
                    int samples=0;float maxGap=0;float deadline=Time.realtimeSinceStartup+18;
                    while(activity.IsRunning && Time.realtimeSinceStartup<deadline)
                    {
            RoomPlayModeSupport.StopObservedRest(activity);
                        yield return new WaitForEndOfFrame();
                        var animation=cat.GetComponent<CatActivityAnimation>();
                        if(animation.ContactSurface!=activity.Perch)continue;
                        var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();skin.BakeMesh(mesh,true);
                        float min=float.PositiveInfinity;var vertices=mesh.vertices;
                        foreach(int index in breeds.Get(i).ContactVertexIndices)
                        {
                            min=Mathf.Min(min,skin.transform.TransformPoint(vertices[index]).y);
                            if(activity.Kind==CatActivityKind.SofaLounge)
                            {
                                Vector3 local=activity.Perch.InverseTransformPoint(skin.transform.TransformPoint(vertices[index]));
                                var size=activity.Perch.GetComponent<CatActivitySurface>().Size;
                                Assert.That(Mathf.Abs(local.x),Is.LessThan(size.x*.5f+.08f),breeds.Get(i).Id+" sofa depth");
                                Assert.That(Mathf.Abs(local.z),Is.LessThan(size.y*.5f+.08f),breeds.Get(i).Id+" sofa cushion width");
                            }
                        }
                        maxGap=Mathf.Max(maxGap,Mathf.Abs(min-activity.Perch.position.y));samples++;
                    }
                    report.AppendLine(breeds.Get(i).Id+","+activity.Kind+","+samples+","+maxGap+","+activity.DidPush);
                    Assert.That(activity.IsRunning,Is.False);Assert.That(samples,Is.GreaterThan(10));
                    Assert.That(maxGap,Is.LessThan(.065f),breeds.Get(i).Id+" visibly meets the support");
                    if(activity.Kind==CatActivityKind.CoffeeTablePlay)
                    {Assert.That(activity.DidPush,Is.True,breeds.Get(i).Id);Assert.That(activity.ContactDistance,Is.LessThan(.1f));}
                    Assert.That(cc.enabled,Is.True);Assert.That(cat.transform.parent,Is.EqualTo(parent));Assert.That(cat.transform.localScale,Is.EqualTo(scale));
                    yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                    Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position,.24f),Is.True);
                }
            }
        }
        finally
        {
            Object.Destroy(mesh);var folder=UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Temp/LivingFurniture");System.IO.Directory.CreateDirectory(folder);
            System.IO.File.WriteAllText(folder+"/living-furniture-breeds.csv",report.ToString());
        }
    }
    [UnityTest] public IEnumerator AutomaticPlacement_ReplacesOldCoordinates_AndLeavesCareAccessible()
    {
        yield return Prepare();var cat=Object.FindAnyObjectByType<CatMovement>();
        HomeStoreService.TrySetPlacement(HomeStoreService.PlayTunnelId,new Vector3(0,0,2.22f),0);
        yield return null;yield return null;Physics.SyncTransforms();
        foreach(var p in Object.FindObjectsByType<HomeProductPlacement>(FindObjectsInactive.Include))
            if(CatCollectionPolicy.IsCatItem(p.ProductId)&&!HomeStoreService.IsStored(p.ProductId))
            {Assert.That(p.IsCurrentPositionValid(),Is.True,p.ProductId);Assert.That(p.BeginPreview(),Is.False);}
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if(t.name=="FoodInteractionPoint"||t.name=="WaterInteractionPoint"||t.name=="SofaJumpEntry"||t.name=="TableJumpEntry")
                Assert.That(CatActivityMotion.TryFloorPath(cat.transform.position,t.position,out _),Is.True,t.name);
        Assert.That(CatCollectionPolicy.DisplayedCount,Is.EqualTo(5));
    }
}
