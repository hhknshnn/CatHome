# Cat Home — banyo yerleşimi ve sakin etkileşimler, 9 Eylül 2026

**Tamamlandı.** [Gerçek Unity galerisi](QA/BATHROOM_POLISH_2026-09-09/index.html): **18 güncel PNG, sekiz video** ve ayrıca önceki düzeni gösteren bir karşılaştırma karesi. [Görsel ve video paketi](QA/BATHROOM_POLISH_2026-09-09/Banyo-Gorseller-ve-Videolar.zip). Tam EditMode **504/504**, **24 benzersiz native test başarılı**, içerik doğrulayıcı **0 hata / 0 uyarı**.

Bu geçiş banyodaki on ürünü hizalı bir plana yerleştirir; aynayı bakım arabasının üstüne alır, paspas ve kum kabının sert pati hareketlerini yumuşatır, küvet etkinliğini kenarda denge yürüyüşüyle sınırlar ve duşa akan su ile kısa köpük efekti ekler. Önceki [bakım ve hareket geçişi](CARE_MOTION_2026-09-09.md) ile [eylem güvenliği](ACTION_STATE_AUDIT_2026-09-09.md) temel alınır. Diğer odaların onaylı yerleşimleri bu işin kapsamı dışında kalır.

## Banyodaki on ürünün düzeni

`BathroomArrangementProfile`, banyonun tam koleksiyonu için konum ve yönleri açıkça tanımlar. `HomeRoomLayoutPlanner.PlanAuthored` bu planı mevcut görünürlük, yaklaşım ve engel denetimlerinden geçirir. `BathroomPolishBuilder.Apply()` banyo prefab bağlarını günceller, yalnız banyoyu yeniden planlar ve banyo sahnesini doğrulayarak kaydeder.

Aşağıdaki değerler metre cinsinden **ürün kökünün oda içindeki konumu** ve Y ekseni dönüşüdür; modelin içindeki tek bir parçanın merkezini göstermez.

| Ürün kimliği | X | Y | Z | Y dönüşü |
| --- | ---: | ---: | ---: | ---: |
| `bathroom.shower` | -2.60 | 0 | 2.12 | 180° |
| `bathroom.vanity-sink` | -3.05 | 0 | .40 | 90° |
| `bathroom.wall-mirror` | -.75 | 1.25 | 2.65 | 0° |
| `bathroom.grooming-cart` | -.75 | 0 | 2.20 | 0° |
| `bathroom.towel-storage` | 2.45 | 0 | 2.375 | 0° |
| `bathroom.tub` | 2.85 | 0 | .35 | 270° |
| `bathroom.toilet` | 2.85 | 0 | -1.40 | 270° |
| `bathroom.laundry-hamper` | -3.05 | 0 | -1.10 | 0° |
| `bathroom.litter-box` | -1.60 | 0 | -1.00 | 0° |
| `bathroom.bath-mat` | 0 | 0 | .25 | 0° |

Ayna kökü 1.25 m yüksekliğe taşınır ve bakım arabasıyla aynı X hizasına gelir. Ayna yaklaşımı arabanın yanındaki açık zeminden alınır; iki ürünün girişleri ayrı kalır. Ayna eylemi `SitLookReaction.Sit` kullanır: kedi oturup aynaya bakar.

Duşun yakın eylem metni de sadeleştirilmiştir: ürün adıyla eylemin birleşip “Duş duş al” üretmesi önlenir; düğmede **Duş al** görünür. Bu ürün dışındaki kısa adlar, enerji yetersizliği metni ve İngilizce dizilim korunur.

[Diğer oda planlarının karşılaştırması](QA/BATHROOM_POLISH_2026-09-09/other-room-layouts-unchanged.json), `RoomLayoutCatalog` içindeki **60 banyo dışı kaydın değişmediğini** doğrular. Bu kanıtın kapsamı katalogdaki bu 60 kayıttır. Salonun ayrı yerleşim düzeni ve beş CAT/bir yatak sınırı korunur.

