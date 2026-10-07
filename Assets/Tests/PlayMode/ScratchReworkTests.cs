#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class ScratchReworkTests
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    ActionReadyReviewTests review;
    static T Read<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static string Output=>SessionState.GetString("CatHome.QA.ResultDirectory","Temp");
    [SetUp]public void Before(){SessionState.EraseString("CatHome.QA.ReviewOnly");review=new ActionReadyReviewTests();review.Before();}
    [TearDown]public void After(){UnityEngine.Object.FindAnyObjectByType<MobileJoystick>()?.CancelInput();review.After();}
    [UnityTest,Timeout(180000)]public IEnumerator Small(){yield return Check("persian");}
    [UnityTest,Timeout(180000)]public IEnumerator Medium(){yield return Check("domestic-shorthair");}
    [UnityTest,Timeout(180000)]public IEnumerator Large(){yield return Check("maine-coon");}
    [Serializable]sealed class VideoCount{public int frames,fps;}
    [UnityTest,Timeout(240000)]public IEnumerator RecordedVideosPlayCompletely()
    {yield return PlayVideos(true);}
    [UnityTest,Timeout(120000)]public IEnumerator RecordedStandingVideosPlayCompletely()
    {yield return PlayVideos(false);}
    IEnumerator PlayVideos(bool includeApproach)
    {
        SessionState.SetString("CatHome.QA.ReviewBreed","domestic-shorthair");yield return (IEnumerator)Call(review,"Boot");Time.captureFramerate=0;
        var root=new GameObject("QA scratch video playback",typeof(Canvas),typeof(UnityEngine.UI.RawImage));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=500;
        var texture=new RenderTexture(1920,1080,0);root.GetComponent<UnityEngine.UI.RawImage>().texture=texture;root.GetComponent<UnityEngine.UI.RawImage>().raycastTarget=false;
        var player=root.AddComponent<UnityEngine.Video.VideoPlayer>();player.playOnAwake=false;player.isLooping=false;player.skipOnDrop=false;player.sendFrameReadyEvents=true;
        player.audioOutputMode=UnityEngine.Video.VideoAudioOutputMode.None;player.renderMode=UnityEngine.Video.VideoRenderMode.RenderTexture;player.targetTexture=texture;
        bool ended=false;string error="";long highest=-1;int decoded=0;
        player.loopPointReached+=_=>ended=true;player.errorReceived+=(_,m)=>error=m;player.frameReady+=(_,f)=>{highest=Math.Max(highest,f);decoded++;};
        try
        {
            foreach(bool approach in includeApproach?new[]{false,true}:new[]{false})
            foreach(string breed in new[]{"domestic-shorthair","persian","maine-coon"})
            {
                string directory=approach?Path.Combine(Output,"joystick-"+breed):Output;
                var expected=JsonUtility.FromJson<VideoCount>(File.ReadAllText(Path.Combine(directory,"video-"+breed+".json")));
                ended=false;error="";highest=-1;decoded=0;player.url=Path.GetFullPath(Path.Combine(directory,"Scratch-"+breed+".mp4"));
                player.Prepare();float end=Time.realtimeSinceStartup+12;while(!player.isPrepared&&error.Length==0&&Time.realtimeSinceStartup<end)yield return null;
                Assert.That(error,Is.Empty);Assert.That(player.isPrepared,Is.True);
                Assert.That(player.length,Is.EqualTo((double)expected.frames/expected.fps).Within(.05));
                player.Play();end=Time.realtimeSinceStartup+30;while(!ended&&error.Length==0&&Time.realtimeSinceStartup<end)yield return null;
                File.WriteAllText(Path.Combine(directory,"playback-"+breed+".json"),"{\"ended\":"+ended.ToString().ToLowerInvariant()+",\"decoded\":"+decoded+",\"highestFrame\":"+highest+"}");
                Assert.That(error,Is.Empty);Assert.That(ended,Is.True);Assert.That(highest,Is.EqualTo(expected.frames-1));Assert.That(decoded,Is.EqualTo(expected.frames));
                player.Stop();
            }
        }
        finally{player.Stop();texture.Release();UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(root);}
    }
    [UnityTest,Timeout(180000)]public IEnumerator PauseCancelAndBlockedStarts()
    {
        SessionState.SetString("CatHome.QA.ReviewBreed","domestic-shorthair");yield return (IEnumerator)Call(review,"Boot");
        HomeStoreService.TrySetStored(HomeStoreService.ScratchPostId,false);yield return null;yield return null;
        var post=CatActivity.Registered.OfType<ScratchPostActivity>().Single(a=>a.StoreProductId==HomeStoreService.ScratchPostId);
        var home=Read<CareAlignmentPolishTests>(review,"home");var cat=Read<CatMovement>(home,"cat");
        yield return Observe(post,home,cat);
        Vector3 ready=cat.transform.position;Quaternion facing=cat.transform.rotation;
        var centre=post.GetComponentInChildren<MeshCollider>().bounds.center;
        Quaternion towardBoard=Quaternion.LookRotation(Vector3.ProjectOnPlane(centre-ready,Vector3.up));
        try
        {
            foreach(float yaw in new[]{30f,-30f,40f,-40f,90f,-90f,180f})
            {
                Call(home,"Place",ready,towardBoard*Quaternion.Euler(0,yaw,0));
                Assert.That(post.CanStartFromPrompt(cat,out _),Is.False,"Side/back-facing pose must not offer scratch: "+yaw);
            }
        }
        finally{Call(home,"Place",ready,facing);}
        yield return null;yield return null;
        Assert.That(post.CanStartFromPrompt(cat,out _),Is.True,"Restored board-facing pose remains ready");
        var front=Vector3.ProjectOnPlane(Camera.main.transform.position-centre,Vector3.up).normalized;
        Call(home,"Place",centre-front*.45f,Quaternion.LookRotation(front));
        Assert.That(post.CanStartFromPrompt(cat,out _),Is.False,"Hidden far side must not offer scratch");
        Call(home,"Place",ready,facing);yield return null;yield return null;
        Assert.That(post.CanStartFromPrompt(cat,out _),Is.True);
        var points=Read<RaycastHit[]>(post,"pairedTop");
        var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.name="QA temporary scratch obstruction";
        block.transform.position=points[0].point+Vector3.ProjectOnPlane(points[0].normal,Vector3.up).normalized*.06f;
        block.transform.localScale=Vector3.one*.12f;Physics.SyncTransforms();
        Assert.That(post.CanStartFromPrompt(cat,out _),Is.False,"Fresh blocker must invalidate cached start");
        UnityEngine.Object.Destroy(block);yield return null;yield return null;
        Assert.That(post.CanStartFromPrompt(cat,out _),Is.True);
        var watch=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<8;i++)Assert.That(post.CanStartFromPrompt(cat,out _),Is.True);watch.Stop();
        File.WriteAllText(Path.Combine(Output,"warm-readiness-ms.txt"),(watch.Elapsed.TotalMilliseconds/8).ToString(System.Globalization.CultureInfo.InvariantCulture));
        int rewards=0;Action<CatActivity> completed=a=>{if(a==post)rewards++;};CatActivity.Completed+=completed;
        try
        {
            Call(review,"Tap",Call(review,"Select",post));Assert.That(post.IsRunning,Is.True);
            while(post.ScratchPhase<.45f)yield return new WaitForEndOfFrame();
            var bones=cat.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("DEF-hand.")||t.name.StartsWith("DEF-forearm.")).ToArray();
            var positions=bones.Select(t=>t.position).ToArray();float phase=post.ScratchPhase;int strokes=post.LeftStrokes+post.RightStrokes;
            Time.timeScale=0;
            for(int i=0;i<5;i++)yield return new WaitForEndOfFrame();
            Assert.That(post.ScratchPhase,Is.EqualTo(phase));Assert.That(post.LeftStrokes+post.RightStrokes,Is.EqualTo(strokes));
            for(int i=0;i<bones.Length;i++)Assert.That(Vector3.Distance(positions[i],bones[i].position),Is.LessThan(.001f),"Pause holds both paws: "+bones[i].name);
            post.CancelForTransition();Time.timeScale=1;yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(post.IsRunning||cat.IsMovementPhysicallyLocked,Is.False);Assert.That(rewards,Is.Zero);
            Assert.That(cat.GetComponent<CharacterController>().enabled,Is.True);
        }
        finally{Time.timeScale=1;post.CancelForTransition();CatActivity.Completed-=completed;}
    }
    [UnityTest,Timeout(180000)]public IEnumerator OtherToys()
    {
        SessionState.SetString("CatHome.QA.ReviewBreed","domestic-shorthair");yield return (IEnumerator)Call(review,"Boot");
        foreach(string id in new[]{HomeStoreService.ToyMouseId,HomeStoreService.PlayTunnelId})HomeStoreService.TrySetStored(id,false);
        yield return null;yield return null;
        foreach(string id in new[]{HomeStoreService.ToyMouseId,HomeStoreService.PlayTunnelId})
            yield return (IEnumerator)Call(review,"Observe",CatActivity.Registered.Single(a=>a.StoreProductId==id));
        var rows=Read<List<ActionReadyReviewTests.Row>>(review,"rows");Assert.That(rows.Count,Is.EqualTo(2));
        Assert.That(rows.All(r=>r.ready&&r.started&&r.completed&&r.released),Is.True);
    }
    [Serializable]sealed class ApproachResult
    {
        public string breed,rejectionReason;
        public float travel,heading,maximumRootDrift,maximumYawDrift;
        public int completions,releaseFrame,tapFrame,releaseFrames;
        public int readinessQueryCount;
        public double firstReadinessQueryMs,meanReadinessQueryMs,maximumReadinessQueryMs,subsequentMeanReadinessQueryMs,subsequentMaximumReadinessQueryMs;
        public double sourceCacheClearedReadyQueryMs,sourceCacheClearedStartQueryMs;
        public Vector3 target,closestPosition,releasePosition;
        public float closestTargetDistance=float.MaxValue,releaseTargetDistance,releaseHeading;
        public List<ApproachFrame> trajectory=new List<ApproachFrame>();
        public bool immediateRejected;
    }
    [Serializable]sealed class ApproachFrame
    {
        public int frame;
        public Vector3 position;
        public float targetDistance,heading;
        public bool ready;
    }
    [Serializable]sealed class ApproachSetupAttempt
    {
        public float distance;
        public Vector3 readyPosition,candidate,actualPosition,rejectedPosition;
        public Quaternion facing;
        public int rejectedStep=-1,sampleCount;
        public bool clear,ready;
        public float promptDistance;
        public double sourceCacheClearedQueryMs;
        public string blockers;
    }
    [Serializable]sealed class ApproachSetup
    {
        public string breed;
        public Vector3 readyPosition,direction,startPosition;
        public Quaternion facing;
        public int readyCandidates,omittedAttempts;
        public List<ApproachSetupAttempt> attempts=new List<ApproachSetupAttempt>();
    }
    static bool SourceCacheClearedReadiness(ScratchPostActivity post,CatMovement cat,out float distance,out double milliseconds)
    {
        ((IDictionary)typeof(CatPawReachResolver).GetField("sourceCache",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).Clear();
        var watch=System.Diagnostics.Stopwatch.StartNew();bool ready=post.CanStartFromPrompt(cat,out distance);watch.Stop();
        milliseconds=watch.Elapsed.TotalMilliseconds;return ready;
    }
    static string ColliderPath(Collider collider)
    {
        string path=collider.name;
        for(var parent=collider.transform.parent;parent!=null;parent=parent.parent)path=parent.name+"/"+path;
        return path+" ("+collider.GetType().Name+")";
    }
    static string DescribeBlockedPose(CatMovement cat,Vector3 position,Quaternion rotation,float tolerance=.015f)
    {
        var guard=Read<CatBodyGuard>(cat,"bodyGuard");var blocked=new List<string>();
        bool bodyClear=guard.IsPoseClear(position,rotation,tolerance,solid=>{blocked.Add("body: "+ColliderPath(solid));return false;});
        bool controllerClear=guard.IsControllerClear(position,rotation);
        if(!controllerClear)
        {
            var controller=cat.GetComponent<CharacterController>();Vector3 scale=cat.transform.lossyScale;
            float radius=controller.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
            float height=Mathf.Max(radius*2f,controller.height*Mathf.Abs(scale.y));
            Vector3 centre=position+rotation*Vector3.Scale(controller.center,scale),rise=Vector3.up*(height*.5f-radius);
            foreach(var solid in Physics.OverlapCapsule(centre-rise,centre+rise,radius,~0,QueryTriggerInteraction.Ignore))
            {
                if(!(bool)Call(guard,"Solid",solid))continue;
                object[] args={centre-rise,centre+rise,radius,solid,Vector3.zero,0f};
                if((bool)Call(guard,"Penetration",args)&&((Vector3)args[4]).y<.8f&&(float)args[5]>.002f)
                    blocked.Add("controller: "+ColliderPath(solid)+" depth="+((float)args[5]).ToString("R",System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        if(blocked.Count==0)blocked.Add("bodyClear="+bodyClear+", controllerClear="+controllerClear+" (no named blocker; inspect query capacity/profile)");
        return string.Join("; ",blocked);
    }
    bool FindClearApproachStart(ScratchPostActivity post,CareAlignmentPolishTests home,CatMovement cat,ApproachSetup trace,string directory)
    {
        Vector3 ready=cat.transform.position,direction=Vector3.ProjectOnPlane(cat.transform.forward,Vector3.up).normalized;
        Quaternion facing=cat.transform.rotation;trace.readyCandidates++;
        var guard=Read<CatBodyGuard>(cat,"bodyGuard");
        trace.readyPosition=ready;trace.direction=direction;trace.facing=facing;
        bool placed=false,found=false;
        try
        {
            foreach(float distance in new[]{.18f,.22f,.26f,.30f,.34f,.40f,.50f,.60f,.70f})
            {
                Vector3 candidate=ready-direction*distance;int samples=Mathf.Max(1,Mathf.CeilToInt(distance/.02f));
                var attempt=new ApproachSetupAttempt{readyPosition=ready,facing=facing,distance=distance,candidate=candidate,sampleCount=samples,clear=true};
                if(trace.attempts.Count==64){trace.attempts.RemoveAt(63);trace.omittedAttempts++;}
                trace.attempts.Add(attempt);
                for(int step=0;step<=samples;step++)
                {
                    Vector3 position=Vector3.Lerp(candidate,ready,(float)step/samples);
                    if(guard.IsPoseClear(position,facing,.0001f)&&guard.IsControllerClear(position,facing))continue;
                    attempt.clear=false;attempt.rejectedStep=step;attempt.rejectedPosition=position;
                    attempt.blockers=DescribeBlockedPose(cat,position,facing,.0001f);break;
                }
                if(!attempt.clear)continue;
                // Fixture search only: inspect the start, then restore the
                // proven ready pose before accepting or continuing the search.
                Call(home,"Place",candidate,facing);placed=true;attempt.actualPosition=cat.transform.position;
                attempt.ready=post.CanStartFromPrompt(cat,out attempt.promptDistance);
                if(attempt.ready)continue;
                trace.startPosition=candidate;found=true;break;
            }
        }
        finally
        {
            if(placed)
            {
                Call(home,"Place",ready,facing);
                Assert.That(post.CanStartFromPrompt(cat,out _),Is.True,"Approach fixture restores the accepted ready pose");
            }
            File.WriteAllText(Path.Combine(directory,"approach-setup.json"),JsonUtility.ToJson(trace,true));
        }
        return found;
    }
    [Serializable]sealed class AcceptedStance
    {
        public string breed,test;
        public int rhythm,frame;
        public Vector3 position,actualPosition,boardCentre,leftTop,leftBottom,rightTop,rightBottom,leftNormal,rightNormal;
        public Quaternion rotation;
        public float headingDegrees,sourcePhase,chestPitch,chestYaw,reachMargin,measuredReach;
        public bool both;
    }
    static void RecordAcceptedStance(ScratchPostActivity post,CatMovement cat,string directory=null)
    {
        Assert.That(post.TryGetPreparedStart(cat,out var accepted),Is.True,"Started scratch exposes its accepted plan without another resolver pass");
        var plan=accepted.PawPlan;var top=Read<RaycastHit[]>(post,"pairedTop");var bottom=Read<RaycastHit[]>(post,"pairedBottom");
        var centre=post.GetComponentInChildren<MeshCollider>().bounds.center;
        var snapshot=new AcceptedStance
        {
            breed=CatBreedService.SelectedBreedId,test=TestContext.CurrentContext.Test.Name,rhythm=post.RhythmVariation,frame=Time.frameCount,
            position=accepted.Position,actualPosition=cat.transform.position,rotation=accepted.Rotation,boardCentre=centre,
            headingDegrees=Vector3.Angle(Vector3.ProjectOnPlane(accepted.Rotation*Vector3.forward,Vector3.up),Vector3.ProjectOnPlane(centre-accepted.Position,Vector3.up)),
            sourcePhase=plan.SourcePhase,chestPitch=plan.ChestPitch,chestYaw=plan.ChestYaw,
            reachMargin=plan.ReachMargin,measuredReach=post.MeasuredReach,both=plan.Both,
            leftTop=top[0].point,leftBottom=bottom[0].point,rightTop=top[1].point,rightBottom=bottom[1].point,
            leftNormal=top[0].normal,rightNormal=top[1].normal
        };
        File.WriteAllText(Path.Combine(directory??Output,"accepted-stance-"+snapshot.breed+"-"+snapshot.test+"-rhythm"+snapshot.rhythm+".json"),JsonUtility.ToJson(snapshot,true));
    }
    [UnityTest,Timeout(360000)]public IEnumerator JoystickApproach_ThreeSizesFaceBoardAndCompleteWithoutRootSnap()
    {
        yield return JoystickApproach(new[]{"persian","domestic-shorthair","maine-coon"},2);
    }
    [UnityTest,Timeout(180000)]public IEnumerator JoystickImmediateRelease_HandoffRemainsContinuous()
    {
        yield return JoystickApproach(new[]{"domestic-shorthair"},0);
    }
    IEnumerator JoystickApproach(string[] breeds,int releaseFrames)
    {
        foreach(string breed in breeds)
        {
            SessionState.SetString("CatHome.QA.ReviewBreed",breed);yield return (IEnumerator)Call(review,"Boot");
            HomeStoreService.TrySetStored(HomeStoreService.ScratchPostId,false);yield return null;yield return null;
            var post=CatActivity.Registered.OfType<ScratchPostActivity>().Single(a=>a.StoreProductId==HomeStoreService.ScratchPostId);
            var home=Read<CareAlignmentPolishTests>(review,"home");var cat=Read<CatMovement>(home,"cat");
            string joystickOutput=Path.Combine(Output,(releaseFrames==0?"joystick-immediate-":"joystick-")+breed);Directory.CreateDirectory(joystickOutput);
            var setupTrace=new ApproachSetup{breed=breed};
            yield return FindReadyStance(post,home,cat,()=>FindClearApproachStart(post,home,cat,setupTrace,joystickOutput));
            Assert.That(SourceCacheClearedReadiness(post,cat,out _,out double coldReadyMs),Is.True,"Found ready stance remains ready with a cleared source cache");
            Vector3 ready=cat.transform.position,centre=post.GetComponentInChildren<MeshCollider>().bounds.center;
            Vector3 direction=Vector3.ProjectOnPlane(cat.transform.forward,Vector3.up).normalized;
            Quaternion facing=cat.transform.rotation;
            // This is fixture setup only. Once the pointer is pressed, movement
            // and orientation come exclusively from the real joystick/controller.
            Call(home,"Place",setupTrace.startPosition,facing);yield return null;yield return null;
            var selectedAttempt=setupTrace.attempts[setupTrace.attempts.Count-1];selectedAttempt.actualPosition=cat.transform.position;
            selectedAttempt.ready=SourceCacheClearedReadiness(post,cat,out selectedAttempt.promptDistance,out selectedAttempt.sourceCacheClearedQueryMs);
            File.WriteAllText(Path.Combine(joystickOutput,"approach-setup.json"),JsonUtility.ToJson(setupTrace,true));
            Assert.That(selectedAttempt.ready,Is.False,breed+" has a clear unready approach setup after settling");
            var skinProbe=cat.gameObject.AddComponent<FinalScratchProbe>();skinProbe.output=joystickOutput;skinProbe.breed=breed;
            var rearProbe=cat.gameObject.AddComponent<ScratchRearSourceProbe>();
            var motionProbe=cat.gameObject.AddComponent<ScratchReworkProbe>();motionProbe.output=joystickOutput;motionProbe.breed=breed;
            motionProbe.recordApproach=true;motionProbe.extraExitFrames=12;
            Vector3 approachStart=cat.transform.position;
            var result=new ApproachResult{breed=breed,releaseFrames=releaseFrames,sourceCacheClearedReadyQueryMs=coldReadyMs,
                sourceCacheClearedStartQueryMs=setupTrace.attempts[setupTrace.attempts.Count-1].sourceCacheClearedQueryMs};
            Action<CatActivity> completed=a=>{if(a==post)result.completions++;};CatActivity.Completed+=completed;
            try
            {
                Vector3 launchPosition=default;Quaternion launchRotation=default;
                // Execute the click in the same coroutine call as PointerUp so
                // nested iterator scheduling cannot silently add a release frame.
                Action begin=()=>
                {
                    result.travel=Vector3.Distance(approachStart,cat.transform.position);
                    Assert.That(result.travel,Is.GreaterThanOrEqualTo(.15f),breed+" reaches the board by walking");
                    AssertReadyGeometry(post,cat,centre);
                    result.heading=Vector3.Angle(Vector3.ProjectOnPlane(cat.transform.forward,Vector3.up),Vector3.ProjectOnPlane(centre-cat.transform.position,Vector3.up));
                    object button;
                    try{button=Call(review,"Select",post);}
                    catch(TargetInvocationException error) when(releaseFrames==0&&error.InnerException is AssertionException)
                    {
                        result.immediateRejected=true;result.rejectionReason="Production HUD did not admit the immediate click: "+error.InnerException.Message;
                        Assert.Ignore(result.rejectionReason);throw;
                    }
                    launchPosition=cat.transform.position;launchRotation=cat.transform.rotation;
                    result.tapFrame=Time.frameCount;
                    if(releaseFrames==0)Assert.That(result.tapFrame,Is.EqualTo(result.releaseFrame),"Immediate handoff must actually release and click in one frame");
                    Call(review,"Tap",button);
                    Assert.That(post.IsRunning,Is.True,breed+" real HUD starts scratch after joystick approach");
                    RecordAcceptedStance(post,cat,joystickOutput);
                    Assert.That(cat.transform.position,Is.EqualTo(launchPosition),"Click must not snap the root to a prepared stance");
                    Assert.That(Quaternion.Angle(cat.transform.rotation,launchRotation),Is.LessThan(.001f),"Click must not snap facing");
                };
                yield return WalkToScratch(post,cat,direction,ready,releaseFrames,result,begin);
                float until=Time.realtimeSinceStartup+40;
                while(post.IsRunning&&Time.realtimeSinceStartup<until)
                {
                    yield return new WaitForEndOfFrame();
                    result.maximumRootDrift=Mathf.Max(result.maximumRootDrift,Vector3.Distance(launchPosition,cat.transform.position));
                    result.maximumYawDrift=Mathf.Max(result.maximumYawDrift,Quaternion.Angle(launchRotation,cat.transform.rotation));
                }
                Assert.That(post.IsRunning,Is.False,breed+" scratch completes");
                Assert.That(result.completions,Is.EqualTo(1));
                Assert.That(result.maximumRootDrift,Is.LessThan(.001f),"Scratching must preserve the approached root position");
                Assert.That(result.maximumYawDrift,Is.LessThan(.001f),"Scratching must preserve the approached heading");
                Assert.That(post.LeftStrokes,Is.EqualTo(2));Assert.That(post.RightStrokes,Is.EqualTo(2));
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
                // Keep observing through the captured gait pose's handoff to
                // ordinary idle; release and Animator evaluation can straddle frames.
                for(int frame=0;frame<motionProbe.extraExitFrames;frame++)yield return new WaitForEndOfFrame();
                WriteScratchMetrics(joystickOutput,breed,skinProbe,motionProbe);
                AssertScratchEvidence(post,skinProbe,motionProbe);
            }
            finally
            {
                UnityEngine.Object.FindAnyObjectByType<MobileJoystick>()?.CancelInput();
                post.CancelForTransition();CatActivity.Completed-=completed;
                File.WriteAllText(Path.Combine(Output,(releaseFrames==0?"joystick-immediate-":"joystick-approach-")+breed+".json"),JsonUtility.ToJson(result,true));
                WriteScratchMetrics(joystickOutput,breed,skinProbe,motionProbe);
                motionProbe.recordApproach=false;
                // These are QA-only components. Dispose recordings before the
                // next breed boot unloads the scene, including assertion failures.
                UnityEngine.Object.DestroyImmediate(motionProbe);
                UnityEngine.Object.DestroyImmediate(rearProbe);
                UnityEngine.Object.DestroyImmediate(skinProbe);
            }
        }
    }
    IEnumerator WalkToScratch(ScratchPostActivity post,CatMovement cat,Vector3 direction,Vector3 target,int releaseFrames,ApproachResult result,Action onReady)
    {
        var joystick=UnityEngine.Object.FindAnyObjectByType<MobileJoystick>();Assert.That(joystick,Is.Not.Null);
        var cameraField=typeof(CatMovement).GetField("cameraTransform",F);
        var joystickField=typeof(CatMovement).GetField("mobileJoystick",F);
        var savedCamera=cameraField.GetValue(cat);var savedJoystick=joystickField.GetValue(cat);
        PointerEventData pointer=null;
        try
        {
            // Match the established care approach fixture's world-axis mapping,
            // restoring both scene references immediately after input ends.
            cameraField.SetValue(cat,null);joystickField.SetValue(cat,joystick);
            Canvas.ForceUpdateCanvases();
            var rect=(RectTransform)joystick.transform;
            var local=Vector2.Scale(new Vector2(direction.x,direction.z).normalized*.22f,rect.rect.size*.5f);
            var canvas=joystick.GetComponentInParent<Canvas>();
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(local));
            pointer=new PointerEventData(EventSystem.current){position=point,pointerId=29,pointerPressRaycast=new RaycastResult{module=canvas.GetComponent<GraphicRaycaster>()}};
            float until=Time.realtimeSinceStartup+20;
            result.target=target;
            joystick.OnPointerDown(pointer);
            bool ready;
            do
            {
                var watch=System.Diagnostics.Stopwatch.StartNew();ready=post.CanStartFromPrompt(cat,out _);watch.Stop();
                double milliseconds=watch.Elapsed.TotalMilliseconds;
                result.readinessQueryCount++;
                result.meanReadinessQueryMs+=(milliseconds-result.meanReadinessQueryMs)/result.readinessQueryCount;
                result.maximumReadinessQueryMs=Math.Max(result.maximumReadinessQueryMs,milliseconds);
                if(result.readinessQueryCount==1)result.firstReadinessQueryMs=milliseconds;
                else
                {
                    result.subsequentMeanReadinessQueryMs+=(milliseconds-result.subsequentMeanReadinessQueryMs)/(result.readinessQueryCount-1);
                    result.subsequentMaximumReadinessQueryMs=Math.Max(result.subsequentMaximumReadinessQueryMs,milliseconds);
                }
                Vector3 remaining=Vector3.ProjectOnPlane(target-cat.transform.position,Vector3.up);
                float targetDistance=remaining.magnitude,heading=Vector3.Angle(cat.transform.forward,direction);
                if(targetDistance<result.closestTargetDistance){result.closestTargetDistance=targetDistance;result.closestPosition=cat.transform.position;}
                if(result.trajectory.Count<256)result.trajectory.Add(new ApproachFrame{frame=Time.frameCount,position=cat.transform.position,targetDistance=targetDistance,heading=heading,ready=ready});
                // Reach the physical endpoint first; readiness can legitimately
                // differ while the Animator is still evaluating locomotion.
                if(targetDistance<=.008f||Vector3.Dot(remaining,direction)<=0f||Time.realtimeSinceStartup>=until)break;
                joystick.OnDrag(pointer);yield return null;
            }while(true);
            Assert.That(Vector3.ProjectOnPlane(target-cat.transform.position,Vector3.up).magnitude,Is.LessThan(.025f),"Real joystick reaches the selected clear endpoint; position="+cat.transform.position);
        }
        finally
        {
            if(pointer!=null)joystick.OnPointerUp(pointer);else joystick.CancelInput();
            result.releaseFrame=Time.frameCount;
            result.releasePosition=cat.transform.position;result.releaseTargetDistance=Vector3.ProjectOnPlane(target-cat.transform.position,Vector3.up).magnitude;
            result.releaseHeading=Vector3.Angle(cat.transform.forward,direction);
            cameraField.SetValue(cat,savedCamera);joystickField.SetValue(cat,savedJoystick);
        }
        for(int frame=0;frame<releaseFrames;frame++)yield return null;
        if(releaseFrames==0&&!post.CanStartFromPrompt(cat,out _))
        {
            result.immediateRejected=true;result.rejectionReason="Production readiness rejected same-frame joystick release; no forced start or delayed click was attempted.";
            Assert.Ignore(result.rejectionReason);
        }
        Assert.That(post.CanStartFromPrompt(cat,out _),Is.True,"Released joystick leaves a ready scratch stance");
        onReady();
    }
    IEnumerator Check(string breed)
    {
        SessionState.SetString("CatHome.QA.ReviewBreed",breed);yield return (IEnumerator)Call(review,"Boot");
        HomeStoreService.TrySetStored(HomeStoreService.ScratchPostId,false);yield return null;yield return null;
        var post=CatActivity.Registered.OfType<ScratchPostActivity>().Single(a=>a.StoreProductId==HomeStoreService.ScratchPostId);
        var home=Read<CareAlignmentPolishTests>(review,"home");var cat=Read<CatMovement>(home,"cat");
        var probe=cat.gameObject.AddComponent<FinalScratchProbe>();probe.output=Output;probe.breed=breed;
        cat.gameObject.AddComponent<ScratchRearSourceProbe>();
        var motion=cat.gameObject.AddComponent<ScratchReworkProbe>();motion.output=Output;motion.breed=breed;
        motion.extraExitFrames=12;
        for(int i=0;i<2;i++)
        {
            yield return Observe(post,home,cat);
            for(int frame=0;frame<motion.extraExitFrames;frame++)yield return new WaitForEndOfFrame();
            WriteScratchMetrics(Output,breed,probe,motion);
            Assert.That(post.LeftStrokes,Is.EqualTo(2));Assert.That(post.RightStrokes,Is.EqualTo(2));
            Assert.That(post.LastLeftSurfaceDistance,Is.LessThan(.003f));Assert.That(post.LastRightSurfaceDistance,Is.LessThan(.003f));
        }
        AssertScratchEvidence(post,probe,motion);
    }
    static void WriteScratchMetrics(string directory,string breed,FinalScratchProbe probe,ScratchReworkProbe motion)
    {
        File.WriteAllText(Path.Combine(directory,"metrics-"+breed+".json"),JsonUtility.ToJson(motion,true));
        File.WriteAllText(Path.Combine(directory,"skin-"+breed+".json"),JsonUtility.ToJson(probe,true));
    }
    static void AssertScratchEvidence(ScratchPostActivity post,FinalScratchProbe probe,ScratchReworkProbe motion)
    {
        Assert.That(post.LeftStrokes,Is.EqualTo(2));Assert.That(post.RightStrokes,Is.EqualTo(2));
        Assert.That(post.LastLeftSurfaceDistance,Is.LessThan(.003f));Assert.That(post.LastRightSurfaceDistance,Is.LessThan(.003f));
        Assert.That(motion.contactFrames,Is.GreaterThan(12));Assert.That(motion.hiddenPawFrames,Is.Zero);
        Assert.That(probe.maximumRootDrift,Is.LessThan(.001f));Assert.That(probe.maximumSkinPenetration,Is.LessThanOrEqualTo(.005f));
        Assert.That(motion.maximumRake,Is.GreaterThan(.02f));
        Assert.That(motion.maximumRecovery,Is.GreaterThan(.006f));
        Assert.That(motion.reversedElbowFrames,Is.Zero,"An elbow crossed its fixed bending plane");
        Assert.That(motion.outOfRangeElbowFrames,Is.Zero,"An elbow exceeded 40..165 degrees");
        Assert.That(motion.maximumJointStep,Is.LessThan(10f),"Joint direction jumped between adjacent 60 FPS frames");
        Assert.That(motion.maximumTransitionJointStep,Is.LessThan(10f),"Rise/settle or exit popped a joint between 60 FPS frames");
        Assert.That(motion.rearSupportFrames,Is.GreaterThan(motion.contactFrames),"Rear support is measured throughout rise, strokes and settle");
        Assert.That(motion.rearSourceMissedFrames,Is.Zero,"Each rear result needs this frame's independent source pose");
        Assert.That(motion.maximumMeasuredPelvisShift,Is.LessThanOrEqualTo(.0001f),"Pelvis remains at this frame's independently sampled source position");
        Assert.That(motion.maximumReportedRearDisplacement,Is.LessThanOrEqualTo(.0001f));
        Assert.That(motion.maximumMeasuredRearFootDisplacement,Is.LessThanOrEqualTo(.0001f),"Both rear feet retain the sampled source support points");
        Assert.That(motion.maximumMeasuredRearFootRotation,Is.LessThan(.1f),"Both rear feet retain source world orientation");
        Assert.That(motion.maximumMeasuredRearLengthChange,Is.LessThanOrEqualTo(.00001f),"Thigh-shin and shin-foot lengths remain authored");
        Assert.That(motion.reversedRearKneeFrames,Is.Zero,"Rear knees retain the source bending hemisphere");
        Assert.That(motion.degenerateRearKneeFrames,Is.Zero,"Rear solve must not create a straight/folded singularity from a bent source leg");
        Assert.That(motion.maximumRearKneePlaneDeviation,Is.LessThan(1f),"Rear knee plane follows the source plane around the new hip-foot axis");
    }
    IEnumerator Observe(ScratchPostActivity post,CareAlignmentPolishTests home,CatMovement cat)
    {
        yield return FindReadyStance(post,home,cat);
        yield return null;
        var button=Call(review,"Select",post);int completed=0;
        Action<CatActivity> handler=a=>{if(a==post)completed++;};CatActivity.Completed+=handler;
        try
        {
            Call(review,"Tap",button);Assert.That(post.IsRunning,Is.True,"Real HUD starts scratch");
            RecordAcceptedStance(post,cat);
            float end=Time.realtimeSinceStartup+90;
            while(post.IsRunning&&Time.realtimeSinceStartup<end)yield return new WaitForEndOfFrame();
            Assert.That(post.IsRunning,Is.False);Assert.That(completed,Is.EqualTo(1));
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
        }
        finally{CatActivity.Completed-=handler;}
    }
    IEnumerator FindReadyStance(ScratchPostActivity post,CareAlignmentPolishTests home,CatMovement cat,Func<bool> acceptReadyCandidate=null)
    {
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);Call(home,"ReadyNeeds");
        var body=post.GetComponentInChildren<MeshCollider>();Vector3 centre=body.bounds.center;
        Vector3 front=Vector3.ProjectOnPlane(Camera.main.transform.position-centre,Vector3.up).normalized;
        bool found=post.CanStartFromPrompt(cat,out _);int n=0;
        if(found&&acceptReadyCandidate!=null)found=acceptReadyCandidate();
        for(float radius=.34f;radius<=.70f&&!found;radius+=.01f)
        foreach(float angle in new[]{40f,50f,60f,30f,65f,20f,10f,0f})
        foreach(bool opposite in acceptReadyCandidate!=null?new[]{false,true}:new[]{false})
        foreach(float yaw in acceptReadyCandidate!=null?new[]{0f,5f,-5f,10f,-10f,14f,-14f}:new[]{0f,5f,-5f,10f,-10f})
        {
            if(found)break;
            Vector3 side=Quaternion.AngleAxis(-angle,Vector3.up)*front;
            // A world-right stance keeps the full cat away from the joystick.
            if(Vector3.Dot(side,Camera.main.transform.right)<0)side=Quaternion.AngleAxis(angle,Vector3.up)*front;
            if(opposite)side=Quaternion.AngleAxis(-Vector3.SignedAngle(front,side,Vector3.up),Vector3.up)*front;
            Call(home,"Place",centre+side*radius,Quaternion.LookRotation(-side)*Quaternion.Euler(0,yaw,0));
            if(post.CanStartFromPrompt(cat,out _))
            {
                yield return null;yield return null;
                found=post.CanStartFromPrompt(cat,out _);
                if(found&&acceptReadyCandidate!=null)found=acceptReadyCandidate();
            }
            if(++n%32==0)yield return null;
        }
        File.AppendAllText(Path.Combine(Output,"stance-search.txt"),CatBreedService.SelectedBreedId+" found="+found+" probes="+n+" root="+cat.transform.position+" yaw="+cat.transform.eulerAngles.y+"\n");
        Assert.That(found,Is.True,"Visible reachable stance");
        AssertReadyGeometry(post,cat,centre);
    }
    static void AssertReadyGeometry(ScratchPostActivity post,CatMovement cat,Vector3 centre)
    {
        float heading=Vector3.Angle(Vector3.ProjectOnPlane(cat.transform.forward,Vector3.up),Vector3.ProjectOnPlane(centre-cat.transform.position,Vector3.up));
        Assert.That(heading,Is.LessThanOrEqualTo(15f),"Cat faces the board before scratch starts");
        var points=Read<RaycastHit[]>(post,"pairedTop");
        Assert.That(Vector3.Distance(points[0].point,points[1].point),Is.GreaterThan(.001f),"Both paws need distinct surface points");
        Assert.That(Vector3.Dot(points[1].point-points[0].point,cat.transform.right),Is.GreaterThan(0f),"Left and right paw targets must not cross");
    }
}

// Independent source sample after animation grounding (500), before torso/rear
// correction (550). Compare each frame with its own natural source pose; do not
// mistake authored rear motion for planting drift or impose front-elbow limits.
[DefaultExecutionOrder(525)]
public sealed class ScratchRearSourceProbe:MonoBehaviour
{
    public sealed class Leg
    {
        public Transform thigh,shin,foot;
        public Vector3 footPosition,axis,bend;
        public Quaternion footRotation;
        public float upperLength,lowerLength,kneeAngle;
    }
    public readonly Leg[] legs={new Leg(),new Leg()};
    public Transform hips;
    public Vector3 hipsPosition,rootForward,rootRight;
    public int sampledFrame=-1;
    void Awake()
    {
        hips=CatBreedVisualFactory.FindDescendant(transform,"DEF-spine");
        for(int side=0;side<2;side++)
        {
            string suffix=side==0?"L":"R";var leg=legs[side];
            leg.thigh=CatBreedVisualFactory.FindDescendant(transform,"DEF-thigh."+suffix);
            leg.shin=CatBreedVisualFactory.FindDescendant(transform,"DEF-shin."+suffix);
            leg.foot=CatBreedVisualFactory.FindDescendant(transform,"DEF-foot."+suffix);
        }
    }
    void LateUpdate()
    {
        sampledFrame=-1;
        var post=CatActivity.Active as ScratchPostActivity;
        if(post==null||hips==null||!post.BelongsTo(GetComponent<CatMovement>()))return;
        hipsPosition=hips.position;rootForward=transform.forward;rootRight=transform.right;
        foreach(var leg in legs)
        {
            if(leg.thigh==null||leg.shin==null||leg.foot==null)return;
            leg.footPosition=leg.foot.position;leg.footRotation=leg.foot.rotation;
            leg.upperLength=Vector3.Distance(leg.thigh.position,leg.shin.position);
            leg.lowerLength=Vector3.Distance(leg.shin.position,leg.foot.position);
            leg.axis=(leg.foot.position-leg.thigh.position).normalized;
            leg.bend=Vector3.ProjectOnPlane(leg.shin.position-leg.thigh.position,leg.axis);
            leg.kneeAngle=Vector3.Angle(leg.thigh.position-leg.shin.position,leg.foot.position-leg.shin.position);
        }
        sampledFrame=Time.frameCount;
    }
}

public sealed class ScratchReworkProbe:MonoBehaviour
{
    public string output,breed;
    public bool recordApproach;
    public int extraExitFrames;
    int exitFramesRemaining;
    public int contactFrames,hiddenContactFrames,hiddenPawFrames;
    public float minimumVisiblePawFraction=1;
    Mesh visibleSkin;
    UnityEditor.Media.MediaEncoder encoder;int recordedFrames,recordedFps;
    public float maximumRake,maximumRecovery,minimumElbowAngle=180,maximumElbowAngle;
    public int reversedElbowFrames,outOfRangeElbowFrames;
    public float maximumJointStep;
    public float maximumTransitionJointStep;
    public string transitionPeakBone;public int transitionPeakFrame;public float transitionPeakPhase;public bool transitionPeakActive;
    public int transitionPeakActiveHand=-1;
    public float transitionPeakWeight,transitionPeakLeftWeight,transitionPeakRightWeight;
    public int rearSupportFrames,rearSourceMissedFrames,reversedRearKneeFrames,degenerateRearKneeFrames;
    public float maximumReportedRearDisplacement,maximumMeasuredPelvisShift,maximumMeasuredPelvisShiftStep;
    Vector3 previousPelvisOffset;
    public float maximumMeasuredRearFootDisplacement,maximumMeasuredRearFootRotation,maximumMeasuredRearLengthChange;
    public float minimumRearKneeAngle=180f,maximumRearKneeAngle,maximumRearKneeAngleStep,maximumRearKneePlaneDeviation;
    readonly float[] previousRearKneeAngles=new float[2];bool havePreviousRearKnees;
    Transform[] transitionBones;Quaternion[] transitionPoses;bool transitionReady,wasScratch;
    StreamWriter transitionTrace,rearTrace;
    readonly Quaternion[] previousJoints=new Quaternion[4];bool havePreviousJoints;
    public float deepestPhase;public int deepestHand;
    float penetration;
    readonly float[] top={float.NegativeInfinity,float.NegativeInfinity};
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly FieldInfo ContactLimbs=typeof(CatToyContactMotion).GetField("limbs",F);
    static float ContactWeight(CatToyContactMotion contact,int index)
    {
        if(contact==null)return 0f;
        var limbs=(Array)ContactLimbs.GetValue(contact);
        var limb=limbs.GetValue(index);
        return (float)limb.GetType().GetField("weight",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(limb);
    }
    IEnumerator Start()
    {
        while(true)
        {
            yield return new WaitForEndOfFrame();
            var post=CatActivity.Active as ScratchPostActivity;
            if(post!=null)exitFramesRemaining=extraExitFrames;
            var contact=GetComponent<CatToyContactMotion>();
            float leftWeight=ContactWeight(contact,0),rightWeight=ContactWeight(contact,1);
            if(transitionTrace==null&&post!=null)
            {
                transitionTrace=new StreamWriter(Path.Combine(output,"joint-trace-"+breed+".csv"),false);
                transitionTrace.WriteLine("frame,active,activeHand,strokePhase,leftWeight,rightWeight,bone,stepDegrees,rotationX,rotationY,rotationZ,rotationW");
            }
            if(transitionBones==null)
            {
                transitionBones=GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("DEF-upper_arm.")||t.name.StartsWith("DEF-forearm.")||t.name.StartsWith("DEF-thigh.")||t.name.StartsWith("DEF-shin.")||t.name.StartsWith("DEF-foot.")||t.name=="DEF-spine.001"||t.name=="DEF-spine").ToArray();
                transitionPoses=new Quaternion[transitionBones.Length];
            }
            for(int bone=0;bone<transitionBones.Length;bone++)
            {
                var current=transitionBones[bone].rotation;
                if(transitionReady&&(post!=null||wasScratch||exitFramesRemaining>0))
                {
                    float delta=Quaternion.Angle(transitionPoses[bone],current);
                    if(delta>maximumTransitionJointStep)
                    {
                        maximumTransitionJointStep=delta;transitionPeakBone=transitionBones[bone].name;transitionPeakFrame=Time.frameCount;
                        transitionPeakPhase=post!=null?post.ScratchPhase:-1;transitionPeakActive=post!=null;
                        transitionPeakActiveHand=post!=null?post.ActiveScratchHand:-1;
                        transitionPeakLeftWeight=leftWeight;transitionPeakRightWeight=rightWeight;
                        transitionPeakWeight=transitionPeakBone.Contains(".L")?leftWeight:transitionPeakBone.Contains(".R")?rightWeight:Mathf.Max(leftWeight,rightWeight);
                    }
                    transitionTrace?.WriteLine(FormattableString.Invariant($"{Time.frameCount},{(post!=null?1:0)},{(post!=null?post.ActiveScratchHand:-1)},{(post!=null?post.ScratchPhase:-1)},{leftWeight},{rightWeight},{transitionBones[bone].name},{delta},{current.x},{current.y},{current.z},{current.w}"));
                }
                transitionPoses[bone]=current;
            }
            transitionReady=true;wasScratch=post!=null;
            if(post==null&&exitFramesRemaining>0)exitFramesRemaining--;
            if(EditorApplication.isPlaying&&SessionState.GetBool("CatHome.QA.ScratchRecord",false)&&(post!=null||recordApproach))
            {
                if(encoder==null)
                {
                    recordedFps=Time.captureFramerate;
                    encoder=new UnityEditor.Media.MediaEncoder(Path.Combine(output,"Scratch-"+breed+".mp4"),new UnityEditor.Media.VideoTrackAttributes{frameRate=new UnityEditor.Media.MediaRational(recordedFps),width=(uint)Screen.width,height=(uint)Screen.height,includeAlpha=false,bitRateMode=UnityEditor.VideoBitrateMode.High});
                }
                var image=ScreenCapture.CaptureScreenshotAsTexture();Assert.That(encoder.AddFrame(image),Is.True);recordedFrames++;UnityEngine.Object.Destroy(image);
            }
            if(post==null){havePreviousRearKnees=false;continue;}
            MeasureRearSupport(post);
            var plans=(CatPawSurfacePlan[])typeof(ScratchPostActivity).GetField("pairedSkin",F).GetValue(post);
            var cam=Camera.main;
            var skin=GetComponent<FinalScratchProbe>();
            if(post.ActiveScratchHand>=0)
            {
                for(int side=0;side<2;side++)
                {
                    string suffix=side==0?"L":"R";
                    var upper=CatBreedVisualFactory.FindDescendant(transform,"DEF-upper_arm."+suffix);
                    var elbow=CatBreedVisualFactory.FindDescendant(transform,"DEF-forearm."+suffix);
                    var wrist=CatBreedVisualFactory.FindDescendant(transform,"DEF-hand."+suffix);
                    float bend=Vector3.Angle(upper.position-elbow.position,wrist.position-elbow.position);
                    if(bend<39.8f||bend>165.2f)outOfRangeElbowFrames++;
                    Vector3 axis=(wrist.position-upper.position).normalized;
                    Vector3 pole=Vector3.ProjectOnPlane(-transform.forward*.65f-Vector3.up,axis).normalized;
                    Vector3 actual=Vector3.ProjectOnPlane(elbow.position-upper.position,axis).normalized;
                    if(Vector3.Dot(pole,actual)<.98f)reversedElbowFrames++;
                    for(int joint=0;joint<2;joint++)
                    {
                        int index=side*2+joint;Quaternion current=joint==0?upper.rotation:elbow.rotation;
                        if(havePreviousJoints)maximumJointStep=Mathf.Max(maximumJointStep,Quaternion.Angle(previousJoints[index],current));
                        previousJoints[index]=current;
                    }
                }
                havePreviousJoints=true;
            }
            else havePreviousJoints=false;
            if(skin.maximumSkinPenetration>penetration){penetration=skin.maximumSkinPenetration;deepestPhase=post.ScratchPhase;deepestHand=post.ActiveScratchHand;}
            for(int side=0;side<2;side++)
            {
                if(post.ActiveScratchHand!=side)continue;
                var plan=plans[side];if(plan==null)continue;
                var p=side==0?contact.LeftContactPosition:contact.RightContactPosition;
                string suffix=side==0?"L":"R";
                var arm=CatBreedVisualFactory.FindDescendant(transform,"DEF-upper_arm."+suffix);
                var fore=CatBreedVisualFactory.FindDescendant(transform,"DEF-forearm."+suffix);
                var hand=CatBreedVisualFactory.FindDescendant(transform,"DEF-hand."+suffix);
                float angle=Vector3.Angle(arm.position-fore.position,hand.position-fore.position);
                minimumElbowAngle=Mathf.Min(minimumElbowAngle,angle);maximumElbowAngle=Mathf.Max(maximumElbowAngle,angle);
                if(post.ScratchPhase>=.40f&&post.ScratchPhase<=.65f)
                {
                    contactFrames++;top[side]=Mathf.Max(top[side],p.y);maximumRake=Mathf.Max(maximumRake,top[side]-p.y);
                    Vector3 ray=p-cam.transform.position;var v=cam.WorldToViewportPoint(p);
                    bool hidden=plan.Collider.Raycast(new Ray(cam.transform.position,ray.normalized),out var block,ray.magnitude-.006f);
                    if(v.z<=0||v.x<.06f||v.x>.94f||v.y<.10f||v.y>.9f||hidden)hiddenContactFrames++;
                    // A contact buried in a rope groove need not itself face
                    // the camera. Measure the visible real paw surface as well.
                    var renderer=GetComponentInChildren<SkinnedMeshRenderer>();
                    if(visibleSkin==null)visibleSkin=new Mesh();renderer.BakeMesh(visibleSkin,true);
                    var vertices=visibleSkin.vertices;int visible=0,total=0;
                    foreach(var def in plan.Definitions)
                    {
                        if(!def.distal)continue;total++;
                        Vector3 point=renderer.transform.TransformPoint(vertices[def.vertexIndex]);var vp=cam.WorldToViewportPoint(point);
                        Vector3 sight=point-cam.transform.position;
                        if(vp.z>0&&vp.x>.06f&&vp.x<.94f&&vp.y>.10f&&vp.y<.9f&&!plan.Collider.Raycast(new Ray(cam.transform.position,sight.normalized),out _,sight.magnitude-.003f))visible++;
                    }
                    float fraction=(float)visible/Mathf.Max(1,total);minimumVisiblePawFraction=Mathf.Min(minimumVisiblePawFraction,fraction);
                    if(fraction<.60f)hiddenPawFrames++;
                    File.AppendAllText(Path.Combine(output,"contact-trace-"+breed+".csv"),string.Join(",",side,post.ScratchPhase,Vector3.Distance(p,plan.Point),hidden,hidden?ray.magnitude-block.distance:0,p.x,p.y,p.z,plan.Point.x,plan.Point.y,plan.Point.z)+"\n");
                }
                else if(plan.Collider.Raycast(new Ray(p+plan.Normal*.5f,-plan.Normal),out var hit,1f))
                    maximumRecovery=Mathf.Max(maximumRecovery,Vector3.Dot(p-hit.point,plan.Normal));
            }
        }
    }
    void MeasureRearSupport(ScratchPostActivity post)
    {
        var reach=GetComponent<CatPawReachMotion>();
        if(reach!=null)
            maximumReportedRearDisplacement=Mathf.Max(maximumReportedRearDisplacement,reach.RearSupportDisplacement);
        var source=GetComponent<ScratchRearSourceProbe>();
        if(source==null||source.sampledFrame!=Time.frameCount){rearSourceMissedFrames++;return;}
        rearSupportFrames++;
        Vector3 pelvisOffset=source.hips.position-source.hipsPosition;
        float forward=Vector3.Dot(pelvisOffset,source.rootForward),vertical=Vector3.Dot(pelvisOffset,Vector3.up);
        float lateral=Vector3.Dot(pelvisOffset,source.rootRight);
        maximumMeasuredPelvisShift=Mathf.Max(maximumMeasuredPelvisShift,pelvisOffset.magnitude);
        if(havePreviousRearKnees)maximumMeasuredPelvisShiftStep=Mathf.Max(maximumMeasuredPelvisShiftStep,Vector3.Distance(previousPelvisOffset,pelvisOffset));
        previousPelvisOffset=pelvisOffset;
        if(rearTrace==null)
        {
            rearTrace=new StreamWriter(Path.Combine(output,"rear-trace-"+breed+".csv"),false);
            rearTrace.WriteLine("frame,side,activeHand,sourceKneeDegrees,kneeDegrees,kneeStepDegrees,planeDeviationDegrees,footDisplacement,footRotationDegrees,upperLengthChange,lowerLengthChange,pelvisForward,pelvisVertical,pelvisLateral");
        }
        for(int side=0;side<2;side++)
        {
            var leg=source.legs[side];
            float displacement=Vector3.Distance(leg.footPosition,leg.foot.position),rotation=Quaternion.Angle(leg.footRotation,leg.foot.rotation);
            float upperChange=Mathf.Abs(leg.upperLength-Vector3.Distance(leg.thigh.position,leg.shin.position));
            float lowerChange=Mathf.Abs(leg.lowerLength-Vector3.Distance(leg.shin.position,leg.foot.position));
            maximumMeasuredRearFootDisplacement=Mathf.Max(maximumMeasuredRearFootDisplacement,displacement);
            maximumMeasuredRearFootRotation=Mathf.Max(maximumMeasuredRearFootRotation,rotation);
            maximumMeasuredRearLengthChange=Mathf.Max(maximumMeasuredRearLengthChange,Mathf.Max(upperChange,lowerChange));
            float knee=Vector3.Angle(leg.thigh.position-leg.shin.position,leg.foot.position-leg.shin.position);
            minimumRearKneeAngle=Mathf.Min(minimumRearKneeAngle,knee);maximumRearKneeAngle=Mathf.Max(maximumRearKneeAngle,knee);
            float step=havePreviousRearKnees?Mathf.Abs(knee-previousRearKneeAngles[side]):0f;
            maximumRearKneeAngleStep=Mathf.Max(maximumRearKneeAngleStep,step);previousRearKneeAngles[side]=knee;
            Vector3 axis=(leg.foot.position-leg.thigh.position).normalized;
            Vector3 actual=Vector3.ProjectOnPlane(leg.shin.position-leg.thigh.position,axis);
            Vector3 expected=Vector3.ProjectOnPlane(Quaternion.FromToRotation(leg.axis,axis)*leg.bend,axis);
            float planeDeviation=0f;
            if(leg.bend.sqrMagnitude>.00000001f)
            {
                if(actual.sqrMagnitude<=.00000001f||expected.sqrMagnitude<=.00000001f)degenerateRearKneeFrames++;
                else
                {
                    if(Vector3.Dot(actual,expected)<=0f)reversedRearKneeFrames++;
                    planeDeviation=Vector3.Angle(actual,expected);
                    maximumRearKneePlaneDeviation=Mathf.Max(maximumRearKneePlaneDeviation,planeDeviation);
                }
            }
            rearTrace.WriteLine(FormattableString.Invariant($"{Time.frameCount},{side},{post.ActiveScratchHand},{leg.kneeAngle},{knee},{step},{planeDeviation},{displacement},{rotation},{upperChange},{lowerChange},{forward},{vertical},{lateral}"));
        }
        havePreviousRearKnees=true;
    }
    void OnDestroy()
    {
        transitionTrace?.Dispose();rearTrace?.Dispose();
        encoder?.Dispose();
        if(recordedFrames>0)File.WriteAllText(Path.Combine(output,"video-"+breed+".json"),"{\"frames\":"+recordedFrames+",\"fps\":"+recordedFps+"}");
        if(visibleSkin!=null)UnityEngine.Object.Destroy(visibleSkin);
    }
}
#endif
