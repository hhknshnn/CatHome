#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

// Bounded measurement, not a forced activity or a production readiness override.
// Negative pitch lifts the head, within the existing32-degree absolute bend.
// The current production motion clamps negative pitch to zero; this diagnostic
// must establish source/skin evidence before any opt-in runtime adaptation.
public sealed class KnockHeadUpSourceSkinTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
    static readonly string[] Regions={"pelvis","chest","neck","head"};
    static readonly Vector3[] Directions={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,
        new Vector3(.019f,.023f,1).normalized,-new Vector3(.013f,1,.027f).normalized,
        -new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
    PreparedInteractionStartTests fixture;
    GameObject sourceVisual;
    Mesh baked;
    CatToyContactMotion contact;
    readonly QaMeshTopologyCache topology=new QaMeshTopologyCache();
    QaExactMeshContact metric;
    readonly List<string> plans=new List<string>(),rows=new List<string>(),summaries=new List<string>();
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");
    static object Field(object o,string name)=>o.GetType().GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Private).Invoke(o,args);
    static T Read<T>(object o,string name)=>(T)o.GetType().GetField(name,Private).GetValue(o);
    static string Csv(params object[] values)=>string.Join(",",values.Select(v=>"\""+(v is Vector3 p?FormattableString.Invariant($"{p.x:R};{p.y:R};{p.z:R}"):
        v is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):v?.ToString()??"").Replace("\"","\"\"")+"\""));
    sealed class Candidate
    {
        public int id,sign;public Vector3 position,target;public Quaternion heading;public Matrix4x4 matrix;
        public bool left,fixedClear,currentGate;public float phase,pitch,yaw,margin,maxCapsule;
    }
    sealed class Maximum
    {public int inside,ambiguous,vertex=-1,votes;public float depth;public Vector3 point;public QaExactMeshContact.Hit exact;}
    [SetUp]public void Before()
    {
        fixture=new PreparedInteractionStartTests();fixture.Before();metric=new QaExactMeshContact(topology);baked=new Mesh();
        plans.Add("candidate,root,heading,target,left,sign,contactPhase,pitch,chestYaw,mathMargin,fixedClear,sourceEnvelopeClear,maxTableCapsuleDepth,selectedForSkin");
        rows.Add("candidate,sign,contactPhase,pitch,chestYaw,sourcePhase,region,inside,ambiguous,maxExactDepth,vertex,votes,point,closestSurface,triangle,submesh,material,pawDistance,boneLengthError,rearShift");
        summaries.Add("candidate,root,heading,target,left,sign,contactPhase,pitch,chestYaw,mathMargin,sourceEnvelopeClear,maxTableCapsuleDepth,maxBodyExactDepth,maxAllSkinExactDepth,contactPawDistance,boneLengthError,rearShift,actualGeometryUsable,scope");
    }
    [TearDown]public void After()
    {
        if(sourceVisual!=null)Object.DestroyImmediate(sourceVisual);if(baked!=null)Object.DestroyImmediate(baked);
        metric?.Clear();topology.Clear();Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output,"knock-headup-plans.csv"),plans);
        File.WriteAllLines(Path.Combine(Output,"knock-headup-source-skin.csv"),rows);
        File.WriteAllLines(Path.Combine(Output,"knock-headup-summary.csv"),summaries);
        fixture?.After();
    }
    [UnityTest,Timeout(240000)]public IEnumerator TwoLegalStances_HeadUpWithin32Degrees_MeasureFullSourceAndActualSkin()
    {
        yield return (IEnumerator)Call(fixture,"Prepare","Balcony_Level01");
        var cat=Read<CatMovement>(fixture,"cat");CatBreedService.Select("russian-blue");yield return QaBreedReadiness.WaitForSelected(cat,"russian-blue");
        var activity=CatActivity.Registered.OfType<KnockOffActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId==HomeStoreService.BalconySideTableId);
        Assert.That(activity.PerchPoint,Is.Null);
        var tag=cat.GetComponentInChildren<CatBreedVisualTag>();var catalog=CatBreedCatalog.Load();var breed=catalog.Find(tag.BreedId);
        var entry=CatPawReachCatalog.Load().Find(tag.BreedId,CatActivityPose.Paw);Assert.That(entry?.samples,Is.Not.Null);
        var builder=Type.GetType("CatPawReachBuilder, CatHome.Editor");Assert.That(builder,Is.Not.Null);
        var classify=builder.GetMethod("Region",Static);
        var clip=(AnimationClip)builder.GetMethod("StateClip",Static).Invoke(null,new object[]{catalog.GameplayController,entry.stateName});
        Assert.That(clip,Is.Not.Null);string path=UnityEditor.AssetDatabase.GetAssetPath(clip),hash=UnityEditor.AssetDatabase.GetAssetDependencyHash(path).ToString();
        var solid=activity.GetComponentsInChildren<MeshCollider>().Single(c=>c.enabled&&!c.isTrigger&&c.name=="BalconySideTable_PremiumModel");
        Assert.That(solid.convex,Is.False);Assert.That(solid.sharedMesh,Is.SameAs(solid.GetComponent<MeshFilter>().sharedMesh));
        var guard=Read<CatBodyGuard>(cat,"bodyGuard");var penetration=typeof(CatBodyGuard).GetMethod("Penetration",Private);
        var resolver=typeof(CatPawReachResolver);var transformSource=resolver.GetMethod("TransformSource",Static);
        var reachMargin=resolver.GetMethod("ReachMargin",Static);var fixedPath=resolver.GetMethod("FixedTrajectoryClear",Static);var trajectory=resolver.GetMethod("TrajectoryClear",Static);
        Assert.That(transformSource!=null&&reachMargin!=null&&fixedPath!=null&&trajectory!=null,Is.True);
        var targetMethod=typeof(KnockOffActivity).GetMethod("TryGroundedContact",Private);
        var reach=Read<Transform>(activity,"reachPoint");
        var liveBones=cat.GetComponentInChildren<Animator>().GetComponentsInChildren<Transform>(true);
        Vector3 liveRoot=cat.transform.position;Quaternion liveHeading=cat.transform.rotation;
        Matrix4x4 tagInRoot=cat.transform.worldToLocalMatrix*tag.transform.localToWorldMatrix;
        int[] ids={126,16};
        Vector3[] positions={new Vector3(-2.74492979f,.05f,-.3049999f),new Vector3(-3.01339769f,.05f,-.149999857f)};
        float[] headings={244.254883f,184.719849f};
        var selected=new List<Candidate>();int mathPlans=0;float deadline=Time.realtimeSinceStartup+195;
        for(int c=0;c<ids.Length;c++)
        {
            var p=positions[c];var q=Quaternion.Euler(0,headings[c],0);
            Assert.That(cat.IsInteractionPoseClear(p,q),Is.True,"Previously legal stance "+ids[c]);
            Assert.That(Vector3.ProjectOnPlane(p-reach.position,Vector3.up).magnitude,Is.LessThanOrEqualTo(.42001f));
            Assert.That(Vector3.Angle(q*Vector3.forward,Vector3.ProjectOnPlane(activity.GlassPivot.position-p,Vector3.up)),Is.LessThanOrEqualTo(45.001f));
            object[] targetArgs={p,q,Vector3.zero,false};Assert.That((bool)targetMethod.Invoke(activity,targetArgs),Is.True);
            Vector3 target=(Vector3)targetArgs[2];bool left=(bool)targetArgs[3];
            Matrix4x4 matrix=Matrix4x4.TRS(p,q,cat.transform.lossyScale)*tagInRoot;
            var source=transformSource.Invoke(null,new object[]{entry,matrix,tag.transform.lossyScale});
            bool fixedClear=(bool)fixedPath.Invoke(null,new[]{(object)cat,source});
            var contacts=(Array)Field(source,"contacts");var all=(Array)Field(source,"all");
            foreach(float pitch in new[]{-8f,-16f,-24f,-32f})
            {
                Candidate best=null;var candidates=new List<Candidate>();
                foreach(float signedYaw in new[]{-35f,-30f,-15f,0f,15f,30f,35f})
                foreach(object sample in contacts)
                {
                    Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Bounded source candidate measurement");
                    int sign=(int)Mathf.Sign(signedYaw);float yaw=Mathf.Abs(signedYaw);
                    float phase=(float)Field(sample,"phase");
                    Quaternion bend=Quaternion.AngleAxis(sign*yaw,Vector3.up)*Quaternion.AngleAxis(pitch,q*Vector3.right);
                    float margin=(float)reachMargin.Invoke(null,new[]{sample,(object)bend,Field(sample,left?"left":"right"),target});
                    if(margin<.004f)continue;
                    bool clear=fixedClear&&(bool)trajectory.Invoke(null,new object[]{cat,source,phase,pitch,sign*yaw,q*Vector3.right});
                    float depth=MaximumCapsuleDepth(all,phase,pitch,sign*yaw,q*Vector3.right,guard,solid,penetration);
                    var candidate=new Candidate{id=ids[c],sign=sign,position=p,heading=q,matrix=matrix,target=target,left=left,
                        phase=phase,pitch=pitch,yaw=sign*yaw,margin=margin,fixedClear=fixedClear,currentGate=clear,maxCapsule=depth};
                    candidates.Add(candidate);mathPlans++;
                    if(best==null||Better(candidate,best))best=candidate;
                }
                foreach(var candidate in candidates)plans.Add(Csv(candidate.id,p,headings[c],target,left,candidate.sign,candidate.phase,candidate.pitch,candidate.yaw,
                    candidate.margin,fixedClear,candidate.currentGate,candidate.maxCapsule,ReferenceEquals(best,candidate)));
                if(best!=null)selected.Add(best);
                else summaries.Add(Csv(ids[c],p,headings[c],target,left,0,"",pitch,"","","","","","","","","",false,"No mathematical candidate in bounded grid"));
                // One fixed-pitch trial per frame; source clip still spans0..1.
                yield return null;
            }
        }
        Assert.That(mathPlans,Is.GreaterThan(0));Assert.That(selected.Count,Is.GreaterThanOrEqualTo(2));
        sourceVisual=CatBreedVisualFactory.Create(breed,catalog.GameplayController,null,"QA remote measured chest yaw");
        sourceVisual.transform.position=new Vector3(0,-100,0);
        foreach(var renderer in sourceVisual.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
        Assert.That(sourceVisual.GetComponentInChildren<CatMovement>(),Is.Null);
        var animator=sourceVisual.GetComponentInChildren<Animator>();animator.enabled=false;
        contact=sourceVisual.AddComponent<CatToyContactMotion>();contact.enabled=false;
        var solve=typeof(CatToyContactMotion).GetMethod("LateUpdate",Private);
        var skin=sourceVisual.GetComponentInChildren<SkinnedMeshRenderer>();var transforms=sourceVisual.GetComponentsInChildren<Transform>(true);
        var localPositions=transforms.Select(t=>t.localPosition).ToArray();var localRotations=transforms.Select(t=>t.localRotation).ToArray();var localScales=transforms.Select(t=>t.localScale).ToArray();
        Vector3 basePosition=animator.transform.localPosition,baseScale=animator.transform.localScale;Quaternion baseRotation=animator.transform.localRotation;
        var weights=skin.sharedMesh.boneWeights;var groups=Enumerable.Repeat(-1,weights.Length).ToArray();
        foreach(int i in breed.ContactVertexIndices)groups[i]=(int)classify.Invoke(null,new object[]{weights[i],skin.bones});
        var chest=CatBreedVisualFactory.FindDescendant(sourceVisual.transform,"DEF-spine.001");
        var hips=CatBreedVisualFactory.FindDescendant(sourceVisual.transform,"DEF-spine");
        var rearL=CatBreedVisualFactory.FindDescendant(sourceVisual.transform,"DEF-foot.L");var rearR=CatBreedVisualFactory.FindDescendant(sourceVisual.transform,"DEF-foot.R");
        Assert.That(chest!=null&&hips!=null&&rearL!=null&&rearR!=null,Is.True);
        foreach(var candidate in selected)
        {
            string side=candidate.left?"L":"R";
            var upper=CatBreedVisualFactory.FindDescendant(sourceVisual.transform,"DEF-upper_arm."+side);
            var fore=CatBreedVisualFactory.FindDescendant(sourceVisual.transform,"DEF-forearm."+side);
            var hand=CatBreedVisualFactory.FindDescendant(sourceVisual.transform,"DEF-hand."+side);
            float allDepth=0,bodyDepth=0,contactDistance=float.PositiveInfinity,maxBoneError=0,maxRear=0;
            foreach(var sample in entry.samples.OrderBy(s=>s.phase))
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Bounded bent-source skin measurement");
                contact.Clear();
                var livePos=liveBones.Select(t=>t.localPosition).ToArray();var liveRot=liveBones.Select(t=>t.localRotation).ToArray();
                for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=localPositions[i];transforms[i].localRotation=localRotations[i];transforms[i].localScale=localScales[i];}
                clip.SampleAnimation(animator.gameObject,(clip.isLooping?Mathf.Repeat(sample.phase,1):sample.phase)*clip.length);
                animator.transform.localPosition=basePosition;animator.transform.localRotation=baseRotation;animator.transform.localScale=baseScale;
                skin.BakeMesh(baked,true);var unbent=baked.vertices;float minY=float.PositiveInfinity;
                foreach(int v in breed.ContactVertexIndices)minY=Mathf.Min(minY,sourceVisual.transform.InverseTransformPoint(skin.transform.TransformPoint(unbent[v])).y);
                Vector3 lift=Vector3.up*CatPawReachCatalog.GroundClearance;
                Vector3 World(Transform bone)=>candidate.matrix.MultiplyPoint3x4(sourceVisual.transform.InverseTransformPoint(bone.position)-Vector3.up*minY)+lift;
                var chain=candidate.left?sample.left:sample.right;
                Assert.That(Vector3.Distance(World(upper),candidate.matrix.MultiplyPoint3x4(chain.upper)+lift),Is.LessThan(.0002f));
                Assert.That(Vector3.Distance(World(fore),candidate.matrix.MultiplyPoint3x4(chain.fore)+lift),Is.LessThan(.0002f));
                Assert.That(Vector3.Distance(World(hand),candidate.matrix.MultiplyPoint3x4(chain.hand)+lift),Is.LessThan(.0002f));
                float upperLength=Vector3.Distance(World(upper),World(fore)),foreLength=Vector3.Distance(World(fore),World(hand));
                Vector3 hipBefore=World(hips),leftBefore=World(rearL),rightBefore=World(rearR);
                float envelope=Mathf.Clamp01(sample.phase<=candidate.phase?sample.phase/candidate.phase:(1-sample.phase)/(1-candidate.phase));
                Quaternion worldBend=Quaternion.AngleAxis(candidate.yaw*envelope,Vector3.up)*Quaternion.AngleAxis(candidate.pitch*envelope,candidate.heading*Vector3.right);
                Quaternion localBend=Quaternion.Inverse(candidate.matrix.rotation)*worldBend*candidate.matrix.rotation;
                chest.rotation=sourceVisual.transform.rotation*localBend*Quaternion.Inverse(sourceVisual.transform.rotation)*chest.rotation;
                Vector3 remoteTarget=sourceVisual.transform.TransformPoint(candidate.matrix.inverse.MultiplyPoint3x4(candidate.target-lift)+Vector3.up*minY);
                // Invoke the existing sixteen-iteration wrist solver, exactly as
                // CatPawReachMotion does. Do not replace it with a test-only IK.
                contact.Reach(remoteTarget,candidate.left,.42f*envelope,16);solve.Invoke(contact,null);
                float distance=Vector3.Distance(World(hand),candidate.target);
                if(Mathf.Abs(sample.phase-candidate.phase)<.0001f)contactDistance=distance;
                float boneError=Mathf.Max(Mathf.Abs(Vector3.Distance(World(upper),World(fore))-upperLength),Mathf.Abs(Vector3.Distance(World(fore),World(hand))-foreLength));
                float rear=Mathf.Max(Vector3.Distance(hipBefore,World(hips)),Mathf.Max(Vector3.Distance(leftBefore,World(rearL)),Vector3.Distance(rightBefore,World(rearR))));
                maxBoneError=Mathf.Max(maxBoneError,boneError);maxRear=Mathf.Max(maxRear,rear);
                Assert.That(boneError,Is.LessThan(.0001f));Assert.That(rear,Is.LessThan(.0001f));
                skin.BakeMesh(baked,true);var vertices=baked.vertices;
                var maxima=new[]{new Maximum(),new Maximum(),new Maximum(),new Maximum(),new Maximum()};
                foreach(int v in breed.ContactVertexIndices)
                {
                    Vector3 point=candidate.matrix.MultiplyPoint3x4(sourceVisual.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[v]))-Vector3.up*minY)+lift;
                    if(!solid.bounds.Contains(point))continue;
                    int votes=Votes(solid,point);int group=groups[v];
                    if(votes>0&&votes<4){maxima[0].ambiguous++;if(group>=0)maxima[group+1].ambiguous++;}
                    if(votes<4)continue;var exact=metric.Measure(solid.sharedMesh,solid.transform,point);
                    Include(maxima[0],v,point,votes,exact);if(group>=0)Include(maxima[group+1],v,point,votes,exact);
                }
                allDepth=Mathf.Max(allDepth,maxima[0].depth);
                for(int r=1;r<5;r++)bodyDepth=Mathf.Max(bodyDepth,maxima[r].depth);
                for(int r=0;r<5;r++)
                {
                    var result=maxima[r];string material="";var materials=solid.GetComponent<Renderer>().sharedMaterials;
                    if(result.vertex>=0&&result.exact.submesh>=0&&result.exact.submesh<materials.Length)material=materials[result.exact.submesh].name;
                    rows.Add(Csv(candidate.id,candidate.sign,candidate.phase,candidate.pitch,candidate.yaw,sample.phase,r==0?"all-skin":Regions[r-1],
                        result.inside,result.ambiguous,result.depth,result.vertex,result.votes,result.point,result.exact.point,result.exact.triangle,result.exact.submesh,material,distance,boneError,rear));
                }
                for(int i=0;i<liveBones.Length;i++){Assert.That(liveBones[i].localPosition,Is.EqualTo(livePos[i]));Assert.That(liveBones[i].localRotation,Is.EqualTo(liveRot[i]));}
                Assert.That(activity.IsRunning,Is.False);Assert.That(CatActivity.Active,Is.Null);
                contact.Clear();yield return null;
                Assert.That(Vector3.Distance(cat.transform.position,liveRoot),Is.LessThan(.002f));Assert.That(Quaternion.Angle(cat.transform.rotation,liveHeading),Is.LessThan(.001f));
            }
            bool usable=bodyDepth<=.015f&&allDepth<=.025f&&contactDistance<=.025f&&maxRear<.0001f&&maxBoneError<.0001f;
            summaries.Add(Csv(candidate.id,candidate.position,candidate.heading.eulerAngles.y,candidate.target,candidate.left,candidate.sign,candidate.phase,candidate.pitch,candidate.yaw,
                candidate.margin,candidate.currentGate,candidate.maxCapsule,bodyDepth,allDepth,contactDistance,maxBoneError,maxRear,usable,
                "Counterfactual head-up within32deg: production clamps negative pitch today; full source+existing IK; not a gameplay PASS"));
            File.WriteAllLines(Path.Combine(Output,"knock-headup-summary.csv"),summaries);
        }
        Assert.That(UnityEditor.AssetDatabase.GetAssetDependencyHash(path).ToString(),Is.EqualTo(hash));
        TestContext.WriteLine("Measured "+selected.Count+" signed candidates from "+mathPlans+" mathematical plans. Diagnostic only; see exact body/paw fields before any runtime selection change.");
    }
    static bool Better(Candidate a,Candidate b)
    {
        if(a.currentGate!=b.currentGate)return a.currentGate;
        if(Mathf.Abs(a.maxCapsule-b.maxCapsule)>.000001f)return a.maxCapsule<b.maxCapsule;
        float ac=Mathf.Abs(a.pitch)+Mathf.Abs(a.yaw)*.7f,bc=Mathf.Abs(b.pitch)+Mathf.Abs(b.yaw)*.7f;
        return ac<bc||Mathf.Approximately(ac,bc)&&a.margin>b.margin;
    }
    static float MaximumCapsuleDepth(Array all,float phase,float pitch,float yaw,Vector3 right,CatBodyGuard guard,MeshCollider solid,MethodInfo penetration)
    {
        float maximum=0;
        foreach(object sample in all)
        {
            float p=(float)Field(sample,"phase");float e=Mathf.Clamp01(p<=phase?p/phase:(1-p)/(1-phase));
            Quaternion bend=Quaternion.AngleAxis(yaw*e,Vector3.up)*Quaternion.AngleAxis(pitch*e,right);
            Vector3 pivot=(Vector3)Field(sample,"pivot");
            var probes=(CatBodyGuardCatalog.Probe[])Field(sample,"upper");
            foreach(var source in probes)
            {
                Vector3 start=pivot+bend*(source.start-pivot),end=pivot+bend*(source.end-pivot);
                object[] args={start,end,source.radius,solid,Vector3.zero,0f};
                if((bool)penetration.Invoke(guard,args))maximum=Mathf.Max(maximum,(float)args[5]);
            }
        }
        return maximum;
    }
    static void Include(Maximum value,int vertex,Vector3 point,int votes,QaExactMeshContact.Hit hit)
    {value.inside++;if(value.vertex>=0&&hit.distance<=value.depth)return;value.vertex=vertex;value.point=point;value.depth=hit.distance;value.votes=votes;value.exact=hit;}
    int Votes(MeshCollider collider,Vector3 point)
    {
        var data=topology.Get(collider.sharedMesh);bool previous=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;int votes=0;
        try{foreach(var direction in Directions)
        {
            if(!collider.Raycast(new Ray(point,direction),out var hit,5))continue;int t=hit.triangleIndex*3;
            if(t<0||t+2>=data.triangles.Length)continue;
            Vector3 a=collider.transform.TransformPoint(data.vertices[data.triangles[t]]),b=collider.transform.TransformPoint(data.vertices[data.triangles[t+1]]),c=collider.transform.TransformPoint(data.vertices[data.triangles[t+2]]);
            if(Vector3.Dot(Vector3.Cross(b-a,c-a).normalized,direction)>0)votes++;
        }}finally{Physics.queriesHitBackfaces=previous;}return votes;
    }
}
#endif
