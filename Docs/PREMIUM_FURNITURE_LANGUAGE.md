# Premium Mobilya Dili — Cat Home
Tarih: 2026-09-02. Karar: **80 oda ürününün tamamı** bu dile getirilecek.

Bu belge, 5 üründe (BalconySunAwning, PatioPergolaArch, BedroomStarCanopy,
PatioPorchSwing, KitchenSinkCabinet) kurulan reçetenin sözleşme hâli. Yeni bir
ürün yazarken buraya bakılır; buradan sapan bir ürün aileye girmez.

## 1. Hat

Blender 5.2, **her zaman headless**:

```
blender --background --factory-startup --python build_<product>.py
blender --background --factory-startup --python build_<product>.py -- --export
```

Açık Blender oturumuna asla dokunulmaz, Blender MCP kullanılmaz. Kaynak
`ArtSource/Blender/PremiumFurniture/`, ürün başına bir `build_*.py`.

- `premium_kit.py` — yapı taşları ve export
- `premium_preview.py` — onay render'ları (hero, ön, siluet)
- Export hedefi `CatHome/Assets/Art/PremiumFurniture/Models/<Product>_Premium.fbx`

## 2. Palet

Krem/beyaz gövde · altın donanım · aqua/mint/coral/pembe/lilac aksan · teal
yalnız derinlik için (evye tabanı gibi).

Renk Blender'da bitmez: FBX malzeme slot adları **`CH_*` olarak kalır**,
`StoreProductContentBuilder.ReplaceMaterials` onları mevcut URP materyalleriyle
değiştirir. Tek renk kaynağı Unity'dir. `premium_kit.CH_COLORS` yalnız önizleme
render'ının odaya benzemesi içindir; değerler `Assets/Art/StoreProducts/Materials/CH_*.mat`
dosyalarından sRGB olarak alınıp lineer'e çevrilir.

## 3. Geometri kuralları

- **Düz kutu yok.** Her parça pahlı. Yapısal parçalarda `segments=BEVEL` (3);
  minderlerde daha yumuşak pah (`segments=4`, geniş `width`).
- **Kumaş gerçek yüzeydir.** `kit.sheet(cols, rows, point_at, materials, band_for)`
  ile parametrik olarak üretilir, `solidify` kalınlık verir. Tente sarkması,
  pergola kemeri, çadır sargısı, çay bezi hep budur.
- **Vekil geometri yok.** Zincir gerçek halkadır (dönüşümlü 90° çevrili torus),
  yıldız gerçek beş köşedir, musluk kavisi silindir dilimleridir. Küre koyup
  "olmuş sayalım" yok.
- **Ortak yapı taşları taşınır.** Yeni bir form birden fazla üründe işe
  yarayacaksa `premium_kit.py`'ye taşınır: `strut` (iki nokta arası silindir),
  `sheet`, `torus` (segment sayısı ayarlanabilir), `paw_badge`.
- **Silüet sınavı.** `<Product>_Silhouette.png` render'ında ürünün ne olduğu tek
  bakışta okunmalı. Okunmuyorsa form yanlıştır, detay eklemek kurtarmaz.
- **Altın pati imzası** her üründe bulunur, durduğu yüzeye uygun yönde
  (`kit.paw_badge` dikey yüzeyler için; yatay yüzeyde ürünün kendi düz varyantı).

## 4. Ölçü sözleşmesi

`StoreCatalogAssets.cs` okunur, model o kutuya yazılır:

- **Bathroom / Kitchen / Bedroom / Garden** modelleri `FitFixtureModel`'den
  geçer: en dar orana göre `× 0.96` üniform ölçek, X/Z ortalanır, Y zemine
  oturur. Ölçü oransal yazılır.
- **Patio / Balcony / Loft (Second Floor)** modelleri geçmez: mesh **tam katalog
  ölçüsünde** yazılır, taban y = 0.
