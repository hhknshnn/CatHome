#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
public sealed class MouseToyNaturalTests {
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
CareAlignmentPolishTests home;ActionReadyReviewTests review;MediaEncoder encoder;
static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
void Place(CatMovement cat,Vector3 p,Quaternion q){var cc=cat.GetComponent<CharacterController>();bool enabled=cc.enabled;cc.enabled=false;cat.transform.SetPositionAndRotation(p,q);cc.enabled=enabled;Physics.SyncTransforms();}
string Output=>SessionState.GetString("CatHome.QA.ResultDirectory","Temp");
[SetUp] public void Before(){home=new CareAlignmentPolishTests();home.Before();review=new ActionReadyReviewTests();typeof(ActionReadyReviewTests).GetField("home",F).SetValue(review,home);}
[TearDown] public void After(){encoder?.Dispose();encoder=null;home.After();}
[UnityTest,Timeout(180000)] public IEnumerator ThreeSizesTwoHunts(){yield return Review(new[]{"persian","domestic-shorthair","maine-coon"});}
[UnityTest,Timeout(180000)] public IEnumerator FinalSmallMediumVideo(){yield return Review(new[]{"persian","domestic-shorthair"});}
IEnumerator Review(string[] breeds){
yield return (IEnumerator)Call(home,"Home");var failures=new List<string>();int index=0;
foreach(var breed in breeds){
yield return (IEnumerator)Call(home,"Breed",breed);var cat=Object.FindAnyObjectByType<CatMovement>();yield return QaBreedReadiness.WaitForSelected(cat,breed);
HomeStoreService.TrySetStored(HomeStoreService.ToyMouseId,false);yield return null;yield return null;
var toy=CatActivity.Registered.OfType<CatEnrichmentActivity>().Single(a=>a.Mode==CatEnrichmentMode.Chase);
var g=CatSpringGeometry.Measure(cat);var surfaces=new CatMeshContactSurface.TargetSet(toy.MovingPart);var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();
for(int variation=0;variation<2;variation++){
Call(review,"ReadyNeeds");yield return RoomPlayModeSupport.WaitForMovementRelease(cat);bool ready=false;
for(int n=0;n<24&&!ready;n++){var side=Quaternion.Euler(0,n*15,0)*Vector3.forward;var p=toy.MovingPart.position+side*(g.MuzzleLocal*g.Scale+g.Reach);p.y=toy.RoutineEntryPoint.position.y;for(int k=0;k<5;k++){Place(cat,p,Quaternion.LookRotation(-side));toy.TryGetStartPose(cat,out var pose);p=pose.ZoneCentre;}Place(cat,p,Quaternion.LookRotation(-side));yield return null;ready=toy.CanStartFromPrompt(cat,out _);}
if(!ready){failures.Add(breed+" stance not found");break;}
toy.TryGetStartPose(cat,out var admitted);cat.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(admitted.ActionTarget-cat.transform.position,Vector3.up))*Quaternion.Euler(0,22,0);Physics.SyncTransforms();yield return null;
var camera=Camera.main;foreach(var c in camera.GetComponents<MonoBehaviour>())c.enabled=false;var focus=(cat.transform.position+toy.MovingPart.position)*.5f+Vector3.up*g.Height*.28f;camera.transform.position=focus+cat.transform.right*(g.Height*3.7f)*(cat.transform.InverseTransformPoint(admitted.ActionTarget).x<=0?-1:1)+cat.transform.forward*g.Height*.25f+Vector3.up*g.Height*1.65f;camera.transform.LookAt(focus);camera.fieldOfView=38;
Time.captureFramerate=24;bool video=variation==0;
if(video&&encoder==null)encoder=new MediaEncoder(Path.Combine(Output,"CatHome_Fare_Oyuncagi.mp4"),new VideoTrackAttributes{frameRate=new MediaRational(24),width=(uint)Screen.width,height=(uint)Screen.height,includeAlpha=false,bitRateMode=VideoBitrateMode.High});
GameObject caption=null;if(video){caption=new GameObject("Mouse review caption",typeof(Canvas));var cv=caption.GetComponent<Canvas>();cv.renderMode=RenderMode.ScreenSpaceOverlay;cv.sortingOrder=30000;var panel=new GameObject("Label",typeof(RectTransform),typeof(UnityEngine.UI.Image));panel.transform.SetParent(caption.transform,false);panel.GetComponent<UnityEngine.UI.Image>().color=new Color(1,1,.96f,.92f);var r=panel.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-145);r.sizeDelta=new Vector2(850,50);var go=new GameObject("Text",typeof(RectTransform));go.transform.SetParent(panel.transform,false);var label=go.AddComponent<TMPro.TextMeshProUGUI>();label.font=((TMPro.TMP_Text)typeof(ActivityPromptController).GetField("actionLabel",F).GetValue(Object.FindAnyObjectByType<ActivityPromptController>())).font;label.fontSize=24;label.alignment=TMPro.TextAlignmentOptions.Center;label.color=new Color(.08f,.18f,.25f);label.text=new[]{"SMALL","MEDIUM","LARGE"}[index]+" - "+breed+" - QA";var rt=go.GetComponent<RectTransform>();rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;}
Vector3 start=cat.transform.position,mouse=toy.MovingPart.position,gesture=Vector3.zero;Quaternion prev=cat.transform.rotation;float yaw=0,maxYaw=0,rootDrift=0,slide=0,gaze=0,minHead=float.PositiveInfinity,minSigned=float.PositiveInfinity,support=0;int complete=0;bool planted=false;Action<CatActivity> done=a=>{if(a==toy)complete++;};CatActivity.Completed+=done;
for(int i=0;i<156;i++){
if(i==5){Call(review,"Tap",Call(review,"Select",toy));if(!toy.IsRunning)failures.Add(breed+" failed start");}
yield return new WaitForEndOfFrame();float angle=Quaternion.Angle(prev,cat.transform.rotation);yaw+=angle;maxYaw=Mathf.Max(maxYaw,angle);prev=cat.transform.rotation;slide=Mathf.Max(slide,Vector3.Distance(mouse,toy.MovingPart.position));var look=cat.GetComponent<CatFurnitureGaze>();if(look!=null)gaze=Mathf.Max(gaze,look.Deflection);
if(toy.IsPerformingGesture){if(!planted){planted=true;gesture=cat.transform.position;}rootDrift=Mathf.Max(rootDrift,Vector3.Distance(gesture,cat.transform.position));var motion=cat.GetComponent<CatMouseToyMotion>();if(motion!=null)support=Mathf.Max(support,motion.SupportDrift);if(i%8==0){skin.BakeMesh(mesh,true);var vertices=mesh.vertices;foreach(int v in g.HeadVertices){var point=skin.transform.TransformPoint(vertices[v]);if(surfaces.TryClosest(point,out var hit)){minHead=Mathf.Min(minHead,Vector3.Distance(point,hit.Point));if(CatMeshContactSurface.TryWorldNormal(hit.Mesh,hit.Filter.transform,hit.Triangle,out var normal))minSigned=Mathf.Min(minSigned,Vector3.Dot(point-hit.Point,normal));}}}}
if(video&&i<96){var texture=ScreenCapture.CaptureScreenshotAsTexture();try{encoder.AddFrame(texture);if(i%8==0){Directory.CreateDirectory(Path.Combine(Output,"frames"));File.WriteAllBytes(Path.Combine(Output,"frames",breed+"-"+i.ToString("D3")+".png"),texture.EncodeToPNG());}}finally{Object.Destroy(texture);}}
}
CatActivity.Completed-=done;if(caption!=null)Object.Destroy(caption);
float travel=Vector3.Distance(start,cat.transform.position);string row=breed+" variant="+variation+" height="+g.Height+" reach="+g.Reach+" contacts="+toy.ContactCount+" minPaw="+toy.ClosestPawDistance+" travel="+travel+" slide="+slide+" turn="+yaw+" maxFrameYaw="+maxYaw+" supportDrift="+support+" strikeRootDrift="+rootDrift+" headMin="+minHead+" headSigned="+minSigned+" gaze="+gaze+" completed="+complete;
File.AppendAllText(Path.Combine(Output,"results.txt"),row+"\n");
if(toy.ContactCount!=1||toy.ClosestPawDistance>g.Paw*.10f)failures.Add(breed+" "+variation+" contact "+toy.ClosestPawDistance);
if(travel<g.Reach*.35f||slide<g.Reach*.15f)failures.Add(breed+" "+variation+" approach/reaction");
if(rootDrift>.002f||support>g.Reach*.06f)failures.Add(breed+" "+variation+" sliding "+support);
if(yaw<10||maxYaw>20||gaze<5)failures.Add(breed+" "+variation+" turning/gaze");
if(minHead<g.Paw*.10f||minSigned<-g.Height*.01f)failures.Add(breed+" "+variation+" head penetration");
if(complete!=1||cat.IsMovementPhysicallyLocked)failures.Add(breed+" "+variation+" completion");
}
Object.Destroy(mesh);index++;
}
encoder?.Dispose();encoder=null;File.WriteAllLines(Path.Combine(Output,"failures.txt"),failures);Assert.That(failures,Is.Empty,string.Join("\n",failures));
}
}
#endif
