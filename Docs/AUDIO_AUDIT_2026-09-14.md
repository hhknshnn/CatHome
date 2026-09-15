# Cat Home — ses incelemesi ve kaynak seçkisi

14 Eylül 2026 · Durum: inceleme ve aday ses seçimi tamamlandı; oyuna ses entegrasyonu yapılmadı.

## Sonuç

En büyük eksik, hareketin temas anında duyulan ses katmanı. Mevcut sistem yemek, su, mırlama ve miyavı karşılıyor; pati basışı, sıçrama inişi, tırmalama, kâğıt çekişi ve düşen eşyalar kendi malzemelerine özgü ses üretmiyor. Birçok rutin sonunda ortak bir elektronik tamamlanma tonu çalıyor. Öncelik gerçek temaslara ses vermek olmalı.

**[Dinlenebilir seçkiyi aç](QA/AUDIO_AUDIT_2026-09-14/index.html)** — 34 yeni ham ses adayı, 16 mevcut sesin değişmemiş karşılaştırma kopyası ve 5 ek doğrudan kayıt kaynağı. Kartlarda kullanım yeri, tetikleme anı, kaynak ve düzenleme ihtiyacı var. Seçki otomatik çalmaz; aynı anda tek ses oynatır, başlangıç dinleme seviyesi %30'dur.

Bu çalışma kaynak kodu, sahne bağlantıları, Unity'nin içe aktardığı ses listesi ve dosyaların teknik çözümlemesini kapsar. **Doğrudan kulakla dinleme, oyundaki bütün rutinlerin sesli kaydı veya fiziksel telefon ses testi yapılmadı.** Bu nedenle adayların ses karakteri için nihai onay verilmiş sayılmaz; kullanım eşleştirmeleri kaynak açıklamalarına, dosya adlarına ve oyunun gerçek hareketlerine dayanan tasarım önerileridir.

## Mevcut durum — yeni incelemenin bulguları

| Alan | Doğrulanan durum | Etkisi |
|---|---|---|
| Unity ses envanteri | `Assets` altında 16 AudioClip: 4 bakım/komut, 4 sevilme, 8 Polyperfect | Eşya, yüzey ve ortam çeşitliliğini karşılayan ithal ses bulunmuyor. Çalışma anında üretilen tonlar bu 16'ya dahil değil. |
| Bakım/komut | `CatVoice` dört Resources sesini yeme/içme/uyku/komut durumuna bağlar | Bu mevcut olay bağlantıları korunmalı. Yaklaşırken yemek sesi eklenmemeli. |
| Sevilme | Dört Pet sesi 8 oda ve bir eski sahnede bağlı | Sevilme için zaten kullanılabilir çeşit var. |
| Polyperfect | 8 sesin GUID veya isim bağlantısı birinci taraf oyun kodu/sahneleri/prefabları/kaynaklarında bulunmadı | Yeni kayıt aramadan önce kısa miyav ve iki oyuncak sesi değerlendirilebilir. Üçüncü taraf demo içerikleri bu kullanım taramasına dahil değil. |
| Ev etkileşimleri | `CatActivity.Completed` çoğu kısa rutin için ortak `Home_Activity` tonunu çağırır; komutlar ve sürekli dinlenme hariç | Tamamlanma sesi, pati/eşya temasını anlatmıyor. |
| Hareket ve eşyalar | `CatJumpMotion`, `CatSurfaceTurnMotion`, tırmalama/kum/saçılma/oyuncak rutinlerinde temas sesini çalan bağlantı yok | Sessiz görünen hareketlerin temel nedeni. Bazı bakım rutinleri ortak CatVoice üzerinden yine ses alır; bütün eşyalar tamamen sessiz denmez. |
| Runner | Sahnedeki 11 ses yuvası boş: müzik + 10 efekt. Kod kendi kısa tonlarını ve 8 saniyelik müziğini üretir | Olay bağlantıları kısmen hazır; gerçek/özenle seçilmiş kayıtlarla değiştirilebilir. |
| Catch | Catch kodunda oynanışa bağlı AudioSource/AudioClip/efekt çağrısı bulunmadı; AudioListener yönetimi var | Pati teması, başarılı yakalama, ıska ve tur sonu için ayrı ses bağlantısı gerekir. |
| Ortam ve müzik | Evde sürekli müzik/cıvıltı/otomatik dokunma plop'u önceki cihaz geri bildirimiyle kaldırılmış | Bu tercih korunmalı. Yeni ortam sesi görünür kaynağa ve odaya bağlı olmalı; müzik ayrıca değerlendirilir. |
| Ses ayarı | `PetSoundController` ana `HomeAudioService.SoundEnabled` değerini kontrol etmiyor | Sevilme sesinin sessize alma ayarından kaçması koddan görülen bulgu; bu tur canlı yeniden üretim yapılmadı. |
| Çift mırlama yolu | Sevilmede `BeginPettingAudio` çağrılır, aynı başlangıç olayı `HomeAudioController` üzerinden `CatVoice.PurrBriefly` de tetikler | Aynı anda iki mırlama kaynağı açılabilir. Sonraki uygulamada tek sahiplik ve canlı mute testi gerekir. |
| Müzik düğmesi | Ev müzik tercihi var; mevcut ev denetleyicisinde müzik kaynağı yok. Runner ayrı ses tercihiyle çalışıyor | Ses/müzik düğmeleri oyuncunun beklediği kapsamla birlikte doğrulanmalı. |