- Ürünün hareketli parçası varsa **iki ayrı tek nesneli FBX** olarak çıkar
  (`<Product>_Premium.fbx` + örn. `<Product>Seat_Premium.fbx`), ortak orijinde.
  Tek FBX'e iki nesne koyulmaz: Blender o zaman Y-up dönüşümünü mesh'e gömmez,
  her çocuğa −90° X transform yazar ve ürün sırt üstü import olur.

**Değişmeyecekler:** footprint, placement kind, katalog yaw'ı, fiyat, ownership
ve save kimlikleri.

## 5. Yön

Oda kameraları `(-1, 3, -5.5)` konumunda ve **+Z'ye** bakar. Oyuncunun gördüğü
yüz dünya `-Z`'sidir.

- Katalog yaw'ı 0 ise: ön yüz **-Z'ye** yazılır, `facesBackward` **eklenmez**.
- Katalog yaw'ı 180 ise: duvar tarafı **+Z'ye** yazılır ve ürün `facesBackward`'a
  **eklenir** (180 + 180 = kimlik).
- Eski Bathroom/Kitchen/Bedroom premium modelleri ters konvansiyonda: ön yüzü
  +Z'ye yazılıp `facesBackward` ile çevriliyorlar. İkisi de aynı sonucu verir;
  yeni modeller yukarıdaki kuralı kullanır.

Yön **tahmin edilmez, ölçülür**: import sonrası her submesh'in ağırlık merkezi
alınır ve sadece ön yüzde bulunan malzemelerin (yastık, çiçek, kulp) ortalama Z
değerinin negatif olduğu doğrulanır.

## 6. Etkileşim (zorunlu)

**3 Eylül 2026'dan itibaren yeni tasarlanan her eşya kediyle gerçek bir
etkileşim taşır.** Sadece güzel bir model bitmiş iş sayılmaz; etkileşim,
silüetle birlikte tasarım anında seçilir. `CatActivity` türetilir
(`TunnelPlayActivity`, `CanopyNapActivity`, `SwingRideActivity`,
`ShowerRinseActivity`).

- Kedi kendi başına mobilyaya çıkamaz: `CatMovement`'ta zıplama yok ve
  `stepOffset` dünya ölçüsünde 0.005. Aktivite `CharacterController`'ı kapatır,
  transform'u sürer, bitince fizik/ölçek/hareket kilidini iade eder ve kediyi
  **footprint dışında** bırakır.
- Kedi **asla** ürünün altına `SetParent` edilmez — çalışma zamanında sahne
  nesnesini prefab instance'ının altına taşımak PlayMode testinde editörü
  kilitliyor. Hareketli parçaya bindirmek için kedinin transform'u, o parçanın
  pivot'u altındaki bir noktaya her kare yazılır.
- Girilebilir/binilebilir ürün `AddProductCollider` kutusunu `isTrigger` yapar.
  Aksi halde katalog boyunda tek som kutu, modelde açık görünen boşluğu da
  kapatır.
- Ürün başına kablolama: aktivite sınıfı, `CatActivityKind` + `QuestType`
  (**sona eklenir, sıra değişmez**), `StoreProductContentBuilder` içinde attach
  yardımcısı, `LevelContentValidator`'da mağaza kimliğine bağlı kayıt, prefab'ı
  doğrulayan EditMode testi ve rutini doğrulayan PlayMode testi.
- Hareketli parçası olan üründe pivot **ölçülerek** yerleştirilir. FBX importu
  X'i çeviriyor, `facesBackward` model child'ı bir kez daha çeviriyor: yazım
  uzayı ile root uzayı arasında net olarak **yalnız Z** ters döner. Hareketli
  mesh ürünün origin'inde yazıldığı için child'ın `localPosition`'ı pivot
  ötelemesini iptal etmelidir (`-axis`). Doğrulama: pivotun X'i ile mesh'in
  gerçek merkezi eşit çıkmalı.

### Kit tuzakları (ölçülerek bulundu)

