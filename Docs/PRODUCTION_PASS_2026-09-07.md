# Cat Home — oyun, hareket ve sunum geçişi · 7–8 Eylül 2026

**Durum: uygulama, editör doğrulaması ve Android paketi tamamlandı.** Kullanıcının son talimatı geçerli: **bilgisayar KAPATILMADI; açık kalacak.** Unity ve Blender kaydedildi. Commit/push yapılmadı; mevcut geniş çalışma ağacı korundu.

Güncel devir: [8 Eylül checkpoint](CatHome_Checkpoint_2026-09-08.md). Görsel kanıt: [gerçek oyun galerisi](QA/PRODUCTION_PASS_2026-09-07/index.html). Önceki mobil optimizasyonun ayrıntıları [Android raporunda](ANDROID_AUDIT_2026-09-07.md); bu belge onun ardından yapılan geniş oyun geçişidir.

## Cihaz paketi ve boyut

[CatHome-Android-Production-2026-09-08.apk](../Builds/Android/CatHome-Android-Production-2026-09-08.apk): **254,487,247 bayt / 254.49 MB / 242.70 MiB**.

| Paket | Doğrudan dosya boyutu |
|---|---:|
| Eski CatHome_Test_0.1.0.apk | 336.34 MB |
| 7 Eylül optimizasyon paketi | 251.78 MB |
| Bu geçişin son paketi | 254.49 MB |

Eski test paketine göre **%24.34 daha küçük**. Son oynanış, sanat, fotoğraf ve ses eklemeleri 7 Eylül optimizasyonuna göre toplam +2.70 MB fark oluşturdu. APK ZIP bütünlüğü doğrulandı. Paket kimliği `com.vexorialabs.cathome`, sürüm `0.1.0` / kod `1`, mimari ARM64; imza önceki paketle aynı ve `apksigner verify` başarılı. Bu paket mevcut kuruluma güncelleme olarak uygulanabilir; kayıt silmeyi gerektiren yeniden kurulum önerilmez.

Bu teslim mevcut Android Debug sertifikasıyla imzalı cihaz deneme paketidir; Google Play mağaza yayını yapılmadı. `development=false` ile derlendi. Dosya adındaki Production, bu çalışma geçişini ayırt eder; mağaza imzası yerine geçmez.

- APK SHA-256: `866d7a03276ee70e222bfcee7fb4fa307ca9572243d877c618504e593fbb68d7`.
- Sertifika SHA-256: `04dba1e4d89a570617665f2afc2e0c7cffff89bd9caeb70f3503d6a8fc272f44`.
- [Boyut/ZIP karşılaştırması](QA/PRODUCTION_PASS_2026-09-07/apk-comparison-final.json), [manifest](QA/PRODUCTION_PASS_2026-09-07/apk-manifest.txt), [imza denetimi](QA/PRODUCTION_PASS_2026-09-07/apk-signature.txt), [Unity build raporu](QA/PRODUCTION_PASS_2026-09-07/build-report-final.json).

Unity build sonucu başarılı; rapordaki hata sayısı 0, uyarı sayısı 61. BuildReport toplamı sembol ve yedek dosyalarını da kapsayabilir; APK boyutu yukarıda dosyanın kendisinden ölçüldü. ZIP açılmış toplamı kurulu uygulamanın cihazda ölçülen depolama boyutu değildir.

61 uyarı incelendi: 50 eski Unity arama API'si, 5 kullanılmayan serialize alanı, 5 URP/Vulkan tamsayı modül optimizasyonu, 1 diagnostics sembol ayarı. Bunlar derlemeyi durdurmadı; validator'ın 0/0 sonucu build'in uyarısız olduğu anlamına gelmez. Sembol uyarısı çökme raporlarının çözümleme ayrıntısını sınırlar. Paket tesliminde geniş ve gereksiz API/URP kaynak değişikliği yapılmadı.

## Samsung A8 ve önceki geri bildirim

Paylaşılan üç SS ve 2.605 saniyelik `3.mp4` incelendi. Türkçe isim ekranında kalan İngilizce metin, açılış sol kenarı, bakım alanı, kapı ve ses kaynakları kod/sahne ile eşleştirildi. Video kareleri ve ses sinyali incelendi; bu kayıt doğrudan kulakla dinleme veya cihaz profili ölçümü kanıtı değildir. SM-X200 önceki Unity kurulum günlüğünde görüldü; kullanıcının kesin cihaz doğrulaması yok ve son kontrolde bağlı ADB cihazı bulunmuyor.

