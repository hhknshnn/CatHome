# Telefon performansı ve ilk kurulum — 26 Eylül 2026

Çalışma başlangıcı **19:00:46 UTC**, kesin üst sınır **22:00:46 UTC**. Kapsam, kullanıcının Redmi Note 9 Pro'da bildirdiği kasılma ve eski/hatalı ilk kullanım yönlendirmeleridir. **Bu tur boyunca telefon bağlı değildi; fiziksel cihaz FPS sonucu yoktur.**

## Uygulanan düzeltmeler

- Mobil bütçe RAM miktarından bağımsız olarak 30 FPS hedefler. Dünya render ölçeği en fazla 0,85; uzun kenar en fazla 1600 piksel olacak şekilde hesaplanır. Ekran ve HUD doğal çözünürlükte kalır. Mobilde SSAO ve ek SMAA geçişi kaldırıldı; mevcut HDR, bloom, 2× MSAA ve gölgeler korundu. Mevcut daha sakin bakım efektleri seçilir. Bu ayarlar hedef bütçedir; her telefonda sabit 30 FPS garantisi değildir.
- Boşta kedi davranışının tekrar eden ağır mini oyun sorgusu kaldırıldı; gerçek mini oyun açılma/kapanma engeli korundu. HUD renk sahipliği çatışması ve gereksiz tekrar çizimleri giderildi. Yüzde ve tutorial ilerleme metni yalnız ilgili değer değiştiğinde yenilenir.
- Tutorial mevcut seçili kedinin gerçek baş kemiğine, canlı joystick'e, ihtiyaç göstergelerine ve oda hedeflerine bağlanır. Eski HUD taşıma/yükseltme işlemi kaldırıldı. Spotlight son yerleşimi takip eder; güvenli alan, ölçekli üst nesne ve asimetrik boşluk hesabı düzeltildi.
- İlk kullanım kartları mevcut Storybook yüzeyleri ve seçili kedi görseliyle güncellendi. Atla düğmesi üst HUD altında ayrılmış konuma taşındı. Konuşma kartının yanlış katmanda kalması ve ihtiyaç anlatımında göstergelerin gizlenmesi düzeltildi; modal dokunma engeli korundu. İlk kullanımda gizli portreden kalan boş panel alanı daraltılır, bitince normal HUD genişliği geri gelir.
- Beş adım, gerçek okşama ve hareket şartı, atlama/duraklatma davranışı ve tek seferlik 180 jeton ödülü korunur. Kedi modeli, yürüyüşü, klipleri ve oda yerleşimleri değiştirilmedi.

Toplam **10 runtime C# dosyası** değişti: `CatIdleBehavior`, `EnergySystem`, `HungerSystem`, `MobilePresentation`, `ModernHomeStrip`, `PetTutorialHint`, `StorybookHudLayout`, `ThirstSystem`, `TutorialSpotlight`, `Home/HomeHudVisibility`.

## Doğrulama

Kanıt klasörü: `Docs/QA/PHONE_PERF_ONBOARDING_2026-09-26` (yerel, Git dışında).

| Son kabul kanıtı | Sonuç |
|---|---:|
| `editmode-performance-final.xml` | 36/36 EditMode |
| `native-onboarding-848-tr-final.xml` | 4/4 PlayMode, 848×392 TR |
| `native-onboarding-1920-en-final.xml` | 4/4 PlayMode, 1920×1080 EN |
| `native-onboarding-2400-tr-final.xml` | 4/4 PlayMode, 2400×1080 TR |
| `native-fresh-idle-2400-fourth.xml` içindeki iki mini oyun sahne yaşam döngüsü kontrolü | 2/2 PlayMode |
| `validator-final.json` | 0 hata / 0 uyarı |

