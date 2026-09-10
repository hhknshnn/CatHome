using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

/// <summary>Material only: follows the existing sand mesh, scale and excavation.</summary>
public static class LitterSandMaterialBuilder
{
    public const string ShaderName = "CatHome/Litter Sand";
    const string Folder = "Assets/Art/ModernPolish/Materials";

    public static void Apply(MeshFilter sand, float localPlane)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Litter material authoring requires Edit Mode.");
        if (sand == null || sand.GetComponent<MeshRenderer>() == null)
            throw new InvalidOperationException("Litter material requires its actual sand renderer.");
        var shader = Shader.Find(ShaderName);
        if (shader == null) throw new InvalidOperationException("Import LitterSand.shader first.");
        float scale = Mathf.Max(.001f, sand.transform.TransformVector(Vector3.up).y);
        float depth = CatLitterSandSurface.MaximumDepth / scale;
        string key = localPlane.ToString("R", CultureInfo.InvariantCulture) + "/" + depth.ToString("R", CultureInfo.InvariantCulture);
        string path = Folder + "/LitterSand_" + Hash128.Compute(key).ToString().Substring(0,12) + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = "Litter Sand" };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetColor("_BaseColor", new Color(.81f,.69f,.46f,1f));
        material.SetColor("_Color", new Color(.81f,.69f,.46f,1f));
        material.SetFloat("_SandPlane", localPlane);
        material.SetFloat("_MaxDepthLocal", depth);
        material.SetFloat("_CavityShade", .37f); material.SetFloat("_RimLight", .1f);
        material.SetFloat("_GrainFrequency", 220f); material.SetFloat("_GrainContrast", .06f);
        material.SetFloat("_Metallic", 0f); material.SetFloat("_Smoothness", .16f);
        material.SetFloat("_Cull", 2f); material.SetFloat("_ZWrite", 1f);
        material.SetFloat("_Surface", 0f); material.SetFloat("_SrcBlend", 1f); material.SetFloat("_DstBlend", 0f);
        material.SetFloat("_SrcBlendAlpha", 1f); material.SetFloat("_DstBlendAlpha", 0f);
        material.SetTexture("_BaseMap", null); material.SetTexture("_DetailAlbedoMap", null);
        material.enableInstancing = true;
        sand.GetComponent<MeshRenderer>().sharedMaterial = material;
        EditorUtility.SetDirty(material); EditorUtility.SetDirty(sand.GetComponent<MeshRenderer>());
    }
}
