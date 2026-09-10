# Cat Home — bütün deneyim için modern tasarım önerisi

8 Eylül 2026 · **Onay bekleyen tasarım çalışması. Oyuna uygulanmadı.**

[Görsel galeri](index.html) · [Ekran kapsamı](SCREEN_INVENTORY.md) · [Animasyon şartnamesi](MOTION.md) · [Görsel üretim kayıtları](PROMPTS.md)

## 1. Tasarım kararı

Öneri; canlı renkleri, dokusu hissedilen üç boyutlu bir evle ve daha açık bir arayüz hiyerarşisiyle birleştirmek. Kedi, oda ve ürünler ekranın ana içeriği olacak. Mavi ana eylemi, mercan duygu ve kısa vurguları, sarı kazanımı anlatacak. Başlıklar, açıklamalar ve bilgi alanları kendi başına büyük düğmelere benzemeyecek.

Bu çalışma yalnızca palet değişikliği değildir. Ekran kompozisyonu, yüzeyler, tipografi, ikon ailesi, kedi portreleri, odaların malzeme ve ışığı, iki mini oyunun çevresi, diyaloglar, bildirimler ve hareketlerin birlikte tasarlanmasını önerir.

**Teslimdeki resimler yapay zekâyla oluşturulmuş tasarım mockuplarıdır.** Yeni Unity ekranı, yeni kedi modeli veya çalışır oyun kanıtı değildir. Kedi yüzündeki daha yumuşak görünüm ve malzeme ayrıntısı mevcut düşük poligonlu modelden daha ileri bir hedef tarif eder; yalnız renk değiştirerek aynı sonuç alınacağı varsayılmaz. Gerçek oyundaki karşılığı, onay sonrasında model/rig uyumu ve mobil maliyetle birlikte çalışılmalıdır. Resimlerdeki ince tüyler için gerçek zamanlı tek tek tüy simülasyonu önerilmiyor.

Mockuplardaki isim, seviye, bakiye, puan, fiyat, süre ve ödüller **yerleşim örneğidir**. Gerçek değerler mevcut servislerden gelecektir. Çizilmiş Google düğmesi, oda kilidi, kedi pozu veya perspektif; mevcut iş kurallarının ya da resmî sağlayıcı varlığının yerine geçmez. Kesin metin, ölçü ve davranış için bu belge ve kapsam tablosu esas alınır. Oda sanat panoları malzeme/ışık hedefidir; mobilyaların taşınması için yeni plan değildir.

## 2. İncelemeden çıkan sorunlar

En yeni `JOYFUL_ARCADE_2026-09-08` galerisinin son HUD, mağaza, Birlikte, diyalog, isim, menü, oda, koleksiyon ve mini oyun kareleri doğrudan incelendi. Yeni checkpoint ve uygulama raporu önce okundu. Önceki UI/UX incelemesi, referans eşleştirme raporu, üretim geçişi, oda etkileşim raporu ve mini oyun raporu tarihsel bağlam olarak okundu; eski kusurların bugün hâlâ var olduğu varsayılmadı. Ekran kapsamı ayrıca güncel panel sınıfları, rehber içeriği, oda kataloğu ve `UiQaVisualTour` yollarıyla karşılaştırıldı.

| En yeni karelerde görülen durum | Tasarımda yapılacak iş | Başarı ölçütü |
|---|---|---|
| Bilgi, başlık, sekme ve eylem benzer kabarıklıkta | Üç yüzey düzeyi: dünya üstü bilgi, içerik yüzeyi, etkin düğme | Başlık düğme gibi görünmez; ana eylem ilk bakışta seçilir |
| Aynı dış panel ve şerit çok sayıda ekranda tekrarlanıyor | Mağaza için ürün ızgarası, Kedim için karakter vitrini, ödül için kısa sahne, ayarlar için sakin satırlar | Her ekran amacıyla tanınır |
| Altın kenar, parlak yüzey ve renkli kart aynı anda yarışıyor | Kenar süsünü azalt; doku ve ışıkla zenginlik oluştur | Küçük ekranda içerik çerçeveden daha güçlü görünür |
| Ahşap, kumaş, seramik benzer hissediliyor | Malzeme ailelerini ayrı roughness, normal ve renk yoğunluğuyla kur | Malzeme siyah-beyaz küçük önizlemede bile ayırt edilir |
| Diyalog büyük kutuda, kedi tepkisiyle bağı zayıf | Kısa alt konuşma paneli, gerçek portre, ayrı konuşma/düşünce/tepki balonları | Kedi ve konuşmacı bağlantısı açık; oyun alanı korunur |
| Runner'da cepheler tekrar eden bloklar, zemin geniş tek yüzey | Vitrin derinliği, kontrollü malzeme çeşidi, ön/orta/arka plan ayrımı | Engel ve jeton dekorun önünde okunur |
| Catch çevresi ile arena benzer renk ağırlığında | Arenayı sakinleştir, çevreyi kenarda zenginleştir, hedefe özgü ince vurgu | Fare, pati ve seçili hedef hareket sırasında net kalır |

