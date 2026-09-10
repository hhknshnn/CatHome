using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class HomeContactFacingTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    HomeStoreSaveState savedStore;
    string savedBreed;
    float savedScale, savedCaptureDelta;
    CatMovement cat;
    BowlInteraction bowls;
    SleepInteraction sleep;
    HungerSystem hunger;
    ThirstSystem thirst;
    GameObject needsHost, buttonHost;
    Button button;

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True);
        savedStore = HomeStoreService.CaptureState(); savedBreed = CatBreedService.SelectedBreedId;
        savedScale = Time.timeScale; savedCaptureDelta = Time.captureDeltaTime;
        Time.timeScale = 1; Time.captureFramerate = 60;
    }
    [TearDown] public void After()
    {
        if (cat != null) CatActionState.CancelForTransition(cat);
        if (buttonHost != null) Object.DestroyImmediate(buttonHost);
        if (needsHost != null) Object.DestroyImmediate(needsHost);
        RoomPlayModeSupport.ReleaseRoom(); HomeStoreService.ApplySavedState(savedStore);
        CatBreedService.Select(savedBreed); Time.timeScale = savedScale; Time.captureDeltaTime = savedCaptureDelta;
    }
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    IEnumerator PrepareHome(string sceneName = "LivingRoom_Level01", IEnumerable<string> roomProducts = null)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(sceneName);
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = roomProducts != null ? roomProducts.ToArray() : HomeStoreService.Products.Where(p => HomeStoreService.IsLivingRoomCollectionProduct(p.Id) || CatCollectionPolicy.IsCatItem(p.Id)).Select(p => p.Id).ToArray();
        state.storedProductIds = state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
        HomeStoreService.ApplySavedState(state); yield return null; yield return null;
        cat = Object.FindAnyObjectByType<CatMovement>(); bowls = cat.GetComponent<BowlInteraction>(); sleep = cat.GetComponent<SleepInteraction>();
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
        foreach (var hint in Object.FindObjectsByType<PetTutorialHint>(FindObjectsInactive.Include, FindObjectsSortMode.None)) hint.enabled = false;
        needsHost = new GameObject("Contact facing QA needs");
        hunger = Object.FindAnyObjectByType<HungerSystem>() ?? needsHost.AddComponent<HungerSystem>();
        thirst = Object.FindAnyObjectByType<ThirstSystem>() ?? needsHost.AddComponent<ThirstSystem>();
        Set(bowls, "hungerSystem", hunger); Set(bowls, "thirstSystem", thirst);
        buttonHost = new GameObject("ActionButton", typeof(RectTransform), typeof(Button)); button = buttonHost.GetComponent<Button>();
        Set(bowls, "interactionButton", button); bowls.ResolveSceneReferences();
    }
    void Move(Vector3 point)
    {
        CatActionState.CancelForTransition(cat); point.y = .05f;
        cat.ApplySavedWorldPose(point, Quaternion.identity); Physics.SyncTransforms();
        RoomPlayModeSupport.ProvisionNeeds(); hunger.ApplySavedValue(35); thirst.ApplySavedValue(35);
    }
    void AssertFacing(string context)
    {
        var bones = cat.GetComponentsInChildren<Transform>();
        Vector3 forward = bones.Single(b => b.name == "DEF-spine.003").position - bones.Single(b => b.name == "DEF-spine").position;
        Assert.That(CatActivityFacing.FacingDot(forward, cat.transform.position, CatActivityFacing.CameraPosition(cat)),
            Is.GreaterThanOrEqualTo(CatActivityFacing.MinimumViewDot), context + " actual torso must be front/side-facing");
    }
    static float HorizontalDistance(Vector3 a, Vector3 b) { Vector3 delta = a - b; delta.y = 0; return delta.magnitude; }

    [UnityTest] public IEnumerator KitchenMeal_TenBreeds_KeepTheAuthoredBowlReach_OnAVisibleClearSide()
    {
        yield return PrepareHome("Kitchen_Level01", HomeStoreService.KitchenCollection);
        var meal = CatActivity.Registered.OfType<MealTimeActivity>().Single();
        Transform stand = Field<Transform>(meal, "standPoint"), bowl = Field<Transform>(meal, "bowlPoint");
        foreach (var breed in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(breed.Id); yield return null; yield return null;
            Move(meal.RoutineEntryPoint.position);
            Assert.That(meal.TryGetPromptDistance(cat, out _), Is.True);
            Assert.That(meal.TryStart(cat), Is.True, breed.Id);
            float deadline = Time.realtimeSinceStartup + 15f; int samples = 0;
            while (meal.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                if (!meal.IsRunning || cat.GetComponent<CatActivityAnimation>().CurrentPose != CatActivityPose.Eat) continue;
                AssertFacing(breed.Id + " kitchen meal"); samples++;
                Assert.That(HorizontalDistance(cat.transform.position, bowl.position),
                    Is.InRange(HorizontalDistance(stand.position, bowl.position) - .10f,
                        HorizontalDistance(stand.position, bowl.position) + .025f), "The existing small eating dip must still reach its bowl.");
                Vector3 inward = bowl.position - cat.transform.position; inward.y = 0f;
                Assert.That(Vector3.Dot(cat.transform.forward, inward.normalized), Is.GreaterThan(.98f));
            }
            Assert.That(meal.IsRunning, Is.False); Assert.That(samples, Is.GreaterThan(0));
            Assert.That(hunger.CurrentHunger, Is.GreaterThan(65f), "The real completed meal restores hunger once.");
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position, .24f), Is.True);
        }
    }

    [UnityTest] public IEnumerator VanitySip_TenBreeds_KeepTheirSupportedPerch_AndPointIntoTheActualWaterTarget()
    {
        foreach (var setup in new[] {
            ("Bathroom_Level01", HomeStoreService.BathroomCollection, HomeStoreService.BathroomVanityId),
            ("Kitchen_Level01", HomeStoreService.KitchenCollection, HomeStoreService.KitchenSinkCabinetId),
            ("Garden_Level01", HomeStoreService.GardenCollection, HomeStoreService.GardenBirdBathId),
            ("Patio_Level01", HomeStoreService.PatioCollection, HomeStoreService.PatioWaterFountainId) })
        {
        yield return PrepareHome(setup.Item1, setup.Item2);
        var sip = CatActivity.Registered.OfType<SinkSipActivity>().Single(a => a.StoreProductId == setup.Item3);
        Assert.That(sip.SipTarget, Is.Not.Null, setup.Item3 + " real model water binding");
        Transform perch = Field<Transform>(sip, "perchPoint"), floor = Field<Transform>(sip, "floorPoint");
        foreach (var breed in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(breed.Id); yield return null; yield return null;
            Move(sip.RoutineEntryPoint.position);
            Assert.That(sip.TryGetPromptDistance(cat, out _), Is.True);
            Assert.That(sip.TryStart(cat), Is.True, breed.Id);
            float deadline = Time.realtimeSinceStartup + 15f; int samples = 0;
            while (sip.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                if (!sip.IsRunning || cat.GetComponent<CatActivityAnimation>().CurrentPose != CatActivityPose.Drink) continue;
                samples++;
                Assert.That(HorizontalDistance(cat.transform.position, perch.position), Is.LessThan(.015f), "Keep the root on its authored supported perch.");
                Vector3 inward = sip.SipTarget != null ? sip.SipTarget.position - perch.position : perch.position - floor.position;
                inward.y = 0f; Vector3 forward = cat.transform.forward; forward.y = 0f;
                Assert.That(Vector3.Dot(forward.normalized, inward.normalized), Is.GreaterThan(.98f), "Face the water; never rotate the mouth away merely for the camera.");
                AssertFacing(breed.Id + " " + setup.Item3 + " water contact");
                if (samples == 24)
                {
                    System.IO.Directory.CreateDirectory("Library/SinkSipFacing");
                    System.IO.File.WriteAllText("Library/SinkSipFacing/last-regression-contact.json",JsonUtility.ToJson(sip.EditorCaptureContactDiagnostic(),true));
                    foreach (string name in new[] { "DEF-hand.L", "DEF-hand.R", "DEF-foot.L", "DEF-foot.R" })
                    {
                        Transform paw = cat.GetComponentsInChildren<Transform>().Single(t => t.name == name);
                        bool supported = Physics.RaycastAll(paw.position + Vector3.up * .10f, Vector3.down, .55f,
                            ~0, QueryTriggerInteraction.Ignore).Any(hit => hit.transform.IsChildOf(sip.transform) && hit.normal.y > .5f);
                        Assert.That(supported, Is.True, breed.Id + " " + setup.Item3 + " real support beneath " + name);
                    }
                }
            }
            Assert.That(sip.IsRunning, Is.False); Assert.That(samples, Is.GreaterThan(0));
            Assert.That(thirst.CurrentThirst, Is.GreaterThan(65f));
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position, .24f), Is.True);
        }
        }
    }

    [System.Serializable] sealed class SinkContactProbe
    {
        public List<SinkSipActivity.ContactDiagnostic> samples=new List<SinkSipActivity.ContactDiagnostic>();
        public List<string> failures=new List<string>();
    }

    [UnityTest] public IEnumerator SinkSip_FourProducts_ReportActualFeetAndWaterBeforeSupportAssertions()
    {
        var report=new SinkContactProbe();
        const string path="Library/SinkSipFacing/native-contact-probe.json";
        System.IO.Directory.CreateDirectory("Library/SinkSipFacing");
        try
        {
            foreach(var setup in new[] {
                ("Bathroom_Level01",HomeStoreService.BathroomCollection,HomeStoreService.BathroomVanityId),
                ("Kitchen_Level01",HomeStoreService.KitchenCollection,HomeStoreService.KitchenSinkCabinetId),
                ("Garden_Level01",HomeStoreService.GardenCollection,HomeStoreService.GardenBirdBathId),
                ("Patio_Level01",HomeStoreService.PatioCollection,HomeStoreService.PatioWaterFountainId) })
            {
                yield return PrepareHome(setup.Item1,setup.Item2);
                CatBreedService.Select(CatBreedCatalog.DefaultBreedId);yield return null;yield return null;
                var sip=CatActivity.Registered.OfType<SinkSipActivity>().Single(a=>a.StoreProductId==setup.Item3);
                Move(sip.RoutineEntryPoint.position);
                if(!sip.TryStart(cat)){report.failures.Add(setup.Item3+" could not start at its real entry");continue;}
                int frame=0,captured=0;float deadline=Time.realtimeSinceStartup+15f;
                while(sip.IsRunning&&Time.realtimeSinceStartup<deadline)
                {
                    yield return new WaitForEndOfFrame();
                    if(!sip.IsSipping||cat.GetComponent<CatActivityAnimation>().CurrentPose!=CatActivityPose.Drink)continue;
                    frame++;if(frame!=24&&frame!=60&&frame!=108)continue;
                    var sample=sip.EditorCaptureContactDiagnostic();report.samples.Add(sample);captured++;
                    foreach(var paw in sample.paws)
                        if(!paw.supported||!paw.surfaceWithin35mmOfPerch)
                            report.failures.Add(setup.Item3+" frame "+frame+" "+paw.name+" actual support missing; product="+paw.inProduct.ToString("F5")+" support="+paw.inSupport.ToString("F5"));
                    System.IO.File.WriteAllText(path,JsonUtility.ToJson(report,true));
                }
                if(captured!=3)report.failures.Add(setup.Item3+" expected 3 real Drinking samples; got "+captured);
                if(sip.IsRunning){report.failures.Add(setup.Item3+" routine timed out");sip.CancelForTransition();}
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            }
        }
        finally{System.IO.File.WriteAllText(path,JsonUtility.ToJson(report,true));}
        Assert.That(report.failures,Is.Empty,"All four products measured before assertions; "+path+"\n"+string.Join("\n",report.failures));
    }

    [UnityTest] public IEnumerator SinkHeadReach_UsesActualMouth_PreservesPawsAndPause_AndReleasesItsOwner()
    {
        yield return PrepareHome("Kitchen_Level01",HomeStoreService.KitchenCollection);
        var sip=CatActivity.Registered.OfType<SinkSipActivity>().Single(a=>a.StoreProductId==HomeStoreService.KitchenSinkCabinetId);
        Move(sip.RoutineEntryPoint.position);Assert.That(sip.TryStart(cat),Is.True);
        float deadline=Time.realtimeSinceStartup+12f;
        while(!sip.IsSipping&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(sip.IsSipping,Is.True);yield return new WaitForSeconds(.45f);yield return new WaitForEndOfFrame();
        var reach=cat.GetComponent<CatSipHeadMotion>();Assert.That(reach.IsActive,Is.True);
        Assert.That(cat.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.isReadable,Is.False,"Keep the modern GPU mesh's CPU copy released during real drinking.");
        Assert.That(reach.MouthVertexCount,Is.InRange(1,24));Assert.That(reach.TotalDeflection,Is.LessThanOrEqualTo(CatSipHeadMotion.MaximumTotalDeflection+.01f));
        var before=sip.EditorCaptureContactDiagnostic();
        System.IO.Directory.CreateDirectory("Library/SinkSipFacing");
        System.IO.File.WriteAllText("Library/SinkSipFacing/head-reach-contact.json",JsonUtility.ToJson(before,true));
        Assert.That(before.closestJawSurfaceToWater,Is.LessThan(.045f),"The actual skinned mouth surface must reach real basin water.");
        Assert.That(reach.ForequarterDeflection,Is.LessThanOrEqualTo(CatSipHeadMotion.MaximumForequarterDeflection+.05f));
        Assert.That(reach.PawPlantError,Is.LessThan(.0001f),"Both front paws retain their actual source support points.");
        foreach(var paw in before.paws)
            Assert.That(paw.supported&&paw.surfaceWithin35mmOfPerch,Is.True,paw.name+" must remain over real furniture mesh.");
        Vector3 root=cat.transform.position,scale=cat.transform.localScale;
        Time.timeScale=0;
        try
        {
            for(int i=0;i<4;i++)yield return new WaitForEndOfFrame();
            var paused=sip.EditorCaptureContactDiagnostic();
            Assert.That(Vector3.Distance(before.closestJawSurfaceWorld,paused.closestJawSurfaceWorld),Is.LessThan(.0005f));
            Assert.That(Vector3.Distance(root,cat.transform.position),Is.LessThan(.00001f));
            Assert.That(cat.transform.localScale,Is.EqualTo(scale));
            foreach(var paw in paused.paws)
                Assert.That(Vector3.Distance(paw.world,before.paws.Single(p=>p.name==paw.name).world),Is.LessThan(.0005f),paw.name+" must remain planted while paused");
            reach.enabled=false;
            var restored=sip.EditorCaptureContactDiagnostic();
            foreach(var paw in restored.paws)
                Assert.That(Vector3.Distance(paw.world,paused.paws.Single(p=>p.name==paw.name).world),Is.LessThan(.0001f),paw.name+" must keep the same support point with and without the forequarter correction");
            Assert.That(reach.IsActive,Is.False);
            var bones=cat.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("DEF-",System.StringComparison.Ordinal)).ToArray();
            var sourcePositions=bones.Select(t=>t.localPosition).ToArray();
            var sourceScales=bones.Select(t=>t.localScale).ToArray();
            var sourceRotations=bones.Select(t=>t.localRotation).ToArray();
            reach.enabled=true;
            Assert.That(reach.Begin(sip,sip.SipTarget),Is.True);reach.Sample(sip,1f);yield return new WaitForEndOfFrame();
            for(int i=0;i<bones.Length;i++)
            {
                Assert.That(bones[i].localPosition,Is.EqualTo(sourcePositions[i]),bones[i].name+" bone attachment must not move");
                Assert.That(bones[i].localScale,Is.EqualTo(sourceScales[i]),bones[i].name+" bone length/scale must not change");
            }
            sip.CancelForTransition();Assert.That(reach.IsActive,Is.False);
            for(int i=0;i<bones.Length;i++)
                Assert.That(Quaternion.Angle(bones[i].localRotation,sourceRotations[i]),Is.LessThan(.06f),bones[i].name+" cancellation restores the true source local pose");
        }
        finally{Time.timeScale=1;sip.CancelForTransition();}
        Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
        Assert.That(cat.GetComponent<CharacterController>().enabled,Is.True);
    }

    [UnityTest] public IEnumerator MissingSipMouthProfile_CancelsWithoutWaterRewardOrRetainedControl()
    {
        yield return PrepareHome("Kitchen_Level01",HomeStoreService.KitchenCollection);
        var sip=CatActivity.Registered.OfType<SinkSipActivity>().Single(a=>a.StoreProductId==HomeStoreService.KitchenSinkCabinetId);
        Move(sip.RoutineEntryPoint.position);float need=thirst.CurrentThirst;
        var cache=typeof(CatSipMouthCatalog).GetField("cached",BindingFlags.Static|BindingFlags.NonPublic);
        var original=CatSipMouthCatalog.Load();var missing=ScriptableObject.CreateInstance<CatSipMouthCatalog>();
        try
        {
            cache.SetValue(null,missing);Assert.That(sip.TryStart(cat),Is.True);
            float deadline=Time.realtimeSinceStartup+12f;
            while(sip.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(sip.IsRunning,Is.False,"A failed real mouth binding must cancel its owner.");
            Assert.That(sip.IsSipping,Is.False);Assert.That(thirst.CurrentThirst,Is.LessThanOrEqualTo(need+.01f));
            Assert.That(cat.IsMovementPhysicallyLocked,Is.False);Assert.That(cat.GetComponent<CharacterController>().enabled,Is.True);
            Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position,.24f),Is.True);
        }
        finally{cache.SetValue(null,original);Object.DestroyImmediate(missing);sip.CancelForTransition();}
    }

    [UnityTest] public IEnumerator MainBowls_TenBreeds_PreserveMuzzleReachFromAClearVisibleSide_AndCancelRecovery()
    {
        yield return PrepareHome();
        foreach (var breed in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(breed.Id); yield return null; yield return null;
            foreach (string kind in new[] { "food", "water" })
            {
                var setup = Field<BowlInteraction.BowlSetup>(bowls, kind); setup.Fill();
                Move(setup.InteractionPoint.position);
                typeof(BowlInteraction).GetMethod("Update", Private).Invoke(bowls, null);
                Assert.That(Field<BowlInteraction.BowlSetup>(bowls, "currentBowl"), Is.SameAs(setup));
                button.onClick.Invoke(); Assert.That(bowls.IsInteracting, Is.True, breed.Id + " " + kind);
                float deadline = Time.realtimeSinceStartup + 12f;
                while (bowls.IsInteracting && bowls.ActiveCareSound == null && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(bowls.ActiveCareSound, Is.EqualTo(kind == "water" ? "CatDrink" : "CatEat"), breed.Id + " " + kind + " real recovery must start");
                yield return new WaitForSeconds(.35f); yield return new WaitForEndOfFrame();
                AssertFacing(breed.Id + " " + kind);
                var bodyBones=cat.GetComponentsInChildren<Transform>();
                Vector3 hips=bodyBones.Single(t=>t.name=="DEF-spine").position;
                Vector3 shoulders=bodyBones.Single(t=>t.name=="DEF-spine.003").position;
                Assert.That(CatActivityFacing.FacingDot(shoulders-hips,(hips+shoulders)*.5f,CatActivityFacing.CameraPosition(cat)),
                    Is.GreaterThanOrEqualTo(CatActivityFacing.MinimumViewDot),breed.Id+" "+kind+" actual body midpoint");
                Assert.That(HorizontalDistance(cat.transform.position, setup.Bowl.position),
                    Is.EqualTo(HorizontalDistance(setup.FeedingPoint.position, setup.Bowl.position)).Within(.01f), "Reach the measured feeding pose after the clear walking entry.");
                Vector3 inward = setup.Bowl.position - cat.transform.position; inward.y = 0;
                Assert.That(Vector3.Dot(cat.transform.forward, inward.normalized), Is.GreaterThan(.98f), "Keep the mouth aimed into its own bowl.");
                Assert.That(CatActivityMotion.IsControllerFloorClear(cat,setup.InteractionPoint.position), Is.True, "Keep the approach and exit clear for the full walking capsule.");
                Assert.That(cat.GetComponent<CatSipHeadMotion>().Distance,Is.LessThan(.035f),"Actual mouth contact replaces the former distant standing radius.");
                bowls.CancelInteraction(); float need = kind == "water" ? thirst.CurrentThirst : hunger.CurrentHunger;
                yield return new WaitForSeconds(.15f);
                Assert.That(kind == "water" ? thirst.IsDrinking : hunger.IsEating, Is.False);
                Assert.That(kind == "water" ? thirst.CurrentThirst : hunger.CurrentHunger, Is.LessThanOrEqualTo(need + .01f));
                Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            }
        }
    }

    [UnityTest] public IEnumerator MainBed_TenBreeds_RestoresOnTheSupportedVisibleAxis_WithoutExtraSleepEvents()
    {
        yield return PrepareHome();
        int started = 0; System.Action onStarted = () => started++; sleep.SleepStarted += onStarted;
        var sample = new Mesh(); var vertices = new List<Vector3>();
        try
        {
            foreach (var breed in CatBreedCatalog.Load().Entries)
            {
                CatBreedService.Select(breed.Id); yield return null; yield return null;
                Move(Field<Transform>(sleep, "bedInteractionPoint").position);
                Assert.That(sleep.TryRestoreSleepingState(out string reason), Is.True, breed.Id + " " + reason);
                yield return null; yield return new WaitForEndOfFrame();
                AssertFacing(breed.Id + " sleeping save");
                var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>(); skin.BakeMesh(sample, true); sample.GetVertices(vertices);
                float minimum = float.PositiveInfinity;
                foreach (int index in breed.ContactVertexIndices)
                    minimum = Mathf.Min(minimum, skin.transform.TransformPoint(vertices[index]).y - sleep.SleepSurface.position.y);
                Assert.That(minimum, Is.InRange(-.035f, .055f), breed.Id + " preserve mattress contact");
                Assert.That(started, Is.Zero, "Loading sleep must never count as a new sleep action.");
                Assert.That(sleep.TryHandleActionButton(), Is.True); Assert.That(sleep.IsSleeping, Is.False);
                Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
                Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
                Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position, .24f), Is.True);
            }
        }
        finally { sleep.SleepStarted -= onStarted; Object.DestroyImmediate(sample); }
    }

    [UnityTest] public IEnumerator ToysAndScratch_FromEachNearbySide_KeepRealContactsOnAVisibleWorkingSide()
    {
        yield return PrepareHome();
        foreach (string id in new[] { HomeStoreService.FeatherToyId, HomeStoreService.ToyMouseId, HomeStoreService.ScratchPostId })
        {
            Assert.That(HomeStoreService.TrySetStored(id, false), Is.True); yield return null; yield return null;
            CatActivity activity = CatActivity.Registered.Single(a => a.StoreProductId == id);
            Vector3 original = activity.transform.position;
            try
            {
                // An open-floor fixture isolates side selection from room layout.
                activity.transform.position = new Vector3(0, 0, -1.3f); Physics.SyncTransforms();
                var renderers = activity.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r is not LineRenderer && r is not ParticleSystemRenderer).ToArray();
                Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                for (int side = 0; side < 4; side++)
                {
                    Vector3 outward = Quaternion.Euler(0, side * 90, 0) * Vector3.forward;
                    Vector3 near = bounds.ClosestPoint(bounds.center + outward * 4) + outward * .36f;
                    Move(near + outward * 1.2f); Assert.That(activity.TryGetPromptDistance(cat, out _), Is.False,id+" side "+side+" distant prompt");
                    Move(near); bool prompted=activity.TryGetPromptDistance(cat,out float promptDistance);
                    Assert.That(prompted, Is.True,id+" side "+side+" near prompt; requested="+near.ToString("F4")+
                        " actual="+cat.transform.position.ToString("F4")+" distance="+promptDistance+" unlocked="+activity.IsUnlocked+
                        " floorClear="+CatActivityMotion.IsFloorClear(cat.transform.position)+" worldBlocked="+cat.AreWorldActionsBlocked+
                        " geometry="+PromptGeometryEvidence(activity,cat));
                    Assert.That(activity.TryStart(cat), Is.True, id + " side " + side);
                    float deadline = Time.realtimeSinceStartup + 14f; int sampled = 0;
                    while (activity.IsRunning && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame();
                        int contacts = activity is CatEnrichmentActivity toy ? toy.ContactCount : ((ScratchPostActivity)activity).LeftStrokes + ((ScratchPostActivity)activity).RightStrokes;
                        bool actualWork = activity is CatEnrichmentActivity enrichment ? enrichment.IsPerformingGesture :
                            cat.GetComponent<CatActivityAnimation>().CurrentPose == CatActivityPose.Scratch;
                        if (contacts == 0 || !activity.IsRunning || !actualWork) continue;
                        AssertFacing(id + " side " + side); sampled++;
                    }
                    Assert.That(activity.IsRunning, Is.False); Assert.That(sampled, Is.GreaterThan(0), id + " real contact must remain visible");
                }
            }
            finally { activity.CancelForTransition(); activity.transform.position = original; HomeStoreService.TrySetStored(id, true); }
        }
    }

    static string PromptGeometryEvidence(CatActivity activity,CatMovement actor)
    {
        var placement=activity.GetComponent<HomeProductPlacement>();
        Transform visual=placement!=null?placement.MovableRoot:null;
        if(visual==null)return "No MovableRoot";
        Vector3 from=actor.transform.position;from.y=0;Vector3 edge=from;
        float nearest=float.PositiveInfinity;Bounds bounds=default;bool found=false;
        foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            if(!renderer.enabled||!renderer.gameObject.activeInHierarchy||renderer is LineRenderer||renderer is ParticleSystemRenderer)continue;
            if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);
            Vector3 probe=new Vector3(from.x,renderer.bounds.center.y,from.z);
            Vector3 point=renderer.transform.TransformPoint(renderer.localBounds.ClosestPoint(renderer.transform.InverseTransformPoint(probe)));point.y=0;
            float square=(point-from).sqrMagnitude;if(square>=nearest)continue;nearest=square;edge=point;
        }
        Vector3 sightFrom=from+Vector3.up*.40f,sightTo=edge+Vector3.up*Mathf.Clamp(bounds.center.y,.16f,.80f);
        Vector3 ray=sightTo-sightFrom;
        var hits=Physics.RaycastAll(sightFrom,ray.normalized,ray.magnitude,~0,QueryTriggerInteraction.Ignore);
        return "visual="+visual.name+" found="+found+" edge="+edge.ToString("F4")+" nearest="+Mathf.Sqrt(nearest)+
            " sight="+string.Join(";",hits.Select(h=>h.transform.name+" own="+h.transform.IsChildOf(visual)+" actor="+h.transform.IsChildOf(actor.transform)));
    }
}
