# Cat Home — modern görünüm uygulaması

8 Eylül 2026. Kullanıcının “polish, butonlar vs çok güzel… Unity ve Blender… tasarla” talimatıyla tasarım önerisinin uygulamasına geçildi. Önceki onay bekleme koşulu bu açık talimatla karşılandı. APK, commit/push veya bilgisayar kapatma işlemi bu çalışmanın parçası değildir.

[Gerçek Unity görüntüleri ve videolar](QA/MODERN_POLISH_2026-09-08/index.html) · [İlk mockup ve düzenleme planı](DesignProposals/2026-09-08_ModernCatHome/PLAN.md)

## Görünümde değişenler

- Ortak yüzeyler soğuk beyaz ve açık maviye, ana eylemler azure maviye geçti. Bilgi kutusu, seçili sekme, ikincil eylem ve ana buton farklı ağırlık taşıyor. Sürekli buton parlaması kaldırıldı; hareket dokunma, odak ve basma durumlarına bağlı.
- HUD ihtiyaçları, cüzdan, kimlik ve dört bölümlü alt gezinme ayrı gruplar oldu. 80 referans px alan ve SafeArea korunuyor. Oda adı 18–22 punto aralığında tek satıra sığıyor. Alt bölgenin opak, girdi almayan zemini diyalog geçişindeki eski görüntü izlerini önlüyor.
- Mağazada ürün fotoğrafları büyüdü; isim, sahiplik ve eylem alanları ayrıldı. Sekmeler gerçek içerikle aynı hiyerarşide, kaydırma yüzeyinin üstünde duruyor. Mağaza, oda, kedi, Birlikte, rehber, görev, ayar, diyalog ve oyun panelleri ortak görsel rolleri kullanıyor.
- Konuşma ekranı kısa alt panele dönüştü. Gerçek kedi portresi, konuşmacı, metin ve “Devam” eylemi tek bakışta okunuyor. Konuşma balonları ve isim kartı aynı aileye bağlandı.
- Tüy rengi düğmeleri ortak eylem mavisine dönüşmez; sekiz gerçek palet tonunu gösterir. Görev/günlük görev satırları her yenilemede aynı beyaz yüzey, lacivert metin ve mavi eylem stilini kullanır. Tanışma butonundaki eski yarı saydam renk geçişi temizlenir; panelin açılış/kapanış saydamlığı korunur.
- Seviye ve tanışma kutlamaları bir kısa açılıştan sonra sabit kalıyor. Sonsuz konfeti, sürekli zıplatma ve kedi resmini tekrar tekrar ezme döngüsü yok. Azaltılmış hareket seçeneği açılış/kapanış sırasında da dikkate alınıyor. Ödül hesabı ve işlem kilitleri aynı.
- Oda, katalog, açılış ve iki arcade dünyası için Blender'da sekiz yüzey ailesi hazırlandı. Ahşap, kumaş, taş, seramik, boya, yaprak, örgü ve süet malzemeleri ayrı ışık tepkisi taşıyor. Gerçek kamera çekiminde görülen fazla damar/çim çizgileri azaltıldı; cihaz gövdeleri, şemsiye ve hamak için dar malzeme düzeltmeleri yapıldı.
- On kedi ırkı aynı modern mesh/malzeme eşleme kaynağını kullanıyor. Yalnız ışık hesabındaki normaller ve malzeme tepkisi düzenlendi; mevcut köşeli silüet ve ırk kimliği korunuyor. Bu uygulama yeni bir kedi modeli veya mockuptaki ince tüylerin gerçek zamanlı karşılığı değildir.
- On ırk için 20 şeffaf portre, üç gerçek komut pozunun 30 fotoğrafı, 103 kanonik ürün ve sekiz oda fotoğrafı yenilendi. Portrelerde yalnız çene/göz kapağı kemikleri fotoğraf için ırkın kendi nötr yerel konum/dönüş/ölçeğine alınır; oyun animasyonları değişmez. Oda fotoğrafları da geçici modern kedi malzemelerini kullanır ve asıl sahne bağlarını geri yükler.

## Kaynaklar ve yeniden üretim

| Alan | Kaynak / işlem |
|---|---|
| Ortak UI | `ModernUiArt`, `ModernScreenBuilder`, `PremiumButtonFx`, `LowPolyPanelGraphic` |
| Bütün ekranları güncelleme | `PremiumTypographyBuilder.ApplyToAllScreens()`; UI/Runner/Catch yanında sekiz odanın yalnız dış Canvas kökleri de işlenir |
| Kedi yüzeyi | `CatModernVisualBuilder.Build()`; kaynak/vendor meshleri değiştirilmez |
| Kedi portreleri | Son UI üretiminden **sonra** `CatModernVisualBuilder.BuildPortraits()` |
| Komut fotoğrafları | `CompanionPreviewBuilder.Build()` |
| Dünya yüzeyleri | `ArtSource/Blender/ModernPolish/build_modern_surfaces.py`, `ModernSurfaces_Source.blend`, `ModernWorldArtBuilder` |
| Başlık sahnesi | `ModernWorldArtBuilder.ApplyTitleStage()` ve `TitleShowcaseContentBuilder.CapturePoster()` |
| Ürün / oda fotoğrafları | `StoreCatalogPreviewBuilder.BuildAll()`, `RoomPreviewCaptureBuilder.CaptureSilently()` |
| Mini oyun fotoğrafları | Hazır, izole gerçek Play'den `MiniGamePreviewBuilder.CaptureLive`; ardından zorunlu import ve Runner kanonik hero güncellemesi |