## 3. Ortak görsel sistem

| Öğe | Önerilen standart |
|---|---|
| Metin | Ana koyu lacivert `#172841`; ikincil metin `#53677D`; beyaz üstünde soluk gri uzun metin yok |
| Ana eylem | Azure `#2779F5`; beyaz yazı, ölçülü üst ışık, küçük temas gölgesi |
| Duygu / vurgu | Mercan `#FF695A`; bütün düğmeleri mercana boyamak yerine anlık duygu ve seçili özel vurgu |
| Ödül | Güneş sarısı `#FFC857`; mevcut kanonik pati jetonu, elmas ve bakiye anlamları korunur |
| Dinlenme / su | Lavanta ve su mavisi; ikon ve metin de durumu söyler, renk tek bilgi kaynağı olmaz |
| Zemin | Soğuk beyaz `#F7FAFF`, açık mavi `#EAF3FF`; dünya arkada kendi zengin renklerini taşır |
| Başlık | Mevcut Türkçe destekli Cat Home Fredoka 500/600; az ve belirgin başlık |
| Gövde | Mevcut Nunito Sans; paragraf, durum, fiyat ve açıklamada tutarlı ağırlık |
| Köşe / boşluk | 8 birim temel aralık; 12–16 küçük kontrol, 20–24 kart, 28–32 büyük modal köşesi; her yüzeye aynı kapsül şekli verilmez |
| Dokunma | 1080 referans genişliğinde en az 48×48 birim; ana eylem 56–64 birim yüksek; tablet ve telefonda fiziksel okunaklılık ayrıca denenir |
| Yazı ölçeği | 1080 referansta gövde 16–18, eylem 18–20, bölüm 24–28, önemli başlık 32–40; dar oranda yazıyı küçültmek son çare |
| Alt gezinme | Mevcut 80 referans px ve dört sabit alan: Mağaza / Oda / Birlikte / Oyunlar; SafeArea ayrıca uygulanır |
| İkonlar | Ortak kamera, ışık ve malzeme ile hazırlanmış küçük üç boyutlu aile; menü/kapama gibi yardımcılar sade çizgi ikon |

İkon havuzu: dört alt gezinme, üç ihtiyaç, profil, görev, ayar, yardım, kilit, sahiplik, ekleme/kaldırma, ses, hareket, oyun kontrolü, can, fare, süre ve mevcut para birimleri. Jeton ve elmasın onaylı kanonik kimliği değiştirilmeyecek. Mockuptaki üretilmiş simgeler biçim/ölçek örneğidir.

Yüzey durumları birlikte tasarlanır: normal, basılı, seçili, devre dışı, odak, yükleniyor, başarı, hata. Her düğmeye ayrı renk/ölçek yaması eklemek yerine tek sistemden üretilir. Dekoratif yüzeyler girdi almaz.

## 4. Ekran ailelerinde düzenleme

### Açılış, ilk giriş ve diyalog

Açılışta seçili kedi ve aynı oyunun diğer kedileri iyi ışıklandırılmış bir sahnede görünür. Ana eylem `Devam et`, yeni kayıt için `Oyna` olur. `Yeni oyun` ikincildir. Hesap seçimi kısa, misafir yolu görünürdür. Sağlayıcı düğmesi gerçek resmî varlıkla hazırlanır. Yükleme ekranında yalnız gerçek ilerleme bilinirse yüzde gösterilir.

İsim verme, klavye açıkken de portreyi ve alanı koruyan kısa bir kart olur. Boş/geçersiz/uzun ad açıklaması alanın hemen altındadır. Dört tanışma adımı korunur; adım metinleri gerçek öğretici akışa bağlanır. Balon aileleri: konuşma için kısa kuyruk, düşünce için küçük noktalar, duygu için yazısız küçük simge. Uzun diyalog ayrı alt panelde sunulur. Ekran dışında kalan balon içeri alınır; kapı/ürün tıklamasını rastgele kapatmaz.

