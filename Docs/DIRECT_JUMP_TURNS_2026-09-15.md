# Koltuk, sehpa ve bütün odalarda doğrudan dönüş — 15 Eylül 2026

Kullanıcı berjerden sonra salon koltuğu ve sehpasında da pati vura vura, ileri geri yer değiştirerek dönüşü reddetti; ardından diğer odaların zıplamalı aktivitelerinin kontrolünü istedi. **Tek tek patiyle dönüş tercihi artık bu 39 zıplamalı aktivite için geçerli değildir.** Kedi sıçrama öncesinde ve basışın toparlanmasından sonra kısa, tek bir dönüş yapar. Yeni görünüm kullanıcının değerlendirmesine hazırdır; önceki hareket onayları bu değişikliğin görsel onayı sayılmaz.

## Oyun değişiklikleri

- `CatJumpMotion` ortak dönüşü sabit merkezde, açıya göre 0,18–0,50 saniye yapar. Tekrarlı pati çevrimi ve destek üzerindeki dışarı/geri yay kaldırıldı. Dönüşte yürüyüş/bakım klibi ilerlemez; duraklama önce kontrol edilir. İniş yönü gerçek iniş noktasıyla kalkıştan önce eşleşir ve uçuşta değişmez. Özgün Jump hazırlığı, uçuş örnekleri ve basış/toparlanma korunur.
- Salon koltuğunda tek sabit duruş, sırt ve kolçak açıklığı için 10 cm açık tarafa, 8 cm minderin içine alındı; uyuma/uyanma aynı desteği kullanır. Sehpanın nesneyi attıktan sonra kameraya dönüp yeniden geri dönmesi kaldırıldı. Pati teması ve tok `PropLand` sesi korunur.
- Balkon bitki rafı ve üst kat kitaplığı yakın, açık çapraz iniş kullanır; uzun geri dönüş önlenir. Kitaplık duruşu sabit 8 cm öne alındı. İniş adayları gerçek denetleyici açıklığı ve bağlantılı zemin yoluyla seçilir.
- Yüksek saksılar iniş yönüne önceden döner. Avlu saksısının açık çapraz inişi korunur; bitişte eski noktaya geri ışınlanma kaldırıldı. Engel bulunan bitiş ve iptal güvenli zemine döner.
- Fıskiyede içme duruşundan çıkış destekli kısa geçişle yapılır; alçak içme gövdesinin bir anda sıçrama duruşuna atlaması düzeltildi. Bahçe kuş banyosunda sabit destek 5,5 cm suya doğru alındı; arka pati eğimli kenarda kalmaz. Bu destek geçicidir, sahneye kaydedilmez ve eylem sonunda kaldırılır.

Zemindeki gerekli açık yaklaşma yolları korunur. Diğer aktivitelerin yerleşimleri, kaynak modeller, kedi boyu/kemik oranları ve ses varlıkları değiştirilmedi.

## Son doğrulama

Esas kaynak [son test manifesti](QA/DIRECT_JUMP_TURNS_2026-09-15/native-final-manifest.json): **20/20 PlayMode/native ve 1/1 EditMode**. Eski aday sonuçlar teslim sonucu değildir. `native-release.xml` içindeki eski kuş banyosu destek hatası, son `native-water-release.xml` 4/4 ile kapandı. Kaynak sıçrama kontrolü `native-edit-release.xml` içindedir.

- Sekiz odada **39/39 aktivite**: tek tamamlanma, açık iniş ve kontrol iadesi; tekrarlı pati dönüşü ve uçuş içinde kök yön değiştirmesi yok. Başlangıçta 31 rutinde pati dönüşü, 8 rutinde uçuş dönüşü vardı. [Envanter](QA/DIRECT_JUMP_TURNS_2026-09-15/final-39-inventory.json).
- Koltuk/sehpa: 10 ırk × 15/30/60 FPS = 60 çevrim; berjer 30 çevrim. Kaynak iskelet ve basış ayrıca 30 ırk/FPS çevriminde kontrol edildi. Duraklatma/iptal/kontrol iadesi geçti.
- Dar raflarda 20 gövde, çadır/kitaplıkta 20 eklem, üç dar yüzeyde 30 dinlenme pati kontrolü; saksılarda 30 gövde ve 30 sıçrama çevrimi. Su noktalarında 40 ağız/destek çevrimi, fıskiye ve kuş banyosunda ayrı ayrı 10 çıkış sürekliliği çevrimi geçti.
- Sekiz odanın **12 gerçek oyun düğmesi 12/12**: berjer, koltuk, sehpa, havlu rafı, meyve sepeti, çadır, çiçek saksısı, bitki rafı, asılı koltuk, avlu saksısı, fıskiye, üst kat kitaplığı. Düğmenin `onClick` akışı kullanıldı; dinlenme gerçek Kalk düğmesiyle sonlandırıldı. Her birinde tek tamamlanma/açık çıkış/kontrol iadesi var. [Düğme manifesti](QA/DIRECT_JUMP_TURNS_2026-09-15/manual-final-manifest.json).
- Seviye doğrulayıcısı **0 hata / 0 uyarı**. Editör ön izlemesinin geçici kamera görüş açısı temizlenerek yazılmış sahne doğrulanır. Önceki kamera uyarısı sahne varlığındaki değişiklik değildi.

