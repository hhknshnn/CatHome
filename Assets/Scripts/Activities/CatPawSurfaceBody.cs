using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class CatPawReachResolver
{
    sealed class SurfaceBodyGeometry
    {
        public CatPawReachCatalog.BodyRegion[] regions;
        public int[][] allVertices;
        public Vector3[][] points;
        public CatBodyGuardBox[] fixedBoxes;
        public SurfaceGeometry owner;
        public CatMovement actor;
        public CatPawReachPlan plan;
        public bool fixedSource;
        public int filled=-1,checkedSample=-1;
        public readonly CatBodyGuardBox[] phaseScratch=new CatBodyGuardBox[4];
        public Func<int,Collider,bool> refinement;
        public bool Refine(int index,Collider solid)
        {
            int sample=checkedSample>=0?checkedSample:fixedSource?(index/4==0||owner.entry.pose==CatActivityPose.Paw?0:owner.all.Length-1):index/4,region=index%4;
            if(filled!=sample)
            {
                if(!FillSurfaceBodyPoints(owner,actor,plan,sample,fixedSource))return false;
                filled=sample;
            }
            var vertices=points[region];var bounds=solid.bounds;
            foreach(var part in regions[region].parts)
            {
                // This AABB is rejection only. All three original corners of
                // every owned face stay in the part; overlap uses full faces.
                var box=new Bounds(vertices[part.vertices[0]],Vector3.zero);
                foreach(int vertex in part.vertices)box.Encapsulate(vertices[vertex]);
                box.Expand(.004f); // same 2 mm world shell as the original body capsules
                if(!box.Intersects(bounds))continue;
                for(int t=0;t<part.triangles.Length;t+=3)
                    if(!actor.IsInteractionTriangleClear(vertices[part.triangles[t]],vertices[part.triangles[t+1]],
                        vertices[part.triangles[t+2]],solid,.015f,.002f))return false;
            }
            return true;
        }
    }

    static SurfaceBodyGeometry BuildSurfaceBodyGeometry(CatPawReachCatalog.Entry entry)
    {
        if(entry.pose!=CatActivityPose.Paw||entry.bodySurfaceVersion==0)return null;
        if(entry.bodySurfaceVersion!=CatPawReachCatalog.BodySurfaceVersion||entry.bodySurface==null||entry.bodySurface.Length!=4)return null;
        string[] names={"pelvis","chest","neck","head"};
        var result=new SurfaceBodyGeometry{regions=entry.bodySurface,points=new Vector3[4][],allVertices=new int[4][]};
        for(int r=0;r<4;r++)
        {
            var region=entry.bodySurface[r];
            if(region==null||region.region!=names[r]||region.skin==null||region.skin.Length<3||region.triangles==null||
                region.triangles.Length==0||region.triangles.Length%3!=0||region.parts==null||region.parts.Length==0||region.parts.Length>32)return null;
            var covered=new HashSet<(int,int,int)>();
            foreach(var part in region.parts)
            {
                if(part?.vertices==null||part.vertices.Length==0||part.triangles==null||part.triangles.Length==0||part.triangles.Length%3!=0)return null;
                var indices=new HashSet<int>(part.vertices);
                foreach(int v in indices)if(v<0||v>=region.skin.Length)return null;
                for(int t=0;t<part.triangles.Length;t+=3)
                {
                    int a=part.triangles[t],b=part.triangles[t+1],c=part.triangles[t+2];
                    if(!indices.Contains(a)||!indices.Contains(b)||!indices.Contains(c))return null;
                    covered.Add((a,b,c));
                }
            }
            for(int t=0;t<region.triangles.Length;t+=3)
                if(!covered.Contains((region.triangles[t],region.triangles[t+1],region.triangles[t+2])))return null;
            result.points[r]=new Vector3[region.skin.Length];result.allVertices[r]=new int[region.skin.Length];
            for(int v=0;v<region.skin.Length;v++)result.allVertices[r][v]=v;
        }
        result.refinement=result.Refine;return result;
    }
    static CatBodyGuardBox SurfaceBodyBox(SurfaceBodyGeometry body,int region,Vector3 normal)
    {
        var box=CatPawSurfaceRegions.FitBox(body.points[region],body.allVertices[region],normal);
        box.halfExtents+=Vector3.one*.001f;return box;
    }
    static bool FillSurfaceBodyPoints(SurfaceGeometry geometry,CatMovement actor,CatPawReachPlan plan,int sample,bool fixedSource)
    {
        if(!fixedSource)return FillSurfacePawPoints(geometry,plan,actor.transform.right,sample,false,true);
        var frame=geometry.all[sample];
        for(int r=0;r<4;r++)CatPawWeightedSkin.Fill(frame.bodySkin[r],frame.pivot,Quaternion.identity,false,true,
            default,default,default,geometry.bodySurface.points[r]);
        return true;
    }
    static bool SurfaceBodyBoxesClear(CatMovement actor,SurfaceGeometry geometry,CatPawReachPlan plan,
        CatBodyGuardBox[] boxes,bool fixedSource,int checkedSample=-1)
    {
        var body=geometry.bodySurface;body.owner=geometry;body.actor=actor;body.plan=plan;body.fixedSource=fixedSource;body.filled=-1;body.checkedSample=checkedSample;
        return actor.IsInteractionBoxesClear(boxes,.015f,body.refinement)&&
            (plan.Surface==null||plan.Surface.TargetBoxesClear(boxes,.015f));
    }
    static bool SurfaceFixedBodyClear(CatMovement actor,SurfaceGeometry geometry)
    {
        if(geometry.bodySurface==null)
        {
            if(!SurfaceReturnsToStart(geometry.entry.pose))return FixedTrajectoryClear(actor,geometry.body);
            // A reversible gesture visits its prefix and returns over that
            // exact path; an unused forward-stepping tail is not played.
            float end=geometry.all[geometry.all.Length-1].sample.phase;
            foreach(var sample in geometry.body.all)
            {
                if(sample.phase>end+.00001f)break;
                singleProbe[0]=sample.pelvis;
                if(!actor.IsInteractionBodyClear(singleProbe))return false;
                if(sample.phase<=.001f&&!UpperBodyClear(actor,sample,Quaternion.identity))return false;
            }
            return true;
        }
        var body=geometry.bodySurface;
        if(body.fixedBoxes==null)
        {
            body.fixedBoxes=new CatBodyGuardBox[8];
            for(int end=0;end<2;end++)
            {
                if(!FillSurfaceBodyPoints(geometry,actor,default,end==0||geometry.entry.pose==CatActivityPose.Paw?0:geometry.all.Length-1,true)){body.fixedBoxes=null;return false;}
                for(int r=0;r<4;r++)body.fixedBoxes[end*4+r]=SurfaceBodyBox(body,r,actor.transform.up);
            }
        }
        return SurfaceBodyBoxesClear(actor,geometry,default,body.fixedBoxes,true);
    }
    static int SurfaceBodySampleAt(SurfaceGeometry geometry,float contactPhase,int cursor)
    {
        // Peak contact first; then every other source phase in its original
        // order. This is only an evaluation order, not a resampled motion.
        int contact=-1;
        for(int i=0;i<geometry.all.Length;i++)
            if(Mathf.Abs(geometry.all[i].sample.phase-contactPhase)<.00001f){contact=i;break;}
        if(contact<0)return cursor;
        if(cursor==0)return contact;
        int sample=cursor-1;return sample>=contact?sample+1:sample;
    }
    static int ContinueSurfaceBodyCandidate(CatMovement actor,SurfaceGeometry geometry,SurfaceCandidate candidate)
    {
        if(candidate.bodyBoxes==null)
        {candidate.bodyBoxes=new CatBodyGuardBox[SurfacePathCount(geometry,candidate.plan)*4];candidate.bodyBuildCursor=0;}
        while(candidate.bodyBuildCursor<SurfacePathCount(geometry,candidate.plan))
        {
            if(!SurfaceTimeAvailable())return 0;
            int sample=SurfaceBodySampleAt(geometry,candidate.plan.SourcePhase,candidate.bodyBuildCursor);
            long reachTime=BeginSurfaceMeasure();
            bool reachable=FillSurfaceBodyPoints(geometry,actor,candidate.plan,sample,false);
            EndSurfaceMeasure(reachTime,"body-reach",sample,reachable);
            if(!reachable)return -1;
            for(int r=0;r<4;r++)candidate.bodyBoxes[sample*4+r]=SurfaceBodyBox(geometry.bodySurface,r,actor.transform.up);
            Array.Copy(candidate.bodyBoxes,sample*4,geometry.bodySurface.phaseScratch,0,4);
            long sampleTime=BeginSurfaceMeasure();
            bool sampleClear=SurfaceBodyBoxesClear(actor,geometry,candidate.plan,geometry.bodySurface.phaseScratch,false,sample);
            EndSurfaceMeasure(sampleTime,"body-phase",sample,sampleClear);
            if(!sampleClear)return -1;
            candidate.bodyBuildCursor++;
        }
        // These yielded phase checks are early rejection only. Repeating the
        // whole body here is redundant: ContinueSurfaceCandidate always runs
        // SurfaceCandidateClear with CURRENT full-body/full-arm physics before
        // returning a plan, including a previously accepted warm plan.
        return 1;
    }
    static bool SurfaceCandidateBodyClear(CatMovement actor,SurfaceGeometry geometry,SurfaceCandidate candidate)
    {
        if(geometry.bodySurface==null)return actor.IsInteractionBodyClear(SurfaceUpper(geometry,candidate,actor.transform.right));
        if(candidate.bodyBoxes==null||candidate.bodyBuildCursor<SurfacePathCount(geometry,candidate.plan))return false;
        return SurfaceBodyBoxesClear(actor,geometry,candidate.plan,candidate.bodyBoxes,false);
    }

    /// <summary>Pure weighted body output for source-vs-rendered QA, including
    /// pelvis/chest blend seams and IK-influenced full boundary faces.</summary>
    public static bool TryPredictSurfaceBodySample(CatMovement actor,CatPawReachPlan reach,CatPawReachCatalog.Sample sample,out Vector3[][] regions)
    {
        regions=null;var tag=actor!=null?actor.GetComponentInChildren<CatBreedVisualTag>():null;
        var entry=reach.Surface?.Entry;
        if(reach.Surface!=null&&reach.Surface.ReturnToStart&&sample.phase>reach.SourcePhase+.00001f)return false;
        if(tag==null||entry?.bodySurfaceVersion!=CatPawReachCatalog.BodySurfaceVersion||entry.bodySurface.Length!=4||!reach.Surface.IsValid)return false;
        var matrix=tag.transform.localToWorldMatrix;
        float envelope=Mathf.Clamp01(sample.phase<=reach.SourcePhase?sample.phase/reach.SourcePhase:(1-sample.phase)/(1-reach.SourcePhase));
        Quaternion bend=Quaternion.AngleAxis(reach.ChestYaw*envelope,Vector3.up)*Quaternion.AngleAxis(reach.ChestPitch*envelope,actor.transform.right);
        Vector3 pivot=World(matrix,sample.torsoPivot);var arm=reach.Left?sample.left:sample.right;
        Vector3 upper=pivot+bend*(World(matrix,arm.upper)-pivot),fore=pivot+bend*(World(matrix,arm.fore)-pivot),hand=pivot+bend*(World(matrix,arm.hand)-pivot),endpoint=Vector3.zero;
        var old=reach.Left?sample.leftPaw:sample.rightPaw;
        foreach(int index in reach.Surface.Vertices)endpoint+=pivot+bend*(World(matrix,old[index])-pivot);endpoint/=reach.Surface.Vertices.Length;
        var solve=CatPawSurfaceCcd.Solve(upper,fore,hand,endpoint,reach.Surface.Point,envelope,reach.Surface.ApproachNormal,reach.Surface.ApproachLift,reach.Surface.LimitLiftToRemainingRise);
        if(Vector3.Distance(solve.Endpoint,CatPawSurfaceCcd.Goal(endpoint,reach.Surface.Point,envelope,reach.Surface.ApproachNormal,reach.Surface.ApproachLift,reach.Surface.LimitLiftToRemainingRise))>.012f)return false;
        regions=new Vector3[4][];
        for(int r=0;r<4;r++)
        {
            var definitions=entry.bodySurface[r].skin;var frame=CatPawWeightedSkin.Build(entry,sample,matrix,definitions);if(frame==null)return false;
            regions[r]=new Vector3[definitions.Length];CatPawWeightedSkin.Fill(frame,pivot,bend,true,reach.Left,solve,upper,fore,regions[r]);
        }
        return true;
    }
}
