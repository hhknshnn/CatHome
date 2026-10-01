#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class NaturalSpringToyTests
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    bool allBreeds;
    CareAlignmentPolishTests home; ActionReadyReviewTests review; MediaEncoder encoder;
    string Output=>SessionState.GetString("CatHome.QA.ResultDirectory","Temp");
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    [SetUp] public void Before(){allBreeds=false;home=new CareAlignmentPolishTests();home.Before();review=new ActionReadyReviewTests();typeof(ActionReadyReviewTests).GetField("home",F).SetValue(review,home);}
    [TearDown] public void After(){encoder?.Dispose();encoder=null;home.After();}
    [UnityTest,Timeout(180000)] public IEnumerator AllBreeds_ActualSkinContactAndClearance(){allBreeds=true;yield return ThreeSizes_ActualSkinContactAndClearance();}
    [UnityTest,Timeout(180000)] public IEnumerator ThreeSizes_ActualSkinContactAndClearance()
    {
        yield return (IEnumerator)Call(home,"Home");
        int sizeIndex=0;
        foreach(string breed in allBreeds?CatBreedCatalog.Load().Entries.Select(e=>e.Id).ToArray():new[]{"persian","domestic-shorthair","maine-coon"})
        {
            yield return (IEnumerator)Call(home,"Breed",breed);
            var cat=Object.FindAnyObjectByType<CatMovement>();yield return QaBreedReadiness.WaitForSelected(cat,breed);
            HomeStoreService.TrySetStored(HomeStoreService.FeatherToyId,false);yield return null;yield return null;
            var toy=CatActivity.Registered.OfType<CatEnrichmentActivity>().Single(a=>a.Mode==CatEnrichmentMode.Spring);
            var row=new ActionReadyReviewTests.Row();yield return (IEnumerator)Call(review,"FindStance",toy,row);if(!row.ready){for(int n=0;n<16&&!row.ready;n++){var side=Quaternion.Euler(0,n*22.5f,0)*Vector3.forward;var p=toy.MovingPart.position+side*CatSpringGeometry.Measure(cat).StandDistance;p.y=toy.RoutineEntryPoint.position.y;for(int k=0;k<4;k++){Call(home,"Place",p,Quaternion.LookRotation(-side));toy.TryGetStartPose(cat,out var pose);p=pose.ZoneCentre;}Call(home,"Place",p,Quaternion.LookRotation(-side));yield return null;row.ready=toy.CanStartFromPrompt(cat,out _);}}Assert.That(row.ready,Is.True);
            // Remain inside the existing admission cone; production Face performs the turn.
            toy.TryGetStartPose(cat,out var admitted);
            cat.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(admitted.ActionTarget-cat.transform.position,Vector3.up))*Quaternion.Euler(0,20,0);Physics.SyncTransforms();yield return null;
            bool video=!allBreeds;Time.captureFramerate=24;
            var geometry=CatSpringGeometry.Measure(cat);
            var targetSet=new CatMeshContactSurface.TargetSet(toy.MovingPart);
            var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();
            float minHead=float.PositiveInfinity,minSigned=float.PositiveInfinity;
            File.AppendAllText(Path.Combine(Output,"sizes.txt"),breed+" height="+geometry.Height+" reach="+geometry.Reach+" stand="+geometry.StandDistance+" paw="+geometry.Paw+" headVertices="+geometry.HeadVertices.Length+"\n");
            var camera=Camera.main;Vector3 focus=(cat.transform.position+toy.ContactPoint.position)*.5f+Vector3.up*.14f;
            foreach(var component in camera.GetComponents<MonoBehaviour>())component.enabled=false;
            camera.transform.position=focus+cat.transform.right*1.7f+cat.transform.forward*.05f+Vector3.up*.65f;
            camera.transform.LookAt(focus);camera.fieldOfView=38;
            if(video&&encoder==null)encoder=new MediaEncoder(Path.Combine(Output,"CatHome_Yayli_Oyuncak_3_Boyut.mp4"),new VideoTrackAttributes{frameRate=new MediaRational(24),width=(uint)Screen.width,height=(uint)Screen.height,includeAlpha=false,bitRateMode=VideoBitrateMode.High});
            GameObject caption=null;
            if(video){caption=new GameObject("Size review caption",typeof(Canvas));var canvas=caption.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;var panel=new GameObject("Label",typeof(RectTransform),typeof(UnityEngine.UI.Image));panel.transform.SetParent(caption.transform,false);panel.GetComponent<UnityEngine.UI.Image>().color=new Color(1,1,.96f,.92f);var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-145);rect.sizeDelta=new Vector2(800,52);var go=new GameObject("Size",typeof(RectTransform));go.transform.SetParent(panel.transform,false);var label=go.AddComponent<TMPro.TextMeshProUGUI>();label.font=((TMPro.TMP_Text)typeof(ActivityPromptController).GetField("actionLabel",F).GetValue(Object.FindAnyObjectByType<ActivityPromptController>())).font;label.fontSize=24;label.alignment=TMPro.TextAlignmentOptions.Center;label.color=new Color(.08f,.18f,.25f);label.text=new[]{"SMALL","MEDIUM","LARGE"}[sizeIndex]+" - "+breed+" - "+(geometry.Height*100).ToString("F1")+" cm - QA";var rt=go.GetComponent<RectTransform>();rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;}
            int completions=0;float yaw=0,reaction=0;Quaternion previous=cat.transform.rotation,rest=toy.MovingPart.localRotation;
            Vector3 root=cat.transform.position;
            var bones=cat.GetComponentsInChildren<Transform>().Where(b=>b.name.StartsWith("DEF-upper_arm.")||b.name.StartsWith("DEF-forearm.")||b.name.StartsWith("DEF-hand.")).ToArray();
            var lengths=bones.Select(b=>b.localPosition).ToArray();
            Action<CatActivity> done=a=>{if(a==toy)completions++;};CatActivity.Completed+=done;
            try
            {
                for(int i=0;i<216;i++)
                {
                    if(i==8){Call(review,"Tap",Call(review,"Select",toy));Assert.That(toy.IsRunning,Is.True);}
                    yield return new WaitForEndOfFrame();
                    yaw+=Quaternion.Angle(previous,cat.transform.rotation);previous=cat.transform.rotation;
                    reaction=Mathf.Max(reaction,Quaternion.Angle(rest,toy.MovingPart.localRotation));
                    Assert.That(Vector3.Distance(root,cat.transform.position),Is.LessThan(.002f),"No scripted root sliding");
                    if(toy.IsPerformingGesture){float nativeReach=Vector3.Distance(geometry.Upper.position,geometry.Fore.position)+Vector3.Distance(geometry.Fore.position,geometry.Hand.position);Assert.That(Mathf.Abs(nativeReach-geometry.Reach),Is.LessThan(geometry.Reach*.015f),"Preserve measured limb lengths");}
                    if(i%8==0&&toy.IsRunning){skin.BakeMesh(mesh,true);var verts=mesh.vertices;foreach(int v in geometry.HeadVertices){var point=skin.transform.TransformPoint(verts[v]);if(targetSet.TryClosest(point,out var surface)){float d=Vector3.Distance(point,surface.Point);minHead=Mathf.Min(minHead,d);if(CatMeshContactSurface.TryWorldNormal(surface.Mesh,surface.Filter.transform,surface.Triangle,out var normal))minSigned=Mathf.Min(minSigned,Vector3.Dot(point-surface.Point,normal));}}}
                    if(video&&i<96){var texture=ScreenCapture.CaptureScreenshotAsTexture();try{Assert.That(encoder.AddFrame(texture),Is.True);if(i%8==0){Directory.CreateDirectory(Path.Combine(Output,"frames"));File.WriteAllBytes(Path.Combine(Output,"frames",breed+"-"+i.ToString("D3")+".png"),texture.EncodeToPNG());}}finally{Object.Destroy(texture);}}
                }
            }
            finally{CatActivity.Completed-=done;Object.Destroy(mesh);if(caption!=null)Object.Destroy(caption);}
            File.AppendAllText(Path.Combine(Output,"results.txt"),breed+" contacts="+toy.ContactCount+" min="+toy.ClosestPawDistance+" turn="+yaw+" reaction="+reaction+" headMin="+minHead+" headSigned="+minSigned+" completed="+completions+"\n");
            Assert.That(toy.ContactCount,Is.EqualTo(2));Assert.That(toy.ClosestPawDistance,Is.LessThan(geometry.Paw*.08f));
            Assert.That(yaw,Is.GreaterThan(10));Assert.That(reaction,Is.GreaterThan(1));
            Assert.That(minSigned,Is.GreaterThan(-geometry.Height*.005f),"Head skin penetrates rendered toy");
            Assert.That(completions,Is.EqualTo(1));Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
            sizeIndex++;
        }
        encoder?.Dispose();encoder=null;
    }
}
#endif
