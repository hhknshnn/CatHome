# Cat Home — kedi sesleri, menü geçişleri ve müzik dengesi

15 Eylül 2026. Kullanıcı mama/su/yürüme/koşma seslerinin daha doğal olmasını, ana menü müziğindeki kısa kesilmelerin giderilmesini ve bakım/uykuda müziğin azalmasını istedi. Bu kapsam tamamlandı; Unity ayrı kayıt kopyasında dinlemeye hazır.

## Kedi sesleri

- Mama: kum hışırtısı yerine gerçek kuru mama çiğneyen kedi kaydı, 14,4 sn döngü. Kaynak: [indieground — cat eating.mp3](https://freesound.org/people/indieground/sounds/238297/).
- Su: tonlu damlalar yerine gerçek dil/sıvı temasından kısa yalama sesleri, 6,1 sn döngü. Kaynak: [16GPanskaZlochova_Eliska — 4_Cat, drinking milk.wav](https://freesound.org/people/16GPanskaZlochova_Eliska/sounds/496277/). Kayıtta sıvı süt; oyunda yalnız lapping sesi kullanılır.
- İki kayıt kaynak sahiplerince **CC0 1.0** lisansıyla sunuluyor. Ücretli servis veya yeni hesap yok. Halka açık yüksek kaliteli MP3 ön izlemeleri mono 48 kHz / 16 bit WAV'a işlendi; doğal zamanlama/perde korundu, ortam uğultusu ve hışırtısı azaltıldı, yüksek darbeler yumuşatıldı. Kaynak/lisans sayfaları, URL ve hashler `ArtSource/Audio/CatCare20260915/` içinde.
- Dört yüzeyde üçer, toplam 12 pati varyasyonu yeniden yerel üretildi. Eski perdesi duyulan darbe gövdesi kaldırıldı; kısa, yumuşak ve perdesiz pati teması kullanılıyor. Pati kazancı 0,11→0,09. `CatFoley` / `MiniGameFoley` zamanlaması ve animasyonlar değişmedi.
- `CatVoice` tek bakım sesi sahibi kalır. Mama 0,34, içme 0,42 kaynak seviyesi; yeni gerçek kayıtların daha düşük ortalama seviyesi dengelendi. Yaklaşırken bakım sesi yok. İptalde ve bakımın doğal tamamlanmasında ses kapanır; `BowlInteraction.ActiveCareSound` artık kaptan uzaklaşmadan önce temizlenir. Hareket sahipliği/çıkış animasyonu aynı.

Hazırlayıcı: `ArtSource/Audio/CatCare20260915/prepare_audio.py`. Genel ses üreticisi manifestteki bu 14 güncel dosyayı korur; eski sentezleri tekrar üzerlerine yazmaz. Ayrıntı [kaynak notunda](../ArtSource/Audio/CatCare20260915/README.md).

## Menü müziği

Kabul edilmiş 60 saniyelik masterda yaklaşık 11,90–12,08 ve 41,90–42,10 saniyelerde -65 dB altına inen kısa boşluklar vardı. İki Do→Fa cümle geçişine çok hafif ortak Do sesi kuyruğu eklendi. **Melodi, tempo, toplam süre ve döngü sınırı aynı.** Yalnız 11,55–12,33 ve 41,55–42,33 aralıkları değişti; dışındaki PCM örnekleri bit düzeyinde aynı. Önce 19 adet sessiz 20 ms pencere, sonra 0. Tepe yine yaklaşık -2 dBFS; kliplenme yok.

Menü klibi çalmadan önce açılır, Streaming yerine DecompressOnLoad/preload kullanır; önceliği 32. Böylece çalma sırasında diskten akış gerekmiyor. Açılmış 60 sn stereo float PCM karşılığı yaklaşık 21,97 MiB; menü kapanınca veri bırakılır, geri dönünce yeniden yüklenir. Bu değer fiziksel telefon bellek ölçümü değildir. Müzik tercihi, uygulama arka planı ve gerçek odak kaybında durma davranışı korunur.

Yeni master SHA256: `5c8f5d51336be24c679d917ced44518fdf2d93e23e322424e7562b795c399a33`.

## Müzik dengesi

| Durum | Kaynak hedef seviyesi |
|---|---:|
| Ana menü | 0,36 — aynı |
| Ev | 0,11 — önce 0,20 |
| Dış mekân | 0,12 |
| Mama / su | en fazla 0,025 |
| Dinlenme | 0,018 |
| Uyku | en fazla 0,012 |

Seviye düşüşü ve geri yükselişi yumuşak; bakım bitince normal oyun seviyesi geri gelir. Koşu/av müzik seviyesi bu istek kapsamında değişmedi. Kedi ve arayüz efektleri müzik azaltma işlemine dahil değil. `GameSoundscape` ve `TitleMusicController` ses kaynağı referansları editör derlemesinde korunur; çalışma sırasında görülen eski boş referans bulgusu giderildi. Bu, bütün oyunun Play sırasında derleme stres testi yapıldığı anlamına gelmez.

## Son doğrulama

**11/11 native PlayMode kontrolü başarılı**, validator **0 hata / 0 uyarı**. Son kanıt: `QA/AUDIO_REFINEMENT_2026-09-15/native-audio-release.xml`. Önceki iki 10/11 XML ara sonuçtur: yeniden açılan menü, otomatik testin gerçek masaüstü odak dışı durumunu alıyordu; testin ön plan varsayımı açıkça yeniden uygulandı. İlk tekrar yeni derleme yüklenmeden çalışmıştı. Son tur güncel derlemede çalıştı.

- Gerçek mama ve su düğmeleri; yaklaşmada sessizlik, tek bakım kaynağı, iptal ve suyun doğal tamamlanması/çıkışında sesin kapanması geçti.
- Bakım müziği 0,025'e indi ve 0,11'e geri döndü; gerçek uyku durumu 0,012'ye indi, uyanınca ev müziği geri geldi.
- Gerçek joystick ile 15/30/60 FPS yürüyüş ve koşu: altı çevrimde 4/4/4/6/5/6 pati olayı, doğru yürüyüş/koşu durumu; durunca yeni adım sesi yok. Bu, tüm ırk/yüzey matrisinin yeniden tarandığı anlamına gelmez.
- Menüde 11,4 / 41,4 / 58,8 saniyeden başlatılan gerçek ses işleme tamponları incelendi. 70 ms ana iş parçacığı duraklamaları altında çalma sürdü; en uzun sıfır serisi 1 örnek, 50 ms sessizlik yok. Döngü geçişi, müzik kapat/aç ile konumdan devam, belleği bırakma ve menüye geri dönüş geçti. Uzaktan bağlantının veya fiziksel hoparlörün çıkışı kaydedilmedi; asistan fiziksel kulakla dinleme yaptığını iddia etmez.
- 124 WAV/manifest hashleri, boş/sayısal bozuk örnek ve tepe payı kontrolü geçti. İki menü geçişi dışındaki PCM örneklerinin aynı kaldığı doğrulandı.
- 16/16 tercih ve önceki editör sessizliği geri yüklendi. Testlerin değiştirdiği EditorSettings, bu işin başlangıç hash'iyle **birebir** aynı byte dizisine döndü (`307A6390…`). Tarihsel kayıt geri yüklemesi yapılmadı.
- 14 sahne dosyası aynı; beş mevcut runtime dosyası yalnız ses davranışı için değişti. Kaynak sıçrama/dönüş/anatomi ve pati gözlemcileri aynı. Üç gerçek kayıt QA öncesiyle aynı: ana/recovery `CAE8CA6F1C2B9FFEADC26B7D061FB432CA4459B24A6B3055A12830F414B00B44`, CP2 `03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D`.

`verified-final.json`, `assets-verified.json`, `preferences-verified.json`, `title-repair.json`, `title-mixer.txt` ve `paw-cadence.txt` ayrıntıları taşır. Tam oda/eşya/on ırk veya fiziksel telefon testi bu turda tekrarlanmadı. Önceki salon tablo giriş bulgusu kapsam dışında kaldı.

## Kullanıcı denemesi

Unity **Play/QA açık**, açılış ekranında; ayrı kopya `Library/UiQaSession/20260915-091726`. GameScene + CatHome_UI + LivingRoom_Level01 temiz; tek oyun kedisi, ses sistemi ve etkin dinleyici. Son `editor-final.json` ölçümünde derleme kapalı, ses/müzik açık, editör sessiz değil. Ölçümde Unity odak dışı olduğundan müzik bilerek duruyor; Game penceresine kullanıcı odağı gelince sürer. Başlıktaki üç dekoratif kedi, üç gameplay kedisi değildir.

Kullanıcı bitirmeden denemeyi kapatmayın. **Tools > Cat Home > Tüm Sesler Denemesi > Bitir** veya Play'i durdurma normal geri yüklemeyi yapar. Yeni `Ses İyileştirmesi` menüsü aynı güvenli ön izlemeyi ve bu 11 yerel testi açar. Test sonrası normal üç sahne yeniden açılır; boş InitTestScene kullanıcıya bırakılmaz.

[Sesleri dinle](QA/AUDIO_REFINEMENT_2026-09-15/index.html). Adım dinleme örnekleri ses karakteri için hazırlanmış derlemedir, canlı oyun kaydı değildir. Video, APK, arşiv, commit, push veya yayın yapılmadı. QA Git dışında kalır.
