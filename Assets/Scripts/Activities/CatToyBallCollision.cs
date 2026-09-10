using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>A query-only copy of visible furniture. Thin walkable toys still stop a ball.</summary>
public sealed class CatToyBallCollision : IDisposable
{
    readonly Scene scene;
    readonly PhysicsScene physics;
    readonly GameObject root;
    public const float Radius=.09f;
    const float Skin=.004f;

    public CatToyBallCollision(Scene room, Transform ball)
    {
        // Disposing a preflight query unloads asynchronously in Play mode. The
        // actual game can create its own isolated query before that completes.
        scene=SceneManager.CreateScene("Ball collision queries "+Guid.NewGuid().ToString("N"),new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        physics=scene.GetPhysicsScene();root=new GameObject("Ball query geometry");
        root.hideFlags=HideFlags.HideAndDontSave;SceneManager.MoveGameObjectToScene(root,scene);
        // Visual FBXs include rugs and low toys deliberately walkable by the cat.
        foreach(var placement in UnityEngine.Object.FindObjectsByType<HomeProductPlacement>(FindObjectsSortMode.None))
        {
            if(placement.gameObject.scene!=room)continue;
            foreach(var filter in placement.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<Renderer>();
                if(filter.sharedMesh==null||renderer==null||!renderer.enabled||filter.transform==ball||filter.transform.IsChildOf(ball))continue;
                var copy=CopyTransform(filter.transform);copy.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
            }
        }
        foreach(var collider in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if(collider.gameObject.scene!=room||!collider.enabled||collider.isTrigger||collider is CharacterController||
               collider.GetComponentInParent<CatMovement>()!=null||collider.GetComponentInParent<HomeProductPlacement>()!=null||
               collider.transform==ball||collider.transform.IsChildOf(ball))continue;
            var copy=CopyTransform(collider.transform);
            if(collider is MeshCollider m)copy.AddComponent<MeshCollider>().sharedMesh=m.sharedMesh;
            else if(collider is BoxCollider b){var c=copy.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;}
            else if(collider is SphereCollider s){var c=copy.AddComponent<SphereCollider>();c.center=s.center;c.radius=s.radius;}
            else if(collider is CapsuleCollider cap){var c=copy.AddComponent<CapsuleCollider>();c.center=cap.center;c.radius=cap.radius;c.height=cap.height;c.direction=cap.direction;}
        }
        Physics.SyncTransforms();
    }
    GameObject CopyTransform(Transform source)
    {
        var copy=new GameObject(source.name);copy.transform.SetParent(root.transform,false);
        copy.transform.SetPositionAndRotation(source.position,source.rotation);copy.transform.localScale=source.lossyScale;return copy;
    }
    public Vector3 OnFloor(Vector3 floor)
    {
        // Only the contact plane immediately under the ball; never snap onto a tall table.
        float y=floor.y;
        if(physics.Raycast(floor+Vector3.up*.18f,Vector3.down,out var hit,.30f,~0,QueryTriggerInteraction.Ignore))y=hit.point.y;
        return new Vector3(floor.x,y+Radius+Skin,floor.z);
    }
    public bool ClearRoll(Vector3 floor,Vector3 end)
    {
        Vector3 from=OnFloor(floor),delta=end-floor;
        return !physics.SphereCast(from,Radius,delta.normalized,out _,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
    }
    public Vector3 Sweep(Vector3 from,Vector3 displacement,out bool blocked)
    {
        blocked=false;float distance=displacement.magnitude;
        if(distance<.000001f)return from;
        if(physics.SphereCast(from,Radius,displacement/distance,out var hit,distance+Skin,~0,QueryTriggerInteraction.Ignore))
        {blocked=true;return from+displacement/distance*Mathf.Max(0,hit.distance-Skin);}
        return from+displacement;
    }
    public void Dispose()
    {
        if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);}
        if(!scene.IsValid()||!scene.isLoaded)return;
#if UNITY_EDITOR
        if(!Application.isPlaying){UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);return;}
#endif
        SceneManager.UnloadSceneAsync(scene);
    }
}
