# Kameradan görünen gerçek su teması

9 Eylül 2026 — kaynak ve doğrulama notu. Su teması kontrolleri tamamlandı. Ana geçişin güncel sonucu **510/510 EditMode, 41 benzersiz native başarılı, validator 0/0**. [Ana rapor](CAMERA_FACING_2026-09-09.md) ve **195 PNG / sekiz videolu** [galeri](QA/CAMERA_FACING_2026-09-09/index.html) günceldir. QA/Play kapalı, 16 tercih geri yüklü ve üç gerçek kayıt dosyası başlangıçla aynıdır.

## Değişiklik

Lavabo, mutfak lavabosu, kuş banyosu ve veranda çeşmesinde kedi gerçek suya dönük, kameradan okunabilir bir destek noktası kullanır. Sırf kameraya bakması için ağzı sudan başka yöne çevrilmez. Yakınlık, açık yaklaşım, sahiplik ve iptal kuralları korunur.

Mutfak lavabosuna gerçek havzanın içinde ayrı bir su yüzeyi eklendi: kaynak yüksekliği **Y=.80**, tezgâh kenarı **Y=.86**, su alanı **X±.28 / Z±.18**. Musluk başlığı havada içme hedefi olarak kullanılmaz. Kuş banyosunun mevcut su alanı ve **Y=.672** yüksekliği korunarak su/ripple görseli seramik gövdeden ayrıldı. İki su görseli de destek collider'ı değildir.

Kuş banyosunda beyaz düz seramik kenar ve sığ gerçek seramik taban patilere destek olur. Eski krem üst dudak ve altın süs, bu düz yüzeyin üstüne taşıyordu. Krem üst profil ile altın halka aşağı alındı; su, beyaz destek yüzeyi ve ürünün X/Z sınırları değişmedi. Native'deki Maine Coon sağ arka pati konumunda ilk çarpılan yüzeyin yukarı normal bileşeni **.314668 → 1.0** oldu. Ölçüm, altta kalan uygun bir üçgeni seçmek yerine gerçekten ilk çarpılan üst yüzeyi kullanır.

## Hareket ve kaynaklar

Yalnız boyun döndürmek mutfakta yeterli değildi: ölçülen boyun zinciri **.17845 m**, hedefe gereken erişim **.27849 m** idi. Su veya kedi ölçeği değiştirilmeden, gerçek üst omurgada en fazla **34°** ön gövde eğimi eklendi. İki ön bacak analitik olarak aynı pati konumu ve yönünde tutulur; erişilemeyen bacak konumu aday olarak reddedilir. Arka bacaklar değiştirilen üst omurga dalının dışındadır. Üç boyun/baş ekleminin toplam sınırı **75°** kalır.

Bu ek düzeltme kökü, kemiğin yerel konumunu, ölçeğini veya uzunluğunu değiştirmez. Kaynak animasyon her karede korunur; giriş/çıkış .2 saniyede karışır. Duraklatma sabit pozu tutar; iptal, devre dışı bırakma ve sahneden çıkış kaynak dönüşleri geri yükler. Eksik ağız profili su ödülü vermeden eylemi iptal eder ve kontrolü bırakır.

Gerçek görünen ağız köşeleri editörde küçük bir kataloğa pişirilir: **7 ortak profil / 168 köşe / 10 ırk**. Runtime modern kedi mesh'inin CPU kopyası açılmaz; en fazla 24 ağız köşesi kemik ağırlıklarıyla hesaplanır. Native test gerçek `BakeMesh` sonucuyla ağız–su mesafesini ayrıca denetler.

- Hareket: [SinkSipActivity](../Assets/Scripts/Activities/SinkSipActivity.cs), [CatSipHeadMotion](../Assets/Scripts/Activities/CatSipHeadMotion.cs).
- Üretim: [SinkSipFacingBuilder](../Assets/Editor/SinkSipFacingBuilder.cs), [CatSipMouthBuilder](../Assets/Editor/CatSipMouthBuilder.cs), [ağız kataloğu](../Assets/Resources/CatSipMouthCatalog.asset).
- Blender: [mutfak suyu](../ArtSource/Blender/CameraFacing/build_kitchen_water.py), [kuş banyosu](../../ArtSource/Blender/PremiumFurniture/build_garden_bird_bath.py). İki üreticinin `.blend` kaynakları kendi dizinlerinde saklanır.

## Doğrulamanın kapsamı

Destek üreticisi ürün başına **10 ırk × 9 gerçek Eating evresi × 4 pati = 360 pati konumunu** kontrol eder. Kuş banyosunun son gerçek gövdesinde **360 × 5 temas yaması noktası = 1.800** ilk üst yüzey kontrolü sıfır hatayla geçti. Native'de başarısız olan ek ara evre de aynı gerçek konumla yeniden ölçüldü.

**Dört ürün × on ırk = 40 su teması denetimi başarılı.** 34° ön gövde çözümünde gerçek ağız–su mesafesi **.045 m altında**; dört pati desteği, duraklatma, değişmeyen kemik konumu/ölçeği ve iptal sonrası kaynak poz ayrıca doğrulandı. Eksik profilin ödülsüz iptali ile bahçe/veranda gruplarının güncel tekrarları da başarılıdır. Sonuçlar [41 benzersiz native test birleşiminde](QA/CAMERA_FACING_2026-09-09/native-test-summary.json) aynı test kimliğinin en güncel kaydı üzerinden tutulur; ara başarısız dosyalar korunur.

Kanıtlar: [ağız erişimi](../Library/SinkSipFacing/head-reach-contact.json), [kuş banyosu ilk yüzey karşılaştırması](../Library/SinkSipFacing/rim-staging/actual-top-surface-comparison.json). Ara başarısız ölçümler silinmedi. Regresyonlar [HomeContactFacingTests](../Assets/Tests/PlayMode/HomeContactFacingTests.cs) ve [CatSipMouthCatalogTests](../Assets/Tests/EditMode/CatSipMouthCatalogTests.cs) içinde bulunur.
