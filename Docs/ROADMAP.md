# Cat Home Roadmap

**Yeni sohbet için güncel checkpoint:** [7 Eylül 2026 devir belgesi](CatHome_Checkpoint_2026-09-07.md). Son tamamlanan iş görünmeyen bakım eylemlerinin düzeltilmesidir; sıradaki geliştirme kullanıcı tarafından yeni sohbette belirlenecek.

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

Son güncelleme: 7 Eylül 2026

### Son çalışma — görünmeyen bakım eylemleri (7 Eylül 2026)

Banyoda eşya yokken çıkan “Mama ye”, salon dışındaki odalarda bırakılmış boş bakım noktalarından kaynaklanıyordu. Mama/su/yatak artık aynı odadaki görünür geometriyi, yakın ve engelsiz girişi denetler; eski düğmeye basmak uzaktan etkileşim başlatmaz. Sekiz oda / 24 nokta, gerçek bakım rutinleri ve kayıtlı uyku doğrulandı. 459/459 EditMode, 5/5 native PlayMode, validator 0/0; [uygulama raporu ve HD kanıt](CARE_PROMPTS_2026-09-07.md).

### Önceki çalışma — Runner Boulevard, coin ve top teması (7 Eylül 2026)

Runner, 25 yeni/yeniden tasarlanan Blender modeliyle taş döşeli, tenteli dükkânlar ve kedi köşeleri olan bir sokağa dönüştürüldü. Sekiz yer engeli, iki tente ve ahşap parkur yenilendi. Ortak coin kabartmalı altın model ve 1536 px ikonla yeniden tasarlandı. Coin doğma/mıknatıs hareketi gerçek eşya hacmini gözetir; top alçak oyuncaklardan ve mobilyadan geçmez. On ırkın son animasyon klibinde yoğun ara poz temas düzeltmesi kullanılır. Güncel doğrulama ve HD kanıt [uygulama raporundadır](RUNNER_BOULEVARD_2026-09-07.md).

### Önceki çalışma — Cat Runner ve Cat Catch yenilemesi (7 Eylül 2026)

İki mini oyunun görselleri 15 headless Blender modeli ve 2048 px yüzey dokularıyla yenilendi. On ırka ayrı zemin düzeltmesi olan gerçek iskelet zıplama/eğilme/atılma klipleri, fizik evresine bağlı animasyon, daha sakin kameralar ve gerçek ön pati teması kullanılır. Runner engelleri zeminle aynı hızda ilerler; eğilme açıklığı görünen geometriyle eşleşir. Catch fareleri ayrı ayaklar, yakınlık ayrışması ve seçili hedef halkası kullanır. Oyun seçimi, karşılama, HUD, duraklatma ve sonuçlar evin ortak premium arayüzüne uyarlandı; menü fotoğrafları gerçek HD Play çekimleriyle yenilendi.

Tam EditMode **457/457**, native PlayMode **6/6**, LevelContentValidator **0 hata / 0 uyarı**. İki oyunun normal hızda 18 saniyelik HD kayıtları ve geniş/dar ekran kanıtları [galeride](QA/MINIGAMES_2026-09-07/index.html); üretim ve davranış sözleşmesi [uygulama raporunda](MINIGAME_REDESIGN_2026-09-07.md). Gerçek mobil cihaz performansı ayrıca ölçülecek.

### Önceki çalışma — diğer odalarda kalite ve etkileşim (7 Eylül 2026)

Yedi odanın kapı, duvar çerçevesi, mimari tonları, bitkileri ve yumuşak ışığı salonla uyumlu hale getirildi. 70 ürünün ölçüleri korundu; yalnız üst kattaki pikap/kitap yığını kadraj için yeniden yerleşti. Kilitli rutin sırasında çalışmayan baş takibi, tek vuruş sonrası bekleyen pati tepkileri, pikabın yanlış dönme ekseni ve dolu ihtiyaçların bazı eşyaları kullanılamaz kılması düzeltildi; sıfır enerjiyle dinlenme ve poz sırasında enerji artışı doğrulandı. [Uygulama raporu](ROOM_POLISH_INTERACTIVITY_2026-09-07.md) ve [gerçek HD görüntü/hareket galerisi](QA/ROOM_POLISH_2026-09-07/index.html) bu geçişin kanıt kaynağıdır.

### Önceki çalışma — yedi odada ortak eşya düzeni (7 Eylül 2026)

Banyo, mutfak, yatak odası, bahçe, balkon, avlu ve üst katın 70 ROOM ürünü; satın alma sırasından bağımsız, tam koleksiyonun alanları önceden ayrılarak düzenlendi. CAT yalnız salondadır. Ölçek, collider, hareketli parçalar ve temaslar birlikte güncellendi. Kapı/pencere, açık orta geçiş, ayrı eylem girişleri ve kamera görünürlüğü çözümün parçasıdır. Yeni oda koleksiyonları `BuildRoomSceneProducts` üzerinden aynı planlayıcıya girer; sığmayan koleksiyon sessizce üst üste konulmaz.

