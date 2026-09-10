# Salon otomatik yerleşim ve etkileşim geçişi — 6 Eylül 2026

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

Kullanıcının son kararı: CAT ürünleri de otomatik yerleşir. Önceki serbest sürükleme/çevirme tercihi artık geçerli değildir. En fazla beş görünür CAT ürünü ve bunlardan en fazla bir yatak sınırı korunur. Satın alma sahipliği kalıcıdır; mağazada Kaldır / Odaya ekle ile koleksiyon değiştirilir.

## Uygulama

- `CatRoomArrangement` aynı ürün grubu için satın alma sırasından bağımsız, belirli bir düzen üretir. Satın alınmamış ROOM mobilyalarının gelecekteki alanı, bakım yaklaşım noktaları, tünel çıkışı ve oyuncak girişleri korunur. Eski oyuncak koordinatları oda açılışında güncellenir. Yol kontrolü, kedi çapını içeren bağlı bir zemin alanı kullanır.
- Berjer kataloğu 1.05 × 0.79 m ve 0.84 m yüksekliğe indirildi; gerçek FBX mevcut malzemeler korunarak yeniden ölçeklendi. Mama ve su kapları sağdaki bakım alanına taşındı. Berjerin oturma teması gerçek üçgenlerden yeniden ölçüldü.
- Top, ancak gerçek pati teması sonrasında yuvarlanır. Uzaktan yakınlık ile yakalama ve kendiliğinden havaya fırlama kaldırıldı. Kedi koklama, sol/sağ pati, takip ve oturma hareketlerini kullanır; yuvarlanma yolu engellere göre seçilir.
- Sabit ikili koltuk ve sehpaya iki bağımsız aktivite eklendi (`CatActivityKind` 100/101). Kedi zıplar, yüzeye oturur ve ayrılmış noktadan yere iner. Koltuğa sol minderin önünden yaklaşır; iki minder arasındaki alçak birleşim yüzeyi kullanılmaz. Sehpadaki mint parça gerçek pati temasıyla kenara itilir, düşüp seker. Koltukta dinlenirken enerji yavaşça artar.
- Kedi kimliği 260 × 88; pembe bağ göstergesi üst barın sol grubunda 160 × 84. Dar oranlarda ihtiyaçlar ikinci sıraya geçer.
- Yedi arayüz simgesi headless Blender ile görünür geometriye göre yeniden ortalandı. Özellikle controller alt tarafa kaymıyor; ay/yıldızlar kırpılmıyor. Alt dock ve açılış kısayollarında tematik fonlar, ortak ikon yuvaları ve düzgün boşluklar kullanılıyor.

## Kontrol durumu

- 4.147 izin verilen beşli CAT kombinasyonu için otomatik yerleşim ve bağlı yollar: geçti.
- Oyunda eski koordinatların değiştirilmesi, mama/su ve koltuk/sehpa yaklaşımları: geçti.
- Tam EditMode: **436/436**. Otomatik düzen matrisi, minderin tüm temas alanı, eski sürükleme yolunun kapanması, kompakt HUD ve tematik simgeler dahil.
- Son native sonuçlar birlikte **8 farklı başarılı PlayMode testi** içerir. 15 yeni CAT ürününün 150 rutini, top/tırmalamanın 20 rutini ve koltuk/sehpanın 20 rutini: **190 ürün/ırk rutini**. Ayrıca on ırkın tünel içindeki gerçek pozlanmış gövde/kuyruk taraması, sıfır enerjide dinlenme ve iptal/sahiplik kontrolleri geçti.
- Tam mobilyalı odada beş CAT ile ek canlı top kontrolü: 3 gerçek vuruş, toplam 1.95 m yuvarlanma, son temas aralığı yaklaşık 0.015 m, açık zeminde bitiş ve hareket kontrolünün geri verilmesi.
- UI taraması 29 ekranı ve görünür düğmelerin EventSystem alıcılarını ölçer. Konuşma / isim pencerelerinin arkasındaki aktivite düğmesi ortak modal kapısına bağlandı. Menü açıkken arka plan kontrollerinin Scrim tarafından engellenmesi beklenen davranıştır.
- Son 1920×1080 ve 1440×1080 HUD taramaları: sıfır düğme çakışması / taşma. 4:3'te ihtiyaçlar ikinci sıraya geçer. Oyunlar düğmesinin gerçek EventSystem hedefi üzerinden pointer-click gönderimi oyun seçiciyi açtı. Galeride 37 özgün oyun karesi bulunur.
- 103 mağaza fotoğrafı gerçek son prefablardan yeniden çekildi; toplu görselde hepsi incelendi. Sekiz oda ön izlemesi de yenilendi. HD kareler konsept veya mockup değildir; Unity oyun çıktısıdır.
- Son düzenleme görünümü: GameScene + CatHome_UI + LivingRoom_Level01, aktif LivingRoom_Level01, bir etkin kamera; LevelContentValidator sıfır hata / sıfır uyarı. Test kopyası kapatıldı. Mağaza ve Oyunlar düğmeleri EventSystem pointer-click kontrolünde kendi pencerelerini açtı.
- Canlı kontrol yalnız `UiQaTestSession` kayıt kopyasında yapılır. Gerçek satın alma, reklam veya ödül talebi kullanılmaz.

Kanıt dizini: `Docs/QA/LIVING_2026-09-06_Automatic`. İlk başarısız test denemeleri teşhis kaydı olarak saklanır; son sonuç olarak sunulmaz.

Sonuç dosyaları: `EditMode_Final.xml`, `PlayMode_Furniture_Final.xml`, `PlayMode_CAT_Rechecks.xml`. `PlayMode_CAT_First.xml` içindeki üç başarılı ve sonrasında değişmeyen test (150 rutin, dinlenme, farklı pati hareketleri) son kapsamın kalan üç testidir; o dosyanın başarısız testleri son başarı sayısına katılmaz. 4.147 düzen için geometri/yol kontrolü yapıldı; her düzenin bütün animasyonları ayrı ayrı oynatılmış değildir.

Gerçek telefon/tablet performansı ve yerel fare/dokunmatik uçtan uca girdi kontrolü bu doğrulamanın kapsamında değildir. Görsel QA ve etkileşim çağrıları ayrı kayıt kopyasında yapılır; gerçek satın alma, ödül talebi, hesap veya cloud değişikliği yapılmaz. Git commit/push kullanıcıya aittir.
