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

// QA only: no change to production reach, collision or completion. Every
// hypothetical pose is restored before yielding and never enters MinimumDistance.
public sealed class CareHeadClearanceTrialTests
{
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags Static=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static readonly int[] SourceFrames={99,224};
    CareAlignmentPolishTests fixture;bool prepared;
    readonly QaMeshTopologyCache topology=new QaMeshTopologyCache();QaExactMeshContact metric;
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");
    static T Read<T>(object value,string name)=>(T)value.GetType().GetField(name,Fields).GetValue(value);
    static void Set(object value,string name,object field)=>value.GetType().GetField(name,Fields).SetValue(value,field);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name,Fields).Invoke(value,args);
    static Transform Bone(Transform root,string name)=>CatBreedVisualFactory.FindDescendant(root,name);
    [SetUp]public void Before(){fixture=new CareAlignmentPolishTests();fixture.Before();prepared=true;metric=new QaExactMeshContact(topology);}
    [TearDown]public void After(){Time.timeScale=1;metric?.Clear();topology.Clear();if(prepared)fixture.After();prepared=false;}
    sealed class Candidate{public string family;public float forward,lower,pitch,goalLift;}
    sealed class Leg
    {
        public string name;public object plant;public Transform upper,lower,foot;
        public Vector3 target,sourceElbow;public Quaternion rotation;
        public float upperLength,lowerLength,soleCorrection;
    }
    struct SkinResult{public float depth,minimumY;public int inside,vertex,triangle,submesh,votes;public string collider,bone;public Vector3 point,nearest,normal;}

    [UnityTest,Timeout(180000)]
    public IEnumerator PersianRealMealTwoSourcePhases_HeadClearanceAndFourPawReachCandidates()
    {
        var rows=new List<string>{"breed,sourceFrame,family,forward,lower,spine001Pitch,goalLift,solved,planted,shoulderPitch,foodDistance,nativeLowestFood,nativeNearestFood,nativePureError,maxPawError,maxBoneLengthError,minimumLegMargin,maximumSkinDepth,insideVertices,skinVertex,skinCollider,skinPoint,skinNearest,skinBone,skinTriangle,skinSubmesh,skinNormal,skinOutwardVotes,minimumSkinY,pelvisForward,chestForward,supportCentreMargin,rootDrift,yawDrift,contactAndAnatomy"};
        var legs=new List<string>{"breed,sourceFrame,family,forward,lower,spine001Pitch,goalLift,leg,upperLength,lowerLength,required,maximumMargin,minimumMargin,plantResult,pawError,soleCorrection,upper,pawTarget,sourceElbow"};
        var source=new List<string>{"breed,bone,parent,world,local,root,yaw,sourceLowestFood,sourceMinimumDistance"};
        var states=new List<string>{"breed,event,mealFrame,root,yaw,minimumDistance,completions"};
        try
        {
            yield return (IEnumerator)Call(fixture,"Home");yield return (IEnumerator)Call(fixture,"Room",HomeRoomService.KitchenId);
            foreach(string breed in new[]{"persian"})
            {
                yield return (IEnumerator)Call(fixture,"Breed",breed);Call(fixture,"ReadyNeeds");
                var cat=Read<CatMovement>(fixture,"cat");
                var meal=CatActivity.Registered.OfType<MealTimeActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene);
                Call(fixture,"FindCareStance",meal.BowlPoint,Read<Transform>(meal,"standPoint"),(Func<bool>)(()=>meal.TryGetPromptDistance(cat,out _)));
                var prompt=Object.FindAnyObjectByType<ActivityPromptController>();Call(cat.GetComponent<BowlInteraction>(),"Update");Call(prompt,"RefreshImmediate");
                Assert.That(Read<CatActivity>(prompt,"candidate"),Is.SameAs(meal));var button=Read<Button>(prompt,"actionButton");
                Assert.That(button!=null&&button.isActiveAndEnabled&&button.interactable,Is.True);
                int completions=0;Action<CatActivity> completed=a=>{if(a==meal)completions++;};CatActivity.Completed+=completed;
                int measured=0;
                try
                {
                    CareAlignmentPolishTests.TransitionMark("Lean before actual button "+breed);button.onClick.Invoke();
                    Assert.That(meal.IsRunning&&meal.IsEating,Is.True);int frame=0;float deadline=Time.realtimeSinceStartup+60f;
                    while(meal.IsRunning&&Time.realtimeSinceStartup<deadline)
                    {
                        yield return new WaitForEndOfFrame();frame++;
                        if(!SourceFrames.Contains(frame))continue;
                        var head=cat.GetComponent<CatMealHeadMotion>();Assert.That(head.IsActive&&Read<float>(head,"weight")>=.999f,Is.True);
                        float minimum=head.MinimumDistance;states.Add(Csv(breed,"before",frame,cat.transform.position,cat.transform.eulerAngles.y,minimum,completions));
                        Time.timeScale=0;
                        try{yield return Measure(breed,frame,cat,meal,head,rows,legs,source);}
                        finally{Time.timeScale=1;}
                        Assert.That(head.MinimumDistance,Is.EqualTo(minimum),"Hypothetical contacts never count as actual eating");
                        states.Add(Csv(breed,"restored",frame,cat.transform.position,cat.transform.eulerAngles.y,head.MinimumDistance,completions));measured++;
                    }
                    Assert.That(measured,Is.EqualTo(2));Assert.That(meal.IsRunning,Is.False);
                    states.Add(Csv(breed,"original-complete",frame,cat.transform.position,cat.transform.eulerAngles.y,cat.GetComponent<CatMealHeadMotion>().MinimumDistance,completions));
                }
                finally{Time.timeScale=1;CatActivity.Completed-=completed;CatActionState.CancelForTransition(cat);}
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            }
            Assert.That(rows.Count-1,Is.EqualTo(40));
        }
        finally
        {
            Directory.CreateDirectory(Output);File.WriteAllLines(Path.Combine(Output,"care-head-clearance-trial-candidates.csv"),rows);
            File.WriteAllLines(Path.Combine(Output,"care-head-clearance-trial-legs.csv"),legs);File.WriteAllLines(Path.Combine(Output,"care-head-clearance-trial-source.csv"),source);
            File.WriteAllLines(Path.Combine(Output,"care-head-clearance-trial-states.csv"),states);
        }
    }

    IEnumerator Measure(string breed,int sourceFrame,CatMovement cat,MealTimeActivity meal,CatMealHeadMotion head,List<string> rows,List<string> legRows,List<string> sourceRows)
    {
        var animator=cat.GetComponentInChildren<Animator>();var visual=animator.transform;var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();
        var all=animator.GetComponentsInChildren<Transform>(true);var winningPosition=all.Select(t=>t.localPosition).ToArray();
        var winningRotation=all.Select(t=>t.localRotation).ToArray();var winningScale=all.Select(t=>t.localScale).ToArray();
        Vector3 root=cat.transform.position;Quaternion yaw=cat.transform.rotation;float nativeBefore=(float)Call(fixture,"ActualMouthDistance",meal.BowlPoint);
        var pose=(CatCareReachGeometry.Pose)typeof(CareReachGeometryDiagnosticTests).GetMethod("Capture",Static).Invoke(null,new object[]{head,cat});
        var work=new CatCareReachGeometry.Workspace();Assert.That(CatCareReachGeometry.TrySolve(pose,meal.BowlPoint.position,1,work,out var baseline),Is.True);
        // The current native component already leans; this raw source baseline is not its final mouth pose.
        var torso=Read<Transform>(head,"torso");var neck=Read<Transform[]>(head,"joints");var spine=Bone(cat.transform,"DEF-spine.001");var pelvis=Bone(cat.transform,"DEF-spine");
        var mesh=new Mesh();var limbType=Read<Array>(head,"forelegs").GetValue(0).GetType();var limbs=new Leg[4];
        var indices=CatBreedCatalog.Load().Find(breed).ContactVertexIndices;
        var colliders=meal.GetComponentsInChildren<MeshCollider>().Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy).ToArray();
        foreach(var collider in colliders)Assert.That(collider.GetComponent<MeshFilter>()?.sharedMesh,Is.SameAs(collider.sharedMesh));
        Assert.That(colliders.Length,Is.GreaterThan(0));
        Call(head,"RestoreFrame");
        var sourcePosition=all.Select(t=>t.localPosition).ToArray();var sourceRotation=all.Select(t=>t.localRotation).ToArray();var sourceScale=all.Select(t=>t.localScale).ToArray();
        Vector3 spinePivot=spine.position,pelvisSource=pelvis.position,torsoSource=torso.position;
        skin.BakeMesh(mesh,true);var originalVertices=mesh.vertices;
        for(int n=0;n<4;n++)
        {
            string side=n%2==0?"L":"R";var leg=new Leg{name=(n<2?"front-":"rear-")+side,
                upper=Bone(cat.transform,(n<2?"DEF-upper_arm.":"DEF-thigh.")+side),
                lower=Bone(cat.transform,(n<2?"DEF-forearm.":"DEF-shin.")+side),foot=Bone(cat.transform,(n<2?"DEF-hand.":"DEF-foot.")+side)};
            Assert.That(leg.lower.parent,Is.SameAs(leg.upper));Assert.That(leg.foot.parent,Is.SameAs(leg.lower));
            Assert.That(leg.upper.IsChildOf(spine),Is.EqualTo(n<2),"Source lower-spine pitch must not drag rear supports");
            leg.upperLength=Vector3.Distance(leg.upper.position,leg.lower.position);leg.lowerLength=Vector3.Distance(leg.lower.position,leg.foot.position);
            leg.rotation=leg.foot.rotation;leg.sourceElbow=leg.lower.position;leg.target=leg.foot.position;
            if(n<2)leg.target=n==0?pose.left.pawTarget:pose.rightLeg.pawTarget;
            else
            {
                float sole=float.PositiveInfinity;
                foreach(var vertex in originalVertices){Vector3 p=skin.transform.TransformPoint(vertex),d=p-leg.foot.position;
                    if(d.y<=0&&d.x*d.x+d.z*d.z<=.0049f)sole=Mathf.Min(sole,p.y);}
                Assert.That(float.IsInfinity(sole),Is.False);leg.soleCorrection=Mathf.Clamp(sole-.005f,0,.065f);leg.target.y-=leg.soleCorrection;
            }
            leg.plant=Activator.CreateInstance(limbType,true);Set(leg.plant,"arm",leg.upper);Set(leg.plant,"fore",leg.lower);Set(leg.plant,"hand",leg.foot);
            limbs[n]=leg;
        }
        foreach(var bone in new[]{pelvis,spine,torso}.Concat(neck).Concat(limbs.SelectMany(l=>new[]{l.upper,l.lower,l.foot})))
            sourceRows.Add(Csv(breed,bone.name,bone.parent.name,bone.position,bone.localPosition,root,yaw.eulerAngles.y,head.Distance,head.MinimumDistance));
        RestoreWinning();
        var candidates=new List<Candidate>();
        foreach(float forward in new[]{.060f,.065f,.070f,.075f})foreach(float lift in new[]{0f,.005f,.010f,.015f,.020f})
            candidates.Add(new Candidate{family="head-clearance",forward=forward,goalLift=lift});
        try
        {
            foreach(var candidate in candidates)
            {
                try
                {
                    RestoreSource();float scale=cat.transform.lossyScale.y/.5f;
                    Vector3 shift=(cat.transform.forward*candidate.forward-Vector3.up*candidate.lower)*scale;
                    Quaternion turn=Quaternion.AngleAxis(candidate.pitch,cat.transform.right);
                    var input=Map(pose,shift,spinePivot,turn);
                    Vector3 solveFood=meal.BowlPoint.position+Vector3.up*candidate.goalLift;
                    bool solved=CatCareReachGeometry.TrySolve(input,solveFood,1,work,out var solution),planted=solved;
                    float maximumPaw=0,boneError=0,margin=float.PositiveInfinity,nativeLowest=float.NaN,nativeNearest=float.NaN,pureError=float.NaN;
                    SkinResult visible=default;float supportMargin=float.NaN,pelvisForward=0,chestForward=0;
                    if(solved)
                    {
                        visual.position+=shift;spine.rotation=turn*spine.rotation;
                        torso.rotation=Quaternion.AngleAxis(solution.shoulderPitch,cat.transform.right)*torso.rotation;
                        for(int n=0;n<4;n++)
                        {
                            var leg=limbs[n];Vector3 elbow=n<2?spinePivot+turn*(leg.sourceElbow-spinePivot)+shift:leg.sourceElbow+shift;
                            Set(leg.plant,"pawPosition",leg.target);Set(leg.plant,"pawRotation",leg.rotation);Set(leg.plant,"elbowPosition",elbow);
                            float distance=Vector3.Distance(leg.upper.position,leg.target),max=leg.upperLength+leg.lowerLength-.00001f-distance;
                            float min=distance-Mathf.Abs(leg.upperLength-leg.lowerLength)-.00001f;
                            bool accepted=(bool)Call(leg.plant,"Plant");planted&=accepted;
                            float error=Vector3.Distance(leg.foot.position,leg.target);maximumPaw=Mathf.Max(maximumPaw,error);margin=Mathf.Min(margin,max);
                            boneError=Mathf.Max(boneError,Mathf.Abs(Vector3.Distance(leg.upper.position,leg.lower.position)-leg.upperLength),
                                Mathf.Abs(Vector3.Distance(leg.lower.position,leg.foot.position)-leg.lowerLength));
                            legRows.Add(Csv(breed,sourceFrame,candidate.family,candidate.forward,candidate.lower,candidate.pitch,candidate.goalLift,leg.name,leg.upperLength,leg.lowerLength,
                                distance,max,min,accepted,error,leg.soleCorrection,leg.upper.position,leg.target,elbow));
                        }
                        neck[0].localRotation=solution.neck0;neck[1].localRotation=solution.neck1;neck[2].localRotation=solution.neck2;
                        nativeLowest=Vector3.Distance((Vector3)Call(Call(head,"LowestMouth"),"World"),meal.BowlPoint.position);
                        nativeNearest=(float)Call(fixture,"ActualMouthDistance",meal.BowlPoint);pureError=Mathf.Abs(Vector3.Distance((Vector3)Call(Call(head,"LowestMouth"),"World"),solveFood)-solution.foodDistance);
                        Assert.That(pureError,Is.LessThan(.00002f),"Extended source mapping agrees with actual rig at fixed root");
                        pelvisForward=Vector3.Dot(pelvis.position-pelvisSource,cat.transform.forward);chestForward=Vector3.Dot(torso.position-torsoSource,cat.transform.forward);
                        supportMargin=SupportMargin((pelvis.position+torso.position)*.5f,limbs.Select(l=>l.target).ToArray());
                        if(planted){skin.BakeMesh(mesh,true);visible=MeasureSkin(mesh.vertices,skin,indices,colliders);}
                    }
                    bool acceptable=solved&&planted&&nativeLowest<=.045f&&nativeNearest<=.045f&&maximumPaw<.0001f&&boneError<.00001f&&
                        visible.depth<=.003f&&visible.minimumY>=-.003f&&supportMargin>=0&&margin>=.003f;
                    rows.Add(Csv(breed,sourceFrame,candidate.family,candidate.forward,candidate.lower,candidate.pitch,candidate.goalLift,solved,planted,solution.shoulderPitch,
                        solution.foodDistance,nativeLowest,nativeNearest,pureError,maximumPaw,boneError,margin,visible.depth,visible.inside,visible.vertex,
                        visible.collider,visible.point,visible.nearest,visible.bone,visible.triangle,visible.submesh,visible.normal,visible.votes,visible.minimumY,pelvisForward,chestForward,supportMargin,
                        Vector3.Distance(root,cat.transform.position),Quaternion.Angle(yaw,cat.transform.rotation),acceptable));
                    Assert.That(cat.transform.position,Is.EqualTo(root));Assert.That(cat.transform.rotation,Is.EqualTo(yaw));
                }
                finally{RestoreWinning();}
                Assert.That((float)Call(fixture,"ActualMouthDistance",meal.BowlPoint),Is.EqualTo(nativeBefore).Within(.000002f));
                CareAlignmentPolishTests.TransitionMark("Lean candidate "+breed+" "+candidate.family+" "+candidate.forward+" "+candidate.lower+" "+candidate.pitch);
                File.WriteAllLines(Path.Combine(Output,"care-head-clearance-trial-candidates.csv"),rows);
                yield return null;
            }
        }
        finally{RestoreWinning();Object.DestroyImmediate(mesh);}
        void RestoreWinning(){for(int n=0;n<all.Length;n++){all[n].localPosition=winningPosition[n];all[n].localRotation=winningRotation[n];all[n].localScale=winningScale[n];}}
        void RestoreSource(){for(int n=0;n<all.Length;n++){all[n].localPosition=sourcePosition[n];all[n].localRotation=sourceRotation[n];all[n].localScale=sourceScale[n];}}
    }
    static CatCareReachGeometry.Pose Map(CatCareReachGeometry.Pose source,Vector3 shift,Vector3 pivot,Quaternion rotation)
    {
        Vector3 Point(Vector3 p)=>pivot+rotation*(p-pivot)+shift;
        CatCareReachGeometry.Leg Limb(CatCareReachGeometry.Leg leg){leg.upper=Point(leg.upper);leg.fore=Point(leg.fore);leg.sourceElbow=Point(leg.sourceElbow);return leg;}
        foreach(var vertex in source.mouth)Assert.That(vertex.influences.All(i=>i.anchor>=-1),Is.True,"All care mouth points must follow torso/neck");
        return new CatCareReachGeometry.Pose{root=source.root,right=source.right,torsoPosition=Point(source.torsoPosition),torsoRotation=rotation*source.torsoRotation,
            joints=source.joints,mouth=source.mouth,left=Limb(source.left),rightLeg=Limb(source.rightLeg)};
    }
    SkinResult MeasureSkin(Vector3[] points,SkinnedMeshRenderer skin,IReadOnlyList<int> indices,MeshCollider[] colliders)
    {
        var result=new SkinResult{minimumY=float.PositiveInfinity,vertex=-1,triangle=-1,submesh=-1};
        var weights=skin.sharedMesh.boneWeights;var bones=skin.bones;Assert.That(weights.Length,Is.EqualTo(points.Length));
        foreach(int index in indices)
        {
            Vector3 point=skin.transform.TransformPoint(points[index]);result.minimumY=Mathf.Min(result.minimumY,point.y);
            foreach(var collider in colliders)
            {
                if(!collider.bounds.Contains(point))continue;int votes=InsideVotes(collider,point);if(votes<4)continue;
                var closest=metric.Measure(collider.sharedMesh,collider.transform,point);result.inside++;
                if(closest.distance<=result.depth)continue;
                result.depth=closest.distance;result.vertex=index;result.collider=collider.name;result.point=point;result.nearest=closest.point;
                result.triangle=closest.triangle;result.submesh=closest.submesh;result.normal=closest.normal;result.votes=votes;
                var w=weights[index];int bone=w.boneIndex0;float weight=w.weight0;
                if(w.weight1>weight){bone=w.boneIndex1;weight=w.weight1;}if(w.weight2>weight){bone=w.boneIndex2;weight=w.weight2;}
                if(w.weight3>weight){bone=w.boneIndex3;weight=w.weight3;}
                result.bone=bone>=0&&bone<bones.Length?bones[bone].name+"@"+weight.ToString("F5",CultureInfo.InvariantCulture):"invalid";
            }
        }
        return result;
    }
    static readonly Vector3[] Directions={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,new Vector3(.019f,.023f,1).normalized,
        -new Vector3(.013f,1,.027f).normalized,-new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
    int InsideVotes(MeshCollider mesh,Vector3 point)
    {
        var data=topology.Get(mesh.sharedMesh);int votes=0;bool previous=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
        try{foreach(var direction in Directions)if(mesh.Raycast(new Ray(point,direction),out var hit,5f))
        {int t=hit.triangleIndex*3;if(t<0||t+2>=data.triangles.Length)continue;Vector3 a=mesh.transform.TransformPoint(data.vertices[data.triangles[t]]),
            b=mesh.transform.TransformPoint(data.vertices[data.triangles[t+1]]),c=mesh.transform.TransformPoint(data.vertices[data.triangles[t+2]]);
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),direction)>0)votes++;}}
        finally{Physics.queriesHitBackfaces=previous;}return votes;
    }
    // A geometric support proxy, not a mass/force simulation. Its source and
    // candidate margins are exported so an apparent reach cannot hide tipping.
    static float SupportMargin(Vector3 point,Vector3[] feet)
    {
        Vector3 centre=Vector3.zero;foreach(var p in feet)centre+=p;centre/=feet.Length;
        var polygon=feet.OrderBy(p=>Mathf.Atan2(p.z-centre.z,p.x-centre.x)).ToArray();float result=float.PositiveInfinity;
        for(int n=0;n<polygon.Length;n++){Vector3 a=polygon[n],edge=polygon[(n+1)%polygon.Length]-a;edge.y=0;Vector3 delta=point-a;delta.y=0;
            result=Mathf.Min(result,(edge.x*delta.z-edge.z*delta.x)/Mathf.Max(.00001f,edge.magnitude));}return result;
    }
    static string Csv(params object[] values)=>string.Join(",",values.Select(value=>"\""+(value is Vector3 p?FormattableString.Invariant($"{p.x:F7};{p.y:F7};{p.z:F7}"):
        value is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):value?.ToString()??"").Replace("\"","\"\"")+"\""));
}
#endif
