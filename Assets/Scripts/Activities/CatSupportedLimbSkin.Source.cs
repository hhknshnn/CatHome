using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class CatSupportedLimbSkin
{
    int[] sourceBoneMap,sourceChainBones;
    string[] sourceInputPaths,sourceProfilePaths;
    string sourceBindingKey,sourceBindingHash;

    // Compatibility API remains independently usable by measurements. A new
    // instance is returned; production uses BindSource plus CaptureSource.
    public static CatSupportedLimbSkin FromSource(CatCareSkinCatalog.Profile profile,string[] paths,Matrix4x4[] worldMatrices)
    {
        var result=BindSource(profile,paths);
        return result!=null&&result.CaptureSource(Matrix4x4.identity,worldMatrices)?result:null;
    }

    // Only immutable topology/path membership is reused. No world pose or
    // collision result is stored as permission for another query.
    public static CatSupportedLimbSkin BindSource(CatCareSkinCatalog.Profile profile,string[] paths)
    {
        if(profile?.bonePaths==null||paths==null||paths.Length==0)return null;
        var lookup=new Dictionary<string,int>();
        for(int i=0;i<paths.Length;i++)
        {if(string.IsNullOrEmpty(paths[i])||lookup.ContainsKey(paths[i]))return null;lookup.Add(paths[i],i);}
        var result=new CatSupportedLimbSkin(profile,new Transform[profile.bonePaths.Length])
        {
            sourceBoneMap=new int[profile.bonePaths.Length],sourceChainBones=new int[12],
            sourceInputPaths=(string[])paths.Clone(),sourceProfilePaths=(string[])profile.bonePaths.Clone(),
            sourceBindingKey=profile.sourceKey,sourceBindingHash=profile.sourceHash
        };
        for(int i=0;i<profile.bonePaths.Length;i++)
        {if(!lookup.TryGetValue(profile.bonePaths[i],out int at))return null;result.sourceBoneMap[i]=at;}
        for(int leg=0;leg<4;leg++)
        {
            string side=(leg&1)==0?"L":"R";
            int upper=Find((leg<2?"DEF-upper_arm.":"DEF-thigh.")+side),
                lower=Find((leg<2?"DEF-forearm.":"DEF-shin.")+side),foot=Find((leg<2?"DEF-hand.":"DEF-foot.")+side);
            if(upper<0||lower<0||foot<0)return null;
            result.sourceChainBones[leg*3]=upper;result.sourceChainBones[leg*3+1]=lower;result.sourceChainBones[leg*3+2]=foot;
            var chain=new Chain{stage=new byte[profile.bonePaths.Length]};
            for(int i=0;i<chain.stage.Length;i++)chain.stage[i]=Under(i,foot)?(byte)3:Under(i,lower)?(byte)2:Under(i,upper)?(byte)1:(byte)0;
            var used=new List<int>();for(int v=0;v<profile.vertexIndices.Length;v++)
                for(int i=profile.starts[v];i<profile.starts[v+1];i++)if(chain.stage[profile.bones[i]]!=0){used.Add(v);break;}
            chain.vertices=used.ToArray();result.chains[leg]=chain;
        }
        result.IndexBones();return result;
        int Find(string name){for(int i=0;i<profile.bonePaths.Length;i++)if(profile.bonePaths[i]==name||profile.bonePaths[i].EndsWith("/"+name,StringComparison.Ordinal))return i;return -1;}
        bool Under(int i,int ancestor)=>i==ancestor||profile.bonePaths[i].StartsWith(profile.bonePaths[ancestor]+"/",StringComparison.Ordinal);
    }

    public bool MatchesSource(CatCareSkinCatalog.Profile profile,string[] paths)
    {
        if(sourceBoneMap==null||!ReferenceEquals(source,profile)||profile.sourceKey!=sourceBindingKey||profile.sourceHash!=sourceBindingHash||
            paths==null||paths.Length!=sourceInputPaths.Length||profile.bonePaths==null||profile.bonePaths.Length!=sourceProfilePaths.Length)return false;
        for(int i=0;i<paths.Length;i++)if(paths[i]!=sourceInputPaths[i])return false;
        for(int i=0;i<profile.bonePaths.Length;i++)if(profile.bonePaths[i]!=sourceProfilePaths[i])return false;
        return true;
    }

    // Every world matrix, influence and chain pose is overwritten on every
    // successful call. Mapping includes current root, heading and grounding.
    // Borrowed scratch: consume synchronously before another CaptureSource.
    public bool CaptureSource(Matrix4x4 mapping,Matrix4x4[] sourceMatrices)
    {
        if(sourceBoneMap==null||sourceMatrices==null||sourceMatrices.Length!=sourceInputPaths.Length)return false;
        for(int i=0;i<matrices.Length;i++)matrices[i]=mapping*sourceMatrices[sourceBoneMap[i]];
        for(int i=0;i<influences.Length;i++)influences[i]=matrices[source.bones[i]].MultiplyPoint3x4(source.bindPoints[i]);
        for(int leg=0;leg<4;leg++)
        {
            var chain=chains[leg];int upper=sourceChainBones[leg*3],lower=sourceChainBones[leg*3+1],foot=sourceChainBones[leg*3+2];
            chain.upperPoint=matrices[upper].MultiplyPoint3x4(Vector3.zero);chain.lowerPoint=matrices[lower].MultiplyPoint3x4(Vector3.zero);
            chain.footPoint=matrices[foot].MultiplyPoint3x4(Vector3.zero);chain.upperRotation=matrices[upper].rotation;chain.footRotation=matrices[foot].rotation;
        }
        return true;
    }
}
