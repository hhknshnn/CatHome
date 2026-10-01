#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using CatHome.Economy;

// Stage/layout fixtures, not a substitute for a real first-install swipe/joystick journey.
// Run at each actual Game view resolution. Only the copied QA save is permitted.
public sealed class PhoneOnboardingGuidanceTests
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    const string RestoreKey = "CatHome.QA.PhoneOnboardingPreferences";
    const string DirectoryRestoreKey = "CatHome.QA.PhoneOnboardingOriginalDirectory";
    [Serializable] sealed class Preference { public string key, text; public int number; public bool existed, isString; }
    [Serializable] sealed class Snapshot { public List<Preference> preferences = new List<Preference>(); }
    Snapshot snapshot;
    readonly List<GameObject> created = new List<GameObject>();
    static string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Docs/QA/PHONE_PERF_ONBOARDING_2026-09-26");

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Requires copied QA session.");
        string copied = Path.GetFullPath(EditorQaSession.SaveDirectory).TrimEnd('/', '\\');
        Assert.That(copied, Is.Not.EqualTo(Path.GetFullPath(Application.persistentDataPath).TrimEnd('/', '\\')));
        Assert.That(copied.StartsWith(Path.GetFullPath("Library/UiQaSession") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
        snapshot = new Snapshot();
        foreach (string key in new[] { "cat.identity.breed.v1", "CatHome_CatName", "CatHome_CollectionMilestones", "cat-home.local-guest-id",
            "cat.identity.coat", "home.audio.sound", "home.audio.music", "cat-home.language", "Player_LightLevel", "CatHome_PetTutorialCompleted",
            "CatHome_IntroductionCompleted", "CatHome_IntroductionStep", "CatHome_OnboardingStep", "CatHome_OnboardingCompleted", "cat-home.account-kind", "CatRunner_BestScore_v1" })
        {
            bool text = key=="cat.identity.breed.v1"||key=="CatHome_CatName"||key=="CatHome_CollectionMilestones"||key=="cat-home.local-guest-id";
            snapshot.preferences.Add(new Preference { key=key, existed=PlayerPrefs.HasKey(key), isString=text,
                text=text?PlayerPrefs.GetString(key):null, number=text?0:PlayerPrefs.GetInt(key) });
        }
        // Arm a durable restore before the first temporary preference write.
        string json=JsonUtility.ToJson(snapshot,true);
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output,"onboarding-preferences-before.json"),json);
        UnityEditor.SessionState.SetString(RestoreKey,json);
        UnityEditor.EditorApplication.playModeStateChanged -= RestoreOnExit;
        UnityEditor.EditorApplication.playModeStateChanged += RestoreOnExit;
    }

    [TearDown] public void After()
    {
        foreach (var go in created) if (go != null) Object.DestroyImmediate(go);
        created.Clear();
        foreach(var hint in Object.FindObjectsByType<PetTutorialHint>(FindObjectsInactive.Include,FindObjectsSortMode.None)) hint.enabled=false;
        Restore();
    }

    static void RestoreOnExit(UnityEditor.PlayModeStateChange state)
    {if(state==UnityEditor.PlayModeStateChange.ExitingPlayMode||state==UnityEditor.PlayModeStateChange.EnteredEditMode)Restore();}
    static void Restore()
    {
        string directory=UnityEditor.SessionState.GetString(DirectoryRestoreKey,"");
        if(!string.IsNullOrEmpty(directory))
        {CatHomeSaveSystem.EditorEndCopiedSession();UnityEditor.SessionState.SetString("CatHome.QA.SaveDirectory",directory);UnityEditor.SessionState.EraseString(DirectoryRestoreKey);}
        string json=UnityEditor.SessionState.GetString(RestoreKey,"");if(string.IsNullOrEmpty(json))return;
        var saved=JsonUtility.FromJson<Snapshot>(json);
        foreach(var pref in saved.preferences)
            if(!pref.existed)PlayerPrefs.DeleteKey(pref.key);
            else if(pref.isString)PlayerPrefs.SetString(pref.key,pref.text);
            else PlayerPrefs.SetInt(pref.key,pref.number);
        PlayerPrefs.Save();
        UnityEditor.SessionState.EraseString(RestoreKey);
        UnityEditor.EditorApplication.playModeStateChanged-=RestoreOnExit;
        File.WriteAllText(Path.Combine(Output,"onboarding-preferences-restored.json"),json);
    }

    static T Get<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
    static void Set(object target,string name,object value)=>target.GetType().GetField(name,Private).SetValue(target,value);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);

    IEnumerator Home()
    {
        PlayerPrefs.SetString(PetTutorialHint.CatNameKey,"Misket");
        PlayerPrefs.SetInt(PetTutorialHint.IntroductionCompletedKey,1);
        PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey,0);
        PlayerPrefs.SetInt(PetTutorialHint.OnboardingStepKey,2);
        DirectLevelPlayBootstrap.RedirectSuppressed=false;
        yield return RoomPlayModeSupport.WaitForPendingContactData();
        yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);
        LevelLoader loader=null;float deadline=Time.realtimeSinceStartup+30f;
        while(Time.realtimeSinceStartup<deadline)
        {loader=Object.FindAnyObjectByType<LevelLoader>();if(loader!=null&&loader.IsReady)break;yield return null;}
        Assert.That(loader!=null&&loader.IsReady,Is.True,"Normal additive bootstrap must settle.");
        if(loader.CurrentRoom.Id!=HomeRoomService.LivingRoomId)
        {
            Assert.That(loader.LoadRoom(HomeRoomService.LivingRoomId),Is.True);
            deadline=Time.realtimeSinceStartup+20f;
            while(!loader.IsReady&&Time.realtimeSinceStartup<deadline)yield return null;
        }
        var title=Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
        if(title!=null)Call(title,"HideImmediate");
        foreach(var popup in Object.FindObjectsByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include,FindObjectsSortMode.None))popup.Close();
        yield return new WaitForSecondsRealtime(.5f);
        yield return new WaitForEndOfFrame();
    }

    [UnityTest] public IEnumerator Spotlight_AsymmetricPaddingAndMovingScaledTargetUseCurrentBounds()
    {
        var canvasGo=new GameObject("OnboardingGeometryFixture",typeof(RectTransform),typeof(Canvas));created.Add(canvasGo);
        canvasGo.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var parent=new GameObject("ScaledParent",typeof(RectTransform)).GetComponent<RectTransform>();parent.SetParent(canvasGo.transform,false);parent.localScale=new Vector3(.8f,1.2f,1f);
        var target=new GameObject("CurrentTarget",typeof(RectTransform)).GetComponent<RectTransform>();target.SetParent(parent,false);target.sizeDelta=new Vector2(80f,50f);
        var overlay=new GameObject("Spotlight",typeof(RectTransform),typeof(CanvasRenderer)).GetComponent<RectTransform>();overlay.SetParent(canvasGo.transform,false);overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;
        var spotlight=overlay.gameObject.AddComponent<TutorialSpotlight>();
        var targets=new Transform[]{target};var padding=new Vector4(7,11,13,29);
        spotlight.Show(targets,null,padding);
        yield return null;yield return new WaitForEndOfFrame();
        AssertHole(spotlight,target,padding);
        target.anchoredPosition=new Vector2(55,-24);
        yield return null;yield return new WaitForEndOfFrame();
        AssertHole(spotlight,target,padding);
        targets[0]=null; // caller mutation must not silently replace cached geometry
        yield return null;AssertHole(spotlight,target,padding);
    }

    static void AssertHole(TutorialSpotlight spotlight,RectTransform target,Vector4 padding)
    {
        Rect bounds=ScreenRect(target);var root=spotlight.rectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,bounds.min,null,out var min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,bounds.max,null,out var max);
        Rect hole=Get<Rect>(spotlight,"hole");
        Assert.That(hole.xMin,Is.EqualTo(min.x-padding.x).Within(.6f));
        Assert.That(hole.xMax,Is.EqualTo(max.x+padding.y).Within(.6f));
        Assert.That(hole.yMin,Is.EqualTo(min.y-padding.z).Within(.6f),"Bottom padding");
        Assert.That(hole.yMax,Is.EqualTo(max.y+padding.w).Within(.6f),"Top padding");
    }

    [UnityTest] public IEnumerator Onboarding_FreshGuestNameRealSwipeJoystickAndSingleReward()
    {
        string originalDirectory=EditorQaSession.SaveDirectory;
        string freshDirectory=Path.Combine(originalDirectory,"fresh-onboarding-"+Guid.NewGuid().ToString("N"));
        Mouse originalMouse=Mouse.current;
        Mouse testMouse=null;
        PetInteraction testedPet=null;
        bool petDebugBefore=false;
        InputSettings originalInputSettings=InputSystem.settings;
        InputSettings testInputSettings=null;
        var onlineField=typeof(AccountIdentityService).GetField("onlineRequestInFlight",BindingFlags.NonPublic|BindingFlags.Static);
        bool onlineBefore=(bool)onlineField.GetValue(null);
        try
        {
            // A new empty child directory isolates both main/recovery saves. The
            // 16-key durable restore above is armed before any transient reset.
            Directory.CreateDirectory(freshDirectory);
            CatHomeSaveSystem.EditorEndCopiedSession();
            UnityEditor.SessionState.SetString(DirectoryRestoreKey,originalDirectory);
            UnityEditor.SessionState.SetString("CatHome.QA.SaveDirectory",freshDirectory);
            // Exercise the real local guest button while excluding optional online
            // account side effects from this offline, copied-save acceptance test.
            onlineField.SetValue(null,true);
            foreach(string key in new[]{PetTutorialHint.CatNameKey,PetTutorialHint.PlayerPrefsKey,
                PetTutorialHint.IntroductionCompletedKey,PetTutorialHint.IntroductionStepKey,
                PetTutorialHint.OnboardingStepKey,PetTutorialHint.OnboardingCompletedKey,
                AccountIdentityService.AccountKindPlayerPrefsKey,AccountIdentityService.LocalGuestIdPlayerPrefsKey,
                "cat.identity.breed.v1","cat.identity.coat","CatHome_CollectionMilestones"})PlayerPrefs.DeleteKey(key);
            HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
            HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
            EconomyService.ApplySavedState(new EconomySaveState { economyVersion=EconomyService.SaveVersion,
                balances=new[]{new CurrencyBalanceEntry(CurrencyCatalog.GetSaveKey(CurrencyType.Coin),0),new CurrencyBalanceEntry(CurrencyCatalog.GetSaveKey(CurrencyType.Diamond),0)},
                processedTransactionIds=Array.Empty<string>() });
            DirectLevelPlayBootstrap.RedirectSuppressed=false;
            yield return RoomPlayModeSupport.WaitForPendingContactData();
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);
            float deadline=Time.realtimeSinceStartup+30;
            LevelLoader loader=null;
            while(Time.realtimeSinceStartup<deadline){loader=Object.FindAnyObjectByType<LevelLoader>();if(loader!=null&&loader.IsReady)break;yield return null;}
            Assert.That(loader!=null&&loader.IsReady,Is.True);
            var title=Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
            Assert.That(TitleScreen.IsShowing,Is.True);yield return new WaitForSecondsRealtime(.4f);
            yield return Capture("fresh-01-title");Click(Get<Button>(title,"playButton"));yield return null;
            var guest=Get<Button>(title,"accountGuestButton");Assert.That(guest.gameObject.activeInHierarchy,Is.True);
            yield return Capture("fresh-02-account");Click(guest);
            deadline=Time.realtimeSinceStartup+5;while(TitleScreen.IsShowing&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(TitleScreen.IsShowing,Is.False);
            var hint=Object.FindAnyObjectByType<PetTutorialHint>();Assert.That(hint,Is.Not.Null);
            var dialogue=Get<CatDialogueView>(hint,"dialogue");
            yield return new WaitForSecondsRealtime(.4f);
            var name=Get<TMP_InputField>(dialogue,"input");Assert.That(name.gameObject.activeInHierarchy,Is.True);
            yield return Capture("fresh-03-name");name.text="Misket";Click(Get<Button>(dialogue,"confirm"));
            Assert.That(Object.FindAnyObjectByType<CatIdentityLabel>().GetComponent<TMP_Text>().text,
                Is.EqualTo("Misket"), "The first confirmed name must reach the already-enabled HUD immediately.");
            for(int intro=0;intro<4;intro++)
            {
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(Get<int>(hint,"introStep"),Is.EqualTo(intro));
                Assert.That(Get<TMP_Text>(dialogue,"messageLabel").isTextOverflowing,Is.False,"Actual introduction must fit.");
                yield return Capture("fresh-04-introduction-"+intro);Click(Get<Button>(dialogue,"panelButton"));
            }
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(Get<int>(hint,"currentStep"),Is.EqualTo(0));yield return Capture("fresh-05-pet");
            var liveSteps=Get<PetTutorialHint.StepDefinition[]>(hint,"steps");
            AssertNeedBand(liveSteps,true);
            Assert.That(liveSteps[0].target.name,Is.EqualTo("DEF-spine.006"),"Fresh guidance must follow the production rig's actual head bone.");
            var pet=Get<PetInteraction>(hint,"petInteraction");var cat=Get<Transform>(hint,"catTarget");
            testedPet=pet;petDebugBefore=Get<bool>(pet,"debugLogs");Set(pet,"debugLogs",true);
            var camera=Get<Camera>(hint,"gameplayCamera")??Camera.main;
            Assert.That(camera,Is.Not.Null,"Gameplay camera must be active after bootstrap.");
            var catCollider=cat.GetComponent<CharacterController>();Assert.That(catCollider,Is.Not.Null,"Actual playable cat collider.");
            Vector2 point=camera.WorldToScreenPoint(catCollider.bounds.center);
            // Real input device events exercise PetInteraction's UI/world raycast,
            // gesture threshold, animation and SuccessfulPetGesture event.
            // The editor is normally unfocused during automation. Use a
            // temporary, unsaved settings clone and explicit player updates;
            // never change the project's input settings asset.
            testInputSettings=Object.Instantiate(originalInputSettings);
            testInputSettings.hideFlags=HideFlags.HideAndDontSave;
            testInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            testInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            testInputSettings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            InputSystem.settings=testInputSettings;
            testMouse=InputSystem.AddDevice<Mouse>();testMouse.MakeCurrent();
            InjectPetMouse(pet,testMouse,new MouseState {position=point});
            WritePetInputProof(pet,camera,point,testMouse,"before-press");
            InjectPetMouse(pet,testMouse,new MouseState {position=point}.WithButton(MouseButton.Left));
            WritePetInputProof(pet,camera,point,testMouse,"after-press");
            Assert.That(Get<bool>(pet,"pointerTracked"),Is.True,"Actual mouse press must hit the live cat collider.");
            InjectPetMouse(pet,testMouse,new MouseState {position=point+Vector2.right*80}.WithButton(MouseButton.Left));
            WritePetInputProof(pet,camera,point+Vector2.right*80,testMouse,"after-swipe");
            InjectPetMouse(pet,testMouse,new MouseState {position=point+Vector2.right*80});yield return null;
            InputSystem.settings=originalInputSettings;
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(Get<int>(hint,"currentStep"),Is.EqualTo(1),"Validated real swipe advances to movement.");yield return Capture("fresh-06-move");
            var joystick=Object.FindAnyObjectByType<MobileJoystick>();Rect stick=ScreenRect((RectTransform)joystick.transform);
            Vector3 start=cat.position;
            var pointer=new PointerEventData(EventSystem.current){pointerId=701,position=stick.center+Vector2.right*(stick.width*.5f)};
            ExecuteEvents.Execute(joystick.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(joystick.gameObject,pointer,ExecuteEvents.dragHandler);
            deadline=Time.realtimeSinceStartup+5f;
            while(Get<int>(hint,"currentStep")==1&&Time.realtimeSinceStartup<deadline)yield return null;
            ExecuteEvents.Execute(joystick.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            Assert.That(Vector3.Distance(start,cat.position),Is.GreaterThanOrEqualTo(.24f));
            Assert.That(Get<int>(hint,"currentStep"),Is.EqualTo(2),"Real joystick movement advances tutorial.");
            for(int stage=2;stage<5;stage++)
            {
                yield return new WaitForSecondsRealtime(.55f);
                Assert.That(Get<int>(hint,"currentStep"),Is.EqualTo(stage));
                yield return Capture("fresh-07-guidance-"+stage);Click(Get<Button>(dialogue,"panelButton"));
            }
            yield return new WaitForSecondsRealtime(1.5f);
            var celebration=Get<OnboardingCelebrationView>(hint,"celebrationView");Assert.That(celebration.IsOpen,Is.True);
            long before=EconomyService.GetBalance(CurrencyType.Coin);yield return Capture("fresh-08-celebration");
            Click(Get<Button>(celebration,"playButton"));yield return new WaitForSecondsRealtime(.65f);
            Assert.That(PetTutorialHint.IsOnboardingCompleted,Is.True);
            Assert.That(EconomyService.GetBalance(CurrencyType.Coin),Is.EqualTo(before+StarterCareRewardService.RewardCoins));
            long settled=EconomyService.GetBalance(CurrencyType.Coin);
            StarterCareRewardService.Grant();
            Assert.That(EconomyService.GetBalance(CurrencyType.Coin),Is.EqualTo(settled),"Replay cannot pay a second reward.");
            Assert.That(cat.GetComponent<CatMovement>().AreWorldActionsBlocked,Is.False);
            yield return new WaitForEndOfFrame();AssertNeedBand(liveSteps,false);
            yield return Capture("fresh-09-home");
            // First-session discovery must work without restarting, owning a
            // paid sofa or manually rebinding the prompt controller.
            var sofa=CatActivity.Registered.OfType<LivingFurnitureActivity>().Single(a=>a.Kind==CatActivityKind.SofaLounge&&a.gameObject.scene==cat.gameObject.scene);
            Vector3 outward=sofa.RoutineEntryPoint.position-sofa.Perch.position;outward.y=0;outward.Normalize();
            var controller=cat.GetComponent<CharacterController>();controller.enabled=false;
            Vector3 near=sofa.RoutineEntryPoint.position+outward*.45f;near.y=.05f;
            cat.SetPositionAndRotation(near,Quaternion.LookRotation(-outward));controller.enabled=true;Physics.SyncTransforms();
            yield return null;yield return null;
            var prompt=Object.FindAnyObjectByType<ActivityPromptController>();
            Assert.That(Get<CatActivity>(prompt,"candidate"),Is.SameAs(sofa),"Fresh guest must discover the free sofa on the first approach.");
            Assert.That(Get<Button>(prompt,"actionButton").gameObject.activeInHierarchy,Is.True);
            yield return Capture("fresh-10-sofa-first-approach");
        }
        finally
        {
            if(testMouse!=null)InputSystem.RemoveDevice(testMouse);
            if(originalMouse!=null&&originalMouse.added)originalMouse.MakeCurrent();
            if(InputSystem.settings!=originalInputSettings)InputSystem.settings=originalInputSettings;
            if(testInputSettings!=null)Object.Destroy(testInputSettings);
            if(testedPet!=null)Set(testedPet,"debugLogs",petDebugBefore);
            onlineField.SetValue(null,onlineBefore);
            foreach(var hint in Object.FindObjectsByType<PetTutorialHint>(FindObjectsInactive.Include,FindObjectsSortMode.None))hint.enabled=false;
            CatHomeSaveSystem.EditorEndCopiedSession();
            UnityEditor.SessionState.SetString("CatHome.QA.SaveDirectory",originalDirectory);
            Restore();
        }
    }

    [UnityTest] public IEnumerator Onboarding_TurkishNameUsesOneFontFace()
    {
        yield return Home();
        var hint=Object.FindAnyObjectByType<PetTutorialHint>();
        var dialogue=Get<CatDialogueView>(hint,"dialogue");
        CatIdentityService.CatName=string.Empty;
        dialogue.ShowNamePrompt("Adım ne olsun?");
        yield return new WaitForSecondsRealtime(.3f);
        var input=Get<TMP_InputField>(dialogue,"input");
        Assert.That(input.fontAsset,Is.SameAs(input.textComponent.font));
        Assert.That(input.textComponent.font,Is.SameAs(PremiumTypography.Body));
        input.text="gu\u0308mu\u0308s\u0327";
        Assert.That(input.text,Is.EqualTo("gümüş"),"Typing uses composed Turkish glyphs before confirmation.");
        yield return Capture("turkish-name-input");
        Click(Get<Button>(dialogue,"confirm"));
        var hud=Object.FindAnyObjectByType<CatIdentityLabel>().GetComponent<TMP_Text>();
        var speaker=Get<TMP_Text>(dialogue,"nameLabel");
        foreach(var label in new[]{hud,speaker})
        {
            label.ForceMeshUpdate(true);
            foreach(var character in label.textInfo.characterInfo.Take(label.textInfo.characterCount))
                if(!char.IsWhiteSpace(character.character))
                    Assert.That(character.fontAsset,Is.SameAs(label.font),"Name glyph "+character.character+" must use the same face in "+label.name);
            Assert.That(label.text,Is.EqualTo("Gümüş"));
        }
        yield return Capture("turkish-name-confirmed");
        // Cover all Turkish forms on the live HUD, and prove no fallback face,
        // synthetic bold or missing-character substitution enters the name.
        foreach(string name in new[]{"çığİıöşüÇĞÖŞÜ","şeker","ipek","ışık"})
        {
            CatIdentityService.CatName=name;
            hud.ForceMeshUpdate(true);
            Assert.That(hud.font,Is.SameAs(PremiumTypography.Emphasis));
            Assert.That((hud.fontStyle&FontStyles.Bold)==0,Is.True);
            foreach(var character in hud.textInfo.characterInfo.Take(hud.textInfo.characterCount))
            {
                Assert.That(character.fontAsset,Is.SameAs(hud.font));
                Assert.That(character.textElement.unicode,Is.EqualTo((uint)character.character));
            }
        }
        PlayerPrefs.SetString(PetTutorialHint.CatNameKey,"ışık");
        Assert.That(CatIdentityService.CatName,Is.EqualTo("Işık"));
        CatIdentityService.CatName="ışık";
        Assert.That(PlayerPrefs.GetString(PetTutorialHint.CatNameKey),Is.EqualTo("Işık"),"Confirming a legacy lowercase name persists the canonical spelling.");
        CatIdentityService.CatName="çağrı Şükrü";
        PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey,1);
        hint.enabled=false;dialogue.SetSuppressed(true);
        yield return new WaitForSecondsRealtime(.4f);
        yield return Capture("turkish-hud-cagri-sukru");
    }

    [UnityTest] public IEnumerator Onboarding_ChosenNameUpdatesHudLiterallyAndSurvivesReload()
    {
        yield return Home();
        var hint=Object.FindAnyObjectByType<PetTutorialHint>();
        var dialogue=Get<CatDialogueView>(hint,"dialogue");
        var hud=Object.FindAnyObjectByType<CatIdentityLabel>().GetComponent<TMP_Text>();
        foreach(var sample in new[]{new[]{"pamuk","Pamuk"},new[]{"şaşkın Çıtır","Şaşkın Çıtır"},new[]{"mİşKeT","MİşKeT"},new[]{"<B>Pati</b>","<B>Pati</b>"}})
        {
            string chosen=sample[0],expected=sample[1];
            CatIdentityService.CatName=string.Empty;
            dialogue.ShowNamePrompt("Adım ne olsun?");
            yield return new WaitForSecondsRealtime(.3f);
            Get<TMP_InputField>(dialogue,"input").text=chosen;
            Click(Get<Button>(dialogue,"confirm"));
            Assert.That(PlayerPrefs.GetString(PetTutorialHint.CatNameKey),Is.EqualTo(expected));
            Assert.That(hud.text,Is.EqualTo(expected),"Confirmation must update the active HUD in the same frame.");
            hud.ForceMeshUpdate(true);
            Assert.That(hud.GetParsedText(),Is.EqualTo(expected),"Player text must not be interpreted as formatting.");
            var speaker=Get<TMP_Text>(dialogue,"nameLabel");speaker.ForceMeshUpdate(true);
            Assert.That(speaker.GetParsedText(),Is.EqualTo(expected));
        }
        // The normal customization panel must update that same identity too.
        var shop=Object.FindAnyObjectByType<CatBreedShopPanel>(FindObjectsInactive.Include);
        dialogue.SetSuppressed(true);
        shop.RequestOpen();yield return new WaitForSecondsRealtime(.3f);
        var shopInput=Get<TMP_InputField>(shop,"nameInput");
        Assert.That(shopInput.fontAsset,Is.SameAs(shopInput.textComponent.font));
        Assert.That(shopInput.textComponent.font,Is.SameAs(PremiumTypography.Body));
        shopInput.text="zeytin";
        Click(Get<Button>(shop,"useButton"));
        Assert.That(hud.text,Is.EqualTo("Zeytin"));
        Assert.That(PlayerPrefs.GetString(PetTutorialHint.CatNameKey),Is.EqualTo("Zeytin"));
        yield return new WaitForSecondsRealtime(.3f);
        PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey,1);
        PlayerPrefs.Save();
        yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);
        float deadline=Time.realtimeSinceStartup+30f;
        LevelLoader loader=null;
        while(Time.realtimeSinceStartup<deadline)
        {loader=Object.FindAnyObjectByType<LevelLoader>();if(loader!=null&&loader.IsReady)break;yield return null;}
        Assert.That(loader!=null&&loader.IsReady,Is.True);
        Assert.That(PlayerPrefs.GetString(PetTutorialHint.CatNameKey),Is.EqualTo("Zeytin"));
        var title=Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
        if(title!=null)Call(title,"HideImmediate");
        foreach(var popup in Object.FindObjectsByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include,FindObjectsSortMode.None))popup.Close();
        yield return new WaitForSecondsRealtime(.5f);
        var restored=Object.FindAnyObjectByType<CatIdentityLabel>();
        Assert.That(restored,Is.Not.Null);
        Assert.That(restored.GetComponent<TMP_Text>().text,Is.EqualTo("Zeytin"),"A fresh HUD reads the saved player name.");
        yield return Capture("name-hud-zeytin");
    }

    [UnityTest] public IEnumerator Onboarding_GuidanceBindsLiveHudAndFitsPhone()
    {
        try
        {
            yield return Home();
            var hint=Object.FindAnyObjectByType<PetTutorialHint>();Assert.That(hint,Is.Not.Null);
            var joystick=Object.FindAnyObjectByType<MobileJoystick>();Assert.That(joystick,Is.Not.Null);
            var stale=new GameObject("JoystickBackground",typeof(RectTransform),typeof(MobileJoystick));created.Add(stale);stale.SetActive(false);
            var steps=Get<PetTutorialHint.StepDefinition[]>(hint,"steps");
            steps[1].target=stale.transform;Call(hint,"ResolveCurrentTargets");
            Assert.That(steps[0].target.name,Is.EqualTo("DEF-spine.006"),"Use the live production head, not the root fallback.");
            Assert.That(steps[1].target,Is.EqualTo(joystick.transform),"Inactive serialized guidance is replaced by the actual live control.");
            Transform needParent=steps[2].target.parent;
            Assert.That(needParent.name,Is.Not.EqualTo("NeedsPresentationRoot"),"The tutorial no longer moves the HUD.");
            foreach(int stage in new[]{0,1,2,3,4})
            {
                var dialogue=Get<CatDialogueView>(hint,"dialogue");dialogue.SetSuppressed(true);
                Set(hint,"currentStep",stage);Set(hint,"advancing",false);Call(hint,"InvalidateTargets");
                yield return new WaitForSecondsRealtime(.45f);yield return new WaitForEndOfFrame();
                AssertNeedBand(steps,true);
                Assert.That(steps[2].target.parent,Is.EqualTo(needParent));
                if(stage<2)
                {
                    var card=Get<RectTransform>(hint,"cardRoot");InsideSafe(ScreenRect(card),"tutorial card");
                    var skip=Get<RectTransform>(hint,"hintRoot").Find("SkipTour").GetComponent<Button>();
                    InsideSafe(ScreenRect((RectTransform)skip.transform),"skip");AssertButtonHit(skip);
                    Assert.That(Get<TMP_Text>(hint,"instructionLabel").isTextOverflowing,Is.False);
                    if(stage==1)Assert.That(card.Find("MoveCatPortrait").GetComponent<Image>().sprite,Is.Not.Null,"Uses selected cat art.");
                }
                else
                {
                    Assert.That(Get<TutorialSpotlight>(hint,"spotlight").gameObject.activeSelf,Is.True);
                    if(stage==2)
                    {
                        var caption=Get<RectTransform>(hint,"spotlightCopy");Rect captionScreen=ScreenRect(caption);InsideSafe(captionScreen,"needs caption");
                        foreach(var need in new[]{steps[2].target,steps[2].secondaryTarget,steps[2].tertiaryTarget})
                        {
                            Assert.That(captionScreen.Overlaps(ScreenRect((RectTransform)need)),Is.False,"Caption must not cover live needs.");
                            var group=need.GetComponent<CanvasGroup>();Assert.That(group,Is.Not.Null);
                            Assert.That(group.alpha,Is.GreaterThan(.9f),"The real explained indicator must be visible, not merely correctly positioned.");
                            Assert.That(group.blocksRaycasts,Is.False,"Tutorial retains input ownership.");
                            var percent=need.Find("PercentageText").GetComponent<TMP_Text>();
                            Assert.That(percent.text,Does.Contain("%"));Assert.That(percent.color.a,Is.GreaterThan(.9f));
                            Assert.That(percent.isTextOverflowing,Is.False);
                        }
                        AssertButtonHit(Get<Button>(dialogue,"panelButton"));
                    }
                }
                yield return Capture("onboarding-stage-"+stage);
            }
            var bowls=Get<Transform>(hint,"catTarget").GetComponent<BowlInteraction>();
            Assert.That(steps[3].target,Is.EqualTo(bowls.FoodBowl));Assert.That(steps[3].secondaryTarget,Is.EqualTo(bowls.WaterBowl));
            Assert.That(steps[4].target,Is.EqualTo(bowls.GetComponent<SleepInteraction>().TutorialBedTarget));
        }
        finally { Restore(); }
    }

    [UnityTest] public IEnumerator Onboarding_TransitionCannotRevealNextLessonEarly()
    {
        try
        {
            yield return Home();
            var hint=Object.FindAnyObjectByType<PetTutorialHint>();
            Set(hint,"currentStep",2);Set(hint,"advancing",false);
            Call(hint,"CompleteCurrentStep");
            Assert.That(Get<int>(hint,"currentStep"),Is.EqualTo(3));
            Call(hint,"EvaluateVisibility");
            Assert.That(Get<TutorialSpotlight>(hint,"spotlight").gameObject.activeSelf,Is.False,"Previous spotlight stays hidden throughout fade delay.");
            yield return new WaitForSecondsRealtime(.45f);
            Assert.That(Get<bool>(hint,"advancing"),Is.False);
            Assert.That(Get<TutorialSpotlight>(hint,"spotlight").gameObject.activeSelf,Is.True);
        }
        finally { Restore(); }
    }

    static IEnumerator Capture(string phase)
    {
        yield return new WaitForEndOfFrame();
        string path=Path.Combine(Output,phase+"-"+Screen.width+"x"+Screen.height+"-"+GameLanguageService.Current+".png");
        var texture=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(path,texture.EncodeToPNG());}finally{Object.Destroy(texture);}
    }
    static Rect ScreenRect(RectTransform rect)
    {var corners=new Vector3[4];rect.GetWorldCorners(corners);Vector2 min=corners[0],max=corners[0];foreach(var p in corners){min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
    static void InsideSafe(Rect rect,string label)
    {Rect safe=Screen.safeArea;Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(safe.xMin-1),label);Assert.That(rect.yMin,Is.GreaterThanOrEqualTo(safe.yMin-1),label);Assert.That(rect.xMax,Is.LessThanOrEqualTo(safe.xMax+1),label);Assert.That(rect.yMax,Is.LessThanOrEqualTo(safe.yMax+1),label);}
    static void AssertButtonHit(Button button)
    {Assert.That(EventSystem.current,Is.Not.Null);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=ScreenRect((RectTransform)button.transform).center},hits);Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.EqualTo(button));}
    static void AssertNeedBand(PetTutorialHint.StepDefinition[] steps,bool compact)
    {
        var food=(RectTransform)steps[2].tertiaryTarget;
        var band=food.Find("StorybookNeedsBand") as RectTransform;Assert.That(band,Is.Not.Null);
        Assert.That(band.sizeDelta.x,Is.EqualTo(compact?804f:1088f));
        Assert.That(band.anchoredPosition.x,Is.EqualTo(compact?264f:122f));
        Rect face=ScreenRect(band),foodRect=ScreenRect(food),energyRect=ScreenRect((RectTransform)steps[2].secondaryTarget);
        float scale=foodRect.width/260f;
        Assert.That(face.xMax,Is.EqualTo(energyRect.xMax+8f*scale).Within(1f),"Backing right edge is unchanged.");
        if(compact)Assert.That(face.xMin,Is.EqualTo(foodRect.xMin-8f*scale).Within(1f),"No empty locked portrait slot.");
        Assert.That(band.Find("HudDivider-266").gameObject.activeSelf,Is.EqualTo(!compact));
        foreach(var pair in new[]{new Vector2(14,136),new Vector2(278,400)})
        {
            var divider=(RectTransform)band.Find("HudDivider"+pair.x);
            Vector2 actual=RectTransformUtility.WorldToScreenPoint(null,divider.position);
            Vector2 expected=RectTransformUtility.WorldToScreenPoint(null,food.TransformPoint(new Vector3(pair.y,0,0)));
            Assert.That(actual.x,Is.EqualTo(expected.x).Within(.6f),"Need dividers keep their screen positions.");
        }
    }
    static void Click(Button button)
    {Assert.That(button.IsActive()&&button.IsInteractable(),Is.True,button.name);AssertButtonHit(button);ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){position=ScreenRect((RectTransform)button.transform).center},ExecuteEvents.pointerClickHandler);}
    static void InjectPetMouse(PetInteraction pet,Mouse mouse,MouseState state)
    {
        // Consume each queued event and run the real input reader in that same
        // input update. Editor test-coroutine scheduling must not expire the
        // wasPressedThisFrame edge before PetInteraction gets its update.
        mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,state);
        // Installed InputSystem.Update() defaults to Editor while unfocused.
        // That is a different state buffer; request its internal typed overload
        // so the real production Update observes a Dynamic player input edge.
        var update=typeof(InputSystem).GetMethod("Update",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(InputUpdateType)},null);
        Assert.That(update,Is.Not.Null);update.Invoke(null,new object[]{InputUpdateType.Dynamic});Call(pet,"Update");
    }
    static void WritePetInputProof(PetInteraction pet,Camera camera,Vector2 point,Mouse mouse,string phase)
    {
        var text=new System.Text.StringBuilder();
        text.AppendLine("phase="+phase+" frame="+Time.frameCount+" focused="+Application.isFocused+" screen="+Screen.width+"x"+Screen.height);
        text.AppendLine("camera="+camera.name+" active="+camera.isActiveAndEnabled+" pixelRect="+camera.pixelRect+" targetPoint="+point);
        text.AppendLine("update="+InputState.currentUpdateType+" inputCurrent="+(Mouse.current==mouse)+" inputPosition="+mouse.position.ReadValue()+" held="+mouse.leftButton.isPressed+" pressed="+mouse.leftButton.wasPressedThisFrame);
        text.AppendLine("petEnabled="+pet.isActiveAndEnabled+" tracked="+Get<bool>(pet,"pointerTracked")+" petting="+pet.IsPetting);
        object[] allowed={null};bool can=(bool)typeof(PetInteraction).GetMethod("CanBeginPetting",Private).Invoke(pet,allowed);text.AppendLine("canBegin="+can+" reason="+allowed[0]);
        foreach(var hit in Physics.RaycastAll(camera.ScreenPointToRay(point),100f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
            text.AppendLine("worldHit="+hit.collider.name+" descendant="+hit.collider.transform.IsChildOf(pet.transform)+" distance="+hit.distance);
        var ui=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point,pointerId=-1},ui);
        foreach(var hit in ui)text.AppendLine("uiHit="+hit.gameObject.name);
        File.WriteAllText(Path.Combine(Output,"fresh-pet-input-"+phase+".txt"),text.ToString());
    }
}
#endif