### Ev ve bakım

Üst bölüm ihtiyaçları kısa ve açık gösterir; kimlik ve para ayrı gruplarda kalır. Sıfıra yaklaşan ihtiyaç yalnız renk değiştirmez, kısa etiket ve ikonla belirtilir. Ekran ortası kedi ve hareket alanına ayrılır. Bağlamsal eylem kartında ürün fotoğrafı, yerelleştirilmiş ürün adı ve eylem birlikte görünür.

Mama/su/uyku/koltuk/tünel için başlama, sürme, bitirme, meşgul ve ulaşılamayan durumların yüzeyleri aynı aileden gelir. `Kalk` ve `Uyan` görünür kalır. Sevme mockupındaki el yalnız dokunma hareketini anlatan gösterimdir; oyuna insan eli modeli veya yeni kamera sistemi ekleme kararı değildir.

### Mağaza, odalar ve koleksiyon

Katalog başlığı kısalır; ürün fotoğrafı kart alanının yaklaşık %60–65'ini alır. Kategori sekmeleri bilgi satırları gibi davranır. Sahiplik, odada olma ve satın alınabilirlik ayrı görünür. Dört geniş ekran sütunu tablette üçe, gerektiğinde ikiye düşer. Kaydırma tüm içerik/boşluk yüzeyinden başlar; sürükleme satın almaya dönüşmez.

Ürün ayrıntısı; büyük fotoğraf, kısa işlev, mevcut fiyat ve açık satın alma seçeneklerinden oluşur. Ön koşulda istenen ürün ile gerekli ürün birlikte görünür. Elmas harcama onayı son bedeli tekrar söyler. Bakiye yetersizliği ve platform mağazası hatası aynı mesaj değildir. Gerçek fiyat gelmeden para tutarı uydurulmaz; sahte indirim eklenmez.

Sekiz oda aynı kart ailesiyle gösterilir. `Buradasın`, `Git`, `Aç` ve kilit açıklaması gerçek sahiplik/seviye/ön koşuldan gelir. Görseldeki `Önceki koleksiyonu tamamla` örnek kopyadır; tüm odaların kilidini buna dönüştürme önerisi değildir. Patio'nun oyuncuya görünen adı mevcut yerelleştirmeden alınır; görsellerde kullanılan Veranda/Avlu ifadesi katalog adını değiştirmez.

Eşya yönetimi yalnız mevcut CAT koleksiyonunu ekleme/kaldırma ve sabit yere yerleşme davranışını anlatır. Yeni sürükle-bırak, serbest dönme veya oda yerleşimi sistemi önerilmez. 103 kanonik ürünün her biri yeni ortak fotoğraf ışığıyla ve kendi gerçek modeliyle yeniden çekilir; sekiz oda fotoğrafı da güncellenir.

### Kedim, Birlikte, rehber ve görevler

Kedim ekranında tek büyük karakter önizlemesi, on ırk için aynı kadrajlı seçim kartları, açık sahiplik ve seçili durum olur. İsim ve mevcut renk/görünüm seçenekleri korunur; hayalî aksesuar satışı eklenmez. Tasarım hedefindeki daha yumuşak kedi yüzü uygulanacaksa aynı seçili kedi; portre, ev, kart, kutlama ve iki mini oyunda birlikte güncellenir.

Birlikte ekranı Miyavla / Otur / Loaf komutlarını gerçek pozlarıyla gösterir. Meşgul durumda aynı düğme hem etkin hem devre dışı görünmez. Rehberin altı bölümü aynı yerleşimde kısa görsel anlatım ve kaydırılabilir açıklamayla sunulur. Metin içerikleri güncel davranışla karşılaştırılır; örneğin Birlikte girişinin alt dock'ta olduğu doğru yazılır.

Görev ekranı günlük ve bölüm hedeflerini ayırır. Devam eden, tamamlanmış, ödülü alınmış ve boş liste durumu hazırlanır. Geri dönüş özeti bakım ihtiyacını anlatır, ödül kazanılmış gibi davranmaz. Yeni günlük giriş ekonomisi veya yeni başarı sistemi yalnız mockupta göründüğü için eklenmez.

### Kutlamalar ve bildirimler

Seviye için büyük seviye işareti ve gerçek açılan içerik; koleksiyon için tamamlanan odanın fotoğrafı; tanışma için kediyle kısa duygusal an. Bu üçü ayrı kompozisyon kullanır. Ödül tepsisi sadece servisin bildirdiği kazanımları içerir. Kısa bildirim oyunu kesmeden köşede görünür. Konfeti tek kısa patlamadır, sonsuz döngü olmaz. Azaltılmış hareket durumunda sonuç doğrudan okunur.

