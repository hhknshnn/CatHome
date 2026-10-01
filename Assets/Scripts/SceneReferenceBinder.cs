using UnityEngine;

/// <summary>
/// Reconnects the few intentional references that cross the common UI and
/// gameplay scene boundary. The references are discovered by component type,
/// so level scenes never need to serialize links into the shared UI scene.
/// </summary>
public static class SceneReferenceBinder
{
    public static void RebindAll()
    {
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        if (cat != null)
            cat.ResolveSceneReferences();

        BowlInteraction[] bowls =
            Object.FindObjectsByType<BowlInteraction>(FindObjectsInactive.Include);
        for (int i = 0; i < bowls.Length; i++)
            bowls[i].ResolveSceneReferences();

        HungerSystem hunger =
            Object.FindAnyObjectByType<HungerSystem>(FindObjectsInactive.Include);
        if (hunger != null)
            hunger.ResolveSceneReferences();

        ThirstSystem thirst =
            Object.FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include);
        if (thirst != null)
            thirst.ResolveSceneReferences();

        EnergySystem energy =
            Object.FindAnyObjectByType<EnergySystem>(FindObjectsInactive.Include);
        if (energy != null)
            energy.ResolveSceneReferences();

        // A disabled legacy canvas can coexist with the shared UI during
        // bootstrap. Bind every menu instead of selecting that stale copy.
        foreach (MainPanelController mainPanel in Object.FindObjectsByType<MainPanelController>(
                     FindObjectsInactive.Include))
            mainPanel.ResolveSceneReferences();

        RoomSelectorPanel roomSelector =
            Object.FindAnyObjectByType<RoomSelectorPanel>(FindObjectsInactive.Include);
        if (roomSelector != null)
            roomSelector.ResolveSceneReferences();

        QuestPanelController questPanel =
            Object.FindAnyObjectByType<QuestPanelController>(FindObjectsInactive.Include);
        if (questPanel != null)
            questPanel.ResolveSceneReferences();

        ActivityPromptController activityPrompt =
            Object.FindAnyObjectByType<ActivityPromptController>(FindObjectsInactive.Include);
        if (activityPrompt != null)
            activityPrompt.ResolveSceneReferences();
    }
}
