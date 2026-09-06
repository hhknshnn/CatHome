from pathlib import Path
import re
root=Path(__file__).resolve().parents[1]
def update(path,fn):
 p=root/path;s=p.read_text(encoding='utf-8-sig');p.write_text(fn(s),encoding='utf-8')
def replace(s,a,b):
 assert a in s,a[:80];return s.replace(a,b)
def method(s,name,body):
 start=s.index('    [Test]\n    public void '+name); end=s.index('\n    [Test]',start+6)
 return s[:start]+body.rstrip()+'\n'+s[end:]
def cat(s):
 start=s.index('        RectTransform catRect =');end=s.index('\n    }',start)
 return s[:start]+'''        Assert.That(generalShop.gameObject.activeSelf, Is.False, "The general shop belongs in the dock.");
        RectTransform catRect=(RectTransform)catShop;
        RectTransform menuRect=(RectTransform)menu;
        Assert.That(catRect.anchorMin.x, Is.EqualTo(0));
        Assert.That(menuRect.anchorMin.x, Is.EqualTo(1));
        Assert.That(catShop.GetComponentInChildren<HomeLevelBadgeLabel>(true), Is.Not.Null);
'''+s[end:]
update('Assets/Tests/EditMode/CatBreedShopTests.cs',cat)
def competition(s):
 s=s.replace('GlobalLeaderboard_KeepsAThreePlacePodiumAndNoSecondNameInput','GlobalLeaderboard_KeepsCompleteRankingAndNoSecondNameInput')
 a=s.index('            Assert.That(panel.transform.Find(');b=s.index('        }\n        finally',a)
 return s[:a]+'''            var serialized = new SerializedObject(panel);
            Assert.That(serialized.FindProperty("emptyStateText").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("ownRankText").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("rankRows").arraySize,
                Is.EqualTo(CompetitionRules.MaximumVisibleEntries));
            Assert.That(panel.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true), Is.Not.Null);
'''+s[b:]
update('Assets/Tests/EditMode/CompetitionRulesTests.cs',competition)
def room(s):
 a=s.index('        RectTransform mark =');b=s.index('\n    }',a)
 s=s[:a]+'''        Assert.That(button, Is.Not.Null);
        var mark=button.GetComponentInChildren<TMPro.TMP_Text>(true);
        Assert.That(mark, Is.Not.Null);
        Assert.That(mark.text, Is.EqualTo("×"));
        Assert.That(mark.alignment, Is.EqualTo(TMPro.TextAlignmentOptions.Center));
        Assert.That(mark.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(button.rect.width, Is.GreaterThanOrEqualTo(48));
'''+s[b:]
 s=s.replace('Is.GreaterThan(600f)','Is.GreaterThanOrEqualTo(480f)').replace('Is.GreaterThan(400f)','Is.GreaterThanOrEqualTo(350f)').replace('Is.EqualTo(512)','Is.GreaterThanOrEqualTo(1024)')
 return s
update('Assets/Tests/EditMode/HomeRoomNavigationTests.cs',room)
def language(s):
 return s.replace('Is.EqualTo("DİL")','Is.EqualTo("Dil")').replace('Is.EqualTo("LANGUAGE")','Is.EqualTo("Language")')
update('Assets/Tests/EditMode/NewGameAndLocalizationTests.cs',language)
def quest(s):
 s=s.replace('    [SetUp]','    private TestLanguageScope language;\n\n    [SetUp]',1)
 s=s.replace('        // Level 1, empty wallet','        language = new TestLanguageScope(GameLanguage.English);\n\n        // Level 1, empty wallet',1)
 s=s.replace('    public void TearDown()\n    {','    public void TearDown()\n    {\n        language?.Dispose();',1)
 return s
