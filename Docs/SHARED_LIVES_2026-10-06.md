# Dört oyun için ortak 20 can — 6 Ekim 2026

Kullanıcı ayrı oyun canları yerine Oyunlar ekranında tek bir 20 can havuzu istedi.

- Eve Dönüş, Pati Avı, Yumak Rotası ve Gölet Keyfi artık `MiniGameLivesService` üzerinden aynı **20 canı** kullanır.
- Menü/karşılama açmak ücretsizdir. Gerçek yeni tur ve tekrar deneme bir can harcar. Çift başlatma mevcut tur kilitleriyle ikinci kez harcamaz. Duraklatma/sürdürme ve menüye dönüş ücretsizdir.
- **10 dakikada 1 can**, çevrimdışı süre dahil, en fazla 20. Saatin geri alınması can üretmez. Dolu havuzdan ilk harcama süreyi başlatır; sonraki harcamalar saati sıfırlamaz.
- Yumakta 2 elmasla +60 saniye ve önceden ödenmiş devam kaydı aynı turdur; sıfır canda da çalışır, ayrıca can harcamaz. Bölümdeki üç hamle ayrı oynanış kuralı olarak korunur.
- Oyunlar başlığında tek ortak sayaç ve sonraki canın süresi; kartlarda oyun süresi/özelliği. Eski ayrı sayaçlar ve Gölet'in “Can gerekmez” metni kaldırıldı. Yeni oyunlarda can bittiğinde yeni tur düğmesi kapanır ve yenilenmede açılır. Pati sonuç düğmesi de yenilenmeyi canlı izler.
- **Kayıt v14:** eski kayıt ilk geçişte ortak 20 can alır. İlerleme, yıldızlar, rekorlar, cüzdan ve devam kaydı korunur. Mevcut sınırsız haklardan en uzun olanı taşınır; aynı günün iki eski reklam sayacı ortak günlük limite birleştirilir. Sonraki yüklemeler havuzu yeniden doldurmaz. Ana oyunda yeni yolculuk başlatmak da ortak canı sıfırlamaz.
- Eski Runner/Catch alanları geriye dönük kayıt/migrasyon ve Catch öğretici bilgisi için tutulur; aktif tur girişleri, reklam ödülleri, bildirim sayacı ve UI yeni havuzu kullanır. Reklam ödülü +2, ortak günlük sınır 3 olarak korunur; yeni reklam sağlayıcısı bağlanmadı.

## Doğrulama

- `QA/SHARED_LIVES_2026-10-06/EditMode-final.xml`: **20/20 seçili native EditMode PASS**. Harcama, limit, saat, çevrimdışı yenilenme, kayıt serileştirme, v6/v12/v13 geçişi, eski ilerleme, reklam sınırı, hak süresi, bozuk değer ve yeni yolculuk koruması.
- `runtime-checks.txt`: normal Unity Play içinde gerçek dört oyun ve hub üzerinden doğrulamalar. Her oyun başlangıç + tekrar denemeyle 20→12; menü ve pause ücretsiz; gerçek QA kayıt dosyasında 12; kayıt havuzu tekrar uygulanınca 12. Dört oyunda sıfır can engeli, yeni oyunlarda yenilenen canla düğmenin açılması ve tek harcama. Yumakta sıfır canla elmas devamı/konum koruma/ödenmiş kaydın ücretsiz sürmesi. Sonradan Pati sonuç düğmesinin yenilenme kontrolü de eklendi. Bunlar **native PlayMode test adedi değildir**; ayrıntı ve nihai koşul sayısı closure.json içindedir.
- Üç çoklu native PlayMode koşusu bağlantı zaman aşımı/yanıtsız editör nedeniyle tamamlanmadı; hiçbirine PASS yazılmadı. Editör kaydedilmiş sahnelerle yeniden açıldı, kurtarma yedekleri korundu. Normal Play çalışırken arka plan çalışması yalnız QA oturumunda açıldı; native çoklu koşudaki sorun çözülmüş sayılmaz. Oynanış doğrulaması normal Play içindeki kontrollü coroutine ve gerçek UI ile tamamlandı.
- Son **54 sunum**: TR/EN × üç oran (1920×1080, 1440×1080, 2340×1080), hub 20/12/0 ve yeni oyunların hazır/boş/boş sonuç ekranları. Metin taşması, düğme çakışması, ekran dışı ve tıklanamayan merkez bulgusu 0. İlk İngilizce başlık taşması giderildi; ilk görseller `screens-initial` içinde.
- Computer Use: gerçek masaüstü fareyle Oyunlar→Gölet→Yeni tur→Duraklat→Oyunlar; **20→19**, dönüşte ek harcama yok. Ek gerçek GameView çekimi `TR-ComputerUse-Returned19.png`; telefon kabulü değildir.
- Mimari validator 0 hata/0 uyarı. Nihai Console ve kaynak/kayıt koruma sonuçları closure.json içinde.

[Son ekran galerisi](QA/SHARED_LIVES_2026-10-06/gallery.html)

## Kapsam ve koruma

Testler gerçek kaydın ayrı kopyasında yürütüldü; beş gerçek kayıt dosyasının karşılaştırması `preservation-final.json` ile yapılır. Oturum kimliği gibi editör tercihleri geriye sarılmaz; bu tur tüm registry tercihleri için birebir eşitlik iddiası yoktur. Başlangıçta takılan, daha önce de okunamayan özgün Eat animasyon klibi taramadan çıkarıldı; okunmuş veya doğrulanmış sayılmaz.

Oda/model/kamera/ışık/tırmalama ve oyun puanlama kuralları değişmedi. Yeni kaynaklar, yardımcı testler ve arşivlenen geçici sahneler manifestte listelenir. Gerçek kayıtlar yeni şemaya ancak kullanıcı normal oyunu çalıştırdığında geçer. APK/commit/push/yayın yapılmadı; çevrimiçi iki yeni tablo hâlâ yayımlanmış değildir.

Nihai koruma: 8483 okunabilir başlangıç dosyasından **8472 aynı / 11 mevcut C# değişik / 10 yeni C#-meta / eksik 0**. Beş gerçek kayıt birebir aynı. İki font, HUD prefabı ve EditorSettings yalnız hash doğrulanmış güncel başlangıcına döndü. Bu tur oluşan test/kurtarma sahneleri QA altında arşivlendi. Son normal Play koşulları 55/55 geçti; native PlayMode sayısına eklenmez. Unity DX11 açık, üç temiz normal sahne, tek AudioListener, dil Türkçe; Play/QA/derleme/build/profiler kapalı. Son Console 0 hata/0 uyarı.
