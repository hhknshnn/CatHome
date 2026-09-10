# Oda eylemlerinde kamera görünürlüğü ve gerçek temas

9 Eylül 2026. Kullanıcının mevcut ve gelecekteki bütün oda eylemleri için istediği önden/yan görünüş, [AGENTS.md](../../AGENTS.md) içinde kalıcı kuraldır. Bu belge uygulanmış geliştirme sözleşmesini ve ilgili üretim/doğrulama yollarını açıklar. Son test ve görsel kabul sonucu ayrı QA raporunda tutulur; burada test başarı sayısı verilmez.

## Ölçüm ve sınırlar

Oyuncunun aynı odadaki gerçek kamerası referanstır. Son animasyon, IK ve destek düzeltmesi uygulandıktan sonra gerçek iskeletin kalça (`DEF-spine`) ve omuz (`DEF-spine.003`) dünya konumları ölçülür. Yatay kalça→omuz yönü ile gövde orta noktasından kameraya yatay yönün normalize dot çarpımı kullanılır. Yalnız kedi kökü veya prefabın ileri ekseni kanıt değildir.

| Değer | Anlamı |
| --- | --- |
| Dot ≥ 0 | Çalışma/dinlenme gövdesinin önden veya yandan okunması |
| `MinimumViewDot = -.01` | Otomatik ölçümün küçük sayısal toleransı |
| `MaximumViewAngle = 65°` | Serbest poz için ortak yardımcının kamera açısı sınırı |
| `PreferredViewDot = .30` | Temas tarafı seçiminin kök yönündeki güven payı |

Bu değerler `CatActivityFacing` içinde bulunur. `.30` tercih payı, son çizilen gövdenin ölçümünü ortadan kaldırmaz. Önceki taslaktaki 80° sınırı geçersizdir.

Oturarak bakışta gövde yönü ile hedef yönü ayrı modellenir: `TryResolveViewFacing` kameraya okunur gövdeyi seçerken gerçek hedefe baş dönüşünü en fazla 35° ile sınırlar. Yalnız bu Sit çağrısında `CatFurnitureGaze` 40° yaw kapasitesi kullanır; diğer jestlerin 16° sınırı değişmez. Bu pay, arka duvardaki bir resme gövdeyi arkaya çevirmeden bakmayı sağlar. Pati/Pounce gibi temaslı eylemlerde gövde gerçek hedef doğrultusunu korur; `.30` tercihini sağlamak için hedef fiziksel konumundan koparılmaz.

Durağan çalışma taraması son duruş/dönüş geçişinden sonraki ilk `.35` saniyeyi dışarıda bırakır ve kalan gerçek çalışma evresinin en kötü yönünü saklar. Tek iyi kare yeterli değildir. Sıfır vektör, eksik kemik veya geçersiz sayı ölçüm eksikliğidir. Yön dot değeri örtülmeyi ölçmez: başın, ön patinin ve gerçek temasın ürün arkasında kaybolması oyuncu kamerasındaki görüntüyle ayrıca denetlenir.

## Fiziksel olarak doğru görünürlük

Görünürlük uğruna gerçek pati/ağız teması, açık giriş, mobilya desteği, komşu eşyalar, duvarlar, kedi ölçeği veya erişilebilir yol bozulamaz. Eşyayı gizlemek, collider kapatmak, kemiği uzatmak, kediyi eşyanın içinden geçirmek veya görüntü derinlik testini kaldırmak çözüm değildir. Geçerli taraf yoksa ürünün konumu/yönü ve temas noktaları birlikte yeniden düzenlenir.

| Eylem | Uygulama | Korunan koşul |
| --- | --- | --- |
| Serbest otur/uzan/miyav ve açık zeminde tutulan poz | `Resolve` ve varıştan sonra kısa `Turn` | Konum sıçraması, ayak kayması, erken dinlenme ödülü yok |
| Raf, havlu, salıncak, dar minder | `AlongAxis` ile aynı destek ekseninin iki güvenli ucu | Gövde ve patiler gerçek destek üzerinde kalır |
| Kitaplık, TV, mangal, ayna, pencere gibi bakış | Gerçek görünen hedefe bakan yakın, açık duruş | Aşırı boyun dönüşü veya içi boş ürün merkezi hedefi yok |
| Mama/su, tırmalama, kâğıt, yoğurma gibi temas | Gerçek yüzey ve mevcut erişim yarıçapı içinde açık çalışma tarafı | Pati/ağız ürünle temasını korur |
| Kapalı yatak, duş, tünel | Ürünün açıklığı, yaklaşımı ve çalışma yönü birlikte üretilir | Camdan, duvardan veya kapalı yan yüzeyden giriş yok |
| Küvet kenarı, tünel, sürtünme yolu, kovalama | Fiziksel olarak geçerli başlangıç ucu ve gerçek yol yönü | Kameraya sabitlenmiş geri/yan kayma yok |

