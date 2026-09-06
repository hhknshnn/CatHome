using CatHome.Economy;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(TMP_Text))]
public sealed class CurrencyBalanceLabel : MonoBehaviour
{
    [SerializeField] private CurrencyType currency;
    public void Configure(CurrencyType value) { currency = value; Refresh(); }
    private void OnEnable() { EconomyService.AnyBalanceChanged += Refresh; Refresh(); }
    private void OnDisable() => EconomyService.AnyBalanceChanged -= Refresh;
    private void Refresh() => GetComponent<TMP_Text>().text =
        (currency == CurrencyType.Diamond ? EconomyService.Diamonds : EconomyService.Coins).ToString("N0");
}
