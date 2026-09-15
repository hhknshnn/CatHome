# Seramik mama kabı — 11 Eylül 2026

Kullanıcının Grup 2 görüntüsündeki `cat.ceramic-bowl` düzeltildi. Önceki 17 eşya turu rutinin tamamlanmasını denetliyordu; bu kabın gerçek ağız–mama temasını ölçmüyordu. Ana mama/su istasyonu bu bildirimdeki ürün değildir.

Eski temas noktası kabın ön dış kenarındaydı: yerel `(0, .07760, -.18001)`. Ortak oyuncak yaklaşımı kediyi bu noktadan ayrıca .39 m uzakta tutuyordu. Yeni üretici gerçek mama alt ağını ölçer: merkez `(0, .089919, .0106304)`, yatay yarıçaplar `(.146402, .122978)`. Yalnız bu ürünün yaklaşımı mevcut on ırklı Eating ağız ölçüsünü kullanır; mamanın yakın tarafında içte kalan hedefe sabit kök hizası seçilir. Giriş tam yürüme kapsülüyle, son kısa adım gerçek patilere uygun açıklıkla denetlenir. Kapsül yalnız açık çıkışa ulaştıktan sonra geri açılır.

Kabın modeli, hacmi, konumu, malzemesi; kedinin ölçeği ve özgün Eating klibi değişmedi. Baş IK'sı veya kare başına ağza göre gövde kaydırma eklenmedi. Seramik ürünün önceki 2.2 saniyelik zamanlanmış Eating evresi aynı. Ana kaplar ve diğer ürünlerin temas politikası korunur.

[Son galeri](QA/CERAMIC_MEAL_2026-09-11/index.html): kullanıcının önceki görüntüsü, yeme karesi ve gerçek 24 fps tam rutin videosu. Eski 17 ürün galerisindeki mama kabı kartı son görüntüye bağlandı.

- **10/10 ırk:** Gerçek deforme ağız yüzeyi ile gerçek mama üçgenleri ölçüldü. Her ırkın en yakın mama uzaklığı en fazla 5.66 mm; Eating örneklerinin %85–100'ünde ağız mama sınırında. Doğal baş kaldırma evresi korunur. Gerçek gövde dot en az .376; bütün pati merkezleri kabın yatay sınırı dışında, en az 1.396 normalize yarıçapta. Tek tamamlanma ve açık çıkış 10/10.
- **Duraklatma/iptal:** Oyun saati sıfır; duraklatılan karede bekleyen coroutine/Animator işleri boşaltıldıktan sonra kök, çene ve klip evresi sabit. İptalde tam kapsül ve açık zemin geri geliyor. İlk testler bekleyen kareleri yanlışlıkla donmuş kare sayıyordu; son `native-verified.xml` başarılı. Üretim duraklatma kodu değiştirilmedi.
- **Diğer eşyalarda hareket:** `AllFifteenProducts_TurnSmoothly_AndDoNotWalkWithoutTravel` başarılı. Son üç benzersiz native testin kaynakları `verification-summary.json` içinde; ara başarısız XML kökleri toplu başarı diye sunulmaz.
- **Gerçek oyun düğmesi:** Russian Blue ile tek tamamlanma, açık çıkış, kontrolün geri dönmesi. Video 113 kare / 24 fps / 4.708 saniye; hız ve süre değiştirilmedi. Görüntüde ağız mamanın içinde.
- **Validator:** 0 hata / 0 uyarı. Sekiz oda sahnesi başlangıç hash'leriyle aynı. Tam EditMode, bütün diğer oda animasyonları ve telefon performansı bu dar düzeltmede yeniden taranmadı.

Gerçek kayıt/recovery başlangıç ve bitiş SHA-256 **3558D75B36B1710960624B629C8F0C2C51A1E1B231E2572B12984F825F896B9B**; CP2 **03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D**. 16 tercih ve varlık durumu geri yüklendi, eski QA kapatıldı. Sonrasında kullanıcının incelemesi için yeni ayrı kayıt kopyasıyla Grup 2 / mama kabı hazır bırakıldı. Son canlı durum `editor-ready.json`; Play'i veya paneldeki denemeyi bitirmek tercihleri ve gerçek kayıt ön izlemesini geri getirir. Salon kullanıcının incelemesine açık; kapatılmadı.

Geçici font, CurrencyHud ve EditorSettings çıktıları geri alındı. APK, arşiv, commit/push, yayın veya bilgisayarı kapatma yapılmadı. Siyah Oriental incelemesindeki malzeme/gölgeye dokunulmadı.
