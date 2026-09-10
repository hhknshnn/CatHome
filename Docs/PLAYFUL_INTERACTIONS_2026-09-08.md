# Yakın etkileşimler ve canlı oyun sunumu — 8 Eylül 2026

Bu geçiş kullanıcının ekran görüntüleriyle verdiği düzeltme isteğini uygular. [203 görsel ve dört videonun galerisi](QA/PLAYFUL_INTERACTIONS_2026-09-08/index.html) ve [bütün görsellerin ZIP paketi](QA/PLAYFUL_INTERACTIONS_2026-09-08/Tum-Gorseller.zip) hazırdır. Önceki Modern/Joyful galerileri tarihsel kayıttır.

## Davranış ve adlar

- Ses sistemi ve oyun konsolu satın alınabilen dekor olarak kalır; kedinin İzle/Oyna eylemleri kaldırılır. Ev içindeki eski Mouse Hunt istasyonu kaldırılır. Cat Catch ayrı mini oyun olarak korunur.
- Eski `level3_mouse` görevi, bir Cat Catch turunda en az üç gerçek yakalama ile tamamlanabilir. Görev kimliği, kayıt tipi ve ödülü korunur. Artık olmayan ev etkinliği bağ hediyesi listesinde gösterilmez.
- Ürün adları ortak `GameProductCopy` üzerinden sadeleştirilir: TV, Kitaplık, Kitaplar, Tablo, Buzdolabı, Gardırop gibi. HUD aynı kaynaktan `TV izle` üretir. Mağaza ve kayıtlardaki ürün kimlikleri değişmez. Alt gezinme ve komut sekmesi `Kedi komutları` olarak adlandırılır.

## Erişilebilir yakın çevre

`CatActivityApproach`, görünür modelin kendi eksenlerindeki hacmini ölçer. Kedinin bulunduğu açık zeminden en yakın yüzeye en fazla 0,80 m yatay mesafe gerekir; görüş, gerçek engeller ve 0,27 m gövde açıklığı ayrıca kontrol edilir. Uzakta seçilmiş eşya bu şartı atlayamaz. Yan yana eşyalar için en yakın geçerli aday ve mevcut küçük tutma toleransı korunur.

Açık oyuncaklar ve izleme eylemleri oyuncunun yaklaştığı tarafı kullanır. Tüy, fare ve teker oyuncakları gerçek temas noktasına erişen kısa, engelsiz yolu seçer; arka kemeri olan fare yuvasında gerekirse en yakın açık kenara dolaşır. Tırmalama direği taban genişliğini gözetir. Top oyunu açık yöne doğru gerçek pati vuruşuyla başlar ve topun süpürülmüş çarpışma denetimini korur. Kapalı yatak ve tünellerin fiziksel girişleri, koltukların gerçek destek yüzeyleri korunur.

Koltuktaki iki dekoratif yastık dinlenilen minderden diğer kol tarafına alınır. Koltuğun boyutu, oda planı ve kedinin destek yüksekliği değişmez; on ırkın son pozdaki gerçek mesh'i yastık hacimlerine karşı kontrol edilir.

## Ekranlar ve hareket

`PlayfulScreenBuilder`, `ModernScreenBuilder` sonrasında uygulanır. Dönüş panelinde mor–lacivert başlık, maskeli kedi portresi, ayrı bakım özeti, renkli ihtiyaç kartları ve belirgin ana eylem bulunur. Runner/Catch girişlerinde büyük oyun fotoğrafı, seçili kedi, kontrol adımları, rekor/can/hedef bilgisi ve renkli başlangıç düğmesi kullanılır. Blender'daki gerçek bonus modellerinin fotoğrafları Runner girişine eklenir.

Oyun HUD'unda skor, süre, jeton, mesafe ve bonus alanları ayrı renklerle okunur. Düğmeler basıldığında taban derinliği ve kısa hareket verir. Hareket azaltma, kapatma, devre dışı ve klavye odak durumları korunur. Türkçe Fredoka/Nunito atlasları kullanılır.

## Runner

- Blender kaynağı `ArtSource/Blender/MiniGames/PlayfulBonuses_Source.blend`: kaykay, oyuncak tren, paketler, ördek, halka yığını, çiçek arabası; ayrıca 2× skor yıldızı ve sürpriz hediye. Sekiz model tek palet malzemesi kullanır.
- Dışa aktarımda yalnız `PaletteUV` katmanı bırakılır. Metinden mesh'e çevrilen 2× işaretinin eski UVMap katmanı Unity'nin ilk renk kanalını bozuyordu; artık altın yıldız ve koyu yazı Blender ile eşleşir. Sekiz modelin ilk UV kanalı test edilir.
- Engeller karıştırılmış bir havuzdan seçilir; bütün çeşitler dolaşılırken aynı engel art arda seçilmez. Eski dokuz çevre varyasyonu ve satır başına en fazla iki engelli şerit korunur.
- Skor yıldızı on saniye boyunca o sırada kazanılan puanları ikiye katlar; eski puanları çoğaltmaz. Süre bittiğinde kazanılmış puan geri alınmaz. Duraklatmada süre durur. Jeton çarpanıyla birlikte çalışır; aynı para ödülü iki kez verilmez.
- Sürpriz hediye mıknatıs, kalkan, çift jeton veya skor yıldızı verir. Bonus aralığı 12–18 saniyedir.
- Coin ve bütün bonuslar hem engellerle hem diğer toplanabilir nesnelerle ayrı alan kullanır. Ölçü salınım, dönme ve büyümenin zarfını içerir; platform yüksekliği ve mıknatıs yolu ayrıca denetlenir. Planlanmış coinleri sessizce eksilten çözüm kullanılmaz.
- Cloud Code kaynağındaki skor doğrulaması yeni puan katkısını kabul edecek şekilde güncellenir. Yerel derleme ile canlı hizmete yayın ayrı işlemlerdir; bu geçiş canlı hizmete yayın yapmaz.