### Runner ve Catch

Oyun seçimi gerçek oyunların iki büyük vitrini olur. Girişte rekor, can ve kontrol özeti tek yerde toplanır. Can yok, reklam hazır, reklam yok ve bağlantı hatası ayrı durumlardır. Runner'ın yol yönü ve engel yaklaşımı değişmez; cephe derinliği, vitrin, tente, bitki ve yol malzemesi geliştirilir. Yakın engeller daha net, uzak dekor daha sakin olur. Dokuz mevcut varyasyonun kimliği ve doğurma sistemi korunur.

Catch'in açık 8×6 oyun alanı korunur. Çevre bitkileri/oturma detayları kenardadır. Hedef işareti, hazırlanma, atılma, temas, kaçırma ve toparlanma okunur. Süre ana bilgi, skor ve fare sayısı ikinci gruptur. Mockup efektleri fiziksel temasın yerine geçmez.

İki oyunda ortak duraklatma ve sonuç ailesi kullanılır. Skor, kazanılan jeton ve rekor açıkça ayrılır. İsteğe bağlı x2 reklam yalnız mevcut servis uygun olduğunda görünür. Sonuç ekranını yeniden açmak veya iki kez tıklamak tekrar ödül vermez.

### Ayarlar, hesap ve servis durumları

Ses, dil, azaltılmış hareket ve hesap ayrı gruplardır. Mevcut seçeneklerin işlevi korunur; görselde örneklenen ek bir kontrol varsa gerçek ayar listesiyle eşleştirilmeden bağlanmaz. Hesap bağlılığı ile eşitleme durumu ayrı yazılır. Çevrimdışı skor listesinde kayıtlı veri olduğu belirtilir. Yükleniyor, boş liste, bağlantı hatası, giriş gerekliliği ve işlemin iptali birbirinden ayrılır.

Yeni oyun ve veri silme kendi açık onaylarını kullanır; tam olarak neyin etkilendiğini mevcut kayıt akışından alır. Yapımcılarda yalnız gerçek katkılar ve gereken atıflar yer alır. Parlaklık bileşeni yalnız etkin kullanıcı yoluna bağlıysa sunulur; sırf kaynak dosyası var diye yeni bir menü eklenmez.

## 5. Sekiz odanın sanat yönü

| Oda | Renk / malzeme hedefi | Korunacak işlevsel sınır |
|---|---|---|
| Salon | Derin teal vurgu, mercan döşeme, açık meşe, mint bakım ürünleri | Onaylı TV/koltuk/berjer/yatak/tepsi planı; CAT yalnız burada |
| Banyo | Aqua seramik, açık terrazzo, pamuklu kumaş | Gerçek duşun açık yarı girişi; kapı ve ürün hacimleri |
| Mutfak | Sıcak sarı, kobalt ayrıntı, ahşap ve sırlı karo | Sabit ürün yerleri ve temas noktaları |
| Yatak odası | Periwinkle, gül tonu, açık ahşap, yumuşak kumaş | Mevcut ROOM rutinleri ve açık girişler |
| Bahçe | Zengin yeşil, mercan çiçek vurgusu, doğal taş | Açık geçiş, çit fiziği, gerçek oyuncak temasları |
| Balkon | Gökyüzü mavisi, terracotta, örgü yüzey | Ön korkuluk görsel kesiti ve fizik sınırları |
| Avlu / Patio | Sıcak taş, gölgeli teal, kanvas | Katalog planı; tentenin/bitkinin oyun alanını kapatmaması |
| Üst kat | Açık ahşap, mürekkep mavisi, sıcak okuma ışığı | Çatı kesiti, pikap ve mevcut ürün planı |

Malzeme yenilemesi, bütün odaların aynı parlak plastikte yeniden boyanması değildir. Pah, normal haritası ve temas gölgesi kontrollü kullanılacak. Büyük ışık farkları önce mevcut ışık/post-process ile denenir; ek canlı ışıklar varsayılan çözüm değildir.

## 6. Uygulama sırası — yalnız onaydan sonra

