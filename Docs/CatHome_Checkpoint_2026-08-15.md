# Cat Home — Ayrıntılı Proje Checkpoint’i

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

**Tarih:** 15 Ağustos 2026, 03:18 (Europe/Istanbul)  
**Proje yolu:** `C:\Users\HAKAN\Desktop\CatHome\CatHome`  
**Unity sürümü:** `6000.4.4f1` (`360f97ecca93`)  
**Checkpoint amacı:** Premium görsel yenileme, Cat Runner geliştirmeleri, Bathroom odası ve çok odalı ev mimarisinin tamamlanmış durumunu güvenli biçimde devretmek.

## 1. Kaydetme ve son editör durumu

- `EditorSceneManager.SaveOpenScenes()` başarıyla tamamlandı.
- `AssetDatabase.SaveAssets()` çalıştırıldı.
- Açık sahnelerin hiçbirinde kaydedilmemiş değişiklik yok (`AnyDirty=False`).
- Canonical çalışma alanı geri yüklendi:
  - `GameScene`
  - `CatHome_UI`
  - `LivingRoom_Level01` — aktif sahne
- Edit Mode sonunda tam olarak:
  - 1 etkin Camera
  - 1 etkin AudioListener
  - 1 etkin EventSystem
- QA sırasında harcanan Cat Runner enerjisi geri yüklendi; son kayıt 5/5 enerjiyle bırakıldı.
- Bu checkpoint sırasında commit oluşturulmadı; belge canlı proje dosyalarının doğrulanmış durumunu tarif eder.

## 2. Tamamlanan ana işler

### 2.1 Premium genel görsel dil

- Arayüzün koyu lacivert ağırlığı azaltıldı; sky, aqua, mint, pembe, berry, lilac, lemon, peach ve cream renk ailesi ana tema oldu.
- Lacivert yalnız metin, iç oyuk ve kontrollü kontrast alanlarında tutuldu.
- Kartlar, kapsüller ve butonlarda oval köşe, çok duraklı renk geçişi, iç çerçeve, üst gloss bandı, parıltı ve kontrollü derinlik kullanılıyor.
- Eylem butonlarında hover, basma, yaylanma ve sheen davranışları ortak premium efekt katmanında toplandı.
- Animasyonlar layout kökünü değil görsel alt kökü hareket ettiriyor; responsive yerleşim korunuyor.
- Fredoka tabanlı metin rolleri ve okunaklı karakter aralıkları korunuyor. Eski ağır dış Shadow/Outline katmanları geri eklenmedi.
- Living Room’a renkli duvar panelleri, cream/gold çıtalar, wainscot, pati motifleri ve düşük maliyetli ortam parıltıları eklendi.
- Bloom ve renk düzenleme ölçülü tutuldu; sahneyi yıkayan aşırı pozlama kullanılmadı.

Önemli altyapı:

- `Assets/Scripts/PremiumUiStyle.cs`
- `Assets/Scripts/LowPolyPanelGraphic.cs`
- `Assets/Scripts/PremiumButtonFx.cs`
- `Assets/Editor/PremiumUiFactory.cs`
- `Assets/Editor/PremiumUiSystemRebuild.cs`
- `Assets/Editor/PremiumWorldVisualBuilder.cs`
- `Assets/Scripts/PremiumWorldAmbientFx.cs`

### 2.2 Premium coin ve diamond varlıkları

- Ortak premium para varlıkları üretildi ve HUD, mağaza ve Runner tarafından tekrar kullanılıyor.
- Coin: kalın altın kenar, parlak iç yüz, kabartma kedi patisi ve ayrı glint görünümü.
- Diamond: koyu mavi siluet, cyan çekirdek, çok yüzlü fasetler ve beyaz yıldız parıltısı.
- Runner coin modeli tek renderer/mesh olacak şekilde optimize edildi.
- Runner coini artık tam tur dönmüyor. Küçük vitrin salınımı yapıyor ve kabartma pati yüzü kameraya dönük kalıyor.
- Prosedürel eski ikonlar yalnız premium asset eksikse fallback olarak duruyor.

