using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Bind real water geometry and choose a supported, readable side of it.</summary>
public static class SinkSipFacingBuilder
{
    struct Triangle
    {
        public Vector3 a,b,c;
        public float normalY;
        public bool Height(Vector3 p,out float y)
        {
            float den=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z); y=0f;
            if(Mathf.Abs(den)<.000001f)return false;
            float u=((b.z-c.z)*(p.x-c.x)+(c.x-b.x)*(p.z-c.z))/den;
            float v=((c.z-a.z)*(p.x-c.x)+(a.x-c.x)*(p.z-c.z))/den;
            y=u*a.y+v*b.y+(1-u-v)*c.y;
            return u>=-.001f&&v>=-.001f&&u+v<=1.001f;
        }
    }
    sealed class Stance
    {
        public string breed;
        public float phase;
        public Vector3[] paws;
        public Vector3 muzzle;
        public Vector3[] horizontalHull;
        public float minimumY;
    }
    // Rotation changes an asymmetric silhouette's AABB centre. Preserve its
    // exact XZ convex hull so every candidate matches runtime support-space
    // centring, instead of rotating a centre measured at zero yaw.
    static Vector3[] Hull(IEnumerable<Vector3> source)
    {
        var points=source.Select(p=>new Vector3(p.x,0,p.z)).Distinct().OrderBy(p=>p.x).ThenBy(p=>p.z).ToArray();
        if(points.Length<3)return points;
        var lower=new List<Vector3>();var upper=new List<Vector3>();
        foreach(var p in points){while(lower.Count>=2&&Cross(lower[lower.Count-2],lower[lower.Count-1],p)<=0)lower.RemoveAt(lower.Count-1);lower.Add(p);}
        for(int i=points.Length-1;i>=0;i--){var p=points[i];while(upper.Count>=2&&Cross(upper[upper.Count-2],upper[upper.Count-1],p)<=0)upper.RemoveAt(upper.Count-1);upper.Add(p);}
        lower.RemoveAt(lower.Count-1);upper.RemoveAt(upper.Count-1);lower.AddRange(upper);return lower.ToArray();
    }
    static float Cross(Vector3 a,Vector3 b,Vector3 c)=>(b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x);
    static Vector3 Correction(Stance stance,Quaternion rotation)
    {
        var bounds=new Bounds(rotation*stance.horizontalHull[0],Vector3.zero);
        foreach(var point in stance.horizontalHull)bounds.Encapsulate(rotation*point);
        return new Vector3(-bounds.center.x,-stance.minimumY+.008f,-bounds.center.z);
    }
    static List<Stance> ReadStances()
    {
        var catalog=CatBreedCatalog.Load();
        var clip=catalog.GameplayController.animationClips.First(c=>c.name.EndsWith("|Eating",StringComparison.Ordinal));
        var result=new List<Stance>();
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var mesh=new Mesh();
        try
        {
            foreach(var breed in catalog.Entries)
            {
                var cat=CatBreedVisualFactory.Create(breed,catalog.GameplayController,null);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cat,scene);
                cat.transform.localScale=Vector3.one*.5f; // Gameplay's CurrentCat_Visual scale.
                try
                {
                    var animator=cat.GetComponentInChildren<Animator>();animator.enabled=false;
                    var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();
                    var paws=new[]{"DEF-hand.L","DEF-hand.R","DEF-foot.L","DEF-foot.R"}.Select(n=>CatBreedVisualFactory.FindDescendant(cat.transform,n)).ToArray();
                    var jaw=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-jaw");
                    for(int phase=0;phase<=8;phase++)
                    {
                        clip.SampleAnimation(animator.gameObject,clip.length*phase/8f);
                        skin.BakeMesh(mesh,true);var vertices=mesh.vertices;
                        Bounds body=new Bounds(skin.transform.TransformPoint(vertices[breed.ContactVertexIndices[0]]),Vector3.zero);
                        foreach(int index in breed.ContactVertexIndices)body.Encapsulate(skin.transform.TransformPoint(vertices[index]));
                        result.Add(new Stance{breed=breed.Id,phase=phase/8f,paws=paws.Select(p=>p.position).ToArray(),muzzle=jaw.position,minimumY=body.min.y,
                            horizontalHull=Hull(breed.ContactVertexIndices.Select(i=>skin.transform.TransformPoint(vertices[i])))});
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(cat);}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(mesh);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        return result;
    }
    public static string MeasureStances()
    {
        var samples=ReadStances();
        foreach(var sample in samples){Vector3 correction=Correction(sample,Quaternion.identity);sample.muzzle+=correction;for(int i=0;i<sample.paws.Length;i++)sample.paws[i]+=correction;}
        return "Eating jaw pivot after gameplay contact alignment: min="+
            new Vector3(samples.Min(p=>p.muzzle.x),samples.Min(p=>p.muzzle.y),samples.Min(p=>p.muzzle.z)).ToString("F4")+
            " max="+new Vector3(samples.Max(p=>p.muzzle.x),samples.Max(p=>p.muzzle.y),samples.Max(p=>p.muzzle.z)).ToString("F4")+
            "; rear foot range="+samples.Min(p=>p.paws.Min(v=>v.z)).ToString("F4")+".."+samples.Max(p=>p.paws.Max(v=>v.z)).ToString("F4");
    }
    public static void Configure(GameObject root,StoreCatalogAsset definition)
    {
        var sip=root.GetComponent<SinkSipActivity>(); if(sip==null)return;
        Vector3 source;
        switch(definition.PrefabName)
        {
            case "BathroomVanitySink":source=new Vector3(.06f,.976f,0f);break;
            case "KitchenSinkCabinet":source=new Vector3(0f,.80f,0f);break;
            case "GardenBirdBath":source=new Vector3(0f,.672f,0f);break;
            case "PatioWaterFountain":source=new Vector3(0f,.268f,0f);break;
            default:return;
        }
        var body=root.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f=>f.name.EndsWith("_PremiumModel",StringComparison.Ordinal)&&f.sharedMesh!=null);
        if(body==null)throw new InvalidOperationException(definition.PrefabName+": missing measured premium body");
        if(definition.PrefabName=="KitchenSinkCabinet"||definition.PrefabName=="GardenBirdBath"||definition.PrefabName=="PatioWaterFountain")AttachWater(body.transform,definition.PrefabName);
        var data=new SerializedObject(sip);
        var perch=data.FindProperty("perchPoint").objectReferenceValue as Transform;
        if(perch==null)throw new InvalidOperationException(definition.PrefabName+": missing perch");
        // Blender export is Y-up; FBX import mirrors X. The existing child
        // rotation and BOTH authoring/product scales are carried by TransformPoint.
        Vector3 target=root.transform.InverseTransformPoint(body.transform.TransformPoint(new Vector3(-source.x,source.y,source.z)));
        Vector3 authored=root.transform.InverseTransformPoint(perch.position);
        var candidates=new List<Vector3>();
        bool counter=definition.PrefabName=="BathroomVanitySink"||definition.PrefabName=="KitchenSinkCabinet";
        if(counter)
        {
            // Search both real counter wings, not a fixed point near its front
            // edge. Every candidate must support the complete ten-breed stance.
            for(float x=-.74f;x<=.74f;x+=.025f)
            for(float z=-.27f;z<=.27f;z+=.03f)
                candidates.Add(root.transform.InverseTransformPoint(body.transform.TransformPoint(new Vector3(-x,.86f,z))));
        }
        else
        {
            float outerRadius=new Vector2(authored.x-target.x,authored.z-target.z).magnitude;
            if(definition.PrefabName=="PatioWaterFountain")outerRadius=.30f*body.transform.TransformVector(Vector3.forward).magnitude;
            for(float radius=.04f;radius<=outerRadius+.0001f;radius+=.02f)
            for(int i=0;i<72;i++)
            {
                float angle=i*5f*Mathf.Deg2Rad;
                candidates.Add(new Vector3(target.x+Mathf.Sin(angle)*radius,authored.y,target.z+Mathf.Cos(angle)*radius));
            }
        }
        bool patio=definition.PrefabName=="PatioWaterFountain";
        var triangles=new SurfaceIndex(ReadTriangles(root,patio));
        var stances=ReadStances();
        Quaternion placement=Quaternion.Euler(0,definition.DefaultYaw,0);
        Vector3 selected=authored; float best=float.PositiveInfinity;
        int grounded=0,readable=0,maxSupportedFeet=0;Vector3 closestRejected=authored;
        foreach(Vector3 request in candidates)
        {
            Vector3 point=request;
            if(!SupportHeight(triangles,point,out float y))continue;
            grounded++;
            point.y=y;
            Vector3 world=definition.DefaultPosition+placement*point;
            Vector3 candidateWater=WaterTarget(root.transform,body.transform,definition.PrefabName,source,point);
            Vector3 facing=placement*(candidateWater-point);
            if(CatActivityFacing.FacingDot(facing,world,HomeRoomCameraProfile.Position)<.30f)continue;
            readable++;
            Quaternion work=Quaternion.LookRotation(new Vector3(candidateWater.x-point.x,0,candidateWater.z-point.z));
            float cost=(point-authored).sqrMagnitude;
            if(cost>=best)continue;
            bool supported=true;int supportedFeet=0;
            foreach(var stance in stances)
            {
                Vector3 correction=Correction(stance,work);
                foreach(Vector3 paw in stance.paws)
                {
                    Vector3 offset=work*paw+correction;
                    Vector3 at=point+new Vector3(offset.x,0,offset.z);
                    if(!(patio?SupportedFirstSurface(triangles,at,point.y+offset.y,point.y):SupportedPaw(triangles,at,point.y)))
                    {supported=false;break;}
                    supportedFeet++;
                }
                if(!supported)break;
            }
            if(supportedFeet>maxSupportedFeet){maxSupportedFeet=supportedFeet;closestRejected=point;}
            if(!supported)continue;
            best=cost;selected=point;
        }
        if(float.IsPositiveInfinity(best))throw new InvalidOperationException(definition.PrefabName+": no supported camera-readable water contact side; candidates="+candidates.Count+
            " grounded="+grounded+" readable="+readable+" best consecutive supported feet="+maxSupportedFeet+"/"+(stances.Count*4)+" at "+closestRejected.ToString("F5"));
        perch.position=root.transform.TransformPoint(selected);
        var contact=root.transform.Find("SipWaterTarget");
        if(contact==null){contact=new GameObject("SipWaterTarget").transform;contact.SetParent(root.transform,false);}
        contact.localPosition=WaterTarget(root.transform,body.transform,definition.PrefabName,source,selected);
        contact.localRotation=Quaternion.identity;contact.localScale=Vector3.one;
        sip.EditorConfigureSipTarget(contact);
    }
    static Vector3 WaterTarget(Transform root,Transform body,string name,Vector3 source,Vector3 perch)
    {
        Vector3 sourcePerch=body.InverseTransformPoint(root.TransformPoint(perch));sourcePerch.x=-sourcePerch.x;
        Vector3 near=source;
        if(name=="KitchenSinkCabinet")
        {near.x=Mathf.Clamp(sourcePerch.x,-.27f,.27f);near.z=Mathf.Clamp(sourcePerch.z,-.17f,.17f);}
        else
        {
            float radius=name=="BathroomVanitySink"?.196f:name=="GardenBirdBath"?.272f:.344f;
            Vector3 outward=sourcePerch-source;outward.y=0f;
            if(name=="BathroomVanitySink")near+=outward.normalized*(radius-.008f);
            else
            {
                // On a small basin the supported body can already be over the
                // disk. Reach inward along the actual muzzle, not to the disk's
                // nearest rim (which can coincide with the feet).
                float scale=body.TransformVector(Vector3.forward).magnitude;
                Vector3 reach=-outward.normalized;
                // Follow a readable side of the lower basin, leaving the real central stem clear.
                if(name=="PatioWaterFountain")reach=Quaternion.Euler(0,240f,0)*reach;
                Vector3 mouth=sourcePerch+reach*(.17f/scale);mouth.y=source.y;
                Vector3 radial=mouth-source;radial.y=0f;
                near=source+Vector3.ClampMagnitude(radial,radius-.008f);
            }
        }
        return root.InverseTransformPoint(body.TransformPoint(new Vector3(-near.x,near.y,near.z)));
    }
    static void AttachWater(Transform body,string product)
    {
        bool kitchen=product=="KitchenSinkCabinet";
        string waterModel=kitchen?"KitchenSinkWater":product=="PatioWaterFountain"?"PatioWaterFountainWater":"GardenBirdBathWater";
        string asset="Assets/Art/PremiumFurniture/Models/"+waterModel+"_Premium.fbx";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(asset);
        if(prefab==null)throw new InvalidOperationException(product+" needs its actual visible basin water: "+asset);
        string childName=kitchen?"KitchenSipWater":product=="PatioWaterFountain"?"PatioSipWater":"GardenSipWater";
        Transform water=body.parent.Find(childName);
        if(water==null)
        {
            var created=(GameObject)PrefabUtility.InstantiatePrefab(prefab);water=created.transform;
            water.name=childName;water.SetParent(body.parent,false);
        }
        water.localPosition=body.localPosition;water.localRotation=body.localRotation;water.localScale=body.localScale;
        foreach(var renderer in water.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Art/StoreProducts/Materials/"+(!kitchen&&m!=null&&m.name.Contains("White")?"CH_White":"CH_AquaBright")+".mat")).ToArray();
        foreach(var collider in water.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
    }
    sealed class SurfaceIndex
    {
        const float Cell=.10f;
        readonly Dictionary<Vector2Int,List<Triangle>> buckets=new Dictionary<Vector2Int,List<Triangle>>();
        public SurfaceIndex(List<Triangle> triangles)
        {
            foreach(var t in triangles)
            {
                float minX=Mathf.Min(t.a.x,t.b.x,t.c.x),maxX=Mathf.Max(t.a.x,t.b.x,t.c.x);
                float minZ=Mathf.Min(t.a.z,t.b.z,t.c.z),maxZ=Mathf.Max(t.a.z,t.b.z,t.c.z);
                // Include the existing barycentric .001 allowance in the
                // lookup bounds; the exact Height test is unchanged.
                float padX=(maxX-minX)*.002f+.000001f,padZ=(maxZ-minZ)*.002f+.000001f;
                for(int x=Mathf.FloorToInt((minX-padX)/Cell);x<=Mathf.FloorToInt((maxX+padX)/Cell);x++)
                for(int z=Mathf.FloorToInt((minZ-padZ)/Cell);z<=Mathf.FloorToInt((maxZ+padZ)/Cell);z++)
                {var key=new Vector2Int(x,z);if(!buckets.TryGetValue(key,out var list))buckets[key]=list=new List<Triangle>();list.Add(t);}
            }
        }
        public List<Triangle> At(Vector3 point)=>buckets.TryGetValue(new Vector2Int(Mathf.FloorToInt(point.x/Cell),Mathf.FloorToInt(point.z/Cell)),out var list)?list:null;
    }
    [Serializable] sealed class SupportDesignReport
    {
        public Vector3 perch,waterCenter,pawMinimum,pawMaximum;
        public float bodyScale,currentRimOuterDiameter,currentWaterDiameter,requiredFlatSupportDiameter,extraOuterDiameter;
        public List<SupportDesignPaw> failures=new List<SupportDesignPaw>();
        public List<SupportDesignPaw> allPaws=new List<SupportDesignPaw>();
    }
    [Serializable] sealed class SupportDesignPaw
    {
        public string breed,paw,nearestSurface;
        public float phase,radialDistance,nearestSurfaceY,heightDifference;
        public Vector3 point;
        public bool supported;
    }
    public static string DiagnoseBirdBathSupport()
    {
        const string path="Assets/Art/StoreProducts/Prefabs/GardenBirdBath.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var body=root.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name.EndsWith("_PremiumModel",StringComparison.Ordinal));
            var renderer=body.GetComponent<Renderer>();var mesh=body.sharedMesh;var vertices=mesh.vertices;
            Matrix4x4 matrix=root.transform.worldToLocalMatrix*body.transform.localToWorldMatrix;
            var surfaces=new List<(Triangle,string)>();
            for(int sub=0;sub<mesh.subMeshCount;sub++)
            {
                var indices=mesh.GetTriangles(sub);string material=renderer.sharedMaterials[sub].name;
                for(int i=0;i<indices.Length;i+=3)
                {
                    var t=new Triangle{a=matrix.MultiplyPoint3x4(vertices[indices[i]]),b=matrix.MultiplyPoint3x4(vertices[indices[i+1]]),c=matrix.MultiplyPoint3x4(vertices[indices[i+2]])};
                    if(Vector3.Cross(t.b-t.a,t.c-t.a).normalized.y>=.6f)surfaces.Add((t,material));
                }
            }
            var report=new SupportDesignReport();report.bodyScale=body.transform.TransformVector(Vector3.forward).magnitude;
            report.waterCenter=root.transform.InverseTransformPoint(body.transform.TransformPoint(new Vector3(0,.672f,0)));
            report.perch=new Vector3(-.03464f,.49651057f,-.00915f);
            report.currentRimOuterDiameter=.698f*report.bodyScale;report.currentWaterDiameter=.544f*report.bodyScale;
            Vector3 toward=report.waterCenter-report.perch;toward.y=0;Quaternion work=Quaternion.LookRotation(toward);
            var support=new SurfaceIndex(surfaces.Select(s=>s.Item1).ToList());
            bool first=true;float requiredRadius=0;
            string[] names={"DEF-hand.L","DEF-hand.R","DEF-foot.L","DEF-foot.R"};
            foreach(var stance in ReadStances())
            {
                Vector3 correction=Correction(stance,work);
                for(int i=0;i<stance.paws.Length;i++)
                {
                    Vector3 offset=work*stance.paws[i]+correction;
                    Vector3 point=report.perch+new Vector3(offset.x,0,offset.z);
                    var sample=new SupportDesignPaw{breed=stance.breed,phase=stance.phase,paw=names[i],point=point,
                        radialDistance=new Vector2(point.x-report.waterCenter.x,point.z-report.waterCenter.z).magnitude,
                        supported=SupportedPaw(support,point,report.perch.y),nearestSurfaceY=999f,heightDifference=999f,nearestSurface="No upward triangle"};
                    foreach(var surface in surfaces)
                        if(surface.Item1.Height(point,out float y)&&Mathf.Abs(y-report.perch.y)<Mathf.Abs(sample.heightDifference))
                        {sample.nearestSurfaceY=y;sample.heightDifference=y-report.perch.y;sample.nearestSurface=surface.Item2;}
                    requiredRadius=Mathf.Max(requiredRadius,sample.radialDistance+.006f);
                    if(first){report.pawMinimum=report.pawMaximum=point;first=false;}
                    else{report.pawMinimum=Vector3.Min(report.pawMinimum,point);report.pawMaximum=Vector3.Max(report.pawMaximum,point);}
                    report.allPaws.Add(sample);if(!sample.supported)report.failures.Add(sample);
                }
            }
            report.requiredFlatSupportDiameter=2*requiredRadius;
            report.extraOuterDiameter=Mathf.Max(0,report.requiredFlatSupportDiameter-report.currentRimOuterDiameter);
            System.IO.Directory.CreateDirectory("Library/SinkSipFacing");
            const string output="Library/SinkSipFacing/bird-bath-support-design.json";
            System.IO.File.WriteAllText(output,JsonUtility.ToJson(report,true));
            return output+"; failures="+report.failures.Count+"/"+report.allPaws.Count+" required flat diameter="+report.requiredFlatSupportDiameter.ToString("F5")+
                " current outer diameter="+report.currentRimOuterDiameter.ToString("F5")+" added outer diameter="+report.extraOuterDiameter.ToString("F5");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static bool SupportedPaw(SurfaceIndex triangles,Vector3 point,float level)
    {
        // Keep a real 6 mm contact patch clear of a rounded counter edge.
        foreach(var offset in PawSupportPatch)
            if(!SupportHeight(triangles,point+offset,out float y)||Mathf.Abs(y-level)>.035f)return false;
        return true;
    }
    static bool SupportedFirstSurface(SurfaceIndex triangles,Vector3 point,float pawHeight,float level)
    {
        foreach(var offset in PawSupportPatch)
        {
            Vector3 at=point+offset;var bucket=triangles.At(at);if(bucket==null)return false;
            float highest=float.NegativeInfinity,normalY=0;
            foreach(var triangle in bucket)
                if(triangle.Height(at,out float y)&&y<=pawHeight+.10f&&y>highest)
                {highest=y;normalY=triangle.normalY;}
            if(normalY<.5f||Mathf.Abs(highest-level)>.035f)return false;
        }
        return true;
    }
    static readonly Vector3[] PawSupportPatch={Vector3.zero,new Vector3(.006f,0,0),new Vector3(-.006f,0,0),new Vector3(0,0,.006f),new Vector3(0,0,-.006f)};
    static bool SupportHeight(SurfaceIndex triangles,Vector3 p,out float height)
    {
        bool found=false;float closest=.065f;height=p.y;
        var bucket=triangles.At(p);if(bucket==null)return false;
        foreach(var t in bucket)if(t.Height(p,out float y)&&Mathf.Abs(y-p.y)<closest)
        {closest=Mathf.Abs(y-p.y);height=y;found=true;}
        return found;
    }
    static List<Triangle> ReadTriangles(GameObject root,bool includeSlopes=false)
    {
        var result=new List<Triangle>();
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if(filter.sharedMesh==null||!filter.name.EndsWith("_PremiumModel",StringComparison.Ordinal))continue;
            var vertices=filter.sharedMesh.vertices;var indices=filter.sharedMesh.triangles;
            Matrix4x4 matrix=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            for(int i=0;i<indices.Length;i+=3)
            {
                var t=new Triangle{a=matrix.MultiplyPoint3x4(vertices[indices[i]]),b=matrix.MultiplyPoint3x4(vertices[indices[i+1]]),c=matrix.MultiplyPoint3x4(vertices[indices[i+2]])};
                t.normalY=Vector3.Cross(t.b-t.a,t.c-t.a).normalized.y;
                if(t.normalY>=(includeSlopes?.0001f:.6f))result.Add(t);
            }
        }
        return result;
    }
}
