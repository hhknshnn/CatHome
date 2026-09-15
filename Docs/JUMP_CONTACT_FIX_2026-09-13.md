# Sıçrama, eşya teması ve düşüş yönü — 13 Eylül 2026

Kullanıcının inceleme sonrası “devam” onayıyla çalışıldı. Kapsam sekiz oda; çıkış sıçraması, eşyalardan geçme ve pati vuruşundan sonraki hareket. Başlangıç 01:05 Türkiye saati. İlk süre tahmini dar çadır/raf ve araba geçişlerinin gerçek fizik kontrolünde çıkan ek bulgular nedeniyle uzadı; kullanıcıya kalan süre güncellendi.

## Yapılan düzeltmeler

Yükselen sıçramada yatay ilerleme doğrusal, dikey uçuş yüksekliğe göre yerçekimli. Kök yönü havada ve özgün toparlanma boyunca sabit. Yön değiştirme gerçek basıştan sonra çapraz pati adımlarıyla yapılır. Dar ve eğimli yüzeylerde gerçek üçgenlerden destek seçilir; kemik boyu ve kedi ölçeği değişmez. Çadırda küçük adımlı dönüş girişe doğru açılır ve yatağa geri döner. Raf üzerinde son basılı duruş korunur.

Özgün `|Jump`, yedi kaynak kedi FBX dosyası ve aşağı inişin mevcut yatay eğrisi/toparlanma zamanlaması korunur. Engel bulunan eşyalarda kalkış noktası veya basılı hazırlık yönü ayarlanır. Asılı koltukta yükselme/iniş açıklığı ayrı seçilir; avlu kemeri, kitaplık, divan, saksılar ve dar raflarda gövde yolu kullanılır. Havada yeni yönlendirme yapılmaz.

Kupa ve plak eylemlerindeki ileri atılan gövde yerine mevcut sabit gövdeli yan pati klibi kullanılır. Gerçek yakın el, gerçek eşya kenarına erişir; temas kısa süre tutulur. Klozette son yaklaşım çalışma yönünde tamamlanır. Dolap kapısında temas, süs çıkıntısının arkasına değil gerçek ön üçgene konur. Üç yüksek saksıda çalışma ekseni, ölçülmüş toprak alanına göre seçilir. Komodinde düşüşten sonra gereksiz oturup kalkma kaldırılır.

Araba eyleminde gövde sekmesi ve arabanın kediye doğru geri salınması kaldırılır. İki gerçek pati teması, tekerlek ekseninde toplam 24 cm ilerleme başlatır. Çapraz itiş, tekerlek eksenine yansıtılır; işaret gerçek temas tarafına göre seçilir. Yaklaşımın ara noktaları çalışma yönüne gereksiz dönmez. Ocak yanındaki dönüş açık koridordan yapılır. Kedi başlangıç alanına yürüdükten sonra eşya yerine döner.

Bardak, kupa ve kitap gerçek temas eden elden uzaklaşır. Masa kenarını terk etme uzaklığı gerçek yüzey sınırı ve eşya boyutundan hesaplanır. Dünya koordinatındaki düşüş, ölçekli ebeveyn nedeniyle ters yöne veya yanlış mesafeye kaymaz. Temas sayısı, duraklatma, iptal ve tam geri yükleme korunur. Meyve sepeti, yemek masası ve salon sehpasının mevcut hareketleri ayrıca ölçüldü; vuruş sonrası ana parçaları doğru yönde ilerledi. Serbest meyvelerin saçılması ayrı davranıştır.

## Dar eşya geometrisi

Yıldızlı çadırın alçak giriş süsü yukarı ve dar alana alındı. Çadırın ana gövdesi, direkleri ve tabanı yeniden ölçeklenmedi. İç nokta ve yüzey yönü kaynak üreticide/prefabda birlikte tutulur. Balkon bitki rafında üst nokta 8 cm açık tarafa alınır; oturma yerine basılı duruş vardır, pati hedefleri yaprak tepelerinden seçilmez.

