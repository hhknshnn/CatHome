using UnityEngine;

// Keeps persistent controls out of full-screen flows without stopping need systems.
public sealed class HomeHudVisibility : MonoBehaviour
{
    [SerializeField] private CanvasGroup[] targets;
    private float[] previousAlpha;
    private bool[] previousInteraction,previousRaycasts;
    private bool hidden;
    public void Configure(CanvasGroup[] groups){Restore();targets=groups;}
    private void OnEnable()
    {
        // Prefab-backed HUD roots may be replaced by another builder. Resolve
        // within this named Canvas so missing saved references cannot expose HUD
        // controls behind a modal, or bind to another room's inactive UI.
        var resolved=new System.Collections.Generic.List<CanvasGroup>();
        foreach(var rect in GetComponentsInChildren<RectTransform>(true))
        {
            if(rect.name!="FoodBar"&&rect.name!="ThirstUI"&&rect.name!="EnergyUI"&&rect.GetComponent<MobileJoystick>()==null)continue;
            var group=rect.GetComponent<CanvasGroup>();
            if(group==null)group=rect.gameObject.AddComponent<CanvasGroup>();
            resolved.Add(group);
        }
        Configure(resolved.ToArray());
    }
    private void LateUpdate()
    {
        bool blocked=HomeUiFlow.IsHomeControlBlocked || TitleScreen.IsShowing || ShopPanelController.IsAnyOpen || CatBreedShopPanel.IsAnyOpen ||
            RoomSelectorPanel.IsAnyOpen || QuestPanelController.IsAnyOpen || SettingsPanel.IsAnyOpen || PrivacyDataPanel.IsAnyOpen ||
            GamesHubPanel.IsAnyOpen || LeaderboardPanel.IsAnyOpen || WhileYouWereAwayPopup.IsAnyOpen ||
            HomeLevelUpCelebrationView.IsAnyOpen || CollectionCompleteCelebrationView.IsAnyOpen || OnboardingCelebrationView.IsAnyOpen;
        if(targets==null)return;
        if(blocked&&!hidden)
        {
            previousAlpha=new float[targets.Length];previousInteraction=new bool[targets.Length];previousRaycasts=new bool[targets.Length];
            for(int i=0;i<targets.Length;i++)if(targets[i]!=null){previousAlpha[i]=targets[i].alpha;previousInteraction[i]=targets[i].interactable;previousRaycasts[i]=targets[i].blocksRaycasts;}
            hidden=true;
        }
        if(blocked){foreach(var group in targets)if(group!=null){group.alpha=0;group.interactable=group.blocksRaycasts=false;}}
        else Restore();
    }
    private void Restore()
    {
        if(!hidden)return;
        for(int i=0;i<targets.Length;i++)if(targets[i]!=null){targets[i].alpha=previousAlpha[i];targets[i].interactable=previousInteraction[i];targets[i].blocksRaycasts=previousRaycasts[i];}
        hidden=false;
    }
    private void OnDisable()=>Restore();
}
