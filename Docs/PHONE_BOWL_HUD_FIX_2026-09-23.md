# Telefon: kap yakınlığı ve alt HUD düzeltmesi — 23 Eylül 2026

Kullanıcının videodaki dört sorunu giderme talebi. Başlangıç **19:14:26 UTC / 22:14:26 TR**; tahmin 45–75 dakika. Bu turun süre sayacı bağlam yenilenince sıfırlanmadı.

## Yapılanlar

- **Kap yakınındaki takılma:** `BowlInteraction.Update`, her karede iki kap için ayrıntılı gövde/deri uygunluğu arıyordu. Düğme teklifi artık görünür/dolu kap, aynı oda, yükseklik ve 0,75 m ölçekli yakınlık kontrolü kullanıyor. Normal yürüyüş, joystick hassasiyeti, klipler, modeller ve bakım temas hesabı değiştirilmedi.
- **Mama ye / Su iç:** yakındaki kap artık kedinin tam yemek duruşunu ve dar bakış açısını yakalamasını beklemeden teklif ediliyor. Tıklama hâlâ mevcut hedefi ve taze fiziksel uygunluğu doğruluyor. Uygun duruş yoksa düğme korunuyor ve kaba dönüp yaklaşma mesajı çıkıyor; otomatik yürüme/ışınlanma veya erken ihtiyaç kazanımı yok. İki bakım düğmesi ve aynı düğmedeki uyku görünümü yeni Storybook tasarımını koruyor.
- **Kedi komutları:** ayrı kalıcı Canvas üzerindeki kısayol gerçek `HomeDock` üçüncü yuvasına bağlandı. Ölçü, ölçek, aralık, görünüm ve dokunma alanı eşleşiyor. `Update` aşamasında eski konuma dönüp `LateUpdate` aşamasında düzelme yarışı da kaldırıldı. Editör ön izlemesi aynı düğme görünümünü ve konumu kullanıyor.
- **Joystick:** merkez Y190 → Y220; güvenli alan alt/sol payları ekleniyor. Tam aşağı sürüklenen başlık ve gölgesi opak alt şeridin üstünde kalıyor. Dock ayrıca alt güvenli alan payına yükseliyor.

## Doğrulama

Esas kayıtlar `Docs/QA/PHONE_BOWL_HUD_FIX_2026-09-23` altında, Git dışında.

- `native-phone-final.xml`: **10/10**. İki kaba gerçek joystick yaklaşımı; dört bakış yönünde görünür teklif; uzak/boş/pasif hedef reddi; uygunsuz tıklamada yerinde kalma ve açıklama; aynı karede eklenen engelde başlangıç reddi; dolu ihtiyaca uygun geri bildirim; yakınlık sorgusu performansı; eski yönün yeniden kontrolü; alt HUD geometrisi, gerçek raycast ve sahne yeniden yükleme.
- `native-care-final.xml`: **2/2**. Oriental Shorthair ve Persian ile dört gerçek mama/su düğmesi, üretim süresinde tamamlanma, gerçek ağız teması, ihtiyaç iyileşmesi ve tek ödül; ayrıca TR/EN dolu ihtiyaç reddi. Kök/yön değişimi 0. Deri kabulü önceki 3 mm sınırıyla ölçüldü; toleranslar gevşetilmedi.
- HUD üç çözünürlükte kontrol edildi: **848×392, 1920×1080, 2400×1080**. Her birinde üç kontrol; dört alt düğmede eşit ölçü/aralık, komut düğmesinde beş dokunma noktası, asimetrik/ölçekli ayrı parent denemeleri, modal dönüşü ve yeniden yükleme. Joystick sekiz tam yönde gerçek çizilen mesh üzerinden kontrol edildi; tam giriş 1, bırakınca 0.
- Son sıcak yakınlık sorgusu: mama p95 **0,0357 ms**, maksimum **0,1084 ms**; su p95 **0,0300 ms**, maksimum **0,1027 ms**. Bu değerler editörde `BowlInteraction.Update` ölçümüdür; bütün kare veya telefon FPS ölçümü değildir.
- Mimari/içerik doğrulaması **0 hata / 0 uyarı**. C# derleme hatası yok; Player derlemesindeki mevcut uyarılar aşağıda ayrıldı.

