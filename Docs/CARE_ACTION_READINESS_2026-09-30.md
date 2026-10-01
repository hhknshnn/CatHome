# Mama, su ve uyku erişimi — 30 Eylül 2026

Başlangıç 15:57:17 UTC; kapanış 16:31:15 UTC, 34.0 dakika. Kullanıcının bildirdiği bakım hataları düzeltildi; APK bu tur istenmedi ve üretilmedi.

## Neden ve düzeltme

- Mama/su düğmesi 0,75 m yakınlıkta görünüyordu; gerçek başlatma alanı daha dardı. Yaklaşma açıklaması çeviri eşlemesinde bulunmadığı için “Biraz sonra yeniden deneyelim.” genel mesajına düşüyordu. Türkçe/İngilizce eşleme eklendi. Yakında fakat hazır değilken “Kaba yaklaş”, uygun duruşta “Mama ye”/“Su iç” gösterilir. Başarılı başlangıç eski yaklaşma mesajını kapatır.
- Normal joystick testi eski elipsin uzak kenarında erişilemeyen bir başlangıcı yakaladı. Salon kaplarının temas hedefi merkezde olduğundan normal yürüyüşte beden kabın önünde duruyor, ağız hedefe ulaşamıyordu. İki hedef, gerçek yenilebilir/içilebilir yüzeyin önüne yaklaşık 8,5 cm alındı. Görünür kaplar ve platform taşınmadı. Kaplar için ölçülmüş kaynak ağız konumuna göre 2,5 cm dış temas kabuğu şartı getirildi; mutfak etkinliğinin özgün erişimi korundu.
- Persian yatağa inişinin 0,61 aşamasında yükseltilmiş arkalık çarpışması tekrarlandı. Yalnız uyku hedefi 8 cm öne taşındı; yatak görseli ve engeller aynı kaldı. Üretici Editor kodu da bu iki hedef düzeniyle eşleştirildi.

Otomatik yaklaşma/ışınlama, iskelet uzatma, temas toleransı gevşetme veya ödül/ihtiyaç dengesi değişikliği yok. Güncel fiziksel kontrol tıklamada kalır; HUD sorgusu deri taraması yapmaz.

## Doğrulama

**8 EditMode + 11 PlayMode = 19 benzersiz PASS.** Son mesaj temizliği sonrası iki ilgili test yeniden geçti; esas sonuç `QA/CARE_ACTION_READINESS_2026-09-30/native-final-manifest.json`.

Persian ve Oriental Shorthair, sıfır enerjiyle normal joystick yaklaşımı ve gerçek HUD işaretçi tıklaması kullanarak mama/su bakımını tamamladı; yatağa çıktı, uyudu, enerjisi arttı ve uyandı. Ayrı dört gerçek 10 saniyelik bakımda her eylem 600 temas karesi, tek tamamlanma, sıfır kök kayması/dönmesi ve sıfır uygunsuz deri teması gösterdi. Mutfak, duraklatma/iptal, yanlış yön, boş/dolu kap, araya aynı karede engel gelmesi, Türkçe/İngilizce ve sorgu maliyeti kontrolleri geçti.

İlk tekrarlar özgün mesaj ve yatak hatalarını gösterir. Ara yürüyüş denemeleri erken hazır olma ve merkez hedef erişimini teşhis etti. Ortak sınırın mutfak regresyonu kaplara özel koşulla düzeltildi. Test düzeneğinde ilk HUD kare/kamera eşlemesi düzeltildi. Ara XML başarısızlıkları tarihsel kanıttır; sıfır test seçilen tur başarı sayısına dahil değildir. Bütün ırklar ve fiziksel telefon bu tur denenmedi.

Computer Use ile gerçek Unity Game View kontrol edildi. Altı 1920×1080 ekran görüntüsü QA klasöründe; [mama](QA/CARE_ACTION_READINESS_2026-09-30/persian-food.png), [su](QA/CARE_ACTION_READINESS_2026-09-30/persian-water.png), [uyku](QA/CARE_ACTION_READINESS_2026-09-30/persian-sleep.png). Sayaçlar kopya kayıtlı test durumudur, oyuncunun ilerlemesi değildir.

## Koruma ve kapanış

Kullanıcının başlangıç Play oturumu normal durdurularak kaydedildi; koruma başlangıcı bundan sonra alındı. Tarihsel kayıt yüklenmedi. Son 7872 okunabilen başlangıç dosyası aynı; 8 kapsam içi değişiklik, iki yeni test/meta, eksik sıfır. Dört gerçek kayıt ve 16 tercih aynı. HUD/ana sahne dosyaları ve kamera/ışıkların 21 YAML bloğu aynı. Salon sahnesinde üç hedef konumu değişti; iki FeedingPoint quaternionunun yalnız -0/0 yazım farkı var. İki font önbelleği bu turun hash doğrulanmış başlangıcına döndü. Özgün Eat klibi başlangıçta okunamadığı için eksiksiz tüm-varlık hash iddiası yok.

Üç temiz sahne, Unity açık; Play/QA/derleme kapalı. Son yerel Console sayacı 0 hata/0 uyarı, etkin Main Camera AudioListener var. APK/commit/push/yayın yok. Önceki LivingFinal APK bu düzeltmeleri içermez. Önceden bilinen Persian berjer iniş iptali bu bakım görevinin dışında kalır. Kaydedildi ve duruldu.