Canonical varlıklar:

- `Assets/Art/PremiumCurrency/Icons/PawCoin_Icon.png`
- `Assets/Art/PremiumCurrency/Icons/DiamondGem_Icon.png`
- `Assets/Art/PremiumCurrency/PawCoin.fbx`
- `Assets/Art/PremiumCurrency/DiamondGem.fbx`
- Blender kaynakları: `ArtSource/Blender/Currency`

### 2.3 While You Were Away popup

- Popup SafeArea içinde gerçek merkeze alındı; geniş ve uzun ekranlarda merkez invarianti korunuyor.
- Eski geniş beyaz/boş görünüm yerine candy-gradient ana kart, berry başlık, aqua süre kapsülü ve üç farklı renkli ihtiyaç kartı kullanılıyor.
- İç/dış rim, gloss, sparkle ve dekoratif pati katmanları eklendi.
- Açılış, kartların sırayla gelişi, idle parıltı ve kapanış animasyonu eklendi.
- Reduced-motion modunda aynı final yerleşime animasyonsuz ulaşılır.
- UI oda sahnesinden önce açılırsa `Show()` yeniden `CatMovement` çözer; popup açıkken kedi girdisi güvenle bloke edilir.

Önemli dosyalar:

- `Assets/Scripts/WhileYouWereAwayPopup.cs`
- `Assets/Scripts/WhileYouWereAwayPopupFx.cs`
- `Assets/Editor/WhileYouWereAwayPopupBuilder.cs`
- `Assets/Tests/EditMode/WhileYouWereAwayPopupTests.cs`

### 2.4 Mağaza, ekonomi ve yerleştirme sözleşmeleri

- Alt dock genel kontrol/mağaza alanıdır ve `SHOP` adını korur; oda seçici olarak kullanılmaz.
- CAT, ROOM ve HOME sekmelerinde görünür dikey scrollbar vardır; kartlar fiyat sırasına göre sunulur.
- Living Room Level 1 koleksiyonu tam 10 ürünlük canonical sözleşmeyle doğrulanır.
- Mevcut odadaki halı, iki kişilik kanepe ve sehpa koleksiyonda tekrar satılmaz.
- Kitaplık önce açılır. Renkli kitap seti yalnız kitaplık alındıktan sonra satın alınır ve doğrudan kitaplığa bırakılır.
- Kitap seti `Book_1`–`Book_10` prefablarını kullanır; üç rafı dolduracak şekilde sırayla dizilir ve kitaplıkla birlikte taşınır.
- TV ünitesi televizyonun ön koşuludur. Televizyon yalnız TV ünitesinin üstüne yerleştirilir.
- Oyun konsolu ve hoparlör sistemi koleksiyonda yer alır.
- Yerleştirilebilir ürünlerde açı değiştirme açıktır; bağlı kitap/TV öğelerinin yerel açıları da kaydedilir.
- Tüm satılabilir ürünlerde `100 coin = 1 diamond` oranı kullanılır; iki fiyat da gerçek premium ikonlarla gösterilir.
- Diamond düğmesi her zaman gerçek diamond tutarını yazar. Bakiye yeterliyse ayrı onay, yetersizse paket ekranı açılır.
- Diamond paketleri: 10, 20, 50, 100, 500 ve 1000.
- Diamond paket düğmeleri doğrudan para vermez; gerçek IAP sağlayıcısının doğrulanmış işleminden sonra grant seam’i çağrılmalıdır.

Not: Gerçek Google Play/App Store IAP sağlayıcısı henüz bağlanmış değildir; katalog ve doğrulama sınırı hazırdır.

### 2.5 Cat Runner — premium mekanik ve görsel paket

- Runner sahne açılışında otomatik başlamaz. Premium welcome paneli şunları gösterir:
  - `START RUN`
  - `EXIT TO MAIN MENU`
  - Mevcut enerji
  - Kalıcı best score
