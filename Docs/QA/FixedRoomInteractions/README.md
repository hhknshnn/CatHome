# Sabit ROOM yerleşimi — doğrulama kayıtları

5 Eylül 2026, Unity 6000.4.4f1 Editor.

- `EditMode-final.xml`: 391 test geçti; mağaza göçü, ROOM taşıma/depolama engeli, ön koşullar, 80 prefab, gerçek collider açıklıkları, ırk temas profilleri, yol ve premium UI kontrolleri.
- `PlayMode-final.xml`: 53 test geçti. Sekiz oda matrisinde toplam 80 ürün × 10 ırk; iskelet hareketi, aktivite pozu, ürün tepkisi, gerçek gövdenin destek yüzeyine teması, yaklaşma, çıkış ve kontrolün geri verilmesi. Salıncakta ırk değişimi/iptal ayrıca testlidir.
- `all-800-combinations.csv`: her eşya/ırk için başlama, bitiş, pozlar, ölçülen kemik hareketi ve açık çıkış zemini.
- `contact-surfaces.txt`: pişirilmiş prefab geometrisinden ölçülen destek noktaları.
- `all-room-clearance-final.txt`: tam koleksiyonların zemin/etkileşim işareti taraması; gerçek yürüyüş bağlantısı PlayMode matrisinde ayrıca doğrulanır.
- `ui-world-corners.json`: 1920×1080 normal Play HUD ve mağaza taraması, 0 düğme çakışması; kamera/listener/EventSystem 1/1/1.

Görseller `Assets/QA/PremiumVisuals/FixedRoomInteractions/` içinde: her oda için tam görünüm, üstten yerleşim ve on eşyalı Maine Coon temas sayfası; ayrıca normal ev/mağaza ekranları. Temas sayfaları inceleme için komşu eşyaları ve duvarları gizler; kitap setinin kitaplığı veya televizyonun ünitesi bu izole karelerde görünmeyebilir. Gerçek bağlantılar tam oda karelerinde ve testlerde denetlenir.

Normal mağaza kontrolünde kitaplık satın alımından sonra (-3.25, 0, 1.05) tasarlanan konumda göründü; kart `IN YOUR ROOM` gösterdi, sürükleme açılmadı. Geçici QA sahipliği geri alındı. Önceki Sphynx seçimi ve kanonik üç sahne geri yüklendi. FREE TEST kapısı korunur; gerçek ekonomi açılmadı. Git commit/push yapılmadı.
