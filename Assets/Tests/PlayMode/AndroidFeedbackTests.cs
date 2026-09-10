using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AndroidFeedbackTests
{
    [TearDown] public void Cleanup() => RoomPlayModeSupport.ReleaseRoom();

    [UnityTest]
    public IEnumerator ClosedRoomDoorsStopTheActualCharacterController()
    {
        int checkedDoors=0;
        foreach(var room in HomeRoomService.Rooms)
        {
            if(room.Id==HomeRoomService.LivingRoomId)continue;
            yield return RoomPlayModeSupport.LoadRoomAlone(System.IO.Path.GetFileNameWithoutExtension(room.ScenePath));
            var door=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="RoomDoor_Premium");
            if(door==null)continue;
            var obstacle=door.GetComponent<BoxCollider>();
            Assert.That(obstacle,Is.Not.Null,room.Id);
            var cat=Object.FindFirstObjectByType<CatMovement>();
            cat.enabled=false;
            var controller=cat.GetComponent<CharacterController>();
            controller.enabled=false;
            cat.transform.position=new Vector3(obstacle.bounds.center.x,.05f,obstacle.bounds.min.z-.8f);
            controller.enabled=true;Physics.SyncTransforms();
            for(int i=0;i<25;i++)controller.Move(new Vector3(0,-.02f,.1f));
            Assert.That(controller.bounds.max.z,Is.LessThanOrEqualTo(obstacle.bounds.min.z+.035f),room.Id+" walked into the door");
            controller.Move(Vector3.back*.6f);
            Assert.That(controller.bounds.max.z,Is.LessThan(obstacle.bounds.min.z-.2f),room.Id+" cannot walk away");
            var clearPose=cat.transform.position;
            cat.ApplySavedWorldPose(clearPose,Quaternion.identity);
            Assert.That(Vector3.Distance(cat.transform.position,clearPose),Is.LessThan(.001f),room.Id+" moved a valid save");
            foreach(float depth in new[]{-.10f,0f,.20f})
            {
                cat.ApplySavedWorldPose(new Vector3(obstacle.bounds.center.x,.05f,obstacle.bounds.center.z+depth),Quaternion.identity);
                Physics.SyncTransforms();
                Assert.That(controller.bounds.max.z,Is.LessThan(obstacle.bounds.min.z),room.Id+" restored inside the door");
                float restoredZ=cat.transform.position.z;
                controller.Move(Vector3.back*.4f);
                Assert.That(cat.transform.position.z,Is.LessThan(restoredZ-.3f),room.Id+" trapped the legacy save");
            }
            checkedDoors++;
        }
        Assert.That(checkedDoors,Is.EqualTo(3));
    }

    [UnityTest]
    public IEnumerator HomeAudioHasNoRepeatingOrPointerPlopSources()
    {
        yield return null;
        var home=Object.FindFirstObjectByType<HomeAudioController>();
        Assert.That(home,Is.Not.Null);
        var sources=home.GetComponents<AudioSource>();
        Assert.That(sources.Length,Is.EqualTo(1));
        Assert.That(sources[0].loop,Is.False);
        Assert.That(sources[0].clip,Is.Null);
        Assert.That(typeof(HomeAudioController).GetMethod("DetectUiClick",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance),Is.Null);
    }
}
