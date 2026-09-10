# Salon eylem düzeltmeleri — 10 Eylül 2026

[Güncel galeri](QA/LIVING_ACTION_REFINEMENT_2026-09-10/index.html): 10 gerçek Unity PNG ve üç normal hızda 24 fps tam rutin videosu. Yalnız kullanıcının bildirdiği dört salon konusu ele alındı.

## Yapılan değişiklikler

1. Mama yerken ağız hedefi gerçek mama yüzeyinin **20 mm üstüne** alındı; baş biraz yükselir. `BowlInteraction.MouthSurfaceClearance` yalnız mama için uygulanır. `CatSipHeadMotion.Distance` hâlâ gerçek içeriğe uzaklığı ölçer; yükseltilmiş hedefe olan hata diye raporlanmaz. Suyun hedef yüksekliği aynı. Gerçek baş–kap kenarı açıklığı ve dört patinin zeminde kalması korunur.
2. Yakında ve açık yolda duran kedi artık önce sabit giriş noktasına gidip geri dönmez. Kısa yaklaşım boyunca gerçek hareket yönüne dönerek son beslenme açısına ulaşır; durduğu yerdeki gereksiz son dönüş kaldırıldı. Zaten hizalı başlangıçlarda ölçülen durarak dönüş en fazla **1.213°**. Gereken doğal yönelme, sahipli ihtiyaç artışı, iptal/pause ve açık zemine dönüş korunur.
3. **TV ünitesinin Pati at eylemi kaldırıldı.** `TvUnitPaw` enum kimliği seri kayıt uyumluluğu için yerinde ve emekli; prefab/sahne bileşeni yok. Üretici ve temizleyici bu eylemi yeniden eklemez. TV ünitesi dekor olarak görünür; televizyonun Watch/İzle eylemi korunur. Koleksiyon prefab kontrolü ve validator bunu açıkça tanır.
4. Ana tırmalama direğinin çalışma noktası kameraya yandan görülen açık taraftan seçilir. Yalnız `home.scratch-post` için kök seçim dot aralığı .05–.35; başka odalardaki tırmalama rutinleri aynı. Gerçek son kalça→omuz yönü on ırkta **0.32144–0.32663**; gövde merkezinin direkle örtülmediği ve iki gerçek pati teması denetlendi. Her ırkta her pati dört başarılı hareket yaptı.

Önceki ince platform, 78 cm kap aralığı, duvar hizası, minder/kitaplık/yatak aralıkları ve CAT 5/1 sınırı aynı.

## Güncel doğrulama

**7/7 benzersiz native test:** üç `LivingBowlContactTests`, bakım platformu güvenliği, TV/dekor testi, dört taraftan tırmalama ve on ırkta tırmalama. Kaynaklar `final-bowls.xml`, `step3-tv.xml`, `step4-scratch.xml`; birleşim `native-test-summary.json`. On ırk × iki kap **20/20**; gerçek ağız–içerik en fazla **0.02837 m**, baş–kenar en az **0.00809 m**. Mama ağzının yüzeyden 8–30 mm yukarıda kaldığı ayrıca her örnekte denetlenir. Uzun tüylü boyun tüylerinin pati sayılmasını önlemek için pati örnekleri gerçek deri kemik ağırlıklarıyla ayırt edilir; önceki yalnız yakınlık silindiri bu tüyleri yanlış seçiyordu. Açıklık ve temas sınırları gevşetilmedi. Ara başarısız taramalar nihai sonuç değildir. Sekiz yakın başlangıç yönünde tam tur yok; pause/iptal kontrolü başarılı.

**5/5 EditMode:** 80 koleksiyon prefabının eylem/dekor bağları ve dört salon yerleşim testi (4.147 izinli kombinasyon). 103 ürün kartı ve sekiz oda ön izlemesi yenilendi; validator **0 hata / 0 uyarı**. Diğer yedi oda sahnesi birebir aynı. Tam 511 testi ve bütün odaların canlı rutin taraması bu dar değişiklik için yeniden çalıştırılmadı; eski sonuçlar tarihsel.

Üç video yaklaşma–etkinlik–çıkışı gerçek 24 fps ile gösterir; kare sayıları ve normal süreler `media-verification.json` içinde doğrulanmıştır. On PNG geçerli; 16:9, 4:3, yakın mama/su ve oyuncu kamerasından yandan tırmalama görüntüleri galeridedir.

## Bırakılan durum

QA/Play/derleme kapalı; üç normal sahne temiz, tek kamera/ses dinleyici ve salt-okunur editör ön izlemesi açık. 16 tercih ve varlık bayrağı geri yüklendi. Gerçek kayıt/recovery başlangıç–son **C826E51CB097C364FE6FFD0179893933DD69A28464D402E3EB232A91CB1BAAB9**; CP2 **03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D** aynı. Kullanıcı arada oynadı; önceki hash'ler geri yüklenmez. APK, indirme arşivi, commit/push, yayın ve kapatma yapılmadı.

Sonraki konu önce kısa plan ve süre verilerek tek tek ele alınır.
