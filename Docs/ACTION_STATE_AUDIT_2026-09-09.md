# Cat Home — eylem ve geçiş hataları, 9 Eylül 2026

Kullanıcının bildirdiği su içerken berjerde kestirme hatası ve aynı yaşam döngüsünü kullanan oda/mini oyun akışları incelendi. Görsel tasarım, eşya yerleşimi ve oyun dengesi bu geçişin konusu değildir.

## Düzeltilen davranışlar

- **Su/mama → mobilya çakışması:** Bakım düğmesinin gizlenmesi artık kedinin boşta olduğu anlamına gelmez. `CatActionState` ve `CatActivity.TryStart`, gerçek bakım/uyku/fiziksel meşguliyetini denetler. Hem yakın eylem görünümü hem doğrudan çağrı ikinci eylemi reddeder. Bakım iptal edildikten sonra gerçek berjer dinlenmesi ve uzanma pozu çalışır.
- **Arka planda devam eden ihtiyaç artışı:** İhtiyaç işlemleri kalıcı HUD üzerinde çalıştığından yalnız kâse coroutine'ini durdurmak yetmiyordu. `HungerSystem` / `ThirstSystem` artık işlem sahibini ve ona ait tamamlanma callback'ini tutar. İptal, yüklenen ihtiyaç değeri ve bileşen kapanışı eski artışı bitirir; başka sahibin yeni işlemini veya ödülünü etkilemez. Başarılı bakımın tek tamamlanma olayı korunur.
- **Eski/gizli düğme çağrıları:** Kapatılan bakım bileşeni tekrar başlayamaz. Uyanma istisnası yalnız gerçek uyku düğmesine aittir. Modal arkasındaki eski Kalk/Uyan çağrıları engellenir. Kedi komutları aynı meşguliyet denetimini kullanır.
- **Yarım bırakılan eşya rutinleri:** Ortak iptal yaklaşım coroutine'ini de durdurur. 25 türetilmiş rutin, yalnız gerçekten başlamış kendi hareketini temizler. Kontrolcü, kedi ölçeği ve hareketli eşya parçaları iade edilir; eski bir eşyanın kapanması yeni eylemin kontrolünü bozamaz. Kuş yemliğinin eksik pivot iadesi ve oyuncak tepki kuyruğunun iptali de düzeltildi.
- **Topun geçici çarpışma sahnesi:** Başlama ön kontrolü reddedilirse ayrılmış fizik sahnesi bırakılır. Unity'nin asenkron sahne kapanışı sırasında sonraki kapsam aynı adı kullanıp hata vermesin diye her kapsamın adı benzersizdir.
- **Odalar arası geçiş:** Hedef doğrulandıktan sonra eski bakım/dinlenme, kayıt ve yüklemeden önce iptal edilir. Gelen kedi yükleme bitene kadar giriş almaz. Geçiş yalnız kendi giriş kilidini kaldırır; başka panelin kilidi korunur. Geçersiz veya aynı odaya istek mevcut dinlenmeyi kesmez.
- **Ev ↔ Runner/Catch:** İki oyun ortak açılış rezervasyonu kullanır. İnaktif başlatıcı, eksik sahne, devam eden oda geçişi veya zaten açık oyun ikinci yüklemeyi başlatamaz. Mini oyun açılırken/açıkken oda değiştirme çağrıları reddedilir. Ev kedisi gizliyken klavye/joystick, sevme ve dünya eylemleri giriş almaz; dönüşte bu engel kalkar.
- **Duraklatma ve eski oyun düğmeleri:** Catch'in havadaki atılışı duraklatmada sıfırlanmaz; aynı atılış dönüşte bir kez çözülür. Uygulama arka plana alınması Catch'i ve Runner'ın başlangıç sayacını duraklatır. Çıkış sonrası bekleyen tekrar/başlat çağrıları fazladan can harcamaz veya aktif turu sıfırlamaz.
- **Gecikmiş Runner reklam ödülü:** Doğrulanmış cevap, reklam açılırken gösterilen turun kimliği ve tutarına bağlıdır. Sonraki turun ödülünü yanlışlıkla ikiye katlamaz; tekrarlanan cevap ikinci ödeme üretmez.
- **Havada tur bitişi:** Catch'in süresi atlayışta bittiğinde sonlanmış atılış zemine yerleşir; geç kalan yakalama temizlenir. Runner'ın üçüncü çarpışması havada olursa kedi mevcut şeridindeki gerçek yol/platform yüksekliğine yerleşir. Her iki düzeltme yatay konumu korur. Duraklatma, devam edecek atlayışın yüksekliğini ve evresini korumaya devam eder.

## Doğrulama

**36 benzersiz native PlayMode testinin son sonuçları başarılı.** [Birleşik sonuç](QA/ACTION_STATE_AUDIT_2026-09-09/native-test-summary.json) her testin hangi XML'den alındığını gösterir. Son iki havada bitiş düzeltmesinden sonra mini oyun grupları **14/14** tekrar geçti: [son mini oyun sonucu](QA/ACTION_STATE_AUDIT_2026-09-09/Native-final-minigames-07.xml). Ham, tarihli ara sonuçlar da korunur; başarısız ara sonuçlar başarılıymış gibi sayılmaz.