## Paspas, kum kabı ve küvet

Banyo paspası, `MatKneadActivity` içinde kendi ürün kimliğiyle nazik yoğurma yolunu seçer. Ön patiler yaklaşık **1.05 saniyede bir**, en fazla **2.4 cm** yükselerek sırayla basar. Yoğurma sırasında oyuncu kökü zıplamaz, gövde ölçeği sıkıştırılmaz. Ardından oturarak geçiş ve mevcut sürekli dinlenme gelir; oyuncunun bitirme eylemi ve dinlenme enerji sözleşmesi korunur. Bahçe papatyalarının önceki nazik hareketi aynı kalır; diğer paspas türlerinin davranışı bu koşula alınmaz.

Kum kabının değişikliği yalnız `BathroomLitterBoxId` ve `LitterDig` birleşimine uygulanır. Kazma ve üzerini örtme evreleri, nötr gövde üzerinde dönüşümlü sığ pati hareketi kullanır: **.9 saniyelik** basış, en fazla **1.6 cm** kaldırma ve **3.6 cm** ileri/geri uzanma. Dönüş daha sakin tamamlanır. İçeri girme, açık zemine çıkma, CharacterController ve sahip olunan hareket kilidinin bırakılması korunur.

`TubEdgeWalkActivity`, küvet kenarına çıkar, kenarda denge yürüyüşünü yapar ve ulaştığı uçtan açık zemine iner. Kenar yürüyüşünün ardından suya pati vurma bölümü kaldırılmıştır. Eski seri alanlar prefab uyumluluğu için kalabilir; çalışan rutinde suya uzanma evresi yoktur. Küvetin katı gövdesi ve mevcut güvenli giriş/çıkış yaklaşımı korunur.

## Duşta akan su ve köpük

`ShowerRinseActivity`, `CatShowerWaterFx` bileşenini gerektiğinde runtime'da ekler. Giriş ve çıkış sırasında efekt kapalıdır. Su yalnız gerçek durulanma evresinde başlar. Hafif köpük kedinin gövdesinde ve duş tepsisinde kademeli görünür; silkelenme başında su aynı çağrıda kesilir. Gövdeden ayrılan köpük kısa bir saçılmanın ardından söner, tepsi köpüğü bulunduğu yerde kaybolur. Etkinlik tamamlanmadan ve kedi çıkışa yürümeye başlamadan efekt kapanır.

Normal sunumda **18 su damlası/şeridi ve 12 köpük**, azaltılmış harekette veya düşük bellekli mobil profilde **6 su damlası/şeridi ve 6 köpük** kullanılır. Azaltılmış hareket dalı su çizgilerini sabit tutar ve köpüğü saçmadan kısa bir sönmeyle bitirir. Normal saçılmanın yatay mesafesi en fazla yaklaşık 25 cm'dir. Mevcut 2.6 saniyelik durulanma, .65 saniyelik silkelenme ve enerji/ödül değerleri değiştirilmez.

Tek tekrar kullanılan mesh ve tek renderer yaklaşık 576 köşeyle çalışır. Collider, giriş yüzeyi, yeni kamera veya gölge eklenmez. Mevcut `CatHome/Window Sun Beam` shader'ı köşe rengi ve alfa sağlar; bu shader, build'e dahil salon sahnesindeki gerçek pencere materyalinden referanslıdır. Her kare için yeni mesh, materyal veya efekt nesnesi üretilmez.

Su kaynağı, `BathroomShower_Premium.fbx` içindeki gerçek yağmur başlığından ayrı headless Blender sürecinde ölçülmüştür. Nozul altı mesh Y değeri **1.80696738**'dir; kaynak bunun 5 mm altına konur. Model dosyası ölçüm sırasında değiştirilmemiştir.

