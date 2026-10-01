using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Uses the established real full-cycle/mesh witness fixture.</summary>
public sealed class MeasuredFurnitureSupportTests
{
    const string Root = "Docs/QA/INTERACTION_POLISH_2026-09-16";
    PreparedInteractionStartTests fixture;
    readonly List<string> rows = new List<string>();
    readonly List<string> footRows = new List<string>();
    readonly List<string> anatomyRows = new List<string>();
    readonly QaMeshTopologyCache topology = new QaMeshTopologyCache();
    QaExactMeshContact metric;
    Mesh sampled;
    readonly Dictionary<Mesh,int[][]> footIndices = new Dictionary<Mesh,int[][]>();
    readonly Dictionary<string, int> frames = new Dictionary<string, int>();
    readonly List<string> failures = new List<string>();
    readonly List<string> limbWitnessRows=new List<string>();
    readonly List<string> environmentRows=new List<string>();
    readonly Dictionary<string,float> visualPhaseErrors=new Dictionary<string,float>();
    PreparedInteractionStartTests environmentObserver;
    ulong environmentScene;
    Collider[] environmentSolids;
    bool prepared;
    string suffix;
    readonly List<string> supportPlanDiagnostics=new List<string>();
    [SetUp] public void Before()
    {
        fixture = new PreparedInteractionStartTests(); fixture.Before(); fixture.CaptureSkinEvidence = true; prepared = true;
        supportPlanDiagnostics.Clear();
        environmentObserver=new PreparedInteractionStartTests();environmentScene=0;environmentSolids=null;
        environmentRows.Clear();visualPhaseErrors.Clear();
        limbWitnessRows.Clear();
        limbWitnessRows.Add("activity,frame,pose,phase,witness,depth,witnessWeights,witnessSource,witnessPost,leg,witnessLeg,planted,sourceUpper,sourceLower,sourceFoot,postUpper,postLower,postFoot,target,desiredLower,axis,desiredAxialError,desiredRadialError,desiredUpperReachError,sourceTargetShift,postTargetResidual,bendRadius,actualRadialLength");
        environmentRows.Add("activity,frame,pose,phase,sceneSkinDepth,collider,vertex,point,detail,plantedBySolver,sourceGeometryContacts,postGeometryContacts,png");
        rows.Clear(); frames.Clear(); failures.Clear(); footRows.Clear(); anatomyRows.Clear(); footIndices.Clear();
        topology.Clear(); metric = new QaExactMeshContact(topology); sampled = new Mesh { name = "Independent supported skin witness" };
        footRows.Add("activity,frame,pose,phase,foot,sourceUpwardGap,postUpwardGap,sourceSupportVertices,postSupportVertices,sourceSkinY,postSkinY,sourceVertex,postVertex,postTriangle,postNormalY");
        anatomyRows.Add("activity,frame,bone,sourceLength,postLength,difference,maximumLocalPositionChange,maximumLocalScaleChange");
        rows.Add("activity,frame,pose,phase,visualLift,requiredLift,topPenetration,legResidual,rootShift,reachLimited,measuredVertices,rayQueries,lastSolveMs,maxSolveMs,solveFrame,plantedLegs,bakes,lateralFootShift,bendDegrees,supportCandidates,acceptedSupportTargets,weightedCandidates,weightedVertices,numericReason,numericRawDepth,numericWitness,numericPlanted");
    }
    [UnityTearDown] public IEnumerator After()
    {
        if (!prepared) yield break; prepared = false;
        try { fixture.After(); }
        finally
        {
            File.WriteAllLines(Root + "/measured-support-" + suffix + ".csv", rows);
            File.WriteAllText(Root+"/measured-support-"+suffix+"-landing-rejection.txt",CatJumpLimbClearance.LastRejection??"none");
            File.WriteAllLines(Root+"/measured-support-"+suffix+"-plans.tsv",supportPlanDiagnostics);
            File.WriteAllLines(Root + "/measured-support-" + suffix + "-feet.csv", footRows);
            File.WriteAllLines(Root + "/measured-support-" + suffix + "-anatomy.csv", anatomyRows);
            File.WriteAllLines(Root+"/measured-support-"+suffix+"-scene-floor-witness.csv",environmentRows);
            File.WriteAllLines(Root+"/measured-support-"+suffix+"-limb-witness.csv",limbWitnessRows);
            environmentObserver?.ClearVisibleProxies();metric?.Clear(); topology.Clear();
            if (sampled != null) UnityEngine.Object.DestroyImmediate(sampled);
            foreach (string file in new[] { "prepared-full-descent-regression.csv", "exact-skin-metric-evidence.csv" })
                if (suffix!="hammock-frozen"&&File.Exists(Root + "/" + file)) File.Copy(Root + "/" + file, Root + "/measured-support-" + suffix + "-" + file, true);
        }
        // Fixture.After can request restoration of a different breed. Settle
        // that request before another test is allowed to load a Single scene.
        float deadline = Time.realtimeSinceStartup + 15f;
        while (CatPawReachCatalog.HasPendingLoads && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(CatPawReachCatalog.HasPendingLoads, Is.False); yield return null;
    }
    [UnityTest, Timeout(120000)] public IEnumerator Hammock_MeasuredSupportAxis_FullCycle()
    {suffix="hammock-axis";yield return Run(new[]{"garden.hammock"});}
    [UnityTest, Timeout(120000)] public IEnumerator Hammock_FrozenSupportGeometryDiagnostics()=>FrozenHammock(false);
    [UnityTest, Timeout(120000)] public IEnumerator Hammock_FrozenPlacementDiagnostics()=>FrozenHammock(true);
    IEnumerator FrozenHammock(bool placement)
    {
        suffix="hammock-frozen";
        const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        yield return (IEnumerator)typeof(PreparedInteractionStartTests).GetMethod("Prepare",hidden).Invoke(fixture,new object[]{"Garden_Level01"});
        var cat=UnityEngine.Object.FindAnyObjectByType<CatMovement>();
        var owner=CatActivity.Registered.Single(a=>a.StoreProductId=="garden.hammock"&&a.gameObject.scene==cat.gameObject.scene);
        object[] args={owner,default(CatActivityStart),null};
        Assert.That((bool)typeof(PreparedInteractionStartTests).GetMethod("FindReadyPose",hidden).Invoke(fixture,args),Is.True,(string)args[2]);
        yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(owner.TryStart(cat),Is.True);var animation=cat.GetComponent<CatActivityAnimation>();
        float deadline=Time.realtimeSinceStartup+30;
        while(owner.IsRunning&&animation.CurrentPose!=CatActivityPose.SitDown&&Time.realtimeSinceStartup<deadline)yield return new WaitForEndOfFrame();
        Assert.That(animation.CurrentPose,Is.EqualTo(CatActivityPose.SitDown));
        owner.StopAllCoroutines();var support=animation.ContactSurface;
        var diagnosticCamera=Camera.main;
        if(diagnosticCamera!=null)
        {
            diagnosticCamera.transform.position=cat.transform.position+new Vector3(1.3f,1f,1.4f);
            diagnosticCamera.transform.LookAt(cat.transform.position+Vector3.up*.35f);
            diagnosticCamera.orthographic=true;diagnosticCamera.orthographicSize=.6f;
        }
        Vector3 originalPosition=cat.transform.position,originalSupport=support.position;
        Quaternion originalHeading=cat.transform.rotation,originalSupportHeading=support.rotation;
        var variants=new List<(string name,Vector3 offset,float yaw)>{("base",Vector3.zero,0)};
        if(placement)variants.AddRange(new[]{("forward16",Vector3.forward*.16f,0f),("back16",Vector3.back*.16f,0f),
            ("left12",Vector3.left*.12f,0f),("right12",Vector3.right*.12f,0f),
            ("up8",Vector3.up*.08f,0f),("up16",Vector3.up*.16f,0f),("up24",Vector3.up*.24f,0f),
            ("yaw90",Vector3.zero,90f),("yaw-90",Vector3.zero,-90f)});
        var placementRows=new List<string>{"variant,maxSkinDepth,pawFailures,allFailures"};
        foreach(var variant in variants)
        {
            int previousRows=environmentRows.Count,previousIssues=failures.Count;
            cat.transform.SetPositionAndRotation(originalPosition+originalHeading*variant.offset,originalHeading*Quaternion.Euler(0,variant.yaw,0));
            support.SetPositionAndRotation(originalSupport+originalHeading*variant.offset,originalSupportHeading*Quaternion.Euler(0,variant.yaw,0));
            foreach(var pose in placement?new[]{CatActivityPose.SitDown,CatActivityPose.TowelSettle}:new[]{CatActivityPose.SitDown,CatActivityPose.TowelSettle,CatActivityPose.TowelWake,CatActivityPose.StandUp})
            foreach(float phase in placement?new[]{0f,.52f,.94f}:new[]{0f,.12f,.24f,.52f,.82f,.94f,1f})
            {
                animation.SetTimedPose(pose,phase,support);
                for(int i=0;i<3;i++){yield return new WaitForEndOfFrame();Sample();}
                if(pose==CatActivityPose.SitDown&&phase==0||pose==CatActivityPose.TowelSettle&&(phase==.52f||phase==.94f))
                    typeof(PreparedInteractionStartTests).GetMethod("CaptureActualGameFrame",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{Root+"/hammock-frozen-"+variant.name+"-"+pose+"-"+F(phase)+".png"});
            }
            float maximum=environmentRows.Skip(previousRows).Select(row=>float.Parse(row.Split(',')[4],CultureInfo.InvariantCulture)).DefaultIfEmpty().Max();
            placementRows.Add(string.Join(",",variant.name,F(maximum),failures.Skip(previousIssues).Count(s=>s.Contains("lifted a source-supported")),failures.Count-previousIssues));
            File.WriteAllLines(Root+"/hammock-placement-diagnostic.csv",placementRows);
        }
        owner.CancelForTransition();
        Assert.That(failures,Is.Empty,string.Join("\n",failures.Distinct()));
    }
    [UnityTest, Timeout(120000)] public IEnumerator BeanBag_MeasuredSupportHeight_FullCycle()
    {suffix="bean-height";yield return Run(new[]{"loft.bean-bag"});}
    [UnityTest, Timeout(120000)] public IEnumerator Chaise_MeasuredSupport_FullCycle()
    {suffix="chaise";yield return Run(new[]{"loft.chaise-lounge"});}
    [UnityTest, Timeout(120000)] public IEnumerator Cushions_MeasuredSupport_FullCycle()
    {suffix="cushions";yield return Run(new[]{"loft.floor-cushions"});}
    [UnityTest, Timeout(240000)]
    public IEnumerator FourRemainingSurfaces_SourceSupportDiagnostics()
    {
        suffix="four-diagnostic";
        yield return Run(new[]{"garden.hammock","loft.bean-bag","loft.chaise-lounge","loft.floor-cushions"});
    }
    [UnityTest, Timeout(240000)]
    public IEnumerator HammockAndCushions_TwoWorstSupportedCycles_ClearRealSkinWithoutStretching()
    {
        suffix = "two-worst";
        yield return Run(new[] { "garden.hammock", "loft.floor-cushions" });
    }
    [UnityTest, Timeout(420000)]
    public IEnumerator SixReportedSurfaces_FullCycles_ClearRealSkinAndCompleteDeparture()
    {
        suffix = "six";
        yield return Run(new[] { "bathroom.tub", "garden.hammock", "balcony.hanging-chair",
            "loft.bean-bag", "loft.chaise-lounge", "loft.floor-cushions" });
    }
    [UnityTest, Timeout(120000)]
    public IEnumerator TubDeparture_KeepsMeasuredSupportUntilNativeJumpFinishes()
    {
        suffix = "tub-departure";
        yield return Run(new[] { "bathroom.tub" });
    }
    [UnityTest, Timeout(120000)]
    public IEnumerator HangingChair_FullCycle_ClearRealSkinAndCompleteDeparture()
    {
        suffix = "hanging-chair";
        yield return Run(new[] { "balcony.hanging-chair" });
    }
    [UnityTest, Timeout(120000)] public IEnumerator HangingChair_British_ClearRealSkinAndCompleteDeparture()
    {
        fixture.BreedUnderTest="british-shorthair";suffix="chair-british-focus";
        yield return Run(new[]{"balcony.hanging-chair"});
    }
    [UnityTest, Timeout(360000)]
    public IEnumerator HangingChair_TenBreeds_ClearRealSkinAndCompleteDeparture()
    {
        var results = new List<string>{"breed,completed,maxSkinDepth"};
        string output="Docs/QA/INTERACTION_POLISH_THREE30_2026-09-17/chair-ten-breeds.csv";
        foreach(var breed in CatBreedCatalog.Load().Entries)
        {
            fixture.BreedUnderTest=breed.Id;suffix="chair-"+breed.Id;frames.Remove("balcony.hanging-chair");
            yield return Run(new[]{"balcony.hanging-chair"});
            var columns=File.ReadAllLines(Root+"/prepared-full-descent-regression.csv")[1].Split(',');
            results.Add(breed.Id+","+columns[3]+","+columns[6]);File.WriteAllLines(output,results);
        }
        Assert.That(results.Count,Is.EqualTo(11));
    }
    [UnityTest, Timeout(120000)] public IEnumerator HangingChair_MaineCoon_CurrentDeparture()
    { fixture.BreedUnderTest="maine-coon";suffix="chair-maine-final";yield return Run(new[]{"balcony.hanging-chair"}); }
    [UnityTest, Timeout(120000)] public IEnumerator HangingChair_Final_domestic_shorthair()
    { fixture.BreedUnderTest="domestic-shorthair";suffix="chair-final-domestic-shorthair";yield return Run(new[]{"balcony.hanging-chair"}); }
    [UnityTest, Timeout(120000)] public IEnumerator HangingChair_Final_domestic_shorthair_orange()
    { fixture.BreedUnderTest="domestic-shorthair-orange";suffix="chair-final-domestic-shorthair-orange";yield return Run(new[]{"balcony.hanging-chair"}); }
    [UnityTest, Timeout(120000)] public IEnumerator HangingChair_Final_khao_manee()
    { fixture.BreedUnderTest="khao-manee";suffix="chair-final-khao-manee";yield return Run(new[]{"balcony.hanging-chair"}); }
    [UnityTest, Timeout(120000)] public IEnumerator HangingChair_Final_british_shorthair()
    { fixture.BreedUnderTest="british-shorthair";suffix="chair-final-british-shorthair";yield return Run(new[]{"balcony.hanging-chair"}); }
    [UnityTest, Timeout(120000)] public IEnumerator HangingChair_Final_domestic_longhair()
    { fixture.BreedUnderTest="domestic-longhair";suffix="chair-final-domestic-longhair";yield return Run(new[]{"balcony.hanging-chair"}); }
    [UnityTest, Timeout(120000)] public IEnumerator HangingChair_Final_maine_coon()
    { fixture.BreedUnderTest="maine-coon";suffix="chair-final-maine-coon";yield return Run(new[]{"balcony.hanging-chair"}); }
    [UnityTest, Timeout(120000)] public IEnumerator HangingChair_Final_oriental_shorthair()
    { fixture.BreedUnderTest="oriental-shorthair";suffix="chair-final-oriental-shorthair";yield return Run(new[]{"balcony.hanging-chair"}); }
    [UnityTest, Timeout(120000)] public IEnumerator HangingChair_Final_persian()
    { fixture.BreedUnderTest="persian";suffix="chair-final-persian";yield return Run(new[]{"balcony.hanging-chair"}); }
    [UnityTest, Timeout(120000)] public IEnumerator HangingChair_Final_russian_blue()
    { fixture.BreedUnderTest="russian-blue";suffix="chair-final-russian-blue";yield return Run(new[]{"balcony.hanging-chair"}); }
    [UnityTest, Timeout(120000)] public IEnumerator HangingChair_Final_sphynx()
    { fixture.BreedUnderTest="sphynx";suffix="chair-final-sphynx";yield return Run(new[]{"balcony.hanging-chair"}); }
    IEnumerator Run(string[] products)
    {
        var method = typeof(PreparedInteractionStartTests).GetMethod("CompleteFullCycles", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        yield return Observe((IEnumerator)method.Invoke(fixture, new object[] { products, false }));
        // Full-cycle completion/first-start/jump/anatomy/floor-release assertions
        // above are preserved. Add a tight actual rendered-mesh requirement;
        // the focused fixture itself deliberately only reports skin depth.
        var cycle = File.ReadAllLines(Root + "/prepared-full-descent-regression.csv").Skip(1).ToArray();
        Assert.That(cycle.Length, Is.EqualTo(products.Length));
        foreach (string row in cycle)
        {
            string[] fields = row.Split(','); string product = fields[1];
            Assert.That(float.Parse(fields[6], CultureInfo.InvariantCulture), Is.LessThanOrEqualTo(.005f), product + " actual full-cycle skin penetration");
            Assert.That(frames.TryGetValue(product, out int count) && count > 12, Is.True, product + " measured support not exercised");
        }
        Assert.That(failures, Is.Empty, string.Join("\n", failures.Distinct()));
    }
    IEnumerator Observe(IEnumerator routine)
    {
        while (routine.MoveNext())
        {
            if (routine.Current is IEnumerator child) yield return Observe(child);
            else
            {
                object current = routine.Current; yield return current;
                if (current is WaitForEndOfFrame) Sample();
            }
        }
    }
    void Sample()
    {
        var owner = CatActivity.Active; if (owner == null) return;
        var cat = UnityEngine.Object.FindAnyObjectByType<CatMovement>(); if (cat == null) return;
        var motion = cat.GetComponent<CatMeasuredSupportMotion>();
        if (motion == null || !motion.IsActive) return;
        var animation = cat.GetComponent<CatActivityAnimation>(); string product = owner.StoreProductId;
        frames.TryGetValue(product, out int count); frames[product] = count + 1;
        rows.Add(string.Join(",", product, Time.frameCount, animation.CurrentPose, F(animation.NativeJumpPhase),
            F(motion.VisualLift), F(motion.RequiredLift), F(motion.MaximumTopPenetration), F(motion.MaximumLegResidual),
            F(motion.MaximumRootShift), motion.ReachLimited, motion.MeasuredVertices, motion.RayQueries,
            motion.LastSolveMs.ToString("R",CultureInfo.InvariantCulture), motion.MaximumSolveMs.ToString("R",CultureInfo.InvariantCulture), motion.SolveFrame,
            motion.PlantedLegCount,motion.BakeCount,F(motion.MaximumFootLateralShift),F(motion.MaximumBendDegrees),motion.SupportCandidateQueries,motion.AcceptedSupportTargets,motion.WeightedCandidateQueries,motion.WeightedCandidateVertices,motion.NumericPlanReason,F(motion.NumericPlanRawDepth),motion.NumericPlanRawWitness,motion.NumericPlanPlantedCount));
        if (motion.SolveFrame != Time.frameCount) failures.Add(product + " support measurement belongs to another frame");
        if(suffix!="four-diagnostic"&&(count%15==0||motion.NumericPlanReason=="AllCandidatesRejected"||animation.IsNativeJump||animation.CurrentPose==CatActivityPose.StandUp||motion.LastSolveMs>35))
        {
            var attempts=new List<string>();
            for(int i=0;i<motion.NumericPlanAttemptCount;i++){var a=motion.NumericPlanAttemptAt(i);attempts.Add(string.Join(",",F(a.lift),F(a.tiltDegrees),F(a.depth),F(a.reachMargin),a.leg,a.sourceVertex,a.diagnosticCode,a.blocker!=null?a.blocker.name:""));}
            var pawPlans=new List<string>();for(int i=0;i<4;i++)pawPlans.Add(i+":"+motion.NumericPlanFootPlanted(i)+":"+motion.NumericPawSettleAt(i).ToString("F6"));
            supportPlanDiagnostics.Add(string.Join("\t",product,Time.frameCount,animation.CurrentPose,F(animation.NativeJumpPhase),true,motion.NumericPlanReason=="Accepted",motion.NumericPlanReason,F(motion.NumericPlanRawDepth),motion.NumericPlanRawWitness,motion.NumericPlanPlantedCount,F(motion.NumericPlanReachLiftCap),motion.CommonLiftReason,F(motion.CommonLiftResidual),F(motion.CommonLiftAllowedByReach),string.Join(";",attempts),string.Join(";",pawPlans)));
        }
        if (motion.MaximumRootShift > .00001f) failures.Add(product + " support moved the actor root");
        if (motion.MaximumLegResidual > .002f) failures.Add(product + " support exceeds actual limb reach");
        if(motion.BakeCount>4)failures.Add(product+" support exceeded the four-bake work bound");
        if(motion.MaximumBendDegrees>25.01f)failures.Add(product+" support exceeded the bounded source knee-plane adjustment");
        var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>();
        var all = cat.GetComponentInChildren<Animator>().GetComponentsInChildren<Transform>(true);
        var winningPositions = all.Select(t => t.localPosition).ToArray();
        var winningRotations = all.Select(t => t.localRotation).ToArray();
        var winningScales = all.Select(t => t.localScale).ToArray();
        var rootBefore = cat.transform.position; var headingBefore = cat.transform.rotation;
        var measuredAdjusted = typeof(CatMeasuredSupportMotion).GetField("adjusted", BindingFlags.Instance | BindingFlags.NonPublic);
        bool wasMeasured = (bool)measuredAdjusted.GetValue(motion);
        var rim = cat.GetComponent<CatTubRimMotion>();
        var rimAdjusted = typeof(CatTubRimMotion).GetField("adjusted", BindingFlags.Instance | BindingFlags.NonPublic);
        bool wasRim = rim != null && (bool)rimAdjusted.GetValue(rim);
        var sourceLengths = new Dictionary<string,float>();
        Vector3[] postSkin = null, sourceSkin = null;
        // One independent visible-paw witness every three rendered frames;
        // same-frame anatomy and solver time are observed on every frame.
        bool feetSample = suffix=="cushions"||count % 3 == 0;
        if (feetSample) postSkin = BakedWorld(skin);
        if(feetSample&&motion.NumericPlanApplied)
        {
            const BindingFlags state=BindingFlags.Instance|BindingFlags.NonPublic;
            var source=(CatSupportedLimbSkin)typeof(CatMeasuredSupportMotion).GetField("supportedSkin",state).GetValue(motion);
            var poses=(CatSupportedLimbSkin.Pose[])typeof(CatMeasuredSupportMotion).GetField("planPoses",state).GetValue(motion);
            Vector3 up=(Vector3)typeof(CatMeasuredSupportMotion).GetField("up",state).GetValue(motion);
            float lift=(float)typeof(CatMeasuredSupportMotion).GetField("planLift",state).GetValue(motion);
            Quaternion rotation=(Quaternion)typeof(CatMeasuredSupportMotion).GetField("planRotation",state).GetValue(motion);
            Vector3 pivot=(Vector3)typeof(CatMeasuredSupportMotion).GetField("planPivot",state).GetValue(motion);
            float maximum=0;int witness=-1;
            for(int slot=0;slot<source.VertexCount;slot++)
            {
                int index=source.SourceIndex(slot);float error=Vector3.Distance(source.CombinedPoint(slot,poses,up*lift,rotation,pivot),postSkin[index]);
                if(error>maximum){maximum=error;witness=index;}
            }
            if(maximum>.0001f)
            {
                string detail=product+" numeric/applied skin mismatch "+animation.CurrentPose+"/"+F(animation.NativeJumpPhase)+" frame="+Time.frameCount+" error="+F(maximum)+" vertex="+witness;
                failures.Add(detail);supportPlanDiagnostics.Add(detail);
            }
        }
        float localPositionChange = 0f, localScaleChange = 0f;
        try
        {
            motion.Restore();
            if (wasRim) typeof(CatTubRimMotion).GetMethod("Restore",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(rim,null);
            foreach (string side in new[] { "L", "R" })
            foreach (string[] chain in Chains)
            for (int i = 0; i < 2; i++)
            {
                var first = CatBreedVisualFactory.FindDescendant(cat.transform, chain[i] + side);
                var second = CatBreedVisualFactory.FindDescendant(cat.transform, chain[i + 1] + side);
                sourceLengths[chain[i] + side] = Vector3.Distance(first.position, second.position);
            }
            for (int i = 0; i < all.Length; i++)
            {
                // Only the visual origin may translate; skeletal local
                // positions/scales must match this exact source frame.
                if (all[i] != all[0]) localPositionChange = Mathf.Max(localPositionChange, Vector3.Distance(all[i].localPosition,winningPositions[i]));
                localScaleChange = Mathf.Max(localScaleChange, Vector3.Distance(all[i].localScale,winningScales[i]));
            }
            if (feetSample) sourceSkin = BakedWorld(skin);
            if(suffix=="four-diagnostic"&&count%15==0)
            {
                var source=(CatSupportedLimbSkin)typeof(CatMeasuredSupportMotion).GetField("supportedSkin",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(motion);
                if(source!=null&&source.Capture())
                {
                    bool clear;var support=animation.ContactSurface!=null?animation.ContactSurface.position:cat.transform.position;
                    bool measured=motion.TryPredictNativePreparation(source,support,cat.transform.rotation,out clear);
                    var attempts=new List<string>();
                    for(int i=0;i<motion.NumericPlanAttemptCount;i++){var a=motion.NumericPlanAttemptAt(i);attempts.Add(string.Join(",",F(a.lift),F(a.tiltDegrees),F(a.depth),F(a.reachMargin),a.leg,a.sourceVertex,a.diagnosticCode,a.blocker!=null?a.blocker.name:""));}
                    supportPlanDiagnostics.Add(string.Join("\t",product,Time.frameCount,animation.CurrentPose,F(animation.NativeJumpPhase),measured,clear,motion.NumericPlanReason,F(motion.NumericPlanRawDepth),motion.NumericPlanRawWitness,motion.NumericPlanPlantedCount,F(motion.NumericPlanReachLiftCap),motion.CommonLiftReason,F(motion.CommonLiftResidual),F(motion.CommonLiftAllowedByReach),string.Join(";",attempts)));
                }
            }
        }
        finally
        {
            for (int i = 0; i < all.Length; i++)
            { all[i].localPosition = winningPositions[i]; all[i].localRotation = winningRotations[i]; all[i].localScale = winningScales[i]; }
            measuredAdjusted.SetValue(motion,wasMeasured);
            if (rim != null) rimAdjusted.SetValue(rim,wasRim);
        }
        if (localPositionChange > .00001f || localScaleChange > .00001f) failures.Add(product + " changed this frame's skeletal position/scale");
        if (Vector3.Distance(rootBefore,cat.transform.position) > .000001f || Quaternion.Angle(headingBefore,cat.transform.rotation) > .0001f)
            failures.Add(product + " source witness altered the actor root");
        foreach (string side in new[] { "L", "R" })
        foreach (string[] chain in Chains)
        {
            for (int i = 0; i < 2; i++)
            {
                var first = CatBreedVisualFactory.FindDescendant(cat.transform, chain[i] + side);
                var second = CatBreedVisualFactory.FindDescendant(cat.transform, chain[i + 1] + side);
                float value = Vector3.Distance(first.position, second.position), baseline = sourceLengths[chain[i] + side];
                anatomyRows.Add(string.Join(",",product,Time.frameCount,chain[i]+side,F(baseline),F(value),F(value-baseline),F(localPositionChange),F(localScaleChange)));
                if (Mathf.Abs(value - baseline) > .0002f) failures.Add(product + " changed same-frame bone length " + chain[i] + side);
            }
        }
        if (feetSample)
        {
            var restored = BakedWorld(skin);
            Assert.That(restored.Length,Is.EqualTo(postSkin.Length));
            for (int i = 0; i < restored.Length; i++)
                if (Vector3.Distance(restored[i],postSkin[i]) > .000002f) { failures.Add(product + " source witness failed to restore visible skin"); break; }
            ObserveFeet(owner,skin,animation,sourceSkin,postSkin);
            ObserveSceneFloorAndPose(cat,owner,skin,animation,motion,sourceSkin,postSkin);
        }
    }
    void ObserveSceneFloorAndPose(CatMovement cat,CatActivity owner,SkinnedMeshRenderer skin,CatActivityAnimation animation,
        CatMeasuredSupportMotion motion,Vector3[] sourceSkin,Vector3[] postSkin)
    {
        const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        if(environmentScene!=cat.gameObject.scene.handle.GetRawData()||environmentSolids==null)
        {
            environmentObserver.ClearVisibleProxies();environmentScene=cat.gameObject.scene.handle.GetRawData();
            // Deliberately no maxY > .12 filter: actual low room floors are
            // part of the independent visible-skin measurement in this test.
            environmentSolids=UnityEngine.Object.FindObjectsByType<Collider>()
                .Where(c=>c!=null&&c.enabled&&!c.isTrigger&&c.gameObject.scene==cat.gameObject.scene&&c.GetComponentInParent<CatMovement>()==null).ToArray();
            typeof(PreparedInteractionStartTests).GetField("roomSolids",hidden).SetValue(environmentObserver,environmentSolids);
        }
        typeof(PreparedInteractionStartTests).GetField("cat",hidden).SetValue(environmentObserver,cat);
        typeof(PreparedInteractionStartTests).GetField("skinSample",hidden).SetValue(environmentObserver,sampled);
        float depth=(float)typeof(PreparedInteractionStartTests).GetMethod("ActualSkinDepth",hidden).Invoke(environmentObserver,null);
        if(depth>.005f)failures.Add(owner.StoreProductId+" actual scene skin including floor exceeds 5 mm");
        if(depth>.001f)ObserveLimbWitness(owner,skin,animation,motion,sourceSkin,postSkin,depth);
        int sourceContacts=0,postContacts=0;
        var indices=PawIndices(skin);
        var surfaces=environmentSolids.OfType<MeshCollider>().Where(c=>c!=null&&c.enabled&&c.sharedMesh!=null).ToArray();
        // Original triangle closest points and normals, independent of the
        // runtime vertical support ray or its privately held planted flags.
        for(int foot=0;foot<4;foot++)
        {
            var bounds=new Bounds(sourceSkin[indices[foot][0]],Vector3.zero);
            foreach(int v in indices[foot]){bounds.Encapsulate(sourceSkin[v]);bounds.Encapsulate(postSkin[v]);}
            bounds.Expand(.036f);
            var nearby=surfaces.Where(c=>c.bounds.Intersects(bounds)).ToArray();
            var a=FootGap(indices[foot],sourceSkin,nearby,Vector3.up);
            var b=FootGap(indices[foot],postSkin,nearby,Vector3.up);
            if(a.gap<=.018f)sourceContacts++;if(b.gap<=.018f)postContacts++;
        }
        float desired=-1;
        if(owner.StoreProductId=="garden.hammock"&&animation.CurrentPose==CatActivityPose.TowelSettle)desired=.222222f;
        if(owner.StoreProductId=="loft.floor-cushions"&&animation.CurrentPose==CatActivityPose.SitDown)desired=.461539f;
        if(animation.IsNativeJump&&animation.NativeJumpPhase<=CatJumpMotion.Takeoff)desired=CatJumpMotion.Takeoff;
        if(animation.IsNativeJump&&animation.NativeJumpPhase>=CatJumpMotion.Touchdown)desired=CatJumpMotion.Touchdown;
        string png=string.Empty;
        if(desired>=0)
        {
            string key=owner.StoreProductId+"-"+animation.CurrentPose+"-"+(desired==CatJumpMotion.Takeoff?"launch":desired==CatJumpMotion.Touchdown?"land":"limb");
            float error=Mathf.Abs(animation.NativeJumpPhase-desired);
            if(error<=.065f&&!visualPhaseErrors.ContainsKey(key))
            {
                visualPhaseErrors[key]=error;png=Root+"/support-pose-"+key+".png";
                typeof(PreparedInteractionStartTests).GetMethod("CaptureActualGameFrame",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{png});
            }
        }
        environmentRows.Add(string.Join(",",owner.StoreProductId,Time.frameCount,animation.CurrentPose,F(animation.NativeJumpPhase),F(depth),
            Q(environmentObserver.DeepestSkinCollider),environmentObserver.DeepestSkinVertex,Q(environmentObserver.DeepestSkinPoint.ToString("F6")),
            Q(environmentObserver.DeepestExactSkinDetail),motion.PlantedLegCount,sourceContacts,postContacts,Q(png)));
        string Q(string value)=>"\""+(value??string.Empty).Replace("\"","\"\"")+"\"";
    }
    void ObserveLimbWitness(CatActivity owner,SkinnedMeshRenderer skin,CatActivityAnimation animation,
        CatMeasuredSupportMotion motion,Vector3[] sourceSkin,Vector3[] postSkin,float depth)
    {
        const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        int vertex=environmentObserver.DeepestSkinVertex;
        if(vertex<0||vertex>=postSkin.Length)return;
        var limbMap=(int[])typeof(CatMeasuredSupportMotion).GetField("limb",hidden).GetValue(motion);
        var data=(Array)typeof(CatMeasuredSupportMotion).GetField("legs",hidden).GetValue(motion);
        var w=skin.sharedMesh.boneWeights[vertex];var bones=skin.bones;
        string weights=Bone(w.boneIndex0,w.weight0)+Bone(w.boneIndex1,w.weight1)+Bone(w.boneIndex2,w.weight2)+Bone(w.boneIndex3,w.weight3);
        for(int i=0;i<data.Length;i++)
        {
            object leg=data.GetValue(i);var kind=leg.GetType();
            object Read(string name)=>kind.GetField(name,BindingFlags.Instance|BindingFlags.Public).GetValue(leg);
            Vector3 Point(string name)=>(Vector3)Read(name);
            var upper=(Transform)Read("upper");var lower=(Transform)Read("lower");var foot=(Transform)Read("foot");
            Vector3 target=Point("target"),desired=Point("desiredLower"),delta=target-upper.position;
            float first=(float)Read("upperLength"),second=(float)Read("lowerLength");
            float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(first-second)+.0001f,first+second-.0001f);
            Vector3 axis=delta.normalized;
            float along=(first*first-second*second+distance*distance)/(2f*distance);
            float radius=Mathf.Sqrt(Mathf.Max(0,first*first-along*along));
            float desiredAxis=Vector3.Dot(desired-upper.position,axis)-along;
            float desiredRadius=Vector3.ProjectOnPlane(desired-upper.position,axis).magnitude-radius;
            float desiredReach=Vector3.Distance(desired,upper.position)-first;
            limbWitnessRows.Add(string.Join(",",owner.StoreProductId,Time.frameCount,animation.CurrentPose,F(animation.NativeJumpPhase),
                vertex,F(depth),Q(weights),V(sourceSkin[vertex]),V(postSkin[vertex]),i,limbMap[vertex],Read("planted"),
                V(Point("sourceUpper")),V(Point("sourceLower")),V(Point("sourceFoot")),V(upper.position),V(lower.position),V(foot.position),
                V(target),V(desired),V(axis),F(desiredAxis),F(desiredRadius),F(desiredReach),F(Vector3.Distance(target,Point("sourceFoot"))),
                F(Vector3.Distance(foot.position,target)),F(radius),F(Vector3.ProjectOnPlane(lower.position-upper.position,axis).magnitude)));
        }
        string Bone(int index,float value)=>value<=0?string.Empty:bones[index].name+":"+F(value)+";";
        string Q(string value)=>"\""+value.Replace("\"","\"\"")+"\"";
        string V(Vector3 value)=>Q(F(value.x)+";"+F(value.y)+";"+F(value.z));
    }
    static readonly string[][] Chains = { new[] { "DEF-upper_arm.", "DEF-forearm.", "DEF-hand." }, new[] { "DEF-thigh.", "DEF-shin.", "DEF-foot." } };
    Vector3[] BakedWorld(SkinnedMeshRenderer skin)
    {
        skin.BakeMesh(sampled,true); var points = sampled.vertices;
        for (int i = 0; i < points.Length; i++) points[i] = skin.transform.TransformPoint(points[i]);
        return points;
    }
    int[][] PawIndices(SkinnedMeshRenderer skin)
    {
        if (footIndices.TryGetValue(skin.sharedMesh,out var cached)) return cached;
        var bones = skin.bones; var weights = skin.sharedMesh.boneWeights;
        Assert.That(weights.Length,Is.EqualTo(skin.sharedMesh.vertexCount),"Independent editor test must have original bone weights");
        var groups = new List<int>[] { new List<int>(),new List<int>(),new List<int>(),new List<int>() };
        for (int foot = 0; foot < 4; foot++)
        {
            string name = (foot < 2 ? "DEF-hand." : "DEF-foot.") + ((foot & 1) == 0 ? "L" : "R");
            var joint = bones.First(b=>b.name == name);
            for (int i = 0; i < weights.Length; i++)
            {
                var w = weights[i];
                float sum = Weight(w.boneIndex0,w.weight0) + Weight(w.boneIndex1,w.weight1) + Weight(w.boneIndex2,w.weight2) + Weight(w.boneIndex3,w.weight3);
                if (sum >= .5f) groups[foot].Add(i);
            }
            float Weight(int index,float weight) => weight > 0f && (bones[index] == joint || bones[index].IsChildOf(joint)) ? weight : 0f;
            Assert.That(groups[foot].Count,Is.GreaterThan(10),name + " actual weighted skin patch");
        }
        cached = groups.Select(g=>g.ToArray()).ToArray(); footIndices.Add(skin.sharedMesh,cached); return cached;
    }
    struct FootHit { public float gap,y,normalY; public int count,vertex,triangle; }
    FootHit FootGap(int[] indices,Vector3[] points,MeshCollider[] solids,Vector3 up)
    {
        var result = new FootHit { gap=float.PositiveInfinity,y=float.PositiveInfinity,vertex=-1,triangle=-1 };
        foreach (int index in indices)
        {
            result.y = Mathf.Min(result.y,points[index].y);
            foreach (var solid in solids)
            {
                var hit = metric.Measure(solid.sharedMesh,solid.transform,points[index]);
                // Independent original-triangle distance, not the runtime
                // downward ray or hand/foot-bone-to-target residual.
                float normal = Vector3.Dot(hit.normal,up);
                if (normal < .5f) continue;
                result.count++;
                if (hit.distance >= result.gap) continue;
                result.gap = hit.distance; result.vertex=index; result.triangle=hit.triangle; result.normalY=normal;
            }
        }
        return result;
    }
    void ObserveFeet(CatActivity owner,SkinnedMeshRenderer skin,CatActivityAnimation animation,Vector3[] source,Vector3[] post)
    {
        var indices = PawIndices(skin);
        var solids = owner.GetComponentsInChildren<MeshCollider>().Where(c=>c.enabled && !c.isTrigger && c.sharedMesh!=null).ToArray();
        Vector3 up = animation.ContactSurface != null ? animation.ContactSurface.up : Vector3.up;
        for (int i = 0; i < indices.Length; i++)
        {
            var a = FootGap(indices[i],source,solids,up); var b = FootGap(indices[i],post,solids,up);
            footRows.Add(string.Join(",",owner.StoreProductId,Time.frameCount,animation.CurrentPose,F(animation.NativeJumpPhase),i,
                F(a.gap),F(b.gap),a.count,b.count,F(a.y),F(b.y),a.vertex,b.vertex,b.triangle,F(b.normalY)));
            // Source swing is legitimate. A source-contacting visible paw
            // must still have a real upper-surface contact after adaptation.
            if (a.gap <= .015f && b.gap > .018f) failures.Add(owner.StoreProductId + " lifted a source-supported visible paw off the real surface: " + i);
        }
    }
    static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
}
