# Salon: sıkışma, koltuk oranı ve tünel çarpışması

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

**Sonraki kullanıcı kararı:** [TV ve yeni ana yatak](TV_BED_FIXES_2026-09-07.md). Aşağıdaki açık arka koridor çözümü artık geçersizdir; yeni yatak duvara dayalıdır, eski çakışan kayıt konumları önüne düzeltilir.

7 Eylül 2026 kullanıcı bildirimi için uygulanan düzeltmeler.

## Sıkışan bakım köşesi

Kayıt kopyasındaki gerçek konum `(-1.30, .05, 2.47)` idi. Eski yatağın arkası ile duvar arasında yaklaşık 15 cm kalıyordu; karakterin fizik gövdesi ve dönüş payı bu açıklığa sığmıyordu. Önceki sahnede sağ analog testi ilerleyemedi (−.012 m).

Yatak z=2.30'dan z=1.70'e, bakım yaklaşımı z=1.05'e alındı. Arka koridor yaklaşık 75 cm oldu. Aynı kayıt konumundan beş farklı başlangıç açısıyla sağa ve ön-sola analog çıkış doğrulandı. Canlı kayıt kopyasında sağ analog 0.7 saniyede 1.42 m ilerledi. Gerçek oyuncu kaydı değiştirilmedi; yeni sahne eski konumu erişilebilir bırakıyor.

## İkili koltuk

Koltuk ve kendi dekorları `.76` dünya ölçeği kullanıyor. Görünen gövde artık yaklaşık **1.52 × .81 × .85 m**; berjerin yüksekliği yaklaşık .80 m. Koltuk geniş kalırken sırt/kol yüksekliği berjerle orantılı hale geldi. Yerleşim kaynağı ölçeği mutlak uygular, tekrarlanan üretimde çarpmaz.

Sol minderin gerçek üst yüzeyi yeniden ölçüldü; zıplama girişi ve kullanılabilir destek alanı güncellendi. On ırkta gövde-minder yüksekliği, minderin yatay sınırları, zıplama ve açık zemine dönüş kontrol edildi. Sehpa etkileşimi de aynı testte korundu.

## Tünel

Önceki inşa edilmiş prefab yalnız seçim trigger'ı taşıyordu. Elle yürüyüş testinde kedi yan yüzeyin içinden bütünüyle geçti. Yeni prefab kumaş mesh collider'ını ve gerçek gövde sınırına oturan katı kutuyu içeriyor. Kutunun iki ağzı da elle yürüyüşe kapalıdır; kullanıcı açıklamasına göre geçiş yalnız **Oyna** komutuyla başlar.

Aktivite yakın ve erişilebilir ağzı seçer, kedi emekleyerek karşı uçtan çıkar. Rutin kendi CharacterController'ını geçici olarak kapatır; ürün engeli açık kalır. İptalde karakterin fiziği geri açılır, ürünün katılığı kaybolmaz.

## Kanıtlar

`QA/ROOM_COLLISION_2026-09-07/`:

- `before-fixes.xml`: eski sahne/prefabda iki yeni hareket testi de sorunu yeniden üretti.
- `movement-collision.xml`: düzeltme sonrası 2/2; kayıtlı köşeden analog çıkış, tünelin dört yönden elle geçilememesi ve iptal sonrası fizik.
- `interaction-breeds.xml`: 5/5; on ırk tünel kumaş sınırı, on ırk ana yatak, 20 koltuk/sehpa rutini, iki uçtan komutla tünel geçişi ve otomatik yerleşim erişimi.
- `EditMode.xml`: 442/442; 4.147 izinli beşli CAT birleşimi dahil.
- `level-validation.txt`: 0 hata / 0 uyarı.
- `room-after.png`, `corner-escaped.png`: gerçek 1920×1080 Play görüntüleri, oyuncu kaydının izole QA kopyası. Görünür HUD düğmelerinde çakışma veya ekran dışına taşma yok.
- `live-state.txt`, `saved-corner-live-exit.txt`, `final-editor-state.txt`: gerçek konum/ölçü, analog çıkış ve normal editör düzenine dönüş.

Salonun oda seçimi ve HOME mağaza fotoğrafları yeniden üretildi ve gözle incelendi. Tünelde görsel/model değişmediği için ürün fotoğrafı aynı kaldı. Unity üç sahneli düzenleme görünümüne döndü; aktif sahne LivingRoom_Level01, tek etkin kamera, QA kapalı. Commit/push kullanıcıya aittir.
