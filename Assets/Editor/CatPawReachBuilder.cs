using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Measures the actual source contact skeleton and grounded skin; never edits source assets.</summary>
public static class CatPawReachBuilder
{
    public const string AssetPath = "Assets/Resources/Home/CatPawReachCatalog.asset";
    private static readonly CatActivityPose[] Poses = {
        CatActivityPose.Scratch, CatActivityPose.BatLeft, CatActivityPose.BatRight,
        CatActivityPose.Push, CatActivityPose.Paw, CatActivityPose.Tug,
        CatActivityPose.Sniff, CatActivityPose.Eat, CatActivityPose.Stalk, CatActivityPose.Hop };
    // Contact candidates plus the complete source entry/exit trajectory. Paw's
    // authored lunge must also be safe before and after the chosen contact.
    private static readonly float[] Phases = {
        0f, .05f, .1f, .15f, .2f, .25f, .3f, .32f, .35f, .4f, .42f, .45f,
        .5f, .52f, .55f, .6f, .62f, .65f, .7f, .75f, .8f, .85f, .9f, .95f, 1f };
    private static readonly float[] SurfacePhases = Phases.Concat(new[] {
        .31f,.33f,.41f,.43f,.51f,.53f,.61f,.63f }).OrderBy(p=>p).ToArray();
    private static readonly string[] Regions = { "pelvis", "chest", "neck", "head" };

    [MenuItem("Tools/Cat Home/Cat/Measure Paw Reach")]
    private static void BuildMenu() => Debug.Log(Build());

