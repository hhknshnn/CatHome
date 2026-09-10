#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class LivingBowlContactTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Docs/QA/LIVING_TV_WALL_2026-09-10");
    HomeStoreSaveState store;
    string breed;
    float timeScale;
    CatMovement cat;
    BowlInteraction bowls;
    HungerSystem hunger;
    ThirstSystem thirst;
    GameObject needs,buttonHost;
    readonly List<string> evidence=new List<string>();
    static object Get(object o,string n)=>o.GetType().GetField(n,Private).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Private).SetValue(o,v);
    static void Call(object o,string n)=>o.GetType().GetMethod(n,Private).Invoke(o,null);
    static float SegmentDistance(Vector3 point,Vector3 a,Vector3 b)
    {var edge=b-a;return Vector3.Distance(point,a+edge*Mathf.Clamp01(Vector3.Dot(point-a,edge)/Mathf.Max(edge.sqrMagnitude,1e-12f)));}
    static float SurfaceDistance(Vector3 point,Vector3 a,Vector3 b,Vector3 c)
    {
        var ab=b-a;var ac=c-a;var normal=Vector3.Cross(ab,ac);
        if(normal.sqrMagnitude>1e-14f)
        {
            var projected=point-normal*(Vector3.Dot(point-a,normal)/normal.sqrMagnitude);var ap=projected-a;
            float aa=Vector3.Dot(ab,ab),cc=Vector3.Dot(ac,ac),cross=Vector3.Dot(ab,ac),den=aa*cc-cross*cross;
            if(den>1e-14f)
            {
                float u=(cc*Vector3.Dot(ap,ab)-cross*Vector3.Dot(ap,ac))/den;
                float v=(aa*Vector3.Dot(ap,ac)-cross*Vector3.Dot(ap,ab))/den;
                if(u>=0&&v>=0&&u+v<=1)return Vector3.Distance(point,projected);
            }
        }
        return Mathf.Min(SegmentDistance(point,a,b),Mathf.Min(SegmentDistance(point,b,c),SegmentDistance(point,c,a)));
    }

    [SetUp] public void Before()
    {Assert.That(EditorQaSession.IsActive,Is.True);store=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;timeScale=Time.timeScale;evidence.Clear();}
    [TearDown] public void After()
    {
        if(bowls!=null)bowls.CancelInteraction();
        if(buttonHost!=null)Object.DestroyImmediate(buttonHost);
        if(needs!=null)Object.DestroyImmediate(needs);
        Time.timeScale=timeScale;RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(store);CatBreedService.Select(breed);
        Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/"+TestContext.CurrentContext.Test.Name+".txt",evidence);
    }
    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        var state=HomeStoreSaveState.CreateDefault();state.ownedProductIds=HomeStoreService.Products.Where(p=>HomeStoreService.IsLivingRoomCollectionProduct(p.Id)).Select(p=>p.Id).ToArray();HomeStoreService.ApplySavedState(state);
        yield return null;yield return null;
        cat=Object.FindAnyObjectByType<CatMovement>();bowls=cat.GetComponent<BowlInteraction>();
        needs=new GameObject("Bowl contact test needs");hunger=Object.FindAnyObjectByType<HungerSystem>()??needs.AddComponent<HungerSystem>();thirst=Object.FindAnyObjectByType<ThirstSystem>()??needs.AddComponent<ThirstSystem>();
        Set(bowls,"hungerSystem",hunger);Set(bowls,"thirstSystem",thirst);Set(bowls,"eatingDuration",4.5f);Set(bowls,"drinkingDuration",4.5f);
        buttonHost=new GameObject("Measured care button",typeof(RectTransform),typeof(Button));Set(bowls,"interactionButton",buttonHost.GetComponent<Button>());bowls.ResolveSceneReferences();
        var idle=cat.GetComponent<CatIdleBehavior>();if(idle!=null)idle.enabled=false;
        yield return null;
    }
    [UnityTest] public IEnumerator DefaultBreed_ContactsBothRealContentsWithoutFullTurn()
    {yield return Prepare();yield return Check(CatBreedCatalog.DefaultBreedId,"food");yield return Check(CatBreedCatalog.DefaultBreedId,"water");}
    [UnityTest] public IEnumerator TenBreeds_KeepMouthContactPawSupportAndBoneLengths()
    {yield return Prepare();foreach(var entry in CatBreedCatalog.Load().Entries)foreach(var kind in new[]{"food","water"})yield return Check(entry.Id,kind);}

    [UnityTest] public IEnumerator NearEntryHeadings_PauseAndCancelRestoreControl()
    {
        yield return Prepare();
        foreach(string kind in new[]{"food","water"})foreach(float heading in new[]{0f,90f,180f,270f})
        {
            var setup=(BowlInteraction.BowlSetup)Get(bowls,kind);setup.Fill();hunger.ApplySavedValue(30);thirst.ApplySavedValue(30);
            var cc=cat.GetComponent<CharacterController>();cc.enabled=false;
            var start=setup.InteractionPoint.position+new Vector3(.12f,.05f,-.12f);
            cat.transform.SetPositionAndRotation(start,Quaternion.Euler(0,heading,0));cc.enabled=true;Physics.SyncTransforms();yield return null;Call(bowls,"Update");
            Assert.That(Get(bowls,"currentBowl"),Is.SameAs(setup));buttonHost.GetComponent<Button>().onClick.Invoke();
            float turn=0,yaw=heading,deadline=Time.realtimeSinceStartup+8;
            while(bowls.IsInteracting && bowls.ActiveCareSound==null && Time.realtimeSinceStartup<deadline)
            {yield return new WaitForEndOfFrame();float next=cat.transform.eulerAngles.y;turn+=Mathf.Abs(Mathf.DeltaAngle(yaw,next));yaw=next;}
            Assert.That(bowls.ActiveCareSound,Is.Not.Null,kind+" heading "+heading);Assert.That(turn,Is.LessThan(340f),"No full turn from any nearby heading");
            var root=cat.transform.position;var mouth=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-jaw").position;
            float need=kind=="food"?hunger.CurrentHunger:thirst.CurrentThirst;Time.timeScale=0;
            for(int i=0;i<4;i++)yield return new WaitForEndOfFrame();
            Assert.That(Vector3.Distance(root,cat.transform.position),Is.LessThan(.00001f));Assert.That(Vector3.Distance(mouth,CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-jaw").position),Is.LessThan(.0005f));
            Assert.That(kind=="food"?hunger.CurrentHunger:thirst.CurrentThirst,Is.EqualTo(need).Within(.0001f));
            bowls.CancelInteraction();Time.timeScale=1;yield return null;
            Assert.That(cat.GetComponent<CatSipHeadMotion>()==null||!cat.GetComponent<CatSipHeadMotion>().IsActive,Is.True);Assert.That(cc.enabled,Is.True);Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
            Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True);
            evidence.Add(kind+",heading="+heading+",turn="+turn.ToString("F2")+",pause/cancel=pass");
        }
    }

    IEnumerator Check(string id,string kind)
    {
        CatBreedService.Select(id);yield return null;yield return null;
        var setup=(BowlInteraction.BowlSetup)Get(bowls,kind);setup.Fill();hunger.ApplySavedValue(30);thirst.ApplySavedValue(30);
        var cc=cat.GetComponent<CharacterController>();cc.enabled=false;var entry=setup.InteractionPoint.position;entry.y=.05f;
        cat.transform.SetPositionAndRotation(entry,setup.FeedingPoint.rotation);cc.enabled=true;Physics.SyncTransforms();
        yield return null;Call(bowls,"Update");
        Assert.That(Get(bowls,"currentBowl"),Is.SameAs(setup),id+" "+kind+" prompt");
        var bones=cat.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("DEF-",StringComparison.Ordinal)).ToArray();
        var positions=bones.Select(t=>t.localPosition).ToArray();var scales=bones.Select(t=>t.localScale).ToArray();var rootScale=cat.transform.localScale;
        var sourceSkin=CatBreedCatalog.Load().Find(id).SourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
        var headVertices=new List<int>();var weights=sourceSkin.sharedMesh.boneWeights;
        for(int i=0;i<weights.Length;i++){var w=weights[i];int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] ws={w.weight0,w.weight1,w.weight2,w.weight3};float head=0;for(int j=0;j<4;j++){string name=sourceSkin.bones[ids[j]].name;if(name=="DEF-spine.006"||name.StartsWith("DEF-jaw")||name.StartsWith("DEF-ear"))head+=ws[j];}if(head>.5f)headVertices.Add(i);}
        // Long-haired neck tufts can enter a hand's proximity cylinder.
        // Paw support must use vertices actually skinned to that paw.
        var pawVertices=new Dictionary<string,List<int>>();
        foreach(string name in new[]{"DEF-hand.L","DEF-hand.R","DEF-foot.L","DEF-foot.R"})
        {
            var sourcePaw=sourceSkin.bones.First(b=>b.name==name);var indices=new List<int>();
            for(int i=0;i<weights.Length;i++)
            {
                var w=weights[i];int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] ws={w.weight0,w.weight1,w.weight2,w.weight3};float influence=0;
                for(int j=0;j<4;j++)if(ws[j]>0&&(sourceSkin.bones[ids[j]]==sourcePaw||sourceSkin.bones[ids[j]].IsChildOf(sourcePaw)))influence+=ws[j];
                if(influence>.5f)indices.Add(i);
            }
            Assert.That(indices,Is.Not.Empty,id+" real paw mesh "+name);pawVertices.Add(name,indices);
        }
        var sourceVisual=CatBreedVisualFactory.Create(CatBreedCatalog.Load().Find(id),CatBreedCatalog.Load().GameplayController,null);
        sourceVisual.transform.position=Vector3.one*100;
        var sourceAnimator=sourceVisual.GetComponentInChildren<Animator>();sourceAnimator.enabled=false;
        var sourceBones=bones.Select(b=>CatBreedVisualFactory.FindDescendant(sourceVisual.transform,b.name)).ToArray();
        var sourceClip=CatFeedingAlignmentCatalog.Load().sourceClip;
        var contents=new List<Vector3>();
        foreach(var filter in setup.Content.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=filter.GetComponent<Renderer>();if(renderer==null||!renderer.enabled)continue;
            // Editor-only readback keeps the shipped meshes non-readable.
            using(var data=UnityEditor.MeshUtility.AcquireReadOnlyMeshData(filter.sharedMesh))
            using(var points=new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount,Unity.Collections.Allocator.Temp))
            {
                data[0].GetVertices(points);
                for(int sub=0;sub<data[0].subMeshCount;sub++)
                using(var indices=new Unity.Collections.NativeArray<int>(data[0].GetSubMesh(sub).indexCount,Unity.Collections.Allocator.Temp))
                {
                    data[0].GetIndices(indices,sub);
                    foreach(int index in indices)contents.Add(filter.transform.TransformPoint(points[index]));
                }
            }
        }
        Assert.That(contents,Is.Not.Empty,"Actual visible contents required");
        float minDistance=float.PositiveInfinity,maxBoneAngle=0;
        float rimY=setup.Bowl.Find("PremiumCareVisual").GetComponentInChildren<Renderer>().bounds.max.y;
        float headRimClearance=float.PositiveInfinity;
        float yaw=cat.transform.eulerAngles.y,totalTurn=0,stationaryTurn=0,maxDistance=0,minView=1,maxView=-1,minPawGap=1,maxPawGap=-1,minStandClearance=1,maxWallZ=-10;int samples=0;
        Vector3 previousRoot=cat.transform.position;
        buttonHost.GetComponent<Button>().onClick.Invoke();Assert.That(bowls.IsInteracting,Is.True,id+" "+kind+" start");
        var mesh=new Mesh();bool consuming=false;float deadline=Time.realtimeSinceStartup+12f;
        try
        {
            while(bowls.IsInteracting && Time.realtimeSinceStartup<deadline)
            {
                yield return new WaitForEndOfFrame();
                float next=cat.transform.eulerAngles.y;
                if(!consuming)
                {
                    float delta=Mathf.Abs(Mathf.DeltaAngle(yaw,next));totalTurn+=delta;
                    Vector3 moved=cat.transform.position-previousRoot;moved.y=0f;
                    if(moved.magnitude<.001f)stationaryTurn+=delta;
                    Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position,.075f),Is.True,"The complete short entry must stay on open floor");
                }
                previousRoot=cat.transform.position;yaw=next;
                if(string.IsNullOrEmpty(bowls.ActiveCareSound))continue;
                consuming=true;samples++;
                Assert.That(CatActivityFacing.FacingDot(cat.transform.forward,cat.transform.position,CatActivityFacing.CameraPosition(cat)),Is.InRange(.05f,.5f),"The requested care pose must remain side-on to the player camera");
                var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();skin.BakeMesh(mesh,true);var vertices=mesh.vertices;
                maxWallZ=Mathf.Max(maxWallZ,vertices.Max(v=>skin.transform.TransformPoint(v).z));
                Assert.That(maxWallZ,Is.LessThanOrEqualTo(CatRoomArrangement.RearPanelZ+.002f),"The full feeding cat must stay in front of the back wall");
                foreach(int index in headVertices){var point=skin.transform.TransformPoint(vertices[index]);var delta=point-setup.Bowl.position;float radius=new Vector2(delta.x,delta.z).magnitude;if(radius>=.106f && radius<=.146f)headRimClearance=Mathf.Min(headRimClearance,point.y-rimY);}
                float distance=float.PositiveInfinity;
                foreach(var v in CatSipMouthCatalog.Load().Find(id).vertices)distance=Mathf.Min(distance,Vector3.Distance(skin.transform.TransformPoint(vertices[v.vertexIndex]),setup.ContactPoint.position));
                maxDistance=Mathf.Max(maxDistance,distance);
                // The vendor clip naturally sweeps across the contents. Its
                // bite must reach the actual surface, not one arbitrary point.
                if(samples%8==0&&minDistance>.01f)
                foreach(var v in CatSipMouthCatalog.Load().Find(id).vertices)
                {
                    var point=skin.transform.TransformPoint(vertices[v.vertexIndex]);
                    for(int i=0;i<contents.Count;i+=3)minDistance=Mathf.Min(minDistance,SurfaceDistance(point,contents[i],contents[i+1],contents[i+2]));
                }
                Assert.That(cat.GetComponent<CatSipHeadMotion>()==null||!cat.GetComponent<CatSipHeadMotion>().IsActive,Is.True,"Original Eating must have no head/body IK");
                var hip=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine");var shoulder=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine.003");
                float bodyView=CatActivityFacing.FacingDot(shoulder.position-hip.position,(shoulder.position+hip.position)*.5f,CatActivityFacing.CameraPosition(cat));
                minView=Mathf.Min(minView,bodyView);maxView=Mathf.Max(maxView,bodyView);
                foreach(string name in new[]{"DEF-hand.L","DEF-hand.R","DEF-foot.L","DEF-foot.R"})
                {
                    var paw=CatBreedVisualFactory.FindDescendant(cat.transform,name);Vector3 bottom=Vector3.zero;float low=float.PositiveInfinity;
                    foreach(int vertex in pawVertices[name])
                    {
                        var p=skin.transform.TransformPoint(vertices[vertex]);var delta=p-paw.position;delta.y=0;
                        if(delta.sqrMagnitude>.0049f || p.y>=paw.position.y || p.y>=low)continue;low=p.y;bottom=p;
                    }
                    Assert.That(float.IsPositiveInfinity(low),Is.False,"Actual paw mesh required");
                    float support=0;
                    var station=Object.FindAnyObjectByType<CatCareStationObstacle>();var local=station.transform.InverseTransformPoint(bottom);
                    // All four paws must stay on the room floor, outside the
                    // compact stand; its walking blocker is not paw support.
                    var minimum=station.Body.center-station.Body.size*.5f;
                    minStandClearance=Mathf.Min(minStandClearance,minimum.z-local.z);
                    Assert.That(local.z,Is.LessThan(minimum.z-.003f),"Every actual paw contact must be outside the stand: "+id+" "+kind+" "+name);
                    float gap=low-support;minPawGap=Mathf.Min(minPawGap,gap);maxPawGap=Mathf.Max(maxPawGap,gap);
                }
                // Compare the final live skeleton to the same original clip
                // sampled independently at the actual Animator phase.
                sourceClip.SampleAnimation(sourceAnimator.gameObject,Mathf.Repeat(cat.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime,1f)*sourceClip.length);
                for(int i=0;i<bones.Length;i++)
                {
                    float angle=Quaternion.Angle(bones[i].localRotation,sourceBones[i].localRotation);maxBoneAngle=Mathf.Max(maxBoneAngle,angle);
                    Assert.That(angle,Is.LessThan(.2f),bones[i].name+" must preserve original clip rotation");
                    Assert.That(Vector3.Distance(bones[i].localPosition,sourceBones[i].localPosition),Is.LessThan(.0001f),bones[i].name+" must preserve original clip position");
                }
                if(samples==12 && id==CatBreedCatalog.DefaultBreedId) {Directory.CreateDirectory(Output);ScreenCapture.CaptureScreenshot(Output+"/probe-"+kind+".png");}
                Assert.That(cat.transform.localScale,Is.EqualTo(rootScale));
            }
        }
        finally {Object.Destroy(mesh);Object.Destroy(sourceVisual);}
        string row=id+","+kind+",samples="+samples+",mouthMax="+maxDistance.ToString("F5")+",viewMin="+minView.ToString("F5")+",turn="+totalTurn.ToString("F2")+",pawMin="+minPawGap.ToString("F5")+",pawMax="+maxPawGap.ToString("F5")+",end="+cat.transform.position.ToString("F4");
        row+=",mouthMin="+minDistance.ToString("F5")+",boneAngleMax="+maxBoneAngle.ToString("F5")+",headRimClearance="+headRimClearance.ToString("F5")+",viewMax="+maxView.ToString("F5")+",standClearance="+minStandClearance.ToString("F5")+",maxWallZ="+maxWallZ.ToString("F5")+",stationaryTurn="+stationaryTurn.ToString("F3");evidence.Add(row);Debug.Log(row);
        Assert.That(headRimClearance,Is.GreaterThanOrEqualTo(.003f),"Actual head surface must clear the ceramic rim: "+row);
        Assert.That(samples,Is.GreaterThan(10),row);
        Assert.That(maxDistance,Is.LessThan(.12f),"Natural head lift stays over the bowl: "+row);
        Assert.That(minDistance,Is.LessThan(.035f),"Original bite reaches the contents every cycle: "+row);
        Assert.That(minView,Is.GreaterThanOrEqualTo(-.01f),row);
        Assert.That(maxView,Is.LessThanOrEqualTo(.5f),"The actual hip-to-shoulder body direction must read from the side throughout: "+row);
        Assert.That(totalTurn,Is.LessThan(180f),row);
        Assert.That(stationaryTurn,Is.LessThan(10f),"An already aligned nearby cat must not turn in place before feeding: "+row);
        Assert.That(minPawGap,Is.GreaterThan(-.015f),row);
        Assert.That(maxPawGap,Is.LessThan(.045f),row);
        Assert.That(bowls.IsInteracting,Is.False,row);Assert.That(cc.enabled,Is.True);Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
        Assert.That(Vector3.Distance(cat.transform.position,entry),Is.LessThan(.04f),"Return to the clear entry before restoring walking.");
    }
}
#endif
