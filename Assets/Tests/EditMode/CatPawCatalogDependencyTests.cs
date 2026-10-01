#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CatPawCatalogDependencyTests
{
    const string Manifest="Docs/QA/INTERACTION_POLISH_2026-09-16/paw-surface-stage/clip-reference-cleanup/apply-manifest.json";
    [Serializable] sealed class Snapshot { public FileEntry[] files; }
    [Serializable] sealed class FileEntry { public string path,stagedSha256,numericPosePayloadSha256; }
    static FileEntry[] Shards()
    {
        Assert.That(File.Exists(Manifest),Is.True,"The local QA byte baseline is required, never silently skipped.");
        var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(Manifest));
        var shards=snapshot.files.Where(f=>f.path.EndsWith(".asset",StringComparison.Ordinal)).ToArray();
        Assert.That(shards.Length,Is.EqualTo(20));return shards;
    }
    static string Sha(byte[] bytes)
    {
        using(var hash=SHA256.Create())return string.Concat(hash.ComputeHash(bytes).Select(b=>b.ToString("x2")));
    }

    [Test] public void TwentyShards_PreserveEveryPoseByte_AndRemoveOnlyUnusedReferences()
    {
        foreach(var entry in Shards())
        {
            var bytes=File.ReadAllBytes(entry.path);
            Assert.That(Sha(bytes),Is.EqualTo(entry.stagedSha256),entry.path+" changed after reviewed staging");
            Assert.That(Sha(bytes),Is.EqualTo(entry.numericPosePayloadSha256),entry.path+" differs from original after only clip-line removal");
            Assert.That(Regex.IsMatch(System.Text.Encoding.UTF8.GetString(bytes),@"(?m)^\s*sourceClip:"),Is.False,entry.path);
        }
    }

    [Test] public void TwentyShards_HaveNoUnityObjectReferences_ExceptTheirScript()
    {
        Assert.That(typeof(CatPawReachCatalog.Entry).GetField("sourceClip"),Is.Null);
        foreach(var entry in Shards())
        {
            var catalog=AssetDatabase.LoadAssetAtPath<CatPawReachCatalog>(entry.path);
            Assert.That(catalog,Is.Not.Null,entry.path);
            using(var serialized=new SerializedObject(catalog))
            {
                var property=serialized.GetIterator();
                while(property.Next(true))
                {
                    if(property.propertyType!=SerializedPropertyType.ObjectReference||property.propertyPath=="m_Script")continue;
                    Assert.That(property.objectReferenceValue,Is.Null,entry.path+" / "+property.propertyPath);
                }
            }
            foreach(string dependency in AssetDatabase.GetDependencies(entry.path,false))
            {
                if(dependency==entry.path)continue;
                Assert.That(AssetDatabase.LoadMainAssetAtPath(dependency),Is.InstanceOf<MonoScript>(),
                    entry.path+" must not pull an animation/model/controller dependency: "+dependency);
            }
        }
    }
}
#endif
