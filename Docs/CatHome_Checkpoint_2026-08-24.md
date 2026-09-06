# Cat Home — Checkpoint 24 Ağustos 2026

**Proje:** `C:\Users\HAKAN\Desktop\CatHome\CatHome`  
**Unity:** `6000.4.4f1` (`CatHome@26b7b116b75259f7`)  
**Kayıt şeması:** v11  
**Commit / push:** yok — kullanıcı manuel yapar

## Tamamlanan canlı dağıtım

- Unity Cloud production ortamında Runner/Catch için altı kanonik Leaderboards
  tanımı dağıtıldı: DAILY, WEEKLY ve ALL-TIME.
- Günlük tabloların ilk geçerli dönemi `2026-08-25T00:00:00Z`, haftalık tabloların
  ilk geçerli dönemi `2026-08-31T00:00:00Z` başlar. All-time tablolar sıfırlanmaz.
- `CatHomeCompetition.ccm` Cloud Code modülü derlendi, yüklendi, yayımlandı ve uzak
  modül listesinde doğrulandı.
- Player istemcisinin Leaderboards skorlarına doğrudan yazmasını reddeden
  access-control politikası production ortamında günceldir.
- Unity Deployment'ın çözümü bulması için sunucu projesine solution ve publish
  profili; Unity tarafına `.ccmr` modül referansı ile `.ac` deployment varlığı
  eklendi. Gizli anahtar, token veya istemci sırrı repoya yazılmadı.

## Sağlamlaştırma

- `CatHomeAuthoringWorkspace`, Test Runner'ın geçici `InitTestScene` sahnesini domain
  reload sonrasında da otomatik test geçişi olarak tanır; normal GameScene bootstrap
  politikası PlayMode test başlangıcını artık ele geçirmez.
- `CatRunnerTrackManager`, controller'dan önce çalışan update sırasını hesaba katarak
  coin bütçesini bir sonraki kare zamanıyla örnekler. Hızlandırılmış/seyrek kareli
  uzun Runner testi planlanan coin sayısının gerisinde kalmaz.

## Son QA

- EditMode: **296/296**
- PlayMode: **10/10**
- `LevelContentValidator`: **0 hata / 0 uyarı**
- Console: temiz
- Açık sahneler: `GameScene + CatHome_UI + LivingRoom_Level01`
- Aktif sahne: `LivingRoom_Level01`
- Camera / AudioListener / EventSystem: **1 / 1 / 1**
- Editor Play: `playModeStartScene = null`, `DisableSceneReload`, domain reload açık
- Test Runner kalıntısı yok; `ApplicationIdleTime=4`, `InteractionMode=0`

## Yayın öncesine kalan dış kontroller

> Kullanıcı kararı (24 Ağustos 2026): Aşağıdaki maddeler şimdilik ertelendi;
> aktif ürün geliştirme sırasına alınmayacak ve yalnız yayına geçmeden hemen önce
> birlikte tamamlanacak.

- Android gerçek cihazda tarayıcı → uygulama deep-link dönüş testi.
- Player Care gizlilik, hesap silme ve veri talebi URL'lerini Unity Dashboard ile
  mağaza formlarına bağlamak.
- Yayına yaklaşınca Player Care'i `cathome.vexorialabs.com` alan adına taşıyıp
  DNS/HTTPS, oyun, Dashboard ve mağaza URL'lerini birlikte güncellemek.
- Metin dondurmadan sonra planlanan tam yerelleştirme paketini tamamlamak.

## Devam — kedi adıyla otomatik leaderboard

- Ayrı leaderboard oyuncu adı ve `SAVE NAME` adımı kaldırıldı. Skor metadatası
  doğrudan `CatIdentityService.CatName` değerini kullanır.
- Cloud Code başarı yanıtındaki `nickname` alanı istemci modeline eklendi; önceki
  deserialization hatası giderildi.
- Production doğrulaması: `lokiş`, Runner DAILY/WEEKLY/ALL-TIME, **101 puan**, sıra
  **#1**. Üç uzak tablo da güncel sonucu geri döndürdü.
- QA: EditMode **297/297**, PlayMode **10/10**, validator **0/0**. Görsel:
  `Assets/QA/PremiumVisuals/2026-08-24_Leaderboard_CatName_Auto_Screen.png`.

