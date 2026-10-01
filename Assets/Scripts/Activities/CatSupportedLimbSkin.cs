using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Source-preserving two-link candidate and its original weighted skin.
/// This is pure geometry: no live transform is assigned and no mesh is read/baked.</summary>
public sealed partial class CatSupportedLimbSkin
{
    public struct Pose
    {
        public Vector3 sourceUpper, elbow, foot, translation, bodyPivot;
        public Quaternion bodyRotation;
        public Quaternion upperDelta, lowerDelta, footDelta;
        internal Matrix4x4 bodyMatrix,upperMatrix,lowerMatrix,footMatrix;
    }
    sealed class Chain
    {
        public Transform upper,lower,foot;
        public byte[] stage;
        public int[] vertices;
        public Vector3 upperPoint,lowerPoint,footPoint;
        public Quaternion upperRotation,footRotation;
    }
    readonly CatCareSkinCatalog.Profile source;
    readonly Transform[] bones;
    readonly Matrix4x4[] matrices;
    readonly Vector3[] influences;
    readonly Dictionary<int,int> sourceToSlot=new Dictionary<int,int>();
    readonly Chain[] chains=new Chain[4];
    int[] boneLimb;
    byte[] boneStage;
    CatSupportedLimbSkin(CatCareSkinCatalog.Profile profile,Transform[] bound)
    {
        source=profile;bones=bound;matrices=new Matrix4x4[bound.Length];influences=new Vector3[source.weights.Length];
        for(int i=0;i<source.vertexIndices.Length;i++)sourceToSlot.Add(source.vertexIndices[i],i);
    }
    public static CatSupportedLimbSkin Bind(CatCareSkinCatalog.Profile source,Animator animator,SkinnedMeshRenderer skin)
    {
        if(!CatCareWeightedSkin.TryBind(source,animator,skin,out _))return null;
        var bound=new Transform[source.bonePaths.Length];
        for(int i=0;i<bound.Length;i++)bound[i]=animator.transform.Find(source.bonePaths[i]);
        var result=new CatSupportedLimbSkin(source,bound);
        for(int leg=0;leg<4;leg++)
        {
            string side=(leg&1)==0?"L":"R";
            var chain=new Chain{upper=CatBreedVisualFactory.FindDescendant(animator.transform,(leg<2?"DEF-upper_arm.":"DEF-thigh.")+side),
                lower=CatBreedVisualFactory.FindDescendant(animator.transform,(leg<2?"DEF-forearm.":"DEF-shin.")+side),
                foot=CatBreedVisualFactory.FindDescendant(animator.transform,(leg<2?"DEF-hand.":"DEF-foot.")+side),stage=new byte[bound.Length]};
            if(chain.upper==null||chain.lower==null||chain.foot==null)return null;
            for(int b=0;b<bound.Length;b++)chain.stage[b]=Descends(bound[b],chain.foot)?(byte)3:Descends(bound[b],chain.lower)?(byte)2:Descends(bound[b],chain.upper)?(byte)1:(byte)0;
            var used=new List<int>();
            for(int v=0;v<source.vertexIndices.Length;v++)
                for(int i=source.starts[v];i<source.starts[v+1];i++)if(chain.stage[source.bones[i]]!=0){used.Add(v);break;}
            chain.vertices=used.ToArray();result.chains[leg]=chain;
        }
        result.IndexBones();return result;
    }
    void IndexBones()
    {
        boneLimb=new int[bones.Length];boneStage=new byte[bones.Length];
        for(int b=0;b<bones.Length;b++)
        {
            boneLimb[b]=-1;
            for(int leg=0;leg<4;leg++)if(chains[leg].stage[b]!=0)
            {boneLimb[b]=leg;boneStage[b]=chains[leg].stage[b];break;}
        }
    }
    static bool Descends(Transform value,Transform ancestor)=>value==ancestor||value.IsChildOf(ancestor);
    public bool Capture()
    {
        for(int b=0;b<bones.Length;b++){if(bones[b]==null)return false;matrices[b]=bones[b].localToWorldMatrix;}
        for(int i=0;i<influences.Length;i++)influences[i]=matrices[source.bones[i]].MultiplyPoint3x4(source.bindPoints[i]);
        foreach(var chain in chains)
        {
            chain.upperPoint=chain.upper.position;chain.lowerPoint=chain.lower.position;chain.footPoint=chain.foot.position;
            chain.upperRotation=chain.upper.rotation;chain.footRotation=chain.foot.rotation;
        }
        return true;
    }
    public IReadOnlyList<int> Vertices(int leg)=>chains[leg].vertices;
    public int SourceIndex(int slot)=>source.vertexIndices[slot];
    public int Slot(int sourceIndex)=>sourceToSlot.TryGetValue(sourceIndex,out int slot)?slot:-1;
    public bool TryPose(int leg,Vector3 target,Vector3 desiredLower,Vector3 translation,out Pose pose)
        =>TryPose(leg,target,desiredLower,translation,Quaternion.identity,Vector3.zero,out pose);
    public bool TryPose(int leg,Vector3 target,Vector3 desiredLower,Vector3 translation,Quaternion bodyRotation,Vector3 bodyPivot,out Pose pose)
        =>TryPose(leg,target,desiredLower,translation,bodyRotation,bodyPivot,Quaternion.identity,out pose);
    public bool TryPose(int leg,Vector3 target,Vector3 desiredLower,Vector3 translation,Quaternion bodyRotation,Vector3 bodyPivot,Quaternion footOrientation,out Pose pose)
    {
        pose=default;if(leg<0||leg>=4)return false;var chain=chains[leg];
        Vector3 upper=BodyPoint(chain.upperPoint,translation,bodyRotation,bodyPivot),
            lower=BodyPoint(chain.lowerPoint,translation,bodyRotation,bodyPivot),foot=BodyPoint(chain.footPoint,translation,bodyRotation,bodyPivot);
        Vector3 delta=target-upper;if(delta.sqrMagnitude<1e-12f)return false;
        float a=Vector3.Distance(upper,lower),b=Vector3.Distance(lower,foot);
        float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.0001f,a+b-.0001f);
        Vector3 axis=delta.normalized,sourceAxis=(foot-upper).normalized;
        Vector3 sourceBend=Vector3.ProjectOnPlane(lower-upper,sourceAxis);
        if(sourceBend.sqrMagnitude<.000025f)sourceBend=Vector3.ProjectOnPlane(bodyRotation*chain.upperRotation*Vector3.forward,sourceAxis);
        Vector3 bend=Quaternion.FromToRotation(sourceAxis,axis)*sourceBend.normalized;
        Vector3 wanted=Vector3.ProjectOnPlane(desiredLower-upper,axis);
        if(wanted.sqrMagnitude>.0000001f)bend=Vector3.RotateTowards(bend,wanted.normalized,25f*Mathf.Deg2Rad,0f).normalized;
        float along=(a*a-b*b+distance*distance)/(2f*distance);
        Vector3 elbow=upper+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        Quaternion first=Quaternion.FromToRotation(lower-upper,elbow-upper);
        Vector3 firstFoot=upper+first*(foot-upper);
        Quaternion second=Quaternion.FromToRotation(firstFoot-elbow,target-elbow);
        Vector3 finalFoot=elbow+second*(firstFoot-elbow);
        Quaternion last=footOrientation*chain.footRotation*Quaternion.Inverse(second*first*bodyRotation*chain.footRotation);
        pose=new Pose{sourceUpper=upper,elbow=elbow,foot=finalFoot,translation=translation,bodyRotation=bodyRotation,bodyPivot=bodyPivot,upperDelta=first,lowerDelta=second,footDelta=last};
        PrepareMatrices(ref pose);return true;
    }
    public int VertexCount => source.vertexIndices.Length;
    public Pose SourcePose(int leg,Vector3 translation)=>SourcePose(leg,translation,Quaternion.identity,Vector3.zero);
    public Pose SourcePose(int leg,Vector3 translation,Quaternion bodyRotation,Vector3 bodyPivot)
    {
        var chain=chains[leg];
        var pose=new Pose{sourceUpper=BodyPoint(chain.upperPoint,translation,bodyRotation,bodyPivot),
            elbow=BodyPoint(chain.lowerPoint,translation,bodyRotation,bodyPivot),foot=BodyPoint(chain.footPoint,translation,bodyRotation,bodyPivot),
            translation=translation,bodyRotation=bodyRotation,bodyPivot=bodyPivot,upperDelta=Quaternion.identity,
            lowerDelta=Quaternion.identity,footDelta=Quaternion.identity};
        PrepareMatrices(ref pose);return pose;
    }
    static Matrix4x4 Around(Vector3 pivot,Quaternion rotation)=>Matrix4x4.TRS(pivot-rotation*pivot,rotation,Vector3.one);
    static void PrepareMatrices(ref Pose pose)
    {
        pose.bodyMatrix=Matrix4x4.TRS(pose.bodyPivot-pose.bodyRotation*pose.bodyPivot+pose.translation,pose.bodyRotation,Vector3.one);
        pose.upperMatrix=Around(pose.sourceUpper,pose.upperDelta)*pose.bodyMatrix;
        pose.lowerMatrix=Around(pose.elbow,pose.lowerDelta)*pose.upperMatrix;
        pose.footMatrix=Around(pose.foot,pose.footDelta)*pose.lowerMatrix;
    }
    public static Vector3 BodyPoint(Vector3 point,Vector3 translation,Quaternion rotation,Vector3 pivot)
        =>pivot+rotation*(point-pivot)+translation;
    public Vector3 Upper(int leg)=>chains[leg].upperPoint;
    public Vector3 Lower(int leg)=>chains[leg].lowerPoint;
    public Vector3 Foot(int leg)=>chains[leg].footPoint;
    public Vector3 SourcePoint(int slot)
    {Vector3 value=Vector3.zero;for(int i=source.starts[slot];i<source.starts[slot+1];i++)value+=influences[i]*source.weights[i];return value;}
    // Each original bone belongs to at most one of the four limb subtrees.
    // Blend original influences only after applying that bone's actual chain;
    // mixed pelvis/limb and adjacent-limb vertices retain every source weight.
    public Vector3 CombinedPoint(int slot,Pose[] poses,Vector3 translation)
        =>CombinedPoint(slot,poses,translation,Quaternion.identity,Vector3.zero);
    public Vector3 CombinedPoint(int slot,Pose[] poses,Vector3 translation,Quaternion bodyRotation,Vector3 bodyPivot)
    {
        Vector3 value=Vector3.zero;
        Vector3 bodyOffset=bodyPivot-bodyRotation*bodyPivot+translation;
        for(int i=source.starts[slot];i<source.starts[slot+1];i++)
        {
            int bone=source.bones[i],leg=boneLimb[bone],stage=boneStage[bone];Vector3 p;
            if(leg<0)p=bodyRotation*influences[i]+bodyOffset;
            else if(stage==1)p=poses[leg].upperMatrix.MultiplyPoint3x4(influences[i]);
            else if(stage==2)p=poses[leg].lowerMatrix.MultiplyPoint3x4(influences[i]);
            else p=poses[leg].footMatrix.MultiplyPoint3x4(influences[i]);
            value+=p*source.weights[i];
        }
        return value;
    }
    public Vector3 Point(int leg,int slot,Pose pose)
    {
        var chain=chains[leg];Vector3 value=Vector3.zero;
        for(int i=source.starts[slot];i<source.starts[slot+1];i++)
        {
            int stage=chain.stage[source.bones[i]];
            Vector3 p=stage==0?pose.bodyMatrix.MultiplyPoint3x4(influences[i]):stage==1?pose.upperMatrix.MultiplyPoint3x4(influences[i]):
                stage==2?pose.lowerMatrix.MultiplyPoint3x4(influences[i]):pose.footMatrix.MultiplyPoint3x4(influences[i]);
            value+=p*source.weights[i];
        }
        return value;
    }
}
