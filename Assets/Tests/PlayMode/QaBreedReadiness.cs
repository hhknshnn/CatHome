using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

// Observes the production replacement barrier. No preloading, model mutation,
// artificial Animator rebinding, or catalog warm-up is performed here.
internal static class QaBreedReadiness
{
    internal static IEnumerator WaitForSelected(CatMovement actor, string expectedBreed = null)
    {
        string expected = expectedBreed ?? CatBreedService.SelectedBreedId;
        float deadline = Time.realtimeSinceStartup + 20f;
        int settled = 0; string actual = "missing"; bool pending = true;
        while (settled < 3 && Time.realtimeSinceStartup < deadline)
        {
            // Readiness observes the preceding frame; it does not measure a
            // rendered pose. Keep its deadline advancing with Scene View open.
            yield return null;
            var tags = actor != null ? actor.GetComponentsInChildren<CatBreedVisualTag>() : new CatBreedVisualTag[0];
            actual = string.Join(";", tags.Select(t => t.BreedId));
            pending = CatPawReachCatalog.HasPendingLoads;
            bool ready = actor != null && CatBreedService.SelectedBreedId == expected &&
                tags.Length == 1 && tags[0].BreedId == expected && !pending && actor.HasBodyGuardProfile;
            settled = ready ? settled + 1 : 0;
        }
        Assert.That(settled, Is.EqualTo(3), "Production breed replacement did not settle: expected=" + expected +
            ";selected=" + CatBreedService.SelectedBreedId + ";actual=" + actual + ";pending=" + pending);
        Physics.SyncTransforms();
    }
}
