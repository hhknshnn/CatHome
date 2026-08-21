# Cat Home — Checkpoint 21 Ağustos 2026

**Proje:** `C:\Users\HAKAN\Desktop\CatHome\CatHome`  
**Unity:** `6000.4.4f1` (`CatHome@26b7b116b75259f7`)  
**Kayıt şeması:** v11  
**Katalog:** 114 ürün  
**Odalar:** 8 oynanabilir oda  
**Commit / push:** yok — kullanıcı manuel yapar

## Güncel ürün durumu

- Coming Soon oda zinciri tamam: Balcony (LV9), Garden Patio (LV10), Second Floor
  (LV12). Üçü de 10 ürünlük koleksiyon, oda ödülü, Room Selector ve SHOP HOME
  kartıyla oynanabilir.
- 114 ürün ve 8×10 oda koleksiyonu için ekonomi rotası doğrulandı. Kanonik tam
  oda rotası 145.000 Coin; preview fiyatları 3000→9000; koleksiyon iadeleri hedef
  %3–%5 bandında.
- Premium v2 genel görsel/işlevsel polish tamamlandı: ana ev, popup'lar, HUD,
  hamburger, Quest, Cat Journal, Settings, Rooms, SHOP ve satın alma akışları,
  Games hub, Runner ve Catch aynı pearl/cream + canlı candy pastel + kontrollü
  altın dilinde doğrulandı.
- Save v11 değişmedi.

## Main Menu v4 — son teslim

- Boş kalabilen canlı kedi portresi ve RenderTexture/retry akışı kaldırıldı.
- Sol marka dock'u çakışmasızdır: CAT HOME amblemi, karşılama/isim, HOME LV,
  Coin, koleksiyon, PLAY/CONTINUE, yardımcı eylemler ve slogan.
- Blender CAT HOME amblemi daha açık pearl, aqua/mint ve champagne-gold
  malzemelerle yeniden render edildi.
- Unity'de ölçülü aqua/mint/gold neon aura, dört köşe ışıltısı ve hafif nefes
  animasyonu `TitleLogoNeonFx` ile çalışır. Reduced Motion tüm logo ve çocuk
  ışıltı hareketini dondurur.
- SHOP, ROOMS ve GAMES üç farklı sanat dili kullanmaz; aynı ortografik kamera,
  ışık, malzeme, kadraj ve rounded diorama ailesindedir.
- Buton/kart yüzeyleri merkezli glow + champagne rim + candy gradient + gloss +
  iç derinlik kullanır; dış Shadow/Outline ve düz ColorTint regresyonu yoktur.

## İlk kez açan oyuncu

İlk açılış mevcut oyuncudan ayrı bir prefab kullanmaz; aynı premium menü veri
durumuna göre yenilenir:

- `YOUR COZY CAT AWAITS`
- `WELCOME HOME` — isim seçilmeden `MELO'S HOME` gibi uydurma ad gösterilmez
- `PLAY`
- HOME LV. 1, 0 Coin, `0 OF 114 • COLLECTED`
- Mobilde QUIT gizli; SETTINGS ve CREDITS ortalı
- SHOP / ROOMS / GAMES görünür teaser'lardır fakat onboarding bitene kadar
  `AFTER TOUR` rozetiyle pasiftir

Bu kilit, sağ kartlardan birinin isim verme/onboarding diyaloğuyla aynı anda
açılması yarışını engeller. PLAY sonrası sıra: isim → tanışma/bakım turu →
kutlama ve starter ödülü. Tur tamamlandıktan sonra kartlar açılır; sonraki
girişler `{NAME}'S HOME` ve `CONTINUE` gösterir.

## Kanonik sanat ve kod

### Sanat

- `Assets/Art/Title/CatHome_TitleHero_v1.png`
- `Assets/Art/Title/CatHome_MainMenuLogo_v1.png`
- `Assets/Art/Title/MainMenu_ShopCard_v1.png`
- `Assets/Art/Title/MainMenu_RoomsCard_v1.png`
- `Assets/Art/Title/MainMenu_GamesCard_v1.png`
- `ArtSource/Blender/Title/CatHome_MainMenuLogo_Source.blend`
- `ArtSource/Blender/Title/CatHome_MainMenuCards_Source.blend`

