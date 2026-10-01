# Zıplama yüzeylerinde doğal dönüş — 1 Ekim 2026

Kullanıcının zıpladığı yerlerde de aynı dönüşü istemesi üzerine yeni takip turu 11:16:32 UTC'de başladı. Kapanış ve süre `QA/NATURAL_JUMP_TURN_2026-10-01/closure.json` içindedir. Tek runtime düzenleme turu yeterli oldu. APK, commit, push ve yayın yok.

## Neden ve değişiklik

Önceki zemin düzeltmesinden bağımsız `CatJumpMotion.TurnDirectly`, sıçrama sonundaki ya da ayağa kalkıştaki pozu dondurup kökü döndürüyordu. Berjerin mevcut hareketi Computer Use ile Play Mode'da izlendi; gerçek Game View başlangıç kaydı `baseline.mp4` alındı. Önceki yavaş `CatSurfaceTurnMotion` sıralı pati sistemi çağrılmıyordu ve yeniden etkinleştirilmedi.

- İniş toparlanmasından sonraki ve aşağı sıçramadan önceki kısa dönüş artık mevcut Walk animasyonu, ölçülmüş yürüyüş temposu ve `CatNaturalTurnMotion` temas katmanını kullanıyor.
- Destekli dönüş 90° için yaklaşık 0,30–0,33 sn, 180° için en fazla 0,50 sn. Tek akışlı yumuşak dönüş; ayrı dört pati dizisi, yeni rig/clip veya Blender yok.
- Başlangıçtaki yatay görsel merkez korunuyor. Kök merkezine öteleme eklenmedi. Kaynak yürüyüş pozu patilerin kaldırılmasını belirliyor; düşük patilere sınırlı yatay tutuş ve hafif gövde ağırlık eğimi uygulanıyor.
- Temas katmanı artık mobilya yüzey çözümünden önce çalışıyor; mevcut gerçek geometri/deri düzeltmesi son söz sahibi. Havada dönüş, sıçrama yörüngesi, touchdown, engel toleransları, yerleşim ve etkileşim sonucu değişmedi.
- `CatActivityAnimation` yalnız açık destekli pivot sırasında kısa yürüyüş parçasına izin veriyor; sonraki etkinlik pozu normal sahipliği geri alıyor.

## Nihai kanıt

Esas dosya `QA/NATURAL_JUMP_TURN_2026-10-01/native-final-manifest.json`: 6 benzersiz native PlayMode testi PASS.

- Berjer: 10 ırk, gerçek HUD başlangıcı/tamamlanma, gerçek deri/uzuv kontrolü; 11.758 örneklenen deri noktası, ölçülen en büyük mobilya içi derinlik 0.
- Koltuk/sehpa/berjer: Domestic Shorthair ve Persian, 24/60 simülasyon FPS, 12 tam döngü. Sehpadaki gerçek oyuncak teması ve sonuç korundu.
- 8 oda: hazır durumdaki 38 tam sıçrama/etkileşim döngüsü; 36'sında yeni destekli pivot gözlendi, iki rutinde pivot gerekmedi. Her biri tek tamamlanma ve kontrol bırakma ile bitti. 38 sonucu bütün olası başlangıçların kabulü olarak yorumlamayın.
- Destekli pivot sırasında pause/resume: kök ve beş örneklenen kemiğin duruşu korundu, devamında etkileşim tamamlandı.
- Önceki zemin dönüşleri: iki ırk, üç hız, sağ/sol 45°/90°/180° toplam 36 senaryo yeniden PASS.
- En son video: Persian ile gerçek berjer düğmesi, sıçrama, yüzeyde 90° dönüş, kısa dinlenme, iniş öncesi ikinci 90° dönüş ve normal tamamlanma.

Seçilen nihai testlerde toplam 62 tamamlanmış etkileşim döngüsü vardır. Ön tanı kaydı, eski giriş koşullarını kullanan baseline-chair başarısızlığı, test hazırlığındaki hatalar ve ara video nihai kabul sayılmaz. Bunlar QA klasöründe tarihsel kanıttır.

36 destekli dönüşlü oda döngüsünde kısa temas ankoruna kaynak hata toplamı 7,7178 m, katman sonrası toplam 1,7660 m (kalan yaklaşık %22,9). Bu, aynı karedeki sınırlı temas düzeltmesinin ölçümüdür; bütün klipte mutlak pati kayması veya son mobilya çözümünden sonraki fiziksel kayma ölçümü değildir. Küçük pati yeniden yerleşmesi tamamen sıfır kayma iddiası taşımaz.

## Video ve koruma

`QA/NATURAL_JUMP_TURN_2026-10-01/CatHome_Ziplama_Donusu.mp4`

Gerçek Unity Game View, 1920×1080, H.264, 24 FPS, 216 kare, 8,999958 sn, sessiz. Tek kesintisiz çekim; hazırlık çekim dışında, kamera ve HUD aynı. Görünen sayaçlar izole QA kopyasıdır. Nihai testlerden sonra kaydedildi, MP4 baştan sona oynatıldı, dönüş ayrıntı kareleri de incelendi. SHA256 `3cca17974a8096b7da13250285afad1ba33ed1b7fc6c8a563b14b2b34f00b178`.

8.284 okunabilen başlangıç kaynak dosyasından 8.281 aynı; yalnız `CatJumpMotion.cs`, `CatActivityAnimation.cs`, `CatNaturalTurnMotion.cs` değişti. Bir test ve metası yeni, eksik dosya yok. Bir özgün Eat klibi başlangıçta kabuktan okunamadı. İki font önbelleği ve EditorSettings yalnız bu turun hash doğrulanmış başlangıcına döndü. Sahne/prefab/model/klip/HUD/kamera/oyuncak yerleşimi/ProjectSettings aynı.

Ana/recovery dahil beş gerçek kayıt/yedek ve 16 tercih aynı. TestResults.xml oyuncu kaydı değil, Unity test çıktısıdır. Üç temiz normal sahne, tek ses dinleyicisi; Play/QA/derleme kapalı, Unity açık. Sahne geçişlerinin mevcut ses dinleyicisi uyarıları dışında nihai Console hata yok. Kaydedildi ve duruldu.
