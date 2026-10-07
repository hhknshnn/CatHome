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
public sealed class LivingFinalPolishTests
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    ActionReadyReviewTests review;
    static T Read<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static string Output=>SessionState.GetString("CatHome.QA.ResultDirectory","Temp");
    [SetUp]public void Before(){review=new ActionReadyReviewTests();review.Before();}
    [TearDown]public void After(){if(Read<FurnitureBodyClearanceTests>(review,"skinCheck")!=null)Call(review,"FinishSkinCheck");review.After();}
    [UnityTest,Timeout(180000)]public IEnumerator ScratchSmall(){yield return Scratch("persian");}
    [UnityTest,Timeout(180000)]public IEnumerator ScratchMedium(){yield return Scratch("domestic-shorthair");}
    [UnityTest,Timeout(180000)]public IEnumerator ScratchLarge(){yield return Scratch("maine-coon");}
    IEnumerator Scratch(string breed)
    {
        SessionState.SetString("CatHome.QA.ReviewBreed",breed);yield return (IEnumerator)Call(review,"Boot");
        HomeStoreService.TrySetStored(HomeStoreService.ScratchPostId,false);yield return null;yield return null;
        var post=CatActivity.Registered.OfType<ScratchPostActivity>().Single(a=>a.StoreProductId==HomeStoreService.ScratchPostId);
        var home=Read<CareAlignmentPolishTests>(review,"home");var cat=Read<CatMovement>(home,"cat");
        var skin=new FurnitureBodyClearanceTests();skin.Before();
        typeof(FurnitureBodyClearanceTests).GetField("confirmActualSkin",F).SetValue(skin,true);
        typeof(ActionReadyReviewTests).GetField("skinCheck",F).SetValue(review,skin);
        var probe=cat.gameObject.AddComponent<FinalScratchProbe>();probe.output=Output;probe.breed=breed;
        for(int i=0;i<2;i++)
        {
            yield return (IEnumerator)Call(review,"Observe",post);
            File.AppendAllText(Path.Combine(Output,"scratch-results.txt"),breed+" rhythm="+post.RhythmVariation+" left="+post.LeftStrokes+" right="+post.RightStrokes+" distances="+post.LastLeftSurfaceDistance+","+post.LastRightSurfaceDistance+"\n");
            Assert.That(post.LeftStrokes,Is.GreaterThan(0));Assert.That(post.RightStrokes,Is.GreaterThan(0));
            Assert.That(post.LastLeftSurfaceDistance,Is.LessThan(ScratchPostActivity.StrokeContactTolerance));Assert.That(post.LastRightSurfaceDistance,Is.LessThan(ScratchPostActivity.StrokeContactTolerance));
        }
        var rows=Read<List<ActionReadyReviewTests.Row>>(review,"rows");Assert.That(rows.All(r=>r.ready&&r.started&&r.completed&&r.released),Is.True);
        Assert.That(Read<HashSet<string>>(review,"skinFailures"),Is.Empty);
        File.WriteAllText(Path.Combine(Output,"scratch-metrics-"+breed+".json"),JsonUtility.ToJson(probe,true));
        Assert.That(probe.maximumRootDrift,Is.LessThan(.001f));
        Assert.That(probe.maximumSkinPenetration,Is.LessThanOrEqualTo(.005f));
        Call(review,"FinishSkinCheck");
    }
    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);
    }
    [UnityTest,Timeout(120000)]public IEnumerator VideoPlayback()
    {
        SessionState.SetString("CatHome.QA.ReviewBreed","domestic-shorthair");yield return (IEnumerator)Call(review,"Boot");Time.captureFramerate=0;
        var root=new GameObject("QA video playback",typeof(Canvas),typeof(UnityEngine.UI.RawImage));var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=500;
        var texture=new RenderTexture(1920,1080,0);root.GetComponent<UnityEngine.UI.RawImage>().texture=texture;root.GetComponent<UnityEngine.UI.RawImage>().raycastTarget=false;
        var player=root.AddComponent<UnityEngine.Video.VideoPlayer>();player.playOnAwake=false;player.isLooping=false;player.skipOnDrop=false;player.sendFrameReadyEvents=true;
        player.audioOutputMode=UnityEngine.Video.VideoAudioOutputMode.None;player.renderMode=UnityEngine.Video.VideoRenderMode.RenderTexture;player.targetTexture=texture;player.url=Path.GetFullPath(Path.Combine(Output,"CatHome_Salon_FinalPolish.mp4"));
        bool ended=false;string error="";long highest=-1;int decoded=0;
        player.loopPointReached+=_=>ended=true;player.errorReceived+=(_,m)=>error=m;player.frameReady+=(_,f)=>{highest=Math.Max(highest,f);decoded++;};
        try
        {
            player.Prepare();float end=Time.realtimeSinceStartup+12;while(!player.isPrepared&&error.Length==0&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(error,Is.Empty);Assert.That(player.isPrepared,Is.True);Assert.That(player.length,Is.EqualTo(14).Within(.05));
            player.Play();end=Time.realtimeSinceStartup+30;while(!ended&&error.Length==0&&Time.realtimeSinceStartup<end)yield return null;
            File.WriteAllText(Path.Combine(Output,"video-playback.json"),"{\"ended\":"+ended.ToString().ToLowerInvariant()+",\"decoded\":"+decoded+",\"highestFrame\":"+highest+"}");
            Assert.That(error,Is.Empty);Assert.That(ended,Is.True);Assert.That(highest,Is.GreaterThanOrEqualTo(334));
        }
        finally{player.Stop();texture.Release();UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(root);}
    }
    [UnityTest,Timeout(240000)]public IEnumerator SpeechSleepAndVideo()
    {
        SessionState.SetString("CatHome.QA.ReviewBreed","domestic-shorthair");yield return (IEnumerator)Call(review,"Boot");
        HomeStoreService.TrySetStored(HomeStoreService.ScratchPostId,false);yield return null;yield return null;
        Time.captureFramerate=24;Time.timeScale=1;GameLanguageService.SetLanguage(GameLanguage.Turkish);
        var home=Read<CareAlignmentPolishTests>(review,"home");var cat=Read<CatMovement>(home,"cat");
        string path=Path.Combine(Output,"CatHome_Salon_FinalPolish.mp4");
        var encoder=new UnityEditor.Media.MediaEncoder(path,new UnityEditor.Media.VideoTrackAttributes{frameRate=new UnityEditor.Media.MediaRational(24),width=(uint)Screen.width,height=(uint)Screen.height,includeAlpha=false,bitRateMode=UnityEditor.VideoBitrateMode.High});
        typeof(ActionReadyReviewTests).GetField("encoder",F).SetValue(review,encoder);
        void Record(bool v)=>typeof(ActionReadyReviewTests).GetField("record",F).SetValue(review,v);
        Record(true);yield return (IEnumerator)Call(review,"Observe",CatActivity.Registered.OfType<ScratchPostActivity>().Single(a=>a.StoreProductId==HomeStoreService.ScratchPostId));Record(false);
        Call(home,"Place",new Vector3(-.4f,.05f,-.8f),Quaternion.Euler(0,130,0));yield return null;
        var speech=CatSpeechBubble.EnsureOn(cat);
        foreach(string context in new[]{"scratch","food","sleep"})
        {
            var seen=new HashSet<string>();
            foreach(var language in new[]{GameLanguage.Turkish,GameLanguage.English})
            {
                GameLanguageService.SetLanguage(language);
                for(int i=0;i<3;i++)
                {
                    string text=LivingRoomSpeech.Next(context,false);Assert.That(seen.Add(text),Is.True);
                    speech.ShowLocalizedOwned(cat,text);yield return new WaitForSecondsRealtime(.35f);
                    var label=Read<TMPro.TMP_Text>(speech,"label");Assert.That(label.text,Is.EqualTo(text));Assert.That(label.isTextOverflowing,Is.False);
                    if(i==0){yield return Shot("speech-"+context+"-"+language);}
                    if(language==GameLanguage.Turkish&&i==0){Record(true);Call(review,"BeginChapter","Salon sohbeti");yield return (IEnumerator)Call(review,"Frames",1f);Record(false);}
                }
            }
        }
        GameLanguageService.SetLanguage(GameLanguage.Turkish);speech.DismissOwned(cat);
        var sleep=Read<SleepInteraction>(home,"sleep");Read<EnergySystem>(home,"energy").ApplySavedValue(0);Call(home,"FindBedStance");yield return null;yield return null;
        Call(Read<BowlInteraction>(home,"bowls"),"Update");Call(review,"Tap",Call(home,"SleepButton"));Assert.That(sleep.IsSleeping,Is.True);
        float end=Time.time+10;while(!sleep.IsSettledOnBed&&Time.time<end)yield return null;Assert.That(sleep.IsSettledOnBed,Is.True);
        var fx=cat.GetComponent<CatSleepZzzEffect>();end=Time.time+5;while(!fx.IsPlaying&&Time.time<end)yield return null;Assert.That(fx.IsPlaying,Is.True);
        Record(true);Call(review,"BeginChapter","Sakin uyku");
        int count=Read<int>(review,"frame");yield return (IEnumerator)Call(review,"Frames",(14*24-count)/24f);Record(false);
        yield return Shot("sleep-final");
        var badge=Read<CatCareFxGraphic>(fx,"badge");var fade=Read<CanvasGroup>(fx,"fade");Assert.That(fade.alpha,Is.EqualTo(1).Within(.02f));
        Assert.That(sleep.TryHandleActionButton(),Is.True);yield return new WaitForSeconds(.12f);Assert.That(fade.alpha,Is.LessThan(1));
        end=Time.time+15;while(sleep.IsSleeping&&Time.time<end)yield return null;Assert.That(sleep.IsSleeping,Is.False);Assert.That(badge.gameObject.activeSelf,Is.False);Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
        yield return Shot("wake-final");
        int frames=Read<int>(review,"frame");encoder.Dispose();typeof(ActionReadyReviewTests).GetField("encoder",F).SetValue(review,null);
        File.WriteAllText(Path.Combine(Output,"video-metadata.json"),"{\"frames\":"+frames+",\"fps\":24,\"width\":"+Screen.width+",\"height\":"+Screen.height+"}");Assert.That(frames,Is.EqualTo(336));
        var rows=Read<List<ActionReadyReviewTests.Row>>(review,"rows");Assert.That(rows.All(r=>r.ready&&r.started&&r.completed&&r.released),Is.True);
    }
}