- Enerji yalnız oyuncu `START RUN` dediğinde harcanır; welcome ekranından çıkış ücret çıkarmaz.
- Kontroller:
  - Canlı yatay drag ve pürüzsüz lane snap
  - Dominant-axis yukarı swipe ile jump
  - Aşağı swipe ile slide
  - Havada aşağı swipe ile fast-drop
  - Çözünürlük/DPI uyumlu gesture threshold
- Yol artık yalnız düz zeminden oluşmaz:
  - giriş rampası
  - yükseltilmiş deck/platform
  - iniş rampası
  - güvenli platform üstü coin ve hazard yerleşimi
- Oyuncu platform yüksekliği için mutlak yüzey örneği; spawnlar için göreli lift örneği kullanılır. Road profile iki kez sayılmaz.
- Çarpışmalar yalnız gerçek alt/üst dikey aralıklar çakıştığında oluşur. Üstte veya altta kalan engel yanlış hit vermez.
- Ayakta çarpılan overhead engel slide sırasında güvenle geçilir.
- Kediyle ilişkili engel/prop çeşitleri artırıldı: scratching post, bowl, pet bed, toy, food, rack, banner ve petshop kompozisyonları.
- Çevre üç derinlik katmanına ayrıldı: yakın dekor, orta dükkân/prop grubu, uzak pastel skyline/cloud.
- Yol koyu surround, canlı iç yüzey, curb, emissive kesikli lane çizgileri ve tema bazlı road detail katmanları kullanır.
- Dekoratif geçit yüksek ve ince hale getirildi; kameraya yaklaşırken ekranı kapatan dev coral blok davranışı kaldırıldı.
- Kamera lane/jump/slope follow, hız FOV’u, landing ve hit feedback’i kullanır.
- Coin pickup burst, sparkle, hit stars, landing dust ve speed streak FX eklendi.
- URP post-processing, ACES, ölçülü Bloom/Color/Vignette ve SMAA eklendi.
- Runner’dan çıkışta sahne önce kendi kamera/listener/canvas sunumunu kapatır, sonra evi geri açar.
- Runner başlatıldığı odayı snapshot olarak tutar ve çıkışta aynı odaya döner.

Önemli dosyalar:

- `Assets/Editor/CatRunnerContentBuilder.cs`
- `Assets/Scripts/Runner/CatRunnerGameController.cs`
- `Assets/Scripts/Runner/CatRunnerPlayer.cs`
- `Assets/Scripts/Runner/CatRunnerTrackManager.cs`
- `Assets/Scripts/Runner/CatRunnerTrackObject.cs`
- `Assets/Scripts/Runner/CatRunnerCameraRig.cs`
- `Assets/Scripts/Runner/CatRunnerFeedbackController.cs`
- `Assets/Scripts/Runner/CatRunnerScenerySegment.cs`
- `Assets/Scripts/Runner/CatRunnerThemeSegment.cs`
- `Assets/Tests/EditMode/CatRunnerPremiumMechanicsTests.cs`
- `Assets/Tests/PlayMode/GameSceneSmokeTests.cs`

### 2.6 Bathroom Level 01

- Yeni oynanabilir oda sahnesi oluşturuldu:
  - `Assets/Scenes/Levels/Bathroom_Level01.unity`
- Görsel dil:
  - mint/cream dama zemin
  - aqua/mint/purple duvar kuşakları
  - gold trim
  - pink/cyan fixture vurguları
  - renkli raf, havlu, bitki, minder ve küçük bakım objeleri
- Temel fixture’lar:
  - küvet
  - vanity/sink
  - tuvalet
  - duş
- Blender’dan gelen fixture’larda builder’ın `-90° X` yön düzeltmesi uygulanır; prefab ve prosedürel fallback’e bu düzeltme verilmez.
- Oda kedi, bowl, bed/care noktaları, kamera, ışık ve dört kullanılabilir placeholder/anchor içerir.
- Builder çözüm sırası: prefab → FBX → procedural fallback.
- Bathroom HOME mağazasında sahiplik ile açılır:
  - 3000 coin
  - 30 diamond
