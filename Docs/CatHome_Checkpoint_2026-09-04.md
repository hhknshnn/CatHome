# Cat Home — Checkpoint 4 Eylül 2026

**Proje:** `C:\Users\HAKAN\Desktop\CatHome\CatHome`
**Unity:** `6000.4.4f1`
**Kayıt şeması:** v11 (değişmedi)
**Commit / push:** yok — kullanıcı manuel yapar

---

## Başlık: dalga 3 tamamlandı — premium eşya sanatı **80/80**

2 Eylül 2026'da alınan karar (sekiz odanın 80 ürününün tamamı
`Docs/PREMIUM_FURNITURE_LANGUAGE.md` diline taşınacak ve **her ürün gerçek bir
kedi etkileşimiyle** gönderilecek) bu turda kapandı.

| Oda | Ürün | Durum |
|---|---|---|
| Living Room | 10 | Bitti |
| Bathroom | 10 | Bitti |
| Kitchen | 10 | Bitti |
| Bedroom | 10 | Bitti |
| Garden | 10 | Bitti |
| Balcony | 10 | Bitti |
| **Patio** | 10 | **Bu turda kapandı** |
| **Second Floor** | 10 | **Bu turda kapandı** |
| **Toplam** | **80/80** | |

Ürün sayısı, fiyatlar, placement'lar, collection ekonomisi ve save kimlikleri
değişmedi. Kayıt şeması v11 sabit.

---

## Patio — 10/10

`PatioPergolaArch` ve `PatioPorchSwing` dalga 3'te zaten bitmişti. Bu turda
**sekiz model** yeniden yazıldı ve **on ürünün onuna** rutin verildi. Patio v1
tek rutinle gönderilmişti: salıncak sürüşü.

### Yazılan modeller

| Ürün | Sözleşme (x, y, z) | Rutin | Sınıf |
|---|---|---|---|
| PatioParasol | 2.20 × 1.85 × 2.20 | `ParasolScratch` | `ScratchPostActivity` |
| PatioDiningSet | 1.70 × 0.58 × 1.50 | `DiningPerch` | `PerchNapActivity` |
| PatioFirePit | 0.90 × 0.80 × 0.90 | `FirePitBask` | `OvenWarmthActivity` |
| PatioWaterFountain | 0.80 × 0.90 × 0.80 | `FountainSip` | `SinkSipActivity` |
| PatioPottedFerns | 0.80 × 1.10 × 0.60 | `FernWatch` | `SitLookActivity` |
| PatioStoneRug | 2.00 × 0.08 × 1.20 | `StoneRugKnead` | `MatKneadActivity` |
| PatioHerbTrough | 1.60 × 0.50 × 0.35 | `HerbTroughDig` | `LitterDigActivity` |
| PatioStringLights | 1.60 × 0.30 × 0.30 | `FestoonGaze` | `SitLookActivity` |
| PatioPergolaArch | (mevcut) | `ArchClimb` | `PantryClimbActivity` |
| PatioPorchSwing | (mevcut) | `SwingRide` | `SwingRideActivity` |

### Kod

- `CatActivityKind` **58–66** eklendi (append-only).
- `QuestType` **31 `PatioClimb`**, **32 `PatioWatch`** eklendi.
- `StoreProductContentBuilder`: `AttachPatioActivity` + dokuz attach metodu;
  FBX map'e dört yeni giriş; `facesBackward`'a **yalnız** `PatioHerbTrough` ve
  `PatioStringLights` (`PatioPergolaArch` zaten içindeydi).
- `LevelContentValidator.ValidatePatioRoom` yazıldı, oda döngüsünden çağrılıyor.

### Yönelim — oda tek kurala sığmıyor, üçüne sığıyor

- `facesBackward` üçlüsü (`PergolaArch`, `HerbTrough`, `StringLights`):
  yaklaşma kök **+Z**.
- `PatioWaterFountain` (x +2.9) ve `PatioPottedFerns` (x +2.95): avlunun sağ
  kenarına dayalı, yaw 0 olmasına rağmen yaklaşma kök **-X**.
