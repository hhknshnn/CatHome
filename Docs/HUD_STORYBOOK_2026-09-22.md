# Oyun içi HUD — 22 Eylül 2026

Kullanıcı `07-hud-reference.png` taslağının ardından “devam et” diyerek yalnız oyun içi HUD uygulamasına izin verdi. Başlangıç **13:51:27 UTC**, kesin 30 dakika sınırı **14:21:27 UTC**. Son kapanış saati ve süre `QA/HUD_STORYBOOK_2026-09-22/closure-summary.json` içindedir; bu süre başka çalışma turuna taşınmaz.

## Uygulanan görünüm

Ana menüyle ortak indigo yüzeyler, krem yazılar ve mint ayrıntılar kullanıldı. Kedi kimliği ve üç ihtiyaç göstergesi ortak üst plakada toplandı; coin simgesi onaylı modern kedi başıyla değiştirildi. Para alanları, artı düğmeleri ve menü düğmesi aynı görsel dile uyarlandı. Alt gezinme grubunda mağaza, oda, kedi komutları ve oyunlar bulunur; mevcut oda mercan renkle vurgulanır. Joystick indigo/mint, mevcut bağlamsal eylem düğmesi mercan renktedir.

Referanstaki daha yüksek alt grup, mevcut oda kamerasının **80 birimlik ayrılmış şeridine** uyarlandı. Kamera alanı ve oynanış görüşü değiştirilmedi; simgeler ve yazılar yatay yerleşimde tutuldu. Referans bir ImageGen taslağıdır; gerçek uygulamanın birebir piksel kopyası olduğu iddia edilmez.

Yalnız HUD sunumu değişti. Düğme olayları, bakım ve etkinlik davranışları, gerçek seçili kedi portresi, ihtiyaç/para değerleri ve mevcut görünürlük kuralları korunur. Kedi komutları kısayolu çalışma anında oluşturulduğundan yalnız bu kısayola görünüm veren sınırlı bir sunum bileşeni eklendi; komut penceresi yeniden tasarlanmadı. Ana menü, diğer pencereler, kedi hareketleri ve oda içeriği bu kapsamın dışındadır.

## Kaynaklar

Yeni sınıflar `StorybookHudBuilder`, `StorybookHudBottomBuilder`, `StorybookHudLayout` ve `StorybookHudBottomPresentation`. Mevcut değişiklikler `PremiumUiFactory`, `TopHudResponsiveLayout`, `CurrencyHud.prefab`, `MainPanel.prefab`, `CatHome_UI.unity` ve `CompactHudArtTests` ile sınırlıdır. Ana menünün onaylı simgeleri yeniden kullanıldı. İhtiyaç yüzdelerinin yeniden yükleme sonrasında boş önbellekle kalması son sunum düzeltmesinde ele alındı.

## Kontroller ve sınırlar

- Seçili EditMode **8/8** geçti. İlk turdaki **7 geçti / 1 kaldı** sonucu, eski ihtiyaç göstergesi ankraj beklentisine aitti; güncellenen sözleşme sonrası başarılı tur alındı. İlk XML silinmedi: `editmode-initial.xml`; başarılı tur `editmode-hud.xml`.
- 1920×1080 ve 1440×1080 boyutlarında görünür dokuz HUD düğmesinin merkez hedefi ve güvenli ekran alanı **9/9** geçti; para göstergesi kontrolleri de geçti. Kanıtlar `raycasts-1920.json` ve `raycasts-1440.json`.
- Türkçe masaüstü ve İngilizce tablet görünümü kontrol edildi. Gerçek Unity çekimleri `screens-final/hud-final-1920.png` ve `screens-final/hud-tablet-en.png`.
- Fiziksel telefon testi yapılmadı. Bu çalışma bütün pencerelerin yeniden tasarımı veya bütün oynanış rutinlerinin yeniden kabulü değildir.

## Koruma ve kapanış

Bu göreve ait başlangıç kopyaları ve hash kayıtları kullanılır; tarihsel kayıt hash'leri geri yüklenmez. Kaynak karşılaştırmasında `GameScene.unity` ve ana menü kaynakları değişmedi. Kontrol sırasında güncellenen iki font varlığının başlangıç baytlarına dönüşü, üç gerçek kayıt, 16 tercih ve editörün son durumu için **en son** `preservation-final.json`, `editor-final.json` ve `closure-summary.json` esas alınır. Bu raporun yazıldığı anda güvenli kapanış sürüyordu; tamamlanmamış geri yüklemeler yapılmış sayılmaz.

QA kökü: `Docs/QA/HUD_STORYBOOK_2026-09-22`. QA Git dışında kalır. Commit, push, APK, video veya yayın yapılmadı. Son uygulama görünümü kullanıcı değerlendirmesine sunulur; yeni görsel onay alınmış sayılmaz. Yeni talep olmadan başka ekranlara geçilmez.

Referans: [HUD taslağı](DesignProposals/2026-09-22_Storybook/07-hud-reference.png) · [Üretim notu](DesignProposals/2026-09-22_Storybook/PROMPT_HUD_REFERENCE.md).

Son kapanış: gerçek üç kayıt aynı, 16 tercih geri geldi, Play/QA kapalı, üç normal sahne temiz. Fontların testte değişen atlasları bu turun başlangıç baytlarına döndürüldü. Son yüzde kontrastı için render öncesi yenileme kancası eklendi; süre sınırı nedeniyle bu son kanca Play içinde yeniden kontrol edilmedi. Eylem düğmesinin gerçek görünür durumu bu turda tetiklenemedi; stil kaynakta uygulanmış olsa da bu akış için görsel kabul iddia edilmez. Nihai kanıt closure-summary/preservation-final/editor-final JSON dosyalarıdır.
