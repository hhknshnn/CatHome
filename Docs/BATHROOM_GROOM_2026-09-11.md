# Banyo — taranma / adım 2, 11 Eylül 2026

Kullanıcı yalnız adım 2'yi, 25 dakika hedefiyle onayladı. Asistan kontrolü tamamlandı; kullanıcı Unity incelemesi bekleniyor. [Gerçek Unity yakın görüntüsü](QA/BATHROOM_GROOM_2026-09-11/groom-close.png). Video üretilmedi. Adım 3 ve diğer banyo işleri yeni onay bekler.

## Düzeltme

Taranma kökü yaklaşık .40084 m/sn ilerlerken önceki Walk sunumu sabit Speed .5 ve LocomotionRate 1 kullanıyordu. `GroomBrushActivity`, fırça geçişinin ve kısa yürüyüşlerinin gerçek hızını artık `CatActivityAnimation.SetWalkSpeed` ile verir. Irkın mevcut ölçülmüş basış kataloğu adım temposunu belirler; yavaş sürtünmede saf Walk kullanılır. Yaklaşma/geri dönüşün kısa adımları .65 m/sn ile sınırlı, yerinde dönüş nötr pozdadır.

Bu API'yi yalnız taranma kullanır. Diğer eylemlerin Walk davranışı, Animator.speed, kaynak klipler, ırk/kemik/ürün ölçekleri ve fırçanın gerçek geçiş çizgisi aynı. Üç geçiş korunur. İptal sırasında tam denetleyici arabanın önüne sığmıyorsa önceden tanımlı açık yaklaşma noktasında yeniden açılır; başka giriş sahibinin kilidi bırakılmaz.

## Doğrulama

- `groom-cadence.xml`: 2/2. On ırk × üç geçişte gerçek Animator çevrimleri ile kök ilerlemesi eşleşti: iki ölçüm de .40084 m/sn; her ırkta 102 kararlı örnek. Hareket doğrultusu/gerçek geçiş çizgisi, bitiş ve tam kapsül açıklığı kontrol edildi. Ayrı testte duraklatma ve başka giriş sahibi varken iptal başarılı.
- `groom-regression.xml`: 2/2. Mevcut taranma tamamlanma testi ve normal yürüyüşün dönüş/duvar/animasyon sahipliği kontrolü başarılı.
- Gerçek yeni yerleşimde oyun düğmesiyle son tur: tek tamamlanma, açık çıkış, kontrol iadesi; yerinde Walk 0 sn. Süre 10.208 sn. `Docs/QA/ROOM_INTERACTIONS_2026-09-11/groom-step2-final/report.json`; önceki `groom-step2-before` karşılaştırmadır.
- Proje doğrulayıcı 0 hata/0 uyarı. Tam test paketi, diğer ürün/oda animasyon taraması veya telefon performansı ölçülmedi. Yakın PNG gerçek hareket sırasında ayrı çekim kamerası ayarıyla üretildi; oyuncu kamerasının tüm ayarları hemen geri getirildi.

## Teslim durumu

Sekiz oda sahnesi ve gerçek ana kayıt/recovery/CP2 başlangıçla birebir aynı (`preservation.json`). Test sonrası tercihler ve geçici font/CurrencyHud/EditorSettings çıktıları geri yüklendi. Son deneme ayrı QA kopyası `Library/UiQaSession/20260911-115802`; Play/QA açık, Banyo/Bakım arabası düğmesi hazır. Kayıt çekimi/derleme kapalı; zaman1, capture0, tek kamera/ses dinleyici. Kullanıcı bitirmeden denemeyi kapatmayın. Son kanıt `editor-ready.json`.

APK/arşiv/commit/push/yayın/kapatma yok. Adım 2'nin kullanıcı onayı olmadan adım 3'e geçilmez.
