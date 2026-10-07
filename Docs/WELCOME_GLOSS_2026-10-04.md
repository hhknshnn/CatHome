# Ana ekran ve dönüş ekranı — parlak revizyon, 4 Ekim 2026

Kullanıcının “computer use ile, blender affinity aktif. revize et. güzel parlak yap” isteği uygulandı. Kapsam yalnız ana ekran ve çevrimdışı dönüş sunumudur. Kabul edilmiş tırmalama işi yeniden açılmadı.

## Görünüm

- Affinity: krem/altın kenarlı kart, ev biçimli logo levhası, parlak mercan eylem düğmesi, küçük menü kartları ve yuvarlak araç düğmeleri. 2048×1536 şeffaf atlas, yerel `.af` ve SVG kaynağı korundu.
- Blender: şampanya-altın, parlak üç boyutlu pati; şeffaf 512×512 render. Önceden açık Blender verisi ayrı kopyaya alındı ve yeni sahne eklenerek korundu.
- Ana ekran: gerçek salon geometrisinin yalnız görsel kopyası; seçili canlı kedi önde, diğer iki kedi arkada. Krem okuma alanı yumuşak geçişle odaya karışır. Oynanış salonunun kamerası, eşyaları ve ışıkları değişmedi. Yeni set 172 MeshRenderer, sıfır collider; yalnız URP kamera verisi ve Volume bileşenleri içerir, oyun davranışı yoktur.
- Dönüş ekranı: seçili kedi portresi, “İyi ki geldin!”, uzakta geçirilen süre, üç ihtiyaç kartı, mevcut değerleri gösteren renkli çubuklar ve “Kedime dön” düğmesi. Çevrimdışı hesaplama, ödül ve kayıt kuralları aynı.
- Yeni oyun onayı ve mevcut düğme/input sahipleri korundu. İlk tur kilitleri, Türkçe/İngilizce, güvenli alan ve azaltılmış hareket davranışı korundu.

## Kaynaklar

Ana tasarım kaynakları dış çalışma kökünde `ArtSource/UI/WelcomeGloss_20261004/`:
`WelcomeGloss_Atlas.af`, `.svg`, `.png`, `build_atlas.py`, `WelcomeGloss_Paw.blend`, `Blender_Initial_Copy.blend`.

Unity kaynakları `Assets/Resources/WelcomeGloss/`; iki yeni runtime dosyası `WelcomeGlossPresentation.cs`, `WelcomeGlossTitleLayout.cs`; iki açık çağrılı Editor yardımcı dosyası `WelcomeGlossBuilder.cs`, `WelcomeGlossReview.cs`. Altı mevcut C# dosyası değişti. Sahne, mevcut prefab, oyun modeli, klip ve kayıt değişikliği yok.

## Doğrulama

- Seçili 5 native PlayMode testi PASS: ilk canlı kare/yeniden açma/temizleme, 10 seçili ırkın gerçek iskeleti, oda ışığının birebir geri gelmesi, azaltılmış hareket/odak, gerçek EventSystem pointer ile dönüş düğmesinin kapanması.
- Seçili 25 EditMode testi PASS: Türkçe başlık glifleri, title bağlantıları/korumalı yeni oyun, düğme ayrımı, mobil render oranı, offline süre/ihtiyaç kuralları, safe area ve azaltılmış hareket.
- TR/EN × 1920×1080 / 1440×1080 / 2400×1080 × ana/dönüş = 12 görünüm; metin kesilmesi/taşması, görünür etkin düğme çakışması ve safe area ihlali yok.
- Son isimsiz başlık ve ilk-tur kilit etiketi rötuşu native testlerden sonra yapıldı; ardından TR/EN 1440×1080 ilk-tur görünümleri ve metin taşması tekrar kontrol edildi. Bu ilk-tur görselleri QA sunum durumudur; gerçek yeni hesap oluşturulmadı.
- Computer Use ile Yeni oyun → Vazgeç, Devam et ve Kedime dön tıklandı; dönüş ekranı `IsOpen=False` olarak doğrulandı. İlerleme sıfırlama onaylanmadı.
- LevelContentValidator: 0 hata / 0 uyarı. Son Console hata/uyarı kaydı yok.
- Fiziksel telefon, FPS veya kullanıcı görsel kabulü iddiası yok.

## Gerçek GameView görüntüleri

`Docs/QA/WELCOME_GLOSS_2026-10-04/title-final.png` ve `return-final.png`, 1920×1080.
Ana ekran gerçek seçili Persian/Mama görünümünü kullanır. Dönüş görselindeki 2 saat, %35/%22/%85 değerleri QA örneğidir; oyuncunun gerçek ilerlemesi değildir.

## Koruma ve kapanış

Yeni başlangıç 2026-10-04T16:48:02Z. 8255 okunabilir başlangıç dosyasından 8247 aynı, altı kapsam içi C# ve iki önceden açık Affinity kilit dosyası farklı; 15 yeni Unity dosyası/meta, eksik dosya yok. Başlangıçta bir özgün Eat klibi okunamadı; tüm varlıklar için eksiksiz hash iddiası yok.

Ana/kurtarma/iki önceki yeni-oyun yedeği ve tarihsel CP2 yedeği dahil beş kayıt dosyası birebir aynı. 16 tercih başlangıçla aynı. İki font önbelleği, EditorSettings ve otomatik recovery çıktıları yalnız bu turun hash doğrulanmış başlangıcına döndürüldü; üretilen ara çıktılar QA altında saklandı.

Son durumda üç temiz normal sahne, tek etkin AudioListener; Play/QA/derleme/build/profiler kapalı, Unity açık ve gerçek kayda dayalı salt okunur ev önizlemesi açık. Affinity ve Blender kaynakları kaydedildi. APK/commit/push/yayın yapılmadı.

Esas kanıtlar: `native-final-manifest.json`, `preservation-final.json`, `editor-final.txt`, iki native XML, `visual-review.txt`, `closure.json`. Ara görseller tanısaldır; son görünüm için `*-final.png` kullanılır.
