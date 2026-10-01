using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>Read-only reward feedback. Grants remain owned by their services.</summary>
public sealed class HomeRewardToast : MonoBehaviour
{
    private const float NoticeWidth = 670f;
    private const float NoticeHeight = 92f;
    private const float HudGap = 16f;
    [SerializeField] private TMP_FontAsset font;
    private readonly Queue<string> pending=new Queue<string>();
    private CanvasGroup group;
    private TMP_Text message;
    private CatMovement movement;
    private Canvas ownerCanvas;
    private StorybookHudLayout storybookHud;
    private RectTransform notice;
    private string seenLogin;
    private float remaining;
    private void Awake()
    {
        ownerCanvas=GetComponentInParent<Canvas>();
        storybookHud=GetComponentInParent<StorybookHudLayout>();
        // Keep passive rewards above home controls and below modal panels.
        var overlay=GetComponent<Canvas>();
        if(overlay==null)overlay=gameObject.AddComponent<Canvas>();
        overlay.overrideSorting=true;
        overlay.sortingOrder=105;
        if(ownerCanvas!=null)overlay.sortingLayerID=ownerCanvas.sortingLayerID;
        var safe=PremiumUiElements.Rect("SafeArea",transform);PremiumUiElements.Fill(safe);safe.gameObject.AddComponent<SafeAreaRect>();
        var card=PremiumUiElements.Panel("RewardNotice",safe,PremiumUiStyle.Mint,0,-165,NoticeWidth,NoticeHeight,24);
        StorybookScreenStyle.Enamel(card,StorybookScreenStyle.Mint,StorybookScreenStyle.Teal,24f);
        notice=card.rectTransform;
        notice.anchorMin=notice.anchorMax=new Vector2(.5f,1);
        group=card.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
        message=PremiumUiElements.Label("Message",card.transform,font,22,StorybookScreenStyle.Ink,0,0,614,72,TextAlignmentOptions.Center);
        message.richText=false;
    }
    private void OnEnable(){AchievementService.RewardGranted+=OnAchievement;}
    private void OnDisable(){AchievementService.RewardGranted-=OnAchievement;if(group!=null)group.alpha=0;}
    private void LateUpdate(){RefreshLayout();}
    private void RefreshLayout()
    {
        if(notice==null)return;
        if(storybookHud==null || !storybookHud.isActiveAndEnabled)
        {
            float safeWidth=Screen.safeArea.width/Mathf.Max(.001f,ownerCanvas!=null?ownerCanvas.scaleFactor:1f);
            notice.localScale=Vector3.one;
            notice.anchoredPosition=new Vector2(0,safeWidth<1760f?-286f:-165f);
            return;
        }

        Rect safe=Screen.safeArea;
        float scale=Mathf.Min(safe.width/1920f,safe.height/1080f);
        if(scale<=0 || !(notice.parent is RectTransform parent))return;
        var camera=ownerCanvas==null || ownerCanvas.renderMode==RenderMode.ScreenSpaceOverlay?null:ownerCanvas.worldCamera;
        var center=new Vector2(safe.center.x,safe.yMax-(StorybookHudLayout.TopPanelBottom+HudGap+NoticeHeight*.5f)*scale);
        Vector2 local;
        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,center,camera,out local))return;
        var anchor=new Vector2(parent.rect.center.x,parent.rect.yMax);
        notice.anchoredPosition=local-anchor;
        float localScale=scale/Mathf.Max(.001f,parent.lossyScale.x);
        notice.localScale=new Vector3(localScale,localScale,1);
    }
    private void OnAchievement(AchievementDefinition definition)
    {
        var reward=GameContentCopy.Text($"{definition.Coins} jeton",$"{definition.Coins} coins");
        if(definition.Diamonds>0)reward=GameContentCopy.Text($"{definition.Diamonds} elmas",$"{definition.Diamonds} diamonds");
        pending.Enqueue(GameContentCopy.Text("Başarım tamamlandı","Achievement unlocked")+"\n"+definition.Title+" · +"+reward);
    }
    private void Update()
    {
        if(group==null)return;
        string login=DailyRetentionService.LastLoginGrantSummary;
        if(!string.IsNullOrEmpty(login) && login!=seenLogin)
        {
            seenLogin=login;
            int streak=DailyRetentionService.LoginStreak;
            string reward=GameContentCopy.Text($"+{DailyRetentionService.LoginCoinsForStreak(streak)} jeton",$"+{DailyRetentionService.LoginCoinsForStreak(streak)} coins");
            if(streak%7==0)reward+=GameContentCopy.Text(" · +1 elmas"," · +1 diamond");
            pending.Enqueue(GameContentCopy.Text($"Birlikte {streak}. gün",$"Day {streak} together")+"\n"+reward);
        }
        if(movement==null)movement=FindAnyObjectByType<CatMovement>();
        bool blocked=movement==null || movement.IsInputBlocked || TitleScreen.IsShowing || HomeUiFlow.IsHomeControlBlocked ||
            ShopPanelController.IsAnyOpen || QuestPanelController.IsAnyOpen || SettingsPanel.IsAnyOpen ||
            CatBreedShopPanel.IsAnyOpen || RoomSelectorPanel.IsAnyOpen || GamesHubPanel.IsAnyOpen ||
            LeaderboardPanel.IsAnyOpen || PrivacyDataPanel.IsAnyOpen || CatJournalPanel.IsAnyOpen || HomeEditModeController.IsAnyOpen;
        if(blocked){group.alpha=0;return;}
        if(remaining<=0 && pending.Count>0){message.text=pending.Dequeue();remaining=5f;}
        if(remaining>0){remaining-=Time.unscaledDeltaTime;group.alpha=1;}else group.alpha=0;
    }
#if UNITY_EDITOR
    public void EditorConfigure(TMP_FontAsset value){font=value;}
#endif
}
