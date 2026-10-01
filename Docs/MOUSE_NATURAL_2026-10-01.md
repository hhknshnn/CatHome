# Fare oyuncağı doğal etkileşim — 1 Ekim 2026

Başlangıç: 13:43:54 UTC. En fazla iki ürün denemesi ve bir düzeltme uygulandı; bundan sonra yalnız final çekimi/koruma kontrolü yapıldı. Kesin sınır 14:08:54 UTC, gerçek kapanış QA closure.json içinde.

Yalnız cat.toy-mouse değişti. Kedi önce hedefe bakar, mevcut doğal dönüşle yönelir, gerçek ön bacak erişiminin %48'i kadar kısa ve hızla eşleşen yürüyüş yapar. Ardından küçük çömelme, öne ağırlık aktarımı, en yakın tek patiyle hızlı alçak hamle ve başla takip gelir. İki varyasyon yaklaşma/hamle sürelerini ve kaçış yönü/mesafesini değiştirir. Oyuncak ancak gerçek ağırlıklı pati deri noktası hareketli üçgen yüzeye tolerans içinde ulaştığında kayar; kaideye artan örtüşme ve çevre engelleri kaçış yönünü sınırlar. Başarılı hareket sonunda fare konumu korunur. Temas başarısızsa ödül verilmeden mevcut iptal/kurtarma yolu kullanılır.

Mevcut Polyperfect DEF-* rig ve CatHome_Polyperfect Animator, ToyStalk ve mevcut ölçülmüş yürüyüş klipleri kullanıldı. CatSpringGeometry'nin yalnız ölçüm/deri temas planı yardımcıları tekrar kullanılır; yaylı oyuncak animasyonu ve kaynak dosyaları değiştirilmedi. CatToyContactMotion omuz-dirsek-pati düzeltmesini, CatFurnitureGaze sınırlı baş takibini sağlar. CatMouseToyMotion yalnız fare için kaynak uzuv oranlarını ve destek noktalarını korur. Breed başına konum sabiti, yeni rig, yeni klip veya Blender düzenlemesi yok.

## Son doğrulama

Computer Use ile önceki akış ve iki aday Play Mode'da izlendi. Unity MCP ile Animator/rig incelendi ve native PlayMode testleri yürütüldü. Üç boyut × iki varyasyon denendi; final ürün sürümünde beş döngü geçti, bir döngü başarısız. correction.xml bu başarısızlığı açıkça içerir. Son final-video.xml yalnız küçük/orta dört döngüyü tekrarlar ve PASS'tir; üç boyutun tümünün geçtiği anlamına gelmez.

- SMALL Persian (ölçülen gövde yüksekliği 44,8 cm): iki temas başarılı; deri hedef farkı 0,49 / 0,25 mm; 11,3 cm yaklaşma; 13,0 / 9,9 cm fare kayması.
- MEDIUM Domestic Shorthair (47,3 cm): iki temas başarılı; fark 6,15 / 0,47 mm; 11,3 cm yaklaşma; 13,0 / 9,9 cm kayma.
- LARGE Maine Coon (70,4 cm): ikinci varyasyon başarılı, 9,99 mm temas farkı; ilk varyasyon 11,63 mm farkla temas eşiğini geçemedi ve iptal oldu.

Geçen küçük/orta döngülerde hamle sırasında kök kayması 0, destek el/ayak kemik noktası sapması yaklaşık 0,50–0,54 mm, doğal dönüş 22° ve en büyük karelik dönüş 8,01° altında. Örneklenen gerçek kafa derisi ile oyuncak arasında en az 12,9 mm açıklık ölçüldü. Bu, bütün deri üçgenlerinde veya bütün ırklarda sıfır clipping/foot sliding garantisi değildir; yaklaşma mevcut ölçülmüş yürüyüş hız eşlemesini kullanır.

Kalan sorun: Maine Coon ilk varyasyonda yaklaşık 12 mm erişim farkı nedeniyle iptal olur; mevcut kurtarma dönüşü bu başarısız yolda ani görünebilir. Kullanıcının 2 deneme + 1 düzeltme sınırı nedeniyle yeni ürün iterasyonu yapılmadı.

## Video

QA/MOUSE_NATURAL_2026-10-01/CatHome_Fare_Oyuncagi.mp4 — gerçek Unity Game View, 1920×1080 H.264, 24 FPS, 192 kare / 8 saniye, sessiz. İlk dört saniye SMALL, sonraki dört saniye MEDIUM; her bölümde dönüş, yaklaşma, tek pati ve fare tepkisi vardır. Boyutlar arası hazırlık kesmesi dışında yapay görüntü yok. Kamera ve etiketler yalnız QA çekimidir; normal kamera/sahne değiştirilmedi. HUD kopya test verisidir. MP4 tarayıcıda 0–7,999958 saniye aralığının tamamını oynattı, ended=true; final kareleri ayrıca incelendi.

## Koruma

8.292 okunabilen başlangıç dosyasından 8.291 aynı; yalnız CatEnrichmentActivity.cs değişti. Yeni CatMouseToyMotion.cs ve MouseToyNaturalTests.cs ile iki meta eklendi. Eksik dosya yok. Spring dosyaları, sahneler, prefablar, klipler, model ve dönüş sistemi aynı. Bir özgün Eat klibi başlangıçta kabuktan okunamadı. Beş kayıt/yedek dosyası byte aynı; 16 tercih aynı. Font önbelleği ve EditorSettings yalnız bu turun doğrulanmış başlangıç kopyalarına döndü. TestResults.xml native test çıktısıdır, oyuncu kaydı değildir.

Play/QA/derleme/build kapalı, üç temiz normal sahne, tek ses dinleyicisi, Unity açık. APK/telefon/commit/push/yayın yok. Kaydedildi; duruldu.
