using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Read-only samples of the untouched vendor jump curves.</summary>
public static class CatJumpSourceAudit
{
    public static string Capture(string folder)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        Directory.CreateDirectory(folder);
        var catalog = CatBreedCatalog.Load();
        var breed = catalog.Find("oriental-shorthair");
        var visual = CatBreedVisualFactory.Create(breed, catalog.GameplayController, null, "Jump source measurement");
        var report = new StringBuilder("clip,seconds,phase,hipX,hipY,hipZ,lfX,lfY,lfZ,rfX,rfY,rfZ,lrX,lrY,lrZ,rrX,rrY,rrZ\n");
        try
        {
            visual.transform.localScale *= .5f;
            var animator = visual.GetComponentInChildren<Animator>(); animator.enabled = false;
            var bones = new[] {"DEF-spine", "DEF-hand.L", "DEF-hand.R", "DEF-foot.L", "DEF-foot.R"}
                .Select(n => CatBreedVisualFactory.FindDescendant(animator.transform, n)).ToArray();
            foreach (string name in new[] {"Jump", "JumpUp", "JumpDown", "Idle", "Sleeping"})
            {
                var clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name.EndsWith("|" + name, StringComparison.Ordinal));
                if (clip == null) clip = AssetDatabase.LoadAllAssetsAtPath(PolyperfectCatIntegrationBuilder.SourceModelPath).OfType<AnimationClip>()
                    .FirstOrDefault(c => !c.name.StartsWith("__preview__") && c.name.EndsWith("|" + name, StringComparison.Ordinal));
                if (clip == null) continue;
                int count = name == "Idle" || name == "Sleeping" ? 1 : 120;
                for (int i = 0; i <= count; i++)
                {
                    clip.SampleAnimation(animator.gameObject, clip.length * i / count);
                    report.Append(name).Append(',').Append(clip.length.ToString("F5", CultureInfo.InvariantCulture)).Append(',').Append(((float)i / count).ToString("F5", CultureInfo.InvariantCulture));
                    foreach (var bone in bones)
                    {
                        var p = bone.position;
                        report.AppendFormat(CultureInfo.InvariantCulture, ",{0:F5},{1:F5},{2:F5}", p.x, p.y, p.z);
                    }
                    report.AppendLine();
                }
            }
        }
        finally { Object.DestroyImmediate(visual); }
        File.WriteAllText(folder + "/native-jump-curves.csv", report.ToString());
        return "Native jump curves sampled without modification.";
    }
}