Avlu fıskiyesinin üst gövde/su katmanları daraltıldı; alt leğen ve temas/destek yüksekliği aynı. Blender kaynakları ve FBX çıktıları tutarlı; bu iki eşyanın kartları ve ilgili oda ön izlemeleri yenilendi. Sahne yerleşim dosyaları değişmedi; prefab düzeltmeleri sahnelere aktarılır.

## Kanıtın sınırları

İlk kapalı küre probları geçersizdi. Son taramalarda açık tetikleyici, bilinen iç içe/ayrı cisimlerle pozitif ve negatif kontrolden geçti. Kemik çevresindeki küreler tarama içindir; otururken minder çevresindeki kalça örtüşmesi gerçek görüntüyle ayrılır. Yalnız bu kürelerden “bütün gövde her karede sıfır kesişme” sonucu çıkarılmaz.

Ara çadır FBX aktarımı 100 kat küçük model üretmişti. Gerçek pati desteği testi bunu yakaladı; birim seçeneği düzeltildi, gerçek ölçü 1,028 × 1,523 × 1,028 m olarak doğrulandı. Bu ara modelle alınan geometrik sonuçlar son kabul kanıtı değildir. Son çadır testleri ve görselleri düzeltilmiş gerçek boyuttadır.

Pati desteği gerçek deri ağı ve pati çevresindeki 5 cm genişliğindeki yüzey alanından ölçülür. Tek kemik ışınının yastık üzerindeki kabartma kenarına denk gelmesi destek yok demek değildir. Arabanın çapraz itişi serbest eşya gibi doğrudan el–merkez doğrultusunu izlemez; ileri yön ve tekerlek ekseni birlikte doğrulanır.

## Son doğrulama

Son benzersiz native testler **19/19**, hedefli EditMode **26/26**, proje denetimi **0 hata / 0 uyarı**. Native testlerin en son geçerli dosyaları `QA/JUMP_CONTACT_FIX_2026-09-13/native-final-manifest.json` içinde tek tek listelenir. Ara başarısız testler son sonuç değildir.

- Sekiz odada 39 sıçrama rutini: 39 tek tamamlanma ve 39 açık çıkış. 38 yükselen sıçramada havadaki kök yön değişimi **0°**. `final-native-verified` son 8/8 testi içerir.
- On ırk × 15/30/60 fps: 30 özgün iskelet/sıçrama çevrimi. Üç yüksek saksı × on ırk: 30 çevrim. Havlu dolabı ve salon oyuncak faresi: onar ırk çevrimi.
- Dokuz hedef temas eylemi × on ırk: **90 gövde/kol açıklığı çevrimi**. Klozet, komodin, dolap, üç yüksek saksı, balkon kupası, plak ve mutfak arabası.
- Üç düşürülen eşya ve araba × on ırk: **40 gerçek temas/yön/tam reset çevrimi**. Çadır ve bitki rafı × on ırk: **20 gerçek pati desteği çevrimi**.
- Rulo, plak, balkon/üst kat pati eylemleri, basılı dönüş ve havlu evrelerinde duraklatma/iptal/kontrol sahipliği de denetlendi.

Bunlar hedefli kapsamların sayılarıdır; bütün oda eşyalarının her birinin on ırkla eksiksiz tarandığı anlamına gelmez. Telefon performansı ölçülmedi. Kullanıcı görsel incelemesi beklenir.

EditMode ilk turunda eski test banyo aynasından eylem bekliyordu. Önceden onaylanan dokuz dekor ürününün açık listesi güncellendi; oyuna eylem eklenmedi. Son 26/26 bu test düzeltmesinden sonradır.

