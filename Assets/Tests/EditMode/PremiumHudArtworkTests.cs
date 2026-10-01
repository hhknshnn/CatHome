using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class PremiumHudArtworkTests
{
    GameObject root;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [TearDown] public void Cleanup() { if(root!=null) Object.DestroyImmediate(root); }

    [TestCase("nav-normal",248,60)]
    [TestCase("nav-selected",248,60)]
    [TestCase("nav-frame",1088,76)]
    [TestCase("nav-normal",80,16)]
    [TestCase("action-coral",280,94)]
    [TestCase("action-coral",124,42)]
    public void ArtworkKeepsCornersOrderedAndInteractionAlpha(string key,float width,float height)
    {
        root=new GameObject("Artwork fixture",typeof(RectTransform),typeof(LowPolyPanelGraphic));
        var graphic=root.GetComponent<LowPolyPanelGraphic>();
        graphic.rectTransform.sizeDelta=new Vector2(width,height);
        graphic.color=new Color(.1f,.2f,.3f,.4f);
        var sprite=Resources.Load<Sprite>("PremiumHudFinal/"+key);
        Assert.That(sprite,Is.Not.Null);
        graphic.ConfigureHudArtwork(sprite);
        Assert.That(graphic.mainTexture,Is.SameAs(sprite.texture));
        var normal=Mesh(graphic);
        try
        {
            Assert.That(normal.vertexCount,Is.EqualTo(16));
            Assert.That(normal.triangles.Length,Is.EqualTo(54));
            var vertices=normal.vertices;
            Assert.That(vertices.Min(v=>v.x),Is.EqualTo(-width/2).Within(.01f));
            Assert.That(vertices.Max(v=>v.y),Is.EqualTo(height/2).Within(.01f));
            for(int y=0;y<4;y++) for(int x=1;x<4;x++)
                Assert.That(vertices[y*4+x].x,Is.GreaterThanOrEqualTo(vertices[y*4+x-1].x));
            for(int y=1;y<4;y++) Assert.That(vertices[y*4].y,Is.GreaterThanOrEqualTo(vertices[(y-1)*4].y));
            Assert.That(normal.colors32.All(c=>c.a==102),Is.True,"Canvas alpha must survive artwork rendering.");
            Assert.That(normal.uv.All(uv=>uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1),Is.True);
            graphic.SetInteractionState(true,false,false);
            var pressed=Mesh(graphic);
            try { Assert.That(pressed.vertices,Is.EqualTo(normal.vertices));Assert.That(pressed.colors32[0].r,Is.LessThan(normal.colors32[0].r)); }
            finally { Object.DestroyImmediate(pressed); }
            graphic.ConfigureHudArtwork(null);
            Assert.That(graphic.mainTexture,Is.Not.SameAs(sprite.texture),"Non-HUD surfaces retain the procedural renderer.");
        }
        finally { Object.DestroyImmediate(normal); }
    }

    [Test]
    public void NavyBackingStaysOpaqueAcrossWholeRectAndKeepsInputPassive()
    {
        root=new GameObject("Backing fixture",typeof(RectTransform),typeof(Image),typeof(PremiumHudNavySurface));
        var image=root.GetComponent<Image>();image.raycastTarget=false;
        image.rectTransform.sizeDelta=new Vector2(1920,80);
        var effect=root.GetComponent<PremiumHudNavySurface>();
        using(var helper=new VertexHelper())
        {
            effect.ModifyMesh(helper);var mesh=new Mesh();helper.FillMesh(mesh);
            try
            {
                Assert.That(mesh.colors32.All(c=>c.a==255),Is.True,"The reserved camera area must be repainted opaquely.");
                Assert.That(mesh.vertices.Min(v=>v.x),Is.EqualTo(-960));
                Assert.That(mesh.vertices.Max(v=>v.x),Is.EqualTo(960));
                Assert.That(mesh.vertices.Min(v=>v.y),Is.EqualTo(-40));
                Assert.That(mesh.vertices.Max(v=>v.y),Is.EqualTo(40));
                Assert.That(mesh.colors32.Select(c=>c.b).Distinct().Count(),Is.GreaterThan(8));
                Assert.That(image.raycastTarget,Is.False);
                effect.enabled=false;image.enabled=false;
                Assert.That(effect.IsActive(),Is.False,"Mini-game visibility stays with the original Image owner.");
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }

    [Test]
    public void RefreshPreservesButtonTargetBindingBoundsAndUnrelatedSurface()
    {
        root=new GameObject("HUD fixture",typeof(RectTransform));
        var button=Button("ShopButton");
        var unrelated=Button("ActionButton");
        var target=button.targetGraphic;
        var bounds=(RectTransform)button.transform;
        bounds.anchoredPosition=new Vector2(27,19);bounds.sizeDelta=new Vector2(248,60);
        var icon=new GameObject("StorybookDockIcon",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
        icon.transform.SetParent(target.transform,false);icon.raycastTarget=false;
        int clicks=0;button.onClick.AddListener(()=>clicks++);
        var presentation=root.AddComponent<StorybookHudBottomPresentation>();
        presentation.RefreshPremiumArtwork();
        Assert.That(button.targetGraphic,Is.SameAs(target));
        Assert.That(bounds.anchoredPosition,Is.EqualTo(new Vector2(27,19)));
        Assert.That(bounds.sizeDelta,Is.EqualTo(new Vector2(248,60)));
        Assert.That(icon.texture,Is.SameAs(Resources.Load<Texture2D>("PremiumHudFinal/shop")));
        Assert.That(icon.uvRect,Is.EqualTo(new Rect(0,0,1,1)));
        Assert.That(icon.raycastTarget,Is.False);
        Assert.That(unrelated.targetGraphic.mainTexture,Is.Not.SameAs(target.mainTexture));
        int dirty=0;target.RegisterDirtyVerticesCallback(()=>dirty++);
        for(int i=0;i<10;i++)presentation.RefreshPremiumArtwork();
        Assert.That(dirty,Is.Zero,"Repeated presentation refresh must not rebuild unchanged artwork.");
        button.onClick.Invoke();Assert.That(clicks,Is.EqualTo(1));
    }

    Button Button(string name)
    {
        var node=new GameObject(name,typeof(RectTransform),typeof(LowPolyPanelGraphic),typeof(Button));
        node.transform.SetParent(root.transform,false);
        var button=node.GetComponent<Button>();button.targetGraphic=node.GetComponent<LowPolyPanelGraphic>();
        return button;
    }
    static Mesh Mesh(LowPolyPanelGraphic graphic)
    {
        using(var helper=new VertexHelper())
        {
            typeof(LowPolyPanelGraphic).GetMethod("OnPopulateMesh",Private,null,new[]{typeof(VertexHelper)},null).Invoke(graphic,new object[]{helper});
            var mesh=new Mesh();helper.FillMesh(mesh);return mesh;
        }
    }
}