### Kod / builder / kilit testi

- `Assets/Scripts/TitleScreen.cs`
- `Assets/Scripts/TitleLogoNeonFx.cs`
- `Assets/Editor/TitleScreenBuilder.cs`
- `Assets/UI/TitleScreen.prefab`
- `Assets/Tests/EditMode/PremiumPresentationTests.cs`

## 1920×1080 QA görselleri

- Devam eden kayıt: `Assets/Screenshots/PremiumV2/MainMenuV4_NeonFinal1920.png`
- İlk açılış: `Assets/Screenshots/PremiumV2/MainMenuV4_FirstLaunchNeonFinal1920.png`
- Premium v2 genel set: `Assets/Screenshots/PremiumV2/Final1920_*.png`

## Son doğrulama

- Derleme / script diagnostics: **0 hata, 0 uyarı**
- EditMode: **268/268 geçti**
- PlayMode: **10/10 geçti**
- `LevelContentValidator`: **0 error / 0 warning**
- Console: **0 error / 0 warning**
- Açık sahneler: `GameScene` + `CatHome_UI` + `LivingRoom_Level01`
- Aktif sahne: `LivingRoom_Level01`
- Etkin Camera / AudioListener / EventSystem: **1 / 1 / 1**
- Editor Play ayarı: `DisableSceneReload`; domain reload açık
- Test Runner kalıntısı yok; geçici `InitTestScene*.unity` dosyaları yok
- Editor interaction normal: `ApplicationIdleTime=4`, `InteractionMode=0`

## Açık işler ve çalışma politikası

- Onaylı `NEW GAME` ve Türkçe/İngilizce çekirdeği tamamlandı. Kalan oyun içi
  HUD/SHOP/Rooms/Runner/Catch metinlerinin tabloya taşınması oyun bitimine yakın,
  bütün metinler kesinleşip dondurulduktan sonra yapılacak zorunlu yayın paketidir.
  Dil sırası: Japonca → Korece → Geleneksel Çince → Brezilya Portekizcesi →
  Latin Amerika İspanyolcası; Almanca/Fransızca oyuncu verisine göre ikinci dalga.
- Yerel kayıt kurtarma tamamlandı: her başarılı save `.recovery` kopyasını yeniler;
  eksik/bozuk ana dosya bu kopyadan döner; daha yeni şema eski kopyayla ezilmez.
- Google hesabı, bulut kayıt ve Runner/Catch DAILY/WEEKLY/ALL-TIME sıralamaları
  Aşama 6 olarak planlandı. Sözleşme: `Docs/ONLINE_COMPETITION_PLAN.md`.
- Mobil performans ve uzun oturum testleri normal ara-adım önerilerine alınmaz.
  Kullanıcı “her şey bitti, deneyelim” veya “her şey bitti, yayınlayalım” dediğinde
  yapılmadıkları yayın öncesi eksik kontrol olarak hatırlatılır.
- Premium v2 ve Main Menu v4 tamamlanmış kabul edilir; yeni çalışma bunları eski
  title v1/v2, canlı portre, karışık kart sanatı veya düz panel diline döndürmemelidir.
- Git commit/push yapılmadı; kullanıcı QA sonrasında manuel commit eder.

## Güvenli devralma

1. `Docs/ROADMAP.md` ve çalışma alanı `AGENTS.md` kurallarını oku.
2. Unity'de kanonik üç sahnenin açık ve `LivingRoom_Level01` sahnesinin aktif
   olduğunu doğrula.
3. Premium UI/title değişirse `TitleScreenBuilder.BuildSilently()` veya tam
   `PremiumUiSystemRebuild.Rebuild()` zincirini kullan.
4. Sonunda EditMode + PlayMode + validator + temiz Console + 1/1/1
   kamera/listener/EventSystem kontrolünü yeniden yap.

## Devam checkpointi — 21 Ağustos 2026 (kayıt kurtarma + rekabet sözleşmesi)

- Save şeması **v11** kaldı.
- `CatHomeSaveSystem` her başarılı atomik yazımdan sonra
  `cat-home-save.json.recovery` kopyasını atomik olarak yeniler.
