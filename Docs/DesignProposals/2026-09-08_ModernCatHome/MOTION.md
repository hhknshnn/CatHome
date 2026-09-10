# Cat Home — hareket, animasyon ve ses sunumu

8 Eylül 2026 · Onay öncesi tasarım şartnamesi. Bu dosya çalışan animasyon klibi değildir.

[Galeri](index.html) · [Uygulama planı](PLAN.md)

## Hareketin amacı

Hareket; dokunmayı, durum değişimini, kedinin niyetini ve fiziksel teması anlaşılır kılmalı. Sürekli sallanan kartlar ve her dokunuşta aynı sıçrama yerine eylemin büyüklüğüne uygun kısa tepkiler öneriliyor. Görsel panolar anahtar pozları gösterir. Galerideki oynatılabilir örnek yalnız arayüzün açılma/kapanma ritmini gösterir; kedi iskeleti veya cihaz performansı için kanıt değildir.

## Arayüz zaman çizelgesi

Aşağıdakiler önerilen sunum süreleridir; mevcut oynanış veya animasyon klibi süresi değildir. UI süreleri zaman ölçeğinden bağımsız, durdurulabilir ve yeniden girişte temizlenebilir olmalıdır.

| Aile | Normal hareket | Azaltılmış hareket | Bitiriş / iptal |
|---|---|---|---|
| Düğmeye basma | 70 ms, en çok %2 küçülme; bırakınca 110 ms dönüş | Renk ve küçük yüzey değişimi; ölçek yok | Parmağın dışarı sürüklenmesi satın alma üretmez |
| Ana panel | 200 ms açılma, 12 px yükselme, %98→100 ölçek ve alfa | 100 ms alfa, konum/ölçek yok | 140 ms kapanış; arka girdi kapanış bitene kadar kilitli |
| Sağ menü çekmecesi | 220 ms, yalnız 24 px giriş hareketi | 100 ms alfa | Geri tuşu en üst katmanı kapatır |
| Sekme değişimi | 160 ms; seçili işaret 8–12 px kayar; içerik kısa çözülür | Seçili durum anında; içerik 80 ms alfa | Art arda dokunma içerikleri üst üste bırakmaz |
| Konuşma balonu | 180 ms, kuyruk ucundan en çok %98→100; metin sonra görünür | 100 ms alfa; metin bir seferde | Yeni kritik balon eskisini düzenli değiştirir |
| Uzun diyalog | Panel 200 ms; metin 30–35 karakter/sn isteğe bağlı | Metin doğrudan tamamlanır | İlk dokunma metni tamamlar, sonraki ilerler; cümle kaybı yok |
| Küçük bildirim | 180 ms giriş, 2,2 sn okuma, 160 ms çıkış | 100 ms giriş/çıkış; okuma süresi aynı | Aynı ödül olayı iki bildirim doğurmaz |
| Ödül sayımı | En çok 650 ms; kaynak miktara doğru tek ilerleme | Son miktar anında | Sunum sayacı ekonomiyi yazmaz; tekrar oynatma ödül vermez |
| Konfeti / kalp | Tek olay, 600–900 ms; sınırlı küçük parçacık | Dekor kapalı, kazanım metni açık | Kapanışta tüm parçacıklar bırakılır |
| Oda geçişi | 220–280 ms kısa çözülme; gerçek hazır olma süreci izlenir | 100 ms çözülme | Eski/yeni kamera çakışmaz; yeni oda hazır olmadan hareket açılmaz |
| Portre vitrini | İlk kare önden üç çeyrek; parmakla kontrollü dönüş | Girdi olmadıkça durur | Boyut/kapanışta render hedefi bırakılır |
| Kaydırma | Parmakla bire bir hareket; sınırlı atalet | Atalet yok; doğrudan kaydırma var | Düğme tıklamasıyla sürükleme ayrılır |

Bu süreler UI için bir başlangıç standardıdır. Görsel karşılaştırmada hızlı/yavaş bulunan alanlar tek bir ortak değişken üzerinden ayarlanır. Dekor bitene kadar oyuncunun asıl eylemi gereksiz bekletilmez.

## Kedi hareket aileleri