Yürüme, koşma, sürünme ve sıçrama yönü gerçek hareketi izler. Kameraya baktırmak için seyir yönü kilitlenmez. Çalışma noktasındaki kısa dönüş duraklatmaya uyar. `Resolve`, gerçek temas yönünü keyfî çevirmek için kullanılmaz; `AlongAxis`, tek yönlü bir açıklığın iki yönde güvenli olduğunu varsaymaz.

## Salonun açık gözlem alanları

`LivingRoomGazeLayoutBuilder` pencere ve uzun bitkinin kalıcı yerleşim kaynağıdır. Eski pencere girişinin `(2.40,0,.15)` konumu gerçek koltuk collider'ının içindeydi; yakınlık yarıçapını büyütmek bu girişi açmaz. Bitkinin eski `(3.30,0,1.90)` konumunda ise yakın ve açık noktalar bulunmasına rağmen mevcut mobilyalar arasında kameraya okunur oturma yönü bulunamadı. Bu iki durumda gerçek giriş/ürün düzeni değiştirildi; gövde ve baş açıları gevşetilmedi.

| Bağ | Kalıcı dünya konumu | Korunan özellik |
| --- | --- | --- |
| Uzun bitkinin bütün ürün kökü | `(3.30,0,1.15)` | Model, collider ve alt noktalar birlikte taşınır; ölçek ve yaw aynıdır |
| Bitki arka giriş noktası | `(3.30,0,1.90)` | Ürüne göre yerel `(0,0,+.75)`; gerçek bitkiye önden/yan okunur bakış |
| Pencere gözlem girişi | `(2.45,0,.85)` | Koltuğun açık arka tarafı; hedef mevcut gerçek cam yüzeyidir |

Mevcut FBX'lerin son ölçek ve dönüşleriyle yapılan ölçümde yeni bitkinin yatay dış sınırları `x=[2.99987,3.58758]`, `z=[.853184,1.487518]` oldu. Gerçek meshleri kapsayan ayrık dışbükey izdüşümlerin bitki–lamba boşluğu `.65351 m`, bitki–berjer boşluğu `.50681 m`; ikisi de `.35 m` eşya ayrımının üzerindedir. Bitkinin arka girişinin bitki dış sınırına payı `.41248 m`'dir. Bu ölçüm, native controller ile yaklaşım/çıkış ve son animasyon kabulünün yerine geçmez.

Lambanın ürün kökü `(2.65,0,2.24)` olarak kalır; `(2.65,0,1.20)` onun **girişidir**. İkisi karıştırılıp lambanın kendisi taşınmaz. Berjer, koltuk ve diğer ürünler bu iki gözlem alanını güncellemek için yeniden üretilmez. `StoreCatalogAssets`, `RoomProductInteractionBuilder`, `SceneObservationFacingBuilder`, `LivingRoomArrangementBuilder` ve `CameraFacingAuthoringBuilder` aynı kaynağa bağlıdır. Dar yenileme `LivingRoomGazeLayoutBuilder.ApplyAndSave()` ile yapılır; eski bitki veya pencere koordinatları tekrar pişirilmez.

## Gerçek bakış yüzeylerinin üretimi

`SitLookFacingBuilder.Configure(prefabRoot, definition)`, son görsel mesh üçgenlerini ürünün güncel katalog konumu/dönüşü ve ortak oda kamerasıyla değerlendirir. Kameraya dönük yüzeyler ve ürünün kendisi tarafından örtülmeyen gerçek noktalar seçilir. `.10 m` hücreleme ve uzamsal dağılımla en fazla 32 hedef saklanır; runtime için üçgen kopyası veya yeni collider üretilmez.

Sahiplik nedeniyle kapalı olan bağlı `MovableRoot`, `unlockedContent` ve mağaza `visualRoot` kapları üretim sırasında okunabilir. Ayrı kapalı eski çocuklar ve devre dışı renderer'lar dışarıda kalır; sahne sahipliği veya görünürlüğü değiştirilmez.

