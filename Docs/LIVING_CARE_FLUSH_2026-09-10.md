# Salon — kaplar karşı duvara sıfır, 10 Eylül 2026

Kullanıcının son isteği TV yanındaki dikine kapların karşı duvara sıfırlanmasıdır. [Güncel galeri](QA/LIVING_CARE_FLUSH_2026-09-10/index.html) dört gerçek Unity PNG'si içerir.

- İstasyon **(-3.375,0,1.6525)**, yaw270. Önceki Z1.40 konumundan **25.25 cm** karşı duvara ilerledi. Platformun gerçek arka ucu **Z2.72000027**; panelZ2.72, sayısal fark0.00000024m. Sol duvara paralel dikine yön aynı; kaplar, platform, engel, girişler ve temas noktaları birlikte taşındı.
- Minder(-1.72,0,2.4387), kitaplık(-.47,0,2.36), ana yatak, tablo ve diğer dört CAT aynı. Önceki yaklaşık28 cm iki boşluk korunur. CAT5/1 sürer.
- Yer değiştirme sırasında su yüzeyinin eşit uzaklıktaki iki üçgeni kayan nokta yuvarlamasıyla yer değiştiriyordu. Bu, su temasını yaklaşık7.4 cm kaydırıp Maine Coon'un başını kenara yaklaştırdı. `PremiumCareStationBuilder.ConfigureBowlContact` eşit uzaklıkta istasyonun yerel sağ eksenini kullanarak kararlı üçgen seçer. Gerçek yüzey, baş/çene erişimi ve kemik uzunluğu kuralları korunur. Üç farklı konumda aynı temas doğrulandı; en büyük dünya farkı0.00000025m'den küçük.

**Son doğrulama:** 4/4 native (`LivingBowlContactTests` üç test ve `ProductionCareTests.PairedCareTray_BlocksNarrowPocketsAndRestoresOldSaves`). On ırk×iki kap20/20; gerçek ağız max0.02465m, baş–kenar min0.02623m. Yakın yönler, pause/iptal, açık çıkış ve gerçek yürüme kapsülü başarılı. İlk başarısız sonuç `native-initial.xml` tarihsel ara kayıttır; güncel sonuç `native-verified.xml`.

4/4 yerleşim EditMode / 4.147 beşli koleksiyon ve minderin tam duvar konumu/açık girişi başarılı. Bu yerleşim testi son temas üçgeni kararlılık düzeltmesinden önce, aynı son mobilya konumlarıyla çalıştı; temas düzeltmesinden sonra native kontroller tekrarlandı. Tam511 test bu adımda çalıştırılmadı. Son gerçek Play'de iki bakım düğmesi ve iki ekran oranı görsel kontrol edildi. 103 ürün kartı/sekiz oda ön izlemesi yenilendi, validator0/0; diğer yedi oda sahnesi aynı.

QA/Play/derleme kapalı; üç normal sahne temiz, tek kamera/ses dinleyici ve salt-okunur ön izleme açık. 16 tercih/varlık bayrağı geri yüklendi. Gerçek kayıt/recovery başlangıç–son **C9E982B6C3066948FB8AF16D0E2559578ECE9B808A59D83F39671905A305AF76**; CP2 **03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D** aynı. APK/arşiv/commit/push/yayın/kapatma yok.

Sonraki salon konusu yine kısa sıralı plan ve süreyle tek tek ele alınır.
