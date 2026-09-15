using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CatHome.Economy;

public sealed class ActivityUnlockTests
{
    private readonly System.Collections.Generic.List<GameObject> productInstances=new System.Collections.Generic.List<GameObject>();
    private Scene scene;
    private BallChaseActivity ball;
    private ScratchPostActivity scratch;
    private MouseHuntActivity mouse;
    private SitLookActivity windowWatch;

    [SetUp]
    public void SetUp()
    {
        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        scene = EditorSceneManager.OpenScene(
            SceneArchitectureBuilder.LevelScenePath,
            OpenSceneMode.Additive);
        ball = FindInScene<BallChaseActivity>();
        scratch = FindInScene<ScratchPostActivity>();
        mouse = FindInScene<MouseHuntActivity>();
        windowWatch = FindWindowWatch();
    }

    [TearDown]
    public void TearDown()
    {
        foreach(var instance in productInstances)if(instance!=null)Object.DestroyImmediate(instance);productInstances.Clear();
        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        if (scene.IsValid())
            EditorSceneManager.CloseScene(scene, true);
    }

    [Test]
    public void RoomProducts_StayHiddenUntilPurchasedWithCoins()
    {
        Assert.That(ball, Is.Not.Null);
        Assert.That(scratch, Is.Not.Null);
        Assert.That(mouse, Is.Null,"The retired home hunt must not return.");
        Assert.That(windowWatch, Is.Null, "The retired Window Watch must not return.");

        Assert.That(ball.IsUnlocked, Is.False);
        Assert.That(scratch.IsUnlocked, Is.False);
        Assert.That(ball.IsContentVisible, Is.False);
        Assert.That(scratch.IsContentVisible, Is.False);

        ProgressionService.ApplySavedState(0, 0, 35, 1, new QuestProgressEntry[0]);
        ball.RefreshUnlockPresentation();
        scratch.RefreshUnlockPresentation();
        Assert.That(ball.IsUnlocked, Is.False, "Bond must not bypass store ownership.");
        Assert.That(scratch.IsUnlocked, Is.False, "Bond must not bypass store ownership.");

        long totalPrice = HomeStoreService.BallBasketPrice + HomeStoreService.ScratchPostPrice;
        EconomyService.AddCurrency(CurrencyType.Coin, totalPrice, EconomySource.Debug);
        Assert.That(HomeStoreService.TryPurchase(HomeStoreService.BallBasketId).Succeeded, Is.True);
        Assert.That(HomeStoreService.TryPurchase(HomeStoreService.ScratchPostId).Succeeded, Is.True);
        // CAT purchases enter storage until the player places them. Ownership
        // alone must not reveal a stored toy or leave an invisible interaction.
        ball.RefreshUnlockPresentation();
        scratch.RefreshUnlockPresentation();
        Assert.That(ball.IsUnlocked, Is.False);
        Assert.That(scratch.IsUnlocked, Is.False);
        Assert.That(ball.IsContentVisible, Is.False);
        Assert.That(scratch.IsContentVisible, Is.False);
        Assert.That(HomeStoreService.TrySetStored(HomeStoreService.BallBasketId, false), Is.True);
        Assert.That(HomeStoreService.TrySetStored(HomeStoreService.ScratchPostId, false), Is.True);
        // EditMode scene loading does not reliably invoke the full runtime
        // OnEnable subscription lifecycle, so repaint explicitly here.
        ball.RefreshUnlockPresentation();
        scratch.RefreshUnlockPresentation();
        Assert.That(ball.IsUnlocked, Is.True);
        Assert.That(scratch.IsUnlocked, Is.True);
        Assert.That(ball.IsContentVisible, Is.True);
        Assert.That(scratch.IsContentVisible, Is.True);
        Assert.That(EconomyService.Coins, Is.EqualTo(0));
    }

    [Test]
    public void EnergyCost_CannotOverdraw()
    {
        var root = new GameObject("EnergyTest");
        EnergySystem energy = root.AddComponent<EnergySystem>();
        energy.ApplySavedValue(9f);

        Assert.That(energy.TrySpendEnergy(10f), Is.False);
        Assert.That(energy.CurrentEnergy, Is.EqualTo(9f));
        Assert.That(energy.TrySpendEnergy(8f), Is.True);
        Assert.That(energy.CurrentEnergy, Is.EqualTo(1f));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void ActivityPrompt_ExplainsInsufficientEnergyInsteadOfLookingBroken()
    {
        var root = new GameObject("ActivityPromptEnergyTest");
        EnergySystem energy = root.AddComponent<EnergySystem>();
        energy.ApplySavedValue(0f);

        ProgressionService.ApplySavedState(
            0, 0, BondMilestoneService.WindowWatchBond, 1, new QuestProgressEntry[0]);
        var observation = root.AddComponent<SitLookActivity>();
        observation.EditorConfigure("prompt-fixture", "Bookshelf", CatActivityKind.BookshelfSniff,
            QuestType.WindowWatch, 0, "WATCH", 1f, 4f, root.transform, null, null);

        Assert.That(observation.IsUnlocked, Is.True);
        Assert.That(
            ActivityPromptController.BuildActionText(observation, energy),
            Is.EqualTo(GameContentCopy.Text($"{Mathf.CeilToInt(observation.EnergyCost)} enerji gerekli",$"Need {Mathf.CeilToInt(observation.EnergyCost)} energy")));

        energy.ApplySavedValue(observation.EnergyCost);
        Assert.That(
            ActivityPromptController.BuildActionText(observation, energy),
            Is.EqualTo(GameInteractionCopy.Text(observation.ActionText)));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void CanopyNap_WaitsForTheStarTipiPurchaseAndRestsTheCat()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/StoreProducts/Prefabs/BedroomStarCanopy.prefab");
        Assert.That(prefab, Is.Not.Null, "BedroomStarCanopy prefab is missing.");
        prefab=Object.Instantiate(prefab);productInstances.Add(prefab);

        CanopyNapActivity nap = prefab.GetComponentInChildren<CanopyNapActivity>(true);
        Assert.That(nap, Is.Not.Null, "The star tipi must carry its nap activity.");
        Assert.That(nap.Kind, Is.EqualTo(CatActivityKind.CanopyNap));
        Assert.That(nap.QuestType, Is.EqualTo(QuestType.Sleep));
        Assert.That(nap.StoreProductId, Is.EqualTo(HomeStoreService.BedroomStarCanopyId));
        Assert.That(nap.RequiredBondXp, Is.EqualTo(0L), "Owning the tipi is the only gate.");
        Assert.That(nap.EnergyCost, Is.EqualTo(0f), "A tired cat must always be able to nap.");
        Assert.That(nap.EnergyRestore, Is.GreaterThan(0f));

        // The cat walks in with physics disabled, so the box must not block it.
        BoxCollider box = prefab.GetComponentInChildren<BoxCollider>(true);
        Assert.That(box, Is.Not.Null);
        Assert.That(box.isTrigger, Is.True, "An enterable tent must not be a solid box.");

        Assert.That(nap.IsUnlocked, Is.False, "The nap waits for the store purchase.");
        Assert.That(
            HomeStoreService.TryGetProduct(HomeStoreService.BedroomStarCanopyId, out var product),
            Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(
            HomeStoreService.TryPurchase(HomeStoreService.BedroomStarCanopyId).Succeeded,
            Is.True);
        nap.RefreshUnlockPresentation();
        Assert.That(nap.IsUnlocked, Is.True);
    }

    [Test]
    public void ShowerRinse_WaitsForTheShowerPurchaseAndIsEnterable()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/StoreProducts/Prefabs/BathroomShower.prefab");
        Assert.That(prefab, Is.Not.Null, "BathroomShower prefab is missing.");
        prefab=Object.Instantiate(prefab);productInstances.Add(prefab);

        ShowerRinseActivity rinse = prefab.GetComponentInChildren<ShowerRinseActivity>(true);
        Assert.That(rinse, Is.Not.Null, "The shower must carry its rinse activity.");
        Assert.That(rinse.Kind, Is.EqualTo(CatActivityKind.ShowerRinse));
        Assert.That(rinse.QuestType, Is.EqualTo(QuestType.ShowerRinse));
        Assert.That(rinse.StoreProductId, Is.EqualTo(HomeStoreService.BathroomShowerId));
        Assert.That(rinse.RequiredBondXp, Is.EqualTo(0L), "Owning the shower is the only gate.");

        // The cat walks in with physics disabled, so the box must not block it.
        BoxCollider box = prefab.GetComponentInChildren<BoxCollider>(true);
        Assert.That(box, Is.Not.Null);
        Assert.That(box.isTrigger, Is.True, "An enterable cabin must not be a solid box.");

        // The stand point has to sit on the tray: stepping up is exactly what the
        // cat's 0.005 step offset cannot do on its own.
        Transform stand = prefab.transform.Find("RinseStandPoint");
        Transform door = prefab.transform.Find("RinseDoorPoint");
        Assert.That(stand, Is.Not.Null);
        Assert.That(door, Is.Not.Null);
        Assert.That(stand.localPosition.y, Is.GreaterThan(door.localPosition.y + 0.05f));
        Assert.That(door.localPosition.x, Is.LessThan(-.2f), "The actual glass occupies +X; enter through the open -X half.");
        Assert.That(stand.localPosition.x, Is.LessThan(-.2f), "The rinse must remain visible through the opening.");

        Assert.That(rinse.IsUnlocked, Is.False, "The rinse waits for the store purchase.");
        Assert.That(
            HomeStoreService.TryGetProduct(HomeStoreService.BathroomShowerId, out var product),
            Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(
            HomeStoreService.TryPurchase(HomeStoreService.BathroomShowerId).Succeeded,
            Is.True);
        rinse.RefreshUnlockPresentation();
        Assert.That(rinse.IsUnlocked, Is.True);
    }

    [Test]
    public void SinkSip_WaitsForTheVanityPurchaseAndPerchesOnTheCounter()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/StoreProducts/Prefabs/BathroomVanitySink.prefab");
        Assert.That(prefab, Is.Not.Null, "BathroomVanitySink prefab is missing.");
        prefab=Object.Instantiate(prefab);productInstances.Add(prefab);

        SinkSipActivity sip = prefab.GetComponentInChildren<SinkSipActivity>(true);
        Assert.That(sip, Is.Not.Null, "The vanity must carry its drinking activity.");
        Assert.That(sip.Kind, Is.EqualTo(CatActivityKind.SinkSip));
        Assert.That(sip.QuestType, Is.EqualTo(QuestType.Drink));
        Assert.That(sip.StoreProductId, Is.EqualTo(HomeStoreService.BathroomVanityId));
        Assert.That(sip.ThirstRestore, Is.GreaterThan(0f));

        // The cat perches on the real counter geometry, not its catalogue box.
        BoxCollider box = prefab.GetComponentInChildren<BoxCollider>(true);
        Assert.That(box, Is.Not.Null);
        Assert.That(box.isTrigger, Is.True, "The catalogue box must not block open space.");
        Assert.That(prefab.GetComponentInChildren<MeshCollider>(true), Is.Not.Null,
            "The real furniture geometry must remain solid.");

        Transform perch = prefab.transform.Find("SipPerchPoint");
        Transform floor = prefab.transform.Find("SipFloorPoint");
        Assert.That(perch, Is.Not.Null);
        Assert.That(floor, Is.Not.Null);
        Assert.That(perch.localPosition.y / ProductScale(prefab), Is.GreaterThan(0.7f),
            "The perch must sit on the counter, not on the floor.");
        Assert.That(floor.localPosition.z, Is.GreaterThan(perch.localPosition.z),
            "The cat must start in front of the vanity and hop back towards the basin.");

        Assert.That(sip.IsUnlocked, Is.False, "The sip waits for the store purchase.");
        Assert.That(
            HomeStoreService.TryGetProduct(HomeStoreService.BathroomVanityId, out var product),
            Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(
            HomeStoreService.TryPurchase(HomeStoreService.BathroomVanityId).Succeeded,
            Is.True);
        sip.RefreshUnlockPresentation();
        Assert.That(sip.IsUnlocked, Is.True);
    }

    [Test]
    public void PaperSpin_WaitsForTheToiletPurchaseAndCarriesASpinnableRoll()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/StoreProducts/Prefabs/BathroomToilet.prefab");
        Assert.That(prefab, Is.Not.Null, "BathroomToilet prefab is missing.");
        prefab=Object.Instantiate(prefab);productInstances.Add(prefab);

        PaperSpinActivity spin = prefab.GetComponentInChildren<PaperSpinActivity>(true);
        Assert.That(spin, Is.Not.Null, "The toilet must carry its paper spin activity.");
        Assert.That(spin.Kind, Is.EqualTo(CatActivityKind.PaperSpin));
        Assert.That(spin.QuestType, Is.EqualTo(QuestType.PaperSpin));
        Assert.That(spin.StoreProductId, Is.EqualTo(HomeStoreService.BathroomToiletId));

        // The roll ships as its own FBX under a pivot: one FBX holding both
        // meshes imports lying on its back.
        Transform pivot = spin.RollPivot;
        Assert.That(pivot, Is.Not.Null, "The spin needs its roll pivot.");
        Assert.That(pivot.childCount, Is.GreaterThan(0), "The pivot must carry the roll mesh.");
        Assert.That(pivot.GetComponentInChildren<Renderer>(true), Is.Not.Null);
        Assert.That(pivot.GetComponentInChildren<Collider>(true), Is.Null,
            "The moving roll must not carry a collider of its own.");

        Transform swat = prefab.transform.Find("SwatPoint");
        Assert.That(swat, Is.Not.Null);
        Assert.That(swat.localPosition.y, Is.EqualTo(0f),
            "The cat swats from the floor, so the swat point stays on it.");

        Assert.That(spin.IsUnlocked, Is.False, "The spin waits for the store purchase.");
        Assert.That(
            HomeStoreService.TryGetProduct(HomeStoreService.BathroomToiletId, out var product),
            Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(
            HomeStoreService.TryPurchase(HomeStoreService.BathroomToiletId).Succeeded,
            Is.True);
        spin.RefreshUnlockPresentation();
        Assert.That(spin.IsUnlocked, Is.True);
    }

    [Test]
    public void TowelNest_WaitsForTheCabinetPurchaseAndUsesTheOpenTop()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/StoreProducts/Prefabs/BathroomTowelStorage.prefab");
        Assert.That(prefab, Is.Not.Null, "BathroomTowelStorage prefab is missing.");
        prefab=Object.Instantiate(prefab);productInstances.Add(prefab);

        TowelNestActivity nest = prefab.GetComponentInChildren<TowelNestActivity>(true);
        Assert.That(nest, Is.Not.Null, "The cabinet must carry its nap activity.");
        Assert.That(nest.Kind, Is.EqualTo(CatActivityKind.TowelNest));
        Assert.That(nest.QuestType, Is.EqualTo(QuestType.Sleep));
        Assert.That(
            nest.StoreProductId, Is.EqualTo(HomeStoreService.BathroomTowelStorageId));
        Assert.That(nest.EnergyRestore, Is.GreaterThan(0f));

        // Like the vanity counter the niche is a perch the cat is lifted onto,
        // so the product box stays solid.
        BoxCollider box = prefab.GetComponentInChildren<BoxCollider>(true);
        Assert.That(box, Is.Not.Null);
        Assert.That(box.isTrigger, Is.True, "The catalogue box must not block open space.");
        Assert.That(prefab.GetComponentInChildren<MeshCollider>(true), Is.Not.Null,
            "The real furniture geometry must remain solid.");

        Transform nestPoint = prefab.transform.Find("NestPoint");
        Transform floor = prefab.transform.Find("NestFloorPoint");
        Assert.That(nestPoint, Is.Not.Null);
        Assert.That(floor, Is.Not.Null);
        Assert.That(nestPoint.localPosition.y, Is.GreaterThan(0.7f),
            "The nest must sit on the towel stack, not on the floor.");
        Assert.That(floor.localPosition.y, Is.EqualTo(0f));
        // The room side is root -Z: this product is absent from `facesBackward`,
        // so the authored front at -Z is still the front.
        Assert.That(floor.localPosition.z, Is.LessThan(nestPoint.localPosition.z),
            "The cat must start in front of the niche and hop back into it.");
        Assert.That(nestPoint.localPosition.y / ProductScale(prefab), Is.EqualTo(1.7472f).Within(.01f),
            "The enclosed towel bay has too little headroom for the large breeds.");
        Assert.That(nestPoint.GetComponent<CatActivitySurface>().AlignAlongSurface, Is.True);

        Assert.That(nest.IsUnlocked, Is.False, "The nap waits for the store purchase.");
        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.BathroomTowelStorageId, out var product),
            Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(
            HomeStoreService.TryPurchase(HomeStoreService.BathroomTowelStorageId).Succeeded,
            Is.True);
        nest.RefreshUnlockPresentation();
        Assert.That(nest.IsUnlocked, Is.True);
    }

