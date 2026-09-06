using System;
using NUnit.Framework;
using UnityEngine;

public sealed class WhileYouWereAwayPopupTests
{
    private TestLanguageScope language;
    [SetUp] public void SetUp(){language=new TestLanguageScope(GameLanguage.English);}
    [TearDown] public void TearDown(){language.Dispose();}

    [Test]
    public void FourHourNeedRate_DepletesExactlyOneHundredPoints()
    {
        Assert.AreEqual(
            100f,
            GameBalanceConfig.FourHourNeedDepletionPerSecond * 4f * 60f * 60f,
            0.0001f
        );
    }

    [Test]
    public void ActiveBalance_UsesSameFourHourRateForAwakeNeeds()
    {
        Assert.IsTrue(GameBalanceConfig.TryGetActiveBalance(out var balance));
        Assert.AreEqual(GameBalanceConfig.FourHourNeedDepletionPerSecond, balance.HungerDecreasePerSecond);
        Assert.AreEqual(GameBalanceConfig.FourHourNeedDepletionPerSecond, balance.ThirstDecreasePerSecond);
        Assert.AreEqual(GameBalanceConfig.FourHourNeedDepletionPerSecond, balance.AwakeEnergyDecreasePerSecond);
        Assert.AreEqual(90f, balance.SatisfiedActionThreshold);
    }

    [Test]
    public void FourMinutesFiftyNineSeconds_DoesNotMeetDisplayRule()
    {
        var summary = CreateSummary(TimeSpan.FromSeconds(299), false, 60f, 59f);
        Assert.IsFalse(WhileYouWereAwayPopup.MeetsDisplayRequirements(summary, 5f));
    }

    [Test]
    public void FiveMinutes_MeetsDisplayRule()
    {
        var summary = CreateSummary(TimeSpan.FromMinutes(5), false, 60f, 59f);
        Assert.IsTrue(WhileYouWereAwayPopup.MeetsDisplayRequirements(summary, 5f));
    }

    [Test]
    public void ProgressNotApplied_DoesNotMeetDisplayRule()
    {
        var summary = CreateSummary(TimeSpan.FromHours(1), false, 60f, 60f, false);
        Assert.IsFalse(WhileYouWereAwayPopup.MeetsDisplayRequirements(summary, 5f));
    }

    [TestCase(8, "8m")]
    [TestCase(84, "1h 24m")]
    [TestCase(1620, "1d 3h")]
    public void DurationFormatting_IsShortAndNatural(int minutes, string expected)
    {
        Assert.AreEqual(expected, WhileYouWereAwayPopup.FormatDuration(TimeSpan.FromMinutes(minutes)));
    }

    [Test]
    public void SleepingEnergyGain_UsesRestoredCopy()
    {
        var summary = CreateSummary(TimeSpan.FromHours(1), true, 35f, 100f);
        Assert.AreEqual(
            GameLanguageService.Format("return.nap",65),
            WhileYouWereAwayPopup.SelectSummaryCopy(summary)
        );
    }

    [Test]
    public void SleepingAtFullEnergy_DoesNotClaimRestoration()
    {
        var summary = CreateSummary(TimeSpan.FromHours(1), true, 100f, 100f);
        StringAssert.DoesNotContain("restored", WhileYouWereAwayPopup.SelectSummaryCopy(summary));
    }

    [Test]
    public void AwakeEnergyLoss_UsesTiredCopy()
    {
        var summary = CreateSummary(TimeSpan.FromHours(1), false, 80f, 55f);
        Assert.AreEqual(
            GameLanguageService.Text("return.missed"),
            WhileYouWereAwayPopup.SelectSummaryCopy(summary)
        );
    }

    [Test]
    public void AwakeUnchangedEnergy_UsesMissedYouCopy()
    {
        var summary = CreateSummary(TimeSpan.FromHours(1), false, 80f, 80f);
        Assert.AreEqual(
            GameLanguageService.Text("return.missed"),
            WhileYouWereAwayPopup.SelectSummaryCopy(summary)
        );
    }

    [Test]
    public void ScopedInputBlock_DoesNotReleaseExistingSleepStyleLock()
    {
        GameObject cat = new GameObject("TestCat", typeof(CharacterController), typeof(CatMovement));
        GameObject popupOwner = new GameObject("PopupOwner");
        try
        {
            CatMovement movement = cat.GetComponent<CatMovement>();
            movement.SetMovementLocked(true);
            movement.AcquireInputBlock(popupOwner);
            movement.ReleaseInputBlock(popupOwner);
            Assert.IsTrue(movement.IsMovementLocked);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(popupOwner);
            UnityEngine.Object.DestroyImmediate(cat);
        }
    }