Her ailede giriş → gerçek etkileşim / tutulan poz → çıkış → iptal kontrolü gerekir. Bütün ürünleri aynı miyav ve tek baş hareketiyle temsil etmek yeterli değildir. Aşağıdaki harita mevcut etkinlik çeşitlerini görsel olarak kapsar; yeni ödül veya yeni oyun mekaniği eklemez.

| Aile / mevcut örnekler | Anahtar poz önerisi | Teknik ve davranış sınırı |
|---|---|---|
| Boşta / bakış | Ağırlık küçük aktarılır, kulak bir kez döner, kısa doğal baş bakışı | Gövde sürekli sallanmaz; kemik boyları değişmez |
| Yürüme / koşma / dönüş | İtiş, destek, salınım; sert yönde önce dönüş sonra ilerleme | Mevcut .65 m/sn alt yürüyüş; tempo gerçek yer hızı; durunca ayak sürükleme yok |
| Miyav | Baş hafif yükselir, ağız kısa açılır, nefesle kapanır | Gerçek klip/ses süresi; bitene kadar komut yığılmaz; enerji/jeton/görev ödülü yok |
| Otur / Loaf / Kalk | Kalça alçalır, ön patiler yerleşir; loaf'ta patiler gövde altına toplanır; kalkış destekle başlar | Mevcut .7 sn oturma, loaf'tan .4 sn ara ve .7 sn kalkış temel alınır; UI süresine göre kısaltılmaz. Enerji yalnız gerçek tutulmuş dinlenmede |
| Mama / su / tokken inceleme | Açık önden yaklaşma, baş kap seviyesine iner; yemek/içme ve koklama farklı küçük hareketler | Gerçek kaba temas; görünmeyen eşya bakım açmaz; koklama yemek görevi ilerletmez |
| Uyku / Uyan | Yatağa yerleşme, göğüste küçük nefes, başı kaldırarak uyanma | Uyku gerçek destek noktasında; yatak arkası kapalı; kayıt geri yükleme korunur |
| Mobilyada dinlenme | Minder teması, doğal omuz/kalça yerleşimi; sonra destekli kalkış/iniş | Koltuk, daybed, minder, canopy/perch/towel/oven warmth aynı yaşam döngüsü; oyuncu Kalk diyene kadar sürer |
| Sevme / fırçalama | Baş temas yönüne hafif döner, gözler yumuşar, omuz küçük tepki verir | Sadece gerçek sevme/fırça evresinde; insan el modeli zorunlu değil |
| Tırmalama | Ön pati sırayla yüzeye basar, gövde arka patilerle desteklenir | Pati yüzeye girmez; tırmalama noktası ve denetleyici sahipliği korunur |
| Yoğurma | Yumuşak yüzeyde sırayla sol/sağ ön pati; omuzdan küçük karşı hareket | MatKnead temas desteği ve düşük genlik; kumaşı aşırı çökertme yok |
| Tünel | Ağızda hizalanma → baş/gövde girişi → içinden yürüyüş → açık çıkış | Yalnız Oyna rutini; iki ağızdan seçim; elle yürürken katı engel korunur |
| Top / yumak / fare oyunu | Hedefe bakma → yaklaşma → tek pati itişi → takip | Sürekli top taraması, görünür oyuncak engelleri, gerçek tek temas; dekorun içinden geçiş yok |
| Küçük nesne devirme | Ağırlık destek patilerinde, tek ön pati uzanır, nesne ayrılır | KnockOff/CartNudge gibi mevcut etkinliklerin gerçek temas zamanı esas |
| Çıkma / inme / kenar yürüyüşü | Önce ön patiler destek alır, arka bacak itişi; inişte ön patiler yumuşar | PantryClimb/TubEdge/Perch; gerçek ürün üçgenleri ve açık çıkış |
| Kutu / sepet içine girme | Başla kontrol, ön pati, gövde, içeride kısa tepki, açık çıkış | HamperDive; ürün hacmi ile kedi hacmi eşleşir |
| Kazma | Ön patiler dönüşümlü, gövde dengeli, kısa arka kontrol | LitterDig; efekt küçük ve yerel, ihtiyaç/ödül koşulları korunur |
| Duş / lavabo | Açık girişten yerleşme, doğal baş eğimi, kısa damla ve silkelenme | ShowerRinse/SinkSip; duşun cam olmayan yarısı ve gerçek su evresi |
| Salıncak | Salıncağın gerçek hareketi, kedi gövdesi destek üstünde dengelenir | SwingRide; azaltılmış harekette dekor sakinleşir, eylem bitirme erişilebilir |
| Kuş / asılı oyuncak / tablo | Sınırlı baş ve göz takibi; gerekiyorsa küçük tek pati uzanması | BirdFeederShake/SitLook; ulaşılamayan nesneye sahte temas yok |
| TV / pikap / kâğıt | Bakış hedefi + gerçek medya; pikap yatay döner; kâğıt kendi rulosunda açılır | Pikap Y ekseni, kâğıt X; TV mevcut kayıtlı medya; ek canlı kamera yok |
| Runner zıplama / iniş | Hazırlık → uçuş → ön pati inişi → koşuya dönüş | Mevcut atlayış eğrisi; tek iniş olayı; köke ikinci görsel yay eklenmez |
| Runner eğilme / toparlanma | Dirsek/diz gerçek bükülür, sırt doğal alçalır; son eğilme pozundan koşuya dönüş | Mevcut 90 ms giriş, 220 ms toparlanma, 1.25–2.6 eğilme çevrimi/sn; Y ezme yok |
| Runner yol / rampa teması | Son örneklenen patiler gerçek yüzeye oturur | Koşu/eğilme/toparlanma .033 m yol/rampa ölçümü; yalnız yukarı düzeltme; havada/pause atlanır |
| Catch av | Hedef seçme → yönlenme → hazırlanma → atılma → pati teması → toparlanma | 28° hazırlık eşiği, 300°/sn dönüş, havada sabit yön, .32 m ön pati teması; tek fare; kaçırma seriyi bitirir |

