# Salon kedi eşyaları — 11 Eylül 2026

**Son kullanıcı kontrolü:** Grup 2 mama kabının ağız hizası ayrıca düzeltildi ve gerçek mama teması on ırkta ölçüldü. [Son rapor ve görüntü](CERAMIC_MEAL_2026-09-11.md). Bu ilk turdaki tamamlanma kontrolü seramik kabın ağız temasını ölçmüyordu; galeride o ürünün kartı yeni görüntüyle güncellendi. Aşağıdaki ilk tur sayıları tarihsel kapsamdır.

17 CAT eşyası gerçek eşyalı salonda denendi. Mevcut beş eşya ve depodaki diğer on iki eşya dört gruba ayrıldı; normal **5 CAT / 1 yatak** sınırı korundu. Kullanıcının kendi Unity incelemesi bekleniyor; salon konusu henüz kapatılmadı.

[Son görüntü galerisi](QA/LIVING_CAT_REVIEW_2026-09-11/index.html): 17 gerçek, tam 24 fps video; 51 başlangıç/çalışma/bitiş PNG'si. Üç eski video karşılaştırma içindir. Son kayıtlar `verified`, önceki eksik veya başarısız denemeler son sonuç değildir.

## Düzeltilenler

- **Ani dönüş / yerinde yürüme:** On beş zenginleştirme eşyasında boş yol adımı kaldırıldı. Kısa dönüşler doğal süreyle yapılıyor; yürüme 1.5 m/sn. Tırmalama direği ve top sepetinin ani dönüşleri de yumuşatıldı. Diğer odaların tırmalama eylemlerinin yön politikası aynı.
- **Gereksiz geri yürüyüş:** Zemindeki oyuncakla iş bitince tam yürüme kapsülü sığıyorsa kedi bulunduğu yerde serbest kalıyor. Yatak, kutu ve tünelin gerçek giriş/çıkışları korunuyor.
- **Ödül ve mama bulmacası:** Maine Coon pati temasına 6–9 mm uzakta kalıyordu. Bu iki üründeki duruş 2 cm yaklaştırıldı; temas eşiği gevşetilmedi. Normal hızda her ikisinde gerçek temas doğrulandı.
- **Üç yeni yatak:** Kedi yatağı, bulut yatak ve tenteli yatakta dinlenme gövdesi kameraya ters bakıyordu. Yatağın uzun ekseninde görünür uç seçiliyor. Gerçek destek yüksekliği, gövde boyutu ve çıkış yolu korunuyor. Mevcut uyku minderinin görünür yönü aynı.
- **Top sepeti:** İlk iki vuruşu yapıp üçüncüde alan/yön bulamama giderildi. Üç vuruşun temas duruşları, top yolu ve kamera yönü başlangıçta birlikte planlanıyor. Dar alanda kısa güvenli dönüşlü rota seçilebiliyor; arama 128 düğümle sınırlı. Engel ve gerçek pati teması kontrolleri korunuyor.

Üretim değişiklikleri `CatEnrichmentActivity`, `ScratchPostActivity`, `BallChaseActivity` içindedir. Model, doku, sahne yerleşimi ve ana mama/su kabının özgün şekli/animasyonu değiştirilmedi. Önceki tamamen siyah Oriental beyazlık incelemesi salt okunurdu; bu çalışma o malzemeye/gölgeye müdahale etmez.

## Doğrulama

- Son gerçek oyun düğmeleriyle **17/17** tam rutin; her biri tek tamamlanma, kontrolün geri dönmesi ve tam yürüme kapsülü için açık çıkış. Dinlenmede gerçek **Kalk** düğmesi kullanıldı.
- On beş eşya × on ırk **150/150** matris; son pati mesafesi ayarından sonra başarılı. Son yatak yönü ayrıca dört yatak × on ırk **40/40** ile doğrulandı. Üç yeni yatakta ölçülen gövde destek alanı içinde kaldı; en düşük gerçek dinlenme yönü dot .9325.
- Son top sepeti ve tırmalama koduyla on ırk × iki eşya **20/20**. Beş CAT eşyası bulunan salonda altı ayrı kabul edilen başlangıçtan **6/6** tam üç vuruş; tek tamamlanma ve yumuşak dönüş sınırı geçti.
- **9 benzersiz native testin son sonuçları başarılı.** Son kayıtlar `native-nap-facing.xml`, `native-basket-route.xml`, `native-final.xml`; etkilenmeyen dört kontrolün kaydı `native-enrichment.xml`. İlk matristeki iki temas hatası ve ara sepet başarısızlıkları son sonuç değildir. Birleştirme `verification-summary.json` içinde.
- İçerik doğrulayıcı **0 hata / 0 uyarı**. Tam proje EditMode paketi ve bütün odaların tüm animasyonları yeniden çalıştırılmadı. Telefon performansı ölçülmedi; yeni APK üretilmedi.

## Kullanıcının denemesi

Unity'de **Tools > Cat Home > Salon Eşya Denemesi** paneli açık; teslim anında Grup 2 gösteriliyor. Bir grup seç, istediğin eşyada **Yanına getir**, ardından oyundaki eylem düğmesine bas. Eylem sırasında grup değiştirme kilitlidir. Yataklarda **Kalk** kullanılır.

| Grup | Eşyalar |
|---|---|
| 1 — önceki beşli | Top sepeti, tırmalama direği, oyun tüneli, çıngıraklı teker, uyku minderi |
| 2 | Kedi yatağı, oyuncak fare, mama kabı, tüy oyuncağı, top pisti |
| 3 | Bulut yatak, ödül bulmacası, kurdele minderi, mama bulmacası |
| 4 | Tenteli yatak, kedi çimi, karton kutu |

Oturum ayrı kayıt kopyasında çalışır. **Denemeyi bitir · normal salona dön** veya Unity Play'i durdurmak QA ayrımını kapatır, önceki 16 tercihi ve gerçek kaydın salt okunur salon ön izlemesini geri getirir. Bu çıkış, QA dilini İngilizceye değiştirme dahil gerçek durdurmayla doğrulandı: Türkçe ve 16 tercih birebir geri geldi. Kanıt `manual-stop-final.json`.

## Korunan durum

Gerçek ana kayıt/recovery başlangıç ve doğrulama sonu SHA-256: **3558D75B36B1710960624B629C8F0C2C51A1E1B231E2572B12984F825F896B9B**. CP2: **03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D**. Üç dosya ve sekiz oda sahnesi aynı. Eski kayıt hash'leri geri yüklenmedi.

Teslimde QA/Play kullanıcı denemesi için açık, derleme kapalı; üç temiz sahne, tek kamera/ses dinleyici, normal zaman ve kayıt hızı. Ayrı kopya `Library/UiQaSession/20260911-080149`. Son durum `manual-ready.json`, görüntü `manual-ready.png`. Geçici font/EditorSettings değişiklikleri geri alındı. APK, arşiv, commit/push, yayın veya kapatma yapılmadı.