Oturarak bakış gerçek ürün yüksekliğinin `.28–.90` bandını, `.58` yüksekliği başlangıç tercihini kullanır. Böylece yüksek bir tablo eski alçak merkez noktasına bağlanmaz. Pati/Pounce hedefleri gerçek üçgenin yazılmış temas yüksekliğiyle kesişiminden türetilir; yüksek dekor yüzeyi pati erişiminin yerine geçmez. Geçerli yüzey bulunamazsa ürün adıyla yazarlık hatası üretilir.

`ConfigureSceneSurface(activity, surface)` aynı çözümü serbest oda eyleminin gerçek yüzeyine uygular. `SceneObservationFacingBuilder` pencere bakışını sahnedeki pencereye, kuş bakışını mevcut avlu ağacına bağlar. Hedefler eylem kökünün yerel uzayında tutulur. Runtime bakış çözümü gerçek hedefe görüşü ve erişilebilir duruşu tekrar denetler.

Seated duruşun gerçek controller yarıçapı ve köke göre ileri merkez ofseti birlikte hesaba katılır; nokta `HomeRoomBoundary` içinde kalır. Kontrol geri verildiğinde oda sınırının kediyi komşu eşyanın içine itebileceği bir çalışma noktası seçilmez. Controller ile yürünecek yol actor-aware `TryFloorPath(cat, ...)` kullanır; controller kapalıyken kullanılan mevcut animasyon yollarının varsayılan `.27 m` kapsülü değişmez.

## Su teması ve yeni desteklerin kabulü

`CatSipMouthBuilder` / `CatSipMouthCatalog`, ilgili ırkın gerçek ağız yüzeyinden en fazla 24 köşenin kemik ağırlığı ve bind konumlarını saklar. Runtime `CatSipHeadMotion` bu profilden son ağız konumunu çözer; çene kemiği merkezi tek başına su teması sayılmaz. Gerçek modern kedi meshinin CPU kopyası içme sırasında açılmaz. Profil veya kemik bağı eksikse eylem su ödülü vermeden iptal edilir ve kontrol bırakılır.

Sığ olmayan kaba erişim, ön gövdede en fazla **34°** sınırlı eğilme ve kaynak poza göre `30°/25°/20°` boyun eklem sınırlarıyla çözülür; toplam boyun sapması en fazla `75°` olur. Ön patiler iki kemikli dönüş çözümüyle kaynak destek noktalarında korunur; uzuv boyu, kemik bağlantı konumu ve ölçeği değiştirilmez. İlgili native doğrulama ön pati yer değiştirmesini **`< .0001 m`** ile ölçer; bu değer pati tutma hatasıdır, ağız erişim mesafesi veya tüm ürünlerin genel fizik toleransı değildir. Pause son pozu dondurur; iptal gerçek kaynak pozu geri koyar. İkisinde de pati destek noktaları korunur.

Gelecekte eklenecek kap/tezgâh/kenarlık için destek kabulü, patinin altında erişilen **üstteki gerçek taşıyıcı yüzeyi** denetlemelidir. Daha alttaki tezgâhı bulan bir ışın, patinin üst kenara gömülmesini veya boşluğun üzerinde durmasını geçerli kılmaz. Dört pati ve küçük temas alanı ürünün gerçek üçgenleri üzerinde, engelleyici üst yüzeyle kesişmeden desteklenir; yürüyüş ve iniş de aynı geometriyi kullanır. Mevcut `SupportHeight` yakın yüzey araması veya tanısal `RaycastAll` içinde herhangi bir ürün isabeti bulunması tek başına bu yeni ürün kabulünün tamamlandığı anlamına gelmez. Yeni destek geometrisi için üst yüzey/kenar ve son skinned pati ölçümü birlikte eklenir.

## Kaynak ve sahne yenileme

Önce modelin son ölçeği/dönüşü, gerçek collider ve destek yüzeyi belirlenir; temas noktaları bu geometriden üretilir. Destek hizalaması, kökte seçilen görünür yönü son anda ters çevirmemelidir. Son kanıt animasyonlu gövdeden alınır.

`RoomProductInteractionBuilder.UpgradePrefab` ölçülmüş geometri/noktalardan sonra bakış ve su temas üreticilerini, ardından destek/giriş üretimini çağırır. `CameraFacingAuthoringBuilder.ApplyAll()` güncel ROOM/CAT içindeki ilgili SitLook/Sink prefablarını yeniler; mevcut sahnelerdeki hedef ve anchor bağlarını güncel kaynakla eşler. Diğer odalarda girişler mevcut dolu oda geometrisine karşı tekrar pişirilir; serbest gözlem hedefleri de bağlanır. Yalnız açtığı sahneleri kapatır ve önceki etkin sahneyi geri koyar.

