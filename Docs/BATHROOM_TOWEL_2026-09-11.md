# Banyo — havlu rafı sıçraması / adım 5, 11 Eylül 2026

Kullanıcı adım5'i 25 dakika hedefiyle onayladı. Kapsam yalnız havlu rafına çıkış ve iniş. Önceki ortak ToyPounce/Jump iki yönde de kullanılıyordu; rafın gerçek üst yüzeyi Y1.57248 m. Genel yolun ardından raf üstünde ayrı dönüş ve uyku pozuna geçiş vardı.

TowelNestActivity artık kaynak modelin özgün JumpUp / JumpDown kliplerini ayrı evrelerle kullanır: çömelme, yükselme, rafa iniş, oturup yatma, dinlenme, uyanıp ayağa kalkma, aşağı sıçrama ve yere yumuşak iniş. Yükselişte dolabın ön yüzünden uzak kalarak yükselir; havada rafın uzun eksenine döner. İnişte arka patiler üst yüzeyin altına düşmeden dolaptan uzaklaşır. Uçuş gerçek raf yüksekliğinden hesaplanır; yukarıda .20 m, aşağı atlayışta .11 m tepe payı, 11 m/sn² düşey ivme kullanılır.

Geçici destek klip boyunca gerçek gövdeyi ortalar; başlangıç ve son yer temasında yatay destek yumuşak bağlanır/çözülür. CatActivityAnimation'ın varsayılan destek davranışı değişmez; ek karışım yalnız bu rutinden istenir. Kök ölçeğini değiştiren eski, son görünüme zaten yansımayan nefes/ezme yazımları kaldırıldı. Özgün kemikler ve model ölçeği korunur. Uyku/Kalk düğmesi, sürekli dinlenme ve enerji kuralları sürer.

TowelJumpAnimationBuilder mevcut ithal kliplere dört ayrı durum bağlar; yeni klip kopyası veya yeni model üretilmez. İlk denemedeki geçici örneklenmiş klipler kaldırıldı. PolyperfectCatIntegrationBuilder yeniden üretimde bu bağları kurar. Oturarak uykuya geçiş Sitting_to_Sleep kaynağıdır; kalkış aynı kaynakta ters zaman örneklemesi ve mevcut StandUp pozu kullanır.

Kanıt kökü Docs/QA/BATHROOM_TOWEL_2026-09-11. Güncel dosya towel-final.xml; towel-motion.xml ilk ara sonuçtur. Native kapsam: on ırkta kaynak yukarı/aşağı sıçrama, dört gerçek pati için dolap içinden geçmeme, raf/zemin inişi, görünür dinlenme ekseni, tek tamamlanma ve açık çıkış; beş evrede duraklatma ve başka giriş sahibini koruyan iptal; önceki iki havlu dinlenme testi; dar destek/breed değişimi/duraklatma testi. Tam EditMode paketi ve telefon performansı yeniden ölçülmedi.

Gerçek kayıtlar ve sekiz oda sahnesi preservation.json ile denetlenir. Havlu dolabının modeli, yerleşimi, diğer eşyalar ve adım3/4 davranışları değişmez. Unity ayrı QA kopyasında havlu rafı düğmesiyle kullanıcıya hazır bırakılır; editor-ready.json son durumu gösterir. Tek gerçek görsel, video/APK/arşiv/commit/push/yayın/kapatma yok. Adım6 kullanıcı onayı bekler.

Son doğrulama: **5/5 native**, **validator 0 hata / 0 uyarı** (`test-summary.json`, `validator.txt`).

Son gerçek oyun düğmesi denemesi **8.875 saniye** (2 sn deneme dinlenmesi dahil), **1 tamamlanma**, açık çıkış ve bırakılmış kontrol; hareketsiz Walk **0 sn**. Son rapor `Docs/QA/ROOM_INTERACTIONS_2026-09-11/towel-step5-final/report.json`. Önceki 5.29 sn rutin yeni oturup yatma/uyanma geçişlerini içermiyordu. Kullanıcı kendi dinlenmesini Kalk düğmesiyle bitirir. Son durumda **1 kedi / 1 görünür kedi derisi / 0 editör ön izleme kopyası**; normal hız, video çekimi kapalı.

[Rafa iniş anı](QA/BATHROOM_TOWEL_2026-09-11/towel-landing.png) tek teslim görselidir. Diğer PNG'ler yerel görsel inceleme içindir; son sürüm `after-` önekli görüntülerdir.