public sealed class FinalScratchProbe:MonoBehaviour
{
    public string output,breed;
    public int frames,skinPoints,deepestIndex,deepestFrame;public Vector3 deepestLocalPoint;public string deepestSurface;public float maximumRootDrift,maximumSkinPenetration;
    public float deepestNormalGap;
    public int deepestExitVotes,deepestTriangle;
    public bool deepestInside;
    public Vector3 deepestWorldPoint,deepestClosestWorldPoint,deepestNormal;
    public float rawMaximumProjectedDepth,rawDeepestExactDistance,rawDeepestNormalGap;
    public int rawDeepestIndex,rawDeepestFrame,rawDeepestExitVotes,insideSkinPoints,classifiedSkinPoints;
    public bool rawDeepestInside,rawDeepestMetricKnown;
    public Vector3 rawDeepestLocalPoint,rawDeepestNormal;
    public string rawDeepestSurface;
    public bool classifierControlsPassed,classifierControlsAttempted,classifierInsideControl,classifierOutsideControl;
    public int classifierControlExitVotes,classifierControlMaximumVotes,classifierControlVertexCount,classifierControlTriangleCount;
    public string classifierControlObject,classifierControlMesh;
    public Vector3 classifierControlLocalPoint;
    public float classifierControlClearance;
    Vector3 root;bool started;int shots;
    Mesh baked;CatMeshContactSurface.TargetSet surface;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly MethodInfo Inside=typeof(CatBodyGuard).GetMethod("DeepInside",Private);
    static readonly FieldInfo Cache=typeof(CatBodyGuard).GetField("surfaceInsideCache",Private);
    static readonly Vector3[] Directions={Vector3.up,Vector3.down,Vector3.right,Vector3.left,Vector3.forward,Vector3.back};
    CatBodyGuard guard;
    IEnumerator Start()
    {
        while(true)
        {
            yield return new WaitForEndOfFrame();
            var post=CatActivity.Active as ScratchPostActivity;
            if(post==null){started=false;continue;}
            if(!started){root=transform.position;started=true;}
            frames++;maximumRootDrift=Mathf.Max(maximumRootDrift,Vector3.Distance(root,transform.position));
            if(frames%4==0)
            {
                if(surface==null)surface=new CatMeshContactSurface.TargetSet(post.transform);
                if(guard==null)guard=(CatBodyGuard)typeof(CatMovement).GetField("bodyGuard",Private).GetValue(GetComponent<CatMovement>());
                Assert.That(guard!=null&&Inside!=null&&Cache!=null,Is.True,"Real skin volume classification must be available");
                var cache=(System.Collections.IDictionary)Cache.GetValue(guard);cache.Clear();
                var skin=GetComponentInChildren<SkinnedMeshRenderer>();if(baked==null)baked=new Mesh();skin.BakeMesh(baked,true);
                var vertices=baked.vertices;var entry=CatBreedCatalog.Load().Find(breed);
                try{foreach(int index in entry.ContactVertexIndices)
                {
                    var point=skin.transform.TransformPoint(vertices[index]);
                    if(!surface.TryClosest(point,out var hit))continue;skinPoints++;
                    float depth=-Vector3.Dot(point-hit.Point,hit.Normal);
                    float nearest=Vector3.Distance(point,hit.Point);if(nearest>=.15f)continue;
                    var collider=hit.Filter.GetComponent<MeshCollider>();
                    Assert.That(collider!=null&&collider.sharedMesh==hit.Mesh,Is.True,"Measured visible mesh must have the same collider geometry");
                    if(!classifierControlsAttempted&&hit.Filter.name=="Body")CheckClassifierControls(collider,hit.Point,hit.Normal);
                    // A nearest triangle's signed plane distance is not a volume
                    // test at a rope crease or open edge. Preserve it as evidence,
                    // then use the existing six-ray winding classifier and an
                    // exact Euclidean metric for the unchanged 5 mm assertion.
                    bool inside=(bool)Inside.Invoke(guard,new object[]{point,collider,0f});classifiedSkinPoints++;
                    bool rawWitness=depth>rawMaximumProjectedDepth;
                    float exact=nearest;Vector3 metricNormal;
                    bool known=!inside&&!rawWitness||CatMeshContactSurface.TryMetric(hit.Mesh,hit.Filter.transform,point,out exact,out metricNormal);
                    Assert.That(known,Is.True,"Unknown mesh metric must not certify zero penetration");
                    if(rawWitness)
                    {
                        rawMaximumProjectedDepth=depth;rawDeepestIndex=index;rawDeepestFrame=frames;rawDeepestLocalPoint=transform.InverseTransformPoint(point);
                        rawDeepestSurface=hit.Filter.name;rawDeepestInside=inside;rawDeepestExactDistance=exact;rawDeepestMetricKnown=known;
                        rawDeepestNormal=hit.Normal;rawDeepestNormalGap=Vector3.Dot(point-hit.Point,hit.Normal);
                        rawDeepestExitVotes=ExitVotes(point,collider);
                    }
                    // An unvalidated classifier cannot turn every sample into
                    // zero. Keep the old conservative reading in that case and
                    // preserve controls/data so the cause remains reviewable.
                    if(!classifierControlsPassed&&depth>maximumSkinPenetration)
                    {maximumSkinPenetration=depth;deepestIndex=index;deepestFrame=frames;deepestLocalPoint=transform.InverseTransformPoint(point);deepestSurface=hit.Filter.name;CaptureDeepest(point,hit,collider,inside);}
                    if(!inside)continue;insideSkinPoints++;
                    if(exact>maximumSkinPenetration){maximumSkinPenetration=exact;deepestIndex=index;deepestFrame=frames;deepestLocalPoint=transform.InverseTransformPoint(point);deepestSurface=hit.Filter.name;CaptureDeepest(point,hit,collider,true);}
                }}finally{cache.Clear();}
            }
            if(frames%15==0&&shots<8)
            {var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,"scratch-"+breed+"-"+(shots++)+".png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
        }
    }
    void CaptureDeepest(Vector3 point,CatMeshContactSurface.Hit hit,MeshCollider collider,bool inside)
    {
        deepestWorldPoint=point;deepestClosestWorldPoint=hit.Point;deepestNormal=hit.Normal;deepestTriangle=hit.Triangle;
        deepestNormalGap=Vector3.Dot(point-hit.Point,hit.Normal);deepestInside=inside;deepestExitVotes=ExitVotes(point,collider);
    }
    void CheckClassifierControls(MeshCollider collider,Vector3 surfacePoint,Vector3 surfaceNormal)
    {
        classifierControlsAttempted=true;classifierControlObject=collider.name;classifierControlMesh=collider.sharedMesh.name;
        classifierControlVertexCount=collider.sharedMesh.vertexCount;
        for(int sub=0;sub<collider.sharedMesh.subMeshCount;sub++)classifierControlTriangleCount+=(int)collider.sharedMesh.GetIndexCount(sub)/3;
        // Match the existing classifier's >=4 outward-exit criterion. Open
        // decorative ends need not supply six exits. A small positive exact
        // clearance proves the control is not sitting on a triangle boundary.
        Vector3 centre=collider.bounds.center;bool found=false;
        var candidates=new List<Vector3>();
        foreach(float offset in new[]{.001f,.003f,.006f,.012f,.024f})candidates.Add(surfacePoint-surfaceNormal*offset);
        foreach(float y in new[]{0f,-.125f,.125f,-.25f,.25f})
        {
            foreach(float x in new[]{0f,-.125f,.125f,-.25f,.25f})
            {
                foreach(float z in new[]{0f,-.125f,.125f,-.25f,.25f})candidates.Add(centre+Vector3.Scale(collider.bounds.size,new Vector3(x,y,z)));
            }
        }
        foreach(var candidate in candidates)
        {
            int votes=ExitVotes(candidate,collider);classifierControlMaximumVotes=Mathf.Max(classifierControlMaximumVotes,votes);
            if(votes<4||!CatMeshContactSurface.TryMetric(collider.sharedMesh,collider.transform,candidate,out float clearance,out _)||clearance<.0005f)continue;
            centre=candidate;classifierControlExitVotes=votes;classifierControlClearance=clearance;found=true;break;
        }
        if(found)
        {
            classifierControlLocalPoint=collider.transform.InverseTransformPoint(centre);
            classifierInsideControl=(bool)Inside.Invoke(guard,new object[]{centre,collider,0f});
        }
        Vector3 outside=collider.bounds.max+Vector3.one;
        classifierOutsideControl=(bool)Inside.Invoke(guard,new object[]{outside,collider,0f});
        if(found)Assert.That(classifierInsideControl,Is.True,"Known interior must classify inside");
        Assert.That(classifierOutsideControl,Is.False,"Known exterior must classify outside");
        classifierControlsPassed=found&&classifierInsideControl&&!classifierOutsideControl;
    }
    static int ExitVotes(Vector3 point,MeshCollider collider)
    {
        if(!collider.bounds.Contains(point))return 0;
        bool previous=Physics.queriesHitBackfaces;int votes=0;
        try
        {
            Physics.queriesHitBackfaces=true;
            foreach(var direction in Directions)
            {
                if(!collider.Raycast(new Ray(point,direction),out var hit,collider.bounds.size.magnitude+.01f))continue;
                Assert.That(CatMeshContactSurface.TryOriginalTriangleNormal(collider.sharedMesh,collider.transform,hit.triangleIndex,out var normal),Is.True);
                if(Vector3.Dot(normal,direction)>.0001f)votes++;
            }
        }
        finally{Physics.queriesHitBackfaces=previous;}
        return votes;
    }
    void OnDestroy(){if(baked!=null)UnityEngine.Object.Destroy(baked);}
}
#endif
