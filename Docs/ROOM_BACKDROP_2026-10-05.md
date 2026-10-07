# Oyun içi pencereler — resimli oda arka planı, 5 Ekim 2026

Kullanıcı önceki krem/nane yüzeyleri fazla beyaz, düz renkli alternatifi de yetersiz buldu. ImageGen ile sunulan oda arka planlı örneği onaylayıp uygulanmasını istedi. Bu tur, önceki `IN_GAME_POLISH_2026-10-05.md` tasarımının görsel devamıdır; eski test sonuçları bu turun kanıtı olarak kullanılmaz.

## Uygulama

Pencereler sıcak ahşap çerçeve, mavi duvar dokusu, ahşap zemin, kenarlarda mercan koltuk/yumak/halı ve lamba ile yeniden düzenlendi. Odalar ve dönüş penceresinde bitki/raf/tablo da görünür. Metni sol üstten başlayan diğer ekranlarda bu dekorlar kaldırılmış ikinci resim kullanılır. Küçük onay, konuşma ve öğretici pencereleri aynı dokunun sakin duvar bölümünü kullanır. İçerik kartları sıcak krem/kum tonuna çekildi; onaylı parlak mercan düğmeler korundu.

Odalar ekranının sol bilgi kartı biraz daralıp sağa alındı; soldaki dekor görünür kaldı. Diğer içerik yerleşimleri, eylemler ve özgün tıklama yüzeyleri korunur. Resim katmanı etkileşim almaz, yerleşim grubuna katılmaz ve içeriğin altında durur. `StorybookRoomBackdrop` tek dekoratif Graphic ekler; yeni kamera veya oda geometrisi oluşturmaz, Update döngüsü yoktur.

Kapsam: hamburger menüsü, Mağaza ve tüm alt pencereleri, Görevler/Bugün, Kedim, Odalar, Ayarlar/gizlilik, oyun seçimi/sıralama, kedi komutları/rehber, kutlamalar/ödül bildirimleri, diyalog/isim/öğretici, mini oyun açılış/duraklatma/sonuç/öğretici, dönüş ekranı ve ana ekranın alt pencereleri. Onaylanmış ana ekran yerleşimi korunur. Oynanış, kayıt, ekonomi, hesap işlemleri, kamera, ışık ve kabul edilmiş tırmalama değiştirilmez.

## Görsel kaynaklar

Built-in ImageGen ile üretilen iki 1672×941 PNG değiştirilmeden Unity kaynaklarına kopyalandı:

- `Assets/Resources/PopupRoom/RoomBackdrop.png`
- `Assets/Resources/PopupRoom/ContentBackdrop.png`

Kaynaklar ve onaylı prototip dış çalışma kökünde `../ArtSource/UI/PopupRoom_20261005/` altında. Üretim istemleri `production-prompt.txt` ve `content-prompt.txt`. Kaynak/Unity dosyaları SHA256 ile birebir doğrulandı. Android içe aktarımı ASTC 6×6, max 2048, mipmap ve CPU-readable kapalıdır. Unity çalışma dokuları 2048×1024; özgün kaynak piksel ölçüsü farklıdır. Bu ayar fiziksel telefon FPS ölçümü değildir.

Bu tur Blender/Affinity ile ek düzenleme gerekmedi. Mevcut onaylı `WelcomeVertical/FramedAction.png` düğme görseli yeniden kullanıldı.

## Kanıtlar

`QA/ROOM_BACKDROP_2026-10-05/` bu turun ayrı kanıt dizinidir. `first/` ve `refined/` ara kontrollerdir. Nihai görünümler `screens/`, mini oyun örnekleri `minigames/` altında bulunur. Gerçek oyuncu dosyasının ayrı QA kopyası kullanılır; dönüş/kutlama/sıralama/mini oyun sonuçlarındaki örnek değerler gerçek kazanım değildir.

![Altı ana pencere](QA/ROOM_BACKDROP_2026-10-05/room-backdrop-final-overview.png)