Uygulama ve sonuçların tek kaynağı: [ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Tam EditMode 450/450, PlayMode 14/14, ürün/ırk matrisi 800/800 geçti. Son duş düzeltmesi sonrası iki ilgili test yeniden geçti. Sekiz odanın 16 geniş/dar canlı karesi ve iki modal taraması temiz; LevelContentValidator 0 hata / 0 uyarı. [HD doğrulama galerisi](QA/ROOM_LAYOUT_2026-09-07/index.html) gerçek oda ve etkileşim karelerini içerir. Projeye ait bütün Markdown belgeleri güncel sözleşmeye bağlandı.

### Son çalışma — bütün odalarda ortak salon kamerası (7 Eylül 2026)

Sekiz oda salonun önden/ortalanmış bakışına geçti. Yeni odalar üreticide, marker açılışında ve normal oda geçişinde ortak `HomeRoomCameraProfile` ayarını alır. Alt gezinme oda görüntüsünden ayrıdır; dar ekran kadrajı aynı kuralla korunur. Balkon ön korkuluğu ve üst kat tavanı kesit görünümü kullanır. Sekiz oda/HOME mağaza fotoğrafı yenilendi. Tam EditMode 443/443, iki kamera PlayMode testi geçti; gerçek geniş/dar ekran kanıtları `Docs/QA/SHARED_ROOM_CAMERA_2026-09-07`, uygulama `Docs/SHARED_ROOM_CAMERA_2026-09-07.md`.

Genel kontrolde bulunan Türkçe atlas temizlenmesi de düzeltildi: derleme mevcut atlasları boşaltmaz; font üreticisi var olan dosyaların karakterlerini de tamamlar.

### Önceki çalışma — onaylı önden kamera, oranlar ve gerçek temas (7 Eylül 2026)

Sol medya / sağ oturma düzeni, açık tablo ve modern bakım köşesi onaylı referansa göre uygulandı. Tünel kısaltıldı ve iki yönden çalışıyor. TV kendi kedilerimizin HD animasyonunu oynatıyor. Atlayış, iki pati tırmalama, gerçek mesafeyle yürüyüş temposu ve yatak temasları yenilendi. Alt gezinme oda görüntüsünün dışında 80 px şerit; ilerleme etiketi sağ eylem alanında. Dar ekran kadrajı ve modal önceliği ayrıca düzeltildi.

800 ROOM/ırk,170 CAT/ırk ve20 koltuk/sehpa rutini;103 mağaza ve8 oda fotoğrafı. Tam doğrulama kaynakları, normal hızda HD hareket kayıtları ve gerçek ekranlar: `Docs/REFERENCE_LIVING_IMPLEMENTATION_2026-09-07.md`, `Docs/QA/REFERENCE_LIVING_2026-09-06/index.html`.

### Önceki çalışma — otomatik salon düzeni, mobilya oyunu ve kompakt HUD (6 Eylül 2026)

CAT eşyaları artık kullanıcı sürüklemesi olmadan otomatik yerleşir; eski konumlar yenilenir. Beş eşya / bir yatak ve koleksiyondan ekle/kaldır korunur. 4.147 beşli koleksiyonun tamamında gelecekteki mobilya alanı, bakım ve etkileşim yolları korunur. Berjer küçüldü; mama/su sağ bakım alanına taşındı. Top üç gerçek pati teması ve takip hareketi kullanır. İkili koltuğa ve sehpaya zıplama, koltukta dinlenme, sehpadaki parçayı itip düşürme eklendi.

Kedi kartı küçültüldü, Bond üst bara alındı. Yedi Blender simgesi yeniden ortalandı, dock ve açılış kısayollarının tematik fonları yenilendi. 103 mağaza fotoğrafı ve sekiz oda ön izlemesi güncellendi. Uygulama ve doğrulama kaydı: `Docs/LIVING_ROOM_AUTOMATIC_LAYOUT_2026-09-06.md`; gerçek HD galeri: `Docs/QA/LIVING_2026-09-06_Automatic/index.html`.

Son tam EditMode 436/436; native sonuçlar birlikte 8 farklı geçen test ve 190 ürün/ırk rutini. 37 oyun karesi; 1920×1080 / 1440×1080 HUD taramasında çakışma veya taşma yok. Son validator 0 hata / 0 uyarı, normal üç sahne / bir kamera düzeni geri açıldı.

### Önceki çalışma — CAT yerleşim ve etkileşim düzeltmesi (6 Eylül 2026)

Kullanıcı aynı anda **5 CAT ürünü / en fazla 1 yatak** sınırını seçti. Koleksiyon sınırsız, mağazada ekle/kaldır akışı var. Store v8 eski kayıtlardaki fazlalıkları sahipliği silmeden koleksiyona alır. Sabit ROOM ürünlerinin gelecekteki zemin alanı ve CAT yaklaşma/tünel çıkış alanları korunur. Dört hacimli ürün küçültüldü; gerçek prefab ön izlemeleri yenilendi. Oyuncağa özgü iskelet hareketleri ve temasla başlayan fiziksel tepki kullanılır. Yataklarda maliyet 0, yalnız dinlenirken +0.75 enerji/sn.

Son EditMode 431/431; iki son native sonuç dosyası birlikte 6 farklı geçen PlayMode testi ve 170 ürün/ırk rutini içerir. Uygulama, HD galeri ve doğrulama sınırları: `Docs/CAT_COLLECTION_REFINEMENT_2026-09-06.md`. Önceki rapor aşağıda tarihsel olarak durur.

### Önceki çalışma — premium kedi oyun eşyaları (6 Eylül 2026)

CAT koleksiyonunun 17 ürünü headless Blender ile yeniden tasarlandı; 15 ürüne ölçülmüş giriş/temas ve hareketli oyuncak rutinleri verildi, top sepeti ile tırmalama direğinin gerçek oyunları yenilendi. Eski tasma/kayış/çıngıraklı tasma kimlikleri top pisti/kurdele minderi/çıngıraklı teker olarak korunur. Fiyatlar ve sahiplik kaydı değişmez. Bilinçli CAT varsayılan yerleşimleri eski hash konumlarını kaldırır; oyuncunun kaydettiği CAT yerleşimi/depolaması korunur. ROOM sabit yerleşim akışı aynı kalır.

1024×1024 mağaza fotoğrafları gerçek son prefablardan yeniden çekildi. Kedi ile ürün temasları gerçek 1920×1080 oyun görüntülerinde incelendi. Tünelin son yüksekliği 0.60; alçak kuyruk ve içeride ayakta pati savurmadan geçiş kullanılır. Maine Coon dahil on ırkın gövde/kuyruk noktaları kemerin içinde kalır. Depodaki CAT eşyası görünmez etkileşim sunmaz.

Son tam EditMode **425/425**. Native PlayMode'da 17 ürün × 10 ırk **170/170** rutin, iptal/sahiplik/depolama ve ayrı son tünel temas testi başarılı; **4 farklı test** ayrı sonuç dosyalarıyla kapandı. Önceki ROOM matrisi bu turun sonucu olarak sayılmaz. Kalıcı rapor `Docs/PREMIUM_CAT_PRODUCTS_2026-09-06.md`, gerçek fotoğraf galerisi `Docs/QA/CAT_2026-09-06/index.html`. Git commit/push kullanıcıya aittir.

## Ürün yönü

Cat Home yalnızca bir sanal kedi bakım oyunu değildir. Bakım, kısa süreli arcade oynanış ve ev geliştirme tek bir tekrar oynanabilir döngü oluşturur.

Ana döngü:

> Kediye bak → Cat Runner oyna → Coin kazan → Evi geliştir → Home XP kazan → Yeni alan ve içerik aç → Tekrar oyna

İlk planlanan mini oyun Cat Runner'dır. MVP kapsamı sonradan ikinci bir mini oyunla (Cat Catch) genişletildi; ikisi de Games hub üzerinden açılır. Yeni mini oyunlar bundan sonra ayrıca değerlendirilecektir.

## Kalıcı progression kararları

| Kaynak | Rolü | MVP durumu |
| --- | --- | --- |
| Coin | Cat Runner'dan kazanılır; mobilya, oyuncak ve dekorasyon için harcanır. | Aktif |
| Bond | Kediyle ilişkinin gücünü temsil eder; etkileşimler, davranışlar ve bakım bonusları açar. | Aktif |
| Home XP | Ev geliştirmelerinden kazanılır; Home Level ve yeni oda/alan açılımlarını belirler. Harcanmaz. | Aktif |
| Diamond | Nadir/premium kaynak. Runner'ın normal ödülü değildir. 7. giriş günü, Home Level 5 ve Bond 250 achievement'larından 1 gelir; IAP paketleri yalnız platform doğrulaması sonrası ödül verir. | Aktif (nadir + IAP seam) |
| Runner Enerji | Koşuya girişte harcanır; en fazla 5'tir ve 10 dakikada 1 yenilenir. | Aktif |
| Player Level | Ayrı bir progression kaynağı değildir. Save alanı yalnız quest chapter migration içindir. | Emekli |

### Player Level emeklilik kuralı

- Yeni özellikler Player Level'a bağlanmayacaktır.
- Mevcut görev grupları oyuncu seviyesi yerine görev bölümü/chapter olarak ele alınacaktır.
- Ev içi etkileşimler Bond, satın alma veya Home Level koşulu kullanacaktır.
- Oda açılımları yalnızca Home Level tarafından yönetilecektir.
- Eski kayıtları bozmamak için `playerLevel` kayıt alanı geçici olarak legacy/migration alanı şeklinde tutulabilir; gameplay tarafından okunmayacaktır.
- Geçiş tamamlandıktan ve eski kayıt testi yazıldıktan sonra alan tamamen kaldırılabilir.

## Sistemlerin oyundaki görevleri

- Bakım: Kedinin ihtiyaçlarını ve mood durumunu iyileştirir; Runner kazancına pozitif bonus verir.
- Ev içi aktiviteler: Kediyle kısa etkileşim sağlar ve Bond progression üretir.
- Cat Runner: Coin'in ana ve tekrarlanabilir kaynağıdır.
- Ev geliştirme: Coin'in ana harcama alanıdır ve Home XP üretir.
- Home Level: Yeni oda, katalog ve büyük içerik açılımlarını yönetir.

Kedi kötü durumdayken Runner kilitlenmez. İyi bakım cezayı kaldırmak yerine ödül kazandırır.

## Mevcut durum

- [x] Bootstrap, ortak UI ve level sahneleri ayrıldı.
- [x] Sahne referans bağlama ve mimari doğrulama kuruldu.
- [x] Bakım, tutorial, görev ve kayıt sistemlerinin temel akışı çalışıyor.
- [x] Coin/Diamond cüzdan altyapısı ve güvenli ödül işlemleri mevcut.
- [x] Top, tırmalama ve fare ev içi aktiviteleri eklendi.
- [x] Kilitli ev içeriği açılana kadar tamamen gizleniyor.
- [x] EditMode ve PlayMode regresyon testleri mevcut.
- [x] Player Level gameplay bağımlılıkları kaldırıldı; eski kayıt alanı yalnızca migration için korunuyor.
- [x] Cat Runner'ın ilk oynanabilir dikey kesiti üretildi ve ana eve bağlandı.
- [x] Runner Enerji sistemi eklendi: 5 maksimum, 10 dakika yenilenme ve çevrimdışı dolum.
- [x] Koşu başına üç çarpışma hakkı ve üçüncü çarpışmada Run Over akışı eklendi.
- [x] Ödüllü reklam için +2 Enerji ve yedi günlük sınırsız Enerji entegrasyon noktaları hazırlandı.
- [x] Home Store eklendi; top sepeti (300 Coin) ve tırmalama tahtası (400 Coin) satın alındığında odada kalıcı olarak açılıyor.
- [x] ROOM mobilyaları satın alındığında tasarlanmış yerine otomatik gelir; eski özel konumlar/depolama Store v7 ile kaldırılır. CAT ürünlerinin eski yerleştirme sistemi korunur. Etkileşim/temas/çarpışma doğrulaması tamamlandı: 80 ürün × 10 ırk, 800/800 kombinasyon; ayrıntı `FIXED_ROOM_INTERACTION_AUDIT.md`.
- [x] Top aktivitesi sevme animasyonu/kalplerden ayrıldı; top sekmesi ve hedefe bakış iyileştirildi; mevcut kedi iskeletine özel pati vurma, pounce ve döngülü tırmalama animasyonları eklendi.
- [x] Home Store beş ziyaret edilebilir alana genişledi: Living Room, Bathroom, Kitchen, Bedroom, Garden (her biri 10 parçalık ROOM koleksiyonu) ve `RoomSelectorPanel` oda navigasyonu.
- [x] İkinci mini oyun Cat Catch ve iki oyunu barındıran Games hub eklendi.
- [x] IAP elmas paket kataloğu (10/20/50/100/500/1000) ve doğrulanmış satın alma seam'i hazırlandı; gerçek tahsilat yalnız platform doğrulaması sonrası ödül verir.
- [x] Living Room penceresi + gün/gece `WindowSystem`; premium pastel duvar/ışık geçişi.
- [x] Home XP ve Home Level eklendi: Home XP ev geliştirmelerinden (Home Store satın alımı) kazanılır, Home Level XP'den türetilir. Save şema **v11** (Home XP + daily + achievement). Oda ve ürün açma koşulları Home Level ile yönetiliyor.
- [x] HOME LV. rozeti HUD + SHOP'ta; level-up kutlaması (blur, 500 coin, reklam ×2).
- [x] Cat Runner 9 scenery silüeti + anti-tekrar; Garden avlusu oda açılınca havuza girer.
- [x] CAT kataloğu 5 yeni yerleştirilebilir ürünle genişledi.
- [x] Garden açık hava avlusu: çim, çit, açık kapı, uzak yol, ağaç, 2 ziyaretçi kuş, kenar çiçek + arı/kelebek; Home Level 5 / 6000 Coin.
- [x] Bond milestone oyunları: Window Watch (80), Feather Play (150), Bird Watch (250); fare 35 Bond.
- [x] Görevler 5 chapter: bakım → ev oyunu → Home Loop (Runner/Store/Tunnel) → Garden Bond.
- [x] Aşama 5 çekirdeği: günlük giriş, 3 günlük görev, achievement + nadir Diamond, koşu x2 reklam seam (save v11).
- [x] Garden polish (19 Ağustos): pergola kısaldı + post-only collider (altına ürün konur); doğal arka plan (katmanlı tepeler, uzak ağaç sırası, kıvrımlı patika, gölet, çayır çiçekleri); iki kendinden sürüşlü chase oyunu — PLAY YARN (hep açık) + PLAY BALL (Sunny Yarn Ball ürününe kilitli).
- [x] Yayın kalitesi paketi (19 Ağustos): home audio, kedi isim+kürk (`CAT JOURNAL`), `SETTINGS` paneli, title overlay. Save v11 değişmedi (PlayerPrefs / self-bootstrap).
- [x] Kedi idle kişiliği (19 Ağustos): boşta tımar/bakış/esneme/pounce; Bond+needs tonu; uzun boşlukta mırıltı + speech bubble. Hareket kilidi yok.
- [x] Aşama #4 yaşam katmanı (19 Ağustos): koleksiyon tamamlama ödülü (COLLECT-only) + `X OF 84 • COLLECTED`; local notification seam; SFX cilası; title → isim → tur + `SKIP TOUR`.
- [x] Premium title v2 (19 Ağustos): iki sütunlu kart, canlı 3D kedi, HOME LV / jeton / koleksiyon hapları, PLAY/CONTINUE, SETTINGS, CREDITS, masaüstü QUIT.
- [x] Coming Soon oda zinciri oynanabilir hale geldi (20 Ağustos): Balcony LV9, Garden Patio LV10, Second Floor LV12; katalog **84 → 114**, save v11 sabit.
- [x] Dalga 3 model sanatı tamamlandı: sekiz odanın **80 ürünü** `PREMIUM_FURNITURE_LANGUAGE` dilinde. 5 Eylül denetiminde Living Room'da bulunan eksik 10 aktivite eklendi; `CatActivityKind` 86, `QuestType` 33. Güncel **80 × 10 ırk** doğrulaması **800/800**, toplam PlayMode **53/53**, EditMode **391/391** geçti.
- [x] Sabit ROOM düzeni (5 Eylül): alım anında tasarlanan yere ekleme; eski taşınmış/depolanmış mobilyaları aynı düzene göç ettirme; taşıma/açı/depolama kaldırıldı. Gerçek model collider'ları, açık pergola/şemsiye altı, ölçülmüş temas pozları, güvenli yaklaşma/çıkış ve ırk ölçeğine uygun animasyonlar tamamlandı. Store v7, ana save v11; FREE TEST korunur. Ayrıntı: `Docs/FIXED_ROOM_INTERACTION_AUDIT.md`.
- [x] Rooms QA düzeltmesi (20 Ağustos): oda geçişinden sonra `X`/scrim yeniden etkinleşir; Second Floor gerçek 1280×720 preview kullanır; Fredoka Ellipsis/`✦` Console uyarıları temizlendi.
- [x] Yerel kayıt kurtarma temeli (21 Ağustos): her başarılı save yanında kalıcı
  `.recovery` kopyası; eksik/bozuk ana dosyada otomatik dönüş; daha yeni save
  şemasını eski kopyayla sessizce ezmeme; 4 EditMode testi.
- [x] Onaylı `NEW GAME` + Türkçe/İngilizce yerel dilim (21 Ağustos): ilerleme
  sıfırlanırken Diamonds, satın alma işlem kimlikleri, sınırsız geçiş hakları,
  ses/erişilebilirlik tercihleri ve dil korunur; Main Menu/Settings/onay penceresi
  canlı dil değiştirir.

## Altı aylık geliştirme sırası

### Aşama 0 — Progression sadeleştirme ve Runner sözleşmesi

Hedef: Cat Runner geliştirilmeden önce çakışan progression kurallarını temizlemek.

- Player Level'ı görev, sahne ve aktivite kilitlerinden çıkarmak.
- Mevcut görev seviyelerini `Quest Chapter` mantığına dönüştürmek.
- Living Room'u başlangıçta doğrudan yüklenen ana ev sahnesi yapmak.
- Top sepeti ve tırmalama tahtasını Coin ile satın alınan içerik, fareyi Bond tabanlı içerik yapmak.
- Eski kayıtlar için migration ve geriye uyumluluk testi eklemek.
- Runner sonucu için veri sözleşmesini tanımlamak: süre, mesafe, toplanan coin, çarpışma, bakım bonusu ve toplam ödül.
- Runner ödülünün cüzdana yalnızca bir kez aktarılmasını güvenceye almak.

Tamamlanma ölçütü: Oyunda veya yeni kodda Player Level'a bağlı hiçbir görünür karar kalmaması; eski kayıtların açılabilmesi; mevcut gameplay testlerinin geçmesi.

### Aşama 1 — Cat Runner oynanabilir prototip

Hedef: Runner'ın eğlenceli olup olmadığını en küçük kapsamla kanıtlamak.

- Ana ev ekranında doğrudan Cat Runner'a götüren `PLAY` butonu.
- Ayrı Cat Runner sahnesi.
- 60 saniyelik koşu.
- Arkadan kamera ve otomatik ileri hareket.
- Üç şeritli sağ/sol hareket.
- Zıplama.
- Mobil swipe ve editörde klavye kontrolü.
- Temel koşu ve sendeleme tepkisi.
- Dört okunaklı ev temalı engel: oyuncak blokları, yastık, mama kabı ve kutu.
- Basit coin dizilimleri ve coin toplama geri bildirimi.
- Engeller ve coinler, görsel zemin akışından daha yüksek kapanma hızıyla oyuncuya yaklaşır.
- İlk iki çarpışmada kısa sendeleme/yavaşlama; üçüncü çarpışmada Run Over.
- Çarpışmalar arasında 1,5 saniye dokunulmazlık.
- Koşuya girişte harcanan 5 kapasiteli Runner Enerjisi.
- Run başlangıcı, süre sonu ve tekrar oynama.

Bu aşamada yapılmayacaklar: kayma hareketi, power-up, reklam, bütün odalar, procedural karmaşıklık, premium ekonomi.

Tamamlanma ölçütü: Oyuncunun 60 saniyelik koşuyu bitirip hemen yeniden denemek istemesi; kontrolün mobilde okunaklı ve tepkisel olması.

### Aşama 2 — Ana döngü bağlantısı

Hedef: Runner'ı Cat Home'dan kopuk bir mini oyun olmaktan çıkarmak.

- Koşu sonunda Distance, Coins, Happy Cat Bonus ve Total sonucu.
- Kazanılan coinlerin mevcut cüzdana güvenli aktarımı.
- Eve dönüş akışı.
- İhtiyaç ve mood durumundan basit pozitif Runner bonusu.
- Coin ile alınabilen ilk iki etkileşimli ev ürünü.
- Satın alma sonrası odada kalıcı görsel ve oynanış değişimi.
- Ses, coin parçacığı, hafif kamera hareketi ve hız hissi.

Tamamlanma ölçütü: Bakım → Runner → ödül → evde görsel gelişme döngüsünün baştan sona çalışması.

### Aşama 3 — Runner içeriği ve Home progression

Hedef: Kanıtlanmış ana döngüye derinlik eklemek.

- Coin Magnet, Shield ve x2 Coin power-up'ları.
- Kayma hareketi ve ona uygun engeller.
- Segment tabanlı salon/koridor parkuru ve varyasyonlar.
- Home XP ve Home Level sistemi.
- Mobilya/dekorasyon kataloğu.
- Bir odanın aşamalı görsel dönüşümü.
- Yeni oda açılış kutlaması ve kamera sunumu.

### Aşama 4 — Yeni alanlar ve içerik ölçekleme

Hedef: İçeriği aynı sistemlerle büyütmek.

- [x] İlk yeni oda veya alan (Garden açık avlu).
- [x] Açılan odanın Runner segment havuzuna eklenmesi (`Variant_GardenCourtyard`, Garden kilidi).
- [x] Bond milestone içerikleri (35 fare, 80 pencere, 150 tüy, 250 kuş).
- [x] Daha fazla ev geliştirmesi ve kedi etkileşimi (Window Watch, Feather Play, Bird Watch).
- [x] Görevlerin Coin, Bond ve Home XP döngüsüne göre yeniden dengelenmesi (Home Loop + Garden Bond).

### Aşama 5 — Retention, monetization hazırlığı ve yayın kalitesi

Hedef: Ana döngü kanıtlandıktan sonra uzun süreli kullanım ve yayın hazırlığı.

- [x] Günlük görev ve giriş ödülü (`DailyRetentionService`, save v11).
- [x] Achievement ve milestone ödülleri (`AchievementService`).
- [x] Nadir Diamond kaynakları (7. giriş günü, Home Level 5, Bond 250).
- [x] Koşu sonunda ödüllü reklam ile x2 Coin seam (`GrantDouble`, ayrı transaction id).
- [x] Yayın kalitesi 1–3 (19 Ağustos): ses/ambiyans, kedi kişiselleştirme, ayarlar + başlık ekranı.
- [x] Kedi idle kişiliği (`CatIdleBehavior`, hareket kilidi yok).
- [x] Yaşam katmanı (19 Ağustos): koleksiyon ödülü + bildirim seam + SFX + onboarding skip; premium title v2.
- Performans, mobil cihaz ve uzun oturum testleri. Yerel kayıt kurtarma tamamlandı.
- İçerik dengesi, onboarding güncellemesi ve genel polish.
- [x] Coming Soon odalar: Balkon (Home Level 9), Garden Patio (10), Second Floor (12) — üçü de oynanabilir.

### Aşama 6 — Hesap, bulut kayıt ve rekabet

Hedef: çevrimdışı ana döngüyü hesap duvarına çevirmeden cihazlar arası güvenli kayıt,
Google hesabı bağlantısı ve adil mini oyun rekabeti eklemek.

- [x] Kalıcı yerel kurtarma kopyası ve bozuk/eksik save testleri.
- [x] Onaylı `NEW GAME`; Diamonds, satın alma işlem kimlikleri, ayarlar ve
  doğrulanmış sınırsız geçiş haklarını koruyan sıfırlama sınırı.
- [x] Türkçe + İngilizce dil çekirdeği ve Ayarlar seçimi; Main Menu, Settings ve
  `NEW GAME` yüzeyleri canlı çevrilir.
- [ ] **Yayın öncesi metin dondurma + tam yerelleştirme paketi** — oyun içeriği,
  ekonomi adları, görevler ve bütün oyuncu metinleri kesinleştikten sonra yapılır;
  geliştirme sırasında parça parça çeviriyle zaman kaybedilmez.
  - [ ] Kalan gameplay/HUD/SHOP/Rooms/Runner/Catch/onboarding metinlerini dil
    tablolarına taşı; kod içinde oyuncuya görünen sabit metin bırakma.
  - [ ] Öncelikli Asya paketi: Japonca (`ja-JP`), Korece (`ko-KR`) ve Geleneksel
    Çince (`zh-Hant`, Tayvan/Hong Kong).
  - [ ] Büyüme paketi: Brezilya Portekizcesi (`pt-BR`) ve Latin Amerika
    İspanyolcası (`es-419`).
  - [ ] Oyuncu/pazar verisine göre ikinci dalga: Almanca ve Fransızca; ardından
    gerekirse Tayca, Endonezce ve Vietnamca.
  - [ ] CJK font/fallback atlasları, satır kırma, auto-size, çoğul, sayı/para/tarih
    biçimleri ve her desteklenen dilde 1920×1080 + mobil taşma/çakışma QA.
  - [ ] Her dil için oyun içi metinlerle birlikte Google Play/App Store başlık,
    kısa açıklama, açıklama ve ekran görüntüsü metinlerini yerelleştir.
  - [ ] Basitleştirilmiş Çince/Çin ana karası yayınını yalnız yerel dağıtım,
    işletmeci ve oyun onayı planı ayrıca kabul edilirse ayrı proje olarak ele al.
- [x] Hesap çekirdeği: ilk PLAY / onaylı NEW GAME sonrasında TR/EN hesap seçimi,
  anında yerel misafir kimliği, UGS anonim oturum, Unity Player Accounts üzerinden
  Google bağlantı akışı ve Settings hesap durumu. Save v11 dışında saklanır.
- [x] Unity Dashboard'da Unity Player Accounts sağlayıcısını, PC + Android/iOS
  platformlarını ve istemci kimliğini tanımla; gerçek Google girişi ile Editor
  localhost dönüşünü doğrula.
- [ ] Android gerçek cihazda Player Accounts deep-link dönüşünü doğrula. Development
  ARM64 APK üretildi; paket kimliği ve `unitydl://com.unityplayeraccounts.<projectId>`
  intent-filter'ı APK manifestinden doğrulandı. Son adım bağlı telefonda tarayıcı →
  uygulama dönüşüdür.
- [x] Cat Home Player Care sitesini herkese açık yayımla:
  `https://cathome-player-care.hhknshnn.chatgpt.site` (`/privacy`,
  `/delete-account`, `/data`).
- [ ] Yayına yaklaşınca Player Care içeriğini `cathome.vexorialabs.com` alt alan
  adına taşı; HTTPS/DNS doğrulamasından sonra oyun, Unity Dashboard ve mağaza
  formlarındaki bütün geçici Player Care URL'lerini yeni kanonik adrese çevir.
- [ ] Player Care gizlilik, hesap silme ve veri talebi URL'lerini Unity
  Dashboard/mağaza formlarına bağla. Google hesap adı/e-posta paylaşımının son
  izin ekranı kullanıcı onayında bekliyor.
- [x] Cloud Save Player Files + kesintisiz `CONTINUE`: tam v11 JSON, SHA-256 yerel
  baz, Cloud Save write lock, en yeni geçerli kaydı otomatik uygulama, kaybeden kopyayı
  yerel destek yedeğinde koruma ve hesap+bulut veri silme akışı. Oyuncuya cihaz/bulut
  seçim ekranı gösterilmez.
- [x] Oyun içi açılır menüde yerelleştirilmiş `RETURN TO MAIN MENU / ANA MENÜYE DÖN`:
  mevcut yolculuğu kaydeder, açılır listeyi kapatır ve sahne yüklemeden Main Menu v4'ü
  yeniden açar.
- [x] Runner ve Catch için DAILY / WEEKLY / ALL-TIME istemci tabloları ve premium
  Games Hub yüzeyi; çevrimdışında son güvenli listeyi gösterir.
- [x] Cloud Code içinde kanonik skor hesabı ve tekrar kullanılan run/hunt kimliği koruması.
- [x] Arşivlenmiş dönem sonuçlarından idempotent günlük/haftalık ödül sözleşmesi.
- [x] E-posta/gerçek ad göstermeyen sıralama kimliği: onboarding'de seçilen kanonik
  kedi adı otomatik kullanılır; ayrı takma ad veya `SAVE NAME` adımı yoktur.
- [x] Altı Leaderboards tanımını, `CatHomeCompetition` Cloud Code modülünü ve
  doğrudan istemci skor yazımını engelleyen access-control politikasını Unity Cloud
  production ortamına dağıt (24 Ağustos 2026).

Ayrıntılı sözleşme: `Docs/ONLINE_COMPETITION_PLAN.md`.

## İlk dikey kesitin kesin tasarımı

- Süre: 60 saniye.
- Kontroller: sağ şerit, sol şerit ve zıplama.
- Tema: sıcak Cat Home salonu ve kısa koridor varyasyonları.
- Ödül: Coin.
- Bakım bağlantısı: Happy Cat Bonus.
- Çarpışma: ilk iki hatada sendeleme ve yavaşlama, üçüncü hatada koşunun bitmesi.
- Sonuç: Run Complete ekranı ve güvenli Coin aktarımı.
- İlk ev harcamaları: 300 Coin top sepeti ve 400 Coin tırmalama tahtası; satın alınana kadar tamamen gizli kalırlar.

## Kapsam koruma kuralları

- Cat Runner eğlenceli bulunmadan kapsamlı ev ekonomisi kurulmayacak.
- İkinci mini oyun Cat Catch olarak eklendi; üçüncü bir mini oyun Coming Soon ile sergilenmez.
- Diamond, reklam ve IAP ana döngü kanıtlandıktan sonra seam olarak durur; doğrulanmamış reklam/IAP cüzdana yazmaz.
- İlk prototip için bütün oda listesi modellenmeyecek.
- Yeni sistemler mevcut save, bakım ve ev içi gameplay'i bozmayacak; her aşamada EditMode, PlayMode ve mimari doğrulama çalıştırılacak.

## Sıradaki çalışma paketi

> **24 Ağustos 2026 ürün kararı:** Player Care URL'lerini Unity Dashboard/mağaza
> formlarına bağlama, kanonik alana taşıma, Android gerçek cihaz deep-link testi
> ve tam yerelleştirme aktif geliştirme işi değildir. Bunların tamamı oyun içeriği
> bittikten sonra, yayına geçmeden hemen önce yürütülecek yayın kapısıdır.

1. ~~İlk Home XP kazanımı ve Home Level sözleşmesi.~~ ✅ Tamam (17 Ağustos 2026): `HomeProgressionService`, save v10, Home Store satın alımından XP.
2. ~~Home Level'ı arayüzde göstermek (HUD/mağaza rozeti)~~ ✅ Tamam (17 Ağustos 2026): `CurrencyHudController` ve `ShopPanelController` tarafından `HomeProgressionService.HomeLevel` ile canlı yenileme.
3. ~~Home Level'ın içerik açılımlarını yönetmesi (Aşama 4): oda/ürün kilitlerini `ProgressionService` quest chapter yerine Home Level'a bağlamak~~ ✅ Tamam (17 Ağustos 2026)
   - Home XP / Home Level service: ✅ Tamam
   - Home Level backend oda/ürün kilitleme: ✅ Tamam
   - Oda Home Level eşikleri uygulanmış: Living=1, Bathroom=2, Kitchen=3, Bedroom=4, Garden=5 ✅ Tamam
   - Home Level görünür HUD/mağaza rozeti: ✅ Tamam (17 Ağustos 2026)
   - Unity doğrulama: EditMode **230/230** + `LevelContentValidator` 0/0 (18 Ağustos 2026). PlayMode bu turda çalıştırılmadı.
4. ~~Runner parkur segmentlerinin görsel çeşitlendirilmesi.~~ ✅ Tamam (17 Ağustos 2026)
   - 9 silüet sahnede: Pet Shop, Toy Corner, Cozy Market, Cat Cafe, Window Garden, Toy Parade, Cozy Reading, Paw Park, Garden Courtyard.
   - Anti-tekrar son iki varyantı dışlar; koşu reset'i history'yi uygulamadan önce sıfırlar, böylece ilk geri dönüşen ufuk parçası son görünenle çakışmaz.
   - `LevelContentValidator` ve `CatRunnerSceneryVariantTests` 9 isimli varyantı kilitler; Garden kilitliyken 8’li havuz kullanılır.
5. ~~Yeni mağaza ürünleri için veri odaklı katalog genişletmesi.~~ ✅ Tamam (17 Ağustos 2026)
   - CAT sekmesine 5 yeni yerleştirilebilir ürün: Bell Collar, Cat Food Tin, Nap Pillow, Cat Grass Pot, Cardboard Hideout.
   - Ekonomi `HomeStoreService` satırı + sanat/placement `StoreCatalogAssets` satırı; oda 10'luk koleksiyonları ve sofa/sehpa/halı yasağı değişmedi.
   - Prefab, ikon ve SHOP kartları üretildi. Katalog 69 → 74, sonra Garden ile **84**. `HomeStoreServiceTests` eşlemeyi kilitler.
6. ~~Aşama 4 ilk yeni alan: Garden avlusu.~~ ✅ Tamam (17 Ağustos 2026)
   - Açık hava avlu: sürekli çim (kare taş yok), yan çitler, açık kapı, uzakta yol, gökyüzü, güneş ve bulutlar.
   - Bahçe ağacı; 2 kuş ağaca gelip gidiyor. Kenar çiçekleri + arı/kelebek.
   - 10 ROOM ürünü: yarn ball, flower pots, daisy bed, sapling, bird bath, sun lounger, balcony set, paw grill, hammock, sun pergola.
   - Unlock: Bedroom koleksiyonu + Home Level 5 + 6000 Coin / 60 elmas. MY ROOMS 5. kart, 3. satıra düşer.

Numaralı paket 1–6 ve Aşama 4 kuyruğu kapandı (17 Ağustos 2026).

7. ~~Garden Runner scenery.~~ ✅ `Variant_GardenCourtyard` 9. silüet; Garden odası kilitliyken havuza girmez.
8. ~~Bond milestone içerikleri.~~ ✅ `BondMilestoneService`: 35 Mouse Hunt, 80 Window Watch, 150 Feather Play, 250 Bird Watch. Quest paneli sonraki hediyeyi gösterir.
9. ~~Ev geliştirmesi ve kedi etkileşimi.~~ ✅ Pencere nöbeti, tüy oyuncağı ve bahçe kuş izleme `SitLookActivity` ile.
10. ~~Görev dengeleme.~~ ✅ Chapter 4 Home Loop (Runner / Store / Tunnel) ve Chapter 5 Garden Bond. Satın alma Home XP, bakım Bond, koşu Coin döngüsünü görevlere bağlar.

Balkon, Garden Patio ve Second Floor oynanabilir (Home Level 9 / 10 / 12); her biri 10 ürünlük koleksiyona sahiptir. Katalog toplamı **114**.

11. ~~Aşama 5 retention çekirdeği.~~ ✅ Tamam (18 Ağustos 2026)
    - UTC giriş serisi: 15–80 Coin, her 7. günde 1 Diamond.
    - 3 günlük ev görevi (Eat/Drink/Pet/Ball/Runner); Quest paneli chapter bitince de gösterir.
    - Achievement: first run, first shop (oturum başı), Home LV 3/5, Bond 80/250, 7-gün streak.
    - Runner x2 Coin reklamı mevcut ödülü değiştirmez; ayrı `cat-runner:{id}:double` txn.
    - Enerji / Catch can / sınırsız geçişe dokunulmadı.

12. ~~Aşama #4 yaşam katmanı + premium title v2.~~ ✅ Tamam (19 Ağustos 2026)
    - Koleksiyon 10'luk + katalog ödülü yalnız COLLECT (`CollectionMilestoneTests`).
    - Local notification seam, SFX cilası, `SKIP TOUR`.
    - Title v2: 3D kedi, HOME LV / jeton / `X OF 84 • COLLECTED`, CONTINUE, CREDITS.
    - EditMode **243/243**, PlayMode **10/10**, validator 0/0.

13. ~~Coming Soon oda zinciri.~~ ✅ Tamam (20 Ağustos 2026)
    - Balcony LV9 / 7000 Coin, Garden Patio LV10 / 8000 Coin, Second Floor LV12 / 9000 Coin.
    - Her oda 10 ROOM ürünü, koleksiyon ödülü, Room Selector ve HOME kartı içerir.
    - Katalog **84 → 114**; save şeması **v11** değişmedi.
    - Second Floor gerçek preview + Rooms `X` kapanış regresyonu + TMP warning temizliği tamamlandı.
    - EditMode **247/247**, PlayMode **10/10**, validator **0/0**, normal Play Console temiz.

## Yayın kalitesi / "tam paket" işleri (19 Ağustos 2026 başladı)

Oyun çekirdek döngüsü tam ama yayın hissi için eksik katmanlar sırayla ekleniyor:

1. [x] **Ana oyun ses/ambiyans** — `HomeAudioService` + `HomeAudioController` (prosedürel müzik + ambiyans + UI/olay SFX; mini-oyunda duck).
2. [x] **Kedi kişiselleştirme** — isim + 8 kürk rengi (kalıcı, kediye canlı tint); `CatJournalPanel` hamburger CAT JOURNAL satırından açılır.
3. Ana menü + ayarlar:
   - [x] **Ayarlar paneli** — `SettingsPanel` (SETTINGS satırı): MUSIC / SOUND / GAME SOUND / VIBRATION / REDUCED MOTION toggle'ları mevcut pref servislerine bağlı.
   - [x] **Başlık / ana menü ekranı** — `TitleScreen` v4: açık Blender CAT HOME amblemi, portresiz ve çakışmasız marka dock'u, HOME LV / jeton / `X OF 114 • COLLECTED`, katmanlı PLAY/CONTINUE, SETTINGS, CREDITS, masaüstü QUIT ve aynı Blender diorama ailesindeki işlevsel SHOP/ROOMS/GAMES kartları. Boot değişikliği yok.
4. [x] **Yaşam katmanı** (19 Ağustos 2026):
   - [x] Kedi idle kişiliği: `CatIdleBehavior` + `CatIdlePersonality`; mevcut clip'ler; kilit almaz; mırıltı + bubble.
   - [x] Koleksiyon tamamlama ödülü + `X OF 84 • COLLECTED`; oda 10'luk bitince kutlama; ödül yalnız COLLECT (`TryClaim`).
   - [x] Bildirimler (local notification seam): enerji dolu, dailies, çevrimdışı ödül. Editörde simülasyon.
   - [x] SFX cilası: yeme/içme, sevme, aktivite, oda değişimi, achievement (`HomeAudioController`).
   - [x] Onboarding polish: title → PLAY → isim → tur; `SKIP TOUR`; title açıkken tutorial gizli.

### Checkpoint — 19 Ağustos 2026 (#4 yaşam katmanı + title v2)

- Koleksiyon: `CollectionMilestoneService` + kutlama overlay (sorting 290). Grant yalnız COLLECT; satın alma cüzdan testlerini bozmaz.
- Bildirim seam: `LocalNotificationService` (enerji / dailies / +4 saat çevrimdışı); editör log, provider yoksa sessiz.
- SFX: yeme/içme/sevme/aktivite/oda/kutlama. Onboarding: `SKIP TOUR` (visual v12).
- Title v2: 3D kedi RT, HOME LV / jeton / `X OF 84 • COLLECTED`, CONTINUE/PLAY, CREDITS, QUIT. Smoke testler `targetTexture == null` kameraları sayar.
- EditMode **243/243**, PlayMode **10/10**, `LevelContentValidator` 0/0, 1 ekran kamerası. Canonical 3-sahne. Git commit/push yok.

### Checkpoint — 20 Ağustos 2026 (Coming Soon odalar + Rooms QA)

- Balcony, Garden Patio ve Second Floor oynanabilir; katalog 114, save v11.
- `SecondFloorPreview.png` ve HOME mağaza kopyası 1280×720 gerçek oda kamerasından üretildi.
- Room Selector `X`, oda geçişinde kapatıldıktan sonra sonraki açılışta yeniden etkinleşir; scrim aynı lifecycle'ı izler.
- Fredoka'nın desteklemediği Ellipsis/`✦` kullanımları warning üretmeyen biçime çevrildi.
- EditMode **247/247**, PlayMode **10/10**, `LevelContentValidator` 0/0; normal Play Console 0 error / 0 warning; kanonik üç sahne ve 1 kamera/listener/EventSystem.

### Checkpoint — 20 Ağustos 2026 (Second Floor SHOP ikonları + simetrik Rooms X)

- Second Floor ROOM koleksiyonundaki 10 mobilyanın tamamı için 512×512 gerçek prefab renderı üretildi; kartlar fallback mobilya glifi yerine kendi ikonlarına bağlandı.
- Alt dock oda etiketi tek satır, merkez hizalı TMP auto-size kullanır (19→11); `SECOND FLOOR • LEVEL 1` dahil 8 kanonik oda adı taşmadan sığar.
- Rooms kapatma işareti font glifi olmaktan çıkarıldı; aynı merkezde ±45° dönen eş boyutlu iki çubukla geometrik olarak simetrik kuruldu.
- Kilit testleri ikon dosyalarını/kart bağlarını, 8 oda adının tek satırda sığmasını ve X geometrisini doğrular.
- EditMode **250/250**, PlayMode **10/10**, `LevelContentValidator` 0/0; normal Play Console 0 error / 0 warning. 1920×1080 home/dock, Rooms ve Second Floor SHOP QA görüntüleri alındı.

### Checkpoint — 20 Ağustos 2026 (Aşama 5 içerik dengesi sözleşmesi)

- 114 ürün, 8×10 oda koleksiyonu, 7 ücretli oda preview'su, Home XP kapıları ve koleksiyon ödülleri rota düzeyinde denetlendi.
- Kanonik tam oda rotası **145.000 Coin**. Preview fiyatları 3000→9000 düzenli artıyor; her preview Home Level kapısı önceki zorunlu yatırımla erişilebilir.
- Oda tamamlama ödülü 500 Coin, oda koleksiyon maliyetinin yaklaşık %3,4–%4,1'i; hedef %3–%5 bandında. Fiyat/ödül değişikliği gerekmedi.
- `HomeEconomyBalanceTests`: rota bütçesi, gate erişilebilirliği ve tamamlama ödülü oranını kilitler.
- EditMode **253/253**, PlayMode **10/10**, validator 0/0.

Bu checkpointten sonraki çalışma Premium v2 görsel dönüşümdü; aşağıdaki checkpointlerde
tamamlandı. Title `NEW GAME` ve yerel dil altyapısı 21 Ağustos devam diliminde kapandı.

### Checkpoint — 20 Ağustos 2026 (Premium v2 görsel dönüşüm — dalga 1)

- Proje öncesi güvenli arşiv: `Backups/CatHome_before_premium_v2_20260820.zip`.
- Kanonik sanat yönü: `Assets/DesignReferences/CatHome_PremiumV2_Reference.png`; krem/pearl taban, canlı candy pastel, kontrollü altın, koyu renk yalnız metin ve küçük iç kontrast.
- İlk giriş ekranı prosedürel düz fondan sinematik ev/kedi kahramanına geçti: `Assets/Art/Title/CatHome_TitleHero_v1.png`; kişisel canlı kedi rozeti, HOME LV/coin/koleksiyon, PLAY/CONTINUE ve ayarlar/credits/quit korunuyor.
- Mağaza koyu veya düz mavi büyük gövde yerine krem showroom yüzeyi, pembe başlık ve okunaklı koyu açıklamalar kullanıyor. Rooms ve While Away aynı parlak premium yüzey ailesinde kaldı.
- Games hub kartları Runner/Catch kahraman sanatlarını taşır; kart → karşılama ekranı görsel sürekliliği sağlandı.
- Blender kaynak kiti: `ArtSource/Blender/PremiumFurniture/PremiumFurnitureKit_Source.blend`. 12 görünür ürün gerçek yuvarlatılmış premium modele geçirildi: 6 Second Floor, 2 Balcony, 4 Patio. Prefab/placement/rotation/save/economy sözleşmeleri değişmedi; SHOP ikonları gerçek modellerden yeniden üretildi.
- `PremiumPresentationTests` title hero, ortak premium buton davranışı ve 12 Blender model bağlantısını kilitler.
- EditMode **256/256**, PlayMode **10/10**, `LevelContentValidator` **0 error / 0 warning**; kanonik `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room ve 1 kamera geri yüklendi.
- Premium dönüşüm devam ediyor: kalan oda ürünlerinin ikinci Blender dalgası, Runner/Catch welcome/gameplay görsel QA ve bütün panel/akışların 1920×1080 son karşılaştırması henüz kapanmadı.

### Checkpoint — 20 Ağustos 2026 (Premium v2 görsel dönüşüm — tamamlandı)

- İlk giriş, ana ev, hamburger, Quest, Cat Journal, Settings, Rooms, SHOP CAT/ROOM/HOME, satın alma, Diamond Treasure, Games hub, Runner ve Catch karşılama/oynanış akışları aynı krem + candy pastel + kontrollü altın premium dilinde ekran ekran doğrulandı.
- Cat Journal büyük canlı kürk önizlemesi, sekiz kompakt renk seçeneği ve ad/kürk sürekliliği açıklaması aldı. Quest paneli candy başlık, okunaklı görev kartları, premium claim butonları ve simetrik kapatma kontrolüyle yenilendi.
- Blender premium mobilya kiti ikinci dalgada 14 görünür ürünü daha kapsadı: Bathroom tub/vanity/toilet; Kitchen island/refrigerator/stove/pantry; Bedroom queen bed/wardrobe/window daybed; Garden pergola/sun lounger/bistro set/hammock. Premium model bağlantısı toplam **26** ürüne çıktı; prefab, placement, fiyat ve save sözleşmeleri korunarak ikonlar gerçek modellerden yeniden üretildi.
- Outdoor Room Selector/HOME kartları gerçek gün/gece sistemini değiştirmeden sabit showroom pozlamasıyla bake edilir. Garden, Balcony ve Patio kartları akşam editör saatinde dahi parlak, okunaklı ve birbirleriyle tutarlı kalır; Second Floor da aynı 16:9 kamera zincirindedir.
- Runner gameplay canlı pet-town paleti ve okunaklı paw coin hattını; Catch gameplay candy arena, lila fareler ve açık HUD kapsüllerini korur. İki welcome kartı kahraman sanatı, skor/can bilgisi ve ana eylemlerle görsel olarak Games hub'a bağlandı.
- Final QA görselleri `Assets/Screenshots/PremiumV2/Final1920_*.png` altında 1920×1080 teslim boyutunda saklandı. Görsel referans `Assets/DesignReferences/CatHome_PremiumV2_Reference.png`, title hero `Assets/Art/Title/CatHome_TitleHero_v1.png`.
- Son doğrulama: EditMode **256/256**, PlayMode **10/10**, `LevelContentValidator` **0 error / 0 warning**, temiz Console; kanonik `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room ve tam **1** etkin kamera. Git commit/push yok.

### Checkpoint — 20 Ağustos 2026 (Referans uyumlu Main Menu v3)

- Ana menü referans panosundaki marka vitrini yapısına geçti: solda 3D CAT HOME amblemi + canlı kedi/statlar + baskın CONTINUE; ortada sinematik kedi sahnesi; sağda görsel SHOP / ROOMS / GAMES kartları.
- Blender kaynağı `ArtSource/Blender/Title/CatHome_MainMenuLogo_Source.blend`; Unity çıktıları `CatHome_MainMenuLogo_v1.png` ve `Models/CatHome_MainMenuLogo.fbx`. Amblem kedi kulaklı pearl plaka, sky/mint katmanlar ve altın kabartma kullanır.
- Üç sağ kart dekor değildir: title fade tamamlandıktan sonra sırasıyla gerçek mağaza, Room Selector ve Games hub açılır. Yavaş additive yüklemede canlı kedi portresinin boş kalmaması için bağlanma yeniden-deneme davranışı kalıcı hale getirildi.
- Final görsel `Assets/Screenshots/PremiumV2/Final1920_MainMenuV3.png`. EditMode **256/256**, PlayMode **10/10**, validator **0/0**; kanonik üç sahne geri yüklendi. Git commit/push yok.

### Checkpoint — 20 Ağustos 2026 (Main Menu v4 — tutarlı premium sanat ailesi)

- Boş kalabilen canlı kedi portresi ve ona ait RenderTexture/yeniden-bağlanma akışı menüden tamamen kaldırıldı. Sol marka dock'u sabit optik ritimle yeniden ölçülendi; logo, karşılama, isim, iki stat, koleksiyon, CONTINUE, yardımcı eylemler ve alt slogan arasında otomatik dikdörtgen taramasında **0 çakışma** var.
- CAT HOME Blender logosu daha açık pearl, aqua/mint ve şampanya altınıyla yeniden render edildi; şeffaf kenar boşluğu Unity'de kırpılarak amblem gerçek alanı dolduruyor. Kaynak: `ArtSource/Blender/Title/CatHome_MainMenuLogo_Source.blend`.
- SHOP / ROOMS / GAMES artık karışık ekran görüntüsü/illüstrasyon kullanmıyor. Üçü aynı ortografik kamera, üç noktalı stüdyo ışığı, candy malzeme ve yuvarlatılmış low-poly diorama setinden üretildi: `ArtSource/Blender/Title/CatHome_MainMenuCards_Source.blend` ve `Assets/Art/Title/MainMenu_*Card_v1.png`.
- Ana ve yardımcı butonlar aynı merkezli dış aura + şampanya rim + dört duraklı candy yüz + üst cam bandı + iç derinlik bandı kullanır; dış Shadow/Outline yoktur. `ColorTint` kapatıldı, `PremiumButtonFx` etkileşimi korunuyor. Üç sağ kart aynı kart/rim/görsel kuyu/etiket hiyerarşisini kullanır.
- SHOP, ROOMS ve GAMES canlı sahnede tek tek doğru hedeflerini açtı. Final görsel `Assets/Screenshots/PremiumV2/MainMenuV4_Final1920.png` (**1920×1080**). `PremiumPresentationTests` **3/3**, tüm EditMode **256/256**, PlayMode **10/10**, validator **0/0**, Console temiz; kanonik üç sahne ve tam 1 kamera/listener/EventSystem geri yüklendi. Git commit/push yok.

### Checkpoint — 21 Ağustos 2026 (Main Menu v4 neon + ilk açılış)

- CAT HOME Blender amblemi daha aydınlık pearl/candy malzemelerle yeniden render edildi; ölçülü neon aura, dört köşe parıltısı ve reduced-motion uyumlu nefes animasyonu `TitleLogoNeonFx` ile eklendi.
- İlk kez açan oyuncu aynı premium menüyü `WELCOME HOME`, `PLAY`, HOME LV. 1, 0 Coin ve `0 OF 114 • COLLECTED` durumuyla görür. SHOP / ROOMS / GAMES kartları teaser olarak görünür ancak onboarding tamamlanana kadar `AFTER TOUR` rozetiyle pasiftir.
- Mobilde QUIT gizlenirken SETTINGS ve CREDITS optik merkeze yeniden hizalanır. QA görselleri: `Assets/Screenshots/PremiumV2/MainMenuV4_NeonFinal1920.png`, `MainMenuV4_FirstLaunchNeonFinal1920.png` ve `MainMenuV4_FirstLaunchMobilePreview.png`.
- EditMode **256/256**, PlayMode **10/10**, `LevelContentValidator` **0 error / 0 warning**; kanonik üç sahne ve 1 kamera/listener/EventSystem geri yüklendi. Git commit/push yok.

**Güncel devralma:** Premium v2, Main Menu v4, yerel kayıt kurtarma, onaylı
`NEW GAME`, Türkçe/İngilizce çekirdek, gerçek Unity Player Accounts/Google bağlantısı,
Cloud Save write-lock akışı, herkese açık Player Care sitesi ve sıralama/ödül dilimi
canlı Unity Cloud production dağıtımıyla tamamlandı. Android gerçek cihaz dönüş testi
kullanıcı tarafından ertelendi. Player Care URL'lerinin Dashboard/mağaza alanlarına
bağlanması ve ileride kanonik alan adına taşınması açık. Tam çeviri paketi metin
dondurmada yapılır. Güncel devir: `Docs/CatHome_Checkpoint_2026-08-24.md`.

### Checkpoint — 21 Ağustos 2026 (onaylı NEW GAME + TR/EN altyapısı)

- Ana menüde yalnız mevcut yolculuk varsa görünen `NEW GAME` eylemi ve ayrı
  `VAZGEÇ` / `EVET, YENİ OYUN` onay katmanı eklendi.
- Sıfırlama: Coin, Home/Bond ilerlemesi, görevler, achievement/daily, oda ve ürün
  sahipliği, yerleştirmeler, Runner/Catch skor/tutorial/misyonları ve kedi adı/kürkü
  temizlenir; başlangıç ihtiyaçları 100/100/100, oda Living Room olur.
- Diamonds, idempotent satın alma işlem kimlikleri, Runner/Catch doğrulanmış
  sınırsız geçiş tarihleri, ses/haptics/reduced-motion tercihleri ve dil korunur.
  Yazmadan önce timestamp'li `.before-new-game-*` destek kopyası alınır.
- `GameLanguageService` paket gerektirmeyen TR/EN çekirdeğidir. Sistem dili Türkçe
  ise ilk açılış Türkçe, diğer diller İngilizce; Ayarlar'daki LANGUAGE/DİL satırı
  seçimi kalıcı ve canlı uygular. Main Menu, Settings ve NEW GAME çevrilmiştir.
- QA: 1920×1080 TR Main Menu, TR/EN Settings ve TR onay görselleri
  `Assets/QA/PremiumVisuals/2026-08-21_*.png`.
- EditMode **268/268**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1**. Save **v11**.
- Git commit/push yok.

### Checkpoint — 21 Ağustos 2026 (hesap çekirdeği + Google/misafir seçimi)

- İlk oyuncunun `PLAY` eylemi ve mevcut oyuncunun onaylı `NEW GAME` eylemi,
  ilerleme başlamadan önce `GOOGLE İLE GİRİŞ YAP` / `MİSAFİR OLARAK DEVAM ET`
  kartını açar. Misafir seçimi interneti beklemeden oyuna girer.
- Google avantajı kartta `BULUT KAYDI • ÇEVRİMİÇİ SIRALAMA • BAŞKA CİHAZDA DEVAM`
  olarak anlatılır. Cat Home parola veya Gmail kutusu erişimi istemez.
- `AccountIdentityService` rastgele yerel misafir kimliğini save v11 dışında tutar;
  Unity Authentication `3.5.2` anonim oyuncuyu ve Unity Player Accounts tarayıcı
  girişini yönetir. Guest→Google yükseltmesi aynı UGS oyuncusuna link uygular.
- NEW GAME sıfırlaması hesap seçimi tamamlanmadan çalışmaz. Google giriş hatası veya
  iptalinde mevcut save güvendedir; hesap bağlantısı NEW GAME ile silinmez.
- Settings yedinci `ACCOUNT / HESAP` satırında `CHOOSE / GUEST / GOOGLE CONNECTED`
  durumu gösterir ve misafir oyuncunun daha sonra bağlantı başlatmasını sağlar.
- Unity Dashboard Player Accounts sağlayıcısı `CatHome` adıyla PC + Android/iOS için
  etkinleştirildi; proje ayarı gerçek OAuth client ID ile senkronlandı. Editor'da
  gerçek Google hesabı, localhost callback, Player Accounts ve UGS Authentication
  bağlantısı başarıyla doğrulandı. Android cihaz deep-link testi yayın öncesinde açık.
- QA: `2026-08-21_AccountChoice_TR.png`, `2026-08-21_AccountChoice_EN.png`,
  `2026-08-21_SettingsAccount_TR.png`.
- EditMode **270/270**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik sahneler ve Camera/AudioListener/EventSystem **1/1/1**. Save **v11**.
- Git commit/push yok.

### Checkpoint — 21 Ağustos 2026 (resmî Google rozeti + mobil dönüş güvenliği)

- Google eylemi Google'ın resmî ön onaylı renkli `G` karesini kullanır. Simge
  değiştirilmez; dışındaki aqua halo, champagne rim ve pearl yüzey Cat Home premium
  diliyle uyumludur. Kaynak/not `Assets/Art/Title/Google/README.md` içindedir.
- Player Accounts dönüşü platformlar arasında güvenli hale getirildi. Desktop
  localhost dönüşü ve Android/iOS deep-link dönüşü `SignedIn`/`SignInFailed` olayıyla
  tamamlanmadan UGS link/sign-in başlamaz. Beklerken `GERİ` kullanılabilir.
- 1920×1080 QA: `Assets/QA/PremiumVisuals/2026-08-21_AccountChoice_TR_GoogleBrand.png`.
- Unity Cloud'da `CatHome` Unity Player Accounts sağlayıcısı PC + Android/iOS için
  etkinleştirildi. Gerçek Google hesabıyla Editor localhost dönüşü tamamlandı;
  Player Accounts ve UGS Authentication oturumları bağlı doğrulandı. Client ID yalnız
  Unity servis ayarındadır; token, istemci sırrı veya erişim anahtarı repoya yazılmadı.
  Dashboard gizlilik politikası bağlantısı ve Android gerçek cihaz dönüş testi açık.
- EditMode **271/271**, PlayMode **10/10**, validator **0/0**; kanonik üç sahne ve
  Camera/AudioListener/EventSystem **1/1/1**. Save **v11**. Git commit/push yok.

### Checkpoint — 21 Ağustos 2026 (Player Care yayını + rekabet dilimi)

- Cat Home Player Care sitesi TR/EN gizlilik, hesap silme ve veri talebi
  sayfalarıyla herkese açık yayımlandı:
  `https://cathome-player-care.hhknshnn.chatgpt.site`.
- Games Hub'a premium `LEADERBOARDS` paneli eklendi. Runner/Catch ile
  DAILY/WEEKLY/ALL-TIME sekmeleri, ilk 50, oyuncunun kendi sırası ve çevrimdışı son
  güvenli liste hazırdır. 24 Ağustos düzeltmesiyle ayrı takma ad akışı kaldırıldı;
  onboarding'deki kedi adı kullanılır. Google gerçek adı veya e-postası tablo
  metadatasına yazılmaz.
- `CatHomeCompetition` Cloud Code modülü Runner/Catch ham oyun verisinden kanonik
  skoru yeniden hesaplar, tekrar kullanılan run/hunt kimliğini reddeder ve yalnız
  sunucu üzerinden altı tabloya yazar. Günlük/haftalık arşiv ödülleri dönem+sıra
  tabanlı ve idempotent işlem kimliklidir.
- Altı `.lb` tanımı (günlük 00:00 UTC, haftalık Pazartesi 00:00 UTC, all-time),
  doğrudan istemci skor yazımını engelleyen access-control politikası ve sunucu
  modülü 21 Ağustos'ta yerelde hazırdı; canlı Unity Cloud dağıtımı 24 Ağustos'ta
  tamamlandı.
- Cloud Code modülü Release build: **0 hata / 0 uyarı**. EditMode **295/295**,
  PlayMode **10/10**, validator **0/0**, Console temiz; kanonik üç sahne, aktif
  Living Room ve Camera/AudioListener/EventSystem **1/1/1**. Save **v11**.
- Android gerçek cihaz deep-link testi kullanıcı tarafından daha sonraya ertelendi.
  Git commit/push yok.

### Checkpoint — 21 Ağustos 2026 (HUNT düşük enerji açıklaması)

- Mouse Hunt maliyeti 12 enerjidir. Enerji yetersizken ortak aktivite düğmesi artık
  yanıltıcı `HUNT` yerine dinamik `NEED 12 ENERGY` gösterir; dokunma uyuma uyarısını
  verir. Yeterli enerjide `HUNT` aktiviteyi başlatır ve `CATCH THE MOUSE 0/3`
  ilerlemesine geçer.
- EditMode **296/296**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1**. Git commit/push yok.

### Checkpoint — 24 Ağustos 2026 (Unity Cloud production dağıtımı)

- Runner/Catch için altı Leaderboards tanımı production ortamına dağıtıldı. Günlük
  ilk dönem `2026-08-25T00:00:00Z`, haftalık ilk dönem
  `2026-08-31T00:00:00Z`; all-time tablolar sıfırlanmaz.
- `CatHomeCompetition.ccm` canlı Cloud Code modülü olarak derlendi, yüklendi ve uzak
  modül listesinde doğrulandı. Doğrudan Player skor yazımını reddeden access-control
  politikası da production ortamında günceldir.
- Test Runner geçici başlangıç sahnesini domain reload sonrasında da tanıyan koruma
  ve hızlandırılmış Runner QA'da coin bütçesini aynı kareye yetiştiren zaman örnekleme
  düzeltmesi eklendi.
- EditMode **296/296**, PlayMode **10/10**, validator **0/0**, Console temiz;
  `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room ve
  Camera/AudioListener/EventSystem **1/1/1** geri yüklendi. Git commit/push yok.

### Checkpoint — 24 Ağustos 2026 (kedi adıyla otomatik leaderboard)

- Ayrı `PLAYER NAME` girişi ve `SAVE NAME` kaldırıldı. Leaderboard kimliği artık
  doğrudan onboarding/CAT JOURNAL kanonik kedi adıdır; Google adı ve e-posta yine
  kullanılmaz.
- Cloud Code `SubmitResult` içindeki `nickname` alanı istemci yanıt modeline eklendi.
  Başarılı skor yazımından sonra oluşan deserialization hatası giderildi ve istemci
  artık gerçek `SCORE SUBMITTED` sonucunu alıyor.
- Production canlı doğrulamasında `lokiş` adı ve **101** doğrulama skoru Runner
  DAILY/WEEKLY/ALL-TIME tablolarının üçünde de sıra **#1** olarak okundu.
- 1920×1080 QA: `Assets/QA/PremiumVisuals/2026-08-24_Leaderboard_CatName_Auto_Screen.png`.
- EditMode **297/297**, PlayMode **10/10**, validator **0/0**; kanonik sahne ve
  Camera/AudioListener/EventSystem **1/1/1** geri yüklendi. Git commit/push yok.

### Checkpoint — 24 Ağustos 2026 (global leaderboard podyumu)

- Kullanıcı kararıyla leaderboard arkadaşlarla sınırlı değil, bütün uygun Cat Home
  oyuncularının yer aldığı global tablodur.
- İlk üç oyuncu altın/gümüş/bronz `GOLD PAW / SILVER PAW / BRONZE PAW` podyumunda;
  4–50 kaydırmalı listede gösterilir. Global oyuncu sayısı ve oyuncunun kendi sıra
  kartı sabit kalır.
- `lokiş` production verisiyle altın #1 podyumunda doğrulandı. 1920×1080 QA:
  `Assets/QA/PremiumVisuals/2026-08-24_Leaderboard_GlobalPodium_Screen.png`.
- EditMode **298/298**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1**. Git commit/push yok.

### Checkpoint — 24 Ağustos 2026 (Home 2.0 — oda düzenleme temeli)

- Hamburger menüsüne kalıcı `EDIT ROOM` eylemi eklendi. Oyuncu sahip olduğu oda
  eşyalarını seçebilir, sürükleyebilir ve tek `TURN` eylemiyle 45 derece döndürebilir.
  Geçerli sürükleme bırakıldığı anda otomatik kaydedilir.
- `STORE` bir eşyayı satmaz veya koleksiyondan çıkarmaz; sahiplik, koleksiyon ve son
  yerleşim korunurken eşya odada gizlenir. Saklanan eşya aynı editörde geri seçilip
  sürüklenerek odaya alınır. Destek eşya saklanırsa bağlı TV/kitap seti de görünür
  kalmaz; sahiplikleri ve yerleşimleri korunur.
- Düzenleme paneli isimli ortak `Canvas` altında premium yüzeylerle üretildi; üst HUD,
  mevcut alt dock, mağaza ve diğer premium arayüz korunur. Seçim için hem doğrudan
  eşyaya dokunma hem başlıktaki küçük önceki/sonraki kontrolleri vardır. Büyük komut
  satırı dokuz butondan `TURN / STORE / DONE` üçlüsüne indirildi.
- `FLOOR ZONE` mint zemin sınırı, `WALL ZONE` odanın gerçek üç duvarındaki lilac
  raylar, `BOOKSHELF ONLY` ve `TV UNIT ONLY` ise tek geçerli hedefte altın çerçeve
  gösterir. Açık kamera kenarı artık duvar sayılmaz; TV ünitesi odada değilse TV
  yerleştirilemez.
- Living Room için ilk Home 2.0 sanat geçişi tamamlandı: kanepede dokulu mint/lilac
  minderler ve pearl şerit, halıda altın dikiş çerçevesi ile pati motifi, sehpada
  runner ve mint merkez süsü. Korunan ana eşya transform/collider değerleri değişmedi.
- 1920×1080 QA: `Assets/QA/PremiumVisuals/2026-08-25_Home2_CompactEditor_WallZone_Stable.png`
  ve `2026-08-25_Home2_CompactEditor_TvUnitTarget.png`.
- EditMode **305/305**, PlayMode **10/10**, validator **0/0**, Console temiz;
  `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room ve
  Camera/AudioListener/EventSystem **1/1/1** geri yüklendi. Git commit/push yok.

### Checkpoint — 25 Ağustos 2026 (Home 2.0 — eşya set kimliği başlangıcı)

- Living Room'un on ürünü iki okunaklı tasarım ailesine ayrıldı: altı parçalık
  `READING` köşesi ve dört parçalık `MEDIA` köşesi. Bu ayrım fiyat, sahiplik veya
  mevcut koleksiyon tamamlama ekonomisini değiştirmez.
- Mağaza kartlarının üst etiketi seti ve gerçek yerleşim ailesini birlikte gösterir:
  `READING • FLOOR`, `READING • WALL`, `READING • BOOKSHELF`, `MEDIA • FLOOR`,
  `MEDIA • WALL` ve `MEDIA • TV UNIT`. READING lilac, MEDIA aqua vurgu ailesidir.
- 1920×1080 QA: `Assets/QA/PremiumVisuals/2026-08-25_Home2_DesignSets_Store.png`
  ve `2026-08-25_Home2_DesignSets_Media.png`. EditMode **305/305**, PlayMode
  **10/10**, validator **0/0**. Git commit/push yok.

### Checkpoint — 25 Ağustos 2026 (proje-geneli UI ayrışması + 3. adım ekonomi görünürlüğü)

- Aynı anda görünen ayrı buton/panel/popup/HUD/dock yüzeyleri için en az 16 px
  görünür boşluk proje kuralı oldu. `PremiumUiOverlapTests` bütün `Assets/UI`
  prefablarındaki varsayılan görünür butonları tarar; EDIT ROOM–HomeDock ve mağaza
  kartı status–fiyat–eylem aralıklarını ayrıca kilitler.
- Taramada bulunan üç gerçek taşma düzeltildi: EDIT ROOM alt eylemleri HomeDock'tan,
  mağazadaki `AT HOME` rozeti fiyat/eylem kapsüllerinden ve Player Care'deki
  `SYNC NOW` butonu hesap silme butonundan ayrıldı.
- EDIT ROOM artık offline dönüş popup'ı, Player Care, Settings, Cat Journal, Games,
  oda/mağaza/görev yüzeyleri veya kutlama katmanlarından biri açıkken açılmaz;
  iki bloklayan modal üst üste bindirilemez.
- 3. adımın ilk ekonomi görünürlüğü paketi mevcut footer'ı kullanır: Living Room
  mağazası `READING X/6 • MEDIA Y/4 • N COINS LEFT` gösterir. Değer doğrudan
  kanonik ürün fiyatı ve sahiplikten hesaplanır; fiyat, ödül, save v11 veya para
  harcama akışı değişmedi.
- 1920×1080 QA: `Assets/QA/PremiumVisuals/2026-08-25_ProjectWide_NoOverlap_EditRoom_Final.png`,
  `2026-08-25_ProjectWide_NoOverlap_PlayerCare.png` ve
  `2026-08-25_Home3_EconomyProgress_Final.png`. EditMode **310/310**, PlayMode
  **10/10**, validator **0/0**, Console temiz. Git commit/push yok.

### Checkpoint — 26 Ağustos 2026 (EDIT ROOM eylem kartı taşma düzeltmesi)

- Ekran görüntüsündeki hata, `TURN / STORE / DONE` yüzlerinin 126 px yüksekliğindeki
  `Toolbar` kartında `y=-70`, `h=64` kullanması nedeniyle alt çerçeveyi 8 px
  aşmasıydı. Eylemler `y=-52`, `h=56` olacak şekilde yeniden yerleştirildi.
- Üç eylem artık krem kartın içinde her kenarda en az 12 px payla kalıyor ve alt
  `HomeDock` yüzeyine yaklaşmıyor. `HomeEditActions_StayFullyInsideToolbarCard`
  testi, builder yeniden çalıştırıldığında bu iç taşmanın geri gelmesini engelliyor.
- 1920×1080 QA:
  `Assets/QA/PremiumVisuals/2026-08-25_HomeEdit_ContainedActions_Final.png`.
  EditMode **311/311**, PlayMode **10/10**, validator **0/0**; kanonik üç sahne,
  aktif Living Room ve tam 1 etkin kamera geri yüklendi. Git commit/push yok.

### Checkpoint — 27 Ağustos 2026 (Unity servis Console temizliği)

- Cat Home kodu tarafından kullanılmayan `com.unity.ai.assistant` önizleme paketi
  kaldırıldı. Böylece abonelik/puan servisine giden ve `PointsBalanceResult` ile
  `SettingsResult` hataları üreten `generators.ai.unity.com` sorguları kesildi.
- Google bağlantısı, Cloud Save, Cloud Code ve leaderboard için gereken Unity
  Services paketleri korundu. Paket çözümlemesi Editor oturumunu yeniledi; geçici
  `TokenExchange` boş yanıtı tekrar oluşmadı.
- EditMode **311/311**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne, aktif Living Room ve Camera/AudioListener/EventSystem **1/1/1**
  geri yüklendi. Git commit/push yok.

### Checkpoint — 28 Ağustos 2026 (3. adım ekonomi hedefi + ücretsiz QA kararı)

- ROOM footer artık yalnız büyük toplam maliyeti göstermek yerine kanonik fiyat
  sırasındaki ilk sahip olunmayan ürünü `NEXT <ITEM>` olarak gösterir. Gerçek
  ekonomi açıldığında mevcut cüzdandan türetilen `READY TO BUY` veya
  `NEED N COINS` bilgisi kullanılır.
- Mağaza kartlarının seviye kilidi eski görev chapter değerinden ayrıldı ve
  kanonik `HomeProgressionService.HomeLevel` kaynağına bağlandı.
- Kullanıcı kararıyla içerik/yerleşim denemeleri sürerken mağaza
  `EconomyChecksEnabled = false` kalır; kartlar `FREE TEST / GET`, footer
  `NEXT <ITEM> • FREE TEST` gösterir ve cüzdan harcamaz. Gerçek Coin harcaması
  yalnız son ekonomi/yayın kapısında açılacaktır; `TryPurchase` yolu testlerle
  korunmaya devam eder.
- 1920×1080 QA:
  `Assets/QA/PremiumVisuals/2026-08-28_Home3_FreeTestNextGoal.png`.
  EditMode **312/312**, PlayMode **10/10**. Git commit/push yok.

### Checkpoint — 28 Ağustos 2026 (Home 2.0 — Bathroom CARE / SPA)

- Bathroom'un on ürünü iki okunaklı tasarım ailesine ayrıldı: altı parçalık
  `CARE` ve dört parçalık `SPA`. CARE lilac, SPA aqua kart vurgusunu kullanır.
- Mağaza kartı artık seti gerçek yerleşim ailesiyle birlikte gösterir. Paw Bath
  Mat, Laundry Hamper, Cat Litter Box ve Grooming Cart `CARE • FLOOR`; Towel
  Storage ile Bubble Wall Mirror `CARE • WALL`; Toilet, Vanity, Bathtub ve
  Shower `SPA • WALL` olarak görünür.
- `FLOOR/WALL` metni editör yerleşim verisinden kopmaması için testte
  `StoreCatalogAssets.PlacementKind` ile birebir karşılaştırılır. Bathroom ROOM
  footer'ı `CARE X/6 • SPA Y/4 • NEXT <ITEM> • FREE TEST` gösterir.
- Fiyatlar, sahiplik, koleksiyon sayısı ve mevcut ücretsiz QA akışı değişmedi;
  `EconomyChecksEnabled = false` korunuyor.
- 1920×1080 QA:
  `Assets/QA/PremiumVisuals/2026-08-28_BathroomHome2_CareSpaStore.png`.
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne, aktif Living Room ve Camera/AudioListener/EventSystem
  **1/1/1** geri yüklendi. Git commit/push yok.

### Checkpoint — 29 Ağustos 2026 (Home 2.0 — Kitchen CAFE / CHEF)

- Kitchen'ın on ürünü `CAFE` (4) ve `CHEF` (6) tasarım ailelerine ayrıldı.
  CAFE sıcak peach/orange, CHEF aqua kart vurgusunu kullanır.
- Paw Breakfast Rug, Rainbow Fruit Basket, Twin Feeding Station ve Breakfast
  Stool `CAFE • FLOOR`; Pantry Shelf `CHEF • WALL`, Dish Cart ve Kitchen Island
  `CHEF • FLOOR`; Sink Cabinet, Refrigerator ve Stove & Oven `CHEF • WALL`
  olarak görünür.
- Runtime `FLOOR/WALL` etiketi testte kanonik
  `StoreCatalogAssets.PlacementKind` verisiyle birebir karşılaştırılır. Kitchen
  ROOM footer'ı `CAFE X/4 • CHEF Y/6 • NEXT <ITEM> • FREE TEST` gösterir.
- Fiyat, sahiplik ve koleksiyon ekonomisi değişmedi; ücretsiz `FREE TEST / GET`
  akışı ve `EconomyChecksEnabled = false` korunuyor.
- 1920×1080 QA:
  `Assets/QA/PremiumVisuals/2026-08-29_KitchenHome2_CafeChefStore.png`.
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne, aktif Living Room ve Camera/AudioListener/EventSystem
  **1/1/1** geri yüklendi. Git commit/push yok.

### Checkpoint — 29 Ağustos 2026 (Home 2.0 — Bedroom COZY / ROYAL)

- Bedroom'ın on ürünü `COZY` (6) ve `ROYAL` (4) tasarım ailelerine ayrıldı.
  COZY lilac, ROYAL pembe kart vurgusunu kullanır.
- Starry Paw Rug, Moon Night Light, Yarn Basket ve Cloud Vanity Stool
  `COZY • FLOOR`; Dream Wall Art ile Pastel Nightstand `COZY • WALL`;
  Rainbow Wardrobe ve Window Daybed `ROYAL • WALL`; Star Canopy ve Queen
  Cloud Bed `ROYAL • FLOOR` olarak görünür.
- Runtime `FLOOR/WALL` etiketi testte kanonik
  `StoreCatalogAssets.PlacementKind` verisiyle birebir karşılaştırılır. Bedroom
  ROOM footer'ı `COZY X/6 • ROYAL Y/4 • NEXT <ITEM> • FREE TEST` gösterir.
- Fiyat, sahiplik ve koleksiyon ekonomisi değişmedi; ücretsiz `FREE TEST / GET`
  akışı ve `EconomyChecksEnabled = false` korunuyor.
- 1920×1080 QA:
  `Assets/QA/PremiumVisuals/2026-08-29_BedroomHome2_CozyRoyalStore.png`.
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne, aktif Living Room ve Camera/AudioListener/EventSystem
  **1/1/1** geri yüklendi. Git commit/push yok.

### Checkpoint — 29 Ağustos 2026 (Home 2.0 — Garden NATURE / PATIO)

- Garden'ın on ürünü `NATURE` (5) ve `PATIO` (5) tasarım ailelerine ayrıldı.
  NATURE aqua/mint, PATIO sıcak peach/orange kart vurgusunu kullanır.
- Sunny Yarn Ball, Flower Pots, Daisy Flower Bed, Little Garden Tree ve Bird
  Bath NATURE; Sun Lounger, Balcony Set, Paw Grill, Garden Hammock ve Sun
  Pergola PATIO ailesindedir.
- Garden açık avlu olduğundan on ürünün tamamı `FLOOR` olarak korunur. Runtime
  etiketi testte kanonik `StoreCatalogAssets.PlacementKind` verisiyle birebir
  karşılaştırılır; sahte bir duvar ailesi eklenmez. ROOM footer
  `NATURE X/5 • PATIO Y/5 • NEXT <ITEM> • FREE TEST` gösterir.
- Fiyat, sahiplik ve koleksiyon ekonomisi değişmedi; ücretsiz `FREE TEST / GET`
  akışı ve `EconomyChecksEnabled = false` korunuyor.
- 1920×1080 QA:
  `Assets/QA/PremiumVisuals/2026-08-29_GardenHome2_NaturePatioStore.png`.
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne, aktif Living Room ve Camera/AudioListener/EventSystem
  **1/1/1** geri yüklendi. Git commit/push yok.

### Checkpoint — 29 Ağustos 2026 (Home 2.0 — Balcony SUNNY / LOUNGE)

- Balcony'nin on ürünü `SUNNY` (5) ve `LOUNGE` (5) tasarım ailelerine ayrıldı.
  SUNNY sıcak peach/orange, LOUNGE lilac kart vurgusunu kullanır.
- Sun Mat, Planter Box ve Bird Feeder `SUNNY • FLOOR`; Herb Shelf ile Railing
  Flowers `SUNNY • WALL`; Lantern String ve Sun Awning `LOUNGE • WALL`;
  Cushion Bench, Side Table ve Hanging Chair `LOUNGE • FLOOR` görünür.
- Runtime `FLOOR/WALL` etiketi testte kanonik
  `StoreCatalogAssets.PlacementKind` verisiyle birebir karşılaştırılır. Balcony
  ROOM footer'ı `SUNNY X/5 • LOUNGE Y/5 • NEXT <ITEM> • FREE TEST` gösterir.
- Fiyat, sahiplik ve koleksiyon ekonomisi değişmedi; ücretsiz `FREE TEST / GET`
  akışı ve `EconomyChecksEnabled = false` korunuyor.
- 1920×1080 QA:
  `Assets/QA/PremiumVisuals/2026-08-29_BalconyHome2_SunnyLoungeStore.png`.
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne, aktif Living Room ve Camera/AudioListener/EventSystem
  **1/1/1** geri yüklendi. Git commit/push yok.

### Checkpoint — 30 Ağustos 2026 (Home 2.0 — Patio OASIS / GATHER)

- Patio'nun on ürünü `OASIS` (5) ve `GATHER` (5) tasarım ailelerine ayrıldı.
  OASIS aqua/mint, GATHER sıcak peach/orange kart vurgusunu kullanır.
- Stone Patio Rug, Potted Ferns ve Water Fountain `OASIS • FLOOR`; Herb Trough
  ile Patio String Lights `OASIS • WALL`; Fire Pit, Patio Dining Set, Garden
  Parasol ve Porch Swing `GATHER • FLOOR`; Pergola Arch `GATHER • WALL` görünür.
- Runtime `FLOOR/WALL` etiketi testte kanonik
  `StoreCatalogAssets.PlacementKind` verisiyle birebir karşılaştırılır. Patio
  ROOM footer'ı `OASIS X/5 • GATHER Y/5 • NEXT <ITEM> • FREE TEST` gösterir.
- Fiyat, sahiplik ve koleksiyon ekonomisi değişmedi; ücretsiz `FREE TEST / GET`
  akışı ve `EconomyChecksEnabled = false` korunuyor.
- 1920×1080 QA:
  `Assets/QA/PremiumVisuals/2026-08-30_PatioHome2_OasisGatherStore.png`.
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne, aktif Living Room ve Camera/AudioListener/EventSystem
  **1/1/1** geri yüklendi. Git commit/push yok.

### Checkpoint — 30 Ağustos 2026 (Home 2.0 — Second Floor NOOK / STUDIO)

- Second Floor'un on Loft ürünü `NOOK` (5) ve `STUDIO` (5) tasarım ailelerine
  ayrıldı. NOOK lilac, STUDIO aqua/mint kart vurgusunu kullanır.
- Loft Floor Runner, Floor Cushions, Book Stacks, Arc Floor Lamp ve Bean Bag
  Chair `NOOK • FLOOR`; Record Player, Study Desk ve Chaise Lounge
  `STUDIO • FLOOR`; Wall Gallery ile Tall Bookcase `STUDIO • WALL` görünür.
- Runtime `FLOOR/WALL` etiketi testte kanonik
  `StoreCatalogAssets.PlacementKind` verisiyle birebir karşılaştırılır. Second
  Floor ROOM footer'ı `NOOK X/5 • STUDIO Y/5 • NEXT <ITEM> • FREE TEST` gösterir.
- Fiyat, sahiplik ve koleksiyon ekonomisi değişmedi; ücretsiz `FREE TEST / GET`
  akışı ve `EconomyChecksEnabled = false` korunuyor.
- 1920×1080 QA:
  `Assets/QA/PremiumVisuals/2026-08-30_SecondFloorHome2_NookStudioStore.png`.
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne, aktif Living Room ve Camera/AudioListener/EventSystem
  **1/1/1** geri yüklendi. Git commit/push yok.

### Checkpoint — 30 Ağustos 2026 (Bathroom görsel kalite ve başlangıç yerleşimi)

- Tam 10 ürünlü Bathroom sahnesi gerçek oyun kamerasında birlikte incelendi.
  Çamaşır sepetinin `z=-1.8` başlangıcı ön-sol kamera sınırında ürünü kesiyor;
  sol duvardaki Towel Storage ve Bubble Wall Mirror'ın `90°` yönü de ön yüzleri
  duvara çevirerek havlu rafını büyük koyu bir arka blok gibi gösteriyordu.
- Laundry Hamper `z=-1.05` konumuna alındı. Towel Storage ve Wall Mirror sol
  duvarda `270°` ile odaya döndürüldü. Havlu rafının büyük koyu arkalığı pearl
  yüzeye, lilac yan çerçeveye ve coral üst şeride geçirildi; ürünün renkli havlu
  katları yeniden okunur hale geldi.
- Bathroom katalog testi bu üç başlangıç değerini ve kanonik `FLOOR/WALL`
  eşleşmesini birlikte kilitler. Fiyat, sahiplik, collection ve save v11
  değişmedi; ücretsiz ekonomi QA akışı korunuyor.
- 1920×1080 karşılaştırma:
  `Assets/QA/PremiumVisuals/2026-08-30_BathroomVisualPass_Before.png` ve
  `Assets/QA/PremiumVisuals/2026-08-30_BathroomVisualPass_Final.png`.
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne, aktif Living Room ve Camera/AudioListener/EventSystem
  **1/1/1** geri yüklendi. Git commit/push yok.

### Checkpoint — 30 Ağustos 2026 (Kitchen görsel kalite ve başlangıç yerleşimi)

- Tam 10 ürünlü Kitchen sahnesi gerçek oyun kamerasında birlikte incelendi.
  Rainbow Fruit Basket'ın `(-3.3, -1.95)` başlangıcı ürünü ön-sol kamera
  sınırında neredeyse tamamen kesiyor; Candy Pantry Shelf'ın `0°` yönü de sol
  duvardan odaya dik çıkarak lavabo önü ve geçiş alanını daraltıyordu.
- Fruit Basket `(-2.9, 0, -0.55)` konumuna alındı; ürün bütünüyle görünür ve
  joystick yüzeyinden ayrıdır. Pantry Shelf `(-3.45, 0, 0.95)` konumunda `270°`
  ile sol duvara paralel ve ön yüzü odaya bakacak şekilde yerleşir.
- Kitchen katalog testi iki başlangıç değerini ve kanonik `FLOOR/WALL`
  eşleşmesini birlikte kilitler. Fiyat, sahiplik, collection ve save v11
  değişmedi; ücretsiz ekonomi QA akışı korunuyor.
- 1920×1080 karşılaştırma:
  `Assets/QA/PremiumVisuals/2026-08-30_KitchenVisualPass_Before.png` ve
  `Assets/QA/PremiumVisuals/2026-08-30_KitchenVisualPass_Final.png`.
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne, aktif Living Room ve Camera/AudioListener/EventSystem
  **1/1/1** geri yüklendi. Git commit/push yok.

### Checkpoint — 30 Ağustos 2026 (boş oda kararı + Kitchen sabit mimari v2)

- Önceki Bathroom/Kitchen koordinat değerlendirmesi ürün kararıyla yeniden
  sınıflandırıldı: sekiz oda da satın alınabilir katalog eşyası olmadan başlar.
  `DefaultPosition` değerleri yalnız teknik geri dönüş güvenliğidir; konum cilası
  artık görsel kalite ilerlemesi sayılmaz.
- Yeni sahiplenilen her yerleştirilebilir ürün önce `STORE` durumunda kaydedilir.
  SHOP ürünü yerleştirme önizlemesi boyunca geçici olarak gösterir; onaylanan
  geçerli konum ürünü odada bırakır, iptal ürünü tekrar depoya alır. Modern TV
  veya kitap setinin görünür desteği yoksa yerleştirme önce TV ünitesine ya da
  kitaplığa yönlendirilir.
- Kitchen boş kabuğu ürünlerden bağımsız olarak geliştirildi: `Sunshine Checker`
  zemin daha açık pearl/butter malzemelere, ince derz ve gold çevre inlay'ine;
  duvarlar pastel wainscot, pearl/gold picture rail ve crown moulding'e geçti.
  Katmanlı sunrise pencere, çerçeveli pati madalyonu, derinlikli kapı ve daha
  ölçülü yön/fill ışıkları eklendi. On ROOM ürününün tamamı authoring halinde
  görünmez kalır.
- 1920×1080 boş oda QA:
  `Assets/QA/PremiumVisuals/2026-08-30_KitchenEmptyShell_Final1920.png`.
  EditMode **316/316**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne, aktif Living Room ve Camera/AudioListener/EventSystem
  **1/1/1** geri yüklendi. Git commit/push yok.

> Kullanıcı kararı (20 Ağustos 2026): mobil performans ve uzun oturum testleri ara çalışma önerilerine alınmaz. Yalnız kullanıcı “her şey bitti, deneyelim” veya “her şey bitti, yayınlayalım” dediğinde yapılmadıkları yayın öncesi eksik kontrol olarak hatırlatılır.

> Kullanıcı kararı (21 Ağustos 2026): yeni diller oyun bitmeye yakın, bütün oyuncu
> metinleri kesinleşip **metin dondurma** yapıldıktan sonra topluca eklenir. Bu iş
> yayın öncesi zorunlu listeden çıkarılmaz; öncelik `ja-JP → ko-KR → zh-Hant →
> pt-BR → es-419`, sonra veriye göre Almanca/Fransızcadır.

Runner → Coin → Home Store → odada kalıcı açılım döngüsü çalışır durumda ve Home XP artık bu döngünün satın alma adımından besleniyor.

### Checkpoint — 30 Ağustos 2026 (sekiz oda boş kabuk görsel kalite paketi)

- Living Room, Bathroom, Kitchen, Bedroom, Garden, Balcony, Patio ve Second
  Floor tek rebuild zincirinde güncellendi. `HomeRoomShellVisualPolishBuilder`
  her sahneye katalog ürünlerinden bağımsız `FixedArchitecturePolish` katmanı
  ekler; bu katman yerleştirme collider'ı üretmez.
- İç odalarda pearl/gold crown ve picture rail, inset zemin çerçevesi ve oda
  kimliği motifleri; dış odalarda eşik/inlay, yol/bordür ve candy vurgu detayları
  eklendi. Bathroom/Bedroom/Second Floor paletleri pearl pastel aileye yaklaştı;
  Garden/Balcony/Patio gündüz okunaklılığı artırıldı ve sekiz odadaki sert gölge
  gücü yumuşatıldı. Kanonik shell, kamera, CatRoot ve ürün footprint'leri değişmedi.
- Bütün satın alınabilir `StoreProductDisplay` renderları taze oda sahnesinde
  görünmezdir. Yeni `EveryRoomScene_StartsWithoutCatalogFurnitureAndKeepsItsFixedPolish`
  testi sekiz sahnenin sabit mimari katmanını ve boş katalog başlangıcını kilitler.
- Sekiz 1920×1080 boş oda QA görüntüsü
  `Assets/QA/PremiumVisuals/2026-08-30_*EmptyShell_Final1920.png` altında;
  home/store/diamond ve Runner welcome/gameplay kontrol görüntüleri aynı klasörde
  `2026-08-30_AllRooms_*_Final1920.png` adlarıyla saklandı.
- Son doğrulama: EditMode **317/317**, PlayMode **10/10**, validator **0/0**,
  Console temiz; kanonik `GameScene + CatHome_UI + LivingRoom_Level01`, aktif
  Living Room ve Camera/AudioListener/EventSystem **1/1/1**. Git commit/push yok.

### Checkpoint — 30 Ağustos 2026 (eşya görsel kalite dalgası 3 başladı: premium model hattı + BalconySunAwning)

- Dalga 3 için envanter çıkarıldı: `Docs/ITEM_ART_INVENTORY.md`. 80 oda ürünü üç
  kaynağa ayrılıyor — **27 premium FBX**, **10 LowPolyLivingRoomPack prefab'ı**
  (Living Room'un tamamı), **43 prosedürel primitive**. Malzeme ailesi zaten
  tutarlı: tüm ürünler paylaşılan 15 `CH_*` materyalini kullanıyor ve eksik mağaza
  ikonu yok. Boşluk malzemede değil, geometride/silüette.
- Öncelik sırası: A) 9 büyük silüetli prosedürel ürün, B) 25 orta prop,
  C) 9 düz halı/ip ürünü (yeni model gerekmez). Ayrı başlık olarak Living Room'un
  pack prefab ailesi `CH_*` dışında kalıyor; karar bekliyor.
