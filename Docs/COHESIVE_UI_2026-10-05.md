# Oyun içi arayüz — ortak malzeme ve renk dili, 5 Ekim 2026

Kullanıcı önceki oda arka planı uygulamasında düğmelerin, kedi platformunun ve içerik yüzeylerinin birbirinden kopuk kaldığını bildirdi. Bu tur arka planı koruyup içerikleri onunla bütünleştirir; önceki turun görsel kabul edildiği iddia edilmez.

## Uygulama

- İçerik kartları ve isim alanları: düşük doygunluklu adaçayı/petrol, sıcak ahşap kenar ve hafif derinlik. Geniş krem-beyaz alanlar kaldırıldı.
- Ana eylemler: aynı kiremit tonunda yüzey, krem yazı ve ince bal/ahşap çerçeve. İkincil eylemler petrol. Basılı, odaklanmış ve devre dışı durumlar da aynı malzemeyi kullanır.
- Kedim: önizleme alanında oda duvarının sakin dokusu, ceviz taban ve bal rengi ahşap platform. Irk kartları adaçayı; önizlenen kart bal rengiyle ayrılır. Mevcut kedinin onay rozeti ayrı kalır. İsim alanı, renk seçimi çerçeveleri ve devam düğmesi ortak stile bağlandı. Gerçek tüy paleti değişmedi.
- Mağaza: 102 gerçek 3D ürün önizlemesi aynı adaçayı fotoğraf zeminiyle yeniden render edildi. Ürün geometrisi, malzemesi, kamera açısı ve kimlikleri değişmedi. Sekmeler, fiyat/ürün yüzeyleri, kaydırma çubuğu ve satın alma alt pencereleri aynı stile uydu.
- Odalar, görev rozetleri, ayar anahtarları, oyun seçimi/sıralama, hamburger menüsü, dönüş ihtiyaç kartları/çubukları, diyalog ve isim alanı ortak renk ve kenarlara geçirildi. Ortak sunum yardımcıları diğer öğretici, kutlama, rehber ve mini oyun pencerelerine de uygulanır.

Kedi platformunun yalnız önizleme malzemeleri değişti; geometrisi, kamera kontrolü ve render bütçesi aynı. Oynanış odası, hareket, etkileşim, ekonomi, kayıt, hesap ve kabul edilmiş tırmalama kuralları değiştirilmedi. Onaylı ana ekranın yerleşimi ve görselleri korundu; alt pencereleri ortak stile dahildir. Bu tur yeni ImageGen, Blender veya Affinity varlığı gerekmedi.

## Kaynaklar ve kanıt

16 mevcut C# dosyası (14 runtime + 2 Editor render yardımcısı) ve 135 mevcut PNG değişti: 102 ürün fotoğrafı ve 33 kedi komutu pozu (10 ırk × 3 poz + 3 ortak yedek). Pozların kadrajı, animasyonu ve kedi malzemeleri korunur; fotoğraf zemini değişir. Yeni runtime dosyası/varlık eklenmedi. Kaynak karşılaştırması `QA/COHESIVE_UI_2026-10-05/source-changes.diff`; görsel ölçüleri/ortak fon ve SHA256 değerleri `catalog-art-manifest.json` ile `companion-art-manifest.json` içinde.

QA kökü: `Docs/QA/COHESIVE_UI_2026-10-05`. Ayrı oyuncu kayıt kopyası kullanıldı. Dönüş, kutlama, sıralama ve mini oyun sonuçlarındaki sayılar sunum örnekleridir; gerçek kazanım değildir. `first/` ara görünümler, `screens/` son ana pencere turudur. İlk kısa görsel tur mağaza kaydırma çubuğu rötuşu için durduruldu; son tur aynı isimleri yeniden üretir.

![Altı ana pencere](QA/COHESIVE_UI_2026-10-05/cohesive-ui-final-overview.png)

[Tam boy GameView galerisi](QA/COHESIVE_UI_2026-10-05/gallery.html)

## Doğrulama ve kapanış

