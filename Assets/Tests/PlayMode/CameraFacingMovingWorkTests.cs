using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Work that moves within its contact area, plus honest directional travel through the real CAT tunnel.</summary>
public sealed class CameraFacingMovingWorkTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    // Reuse the full audit's exact save/prefs/needs isolation and restoration,
    // without running its inventory, sampling, or writing its report files.
    CameraFacingRoomAuditTests isolation;
    CatMovement cat;
    GameObject extraNeeds;
    CatActivity tracked;
    int completions;
    readonly List<string> errors = new List<string>();
    readonly List<Row> rows = new List<Row>();
    [Serializable] sealed class Row
    {
        public string room, product, phase, policy;
        public int samples, completions;
        public float minViewDot=1f, minMotionDot=1f, maxContactOffset, closestPawSurface=-1f;
        public bool completed, exitClear;
    }
    [Serializable] sealed class Evidence { public Row[] rows; public string[] errors; }
    sealed class Surface { public MeshCollider collider; }

    [SetUp] public void Before()
    {
        isolation=new CameraFacingRoomAuditTests(); isolation.Before();
        rows.Clear(); errors.Clear(); CatActivity.Completed+=Completed;
    }
    [TearDown] public void After()
    {
        if(cat!=null)CatActionState.CancelForTransition(cat);
        CatActivity.Completed-=Completed;
        if(extraNeeds!=null)Object.DestroyImmediate(extraNeeds);
        isolation?.After();
    }
    void Completed(CatActivity activity){if(activity==tracked)completions++;}
    static Transform Point(object owner,string name)=>(Transform)owner.GetType().GetField(name,Private).GetValue(owner);
    void Check(bool condition,string message){if(!condition&&!errors.Contains(message))errors.Add(message);}

    IEnumerator Prepare(string roomId)
    {
        var room=HomeRoomService.Rooms.Single(r=>r.Id==roomId);
        yield return RoomPlayModeSupport.LoadRoomAlone(room.SceneName);
        typeof(CatHomeSaveSystem).GetField("initialized",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,false);
        var state=HomeStoreSaveState.CreateDefault(); state.currentRoomId=roomId;
        state.ownedProductIds=HomeStoreService.Products.Where(p=>HomeStoreService.IsProductInRoomCollection(roomId,p.Id)||CatCollectionPolicy.IsCatItem(p.Id)).Select(p=>p.Id).ToArray();
        state.storedProductIds=state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
        HomeStoreService.ApplySavedState(state); CatBreedService.Select(CatBreedCatalog.DefaultBreedId);
        extraNeeds=new GameObject("Moving work QA needs");
        if(Object.FindAnyObjectByType<HungerSystem>()==null)extraNeeds.AddComponent<HungerSystem>();
        if(Object.FindAnyObjectByType<ThirstSystem>()==null)extraNeeds.AddComponent<ThirstSystem>();
        yield return null;yield return null;
        cat=Object.FindAnyObjectByType<CatMovement>();
        var idle=cat.GetComponent<CatIdleBehavior>();if(idle!=null)idle.enabled=false;
        RoomPlayModeSupport.ProvisionNeeds();
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
    }
    void Place(Vector3 point)
    {
        CatActionState.CancelForTransition(cat);point.y=.05f;
        cat.ApplySavedWorldPose(point,Quaternion.identity);Physics.SyncTransforms();RoomPlayModeSupport.ProvisionNeeds();
    }

    [UnityTest] public IEnumerator KitchenMeal_ActualEatingAndSatisfiedInspection_DipsRemainVisibleAtTheBowl()
    {
        yield return Prepare(HomeRoomService.KitchenId);
        var meal=CatActivity.Registered.OfType<MealTimeActivity>().Single(a=>a.StoreProductId==HomeStoreService.KitchenFeedingStationId);
        Transform stand=Point(meal,"standPoint"),bowl=Point(meal,"bowlPoint");
        foreach(bool satisfied in new[]{false,true})
        {
            Place(meal.RoutineEntryPoint.position);
            var hunger=Object.FindAnyObjectByType<HungerSystem>();hunger.ApplySavedValue(satisfied?100f:35f);
            string phase=satisfied?"inspection_dips":"eating_dips";
            yield return Observe(HomeRoomService.KitchenId,meal,phase,satisfied?CatActivityPose.Sniff:CatActivityPose.Eat,row=>
            {
                float radius=Planar(stand.position-bowl.position).magnitude;
                float actual=Planar(cat.transform.position-bowl.position).magnitude;
                row.maxContactOffset=Mathf.Max(row.maxContactOffset,Mathf.Abs(actual-radius));
                Check(actual>=radius-.10f&&actual<=radius+.025f,phase+": mouth dip left the authored bowl reach");
                Check(Vector3.Dot(Planar(cat.transform.forward).normalized,Planar(bowl.position-cat.transform.position).normalized)>.98f,
                    phase+": body must keep pointing into its actual bowl");
            });
            Check(meal.InspectingOnly==satisfied,phase+": wrong needs branch");
            if(!satisfied)Check(hunger.CurrentHunger>65f,phase+": real completed meal did not restore hunger");
        }
        Finish("meal-moving-work");
    }

    [UnityTest] public IEnumerator OutdoorScrapes_AllThreeActualPawCycles_AreReadableAndTouchTheirProductSurface()
    {
        foreach(var setup in new[]{(HomeRoomService.GardenId,HomeStoreService.GardenFlowerPotsId),
            (HomeRoomService.BalconyId,HomeStoreService.BalconyPlanterBoxId),(HomeRoomService.PatioId,HomeStoreService.PatioHerbTroughId)})
        {
            yield return Prepare(setup.Item1);
            var dig=CatActivity.Registered.OfType<LitterDigActivity>().Single(a=>a.StoreProductId==setup.Item2);
            Transform contact=Point(dig,"digPoint");
            var surfaces=dig.GetComponentsInChildren<MeshCollider>().Where(c=>c.enabled&&!c.isTrigger&&c.sharedMesh!=null&&
                c.GetComponent<MeshFilter>()!=null&&c.GetComponent<MeshFilter>().sharedMesh==c.sharedMesh&&
                c.GetComponent<Renderer>()!=null&&c.GetComponent<Renderer>().enabled)
                .Select(c=>new Surface{collider=c}).ToArray();
            Assert.That(surfaces,Is.Not.Empty,"The actual rendered product mesh must supply the measured contact surface.");
            var paws=cat.GetComponentsInChildren<Transform>().Where(t=>t.name=="DEF-hand.L"||t.name=="DEF-hand.R").ToArray();
            Place(dig.RoutineEntryPoint.position);
            yield return Observe(setup.Item1,dig,"paw_scrapes",CatActivityPose.Paw,row=>
            {
                float offset=Planar(cat.transform.position-contact.position).magnitude;
                row.maxContactOffset=Mathf.Max(row.maxContactOffset,offset);
                Check(offset<=.085f,setup.Item2+": scrape left its authored contact patch");
                foreach(var paw in paws)
                {
                    float gap=SurfaceGap(paw.position,surfaces);
                    if(!float.IsInfinity(gap)&&(row.closestPawSurface<0||gap<row.closestPawSurface))row.closestPawSurface=gap;
                }
            });
            Row measured=rows[rows.Count-1];
            Check(paws.Length==2&&measured.closestPawSurface>=0&&measured.closestPawSurface<=.08f,setup.Item2+": no real front paw approached the visible product surface");
        }
        Finish("outdoor-moving-work");
    }

    [UnityTest] public IEnumerator CatTunnel_BothOpenEnds_FollowActualTravelThroughTheArchAndReleaseControls()
    {
        yield return Prepare(HomeRoomService.LivingRoomId);
        Check(HomeStoreService.TrySetStored(HomeStoreService.PlayTunnelId,false),"tunnel: could not display within CAT 5/1");
        yield return null;yield return null;Physics.SyncTransforms();
        var tunnel=CatActivity.Registered.OfType<CatEnrichmentActivity>().Single(a=>a.StoreProductId==HomeStoreService.PlayTunnelId);
        Check(CatCollectionPolicy.DisplayedCount<=CatCollectionPolicy.Capacity,"tunnel: CAT capacity exceeded");
        foreach(bool reverse in new[]{false,true})
        {
            Vector3 entry=(reverse?tunnel.ExitPoint:tunnel.RoutineEntryPoint).position;
            Vector3 exit=(reverse?tunnel.RoutineEntryPoint:tunnel.ExitPoint).position;
            Vector3 axis=Planar(exit-entry).normalized;
            Place(entry);Vector3 previous=cat.transform.position;
            yield return Observe(HomeRoomService.LivingRoomId,tunnel,reverse?"crawl_from_far_end":"crawl_from_front",CatActivityPose.Crawl,row=>
            {
                row.policy="travel follows real velocity; camera dot recorded, not a stationary-facing gate";
                Vector3 motion=Planar(cat.transform.position-previous);previous=cat.transform.position;
                Vector3 offset=Planar(cat.transform.position-entry);
                float lateral=(offset-axis*Vector3.Dot(offset,axis)).magnitude;
                row.maxContactOffset=Mathf.Max(row.maxContactOffset,lateral);
                Check(lateral<=.035f,row.phase+": body left the actual opening axis");
                if(motion.sqrMagnitude>.000001f)
                {
                    var bones=cat.GetComponentsInChildren<Transform>();
                    Vector3 body=Planar(bones.Single(t=>t.name=="DEF-spine.003").position-bones.Single(t=>t.name=="DEF-spine").position).normalized;
                    float dot=Vector3.Dot(body,motion.normalized);row.minMotionDot=Mathf.Min(row.minMotionDot,dot);
                    Check(dot>=.90f,row.phase+": rendered body travels sideways/backwards");
                    Check(Vector3.Dot(Planar(cat.transform.forward).normalized,motion.normalized)>.98f,row.phase+": root does not follow the real velocity");
                }
            },false);
            Check(Planar(cat.transform.position-exit).magnitude<.04f,"tunnel: did not emerge at the real opposite opening");
        }
        Finish("tunnel-directional-work");
    }

    IEnumerator Observe(string room,CatActivity activity,string phase,CatActivityPose work,Action<Row> sample,bool requireCameraFacing=true)
    {
        var row=new Row{room=room,product=activity.StoreProductId,phase=phase,policy="actual moving contact work, front/side torso"};rows.Add(row);
        tracked=activity;completions=0;
        if(!activity.TryGetPromptDistance(cat,out _)||!activity.TryStart(cat)){Check(false,row.product+": could not start "+phase);yield break;}
        var animation=cat.GetComponent<CatActivityAnimation>();
        var bones=cat.GetComponentsInChildren<Transform>();Transform hips=bones.Single(t=>t.name=="DEF-spine"),shoulders=bones.Single(t=>t.name=="DEF-spine.003");
        Transform requiredSupport=activity is MealTimeActivity?Point(activity,"standPoint"):activity is LitterDigActivity?Point(activity,"digPoint"):null;
        float deadline=Time.realtimeSinceStartup+20f,poseTime=0;bool previouslyWorking=false;
        while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
        {
            yield return new WaitForEndOfFrame();if(!activity.IsRunning)break;
            CatActivityPose pose=animation.CurrentPose;
            bool working=pose==work&&(requiredSupport==null||animation.ContactSurface==requiredSupport);
            poseTime=working?(previouslyWorking?poseTime+Time.deltaTime:0):0;previouslyWorking=working;
            if(!working||poseTime<.35f)continue;
            // The .35s entry blend settles once. Real dips/strokes remain
            // included regardless of their root speed or pitch oscillation.
            float dot=CatActivityFacing.FacingDot(shoulders.position-hips.position,(hips.position+shoulders.position)*.5f,CatActivityFacing.CameraPosition(cat));
            row.minViewDot=Mathf.Min(row.minViewDot,dot);row.samples++;
            if(requireCameraFacing)Check(dot>=CatActivityFacing.MinimumViewDot,row.product+"/"+phase+": actual working torso faces away");
            sample(row);
        }
        row.completed=!activity.IsRunning&&completions==1;row.completions=completions;
        if(activity.IsRunning)activity.CancelForTransition();
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        row.exitClear=CatActivityMotion.IsFloorClear(cat.transform.position,.24f)&&!cat.IsMovementPhysicallyLocked&&cat.GetComponent<CharacterController>().enabled;
        Check(row.completed,row.product+"/"+phase+": did not complete exactly once");
        Check(row.samples>=12,row.product+"/"+phase+": insufficient real work samples");
        Check(row.exitClear,row.product+"/"+phase+": exit/control release failed");tracked=null;
    }
    static Vector3 Planar(Vector3 value){value.y=0;return value;}
    static float SurfaceGap(Vector3 paw,Surface[] surfaces)
    {
        float closest=float.PositiveInfinity;
        foreach(var surface in surfaces)
            if(surface.collider.Raycast(new Ray(paw+Vector3.up*1.5f,Vector3.down),out var hit,3f)&&hit.normal.y>.5f)
                closest=Mathf.Min(closest,Mathf.Abs(paw.y-hit.point.y));
        return closest;
    }
    void Finish(string name)
    {
        Directory.CreateDirectory("Temp/CameraFacingAudit");
        File.WriteAllText("Temp/CameraFacingAudit/"+name+".json",JsonUtility.ToJson(new Evidence{rows=rows.ToArray(),errors=errors.ToArray()},true));
        Assert.That(errors,Is.Empty,string.Join("\n",errors));
    }
}
