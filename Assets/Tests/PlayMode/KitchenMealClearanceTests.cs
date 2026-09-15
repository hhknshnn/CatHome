using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class KitchenMealClearanceTests
{
    HomeStoreSaveState store; string breed; float rate,capture; CatMovement cat;
    readonly List<string> rows=new List<string>();
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Temp/KitchenMealClearance");
    [SetUp] public void Before(){Assert.That(EditorQaSession.IsActive,Is.True);store=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;rate=Time.timeScale;capture=Time.captureDeltaTime;Time.timeScale=1;Time.captureFramerate=60;}
    [TearDown] public void After(){if(cat!=null)CatActionState.CancelForTransition(cat);RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(store);CatBreedService.Select(breed);Time.timeScale=rate;Time.captureDeltaTime=capture;Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/meal-head-clearance.csv",rows);}
    [UnityTest] public IEnumerator TenBreeds_HeadClearsTheBowlAndFoodThroughoutTheMeal()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Kitchen_Level01");
        var state=HomeStoreSaveState.CreateDefault();state.ownedProductIds=HomeStoreService.KitchenCollection.ToArray();HomeStoreService.ApplySavedState(state);
        yield return null;yield return null;cat=Object.FindAnyObjectByType<CatMovement>();cat.GetComponent<CatIdleBehavior>().enabled=false;
        var meal=CatActivity.Registered.OfType<MealTimeActivity>().Single();var failures=new List<string>();
        foreach(var b in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(b.Id);yield return null;yield return null;
            var src=b.SourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);var weights=src.sharedMesh.boneWeights;var indices=new List<int>();
            for(int i=0;i<weights.Length;i++){var w=weights[i];int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] ws={w.weight0,w.weight1,w.weight2,w.weight3};float sum=0;for(int j=0;j<4;j++){string n=src.bones[ids[j]].name;if(n=="DEF-spine.006"||n.StartsWith("DEF-jaw")||n.StartsWith("DEF-ear"))sum+=ws[j];}if(sum>.5f)indices.Add(i);}
            Assert.That(indices,Is.Not.Empty);var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();
            var cc=cat.GetComponent<CharacterController>();cc.enabled=false;var start=meal.RoutineEntryPoint.position;start.y=0;cat.transform.SetPositionAndRotation(start,Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();RoomPlayModeSupport.ProvisionNeeds();
            var bodyColliders=meal.GetComponentsInChildren<MeshCollider>();Assert.That(bodyColliders,Is.Not.Empty);
            int events=0;Action<CatActivity> complete=a=>{if(a==meal)events++;};CatActivity.Completed+=complete;
            Assert.That(meal.TryStart(cat),Is.True,b.Id);float rim=float.PositiveInfinity,food=float.PositiveInfinity,maxMouth=0;int samples=0;float deadline=Time.realtimeSinceStartup+20;
            try{while(meal.IsRunning&&Time.realtimeSinceStartup<deadline){yield return new WaitForEndOfFrame();if(!meal.IsEating)continue;samples++;if(samples<30)continue;var head=cat.GetComponent<CatMealHeadMotion>();maxMouth=Mathf.Max(maxMouth,head.Distance);if(samples%8!=0)continue;skin.BakeMesh(mesh,true);var vertices=mesh.vertices;foreach(int i in indices){var world=skin.transform.TransformPoint(vertices[i]);var p=meal.transform.InverseTransformPoint(world);float r=new Vector2(p.x-.17664003f,p.z).magnitude;if(r>.097f&&r<.118f)rim=Mathf.Min(rim,p.y-.17075f);if(r<.097f){var ray=new Ray(new Vector3(world.x,meal.transform.position.y+.7f,world.z),Vector3.down);foreach(var collider in bodyColliders)if(collider.Raycast(ray,out var hit,1f))food=Mathf.Min(food,world.y-hit.point.y);}}if(b.Id=="oriental-shorthair"&&samples==64){ScreenCapture.CaptureScreenshot(Output+"/meal-clearance.png");}}}
            finally{CatActivity.Completed-=complete;Object.Destroy(mesh);}
            string row=b.Id+","+samples+","+events+",rim="+rim+",food="+food+",mouth="+maxMouth;rows.Add(row);
            if(events!=1||samples<70||rim<.005f||food<-.003f||maxMouth>.045f)failures.Add(row);
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True,b.Id);
        }
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }
}
