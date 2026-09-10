using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ReferenceFramingTests
{
    [TestCase(1.42f)]
    [TestCase(1.92f)]
    [TestCase(2.59f)]
    public void LandscapeFraming_RetainsTheApprovedHorizontalRoom(float aspect)
    {
        float fov=HomeWorldViewport.FitFieldOfView(38,aspect);
        float visibleWidth=Mathf.Tan(fov*Mathf.Deg2Rad*.5f)*aspect;
        Assert.That(visibleWidth,Is.GreaterThanOrEqualTo(Mathf.Tan(19*Mathf.Deg2Rad)*1.92f-.0001f));
        if(aspect>=1.92f)Assert.That(fov,Is.EqualTo(38).Within(.001f));
    }

    [TestCase(typeof(WhileYouWereAwayPopup))]
    [TestCase(typeof(ShopPanelController))]
    [TestCase(typeof(GamesHubPanel))]
    public void PendingCollection_WaitsForAnOpenScreen(Type screen)
    {
        var property=screen.GetProperty("IsAnyOpen",BindingFlags.Static|BindingFlags.Public);
        var setter=property.GetSetMethod(true);object original=property.GetValue(null);
        Assert.That(CollectionCompleteCelebrationView.CanPresent,Is.True,"Unblocked home must accept its pending celebration.");
        GameObject host=null;FieldInfo active=null;object previousActive=null;
        try
        {
            if(setter!=null)setter.Invoke(null,new object[]{true});
            else
            {
                host=new GameObject("QA pending modal");host.SetActive(false);var view=host.AddComponent(screen);
                screen.GetField(screen==typeof(WhileYouWereAwayPopup)?"isOpen":"open",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,true);
                active=screen.GetField("activeInstance",BindingFlags.Static|BindingFlags.NonPublic);
                if(active!=null){previousActive=active.GetValue(null);active.SetValue(null,view);}
            }
            Assert.That(CollectionCompleteCelebrationView.CanPresent,Is.False);
        }
        finally
        {
            if(host!=null)UnityEngine.Object.DestroyImmediate(host);
            if(active!=null)active.SetValue(null,previousActive);
            if(setter!=null)setter.Invoke(null,new[]{original});
        }
    }
}
