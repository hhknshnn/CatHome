# UI/UX referansa uyum geçişi — 6 Eylül 2026

Kullanıcının ikinci uygulamayı referans görsellerinden hâlâ uzak bulması üzerine, yalnız tipografi değil ortak yüzey, ikon, HUD oranı ve sahne sunumu yeniden işlendi. Sonuç mevcut kediler ve mobilyalarla çalışan Unity arayüzüdür.

[Önce/sonra karşılaştırması ve gerçek HD ekranlar](QA/UIUX_2026-09-06_ReferenceMatch/index.html) · [Önceki uygulama](QA/UIUX_2026-09-06_Refinement/index.html) · [Onaylanan görsel referanslar](UIUX_References_2026-09-06/index.html)

## Uygulanan görünüm

- Mağaza çantası, koltuk, oyun kumandası, mama kasesi, su damlası, ay ve pati simgesi Blender’da gerçek geometri olarak modellendi. Şeffaf 768×768 çıktılar ortak arayüzde kullanılır. Kaynak `ArtSource/Blender/Interface/build_icons.py`; Unity çıktıları `Assets/Resources/PremiumInterface`. Ortak jeton/elmas sanat varlıkları korunur.
- Krem kartlar ve eylemler iç içe şampanya kenarları, yumuşak yüzey geçişi ve temas derinliği kullanır. Yuvarlak kenarlarda analitik alfa geçişi vardır. Bütün geometri `LowPolyPanelGraphic` içinde kalır; yeni raycast engelleyici dekor veya Shadow bileşeni eklenmez.
- Başlık ve belirgin eylemler gerçek Fredoka SemiBold, açıklamalar Nunito Sans SemiBold kullanır. 2048 SDF atlasları ve Türkçe karakterler korunur; yapay Bold uygulanmaz.
- Son gözle kontrolde Fredoka'nın eksik Türkçe harfleri yüzünden aynı kelimede ince fallback harfler bulundu. Resmi tam kaynak da bu Unicode eşlemelerini içermediğinden `ArtSource/Typography/build_fredoka_tr.py`, ş/Ş/ğ/Ğ/İ harflerini Fredoka'nın kendi aksan ve temel harflerinden oluşturur. 500/600 ağırlıklı statik dosyalar ve SDF atlasları yenilendi; değiştirilmiş aile Cat Home Fredoka olarak adlandırıldı. Kaynak ve lisans [tipografi kaydında](../ArtSource/Typography/README.md).
- Kedi kartı, ihtiyaçlar ve alt menü büyütüldü. İhtiyaç kapsülleri 260, merkez aralığı 284 birimdir. Alt menüde üç boyutlu simgeler ve ortak bir krem yüzey bulunur; dar ekranlarda joystick/bakım alanı için ölçeklenir. Para ekleme düğmeleri 44 birim dokunma alanını korur.
- Ana menüde mevcut üç kedinin canlı sahnesi, belirgin mercan devam eylemi ve üç boyutlu oda/oyun kısayolları kullanılır. Mağazanın gerçek ürün fotoğrafları korunur.
- Pencereler açılınca yalnız dünya kamerası bir kez örneklenir; iki geçişli Gauss filtresiyle yumuşatılır. Kapanışta render hedefi bırakılır. UI, kamera görüntüsüne dahil edilmez ve bulanık yüzey raycast almaz. İlk görsel kontrolde fark edilen düşük çözünürlüklü karelenme son filtrede giderildi.
- Düz duvarlarda desen üreten düşük örnekli/yarım çözünürlüklü temas gölgesi, tam çözünürlüklü Blue Noise/Depth Normals ve bilateral filtreye geçti. AO yoğunluğu 0.32, yarıçapı 0.18. Salon için gölgesiz 0.38 sıcak dolgu eklendi; mevcut ölçülü post-processing ve 0.9/0.6 ışıklar korunur.
- Sekiz odanın kamera kuralı birlikte güncellendi: FOV 44.5, yön 25/10/0. Oda önizlemeleri yeniden çekildi ve sekizli kontak sayfası gözle incelendi. Modellerin ölçüsü, yerleşimi, çarpışması ve etkileşim noktaları değişmedi.

