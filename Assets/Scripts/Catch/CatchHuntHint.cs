using TMPro;
using UnityEngine;

/// <summary>Actionable feedback in the existing HUD, without covering the playing field.</summary>
public sealed class CatchHuntHint:MonoBehaviour
{
    private CatCatchGameController game;
    private CatCatchPlayer cat;
    private TMP_Text label;
    private int score,misses;
    private float messageUntil;
    private string message;
    private void Awake(){label=GetComponent<TMP_Text>();game=GetComponentInParent<CatCatchGameController>();cat=game!=null?game.GetComponentInChildren<CatCatchPlayer>():null;}
    private void OnEnable(){score=misses=0;messageUntil=0;}
    private void Update()
    {
        if(game==null||cat==null||label==null||!game.IsHunting)return;
        if(game.Score>score){message="+"+(game.Score-score)+"  ·  "+GameContentCopy.Text("Güzel yakaladın!","Lovely catch!");messageUntil=Time.time+1.2f;}
        else if(game.StrikesMissed>misses){message=GameContentCopy.Text("Az kaldı! Yeniden bir fare seç.","So close! Choose a mouse and try again.");messageUntil=Time.time+1.2f;}
        score=game.Score;misses=game.StrikesMissed;
        label.text=Time.time<messageUntil?message:cat.IsPouncing?GameContentCopy.Text("Hedefe odaklandım…","Eyes on the prize…"):
            cat.IsBusy?GameContentCopy.Text("Bir nefes, sonra yeni bir hamle.","A little breather before the next move."):
            cat.Prey!=null?GameContentCopy.Text("Peşindeyim! Hazır olunca atılacağım.","On the trail! I'll pounce when ready."):GameLanguageService.Text("catch.hint");
    }
}
