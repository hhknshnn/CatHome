# Android ses, paket boyutu ve cihaz geri bildirimi — 7 Eylül 2026

Kullanıcının üç ekran görüntüsü ve `3.mp4` kaydı incelendi. Çalışma 7 Eylül checkpoint'i ve üst klasördeki AGENTS.md üzerinden sürdürüldü. Bu raporun Android ayarları, eski belgelerdeki mobil tam çözünürlük / yüksek gölge / ek SMAA beklentilerini günceller; PC sunumu ve onaylı oda yerleşimleri korunur.

## Bulgular ve düzeltmeler

- **Tekrarlayan elektronik ses:** videonun ses izindeki baskın frekanslar, `HomeAudioController` tarafından üretilen `Home_CozyLoop` notalarıyla eşleşiyor. Bu bir ses izi/kaynak kod karşılaştırmasıdır; kayıt doğrudan dinlenemedi. Sentetik sürekli müzik, ortam cıvıltısı ve her dokunuşta otomatik çalan UI plop kaldırıldı. Bakım, satın alma ve ödül olaylarının sesleri kalır; kullanıcının ses tercihleri değiştirilmez. Ev sesi artık tek, döngüsüz AudioSource kullanır.
- **Açılıştaki sol şerit:** tam ekran sanat SafeArea altında daralıyordu. `TitleScreenBuilder` sanat katmanını Canvas köküne taşır; butonlar SafeArea içinde kalır. Son prefab ve UI sahnesi birlikte güncellendi.
- **Karışık dil:** isim isteme, dört tanışma cümlesi ve bakım öğreticisinin metinleri `GameContentCopy` ile Türkçe/İngilizce seçimine bağlandı. Kaydedilmiş öğretici adımı ve kedi adı korunur.
- **Kapıya giren kedi:** banyo, mutfak ve yatak odasının yeni görünür kapısında katı collider yoktu. Yapısal arka duvar, kapının önündeki görünür hacmi korumuyordu. `HomeRoomPremiumFinishBuilder.EnsureDoorCollision` gerçek kapı mesh'ini ölçerek katı kutu üretir. Üç sahne güncellendi; builder tekrar çalışınca aynı koruma üretilir. Ek kayıt denemesinde eski z=2,50/2,70 konumunun oda sınırınca z=2,47'ye alınması, yeni engelin içinde kalıp duvar arkasına itilmesine yol açtı. `RoomDoorObstacle` yalnız kapıyla çakışan eski kaydı açık ön tarafa düzeltir; geçerli pozlar aynen kalır. Gerçek kayıt dosyası yerine kopyada üretildi; önceki başarısız fizik deneyi `door-saved-pose-before.json` içinde saklandı ve üç kapıda regresyon testine eklendi.

## APK ve gereksiz içerik

Eski test paketi: `C:/Users/HAKAN/Desktop/Builds CatHome/CatHome_Test_0.1.0.apk`, **336.340.043 bayt = 336,34 MB = 320,76 MiB**. Arşiv girdilerinin açılmış toplamı 430.187.420 bayttır; bu, Android'in kurulum sonrası depolama veya RAM ölçümü değildir. Unity BuildReport toplamı ayrıca sembol/yedek dosyaları içerdiğinden APK boyutu olarak kullanılmadı.

Eski tam build günlüğünde kullanıcı içerikleri 612,7 MiB, dokular 423,7 MiB görünüyordu. Eski Petshop sanatının 35 rapor girdisi yaklaşık 209,2 MiB; kullanılmayan AI inference kaynakları yaklaşık 28,6 MiB tutuyordu. Bunlar açılmış build içerik ölçüleridir, APK'dan aynı miktarda düşüş anlamına gelmez.

- Runner'ın yeni Boulevard sanatı altında devre dışı bırakılmış 1.000 eski dekor kökü hâlâ sahnede serileşiyordu. Bunların referansları, 85,3 MiB'lık eski bir tasma normal dokusu dahil kullanılmayan sanatın paketlenmesine yol açıyordu. `RunnerRetiredArtBuilder` yalnız yeni aktif karşılığı bulunan dekor kaplarının kapalı eski çocuklarını kaldırır; dokuz varyasyon kökü, havuz şablonları, uyarılar ve oynanış ölçüleri korunur. `RunnerBoulevardBuilder.Apply` temizliği son adım olarak çağırır. Runner artık Bublisher varlığına bağımlı değildir. Evde kullanılan eski bakım kaynakları silinmedi.
- `com.unity.ai.inference` için proje kodunda kullanım yoktu; paket kendi Resources içeriğini build'e ekliyordu. Manifest ve çözümlenen bağımlılık kilidi güncellendi. Kedi ırkları, animasyonları, ürünler ve TV videosu kaldırılmadı.
- `BackUpThisFolder_ButDontShipItWithYourGame` ve `BurstDebugInformation_DoNotShip` klasörleri telefona gönderilecek oyun dosyaları değildir.

## Son Android paketi

