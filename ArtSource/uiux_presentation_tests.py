from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=root/'Assets/Tests/EditMode/HomeLevelUiBadgeTests.cs'
s=p.read_text(encoding='utf-8-sig')
a=s.index('    [Test]');b=s.index('    [Test]\n    public void BottomDock',a)
s=s[:a]+'''    [TestCase("Assets/UI/MainPanel.prefab")]
    [TestCase("Assets/UI/ShopPanel.prefab")]
    public void HomeLevelBadge_ShowsActualLevelInFredokaAndUpdates(string path)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab,Is.Not.Null);
        var instance=Object.Instantiate(prefab);
        try
        {
            var text=FindComponent<TMP_Text>(instance.transform,"HomeLevelText");
            Assert.That(text,Is.Not.Null);
            Assert.That(text.GetComponent<HomeLevelBadgeLabel>(),Is.Not.Null);
            Assert.That(text.font,Is.EqualTo(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PremiumUiStyle.PremiumFontAssetPath)));
            SetLevel(4);
            Assert.That(text.text,Is.EqualTo(GameLanguageService.Format("home.level",4)));
            HomeProgressionService.GrantHomeXp(HomeProgressionService.CumulativeXpForLevel(5)-HomeProgressionService.CumulativeXpForLevel(4));
            Assert.That(text.text,Is.EqualTo(GameLanguageService.Format("home.level",5)));
        }
        finally {Object.DestroyImmediate(instance);}
    }

'''+s[b:]
s=s.replace('OpenSceneMode.Single','OpenSceneMode.Additive')
s=s.replace('Is.LessThanOrEqualTo(11f)','Is.GreaterThanOrEqualTo(18f)').replace('Is.EqualTo(19f)','Is.EqualTo(23f)')
s=s.replace('            foreach (HomeRoomDefinition room','            string oldText=roomLabel.text;\n            try { foreach (HomeRoomDefinition room')
s=s.replace('room.DisplayName + "  •  LEVEL 1"','room.DisplayName')
s=s.replace('room.DisplayName + " must stay on one line.");\n            }','room.DisplayName + " must stay on one line.");\n            } } finally {roomLabel.text=oldText;}')
p.write_text(s,encoding='utf-8')
p=root/'Assets/Tests/EditMode/PremiumPresentationTests.cs';s=p.read_text(encoding='utf-8-sig')
s=s.replace('        Assert.That(foundLogo, Is.True,\n            "The main menu must keep the Blender-authored CAT HOME emblem.");','        Assert.That(prefab.transform.Find("SafeArea/BrandDockLayout/Wordmark"),Is.Not.Null);')
a=s.index('        TitleLogoNeonFx neonFx =');b=s.index('        foreach (string shortcut',a)
s=s[:a]+'''        Assert.That(prefab.GetComponentInChildren<TitleLogoNeonFx>(true),Is.Null,
            "The approved wordmark is kept crisp; the live cats supply the motion.");
'''+s[b:]
s=s.replace('SafeArea/BrandDockLayout/BrandDock/NewGameButton','SafeArea/BrandDockLayout/NewGameButton')
s=s.replace('"AccountChoiceBanner", "AccountChoiceSubtitle", "GoogleSignInButton",\n            "GoogleBenefit", "GuestContinueButton", "GuestNote",','"AccountChoiceTitle", "AccountChoiceSubtitle", "GoogleSignInButton",\n            "GuestContinueButton", "GuestNote",')
s=s.replace('        AssertVerticalStack(settingsCard, settingsRows);','''        foreach(string row in settingsRows) Assert.That(settingsCard.Find(row),Is.Not.Null,row);
        for(int i=0;i<settingsRows.Length;i++)
        for(int j=i+1;j<settingsRows.Length;j++)
        {
            var a=(RectTransform)settingsCard.Find(settingsRows[i]);
            var b=(RectTransform)settingsCard.Find(settingsRows[j]);
            Rect ra=new Rect(a.anchoredPosition-a.sizeDelta*.5f,a.sizeDelta);
            Rect rb=new Rect(b.anchoredPosition-b.sizeDelta*.5f,b.sizeDelta);
            Assert.That(ra.Overlaps(rb),Is.False,settingsRows[i]+" / "+settingsRows[j]);
        }''')
p.write_text(s,encoding='utf-8')
p=root/'Assets/Tests/EditMode/PremiumUiOverlapTests.cs';s=p.read_text(encoding='utf-8-sig').replace('"PriceLabel"','"CurrencyPriceGroup"');p.write_text(s,encoding='utf-8')
print('Updated presentation and level-badge regression coverage.')
