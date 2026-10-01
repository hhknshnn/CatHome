using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Measured architectural finish; never rebuilds or scales catalog products.</summary>
public static class HomeRoomPremiumFinishBuilder
{
    public const string RootName = "PremiumRoomFinish";
    const string Folder = "Assets/Art/RoomShellPolish";

    public static string ApplyAll()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        var setup = EditorSceneManager.GetSceneManagerSetup(); int count = 0;
        try
        {
            foreach (var room in HomeRoomService.Rooms)
            {
                if (room.Id == HomeRoomService.LivingRoomId) continue;
                var scene = SceneManager.GetSceneByPath(room.ScenePath);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(room.ScenePath, OpenSceneMode.Additive);
                Apply(scene, room.Id);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); count++;
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        return count + " room shells finished; product poses, scales and collision preserved.";
    }

    public static void Apply(Scene scene, string roomId)
    {
        if (roomId == HomeRoomService.LivingRoomId || !scene.IsValid()) return;
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (var old in all.Where(t => t != null && (t.name == RootName || t.name == "PremiumFoliage")))
            if(old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var environment = all.FirstOrDefault(t => t.name == "01 Environment");
        var root = new GameObject(RootName).transform;
        SceneManager.MoveGameObjectToScene(root.gameObject, scene);
        if (environment != null) root.SetParent(environment, false);

        var ivory = Material("Ivory", new Color32(250,243,226,255), .38f);
        var brass = Material("Champagne", new Color32(218,178,104,255), .52f, .30f);
        var sage = Material("Sage", new Color32(157,198,172,255), .30f);
        var leaf = Material("Leaf", new Color32(114,176,134,255), .24f);
        var leafDark = Material("LeafShade", new Color32(76,136,109,255), .23f);
        bool outdoor = roomId == HomeRoomService.GardenId || roomId == HomeRoomService.PatioId || roomId == HomeRoomService.BalconyId;
        var originals=new Dictionary<string,Material>(StringComparer.Ordinal);

        // Material instances belong to architecture only. No shared ROOM/CAT material is recolored.
        foreach (var r in all.Select(t => t.GetComponent<MeshRenderer>()).Where(r => r != null).ToArray())
        {
            if (r.GetComponentInParent<StoreProductDisplay>() != null || r.GetComponentInParent<CatMovement>() != null) continue;
            if (r.GetComponentInParent<CatActivity>() != null || r.transform.IsChildOf(root)) continue;
            string n = r.name.ToLowerInvariant();
            if (n.Contains("sky") || n.Contains("cloud") || n.Contains("sun") || n.Contains("glass") || n.Contains("pane")) continue;
            var mats = r.sharedMaterials;
            for (int i=0;i<mats.Length;i++)
            {
                if (mats[i] == null) continue;
                var sourceMaterial=ModernWorldArtBuilder.ResolveSourceMaterial(mats[i]);
                if(sourceMaterial.shader!=null && sourceMaterial.shader.name==ModernWorldArtBuilder.ShaderName)continue;
                string originalName=sourceMaterial.name;
                while(originalName.StartsWith("Finish_Shell_",StringComparison.Ordinal))originalName=originalName.Substring("Finish_Shell_".Length);
                if(!originals.TryGetValue(originalName,out var original))
                {
                    original=AssetDatabase.FindAssets(originalName+" t:Material")
                        .Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                        .FirstOrDefault(m=>m!=null && m.name==originalName);
                    if(original==null)original=sourceMaterial;
                    originals.Add(originalName,original);
                }
                string m = originalName.ToLowerInvariant();
                Color color = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : Color.white;
                if (m.Contains("woodlight")) color = new Color32(223,201,171,255);
                else if (m.Contains("wood")) color = new Color32(206,178,139,255);
                else if (m.Contains("grassdeep")) color = new Color32(91,145,102,255);
                else if (m.Contains("grasslight")) color = new Color32(158,191,126,255);
                else if (m.Contains("grass")) color = new Color32(132,171,113,255);
                else if (m.Contains("stone") || m.Contains("pav")) color = Color.Lerp(color,new Color32(229,219,197,255),.72f);
                else if (m.Contains("gold")) color = new Color32(218,178,104,255);
                else if (m.Contains("cream") || m.Contains("white") || m.Contains("pearl")) color = new Color32(250,243,226,255);
                else if (m.Contains("wall")) color = Color.Lerp(color,new Color32(220,226,212,255),.32f);
                else color = Color.Lerp(color,new Color32(233,228,210,255),.18f);
                mats[i] = Material("Shell_"+originalName,color,m.Contains("grass")?.16f:.34f);
            }
            r.sharedMaterials = mats;
            if (n.Contains("floor") || n.Contains("tile") || n.Contains("plank") || n.Contains("grass") || n.Contains("pav")) r.receiveShadows=true;

            // New leaf silhouettes are fitted to the exact previous visual bounds.
            bool foliage = n == "crownlow" || n == "crownhigh" || n == "foliage" || n == "foliagehigh" || n == "bush" || n.StartsWith("topiaryback_");
            if (foliage && (r.enabled || r.GetComponent<RoomArchitectureVisualSource>() != null))
            {
                Bounds b=r.bounds; r.enabled=false;
                if(r.GetComponent<RoomArchitectureVisualSource>()==null)r.gameObject.AddComponent<RoomArchitectureVisualSource>();
                var plant=Model(r.transform,"RoomFoliage_Premium",b.center,Quaternion.identity,leafDark,brass,leaf);
                plant.name="PremiumFoliage";
                FitCentered(plant,b.size); plant.gameObject.AddComponent<RoomFoliageMotion>();
            }
            if(n.StartsWith("hedge") && (r.enabled || r.GetComponent<RoomArchitectureVisualSource>() != null))
            {
                Bounds b=r.bounds;r.enabled=false;
                if(r.GetComponent<RoomArchitectureVisualSource>()==null)r.gameObject.AddComponent<RoomArchitectureVisualSource>();
                bool alongX=b.size.x>b.size.z;int count=Mathf.CeilToInt(Mathf.Max(b.size.x,b.size.z)/.70f);
                for(int i=0;i<count;i++)
                {
                    Vector3 center=b.center;Vector3 size=b.size;
                    if(alongX){size.x=b.size.x/count;center.x=b.min.x+size.x*(i+.5f);}
                    else{size.z=b.size.z/count;center.z=b.min.z+size.z*(i+.5f);}
                    var plant=Model(r.transform,"RoomFoliage_Premium",center,Quaternion.identity,leafDark,brass,leaf);
                    plant.name="PremiumFoliage";FitCentered(plant,size);
                }
            }
        }

        // Door frame and handle keep the same architectural position and opening reservation.
        var door=all.FirstOrDefault(t=>t.name=="DoorPanel");
        if(door!=null)
        {
            foreach(var renderer in door.parent.GetComponentsInChildren<MeshRenderer>(true))
                if(renderer.name.StartsWith("Door",StringComparison.Ordinal))renderer.enabled=false;
            var model=Model(root,"RoomDoor_Premium",new Vector3(door.position.x,0,2.52f),Quaternion.identity,ivory,brass,sage);
            EnsureDoorCollision(model);
        }

        if(!outdoor)
        {
            for(int i=0;i<7;i++)
            {
                float x=-3.18f+i*1.06f;
                if(door!=null && Mathf.Abs(x-door.position.x)<1.36f)continue;
                Model(root,"RoomWallPanel_Premium",new Vector3(x,.64f,2.695f),Quaternion.identity,ivory,brass,sage);
            }
            for(int side=-1;side<=1;side+=2)
                for(int i=0;i<5;i++)
                    Model(root,"RoomWallPanel_Premium",new Vector3(side*3.695f,.64f,-2.02f+i*1.07f),Quaternion.Euler(0,side<0?270:90,0),ivory,brass,sage);
        }

        // Match the approved living-room shadowless soft fill, with existing key/fill lights retained.
        if(!all.Any(t=>t.name=="ReferenceSoftFill"))
        {
            var go=new GameObject("ReferenceSoftFill");go.transform.SetParent(root,false);
            go.transform.rotation=Quaternion.Euler(32,170,0);
            var light=go.AddComponent<Light>();light.type=LightType.Directional;
            light.color=new Color(1,.97f,.92f);light.intensity=.38f;light.shadows=LightShadows.None;light.cullingMask=1;
        }
        ModernWorldArtBuilder.Apply(scene, roomId);
    }

    public static void EnsureDoorCollision(Transform door)
    {
        // The visible closed door sits in front of the structural back wall.
        // Its measured solid volume must stop walking before the cat enters it.
        var filters=door.GetComponentsInChildren<MeshFilter>(true);
        Bounds bounds=new Bounds();bool found=false;
        foreach(var filter in filters)
        {
            if(filter.sharedMesh==null)continue;
            var b=filter.sharedMesh.bounds;
            for(int i=0;i<8;i++)
            {
                var p=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                p=door.InverseTransformPoint(filter.transform.TransformPoint(p));
                if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);
            }
        }
        if(!found)throw new InvalidOperationException("Door has no measurable mesh.");
        var collider=door.GetComponent<BoxCollider>();if(collider==null)collider=door.gameObject.AddComponent<BoxCollider>();
        collider.center=bounds.center;collider.size=bounds.size;collider.isTrigger=false;collider.enabled=true;
        if(door.GetComponent<RoomDoorObstacle>()==null)door.gameObject.AddComponent<RoomDoorObstacle>();
    }

