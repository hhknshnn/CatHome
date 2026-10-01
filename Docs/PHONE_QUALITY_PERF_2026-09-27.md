# Redmi Note 9 Pro — kalite ayarlarını koruyan FPS artışı, 27 Eylül 2026

Kullanıcı mevcut görüntü kalitesini düşürmeden FPS artışı istedi; çalışma sırasında 30 dakika kesin teslim sınırı koydu. İlk başlangıç 26 Eylül 21:23:00 UTC; kısaltılmış sınır 21:46:04–22:16:04 UTC. Teknik kapanış zamanı `QA/PHONE_QUALITY_PERF_2026-09-27/closure.json` içindedir. Süre yeniden başlatılmadı.

## Yapılan değişiklik

Yalnız `ProjectSettings/ProjectSettings.asset` içindeki Android Vulkan ret listesine bir koşul eklendi: ürün kodu `^joyeuse_eea$` **ve** GPU adı `^Adreno \(TM\) 618$`. Bu Redmi varyantı mevcut OpenGLES3 yedeğini seçer. Diğer cihazların Vulkan→OpenGLES3 sırası aynı kalır. Bu seçim tüm eski telefonlara genellenmedi.

Kalite seviyesi, 2400×1080 ekran/HUD, mevcut 0,6667 dünya ölçeği, HDR, Bloom, 2×MSAA, 1024 gölge haritası, 16 metre gölge mesafesi ve yumuşak gölgeler korunur. Önceki turun SSAO kapalı ayarı aynı kalır. Model, animasyon, malzeme, shader, sahne, prefab, hareket ve öğretici değiştirilmedi. Daha hızlı olduğu kanıtlanmamış shader/statik birleştirme/CPU önerileri uygulanmadı.

## Fiziksel sonuç

Redmi Note 9 Pro, Android 12 / Adreno 618, aynı kayıt ve aynı ekranlar. Aşağıdaki dört sonuç **normal Release APK** ölçümüdür; teşhis bileşeni son APK'nın IL2CPP metadata'sında yoktur, Frame Timing Stats kapalıdır. Son APK normal açıldı; `-force-gles` komut satırı kullanılmadı.

| Ekran | Önce FPS | Sonra FPS | Artış | Önce p95 | Sonra p95 |
|---|---:|---:|---:|---:|---:|
| Ana menü | 19,92 | 25,48 | %27,9 | 50,16 ms | 50,18 ms |
| Salon, normal HUD ve bekleyen kedi | 15,06 | 19,56 | %29,8 | 66,89 ms | 66,81 ms |

Her örnek yaklaşık 30 saniye gerçek uygulama SurfaceView sunum zamanlarını kullanır; kapsam 29,47–29,92 saniye, halka boşluğu 0 ve ilerleyen zaman damgaları doğrulandı. Esas dosyalar `device/release-before-menu`, `release-before-room`, `release-after-menu`, `release-after-room` altındaki `summary.json`, ham zamanlar ve önce/sonra PNG'lerdir. Ortalama yükseldi; p95 belirgin iyileşmedi. **Sabit 30 FPS sağlanmadı.** Bütün odalar, mini oyunlar, yürüyüş/etkileşim yükü ve uzun süreli ısınma kabulü değildir.

Menü ve salonun gerçek telefon görüntüleri incelendi; yerleşim, doku, ışık/gölge ve HUD'da belirgin kayıp görülmedi. Animasyon anı ve ihtiyaç değerleri farklı olduğundan piksel eşitliği veya kullanıcı görsel onayı iddia edilmez. Telefon şarjdaydı; kısa örnekler farklı sıcaklıklarda alındı. Thermal durum alanının 0 olması tek başına termal kısıtlama yokluğu kanıtı sayılmaz.

## Teşhis ve sınırları

Aynı geçici teşhis APK'sında grafik API adı doğrudan okunarak Vulkan ve OpenGLES3 karşılaştırıldı; kalite alanları aynıydı. Vulkan menü örneğinde ortalama ana iş parçacığı yaklaşık 17,8 ms, GPU yaklaşık 55,9 ms ve sunum beklemesi 38,1 ms idi. Görüntü çizimi/sürücü yolu hedefli denemeyi haklı çıkardı. OpenGLES frame-timing GPU sayıları kendi başına karşılaştırma kabulü değildir; son FPS ölçümleri dışarıdan ve teşhis kodu olmadan alındı.

`probe-baseline-room` ve `gles-room` adlı ilk tanı görüntülerinde “Yeniden hoş geldin” penceresi açıktır; bunlar açık salon sonucu değildir. `gles-room-open` gerçekten açık salondur. Başlangıç yüklemesini içeren `probe-baseline-menu` de sabit menü sonucu sayılmaz. Bu ara örneklerin hiçbiri yukarıdaki son tabloya alınmadı.

Son normal sürümün Unity başlangıç log filtresi boş döndü; son paketten canlı API adı bağımsız okunamadı. Dar kapsamlı proje filtresi, normal açılış, doğru kurulu paket hash'i ve Release ölçümlerindeki kazanç doğrulandı; teşhis APK'sının canlı API kanıtı ayrı tutulur. Filtre davranışı Unity'nin [resmî Vulkan filtreleme açıklaması](https://docs.unity.cn/6000.0/Documentation/Manual/allow-deny-vulkan-usage.html) ile uyumludur.

## APK, koruma ve kapanış

`Builds/Android/CatHome_Test_0.1.0_QualityPerf_20260927.apk`: 301382655 bayt; SHA256 `EEAF1CB045764690D7FA4BEEBA65CA03D5503E642C2E2DE51F8B3E5687A428B8`. IL2CPP Release ARM64, LZ4/StrictMode, 299,31 saniye, 0 hata / 21 uyarı. APK v2 imzası ve önceki test paketiyle aynı sertifika doğrulandı. Telefondaki base.apk hash'i birebir aynı.

Uygulama mevcut kurulumun üzerine güncellendi; kaldırma/veri temizleme yok. Ana ve recovery kayıtları son kurulumun hemen öncesi/sonrası byte aynı kaldı. Normal açılış/ölçüm sırasında ihtiyaç ve zaman alanları doğal olarak güncellenir; tarihsel kayıt geri yazılmadı. İlerleme kontrolü `phone-progress-final.json` içindedir.

Güncel görev başlangıcının 7585 proje dosyasından 7584'ü aynı; yalnız yukarıdaki ProjectSettings değişti. Ek/silinmiş dosya ve okuma hatası 0. Geçici QA dosyaları bu sayımın dışında ve Git dışında kalır. Üç gerçek PC kaydı ve 16 tercih aynı; Play/test/derleme kapalı, üç temiz normal sahne, Unity açık. Son normal APK'da teşhis türünün bulunmadığı doğrulandı. Telefonda teşhis istek/rapor dosyaları kaldırıldı; SurfaceFlinger ölçümü kapatıldı, ekranı şarjda açık tutma ayarı özgün 0'a döndü. Oyun normal arka planda, yeni APK kurulu kaldı. Commit/push/yayın yok.

Esas kanıt: `build-final.json`, `artifact-final.json`, `preservation-final.json`, `editor-final.json`, `native-final-manifest.json` ve `closure.json`. Kullanıcının süre sınırı içinde bu değişiklik teslim edildi; başka bir geliştirme turu kendiliğinden başlamaz.
