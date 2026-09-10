# Cat Home — Renkli arayüz ve arcade dünyaları, 8 Eylül 2026

**Güncel uygulama, son kontroller ve Android paketi tamamlandı.** Kullanıcının son talimatı bilgisayarı **KAPATMAMAK**. Commit/push manuel kalır.

Bu tur, [8 Eylül checkpoint](CatHome_Checkpoint_2026-09-08.md) ve [önceki üretim geçişi](PRODUCTION_PASS_2026-09-07.md) üzerine gelir. Kullanıcının yeni ekran görüntülerindeki tekdüze HUD/paneller, içerikten kaymayan listeler, oturma/loaf sırasında dinlenme ve mini oyunların görsel/hareket sorunları ele alındı. Önceki oda planları, bakım istasyonu, doğal sesler, sahiplik ve kayıt kimlikleri korunur.

## Arayüzde yapılanlar

`JoyfulUiArt`, `JoyfulScreenBuilder`, `JoyfulMotifGraphic` ve `JoyfulConfettiGraphic` ortak sunumu oluşturur. İhtiyaç kartlarında mama için sıcak turuncu, su için mavi, enerji için mor alanlar kullanılır. Kedi kimliği ve analog mavi; alt gezinmede Mağaza sarı, oda mavi, Birlikte mor, Oyunlar mercandır. Böylece sık kullanılan eylemler renk ve ikonla ayırt edilir. Yeni motifler dekoratiftir; dokunmayı engellemez.

Ana menü, mağaza, oda listesi, oyun seçimi, dönüş ve kutlama panellerinde belirgin başlık alanları, fotoğraf zeminleri ve renkli eylemler vardır. Mağazanın çalışırken yenilenen düğmeleri de yeni paleti kullanır: kullanılabilir ana eylem mavi/beyaz; sahip olunan veya kullanılamayan durumlar açık zemin/koyu yazı kullanır. Seçili oda kartı açık mavi, diğer kartlar açık kremdir. Dar ekranda ürün fotoğrafı zemini kartın genişliğine gerilir; sabit genişlikle komşu kartın üzerine taşmaz.

Bakım ve aktivite düğmelerinin gerçek ön yazısı bazı sahnelerde hâlâ `Text (TMP)` adındadır. `PremiumTypography`, bu etiketi `ContextFace → ActionButton / ActivityActionButton / ActivityProgressBadge` bağlamından tanır ve Fredoka kullanır. Yalnız `ActionButtonText` adına bakmak yeterli değildir. Nunito açıklama metinlerinde kalır; Türkçe font atlası kuralları korunur.

Kanıt: [son HUD](QA/JOYFUL_ARCADE_2026-09-08/screens-wide/03_HomeHUD_Final.png), [mağaza](QA/JOYFUL_ARCADE_2026-09-08/screens-wide/13_ShopCat.png), [odalar](QA/JOYFUL_ARCADE_2026-09-08/screens-wide/11_Rooms.png), [oyun seçimi](QA/JOYFUL_ARCADE_2026-09-08/screens-wide/12_Games_Final.png).

## Kaydırmanın asıl nedeni ve ortak düzeltme

Mağaza, oda ve ırk listelerinin kırpılan içerik alanında raycast alacak bir `Graphic` bulunmuyordu. Fotoğraflar ve boşluklar dekoratif olduğu için oralardan başlayan tekerlek/sürükleme olayları `ScrollRect` yerine dış panele gidiyordu. Rehberdeki tamamen şeffaf yüzeyin çizimden elenmesi de aynı giriş yolunu bozabiliyordu.

`PremiumScrollInput.Ensure(ScrollRect)` görünüm alanına şeffaf bir giriş yüzeyi ekler veya mevcut yüzeyi kullanır. `raycastTarget=true`, `cullTransparentMesh=false` ve en az 48 kaydırma hassasiyeti uygulanır. İçerik fotoğrafı, boşluk, fare sürükleme, dokunarak sürükleme ve fare tekerleği Unity'nin kendi EventSystem/ScrollRect akışından geçer. Kart düğmeleri üstte kendi tıklamasını almaya devam eder; sürükleme sırasında yanlış satın alma tıklaması üretilmez. Her kare çalışan ayrı bir giriş taraması eklenmedi.