    static Transform Model(Transform parent,string name,Vector3 position,Quaternion rotation,Material ivory,Material brass,Material mint)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Models/"+name+".fbx");
        if(prefab==null)throw new InvalidOperationException("Missing architectural model: "+name);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
        instance.name=name;instance.transform.SetPositionAndRotation(position,rotation);
        foreach(var renderer in instance.GetComponentsInChildren<Renderer>())
        {
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
                materials[i]=name=="RoomFoliage_Premium" && materials[i]!=null && materials[i].name.Contains("Teal")?ivory:
                    materials[i]!=null&&materials[i].name.Contains("Gold")?brass:
                    materials[i]!=null&&(materials[i].name.Contains("Mint")||materials[i].name.Contains("Teal"))?mint:ivory;
            renderer.sharedMaterials=materials;renderer.receiveShadows=true;
        }
        if (name == "RoomWallPanel_Premium")
            HomeEnvironmentCollisionBuilder.EnsurePremiumWallPanel(instance.transform);
        return instance.transform;
    }
    static void FitCentered(Transform root,Vector3 target)
    {
        var renderers=root.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
        foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
        root.localScale=Vector3.Scale(root.localScale,new Vector3(target.x/bounds.size.x,target.y/bounds.size.y,target.z/bounds.size.z));
    }
    static Material Material(string name,Color color,float smooth,float metallic=0)
    {
        string path=Folder+"/Materials/Finish_"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",smooth);material.SetFloat("_Metallic",metallic);
        EditorUtility.SetDirty(material);return material;
    }
}