40 EditMode ve 9 PlayMode testi PASS. `EditMode-native.xml` ve `PlayMode-native.xml` mağaza kaydırma rötuşundan sonra üretilmiştir. Kapsam: çerçeveler, yerleşim/çakışma, görev/dönüş sunumu, EventSystem kaydırma, portre ayrımı, dönüş düğmesi ve üç kedi önizleme bütçesi/giriş/temizleme kontrolü. Kedi komutları fotoğraf rötuşundan sonra ilgili `CompanionComfortLayoutTests` yeniden PASS (`Companion-final-native.xml`). Önceki 49 test çalıştırması ayrı tarihsel XML'lerde saklıdır; tekrarlar benzersiz 49 test sayısına eklenmez.

- 40 görünüm × TR/EN × 1920×1080 ve 848×392; ayrıca 8 ana görünüm × TR/EN × 1440×1080 ve 2400×1080 = 192 gerçek GameView. Kedi komutları son fotoğraf rötuşundan sonra 8 koşulda yeniden çekilip galeriye alındı; ilk tam turun metin raporu ayrıca korunur.
- Runner/Catch × açılış/duraklatma/sonuç/öğretici × TR/EN × iki oran = 32 ek görünüm. Bu mini oyun grubunda otomatik TMP taşma taraması yoktur; görsel inceleme ve düğme yerleşimi kontrolü yapıldı. Tur/ödül işlemi başlatılmadı.
- Açıklanamayan düğme sınırı/çakışma/hit bulgusu yok. Menü perdesinin arkasındaki 40 HUD hit bulgusu beklenen engellemedir. Ham bulgular saklanır.
- Metin taramasında `TR-1920-Quests text=Status` bir kez bildirildi. Aynı dil/ölçüde ayrı yeniden açılışta üç görev durum etiketinin taşma/kesilme durumu False çıktı; görüntüde de taşma yok. İlk bulgu silinmedi, görev davranışına düzeltme yapılmış sayılmaz. Kanıt `quest-text-recheck.txt` ve `rechecks/TR-1920-Quests.png`.
- Computer Use: Menü → Kedim → Maine Coon önizlemesi → pembe tüy önizlemesi → kaydırma tutamacıyla listenin başına dön → Kapat geçti. Mevcut Persian/tüy 0 korundu, kapanışta giriş engeli yok. Ekip etme işlemi yapılmadı.
- LevelContentValidator: 0 hata/0 uyarı. Son Console: 0 hata, mini oyun turundan kalan 2 `Parameter 'Hash 0' does not exist` uyarısı. Bu önceden görülen Catch uyarısı düzeltilmiş sayılmaz. Test sahnesi geçişlerinde geçici ses dinleyicisi mesajları görüldü; son normal sahnede tek etkin dinleyici var.

8293 okunabilir başlangıç dosyasından 8142 aynı; yalnız 16 C# ve 135 PNG değişti, yeni/eksik dosya ve son okuma hatası yok. Özgün Eat klibi başlangıçta okunamadığından eksiksiz tüm-varlık hash iddiası yok. Beş gerçek kayıt dosyası ve 16 tercih aynı. Font önbellekleri, CurrencyHud prefabı ve EditorSettings bu turun hash doğrulanmış başlangıcıyla karşılaştırıldı; farklı olanlar yalnız o başlangıca döndü. Sahne/prefab/model/klip/oynanış/kamera/ışık kaynakları aynı.

Unity açık; üç temiz normal sahne, tek etkin ses dinleyicisi. Play/QA/derleme/build/profiler kapalı. Kesin sonuçlar `native-final-manifest.json`, `visual-summary.json`, `preservation-final.json`, `editor-final.txt` ve `closure.json` içinde. Başlangıç 2026-10-05 12:07:26 UTC; kesin kapanış/süre closure esas. Fiziksel telefon/FPS ve kullanıcının bu entegre görünüm için görsel kabulü henüz yok. APK/commit/push/yayın yapılmadı.
