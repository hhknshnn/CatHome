using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Deterministic furniture-aware CAT display. Ownership and storage are unchanged.</summary>
[DefaultExecutionOrder(900)]
public sealed class CatRoomArrangement : MonoBehaviour
{
    public const float RearPanelZ=2.72f;
    public static readonly Vector3 RearWallNapPillowPosition=new Vector3(-1.72f,0,2.4387f);
    public struct Pose
    {
        public Vector3 position, entry, exit;
        public float yaw;
        public bool hasExit;
        internal int[] blockedCells;
    }
    sealed class Item
    {
        public HomeProductPlacement product;
        public readonly List<Pose> poses=new List<Pose>();
    }
    readonly Dictionary<string,Item> items=new Dictionary<string,Item>();
    readonly List<HomeProductPlacement> furniture=new List<HomeProductPlacement>();
    readonly List<Vector3> care=new List<Vector3>();
    readonly List<Vector3> protectedPoints=new List<Vector3>();
    readonly bool[] roomGrid=new bool[31*27];
    bool pending=true, cached, applying, preferSaved;
    int visits;
    public string LastFailure { get; private set; }
    public int DisplayedCount { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;
        for(int i=0;i<SceneManager.sceneCount;i++)Request(SceneManager.GetSceneAt(i));
    }
    static void Loaded(Scene scene,LoadSceneMode mode)=>Request(scene);
    public static CatRoomArrangement Request(Scene scene)
    {
        if(!scene.IsValid() || !scene.isLoaded || scene.path!=HomeRoomService.LivingRoomScenePath)return null;
        foreach(var root in scene.GetRootGameObjects())
        {
            var found=root.GetComponentInChildren<CatRoomArrangement>(true);
            if(found!=null){if(!found.applying)found.pending=true;return found;}
        }
        var go=new GameObject("AutomaticCatArrangement");SceneManager.MoveGameObjectToScene(go,scene);
        return go.AddComponent<CatRoomArrangement>();
    }
    public static bool PrepareAddition(string id)
    {
        if(!Application.isPlaying)return true;
        if(!CatCollectionPolicy.IsCatItem(id))return true;
        var scene=SceneManager.GetSceneByPath(HomeRoomService.LivingRoomScenePath);
        // CAT belongs to the living room. Purchases made elsewhere are arranged on entry.
        var layout=Request(scene);return layout==null || layout.Arrange(id);
    }
    void LateUpdate()
    {
        if(!Application.isPlaying || !pending || CatActivity.Active!=null)return;
        pending=false;Arrange(null);
    }
    void CacheRoom()
    {
        if(cached)return;
        items.Clear();furniture.Clear();care.Clear();protectedPoints.Clear();
        foreach(var p in FindObjectsByType<HomeProductPlacement>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(p.gameObject.scene!=gameObject.scene)continue;
            if(CatCollectionPolicy.IsCatItem(p.ProductId))items[p.ProductId]=new Item{product=p};
            else if(HomeStoreService.IsFixedRoomProduct(p.ProductId))
            {
                if(p.ReservesFloorSpace())furniture.Add(p);
                var a=p.GetComponent<CatActivity>();
                if(a!=null && a.RoutineEntryPoint!=null)protectedPoints.Add(a.RoutineEntryPoint.position);
            }
        }
        foreach(var t in FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(t.gameObject.scene!=gameObject.scene)continue;
            if(t.name=="FoodInteractionPoint" || t.name=="WaterInteractionPoint" ||
               t.name=="SleepInteractionPoint" || t.name=="BedInteractionPoint" || t.name=="SofaJumpEntry" || t.name=="TableJumpEntry")
            {care.Add(t.position);protectedPoints.Add(t.position);}
        }
        Physics.SyncTransforms();
        for(int z=0;z<27;z++)for(int x=0;x<31;x++)
        {
            Vector3 p=Grid(x,z);bool clear=CatActivityMotion.IsFloorClear(p,.24f,true);
            foreach(var f in furniture)
                if(HomeProductPlacement.FootprintsOverlap(p,new Vector2(.48f,.48f),0,f.MovableRoot.position,f.Footprint,f.MovableRoot.eulerAngles.y,.02f)){clear=false;break;}
            roomGrid[z*31+x]=clear;
        }
        cached=true;
    }
    public void Invalidate(){cached=false;pending=true;}
    public bool TryPlan(IReadOnlyList<string> ids,out Dictionary<string,Pose> plan)
    {
        CacheRoom();plan=new Dictionary<string,Pose>();
        int displayed=0;foreach(var product in HomeStoreService.Products)
            if(CatCollectionPolicy.IsCatItem(product.Id)&&HomeStoreService.IsOwned(product.Id)&&!HomeStoreService.IsStored(product.Id))displayed++;
        bool currentDisplay=displayed==ids.Count;foreach(string id in ids)currentDisplay&=HomeStoreService.IsOwned(id)&&!HomeStoreService.IsStored(id);
        if(preferSaved!=currentDisplay){foreach(var item in items.Values)item.poses.Clear();preferSaved=currentDisplay;}
        if(ids.Count>CatCollectionPolicy.Capacity)return false;
        var selected=new List<Item>();int beds=0;
        foreach(string id in ids)
        {
            if(!items.TryGetValue(id,out var item))return false;
            if(CatCollectionPolicy.IsBed(id))beds++;
            if(beds>1)return false;
            EnsureCandidates(item);selected.Add(item);
        }
        selected.Sort((a,b)=>{
            int bed=CatCollectionPolicy.IsBed(b.product.ProductId).CompareTo(CatCollectionPolicy.IsBed(a.product.ProductId));
            if(bed!=0)return bed;
            int area=(b.product.Footprint.x*b.product.Footprint.y).CompareTo(a.product.Footprint.x*a.product.Footprint.y);
            return area!=0?area:StringComparer.Ordinal.Compare(a.product.ProductId,b.product.ProductId);
        });
        var chosen=new Pose[selected.Count];visits=0;
        if(!Search(selected,chosen,0))return false;
        for(int i=0;i<selected.Count;i++)plan.Add(selected[i].product.ProductId,chosen[i]);
        return true;
    }
    public bool Arrange(string incoming)
    {
        if(applying)return true;
        if(CatActivity.Active!=null)return false;
        var ids=new List<string>();
        foreach(var p in HomeStoreService.Products)
            if(CatCollectionPolicy.IsCatItem(p.Id) && HomeStoreService.IsOwned(p.Id) && (!HomeStoreService.IsStored(p.Id)||p.Id==incoming))ids.Add(p.Id);
        if(!TryPlan(ids,out var plan)){LastFailure="Bu düzen için yeterli açık alan bulunamadı.";return false;}
        applying=true;
        try
        {
            foreach(var pair in plan)
            {
                var p=items[pair.Key].product;var pose=pair.Value;
                p.MovableRoot.SetPositionAndRotation(pose.position,Quaternion.Euler(0,pose.yaw,0));
                if(!HomeStoreService.TryGetWorldPlacement(pair.Key,out var previous,out float yaw) ||
                   (previous-pose.position).sqrMagnitude>.0001f || Mathf.Abs(Mathf.DeltaAngle(yaw,pose.yaw))>.01f)
                    HomeStoreService.TrySetPlacement(pair.Key,pose.position,pose.yaw);
            }
            DisplayedCount=plan.Count;LastFailure=null;pending=false;
            Physics.SyncTransforms();CatActivityMotion.KeepCatClearAfterPurchase(gameObject.scene);
            return true;
        }
        finally {applying=false;}
    }
    void EnsureCandidates(Item item)
    {
        if(item.poses.Count>0)return;
        var p=item.product;var activity=p.GetComponent<CatActivity>();
        Vector3 localEntry=activity!=null && activity.RoutineEntryPoint!=null?
            p.MovableRoot.InverseTransformPoint(activity.RoutineEntryPoint.position):Vector3.back*(p.Footprint.y*.5f+.36f);
        var tunnel=activity as CatEnrichmentActivity;
        bool hasExit=tunnel!=null && tunnel.Mode==CatEnrichmentMode.Tunnel && tunnel.ExitPoint!=null;
        Vector3 localExit=hasExit?p.MovableRoot.InverseTransformPoint(tunnel.ExitPoint.position):Vector3.zero;
        // Five preferred display bays; a compact grid supplies alternatives for wider products.
        var points=new List<Vector3>{new Vector3(-1.8f,0,.85f),new Vector3(-1.8f,0,.55f),new Vector3(-1.85f,0,-1.8f),
            new Vector3(-.35f,0,-1.5f),new Vector3(1.5f,0,-1.85f),new Vector3(-1.8f,0,-.4f),new Vector3(.10f,0,.35f)};
        // Enclosed beds keep the rear care aisle open from their front-left bay.
        if(CatCollectionPolicy.IsBed(p.ProductId)&&p.ProductId!=HomeStoreService.NapPillowId)points.Insert(0,new Vector3(-1.85f,0,-1.8f));
        if(p.ProductId==HomeStoreService.NapPillowId)points.Insert(0,RearWallNapPillowPosition);
        if(p.ProductId==HomeStoreService.BallBasketId)points.Insert(0,new Vector3(-1.65f,0,-1.75f));
        for(int z=0;z<8;z++)for(int x=0;x<16;x++)points.Add(new Vector3(-2.35f+x*.35f,0,.6f-z*.35f));
        // The basket is used from its room-facing side, clear of the joystick.
        var yaws=p.ProductId==HomeStoreService.BallBasketId?new[]{180f,0f,90f,270f}:new[]{0f,180f,90f,270f};
        // Relocating the pad must not shuffle the user's other four displays.
        // A saved pose is preferred only when all normal clearance tests pass.
        if(preferSaved && p.ProductId!=HomeStoreService.NapPillowId && HomeStoreService.TryGetWorldPlacement(p.ProductId,out var savedPosition,out float savedYaw))
        {
            points.Insert(0,savedPosition);
            var ordered=new List<float>{savedYaw};foreach(float yaw in yaws)if(Mathf.Abs(Mathf.DeltaAngle(savedYaw,yaw))>.1f)ordered.Add(yaw);
            yaws=ordered.ToArray();
        }
        foreach(var point in points)foreach(float yaw in yaws)
        {
            // From the approved front camera this rear/right pocket is hidden
            // by the permanent coffee table even when its floor is clear.
            if(point.x>.45f && point.z>-.70f)continue;
            var rot=Quaternion.Euler(0,yaw,0);
            var candidate=new Pose{position=point,yaw=yaw,entry=point+rot*localEntry,exit=point+rot*localExit,hasExit=hasExit};
            // The grid may find a clear cell beside an entrance that itself is
            // too close to a wall. Test the actual standing capsule at both ends.
            if(!IsEntranceClear(candidate.entry) || (hasExit && !IsEntranceClear(candidate.exit)))continue;
            bool clear=true;
            foreach(var protect in protectedPoints)
                if(HomeProductPlacement.FootprintsOverlap(point,p.Footprint,yaw,protect,new Vector2(.56f,.56f),0,.06f)){clear=false;break;}
            if(!clear || !p.IsRoomPoseValid(point,yaw))continue;
            var blocked=new List<int>();
            for(int i=0;i<roomGrid.Length;i++)if(roomGrid[i] && HomeProductPlacement.FootprintsOverlap(Grid(i%31,i/31),new Vector2(.48f,.48f),0,point,p.Footprint,yaw,.01f))blocked.Add(i);
            candidate.blockedCells=blocked.ToArray();
            item.poses.Add(candidate);
        }
        Physics.SyncTransforms();
    }
    bool IsEntranceClear(Vector3 point)
    {
        if(!CatActivityMotion.IsFloorClear(point,.27f,true))return false;
        foreach(var f in furniture)
            if(HomeProductPlacement.FootprintsOverlap(point,new Vector2(.54f,.54f),0,
                f.MovableRoot.position,f.Footprint,f.MovableRoot.eulerAngles.y,.01f))return false;
        return true;
    }
    bool Search(List<Item> selected,Pose[] chosen,int depth)
    {
        if(++visits>40000)return false;
        if(depth==selected.Count)return Connected(selected,chosen);
        var item=selected[depth];
        foreach(var pose in item.poses)
        {
            bool clear=true;
            for(int j=0;j<depth;j++)if(Conflict(item.product,pose,selected[j].product,chosen[j])){clear=false;break;}
            if(!clear)continue;
            chosen[depth]=pose;if(Search(selected,chosen,depth+1))return true;
        }
        return false;
    }
    static bool Conflict(HomeProductPlacement a,Pose ap,HomeProductPlacement b,Pose bp)
    {
        if(HomeProductPlacement.FootprintsOverlap(ap.position,a.Footprint,ap.yaw,bp.position,b.Footprint,bp.yaw,.40f))return true;
        var clearance=new Vector2(.56f,.56f);
        if(HomeProductPlacement.FootprintsOverlap(ap.entry,clearance,0,bp.position,b.Footprint,bp.yaw,.04f) ||
           HomeProductPlacement.FootprintsOverlap(bp.entry,clearance,0,ap.position,a.Footprint,ap.yaw,.04f))return true;
        return (ap.hasExit && HomeProductPlacement.FootprintsOverlap(ap.exit,clearance,0,bp.position,b.Footprint,bp.yaw,.04f)) ||
               (bp.hasExit && HomeProductPlacement.FootprintsOverlap(bp.exit,clearance,0,ap.position,a.Footprint,ap.yaw,.04f));
    }
    static Vector3 Grid(int x,int z)=>new Vector3(-3.6f+x*.24f,0,-3.0f+z*.24f);
    bool Connected(List<Item> selected,Pose[] poses)
    {
        var open=(bool[])roomGrid.Clone();
        foreach(var pose in poses)foreach(int cell in pose.blockedCells)open[cell]=false;
        var targets=new List<Vector3>(care);
        foreach(var pose in poses){targets.Add(pose.entry);if(pose.hasExit)targets.Add(pose.exit);}
        if(targets.Count==0)return true;
        int seed=NearestOpen(targets[0],open);if(seed<0)return false;
        var seen=new bool[open.Length];var queue=new Queue<int>();queue.Enqueue(seed);seen[seed]=true;
        while(queue.Count>0)
        {
            int current=queue.Dequeue(),x=current%31,z=current/31;
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
            {
                int nx=x+dx,nz=z+dz;if(nx<0||nx>=31||nz<0||nz>=27)continue;
                int n=nz*31+nx;if(!open[n]||seen[n])continue;
                if(dx!=0 && dz!=0 && (!open[z*31+nx]||!open[nz*31+x]))continue;
                seen[n]=true;queue.Enqueue(n);
            }
        }
        foreach(var target in targets){int n=NearestOpen(target,open);if(n<0||!seen[n])return false;}
        for(int i=0;i<selected.Count;i++)
            if(selected[i].product.ProductId==HomeStoreService.BallBasketId && !HasBallLane(poses[i],open))return false;
        return true;
    }
    static bool HasBallLane(Pose pose,bool[] open)
    {
        Vector3 preferred=pose.entry-pose.position;preferred.y=0;preferred.Normalize();
        for(int turn=0;turn<8;turn++)
        {
            Vector3 direction=Quaternion.Euler(0,turn*45,0)*preferred;bool clear=true;
            for(int step=0;step<=13;step++)
                if(NearestOpen(pose.entry+direction*(step*.1f),open)<0){clear=false;break;}
            if(clear)return true;
        }
        return false;
    }
    static int NearestOpen(Vector3 p,bool[] open)
    {
        int best=-1;float distance=.30f*.30f;
        int cx=Mathf.RoundToInt((p.x+3.6f)/.24f),cz=Mathf.RoundToInt((p.z+3f)/.24f);
        for(int z=Mathf.Max(0,cz-2);z<=Mathf.Min(26,cz+2);z++)for(int x=Mathf.Max(0,cx-2);x<=Mathf.Min(30,cx+2);x++)
        {
            int i=z*31+x;if(!open[i])continue;
            Vector3 delta=Grid(i%31,i/31)-p;delta.y=0;
            if(delta.sqrMagnitude<distance){distance=delta.sqrMagnitude;best=i;}
        }
        return best;
    }
}

