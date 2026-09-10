using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class HomeRoomPremiumFinishTests
{
    [Test]
    public void RecordSpindle_KeepsTheDiscHorizontalWhilePaperKeepsItsRollAxis()
    {
        foreach(var name in new[]{"LoftRecordPlayer","BathroomToilet"})
        {
            string path="Assets/Art/StoreProducts/Prefabs/"+name+".prefab";
            var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
            try
            {
                var activity=root.GetComponent<PaperSpinActivity>();
                Assert.That(activity,Is.Not.Null,name);
                Vector3 axis=name=="LoftRecordPlayer"?Vector3.up:Vector3.right;
                Vector3 before=activity.RollPivot.TransformDirection(axis);
                typeof(PaperSpinActivity).GetMethod("SpinDown",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
                    .Invoke(activity,new object[]{120f,.25f});
                Assert.That(Vector3.Angle(before,activity.RollPivot.TransformDirection(axis)),Is.LessThan(.01f),name);
                Assert.That(Quaternion.Angle(Quaternion.identity,activity.RollPivot.localRotation),Is.GreaterThan(1f),name);
            }
            finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
        }
    }

    [Test]
    public void ReapplyingFinish_PreservesProductScalePlacementAndArchitectureCollision()
    {
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene(HomeRoomService.GardenScenePath,OpenSceneMode.Additive);
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var products=all.Select(t=>t.GetComponent<StoreProductDisplay>()).Where(p=>p!=null).ToDictionary(p=>p,p=>(p.transform.position,p.transform.rotation,p.transform.localScale));
            var colliders=all.Select(t=>t.GetComponent<Collider>()).Where(c=>c!=null).ToDictionary(c=>c,c=>(c.transform.position,c.transform.localScale,c.enabled,c.isTrigger));
            HomeRoomPremiumFinishBuilder.Apply(scene,HomeRoomService.GardenId);
            int renderers=scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Renderer>(true).Length);
            var colors=all.Where(t=>t!=null).Select(t=>t.GetComponent<Renderer>()).Where(r=>r!=null)
                .ToDictionary(r=>r,r=>r.sharedMaterials.Select(m=>m!=null&&m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.clear).ToArray());
            HomeRoomPremiumFinishBuilder.Apply(scene,HomeRoomService.GardenId);
            Assert.That(scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Renderer>(true).Length),Is.EqualTo(renderers));
            foreach(var p in products)Assert.That((p.Key.transform.position,p.Key.transform.rotation,p.Key.transform.localScale),Is.EqualTo(p.Value),p.Key.ProductId);
            foreach(var c in colliders)Assert.That((c.Key.transform.position,c.Key.transform.localScale,c.Key.enabled,c.Key.isTrigger),Is.EqualTo(c.Value),c.Key.name);
            foreach(var c in colors)if(c.Key!=null)Assert.That(c.Key.sharedMaterials.Select(m=>m!=null&&m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.clear).ToArray(),Is.EqualTo(c.Value),"Colors must not fade on repeated application");
            var obstacle=all.First(t=>t.name=="Courtyard Tree");
            Assert.That(obstacle.GetComponentsInChildren<Renderer>().Count(r=>r.enabled),Is.GreaterThan(1),"New foliage remains under the measured obstacle");
            EditorSceneManager.CloseScene(scene,true);
        }
        finally
        {
            if(setup.Any(s=>s.isLoaded) && setup.Count(s=>s.isActive)==1)EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }

    [Test]
    public void UnknownFutureRoom_UsesTheSharedFinish()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        try
        {
            HomeRoomPremiumFinishBuilder.Apply(scene,"future-room-test");
            Assert.That(scene.GetRootGameObjects().Count(r=>r.name==HomeRoomPremiumFinishBuilder.RootName),Is.EqualTo(1));
            Assert.That(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>()).Count(),Is.GreaterThan(10));
            Assert.That(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Collider>()).Count(),Is.Zero);
        }
        finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
    }
}