Gerçek düğme turunun ilk hazırlığı, deneme sahipliği nedeniyle sıraya giren koleksiyon kutlamalarıyla örtüldü. `buttons` bu başarısız hazırlığı saklar. Kutlama bileşeni yalnız deneme Play oturumunda kapatıldı; son kanıt `buttons-verified`. Salon envanterine sabit koltuk/sehpa dahil edilerek güncel dokuz eylem beklentisi düzeltildi.

## Sekiz oda / gerçek düğmeler

Toplam **74/75 farklı eylem** tamamlandı; tek tamamlanma, açık çıkış ve kontrol iadesi birlikte kontrol edildi. İlk tam tur 72/75 idi. Lambader açık taraftan tekrarlandığında geçti. Bahçedeki ücretsiz kuş izleme 250 bağ puanı ister; deneme kaydının 156 puanı yalnız QA kopyasında 250'ye çıkarıldıktan sonra geçti. Kilit, oyun hatası değildi.

| Oda | Son başarılı eylem |
| --- | ---: |
| Salon | 8/9 |
| Banyo | 9/9 |
| Mutfak | 11/11 |
| Yatak odası | 8/8 |
| Bahçe | 10/10 |
| Balkon | 9/9 |
| Avlu | 9/9 |
| Üst kat | 10/10 |

**Açık konu:** salon `room.modern-painting` için yakın/açık giriş bulunamıyor. Önceki checkpoint'te de vardı. Tablo ve onaylı salon yerleşimi değiştirilmedi. Lambader ilk yaklaşım noktasında başlamadı; açık taraftan tek çevrim tamamlandı. Lambaderin her yaklaşım noktasından çalıştığı iddia edilmez.

Son düğme kanıtı `buttons-verified`; lambader tekrarı `details-verified/room.floor-lamp-report.json`, kuş `bird-unlocked/report.json`. Birleştirilmiş sonuç `buttons-summary.json`. Yenilenen yakın çekimlerin tamamlanan eşya çevrimleri `details-verified` raporlarında. Önceki duvar arkasına giren sabit yakın çekim kamerası görselleri galeriye alınmadı; oda içinden çekilen yeni yakın görüntüler kullanıldı. Bu kamera yalnız QA çekimine aittir.

[Yerel galeri](QA/JUMP_CONTACT_FIX_2026-09-13/index.html): **136 gerçek PNG**, video yok. Sıçrama, basılı dönüş, temas ve iniş kareleri oda oda. Bütün görsel yolları doğrulandı.

## Kayıt ve teslim

Son durum: 2026-09-13T03:31:09.2371472Z (Türkiye saati yaklaşık 06:31). Çalışma yaklaşık 5 saat 26 dakika sürdü.

- Üç gerçek kayıt, 14 sahne dosyası ve yedi özgün kedi FBX'i bu turun başlangıcıyla aynı. `integrity-final.json`.
- Ana/recovery SHA256: `58D6848FD22D6508D6D91A475E8521F5FCF861715482EA68D6E7466EF2185A53`.
- CP2 SHA256: `03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D`.
- 16 tercih geri yüklendi. QA/Play/çekim/derleme kapalı; zaman 1. Normal üç sahne temiz; tek kedi/kamera/ses dinleyici. Gerçek kaydın salt okunur editör ön izlemesi açık. `preferences-restored.json`, `editor-final.json`.
- Son mimari denetim 0/0: `validator-final.json`. Ön izleme geçici kamera görüş açısını ekran oranına göre değiştirir; kaydedilmiş sahne denetiminde ön izleme temizlenip sonra geri açıldı. Bu geçici görünüm sahne dosyasına yazılmadı.
- Denemenin yazı tipi/EditorSettings yan etkileri tur başlangıç kopyasına döndürüldü. Önceden var olan oyun değişiklikleri korunur; toplu Git geri alma yapılmadı.
- APK, arşiv, commit, push ve yayın yok. QA dosyaları Git dışında. Son görsel davranış kullanıcı incelemesinde.