- Yeni premium model hattı: `ArtSource/Blender/PremiumFurniture/` altında
  `premium_kit.py` (bevel'lı primitive yapı taşları, sarkmalı kumaş üreteci,
  `CH_*` palet, `drop_to_floor`, FBX export) ve `premium_preview.py` (modelin
  kendi bounds'una göre çerçevelenen hero + ortografik ön görünüm + düz siluet).
  Her ürün `blender --background --factory-startup --python <script>` ile ayrı
  process'te üretilir; açık bir Blender oturumuna dokunmaz. Onay render'ları
  `ArtSource/Blender/PremiumFurniture/Previews/` altında.
- **Blender 5.2 eksen notu:** exporter `axis_up="Y"` dönüşümünü kendi yapıyor.
  Banyo kitindeki `+90° X` ön-rotasyonu bu sürümde modeli Unity'de sırtüstü
  yatırıyor. Yeni hat `join_fixture` içinde ön-rotasyon uygulamaz.
- İlk ürün tamam: **BalconySunAwning** prosedürel 7 kutudan premium modele geçti
  (7.434 tri) — sarkmalı çizgili kumaş, krem gövde + gold trim/uç kapakları,
  kumaş altına gizlenmiş destek kolları, gold ön ray, aqua hem piping, kumaş
  şeritleriyle hizalı 7 fistolu volan ve ortada gold pati imzası.
- İki yerleşim hatası düzeltildi: model `facesBackward` listesine eklendi (montaj
  gövdesi artık duvara bakıyor) ve ürün asılı kayda çevrildi —
  `height .71f`, `hungHeight 2.24f`. Balkon kapısı 0–2.20 arası olduğu için tente
  artık kapının üstünde, y 2.24–2.95 aralığında duruyor. Footprint 2.4 × 1.1,
  WallEdge, 180° yaw, fiyat, ownership ve save kimlikleri değişmedi.
- Prosedürel `BuildBalconySunAwningVisual` fallback'i duruyor ama aynı ters yönü
  kullanıyor. Diğer WallEdge prosedürel ürünlerde de aynı konvansiyon şüphesi var
  (`BalconyRailingFlowers`, `BalconyHerbShelf`, `PatioHerbTrough`,
  `PatioPergolaArch`, `LoftWallGallery`, `BedroomDreamArt`); sıraları geldiğinde
  tek tek doğrulanacak.
- `Balcony_Level01` içine önceki QA denemesinden kaçak `BalconySunAwning_Premium(Clone)`
  yazılmıştı; sekiz oda tarandı, tek artık silindi ve sahne kaydedildi.
- QA görüntüsü: `Assets/QA/PremiumVisuals/2026-08-30_BalconySunAwning_InRoom_QA.png`.
- Son doğrulama: EditMode **317/317**, Console temiz, kanonik
  `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room,
  Camera/AudioListener/EventSystem **1/1/1**. Git commit/push yok.

### Checkpoint — 2 Eylül 2026 (Öncelik A #2: PatioPergolaArch premium)

- İkinci Öncelik A ürünü tamam: **PatioPergolaArch** 11 prosedürel primitive'den
  premium modele geçti (Blender 11.758 yüz / Unity 20.852 üçgen). Yapı: kaide +
  pah + iki altın bilezik + başlıklı dört sütun, diz payandaları, çift başlık
  kirişi ve altın uç kapakları, beş mertek + altın uç boncukları, dört aqua lata,
  `sheet` ile üretilmiş gerçek kavisli ön kemer + altın kakma şerit, 45° elmas
  kilit taşı üstünde altın pati imzası, iki ön sütunda sarmal asma (sap boncuğu,
  yaprak, beş yapraklı çiçek) ve latayı aşan yeşillik.
- Ölçü katalog sözleşmesine sığdırıldı: 2.37 × 1.84 × 0.98 (limit 2.4 × 1.85 × 1.0),
  mesh tabanı y = 0. Footprint, `Vector3(0, 0, 2.2)`, 180° yaw, WallEdge, fiyat,
  ownership ve save kimlikleri değişmedi.
- Yön konvansiyonu doğrulandı: duvar tarafı +Z, süslü taraf −Z. Ürün
  `facesBackward` listesine eklendi; instance yaw 180 + model 180 = net kimlik,
  dolayısıyla süsleme odaya bakıyor. Submesh ağırlık merkezleriyle ölçüldü
  (CH_Pink avgZ −0.367, CH_AquaBright avgZ 0.000). Böylece 30 Ağustos'ta açık
  kalan "diğer WallEdge ürünleri de ters mi" şüphesinin `PatioPergolaArch`
  maddesi kapandı — aynı konvansiyonu kullanıyor.
- Prosedürel `BuildPatioPergolaArchVisual` fallback olarak duruyor; Patio dispatch
  zaten önce `TryBuildPremiumFurnitureVisual`'ı deniyor.
- Hat düzeltmeleri: `premium_kit.py` paletine `CH_TealLight` + `CH_Teal` eklendi ve
  `cube()` artık `segments` parametresi alıyor (yapı pahı 5 → 3, mesh yarıya indi,
  awning betiği etkilenmedi). `premium_preview.py` **gerçek bir hata** taşıyordu:
  Y-up modeller Z-up önizleme sahnesinde yatık render ediliyordu (bkz. eski
  `BalconySunAwning_Hero.png`); render sırasında geçici +90° X uygulanıyor, export
  yolu değişmedi.
- Onay render'ları: `ArtSource/Blender/PremiumFurniture/Previews/PatioPergolaArch_{Hero,Front,Canopy,Silhouette}.png`.
  Üç tur düzeltme gerekti: taşan ayak izi (2.52 → 2.37), kemerden kopuk altın ark,
  kirişe saplanan payandalar, tepede yüzen yaprak tabakları.
- Oda QA görüntüsü: `Assets/QA/PremiumVisuals/2026-09-02_PatioPergolaArch_InRoom_QA.png`.
  Oda sahneleri aynı dünya koordinatlarını paylaştığı için QA sırasında
  `LivingRoom_Level01` geçici kapatıldı, render sonrası kirletmeden geri açıldı.
- Son doğrulama: EditMode **317/317**, Console temiz, kanonik
  `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room, tek etkin
  kamera, kaçak `(Clone)` yok, hiçbir sahne dirty değil. Git commit/push yok.
- Açık kalan: mağaza ikonları premium modellerden sonra yenilenmiyor
  (`PatioPergolaArch_Icon.png` ve `BalconySunAwning_Icon.png` hâlâ 20 Ağustos
  tarihli prosedürel görünüm). Ayrı bir ikon tazeleme turu gerek.

### Checkpoint — 2 Eylül 2026 (Öncelik A #3: BedroomStarCanopy premium)

- Üçüncü Öncelik A ürünü tamam: **BedroomStarCanopy** 5 prosedürel primitive'den
  premium modele geçti (Blender 8.356 yüz / Unity 16.080 üçgen).
- **Form kararı:** ilk deneme kataloğun tarifini birebir izledi — taç halkası,
  tek merkez direk, sarkan kumaş. Onay render'ında bu net biçimde *abajur*
  okudu (koni + çubuk = lamba silueti). Katalog adı, id'si ve fiyatı korunarak
  form **yıldız çadırına** çevrildi: tepede kesişip karşı tarafa taşan dört
  altın direk, alt üçte ikiyi saran kumaş ve önde gerçek açık kapı. Kullanıcı
  onayı alındıktan sonra export edildi.
- İçerik: krem/lilac mat + puf kenar + pembe minder, lilac/aqua şeritli sargı,
  **beyaz iç astar** (açıklığın "içerisi" olarak okuması için), dar altın etek
  bordürü, kapı kenarlarında altın şerit bant + tutamak + yıldız, kapı boyu
  yıldız çelengi, tepe düğümünden sarkan yıldız, sargı üstünde dört aplike
  yıldız, minderde aqua yıldız yastık ve düz altın pati imzası.
- Yıldızlar küre yerine gerçek beş köşeli geometri (`star()` yardımcısı);
  direkler `to_track_quat` ile serbest yönlü silindir (`pole()` yardımcısı).
- Yerleşim: Bedroom modelleri `FitFixtureModel`'den geçtiği için mesh oransal
  yazıldı; prefab'ta model scale 0.98, bounds 1.00 × 1.49 × 1.00, merkez y 0.74.
  Oda kamerası (-1, 3, -5.5) +Z'ye baktığı ve ürün yaw 0 olduğu için açıklık
  -Z'ye yazıldı ve ürün **`facesBackward` listesine eklenmedi**. Submesh ağırlık
  merkezleri doğruladı: `CH_LemonBright` avgZ -0.082, `CH_Gold` avgZ -0.044.
  Footprint, konum, fiyat, ownership ve save kimlikleri değişmedi.
- Prosedürel `BuildBedroomStarCanopyVisual` fallback olarak duruyor.
- Hat düzeltmesi: `premium_preview.py` ortho kameraları en/boy oranını hesaba
  katmıyordu ve 1.5 m'lik modelin tepesini kesiyordu; `ortho_scale` artık
  çözünürlük oranıyla hesaplanıyor.
- Onay render'ları: `ArtSource/Blender/PremiumFurniture/Previews/BedroomStarCanopy_{Hero,Front,Silhouette}.png`.
- Oda QA görüntüsü: `Assets/QA/PremiumVisuals/2026-09-02_BedroomStarCanopy_InRoom_QA.png`.
  QA sırasında öğrenilen: sahnedeki mağaza ürünlerinin `VisualContent` çocuğu
  sahip olunmadığı için kapalıdır; oda render'ı almadan önce açılması gerekir.
- Son doğrulama: EditMode **317/317**, Console temiz, kanonik
  `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room,
  Camera/AudioListener/EventSystem **1/1/1**, kaçak `(Clone)`/QA nesnesi yok,
  hiçbir sahne dirty değil. Git commit/push yok.

### Checkpoint — 2 Eylül 2026 (yıldız çadırında uyuma — ilk eşya etkileşimi)

Soru: kedi çadırın içine girip yatabiliyor mu? Ölçüldü, hayır — üç ayrı engel vardı:

1. `AddProductCollider` her ürüne katı `BoxCollider` veriyor (çadırınki
   1.12 × 1.55 × 1.05); kedi kapıya değil görünmez kutuya çarpıyordu.
2. Prefab'ta hiç `CatActivity` yoktu; tüm katalogda yalnız `PlayTunnel` ve
   `FeatherToy` etkileşimli.
3. Collider açılsa bile sığmıyordu: kedinin `CharacterController`'ı dünya
   ölçüsünde 0.50 çap × 0.60 boy, çadır kedinin baş hizasında (y 0.60) sadece
   0.46 geniş ve mat eşiği 0.049 > `stepOffset` 0.005.

Çözüm `PlayTunnel` deseni: fizik kapatılıp kedi script ile içeri yürütülüyor.

- Yeni `CanopyNapActivity` (`Assets/Scripts/Activities/`): kapı noktasına yürü,
  içeri gir, kapıya dön, kıvrıl, nefes alıp verme salınımıyla uyu, geri çık.
  Rutin fiziği geri vermeden **önce** kediyi footprint dışına çıkarır.
- `CatActivityKind.CanopyNap = 7` eklendi (sona eklendi, mevcut indeksler sabit).
  `QuestType` **yeni değer almadı**: şekerleme `QuestType.Sleep` sayıyor.
- Kilit yalnız mağaza sahipliği (`bedroom.star-canopy`), Bond eşiği 0 —
  `BondMilestoneService` kataloğu değişmedi.
- Enerji **harcanmıyor** (`EnergyCost` 0, yorgun kedi her zaman uyuyabilsin),
  bitişte `EnergySystem.RestoreEnergy(22)` ile geri veriliyor. `RestoreEnergy`
  yeni ve tek işi bu; yataktaki uyku hâlâ `Update` içindeki saniyelik kazanımı
  kullanıyor. Enerji ≥ 92 iken aktivite "I AM WIDE AWAKE!" ile reddediyor.
- Ürün kutusu `PlayTunnel` gibi `isTrigger = true` oldu; girilebilir bir çadır
  katı kutu olarak kalamaz.
- `LevelContentValidator.ValidateBedroomRoom` artık çadır uyku aktivitesini ve
  onun doğru ürüne bağlı olduğunu arıyor.
- Testler: EditMode'da prefab sözleşmesi + `RestoreEnergy` sınırları;
  PlayMode'da `Bedroom_Level01` yüklenip gerçek kedi çadıra sokuluyor —
  yuvaya ulaşma, footprint dışına çıkma, fizik/ölçek/hareket kilidinin iadesi
  ve enerji kazancı doğrulanıyor. PlayMode testinde `IsMovementLocked` yerine
  `IsMovementPhysicallyLocked` kontrol edilir: tek sahne yüklemesinde başka
  sistemlerin input-kategori bloğu hâlâ açık kalıyor, aktivite yalnız fiziksel
  kilidin sahibi.

### Checkpoint — 2 Eylül 2026 (Öncelik A #4: PatioPorchSwing premium)

- Dördüncü Öncelik A ürünü tamam: **PatioPorchSwing** 8 prosedürel primitive'den
  premium modele geçti (Blender 10.912 yüz / Unity 21.072 üçgen), ölçü
  1.57 × 1.50 × 0.65 (limit 1.6 × 1.5 × 0.7), bounds merkezi y 0.75, taban y 0.
- Envanterin "zincir/minder zayıf" notu iki asıl iş oldu:
  **zincir gerçek halka** (her yan için tepe kancasından inen ana zincir, ayırıcı
  halka, koltuğun ön/arka köşesine giden iki kol; halkalar dönüşümlü 90° çevrili
  ve alt uçta altın göz + braket donanımına bağlanıyor) ve **minder** (çıtalı
  oturak + altın biye + tuftlu sırt minderi + iki atkı yastığı).
  Ayrıca Z'de açılan A-ayak, çapraz bağ, altın bilezik/ayak pedleri, uç kapaklı
  üst kiriş, yuvarlak uçlu kolçaklar, sırt direklerinde altın finial ve sırt
  minderinde altın pati imzası.
- **Yön hatası düzeltildi:** prosedürel sürümde sırt minderi -Z'deydi, yani ürün
  oyuncuya sırtını dönüyordu. Premium model oturağı -Z'ye bakacak şekilde
  yazıldı; yaw 0 olduğu için `facesBackward` gerekmedi. Submesh ağırlık
  merkezleriyle doğrulandı: `CH_MintBright` (sırt minderi) avgZ +0.144,
  `CH_Pink`/`CH_AquaBright` (atkı yastıkları) +0.100/+0.089.
- Prosedürel `BuildPatioPorchSwingVisual` fallback olarak duruyor — aynı ters
  yönü kullanmaya devam ediyor, FBX yoksa devreye girer.
- Hat: `premium_kit.strut()` (iki nokta arası yönlendirilmiş silindir) yıldız
  çadırındaki yerel `pole()`'den ortak yardımcıya taşındı; çadır betiği
  güncellendi ve birebir aynı mesh üretiyor (8.356 yüz, aynı ölçü — doğrulandı).
  `kit.torus()` artık `major_segments`/`minor_segments` alıyor; zincir halkaları
  12×6 ile ucuz kalıyor.
- Onay render'ları: `ArtSource/Blender/PremiumFurniture/Previews/PatioPorchSwing_{Hero,Front,Silhouette}.png`.
  Üç tur: oturak çok yüksekti (çerçevenin yarısı boş, 0.80 → 0.62), zincirler
  kolçakların içinde kalıyordu (x 0.52 → 0.60), zincir uçları boşlukta bitiyordu
  (donanım eklendi), kolçak ucundaki altın halka kopuk duruyordu, sırt direkleri
  güdük çıkıyordu.
- Oda QA görüntüsü: `Assets/QA/PremiumVisuals/2026-09-02_PatioPorchSwing_InRoom_QA.png`.
- Son doğrulama: EditMode **319/319**, `LevelContentValidator` 0 hata/0 uyarı,
  Console temiz, kanonik `GameScene + CatHome_UI + LivingRoom_Level01`, aktif
  Living Room, Camera/AudioListener/EventSystem **1/1/1**, kaçak nesne yok,
  hiçbir sahne dirty değil. Git commit/push yok.

### Checkpoint — 2 Eylül 2026 (salıncakta sallanma — ikinci eşya etkileşimi)

Soru: kedi salıncağın üstüne atlayabilir mi? Ölçüldü, hayır — üç engel:
zıplama girdisi yok (`CatMovement` dikey hızı sadece yerçekimi), `stepOffset`
dünya ölçüsünde 0.005, ürün kutusu katı. Çözüm yine `PlayTunnel` deseni.

- Yeni `SwingRideActivity`: kedi mount noktasına yürüyor, yay çizerek oturağa
  zıplıyor, salıncak yumuşak giriş/çıkış zarfıyla ±9° sallanıyor, sonra inip
  bindiği yere dönüyor. `CatActivityKind.SwingRide = 8`, `QuestType.SwingRide = 13`
  (ikisi de sona eklendi; `DailyRetentionService` bilinmeyen tipi eşleştirmiyor).
  Kilit yalnız mağaza sahipliği, enerji **6 harcıyor** (uyku değil, oyun),
  bitişte `ProgressionService.AddBondXp(4)` — quest/ekonomi config'ine
  dokunmadan gerçek ödül. Ürün kutusu `isTrigger`.
- **Model ikiye ayrıldı.** Oturak, zincirler ve minderler birlikte sallanmalı,
  ama tek mesh'te bu mümkün değildi. Şimdi iki ayrı **tek nesneli** FBX:
  `PatioPorchSwing_Premium.fbx` (iskelet, 2.554 yüz) ve
  `PatioPorchSwingSeat_Premium.fbx` (oturak, 8.358 yüz), ortak orijinde.
  Builder `SwingPivot`'u üst kirişin altına (y 1.39) kuruyor ve oturağı doğrudan
  onun altına instantiate ediyor.
- **İki gerçek tuzak ölçülerek bulundu:**
  1. *Tek FBX'te iki nesne olmaz.* Blender çok nesneli export'ta Y-up dönüşümünü
     mesh'e gömmüyor, her çocuğa -90° X transform olarak yazıyor; ürün sırt üstü
     import oldu (bounds 1.568 × **0.650 × 1.498**). İki ayrı tek nesneli export
     sorunu kaldırdı (bounds 1.568 × 1.498 × 0.650).
  2. *Prefab instance'ının çocuğu yeniden ebeveynlenemiyor.* `bench.SetParent(pivot)`
     editörde sessizce hiçbir şey yapmadı; hiyerarşi çıktısıyla kanıtlandı.
     Bu yüzden oturak artık pivot'un altına doğrudan instantiate ediliyor.
- **Kediyi parent etmek editörü kilitliyor.** İlk sürüm kediyi sürüş boyunca
  `SwingPivot`'a parent ediyordu; PlayMode testi 2/2 editörü kilitledi, biri taze
  restart sonrası. Minimal `PatioSceneProbeTests` sahnenin suçsuz olduğunu
  gösterdi. Parent etmek kaldırılıp kedi her kare `SwingSeatPoint`'e (pivot'un
  altında, sallanmayla birlikte dönüyor) yazılınca kilitlenme gitti ve test
  baştan sona koştu. Kural `AGENTS.md`'ye yazıldı.
- Doğrulanan davranış (kilitlenmeyen tam koşuda hepsi geçti): sahiplik kilidi,
  başlarken `CharacterController` kapanması, oturağın gerçekten sallanması
  (> 3°, 9° sınırında), kedinin oturağa ulaşması, **sallanma boyunca oturağa
  kilitli kalması (< 0.05 m sapma)**, sürüşün kendiliğinden bitmesi, pivot'un düz
  kalması, kedinin bindiği yerden inmesi, fizik/ölçek/hareket kilidinin iadesi,
  Bond +4 ve enerji harcanması.
- Görsel doğrulama: `Assets/QA/PremiumVisuals/2026-09-02_PatioPorchSwing_Rocked_QA.png`
  (pivot 8°'ye kurulu; oturak, zincirler, minderler ve pati birlikte eğiliyor,
  iskelet sabit).

**Açık madde:** `SwingRideTests` içindeki tek assertion düzeltildi ama
koşturulamadı. Patlayan assertion kodda değil testteydi — küçük açılı sarkaç
çoğunlukla yatay hareket ediyor (Z ±0.10 m), dikey sadece ~11 mm; yükseklik
ölçmek yanlış metrikti. Yerine yön bağımsız `maxRideTravel` kondu. Bu, zaten
geçen "oturağa kilitli kalma" assertion'ının daha zayıf bir ölçümü.
EditMode **319/319**, `LevelContentValidator` 0 hata/0 uyarı, prefab (2 renderer,
bounds 1.568 × 1.498 × 0.650, pivot bağlı), kanonik sahne yığını, 1/1/1 ve oda QA
render'ı bu turda doğrulandı.

`SwingRideTests`'i **MCP üzerinden koşmayı bırak.** Dört ayrı editör
yeniden başlatmasında denendi: bir kez baştan sona koştu (yalnız yukarıdaki
metrik assertion'ı patladı), iki kez editörü kilitledi, iki kez
"tests did not start within timeout" verdi. Sahne, `refresh_unity` ve artık
`InitTestScene` dosyaları eleme yoluyla suçsuz bulundu; kalan sebep MCP-PlayMode
köprüsünün bu projedeki kararsızlığı. Doğrulama **Unity Test Runner penceresinden
elle** yapılmalı: `Window > General > Test Runner > PlayMode > SwingRideTests`.

### Checkpoint — 2 Eylül 2026 (Öncelik A #5: KitchenSinkCabinet premium)

- Beşinci Öncelik A ürünü tamam: **KitchenSinkCabinet** 9 prosedürel primitive'den
  premium modele geçti (Blender 8.490 yüz / Unity 16.612 üçgen), yazım ölçüsü
  1.65 × 1.15 × 0.76. Kitchen `FitFixtureModel`'den geçtiği için prefab'ta model
  scale 0.943, bounds 1.556 × 1.083 × 0.720, merkez y 0.541 (taban y 0).
- Katalogdaki **1.16 yükseklik** asıl kısıttı: tezgâh + musluk insan ölçüsüyle
  bu kutuya sığmıyor. Tezgâh 0.86'ya kondu, kalan 0.30 gooseneck'e ayrıldı.
- İçerik: girintili krem süpürgelik + altın reveal, krem gövde, evye açıklığı
  gerçek delik olan tezgâh, aqua duvarlı + teal tabanlı çukur evye + altın
  süzgeç, silindir dilimlerinden kurulmuş gooseneck musluk + kol, çekmece +
  altın çubuk kulp, kabartma panelli iki shaker kapak + ince altın kulplar,
  kulpa asılı dalgalı çay bezi, sabunluk ve ot saksısı, sağ kapak panelinde
  altın pati imzası.
- Yön: kapaklar -Z'ye yazıldı, yaw 0, `facesBackward` **yok**. Submesh ağırlık
  merkezleriyle doğrulandı (`CH_Cream` -0.170, `CH_Gold` -0.165, `CH_White`
  -0.113, `CH_CoralBright` -0.195; evye `CH_Teal` 0.000 / `CH_AquaBright` +0.011
  ortada). Not: eski Kitchen premium modelleri ters konvansiyonda — ön yüzü +Z'ye
  yazılıp `facesBackward` ile çevriliyorlar; iki yol da aynı sonucu veriyor.
- Prosedürel `BuildKitchenSinkCabinetVisual` fallback olarak duruyor.
- Onay render'ları: `ArtSource/Blender/PremiumFurniture/Previews/KitchenSinkCabinet_{Hero,Front,Silhouette}.png`.
  Dört tur, üçü gerçek hata: **musluk hiç yoktu** (`gooseneck()` yazılmış ama
  çağrılmamış), **evye dolu görünüyordu** (çukur tezgâhın altında kalıyordu,
  duvarlar tezgâh yüzeyine kadar uzatıldı), **kapak paneli görünmüyordu** (gömük
  panel kapak yüzünün arkasındaydı, kabartmaya çevrildi). Ayrıca musluk kavisi
  tırtıklıydı (küre dizisi yerine silindir dilimleri), tezgâh dikişleri
  belirgindi (pah inceltildi) ve ayak izi 0.78'e taşmıştı.
- Oda QA görüntüsü: `Assets/QA/PremiumVisuals/2026-09-02_KitchenSinkCabinet_InRoom_QA.png`.
- Son doğrulama: EditMode **319/319**, `LevelContentValidator` 0 hata/0 uyarı,
  Console temiz, kanonik `GameScene + CatHome_UI + LivingRoom_Level01`, aktif
  Living Room, Camera/AudioListener/EventSystem **1/1/1**, kaçak nesne yok,
  hiçbir sahne dirty değil. PlayMode koşulmadı (bu değişiklikler EditMode
  kapsamında). Git commit/push yok.

### Checkpoint — 2 Eylül 2026 (kapsam kararı + Living Room ilk parti)

**Kapsam kararı:** 80 oda ürününün tamamı premium mobilya diline getirilecek.
Sıra **oda oda**, Living Room'un yedi pack prefab'ı premium FBX'e taşınacak ve
reçeteden önce yapılmış 26 premium model de yenilenecek.
Dil sözleşmesi `Docs/PREMIUM_FURNITURE_LANGUAGE.md` olarak yazıldı; `AGENTS.md`
oraya işaret ediyor. Envanter oda oda yeniden kuruldu (5 bitti, 75 kaldı).

Envanterdeki bir hata düzeltildi: Living Room'un **on ürünü de** pack'e dayanıyor.
Katalogda `Generated(...)` görünen üçünün (`GameConsoleSet`, `SpeakerSystem`,
`ColorfulBookSet`) da özel kurucuları LowPolyLivingRoomPack'ten besleniyor.

**Living Room parti 1 — TallBookshelf + ClassicArmchair**

- `TallBookshelf` 4.153 yüz, 1.60 × 1.85 × 0.63. Krem gövde, altın köşeli
  süpürgelik, lilac arka panel, altın dudaklı dört raf, korniş + altın bant,
  taban bölmesinde yaslanan kitaplar ve saksı, süpürgelikte altın pati.
- `ClassicArmchair` 5.014 yüz, 1.35 × 1.12 × 1.07. Altın ayaklı torna bacaklar,
  döşemeli kaide, altın biyeli coral oturak, gerçek rulo kolçaklar (silindir +
  küre kapak + altın spiral), eğimli düğmeli sırt, aqua atkı yastığı, altın pati.
- **Sert kısıt:** `ColorfulBookSet` kitaplığın köküne y 0.65/1.05/1.45, z +0.2
  konumlarına yapışıyor. Raf yüzeyleri 0.638/1.038/1.438'e yazıldı, üst sıraya
  0.32 baş boşluğu bırakıldı. Import sonrası ölçüldü: kitap seti y 0.638..1.687,
  z 0.084..0.310 — ilk denemede raflar 0.26'da bitiyordu ve kitapların ön 5 cm'i
  boşlukta kalıyordu; raflar ön kenara genişletildi.
- **Katalog cerrahisi gerekmedi.** `StoreProductContentBuilder`'a
  `TryBuildPremiumRoomProductPrefab` eklendi: pack kaynağını yüklemeden önce
  premium FBX deneniyor, yoksa pack varlığı fallback olarak kalıyor. Böylece
  footprint, `VisualScale`, `VisualOffset`, fiyat ve save kimlikleri hiç
  değişmedi. Kalan 8 Living Room ürünü de bu yoldan geçecek.
- Yön: kitaplık yaw 90 ile sol duvarda, açık yüzü **+Z**'ye yazıldı,
  `facesBackward` yok. Koltuk yaw 0, ön yüzü -Z, `facesBackward` yok.
- Hat düzeltmeleri: `premium_kit.report_parts()` (ölçü taşınca hangi parçanın
  taşırdığını isimlendiriyor — kitaplıkta `PawToe=1.885` ve `PawPad=0.361` diye
  tespit edildi) ve `premium_preview.render_views(..., front_plus_z=True)`
  (ön yüzü +Z olan duvar ünitelerinin arkasını göstermemesi için), artı kamera
  mesafesi/lensi 1.85'lik modeller kadraja sığacak şekilde ayarlandı.

**Bulgu — sahnedeki varsayılan konumlar çakışıyor.** Living Room'da kitaplık
(x -3.56..-2.94) ile koltuk (x -3.45..-2.15) 0.51 m örtüşüyor. Bu yeni
modellerden gelmiyor: pack koltuk 1.30 genişliğinde, benimki 1.35, örtüşme pack
sürümde de vardı. `LevelContentValidator` bunu hata saymıyor ve oyuncu eşyayı
zaten taşıyabiliyor, ama iki ürün birden satın alındığında iç içe duruyorlar.
Varsayılan konumların ayrıştırılması ayrı bir karar. QA görüntüsü koltuk geçici
1.35 kaydırılarak alındı (sahneye yazılmadı).

- Oda QA görüntüsü: `Assets/QA/PremiumVisuals/2026-09-02_LivingRoom_Batch1_QA.png`.
- Doğrulama: EditMode **319/319**, `LevelContentValidator` 0 hata/0 uyarı,
  Console temiz, kanonik `GameScene + CatHome_UI + LivingRoom_Level01`, aktif
  Living Room, 1/1/1, kaçak nesne yok, hiçbir sahne dirty değil. Git yok.

### Checkpoint — 2 Eylül 2026 (Living Room parti 2: FloorLamp + TallHouseplant + TvUnit)

| Model | Yüz | Yazım ölçüsü | Katalog kutusu |
|---|---|---|---|
| FloorLamp | 3.996 | 0.70 × 1.84 × 0.70 | 0.7 × 1.85 × 0.7 |
| TallHouseplant | 7.426 | 0.82 × 1.25 × 0.88 | 1.0 × 1.25 × 1.0 |
| TvUnit | 4.596 | 1.82 × **0.53** × 0.60 | 1.82 × 0.58 × 0.58 |

- **TvUnit'te sert kısıt.** `ModernTelevision` bu üniteye `ProductSurfaceOnly` ile
  local (0, 0.62, 0) noktasından bağlanıyor ve TV mesh'i kendi origin'inin 0.088
  altında başlıyor, yani ayağı 0.532'ye iniyor. Ünitenin üst yüzeyi kataloğun
  0.58 tavanına değil **0.53**'e kondu; import sonrası doğrulandı (prefab üstü
  0.530, TV ayağı 0.532). Pack ünitesi 0.517'deydi ve TV 1.5 cm havada duruyordu.
- İçerik: lambada `revolve` ile gerçek konik abajur, altın çemberler, sarkan
  boncuklu çekme zinciri, kaidede yatık altın pati. Saksıda `revolve` konik
  gövde, altın ağız/bant, kendi sapları üstünde eğik yapraklar. TvUnit'te altın
  ayaklı süpürgelik, kabartma panelli iki çekmece, gerçek açık orta raf (lilac
  arka, altın dudaklı raf, kitaplar, kâse) ve sağ çekmecede altın pati.
- Yön: lamba ve saksı yaw 0 (ön yüz -Z), TvUnit yaw 90 (ön yüz +Z, sol duvar).
  Üçü de `facesBackward` almadı. Üçü de `TryBuildPremiumRoomProductPrefab`
  yolundan geçiyor, katalog girdilerine dokunulmadı.
- Hat kazanımları: **`kit.revolve()`** (dönel yüzey — saksı, abajur, ileride vazo
  ve kova) ve **`kit.sphere(rotation=)` + `kit.aim_euler(yaw, droop)`**.
  İkincisi gerçek bir tuzağı çözüyor: Blender'ın XYZ euler sırası (Rz·Ry·Rx)
  yüzünden tuple'a yazılan bir eğim, her parçayı yaw'ından bağımsız aynı dünya
  ekseninde eğiyor; kuaterniyon bileşimi eğimi parçanın kendi eksenine kilitliyor.
- Üç tur, ikisi gerçek hata: (1) saksı yaprakları çubuk üstünde **düz lolipop**
  okuyordu — eğim yoktu, saplar kalın ve kremdi; eğim + uç lobu + ince yeşil sap
  ile düzeldi ve 1.08'e taşan ayak izi 0.88'e çekildi. (2) TvUnit'te derinlik
  taşmasını kırparken **çekmece yüzleri gövdenin içine gömüldü**, hero düz kutu
  çıktı; gövde 0.50'ye sığlaştırıldı, sonra orta raf da görünmüyordu — gövde tek
  dolu kutuydu, ikiye bölünüp boşluk gerçek açıklık oldu.
- Oda QA görüntüsü: `Assets/QA/PremiumVisuals/2026-09-02_LivingRoom_Batch2_QA.png`
  (TV üniteye oturmuş, kitap seti raflarda).
- Doğrulama: EditMode **319/319**, `LevelContentValidator` 0 hata/0 uyarı,
  Console temiz, kanonik sahne yığını, aktif Living Room, 1/1/1, kaçak nesne yok,
  hiçbir sahne dirty değil. Git yok.
- Living Room: **5 bitti, 5 kaldı** (ModernTelevision, SpeakerSystem,
  ModernPainting, GameConsoleSet, ColorfulBookSet).
- Not: varsayılan konum çakışması TvUnit ile kitaplık arasında da var
  (z 0.25..0.49, 0.24 m). Parti 1'de raporlanan sorunun aynısı, karar bekliyor.

### Checkpoint — 2 Eylül 2026 (Living Room parti 3: ModernTelevision + SpeakerSystem + ModernPainting)

| Model | Yüz | Yazım ölçüsü | Katalog kutusu |
|---|---|---|---|
| ModernTelevision | 2.330 | 1.52 × 1.02 × 0.16 | 1.52 × 1.02 × 0.18 |
| SpeakerSystem | 5.000 | 1.18 × 0.64 × 0.50 | 1.18 × 0.66 × 0.58 |
| ModernPainting | 2.822 | 1.10 × 0.82 × 0.10 | 1.1 × 0.82 × 0.2 |

Üçünün de ayrı bir bağlanma kısıtı vardı:

- **TV origin'i.** `HomeRequiredProductAttachment` TV'yi üniteye local (0, 0.62, 0)
  ile bağlıyor ve pack modeli kendi origin'inin 0.088 **altında** başlıyordu.
  Bu modelde `drop_to_floor` bilerek kullanılmadı; mesh -0.088'e indirildi ve
  betik her koşuda tabanı yazdırarak doğruluyor. Prefab'ta bounds min y -0.088,
  parti 2'deki TvUnit'in 0.53'lük üst yüzeyine tam oturuyor.
- **Tablo `VisualOffset`'i.** Katalog görseli y 1.35'e kaldırıyor. Premium sarmalayıcı
  bunu uygulamıyordu — `TryBuildPremiumRoomProductPrefab` artık `VisualOffset`'i
  model çocuğuna uyguluyor (collider `VisualContent` üzerinde kalıyor, jenerik
  pack yolunun yaptığının aynısı). Prefab'ta model localY 1.350, dünyada
  y 1.35..2.17.
- **Hoparlörün özel builder'ı.** `BuildSpeakerSystemPrefab` dispatch'te erken
  dönüyordu, premium dal ona hiç ulaşmıyordu. Artık önce premium deneniyor,
  yoksa pack birleştirmesi fallback kalıyor.

İçerik: TV'de krem çerçeve, gömük ekran, ekranda pastel kompozisyon, altın çıta,
ayak ve pati. Hoparlörde iki krem kabin + `revolve` ile **gerçek çukur koniler**
(altın çember, aqua göbek), amfi (ekran, VU çubukları, altın düğmeler), altın
kaideler. Tabloda dört ray ile gerçek gömük tuval, krem paspartu, pastel
kompozisyon, köşede pati.

**Gerçek hata:** hoparlörde koniler ve altın çıta kabinin **9 cm önünde**
duruyordu — `FRONT_Z` amfi kabinine göre hesaplanmıştı (0.44 derin), hoparlör
kutusu ise 0.30. `AMP_FRONT_Z` / `SPEAKER_FRONT_Z` ayrıldı. Ayrıca `CH_Screen`
palete eklendi ve tabloda yıldızlar güneşin arkasında kalıyordu.

- Oda QA görüntüsü: `Assets/QA/PremiumVisuals/2026-09-02_LivingRoom_Batch3_QA.png`.
- Doğrulama: EditMode **319/319**, `LevelContentValidator` 0 hata/0 uyarı,
  Console temiz, kanonik sahne yığını, 1/1/1, kaçak nesne yok, hiçbir sahne
  dirty değil. Git yok.
- Living Room: **8 bitti, 2 kaldı** (`GameConsoleSet`, `ColorfulBookSet`).
- Varsayılan konum çakışması listesine bir madde daha: tablo (z 0.15..1.25)
  kitaplığın (z 0.25..1.85) arkasında kalıyor.

### Checkpoint — 2 Eylül 2026 (Living Room tamamlandı: son parti + sahne demirbaşları)

**Living Room artık 10/10 premium.** Son parti `GameConsoleSet` (4.735 yüz) ve
`ColorfulBook` (784 yüz), ardından iki sahne demirbaşı `LivingSofa` (4.540) ve
`LivingCoffeeTable` (3.197). Pack bağımlılığı odadan kalktı.

- **`ColorfulBookSet` tek mesh yapılamazdı.** `HomeBookshelfBookSet` on ayrı
  Transform tutuyor, her kitabı tek tek rafa uçuruyor ve `ShelfRowCount`'u kitap
  hedef pozisyonlarından okuyor. Onun yerine **tek premium kitap** yazıldı ve
  `BuildBookshelfBookSetPrefab` onu on kez örnekleyip her birine ayrı bir `CH_*`
  sırt rengi veriyor (coral/aqua/mint/lilac/lemon/pink/tealLight/teal/orange/
  purple). Dizilim ve animasyon mantığına dokunulmadı. Doğrulandı: 10 kitap,
  3 sıra, 10 farklı renk. Kitap, pack kitabının 2.35 ölçekten sonraki boyutunda
  (0.075 × 0.287 × 0.226) yazıldı, `bookScale` premium varken 1 oluyor.
  Tabanı pack konvansiyonuna göre **-0.012**'ye indirildi; ilk denemede kitaplar
  raftan 12 mm havada duruyordu.
- **Konsol seti** ve **hoparlör** gibi özel kurucusu olan ürünlerde artık önce
  premium deneniyor, pack birleştirmesi fallback kalıyor.

**Sahne demirbaşları — ilk kez `LivingRoom_Level01.unity` kaydedildi.**
`Sofa_2Seat` ve `Table` `RoomFurniture` altında elle yerleştirilmiş pack
prefab'larıydı. Premium FBX'leri `_PremiumModel` adıyla aynı konum/rotasyonda
konuldu, `CH_*` materyalleri bağlandı, eskiler silindi.

- Kısıtlar ölçülerek karşılandı: `PremiumWorldVisualBuilder`'ın yazdığı
  `SofaComfortSet` yastıkları local (±0.48, 0.77, 0.20) ve `PearlSeatBand`
  (0, 0.34, 0.40) yerlerinde duruyor; sehpanın **tabla üstü tam 0.484**, çünkü
  `PearlRunner` 0.493'te ve merkez süsü 0.53'te. Betik her koşuda tablayı
  yazdırıp doğruluyor.
- **Ölçek tuzağı:** `RoomFurniture` zinciri **4× ölçekli**. `localScale = one`
  ile konan modeller dünyada 4× çıktı (koltuk 7.96 m). Ebeveynin `lossyScale`'i
  tersine çevrilerek düzeltildi; son ölçüler koltuk 1.990 × 1.119 × 1.050,
  sehpa 1.020 × 0.484 × 0.710, ikisi de taban y 0 (pack koltuk 0.05 gömülüydü).

**Düzeltilen gerçek hata — premium modeller düzleşiyordu.**
`RecolorLivingRoomFurniture` sahnedeki her renderer'ı **ada bakarak** tek düz
materyale eziyordu: "Armchair"→lilac, "Lamp"→gold, "Plant"→mint, "Tv"→aqua,
"Console"→aqua, "Sofa"→coral, "Table"→peach. Bu builder ilk çalıştığında bu
oturumun beş modeli tek renge inecekti. Artık `HomeProductPlacement` taşıyan
(tüm mağaza ürünleri) ve adı `_PremiumModel` ile biten hiçbir renderer'a
dokunmuyor.

- Oda QA görüntüsü: `Assets/QA/PremiumVisuals/2026-09-02_LivingRoom_Complete_QA.png`.
- Doğrulama: EditMode **319/319**, `LevelContentValidator` 0 hata/0 uyarı,
  Console temiz, kanonik sahne yığını, 1/1/1, kaçak nesne yok. Sahne diskten
  yeniden yüklenerek QA aktivasyonları atıldı ve mağaza ürünlerinin **görünür
  renderer sayısı 0** olarak doğrulandı. Git yok.

**Living Room 10/10.** Kalan üç varsayılan konum çakışması (kitaplık–koltuk
0.51 m, kitaplık–TV ünitesi 0.24 m, kitaplık–tablo) hâlâ açık; üçü de kitaplığı
içeriyor ve ölçerek topluca ayrıştırılmayı bekliyor.

### Checkpoint — 2 Eylül 2026 (Bathroom başladı: BathroomTub)

Living Room bittikten sonra sıradaki oda **Bathroom** (oyuncunun ilk açtığı oda,
3000 coin). Odanın on ürününün sözleşmesi çıkarıldı; dördü (Tub, VanitySink,
Toilet, Shower) reçeteden önce yapılmış premium modeller ve **ters
konvansiyonda** (ön yüz +Z + `facesBackward`), altısı prosedürel.

**BathroomTub** — 6.342 yüz, yazım ölçüsü 2.43 × 1.16 × 1.33
(limit 2.45 × 1.36 × 1.31). Bathroom `FitFixtureModel`'den geçiyor: prefab'ta
model scale 0.944, bounds 2.292 × 1.093 × 1.258.

- Yeni yapı tekniği: `shell()` — halka profillerinden lofting yapan süper-elips
  kesitli gövde. Küvet böylece yuvarlatılmış dikdörtgen okuyor, silindir yığını
  değil. Aynı teknik duş teknesi ve lavabo çanağında da kullanılacak.
- İçerik: krem dış kabuk + beyaz iç astar, aqua su + beyaz köpük, altın rulo
  kenar, dört altın ayak, kavisli altın musluk + iki kol, kenara asılı altın
  bantlı havlu, mint şişe, ön yüzde altın pati.
- **Yön konvansiyonu doğrulandı, mevcut `facesBackward` kaydı korundu.** Model
  yeni kurala göre ön yüzü -Z'ye yazıldı. Ölçüm: mesh'te havlu (`CH_CoralBright`)
  avgZ **-0.610**; ürün z 1.86'ya yaw 180 ile konduğunda havlu dünyada
  z **1.305**'e, yani odaya bakan tarafa düşüyor. Eski model ters konvansiyondaydı
  ama aynı `facesBackward` girdisi iki konvansiyon için de doğru sonucu veriyor,
  bu yüzden builder'da değişiklik gerekmedi.
- `BathroomTub_Premium.fbx` **eski dosyanın üzerine** yazıldı; eski premium küvet
  modeli artık yok.
- İki tur düzeltme: köpük suyu tamamen örtüyordu (azaltıldı, aqua göründü) ve
  kabuk orta yükseklikte şiştiği için pati yarı gömülüydü (dışarı alındı).
- Oda QA görüntüsü: `Assets/QA/PremiumVisuals/2026-09-02_BathroomTub_InRoom_QA.png`.
- Doğrulama: EditMode **319/319**, `LevelContentValidator` 0 hata/0 uyarı,
  Console temiz, kanonik sahne yığını, 1/1/1, kaçak nesne yok, hiçbir sahne
  dirty değil. Git yok.

**BathroomShower** — 13.149 üçgen, yazım ölçüsü 1.66 × 2.22 × 1.30
(limit 1.65 × 2.21 × 1.28). Prefab'ta model scale 0.94 (`FitFixtureModel`),
bounds 1.57 × 2.09 × 1.23, dokuz `CH_*` materyal.

- İçerik: süperelips lofted duş teknesi + altın kenar + süzgeç, tam boy fayanslı
  arka duvar (derz çizgileri, nane su basmanı, krem korniş, altın finial),
  göğüs hizası tonlu paravanlar, üstü açık altın çerçeve, çerçeveden uzanan
  yağmur kolu + başlık, el duşu rayı, köşe rafında şişeler, ön korkuluğa atılmış
  krem çizgili mercan havlu, cam kapıda altın pati.
- **İki tur elendi.** (1) Tam boy opak paravanlar ürünü *buzdolabı* gibi
  gösteriyordu ve gökkuşağını tamamen kapatıyordu — `CH_*` palette şeffaf
  materyal olmadığı için paravanlar 1.15'e indirildi, üst açık bırakıldı.
  (2) Gökkuşağı önce küre boncuklardan yapılmıştı, o ölçekte *tırtıl* okuyordu —
  `kit.sheet` ile tek parça şerit bandına çevrildi (u ekseni bantları, v ekseni
  yayı tarıyor).
- `shell()` küvetten `premium_kit.py`'a taşındı ve `power` parametresi kazandı
  (0.55 = daha köşeli duş teknesi, 0.62 = küvet). `build_bathroom_tub.py` artık
  paylaşılan sürümü kullanıyor.
- Pati imzası tekne ön yüzüne sığmadı (yalnız 0.155 yüksek, oda kamerasından
  okunmuyordu); cam kapıya altın rozet olarak işlendi.
- Kablolama: `TryBuildPremiumBathroomHeroPrefab` hero listesi, `fileName`
  kaydı ve `facesBackward` girdisi eklendi. `TryGetBathroomFixtureSource`
  içindeki eski `Assets/Art/Bathroom/Models/BathroomShower.fbx` yolu artık
  ölü dal (küvette olduğu gibi).
- **Yön ölçüldü.** Gökkuşağının `CH_LemonBright` bandı yalnız arka duvarda var:
  ürün (2.95, 0, 1.82) + yaw 180 ile yerleştirildiğinde dünya z 2.337..2.356'ya,
  yani odanın arka tarafına düşüyor; en öndeki yüzey z 1.206. Açık ön yüz odaya
  bakıyor.
- Oda QA görüntüleri: `Assets/QA/PremiumVisuals/2026-09-03_BathroomShower_InRoom_QA.png`
  ve `..._Closeup_QA.png`.
- Doğrulama: EditMode **319/319**, `LevelContentValidator` 0 hata/0 uyarı,
  Console temiz, kanonik sahne yığını, hiçbir sahne dirty değil. Git yok.

**ShowerRinseActivity — duşa girip yıkanma (3 Eylül 2026).** Kullanıcı kararı:
**bundan sonra tasarlanan her eşya kediyle gerçek bir etkileşim taşıyacak,
sadece görsel olmayacak.** Kural `Docs/PREMIUM_FURNITURE_LANGUAGE.md` §6'ya ve
`AGENTS.md`'ye yazıldı; §6 artık "isteğe bağlı" değil.

- Kedi duşa kendi başına giremiyordu, iki sebeple: (1) `AddProductCollider` her
  mağaza ürününe katalog boyunda tek som kutu veriyor — modelde açık görünen ön
  boşluk fizikte kapalıydı; (2) tekne iç zemini 0.12 yüksekte, kedinin
  `CharacterController` `stepOffset`'i dünya ölçüsünde 0.005. Geometri sığıyordu
  (açıklık 0.80 m, kedi kapsülü 0.50 m çap) — engel collider ve basamaktı.
- Çözüm yıldız çadırı/salıncak deseni: ürün kutusu `isTrigger`, aktivite
  `CharacterController`'ı kapatıp transform'u sürüyor. Rutin: kapı noktasına
  yürü → tekneye adım (y 0.125'e yükselerek) → odaya dön → yıkanma zıplaması →
  sönümlenen silkelenme (±24° yaw + ıslak squash) → dışarı çık → fizik, ölçek ve
  hareket kilidi iade, kedi footprint dışında.
- Kablolama: `CatActivityKind.ShowerRinse = 9`, `QuestType.ShowerRinse = 14`
  (ikisi de **sona eklendi**), `AttachShowerRinseActivity`, `LevelContentValidator`
  Bathroom bölümünde `BathroomShowerId`'ye bağlı kayıt.
  Ankraj (root local): kapı (0.33, 0, 0.95), tekne (0.12, 0.125, 0.05),
  etkileşim (0.33, 0, 1.05). Root uzayı mesh'in aynası: model ön yüzü -Z'de
  yazılı ve 180 dönüş model child'ında, dolayısıyla giriş burada +Z.
- Doğrulama: EditMode **320/320** (yeni `ShowerRinse_WaitsForTheShowerPurchaseAndIsEnterable`
  dahil), `LevelContentValidator` 0 hata/0 uyarı, Console temiz.
- **Açık iş:** `Tests/PlayMode/ShowerRinseTests.cs` yazıldı ama **koşulmadı**. **(4 Eylül 2026: koşuldu, geçti.)**
  MCP üzerinden tek sınıf hedeflenerek başlatıldı, editör yine kilitlendi
  (oturumda iki `refresh_unity compile=request` ve uzun bir builder koşusundan
  sonra; hafızadaki "MCP PlayMode ancak restart'tan hemen sonra güvenli" notunu
  doğruluyor). Unity yeniden başlatılıp Test Runner penceresinden koşulmalı —
  `SwingRideTests`'in düzeltilmiş assert'i de hâlâ bu kuyrukta. Kuyruk şu an
  beş dosya: `ShowerRinseTests`, `SinkSipTests`, `PaperSpinTests`,
  `TowelNestTests`, `SwingRideTests`, `BathroomPropActivityTests`,
  `KitchenActivityTests`.

**BathroomVanitySink + SinkSipActivity (3 Eylül 2026).** 12.570 üçgen, yazım
ölçüsü 1.96 × 1.69 × 0.86 — katalog sözleşmesiyle **birebir**, `FitFixtureModel`
oranı 0.956 (mümkün olan en iyisi). Etkileşim tasarım anında seçildi: kedi
tezgâha sıçrar, akan sudan içer, iner.

- İçerik: krem gövde + iki panelli kapak (altın çerçeve, dikey kulp), sol uçta
  açık havlu nişi (iki raf, dört rulo), beyaz tezgâh + altın kenar, nane
  su sıçratma bandı, tezgâh üstü çanak lavabo + altın kenar, altın kaz boynu
  musluk + akan su + halkalar, diş fırçası bardağı, sabunluk, altın çerçeveli
  ayna, kapakta altın pati.
- Üç tur elendi: (1) havlu nişini tek som gövde kutusu yutmuştu — gövde iki
  bloğa bölündü (TvUnit orta göz hatasının aynısı); (2) kaz boynu boncuk dizisi
  okuyordu; (3) lavabo suyu çanak duvarının içinde kalmıştı.
- **Kit'te gerçek tuzak bulundu:** `kit.cylinder`'a `rotation` verilince `axis`
  parametresi tamamen geçersiz kalıyor, taban dönüşün yerine geçiyor. Eğimli
  dikey parça için doğrusu `(pi/2, 0, lean)`, X ekseni için `(0, pi/2, lean)`.
  Bu yüzden **`build_bathroom_tub.py`'da küvetin iki musluk kolu** ve
  **`build_kitchen_sink_cabinet.py`'da lavabo kolu** yanlış eksende yatıyordu;
  ikisi de düzeltilip yeniden export edildi.
- Yeni paylaşılan yardımcı `kit.tube(parts, name, points, radius, material)`:
  eklem küreleri arasına aynı yarıçapta silindir köprüler koyar. Küvet musluğu
  da buna geçirildi.
- `SinkSipActivity`: çömel → tezgâha yay çizerek sıçra → yalanma → in.
  `ThirstSystem.RestoreThirst()` **yeni eklendi** ve `BeginDrinking` yerine o
  kullanılıyor: `BeginDrinking` `Drank` olayını tetikleyip bir Drink görevi
  yazıyor, `CatActivity.CompleteActivity` de bir tane yazıyor — aynı yudum iki
  kez sayılacaktı. Kutu **trigger yapılmadı**: vanity girilecek yer değil,
  üstüne çıkılan mobilya.
- Perch yüksekliği sabit yazılmadı; `AttachSinkSipActivity` modelin gerçek
  `FitFixtureModel` ölçeğini okuyup `0.860 * scale` hesaplıyor (şu an 0.822).
- Yön ölçüldü: ayna camı (`CH_AquaBright`) dünya z 2.134 ortalamasıyla duvar
  tarafında, kapaklar z 1.637'de odaya bakıyor.
- Doğrulama: EditMode **322/322** (yeni iki test dahil), `LevelContentValidator`
  0 hata/0 uyarı, kanonik sahne yığını, 1 etkin kamera, hiçbir sahne dirty değil.
- Oda QA: `2026-09-03_BathroomTrio_InRoom_QA.png`,
  `2026-09-03_BathroomVanitySink_Closeup_QA.png`.
- **Açık iş:** `Tests/PlayMode/SinkSipTests.cs` yazıldı, koşulmadı. **(4 Eylül 2026: koşuldu, geçti.)**

**Menü tuzağı çözüldü (3 Eylül 2026).** Bu oturumdaki "3-4 dakikalık Unity
kilitlenmeleri" MCP'nin suçu değilmiş: `Tools/Cat Home/Store/Build Real Product
Content` işini bitirince `EditorUtility.DisplayDialog` açıyor ve modal pencere
main thread'i tutuyor. Ölçüm: Unity 20 saniyede 0.2 sn CPU harcıyor ve
`Responding: True` — editör boşta, sadece OK bekliyor. Artık menü hiç
çağrılmıyor; `StoreProductContentBuilder.BuildSilently()` ve
`LevelContentValidator.ValidateProject()` doğrudan `execute_code` ile
çalıştırılıyor, ikisi de tek çağrıda dönüyor.

**BathroomToilet + PaperSpinActivity (3 Eylül 2026).** 6.998 + 820 üçgen
(gövde + makara), yazım ölçüsü 0.90 × 1.54 × 1.08 (limit 0.94 × 1.18 × 1.56).
Prefab'ta model scale 0.972, bounds 0.860 × 1.498 × 1.052.

- **Bu dalganın ilk yaw 270 ürünü.** Sağ duvarda duruyor. Konvansiyon yine de
  değişmiyor: 270 + `facesBackward`'ın 180'i, local -Z'yi dünya -X'e yani oda
  tarafına gönderiyor. Ölçüldü: sifon bandı (`CH_MintBright`) dünya x 3.381'de
  (ürün x 3.05, duvar +X tarafı), kapak minderi 3.041'de ortada, makara
  z -0.614'te odaya bakan tarafta.
- İçerik: kaideli ayak, süperelips hazne + ince altın kenar, beyaz oturak
  halkası, **kapalı** krem kapak + mercan minder + altın düğme, sifon + nane
  bant + altın basma düğmesi, üstünde altın askılı raf (lilac saksı + nane
  yapraklar, mercan sepet), yan tarafta altın kâğıtlık, sifon önünde altın pati.
- Üç hata elendi: (1) sifon havada duruyordu — hazneyi sifona bağlayan arka
  kolon eklendi; (2) `shell()` yalnız yan duvar loft'ladığı için **kapak orta
  yeri delik bir halkaydı** — kapak ve minder ezilmiş kürelere çevrildi;
  (3) kâğıtlık mili makaranın içinde gömülüydü.
- Ayrıca `kit.torus`'ta yön hatası: Z ekseninde döndürülen torus XY düzleminde
  kalıyor, makaranın kenar halkası için normal X'te olmalı — `(0, pi/2, 0)`.
- **Makara ikinci FBX** (`BathroomToiletRoll_Premium.fbx`), aynı origin'de.
  `AttachPaperSpinActivity` onu `RollPivot` altına kuruyor.
- **Ölçülerek bulunan tuzak:** FBX importu X'i çeviriyor, model child'ındaki 180
  de bir kez daha çeviriyor — net sonuçta yazım uzayı ile root uzayı arasında
  **yalnız Z ters dönüyor**. İlk hesap X'i de çevirdiği için pivot makaranın
  73 cm uzağına düşmüştü; o haliyle dönüş makarayı kendi ekseninde döndürmek
  yerine savuracaktı. Doğrulama: `pivotX = rollCentreX = -0.364`.
- Ayrıca makara child'ının `localPosition`'ı sıfır bırakılmıştı; mesh zaten
  tuvaletin origin'inde yazıldığı için pivot ötelemesi **iki kez** uygulanıyordu.
  Doğrusu `-axis`.
- `PaperSpinActivity`: kedi yere basar, üç kez pati atar, makara her vuruşta
  hızlanıp sönümlenerek döner. Kedi hiçbir zaman pivot'un altına `SetParent`
  edilmiyor. `CatActivityKind.PaperSpin = 11`, `QuestType.PaperSpin = 15`
  (ikisi de sona eklendi).
- Doğrulama: EditMode **323/323**, `LevelContentValidator` 0 hata/0 uyarı,
  kanonik sahne yığını, 1 etkin kamera, hiçbir sahne dirty değil.
- Oda QA: `2026-09-03_BathroomToilet_Closeup_QA.png`,
  `2026-09-03_BathroomFour_InRoom_QA.png`.
- **Açık iş:** `Tests/PlayMode/PaperSpinTests.cs` yazıldı, koşulmadı. **(4 Eylül 2026: koşuldu, geçti.)**

### BathroomTowelStorage — 3 Eylül 2026

- Sözleşme birebir: 1.02 × 1.82 × 0.50, 12.200 üçgen, WallEdge (-3.05, 0, .25),
  yaw 270. Prefab bounds 0.979 × 1.747 × 0.480 (FitFixtureModel 0.96).
- Üç katman: panelli iki alt kapak, **açık niş** (0.620 → 1.260), kapaklı üst
  bölme, taç. Nişin içinde 0.026'lık dikey bölme iki göz yapar: sol bayda iki
  yatık rulo + üstünde raf ve saksı, sağ bayda dört katlı havlu yatağı
  (üst yüzey 0.900). Yan yüzde altın ray + iki yaprak halinde sarkan havlu.
- **Yön klozetin tersi ve bu ölçüldü.** Yaw 270 local -Z'yi dünya +X'e gönderir.
  Dolap sol duvarda (x -3.05) olduğu için oda tarafı +X; yani local -Z zaten
  odaya bakıyor ve ürün **`facesBackward`'a girmiyor**. Klozet aynı yaw'ı
  taşıyor ama sağ duvarda, o yüzden 180'i var. Sonuç: burada model child'ında
  180 yok, FBX importunun X aynasını geri çevirecek bir şey de yok —
  yazım noktası (x, y, z) kök uzayında **(-x, y, z)** oluyor, **Z ters
  dönmüyor**. Klozetteki kuralın tam tersi.
- Doğrulama varsayımla değil üçgen centroid'i sayarak yapıldı: yan ray x
  -0.470..-0.442'de (yazımda +0.475 → ayna ✓), yatak üst yüzeyi x < -0.14'te
  84 üçgen / x > -0.13'te 13, nişin arka duvarı z +0.15'te, ön tarafta hiç
  geometri yok (açıklık -Z ✓). Vertex taraması işe yaramadı: beveled cube'ların
  vertex'i yalnız köşelerde, orta yüzeyde hiç yok.
- `TowelNestActivity`: kedi niş önüne yürür, çömelir, yay çizerek yatağa sıçrar,
  dışa döner, kıvrılır, nefes salınımıyla uyur, açılır ve zemine iner.
  Enerji +26, `QuestType.Sleep` (CanopyNap ile aynı — yeni QuestType açılmadı),
  `CatActivityKind.TowelNest = 12`. Kedi hiçbir zaman `SetParent` edilmiyor.
- **Ölçülerek elenen kusurlar (5 tur):**
  1. Tepedeki saksı + şişe modeli 2.241'e çıkarıyordu (sözleşme 1.82) — proplar
     nişin sol üst rafına indi, taç kendi mint bandını taşıyor.
  2. Yan ray modeli 1.13 genişletiyordu (sözleşme 1.02) — gövde 0.455 yarım
     genişliğe içerildi, kaide ve taç tam footprint'te kaldı: çıkıntı artık
     gerçek, taşma yok.
  3. Taç ±0.04 çıkıntısı derinliği 0.54 yapıyordu — çıkıntı silindi.
  4. Havlu yatağı dört kat × 0.030'du, odada dört kalem çizgisi okuyordu —
     kat 0.061'e çıktı, yatak üstü 0.900.
  5. Rulolar ve yatak tek yığın okuyordu — dikey bölme eklendi.
  6. Saksı en üst rulonun 0.031 üstünde havada duruyordu — rulo rafı eklendi ve
     prop tabanı ondan türetildi; üçüncü rulo da kaldırıldı, çünkü rafı 1.008'e
     itip saksıya 0.15'lik bir yuva bırakıyordu.
  7. Kat çizgileri katın **içine** gömülüydü (crease z -0.156, ön yüz -0.1725).
  8. Ray havlusu kendi rayını örtüyordu; sonra da yan yüze yapışık tek levha
     okuyordu — yapraklar X'te de ayrıldı.
- **Ders — kutu ölçüsü propu da sayar.** Sözleşme yüksekliği modelin toplam
  bounding box'ı; tepeye konan saksı da ona dahil. Tepe propu ancak gövdeyi
  kısaltarak sığar, bu da tam boy dolabın silüetini bozar. Doğrusu: propu
  gövdenin **içinde** görünür bir rafa koymak.
- Oda QA: `2026-09-03_BathroomTowelStorage_InRoom_QA.png`. Kamera açısı da
  ölçüldü — oda `Main Camera`'sı (-1, 3, -5.5)'ten +Z'ye bakıyor, yani ürünün
  dünya -Z yüzünü görüyor; ilk QA çekimi ters taraftan bakıp yan rayı hiç
  göstermemişti.
- `LevelContentValidator` Bathroom bloğuna dolap girdisi eklendi, EditMode'a da
  `TowelNest_WaitsForTheCabinetPurchaseAndNestsInsideTheNiche` — testin iki
  assert'i ayna kuralını kilitliyor: `NestPoint.x < 0` ve
  `NestFloorPoint.z < NestPoint.z`.
- Doğrulama: EditMode **324/324**, `LevelContentValidator` 0 hata/0 uyarı,
  Console temiz, kanonik sahne yığını, 1 etkin kamera, hiçbir sahne dirty değil.
- **Açık iş:** `Tests/PlayMode/TowelNestTests.cs` yazıldı, koşulmadı. **(4 Eylül 2026: koşuldu, geçti.)**

### Bathroom kapandı — 10/10, 3 Eylül 2026

Kalan beş ürün tek turda bitti, ve küvete de eksik olan etkileşimi eklendi.
Oda artık tamamen yeni dilde ve **on üründen onunda** gerçek bir kedi davranışı
var.

| Ürün | Ölçü (sözleşme birebir) | Üçgen | Etkileşim |
|---|---|---|---|
| BathroomLitterBox | 1.30 × 0.49 × 1.02 | 6.598 | `LitterDigActivity` |
| BathroomGroomingCart | 0.90 × 1.03 × 0.63 | 15.318 | `GroomBrushActivity` |
| BathroomLaundryHamper | 0.82 × 0.92 × 0.82 | 5.120 | `HamperDiveActivity` |
| BathroomWallMirror | 0.92 × 0.78 × 0.15 | 6.392 | `SitLookActivity` (MirrorGaze) |
| BathroomBathMat | 1.90 × 0.09 × 1.15 | 10.636 | `MatKneadActivity` |
| BathroomTub (model eskiydi) | — | — | `TubEdgeWalkActivity` (yeni) |

- **Yön:** beşi de `facesBackward` dışında (Floor yaw 0, ayna sol duvarda yaw
  270), yani hepsinde **X aynalanır, Z aynalanmaz** — `BathroomTowelStorage` ile
  aynı, `BathroomToilet` ile ters. Küvet ise `facesBackward` içinde, yani onda
  yalnız Z döner: iki kural aynı odada yan yana duruyor.
- **Ayna varsayımla değil ölçümle doğrulandı.** Üçgen centroid'i sayarak:
  halının kabartma pedi x<−0.05'te 1422 / x>0.05'te 440 üçgen; sepetin rozeti
  x<0'da 995 / x>0'da 39; arabanın fırçası minZ −0.300 (ön yüz); kedi kutusunun
  eşik üstü duvarı yalnız z>0.30'da (ön açık).
- `SitLookActivity` yeniden kullanıldı: ayna 1.58'de asılı, kedinin çok
  üstünde, ve pencere/kuş beat'i zaten tam bu — yeni sınıf yazılmadı.
- `CatActivityKind` 13–18, `QuestType` 16–21 eklendi (hepsi sona).

**Ölçülerek elenen kusurlar**

1. Kedi kutusu ilk turda **alçak bir kanepe** okuyordu: tam genişlikte ön dudak
   + dört düz duvar. Ön yüz iki yanağa ve aralarındaki alçak eşiğe bölündü —
   ürünü adlandıran şey o ağız. İkinci turda 0.48'lik düz duvarlar kepçeyi,
   kovayı ve kumun çoğunu bir mint çitin arkasında saklıyordu; yan duvarlar
   arkada tam, önde 0.245 olacak şekilde kademelendi.
2. Kepçe yan duvara asılıyken model **1.43 genişti** (sınır 1.30). Kumun içine
   girdi. Sonra sapı 0.220'de tepeyi 0.535'e çıkarıyordu, 0.170'e indi.
3. `kit.paw_badge` **yatay yüzeyde çalışmıyor**: parmakları X ve **Y**'de
   yayıyor, yani kumun üstünde üzüm salkımı gibi dikiliyordu. Yatay yüzeyler
   için parmakları X ve Z'ye yayan yerel bir `horizontal_paw` yazıldı.
4. Kumdaki lilac oluklar oda çekiminde **kumun üstüne bırakılmış iki pastel
   boya** gibi okudu; kaldırıldı, yerine kumun kendi malzemesinden iki alçak
   tümsek.
5. Çamaşır sepetinin kapağı arkaya devrikken model **1.21 × 1.06** oldu
   (sözleşme 0.92 × 0.82). Kapak tamamen kaldırıldı — zaten `HamperDive` açık
   ağız istiyor. Sonra slat'lar arasında 0.036 boşluk kalınca sepet **açık raf
   rafı** gibi okudu; arkasına kapalı bir gövde kondu. Kulplar rim'in altın
   rengine karıştı, yerini ön yüzde bir plaka + **düz** pati aldı (küresel pati
   0.028 derinliğiyle footprint'i 0.05 aşıyordu).
6. Grooming cart'ın fırçası 0.470'te iki raf arasında asılıydı ve **alt rafın
   tamamını kapatıyordu**; 0.190'a, alt rafın altına, kedi yanak hizasına indi.
7. Aynanın apliği `rotation=(pi/2,0,0)` ile **tavana bakan disk** olmuştu; cama
   paralel yüz `axis="Z"` demek.
8. Küvet **ön-arka simetrik değil**: yüksek sırtlık root −Z'de 1.08'e çıkıyor,
   yürünebilir rim root +Z'de yalnız 0.744. İlk hesap bounds'un yarısını alıp
   kediyi sırtlığa, yüzü duvara koyuyordu. Prefab bant bant profillenerek
   düzeltildi.

**Ders — bir prop yerleştirmeden önce modeli profille.** Bu dalgada iki kez
aynı hata çıktı: bir ürünün bounds'undan türetilen nokta, ürünün asimetrik
olduğu yerde yanlış tarafa düştü. Bounds bir kutudur; ürün değildir.

- Oda QA: `2026-09-03_BathroomLitterBox_InRoom_QA.png`,
  `..._BathroomGroomingCart_...`, `..._BathroomLaundryHamper_...`,
  `..._BathroomWallMirror_...`, `..._BathroomBathMat_...`,
  `2026-09-03_BathroomTub_RimWalk_QA.png`, `2026-09-03_BathroomTen_InRoom_QA.png`.
- Doğrulama: EditMode **330/330**, `LevelContentValidator` 0 hata/0 uyarı,
  Console temiz, kanonik sahne yığını, 1 etkin kamera, hiçbir sahne dirty değil.
- **Açık iş:** `Tests/PlayMode/BathroomPropActivityTests.cs` (altı test) yazıldı,
  koşulmadı. **(4 Eylül 2026: koşuldu, geçti.)**

Bathroom: **10 bitti, 0 kaldı.**

### Kitchen kapandı — 10/10, 3 Eylül 2026

Odanın dokuz modeli yeniden yazıldı (`KitchenSinkCabinet` zaten yeni dildeydi)
ve **on üründen onuna** kedi etkileşimi verildi. Kitchen'ın öncesinde tek bir
etkileşimi bile yoktu.

| Ürün | Ölçü | Üçgen | Etkileşim |
|---|---|---|---|
| KitchenIsland | 2.25 × 1.08 × 1.05 | 10.875 | `PerchNapActivity` / IslandPerch |
| KitchenRefrigerator | 1.25 × 2.25 × 0.78 | 5.628 | `SitLookActivity` / FridgeStare |
| KitchenPantryShelf | 1.15 × 1.85 × 0.55 | 8.632 | `PantryClimbActivity` |
| KitchenStoveOven | 1.18 × 1.30 × 0.74 | 7.386 | `OvenWarmthActivity` |
| KitchenDishCart | 0.90 × 1.15 × 0.61 | 11.018 | `CartNudgeActivity` |
| KitchenCounterStool | 0.73 × 0.81 × 0.67 | 4.162 | `PerchNapActivity` / StoolPerch |
| KitchenFruitBasket | 0.78 × 0.72 × 0.78 | 12.068 | `SitLookActivity` / FruitSwat |
| KitchenFeedingStation | 1.35 × 0.49 × 0.72 | 5.850 | `MealTimeActivity` |
| KitchenPawMat | 2.20 × 0.09 × 1.12 | 9.200 | `MatKneadActivity` / KitchenMatKnead |
| KitchenSinkCabinet (model eskiydi) | — | — | `SinkSipActivity` / KitchenSip |

- **Oda tek yön kuralına indirildi.** `KitchenIsland`, `KitchenRefrigerator`,
  `KitchenStoveOven` ve `KitchenPantryShelf` `facesBackward` listesindeydi çünkü
  eski modellerinin ön yüzü +Z'de yazılmıştı — ada bu yüzden oturma tarafını
  arka duvara, dolap sırtlarını odaya dönüktü. Yeniden yazımda dördünün de ön
  yüzü -Z'ye alındı ve **listeden çıkarıldılar**. Artık Kitchen'ın on ürünü de
  aynı kuralda: ön -Z, `facesBackward` yok, yani **X aynalanır, Z aynalanmaz**.
- Ayna ölçüldü: halının kabartma pedi x<0'da 1852 / x>0'da 158 üçgen; besleme
  istasyonunun maması x>0'da 598 / x<0'da 84 (mama kabı -X'te yazılmış, +X'e
  düşüyor — ters olsaydı kedi su kabından yerdi); ocağın çaydanlığı +X'te;
  buzdolabının kapı bandı minZ -0.374, yani kulplar odaya bakıyor.
- **Üç sınıf yeniden kullanıldı, beş yenisi yazıldı.** `SitLookActivity`
  buzdolabı ve meyve sepeti için, `SinkSipActivity` mutfak lavabosu için,
  `MatKneadActivity` mutfak halısı için. Yeni: `PerchNapActivity` (ada ve tabure
  paylaşıyor), `PantryClimbActivity`, `OvenWarmthActivity`, `CartNudgeActivity`,
  `MealTimeActivity`.
- `CatActivityKind` 19–28 eklendi. Altı rutin üç paylaşılan sınıftan geldiği
  için **validator ve testler tipe değil kind'a bakıyor** — bir
  `FindInScene<T>` paylaşılan sınıfın yalnız ilk örneğini görürdü. Validator'a
  `RequireKitchenActivity(scene, kind, productId, label, report)` eklendi.
- `QuestType` üç yeni değer aldı (`KitchenWatch`, `PantryClimb`, `CartNudge`);
  gerisi mevcutları kullanıyor — yemek `Eat`, lavabo `Drink`, ada/tabure/fırın
  `Sleep`, halı `MatKnead`.
- `CartNudgeActivity` odanın tek hareketli ürünü, ama tuvalet kâğıdı makarasının
  aksine **ikinci FBX ve pivot istemiyor**: arabanın tamamı yuvarlanıyor, o
  yüzden rutin doğrudan `VisualContent`'i sürüp yerine yaylandırıyor. Test
  `localPosition`'ın 0.001 içinde başladığı yere döndüğünü doğruluyor — her
  itişten biraz kalsa araba sonunda kataloğun koymadığı bir yerde dururdu.
- `MealTimeActivity` `HungerSystem.Feed` kullanıyor, `BeginEating` değil:
  `BeginEating` `Ate` fırlatıyor, o da bir Eat görevi yazıyor, `CatActivity` de
  tamamlanınca bir tane daha yazıyor — tek öğün iki kez sayılırdı. Vanity
  lavabosundaki `RestoreThirst` kararının aynısı.

**Ölçülerek elenen kusurlar**

1. Ada ilk turda **1.494 boyundaydı** (sözleşme 1.08): tezgâh 1.08'e konmuş,
   kavanoz ve saksı onun üstüne çıkmıştı. Sözleşme yüksekliği modelin tamamı,
   tezgâhın değil — tezgâh 0.86'ya indi ve üstündeki her şey kalan 0.22'ye
   sığdırıldı. Bu, banyo dolabının ödediği dersin ikinci faturası.
2. Adanın sepet bölmesi **gövdenin içinde kalmıştı**: carcass tam genişlikteydi
   ve bölmeyi yutuyordu, ada iki kapaklı kapalı bir kutu okuyordu. Carcass artık
   bölmeden önce bitiyor.
3. Adanın tezgâh üstü mint kaplaması 2.10 × 0.90'lık bir levhaydı ve taşı
   boyanmış gösteriyordu; kenardan içeri çekilmiş ince bir bant oldu.
4. Buzdolabının kapıları sabit aralıklarla yazılmıştı ve gövdenin **üst 0.53'ü
   çıplak krem** kalıyordu; iki kanat artık gövdeyi bölüşüyor.
5. Tabure **0.61 × 0.61**'di, kutusu 0.72 × 0.66. Katalog footprint'i bir elips;
   yuvarlak oturak ikisinden yalnız küçüğünü doldurabiliyor. Oturak ve ayak
   açıklığı X'te 1.091 ile gerildi.
6. Fırın kulpu ve buzdolabı kulpları derinliği taşırıyordu (0.80 ve 0.75'e karşı
   0.74 ve 0.78); ikisi de gövdeye çekildi.
7. Araba **0.53 derindi** (sözleşme 0.62) ve rozet patisi 0.62'yi aşıyordu —
   gövde derinleşti, pati düz sürüme çevrildi.

- Oda QA: `2026-09-03_KitchenTen_InRoom_QA.png`,
  `..._KitchenIsland_...`, `..._KitchenWall_...`, `..._KitchenProps_...`.
- Doğrulama: EditMode **336/336**, `LevelContentValidator` 0 hata/0 uyarı,
  Console temiz, kanonik sahne yığını, 1 etkin kamera, hiçbir sahne dirty değil.
- **Açık iş:** `Tests/PlayMode/KitchenActivityTests.cs` (üç test, biri on rutinin
  tamamını sırayla koşuyor) yazıldı, koşulmadı. **(4 Eylül 2026: koşuldu, geçti.)**

Kitchen: **10 bitti, 0 kaldı.** İki oda tamamlandı. Öncelik A'da `GardenSapling`
ve `BalconyHerbShelf` kaldı; sıradaki oda kararı bekliyor.

### Low Poly Animated Cats entegrasyonu — 3 Eylül 2026

- Satın alınan Polyperfect paketinin siyah-beyaz `Cat_Domestic_Shorthair`
  prefabı kanonik oynanabilir kedi oldu. Proje varlığı
  `Assets/Art/Cat/Polyperfect/CatHome_DomesticShorthair.prefab` altında; paket
  dosyaları değiştirilmedi.
- Paketin bool/trigger tabanlı Controller'ı doğrudan kullanılmadı. Cat Home'un
  `Speed` Float ve `Idle / Pet / Eat / Drink / LieDown / Sleep / Activity*`
  durum sözleşmesini koruyan `CatHome_Polyperfect.controller` üretildi. Kaynak
  FBX'teki 23 klibin tamamı doğrulandı; locomotion Idle/Walk/Run blend tree,
  bakım ve aktivite durumları paketin uygun kliplerine bağlandı.
- Model-fit ölçeği proje prefabının içinde tutuluyor; home, Runner ve Catch kendi
  mevcut dış ölçeklerini koruyor. `applyRootMotion=false`, hareketli kafa kemiğine
  bağlı `HeartSpawn`, URP Lit materyal ve mevcut kürk tint sistemi korunuyor.
- Sekiz ev sahnesi, Cat Runner ve Cat Catch yeni prefaba geçirildi; iki mini oyun
  builder'ı da bundan sonraki rebuild'lerde aynı kanonik yolu kullanıyor. Eski
  Rig V2.1 Play-only swap yeni Controller'ı tekrar değiştiremiyor.
- Doğrulama: yeni entegrasyon testleri **4/4**, tüm EditMode **340/340**, ilgili
  Runner + Cat Catch PlayMode **4/4**, `LevelContentValidator` 0 hata/0 uyarı.
  Home'da locomotion yönü ile Pet/Eat/Sleep/Pounce pozları ve iki mini oyunun
  gerçek gameplay görüntüleri kontrol edildi; Console temiz ve kanonik üç sahne
  yığını geri kuruldu.

### Living Room aydınlık ve dolaşım güvenliği — 3 Eylül 2026

- Living Room artık cihaz saatinden ve eski `LOW / MED / HIGH` tercihinden
  etkilenmiyor; sabit gündüz/noon profiliyle diğer odalar gibi aydınlık kalıyor.
  Eski tercih verisi save uyumluluğu için okunabilir durumda, ancak ışık
  çarpanı kanonik `1.10` değerinde sabit.
- Sağ üstteki saat, ampul düğmesi ve parlaklık paneli hem ana UI sahnesinden hem
  `MainPanel` prefab/builder hattından kaldırıldı. Validator bunların geri
  eklenmesini artık hata sayıyor.
- Sekiz ev sahnesine ortak kabuk ölçülerinden üretilen `HomeRoomBoundary`
  eklendi. `CatMovement`, yalnız controller kapsülünü değil yeni kedinin gerçek
  renderer ayak izini de hesaba katarak dört kenarda güvenli sınır içinde
  tutuyor; kayıtlı bozuk/dışarı taşmış pozlar da aynı sınıra çekiliyor.
- Yenilenen Living Room'un matbu `LivingSofa_PremiumModel` ve
  `LivingCoffeeTable_PremiumModel` görsellerine ölçülmüş, solid `BoxCollider`
  eklendi. World/lighting rebuild hatları bu güvenlik katmanını yeniden
  uyguluyor.
- Doğrulama: yeni EditMode testleri **4/4**, tüm EditMode **344/344**, ilgili
  bootstrap PlayMode smoke testleri **2/2**, `LevelContentValidator` 0 hata / 0
  uyarı. Manuel Play'de dört oda kenarı, koltuk ve sehpa teması, sabit gündüz ve
  kaldırılan saat/ışık kontrolleri doğrulandı. Tam PlayMode turundaki önceden
  yazılıp koşulmamış Bathroom/Kitchen aktivite testlerinin 13'ü, tek oda test
  kurulumunda ortak `EnergySystem` bulunmadığı için ayrı test-harness işi olarak
  açık kaldı; bu değişikliklerin iki bootstrap testi geçti.

### CAT SHOP ve seçilebilir paket kedileri — 4 Eylül 2026

- Sağ üstte kaldırılan ışık denetiminin yeri, seçili kedinin gerçek paket
  portresini kullanan `CatShopButton` ile dolduruldu. CAT SHOP açıkken alttaki
  topbar devre dışı kalıyor; üç sağ üst düğme arasında 1920×1080'de en az 16 px
  görünür boşluk korunuyor.
- Polyperfect paketindeki **10 farklı kedi** kataloglandı. Deneme aşamasında
  tamamı kilitsiz ve doğrudan seçilebilir; kartlar temsili çizim yerine paketin
  kendi gerçek kedi portrelerini kullanıyor.
- Seçilen kedi canlı 3D olarak iki katlı yuvarlak platformda gösteriliyor.
  Önizleme yavaşça otomatik dönüyor; fare veya parmak sürüklemesi yatay ve dikey
  açıları değiştiriyor, tekerlek yakınlaştırmayı destekliyor.
- `USE THIS CAT` seçimi kalıcıdır ve ortak Cat Home animasyon denetleyicisini
  koruyarak Home, Cat Runner ve Cat Catch görsellerine uygulanır. Paket kaynağında
  doğrulanan **23 animasyon klibi** bulunuyor; Cat Home durumları bunların uygun
  Idle/Walk/Run/bakım/aktivite kliplerine bağlı kalıyor.
- Takip düzeltmesi: Polyperfect Generic kliplerinin `Cat_Domestic_Shorthair/...`
  yoluna bağlı olması, diğer prefabların `Cat_Mainecoon`, `Cat_Sphynx` gibi farklı
  model-kök adlarında kemik hareketini kesiyordu. Yalnız çalışma anındaki kopyanın
  bağlama kökü standartlaştırıldı; paket dosyaları değiştirilmedi. Koruma testi
  artık 10 kedinin her birinde gerçek bir bacak kemiği hareketi ve zorunlu
  Animator durumlarını örnekliyor. Maine Coon ayrıca gerçek joystick girdisiyle
  odada yürütüldü; Sleep bileşeninin aynı Animator ve `LieDown / Sleep / Idle`
  durumlarını gördüğü doğrulandı.
- Doğrulama: CAT SHOP EditMode testleri **4/4**, tüm EditMode **348/348**,
  ilgili Living Room/Runner/Catch PlayMode kontrolleri **5/5** ve
  `LevelContentValidator` 0 hata / 0 uyarı. Tam PlayMode turunda 27 testin 14'ü
  geçti; kalan 13 test, bir önceki kontrol noktasında kaydedilmiş aynı
  Bathroom/Kitchen tek-oda `EnergySystem` test-harness eksiğinde kaldı ve CAT
  SHOP kaynaklı yeni bir hata oluşmadı. Manuel Play'de CAT SHOP açılışı,
  Sphynx kartı, dört yönlü döndürme, canlı Home değişimi, animator durumları ve
  topbar portresinin güncellenmesi doğrulandı. QA görüntüleri:
  `2026-09-04_CatShop_Turntable_Final.png`,
  `2026-09-04_CatShop_Sphynx_Selected.png`,
  `2026-09-04_CatShop_Sphynx_InHome.png`.

### Bedroom modelleri ve etkileşimleri — 4 Eylül 2026

Odanın dokuz modeli yeniden yazıldı (`BedroomStarCanopy` zaten yeni dildeydi) ve
**on üründen onuna** kedi etkileşimi verildi. Oda görsel QA'sı ve PlayMode turu
henüz yapılmadı; aşağıdaki "Açık iş" bunu ayrıntılandırıyor.

| Ürün | Ölçü | Üçgen | Etkileşim |
|---|---|---|---|
| BedroomQueenBed | 2.32 × 1.05 × 1.40 | 6.086 | `PerchNapActivity` / BedNap |
| BedroomWardrobe | 1.15 × 2.15 × 0.59 | 5.946 | `ScratchPostActivity` / WardrobeScratch |
| BedroomPawRug | 2.21 × 0.09 × 1.29 | 11.198 | `MatKneadActivity` / BedroomMatKnead |
| BedroomWindowDaybed | 2.00 × 0.73 × 0.82 | 4.842 | `SitLookActivity` / DaybedWatch |
| BedroomNightLight | 0.45 × 0.84 × 0.45 | 4.569 | `SitLookActivity` / NightLightGaze |
| BedroomDreamArt | 1.05 × 0.79 × 0.10 | 5.092 | `SitLookActivity` / ArtGaze |
| BedroomNightstand + Glass | 0.70 × 0.72 × 0.52 | 4.984 + 516 | `KnockOffActivity` |
| BedroomYarnBasket | 0.73 × 0.52 × 0.73 | 10.480 | `SitLookActivity` / YarnSwat |
| BedroomVanityStool | 0.61 × 0.49 × 0.61 | 7.366 | `PerchNapActivity` / VanityStoolNap |
| BedroomStarCanopy (model eskiydi) | — | — | `CanopyNapActivity` |

- **Tek yeni sınıf `KnockOffActivity`.** Bardağı iki kez yoklayan, üçüncüde
  komodinden düşüren, düşüşü ve sekmeyi sürüp bardağı **tam başladığı yere**
  geri koyan rutin. Yerde bırakılan bardak, kataloğun hiç koymadığı bir yerde
  durur ve bir sonraki kedi zaten düşmüş bardağı düşürür. Gerisi yeniden
  kullanım: `PerchNapActivity` ×2, `SitLookActivity` ×4, `ScratchPostActivity`,
  `MatKneadActivity`, mevcut `CanopyNapActivity`.
- **Oda iki yön kuralını birlikte taşıyor — ve ilk geçişte yanlış varsayıldı.**
  Zemin ürünleri (yatak, halı, gece lambası, yumak sepeti, tuvalet taburesi,
  yıldız çadırı) `facesBackward` dışında, oda kök **-Z**. Dört duvar ürünü
  (gardırop yaw 270, daybed 180, komodin ve tablo 90) listede, oda kök **+Z**.
  Dördü de -Z varsayılmıştı: kedi gardıroba, komodine, tabloya ve daybed'e
  duvarın içinden yaklaşacaktı. Ölçümle yakalandı ve düzeltildi; dünya
  koordinatlarında doğrulandı — DreamArt anchor duvardan 0.85 içeride,
  Nightstand 0.84, Wardrobe 0.78, Daybed 0.86. Ders `AGENTS.md`'ye yazıldı.
- Ayna ölçüldü: QueenBed yastıkları x>0'da 1955 / x<0'da 481 üçgen (kedi boş
  yarıya, -X'e yatıyor) · PawRug kabartma pedi x<0'da 1874 / x>0'da 696 ·
  YarnBasket'in gevşek yumağı x>0'da 1887 · Nightstand saati z<0'da 1121 /
  z>0'da 16 (`facesBackward` → Z aynalanıyor, X aynalanmıyor).
- **Komodinde prop faturasını gövde ödedi.** Bardak `KnockOffActivity`'nin
  konusu olduğu için kaldırılamıyordu; 0.72'lik tabla + saat + bardak 0.72
  sözleşmesine karşı 0.872 okudu. Gövde kısaltıldı (`CARCASS_TOP 0.508`, tabla
  `0.568`), ~0.15 proplara ayrıldı. `PREMIUM_FURNITURE_LANGUAGE.md` §6'ya
  eklendi.
- `CatActivityKind` 29–37 ve `QuestType` 25–26 eklendi (hiçbiri yeniden
  sıralanmadı). `LevelContentValidator`'a on Bedroom girdisi eklendi ve
  `RequireKitchenActivity` → `RequireRoomActivity` olarak genelleştirildi;
  Kitchen çağrıları da güncellendi.
- EditMode testleri: `ActivityUnlockTests`'e altı Bedroom testi eklendi —
  `Bedroom_EveryProductCarriesItsOwnRoutine` (on ürün / on kind / on kapı),
  `Bedroom_ApproachesEveryProductFromTheRoomAndNotTheWall` (yukarıdaki hatanın
  regresyon testi), `BedNap_TakesTheClearHalfAndLeavesThePillows`,
  `KnockOff_ReachesOverTheNightstandAndPushesTheGlass`,
  `BedroomMatKnead_WorksTheRugsRaisedPad`,
  `DaybedWatch_SitsOnTheCushionAndLooksOutTheWindow`. Kitchen'ın
  `LoadKitchenActivity` yardımcısı `LoadRoomActivity` olarak genelleştirildi.
- Doğrulama: EditMode **354/354**, Console temiz.
- **Açık iş — oda henüz kapanmadı:**
  1. **Oda QA render'ı alınmadı.** Dokuz modelin oda içi görüntüsü yok; yalnız
     `BedroomQueenBed`'in hero render'ına bakıldı. Bathroom ve Kitchen'da her
     modelde hero render turunda 2–8 kusur elenmişti; bu tur atlandı, oda
     QA'sında kusur çıkması beklenir.
  2. `Tests/PlayMode/BedroomActivityTests.cs` (iki test: on rutini sırayla
     koşan tur + bardağın yerine döndüğünü doğrulayan test) yazıldı, koşulmadı. **(4 Eylül 2026: koşuldu, geçti.)**
  3. PlayMode kuyruğu sekiz dosya: `ShowerRinseTests`, `SinkSipTests`,
     `PaperSpinTests`, `TowelNestTests`, `SwingRideTests`,
     `BathroomPropActivityTests`, `KitchenActivityTests`,
     `BedroomActivityTests`. MCP üzerinden PlayMode editörü kilitliyor;
     Test Runner penceresinden elle koşulmalı.

Bedroom: **10 model + 10 etkileşim bitti**, oda QA ve PlayMode kaldı. Üç odanın
modelleri tamam (Bathroom, Kitchen, Bedroom) + Living Room = **43/80**.

### Bedroom hero render turu — 4 Eylül 2026

Bir önceki kontrol noktasının birinci açık işi kapandı: dokuz modelin hero/front
render'ları tek tek incelendi ve **dokuzunda da kusur çıktı**. Bathroom ve
Kitchen'da bu tur model başına yapılıyordu; Bedroom'da atlanmıştı.

- **Preview bayrağı yanlıştı ve bir modelin tamamını gizlemişti.**
  `build_bedroom_window_daybed.py` `render_views(..., front_plus_z=True)`
  çağırıyordu; kendi docstring'i "front authored at -Z" diyor. Bayrak
  `facesBackward`'a göre değil, yazılan geometriye göre verilir. İki kamera da
  modelin arkasına bakıyordu, o yüzden onay render'ı boş bir krem panel
  gösteriyordu: rafın kitapları, sepeti, iki bolster'ı ve yastığı hiç
  görülmemişti. Ders `AGENTS.md`'ye yazıldı.
- **Beş modelde aynı hata: süs parçası süslediği yüzeyin içine yazılmıştı.**
  Sepetin çıtaları, taburenin pilileri, gardırobun kaide şeridi, komodinin tabla
  şeridi ve gece lambasının ışık diski. Hepsinde parçanın yalnız en şişkin yeri
  yüzeyden taşıyordu. `PREMIUM_FURNITURE_LANGUAGE.md` §6'ya eklendi.
- **Üç modelde `rotation=(pi/2,0,0)` diski tavana baktırmıştı** — gece
  lambasının yıldızları, komodinin saati, tablonun yıldızları. Kural dokümanda
  zaten vardı. Hepsi `axis="Z"` oldu; komodinin saati bu yüzden render'da hiç
  görünmüyordu.
- **Komodinin bardağı hiç render edilmemişti.** Ayrı FBX olduğu için
  `render_views` onu sahnede Y-up bırakıyor, kadrajın köşesinde yan yatıyordu —
  `KnockOffActivity`'nin konusu olan tek prop. Preview'da bardak artık gövdeye
  parent ediliyor.
- Ürün başına düzeltmeler: gece lambasının kopuk üçayağı ve ters hilali;
  daybed'in koltuk döşemesine gömülmüş raf içeriği (`SHELF_TOP 0.330` üstüne
  0.19–0.23 boyunda kitap/sepet); yatağın 0.35 üst üste binen yastıkları;
  halının krem ovalden taşan yoğurma pedi (elipsin ped kenarında derinliği
  0.201, ped 0.350 istiyordu) ve ovalin dışındaki iki yıldızı; sepetin havada
  duran gevşek yumağı; taburenin havada başlayan pilileri; gardırobun
  dengesiz ayna/pati cephesi (artık iki ayna + ortada pati kilit göbeği);
  tablonun çerçeveden taşan tepesi ve üst kenarı aşan iki yıldızı.
- Dokuz FBX yeniden export edildi, `StoreProductContentBuilder.BuildSilently()`
  koşuldu. Prefab anchor'ları yeniden ölçüldü: dört duvar ürününün anchor'ı
  hâlâ kök +Z (0.78–0.86), altı zemin ürününün kök -Z (-0.62…-1.22).
  `BedroomPawRug`'ın `KneadPadPoint`'i ped küçüldüğü için -0.490'dan -0.465'e
  taşındı; işaret ve yükseklik değişmedi.
- Doğrulama: EditMode **354/354**, `LevelContentValidator` 0 hata / 0 uyarı,
  Console temiz.
- **Kalan açık iş:** oda içi QA render'ı (hero turu modelleri tek tek doğruladı,
  ama dokuzu bir arada `Bedroom_Level01` içinde henüz görülmedi) ve PlayMode
  kuyruğu — sekiz dosya, Test Runner'dan elle koşulmalı.

### Bedroom oda QA — 4 Eylül 2026

Dört capture alındı: `2026-09-04_BedroomTen_InRoom_QA.png` (kanonik oda
kamerası), `..._BedroomLeftWall_...`, `..._BedroomRightWall_...` ve
`..._BedroomStool_...`. Kanonik kamera on üründen ikisini (gece lambası ve
tablo) kadraj dışında bıraktığı için iki duvar açısı ayrıca çekildi.

- **Hero render'ın yakalayamadığı bir hata çıktı: taburenin minderi Unity'de
  yoktu.** `kit.revolve` `thickness=0.0` ile tek yüzlü kabuk üretiyor; Blender
  iki yüzü de çizdiği için onay render'ı doğru görünüyordu, Unity arka yüzleri
  elediği için kubbe kayboluyor ve altındaki beyaz oturak kasası minderin
  ortasında sert bir leke olarak okuyordu. Odanın `thickness=0.0` yazılmış tek
  revolve'u buydu. Mindere kalınlık verildi (0.012), profil kutba kadar
  götürülüp tepe kapatıldı. Ders `PREMIUM_FURNITURE_LANGUAGE.md` §6'ya yazıldı:
  **bu sınıf hatayı yalnız oda içi capture yakalar.**
- Aynı üründe iki rötuş daha: dört beyaz kapitone düğmesi lilac minderin
  üstünde leke okuyordu (altına çevrildi, küçültüldü ve kubbe yüzeyine
  taşındı — solidify sonrası minderin içinde kalmışlardı), mercan kurdele
  bandı pililerden 0.010 uzakta durup hula hoop okuyordu (0.292'den 0.286'ya).
- Geri kalan dokuz ürün oda içinde temiz: hepsi zeminde, iç içe geçme yok,
  duvar ürünlerinin dördü de oda tarafından yaklaşılabilir duruyor, tablo sol
  duvarda ve gece lambası sağ duvarda doğru okuyor.
- Doğrulama: EditMode **354/354**, `LevelContentValidator` 0 hata / 0 uyarı,
  Console temiz.

Bedroom'un son açık işi de kapandı — aşağıya bak.

### PlayMode kuyruğu boşaldı — 4 Eylül 2026

> **Bu bölümün "hepsi geçti" iddiası doğru değildi.** Aynı günün ilerleyen
> `Run All` koşusunda bu sekiz dosyanın altısı kırıldı: `TowelNestTests`,
> `BathroomPropActivityTests`, `KitchenActivityTests` ve `BedroomActivityTests`
> hem yanlış `parent Is.Null` assert'ini hem de eksik enerji sağlamasını
> taşıyordu, `SinkSipTests` ve `PaperSpinTests` de enerji sistemi olmadan
> koşuyordu. Doğru kayıt yukarıdaki "PlayMode kuyruğu (üç tur koşuldu)"
> maddesindedir.

Sekiz PlayMode dosyası Test Runner penceresinden elle koşuldu:
`ShowerRinseTests`, `SinkSipTests`, `PaperSpinTests`, `TowelNestTests`,
`SwingRideTests`, `BathroomPropActivityTests`, `KitchenActivityTests`,
`BedroomActivityTests`. Bunların bir kısmı 2 Eylül'den beri yazılmış ama
koşulmamış olarak bekliyordu.

MCP üzerinden PlayMode hâlâ editörü kilitliyor; bu kuyruk her zaman elle
koşulacak.

**Bedroom kapandı: 10 model, 10 etkileşim, EditMode 354/354, PlayMode elle
yeşil, oda QA alındı, validator temiz.** Dört odanın içeriği bitti
(Living Room, Bathroom, Kitchen, Bedroom) — **43/80**.



### Garden kapandı — 10/10, 4 Eylül 2026

Odanın on modeli de yeniden yazıldı ve **on üründen onuna** kedi etkileşimi
verildi. Garden'ın öncesinde yalnız sahnedeki bedava yumak oyuncağının bir
rutini vardı; satın alınan on üründe hiçbiri yoktu.

| Ürün | Ölçü | Üçgen | Etkileşim |
|---|---|---|---|
| GardenPergola | 2.00 × 1.85 × 1.46 | 8.566 | `PantryClimbActivity` / PergolaClimb |
| GardenSapling | 0.78 × 1.85 × 0.79 | 12.418 | `ScratchPostActivity` / TreeScratch |
| GardenBistroSet | 1.30 × 0.78 × 0.77 | 11.346 | `PerchNapActivity` / BistroPerch |
| GardenHammock + Bed | 1.17 × 0.84 × 0.71 | 3.091 + 3.944 | `SwingRideActivity` / HammockSway |
| GardenSunLounger | 1.39 × 0.49 × 0.54 | 5.834 | `TowelNestActivity` / SunBask |
| GardenGrill | 0.80 × 1.06 × 0.61 | 5.926 | `SitLookActivity` / GrillWatch |
| GardenBirdBath | 0.70 × 0.83 × 0.77 | 7.728 | `SinkSipActivity` / BirdBathSip |
| GardenFlowerPots | 0.82 × 0.62 × 0.70 | 8.944 | `LitterDigActivity` / PotDig |
| GardenDaisyBed | 1.13 × 0.34 × 0.74 | 12.424 | `MatKneadActivity` / DaisyRoll |
| GardenYarnBall + Ball | 0.48 × 0.12 × 0.48 | 4.086 + 1.446 | `GardenYarnChaseActivity` / YarnBallChase |

- **Yeni sınıf yazılmadı.** On rutin de mevcut sınıfların yeniden kullanımı;
  Bedroom'un `KnockOffActivity`'si gibi bir imza hareketi Garden'da gerekmedi.
  `GardenYarnChaseActivity` zaten vardı ama sahnedeki bedava oyuncağa bağlıydı —
  mağaza sürümü kendi `CatActivityKind`'ını aldı, yoksa kind'a göre arama
  hangisi önce yüklendiyse onu bulurdu.
- **Odanın on ürünü de zemin ürünü, ama tek yön kuralı çıkmadı.** Üçü sıfırdan
  farklı yaw taşıyor ve aynı fikirde değiller: yaw 90 yerel -Z'yi dünya -X'e,
  yaw 270 +X'e gönderiyor, dolayısıyla şezlong (x +2.55) zaten avluya bakıyor
  ve çevrilmiyor, hamak (x -2.75) ve mangal (x +2.65) `facesBackward`'a
  giriyor. Bedroom'un dersi burada duvarsız bir odada tekrarlandı: **konum
  karar veriyor, yaw değil.**
- **İki ürün ikişer FBX.** Hamağın yatağı `SwingRideActivity`'nin pivotu altına
  asılıyor, yumak topu `GardenYarnChaseActivity` tarafından zıplatılıyor. İkisi
  de porch swing'in oturağıyla aynı kural: ayrı tek-nesneli dosya, ortak orijin.
- **Yakalanan hatalar** (hepsi render veya prefab ölçümüyle): bistro masasının
  fincanı `kit.revolve` konum almadığı için masa direğinin göbeğinde kalmıştı;
  aynı ürünün `PerchPoint`'i mesh Z'de merkezli olmadığı için `FitFixtureModel`
  modeli -0.137 kaydırınca masanın 0.14 arkasında havada duruyordu; şezlong
  hamak için yazılan bir sed kalıbı yüzünden sessizce `facesBackward`'a
  girmişti; mangalın patisi hazne duvarının içindeydi; gece lambası dersinden
  bilinen "süs parçası süslediği yüzeyin içine gömülür" tuzağı sepet çıtaları,
  saksı örgüsü, sütun yivleri ve papatya yatağının yosun halkasında tekrar
  çıktı ve her birinde yüzey yarıçapı hesaplanarak düzeltildi.
- Oda QA: `2026-09-04_GardenTen_InRoom_QA.png`, `..._GardenLeftSide_...`,
  `..._GardenRightSide_...`. Pergolanın kanvası oda kamerasından ürünün en
  büyük yüzeyi olarak okuyor — çatının üstünde olması kararı burada doğrulandı;
  preview hero kamerası 1.68'de olduğu için onu hiç düzgün göstermiyordu.
- Doğrulama: EditMode **364/364**, `LevelContentValidator` 0 hata / 0 uyarı,
  Console temiz.
- **Açık iş — bilerek ertelendi (4 Eylül 2026 kararı):** Garden PlayMode
  testleri yazılmadı. `BedroomActivityTests`
  desenine göre `GardenActivityTests.cs` gerekiyor (on rutini sırayla koşan bir
  tur + hamağın yatağını ve yumak topunu yerine koyduğunu doğrulayan testler).
  Geometri EditMode'da kilitli; eksik olan davranış turu. Sonraki odaların
  PlayMode dosyalarıyla birlikte yazılacak.

Garden: **10 bitti, 0 kaldı.** Beş odanın içeriği tamam (Living Room, Bathroom,
Kitchen, Bedroom, Garden) — **53/80**.

### Second Floor kapandı — 10/10, dalga 3 bitti, 4 Eylül 2026

Odanın **on modelinin onu da** sıfırdan yazıldı (altısı dalga 2'den kalma
tarif-öncesi FBX'ti, dördü prosedürel Unity primitifiydi) ve **on üründen
onuna** kedi etkileşimi verildi. Loft, evde hiçbir ürününde rutin olmayan son
odaydı.

- **Yeni `CatActivityKind` 67–76 ve `QuestType` 33.** Onunun onu da paylaşılan
  sınıfları kendi kind'ıyla kullanıyor; yeni sınıf gerekmedi.
- **Odanın iki hareketli parçası var, ikisi de ayrı tek-nesneli FBX.**
  `LoftBookStackBook` (yığından düşen cilt, `KnockOffActivity`) ve
  `LoftRecordPlayerDisc` (dönen plak, `PaperSpinActivity`). İkisinin de mesh'i
  ürün orijininde yazıldı, builder pivot ofsetini çocukta iptal ediyor.
- **Oda tek yaklaşma kuralına sığmıyor, üçüne sığıyor.** İki ürün
  `facesBackward` içinde ve kök +Z'den; `LoftFloorCushions` ile
  `LoftBookStack` sağ kenarda, kök -X'ten; `LoftArcLamp` sol kenarda, kök
  **+X**'ten; kalan beşi açık zemin, kök -Z. `SecondFloor_ApproachesEvery
  ProductFromTheRoomAndNotTheWall` dördünü de kilitliyor.
- **`FitFixtureModel` yok; on iki dosyanın yedisi ilk derlemede taştı.**
  Döndürülmüş minder kendi span'inden büyüdü, pati imzası iğnelendiği panelin
  ötesine uzandı, üst raf kitapları kornişi deldi, sürgü boncuğu 0.026
  yarıçapıyla ön yüzün dışına çıktı. Hepsi elle budandı ve
  `SecondFloorProducts_AreAuthoredInsideTheirCatalogBox` ile kilitlendi.

Second Floor: **10 bitti, 0 kaldı.** **Dalga 3 tamamlandı — 80/80.** Sekiz odanın
tamamı premium tasarım dilinde ve her ürünün gerçek bir kedi etkileşimi var.
Garden, Balcony, Patio ve Second Floor PlayMode testleri yazıldı
(`GardenActivityTests`, `BalconyActivityTests`, `PatioActivityTests`,
`SecondFloorActivityTests`); MCP üzerinden koşulmaz, Test Runner'dan elle
çalıştırılır.

### Patio kapandı — 10/10, 4 Eylül 2026

Odanın on ürününden **sekizi** yeniden yazıldı (`PergolaArch` ve `PorchSwing`
dalga 3'te zaten bitmişti) ve **on üründen onuna** kedi etkileşimi verildi.
Patio v1 tek rutinle gönderilmişti: salıncak sürüşü.

- **Yeni `CatActivityKind` 58–66 ve `QuestType` 31–32.** Hiçbiri yeni sınıf
  gerektirmedi; ondan dokuzu paylaşılan sınıfları kendi kind'ıyla kullanıyor
  (`PantryClimbActivity`, `ScratchPostActivity`, `PerchNapActivity`,
  `OvenWarmthActivity`, `SinkSipActivity`, `SitLookActivity` x2,
  `LitterDigActivity`, `MatKneadActivity`).
- **Oda tek bir yaklaşma kuralına sığmıyor, üç tanesine sığıyor.** Üç ürün
  `facesBackward` içinde ve kök +Z'den yaklaşılıyor; `PatioWaterFountain` ile
  `PatioPottedFerns` avlunun sağ kenarına dayalı, yaw 0 olmalarına rağmen
  yaklaşma kök **-X**'ten; kalan dördü açık zemin, kök -Z. `Patio_Approaches
  EveryProductFromTheCourtyard` üçünü de kilitliyor.
- **`FitFixtureModel` yok, altı model ilk derlemede taştı.** Şemsiye kaburgaları
  kubbenin altında kayboldu, fıskiyenin kabuk süsleri 0.03 dışarı çıktı,
  eğreltinin tomurcuğu 0.01 aştı, saksılığın derinliği 0.38'e gitti, halının
  pati imzası 0.08'lik ürünü 0.12 yaptı. Hepsi elle budandı,
  `PatioProducts_AreAuthoredInsideTheirCatalogBox` ile kilitlendi.
- **Açık kalan iki yerleşim notu (model kusuru değil, katalog yerleşimi).**
  `PatioStringLights` 1.95'te asılı ama Patio'nun arka sınırı 0.6'lık bir
  parapet, dolayısıyla dizi boşlukta duruyor gibi okunuyor; 0.30'luk kutuya
  direk sığmadığı için model tarafında çözümü yok. `PatioPottedFerns` (2.95,
  1.5) sahnedeki dekor küresi ve salıncak iskeletiyle çakışıyor, saksı
  kameradan görünmüyor. İkisi de `StoreCatalogAssets` yerleşim kararı; save
  kimliklerini etkilediği için bu turda değiştirilmedi.

Patio: **10 bitti, 0 kaldı.** Yedi odanın içeriği tamam (Living Room, Bathroom,
Kitchen, Bedroom, Garden, Balcony, Patio) — **71/80**. Kalan tek oda Second
Floor. Garden ve Balcony'de olduğu gibi Patio PlayMode testleri de bilerek
ertelendi.

### Balcony kapandı — 10/10, 4 Eylül 2026

Odanın on modeli de yeniden yazıldı ve **on üründen onuna** kedi etkileşimi
verildi. Balcony v1 (20 Ağustos) bilerek rutinsiz gönderilmişti.

- **AGENTS'taki v1 kuralıyla çelişki çözüldü.** O bölüm "Bespoke aktivite yok"
  ve "balcony-özel validator eklenmedi — YASAK" diyor; 2 Eylül dalga 3 kararı
  ise 80 ürünün hepsini kapsıyor ve her ürüne rutin + validator girdisi + test
  şart koşuyor. v1 satırı o sürümün ne gönderdiğini anlatıyor, kalıcı yasak
  değil. `ValidateBalconyRoom` yazıldı; oda artık diğer beşiyle aynı kontratta.
- **Odanın imza rutini yeni bir sınıf: `BirdFeederShakeActivity`.** Projede
  kedinin bir ürünü **alttan** çalıştırdığı tek rutin — tünek, tırmanış ve
  uykuların hepsi kediyi ürünün üstüne çıkarıyor. Burada kedi güvertede kalıyor,
  arka ayakları üstünde doğrulup yemliği patlıyor ve tohum yağıyor. Yemlik ve
  tohum kendi pivotlarında asılı, ikisi de ayrı tek-nesneli FBX.
- **İki eski premium ürün `facesBackward` listesinden çıkarıldı.**
  `BalconyCushionBench` ve `BalconyHangingChair` eski mesh'leri ön yüzü +Z'ye
  yazıldığı için listedeydi; yeniden yazımda ön yüz -Z'ye alındı ve ikisi de
  çıkarıldı — Kitchen'ın dört modelinde yapılanın aynısı. Yerlerine
  `BalconyRailingFlowers` ve `BalconyLanternString` girdi (ikisi de yaw 180
  duvar kenarı).
- **Ölçü disiplini bu odada farklı: Balcony `FitFixtureModel`'den geçmiyor.**
  Mesh tam katalog ölçüsünde yazılmak zorunda ve taşan bir model taşarak
  gönderiliyor. On modelin **altısı** ilk derlemede taştı ve elle budandı:
  tentenin finial'leri ve püskülü, ot rafının tepe saksıları ve çerçeve
  başlıkları, bankın yastıkları, sehpanın tepsi kulpları, saksı kutusunun
  küreği ve bandı, korkuluk kutusunun kancaları. Bunu kilitleyen yeni bir test
  var: `BalconyProducts_AreAuthoredInsideTheirCatalogBox`.
- **`kit.torus`'un `scale` parametresi elips yapmıyor.** Ölçek düz döndürmeden
  önce uygulanıyor, yani Z'de 0.20 vermek halkayı Z'de 1.52 genişliğinde
  bırakıyor ve 0.35'lik bir sözleşmeyi 1.55'e çıkarıyor. Korkuluk kutusunda
  yakalandı, yerine çubuk kullanıldı.
- **Sehpa da komodinin faturasını ödedi:** bardak `KnockOffActivity`'nin konusu
  olduğu için kaldırılamıyordu, 0.43'lük tepsi bardağın ağzını 0.58'e çıkarıp
  0.50 sözleşmesini deliyordu. Tepsi 0.345'e indi.
- **Ulaşılamayan iki ürün dürüstçe bakış rutini aldı.** Tente 2.24'e, fener
  dizisi 1.92'ye asılıyor ve kendi kutuları 0.71 / 0.30; kedinin patiyle
  değebileceği hiçbir şey yok. Plandaki "ip püskülüne pati" fikri bu yüzden
  bırakıldı. Korkuluk çiçekleri ise gerçekten erişilebilir olduğu için pati
  vuruşu aldı — aradaki fark dosyalarda yazılı.
- Oda QA: `2026-09-04_BalconyTen_InRoom_QA.png`, `..._BalconyLeftSide_...`,
  `..._BalconyRightSide_...`. Yakaladığı hata: korkuluk çiçek kutusu ray
  kancalarıyla modellenmişti ama katalog ona `HungHeight` vermiyor — güvertede
  duruyor. Kancalar ayağa çevrildi. **Bağlantı parçasını modellemeden önce
  placement kind'ı oku.**
- Doğrulama: EditMode **370/370**, `LevelContentValidator` 0 hata / 0 uyarı,
  Console temiz.
- **Açık iş:** Balcony PlayMode testleri yazılmadı (Garden ile aynı erteleme).

Balcony: **10 bitti, 0 kaldı.** Altı odanın içeriği tamam — **63/80**.

## Aktif kalan iş — dalga 3 kapandıktan sonra (4 Eylül 2026)

Dalga 3 model + rutin işi bitti (**80/80**). Ayrıntı: `Docs/CatHome_Checkpoint_2026-09-04.md`.

1. ~~**PlayMode kuyruğu**~~ — **KAPANDI (4 Eylül 2026): PlayMode 39/39.**
   Dördüncü elle koşuda `Run All` tamamen yeşil: on yedi fixture, sıfır
   kırılma, 83,6 sn. Böylece dalga 3'ün son açık işi bitti — sekiz odanın
   seksen ürünü hem EditMode geometri testlerinden hem de PlayMode davranış
   testlerinden geçmiş durumda. Aşağıdaki teşhis kaydı, aynı tuzaklar tekrar
   kurulmasın diye duruyor.

   Garden, Balcony, Patio ve Second Floor turları (`GardenActivityTests`,
   `BalconyActivityTests`, `PatioActivityTests`, `SecondFloorActivityTests`)
   4 Eylül'de Test Runner'dan dört kez elle koşuldu. Hiçbir turda kırılmanın
   nedeni ürün ya da rutin olmadı; üçü de test altyapısıydı.
   - **1. tur (4 geçti, 6 kırıldı) — yanlış assert.**
     `cat.transform.parent Is.Null`. Her oda sahnesi kediyi
     `03 Character/CatRoot` altında kuruyor, yani parent hiçbir zaman boş
     değil. Assert artık rutin öncesi parent'ı kaydedip değişmediğini
     doğruluyor. Aynı yanlış satır koşulmamış hâlde
     `BathroomPropActivityTests`, `BedroomActivityTests`,
     `KitchenActivityTests` ve `TowelNestTests` içinde de duruyordu; dördü de
     düzeltildi. Reddedilen bir `TryStart`'ın nedenini (sahiplik / enerji /
     başka aktivite) göstermek için hata mesajına tanı bilgisi eklendi.
   - **2. tur (9 geçti, 1 kırıldı) — Garden'ın kendi susuzluk sistemi.**
     Garden, kendi `HungerSystem`/`ThirstSystem`/`EnergySystem`'ini kuran
     **tek** oda sahnesi; susuzluk dolu başlıyor ve `BirdBathSip`
     (`SinkSipActivity`, 96 üstünde reddeder) haklı olarak "I AM NOT THIRSTY!"
     diyor. Patio'nun aynı `FountainSip`'i geçmişti çünkü Patio sahnesinde
     `ThirstSystem` yok ve kapı hiç çalışmıyor.
   - **3. tur — `Run All`, 39 testin 17'si kırıldı; dört yeni dosya temizdi.**
     Garden turu dahil dört odanın turları geçti; kırılanlar 2 Eylül'den beri
     yazılı duran **eski** dosyalardı: Bathroom 6, Kitchen 3, Bedroom 2,
     `PaperSpinTests`, `SinkSipTests`, `SwingRideTests`, `TowelNestTests` 2 ve
     Garden'ın yumak testi. İki gerçek kök neden çıktı:
     1. **Oda sahnesini Single yüklemek `DirectLevelPlayBootstrap`'i
        tetikliyor.** Bu, tasarımcının o sahneden Play'e basmasıyla aynı
        görünüyor, dolayısıyla `GameScene` additive ekleniyor; birkaç kare
        sonra `LevelLoader` kayıt dosyasını uyguluyor. `SwingRideTests`'in
        "salıncak satın alınmayı bekler" assert'i bu yüzden kırıldı: fixture
        sahipliği sıfırladıktan **sonra** kayıt geri geliyordu. Aynı yarış,
        `CatHome_UI` ile gelen gerçek `EnergySystem`'in bazen zamanında
        görünüp bazen görünmemesine yol açıyordu — yani bu dosyalar hiçbir
        zaman gerçekten yeşil olmamıştı, sadece bazen geçiyorlardı.
     2. **Enerji sağlaması eksikti.** `ShowerRinseTests`, `CanopyNapTests` ve
        `SwingRideTests` kendi `EnergySystem`'ini kuruyordu; sonra yazılan altı
        dosya kurmuyordu ve `CatActivity.TryStart` enerji sistemi yokken her
        rutini reddediyor.
     Çözüm: yeni `RoomPlayModeSupport` (redirect'i kapatır, odayı tek başına
     yükler, enerji 40 / susuzluk 35 / açlık 35 sağlar, hareket kilidinin
     bırakılmasını sınırlı süre bekler) ve tek satırlık üretim kancası
     `DirectLevelPlayBootstrap.RedirectSuppressed`. Oda yükleyen **on üç**
     fixture bu yardımcıya bağlandı. Yumak testinin kilit assert'i de bu
     bekleme ile düzeltildi: kovalama bir pounce ile bitiyor ve o reaction,
     rutin bittiğini bildirdikten sonra kilidi bir an daha tutuyor.
   Geometri EditMode'da kilitli. MCP üzerinden PlayMode koşulmaz; Test
   Runner'dan elle çalıştırılır.
2. ~~**Katalog yerleşim notları**~~ — **KAPANDI (5 Eylül 2026).** İki Patio
   yerleşim kusuru da düzeltildi; EditMode 381/381, `LevelContentValidator`
   0 hata / 0 uyarı, Patio QA çekimleri yenilendi.
   - **`PatioStringLights` artık asılı değil, kendi direklerinde duruyor.**
     Sorun yerleşim değil sözleşmeydi: ürün 1.95'te `WallEdge` olarak
     gönderilmişti ama Patio'nun arka sınırı 0.63 alçak duvar + 1.00 çit ve
     oradaki tek yüksek yapı `PatioPergolaArch`, yani ayrı bir satın alma.
     Kemeri almayan oyuncuda dize havada duruyordu. Blender modeli iki direk,
     kurdele bağlar ve kısa/uzun almaşık ampul inişleriyle yeniden yazıldı;
     bunting silindi (ampullerle aynı bantta bulamaç okuyordu, altına indirmek
     uçları 1.37'ye yani pati menziline çekiyordu). Sözleşme
     1.60 × 0.30 × 0.30 `WallEdge`/hung 1.95 yerine **1.40 × 2.10 × 0.30
     `Floor`**, yerleşim **(1.95, 0, 2.45)**, yaw 180 ve `facesBackward`
     korundu. Ölçülen dünya kutusu x 1.255..2.645, z 2.373..2.590: kemere
     0.069, sağ köşe saksısına 0.055, alçak duvara 0.020 pay.
     `GazeLookPoint` ampul bandına (y 1.574) taşındı; en alçak ampul 1.434'te,
     yani rutin hâlâ dürüst bir gaze. `HomeStoreService.GetPlacementFamilyLabel`
     WALL yerine FLOOR döndürüyor (Patio artık 8 floor / 2 wall).
   - **`PatioPottedFerns` salıncağın önüne alındı: (2.95, 0, −1.7).** Eski
     (2.95, 0, 1.5) hem salıncak ayak iziyle (z .65–1.35) hem kabuğun sağ köşe
     saksısıyla (x 2.7–3.3, z 1.7–2.3) çakışıyordu ve salıncak iskeleti saksıyı
     oda kamerasından tamamen gizliyordu. Salıncağın arkasındaki sağ kenar
     şeridi 0.35 derin, saksı 0.75 istiyor — yana değil öne taşındı. Model
     değişmedi; `-X` yaklaşımı korunduğu için
     `Patio_ApproachesEveryProductFromTheCourtyard` aynen geçiyor. Ölçülen kutu
     x 2.617..3.310, z −2.041..−1.370: fıskiyeye 0.486, ön duvara 0.769, sağ
     duvara 0.30 pay.
   - Kayıtları kırmıyor: `HomeStorePlacementEntry` yalnız oyuncunun kendi
     taşıdığı ürünler için yazılır, varsayılan yerleşim katalogdan okunur.
3. ~~**Mağaza kart görselleri ürünle eşleşmiyor**~~ — **KAPANDI (5 Eylül 2026).**
   Kullanıcı mağazada gördüğü eşyanın odaya konan eşyaya benzemediğini bildirdi.
   İki ayrı kök neden çıktı:
   - `StoreCatalogPreviewBuilder` kartı **ürünün gerçek prefab'ından değil**
     `SourceAssetPath`'ten çiziyordu. `Custom`/`Pet`/`Room` girdilerinin 28'i
     ham FBX ya da üçüncü parti pack prefab'ına bakıyor; oysa
     `StoreProductContentBuilder` gerçek ürünü kurarken üstüne kendi görselini,
     ölçeğini ve ofsetini uyguluyor. Yani mağaza, oyuncunun **hiç almadığı** bir
     nesnenin fotoğrafını gösteriyordu: eski pack koltuğu ve lambası, yüzü yukarı
     yazılmış `RoundWallClock.fbx` (boş altın disk) ve tablasız `SideTable.fbx`.
     Artık her kart `Assets/Art/StoreProducts/Prefabs/{PrefabName}.prefab`'tan
     çekiliyor; prefab ölçeği zaten taşıdığı için `VisualScale`/`VisualOffset`
     ikinci kez uygulanmıyor (pack ürünlerinde 7x'e kadar çıkıyordu).
   - Yeniden yazılan modellerin kartları hiç tazelenmemişti, çünkü üretim yolu
     `BuildMissingAndBedroomPreviews()` yalnız **eksik** kartı çiziyor. Dalga 2 ve
     3 bu yüzden eski görsellerle gönderilmiş. 101 kartın tamamı `BuildAll()` ile
     yeniden basıldı.

   Ayrıca on ürün kartı nesnenin **arkasını** gösteriyordu; ön izleme kamerası
   hep prefab'ın -Z tarafında durduğu için bunlar 208° dönüşe alındı
   (`LoftTallBookcase`, `LoftWallGallery`, `BedroomDreamArt`, `TallBookshelf`,
   `ModernPainting`, `ModernTelevision`, `TvUnit`, `WallMirror`,
   `RetroTelevision`, `RoundWallClock`). Bu küme `facesBackward` ile aynı
   **değil**: banyo armatürleri o listede oldukları hâlde 28°'de doğru
   fotoğraflanıyor. Kartların tümü kontakt sayfasıyla gözle denetlendi.

   **Açık kalan (kozmetik):** `BalconyLanternString` ve `PatioStringLights` gibi
   geniş-ama-sığ ürünler 28°'de ince bir çapraz olarak okunuyor ve kartın
   dörtte birini dolduruyor. Bu kümeye ayrı bir ön izleme açısı gerekir.

4. ~~**Halılar kediyi durduruyor**~~ — **KAPANDI (5 Eylül 2026).** Kedinin
   `CharacterController`'ı 0.01 step offset ile yazılmış (bilerek: tırmanma
   rutini olmayan mobilyaya çıkmasın diye), dolayısıyla bir santimden yüksek her
   **katı** collider duvar demek. Altı zemin halısının hepsi 0.080 boyunda ve
   katıydı — yani oyundaki her halı kedinin çarptığı sekiz santimlik bir eşikti.
   Sessizce geçmişti çünkü `MatKneadActivity` kediyi halının **yanındaki**
   anchor'dan sürüyor. `StoreProductContentBuilder.ConfigureProductCollider`
   artık 0.12 ve altındaki zemin ürünlerini trigger yapıyor — kum kabı, duş,
   tünel, yıldız çadır, hamak, salıncak ve şezlongun zaten elle yaptığı çağrının
   aynısı. Eşik `WalkingLeash`'in 0.150'sinin altında, o katı kalıyor.
   `ActivityUnlockTests.FloorMats_AreTriggersSoTheCatCanWalkOverThem` kilitliyor.

5. **KAPANDI — kedi ırkı değişiminde animasyon (5 Eylül 2026).**
   `Destroy` eski görseli kare sonunda kaldırır. `ApplyImmediately` aynı karede
   tekrar taramaya izin verdiği için `GetComponentInChildren<Animator>(true)`
   henüz silinmemiş, pasif eski modeli yeniden seçiyordu. İkinci replacement
   onun pasifliğini kopyalıyor ve altı oyun bileşeni görünmeyen animatöre
   bağlanıyordu. Canlı Play'de seçim + aynı karede `Update` ile ölçüldü:
   kedinin altında iki animatör; görünür olan aktif, hareketin bağlı olduğu pasif.
   EditMode'daki tek seferlik klip örneklemesi bu yaşam döngüsünü yakalayamaz.

   `ApplyToOwner` artık doğrulanmış yeni model hazır olduğunda eski **görsel**
   kökünü pasifleştirip sahibinden ayırıyor, sonra `Destroy` ediyor ve oyun
   bileşenlerini yeni animatöre bağlıyor. `CatRoot` parent'ı değişmez.
   Anlık seçim sonraki periyodik taramayı 0.25 saniye ileri alır. Aynı karede
   birden fazla seçim de yalnız son modeli bırakır; pasif oda kedileri korunur.
   Ortak klipler ve `NormalizeAnimationBindingRoot` aynen kalır; avatar,
   eksik klip veya `optimizeGameObjects` sorunu değildir.

   `CatBreedSwapTests` artık beş senaryo içerir: on ırk ve geri dönüş, tek görsel,
   seçimden hemen sonra tarama, aynı karede art arda seçimler, pasif sahibin
   yeniden etkinleşmesi. Altı animatör tüketicisinin görünür kediye bağlılığı
   kontrol edilir. Düzeltme öncesi Test Runner: **3 geçti / 2 başarısız**;
   iki yeni zamanlama testi kusuru yakaladı. Düzeltme sonrası **PlayMode 44/44**
   (ırk testleri 5/5), **EditMode 382/382**, validator **0 hata / 0 uyarı**.
   Normal Play'de mağazanın kart/USE THIS CAT/kapatma düğmelerinin `onClick`
   akışıyla Persian seçildi. `Animator.Update` çağırmadan joystick girdisiyle
   45 gerçek karede 0.696 birim yürüyüş / 11.06° omurga hareketi; eski
   Maine Coon seçimine dönüşte 0.692 birim / 7.04° ölçüldü. Her seferinde tek
   animatör, altı home tüketicisinde doğru referans; Console temiz.
   Üç ana sahne, Living Room aktif, kamera/listener/EventSystem **1/1/1**,
   `playModeStartScene = null` ve `DisableSceneReload` geri yüklendi.
   Test çıktısı `Temp/BreedSwapFix/PlayMode.xml`, canlı ölçüm `LiveQA.txt`.
   Git commit/push yok.
   **PlayMode testleri Test Runner'dan çalıştırılır; MCP üzerinden başlatılmaz.**

   **Gölge temizliği (5 Eylül 2026):** Kullanıcı isteğiyle kedilerin altındaki
   gri yuvarlak temas gölgesi kaldırıldı. Yalnız bu efekti kuran
   `CatShadowController` ve `SoftCatShadow.shader` (meta dosyalarıyla) silindi;
   sahne/prefab referansları olmadığı doğrulandı. Gerçek model gölgeleri korunur.
   Normal Play'de on ırkın tamamında yapay gölge yok, mesh gölgesi açık ve tek
   aktif animatör var; önceki Sphynx seçimi geri yüklendi. Ev ekranı görsel
   kontrolü geçti; kamera/listener/EventSystem **1/1/1**, validator **0/0**,
   Console temiz. Görsel: `Temp/CatShadowRemoval/HomeWithoutSoftShadow-1.png`.

6. **Gerçek ekonomi kapısı:** içerik ve yerleşim QA'sı bittikten sonra mevcut
   `FREE TEST / GET` akışından kanonik Coin harcamasına geç. Bu adıma kadar
   `EconomyChecksEnabled = false` kalır.
7. **Yeni bölüm/oyun içeriği:** eşya sanatı ve ekonomi sabitlendikten sonra yeni
   oda ya da üçüncü gerçek mini oyun seçilir. Tam tasarlanmamış bir `COMING SOON`
   kartı eklenmez.

Yayın kapısı işleri aktif içerik listesinin sonunda kalır: metin dondurma ve tam
yerelleştirme, Android gerçek cihaz deep-link, mobil performans/uzun oturum,
Player Care kanonik alan + Dashboard/mağaza URL'leri ve gerçek IAP/mağaza doğrulaması.

### Checkpoint — 5 Eylül 2026 (canlı kedi açılışı ve HD netlik)

Kullanıcının isteğiyle açılıştaki statik kedi fotoğrafı gerçek oyun ırklarından oluşan canlı 3D sahneye geçti. Seçili kedi önde, iki arkadaşı yanında; HD çizim, hareket azaltma, odak ve kapanış yaşam döngüsü uygulanıyor. Menü dock'u SafeArea ölçeklemesi kullanır. Sekiz oda + iki mini oyunda kenar yumuşatma ve yumuşak gölge ayrıntısı yükseltildi. Oda/HOME fotoğrafları gerçek 1920×1080; dış mekânları beyazlatan eski parlaklık işlemi kaldırıldı. Test ve görsel kanıtların kaydı `PREMIUM_HD_AUDIT.md` içindedir. FREE TEST, ekonomi ve kayıt sözleşmeleri değişmedi. Git commit/push yok.

Tamamlandı: EditMode **402/402**; tam PlayMode **56/56** (800/800 eşya-ırk eşleşmesi dahil), son açılış/ışık testleri **4/4**. Validator **0/0**, normal Play Console temiz. Açılış 16:9, 4:3 ve 20:9; ev, HOME mağazası ve oda seçici görsel kontrolü geçti. Mağaza metinleri fiyat/eylem alanından 20 px ayrıldı. Üç ana sahne ve tek kamera ile ev önizlemesi geri yüklendi. Gerçek Android cihaz performans ölçümü yayın kapısında kalır.

### Checkpoint — 6 Eylül 2026 (onaylanan modern UI/UX uygulaması)

Kullanıcının onayladığı altı referans ekran ve önceki incelemenin tüm oyuncu yüzeyleri uygulandı: canlı kedi açılışı, ev HUD/menü/dock/bağlamsal eylemler, mağaza ve satın alma katmanları, Kedim, sekiz oda, görev/günlük/geri dönüş/kutlamalar, hesap/ayar/gizlilik/tanışma, oyun seçimi/sıralama ve Runner/Catch giriş-HUD-öğretici-duraklatma-sonuç-can yok durumları. Kanonik görünüm ivory/mint, turkuaz seçim, mercan ana eylem ve ink yazıdır; eski neon/rainbow zorunlulukları bu kullanıcı kararıyla geçersizdir. Gerçek mevcut kediler, eşyalar ve odalar kullanılır.

HD ürün/oda/oyun önizlemeleri; 16:9, 4:3 ve 20:9 düzenler; TR/EN ana akışlar incelendi. Dar ekranda ihtiyaçlar ikinci sıraya geçer, mağaza tek kompozisyon olarak ölçeklenir. Birden fazla kedi önizlemesinin birbirine karışması, pasif MainPanel bağlaması, kartları örten mağaza arka planı, modal arkasındaki ev düğmeleri, title Canvas'ına bağlı konuşma ve gizli aktivite ilerleme kartı düzeltildi.

Son tam EditMode **405/405**. Native tam PlayMode ilk koşusu **56/57**; bulunan MainPanel kusurunun tekrar koşusu **1/1**, yeni önizleme testi **1/1**, son ev akışı tekrar kontrolü **5/5**. Böylece çalışmadaki **58 ayrı oyun testi başarılı**; tam koşudaki eşya/ırk matrisi **800/800**. Sonuçlar ayrı XML'lerde korunur. Ekonomi FREE TEST kalır; gerçek hesap, ödeme, reklam ve ödül işlemleri QA için çağrılmadı. QA kayıt kopyası ve çevrimiçi gönderim engeli kullanıldı.

Kalıcı teslim: `Docs/UIUX_IMPLEMENTATION_2026-09-06.md`; gerçek HD görüntüler ve yerleşim/test kanıtları `Docs/QA/UIUX_2026-09-06/index.html`. Önceki inceleme tarihsel rapor olarak korunur. Gerçek Android performansı/çentik/klavye ve platform sağlayıcısı doğrulaması mevcut yayın kapısında kalır. Git commit/push kullanıcıya aittir.

### Checkpoint — 6 Eylül 2026 (geri dönüş tıklaması ve ikinci UI kalite geçişi)

Kullanıcının bildirdiği geri dönüş penceresi kilidi düzeltildi: builder, düğmenin adı değişince gerçek tıklama yüzeyini kapatıyordu. Artık `Button.targetGraphic` ilişkisi korunur; native fareyle kapanış ve kedi kontrolünün bırakılması doğrulandı. Başlıklarda Fredoka Medium, küçük yazıda Nunito Sans SemiBold; 2048 SDF atlasları, sentetik Bold'dan arındırılmış tipografi, yumuşak panel derinliği ve daha rahat açılış/oda/oyun/geri dönüş düzenleri uygulandı. Oda rozetlerinin dil değişimi de düzeltildi.

Bu geçişin kontrolleri: tam EditMode **405/405**, native geri dönüş pointer testi **1/1**, ev/oyun geçişleri **5/5**. Galeri **49** gerçek HD kare içerir; 16:9, 4:3, 20:9 ve seçili TR/EN ekranlar incelendi. Geometri ölçümünde çakışma/taşma yok; menü Scrim'i arkasındaki beklenen 6 düğme engeli ham kayıtta açıklanır. Önceki tam PlayMode matrisi yeniden koşulmuş gibi sayılmaz. Güncel kayıt `Docs/UIUX_REFINEMENT_2026-09-06.md`, galeri `Docs/QA/UIUX_2026-09-06_Refinement/index.html`. QA ayrı kayıt kullanır; git commit/push yok.

### Checkpoint — 6 Eylül 2026 (referansa uyum: gerçek 3D simgeler ve ortak yüzey)

Kullanıcının son görsel geri bildirimiyle 7 arayüz simgesi headless Blender’da modellendi; 768 px şeffaf sanat varlıkları HUD, mağaza, joystick ve ana menü kısayollarına uygulandı. Katmanlı krem/şampanya kenarlar, analitik kenar yumuşatma, daha büyük ve belirgin eylemler, responsive alt menü ve pencere arkasında dünya kamerasından tek seferlik Gauss bulanıklığı eklendi. Düz duvarlardaki AO deseni temizlendi; salon yumuşak dolgu ve sekiz oda ortak kamera yönü aldı. Oda fotoğrafları yeniden çekildi.

Fredoka'nın bazı Türkçe harfleri hiç içermediği ve fallback yüzünden aynı kelimede farklı ağırlık oluştuğu gözle bulundu. Harfler kendi aksanlarından tamamlandı; gerçek 500/600 statik fontlar, SDF atlasları ve fallback kapalı iki test eklendi. Son tam EditMode **407/407**. Bu geçişin native PlayMode ev/oyun ve geri dönüş pointer testleri **6/6**; son font/filtre sonrasında normal Play turu ve sahne EventSystem pointer kapanışı da doğrulandı. Son OS fare denemesinde Unity odağı alınamadı; başarılı manuel tıklama olarak sayılmaz.

**53 doğal HD kare**, TR/EN ve 16:9/4:3/20:9; çakışma/taşma **0/0**, açıklanmış hamburger arkası 6 engel dışında pointer sorunu yok. Son normal Play 3 sahne, kamera/listener/EventSystem **1/1/1**, kontrol serbest, Console temiz. QA kapalı, üçlü ev önizlemesi ve validator **0/0**. Gerçek Android AO maliyeti henüz ölçülmedi. Güncel kayıt `Docs/UIUX_REFERENCE_MATCH_2026-09-06.md`; önce/sonra kaydırıcılı galeri `Docs/QA/UIUX_2026-09-06_ReferenceMatch/index.html`. Git commit/push yok.
