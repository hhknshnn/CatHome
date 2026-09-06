using UnityEngine.SceneManagement;

// Shared home controls must stay out of the mini-game and dialogue layers.
public static class HomeUiFlow
{
    public static bool IsMiniGameVisible =>
        SceneManager.GetSceneByName(CatRunnerLauncher.RunnerSceneName).isLoaded ||
        SceneManager.GetSceneByName(CatCatchLauncher.CatchSceneName).isLoaded;
    public static bool IsHomeControlBlocked => IsMiniGameVisible || CatDialogueView.IsAnyVisible ||
        WhileYouWereAwayPopup.IsAnyOpen || HomeLevelUpCelebrationView.IsAnyOpen ||
        CollectionCompleteCelebrationView.IsAnyOpen || OnboardingCelebrationView.IsAnyOpen;
}