## Devam — global podyum görünümü

- Leaderboard bütün uygun Cat Home oyuncularını kapsayan global tablodur; Google
  arkadaşlığına göre filtrelenmez.
- İlk üç için altın/gümüş/bronz premium podyum, global oyuncu sayısı, 4–50 kaydırmalı
  liste ve sabit kişisel sıra kartı eklendi.
- Canlı production verisindeki `lokiş` skoru altın #1 kartında doğrulandı.
- QA: EditMode **298/298**, PlayMode **10/10**, validator **0/0**, Console temiz;
  `Assets/QA/PremiumVisuals/2026-08-24_Leaderboard_GlobalPodium_Screen.png`.

## Devam — Home 2.0 oda düzenleme temeli

- Hamburger menüsündeki `EDIT ROOM`, açık odanın sahip olunan ve yerleştirilebilir
  ürünlerini tek premium düzenleme yüzeyinde yönetir.
- Oyuncu ürüne dokunabilir veya başlık oklarıyla seçebilir. Geçerli sürükleme bırakma
  anında otomatik kaydolur; görünür ana eylemler `TURN`, `STORE`, `DONE` üçlüsüdür.
- `STORE` sahipliği/collection ilerlemesini silmez ve son yerleşimi korur. Saklanan
  ürün editörden geri seçilip tekrar odaya uygulanabilir. Save alt sürümü 6'dır;
  ana kayıt şeması v11 olarak kalır ve eski kayıtlarda tüm sahip olunan ürünler
  görünür kabul edilir.
- Living Room ilk görsel geçişi: kanepe minder/pearl şerit, halı altın dikiş/pati,
  sehpa runner/mint süs. Ana oda ürünlerinin kilitli transform ve collider'ları
  değişmeden detay katmanlarıyla uygulanmıştır.
- Zemin ürünleri mint, duvar ürünleri lilac; TV ünitesi/kitaplık gibi tek hedefli
  ürünler altın hedefle açıklanır. Açık kamera kenarı duvar değildir. TV yalnız
  görünür TV ünitesine, kitaplar yalnız görünür kitaplığa yerleşir; destek saklanırsa
  bağlı ürün de saklanır.
- QA: EditMode **305/305**, PlayMode **10/10**, validator **0/0**, Console temiz;
  `Assets/QA/PremiumVisuals/2026-08-25_Home2_CompactEditor_WallZone_Stable.png` ve
  `2026-08-25_Home2_CompactEditor_TvUnitTarget.png`.
- Kanonik üç sahne, aktif Living Room, Camera/AudioListener/EventSystem **1/1/1**,
  `playModeStartScene = null` ve `DisableSceneReload` geri yüklendi. Git commit/push yok.

## Devam — Home 2.0 eşya set kimliği

- Living Room koleksiyonu `READING` (6) ve `MEDIA` (4) tasarım ailelerine ayrıldı.
- Mağaza kartı set + yerleşim kuralını aynı premium kapsülde gösterir; READING lilac,
  MEDIA aqua vurgu kullanır. Mevcut fiyatlar, koleksiyon sayısı ve save v11 değişmedi.
- QA: `Assets/QA/PremiumVisuals/2026-08-25_Home2_DesignSets_Store.png` ve
  `2026-08-25_Home2_DesignSets_Media.png`; EditMode **305/305**, PlayMode **10/10**,
  validator **0/0**. Git commit/push yok.

## Devam — proje-geneli UI ayrışması ve 3. adım ekonomi görünürlüğü

- Bütün varsayılan görünür UI butonları otomatik çakışma taramasına alındı; ayrı
  işlevsel yüzeyler arasında minimum 16 px görünür boşluk kilitlendi.
- EDIT ROOM/HomeDock, mağaza `AT HOME`/fiyat/eylem ve Player Care iki alt eylemi
  ayrıldı. EDIT ROOM bloklayan bir popup veya panel açıkken açılmıyor.
- ROOM footer yeni panel eklemeden Living Room `READING`/`MEDIA` ilerlemesini ve
  kalan Coin bütçesini gösteriyor; bütün tutarlar katalogdan türetiliyor.
