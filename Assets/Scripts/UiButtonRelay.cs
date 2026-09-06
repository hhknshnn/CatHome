using UnityEngine;
using UnityEngine.UI;

/// <summary>Serialized relay for an outside-tap target to the dialog's cancel action.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Button))]
public sealed class UiButtonRelay : MonoBehaviour
{
    [SerializeField] private Button target;
    public void Configure(Button value) => target=value;
    private void OnEnable() => GetComponent<Button>().onClick.AddListener(Invoke);
    private void OnDisable() => GetComponent<Button>().onClick.RemoveListener(Invoke);
    private void Invoke() { if(target!=null && target.interactable) target.onClick.Invoke(); }
}