| Aşama | Somut iş ve çıktı | Bağımlılık / kabul kapısı |
|---|---|---|
| 0. Tasarım kararını kilitle | Bu pano setine göre renk, kedi görünümü, UI yoğunluğu ve dünya hedefini netleştir; kabul edilen görselleri sürümlü kaynak yap | Kullanıcının açık uygulama onayı |
| 1. Tek referans kesit | Salon HUD + bir mağaza görünümü + bir diyalog + Runner'ın kısa parkuru gerçek oyunda aynı kaliteye getirilir | Tam yayılım öncesi yan yana gerçek kare/mockup karşılaştırması; mobil maliyetin ilk ölçümü |
| 2. Ortak arayüz sistemi | Renk/ölçü/font/ikon/durum kaynakları, modal, kart, sekme, buton, balon, bildirim | Eski builder'ların sonradan üstünü boyaması önlenir; tekrar üretimde tek görünüm |
| 3. Kedi ve görsel üretim | On ırk görünüm uyumu, ortak portre ışığı, üç komut pozu, ikon ailesi; malzeme/rig denemesi | Irk kimliği ve iskelet uyumu; on ırkta hareket/temas karşılaştırması |
| 4. Ev ve katalog | Sekiz oda malzemesi/ışığı, HUD/etkileşim, mağaza/oda/kedi/Birlikte/rehber/görev | Oda planları ve sahiplik korunur; 103 ürün ve sekiz oda fotoğrafı güncellenir |
| 5. Mini oyunlar | Runner çevre seti ve dokuz varyasyon; Catch çevre/arena; giriş/HUD/öğretici/can/pause/sonuç | Engel/jeton mesafesi, Catch temasları ve kontrol pencereleri korunur; canlı hazır Play'den fotoğraf |
| 6. Duygu ve hareket | Konuşma balonları, ilk tanışma, kutlama, ödül/başarı bildirimi, UI geçişleri | Hareket çizelgesi ve azaltılmış hareket; kullanıcı girişini bekleten gereksiz efekt yok |
| 7. Tam durum taraması | Uzun metin/klavye, sahiplik/kilit/bakiye, boş/hata/offline, reklam/IAP hazır değil, silme/iptal | Tüm görünür eylemler doğru hedefe gider; modal arkası kapalı; kaydırma gerçek girdiden çalışır |
| 8. Teslim kontrolü | Gerçek ekran/video galerisi, performans karşılaştırması, test/validator raporu, dosya ve kaynak dökümü | APK yalnız ayrıca açıkça istenirse; sonucun güzel resimden ibaret olmadığı gerçek karelerle gösterilir |

İlk referans kesiti tamamlamadan bütün projeyi topluca yeniden boyamak önerilmiyor. Her aşama, kendi tamamlanmış görseli ve davranış kanıtıyla kapatılır. Bu sıra yeniden çalışma riskini azaltır; burada gün/saat süresi verilmesi henüz yapılmamış model ve cihaz ölçümüne bağlı olduğundan güvenilir değildir.

## 7. Teknik uygulama haritası

Bu bölüm ileride hangi sistemlerin ele alınacağını gösterir; bu teslimde bu dosyalara dokunulmadı.

| Alan | Mevcut bağlanma noktaları | Uygulama yaklaşımı |
|---|---|---|
| Ortak görünüm | `JoyfulScreenBuilder`, `PremiumUiSystemRebuild`, `PremiumTypography`, `LowPolyPanelGraphic`, ortak ikon kaynakları | Birbiri üzerine renk basan ek geçişler yerine tek sahipli stil kaynağı; eski yollar kontrollü yönlendirilir |
| HUD / giriş | `PremiumHomeDockLayout`, `TopHudResponsiveLayout`, `HomeWorldViewport`, `PremiumScrollInput`, `ActivityPromptController` | 80 px alt bölge, SafeArea ve girdi sözleşmesi korunarak yerleşim yenilenir |
| Diyalog / kutlama | `CatDialogueView`, `CatSpeechBubble`, `TutorialSpotlight`, üç celebration view, `HomeRewardToast`, `PremiumModalBackdrop` | Aynı portre ailesi; tek modal yaşam döngüsü; kapanış/iptalde kaynak temizliği |
| Katalog / odalar | `ShopPanelController`, `RoomSelectorPanel`, `CatBreedShopPanel`, `CatCompanionPanel`, ilgili builder'lar | Yeni kart/sekme yüzeyi mevcut satın alma ve sahiplik işlemlerine bağlanır |
| Dünya / fotoğraf | `HomeRoomPremiumFinishBuilder`, `ArcadeMiniGameArtBuilder`, Blender üreticileri, katalog/oda/komut/mini oyun preview builder'ları | Görsel kaynaklar tekrar üretilebilir; değişen içerik ve kart aynı teslimde güncellenir |
| Hareket | `CatMovement`, `CatActivity` ailesi, `MiniGameAnimationBuilder`, `MiniGameCatAnimation`, `RunnerGroundContact`, Catch player | Önce sunum/katmanlama; oyun fiziğini veya geçerli temas çözümünü estetik gerekçeyle bozma yok |
| Ayarlar / online | `SettingsPanel`, `PrivacyDataPanel`, `LeaderboardPanel`, `DiamondStorePanel` | Görünüm ve durum metni yenilenir; gerçek sağlayıcı/ödül sonuçlarına dayanır |

