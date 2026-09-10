# Cat Home — uygulanan UI/UX tasarımı

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

Güncelleme: Kullanıcının bildirdiği gerçek tıklama kusuru ve görsel kalite farkları [ikinci kalite geçişinde](UIUX_REFINEMENT_2026-09-06.md) düzeltildi. Aşağıdaki metin ilk uygulamanın tarihsel kaydıdır; son font ve yüzey kararları yeni belgede bulunur.

6 Eylül 2026. Onaylanan altı referans ekranın dili, önceki incelemedeki oyuncu ekranlarının tamamına uygulandı. Bu belge uygulama ve doğrulama kaydıdır.

[Gerçek oyun görüntüleri](QA/UIUX_2026-09-06/index.html) · [Onaylanan referanslar](UIUX_References_2026-09-06/index.html) · [Önceki arayüz](QA/UIUX_2026-09-05/index.html)

## Tasarım sistemi

Sıcak ivory (#FFF9EF), mint (#DDEDE3), turkuaz seçim (#218F87), mercan ana eylem (#F5786C), koyu ink yazı (#243536). Fredoka, ince kenarlar, ölçülü iç parlaklık, tutarlı boşluk ve cümle düzeni. Süsleme yerine kedi, oda ve ürün öne çıkar. Seçim/basılma/geçiş hareketleri korunur; sürekli parlayan ve sallanan düğmeler kaldırıldı. Hareket azaltma aynı son düzeni korur.

LowPolyPanelGraphic, PremiumUiFactory, PremiumButtonFx ve PremiumAmbientSparkle ortak altyapıdır. Kullanıcının yeni kararı eski neon/rainbow ve büyük candy çerçeve şartlarını geçersiz kılar; çalışma alanı kuralları güncellendi.

## Tamamlanan ekranlar

| Alan | Uygulanan deneyim |
|---|---|
| Başlangıç | Gerçek üç oyun kedisi ve mevcut mobilyalarla canlı HD sahne; belirgin Devam et, küçük Kedim/Odalar/Oyunlar; yapımcılar, yeni oyun ve hesap seçimi |
| Ev | Gerçek seçili portre/isim/seviye; kompakt ihtiyaç/para; sade mağaza/oda/oyun dock'u; yakındaki bakım eylemi; konuşma, etkinlik ilerlemesi ve kısa ödül bildirimi |
| Mağaza | Kedi eşyaları/Mobilyalar/Odalar; dört sütunlu gerçek ürün fotoğrafları; sahiplik, tam fiyatlar, otomatik yerleşim bilgisi; sahip olunan odaya ziyaret |
| Satın alma | Büyük gerçek ürün; istenen/gerekli ürün ilişkisi; jeton/elmas; ayrı elmas onayı; 10/20/50/100/500/1000 paketleri ve sağlayıcı yok durumu |
| Kedim | İsim, renk ve on ırk tek akışta; büyük canlı önizleme, gerçek portreler; onayda uygula, kapatmada taslağı iptal et |
| Odalar | Sekiz gerçek oda; iki sütun, 480×270 (16:9) fotoğraf; mevcut/kilitli/sahip olunan durum; kaydırmalı gezinme |
| İlerleme | Bölüm/günlük görevleri; ilerleme ve ödül; bakım odaklı geri dönüş; yuva seviyesi, koleksiyon ve ilk gün kutlamaları |
| Hesap ve yardım | Ses/erişilebilirlik/dil/hesap grupları; hesap bağlantısı ile bulut durumunun ayrımı; açık veri silme onayı; tanışma ve isim girişi |
| Oyunlar | Gerçek oyun fotoğrafları; sıralamada dolu/boş/çevrimdışı durum ve oyuncunun kendi satırı |
| Runner ve Catch | Gerçek seçili kediyle giriş; rekor/can/başlat; HUD, öğretici, duraklatma, sonuç, can bitmesi ve doğrulanmış reklam alternatifi |

## Netlik ve oranlar

- UI vektörel/TMP çizilir. Ürün fotoğrafları 1024; oda ve mini oyun fotoğrafları 1920×1080; açılış en az Full HD, ekran oranına göre en çok 3840×2160.
- Native render ölçeği, 4× MSAA, yüksek SMAA, dithering ve ölçülü oda ışık profili korunur. Mobil gölge 2048/2, PC 4096/4 cascade.
- Galeride 81 gerçek Game görüntüsü bulunur: 1920×1080, 1440×1080 ve 2400×1080. Düşük çözünürlüklü görüntüler büyütülmedi. Runner/Catch girişleri üç oranda; tablet ödül bildirimi ihtiyaçların altındaki ayrı sırada doğrulandı.
- Dar SafeArea'da ihtiyaçlar ikinci sıraya geçer; 244 genişlik/276 merkez aralığı. Mağaza 1720×930 kompozisyon olarak ölçeklenir.
- Türkçe ve İngilizce ana akışlar görsel olarak karşılaştırıldı. Ürün/oda/görev/eylem kopyaları ortak sözlüklere taşındı.

## Görsel kontrolde düzeltilen kusurlar

- Aynı kameraya giren ikinci kedi: önizlemeler ayrı sahne yerleri kullanır; kapanan model aynı karede pasifleştirilir.
- Shop arka yüzeyinin kartları kapatması: yeni içerik kapsayıcısında çizim sırası korundu ve regresyon testi eklendi.
- Pasif eski MainPanel kopyasına bağlama: etkin UI kopyası da bağlanır, başlangıçta kendi referanslarını çözer.
- Mini oyun/diyalog üzerinde ev düğmeleri: ortak ev kontrol kapısı alttaki eylemleri gizler.
- Eksik Unity CanvasGroup wrapper'ları: null-coalescing yerine Unity'nin null kontrolü ve aynı Canvas içinde yeniden bağlama.
- Konuşma balonu kapanan title yerine isimli ana Canvas'a bağlanır. Başlangıçta pasif etkinlik ilerleme kartı gösterilirken etkinleştirilir.
- Can yokken başlat/reklam eylemleri üst üste görünmez. Onay katmanları arkadaki düğmeleri fare/dokunma/klavye için kapatır.
- Tablet mağaza başlığı kart içinde kalır; ihtiyaçlar para alanına girmez. İngilizce dil seçimi tek satırdır.
- Runner jeton geri bildirimi açık yüzeyde beyaz/sarıya dönmez; ink/turkuaz darbesi kullanır. Catch süre sayacı normalde ink, kritik sürede koyu kırmızıdır.

## Doğrulama

- Son tam **EditMode: 405/405**, 0 başarısız/atlanan; 9,01 saniye. Rapor: QA/UIUX_2026-09-06/EditMode_Final.xml.
- Native Test Runner ilk tam **PlayMode: 56/57**. Tek MainPanel bağlama kusuru düzeltildi; başarısız test **1/1** tekrar geçti. Yeni önizleme regresyonu ayrıca **1/1** geçti. Böylece bu çalışmadaki **58 ayrı oyun testinin tamamı başarılı**. Son değişikliklerden sonra ev akışı ayrıca **5/5** doğrulandı.
- Raporlar: PlayMode_Full_Initial.xml, PlayMode_SmokeFix.xml, PlayMode_PreviewIsolation.xml, PlayMode_FinalSmoke.xml. Sonuçlar tek bir hayali tam koşu gibi birleştirilmedi.
- Tam oyun koşusu 80 eşya × 10 ırk matrisinin **800/800** örneğini içerir. Etkileşim/geometri bu UI çalışmasında yeniden yazılmadı.
- Son LevelContentValidator: 0 hata / 0 uyarı. Normal Play konsolu temiz; 3 sahne, 1 etkin kamera/listener/EventSystem. QA kopyası kapatıldı, ana üçlü düzen ve LivingRoom_Level01 aktif sahnesi geri yüklendi; Edit Mode Game görünümünde oda ve HUD gözle doğrulandı. Kayıtlar: FinalLiveState.txt / FinalWorkspace.txt.
- Her galeri karesinin yanındaki *.layout.txt, görünür düğmelerin GetWorldCorners kaydıdır; 81 kayıtta 0 çakışma / 0 SafeArea dışı düğme. Tablet başlığı ve ihtiyaç/para ayrışması ayrıca gözle incelendi. Validator, normal Play ve kanonik sahne geri yüklemesi FinalLiveState / FinalWorkspace kayıtlarındadır.

## Korunan sözleşmeler

Sabit ROOM yerleşimi ve eski taşınmış/saklanmış ürün göçü; CAT eşyalarının eski yerleşimi; kitaplık/kitap ve TV ünitesi/TV ön koşulları; iki para birimi ve mevcut **FREE TEST** korunur. Ekonomi açılmadı. Mini oyun karşılama ekranını açmak can harcamaz. Ödül/IAP/reklam mevcut doğrulama yollarını kullanır.

QA, Library/UiQaSession altında oyuncu kaydının ayrı kopyasıyla çalıştı; cloud sync ve çevrimiçi skor gönderimi engellendi. Örnek satın alma, sıralama, geri dönüş ve kutlama görselleri veri/ödül/onay işlemi çalıştırmadan üretildi ve galeride işaretlendi. İngilizce kontrolü kaydedilmiş dil tercihini değiştirmedi. Git commit/push yapılmadı.

## Yayın öncesi cihaz kapısı

Fiziksel Android telefon/tablet performansı, gerçek çentik ve ekran klavyesi, platform hesabı/IAP/reklam sağlayıcısı uçtan uca doğrulaması ve oyuncu kullanılabilirlik denemesi bu masaüstü çalışma kapsamında yapılmadı. Bunlar mevcut yayın kapısında kalır; Unity görüntüleri bu kontrollerin yerine geçmez.