Ortak bağlantılar `ShopPanelController`, `QuestPanelController`, `RoomSelectorPanel`, `CatBreedShopPanel` ve `CatCompanionPanel` rehberindedir. Azaltılmış harekette atalet durur; oyuncunun doğrudan kaydırma hareketi açık kalır. Tercih geri açıldığında tasarlanmış atalet ayarı geri gelir.

`PopupScrollInputTests` gerçek EventSystem/GraphicRaycaster ile içerik üzerinden tekerlek, fare ve dokunma sürüklemesini, düğme tıklamasını ve azaltılmış hareketi denetler. Ayrıca gerçek panel prefablarının görünüm alanları kullanılır. Bu üç native test [UI sonucunda](QA/JOYFUL_ARCADE_2026-09-08/Native-ui-final.xml) başarılıdır.

## Birlikte komutları ve dinlenme

Kullanıcının yeni kararı, önceki checkpoint'teki oturma/loaf için enerji kazanmama kuralını günceller. `CatCommandActivity.RestEnergyPerSecond = .35f`; `EnergySystem` yalnız `IsRecoveringEnergy`, yani gerçek kalıcı dinlenme evresi sırasında saniyede **0,35 enerji puanı** ekler. 0–100 göstergesinde bu, uygun koşullarda dakikada 21 puandır. Mobilya dinlenmesinin ayrı **0,75 puan/sn** değeri korunur.

Oturma/loaf girişinde, kalkışında, iptalinde veya miyav sırasında bu artış verilmez. Oyun duraklatıldığında artış durur; değer 100'ü aşmaz. Anlık bitiş bonusu, jeton, bağ veya görev ödülü eklenmedi. Oturma ve loaf oyuncu Kalk diyene kadar sürer. `CompanionRestEnergyTests` gerçek salonda tutulan poz, duraklatma, iptal, miyav, 100 sınırı ve ödül/görev değişmemesini doğrular; iki test UI paketinde başarılıdır.

Komut panelindeki üç fotoğraf artık seçili ırkın gerçek model ve pozundan gelir. `CompanionPreviewBuilder.Build()` on ırk × üç komut için 30 fotoğraf üretir; eski çağrılar için üç kanonik fotoğraf korunur. Kaynak `Assets/Resources/Companion/{breedId}/{Meow|Sit|Loaf}.png`; boyut 512×288, Android ETC2 RGB4, mipmap ve okunabilir CPU kopyası kapalıdır. Kadraj, pozlanmış gerçek mesh noktalarından ölçülür. Oyun sırasında bu kartlar için üç yeni canlı kamera açılmaz. [Birlikte paneli](QA/JOYFUL_ARCADE_2026-09-08/screens-wide/16_Companion.png) ve altı bölümden oluşan [rehber](QA/JOYFUL_ARCADE_2026-09-08/screens-wide/Guide-3.png) aynı renk dilini kullanır.

## Kutlama arka planları ve kaynak temizliği

İlk canlı turda koleksiyon ekranının altında 80 piksel düz mor şerit görüldü. Oda kamerası alt gezinme için yer ayırırken kutlama yalnız düz bir Scrim kullanıyordu; HUD gizlenince ayrılmış alan görünür kalıyordu. Tanışma kutlamasında da yalnız düz karartma vardı. Seviye kutlamasının eski ekran görüntüsü yöntemi ise HUD/alt şeridi görüntünün içine alabiliyor ve tam ekran CPU piksel kopyası oluşturuyordu.

`CollectionCompleteCelebrationView`, `HomeLevelUpCelebrationView` ve `OnboardingCelebrationView` artık `PremiumModalBackdrop` kullanır. Ortak çözüm, mevcut dünya kamerasını yakalama süresince tam viewport ile render eder; önceki kamera dikdörtgenini `finally` içinde geri yükler. Görüntü HUD içermez. Tek yumuşatılmış görüntü modal açılışında veya ekran boyutu değiştiğinde yenilenir; gizlenme/devre dışı kalmada render hedefi bırakılır. Ek kalıcı kamera yoktur.