Test ekran görüntülerinin ilk asenkron sürümleri geçiş/başlık durumunu gösterebildi; son telefon ve 2400×1080 görüntülerinde eşzamanlı kare yakalama kullanıldı. Sıfır test seçen ilk filtre turu kabul sayılmadı. İlk UI testindeki eski `CanvasRenderer.GetMesh` imzası düzeltildi. Esas son XML/manifest kayıtlarıdır.

Fiziksel telefon bu turda bağlı değildi. Yeni APK'nın cihazdaki akıcılığı ve dokunma hissi için kullanıcı telefon denemesi ayrıca gereklidir; editör ölçümü telefon başarısı olarak sunulmaz.

## Kayıt ve kapsam koruması

Yeni başlangıç hashleri alındı; PlayMode yalnız ayrı `Library/UiQaSession` kopyasında çalıştı. Üç gerçek kayıt, sahneler, prefablar, kedi FBX/animasyonları ve sesler aynı. 16 tercih geri yüklendi, QA yolu kaldırıldı ve normal üç temiz sahneye dönüldü. Testin iki dinamik font önbelleği, **bu turun başlangıç hashleriyle birebir eşleşen** baytlarla geri getirildi; tarihsel bir oyun kaydı geri yüklenmedi.

Değişiklikler: beş runtime C# (biri yalnız editör ön izlemesi), bir editor C#, iki mevcut test uyarlaması, iki yeni telefon testi ve metaları. Mevcut diğer değişikliklere dokunulmadı. Commit/push/yayın/cihaza kurulum yapılmadı; Unity açık bırakıldı.

## Android teslimi

**Başarılı:** `Builds/Android/CatHome_Test_0.1.0_PhoneFix_20260923.apk`.

- Android IL2CPP Release / ARM64 / LZ4 / StrictMode. 19:46:03–19:49:34 UTC; **211,34 saniye**. **0 hata / 10 uyarı**: değiştirilmeyen sınıflardaki altı eski API ve üç kullanılmayan alan uyarısı, ayrıca mevcut tanılama sembol ayarı uyarısı. Bu göreve ait yeni derleme hatası yok.
- **301.458.043 bayt / 287,49 MiB**. SHA-256 `8E2302EDC27303BD8D932395008C390064E0E1346E120CC0ED9C858709B6508F`.
- APK v2 imzası doğrulandı; önceki test paketindeki Android Debug sertifikası kullanılıyor. Paket `com.vexorialabs.cathome`, sürüm `0.1.0` / kod `1`, `arm64-v8a`. Telefona kurulum veya yayın yapılmadı.
- Son koruma: 6.852 başlangıç dosyasından **6.844 aynı / yalnız beklenen 8 C# farklı**. İki yeni test ve iki meta. Üç kayıt, 87 sahne, 320 prefab, 396 FBX, 60 animasyon, 145 WAV ve 27 ProjectSettings aynı. Derlemeden sonra kayıt/ayar/font için ayrıca 32/32 hash eşleşmesi doğrulandı.
- Son durum: Play/QA/derleme kapalı, üç temiz normal sahne, Unity açık. Esas kayıtlar `native-final-manifest.json`, `build-final.json`, `apk-file.json`, `preservation-final.json`, `preservation-after-build.json`, `editor-final.json` ve `closure.json`.

Unity BuildSummary zamanları `Unspecified` türündedir; yerel saate yeniden çevrilmedi. Derleme başlangıç/bitişinde BuildRunner'ın UTC metadata'sı kullanıldı.
