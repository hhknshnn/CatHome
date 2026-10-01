#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class CatSupportedLimbMaskTests
{
    [Test] public void TenBreeds_SupportedLimbAndDistalMasks_MatchOriginalSkinWeights()
    {
        foreach(var breed in CatBreedCatalog.Load().Entries)
        {
            var skin=breed.SourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var weights=skin.sharedMesh.boneWeights;var bones=skin.bones;
            for(int limb=0;limb<4;limb++)
            {
                string side=(limb&1)==0?"L":"R";
                var root=bones.Single(b=>b.name==(limb<2?"DEF-upper_arm.":"DEF-thigh.")+side);
                var distal=bones.Single(b=>b.name==(limb<2?"DEF-hand.":"DEF-foot.")+side);
                var expected=new List<int>();var expectedDistal=new List<int>();
                for(int v=0;v<weights.Length;v++)
                {
                    if(Total(weights[v],root)>=.5f)expected.Add(v);
                    if(Total(weights[v],distal)>=.5f)expectedDistal.Add(v);
                }
                Assert.That(breed.SupportLimbVertices(limb),Is.EqualTo(expected),breed.Id+" limb "+limb);
                Assert.That(breed.SupportPawVertices(limb),Is.EqualTo(expectedDistal),breed.Id+" unchanged distal "+limb);
                Assert.That(expectedDistal.All(v=>expected.Contains(v)),Is.True);
            }
            float Total(BoneWeight w,Transform joint)
            {
                return Part(w.boneIndex0,w.weight0)+Part(w.boneIndex1,w.weight1)+Part(w.boneIndex2,w.weight2)+Part(w.boneIndex3,w.weight3);
                float Part(int index,float weight)=>weight>0&&(bones[index]==joint||bones[index].IsChildOf(joint))?weight:0;
            }
        }
    }
}
#endif
