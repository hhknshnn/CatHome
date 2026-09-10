# Salon dışındaki odalarda görsel uyum ve etkileşim — 7 Eylül 2026

Kullanıcı, salon dışındaki odaların eski görünen bölümlerinin salon seviyesine yaklaştırılmasını, ölçeklerin korunmasını ve etkileşimsiz kalan durumların düzeltilmesini istedi. Mini oyunlar sonraki çalışmanın konusudur.

## Görsel değişiklikler

Önceki ortak yerleşim kuralı ve CAT koleksiyonunun yalnız salonda kalması kararı korundu. ROOM/CAT ürün modelleri, fiyatları, sahiplik kimlikleri ve ürün ölçekleri yeniden yazılmadı. Son hareket kontrolünde üst kattaki pikabın kullanım alanı kadraja sığmadığı için yalnız üst kat planı yeniden hesaplandı; pikap arka orta alana, kitap yığını boşalan ön sol alana geçti. Diğer ürün konumları korundu. Görsel farkın belirgin olduğu sabit mimari üzerinde çalışıldı:

- Banyo, mutfak ve yatak odası için pahlı ivory kapı, iki girintili mint panel, ince metal şerit ve gerçek kol modeli; mevcut kapı alanına yerleşir.
- İç odaların alt duvarlarında salonla uyumlu çerçeveli paneller; kapı boşluğu kapatılmaz. Özellikle üst katın geniş düz duvarları işlendi.
- Ahşap zeminlerde birbirine daha yakın sıcak meşe tonları; avluda daha yumuşak taş, bahçede dengeli yeşil. Banyo ve mutfağın karo, yatak odasının ahşap kimliği korunur.
- Bahçe, avlu ve balkon bitkilerinde dolgun, yüzeyine yakın yaprakları olan taçlar. Uzun çit bitkileri eski kutularının içinde bölümlenir. Yeni görseller özgün mimari nesnenin altında kalır; ağaç engeli ve kuş hedefinin ilişkisi kaybolmaz. Hafif yaprak hareketi azaltılmış hareket ayarında durur.
- Diğer yedi oda, salondaki `.38` şiddetli gölgesiz yumuşak dolguyu kullanır; mevcut ana/yardımcı ışıklar korunur. Zeminler gerçek gölgeleri alır.
- Dış mekân fotoğrafı artık yumuşak dolgu ışığını ikinci bir güçlü ana ışığa çevirmiyor. Fotoğrafın ek key/fill şiddetleri `.7/.35` oldu; ana yönlü ışık `1.05`, `ReferenceSoftFill` kendi `.38` değerinde kalır. İç odaların mevcut ışık düzeni kullanılmaya devam eder.

Kaynaklar: `ArtSource/Blender/PremiumFurniture/build_room_shell_details.py`, üç FBX `Assets/Art/RoomShellPolish/Models`, `HomeRoomPremiumFinishBuilder`, `RoomFoliageMotion`. Blender yalnız headless çalıştırıldı. Ortak mağaza materyalleri boyanmaz; mimari kendi `Finish_*` malzemelerini kullanır. Yeniden uygulama türetilmiş renkten bir kez daha türetmez; özgün materyali çözer, böylece renk solması ve çoğalan materyal adları önlenir.

## Etkileşimde bulunan ve düzeltilen durumlar

1. `SitLookActivity` eski tek vuruştan `.58` saniye sonra oturma pozuna dönüp kalan sürede bekliyordu. Artık üç davranış evresi kullanır; pati oyununda iki taraf dönüşümlü, uzak/asılı nesnelerde oturarak bakış korunur. Ulaşılamayan tavana yapay temas yaptırılmaz.
2. Eski `CatMovement.SuggestLookDirection` komutu yalnız boşta çalışıyordu; aktivite kilidi sırasında çağrılması baş takibi üretmiyordu. `CatFurnitureGaze` gerçek baş kemiğini sınırlı açılarla hedefe çevirir. Kedi kökünü veya kemiğin uzunluğunu değiştirmez; kaynak poz her karede, bitişte ve iptalde geri yüklenir.
3. `PerchNapActivity`, `CanopyNapActivity`, `TowelNestActivity` ve `OvenWarmthActivity` yüksek enerji yüzünden kullanımı reddetmez; kısa dinlenme her zaman mümkündür. Dinlenme başlangıç bedeli sıfırdır. Mobilyada dinlenilen gerçek poz boyunca enerji `.75/s` artar; tamamlanma ödülleri mevcut değerlerini korur. Dar taburedeki oturarak dinlenme de bu kapsamdadır. Enerjisi sıfır kedi bu ürünleri kullanabilir.
4. Tok veya susamamış kedi, yemek/su ürününü tamamen reddetmek yerine koklayıp inceler. `InspectingOnly` durumunda ihtiyaç ödülü verilmez ve yemek/içmek görevi ilerletilmez. Aç/susamış durumdaki yeme/içme rutini korunur. İlerleme etiketi incelemeyi belirtir.
5. Pikap, kağıt rulosunun X dönme eksenini kullanarak diski masa içinden çeviriyordu. `PaperSpinActivity` `RecordSpin` için dikey Y eksenini kullanır; plak yatay kalır. Kağıt rulosunun eski ekseni korunur. Pikabın bitiş mesajı da kağıtla ilgili metin yerine kendi etkileşimini anlatır.
6. Kadraj denetimi eskiden `.35 m` gövde merkezinin yalnız `.18 m` altını ölçüyordu; ayakların bulunduğu son `.17 m` denetlenmiyordu. `FitsPlayerView` artık zemin seviyesini kapsar. Son fiziksel giriş düzeltmesi de aynı kuralı kullanır; yeni odalara eski kadraj hatası taşınmaz.