## Sahne hareketleri ve efektler

- Bitki ve kumaş: düşük genlik, seyrek ve dekoratif; collider oynamaz. Azaltılmış harekette durur.
- Pencere ışığı: materyal/ışık tasarımı; pahalı sürekli güneş simülasyonu önerilmez. Mevcut zaman ve ışık tercihleri varsa onlara bağlanır.
- Jeton: kanonik pati yüzü korunur; görünür parlama kontrollüdür. Spawn açıklığı ve mıknatıs yolu engelleri aşmaz.
- Hedef halkası: sadece ilgili hedef; zemine oturur, kedinin patisini veya fareyi örtecek opak disk olmaz.
- Vuruş/başarı: küçük yerel yıldız/çizgi, en fazla kısa bir olay. Kamera sarsılması ve FOV etkisi azaltılmış hareketi izler.
- Başlangıç/sonuç fotoğrafı: hazır gerçek Play kamerasından alınır; görsel üretim karesini gerçek oynanış diye sunma yok.

## Ses eşleşmesi

Doğal miyav, mırlama, mama ve su sesleri gerçek eylem evresine bağlı kalır. İptal, pause, mute ve sahne geçişinde biter. Her düğmeye plop eklenmez, eski sentetik sürekli müzik/ortam döngüsü geri getirilmez. Ödül ve alışveriş gibi anlamlı olaylarda mevcut kısa sesler kullanılabilir; sesin olması yeni bir ödül olayı tanımlamaz.

## Hareket kabul kontrolü

1. On ırkta yan, ön ve arka görünüş: kemik/pati/zemin, kulak/kuyruk ve görünür yüzey teması.
2. Giriş, sürme, çıkış, iptal; pause, odak kaybı, oda/ırk değişimi sırasında kaynak ve hareket kilidi temizliği.
3. Runner iki hız, düz/rampa ve 30/60 simülasyon adımı; Catch durağan/kaçırma/ardışık temas durumları.
4. Mobilyada giriş ve çıkış gerçek oda kamerasından, komşu eşyalar görünürken; yakın çekim tek başına yeterli değil.
5. Normal ve azaltılmış hareket, ses açık/kapalı; UI ritmi ile oyun fiziği birbirinden bağımsız doğrulanır.
6. Galeride storyboard ile gerçek yeni oyun kaydı ayrı etiketlenir. Bu teslimde gerçek yeni animasyon kaydı üretilmedi.