- `kit.cylinder`'a `rotation` verilirse `axis` **tamamen geçersiz kalır**; taban
  dönüşün yerine geçer. Eğimli dikey parça `(pi/2, 0, lean)`, X ekseni
  `(0, pi/2, lean)`.
- `kit.torus`'un `rotation`'ı da tabanı ezer: normali X olan halka için
  `(0, pi/2, 0)`. Z ile döndürmek halkayı XY düzleminde bırakır.
- `kit.shell()` yalnız yan duvar loft'lar, kapak üretmez. Kapalı üst yüzey
  gerekiyorsa ezilmiş küre ya da beveled cube kullan.
- Yay/boru için `kit.tube(parts, name, points, radius, material)`: aralıklı
  küreler ürün ölçeğinde boncuk dizisi okur.
- **Sözleşme ölçüsü propu da kapsar.** Katalog yüksekliği modelin toplam
  bounding box'ı, gövdesi değil. Tepeye konan saksı `BathroomTowelStorage`'ı
  1.82 yerine 2.241 yapmıştı; sığdırmanın tek yolu gövdeyi kısaltmaktı, o da
  tam boy dolabın silüetini bozuyordu. Tepe propu yerine gövdenin **içindeki**
  görünür bir rafa koy.
- **Prop etkileşimin kendisiyse faturayı gövde öder.** `BedroomNightstand`'da
  bardak `KnockOffActivity`'nin konusu; kaldırılamaz. İlk geçişte 0.72'lik tabla
  + saat + bardak, 0.72 sözleşmesine karşı 0.872 okudu. Çözüm gövdeyi kısaltmak
  oldu: `CARCASS_TOP 0.508`, tabla `0.568`, kalan ~0.15 proplara ayrıldı. Tepe
  propu vazgeçilmezse yüksekliği **önce** propa ayır, gövdeyi kalanına yaz.
- **`kit.paw_badge` yalnız dikey yüzeyler içindir.** Parmakları X ve **Y**'de
  yayıyor; yatay bir yüzeye koyunca pati ayağa kalkıp üzüm salkımı okur. Zemin,
  kum, halı gibi yatay yüzeyler için parmakları X ve **Z**'ye yayan yerel bir
  pati yaz, ve küreleri Y'de ez.
- **Pati rozeti derinlik yer.** `paw_badge`'in küreleri normal yönünde 0.028
  kalınlık ekliyor; dar footprint'li bir üründe (çamaşır sepeti 0.82) bu tek
  başına sözleşmeyi 0.05 aşırıyor. Yüzeyle aynı hizada duran **düz** pati yaz.
- **`rotation=(pi/2,0,0)` bir diski tavana baktırır.** Duvara/cama paralel bir
  disk (aplik, madalyon) `axis="Z"` ister; `rotation` verilirse `axis` zaten
  tamamen geçersiz kalıyor.
- **Kapalı okuması gereken gövdeye önce bir carcass koy.** Üst üste dizilmiş
  slat'lar tek başına 0.03'lük boşluklar bırakıyor ve ürün açık raf rafı okuyor;
  slat'ları kapalı bir kutunun üstüne bindirme olarak kullan.
- **Yuvarlak bir parça elips bir footprint'i dolduramaz.** Katalog `0.72 x 0.66`
  diyorsa daire ikisinden yalnız küçüğünü tutturur ve model kutunun içinde
  yüzer; `kit.cylinder`/`kit.torus`'un `scale` parametresiyle X'te ger.
  `KitchenCounterStool` böyle 0.61 × 0.61'den 0.73 × 0.67'ye çıktı.
- **Açık bölme, gövdeyi bölmekle olur.** Tam genişlikte bir carcass'ın içine
  sepet koyarsan bölme görünmez; carcass'ı bölmeden önce bitir.
  `KitchenIsland` ve `BathroomTowelStorage` aynı hatayı yaptı.
