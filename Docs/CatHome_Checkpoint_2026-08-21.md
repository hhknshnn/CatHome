# Cat Home — Checkpoint 21 Ağustos 2026

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

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
- EditMode: **270/270 geçti**
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
- Hesap çekirdeği tamamlandı: ilk PLAY/onaylı NEW GAME hesabı seçtirir, misafir
  anında çalışır, Settings hesap durumunu gösterir, Google/Unity Player Accounts
  bağlantısı çalışır. Dashboard sağlayıcı/client-ID ve gerçek Editor Google dönüşü
  tamamlandı; Android gerçek cihaz deep-link testi yayın öncesinde açık.
- Bulut kayıt ve Runner/Catch DAILY/WEEKLY/ALL-TIME sıralamaları tamamlandı; altı
  tablo, Cloud Code ve access-control paketi 24 Ağustos 2026'da Unity Cloud
  production ortamına dağıtıldı. Güncel devir:
  `Docs/CatHome_Checkpoint_2026-08-24.md`.
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
  bağlantısı, Cloud Save write-lock ile kesintisiz en-yeni-kayıt çözümü, Runner/Catch için ayrı
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

## Devam checkpointi — 21 Ağustos 2026 (hesap çekirdeği)

- İlk oyuncuda Main Menu `PLAY`, mevcut oyuncuda onaylı `NEW GAME`, ayrı premium
  hesap kartını açar: `GOOGLE İLE GİRİŞ YAP` veya `MİSAFİR OLARAK DEVAM ET`.
- Google seçeneğinin altında bulut kayıt, çevrimiçi sıralama ve başka cihazda devam
  avantajı gösterilir. Misafir seçimi çevrimiçi servisi beklemeden oyunu başlatır.
- `AccountIdentityService` ve Unity Authentication **3.5.2** eklendi. Rastgele yerel
  misafir kimliği ile hesap tercihi save v11 dışında kalır; NEW GAME bunları silmez.
- Unity Player Accounts tarayıcı akışı, geri dönen anonim oyuncuyu aynı UGS Player
  ID'sine bağlar. Giriş iptal/hatasında save sıfırlanmaz veya değişmez.
- Settings paneli yedi satırdır; `ACCOUNT / HESAP` satırı `CHOOSE / GUEST /
  GOOGLE CONNECTED` durumunu gösterir ve sonradan bağlantı başlatır.
- Dashboard Unity Player Accounts sağlayıcısı PC + Android/iOS için etkinleştirildi;
  gerçek OAuth client ID Unity servis ayarına yazıldı. Editor localhost dönüşü gerçek
  Google hesabıyla doğrulandı. Android cihaz deep-link testi açık; gizli değer yok.
- 1920×1080 QA:
  - `Assets/QA/PremiumVisuals/2026-08-21_AccountChoice_TR.png`
  - `Assets/QA/PremiumVisuals/2026-08-21_AccountChoice_EN.png`
  - `Assets/QA/PremiumVisuals/2026-08-21_SettingsAccount_TR.png`
- EditMode **270/270**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room ve
  Camera/AudioListener/EventSystem **1/1/1**. Save şeması **v11**.
- Git commit/push yok.

## Devam checkpointi — 21 Ağustos 2026 (resmî Google rozeti + callback)

- `GOOGLE İLE GİRİŞ YAP` düğmesine Google'ın resmî, ön onaylı renkli `G` karesi
  eklendi. Varlık yeniden çizilmez/renklendirilmez; premium aqua halo ve pearl yüzey
  simgenin dışında kalır.
- Android/iOS'ta `StartSignInAsync` tarayıcıyı açınca hemen döndüğü için giriş artık
  `SignedIn`/`SignInFailed` olayını bekler. Böylece gerçek deep-link dönüşü yanlışlıkla
  iptal sayılmaz; oyuncu beklerken `GERİ` ile çıkabilir.