Modern yüzey adları önceki üreticilere özgün malzeme gibi verilmez. Kaynak yol/ad bilgisi çözülür; tekrar üretimde yeni bir Modern-from-Modern zinciri kurulmaz. Runner'ın geri dönüştürülen parçalarının tema dizileri de modern malzemeleri tutar. Dünya üreticilerinin sonunda aynı malzeme adımı vardır. Türkçe önceden pişirilmiş font atlasları ve build sırasında temizlenmeme ayarı korunur.

## Korunan oyun sözleşmeleri

Sekiz odanın sabit planı, salonun beş CAT/bir yatak sınırı, kamera konumu ve bakışı, eşyaların ölçeği, fiziksel giriş/çıkışlar, kapı ve bakım engelleri değişmedi. Runner yaklaşma/atlama/eğilme ve zemin temas kuralları; Catch hazırlık, gerçek pati teması ve toparlanma davranışı korunuyor. Otur/Loaf'ın yalnız tutulan dinlenme evresindeki +.35 enerji/sn ve mobilyanın +.75 enerji/sn sözleşmeleri aynı.

Gerçek kayıtla oyun testi yapılmaz. `ModernPolishQa` JSON/recovery kopyasını kullanır ve sunum tercihlerini mevcut olma bilgisiyle birlikte geri yükler. Gerçek hesap girişi/silme/satın alma yapılmaz. Sıralama ve ödül örnekleri sunum testidir; sunucudan alınmış yeni sonuç veya yeni ekonomi kuralı değildir.

## Kanıt ve teknik sınırlar

Tam EditMode **488/488**, seçilmiş gerçek native PlayMode **19/19**, mimari doğrulama **0 hata / 0 uyarı** ile geçti. Dünya karşılaştırması, 118 dosyadaki 5.918 fiziksel transform/mesh/collider/kamera/ışık kaydını denetler; UI RectTransform düzenlemeleri bu fiziksel değişmezlik kontrolünün dışındadır.

Son buton ve renk örneği düzeltmelerinden sonra aynı native grubun dört UI kontrolü **4/4** tekrar geçti; benzersiz native test sayısı yine 19'dur. Sekiz canlı tüy rengi paletle tek tek eşleştirildi. Son görev satırı değişikliği renk/yüzey kapsamındadır ve iki ekran oranında görsel olarak kontrol edildi; bu son kozmetik adımdan sonra bütün 488 test tekrar çalıştırılmış değildir.

Teslim galerisi **265 PNG ve iki sessiz MP4** içerir: 58 ana ekran/kaydırma durumu, 28 ek menü durumu, 18 mini oyun karesi, sekiz oda, 20 kedi portresi, 30 komut pozu ve 103 katalog fotoğrafı. Runner/Catch videolarının her biri gerçek 432 ardışık kareden, 1920×1080 / 24 fps / 18 saniye olarak kodlandı; çıktı videoları tekrar çözülerek kare sayıları doğrulandı. 16:9 ve 4:3 UI kontrollerinde ekran dışına taşan veya çakışan birincil kontrol görülmedi. Modal arkasında kalan bakım/HUD düğmelerinin bloke olması beklenen davranıştır.

Son sonuçlar [EditMode JSON](QA/MODERN_POLISH_2026-09-08/EditMode-final.json), [native PlayMode JSON](QA/MODERN_POLISH_2026-09-08/PlayMode-final.json), [mimari doğrulama](QA/MODERN_POLISH_2026-09-08/validator-final.txt), [fiziksel kayıt karşılaştırması](QA/MODERN_POLISH_2026-09-08/final-world-preservation.json) ve [asıl kayıt karşılaştırması](QA/MODERN_POLISH_2026-09-08/save-final.json) içindedir. İlk başarısız test turu ayrı tutulur; sonuç olarak sunulmaz.

Son karşılaştırmada 5.918 korunan kaydın tamamı eşleşti, ek fizik nesnesi yok. Asıl kayıt, recovery ve eski yedek SHA-256/uzunlukları değişmedi. 16 sunum tercihi mevcut olma bilgisiyle tam geri yüklendi; QA ve Play kapatıldı. GameScene, CatHome_UI ve LivingRoom_Level01 kaydedilmiş durumda, tek etkin kamera var. Çekimdeki tam viewport, son sahne kaydından önce 80 px dock alanına geri alındı. [Son Unity durumu](QA/MODERN_POLISH_2026-09-08/final-state.json).

Blender yüzey PNG kaynakları toplam 2.898.228 bayttır. Sekiz adet 512² ETC2 RGB4 harita ve mip zincirinin hesaplanan üst sınırı yaklaşık 1,33 MiB'dir; bu ölçü gerçek cihaz RAM/FPS ölçümü değildir. Şeffaf kedi portreleri ayrı olarak ETC2 RGBA8 kullanır. Yeni shader mevcut Unity URP Forward/Forward+ yoluna dayanır. Fiziksel tablette GPU, RAM, ısı ve kare hızı ölçülmedi; Android paket üretilmedi. Video kareleri gerçek oyun görüntüleridir, 24 fps kayıt ayarı cihazın ulaşacağı FPS anlamına gelmez.
