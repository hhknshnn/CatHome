# Oyun içi pencerelerin ortak tema revizyonu — 5 Ekim 2026

Kullanıcı onayladığı dikey ana ekranın ardından, hamburger menüsündeki bütün sekmelerin ve oyun içinde açılan eski lacivert pencerelerin de aynı tasarıma taşınmasını istedi.

## Tasarım ve kapsam

Ortak pencere yüzeyi sıcak krem, içerik kartları çok açık nane, yazılar petrol/yeşil gri oldu. İnce nane dış çizgi ve krem iç çerçeve, küçük yüzey gölgeleri ve parlak düğmeler kullanılır. Birincil mercan düğmeler, önceki turda onaylanan ImageGen `WelcomeVertical/FramedAction.png` görselini doğrudan paylaşır. Yeni ImageGen üretimi yapılmadı; mevcut onaylı kaynak yeniden kullanıldı. Google hesap düğmesinin mevcut marka yüzeyi korunur.

Uygulama alanları:

- Hamburger menüsü; Mağaza'nın kedi eşyaları/mobilyalar/odalar sekmeleri, ürün kartları, satın alma/önkoşul/elmas onayları ve paket penceresi.
- Görevler/Yuva yolculuğu/Bugün; Kedim, ırk ve tüy rengi seçimi; Odalar ve mevcut/kilitli/ziyaret edilebilir durumlar.
- Ayarlar, açma-kapama anahtarları, gizlilik/veri ve silme onayı sunumu.
- Oyun seçimi ve liderlik tablosu; kedi komutları ve altı rehber sayfası.
- Seviye/koleksiyon/ilk gün kutlamaları ve ödül bildirimleri; diyalog, isim girişi, öğretici kartları.
- Mini oyunların açılış, duraklatma, sonuç ve öğretici pencereleri.
- Ana ekranın kredi/yeni oyun/hesap alt pencereleri de aynı ortak stile bağlandı. Onaylı ana ekran yerleşimi korunur. Dönüş penceresinin mevcut açık krem/nane tasarımı kontrol edildi.

Yeni görünüm, mevcut hit alanları ve düğmelerin üzerinde çizilir. Yerleşim, satın alma/ödül/hesap işlemleri, kayıt modeli, oynanış sahneleri, kamera, ışık ve kabul edilmiş tırmalama değiştirilmedi. Bu çalışma emekliye ayrılmış düzenleme ekranlarını tekrar açmaz.

## Kanıtlar

Güncel kanıt dizini `QA/IN_GAME_POLISH_2026-10-05/`. Gerçek GameView görüntüleri `screens/`; mini oyun sunum örnekleri `minigames/`. `first/` ilk görsel incelemedir; son ekranlar ve manifest esas alınır.

Görüntüler ve testler ayrı oyuncu kayıt kopyasında hazırlanır. Kutlama, dönüş, sıralama ve mini oyun sonuç örnekleri gerçek kazanım kanıtı değildir. Satın alma, ödül alma, hesap silme, oturum açma veya bulut eşitleme işlemi tetiklenmez.

## Son doğrulama

- 40 EditMode + 6 PlayMode = 46 test PASS. Yerleşim, yüzey çerçevesi, görev/dönüş sunumu, fare/tekerlek/dokunma kaydırması, portre önizlemesi ve pencere giriş engelleri kontrol edildi. `EditMode-native.xml` ve `PlayMode-native.xml` esas.
- 40 ana/alt görünüm × TR/EN × 1920×1080 ve 848×392; ayrıca 8 ana görünüm × TR/EN × 1440×1080 ve 2400×1080: toplam 192 GameView. TMP taşma/kesilme yok. Açıklanamayan yerleşim bulgusu yok. Menü scrim'i arkasındaki 40 eski HUD hit testi kasıtlı engellidir; ham bulgular sıfır diye sunulmaz.
- Runner/Catch × açılış/duraklatma/sonuç/öğretici × TR/EN × 1920×1080 ve 848×392: 32 sunum görünümü. Düğme sınırı/çakışma/hit bulgusu yok; bu ek grupta otomatik TMP taşma taraması yapılmadı. Sonuçta 1240 puan/+24 jeton yalnız `MiniGameResultView.Present` örneğidir; tur başlatılmadı, ödül verilmedi.
- İki ek öğretici kart örneği: TR sevme ve EN hareket. Denetleyici QA amacıyla devre dışıdır; bu iki örnek ilk-tur yerelleştirme kabulü değildir.
- Computer Use ile gerçek Görevler → Bugün → Kapat ve Mağaza → Odalar → Kapat tıklamaları geçti. Giriş engeli sıfıra döndü. GameView 0,45× fit kullanıldı.
- LevelContentValidator: 0 hata/0 uyarı. Unity'nin kendi Console sayacı: 0 hata/2 uyarı. İki `Parameter 'Hash 0' does not exist` uyarısı Catch açılışında `CatCatchPlayer.ReportAnimatedSpeed → SetInputEnabled → ShowWelcome/Awake` kaynaklıdır. İlgili kod/sahneler bu turda değişmedi; uyarı çözülmüş sayılmaz. MCP ayrıntı etiketi bunları Error döndürse de native `GetCountsByType` ve sarı Console simgesi Warning gösterir.

## Koruma ve kapanış

Yeni turun başlangıcı 5 Ekim 2026 08:35:56 UTC. Başlangıçtaki gerçek Play normal durdurulduktan sonra güncel koruma alındı. 8282 okunabilir dosyadan 8265 aynı, yalnız 17 runtime C# farklı; iki Editor görsel QA yardımcısı ve metaları dört yeni dosyadır. Eksik dosya/son okuma hatası yok. Özgün bir Eat animasyonu başlangıçta okunamadı; eksiksiz tüm-varlık hash iddiası yok.

Beş gerçek kayıt dosyası (ana/recovery/iki eski sıfırlama yedeği/CP2) ve 16 tercih aynı. İki font önbelleği, CurrencyHud prefabındaki QA önizleme sayaç/yerleşim yan etkisi ve EditorSettings yalnız bu turun hash doğrulanmış başlangıcına döndü. Sahne/prefab/model/oynanış/kamera/ışık kaynakları aynı. Üç temiz normal sahne ve tek etkin ses dinleyicisiyle Unity açık; Play, QA, derleme, build ve profiler kapalı.

Son kanıtlar: `native-final-manifest.json`, `closure.json`, `preservation-final.json`, `editor-final.txt`, `visual-final-summary.json`. İlk/ara incelemeler tarihsel; nihai `screens/`, `minigames/`, `tutorials/` esas. Yaklaşık 33,4 dakika; kesin kapanış zamanı `closure.json` içindedir.

Telefon/FPS testi ve entegre son görünüm için kullanıcı görsel kabulü yok. APK/commit/push/yayın yapılmadı.

![Altı ana pencere](QA/IN_GAME_POLISH_2026-10-05/in-game-final-overview.png)

[Tam boy görsel galeri](QA/IN_GAME_POLISH_2026-10-05/gallery.html)
