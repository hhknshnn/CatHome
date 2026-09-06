using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>Read-only reward feedback. Grants remain owned by their services.</summary>
public sealed class HomeRewardToast : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    private readonly Queue<string> pending=new Queue<string>();
    private CanvasGroup group;
    private TMP_Text message;
    private CatMovement movement;
    private Canvas ownerCanvas;
    private string seenLogin;
    private float remaining;
    private void Awake()
    {
        ownerCanvas=GetComponentInParent<Canvas>();
        var safe=PremiumUiElements.Rect("SafeArea",transform);PremiumUiElements.Fill(safe);safe.gameObject.AddComponent<SafeAreaRect>();
        var card=PremiumUiElements.Panel("RewardNotice",safe,PremiumUiStyle.Mint,0,-165,670,92,24);
        card.rectTransform.anchorMin=card.rectTransform.anchorMax=new Vector2(.5f,1);
        group=card.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
        message=PremiumUiElements.Label("Message",card.transform,font,22,PremiumUiStyle.Ink,0,0,614,72,TextAlignmentOptions.Center);
        message.richText=false;
    }
    private void OnEnable(){AchievementService.RewardGranted+=OnAchievement;}
    private void OnDisable(){AchievementService.RewardGranted-=OnAchievement;if(group!=null)group.alpha=0;}
    private void OnAchievement(AchievementDefinition definition)
    {
        var reward=GameContentCopy.Text($"{definition.Coins} jeton",$"{definition.Coins} coins");
        if(definition.Diamonds>0)reward=GameContentCopy.Text($"{definition.Diamonds} elmas",$"{definition.Diamonds} diamonds");
        pending.Enqueue(GameContentCopy.Text("Başarım tamamlandı","Achievement unlocked")+"\n"+definition.Title+" · +"+reward);
    }
    private void Update()
    {
        if(group==null)return;
        float safeWidth=Screen.safeArea.width/Mathf.Max(.001f,ownerCanvas!=null?ownerCanvas.scaleFactor:1f);
        ((RectTransform)group.transform).anchoredPosition=new Vector2(0,safeWidth<1760f?-286f:-165f);
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
        bool blocked=movement==null || movement.IsInputBlocked || TitleScreen.IsShowing || WhileYouWereAwayPopup.IsAnyOpen || ShopPanelController.IsAnyOpen || QuestPanelController.IsAnyOpen || SettingsPanel.IsAnyOpen || HomeLevelUpCelebrationView.IsAnyOpen;
        if(blocked){group.alpha=0;return;}
        if(remaining<=0 && pending.Count>0){message.text=pending.Dequeue();remaining=5f;}
        if(remaining>0){remaining-=Time.unscaledDeltaTime;group.alpha=1;}else group.alpha=0;
    }
#if UNITY_EDITOR
    public void EditorConfigure(TMP_FontAsset value){font=value;}
#endif
}
