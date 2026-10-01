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

public sealed class KnockSurfaceContactTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    PreparedInteractionStartTests fixture;CatMovement cat;KnockOffActivity activity;CatPawReachPlan plan;
    CatToyContactMotion contact;CatPawReachMotion motion;bool contactEnabled,motionEnabled;
    KnockSourcePoseWitness sourceWitness;
    readonly QaMeshTopologyCache topology=new QaMeshTopologyCache();QaExactMeshContact metric;Mesh skin;
    readonly List<string> rows=new List<string>();
    static readonly Vector3[] Rays={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,
        new Vector3(.019f,.023f,1).normalized,-new Vector3(.013f,1,.027f).normalized,-new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
    object Call(string name,params object[] args)=>typeof(PreparedInteractionStartTests).GetMethod(name,Private).Invoke(fixture,args);
    T Field<T>(string name)=>(T)typeof(PreparedInteractionStartTests).GetField(name,Private).GetValue(fixture);
    static string Csv(params object[] args)=>string.Join(",",args.Select(a=>"\""+(a is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):a?.ToString()??"").Replace("\"","\"\"")+"\""));
    [SetUp]public void Before(){fixture=new PreparedInteractionStartTests();fixture.Before();skin=new Mesh();metric=new QaExactMeshContact(topology);rows.Add("case,frame,running,strokes,actualPatchDistance,maximumArmDepth,rootShift,yaw,pose,pitch,chestYaw,phase,details");}
    [TearDown]public void After()
    {
        Time.timeScale=1;if(contact!=null)contact.enabled=contactEnabled;if(motion!=null)motion.enabled=motionEnabled;
        if(sourceWitness!=null)Object.DestroyImmediate(sourceWitness);
        if(skin!=null)Object.DestroyImmediate(skin);metric?.Clear();topology.Clear();
        Directory.CreateDirectory("Docs/QA/INTERACTION_POLISH_2026-09-16");
        File.WriteAllLines("Docs/QA/INTERACTION_POLISH_2026-09-16/knock-mesh-surface-"+TestContext.CurrentContext.Test.Name+".csv",rows);
        fixture?.After();
    }
    IEnumerator Prepare()
    {
        yield return (IEnumerator)Call("Prepare","Balcony_Level01");cat=Field<CatMovement>("cat");
        CatBreedService.Select("russian-blue");yield return QaBreedReadiness.WaitForSelected(cat,"russian-blue");
        activity=CatActivity.Registered.OfType<KnockOffActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId==HomeStoreService.BalconySideTableId);
        Assert.That(activity.PerchPoint,Is.Null);Assert.That(activity.GlassPivot.GetComponentsInChildren<Collider>().Length,Is.Zero,"Do not add a glass collider to make this test pass");
        Vector3 first=new Vector3(-2.74492979f,.05f,-.3049999f),second=new Vector3(-3.01339769f,.05f,-.149999857f);
        Vector3 firstBack=-(Quaternion.Euler(0,244.254883f,0)*Vector3.forward),secondBack=-(Quaternion.Euler(0,184.719849f,0)*Vector3.forward);
        Vector3[] nearbyPoints={first+firstBack*.04f,first+firstBack*.08f,second+secondBack*.04f,second+secondBack*.08f};
        float[] nearbyYaws={244.254883f,244.254883f,184.719849f,184.719849f};
        // Keep the original physically legal positions first. Nearby candidates
        // are additional player choices, never a runtime alignment or bypass.
        Vector3[] points={first,second,nearbyPoints[0],nearbyPoints[1],nearbyPoints[2],nearbyPoints[3]};
        float[] yaws={244.254883f,184.719849f,nearbyYaws[0],nearbyYaws[1],nearbyYaws[2],nearbyYaws[3]};bool found=false;
        for(int i=0;i<points.Length;i++)
        {
            var cc=cat.GetComponent<CharacterController>();cc.enabled=false;cat.transform.SetPositionAndRotation(points[i],Quaternion.Euler(0,yaws[i],0));cc.enabled=true;Physics.SyncTransforms();
            if(!cat.IsInteractionPoseClear(points[i],cat.transform.rotation)){rows.Add(Csv("physical-refusal",i));continue;}
            yield return null;float deadline=Time.realtimeSinceStartup+8;bool ready=activity.TryGetStartPose(cat,out var prepared);
            while(!ready&&activity.IsStartSearchPending&&Time.realtimeSinceStartup<deadline)
            {yield return null;ready=activity.TryGetStartPose(cat,out prepared);}
            rows.Add(Csv("preflight",i,false,0,0,0,0,0,"Paw",0,0,0,"ready="+ready+";pending="+activity.IsStartSearchPending+";math="+CatPawReachResolver.SurfaceMathCandidatesLastQuery+";physics="+CatPawReachResolver.SurfacePhysicsCandidatesLastQuery));
            if(ready){plan=prepared.PawPlan;found=true;break;}
        }
        Assert.That(found,Is.True,"At least one legal stance must offer a fully checked true-mesh Paw path; no forced start or body-only exemption");
        Assert.That(plan.Surface?.IsValid,Is.True);Assert.That(plan.Surface.ReturnToStart,Is.True);
        Assert.That(plan.SourcePhase,Is.InRange(.15f,.25f));Assert.That(plan.Surface.MeshHit.IsValid,Is.True);Assert.That(plan.Surface.Collider,Is.Null);
        Assert.That(activity.TryGetPromptDistance(cat,out _),Is.True);RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(activity.TryStart(cat),Is.True,"Prompt/click must agree with current physics");
        contact=cat.GetComponent<CatToyContactMotion>();motion=cat.GetComponent<CatPawReachMotion>();contactEnabled=contact.enabled;motionEnabled=motion.enabled;
    }
    [UnityTest,Timeout(180000)]public IEnumerator SideTable_RealMeshPaw_CompletesWithClearFullArmsAndUnchangedRoot()
    {
        yield return Prepare();Vector3 start=cat.transform.position;Quaternion heading=cat.transform.rotation;
        var animator=cat.GetComponentInChildren<Animator>();var binding=new CatPawSurfaceBinding(plan.Surface,animator.transform);
        var renderer=cat.GetComponentInChildren<SkinnedMeshRenderer>();
        string[] names={"DEF-upper_arm.L","DEF-forearm.L","DEF-hand.L","DEF-upper_arm.R","DEF-forearm.R","DEF-hand.R"};
        var joints=names.Select(n=>CatBreedVisualFactory.FindDescendant(cat.transform,n)).ToArray();
        Assert.That(joints.All(j=>j!=null),Is.True);
        sourceWitness=cat.gameObject.AddComponent<KnockSourcePoseWitness>();
        sourceWitness.Bind(joints,animator.transform.GetComponentsInChildren<Transform>(true),cat.GetComponent<CatActivityAnimation>());
        float[] links={Vector3.Distance(joints[0].position,joints[1].position),Vector3.Distance(joints[1].position,joints[2].position),
            Vector3.Distance(joints[3].position,joints[4].position),Vector3.Distance(joints[4].position,joints[5].position)};
        int[] armVertices=plan.Surface.Entry.leftArmSkin.Concat(plan.Surface.Entry.rightArmSkin).Select(v=>v.vertexIndex).Distinct().ToArray();
        var solids=Object.FindObjectsByType<MeshCollider>().Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.scene==cat.gameObject.scene&&c.GetComponentInParent<CatMovement>()==null).ToArray();
        var targets=activity.GlassPivot.GetComponentsInChildren<MeshFilter>();
        Assert.That(plan.Surface.Entry.bodySurfaceVersion,Is.EqualTo(CatPawReachCatalog.BodySurfaceVersion));
        int[] bodyVertices=plan.Surface.Entry.bodySurface.SelectMany(r=>r.skin).Select(v=>v.vertexIndex).Distinct().ToArray();
        bool sourceRetreated=false;float previousSource=-1f,maximumSource=0f;
        int completed=0,frames=0,contacts=0;float maximumArm=0,maximumBody=0,maximumBind=0,nearest=float.PositiveInfinity;
        Action<CatActivity> onDone=a=>{if(a==activity)completed++;};CatActivity.Completed+=onDone;
        try
        {
            float deadline=Time.realtimeSinceStartup+25;
            while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
            {
                yield return new WaitForEndOfFrame();renderer.BakeMesh(skin,true);var vertices=skin.vertices;
                var sourceDriver=cat.GetComponent<CatActivityAnimation>();
                if(sourceDriver.CurrentPose==CatActivityPose.Paw)
                {
                    float currentSource=sourceDriver.NativeJumpPhase;
                    Assert.That(currentSource,Is.InRange(0f,plan.SourcePhase+.00001f),"The played source must stay inside the fully checked short path");
                    maximumSource=Mathf.Max(maximumSource,currentSource);
                    sourceRetreated|=previousSource>.04f&&currentSource<previousSource-.001f;
                    previousSource=currentSource;
                }
                Vector3 patch=Vector3.zero;foreach(int index in plan.Surface.Vertices)patch+=renderer.transform.TransformPoint(vertices[plan.Surface.Definitions[index].vertexIndex]);patch/=plan.Surface.Vertices.Length;
                maximumBind=Mathf.Max(maximumBind,Vector3.Distance(patch,binding.Point()));
                float distance=Vector3.Distance(patch,plan.Surface.Point);nearest=Mathf.Min(nearest,distance);if(distance<.025f)contacts++;
                float frameDepth=0;string deepest="";
                foreach(int vertex in armVertices)
                {
                    Vector3 point=renderer.transform.TransformPoint(vertices[vertex]);
                    foreach(var solid in solids)
                    {
                        if(!solid.bounds.Contains(point))continue;
                        float depth=InsideDepth(solid.sharedMesh,solid.transform,point,solid);
                        if(depth>frameDepth){frameDepth=depth;deepest=solid.name+"/"+vertex;}
                    }
                    foreach(var target in targets)
                    {
                        if(!target.GetComponent<Renderer>().bounds.Contains(point))continue;
                        float depth=InsideDepth(target.sharedMesh,target.transform,point);
                        if(depth>frameDepth){frameDepth=depth;deepest=target.name+"/"+vertex;}
                    }
                }
                float bodyDepth=0;string bodySolid="";
                foreach(int vertex in bodyVertices)
                {
                    Vector3 point=renderer.transform.TransformPoint(vertices[vertex]);
                    foreach(var solid in solids)
                    {
                        if(!solid.bounds.Contains(point))continue;
                        float depth=InsideDepth(solid.sharedMesh,solid.transform,point,solid);
                        if(depth>bodyDepth){bodyDepth=depth;bodySolid=solid.name+"/"+vertex;}
                    }
                    foreach(var target in targets)
                    {
                        if(!target.GetComponent<Renderer>().bounds.Contains(point))continue;
                        float depth=InsideDepth(target.sharedMesh,target.transform,point);
                        if(depth>bodyDepth){bodyDepth=depth;bodySolid=target.name+"/"+vertex;}
                    }
                }
                maximumBody=Mathf.Max(maximumBody,bodyDepth);
                rows.Add(Csv("actual-body",frames,activity.IsRunning,activity.ContactStrokes,distance,bodyDepth,Vector3.Distance(start,cat.transform.position),Quaternion.Angle(heading,cat.transform.rotation),plan.Pose,plan.ChestPitch,plan.ChestYaw,plan.SourcePhase,bodySolid));
                maximumArm=Mathf.Max(maximumArm,frameDepth);
                rows.Add(Csv("actual",frames++,activity.IsRunning,activity.ContactStrokes,distance,frameDepth,Vector3.Distance(start,cat.transform.position),Quaternion.Angle(heading,cat.transform.rotation),plan.Pose,plan.ChestPitch,plan.ChestYaw,plan.SourcePhase,deepest));
                Assert.That(Vector3.Distance(start,cat.transform.position),Is.LessThan(.001f));Assert.That(Quaternion.Angle(heading,cat.transform.rotation),Is.LessThan(.2f));
                Assert.That(sourceWitness.Frame,Is.EqualTo(Time.frameCount),"Anatomy must compare the same rendered frame after source500 and before procedural550/600");
                float sourceDrift=0,addedLengthError=0;
                for(int limb=0;limb<2;limb++)for(int segment=0;segment<2;segment++)
                {
                    int at=limb*2+segment;
                    float actualLength=Vector3.Distance(joints[limb*3+segment].position,joints[limb*3+segment+1].position);
                    addedLengthError=Mathf.Max(addedLengthError,Mathf.Abs(actualLength-sourceWitness.Lengths[at]));
                    sourceDrift=Mathf.Max(sourceDrift,Mathf.Abs(sourceWitness.Lengths[at]-links[at]));
                }
                sourceWitness.LocalErrors(out float localPositionError,out float localScaleError);
                rows.Add(Csv("source-anatomy",Time.frameCount,activity.IsRunning,activity.ContactStrokes,
                    sourceWitness.SourcePhase,addedLengthError,sourceDrift,localPositionError,localScaleError,
                    sourceWitness.MaximumParentAxisRatio,plan.ChestYaw,plan.SourcePhase,
                    "same-frame linkError; source-vs-start drift; all-bone localPositionError/localScaleError; maximum parent axis ratio"));
                Assert.That(addedLengthError,Is.LessThan(.0001f),"Procedural motion must preserve this frame's original source link lengths");
                Assert.That(localPositionError,Is.LessThan(.0001f),"Procedural motion must not translate source bone joints");
                Assert.That(localScaleError,Is.LessThan(.0001f),"Procedural motion must not scale source bones");
                Assert.That(motion.RearSupportDisplacement,Is.LessThan(.0001f));
                yield return null;
            }
        }
        finally{CatActivity.Completed-=onDone;}
        Assert.That(completed,Is.EqualTo(1));Assert.That(activity.ContactStrokes,Is.EqualTo(activity.TeaseCount+1));
        Assert.That(sourceRetreated,Is.True,"Actual source time must retreat; a frozen contact pose is not a return");
        Assert.That(maximumSource,Is.EqualTo(plan.SourcePhase).Within(.001f));
        Assert.That(frames,Is.GreaterThan(15));Assert.That(contacts,Is.GreaterThan(0));Assert.That(nearest,Is.LessThan(.025f));
        Assert.That(maximumBody,Is.LessThanOrEqualTo(.015f),"All four rendered body regions and their complete seam faces must stay within the existing body tolerance");
        Assert.That(maximumBind,Is.LessThan(.0002f));Assert.That(maximumArm,Is.LessThanOrEqualTo(.002f),"Whole rendered arms must clear real table and collider-free prop triangles");
        Assert.That(cat.GetComponent<CharacterController>().enabled,Is.True);Assert.That(CatActivity.Active,Is.Null);
    }
    [UnityTest,Timeout(180000)]public IEnumerator SideTable_PoisonedCachedZero_CannotCreditAnUncorrectedLivePaw()
    {
        yield return Prepare();var binding=new CatPawSurfaceBinding(plan.Surface,cat.GetComponentInChildren<Animator>().transform);
        motion.Clear();contact.Clear();motion.enabled=false;contact.enabled=false;
        var cached=typeof(CatToyContactMotion).GetField("<Distance>k__BackingField",Private);Assert.That(cached,Is.Not.Null);
        int frames=0;float deadline=Time.realtimeSinceStartup+3;
        while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
        {
            cached.SetValue(contact,0f);yield return new WaitForEndOfFrame();float actual=Vector3.Distance(binding.Point(),plan.Surface.Point);
            rows.Add(Csv("poison",frames++,activity.IsRunning,activity.ContactStrokes,actual,0,0,0,plan.Pose,plan.ChestPitch,plan.ChestYaw,plan.SourcePhase,"IK disabled; stale cached Distance=0"));
            Assert.That(actual,Is.GreaterThan(.025f),"Negative witness is invalid if the uncorrected real source naturally touches");
            Assert.That(activity.ContactStrokes,Is.Zero);yield return null;
        }
        Assert.That(frames,Is.GreaterThan(5));Assert.That(activity.IsRunning,Is.False);Assert.That(activity.ContactStrokes,Is.Zero);
        Assert.That(activity.MinimumPawDistance,Is.GreaterThanOrEqualTo(.025f));
    }
    float InsideDepth(Mesh mesh,Transform transform,Vector3 point,MeshCollider collider=null)
    {
        var data=topology.Get(mesh);var matrix=transform.localToWorldMatrix;float winding=matrix.determinant<0?-1f:1f;int votes=0;
        if(collider!=null)
        {
            bool previous=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
            try
            {
                foreach(var direction in Rays)
                {
                    if(!collider.Raycast(new Ray(point,direction),out var hit,5))continue;int t=hit.triangleIndex*3;
                    if(t<0||t+2>=data.triangles.Length)continue;
                    Vector3 a=matrix.MultiplyPoint3x4(data.vertices[data.triangles[t]]),b=matrix.MultiplyPoint3x4(data.vertices[data.triangles[t+1]]),c=matrix.MultiplyPoint3x4(data.vertices[data.triangles[t+2]]);
                    if(Vector3.Dot(Vector3.Cross(b-a,c-a)*winding,direction)>0)votes++;
                }
            }
            finally{Physics.queriesHitBackfaces=previous;}
            return votes>=4?metric.Measure(mesh,transform,point).distance:0;
        }
        foreach(var direction in Rays)
        {
            float nearest=float.PositiveInfinity;bool exit=false;
            for(int i=0;i<data.triangles.Length;i+=3)
            {
                Vector3 a=matrix.MultiplyPoint3x4(data.vertices[data.triangles[i]]),b=matrix.MultiplyPoint3x4(data.vertices[data.triangles[i+1]]),c=matrix.MultiplyPoint3x4(data.vertices[data.triangles[i+2]]);
                Vector3 e1=b-a,e2=c-a,h=Vector3.Cross(direction,e2);float det=Vector3.Dot(e1,h);if(Mathf.Abs(det)<1e-10f)continue;
                Vector3 s=point-a;float u=Vector3.Dot(s,h)/det;if(u<0||u>1)continue;Vector3 q=Vector3.Cross(s,e1);float v=Vector3.Dot(direction,q)/det;
                if(v<0||u+v>1)continue;float t=Vector3.Dot(e2,q)/det;if(t<=.000001f||t>=nearest)continue;
                nearest=t;exit=Vector3.Dot(Vector3.Cross(e1,e2)*winding,direction)>0;
            }
            if(exit)votes++;
        }
        return votes>=4?metric.Measure(mesh,transform,point).distance:0;
    }
}