- **Kaide ve taç tam footprint'te, gövde içeride.** Gövdeyi sözleşme
  genişliğinde yapıp yanına ray/kulp eklersen taşarsın. Gövdeyi 0.05 kadar
  içeri al: çıkıntı gerçek okur, yan aksesuar da bütçeye sığar.
- **Bir yüzeyi süsleyen parça o yüzeyin dışına yazılır.** Bedroom'un dokuz
  modelinin beşi aynı hatayla çıktı: süs parçası, süslediği duvarın yarıçapının
  *içine* yazılmıştı ve yalnız en şişkin yeri dışarı taşıyordu. Sepet çıtaları
  (`0.296→0.350`, duvar yüzeyi `0.312→0.368`) beyaz damla dizisi okudu; tuvalet
  taburesinin pilileri (`0.258→0.280`, yüzey `0.270→0.294`) havada başlayıp biten
  çıtalar oldu; gardırobun kaide şeridi `-0.274`'te 0.014 yarıçapla `-0.290`
  yüzeyine gömüldü; komodinin tabla şeridi de öyle. **Önce yüzeyin dış
  yarıçapını hesapla (profil + `thickness`), sonra süsü onun dışına koy.**
  Sözleşme kenardaysa yüzeyi içeri al — süsü değil: sepette duvar 0.016 içeri
  alındı, çıtalar 0.732 çapla sözleşmenin altında kaldı.
- **`rotation=(pi/2,0,0)` kuralı üç üründe daha ihlal edildi.** Gece lambasının
  yıldızları, komodinin saati ve tablonun yıldızları diski tavana baktırıp
  uçlarını XY'de bırakmıştı; üçü de oda kamerasından ya filiz ya hiç okumuyordu.
  Odaya bakan her disk `axis="Z"` ister. Kural zaten yazılıydı — model yazarken
  **her disk için tek tek** kontrol et.
- **`kit.revolve`'a her zaman `thickness` ver.** `thickness=0.0` tek yüzlü bir
  kabuk üretir. Blender iki yüzü de çizdiği için onay render'ı doğru görünür;
  Unity arka yüzleri eler, kabuk kaybolur ve içindeki ne varsa görünür.
  `BedroomVanityStool`'un minderi odanın `thickness=0.0` ile yazılmış tek
  revolve'uydu: kedinin tünediği oturak Unity'de yok oldu ve altındaki beyaz
  oturak kasası minderin ortasında sert bir leke olarak okudu. Blender
  render'ında hiç görünmedi — **bunu yalnız oda içi capture yakalar.**
- **`kit.revolve` tepeyi kapatmaz.** `close_bottom` yalnız tabanı kapatır;
  profil sıfırdan büyük bir yarıçapta biterse yüzeyde o genişlikte delik kalır.
  Kapalı okuması gereken bir kubbede profili kutba kadar götür
  (`..., (0.090, TOP - 0.002), (0.0, TOP)`). Açık kalması gereken formlarda
  (abajur, sepet) sorun değil.
- **`FitFixtureModel`'den geçmeyen odalarda pay yok.** Patio, Balcony ve Loft
  modelleri tam katalog ölçüsünde yazılır; taşan model taşarak gönderilir.
  Balcony'nin on modelinin **altısı** ilk derlemede aştı: finial yarıçapı
  footprint'e eklenmişti, püskül model kutusunun altına sarkıyordu, tepe
  saksıları tavanı deliyordu, yastıklar 0.05 taşıyordu, tepsi kulpları 0.02.
  **Süs parçasının kendi yarıçapını sözleşmenin içine say**, dışına değil.
- **`kit.torus`'un `scale` parametresi elips üretmez.** Ölçek düz döndürmeden
  *önce* uygulanıyor, yani `scale=(1, 1, 0.20)` halkayı Z'de daraltmıyor —
  daralttığı eksen döndürme sonrası Y oluyor. `BalconyRailingFlowers`'ta 0.760
  yarıçaplı bir halka 0.35'lik sözleşmeyi 1.55'e çıkardı. Elips gerekiyorsa
  `kit.cylinder`'ı `scale` ile ger ya da çubuk kullan.
