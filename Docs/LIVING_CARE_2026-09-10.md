# Salon — yerleşim ve mama/su teması, 10 Eylül 2026

Kullanıcının bu adımı yalnız salonun yatağı, kitaplığı ve ana mama/su istasyonunu kapsar. Sonraki yorumlara ayrı plan ve süreyle geçilir. [Güncel görsel galeri](QA/LIVING_CARE_2026-09-10/index.html): sekiz gerçek Unity PNG'si ve iki gerçek 24 fps video.

## Uygulanan yerleşim

- Ana kedi yatağı berjerin soluna taşındı: X **.68**, arka panelle aynı temas hizası. Uyuma yüzeyi ve açık kalkış noktası gerçek yatak geometrisinden yeniden ölçüldü.
- Kitaplık ve ona bağlı kitap seti eski yatak bölgesine yaklaştırıldı: **(-.95, 0, 2.36)**. Prefab ve katalog üretimi aynı konumu kullanır.
- Mama/su istasyonu televizyonun yanında sol duvara alındı: **(-3.38, 0, 1.56), yaw270°**. Beyaz arkalık görünür duvar paneline yaklaşık **5 mm** aralıkla oturur; TV ile istasyon arasında yaklaşık **.35 m** boşluk vardır. Kaplar tepsinin ön tarafındadır; görünür geometri ve katı engeller korunur.
- Kedi, her iki kapta yaklaşık **231.5°** yöne bakar. Son kalça–omuz yönü kameradan yandan okunur; kök yönündeki **.30** güven payı korunur.

## Başlangıç ve gerçek temas

`BowlInteraction` artık kâsenin çevresinde yeni duruş aramak yerine açık yaklaşma noktasından kısa, ölçülmüş beslenme duruşuna gider. Hedef çevresindeki çok küçük mesafeyi kovalamaz; dönüşler en kısa açıdan yapılır. Yürüme kapsülü yalnız son dar adımda geçici olarak bırakılır; normal yürüyüşün katı tepsi/duvar kontrolü değiştirilmez. Bitiş veya iptalde açık girişe dönülmeden kapsül yeniden açılmaz.

`PremiumCareStationBuilder`, ağız hedefini mama tanesinin gerçek yukarı bakan üçgeni veya gerçek su yüzeyinden üretir. Çok küçük mama üçgenlerinde alan vektörü Normalize edilmez; Unity'nin sıfırlama toleransı geçerli yüzeyi kaybettirmez. `CatSipHeadMotion` mevcut ağız/ön gövde çözümünü işlem sahipliğiyle ana kaplara da uygular. Eski lavabo/banyo sahipliği korunur.

Beslenme animasyonuna geçişten sonra gerçek pati yüzeyi giriş başına bir kez ölçülür. Uzun tüylü ırklarda tek bir alçak tüyün bütün gövdeyi kaldırmaması için dört pati birlikte dengelenir. Paylaşılan GPU mesh'inin CPU okuma bayrağı açılmaz; geçici ölçüm mesh'i bırakılır. Kök ölçeği, kemik yerel bağları ve uzunlukları değiştirilmez. Hareket, baş teması ve ihtiyaç artışı duraklatmada donar; iptal yalnız kendi işlemini temizler. Tamamlanma/ödül olayları birer kez gelir.

Üretim girişi `LivingCareLayoutBuilder.ApplyAndSave()`; yalnız salonun ilgili iki ürün prefabı, yatak ve bakım istasyonu güncellenir. Diğer yedi oda sahnesinin SHA-256 değeri başlangıçla aynıdır. Salon 5 CAT / en fazla 1 CAT yatağı kuralı korunur. 103 ürün kartı ve sekiz oda ön izlemesi güncellendi.

## Doğrulama

- **13/13 native PlayMode:** `living-care-verified.xml`. Önceki başarısız ara dosyalar son sonuç değildir.
- **510/510 tam EditMode:** `EditMode.xml`; izin verilen 4.147 beşli CAT koleksiyonunun bağlı yerleşim kontrolü dahil.
- **Validator: 0 hata / 0 uyarı.**
- **On ırk × iki kap = 20/20:** gerçek skinned ağız yüzeyi, dört pati, son gövde görünürlüğü ve kemik bağları ölçüldü. En büyük ağız–hedef mesafesi **.03034 m**, en küçük son gövde görünürlük dot değeri **.34033**. Pati yüzeyi/floor farkı **−.01414 … +.03426 m**; test sınırları gevşetilmedi.
- İki kap × dört başlangıç yönünde tam tur yok; duraklatma ve iptal sonrası açık yürüyüş geri gelir. Yeni ana yatak on ırkla, tepsiye çarpmadan giriş/çıkış, bakım işlemi sahipliği ve eski lavabo ağız çözümü ayrıca başarılı.
- İki videoda normal hız, gerçek tamamlanma sayısı **1**, kare sayısı/süre ve bütün beslenme evresindeki ağız mesafesi doğrulandı. `LivingCareVisualCapture` yalnız izole QA'da çalışır; 24 fps ve kamera/ekran ayarlarını sonunda geri koyar. Boş ses kimliği tüketim evresi sayılmaz.

## Kayıt ve devam noktası

Başlangıçtaki gerçek Play oturumu normal kapandıktan sonra alınan ana kayıt ve recovery SHA-256: **BC4324D449232253CA02EED15EB765BD6709013E4F5DBBB7064EA1C2C31F5E17**. CP2: **03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D**. Üçü sonunda aynıdır; tarihsel hash geri yüklenmedi. 16 tercih ve anahtar varlık bayrağı birebir geri yüklendi.

QA/Play kapalı, derleme yok; üç normal sahne temiz, tek kamera/ses dinleyici; Unity ve güncel salt-okunur ev ön izlemesi açık. Kanıtlar `editor-restored.json`, `preferences-restored.txt`, `save-hashes-after.json`, `scene-hashes-after.json`, `verification-summary.json` dosyalarındadır. APK, ZIP/RAR, commit/push, canlı yayın veya bilgisayarı kapatma yapılmadı. Fiziksel cihaz performansı ölçülmedi.
