# Cat Home — Checkpoint 16 Ağustos 2026 (Catch + tam iş listesi)

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

**Proje:** `C:\Users\HAKAN\Desktop\CatHome\CatHome`  
**Unity:** `6000.4.4f1`  
**Kayıt şeması:** v9  
**Commit / push:** yok (istenmedi)  
**Bu belge:** kod değişikliği yok. Yalnız inceleme + kayıt.

**Kanıt videosu:** `C:\Users\HAKAN\Videos\Captures\CatHome - LivingRoom_Level01 - Android - Unity 6.4 (6000.4.4f1) _DX12_ 2026-08-16 13-58-37.mp4`  
Süre ~7.6 sn, 1920×1088, Play 1920×1080 Landscape.

---

## A. Video’de görülen hatalar (düzeltilmedi)

### A1. Mice ve skor / coin aşırı şişiyor

HUD (sarı hap **skor**, pembe hap **süre**, camgöbeği hap **MICE**):

| An (~sn) | Skor (sarı) | Süre | MICE |
| --- | ---: | ---: | ---: |
| 0 | 44408 | 38 | 91 |
| 2 | 52400 | 36 | 100 |
| 4 | 62040 | 34 | 110 |
| 6 | 80264 | 32 | 127 |
| 7 | 84888 | 31 | 131 |

7 sn’de **+40 fare**, skor **+40480**. Kedi karelerde neredeyse yerinde; fareler önünde yığılı. Bu bir “iyi oyuncu” tempo’su değil, otomatik emme.

**Kök neden 1 — yakalama pounce’a bağlı değil, her kare mesafe kontrolü.**

`CatCatchGameController.TryCatchMice()` hunt boyunca her `Update` çalışır. Fare XZ mesafesi `PounceReach` içindeyse anında `Hide()`, `catches++`, skor eklenir, `LaunchOneMouse()`.

- Duruşta reach **1.2**
- Pounce sırasında **1.2 × 1.35 = 1.62**
- Arena fare alanı kabaca **4.8 × 3.3**; kedi merkezde geniş bir daireyi kaplar
- Yakalamak için tıklama / pounce / çarpışma animasyonu gerekmez

**Kök neden 2 — yakalanan fare aynı karede yeniden doğar.**

Catch sonrası `LaunchOneMouse()` idle fareyi `RandomArenaPoint(1.35)` ile geri koyar. `KeepMiceOnTheFloor()` en az **4** aktif fare tutar. Spawn minimumu 1.35, pounce reach 1.62 → uçuşta yeni fare anında tekrar yenir. Duruşta bile 1.35’ten 2.1 m/s wander ile 1.2 daireye birkaç karede girer.

Video hızı (~5.7 fare/sn) bu döngüyle uyumlu.

**Kök neden 3 — skor ve coin formülü sayacı katlar.**

```text
her yakalama:  score += 120 + catches * 8
hunt sonu:     coins  = catches * CoinRewardPerCatch   (8)
```

91→131 arası 40 yakalama skor artışını (40480) birebir üretir. Sarı hap **jeton bakiyesi değil, anlık skor**. Kullanıcı “coin” görüyorsa bu sayı. Gerçek jeton av **tamamlanırsa** `catches * 8` olarak verilir: videodaki 131 fare bile **1048 jeton**; 31 sn kala aynı hızda yüzlerce fare / binlerce jeton olur.

**Kök neden 4 — erken çıkışta coin kesildi, süre bitince şişik ödül duruyor.**

`FinishHunt(false)` (pause EXIT / HOME) coin vermez. Süre 0 olunca `FinishHunt(true)` o ana kadarki `catches` kadar jeton basar. Video’deki emme bitiş ödülünü de şişirir.

### A2. Zıplama ve yakalama animasyonu kötü

Videoda kedi idle duruşta kalıyor; fareler yok oluyor; pounce/ısırış okunmuyor.

**Kök neden 1 — seyahat ile klip senkron değil.**

- Dünya hareketi: `LateUpdate` içinde 0.28–0.5 sn lerp + parabol (`pounceHeight` 0.55–1.15)
- İskelet: `ActivityPounce` **0.72 sn**, yerinde çömelme / öne uzanma (`Root` Y −0.065 sonra +0.14)
- Klip seyahat etmez; kök hareket ayrı. Süreler uyumsuz → kayma + gerilme + “yerinde hamle”

**Kök neden 2 — yakalama animasyonu yok.**

Fare `Hide()` / `SetActive(false)`. `ActivityPawSwat` veya ısırış yok. HUD sayısı artar, sahnede patlama/yakalama yok.

**Kök neden 3 — anlık rotasyon.**

`transform.rotation = Quaternion.LookRotation(...)` pounce başında snap. Ease yok.

**Kök neden 4 — fare görseli.**

Procedural küreler; `Sin` ile scale nabzı. Yakalama pozu yok; yığılınca “mor top” yığını.

**Kök neden 5 — tıklama hedefi hâlâ kırılgan.**

`CatchPounceInput` (tam ekran Image + `IPointerDown`) + zemin raycast eklendi. Video kedi yerinde, sayaç uçuyor: ya tıklama zıplatmıyor ya zıplama okunmuyor; yakalama yine de proximity ile işliyor. Animasyon “kötü” algısı: idle kedi + görünmez emme.

### A3. Videoda görülen diğer notlar (bu turda istenen şikayet değil)

- Pause **II** sağ üstte var.
- Konsol: `WhileYouWereAwayPopup` / `LevelLoader` input block alıp bırakmış; videonun av anında `blockers=0`. Catch, `CatMovement` kullanmaz.
- Hierarchy’de ev UI hâlâ yüklü; Catch `ParkHomePresentation` canvas’ları kapatır. Av HUD Catch canvas.

