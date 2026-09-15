using UnityEngine;
using UnityEngine.UI;

/// <summary>Feedback for accepted UI actions, never for arbitrary screen taps.</summary>
[DisallowMultipleComponent]
public sealed class GameAudioButton : MonoBehaviour
{
    private Button button;
    private Toggle toggle;
    private void Awake(){button=GetComponent<Button>();toggle=GetComponent<Toggle>();}
    private void OnEnable(){if(button!=null)button.onClick.AddListener(Click);if(toggle!=null)toggle.onValueChanged.AddListener(Toggle);}
    private void OnDisable(){if(button!=null)button.onClick.RemoveListener(Click);if(toggle!=null)toggle.onValueChanged.RemoveListener(Toggle);}
    private void Click()
    {
        // A valid click can hide this panel before our listener runs.
        string id=name.ToLowerInvariant();
        AudioCue cue=id.Contains("close")||id.Contains("cancel")||id.Contains("back")?AudioCue.UIClose:
            id.Contains("toggle")||id.Contains("settingrow")?AudioCue.UIToggle:
            id.Contains("open")||id.Contains("settings")||id.Contains("rooms")||id.Contains("games")?AudioCue.UIOpen:AudioCue.UIClick;
        GameAudio.UI(cue);
    }
    private void Toggle(bool value)=>GameAudio.UI(AudioCue.UIToggle);
    public static void BindVisibleControls()
    {
        foreach(var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
            if(b.GetComponent<GameAudioButton>()==null)b.gameObject.AddComponent<GameAudioButton>();
        foreach(var t in Object.FindObjectsByType<Toggle>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
            if(t.GetComponent<GameAudioButton>()==null)t.gameObject.AddComponent<GameAudioButton>();
    }
}
