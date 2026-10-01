# Cat Home checkpoint — 27 Eylül 2026 gece teslimi

Teknik kapanış: **27.09.2026 04:49:25 Türkiye / 01:49:25 UTC**. Başlangıç 26 Eylül 22:40:26 UTC / 27 Eylül 01:40:26 TR; toplam **189.0 dakika**. Düzeltme fazı 00:55 UTC'de kapandı. Polish 00:58 UTC'de başladı; görsel düzenleme, derleme ve son kapanış 51.43 dakika sürdü, kendi 03:58 UTC üç saat sınırının içinde. Yeni çalışma turu kendiliğinden açılmaz.

## Altı sorunun sonucu

- **Engelde takılma:** Dönüşü engellenen kedinin yan/geri joystick girişi korumalı çıkış yapabilir. Kuyruğu kısa dönüş yönünde duvara takılan durumda diğer dönüş yönü aynı taze fizik kontrolünden geçirilir. Kontrol bırakılınca durum sıfırlanır; açık zemin yürüyüşü ve çarpışma toleransları korunur.
- **İlk koltuk düğmesi:** Kestir, 0,75 m yakınlıkta kedinin baktığı yönden bağımsız görünür. Sehpanın düz yol kontrolü keşfi gizlemez. Tıklama hâlâ doğru ön konumu, yönü ve gerçek sıçrama fiziğini ister; uygun değilse yaklaş/dön açıklaması çıkar. Otomatik yaklaşma veya ışınlama yok.
- **Bir süre sonra su içememe:** Bekleme/gerinme/temizlenme hareketi kontrolü önce doğal ayakta duruşa bırakır, sonra kap başlangıcı taze temasla değerlendirilir. Sıfır ihtiyaç, tekrar ve oda dönüşü kontrol edildi.
- **Mama eyleminin erken bitmesi:** Giriş/çıkış 0,35 sn yumuşatıldı; gerçek temasa ulaşma süresi 2 sn'ye kadar. Temas yoksa ihtiyaç/ödül verilmez. Geçerli bakım 10 sn; tekrar basma tek tamamlama üretir.
- **Boyun bükülmesi:** Her kaynak animasyon karesinde eklem sınırları ve gerçek deri kontrolü; önceki boyun çözümü yalnız başlangıç tahmini. Kaba doğru en fazla 7 cm görsel gövde eğimi, dört sabit pati, ölçülen zeminde en fazla 6 mm düzeltme. Kök, model, klip ve kemik uzunlukları aynı. Duraklatma pozu sabit tutar, taze engel denetimi sürer.
- **Tutorial/footer:** Konuşma ve isim kartı gerçek alt şerit üst sınırından 16 birim ayrılır; ekran oranı ve yerleşim değişiminde yenilenir. Giriş hareketi bu boşluğu aşmaz.

## Tema düzenlemesi

Ortak pencerelerde sakin indigo/krem/turkuaz yüzeyler, ince çerçeve, kontrollü gölge ve net eylem durumları uygulandı. Ayarlar gerçek aç/kapa anahtarlarıyla, görevler ilerleme/durum rozetleri ve açık metin sıralamasıyla düzenlendi. Ürün kartlarındaki arka plan birleşim izleri kaldırıldı; ayrıntı görselleri yuvarlatıldı. Konuşma/isim alanlarının okunabilirliği iyileşti. İngilizce günlük görev açıklamaları eylemi açıklar. Ana HUD'ın beğenilen görünümü ve dünya/model varlıkları korundu. Kullanıcı yeni polish görünümünü henüz değerlendirmedi.

## Doğrulama ve sınırlar

- Son seçili native testler **23/23 çalıştırma, 18 benzersiz kontrol**. Bütün proje test takımı değildir.
- **10 ırk × 2 gerçek 10 sn bakım**: 4.562 kare, 3.998 temas karesi, 0 güvensiz kare; 20/20 tamamlama. Deri en fazla 2,896 mm; bakım kabulü 3 mm olarak kaldı.
- Her bakım sonrasında 6 sn gerçek joystick: 20/20 çıkış, en az 1,325 m ilerleme; normal yürüyüş derisi en fazla 6,402 mm, özgün 15 mm sınırı içinde.
- 72 kap duruşu / 41 kabul / 41 temas; 6 bekleme hareketi; 6 tekrar/tek ödül; mutfakta duraklatma/iptal; 18 köşe + 18 duvar; dört yön koltuk ve gerçek ilk misafir akışı.
- **186 görünüm / 942 görünür dokunma hedefi / 0 hata / 0 taşma**. 1920×1080, 848×392, 2400×1080; Türkçe ve İngilizce. Ayarlardaki beş aç/kapa çifti işlev ve görsel olarak geçti; başlangıç değerleri döndü.
- İçerik validator 0 hata/0 uyarı. Android derleme 0 hata/21 uyarı: eski API, kullanılmayan alan, tanı sembolü ve Vulkan shader uyarıları kaydedildi.
- **Fiziksel telefon, uzun ısınma, gerçek klavye/çentik ve 30 FPS kabulü yok.** Testteki 20 FPS simülasyonu telefon ölçümü değildir. Otomatik cihaz kalite sistemi bu turda yazılmadı.
- Ara başarısız aday XML'leri tarihsel olarak saklı; esas yalnız native-final-manifest.json dosyasının gösterdiği son koşulardır.

