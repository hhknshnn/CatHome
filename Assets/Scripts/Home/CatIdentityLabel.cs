using TMPro;
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(TMP_Text))]
public sealed class CatIdentityLabel : MonoBehaviour
{
    private void OnEnable() { CatIdentityService.Changed += Refresh; Refresh(); }
    private void OnDisable() => CatIdentityService.Changed -= Refresh;
    private void Refresh()
    {
        var label = GetComponent<TMP_Text>();
        // Names are player text, including any literal formatting characters.
        label.richText = false;
        label.parseCtrlCharacters = false;
        label.text = CatIdentityService.DisplayName;
    }
}
