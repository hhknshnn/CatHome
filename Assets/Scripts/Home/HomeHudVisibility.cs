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
        bool otherWindow=HomeUiFlow.IsMiniGameVisible || CatCompanionPanel.IsAnyOpen || TitleScreen.IsShowing || ShopPanelController.IsAnyOpen || CatBreedShopPanel.IsAnyOpen ||
            RoomSelectorPanel.IsAnyOpen || QuestPanelController.IsAnyOpen || SettingsPanel.IsAnyOpen || PrivacyDataPanel.IsAnyOpen ||
            GamesHubPanel.IsAnyOpen || LeaderboardPanel.IsAnyOpen || WhileYouWereAwayPopup.IsAnyOpen ||
            HomeLevelUpCelebrationView.IsAnyOpen || CollectionCompleteCelebrationView.IsAnyOpen || OnboardingCelebrationView.IsAnyOpen;
        bool blocked=otherWindow||CatDialogueView.IsAnyVisible;
        bool explainNeeds=!otherWindow&&CatDialogueView.IsAnyVisible&&PetTutorialHint.IsShowingNeedsGuide;
        if(targets==null)return;
        if(blocked&&!hidden)
        {
            previousAlpha=new float[targets.Length];previousInteraction=new bool[targets.Length];previousRaycasts=new bool[targets.Length];
            for(int i=0;i<targets.Length;i++)if(targets[i]!=null){previousAlpha[i]=targets[i].alpha;previousInteraction[i]=targets[i].interactable;previousRaycasts[i]=targets[i].blocksRaycasts;}
            hidden=true;
        }
        if(blocked)
        {
            for(int i=0;i<targets.Length;i++)
            {
                var group=targets[i];if(group==null)continue;
                bool isNeed=group.name=="FoodBar"||group.name=="HungerUI"||group.name=="ThirstUI"||group.name=="EnergyUI";
                // Show only what this lesson explains. The modal still owns all
                // input, and any other window keeps the normal HUD suppression.
                group.alpha=explainNeeds&&isNeed?previousAlpha[i]:0f;
                group.interactable=group.blocksRaycasts=false;
            }
        }
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
