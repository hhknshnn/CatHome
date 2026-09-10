# Salon — orijinal yeme animasyonu, 10 Eylül 2026

> Sonraki kullanıcı kararı: [kapların özgün şekli ve hacmi geri alındı](BOWL_SHAPE_RESTORED_2026-09-10.md). Aşağıdaki sığlaştırılmış kap ölçüleri ve bunlarla alınan temas testleri tarihsel kayıttır. Orijinal animasyon kararı korunur.

Kullanıcı ek baş/göğüs bükmesinin boynu kırılmış gibi gösterdiğini belirtti ve orijinal yeme animasyonunun korunup yalnız kaplara hizalanmasını istedi. Bu karar, önceki ana kaplarda sürekli ağız IK'sı ve 20 mm yükseltilmiş hedef yaklaşımının yerine geçer.

[Güncel galeri](QA/ORIGINAL_FEEDING_2026-09-10/index.html): dokuz gerçek Unity PNG ve iki normal hızda 24 fps tam rutin videosu.

## Uygulama

- Ana mama/su eyleminden `CatSipHeadMotion` kullanımı tamamen kaldırıldı; bu sınıfın `BowlInteraction` giriş/örnekleme/durdurma API'leri de silindi. Baş, çene, göğüs, bacaklar ve kemik uzunlukları ek çözücüyle değiştirilmez. Diğer odalardaki `SinkSipActivity` yolu aynı kalır.
- Eat ve Drink, mevcut kontrolcüdeki orijinal **Cat_Domestic_Shorthair|Eating (4.3 saniye)** klibini aynı hızda oynatır. Klip, kontrolcü, kedi modeli ve iskelet değiştirilmedi. Tüketim boyunca kök sabittir; her karede ağzı takip ettirerek gövde kaydırılmaz.
- `CatFeedingAlignmentBuilder`, orijinal klibin 64 örneğinden on ırkın doğal ağız doğrultusu ve ayak desteğini ölçer. `CatFeedingAlignmentCatalog` yalnız duruş konumunu saklar; kaynak klip/ırk geometrisi değişirse yeniden üretilir. Runtime, bu ölçüm ve mevcut yan açıyla kediyi yaklaşırken yerleştirir. Yakın tarafta 35 mm pay vardır; orijinal baş hareketi kabın içinde doğal olarak alçalır/yükselir.
- Yüksek kaplara ulaşmak için boynu zorlamak yerine kaplar sığlaştırıldı: yalnız kap/içerik görsel yüksekliği ×.3; alt destek +7.2 mm ile platform üstünde tutulur. Yatay çap, kapların TV yanındaki konumu, 78 cm aralık ve platformun duvar hizası aynı. Ana kapların gerçek içerik hedefi yeni yüzeyden yeniden ölçüldü. Kaynak üretici `PremiumCareStationBuilder` aynı sonucu üretir.
- Önceki doğal yaklaşım ve sahipli ihtiyaç/pause/iptal akışı sürer. TV ünitesinin kaldırılmış Pati at eylemi ve yandan tırmalama değişmedi.

## Güncel kontrol

**4/4 native:** üç `LivingBowlContactTests` ve platform yürüme/güvenlik testi. On ırk × iki kap **20/20**, her biri kaynak klibin tam çevrimini kapsar. Son canlı iskelet, aynı Animator fazında bağımsız örneklenen orijinal kliple her karede karşılaştırıldı: en büyük kemik açı farkı **0.00000°**; yerel kemik konumları ve ölçek korunur. Ana kapta etkin `CatSipHeadMotion` bulunmadığı ayrıca doğrulanır.

Orijinal animasyon doğal baş kaldırma içerdiği için bütün karelerde eski .035 m ağız kilidi aranmaz. Her tam çevrimde gerçek içerik üçgenlerine erişim aranır; kabın tek bir merkez noktasına olan uzaklık ısırma teması sayılmaz. Yüzeye 10 mm altı erişim bulunduğunda pahalı yakınlık örneklemesi durur. Ölçülen erişim: yirmi durumdaki en uzak ölçülmüş yakın erişim **0.00838 m**, doğal baş kaldırma sırasında en büyük uzaklık **0.08276 m**. Baş–kap kenarı en az **0.00616 m**; gerçek patiler zeminde ve stand dışında. Gereksiz dönüş, sekiz yakın başlangıç yönü, pause ve iptal de kontrol edildi. İlk ara başarısız uygulama/testler güncel sonuç değildir; son sonuçlar `native-test-summary.json` ile iki koşunun test kimlikleri üzerinden birleştirilir. `native-partial.xml` içindeki eski başarısız ırk testi yerine güncel `matrix-verified.xml` sonucu kullanılır.

**5/5 EditMode:** 80 koleksiyon prefab bağı ve dört salon yerleşim testi (4.147 izinli koleksiyon). 103 kart/sekiz oda ön izlemesi yenilendi; validator **0 hata / 0 uyarı**. Diğer yedi oda sahnesinin hash'i aynı. Önceki tam 511 ve tırmalama testleri bu dar değişiklikte tekrar çalıştırılmadı.

## Bırakılan durum

QA/Play/derleme kapalı; üç normal sahne temiz, tek kamera/ses dinleyici ve salt-okunur ön izleme açık. 16 tercih ve varlık bayrağı geri yüklendi. Gerçek kayıt/recovery başlangıç–son **07B75EFA0B3E878DA335FD190982B81F0F387592249C0C9BA3129780C8CCC0FA**, CP2 aynı. Kullanıcı arada oynadı; eski kayıt hash'leri geri yüklenmez. APK/arşiv/commit/push/yayın/kapatma yapılmadı. Sonraki konu önce plan ve süreyle tek tek ele alınır.
