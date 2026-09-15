#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class CeramicBowlContactTests
{
    HomeStoreSaveState store; string breed; float timeScale,capture;
    CatMovement cat; CatEnrichmentActivity bowl;
    readonly List<string> evidence=new List<string>();
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Temp/CeramicMeal");
    [SetUp] public void Before()
    {Assert.That(EditorQaSession.IsActive,Is.True);store=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;timeScale=Time.timeScale;capture=Time.captureDeltaTime;Time.timeScale=1;Time.captureFramerate=60;}
    [TearDown] public void After()
    {
        if(CatActivity.Active!=null)CatActivity.Active.CancelForTransition();
        RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(store);CatBreedService.Select(breed);
        Time.timeScale=timeScale;Time.captureDeltaTime=capture;
        Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/"+TestContext.CurrentContext.Test.Name+".csv",evidence);
    }
    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        var state=HomeStoreSaveState.CreateDefault();
        state.ownedProductIds=HomeStoreService.Products.Where(p=>CatCollectionPolicy.IsCatItem(p.Id)||HomeStoreService.IsLivingRoomCollectionProduct(p.Id)).Select(p=>p.Id).ToArray();
        state.storedProductIds=state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();HomeStoreService.ApplySavedState(state);
        foreach(string id in new[]{HomeStoreService.CozyPodBedId,HomeStoreService.ToyMouseId,HomeStoreService.CeramicBowlId,HomeStoreService.FeatherToyId,HomeStoreService.CollarId})
            Assert.That(HomeStoreService.TrySetStored(id,false),Is.True,id);
        yield return null;yield return null;
        cat=Object.FindAnyObjectByType<CatMovement>();var idle=cat.GetComponent<CatIdleBehavior>();if(idle!=null)idle.enabled=false;
        bowl=Object.FindObjectsByType<CatEnrichmentActivity>(FindObjectsSortMode.None).Single(a=>a.StoreProductId==HomeStoreService.CeramicBowlId);
    }
    void Place()
    {
        var cc=cat.GetComponent<CharacterController>();cc.enabled=false;
        var point=bowl.RoutineEntryPoint.position;point.y=0;cat.transform.SetPositionAndRotation(point,Quaternion.identity);
        cc.enabled=true;Physics.SyncTransforms();RoomPlayModeSupport.ProvisionNeeds();
    }
    static List<Vector3> Food(CatEnrichmentActivity activity)
    {
        var result=new List<Vector3>();
        foreach(var filter in activity.GetComponentsInChildren<MeshFilter>())
        using(var data=UnityEditor.MeshUtility.AcquireReadOnlyMeshData(filter.sharedMesh))
        using(var vertices=new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount,Unity.Collections.Allocator.Temp))
        {
            data[0].GetVertices(vertices);var materials=filter.GetComponent<Renderer>().sharedMaterials;
            for(int sub=0;sub<data[0].subMeshCount;sub++)
            {
                if(materials[sub].name.IndexOf("Cream",StringComparison.OrdinalIgnoreCase)<0)continue;
                using(var indices=new Unity.Collections.NativeArray<int>(data[0].GetSubMesh(sub).indexCount,Unity.Collections.Allocator.Temp))
                {data[0].GetIndices(indices,sub);foreach(int i in indices)result.Add(filter.transform.TransformPoint(vertices[i]));}
            }
        }
        return result;
    }
    static float Edge(Vector3 p,Vector3 a,Vector3 b)
    {var d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(d.sqrMagnitude,1e-12f)));}
    static float Triangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
    {
        var ab=b-a;var ac=c-a;var n=Vector3.Cross(ab,ac);
        if(n.sqrMagnitude>1e-14f)
        {
            var q=p-n*(Vector3.Dot(p-a,n)/n.sqrMagnitude);var ap=q-a;
            float aa=Vector3.Dot(ab,ab),cc=Vector3.Dot(ac,ac),cross=Vector3.Dot(ab,ac),den=aa*cc-cross*cross;
            if(den>1e-14f){float u=(cc*Vector3.Dot(ap,ab)-cross*Vector3.Dot(ap,ac))/den,v=(aa*Vector3.Dot(ap,ac)-cross*Vector3.Dot(ap,ab))/den;if(u>=0&&v>=0&&u+v<=1)return Vector3.Distance(p,q);}
        }
        return Mathf.Min(Edge(p,a,b),Mathf.Min(Edge(p,b,c),Edge(p,c,a)));
    }
    [UnityTest] public IEnumerator TenBreeds_BiteRealFood_KeepPawsOutsideBowl_AndRelease()
    {
        yield return Prepare();var food=Food(bowl);Assert.That(food,Is.Not.Empty);
        var foodBounds=new Bounds(food[0],Vector3.zero);foreach(var point in food)foodBounds.Encapsulate(point);
        var body=bowl.GetComponentInChildren<Renderer>().bounds;
        var failures=new List<string>();var mesh=new Mesh();int events=0;
        Action<CatActivity> completed=a=>{if(a==bowl)events++;};CatActivity.Completed+=completed;
        evidence.Add("breed,samples,insideFood,minFoodDistance,minFacing,minPawRadius,completed,exitClear");
        try{foreach(var entry in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(entry.Id);yield return null;yield return null;Place();events=0;
            var scale=cat.transform.localScale;var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();
            var mouth=CatSipMouthCatalog.Load().Find(entry.Id).vertices;
            var hips=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine");var shoulders=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine.003");
            var paws=new[]{"DEF-hand.L","DEF-hand.R","DEF-foot.L","DEF-foot.R"}.Select(n=>CatBreedVisualFactory.FindDescendant(cat.transform,n)).ToArray();
            Assert.That(bowl.TryStart(cat),Is.True,entry.Id+" starts");
            float deadline=Time.realtimeSinceStartup+25,minDistance=10,minDot=1,minPaw=10;int samples=0,inside=0;
            while(bowl.IsRunning&&Time.realtimeSinceStartup<deadline)
            {
                yield return new WaitForEndOfFrame();
                if(!bowl.IsRunning||cat.GetComponent<CatActivityAnimation>().CurrentPose!=CatActivityPose.Eat)continue;
                skin.BakeMesh(mesh,true);var vertices=mesh.vertices;float radius=float.PositiveInfinity;
                foreach(var v in mouth)
                {
                    Vector3 p=skin.transform.TransformPoint(vertices[v.vertexIndex]);var delta=p-foodBounds.center;
                    radius=Mathf.Min(radius,delta.x*delta.x/(foodBounds.extents.x*foodBounds.extents.x)+delta.z*delta.z/(foodBounds.extents.z*foodBounds.extents.z));
                    if(samples%6==0)for(int i=0;i<food.Count;i+=3)minDistance=Mathf.Min(minDistance,Triangle(p,food[i],food[i+1],food[i+2]));
                }
                if(radius<=1)inside++;samples++;
                minDot=Mathf.Min(minDot,CatActivityFacing.FacingDot(shoulders.position-hips.position,(shoulders.position+hips.position)*.5f,CatActivityFacing.CameraPosition(cat)));
                foreach(var paw in paws){var d=paw.position-body.center;minPaw=Mathf.Min(minPaw,Mathf.Sqrt(d.x*d.x/(body.extents.x*body.extents.x)+d.z*d.z/(body.extents.z*body.extents.z)));}
                Assert.That(cat.transform.localScale,Is.EqualTo(scale));
                Assert.That(cat.GetComponent<CatSipHeadMotion>()==null||!cat.GetComponent<CatSipHeadMotion>().IsActive,Is.True,"Original pose, no head IK");
            }
            if(bowl.IsRunning)bowl.CancelForTransition();yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            bool clear=CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position);
            string row=string.Join(",",entry.Id,samples,inside,minDistance.ToString(System.Globalization.CultureInfo.InvariantCulture),minDot.ToString(System.Globalization.CultureInfo.InvariantCulture),minPaw.ToString(System.Globalization.CultureInfo.InvariantCulture),events==1,clear);evidence.Add(row);
            if(samples<20||inside<samples*.85f||minDistance>.025f||minDot<-.01f||minPaw<1||events!=1||!clear)failures.Add(row);
            Assert.That(cat.GetComponent<CharacterController>().enabled&&!cat.IsMovementPhysicallyLocked,Is.True);
        }}finally{CatActivity.Completed-=completed;Object.Destroy(mesh);}
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }
    [UnityTest] public IEnumerator PauseAndCancel_KeepMealStill_AndRestoreControl()
    {
        yield return Prepare();Place();Assert.That(bowl.TryStart(cat),Is.True);
        float deadline=Time.realtimeSinceStartup+15;
        while(cat.GetComponent<CatActivityAnimation>().CurrentPose!=CatActivityPose.Eat&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(cat.GetComponent<CatActivityAnimation>().CurrentPose,Is.EqualTo(CatActivityPose.Eat));
        yield return new WaitForSeconds(.3f);
        // Pausing the game must use its normal clock, not the capture clock.
        Time.captureFramerate=0;Time.timeScale=0;
        // Flush coroutine and Animator work queued on the pause frame before sampling.
        for(int i=0;i<4;i++)yield return null;
        yield return new WaitForEndOfFrame();
        var jaw=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-jaw");var mouth=jaw.position;var root=cat.transform.position;
        var animator=cat.GetComponentInChildren<Animator>();
        float phase=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        for(int i=0;i<5;i++)yield return new WaitForEndOfFrame();
        Assert.That(Time.deltaTime,Is.Zero,"The gameplay clock is paused");
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.EqualTo(phase).Within(.00001f));
        Assert.That(Vector3.Distance(root,cat.transform.position),Is.LessThan(.00001f));Assert.That(Vector3.Distance(mouth,jaw.position),Is.LessThan(.0005f));
        evidence.Add("Root, mouth and animation phase remain still after queued pause-frame work.");
        bowl.CancelForTransition();Time.timeScale=1;yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.GetComponent<CharacterController>().enabled&&!cat.IsMovementPhysicallyLocked,Is.True);
        Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True);
    }
}
#endif