`PremiumReferenceArtBuilder.BuildSilently()` bu geçişi ortak prefablar ve üç UI sahnesine uygular; Play sırasında çalışmayı reddeder. `PremiumUiSystemRebuild` yeni geçişi de çağırır. İşlem tekrar çalıştırılabilir; ikon veya dekor kopyaları biriktirmez.

## Doğrulama

Kanıt klasörü `QA/UIUX_2026-09-06_ReferenceMatch/`. Oyuncunun kaydının ayrı yerel kopyası kullanıldı; ödeme, reklam veya hesap sağlayıcıları çağrılmadı. Görsel örnekler para/ödül vermedi. Mağazadaki “Ücretsiz deneme” mevcut Free Test Mode ayarıdır; bu görsel geçiş fiyatlandırmayı değiştirmez.

- Son tam EditMode: **407/407** (`EditMode_Final.xml`). İlk koşudaki 3 bulgu: kamera yönlerinin odalar arasında farklı kalması ve 44 birim dokunma alanının daralması düzeltildi; oda/oyun kısayolu testi yeni gerçek Blender ikonlarını doğrulayacak şekilde güncellendi. İki yeni test, Türkçe harflerin her iki başlık ağırlığında fallback kullanmadan bulunduğunu denetler.
- Native PlayMode: ev/mini oyun geçişleri **5/5**, geri dönüşün gerçek EventSystem pointer alıcısı ve kapanışı **1/1**. Bu altı test yüzey/yerleşim ve ilk blur sürümünde geçti; son Gauss filtresi sonrasında tam EditMode ve normal Play ekran/pointer kontrolleri yapıldı. Önceki 800 ürün–ırk etkileşimi yeniden koşulmuş gibi sayılmaz.

- **53 HD kare:** 29 ev/pencere durumu, iki mini oyunda giriş/oyun/duraklatma için 6, tablet ve geniş ekran için 12, İngilizce için 6 kare. 1920×1080, 1440×1080 ve 2400×1080 doğal çözünürlük; yeniden büyütme yok.
- Tüm karelerin `GetWorldCorners` taramasında **0 OVERLAP / 0 OUTSIDE**. Yalnız hamburger menüsünün Scrim'i arkasında kalan 6 ev düğmesi beklenen şekilde erişilemez; `06_Menu_Final.layout.txt` ham kayıtları korunur. Diğer ekranlarda beklenmedik pointer alıcısı engeli bulunmadı.
- Son sahnede EventSystem raycast, pointer down/up/click gerçek `WelcomeBackButton` alıcısına gönderildi. Pencere kapandı, kedi ve dünya kontrolü serbest kaldı; açık blur render hedefi **0** oldu. Son OS fare otomasyonu denemelerinde `Application.isFocused=False` kaldığı için bu oturum için native fare başarısı iddia edilmez. `NativeReturnPointer.txt` bu sınırlamayı açıkça kaydeder.
- Son normal Play: üç sahne, bir kamera, bir ses dinleyicisi ve bir EventSystem; başlangıç katmanı kapalı, kedi kontrolü serbest, Console'da hata/uyarı yok. `FinalLiveState.txt` kaydedildi. QA kapatılıp üçlü ev önizlemesi geri yüklendi: aktif Living Room, validator **0 hata / 0 uyarı**, `playModeStartScene=null`, `DisableSceneReload`, domain reload açık. `FinalWorkspace.txt` kaydı tamamlandı.

Bu çalışma masaüstü Unity’de doğrulanır. Tam çözünürlüklü AO’nun fiziksel Android cihazdaki maliyeti ölçülmedi; cihaz performans kapısı açık kalır. Referans çizgisine yaklaşan bu uygulama, oluşturulmuş referans resmin piksel kopyası değildir. Galeri tasarım çizimleri ile çalışan oyun karelerini açıkça ayırır. Git commit/push yapılmadı.
