using System;
using CatHome.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Premium diamond-package selector. It never grants currency by itself: a
/// platform purchase provider must validate the transaction and then call
/// CompleteVerifiedPurchase.
/// </summary>
[DisallowMultipleComponent]
public sealed class DiamondStorePanel : MonoBehaviour
{
    [Serializable]
    private struct PackView
    {
        public string productId;
        public Button button;
        public TMP_Text amountText;
        public GameObject recommendedBadge;
    }

    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private PackView[] packs = new PackView[0];

    private bool listenersBound;
    private long recommendedMinimum;

    public static event Action<string> PurchaseRequested;
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        BindListeners();
        SetVisible(false);
    }

    private void OnEnable()
    {
        BindListeners();
        EconomyService.AnyBalanceChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        EconomyService.AnyBalanceChanged -= Refresh;
    }

    public void Open(long minimumDiamonds = 0L)
    {
        recommendedMinimum = Math.Max(0L, minimumDiamonds);
        IsOpen = true;
        SetVisible(true);
        SetFeedback("CHOOSE A DIAMOND PACK");
        Refresh();
    }

    public void Close()
    {
        IsOpen = false;
        SetVisible(false);
    }

    public EconomyTransactionResult CompleteVerifiedPurchase(
        string productId,
        string transactionId)
    {
        EconomyTransactionResult result =
            DiamondPackCatalog.GrantConfirmedPurchase(productId, transactionId);
        SetFeedback(result.IsSettled
            ? "DIAMONDS ADDED TO YOUR WALLET!"
            : "PURCHASE COULD NOT BE COMPLETED.");
        Refresh();
        return result;
    }

    private void RequestPack(int index)
    {
        if (packs == null || index < 0 || index >= packs.Length)
            return;
        string productId = packs[index].productId;
        if (string.IsNullOrEmpty(productId))
            return;

        SetFeedback("CONNECTING TO YOUR DEVICE STORE...");
        if (PurchaseRequested != null)
        {
            PurchaseRequested.Invoke(productId);
            return;
        }

        SetFeedback("PACKAGE READY • STORE CONNECTION REQUIRED");
    }

    private void Refresh()
    {
        if (balanceText != null)
            balanceText.text = EconomyService.Diamonds.ToString("N0") + " DIAMONDS";

        if (packs == null)
            return;
        bool recommendedAssigned = false;
        for (int i = 0; i < packs.Length; i++)
        {
            DiamondPackDefinition definition;
            bool found = DiamondPackCatalog.TryGet(packs[i].productId, out definition);
            if (packs[i].amountText != null)
                packs[i].amountText.text = found
                    ? definition.DiamondAmount.ToString("N0")
                    : "—";

            bool recommended = !recommendedAssigned && found &&
                               recommendedMinimum > 0L &&
                               definition.DiamondAmount >= recommendedMinimum;
            if (packs[i].recommendedBadge != null)
                packs[i].recommendedBadge.SetActive(recommended);
            if (recommended)
                recommendedAssigned = true;
        }
    }

    private void SetFeedback(string value)
    {
        if (feedbackText != null)
            feedbackText.text = value;
    }

    private void SetVisible(bool visible)
    {
        if (rootGroup == null)
            return;
        rootGroup.alpha = visible ? 1f : 0f;
        rootGroup.interactable = visible;
        rootGroup.blocksRaycasts = visible;
    }

    private void BindListeners()
    {
        if (listenersBound)
            return;
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (packs != null)
        {
            for (int i = 0; i < packs.Length; i++)
            {
                if (packs[i].button == null)
                    continue;
                int captured = i;
                packs[i].button.onClick.AddListener(() => RequestPack(captured));
            }
        }
        listenersBound = true;
    }

    private void OnDestroy()
    {
        PurchaseRequested = null;
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        CanvasGroup group,
        Button close,
        TMP_Text balance,
        TMP_Text feedback,
        string[] productIds,
        Button[] buttons,
        TMP_Text[] amounts,
        GameObject[] recommendedBadges)
    {
        rootGroup = group;
        closeButton = close;
        balanceText = balance;
        feedbackText = feedback;
        int count = productIds != null ? productIds.Length : 0;
        packs = new PackView[count];
        for (int i = 0; i < count; i++)
        {
            packs[i].productId = productIds[i];
            packs[i].button = buttons != null && i < buttons.Length ? buttons[i] : null;
            packs[i].amountText = amounts != null && i < amounts.Length ? amounts[i] : null;
            packs[i].recommendedBadge = recommendedBadges != null && i < recommendedBadges.Length
                ? recommendedBadges[i]
                : null;
        }
    }
#endif
}