Seviye kutlamasındaki `ScreenCapture.CaptureScreenshotAsTexture`, `GetPixels32` ve beş geçici küçültme/büyütme hedefi kaldırıldı. Koleksiyon ve seviye bileşenleri kapanırken çalışan coroutine ve modal bayraklarını da temizler. Tanışmadaki `ConfigureInteractionLayer`, önceden sahneye pişirilmiş panelleri de ortak arka plana geçirir; dim alanı giriş engelini korur. Seviye kutlamasının döngüleri azaltılmış harekette sonlanır, konfeti gizlenir ve kapanış beklemeden gerçekleşir.

Son çekimler: [koleksiyon 16:9](QA/JOYFUL_ARCADE_2026-09-08/screens-wide/06_Collection_Final.png), [koleksiyon 4:3](QA/JOYFUL_ARCADE_2026-09-08/screens-narrow/06_Collection_Final.png), [seviye](QA/JOYFUL_ARCADE_2026-09-08/screens-wide/07_LevelUp_Final.png), [tanışma](QA/JOYFUL_ARCADE_2026-09-08/screens-wide/08_Onboarding_Final.png). Mor alt şerit son koleksiyon karelerinde yoktur. Aynı isimli `_Final` içermeyen kareler önceki ara durumu gösterir.

## Runner ve Catch dünyaları

Yeni headless Blender kaynağı `ArtSource/Blender/MiniGames/build_arcade_worlds.py`; Unity uygulaması `ArcadeMiniGameArtBuilder.ApplyRunner / ApplyCatch` üzerinden yapılır. Üç renkli cephe, turkuaz yol, festival kemeri, palmiye, bulut grubu ve bahçe arenası olmak üzere **sekiz FBX** üretilir. `arcade_worlds_metrics.json`, modellerin her birinde tek materyal ve ortak **256×16** palet dokusu kullanıldığını kaydeder. Ev materyalleri yeniden boyanmaz; mini oyunlara ait materyal kopyaları kullanılır.

Runner'da sıcak renkli dükkânlar, belirgin gökyüzü/bulutlar, palmiye ve kemerler turkuaz yol boyunca ilerler. Engel ve çevre renkleri birbirinden ayrılır. Var olan dokuz varyasyonun kimlikleri, geri dönüşüm kökleri, Garden kilidi ve engel/jeton kuralları korunur. Görsel üretici ek kamera veya ışık eklemez. Kanonik pati jetonu korunur.

Catch, renkli kulüp binası, bahçe, bitki ve oturma çevresiyle yenilendi. Dekor oyun alanının dışında kalır; ortadaki 8×6 alan gerçek hareket için açıktır. Hedef halkası mercan rengine geçti. Önceki hizalı hazırlık, havada yön değiştirmeme, gerçek pati teması, tek temas/tek fare ve boşta duran kedinin fare toplamaması kuralları korunur. Native Catch hareket testleri son motion paketinde başarılıdır.

Giriş, duraklatma, sonuç ve oyun seçimi panellerinin fotoğrafları gerçek hazır Play kamerasından alınır; eksik ilk editör karesiyle değiştirilmez. Mevcut kanıtlar [Runner giriş](QA/JOYFUL_ARCADE_2026-09-08/minigames/Runner-Welcome.png), [Catch giriş](QA/JOYFUL_ARCADE_2026-09-08/minigames/Catch-Welcome.png), [Runner video](QA/JOYFUL_ARCADE_2026-09-08/minigames/Runner-24fps.mp4) ve [Catch video](QA/JOYFUL_ARCADE_2026-09-08/minigames/Catch-24fps.mp4) dosyalarıdır.

## Runner eğilme ve zemin teması

Yeni eğilme gerçek iskeletin dirsek/diz yönlerini ve pati dünya dönüşünü korur; kısa alçak adım ve yumuşak kuyruk eğrisi kullanır. Gövde Y ölçeği ezilmez. Eğilme temposu 1,25–2,6 çevrim/sn, giriş harmanı 90 ms'dir. On ırkın klipleri yeniden üretildi.

