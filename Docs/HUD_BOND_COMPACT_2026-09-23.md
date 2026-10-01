# Bond XP yerleşimi ve telefon oranları — 23 Eylül 2026

Kullanıcı Bond XP'nin ikinci satırdaki konumunu beğenmedi ve üst düğmelerin telefonda fazla yer kaplayıp kaplamayacağını sordu. Başlangıç 13:21:57 UTC, tahmin 15–20 dakika, kesin sınır 13:46:57 UTC. Kapanış zamanı QA manifestinde kayıtlıdır.

Bond XP, sağdaki jeton ve elmas sayaçlarının yanına taşındı. İkinci satır kaldırıldı; Bond paneli diğer sayaçlarla aynı 88 referans birim yüksekliğinde. 1920×1080 tabanında ihtiyaç paneliyle 30, jeton sayacıyla 28 birim boşluk var. Artı düğmelerinin 48 birim dokunma alanı ve mevcut yazı boyutları korundu. Diğer sayaçların mevcut 84/88 değerleri ortak yükseklik ve konum sabitlerine bağlandı. Yalnız `Assets/Scripts/StorybookHudLayout.cs` değişti.

Üstten son HUD öğesinin altına kadar ayrılan düşey mesafe 217'den 134 birime, ekran yüksekliğinin %20,1'inden %12,4'üne indi. Panelin kendi yüksekliği %8,1. Bu ölçü bütün ekran genişliğini kaplayan katı bir örtü alanı değildir; üst boşluk ve portrenin alt sınırı dahil düşey yerleşim ölçüsüdür.

1920×1080 ve 1280×720 (16:9), 2340×1080 (19.5:9), 2400×1080 (20:9) Unity Game View ölçümleri ve gerçek oyun görüntüleri kontrol edildi. Her çözünürlükte beş üst panel aynı hizada, güvenli alan içinde ve birbirinden ayrı; Bond'un ikon, değer ve artı düğmesi kendi panelinin içinde. Dokuz HUD düğmesinin dokunma hedefleri ve canlı bakiyeler geçti. Günlük bildirim panelin altında 16 ölçekli birim boşlukla görünür; dokunmayı engellemiyor. Yedi pencerede 52/52 dokunma hedefi, bildirimin gizlenmesi ve süresini koruyarak devam etmesi geçti. Console: 0 hata / 0 uyarı.

Fiziksel telefon ve çentik simülasyonu yapılmadı. 48 referans birimlik artı alanı 720p'de 32 piksele ölçeklenir; bu fiziksel 48 dp garantisi değildir. Gerçek cihazda okunabilirlik ve parmakla kullanım kullanıcı denemesiyle değerlendirilmelidir. UI dışındaki kaynaklar ve kedi hareketi değiştirilmedi; geniş EditMode testi tekrarlanmadı.

Görev başlangıcında kullanıcı normal, korumasız Play oturumundaydı. Oturum normal biçimde durdurulurken ana ve recovery kayıtları oyunun kendi kaydetme akışıyla güncellendi; bu güncel ilerleme korundu. Durdurma öncesi ve sonrası dosyalar ayrı saklandı. QA için durdurma sonrası güncel kayıtların ayrı kopyası kullanıldı; üç gerçek kayıt bu QA başlangıcından kapanışa byte aynı kaldı. Tarihsel kayıt geri yüklenmedi.

5710 başlangıç oyun dosyasından yalnız bir UI C# dosyası değişti; eksik dosya yok. 16 tercih geri geldi, Play/QA kapalı, üç normal sahne temiz, Unity açık. Commit/push/APK/video/yayın yok. Yeni görünüm kullanıcı tarafından henüz değerlendirilmedi; yeni iş otomatik başlamaz.

Kanıt: `QA/HUD_BOND_COMPACT_2026-09-23` içindeki `bond-row-*`, `geometry-final-*`, `raycasts-final-*`, `modal-final.json`, `preservation-final.json`, `editor-final.json`, `closure-summary.json`. Son görüntü: `screens-final/hud-bond-final-1920.png`.
