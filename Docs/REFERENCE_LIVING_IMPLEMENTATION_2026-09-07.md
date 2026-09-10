# Onaylı salon referansının uygulaması

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

Sonraki kullanıcı düzeltmesi: [Salon çarpışma düzeltmeleri](ROOM_COLLISION_FIXES_2026-09-07.md). Ana yatak artık z=1.70, ikili koltuk dünya ölçeği .76; tünelden yalnız Oyna komutuyla geçilir. Aşağıdaki ilk uygulama kaydının bu konulardaki ölçüleri tarihseldir.

Kullanıcı kararı: “Onaylıyorum. Referanstaki gibi uygula.” Bu kayıt, `DesignProposals/2026-09-06_RoomScale/PLAN.md` içindeki önden kamera ve ayrı alt şerit (A) yönünün gerçek Unity uygulamasını izler.

## Yerleşim ve boyut

Salon kamerası merkezde, `(0, 3.6, -6.5)`, 23° eğim ve 38° görüş açısı kullanıyor. Sol medya grubu ile sağ oturma grubu birbirine bakıyor. Kitaplık arka solda, tablo arka orta duvarda açıkta. Berjer daraltıldı; mama ve su ayrı, arkadaki bakım köşesinde.

38° referans 16:9 oyun kadrajıdır. Dar 4:3 ekranda `HomeWorldViewport.FitFieldOfView` görüşü büyüterek aynı yatay oda alanını korur; geniş ekranda 38° kalır. Kalıcı sahne lensi ve kamera yönü değişmez. Popup arka planı geçici olarak tam görüntü alanından alınır ve dünya kamerasının alt şerit sınırı hemen geri yüklenir.

TV ünitesi 2.20 m genişlik / 0.523 m yükseklik; konsol 0.46 m genişlikte ve açık rafın içinde. İki hoparlörün her biri 0.23 m genişlikte, ince kendi ayaklarıyla TV yanında. Böylece konsol veya ses sistemi TV ünitesi henüz alınmamış kayıtlarda da fiziksel desteğe sahip. Var olan sahiplik ve satın alma ön koşulları korundu.

Katalogdaki 86 ROOM tanımının (80 güncel ürün ve altı eski tanım) gerçek prefab ölçüleri `QA/REFERENCE_LIVING_2026-09-06/room-proportions.csv` içinde. Eşyalar tek bir katsayıyla küçültülmedi; tünelin iç açıklığı ve ırkların doğal farkları korundu. Yeni tünel katalog kutusu 0.58 × 0.76 m, yüksekliği 0.60 m. Kalıcı ana yatak 0.94 × 0.70 m; mama/su istasyonları yaklaşık 0.34 × 0.32 m.

Beş seçilebilir CAT / en fazla bir CAT yatağı sınırı ve koleksiyon/depolama korunuyor. Bütün 4.147 izinli beşli ürün birleşimi otomatik yerleşim ve bağlantılı geçiş kontrolünden geçti. Kalıcı bakım yatağı bu beşli seçimden ayrı. ROOM alımları sabit yerde görünür; CAT eski sürükleme koordinatları yeni otomatik düzene taşınır.

## Hareket ve seçim

- Analog gücü gerçek yürüme hızını belirliyor; güçlü girişte yeterli enerji varsa koşu başlıyor. Animasyon temposu katedilen mesafeden hesaplanıyor. Fizik denetleyicisinin minimum adım eşiği sıfırlandı: yüksek kare hızında hafif analog hareketin kaybolması önlendi. Duvar önünde gövde durunca patiler de duruyor.
- Ortak atlayış çömelme, itiş/uçuş ve iniş evreleri kullanıyor; yol boyunca yürüme pozu oynatılmıyor. Koltuk ve sehpa da aynı hareketi kullanıyor. Sehpadaki nesne ancak gerçek pati teması gerçekleşince itiliyor.
- Tırmalama ayrı bir gövde pozu ve iki ön kol zinciriyle, iki patinin dönüşümlü yüzeye temas ettiği vuruşlar kullanıyor. On ırkta iki patinin temas sayısı ayrı doğrulanıyor.
- Tünel girişini iki ağza olan erişilebilir yol belirliyor. Ters girişte çıkış da ters çevriliyor; iç kumaş ve kuyruk koruması korunuyor.
- Ürüne dokunma seçimi sabitler. Eylem alanında ürün adı görünür ve ürünün köşeleri ince turkuaz işaretlerle belirtilir. Yakın ürünler arasında etiket titremesini önleyen mesafe payı vardır.
- Dinlenirken mevcut yavaş enerji toparlanması korunuyor; koşu küçük ek enerji tüketir.

