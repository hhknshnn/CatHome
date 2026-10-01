// Next-stage native QA draft; depends on the warmth helper and owned-bubble seam.
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class WarmthFeedbackLifecycleTests
{
    HomeStoreSaveState store;
    GameLanguage language;
    bool quiet,hadOnboarding,hadLanguage;
    int onboarding;
    float scale,capture;
    CatMovement cat;
    OvenWarmthActivity warmth;
    CatWarmthFeedback feedback;
    CatSpeechBubble bubble;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive,Is.True);
        store=HomeStoreService.CaptureState();language=GameLanguageService.Current;
        quiet=CatRunnerProgressService.ReducedMotion;scale=Time.timeScale;capture=Time.captureDeltaTime;
        hadOnboarding=PlayerPrefs.HasKey(PetTutorialHint.OnboardingCompletedKey);
        onboarding=PlayerPrefs.GetInt(PetTutorialHint.OnboardingCompletedKey);
        hadLanguage=PlayerPrefs.HasKey(GameLanguageService.PlayerPrefsKey);
        PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey,1);
        Time.timeScale=1;Time.captureFramerate=30;
    }
    [TearDown] public void After()
    {
        Time.timeScale=1;if(CatActivity.Active!=null)CatActivity.Active.CancelForTransition();
        RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(store);
        GameLanguageService.SetLanguage(language);CatRunnerProgressService.SetReducedMotion(quiet);
        if(!hadLanguage)PlayerPrefs.DeleteKey(GameLanguageService.PlayerPrefsKey);
        if(hadOnboarding)PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey,onboarding);
        else PlayerPrefs.DeleteKey(PetTutorialHint.OnboardingCompletedKey);
        Time.timeScale=scale;Time.captureDeltaTime=capture;
    }
    IEnumerator Prepare(string scene)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(scene);
        var state=HomeStoreSaveState.CreateDefault();state.ownedProductIds=HomeStoreService.Products.Select(p=>p.Id).ToArray();
        state.storedProductIds=state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();HomeStoreService.ApplySavedState(state);
        yield return null;yield return null;
        cat=Object.FindAnyObjectByType<CatMovement>();var idle=cat.GetComponent<CatIdleBehavior>();if(idle!=null)idle.enabled=false;
        foreach(var hint in Object.FindObjectsByType<PetTutorialHint>(FindObjectsInactive.Include))hint.enabled=false;
        warmth=Object.FindObjectsByType<OvenWarmthActivity>().Single();
        bubble=cat.GetComponent<CatSpeechBubble>()??cat.gameObject.AddComponent<CatSpeechBubble>();
        warmth.TryGetStartPose(cat,out var hintPose);bool ready=false;
        foreach(float radius in new[]{0f,.08f,.16f,.22f})
        {
            for(int angle=0;angle<(radius==0?1:16)&&!ready;angle++)
            {
                Vector3 p=hintPose.ZoneCentre+Quaternion.Euler(0,angle*22.5f,0)*Vector3.forward*radius;p.y=.05f;
                Quaternion q=Quaternion.Euler(0,37,0);if(!cat.IsInteractionPoseClear(p,q))continue;
                var cc=cat.GetComponent<CharacterController>();cc.enabled=false;cat.transform.SetPositionAndRotation(p,q);cc.enabled=true;
                Physics.SyncTransforms();ready=warmth.TryGetPromptDistance(cat,out _);
            }
            if(ready)break;
        }
        Assert.That(ready,Is.True,scene+" real prepared stance");RoomPlayModeSupport.ProvisionNeeds();
    }
    IEnumerator BeginRest()
    {
        Vector3 p=cat.transform.position;Quaternion q=cat.transform.rotation;
        Assert.That(warmth.TryStart(cat),Is.True);
        float deadline=Time.realtimeSinceStartup+5;
        do
        {
            yield return new WaitForEndOfFrame();feedback=cat.GetComponent<CatWarmthFeedback>();
            Assert.That(Vector3.ProjectOnPlane(cat.transform.position-p,Vector3.up).magnitude,Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(q,cat.transform.rotation),Is.LessThan(.2f));
        }while((feedback==null||!feedback.IsActive||feedback.BubbleCount==0)&&Time.realtimeSinceStartup<deadline);
        Assert.That(feedback!=null&&feedback.IsActive,Is.True);
        Assert.That(feedback.VisualRoot!=null&&feedback.VisualRoot.gameObject.activeSelf,Is.True);
        Assert.That(feedback.VisualRoot.GetComponent<MeshFilter>().sharedMesh.vertexCount,Is.EqualTo(54));
        Assert.That(feedback.VisualRoot.GetComponentsInChildren<Collider>().Length,Is.Zero);
        Assert.That(feedback.VisualRoot.GetComponentsInChildren<Light>().Length,Is.Zero);
        Assert.That(bubble.IsOwnedBy(feedback),Is.True);
        Assert.That(((TMP_Text)typeof(CatSpeechBubble).GetField("label",Private).GetValue(bubble)).text,
            Is.EqualTo(GameContentCopy.CatReaction("SO WARM!")));
    }
    [UnityTest,Timeout(180000)] public IEnumerator AllWarmthTypes_BothLanguages_NormalAndCancelledLifecycle()
    {
        foreach(string scene in new[]{"Kitchen_Level01","Patio_Level01","SecondFloor_Level01","Balcony_Level01"})
        foreach(var lang in new[]{GameLanguage.Turkish,GameLanguage.English})
        {
            yield return Prepare(scene);GameLanguageService.SetLanguage(lang);yield return BeginRest();
            var visual=feedback.VisualRoot;var material=visual.GetComponent<MeshRenderer>().sharedMaterial;
            float clock=feedback.Elapsed;int count=feedback.BubbleCount;Time.timeScale=0;
            yield return null;yield return null;yield return null;
            Assert.That(feedback.Elapsed,Is.EqualTo(clock));Assert.That(feedback.BubbleCount,Is.EqualTo(count));
            Time.timeScale=1;
            if(lang==GameLanguage.Turkish)warmth.CancelForTransition();
            else
            {
                Assert.That(warmth.RequestRestStop(),Is.True);
                float deadline=Time.realtimeSinceStartup+4;
                while(warmth.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(warmth.IsRunning,Is.False);
            }
            Assert.That(feedback.IsActive,Is.False);Assert.That(visual.gameObject.activeSelf,Is.False);
            Assert.That(bubble.IsOwnedBy(feedback),Is.False);Assert.That(material,Is.Not.Null,"Reusable owned material survives Stop.");
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(cat.GetComponents<CatWarmthFeedback>().Length,Is.EqualTo(1));
        }
    }
    [UnityTest,Timeout(90000)] public IEnumerator PauseLanguageQuietMotion_AndNewerBubbleOwnership()
    {
        yield return Prepare("Kitchen_Level01");CatRunnerProgressService.SetReducedMotion(true);
        GameLanguageService.SetLanguage(GameLanguage.Turkish);yield return BeginRest();
        Assert.That(feedback.QuietMotion,Is.True);
        var filter=feedback.VisualRoot.GetComponent<MeshFilter>();var first=filter.sharedMesh.vertices;
        yield return new WaitForSeconds(.3f);var next=filter.sharedMesh.vertices;
        for(int i=0;i<next.Length;i++)Assert.That(Mathf.Abs(next[i].y-first[i].y),Is.LessThan(.00001f),
            "Reduced motion must not rise; camera-facing orientation may still follow the camera.");
        GameLanguageService.SetLanguage(GameLanguage.English);yield return null;
        Assert.That(((TMP_Text)typeof(CatSpeechBubble).GetField("label",Private).GetValue(bubble)).text,Is.EqualTo("So warm."));
        while(feedback.Elapsed<29.8f)yield return null;
        Assert.That(feedback.BubbleCount,Is.EqualTo(1));
        while(feedback.Elapsed<30.2f)yield return null;
        Assert.That(feedback.BubbleCount,Is.EqualTo(2));
        bubble.Show("GREAT PLAY!");warmth.CancelForTransition();
        yield return new WaitForSeconds(.12f);
        Assert.That(((TMP_Text)typeof(CatSpeechBubble).GetField("label",Private).GetValue(bubble)).text,
            Is.EqualTo(GameContentCopy.CatReaction("GREAT PLAY!")),"Cancel must not dismiss a newer bubble.");
        Assert.That(((CanvasGroup)typeof(CatSpeechBubble).GetField("group",Private).GetValue(bubble)).alpha,Is.GreaterThan(.1f));
        Assert.That(feedback.IsActive,Is.False);Assert.That(feedback.VisualRoot.gameObject.activeSelf,Is.False);
    }
}