Son kaynak üzerinde tam **EditMode 489/489**: [XML](QA/ACTION_STATE_AUDIT_2026-09-09/EditMode-action-state-grounding-final.xml). İçerik denetimi **0 hata / 0 uyarı**: [validator](QA/ACTION_STATE_AUDIT_2026-09-09/validator-final.json). Kaynak ve test değişiklikleri için `git diff --check` temizdir.

Son konsoldaki kırmızı kayıt, `EconomyServiceTests` içindeki kasıtlı bozuk görünüm/callback istisnasıdır; ilgili test başarılıdır. Test sonuç kaydetme kayıtlarıyla birlikte [temizlemeden önce saklandı](QA/ACTION_STATE_AUDIT_2026-09-09/console-final-before-clear.json); normal editöre dönüşte test konsolu temizlendi.

Doğrulanmış kapsam: gerçek su/mama/uyku ve berjer; 17 CAT eşyasının aktifken depoya kaldırılması; 70 farklı oda ürününün normal tamamlanması; on ırkta kedi komutları; iki mini oyunun mevcut oynanış testleri. Sekiz odada **78 benzersiz ROOM ürünü / 81 rutin satırı** için iptal sonrası hareket, ihtiyaç/ödül, kontrolcü, ölçek, hareketli parçalar ve başka eylemin kilidi kontrol edildi. Sekiz oda × iki oyun için 16, bakım sırasında dört ek giriş/dönüş geçti. Bu 20 geçişte aynı oda/kedi, tek kamera ve ses dinleyici, sabit can sayısı ve gizli kediye gerçek yön girdisi altında sabit konum kontrol edildi.

### Test teşhisleri

- İlk oda testi bağımsız modal sahibini eski odada yaratıyordu; oda kapanınca bu nesne doğal olarak siliniyordu. Sahip kalıcı GameScene'e taşındı; üretim kodunun başka kilidi kaldırmadığı doğrulandı.
- Kitap yanındaki 6,78 cm hareket eski coroutine değildi: hareket bileşeni kapalı altı saniyede sıfır değişim; ilk gerçek CharacterController adımında tek fiziksel yerleşme. Coroutine yaşamı ve normal fizik ayrı ölçülüyor.
- Mini oyun dönüşündeki 5 cm fark yalnız Y eksenindeki zemin temas payıydı. Bütün 20 dönüşün vektörleri kaydedildi; yatay konum, gerçek zemin teması ve oyun açıkken üç eksende sabitlik doğrulandı.
- Bahçe tırmalamasının eski sahne girişinde kilit, mesafe, enerji ve sahiplik geçerliydi; gerçek pati temasına erişilebilir yol bulunamadığı için başlangıç doğru olarak reddediliyordu. Aynı ürünün oyuncunun ulaşabildiği yakın kenarı seçilerek doğrulandı; erişim toleransı gevşetilmedi.
- İptal matrisi bağımsız rastgele boşta/kuş bakışlarını ayırır: kök dönüşü, merkezden kayık kontrolcünün dünya merkezini oynatıp fiziksel yerleşmeye yol açabiliyordu. Altı saniye boyunca iptal edilmiş coroutine hareketi için .001 m, normal fizik yerleşmesinden sonraki kararlılık için .025 m sınırları korunur; zeminin açık olması ayrıca denetlenir.
- Bahçedeki iki satın almaya bağlı chase istasyonu tarihsel tasarım sözleşmesinde açıkça tanımlıdır (`AGENTS.md`, 19 Ağustos bölümü). Normal kullanım raporundaki 71 rutin / 70 ürün sayımı bu iki ayrı istasyondan gelir; bu turda içerik silinmedi.

## Kayıt ve teslim

Bu oturumun gerçek kayıt başlangıç SHA-256 değeri `943FD6126DA61B098E34FAD1543349150919B1F74B61A17A763C019FC9E1B5D2`; recovery aynı. CP2 yedeği `03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D`. [Son karşılaştırmada](QA/ACTION_STATE_AUDIT_2026-09-09/save-final.json) üç dosyanın hash, boyut ve yazım zamanları başlangıçla aynıdır. Önceki günün hash'leri geri yüklenmedi. Testler `Library/UiQaSession` altındaki ayrı kopyada ve kapalı bulut eşitlemesiyle yapıldı.

**16 tercih ve varlık bayrakları aynen geri yüklendi; QA ve Play kapalı.** Unity açık; GameScene / CatHome_UI / LivingRoom_Level01 üçü temiz, bir etkin kamera ve bir ses dinleyici var. Gerçek kaydı yalnız okuyan geçici editör ön izlemesi tekrar açık; sahneye pişirilmedi. [Editör son durumu](QA/ACTION_STATE_AUDIT_2026-09-09/editor-final-state.json), [tercih doğrulaması](QA/ACTION_STATE_AUDIT_2026-09-09/preferences-restored.txt).

Yeni APK/Android paket derlemesi, commit/push, Cloud Code yayını ve bilgisayar kapatma yapılmadı. Fiziksel cihaz denemesi kullanıcı tarafından akşam yapılacak; buradaki sonuçlar Unity editöründeki kontrollerdir.
