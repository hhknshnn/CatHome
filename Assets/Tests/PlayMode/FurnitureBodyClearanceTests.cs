using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class FurnitureBodyClearanceTests
{
    HomeStoreSaveState saved;
    string breed;
    float capture,scale;
    GameObject probeObject;
    SphereCollider sphere;
    readonly List<string> failures=new List<string>();
    Mesh skinSample;
    int skinSampleFrame=-1, chairProbeCandidates, chairVerticesChecked;
    float chairMaximumSkinPenetration;
    bool confirmActualSkin;
    readonly Dictionary<Collider,MeshCollider> renderedFurniture=new Dictionary<Collider,MeshCollider>();
    readonly HashSet<string> capturedContacts=new HashSet<string>();
    static readonly Dictionary<Mesh,(Vector3[] vertices,int[] triangles)> meshTopology=new Dictionary<Mesh,(Vector3[],int[])>();
    readonly List<Vector3> chairSkinVertices=new List<Vector3>();
    readonly List<Vector3> sampledVertices=new List<Vector3>();
    [SetUp] public void Before()
    {
        skinSampleFrame=-1;chairProbeCandidates=0;chairVerticesChecked=0;chairMaximumSkinPenetration=0;confirmActualSkin=false;
        capturedContacts.Clear();
        meshTopology.Clear();
        Assert.That(EditorQaSession.IsActive,Is.True);
        saved=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;
        capture=Time.captureDeltaTime;scale=Time.timeScale;Time.timeScale=1;Time.captureFramerate=24;
        probeObject=new GameObject("Calibrated body probe");UnityEngine.Object.DontDestroyOnLoad(probeObject);
        probeObject.transform.position=Vector3.one*500;sphere=probeObject.AddComponent<SphereCollider>();sphere.isTrigger=true;sphere.radius=.075f;
        var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.transform.position=Vector3.one*510;Physics.SyncTransforms();
        Vector3 direction;float depth;var collider=box.GetComponent<BoxCollider>();
        Assert.That(Physics.ComputePenetration(sphere,box.transform.position,Quaternion.identity,collider,box.transform.position,box.transform.rotation,out direction,out depth)&&depth>.5f,Is.True,"An enabled probe must detect a known overlap; a disabled probe silently returns false.");
        Assert.That(Physics.ComputePenetration(sphere,box.transform.position+Vector3.right*2,Quaternion.identity,collider,box.transform.position,box.transform.rotation,out direction,out depth),Is.False);
        var meshProof=box.AddComponent<MeshCollider>();meshProof.sharedMesh=box.GetComponent<MeshFilter>().sharedMesh;
        Assert.That(InsideRenderedMesh(meshProof,box.transform.position,out _),Is.True,"Closed mesh interior must be detected");
        Assert.That(InsideRenderedMesh(meshProof,box.transform.position+Vector3.up,out _),Is.False,"Air above a support must stay clear");
        Assert.That(InsideRenderedMesh(meshProof,box.transform.position+Vector3.left,out _),Is.False,"Entering and leaving a closed mesh counts twice");
        UnityEngine.Object.Destroy(box);failures.Clear();
    }
    [TearDown] public void After()
    {
        CatActionState.CancelForTransition(UnityEngine.Object.FindAnyObjectByType<CatMovement>());
        RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(saved);CatBreedService.Select(breed);
        Time.captureDeltaTime=capture;Time.timeScale=scale;
        if(probeObject!=null)UnityEngine.Object.Destroy(probeObject);
        if(skinSample!=null)UnityEngine.Object.Destroy(skinSample);
        foreach(var proof in renderedFurniture.Values)if(proof!=null)UnityEngine.Object.Destroy(proof.gameObject);
        renderedFurniture.Clear();
        meshTopology.Clear();
        if(chairProbeCandidates>0)
            System.IO.File.WriteAllText(UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Temp")+"/chair-skin-contact.txt",
                "Broad probe candidates: "+chairProbeCandidates+"\nActual skin vertices checked: "+chairVerticesChecked+
                "\nMaximum measured skin penetration (m): "+chairMaximumSkinPenetration.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));
    }
    [UnityTest,Timeout(600000)] public IEnumerator CorrectedContacts_TenBreeds_KeepBodyAndArmsOutsideFurniture()
        => CheckContacts(new[]{HomeStoreService.BathroomToiletId,HomeStoreService.BedroomNightstandId,HomeStoreService.BedroomWardrobeId,
            HomeStoreService.GardenFlowerPotsId,"balcony.planter-box",HomeStoreService.BalconySideTableId,"patio.herb-trough",HomeStoreService.LoftRecordPlayerId},80);
    [UnityTest,Timeout(180000)] public IEnumerator Cart_TenBreeds_KeepBodyAndArmsOutsideFurniture()
        => CheckContacts(new[]{HomeStoreService.KitchenDishCartId},10);
    [UnityTest,Timeout(180000)] public IEnumerator Armchair_TenBreeds_KeepBodyAndArmsOutsideFurniture()
        => CheckContacts(new[]{"room.armchair"},10);
    [UnityTest,Timeout(600000)] public IEnumerator AllJumpingFurniture_KeepActualBodyOutsideFurniture()
    {
        confirmActualSkin=true;
        yield return CheckContacts(null,39,false);
    }
    [UnityTest] public IEnumerator LivingSofa_TenBreeds_KeepActualBodyOutsideFurniture()
    {confirmActualSkin=true;yield return CheckContacts(new[]{"SofaLounge"},10);}
    [UnityTest] public IEnumerator NarrowShelves_TenBreeds_KeepBodyOutsideTheWall()
    {confirmActualSkin=true;yield return CheckContacts(new[]{HomeStoreService.BalconyHerbShelfId,"loft.tall-bookcase"},20);}
    [UnityTest] public IEnumerator Planters_TenBreeds_TurnAndLeaveWithoutBodyClipping()
    {confirmActualSkin=true;yield return CheckContacts(new[]{HomeStoreService.GardenFlowerPotsId,HomeStoreService.BalconyPlanterBoxId,HomeStoreService.PatioHerbTroughId},30);}
    public static bool IsJumpActivity(CatActivity a) => a.IsUnlocked && a.IsContentVisible &&
        (a is PerchNapActivity || a is TowelNestActivity || a is TubEdgeWalkActivity || a is SinkSipActivity ||
        a is SwingRideActivity || a is PantryClimbActivity || a is LivingFurnitureActivity || a is SurfaceScatterActivity ||
        a is HamperDiveActivity || (a is KnockOffActivity k && k.PerchPoint!=null) ||
        (a is CanopyNapActivity c && c.NestPoint.position.y>.12f) || a is GardenYarnChaseActivity ||
        (a is LitterDigActivity l && l.UsesRaisedPlanter));
    private IEnumerator CheckContacts(string[] ids,int expected,bool tenBreeds=true)
    {
        int cycles=0;
        foreach(var room in HomeRoomService.Rooms)
        {
            if(ids!=null&&!HomeStoreService.GetRoomCollection(room.Id).Any(id=>ids.Contains(id))&&!(room.Id==HomeRoomService.LivingRoomId&&ids.Contains("SofaLounge")))continue;
            yield return RoomPlayModeSupport.LoadRoomAlone(room.SceneName);
            var owned=HomeStoreSaveState.CreateDefault();owned.ownedProductIds=HomeStoreService.GetRoomCollection(room.Id).ToArray();HomeStoreService.ApplySavedState(owned);yield return null;yield return null;
            var cat=UnityEngine.Object.FindAnyObjectByType<CatMovement>();cat.GetComponent<CatIdleBehavior>().enabled=false;
            var activities=CatActivity.Registered.Where(a=>ids==null?IsJumpActivity(a):ids.Contains(a.StoreProductId)||ids.Contains(a.ActivityId)).ToArray();
            foreach(var b in CatBreedCatalog.Load().Entries.Where(b=>tenBreeds||b.Id=="oriental-shorthair"))
            {
                CatBreedService.Select(b.Id);yield return null;yield return null;
                var hips=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine");
                var chest=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine.003");
                var head=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine.006");
                var limbs=new[]{"L","R"}.Select(side=>new[]{CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-upper_arm."+side),CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-forearm."+side),CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-hand."+side)}).ToArray();
                foreach(var activity in activities)
                {
                    var cc=cat.GetComponent<CharacterController>();cc.enabled=false;var origin=activity.RoutineEntryPoint.position;origin.y=.05f;
                    cat.transform.SetPositionAndRotation(origin,Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();RoomPlayModeSupport.ProvisionNeeds();yield return null;
                    int completed=0;Action<CatActivity> handler=a=>{if(a==activity)completed++;};CatActivity.Completed+=handler;
                    var hits=new HashSet<string>();
                    try
                    {
                        Assert.That(activity.TryStart(cat),Is.True,activity.StoreProductId+"/"+b.Id);
                        float deadline=Time.realtimeSinceStartup+25;int frame=0;
                        while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
                        {
                            yield return new WaitForEndOfFrame();frame++;
                            if(activity.IsWaitingForRestStop && activity.RestingSeconds>1.2f)activity.RequestRestStop();
                            if(frame%2!=0)continue;
                            Physics.SyncTransforms();string label=activity.StoreProductId+"/"+b.Id+"/"+cat.GetComponent<CatActivityAnimation>().CurrentPose+"@"+cat.GetComponent<CatActivityAnimation>().NativeJumpPhase.ToString("F2");
                            Check(cat,hips.position,.075f,label+" hips",hits);Check(cat,chest.position,.075f,label+" chest",hits);Check(cat,head.position,.06f,label+" head",hits);
                            for(int i=1;i<4;i++)Check(cat,Vector3.Lerp(hips.position,chest.position,i/4f),.065f,label+" torso",hits);
                            for(int side=0;side<2;side++)for(int i=1;i<=3;i++)
                            {Check(cat,Vector3.Lerp(limbs[side][0].position,limbs[side][1].position,i/3f),.023f,label+" upper arm",hits);Check(cat,Vector3.Lerp(limbs[side][1].position,limbs[side][2].position,i/3f),.018f,label+" forearm",hits);}
                        }
                        Assert.That(activity.IsRunning,Is.False,activity.StoreProductId+" timeout");
                        Assert.That(completed,Is.EqualTo(1),activity.StoreProductId+"/"+b.Id+(activity is KnockOffActivity?" nearest paw="+((KnockOffActivity)activity).MinimumPawDistance.ToString("F4"):""));
                        Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True,activity.StoreProductId+" exit");
                        if(activity is ScratchPostActivity){var scratch=(ScratchPostActivity)activity;Assert.That(scratch.LeftStrokes,Is.GreaterThanOrEqualTo(2));Assert.That(scratch.RightStrokes,Is.GreaterThanOrEqualTo(2));}
                        if(activity is KnockOffActivity)Assert.That(((KnockOffActivity)activity).ContactStrokes,Is.EqualTo(3));
                        failures.AddRange(hits);cycles++;
                    }
                    finally {CatActivity.Completed-=handler;}
                }
            }
        }
        Assert.That(cycles,Is.EqualTo(expected));
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }
    void Check(CatMovement cat,Vector3 point,float radius,string label,HashSet<string> hits)
    {
        foreach(var other in Physics.OverlapSphere(point,radius,~0,QueryTriggerInteraction.Ignore))
        {
            if(other==sphere||other.GetComponentInParent<CatMovement>()!=null)continue;
            sphere.radius=radius;Vector3 direction;float depth;
            if(Physics.ComputePenetration(sphere,point,Quaternion.identity,other,other.transform.position,other.transform.rotation,out direction,out depth)&&depth>.025f)
            {
                // A 15 cm hip probe extends below a seated cat's actual skin.
                // Confirm chair candidates against the rendered, tail-masked
                // mesh instead of treating intended cushion support as clipping.
                if((confirmActualSkin||CatActivity.Active?.StoreProductId=="room.armchair") && !ChairSkinPenetrates(cat,other,point,radius))continue;
                hits.Add(label+" overlaps "+other.name);
                var activity=CatActivity.Active;string key=(string.IsNullOrEmpty(activity.StoreProductId)?activity.ActivityId:activity.StoreProductId)+"-"+cat.GetComponent<CatActivityAnimation>().CurrentPose;
                if(confirmActualSkin&&capturedContacts.Add(key))
                {
                    string folder=UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Temp")+"/contact-candidates";System.IO.Directory.CreateDirectory(folder);
                    AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("RoomInteractionReview")).First(t=>t!=null)
                        .GetMethod("ActivityDetail",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{folder+"/"+key+".png",activity,cat});
                    System.IO.File.AppendAllText(folder+"/geometry.txt",key+" / "+other.name+" / "+other.transform.position+" / scale="+other.transform.lossyScale+" / euler="+other.transform.eulerAngles+" / bounds="+other.bounds+" / cat="+cat.transform.position+Environment.NewLine);
                }
            }
        }
    }
    bool ChairSkinPenetrates(CatMovement cat,Collider other,Vector3 centre,float radius)
    {
        chairProbeCandidates++;
        // The permanent sofa's navigation box fills the space above its seat.
        // Keep that broad-phase volume, but confirm visual clipping against
        // its rendered mesh without changing the live navigation collider.
        Collider contact=other;Vector3 contactPosition=other.transform.position;Quaternion contactRotation=other.transform.rotation;
        var furnitureMesh=other is BoxCollider&&other.name=="LivingSofa_PremiumModel"?other.GetComponent<MeshFilter>():null;
        if(furnitureMesh!=null&&furnitureMesh.sharedMesh!=null)
        {
            if(!renderedFurniture.TryGetValue(other,out var proof))
            {
                var item=new GameObject("Rendered furniture contact proof");UnityEngine.Object.DontDestroyOnLoad(item);
                item.transform.SetPositionAndRotation(Vector3.one*1000,furnitureMesh.transform.rotation);item.transform.localScale=furnitureMesh.transform.lossyScale;
                proof=item.AddComponent<MeshCollider>();proof.sharedMesh=furnitureMesh.sharedMesh;renderedFurniture.Add(other,proof);
            }
            contact=proof;contactPosition=furnitureMesh.transform.position;contactRotation=furnitureMesh.transform.rotation;
        }
        if(skinSampleFrame!=Time.frameCount)
        {
            skinSampleFrame=Time.frameCount;
            var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();
            if(skinSample==null)skinSample=new Mesh();
            skin.BakeMesh(skinSample,true);skinSample.GetVertices(sampledVertices);chairSkinVertices.Clear();
            var entry=CatBreedCatalog.Load().Find(CatBreedService.SelectedBreedId);
            Assert.That(entry.ContactVertexIndices,Is.Not.Empty,"Real body mask required");
            foreach(int index in entry.ContactVertexIndices)chairSkinVertices.Add(skin.transform.TransformPoint(sampledVertices[index]));
        }
        const float probeRadius=.003f;bool intersects=false;sphere.radius=probeRadius;
        foreach(var vertex in chairSkinVertices)
        {
            if((vertex-centre).sqrMagnitude>(radius+.02f)*(radius+.02f))continue;
            chairVerticesChecked++;
            if(contact!=other&&InsideRenderedMesh((MeshCollider)contact,vertex-contactPosition+contact.transform.position,out float insideDepth))
            {
                chairMaximumSkinPenetration=Mathf.Max(chairMaximumSkinPenetration,insideDepth);
                if(insideDepth>.005f)intersects=true;
            }
            if(Physics.ComputePenetration(sphere,vertex,Quaternion.identity,contact,contactPosition,contactRotation,out _,out var depth))
            {
                float penetration=Mathf.Max(0,depth-probeRadius);
                chairMaximumSkinPenetration=Mathf.Max(chairMaximumSkinPenetration,penetration);
                if(penetration>.005f)intersects=true;
            }
        }
        sphere.radius=radius;return intersects;
    }
    static bool InsideRenderedMesh(MeshCollider mesh,Vector3 point,out float depth)
    {
        // A concave MeshCollider penetration query can miss points deep inside
        // a closed backrest. The first triangle's original winding identifies
        // an exit from solid geometry; counting crossings is unreliable where
        // the sofa's separate upholstered pieces share coincident surfaces.
        bool before=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;depth=float.PositiveInfinity;
        try
        {
            int inside=0;
            if(!meshTopology.TryGetValue(mesh.sharedMesh,out var topology))
            {topology=(mesh.sharedMesh.vertices,mesh.sharedMesh.triangles);meshTopology.Add(mesh.sharedMesh,topology);}
            var vertices=topology.vertices;var triangles=topology.triangles;
            foreach(var axis in new[]{new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,new Vector3(.019f,.023f,1).normalized})
            {
                if(mesh.Raycast(new Ray(point,axis),out var hit,5f))
                {
                    int triangle=hit.triangleIndex*3;
                    Vector3 normal=Vector3.Cross(vertices[triangles[triangle+1]]-vertices[triangles[triangle]],vertices[triangles[triangle+2]]-vertices[triangles[triangle]]);
                    if(Vector3.Dot(mesh.transform.TransformDirection(normal),axis)>0){inside++;depth=Mathf.Min(depth,hit.distance);}
                }
            }
            return inside>=2;
        }
        finally{Physics.queriesHitBackfaces=before;}
    }
}