[Tam boy gerçek GameView galerisi](QA/ROOM_BACKDROP_2026-10-05/gallery.html)

## Son doğrulama

- 40 EditMode ve 6 PlayMode testi PASS. Bu tur üretilen `EditMode-native.xml` ve `PlayMode-native.xml` esas: pencere çerçeveleri, yerleşim, görev/dönüş sunumu, EventSystem fare/tekerlek/dokunma kaydırması, portre ayrımı ve dönüş düğmesinin kapanması.
- 40 görünüm × TR/EN × 1920×1080 ve 848×392; ayrıca 8 ana görünüm × TR/EN × 1440×1080 ve 2400×1080 = 192 gerçek GameView. Metin taşması/kesilmesi ve açıklanamayan yerleşim bulgusu yok. Menü scrim'i arkasındaki 40 HUD hit bulgusu beklenen engellemedir; ham bulgular saklanır.
- Runner/Catch × açılış/duraklatma/sonuç/öğretici × TR/EN × iki oran = 32 görünüm. Düğme sınırı/çakışma/hit bulgusu yok. Bu mini oyun ek grubunda otomatik TMP taşma taraması yapılmadı; tur başlatılmadı, örnek sonuç ödülü verilmedi.
- İki ek öğretici örneği: TR sevme ve EN hareket. Devre dışı öğretici denetleyicisiyle sunum kontrolüdür; ilk-tur davranış kabulü değildir.
- Oluşturulmuş 23 arka plan katmanında raycast, layout-ignore, çizim sırası ve tekrar katman kontrolü: 0 ihlal.
- Computer Use: Menü → Odalar → kaydırma tutamacını alta sürükle → Kapat; Menü → Ayarlar → Kapat geçti. Kapanışta giriş engeli 0. İlk fiziksel tekerlek enjeksiyonu listeyi hareket ettirmedi; bu deneme geçti sayılmaz. Sürükleme geçti, native EventSystem tekerlek testleri ayrıca geçti.
- LevelContentValidator 0 hata/0 uyarı. Son normal editörün native Console sayacı 0 hata/0 uyarı. Görsel turdaki iki Catch `Parameter 'Hash 0' does not exist` uyarısı ve test geçişlerindeki ses dinleyicisi mesajları tarihsel olarak korunur; bu kaynaklarda düzeltme yapılmadı. Son normal sahnede tek etkin ses dinleyicisi var.

## Koruma ve kapanış

Başlangıç 2026-10-05 10:44:03 UTC. 8286 okunabilir başlangıç dosyasından 8267 aynı; 18 mevcut runtime C# ve 1 Editor QA yardımcısı değişti. Bir yeni runtime dosyası/metası, iki PNG/metası ve klasör metası: 7 yeni dosya. Eksik dosya/son okuma hatası yok. Özgün Eat klibi başlangıçta okunamadı; eksiksiz tüm-varlık hash iddiası yok. Dış `ArtSource/UI/PopupRoom_20261005` yeni kaynakları ayrıca `art-manifest.json` ile kayıtlıdır.

Beş gerçek kayıt dosyası ve 16 tercih başlangıçla aynı. İki font önbelleği ve EditorSettings yalnız bu turun hash doğrulanmış başlangıcına döndü; CurrencyHud prefabı başlangıçla zaten aynıydı. Sahne/prefab/model/oynanış/kamera/ışık kaynakları aynı. Üç temiz normal sahne, tek ses dinleyicisi; Unity açık, Play/QA/derleme/build/profiler kapalı.

Son kanıtlar `native-final-manifest.json`, `closure.json`, `preservation-final.json`, `editor-final.txt`, `visual-summary.json`, `art-manifest.json`. Kesin kapanış zamanı ve süre closure içindedir. Fiziksel telefon/FPS ve entegre son görünüm için kullanıcı görsel kabulü yok. APK/commit/push/yayın yapılmadı.
