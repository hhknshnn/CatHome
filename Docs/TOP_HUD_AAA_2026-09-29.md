# Top HUD — Blender final art polish, 29 Eylül 2026

Kapsam: kabul edilmiş üst HUD yerleşiminde yalnız sanat kalitesi. Referans `C:/Users/HAKAN/Desktop/HUD-Ref.png`. Yeni yerleşim, APK, Play, telefon, commit/push/yayın yapılmadı.

## Teslim

- Gerçek 1920×1080 Unity **Game View / Edit Mode önizlemesi**: `QA/TOP_HUD_AAA_2026-09-29/final-game-view.png`.
- Referans/final yan yana: `QA/TOP_HUD_AAA_2026-09-29/reference-vs-final.png`. İki kare aynı gösterim genişliğine ölçeklenir; referansın farklı oda doluluğu ve kedisi projeye uygulanmadı.
- Gerçek piksel boyutunda önce/final üst HUD: `QA/TOP_HUD_AAA_2026-09-29/before-vs-final-top.png`.
- Dosya listesi: `QA/TOP_HUD_AAA_2026-09-29/final-assets.json`.

Blender 5.2 / Cycles ile altı ikon ve on bir üst HUD yüzeyi render edildi. Kullanıcının açık Blender sahnesi değiştirilmedi; ayrı arka plan stüdyoları kullanıldı. Bir ilk Unity değerlendirmesi ve ardından bir düzeltme aşaması yapıldı. Düzeltme aşamasında küçük boyutlu yansıma/faset ve görünmeyen elmas parıltısı için ek hedefli renderlar alındı. Raster üzerinde ikon boyama/ImageGen yok; Python yalnız ölçüm, sprite import sınırları ve ekran karşılaştırması için kullanıldı.

Mama: mercan porselen, altın ağız detayı, daha büyük ve okunur mama parçaları. Su: sürekli bombeli damla, uzun stüdyo ışığı. Enerji: geniş bevel'li sedef hilal ve tek altın yıldız. Coin: kalın işlenmiş altın kenar, oyuk alan, dış kenar detayları ve bombeli kedi kabartması. Diamond: kapalı üç boyutlu geniş fasetler, cyan/mavi ayrımı, **tek** kontrollü parıltı. Rozet: saten altın kabartma, oyuk alan, cilalı omuz. Yüzeyler: daha güçlü mercan/cyan/violet ayrımı, metal kenar ve kavisli emaye; menü ve para kutuları aynı ailede. Bar altına ek beyaz çizgi eklenmedi.

## Final kaynakları

Unity sprite kökü: `Assets/Resources/TopHudExact/`.

- İkonlar: `food.png`, `water.png`, `energy.png`, `coin.png`, `diamond.png`, `badge.png`.
- İhtiyaç yüzeyleri: `panel-food.png`, `panel-water.png`, `panel-energy.png`, `well-food.png`, `well-water.png`, `well-energy.png`.
- Diğer üst yüzeyler: `profile.png`, `portrait-ring.png`, `currency.png`, `plus.png`, `menu.png`.

Düzenlenebilir Blender kaynakları proje üst klasöründe:

- `ArtSource/Blender/TopHudAAA/icons-final.blend`
- `ArtSource/Blender/TopHudAAA/panels.blend`

Yeniden üretim: aynı klasörde `build-icons.py` → `refine-icons.py` → `finish-sparkle.py`; paneller `build-panels.py`. Renderlar önce QA/renders içine çıkar; Unity'ye aktarım QA/install.py ile mevcut GUID ve sprite kimliklerini koruyarak yapılır. `icons.blend` ilk geçiştir; teslim kaynağı **icons-final.blend**.

## Doğrulama ve sınırlar

Yalnız `StorybookHudDetails.cs` içindeki üç ihtiyaç ikonunun kutusu ve optik ofseti değişti. Mama 66×68, su 76×78, enerji 78×80; 96×96 yuvalar, bütün panel ve buton ölçüleri aynı. Görünen alfa alanları sırasıyla 2989 / 2878 / 3074 px², en büyük/küçük oranı 1,068. Alfa ağırlık merkezlerinin yuva merkezinden kalan sapması en çok **0,015 px**. Bu metrik görsel ağırlığın yardımcı ölçüsüdür, öznel sanat kalitesinin kanıtı değildir.

28 izlenen RectTransform/metin kaydından yalnız üç ihtiyaç ikonu farklı; diğer 25 aynı. Coin/diamond 64×64 ikon, değer ve artı hizası aynı. Sekiz incelenen HUD metninde taşma yok. Dört üst butonun aktif/dokunulabilir durumları ve hedef görselleri doğrulandı; bu tur gerçek tıklama/PlayMode işlev testi yapılmadı. Ekonomi/binding/bar kodları değişmedi. Son Console sorgusu 0 hata / 0 uyarı.

Oda ve joystick kare bölgelerinde yalnız en çok 3/255 render yuvarlama farkı var. Alt HUD'da `Kedi komutları` yazısı bölgesinde yeniden çizim sonrası 897 piksel 3/255 üzerinde farklı; alt HUD kaynakları, fontları ve sahneleri byte aynı. Bu nedenle tüm alt HUD'ın piksel piksel aynı olduğu iddia edilmez.

Güncel başlangıçtaki 9.299 okunabilen dosyadan 9.275 aynı; değişenler **17 PNG + 6 mevcut sprite meta + 1 sunum C#**. Eksik dosya ve son okuma hatası 0. Bir eski animasyon başlangıçta okunamadı; tüm varlıklar için eksiksiz hash koruma iddiası yok. Gerçek kayıtlar, sahneler, fontlar, ProjectSettings ve kapsam dışı oyun kaynakları aynı. Tercih başlangıcı için denenen registry yolu bulunamadı: bağımsız tercih eşitliği iddiası yok; tercih yazımı ve Play yapılmadı.

Üç normal sahne temiz; Play/derleme kapalı; Unity açık. Fiziksel cihaz veya canlı oynanış kabulü değildir. Görsel sonuç kullanıcı incelemesine sunulur; AAA kalitesi veya kullanıcı estetik onayı nesnel olarak doğrulanmış sayılmaz. İstenen dosyalar teslim edildikten sonra duruldu.
