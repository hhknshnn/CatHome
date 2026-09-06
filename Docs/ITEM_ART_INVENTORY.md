# Eşya Görsel Kalite Envanteri — Dalga 3 Hazırlığı
Tarih: 2026-08-30. Kaynak: StoreCatalogAssets.cs + HomeStoreService oda koleksiyonları + prefab YAML taraması.
Bu belge yalnız sınıflandırma ve öncelik listesidir. Hiçbir art/prefab değiştirilmedi.

## Özet
80 oda ürünü (8 oda x 10), üç art kaynağına ayrılıyor:

| Kaynak | Adet | Durum |
|---|---|---|
| premium-fbx (Blender, `*_Premium.fbx`) | 80 | Dalga 3 kapandı (4 Eylül 2026) |
| pack-prefab (LowPolyLivingRoomPack) | 0 | Living Room 10/10 premium FBX'e taşındı |
| procedural (Unity primitive parça) | 0 | Sekiz odanın prosedürel ürünleri yeniden yazıldı |

**4 Eylül 2026 durumu: dalga 3 tamamlandı, 80/80.** Sekiz odanın tamamı premium
tasarım dilinde yeniden yazıldı ve her ürün gerçek bir kedi etkileşimiyle
gönderiliyor. Aşağıdaki öncelik listeleri tarihsel kayıt olarak duruyor.

Malzeme ailesi zaten tutarlı: tüm ürünler 15 paylaşılan `CH_*` materyalini kullanıyor
(CH_Gold, CH_Cream, CH_MintBright, CH_CoralBright, CH_AquaBright, CH_LilacBright, CH_LemonBright, ...).
Yani boşluk malzemede değil, **geometride/silüette**.

Bütün 80 ürünün mağaza ikonu mevcut, eksik ikon yok.

## Oda başına art kaynağı dağılımı
| Oda | premium | pack | procedural |
|---|---|---|---|
| Living Room | 10 | 0 | 0 |
| Bathroom | 10 | 0 | 0 |
| Kitchen | 10 | 0 | 0 |
| Bedroom | 10 | 0 | 0 |
| Garden | 10 | 0 | 0 |
| Balcony | 10 | 0 | 0 |
| Patio | 10 | 0 | 0 |
| Second Floor | 10 | 0 | 0 |


## KARAR — 2 Eylül 2026: kapsam 80 ürünün tamamı

Bu belge başlangıçta yalnız prosedürel ürünleri hedefliyordu. Karar değişti:
**80 oda ürününün hepsi** `Docs/PREMIUM_FURNITURE_LANGUAGE.md` dilinde yeniden
üretilecek. Üç ek karar:

1. **Sıra oda oda.** Bir odanın tüm ürünleri bitmeden sonrakine geçilmez —
   yarım kalmış oda, hiç dokunulmamış odadan kötü görünür.
2. **Living Room pack'ten çıkıyor.** Yedi pack prefab'ı premium FBX'e taşınacak.
   Katalog girdileri `RoomAt(...)` yerine `Generated(...)` olacak; footprint,
   placement, yaw, fiyat, ownership ve save kimlikleri değişmeyecek.
3. **26 eski premium FBX de yenilenecek.** Premium sayılıyorlar ama reçeteden
   önce yapıldılar; aynı odada iki premium kuşağı yan yana durmayacak.

Living Room'un **on ürünü de** LowPolyLivingRoomPack'e dayanıyor, katalog
girdisi ne olursa olsun:

- Yedisi `RoomAt(...)`/`Room(...)` ile doğrudan pack prefab'ı örnekliyor
  (TvUnit, ModernTelevision, FloorLamp, TallBookshelf, TallHouseplant,
  ModernPainting, ClassicArmchair).
- Üçü katalogda `Generated(...)` görünüyor ama `StoreProductContentBuilder`
  içindeki özel kurucuları yine pack'ten besleniyor:
  `BuildGameConsoleSetPrefab` (`Console_Modern`, `Gamepad_Classic`),
  `BuildSpeakerSystemPrefab` (`Stereo_MainUnit`, `Stereo_Speaker`),
  `BuildBookshelfBookSetPrefab` (pack kitapları).

