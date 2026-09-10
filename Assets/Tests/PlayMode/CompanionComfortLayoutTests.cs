using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class CompanionComfortLayoutTests
{
    [UnityTest]
    public IEnumerator WideAndTablet_CommandTargetsStaySeparate_AndGuideKeepsItsOwnControls()
    {
        Assert.That(EditorQaSession.IsActive,Is.True);
        int width=Screen.width,height=Screen.height;
        bool had=PlayerPrefs.HasKey(PetTutorialHint.OnboardingCompletedKey);
        int old=PlayerPrefs.GetInt(PetTutorialHint.OnboardingCompletedKey);
        var qa=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UiQaVisualTour")).First(t=>t!=null);
        var collectionStates=new Dictionary<CollectionCompleteCelebrationView,bool>();
        try
        {
            PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey,1);
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);
            var deadline=Time.realtimeSinceStartup+30;
            while(Time.realtimeSinceStartup<deadline)
            {
                var loader=Object.FindAnyObjectByType<LevelLoader>();
                if(loader!=null&&loader.IsReady&&!loader.IsTransitioning)break;
                yield return null;
            }
            // A pending collection from another fixture may legitimately show
            // between the two resolutions. Keep that unrelated presentation
            // quiet while separately asserting its real companion modal gate.
            foreach(var celebration in Object.FindObjectsByType<CollectionCompleteCelebrationView>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                collectionStates[celebration]=celebration.enabled;
                celebration.enabled=false;
            }
            qa.GetMethod("Clear").Invoke(null,null);
            yield return null;
            var panel=Object.FindAnyObjectByType<CatCompanionPanel>();
            var cat=Object.FindAnyObjectByType<CatMovement>();
            Assert.That(panel,Is.Not.Null);
            Assert.That(cat,Is.Not.Null);
            panel.Close();
            CatActionState.CancelForTransition(cat);
            // Clear requests the real animated close. The offline-return popup
            // keeps IsAnyOpen and its input owner until its 0.28 s fade finishes;
            // a couple of frames are not a ready-home precondition.
            deadline=Time.realtimeSinceStartup+5f;
            while(Time.realtimeSinceStartup<deadline &&
                  !ReadyForCommands(panel,cat))
                yield return null;
            cat.LogInputBlockOwners("Companion layout fixture ready");
            Assert.That(WhileYouWereAwayPopup.IsAnyOpen,Is.False,"The return popup must finish closing first.");
            Assert.That(ReadyForCommands(panel,cat),Is.True,DescribeState(panel,cat,"Ready precondition"));
            foreach(int targetWidth in new[]{1920,1440})
            {
                qa.GetMethod("Resolution").Invoke(null,new object[]{targetWidth,1080});
                deadline=Time.realtimeSinceStartup+5f;
                while(Time.realtimeSinceStartup<deadline &&
                      (Screen.width!=targetWidth || Screen.height!=1080 || !ReadyForCommands(panel,cat)))
                    yield return null;
                string before=DescribeState(panel,cat,"Before Show "+targetWidth);
                Debug.Log(before);
                Assert.That(ReadyForCommands(panel,cat),Is.True,before);
                Assert.That(new Vector2Int(Screen.width,Screen.height),Is.EqualTo(new Vector2Int(targetWidth,1080)),before);
                panel.Show(false);
                string immediate=DescribeState(panel,cat,"Immediately after Show");
                Debug.Log(immediate);
                Assert.That(CatCompanionPanel.IsAnyOpen,Is.True,before+"\n"+immediate);
                Assert.That(CollectionCompleteCelebrationView.CanPresent,Is.False,
                    "Pending collection celebrations must wait while the commands panel is open.");
                yield return null;
                string nextFrame=DescribeState(panel,cat,"After one Update");
                Assert.That(CatCompanionPanel.IsAnyOpen,Is.True,before+"\n"+immediate+"\n"+nextFrame);
                var buttons=panel.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("CommandButton")).ToArray();
                Assert.That(buttons.Length,Is.EqualTo(3));
                foreach(var button in buttons)
                {
                    var rect=(RectTransform)button.transform;
                    var photo=button.transform.parent.Find("PoseFrame") as RectTransform;
                    var corners=new Vector3[4];rect.GetWorldCorners(corners);
                    var picture=new Vector3[4];photo.GetWorldCorners(picture);
                    Assert.That(corners[1].y+5,Is.LessThan(picture[0].y),"Photo and button need a clear gap.");
                    Assert.That(corners[0].x,Is.GreaterThanOrEqualTo(0));
                    Assert.That(corners[2].x,Is.LessThanOrEqualTo(Screen.width));
                    var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
                    var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                    Assert.That(hits.First().gameObject.GetComponentInParent<Button>(),Is.EqualTo(button),"The visible command must receive its own click.");
                }
                panel.Show(true);yield return null;
                Assert.That(CollectionCompleteCelebrationView.CanPresent,Is.False,
                    "Pending collection celebrations must also wait while the guide is open.");
                Assert.That(panel.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("CommandButton")),Is.False);
                var scroll=panel.GetComponentInChildren<ScrollRect>();Assert.That(scroll,Is.Not.Null);
                Assert.That(scroll.viewport.rect.height,Is.GreaterThan(200));
                panel.Close();yield return null;
                Assert.That(cat.HasScopedInputBlock,Is.False);
            }
        }
        finally
        {
            Object.FindAnyObjectByType<CatCompanionPanel>()?.Close();
            qa.GetMethod("Resolution").Invoke(null,new object[]{width,height});
            if(had)PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey,old);else PlayerPrefs.DeleteKey(PetTutorialHint.OnboardingCompletedKey);
            foreach(var state in collectionStates)if(state.Key!=null)state.Key.enabled=state.Value;
        }
    }

    const BindingFlags InstanceFlags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static T Field<T>(CatCompanionPanel panel,string name)=>(T)typeof(CatCompanionPanel).GetField(name,InstanceFlags).GetValue(panel);
    static bool AnotherPanel(CatCompanionPanel panel)=>(bool)typeof(CatCompanionPanel).GetProperty("AnotherPanel",InstanceFlags).GetValue(panel);
    static bool ReadyForCommands(CatCompanionPanel panel,CatMovement cat)
    {
        var menu=Field<MainPanelController>(panel,"menu");
        return !AnotherPanel(panel) && PetTutorialHint.IsOnboardingCompleted &&
               (menu==null || !menu.IsOpen) && !cat.HasScopedInputBlock;
    }
    static string DescribeState(CatCompanionPanel panel,CatMovement liveCat,string phase)
    {
        var singleton=(CatCompanionPanel)typeof(CatCompanionPanel).GetField("instance",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        var cached=Field<CatMovement>(panel,"cat");
        var menu=Field<MainPanelController>(panel,"menu");
        var modal=Field<GameObject>(panel,"modal");
        return phase+": panel="+Identity(panel)+"; singleton="+Identity(singleton)+
            "; panelCount="+Object.FindObjectsByType<CatCompanionPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length+
            "; cachedCat="+Identity(cached)+"; liveCat="+Identity(liveCat)+"; sameCat="+(cached==liveCat)+
            "; modal="+(modal!=null && modal.activeSelf)+"; singletonOpen="+CatCompanionPanel.IsAnyOpen+
            "; menu="+Identity(menu)+"/"+(menu!=null && menu.IsOpen)+
            "; onboarding="+PetTutorialHint.IsOnboardingCompleted+
            "; introduction="+PlayerPrefs.GetInt(PetTutorialHint.IntroductionCompletedKey,0)+
            "; AnotherPanel="+AnotherPanel(panel)+"; title="+TitleScreen.IsShowing+
            "; mini="+HomeUiFlow.IsMiniGameVisible+"; dialogue="+CatDialogueView.IsAnyVisible+
            "; settings="+SettingsPanel.IsAnyOpen+"; shop="+ShopPanelController.IsAnyOpen+
            "; quests="+QuestPanelController.IsAnyOpen+"; rooms="+RoomSelectorPanel.IsAnyOpen+
            "; breeds="+CatBreedShopPanel.IsAnyOpen+"; games="+GamesHubPanel.IsAnyOpen+
            "; leaderboard="+LeaderboardPanel.IsAnyOpen+"; privacy="+PrivacyDataPanel.IsAnyOpen+
            "; offline="+WhileYouWereAwayPopup.IsAnyOpen+"; onboardingModal="+OnboardingCelebrationView.IsAnyOpen+
            "; collection="+CollectionCompleteCelebrationView.IsAnyOpen+"; level="+HomeLevelUpCelebrationView.IsAnyOpen;
    }
    static string Identity(Component component)=>component==null?"null/destroyed":
        component.GetType().Name+"#"+component.GetInstanceID()+"@"+component.gameObject.scene.name+
        "/active="+component.gameObject.activeInHierarchy;
}
