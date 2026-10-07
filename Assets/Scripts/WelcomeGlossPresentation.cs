using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Title and return-only artwork. Existing buttons and their input owners remain intact.</summary>
public static class WelcomeGlossPresentation
{
    public static readonly Color Ink = new Color32(27, 66, 70, 255);
    static readonly Color Muted = new Color32(67, 108, 108, 255);
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    public static Sprite Art(string name)
    {
        if (sprites.TryGetValue(name, out var found) && found != null) return found;
        if (name == "titleAction" || name == "titleTile" || name == "roofMark")
        {
            string file = name == "titleAction" ? "FramedAction" : name == "titleTile" ? "FramedShortcut" : "RoofCatMark";
            var source = Resources.Load<Texture2D>("WelcomeVertical/" + file);
            if (source == null) return null;
            float k = source.width / (name == "titleAction" ? 2172f : 1254f);
            Rect crop = name == "titleAction" ? new Rect(16,125,2138,492) :
                name == "titleTile" ? new Rect(78,92,1100,1070) : new Rect(85,222,1160,755);
            crop = new Rect(crop.x*k,crop.y*k,crop.width*k,crop.height*k);
            Vector4 edges = name == "titleAction" ? new Vector4(320,70,320,70)*k :
                name == "titleTile" ? Vector4.one*250*k : Vector4.zero;
            var approved = Sprite.Create(source,crop,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,edges);
            approved.name = "Approved vertical title " + name; sprites[name]=approved; return approved;
        }
        string resource = name == "action" ? "CoralAction" : name == "emblem" ? "HomeEmblem" : "LandscapePanel";
        var texture = Resources.Load<Texture2D>("WelcomePolish/" + resource);
        if (texture == null) return null;
        float panelScale = texture.width / 2172f;
        Rect rect = new Rect(5, 96, 2162, 548);
        rect = new Rect(rect.x * panelScale, rect.y * panelScale, rect.width * panelScale, rect.height * panelScale);
        Vector4 border = new Vector4(180, 120, 180, 120) * panelScale;
        if (name == "action")
        {
            float scale = texture.width / 2172f;
            rect = new Rect(66 * scale, 126 * scale, 2044 * scale, 498 * scale);
            border = new Vector4(250, 85, 250, 85) * scale;
        }
        else if (name == "emblem")
        {
            float scale = texture.width / 1254f;
            rect = new Rect(118 * scale, 145 * scale, 1020 * scale, 958 * scale);
            border = Vector4.zero;
        }
        var sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, border);
        sprite.name = "Welcome Polish " + name;
        sprites[name] = sprite;
        return sprite;
    }

    public static void At(Transform t, Vector2 position, Vector2 size)
    {
        if (!(t is RectTransform r)) return;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = position; r.sizeDelta = size; r.localScale = Vector3.one;
    }
    static void Hide(Transform root, string path)
    { var t = root.Find(path); if (t != null) t.gameObject.SetActive(false); }
    static void Text(Transform root, string path, float size, Vector2 p, Vector2 dimensions, bool body = false, bool centered = false)
    {
        var t = root.Find(path); if (t == null) return;
        At(t, p, dimensions);
        var label = t.GetComponent<TMP_Text>(); if (label == null) return;
        label.font = body ? PremiumTypography.Body : PremiumTypography.Emphasis;
        label.fontSharedMaterial = label.font.material;
        label.fontStyle = FontStyles.Normal; label.fontWeight = FontWeight.Regular;
        label.fontSize = label.fontSizeMax = size; label.fontSizeMin = size * .78f;
        label.enableAutoSizing = true; label.enableVertexGradient = false;
        label.color = body ? Muted : Ink; label.raycastTarget = false;
        label.alignment = centered ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
        label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Ellipsis;
    }
    static void Surface(Transform t, string art)
    {
        if (t == null) return;
        var g = t.GetComponent<LowPolyPanelGraphic>(); if (g == null) return;
        g.enabled = true; g.color = Color.white;
        if (art == "tile" || art == "utility" || art == "portrait")
        {
            g.ConfigureHudArtwork(null);
            bool portrait = art == "portrait";
            g.ConfigureScreenStyle(portrait ? new Color32(229, 247, 237, 255) : new Color32(249, 253, 245, 255),
                portrait ? new Color32(150, 206, 187, 255) : new Color32(225, 239, 224, 255), art == "utility" ? 38 : 24, false, false);
        }
        else g.ConfigureHudArtwork(Art(art));
    }
    static Image Picture(Transform parent, string name, Sprite sprite, Vector2 p, Vector2 size)
    {
        var t = parent.Find(name);
        var image = t == null ? null : t.GetComponent<Image>();
        if (image == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false); image = go.GetComponent<Image>();
        }
        image.sprite = sprite; image.color = Color.white; image.raycastTarget = false;
        At(image.transform, p, size); return image;
    }
    static void Emblem(Transform parent, string name, Vector2 p, float size)
    { Picture(parent, name, Art("emblem"), p, new Vector2(size, size * .94f)); }
    static void Divider(Transform parent, string name, float y)
    { var line=Bar(parent,name,new Color32(204,219,204,255)); At(line.transform,new Vector2(0,y),new Vector2(460,1.5f)); }
    static void Button(Transform t, string art, Vector2 p, Vector2 size, float textSize, bool primary = false)
    {
        if (t == null) return;
        At(t, p, size); var b = t.GetComponent<Button>(); if (b == null) return;
        Surface(b.targetGraphic.transform, art);
        var contrast = t.GetComponent<StorybookActionContrast>();
        if (contrast == null) contrast = t.gameObject.AddComponent<StorybookActionContrast>();
        contrast.Configure(b, primary ? Color.white : Ink, Muted);
        foreach (var l in t.GetComponentsInChildren<TMP_Text>(true))
        { l.enableVertexGradient = false; l.color = primary ? Color.white : Ink; l.fontSizeMax = textSize; l.fontSize = textSize; l.fontSizeMin = textSize * .8f; }
        foreach (var glyph in t.GetComponentsInChildren<StorybookTitleGlyph>(true)) glyph.color = primary ? Color.white : Ink;
    }

    public static void Title(Transform root)
    {
        var safe = root.Find("SafeArea"); var dock = safe?.Find("BrandDockLayout"); if (dock == null) return;
        var previous = safe.GetComponent<TitleScreenLayout>(); if (previous != null) previous.enabled = false;
        var layout = safe.GetComponent<WelcomeGlossTitleLayout>(); if (layout == null) layout = safe.gameObject.AddComponent<WelcomeGlossTitleLayout>();
        var veil = root.Find("LeftPearlVeil"); if (veil != null) veil.gameObject.SetActive(true);
        foreach (string name in new[] { "PawWordmark", "YourHomeJourney", "GlossLogo", "GlossLogoPaw", "PolishMenuSurface", "PolishHomeEmblem", "PolishBrandRule", "PolishNavigationRule" }) Hide(dock, name);
        Picture(dock,"ApprovedRoofMark",Art("roofMark"),new Vector2(-120,395),new Vector2(188,122));
        Text(dock, "Wordmark", 66, new Vector2(-72,305), new Vector2(396,90), false, true);
        dock.Find("Wordmark").GetComponent<TMP_Text>().text = "CAT <color=#3E9688>HOME</color>";
        Text(dock, "CatName", 72, new Vector2(0,165), new Vector2(540,96));
        Text(dock, "Greeting", 40, new Vector2(0,86), new Vector2(540,64), true);
        dock.Find("ExperiencePromise").gameObject.SetActive(true);
        Text(dock,"ExperiencePromise",31,new Vector2(0,25),new Vector2(540,52),true);
        Button(dock.Find("PlayButton"), "titleAction", new Vector2(-8,-98), new Vector2(526,118), 42, true);
        var face = dock.Find("PlayButton/Visual");
        foreach(string name in new[]{"PawMedal","WelcomePaw","GlossPaw"})Hide(face,name);
        var label=face.Find("Label") as RectTransform;
        label.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;label.offsetMin=new Vector2(35,10);label.offsetMax=new Vector2(-62,-10);
        At(face.Find("ContinueArrow"),new Vector2(203,0),new Vector2(36,44));
        Button(dock.Find("NewGameButton"),"tile",new Vector2(-8,-196),new Vector2(278,70),31);
        var secondary=dock.Find("NewGameButton").GetComponent<Button>();
        var secondaryFace=secondary.targetGraphic.GetComponent<LowPolyPanelGraphic>();
        secondaryFace.ConfigureModernStyle(Color.clear,Color.clear,0,false,false);
        secondaryFace.color=Color.clear;
        At(dock.Find("NewGameButton/Visual/RestartIcon"),new Vector2(-105,0),Vector2.one*33);
        var newLabel=dock.Find("NewGameButton/Visual/Label") as RectTransform;
        newLabel.offsetMin=new Vector2(47,3);newLabel.offsetMax=new Vector2(-8,-3);
        At(dock.Find("HomeLevelPill"),new Vector2(-40,-455),new Vector2(460,46));
        var level=dock.Find("HomeLevelPill").GetComponent<LowPolyPanelGraphic>();if(level!=null)level.enabled=false;
        var progress=dock.Find("HomeLevelPill/ProgressIcon");progress.gameObject.SetActive(true);
        At(progress,new Vector2(-210,0),Vector2.one*36);
        var progressGlyph=progress.GetComponent<Graphic>();if(progressGlyph!=null)progressGlyph.color=new Color32(70,208,177,255);
        Text(dock,"HomeLevelPill/Label",26,new Vector2(32,0),new Vector2(380,44),true);
        var links=safe.Find("MainMenuShortcuts");var dockFace=links.GetComponent<LowPolyPanelGraphic>();if(dockFace!=null)dockFace.enabled=false;
        var names=new[]{"ShopShortcut","RoomsShortcut","GamesShortcut"};
        for(int i=0;i<names.Length;i++)
        {
            var t=links.Find(names[i]);Button(t,"titleTile",new Vector2((i-1)*188,0),new Vector2(170,168),28);
            Text(t,"Visual/Label",28,new Vector2(0,-48),new Vector2(156,39),false,true);
            Text(t,"Visual/AfterTourBadge",14,new Vector2(0,55),new Vector2(154,21),true,true);
            Hide(t,"Visual/ShortcutBackdropArt");
        }
        var preview=links.Find("ShopShortcut/Visual/Preview");var oldPreview=preview.GetComponent<RawImage>();if(oldPreview!=null)oldPreview.enabled=false;
        var portrait=Picture(preview,"SelectedBreed",null,Vector2.zero,Vector2.one*75);
        var selected=portrait.GetComponent<SelectedCatPortrait>();if(selected==null)selected=portrait.gameObject.AddComponent<SelectedCatPortrait>();selected.Refresh();
        var utilities=safe.Find("TitleUtilities");
        foreach(Transform t in utilities)
        {
            if(t.GetComponent<Button>()==null)continue;
            Button(t,"utility",Vector2.zero,Vector2.one*70,20);
            Hide(t,"Visual/Label");At(t.Find("Visual/UtilityIcon"),Vector2.zero,Vector2.one*31);
        }
        layout.Apply(true);
    }

    public static void Return(Transform panel)
    {
        if(panel==null)return;
        Hide(panel,"ReturnFace/LivingPearlSurface");Hide(panel,"ReturnBanner");Hide(panel,"ReturnKicker");
        StorybookScreenStyle.RoomShell(panel.Find("ReturnFace")?.GetComponent<LowPolyPanelGraphic>(), 32f, false, true);
        At(panel.Find("ReturnFace"),new Vector2(0,0),new Vector2(1300,690));
        StorybookScreenStyle.Inset(panel.Find("PortraitMedallion")?.GetComponent<LowPolyPanelGraphic>(), StorybookScreenStyle.Photo, 26f);
        At(panel.Find("PortraitMedallion"),new Vector2(-427,50),new Vector2(270,240));
        Hide(panel,"GlossPawSeal");
        Emblem(panel,"PolishHomeSeal",new Vector2(-300,-36),70);
        Text(panel,"Title",48,new Vector2(143,207),new Vector2(740,70),false,true);
        At(panel.Find("AwayDurationBadge"),new Vector2(143,145),new Vector2(740,42));
        Text(panel,"AwayDurationBadge/Duration",27,Vector2.zero,new Vector2(720,42),true,true);
        At(panel.Find("ReturnSummaryBadge"),new Vector2(143,91),new Vector2(740,50));
        var summaryFace=panel.Find("ReturnSummaryBadge/SummaryFace")?.GetComponent<Graphic>();if(summaryFace!=null)summaryFace.enabled=false;
        Hide(panel,"ReturnSummaryBadge/FriendBadge");
        Text(panel,"ReturnSummaryBadge/ReturnSummary",25,Vector2.zero,new Vector2(720,50),true,true);
        string[] names={"HungerRewardCard","ThirstRewardCard","EnergyRewardCard"};
        for(int i=0;i<names.Length;i++)
        {
            var t=panel.Find(names[i]);At(t,new Vector2(143+(i-1)*266,-43),new Vector2(252,144));
            StorybookScreenStyle.Card(t.GetComponent<LowPolyPanelGraphic>(),24f);
            At(t.Find("Icon"),new Vector2(-87,25),Vector2.one*60);
            Text(t,"NeedLabel",25,new Vector2(29,44),new Vector2(148,34),true);
            Text(t,"Value",32,new Vector2(29,6),new Vector2(158,45));
            Hide(t,"NeedAccent");
        }
        Button(panel.Find("WelcomeBackButton"),"action",new Vector2(143,-218),new Vector2(620,108),36,true);
        StorybookScreenStyle.Action(panel.Find("WelcomeBackButton")?.GetComponent<UnityEngine.UI.Button>());
        var fx=panel.GetComponentInParent<WhileYouWereAwayPopupFx>();
        if(fx!=null)fx.ConfigureDesignSize(new Vector2(1360,760));
    }

    public static void Need(Transform panel,string name,float after,Color tint)
    {
        var t=panel.Find(name);if(t==null)return;
        var label=t.Find("Value")?.GetComponent<TMP_Text>();if(label!=null)label.text=Mathf.RoundToInt(after)+"%";
        var track=Bar(t,"GlossNeedTrack",StorybookScreenStyle.Track);At(track.transform,new Vector2(0,-45),new Vector2(230,12));
        var fill=Bar(track.transform,"Fill",tint);
        var r=fill.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=new Vector2(Mathf.Clamp01(after/100f),1);r.offsetMin=r.offsetMax=Vector2.zero;
    }
    static LowPolyPanelGraphic Bar(Transform parent,string name,Color tint)
    {
        var t=parent.Find(name);var g=t==null?null:t.GetComponent<LowPolyPanelGraphic>();
        if(g==null){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(LowPolyPanelGraphic));go.transform.SetParent(parent,false);g=go.GetComponent<LowPolyPanelGraphic>();}
        g.ConfigureGlassStyle(tint,tint,6,false,false);g.raycastTarget=false;return g;
    }
}