- Kilitli görünmesi hata değildir; satın alındıktan sonra Room Selector’dan geçiş yapılır.

Varlıklar:

- `Assets/Art/Bathroom/Models/BathroomTub.fbx`
- `Assets/Art/Bathroom/Models/BathroomVanitySink.fbx`
- `Assets/Art/Bathroom/Models/BathroomToilet.fbx`
- `Assets/Art/Bathroom/Models/BathroomShower.fbx`
- `ArtSource/Blender/Bathroom/BathroomFixtureKit_Source.blend`
- `ArtSource/Blender/Bathroom/BathroomFixtureKit_Preview.png`
- `Assets/Editor/BathroomLevelBuilder.cs`
- `Assets/Scripts/HomeRooms/BathroomUsablePlaceholder.cs`

Not: Dört usable anchor mimari olarak hazırdır; özel banyo mini oyunları/etkileşim animasyonları sonraki içerik turunda bu anchorlara bağlanabilir.

### 2.7 Çok odalı ev ve oda geçiş mimarisi

- Oda kataloğu ve tek kaynak runtime durumu `HomeRoomService` içindedir.
- Ana API:
  - `HomeRoomService.CurrentRoomId`
  - `HomeRoomService.CurrentRoomScenePath`
  - `HomeRoomService.CurrentRoomSceneName`
  - `LevelLoader.LoadRoom(string roomId)`
- Kök `CatHomeSaveData` sürümü 7’dir. Aktif oda kimliği, bunun içindeki
  `HomeStoreSaveState` sürüm 5 bölümünde saklanır.
- Bilinmeyen, bozuk veya kilitli kayıt Living Room’a güvenli fallback yapar; sahiplik atlanmaz.
- Oda geçişi additive ve atomiktir:
  - mevcut oda kamerası hedef oda doğrulanana kadar korunur
  - hedef hazır olduğunda kamera/listener handoff yapılır
  - eski oda daha sonra unload edilir
- Hamburger menüsündeki `ROOMS`, premium `RoomSelectorPanel` açar.
- Alt dock’taki `SHOP`, genel mağazayı açmaya devam eder.
- Room Selector SafeArea uyumludur ve Living Room/Bathroom gerçek dünya preview görsellerini kullanır.
- Kart metni ve eylem düğmesi preview görselinin altında kalır; görselin üzerine binmez.
- Runner’a Bathroom’dan girilirse welcome çıkışı ve oyun sonucu Bathroom’a döner.

Önemli dosyalar:

- `Assets/Scripts/HomeRooms/HomeRoomService.cs`
- `Assets/Scripts/HomeRooms/HomeRoomSceneMarker.cs`
- `Assets/Scripts/HomeRooms/RoomSelectorPanel.cs`
- `Assets/Editor/RoomSelectorPanelBuilder.cs`
- `Assets/Scripts/LevelLoader.cs`
- `Assets/Scripts/DirectLevelPlayBootstrap.cs`
- `Assets/Tests/EditMode/HomeRoomNavigationTests.cs`
- `Docs/BathroomAndRoomNavigation.md`

### 2.8 Siyah ekran, kamera ve EventSystem korumaları

- Tek bir bilinen oda sahnesinden Direct Play artık odayı `GameScene` ile Single değiştirmez.
- Oda kamerası korunurken `GameScene` ve `CatHome_UI` additive yüklenir; ilk karede `No cameras rendering` boşluğu oluşmaz.
- Canonical üçlü stack Play’e girerken mevcut sahneleri korur ve `DisableSceneReload` kullanır; domain reload normal kullanıcı Play’inde açık kalır.
- Otomatik PlayMode testleri kendi geçici start scene ve Enter Play Mode ayarlarını yönetir. Workspace hook test sırasında normal Play politikasını uygulamaz.
- Runner additive sahnedir ve `CatHome_UI` içindeki tek ortak EventSystem’i kullanır.
- Runner sahnesinde `RunnerEventSystem` üretilmez; bu karar validator tarafından korunur.

