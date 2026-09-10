# Cat Home — sakin hareket, bakım sunumu ve oyun geçişleri, 9 Eylül 2026

**Tamamlandı:** 499/499 EditMode, 26 benzersiz native testin son sonucu başarılı, validator 0 hata / 0 uyarı. [Yeni galeri](QA/CARE_MOTION_2026-09-09/index.html): 69 gerçek Unity ekranı ve yedi video. Kullanıcının onayıyla güncel koleksiyon gerçek yerel kayıt ve recovery dosyasına uygulandı; salonda beş CAT, depoda 12 CAT var.

Bu geçiş, ev kedisinin ihtiyaçları düşükken koşu ayaklarının yer hızına yetişmemesi, bakım efektlerinin sunumu, kedi komutları panelinin düzeni, bahçedeki sert hareketler ve mini oyunlardan geri dönüş akışını ele alır. Önceki [eylem ve geçiş güvenliği](ACTION_STATE_AUDIT_2026-09-09.md) kuralları korunur. Önceki görsel galeri [Playful Interactions](QA/PLAYFUL_INTERACTIONS_2026-09-08/index.html) tarihsel temeldir.

## Evde mesafeyle uyumlu yürüyüş ve koşu

`CatMovement`, yürüyüş/koşu görünümünü joystick'in koşu niyetinden değil, CharacterController'ın gerçekten aldığı yatay mesafeden seçer. İhtiyaç yavaşlamasının ardından hızı zorla .65 m/sn'ye yükselten alt sınır kaldırılmıştır; .65 m/sn normal ihtiyaçlarla analog yürüyüşün başlangıç hızıdır. Ev koşusunun üst sınırı **1.5 m/sn** olmuştur. Sekiz sahnede kayıtlı eski 2.2 değeri kaynakta sınırlandığından bu düzeltme için oda sahnelerini yeniden üretmek gerekmez.

`CatHomeLocomotionBuilder.Build()` on ırkı `CatBreedVisualFactory` ile gerçek oyun bağlarına ve ölçeğine getirir. Walk ve Run kliplerinin dört pati kemiği 240 Hz örneklenir; yere yakın, geriye doğru basış evresinin medyan hızından çevrim başına mesafe hesaplanır. Sonuç `Assets/Resources/Home/CatHomeLocomotionCatalog.asset` içinde tutulur. Klip, ırk iskeleti veya görsel ölçeği değişirse ölçüm yeniden üretilmelidir.

Mevcut .5 oyuncu kök ölçeğinde ölçülen değerler:

| Klip | Çevrim süresi | Çevrim başına mesafe | 1× klip hızının mesafe karşılığı |
| --- | ---: | ---: | ---: |
| Walk | .866667 sn | .445156 m | .513642 m/sn |
| Run | .366667 sn | .757295 m | 2.065349 m/sn |

Oynatma oranı gerçek hız × klip süresi / ölçülen çevrim mesafesiyle hesaplanır; eski .8 alt tempo sınırı yoktur. Yürüyüş/koşu karışımı 1.05–1.4 m/sn arasında gerçek hızla değişir. Duruşta Speed aynı karede sıfırlanır; bağımsız damping kuyruğu duvar önünde ayak hareketi sürdürmez. Girdi olmayan kontrolcü yerleşmesi yürüme sayılmaz. Ters yönde önce dönüş yapılır; sınırlı 220/320°/sn dönüş seçimi de gerçek yürüyüş/koşuya bağlıdır.

Ölçüm, bildirilen hatayı açıklamaktadır: eski açlık çarpanıyla .99 m/sn giden kedi, eski en düşük koşu temposunda yaklaşık 1.65 m/sn basış karşılığı üretiyordu. Bu, medyan basış hızına göre yaklaşık %67 farktır. Kaynak Run klibinin basış evresi kendi içinde değişken olduğundan medyan kalıntısı yaklaşık %50'dir; ortalama çevrim/mesafe eşleşmesi tek başına her karede kusursuz sabit pati teması kanıtı sayılmaz. Üç ihtiyaç seviyesinin gerçek 24 fps kareleri ve normal hızda kodlanmış videoları galeridedir; bu çalışma sıfır pati kayması iddiası taşımaz. [Ham adım ölçümü](QA/HOME_LOCOMOTION_2026-09-09/stride-measurements.csv).

Ev hareketi `Animator.speed` veya bakım/komutun etkin durumunu değiştirmez. Uyku, komut, mobilya ve mini oyun animasyon sahipliği korunur. Runner/Catch'in ayrı hareket temposu bu kalibrasyonu kullanmaz.

## Kedi komutları paneli

`CatCompanionPanel` daha geniş başlık, ayrı sekme satırı ve üç eşit komut kartı kullanır. Her kartta ırka ait poz fotoğrafı, kısa açıklama ve ayrı eylem düğmesi vardır. Durum metni kartların altında, ihtiyaçlar veya dinlenmeyi bitirme düğmesi ayrı alt satırdadır. Kart bütünü güvenli ekran alanının genişlik ve yüksekliğine göre ölçeklenir.

