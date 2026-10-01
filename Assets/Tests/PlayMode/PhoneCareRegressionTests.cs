#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class PhoneCareRegressionTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    CareAlignmentPolishTests home;
    static string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Docs/QA/OVERNIGHT_FIX_POLISH_2026-09-27");
    static T Read<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
    static object Call(object owner, string name, params object[] values) => owner.GetType().GetMethod(name, Private).Invoke(owner, values);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
    CatMovement Cat => Read<CatMovement>(home, "cat");
    BowlInteraction Bowls => Read<BowlInteraction>(home, "bowls");

    [SetUp] public void Before() { home = new CareAlignmentPolishTests(); home.Before(); }
    [TearDown] public void After() { home?.After(); }

    [UnityTest, Timeout(240000)]
    public IEnumerator CareClinicalViews_ThreeBreeds_RealFoodAndWater()
    {
        yield return (IEnumerator)Call(home,"Home");Time.captureFramerate=20;
        var hidden=new Dictionary<Renderer,bool>();
        try
        {
            foreach(var renderer in Object.FindObjectsByType<Renderer>())
                if(!renderer.transform.IsChildOf(Cat.transform)&&renderer.bounds.max.y>.30f)
                {hidden.Add(renderer,renderer.forceRenderingOff);renderer.forceRenderingOff=true;}
            foreach(string breed in new[]{"persian","domestic-longhair","maine-coon"})
            {
                yield return (IEnumerator)Call(home,"Breed",breed);
                foreach(string kind in new[]{"food","water"})
                {
                    Call(home,"ReadyNeeds");var setup=Read<BowlInteraction.BowlSetup>(Bowls,kind);setup.Fill();
                    Read<BowlInteraction.BowlSetup>(Bowls,kind=="food"?"water":"food").Empty();var target=setup.ContactPoint;
                    Call(home,"FindCareStance",target,setup.InteractionPoint,(Func<bool>)(()=>CatMealHeadMotion.TryPrepareCareStart(Cat,target,out _)));
                    var camera=Camera.main;camera.orthographic=true;camera.orthographicSize=.55f;
                    Vector3 centre=Cat.transform.position+Vector3.up*.20f;
                    camera.transform.position=centre+Cat.transform.right*2f+Vector3.up*.85f+Cat.transform.forward*.20f;camera.transform.LookAt(centre);
                    Call(Bowls,"Update");Read<Button>(Bowls,"interactionButton").onClick.Invoke();Assert.That(Bowls.IsInteracting,Is.True);
                    for(int frame=0;frame<290&&Bowls.IsInteracting;frame++)
                    {
                        yield return new WaitForEndOfFrame();
                        if(frame==3||frame==7||frame==20||frame==60||frame==150||frame==215)
                        {
                            var texture=ScreenCapture.CaptureScreenshotAsTexture();
                            try{File.WriteAllBytes(Path.Combine(Output,"clinical-"+breed+"-"+kind+"-"+frame+".png"),texture.EncodeToPNG());}
                            finally{Object.Destroy(texture);}
                        }
                    }
                    Assert.That(Bowls.IsInteracting,Is.False);CatActionState.CancelForTransition(Cat);
                }
            }
        }
        finally{foreach(var pair in hidden)if(pair.Key!=null)pair.Key.forceRenderingOff=pair.Value;}
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator Maine_CareSourceDiagnostic()
    {
        yield return (IEnumerator)Call(home, "Home");
        yield return (IEnumerator)Call(home, "Breed", "maine-coon");
        Time.captureFramerate = 20;
        var rows = new List<string> { "radius,frame,oldBroad,active,contact,clear,distance,weight,lean,paw,shoulder,vertex,collider,depth,pointY,floorY,rootY" };
        var setup=Read<BowlInteraction.BowlSetup>(Bowls,"food");var target=setup.ContactPoint;
        Vector3 outward=setup.InteractionPoint.position-target.position;outward.y=0;outward.Normalize();
        foreach(float radius in new[]{.30f,.34f,.38f,.42f})
        {
            Call(home,"ReadyNeeds");setup.Fill();Read<BowlInteraction.BowlSetup>(Bowls,"water").Empty();
            Call(home,"Place",target.position+outward*radius,Quaternion.LookRotation(-outward));
            var animator=Cat.GetComponentInChildren<Animator>();animator.Play("Idle",0,.43f);animator.Update(0);
            yield return null;
            bool oldBroad=Cat.IsInteractionPoseClear(Cat.transform.position,Cat.transform.rotation);
            if(!CatMealHeadMotion.TryPrepareCareStart(Cat,target,out _)){rows.Add(Csv(radius,-1,oldBroad,false));continue;}
            Call(Bowls,"Update");Read<Button>(Bowls,"interactionButton").onClick.Invoke();
            for(int frame=0;frame<260&&Bowls.IsInteracting;frame++)
            {
                yield return new WaitForEndOfFrame();
                var head=Cat.GetComponent<CatMealHeadMotion>();var clearance=Read<CatCareSkinClearance>(head,"careClearance");
                rows.Add(Csv(radius,frame,oldBroad,Bowls.IsInteracting,Bowls.IsAtContact,head.CareFrameClear,head.Distance,Read<float>(head,"weight"),head.BodyLeanDistance,head.PawPlantError,head.ForequarterDeflection,
                    clearance?.LastRejectedSourceVertex,clearance?.LastRejectedCollider?.name,clearance?.LastMaximumDepth,clearance?.LastRejectedPoint.y,
                    clearance!=null?Read<float>(clearance,"floorY"):0,Cat.transform.position.y));
            }
            CatActionState.CancelForTransition(Cat);for(int i=0;i<10;i++)yield return null;
        }
        File.WriteAllLines(Path.Combine(Output,"maine-care-source.csv"),rows);
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator Longhair_AdmissionDiagnostic()
    {
        yield return (IEnumerator)Call(home, "Home");
        yield return (IEnumerator)Call(home, "Breed", "domestic-longhair");
        var setup = Read<BowlInteraction.BowlSetup>(Bowls, "food"); var target = setup.ContactPoint;
        var profile = CatFeedingAlignmentCatalog.Load().Find("domestic-longhair");
        Vector3 outward = setup.InteractionPoint.position - target.position; outward.y = 0; outward.Normalize();
        var rows = new List<string> { "phase,radius,angle,zone,facing,broad,standing,accepted,vertex,collider,body,controller,witness" };
        foreach (float phase in new[] { 0f, .25f, .5f, .75f })
        {
            var animator = Cat.GetComponentInChildren<Animator>(); animator.Play("Idle", 0, phase); animator.Update(0);
            foreach (float radius in new[] { .26f, .30f, .34f, .38f, .42f, .46f, .5f })
            foreach (float angle in new[] { 0f, -30f, 30f, -60f, 60f, 90f, 180f })
            {
                Vector3 side = Quaternion.Euler(0, angle, 0) * outward;
                Call(home, "Place", target.position + side * radius, Quaternion.LookRotation(-side));
                Vector3 centre = target.position - Cat.transform.rotation * profile.mouthOffset; centre.y = Cat.transform.position.y;
                float zone = Vector3.Distance(Cat.transform.position, centre);
                bool broad = Cat.IsInteractionPoseClear(Cat.transform.position, Cat.transform.rotation);
                bool accepted = CatMealHeadMotion.TryPrepareCareStart(Cat, target, out _);
                var head = Cat.GetComponent<CatMealHeadMotion>() ?? Cat.gameObject.AddComponent<CatMealHeadMotion>();
                bool standing = (bool)Call(head, "IsStandingSkinClear", Cat, target, false);
                var checks = Read<Array>(head, "standingChecks"); int vertex = -2; string collider = "";
                foreach (var check in checks)
                {
                    if (check == null) continue;
                    var flags = BindingFlags.Public | BindingFlags.Instance;
                    if ((Transform)check.GetType().GetField("target", flags).GetValue(check) != target) continue;
                    var clearance = (CatCareSkinClearance)check.GetType().GetField("clearance", flags).GetValue(check);
                    vertex = clearance.LastRejectedSourceVertex;
                    collider = clearance.LastRejectedCollider != null ? clearance.LastRejectedCollider.name : "floor:" + clearance.LastRejectedPoint.y.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                }
                var guard = Read<CatBodyGuard>(Cat, "bodyGuard");
                bool body = guard.IsPoseClear(Cat.transform.position, Cat.transform.rotation);
                bool controller = guard.IsControllerClear(Cat.transform.position, Cat.transform.rotation);
                string witness = "";
                if (!broad)
                {
                    foreach (var solid in Physics.OverlapSphere(Cat.transform.position, 1f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (solid.transform.IsChildOf(Cat.transform)) continue;
                        solid.enabled = false;
                        bool cleared = Cat.IsInteractionPoseClear(Cat.transform.position, Cat.transform.rotation);
                        solid.enabled = true;
                        if (cleared) witness += solid.name + "/" + solid.GetType().Name + "|";
                    }
                }
                rows.Add(Csv(phase, radius, angle, zone, true, broad, standing, accepted, vertex, collider, body, controller, witness));
            }
        }
        File.WriteAllLines(Path.Combine(Output, "longhair-admission.csv"), rows);
    }

    [UnityTest, Timeout(420000)]
    public IEnumerator TenBreeds_ActualCareButtons_KeepTenSecondsAndClearMotionAtTwentyFps()
    {
        yield return (IEnumerator)Call(home, "Home");
        Time.captureFramerate = 20;
        var failures = new List<string>();
        var rows = new List<string> { "breed,kind,completed,contactSeconds,unsafeFrames,rootDrift,yawDrift,maxPawError,maxJoint0,maxJoint1,maxJoint2,p95SolveMs,maxSolveMs" };
        foreach (var breed in CatBreedCatalog.Load().Entries)
        {
            yield return (IEnumerator)Call(home, "Breed", breed.Id);
            foreach (string kind in new[] { "food", "water" })
            {
                Call(home, "ReadyNeeds");
                var hunger = Read<HungerSystem>(home, "hunger"); var thirst = Read<ThirstSystem>(home, "thirst");
                var setup = Read<BowlInteraction.BowlSetup>(Bowls, kind); setup.Fill();
                Read<BowlInteraction.BowlSetup>(Bowls, kind == "food" ? "water" : "food").Empty();
                var target = setup.ContactPoint != null ? setup.ContactPoint : setup.Bowl;
                Call(home, "FindCareStance", target, setup.InteractionPoint, (Func<bool>)(() => CatMealHeadMotion.TryPrepareCareStart(Cat, target, out _)));
                Vector3 root = Cat.transform.position; Quaternion yaw = Cat.transform.rotation;
                int completed = 0, unsafeFrames = 0; float contactSeconds = 0, drift = 0, turn = 0, paw = 0;
                Vector3 joints = Vector3.zero; var timings = new List<double>();
                Action done = () => completed++;
                if (kind == "food") hunger.Ate += done; else thirst.Drank += done;
                try
                {
                    Call(Bowls, "Update"); Read<Button>(Bowls, "interactionButton").onClick.Invoke();
                    Assert.That(Bowls.IsInteracting, Is.True, breed.Id + "/" + kind);
                    float deadline = Time.realtimeSinceStartup + 25;
                    while (Bowls.IsInteracting && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame();
                        var head = Cat.GetComponent<CatMealHeadMotion>();
                        if (Bowls.IsAtContact) contactSeconds += Time.deltaTime;
                        if (head.IsActive)
                        {
                            if (!head.CareFrameClear) unsafeFrames++;
                            joints = Vector3.Max(joints, head.JointDeflections); paw = Mathf.Max(paw, head.PawPlantError);
                            timings.Add(head.ClearanceMilliseconds);
                        }
                        drift = Mathf.Max(drift, Vector3.Distance(root, Cat.transform.position));
                        turn = Mathf.Max(turn, Quaternion.Angle(yaw, Cat.transform.rotation));
                    }
                    timings.Sort();
                    rows.Add(Csv(breed.Id, kind, completed, contactSeconds, unsafeFrames, drift, turn, paw, joints.x, joints.y, joints.z,
                        timings.Count > 0 ? timings[(timings.Count - 1) * 95 / 100] : -1, timings.Count > 0 ? timings[timings.Count - 1] : -1));
                    File.WriteAllLines(Path.Combine(Output, "ten-breed-care.csv"), rows);
                    if (completed != 1 || contactSeconds < 9.9f || unsafeFrames != 0 || drift > .0001f || turn > .01f || paw > .0001f ||
                        joints.x > 30.01f || joints.y > 25.01f || joints.z > 20.01f || Cat.IsMovementPhysicallyLocked)
                        failures.Add(breed.Id + "/" + kind + " " + rows[rows.Count - 1]);
                }
                finally { if (kind == "food") hunger.Ate -= done; else thirst.Drank -= done; CatActionState.CancelForTransition(Cat); }
            }
        }
        Assert.That(rows.Count, Is.EqualTo(21)); Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [UnityTest,Timeout(240000)]
    public IEnumerator LongIdleDepletion_WaterCanStartAcrossAllIdleBeats()
    {
        yield return (IEnumerator)Call(home,"Home");yield return (IEnumerator)Call(home,"Breed","russian-blue");Time.captureFramerate=20;
        var thirst=Read<ThirstSystem>(home,"thirst");var idle=Cat.GetComponent<CatIdleBehavior>();idle.enabled=true;
        Call(home,"Place",new Vector3(0,.05f,-1),Quaternion.identity);
        thirst.ApplySavedValue(100);float decrease=Read<float>(thirst,"decreasePerSecond");int idleFrames=0;
        try
        {
            Set(thirst,"decreasePerSecond",.9f);
            for(int frame=0;frame<2000;frame++){yield return null;if(idle.IsPerformingBeat)idleFrames++;}
        }
        finally{Set(thirst,"decreasePerSecond",decrease);}
        Assert.That(thirst.CurrentThirst,Is.LessThan(15));Assert.That(idleFrames,Is.GreaterThan(20));
        var rows=new List<string>{"idleBeat,started,completed,seconds,unsafeFrames"};var failures=new List<string>();
        foreach(CatIdleBeat beat in Enum.GetValues(typeof(CatIdleBeat)))
        {
            thirst.ApplySavedValue(10);Read<HungerSystem>(home,"hunger").ApplySavedValue(60);Read<EnergySystem>(home,"energy").ApplySavedValue(60);
            var setup=Read<BowlInteraction.BowlSetup>(Bowls,"water");setup.Fill();Read<BowlInteraction.BowlSetup>(Bowls,"food").Empty();
            // Reach a real standing-ready spot before the idle pose starts.
            // Teleporting the fixture inward while it is grooming can create
            // a stance that was never clear for the player standing there.
            idle.enabled=false;Cat.GetComponentInChildren<Animator>().CrossFadeInFixedTime("Base Layer.Idle",.35f,0);
            yield return new WaitForSeconds(.45f);
            Call(home,"FindCareStance",setup.ContactPoint,setup.InteractionPoint,(Func<bool>)(()=>CatMealHeadMotion.TryPrepareCareStart(Cat,setup.ContactPoint,out _)));
            yield return null;yield return null;
            Assert.That(CatMealHeadMotion.TryPrepareCareStart(Cat,setup.ContactPoint,out _),Is.True,"Standing-ready before idle "+beat);
            idle.enabled=true;idle.BeginBeatForTesting(beat);for(int i=0;i<5;i++)yield return null;
            int completed=0,unsafeFrames=0;float seconds=0;Action done=()=>completed++;thirst.Drank+=done;
            try
            {
                Call(Bowls,"Update");Read<Button>(Bowls,"interactionButton").onClick.Invoke();bool started=Bowls.IsInteracting;
                float deadline=Time.realtimeSinceStartup+25;
                while(Bowls.IsInteracting&&Time.realtimeSinceStartup<deadline)
                {
                    yield return new WaitForEndOfFrame();if(Bowls.IsAtContact)seconds+=Time.deltaTime;
                    var head=Cat.GetComponent<CatMealHeadMotion>();if(head.IsActive&&!head.CareFrameClear)unsafeFrames++;
                }
                rows.Add(Csv(beat,started,completed,seconds,unsafeFrames));
                if(!started||completed!=1||seconds<9.9f||unsafeFrames!=0)failures.Add(rows[rows.Count-1]);
            }
            finally{thirst.Drank-=done;CatActionState.CancelForTransition(Cat);}
        }
        File.WriteAllLines(Path.Combine(Output,"long-idle-water.csv"),rows);
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator RepeatedCare_AfterDepletionIdleAndRoomReturn_CompletesAndReleases()
    {
        yield return (IEnumerator)Call(home, "Home");
        yield return (IEnumerator)Call(home, "Breed", "russian-blue");
        Time.captureFramerate = 20;
        var rows = new List<string> { "round,kind,needBefore,needAfter,contactSeconds,completed,rootDrift,unlocked,bowlEvents" };
        for (int round = 0; round < 3; round++)
        {
            if (round == 1)
            {
                yield return (IEnumerator)Call(home, "Room", HomeRoomService.BathroomId);
                yield return (IEnumerator)Call(home, "Room", HomeRoomService.LivingRoomId);
                Time.captureFramerate = 20;
            }
            var hunger = Read<HungerSystem>(home, "hunger");
            var thirst = Read<ThirstSystem>(home, "thirst");
            var idle = Cat.GetComponent<CatIdleBehavior>(); idle.enabled = true;
            Call(home, "ReadyNeeds");
            // Advance actual need Update ticks, including crossing the refusal
            // threshold, rather than only assigning a permanently-low fixture.
            thirst.ApplySavedValue(100); float decrease = Read<float>(thirst, "decreasePerSecond");
            try
            {
                Set(thirst, "decreasePerSecond", round == 2 ? 40f : 25f);
                for (int frame = 0; frame < 60; frame++) yield return null;
            }
            finally { Set(thirst, "decreasePerSecond", decrease); }
            Assert.That(thirst.CurrentThirst, Is.LessThan(30));
            foreach (string kind in new[] { "water", "food" })
            {
                if (kind == "food") hunger.ApplySavedValue(round == 2 ? 0 : 35);
                var setup = Read<BowlInteraction.BowlSetup>(Bowls, kind); setup.Fill();
                Read<BowlInteraction.BowlSetup>(Bowls, kind == "food" ? "water" : "food").Empty();
                var target = setup.ContactPoint != null ? setup.ContactPoint : setup.Bowl;
                Call(home, "FindCareStance", target, setup.InteractionPoint, (Func<bool>)(() =>
                    CatMealHeadMotion.TryPrepareCareStart(Cat, target, out _)));
                float before = kind == "food" ? hunger.CurrentHunger : thirst.CurrentThirst;
                Vector3 root = Cat.transform.position; int completions = 0,bowlEvents=0; float contactSeconds = 0, drift = 0;
                Action completed = () => completions++;
                UnityEngine.Events.UnityAction bowlCompleted=()=>bowlEvents++;
                setup.OnInteractionCompleted.AddListener(bowlCompleted);
                if (kind == "food") hunger.Ate += completed; else thirst.Drank += completed;
                try
                {
                    Call(Bowls, "Update"); Read<Button>(Bowls, "interactionButton").onClick.Invoke();
                    Assert.That(Bowls.IsInteracting, Is.True, "round " + round + " " + kind);
                    for(int click=0;click<4;click++)Read<Button>(Bowls,"interactionButton").onClick.Invoke();
                    float deadline = Time.realtimeSinceStartup + 25;
                    while (Bowls.IsInteracting && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame();
                        if (Bowls.IsAtContact) contactSeconds += Time.deltaTime;
                        drift = Mathf.Max(drift, Vector3.Distance(root, Cat.transform.position));
                    }
                    float after = kind == "food" ? hunger.CurrentHunger : thirst.CurrentThirst;
                    rows.Add(Csv(round, kind, before, after, contactSeconds, completions, drift, !Cat.IsMovementPhysicallyLocked,bowlEvents));
                    File.WriteAllLines(Path.Combine(Output, "repeated-care.csv"), rows);
                    Assert.That(completions, Is.EqualTo(1));
                    Assert.That(bowlEvents, Is.EqualTo(1),"Repeated clicks must not duplicate the bowl reward.");
                    Assert.That(contactSeconds, Is.GreaterThanOrEqualTo(9.9f), "Production ten-second recovery cannot abort early.");
                    Assert.That(after, Is.GreaterThan(99)); Assert.That(drift, Is.LessThan(.0001f));
                    Assert.That(Bowls.IsInteracting || hunger.IsEating || thirst.IsDrinking || Cat.IsMovementPhysicallyLocked, Is.False);
                }
                finally { if (kind == "food") hunger.Ate -= completed; else thirst.Drank -= completed;setup.OnInteractionCompleted.RemoveListener(bowlCompleted); }
            }
        }
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator CareNeck_SourceFrameDiagnostic()
    {
        yield return (IEnumerator)Call(home, "Home");
        yield return (IEnumerator)Call(home, "Breed", "persian");
        Time.captureFramerate = 20;
        Call(home, "ReadyNeeds");
        var setup = Read<BowlInteraction.BowlSetup>(Bowls, "food");
        Read<BowlInteraction.BowlSetup>(Bowls, "water").Empty(); setup.Fill();
        var target = setup.ContactPoint;
        Vector3 outward = setup.InteractionPoint.position - target.position; outward.y = 0; outward.Normalize();
        Call(home, "Place", target.position + outward * .34f, Quaternion.LookRotation(-outward) * Quaternion.Euler(0, -15, 0));
        var animator = Cat.GetComponentInChildren<Animator>(); animator.Play("Idle", 0, .43f); animator.Update(0);
        var camera = Camera.main; camera.orthographic = true; camera.orthographicSize = .58f;
        Vector3 centre = Cat.transform.position + Cat.transform.forward * .13f + Vector3.up * .18f;
        camera.transform.position = centre - camera.transform.forward * 2.5f;
        camera.transform.LookAt(centre);
        yield return null;
        Call(Bowls, "Update");
        Read<Button>(Bowls, "interactionButton").onClick.Invoke();
        var rows = new List<string> { "frame,time,active,contact,distance,minDistance,neck0,neck1,neck2,shoulder,lean,clear" };
        for (int frame = 0; frame < 45; frame++)
        {
            yield return new WaitForEndOfFrame();
            var head = Cat.GetComponent<CatMealHeadMotion>();
            rows.Add(Csv(frame, frame * Time.deltaTime, Bowls.IsInteracting, Bowls.IsAtContact,
                head.Distance, head.MinimumDistance, head.JointDeflections.x, head.JointDeflections.y,
                head.JointDeflections.z, head.ForequarterDeflection, head.BodyLeanDistance, head.CareFrameClear));
            if (frame == 2 || frame == 5 || frame == 10 || frame == 15 || frame == 20 || frame == 30)
                ScreenCapture.CaptureScreenshot(Path.Combine(Output, "neck-source-" + frame + ".png"));
        }
        File.WriteAllLines(Path.Combine(Output, "neck-source-frames.csv"), rows);
    }

    [UnityTest, Timeout(420000)]
    public IEnumerator LowFrameRate_AdmissionAndReachDiagnostic()
    {
        yield return (IEnumerator)Call(home, "Home");
        Time.captureFramerate = 20;
        var rows = new List<string> { "breed,kind,radius,yaw,admitted,started,contact,elapsed,maxNeck,maxAngularSpeed,minimumDistance" };
        var failures = new List<string>();
        foreach (string breed in new[] { "russian-blue", "persian", "oriental-shorthair" })
        {
            yield return (IEnumerator)Call(home, "Breed", breed);
            foreach (string kind in new[] { "food", "water" })
            {
                var setup = Read<BowlInteraction.BowlSetup>(Bowls, kind);
                var target = setup.ContactPoint != null ? setup.ContactPoint : setup.Bowl;
                Vector3 outward = setup.InteractionPoint.position - target.position; outward.y = 0; outward.Normalize();
                foreach (float radius in new[] { .30f, .34f, .38f, .42f })
                foreach (float yaw in new[] { -15f, 0f, 15f })
                {
                    Call(home, "ReadyNeeds"); setup.Fill();
                    Read<BowlInteraction.BowlSetup>(Bowls, kind == "food" ? "water" : "food").Empty();
                    Call(home, "Place", target.position + outward * radius, Quaternion.LookRotation(-outward) * Quaternion.Euler(0, yaw, 0));
                    var animator = Cat.GetComponentInChildren<Animator>(); animator.Play("Idle", 0, .43f); animator.Update(0);
                    yield return null;
                    bool admitted = CatMealHeadMotion.TryPrepareCareStart(Cat, target, out _);
                    Call(Bowls, "Update");
                    if (!admitted) { rows.Add(Csv(breed, kind, radius, yaw, admitted)); continue; }
                    var button = Read<Button>(Bowls, "interactionButton"); button.onClick.Invoke();
                    bool started = Bowls.IsInteracting, contact = false;
                    float elapsed = 0, maxNeck = 0, maxAngularSpeed = 0;
                    var neck = CatBreedVisualFactory.FindDescendant(Cat.transform, "DEF-spine.005");
                    Quaternion previous = neck.rotation;
                    float deadline = Time.realtimeSinceStartup + 8;
                    while (Bowls.IsInteracting && !contact && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame(); elapsed += Time.deltaTime;
                        var head = Cat.GetComponent<CatMealHeadMotion>();
                        maxNeck = Mathf.Max(maxNeck, head.TotalDeflection);
                        maxAngularSpeed = Mathf.Max(maxAngularSpeed, Quaternion.Angle(previous, neck.rotation) / Mathf.Max(.001f, Time.deltaTime)); previous = neck.rotation;
                        contact = Bowls.IsAtContact;
                    }
                    var result = Csv(breed, kind, radius, yaw, admitted, started, contact, elapsed, maxNeck, maxAngularSpeed,
                        Cat.GetComponent<CatMealHeadMotion>().MinimumDistance);
                    rows.Add(result);
                    if (!started || !contact) failures.Add(result);
                    Directory.CreateDirectory(Output);
                    File.WriteAllLines(Path.Combine(Output, "care-admission-diagnostic.csv"), rows);
                    if (started && !contact && rows.Count < 30)
                    { ScreenCapture.CaptureScreenshot(Path.Combine(Output, "care-failed-" + breed + "-" + kind + "-" + radius + "-" + yaw + ".png")); yield return new WaitForEndOfFrame(); }
                    CatActionState.CancelForTransition(Cat);
                    for (int i = 0; i < 8; i++) yield return null;
                }
            }
        }
        File.WriteAllLines(Path.Combine(Output, "care-admission-diagnostic.csv"), rows);
        Assert.That(rows.Count, Is.EqualTo(73), "Diagnostic coverage only; contact failures are retained in the report.");
        Assert.That(failures, Is.Empty, "Every accepted start must reach actual food/water: " + string.Join("\n", failures));
    }
    static string Csv(params object[] values) => string.Join(",", values.Select(value =>
        value is IFormattable number ? number.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : value?.ToString() ?? ""));
}
#endif