Önemli dosyalar:

- `Assets/Editor/CatHomeAuthoringWorkspace.cs`
- `Assets/Scripts/DirectLevelPlayBootstrap.cs`
- `Assets/Scripts/LevelLoader.cs`
- `Assets/Editor/LevelContentValidator.cs`

## 3. Build Settings sırası

Doğrulanmış enabled sahneler:

1. `0 — GameScene`
2. `1 — CatHome_UI`
3. `2 — LivingRoom_Level01`
4. `3 — CatRunner`
5. `4 — Bathroom_Level01`

`SceneArchitectureBuilder` yeni sahneler eklenirken Runner ve diğer bilinen ek sahneleri silmeden bu listeyi korur.

## 4. Son doğrulama sonuçları

### Otomatik testler

- EditMode: **140/140 geçti**, 0 failed, 0 skipped.
- PlayMode: **4/4 geçti**, 0 failed, 0 skipped.
- Popup hedefli paket: **16/16 geçti**.
- Home room/navigation hedefli paket: **7/7 geçti**.
- Runner premium mekanik hedefli paketler: **33/33 ve 38/38 geçti** (ilgili geliştirme turlarında).

### Mimari ve editör

- `LevelContentValidator.ValidateProject()`:
  - **0 hata**
  - **0 uyarı**
- Unity Console son kontrol:
  - **0 error**
  - **0 warning**
- Son canonical stack:
  - `GameScene`
  - `CatHome_UI`
  - `LivingRoom_Level01` aktif
- Son enabled runtime sayıları:
  - Camera: 1
  - AudioListener: 1
  - EventSystem: 1
- Açık sahnelerin tamamı kaydedilmiş ve temizdir.

## 5. Görsel QA kanıtları

Son önemli ekran görüntüleri:

- Home Edit Mode: `Assets/QA/PremiumVisuals/2026-08-15_Home_EditMode.png`
- Home Play: `Assets/QA/PremiumVisuals/2026-08-15_Home_Play.png`
- Offline popup final: `Assets/QA/PremiumVisuals/2026-08-15_OfflinePopup_Play_v2.png`
- Room Selector final: `Assets/QA/PremiumVisuals/2026-08-15_RoomSelector_Play_v2.png`
- Bathroom Edit Mode final: `Assets/QA/PremiumVisuals/2026-08-15_Bathroom_EditMode_v2.png`
- Bathroom Direct Play final: `Assets/QA/PremiumVisuals/2026-08-15_Bathroom_DirectPlay_v2.png`
- Runner welcome from Bathroom: `Assets/QA/PremiumVisuals/2026-08-15_RunnerWelcome_FromBathroom.png`
- Runner gameplay final: `Assets/QA/PremiumVisuals/2026-08-15_RunnerGameplay_Final_v2.png`

Game View canonical hedefi `1920x1080 Landscape` olarak bırakılmıştır. QA araç önizlemeleri gerektiğinde daha küçük kopya gösterebilir; kaynak Game View oranı 16:9’dur.

## 6. Yeniden üretim sırası

Sahne veya builder tabanlı büyük bir değişiklikten sonra şu sıra kullanılmalı:

1. `BathroomLevelBuilder.BuildSilently()`
2. `CatRunnerContentBuilder.BuildSilently()`
3. `PremiumUiSystemRebuild.Rebuild()`
   - popup
   - room selector
   - currency/HUD/shop
   - premium world presentation
4. `CatHomeAuthoringWorkspace.OpenFullHomePreview(false)`
5. `LevelContentValidator.ValidateProject()`
6. Tüm EditMode testleri
7. Tüm PlayMode testleri
8. Living Room, popup, store, room selector, Bathroom direct Play, Runner welcome/gameplay ve odaya dönüş görsel QA’sı
9. Console temizliği ve son kez canonical stack/tek kamera/tek EventSystem kontrolü

Builder çalıştırıldıktan sonra yalnız açık sahneyi bırakmak teslim için yeterli değildir; canonical üçlü çalışma alanı mutlaka geri yüklenmelidir.