- 1920×1080 QA: `Assets/QA/PremiumVisuals/2026-08-21_AccountChoice_TR_GoogleBrand.png`.
- Unity Cloud'da `CatHome` Unity Player Accounts sağlayıcısı PC + Android/iOS için
  etkinleştirildi. Gerçek Google hesabıyla Editor localhost callback tamamlandı;
  Player Accounts ve UGS Authentication oturumları bağlı doğrulandı. Client ID Unity
  servis ayarındadır; token/sır repoya yazılmadı. Gizlilik politikası bağlantısı ve
  Android gerçek cihaz dönüş testi yayın öncesinde açık.
- EditMode **271/271**, PlayMode **10/10**, validator **0/0**, Console temiz; kanonik
  `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room ve
  Camera/AudioListener/EventSystem **1/1/1**. Save **v11**. Git commit/push yok.

## Devam checkpointi — 21 Ağustos 2026 (Cloud Save + oyuncu verileri)

- Unity Cloud Save Player Files **3.4.0** eklendi. `cat_home_save_v11`, mevcut tam
  v11 JSON'u taşır; e-posta, Google adı, parola, access token veya servis sırrı taşımaz.
- SHA-256 yerel baz + Cloud Save `writeLock` tek ve iki taraflı değişimi tanır. En yeni
  geçerli kayıt otomatik uygulanır; tam zaman eşitliğinde bulut kazanır. `CONTINUE`
  doğrudan oyunu açar ve oyuncuya cihaz/bulut seçim ekranı gösterilmez.
- Bulut kazanırsa gelecekteki/geçersiz şema reddedilir, `.before-cloud-*` kurtarma
  kopyası alınır, kayıt atomik uygulanır ve aktif oda yeniden bağlanır. Cihaz kazanırsa
  eski bulut JSON'u `.cloud-shadow-*` yerel destek yedeğine alınıp güncel write lock
  ile buluta yüklenir.
- Her başarılı yerel save üç saniye debounce ile eşitlenir. Unity Services hazır
  olmadan Authentication singleton'ına erişilmez; çevrimdışı normal açılış temizdir.
- Settings'te `PRIVACY & DATA / GİZLİLİK VE VERİ` açılır: gizlilik politikası, hesap
  silme bilgisi, veri talepleri, manuel sync ve iki-dokunuşlu hesap+bulut silme.
  Hesap silme NEW GAME değildir; yerel misafir save'i cihazda korunur.
- Gerçek Google dönüşü tekrar doğrulandı: Player Accounts açık, UGS Authentication
  bağlı ve gerçek Cloud Save durumu `Synced`.
- Android development ARM64 APK:
  `Builds/Android/CatHome-PlayerAccounts-Dev.apk` (**177,872,682 byte**). APK manifesti
  `com.vexorialabs.cathome`, min SDK 25, target SDK 36, yalnız `arm64-v8a` ve
  `unitydl://com.unityplayeraccounts.492516d1-dc10-4c60-91fc-bda6375371fd`
  BROWSABLE VIEW intentini içerir. ADB'de cihaz görünmediği için fiziksel dönüş açık.
- Cat Home Player Care sitesi TR/EN gizlilik, hesap silme ve veri talebi sayfalarıyla
  hazırdır; güvenli yayın politikası gereği kullanıcı herkese açık erişimi onayladıktan
  sonra Dashboard/mağaza URL'lerine bağlanacaktır.
- 1920×1080 QA:
  - `Assets/QA/PremiumVisuals/2026-08-21_PrivacyData_TR.png`
- EditMode **282/282**, PlayMode **10/10**, validator **0/0**; kanonik
  `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room ve
  Camera/AudioListener/EventSystem **1/1/1**. Save **v11**. Git commit/push yok.

## Devam checkpointi — 21 Ağustos 2026 (oyundan ana menüye dönüş)

- Oyun içi açılır menüye premium ev + dönüş oku ikonlu, TR/EN yerelleştirilmiş
  `RETURN TO MAIN MENU / ANA MENÜYE DÖN` satırı eklendi.
- Eylem mevcut v11 yolculuğunu kaydeder, açılır menünün input bloğunu bırakır ve
  sahne yeniden yüklemeden Main Menu v4 katmanını açar. Oda, kamera, HUD ve hesap
  durumu korunur; NEW GAME çalışmaz.
- Canlı Play doğrulaması: tek `MainPanelController`, satır dokunması sonrası
  `TitleScreen.IsShowing = true` ve açılır menü kapalı.
- EditMode **283/283**, PlayMode **10/10**, validator **0/0**; kanonik
  `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room ve
  Camera/AudioListener/EventSystem **1/1/1**. Save **v11**. Git commit/push yok.

