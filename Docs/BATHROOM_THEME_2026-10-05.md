# Banyo — salonun devamı, 5 Ekim 2026

Kullanıcının, A pencere tasarımı bittikten sonra sıradaki ilk odaya aynı kamera ve genel temayla devam etme talebi uygulandı. Oda sırasındaki ilk oda banyodur; bu tur yalnız banyoyu kapsar. Son görsel kabulü kullanıcıya aittir.

## Görsel değişiklik

- Banyo, salonun `(0, 2.7, -6.5)` kamera konumunu, `18°` açısını ve `36°` temel görüş alanını kullanır. Tablet oranında mevcut uyarlama `47.2943°` olur. Katalog fotoğrafı HUD içermeyen mevcut `42°` kadrajı kullanır.
- Adaçayı üst duvarlar, petrol paneller, sıcak ve düşük kontrastlı taş zemin; nane seramik, krem porselen, mercan kumaş ve ölçülü pirinç ayrıntılar ortak bir palete bağlandı. Eski renkli duvar şeritleri ve dekoratif baloncuklar görünmez yapıldı.
- Sağ duvara küçük bahçe manzarası, pencere bitkisi, arka duvara raf bitkisi ve botanik resim eklendi. Yeni dekorların çarpışma yüzeyi veya ışığı yok.
- Klozet X `3.29118 → 2.86`, kum kabı X `-3.17891 → -2.40`: alçalan kamerada ön eşyalar kadraja alındı. Ürünlerle birlikte mevcut yaklaşma/temas noktaları taşındı. Diğer sekiz ürünün katalog konumu aynı.
- On ürünün prefabında yalnız malzeme referansları değişti; geometri, ölçek, collider ve etkinlik verisi aynı. On alfa ürün görseli ve iki banyo oda/mağaza fotoğrafı yeniden üretildi.
- Oda ve ürün kurucuları yeni temayı tekrar üretir. Eski genel malzeme geçişinin yeni banyo yüzeylerini yeniden renklendirmesi önlendi; fotoğraf ve yerleştirilmiş ürün aynı malzemeleri kullanıyor.
- Yerleşim doğrulaması ilgili odanın gerçek kamera profilini kullanır. Zaten dekor olan banyo aynası, planlayıcıda da dekor olarak tanınır. Fiziksel kabul eşikleri gevşetilmedi.

## Doğrulama ve açık sonuçlar

**Bütün testler geçmiş değildir.** Toplam 62 benzersiz native testin 54'ü PASS, 8'i FAIL olarak saklandı.

- İlk EditMode grubu: **47/48 PASS**. Salon koltuğunun BoxCollider trigger beklentisi eski testte FAIL. Salon sahnesi ve test kaynak dosyası bu turun başlangıcıyla byte olarak aynı; başlangıç sahnesinde de ilgili collider `m_IsTrigger: 1`. Bu kabul edilmiş salon davranışı değiştirilmedi.
- İlk PlayMode grubu: **7/13 PASS**. Kamera, geçiş, duş ve aynanın dekor olması geçti. Beş eski test sabit hazır olmayan konumdan başlatma aşamasında kaldı. Bir eski test, değişmemiş `MatKneadActivity.UsesGentleKneading` davranışının tersini bekliyor. Eski fixture'ların tamamı başlangıç kaynaklarıyla yeniden koşturulmadı; hepsine kesin eski hata etiketi verilmez.
- Güncel HUD düğmeleriyle **Persian 9/9, Maine Coon 7/9** tam etkileşim tamamladı. Hazırlıkta fiziksel olarak uygun duruş aranır; ardından gerçek düğmenin pointer-click akışı, tek tamamlanma ve hareket kilidinin bırakılması ölçülür. Duruşlar arasındaki test yerleştirmeleri gerçek joystick yaklaşması sayılmaz.
- Maine Coon klozette uygun başlangıç bulamadı, küvette görünen düğme başlatmadı. Aynı test, hash doğrulanmış başlangıç **banyo sahnesi ve yerleşim kataloğu** geçici olarak kullanılarak tekrarlandı: aynı iki FAIL ve aynı 16 başarı. Karşılaştırmada güncel kamera kodu ve geometri verisi başlangıçla aynı olan ürün prefabları kullanıldı. Sorunlar yeni oda düzenine özgü görünmüyor; **çözülmüş sayılmaz**.
- Son yerleşimle tekrar: iki boyut etkileşim testindeki aynı iki açık durum; iki kamera testi ve oda geçiş testi PASS. Son malzeme birleştirmesi geometri/etkinlik verisini değiştirmedi; ayrıca 10 ürün fotoğraf/sahne malzemesi ve 10 malzemenin yeniden üretimde korunması doğrudan doğrulandı.
- `LevelContentValidator`: **0 hata / 0 uyarı**. Banyo yerleşim doğrulaması temiz. On ürün PNG'sinde alfa içeriği ve kenar kontrolü temiz.