    /// <summary>Run outside Play Mode. Saves only its own catalog and returns CSV source-space measurements.</summary>
    public static string Build() => Build(false);
    // The reviewed refinement changes only dense Scratch surface shards.
    // Existing Body assets/index are neither dirtied nor reserialized.
    public static string BuildSurfaceGeometry() => Build(true);
    /// <summary>Rebuild one opted-in pose for all breeds; leaves every other shard and index unchanged.</summary>
    public static string BuildSurfaceGeometry(CatActivityPose pose)
    {
        if(!CatPawReachCatalog.SupportsSurface(pose))throw new ArgumentOutOfRangeException(nameof(pose));
        return Build(true,pose);
    }
    static string Build(bool surfacesOnly,CatActivityPose? surfacePose=null)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Measure paw reach outside Play Mode.");
        var breeds = CatBreedCatalog.Load();
        if (breeds == null || breeds.GameplayController == null)
            throw new InvalidOperationException("The source breed catalog/controller is missing.");
        var entries = new List<CatPawReachCatalog.Entry>();
        var surfaces = new List<CatPawReachCatalog.Entry>();
        var report = new StringBuilder("breed,pose,state,phase,sourceMinimumY,leftUpper,leftFore,leftReach,rightUpper,rightFore,rightReach,torsoY,headY,leftRearY,rightRearY,leftHandZ,rightHandZ,bodyProbes,leftPawVertices,rightPawVertices,leftDistal,rightDistal,skinBindError,sourceClipGuid,sourceClipFileId,sourceClipName,leftPawTriangles,rightPawTriangles,leftBoundaryTriangles,rightBoundaryTriangles,leftArmVertices,rightArmVertices,leftArmTriangles,rightArmTriangles,leftArmOuterBoundary,rightArmOuterBoundary,sourceBones\n");
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (var breed in breeds.Entries)
            {
                var visual = CatBreedVisualFactory.Create(breed, breeds.GameplayController, null, "Paw reach measurement");
                if (visual == null) throw new InvalidOperationException("Cannot instantiate " + breed.Id);
                SceneManager.MoveGameObjectToScene(visual, scene);
                var mesh = new Mesh();
                try
                {
                    var animator = visual.GetComponentInChildren<Animator>();
                    animator.enabled = false;
                    var skin = visual.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (skin == null || skin.sharedMesh == null || breed.ContactVertexIndices.Count == 0)
                        throw new InvalidOperationException(breed.Id + " has no measured body skin mask.");
                    var rest = visual.GetComponentsInChildren<Transform>(true).Select(t => new LocalTransform(t)).ToArray();
                    var rig = new Rig(visual.transform);
                    var bones = skin.bones;
                    var weights = skin.sharedMesh.boneWeights;
                    if (skin.sharedMesh.blendShapeCount != 0)
                        throw new InvalidOperationException(breed.Id + " requires explicit blendshape paw skinning.");
                    var leftPaw = PawVertices(skin, animator.transform, "L");
                    var rightPaw = PawVertices(skin, animator.transform, "R");
                    var leftArm = ArmVertices(skin,animator.transform,"L");
                    var rightArm = ArmVertices(skin,animator.transform,"R");
                    var bodyCoverage=(!surfacesOnly||!surfacePose.HasValue||surfacePose.Value==CatActivityPose.Paw)?
                        CatPawBodyCoverageBuilder.Build(skin,animator.transform,breed.ContactVertexIndices):null;
                    var leftArmTriangles = PawTriangles(skin.sharedMesh,leftArm,out int leftArmBoundary);
                    var rightArmTriangles = PawTriangles(skin.sharedMesh,rightArm,out int rightArmBoundary);
                    var leftTriangles = PawTriangles(skin.sharedMesh,leftPaw,out int leftBoundary);
                    var rightTriangles = PawTriangles(skin.sharedMesh,rightPaw,out int rightBoundary);
                    var groups = new int[weights.Length];
                    for (int i = 0; i < groups.Length; i++) groups[i] = -1;
                    foreach (int index in breed.ContactVertexIndices)
                        if (index >= 0 && index < groups.Length) groups[index] = Region(weights[index], bones);
                    Vector3 visualPosition = animator.transform.localPosition, visualScale = animator.transform.localScale;
                    Quaternion visualRotation = animator.transform.localRotation;
                    foreach (var pose in Poses)
                    {
                        bool captureSurface=CatPawReachCatalog.SupportsSurface(pose);
                        if(surfacesOnly&&(!captureSurface||surfacePose.HasValue&&pose!=surfacePose.Value))continue;
                        var bodyRegions=pose==CatActivityPose.Paw?bodyCoverage:null;
                        // Each stored entry owns its bindings. Extending Paw's bone
                        // table must not reindex the already captured Scratch entry.
                        var poseLeftArm=CatPawBodyCoverageBuilder.Clone(leftArm);var poseRightArm=CatPawBodyCoverageBuilder.Clone(rightArm);
                        var bindingSets=new List<CatPawReachCatalog.PawVertex[]>{poseLeftArm,poseRightArm};
                        if(bodyRegions!=null)bindingSets.AddRange(bodyRegions.Select(r=>r.skin));
                        var sourceBones=BindSourceBones(animator.transform,bindingSets.ToArray());
                        var phases=captureSurface?SurfacePhases:Phases;
                        string stateName = CatActivityAnimation.StateFor(pose);
                        AnimationClip clip = StateClip(breeds.GameplayController, stateName);
                        if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip,out string clipGuid,out long clipFileId))
                            throw new InvalidOperationException("Cannot identify source clip "+clip.name);
                        var samples = new CatPawReachCatalog.Sample[phases.Length];
                        for (int phaseIndex = 0; phaseIndex < phases.Length; phaseIndex++)
                        {
                            foreach (var transform in rest) transform.Restore();
                            float phase = phases[phaseIndex];
                            float sampleTime = (clip.isLooping ? Mathf.Repeat(phase, 1f) : phase) * clip.length;
                            clip.SampleAnimation(animator.gameObject, sampleTime);
                            // Match CatActivityAnimation.LateUpdate: the source
                            // skeleton's lunge survives, but visual-root curves
                            // cannot translate, rotate or scale the whole cat.
                            animator.transform.localPosition = visualPosition;
                            animator.transform.localRotation = visualRotation;
                            animator.transform.localScale = visualScale;
                            skin.BakeMesh(mesh, true);
                            var vertices = mesh.vertices;
                            var points = new Vector3[vertices.Length];
                            float minimum = float.PositiveInfinity;
                            foreach (int index in breed.ContactVertexIndices)
                            {
                                if (index < 0 || index >= vertices.Length)
                                    throw new InvalidOperationException(breed.Id + " has a stale body skin mask.");
                                points[index] = visual.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[index]));
                                minimum = Mathf.Min(minimum, points[index].y);
                            }
                            Vector3 groundOffset = Vector3.up * -minimum;
                            var sample = rig.Sample(visual.transform, phase, minimum, groundOffset);
                            float skinBindError=0;
                            if(captureSurface)
                            {
                                sample.skinMatrices=SourceMatrices(sourceBones,animator.transform,visual.transform,groundOffset);
                                sample.leftPaw=PawPoints(leftPaw,vertices,skin.transform,visual.transform,groundOffset);
                                sample.rightPaw=PawPoints(rightPaw,vertices,skin.transform,visual.transform,groundOffset);
                                skinBindError=Mathf.Max(ValidateSkin(leftPaw,vertices,skin,animator.transform),ValidateSkin(rightPaw,vertices,skin,animator.transform),ValidateSkin(poseLeftArm,vertices,skin,animator.transform),ValidateSkin(poseRightArm,vertices,skin,animator.transform));
                                if(bodyRegions!=null)foreach(var region in bodyRegions)skinBindError=Mathf.Max(skinBindError,ValidateSkin(region.skin,vertices,skin,animator.transform));
                                if(skinBindError>.0001f)throw new InvalidOperationException(breed.Id+"/"+pose+": paw bind skin differs from BakeMesh by "+skinBindError);
                            }
                            var regionPoints = Regions.Select(_ => new List<Vector3>()).ToArray();
                            foreach (int index in breed.ContactVertexIndices)
                                if (index < groups.Length && groups[index] >= 0)
                                    regionPoints[groups[index]].Add(points[index] + groundOffset);
                            sample.bodyProbes = new CatBodyGuardCatalog.Probe[Regions.Length];
                            for (int region = 0; region < Regions.Length; region++)
                            {
                                if (regionPoints[region].Count == 0)
                                    throw new InvalidOperationException(breed.Id + "/" + pose + " has no " + Regions[region] + " samples.");
                                sample.bodyProbes[region] = FitBody(Regions[region], regionPoints[region]);
                            }
                            samples[phaseIndex] = sample;
                            report.AppendLine(string.Join(",", breed.Id, pose, stateName, F(phase), F(minimum),
                                F(sample.left.upperLength), F(sample.left.foreLength), F(sample.left.Reach),
                                F(sample.right.upperLength), F(sample.right.foreLength), F(sample.right.Reach),
                                F(sample.torsoPivot.y), F(sample.head.y), F(sample.leftRear.y), F(sample.rightRear.y),
                                F(sample.left.hand.z), F(sample.right.hand.z), sample.bodyProbes.Length,sample.leftPaw.Length,sample.rightPaw.Length,
                                captureSurface?leftPaw.Count(v=>v.distal):0,captureSurface?rightPaw.Count(v=>v.distal):0,F(skinBindError),clipGuid,clipFileId.ToString(CultureInfo.InvariantCulture),
                                "\""+clip.name.Replace("\"","\"\"")+"\"",leftTriangles.Length/3,rightTriangles.Length/3,leftBoundary,rightBoundary,leftArm.Length,rightArm.Length,leftArmTriangles.Length/3,rightArmTriangles.Length/3,leftArmBoundary,rightArmBoundary,sourceBones.Length));
                        }
                        if(captureSurface)surfaces.Add(new CatPawReachCatalog.Entry{breedId=breed.Id,pose=pose,
                            stateName=stateName,samples=samples,leftPaw=leftPaw,rightPaw=rightPaw,
                            surfaceGeometryVersion=CatPawReachCatalog.SurfaceGeometryVersion,
                            leftPawTriangles=leftTriangles,rightPawTriangles=rightTriangles,
                            skinBones=sourceBones,leftArmSkin=poseLeftArm,rightArmSkin=poseRightArm,
                            bodySurfaceVersion=bodyRegions!=null?CatPawReachCatalog.BodySurfaceVersion:0,bodySurface=bodyRegions??Array.Empty<CatPawReachCatalog.BodyRegion>(),
                            leftArmTriangles=leftArmTriangles,rightArmTriangles=rightArmTriangles});
                        if(!surfacesOnly)entries.Add(new CatPawReachCatalog.Entry{breedId=breed.Id,pose=pose,stateName=stateName,
                            samples=samples.Where(sample=>Array.IndexOf(Phases,sample.phase)>=0).Select(BodyOnly).ToArray()});
                    }
                }
                finally { Object.DestroyImmediate(mesh); Object.DestroyImmediate(visual); }
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        // No direct references from the tiny index to shards: Resources loads
        // only the current breed, and dense skin only for opted-in surface poses.
        foreach(var breed in breeds.Entries)
        {
            if(!surfacesOnly)SaveCatalog("Assets/Resources/"+CatPawReachCatalog.BodyResourceName(breed.Id)+".asset",
                entries.Where(e=>e.breedId==breed.Id).ToArray());
            foreach(var entry in surfaces.Where(e=>e.breedId==breed.Id))
                SaveCatalog("Assets/Resources/"+CatPawReachCatalog.SurfaceResourceName(entry.breedId,entry.pose)+".asset",new[]{entry});
        }
        if(!surfacesOnly)SaveCatalog(AssetPath,Array.Empty<CatPawReachCatalog.Entry>(),entries.Count);
        return report.ToString();
    }
    static CatPawReachCatalog.Sample BodyOnly(CatPawReachCatalog.Sample value)=>new CatPawReachCatalog.Sample{
        phase=value.phase,sourceMinimumY=value.sourceMinimumY,torsoPivot=value.torsoPivot,pelvis=value.pelvis,
        head=value.head,leftRear=value.leftRear,rightRear=value.rightRear,torsoRotation=value.torsoRotation,
        torsoLocalRotation=value.torsoLocalRotation,left=value.left,right=value.right,bodyProbes=value.bodyProbes};
    static void SaveCatalog(string path,CatPawReachCatalog.Entry[] values,int indexedCount=-1)
    {
        string folder=System.IO.Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(folder);
        var existing=AssetDatabase.LoadMainAssetAtPath(path);
        if(existing!=null&&!(existing is CatPawReachCatalog))throw new InvalidOperationException("Refusing to replace non-catalog asset "+path);
        var catalog=existing as CatPawReachCatalog;
        if(catalog==null){catalog=ScriptableObject.CreateInstance<CatPawReachCatalog>();AssetDatabase.CreateAsset(catalog,path);}
        catalog.EditorConfigure(values,indexedCount);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
    }
    static void EnsureFolder(string folder)
    {
        if(AssetDatabase.IsValidFolder(folder))return;
        string parent=System.IO.Path.GetDirectoryName(folder).Replace('\\','/');EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(folder));
    }

    // Record complete faces inside the already measured rigid paw set.
    // Faces crossing its outer wrist boundary are counted explicitly; this
    // refinement does not add mixed elbow vertices or alter the skin model.
    private static int[] PawTriangles(Mesh mesh,CatPawReachCatalog.PawVertex[] paw,out int boundary)
    {
        var local=new Dictionary<int,int>();
        for(int i=0;i<paw.Length;i++)local.Add(paw[i].vertexIndex,i);
        var result=new List<int>();boundary=0;
        using(var source=MeshUtility.AcquireReadOnlyMeshData(mesh))
        for(int sub=0;sub<source[0].subMeshCount;sub++)
        {
            var descriptor=source[0].GetSubMesh(sub);
            if(descriptor.topology!=UnityEngine.MeshTopology.Triangles)continue;
            using(var indices=new Unity.Collections.NativeArray<int>(descriptor.indexCount,Unity.Collections.Allocator.Temp))
            {
                source[0].GetIndices(indices,sub);
                for(int i=0;i+2<indices.Length;i+=3)
                {
                    bool a=local.TryGetValue(indices[i],out int x),b=local.TryGetValue(indices[i+1],out int y),c=local.TryGetValue(indices[i+2],out int z);
                    if(a&&b&&c){result.Add(x);result.Add(y);result.Add(z);}
                    else if(a||b||c)boundary++;
                }
            }
        }
        if(result.Count==0)throw new InvalidOperationException("No complete source paw faces: "+mesh.name);
        return result.ToArray();
    }

    private static CatPawReachCatalog.PawVertex[] PawVertices(SkinnedMeshRenderer skin, Transform animationRoot, string side)
    {
        var mesh=skin.sharedMesh;var weights=mesh.boneWeights;var vertices=mesh.vertices;var bind=mesh.bindposes;var bones=skin.bones;
        var hand=CatBreedVisualFactory.FindDescendant(animationRoot,"DEF-hand."+side);
        var fore=CatBreedVisualFactory.FindDescendant(animationRoot,"DEF-forearm."+side);
        if(hand==null||fore==null)throw new InvalidOperationException("Missing paw hierarchy "+side);
        var result=new List<CatPawReachCatalog.PawVertex>();
        for(int v=0;v<weights.Length;v++)
        {
            var w=weights[v];int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] amounts={w.weight0,w.weight1,w.weight2,w.weight3};
            float foreWeight=0,handWeight=0,distalWeight=0;
            for(int i=0;i<4;i++)if(amounts[i]>0)
            {
                var b=bones[ids[i]];
                if(b==fore||b.IsChildOf(fore))foreWeight+=amounts[i];
                if(b==hand||b.IsChildOf(hand))handWeight+=amounts[i];
                if(b!=hand&&b.IsChildOf(hand))distalWeight+=amounts[i];
            }
            // All influences share the unchanged forearm subtree, so a fore/arm
            // solve is exactly rigid for these points; mixed elbow skin is not
            // silently approximated as a paw surface.
            if(foreWeight<.9999f||handWeight<.5f)continue;
            var influences=new List<CatPawReachCatalog.PawInfluence>();
            for(int i=0;i<4;i++)if(amounts[i]>0)influences.Add(new CatPawReachCatalog.PawInfluence{
                bonePath=AnimationUtility.CalculateTransformPath(bones[ids[i]],animationRoot),
                bindPosition=bind[ids[i]].MultiplyPoint3x4(vertices[v]),weight=amounts[i]});
            result.Add(new CatPawReachCatalog.PawVertex{vertexIndex=v,distal=distalWeight>=.25f,influences=influences.ToArray()});
        }
        if(result.Count(v=>v.distal)<3)throw new InvalidOperationException("Fewer than three visible distal paw vertices: "+side);
        return result.ToArray();
    }
    // Include every vertex influenced by the actual upper-arm subtree, with
    // all corners of every source face touching that set. The one-ring halo
    // retains true torso/elbow mixed weights; it is never made rigid by force.
    private static CatPawReachCatalog.PawVertex[] ArmVertices(SkinnedMeshRenderer skin,Transform root,string side)
    {
        var mesh=skin.sharedMesh;var weights=mesh.boneWeights;var vertices=mesh.vertices;var bind=mesh.bindposes;var bones=skin.bones;
        var upper=CatBreedVisualFactory.FindDescendant(root,"DEF-upper_arm."+side);
        if(upper==null)throw new InvalidOperationException("Missing front arm "+side);
        var selected=new bool[weights.Length];
        for(int v=0;v<weights.Length;v++)
        {
            var w=weights[v];
            selected[v]=Part(w.boneIndex0,w.weight0)||Part(w.boneIndex1,w.weight1)||Part(w.boneIndex2,w.weight2)||Part(w.boneIndex3,w.weight3);
        }
        var halo=(bool[])selected.Clone();
        using(var source=MeshUtility.AcquireReadOnlyMeshData(mesh))
        for(int sub=0;sub<source[0].subMeshCount;sub++)
        {
            var descriptor=source[0].GetSubMesh(sub);if(descriptor.topology!=MeshTopology.Triangles)continue;
            using(var indices=new Unity.Collections.NativeArray<int>(descriptor.indexCount,Unity.Collections.Allocator.Temp))
            {
                source[0].GetIndices(indices,sub);
                for(int i=0;i+2<indices.Length;i+=3)
                {int a=indices[i],b=indices[i+1],c=indices[i+2];if(selected[a]||selected[b]||selected[c])halo[a]=halo[b]=halo[c]=true;}
            }
        }
        var result=new List<CatPawReachCatalog.PawVertex>();
        for(int v=0;v<weights.Length;v++)if(halo[v])
        {
            var w=weights[v];int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] amounts={w.weight0,w.weight1,w.weight2,w.weight3};
            var influences=new List<CatPawReachCatalog.PawInfluence>();
            for(int i=0;i<4;i++)if(amounts[i]>0)influences.Add(new CatPawReachCatalog.PawInfluence{
                bonePath=AnimationUtility.CalculateTransformPath(bones[ids[i]],root),bindPosition=bind[ids[i]].MultiplyPoint3x4(vertices[v]),weight=amounts[i]});
            result.Add(new CatPawReachCatalog.PawVertex{vertexIndex=v,distal=false,influences=influences.ToArray()});
        }
        return result.ToArray();
        bool Part(int index,float weight)=>weight>0&&(bones[index]==upper||bones[index].IsChildOf(upper));
    }
    private static CatPawReachCatalog.SkinBone[] BindSourceBones(Transform root,params CatPawReachCatalog.PawVertex[][] sides)
    {
        var paths=sides.SelectMany(s=>s).SelectMany(v=>v.influences).Select(i=>i.bonePath).Distinct().OrderBy(p=>p,StringComparer.Ordinal).ToArray();
        var result=new CatPawReachCatalog.SkinBone[paths.Length];
        var chest=CatBreedVisualFactory.FindDescendant(root,"DEF-spine.001");
        var lu=CatBreedVisualFactory.FindDescendant(root,"DEF-upper_arm.L");var lf=CatBreedVisualFactory.FindDescendant(root,"DEF-forearm.L");
        var ru=CatBreedVisualFactory.FindDescendant(root,"DEF-upper_arm.R");var rf=CatBreedVisualFactory.FindDescendant(root,"DEF-forearm.R");
        for(int i=0;i<paths.Length;i++)
        {
            var b=root.Find(paths[i]);if(b==null)throw new InvalidOperationException("Missing source skin bone "+paths[i]);
            var motion=Under(b,lf)?CatPawReachCatalog.SkinMotion.LeftFore:Under(b,lu)?CatPawReachCatalog.SkinMotion.LeftUpper:
                Under(b,rf)?CatPawReachCatalog.SkinMotion.RightFore:Under(b,ru)?CatPawReachCatalog.SkinMotion.RightUpper:
                Under(b,chest)?CatPawReachCatalog.SkinMotion.Chest:CatPawReachCatalog.SkinMotion.Fixed;
            result[i]=new CatPawReachCatalog.SkinBone{path=paths[i],motion=motion};
        }
        foreach(var vertex in sides.SelectMany(s=>s))foreach(var influence in vertex.influences)
            influence.sourceBone=Array.IndexOf(paths,influence.bonePath);
        return result;
        bool Under(Transform bone,Transform ancestor)=>ancestor!=null&&(bone==ancestor||bone.IsChildOf(ancestor));
    }
    private static Matrix4x4[] SourceMatrices(CatPawReachCatalog.SkinBone[] bones,Transform root,Transform visual,Vector3 offset)
    {
        var matrices=new Matrix4x4[bones.Length];var worldToTag=Matrix4x4.Translate(offset)*visual.worldToLocalMatrix;
        for(int i=0;i<bones.Length;i++)matrices[i]=worldToTag*root.Find(bones[i].path).localToWorldMatrix;
        return matrices;
    }

    private static Vector3[] PawPoints(CatPawReachCatalog.PawVertex[] definitions,Vector3[] vertices,Transform skin,Transform visual,Vector3 offset)
    {
        var result=new Vector3[definitions.Length];
        for(int i=0;i<result.Length;i++)result[i]=visual.InverseTransformPoint(skin.TransformPoint(vertices[definitions[i].vertexIndex]))+offset;
        return result;
    }
    private static float ValidateSkin(CatPawReachCatalog.PawVertex[] definitions,Vector3[] vertices,SkinnedMeshRenderer skin,Transform animationRoot)
    {
        float maximum=0;
        foreach(var v in definitions)
        {
            Vector3 point=Vector3.zero;
            foreach(var influence in v.influences)
            {
                var bone=animationRoot.Find(influence.bonePath);
                if(bone==null)throw new InvalidOperationException("Paw bone path missing: "+influence.bonePath);
                point+=bone.TransformPoint(influence.bindPosition)*influence.weight;
            }
            maximum=Mathf.Max(maximum,Vector3.Distance(point,skin.transform.TransformPoint(vertices[v.vertexIndex])));
        }
        return maximum;
    }

    private sealed class LocalTransform
    {
        readonly Transform target;
        readonly Vector3 position, scale;
        readonly Quaternion rotation;
        public LocalTransform(Transform target)
        { this.target = target; position = target.localPosition; scale = target.localScale; rotation = target.localRotation; }
        public void Restore()
        { target.localPosition = position; target.localScale = scale; target.localRotation = rotation; }
    }

    private sealed class Rig
    {
        readonly Transform torso, pelvis, head, leftRear, rightRear;
        readonly Transform[] left, right;
        public Rig(Transform root)
        {
            torso = Bone(root, "DEF-spine.001"); pelvis = Bone(root, "DEF-spine"); head = Bone(root, "DEF-spine.006");
            leftRear = Bone(root, "DEF-foot.L"); rightRear = Bone(root, "DEF-foot.R");
            left = Arm(root, "L"); right = Arm(root, "R");
        }
        public CatPawReachCatalog.Sample Sample(Transform root, float phase, float minimum, Vector3 offset)
        {
            Quaternion inverse = Quaternion.Inverse(root.rotation);
            return new CatPawReachCatalog.Sample {
                phase = phase, sourceMinimumY = minimum,
                torsoPivot = Point(root, torso, offset), torsoRotation = inverse * torso.rotation, torsoLocalRotation = torso.localRotation,
                pelvis = Point(root, pelvis, offset), head = Point(root, head, offset),
                leftRear = Point(root, leftRear, offset), rightRear = Point(root, rightRear, offset),
                left = Chain(root, left, offset), right = Chain(root, right, offset) };
        }
        static CatPawReachCatalog.ArmChain Chain(Transform root, Transform[] bones, Vector3 offset)
        {
            Quaternion inverse = Quaternion.Inverse(root.rotation);
            Vector3 upper = Point(root, bones[0], offset), fore = Point(root, bones[1], offset), hand = Point(root, bones[2], offset);
            float upperLength = Vector3.Distance(upper, fore), foreLength = Vector3.Distance(fore, hand);
            if (upperLength <= .001f || foreLength <= .001f)
                throw new InvalidOperationException("Source contact limb has zero length.");
            return new CatPawReachCatalog.ArmChain { upper = upper, fore = fore, hand = hand,
                upperLength = upperLength, foreLength = foreLength,
                upperRotation = inverse * bones[0].rotation, foreRotation = inverse * bones[1].rotation, handRotation = inverse * bones[2].rotation,
                upperLocalRotation = bones[0].localRotation, foreLocalRotation = bones[1].localRotation, handLocalRotation = bones[2].localRotation };
        }
        static Transform[] Arm(Transform root, string side) => new[] {
            Bone(root, "DEF-upper_arm." + side), Bone(root, "DEF-forearm." + side), Bone(root, "DEF-hand." + side) };
        static Transform Bone(Transform root, string name) => CatBreedVisualFactory.FindDescendant(root, name) ??
            throw new InvalidOperationException("Missing source bone " + name);
        static Vector3 Point(Transform root, Transform bone, Vector3 offset) => root.InverseTransformPoint(bone.position) + offset;
    }

    private static AnimationClip StateClip(RuntimeAnimatorController controller, string stateName)
    {
        var replacements = new List<AnimatorOverrideController>();
        while (controller is AnimatorOverrideController replacement)
        { replacements.Add(replacement); controller = replacement.runtimeAnimatorController; }
        var source = controller as AnimatorController;
        if (source == null || source.layers.Length == 0) throw new InvalidOperationException("Source animator controller is unavailable.");
        var state = FindState(source.layers[0].stateMachine, stateName);
        if (state == null || !(state.motion is AnimationClip clip))
            throw new InvalidOperationException("Source state " + stateName + " must bind one measured clip.");
        if (state.mirror || state.mirrorParameterActive || state.timeParameterActive || state.cycleOffsetParameterActive || Mathf.Abs(state.cycleOffset) > .0001f)
            throw new InvalidOperationException("Source state " + stateName + " has timing/mirroring modifiers that need explicit measurement.");
        for (int i = replacements.Count - 1; i >= 0; i--)
        { var replacement = replacements[i][clip]; if (replacement != null) clip = replacement; }
        return clip;
    }

    private static AnimatorState FindState(AnimatorStateMachine machine, string name)
    {
        foreach (var state in machine.states) if (state.state.name == name) return state.state;
        foreach (var child in machine.stateMachines)
        { var found = FindState(child.stateMachine, name); if (found != null) return found; }
        return null;
    }

    private static int Region(BoneWeight weight, Transform[] bones)
    {
        var totals = new float[4];
        Add(totals, bones, weight.boneIndex0, weight.weight0); Add(totals, bones, weight.boneIndex1, weight.weight1);
        Add(totals, bones, weight.boneIndex2, weight.weight2); Add(totals, bones, weight.boneIndex3, weight.weight3);
        if (totals.Sum() < .5f) return -1;
        int best = 0;
        for (int i = 1; i < totals.Length; i++) if (totals[i] > totals[best]) best = i;
        return best;
    }
    private static void Add(float[] totals, Transform[] bones, int index, float weight)
    {
        if (weight <= 0f || index < 0 || index >= bones.Length || bones[index] == null) return;
        var bone = bones[index]; string name = bone.name;
        // The reach motion rotates spine.001; only the root pelvis stays fixed.
        int region = name == "DEF-spine" ? 0 :
            name == "DEF-spine.001" || name == "DEF-spine.002" || name == "DEF-spine.003" ? 1 :
            name == "DEF-spine.004" || name == "DEF-spine.005" ? 2 : -1;
        if (region < 0)
            for (var parent = bone; parent != null; parent = parent.parent)
                if (parent.name == "DEF-spine.006") { region = 3; break; }
        if (region >= 0) totals[region] += weight;
    }

    private static CatBodyGuardCatalog.Probe FitBody(string region, List<Vector3> points)
    {
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (var point in points) bounds.Encapsulate(point);
        var best = default(CatBodyGuardCatalog.Probe); float bestVolume = float.PositiveInfinity;
        for (int axis = 0; axis < 3; axis++)
        {
            Vector3 centre = bounds.center; float radialSquared = 0f;
            foreach (var point in points)
            { Vector3 transverse = point - centre; transverse[axis] = 0f; radialSquared = Mathf.Max(radialSquared, transverse.sqrMagnitude); }
            float first = float.PositiveInfinity, last = float.NegativeInfinity;
            foreach (var point in points)
            {
                Vector3 transverse = point - centre; transverse[axis] = 0f;
                float cap = Mathf.Sqrt(Mathf.Max(0f, radialSquared - transverse.sqrMagnitude));
                first = Mathf.Min(first, point[axis] + cap); last = Mathf.Max(last, point[axis] - cap);
            }
            if (first > last) first = last = (first + last) * .5f;
            Vector3 start = centre, end = centre; start[axis] = first; end[axis] = last;
            // A small source-space shell covers numerical skin interpolation.
            // It scales with the visual; floor clearance is separate world data.
            float radius = Mathf.Sqrt(radialSquared) + .004f;
            float volume = Mathf.PI * radius * radius * (Vector3.Distance(start, end) + 4f * radius / 3f);
            if (volume >= bestVolume) continue;
            bestVolume = volume;
            best = new CatBodyGuardCatalog.Probe { region = region, start = start, end = end, radius = radius };
        }
        return best;
    }
    private static string F(float value) => value.ToString("F6", CultureInfo.InvariantCulture);
}