Kaynaklar: `Assets/Scripts/CatVoice.cs`, `PetSoundController.cs`, `PetInteraction.cs`, `Home/HomeAudioController.cs`, `Home/HomeAudioService.cs`, `Runner/CatRunnerAudioController.cs`, `Catch/CatCatchGameController.cs`, `Assets/Scenes/Runner/CatRunner.unity`. Önceki karar: [Android ses raporu](ANDROID_AUDIT_2026-09-07.md), [doğal bakım sesleri](PRODUCTION_PASS_2026-09-07.md).

### Ses seviyesi hakkında ölçüm

Tarayıcıda 48 kHz'e çözümlenen mevcut `CatEat` / `CatDrink` kayıtlarının bütün dosya RMS değeri yaklaşık -36 dBFS. Oyun bakım döngüsünü yaklaşık 0,25–0,28 kaynak seviyesiyle çalıyor. Bu, bakım seslerinin düşük algılanmasını açıklayabilecek bir etken; doğrudan işitsel değerlendirme değildir. `CatEat` tepe değeri zaten yaklaşık -0,86 dBFS: yalnız genel sesi yükseltmek yerine sakin gövde sesi ile ani tepelerin dengesi incelenmeli.

Seçkideki bazı dosyalarda 48 kHz çözümleme/yeniden örnekleme sonrası 0 dBFS üzeri örnek tepeleri görüldü. Bu tek başına orijinal kaydın kırpıldığını kanıtlamaz. Son hazırlamada tepe payı bırakılmalı; ham dosyalar olduğu gibi yüksek seviyede oyuna konmamalı. Ayrıntı: `audio-metrics.json`.

## Animasyon ve oyun olaylarına ses eşleştirmesi