- Kalan dördü açık zemin: kök **-Z**.

`Patio_ApproachesEveryProductFromTheCourtyard` üçünü de kilitliyor.

---

## Second Floor — 10/10

**On modelin onu da sıfırdan yazıldı**: altısı dalga-2 tarif öncesi FBX'ti,
dördü prosedürel Unity primitifiydi. Loft, evde hiçbir ürününde rutin olmayan
son odaydı.

### Yazılan modeller

| Ürün | Sözleşme (x, y, z) | Rutin | Sınıf |
|---|---|---|---|
| LoftFloorRunner | 1.90 × 0.08 × 1.15 | `RunnerKnead` | `MatKneadActivity` |
| LoftFloorCushions | 0.90 × 0.50 × 0.90 | `CushionNest` | `TowelNestActivity` |
| LoftBookStack | 0.60 × 0.60 × 0.50 | `BookKnockOff` | `KnockOffActivity` |
| LoftArcLamp | 0.70 × 1.85 × 0.70 | `LampGlowBask` | `OvenWarmthActivity` |
| LoftBeanBag | 0.95 × 0.55 × 0.95 | `BeanBagNap` | `PerchNapActivity` |
| LoftRecordPlayer | 0.90 × 0.70 × 0.60 | `RecordSpin` | `PaperSpinActivity` |
| LoftStudyDesk | 1.40 × 0.80 × 0.90 | `DeskPerch` | `PerchNapActivity` |
| LoftWallGallery | 1.60 × 0.90 × 0.20 (asılı 1.55) | `GalleryGaze` | `SitLookActivity` |
| LoftTallBookcase | 1.40 × 2.10 × 0.40 | `BookcaseClimb` | `PantryClimbActivity` |
| LoftChaiseLounge | 1.60 × 0.70 × 0.70 | `ChaiseNap` | `CanopyNapActivity` |

Ayrıca iki **hareketli parça FBX'i** (ürün orijinini paylaşan, ayrı tek nesne):

- `LoftBookStackBook_Premium.fbx` — yığından düşen cilt, `KnockOffActivity`.
- `LoftRecordPlayerDisc_Premium.fbx` — dönen plak, `PaperSpinActivity`.

İkisinin de mesh'i ürün orijininde yazıldı; builder çocukta pivot ofsetini
(`-axis`) iptal ediyor, yoksa parça ofsetin iki katına oturur.

### Kod

- `CatActivityKind` **67–76** eklendi.
- `QuestType` **33 `LoftWatch`** eklendi.
- `StoreProductContentBuilder`: `AttachLoftActivity` + on attach metodu; FBX
  map'e dört yeni giriş (`FloorRunner`, `FloorCushions`, `BookStack`,
  `WallGallery`); `facesBackward`'a `LoftWallGallery` ve `LoftTallBookcase`.
- `LevelContentValidator.ValidateSecondFloorRoom` yazıldı.

### Yönelim

- `facesBackward` ikilisi (`WallGallery`, `TallBookcase`): kök **+Z**.
- `LoftFloorCushions` (x +2.9) ve `LoftBookStack` (x +2.9): kök **-X**.
- `LoftArcLamp` (x **-3.2**, sol kenar): kök **+X**.
- Kalan beşi açık zemin: kök **-Z**.

`SecondFloor_ApproachesEveryProductFromTheRoomAndNotTheWall` dördünü de
kilitliyor.

---

## Ortak notlar

- **Yirmi rutinin hiçbiri yeni aktivite sınıfı gerektirmedi.** Hepsi paylaşılan
  sınıfları kendi `CatActivityKind`'ıyla kullanıyor. Bu yüzden validator ve
  testler aktiviteyi **kind ile** arar — `GetComponent<T>` odada ilk kurulan
  örneği döndürür ve ikinci ürünün rutini görünmez olur.
- **Erişilemeyen ürün dürüst beat alır.** 1.55–1.95 arasında asılı olan
  `PatioStringLights` ve `LoftWallGallery` bakış (`gaze`) alıyor; vuruş,
  kedinin dokunamadığı bir şey hakkında yalan olurdu. Aynı kural balkonun
  tentesi ve fener dizisinde de geçerli.
