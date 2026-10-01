using System;
using UnityEngine;

/// <summary>Exact linear skinning of the source front limbs after the existing
/// chest rotation and CCD joint rotations. No stretch or model mutation.</summary>
public static class CatPawWeightedSkin
{
    // Two upper/fore twist chains, hand/toes and a small torso seam halo.
    // Unexpected anatomy refuses a profile; no overflow vertex is discarded.
    public const int MaximumBoneGroups = 17; // measured maximum across all ten complete arm/torso seams
    public sealed class Frame
    {
        public int[] starts;
        public Vector3[] points;
        public float[] weights;
        public CatPawReachCatalog.SkinMotion[] motions;
    }
    public static Frame Build(CatPawReachCatalog.Entry entry,CatPawReachCatalog.Sample sample,
        Matrix4x4 world,CatPawReachCatalog.PawVertex[] definitions)
    {
        return Build(entry?.skinBones,sample?.skinMatrices,world,definitions,Vector3.up*CatPawReachCatalog.GroundClearance);
    }
    // Shared source skinning for static NativeJump and the dynamic paw solver.
    // Offset is explicit: paw adds .008 worldY; jump applies its existing root correction later.
    public static Frame Build(CatPawReachCatalog.SkinBone[] bones,Matrix4x4[] matrices,
        Matrix4x4 world,CatPawReachCatalog.PawVertex[] definitions,Vector3 worldOffset)
    {
        if(bones==null||matrices==null||matrices.Length!=bones.Length||
            definitions==null||definitions.Length==0)return null;
        int count=0;
        foreach(var vertex in definitions)
        {
            if(vertex?.influences==null||vertex.influences.Length==0)return null;
            float total=0;
            foreach(var influence in vertex.influences)
            {
                if(influence==null||influence.sourceBone<0||influence.sourceBone>=bones.Length||
                    influence.weight<=0||bones[influence.sourceBone].path!=influence.bonePath)return null;
                total+=influence.weight;count++;
            }
            if(Mathf.Abs(total-1f)>.0001f)return null;
        }
        var result=new Frame{starts=new int[definitions.Length+1],points=new Vector3[count],
            weights=new float[count],motions=new CatPawReachCatalog.SkinMotion[count]};
        int at=0;
        for(int v=0;v<definitions.Length;v++)
        {
            result.starts[v]=at;
            foreach(var influence in definitions[v].influences)
            {
                result.points[at]=world.MultiplyPoint3x4(matrices[influence.sourceBone].MultiplyPoint3x4(influence.bindPosition))+
                    worldOffset;
                result.weights[at]=influence.weight;
                result.motions[at++]=bones[influence.sourceBone].motion;
            }
        }
        result.starts[definitions.Length]=at;return result;
    }
    public static void Fill(Frame frame,Vector3 chestPivot,Quaternion chest,bool active,bool activeLeft,
        CatPawSurfaceCcd.State solve,Vector3 sourceUpper,Vector3 sourceFore,Vector3[] result)
    {
        var upper=activeLeft?CatPawReachCatalog.SkinMotion.LeftUpper:CatPawReachCatalog.SkinMotion.RightUpper;
        var fore=activeLeft?CatPawReachCatalog.SkinMotion.LeftFore:CatPawReachCatalog.SkinMotion.RightFore;
        for(int v=0;v<result.Length;v++)
        {
            Vector3 point=Vector3.zero;
            for(int i=frame.starts[v];i<frame.starts[v+1];i++)
            {
                var motion=frame.motions[i];Vector3 weighted=frame.points[i];
                if(motion!=CatPawReachCatalog.SkinMotion.Fixed)weighted=chestPivot+chest*(weighted-chestPivot);
                if(active&&motion==upper)weighted=solve.Upper+solve.UpperRotation*(weighted-sourceUpper);
                else if(active&&motion==fore)weighted=solve.Fore+solve.PawRotation*(weighted-sourceFore);
                point+=weighted*frame.weights[i];
            }
            result[v]=point;
        }
    }
}
