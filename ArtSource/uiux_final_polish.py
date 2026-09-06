from pathlib import Path
root=Path(__file__).resolve().parents[1]
def edit(path,old,new):
 p=root/path;s=p.read_text(encoding='utf-8-sig');assert old in s,(path,old[:70]);p.write_text(s.replace(old,new),encoding='utf-8')
edit('Assets/Editor/QuestPanelBuilder.cs','chapterText.color=Color.white;','chapterText.color=Color.white; chapterText.fontSize=23; chapterText.textWrappingMode=TextWrappingModes.NoWrap;')
edit('Assets/Editor/QuestPanelBuilder.cs','claimFaceGraphic.color = new Color32(43, 205, 174, 255);','claimFaceGraphic.color = PremiumUiStyle.Coral;')
edit('Assets/Editor/QuestPanelBuilder.cs','ConfigurePanel(claimFaceGraphic, 14f, 5f);','PremiumUiStyle.ConfigureSurface(claimFaceGraphic, PremiumUiStyle.Coral, 18f, 1f);')
edit('Assets/Editor/QuestPanelBuilder.cs','claimFace.transform, "Label", font, 28f, CreamBar','claimFace.transform, "Label", font, 24f, PremiumUiStyle.Ink')
edit('Assets/Editor/QuestPanelBuilder.cs','claimLabel.characterSpacing = 3f;','claimLabel.characterSpacing = 0.6f;')
edit('Assets/Scripts/QuestPanelController.cs','new Color32(84, 42, 53, 255)','PremiumUiStyle.Ink')
edit('Assets/Scripts/QuestPanelController.cs','new Color32(84, 42, 53, 140)','PremiumUiStyle.Muted')
edit('Assets/Scripts/QuestPanelController.cs','new Color32(198, 86, 34, 255)','PremiumUiStyle.Teal')
edit('Assets/Scripts/QuestPanelController.cs','new Color32(72, 132, 78, 255)','PremiumUiStyle.Teal')
edit('Assets/Scripts/Runner/CatRunnerProgressService.cs','? "COINS ✓"\n            : $"COINS {state.dailyCoins}/{DailyCoinTarget}"','? GameContentCopy.Text("Jeton ✓", "Coins ✓")\n            : GameContentCopy.Text($"Jeton {state.dailyCoins}/{DailyCoinTarget}",$"Coins {state.dailyCoins}/{DailyCoinTarget}")')
edit('Assets/Scripts/Runner/CatRunnerProgressService.cs','? "JUMPS ✓"\n            : $"JUMPS {state.dailyJumps}/{DailyJumpTarget}"','? GameContentCopy.Text("Zıplama ✓", "Jumps ✓")\n            : GameContentCopy.Text($"Zıplama {state.dailyJumps}/{DailyJumpTarget}",$"Jumps {state.dailyJumps}/{DailyJumpTarget}")')
edit('Assets/Scripts/Runner/CatRunnerProgressService.cs','? "DISTANCE ✓"\n            : $"DISTANCE {state.dailyDistance}/{DailyDistanceTarget} m"','? GameContentCopy.Text("Mesafe ✓", "Distance ✓")\n            : GameContentCopy.Text($"Mesafe {state.dailyDistance}/{DailyDistanceTarget} m",$"Distance {state.dailyDistance}/{DailyDistanceTarget} m")')
edit('Assets/Scripts/Runner/CatRunnerProgressService.cs','return $"DAILY MISSIONS   •   {coins}   •   {jumps}   •   {distance}";','return GameContentCopy.Text("Günün hedefleri", "Daily goals") + $"\\n{coins}   ·   {jumps}   ·   {distance}";')
for old,new in [('SETTINGS','Settings'),('CREDITS','Credits'),('QUIT','Quit'),('AYARLAR','Ayarlar'),('YAPIMCILAR','Yapımcılar'),('ÇIKIŞ','Çıkış'),('RETURN TO MAIN MENU','Main menu'),('ANA MENÜYE DÖN','Ana menü')]:
 edit('Assets/Scripts/Localization/GameLanguageService.cs','"'+old+'"','"'+new+'"')
edit('Assets/Editor/MainPanelBuilder.cs','if (id == "MAIN MENU")\n        {\n            LocalizedLabel localized = GetOrAdd<LocalizedLabel>(label.gameObject);\n            localized.EditorConfigure(label, "menu.main_menu");\n        }','string key = id == "MAIN MENU" ? "menu.main_menu" : id == "HOME STORE" ? "title.shop" : id == "QUESTS" ? "quests.title" : id == "ROOMS" ? "title.rooms" : id == "SETTINGS" ? "title.settings" : null;\n        if (key != null) GetOrAdd<LocalizedLabel>(label.gameObject).EditorConfigure(label,key);')
print('Final copy and interaction polish written.')
