using UnityEngine;

// Optional shared-planner fallback for a raw source-body refusal. No actor,
// animation, collider or source-data mutation; no ignored support collider.
public static class CatJumpSupportedPreparation
{
    public static bool TryClear(CatMovement actor,CatJumpClearanceCatalog.Entry entry,
        CatJumpClearanceCatalog.Sample sample,Matrix4x4 sourceMatrix,Vector3 correction,
        Vector3 supportPosition,Quaternion heading,out bool clear)
    {
        clear=false;
        if(actor==null||entry==null||sample==null||sample.phase>CatJumpMotion.Takeoff+.000001f||
            entry.supportedPreparationVersion!=CatJumpClearanceCatalog.SupportedPreparationVersion||
            entry.supportBonePaths==null||entry.supportBonePaths.Length==0||sample.supportSkinMatrices==null||
            sample.supportSkinMatrices.Length!=entry.supportBonePaths.Length)return false;
        var owner=CatActivity.Active;var motion=actor.GetComponent<CatMeasuredSupportMotion>();
        var animation=actor.GetComponent<CatActivityAnimation>();
        if(owner==null||!owner.TryGetPreparedStart(actor,out _)||motion==null||motion.Owner!=owner||
            !motion.IsActive||animation==null||!animation.IsActive||animation.IsNativeJump)return false;
        // The caller includes the actual preceding lift also carried by
        // BeginNativeJump, and rechecks after the real departure pivot.
        var profile=CatCareSkinCatalog.Load()?.Find(entry.breedId);
        if(profile==null||profile.sourceKey!=entry.supportProfileKey||profile.sourceHash!=entry.supportProfileHash||
            profile.bonePaths==null||profile.bonePaths.Length!=entry.supportBonePaths.Length)return false;
        for(int b=0;b<profile.bonePaths.Length;b++)
            if(profile.bonePaths[b]!=entry.supportBonePaths[b])return false;
        Matrix4x4 mapping=Matrix4x4.Translate(correction)*sourceMatrix;
        var source=motion.CapturePreparationSource(entry.breedId,profile,entry.supportBonePaths,mapping,sample.supportSkinMatrices);
        if(source==null)return false;
        // Runtime evaluates and applies this same numerical plan before its
        // legacy solver. Missing/unreachable/blocked plans keep the old refusal.
        return motion.TryPredictNativePreparation(source,supportPosition,heading,out clear);
    }
}
