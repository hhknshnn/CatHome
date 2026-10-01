using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object=UnityEngine.Object;

// Presentation QA only: calls views on an isolated save, never purchase/claim/auth actions.
public static class UiQaVisualTour
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Queue<string> cases;
    static string current;
    static int phase;
    static double next;
    public static string Status {get;private set;}="Idle";
    public const string DirectoryPath="Docs/QA/UIUX_2026-09-06/screens";
    public static string OutputDirectory {get;set;}=DirectoryPath;
    public static T Find<T>() where T:Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include).OrderByDescending(x=>x.gameObject.activeInHierarchy).FirstOrDefault();
    public static object Call(object target,string method,params object[] args)
    {if(target==null)throw new Exception("Missing target for "+method);return target.GetType().GetMethod(method,Flags).Invoke(target,args);}
    public static void Set(object target,string field,object value)=>target.GetType().GetField(field,Flags).SetValue(target,value);
    public static T Get<T>(object target,string field)=>(T)target.GetType().GetField(field,Flags).GetValue(target);
    public static void StartHome()
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new Exception("Use an isolated Play session.");
        cases=new Queue<string>(new[]{"01_Title","02_Credits","03_NewGame","04_AccountChoice","05_Home","06_Menu","07_ShopRoom","08_ShopCat","09_ShopHome","10_MyCat","11_Rooms","12_Settings","13_Privacy","14_DeleteConfirmation","15_Quests","16_Dailies","17_Purchase","18_Prerequisite","19_DiamondConfirmation","20_DiamondPacks","21_Games","22_LeaderboardEmpty","23_LeaderboardExample","24_ReturnExample","25_LevelUpExample","26_CollectionExample","27_OnboardingExample","28_DialogueExample","29_NameExample"});
        Find<PetTutorialHint>().enabled=false;
        phase=0;next=0;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    public static void StartCases(params string[] names)
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new Exception("Use isolated Play.");
        Find<PetTutorialHint>().enabled=false;cases=new Queue<string>(names);phase=0;next=0;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        if(!Application.isPlaying){EditorApplication.update-=Tick;Status="Stopped";return;}
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            if(phase==0){Clear();if(cases.Count==0){EditorApplication.update-=Tick;Status="Complete";return;}current=cases.Dequeue();Status=current;phase=1;next=EditorApplication.timeSinceStartup+1;}
            else if(phase==1){Show(current);phase=2;next=EditorApplication.timeSinceStartup+2.2;}
            else {Capture(current+"_Final");phase=0;next=EditorApplication.timeSinceStartup+1;}
        }
        catch(Exception e){Debug.LogException(e);Status="Failed: "+current+": "+e.Message;EditorApplication.update-=Tick;}
    }
    public static void Clear()
    {
        Find<MainPanelController>()?.RequestClose();
        for(int i=0;i<4;i++)Find<ShopPanelController>()?.RequestClose();Find<CatBreedShopPanel>()?.RequestClose();Find<RoomSelectorPanel>()?.RequestClose();
        Find<QuestPanelController>()?.RequestClose();Find<SettingsPanel>()?.RequestClose();Find<PrivacyDataPanel>()?.RequestClose();Find<PrivacyDataPanel>()?.RequestClose();
        Find<GamesHubPanel>()?.Hide();Find<LeaderboardPanel>()?.Hide();Find<WhileYouWereAwayPopup>()?.Close();
        var title=Find<TitleScreen>();if(title!=null){Call(title,"OnCreditsClose");Call(title,"OnNewGameCancel");Call(title,"SetAccountChoiceVisible",false);Call(title,"HideImmediate");}
        var level=Find<HomeLevelUpCelebrationView>();if(level!=null&&HomeLevelUpCelebrationView.IsAnyOpen)Call(level,"BeginClose");
        var collection=Find<CollectionCompleteCelebrationView>();if(collection!=null){Call(collection,"HideImmediate");Set(collection,"isOpen",false);typeof(CollectionCompleteCelebrationView).GetProperty("IsAnyOpen").GetSetMethod(true).Invoke(null,new object[]{false});}
        var onboarding=Find<OnboardingCelebrationView>();if(onboarding!=null&&onboarding.IsOpen){onboarding.gameObject.SetActive(false);onboarding.gameObject.SetActive(true);}
        Find<CatDialogueView>()?.Hide();
    }
    public static void Show(string key)
    {
        if(key.Contains("Title")){Find<TitleScreen>().RequestShow();return;}
        if(key.Contains("Credits")){Find<TitleScreen>().RequestShow();Call(Find<TitleScreen>(),"OnCredits");return;}
        if(key.Contains("NewGame")){Find<TitleScreen>().RequestShow();Call(Find<TitleScreen>(),"OnNewGame");return;}
        if(key.Contains("AccountChoice")){Find<TitleScreen>().RequestShow();Call(Find<TitleScreen>(),"ShowAccountChoice",false);return;}
        if(key.Contains("Menu")){Find<MainPanelController>().RequestOpen();return;}
        if(key.Contains("ShopRoom")){Find<ShopPanelController>().RequestOpen(HomeStoreCategory.Room);return;}
        if(key.Contains("ShopCat")){Find<ShopPanelController>().RequestOpen(HomeStoreCategory.Cat);return;}
        if(key.Contains("ShopHome")){Find<ShopPanelController>().RequestOpen(HomeStoreCategory.Home);return;}
        if(key.Contains("MyCat")){Find<CatBreedShopPanel>().RequestOpen();return;}
        if(key.Contains("Rooms")){Find<RoomSelectorPanel>().RequestOpen();return;}
        if(key.Contains("Settings")){Find<SettingsPanel>().RequestOpen();return;}
        if(key.Contains("Privacy")){Find<PrivacyDataPanel>().RequestOpen();return;}
        if(key.Contains("DeleteConfirmation")){Find<PrivacyDataPanel>().RequestOpen();Call(Find<PrivacyDataPanel>(),"SetConfirmation",true);return;}
        if(key.Contains("Quests")||key.Contains("Dailies")){Find<QuestPanelController>().RequestOpen();Call(Find<QuestPanelController>(),"SelectDaily",key.Contains("Dailies"));return;}
        if(key.Contains("Purchase")||key.Contains("Prerequisite")||key.Contains("DiamondConfirmation"))
        {
            var shop=Find<ShopPanelController>();shop.RequestOpen(HomeStoreCategory.Room);
            Call(shop,"OpenPurchaseDialog","room.bookshelf",key.Contains("Prerequisite")?"room.colorful-book-set":null);
            if(key.Contains("DiamondConfirmation"))
            {
                Set(shop,"diamondConfirmationOpen",true);
                Get<TMP_Text>(shop,"diamondConfirmationTitle").text=GameLanguageService.Text("shop.confirm");
                Get<TMP_Text>(shop,"diamondConfirmationMessage").text=GameLanguageService.Format("shop.confirm_body","Uzun kitaplık","11");
                Get<TMP_Text>(shop,"diamondConfirmationButtonText").text=GameLanguageService.Format("shop.buy_diamonds","11");
                Get<RawImage>(shop,"diamondConfirmationIcon").texture=Get<RawImage>(shop,"purchaseIcon").texture;
                Call(shop,"SetDiamondConfirmationVisible",true);
            }
            return;
        }
        if(key.Contains("DiamondPacks")){Find<ShopPanelController>().RequestDiamondStore();return;}
        if(key.Contains("Games")){Find<GamesHubPanel>().Show();return;}
        if(key.Contains("Leaderboard"))
        {
            var board=Find<LeaderboardPanel>();board.gameObject.SetActive(true);Set(board,"open",true);
            var group=Get<CanvasGroup>(board,"rootGroup");group.alpha=1;group.interactable=group.blocksRaycasts=true;
            Call(board,"RefreshIdentity");Call(board,"RefreshHeaders");var snapshot=new CompetitionSnapshot{isOfflineCopy=true};
            if(key.Contains("Example")){snapshot.totalPlayers=124;for(int i=0;i<6;i++)snapshot.entries.Add(new CompetitionEntry{rank=i+1,nickname=new[]{"Misket","Pamuk","Tarçın","Luna","Zeytin","Boncuk"}[i],score=12400-i*1050,isCurrentPlayer=i==2});snapshot.currentPlayer=snapshot.entries[2];}
            Call(board,"ApplySnapshot",snapshot);return;
        }
        if(key.Contains("Return")){var view=Find<WhileYouWereAwayPopup>();Call(view,"Populate",new CatHomeSaveSystem.OfflineReturnSummary(999,TimeSpan.FromHours(2),true,false,85f,35f,90f,22f,65f,85f));Call(view,"Show");return;}
        if(key.Contains("LevelUp")){Find<HomeLevelUpCelebrationView>().Show(4);return;}
        if(key.Contains("Collection")){Call(Find<CollectionCompleteCelebrationView>(),"Show",new CollectionMilestone("qa.example","Salon",HomeRoomService.LivingRoomId,500,0,15));return;}
        if(key.Contains("Onboarding")){Find<OnboardingCelebrationView>().Show();return;}
        if(key.Contains("Dialogue")){Find<CatDialogueView>().ShowMessage("Misket","Birlikte evimizi keşfedelim. Biraz oyun oynamaya ne dersin?");return;}
        if(key.Contains("Name")){Find<CatDialogueView>().ShowNamePrompt("Bana bir isim verir misin?");return;}
    }
    public static void Capture(string name)
    {
        Directory.CreateDirectory(OutputDirectory);Canvas.ForceUpdateCanvases();
        ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory,name+".png"));
        File.WriteAllText(Path.Combine(OutputDirectory,name+".layout.txt"),Measure());
    }
    public static string Measure()
    {
        var rows=new List<Tuple<Button,Rect>>();var output=new System.Text.StringBuilder();
        output.AppendLine($"Screen {Screen.width}x{Screen.height}; SafeArea {Screen.safeArea}");
        foreach(var button in Object.FindObjectsByType<Button>())
        {
            if(!button.IsActive()||!button.IsInteractable()||button.targetGraphic==null||!button.targetGraphic.enabled||button.targetGraphic.color.a<.03f)continue;
            if(button.GetComponentsInParent<Canvas>().Any(c=>!c.isActiveAndEnabled))continue;
            float alpha=1;foreach(var g in button.GetComponentsInParent<CanvasGroup>())alpha*=g.alpha;if(alpha<.05f)continue;
            var rect=ScreenRect(button.transform as RectTransform);if(rect.width>Screen.width*.9f&&rect.height>Screen.height*.7f)continue;
            bool clipped=false;foreach(var mask in button.GetComponentsInParent<RectMask2D>())if(!ScreenRect(mask.rectTransform).Overlaps(rect))clipped=true;
            if(clipped)continue;rows.Add(Tuple.Create(button,rect));output.AppendLine(button.name+" "+rect);
            if(!Screen.safeArea.Contains(rect.min)||!Screen.safeArea.Contains(rect.max))output.AppendLine("OUTSIDE "+button.name);
            bool centerVisible=true;
            foreach(var mask in button.GetComponentsInParent<RectMask2D>())
                if(!ScreenRect(mask.rectTransform).Contains(rect.center))centerVisible=false;
            var events=UnityEngine.EventSystems.EventSystem.current;
            if(centerVisible&&events!=null)
            {
                var pointer=new UnityEngine.EventSystems.PointerEventData(events){position=rect.center};
                var hits=new List<UnityEngine.EventSystems.RaycastResult>();events.RaycastAll(pointer,hits);
                var receiver=hits.Count==0?null:hits[0].gameObject.GetComponentInParent<Button>();
                if(receiver!=button)output.AppendLine("UNCLICKABLE "+button.name+" hit="+(hits.Count==0?"none":hits[0].gameObject.name));
            }
        }
        for(int i=0;i<rows.Count;i++)for(int j=i+1;j<rows.Count;j++)
        {
            var a=rows[i];var b=rows[j];if(a.Item1.transform.IsChildOf(b.Item1.transform)||b.Item1.transform.IsChildOf(a.Item1.transform))continue;
            if(a.Item2.Overlaps(b.Item2))output.AppendLine("OVERLAP "+a.Item1.name+" / "+b.Item1.name);
        }
        return output.ToString();
    }
    static Rect ScreenRect(RectTransform r)
    {
        var c=new Vector3[4];r.GetWorldCorners(c);var canvas=r.GetComponentInParent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        Vector2 min=RectTransformUtility.WorldToScreenPoint(camera,c[0]),max=min;
        foreach(var v in c){var p=RectTransformUtility.WorldToScreenPoint(camera,v);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    public static void Resolution(int width,int height)
    {
        var asm=typeof(Editor).Assembly;var st=asm.GetType("UnityEditor.GameViewSizes");var t=asm.GetType("UnityEditor.GameViewSize");
        var sizes=st.GetProperty("instance",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.FlattenHierarchy).GetValue(null);
        var group=st.GetProperty("currentGroup",Flags).GetValue(sizes);var gt=group.GetType();int count=(int)gt.GetMethod("GetTotalCount",Flags).Invoke(group,null),index=-1;
        for(int i=0;i<count;i++){var size=gt.GetMethod("GetGameViewSize",Flags).Invoke(group,new object[]{i});if((int)t.GetProperty("width",Flags).GetValue(size)==width&&(int)t.GetProperty("height",Flags).GetValue(size)==height){index=i;break;}}
        if(index<0){var kind=Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution");var size=Activator.CreateInstance(t,Flags,null,new object[]{kind,width,height,"UI QA "+width+"x"+height},System.Globalization.CultureInfo.InvariantCulture);gt.GetMethod("AddCustomSize",Flags).Invoke(group,new[]{size});index=count;}
        var view=EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,index);view.Repaint();
    }
}

