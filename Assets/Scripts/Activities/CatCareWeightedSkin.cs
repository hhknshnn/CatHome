using UnityEngine;

/// <summary>Exact source contact vertices from cached bone matrices. No BakeMesh,
/// runtime mesh read, collider mutation, per-frame allocation or pose adjustment.</summary>
public sealed class CatCareWeightedSkin
{
    readonly CatCareSkinCatalog.Profile source;
    readonly Transform[] bones;
    readonly Matrix4x4[] matrices;
    public int Count=>source.vertexIndices.Length;
    public int SourceIndex(int index)=>source.vertexIndices[index];
    CatCareWeightedSkin(CatCareSkinCatalog.Profile profile,Transform[] transforms)
    {source=profile;bones=transforms;matrices=new Matrix4x4[bones.Length];}
    public static bool TryBind(CatCareSkinCatalog.Profile profile,Animator animator,SkinnedMeshRenderer skin,out CatCareWeightedSkin result)
    {
        result=null;
        if(profile==null||animator==null||skin==null||skin.sharedMesh==null||profile.meshVertexCount!=skin.sharedMesh.vertexCount||
            profile.meshName!=skin.sharedMesh.name||profile.vertexIndices==null||profile.vertexIndices.Length==0||
            profile.starts==null||profile.starts.Length!=profile.vertexIndices.Length+1||profile.bonePaths==null||profile.bones==null||
            profile.bindPoints==null||profile.weights==null||profile.bones.Length!=profile.bindPoints.Length||profile.bones.Length!=profile.weights.Length||
            profile.starts[0]!=0||profile.starts[profile.starts.Length-1]!=profile.weights.Length)return false;
        var transforms=new Transform[profile.bonePaths.Length];
        for(int i=0;i<transforms.Length;i++){transforms[i]=animator.transform.Find(profile.bonePaths[i]);if(transforms[i]==null)return false;}
        for(int vertex=0;vertex<profile.vertexIndices.Length;vertex++)
        {
            if(profile.vertexIndices[vertex]<0||profile.vertexIndices[vertex]>=profile.meshVertexCount||
                profile.starts[vertex+1]<=profile.starts[vertex]||profile.starts[vertex+1]>profile.weights.Length)return false;
            float sum=0;
            for(int i=profile.starts[vertex];i<profile.starts[vertex+1];i++)
            {if(profile.bones[i]<0||profile.bones[i]>=transforms.Length||!(profile.weights[i]>0))return false;sum+=profile.weights[i];}
            if(Mathf.Abs(sum-1f)>.0001f)return false;
        }
        result=new CatCareWeightedSkin(profile,transforms);return result.CaptureMatrices();
    }
    public bool CaptureMatrices()
    {
        for(int i=0;i<bones.Length;i++){if(bones[i]==null)return false;matrices[i]=bones[i].localToWorldMatrix;}
        return true;
    }
    public Vector3 Point(int vertex)
    {
        Vector3 point=Vector3.zero;
        for(int i=source.starts[vertex];i<source.starts[vertex+1];i++)
            point+=matrices[source.bones[i]].MultiplyPoint3x4(source.bindPoints[i])*source.weights[i];
        return point;
    }
}
