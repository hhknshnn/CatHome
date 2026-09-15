using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ProductionCareTests
{
    private HomeStoreSaveState store;private string breed;private bool sound;
    [SetUp]public void Before(){store=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;sound=HomeAudioService.SoundEnabled;}
    [TearDown]public void After()
    {if(CatActivity.Active!=null)CatActivity.Active.enabled=false;Time.timeScale=1;HomeAudioService.SoundEnabled=sound;RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(store);CatBreedService.Select(breed);}
    private IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        var state=HomeStoreSaveState.CreateDefault();state.ownedProductIds=HomeStoreService.Products.Where(p=>HomeStoreService.IsLivingRoomCollectionProduct(p.Id)).Select(p=>p.Id).ToArray();HomeStoreService.ApplySavedState(state);yield return null;
        yield return RoomPlayModeSupport.WaitForMovementRelease(Object.FindFirstObjectByType<CatMovement>());
    }
    [UnityTest]public IEnumerator PairedCareTray_BlocksNarrowPocketsAndRestoresOldSaves()
    {
        yield return Prepare();var cat=Object.FindFirstObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();var station=Object.FindFirstObjectByType<CatCareStationObstacle>();Assert.That(station,Is.Not.Null);
        cat.enabled=false;
        Vector3 open=station.FrontExit.position-station.Body.bounds.center;open.y=0;open.Normalize();
        foreach(float x in new[]{-.34f,-.12f,.12f,.34f})foreach(float yaw in new[]{0f,90f,180f,270f})
        {
            Vector3 inside=station.Body.transform.TransformPoint(new Vector3(x,.05f,0));
            cat.ApplySavedWorldPose(inside,Quaternion.Euler(0,yaw,0));
            Assert.That(Vector3.Distance(cat.transform.position,station.FrontExit.position),Is.LessThan(.02f));
            Assert.That(Quaternion.Angle(cat.transform.rotation,Quaternion.Euler(0,yaw,0)),Is.LessThan(.1f),"Restore must preserve the saved heading");
            Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position,.28f),Is.True,"Legacy care pose must be in the open aisle");
            Vector3 outside=station.Body.transform.TransformPoint(new Vector3(0,.05f,.95f));
            // The collection now has a coffee table beside the old opposite
            // point. Pick actual open room floor, never a goal inside that prop.
            if (!CatActivityMotion.IsControllerFloorClear(cat,outside))
            {
                var clearGoals = new[] { new Vector3(-.55f,.05f,-.65f), new Vector3(-1.1f,.05f,-1.3f), new Vector3(0,.05f,-1.4f) }
                    .Where(p=>CatActivityMotion.IsControllerFloorClear(cat,p)).ToArray();
                Assert.That(clearGoals,Is.Not.Empty,"No actual open room exit remains beside the care assembly.");
                outside = clearGoals[0];
            }
            Assert.That(CatActivityMotion.IsControllerFloorClear(cat,outside),Is.True,"The exit goal must be real open room floor.");
            Assert.That(CatActivityMotion.TryFloorPath(cat,cat.transform.position,outside,out var route),Is.True,
                $"The open care side must connect around the solid tray to the room; start={cat.transform.position:F5}; end={outside:F5}; " +
                $"radius={CatActivityMotion.ControllerFloorRadius(cat):F5}; startClear={CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position)}; endClear={CatActivityMotion.IsControllerFloorClear(cat,outside)}");
            foreach(Vector3 raw in route)
            {
                Vector3 target=raw;target.y=.05f;
                Vector3 heading=target-cat.transform.position;heading.y=0;
                if(heading.sqrMagnitude>.0001f)cat.transform.rotation=Quaternion.LookRotation(heading);
                for(int step=0;step<100&&Vector2.Distance(new Vector2(cat.transform.position.x,cat.transform.position.z),new Vector2(target.x,target.z))>.02f;step++)
                {
                    Vector3 direction=target-cat.transform.position;direction.y=0;
                    cc.Move(Vector3.ClampMagnitude(direction,.05f)+Vector3.down*.02f);
                }
                Assert.That(Vector2.Distance(new Vector2(cat.transform.position.x,cat.transform.position.z),new Vector2(target.x,target.z)),Is.LessThan(.04f),
                    $"The actual controller must traverse each clear side-route segment; target={target:F5}; root={cat.transform.position:F5}; " +
                    $"capsuleCentre={cat.transform.TransformPoint(cc.center):F5}; bottom={cc.bounds.min.y:F5}; radius={cc.radius:F5}; scale={cat.transform.lossyScale:F5}; " +
                    $"route={string.Join(";",route.Select(p=>p.ToString("F4")))}");
            }
            Assert.That(Vector3.Distance(cat.transform.position,station.FrontExit.position),Is.GreaterThan(.45f),"Can leave restored care pose");
            foreach(Vector3 raw in route.AsEnumerable().Reverse().Concat(new[]{station.FrontExit.position}))
            {
                Vector3 target=raw;target.y=.05f;
                Vector3 heading=target-cat.transform.position;heading.y=0;
                if(heading.sqrMagnitude>.0001f)cat.transform.rotation=Quaternion.LookRotation(heading);
                for(int step=0;step<100&&Vector2.Distance(new Vector2(cat.transform.position.x,cat.transform.position.z),new Vector2(target.x,target.z))>.02f;step++)
                {
                    Vector3 direction=target-cat.transform.position;direction.y=0;
                    cc.Move(Vector3.ClampMagnitude(direction,.05f)+Vector3.down*.02f);
                }
                Assert.That(Vector2.Distance(new Vector2(cat.transform.position.x,cat.transform.position.z),new Vector2(target.x,target.z)),Is.LessThan(.04f),
                    $"The actual controller must also enter the open care side from the room; target={target:F5}; root={cat.transform.position:F5}; " +
                    $"capsuleCentre={cat.transform.TransformPoint(cc.center):F5}; bottom={cc.bounds.min.y:F5}");
            }
            // The controller centre is biased toward the cat's head. As in
            // normal locomotion, face the tray before walking into its edge;
            // pushing a saved rear-facing pose backwards measures another end.
            cat.transform.rotation=Quaternion.LookRotation(-open);
            Physics.SyncTransforms();
            for(int step=0;step<25;step++)cc.Move(-open*.05f+Vector3.down*.02f);
            // Test the actual controller against the assembly's closed bowl
            // pockets; the .15 m margin must not be weakened to admit a climb.
            Vector3 local=station.Body.transform.InverseTransformPoint(cat.transform.position);
            Assert.That(local.z,Is.LessThan(station.Body.center.z-station.Body.size.z*.5f-.15f),
                $"The controller cannot climb onto the paired tray; root={cat.transform.position:F5}; local={local:F5}; " +
                $"capsuleBottom={cc.bounds.min.y:F5}; capsuleCentre={cat.transform.TransformPoint(cc.center):F5}; " +
                $"radius={cc.radius:F5}; step={cc.stepOffset:F5}; skin={cc.skinWidth:F5}; scale={cat.transform.lossyScale:F5}; " +
                $"trayTop={station.Body.bounds.max.y:F5}; savedX={x:F2}; savedYaw={yaw:F0}");
            var stopped=cat.transform.position;
            Assert.That(CatActivityMotion.TryFloorPath(cat,stopped,outside,out _),Is.True,"Every blocked bowl approach must retain a clear exit route");
        }
    }
    [UnityTest]public IEnumerator CompanionCommands_AllBreeds_HoldUntilStoppedAndKeepGroundContact()
    {
        yield return Prepare();var cat=Object.FindFirstObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();var command=cat.GetComponent<CatCommandActivity>();var mesh=new Mesh();
        Time.timeScale=4;
        foreach(var entry in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(entry.Id);yield return null;yield return null;
            foreach(var kind in new[]{CatCompanionCommand.Sit,CatCompanionCommand.Loaf})
            {
                cc.enabled=false;cat.transform.SetPositionAndRotation(new Vector3(-.5f,.05f,-1.4f),Quaternion.Euler(0,180,0));cc.enabled=true;
                Assert.That(command.Issue(kind),Is.True,entry.Id+" "+kind);
                yield return new WaitForSeconds(20f);
                Assert.That(command.IsRunning&&command.IsWaitingForRestStop,Is.True,"No automatic end after twenty seconds");
                Assert.That(command.Issue(CatCompanionCommand.Meow),Is.False,"Busy cat must reject another command");
                yield return new WaitForEndOfFrame();var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();skin.BakeMesh(mesh,true);var vertices=mesh.vertices;
                float min=float.PositiveInfinity,max=float.NegativeInfinity;
                foreach(int i in entry.ContactVertexIndices){float y=skin.transform.TransformPoint(vertices[i]).y-cat.transform.position.y;min=Mathf.Min(min,y);max=Mathf.Max(max,y);}
                Assert.That(min,Is.InRange(-.012f,.035f),entry.Id+" "+kind+" contact");
                Assert.That(max,Is.InRange(.16f,1f),entry.Id+" unsquashed body");
                Assert.That(command.RequestRestStop(),Is.True);yield return new WaitForSeconds(1.5f);
                Assert.That(command.IsRunning,Is.False);Assert.That(cat.IsMovementPhysicallyLocked,Is.False);Assert.That(cc.enabled,Is.True);
            }
        }
        Object.Destroy(mesh);
    }
    [UnityTest]public IEnumerator CatVoice_UsesAuthoredAssetsAndHonoursMuteAndCancel()
    {
        yield return Prepare();var cat=Object.FindFirstObjectByType<CatMovement>();var cc=cat.GetComponent<CharacterController>();cc.enabled=false;cat.transform.position=new Vector3(-.5f,.05f,-1.4f);cc.enabled=true;
        var voice=CatVoice.EnsureOn(cat);var command=cat.GetComponent<CatCommandActivity>();HomeAudioService.SoundEnabled=true;
        Assert.That(command.Kind,Is.EqualTo(CatActivityKind.CompanionCommand),"A runtime command must not inherit a toy's default activity identity");
        foreach(var cue in new[]{AudioCue.Meow,AudioCue.Purr,AudioCue.Eat,AudioCue.Drink})Assert.That(GameAudio.Clip(cue).length,Is.GreaterThan(.3f));
        Assert.That(voice.Meow(),Is.True);Assert.That(voice.Meow(),Is.False,"Repeated taps cannot stack the call");
        Assert.That(command.Issue(CatCompanionCommand.Loaf),Is.True);yield return new WaitForSeconds(1.5f);
        Assert.That(voice.PlayingLoop,Is.EqualTo("Purr_1"));HomeAudioService.SoundEnabled=false;yield return null;
        Assert.That(voice.PlayingLoop,Is.Empty);Assert.That(voice.IsVocalizing,Is.False);
        command.enabled=false;HomeAudioService.SoundEnabled=true;yield return new WaitForSeconds(.3f);Assert.That(voice.PlayingLoop,Is.Empty);
    }
}
