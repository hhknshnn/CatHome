# 30 FPS için shader denemesi — kaynak geri alındı, telefon kapanışı bekliyor

Kullanıcı son normal sürümü telefonda beğendiğini ve 30 FPS istediğini belirtti. Görüntüyü koruyan tek shader denemesi yapıldı. Başlangıç 26 Eylül 22:22:15 UTC; kesin bitiş sınırı 22:52:15 UTC. Son süre ve durum QA `closure.json` içindedir.

## Sonuç

`ModernSurfaceForwardPass.hlsl` içinde nesne ölçeği/konum dönüşümü pikselden köşe aşamasına taşındı; yalnız baskın normal eksenini seçmek için kullanılan normalizasyon kaldırıldı. Kalite ayarları aynı kaldı. IL2CPP Release ARM64/LZ4/StrictMode aday derleme 59,29 sn, 0 hata/8 uyarı geçti. Aday APK v2 imzası ve kurulu dosya hash'i doğrulandı.

| Ekran | Güncel normal sürüm | Shader adayı |
|---|---:|---:|
| Ana menü | 25,59 FPS | 26,01 FPS |
| Salon | 19,67 FPS | 20,15 FPS |
| Banyo | 19,27 FPS | Bağlantı kesildiği için ölçülemedi |

30 saniyelik gerçek uygulama SurfaceView örnekleri; tüm tamamlanan örnekler kapsam kontrolünü geçti, halka boşluğu 0. Menü/salon farkları yaklaşık %1,6/%2,4: tekrar ve ters karşılaştırma olmadan anlamlı kazanım olarak kabul edilmedi. **30 FPS sağlanmadı; aday üretime alınmadı.** Görsel incelemede belirgin kayıp yok; seçili sabit salon bölgelerinde ortalama RGB farkı 0,24–0,25/255, en yüksek kanal farkı 1. Bu tüm görüntünün birebir eşitliği değildir.

Aday shader bu görevin güncel başlangıç yedeğine hash doğrulanarak geri döndü. Son 7585/7585 proje dosyası aynı; ek/silinmiş dosya/okuma hatası 0, üç gerçek PC kaydı ve 16 tercih aynı. Kalıcı oyun kaynağı değişikliği yok. Play/test/derleme kapalı; üç temiz normal sahne, Unity açık. Otomatik kalite sistemi bu turda geliştirilmedi.

## Telefon bağlantısı ve kalan zorunlu kapanış

22:35 civarında telefon USB bağlantısı kesildi. Kullanıcı bağlantının kesildiğini doğruladı ve yeniden bağlama sorusuna **“Şu an bağlayamıyorum”** yanıtını verdi. Bu nedenle yeni deneyler durduruldu. Son ölçüm betiği SurfaceFlinger timestats'i kendi kapanışında kapattı; telefonun daha sonraki durumu okunamadı.

Telefonda son doğrulanmış kurulu dosya **adaydır**:

- `Builds/Android/CatHome_Test_0.1.0_ShaderPerfCandidate_20260927.apk`, 301404983 bayt.
- SHA256 `34FD702708EF96F59FC7FA69D2EC4E0CED9498F6CD8BE51C1EB6A8657A4BE8BB`.
- Geçici şarjda ekranı açık tutma ayarı `svc power stayon true` bağlantı yokken geri alınamadı; başlangıç değeri 0'dı.

Telefon yeniden bağlanınca yalnız şu kapanış tamamlanmalıdır: normal arka plana alma/kayıt yazımının bitmesi, güncel ana/recovery yedeği, kullanıcı tarafından beğenilmiş `CatHome_Test_0.1.0_QualityPerf_20260927.apk` dosyasını **veri silmeden üzerine kurma**, kurulu SHA256 `EEAF1CB045764690D7FA4BEEBA65CA03D5503E642C2E2DE51F8B3E5687A428B8` doğrulaması, stayon özgün0'a dönüş ve ölçüm kapatma. Geçmiş telefon kaydı geri yüklenmez. Telefon geri alma yapılmış gibi sunulmaz; kaynak ile geçici kurulu adayın farklı olduğu açıkça bildirilmiştir. Bu kalan işlem yeni bir optimizasyon turu değildir.

## Kayıt kanıtının sınırı

Güncel başlangıçta kullanıcı kayıtları seviye3/679jeton/6BondXP ve banyodadır; önceki turdaki seviye1/215jeton kaydı kullanılmadı. Normal oda geçişi ve yeniden açılış sırasında oyunun `ach.first-shop`/`ach.home-level-3` ödülleri ve ihtiyaç/zaman/can yenileme alanları güncellendi. Kurulum öncesi ana dosya arka plana geçiş kaydı tamamlanırken okunmuştu; bu yüzden ana dosyada byte eşitliği iddia edilmez. **Kurulum öncesi recovery ile kurulum sonrası ana ve recovery üçü byte aynı** (`5ABEBCC3CAD3E5A05D861DCB38AE4EA4340B6E853423E068A192D3CE00320F0E`). Kurulum öncesi/sonrası ana JSON farkı yalnız ihtiyaçlar, zaman, küçük kökY farkı ve normal mini oyun enerjisi yenilenmesidir. Kayıt silme veya tarihsel geri yükleme yok. Bağlantı kesildiği için son telefon kaydı yeniden alınamadı.

Kanıt: `QA/PHONE_SHADER_PERF30_2026-09-27` içindeki başlangıç/koruma dosyaları, `build-candidate.json`, `visual-comparison.json`, ham `device` ölçümleri ve kapanış manifesti. QA Git dışında; commit/push/yayın yok.
