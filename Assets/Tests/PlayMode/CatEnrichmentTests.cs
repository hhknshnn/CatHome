using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CatEnrichmentTests
{
    HomeStoreSaveState store;string breed;float speed;
    [SetUp] public void Before(){store=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;speed=Time.timeScale;}
    [TearDown] public void After()
    {
        if(CatActivity.Active!=null)CatActivity.Active.enabled=false;
        Time.timeScale=speed;RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(store);CatBreedService.Select(breed);
    }
    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        typeof(CatHomeSaveSystem).GetField("initialized",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,false);
        var owned=new List<string>();
        foreach(var d in Object.FindObjectsByType<StoreProductDisplay>(FindObjectsInactive.Include,FindObjectsSortMode.None))owned.Add(d.ProductId);
        owned.Add(HomeStoreService.BallBasketId);owned.Add(HomeStoreService.ScratchPostId);
        var state=HomeStoreSaveState.CreateDefault();state.ownedProductIds=owned.ToArray();state.storedProductIds=owned.FindAll(CatCollectionPolicy.IsCatItem).ToArray();HomeStoreService.ApplySavedState(state);
        yield return null;Physics.SyncTransforms();
    }
    void EquipOnly(string id)
    {
        foreach(var p in HomeStoreService.Products)
            if(CatCollectionPolicy.IsCatItem(p.Id)&&HomeStoreService.IsOwned(p.Id)) HomeStoreService.TrySetStored(p.Id,true);
        HomeStoreService.TrySetPlacement(id,new Vector3(id==HomeStoreService.PlayTunnelId?1.4f:0,0,-.8f),0);
        Assert.That(HomeStoreService.TrySetStored(id,false),Is.True,id);
        Physics.SyncTransforms();
    }
    [UnityTest] public IEnumerator AllFifteenProducts_AllTenBreeds_AnimateAndReleaseInTheCompleteRoom()
    {
        yield return Prepare();
        var cat=Object.FindAnyObjectByType<CatMovement>();var controller=cat.GetComponent<CharacterController>();var parent=cat.transform.parent;var scale=cat.transform.localScale;
        var products=Object.FindObjectsByType<CatEnrichmentActivity>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Assert.That(products.Length,Is.EqualTo(15));var failures=new List<string>();
        var report=new System.Text.StringBuilder("product,breed,started,finished,boneMotion,movingPart,exitClear\n");
        var breeds=CatBreedCatalog.Load();Time.timeScale=8;
        for(int i=0;i<breeds.Count;i++)
        {
            CatBreedService.Select(breeds.Get(i).Id);yield return null;yield return null;
            Transform bone=null;foreach(var t in cat.GetComponentsInChildren<Transform>())if(t.name=="DEF-spine.003")bone=t;
            Assert.That(bone,Is.Not.Null);
            foreach(var activity in products)
            {
                EquipOnly(activity.StoreProductId);yield return null;
                RoomPlayModeSupport.ProvisionNeeds();controller.enabled=false;cat.transform.SetPositionAndRotation(new Vector3(0,0,-1.7f),Quaternion.identity);controller.enabled=true;Physics.SyncTransforms();
                string label=activity.StoreProductId+"/"+breeds.Get(i).Id;
                bool started=activity.TryStart(cat);float motion=0;bool moved=activity.MovingPart==null;
                var rotation=bone.localRotation;var toyRotation=activity.MovingPart!=null?activity.MovingPart.localRotation:Quaternion.identity;
                var toyPosition=activity.MovingPart!=null?activity.MovingPart.localPosition:Vector3.zero;
                float deadline=Time.realtimeSinceStartup+14;
                while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
                {
                    yield return null;motion=Mathf.Max(motion,Quaternion.Angle(rotation,bone.localRotation));
                    if(activity.MovingPart!=null)moved|=Quaternion.Angle(toyRotation,activity.MovingPart.localRotation)>.5f||Vector3.Distance(toyPosition,activity.MovingPart.localPosition)>.002f;
                }
                bool finished=!activity.IsRunning;bool clear=CatActivityMotion.IsFloorClear(cat.transform.position,.24f);
                report.AppendLine(label.Replace('/',',')+","+started+","+finished+","+motion.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+moved+","+clear);
                if(!started||!finished||motion<.5f||!moved||!clear)failures.Add(label+" start="+started+" finish="+finished+" motion="+motion+" toy="+moved+" clear="+clear);
                if(activity.IsRunning){activity.enabled=false;activity.enabled=true;}
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                Assert.That(cat.IsMovementPhysicallyLocked,Is.False,label);Assert.That(controller.enabled,Is.True,label);
                Assert.That(cat.transform.parent,Is.EqualTo(parent));Assert.That(cat.transform.localScale,Is.EqualTo(scale));
            }
        }
        Directory.CreateDirectory("Docs/QA/CAT_2026-09-06_Refinement");File.WriteAllText("Docs/QA/CAT_2026-09-06_Refinement/breed_matrix.csv",report.ToString());
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }
    [UnityTest] public IEnumerator CancelAndOwnership_RestorePhysicsPoseAndToy()
    {
        yield return Prepare();var cat=Object.FindAnyObjectByType<CatMovement>();
        foreach(var activity in Object.FindObjectsByType<CatEnrichmentActivity>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            EquipOnly(activity.StoreProductId);yield return null;
            RoomPlayModeSupport.ProvisionNeeds();var controller=cat.GetComponent<CharacterController>();controller.enabled=false;cat.transform.position=new Vector3(0,0,-1.7f);controller.enabled=true;
            var start=cat.transform.position;var rotation=activity.MovingPart!=null?activity.MovingPart.localRotation:Quaternion.identity;
            Assert.That(activity.TryStart(cat),Is.True,activity.StoreProductId);yield return null;activity.enabled=false;yield return null;
            Assert.That(CatActivity.Active,Is.Null);Assert.That(controller.enabled,Is.True);Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
            var returned=cat.transform.position-start;
            Assert.That(new Vector2(returned.x,returned.z).magnitude,Is.LessThan(.02f));
            Assert.That(Mathf.Abs(returned.y),Is.LessThan(.075f),"The controller may settle onto the floor when enabled");
            if(activity.MovingPart!=null)Assert.That(Quaternion.Angle(activity.MovingPart.localRotation,rotation),Is.LessThan(.1f));
            activity.enabled=true;
            Assert.That(HomeStoreService.TrySetStored(activity.StoreProductId,true),Is.True);
            Assert.That(activity.TryStart(cat),Is.False,"Stored toys must not offer invisible play");
            Assert.That(activity.IsContentVisible,Is.False);HomeStoreService.TrySetStored(activity.StoreProductId,false);
        }
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        foreach(var activity in Object.FindObjectsByType<CatEnrichmentActivity>(FindObjectsInactive.Include,FindObjectsSortMode.None))Assert.That(activity.TryStart(cat),Is.False);
    }
    [UnityTest] public IEnumerator BasketAndScratchPost_KeepRealPlayOnEveryBreed()
    {
        yield return Prepare();var cat=Object.FindAnyObjectByType<CatMovement>();var controller=cat.GetComponent<CharacterController>();
        var scratch=Object.FindAnyObjectByType<ScratchPostActivity>();var basket=Object.FindAnyObjectByType<BallChaseActivity>();
        var ball=(Transform)typeof(BallChaseActivity).GetField("ball",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(basket);
        var breeds=CatBreedCatalog.Load();Time.timeScale=1;
        for(int i=0;i<breeds.Count;i++)
        {
            CatBreedService.Select(breeds.Get(i).Id);yield return null;yield return null;
            RoomPlayModeSupport.ProvisionNeeds();controller.enabled=false;cat.transform.position=new Vector3(0,0,-1.7f);controller.enabled=true;
            EquipOnly(scratch.StoreProductId);yield return null; Assert.That(scratch.TryStart(cat),Is.True);float deadline=Time.realtimeSinceStartup+12;bool pose=false;
            while(scratch.IsRunning&&Time.realtimeSinceStartup<deadline){yield return null;pose|=cat.GetComponent<CatActivityAnimation>().CurrentPose==CatActivityPose.Scratch;}
            Assert.That(scratch.IsRunning,Is.False);Assert.That(pose,Is.True);Assert.That(controller.enabled,Is.True);Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position,.24f),Is.True);
            RoomPlayModeSupport.ProvisionNeeds();EquipOnly(basket.StoreProductId);yield return null;Assert.That(basket.TryStart(cat),Is.True);deadline=Time.realtimeSinceStartup+12;
            while(basket.IsRunning&&Time.realtimeSinceStartup<deadline)
            {
                yield return null;
                if(basket.CatchCount>0)Assert.That(basket.LastHitDistance,Is.LessThan(.095f),"A real paw contact must precede each roll");
            }
            Assert.That(basket.IsRunning,Is.False);Assert.That(basket.CatchCount,Is.EqualTo(3),basket.InterruptedReason+" / "+breeds.Get(i).Id);
            Assert.That(basket.RolledDistance,Is.GreaterThan(1.9f));
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);Assert.That(cat.IsInputBlocked,Is.False);
        }
    }
    [UnityTest] public IEnumerator RestingAtZeroEnergy_GainsSlowly_AndCancelStopsRecovery()
    {
        yield return Prepare(); EquipOnly(HomeStoreService.CloudBedId);yield return null;
        var cat=Object.FindAnyObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();cc.enabled=false;cat.transform.position=new Vector3(0,0,-2);cc.enabled=true;
        var energy=RoomPlayModeSupport.ProvisionNeeds();energy.ApplySavedValue(0);
        CatEnrichmentActivity bed=null;
        foreach(var a in Object.FindObjectsByType<CatEnrichmentActivity>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(a.StoreProductId==HomeStoreService.CloudBedId)bed=a;
        Assert.That(bed.TryStart(cat),Is.True,"An exhausted cat can rest");
        float deadline=Time.realtimeSinceStartup+12;
        while(!bed.IsResting&&bed.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(bed.IsResting,Is.True);float before=energy.CurrentEnergy;
        yield return new WaitForSeconds(1f);
        Assert.That(energy.CurrentEnergy-before,Is.InRange(.65f,.9f),"Gain is small and there is no passive drain while resting");
        bed.enabled=false;float stopped=energy.CurrentEnergy;yield return new WaitForSeconds(.3f);
        Assert.That(bed.IsResting,Is.False);Assert.That(energy.CurrentEnergy,Is.LessThanOrEqualTo(stopped+.01f));
        Assert.That(cc.enabled,Is.True);bed.enabled=true;
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        energy.ApplySavedValue(99.8f);Assert.That(bed.TryStart(cat),Is.True);
        deadline=Time.realtimeSinceStartup+12;while(bed.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(bed.IsRunning,Is.False);Assert.That(energy.CurrentEnergy,Is.InRange(99.9f,100f));
    }
    [UnityTest] public IEnumerator ToyResponses_FollowRealPawContact_AndUseVariedPoses()
    {
        yield return Prepare();var cat=Object.FindAnyObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();
        var poses=new HashSet<CatActivityPose>();var failures=new List<string>();Time.timeScale=1;
        foreach(var a in Object.FindObjectsByType<CatEnrichmentActivity>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(a.MovingPart==null||a.Mode==CatEnrichmentMode.Tunnel||a.Mode==CatEnrichmentMode.Hide)continue;
            EquipOnly(a.StoreProductId);yield return null;RoomPlayModeSupport.ProvisionNeeds();
            cc.enabled=false;cat.transform.position=new Vector3(0,0,-2);cc.enabled=true;
            Assert.That(a.TryStart(cat),Is.True,a.StoreProductId);float deadline=Time.realtimeSinceStartup+15;
            while(a.IsRunning&&Time.realtimeSinceStartup<deadline){yield return null;poses.Add(cat.GetComponent<CatActivityAnimation>().CurrentPose);}
            if(a.ContactCount==0)failures.Add(a.StoreProductId);
            Assert.That(a.IsRunning,Is.False);yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        }
        Assert.That(failures,Is.Empty,"No real paw contact: "+string.Join(", ",failures));
        foreach(var pose in new[]{CatActivityPose.Pounce,CatActivityPose.Stalk,CatActivityPose.Sniff,CatActivityPose.BatLeft,CatActivityPose.BatRight,CatActivityPose.Push,CatActivityPose.Tug})Assert.That(poses.Contains(pose),Is.True,pose.ToString());
    }
    [UnityTest] public IEnumerator Tunnel_AllBreeds_KeepThePosedBodyAndTailInsideTheCloth()
    {
        yield return Prepare();var cat=Object.FindAnyObjectByType<CatMovement>();
        var controller=cat.GetComponent<CharacterController>();CatEnrichmentActivity tunnel=null;
        foreach(var activity in Object.FindObjectsByType<CatEnrichmentActivity>(FindObjectsSortMode.None))
            if(activity.Mode==CatEnrichmentMode.Tunnel)tunnel=activity;
        EquipOnly(tunnel.StoreProductId);yield return null;
        var breeds=CatBreedCatalog.Load();var sample=new Mesh();Time.timeScale=2;
        var report=new System.Text.StringBuilder("breed,samples,maxEnvelopeRatio,point,pose\n");
        try
        {
            for(int i=0;i<breeds.Count;i++)
            {
                CatBreedService.Select(breeds.Get(i).Id);yield return null;yield return null;
                RoomPlayModeSupport.ProvisionNeeds();controller.enabled=false;cat.transform.position=new Vector3(0,0,-1.7f);controller.enabled=true;
                Assert.That(tunnel.TryStart(cat),Is.True);int samples=0;float maximum=0;float deadline=Time.realtimeSinceStartup+12;
                Vector3 worst=Vector3.zero;CatActivityPose worstPose=CatActivityPose.Walk;
                var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();
                while(tunnel.IsRunning&&Time.realtimeSinceStartup<deadline)
                {
                    yield return new WaitForEndOfFrame();
                    var localCat=tunnel.transform.InverseTransformPoint(cat.transform.position);
                    // Automatic placement changes the approach route. A cat walking
                    // metres beside the tunnel is not inside its cloth cross-section.
                    if(Mathf.Abs(localCat.z)>.6f || Mathf.Abs(localCat.x)>.32f)continue;
                    skin.BakeMesh(sample,true);
                    foreach(var vertex in sample.vertices)
                    {
                        var p=tunnel.transform.InverseTransformPoint(skin.transform.TransformPoint(vertex));
                        if(Mathf.Abs(p.z)>.375f||p.y<.035f)continue;
                        // Inner arch clearance, including the tail vertices that
                        // the normal paw-to-floor contact mask deliberately omits.
                        float ratio=p.x*p.x/(.253f*.253f)+(p.y-.018f)*(p.y-.018f)/(.535f*.535f);
                        if(ratio>maximum){maximum=ratio;worst=p;worstPose=cat.GetComponent<CatActivityAnimation>().CurrentPose;}samples++;
                    }
                }
                report.AppendLine(breeds.Get(i).Id+","+samples+","+maximum.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\""+worst.ToString("F3")+"\","+worstPose);
                Assert.That(tunnel.IsRunning,Is.False);Assert.That(samples,Is.GreaterThan(100));
                Assert.That(maximum,Is.LessThan(1f),breeds.Get(i).Id+" must not penetrate the tunnel fabric");
            }
        }
        finally{Object.Destroy(sample);Directory.CreateDirectory("Docs/QA/CAT_2026-09-06_Refinement");File.WriteAllText("Docs/QA/CAT_2026-09-06_Refinement/tunnel_clearance.csv",report.ToString());}
    }
}
