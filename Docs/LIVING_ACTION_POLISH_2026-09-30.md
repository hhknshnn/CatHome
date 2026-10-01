# Salon hizalama ve eylem düğmeleri — 30 Eylül 2026

Başlangıç 16:46:30 UTC; kapanış QA/LIVING_ACTION_POLISH_2026-09-30/closure.json. Kullanıcının perde, kitaplık, büyük saksı, kap komutu ve eski eylem düğmeleri talebi tamamlandı.

- Perde çubuğu gerçek pencere çerçevesinin 4,5 cm üstünde, kumaş altı denizlikten 4 cm yukarıda; pencere merkezine ve genişliğine göre hizalandı.
- Kitaplık ve kitaplar birlikte yükseltildi. Kitaplık/pencere alt dünya yüksekliği 1,119937 m. Zemindeki etkileşim noktaları yükseltilmedi.
- TV yanındaki büyük bitki geriye çekildi; görünür sınırı ünitenin ön çizgisinin 2,5 cm gerisinde.
- Yakın kap düğmeleri doğrudan Mama ye / Su iç gösterir. Kaba yaklaş eylemi kaldırıldı. Tıklamadaki gerçek fiziksel temas kontrolü korunur; uygun olmayan duruşta açıklama gösterilir, kedi otomatik taşınmaz.
- Mama, su, uyku, uyanma ve eşya eylemleri yeni altın çerçeveli mercan yüzeyi kullanır. Etkinlik ilerleme rozeti mevcut lacivert/altın yüzeye bağlandı. Sehpa/Koltuk tekrarları yerine Sehpaya çık / Koltuğa çık kullanılır. Dokunma alanı ve konumu aynı.

Yeni yüzey yerleşik ImageGen ile tek üretimde oluşturuldu: Assets/Resources/PremiumHudFinal/action-coral.png. Kaynak 2172×724 şeffaf PNG; Unity nine-slice. Tam nihai istem ve üretim modu [imagegen-prompt.md](QA/LIVING_ACTION_POLISH_2026-09-30/imagegen-prompt.md) içinde. Blender/Affinity değişikliği gerekmedi.

## Doğrulama

13/13 EditMode; 12 benzersiz PlayMode testi geçti (dar oran tekrarını sayınca 13 nihai PlayMode çalıştırması). İki ırkta joystick ile mama/su/uyku/uyanma, gerçek HUD tıklaması, bakımın tamamlanması, engel/tokluk/mesafe, oda yolları, altı gözlem etkinliği ve raf/kitap satın alma-yükleme kontrolleri. Sehpa gerçek tıklama ve tam hareketi 1920×1080 ve 848×392, TR/EN. Telefon donanım testi yok.

İlk sehpa fixture'ı geçerli duruşa yerleştirmediğinden başarısız oldu; mevcut yasal başlangıç bölgesini kullanan fixture ile düzeltildi. Üretim hareket/temas kodu değiştirilmedi. İlk ekranlar menü kapanış geçişini yakaladı; son görüntüler geçiş bitince alındı. MCP bazı bitmiş işleri running raporladı; native XML ve Play kapalı durumu doğrulandıktan sonra yalnız eski iş kaydı temizlendi. Native final manifest esas.

[Son gerçek Game View](QA/LIVING_ACTION_POLISH_2026-09-30/final.png) ve [dar oran](QA/LIVING_ACTION_POLISH_2026-09-30/table-button-848-Turkish.png). Görseller ayrı kayıt kopyasıyla test oturumudur; sayaçlar oyuncunun ilerlemesi değildir. Computer Use ile Unity Game View ayrıca kontrol edildi.

7882 okunabilen başlangıç dosyasından 7874 aynı, 8 kapsam içi değişiklik, 4 yeni dosya, eksik yok. Bir özgün Eat klibi başlangıçta okunamadı. Dört gerçek kayıt ve 16 tercih aynı. Kamera/ışıkların 21 bileşen/transform bloğu ile GameScene ve UI sahnesi aynı; modeller, mevcut dokular, sesler ve hareket/temas sistemleri korunur. İki font önbelleği ve EditorSettings yalnız bu turun başlangıç baytlarına döndü.

Üç temiz sahne; Play/QA/derleme kapalı, Unity açık. Son Console: 0 hata; testlerin geçici boş sahne geçişinden kalan bir ses dinleyicisi uyarısı. Güncel normal sahnede tek etkin ses dinleyicisi doğrulandı. APK/commit/push/yayın yok. Önceki APK bu değişiklikleri içermez. Önceden kayıtlı Persian berjer inişi bu işin kapsamı dışındadır.
