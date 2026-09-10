# Mini oyunlarda görsel, hareket ve arayüz yenilemesi — 7 Eylül 2026

> Runner'ın sonraki sokak/coin/engel/kamera ve ara poz temas düzeltmesi [Runner Boulevard raporundadır](RUNNER_BOULEVARD_2026-09-07.md). Aşağıdaki görsel ayarlar ve test sonuçları ilk geçişin tarihsel kaydıdır.

Kapsam Cat Runner ve Cat Catch: mevcut kediler, Blender ile hazırlanmış oyun görselleri, kameralar, hareket/temas, oyun seçimi, karşılama, HUD, duraklatma ve sonuç ekranları. Ev odalarının eşya yerleşimi ve ölçeği bu çalışmada değiştirilmedi.

## Oynanış ve animasyon

- Runner yol, eşya, platform ve jetonları aynı mesafede kaydırır. Eski `1.35` yaklaşma çarpanı engelleri zeminden bağımsız kaydırıyordu. `LevelContentValidator` da yeni ortak hız sözleşmesini denetler.
- Eğilme bütün modeli Y yönünde sıkıştırmaz. Gerçek omurga, baş, kuyruk ve bacak eklemleri kullanılır. Zıplama, hareketin fiziksel yükseliş/iniş evresine bağlı bir iskelet pozu kullanır; vücut ikinci bir görsel yay çizmez.
- Mini oyunların üç temel pozu ve on ırkın ayrı zemin düzeltmeleri `MiniGameAnimationBuilder` tarafından üretilir. `MiniGameCatAnimation`, sadece ilgili kedide bir Animator override kullanır; evin hareket durumları değişmez. Irk değiştirilince yeni Animator yeniden bağlanır.
- Runner inişindeki hassasiyet hatası giderildi: yükseklik bir milimetreden az olduğunda fizik adımı hâlâ devam ederken olayın atlanması engellendi. Her zıplama tek iniş olayı üretir.
- Sekiz zemin engelinin yüksekliği inşa edilmiş mesh'ten ölçülür. İki kumaş üst engelinin gerçek alt boşluğu .50 m'dir; on ırkın bütün eğilme çevrimi bu açıklığa karşı ölçülür. Engel kökü artık sallanıp esnemez.
- Catch saldırısı kısa hazırlık, ileri atılma, ön pati teması ve toparlanma evrelerine ayrılır. Hedefin ilerlemesi öngörülür; gövde hedefin gerisinde durur ve yakalama noktası iki ön patinin gerçek konumudur. Aynı temas yalnız bir fareyi yakalar.
- Yakalama yarıçapı .62 m'den .32 m'ye daraldı. Hareketsiz kedinin yanından geçen fare yakalanmaz. Farelerin kaçış/yorgunluk ve kaçan hedefi yeniden deneme düzeni korunur.
- Farelerin vücudu sürekli şişip zıplamaz; ayrı kumaş ayaklar kat edilen mesafeye göre hareket eder. Yakın fareler yumuşak ayrışma uygular. Seçilen farenin altında ince bir hedef halkası görünür.

## Kamera ve görsel dil

Runner kameranın şerit takibi .13, zıplama takibi .08, azami yatışı .65° ve FOV aralığı 50–54'tür. Her jetonda FOV sıçraması kaldırıldı; hafif hızlanma/vuruş tepkisi, yol eğimi ve azaltılmış hareket tercihi korunur. Duraklatma kamerayı da durdurur. Catch tüm oyun alanını gösterir ve yalnız çok küçük, yumuşak yatay kadraj takibi yapar.

Blender kaynağı `ArtSource/Blender/MiniGames/build_minigame_collection.py`:

| Grup | Üretilen görsel |
| --- | --- |
| Zemin engelleri | Dokuma sepet ve sarımlı yumak, sisal direk, seramik kap, süpürge, destekli yatak, ödül paketleri, taşıma çantası, dikişli minder |
| Eğilme engelleri | Kumaş kemer ve tente; görünen açıklıkla aynı temas sınırı |
| Catch | Dikişli oyuncak fare, ayrı ayaklar; parke, kumaş minder, paneller, pencere, perde ve bitkili oyun alanı |
| Runner çevresi | Üç farklı detaylı kasaba cephesi, kapı, vitrin, saçak, balkon ve pati tabelaları |
| Yüzeyler | 2048×2048 meşe damarı ve keten dokuma dokuları |

