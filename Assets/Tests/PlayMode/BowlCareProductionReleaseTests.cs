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
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Real HUD, production source time and independent native skin measurements.
// No manual pose/care writes, contact overrides or acceptance tolerance changes.
public sealed class BowlCareProductionReleaseTests
{
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    CareAlignmentPolishTests fixture;
    readonly QaMeshTopologyCache topology=new QaMeshTopologyCache();
    QaExactMeshContact exact;
    static T Read<T>(object owner,string name)=>(T)owner.GetType().GetField(name,Fields).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Fields).SetValue(owner,value);
    static object Call(object owner,string name,params object[] values)=>owner.GetType().GetMethod(name,Fields).Invoke(owner,values);
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_FINISH_2026-09-16");
    [SetUp]public void Before(){fixture=new CareAlignmentPolishTests();fixture.Before();exact=new QaExactMeshContact(topology);}
    [TearDown]public void After(){Time.timeScale=1;exact?.Clear();topology.Clear();fixture?.After();fixture=null;}

    [UnityTest,Timeout(240000)]
    public IEnumerator TwoBreeds_FourRealBowlButtons_ProductionDurationAndClearEverySourceFrame()
    {
        yield return VerifyCare(new[]{"oriental-shorthair","persian"});
    }
    [UnityTest,Timeout(900000)]
    public IEnumerator TenBreeds_TwentyRealCareActions_IndependentSkinEveryFrame()
    {
        Time.captureFramerate=20;
        yield return VerifyCare(CatBreedCatalog.Load().Entries.Select(entry=>entry.Id).ToArray(),true,120);
    }
    [UnityTest,Timeout(300000)]
    public IEnumerator CriticalBreeds_CareAndWalkingExitDiagnostic()
    {Time.captureFramerate=20;yield return VerifyCare(new[]{"domestic-longhair","maine-coon"},true,120);}
    IEnumerator VerifyCare(string[] breeds,bool verifyWalkingExit=false,int walkingFrames=40)
    {
        var failures=new List<string>();
        var rows=new List<string>{"breed,kind,frames,contactFrames,unsafeFrames,completed,needDelta,maxRoot,maxYaw,minimumMouth,maxSkinDepth,minSkinY,maxPawError,witnessVertex,witnessCollider"};
        var witness=new List<string>{"breed,kind,frame,headActive,weight,lean,vertex,collider,depth,point"};
        var exits=new List<string>{"breed,kind,travel,awayTravel,maxWalkingSkinDepth,start,target,away,heading,end,blockedTurnFrames,finalFacingAngle"};
        var walkFrames=new List<string>{"breed,kind,frame,position,yaw,backing,blocked,groundSpeed,escapeSlideSeconds,escapeTurnSign"};
        var baked=new Mesh();
        try
        {
            yield return (IEnumerator)Call(fixture,"Home");
            foreach(string breed in breeds)
            {
                yield return (IEnumerator)Call(fixture,"Breed",breed);
                var cat=Read<CatMovement>(fixture,"cat");var bowls=Read<BowlInteraction>(fixture,"bowls");
                var hunger=Read<HungerSystem>(fixture,"hunger");var thirst=Read<ThirstSystem>(fixture,"thirst");
                foreach(string kind in new[]{"food","water"})
                {
                    Call(fixture,"ReadyNeeds");var setup=Read<BowlInteraction.BowlSetup>(bowls,kind);setup.Fill();
                    Assert.That(Read<float>(bowls,kind=="food"?"eatingDuration":"drinkingDuration"),Is.EqualTo(10f),"Use the production balance duration without overriding it.");
                    Transform target=setup.ContactPoint!=null?setup.ContactPoint:setup.Bowl;
                    // Nearby offers deliberately cover more than a physically ready
                    // stance; this contact release test must begin at a ready stance.
                    Func<bool> offered=()=>{Call(bowls,"Update");return ReferenceEquals(Read<BowlInteraction.BowlSetup>(bowls,"currentBowl"),setup)&&CatMealHeadMotion.TryPrepareCareStart(cat,target,out _);};
                    Call(fixture,"FindCareStance",target,setup.InteractionPoint,offered);
                    var button=(Button)Call(fixture,"BowlButton",setup);
                    var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();
                    var indices=CatBreedCatalog.Load().Find(breed).ContactVertexIndices;
                    // Original visible physical surfaces, independently selected.
                    var solids=Physics.OverlapSphere(target.position,.85f,~0,QueryTriggerInteraction.Ignore)
                        .OfType<MeshCollider>().Where(c=>c.enabled&&!c.isTrigger&&!c.transform.IsChildOf(cat.transform)&&
                            c.GetComponent<MeshFilter>()?.sharedMesh==c.sharedMesh&&c.GetComponent<Renderer>()?.enabled==true).Distinct().ToArray();
                    Assert.That(solids.Length,Is.GreaterThan(0));
                    Vector3 root=cat.transform.position;Quaternion yaw=cat.transform.rotation;
                    float before=kind=="food"?hunger.CurrentHunger:thirst.CurrentThirst;
                    int completed=0,contactFrames=0,unsafeFrames=0,frame=0;
                    float minMouth=float.PositiveInfinity,maxSkin=0,minY=float.PositiveInfinity,maxRoot=0,maxYaw=0,maxPaw=0;
                    int worstVertex=-1;string worstCollider="";
                    Action onComplete=()=>completed++;
                    if(kind=="food")hunger.Ate+=onComplete;else thirst.Drank+=onComplete;
                    try
                    {
                        button.onClick.Invoke();Assert.That(bowls.IsInteracting,Is.True);
                        float deadline=Time.realtimeSinceStartup+45;
                        while(bowls.IsInteracting&&Time.realtimeSinceStartup<deadline)
                        {
                            yield return new WaitForEndOfFrame();frame++;
                            var head=cat.GetComponent<CatMealHeadMotion>();bool active=head!=null&&head.IsActive;
                            if(active&&!head.CareFrameClear)unsafeFrames++;
                            float distance=(float)Call(fixture,"ActualMouthDistance",target);
                            if(bowls.IsAtContact){minMouth=Mathf.Min(minMouth,distance);if(distance<=.045f)contactFrames++;}
                            if(active)maxPaw=Mathf.Max(maxPaw,head.PawPlantError);
                            maxRoot=Mathf.Max(maxRoot,Vector3.Distance(root,cat.transform.position));maxYaw=Mathf.Max(maxYaw,Quaternion.Angle(yaw,cat.transform.rotation));
                            skin.BakeMesh(baked,true);var points=baked.vertices;
                            foreach(int vertex in indices)
                            {
                                Vector3 point=skin.transform.TransformPoint(points[vertex]);minY=Mathf.Min(minY,point.y);
                                foreach(var solid in solids)
                                {
                                    if(!solid.enabled||!solid.gameObject.activeInHierarchy||!solid.bounds.Contains(point)||InsideVotes(solid,point)<4)continue;
                                    float depth=exact.Measure(solid.sharedMesh,solid.transform,point).distance;
                                    if(depth<=maxSkin)continue;
                                    maxSkin=depth;worstVertex=vertex;worstCollider=solid.name;
                                    if(depth>.003f)witness.Add(Csv(breed,kind,frame,active,active?Read<float>(head,"weight"):0,active?head.BodyLeanDistance:0,vertex,solid.name,depth,point));
                                }
                            }
                        }
                        float delta=(kind=="food"?hunger.CurrentHunger:thirst.CurrentThirst)-before;
                        rows.Add(Csv(breed,kind,frame,contactFrames,unsafeFrames,completed,delta,maxRoot,maxYaw,minMouth,maxSkin,minY,maxPaw,worstVertex,worstCollider));Flush();
                        string label=breed+"/"+kind+": ";
                        if(bowls.IsInteracting)failures.Add(label+"routine timed out");
                        if(completed!=1||delta<60)failures.Add(label+"one real completion and normal need recovery required");
                        if(contactFrames<=8||minMouth>.045f)failures.Add(label+"actual mouth contact missing");
                        if(unsafeFrames!=0||maxSkin>.003f||minY<-.003f)failures.Add(label+"unsafe source frame; max depth="+maxSkin.ToString("R",CultureInfo.InvariantCulture));
                        if(maxRoot>.0001f||maxYaw>.01f||maxPaw>.0001f)failures.Add(label+"root/yaw/paw changed");
                    }
                    finally
                    {
                        if(kind=="food")hunger.Ate-=onComplete;else thirst.Drank-=onComplete;
                        CatActionState.CancelForTransition(cat);
                    }
                    yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                    Assert.That(cat.GetComponent<CharacterController>().enabled&&!cat.IsMovementPhysicallyLocked,Is.True);
                    if(verifyWalkingExit)
                    {
                        var stick=Object.FindAnyObjectByType<MobileJoystick>();Assert.That(stick,Is.Not.Null);
                        Set(cat,"cameraTransform",null);
                        Vector3 start=cat.transform.position,away=start-target.position;away.y=0;away.Normalize();
                        float walkDepth=0;float startYaw=cat.transform.eulerAngles.y;int blockedTurns=0;
                        if(breed=="maine-coon"&&kind=="water")ScreenCapture.CaptureScreenshot(Path.Combine(Output,"maine-water-before-walk.png"));
                        try
                        {
                            typeof(MobileJoystick).GetProperty("Direction").SetValue(stick,new Vector2(away.x,away.z));
                            for(int step=0;step<walkingFrames;step++)
                            {
                                yield return new WaitForEndOfFrame();if(Read<bool>(cat,"collisionLimitedTurn"))blockedTurns++;
                                walkFrames.Add(Csv(breed,kind,step,cat.transform.position,cat.transform.eulerAngles.y,
                                    Read<bool>(cat,"backingFromCorner"),Read<bool>(cat,"collisionLimitedTurn"),Read<float>(cat,"currentGroundSpeed"),
                                    Read<float>(cat,"escapeSlideSeconds"),Read<float>(cat,"escapeTurnSign")));
                                skin.BakeMesh(baked,true);var points=baked.vertices;
                                foreach(int vertex in indices)
                                {
                                    Vector3 point=skin.transform.TransformPoint(points[vertex]);
                                    foreach(var solid in solids)
                                        if(solid.enabled&&solid.gameObject.activeInHierarchy&&solid.bounds.Contains(point)&&InsideVotes(solid,point)>=4)
                                            walkDepth=Mathf.Max(walkDepth,exact.Measure(solid.sharedMesh,solid.transform,point).distance);
                                }
                            }
                        }
                        finally{stick.CancelInput();}
                        Vector3 moved=cat.transform.position-start;float travel=Vector3.Dot(moved,away);
                        float finalFacing=Vector3.Angle(cat.transform.forward,away);
                        exits.Add(Csv(breed,kind,moved.magnitude,travel,walkDepth,start,target.position,away,startYaw,cat.transform.position,blockedTurns,finalFacing));
                        if(breed=="maine-coon"&&kind=="water")ScreenCapture.CaptureScreenshot(Path.Combine(Output,"maine-water-after-walk.png"));
                        File.WriteAllLines(Path.Combine(Output,"bowl-care-walking-exit.csv"),exits);
                        File.WriteAllLines(Path.Combine(Output,"bowl-care-walking-frames.csv"),walkFrames);
                        if(moved.magnitude<.25f||travel<.20f||walkDepth>.015f)failures.Add(breed+"/"+kind+" walking exit: "+exits[exits.Count-1]);
                        if(walkingFrames>=120&&finalFacing>45f)failures.Add(breed+"/"+kind+" must finish turning toward joystick after escape: "+finalFacing);
                        // Let the real locomotion blend return to standing before
                        // probing the next bowl; never feed from a sampled walk.
                        yield return new WaitForSeconds(.5f);
                    }
                    yield return null;
                }
            }
            Assert.That(rows.Count,Is.EqualTo(breeds.Length*2+1));Assert.That(failures,Is.Empty,string.Join("\n",failures));
        }
        finally{Object.DestroyImmediate(baked);Flush();}
        void Flush(){Directory.CreateDirectory(Output);File.WriteAllLines(Path.Combine(Output,"bowl-care-production-release.csv"),rows);File.WriteAllLines(Path.Combine(Output,"bowl-care-production-witness.csv"),witness);}
    }
    static readonly Vector3[] Directions={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,new Vector3(.019f,.023f,1).normalized,
        -new Vector3(.013f,1,.027f).normalized,-new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
    int InsideVotes(MeshCollider mesh,Vector3 point)
    {
        var data=topology.Get(mesh.sharedMesh);int votes=0;bool previous=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
        try
        {
            foreach(var direction in Directions)if(mesh.Raycast(new Ray(point,direction),out var hit,5f))
            {
                int t=hit.triangleIndex*3;if(t<0||t+2>=data.triangles.Length)continue;
                Vector3 a=data.vertices[data.triangles[t]],b=data.vertices[data.triangles[t+1]],c=data.vertices[data.triangles[t+2]];
                Vector3 normal=mesh.transform.worldToLocalMatrix.transpose.MultiplyVector(Vector3.Cross(b-a,c-a));
                if(Vector3.Dot(normal,direction)>0)votes++;
            }
        }
        finally{Physics.queriesHitBackfaces=previous;}return votes;
    }
    static string Csv(params object[] values)=>string.Join(",",values.Select(value=>"\""+(value is Vector3 p?FormattableString.Invariant($"{p.x:F7};{p.y:F7};{p.z:F7}"):value is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):value?.ToString()??"").Replace("\"","\"\"")+"\""));
}
#endif