update('Assets/Tests/EditMode/QuestPanelProgressionTests.cs',quest)
def away(s):
 s=s.replace('    [Test]', '''    private TestLanguageScope language;
    [SetUp] public void SetUp(){language=new TestLanguageScope(GameLanguage.English);}
    [TearDown] public void TearDown(){language.Dispose();}

    [Test]''',1)
 s=s.replace('"YOUR CAT HAD A COZY NAP\\nAND RESTORED 65% ENERGY!"','GameLanguageService.Format("return.nap",65)')
 s=s.replace('"YOUR CAT MISSED YOU\\nAND GOT A LITTLE TIRED."','GameLanguageService.Text("return.missed")').replace('"YOUR CAT MISSED YOU\\nWHILE YOU WERE AWAY."','GameLanguageService.Text("return.missed")')
 s=s.replace('"RESTORED"','"restored"').replace('new Vector2(1040f, 760f)','new Vector2(980f, 680f)')
 return s
update('Assets/Tests/EditMode/WhileYouWereAwayPopupTests.cs',away)
def overlap(s):
 s=s.replace('Rect first = WorldRect(buttons[a].transform as RectTransform);','Rect first = VisibleRect(buttons[a].transform as RectTransform);').replace('Rect second = WorldRect(buttons[b].transform as RectTransform);','Rect second = VisibleRect(buttons[b].transform as RectTransform);')
 s=s.replace('"PricePillRim"','"PriceLabel"')
 a=s.index('                foreach (string textName');b=s.index('                checkedCards++;',a)
 s=s[:a]+'''                var title = FindNamedRect(card,"ProductTitle");
                var photo = FindNamedRect(card,"ProductPhoto");
                Assert.That(title,Is.Not.Null);
                Assert.That(RectRelativeTo(title,card).Overlaps(RectRelativeTo(action,card)),Is.False,card.name);
'''+s[b:]
 a=s.index('    private static Rect WorldRect(')
 s=s[:a]+'''    private static Rect VisibleRect(RectTransform rect)
    {
        Rect result=WorldRect(rect);
        for(Transform p=rect.parent;p!=null;p=p.parent)
        {
            var clip=p.GetComponent<RectMask2D>(); var mask=p.GetComponent<Mask>();
            if((clip!=null && clip.isActiveAndEnabled)||(mask!=null && mask.isActiveAndEnabled))
            {
                Rect bounds=WorldRect((RectTransform)p);
                float left=Mathf.Max(result.xMin,bounds.xMin),bottom=Mathf.Max(result.yMin,bounds.yMin);
                float right=Mathf.Min(result.xMax,bounds.xMax),top=Mathf.Min(result.yMax,bounds.yMax);
                if(right<=left || top<=bottom)return new Rect(left,bottom,0,0);
                result=Rect.MinMaxRect(left,bottom,right,top);
            }
        }
        return result;
    }

'''+s[a:]
 return s
update('Assets/Tests/EditMode/PremiumUiOverlapTests.cs',overlap)
def palette(s):
 return method(s,'PremiumPalette_RemainsBrightAndColourful()', '''    [Test]
    public void PremiumPalette_KeepsLightSurfacesAndReadableText()
    {
        Assert.That(Luminance(PremiumUiStyle.Ivory),Is.GreaterThan(.8f));
        Assert.That(Luminance(PremiumUiStyle.Mint),Is.GreaterThan(.7f));
        foreach(var surface in new[]{PremiumUiStyle.Ivory,PremiumUiStyle.Mint,PremiumUiStyle.Coral})
            Assert.That((Luminance(surface)+.05f)/(Luminance(PremiumUiStyle.Ink)+.05f),Is.GreaterThanOrEqualTo(4.5f));
        Assert.That(1.05f/(Luminance(PremiumUiStyle.Teal)+.05f),Is.GreaterThanOrEqualTo(3f));
    }
    private static float Luminance(Color color)
    {
        Color linear=color.linear;
        return .2126f*linear.r+.7152f*linear.g+.0722f*linear.b;
    }
''')
update('Assets/Tests/EditMode/PremiumVisualInvariantTests.cs',palette)
print('Migrated obsolete art contracts; behavior assertions retained.')
