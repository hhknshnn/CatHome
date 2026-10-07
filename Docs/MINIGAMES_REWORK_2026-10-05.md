# Mini oyun revizyonu — 5 Ekim 2026

Kullanıcının dört geri bildirimi uygulandı. Bu teslim, MINIGAMES_COMPLETE belgesindeki altı bölüm/manuel pati/eski gölet tanımlarının yerine geçer.

## Oynanış ve sonuçlar

- **Sonuçlar:** Eve Dönüş, Pati Avı, Yumak Rotası ve Gölet Keyfi için dört sekme; günlük/haftalık/tüm zamanlar. Yeni oyunların tarihli yerel rekorları kayıtta. Bağlantı yoksa kişisel rekor açıkça belirtilir; dünya sırası veya oyuncu sayısı uydurulmaz.
- **Pati Avı:** Ayrı pati düğmesi kaldırıldı. Fareye dokunmak tek sıçrama kuyruğa alır; kedi yerde yaklaşır, öngörülen iniş erişime girince sıçrar. Mevcut yön/gerçek patiyle yakalama kuralları korunur.
- **Yumak Rotası:** 24 bölüm (12 düzen ve döndürülmüş varyasyonları), 180 saniye, bölüm başına üç hamle. Noktalı rota minder/kenar sekmelerini aynı sabit .012 s fizik adımıyla öngörür. Sepet kapısı daraltıldı; yumak .42 saniyelik yayla sepet merkezine yerleşmeden puan verilmez. Final merkez Y=.31, sepet zemini/rimi içinde.
- **Yumak puanı:** Çözülen bölüm için 100 + sıfır tabanlı bölüm * 25 + yıldız * 25 + inci varsa 25. Jeton toplam puanın 25'e bölümü; devam sonunda yalnız yeni kazanılan fark ödenir.
- **Süre/Devam:** Saat sıfırda konum durur. Son bölüm tamamlanmışsa boş devam satılmaz. Diğer durumlarda tur başına bir kez, açık onayla **2 elmas karşılığı +60 saniye**. Bölüm, hamle, puan, yumak/kedi konumu, hız ve eylem aşaması korunur. Ödenmiş devam turu duraklatılıp kapatılırsa ücretsiz sürdürülür. İkinci ücret/tekrarlanan ödül engellenir.
- **Gölet:** Kedi geniş ön iskelede sağa/sola yönlendirilir. Serbest yüzen balık kıyıda erişilebilir olduğunda yeşil halka belirir; balığa dokunmak sıçrama/yakalama/geri dönüş başlatır. Uygun olmayan tıklama seri bozabilir. Altı balık, beşe çıkan seri, kusursuz yakalama, 12 saniyede değişen bonus balık ve görsel bonus sayacı; 75 saniyelik tur. Albümde altı gerçek model portresi.
- **Kayıt:** v13, eski altı bölüm yıldızlarını 24 alana taşır; eski gölet yakalama rekoru veri olarak korunur, yeni puan rekoruyla karıştırılmaz. Devam kaydı derin kopyalanır, geçersiz sayısal değerler reddedilir/sınırlandırılır.

## Doğrulama

Esas kanıt: QA/MINIGAMES_REWORK_2026-10-05/native-final-manifest.json.

**34 EditMode + 16 PlayMode = 50 benzersiz seçili native test PASS.** İlk hatalar tarihsel XML'lerde tutuldu; son seçili testlerde açık FAIL yok. Tüm proje testleri çalıştırıldı denmez. Son EditMode aracının başlatma zaman aşımı yanıtına rağmen Unity XML'i 19:47:02–19:47:04 UTC arasında 34/34 PASS kaydetti; native XML esas. Boş filtreli koşu 0 testtir, başarıya eklenmedi.

