# UI/UX ikinci kalite geçişi — 6 Eylül 2026

Bu tarihsel geçişin ardından [referansa uyum uygulaması](UIUX_REFERENCE_MATCH_2026-09-06.md) geldi. Güncel yüzey, font ağırlığı, simgeler ve HD karşılaştırma orada kayıtlıdır.

Kullanıcının gerçek oyun görüntülerinde bildirdiği geri dönüş ekranı kilidi giderildi. Önceki uygulamanın referansla arasındaki tipografi, yüzey ve yerleşim farkları ortak sistemde iyileştirildi.

[Yeni oyun görüntüleri](QA/UIUX_2026-09-06_Refinement/index.html) · [Önceki uygulama](QA/UIUX_2026-09-06/index.html) · [Tasarım referansı](UIUX_References_2026-09-06/index.html)

## Tıklama kusuru

`WhileYouWereAwayPopupBuilder.ValidateGraphics`, sadece `ButtonFace` adlı yüzeye tıklama izni veriyordu. Yeni ortak düğmenin yüzeyi `Visual` adını taşıdığı için görünür ve interactable olmasına rağmen raycast kapalıydı; tıklama WarmOverlay'e ulaşıyordu. Kural artık nesne adına değil gerçek `Button.targetGraphic` ilişkisine dayanır. Prefab ve sahne yeniden üretildi. Açık geri dönüş/kutlama katmanları alt dock'u da gizler.

Unity'de gerçek fare tıklamasından sonra popup kapandı ve `CatMovement.IsInputBlocked=false` oldu. `ReturnPopupInputTests` yerel prefabı açar, EventSystem raycast sonucunu denetler ve pointer olayını gerçek alıcıya göndererek kapanışı doğrular. Önceki geometrik çakışma ölçümü bu kusuru yakalayamazdı; galeri ölçümüne tıklama alıcısı kontrolü de eklendi.

## Görsel değişiklikler

- Başlıklarda gerçek Fredoka Medium; açıklama, durum ve küçük eylemlerde Nunito Sans SemiBold. Zaten kalın olan yazıya eklenen yapay Bold ve aşırı karakter aralığı kaldırıldı. İki yazı için 2048 SDF atlasları, Türkçe karakterler ve ortak runtime/builder kuralları eklendi.
- İnce ışıklı kenar ve yumuşak temas derinliği aynı panel grafiğinde çizilir; fazladan etkileşimli katman veya Shadow bileşeni oluşturulmaz. Hareket yüzey çocuklarında kalır.
- Açılışın okuma yüzeyi canlı sahneye yumuşak geçer. Pati madalyalı ana eylem, daha dengeli başlık boşlukları ve wordmark düzeni kullanılır.
- Odalarda ayrı başlık ve düzenli yuva kartı; gerçek 16:9 fotoğraflar ve kaydırmalı sekiz oda korunur.
- Oda fotoğrafındaki mevcut/kilitli etiketler de dil değişikliğini takip eder; İngilizce ekranda Türkçe rozet kalmaz.
- Oyun kartlarında daha rahat açıklama alanı, aynı hizada can bilgisi ve tek satırlı geniş eylemler kullanılır.
- Geri dönüşte portre madalyonu, daha okunur ihtiyaç değerleri ve belirgin devam düğmesi kullanılır.

Ortak font/yüzey geçişi tüm UI prefablarına, paylaşılan arayüze ve iki mini oyun sahnesine uygulandı. Başlangıç, oda ve ürün modelleri mevcut oyun varlıklarıdır. Sabit eşya yerleşimi, kayıt, para ve doğrulanmış sağlayıcı davranışı değişmedi.

## Doğrulama kayıtları

`QA/UIUX_2026-09-06_Refinement/` altındaki XML, gerçek tıklama kaydı ve ekran ölçümleri bu geçişe aittir. QA ayrı kayıt kopyası kullanır. Gerçek kullanıcı kaydı, ödeme/reklam ve hesap işlemleri görsel üretmek için çalıştırılmaz. Git commit/push yapılmadı.

- Tam EditMode: **405/405**. İlk koşudaki iki hata eski font beklentisiydi; test, gerçek seviye güncellemesi denetimini koruyarak yeni gövde fontunu ve sentetik Bold bulunmadığını doğrular.
- Native PlayMode: geri dönüş penceresinde gerçek pointer alıcısı/kapanış **1/1**; ev ve oyun geçişleri **5/5**. Önceki çalışmanın tam oyun testleri bu geçişte yeniden koşulmuş gibi sayılmaz.
- **49 HD kare:** 29 ev/pencere durumu, Runner/Catch giriş-oyun-duraklatma için 6 kare, tablet ve geniş ekran için 8 kare, İngilizce için 6 kare. Çözünürlükler 1920×1080, 1440×1080 ve 2400×1080; görseller yeniden büyütülmedi.
- Karelerin `GetWorldCorners` ölçümlerinde **0 OVERLAP / 0 OUTSIDE**. Pointer taramasında yalnız açık hamburger menüsünün Scrim'i arkasındaki 6 ev düğmesi kapalı: `06_Menu_Final.layout.txt`. Bunlar modal arkasında beklenen engellemedir; ham `UNCLICKABLE` kayıtları silinmedi. Diğer ekranlarda beklenmedik alıcı engeli yok; geri dönüş düğmesi ayrıca gerçek fareyle doğrulandı.
- Son normal Play: 3 sahne, tek kamera/ses dinleyicisi/EventSystem, kapalı başlangıç katmanı, serbest kedi kontrolü; Console'da hata/uyarı yok. Ardından QA oturumu kapatıldı ve `GameScene` + `CatHome_UI` + aktif `LivingRoom_Level01` ev önizlemesi geri yüklendi. Validator **0 hata / 0 uyarı**, `playModeStartScene=null`, `DisableSceneReload` ve 1920×1080 korundu. Kayıtlar: `FinalLiveState.txt` ve `FinalWorkspace.txt`.

Bu geçiş masaüstü Unity'de doğrulandı. Gerçek Android cihaz performansı, çentik/klavye ve platform sağlayıcı kontrolleri mevcut yayın kapısında kalır. Referanslar sanat yönünü belirler; galerideki kareler uygulanmış oyunun gerçek sonucudur.
