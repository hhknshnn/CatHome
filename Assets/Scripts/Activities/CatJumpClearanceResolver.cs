using UnityEngine;

/// <summary>
/// Pure NativeJump preparation/recovery clearance. No live clip sampling,
/// actor/visual pose writes, scale changes or locomotion/approach routing.
/// Does not claim flight, support-turn or later rest-pose clearance.
/// </summary>
public static class CatJumpClearanceResolver
{
    const float BodyTolerance=.005f;
    static readonly CatBodyGuardCatalog.Probe[] probes = new CatBodyGuardCatalog.Probe[4];
    static readonly CatBodyGuardCatalog.Probe[] trunk = new CatBodyGuardCatalog.Probe[3];
    static readonly CatBodyGuardCatalog.Probe[] one = new CatBodyGuardCatalog.Probe[1];
    public struct Rejection { public float phase; public Vector3 root; public Quaternion heading; }

    public static bool EndsClear(CatMovement actor, Vector3 from, Vector3 to,
        Quaternion heading, bool landOnSupport, bool preparedGroundLaunch, out Rejection rejection)
    {
        rejection = default;
        if (actor == null) return false;
        var visual = actor.GetComponentInChildren<CatBreedVisualTag>();
        var entry = visual != null ? CatJumpClearanceCatalog.Load()?.Find(visual.BreedId) : null;
        if (entry?.samples == null || entry.samples.Length == 0) return false;
        Matrix4x4 tagMatrix = visual.transform.localToWorldMatrix;
        var supportMotion = actor.GetComponent<CatMeasuredSupportMotion>();
        if (supportMotion != null && supportMotion.TryReadSourceVisualMatrix(visual.transform, out var sourceVisual))
            tagMatrix = sourceVisual;
        Matrix4x4 tagInRoot = actor.transform.worldToLocalMatrix * tagMatrix;
        Vector3 actorScale = actor.transform.lossyScale;
        Vector3 startOffset = Vector3.zero;
        if (!preparedGroundLaunch)
        {
            var animation = actor.GetComponent<CatActivityAnimation>();
            if (animation == null || !animation.TryReadNativeJumpStartOffset(heading, out startOffset)) return false;
        }
        Vector3 centre = tagInRoot.MultiplyPoint3x4(entry.sourceZeroCentre);
        Vector3 endOffset = landOnSupport ? new Vector3(-centre.x, 0, -centre.z) : Vector3.zero;
        foreach (var sample in entry.samples)
        {
            bool preparation = sample.phase <= CatJumpMotion.Takeoff + .000001f;
            if (!preparation && sample.phase < CatJumpMotion.Touchdown - .000001f) continue;
            if (sample.probes == null || sample.probes.Length != probes.Length) return false;
            Vector3 root = preparation ? from : to;
            Matrix4x4 rootMatrix = Matrix4x4.TRS(root, heading, actorScale);
            Matrix4x4 sourceMatrix = rootMatrix * tagInRoot;
            Vector3 correction = rootMatrix.MultiplyVector(preparation ? startOffset : endOffset);
            correction += Vector3.up * (root.y - sourceMatrix.m13 + .008f);
            // BeginNativeJump carries this exact measured departure lift.
            // GroundLanding is checked again after the actual allowed pivot.
            if(preparation&&!preparedGroundLaunch&&supportMotion!=null&&supportMotion.IsActive)
                correction+=Vector3.up*supportMotion.VisualLift;
            if(!preparation&&!CatActivityStartResolver.LandsOnSupport(actor,to)&&!CatJumpLimbClearance.IsClear(actor,entry,sample,sourceMatrix,correction))
            {rejection=new Rejection{phase=sample.phase,root=root,heading=heading};return false;}
            float scale = Mathf.Max(((Vector3)sourceMatrix.GetColumn(0)).magnitude,
                Mathf.Max(((Vector3)sourceMatrix.GetColumn(1)).magnitude, ((Vector3)sourceMatrix.GetColumn(2)).magnitude));
            for (int i = 0; i < probes.Length; i++)
            {
                var p = sample.probes[i]; p.start = sourceMatrix.MultiplyPoint3x4(p.start) + correction;
                p.end = sourceMatrix.MultiplyPoint3x4(p.end) + correction; p.radius *= scale; probes[i] = p;
            }
            if (HasBodyCoverage(sample))
            {
                bool clear = true;
                for(int regionIndex=0;regionIndex<sample.bodyCoverage.Length;regionIndex++)
                {
                    var region=sample.bodyCoverage[regionIndex];
                    if(RegionClear(actor,region,sourceMatrix,correction,scale))continue;
                    // Keep the existing inexpensive conservative success path.
                    // Only its rejected region may request exact source faces;
                    // absent data preserves the original refusal unchanged.
                    if(CatJumpWeightedBody.TryClearRegion(actor,entry,sample,regionIndex,sourceMatrix,correction,out bool exactClear)&&exactClear)continue;
                    clear=false;break;
                }
                if (clear) continue;
                if (preparation && !preparedGroundLaunch &&
                    CatJumpSupportedPreparation.TryClear(actor,entry,sample,sourceMatrix,correction,from,heading,out bool supportedClear) && supportedClear) continue;
                rejection = new Rejection { phase = sample.phase, root = root, heading = heading }; return false;
            }
            // Backward compatibility: partial or absent surface data does not
            // bypass the previously installed four-body/head-refinement gate.
            bool refined = sample.headCoverageVersion == CatJumpClearanceCatalog.HeadCoverageVersion &&
                sample.headTriangleCount > 0 && sample.headProbes != null && sample.headProbes.Length > 0 &&
                sample.headProbes.Length <= CatJumpClearanceCatalog.MaxHeadProbes;
            if (!refined)
            {
                // Missing/stale refinement never turns an old rejection into acceptance.
                if (actor.IsInteractionBodyClear(probes,BodyTolerance)) continue;
            }
            else
            {
                for (int i = 0; i < trunk.Length; i++) trunk[i] = probes[i];
                if (actor.IsInteractionBodyClear(trunk,BodyTolerance))
                {
                    one[0] = Transform(sample.headEnvelope, sourceMatrix, correction, scale);
                    bool headClear = actor.IsInteractionBodyClear(one,BodyTolerance);
                    if (!headClear)
                    {
                        headClear = true;
                        foreach (var part in sample.headProbes)
                        {
                            one[0] = Transform(part, sourceMatrix, correction, scale);
                            if (actor.IsInteractionBodyClear(one,BodyTolerance)) continue;
                            headClear = false; break;
                        }
                    }
                    if (headClear) continue;
                }
            }
            if (preparation && !preparedGroundLaunch &&
                CatJumpSupportedPreparation.TryClear(actor,entry,sample,sourceMatrix,correction,from,heading,out bool supportedFallback) && supportedFallback) continue;
            rejection = new Rejection { phase = sample.phase, root = root, heading = heading }; return false;
        }
        return true;
    }
    static bool HasBodyCoverage(CatJumpClearanceCatalog.Sample sample)
    {
        if (sample.bodyCoverageVersion != CatJumpClearanceCatalog.BodyCoverageVersion ||
            sample.bodyCoverage == null || sample.bodyCoverage.Length != 4) return false;
        for (int i = 0; i < 4; i++)
        {
            var region = sample.bodyCoverage[i];
            if (region == null || region.region != sample.probes[i].region || region.triangleCount <= 0 ||
                region.parts == null || region.parts.Length == 0 || region.parts.Length > CatJumpClearanceCatalog.MaxRegionProbes) return false;
        }
        return true;
    }
    static bool RegionClear(CatMovement actor, CatJumpClearanceCatalog.RegionCoverage region,
        Matrix4x4 matrix, Vector3 correction, float scale)
    {
        // The full-triangle envelope is a cheap safe accept. Only an overlap
        // consults the finer capsules; every triangle belongs wholly to a part.
        one[0] = Transform(region.envelope, matrix, correction, scale);
        if (actor.IsInteractionBodyClear(one,BodyTolerance)) return true;
        foreach (var part in region.parts)
        {
            one[0] = Transform(part, matrix, correction, scale);
            if (!actor.IsInteractionBodyClear(one,BodyTolerance)) return false;
        }
        return true;
    }
    static CatBodyGuardCatalog.Probe Transform(CatBodyGuardCatalog.Probe p, Matrix4x4 matrix, Vector3 correction, float scale)
    {
        p.start = matrix.MultiplyPoint3x4(p.start) + correction;
        p.end = matrix.MultiplyPoint3x4(p.end) + correction; p.radius *= scale;
        return p;
    }
}
