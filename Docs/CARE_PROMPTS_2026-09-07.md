# Görünmeyen bakım eylemlerinin düzeltilmesi

7 Eylül 2026. [Gerçek oyun ekranları](QA/CARE_PROMPTS_2026-09-07/index.html).

**Devir durumu:** Uygulama ve doğrulama tamamlandı. Kullanıcı yeni sohbete geçmek için belge/checkpoint istedi; ortak başlangıç [7 Eylül checkpoint'idir](CatHome_Checkpoint_2026-09-07.md).

## Sorun ve neden

Banyodaki sol ön köşede görünür mama kabı olmamasına rağmen “Mama ye” çıkıyordu. Salon dışındaki yedi oda üreticisi, eski bakım bileşenlerinin referansları için boş `FoodBowlAnchor`, `WaterBowlAnchor` ve `RestAnchor` nesneleri bırakıyor. `BowlInteraction` yalnız doluluk/ihtiyaç ve yatay mesafeyi, `SleepInteraction` yalnız yatağa giriş mesafesini kontrol ediyordu. Boş noktalar gerçek eşya kabul ediliyordu.

Kayıt kopyasında Bathroom `(-3.25, .05, -1.62)` konumunda sıfır renderer taşıyan mama noktası aday oldu. Aynı bölgede boş uyku girişinin alanı da örtüştüğü için düzeltme öncesi canlı karede “Uyu” görüldü. Kullanıcının “Mama ye” karesi ve ayrıca yeniden üretilen “Uyu” karesi galeride ayrı etiketlidir.

## Değişiklik

- Yeni `CareInteractionTarget`, hedefte etkin mesh bulunmasını, hedefin ve girişin kedinin yüklenmiş odasında ve etkin olmasını ister. Kamera culling sonucu olan `Renderer.isVisible` kullanılmaz; renderer listesi tekrar kullanılarak her kare yeni dizi üretilmez.
- Mama/su mesafesi erişilebilir `InteractionPoint` noktasına ölçülür. Varsayılan .45 m sınırında `CatActivityMotion.ClearSegment` katı engelleri denetler. Gerçek salon kaplarının girişleri gövdelerinden .53 m uzakta olduğundan yalnız eski merkez ölçümü doğru girişte düğmeyi de kaçırıyordu.
- Düğmeye basıldığında aynı hedefin oda, görünürlük, mesafe ve yolu tekrar doğrulanır. Bir önceki karede gösterilmiş eylem uzaktan ışınlama yapmaz veya başka bakım eşyasına dönüşmez.
- Uyku da görünür yatak ve açık giriş ister. Boş/gizli yatağa kayıtlı uyku geri yüklenmez; gerçek yatağın üstündeyken “Uyan” eylemi korunur.
- Boş bağlar sahnelerden silinmedi; oda adına göre özel yasak eklenmedi. Gelecekteki gerçek bakım eşyaları aynı kuralla çalışır. Oda mobilyalarının kendi `CatActivity` etkileşimleri değişmedi.

## Doğrulama

| Kontrol | Sonuç |
| --- | --- |
| Tam EditMode | 459/459 başarılı |
| Yeni native bakım testleri | 3/3 başarılı |
| Mevcut yatak ve kayıtlı uyku testleri | 2/2 başarılı; on ırkın minder teması dahil |
| Sekiz oda / mama, su, uyku | 24/24 beklenen sonuç |
| LevelContentValidator | 0 hata, 0 uyarı |
| Canlı HUD butonlarının sınır/raycast/çakışma taraması | Banyo ve salon temiz |

`CarePromptTests` gerçek sahneleri yükler ve açlık/susuzluk sistemlerini sağlar; testlerin eksik ihtiyaç sistemi nedeniyle yanlışlıkla geçmesi önlenir. Görseli olmayan, pasif, renderer'ları kapalı, başka sahneye taşınmış, uzak veya girişi engellenmiş kaplar denenir. Gerçek kaplarda yeme/içme, yatakta uyku/uyanma ve gizli yatağa uyku yükleme reddi de çalıştırılır.

Son gerçek banyo karesi 1920×1080: `(-3.35, .05, -1.57)`, dünya eylemleri serbest, modal kapalı, bakım düğmesi/aday/uyku isteği false. Aynı normal oda geçişiyle salonda gerçek kap önünde “Mama ye” görünür. Kamera/listener/EventSystem 1/1/1.

QA `UiQaTestSession` kayıt kopyasında yapıldı. Ekran kontrolünde bekleyen koleksiyon kutlaması ödül alınmadan yalnız QA oturumunda susturuldu. Asıl kayıt SHA-256 karşılaştırmasıyla değişmemiştir. QA/native test oturumları kapandı; test için geçici throttling durumu temiz, önceki editör etkileşim tercihi korundu. Düzenleme yığını `GameScene` + `CatHome_UI` + aktif `LivingRoom_Level01`, `playModeStartScene=null`, `DisableSceneReload` olarak geri yüklendi. Git commit/push yapılmadı.

Kanıtlar: [bakım test XML](QA/CARE_PROMPTS_2026-09-07/CarePlayMode.xml), [yatak test XML](QA/CARE_PROMPTS_2026-09-07/BedPlayMode.xml), [EditMode XML](QA/CARE_PROMPTS_2026-09-07/EditMode.xml), [24 nokta tablosu](QA/CARE_PROMPTS_2026-09-07/room-care.csv), [sahne/kayıt denetimi](QA/CARE_PROMPTS_2026-09-07/validation.txt).