Önceki geçişte Runner'ın kullanılmayan kapalı dekor bağları yaklaşık 85 MB doku yükünden kurtarıldı, kullanılmayan AI inference bağımlılığı kaldırıldı. Kaynak `.blend`, Docs/QA, Library ve ham video klasörleri oyunun Assets paketine eklenmez. Eski bakım prefablarının hâlen bağlı kaynakları güvenli olmayan toplu silmeyle çıkarılmadı; ayrıntılar önceki Android raporunda.

Mobil profil: render scale .85, 2x MSAA, 1024/tek cascade/16 m gölge, yarım çözünürlüklü AO; ek SMAA kapalı. RAM ≤4 GB'de hedef 30, üstünde 60 FPS. Açılış hedefi en fazla 1600 px / 2x AA; arkasındaki görünmeyen oda çizimi mobilde durur. **Fiziksel tablette FPS, bellek, ısınma ve uzun oturum ölçümü yapılmadı; akıcılık garantisi verilmez.** Yeni APK bu cihaz kontrolü için hazırdır.

## Bakım istasyonu ve oda düzenleri

Mama/su tek seramik tepsiye, önünde açık koridor olacak şekilde yerleştirildi. Tepsi ve arkalık kaplar arası/arka sıkışma ceplerini fiziksel olarak kapatır. Mama `(0.30, .06, 2.35)`, su `(.98, .06, 2.35)`; yaklaşımlar aynı X ve `z=1.72`. Tepsi `( .64, 0, 2.465 )`, açık çıkış `(.64, .05, 1.68)`. Berjer ölçeği korunarak `(2.18,0,1.95)` / 50° oldu.

`CatCareStationObstacle`, yalnız eski uyanık kayıt istasyonun genişletilmiş katı alanına denk geliyorsa açık öne taşır. Geçerli konumlar ve gerçek yatakta uyku korunur. `BowlInteraction` kap/görünürlük/oda/mesafe/yol denetimini tıklamada tekrar yapar; gerçek yürüyüşle yaklaşır, ulaşamazsa zaman aşımıyla kontrolü bırakır. Başlık, konuşma ve modal arkasında bakım düğmesi çalışmaz. Ses yalnız gerçek yemek/içme evresinde başlar.

Sekiz odanın tüm ROOM ürünleri ortak sabit katalog planlarını kullanır. **Satın alma/sahiplik korunur; tüm mobilyalar oyuncuya bedava verilmedi.** Sahip olunan ürün aynı belirli yere yerleşir. CAT yalnız salonda, en fazla beş görünür eşya / bir yatak. Hayalet mama/su/yatak bulunan boş odalarda bakım eylemi açılmaz.

Berjer değişince FloorLamp'ın eski girişinin kapanması gerçek testte bulundu; giriş `(2.65,0,1.20)` açık yürüyüş hattına alındı. Son içerik üretiminden sonra `StoreCatalogPreviewBuilder.BuildAll()` ve `RoomPreviewCaptureBuilder.CaptureSilently()` çalıştı: 103 kanonik ürün kartı ve sekiz oda fotoğrafı yenilendi. Dosya klasöründeki 116 fotoğrafın 13'ü tarihsel/katalog dışıdır; hepsinin oyuna yeni ürün olarak eklendiği söylenmez.

## Analog ve kalıcı dinlenme

Analog ölü bölgesi .08; en hafif yürüyüş **.65 m/sn normal adım**. İhtiyaç çarpanları bunu ağır çekime indirmez. Yürüyüş dönüşü 220°/sn, koşu 320°/sn; 60° üzerindeki değişimde önce kontrollü dönme, sonra ileri yürüme kullanılır. Animasyon gerçek zemin mesafesine bağlıdır; duvara dayanıp hareket sıfır olduğunda sönüm artığı bırakılmadan durur. İkinci parmak analog sahipliğini alamaz; odak kaybı girdiyi temizler.

Koltuk, minder, CAT yatakları, güneşlenme, havlu/örtü ve salıncak gibi dinlenmeler kullanıcı **Kalk** diyene kadar sürer. `CatActivity` dinlenme evresini ayrı izler; çıkış normal iniş/kalkış animasyonunu tamamlar ve hareket kilidini bırakır. Enerji dinlenirken +.75/sn; eski bitişte toplu ödül yok. Dinlenme başlatmanın enerji bedeli sıfırdır; dolu enerji engel değildir. Ana bakım yatağının **Uyan** akışı korunur.

Yakın eşya düğmesinde ürün adı + kısa **Kalk**, Birlikte panelinde **Kalk · Dinlenmeyi bitir** vardır. Uzun yazının dar HUD alanına sıkışması son görsel kontrolde düzeltildi.

