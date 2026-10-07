# Yatay ana ekran ve dönüş revizyonu — 5 Ekim 2026

Kullanıcı 4 Ekim görünümündeki büyük kedileri, sol menüyü, krem-altın çerçeveleri ve pati rozetini beğenmedi. Bu turda ImageGen ve Computer Use/Affinity kullanılarak yeniden tasarlandı. Kullanıcının tur içindeki “tasarım yatay olmalı” yönlendirmesi son tasarımın esasıdır; ilk dikey kart denemesi kullanılmıyor.

## Son görünüm

- Ana menü altta yatay bir şerit: solda marka ve karşılama, ortada Devam et/Yeni oyun, sağda Kedim/Odalar/Oyunlar. Salonun sol tarafı açıldı. Yeni nane-krem porselen yüzey, mercan düğme ve kedi-ev rozeti birlikte kullanılıyor; altın pati/altın UI çerçevesi kaldırıldı.
- Başlık kedilerinin kök ölçeği 1,2/1/1 yerine oynanıştaki gerçek 0,5/0,5/0,5. Irk fabrikasının kendi boyut farklılıkları korunur. Seçili kedi sehpadan ayrılıp açık zemine alındı. Başlık kamerası (0;2,7;-6,5), 18° ve FOV36 ile oyun kamera ayarlarına getirildi. Yalnız başlığa ait görsel sahne kopyası değişti; oynanış sahnesi/kamera/ışık/model/animasyon ve kabul edilmiş tırmalama aynı.
- Dönüş penceresi de yatay: solda seçili portre, sağda başlık/süre/üç ihtiyaç, altta eylem. İhtiyaç çubukları mevcut gerçek değerlere bağlı; çevrimdışı hesaplama değişmedi.
- Unity GameView önizlemesindeki 1,3× yakınlaştırma Computer Use ile ekrana sığan 0,45× düzeyine alındı. Önceki ekran görüntüsündeki kırpılmanın bir nedeni buydu.

## Kaynaklar ve görseller

Yerel ImageGen aracı kullanıldı; API/CLI kullanılmadı. Üç üretim dokusu şeffaf PNG olarak `Assets/Resources/WelcomePolish/` altında: `LandscapePanel.png`, `CoralAction.png`, `HomeEmblem.png`. Dokular, üretilen gerçek alfa ile içe aktarıldı; UI metni canlı ve çevrilebilir.

Dış çalışma kökünde `ArtSource/UI/WelcomePolish_20261005/` kaynakları bulunur. Computer Use ile açılıp kaydedilen son Affinity dosyası **WelcomeHorizontalFinal.af**. Son SVG'de oyunun Fredoka/Nunito fontları vektör eğrilerine çevrildi; font kurulumu gerekmez. Yeniden üretilebilir kaynak betikleri ve metinli SVG de saklıdır. Affinity dosyası menü yüzeyinin düzenlenebilir tasarım kaynağıdır; Unity'deki canlı yerleşim, ikonlar ve çeviriler C# tarafından yönetilir. Blender bu yeni turda değiştirilmedi.

Son kullanılan istem seti `ArtSource/UI/WelcomePolish_20261005/final-prompts.json`. İlk dikey deneme kaynak olarak saklıdır, üretim Resources klasöründe değildir.

Gerçek Unity GameView:

- `QA/WELCOME_POLISH_2026-10-05/title-final.png`
- `QA/WELCOME_POLISH_2026-10-05/return-final.png`

Dönüşteki 2 saat ve %35/%22/%85 değerleri izole QA örneğidir; gerçek oyuncu ilerlemesi değildir. İlk-tur ekranları da yalnız sunum örneğidir; yeni hesap/sıfırlama yapılmadı.

## Doğrulama

- 5 native PlayMode testi PASS: başlık yaşam döngüsü/temizleme, 10 ırkın iskeleti, ışık restorasyonu, azaltılmış hareket/odak ve gerçek EventSystem dönüş düğmesi.
- 25 EditMode testi PASS: Türkçe glifler, title bağları/korumalı yeni oyun, düğme ayrımı, mobil render, süre/ihtiyaç ve safe area davranışı.
- TR/EN × 1920×1080, 1440×1080, 2400×1080, 848×392 × iki ekran = 16 görünüm: kesilme, metin taşması, etkin görünür düğme çakışması veya safe area ihlali yok. Ayrıca TR/EN ilk-tur 1440×1080 sunumunda metin taşması yok. Yeni oyun yüzeyinin saydam Graphic rengi ölçüm listesine dahil edilmediğinden bu düğme ayrıca gerçek Computer Use tıklamasıyla doğrulandı.
- Computer Use: Yeni oyun → Vazgeç, Devam et, Kedime dön geçti. Sıfırlama onaylanmadı. Son dönüş `IsOpen=False`.
- LevelContentValidator 0 hata / 0 uyarı. Son Console hata/uyarı yok.
- Telefon/FPS ve kullanıcı görsel kabulü henüz yok; bu kontroller kusursuz görsel kabul iddiası değildir.

## Koruma ve kapanış

İlk inceleme 06:20:18 UTC, bu turun hash başlangıcı 06:25:13 UTC. Esas son kanıtlar `native-final-manifest.json`, `preservation-final.json`, `editor-final.txt`, iki native XML ve `closure.json`.

8268 okunabilir başlangıç dosyasından 8262 aynı; 5 mevcut C# ve yalnız başlık sahne kopyası prefabı değişti. 7 yeni Unity dosyası/meta, eksik yok. Başlangıçta bir eski Eat klibi okunamadı; tüm varlıklar için eksiksiz hash iddiası yok. Gerçek ana/kurtarma/iki önceki yeni-oyun yedeği/CP2 dahil beş kayıt dosyası aynı; 16 tercih aynı. Font önbelleği ve EditorSettings yalnız bu turun doğrulanmış başlangıcına döndü.

Üç temiz normal sahne, tek etkin dinleyici; Play/QA/derleme/build/profiler kapalı, Unity açık ve normal salt okunur ev önizlemesinde. APK/commit/push/yayın yapılmadı.
