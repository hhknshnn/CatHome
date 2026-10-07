# Tırmalama eklem düzeltmesi — 2 Ekim 2026

Başlangıç 04:23:03 UTC / 07:23:03 İstanbul. Kullanıcının kesin 25 dakika sınırı 04:48:03 UTC. Nihai kapanış QA/SCRATCH_JOINT_FIX_2026-10-02/closure.json dosyasında.

**Kısmi düzeltme; tam görsel/temas kabulü sağlanmadı.** Önceki başarı iddiası geri çekilmiş durumdadır. Bu turdaki değişikliklerin kullanıcı görsel onayı yoktur.

Tırmalama için sınırsız ardışık kol döndürme yerine gerçek iki kemik uzunluğunu koruyan analitik çözüm uygulandı. Dirseğin bükülme düzlemi vücudun aşağı/geri yönünde sabit; iç açı 40–165 derece. Kaynak bilek dönüşü korunuyor. Boş patiyi geriye/yukarı çekme kaldırıldı. Kedi bir kez yükseliyor, iki ön pati direğe yakın kalıp dönüşümlü kısa tarıyor, sonunda bir kez iniyor. Aynı vuruş içinde tüm gövdeyi tekrar tekrar kaldırma kaldırıldı. Duraklamada son kol açıları tutulur.

İlk üç boyut × iki ritim ölçümünde ters dirsek ve açı sınırı aşımı sıfır, kök kayması sıfırdı. Ancak tam testler başarısız: Medium deri 5,1420 mm, Small 5,2574 mm, Large 5,1510 mm mevcut 5 mm sınırını aştı. Büyük kedinin bir temas karesinde distal görünürlük %59,32 ile %60 sınırının altında kaldı. Bu sınırlar gevşetilmedi. Fare ve tünel kontrolü geçti. Sonraki dışa itme denemesi deri mesafesini düzeltti ama patiyi yüzeyden uzaklaştırdı; bu deneme geri alındı. Son kalan sürüm orta boy ve duraklatma/iptal testiyle yeniden kontrol edildi; esas sonuç native-final-manifest.json içindedir. İlk üç boyut sonucu son duraklatma önbelleği değişiminden öncedir; tüm ırklar son sürümde geçti iddiası yoktur.

Gerçek Game View kayıtlarından kare dizileri incelendi. Son orta boy video iki ritmi içerir; kare çözme doğrulaması manifestte bulunur. Sayaçlar ayrı QA kaydına aittir. Bu turda Computer Use Unity hedeflemesi yanlış ön plan görüntüsü döndürdü; öne alma zaman aşımına uğradı. Canlı gerçek hızda Computer Use görsel kabulü yapılmış sayılmaz. Önceki native Unity `Access version should be odd when acquiring lock` uyarıları sürüyor; çözüldü denmez.

Yalnız iki runtime C# ve mevcut ScratchReworkTests değiştirildi. Yeni test ters dirsek, açı sınırı ve kareler arası eklem sıçramasını ölçüyor. Model, kaynak animasyon klipleri, kök, kamera, sahne, HUD ve UI yenilemesi kapsam dışındadır. APK/commit/push/yayın yok. UI talebi önceki süre içinde tamamlanmamıştı; bu turda yeniden başlatılmadı.

Son seçili sonuçlar: Medium FAIL (deri 5,142415 mm); pause/iptal/taze engel/arka başlangıç PASS; fare+tünel önceki aynı tur sürümünde PASS. Medium iki ritim, 458 kare, 413.706 deri noktası; ters dirsek 0, açı aşımı 0, kök 0. En büyük ardışık eklem dönüşü 60 FPS örneklerinde 9,6694 derece. Bu metrikler görsel doğallığın tek başına kanıtı değildir. Son video 458/458 kare çözüldü; 1080p/60 FPS/7,633 saniye; yalnız orta boy video nihai sürüm kanıtıdır.

7.938 okunabilen başlangıç dosyasından 7.935 aynı; yalnız iki runtime ve bir test C# değişik, yeni/eksik yok. Dört gerçek kayıt ve CP2 yedeği aynı; 16 tercih aynı. Bir eski Eat klibi başlangıçta okunamadı. İki font önbelleği ve EditorSettings yalnız bu turun başlangıcına döndü. Son üç temiz normal sahne, tek ses dinleyicisi; Play/QA/derleme/build/profiler kapalı, Unity açık. Kapanış 2026-10-02T04:47:42.514077+00:00. Yeni tur kendiliğinden başlamaz.