Toplam 15 FBX üretilir. `collection_metrics.json` boyut ve geometri sayılarını kaydeder. Oyunların materyal kopyaları `Assets/Art/MiniGames/Materials` altındadır; ROOM/CAT mobilyalarının kanonik materyalleri yeniden boyanmaz. Dokuz Runner çevre varyasyonu, son iki varyasyonu tekrar etmeme ve Garden sahiplik kapısı korunur. Yumuşak gökyüzü geçişi ve sadeleştirilmiş hız çizgileri sahnenin okunmasını destekler.

## Ekranlar ve ön izlemeler

Karşılama ekranı gerçek oyun fotoğrafı, seçili kedi, rekor, can ve belirgin başlat/eve dön eylemlerini ayırır. Runner HUD daha kompakt bilgi grupları kullanır. Sonuç kartında kedi, büyük skor, gerçek jeton simgesi, tur özeti ve iki ana eylem aynı hiyerarşidedir. Fredoka/Nunito, ivory/mint/mercan/ink paleti ve mevcut ortak premium yüzey/buton altyapısı kullanılır.

Oyun seçimi kartları ve iki karşılama fotoğrafı `Assets/Art/Games` altındaki gerçek, hazır Play sahnesinden çekilmiş 1920×1080 görüntüleri kullanır. Runner'ın kanonik `CatRunnerHero_v1.png` dosyası aynı güncel çekimi taşır. Fotoğraf üreticisi `MiniGamePreviewBuilder.CaptureLive`; ekran/hareket kaydı `MiniGameVisualQa`dır. Editörde sahne açıldığı karede fotoğraf çekilmez.

Can yalnız başlatınca harcanır. Sıfır canla karşılama açılır. Doğrulanmış reklam/IAP sınırları, ayrı Catch canları, Runner günlük hedefleri, kayıtlı skorlar, tek seferlik tur ödülü ve ayrı x2 işlemi korunur.

## Doğrulama ve kanıt

Tam EditMode **457/457**, native PlayMode **6/6**, LevelContentValidator **0 hata / 0 uyarı**. Güncel XML, gerçek ekranlar, dokunma alıcısı/alan ölçümleri ve iki adet 18 saniyelik 1920×1080 / 24 fps hareket kaydı [doğrulama galerisindedir](QA/MINIGAMES_2026-09-07/index.html). `PlayMode-Initial.xml` ve `EditMode-InitialFull.xml` ilk geçişte bulunan hataları saklar; nihai durum için `*-Final.xml` dosyaları kullanılır.

Native PlayMode turu: Catch hareketsiz/kovalama/zemin kontrolleri, Runner havuz/duraklatma ve evden oyuna/geri dönüş/sıfır can akışları; ayrıca on ırkta oran değiştirmeden zıplama, tek iniş ve eğilme. EditMode bütün eğilme çevrimlerini ve on engelin gerçek mesh yüksekliğini denetler.

Canlı QA, `UiQaTestSession` altındaki kayıt kopyasında çalışır; çevrimiçi skor ve cloud sync kapalıdır. Görseller test oyunlarından alınır. Cihaz üstünde mobil GPU maliyeti bu masaüstü doğrulamasına dahil değildir. Git commit/push kullanıcıya aittir.

Son 20 ekran alanı/tıklama ölçümünde çakışma, SafeArea dışına taşma veya tıklanamayan düğme kaydı yoktur. Catch başlangıç açıklaması otomatik takip/atılma davranışını doğru anlatacak şekilde düzeltildi ve iki en-boy oranında yeniden görüntülendi.

Oturum sonunda asıl kayıt dosyasının SHA256 özeti başlangıçla aynı kaldı. Normal eve dönüşte kamera / AudioListener / EventSystem sayısı **1 / 1 / 1**; son düzen `GameScene + CatHome_UI + LivingRoom_Level01`, aktif sahne LivingRoom'dur. QA, native test ve No Throttling kapalı, `playModeStartScene=null`, `DisableSceneReload` açıktır. Ayrıntı [FinalSession.txt](QA/MINIGAMES_2026-09-07/FinalSession.txt).