| Hareket / kullanım | Önerilen ses ve bulunan aday | Doğru tetikleme |
|---|---|---|
| Ahşap, halı, çim, sert zemin yürüyüşü | Kenney Impact: `footstep_wood`, `footstep_carpet`, `footstep_grass`, `footstep_concrete` serileri | Pati gerçek yüzeye basınca; havadayken ve ayak sabitken sessiz. Genel ayak kayıtlarıdır; ayakkabı/topuk karakteri taşıyanlar elenir, pati ölçeğine uyarlanır. |
| Sıçrama kalkış / iniş | Öncelikli doğal kaynak: [3_Cat, jump.wav](https://freesound.org/people/16GPanskaZlochova_Eliska/sounds/496278/). Yerel karşılaştırma: `impactSoft_medium_000`, `impactWood_light_000` | Kalkış ve ilk yük taşıyan iniş ayrı olaylar; uçuş uzunluğundan bağımsız. Kaynak zıplama klibi veya hareket eğrisi değiştirilmez. |
| Basılı dönüş | İlgili yüzeyin çok kısa ve kısık pati sesi | Tek tek tamamlanan basışlar. Dört patiye aynı anda dört darbe çalınmaz. |
| Direk tırmalama | [Cat Scratching A Post — Samantha_Dolman](https://freesound.org/people/Samantha_Dolman/sounds/490730/) | Aşağı doğru her gerçek pati sürtünmesine kısa bir kesit; yaklaşma ve geri çekilmede çalmaz. |
| Dolap / ahşap tırmalama | [scratching.mp3 — leftovertunacasserole](https://freesound.org/people/leftovertunacasserole/sounds/611116/) | Ahşap yüzeyde gerçek temas. Yerel `scrape1.wav` yalnız genel sürtünme alternatifi; aynı sesin sisal/ahşap/taş için kullanılması önerilmez. |
| Kum tuvaleti / saksı eşeleme | Kedi kayıt paketindeki `10_Cat, rake.wav`; yerel alternatif Peludo `sand_footsteps_0.mp3` | Kazma ve örtmede doğrulanmış tek pati vuruşu. Kumda dinlenme/inceleme sırasında döngü çalmaz. Tuvalet evrelerine abartılı insan sesleri eklenmez. |
| Patiyle bakım / sevilme | Mevcut mırlama + [Petting-scratching a cat — Sadiquecat](https://freesound.org/people/Sadiquecat/sounds/695629/) | Gerçek tüy teması/yalama evresinde; sevilme bitince yumuşak kapanış. Tek mırlama sahibi. |
| Mama / su içme | Önce mevcut CatEat/CatDrink kaydının seviye ve süre dengesi; doğal alternatifler Cat sounds paketinde | Yalnız gerçek Eat/Drink evresi. Her baş kaldırmada yeni rastgele ses veya bitiş plop'u yok. |
| Yatak, havlu, çadır, koltukta yatma/kalkma | Owlish `blanket-movement-1/2`, Kenney `cloth1` | Örtü/gövde hareketinde kısa hışırtı; hareketsiz uykuda kumaş döngüsü çalmaz. |
| Paspas yoğurma | Çok hafif `cloth1` kesiti + mevcut mırlama | Gerçek basınç ritmi; her pati için belirgin vuruş zorunlu değil. |
| Tünel, sepet, kumaş saklanma | `cloth1`, örtü hareketi ve malzemeye göre hafif sürtünme | Görünür gövde/örtü temasında; girişten çıkışa aynı sabit döngü değil. |
| Salıncak / hamak / asılı koltuk | Kenney `creak1` + kumaş | Yeterli hareket varsa seyrek yön değişimi sesi; ağır kapı gıcırtısı gibi duyulan kayıt elenir. |
| Bardak, kupa, kase düşüşü | Kenney `impactGlass_light_000`, `impactPlate_light_000` | İlk pati itişi, ilk zemine darbe ve daha kısık sekme ayrı. Görselde kırılmayan nesneye cam kırılması eklenmez. |
| Meyve / sepet devrilmesi | Owlish `fruit1`, hafif ahşap darbe ve sepet sürtünmesi | Üç gerçek vuruş, devrilme, ilk zemin darbesi ve sekmeler. On meyvenin sesi sınırsız üst üste bindirilmez. |
| Kitap düşürme | Kenney `bookPlace1`, gerektiğinde `bookFlip1` | Kitabın kendi darbe anı; havadayken darbe çalmaz. Sayfa sesi yalnız sayfa hareketine bağlı. |
| Kâğıt rulosu | Owlish `tissue-pull`, Luckius `Paper Sound - 1` | Gerçek çekiş/vuruş, rulo dönmesi ve varsa görünür kopma birbirinden ayrılır. |
| Pikap | Owlish `record_player_loop` mekanik doku adayı | Yalnız dönerken; parmakla sürtme/vuruş ayrı. Kaydın içinde üçüncü taraf müzik varsa o bölüm elenir. |
| Yemek arabası | Hafif metal temas + tekerlek için ayrıca kısa yuvarlanma kaydı | İki gerçek itiş ve ölçülen tekerlek hareketi. Metal darbe sesi tek başına yuvarlanma katmanını karşılamaz. |
| Top, yay, kurdele, çıngıraklı oyuncak | Mevcut `SFX_Cat_Toy_A/B`, kumaş ve `impactGeneric_light_000` | Gerçek başarılı pati teması; top/teker için yuvarlanma, yay için titreşim katmanı ayrıca seçilir. |
| Lavabo, duş, fıskiye | Owlish `tap-water-1/2` | Görünür su açıkken ve doğru odadayken. Bunlar musluk kayıtlarıdır; duş ve fıskiye için birebir son seçim değildir. Geçişler yumuşak olmalı. |
| Bahçe / yemlik kuşu | syncopika `birdchirping071414.wav` | Görünür/etkin kuş kaynağıyla seyrek, kısık. Kapalı odalara taşmaz. |
| İzleme / dekor | Çoğu durumda ayrıca efekt gerekmiyor | Dekora veya sessiz bakışa yapay bir ses zorlanmaz. |
| Runner | Yüzey teması, kısa kalkış/iniş, yumuşak engel darbesi, jeton için `pluck_001` / kesilmiş `handleCoins` | Mevcut jump/slide/coin/hit olaylarına bağlanır; arka arkaya jetonlarda ses sayısı sınırlanır. |
| Catch | Hafif pati teması, başarılı yakalamada kısa ödül, ıskada yalnız hareket sesi | Gerçek `TryConsumeStrike` ve başarılı hedef çözümünden sonra; düğmeye basınca başarı sesi çalmaz. Acı çığlığı kullanılmaz. |
| Alışveriş, görev, seviye, sonuç | `confirmation_001`, `error_001`, `handleCoins`; büyük başarı için Cozy Puzzle Clear Jingle | Doğrulanmış olaya tek ses; aynı ödülde bakiye + görev + seviye sesleri üst üste patlamamalı. Otomatik bütün-ekran dokunma sesi geri gelmez. |

Tam dosya yolları, SHA256, kaynak ve açıklamalar [selection.json](QA/AUDIO_AUDIT_2026-09-14/selection.json) içinde. Tablodaki seri isimlerinin seçkide 000 örneği var; Kenney arşivlerinde diğer varyantlar da mevcut. Bunlar rastgele seçilmiş nihai oyun havuzu olarak sunulmaz.

## Kaynak ve lisans kaydı

| Kaynak | Kullanım adayı | Bu turdaki durum |
|---|---|---|
| [Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds) | Yüzey ve hafif malzeme darbeleri | CC0; paket ve içindeki lisans indirildi. |
| [Kenney RPG Audio](https://kenney.nl/assets/rpg-audio) | Kumaş, kitap, gıcırtı, jeton | CC0; paket ve içindeki lisans indirildi. |
| [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) | Gerçek işlem onayı/hata, küçük ödül | CC0; paket ve içindeki lisans indirildi. |
| [OwlishMedia Sound Effects Pack](https://opengameart.org/node/86682) | Örtü, musluk, kâğıt, eşya, pikap | Üreticinin sayfasında CC0; sayfa ve özgün paket saklandı. |
| [Luckius Various Paper Sound Effects](https://opengameart.org/content/various-paper-sound-effects) | Rulo/kâğıt | CC0; WAV/MP3 içeren paket saklandı. |
| [syncopika Bird chirping sounds](https://opengameart.org/content/bird-chirping-sounds) | Kuş | CC0; özgün WAV indirildi. |
| [Peludo Water Splash and sand footsteps](https://opengameart.org/content/water-splash-and-sand-footsteps) | Kum dokusu için ham alternatif | CC0 beyanı; üretici ayrıca itch.io sayfasına atıf istiyor. Son oyunda kaynak kredisine eklenmeli. |
| [MintoDog Cozy Puzzle Title](https://opengameart.org/content/cozy-puzzle-title) | 95 BPM menü müziği alternatifi | CC0; özgün OGG indirildi. |
| [MintoDog Cozy Puzzle In-Game 1](https://opengameart.org/content/cozy-puzzle-in-game-1) | 118 BPM mini oyun müziği alternatifi | CC0; özgün OGG indirildi. |
| [MintoDog Cozy Puzzle Jingle & Result](https://opengameart.org/content/cozy-puzzle-jingle-result) | Büyük başarı / tur sonu | CC0; OGG paketi indirildi; yalnız iki jingle seçkiye alındı. |

Yeni indirilen 10 kaynak ücretsiz ve yayıncı sayfalarında CC0 olarak işaretli. Kaynak URL'leri, erişim tarihi, indirme URL'leri ve arşiv hashleri [source-manifest.json](QA/AUDIO_AUDIT_2026-09-14/source-manifest.json) içinde. Kaynak sayfalarının yerel HTML kopyaları kanıt içindir; oyuna alınmaz. Üç Kenney paketinin lisans metinleri ayrıca arşiv içinden çıkarıldı.

Freesound'daki gerçek tırmalama ve sıçrama seslerinin tekil sayfalarında CC0 doğrulandı, fakat orijinal indirme hesap girişi istiyor. Bu dosyalar **indirilmiş gibi sayılmadı**. Kedi kayıt paketinin listesi görüldü; diğer tekil kayıtların lisans/dosya doğrulaması edinim sırasında yapılmalı. Mevcut Polyperfect/Pet/üretilmiş bakım seslerinin kullanım hakkı bu turda yeniden denetlenmedi; hepsine CC0 denmez.

Müzik seçenekleri ayrı bir karşılaştırma grubudur. Önceki rahatsız edici kısa elektronik ev döngüsünün geri gelmesi önerilmiyor. Müzik seçilecekse ses efektlerini örtemeyen seviye, uzun yapı, ayrı müzik ayarı ve kesintisiz geçiş gerekir.

## Uygulamada korunacak teknik davranış

1. Ses, animasyonun başlangıcından tahmin edilen sabit bir saniyeye değil gerçek evre/temas olayına bağlanmalı. `CatJumpMotion` inişte bir kez `stage(2,1)` çağırıyor; yerleşik dönüş tamamlanan basışları, tırmalama sağ/sol vuruşları, kum `ObserveSolvedContact` temasını zaten sayıyor. Bu işaretler ses katmanına taşınabilir; onaylı hareket değişmez.
2. Meyve/kap saçılması kodla hareket ettiriliyor. Yalnız `OnCollisionEnter` eklemek bütün darbeleri yakalamaz; mevcut yere-varış ve sekme evreleri kullanılmalı. Reset/oda değişimi ses üretmemeli.
3. Birbirini kesmeyen sınırlı kaynak havuzu; aynı sesin arka arkaya aynen yinelenmesini önleyen 3–5 varyant; kısa bekleme aralığı ve malzeme/seviye ayrımı önerilir. Bu sayılar henüz ölçülmüş son miks ayarları değildir.
4. Sevilme, yemek, uyku, ortam ve mini oyun kaynakları mute, uygulama arka planı, duraklatma, iptal ve oda değişimini birlikte izlemeli. Mırlama tek sistemde sahiplenilmeli.
5. Son dosyalar yalnız seçilen kısa efektler ve gerekli döngülerden oluşmalı. Kaynak arşivlerinin 157.801.767 bayt toplamı oyun/APK boyutu değildir. 34 aday + 16 mevcut karşılaştırma dosyasının seçkideki toplamı 26.966.015 bayt; bunlar da son sıkıştırılmış oyun paketi ölçümü değildir.

## Önerilen uygulama sırası

Önce **pati/iniş + tırmalama + eşya darbesi + sevilme mute/mırlama düzeltmesi**. Bu paket, kullanıcının en görünür sessizliklerini giderir. Ardından kum/kâğıt/kumaş/su ve oyuncak alt türleri; sonra Runner/Catch olayları; en son ortam ve müzik dengesi.

Hâlâ özel kayıt seçimi gereken alt türler: hafif oyuncak/tekerlek yuvarlanması, yay/çıngırak titreşimi ve musluktan farklı duş/fıskiye dokusu. Seçki bu seslerin tümünü bitmiş olarak karşılıyor diye sunulmaz. İlk uygulama önce birkaç temsilci eşyada sesli denemeyle doğrulanmalı, ardından aynı ses davranışı diğer ilgili eşyalara genişletilmeli.

## Doğrulama ve kapanış

- Unity canlı salt-okunur sorgusu: 16 ithal AudioClip; Play ve derleme kapalı; üç normal sahne temiz; ev ses/müzik tercihleri 1/1. Bu bilgi 14 Eylül ses incelemesinde yeniden ölçüldü.
- Mevcut 16 dosyanın birinci taraf referans taraması `existing-audio-references.json` içinde. Bakım sesleri Resources üzerinden yüklendiği için GUID bağlantısı olmaması kullanılmadıkları anlamına gelmez.
- Dinleme sayfasındaki **50/50 dosya** tarayıcının ses çözücüsüyle açıldı, sıfır çözümleme hatası. Arama/kategori/boş sonuç durumu ve 390 px genişlikte yatay taşma denetlendi. Bu fiziksel ses çıkışı veya işitsel kalite testi değildir.
- Kanıtlar: `editor-initial.json`, `audio-metrics.json`, `gallery-verification.json`, `source-manifest.json`, `selection.json`, masaüstü/dar sayfa PNG'leri ve `integrity-final.json`.
- Başlangıç/son karşılaştırmasında seçili kod, editör kodu, sahne, ses, kaynak kedi varlığı ve ProjectSettings kapsamındaki **1.445 dosya aynı**; değişen/eklenen dosya 0. Hash taramasının tam kapsamı `integrity-before.json` içinde; bütün projenin her dosyası taranmış gibi sunulmaz.
- Oyun koduna, mevcut seslere, kaynak sıçramalara, modellere, sahnelere veya tercihlere değişiklik yapılmadı. Play açılmadı; gerçek oyun kaydı üzerinde işlem yapılmadı. Yeni oyun testi, APK, commit, push veya yayın yok. Ses inceleme çıktıları `Docs/QA` altında ve Git dışında; bu Markdown raporu Docs kökünde kalır.