// Native source is finalized at500. Read it before chest550 and arm600 without
// restoring, resampling or changing any live animation or transform state.
[DefaultExecutionOrder(540)]
public sealed class KnockSourcePoseWitness : MonoBehaviour
{
    Transform[] joints,bones;Vector3[] positions,scales;CatActivityAnimation source;
    public readonly float[] Lengths=new float[4];
    public int Frame {get;private set;}=-1;
    public float SourcePhase {get;private set;}
    public float MaximumParentAxisRatio {get;private set;}
    public void Bind(Transform[] chain,Transform[] sourceBones,CatActivityAnimation driver)
    {joints=chain;bones=sourceBones;source=driver;positions=new Vector3[bones.Length];scales=new Vector3[bones.Length];}
    void LateUpdate()
    {
        if(joints==null||bones==null)return;
        for(int limb=0;limb<2;limb++)for(int segment=0;segment<2;segment++)
            Lengths[limb*2+segment]=Vector3.Distance(joints[limb*3+segment].position,joints[limb*3+segment+1].position);
        for(int i=0;i<bones.Length;i++){positions[i]=bones[i].localPosition;scales[i]=bones[i].localScale;}
        MaximumParentAxisRatio=1;
        foreach(var joint in joints)
        {
            if(joint.parent==null)continue;var m=joint.parent.localToWorldMatrix;
            float x=((Vector3)m.GetColumn(0)).magnitude,y=((Vector3)m.GetColumn(1)).magnitude,z=((Vector3)m.GetColumn(2)).magnitude;
            float minimum=Mathf.Min(x,Mathf.Min(y,z));
            if(minimum>.000001f)MaximumParentAxisRatio=Mathf.Max(MaximumParentAxisRatio,Mathf.Max(x,Mathf.Max(y,z))/minimum);
        }
        SourcePhase=source!=null?source.NativeJumpPhase:-1;Frame=Time.frameCount;
    }
    public void LocalErrors(out float positionError,out float scaleError)
    {
        positionError=scaleError=0;
        for(int i=0;i<bones.Length;i++)
        {positionError=Mathf.Max(positionError,Vector3.Distance(positions[i],bones[i].localPosition));scaleError=Mathf.Max(scaleError,Vector3.Distance(scales[i],bones[i].localScale));}
    }
}