    [Test]
    public void PopupLayout_IsExactlyCenteredAt1920By1080()
    {
        GameObject host = CreatePopupFxFixture(
            new Vector2(1920f, 1080f),
            out WhileYouWereAwayPopupFx fx,
            out RectTransform layout,
            out RectTransform visual,
            out _);
        try
        {
            fx.PrepareForOpen();

            Assert.AreEqual(new Vector2(0.5f, 0.5f), layout.anchorMin);
            Assert.AreEqual(layout.anchorMin, layout.anchorMax);
            Assert.AreEqual(Vector2.zero, layout.anchoredPosition);
            Assert.AreEqual(new Vector2(980f, 680f), layout.sizeDelta);
            Assert.AreEqual(Vector3.one, layout.localScale);
            Assert.AreNotEqual(Vector2.zero, visual.anchoredPosition,
                "Only the AnimationContainer should move during the entrance.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void PopupLayout_TallSafeAreaStaysCenteredAndFitsInsideMargins()
    {
        Vector2 safeSize = new Vector2(900f, 1600f);
        GameObject host = CreatePopupFxFixture(
            safeSize,
            out WhileYouWereAwayPopupFx fx,
            out RectTransform layout,
            out _,
            out _);
        try
        {
            fx.PrepareForOpen();

            float expectedScale = WhileYouWereAwayPopupFx.CalculateFitScale(
                safeSize,
                new Vector2(980f, 680f),
                new Vector2(44f, 34f));
            Assert.AreEqual(expectedScale, layout.localScale.x, 0.0001f);
            Assert.AreEqual(expectedScale, layout.localScale.y, 0.0001f);
            Assert.AreEqual(Vector2.zero, layout.anchoredPosition);
            Assert.LessOrEqual(980f * expectedScale, safeSize.x - 88f + 0.01f);
            Assert.LessOrEqual(680f * expectedScale, safeSize.y - 68f + 0.01f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void PopupFx_ReducedMotionUsesImmediateFinalState()
    {
        GameObject host = CreatePopupFxFixture(
            new Vector2(1920f, 1080f),
            out WhileYouWereAwayPopupFx fx,
            out RectTransform layout,
            out RectTransform visual,
            out RectTransform rewardCard);
        try
        {
            Vector2 visualRest = visual.anchoredPosition;
            Quaternion rotationRest = visual.localRotation;
            Vector2 cardRest = rewardCard.anchoredPosition;
            Vector3 cardScale = rewardCard.localScale;

            fx.SetReducedMotion(true);
            fx.PrepareForOpen();
            fx.ApplyOpening(0.15f);
            fx.ApplyClosing(0.75f);

            Assert.AreEqual(visualRest, visual.anchoredPosition);
            Assert.AreEqual(rotationRest, visual.localRotation);
            Assert.AreEqual(cardRest, rewardCard.anchoredPosition);
            Assert.AreEqual(cardScale, rewardCard.localScale);
            Assert.AreEqual(Vector2.zero, layout.anchoredPosition);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    private static GameObject CreatePopupFxFixture(
        Vector2 safeSize,
        out WhileYouWereAwayPopupFx fx,
        out RectTransform layout,
        out RectTransform visual,
        out RectTransform rewardCard)
    {
        GameObject host = new GameObject("PopupFxFixture", typeof(RectTransform));
        GameObject safe = new GameObject("SafeArea", typeof(RectTransform));
        safe.transform.SetParent(host.transform, false);
        RectTransform safeRect = safe.GetComponent<RectTransform>();
        safeRect.sizeDelta = safeSize;

        GameObject layoutObject = new GameObject("CenteredSafeLayout", typeof(RectTransform));
        layoutObject.transform.SetParent(safe.transform, false);
        layout = layoutObject.GetComponent<RectTransform>();

        GameObject visualObject = new GameObject("AnimationContainer", typeof(RectTransform));
        visualObject.transform.SetParent(layoutObject.transform, false);
        visual = visualObject.GetComponent<RectTransform>();
        visual.anchoredPosition = new Vector2(9f, 7f);
        visual.localRotation = Quaternion.Euler(0f, 0f, 3f);

        GameObject cardObject = new GameObject("RewardCard", typeof(RectTransform));
        cardObject.transform.SetParent(visualObject.transform, false);
        rewardCard = cardObject.GetComponent<RectTransform>();
        rewardCard.anchoredPosition = new Vector2(20f, 30f);
        rewardCard.localScale = Vector3.one * 0.96f;

        fx = host.AddComponent<WhileYouWereAwayPopupFx>();
        fx.EditorConfigure(
            safeRect,
            layout,
            visual,
            null,
            null,
            null,
            new[] { rewardCard },
            new[] { rewardCard },
            new LowPolyPanelGraphic[0],
            new RectTransform[0]);
        return host;
    }

    private static CatHomeSaveSystem.OfflineReturnSummary CreateSummary(
        TimeSpan duration,
        bool sleeping,
        float energyBefore,
        float energyAfter,
        bool applied = true)
    {
        return new CatHomeSaveSystem.OfflineReturnSummary(
            1,
            duration,
            applied,
            sleeping,
            76f,
            48f,
            64f,
            29f,
            energyBefore,
            energyAfter
        );
    }
}
