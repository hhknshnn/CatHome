using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Deterministic scene authoring. Existing home scenes and source models are not rebuilt.</summary>
public static class CozyGamesBuilder
{
    public const string Root="Assets/Art/CozyGames";
    static readonly string[] Names={"Sage","Petrol","Mint","Coral","Oak","Honey","Cream","Ink","Pearl","Water","Rug","Paving","Stone","StoneWarm"};
    static readonly string[] Hex={"819B85","315E60","9BC9AE","D78269","AB9983","C3A16A","DBD1B6","29484D","F2E8CF","4F9992","C5BFA5","ABB8AF","D6CFB9","BFB49A"};
    public static string BuildNewGames()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
        EnsureMaterials();
        Build(CozyGameKind.Yarn);Build(CozyGameKind.Pond);
        var scenes=EditorBuildSettings.scenes.ToList();
        foreach(string n in new[]{CozyMiniGame.YarnScene,CozyMiniGame.PondScene}){string p="Assets/Scenes/Cozy/"+n+".unity";if(!scenes.Any(s=>s.path==p))scenes.Add(new EditorBuildSettingsScene(p,true));}
        EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();return "YarnRoute + PondPlay built.";
    }
    public static void EnsureMaterials()
    {
        Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory("Assets/Resources/CozyGames");
        for(int i=0;i<Names.Length;i++)
        {
            string p=Root+"/Materials/"+Names[i]+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}
            ColorUtility.TryParseHtmlString("#"+Hex[i],out var c);m.color=c;m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",Names[i]=="Water"?.72f:.26f);
            m.SetFloat("_Metallic",Names[i]=="Honey"?.10f:0);
            if(Names[i]!="Water")
            {
                m.shader=Shader.Find("CatHome/Modern Surface");
                string surface=Names[i]=="Oak"?"Oak":Names[i]=="Honey"?"Rattan":Names[i]=="Mint"||Names[i]=="Cream"||Names[i]=="Rug"?"Linen":Names[i].StartsWith("Stone")||Names[i]=="Paving"?"Stone":Names[i]=="Coral"?"Ceramic":"Plaster";
                m.SetTexture("_DetailAlbedoMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/ModernPolish/Textures/Modern_"+surface+"_Surface.png"));
                m.SetTextureScale("_DetailAlbedoMap",Vector2.one*(surface=="Oak"?.65f:2));
                m.SetFloat("_DetailAlbedoMapScale",surface=="Oak"?.45f:.20f);m.SetFloat("_DetailNormalMapScale",.00006f);
            }
            else m.shader=Shader.Find("CatHome/Cozy Pond");
            EditorUtility.SetDirty(m);
        }
        string aimPath="Assets/Resources/CozyGames/Aim.mat";var aim=AssetDatabase.LoadAssetAtPath<Material>(aimPath);
        if(aim==null){aim=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(aim,aimPath);}aim.color=new Color(.6f,.95f,.78f);aim.SetColor("_BaseColor",aim.color);
        foreach(string path in Directory.GetFiles(Root+"/Models","*.fbx"))
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(model==null)throw new Exception("Import model: "+path);
            var go=new GameObject(Path.GetFileNameWithoutExtension(path));
            var imported=Object.Instantiate(model,go.transform,false);
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>Mat(m==null?"Cream":m.name.Replace("Cozy_","").Replace(" (Instance)",""))).ToArray();
            PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+go.name+".prefab");Object.DestroyImmediate(go);
        }
    }
    public static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+name+".mat")??AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Cream.mat");
    public static GameObject Model(string name,Transform parent,Vector3 position,Vector3? scale=null)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+name+".prefab");
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.SetParent(parent,false);go.transform.localPosition=position;if(scale.HasValue)go.transform.localScale=scale.Value;return go;
    }
    public static GameObject Shape(string name,Transform parent,Vector3 position,Vector3 scale,string material,PrimitiveType type=PrimitiveType.Cube)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
        Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=Mat(material);return go;
    }
    static Transform Empty(string name,Transform parent,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
    static void Build(CozyGameKind kind)
    {
        string name=kind==CozyGameKind.Yarn?CozyMiniGame.YarnScene:CozyMiniGame.PondScene;
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var root=new GameObject(name+"Root");SceneManager.MoveGameObjectToScene(root,scene);root.transform.position=new Vector3(kind==CozyGameKind.Yarn?100:140,1000,0);
        var game=root.AddComponent<CozyMiniGame>();game.kind=kind;game.actor=Empty("SelectedCat",root.transform,Vector3.zero);game.actor.localScale=Vector3.one*.72f;
        var cam=new GameObject("GameCamera",typeof(Camera),typeof(AudioListener),typeof(UniversalAdditionalCameraData));cam.transform.SetParent(root.transform,false);
        cam.transform.localPosition=new Vector3(0,7.4f,-6.8f);cam.transform.localRotation=Quaternion.LookRotation(new Vector3(0,0,.3f)-cam.transform.localPosition);
        game.gameCamera=cam.GetComponent<Camera>();game.gameCamera.orthographic=true;game.gameCamera.orthographicSize=3.65f;game.gameCamera.nearClipPlane=.1f;game.gameCamera.farClipPlane=35;
        game.gameCamera.clearFlags=CameraClearFlags.SolidColor;game.gameCamera.backgroundColor=new Color32(111,143,132,255);cam.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
        var sun=new GameObject("SoftAfternoon",typeof(Light));sun.transform.SetParent(root.transform,false);sun.transform.localRotation=Quaternion.Euler(48,-35,0);var light=sun.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.05f;light.color=new Color(1,.91f,.77f);light.shadows=LightShadows.Soft;
        var fill=new GameObject("WindowFill",typeof(Light));fill.transform.SetParent(root.transform,false);fill.transform.localPosition=new Vector3(1,5,-2);var fl=fill.GetComponent<Light>();fl.type=LightType.Point;fl.intensity=3;fl.range=12;fl.color=new Color(.73f,.88f,1);fl.shadows=LightShadows.None;
        if(kind==CozyGameKind.Yarn)BuildYarn(root.transform,game);else BuildPond(root.transform,game);
        Directory.CreateDirectory("Assets/Scenes/Cozy");EditorSceneManager.SaveScene(scene,"Assets/Scenes/Cozy/"+name+".unity");EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);
    }
    static void BuildYarn(Transform root,CozyMiniGame game)
    {
        LivingBackdrop(root);
        Shape("LinenBoardEdge",root,new Vector3(0,-.03f,0),new Vector3(5.6f,.14f,4),"Petrol");
        Shape("WovenPlayMat",root,new Vector3(0,.035f,0),new Vector3(5.32f,.02f,3.72f),"Rug");
        for(int i=0;i<36;i++)Shape("WovenThread",root,new Vector3(-2.56f+i*.146f,.048f,0),new Vector3(.008f,.004f,3.65f),"Honey");
        for(int i=0;i<26;i++)Shape("WeftThread",root,new Vector3(0,.049f,-1.75f+i*.14f),new Vector3(5.2f,.004f,.005f),"Cream");
        for(int side=-1;side<=1;side+=2){Shape("StitchedBorder",root,new Vector3(side*2.68f,.10f,0),new Vector3(.07f,.15f,3.88f),"Mint");Shape("StitchedBorder",root,new Vector3(0,.10f,side*1.89f),new Vector3(5.4f,.15f,.07f),"Mint");}
        game.ball=Model("YarnBall",root,new Vector3(-2,.27f,-1)).transform;
        game.basket=Model("Basket",root,new Vector3(2,.04f,1)).transform;
        game.bonus=Shape("PearlBonus",root,new Vector3(0,.14f,1.3f),Vector3.one*.15f,"Pearl",PrimitiveType.Sphere).transform;
        game.obstacleRoot=Empty("PuzzleCushions",root,Vector3.zero);game.cushionPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Cushion.prefab");
    }
    public static void LivingBackdrop(Transform root)
    {
        for(int i=0;i<22;i++)for(int row=-2;row<=2;row++)Shape("OakPlank",root,new Vector3(-7.875f+i*.75f,-.12f,row*2.7f+(i%2)*1.35f),new Vector3(.746f,.15f,2.692f),"Oak");
        Shape("SageWall",root,new Vector3(0,1.8f,3.1f),new Vector3(12,3.8f,.15f),"Sage");
        Shape("PetrolWainscot",root,new Vector3(0,.55f,3),new Vector3(12,1.1f,.12f),"Petrol");
        Shape("DadoRail",root,new Vector3(0,1.13f,2.88f),new Vector3(12,.06f,.12f),"Cream");
        for(int i=-5;i<=5;i++)Shape("PanelStile",root,new Vector3(i,.55f,2.91f),new Vector3(.035f,.92f,.04f),"Mint");
        Shape("WindowFrame",root,new Vector3(-2.5f,2.15f,2.94f),new Vector3(1.45f,1.42f,.18f),"Cream");
        Shape("GardenView",root,new Vector3(-2.5f,2.15f,2.82f),new Vector3(1.25f,1.22f,.03f),"Mint");
        Shape("WindowMullion",root,new Vector3(-2.5f,2.15f,2.78f),new Vector3(.035f,1.22f,.04f),"Cream");
        Shape("WindowMullion",root,new Vector3(-2.5f,2.15f,2.78f),new Vector3(1.25f,.035f,.04f),"Cream");
        Shape("WindowShelf",root,new Vector3(-2.5f,1.41f,2.74f),new Vector3(1.62f,.075f,.45f),"Oak");Model("Plant",root,new Vector3(-2.9f,1.45f,2.73f),Vector3.one*.65f);
        var sofa=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/StoreProducts/Prefabs/TwoSeatSofa.prefab"),root);
        sofa.name="ReadingSofa";sofa.transform.localPosition=new Vector3(3.95f,.025f,.25f);sofa.transform.localRotation=Quaternion.Euler(0,-90,0);sofa.transform.localScale=Vector3.one;
        foreach(var t in sofa.GetComponentsInChildren<Transform>(true))t.gameObject.SetActive(true);
        foreach(var component in sofa.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(component);
        foreach(var c in sofa.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
        foreach(var r in sofa.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>Mat("Mint")).ToArray();
        for(int i=0;i<2;i++){var pillow=Model("Cushion",root,new Vector3(3.85f,.55f,-.25f+i*.9f),new Vector3(.65f,1,.58f));pillow.transform.localRotation=Quaternion.Euler(0,0,-12);}
        var plant=LivingCompositionBuilder.Model("LargeNaturalPlant",root);plant.transform.localPosition=new Vector3(-3.75f,0,1.6f);
        var curtains=LivingCompositionBuilder.Model("ShortWindowCurtains",root);curtains.transform.localPosition=new Vector3(-2.5f,2.15f,2.68f);curtains.transform.localScale=Vector3.one*.92f;
        var view=Shape("WindowCountryside",root,new Vector3(-2.5f,2.15f,2.795f),new Vector3(1.25f,1.22f,1),"Mint",PrimitiveType.Quad);
        string viewPath=Root+"/Materials/GardenView.mat";var viewMat=AssetDatabase.LoadAssetAtPath<Material>(viewPath);
        if(viewMat==null){viewMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(viewMat,viewPath);}
        viewMat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/LivingComposition/Textures/WindowCountryside.png"));viewMat.SetColor("_BaseColor",Color.white);view.GetComponent<Renderer>().sharedMaterial=viewMat;
        Model("Basket",root,new Vector3(3.7f,0,-1.8f));Model("YarnBall",root,new Vector3(3.7f,.25f,-1.8f),Vector3.one*.7f);
        Shape("WallShelf",root,new Vector3(.5f,2.0f,2.73f),new Vector3(2.2f,.09f,.40f),"Oak");
        for(int i=0;i<7;i++)Shape("Book",root,new Vector3(-.38f+i*.17f,2.22f,2.72f),new Vector3(.13f,.34f+(i%3)*.055f,.23f),i%3==0?"Coral":i%3==1?"Cream":"Mint");
        Model("Plant",root,new Vector3(1.18f,2.06f,2.7f),Vector3.one*.75f);
    }
    static void BuildPond(Transform root,CozyMiniGame game)
    {
        Shape("GardenEarth",root,new Vector3(0,-.35f,0),new Vector3(16,.5f,12),"Sage");
        Shape("PondBasin",root,new Vector3(.2f,-.12f,.1f),new Vector3(6.5f,.3f,4.6f),"Petrol",PrimitiveType.Sphere);
        Shape("PondWater",root,new Vector3(.2f,.015f,.1f),new Vector3(6,.075f,4.1f),"Water",PrimitiveType.Sphere);
        for(int i=0;i<26;i++){float a=i*Mathf.PI*2/26;float x=.2f+3.17f*Mathf.Cos(a),z=.1f+2.18f*Mathf.Sin(a);if(z< -1.6f)continue;var stone=Shape("RoundedBankStone",root,new Vector3(x,.03f,z),new Vector3(.55f,.29f,.41f),i%3==0?"StoneWarm":"Stone",PrimitiveType.Sphere);stone.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}
        for(int i=0;i<30;i++)Shape("DeckPlank",root,new Vector3(-3.0f+i*.205f,.06f,-2.30f),new Vector3(.196f,.10f,.85f),"Oak");
        Shape("DockFrontTrim",root,new Vector3(-.025f,.03f,-2.77f),new Vector3(6.30f,.19f,.08f),"Honey");
        for(int i=0;i<8;i++){float a=i*Mathf.PI*2/8;if(Mathf.Sin(a)<-.6f)continue;var plant=LivingCompositionBuilder.Model(i%2==0?"LargeNaturalPlant":"SmallCeramicPlant",root);plant.transform.localPosition=new Vector3(3.85f*Mathf.Cos(a),-.08f,2.9f*Mathf.Sin(a));plant.transform.localScale=Vector3.one*(1.0f+(i%3)*.15f);}
        for(int i=0;i<32;i++){float a=i*2.39996f;float radius=3.7f+(i%3)*.42f;float x=radius*Mathf.Cos(a),z=radius*.74f*Mathf.Sin(a);if(Mathf.Abs(x)<3.1f&&z< -1.7f)continue;Shape("GardenPebble",root,new Vector3(x,-.065f,z),new Vector3(.18f,.09f,.13f),i%2==0?"Cream":"Honey",PrimitiveType.Sphere);}
        for(int i=0;i<5;i++)Shape("GardenPath",root,new Vector3(-1.4f,-.06f,-2.8f-i*.6f),new Vector3(.8f,.06f,.45f),"Cream",PrimitiveType.Cylinder);
        for(int i=0;i<7;i++){var pad=Shape("LilyPad",root,new Vector3(-1.85f+i*.6f,.08f,1.32f+Mathf.Sin(i)*.13f),new Vector3(.40f,.023f,.34f),"Mint",PrimitiveType.Cylinder);if(i%2==0)Shape("Lotus",root,pad.transform.localPosition+Vector3.up*.06f,Vector3.one*.12f,"Pearl",PrimitiveType.Sphere);}
        for(int i=0;i<5;i++){Shape("FencePost",root,new Vector3(-4+i*2,1,3.9f),new Vector3(.16f,2,.16f),"Oak");}
        for(int i=0;i<2;i++)Shape("FenceRail",root,new Vector3(0,.65f+i*.7f,3.9f),new Vector3(9,.13f,.1f),"Honey");
        game.fishRoot=Empty("Fish",root,Vector3.zero);
        for(int i=0;i<6;i++)Model("Fish"+i,game.fishRoot,new Vector3(-1.8f+(i%3)*1.75f,.075f,-.75f+(i/3)*1.45f),Vector3.one*1.05f);
        game.ripple=Model("Ripple",root,Vector3.zero).transform;
    }
}