## Kedi komutları ve sesler

`Birlikte` panelinde **Miyavla / Otur / Loaf**: gerçek kedi fotoğrafları, açık durum, bakım yüzdeleri ve rehber sekmesi. Otur/Loaf kullanıcı bitirene kadar sürer. Fiziksel aktivite sırasında yeni komut düğmeleri devre dışıdır; dinlenmeyi bitirme çalışır. Komutlar enerji/para tüketmez ve görev ilerlemesi üretmez. `CatCommandActivity.Kind=CompanionCommand` (append-only 102); varsayılan BallChase kimliği kullanılmaz.

Loaf gerçek eklemlerden üretilir, kök Y ölçeği sıkıştırılmaz. Oturma/kalkma geçişleri vardır. On ırkta bekleme, fiziksel alan ve çıkış kontrol edildi. Miyav yaklaşık dört saniyelik klip ömrünü izler, üst üste yığılmaz.

`CatVoice`, iki ses kaynağıyla miyav ve eylem döngülerini yönetir. Mama, su, uyku/dinlenme mırlaması gerçek evreye bağlıdır; iptal/duraklatma/ses kapatma/mini oyun/açılış geçişlerinde biter. Elektronik sürekli döngü ve dokunma plop'u kaldırılmış olarak kalır. Eski sentetik mama/su/mırlama kalıpları çağrılmaz; anlamlı ödül/alışveriş kısa sesleri korunur.

Dört doğal SFX CassetteAI/fal ile üretildi; kaynaklar `ArtSource/Audio/GeneratedOriginals`, işleme `ArtSource/Audio/prepare_cat_audio.py`. Mono PCM 24 kHz, yumuşak uç/döngü geçişleri; dört WAV toplam 1.315.376 bayt. Unity Vorbis .65 / 24 kHz uygular. WAV toplamı APK artışıyla aynı ölçü değildir. Galeride sesler ayrıca oynatılabilir; sessiz QA videolarının ses içerdiği iddia edilmez.

## Runner ve Catch

Runner eğilmesinde gerçek .033 m taş yüzeyi ve platform eğrisi animasyon pozu sonrasında ölçülür. On ırkın ayak/gövde açıklığı korunur; Y ezme yok, eğilmede en fazla 2° yatış. Mesh/List tekrar kullanılır, temas düzeltmesi yalnız aktif eğilen kediye uygulanır. Irk değişiminde eski zemin ofseti yeni modele taşınmaz. Şerit geçişi, zıplama/iniş ve gerçek rampalarda eğilme native testten geçti.

Boulevard'ın özgün sokak görünümü korundu; her üçüncü blokta festival kordonu eklendi. Ek sahne kamerası/ışığı yok. Eski neon/mor rampa dönmedi. Son gerçek oyun fotoğrafı hem Runner kartına hem kanonik `CatRunnerHero_v1.png` dosyasına işlendi.

Catch yönlenme sınırı 28°, dönüş en fazla 300°/sn; sert yön değişiminde yerde dönme, sonra ileri koşu. Öngörülen atılma noktası da hizalanma ister. Hazırlık sonrası havada yön sabit, bir temas tek fare; kaçırma seriyi bitirir. Yeni tur atılma/başarı sayaçlarını temizler. HUD takip/atılma/yakalama/kaçırma durumunu anlatır.

Catch'e renkli duvar kemeri, kumaş flamalar ve oyun alanının dışına raflar eklendi; oyun hareket alanı korunur. Gerçek hazır sahneden ön izleme alındı. `CatCatchContentBuilder.BakeWelcomeHero` mevcut canlı fotoğrafı eksik ilk editör karesiyle ezmez; sanat değiştiğinde `MiniGamePreviewBuilder.CaptureLive` ile yeni fotoğraf ve import gerekir.

## Ana menü, pop-up ve ilk kullanım

Ana menü bağ kurma / yuva / oyun bölümlerini üç boyutlu ikonlarla anlatır. Yeni oyun onayı seçili kedi fotoğrafı, **sıfırlanacaklar / korunacaklar** kartları ve açık eylemler sunar; QA'da gerçek kayıt sıfırlanmadı. Koleksiyon, yuva seviyesi ve ilk gün kutlamaları fotoğraf/karakter sahnesi, ödül tepsisi ve belirgin devam düğmeleriyle yeniden düzenlendi. Koleksiyon metni doğru odanın `10/10` değerini kullanır.

