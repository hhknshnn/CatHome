using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ReferenceMotionTests
{
    HomeStoreSaveState store;string breed;
    [SetUp]public void Before(){store=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;}
    [TearDown]public void After()
    {
        if(CatActivity.Active!=null)CatActivity.Active.enabled=false;
        Time.timeScale=1;RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(store);CatBreedService.Select(breed);
    }
    IEnumerator Prepare(string id)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        var state=HomeStoreSaveState.CreateDefault();state.ownedProductIds=HomeStoreService.Products.Where(p=>HomeStoreService.IsLivingRoomCollectionProduct(p.Id)||CatCollectionPolicy.IsCatItem(p.Id)).Select(p=>p.Id).ToArray();
        state.storedProductIds=state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();HomeStoreService.ApplySavedState(state);
        yield return null;Physics.SyncTransforms();
        var layout=Object.FindAnyObjectByType<CatRoomArrangement>();layout.Invalidate();
        if(id!=null)Assert.That(HomeStoreService.TrySetStored(id,false),Is.True);yield return null;
    }
    [UnityTest]public IEnumerator Scratch_AllBreeds_ContactThePostWithBothFrontPaws()
    {
        yield return Prepare(HomeStoreService.ScratchPostId);
        var cat=Object.FindAnyObjectByType<CatMovement>();var controller=cat.GetComponent<CharacterController>();
        var scratch=Object.FindAnyObjectByType<ScratchPostActivity>();var breeds=CatBreedCatalog.Load();
        var evidence=new List<string>();
        for(int i=0;i<breeds.Count;i++)
        {
            CatBreedService.Select(breeds.Get(i).Id);yield return null;yield return null;RoomPlayModeSupport.ProvisionNeeds();
            controller.enabled=false;cat.transform.position=scratch.RoutineEntryPoint.position;controller.enabled=true;
            Assert.That(scratch.TryStart(cat),Is.True,breeds.Get(i).Id);float until=Time.realtimeSinceStartup+15;
            int visibleSamples=0;float minView=1,maxView=-1;
            while(scratch.IsRunning && Time.realtimeSinceStartup<until)
            {
                RoomPlayModeSupport.StopObservedRest(scratch);yield return new WaitForEndOfFrame();
                var contact=cat.GetComponent<CatToyContactMotion>();
                if(contact==null||float.IsPositiveInfinity(contact.LeftDistance)||float.IsPositiveInfinity(contact.RightDistance))continue;
                var hip=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine");
                var shoulder=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine.003");
                Vector3 body=(hip.position+shoulder.position)*.5f,camera=CatActivityFacing.CameraPosition(cat);
                float view=CatActivityFacing.FacingDot(shoulder.position-hip.position,body,camera);
                minView=Mathf.Min(minView,view);maxView=Mathf.Max(maxView,view);visibleSamples++;
                Assert.That(view,Is.InRange(-.01f,.5f),"Actual scratching torso must be side-on: "+breeds.Get(i).Id);
                var ray=body-camera;
                Assert.That(Physics.RaycastAll(camera,ray.normalized,ray.magnitude-.02f,~0,QueryTriggerInteraction.Ignore)
                    .Any(hit=>hit.transform==scratch.transform||hit.transform.IsChildOf(scratch.transform)),Is.False,"The post must not hide the body centre");
            }
            Assert.That(scratch.IsRunning,Is.False);Assert.That(scratch.LeftStrokes,Is.GreaterThan(1),breeds.Get(i).Id+" left paw");
            Assert.That(scratch.RightStrokes,Is.GreaterThan(1),breeds.Get(i).Id+" right paw");
            Assert.That(visibleSamples,Is.GreaterThan(10));
            evidence.Add(breeds.Get(i).Id+",samples="+visibleSamples+",viewMin="+minView.ToString("F5")+",viewMax="+maxView.ToString("F5")+",left="+scratch.LeftStrokes+",right="+scratch.RightStrokes);
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(cat.IsMovementPhysicallyLocked,Is.False);Assert.That(controller.enabled,Is.True);
        }
        System.IO.File.WriteAllLines(System.IO.Path.Combine(UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA"),"scratch-side-contact.txt"),evidence);
    }
    [UnityTest]public IEnumerator Tunnel_EntersFromEitherEndAndExitsAtTheOppositeEnd()
    {
        yield return Prepare(HomeStoreService.PlayTunnelId);
        var cat=Object.FindAnyObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();
        var tunnel=Object.FindObjectsByType<CatEnrichmentActivity>(FindObjectsSortMode.None).First(a=>a.Mode==CatEnrichmentMode.Tunnel);
        foreach(bool reverse in new[]{false,true})
        {
            RoomPlayModeSupport.ProvisionNeeds();Vector3 start=reverse?tunnel.ExitPoint.position:tunnel.RoutineEntryPoint.position;
            Vector3 end=reverse?tunnel.RoutineEntryPoint.position:tunnel.ExitPoint.position;
            cc.enabled=false;cat.transform.position=start;cc.enabled=true;
            Assert.That(tunnel.DistanceTo(cat),Is.LessThan(tunnel.InteractionRadius));Assert.That(tunnel.TryStart(cat),Is.True);
            float until=Time.realtimeSinceStartup+12;while(tunnel.IsRunning && Time.realtimeSinceStartup<until){RoomPlayModeSupport.StopObservedRest(tunnel);yield return null;}
            Assert.That(tunnel.IsRunning,Is.False);Assert.That(Vector3.Distance(cat.transform.position,end),Is.LessThan(.12f));
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        }
    }
    [UnityTest]public IEnumerator Analog_WalksNormallyRunsWithEnergyAndStopsItsFeetAtAWall()
    {
        yield return Prepare(null);
        var cat=Object.FindAnyObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();
        var host=new GameObject("QA analog",typeof(RectTransform),typeof(MobileJoystick));var stick=host.GetComponent<MobileJoystick>();
        typeof(CatMovement).GetField("mobileJoystick",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(cat,stick);
        var direction=typeof(MobileJoystick).GetProperty("Direction");var energy=RoomPlayModeSupport.ProvisionNeeds();cat.ResolveSceneReferences();
        try
        {
            cc.enabled=false;cat.transform.position=new Vector3(-.5f,0,-1.4f);cc.enabled=true;
            Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position,.24f),Is.True,
                string.Join(",",Physics.OverlapSphere(cat.transform.position+Vector3.up*.25f,.24f).Select(c=>c.name)));
            direction.SetValue(stick,Vector2.right*.25f);yield return new WaitForSeconds(.3f);
            string detail="input="+stick.Direction+" position="+cat.transform.position+" grounded="+cc.isGrounded;
            foreach(var field in new[]{"hungerSpeedMultiplier","thirstSpeedMultiplier","energySpeedMultiplier","moveSpeed","verticalVelocity"})
                detail+=" "+field+"="+typeof(CatMovement).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(cat);
            yield return new WaitForSeconds(.25f);
            Assert.That(cat.GroundSpeed,Is.InRange(.64f,.86f),detail);Assert.That(cat.IsRunning,Is.False);
            direction.SetValue(stick,Vector2.right);yield return new WaitForSeconds(.2f);
            Assert.That(cat.GroundSpeed,Is.GreaterThan(1.5f));Assert.That(cat.IsRunning,Is.True);
            energy.ApplySavedValue(0);yield return new WaitForSeconds(.2f);
            Assert.That(cat.IsRunning,Is.False);Assert.That(cat.GroundSpeed,Is.LessThan(.9f));
            direction.SetValue(stick,Vector2.down);cc.enabled=false;cat.transform.position=new Vector3(0,0,-2.72f);cc.enabled=true;
            yield return new WaitForSeconds(.65f);
            Assert.That(cat.GroundSpeed,Is.LessThan(.03f));Assert.That(cat.GetComponentInChildren<Animator>().GetFloat("Speed"),Is.LessThan(.03f));
        }
        finally{Object.Destroy(host);}
    }
    [UnityTest]public IEnumerator MainBed_AllBreeds_RestOnTheNewCushion()
    {
        yield return Prepare(null);var cat=Object.FindFirstObjectByType<CatMovement>();
        var sleep=cat.GetComponent<SleepInteraction>();var cc=cat.GetComponent<CharacterController>();
        var entry=(Transform)typeof(SleepInteraction).GetField("bedInteractionPoint",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(sleep);
        var point=(Transform)typeof(SleepInteraction).GetField("sleepPoint",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(sleep);
        var report=new System.Text.StringBuilder("breed,minY,maxAbsX,maxAbsZ\n");var mesh=new Mesh();
        try
        {
            foreach(var breed in CatBreedCatalog.Load().Entries)
            {
                CatBreedService.Select(breed.Id);yield return null;yield return null;RoomPlayModeSupport.ProvisionNeeds();
                cc.enabled=false;cat.transform.position=entry.position;cc.enabled=true;
                Assert.That(sleep.TryHandleActionButton(),Is.True);Assert.That(sleep.IsSleeping,Is.True);
                yield return new WaitForSeconds(1.7f);yield return new WaitForEndOfFrame();
                var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();skin.BakeMesh(mesh,true);var vertices=mesh.vertices;
                float min=float.PositiveInfinity,x=0,z=0;
                foreach(int i in breed.ContactVertexIndices)
                {var p=skin.transform.TransformPoint(vertices[i])-point.position;min=Mathf.Min(min,p.y);x=Mathf.Max(x,Mathf.Abs(p.x));z=Mathf.Max(z,Mathf.Abs(p.z));}
                report.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3}",breed.Id,min,x,z));
                Assert.That(min,Is.InRange(-.035f,.055f),breed.Id+" cushion contact");
                var pad=point.GetComponent<CatActivitySurface>().Size;
                Assert.That(x,Is.LessThan(pad.y*.5f),breed.Id+" bed width");Assert.That(z,Is.LessThan(pad.x*.5f),breed.Id+" bed depth");
                Assert.That(sleep.TryHandleActionButton(),Is.True);yield return new WaitForSeconds(.25f);
            }
        }
        finally
        {
            if(sleep.IsSleeping)sleep.TryHandleActionButton();Object.Destroy(mesh);
            System.IO.File.WriteAllText("Docs/QA/REFERENCE_LIVING_2026-09-06/main-bed-contact.csv",report.ToString());
        }
    }

    [UnityTest]public IEnumerator SavedBedCorner_RestoresToTheFrontAndCanWalkAway()
    {
        yield return Prepare(null);
        var cat=Object.FindFirstObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();
        var host=new GameObject("QA corner analog",typeof(RectTransform),typeof(MobileJoystick));var stick=host.GetComponent<MobileJoystick>();
        typeof(CatMovement).GetField("mobileJoystick",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(cat,stick);
        var direction=typeof(MobileJoystick).GetProperty("Direction");
        try
        {
            foreach(float yaw in new[]{0f,90f,110.68f,180f,270f})
            foreach(var input in new[]{Vector2.right,new Vector2(-.5f,-1).normalized})
            {
                RoomPlayModeSupport.ProvisionNeeds();
                cat.ApplySavedWorldPose(new Vector3(-1.30f,.05f,2.47f),Quaternion.Euler(0,yaw,0));
                Assert.That(cat.transform.position.z,Is.LessThan(1.7f),"Old rear-corner saves must resume in front of the bed.");
                var start=cat.transform.position;direction.SetValue(stick,input);
                yield return new WaitForSeconds(1.2f);direction.SetValue(stick,Vector2.zero);
                var delta=cat.transform.position-start;delta.y=0;
                Assert.That(Vector3.Dot(delta,new Vector3(input.x,0,input.y)),Is.GreaterThan(.75f),"Saved corner yaw="+yaw+" input="+input+" end="+cat.transform.position);
                Assert.That(cat.IsMovementLocked,Is.False);
            }
        }
        finally{Object.Destroy(host);}
    }

    [UnityTest]public IEnumerator Bed_BlocksManualEntryAndRearPassage_AndRestoresSleepingSaves()
    {
        yield return Prepare(null);
        var cat=Object.FindFirstObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();
        var bed=Object.FindFirstObjectByType<CatBedObstacle>();var sleep=cat.GetComponent<SleepInteraction>();
        Assert.That(bed.Body.bounds.max.z,Is.EqualTo(2.80f).Within(.005f),"Bed must meet the back wall.");
        Assert.That(bed.transform.Find("PremiumCareVisual").GetComponentInChildren<Renderer>().bounds.max.z,Is.EqualTo(2.72f).Within(.005f),"Visible upholstery must touch the panelling.");
        var host=new GameObject("QA solid bed analog",typeof(RectTransform),typeof(MobileJoystick));var stick=host.GetComponent<MobileJoystick>();
        typeof(CatMovement).GetField("mobileJoystick",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(cat,stick);
        var direction=typeof(MobileJoystick).GetProperty("Direction");
        try
        {
            // Both sides of the rear seam and the open-looking front resist manual walking.
            foreach(int side in new[]{-1,0,1})
            {
                RoomPlayModeSupport.ProvisionNeeds();
                var start=side==0?bed.FrontExit.position:new Vector3(bed.transform.position.x+side*.98f,.05f,2.48f);
                var input=side==0?Vector2.up:Vector2.left*side;
                cc.enabled=false;cat.transform.SetPositionAndRotation(start,Quaternion.LookRotation(new Vector3(input.x,0,input.y)));cc.enabled=true;
                direction.SetValue(stick,input);yield return new WaitForSeconds(.85f);direction.SetValue(stick,Vector2.zero);
                if(side==0)Assert.That(cat.transform.position.z,Is.LessThan(bed.Body.bounds.min.z-.18f));
                else Assert.That((cat.transform.position.x-bed.transform.position.x)*side,Is.GreaterThan(.74f),"Cannot enter behind the bed from side "+side);
            }
            foreach(var old in new[]{bed.transform.position,bed.transform.position+Vector3.forward*.28f,new Vector3(-1.3f,.05f,2.47f)})
            {cat.ApplySavedWorldPose(old,Quaternion.identity);Assert.That(Vector3.Distance(cat.transform.position,bed.FrontExit.position),Is.LessThan(.02f));}
            var clear=new Vector3(0,0,-2);cat.ApplySavedWorldPose(clear,Quaternion.identity);
            Assert.That(cat.transform.position,Is.EqualTo(clear),"Valid saved positions stay unchanged.");
            // A legitimate sleeping save stays on its pad through collection refresh.
            RoomPlayModeSupport.ProvisionNeeds();Assert.That(sleep.TryRestoreSleepingState(out var reason),Is.True,reason);
            yield return null;var rest=cat.transform.position;
            CatRoomArrangement.Request(cat.gameObject.scene).Invalidate();yield return null;yield return null;
            Assert.That(sleep.IsSleeping,Is.True);Assert.That(cat.transform.position,Is.EqualTo(rest));
            Assert.That(sleep.TryHandleActionButton(),Is.True);yield return null;
            Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position,.30f),Is.True);Assert.That(cat.IsMovementLocked,Is.False);
        }
        finally{if(sleep.IsSleeping)sleep.TryHandleActionButton();Object.Destroy(host);}
    }

    [UnityTest]public IEnumerator SelectedTelevision_RequiresProximityAndRechecksAtClickTime()
    {
        yield return Prepare(null);
        var cat=Object.FindFirstObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();
        var tv=CatActivity.Registered.First(a=>a.Kind==CatActivityKind.TelevisionWatch);
        var host=new GameObject("QA action prompt");var prompt=host.AddComponent<ActivityPromptController>();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var selected=typeof(ActivityPromptController).GetField("selected",flags);
        var candidate=typeof(ActivityPromptController).GetField("candidate",flags);
        var find=typeof(ActivityPromptController).GetMethod("FindNearestCandidate",flags);
        try
        {
            cat.ApplySavedWorldPose(new Vector3(-.6f,.05f,2.2f),Quaternion.identity);
            Assert.That(tv.DistanceTo(cat),Is.GreaterThan(tv.InteractionRadius+1));
            selected.SetValue(prompt,tv);candidate.SetValue(prompt,tv);
            Assert.That(find.Invoke(prompt,null),Is.Not.SameAs(tv));Assert.That(selected.GetValue(prompt),Is.Null);
            cc.enabled=false;cat.transform.position=tv.RoutineEntryPoint.position;cc.enabled=true;
            selected.SetValue(prompt,tv);Assert.That(find.Invoke(prompt,null),Is.SameAs(tv));
            candidate.SetValue(prompt,tv);var energy=RoomPlayModeSupport.ProvisionNeeds();float before=energy.CurrentEnergy;
            cat.ApplySavedWorldPose(new Vector3(1,0,-2),Quaternion.identity);
            typeof(ActivityPromptController).GetMethod("HandleAction",flags).Invoke(prompt,null);
            Assert.That(tv.IsRunning,Is.False);Assert.That(energy.CurrentEnergy,Is.EqualTo(before));
        }
        finally{Object.Destroy(host);}
    }

    [UnityTest]public IEnumerator Tunnel_StopsManualMovementAtBothSidesAndMouths_AndStaysSolidAfterCancel()
    {
        yield return Prepare(HomeStoreService.PlayTunnelId);
        var cat=Object.FindFirstObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();
        var tunnel=CatActivity.Registered.OfType<CatEnrichmentActivity>().First(a=>a.Mode==CatEnrichmentMode.Tunnel);
        tunnel.transform.SetPositionAndRotation(new Vector3(-1,0,-.7f),Quaternion.identity);Physics.SyncTransforms();
        var host=new GameObject("QA tunnel analog",typeof(RectTransform),typeof(MobileJoystick));var stick=host.GetComponent<MobileJoystick>();
        typeof(CatMovement).GetField("mobileJoystick",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(cat,stick);
        var direction=typeof(MobileJoystick).GetProperty("Direction");
        try
        {
            foreach(var side in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
            {
                RoomPlayModeSupport.ProvisionNeeds();cc.enabled=false;
                cat.transform.SetPositionAndRotation(tunnel.transform.position+side*.95f,Quaternion.LookRotation(-side));cc.enabled=true;
                direction.SetValue(stick,new Vector2(-side.x,-side.z));yield return new WaitForSeconds(.8f);
                direction.SetValue(stick,Vector2.zero);
                float distance=Vector3.Dot(cat.transform.position-tunnel.transform.position,side);
                Assert.That(distance,Is.GreaterThan(side.x!=0?.46f:.53f),"Ghosting through tunnel from "+side);
                Assert.That(cat.GroundSpeed,Is.LessThan(.05f),"Feet must stop at the cloth.");
            }
            cc.enabled=false;cat.transform.position=tunnel.RoutineEntryPoint.position;cc.enabled=true;
            Assert.That(tunnel.TryStart(cat),Is.True);yield return new WaitForSeconds(.2f);
            Assert.That(cc.enabled,Is.False);tunnel.enabled=false;yield return null;
            Assert.That(cc.enabled,Is.True);
            Assert.That(tunnel.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger),Is.True);
            Assert.That(CatActivityMotion.IsFloorClear(tunnel.transform.position,.25f),Is.False);
        }
        finally{Object.Destroy(host);}
    }

    [UnityTest]public IEnumerator ProductPicking_SeesToysAndFurnitureButHonorsVisibleOcclusion()
    {
        yield return Prepare(HomeStoreService.PlayTunnelId);
        var camera=Camera.main;var tunnel=CatActivity.Registered.First(a=>a.Kind==CatActivityKind.TunnelPlay);
        var cat=Object.FindFirstObjectByType<CatMovement>();cat.GetComponent<CharacterController>().enabled=false;
        cat.transform.position=new Vector3(0,0,-2.5f);Physics.SyncTransforms();
        var point=camera.WorldToScreenPoint(tunnel.transform.position+Vector3.up*.3f);
        Assert.That(ActivityPromptController.PickWorldActivity(camera,point),Is.SameAs(tunnel),"Invisible boundary must not block the tunnel.");
        var sofa=CatActivity.Registered.OfType<LivingFurnitureActivity>().First(a=>a.Kind==CatActivityKind.SofaLounge);
        var cushion=sofa.SelectionVisual.GetComponentsInChildren<Renderer>().First();
        Assert.That(ActivityPromptController.PickWorldActivity(camera,camera.WorldToScreenPoint(cushion.bounds.center)),Is.SameAs(sofa));
        var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            var ray=camera.ScreenPointToRay(point);obstacle.transform.position=ray.GetPoint(3);
            obstacle.transform.localScale=Vector3.one*.4f;Physics.SyncTransforms();
            Assert.That(ActivityPromptController.PickWorldActivity(camera,point),Is.Null,"Visible geometry occludes the toy.");
            obstacle.GetComponent<Renderer>().enabled=false;
            Assert.That(ActivityPromptController.PickWorldActivity(camera,point),Is.SameAs(tunnel));
            Assert.That(HomeStoreService.TrySetStored(HomeStoreService.PlayTunnelId,true),Is.True);yield return null;
            Assert.That(ActivityPromptController.PickWorldActivity(camera,point),Is.Not.SameAs(tunnel),"Stored toys cannot be picked.");
        }
        finally{Object.Destroy(obstacle);}
    }
}
