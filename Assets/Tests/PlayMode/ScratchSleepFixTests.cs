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
public sealed class ScratchSleepFixTests
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    ActionReadyReviewTests review;
    static T Read<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static string Output=>SessionState.GetString("CatHome.QA.ResultDirectory","Temp");
    [SetUp]public void Before(){SessionState.EraseString("CatHome.QA.ReviewOnly");review=new ActionReadyReviewTests();review.Before();}
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
            Assert.That(post.LastLeftSurfaceDistance,Is.LessThan(.003f));Assert.That(post.LastRightSurfaceDistance,Is.LessThan(.003f));
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
        player.audioOutputMode=UnityEngine.Video.VideoAudioOutputMode.None;player.renderMode=UnityEngine.Video.VideoRenderMode.RenderTexture;player.targetTexture=texture;player.url=Path.GetFullPath(Path.Combine(Output,"CatHome_Scratch_Sleep_Fix.mp4"));
        bool ended=false;string error="";long highest=-1;int decoded=0;
        player.loopPointReached+=_=>ended=true;player.errorReceived+=(_,m)=>error=m;player.frameReady+=(_,f)=>{highest=Math.Max(highest,f);decoded++;};
        try
        {
            player.Prepare();float end=Time.realtimeSinceStartup+12;while(!player.isPrepared&&error.Length==0&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(error,Is.Empty);Assert.That(player.isPrepared,Is.True);Assert.That(player.length,Is.EqualTo(10).Within(.05));
            player.Play();end=Time.realtimeSinceStartup+30;while(!ended&&error.Length==0&&Time.realtimeSinceStartup<end)yield return null;
            File.WriteAllText(Path.Combine(Output,"video-playback.json"),"{\"ended\":"+ended.ToString().ToLowerInvariant()+",\"decoded\":"+decoded+",\"highestFrame\":"+highest+"}");
            Assert.That(error,Is.Empty);Assert.That(ended,Is.True);Assert.That(highest,Is.GreaterThanOrEqualTo(238));
        }
        finally{player.Stop();texture.Release();UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(root);}
    }
    [UnityTest,Timeout(240000)]public IEnumerator SpeechSleepAndVideo()
    {
        SessionState.SetString("CatHome.QA.ReviewBreed","domestic-shorthair");yield return (IEnumerator)Call(review,"Boot");
        HomeStoreService.TrySetStored(HomeStoreService.ScratchPostId,false);yield return null;yield return null;
        Time.captureFramerate=24;Time.timeScale=1;GameLanguageService.SetLanguage(GameLanguage.Turkish);
        var home=Read<CareAlignmentPolishTests>(review,"home");var cat=Read<CatMovement>(home,"cat");
        string path=Path.Combine(Output,"CatHome_Scratch_Sleep_Fix.mp4");
        var encoder=new UnityEditor.Media.MediaEncoder(path,new UnityEditor.Media.VideoTrackAttributes{frameRate=new UnityEditor.Media.MediaRational(24),width=(uint)Screen.width,height=(uint)Screen.height,includeAlpha=false,bitRateMode=UnityEditor.VideoBitrateMode.High});
        typeof(ActionReadyReviewTests).GetField("encoder",F).SetValue(review,encoder);
        void Record(bool v)=>typeof(ActionReadyReviewTests).GetField("record",F).SetValue(review,v);
        Record(true);yield return (IEnumerator)Call(review,"Observe",CatActivity.Registered.OfType<ScratchPostActivity>().Single(a=>a.StoreProductId==HomeStoreService.ScratchPostId));Record(false);
        Call(home,"Place",new Vector3(-.4f,.05f,-.8f),Quaternion.Euler(0,130,0));yield return null;
        var speech=CatSpeechBubble.EnsureOn(cat);
        GameLanguageService.SetLanguage(GameLanguage.Turkish);speech.DismissOwned(cat);
        var sleep=Read<SleepInteraction>(home,"sleep");Read<EnergySystem>(home,"energy").ApplySavedValue(0);Call(home,"FindBedStance");yield return null;yield return null;
        Call(Read<BowlInteraction>(home,"bowls"),"Update");Call(review,"Tap",Call(home,"SleepButton"));Assert.That(sleep.IsSleeping,Is.True);
        float end=Time.time+10;while(!sleep.IsSettledOnBed&&Time.time<end)yield return null;Assert.That(sleep.IsSettledOnBed,Is.True);
        var fx=cat.GetComponent<CatSleepZzzEffect>();end=Time.time+5;while(!fx.IsPlaying&&Time.time<end)yield return null;Assert.That(fx.IsPlaying,Is.True);
        Record(true);Call(review,"BeginChapter","Uyku · balon + ay + Zzz");
        int targetFrames=10*24;int remaining=targetFrames-Read<int>(review,"frame");
        for(int i=0;i<remaining;i++)
        {
            if(i%28==0)speech.ShowLocalizedOwned(cat,LivingRoomSpeech.Next("sleep",false));
            yield return (IEnumerator)Call(review,"Frames",1f/24f);
            if(i>12)
            {
                var moon=Read<UnityEngine.UI.Image>(fx,"pearl");var zzz=Read<TMPro.TMP_Text>(fx,"dream");
                Assert.That(moon.gameObject.activeInHierarchy,Is.True);Assert.That(zzz.text,Is.EqualTo("Zzz"));Assert.That(zzz.alpha,Is.GreaterThan(.5f));
                Assert.That(speech.TryVisibleScreenRect(out var bubbleRect),Is.True);
                var corners=new Vector3[4];moon.rectTransform.GetWorldCorners(corners);var moonRect=Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
                Assert.That(moonRect.Overlaps(bubbleRect),Is.False,"Moon and speech overlap");
                zzz.rectTransform.GetWorldCorners(corners);var zRect=Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
                Assert.That(zRect.Overlaps(bubbleRect),Is.False,"Zzz and speech overlap");
            }
        }
        Record(false);yield return Shot("sleep-bubble-coexist");
        var badge=Read<CatCareFxGraphic>(fx,"badge");var fade=Read<CanvasGroup>(fx,"fade");Assert.That(fade.alpha,Is.EqualTo(1).Within(.02f));
        Assert.That(sleep.TryHandleActionButton(),Is.True);yield return new WaitForSeconds(.12f);Assert.That(fade.alpha,Is.LessThan(1));
        end=Time.time+15;while(sleep.IsSleeping&&Time.time<end)yield return null;Assert.That(sleep.IsSleeping,Is.False);Assert.That(badge.gameObject.activeSelf,Is.False);Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
        yield return Shot("wake-final");
        int frames=Read<int>(review,"frame");encoder.Dispose();typeof(ActionReadyReviewTests).GetField("encoder",F).SetValue(review,null);
        File.WriteAllText(Path.Combine(Output,"video-metadata.json"),"{\"frames\":"+frames+",\"fps\":24,\"width\":"+Screen.width+",\"height\":"+Screen.height+"}");Assert.That(frames,Is.EqualTo(240));
        var rows=Read<List<ActionReadyReviewTests.Row>>(review,"rows");Assert.That(rows.All(r=>r.ready&&r.started&&r.completed&&r.released),Is.True);
    }
}

#endif
