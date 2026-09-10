# Sabit oda düzeni ve kedi etkileşimleri — 5 Eylül 2026

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

Tamamlandı. Unity 6000.4.4f1 içinde **391/391 EditMode**, **53/53 PlayMode** ve sekiz odada **800/800 eşya–ırk kombinasyonu** geçti. Son canlı tur görünür gövde/yüzey temasını da ölçer. Validator 0 hata / 0 uyarı. Bu kayıt Unity Editor doğrulamasıdır; Android cihaz performans testi değildir.

## Kullanıcı kararı

- Alınan ROOM eşyası otomatik olarak tasarlanan yerine gelir ve odada görünür kalır.
- ROOM ürünlerinin mevcut özel konumları da tasarlanan konuma döner; sürükleme, açı değiştirme ve depolama kalkar.
- Sekiz odadaki 80 koleksiyon eşyası, 10 kedi ırkıyla uygun dinamik etkileşim taşımalıdır.
- Gerçek geometri, yaklaşma yolları, temas ve iç içe geçme kontrol edilir.
- Ekonomi açma kararı verilmedi; mevcut FREE TEST kapısı korunur.

## Ölçülen başlangıç sorunları

- GardenPergola: 2 × 1.85 × 1.5 katı kutu, sığınağın açık içini kapatıyor.
- PatioParasol: 2.2 × 1.85 × 2.2 katı kutu, şemsiye altını kapatıyor.
- Diğer oda ürünleri de gerçek şekil yerine katalog kutusuyla çarpışıyor.
- Living Room koleksiyonunun 10 prefabında CatActivity yok; önceki 80/80 etkileşim kaydı doğru değil.
- SwingRide oturma klibini başlatmıyor. Çok sayıda diğer rutin iskelet pozu yerine tüm CatRoot'u esnetiyor.
- SitLook'ın varsayılan oturma tepkisi yanlışlıkla kaşınma klibini başlatıyor.
- Irkların ortak kliplerle taban ofsetleri farklı. Kendi kliplerinde de ofset var; klip bağlantısını rastgele değiştirmek çözüm değil.
- Temas taramasında KitchenSinkCabinet, PatioDiningSet ve PatioPergolaArch noktalarında destek bulunamadı. Dar raflar/kenarlar tam gövde boyutu olarak yorumlanmamalı; kediyi küçücük yapmak kabul edilemez.

## Tamamlanan uygulama ve doğrulama

- [x] Store v7 eski ROOM depolama/konumlarını yok sayar; CAT yerleşimleri, sahiplik ve para/ilerleme korunur.
- [x] Alım ve test alımı doğrudan görünürlük sağlar; bağımlı TV/kitaplar otomatik bağlanır.
- [x] Eski yerleştirme girişleri kapatıldı; menü builder'ından EDIT ROOM çıkarıldı.
- [x] Tüm 80 prefabın katalog kutusu tetikleyiciye çevrildi; gerçek FBX geometrisi MeshCollider olarak kullanıldı.
- [x] Living Room'a 10 aktivite eklendi; CatActivityKind 77–86 sona eklendi.
- [x] Rutinler yürüyüş/oturma/uyuma/yeme/içme/pati aşamalarını seçiyor.
- [x] Temaslar yukarı bakan gerçek üçgen yüzeylerden ölçüldü; kuyruk dışındaki gövde pozu kullanılır, ırk ölçeği değiştirilmez.
- [x] Desteksiz noktalar düzeltildi; beş dar/kapalı raf noktası açık üst yüzeylere taşındı.
- [x] 8 odanın tam koleksiyon yaklaşma/çıkış zemini tarandı; tüm işaretler açık ve erişilebilir. Loft pikap/masa yerleşim çakışması ayrı tasarlanmış yerle kapatıldı.
- [x] Eski ROOM sürükleme/depolama testleri yeni sözleşmeye dönüştürüldü; gerçek geometri, 80 × 10 ırk ve salıncakta ırk değiştirme/iptal testleri eklendi.
- [x] Gerekli prefab/sahne/UI rebuild ve tüm EditMode kontrolleri.
- [x] Gerçek Play'de mağaza alımı, HUD, temiz Console, validator ve kanonik üç sahne.
- [x] Son temas düzeltmesinden sonra Test Runner PlayMode ve yenilenen etkileşim görselleri.

