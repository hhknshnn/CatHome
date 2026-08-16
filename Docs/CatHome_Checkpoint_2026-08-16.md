# Cat Home — Checkpoint 16 Ağustos 2026

**Proje:** `C:\Users\HAKAN\Desktop\CatHome\CatHome`  
**Unity:** `6000.4.4f1` (`CatHome@26b7b116b75259f7`)  
**Kayıt şeması:** v9  
**Commit:** yok (istenmedi)

## Doğrulama

- `LevelContentValidator`: **0 error, 0 warning**
- EditMode: **172 passed / 0 failed**
- Canonical stack: `GameScene` + `CatHome_UI` + `LivingRoom_Level01`
- PlayMode ekran görüntüleri: `Assets/Screenshots/Checkpoint_2026-08-16/`

## 1. Living room pastel uyumu

Oturma odası mutfak/banyo ile aynı cream / aqua / mint / peach / lilac paletine çekildi.

- Duvar materyalleri: `PremiumWallSky`, `PremiumWallMint`, `PremiumWallPeach`
- Zemin: `PremiumCreamTrim`
- Koltuk coral, halı mint, sehpa peach, lamba gold
- Pack materyali `M_LowPolyLivingRoom.mat` açık pastel tinte alındı

**Dosyalar:** `Assets/Editor/PremiumWorldVisualBuilder.cs`

## 2. Cat tunnel — ölçek + geçiş

Tünel kedi ölçeğine küçültüldü (ayak izi `0.58 x 0.90`, yükseklik `0.44`). İçi boş gökkuşağı halkaları; collider trigger. Kedi giriş→çıkış squash crawl yapıyor (`ActivityTunnelCrawl`).

**Dosyalar:**

- `Assets/Scripts/Activities/TunnelPlayActivity.cs`
- `Assets/Scripts/Activities/CatActivity.cs` (`TunnelPlay`)
- `Assets/Scripts/Activities/CatActivityReaction.cs`
- `Assets/Scripts/QuestType.cs` (`TunnelPlay = 7`)
- `Assets/Editor/CatActivityAnimationBuilder.cs`
- `Assets/Editor/StoreProductContentBuilder.cs`
- `Assets/Editor/StoreCatalogAssets.cs`
- Prefab: `Assets/Art/StoreProducts/PlayTunnel.prefab`

## 3. Banyo aynası duvar yüksekliği

Procedural pastel ayna; `HungHeight = 1.58`. Duvar yerleşiminde Y korunur; wall/wainscot/baseboard overlap yok sayılır. Play’de ayna sol duvarda, vanity üstünde asılı.

**Dosyalar:** `HomeProductPlacement.cs`, `StoreCatalogAssets.cs`, `StoreProductContentBuilder.cs`

## 4. Cat Runner UI

Welcome iki sütunlu premium kart (hero, BEST SCORE, LIVES 5/5, START RUN, EXIT). HUD köşe kapsülleri yeniden hizalandı. Result kartı büyütüldü; `RUN AGAIN • LIVES N`.

**Dosyalar:** `CatRunnerContentBuilder.cs`, `CatRunnerGameController.cs`

## 5. Bedroom (mutfaktan sonraki oda)

Sahne: `Assets/Scenes/Levels/Bedroom_Level01.unity`  
Oda id: `bedroom-01`  
Kilidi: mutfak koleksiyonu tamam + `home.bedroom-preview` (5000 jeton / 50 elmas)

ROOM sekmesi seçili yatak odasındayken 10 ürün (ucuzdan pahalıya):

| Ürün | Id | Jeton |
| --- | --- | --- |
| Starry Paw Rug | `bedroom.paw-rug` | 200 |
| Moon Night Light | `bedroom.night-light` | 400 |
| Dream Wall Art | `bedroom.dream-art` | 600 |
| Yarn Basket | `bedroom.yarn-basket` | 800 |
| Pastel Nightstand | `bedroom.nightstand` | 1000 |
| Cloud Vanity Stool | `bedroom.vanity-stool` | 1200 |
| Rainbow Wardrobe | `bedroom.wardrobe` | 1500 |
| Window Daybed | `bedroom.window-daybed` | 1800 |
| Star Canopy | `bedroom.star-canopy` | 2100 |
| Queen Cloud Bed | `bedroom.queen-bed` | 2500 |

