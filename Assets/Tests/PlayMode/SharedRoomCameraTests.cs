using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SharedRoomCameraTests
{
    [TearDown] public void After() => RoomPlayModeSupport.ReleaseRoom();

    [UnityTest] public IEnumerator AllRooms_KeepTheLivingRoomViewAndNavigationStripAfterLoading()
    {
        foreach (var room in HomeRoomService.Rooms)
        {
            yield return RoomPlayModeSupport.LoadRoomAlone(room.SceneName);
            yield return null;
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.enabled).ToArray();
            Assert.That(cameras.Length, Is.EqualTo(1), room.Id);
            var camera = cameras[0];
            Assert.That(Vector3.Distance(camera.transform.position, HomeRoomCameraProfile.Position), Is.LessThan(.001f), room.Id);
            Assert.That(Quaternion.Angle(camera.transform.rotation, Quaternion.Euler(HomeRoomCameraProfile.Angles)), Is.LessThan(.01f), room.Id);
            Assert.That(camera.GetComponent<HomeWorldViewport>(), Is.Not.Null, room.Id);
            Assert.That(camera.rect.yMin, Is.GreaterThan(0), "The dock must not cover the room: " + room.Id);
            float aspect = Screen.width / (Screen.height * camera.rect.height);
            Assert.That(camera.fieldOfView, Is.EqualTo(HomeWorldViewport.FitFieldOfView(HomeRoomCameraProfile.FieldOfView, aspect)).Within(.01f), room.Id);
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x => x.enabled), Is.EqualTo(1), room.Id);
        }
    }

    [UnityTest] public IEnumerator NewUnlistedRoom_InheritsTheProfileWhenItsMarkerIsEnabled()
    {
        var scene = SceneManager.CreateScene("Future room camera QA");
        var host = new GameObject("Future room");SceneManager.MoveGameObjectToScene(host, scene);
        var cameraHost = new GameObject("Future room camera", typeof(Camera));cameraHost.transform.SetParent(host.transform);
        var camera = cameraHost.GetComponent<Camera>();var marker = host.AddComponent<HomeRoomSceneMarker>();
        var unrelated = new GameObject("Unrelated cinematic", typeof(Camera));var other = unrelated.GetComponent<Camera>();
        other.fieldOfView = 57;other.transform.position = new Vector3(20, 10, 5);
        try
        {
            marker.EditorConfigure("future-room-not-in-catalog", camera, null);
            host.SetActive(false);
            camera.transform.SetPositionAndRotation(new Vector3(-4, 8, -9), Quaternion.Euler(45, 30, 0));
            camera.orthographic = true;camera.fieldOfView = 60;
            host.SetActive(true);yield return null;
            Assert.That(camera.transform.position, Is.EqualTo(HomeRoomCameraProfile.Position));
            Assert.That(Quaternion.Angle(camera.transform.rotation, Quaternion.Euler(HomeRoomCameraProfile.Angles)), Is.LessThan(.01f));
            Assert.That(camera.orthographic, Is.False);Assert.That(camera.GetComponents<HomeWorldViewport>().Length, Is.EqualTo(1));
            HomeRoomCameraProfile.Apply(camera);
            Assert.That(camera.GetComponents<HomeWorldViewport>().Length, Is.EqualTo(1), "Repeated loads do not duplicate the viewport.");
            Assert.That(other.fieldOfView, Is.EqualTo(57));Assert.That(other.transform.position, Is.EqualTo(new Vector3(20, 10, 5)), "Cinematic and minigame cameras are independent.");
        }
        finally { Object.DestroyImmediate(host);Object.DestroyImmediate(unrelated);SceneManager.UnloadSceneAsync(scene); }
    }
}
