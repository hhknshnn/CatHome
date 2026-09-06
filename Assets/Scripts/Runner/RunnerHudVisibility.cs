using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class RunnerHudVisibility : MonoBehaviour
{
    private CanvasGroup group;
    private CatRunnerGameController game;
    private void Awake(){group=GetComponent<CanvasGroup>();game=GetComponentInParent<CatRunnerGameController>();}
    private void LateUpdate()
    {
        bool visible=game!=null && game.IsGameplayActive;
        group.alpha=visible?1:0;group.interactable=visible;group.blocksRaycasts=visible;
    }
}
