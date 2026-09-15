# Diğer odalarda etkileşim denetimi — 11 Eylül 2026

**Güncel banyo sonucu / adım7:** [son kontrol](BATHROOM_FINAL_2026-09-11.md), siyah Oriental ile **9/9 gerçek düğme**, **33/33 benzersiz native**, **8/8 EditMode**, validator0/0. Ayna artık dekor; aşağıdaki on eylem/on video turu adım1–6 öncesinin tarihsel kaydıdır. Güncel kanıt `QA/ROOM_INTERACTIONS_2026-09-11/bathroom-step7-verified/report.json` ve `QA/BATHROOM_FINAL_2026-09-11/test-summary.json`. Kâğıt erişimi aynı duvar hizasında fiziksel rulo/tutucu ve temas noktalarıyla düzeltildi. Unity ayrı kayıt kopyasında tek siyah kediyle kullanıcıya açık. Yeni video hazırlanmadı; banyo henüz kullanıcı tarafından kapatılmadı.

Kullanıcı salonu onayladı. Aynı süreç diğer odalarda uygulanıyor: önce asistanın gerçek oyun düğmeleriyle kontrolü ve gerekli düzeltmeler, ardından kullanıcının oda incelemesi. Her oda tek tek ele alınır; yeni bulgu olmadan onaylı salon düzeni değiştirilmez.

Envanter: banyo, mutfak, yatak odası, bahçe, balkon, avlu ve üst katta onar eşya; toplam **70 ROOM ürünü**. Kayıt ve kanıtlar `Docs/QA/ROOM_INTERACTIONS_2026-09-11` altında, önceki turlardan ayrı tutulur. `RoomInteractionReview` ayrı kayıt kopyasında oda içindeki on eşyayı gerçek düğmelerle başlatır; tam kare örnekleri, süre, dönüş, boş yürüyüş, tamamlanma ve tam hareket kapsülü çıkışını kaydeder. Dinlenmeler gerçek Kalk düğmesiyle biter. Sayısal hareket işaretleri görsel inceleme gerektirir; tek başına bütün animasyonu doğru ilan etmez.

## Banyo — asistan kontrolü tamamlandı, kullanıcı incelemesinde

[Son galeri](QA/ROOM_INTERACTIONS_2026-09-11/index.html): **10/10 gerçek oyun düğmesiyle tamamlanma**, her üründe tek tamamlanma/açık çıkış/kontrolün geri dönmesi; on tam **24 fps** video. Son kayıt `bathroom-final`; `bathroom-before` ve `bathroom-verified` ara karşılaştırmalardır. **18 benzersiz hedefli native testin son sonucu başarılı**, validator **0/0**. Son ortak giriş değişikliğinden sonra dört hedefli native test ve on gerçek düğme rutini tekrarlandı. Bütün 18 test o son küçük adımda tekrar çalıştırılmış gibi sunulmaz.

Son tıklamalar 9.6–20.2 ms; ayna **18.5 ms**. Yerinde Walk ölçümü bakım arabası/kum/duş/havlulukta 0, paspasta .125 sn, kâğıt rulosunda bir kare .042 sn. Bakım arabası ve sepetin yaklaşık 59° karelik dönüşü 18.7° oldu. Küvette gerçek sıçrama, duşta silkelenme gibi hareketli evreler korunur; bütün eylemler durağan sayılmaz. Telefon performansı ölçülmedi.

İlk gerçek düğme turunda 9/10 rutin tamamlandı; aynanın başlangıcı sonraki adımda ayrıca incelendi. Kâğıt rulosu sonundaki tam denetleyici çıkışı başarısızdı. Paspas, kum kabı, duş ve havlulukta hareketsiz Walk evreleri; bazı dönüşlerde yüksek karelik açı değişimi görüldü. İlk görüntüler `bathroom-before`, tarihsel karşılaştırma içindir.