Tanışma dört adımda ilerleme ve açık hareket/bakım açıklamaları gösterir. İsim ekranı ve 52 kedi tepkisi Türkçeleştirildi. `Birlikte > Oyun rehberi` altı kaydırılabilir bölüm sunar: hareket; bakım; mağaza/odalar; komutlar; mini oyunlar; kayıt/isteğe bağlı ödüller. Son sayfa ve kaydırma dipleri dahil 16:9 ile 4:3 ölçülerinde kontrol edildi.

Ivory/mint/coral dili, Fredoka/Nunito fontları, Türkçe pişmiş atlaslar ve kanonik coin sanatı korunur. Rehber metni taşmadan kayar; başlık/sekme boşlukları ve fotoğraf oranları son görsel geçişte düzeltildi. Dinlenme yokken panelin alt alanı Tokluk/Su/Enerji yüzdelerini gösterir.

## Doğrulama ve kanıt sınırları

| Kontrol | Son sonuç |
|---|---:|
| Tam EditMode | 470/470 |
| Bu geçişteki benzersiz native PlayMode testlerinin son sonuçları | 38/38 |
| ROOM ürün × ırk: sekiz oda | 800/800 başlama, bitiş, açık çıkış |
| CAT ürün × ırk | 170/170 rutin |
| LevelContentValidator | 0 hata / 0 uyarı |

38 sayısı tek bir koşunun test sayısı değildir; [native-test-summary.json](QA/PRODUCTION_PASS_2026-09-07/native-test-summary.json) her testin en son sonucunu birleştirir. İlk başarısız kayıtlar silinmedi: analog duvar animasyonu ve FloorLamp girişi düzeltilerek son hedefli tekrarlarda geçti. [EditMode-final.xml](QA/PRODUCTION_PASS_2026-09-07/EditMode-final.xml), [oda matrisi](QA/PRODUCTION_PASS_2026-09-07/room-matrix-summary.json), [validator](QA/PRODUCTION_PASS_2026-09-07/validation-final.txt) esas alınır.

Gerçek düğme dinleyicisiyle Loaf başlatma, meşgul komutları kapatma, Kalk'a basma ve kontrolü bırakma ayrıca denetlendi. 1920×1080 ve 1440×1080 ekranlar; ana menü, pop-up, tanışma, bütün rehber sayfaları, komutlar, iki oyunun hoş geldin/pause/sonuçları galeride. Açık menü arkasındaki HUD düğmelerinin Scrim nedeniyle tıklanamaması beklenen davranıştır; bu tarihsel tarama satırları ön panelde bozuk düğme olarak yorumlanmaz.

Runner, Runner kontrol turu ve Catch videoları 18'er saniye / 432 gerçek kare; koltuk kaydı 310 kare / yaklaşık 12.92 saniye. Bunlar sabit simülasyon adımıyla 24 kare/sn alınmış **sessiz editör görüntüleridir**. Mobil akıcılık ölçümü değildir. Catch kayıt penceresinde altı yakalama, ardından tur sonu ayrıca görüntülendi.

## Kayıtlar ve kaynak teslimi

QA yalnız ayrı kayıt kopyasında çalıştı; gerçek hesap/cloud kaydı kullanılmadı. Başlangıç/bitiş SHA-256 eşleşiyor: ana kayıt `514F4623FD4A9BD49DA528444F6557BB7502B01CE35B6FA3E6B6D781C6042D7D`; eski CP2 yedeği de değişmedi. Bu geçişin başlangıcı [save-before.json](QA/PRODUCTION_PASS_2026-09-07/save-before.json), bitişi [save-after.json](QA/PRODUCTION_PASS_2026-09-07/save-after.json). Önceki günün farklı baseline'ı geri yükleme gerekçesi değildir.

Unity son durumda Play kapalı, QA oturumu kapalı; `GameScene`, `CatHome_UI`, `LivingRoom_Level01`, tek etkin kamera. Sahne ve varlıklar kaydedildi; [son çalışma alanı](QA/PRODUCTION_PASS_2026-09-07/workspace-final.json).

Blender açık oturumu `ArtSource/Blender/Sessions/CatHome_OpenSession_2026-09-08.blend` olarak kaydedildi. Yeni model kaynakları `ArtSource/Blender/PremiumFurniture/CareStationTray_Source.blend` ve `ArtSource/Blender/MiniGames/PlayfulPass_Source.blend`; üretici betikleri yanlarında. Model üretimi headless yapıldı; kullanıcının açık iki nesneli oturumu değiştirilmeden kaydedildi. **Bilgisayar açık, kapanış otomasyonu yok.**