**[Yeni APK](../Builds/Android/CatHome-Android-Optimized-2026-09-07.apk)** — son kapı/kayıt düzeltmesini içerir. 7 Eylül 22:26 TRT build'i başarılı: 0 hata, 50 uyarı. Uyarıların 41'i eski Unity API kullanımına, üçü kullanılmayan alanlara, beşi URP shader derleyicisine, biri tanılama sembol ayarına aittir; ayrıntıları build JSON'unda saklandı.

| Dosya ölçümü | Eski APK | Yeni APK |
| --- | --- | --- |
| Bayt | 336.340.043 | 251.783.351 |
| MB (1.000.000 bayt) | 336,34 | **251,78** |
| MiB (1.048.576 bayt) | 320,76 | 240,12 |
| ZIP girdilerinin açılmış toplamı | 430.187.420 | 345.637.699 |

**84,56 MB / %25,14 azalma.** İki APK da `apksigner verify` kontrolünü geçti; SHA-256 sertifika özeti aynı: `04dba1e4d89a570617665f2afc2e0c7cffff89bd9caeb70f3503d6a8fc272f44`. Uygulama kimliği `com.vexorialabs.cathome`, sürüm `0.1.0` / kod 1, mimari ARM64 aynı. Mevcut uygulamayı kaldırmadan güncelleme olarak kurulabilir.

Yeni APK SHA-256: `18826e143f46de60014fef806a5eb76d1541b77027377bef7de3a1a15ae16597`. [Ölçüm kaydı](QA/ANDROID_AUDIT_2026-09-07/apk-comparison.json) doğrudan dosyadan üretilmiştir.

Son build'de AI inference kaynağı sayısı sıfırdır. Kedi modelleri/animasyonları hâlâ en büyük içerikler arasındadır. Bakım prefablarının korunan eski model/materyal bağları yaklaşık **38,0 MiB** açılmış içerik taşır; dört büyük doku bu miktarın çoğunu oluşturur. Bu bağlar bu geçişte kaldırılmadı; daha ileri paket küçültme için ayrı adaydır. Toplam raporlanmış paket içeriği 431.023.914 bayttır; bu da cihaz RAM ölçümü değildir.

## Mobil çizim maliyeti

| Ayar | Önce | Sonra |
| --- | --- | --- |
| Dünya render ölçeği | 1,00 | 0,85; HUD yerel çözünürlükte |
| MSAA | 4× | 2× |
| Ek kamera SMAA | High | Mobilde kapalı |
| Ana ışık gölgesi | 2048, iki cascade | 1024, tek cascade, 16 m |
| AO örnekleme çözünürlüğü | Tam | Yarım; renk/yoğunluk korunur |
| Açılış kedi render hedefi | En az HD, 4× AA | En uzun kenar en fazla 1600, büyütme yok, 2× AA |
| Tam açılışın arkasındaki oda | Çiziliyor | Mobilde geçici culling mask 0; kapanınca geri yüklenir |
| Mobil hedef kare hızı | Açık seçim yok | Bildirilen RAM ≤4 GB ise 30; üstünde 60 |

Render ölçeği 0,85, dünya hedefindeki piksel sayısını matematiksel olarak %27,75 azaltır. Bu bir ölçülmüş FPS artışı değildir. Açılış kedilerinin animasyonu, azaltılmış hareket tercihi ve render hedefi temizliği korunur.

Unity kurulum günlüğü tablette `SM-X200`, başka bir kurulumda `SM-S916B` gösteriyor. Kullanıcının APK/modeli yeniden bulması gerekmedi. İnceleme sırasında `adb devices -l` boştu; gerçek tablet FPS, GPU zamanı, RAM ve ısınma testi yapılamadı. 2,605 saniyelik videonun 96 kodlanmış karesi ve değişken kare zamanları cihaz performans ölçümü yerine kullanılamaz.

## Doğrulama ve kanıt

- Tam EditMode: **467/467**.
- Native PlayMode: **9/9**; üç kapıda gerçek CharacterController ile çarpışma/geri çıkış, döngüsüz ses, on ırklı açılış iskeleti/render ömrü, hareket azaltma/odak, Runner sahne geçişi/havuzu ve 800 coin/engel sırası dahil.
- LevelContentValidator: **0 hata / 0 uyarı**.
- Canlı QA kopya kayıtta çalıştırıldı. Gerçek kayıt SHA-256: `7cadd99f939ad7966a5d5c155b36da5642442aab2798dae48142d6d302175a08`; önce/sonra aynı. Mevcut CP2 yedeğinin özeti de değişmedi.
- [Kanıt galerisi](QA/ANDROID_AUDIT_2026-09-07/index.html): ekranlar, giriş videosu kareleri, build sayıları ve test XML'leri. Tanışma fotoğrafı gerçek üretim öğreticisini QA kaydında yeniden gösterir; HUD'suz fotoğrafta kamera viewport'u geçici tam çerçevedir. Banyo karesi 1920×1200 Mobile kalite profilidir; editör çekimi fiziksel tablet testi sayılmaz.
- QA sonunda Play/QA kapatıldı; GameScene + CatHome_UI + LivingRoom_Level01, tek etkin kamera düzenine dönüldü. Dil ve kalite tercihleri geri yüklendi. Commit/push yapılmadı.