Kalıcı sonuç dosyaları [QA/FixedRoomInteractions](QA/FixedRoomInteractions/README.md) altında; 26 görsel `Assets/QA/PremiumVisuals/FixedRoomInteractions/` içinde. Geçici ölçümler `Temp/FixedRoomAudit/`, başlangıç kopyaları onun `before/` alt dizininde. Commit/push kullanıcıya aittir.

## Canlı test turunun yakaladığı ek sorunlar

- İlk güncellenmiş temel EditMode turu: **382/382** geçti.
- İlk genişletilmiş PlayMode turu: **46 geçti / 6 başarısız**, 541.7 saniye. Sekiz odanın **800 kombinasyonu** çalıştırıldı; temel 44 testin Garden turu ve beş ırk matrisi oda testi sorun buldu. Rapor: `Temp/FixedRoomAudit/PlayMode-first.xml`.
- Büyük ırkların kuyruk dahil renderer çapı duvar payı olarak kullanılıyordu. Rutin kontrolü geri verince bu pay kediyi duvardan uzağa, ön sıradaki eşyaların içine itiyordu. Sınır artık CharacterController yarıçapıyla eşleşir; adım yüksekliği değişmedi.
- Fırçalanma, yürüyüşten ayrı gerçek tımar klibine geçti. İplik topunun ara hareketleri de açık zemin yolunu izler.
- Aynı yere yığılan etkileşim işaretleri bazı eşyaların düğmesini sürekli gizliyordu. Tam oda üzerinde erişilebilir ve ayrı yaklaşım noktaları hesaplanır; ırk matrisi en yakın eylem seçimini de kontrol eder.
- PatioPergolaArch modelinde eski rutinin varsaydığı 0.78 yüksekliğinde raf yok. Kedi sarmaşığa basmak yerine gerçek üst kafese sıçrar. Asılı koltukta giriş yükseltilmiş mindere sıçrayarak yapılır.
- Temas ölçümünde yalnız yukarı bakan yüzler destek sayılır; alt yüz ve dik sırtlık seçilmez. Kuyruk temas merkezinden çıkarılır. Kedi ölçeği sabittir; dar tabure oturma, ince uzun raf yatma pozu kullanır.
- Satın alım kediyle aynı noktaya denk gelirse, mobilya görünür olduktan sonra kedi en yakın açık zemine alınır.
- Genişletilmiş EditMode ilk turunda 388 testten bir açıklık örneği yanlış noktadaydı: pergolanın merkezi gerçek arka banka temas eder. Test gerçek geçiş alanını (yerel z=-0.35) ve şemsiyenin hem açık altını hem katı direğini kontrol eder.
- İkinci ve üçüncü PlayMode turları **52/53** geçti; sekiz ırk matrisinin **800/800** kombinasyonu tamamlandı. Garden ardışık turundaki başarısızlık ayrıca teşhis edildi: Daisy çıkışındaki çapraz açıklığı dört yönlü yol araması göremiyordu. Görünürlük olay sırasını düzeltmek tek başına yeterli değildi.
- `CatBreedCatalog.Entry.ContactVertexIndices` gövde/kuyruk ayrımını editörde önceden hazırlar. Kaynak FBX'lerin Read/Write ayarı kapalı kalır; oyuncu derlemesi `boneWeights` okumaz. On ırk profili ve indeks sınırları EditMode ile doğrulandı.
- `RoomProductFeedback` bütün 80 üründe kullanım sırasında hafif malzeme aydınlanması sağlar. Katı mobilya ve temas noktaları bu efekt için hareket ettirilmez; azaltılmış hareket tercihinde sabit vurgu kullanılır. Salıncak, iplik, kâğıt ve benzeri gerçek hareketli parçalar kendi rutinlerini korur.
- Son EditMode: **391/391**, iş `b721b6b773cc4fe3923922e86049cf34`. Gerçek Garden sahnesinde Daisy çıkışından iplik oyuncağına çapraz geçiş ve Loft pikap/masa ayrılığı ek testlerle kilitlendi. Eski testlerin dar nişe yerleşmeyi zorunlu kılan varsayımları, ölçülen açık yüzey sözleşmeleriyle değiştirildi.
- Görsel tarama: KitchenPantryShelf, BalconyHerbShelf, LoftTallBookcase, BathroomTowelStorage ve GardenPergola'nın eski noktalarında büyük ırklar üst raf/çatıya giriyordu. Açık üst yüzeyler sırasıyla 1.770 / 1.270 / 2.100 / 1.747 / 1.705 m. Garden sıçraması çatının dışındaki ön zeminden başlar; hayalî ara basamak kullanılmaz. Balkon bitki rafında saksılar arasına oturma pozu seçilir.
- Tam oda karesinde Loft pikap/masa katalog kutuları X'te 0.35 m, Z'de 0.55 m çakışıyordu. Pikap (1.6, 0, -1.4) konumundan boş sol ön alana (-1.7, 0, -1.4) taşındı; yaw ve ürüne yaklaşım yönü korunur.
- Yol araması sekiz yön kullanır. Çapraz adımlar da süpürülen kapsül ile kontrol edilir; köşeden eşya içine kesme yapılmaz. Tam oda yerleşim hesabı aynı geçişleri kullanır ve çıkışın yürünebilir ağa bağlanmasını denetler.
- Dördüncü tam PlayMode turu **53/53** geçti; 800 kombinasyonda başlangıç zeminine dönüş yolu ve ürünün görsel tepkisi de doğrulandı (`PlayMode-fourth.xml`).
- Normal Play: kitaplık mağaza üzerinden alındı; tasarlanan (-3.25, 0, 1.05) konumunda görünür oldu, depolanmadı, yerleştirme modu açılmadı. Kart `IN YOUR ROOM`, geri bildirim `ADDED TO ITS PLACE IN YOUR ROOM!`. Geçici QA sahipliği geri alındı; FREE TEST kapısı korundu.
- UI taraması GameScene'deki eski MainPanelCanvas ile CatHome_UI'deki güncel kopyanın aynı düğmeleri üst üste çizdiğini yakaladı. GameScene kopyası silinmeden devre dışı bırakıldı. Son gerçek Play `GetWorldCorners` taramasında HUD ve mağaza için **0 çakışma**; kamera/listener/EventSystem **1/1/1**, üç sahne doğru ve aktif sahne LivingRoom. Validator **0 hata / 0 uyarı**.
- Son görsel ölçüm, kitaplık üzerindeki görünür kedi ile CPU temas örneğinin farklı boyutta olduğunu yakaladı. 1.5 ölçekli iskelet hiyerarşisinde varsayılan `BakeMesh` örneği tekrar büyütülüyordu. Aynı kamerada canlı skin ve bake edilmiş mesh karşılaştırıldı; `BakeMesh(mesh, true)` görünür skin ile eşleşir. Temas hesabı bunu kullanır. PlayMode matrisine kare sonunda gerçek gövde/yüzey mesafesi kontrolü eklendi (izin: -0.005 ile +0.020 m).
- Son temas değişikliğinden sonra EditMode **391/391**, iş `87303377d0bf4692940bdbd94f165563`. Beşinci tam PlayMode turu **53/53**, 536.77 saniye; sekiz matrisin **800/800** kombinasyonu geçti. 80 Maine Coon temas görüntüsü tekrar incelendi; kitaplık/pergola üzerindeki boşluk kapandı. Kontrol, iskelet hareketi, farklı aktivite pozları, eşyanın görsel tepkisi, temas mesafesi, güvenli zemin ve başlangıca dönüş yolunu kapsar.
- Oda seçici/HOME kartları `RoomPreviewCaptureBuilder.CaptureSilently()` ile güncel yerleşimden tekrar çekildi. Kart yenilemesinden sonraki EditMode XML sonucu da **391/391**, 8.03 saniye, 17:59:23. MCP iş takipçisi bu son tur için başlatma zaman aşımı bildirse de Unity'nin yazdığı XML 391 tamamlanmış testi doğrular; sonuç dosyası kalıcı QA klasöründedir. Aktif test/No Throttling/Play geri yükleme bayrakları temizdir.
