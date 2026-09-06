from pathlib import Path
r=Path(__file__).resolve().parents[1]
def edit(path,old,new):
 p=r/path;s=p.read_text(encoding='utf-8-sig');assert old in s,(path,old[:80]);p.write_text(s.replace(old,new),encoding='utf-8')
def span(path,start,end,new):
 p=r/path;s=p.read_text(encoding='utf-8-sig');a=s.index(start);b=s.index(end,a);p.write_text(s[:a]+new+s[b:],encoding='utf-8')
p='Assets/Scripts/CatDialogueView.cs'
span(p,'        Panel(panel,"Shadow"','        LowPolyPanelGraphic face=', '')
edit(p,'PremiumUiStyle.Champagne,34f,5f','PremiumUiStyle.Ivory,28f,2f')
edit(p,'PremiumUiStyle.Navy,27f,7f','PremiumUiStyle.Ivory,24f,0f')
edit(p,'PremiumUiStyle.Champagne,38f,5f','PremiumUiStyle.Mint,28f,2f')
edit(p,'PremiumUiStyle.Champagne,24f,4f','PremiumUiStyle.Ivory,24f,0f')
edit(p,'PremiumUiStyle.ChampagneLight,new Vector2(0f,1f)','PremiumUiStyle.Teal,new Vector2(0f,1f)')
edit(p,'PremiumUiStyle.Navy,24f,5f','PremiumUiStyle.Coral,22f,2f')
edit(p,'Color.white,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero); label.text="CONFIRM"','PremiumUiStyle.Ink,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero); label.text=GameContentCopy.Text("Tamam","Confirm")')
edit(p,'continueLabel.text="TAP TO CONTINUE"','continueLabel.text=GameContentCopy.Text("Devam etmek için dokun","Tap to continue")')
edit(p,'?"YOUR CAT":safeName.ToUpperInvariant()','?GameContentCopy.Text("Kedin","Your cat"):safeName')
edit(p,'nameLabel.text="HELLO!"','nameLabel.text=GameContentCopy.Text("Merhaba!","Hello!")')
edit(p,'placeholder.text="ENTER MY NAME…"','placeholder.text=GameContentCopy.Text("Bana bir isim ver…","Give me a name…")')
edit(p,'g.ConfigureTutorialStyle(color,cut,bevel)','PremiumUiStyle.ConfigureAccentSurface(g,color,color,cut,2f)')
p='Assets/Scripts/PetTutorialHint.cs'
edit(p,'CreatePanel(cardRoot,"HardDepthShadow",cardSize+new Vector2(12f,12f),Vector2.zero,PremiumUiStyle.Shadow,28f,7f);','')
edit(p,'CreatePanel(cardRoot,"ChampagneFrame",cardSize,Vector2.zero,PremiumUiStyle.Champagne,28f,9f);','CreatePanel(cardRoot,"IvoryFrame",cardSize,Vector2.zero,PremiumUiStyle.Ivory,24f,2f);')
edit(p,'PremiumUiStyle.Navy,23f,6f','PremiumUiStyle.Ivory,20f,0f')
edit(p,'instructionLabel.color=PremiumUiStyle.Ivory','instructionLabel.color=PremiumUiStyle.Ink')
edit(p,'skipLabel.color=PremiumUiStyle.Ivory','skipLabel.color=PremiumUiStyle.Ink')
edit(p,'skipLabel.text="SKIP TOUR"','skipLabel.text=GameContentCopy.Text("Turu atla","Skip tour")')
edit(p,'CreatePanel(spotlightCopy,"CaptionShadow",new Vector2(762f,90f),Vector2.zero,PremiumUiStyle.Shadow,26f,5f);','')
edit(p,'PremiumUiStyle.Navy,26f,5f','PremiumUiStyle.Ivory,24f,2f')
edit(p,'spotlightTitle.color=PremiumUiStyle.Ivory','spotlightTitle.color=PremiumUiStyle.Ink')
edit(p,'spotlightTitle.outlineWidth=.10f','spotlightTitle.outlineWidth=0f')
edit(p,'spotlightSubtitle.color=PremiumUiStyle.ChampagneLight','spotlightSubtitle.color=PremiumUiStyle.Muted')
edit(p,'p.ConfigureTutorialStyle(color,cut,bevel)','PremiumUiStyle.ConfigureAccentSurface(p,color,color,cut,2f)')
edit(p,'t.color=new Color32(255,235,190,255)','t.color=PremiumUiStyle.Ink')
p='Assets/Scripts/Home/DailyRetentionService.cs'
edit(p,'            TitleFor(type),\n            "Daily: " + TitleFor(type).ToLowerInvariant(),','            GameQuestCopy.Title(type,TitleFor(type)),\n            GameQuestCopy.Description(type,quest.requiredCount,"Daily: " + TitleFor(type).ToLowerInvariant()),')
p='Assets/Scripts/WhileYouWereAwayPopup.cs'
edit(p,'        if (days > 0)\n            return hours > 0', '''        if(GameLanguageService.Current==GameLanguage.Turkish)
        {
            if(days>0)return hours>0?$"{days} gün {hours} saat":$"{days} gün";
            if(hours>0)return minutes>0?$"{hours} saat {minutes} dakika":$"{hours} saat";
            return $"{minutes} dakika";
        }
        if (days > 0)
            return hours > 0''')
p='Assets/Scripts/ShopPanelController.cs'
edit(p,'return "BOOKSHELF"','return GameContentCopy.Text("Kitaplık","Bookshelf")')
edit(p,'return "TV UNIT"','return GameContentCopy.Text("TV ünitesi","TV unit")')
edit(p,'return "REQUIRED ITEM"','return GameContentCopy.Text("Gerekli eşya","Required item")')
edit(p,'card.actionText.text = text','card.actionText.text = GameStatusCopy.Text(text)')
# Progress summary uses the current collection rather than internal set names.
span(p,'    private static string BuildRoomEconomyFeedback()', '    private static string BuildNextRoomGoal(', '''    private static string BuildRoomEconomyFeedback()
    {
        string id=HomeRoomService.CurrentRoomId;
        int owned=HomeStoreService.GetRoomOwnedCount(id),total=HomeStoreService.GetRoomCollection(id).Count;
        return GameContentCopy.Text($"Koleksiyonun {owned}/{total} · Eşyalar tasarlanan yerine eklenir.",$"Your collection {owned}/{total} · Items go into their designed places.");
    }

''')
print('Updated onboarding, quests, store and duration copy.')
