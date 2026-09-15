using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class KitchenBedroomSupportedMotionTests
{
    HomeStoreSaveState store;
    string breed;
    float timeScale, capture;
    CatMovement cat;
    readonly List<string> evidence = new List<string>();
    readonly List<string> failures = new List<string>();
    static string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Temp/SupportedFurniture");
    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True);
        store = HomeStoreService.CaptureState(); breed = CatBreedService.SelectedBreedId;
        timeScale = Time.timeScale; capture = Time.captureDeltaTime;
        Time.timeScale = 1; Time.captureFramerate = 60;
    }
    [TearDown] public void After()
    {
        if (cat != null) CatActionState.CancelForTransition(cat);
        RoomPlayModeSupport.ReleaseRoom(); HomeStoreService.ApplySavedState(store); CatBreedService.Select(breed);
        Time.timeScale = timeScale; Time.captureDeltaTime = capture;
        Directory.CreateDirectory(Output); File.WriteAllLines(Output + "/" + TestContext.CurrentContext.Test.Name + ".csv", evidence);
    }
    IEnumerator Prepare(string scene, IReadOnlyList<string> products)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(scene);
        var state = HomeStoreSaveState.CreateDefault(); state.ownedProductIds = products.ToArray();
        HomeStoreService.ApplySavedState(state); yield return null; yield return null;
        var celebration = Object.FindAnyObjectByType<HomeLevelUpCelebrationView>();
        if (celebration != null && HomeLevelUpCelebrationView.IsAnyOpen)
        {
            typeof(HomeLevelUpCelebrationView).GetMethod("BeginClose", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(celebration, null);
            yield return new WaitForSecondsRealtime(.3f);
        }
        cat = Object.FindAnyObjectByType<CatMovement>(); cat.GetComponent<CatIdleBehavior>().enabled = false;
    }
    void Start(CatActivity activity)
    {
        CatActionState.CancelForTransition(cat);
        var cc = cat.GetComponent<CharacterController>(); cc.enabled = false;
        Vector3 point = activity.RoutineEntryPoint.position; point.y = 0;
        cat.transform.SetPositionAndRotation(point, Quaternion.identity); cc.enabled = true;
        Physics.SyncTransforms(); RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(activity.TryStart(cat), Is.True, activity.StoreProductId);
    }
    static Transform Surface(CatActivity a) => a is PerchNapActivity perch ? perch.PerchPoint :
        a is CanopyNapActivity canopy ? canopy.NestPoint :
        a is PantryClimbActivity pantry ? pantry.UpperShelfPoint :
        (Transform)a.GetType().GetField("baskPoint", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(a);

    void CheckPaws(CatActivity a, Transform support, string identity)
    {
        var feet = new[] { "DEF-hand.L", "DEF-hand.R", "DEF-foot.L", "DEF-foot.R" }
            .Select(n => CatBreedVisualFactory.FindDescendant(cat.transform, n)).ToArray();
        var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>();
        var mesh = new Mesh(); skin.BakeMesh(mesh, true);
        var soles = feet.Select(f => f.position.y).ToArray();
        foreach (var v in mesh.vertices)
        {
            Vector3 p = skin.transform.TransformPoint(v); int nearest = -1; float best = .085f * .085f;
            for (int i = 0; i < feet.Length; i++)
            {
                // Long-haired paw bones sit farther above the sole than the
                // short-haired radius. Eating uses the same downward column
                // as CatFeedingAlignmentBuilder, not a sphere around the bone.
                Vector3 delta = p - feet[i].position;
                float d = delta.y <= 0f
                    ? delta.x * delta.x + delta.z * delta.z : delta.sqrMagnitude;
                if (d < best) { best = d; nearest = i; }
            }
            if (nearest >= 0) soles[nearest] = Mathf.Min(soles[nearest], p.y);
        }
        Object.Destroy(mesh);
        var missing = new List<string>();
        for (int i = 0; i < feet.Length; i++)
        {
            // A paw spans a patch; a single bone ray can hit the sloped edge of an embroidered motif.
            var hits = (from x in new[]{-.025f,0f,.025f} from z in new[]{-.025f,0f,.025f}
                from hit in Physics.RaycastAll(feet[i].position + new Vector3(x,.10f,z), Vector3.down,.35f,~0,QueryTriggerInteraction.Ignore) select hit).ToArray();
            bool supported = hits.Any(h => h.normal.y >= .60f && Mathf.Abs(h.point.y - support.position.y) < .035f &&
                Mathf.Abs(soles[i] - h.point.y) < (a.Kind == CatActivityKind.VanityStoolNap ? .045f : .04f) && (support.position.y < .12f || h.collider.transform.IsChildOf(a.transform)));
            if (!supported) missing.Add(identity + " " + feet[i].name + " unsupported at " + feet[i].position.ToString("F4") + " sole=" + soles[i].ToString("F4") + " hits=" + string.Join(";",hits.Select(h=>h.collider.name+":"+h.point.y.ToString("F3")+"/"+h.normal.y.ToString("F2"))));
        }
        // The vendor Sleeping pose curls one Persian forepaw above the other
        // supports. A seated cat still requires all four; sleeping retains
        // three contacts and a bounded natural curl instead of forced leg IK.
        bool sleeping = cat.GetComponent<CatActivityAnimation>().CurrentPose == CatActivityPose.Sleep;
        bool naturalCurl = sleeping && missing.Count <= 1 && soles.All(y => y - support.position.y >= -.015f && y - support.position.y < .07f);
        if (!naturalCurl) failures.AddRange(missing);
    }

    [UnityTest] public IEnumerator KitchenRestAndClimb_TenBreeds_KeepRealPawSupportAndNaturalJumpClips()
    {
        yield return Prepare("Kitchen_Level01", HomeStoreService.KitchenCollection);
        foreach (var b in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(b.Id); yield return null; yield return null;
            foreach (var id in new[] { HomeStoreService.KitchenIslandId, HomeStoreService.KitchenCounterStoolId,
                HomeStoreService.KitchenPantryShelfId, HomeStoreService.KitchenStoveOvenId })
                yield return CheckRoutine(id, b.Id);
        }
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }
    [UnityTest] public IEnumerator BedroomYarn_TenBreedsAndFrameRates_PlayRealBall()
    {
        yield return Prepare("Bedroom_Level01", HomeStoreService.BedroomCollection);
        var ball = CatActivity.Registered.OfType<BallChaseActivity>().Single(a => a.Kind == CatActivityKind.YarnSwat);
        foreach (var b in CatBreedCatalog.Load().Entries)
        foreach (int rate in b.Id == "oriental-shorthair" ? new[] {15,30,60} : new[] {30})
        {
            CatBreedService.Select(b.Id); yield return null; yield return null; Time.captureFramerate = rate;
            int events=0, taps=0, moving=0; Action<CatActivity> handler=a=>{if(a==ball)events++;}; CatActivity.Completed+=handler;
            Start(ball); float deadline=Time.realtimeSinceStartup+24;
            try
            {
                while(ball.IsRunning && Time.realtimeSinceStartup<deadline)
                {
                    yield return new WaitForEndOfFrame(); var pose=cat.GetComponent<CatActivityAnimation>().CurrentPose;
                    if(pose==CatActivityPose.Walk)moving++;
                    if(pose==CatActivityPose.BatLeft || pose==CatActivityPose.BatRight)
                    {
                        taps++; var hips=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine");
                        var shoulders=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine.003");
                        Assert.That(CatActivityFacing.FacingDot(shoulders.position-hips.position,hips.position,CatActivityFacing.CameraPosition(cat)),Is.GreaterThanOrEqualTo(-.01f));
                    }
                }
                Assert.That(events,Is.EqualTo(1),b.Id+"/"+rate+" "+ball.InterruptedReason);
                Assert.That(ball.CatchCount,Is.EqualTo(3)); Assert.That(ball.RolledDistance,Is.GreaterThan(1.05f));
                Assert.That(ball.LastHitDistance,Is.LessThan(.095f)); Assert.That(moving>2&&taps>2,Is.True);
                Assert.That(ball.Ball.gameObject.activeSelf,Is.False);
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True);
                evidence.Add(b.Id+","+rate+","+ball.CatchCount+","+ball.RolledDistance+","+ball.LastHitDistance);
            }
            finally {CatActivity.Completed-=handler;}
        }
    }

    [UnityTest] public IEnumerator BedroomYarn_PauseCancelAndReplay_ReleaseControl()
    {
        yield return Prepare("Bedroom_Level01",HomeStoreService.BedroomCollection);
        var ball=CatActivity.Registered.OfType<BallChaseActivity>().Single(a=>a.Kind==CatActivityKind.YarnSwat);
        for(int pass=0;pass<2;pass++)
        {
            Start(ball); float deadline=Time.realtimeSinceStartup+12;
            while(ball.CatchCount<1&&ball.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(ball.CatchCount,Is.GreaterThan(0)); Time.timeScale=0;
            var position=ball.Ball.position;var catPosition=cat.transform.position;
            for(int f=0;f<6;f++)yield return null;
            Assert.That(Vector3.Distance(ball.Ball.position,position),Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(cat.transform.position,catPosition),Is.LessThan(.0001f));
            ball.CancelForTransition();Time.timeScale=1;yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(ball.Ball.gameObject.activeSelf,Is.False);
            Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True);
        }
    }

    [UnityTest] public IEnumerator BedroomDaybed_TenBreeds_SleepOnSupportedCushion()
    {
        yield return Prepare("Bedroom_Level01",HomeStoreService.BedroomCollection);
        foreach(var b in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(b.Id);yield return null;yield return null;
            yield return CheckRoutine(HomeStoreService.BedroomWindowDaybedId,b.Id);
        }
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }

    [UnityTest] public IEnumerator BedroomDaybed_CancelDuringJumpAndRest_ReleasesToFloor()
    {
        yield return Prepare("Bedroom_Level01",HomeStoreService.BedroomCollection);
        var a=CatActivity.Registered.OfType<PerchNapActivity>().Single(v=>v.Kind==CatActivityKind.DaybedWatch);
        for(int phase=0;phase<2;phase++)
        {
            Start(a);float deadline=Time.realtimeSinceStartup+12;
            while(a.IsRunning&&Time.realtimeSinceStartup<deadline)
            {
                yield return new WaitForEndOfFrame();
                var pose=cat.GetComponent<CatActivityAnimation>().CurrentPose;
                if(phase==0&&pose==CatActivityPose.TowelJumpUp&&cat.transform.position.y>.12f)break;
                if(phase==1&&pose==CatActivityPose.Sleep&&a.IsWaitingForRestStop)break;
            }
            Assert.That(a.IsRunning,Is.True);Time.timeScale=0;yield return null;
            var position=cat.transform.position;for(int f=0;f<6;f++)yield return null;
            Assert.That(Vector3.Distance(cat.transform.position,position),Is.LessThan(.0001f));
            a.CancelForTransition();Time.timeScale=1;yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True);
            Assert.That(cat.GetComponent<CharacterController>().enabled,Is.True);
        }
    }

    [UnityTest] public IEnumerator BedroomRest_TenBreeds_KeepSupportAndNaturalTransitions()
    {
        yield return Prepare("Bedroom_Level01", HomeStoreService.BedroomCollection);
        foreach (var b in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(b.Id); yield return null; yield return null;
            foreach (string id in new[] { HomeStoreService.BedroomQueenBedId, HomeStoreService.BedroomVanityStoolId, HomeStoreService.BedroomStarCanopyId })
                yield return CheckRoutine(id, b.Id);
        }
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }
    [UnityTest,Timeout(240000)] public IEnumerator NarrowPerches_TenBreeds_KeepAllPawsOnTheRealSurface()
    {
        foreach(var pair in new[]{new[]{"Bedroom_Level01",HomeStoreService.BedroomStarCanopyId},new[]{"Balcony_Level01",HomeStoreService.BalconyHerbShelfId},new[]{"SecondFloor_Level01","loft.tall-bookcase"}})
        {
            yield return Prepare(pair[0],HomeStoreService.GetRoomCollection(HomeRoomService.Rooms.Single(r=>r.SceneName==pair[0]).Id));
            foreach(var b in CatBreedCatalog.Load().Entries){CatBreedService.Select(b.Id);yield return null;yield return null;yield return CheckRoutine(pair[1],b.Id);}
        }
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }

    IEnumerator CheckRoutine(string id, string breedId)
    {
        var a = CatActivity.Registered.Single(v => v.StoreProductId == id); var surface = Surface(a);
        int events = 0; Action<CatActivity> handler = v => { if (v == a) events++; }; CatActivity.Completed += handler;
        var scale = cat.transform.localScale; var parent = cat.transform.parent; int up = 0, down = 0, held = 0;
        bool checkedPaws = false; Vector3 last = cat.transform.position; float idleWalk = 0;
        Start(a); float deadline = Time.realtimeSinceStartup + 22f;
        try
        {
            while (a.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame(); if (!a.IsRunning) break;
                var pose = cat.GetComponent<CatActivityAnimation>().CurrentPose;
                if (pose == CatActivityPose.TowelJumpUp) up++;
                if (pose == CatActivityPose.TowelJumpDown) down++;
                if (pose == CatActivityPose.Walk && Vector3.Distance(last, cat.transform.position) < .0001f) idleWalk += Time.deltaTime;
                last = cat.transform.position;
                Assert.That(cat.transform.localScale, Is.EqualTo(scale), id + " root scale");
                var currentSupport=cat.GetComponent<CatActivityAnimation>().ContactSurface??surface;
                if ((pose == CatActivityPose.Sleep || pose == CatActivityPose.Sit || (id==HomeStoreService.BalconyHerbShelfId && pose==CatActivityPose.GentleKnead && !(cat.GetComponent<CatSurfaceTurnMotion>()?.IsTurning??false))) && Vector3.Distance(cat.transform.position, currentSupport.position) < .06f)
                {
                    held++;
                    if (held == 35)
                    {
                        CheckPaws(a, currentSupport, breedId + "/" + id); checkedPaws = true;
                        var hips = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-spine");
                        var shoulders = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-spine.003");
                        if (CatActivityFacing.FacingDot(shoulders.position - hips.position, hips.position, CatActivityFacing.CameraPosition(cat)) < -.01f)
                            failures.Add(breedId + "/" + id + " hidden torso");
                    }
                    if (a.IsWaitingForRestStop && held > 40) a.RequestRestStop();
                }
            }
            string row = breedId + "," + id + "," + events + "," + up + "," + down + "," + held + "," + idleWalk;
            evidence.Add(row);
            Assert.That(a.IsRunning, Is.False, row); Assert.That(events, Is.EqualTo(1), row);
            Assert.That(checkedPaws, Is.True, row);
            if (surface.position.y > .12f) Assert.That(up > 5 && down > 5, Is.True, "Native up/down clips: " + row);
            Assert.That(idleWalk, Is.LessThan(.15f), "Stationary Walk: " + row);
            Assert.That(cat.transform.parent, Is.SameAs(parent));
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(cat.GetComponent<CharacterController>().enabled && !cat.IsMovementPhysicallyLocked, Is.True);
            Assert.That(CatActivityMotion.IsControllerFloorClear(cat, cat.transform.position), Is.True, row);
        }
        finally { CatActivity.Completed -= handler; }
    }

    [UnityTest] public IEnumerator KitchenMeal_TenBreeds_ReachRealKibbleWithPlantedPaws()
    {
        yield return Prepare("Kitchen_Level01", HomeStoreService.KitchenCollection);
        var meal = CatActivity.Registered.OfType<MealTimeActivity>().Single();
        var floor = new GameObject("Meal ground proof").transform;
        try
        {
            foreach (var b in CatBreedCatalog.Load().Entries)
            {
                CatBreedService.Select(b.Id); yield return null; yield return null;
                int events = 0; Action<CatActivity> handler = a => { if (a == meal) events++; }; CatActivity.Completed += handler;
                Start(meal); int samples = 0; float nearest = 999f, maximum = 0; Vector3 stand = Vector3.zero;
                float deadline = Time.realtimeSinceStartup + 18f;
                try
                {
                    while (meal.IsRunning && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame(); if (!meal.IsRunning || !meal.IsEating) continue;
                        samples++; if (samples < 30) continue;
                        var head = cat.GetComponent<CatMealHeadMotion>();
                        nearest = Mathf.Min(nearest, head.Distance); maximum = Mathf.Max(maximum, head.Distance);
                        Assert.That(head.PawPlantError, Is.LessThan(.001f), b.Id);
                        if (samples == 35) { stand = cat.transform.position; floor.position = new Vector3(stand.x, 0, stand.z); CheckPaws(meal, floor, b.Id + "/meal"); }
                        if (samples > 35) Assert.That(Vector3.Distance(cat.transform.position, stand), Is.LessThan(.001f), "The eating body must not slide.");
                    }
                    string row = b.Id + ",meal," + samples + "," + nearest + "," + maximum + "," + events; evidence.Add(row);
                    Assert.That(meal.IsRunning, Is.False, row); Assert.That(events, Is.EqualTo(1), row);
                    Assert.That(samples, Is.GreaterThan(70), row); Assert.That(maximum, Is.LessThan(.045f), row);
                    Assert.That(nearest, Is.LessThan(.045f), row);
                    yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                    Assert.That(CatActivityMotion.IsControllerFloorClear(cat, cat.transform.position), Is.True, row);
                }
                finally { CatActivity.Completed -= handler; }
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }
        finally { Object.Destroy(floor.gameObject); }
    }

    [UnityTest] public IEnumerator BedroomGlass_TenBreeds_TouchBeforeTheFall_AndRestoreOnCancel()
    {
        yield return Prepare("Bedroom_Level01", HomeStoreService.BedroomCollection);
        var knock = CatActivity.Registered.OfType<KnockOffActivity>().Single();
        Vector3 glassHome = knock.GlassPivot.localPosition;
        foreach (var b in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(b.Id); yield return null; yield return null;
            int events=0; Action<CatActivity> handler=a=>{if(a==knock)events++;}; CatActivity.Completed+=handler;
            bool support=false; Start(knock); float deadline=Time.realtimeSinceStartup+22f;
            try
            {
                while(knock.IsRunning && Time.realtimeSinceStartup<deadline)
                {
                    yield return new WaitForEndOfFrame();
                    if(!support && knock.IsOnNightstand && !knock.IsTapping && knock.ContactStrokes>0 && cat.GetComponent<CatActivityAnimation>().CurrentPose==CatActivityPose.GentleKnead)
                    {CheckPaws(knock,knock.PerchPoint,b.Id+"/glass");support=true;}
                }
                Assert.That(knock.ContactStrokes,Is.EqualTo(3),b.Id+" real glass contact, min="+knock.MinimumPawDistance);
                Assert.That(events,Is.EqualTo(1),b.Id); Assert.That(support,Is.True,b.Id);
                Assert.That(Vector3.Distance(knock.GlassPivot.localPosition,glassHome),Is.LessThan(.001f),b.Id);
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True,b.Id);
                evidence.Add(b.Id+",glass,"+knock.ContactStrokes+","+events);
            }
            finally {CatActivity.Completed-=handler;}
        }
        Start(knock); float timeout=Time.realtimeSinceStartup+12f;
        while(!knock.IsOnNightstand && knock.IsRunning && Time.realtimeSinceStartup<timeout)yield return null;
        Assert.That(knock.IsOnNightstand,Is.True);Time.timeScale=0;
        Vector3 paused=cat.transform.position;
        for(int i=0;i<6;i++)yield return null;
        Assert.That(cat.transform.position,Is.EqualTo(paused));Time.timeScale=1;knock.CancelForTransition();
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(Vector3.Distance(knock.GlassPivot.localPosition,glassHome),Is.LessThan(.001f));
        Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True);
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }

    [UnityTest] public IEnumerator KitchenSink_TenBreeds_ReachWaterOnFourSupportedPaws()
    {
        yield return Prepare("Kitchen_Level01", HomeStoreService.KitchenCollection);
        var sink = CatActivity.Registered.OfType<SinkSipActivity>().Single();
        foreach (var b in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(b.Id); yield return null; yield return null; Start(sink);
            int samples = 0; float deadline = Time.realtimeSinceStartup + 18f;
            while (sink.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame(); if (!sink.IsRunning || !sink.IsSipping) continue;
                samples++; if (samples != 45 && samples != 90) continue;
                var sample = sink.EditorCaptureContactDiagnostic();
                Assert.That(sample.paws.All(p => p.supported && p.surfaceWithin35mmOfPerch), Is.True, b.Id + " actual sink supports");
                Assert.That(sample.closestJawSurfaceToWater, Is.LessThan(.045f), b.Id + " actual jaw surface/water, same contact limit as the bathroom");
            }
            Assert.That(sink.IsRunning, Is.False, b.Id); Assert.That(samples, Is.GreaterThan(90), b.Id);
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(CatActivityMotion.IsControllerFloorClear(cat, cat.transform.position), Is.True, b.Id);
            evidence.Add(b.Id + ",sink," + samples);
        }
    }
}
