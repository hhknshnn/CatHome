from pathlib import Path
r=Path(__file__).resolve().parents[1]
def edit(path,old,new):
 p=r/path;s=p.read_text(encoding='utf-8-sig');assert old in s,(path,old[:90]);p.write_text(s.replace(old,new),encoding='utf-8')
edit('Assets/Editor/CatRunnerContentBuilder.cs','PremiumUiFactory.PolishHierarchy(canvasObject.transform, premiumFont);\n        results.SetActive(false);','PremiumUiFactory.PolishHierarchy(canvasObject.transform, premiumFont);\n        PremiumMiniGameUiBuilder.PolishHud(safeArea);\n        results.SetActive(false);')
edit('Assets/Editor/CatCatchContentBuilder.cs','PremiumUiFactory.PolishHierarchy(canvasObject.transform, font);\n        hud.SetActive(false);','PremiumUiFactory.PolishHierarchy(canvasObject.transform, font);\n        PremiumMiniGameUiBuilder.PolishHud(hud.transform);\n        PremiumUiElements.Localize(hint,"catch.hint");\n        PremiumUiElements.Localize(score.transform.parent.Find("Title").GetComponent<TMP_Text>(),"games.score");\n        hud.SetActive(false);')
edit('Assets/Scripts/Catch/CatCatchGameController.cs','$"1 LIFE PER HUNT   •   {CatchLivesService.CurrentLives}/{CatchLivesService.MaximumLives} READY"','GameLanguageService.Format("games.lives_ready",CatchLivesService.CurrentLives,CatchLivesService.MaximumLives)')
for old,tr,en in [('REDUCED MOTION  ON','Hareketi azalt · Açık','Reduced motion · On'),('REDUCED MOTION  OFF','Hareketi azalt · Kapalı','Reduced motion · Off'),('SOUND  ON','Ses · Açık','Sound · On'),('SOUND  OFF','Ses · Kapalı','Sound · Off'),('HAPTICS  ON','Titreşim · Açık','Haptics · On'),('HAPTICS  OFF','Titreşim · Kapalı','Haptics · Off')]:
 edit('Assets/Scripts/Runner/CatRunnerGameController.cs','"'+old+'"',f'GameContentCopy.Text("{tr}","{en}")')
# Existing products retain authored content; a purchased room is a navigation target.
edit('Assets/Scripts/ShopPanelController.cs','''        if (productId == HomeStoreService.HomeRoomsPreviewId)
        {
            SetFeedback("LIVING ROOM IS YOUR CURRENT ROOM  •  COMPLETE ALL " +
                        HomeStoreService.LivingRoomItemCount + " ITEMS");
            return;
        }''','''        foreach(var room in HomeRoomService.Rooms)
        {
            string roomProduct=room.IsAlwaysUnlocked?HomeStoreService.HomeRoomsPreviewId:room.RequiredOwnershipId;
            if(roomProduct!=productId || !HomeRoomService.IsRoomUnlocked(room.Id))continue;
            if(HomeRoomService.CurrentRoomId==room.Id)
                SetFeedback(GameContentCopy.Text("Zaten bu odadasın.","You’re already in this room."));
            else
            {
                var loader=FindAnyObjectByType<LevelLoader>(FindObjectsInactive.Include);
                if(loader!=null && loader.LoadRoom(room.Id))RequestClose();
            }
            return;
        }''')
print('HUD and owned-room flow updated.')
