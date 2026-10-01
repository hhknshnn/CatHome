# HUD yüksekliği ve bildirim yerleşimi — 23 Eylül 2026

Kullanıcının ihtiyaç panelini coin/elmas/menü yüksekliğine eşitleme ve “Birlikte 2. gün” bildirimini görünür tutma isteği tamamlandı. Kullanıcı bilgisayar kontrolüyle devam izni verdikten sonraki tur 08:50:10 UTC'de başladı; bitiş ve süre QA closure-summary.json içinde. Önceki turun süre aşımı ve eksik kapanışı tarihsel incomplete-note.md dosyasında korunur; başarılı teslim olarak sayılmaz.

## Son değişiklik

- Ortak üst panel 112'den88birime indirildi; coin/elmas/menüyle aynı üst-alt hizaya geldi. Kimlik hedefi88, portre72, kedi resmi64birim. Canlı değerler ve düğme davranışları aynı.
- Günlük/başarım bildirimi HUD katmanının üstünde, pencere katmanlarının altında çizilir. Panelin altına16birim boşlukla, aynı ekran ölçeğiyle yerleşir. Dokunmayı yakalamaz.
- Açılır pencere/mini oyun/diğer engelleyici görünüm sırasında bildirim gizlenir, kuyruğu ve kalan süresi korunur; kapanınca devam eder. Ödül kazanma ve kayıt mantığı değişmedi.
- MainPanel.prefab yalnız3ölçü satırı, CatHome_UI.unity yalnız2satır değişti; kimlikler/bağlantılar korunur. CurrencyHud.prefab başlangıçla byteaynı. Genel HUD üreticisi son kaynakları yeniden üretmek için çalıştırılmadı.

## Doğrulama

- Son EditMode8/8: editmode-final.xml.
- 1920×1080,1440×1080,2400×1080: üst dört panel eşit yükseklik/hiza; bildirim güvenli alanda ve görünür; aralık16/12/16piksel; bildirim dokunmaları engellemiyor. Her boyutta9/9anaHUD hedefi ve canlı bakiyeler geçti. geometry-final-*.json,raycasts-final-*.json.
- Oda,ayarlar,görevler,geri dönüş,kedim,oyunlar,gizlilik:7/7gizleme-süre durdurma-aynı mesajla devam;52/52görünür pencere ve perde dokunma hedefi geçti. modal-final.json.
- İlk merkez örneklemesi tam ekran perde düğmelerini pencerenin arkasındaki merkezlerinden ölçüyordu. Son kontrolde bu perdelerin açık kenarı kullanıldı; ürün kodunda kabulü gevşeten değişiklik yok. Ara sonuç modal-center-probe.json içinde korunur.
- Gerçek Unity görüntüleri screens-final/. Gün2mesajı ayrı QA kopyasında yalnız sunum kuyruğuna verildi; gerçek hesaba eködül verilmedi. Fiziksel telefon testi yapılmadı.

## Koruma ve kapanış

Son preservation-final.json:5706başlangıç dosyası karşılaştırıldı; yalnız6istenen kaynak dosyası değişti (üçC#, birEditMode kontrolü, MainPanel.prefab, UIscene). Üç gerçek kayıt byteaynı; kaynak kedi/oda/hareketler, GameScene ve ana menü aynı. Testte değişen iki font atlası ve CurrencyHud önizlemesi bu görevin başlangıç baytlarına geri alındı.

editor-final.json:16tercih geri geldi; önceki ses ve GameView ayarları geri yüklendi; QA koruma oturumu güvenli kapandı; Play kapalı, üç normal sahne temiz. Son derleme/konsol temiz. Unity ve bilgisayar açık. Commit/push/APK/video/yayın yok. QA çıktıları Git dışında.

QA kökü: Docs/QA/HUD_POPUP_FIX_2026-09-23. Yeni kullanıcı işi olmadan başka ekranlara geçilmez.