Yani odada "kolay başlangıç" ürünü yok: her biri hem yeni model hem builder
değişikliği istiyor. Pack bağımlılığı ancak onunun da bitmesiyle kalkar.

### Oda oda kalan iş

| Oda | Yeni dilde biten | Kalan |
|---|---|---|
| Living Room | **10** | 0 |
| Bathroom | **10** | 0 |
| Kitchen | **10** | 0 |
| Bedroom | **10** | 0 |
| Garden | **10** | 0 |
| Balcony | **10** | 0 |
| Patio | **10** | 0 |
| Second Floor | **10** | 0 |
| **Toplam** | **80** | **0** |

Aşağıdaki Öncelik A/B/C listeleri artık sıra belirlemiyor; oda içindeki iş
sırasını belirlemek için referans olarak duruyorlar (büyük silüetten küçüğe).

## Öncelik A — büyük silüet: **liste kapandı** (0 kaldı, 9 bitti)

**Bitti:**
- BalconySunAwning — premium FBX, 30 Ağustos 2026. Kapı üstüne asıldı (`hungHeight 2.24`).
- PatioPergolaArch — premium FBX, 2 Eylül 2026. 4 sütun + kafes + kavisli ön kemer + tırmanan asma.
- BedroomStarCanopy — premium FBX, 2 Eylül 2026. Yıldız çadırı; katalog adı korundu, form cibinlikten tipiye çevrildi.
- PatioPorchSwing — premium FBX, 2 Eylül 2026. Gerçek halkalı zincir, çıtalı oturak, tuftlu minderler; oturak yönü düzeltildi.
- KitchenSinkCabinet — premium FBX, 2 Eylül 2026. Çukur evye, gooseneck musluk, shaker kapaklar, çekmece, çay bezi.
- Kitchen **10/10 bitti** (3 Eylül 2026): dokuz model yeniden yazıldı
  (SinkCabinet zaten yeni dildeydi) ve on ürünün onunda da kedi etkileşimi var —
  IslandPerch, StoolPerch, FridgeStare, FruitSwat, PantryClimb, OvenWarmth,
  CartNudge, MealTime, KitchenSip, KitchenMatKnead. Oda tek yön kuralına
  indirildi: ön yüz -Z, dördü `facesBackward` listesinden çıkarıldı.
- Bathroom **10/10 bitti** (3 Eylül 2026): odanın tamamı yeni dilde ve her
  ürünün gerçek bir kedi etkileşimi var — ShowerRinse, SinkSip, PaperSpin,
  TowelNest, LitterDig, GroomBrush, HamperDive, MirrorGaze, MatKnead,
  TubEdgeWalk.
- BathroomTowelStorage — premium FBX, 3 Eylül 2026. Üç katmanlı dolap: panelli alt kapaklar, açık niş (rulo bayı + katlı havlu yatağı), kapaklı üst bölme, yan altın ray. `TowelNestActivity` ile kedi nişe sıçrayıp uyuyor.
- Bedroom **10/10 bitti** (4 Eylül 2026): dokuz model yeniden yazıldı
  (StarCanopy zaten yeni dildeydi) ve on ürünün onunda da kedi etkileşimi var —
  BedNap, WardrobeScratch, DaybedWatch, KnockOff, VanityStoolNap, YarnSwat,
  NightLightGaze, ArtGaze, BedroomMatKnead, CanopyNap. Tek yeni sınıf
  `KnockOffActivity`; gerisi mevcut sınıfların yeniden kullanımı. Oda tek yön
  kuralına **inmedi**: zemin ürünleri `facesBackward` dışında (oda kök -Z),
  dört duvar ürünü içinde (oda kök +Z). Ayrıntı `AGENTS.md`'de.
- Garden **10/10 bitti** (4 Eylül 2026): on modelin onu da yeniden yazıldı ve
  on ürünün onunda da kedi etkileşimi var — PergolaClimb, TreeScratch,
  BistroPerch, HammockSway, SunBask, GrillWatch, BirdBathSip, PotDig,
  DaisyRoll, YarnBallChase. **Yeni sınıf yok**, onu da mevcut sınıfların
  yeniden kullanımı. Oda tek yön kuralına inmedi: on ürünün hepsi zemin ürünü
  ama üçü sıfırdan farklı yaw taşıyor ve aynı fikirde değiller — şezlong
  (yaw 90, x +2.55) çevrilmiyor, hamak (yaw 90, x -2.75) ve mangal (yaw 270,
  x +2.65) `facesBackward`'a giriyor. Yaw değil konum karar veriyor.
