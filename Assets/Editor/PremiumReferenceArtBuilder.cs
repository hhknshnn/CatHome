using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using U=PremiumUiElements;

/// <summary>The approved reference's scale, sculpted art and enamel surfaces.</summary>
public static class PremiumReferenceArtBuilder
{
    public static string BuildSilently()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Finish Play before authoring UI.");
        foreach(string name in new[]{"Shop","Rooms","Games","Food","Water","Energy","Paw"})
        {
            string path="Assets/Resources/PremiumInterface/"+name+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)continue;
            importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=1024;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        }
        string report=PremiumTypographyBuilder.ApplyToAllScreens();
        CatHomeAuthoringWorkspace.OpenFullHomePreview(false);
        PremiumReferenceSceneBuilder.Apply();
        EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();return report;
    }

    public static void PolishScreen(Transform root)
    {
        if(Resources.Load<Texture2D>("PremiumInterface/Shop")==null)return;
        // Snapshot before adding noninteractive artwork. Every operation reuses
        // a named child, so rebuilding a prefab cannot stack extra decorations.
        foreach(var r in root.GetComponentsInChildren<RectTransform>(true))
        {
            switch(r.name)
            {
                case "FoodBar":case "ThirstUI":case "EnergyUI": Need(r);break;
                case "CatShopButton": Identity(r);break;
                case "HomeDock": Dock(r);break;
                case "ActivityProgressBadge":
                    r.anchorMin=r.anchorMax=new Vector2(1,0);r.pivot=new Vector2(.5f,.5f);
                    r.anchoredPosition=new Vector2(-190,204);r.sizeDelta=new Vector2(280,66);break;
                case "JoystickBackground": Joystick(r);break;
                case "CoinEntry":case "DiamondEntry": Currency(r);break;
                case "BondXpEntry": Bond(r);break;
                case "ShopShortcut":Shortcut(r,"Paw");break;
                case "GamesShortcut":Shortcut(r,"Games");break;
                case "RoomsShortcut":Shortcut(r,"Rooms");break;
                case "ShopPanelCanvas": ShopHeading(r);break;
                case "Icon_HOME STORE":MenuIcon(r,"Shop");break;
                case "Icon_ROOMS":MenuIcon(r,"Rooms");break;
                case "Icon_CAT JOURNAL":MenuIcon(r,"Paw");break;
            }
            bool backdrop=r.name=="RoomSelectorScrim"||r.name=="CatShopScrim"||r.name=="SettingsScrim"||r.name=="WarmOverlay"||r.name=="GamesHubPanel"||
                (r.name=="Scrim"&&r.GetComponentInParent<ShopPanelController>()!=null);
            if(backdrop&&r.GetComponent<PremiumModalBackdrop>()==null)r.gameObject.AddComponent<PremiumModalBackdrop>();
        }
        foreach(var b in root.GetComponentsInChildren<Button>(true))
        {
            var surface=b.targetGraphic as LowPolyPanelGraphic;
            if(surface==null)continue;
            var rect=b.transform as RectTransform;
            if(rect.rect.height>180||rect.rect.width<130)continue;
            foreach(var label in b.GetComponentsInChildren<TMP_Text>(true))
            {
                if(label.name!="Label"&&label.name!="ButtonText"&&label.name!="PlayLabel")continue;
                if(surface.color.r>.7f&&surface.color.g<.68f&&surface.color.b<.65f)label.color=new Color32(255,253,242,255);
                if(label.fontSize>=21&&label.fontSize<27){label.fontSize=27;label.enableAutoSizing=true;label.fontSizeMin=22;label.fontSizeMax=27;}
            }
        }
    }

    static RawImage Icon(Transform parent,string name,float x,float y,float size)
    {
        var old=parent.Find("Sculpted"+name) as RectTransform;
        var r=old!=null?old:U.Rect("Sculpted"+name,parent);U.At(r,x,y,size,size);
        var image=r.GetComponent<RawImage>();if(image==null)image=r.gameObject.AddComponent<RawImage>();
        image.texture=Resources.Load<Texture2D>("PremiumInterface/"+name);image.raycastTarget=false;image.color=Color.white;return image;
    }
    static RectTransform Child(Transform root,string name)
    {
        foreach(var r in root.GetComponentsInChildren<RectTransform>(true))if(r.name==name)return r;return null;
    }
    static void Type(TMP_Text label,float size)
    {
        if(label==null)return;label.fontSize=size;label.enableAutoSizing=false;PremiumTypography.Apply(label);
    }
    static void Need(RectTransform root)
    {
        root.sizeDelta=new Vector2(TopHudResponsiveLayout.NeedBarWidth,TopHudResponsiveLayout.TopBarHeight);
        string name=root.name=="FoodBar"?"Food":root.name=="ThirstUI"?"Water":"Energy";
        foreach(Transform child in root)
            if(child.name.EndsWith("Icon")&&!child.name.StartsWith("Sculpted"))child.gameObject.SetActive(false);
        Icon(root,name,-92,0,62);
        var track=Child(root,"FillTrackMask");if(track!=null)U.At(track,31,-18,164,12);
        foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if(label.name.EndsWith("LabelFront")){Type(label,25);U.At(label.rectTransform,0,12,104,36);}
            else if(label.name=="PercentageText"){Type(label,24);U.At(label.rectTransform,83,12,66,36);}
        }
    }
    static void Identity(RectTransform root)
    {
        root.sizeDelta=new Vector2(260,88);root.anchoredPosition=new Vector2(32,-22);
        var face=root.GetComponentInChildren<LowPolyPanelGraphic>(true);if(face==null)return;
        var portrait=Child(root,"SelectedCatFace");
        if(portrait!=null)
        {
            U.At(portrait,-84,0,70,70);
            var disc=Child(face.transform,"PortraitMedallion");
            if(disc==null)disc=U.Panel("PortraitMedallion",face.transform,PremiumUiStyle.Ivory,-84,0,76,76,38).rectTransform;
            U.At(disc,-84,0,76,76);
            disc.SetAsFirstSibling();portrait.SetAsLastSibling();
        }
        foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if(label.name=="CatName"){Type(label,27);U.At(label.rectTransform,39,14,154,34);}
            if(label.name=="HomeLevelText"){Type(label,19);label.color=PremiumUiStyle.Teal;U.At(label.rectTransform,39,-17,154,28);}
        }
    }
    static void Dock(RectTransform root)
    {
        root.sizeDelta=new Vector2(820,64);root.anchoredPosition=new Vector2(0,40);
        var surround=Child(root,"DockEnamelTray");
        if(surround==null)surround=U.Panel("DockEnamelTray",root,PremiumUiStyle.WarmIvory,0,0,936,136,62).rectTransform;
        surround.SetAsFirstSibling();U.At(surround,0,0,1928,80);
        var tray=surround.GetComponent<LowPolyPanelGraphic>();tray.ConfigureTutorialStyle(PremiumUiStyle.WarmIvory,0,1);tray.ConfigureReferenceFinish(false);tray.ConfigureElevation(false);tray.raycastTarget=false;
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {
            var rect=(RectTransform)button.transform;string icon;
            if(button.name=="ShopButton"){U.At(rect,-288,0,220,56);icon="Shop";}
            else if(button.name=="PlayCatRunnerButton"){U.At(rect,288,0,220,56);icon="Games";}
            else if(button.name=="CurrentRoomStatus"){U.At(rect,0,0,316,56);icon="Rooms";}
            else continue;
            var visual=button.targetGraphic.transform;
            // An intentional art well keeps every silhouette clear of the rim.
            // The oversized, muted motif restores the reference's illustrated buttons.
            float artX=icon=="Rooms"?-122:-78;
            var well=Child(visual,"DockArtWell");
            if(well==null)well=U.Panel("DockArtWell",visual,PremiumUiStyle.Mint,artX,0,44,44,20).rectTransform;
            U.At(well,artX,0,44,44);well.SetAsFirstSibling();
            var motif=Child(visual,"BackdropMotif");
            if(motif==null)motif=U.Rect("BackdropMotif",visual);
            U.At(motif,icon=="Rooms"?124:76,0,46,44);
            var wash=motif.GetComponent<RawImage>();if(wash==null)wash=motif.gameObject.AddComponent<RawImage>();
            wash.texture=Resources.Load<Texture2D>("PremiumInterface/"+icon);wash.color=new Color(1,1,1,.19f);wash.raycastTarget=false;
            motif.SetSiblingIndex(1);
            Icon(visual,icon,artX,0,38);
            foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if(label.name=="RunnerEnergyLabel"){Type(label,13);U.At(label.rectTransform,24,-15,238,18);}
                else if(label.name=="RoomProgressLabel")
                {Type(label,22);label.enableAutoSizing=true;label.fontSizeMin=18;label.fontSizeMax=22;U.At(label.rectTransform,24,8,238,28);}
                else{Type(label,22);U.At(label.rectTransform,20,0,142,40);}
            }
        }
        var fit=root.GetComponent<PremiumHomeDockLayout>();if(fit==null)root.gameObject.AddComponent<PremiumHomeDockLayout>();
    }
    static void Joystick(RectTransform root)
    {
        var joystick=root.GetComponent<MobileJoystick>();if(joystick==null)return;
        var baseRect=Child(root,"PremiumBase");
        if(baseRect!=null)
        {
            U.At(baseRect,0,0,188,188);var surface=baseRect.GetComponent<LowPolyPanelGraphic>();
            surface.SetPremiumBaseColor(new Color32(202,220,187,255));
        }
        var handle=Child(root,"PremiumHandle");
        if(handle!=null)
        {
            U.At(handle,0,0,92,92);handle.GetComponent<LowPolyPanelGraphic>().SetPremiumBaseColor(PremiumUiStyle.Ivory);
            Icon(handle,"Paw",0,0,62);
        }
        if(baseRect!=null)
            for(int i=0;i<4;i++)
            {
                string n="Direction"+i;var r=Child(baseRect,n);TMP_Text label;
                if(r==null)label=U.Label(n,baseRect,PremiumTypography.Body,20,PremiumUiStyle.Ivory,0,0,26,26,TextAlignmentOptions.Center);
                else label=r.GetComponent<TMP_Text>();
                float a=i*Mathf.PI*.5f;U.At(label.rectTransform,Mathf.Sin(a)*74,Mathf.Cos(a)*74,26,26);label.text="▲";label.rectTransform.localEulerAngles=new Vector3(0,0,-90*i);
            }
    }
    static void Currency(RectTransform root)
    {
        root.sizeDelta=new Vector2(160,84);
        var layout=root.GetComponent<CurrencyEntryLayout>();if(layout==null)return;
        var so=new SerializedObject(layout);
        so.FindProperty("iconSize").floatValue=40;so.FindProperty("maxFontSize").floatValue=28;
        so.FindProperty("maxValueWidth").floatValue=42;so.FindProperty("plusSize").floatValue=44;
        so.ApplyModifiedPropertiesWithoutUndo();layout.Relayout();
    }
    static void Bond(RectTransform root)
    {
        root.anchorMin=root.anchorMax=new Vector2(0,1);root.pivot=new Vector2(0,1);
        root.anchoredPosition=new Vector2(308,-24);
        Currency(root);
    }
    static void Shortcut(RectTransform root,string name)
    {
        var preview=Child(root,"Preview");if(preview==null)return;
        var image=preview.GetComponent<RawImage>();if(image!=null)image.texture=Resources.Load<Texture2D>("PremiumInterface/"+name);
        U.At(preview,-41,12,106,106);
        var parent=preview.parent;var motif=Child(parent,"ShortcutBackdropArt");
        if(motif==null)motif=U.Rect("ShortcutBackdropArt",parent);
        U.At(motif,57,9,100,100);
        var art=motif.GetComponent<RawImage>();if(art==null)art=motif.gameObject.AddComponent<RawImage>();
        art.texture=Resources.Load<Texture2D>("PremiumInterface/"+name);art.color=new Color(1,1,1,.18f);art.raycastTarget=false;
        motif.SetAsFirstSibling();
    }
    static void MenuIcon(RectTransform root,string name)
    {
        foreach(Transform child in root)if(!child.name.StartsWith("Sculpted"))child.gameObject.SetActive(false);
        Icon(root,name,0,0,38);
    }
    static void ShopHeading(RectTransform root)
    {
        var header=Child(root,"HeaderTitle");if(header==null)header=Child(root,"Title");
        if(header==null)return;
        // Only the shop's own heading; a purchase title has its own hierarchy.
        var label=header.GetComponent<TMP_Text>();if(label==null||header.rect.width<300)return;
        var icon=Icon(header,"Shop",0,0,90);icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=new Vector2(0,.5f);
        icon.rectTransform.anchoredPosition=new Vector2(44,0);
        label.margin=new Vector4(108,0,0,0);
    }
}