**42 benzersiz kontrol; çözünürlük tekrarları dahil 50 seçili çalıştırma** (36 + 12 + 2). Ara başarısız XML'ler geçmiş kanıttır, son kabul sonucu değildir. Gerçek ilk açılış akışı; misafir girişi, isim verme, dört tanışma konuşması, gerçek giriş sistemiyle kedi üzerinde kaydırma, joystick, bilgi adımları, ödülün yalnız bir kez verilmesi ve normal HUD'a dönüşle yürütüldü. Canlı hedefler, gerçek UI ışınları, ihtiyaç yazılarının görünürlüğü, hareketli hedefler ve güvenli alan kontrol edildi. Testler ayrı QA kayıt dizini ve geri yükleme garantili tercih snapshot'ı kullanır.

Aynı Windows Editör/RTX 5060 ortamında, 500 örnekli yalnız sorgu mikroölçümü: eski ortalama **0,8383268 ms**, gerçek yeni yöntem **0,0006804 ms** (`idle-query-final-comparison.json`). Bu değerler bütün kare süresi veya telefon FPS ölçümü değildir.

İzole mobil URP kopyasıyla 2400×1080 önizleme: ölçek **0,6666667**, SSAO kapalı, HDR açık, MSAA 2, gölge mesafesi 16. Görsel `mobile-budget-preview.png`; geçici pipeline ve kamera ayarlarının geri döndüğü `mobile-budget-preview-restored.json` ile doğrulandı. Bu da editör önizlemesidir.

## Koruma ve açık teslim durumu

20:02:54 UTC'de DX12 editöründe yerel GPU/device hatasıyla çökme yaşandı. Üç gerçek kayıt ve 16 tercih doğrulandı; aynı CatHome projesi Unity **6000.4.4f1 / DX11** ile yeniden açıldı. Diğer Tap/Unity Hub süreçlerinde değişiklik yapılmadı. Bu editör olayı telefon ölçümü olarak yorumlanmaz.

`preservation-final.json`: başlangıçtaki 7577 dosyanın 7563'ü aynı, yalnız beklenen 14 dosya farklı (10 runtime + 4 mevcut test/assembly dosyası), eksik/okunamayan dosya yok. Dört yeni test ve dört meta eklendi. Üç gerçek kayıt, mevcut sahneler, prefablar, modeller, animasyonlar, sesler, fontlar ve ProjectSettings aynı. Fontlar ve EditorSettings bu görevin güncel başlangıç baytları/hashleriyle geri getirildi; tarihsel kayıt geri yüklenmedi. Editörün bu çökmede oluşturduğu üç recovery sahnesi ve metaları silinmeden QA/crash-recovery altında saklandı.

**APK başarılı:** `Builds/Android/CatHome_Test_0.1.0_PerfTutorial_20260926.apk`. IL2CPP Release / ARM64 / LZ4 / StrictMode; 20:18:05–20:24:21 UTC, 375,30 saniye, **0 hata / 21 uyarı**. Uyarılar: 10 eski API kullanımı, 3 kullanılmayan alan, 7 URP shader derleyici uyarısı ve 1 tanılama sembol ayarı. Ayrıntılar `build-final.json` içinde; uyarılar sıfırmış gibi sunulmaz.

APK **301.384.899 bayt**; SHA256 `46BCD27F65B59B8DFE81FEFC55B4E3B8A9CC96EFD472921C1606CA535AB7973E`. APK v2 imzası doğrulandı; paket `com.vexorialabs.cathome`, sürüm 0.1.0/code1, arm64-v8a, minimum SDK25/hedef36. Android Debug sertifikalı test paketidir. Telefona yükleme ve fiziksel FPS kabulü yapılmadı; mevcut oyunu silmeden güncelleme denenmelidir.

Son `editor-final.json`: 16 tercih başlangıçla aynı; Play/QA/derleme/profiler kapalı, üç temiz normal sahne, Unity açık ve doğru CatHome projesine bağlı. `native-final-manifest.json` son kabul listesidir; `index.html` üç çözünürlükte 36 seçili gerçek ekranı gösterir. 1920 İngilizce turundan sonra yalnız aynı içeriği üreten ilerleme metni önbelleği eklendi; son iki Türkçe tur bu değişikliği de içerir. Başlangıç/süre sınırı yeniden başlatılmadı; kesin kapanış `closure.json` içindedir. Commit/push/yayın yapılmadı; yeni genel UI veya performans turu kendiliğinden başlamaz.
