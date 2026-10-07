# Tırmalama yönü, gerçek pati erişimi ve geçiş — 2 Ekim 2026

## Kullanıcı kabulü — 2 Ekim 2026

Kullanıcı mevcut tırmalamayı kendisi test etti ve yeterli bulup kabul etti: “Tırmalama bence geçti soru yok.” Tırmalama işi kullanıcı kabulüyle kapandı. Aşağıdaki teknik test sonuçları ve joystick doğrulama sınırları tarihsel kanıt olarak korunur; başarısız otomatik testler geçmiş sayılmaz. Bu kabul kaydında oyun kodu değiştirilmedi, yeni test veya geliştirme turu başlatılmadı.

Kullanıcı tahtaya doğru yaklaşma ve buna uygun pati hareketini istedi. Bu turun başlangıcı 2026-10-02T05:46:28.616780+00:00; kapanış 2026-10-02T07:40:26.040093+00:00. Son kullanıcı sınırı 07:40:24 UTC / 10:40:24 İstanbul. APK izni yok.

Tahta merkezine en fazla 15° bakış, aynı gövde pozunda iki ayrı pati ve dört vuruş uç noktası doğrulanır. Temas gerçek ağırlıklı parmak derisinden hesaplanır. Bileğe yakın yama erişemiyorsa tutarlı gerçek parmak ucu seçilir; seçilen üç nokta tüm vuruşta aynıdır. Büyük patide hedefi aşmayı önlemek için çözüm 20 küçük adımda, 0,65 düzeltmeyle yapılır. Başlangıç dirsek düzlemi süreklidir; sıfır ağırlık kaynak pozu aynen korur. Gerçek öndeki deri yalnız yüzey normalinde düzeltilir.

Kök, kamera, direk, kemik uzunlukları ve arka destekler korunur. Denenen pelvis/gövde kaydırma kaldırıldı; CatPawReachMotion bu turun başlangıcıyla byte aynıdır. 0,8 saniyelik yükseliş/iniş ve dönüşümlü iki pati korunur. 10°/60 FPS eklem adımı, 40–165° vuruş dirseği, 5 mm deri ve 3 mm temas sınırları gevşetilmedi.

## Son seçili native sonuçlar

- ScratchReworkTests.JoystickApproach_ThreeSizesFaceBoardAndCompleteWithoutRootSnap: **Failed** — PlayMode-approach-3.xml
  Visible reachable stance   Expected: True   But was:  False
- ScratchReworkTests.JoystickImmediateRelease_HandoffRemainsContinuous: **Failed** — PlayMode-approach-3.xml
  Visible reachable stance   Expected: True   But was:  False
- ScratchReworkTests.Large: **Passed** — PlayMode-recorded-1.xml
- ScratchReworkTests.Medium: **Passed** — PlayMode-recorded-1.xml
- ScratchReworkTests.OtherToys: **Passed** — PlayMode-recorded-1.xml
- ScratchReworkTests.PauseCancelAndBlockedStarts: **Passed** — PlayMode-recorded-1.xml
- ScratchReworkTests.Small: **Passed** — PlayMode-recorded-1.xml
- ScratchReworkTests.RecordedStandingVideosPlayCompletely: **Passed** — PlayMode-approach-3.xml

Esas kanıt native-final-manifest.json içindeki son seçili sonuçlardır. Ara başarısız XMLler saklandı; eski başarısız yaklaşma sonuçları daha yeni koşularla birlikte selectedRuns içinde ayrı görülebilir. Son yaklaşma test düzenlemeleri normal üç boyut hareketini değiştirmedi; runtime üç kayıt ve regresyon koşusundan beri aynıdır.

## Ölçümler ve görüntü

- persian: 560 kare, 578200 deri noktası; en derin gerçek giriş 0.0000 mm; kök 0.000000 m; en büyük geçiş adımı 3.9590°; ters dirsek 0, açı aşımı 0.
- domestic-shorthair: 560 kare, 508060 deri noktası; en derin gerçek giriş 0.0000 mm; kök 0.000000 m; en büyük geçiş adımı 4.1064°; ters dirsek 0, açı aşımı 0.
- maine-coon: 560 kare, 609420 deri noktası; en derin gerçek giriş 0.0000 mm; kök 0.000000 m; en büyük geçiş adımı 6.5193°; ters dirsek 0, açı aşımı 0.

Üç ana video gerçek Game View 1920×1080, 60 FPS, ikişer ritimdir. Kareler baştan sona çözüldü; Unity VideoPlayer tam oynatım sonuçları manifestte ayrı kaydedilir. Bu, cihazdaki oyun FPS ölçümü değildir. Kare dizileri görsel olarak incelendi: orta kedide yandan iki pati hareketi okunur; küçük/büyük seçili arka açıda kedinin gövdesi patileri önemli ölçüde örter. Direğe göre görünürlük metriği kedinin kendi gövdesiyle örtüşmeyi ölçmez. Kullanıcı görsel kabulü yok; tam cihaz/bütün ırk/bütün konum kabulü değildir. Sayaçlar QA kayıt kopyasıdır.

## Yaklaşma ve sınırlar

**Gerçek joystick yaklaşması tam kabul edilmedi.** Hareket/temas düzeltmesi geçse de yaklaşma uçtan uca doğrulanmış sayılmaz.
Yaklaşma fixture araması yalnız fiziksel olarak açık yürüyüş başlangıcı seçer; gerçek joystick başladıktan sonra kök taşınmaz. İlk seçilen hazır duruşlara giden düz yolda gerçek direk MeshCollider gövde kontrolünü reddetti. İki kamera tarafı ve izinli bakış açıları tarandı; en son denemede gerçek yürüyüş hedef noktasında durdurulup normal eylem kabulü denetlendi. İptal, duraklatma, taze engel, ters/yan bakış reddi ve fare/tünel seçili native sonuçları yukarıdadır.

## Koruma ve kapanış

7934 başlangıç dosyası aynı. Değişenler: Assets/Scripts/Activities/CatPawReachResolver.cs, Assets/Scripts/Activities/CatToyContactMotion.cs, Assets/Scripts/Activities/ScratchPostActivity.cs, Assets/Tests/PlayMode/ScratchReworkTests.cs. Yeni/eksik: 0/0. Dört gerçek kayıt ve CP2 yedeği başlangıç hashleriyle korundu; tarihsel kayıt yüklenmedi. Tercihler 16/16 aynı. Bir eski Eat klibi başlangıçta okunamadı. Font önbellekleri ve EditorSettings yalnız bu turun başlangıcından geri alındı. Yeni recovery/test sahneleri QA içinde korundu.

İlk Unity koşusu önceden var olan native Access version hatasıyla takıldı. İlk yeniden başlatma otomatik onayca reddedildi; kullanıcı açıkça onayladıktan sonra yalnız doğrulanmış CatHome editörü yeniden açıldı (06:00:31 UTC, DX11). Bu uyarının kök nedeni çözülmüş sayılmaz. Son editör durumu editor-final.json içindedir. Unity açık; Play/QA/derleme/build/profiler kapalı, üç normal temiz sahne ve tek etkin ses dinleyicisi hedeflenip denetlendi. APK/commit/push/yayın yok. UI işi başlatılmadı. Süre sınırında duruldu.