Kalkışta son gerçek eğilme pozu 220 ms boyunca yeni koşu pozuna harmanlanır. Eski Duck klibini tekrar ilerleten çapraz geçiş kaldırıldı. Koşu temposu gerçek kısa kaynak klibin süresinden hesaplanır; 2,8 m adım ve en fazla 4 çevrim/sn kullanılır. Parkur hızı, atlama yüksekliği ve çarpışma mesafeleri değişmez. Test, on ırkta 7 ve 11,9 m/sn hızları, 30 ve 60 fps simülasyon adımlarını ve üç ayrı kalkış fazını, harman bittikten sonrasıyla birlikte sınar.

`RunnerGroundContact`, koşu, eğilme ve toparlanmanın son pozunu görünür .033 m yol ve rampaya göre düzeltir. Yukarı yönlü düzeltme gallop pozunu aşağı çekmez. Tek mesh/vertex listesi ve dokuz yeniden kullanılan yüzey örneği vardır; havadaki kedi ve duraklatma atlanır. Her kare normal şerit hareketi kök konum/dönüşünü yeniler, ofset birikmez. Ayrı on-ırk testi 4.800 gerçek koşu/toparlanma karesini iki hız ve üç zemin eğiminde ölçer.

İlk testlerde bulunan kök ölçeği aktarımı, koşu patisinin zemine girmesi ve çift hareketli kalkış geçişi düzeltildi. `Native-initial.xml`, `Native-ground-initial.xml` ve ilk poz klasörü tarihsel teşhis kanıtıdır. Son doğrulama daha yeni `Native-ground-final.xml` dosyasıdır. Ek ölçümde 30 fps kalkış karesinin 64,53° hareketinin 63,24° kısmının kaynak koşu klibinden geldiği görüldü. Eğilmenin katı açı kontrolü korunur; kalkış kontrolü doğal koşunun hareketini ayrı ölçerek geçişin eklediği sıçramayı sınar. Kaynak koşu bu testi geçirmek amacıyla eklem başına kırpılmaz.

Son yan/arka kayıt [gerçek poz galerisi](QA/JOYFUL_ARCADE_2026-09-08/runner-pose/20260908-073205/index.html) ve aynı klasördeki `native-frames.csv` içindedir: 960 gerçek kare, en düşük ölçülen yol açıklığı **0.00391 m**. Turuncu kısa tüylü kedi ve Maine Coon, 7/11,9 m/sn; sekiz adet 2 saniyelik 60 fps sessiz video. Bunlar cihaz FPS ölçümü değildir.

## Son doğrulama

- Tam EditMode: **482/482**, [XML](QA/JOYFUL_ARCADE_2026-09-08/EditMode-final.xml).
- Bu geçişte **17 farklı native PlayMode testi başarılı**; tekrar eden testler tekilleştirildi. [Birleşik son sonuç](QA/JOYFUL_ARCADE_2026-09-08/native-test-summary.json), [son hareket/temas/ön izleme paketi](QA/JOYFUL_ARCADE_2026-09-08/Native-ground-final.xml), [kaydırma ve dinlenme](QA/JOYFUL_ARCADE_2026-09-08/Native-ui-final.xml).
- LevelContentValidator: **0 hata / 0 uyarı**, [kayıt](QA/JOYFUL_ARCADE_2026-09-08/validation.txt).
- Yerel görsel tur: **62/62 adım, 58 ekran çekimi**, içerik taşmayan dört alt-kaydırma adımı atlandı. 1920×1080 ve 1440×1080; son HUD/kutlama/oyun seçimi kareleri `_Final` adıyla ayrılır. Galeri son kareyi seçer. Gerçek EventSystem kaydırma testleri bu görsel turdan ayrıdır.
- Runner/Catch giriş, oyun, duraklatma ve sonuç ekranları iki oranda incelendi. Normal oyun videoları 432 gerçek kare / 24 fps / 18 saniye; ses içermez. [Medya ölçümü](QA/JOYFUL_ARCADE_2026-09-08/media-metadata.json).

## Android boyutu ve performans sınırı

[Yeni APK](../Builds/Android/CatHome-Android-Joyful-2026-09-08.apk): **197.19 MB (197,194,351 bayt)**. İlk 336,34 MB teste göre **%41.37 küçük**; önceki üretim paketine göre fark -57.29 MB. Ölçüm doğrudan APK dosyasındandır; ZIP açılmış toplamı kurulu uygulamanın cihazdaki alanı değildir.

