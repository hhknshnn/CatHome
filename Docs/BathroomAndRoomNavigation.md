# Bathroom, Kitchen and room navigation

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

## Runtime room flow

- `HomeRoomService` is the canonical catalog for visitable home rooms.
- `LevelDefinition` remains quest/chapter metadata and never chooses the active room.
- `LevelLoader.LoadRoom(roomId)` loads the destination additively, validates its marker,
  cat and camera, parks the incoming camera before the next rendered frame, enables one
  destination camera/listener, unloads the old room, rebinds the shared UI, then saves.
- `HomeStoreSaveState.currentRoomId` is stored inside the existing canonical Cat Home JSON.
  Unknown room ids and rooms whose ownership was lost migrate to `living-room-01`.
- `bathroom-01` is unlocked only when
  `HomeStoreService.HomeBathroomPreviewId` is owned. During the current hands-on QA pass,
  `HomeStoreService.EconomyChecksEnabled` is `false`: clicking the locked Bathroom card
  grants that ownership without spending currency and opens the room. Set the switch back
  to `true` before release to restore the normal HOME-store purchase gate.
- `kitchen-01` is the next room in the chain. Its HOME product normally requires
  Bathroom ownership and all ten Bathroom products. In the current free QA mode,
  selecting Kitchen recursively acquires the Bathroom-room prerequisite and opens
  Kitchen immediately without spending currency.
- Runner and other home-return flows can use
  `HomeRoomService.CurrentRoomSceneName`.

## Bathroom asset inventory

Bathroom now authors an empty premium shell. Fixtures and decor are not permanent room
furniture: all ten pieces are hidden `StoreProductDisplay` products until acquired from
the room-aware ROOM tab.

| Fixture | Production model | Authored size |
| --- | --- | --- |
| Tub | `BathroomTub.fbx` | 2.450 × 1.360 × 1.303 m |
| Vanity and sink | `BathroomVanitySink.fbx` | 1.960 × 1.690 × 0.855 m |
| Toilet | `BathroomToilet.fbx` | 0.940 × 1.560 × 1.181 m |
| Shower | `BathroomShower.fbx` | 1.650 × 2.205 × 1.280 m |

The ten-product Bathroom collection, sorted by coin price, is:

1. Paw Bath Mat
2. Laundry Hamper
3. Cat Litter Box
4. Grooming Cart
5. Towel Storage
6. Bubble Wall Mirror
7. Pastel Toilet
8. Vanity & Sink
9. Cat Paw Bathtub
10. Rainbow Shower

The four imported fixtures retain the Blender `-90° X` correction inside their generated
store prefabs. The other products reuse project art or low-poly procedural premium forms.
Every product supports rotation and uses the canonical `100 coins = 1 diamond` price
ratio, although the current QA click path displays `FREE TEST` / `GET` and spends neither.
Invisible care and future-activity anchors keep the cat and Bathroom interaction seams
wired without adding visible starter furniture.

## Kitchen Level 01

Kitchen also starts as an empty premium shell. Its cream/lemon checker floor, aqua
backsplash, mint/coral wall panels, sunrise window, gold trim and paw medallion are fixed
architecture; all usable furniture and appliances are hidden ROOM products until owned.

The ten-product Kitchen collection, sorted by coin price, is:

1. Paw Breakfast Rug
2. Rainbow Fruit Basket
3. Twin Feeding Station
4. Breakfast Stool
5. Candy Pantry Shelf
6. Pastel Dish Cart
7. Paw Sink Cabinet
8. Candy Refrigerator
9. Cupcake Stove & Oven
10. Cat Kitchen Island

All ten pieces use generated low-poly premium geometry, support placement rotation, and
keep the canonical `100 coins = 1 diamond` ratio. The authored default positions produce
the filled QA composition, while every visual remains inactive in the saved scene so a
normal Kitchen entry is empty. Kitchen and Bathroom each author their own camera,
AudioListener, cat, spawn, invisible care anchors and `GameTimeService`.

## Authoring commands

- Generate the room asset: `Tools > Cat Home > Rooms > Rebuild Bathroom Level 01 (Silent)`
- Generate Kitchen: `Tools > Cat Home > Rooms > Rebuild Kitchen Level 01 (Silent)`
- Generate product prefabs and room instances: `Tools > Cat Home > Store > Build Real Product Content`
- Generate product photos: `Tools > Cat Home > Store > Rebuild Catalog Previews`
- Generate the persistent picker: `Tools > Cat Home > Rooms > Build Room Selector`
- Validate everything: `Tools > Cat Home > Architecture > Validate Project Architecture`

After generation, restore the canonical authoring stack (`GameScene`, `CatHome_UI`,
`LivingRoom_Level01`, active Living Room) before final Play-mode QA.