### A4. Düzeltme yönü (uygulanmadı)

1. Yakalama yalnız pounce inişinde, iniş anındaki fareye, **1 fare / pounce**, cooldown.
2. Respawn reach dışında ve kısa spawn koruması.
3. Pounce klibi seyahat süresiyle hizalansın veya seyahatsiz klip kalksın; inişte paw-swat.
4. HUD: skor / jeton ayrımı; tavan veya doğrusal skor.
5. `FinishHunt(true)` şişik `catches` ile jeton basmasın (1 düzeltilince 5 de iner).

---

## B. Bu oturumda yapılan işler (kod; video hatalarını çözmedi)

Sıra: görünmez fareler → tutorial → tıklama yok → yerinde hamle / zemine gömülme → pause/exit.

| Tur | İstek | Ne yapıldı | Sonuç |
| --- | --- | --- | --- |
| 1 | Fare görünmüyor | Spawn `ArenaRoot.TransformPoint`; `CatCatchMouse` serialize; 4 fare zeminde | Fareler arenada |
| 2 | Oynama tutorial | Runner tarzı 3 adım + SKIP; `catchLives.tutorialCompleted`; süre tutorial’da durur | Kart eklendi |
| 3 | Tıklanmıyor | Eski `Input.GetMouseButtonDown` (proje Input System only) → `Mouse`/`Touchscreen`; kamera bind | Tıklama EventSystem’e kaydı |
| 4 | Yerinde hamle, zemine girme | Arc lerp `LateUpdate`; floor Y; CC collider kapat | Kullanıcı: hâlâ hedefe atlamıyor |
| 5 | Hâlâ atlamıyor; pause/menü; erken çıkışta coin yok | `CatchPounceInput` tam ekran; **II** / RESUME / EXIT; `FinishHunt(false)` jeton yok | Pause var. Yakalama hâlâ proximity vacuum (A1). Zıplama hâlâ A2 |

**Catch dosyaları (son durum):**

- `Assets/Scripts/Catch/CatCatchGameController.cs`
- `Assets/Scripts/Catch/CatCatchPlayer.cs`
- `Assets/Scripts/Catch/CatCatchMouse.cs`
- `Assets/Scripts/Catch/CatchPounceInput.cs`
- `Assets/Scripts/Catch/CatchLivesService.cs`
- `Assets/Scripts/Catch/CatCatchLauncher.cs`
- `Assets/Editor/CatCatchContentBuilder.cs`
- `Assets/Scenes/Catch/CatCatch.unity`
- `Assets/Tests/EditMode/CatchLivesServiceTests.cs`

**Catch kuralları (şimdi):**

- 60 sn hunt; can `START HUNT`’ta
- Tutorial ilk avda; SKIP veya 3 adım
- Pause: timeScale 0; EXIT coin yok, sonuç ekranı yok
- Süre bitince sonuç + `catches * 8` jeton (vacuum yüzünden şişer)
- Dock GAMES → hub; Catch canları Runner’dan ayrı (max 5)

---

## C. Sabah teslimatı (10 madde, aynı gün)

Önceki belge: `Docs/CatHome_Checkpoint_2026-08-16.md`

### 1. Living room pastel

Oturma odası cream/aqua/mint/peach/lilac. Orijinal duvar renderer gizlendi; `PastelWallLiners`.  
`PremiumWorldVisualBuilder.cs`

### 2. Cat tunnel

Ayak izi `0.58 × 0.90`, yükseklik `0.44`. Trigger + `TunnelPlayActivity` / `ActivityTunnelCrawl`.

### 3. Banyo aynası

Procedural pastel; `HungHeight = 1.58`; duvar overlap yok sayılır.

### 4. Cat Runner UI

Welcome iki sütun (hero, BEST, LIVES, START RUN, EXIT). HUD kapsül. Result kartı.

### 5. Bedroom

`bedroom-01`, `Bedroom_Level01.unity`, 10 ROOM ürünü (200–2500 jeton), kilit mutfak complete + `home.bedroom-preview`. Kamera/kedi/shell Living Room ile aynı.

### 6–8. Catch iskeleti, GAMES, ayrı 5 can

Catch sahnesi, Games hub, `CatchLivesService` vs `RunnerEnergyService`, save v9 `catchLives` + `catchBestScore`.

### 9. Görsel dil

Pastel; Catch/Games `LowPolyPanelGraphic` + `PremiumButtonFx`.

### 10. Ekran görüntüleri

`Assets/Screenshots/Checkpoint_2026-08-16/` (01–10).

---

## D. Doğrulama (son Catch rebuild sonrası)

O sırada (vacuum / animasyon düzeltilmeden):

- Derleme temiz
- Catch lives EditMode **4/4**
- Daha geniş EditMode **173/173**, PlayMode **7/7** (tutorial testiyle)
- `LevelContentValidator` 0 error (Catch rebuild’lerden birinde)
- Stack: `GameScene` + `CatHome_UI` + `LivingRoom_Level01`, 1 kamera

Video, bu doğrulamaların A1/A2’yi yakalamadığını gösteriyor: EditMode Catch ekonomisini / pounce animasyonunu ölçmüyor.

---

## E. Bilinçli olarak yapılmayanlar

- A1 vacuum / skor-coin tavanı **düzeltilmedi** (bu mesaj: başka değişiklik yok)
- A2 pounce/catch animasyonu **yeniden yapılmadı**
- Git commit / push yok
- IAP hâlâ yalnız doğrulanmış satın alma ile elmas verir
