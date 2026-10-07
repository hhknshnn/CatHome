#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class ActionWelcomeRefreshTests
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Output="Docs/QA/ACTION_UI_REFRESH_2026-10-02";
    CareAlignmentPolishTests home;
    CatMovement cat;ScratchPostActivity post;MediaEncoder encoder;int frames;
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static T Read<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
    [SetUp]public void Before(){cat=null;post=null;home=new CareAlignmentPolishTests();home.Before();}
    [TearDown]public void After(){encoder?.Dispose();encoder=null;Object.FindAnyObjectByType<MobileJoystick>()?.CancelInput();home.After();}
    IEnumerator Boot(string breed)
    {
        if(cat==null)yield return (IEnumerator)Call(home,"Home");
        yield return (IEnumerator)Call(home,"Breed",breed);
        cat=Read<CatMovement>(home,"cat");yield return QaBreedReadiness.WaitForSelected(cat,breed);
        HomeStoreService.TrySetStored(HomeStoreService.ScratchPostId,false);yield return null;yield return null;
        post=CatActivity.Registered.OfType<ScratchPostActivity>().Single(a=>a.StoreProductId==HomeStoreService.ScratchPostId);
        Call(home,"ReadyNeeds");
    }
    IEnumerator Ready()
    {
        string size=CatBreedService.SelectedBreedId=="persian"?"Small":CatBreedService.SelectedBreedId=="maine-coon"?"Large":"Medium";
        string saved="Docs/QA/SCRATCH_FACING_FIX_2026-10-02/accepted-stance-"+CatBreedService.SelectedBreedId+"-"+size+"-rhythm0.json";
        var stance=JsonUtility.FromJson<Stance>(File.ReadAllText(saved));Call(home,"Place",stance.position,stance.rotation);yield return null;yield return null;
        if(post.CanStartFromPrompt(cat,out _))yield break;
        var scratch=new ScratchReworkTests();
        Func<bool> faces=()=>Vector3.Angle(cat.transform.forward,Vector3.ProjectOnPlane(post.GetComponentInChildren<MeshCollider>().bounds.center-cat.transform.position,Vector3.up))<=15f;
        yield return (IEnumerator)Call(scratch,"FindReadyStance",post,home,cat,faces);
        yield return null;yield return null;
    }
    [Serializable]sealed class Stance{public Vector3 position;public Quaternion rotation;}
    Button ActionButton()
    {
        var prompt=Object.FindAnyObjectByType<ActivityPromptController>();Call(prompt,"RefreshImmediate");
        var b=Read<Button>(prompt,"actionButton");Assert.That(b.gameObject.activeInHierarchy,Is.True,"Scratch action must be visible");
        Assert.That(Read<CatActivity>(prompt,"candidate"),Is.SameAs(post));return b;
    }
    static void Tap(Button b)
    {
        Canvas.ForceUpdateCanvases();var e=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,b.transform.position),button=PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
        Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(b));
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.pointerClickHandler);
    }
    [UnityTest,Timeout(90000)]public IEnumerator SmallAction(){yield return CheckAction("persian");}
    [UnityTest,Timeout(90000)]public IEnumerator MediumAction(){yield return CheckAction("domestic-shorthair");}
    [UnityTest,Timeout(90000)]public IEnumerator LargeAction(){yield return CheckAction("maine-coon");}
    IEnumerator CheckAction(string breed)
    {
        {
            yield return Boot(breed);yield return Ready();
            Vector3 p=cat.transform.position;Quaternion q=cat.transform.rotation;bool turned=false;
            foreach(float away in new[]{0f,.015f,.03f,.045f,.06f})
            {
              foreach(float yaw in new[]{-20f,20f,-30f,30f})
              {
                var centre=post.GetComponentInChildren<MeshCollider>().bounds.center;
                var candidate=p+Vector3.ProjectOnPlane(p-centre,Vector3.up).normalized*away;
                Call(home,"Place",candidate,q*Quaternion.Euler(0,yaw,0));yield return null;yield return null;
                if(post.CanStartFromPrompt(cat,out _)){turned=true;p=cat.transform.position;break;}
              }
              if(turned)break;
            }
            File.AppendAllText(Output+"/action-results.txt",breed+" turnReady="+turned+" position="+p+"\n");
            Assert.That(turned,Is.True,"A clear off-axis stance should offer the action: "+breed);
            var button=ActionButton();Tap(button);Assert.That(post.IsRunning,Is.True);
            float end=Time.realtimeSinceStartup+20;float drift=0;
            while(post.IsRunning&&Time.realtimeSinceStartup<end){drift=Mathf.Max(drift,Vector3.Distance(p,cat.transform.position));yield return null;}
            Assert.That(post.IsRunning,Is.False);Assert.That(drift,Is.LessThan(.001f));
            Assert.That(post.LeftStrokes,Is.EqualTo(2));Assert.That(post.RightStrokes,Is.EqualTo(2));
            File.AppendAllText(Output+"/action-results.txt",breed+" completed rootDrift="+drift+"\n");
        }
    }
    IEnumerator Capture(int count,string still=null)
    {
        for(int i=0;i<count;i++)
        {
            yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();
            encoder.AddFrame(texture);frames++;
            if(i==count/2&&still!=null)File.WriteAllBytes(Output+"/"+still+".png",texture.EncodeToPNG());
            Object.Destroy(texture);
        }
    }
    [UnityTest,Timeout(90000)]public IEnumerator JoystickApproachShowsWorkingButton()
    {
        yield return Boot("domestic-shorthair");yield return Ready();
        var target=cat.transform.position;var facing=cat.transform.rotation;var direction=facing*Vector3.forward;
        Call(home,"Place",target-direction*.20f,facing);yield return null;yield return null;
        var origin=cat.transform.position;var joystick=Object.FindAnyObjectByType<MobileJoystick>();
        var cameraField=typeof(CatMovement).GetField("cameraTransform",F);var savedCamera=cameraField.GetValue(cat);cameraField.SetValue(cat,null);
        var rect=(RectTransform)joystick.transform;Canvas.ForceUpdateCanvases();
        var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(Vector2.Scale(new Vector2(direction.x,direction.z).normalized*.22f,rect.rect.size*.5f)));
        var pointer=new PointerEventData(EventSystem.current){position=point,pointerId=29};
        bool ready=false;float end=Time.realtimeSinceStartup+20;
        try
        {
            joystick.OnPointerDown(pointer);
            do{joystick.OnDrag(pointer);yield return null;ready=post.CanStartFromPrompt(cat,out _);}while(!ready&&Time.realtimeSinceStartup<end);
        }
        finally{joystick.OnPointerUp(pointer);cameraField.SetValue(cat,savedCamera);}
        yield return null;yield return null;
        File.WriteAllText(Output+"/joystick.txt","ready="+ready+" travel="+Vector3.Distance(origin,cat.transform.position)+" position="+cat.transform.position);
        Assert.That(ready,Is.True);Assert.That(Vector3.Distance(origin,cat.transform.position),Is.GreaterThan(.03f));
        var start=cat.transform.position;Tap(ActionButton());Assert.That(post.IsRunning,Is.True);end=Time.realtimeSinceStartup+20;
        while(post.IsRunning&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(post.LeftStrokes,Is.EqualTo(2));Assert.That(post.RightStrokes,Is.EqualTo(2));Assert.That(Vector3.Distance(start,cat.transform.position),Is.LessThan(.001f));
        File.AppendAllText(Output+"/joystick.txt","\ncompleted=true rootDrift="+Vector3.Distance(start,cat.transform.position));
    }
    [UnityTest,Timeout(90000)]public IEnumerator VideoPlayback()
    {
        yield return Boot("domestic-shorthair");Time.captureFramerate=0;
        var go=new GameObject("QA final playback");var player=go.AddComponent<UnityEngine.Video.VideoPlayer>();
        var texture=new RenderTexture(1920,1080,0);player.renderMode=UnityEngine.Video.VideoRenderMode.RenderTexture;player.targetTexture=texture;
        player.playOnAwake=false;player.skipOnDrop=false;player.sendFrameReadyEvents=true;player.audioOutputMode=UnityEngine.Video.VideoAudioOutputMode.None;
        bool ended=false;string error="";int decoded=0;long highest=-1;player.frameReady+=(_,f)=>{decoded++;highest=Math.Max(highest,f);};player.loopPointReached+=_=>ended=true;player.errorReceived+=(_,m)=>error=m;
        try
        {
            player.url=Path.GetFullPath(Output+"/CatHome_Action_UI_Refresh.mp4");player.Prepare();float end=Time.realtimeSinceStartup+12;
            while(!player.isPrepared&&error.Length==0&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(player.isPrepared,Is.True);Assert.That(player.length,Is.EqualTo(18).Within(.05));player.Play();end=Time.realtimeSinceStartup+30;
            while(!ended&&error.Length==0&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(error,Is.Empty);Assert.That(ended,Is.True);Assert.That(decoded,Is.EqualTo(432));Assert.That(highest,Is.EqualTo(431));
            File.WriteAllText(Output+"/playback.json","{\"ended\":true,\"decoded\":"+decoded+",\"highestFrame\":"+highest+"}");
        }
        finally{player.Stop();texture.Release();Object.Destroy(texture);Object.Destroy(go);}
    }
    [UnityTest,Timeout(180000)]public IEnumerator Record18Seconds()
    {
        yield return Boot("domestic-shorthair");yield return Ready();
        Time.captureFramerate=24;
        var title=Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);title.RequestShow();yield return new WaitForSecondsRealtime(.7f);
        encoder=new MediaEncoder(Output+"/CatHome_Action_UI_Refresh.mp4",new VideoTrackAttributes{frameRate=new MediaRational(24),width=(uint)Screen.width,height=(uint)Screen.height,includeAlpha=false,bitRateMode=VideoBitrateMode.High});
        frames=0;yield return Capture(96,"title");
        Call(title,"HideImmediate");
        var popup=Object.FindAnyObjectByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include);popup.enabled=true;
        Call(popup,"Populate",new CatHomeSaveSystem.OfflineReturnSummary(999,TimeSpan.FromHours(2),true,false,85,35,90,42,65,85));Call(popup,"Show");
        yield return Capture(96,"welcome-back");Tap(Read<Button>(popup,"welcomeBackButton"));
        yield return Capture(24);
        CatSpeechBubble.EnsureOn(cat).ShowLocalized("Patilerim oyun için hazır!");
        yield return Capture(48,"speech-action");
        Tap(ActionButton());Assert.That(post.IsRunning,Is.True);
        yield return Capture(168,"scratch");
        Assert.That(post.IsRunning,Is.False);Assert.That(post.LeftStrokes,Is.EqualTo(2));Assert.That(post.RightStrokes,Is.EqualTo(2));
        encoder.Dispose();encoder=null;
        File.WriteAllText(Output+"/video.json","{\"frames\":"+frames+",\"fps\":24,\"seconds\":18,\"source\":\"Unity Game View; QA save\"}");
        Assert.That(frames,Is.EqualTo(432));
    }
}
#endif
