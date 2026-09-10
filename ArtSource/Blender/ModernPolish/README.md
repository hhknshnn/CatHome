# Cat Home — modern dünya materyalleri

Bu kaynaklar, onaylanan görsel yenilemeyi mevcut oyun geometrisini taşımadan uygular. Sekiz yüzey tarifi Blender 5.2 / Cycles ile özgün olarak üretildi. Mockup görsellerinden kırpılmış veya düzenlenmiş oyun dokuları değildir.

## Üretim ve uygulama

1. `build_modern_surfaces.py`, Blender'ın `--background --factory-startup` kipinde çalışır. Sekiz adet 512×512 paketli doku, `ModernSurfaces_Source.blend`, gerçek Blender malzeme inceleme görseli ve `surface_metrics.json` üretir. Açık Blender oturumunu kullanmaz.
2. `build_surface_shader.ps1`, projenin kurulu `com.unity.render-pipelines.universal@18d0e59f18f1` paketindeki Lit shader'ını temel alır. Paket kodunun lisansı `Assets/Art/ModernPolish/Shaders/URP-LICENSE.md` içindedir; özel yüzey işlevi projeye aittir.
3. Unity içindeki `ModernWorldArtBuilder.EnsureAssets()` dokuları doğrusal renk, mipmap, repeat, trilinear, anisotropy 2, okunabilir CPU kopyası kapalı ve Android ETC2 RGB4 / 512 olarak içe aktarır.
4. Yalnız materyal uygulamak için `ApplyAllRooms()`, `ApplyCatalogPrefabs()`, `ApplyRunner(root)`, `ApplyCatch(root)` veya `ApplyRoot(root, roomId)` kullanılır. `ApplyTitleStage()` mevcut açılış prefabını bitirir; dekoru, kameraları veya ışıkları yeniden üretmez.
5. Uygulamadan sonra ürün/oda fotoğrafları, açılış posteri ve hazırlanmış gerçek Play oturumundaki mini oyun fotoğrafları yenilenir. Fotoğraf yenileme ve Unity doğrulamaları ana görev tarafından yürütülür.

Oda kabuğu, referans oda, salon, katalog, tek ürün, CAT prefabı, bakım modeli, arcade ve açılış üreticileri son sanat adımında ortak materyal uygulamasını çağırır. Bu bağlantıların eklenmesi kendi başına tam dünya üretimi başlatmaz. Eski mimari/arcade renk geçişleri türetilmiş `Modern_*` adından karar vermeden önce özgün materyali çözer. Mini oyun materyal araması tam özgün adı kullanır.

## Son oyun görüntülerine göre ayar

İlk oda fotoğraflarındaki güçlü ahşap dalgaları ve çimdeki çapraz damarlar azaltıldı. Meşe daha nötr bir sıcak tona alındı. Büyük çim ve yaprak hacimleri, `Leaf` ailesini ve yeşil rengini koruyarak çizgisiz organik `Plaster` doku haritasını paylaşır. Kaynak `Leaf` damar örneği Blender incelemesi için durur; dünya bitişinde kullanılmaz.

Katalogdaki ikinci kontrolde doğrulanan belirli model/slot istisnaları da korunur: buzdolabı ve ocak gövdesi emaye/seramik, şemsiye örtüsü ve hamak yatağının krem şeritleri kumaştır. Hamağın ayrı sabit iskeletindeki beyaz/mint parçalar boyalı sert yüzeydir. İstisnalar tam model adlarını kullanır; diğer ürünlerin sınıflandırmasını değiştirmez. Şemsiyenin küçük taban diski örtüyle aynı krem slotu paylaşır; bunun için mesh ayrımı yapılmaz.

| Aile | Renk ayrıntısı katsayısı | Kabartı katsayısı | Pürüzsüzlük |
|---|---:|---:|---:|
| Meşe — mimari | .05 | .000012 | .35 |
| Meşe — ürün | .14 | .000035 | .35 |
| Kumaş | .28 | .00030 | .12 |
| Taş | .20 | .00010 | .19 |
| Seramik | .30 | .00004 | .66 |
| Boya/sıva | .22 | .00008 | .22 |
| Yaprak/çim | .08 | .000015 | .16 |
| Rattan | .35 | .00050 | .16 |
| Süet | .24 | .00010 | .10 |

Metal detaylar .64 metaliklik, .57 pürüzsüzlük ve .00003 kabartı kullanır. Blender küre incelemesi ham yüzey tariflerini gösterir; son Unity kontrastını temsil etmez. Son görünüm gerçek oda ve oyun fotoğraflarıyla değerlendirilir.

## Korunan davranış ve mobil bütçe

RGB kanalları sırasıyla yarı nötr albedo, pürüzlülük değişimi ve mikro yükseklik taşır. Shader bir ek doku örneği alır. Baskın eksen üzerinden nesne uzayında, dünya birimlerine göre ölçeklenen izdüşüm; sabit palet UV'li arcade modellerinde de çalışır ve hareketli Runner parçalarında yüzeye bağlı kalır. Üç eksenden ayrı ayrı örnekleme, displacement veya ek mesh kopyası yoktur. Yüzey normaline katkı sınırlıdır; kıvrımlı yüzeylerde eksen geçişleri yakın incelemede hafif görülebilir.

Özgün albedo/normal haritaları ve hazırlanmış palet dokularının renkleri korunur. Her türetilmiş materyal özgün varlık yolu ve adını saklar. Yeniden uygulama özgün kaynaktan hesaplanır; renk ve ölçek birikmez. Kaynak silinirse mevcut türetilmiş materyal yeniden türetilmez. Runner'ın geri dönüşümde kullandığı beş tema dizisi de yeni materyallere bağlanır.

Materyal geçişi mesh, transform, collider, destek/temas noktası, ürün yerleşimi, kamera/ışık sayısı, görünürlük veya oyun kurallarını değiştirmez. Kedi/vendor materyalleri, UI, saydam yüzeyler, video, su, ayna, cam, gökyüzü/bulut, kanonik jetonlar, güçlendirme ve uyarı materyalleri hariç tutulur.

Sekiz kaynak PNG toplam **2.898.228 bayt** tutar. Sekiz dokunun tümü ETC2 RGB4 ve mipmap ile GPU'da bulunursa hesaplanan doku belleği **1.398.208 bayt / 1,33 MiB** olur. Mevcut dünya yaprak için sıva haritasını paylaştığından farklı harita sayısı yedidir. Bu değerler APK boyutu veya fiziksel cihaz performans ölçümü değildir; yeni APK üretilmez.

Projede PC renderer'ı Forward+, mobil renderer Forward kullanır. Özel yüzey bu ortak forward geçişindedir; URP'nin mevcut ışık, gölge, derinlik, hareket vektörü ve materyal sabit tamponu korunur. Gölge/derinlik geçişleri mikro kabartı eklemez. İleride deferred çizime geçilirse GBuffer için aynı yüzey adımı ayrıca eklenmelidir. Gerçek cihaz FPS/GPU ölçümü bu kaynak klasöründen çıkarılamaz.
