using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class BallCollisionTests
{
    [UnityTest]
    public IEnumerator ThinWalkableToyAndTallFurniture_StopAHighSpeedBallWithoutChangingCatColliders()
    {
        var room=SceneManager.CreateScene("Ball collision regression");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(floor,room);
        floor.transform.position=new Vector3(0,-.10f,0);floor.transform.localScale=new Vector3(5,.20f,5);
        var toy=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(toy,room);
        toy.AddComponent<HomeProductPlacement>().enabled=false;
        toy.transform.position=new Vector3(0,.06f,.65f);toy.transform.localScale=new Vector3(.55f,.12f,.42f);
        toy.GetComponent<BoxCollider>().isTrigger=true;
        var chair=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(chair,room);
        chair.transform.position=new Vector3(1,.55f,.65f);chair.transform.localScale=new Vector3(.60f,1.1f,.42f);
        var ball=new GameObject("Ball query caller");SceneManager.MoveGameObjectToScene(ball,room);
        Physics.SyncTransforms();yield return null;
        using(var world=new CatToyBallCollision(room,ball.transform))
        {
            var start=world.OnFloor(Vector3.zero);
            Assert.That(world.ClearRoll(Vector3.zero,Vector3.forward*1.5f),Is.False,"A walkable trigger toy still has visible solid geometry");
            var stopped=world.Sweep(start,Vector3.forward*4f,out bool blocked);
            Assert.That(blocked,Is.True);Assert.That(stopped.z,Is.InRange(0,.36f),"Sweep must not tunnel even at a 4m frame step");
            Assert.That(world.ClearRoll(Vector3.right,Vector3.right+Vector3.forward*1.5f),Is.False,"Chair body");
            world.Sweep(new Vector3(1,1.3f,0),Vector3.forward*1.5f,out blocked);
            Assert.That(blocked,Is.False,"An actual flight above the furniture stays possible");
            Assert.That(world.ClearRoll(Vector3.left,Vector3.left+Vector3.forward*1.5f),Is.True,"Open floor must remain usable");
            Assert.That(toy.GetComponent<BoxCollider>().isTrigger,Is.True,"Do not block the cat with the toy's box");
            Assert.That(toy.GetComponent<MeshCollider>(),Is.Null,"Query geometry never changes the room");
        }
        yield return SceneManager.UnloadSceneAsync(room);
    }
}
