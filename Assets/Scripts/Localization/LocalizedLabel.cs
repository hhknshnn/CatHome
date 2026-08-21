using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LocalizedLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private string textKey;

    private void OnEnable()
    {
        GameLanguageService.Changed += Refresh;
        Refresh();
    }

    private void OnDisable() => GameLanguageService.Changed -= Refresh;

    public void EditorConfigure(TMP_Text label, string key)
    {
        target = label;
        textKey = key;
        Refresh();
    }

    public void Refresh()
    {
        if (target == null)
            target = GetComponent<TMP_Text>();
        if (target != null && !string.IsNullOrWhiteSpace(textKey))
            target.text = GameLanguageService.Text(textKey);
    }
}