Kedi komutları ve oyun rehberi kendi içerik/kontrol alanlarını kullanır. Rehberin kaydırılabilir metni ve geri/devam düğmeleri korunur. Meşguliyet ve modal sahipliği önceki güvenlik kapısını kullanmaya devam eder. Otur/Loaf'ın gerçek tutulan dinlenme evresindeki enerji sözleşmesi değişmez. 1920×1080 ve 1440×1080 oranlarında gerçek fotoğraf/düğme ayrımı, görünür düğmenin kendi raycast hedefini alması, rehber kaydırma ve kapanıştaki giriş serbestliği doğrulandı. Ek olarak `CollectionCompleteCelebrationView.CanPresent`, komutlar veya rehber açıkken bekleyen koleksiyon kutlamasının araya girmesini önler.

## Uyku ve sevme efektleri

`CatSleepZzzEffect`, eski yazı efektinin yerine ay/yıldız içeren mor tonlu bir uyku madalyonu gösterir. `PetHeartEffect`, yumuşak renk geçişli ve küçük parlak ayrıntılı kalpleri kısa yükselme/solma hareketiyle üretir. Şekiller `CatCareFxGraphic` tarafından vektör olarak çizilir; yaş/solma sırasında geometrinin her karede yeniden üretilmesi gerekmez.

İki efekt, kedinin oda sahnesine ait tek `CatCareFxCanvas` kullanır: en fazla dört kalp ve bir uyku işareti. 1920×1080 referanslı ekran ölçeği, gerçek oda kamerasından baş konumuna bağlanır; ırk değişiminde mevcut baş yeniden bulunur. Dekor giriş almaz; GraphicRaycaster yoktur ve raycast engelleme kapalıdır. Gizli kedi, ekran dışı baş, farklı oda kamerası veya mini oyun görünürlüğü efektleri bastırır; oda kapanınca overlay bırakılır.

Pause süreyi/hareketi dondurur. Uyanma işareti hemen kaldırır; sevme bittiğinde mevcut kalpler kısa solmalarını tamamlar. Azaltılmış hareket veya düşük bellekli mobil profilinde tek sabit kalp ve salınmayan uyku işareti kullanılır. Bu kaynak sınırları fiziksel cihaz performans ölçümü yerine geçmez.

## Bahçede daha sakin etkileşim

Papatya yatağındaki `MatKneadActivity`, sert pati vurma klibi yerine `CatGentleKneadMotion` kullanır. Ön patiler yaklaşık 1.05 saniyelik sırayla, en fazla .024 m kalkar; en az bir pati yerde kalır. Giriş ve çıkış .35 saniyelik yumuşak zarfla yapılır. Gerçek pati hedefleri `CatToyContactMotion` üzerinden izlenir; kedi kökü zıplatılmaz, döndürülmez veya ezilmez. İptal, temas sahipliğini ve kalan hareketi temizler.

Bahçe ızgarasını izleme tepkisi `SitLookReaction.Sit` olarak düzenlenmiştir: kedi oturup bakar, ızgaraya pati atmaz. İlgili üretici ile eski sahne/prefab override'ını düzelten `ComfortPresentationBuilder` aynı tepkiyi üretir. Ürün ölçüsü, yerleşim ve ödül sözleşmesi bu sunum değişikliğiyle değiştirilmez.

## Mini oyunlar ve skor tablosu dönüşleri

Runner ve Catch başlangıç/duraklatma ekranlarında **Eve dön** ve **Oyunlara dön** ayrı seçeneklerdir. Sonuç ekranındaki eylemler ev, oyunlar ve tekrar başlat olarak ayrı hedeflere ayrılır. `MiniGameNavigationBuilder`, aynı satırı tekrar uyguladığında genişliklerin küçülmemesini ve düğmelerin kartın içinde kalmasını korur; oyun dünyasını veya sanatını yeniden üretmez.

Dönüş hedefi `CatRunnerSessionContext` üzerinden taşınır. Aynı çıkışta sıraya giren eski ikinci çağrı ilk seçimi değiştiremez; can ve sonuç ödülü iki kez işlenmez. Oyunlar paneline dönüşte mini oyun kapanır, aynı eve dönülür ve panel açılır. Skor tablosunun tamamını düğme yapan arka plan kaldırılır; satıra dokunmak, filtrelemek veya kaydırmak paneli kapatmaz. Kapat düğmesi oyunlar paneline döner; yinelenen kapatma paneli tekrar gizlemez.

## Gerçek kayda uygulanan koleksiyon

Kullanıcı tüm güncel ürünlerin gerçek koleksiyonunda açılmasını istemiştir. `CurrentCollectionGrantPreparation` mevcut kayda **80 ROOM + 17 CAT** güncel ürününü ve sekiz odanın erişimi için gereken **7 HOME anahtarını** ekleyen saf dönüşümü hazırlar. Bu geçişteki manifest **39 yeni sahiplik** gösterir. Eski veya bilinmeyen sahiplikler silinmez; güncel koleksiyon dışındaki tarihsel ROOM dekorları yeni açılmaz.

