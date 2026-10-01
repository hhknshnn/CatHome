# CAT HOME — Exact reference geometry rebuild, 28 Eylül 2026

Kullanıcının yeni talebi önceki geometri kilidini kaldırdı; yalnız üst HUD ele alındı. Başlangıç yaklaşık 19:30 UTC. APK, commit, push veya yayın yok. Son kapanış ve ölçüler `QA/TOP_HUD_EXACT_2026-09-28/closure.json` ve `geometry-comparison.json` ile okunur.

## Ölçüm

HUD-Ref.png 1672×941, gerçek Game View 1920×1080. Referansın görünür dış kenarları kaynak çözünürlükte elle ölçüldü; belirsizlik ±2 kaynak piksel. Gölge/glow dışarıda. Referans 1920×1080'e oranlandı; ham ve oranlı kutular `reference-measurements.json`, çizilmiş sınırlar `reference-measured.png`. Başlangıç gerçek görüntü ve RectTransform ölçüleri `before.png` / `geometry-before.json`.

| Öğe | Önce | Referansa taşınan kök boyutu |
|---|---:|---:|
| Profil | 330×80 | 357×103 |
| Tokluk | 318×68 | 349×90 |
| Su | 318×68 | 318×90 |
| Enerji | 318×68 | 329×90 |
| Coin | 164×68 (kısa değer) | en az 187×73 |
| Diamond | 164×68 (kısa değer) | en az 184×73 |
| Menü | 64×68 | 77×75 |

Portre halkası 78→103, ihtiyaç madalyonları 96, ihtiyaç ikon kutusu 82×84, para ikonları 64, profil rozeti 36, artı 50. XP izi 104×8→137×12. Referans koşulundaki gerçek kök ölçülerinin genişlik/yükseklik farkları %1 altında, konum farkı en çok yaklaşık 1,5 piksel. Son ikon merkezi farkları ihtiyaçlarda yatay ≤0,15/düşey ≤1,35; para ikonlarında yatay ≤1,63/düşey ≤3,85 piksel. Ana grup boşlukları referanstan en çok 2,5 piksel ayrılıyor (`spacing-icon-centers.json`). Bu, render edilen her pikselin aynı olduğu iddiası değildir; taşan medalyonlar, kaynak saydamlığı ve ışık yayılımı ayrıca görsel karşılaştırıldı.

Büyük gerçek bakiyelerde para kutuları içerik kadar genişler; ihtiyaç grubu aradaki boşluğa hafif küçülerek sığar. Örneğin 123,456/9,876 için ihtiyaç yüksekliği ~81,1 olur; referansın 0/0 koşulunda ~89,8. Kutular çakışmaz ve sayılar taşmaz.

## Blender kaynakları ve uygulama

`ArtSource/Blender/TopHudExact/build-panels.py`, `build-icons.py`, `panels.blend`, `icons.blend` tam düzenlenebilir kaynaklardır. Yerel Blender 5.2 Cycles ile render edildi. Başka açık Blender sahnesi değiştirilmedi. 23/23 kenara değmeyen şeffaf PNG `Assets/Resources/TopHudExact` içinde, Unity sprite importu görünür alfa sınırına iki kaynak piksel filtre payıyla oturur. Panel masterları 1024 genişlikte; ikonlar 512; yuvarlak yüzeyler yaklaşık 384. PNG'nin geometrisini raster çizimle üretme veya ImageGen kullanma yok.

Profil/ihtiyaç/para/menü/artı yüzeyleri gerçek katmanlı Blender enamel/geometri/ışık renderlarıdır. Altın kenar, beyaz üst yansıma, ince alt derinlik ve statik ışık yıldızları sprite içinde bulunur. Tokluk pembe jel barı, cyan su, violet enerji; canlı dolum mevcut Image bağlantısıyla sürer. Kedi jetonu altın kenarlı kabartmalı cameo; elmasın yüzey yönleri ve faset çizgileri yeniden işlendi. Portre mevcut seçili ırktan gelir.

Yalnız `StorybookHudLayout.cs`, `StorybookHudDetails.cs`, `TopHudProfileProgress.cs` değişti. Sonuncusu gerçek Home XP oranını yeni XP izinin gerçek genişliğine uygular; XP/seviye/ekonomi hesabı değiştirilmedi. Görsel kurulum tekrarlandığında dolumun iz yatağının üstünde ve ikonların madalyonlarının üstünde kalması sağlandı. Button hedefleri gerçek mevcut davranış sahiplerine bağlıdır; dekorlar raycast almaz. Yeni animasyon eklenmedi.

## Doğrulama ve görseller

- 1920×1080 gerçek çalışan Unity Game View, ayrı QA kayıt kopyası.
- %37/%62/%81 ihtiyaçlar ve gerçek dolum oranları; seviye 2, XP 375; 123,456/9,876 servis değişiminden HUD'a ulaştı. `runtime-bindings.png`, `live-bindings-final.json`.
- Dört gerçek düğmede merkez + dört iç köşe: 20/20 global EventSystem raycast. Menü, profil, coin artısı ve diamond artısı gerçek pointer-click ile doğru pencereleri açtı: 4/4 (`button-events-final.json`, `raycast-final.json`). Son hizalama ve renderlardan sonra tekrarlandı. Satın alma yapılmadı.
- Referans koşulunda hedef metin taşması 0, konsol hata/uyarı 0. Tam oyun/test paketi veya fiziksel telefon denemesi yapılmadı.
- `final-live-values.png`: güncel oyuncu kaydının ayrı kopyasındaki gerçek değerler.
- `final-game-view.png`: aynı çalışan Game View'da, yalnız QA kopyasının gerçek servislerine 100/100/100, 0/0, seviye 1/XP 0 verilmiş referans karşılaştırma koşulu. HUD metnine sabit değer yazılmadı; gerçek kayıt değişmedi.
- `reference-overlay-50.png`: aynı boyutta %50 üst üste görünüm. `reference-difference-2x.png`: farkın iki kat parlaklığı. `top-overlay-50.png`, `top-difference-2x.png`, `bounding-box-overlay.png`, `comparison.png`, `index.html` inceleme çıktıları.

Oda, kamera ve eşyalar referansa benzetilmedi; tam kare fark görselindeki dünya farkları HUD hatası diye değerlendirilmez. Referansın beyaz kedisi yerine oyuncunun seçili turuncu kedisi korunur. Jeton/ay/damla/elmas siluetleri ve yansıma dağılımı birebir aynı değildir. Genel ölçek farkı kapatıldı; estetik kullanıcı kabulü henüz alınmadı.

## Koruma

Bu turdaki güncel başlangıçtan 8.112 okunabilen mevcut dosya hash'i alındı. Kaynak animasyonlardan biri yine kabuk tarafından okunamadı; onun için koruma hash iddiası yok. Son koruma dosyası `preservation-check.json` esas: yalnız üç üst HUD sunum kaynağı farklı, diğer 8.109 aynı; eksik dosya/son okuma hatası yok. Gerçek kayıtlar, mevcut alt gezinme/joystick/oda/kamera/eşya/oynanış/ekonomi kaynakları, sahneler, ayarlar ve mevcut fontlar aynı. 33 tercih bu turun başlangıç değerlerine döndürüldü. Tarihsel kayıt geri yüklenmedi.

Son Play/QA/derleme kapalı, üç normal sahne temiz, Unity açık bırakılır. Yeni çalışma başlatılmaz. APK üretilmedi.
