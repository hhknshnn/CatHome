# Banyo — yalnız yerleşim adımı, 11 Eylül 2026

Kullanıcı yalnız adım 1'i onayladı; video istemiyor. Adım 2 ve animasyon istekleri yeni onay bekliyor. [Gerçek oyun görüntüsü](QA/BATHROOM_PLACEMENT_2026-09-11/bathroom-placement.png).

- Kum kabı `(-3.17891,0,-1.35)`, yaw270: arka yüz sol dekoratif duvar paneline 2 mm açıklıkla yaslı; giriş odaya bakar.
- Klozet `(3.29118,0,-1.30)`, yaw270: rezervuar arkası sağ panele 2 mm açıklıkla yaslı. Açık yaklaşma noktası yeni yöne taşındı; animasyon/temas noktası aynı.
- Çamaşır sepeti `(-1.525,0,2.27108)`, yaw0: bakım arabasının solunda; iki gerçek ön yüz Z1.94833'te hizalı. Gerçek sepet–araba aralığı 8.99 cm, sepet–duş aralığı 6.25 cm. Bu aralıklar geçiş koridoru değil; ön girişler açık.

Kaynak `BathroomArrangementProfile`, katalog ve sahne aynı üç pozu tutar. Yakın mobilya grubunun yalnız ilgili çiftlerinde ölçülmüş açıklık kullanılır; diğer odaların varsayılan payları aynı. Duvara yaslanan iki ürünün plan zarfı oda içinde kalır. Klozetin sağ kenardaki görünüm payı bu ürüne özel ayarlı. Sepetin doldurduğu eski ayna girişinin yerine bakım arabasının sağındaki açık giriş kullanılır; ayna hâlâ etkin, dekor dönüşümü bu adımda yapılmadı.

4/4 mevcut yerleşim EditMode testi, yerleşim doğrulayıcı 0 hata; üç taşınan eşyanın ve komşu araba/duşun .31 m giriş açıklığı başarılı. Yalnız üç katalog satırı ve banyo sahnesi değişti. Diğer yedi oda sahnesi, gerçek kayıt/recovery/CP2 aynı. Animasyon turu veya video çekilmedi; önceki animasyon sonuçları yeni yönlerin doğrulaması sayılmaz.

Unity kullanıcı incelemesi için ayrı QA kopyasıyla banyoda açık bırakılır. Önceki QA kapatılıp tercihler ve geçici font/CurrencyHud/EditorSettings çıktıları geri alındı. Kullanıcı yerleşimi onaylamadan adım 2'ye geçilmez. APK/arşiv/commit/push/yayın/kapatma yok.