80 güncel ROOM ürününün depo işareti kaldırılır. Yeni CAT ürünleri depoya alınır. Hazırlanan bu kayıtta mevcut beş görünür CAT aynı kalmıştır: `home.ball-basket`, `home.scratch-post`, `cat.play-tunnel`, `cat.bell-collar`, `cat.nap-pillow`. Beş eşya/bir yatak sınırı korunur. Geçersiz eski bir görünür kümede kanonik politika sırası kullanılarak fazlalar depoya alınır; sahiplik ve kayıtlı yerleşimler silinmez.

Hazırlık yalnız `homeStore.ownedProductIds` ve `homeStore.storedProductIds` alanlarını değiştirir. Para, elmas, can, skor, ihtiyaçlar, ırk, mevcut oda, kedi pozu, tercihler ve `lastSaveUtc` korunur; JSON'daki bilinmeyen üst/alt alanlar da düşürülmez. Gerçek kayda yazma metodu helper içinde yoktur. Kaynak hash'i doğrulanan çalışma alanı yedeğinden hazırlanmış JSON ve manifest üretilir. [Hazırlanmış işlem manifesti](QA/CARE_MOTION_2026-09-09/collection-grant/prepared-current-collection-manifest.json).

Bu geçişin gerçek kayıt/recovery başlangıcı **CC6E20B5875DD93F6738D1A0CF6ED9B1A845E5E27647EC80671C75B66F16F27F**; CP2 yedeği **03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D**. Önceki geçişin kayıt hash'i geri yüklenmez. QA bittikten sonra iki gerçek dosya yeniden başlangıç hash’iyle karşılaştırıldı ve uygulama öncesi ayrıca yedeklendi. Hazırlanan aynı baytlar ana kayıt ve recovery dosyasına atomik dosya değiştirme ile uygulandı. Son hash ikisinde de **0A0F77B6412260040B9A03EDFB45E3DB87F0AB742E7FC2228EB1EE0C235D3438**. CP2 yedeği değişmedi. Uygulama sonrası JSON karşılaştırması yalnız iki izinli koleksiyon alanının değiştiğini doğruladı. [Uygulama sonucu](QA/CARE_MOTION_2026-09-09/collection-grant/application-result.json).

Bu işlem bilgisayardaki yerel kayıt ve recovery dosyasına uygulandı; bulut eşitlemesi veya bulut yayını çalıştırılmadı. Mevcut bulut çatışma çözümü iki kopyanın zamanını karşılaştırır ve koleksiyonları birleştirmez; gelecekte başka bir bulut kaydının tercih edildiği durum bu yerel doğrulamanın kapsamı değildir. `lastSaveUtc`, çevrimdışı ihtiyaç hesabı için aynen korundu.

## Son doğrulama ve teslim

- Son tam EditMode **499/499**, [son XML](QA/CARE_MOTION_2026-09-09/EditMode-final.xml).
- **26 benzersiz native testin son sonucu başarılı.** İlk gruptaki 25 başarı ve komut panelinin son başarılı tekrarı test kimliğiyle birleştirildi. [Birleşik sonuç](QA/CARE_MOTION_2026-09-09/native-test-summary.json). Başarısız ara XML dosyaları başarı sayılmadı.
- [Validator](QA/CARE_MOTION_2026-09-09/validator-final.txt): **0 hata / 0 uyarı**.
- On ırkta klip ölçümü/Animator çevrimi, dört ihtiyaç durumu, duvar/duruş/ters yön ve uyku sahipliği; bakım efektlerinin pause/reduced motion/ırk değişimi/oda kapanışı; papatya ve mangal; liderlik tablosu ve iki oyunun 12 çıkış yolu; önceki bakım ve mini oyun güvenlik testleri doğrulandı.
- Gerçek görsel tur **62 ekran** üretti: sekiz oda, komutlar/rehber/dinlenme, liderlik tablosu, iki oyunun başlangıç/duraklatma/sonuç ve dönüşleri. İki ekran oranındaki ölçümlerde taşan, üst üste binen veya başka hedefin altında kalan düğme kaydı yok.
- Yedi ek hareket karesi ve **1920×1080, gerçek 24 fps / toplam 34 saniye** video: üç hareket örneği (2+2+2 sn), uyku (7 sn), sevme (5 sn), papatya (8 sn), mangal (8 sn). Bahçe hareket kanıtında yalnız joystick geçici gizlendi; normal oda fotoğrafları gerçek HUD ile çekildi. Blender kareleri hızlandırmadan kodladı. Görsel tur ile son hareket turunda salonun kayıtlı CAT konumları farklı QA başlangıçları kullanır; gerçek kaydın konum alanları değiştirilmedi.
- QA/Play kapalı, **16 tercih tam geri yüklendi**. Üç normal sahne temiz; tek dünya kamerası ve ses dinleyici. Unity açık ve gerçek kaydı yalnız okuyan güncel editör ön izlemesi yenilendi.

Yeni APK/Android paket derlemesi, commit/push veya canlı Cloud Code yayını yapılmadı. Bilgisayar açık. Fiziksel tablet FPS/GPU/RAM/ısı ölçümü yapılmadı; kayıt hızı cihaz performansı değildir.
