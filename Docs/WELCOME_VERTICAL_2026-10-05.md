# Onaylanan dikey sol menünün entegrasyonu — 5 Ekim 2026

Kullanıcı önceki yatay alt menüyü reddedip dikey sol menü prototipi istedi. ImageGen prototipini ardından “bu daha iyi bu şekilde devam et… Butonlara çerçeve ekle… entegre et” diyerek onayladı. Bu teslim o onaylı yönü uygular. Oyun ekranı yatay, menü solda dikeydir; önceki yatay alt şerit artık ana ekranda kullanılmaz.

## Uygulanan tasarım

- Salonla yumuşakça birleşen sıcak krem sol alan; üstte açık çatı/kedi işareti ve CAT HOME, altında karşılama, ana eylem, ikincil yeni oyun bağlantısı, üç kısa yol ve yuva seviyesi.
- ImageGen ile üretilmiş parlak mercan ana düğme, ince krem/nane çerçeve; Kedim/Odalar/Oyunlar için aynı aileden çerçeveli küçük kartlar. Metinler canlı Unity metni olarak kalır, TR/EN değişir.
- Onaylanan prototipteki açık yerleşim korunur; çevreleyen büyük panel ve yatay alt menü kaldırılır. Gerçek salon, canlı kedi modelleri ve önceki turda düzeltilmiş oynanış ölçeği kullanılır. Görsel prototipin boyalı salonu oyuna arka plan resmi olarak yerleştirilmedi.
- Bu tur yalnız ana ekran sunumudur. Dönüş penceresinin mevcut sunumu ve davranışı değişmedi; regresyon kontrollerine dahil edildi. Oynanış sahnesi, kamera, ışık, modeller, animasyonlar ve kabul edilmiş tırmalama değişmedi.

## Kaynaklar

Yerleşik `image_gen` kullanıldı; CLI/API kullanılmadı. Yeni şeffaf dokular `Assets/Resources/WelcomeVertical/FramedAction.png`, `FramedShortcut.png`, `RoofCatMark.png`. Üretilen alfa korunur; düğmeler kesitlenebilir Sprite olarak yüklenir. Yalnız başlık için yeni sanat anahtarları kullanılır, mevcut dönüş dokuları aynı kalır.

Dış çalışma kökünde `ArtSource/UI/WelcomeVertical_20261005/` altında onaylanan `ApprovedPrototype.png`, üç dokunun kaynak kopyası ve tam `imagegen-prompts.json` bulunur. Bu entegrasyon turunda Affinity/Blender dosyaları düzenlenmedi. Computer Use, Unity'deki gerçek tıklama doğrulamasında kullanıldı.

Gerçek son 1920×1080 GameView: `QA/WELCOME_VERTICAL_2026-10-05/title-final.png`.

## Doğrulama

- 5 native PlayMode + 25 native EditMode testi başarılı: başlık yaşam döngüsü/temizleme, 10 ırkın gerçek iskeleti, ışık restorasyonu, azaltılmış hareket/odak, dönüş düğmesi, çeviriler, bağlar ve yerleşim.
- TR/EN × 1920×1080, 1440×1080, 2400×1080, 848×392 × ana ekran/dönüş = 16 görünüm başarılı. Görünür etkin düğmelerde çakışma, safe area ihlali ve metin taşması bulunmadı. Ayrıca TR/EN ilk-tur sunumunda taşma yok.
- Saydam yeni oyun bağlantısı otomatik görünür-Graphic ölçümüne dahil olmadığından Computer Use ile ayrıca tıklandı: Yeni oyun → onay penceresi → Vazgeç; ardından Devam et → salon başarılı. Sıfırlama yapılmadı. Dönüş düğmesi bu tur native EventSystem testinde doğrulandı.
- LevelContentValidator: 0 hata / 0 uyarı. Son Console: hata/uyarı yok.
- Dönüş ekranlarının süre/ihtiyaç sayıları ve ilk-tur kilitli kart görüntüleri izole QA sunum örnekleridir. Gerçek oyuncu ilerlemesi değildir. Fiziksel telefon/FPS testi yapılmadı. Prototip kullanıcı onaylıdır; oyundaki son entegrasyon için ayrıca görsel kabul iddiası yoktur.

## Koruma ve kapanış

Yeni tur başlangıcı 08:02:06 UTC. Esas kanıtlar `QA/WELCOME_VERTICAL_2026-10-05/native-final-manifest.json`, `preservation-final.json`, `editor-final.txt`, native XML'ler ve `closure.json`.

8275 okunabilir başlangıç dosyasından 8271 aynı; yalnız 3 runtime C# ve mevcut Editor inceleme yardımcısı değişti. 7 yeni Unity dosyası/meta, eksik yok. Bir eski Eat klibi başlangıçta okunamadı; eksiksiz tüm-varlık hash iddiası yok. Beş gerçek kayıt dosyası ve 16 tercih aynı. Font önbelleği ve EditorSettings bu turun doğrulanmış başlangıcına döndü; ara sürümler QA altında saklandı.

Üç temiz normal sahne, tek etkin ses dinleyicisi; Play/QA/derleme/build/profiler kapalı, Unity açık. APK/commit/push/yayın yok.