[Gerçek Unity ekran galerisi](QA/OVERNIGHT_FIX_POLISH_2026-09-27/index.html): 186 görünüm, 12 önce/sonra çifti.
[Ayrıntılı çalışma kaydı](OVERNIGHT_FIX_POLISH_2026-09-27.md).

## Android teslimi

**Builds/Android/CatHome_Test_0.1.0_OvernightFixPolish_20260927.apk**

- 301396111 bayt; SHA256 **3B146360BB0BD2ED7CADD416877B58A47B60D732D817C2271D504864FE1B2E22**.
- IL2CPP Release ARM64 / LZ4 / StrictMode, 269.84 sn. 01:33:08–01:37:38 UTC.
- APK v2 imzası, eski iyi APK ile aynı sertifika, manifest ve ARM64 doğrulandı.
- Development/debug kapalı; önceki FPS probu ve QA kodu paket metadata'sında yok.
- Telefona kurulmadı, yayımlanmadı.

## Koruma ve kapanış

Görev başlangıcındaki 7.585 dosyanın **7.564'ü aynı**, 21 mevcut C# değişti (17 runtime + 4 test). Yeni 2 runtime + 3 test ve 5 meta var. Eksik dosya veya okuma hatası yok. Sahne, prefab, FBX, animasyon, ses, fontlar ve ProjectSettings başlangıçla aynı. İki fontun test glif önbelleği yalnız bu görevin hash doğrulamalı başlangıç baytlarına döndü.

**Üç gerçek PC kaydı ve 16 tercih aynı.** Tarihsel kayıt yüklenmedi. UiQaTestSession.End → EditorEndCopiedSession koruması aynen sürer. Son üç normal sahne temiz; Play/QA/derleme kapalı. **Unity normal çıkışla kapandı**, zorla sonlandırma yok. Kaynaklar ve belgeler diskte kayıtlı; commit/push/yayın yok, QA Git dışında.

**27 Eylül 2026 saat 10:00 Türkiye için tek seferlik bilgisayar kapatma planlandı.** Windows görevi: CatHome-OneTimeClosure-20260927. Tek TimeTrigger, tekrar yok; 10:10'da süresi biter, ertesi gün telafi çalışması yok. Güvenli kapanış işareti zorunlu; komut /s /t 0, /f yok. Başka uygulamadaki kaydedilmemiş çalışma kapatmayı engelleyebilir. Uyanma isteği etkin. Eski 09:40 Codex heartbeat'i silindi. Gerçek güç kapanması henüz gerçekleşmedi; sonrasında shutdown-issued.json / shutdown-result.json okunur. Bu görev başka güne taşınmaz, yeniden kurulmaz.

## Telefon yeniden bağlanınca

Redmi Note 9 Pro'da önceki **ShaderPerfCandidate** hâlâ kurulu; geri alma olmadı. Geçici **stayon true** ayarı da telefonda kaldı, özgün değer **0**. Yeni güncel telefon kaydını koruyarak, verileri silmeden üzerine APK güncellemesi ve stayon 0 doğrulaması yapılmalı. Tarihsel kayıt geri yükleme, uninstall veya veri temizleme yapılmaz.

Beğenilen önceki APK: CatHome_Test_0.1.0_QualityPerf_20260927.apk, SHA256 EEAF1CB045764690D7FA4BEEBA65CA03D5503E642C2E2DE51F8B3E5687A428B8. Telefonda kalan aday SHA256 34FD702708EF96F59FC7FA69D2EC4E0CED9498F6CD8BE51C1EB6A8657A4BE8BB. Yeni paket tüm gece düzeltmelerini içerir; fiziksel kullanıcı denemesi hâlâ bekler.

Esas kapanış: QA/OVERNIGHT_FIX_POLISH_2026-09-27 altında closure.json, artifact-final.json, native-final-manifest.json, preservation-final.json, editor-final.json, unity-closed.json, shutdown-schedule.json.