- **Bağlantı parçasını modellemeden önce `HomeProductPlacementKind`'ı oku.**
  `BalconyRailingFlowers` ray kancalarıyla modellenmişti; katalog ona
  `HungHeight` vermiyor, yani güvertede duruyor ve kancalar olmayan bir raya
  uzanıyordu. Kanca, ayak, askı — hepsi placement'ın sonucu, tahminin değil.
- **Ulaşılamayan ürün rutini de dürüst olmalı.** `HungHeight 2.24` + 0.71'lik
  kutu demek, kedinin patiyle değebileceği hiçbir şey yok demek. Tentede ve
  fener dizisinde beat bu yüzden bakış (`SitLookActivity`); sallama ya da
  vuruş, kedinin dokunamadığı bir şey hakkında yalan olurdu. Aynı odadaki
  korkuluk çiçekleri gerçekten 0.76'da, o yüzden **o** pati vuruşu aldı.
- **`FitFixtureModel`'den geçmeyen odalarda pay yok.** Patio, Balcony ve Loft
  modelleri tam katalog ölçüsünde yazılır; taşan model taşarak gönderilir.
  Balcony'nin on modelinin **altısı** ilk derlemede aştı: finial yarıçapı
  footprint'e eklenmişti, püskül model kutusunun altına sarkıyordu, tepe
  saksıları tavanı deliyordu, yastıklar 0.05 taşıyordu, tepsi kulpları 0.02.
  **Süs parçasının kendi yarıçapını sözleşmenin içine say**, dışına değil.
- **`kit.torus`'un `scale` parametresi elips üretmez.** Ölçek düz döndürmeden
  *önce* uygulanıyor, yani `scale=(1, 1, 0.20)` halkayı Z'de daraltmıyor —
  daralttığı eksen döndürme sonrası Y oluyor. `BalconyRailingFlowers`'ta 0.760
  yarıçaplı bir halka 0.35'lik sözleşmeyi 1.55'e çıkardı. Elips gerekiyorsa
  `kit.cylinder`'ı `scale` ile ger ya da çubuk kullan.
- **Bağlantı parçasını modellemeden önce `HomeProductPlacementKind`'ı oku.**
  `BalconyRailingFlowers` ray kancalarıyla modellenmişti; katalog ona
  `HungHeight` vermiyor, yani güvertede duruyor ve kancalar olmayan bir raya
  uzanıyordu. Kanca, ayak, askı — hepsi placement'ın sonucu, tahminin değil.
- **Ulaşılamayan ürün rutini de dürüst olmalı.** `HungHeight 2.24` + 0.71'lik
  kutu demek, kedinin patiyle değebileceği hiçbir şey yok demek. Tentede ve
  fener dizisinde beat bu yüzden bakış (`SitLookActivity`); sallama ya da
  vuruş, kedinin dokunamadığı bir şey hakkında yalan olurdu. Aynı odadaki
  korkuluk çiçekleri gerçekten 0.76'da, o yüzden **o** pati vuruşu aldı.

- **`kit.sphere`'in `scale` parametresi YARIÇAP, yarı-genişlik değil.** Merkez
  yarıçapı + `scale` = ürünün dış sınırı. `PatioWaterFountain`'in kabuk süsleri
  0.360'ta 0.056 ile duruyordu; bunu yarı-genişlik sanmak modeli 0.80'lik
  sözleşmenin 0.03 dışına taşıdı ve ancak `PatioProducts_AreAuthoredInsideTheir
  CatalogBox` yakaladı.
- **`kit.paw_badge` XY düzleminde kurulur ve parmakları hep +Y'ye açar.** Alçak
  bir üründe imzayı dikine kaldırıyor: `PatioStoneRug` 0.08'lik sözleşmede
  0.12 oldu. Düz ürüne düz pati gerekir — yassı küreleri elle diz, yukarıdan
  okunsun; halı zaten yalnız o açıdan görülür.