- Balcony **10/10 bitti** (4 Eylül 2026): on modelin onu da yeniden yazıldı ve
  on ürünün onunda da kedi etkileşimi var — AwningGaze, HerbShelfClimb,
  FeederShake, EggChairNap, BenchNap, TableKnockOff, SunMatBask, PlanterDig,
  RailingSwat, LanternGaze. Odanın imza rutini yeni sınıf
  `BirdFeederShakeActivity`: projede kedinin bir ürünü **alttan** çalıştırdığı
  tek beat. `CushionBench` ve `HangingChair` `facesBackward`'dan çıkarıldı,
  `RailingFlowers` ve `LanternString` girdi. Balcony `FitFixtureModel`'den
  geçmediği için on modelin altısı ilk derlemede sözleşmeyi aştı ve elle
  budandı; `BalconyProducts_AreAuthoredInsideTheirCatalogBox` bunu kilitliyor.

Ekranda en çok yer kaplayan, blok görünümü en çabuk fark edilen ürünler.
`vol` = footprint x yükseklik, `prim` = primitive parça sayısı.

| Oda | Ürün | vol | h | prim | Not |
|---|---|---|---|---|---|


## Öncelik B — orta prop, silüet zayıf (0 kaldı, 25 bitti)
Biten: BathroomLitterBox, BathroomLaundryHamper, BathroomGroomingCart,
BathroomWallMirror, KitchenCounterStool, KitchenFeedingStation,
KitchenFruitBasket (hepsi 3 Eylül 2026, premium FBX + etkileşim),
BedroomNightstand, BedroomNightLight, BedroomDreamArt, BedroomYarnBasket,
BedroomVanityStool (4 Eylül 2026, premium FBX + etkileşim),
GardenBirdBath, GardenGrill, GardenFlowerPots, GardenDaisyBed,
PatioPottedFerns, PatioHerbTrough, LoftFloorCushions, LoftBookStack,
LoftWallGallery (4 Eylül 2026, premium FBX + etkileşim).

Kalan yok.

Ortak sorun: 3-7 kutu/silindir parça, pah yok, ayak/kulp/doku detayı yok.

## Öncelik C — düz zemin ve ip ürünleri (0 kaldı, 9 bitti)
Biten: BathroomBathMat, KitchenPawMat (3 Eylül 2026), BedroomPawRug ve
GardenYarnBall, PatioStoneRug, PatioStringLights ve LoftFloorRunner
(4 Eylül 2026) — 0.08-0.28 yükseklikte silüet yok, o yüzden
her şey plandan geliyor: bantlı bordür, saçak, kakma pati ve kedinin yoğurduğu
tek kabartma ped.

Kalan yok.
Bunlar zaten düz olmayı hak eden formlar; yeni model değil, kenar/kalınlık/malzeme rötuşu yeter.

> **Not (5 Eylül 2026):** `PatioStringLights` artık bu gruba ait değil. Patio'nun
> asılacak duvarı olmadığı için ürün kendi iki direğini taşıyacak şekilde yeniden
> yazıldı: sözleşme 1.60 × 0.30 × 0.30 `WallEdge`/hung 1.95 yerine
> **1.40 × 2.10 × 0.30 `Floor`**, yerleşim `(1.95, 0, 2.45)`. Rutin değişmedi
> (`FestoonGaze`). Ayrıntı `Docs/PREMIUM_FURNITURE_LANGUAGE.md` §6.

## Ayrı başlık — Living Room aile tutarsızlığı: kapandı
Living Room'un 10 ürünü LowPolyLivingRoomPack'ten çıkarıldı ve premium FBX ailesine
taşındı (dalga 3, oda 10/10). Pack malzemesi / silüet tutarsızlığı artık yok.

## Değişmeyecekler
Footprint, placement kind, rotasyon, fiyat, ownership ve save kimlikleri her üründe korunacak.
