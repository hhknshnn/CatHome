# Step 1 — doğal zemin dönüşü, 1 Ekim 2026

Başlangıç: 10:37:26 UTC (13:37:26 Türkiye). Kesin kapanış ve süre `QA/NATURAL_TURN_2026-10-01/closure.json` içindedir. İki runtime düzenleme turu yapıldı; ardından eski testlerin animatör değişimini ve gerçek giriş karesini beklemesi düzeltildi. APK, commit, push veya yayın yapılmadı.

## Neden ve değişiklik

Computer Use ile mevcut Play Mode sağ/sol hareketleri önce kayıt kopyasında gözlendi. Unity üzerinden etkin rig, denetleyici ve klipler incelendi: mevcut normalize iskelet, Idle/Walk/Run, ayrı dönüş klibi yok; root motion kapalı.

Büyük yön farkında kök dönerken yürüyüş hızı sıfırdı; Animator Idle pozunda kalıyordu. Ayrıca öne kaydırılmış CharacterController merkezi, dönüşten sonraki Move çağrısında eski dünya merkezini koruyarak kökü yana çekiyordu. Tanı kaydı `steer-trace.txt` ilk başarısız ve son başarılı ölçümleri içerir.

- CatMovement dönüş hızı kısa ivmelenme/yavaşlamayla 420°/sn (koşuda 480°/sn) sınırını kullanır. Küçük yön farkında mevcut ileri yürüyüş sürer; ters komutta kısa pivot ardından yürüyüşe geçilir. Kabul edilen dönüşten sonra Physics.SyncTransforms kapsülü Move öncesi eşitler. Gövde engel kontrolleri ve toleransları korunur.
- Mevcut Walk klibinin ölçülmüş temposu, doğrusal hareket yokken de gerçek açısal hıza bağlanır. Yeni rig, yeni klip, Blender veya root motion eklenmedi.
- CatNaturalTurnMotion, kaynak yürüyüş pozundaki düşük patileri kısa süreli dünya temaslarında tutar; düzeltme 4 cm ile sınırlıdır, yükselen veya uzayan pati bırakılır. Ön gövdede en fazla 1,5° ağırlık eğimi vardır. Kök konumu değiştirilmez; sıralı dört pati veya tekrar eden küçük dönüş döngüsü kurulmadı.
- CatActivityFacing aynı tempo ve temas katmanını yer etkileşimlerinin kısa dönüşlerinde kullanır; etkileşim sahibi ve sonraki poz korunur. Çömelme/tünel, havadaki sıçrama ve eşya üzerindeki ayrı destek sistemi devralınmaz.

## Doğrulama

Esas kanıt: `QA/NATURAL_TURN_2026-10-01/native-final-manifest.json`. Yalnız bu manifestte seçilen nihai vakalar kabul kanıtıdır; ara XML dosyalarında tarihsel test-fixture başarısızlıkları vardır.

8 benzersiz native PlayMode kontrolü geçti:

- Domestic Shorthair ve Persian, 24/30/60 simülasyon kare hızında sağ/sol 45°, 90°, 179,9°: 36 senaryo. 45° 0,17–0,20 sn; 90° 0,29–0,30 sn; 180° yaklaşık 0,50–0,52 sn. Karelik ani sıçrama sınırı ve gövde açıklığı geçti.
- Yürürken 45° yön değişiminde kesintisiz ileri hareket; etkileşim dönüşünde kök yerinde, bitince hemen hareket.
- Gerçek top sepeti HUD başlangıcı, ortak dönüş yolu, üç gerçek pati yakalaması ve tamamlanma/kilit bırakma. 120 dönüş karesinde etkin yürüyüş pozu gözlendi.
- On ırkta dört ihtiyaç/hız durumu: gerçek mesafe ve animasyon döngüsü uyumu.
- Ters yönün ilk gerçek giriş karesi, duvarda adımlamayı durdurma ve fiziksel animasyon sahibini koruma.
- Köşe kaçışı ve ilk girişte yana itilmemesi; mevcut gerçek deri kontrolü.
- İki ırkta gerçek mama/su ve mutfak yemeği düğmeleri, temas ve sonuç koruması.
- Nihai sekiz saniyelik video kaydı.

Örneklenen kısa pati temaslarında aynı karede kaynak pozunun ankora hatası ile düzeltme sonrası hata karşılaştırıldı; toplam kalan oran yaklaşık %17,5. Bu sayı bütün video boyunca mutlak pati kayması veya fiziksel telefon ölçümü değildir. Kaynak yürüyüşün kaldırdığı patiler yeniden yerleşirken küçük kayma tamamen sıfırlanmış sayılmaz.

## Son video

`QA/NATURAL_TURN_2026-10-01/CatHome_Dogal_Donus.mp4`

1920×1080, H.264, 24 FPS, 192 kare, 7,999958 sn, sessiz. Nihai testlerden sonra gerçek Unity Game View'dan kaydedilen tek kesintisiz çekimdir: 90° sağ, 180° ters dönüş ve yürürken yön değiştirmeler. Hazırlık konumlandırması çekim dışında; kamera ve HUD kaynakları değiştirilmedi. Görünen seviye/ihtiyaçlar QA kopyasına aittir. MP4 tarayıcıda baştan sona oynadı; ayrıntı kareleri de incelendi. `iteration1-preview.mp4` eski tanı çekimidir, teslim videosu değildir.

## Koruma ve sınır

8.280 okunabilen başlangıç dosyasından 8.277 aynı; yalnız iki mevcut runtime dosyası ve bir mevcut test değişti. Bir runtime, bir test ve ikisinin metası eklendi. Eksik dosya yok. Bir özgün Eat animasyonu başlangıçta kabuktan okunamadı. Sahne, prefab, model, klip, ses, HUD, kamera, yerleşim ve ProjectSettings kaynakları aynı. İki font önbelleği ve EditorSettings yalnız bu turun hash doğrulanmış başlangıcından döndü.

Ana/recovery dahil beş kayıt/yedek dosyası ve 16 tercih aynı. Unity'nin persistentDataPath altında yazdığı TestResults.xml test çıktısıdır, oyuncu kaydı değildir. Play/QA/derleme kapalı; üç temiz normal sahne, tek etkin ses dinleyicisi, Unity açık. Test sahnesi geçişlerinde dinleyici bulunmama uyarıları vardır.

Dar mobilya yüzeylerindeki mevcut destekli dönüş sistemi bu zemin ve etkileşim öncesi dönüş düzeltmesinin dışında kaldı. Kaydedildi; yeni çalışma kendiliğinden başlamaz.