- **Yuvarlak bir hazneyi düz disk kapatır.** `PatioFirePit`'in taş kenarı dolu
  silindirdi ve ürünün tek sebebi olan ateşi tamamen gizliyordu. Kenar halka
  (`kit.torus`) olunca kütükler, korlar ve alevler göründü. Aynı kural: alev
  konileri kütüklerin **altından** başlamalı, üstünden değil.
- **Düşen su üç denemede oturdu.** Kâsenin ağzından leğene inen dört ayrı
  huzme, dört açılmış masa ayağıyla birebir aynı geometridir ve öyle okunur.
  Tam boy perde ise gövdeyi kapatan turkuaz bir kova olur. Doğrusu kısa,
  dışa açılan bir etek + aşağı doğru küçülen damlalar + iniş noktasında halka.
- **Yaprak, çubuğa dizilmiş küre değildir.** Hem `PatioPottedFerns` hem
  `PatioHerbTrough` ilk derlemede sap boyunca yassı küre dizdi ve ikisi de
  "çubuğa geçirilmiş çakıl" gibi okundu. Çalışan biçim: bir orta damar + ondan
  **çiftler halinde** açılan kısa çubuklar (`kit.strut`). Eğreltide `Z` bütçesi
  0.60 olduğu için yapraklar daireye değil **elipse** dizilir.
- **Rutinin hedefi görünmezse rutin yoktur.** `PatioHerbTrough`'un kazılmış
  çukuru toprakla aynı tondaydı ve kutu kapağı 0.400'de olduğu için ön ribin
  arkasında kalıyordu. Toprak `CH_Cream`, kazılan malzeme `CH_White` yapıldı ve
  kapak 0.372'ye indirildi. Oda QA çekimi bunu gösteren tek pas.

- **Eksende döndürülmüş kare büyür.** Yarı-genişliği `0.5 * span * (cos(yaw) +
  sin(yaw))` olur. `LoftFloorCushions`'ın üç minderi serbest yaw'la ölçülmüştü;
  0.90'lık sözleşme 0.98'e çıktı. Yaw veriyorsan span'i ona göre küçült.
- **`kit.paw_badge`'in ped küresi, iğnelendiği yüzeyin ötesine uzanır.** Kendi
  yarıçapı kadar dışarı taşar, o yüzden yüzeye *yaslanmaz*, içeri gömülür.
  `LoftRecordPlayer` ve `LoftStudyDesk` ikisi de bu yüzden sözleşme dışına
  çıktı ve hiçbirinde suçlu belli değildi.
- **Rafa koyduğun nesneyi kitap dizisinin aralıklarına göre yerleştir.**
  `LoftTallBookcase`'in saksısı ve saati raf koordinatına bakılarak konmuştu;
  ikisi de kitap dizisinin *içinde* kaldı ve rafa sıkışmış iki renkli blok gibi
  okundu. Diziyi span olarak tanımla, nesneyi boşluğa koy.
- **Üst raf içeriği korniş altını aşmamalı.** Alt raflara göre boyutlanan
  kitaplar `LoftTallBookcase`'i 2.10'luk sözleşmenin 0.05 dışına çıkardı. Raf
  başına yükseklik tavanı ver (`cap`), tek bir global boyut kullanma.
- **Yığının üstündeki çanak, altındaki katmandan BAŞLAMALI.** Kendi taban
  yüksekliğinden başlatılırsa 0.03 havada durur ve yığının üstünde duran ayrı
  bir kâse gibi okunur. `LoftFloorCushions` ve `LoftBeanBag` aynı hatayı
  sırayla yaptı.
- **Zincirlenmiş `kit.strut`'larda her parçaya ayrı yarıçap verme.** Her ekte
  görünür bir basamak bırakır ve gövde "ipe dizilmiş boncuk" gibi okunur.
  `LoftArcLamp`'ın gövdesi tek yarıçapla düzeldi.