Paket `com.vexorialabs.cathome`, sürüm 0.1.0 / code 1, yalnız ARM64. ZIP bütünlüğü ve imza doğrulandı; önceki paketle aynı sertifika kullanılır. [APK içerik/boyut karşılaştırması](QA/JOYFUL_ARCADE_2026-09-08/apk-comparison-final.json), [imza](QA/JOYFUL_ARCADE_2026-09-08/apk-signature.txt), [manifest](QA/JOYFUL_ARCADE_2026-09-08/apk-manifest.txt), [build raporu](QA/JOYFUL_ARCADE_2026-09-08/build-result.json). QA ekranları, Blender kaynakları ve çalışma kopyası APK'ya eklenmez.

Android derlemesi **0 hata ve 61 uyarıyla** tamamlandı: 50 eski API kullanım uyarısı, 5 kullanılmayan alan uyarısı, 5 URP shader uyarısı ve 1 Diagnostics/Debug Symbols ayarı uyarısı. Ayrıntılar [derleme uyarıları](QA/JOYFUL_ARCADE_2026-09-08/build-warnings.json) dosyasındadır. Validator'ın 0/0 sonucu bu ayrı derleme uyarılarını kapsamaz.

Yeni sekiz FBX toplam 1.229.664 bayt, ortak palet 232 bayttır. Yol 49.632 yerine 1.596 üçgen; yeni cepheler 75.544–80.688 yerine 8.724–9.364 üçgendir. Önceki Android optimizasyonları korunur. Küçük kedi ön izlemesi 256²/1×, orta 512²/2×; mobil en fazla 512² ve 15 fps. Azaltılmış harekette yalnız girdi/tercih değişince çizilir. Ana ışık sert gölgeli, dolgular gölgesizdir; URP mobil gölge atlası mevcut 1024 kalır. Seviye kutlamasının tam ekran CPU okuması kaldırıldı.

[adb-devices.txt](QA/JOYFUL_ARCADE_2026-09-08/adb-devices.txt) bağlı cihaz olmadığını gösterir. Samsung A8 FPS, GPU/RAM, ısınma, kurulu alan ve dokunma hissi ölçülmedi. Yeni paketin cihazda kullanıcının kontrolü bu sınırı tamamlayacaktır.

Boyut incelemesinde kalan en büyük kaynaklar ırkların model/animasyon verileri ve bazı ortak mobilya dokularıdır. [En büyük 30 kaynak](QA/JOYFUL_ARCADE_2026-09-08/build-largest-assets.json) Unity'nin içerik boyutlarını gösterir; bunlar son APK sıkıştırmasından önceki değerlerdir ve APK baytlarıyla doğrudan toplanmaz. QA videoları, Blender çalışma dosyaları ve dokümantasyon paketi şişiren içerikler arasında değildir.

## Kayıt ve kaydedilen çalışma alanı

QA `Library/UiQaSession/20260908-055521` kopyasında yürüdü. [Başlangıç](QA/JOYFUL_ARCADE_2026-09-08/save-before.json) ve [bitiş](QA/JOYFUL_ARCADE_2026-09-08/save-after.json) hem asıl kayıt hem eski yedek için eşleşir. Asıl kayıt SHA-256: `06013EDA8A16C339167EF4FF8BE285C7E45418892A65583CFD7EBAC5C11B840D`. Önceki checkpoint'teki tarihsel hash geri yüklenmez.

Unity Play ve QA kapalı; `GameScene`, `CatHome_UI`, `LivingRoom_Level01` ve tek etkin kamera düzeni kaydedildi. [Unity son durum](QA/JOYFUL_ARCADE_2026-09-08/unity-final-state.json). Açık Blender oturumunun iki nesnesi `ArtSource/Blender/Sessions/CatHome_OpenSession_2026-09-08.blend` içinde kaydedildi; yeni arcade kaynak dosyası `ArtSource/Blender/MiniGames/ArcadeWorlds_Source.blend` içindedir. [Blender kayıt kanıtı](QA/JOYFUL_ARCADE_2026-09-08/blender-session-save.json).

Bilgisayar açık bırakıldı. Kapatma otomasyonu oluşturulmadı. Geniş çalışma ağacı korundu; otomatik commit/push yapılmadı.
