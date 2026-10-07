# A — Yumuşak cam, 5 Ekim 2026

Kullanıcının “evet A ile devam edelim” onayıyla seçilen mockup oyun içi pencere sistemine uygulandı. Önceki resimli oda/ahşap yüzeylerin yerine gerçek salonu gösteren yarı saydam petrol camı, ince nane kenarlar, krem metin ve mercan ana eylemler kullanılıyor. Referans `../../ArtSource/UI/SoftGlass_20261005/approved-mockup-A.png`; entegre son görünümün kullanıcı kabulü ayrıca bekler.

## Uygulama

- Hamburger, mağaza ve alt onaylar, görevler, Kedim, Odalar, Ayarlar, gizlilik, oyunlar, sıralama, rehber/komutlar, kutlamalar, diyaloglar, dönüş ve mini oyun pencereleri ortak yüzeylerden beslenir. Ana ekranın onaylanmış dikey sol menü yerleşimi korunur.
- Kartlar ve alanlar aynı cam ailesinde; seçili kart nane tonuyla ayrılır. Mercan düğmeler yuvarlak, ince kenarlı ve ölçülü parlaktır. Şeffaf çerçeve gerçek halka geometrisidir; merkeze ek opak yüzey çizmez.
- Kedi önizlemesindeki ahşap platform yerine mint minder geometrisi var. Gerçek kedi modeli, rig, poz ve render bütçesi korunur; önizleme kadrajı minderin tamamını kapsar. Tüy seçenekleri gerçek renk değerlerini korur.
- 103 ürün görseli ve 33 kedi komut pozu alfa kanalıyla yeniden render edildi. Beyaz/düz fotoğraf zeminleri kaldırıldı. Eski prefabın hâlâ kullandığı NapPillow görseli de dahil; kataloğun sahiplik/ürün davranışı değiştirilmedi. Duş ve retro TV kenarda kesilmemesi için biraz geniş kadrajlandı.
- Mevcut modal arka plan yakalaması açılış başına bir kare alır. Sürekli yeni render döngüsü eklenmedi. Dönüşte eski yenilemenin yeniden koyduğu koyu metinler sunum katmanından düzeltilir.

## Doğrulama

Son kaynaklarla 43 EditMode ve 9 PlayMode testi PASS: 52 benzersiz kontrol. Panel geometrisi, sunum, davranış, çakışma, görev ilerlemesi, dönüş, önizleme bütçesi, komut yerleşimi ve popup kaydırma/girdi kontrolleri kapsandı. İlk koşular saklıdır; esas `EditMode-final-native.xml` ve `PlayMode-final-native.xml`.

192 ana/alt pencere görünümü: TR/EN, 1920×1080 ve 848×392 tam tur; sekiz temel pencere ayrıca 1440×1080 ve 2400×1080. Açıklanamayan yerleşim bulgusu ve metin taşması sıfır. Menü arkasındaki 40 engelli HUD hit'i kasıtlıdır. Runner/Catch için TR/EN iki boyutta 32 açılış/duraklatma/sonuç/öğretici görünümü incelendi; mini ekranlarda ayrı otomatik TMP taraması yapılmadı. 136 görselin alfa içeriği boş değil; dış kenara dayanan içerik bulunmadı.

Computer Use ile gerçek Game View: hamburger → Kedim → Maine Coon önizle → listeyi başa sürükle → Kapat geçti. Önizleme kaydedilmedi; salondaki Persian kaldı, giriş engeli sıfıra döndü. Ödül/sıralama/dönüş ve mini sonuç görüntüleri QA sunum örnekleridir.

LevelContentValidator: 0 hata, 0 uyarı. Son konsolda varlık yenilemesi sırasında bir MCP WebSocket uyarısı görüldü; bağlantı toparlandı. Tarihsel Catch animasyon uyarılarına bu işte müdahale edilmedi. Telefon/FPS ölçümü yapılmadı.

## Koruma ve kapanış

Başlangıç 2026-10-05T13:29:05.700927Z. Kesin kapanış ve süre `QA/SOFT_GLASS_2026-10-05/closure.json` içindedir. 8293 okunabilir başlangıç dosyasından 8108 aynı; yalnız 16 C#, 136 PNG ve 33 PNG meta değişti. Unity kapsamındaki yeni/eksik dosya yok. Bir özgün Eat klibi başlangıçta okunamadı; tüm-varlık hash garantisi verilmez.

5 gerçek kayıt dosyası ve 16 tercih aynı. Font önbellekleri, CurrencyHud prefabı ve EditorSettings yalnız bu turun hash doğrulanmış başlangıcına döndü. Oynanış salonu, sahneler, kedi hareketi/tırmalama, kamera, ışık, ekonomi ve kayıt kuralları aynı. Unity açık; 3 temiz sahne, tek etkin ses dinleyicisi; Play/QA/derleme/build/profiler kapalı. APK, commit, push veya yayın yapılmadı.

Görsel galeri: [gallery.html](QA/SOFT_GLASS_2026-10-05/gallery.html). Altı temel pencere: [soft-glass-final-overview.png](QA/SOFT_GLASS_2026-10-05/soft-glass-final-overview.png). Esas kanıt: `native-final-manifest.json`, `preservation-final.json`, `editor-final.txt`, `transparent-art-audit.json`, `source-changes.diff`. `first-tour` ilk denemelerdir; final kanıtı değildir.