Gerçek Game View: mevcut dört ürün, tam 10 ürün; tam oda `1920×1080`, `848×392`, `1440×1080`, `2400×1080` oranlarında incelendi. Son yerleşim taramasında dışarı taşan, çakışan veya erişilemeyen görünür düğme bulgusu yok. Odalar ve banyo mağazası görüntüleri de saklandı.

Computer Use: hamburger → Odalar → Kapat geçildi; banyoya dönüşte giriş/fiziksel kilitleri kapalı. Tek kısa joystick sürüklemesinde ölçülebilir yer değişimi olmadı; joystick gezinme kabulü verilmez. Telefon ve FPS ölçülmedi. Son konsolda varlık yenilemesi sırasında bir MCP WebSocket uyarısı vardı; bağlantı toparlandı, derleme hatası yok.

## Koruma ve teslim

8293 okunabilir başlangıç dosyasından **8258 aynı**. Kapsam içi 35 dosya değişti: 11 mevcut C#, 12 PNG, 10 banyo prefabı, yalnız banyo sahnesi ve yerleşim kataloğu. Yeni 23 dosya: bir Editor yardımcı ve metası, 10 malzeme ve metaları, klasör metası. Başlangıçtaki bir Eat klibi okunamadı; bütün varlıklar hash'lendi iddiası yok.

Beş gerçek kayıt ve 16 tercih aynı. Salon ve diğer oda sahneleri, bütün hareket/etkileşim runtime kodu, model/animasyon dosyaları korundu. Banyonun beş ışık/RenderSettings YAML bloğu aynı. Font, CurrencyHud ve EditorSettings yalnız bu turun doğrulanmış başlangıcına döndü. Test üretiminden kalan 20 referanssız malzeme QA içine arşivlendi.

Unity açık; üç temiz sahne ve tek etkin ses dinleyicisi. Play, QA, derleme, build ve profiler kapalı. APK, commit, push, yayın yapılmadı. Bu tur kapandı; mutfak veya başka oda kendiliğinden başlatılmaz.

Başlangıç/kapanış ve süre için `QA/BATHROOM_THEME_2026-10-05/closure.json` esas. Görsel görev tamamlandı; mevcut iki Maine Coon sorunu ve eski test FAIL'leri açık.

[Görsel galeri](QA/BATHROOM_THEME_2026-10-05/gallery.html) · [Önce / sonra](QA/BATHROOM_THEME_2026-10-05/before-after.png) · [Son tam oda](QA/BATHROOM_THEME_2026-10-05/final-full-1920x1080.png) · [Mevcut dört ürün](QA/BATHROOM_THEME_2026-10-05/final-current.png)

Esas QA: `native-final-manifest.json`, `PlayMode-final-actions.xml`, `PlayMode-baseline-comparison.xml`, `review-Bathroom_TwoSizes_RealButtonsComplete.json`, `baseline-actions.json`, `material-consistency.txt`, `protected-data-audit.json`, `layout-diff.json`, `legacy-living-test-baseline.json`, `preservation-final.json`, `editor-final.txt`, `source-changes.diff`. Tam koleksiyon yalnız kopya QA kayıtlarında açıldı; ekran sayaçları gerçek ilerleme kanıtı değildir.
