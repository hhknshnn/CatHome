# Üst HUD — dört Blender ikonunun yenilenmesi, 28 Eylül 2026

Kullanıcının bu turdaki kapsamı yalnız coin, profil altındaki gold badge ring, diamond ve food. Mevcut HUD yerleşimi korundu. Başlangıç 20:17:01 UTC; kapanış QA/closure.json. APK, commit, push, yayın yok.

Blender 5.2 Cycles, 128 sample ve denoise ile dört şeffaf 768×768 master render alındı. Kaynak: `ArtSource/Blender/TopHudPremiumFour/build.py` ve `premium-four.blend`. Raster üzerinde çizim veya ImageGen kullanılmadı. Son üretim kaydı `QA/TOP_HUD_PREMIUM_FOUR_2026-09-28/render-delivery.log`.

- Coin: çift cilalı altın kenar, oyuk altın alan, tırtıllı yan yüzey ve küçük kenar tanecikleri; birleştirilmiş üç boyutlu sevimli kedi kabartması, amber göz/burun, açık emaye yanaklar, küçük statik parıltılar.
- Gold badge: kalın bombeli altın halka, koyu iç oyuk ve iç parlak kenar; dört ayrı parmaklı büyük pençe kabartması.
- Diamond: üçgen fasetli sekizli brilliant kesim, ayrık crown/girdle/pavilion, doygun cyan ve mavi yüzeyler, ince cilalı bevel ve iki beyaz glint.
- Food: yuvarlatılmış mercan porselen gövde, krem ağız ve ince altın şerit, dolgun kavrulmuş mama parçaları, dört parmaklı temiz ön pençe kabartması.

Yalnız `Assets/Resources/TopHudExact/{coin,badge,diamond,food}.png` ve bunların dört mevcut metasındaki sprite crop bilgileri değişti. Mevcut GUID ve sprite kimlikleri korundu. 64×64 coin/diamond, 36×36 badge ve 82×84 food kutuları aynı. Görünür siluetler doğal oranlarında bu kutulara sığar. Kaynak C#, font, sahne, prefab, ekonomi ve binding değişikliği yok.

## Görsel doğrulama

Gerçek Unity Game View 1920×1080 **Edit Mode önizlemesi** kaydedildi; bu tur Play açılmadı ve canlı oynanış testi yapılmadı. Önizlemenin mevcut 100/100/100 ve 0/0 değerleri değiştirilmedi. Önce/sonra aynı kamera, oda ve kedi pozu kullanıldı. Son görüntü `final-game-view.png`, üst HUD kırpımı `final-top-hud.png`; `before-after-top.png`, `before-after-difference.png`, `four-icons-review.png` aynı QA klasöründedir.

20 ana RectTransform ve metin kaydı önce/sonra aynı, metin taşması 0. Dört ikonun sınırları dışında yalnız iki pikselde kanal farkı 2/255 üstünde; tam karede küçük render yuvarlama farkları bulunduğu için piksel piksel bütün ekran eşitliği iddia edilmez. Geometri sabitliği `visual-geometry-check.json` ile kaydedildi. 36/64/82 px küçültme ve gerçek HUD görüntüsü gözle incelendi. Fiziksel telefon testi ve kullanıcı estetik kabulü yok.

İçe aktarmadan sonraki ilk Game View yakalaması yeni ikonları göstermedi. Yüklenen sprite ve GPU dokusu doğrulandı; Unity penceresi öne getirilip gerçek redraw yapıldığında dört ikon doğru göründü. Son kanıt yalnız yenilenmiş `final-game-view.png` ve bundan üretilen karşılaştırmadır. Bu geçici önizleme durumu için ürün kodu değiştirilmedi.

## Koruma

Bu turun güncel başlangıcındaki 9.295 okunabilen dosyadan 9.287 aynı; yalnız yukarıdaki sekiz asset/meta farklı. Eksik dosya ve son okuma hatası 0. Bir eski animasyon başlangıçta yine okunamadığından o dosya için hash iddiası yok. Gerçek kayıtlar ve 33 tercih aynı. Son üç normal sahne temiz, Play/derleme kapalı, Unity açık. Console araç sorgusunda hata/uyarı 0. Yeni animasyon ve APK üretilmedi. Kullanıcının istediği dört öğeden sonra duruldu.
