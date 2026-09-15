using System.Collections;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed partial class JumpContinuityTests
{
    [UnityTest] public IEnumerator CoffeeTable_TenBreedsAndFrameRates_UseShortFlowingTurn()
    {
        yield return Prepare("LivingRoom_Level01", HomeRoomService.LivingRoomId);
        var activity = CatActivity.Registered.Single(a => a.Kind == CatActivityKind.CoffeeTablePlay);
        foreach (var breed in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(breed.Id); yield return null; yield return null;
            foreach (int fps in new[]{15,30,60})
            {
                Time.captureFramerate = fps;
                string id = "coffee-" + breed.Id + "-" + fps;
                yield return Record(HomeRoomService.LivingRoomId, id, activity, true);
                var trace = JsonUtility.FromJson<Trace>(File.ReadAllText(Folder + "/" + id + "-motion.json"));
                VerifyDirectTurn(trace);
            }
        }
    }

    [UnityTest] public IEnumerator CoffeeTable_RecordsSupportedTurn()
    {
        yield return Prepare("LivingRoom_Level01", HomeRoomService.LivingRoomId);
        var activity = CatActivity.Registered.Single(a => a.Kind == CatActivityKind.CoffeeTablePlay);
        var capture = cat.StartCoroutine(CaptureTurn(activity));
        try { yield return Record(HomeRoomService.LivingRoomId, "CoffeeTablePlay", activity, true); }
        finally { cat.StopCoroutine(capture); }
    }

    IEnumerator CaptureTurn(CatActivity activity)
    {
        int captured=0;float next=0;var previous=cat.transform.rotation;
        while (true)
        {
            yield return new WaitForEndOfFrame();
            var animation=cat.GetComponent<CatActivityAnimation>();
            bool turn=cat.transform.position.y>.12f&&Quaternion.Angle(previous,cat.transform.rotation)>.5f&&
                ((animation.IsNativeJump&&animation.NativeJumpPhase>=.99f)||animation.CurrentPose==CatActivityPose.StandUp);
            previous=cat.transform.rotation;
            if(!turn||Time.time<next)continue;
            next=Time.time+.08f;captured++;
            Directory.CreateDirectory(Folder + "/coffee-visuals");
            System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("RoomInteractionReview")).First(t => t != null)
                .GetMethod("ActivityDetail", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[]{Folder + "/coffee-visuals/turn-" + captured + ".png", activity, cat});
        }
    }
}