- QA: `2026-08-25_ProjectWide_NoOverlap_EditRoom_Final.png`,
  `2026-08-25_ProjectWide_NoOverlap_PlayerCare.png`,
  `2026-08-25_Home3_EconomyProgress_Final.png`; EditMode **310/310**, PlayMode
  **10/10**, validator **0/0**, Console temiz. Git commit/push yok.

## Devam — EDIT ROOM eylem kartı taşma düzeltmesi

- `TURN / STORE / DONE` butonlarının 64 px yüksekliği ve `y=-70` konumu,
  126 px'lik `Toolbar` kartının alt çerçevesini 8 px aşıyordu.
- Butonlar `y=-52`, `h=56` düzenine alındı; üçü de kart içinde her kenarda
  en az 12 px payla kalıyor ve `HomeDock` ile ayrışıyor.
- Yeni `HomeEditActions_StayFullyInsideToolbarCard` testi bu iç taşmayı kilitliyor.
  QA: `Assets/QA/PremiumVisuals/2026-08-25_HomeEdit_ContainedActions_Final.png`;
  EditMode **311/311**, PlayMode **10/10**, validator **0/0**. Kanonik üç sahne,
  aktif Living Room ve 1 etkin kamera geri yüklendi. Git commit/push yok.

## Devam — Unity servis Console temizliği

- Kullanılmayan `com.unity.ai.assistant` kaldırıldı; abonelik/puan sorgularından
  gelen `PointsBalanceResult` ve `SettingsResult` Console hataları kesildi.
- Gerekli Authentication, Cloud Save, Cloud Code, Leaderboards, Deployment ve
  Services Tooling paketleri korundu. Paket çözümlemesinden sonra `TokenExchange`
  boş yanıtı tekrar oluşmadı.
- EditMode **311/311**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1** geri yüklendi.
  Git commit/push yok.

## Devam — 3. adım ekonomi hedefi ve ücretsiz QA

- ROOM footer ilk sahip olunmayan ürünü sıradaki hedef olarak gösterir. Gerçek
  ekonomi açıldığında `READY TO BUY / NEED N COINS`, mevcut ücretsiz denemede
  `NEXT <ITEM> • FREE TEST` kullanılır.
- Mağaza Home Level kilitleri artık eski quest chapter yerine
  `HomeProgressionService.HomeLevel` üzerinden okunur.
- Kullanıcı kararı: `EconomyChecksEnabled = false` içerik ve yerleşim testleri
  bitene kadar korunur; gerçek Coin harcaması son ekonomi/yayın kapısında açılır.
- QA: `Assets/QA/PremiumVisuals/2026-08-28_Home3_FreeTestNextGoal.png`;
  EditMode **312/312**, PlayMode **10/10**. Git commit/push yok.

## Devam — Home 2.0 Bathroom CARE / SPA

- Bathroom koleksiyonu `CARE` (6) ve `SPA` (4) tasarım ailelerine ayrıldı;
  CARE lilac, SPA aqua vurgu kullanıyor.
- Mağaza kartlarında set ile gerçek yerleşim ailesi birlikte gösteriliyor:
  ilk dört serbest ürün `FLOOR`, Towel Storage/Mirror ve dört SPA fixture ürünü
  `WALL`. Runtime etiketi editörün kanonik `StoreCatalogAssets.PlacementKind`
  verisiyle testte birebir doğrulanıyor.
- Bathroom ROOM footer'ı `CARE X/6 • SPA Y/4 • NEXT <ITEM> • FREE TEST` ritmine
  geçti. Ücretsiz `GET` akışı, fiyatlar, sahiplik ve koleksiyon ekonomisi değişmedi.
- QA: `Assets/QA/PremiumVisuals/2026-08-28_BathroomHome2_CareSpaStore.png`;
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1** geri yüklendi.
  Git commit/push yok.

## Devam — Home 2.0 Kitchen CAFE / CHEF

- Kitchen koleksiyonu `CAFE` (4) ve `CHEF` (6) tasarım ailelerine ayrıldı;
  CAFE sıcak peach/orange, CHEF aqua vurgu kullanıyor.
