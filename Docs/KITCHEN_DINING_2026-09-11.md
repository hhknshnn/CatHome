# Mutfak mama, meyve ve yemek takımı — 11 Eylül 2026

Kullanıcı önceki mutfak teslimindeki kafa gömülmesini bildirdi. Meyve sepetinin tezgâha alınmasını, kedinin tezgâha çıkıp sepeti düşürmesini ve meyvelerin dökülmesini istedi. Mutfak ortası için odanın tarzında yemek masası/sandalyeler ve masadan eşya atma eylemi istedi. Önceki orta alanı boş tutma kararı mutfak için bu yeni kapsamla değişti. Diğer odalar korunur. Sıra: mama geometrisi; tezgâh/sepet; yemek takımı ve hareket; birleşik doğrulama ve gerçek PNG teslimi. Tahmin90–120dk; başlangıç17:37UTC. Yeni video/arşiv/APK/commit/push/yayın/kapatma yok.

Kanıt kökü `Docs/QA/KITCHEN_DINING_2026-09-11`. Kayıt ve sekiz oda sahnesinin başlangıç SHA256'ları ayrı tutuldu. Eski teslim sonuçları bu revizyonun son doğrulaması değildir. Mama testinde yalnız ağız uzaklığı yeterli kabul edilmez; gerçek baş/çene yüzeyi ve kap kenarı/yiyecek açıklığı ölçülür.

QA oturum notu: önceki manuel denemenin gecikmiş kapanış çağrısı yeni deneme kopyasını kapattı. Kısa normal Play sırasında otomatik yazılmış ana/recovery dosyaları, bu turun17:38:54 kopyasındaki başlangıç hash'i birebir doğrulanan özgün baytlarla geri getirildi. Aradaki otomatik dosyalar yalnız yerel kanıt klasöründe saklandı. `RoomInteractionReview` gecikmiş kapanış artık çağrı anında manuel oturum bayrağını tekrar kontrol eder; kapanmış eski oturum yeni QA'yı kapatamaz. Bu kayıt değişikliği oyun tasarımı işlemi değildir; doğrulama sonunda üç gerçek kayıt başlangıçla yeniden karşılaştırılır.

## Mama

Eski çözüm hedefe en yakın ağız noktasını seçtiğinde üst dudak yakın görünürken alt çene yiyeceğe girebiliyordu. `CatMealHeadMotion` artık gerçek kaynak ağız noktaları arasında en alt çeneyi çözer. Hedef yiyeceğin açık kenarına yatay25mm ve yukarı25mm kayar; uzaklık hâlâ gerçek mama noktasına göre raporlanır. Sabit kök, özgün Eating klibi, kap modeli ve ölçüsü korunur; salonun beslenmesi değişmez. Yeni gerçek skinned baş/çene ve yemek üçgeni ölçümü tüm10ırkta geçti. Son kanıt `meal-lip-clearance.xml`:2/2 native; yemek açıklığı en az4.4mm, kap kenarı42.9mm, en büyük gerçek ağız uzaklığı34.21mm. Önceki yalnız yakınlık testi bu hatayı yakalamıyordu.

## Tezgâh ve sepet

Tezgâhın üstündeki küçük sabit süsler kaldırıldı; gövde korunur. Sepet artık gerçek743.04mm tezgâh yüzeyinde `(-1.72,.74304,2.05)`, oda ölçeği.52. Kaynak sepetin on meyvesi ayrı hareketli mesh, çerçevesi ayrı parça oldu; kaynak meyve şekilleri korunur. Tezgâh sahipliği gerekir; mevcut bağımlı ürün akışı kullanılır. Kedi açık zeminden çıkar, gerçek pati temasından sonra üç kez iter; sepet kenarı geçer, on meyve düşer/sekerek dağılır. Kedi iner; çevrim veya iptal sonunda parçalar tam özgün yerlerine döner. Duraklatmada hareket durur. Ortaya masa geldiği için giriş `(-1.21,0,1.00)` olarak açık sola alındı. Sepet arka pati desteği uzun Oriental dahil onırkta doğrulandı. Son birleşik sonuç `kitchen-final-native.xml`:6/6. Görsel inceleme, çok parçalı FBX içindeki dönüşün tek parçalı eski modelden farklı taşındığını yakaladı; model kökündeki+90° düzeltmeyle taban tezgâha basar ve iki kat yukarı yükselir. Native test bu dik başlangıç duruşunu da ölçer. Eski ara masa/sepet denemeleri son sonuç değildir.

## Yemek takımı