- **Patio, Balcony ve Loft `FitFixtureModel`'den geçmez.** Patio'da altı model,
  Second Floor'da on iki dosyanın yedisi ilk derlemede sözleşmeyi aştı.
  Hepsi elle budandı, sonra iki testle kilitlendi:
  `PatioProducts_AreAuthoredInsideTheirCatalogBox` ve
  `SecondFloorProducts_AreAuthoredInsideTheirCatalogBox`.

### Yeni yazım tuzakları (hepsi `PREMIUM_FURNITURE_LANGUAGE.md` §6'ya işlendi)

1. `kit.sphere`'in `scale` parametresi **yarıçap**, yarı-genişlik değil —
   fıskiyeyi 0.03 dışarı taşırdı.
2. `kit.paw_badge` XY düzleminde kurulur ve parmakları hep +Y'ye açar: düz
   üründe imzayı dikine kaldırır (halı 0.08 → 0.12), panelde ise ped küresi
   yüzeyin ötesine uzanır (pikap, çalışma masası).
3. Eksende döndürülmüş kare büyür: `0.5 · span · (cos + sin)` — minderler
   0.90 → 0.98.
4. Döndürülmüş minderin biyesi `kit.cylinder` olamaz (çubuk yaw almaz); aynı
   yaw'ı taşıyan ince `kit.cube` welt gerekir.
5. Rafa konan nesne kitap dizisinin **aralığına** gitmeli; dizinin içinde
   kalırsa renkli blok gibi okunur.
6. Üst raf içeriğine ayrı yükseklik tavanı gerekir; alt raf ölçüsüyle kornişi
   deler.
7. Yığının üstündeki çanak **alt katmandan** başlamalı, yoksa havada durur.
8. Zincirli `kit.strut`'ta parça başına yarıçap = "ipe dizilmiş boncuk"; tek
   yarıçap düz okur.
9. Dolu disk yuvarlak hazneyi kapatır — ateş çukurunun kenarı halka olmalı,
   alev konileri kütüklerin altından başlamalı.
10. Düşen su: dört huzme = dört masa ayağı; tam boy perde = turkuaz kova.
    Doğrusu kısa etek + küçülen damlalar + iniş halkası.
11. Yaprak, çubuğa dizilmiş küre değildir; orta damar + çift kısa çubuk.
12. Rutinin hedefi görünmüyorsa rutin yoktur (kazı çukuru tonu + kutu kapağı).
13. Süs, rutinin giriş ağzını kapatamaz (`CanopyNapActivity` / şezlong).
14. Footprint gerçek yayı kaldırmıyorsa eğimi **tabanı ters yöne iterek** al.

---

## Açık kalan işler (model kusuru değil — katalog yerleşimi)

> **5 Eylül 2026: ikisi de kapandı.** Ayrıntı `Docs/ROADMAP.md`, "Aktif kalan
> iş" 2. madde. Özet: festoon kendi direklerini kazandı
> (1.40 × 2.10 × 0.30 `Floor`, `(1.95, 0, 2.45)`) ve saksı salıncağın önüne
> alındı `(2.95, 0, -1.7)`.

- ~~`PatioStringLights` 1.95'te asılı ama Patio'nun arka sınırı 0.6'lık bir
  parapet; dizi boşlukta duruyor gibi okunuyor. 0.30'luk kutuya direk sığmadığı
  için model tarafında çözümü yok.~~ — Kutu bizim kararımızdı: sözleşme
  büyütüldü ve model iki direkle yeniden yazıldı.
- ~~`PatioPottedFerns` (2.95, 0, 1.5) sahnedeki dekor küresi ve salıncak
  iskeletiyle çakışıyor; saksı oda kamerasından görünmüyor.~~ — Sağ kenarda
  kaldı ama salıncağın önüne indi; model değişmedi.

