# Cat Home — görünür eylemler ve banyo teması

9 Eylül 2026. Kaynak, temas ve kapsam doğrulaması tamamlandı: **510/510 EditMode, 41 benzersiz native testin güncel sonucu başarılı, validator 0 hata / 0 uyarı**. [Galeri](QA/CAMERA_FACING_2026-09-09/index.html) **195 PNG ve sekiz gerçek Unity videosu** içerir. Normal editör düzeni ve gerçek kayıtların korunduğu doğrulandı.

## Kullanıcının istediği davranış

Teslim sonrası kullanıcı tercihi: Bundan sonra ayrıca indirme dosyası/paketi oluşturulmayacak. Açıkça yeniden istenmedikçe ZIP/RAR arşivi hazırlanmaz; yerel görseller, videolar, galeri ve Markdown kayıtlarıyla teslim edilir. Kalıcı kural çalışma alanının `AGENTS.md` dosyasında ve güncel checkpoint'te kayıtlıdır.

Klozet kâğıdına gerçek pati teması ve kopan kâğıtlar; yanda, ekrandan görünen kum kabı; gerçek kum çukuru, nazik kazma, çömelme ve kapatma. Bütün mevcut ve gelecekteki ev odalarında eylemin önden ya da yandan okunması.

## Uygulama

- Banyoda kum kabı sol kenara, klozet sağ ön bölüme alındı. Klozetin ön yüzü ve kâğıt rulosunun çalışma tarafı oda kamerasından görünür. Kâğıda temas noktası ile ürüne yaklaşma noktası ayrıldı; giriş yerleşimi artık pati hedefini kaydırmaz.
- Rulonun sabit uzun kuyruğu kaldırıldı; gerçek pati temasıyla sınırlı sayıda kâğıt parçası kopar, savrulur ve kaybolur. Görsel efekt temasın yerine geçmez.
- Kum gerçek yüzey ağı olarak en fazla **.038 m** çöker ve kenara yığılır. İnceleme, dönüşümlü kazma, gerçek iskeletle çömelme, aynı çukuru kapatma ve çıkış ayrı evrelerdir. Kum derinleşince üst omurga en fazla **14°** eğilir; kök ölçeği, kemik uzunluğu, kalça ve arka pati desteği değişmez. Pati teması çözüm sonrasında **.014 m** altında kabul edilir; native son poz denetimi **.015 m** sınırını korur. Duraklatma sabit pozu tutar; iptal geçici ağı, efektleri ve hareket sahipliğini bırakır.
- Kumun ayrı `CatHome/Litter Sand` malzemesi ince tanecik, gerçek çukur yüksekliğine bağlı koyulaşma ve yükselen kum kenarında aydınlık kullanır. Fiziksel ağ ve temas verisi değişmez; düz bir çukur çıkartması kullanılmaz. Blender üçgen ölçümünde katı tabanın, çukurun en altından yaklaşık **.042 m** aşağıda kaldığı doğrulandı. Modern malzeme üretimi bu özel shader'ı korur.
- Ortak eylem yönü gerçek oda kamerasından hesaplanır. Durağan çalışma/dinlenme yönü ile gerçek seyir yönü ayrılır. Dar desteklerde güvenli eksen, temaslı eylemlerde gerçek pati/ağız noktası korunur. Yürüyüş öncesinde dönülür.
- Bakış eylemleri ürünün gerçek görünen yüzeyini kullanır. Gövdenin kamera yönü ile başın hedef yönü ayrı sınırlanır. Yeni oda veya eylem için aynı kural üretim ve doğrulama yolundadır.
- Su içme noktaları gerçek su yüzeyine ve dört patinin gerçek desteğine göre değerlendirilir. Dört ürün/on ırk denetimi ve gerçek ağız erişimi başarılıdır. Gerekli küçük ağız profilleri editörde hazırlanır; mobilde bütün kedi mesh'inin CPU kopyası açılmaz. Ayrıntılar [su teması notundadır](CAMERA_FACING_WATER_CONTACT.md).

Önceki sakin paspas, yalnız küvet kenarında yürüyüp inme, duş suyu/köpüğü/silkelenmesi ve oturarak ayna bakışı korunur. Mini oyunların oynanış kameraları bu ev eylemi kuralıyla çevrilmez.

## Kalıcı üretim kuralı