## Editör ve kayıt

`CatHomeEditPreview` kayıtlı koleksiyonu, seçili kediyi ve komut düğmesini Play kapalıyken geçici ön izleme olarak gösterir. Yerleşim hesabı yalnız planı okur; satın alma, kayıt yükleme veya kaydetme API'si çağırmaz. Geçici görüntü sahne kaydından, derlemeden ve Play girişinden önce kaldırılır. Asıl sahne değerleri geri konur.

Dar editör ekranında ihtiyaç göstergeleri Play'deki ikinci satır düzenine geçer; kamera aynı `HomeWorldViewport.FitFieldOfView` hesabını kullanır. Ön izleme kapanınca özgün HUD konumu/ölçeği ve FOV geri yüklenir. Çok sahneli Play açılışında ikinci GameScene/LevelLoader eklenmesine yol açan başlangıç yarışı da engellenir; doğrudan salon/mutfak girişleri korunur.

QA ayrı kayıt kopyası kullanır; gerçek JSON ve yedekleri başlangıç özetiyle karşılaştırılır. Sunum tercihlerinin değerleri ve varlık durumları geri yüklenir. APK üretilmez, commit/push yapılmaz, bilgisayar kapatılmaz.

## Son doğrulama

- Tam EditMode: **489/489**, `EditMode-Final-04.xml`. Sonrasında yalnız editör ön izlemesinin dar ekran yerleşimi eklendi; bu adım iki oranda gerçek Unity penceresinde ve geri yükleme denetimiyle ayrıca doğrulandı.
- **13 benzersiz native PlayMode testinin son sonucu başarılı**; `native-test-summary.json` her testin son XML'ine bağlanır. Önceki başarısız ara XML'ler tarihsel olarak korunur, başarı sayısına katılmaz.
- On ırkta koltuk dinlenmesi: yastık hacimlerinde sıfır kedi mesh noktası. Açık oyuncaklarda yakın çevre ve gerçek pati teması, tırmalama direğinde dört yön/iki pati, tam beş oyuncaklı odada on ırk; 78 ROOM eylemi ve 17 CAT ürününün yakınlık sözleşmesi doğrulandı.
- Runner: 800 sıralı üretim denetimi; coin/bonus/engel ayrımı, platform ve mıknatıs yolu. Altı toplanabilir türün 480'er hareket pozunda toplam **2.880 örnek**, ölçülen hareket hacmi dışında sıfır mesh köşesi (`pickup-motion-envelope.csv`).
- Eski avlanma görevinin Cat Catch bağlantısı gerçek bir turdaki 22 yakalamayla bir kez tamamlandı (`catch-quest.txt`).
- İçerik denetleyici **0 hata / 0 uyarı**. Son kod derlemesinde hata yok; projedeki mevcut kullanımdan kaldırılmış API uyarıları bu geçişin kapsamı dışında.
- 16:9 ve 4:3 gerçek UI kareleri, güncellenmiş 103 ürün ve sekiz oda fotoğrafı, sekiz Blender model fotoğrafı. Dört sessiz video: Runner ve Catch 18'er saniye; koltuk 310/24 ≈12,92 saniye, tırmalama 94/24 ≈3,92 saniye. Bunlar fiziksel tablet FPS/GPU/RAM/ısı ölçümü değildir.
- Yeni engelleri ve eşzamanlı bonus çubuğunu gösteren özel kareler gerçek Unity varlıklarıyla düzenlenmiş sahne gösterimleridir; rastgele oyun akışı kanıtı olarak sunulmaz. Ayrı Runner videosu normal üretim akışını kullanır.
- Ön izleme iki kez yenilendiğinde tek geçici kök; kaldırıldığında sıfır kök, özgün TRS/görünürlükte sıfır fark ve FOV geri yüklemesi başarılı (`editor-preview-check.txt`). Unity açık, **Play ve QA kapalı**; üç normal sahne kaydedilmiş, bekleyen sahne değişikliği yok.
- Gerçek kayıt ve recovery SHA-256 **db186b0aba96a32e885631563fd15528a05501987fbf0ac915e53ff8d6823675**; eski CP2 yedeği **03d4fd1a6420ff0d6cd6213fe08ea57598038ec589ba7ca02475692036ea9a8d**. Üç dosya başlangıçla birebir aynı (`save-final.json`). 16 sunum tercihi ve varlık durumu aynen geri yüklendi.
- Cloud Code yerel Release derlemesi 0 hata/0 uyarı ile geçti. **Canlı servise yayın yapılmadı**; yeni skor bonusunun çevrimiçi kabulü için bu kaynak ayrıca yayımlanmalı. APK, git commit/push ve bilgisayar kapatma yapılmadı.

QA hazırlığında yalnız güncel oda koleksiyonları ve CAT ürünleri kullanılır. Katalogdaki bütün 114 tarihsel ürünü birden sahip yapmayın: emekli oda dekorları eski yerleriyle çakışabilir. Son hareket kayıtları doğru 97 ürün kümesinden, etkin oyuncak sınırı korunarak alındı.
