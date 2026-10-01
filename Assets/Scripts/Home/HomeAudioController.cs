using UnityEngine;
using CatHome.Economy;

/// <summary>Routes real rewards and room events to the authored audio bank.</summary>
[DisallowMultipleComponent]
public sealed class HomeAudioController : MonoBehaviour
{
    private static HomeAudioController instance;
    private float sessionReadyTime;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()=>instance=null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if(instance!=null)return;
        var host=new GameObject("HomeAudioController");DontDestroyOnLoad(host);instance=host.AddComponent<HomeAudioController>();
    }
    private void Awake()
    {
        if(instance!=null&&instance!=this){Destroy(gameObject);return;}instance=this;
        sessionReadyTime=Time.unscaledTime+1.5f;
    }
    private bool Ready=>Time.unscaledTime>=sessionReadyTime&&!TitleScreen.IsShowing&&!HomeUiFlow.IsMiniGameVisible;
    private void OnEnable()
    {
        HomeProgressionService.LeveledUp+=HandleLevelUp;
        HomeStoreService.OwnershipChanged+=HandleOwnership;
        EconomyService.BalanceChanged+=HandleBalance;
        HomeRoomService.CurrentRoomChanged+=HandleRoom;
        CatActivity.Completed+=HandleActivity;
        AchievementService.RewardGranted+=HandleAchievement;
        LocalNotificationService.RefreshSchedules(System.DateTime.UtcNow);
    }
    private void OnDisable()
    {
        HomeProgressionService.LeveledUp-=HandleLevelUp;
        HomeStoreService.OwnershipChanged-=HandleOwnership;
        EconomyService.BalanceChanged-=HandleBalance;
        HomeRoomService.CurrentRoomChanged-=HandleRoom;
        CatActivity.Completed-=HandleActivity;
        AchievementService.RewardGranted-=HandleAchievement;
    }
    private void OnApplicationPause(bool paused){if(paused)LocalNotificationService.NotifyAppPaused(System.DateTime.UtcNow);}
    private void HandleLevelUp(int level){if(Ready)PlayCelebration();}
    private void HandleOwnership(string id){if(Ready&&!string.IsNullOrEmpty(id))GameAudio.UI(AudioCue.Purchase);}
    private void HandleRoom(string id){if(Ready)PlayRoomChange();}
    private void HandleAchievement(AchievementDefinition reward){if(Ready)PlayCelebration();}
    private void HandleActivity(CatActivity activity)
    {
        if(!Ready||activity==null||activity is CatCommandActivity||activity.SupportsContinuousRest)return;
        PlayActivity();
    }
    private void HandleBalance(CurrencyBalanceChange change)
    {
        if(!Ready||!change.IsIncrease||change.Source==EconomySource.SaveLoad||change.Source==EconomySource.Migration||
            change.Source==EconomySource.Debug||change.Source==EconomySource.PurchaseRestore)return;
        GameAudio.UI(change.Currency==CurrencyType.Diamond?AudioCue.Diamond:AudioCue.Coin);
    }
    public static void PlayPurr(){var cat=FindAnyObjectByType<CatVoice>();if(cat!=null)cat.PurrBriefly();}
    public static void PlayCelebration()=>GameAudio.UI(AudioCue.LevelUp);
    public static void PlayCare() { /* Eating follows the actual pose in CatVoice. */ }
    public static void PlayDrink() { /* Drinking follows the actual pose in CatVoice. */ }
    public static void PlayHearts()=>PlayPurr();
    public static void PlayActivity()=>GameAudio.Play(AudioCue.Success,.7f);
    public static void PlayRoomChange()=>GameAudio.UI(AudioCue.RoomChange);
    private void OnDestroy(){if(instance==this)instance=null;}
}