    /// <summary>
    /// One table-driven pass over the five props that closed out the Bathroom.
    /// Every one of them is absent from `facesBackward`, so an authored point
    /// (x, y, z) lands at (-x, y, z): the assertions below lock that mapping in,
    /// because getting it wrong puts the cat on the wrong side of the product
    /// and nothing else in the build would catch it.
    /// </summary>
    private static GameObject LoadStoreProduct(string prefabName)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/StoreProducts/Prefabs/" + prefabName + ".prefab");
        Assert.That(prefab, Is.Not.Null, prefabName + " prefab is missing.");
        return prefab;
    }

    private static void AssertUnlocksWithPurchase(CatActivity activity, string productId)
    {
        GameObject copy = null;
        if (UnityEditor.EditorUtility.IsPersistent(activity))
        {
            // Refresh changes activeSelf. Never apply it to an imported prefab:
            // Unity can use that mutated asset for later scene loads in this run.
            copy = Object.Instantiate(activity.gameObject);
            activity = copy.GetComponent<CatActivity>();
        }
        try
        {
        Assert.That(activity.StoreProductId, Is.EqualTo(productId));
        Assert.That(activity.IsUnlocked, Is.False, "The activity waits for the purchase.");
        Assert.That(HomeStoreService.TryGetProduct(productId, out var product), Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(HomeStoreService.TryPurchase(productId).Succeeded, Is.True);
        activity.RefreshUnlockPresentation();
        Assert.That(activity.IsUnlocked, Is.True);
        }
        finally { if (copy != null) Object.DestroyImmediate(copy); }
    }

    [Test]
    public void LitterDig_WaitsForTheTrayPurchaseAndOpensTheBoxAsATrigger()
    {
        var prefab = LoadStoreProduct("BathroomLitterBox");
        LitterDigActivity dig = prefab.GetComponentInChildren<LitterDigActivity>(true);
        Assert.That(dig, Is.Not.Null, "The tray must carry its dig activity.");
        Assert.That(dig.Kind, Is.EqualTo(CatActivityKind.LitterDig));
        Assert.That(dig.QuestType, Is.EqualTo(QuestType.LitterDig));

        // The 0.15 sill is above the controller's step offset, so unlike the
        // vanity this product has to be walk-in.
        BoxCollider box = prefab.GetComponentInChildren<BoxCollider>(true);
        Assert.That(box, Is.Not.Null);
        Assert.That(box.isTrigger, Is.True, "A litter tray is entered, not bumped into.");

        Transform mouth = prefab.transform.Find("DigMouthPoint");
        Transform inside = prefab.transform.Find("DigPoint");
        Assert.That(mouth, Is.Not.Null);
        Assert.That(inside, Is.Not.Null);
        Assert.That(inside.localPosition.y, Is.GreaterThan(0.1f),
            "The dig point sits on the litter, not on the floor.");
        Assert.That(mouth.localPosition.z, Is.LessThan(inside.localPosition.z),
            "The room side is root -Z, so the cat comes in from there.");

        AssertUnlocksWithPurchase(dig, HomeStoreService.BathroomLitterBoxId);
    }

    [Test]
    public void GroomBrush_WaitsForTheCartPurchaseAndRubsAlongTheRoller()
    {
        var prefab = LoadStoreProduct("BathroomGroomingCart");
        GroomBrushActivity groom = prefab.GetComponentInChildren<GroomBrushActivity>(true);
        Assert.That(groom, Is.Not.Null, "The cart must carry its groom activity.");
        Assert.That(groom.Kind, Is.EqualTo(CatActivityKind.GroomBrush));
        Assert.That(groom.QuestType, Is.EqualTo(QuestType.Groom));

        // Nothing on the cart moves, so the box stays solid.
        BoxCollider box = prefab.GetComponentInChildren<BoxCollider>(true);
        Assert.That(box, Is.Not.Null);
        Assert.That(box.isTrigger, Is.True, "The catalogue box must not block open space.");
        Assert.That(prefab.GetComponentInChildren<MeshCollider>(true), Is.Not.Null,
            "The real furniture geometry must remain solid.");

        Transform start = prefab.transform.Find("GroomRubStartPoint");
        Transform end = prefab.transform.Find("GroomRubEndPoint");
        Transform approach = prefab.transform.Find("GroomApproachPoint");
        Assert.That(start, Is.Not.Null);
        Assert.That(end, Is.Not.Null);
        Assert.That(approach, Is.Not.Null);
        Assert.That(start.localPosition.z, Is.EqualTo(end.localPosition.z).Within(0.001f),
            "The rub line runs along the roller, which is the X axis.");
        Assert.That(start.localPosition.x, Is.Not.EqualTo(end.localPosition.x));
        Assert.That(start.localPosition.z, Is.LessThan(0f),
            "The roller is on the front face, which is root -Z.");
        Assert.That(approach.localPosition.z, Is.LessThan(start.localPosition.z));

        AssertUnlocksWithPurchase(groom, HomeStoreService.BathroomGroomingCartId);
    }

    [Test]
    public void HamperDive_WaitsForTheHamperPurchaseAndDropsTheCatIntoThePile()
    {
        var prefab = LoadStoreProduct("BathroomLaundryHamper");
        HamperDiveActivity dive = prefab.GetComponentInChildren<HamperDiveActivity>(true);
        Assert.That(dive, Is.Not.Null, "The hamper must carry its dive activity.");
        Assert.That(dive.Kind, Is.EqualTo(CatActivityKind.HamperDive));
        Assert.That(dive.QuestType, Is.EqualTo(QuestType.HamperDive));
        Assert.That(dive.EnergyRestore, Is.GreaterThan(0f));

        Transform floor = prefab.transform.Find("DiveFloorPoint");
        Transform pile = prefab.transform.Find("DivePilePoint");
        Assert.That(floor, Is.Not.Null);
        Assert.That(pile, Is.Not.Null);
        Assert.That(pile.localPosition.y, Is.GreaterThan(0.7f),
            "The pile point sits on the laundry, over the rim.");
        Assert.That(dive.SinkDepth, Is.LessThan(pile.localPosition.y),
            "Sinking must not push the cat through the floor of the basket.");
        Assert.That(floor.localPosition.z, Is.LessThan(pile.localPosition.z));

        AssertUnlocksWithPurchase(dive, HomeStoreService.BathroomLaundryHamperId);
    }

    [Test]
    public void BathroomMirror_IsDecorationOnly()
    {
        var prefab = LoadStoreProduct("BathroomWallMirror");
        Assert.That(prefab.GetComponentInChildren<CatActivity>(true), Is.Null);
        Assert.That(prefab.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(0));
    }

    [Test]
    public void MatKnead_WaitsForTheMatPurchaseAndWorksTheRaisedPad()
    {
        var prefab = LoadStoreProduct("BathroomBathMat");
        MatKneadActivity knead = prefab.GetComponentInChildren<MatKneadActivity>(true);
        Assert.That(knead, Is.Not.Null, "The mat must carry its knead activity.");
        Assert.That(knead.Kind, Is.EqualTo(CatActivityKind.MatKnead));
        Assert.That(knead.QuestType, Is.EqualTo(QuestType.MatKnead));

        Transform pad = prefab.transform.Find("KneadPadPoint");
        Transform exit = prefab.transform.Find("KneadExitPoint");
        Assert.That(pad, Is.Not.Null);
        Assert.That(exit, Is.Not.Null);
        // The pad is authored at +X on a product that is not in `facesBackward`,
        // so the FBX import's X mirror has to leave it on -X.
        Assert.That(pad.localPosition.x, Is.LessThan(0f),
            "The knead pad is authored at +X and lands at -X.");
        Assert.That(exit.localPosition.x, Is.EqualTo(pad.localPosition.x).Within(0.001f));
        Assert.That(exit.localPosition.z, Is.LessThan(pad.localPosition.z),
            "The cat steps off towards the room, which is root -Z.");
        Assert.That(pad.localPosition.y, Is.GreaterThan(0f).And.LessThan(0.12f),
            "A bath mat is 0.08 tall; the pad point must sit on it, not above it.");

        AssertUnlocksWithPurchase(knead, HomeStoreService.BathroomBathMatId);
    }

    [Test]
    public void TubEdgeWalk_WaitsForTheTubPurchaseAndPutsTheCatOnTheLowRim()
    {
        var prefab = LoadStoreProduct("BathroomTub");
        TubEdgeWalkActivity edge = prefab.GetComponentInChildren<TubEdgeWalkActivity>(true);
        Assert.That(edge, Is.Not.Null, "The tub must carry its rim walk activity.");
        Assert.That(edge.Kind, Is.EqualTo(CatActivityKind.TubEdgeWalk));
        Assert.That(edge.QuestType, Is.EqualTo(QuestType.TubEdgeWalk));

        Transform rimStart = prefab.transform.Find("EdgeRimStartPoint");
        Transform rimEnd = prefab.transform.Find("EdgeRimEndPoint");
        Transform water = prefab.transform.Find("EdgeWaterPoint");
        Transform floor = prefab.transform.Find("EdgeFloorPoint");
        Assert.That(rimStart, Is.Not.Null);
        Assert.That(rimEnd, Is.Not.Null);
        Assert.That(water, Is.Not.Null);
        Assert.That(floor, Is.Not.Null);

        // The tub is front-to-back asymmetric: the tall backrest is at root -Z
        // and the walkable rim is at root +Z. Putting the cat on the wrong one
        // faces it into the wall, and only these assertions catch that.
        Assert.That(rimStart.localPosition.z, Is.GreaterThan(0f),
            "The walkable rim is at root +Z, not on the backrest.");
        Assert.That(rimEnd.localPosition.z,
            Is.EqualTo(rimStart.localPosition.z).Within(0.001f));
        Assert.That(floor.localPosition.z, Is.GreaterThan(rimStart.localPosition.z),
            "The cat approaches from outside the rim.");
        Assert.That(rimStart.localPosition.y, Is.GreaterThan(water.localPosition.y),
            "The rim stands above the water it is dipped into.");
        Assert.That(rimStart.localPosition.y, Is.LessThan(1.0f),
            "The rim is the low edge at 0.744, not the 1.08 backrest.");
        Assert.That(rimStart.localPosition.x, Is.LessThan(rimEnd.localPosition.x));

        AssertUnlocksWithPurchase(edge, HomeStoreService.BathroomTubId);
    }

    /// <summary>
    /// Finds the activity on a prefab by KIND, because most of the reauthored
    /// rooms build several routines out of the same shared classes and
    /// GetComponent&lt;T&gt; would return whichever one happens to be first.
    /// </summary>
    private static CatActivity LoadRoomActivity(string prefabName, CatActivityKind kind)
    {
        var prefab = LoadStoreProduct(prefabName);
        foreach (CatActivity candidate in prefab.GetComponentsInChildren<CatActivity>(true))
        {
            if (candidate.Kind == kind)
                return candidate;
        }

        Assert.Fail(prefabName + " is missing a " + kind + " activity.");
        return null;
    }

    [Test]
    public void Kitchen_EveryProductCarriesItsOwnRoutine()
    {
        // The whole room in one table: ten products, ten kinds, ten gates. If a
        // reauthored model ever loses its routine this is the test that says so.
        var expected = new (string Prefab, CatActivityKind Kind, string ProductId, QuestType Quest)[]
        {
            ("KitchenIsland", CatActivityKind.IslandPerch,
             HomeStoreService.KitchenIslandId, QuestType.Sleep),
            ("KitchenCounterStool", CatActivityKind.StoolPerch,
             HomeStoreService.KitchenCounterStoolId, QuestType.Sleep),
            ("KitchenRefrigerator", CatActivityKind.FridgeStare,
             HomeStoreService.KitchenRefrigeratorId, QuestType.KitchenWatch),
            ("KitchenFruitBasket", CatActivityKind.FruitSwat,
             HomeStoreService.KitchenFruitBasketId, QuestType.KitchenWatch),
            ("KitchenPantryShelf", CatActivityKind.PantryClimb,
             HomeStoreService.KitchenPantryShelfId, QuestType.PantryClimb),
            ("KitchenStoveOven", CatActivityKind.OvenWarmth,
             HomeStoreService.KitchenStoveOvenId, QuestType.Sleep),
            ("KitchenDishCart", CatActivityKind.CartNudge,
             HomeStoreService.KitchenDishCartId, QuestType.CartNudge),
            ("KitchenFeedingStation", CatActivityKind.MealTime,
             HomeStoreService.KitchenFeedingStationId, QuestType.Eat),
            ("KitchenSinkCabinet", CatActivityKind.KitchenSip,
             HomeStoreService.KitchenSinkCabinetId, QuestType.Drink),
            ("KitchenPawMat", CatActivityKind.KitchenMatKnead,
             HomeStoreService.KitchenPawMatId, QuestType.MatKnead),
        };

        foreach (var row in expected)
        {
            CatActivity activity = LoadRoomActivity(row.Prefab, row.Kind);
            Assert.That(activity.QuestType, Is.EqualTo(row.Quest), row.Prefab);
            Assert.That(activity.StoreProductId, Is.EqualTo(row.ProductId), row.Prefab);
            Assert.That(activity.IsUnlocked, Is.False, row.Prefab + " waits for the purchase.");
        }
    }

    [Test]
    public void IslandPerch_PutsTheCatOnTheClearStretchOfCounter()
    {
        var perch = (PerchNapActivity)LoadRoomActivity(
            "KitchenIsland", CatActivityKind.IslandPerch);
        var prefab = LoadStoreProduct("KitchenIsland");

        Transform point = prefab.transform.Find("PerchPoint");
        Transform floor = prefab.transform.Find("PerchFloorPoint");
        Assert.That(point, Is.Not.Null);
        Assert.That(floor, Is.Not.Null);
        Assert.That(point.localPosition.y, Is.GreaterThan(0.7f),
            "The perch is the counter, not the plinth.");
        // The clear stretch is authored at +X over the basket bay, so the FBX
        // import's X mirror has to land it at -X.
        Assert.That(point.localPosition.x, Is.LessThan(0f),
            "The clear counter is authored at +X and lands at -X.");
        Assert.That(floor.localPosition.z, Is.LessThan(point.localPosition.z),
            "The cat approaches from the room, which is root -Z.");
        Assert.That(perch.EnergyRestore, Is.GreaterThan(0f));

        AssertUnlocksWithPurchase(perch, HomeStoreService.KitchenIslandId);
    }

    [Test]
    public void MealTime_EatsFromTheFoodBowlAndNotTheWaterOne()
    {
        var meal = (MealTimeActivity)LoadRoomActivity(
            "KitchenFeedingStation", CatActivityKind.MealTime);
        var prefab = LoadStoreProduct("KitchenFeedingStation");

        Transform bowl = prefab.transform.Find("MealBowlPoint");
        Transform stand = prefab.transform.Find("MealStandPoint");
        Assert.That(bowl, Is.Not.Null);
        Assert.That(stand, Is.Not.Null);
        // The food bowl is authored at -X and the water bowl at +X, so the
        // mirror has to put the meal point on +X. Getting this backwards would
        // have the cat eating out of the water dish.
        Assert.That(bowl.localPosition.x, Is.GreaterThan(0f),
            "The food bowl is authored at -X and lands at +X.");
        Assert.That(bowl.localPosition.y, Is.GreaterThan(0.15f),
            "The bowl rim sits on the raised stand.");
        // The accepted side approach keeps the body on the floor. The mouth,
        // rather than the root, reaches the food point during the source clip.
        Assert.That(stand.localPosition.y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(Vector2.Distance(new Vector2(stand.localPosition.x, stand.localPosition.z),
            new Vector2(bowl.localPosition.x, bowl.localPosition.z)), Is.InRange(0.5f, 0.9f));
        Assert.That(meal.HungerRestore, Is.GreaterThan(0f));

        AssertUnlocksWithPurchase(meal, HomeStoreService.KitchenFeedingStationId);
    }

    [Test]
    public void PantryClimb_UsesTheClearTopInsteadOfOccupiedShelves()
    {
        var climb = (PantryClimbActivity)LoadRoomActivity(
            "KitchenPantryShelf", CatActivityKind.PantryClimb);
        var prefab = LoadStoreProduct("KitchenPantryShelf");

        Transform floor = prefab.transform.Find("ClimbFloorPoint");
        Transform lower = prefab.transform.Find("ClimbLowerPoint");
        Transform upper = prefab.transform.Find("ClimbUpperPoint");
        Assert.That(floor, Is.Not.Null);
        Assert.That(lower, Is.Not.Null);
        Assert.That(upper, Is.Not.Null);
        Assert.That(floor.localPosition.y, Is.EqualTo(0f));
        Assert.That(lower.localPosition.y, Is.GreaterThan(0.3f));
        Assert.That(upper.localPosition.y, Is.GreaterThan(lower.localPosition.y),
            "The climb has to go up, one shelf at a time.");
        Assert.That(climb.UsesIntermediatePerch, Is.False, "The old intermediate board is filled with jars.");
        Assert.That(upper.localPosition.y / ProductScale(prefab), Is.EqualTo(1.7696f).Within(.01f));

        AssertUnlocksWithPurchase(climb, HomeStoreService.KitchenPantryShelfId);
    }

    [Test]
    public void CartNudge_RollsTheVisualAndKnowsWhereHomeIs()
    {
        var nudge = (CartNudgeActivity)LoadRoomActivity(
            "KitchenDishCart", CatActivityKind.CartNudge);
        var prefab = LoadStoreProduct("KitchenDishCart");

        // The cart is the only Kitchen product that moves, and it moves as one
        // piece — so the activity drives VisualContent itself rather than a
        // pivot with a second FBX under it.
        Assert.That(nudge.CartVisual, Is.Not.Null);
        Assert.That(nudge.CartVisual.name, Is.EqualTo("VisualContent"));
        Assert.That(nudge.CartVisual.GetComponentInChildren<Renderer>(true), Is.Not.Null);
        Assert.That(nudge.RollDistance, Is.GreaterThan(0f).And.LessThan(0.6f),
            "A shove has to be visible without walking the cart across the room.");
        Assert.That(prefab.transform.Find("NudgeShovePoint"), Is.Not.Null);

        AssertUnlocksWithPurchase(nudge, HomeStoreService.KitchenDishCartId);
    }

    [Test]
    public void KitchenMatKnead_WorksTheRunnersRaisedPad()
    {
        var knead = (MatKneadActivity)LoadRoomActivity(
            "KitchenPawMat", CatActivityKind.KitchenMatKnead);
        var prefab = LoadStoreProduct("KitchenPawMat");

        Transform pad = prefab.transform.Find("KneadPadPoint");
        Transform exit = prefab.transform.Find("KneadExitPoint");
        Assert.That(pad, Is.Not.Null);
        Assert.That(exit, Is.Not.Null);
        Assert.That(pad.localPosition.x, Is.LessThan(0f),
            "The pad is authored at +X and lands at -X.");
        Assert.That(pad.localPosition.y, Is.GreaterThan(0f).And.LessThan(0.12f),
            "A runner is 0.08 tall; the pad point sits on it.");
        Assert.That(exit.localPosition.z, Is.LessThan(pad.localPosition.z));

        AssertUnlocksWithPurchase(knead, HomeStoreService.KitchenPawMatId);
    }

    [Test]
    public void Bedroom_EightRoutinesAndTwoDecorations()
    {
        // The whole room in one table: ten products, ten kinds, ten gates. Nine
        // of the ten routines come from six shared classes, so the table is the
        // only place the pairing is written down in full.
        var expected = new (string Prefab, CatActivityKind Kind, string ProductId, QuestType Quest)[]
        {
            ("BedroomQueenBed", CatActivityKind.BedNap,
             HomeStoreService.BedroomQueenBedId, QuestType.Sleep),
            ("BedroomWardrobe", CatActivityKind.WardrobeScratch,
             HomeStoreService.BedroomWardrobeId, QuestType.Scratch),
            ("BedroomWindowDaybed", CatActivityKind.DaybedWatch,
             HomeStoreService.BedroomWindowDaybedId, QuestType.BedroomWatch),
            ("BedroomNightstand", CatActivityKind.KnockOff,
             HomeStoreService.BedroomNightstandId, QuestType.KnockOff),
            ("BedroomVanityStool", CatActivityKind.VanityStoolNap,
             HomeStoreService.BedroomVanityStoolId, QuestType.Sleep),
            ("BedroomYarnBasket", CatActivityKind.YarnSwat,
             HomeStoreService.BedroomYarnBasketId, QuestType.PlayBall),
            ("BedroomPawRug", CatActivityKind.BedroomMatKnead,
             HomeStoreService.BedroomPawRugId, QuestType.MatKnead),
            ("BedroomStarCanopy", CatActivityKind.CanopyNap,
             HomeStoreService.BedroomStarCanopyId, QuestType.Sleep),
        };

        foreach (var row in expected)
        {
            CatActivity activity = LoadRoomActivity(row.Prefab, row.Kind);
            Assert.That(activity.QuestType, Is.EqualTo(row.Quest), row.Prefab);
            Assert.That(activity.StoreProductId, Is.EqualTo(row.ProductId), row.Prefab);
            Assert.That(activity.IsUnlocked, Is.False, row.Prefab + " waits for the purchase.");
        }
        foreach (string name in new[] { "BedroomNightLight", "BedroomDreamArt" })
            Assert.That(LoadStoreProduct(name).GetComponent<CatActivity>(), Is.Null, name + " decoration");
    }

    /// <summary>
    /// The one rule no other room needed, and the one this room got wrong the
    /// first time: the Bedroom carries both orientation mappings at once. Its
    /// floor products are absent from `facesBackward` and put the room at root
    /// -Z; its four wall products are in it and put the room at root +Z. Every
    /// anchor below is a cat walking in from the room. Flip a sign here and the
    /// cat approaches through the wall instead, and nothing else would catch it.
    /// </summary>
    [Test]
    public void Bedroom_ApproachesEveryProductFromTheRoomAndNotTheWall()
    {
        var wallProducts = new[]
        {
            "BedroomWardrobe", "BedroomWindowDaybed",
            "BedroomNightstand", "BedroomDreamArt",
        };

        foreach (string prefabName in wallProducts)
        {
            Transform anchor = LoadStoreProduct(prefabName).transform.Find("InteractionAnchor");
            Assert.That(anchor, Is.Not.Null, prefabName + " has no InteractionAnchor.");
            Assert.That(anchor.localPosition.z, Is.GreaterThan(0.5f),
                prefabName + " is a wall product: the room is at root +Z.");
        }

        var floorProducts = new[]
        {
            "BedroomQueenBed", "BedroomVanityStool", "BedroomYarnBasket",
            "BedroomNightLight", "BedroomPawRug", "BedroomStarCanopy",
        };

        foreach (string prefabName in floorProducts)
        {
            Transform anchor = LoadStoreProduct(prefabName).transform.Find("InteractionAnchor");
            Assert.That(anchor, Is.Not.Null, prefabName + " has no InteractionAnchor.");
            Assert.That(anchor.localPosition.z / ProductScale(LoadStoreProduct(prefabName)), Is.LessThan(-0.5f),
                prefabName + " is a floor product: the room is at root -Z.");
        }
    }

    [Test]
    public void BedNap_TakesTheClearHalfAndLeavesThePillows()
    {
        var perch = (PerchNapActivity)LoadRoomActivity(
            "BedroomQueenBed", CatActivityKind.BedNap);
        var prefab = LoadStoreProduct("BedroomQueenBed");

        Transform point = prefab.transform.Find("PerchPoint");
        Transform floor = prefab.transform.Find("PerchFloorPoint");
        Assert.That(point, Is.Not.Null);
        Assert.That(floor, Is.Not.Null);
        Assert.That(point.localPosition.y, Is.GreaterThan(0.4f),
            "The nap is on the mattress, not on the floor beside it.");
        // The pillows land at +X in root space, so the clear half is -X. Getting
        // this backwards sleeps the cat on top of the pillows.
        Assert.That(point.localPosition.x, Is.LessThan(0f),
            "The pillows land at +X; the clear half is -X.");
        Assert.That(floor.localPosition.z, Is.LessThan(point.localPosition.z),
            "The cat climbs up from the room, which is root -Z.");
        Assert.That(perch.EnergyRestore, Is.GreaterThan(0f));

        AssertUnlocksWithPurchase(perch, HomeStoreService.BedroomQueenBedId);
    }

    [Test]
    public void KnockOff_ReachesOverTheNightstandAndPushesTheGlass()
    {
        var knock = (KnockOffActivity)LoadRoomActivity(
            "BedroomNightstand", CatActivityKind.KnockOff);
        var prefab = LoadStoreProduct("BedroomNightstand");

        Transform reach = prefab.transform.Find("KnockReachPoint");
        Transform anchor = prefab.transform.Find("InteractionAnchor");
        Assert.That(reach, Is.Not.Null);
        Assert.That(anchor, Is.Not.Null);

        // The glass is its own FBX under a pivot the routine drives, the way the
        // toilet paper roll is. Without the pivot there is nothing to knock.
        Assert.That(knock.GlassPivot, Is.Not.Null);
        Assert.That(knock.GlassPivot.name, Is.EqualTo("GlassPivot"));
        Assert.That(knock.GlassPivot.GetComponentInChildren<Renderer>(true), Is.Not.Null);
        Assert.That(knock.GlassPivot.localPosition.y, Is.GreaterThan(0.4f),
            "The glass starts on the top surface, not on the floor.");
        Assert.That(knock.TeaseCount, Is.GreaterThanOrEqualTo(2),
            "Two exploratory taps before the one that lands.");
        Assert.That(knock.FallHeight, Is.GreaterThan(0.05f).And.LessThan(1f));

        // A wall product: the cat stands at root +Z and reaches back over the
        // top, so the reach point is nearer the nightstand than the anchor is.
        Assert.That(reach.localPosition.z, Is.LessThan(anchor.localPosition.z));
        Assert.That(reach.localPosition.z, Is.GreaterThan(0f));

        AssertUnlocksWithPurchase(knock, HomeStoreService.BedroomNightstandId);
    }

    [Test]
    public void BedroomMatKnead_WorksTheRugsRaisedPad()
    {
        var knead = (MatKneadActivity)LoadRoomActivity(
            "BedroomPawRug", CatActivityKind.BedroomMatKnead);
        var prefab = LoadStoreProduct("BedroomPawRug");

        Transform pad = prefab.transform.Find("KneadPadPoint");
        Transform exit = prefab.transform.Find("KneadExitPoint");
        Assert.That(pad, Is.Not.Null);
        Assert.That(exit, Is.Not.Null);
        Assert.That(pad.localPosition.x, Is.LessThan(0f),
            "The raised pad is authored at +X and lands at -X.");
        Assert.That(pad.localPosition.y, Is.GreaterThan(0f).And.LessThan(0.12f),
            "The rug is 0.09 tall; the pad point sits on it.");
        Assert.That(exit.localPosition.z, Is.LessThan(pad.localPosition.z),
            "The cat steps off towards the room, which is root -Z.");
        Assert.That(knead.EnergyRestore, Is.GreaterThan(0f));

        AssertUnlocksWithPurchase(knead, HomeStoreService.BedroomPawRugId);
    }

    [Test]
    public void DaybedNap_UsesRealCushionAndKeepsPurchaseAndQuestIdentity()
    {
        var nap = (PerchNapActivity)LoadRoomActivity("BedroomWindowDaybed", CatActivityKind.DaybedWatch);
        Assert.That(nap.PerchPoint, Is.Not.Null);
        Assert.That(nap.PerchPoint.localPosition.y, Is.InRange(.49f,.53f));
        Assert.That(nap.PerchPoint.GetComponent<CatActivitySurface>().ResolvePose(CatActivityPose.Sit), Is.EqualTo(CatActivityPose.Sleep));
        Assert.That(nap.SupportsContinuousRest, Is.True);
        Assert.That(nap.FloorPoint.localPosition.z, Is.GreaterThan(.7f));
        AssertUnlocksWithPurchase(nap, HomeStoreService.BedroomWindowDaybedId);
    }

    [Test]
    public void PergolaClimb_LandsAboveTheCanopyFromTheCourtyard()
    {
        var climb = (PantryClimbActivity)LoadRoomActivity(
            "GardenPergola", CatActivityKind.PergolaClimb);
        var prefab = LoadStoreProduct("GardenPergola");

        Transform floor = prefab.transform.Find("ClimbFloorPoint");
        Transform lower = prefab.transform.Find("ClimbLowerPoint");
        Transform upper = prefab.transform.Find("ClimbUpperPoint");
        Assert.That(floor, Is.Not.Null);
        Assert.That(lower, Is.Not.Null);
        Assert.That(upper, Is.Not.Null);
        Assert.That(floor.localPosition.y, Is.EqualTo(0f));
        Assert.That(lower.localPosition.y, Is.GreaterThan(0.3f),
            "The bench is the first hop, not a doorstep.");
        Assert.That(upper.localPosition.y, Is.GreaterThan(lower.localPosition.y),
            "The climb has to go up, bench then rail.");
        Assert.That(climb.UsesIntermediatePerch, Is.False, "The rail below the canopy has no headroom.");
        Assert.That(upper.localPosition.y / ProductScale(prefab), Is.GreaterThan(1.69f));
        // The bench stands against the back lattice, so every point of the climb
        // is at root +Z and the cat starts inside the shelter. The pergola is a
        // yaw 0 floor product and absent from `facesBackward`, so Z is not
        // mirrored and these signs are the authored ones.
        Assert.That(floor.localPosition.z, Is.LessThan(-1f), "Take off outside the roof, never through it.");
        Assert.That(prefab.transform.Find("InteractionAnchor").localPosition.z,
            Is.LessThan(-0.75f),
            "The cat still walks in from the courtyard, which is root -Z.");

        AssertUnlocksWithPurchase(climb, HomeStoreService.GardenPergolaId);
    }

    [Test]
    public void TreeScratch_StandsTheCatAtTheRibbedBandAndNotInThePlanter()
    {
        var scratch = (ScratchPostActivity)LoadRoomActivity(
            "GardenSapling", CatActivityKind.TreeScratch);
        var prefab = LoadStoreProduct("GardenSapling");

        Transform point = prefab.transform.Find("ScratchPoint");
        Transform anchor = prefab.transform.Find("InteractionAnchor");
        Assert.That(point, Is.Not.Null);
        Assert.That(anchor, Is.Not.Null);
        Assert.That(point.localPosition.y, Is.EqualTo(0f),
            "The cat scratches standing on the lawn.");
        // Yaw 0 floor product, absent from `facesBackward`: the courtyard is at
        // root -Z, and the point has to clear the 0.41 planter or the cat works
        // basket weave instead of bark.
        Assert.That(point.localPosition.z / ProductScale(prefab), Is.LessThan(-0.42f),
            "The scratch point stands clear of the planter rim.");
        Assert.That(anchor.localPosition.z, Is.LessThan(point.localPosition.z),
            "The cat walks in from further out than it scratches.");

        AssertUnlocksWithPurchase(scratch, HomeStoreService.GardenSaplingId);
    }

    [Test]
    public void BistroPerch_PutsTheCatOnTheTableAndNotOnTheCup()
    {
        var perch = (PerchNapActivity)LoadRoomActivity(
            "GardenBistroSet", CatActivityKind.BistroPerch);
        var prefab = LoadStoreProduct("GardenBistroSet");

        Transform point = prefab.transform.Find("PerchPoint");
        Transform floor = prefab.transform.Find("PerchFloorPoint");
        Assert.That(point, Is.Not.Null);
        Assert.That(floor, Is.Not.Null);
        Assert.That(point.localPosition.y, Is.GreaterThan(0.55f),
            "The perch is the table top, not a chair seat at 0.40.");
        // The mesh has to stay centred in Z or FitFixtureModel re-centres it and
        // the authored table position no longer matches the point: the first
        // pass put the table at +0.180, the fit shifted the model -0.137, and
        // the perch landed behind the table in mid-air.
        Transform model = prefab.transform.Find("VisualContent")
            .GetChild(0);
        Assert.That(Mathf.Abs(model.localPosition.z), Is.LessThan(0.05f),
            "The bistro mesh must be authored centred in Z.");
        Assert.That(Mathf.Abs(point.localPosition.z), Is.LessThan(0.12f),
            "The perch sits on the table, which stands near the middle.");
        Assert.That(floor.localPosition.z, Is.LessThan(point.localPosition.z),
            "The cat jumps up from the courtyard, which is root -Z.");
        Assert.That(perch.EnergyRestore, Is.GreaterThan(0f));

        AssertUnlocksWithPurchase(perch, HomeStoreService.GardenBistroSetId);
    }

    [Test]
    public void HammockSway_HangsTheBedUnderAPivotAndIsBoardedFromTheCourtyard()
    {
        var sway = (SwingRideActivity)LoadRoomActivity(
            "GardenHammock", CatActivityKind.HammockSway);
        var prefab = LoadStoreProduct("GardenHammock");

        // The bed is a second FBX hung under a pivot, the way the porch swing's
        // bench is. Without the pivot there is nothing to swing.
        Assert.That(sway.SwingPivot, Is.Not.Null);
        Assert.That(sway.SwingPivot.name, Is.EqualTo("SwingPivot"));
        Assert.That(sway.SwingPivot.localPosition.y, Is.GreaterThan(0.5f),
            "The pivot is the hanging line between the two frame rings.");
        Transform bed = sway.SwingPivot.Find("HammockBed");
        Assert.That(bed, Is.Not.Null, "The hammock bed must hang under the pivot.");
        Assert.That(bed.GetComponentInChildren<Renderer>(true), Is.Not.Null);

        // GardenHammock is the first Garden product in `facesBackward`: yaw 90
        // would otherwise point its front at the fence, so Z flips and the
        // courtyard is at root +Z. Both approach points are positive, which is
        // the opposite of every other Garden product so far.
        Transform mount = prefab.transform.Find("SwingMountPoint");
        Transform anchor = prefab.transform.Find("InteractionAnchor");
        Assert.That(mount, Is.Not.Null);
        Assert.That(anchor, Is.Not.Null);
        Assert.That(mount.localPosition.z, Is.GreaterThan(0.4f),
            "The cat boards from root +Z, because the hammock faces backward.");
        Assert.That(anchor.localPosition.z, Is.GreaterThan(mount.localPosition.z),
            "The cat walks in from further out than it boards.");

        // A product the cat is carried by turns its box into a trigger, the way
        // the tunnel and the porch swing do.
        var collider = prefab.transform.Find("VisualContent")
            .GetComponent<BoxCollider>();
        Assert.That(collider, Is.Not.Null);
        Assert.That(collider.isTrigger, Is.True,
            "A ridden product must not block the cat with a solid box.");

        AssertUnlocksWithPurchase(sway, HomeStoreService.GardenHammockId);
    }

    [Test]
    public void SunBask_NestsInTheTowelAndApproachesFromTheCourtyard()
    {
        var bask = (TowelNestActivity)LoadRoomActivity(
            "GardenSunLounger", CatActivityKind.SunBask);
        var prefab = LoadStoreProduct("GardenSunLounger");

        Transform nest = prefab.transform.Find("NestPoint");
        Transform floor = prefab.transform.Find("NestFloorPoint");
        Assert.That(nest, Is.Not.Null);
        Assert.That(floor, Is.Not.Null);
        Assert.That(nest.localPosition.y, Is.GreaterThan(0.24f).And.LessThan(0.36f),
            "The nest is the towel on the deck, not the raised back at 0.48.");
        // The towel is authored at +X across the foot half and lands at -X.
        Assert.That(nest.localPosition.x, Is.LessThan(0f),
            "The towel is authored at +X and lands at -X.");
        Assert.That(floor.localPosition.x, Is.LessThan(-.9f),
            "The cat boards from the open foot end; the side lane stays free.");
        Assert.That(Mathf.Abs(floor.localPosition.z), Is.LessThan(.01f));
        Assert.That(bask.EnergyRestore, Is.GreaterThan(0f));

        AssertUnlocksWithPurchase(bask, HomeStoreService.GardenSunLoungerId);
    }

    [Test]
    public void GardenGrill_RemainsVisibleDecorationWithoutASecondWatchAction()
    {
        var prefab=LoadStoreProduct("GardenGrill");
        Assert.That(prefab.GetComponentsInChildren<CatActivity>(true),Is.Empty);
        Assert.That(prefab.GetComponentsInChildren<Renderer>(true).Length,Is.GreaterThan(0));
    }

    [Test]
    public void Garden_EveryInteractiveProductCarriesItsOwnRoutine()
    {
        // Nine interactive products; the grill remains decoration. Every
        // routine here is a reused class, so the table is the only place the
        // pairing is written down in full.
        var expected = new (string Prefab, CatActivityKind Kind, string ProductId, QuestType Quest)[]
        {
            ("GardenPergola", CatActivityKind.PergolaClimb,
             HomeStoreService.GardenPergolaId, QuestType.GardenClimb),
            ("GardenSapling", CatActivityKind.TreeScratch,
             HomeStoreService.GardenSaplingId, QuestType.Scratch),
            ("GardenBistroSet", CatActivityKind.BistroPerch,
             HomeStoreService.GardenBistroSetId, QuestType.Sleep),
            ("GardenHammock", CatActivityKind.HammockSway,
             HomeStoreService.GardenHammockId, QuestType.SwingRide),
            ("GardenSunLounger", CatActivityKind.SunBask,
             HomeStoreService.GardenSunLoungerId, QuestType.Sleep),
            ("GardenBirdBath", CatActivityKind.BirdBathSip,
             HomeStoreService.GardenBirdBathId, QuestType.Drink),
            ("GardenFlowerPots", CatActivityKind.PotDig,
             HomeStoreService.GardenFlowerPotsId, QuestType.LitterDig),
            ("GardenDaisyBed", CatActivityKind.DaisyRoll,
             HomeStoreService.GardenDaisyBedId, QuestType.MatKnead),
            ("GardenYarnBall", CatActivityKind.YarnBallChase,
             HomeStoreService.GardenYarnBallId, QuestType.PlayBall),
        };

        foreach (var row in expected)
        {
            CatActivity activity = LoadRoomActivity(row.Prefab, row.Kind);
            Assert.That(activity.QuestType, Is.EqualTo(row.Quest), row.Prefab);
            Assert.That(activity.StoreProductId, Is.EqualTo(row.ProductId), row.Prefab);
            Assert.That(activity.IsUnlocked, Is.False, row.Prefab + " waits for the purchase.");
        }
    }

    /// <summary>
    /// Garden has no wall products at all, so unlike the Bedroom the mapping is
    /// never decided by a wall — it is decided by where each product stands.
    /// Three of the ten carry a non-zero yaw and they do not agree: the lounger
    /// (yaw 90, x +2.55) faces the courtyard already, the hammock (yaw 90,
    /// x -2.75) does not, and the grill (yaw 270, x +2.65) does not either. The
    /// two that flip approach from root +Z and the other eight from root -Z.
    /// </summary>
    [Test]
    public void Garden_ApproachesEveryProductFromTheCourtyard()
    {
        var flipped = new[] { "GardenHammock", "GardenGrill" };
        foreach (string prefabName in flipped)
        {
            Transform anchor = LoadStoreProduct(prefabName).transform.Find("InteractionAnchor");
            Assert.That(anchor, Is.Not.Null, prefabName + " has no InteractionAnchor.");
            Assert.That(anchor.localPosition.z, Is.GreaterThan(0.5f),
                prefabName + " is in `facesBackward`: the courtyard is at root +Z.");
        }

        var plain = new[]
        {
            "GardenPergola", "GardenSapling", "GardenBistroSet", "GardenSunLounger",
            "GardenBirdBath", "GardenFlowerPots", "GardenDaisyBed", "GardenYarnBall",
        };
        foreach (string prefabName in plain)
        {
            Transform anchor = LoadStoreProduct(prefabName).transform.Find("InteractionAnchor");
            Assert.That(anchor, Is.Not.Null, prefabName + " has no InteractionAnchor.");
            Assert.That(anchor.localPosition.z, Is.LessThan(-0.4f),
                prefabName + " is not in `facesBackward`: the courtyard is at root -Z.");
        }
    }

    [Test]
    public void BirdBathSip_PerchesOnTheRimAboveTheWater()
    {
        var sip = (SinkSipActivity)LoadRoomActivity(
            "GardenBirdBath", CatActivityKind.BirdBathSip);
        var prefab = LoadStoreProduct("GardenBirdBath");

        Transform perch = prefab.transform.Find("SipPerchPoint");
        Transform floor = prefab.transform.Find("SipFloorPoint");
        Assert.That(perch, Is.Not.Null);
        Assert.That(floor, Is.Not.Null);
        Assert.That(perch.localPosition.y / ProductScale(prefab), Is.GreaterThan(0.5f),
            "The cat drinks from the rim, not from the base at 0.10.");
        // The supported root is between the paws, not itself on the near rim.
        // Actual paw/rim support is checked in the native jump/contact tests.
        Assert.That(floor.localPosition.y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(Vector2.Distance(new Vector2(floor.localPosition.x, floor.localPosition.z),
            new Vector2(perch.localPosition.x, perch.localPosition.z)), Is.GreaterThan(0.5f));
        Assert.That(floor.localPosition.z, Is.LessThan(perch.localPosition.z),
            "The cat climbs up from further out than it perches.");
        Assert.That(sip.ThirstRestore, Is.GreaterThan(0f));

        AssertUnlocksWithPurchase(sip, HomeStoreService.GardenBirdBathId);
    }

    [Test]
    public void YarnBallChase_DrivesABallThatIsItsOwnObject()
    {
        var chase = (GardenYarnChaseActivity)LoadRoomActivity(
            "GardenYarnBall", CatActivityKind.YarnBallChase);
        var prefab = LoadStoreProduct("GardenYarnBall");

        // The ball has to be a separate object the routine can move. Baked into
        // the nest mesh it could not hop, and the product would be a basket the
        // cat stares at.
        Assert.That(chase.YarnBall, Is.Not.Null);
        Assert.That(chase.YarnBall.name, Is.EqualTo("YarnBall"));
        Assert.That(chase.YarnBall.GetComponentInChildren<Renderer>(true), Is.Not.Null);
        Assert.That(chase.HopPoints.Length, Is.GreaterThanOrEqualTo(3),
            "A chase needs somewhere to chase to.");
        foreach (Transform hop in chase.HopPoints)
        {
            Assert.That(hop, Is.Not.Null);
            Assert.That(hop.localPosition.z, Is.LessThan(0f),
                "The ball hops out into the courtyard, which is root -Z.");
        }
        // The free lawn toy in the Garden shell uses the same class under
        // CatActivityKind.BallChase; this one must not collide with it.
        Assert.That(chase.Kind, Is.EqualTo(CatActivityKind.YarnBallChase));

        AssertUnlocksWithPurchase(chase, HomeStoreService.GardenYarnBallId);
    }

    [Test]
    public void BalconyAwning_RemainsVisibleDecoration()
    {
        var prefab = LoadStoreProduct("BalconySunAwning");
        Assert.That(prefab.GetComponent<CatActivity>(),Is.Null);
        Assert.That(prefab.GetComponentsInChildren<Renderer>(true).Length,Is.GreaterThan(0));
    }

    /// <summary>
    /// `facesBackward` puts a 180 on the MODEL CHILD, and nothing else in the
    /// prefab moves with it. The approach points are placed in root space by the
    /// builder, so a product that is wrongly in the list still has correct-looking
    /// anchors and only its mesh is turned around — which is exactly how four
    /// Garden products were silently added to the list and shipped facing the
    /// wrong way. Assert the rotation itself, not just the points.
    /// </summary>
    [Test]
    public void FacesBackward_TurnsOnlyTheProductsThatNeedIt()
    {
        var flipped = new[]
        {
            "GardenHammock", "GardenGrill", "BalconyHerbShelf", "BalconySunAwning",
            "BalconyRailingFlowers", "BalconyLanternString",
            "BedroomWardrobe", "BedroomWindowDaybed", "BedroomNightstand",
            "BedroomDreamArt",
            "PatioPergolaArch", "PatioHerbTrough", "PatioStringLights",
            "LoftWallGallery", "LoftTallBookcase",
        };
        foreach (string prefabName in flipped)
        {
            Transform model = FindPremiumModel(LoadStoreProduct(prefabName));
            Assert.That(model, Is.Not.Null, prefabName + " has no premium model child.");
            Assert.That(model.localRotation.eulerAngles.y, Is.EqualTo(180f).Within(0.5f),
                prefabName + " must be in `facesBackward`.");
        }

        var plain = new[]
        {
            "GardenPergola", "GardenSapling", "GardenBistroSet", "GardenSunLounger",
            "GardenBirdBath", "GardenFlowerPots", "GardenDaisyBed", "GardenYarnBall",
            "BedroomQueenBed", "BedroomVanityStool", "BedroomPawRug",
            "BalconyHangingChair", "BalconyCushionBench", "BalconySideTable",
            "BalconySunMat", "BalconyPlanterBox", "BalconyBirdFeeder",
            "KitchenIsland", "KitchenPantryShelf",
            "PatioStoneRug", "PatioPottedFerns", "PatioWaterFountain",
            "PatioFirePit", "PatioDiningSet", "PatioParasol",
            "LoftFloorRunner", "LoftFloorCushions", "LoftBookStack",
            "LoftArcLamp", "LoftBeanBag", "LoftRecordPlayer",
            "LoftStudyDesk", "LoftChaiseLounge",
        };
        foreach (string prefabName in plain)
        {
            Transform model = FindPremiumModel(LoadStoreProduct(prefabName));
            Assert.That(model, Is.Not.Null, prefabName + " has no premium model child.");
            Assert.That(model.localRotation, Is.EqualTo(Quaternion.identity)
                .Using<Quaternion>((a, b) => Quaternion.Angle(a, b) < 0.5f ? 0 : 1),
                prefabName + " must NOT be in `facesBackward`.");
        }
    }

    [Test]
    public void PatioStringLights_RemainVisibleDecoration()
    {
        var prefab=LoadStoreProduct("PatioStringLights");
        Assert.That(prefab.GetComponent<CatActivity>(),Is.Null);
        Assert.That(prefab.GetComponentsInChildren<Renderer>(true).Length,Is.GreaterThan(0));
    }

    [Test]
    public void Patio_EveryProductCarriesItsOwnRoutine()
    {
        // Patio v1 shipped one routine — the porch swing ride — across ten
        // products. The wave-3 decision says every product ships a real cat
        // interaction, and this is the table that says so for this room.
        var expected = new (string Prefab, CatActivityKind Kind, string ProductId, QuestType Quest)[]
        {
            ("PatioPergolaArch", CatActivityKind.ArchClimb,
             HomeStoreService.PatioPergolaArchId, QuestType.PatioClimb),
            ("PatioPorchSwing", CatActivityKind.SwingRide,
             HomeStoreService.PatioPorchSwingId, QuestType.SwingRide),
            ("PatioParasol", CatActivityKind.ParasolScratch,
             HomeStoreService.PatioParasolId, QuestType.Scratch),
            ("PatioDiningSet", CatActivityKind.DiningPerch,
             HomeStoreService.PatioDiningSetId, QuestType.Sleep),
            ("PatioFirePit", CatActivityKind.FirePitBask,
             HomeStoreService.PatioFirePitId, QuestType.Sleep),
            ("PatioWaterFountain", CatActivityKind.FountainSip,
             HomeStoreService.PatioWaterFountainId, QuestType.Drink),
            ("PatioPottedFerns", CatActivityKind.FernWatch,
             HomeStoreService.PatioPottedFernsId, QuestType.PatioWatch),
            ("PatioHerbTrough", CatActivityKind.HerbTroughDig,
             HomeStoreService.PatioHerbTroughId, QuestType.LitterDig),
            ("PatioStoneRug", CatActivityKind.StoneRugKnead,
             HomeStoreService.PatioStoneRugId, QuestType.MatKnead),
        };

        foreach (var row in expected)
        {
            CatActivity activity = LoadRoomActivity(row.Prefab, row.Kind);
            Assert.That(activity.QuestType, Is.EqualTo(row.Quest), row.Prefab);
            Assert.That(activity.StoreProductId, Is.EqualTo(row.ProductId), row.Prefab);
            Assert.That(activity.IsUnlocked, Is.False, row.Prefab + " waits for the purchase.");
        }
    }

    /// <summary>
    /// A rug is a surface, not a wall.
    ///
    /// The cat's CharacterController runs a 0.01 step offset on purpose, so it
    /// cannot stroll onto furniture that has no climb routine. That makes ANY
    /// solid collider over a centimetre an obstacle, and all six floor mats are
    /// 0.080 tall: every rug in the game was an eight-centimetre kerb the cat
    /// bounced off. They are triggers now. The height cut sits below
    /// `WalkingLeash` at 0.150, which stays solid.
    /// </summary>
    [Test]
    public void FloorMats_AreTriggersSoTheCatCanWalkOverThem()
    {
        foreach (string prefabName in new[] { "BathroomBathMat", "KitchenPawMat",
                                              "BedroomPawRug", "BalconySunMat",
                                              "PatioStoneRug", "LoftFloorRunner" })
        {
            GameObject prefab = LoadStoreProduct(prefabName);
            BoxCollider box = prefab.GetComponentInChildren<BoxCollider>(true);
            Assert.That(box, Is.Not.Null, prefabName + " has no product box.");
            Assert.That(box.isTrigger, Is.True,
                prefabName + " is a floor mat and must not block the cat.");
        }

        GameObject leash = LoadStoreProduct("WalkingLeash");
        BoxCollider leashBox = leash.GetComponentInChildren<BoxCollider>(true);
        Assert.That(leashBox, Is.Not.Null);
        Assert.That(leashBox.isTrigger, Is.True,
            "The former leash is now a playable ribbon mat; it must be walkable.");
    }

    /// <summary>
    /// Patio models skip `FitFixtureModel` exactly the way the Balcony's do, so
    /// an oversized mesh ships oversized and nothing rescues it. Six of ten
    /// Balcony models overshot on their first build; this locks the Patio ten.
    /// </summary>
    [Test]
    public void PatioProducts_AreAuthoredInsideTheirCatalogBox()
    {
        var boxes = new (string Prefab, float X, float Y, float Z)[]
        {
            ("PatioStoneRug", 2.00f, 0.08f, 1.20f),
            ("PatioPottedFerns", 0.80f, 1.10f, 0.60f),
            ("PatioHerbTrough", 1.60f, 0.50f, 0.35f),
            ("PatioStringLights", 1.40f, 2.10f, 0.30f),
            ("PatioWaterFountain", 0.80f, 0.90f, 0.80f),
            ("PatioFirePit", 0.90f, 0.80f, 0.90f),
            ("PatioDiningSet", 1.70f, 0.58f, 1.50f),
            ("PatioParasol", 2.20f, 1.85f, 2.20f),
            ("PatioPorchSwing", 1.60f, 1.50f, 0.70f),
            ("PatioPergolaArch", 2.40f, 1.85f, 1.00f),
        };

        foreach (var box in boxes)
        {
            GameObject prefab = LoadStoreProduct(box.Prefab);
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0), box.Prefab);
            Bounds bounds = new Bounds(
                renderers[0].transform.TransformPoint(renderers[0].localBounds.center),
                Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                Bounds local = renderer.localBounds;
                Vector3 centre = renderer.transform.TransformPoint(local.center);
                Vector3 extents = renderer.transform.TransformVector(local.extents);
                bounds.Encapsulate(new Bounds(centre, new Vector3(
                    Mathf.Abs(extents.x) * 2f, Mathf.Abs(extents.y) * 2f,
                    Mathf.Abs(extents.z) * 2f)));
            }

            // A centimetre of slack for bevels; anything more is a real overshoot.
            Assert.That(bounds.size.x, Is.LessThanOrEqualTo(box.X + 0.011f),
                box.Prefab + " is wider than its catalog footprint.");
            Assert.That(bounds.size.y, Is.LessThanOrEqualTo(box.Y + 0.011f),
                box.Prefab + " is taller than its catalog height.");
            Assert.That(bounds.size.z, Is.LessThanOrEqualTo(box.Z + 0.011f),
                box.Prefab + " is deeper than its catalog footprint.");
        }
    }

    /// <summary>
    /// The Patio is the room where a single approach rule would be wrong three
    /// times over: two products stand against the back wall in `facesBackward`
    /// and are approached from root +Z, two stand hard against the right of the
    /// courtyard and are approached from root -X, and the rest are open floor
    /// products approached from root -Z. Yaw never decides this on its own.
    /// </summary>
    [Test]
    public void Patio_ApproachesEveryProductFromTheCourtyard()
    {
        foreach (string prefabName in new[] { "PatioHerbTrough", "PatioStringLights",
                                              "PatioPergolaArch" })
        {
            Transform anchor = LoadStoreProduct(prefabName).transform.Find("InteractionAnchor");
            Assert.That(anchor, Is.Not.Null, prefabName + " has no InteractionAnchor.");
            Assert.That(anchor.localPosition.z, Is.GreaterThan(0.4f),
                prefabName + " is in `facesBackward`: the courtyard is at root +Z.");
        }

        foreach (string prefabName in new[] { "PatioWaterFountain" })
        {
            Transform anchor = LoadStoreProduct(prefabName).transform.Find("InteractionAnchor");
            Assert.That(anchor, Is.Not.Null, prefabName + " has no InteractionAnchor.");
            Assert.That(anchor.localPosition.x, Is.LessThan(-0.4f),
                prefabName + " stands at the right edge: the courtyard is at root -X.");
        }

        Transform fern = LoadStoreProduct("PatioPottedFerns").transform.Find("InteractionAnchor");
        Assert.That(fern, Is.Not.Null);
        Assert.That(fern.localPosition.z, Is.LessThan(-0.5f),
            "The rotated fern leaves its front open to the courtyard.");
        Assert.That(fern.localPosition.x, Is.GreaterThan(0.1f),
            "The entry clears the side of the fern pot.");

        foreach (string prefabName in new[] { "PatioParasol", "PatioDiningSet",
                                              "PatioFirePit", "PatioStoneRug" })
        {
            Transform anchor = LoadStoreProduct(prefabName).transform.Find("InteractionAnchor");
            Assert.That(anchor, Is.Not.Null, prefabName + " has no InteractionAnchor.");
            Assert.That(anchor.localPosition.z, Is.LessThan(-0.4f),
                prefabName + " is open floor: the courtyard is at root -Z.");
        }
    }

    /// <summary>
    /// The fire pit's spark screen is deliberately open on one side, and that
    /// side has to be the side the cat lies on. Both the bask point and the
    /// screen gap are authored at -Z; if one of them ever moves, the cat curls
    /// up against a wall of mesh.
    /// </summary>
    [Test]
    public void FirePitBask_PutsTheCatOnTheOpenFaceOfTheScreen()
    {
        var bask = (OvenWarmthActivity)LoadRoomActivity(
            "PatioFirePit", CatActivityKind.FirePitBask);
        var prefab = LoadStoreProduct("PatioFirePit");

        Transform baskPoint = prefab.transform.Find("BaskPoint");
        Transform door = prefab.transform.Find("BaskDoorPoint");
        Assert.That(baskPoint, Is.Not.Null);
        Assert.That(door, Is.Not.Null);
        Assert.That(baskPoint.localPosition.z, Is.LessThan(-0.5f),
            "The cat lies on the open face of the screen, which is authored at -Z.");
        Assert.That(door.localPosition.z, Is.LessThan(0f),
            "The warm face and the screen gap have to be the same side.");
        Assert.That(door.localPosition.y, Is.GreaterThan(0.2f),
            "The warmth reads from the ledge, not from the floor.");

        AssertUnlocksWithPurchase(bask, HomeStoreService.PatioFirePitId);
    }

    /// <summary>
    /// A cat drinks from the LOW basin of a two-tier fountain, never from the
    /// top bowl at 0.640. The rim was authored at 0.300 for that reason.
    /// </summary>
    [Test]
    public void FountainSip_DrinksFromTheLowBasinAndNotTheTopBowl()
    {
        var sip = (SinkSipActivity)LoadRoomActivity(
            "PatioWaterFountain", CatActivityKind.FountainSip);
        var prefab = LoadStoreProduct("PatioWaterFountain");

        Transform perch = prefab.transform.Find("SipPerchPoint");
        Transform floor = prefab.transform.Find("SipFloorPoint");
        Assert.That(perch, Is.Not.Null);
        Assert.That(floor, Is.Not.Null);
        Assert.That(perch.localPosition.y, Is.LessThan(0.42f),
            "The upper bowl is at 0.640 and is not what a cat drinks from.");
        Assert.That(perch.localPosition.y, Is.GreaterThan(0.2f),
            "The basin rim is at 0.300, not on the ground.");
        Assert.That(floor.localPosition.x, Is.LessThan(-0.4f),
            "The fountain is at the right edge of the courtyard.");

        AssertUnlocksWithPurchase(sip, HomeStoreService.PatioWaterFountainId);
    }

    /// <summary>
    /// The dining table has a parasol hole through its middle, so the perch has
    /// to be on the clear half — a cat cannot lie on a hole.
    /// </summary>
    [Test]
    public void DiningPerch_SitsOnTheClearHalfAndNotOnTheParasolHole()
    {
        var perchNap = (PerchNapActivity)LoadRoomActivity(
            "PatioDiningSet", CatActivityKind.DiningPerch);
        var prefab = LoadStoreProduct("PatioDiningSet");

        Transform perch = prefab.transform.Find("PerchPoint");
        Assert.That(perch, Is.Not.Null);
        Assert.That(perch.localPosition.y, Is.EqualTo(0.44f).Within(0.02f),
            "The table top is authored at 0.440.");
        Assert.That(Mathf.Abs(perch.localPosition.x), Is.GreaterThan(0.3f),
            "The centre of the top is the parasol hole; the perch is offset from it.");

        AssertUnlocksWithPurchase(perchNap, HomeStoreService.PatioDiningSetId);
    }

    [Test]
    public void SecondFloor_EveryProductCarriesItsOwnRoutine()
    {
        // The loft was the last room in the house with no cat behaviour on any
        // of its ten products. This is the table that closes wave 3.
        var expected = new (string Prefab, CatActivityKind Kind, string ProductId, QuestType Quest)[]
        {
            ("LoftFloorRunner", CatActivityKind.RunnerKnead,
             HomeStoreService.LoftFloorRunnerId, QuestType.MatKnead),
            ("LoftFloorCushions", CatActivityKind.CushionNest,
             HomeStoreService.LoftFloorCushionsId, QuestType.Sleep),
            ("LoftBookStack", CatActivityKind.BookKnockOff,
             HomeStoreService.LoftBookStackId, QuestType.KnockOff),
            ("LoftArcLamp", CatActivityKind.LampGlowBask,
             HomeStoreService.LoftArcLampId, QuestType.Sleep),
            ("LoftBeanBag", CatActivityKind.BeanBagNap,
             HomeStoreService.LoftBeanBagId, QuestType.Sleep),
            ("LoftRecordPlayer", CatActivityKind.RecordSpin,
             HomeStoreService.LoftRecordPlayerId, QuestType.PaperSpin),
            ("LoftStudyDesk", CatActivityKind.DeskPerch,
             HomeStoreService.LoftStudyDeskId, QuestType.Sleep),
            ("LoftWallGallery", CatActivityKind.GalleryGaze,
             HomeStoreService.LoftWallGalleryId, QuestType.LoftWatch),
            ("LoftTallBookcase", CatActivityKind.BookcaseClimb,
             HomeStoreService.LoftTallBookcaseId, QuestType.PantryClimb),
            ("LoftChaiseLounge", CatActivityKind.ChaiseNap,
             HomeStoreService.LoftChaiseLoungeId, QuestType.Sleep),
        };

        foreach (var row in expected)
        {
            CatActivity activity = LoadRoomActivity(row.Prefab, row.Kind);
            Assert.That(activity.QuestType, Is.EqualTo(row.Quest), row.Prefab);
            Assert.That(activity.StoreProductId, Is.EqualTo(row.ProductId), row.Prefab);
            Assert.That(activity.IsUnlocked, Is.False, row.Prefab + " waits for the purchase.");
        }
    }

    /// <summary>
    /// Loft models skip `FitFixtureModel` the way the Balcony's and the Patio's
    /// do, so an oversized mesh ships oversized. Seven of the twelve Loft files
    /// overshot on their first build — a rotated cushion grows past its own
    /// span, a paw badge reaches past the panel it is pinned to, top-shelf
    /// books pierce a cornice — and this locks all ten products down.
    /// </summary>
    [Test]
    public void SecondFloorProducts_AreAuthoredInsideTheirCatalogBox()
    {
        var boxes = new (string Prefab, float X, float Y, float Z)[]
        {
            ("LoftFloorRunner", 1.90f, 0.08f, 1.15f),
            ("LoftFloorCushions", 0.90f, 0.50f, 0.90f),
            ("LoftBookStack", 0.60f, 0.60f, 0.50f),
            ("LoftArcLamp", 0.70f, 1.85f, 0.70f),
            ("LoftBeanBag", 0.95f, 0.55f, 0.95f),
            ("LoftRecordPlayer", 0.90f, 0.70f, 0.60f),
            ("LoftStudyDesk", 1.40f, 0.80f, 0.90f),
            ("LoftWallGallery", 1.60f, 0.90f, 0.20f),
            ("LoftTallBookcase", 1.40f, 2.10f, 0.40f),
            ("LoftChaiseLounge", 1.60f, 0.70f, 0.70f),
        };

        foreach (var box in boxes)
        {
            GameObject prefab = LoadStoreProduct(box.Prefab);
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0), box.Prefab);
            Bounds bounds = new Bounds(
                renderers[0].transform.TransformPoint(renderers[0].localBounds.center),
                Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                Bounds local = renderer.localBounds;
                Vector3 centre = renderer.transform.TransformPoint(local.center);
                Vector3 extents = renderer.transform.TransformVector(local.extents);
                bounds.Encapsulate(new Bounds(centre, new Vector3(
                    Mathf.Abs(extents.x) * 2f, Mathf.Abs(extents.y) * 2f,
                    Mathf.Abs(extents.z) * 2f)));
            }

            // A centimetre of slack for bevels; anything more is a real overshoot.
            Assert.That(bounds.size.x, Is.LessThanOrEqualTo(box.X + 0.011f),
                box.Prefab + " is wider than its catalog footprint.");
            Assert.That(bounds.size.y, Is.LessThanOrEqualTo(box.Y + 0.011f),
                box.Prefab + " is taller than its catalog height.");
            Assert.That(bounds.size.z, Is.LessThanOrEqualTo(box.Z + 0.011f),
                box.Prefab + " is deeper than its catalog footprint.");
        }
    }

    /// <summary>
    /// The loft mixes three approach rules. Two products stand against the back
    /// wall in `facesBackward` and are approached from root +Z; three stand hard
    /// against a side of the room and are approached across X even though their
    /// yaw is 0; the rest are open floor and are approached from root -Z. Yaw
    /// decides none of it — the position does.
    /// </summary>
    [Test]
    public void SecondFloor_ApproachesEveryProductFromTheRoomAndNotTheWall()
    {
        foreach (string prefabName in new[] { "LoftWallGallery", "LoftTallBookcase" })
        {
            Transform anchor = LoadStoreProduct(prefabName).transform.Find("InteractionAnchor");
            Assert.That(anchor, Is.Not.Null, prefabName + " has no InteractionAnchor.");
            Assert.That(anchor.localPosition.z, Is.GreaterThan(0.5f),
                prefabName + " is in `facesBackward`: the room is at root +Z.");
        }

        foreach (string prefabName in new[] { "LoftFloorCushions", "LoftBookStack" })
        {
            Transform anchor = LoadStoreProduct(prefabName).transform.Find("InteractionAnchor");
            Assert.That(anchor, Is.Not.Null, prefabName + " has no InteractionAnchor.");
            Assert.That(anchor.localPosition.x, Is.LessThan(-0.4f),
                prefabName + " stands at the right of the loft: the room is at root -X.");
        }

        Transform lamp = LoadStoreProduct("LoftArcLamp").transform.Find("InteractionAnchor");
        Assert.That(lamp, Is.Not.Null);
        Assert.That(lamp.localPosition.x, Is.GreaterThan(0.4f),
            "The lamp stands at the LEFT of the loft, so the room is at root +X.");

        foreach (string prefabName in new[] { "LoftFloorRunner", "LoftBeanBag",
                                              "LoftRecordPlayer", "LoftStudyDesk" })
        {
            Transform anchor = LoadStoreProduct(prefabName).transform.Find("InteractionAnchor");
            Assert.That(anchor, Is.Not.Null, prefabName + " has no InteractionAnchor.");
            Assert.That(anchor.localPosition.z, Is.LessThan(-0.4f),
                prefabName + " is open floor: the room is at root -Z.");
        }

        Transform chaise = LoadStoreProduct("LoftChaiseLounge").transform.Find("InteractionAnchor");
        Assert.That(chaise, Is.Not.Null);
        Assert.That(chaise.localPosition.z, Is.GreaterThan(0.5f),
            "With the headboard against the wall, entry is on the open seat side.");
        Assert.That(chaise.localPosition.x, Is.GreaterThan(0.5f),
            "The entry clears the end of the chaise.");
    }

    /// <summary>
    /// The loft's two moving parts. Both hang under their own pivot from a
    /// second single-object FBX on the product's origin, and both children have
    /// to cancel the pivot offset or the part lands at twice it.
    /// </summary>
    [Test]
    public void SecondFloor_HangsItsTwoMovingPartsOnTheirOwnPivots()
    {
        var knock = (KnockOffActivity)LoadRoomActivity(
            "LoftBookStack", CatActivityKind.BookKnockOff);
        Assert.That(knock.GlassPivot, Is.Not.Null);
        Assert.That(knock.GlassPivot.Find("Book"), Is.Not.Null,
            "The loose volume is a second FBX hung under the pivot.");
        Assert.That(knock.GlassPivot.localPosition.y, Is.EqualTo(0.49f).Within(0.02f),
            "The book sits on the stack's board at 0.454, not inside it.");
        Assert.That(knock.GlassPivot.Find("Book").localPosition,
            Is.EqualTo(-knock.GlassPivot.localPosition)
                .Using<Vector3>((a, b) => (a - b).sqrMagnitude < 1e-6f ? 0 : 1),
            "The book mesh is authored on the product origin and must cancel the pivot.");

        var spin = (PaperSpinActivity)LoadRoomActivity(
            "LoftRecordPlayer", CatActivityKind.RecordSpin);
        Assert.That(spin.RollPivot, Is.Not.Null);
        Assert.That(spin.RollPivot.Find("Record"), Is.Not.Null,
            "The record is a second FBX hung under the pivot.");
        Assert.That(spin.RollPivot.localPosition.y, Is.EqualTo(0.40f).Within(0.02f),
            "The record sits in the platter well at 0.400.");
        Assert.That(spin.RollPivot.localPosition.x, Is.GreaterThan(0f),
            "The platter is authored at -X and this product is not in `facesBackward`.");
    }

    /// <summary>
    /// The chaise is entered rather than climbed onto, so its product box has to
    /// be a trigger the way the star tipi's and the play tunnel's are, and the
    /// way in has to be the open foot end and not the scrolled head.
    /// </summary>
    [Test]
    public void ChaiseNap_EntersAtTheOpenFootEndAndSettlesInFrontOfTheBolster()
    {
        var nap = (CanopyNapActivity)LoadRoomActivity(
            "LoftChaiseLounge", CatActivityKind.ChaiseNap);
        var prefab = LoadStoreProduct("LoftChaiseLounge");

        Transform door = prefab.transform.Find("NapDoorPoint");
        Transform nest = prefab.transform.Find("NapNestPoint");
        Assert.That(door, Is.Not.Null);
        Assert.That(nest, Is.Not.Null);
        Assert.That(door.localPosition.x, Is.GreaterThan(0.5f),
            "The head is authored at +X and lands at root -X, so the way in is root +X.");
        Assert.That(nest.localPosition.x, Is.LessThan(0f),
            "The nest is the pocket in front of the bolster, towards the head.");
        Assert.That(nest.localPosition.y, Is.GreaterThan(0.25f),
            "The cat settles on the seat, not on the floor beside it.");

        var collider = prefab.transform.Find("VisualContent").GetComponent<BoxCollider>();
        Assert.That(collider, Is.Not.Null);
        Assert.That(collider.isTrigger, Is.True,
            "An entered product's box has to be a trigger or the cat cannot walk in.");

        AssertUnlocksWithPurchase(nap, HomeStoreService.LoftChaiseLoungeId);
    }

    private static Transform FindPremiumModel(GameObject prefab)
    {
        foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.EndsWith("_PremiumModel", System.StringComparison.Ordinal))
                return child;
        }

        return null;
    }

    [Test]
    public void HerbShelfClimb_SitsOnTheOpenHeaderBetweenThePots()
    {
        var climb = (PantryClimbActivity)LoadRoomActivity(
            "BalconyHerbShelf", CatActivityKind.HerbShelfClimb);
        var prefab = LoadStoreProduct("BalconyHerbShelf");

        Transform floor = prefab.transform.Find("ClimbFloorPoint");
        Transform lower = prefab.transform.Find("ClimbLowerPoint");
        Transform upper = prefab.transform.Find("ClimbUpperPoint");
        Assert.That(floor, Is.Not.Null);
        Assert.That(lower, Is.Not.Null);
        Assert.That(upper, Is.Not.Null);
        Assert.That(floor.localPosition.y, Is.EqualTo(0f));
        Assert.That(lower.localPosition.y, Is.GreaterThan(0.3f));
        Assert.That(climb.UsesIntermediatePerch, Is.False);
        Assert.That(upper.localPosition.y / ProductScale(prefab), Is.EqualTo(1.27f).Within(.01f));
        Assert.That(upper.GetComponent<CatActivitySurface>().ResolvePose(CatActivityPose.Sleep), Is.EqualTo(CatActivityPose.Sit));
        // Yaw 90 at x -3.22 would face the rack into the left wall, so the shelf
        // is in `facesBackward` and the deck is at root +Z.
        Assert.That(floor.localPosition.z, Is.GreaterThan(0f),
            "The cat climbs on from the deck, which is root +Z.");
        Assert.That(prefab.transform.Find("InteractionAnchor").localPosition.z,
            Is.GreaterThan(floor.localPosition.z));

        AssertUnlocksWithPurchase(climb, HomeStoreService.BalconyHerbShelfId);
    }

    [Test]
    public void Balcony_EveryInteractiveProductCarriesItsOwnRoutine()
    {
        // Balcony v1 shipped deliberately without cat routines; the wave-3
        // decision brought it up to the same contract as every other room. This
        // is the table that says so.
        var expected = new (string Prefab, CatActivityKind Kind, string ProductId, QuestType Quest)[]
        {
            ("BalconyHerbShelf", CatActivityKind.HerbShelfClimb,
             HomeStoreService.BalconyHerbShelfId, QuestType.PantryClimb),
            ("BalconyBirdFeeder", CatActivityKind.FeederShake,
             HomeStoreService.BalconyBirdFeederId, QuestType.FeederShake),
            ("BalconyHangingChair", CatActivityKind.EggChairNap,
             HomeStoreService.BalconyHangingChairId, QuestType.Sleep),
            ("BalconyCushionBench", CatActivityKind.BenchNap,
             HomeStoreService.BalconyCushionBenchId, QuestType.Sleep),
            ("BalconySideTable", CatActivityKind.TableKnockOff,
             HomeStoreService.BalconySideTableId, QuestType.KnockOff),
            ("BalconySunMat", CatActivityKind.SunMatBask,
             HomeStoreService.BalconySunMatId, QuestType.Sleep),
            ("BalconyPlanterBox", CatActivityKind.PlanterDig,
             HomeStoreService.BalconyPlanterBoxId, QuestType.LitterDig),
            ("BalconyRailingFlowers", CatActivityKind.RailingSwat,
             HomeStoreService.BalconyRailingFlowersId, QuestType.BalconyWatch),
            ("BalconyLanternString", CatActivityKind.LanternGaze,
             HomeStoreService.BalconyLanternStringId, QuestType.BalconyWatch),
        };

        foreach (var row in expected)
        {
            CatActivity activity = LoadRoomActivity(row.Prefab, row.Kind);
            Assert.That(activity.QuestType, Is.EqualTo(row.Quest), row.Prefab);
            Assert.That(activity.StoreProductId, Is.EqualTo(row.ProductId), row.Prefab);
            Assert.That(activity.IsUnlocked, Is.False, row.Prefab + " waits for the purchase.");
        }
    }

    /// <summary>
    /// Balcony models skip `FitFixtureModel` entirely, so an oversized mesh is
    /// shipped oversized — there is no fit pass to rescue it. Six of the ten
    /// overshot on the first build and were trimmed by hand; this locks that in.
    /// </summary>
    [Test]
    public void BalconyProducts_AreAuthoredInsideTheirCatalogBox()
    {
        var boxes = new (string Prefab, float X, float Y, float Z)[]
        {
            ("BalconySunAwning", 2.40f, 0.71f, 1.10f),
            ("BalconyHerbShelf", 1.00f, 1.40f, 0.50f),
            ("BalconyBirdFeeder", 0.60f, 1.40f, 0.60f),
            ("BalconyHangingChair", 0.90f, 1.50f, 0.90f),
            ("BalconyCushionBench", 1.60f, 0.55f, 0.60f),
            ("BalconySideTable", 0.70f, 0.50f, 0.70f),
            ("BalconySunMat", 1.90f, 0.08f, 1.15f),
            ("BalconyPlanterBox", 0.85f, 0.50f, 0.55f),
            ("BalconyRailingFlowers", 1.60f, 0.90f, 0.35f),
            ("BalconyLanternString", 1.60f, 0.30f, 0.30f),
        };

        foreach (var box in boxes)
        {
            GameObject prefab = LoadStoreProduct(box.Prefab);
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0), box.Prefab);
            Bounds bounds = renderers[0].localBounds;
            bounds = new Bounds(renderers[0].transform.TransformPoint(bounds.center),
                                Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                Bounds local = renderer.localBounds;
                Vector3 centre = renderer.transform.TransformPoint(local.center);
                Vector3 extents = renderer.transform.TransformVector(local.extents);
                bounds.Encapsulate(new Bounds(centre, new Vector3(
                    Mathf.Abs(extents.x) * 2f, Mathf.Abs(extents.y) * 2f,
                    Mathf.Abs(extents.z) * 2f)));
            }

            // A centimetre of slack for bevels; anything more is a real overshoot.
            Assert.That(bounds.size.x, Is.LessThanOrEqualTo(box.X + 0.011f),
                box.Prefab + " is wider than its catalog footprint.");
            Assert.That(bounds.size.y, Is.LessThanOrEqualTo(box.Y + 0.011f),
                box.Prefab + " is taller than its catalog height.");
            Assert.That(bounds.size.z, Is.LessThanOrEqualTo(box.Z + 0.011f),
                box.Prefab + " is deeper than its catalog footprint.");
        }
    }

    [Test]
    public void FeederShake_HangsTheFeederAndTheSeedOnTheirOwnPivots()
    {
        var shake = (BirdFeederShakeActivity)LoadRoomActivity(
            "BalconyBirdFeeder", CatActivityKind.FeederShake);
        var prefab = LoadStoreProduct("BalconyBirdFeeder");

        // The room's one bespoke routine. Both moving parts hang under their own
        // pivot, because the feeder swings and the seed drops independently.
        Assert.That(shake.FeederPivot, Is.Not.Null);
        Assert.That(shake.SeedPivot, Is.Not.Null);
        Assert.That(shake.FeederPivot.Find("Feeder"), Is.Not.Null,
            "The feeder is a second FBX hung under the pivot.");
        Assert.That(shake.SeedPivot.Find("Seed"), Is.Not.Null,
            "The seed is a third FBX hung under its own pivot.");
        Assert.That(shake.FeederPivot.localPosition.y, Is.GreaterThan(1f),
            "The feeder hangs above a cat, which is the whole point.");
        Assert.That(shake.BatCount, Is.GreaterThanOrEqualTo(2));

        // The cat works this one from BELOW: it stays on the deck and rears up,
        // so the reach point is on the floor and not on the product.
        Transform reach = prefab.transform.Find("ShakeReachPoint");
        Assert.That(reach, Is.Not.Null);
        Assert.That(reach.localPosition.y, Is.EqualTo(0f),
            "The cat never leaves the deck for this routine.");
        Assert.That(reach.localPosition.z, Is.LessThan(0f));

        AssertUnlocksWithPurchase(shake, HomeStoreService.BalconyBirdFeederId);
    }
    [Test]
    public void RestoreThirst_AddsBackWithoutOverfilling()
    {
        var root = new GameObject("SipThirstTest");
        ThirstSystem thirst = root.AddComponent<ThirstSystem>();

        thirst.ApplySavedValue(40f);
        thirst.RestoreThirst(45f);
        Assert.That(thirst.CurrentThirst, Is.EqualTo(85f));

        thirst.RestoreThirst(-5f);
        Assert.That(thirst.CurrentThirst, Is.EqualTo(85f), "Negative restores must be ignored.");

        thirst.RestoreThirst(90f);
        Assert.That(thirst.CurrentThirst, Is.EqualTo(100f));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void RestoreEnergy_AddsBackWithoutOverfilling()
    {
        var root = new GameObject("NapEnergyTest");
        EnergySystem energy = root.AddComponent<EnergySystem>();

        energy.ApplySavedValue(40f);
        energy.RestoreEnergy(22f);
        Assert.That(energy.CurrentEnergy, Is.EqualTo(62f));

        energy.RestoreEnergy(-5f);
        Assert.That(energy.CurrentEnergy, Is.EqualTo(62f), "Negative restores must be ignored.");

        energy.RestoreEnergy(90f);
        Assert.That(energy.CurrentEnergy, Is.EqualTo(100f));

        Object.DestroyImmediate(root);
    }

    private T FindInScene<T>() where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }
        return null;
    }

    private SitLookActivity FindWindowWatch()
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            SitLookActivity[] activities = root.GetComponentsInChildren<SitLookActivity>(true);
            for (int i = 0; i < activities.Length; i++)
            {
                if (activities[i] != null && activities[i].Kind == CatActivityKind.WindowWatch)
                    return activities[i];
            }
        }

        return null;
    }
    // Geometry assertions use authored units; the native matrix verifies the actual resized surface.
    private static float ProductScale(GameObject prefab)
    {
        var stamp = prefab.GetComponent<RoomProductScaleStamp>();
        return stamp != null ? stamp.AppliedScale : 1f;
    }
}
