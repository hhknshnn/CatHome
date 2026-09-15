# Doğal dönüş ve sehpa düşüş sesi — 15 Eylül 2026

Kullanıcı genel sesleri onayladı. Bu tur, salondaki sehpa dönüşünde belirginleşen küçük ve eşit pati adımlarını azaltır; aynı ortak hareketi kullanan diğer eşyaları kontrol eder. Sehpadan düşen nesneye tok bir yere vurma sesi ekler. Yeni dönüşün ve bu tek sesin kullanıcı değerlendirmesi henüz alınmadı.

## Dönüş

`CatSurfaceTurnMotion` geniş yüzeyde 18° yerine en fazla 30° çevrim kullanır. Sehpanın yaklaşık 87° dönüşü 20 yerine 12 pati adımıdır: 24 FPS ölçümünde 2,6667 yerine 1,7083 sn; on ırkın 15/30/60 FPS denemelerinde 1,67–1,70 sn ve 12 adım. Ön pati yönü açarken arka pati daha kısa sürede takip eder; 155/95 ms. Pati yayı yaklaşık 3–12 mm ile alçak kalır. Baş en fazla 9° öncülük eder. Gövdenin orta bölümü düzenli döner, yalnız giriş/çıkış yumuşatılır.

Açık yüzeylerde arka pati hedefleri iki yana 12'şer mm açılır; takip eden pati için yer kalır. Çadır, koltuk, asılı koltuk ve dar bitki rafı mevcut açıklık yolunu/18° çevrimini korur; alçak pati yayları ve %20 daha kısa süre kullanır. Dar yerlerde kaynak duruşun evresi süreye göre korunur. Destek kökü açıklık hareketiyle birlikte ilerlemeye devam eder.

Kaynak sıçrama, havada yön tutma, bacak bükülme düzlemi, kemik boyları, üç basılı patinin korunması, arka pati sırası ve çömelme sınırları aynı. `CatJumpMotion` ve `CatActivityAnimation` değişmedi. Mevcut test toleransları gevşetilmedi.

İlk 30° denemesi her yüzeye uygulanınca altı rutinde pati aralığı/erişim veya geçiş sorunu görüldü. Bu ara sürüm teslim değildir. Yüzeye göre ayrım ve arka pati açıklığı son testlerde doğrulandı.

## Düşüş sesi

`PropLand_1/2/3`: 0,22 sn, 48 kHz, mono; kısa ve kuru tok vuruş. Yerel üretim kaynağı `ArtSource/Audio/GameAudio/generate_audio.py` içindeki `prop_land(take)`; bağımsız sabit rastgele tohum kullanır, diğer seslerin üretim sırasını değiştirmez. Üç yeni WAV dışında önceki müzik ve efektler aynıdır. İki eski gerçek CC0 bakım kaydı aynen korunur.

`LivingFurnitureActivity.PushToy` ilk düşüşün çizilmiş iniş noktasına ulaştığı karede `PropLand` çalar; eski hafif `BallTap` iniş olayı kaldırıldı. Patiyle ilk dokunma ve sehpa üzerinde yuvarlanma aynı. Test, mevcut `toyLanding` konumu ile ses karesini karşılaştırır; yeni Rigidbody/çarpışma sistemi değildir. Reset fazladan ses üretmez. Duraklatmanın verildiği karede eski delta ile bir kez daha ilerleme engellendi; yuvarlanma, düşüş ve sekme bekler, iptal sessizce eski yerine döndürür.

`AudioCue` sonuna bir aile eklendi; önceki kimlikler, 12 kaynak sınırı ve ses/müzik seviyeleri aynı. Banka 122 efekt + 5 müzik = 127 dosyadır. Üç yeni sesin tepe seviyesi −2,853 dBFS, kırpılan örnek sayısı sıfırdır.

## Son doğrulama

Esas [native manifesti](QA/NATURAL_TURN_2026-09-15/native-final-manifest.json): **12/12 benzersiz kontrolün son sonucu başarılı**.

- `native-turn-release.xml`: 7/7. Sekiz odada 39 sıçramalı rutin; sehpa için 10 ırk × 15/30/60 FPS; üç dar eşyada 30 çevrim; çadır/kitaplıkta 20 anatomi çevrimi; 30 ırk/FPS kaynak sıçrama kontrolü; dönüşte duraklatma/iptal.
- `native-body-bank-paws.xml`: gövde/kol açıklığında 80 çevrim, çadır/raf gerçek pati desteğinde 20 çevrim ve 127 sesin açılması başarılı. Bu dosyanın iki ara ses testi başarısızdır; aşağıdaki son sonuçlarla yer değiştirir.
- `native-impact-release.xml`: 2/2. 15/30/60 FPS'de her düşüşte tek ses, üç varyasyon, iniş konumuyla aynı kare; duraklatma, iptal, tekrar ve ses kapalı kullanım. İlk ses fixture'ında dönüş karşılama penceresi gerçek düğmeyle kapatıldı. Diğer ilk hata, yukarıdaki bir karelik duraklama hareketiydi; runtime'da giderildi.

Proje doğrulayıcısı: **0 hata / 0 uyarı**. Tüm eşya × on ırk matrisi veya fiziksel telefon/kulak değerlendirmesi değildir. Fotoğraflar gerçek Unity kareleridir; video üretilmedi. [Yerel inceleme sayfası](QA/NATURAL_TURN_2026-09-15/index.html).

## Korunanlar ve devam

615 başlangıç dosyasından 613'ü aynı; yalnız bu kümedeki dönüş ve sehpa davranışı değişti. Ayrıca `GameAudio` enum'una tek kimlik eklendi. 71 sahne dosyası (14 oyun sahnesi dahil), 396 FBX ve 141 başlangıç WAV'ı aynı. Yeni sesler ayrı üç dosyadır. 127 banka kaydı dosya hash'leriyle doğrulandı.

Üç gerçek kayıt değişmedi; ana/recovery `CAE8CA6F1C2B9FFEADC26B7D061FB432CA4459B24A6B3055A12830F414B00B44`, CP2 `03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D`. 16/16 tercih ve editör sessizliği testler sonunda geri yüklendi. EditorSettings başlangıç byte hash'i `307A63900424388D1A972749E679729D67DD7A661EF75AD6929D539E0127AE56` ile aynı; yalnız testin geçici Play seçenekleri geri alındı. Tarihsel kayıt kullanılmadı.

Kullanıcı denemesi ayrı `Library/UiQaSession/20260915-105515` kopyasında. Son durum `editor-final.json` ve gerçek sehpa düğmesi kontrolü `manual-coffee/report.json` dosyalarında tutulur. GameScene + CatHome_UI + LivingRoom_Level01 normal sahneleridir; test Init sahnesi bırakılmaz. Kullanıcı bitirmeden Play kapatılmaz. **Tools > Cat Home > Tüm Sesler Denemesi > Bitir** veya Play durdurma 16 tercihi geri getirir. Unity odak dışındayken sesin susması mevcut davranıştır.

Eski salon `room.modern-painting` yakın giriş konusu kapsam dışında. APK, arşiv, commit, push, yayın veya uygulama/PC kapatma yapılmadı. Plan/süre, tek tek doğrulama ve QA'nın Git dışında kalması kuralları sürer.