## Devam checkpointi — 21 Ağustos 2026 (Player Care + çevrimiçi rekabet)

- Cat Home Player Care sitesi herkese açık ve canlıdır:
  `https://cathome-player-care.hhknshnn.chatgpt.site`.
  TR/EN `/privacy`, `/delete-account` ve `/data` rotaları yayınlandı. Unity
  Dashboard/mağaza alanlarına URL bağlama, Google'ın hesap adı/e-posta paylaşımı
  için gösterdiği son izin ekranında kullanıcı onayını bekler.
- Geçici yayın adresi oyun bitimine yakın `cathome.vexorialabs.com` alt alan adına
  taşınacaktır. DNS/HTTPS doğrulandıktan sonra oyun, Dashboard ve mağaza URL'leri
  tek seferde yeni kanonik adrese çevrilecektir.
- Games Hub'a premium `LEADERBOARDS` yüzeyi eklendi: Runner/Catch, DAILY/WEEKLY/
  ALL-TIME, ilk 50, kendi sıra kartı, çevrimdışı son güvenli görünüm ve 3–16
  karakterlik denetimli takma ad. Google gerçek adı/e-postası skor metadatasında
  kullanılmaz.
- Runner ve Catch yalnız tamamlanmış oyunların ham ölçümlerini gönderir.
  `CatHomeCompetition` Cloud Code modülü skoru sunucuda tekrar hesaplar, oyun
  zarfını doğrular ve tekrar kullanılan run/hunt kimliklerini Cloud Save işlem
  anahtarlarıyla reddeder. İstemcinin tablolara doğrudan skor yazması access-control
  politikasında kapalıdır.
- Altı tablo tanımı hazırdır: Runner/Catch × günlük/haftalık/all-time. Günlük ve
  haftalık tablolar arşivlenir; dönem ödülü sıra+katılım dilimine göre Coin/Diamond
  döndürür ve `leaderboard:{board}:{version}` işlem kimliğiyle yalnız bir kez uygulanır.
- Yerel sunucu Release build **0 hata / 0 uyarı**. Unity Cloud canlı dağıtımı
  24 Ağustos 2026'da tamamlandı; Android gerçek cihaz deep-link testi kullanıcı
  kararıyla ertelendi.
- QA görseli:
  `Assets/Screenshots/Checkpoint_2026-08-21/LeaderboardPanel_1920x1080_UI.png`.
- EditMode **295/295**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room ve
  Camera/AudioListener/EventSystem **1/1/1**. Save **v11**. Git commit/push yok.

## Devam checkpointi — 21 Ağustos 2026 (HUNT enerji geri bildirimi)

- Canlı teşhis: `ActivityActionButton` raycast/listener bağlantısı sağlamdı; mevcut
  kayıtta enerji **0**, Mouse Hunt maliyeti **12** olduğu için aktivite başlamıyordu.
  Eski `HUNT` etiketi yetersiz enerji nedenini gizleyip düğmeyi bozuk gösteriyordu.
- `ActivityPromptController` enerji yetersizken `NEED 12 ENERGY` (aktivitenin gerçek
  maliyetinden dinamik) gösterir. Düğme dokunulabilir kalır ve kedi `I NEED A NAP
  FIRST!` geri bildirimini verir; yeterli enerjide etiket tekrar `HUNT` olur.
- Canlı doğrulama: 12 enerjiyle dokunma sonrası `IsRunning = true`, enerji **0** ve
  ilerleme `CATCH THE MOUSE 0/3`; böylece düğme ve aktivite yolu birlikte doğrulandı.
- Yeni kilit testiyle EditMode **296/296**, PlayMode **10/10**, validator **0/0**,
  Console temiz; kanonik üç sahne, aktif Living Room ve
  Camera/AudioListener/EventSystem **1/1/1**. Git commit/push yok.