| Koordinat alanı | Ölçülmüş su çıkışı |
| --- | --- |
| Mesh-local | `(0, 1.80196738, -.03000030)` |
| Modelin `.94206446` ölçeği, 180° dönüşü ve Z ofseti sonrası; oda planı ölçeği öncesi ürün kökü | `(0, 1.69756942, .02620413)` |
| Mevcut `.88` plan ölçeği sonrası ürün kökü | `(0, 1.49386109, .02305963)` |

`StoreProductContentBuilder` ve `RoomProductInteractionBuilder`, `RinseWaterOutlet` noktasını üretip `EditorConfigureWaterOutlet` ile bağlar. Ölçek damgası bu noktaya bir kez uygulanır. Eski prefabların fallback noktası yerine ölçülmüş kaynak kullanılır. FBX içinde önceden bulunan dekoratif mavi damlalar bu geçişte modelden silinmemiştir.

Efekt zamanı `Time.deltaTime` ile ilerler; pause konumu ve opaklığı dondurur. İptal, bileşenin devre dışı kalması, sahip kedinin kaybolması ve mini oyun geçişi görünürlüğü kapatır. Oda kaldırıldığında runtime mesh ve materyal açıkça bırakılır. Köpükler durdurulan rutinden sonraki rutine taşınmaz; temiz havuz yeniden kullanılır.

Gövde köpüğü iskeletin içinde bırakılmaz. Oturma geçişi yerleştikten sonra gerçek skinned gövde bir kez ölçülür ve altı köpük merkezi kameraya bakan yüzeye alınır. Disk alanındaki komşu yüzey örnekleri de hesaba katılır; normal derinlik testi korunur. Bu ölçüm için ayrılan mesh tekrar kullanılır ve oda kaldırılınca bırakılır. Native görsel kontrol, efekti açıp kapatarak gerçek ekran piksellerinde en az dört köpük bölgesinin görülebildiğini doğrular.

## Kayıt ve işlem sınırları

Bu banyo geçişi için yeni gerçek kayıt referansı alınmıştır. [Başlangıç SHA-256 listesi](QA/BATHROOM_POLISH_2026-09-09/save-hashes-before.json) ve [çalışma alanı yedeği](QA/BATHROOM_POLISH_2026-09-09/player-save-before/) kullanılır:

- Ana kayıt ve recovery: `5E0A4E2D0E12407C2FDA1ECC20F5A5FA55B1D3CA8710D88046CED19C2B7C7E70`.
- CP2 yedeği: `03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D`.

QA ayrı kayıt oturumunda yürütüldü. Ana kayıt, recovery ve CP2 dosyalarının kapanış SHA-256 değerleri başlangıçla **birebir aynı**; [son karşılaştırma](QA/BATHROOM_POLISH_2026-09-09/save-integrity.json). Önceki raporların eski kayıtları geri yüklenmedi. [16 sunum tercihi ve anahtarların varlık durumu](QA/BATHROOM_POLISH_2026-09-09/preferences-restored.txt) tam geri yüklendi.

APK üretilmez; Android paket derlemesi, canlı yayın veya bulut uygulaması yapılmaz. Git commit/push manuel kalır. Fiziksel cihazda FPS, GPU, RAM, ısı veya pil ölçümü yapılmamıştır; editör çekimleri cihaz performansı kanıtı sayılmaz.

## Son doğrulama ve teslim