- Ana save eksik veya okunamazsa son başarılı recovery kopyası doğrulanıp geri
  yüklenir; bozuk dosya timestamp'li `.corrupt-*` olarak korunur.
- Desteklenenden yeni save şeması recovery ile sessizce geriye alınmaz; `.incompatible-*`
  olarak korunur.
- `CatHomeSaveRecoveryTests`: **4/4**.
- Tüm EditMode: **260/260**; PlayMode: **10/10**.
- `LevelContentValidator`: **0 hata / 0 uyarı**; Console temiz.
- Kanonik `GameScene + CatHome_UI + LivingRoom_Level01`; aktif Living Room;
  Camera/AudioListener/EventSystem **1/1/1**.
- Online ürün yönü: hesap duvarı olmayan anonim başlangıç, isteğe bağlı Google
  bağlantısı, Cloud Save write-lock çatışma seçimi, Runner/Catch için ayrı
  DAILY/WEEKLY/ALL-TIME tabloları ve sunucu doğrulamalı idempotent ödüller.
- Git commit/push yok.

## Yayın öncesi dil kararı — 21 Ağustos 2026

- Yeni diller geliştirme ortasında parça parça çevrilmeyecek.
- Oyun içeriği, görevler, ürün/oda adları, ekonomi metinleri ve tüm arayüz kopyası
  kesinleşince metin dondurma yapılacak.
- Son yerelleştirme paketi tüm görünen metinleri tablolara taşımayı; CJK font ve
  satır/taşma QA'sını; mağaza sayfası metinlerini ve ekran görüntüsü kopyalarını kapsar.
- Öncelikli hedefler: Japonya, Güney Kore, Tayvan/Hong Kong, Brezilya ve Latin Amerika.
- Çin ana karası, yalnız çeviri işi değil dağıtım/onay gerektiren ayrı yayın projesidir.

## Devam checkpointi — 21 Ağustos 2026 (onaylı NEW GAME + TR/EN altyapısı)

- Main Menu v4'e mevcut yolculukta görünen `NEW GAME` ve ayrı onay katmanı eklendi.
  İlk oyuncuda düğme gizlidir; masaüstü yardımcı eylemleri dört, mobil üç düğme
  olarak optik merkeze yerleşir.
- Onaylanan sıfırlama Coin, Home/Bond ilerlemesi, görev/daily/achievement,
  oda/eşya sahipliği ve yerleşimleri, Runner/Catch skor/tutorial/misyonlarını,
  isim ve kürkü temizler. İhtiyaçlar 100/100/100, oda Living Room olur.
- Diamonds, işlenmiş satın alma transaction kimlikleri, doğrulanmış sınırsız
  Runner/Catch geçişleri, müzik/ses/haptics/reduced-motion tercihleri ve dil
  korunur. Yazımdan önce timestamp'li `.before-new-game-*` güvenlik kopyası alınır.
- `GameLanguageService` Türkçe cihazda Türkçe, diğer sistem dillerinde İngilizce
  başlar. Ayarlar'daki `DİL / LANGUAGE` satırı seçimi kalıcı ve canlı uygular.
  Main Menu, Settings, Credits ve NEW GAME katmanı TR/EN sözlüğüne taşındı.
- Fredoka dinamik atlası `İŞĞÜÖÇışğüöç` karakterlerinin tamamını üretir.
- QA görselleri:
  - `Assets/QA/PremiumVisuals/2026-08-21_MainMenu_TR_NewGame.png`
  - `Assets/QA/PremiumVisuals/2026-08-21_Settings_TR_Language.png`
  - `Assets/QA/PremiumVisuals/2026-08-21_Settings_EN_Language.png`
  - `Assets/QA/PremiumVisuals/2026-08-21_NewGame_Confirm_TR.png`
- `NewGameAndLocalizationTests`: **7/7**; tüm EditMode **268/268**;
  PlayMode **10/10**; validator **0/0**; Console temiz.
- Kanonik `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room;
  Camera/AudioListener/EventSystem **1/1/1**. Save şeması **v11**.
- Git commit/push yok.