`CatRunnerContentBuilder` kendi `RunnerLaunchUI` hiyerarşisini de premium factory ile işler. Böylece builder bağımsız çalıştırılsa bile `ShopButton`, `PlayCatRunnerButton` ve `RewardedEnergyButton` üzerindeki `PremiumButtonFx` kaybolmaz. Validator bu üç bileşeni zorunlu olarak kontrol eder.

## 7. Kalıcı korunacak sözleşmeler

- Popup SafeArea merkezini kaybetmemeli ve yeniden düz beyaz panele dönmemeli.
- Büyük UI dolgularında koyu lacivert tekrar ana renk olmamalı.
- Premium currency assetleri prosedürel düz şekillerle değiştirilmemeli.
- Bottom dock `SHOP`; hamburger menüsü `ROOMS` davranışını korumalı.
- Kitaplık/kitap ve TV ünitesi/TV ön koşulları kaldırılmamalı.
- `100 coin = 1 diamond` fiyat sözleşmesi ve iki fiyatlı satın alma korunmalı.
- Bathroom sahiplik kontrolü atlanmamalı.
- Direct Play mevcut oda kamerasını koruyan additive bootstrap kullanmalı.
- Runner welcome atlanmamalı; enerji sahne açılışında harcanmamalı.
- Runner platform mutlak/göreli yükseklik API’leri tekrar birleştirilmemeli.
- Runner tam tur coin dönüşü geri getirilmemeli; pati yüzü görünür kalmalı.
- Runner sahnesine ikinci EventSystem eklenmemeli.
- Runner dönüş hedefi hardcode Living Room olmamalı; launch snapshot kullanılmalı.
- Otomatik PlayMode test geçişi workspace hook tarafından ezilmemeli.

Bu kurallar çalışma alanı hafızasına da yazılmıştır:

- `C:\Users\HAKAN\Desktop\CatHome\AGENTS.md`

## 8. Bilinen sonraki geliştirme alanları

Bunlar mevcut teslimi engelleyen hata değildir:

1. Google Play/App Store gerçek IAP provider bağlantısı ve sandbox purchase doğrulaması.
2. Bathroom usable anchorları için özel yıkama, tarama, su oyunu veya bakım mini aktiviteleri.
3. Bathroom’a özel 10 parçalık yerleştirilebilir mağaza koleksiyonunun içerik üretimi.
4. Yeni Kitchen/Bedroom/Balcony/Garden odalarının aynı `HomeRoomService` kataloğuna eklenmesi.
5. Uzun cihaz QA turu: farklı Android çözünürlükleri, notch/SafeArea ve düşük seviye GPU profili.
6. Runner için daha fazla tematik segment, ancak mevcut lane/collision sözleşmesini bozmadan.

## 9. Devralma için ilk kontrol listesi

Projeyi yeniden açan kişi şu sırayla başlamalı:

1. Unity’nin `GameScene + CatHome_UI + LivingRoom_Level01` açtığını ve Living Room’un aktif olduğunu doğrula.
2. Console’da derleme hatası olmadığını doğrula.
3. Normal Play’e bir kez bas; ilk karede siyah ekran/`No cameras rendering` olmamalı.
4. Hamburger → `ROOMS` ile oda seçiciyi aç.
5. Bathroom sahipliği varsa geçişi; yoksa HOME mağazası yönlendirmesini doğrula.
6. Runner welcome ekranını, START/EXIT davranışını ve başlangıç odasına dönüşü doğrula.
7. Büyük builder değişikliğinden önce bu checkpoint ile `AGENTS.md` kurallarını oku.

## 10. Kapanış

Bu checkpoint oluşturulmadan önce sahneler ve assetler kaydedildi. Otomatik testler, mimari validator, Console ve canonical editör durumu temiz doğrulandı. Kullanıcının talebi doğrultusunda checkpoint tamamlandıktan sonra bilgisayar kapatma komutu verilecektir.