Hepsi 100 jeton = 1 elmas. Boş kabuk; ürünler satın alınınca görünür. Kamera/kedi/shell Living Room ile aynı.

**Dosyalar:** `BedroomLevelBuilder.cs`, `HomeRoomService.cs`, `HomeStoreService.cs`, `ShopPanelController.cs`, `RoomSelectorPanelBuilder.cs`, `StoreProductContentBuilder.cs`, `SceneArchitectureBuilder.cs`, `LevelContentValidator.cs`

## 6–8. Cat Catch, Games menüsü, ayrı canlar

**Cat Catch:** 60 sn tap-to-pounce, 5 fare, pastel pembe arena. Welcome `START HUNT` / `EXIT TO MAIN MENU`. Can yalnız hunt başında harcanır.

**Games hub:** Dock `PlayCatRunnerButton` etiketi **GAMES**. İçinde CAT RUNNER ve CAT CATCH, her birinin kendi `LIVES 5/5` sayacı.

**Ayrı canlar:**

- Runner: `RunnerEnergyService` max 5
- Catch: `CatchLivesService` max 5, 10 dk yenilenme, kayıt `catchLives`
- Birinin harcanması diğerini etkilemez

Save v9: `catchLives` + `catchBestScore`.

**Dosyalar:**

- `Assets/Scripts/Games/GamesHubPanel.cs`
- `Assets/Scripts/Catch/CatchLivesService.cs`
- `Assets/Scripts/Catch/CatCatchGameController.cs`
- `Assets/Scripts/Catch/CatCatchPlayer.cs`
- `Assets/Scripts/Catch/CatCatchMouse.cs`
- `Assets/Scripts/Catch/CatCatchLauncher.cs`
- `Assets/Editor/CatCatchContentBuilder.cs`
- `Assets/Scenes/Catch/CatCatch.unity`
- `CatRunnerLauncher.cs`, `CatHomeSaveSystem.cs`, `EconomyTypes.cs`

## 9. Görsel dil

Pastel cream/aqua/mint/coral/lilac/lemon. Catch ve Games hub `LowPolyPanelGraphic` + `PremiumButtonFx`. Catch, ev HUD’unu Runner gibi gizler.

## 10. Ekran görüntüleri

Hepsi Play Mode, `Assets/Screenshots/Checkpoint_2026-08-16/`:

| Dosya | İçerik |
| --- | --- |
| `01_living_room_home.png` | Ev + While You Were Away |
| `02_living_room_after_popup.png` | Oturma odası + küçük tünel |
| `03b_games_hub_ui.png` | Games hub, ayrı 5/5 canlar |
| `04_cat_catch_welcome.png` | Cat Catch welcome |
| `05_cat_runner_welcome.png` | Cat Runner welcome |
| `06_cat_runner_hud.png` | Runner oyun HUD |
| `07_cat_runner_result.png` | Runner sonuç |
| `08_living_room_pastel.png` | GAMES dock, RUN/CATCH canları |
| `09_bedroom.png` | Yatak odası + 10 eşya |
| `10_bathroom.png` | Banyo + duvar aynası |

## Sistem notları / sonraki iş

- Bedroom room-selector ve mağaza ikonu `BedroomPreview.png` / `BedroomRoomPreview.png` olarak eklendi.
- Living Room orijinal duvar renderer’ları gizlendi; görünür yüzey pastel liner + candy wainscot.
- PlayMode: 7/7 geçti.
- IAP elmas paketleri hâlâ doğrulanmış satın alma olmadan can/elmas vermez.
