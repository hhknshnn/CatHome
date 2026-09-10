# Oda seçiminde toplu renk değişimi — 10 Eylül 2026

Kullanıcının gönderdiği 10,44 saniyelik masaüstü kaydı ve Console çıktısı incelendi. Videoda yaklaşık 8,1 saniyede Banyo'ya tıklanınca görünen bütün oda kartlarının zemini aynı anda koyulaşıyordu. Tek oda yükleniyordu; paylaşılan Console satırları bağlantı, bildirim, kayıt yükleme ve giriş kilidi bilgilerini içeriyordu.

## Neden ve düzeltme

`RoomSelectorPanel`, oda yüklenirken ikinci seçimi önlemek için bütün kartların `Button.interactable` değerini kapatıyordu. `PremiumButtonFx`, bu geçici kilidi bütün kartlara devre dışı yüzey rengi olarak çiziyordu. Bu yüzden tek seçim, bütün kartlar seçilmiş gibi görünüyordu.

[RoomSelectorPanel.cs](../Assets/Scripts/HomeRooms/RoomSelectorPanel.cs), açılış/geçiş/kapanış sırasında kartların geçici giriş kilidiyle birlikte yalnız bu kartların `PremiumButtonFx` bileşenlerini askıya alır. Efektin mevcut `OnDisable` temizliği basılma, odak, parlama ve geçici ölçeği bırakır. Kartın normal rengi ve mevcut oda vurgusu korunur. Düğmeler gerçekten devre dışı kalır; `OnRoomSelected` durum kapısı da ikinci çağrıyı reddetmeye devam eder.

Panel tekrar kullanılabilir olduğunda yalnız bu panelin askıya aldığı efektler açılır. Başarısız geçiş ve yeniden açılışta giriş ve hover geri gelir. Önceden zaten kapalı bir efekt zorla açılmaz. Ortak efekt sınıfı, oda yükleyici, kaynak prefablar ve oda yerleşimleri değiştirilmedi.

## Doğrulama

- **5/5 native PlayMode:** iki yeni oda seçimi testi ve mevcut üç kaydırma/giriş testi. [Son XML](QA/ROOM_SELECTOR_2026-09-10/Native-room-selector.xml).
- **23/23 EditMode:** mevcut `HomeRoomNavigationTests` grubu. [Son XML](QA/ROOM_SELECTOR_2026-09-10/EditMode-room-navigation.xml).
- **1920×1080 ve 1440×1080:** sekiz kartın gerçek yüzey çiziminden üretilen köşe renkleri, kontrollü geçiş beklemesinden önce ve sonra birebir aynı. Tıklama, klavye submit ve eski doğrudan listener çağrıları beklemede yeni yükleme başlatmıyor. Başarısız yükleme sonrası hover ve paneli tekrar açma başarılı.
- **Gerçek Banyo ve Salon geçişleri:** seçilen hedef bir kez yükleniyor; toplu eski callback çağrıları ikinci geçiş üretmiyor. Varışta modal kilit bırakılıyor, panel yeniden açılabiliyor ve tek mevcut oda rozeti bulunuyor.
- Mevcut popup testleri fotoğraf/boşluk/kart üzerinden wheel, fare ve dokunmatik sürükleme, normal tıklama ve azaltılmış hareket kontrollerini doğruladı.
- Altı güncel PNG boyut ve dosya geçerliliği açısından kontrol edildi. Geniş ekran ve 4:3 açılış/bekleme görselleri ile gerçek varış görüntüleri saklandı. Bekleme ekranları yükleme olayının kontrollü tetiklendiği testten, varış ekranları gerçek oda değişimlerinden gelir.

Görseller: [geniş ekran açık](QA/ROOM_SELECTOR_2026-09-10/open-1920.png), [geniş ekran geçiş beklemesi](QA/ROOM_SELECTOR_2026-09-10/travelling-1920.png), [4:3 açık](QA/ROOM_SELECTOR_2026-09-10/open-1440.png), [4:3 geçiş beklemesi](QA/ROOM_SELECTOR_2026-09-10/travelling-1440.png), [Banyo varışı](QA/ROOM_SELECTOR_2026-09-10/arrived-bathroom-01.png), [Salon varışı](QA/ROOM_SELECTOR_2026-09-10/arrived-living-room-01.png).

[Yeni regresyonlar](../Assets/Tests/PlayMode/RoomSelectorFeedbackTests.cs) ve [doğrulama özeti](QA/ROOM_SELECTOR_2026-09-10/verification-summary.json) saklanır. İlk test yardımcısındaki belirsiz reflection overload çağrısı düzeltildi; başarısız ara XML korundu ve son başarı sayısına katılmadı. Görsel kaydın sonraki karede yazılması için evreyi sabit tutan bekleme eklendikten sonra iki regresyon ve üç kaydırma testi birlikte tekrar geçti.

Bu dar değişiklikte tam 510 EditMode paketi ve içerik validator'ı yeniden çalıştırılmadı; önceki 9 Eylül sonuçları tarihsel temeldir. Fiziksel cihaz performansı ölçülmedi.

## Gerçek kayıt ve normal editör düzeni

Çalışma başladığında Unity gerçek kayıtla Play modundaydı. Play kapatılırken oyun normal son kaydını yazdı; bu nedenle test koruma başlangıcı **Play kapandıktan sonra** alındı. Eski kayıtlar geri yüklenmedi. Play kapanışı öncesi ve sonrası hash listeleri ayrı saklanır.

QA başlangıcı ve sonu ana kayıt/recovery SHA-256: **A4764D565EFBBC778D444D3A3F9EF895FFA34BA36A234B22A386414E26B515C8**. CP2 yedeği: **03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D**. Üç dosya test boyunca birebir korundu. [Başlangıç](QA/ROOM_SELECTOR_2026-09-10/save-before-qa.json), [son](QA/ROOM_SELECTOR_2026-09-10/save-after-qa.json).

**16 tercih ve varlık bayrakları tam geri yüklendi. QA/Play kapalı; derleme yok.** GameScene / CatHome_UI / LivingRoom_Level01 temiz; bir kamera ve bir ses dinleyici var. Gerçek kaydı yalnız okuyan editör ön izlemesi yenilendi. [Tercihler](QA/ROOM_SELECTOR_2026-09-10/preferences-restored.txt), [editör durumu](QA/ROOM_SELECTOR_2026-09-10/editor-restored.json).

APK/Android derlemesi, ZIP/RAR paketi, commit/push ve canlı yayın yapılmadı. Bilgisayar açık kaldı.
