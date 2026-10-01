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
using Object=UnityEngine.Object;

public sealed class GameplayRecoveryTests
{
    const string Root="Docs/QA/INTERACTION_RECOVERY_5H_2026-09-18";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly List<string> rows=new List<string>();
    PreparedInteractionStartTests fixture;
    CatMovement cat; CharacterController controller; MobileJoystick joystick;
    GameObject inputHost,promptHost; ActivityPromptController prompt;
    readonly System.Diagnostics.Stopwatch clock=new System.Diagnostics.Stopwatch();
    static string F(float v)=>v.ToString("R",CultureInfo.InvariantCulture);
    static void Set(object value,string name,object data)=>value.GetType().GetField(name,Private).SetValue(value,data);
    void Input(Vector2 value)=>typeof(MobileJoystick).GetProperty("Direction").SetValue(joystick,value);

    [SetUp] public void Before()
    {
        fixture=new PreparedInteractionStartTests();fixture.Before();
        rows.Clear();rows.Add("activity,sample,x,z,yaw,ready,ms,frameMs,groundSpeed");
    }
    [TearDown] public void After()
    {
        if(joystick!=null)Input(Vector2.zero);
        Directory.CreateDirectory(Root);
        File.WriteAllLines(Root+"/"+TestContext.CurrentContext.Test.Name+".csv",rows);
        if(inputHost!=null)Object.DestroyImmediate(inputHost);
        if(promptHost!=null)Object.DestroyImmediate(promptHost);
        fixture.After();
    }
    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        var state=HomeStoreSaveState.CreateDefault();state.ownedProductIds=HomeStoreService.Products.Select(x=>x.Id).ToArray();
        state.storedProductIds=state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
        HomeStoreService.ApplySavedState(state);CatBreedService.Select("russian-blue");
        cat=Object.FindAnyObjectByType<CatMovement>();controller=cat.GetComponent<CharacterController>();
        yield return QaBreedReadiness.WaitForSelected(cat,"russian-blue");
        cat.GetComponent<CatIdleBehavior>().enabled=false;
        inputHost=new GameObject("QA real locomotion input",typeof(RectTransform));joystick=inputHost.AddComponent<MobileJoystick>();
        Set(cat,"mobileJoystick",joystick);Set(cat,"cameraTransform",null);
        promptHost=new GameObject("QA live prompt loop");prompt=promptHost.AddComponent<ActivityPromptController>();prompt.enabled=false;
        RoomPlayModeSupport.ProvisionNeeds().ApplySavedValue(99);
        yield return null;
    }
    void Place(Vector3 p,Quaternion r)
    {
        Input(Vector2.zero);controller.enabled=false;cat.transform.SetPositionAndRotation(p,r);controller.enabled=true;
        Set(cat,"verticalVelocity",0f);Physics.SyncTransforms();
    }
    [UnityTest,Timeout(180000)] public IEnumerator LivingRoom_PromptStances_Diagnostic()
    {
        yield return Prepare();
        foreach(var a in CatActivity.Registered.Where(x=>x!=null&&x.isActiveAndEnabled&&x.IsUnlocked&&!x.IsRetired&&x.gameObject.scene==cat.gameObject.scene).ToArray())
        {
            if(a.RoutineEntryPoint==null)continue;
            Vector3 p=a.RoutineEntryPoint.position;p.y=.05f;
            Vector3 to=a.transform.position-p;to.y=0;
            Quaternion r=to.sqrMagnitude>.001f?Quaternion.LookRotation(to):a.RoutineEntryPoint.rotation;
            for(int k=0;k<3;k++)
            {
                Place(p+r*new Vector3((k-1)*.08f,0,0),r);yield return null;
                clock.Restart();bool ready=a.TryGetPromptDistance(cat,out _);clock.Stop();
                rows.Add(string.Join(",",string.IsNullOrEmpty(a.StoreProductId)?a.Kind.ToString():a.StoreProductId,k,F(cat.transform.position.x),F(cat.transform.position.z),F(cat.transform.eulerAngles.y),ready,clock.Elapsed.TotalMilliseconds.ToString("F5",CultureInfo.InvariantCulture),F(Time.unscaledDeltaTime*1000),F(cat.GroundSpeed)));
            }
        }
    }
    [UnityTest,Timeout(180000)] public IEnumerator JumpCatalog_ColdReadinessWaitsWithoutBlockingFirstPrompt()
    {
        yield return Prepare();
        while(CatJumpClearanceCatalog.HasPendingLoad)yield return null;
        CatJumpClearanceCatalog.EditorClearResourceCacheForQa();
        int before=Time.frameCount;
        clock.Restart();CatPawReachCatalog.Preload("russian-blue");clock.Stop();
        double request=clock.Elapsed.TotalMilliseconds;
        Assert.That(request,Is.LessThan(16),"Preload must issue a request instead of parsing the source catalog synchronously.");
        float deadline=Time.realtimeSinceStartup+15;
        double integrationMax=0;
        while(Time.realtimeSinceStartup<deadline)
        {
            clock.Restart();bool integrated=CatPawReachCatalog.IsReadyFor("russian-blue");clock.Stop();
            integrationMax=Math.Max(integrationMax,clock.Elapsed.TotalMilliseconds);
            if(integrated)break;
            yield return null;
        }
        Assert.That(integrationMax,Is.LessThan(16),"Source preparation itself must remain a small frame slice.");
        Assert.That(CatJumpClearanceCatalog.IsReady,Is.True);
        Assert.That(Time.frameCount,Is.GreaterThan(before),"The first approach data must be awaited during room readiness.");
        // Match LevelLoader's existing complete-frame readiness barrier.
        yield return null;
        var a=CatActivity.Registered.Single(x=>x.StoreProductId=="room.armchair");
        Vector3 p=a.RoutineEntryPoint.position;p.y=.05f;
        Vector3 v=a.transform.position-p;v.y=0;Place(p,Quaternion.LookRotation(v));
        int queriesBefore=CatJumpWeightedBody.RegionQueries,regionsBefore=CatJumpWeightedBody.CachedRegions;
        var worlds=(System.Collections.IDictionary)typeof(CatMeshContactSurface).GetField("metricWorlds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        int worldsBefore=worlds.Count;
        clock.Restart();bool ready=a.TryGetPromptDistance(cat,out _);clock.Stop();
        double firstPromptMs=clock.Elapsed.TotalMilliseconds;
        File.WriteAllText(Root+"/cold-prompt-detail.csv","queriesBefore,regionsBefore,queriesAfter,regionsAfter,requestMs,promptMs\n"+string.Join(",",queriesBefore,regionsBefore,CatJumpWeightedBody.RegionQueries,CatJumpWeightedBody.CachedRegions,request.ToString("R",CultureInfo.InvariantCulture),clock.Elapsed.TotalMilliseconds.ToString("R",CultureInfo.InvariantCulture)));
        rows.Add(string.Join(",","jump-catalog-first-prompt",Time.frameCount-before,F(p.x),F(p.z),F(cat.transform.eulerAngles.y),ready,clock.Elapsed.TotalMilliseconds.ToString("F5",CultureInfo.InvariantCulture),F(Time.unscaledDeltaTime*1000),F(cat.GroundSpeed)));
        var repeated=new List<string>{"sample,ms,worldsBefore,worldsAfter,refinedTriangles"};
        repeated.Add("0,"+firstPromptMs.ToString("R",CultureInfo.InvariantCulture)+","+worldsBefore+","+worlds.Count+","+CatJumpWeightedBody.RefinedTriangles);
        for(int attempt=1;attempt<=2;attempt++)
        {
            Place(p+Vector3.right*(attempt*.001f),Quaternion.LookRotation(v));
            clock.Restart();a.TryGetPromptDistance(cat,out _);clock.Stop();
            repeated.Add(attempt+","+clock.Elapsed.TotalMilliseconds.ToString("R",CultureInfo.InvariantCulture)+",,"+worlds.Count+","+CatJumpWeightedBody.RefinedTriangles);
        }
        File.WriteAllLines(Root+"/cold-prompt-repeated.csv",repeated);
        Assert.That(regionsBefore,Is.EqualTo(252),"All 63 end poses must be prepared before the first prompt.");
        Assert.That(CatJumpWeightedBody.CachedRegions,Is.EqualTo(regionsBefore),"The first query must not create source geometry.");
        // Keep the project's existing 50 ms first-interaction acceptance.
        // The additional 16 ms aspiration remains visible in the raw report;
        // it is not a 60 FPS guarantee for first-use editor/JIT work.
        Assert.That(firstPromptMs,Is.LessThan(50),"First interaction CPU budget, matching the existing contextual HUD checks.");
        File.WriteAllText(Root+"/cold-readiness-budget.json",JsonUtility.ToJson(new ColdReadinessBudget{frames=Time.frameCount-before,integrationMaxMs=integrationMax,firstPromptMs=firstPromptMs,belowSixteenMs=firstPromptMs<16},true));
    }
    [Serializable] sealed class ColdReadinessBudget { public int frames;public double integrationMaxMs,firstPromptMs;public bool belowSixteenMs; }
    [UnityTest,Timeout(180000)] public IEnumerator LivingRoom_WalkAndPromptFrameBudget()
    {
        yield return Prepare();
        var refresh=typeof(ActivityPromptController).GetMethod("RefreshImmediate",Private);
        var starts=new[]{new Vector3(0,.05f,-1.8f),new Vector3(-1,.05f,.2f),new Vector3(1.8f,.05f,-1f),new Vector3(1,.05f,.3f)};
        var dirs=new[]{Vector2.up,Vector2.right,Vector2.up,Vector2.left};
        double maximum=0;float travelled=0;
        for(int leg=0;leg<starts.Length;leg++)
        {
            var v=new Vector3(dirs[leg].x,0,dirs[leg].y);Place(starts[leg],Quaternion.LookRotation(v));yield return null;
            Input(dirs[leg]);
            for(int i=0;i<60;i++)
            {
                Vector3 before=cat.transform.position;yield return new WaitForEndOfFrame();travelled+=Vector3.Distance(before,cat.transform.position);
                clock.Restart();refresh.Invoke(prompt,null);clock.Stop();maximum=Math.Max(maximum,clock.Elapsed.TotalMilliseconds);
                rows.Add(string.Join(",","walk"+leg,i,F(cat.transform.position.x),F(cat.transform.position.z),F(cat.transform.eulerAngles.y),true,clock.Elapsed.TotalMilliseconds.ToString("F5",CultureInfo.InvariantCulture),F(Time.unscaledDeltaTime*1000),F(cat.GroundSpeed)));
            }
        }
        Input(Vector2.zero);
        Assert.That(travelled,Is.GreaterThan(2),"Real input must move through the room.");
        Assert.That(maximum,Is.LessThan(16),"The proximity HUD must not stall ordinary movement.");
    }

    [UnityTest,Timeout(180000)] public IEnumerator FullHome_RealHudWalkingAndReturnPopup_StayResponsive()
    {
        Time.captureDeltaTime=0;
        DirectLevelPlayBootstrap.RedirectSuppressed=false;
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("GameScene");
        float deadline=Time.realtimeSinceStartup+30;
        LevelLoader loader=null;
        while(Time.realtimeSinceStartup<deadline)
        {loader=Object.FindAnyObjectByType<LevelLoader>();if(loader!=null&&loader.IsReady&&!loader.IsTransitioning)break;yield return null;}
        Assert.That(loader!=null&&loader.IsReady,Is.True);
        if(HomeRoomService.CurrentRoomId!=HomeRoomService.LivingRoomId)
        {
            Assert.That(loader.LoadRoom(HomeRoomService.LivingRoomId),Is.True);
            yield return null;deadline=Time.realtimeSinceStartup+20;
            while(!loader.IsReady&&Time.realtimeSinceStartup<deadline)yield return null;
        }
        var title=Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
        Assert.That(title,Is.Not.Null);
        title.RequestShow();yield return new WaitForSecondsRealtime(.7f);
        ((UnityEngine.UI.Button)typeof(TitleScreen).GetField("playButton",Private).GetValue(title)).onClick.Invoke();
        yield return new WaitForSecondsRealtime(1.5f);
        Assert.That(TitleScreen.IsShowing,Is.False);
        var popup=Object.FindAnyObjectByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include);
        popup.Close();yield return new WaitForSecondsRealtime(.4f);
        cat=Object.FindAnyObjectByType<CatMovement>();controller=cat.GetComponent<CharacterController>();
        CatBreedService.Select("russian-blue");yield return QaBreedReadiness.WaitForSelected(cat,"russian-blue");
        cat.GetComponent<CatIdleBehavior>().enabled=false;Set(cat,"cameraTransform",null);
        joystick=Object.FindAnyObjectByType<MobileJoystick>();Assert.That(joystick,Is.Not.Null);
        Set(cat,"mobileJoystick",joystick);RoomPlayModeSupport.ProvisionNeeds().ApplySavedValue(99);
        Assert.That(cat.IsMovementLocked,Is.False);
        Assert.That(Object.FindAnyObjectByType<ActivityPromptController>().isActiveAndEnabled,Is.True);
        var times=new List<float>();float distance=0;
        var starts=new[]{new Vector3(0,.05f,-1.8f),new Vector3(-1,.05f,.2f),new Vector3(1.8f,.05f,-1f),new Vector3(1,.05f,.3f)};
        var dirs=new[]{Vector2.up,Vector2.right,Vector2.up,Vector2.left};
        for(int leg=0;leg<4;leg++)
        {
            Place(starts[leg],Quaternion.LookRotation(new Vector3(dirs[leg].x,0,dirs[leg].y)));
            yield return new WaitForSecondsRealtime(.1f);Input(dirs[leg]);
            float until=Time.realtimeSinceStartup+2;
            while(Time.realtimeSinceStartup<until)
            {
                Vector3 previous=cat.transform.position;yield return new WaitForEndOfFrame();
                distance+=Vector3.Distance(previous,cat.transform.position);times.Add(Time.unscaledDeltaTime*1000);
                rows.Add(string.Join(",","full-home-walk"+leg,Time.frameCount,F(cat.transform.position.x),F(cat.transform.position.z),F(cat.transform.eulerAngles.y),!cat.IsMovementLocked,0,F(Time.unscaledDeltaTime*1000),F(cat.GroundSpeed)));
            }
            Input(Vector2.zero);
        }
        Assert.That(distance,Is.GreaterThan(2),"The real joystick must move the real cat with the full game HUD running.");
        times.Sort();Assert.That(times[(int)(times.Count*.95f)],Is.LessThan(33.34f),"95% of steady full-home frames must fit 30 fps on this editor machine.");
        typeof(WhileYouWereAwayPopup).GetMethod("Show",Private).Invoke(popup,null);
        Assert.That(WhileYouWereAwayPopup.IsAnyOpen,Is.True,"Open state is visible in the same frame.");
        Assert.That(cat.IsMovementLocked,Is.True,"Return screen still owns input.");
        yield return new WaitForSecondsRealtime(.65f);
        ((UnityEngine.UI.Button)typeof(WhileYouWereAwayPopup).GetField("welcomeBackButton",Private).GetValue(popup)).onClick.Invoke();
        yield return new WaitForSecondsRealtime(.4f);
        Assert.That(WhileYouWereAwayPopup.IsAnyOpen,Is.False);Assert.That(cat.IsMovementLocked,Is.False);
        clock.Restart();for(int i=0;i<10000;i++)Assert.That(WhileYouWereAwayPopup.IsAnyOpen,Is.False);clock.Stop();
        Assert.That(clock.Elapsed.TotalMilliseconds,Is.LessThan(30),"Repeated shared HUD reads must not rescan the loaded world.");
    }
}