Yeni Blender kaynağı `ArtSource/Blender/PremiumFurniture/build_kitchen_dining.py` ve `KitchenDiningSet_Source.blend`; oyun modeli `KitchenDiningSet_Premium.fbx`. Krem/mint masa, dört mercan/mint minderli sandalye, dört servis düzeni ve bağımsız kupa/kase/tuzluk. Masa1.80×1.12m, üstY.78m, merkez `(.15,0,-.35)`. `KitchenDiningSetBuilder` sabit oda eşyasını ve gerçek beş statik mesh çarpışmasını tekrar üretir. Satın alınacak on ürünün sayısı/fiyatı değişmez; ayrıca ücretsiz `DiningScatter=103` eylemi eklenir. Kedi sağ açık uçtan çıkar; gerçek pati teması olmadan hiçbir parçanın düşüşü başlamaz. Üç eşya sırayla açık uçtan yere atılır; kedi indiğinde eski yerlerine döner. Oyun düğmesi “Masadan eşya at”. Onırkta üç temas, üç parça, tek tamamlanma, dört pati desteği, kameraya görünen gövde ve açık iniş doğrulandı. Sıçrama, pati atma ve düşüş sırasında duraklatma/iptal eşyaları ve kediyi geri getirir.

## Son doğrulama

45/45 hedefli EditMode (mağaza, mutfak eylem bağları ve oda planları), validator0hata/0uyarı. Önceki60Hz ölçümlere ek olarak gerçek oyun düğmesinde24Hz sepetin ikinci pati mesafesi28.38477mm ile28mm sınırını aştı; rutin iptal oldu. Eşik artırılmadı: `SurfaceScatterActivity.Touch` tam erişimi216ms tutar. Böylece tek karelik erişim tepe noktası düşük kare hızında atlanmaz. Oriental’ın karşı patisinin erişimi de sınırdaydı: duruş sepete21.6mm yaklaştırıldı, `.302/0/.328` yerel destek noktası kullanılır. Fiziksel28mm temas kabulü değişmedi. Son `scatter-verified.xml`:6/6;20ırk/rutin24Hz ve siyah kediyle15/30/60Hz altı ek tam çevrim başarılı. Üç gerçek pati temasından sonra tüm parçalar düşer.

Tezgâh/sepet ürün kartları ve mutfak oda ön izlemesi yenilendi. Yedi diğer oda sahnesi başlangıçla birebir aynı. Gerçek ana/recovery SHA256: **3558D75B36B1710960624B629C8F0C2C51A1E1B231E2572B12984F825F896B9B**; CP2: **03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D**.16tercih karşılaştırması16/16; ilk test oturumu kapandıktan sonra gerçek kayıttan yeni ayrı görsel/kullanıcı denemesi açıldı. Kare hızı kontrolleri Unity animasyon zaman adımıdır; fiziksel telefon performansı ölçülmedi.

## Teslim

**10/10 benzersiz native,45/45 hedefli EditMode,11/11 gerçek oyun düğmesi; validator0/0.** Son düğme kanıtı `button-report.json` / `ROOM_INTERACTIONS_2026-09-11/KITCHEN_DINING_VERIFIED`. Her eylemde tek tamamlanma, açık çıkış ve kontrol iadesi var. İlk `KITCHEN_DINING_FINAL`10/11 ara denemedir; son sonuç olarak kullanılmaz. Yalnız İzle hâlâ bir: buzdolabı.

Native son kaynakları `native-summary.json` içinde test bazında belirtilir. Mama10ırk, dört destek/dinlenme rutini40ırk/rutin, sepet ve masa20ırk/rutin: toplam yedi kritik rutin için70benzersiz ırk/rutin. Bütün11eylemin tümırk taraması değildir. Kare adımı15/24/30/60 kontrolleri gerçek telefon performansı iddiası değildir.

[Son galeri](QA/KITCHEN_DINING_2026-09-11/index.html) altı gerçek Unity PNG içerir; video yok. Son üç kayıt ve yedi diğer oda hash karşılaştırmaları başarılı. Tercihler16/16 geri yüklendi; ayrı kullanıcı denemesi `Library/UiQaSession/20260911-192456`. Unity mutfakta tek Oriental Shorthair, tek kamera/dinleyici, üç temiz sahne ile açık. `Oda etkileşim denemesi` paneli ve Sepeti devir düğmesi hazır. Çekim/derleme kapalı, normal zaman; panelden denemeyi bitirmek tercihleri geri getirir. `editor-ready.json` son durumdur.

APK/arşiv/commit/push/yayın/kapatma yapılmadı. Banyo/salon ve diğer oda tasarımları bu turda değiştirilmedi. Mutfak revizyonu kullanıcı incelemesindedir.
