using TMPro;
using UnityEngine;

public sealed class MiniGameResultView : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text rewardText;
    public void Present(int score,long reward)
    {
        if(scoreText!=null) scoreText.text=score.ToString("N0");
        if(rewardText!=null) rewardText.text="+"+reward.ToString("N0");
    }
    public void Configure(TMP_Text score,TMP_Text reward) {scoreText=score;rewardText=reward;}
}
