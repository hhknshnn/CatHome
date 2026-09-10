# Bütün odalarda ortak salon kamerası — 7 Eylül 2026

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

Kullanıcı isteği: mevcut sekiz oda salonun önden, ortalanmış bakışını kullansın; yeni odalar da aynı düzene katılsın.

## Uygulama

- Tek kaynak `HomeRoomCameraProfile`: dünya konumu `(0, 3.6, -6.5)`, dönüş `(23, 0, 0)`, temel dikey FOV `38`. Perspektif kamera ve ortak oda kutusu korunur.
- `HomeWorldViewport` bütün odalarda alt gezinme için 80 referans piksel ayırır. Dar ekranlarda mevcut yatay kadraj koruması aynı temel FOV üzerinden çalışır.
- Sekiz kayıtlı sahnenin kameraları `HomeRoomCameraBuilder.ApplyAll()` ile güncellendi. Mobilya, ışık ve kayıt verisini yeniden oluşturan oda builder'ları çalıştırılmadı.
- Yedi oda üreticisi ve ortak görsel yenileme aracı artık aynı profili kullanır. `LevelLoader` oda geçişinde; `HomeRoomSceneMarker` açıldığında/yeniden etkinleştiğinde aynı ayarı uygular. Böylece gelecekteki odalar katalog adına bağlı istisna gerektirmez. Açılış ve mini oyun kameraları bu akışa dahil değildir.
- Balkonun ön korkuluğunun çizimi, üst katın tavan/çatı çizimi kesit görünümü için kapatıldı. Nesneler, fizik bileşenleri, yan korkuluklar ve üst katın sarkıt lambası korunur. İlgili üreticiler aynı sonucu tekrar üretir.
- Sekiz oda fotoğrafı ve HOME mağaza kopyaları gerçek Unity sahnelerinden yenilendi. Fotoğraflar aynı poz/açıyı, HUD bulunmayan tam çerçeve için salonun mevcut `42` FOV değerini kullanır. Oyundaki görünümün kendisi değildir; gerçek HUD kareleri ayrıca kaydedilir.

## Yeni oda eklerken

1. Oda `HomeRoomShellMetrics` ölçülerini ve katalogdaki `HomeRoomService.Rooms` sözleşmesini kullanır.
2. Üreticide oda kamerasını oluşturduktan sonra `HomeRoomCameraProfile.Apply(camera)` çağrılır; bağımsız konum/açı/FOV sabiti eklenmez.
3. `HomeRoomSceneMarker.EditorConfigure(id, camera, cat)` ile açık kamera bağı verilir. Normal oda geçişindeki `LevelLoader` da profili uygular.
4. Kameranın önüne gelen çatı/ön duvar/korkuluk, fizik sınırını silmeden kesit görünümü olarak tasarlanır. `HomeRoomCameraBuilder.HideForeground` yalnız belirtilen mimari parçanın çizimini kapatır; mobilya üzerinde kullanılmaz.
5. `LevelContentValidator`, katalogdan türeyen `HomeRoomShellTests` ve `SharedRoomCameraTests` yeni odayı da kapsar. Oda fotoğrafları `RoomPreviewCaptureBuilder.CaptureSilently()` ile yenilenir; HUD'lu Play kadrajı gözle incelenir.

## Doğrulama

- Tam EditMode: **443/443**. Kamera/shell kuralı bütün katalog odalarını kapsar; balkon/üst kat kesiti ayrıca denetlenir.
- Native PlayMode: **2/2**. Sekiz sahne yüklenir; ortak poz, açı, uyarlanan FOV, alt şerit, tek kamera/listener ölçülür. Katalogda bulunmayan yeni bir oda yeniden etkinleştirildiğinde ortak ayarı alır; alakasız sinematik kamera değişmez.
- Canlı normal `LevelLoader` geçişleri: sekiz oda × 1920×1080 ve 1440×1080 = **16 kadraj**. Her karede aynı poz/açı, tek dünya kamerası, tek listener ve tek EventSystem. Geniş ekranda FOV 38, dar ekranda mevcut kadraj korumasıyla yaklaşık 49.785.
- 16 HUD taramasında **0 çakışma / 0 taşma / 0 tıklanamayan düğme**. `LevelContentValidator`: **0 hata / 0 uyarı**. Canlı tur sonunda hata günlüğü boş.
- Testlerin ilk tekrarında Unity'nin `Access version should be odd when acquiring lock` görüntüleme hatası görüldü. Editör yeniden açıldı. Ardından genel kontrol, derleme temizliğinin önceden pişirilmiş Fredoka atlaslarını boşalttığını gösterdi. `PremiumTypographyBuilder` mevcut atlasların karakterlerini de tamamlayacak ve `m_ClearDynamicDataOnBuild=false` bırakacak biçimde düzeltildi; üç ortak atlas diske kaydedildi. Son tam 443/443 sonucu bu düzeltmenin sonrasındadır.

Sonuçlar ve gerçek HD kareler `Docs/QA/SHARED_ROOM_CAMERA_2026-09-07` altında tutulur. Canlı QA, `UiQaTestSession` kayıt kopyasında yapılır; bütün ROOM/HOME ürünlerinin gösterildiği kareler örnek sahipliktir. Fotoğraf amaçlı kedi konumları da yalnız bu kopyada kullanılmıştır. Gerçek cihaz performansı ölçülmedi.