- Tam EditMode sonucu: **504/504 başarılı**, sıfır başarısız/atlanan; [son XML](QA/BATHROOM_POLISH_2026-09-09/EditMode-Complete.xml).
- Native PlayMode testlerinin son birleşik sonucu: **24 benzersiz testin tamamı başarılı**; [test adına göre son sonuçlar](QA/BATHROOM_POLISH_2026-09-09/native-test-summary.json). Bu sayı tekrar koşulan testleri ikinci kez saymaz. Duşun son üç testi ayrı tekrarda 3/3 geçti.
- Banyo matrisi: **10 ürün × 10 ırk = 100/100 gerçek rutin** başlama, normal bitiş ve açık çıkış kontrolünden geçti; [CSV](QA/BATHROOM_POLISH_2026-09-09/room-matrix/bathroom-01.csv). Bahçe nazik hareketleri ve bakım eylemi sahiplik/iptal korumaları ayrıca geçti.
- Kapsam: `BathroomArrangementProfileTests`, `BathroomGentleActivityTests`, `BathroomShowerPolishTests` ve ilgili mevcut banyo/temas regresyonları. Duş testleri gerçek durulanma/silkelenme evrelerini, pause/reduced motion dalını, iptal, disable ve oda kaldırılmasındaki kaynak temizliğini içerir.
- Son içerik doğrulayıcı sonucu: **0 hata / 0 uyarı**; [kayıt](QA/BATHROOM_POLISH_2026-09-09/validator-final.txt).
- Gerçek Unity çekimleri: **18 güncel PNG / sekiz video**. İki oda görünümü 1920×1080 ve 1440×1080; beş rutinin eylem ve dönüş kareleri; kum, paspas ve duşun üç yakın çekimi ile dönüş kareleri. Önceki oda fotoğrafı ayrıca tarihsel karşılaştırma olarak etiketlenir.
- Videolar gerçek **24 fps**, 1920×1080, **10–10.625 saniye**; hızlandırma/yavaşlatma yok. Beş normal kamera rutini ve üç ayrıntı kaydı içerir. Her kayıtta gerçek yakınlık kapısı, `TryStart`, normal tamamlama ve açık zemin denetlenir; paspasın sürekli dinlenmesi normal bitirme eylemiyle sonlandırılır.
- Yakın çekimler yalnız kayıt sırasında mevcut kameranın konum/FOV değerini geçici değiştirir; ürün saklanmaz veya taşınmaz. Kumda yükseltilen kayıt açısı ön patileri kabın arka kenarı üstünden gösterir. Kamera, viewport, çözünürlük, ihtiyaçlar ve zaman ayarları kapanışta geri yüklenir. Duşun yakın videosu durulanma/silkelenmeye odaklanır; tam giriş/çıkış geniş videosunda görünür.
- Beş geniş rutin ve üç yakın rutinin gerçek kareleri gözle incelendi. Son ayrıntı kontrolündeki 18 örnek karede kum/paspasta küçük dönüşümlü pati hareketi, duşta görünür su/köpük, silkelenmede saçılma ve çıkıştan önce tam temizlenme görüldü. Türkçe düğme **Duş al** olarak doğrulandı.
- `StoreCatalogPreviewBuilder.BuildAll()` ve `RoomPreviewCaptureBuilder.CaptureSilently()` ile kanonik ürün kartları ve sekiz oda ön izlemesi yenilendi. Son galeri ve ZIP, tüm beklenen PNG'lerin boyut/CRC bilgilerini ve sekiz MP4'ün tamamlama, kare sayısı, boyut ve SHA-256 kayıtlarını doğrular; eksik veya geçersiz medya **yok**.
- [Son editör durumu](QA/BATHROOM_POLISH_2026-09-09/editor-final-state.txt): Unity açık, QA/Play kapalı; GameScene / CatHome_UI / LivingRoom_Level01 temiz, tek etkin kamera/ses dinleyici. Gerçek JSON'u yalnız okuyan güncel editör ön izlemesi yeniden açıldı. Console hata kaydı sıfır.

Ara başarısız XML dosyaları tarihsel kanıt olarak durur; son birleşim test kimliğine göre en yeni sonucu kullanır. İlk geçişte eski “banyo paspası nazik değildir” beklentisi güncellendi. Duşun piksel kanıtından sonraki hareket ölçümü, PNG işleminin duvar saatine bağlı olmaması için sabit test zamanı ve PNG'den önce yapılan evre/ilerleme ölçümüyle doğrulandı. Son genel testten sonra yalnız yakın çekim yardımcısının kum kamerası yükseltildi; bu son kayıt gerçek rutinin tamamlanması ve görünür pati kareleriyle ayrıca doğrulandı.