- İlk dört kahvaltı/kedi bakım ürünü `CAFE • FLOOR`. CHEF tarafında Dish Cart
  ve Kitchen Island `FLOOR`; Pantry Shelf, Sink Cabinet, Refrigerator ve Stove
  & Oven `WALL` olarak gösteriliyor.
- Runtime yerleşim etiketi editörün kanonik `StoreCatalogAssets.PlacementKind`
  verisiyle testte birebir doğrulanıyor. Kitchen ROOM footer'ı
  `CAFE X/4 • CHEF Y/6 • NEXT <ITEM> • FREE TEST` ritmini kullanıyor.
- Ücretsiz `GET` akışı, fiyatlar, sahiplik ve koleksiyon ekonomisi değişmedi.
- QA: `Assets/QA/PremiumVisuals/2026-08-29_KitchenHome2_CafeChefStore.png`;
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1** geri yüklendi.
  Git commit/push yok.

## Devam — Home 2.0 Bedroom COZY / ROYAL

- Bedroom koleksiyonu `COZY` (6) ve `ROYAL` (4) tasarım ailelerine ayrıldı;
  COZY lilac, ROYAL pembe vurgu kullanıyor.
- Dream Wall Art, Pastel Nightstand, Rainbow Wardrobe ve Window Daybed `WALL`;
  diğer altı Bedroom ürünü `FLOOR` ailesinde gösteriliyor.
- Runtime yerleşim etiketi editörün kanonik `StoreCatalogAssets.PlacementKind`
  verisiyle testte birebir doğrulanıyor. Bedroom ROOM footer'ı
  `COZY X/6 • ROYAL Y/4 • NEXT <ITEM> • FREE TEST` ritmini kullanıyor.
- Ücretsiz `GET` akışı, fiyatlar, sahiplik ve koleksiyon ekonomisi değişmedi.
- QA: `Assets/QA/PremiumVisuals/2026-08-29_BedroomHome2_CozyRoyalStore.png`;
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1** geri yüklendi.
  Git commit/push yok.

## Devam — Home 2.0 Garden NATURE / PATIO

- Garden koleksiyonu `NATURE` (5) ve `PATIO` (5) tasarım ailelerine ayrıldı;
  NATURE aqua/mint, PATIO sıcak peach/orange vurgu kullanıyor.
- Garden açık avlu olduğu için on ürünün tamamı `FLOOR` olarak korunuyor. Runtime
  etiketi editörün kanonik `StoreCatalogAssets.PlacementKind` verisiyle testte
  birebir doğrulanıyor; yapay bir duvar sınıfı eklenmedi.
- Garden ROOM footer'ı
  `NATURE X/5 • PATIO Y/5 • NEXT <ITEM> • FREE TEST` ritmini kullanıyor.
- Ücretsiz `GET` akışı, fiyatlar, sahiplik ve koleksiyon ekonomisi değişmedi.
- QA: `Assets/QA/PremiumVisuals/2026-08-29_GardenHome2_NaturePatioStore.png`;
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1** geri yüklendi.
  Git commit/push yok.

## Devam — Home 2.0 Balcony SUNNY / LOUNGE

- Balcony koleksiyonu `SUNNY` (5) ve `LOUNGE` (5) tasarım ailelerine ayrıldı;
  SUNNY sıcak peach/orange, LOUNGE lilac vurgu kullanıyor.
- Herb Shelf, Railing Flowers, Lantern String ve Sun Awning `WALL`; diğer altı
  Balcony ürünü `FLOOR` ailesinde gösteriliyor.
- Runtime yerleşim etiketi editörün kanonik `StoreCatalogAssets.PlacementKind`
  verisiyle testte birebir doğrulanıyor. Balcony ROOM footer'ı
  `SUNNY X/5 • LOUNGE Y/5 • NEXT <ITEM> • FREE TEST` ritmini kullanıyor.
- Ücretsiz `GET` akışı, fiyatlar, sahiplik ve koleksiyon ekonomisi değişmedi.
- QA: `Assets/QA/PremiumVisuals/2026-08-29_BalconyHome2_SunnyLoungeStore.png`;
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1** geri yüklendi.
  Git commit/push yok.

## Devam — Home 2.0 Patio OASIS / GATHER