Mevcut salıncak, tırmalama, çıkma/inme, devirme, kazma ve su etkileşimleri korunur. Bütün ürünlerin ayrı kimliği ve gerçek girişleri denetlenir; dekoratif mimari mağaza eşyası veya yeni oyun ürünü sayılmaz.

## Kalıcı üretim kuralı

`HomeRoomShellVisualPolishBuilder.Apply` ortak bitişi çağırır; mevcut oda üreticileri yeniden çalışınca yeni görünüm kaybolmaz. Bilinmeyen yeni oda ortak iç mekân bitişini kullanabilir. Eşyaların yerleşim ve oran sözleşmesi [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md); bu çalışma onu değiştirmez. Renk ve ışık düzenlemesi için ürünlerin kök ölçeği kullanılmaz.

## Doğrulama

Son tam EditMode: **453/453 geçti**. Tekrarlı üretimde konum, ölçek, mimari collider durumu ve renk korunumu; yeni oda için ortak bitiş, ayakları kapsayan kadraj ve pikabın yatay kalan diski ayrıca sınandı.

- İlk geniş PlayMode koşusunda 15/16 test geçti. Yeni dolu-ihtiyaç testinin eksik test göstergeleri ve bahçedeki ikinci etkinliği saymayan varsayımı düzeltildi; iki test yeniden 2/2 geçti. Ürün başına bir etkinlik varsayılmıyor: 70 üründe 71 ayrı etkinlik tamamlandı.
- Son pikap/kadraj düzeltmesinden sonra banyo ve üst katın 200 ürün/ırk rutini, dolu ihtiyaçlar, sıfır enerjide dinlenme/iptal ve üç yakınlık kontrolü **7/7 geçti**. Sonuçların birleşiminde 16 ayrı PlayMode testi başarılı; sekiz güncel CSV toplam **800/800** başarılı ürün/ırk rutini içerir. Yakınlık kontrolü 80 ürünü ve bahçedeki ek rutini kapsar.
- Son hareket kaydında bulunan pikap düzeltmeleri sonrası oda ve mağaza önizlemeleri yeniden üretildi. 108 gerçek ürün kartı toplu görselde gözden geçirildi. Ürün modellerinin ölçekleri korunur.
- Canlı tur yedi odada 1920×1080 ve 1440×1080 olmak üzere 14 kare kullanır. Görünür düğmelerde çakışma/taşma/tıklama denetimi temizdir; her geçişte kamera/dinleyici/EventSystem 1/1/1. Yedi HD hareket kaydı gerçek simülasyon hızında 24 fps olarak sunulur; üst kat kayıtları son düzeltmelerden sonra yenilendi.
- `LevelContentValidator`: 0 hata / 0 uyarı. Test başlangıcında açık kalan ek sahne, native test yerine oyun bootstrap'ını açabiliyordu; `HoldFastPlayModeForManualTestRun` açık başlangıç sahnesi bağını da temizler. Son gerçek test koşuları normal tamamlandı; geçersiz hazırlık oturumu sonuç olarak sayılmaz.

Galeri: [gerçek oda görüntüleri ve hareket kayıtları](QA/ROOM_POLISH_2026-09-07/index.html). Test XML'leri, güncel 800 satırlık matris, temas yakın çekimleri, 71 dolu-ihtiyaç etkinliği ve canlı ekran ölçümleri bu dizindedir. Bu kanıt Unity Editor kapsamındadır; gerçek mobil cihaz performansı bu geçişte ölçülmedi.

Kanıt dizini `Docs/QA/ROOM_POLISH_2026-09-07`. Bütün native testler ve canlı kayıtlar `UiQaTestSession` kopyasında yürütülür. Gerçek oyuncu dosyasının özeti ayrıca karşılaştırılır. Git commit/push kullanıcı tarafından yapılır.