- Kâğıt rulosu: .27 m temas yürüyüşü açıkken .31 m tam dönüş zarfı klozete yaklaşıyordu. Çalışma noktası/gerçek pati teması korunup açık zemine kısa çıkış eklendi. İptal sonrası da tam kapsül açıklığı sağlanır. On ırklı gerçek pati ve iki duraklatma/iptal testi **3/3** geçti (`bathroom-paper-exit.xml`).
- Paspas: yerinde dönüşte Walk kaldırıldı, hareket doğrultusu önce alınır; dönüş süreleri açıya göre uzar, gerçek ilerleme 1.5 m/sn ile sınırlıdır. Nazik pati hareketi, dinlenme ve Kalk aynı. On ırklı pati ve dinlenme bitişi **2/2** geçti (`bathroom-mat-contact.xml`).
- Kum kabı: doğrulanmış giriş/çıkış uçları korunarak boş yürüyüş ve hareket sırasında yan kayma kaldırıldı. Gerçek kazma, çömelme ve örtme değiştirilmedi. On ırklı temas ve bütün kum evrelerinde duraklatma/iptal **2/2** geçti (`bathroom-litter-motion.xml`).
- Duş: giriş, yerinde dönüş ve çıkış aynı açık uçlarla yapılır; Walk yalnız gerçek ilerlemede oynar. Su/köpük evresi ve temizleme **3/3** geçti (`bathroom-shower-motion.xml`).
- Havluluk: boş Walk adımları ve görünür poz üretmeyen eski ölçek beklemeleri kaldırıldı; gerçek sıçrama/uyku aynı. Dar destek, ırk değişimi ve duraklatma **1/1** geçti (`bathroom-towel-motion.xml`).
- Bakım arabası, küvet ve sepet: sabit .16 saniyeye sıkıştırılan dönüşler açıya göre uzar; doğrulanmış yaklaşma uçları aynı. Fırça geçişi aynı gerçek çizgide sabit ilerler; hareket etmeden Walk beklemez. Küvetin kenar yürüyüşü ve sepetin saklanma evresi korunur. Temas/yol ve iptal kontrolleri başarılı. Küvet testi artık kamera için seçilen yürüyüş yönünü ölçer ve iki gerçek kenar ucunun korunduğunu ayrıca doğrular; eski test daima ham başlangıç/bitiş sırasını varsayıyordu.
- Ortak giriş: salon dışındaki odalarda zorunlu .18 saniyelik büyük dönüş açıya göre uzar. Salonun mevcut giriş davranışı aynı. Kâğıt rulosunun yeni çıkış adımı da aynı kontrollü dönüşü kullanır. Son ortak giriş düzeltmesi dört hedefli native test ve tam banyo kaydıyla doğrulandı.
- Lavabo: on ırkta üçer gerçek deforme ağız/su örneği; en büyük ölçüm **25.32 mm**, dört pati gerçek destekte ve tam denetleyici çıkışı açık. **1/1 native / 10/10 ırk** başarılı (`bathroom-vanity-mouth.xml`, `bathroom-vanity-mouth.csv`). Lavabo modeli ve hareketi değiştirilmedi.
- Ayna: ilk başarısız denemede zemin yüksekliğini sıfırlayan QA yerleştirmesi komşu eşyanın düğmesine düşmüştü. QA artık mevcut zemin yüksekliğini korur ve düğmenin gerçek hedefini doğrular. Doğru başlangıçtaki gerçek tıklama gecikmesi **3281 ms** idi; yakındaki ayna için yerel aramayla **24.4 ms**, tek tamamlanma ve açık çıkış görüldü. Uzak başlangıçlarda eski oda rotası korunur. Eski native test dört metre uzaktaki spawn'dan, gerçek yakınlık düğmesi yokken başlatıyordu; bu koşulda eski genel arama da `ViewStandBlocked` döndürüyor. Native test artık tam kapsülün sığdığı gerçek ayna düğmesinden başlar; kamera, oturma ve sahipli iptal eşikleri korunur. Gerçek yakınlık başlangıcıyla oturma ve sahipli iptal **2/2** geçti (`bathroom-mirror-nearby.xml`).

## Sonraki odalar

Mutfak, yatak odası, bahçe, balkon, avlu ve üst kat bu yeni turda henüz denetlenmedi. Paylaşılan hareket sınıflarını kullanan ürünler sonraki oda kontrollerine dahildir; eski ırk matrisleri bu turun sonucu sayılmaz. Salon kullanıcı onayıyla kapalı. Banyo kullanıcının incelemesinde; ardından mutfak.

## Kayıt ve teslim durumu

Gerçek ana kayıt ve recovery SHA-256 başlangıç/son **3558D75B36B1710960624B629C8F0C2C51A1E1B231E2572B12984F825F896B9B**; CP2 **03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D**. Sekiz oda sahnesi birebir aynı. 16 tercih ve varlık bayrağı geri yüklenip karşılaştırıldı. Geçici font, CurrencyHud ve EditorSettings çıktıları geri alındı.

Kullanıcı için yeni ayrı QA kopyası `Library/UiQaSession/20260911-093209`. Play açık; `Oda Etkileşim Denemesi` banyoda on eşya ve ilk paspas düğmesiyle hazır. Panelden bitirme veya Play'i durdurma QA'yı kapatır, tercihleri ve gerçek kayıt ön izlemesini geri getirir. Son canlı durum `editor-ready.json`. Kullanıcı bitirmeden bu denemeyi kapatmayın. APK, arşiv, commit/push, yayın veya bilgisayarı kapatma yok.