- **Döndürülmüş minderin biyesi `kit.cylinder` olamaz** — çubuk yaw almaz ve
  ofsetli konumdan tam bir span dışarı uzar. Aynı yaw'ı taşıyan, biraz daha
  büyük ve çok ince bir `kit.cube` (welt) kullan.
- **`CanopyNapActivity`'nin giriş ağzını süsle kapatma.** `LoftChaiseLounge`'un
  yastığı ilk derlemede açık ayak ucundaydı; rutinin kediyi içeri soktuğu tek
  açıklığı tıkıyordu. Yastık başa gider, ayak ucu boş kalır.
- **Footprint gerçek bir yayı kaldırmıyorsa eğimi tabanı ters yöne iterek satın
  al.** `LoftArcLamp`'ın 0.70'lik ayak izinde tepe ancak 0.155 uzanabiliyordu ve
  düz bir lambader gibi okunuyordu. Taban +0.086'ya, başlık -0.196'ya alınınca
  aradaki 0.28 eğimi görünür kıldı; iki uç da tam 0.35'e oturuyor.
- **Asılı ürün, asılacak bir şey olduğunu varsayamaz — odanın kabuğunu ölç.**
  `PatioStringLights` 1.95'te `WallEdge` olarak gönderildi ama Patio'nun arka
  sınırı 0.63'lük alçak duvar ve 1.00'lık çittir; oradaki tek yüksek yapı
  `PatioPergolaArch`, yani **ayrı bir satın alma**. Kemeri almayan oyuncu
  havada duran bir ampul çubuğu görüyordu. Bir ürünün doğruluğu başka bir
  ürünün sahipliğine bağlanamaz: festoon kendi iki direğini taşıyacak şekilde
  yeniden yazıldı (1.40 × 2.10 × 0.30, `Floor`). Bahçe/veranda tipi odalarda
  `WallEdge` seçmeden önce `HomeRoomShellMetrics` ve oda builder'ının gerçek
  duvar yüksekliğine bak.
- **Aynı yükseklik bandındaki iki dize tek bir bulamaç olarak okunur.** Aynı
  festoonda ampul dizisi ve flama bunting'i 0.30'luk kutuda üst üste biniyordu;
  1.40 × 2.10'a büyüyünce bu tek bir pastel blob'a dönüştü. Bunting'i ampullerin
  altına indirmek uçları 1.37'ye çekiyordu, yani **pati menziline** — o zaman
  ürün gaze değil vuruş borçlanırdı. Çözüm ikinci dizeyi silmek, ampul iniş
  boylarını kısa/uzun almaşık yapmak ve el işi notunu direk başına bir kurdeleye
  bırakmak oldu. İki dize gerekiyorsa aralarında en az bir ampul boyu düşey
  boşluk bırak ve alt dizenin **erişim menzilini** kontrol et.

## 7. Ürün başına iş akışı

1. `StoreCatalogAssets.cs`'ten sözleşmeyi oku (footprint, height, placement, yaw)
2. `build_<product>.py` yaz, `--export` olmadan çalıştır
3. Hero + ön + siluet render'larına **bak**, kusuru bul, düzelt, tekrarla
   (tipik 3–4 tur)
4. Onaya sun
5. `--export`, `TryBuildPremiumFurnitureVisual`'a kaydet, gerekiyorsa
   `facesBackward`
6. `BuildProductAssetsSilently()`, prefab bounds + submesh yönü doğrula
7. Oda QA render'ı — sahnedeki ürünün `VisualContent`'i sahip olunmadığı için
   kapalıdır, render öncesi açılmalı; oda sahneleri aynı dünya koordinatlarını
   paylaştığı için `LivingRoom_Level01` geçici kapatılıp sonra kirletmeden
   geri açılır
8. EditMode + `LevelContentValidator` + kanonik sahne yığını + Console
9. ROADMAP checkpoint + envanter güncellemesi
