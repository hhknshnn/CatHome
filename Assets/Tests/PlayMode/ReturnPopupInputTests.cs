#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class ReturnPopupInputTests
{
    GameObject root, eventRoot;
    [UnityTest]
    public IEnumerator VisibleContinueButton_ReceivesPointerAndClosesReturnScreen()
    {
        root=new GameObject("Return popup input test",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10000;
        var es=EventSystem.current;
        if(es==null){eventRoot=new GameObject("Test events",typeof(EventSystem));es=eventRoot.GetComponent<EventSystem>();}
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/WhileYouWereAwayPopup.prefab");
        var instance=Object.Instantiate(prefab,root.transform);
        var popup=instance.GetComponent<WhileYouWereAwayPopup>();
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(WhileYouWereAwayPopup).GetMethod("Show",flags).Invoke(popup,null);
        yield return new WaitForSecondsRealtime(.65f);
        Canvas.ForceUpdateCanvases();
        var button=(Button)typeof(WhileYouWereAwayPopup).GetField("welcomeBackButton",flags).GetValue(popup);
        var pointer=new PointerEventData(es){position=RectTransformUtility.WorldToScreenPoint(null,button.transform.position),button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>();es.RaycastAll(pointer,hits);
        Assert.That(hits,Is.Not.Empty,"The visible button needs a real raycast surface.");
        Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button),"A backdrop or decoration must not steal the continue tap.");
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
        yield return new WaitForSecondsRealtime(.4f);
        Assert.That(popup.IsOpen,Is.False);
        var group=instance.GetComponent<CanvasGroup>();
        Assert.That(group.blocksRaycasts,Is.False,"The closed return overlay must release input.");
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(root!=null)Object.Destroy(root);if(eventRoot!=null)Object.Destroy(eventRoot);yield return null;}
}
#endif