Bu dar yenileme oda hiyerarşilerini ve materyalleri topluca yeniden üretmez; salonun iki gözlem alanına ilişkin açık yerleşim düzeltmesini de uygular. Bu nedenle bu geçişi “bütün ürün konumları aynı kaldı” diye özetlemek yanlıştır. Ürün konumu, yönü veya görünümü gerçekten değiştiğinde ilgili kaynak, gerçek sahne, ürün kartı ve oda fotoğrafı aynı sonucu taşımalıdır. Değişmeyen oda planları sebepsiz yeniden hesaplanmaz.

## Gelecek oda ve eylemin kapsam kapısı

`HomeActivityFacingValidation`, sahnelerdeki somut `CatActivity` türlerini açık incelenmiş tür kümesiyle karşılaştırır. Bilinmeyen tür, kendi kamera/temas/yol politikası ve native kanıtı eklenene kadar hata verir. `LevelContentValidator.ValidateHomeRoomScenes` bu denetimi bütün `HomeRoomService.Rooms` için çağırır. Statik kapsama listesinde bulunmak, gerçek animasyonun başarılı olduğu anlamına gelmez.

Yeni oda, ürün, eylem, klip, destek düzeltmesi veya yerleşim aynı kapsama dahildir. Tarama güncel ROOM/CAT kimliklerinden türetilir; tarihsel kaldırılmış dekor veya eski Mouse Hunt eklenmez. Bugünkü koleksiyon sayısı gelecek kapsamın sabit sayısı değildir. CAT yalnız salonda ve aynı anda en fazla beş ürün / bir yatak kuralıyla sırayla denenir. Ana yatak, mama/su ve kedi komutları ayrıca kapsanır.

Envanter ürünü ile fiziksel rutin örneği ayrı sayılır. Aynı ürüne bağlı iki tarihsel istasyon tek ürün fakat iki rutindir. Etkileşimsiz dekorlar kimlik ve gerekçeyle ayrılır; sayıyı tamamlamak için yeni eylem uydurulmaz.

## Test ve görsel kabul

- `CameraFacingRoomAuditTests`, gerçek çalışma evresini son karedeki kemiklerle ölçer; giriş, tamamlanma, yön ve örnek sayısı sorunlarını ürün/evre adıyla toplar. Beş serbest sahne eylemi mağaza envanterinden ayrı doğrulanır.
- `CameraFacingMovingWorkTests`, hareket ederek yapılan yemeği, dış mekân kazı evrelerini ve gerçek tünel geçişini kendi temas/hareket ölçümüyle denetler. Tünel seyri durağan poz kuralını geçirmek için ters yürütülmez.
- `no_stationary_stage` / `not_assessed`, hiç başlayamama, beklenen evreye ulaşamama, sıfır örnek veya zaman aşımı sessiz başarı değildir. Durağan ölçüm uygulanmıyorsa uygun hareket/temas kanıtı gerekir; genel testin yeşil olması tek başına satırı kapatmaz.
- Varsayılan ırkla bütün oda taraması, on ırk temas/gövde denetiminin yerine geçmez. Temas, dar destek ve son görsel hizalamanın farklılaştığı durumlar ilgili ırk testleriyle tamamlanır. İptal, pause, oda boşaltma, sahiplik kapanışı ve kontrolün geri verilmesi korunur.
- Gerçek oyuncu kamerasından, komşu eşyalar görünürken çalışma/temas görüntüsü incelenir. Yakın çekim ek kanıttır; oyuncu kamerasındaki örtülmeyi gizleyemez. Hareketli evre için gerçek kare dizisi veya video kullanılır.
- QA ayrı kayıt kopyasında yürür; asıl kayıt ve tercihler korunur. Deterministik test zamanlaması fiziksel cihaz FPS/GPU ölçümü değildir. APK, canlı yayın veya commit/push bu denetimin yan etkisi değildir.

Bu geçişin sonuçları [CAMERA_FACING_2026-09-09 QA klasöründe](QA/CAMERA_FACING_2026-09-09/) tutulur. Eksik kapsam veya açık ölçüm sorunu varken “bütün eylemler doğrulandı” denmez. Son başarılı XML, kapsam raporu ve doğrulanmış gerçek medya tamamlanınca sonuç raporu ayrıca yayımlanır.
