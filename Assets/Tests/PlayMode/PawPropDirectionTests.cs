using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PawPropDirectionTests
{
    HomeStoreSaveState saved;string breed;float capture,scale;
    [SetUp] public void Before(){saved=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;capture=Time.captureDeltaTime;scale=Time.timeScale;Time.captureFramerate=24;Time.timeScale=1;}
    [TearDown] public void After(){CatActionState.CancelForTransition(UnityEngine.Object.FindAnyObjectByType<CatMovement>());RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(saved);CatBreedService.Select(breed);Time.captureDeltaTime=capture;Time.timeScale=scale;}
    [UnityTest,Timeout(600000)] public IEnumerator KnockedPropsAndCart_TenBreeds_MoveAwayFromTheContactingHand_AndReset()
    {
        int cycles=0;var failures=new List<string>();
        foreach(var room in HomeRoomService.Rooms)
        {
            if(!new[]{HomeRoomService.KitchenId,HomeRoomService.BedroomId,HomeRoomService.BalconyId,HomeRoomService.SecondFloorId}.Contains(room.Id))continue;
            yield return RoomPlayModeSupport.LoadRoomAlone(room.SceneName);
            var owned=HomeStoreSaveState.CreateDefault();owned.ownedProductIds=HomeStoreService.GetRoomCollection(room.Id).ToArray();HomeStoreService.ApplySavedState(owned);yield return null;yield return null;
            var cat=UnityEngine.Object.FindAnyObjectByType<CatMovement>();cat.GetComponent<CatIdleBehavior>().enabled=false;
            var activity=CatActivity.Registered.First(a=>a is KnockOffActivity || a is CartNudgeActivity);
            var knock=activity as KnockOffActivity;var cart=activity as CartNudgeActivity;
            var prop=knock!=null?knock.GlassPivot:cart.CartVisual;
            foreach(var entry in CatBreedCatalog.Load().Entries)
            {
                CatBreedService.Select(entry.Id);yield return null;yield return null;
                var cc=cat.GetComponent<CharacterController>();cc.enabled=false;var origin=activity.RoutineEntryPoint.position;origin.y=.05f;cat.transform.SetPositionAndRotation(origin,Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();RoomPlayModeSupport.ProvisionNeeds();yield return null;
                string label=activity.StoreProductId+"/"+entry.Id;
                Vector3 home=prop.localPosition,previous=prop.position,strike=Vector3.zero,away=Vector3.zero;
                Quaternion rotation=prop.localRotation;int completed=0,samples=0;float dot=1,maxMotion=0;bool paused=false;
                Action<CatActivity> handler=a=>{if(a==activity)completed++;};CatActivity.Completed+=handler;
                Assert.That(activity.TryStart(cat),Is.True,label);float deadline=Time.realtimeSinceStartup+24;
                while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
                {
                    yield return new WaitForEndOfFrame();if(!activity.IsRunning)break;
                    bool tapping=knock!=null?knock.IsTapping:cart.IsPushing;
                    var paw=cat.GetComponent<CatToyContactMotion>();
                    if(tapping && paw!=null && paw.Distance<.028f)
                    {
                        var left=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-hand.L");var right=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-hand.R");
                        var bounds=prop.GetComponentInChildren<Renderer>().bounds;
                        strike=Vector3.Distance(left.position,bounds.ClosestPoint(left.position))<Vector3.Distance(right.position,bounds.ClosestPoint(right.position))?left.position:right.position;
                        away=Vector3.ProjectOnPlane(bounds.center-strike,Vector3.up).normalized;
                    }
                    var delta=Vector3.ProjectOnPlane(prop.position-previous,Vector3.up);
                    if(!tapping && delta.sqrMagnitude>.0000001f && away.sqrMagnitude>.5f)
                    {
                        dot=Mathf.Min(dot,Vector3.Dot(delta.normalized,away));samples++;
                        // Cart wheels project the diagonal shove onto their axis; loose props do not.
                        if(cart!=null)Assert.That(Mathf.Abs(Vector3.Dot(delta.normalized,cart.WorldRollDirection)),Is.GreaterThan(.995f),label+" wheel axis");
                    }
                    maxMotion=Mathf.Max(maxMotion,Vector3.Distance(prop.localPosition,home));
                    if(!paused&&samples>2)
                    {
                        paused=true;float time=Time.timeScale;Time.timeScale=0;yield return new WaitForEndOfFrame();var p=prop.position;var c=cat.transform.position;
                        for(int frame=0;frame<3;frame++)yield return new WaitForEndOfFrame();
                        Assert.That(Vector3.Distance(p,prop.position),Is.LessThan(.0001f),label+" paused prop");Assert.That(Vector3.Distance(c,cat.transform.position),Is.LessThan(.0001f),label+" paused cat");Time.timeScale=time;
                    }
                    previous=prop.position;
                }
                CatActivity.Completed-=handler;
                if(activity.IsRunning)activity.CancelForTransition();
                if(completed!=1 || samples<3 || dot<(cart!=null?.05f:.85f) || maxMotion<.05f)failures.Add(label+" completion="+completed+" samples="+samples+" dot="+dot.ToString("F3")+" motion="+maxMotion.ToString("F3")+" contact="+(knock!=null?knock.MinimumPawDistance:cart.MinimumPawDistance).ToString("F4"));
                Assert.That(Vector3.Distance(prop.localPosition,home),Is.LessThan(.001f),label+" reset");Assert.That(Quaternion.Angle(prop.localRotation,rotation),Is.LessThan(.001f),label+" rotation reset");
                Assert.That(CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position),Is.True,label+" exit");cycles++;
            }
        }
        Assert.That(cycles,Is.EqualTo(40));Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }
}