İkisi de `StoreCatalogAssets` yerleşim kararıydı. Kayıtları kırmıyor:
`HomeStorePlacementEntry` yalnız oyuncunun kendi taşıdığı ürünler için yazılır.

---

## Son QA

- EditMode: **381/381**
- PlayMode: **39/39 — kuyruk kapandı.** Garden / Balcony / Patio / Second Floor
  turları yazıldı
  (`Assets/Tests/PlayMode/{Garden,Balcony,Patio,SecondFloor}ActivityTests.cs`).
  Dört elle koşu yapıldı; **hiçbir kırılma ürün ya da rutin hatası değildi**,
  üçü de test altyapısıydı.
  - 1. koşu **4 geçti / 6 kırıldı** — testin `cat.transform.parent Is.Null`
    assert'i. Oda sahneleri kediyi `03 Character/CatRoot` altında kuruyor.
    Assert artık kaydedilen parent'ı karşılaştırıyor; aynı yanlış satır
    `BathroomPropActivityTests`, `BedroomActivityTests`,
    `KitchenActivityTests` ve `TowelNestTests` içinde de düzeltildi.
  - 2. koşu **9 geçti / 1 kırıldı** — yalnız Garden turu, çünkü Garden kendi
    ihtiyaç sistemlerini kuran tek oda sahnesi ve dolu susuzlukta
    `BirdBathSip` haklı olarak reddediyor. Dört dosya her rutin öncesi
    enerji 40 / susuzluk 35 / açlık 35 sağlaması yapmaya başladı.
  - 3. koşu (`Run All`, 39 test) **22 geçti / 17 kırıldı** — dört yeni dosya
    temiz; kırılanlar 2 Eylül'den beri yazılı duran eski dosyalardı. İki kök
    neden: oda sahnesini Single yüklemek `DirectLevelPlayBootstrap`'i
    tetikliyor ve additive `GameScene` birkaç kare sonra kayıt dosyasını
    fixture'ın üstüne uyguluyor (`SwingRideTests`'in sahiplik assert'i),
    ayrıca altı dosya hiç `EnergySystem` kurmuyordu ve `CatActivity.TryStart`
    enerji sistemi yokken her rutini reddediyor. Yeni `RoomPlayModeSupport`
    ile on üç fixture redirect'i kapatıyor, odayı tek başına yüklüyor,
    ihtiyaçları sağlıyor ve hareket kilidinin bırakılmasını sınırlı süre
    bekliyor.
  - 4. koşu (`Run All`, Test Runner'dan elle) **39 geçti / 0 kırıldı** — on
    yedi fixture, 83,6 sn. Dalga 3'ün son açık işi kapandı. MCP üzerinden
    PlayMode koşulmaz.
- `LevelContentValidator`: **0 hata / 0 uyarı**
- Console: temiz (yalnız test koşusunun beklenen `IgnoreFailingMessages`
  kayıtları)
- Oda QA çekimleri: `Assets/QA/PremiumVisuals/Patio_{Wide,BackWall,RightWall,LeftWall}.png`
  ve `Loft_{Wide,BackWall,RightWall,LeftWall}.png`
- Blender: 5.2, tamamı `--background --factory-startup` headless
- Git commit / push: yok

## Güncellenen belgeler

- `Docs/PREMIUM_FURNITURE_LANGUAGE.md` — §6'ya 14 yeni tuzak
- `Docs/ROADMAP.md` — "Patio kapandı" ve "Second Floor kapandı — dalga 3 bitti"
  checkpoint'leri
- `Docs/ITEM_ART_INVENTORY.md` — 80/80; oda tablosu ve kaynak sayıları kapatıldı;
  Öncelik A/B/C listelerinin üçü de kapandı; Living Room pack tutarsızlığı kapandı
- `AGENTS.md` — dalga 3 kapanış bölümü, oda başına yönelim gerçekleri, yeni
  yazım tuzakları ve "yerleşim sorunu bildirilir, sessizce düzeltilmez" kuralı
- `Docs/ROADMAP.md` son güncelleme 4 Eylül; Mevcut durum + aktif kalan iş listesi
  dalga 3 kapanışına çekildi
