# Doğal basılı dönüşler — 14 Eylül 2026

Kullanıcı çıkış ve iniş sıçramalarını onayladı; sıçrama sonrası yana dönerken birlikte vuran patilerin daha doğal olmasını istedi. Hedef 75 dakika. Bu tur yaklaşık 70 dakikada tamamlandı; son belge saati 10:58 Türkiye.

## Değişen davranış

`CatSurfaceTurnMotion` artık çapraz pati çiftlerini aynı anda kaldırmaz. İç ön pati dönüşü açar; diğer patiler sırayla yer değiştirir. Gövde bütün dönüş boyunca yumuşakça ilerler; her basışta yeniden durup hızlanmaz. Ön ve arka basış süreleri farklıdır. Pati yayı eski 3,5 cm yerine adım uzunluğuna göre 0,9–2,2 cm aralığındadır.

Baş en çok 6° dönüş yönüne bakar; en çok 6 mm ağırlık aktarımı eşlik eder. Kaynak Idle pozu hafifçe ilerler. Eklem boyları ve kedi ölçeği değişmez; basılı patiler gerçek destek üzerinde tutulur. Dar çadırda dirsekler yana açılmak yerine geriye katlanır. Çadırın gerekli alçak duruşu ve 20 cm çıkış yayı korunur; açık yön artık kedinin o anki yönünden değil eşyanın girişinden alınır.

Tekli koltukta 10 cm, asılı koltukta 15 cm açık tarafa adım payı vardır. Bu koltuklar kalça kenarı geçmeden geriye çekilmez. Bacaklar gövdenin altına döndükçe çömelme azalır; sonraki sıçramadan önce kısa basılı toparlanma vardır. Bitki rafının gerçek tahta desteği ve son basılı duruşu korunur.

**Beğenilen sıçrama korunmuştur:** `CatJumpMotion.cs`, `CatActivityAnimation.cs`, sıçrama açıklığı profilleri ve yedi özgün kedi FBX'i bu tur başlangıcıyla aynı. Havada yeni dönüş veya yeni taşıma yok. Oda yerleşimleri/model dosyaları için yeni üretim yapılmadı. Değişen çalışma kodu yalnız `CatSurfaceTurnMotion.cs`; ölçümlü regresyon kontrolleri `JumpContinuityTests.cs` içinde.

## Son doğrulama

- **9/9 benzersiz native test**, **26/26 EditMode**, proje denetimi **0 hata / 0 uyarı**. Son testlerin tek tek kaynakları `native-final-manifest.json` içinde. `native-verified` dosyasındaki iki ara başarısızlık, `final-acceptance` içindeki son 2/2 ile kapanır; ara sonuçlar son kabul değildir.
- Sekiz odada **39 sıçrama rutini**: tek tamamlanma ve açık çıkış. Bunların 34'ünde toplam **40 basılı dönüş** var; kalan rutinler basış sonrası yön değiştirmiyor. Ölçülen **1.338 adım içi karede**, diğer üç patinin 2,5 mm'den fazla kayması veya aynı anda birden fazla pati hareketi görülmedi. Basış sınırındaki kareler bu ayrı ölçüme dahil değildir.
- On ırk × 15/30/60 fps: 30 özgün sıçrama/iskelet çevrimi. Üç yüksek saksıda 30, havlu dolabı ve oyuncak farede onar çevrim. Tekli koltuk/çadır/asılı koltukta ayrıca **30 ırk/rutin** basış ve sıçrama kontrolü. Çadır/bitki rafında **20 gerçek pati desteği** çevrimi. Duraklatma, iptal ve kontrol sahipliği başarılı.
- Dört dar eşya × on ırk: **40 gövde/kol incelemesi** tamamlandı. Çadırın geniş kalça probu bazı karelerde uyarı üretir; aynı bölgelerde gerçek görünür ağ köşeleri ayrıca ölçüldü. En büyük ölçüm **1.17 mm**, siyah Oriental'da yaklaşık sıfır. Bu örnekleme, tüm üçgenlerin matematiksel çakışmazlık kanıtı değildir.
- Sekiz odada seçili **10 gerçek oyun düğmesi**, 10/10 tek tamamlanma/açık çıkış/kontrol iadesi. Son kanıt `visuals-final`. Bu, bütün 75 eşya düğmesinin yeniden tarandığı anlamına gelmez. Önceki salon tablo giriş bulgusu bu kapsamda değiştirilmedi.

[Galeri](QA/NATURAL_SURFACE_TURN_2026-09-14/index.html): **70 gerçek Unity PNG**. On eşyada ardışık basışlar ve bitiş duruşu; kaydırıcıyla kare karşılaştırması. Video yok. Bütün oda eşyalarının on ırkla tam matrisi veya fiziksel telefon performansı bu turda ölçülmedi. Yeni dönüşün son görsel onayı kullanıcıda.

## Kayıt ve editör durumu

Üç gerçek kayıt, 14 sahne dosyası ve yedi özgün kedi FBX'i başlangıçla aynı (`integrity-final.json`). Ana/recovery SHA256: `97590845CADE70487CB0458DF1CA6EE932C21ACB1A91A16A4626B091F4A9F458`. CP2: `03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D`. Eski tarihsel kayıt geri yüklenmedi.

16 tercih geri yüklendi. QA/Play/çekim/derleme kapalı; zaman 1. Normal üç sahne temiz; tek kedi/kamera/ses dinleyici, salt okunur ön izleme açık. Yazı tipi ve EditorSettings deneme yan etkileri bu turun başlangıç kopyasına döndürüldü; önceden var olan çalışma değişiklikleri korundu. APK, arşiv, commit, push ve yayın yapılmadı. QA çıktıları Git dışında.
