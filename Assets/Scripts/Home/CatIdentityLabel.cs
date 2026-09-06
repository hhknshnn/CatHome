using TMPro;
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(TMP_Text))]
public sealed class CatIdentityLabel : MonoBehaviour
{
    private void OnEnable() { CatIdentityService.Changed += Refresh; Refresh(); }
    private void OnDisable() => CatIdentityService.Changed -= Refresh;
    private void Refresh() => GetComponent<TMP_Text>().text = CatIdentityService.DisplayName;
}