- Patio koleksiyonu `OASIS` (5) ve `GATHER` (5) tasarım ailelerine ayrıldı;
  OASIS aqua/mint, GATHER sıcak peach/orange vurgu kullanıyor.
- Herb Trough, Patio String Lights ve Pergola Arch `WALL`; diğer yedi Patio
  ürünü `FLOOR` ailesinde gösteriliyor.
- Runtime yerleşim etiketi editörün kanonik `StoreCatalogAssets.PlacementKind`
  verisiyle testte birebir doğrulanıyor. Patio ROOM footer'ı
  `OASIS X/5 • GATHER Y/5 • NEXT <ITEM> • FREE TEST` ritmini kullanıyor.
- Ücretsiz `GET` akışı, fiyatlar, sahiplik ve koleksiyon ekonomisi değişmedi.
- QA: `Assets/QA/PremiumVisuals/2026-08-30_PatioHome2_OasisGatherStore.png`;
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1** geri yüklendi.
  Git commit/push yok.

## Devam — Home 2.0 Second Floor NOOK / STUDIO

- Second Floor koleksiyonu `NOOK` (5) ve `STUDIO` (5) tasarım ailelerine ayrıldı;
  NOOK lilac, STUDIO aqua/mint vurgu kullanıyor.
- Wall Gallery ve Tall Bookcase `WALL`; diğer sekiz Loft ürünü `FLOOR`
  ailesinde gösteriliyor.
- Runtime yerleşim etiketi editörün kanonik `StoreCatalogAssets.PlacementKind`
  verisiyle testte birebir doğrulanıyor. Second Floor ROOM footer'ı
  `NOOK X/5 • STUDIO Y/5 • NEXT <ITEM> • FREE TEST` ritmini kullanıyor.
- Ücretsiz `GET` akışı, fiyatlar, sahiplik ve koleksiyon ekonomisi değişmedi.
- QA: `Assets/QA/PremiumVisuals/2026-08-30_SecondFloorHome2_NookStudioStore.png`;
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1** geri yüklendi.
  Git commit/push yok.

## Devam — Bathroom görsel kalite ve başlangıç yerleşimi

- Tam Bathroom koleksiyonu oyun kamerasında birlikte incelendi. Laundry Hamper
  ön-sol kamera sınırında kesiliyor; Towel Storage ile Wall Mirror'ın sol duvar
  yönü ön yüzleri duvara çeviriyordu.
- Hamper başlangıcı `z=-1.05`; iki sol duvar ürünü `270°` oldu. Havlu rafının
  koyu arka bloğu pearl/lilac/coral açık premium katmanlarla yenilendi.
- Test, yeni başlangıç konumu/yönleri ile katalog yerleşim türlerini kilitliyor.
  Fiyat, sahiplik, collection, save v11 ve ücretsiz QA ekonomisi değişmedi.
- QA: `Assets/QA/PremiumVisuals/2026-08-30_BathroomVisualPass_Before.png` ve
  `Assets/QA/PremiumVisuals/2026-08-30_BathroomVisualPass_Final.png`;
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1** geri yüklendi.
  Git commit/push yok.

## Devam — Kitchen görsel kalite ve başlangıç yerleşimi

- Tam Kitchen koleksiyonu oyun kamerasında birlikte incelendi. Fruit Basket
  ön-sol kamera sınırında kesiliyor; Pantry Shelf sol duvardan odaya dik çıkıp
  lavabo önünü daraltıyordu.
- Fruit Basket `(-2.9, 0, -0.55)` konumuna taşındı. Pantry Shelf
  `(-3.45, 0, 0.95)` ve `270°` ile sol duvara paralel, odaya dönük yerleşti.
- Test yeni başlangıç konumu/yönlerini ve katalog yerleşim türlerini kilitliyor.
  Fiyat, sahiplik, collection, save v11 ve ücretsiz QA ekonomisi değişmedi.
- QA: `Assets/QA/PremiumVisuals/2026-08-30_KitchenVisualPass_Before.png` ve
  `Assets/QA/PremiumVisuals/2026-08-30_KitchenVisualPass_Final.png`;
  EditMode **312/312**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1** geri yüklendi.
  Git commit/push yok.