Koltukta gezinmeyi engelleyen kutu, minderin üzerindeki boşluğu da doldurur; gövde testi bunu görünür geometri sanmamalıdır. Koltuk için gerçek deri örnekleri gerçek model üçgenleriyle karşılaştırıldı; canlı gezinme kutusu değişmedi. İç/dış örnekleriyle üçgen yönü kontrol edildi. Diğer duvar ve engel kontrolleri yerinde kaldı.

Statik dinlenme pati testleri dönüşün her karesinde dört tabanın aynı noktada kilitli olduğunu iddia etmez. Bütün 39 aktivitenin on ırkla tam matrisi ve fiziksel telefon performansı ölçülmedi. Galeri gerçek Unity PNG'leridir; video üretilmedi. [Galeri](QA/DIRECT_JUMP_TURNS_2026-09-15/index.html).

## Deneme kapanışı ve kayıt koruması

İlk eski test oturumunda düğme hazırlığı/başlatması başarısızdı. Temiz kopyada aynı gerçek düğmeler geçti; son ön izlemede dönüş penceresini kapatır kapatmaz aynı karede düğme seçmeye çalışmak da hazırlık uyarısı üretti. Pencerenin giriş kilidi bırakıldıktan sonra gerçek sehpa düğmesi tekrar geçti. Oyun düğmesi kodu değiştirilmedi. Başarısız hazırlıklar son sonuç değildir.

Son bütünlük kontrolünde hızlı Play kapanışının canlı kayıt referanslarını tutabildiği görüldü: QA dizin anahtarı kaldırıldıktan sonraki geç kayıt çağrıları gerçek ana/recovery dosyalarına yazmıştı. Aradaki otomatik dosyalar `save-repair/automatic-save*` olarak korundu. Başlangıç envanterindeki SHA256 ile birebir eşleşen özgün baytlar `save-repair/baseline-exact.json` üzerinden geri getirildi; tarihsel checkpoint değerinden tahmin edilen kayıt yüklenmedi.

`UiQaTestSession.End`, dizin ayrımını kaldırmadan önce `CatHomeSaveSystem.EditorEndCopiedSession` ile canlı kayıt referanslarını bırakır. Bu yardımcı yalnız editörde derlenir. Düzeltme sonrasında gerçek sehpa düğmesiyle yeni bir aç/kapat çevrimi ve kapalı oturumda zorlanmış `SaveNow(true)` / `SaveForSuspension` çağrıları doğrulandı; gerçek dosyalar aynı kaldı. Kanıt `closed-save-regression.json`, `editor-closed.json`, `preferences-verified.json`.

Son envanter: **67/67 sahne, 396/396 FBX, 320/320 prefab, 144/144 WAV ve 3/3 gerçek kayıt başlangıçla aynı**. Beş hareket dosyası ve yalnız editör kapanış koruması eklenen kayıt sınıfı değişti. Ana/recovery SHA256: `72EBFF5B1C95A86931060F7B54A1B7612884D3209CFC2604CBC8877017DB9873`; CP2: `03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D`. Ayrıntı [bütünlük raporunda](QA/DIRECT_JUMP_TURNS_2026-09-15/preservation-summary.json).

16 tercih ve editör sessizliği geri yüklenir. Son kullanıcı denemesi ayrı QA kopyasında salonda, sehpa düğmesiyle açık bırakılır; kesin canlı durum `editor-final.json` içindedir. Kullanıcı bitirmeden kapatılmaz. **Tools → Cat Home → Tüm Sesler Denemesi → Bitir** veya Play durdurma denemeyi bitirir. Unity/Blender/PC kapatma, APK, video, teslim arşivi, commit, push veya yayın yapılmadı. QA Git dışında tutulur. Eski salon tablo yakın giriş konusu bu kapsamda değiştirilmedi.