## 8. Koruma ve doğrulama ölçütleri

- Üç sahne / tek etkin dünya kamerası düzeni ve mobil profil korunur. Menü fotoğrafı için sürekli yeni kameralar açılmaz. Portre üst sınırı 512² / 15 fps, küçük görünüm 256² / 1× kalır; azaltılmış harekette gereksiz tekrar çizim durur.
- Ortak oda kamerası `(0,3.6,-6.5)`, `(23,0,0)`, temel FOV 38 korunur. Konsept çizimlerdeki perspektif farkları kamerayı değiştirme talimatı değildir.
- CAT 5/1, sekiz sabit ROOM planı, gerçek bakım nesnesi kontrolü, açık giriş mesafeleri, kapı/yatak/tepsi eski kayıt düzeltmeleri korunur. Oda fotoğrafları sahip olunmayan ürünü oyuncuya kazandırmaz.
- Otur/Loaf yalnız gerçek dinlenmede +0,35 enerji/sn; mobilyada +0,75/sn. Giriş/çıkış/pause ve miyavda bonus yok; üst sınır 100. Bu mockup yeni jeton/bağ/görev ödülü tanımlamaz.
- Runner koşu/eğilme/toparlanma son poz teması, .033 m yol ve rampa profili korunur. Eğilme/kalkış pencereleri ve iskelet boyutları görsel efekt için değiştirilmez. Catch yön/havada sabitlik/ön pati teması/tek fare kuralları korunur.
- Türkçe atlaslar, `m_ClearDynamicDataOnBuild=false`, yerelleştirilmiş ürün ve eylem adları korunur. Türkçe ve İngilizce uzun metin, 16:9, 4:3 ve 20:9 görüntüleri alınır; mobil klavye/odak kaybı ayrıca kontrol edilir.
- Modal açılışında dünya bir kez yakalanır; HUD/alt şerit görüntüye karışmaz. Kapanışta render hedefi ve coroutine bırakılır. Dekor raycast almaz; açık modalın arkasındaki dünya/HUD girdi alamaz.
- Fiziksel cihazda aynı sahne ve aynı koşullarda CPU/GPU kare süresi, bellek ve uzun oturum sıcaklık karşılaştırması yapılır. RAM ≤4 GB'de 30 FPS mevcut hedeftir; ölçülmemiş cihaz için performans sözü verilmez. Tek tek tüy, sürekli tam ekran blur ve aşırı alfa katmanı varsayılan çözüm değildir.
- Uygulama sonrası ilgili native giriş/mini oyun/ırk testleri, tam EditMode ve validator çalışır. Ürün geometri/temas değişirse 800 ROOM / 170 CAT geçmiş matrisinin ilgili kapsamı yeniden doğrulanır; eski test sayıları yeni başarı gibi sunulmaz.
- QA ayrı kayıt kopyasında yürür. Asıl kayıt ve yedek başlangıç/bitiş hash'i karşılaştırılır. Commit/push manuel; APK üretimi ve bilgisayarı kapatma bu çalışmanın parçası değildir.

## 9. Onay kapsamı

İstenen onay; bu görsel yönün, tüm ekran ailelerinin ve aşamalı uygulama planının oyun üzerinde uygulanmaya başlanması içindir. Onaydan önce yapılan iş inceleme, mockup üretimi ve bu teklif dosyalarıdır. **Unity sahneleri, Assets, oyun kodu, Blender modelleri ve oyuncu kaydı değiştirilmedi; Android derlemesi başlatılmadı.**

İlk bakılacak görseller: `01` ana oyun; `05–06` mağaza; `08–09` dünya; `15–16` mini oyun; `04` diyalog; `20` ve `23` hareket. Bunlar onaylanacak kalite hedefini en açık gösteren parçalardır. Tam kapsam galeride ve ekran envanterinde yer alır.
