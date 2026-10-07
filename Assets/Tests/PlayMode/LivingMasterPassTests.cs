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
using UnityEngine.TestTools;

public sealed class LivingMasterPassTests
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    ActionReadyReviewTests review;
    static T Read<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    [SetUp] public void Before(){review=new ActionReadyReviewTests();review.Before();SessionState.SetString("CatHome.QA.ReviewBreed","domestic-shorthair");SessionState.SetString("CatHome.QA.ReviewOnly","");}
    [TearDown] public void After(){review.After();}
    [UnityTest] public IEnumerator TunnelEnvelope_Diagnostic()
    {
        SessionState.SetString("CatHome.QA.ReviewBreed","maine-coon");
        yield return (IEnumerator)Call(review,"Boot");HomeStoreService.TrySetStored(HomeStoreService.PlayTunnelId,false);yield return null;yield return null;
        var home=Read<CareAlignmentPolishTests>(review,"home");var cat=Read<CatMovement>(home,"cat");
        var tunnel=CatActivity.Registered.OfType<CatEnrichmentActivity>().Single(a=>a.StoreProductId==HomeStoreService.PlayTunnelId);
        Call(home,"Place",tunnel.RoutineEntryPoint.position,Quaternion.LookRotation(tunnel.ExitPoint.position-tunnel.RoutineEntryPoint.position));
        LivingTunnelPassage.Resolve(tunnel,cat,out _,out var plan);
        string before=LivingTunnelPassage.LastRejection;
        Call(home,"Place",plan.entry,Quaternion.LookRotation(plan.axis));yield return null;
        bool ready=LivingTunnelPassage.Resolve(tunnel,cat,out _,out plan);
        string probes="";var axis=plan.axis;var origin=plan.entry;
        for(int i=0;i<12;i++){Call(home,"Place",origin-axis*(i*.05f),Quaternion.LookRotation(axis));bool ok=LivingTunnelPassage.Resolve(tunnel,cat,out _,out var testPlan);probes+="\n"+i+" ready="+ok+" pose="+cat.IsInteractionPoseClear(cat.transform.position,cat.transform.rotation)+" reason="+LivingTunnelPassage.LastRejection;}
        File.WriteAllText(Path.Combine(SessionState.GetString("CatHome.QA.ResultDirectory","Temp"),"tunnel-envelope.txt"),"Before: "+before+"\nReady: "+ready+"\nEntry: "+plan.entry+"\nExit: "+plan.exit+"\nHeight: "+plan.bodyHeight+"\nWidth: "+plan.bodyWidth+"\nRoof: "+plan.roof+probes);
    }
    [UnityTest,Timeout(240000)] public IEnumerator ScratchAndTunnel_Small_RealButtons(){yield return MotionBreed("persian");}
    [UnityTest,Timeout(240000)] public IEnumerator ScratchAndTunnel_Large_RealButtons(){yield return MotionBreed("maine-coon");}
    IEnumerator MotionBreed(string breed)
    {
        SessionState.SetString("CatHome.QA.ReviewBreed",breed);
        yield return ScratchAndTunnel_CurrentBreed_RealButtons();
    }
    [UnityTest,Timeout(240000)] public IEnumerator ScratchAndTunnel_CurrentBreed_RealButtons()
    {
        yield return (IEnumerator)Call(review,"Boot");
        foreach(string id in new[]{HomeStoreService.ScratchPostId,HomeStoreService.PlayTunnelId})HomeStoreService.TrySetStored(id,false);
        yield return null;yield return null;
        var home=Read<CareAlignmentPolishTests>(review,"home");var cat=Read<CatMovement>(home,"cat");
        var probe=cat.gameObject.AddComponent<LivingMasterMotionProbe>();
        foreach(string id in new[]{HomeStoreService.ScratchPostId,HomeStoreService.PlayTunnelId,HomeStoreService.ScratchPostId})
        {
            var activity=CatActivity.Registered.Single(a=>a.StoreProductId==id);
            yield return (IEnumerator)Call(review,"Observe",activity);
            if(id==HomeStoreService.PlayTunnelId)
            {
                var tunnel=(CatEnrichmentActivity)activity;
                LivingTunnelPassage.Resolve(tunnel,cat,out _,out var reverse);
                Call(home,"Place",reverse.entry-reverse.axis*.10f,Quaternion.LookRotation(reverse.axis));yield return null;
                Assert.That(activity.CanStartFromPrompt(cat,out _),Is.True,"Reverse opening");
                var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.name="QA temporary blocked tunnel";
                block.transform.position=(reverse.entry+reverse.exit)*.5f+Vector3.up*.25f;block.transform.localScale=new Vector3(.25f,.5f,.25f);Physics.SyncTransforms();
                Assert.That(activity.CanStartFromPrompt(cat,out _),Is.False,"Fresh obstruction hides the action");
                UnityEngine.Object.Destroy(block);yield return null;yield return null;
                Call(review,"Tap",Call(review,"Select",activity));Assert.That(activity.IsRunning,Is.True);
                float end=Time.time+20;while(activity.IsRunning&&Time.time<end)yield return null;
                Assert.That(activity.IsRunning,Is.False);Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
            }
        }
        var rows=Read<List<ActionReadyReviewTests.Row>>(review,"rows");Call(review,"Write",true);
        Assert.That(rows.Count,Is.EqualTo(3));
        var post=CatActivity.Registered.OfType<ScratchPostActivity>().Single(a=>a.StoreProductId==HomeStoreService.ScratchPostId);
        probe.left=post.LeftStrokes;probe.right=post.RightStrokes;probe.rhythm=post.RhythmVariation;
        File.WriteAllText(Path.Combine(SessionState.GetString("CatHome.QA.ResultDirectory","Temp"),"motion-skin-"+CatBreedService.SelectedBreedId+".json"),JsonUtility.ToJson(probe,true));
        Assert.That(post.RhythmVariation,Is.EqualTo(1));
        Assert.That(probe.maximumTunnelPenetration,Is.LessThanOrEqualTo(.015f));
        Assert.That(probe.maximumScratchRootDrift,Is.LessThan(.001f));
        Assert.That(rows.Where(r=>!r.ready||!r.started||!r.completed||!r.released).Select(r=>r.id+": "+r.error),Is.Empty);
    }
    [UnityTest,Timeout(240000)] public IEnumerator SleepAndPearl_VisualReview()
    {
        yield return (IEnumerator)Call(review,"Boot");
        var home=Read<CareAlignmentPolishTests>(review,"home");
        var cat=Read<CatMovement>(home,"cat");
        var sleep=Read<SleepInteraction>(home,"sleep");
        Read<EnergySystem>(home,"energy").ApplySavedValue(0);
        var floor=Read<Transform>(sleep,"bedInteractionPoint");var target=sleep.SleepSurface;
        Call(home,"Place",floor.position,Quaternion.LookRotation(Vector3.ProjectOnPlane(target.position-floor.position,Vector3.up)));
        yield return null;
        Assert.That(sleep.TryHandleActionButton(),Is.True);
        yield return new WaitForSeconds(6f);
        Assert.That(sleep.IsSettledOnBed,Is.True);
        Assert.That(cat.GetComponent<CatSleepZzzEffect>().IsPlaying,Is.True);
        yield return new WaitForEndOfFrame();
        var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(SessionState.GetString("CatHome.QA.ResultDirectory","Temp"),"sleep-review.png"),texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
        if(SessionState.GetBool("CatHome.QA.HoldMasterPreview",false))yield return new WaitForSecondsRealtime(35);
        Assert.That(sleep.TryHandleActionButton(),Is.True);
        float end=Time.time+15;while(sleep.IsSleeping&&Time.time<end)yield return null;
        Assert.That(sleep.IsSleeping,Is.False);Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
    }
    void Recording(bool value)=>typeof(ActionReadyReviewTests).GetField("record",F).SetValue(review,value);
    IEnumerator VideoFrames(float seconds)=>(IEnumerator)Call(review,"Frames",seconds);
    [UnityTest,Timeout(600000)] public IEnumerator Showcase_RealGameView_28Seconds()
    {
        SessionState.SetString("CatHome.QA.ReviewBreed","domestic-shorthair-orange");
        yield return (IEnumerator)Call(review,"Boot");Time.captureFramerate=24;Time.timeScale=1;
        GameLanguageService.SetLanguage(GameLanguage.Turkish);
        foreach(string id in new[]{HomeStoreService.ScratchPostId,HomeStoreService.PlayTunnelId,HomeStoreService.CeramicBowlId,HomeStoreService.FeatherToyId,HomeStoreService.CloudBedId})Assert.That(HomeStoreService.TrySetStored(id,false),Is.True);
        yield return null;yield return null;
        string output=Path.Combine(SessionState.GetString("CatHome.QA.ResultDirectory","Temp"),"CatHome_Salon_MasterPass.mp4");
        var encoder=new UnityEditor.Media.MediaEncoder(output,new UnityEditor.Media.VideoTrackAttributes{frameRate=new UnityEditor.Media.MediaRational(24),width=(uint)Screen.width,height=(uint)Screen.height,includeAlpha=false,bitRateMode=VideoBitrateMode.High});
        typeof(ActionReadyReviewTests).GetField("encoder",F).SetValue(review,encoder);Recording(true);
        foreach(string id in new[]{HomeStoreService.ScratchPostId,HomeStoreService.PlayTunnelId})yield return (IEnumerator)Call(review,"Observe",CatActivity.Registered.Single(a=>a.StoreProductId==id));
        Recording(false);var home=Read<CareAlignmentPolishTests>(review,"home");var cat=Read<CatMovement>(home,"cat");
        Call(home,"Place",new Vector3(-.5f,.05f,-.7f),Quaternion.Euler(0,130,0));yield return null;
        var speech=CatSpeechBubble.EnsureOn(cat);Call(review,"BeginChapter","Salon sohbetleri");Recording(true);Call(review,"BeginChapter","Salon sohbetleri");
        for(int i=0;i<2;i++){speech.ShowLocalizedOwned(cat,LivingRoomSpeech.Next("scratch",true));yield return VideoFrames(1.25f);}
        speech.DismissOwned(cat);Recording(false);
        var sleep=Read<SleepInteraction>(home,"sleep");Read<EnergySystem>(home,"energy").ApplySavedValue(0);
        Call(home,"FindBedStance");yield return null;yield return null;
        Recording(true);Call(review,"BeginChapter","Sakin bir uyku");Call(Read<BowlInteraction>(home,"bowls"),"Update");
        Call(review,"Tap",Call(home,"SleepButton"));Assert.That(sleep.IsSleeping,Is.True);
        yield return VideoFrames(6f);Assert.That(sleep.IsSettledOnBed,Is.True);yield return Shot("showcase-sleep");Recording(false);
        Assert.That(sleep.TryHandleActionButton(),Is.True);float until=Time.time+15;while(sleep.IsSleeping&&Time.time<until)yield return null;Assert.That(sleep.IsSleeping,Is.False);
        var popup=UnityEngine.Object.FindAnyObjectByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include);popup.enabled=true;
        Call(popup,"Populate",new CatHomeSaveSystem.OfflineReturnSummary(765432,TimeSpan.FromMinutes(35),true,true,75,62,80,61,28,67));
        Recording(true);Call(review,"BeginChapter","Yeniden ho\u015f geldin");Call(popup,"Show");yield return VideoFrames(3f);yield return Shot("showcase-welcome");Recording(false);
        Call(review,"Tap",Read<UnityEngine.UI.Button>(popup,"welcomeBackButton"));yield return new WaitForSeconds(.7f);
        var reward=UnityEngine.Object.FindAnyObjectByType<HomeLevelUpCelebrationView>(FindObjectsInactive.Include);reward.enabled=true;
        Recording(true);Call(review,"BeginChapter","K\u00fc\u00e7\u00fck ba\u015far\u0131lar");reward.Show(4);yield return VideoFrames(3f);yield return Shot("showcase-reward");
        int frames=Read<int>(review,"frame");if(frames<28*24)yield return VideoFrames((28*24-frames)/24f);
        frames=Read<int>(review,"frame");Recording(false);encoder.Dispose();typeof(ActionReadyReviewTests).GetField("encoder",F).SetValue(review,null);
        File.WriteAllText(Path.Combine(SessionState.GetString("CatHome.QA.ResultDirectory","Temp"),"showcase-metadata.json"),"{\"frames\":"+frames+",\"fps\":24,\"width\":"+Screen.width+",\"height\":"+Screen.height+",\"audio\":false}");
        Assert.That(frames,Is.InRange(20*24,30*24));
        Call(review,"Tap",Read<UnityEngine.UI.Button>(reward,"collectButton"));yield return new WaitForSeconds(.8f);
        var rows=Read<List<ActionReadyReviewTests.Row>>(review,"rows");Assert.That(rows.All(r=>r.ready&&r.started&&r.completed&&r.released),Is.True);Call(review,"Write",true);
    }
    [UnityTest,Timeout(120000)] public IEnumerator Showcase_Playback_AllFramesDecode()
    {
        yield return (IEnumerator)Call(review,"Boot");Time.captureFramerate=0;
        var root=new GameObject("QA exported video playback",typeof(Canvas),typeof(UnityEngine.UI.RawImage));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=500;
        var texture=new RenderTexture(1920,1080,0);root.GetComponent<UnityEngine.UI.RawImage>().texture=texture;root.GetComponent<UnityEngine.UI.RawImage>().raycastTarget=false;
        var player=root.AddComponent<UnityEngine.Video.VideoPlayer>();player.playOnAwake=false;player.isLooping=false;player.skipOnDrop=false;player.sendFrameReadyEvents=true;
        player.audioOutputMode=UnityEngine.Video.VideoAudioOutputMode.None;player.renderMode=UnityEngine.Video.VideoRenderMode.RenderTexture;player.targetTexture=texture;
        player.url=Path.GetFullPath(Path.Combine(SessionState.GetString("CatHome.QA.ResultDirectory","Temp"),"CatHome_Salon_MasterPass.mp4"));
        bool ended=false;string error="";long highest=-1;int decoded=0;
        player.loopPointReached+=_=>ended=true;player.errorReceived+=(_,message)=>error=message;player.frameReady+=(_,frame)=>{highest=Math.Max(highest,frame);decoded++;};
        try
        {
            player.Prepare();float deadline=Time.realtimeSinceStartup+12;while(!player.isPrepared&&error.Length==0&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(error,Is.Empty);Assert.That(player.isPrepared,Is.True);Assert.That(player.frameCount,Is.InRange(671ul,672ul));Assert.That(player.length,Is.EqualTo(28).Within(.05));
            player.Play();deadline=Time.realtimeSinceStartup+45;int shot=0;
            while(!ended&&error.Length==0&&Time.realtimeSinceStartup<deadline)
            {
                if(player.time>=Mathf.Max(.1f,shot*6)&&shot<5){yield return Shot("playback-"+shot);shot++;}
                yield return null;
            }
            File.WriteAllText(Path.Combine(SessionState.GetString("CatHome.QA.ResultDirectory","Temp"),"video-playback.json"),"{\"ended\":"+ended.ToString().ToLowerInvariant()+",\"decodedEvents\":"+decoded+",\"highestFrame\":"+highest+",\"reportedFrames\":"+player.frameCount+",\"length\":"+player.length.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
            Assert.That(error,Is.Empty);Assert.That(ended,Is.True);Assert.That(highest,Is.GreaterThanOrEqualTo(670));
        }
        finally{player.Stop();texture.Release();UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(root);}
    }
    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(SessionState.GetString("CatHome.QA.ResultDirectory","Temp"),name+".png"),texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
    }
    [UnityTest,Timeout(240000)] public IEnumerator SpeechWelcomeAndReward_VisualsAndRealDismiss()
    {
        yield return (IEnumerator)Call(review,"Boot");
        var home=Read<CareAlignmentPolishTests>(review,"home");var cat=Read<CatMovement>(home,"cat");
        Call(home,"Place",new Vector3(-.4f,.05f,-.8f),Quaternion.Euler(0,130,0));
        var speech=CatSpeechBubble.EnsureOn(cat);
        foreach(var language in new[]{GameLanguage.Turkish,GameLanguage.English})
        {
            GameLanguageService.SetLanguage(language);var seen=new HashSet<string>();
            for(int i=0;i<3;i++)
            {
                string text=LivingRoomSpeech.Next("scratch",false);Assert.That(seen.Add(text),Is.True);
                speech.ShowLocalizedOwned(null,text);
                yield return new WaitForSeconds(.35f);
                var label=Read<TMPro.TMP_Text>(speech,"label");Assert.That(label.isTextOverflowing,Is.False);
                Assert.That(Read<CanvasGroup>(speech,"group").alpha,Is.GreaterThan(.95f));
                yield return Shot("bubble-"+language+"-"+i);
            }
        }
        foreach(var language in new[]{GameLanguage.Turkish,GameLanguage.English})
        foreach(string context in new[]{"scratch","tunnel","spring","mouse","food","water","sleep","rest","ribbon","ball","grass","play","watch","hide","puzzle"})
        foreach(bool after in new[]{false,true})
        {GameLanguageService.SetLanguage(language);var unique=new HashSet<string>();for(int i=0;i<3;i++)Assert.That(unique.Add(LivingRoomSpeech.Next(context,after)),Is.True,context);}
        GameLanguageService.SetLanguage(GameLanguage.Turkish);
        var popup=UnityEngine.Object.FindAnyObjectByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include);
        popup.enabled=true;
        Call(popup,"Populate",new CatHomeSaveSystem.OfflineReturnSummary(987654,TimeSpan.FromMinutes(35),true,true,75,62,80,61,28,67));
        Call(popup,"Show");yield return new WaitForSeconds(1.5f);Assert.That(popup.IsOpen,Is.True);
        Assert.That(Read<CanvasGroup>(speech,"group").alpha,Is.Zero,"World bubble is hidden behind modal");
        yield return Shot("welcome-pearl");
        if(SessionState.GetBool("CatHome.QA.HoldMasterPreview",false))yield return new WaitForSecondsRealtime(12);
        var close=Read<UnityEngine.UI.Button>(popup,"welcomeBackButton");Call(review,"Tap",close);yield return new WaitForSeconds(.7f);Assert.That(popup.IsOpen,Is.False);
        var reward=UnityEngine.Object.FindAnyObjectByType<HomeLevelUpCelebrationView>(FindObjectsInactive.Include);
        reward.enabled=true;reward.Show(4);yield return new WaitForSeconds(1.5f);yield return Shot("reward-pearl");
        if(SessionState.GetBool("CatHome.QA.HoldMasterPreview",false))yield return new WaitForSecondsRealtime(12);
        Call(review,"Tap",Read<UnityEngine.UI.Button>(reward,"collectButton"));yield return new WaitForSeconds(.8f);
        Assert.That(HomeLevelUpCelebrationView.IsAnyOpen,Is.False);
        var collection=UnityEngine.Object.FindAnyObjectByType<CollectionCompleteCelebrationView>(FindObjectsInactive.Include);collection.enabled=true;
        Call(collection,"Show",CollectionMilestoneService.Catalog[0]);yield return new WaitForSeconds(1.5f);yield return Shot("collection-pearl");
        Call(review,"Tap",Read<UnityEngine.UI.Button>(collection,"collectButton"));yield return null;Assert.That(CollectionCompleteCelebrationView.IsAnyOpen,Is.False);
        collection.enabled=false;
        var toast=UnityEngine.Object.FindAnyObjectByType<HomeRewardToast>(FindObjectsInactive.Include);toast.enabled=true;
        Read<Queue<string>>(toast,"pending").Clear();Read<Queue<string>>(toast,"pending").Enqueue("Ba\u015far\u0131m tamamland\u0131\nSalon arkada\u015fl\u0131\u011f\u0131 \u00b7 +50 jeton");
        typeof(HomeRewardToast).GetField("remaining",F).SetValue(toast,0f);yield return new WaitForSeconds(.8f);yield return Shot("toast-pearl");
        Assert.That(Read<TMPro.TMP_Text>(toast,"message").isTextOverflowing,Is.False);
        foreach(char c in "\u0130\u0131\u015e\u015f\u011e\u011f\u00c7\u00e7\u00d6\u00f6\u00dc\u00fc")Assert.That(Read<TMPro.TMP_Text>(toast,"message").font.HasCharacter(c,false,false),Is.True,"Turkish toast glyph "+c);
        Assert.That(Read<CanvasGroup>(toast,"group").alpha,Is.GreaterThan(.95f));
    }
}
[DefaultExecutionOrder(20000)]
public sealed class LivingMasterMotionProbe : MonoBehaviour
{
    public float maximumTunnelPenetration,maximumScratchRootDrift,maximumChestPitch,maximumChestYaw;
    public int tunnelFrames,skinVertices,left,right,rhythm;
    CatActivity previous;Vector3 root;Mesh sample;
    void LateUpdate()
    {
        var active=CatActivity.Active;if(active==null){previous=null;return;}
        if(active!=previous){root=transform.position;previous=active;}
        if(active is ScratchPostActivity)
        {
            maximumScratchRootDrift=Mathf.Max(maximumScratchRootDrift,Vector3.Distance(root,transform.position));
            var reach=GetComponent<CatPawReachMotion>();if(reach!=null){maximumChestPitch=Mathf.Max(maximumChestPitch,Mathf.Abs(reach.AppliedChestPitch));maximumChestYaw=Mathf.Max(maximumChestYaw,Mathf.Abs(reach.AppliedChestYaw));}
        }
        if(active.StoreProductId!=HomeStoreService.PlayTunnelId||Time.frameCount%3!=0)return;
        var tunnel=(CatEnrichmentActivity)active;var skin=GetComponentInChildren<SkinnedMeshRenderer>();
        if(sample==null)sample=new Mesh();skin.BakeMesh(sample,true);var vertices=sample.vertices;
        var catalog=CatBreedCatalog.Load().Find(GetComponentInChildren<CatBreedVisualTag>().BreedId);
        var middle=(tunnel.RoutineEntryPoint.position+tunnel.ExitPoint.position)*.5f;
        var axis=(tunnel.ExitPoint.position-tunnel.RoutineEntryPoint.position).normalized;axis.y=0;
        var meshes=tunnel.GetComponentsInChildren<MeshCollider>();tunnelFrames++;
        foreach(int i in catalog.ContactVertexIndices)
        {
            Vector3 point=skin.transform.TransformPoint(vertices[i]);if(point.y<.09f)continue;
            Vector3 origin=middle+axis*Vector3.Dot(point-middle,axis);origin.y=.07f;
            Vector3 delta=point-origin;
            foreach(var mesh in meshes)if(mesh.Raycast(new Ray(origin,delta.normalized),out var hit,delta.magnitude))
                maximumTunnelPenetration=Mathf.Max(maximumTunnelPenetration,delta.magnitude-hit.distance);
            skinVertices++;
        }
    }
    void OnDestroy(){if(sample!=null)UnityEngine.Object.Destroy(sample);}
}
#endif