[Oda eylemlerinde kamera görünürlüğü ve gerçek temas](CAMERA_FACING_AUTHORING_RULE.md) ve çalışma alanının [AGENTS.md](../../AGENTS.md) dosyası mevcut/yeni oda sözleşmesini tutar. Yalnız kök dönüşü yeterli kanıt değildir: son animasyon ve temas düzeltmesinden sonra gerçek kalça–omuz yönü ölçülür; örtülme ayrıca gerçek görüntüden incelenir.

## Doğrulama

| Denetim | Güncel sonuç |
| --- | --- |
| Tam EditMode | 510 başarılı / 0 başarısız / 0 atlanan |
| Benzersiz native testler | 41 başarılı / 0 başarısız |
| İçerik validator'ı | 0 hata / 0 uyarı |
| Sekiz oda envanteri | 80 ROOM kimliği; 79 etkin rutin, iki bilinçli dekor |
| Salon CAT ve ücretsiz sahne eylemleri | 17 CAT rutini + 5 ücretsiz rutin; açık kapsam yok |

Envanter taraması varsayılan ırkla yapılır; ırka bağlı temas, kum, kâğıt, bakım ve dinlenme kontrolleri ayrıca **on ırkla** yürütülür. Dört su ürünü için **40 ürün×ırk** denetimi, 34° ön gövde/ağız erişimi, bahçe/veranda tekrarları ve kumun tam rutin/duraklatma kontrolleri başarılıdır. Bu sayılar bütün 101 rutinin on ırkla yeniden tarandığı anlamına gelmez.

Küvet, fırça, mutfak mama istasyonu, üç dış mekân eşelemesi ve iki açıklıktan tünel geçişi, durağan örnek üretmedikleri için ayrı hareket/temas testleriyle kapatılır. Bilinmeyen `no_stationary_stage` başarı sayılmaz. [Kapsam birleşimi](QA/CAMERA_FACING_2026-09-09/coverage-summary.json) `coverageClosed=true` ve boş hata listesi taşır; [native birleşimi](QA/CAMERA_FACING_2026-09-09/native-test-summary.json), [son EditMode](QA/CAMERA_FACING_2026-09-09/EditMode-final.json) ve [validator](QA/CAMERA_FACING_2026-09-09/validator-final.txt) kanıtları saklanır. Ara başarısız XML dosyaları silinmez; aynı testin en güncel sonucu esas alınır.

**103 ürün kartı ve sekiz oda ön izlemesi yenilendi.** Sayısal yön/temas ölçümü görsel incelemenin veya fiziksel cihaz performans ölçümünün yerine geçmez.

## Son görseller ve normal düzene dönüş

Son kum malzemesiyle geniş/yakın çekimler, 4:3 banyo görünümü ve sekiz odanın HD fotoğrafları yenilendi. Galeride **195 PNG / sekiz video** bulunur; eksik görsel veya geçersiz video yoktur. Sekiz MP4 gerçek **24 fps** kaynak karelerinden, hız ve süre değiştirilmeden yeniden kodlandı; her kayıtta tek tamamlanma, kare sayısı ve video boyutları doğrulandı. Çekim ayarları geri yüklendi.

**QA/Play kapalı; derleme sürmüyor.** GameScene / CatHome_UI / LivingRoom_Level01 yüklü ve temiz; tek kamera ve tek ses dinleyici var. 16 sunum tercihi ve varlık bayrakları birebir geri yüklendi. Unity ile gerçek JSON'u yalnız okuyan güncel editör ön izlemesi açık. Kanıt: [editör durumu](QA/CAMERA_FACING_2026-09-09/editor-restored.json), [tercihler](QA/CAMERA_FACING_2026-09-09/preferences-restored.txt).

Ana kayıt/recovery başlangıç ve bitiş SHA-256 değeri `D620A1C69019C2FBA1C3567CFA4A434E64217B22CAE18B2893C1726A0938B67E`; CP2 yedeği `03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D`. Üçü başlangıçla aynıdır; QA ayrı kopyada yürütüldü. [Kapanış karşılaştırması](QA/CAMERA_FACING_2026-09-09/save-hashes-after.json) saklanır. Eski teslimlerin kayıt özetleri geri yükleme kaynağı değildir.

APK/Android derlemesi, bulut yayını ve commit/push bu çalışmanın parçası değildir. Bilgisayar açık kalır. Fiziksel cihaz performansı burada ölçülmez.