Yeni ana yatakta uyku pozunun gövde teması gerçek görünen mesh üzerinden minderin 5 mm üstüne hizalanır. On ırkta, iri Maine Coon dahil, kıvrılmış gövde yatağın kullanılabilir alanında kalır. ROOM tırmalama rutinlerinin yaklaşım sahipliği düzeltildi; aktivite bitiminde hareket denetleyicisi kapalı kalmaz.

Seçim, yürünebilir oyuncakların trigger geometrisini de görür. Görünmez oda sınırları seçim ışınını kesmez; gerçek görünen mobilyalar arkalarındaki nesneyi gizlemeye devam eder. Depodaki eşyalar seçilemez. Hem tünel hem sabit ikili koltuk için bu davranış native PlayMode testiyle ölçüldü.

## Görüntü ve alt gezinme

Alt gezinme 1080p referansta 80 px yüksekliğinde ayrı bir şerittir. Sekiz oda kamerasının görüntüsü bu şeridin üstünde biter. Telefonun alt güvenli alan payı da şeride dahil edilir. Kamera tabanına eşya gizlenmez. Mağaza/Salon/Oyunlar erişimi ve mevcut Blender ikonları korunur.

İlerleme etiketi de alt ortadan sağdaki eylem alanına taşındı. Özellikle ön sıradaki tırmalama direğinde kedinin gövdesini örten ikinci bir şerit kalmaz. Dock ikonları 38 px, kendi sanat alanları 44 px ve düğmeler 56 px yüksekliğindedir.

TV içeriği oyunun kendi üç kedisinin Unity'de üretilmiş 1920×1080, 24 fps, 10 saniyelik animasyonudur; çalışma anında ikinci dünya kamerası açılmaz. Oda kapanınca video ve render hedefi bırakılır; azaltılmış hareket ayarında durağan kendi kedi karesi kullanılır.

Blender kaynakları `ArtSource/Blender/PremiumFurniture/build_reference_living.py`, `build_pet_collection.py` ve video kodlama betiklerindedir. Üretim yalnız headless Blender 5.2 ile yapıldı. Modeller mevcut `CH_*` materyal ailesini kullanır. Yapay gri kedi gölgesi geri eklenmedi.

## Doğrulama

- 442/442 EditMode testi geçti. Kapsam 4.147 izinli beşli CAT yerleşimini, dar/geniş kamera hesabını, modal önceliğini ve UI çakışmalarını içerir.
- Sekiz odada 80 ROOM ürünü × 10 ırk = 800/800 rutin. Başlama, tamamlanma, iskelet hareketi ve açık çıkış kaydedildi.
- CAT ürünlerinde 170 ürün/ırk rutini, koltuk/sehpa için 20 ek rutin, çift pati tırmalama, iki yönlü tünel, kumaş/gövde/kuyruk sınırı, analog yürüme/koşu ve ana yatak teması kontrolleri geçti.
- 103 mağaza fotoğrafı son inşa edilmiş prefablardan ve sekiz oda fotoğrafı yeniden üretildi; toplu görseller gözle incelendi. Salon fotoğrafı, gerçek oyun politikasındaki beş CAT ürününü kullanır; gizli 17 ürünü birden göstermez. HUD içermeyen 16:9 katalog fotoğrafında salonun tamamını korumak için yalnız çekim kamerası 42° kullanır.
- Normal oyun hızında 1920×1080 / 24 fps tırmalama, tünel, sehpa ve koltuk kayıtları alındı. Görsel galeri: [gerçek oyun ve hareketler](QA/REFERENCE_LIVING_2026-09-06/index.html).

[Doğrulama manifesti](QA/REFERENCE_LIVING_2026-09-06/verification-manifest.json) her native testin son sonucunu kaynak XML'e bağlar. İlk denemelerdeki hatalı sonuçlar izlenebilirlik için tutuldu; ilgili testlerin son tekrarları başarılıdır. Oda matrislerinin her biri 100 başarılı satır içerir. Sahne doğrulaması 0 hata / 0 uyarı.

Gerçek oyuncu kaydı yerine `UiQaTestSession` kopyası kullanıldı. Gerçek telefon performansı bu masaüstü doğrulamasında ölçülmedi. Git commit/push kullanıcıya bırakılır.

Koleksiyon kutlaması artık mağaza, geri dönüş, oyun seçimi, diğer modallar veya devam eden kedi etkileşimi kapanana kadar bekler. Bekleyen ödül kuyruğu korunur; kapıları denemek için gerçek ödül toplanmadı. Seçim görseli galeride native ışın sorgusunun sonucu hedefe atanarak üretildi; işletim sistemi fare odağı bu otomasyon oturumunda güvenilir olmadığından fiziksel dokunmatik cihaz testi olarak sunulmaz.
