using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>Authoring coverage gate. Runtime and visual evidence still verify the actual pose.</summary>
public static class HomeActivityFacingValidation
{
    static readonly HashSet<Type> ReviewedTypes = new HashSet<Type>
    {
        typeof(CatCommandActivity), typeof(CatEnrichmentActivity), typeof(LivingFurnitureActivity),
        typeof(SitLookActivity), typeof(MatKneadActivity), typeof(OvenWarmthActivity),
        typeof(PerchNapActivity), typeof(TowelNestActivity), typeof(CanopyNapActivity),
        typeof(PantryClimbActivity), typeof(SwingRideActivity), typeof(ShowerRinseActivity),
        typeof(LitterDigActivity), typeof(PaperSpinActivity), typeof(ScratchPostActivity),
        typeof(SinkSipActivity), typeof(MealTimeActivity), typeof(TubEdgeWalkActivity),
        typeof(GroomBrushActivity), typeof(HamperDiveActivity), typeof(KnockOffActivity),
        typeof(CartNudgeActivity), typeof(BirdFeederShakeActivity), typeof(BallChaseActivity),
        typeof(GardenYarnChaseActivity)
    };

    public static IEnumerable<string> Validate(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        foreach (var activity in root.GetComponentsInChildren<CatActivity>(true))
        {
            if (!ReviewedTypes.Contains(activity.GetType()))
                yield return scene.name + "/" + activity.name + ": add an explicit camera-facing/contact/path policy and native evidence for " + activity.GetType().Name;
            var litter = activity as LitterDigActivity;
            if (litter != null && litter.UsesGentleScraping && (litter.LitterSurface == null || !litter.LitterSurface.IsConfigured))
                yield return scene.name + "/" + activity.name + ": litter action needs its independent excavatable surface.";
            if (activity is SinkSipActivity)
            {
                var mouths = CatSipMouthCatalog.Load();
                if (mouths == null) yield return scene.name + "/" + activity.name + ": bake the actual cat mouth profiles.";
                else foreach (var breed in CatBreedCatalog.Load().Entries)
                {
                    var profile = mouths.Find(breed.Id);
                    if (profile == null || profile.vertices == null || profile.vertices.Length == 0)
                        yield return scene.name + "/" + activity.name + ": missing mouth skinning for " + breed.Id;
                }
            }
        }
    }
}