- 24 bölüm frame-separated pointer down/drag/up girdisiyle tamamlandı; her başarıda top merkez sapması <1 mm ve Y=.31; 72 yıldız, tek ödeme. Geometri turunda 600 saniyelik QA saati ve 2× zaman kullanıldı; standart sürenin oynanabilirliği bu testle ölçülmez.
- 180 s başlangıç; timeout, konum donması, yetersiz bakiye, Vazgeç, çift onay, tam 2 elmas/60 s, çıkıp yeniden ücretsiz sürdürme, ikinci ücretin engellenmesi ve tek ödül.
- Gölet: pointer ile hizalanma, altı erişilebilir balık, çift tıklamada tek yakalama, doğal 75 s bitişi (2× test zamanı), duraklamada saat/balıkların durması, puan dökümü ve tek ödül.
- Dört sonuç sekmesi ayrı oyunu/skoru gösterdi; kişisel sonuçta sahte dünya sırası yok.
- Pati: tek fare tıklaması, tek sıçrama/tek yakalama; yerde yaklaşma, havada yönü koruma; pause/oyun sonu/çıkış güvenliği.
- Computer Use ile Ava başla ve turuncu fareye **bir** masaüstü tıklaması: 1 sıçrama/1 yakalama/120 puan. Araç gecikmesi için hedef seçilirken QA zaman ölçeği 0, takip/sıçrama 1 idi; kesintisiz gerçek zamanlı telefon testi değildir.
- Son 90 GameView sunumu: TR/EN × 3 oran (1920×1080, 1440×1080, 2340×1080); görünür metin taşması/düğme çakışması/ekran dışı/tıklanamaz merkez 0. İlk devam onayında alttaki sonuç metni görünüyordu, düzeltildi; ilk görüntüler screens-initial içinde. İki ek gerçek oynanış çekimi: sepette yumak ve masaüstü av sonucu.
- Mimari validator 0 hata/0 uyarı, son Console 0 hata/0 uyarı.

[Gerçek ekran galerisi](QA/MINIGAMES_REWORK_2026-10-05/gallery.html)

## Çevrimiçi kapsam

Cloud Code SubmitCozyScore ve yeni ödül-board kimlikleri hazır. Tam modül mevcut yerel SDK paketleriyle 0 hata/0 uyarı derlendi; 7 pozitif/ret puan sözleşmesi kontrolü geçti. **Yeni iki oyunun dünya tabloları oluşturulmadı, bulut modülü yayımlanmadı; canlı çevrimiçi kabul yok.** Altı tablo ve sözleşme Server/COZY_COMPETITION_2026-10-05.md içinde. Metrik sınır doğrulaması sunucu tarafında oynanış tekrarını/anti-cheat garantisini temsil etmez.

## Koruma ve kapanış

Başlangıç: 2026-10-05T19:06:28.995445Z. Gerçek kapanış closure.json içindedir. 8463 okunabilir başlangıç dosyasından **8444 aynı / 19 kapsam içi değişik / 20 yeni kaynak-meta / eksik 0**. Bir özgün Eat klibi başlangıçta okunamadı. Server ayrıca bir kaynak değişikliği ve bir yeni belge olarak server-final.json ile izlenir.

**Beş gerçek kayıt dosyası birebir aynı.** 33 typed registry tercihi alındı: 29 aynı; dört Unity editor/play/connection oturum kimliği veya sayacı doğal olarak değişti, geri sarılmadı. Oyuncu ayarı değişikliği yok; 33/33 aynı denmez. İki font, HUD prefabının geçici sayaç/ölçüleri, üç banyo malzemesi ve EditorSettings yalnız bu turun hash doğrulanmış başlangıcına döndürüldü.

Ev odaları, kabul edilmiş tırmalama, kamera/ışık/model/klipler korunur. Üç temiz normal sahne ve tek etkin AudioListener. Unity açık; Play/QA/derleme/build/profiler kapalı. Fiziksel telefon, FPS/ısınma ve kullanıcı oynanış/görsel kabulü yok. APK/commit/push/yayın yapılmadı. Tur kapandı; yeni iş kendiliğinden başlamaz.

