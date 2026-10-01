# Cat Home — canlı masalsı görsel yenileme

**Güncel:** Son HUD referansı sonrası kullanıcı uygulama izni verdi. HUD uygulandı; teknik sonuç ve sınırlar `../../HUD_STORYBOOK_2026-09-22.md`. Aşağıdaki referans onayı bekleme notları tarihseldir.

**Son adım — oyun içi HUD referansı:** Kullanıcı ana menüden sonra sıradaki adımı sordu ve önce HUD referansı hazırlama önerisine “devam et” dedi. [07-hud-reference.png](07-hud-reference.png) yerleşik ImageGen ile hazırlandı; [üretim istemi ve sınırlar](PROMPT_HUD_REFERENCE.md). Bu adımda Unity/kod/prefab/sahne/kayıt değişikliği yapılmadı. HUD uygulaması için bu yeni görselin değerlendirmesi beklenir; önceki ana menü uygulaması yerinde kalır. Üç ihtiyaç, jeton/elmas ve ayrı bağ puanı, mevcut dört alt bağlantı ve bağlamsal eylem korunur. Görseldeki 0% ve bakiyeler eski ekran örneğidir; gerçek kayıt durumu değiştirilmedi. Oda/kedi görseli üretimde yeniden çizilebildiğinden piksel düzeyinde aynılık iddia edilmez; uygulama kapsamı yalnız UI'dır.

22 Eylül 2026. Güncel durum: **yalnız ana menü uygulandı**. [Uygulama raporu](../../MAIN_MENU_STORYBOOK_2026-09-22.md). Kullanıcı referans sonrası “devam et” dedi ve denemeyi toplam 30 dakika ile sınırlandırdı. Başlangıç 11:22:51 UTC, son sınır 11:52:51 UTC. Diğer UI ekranları tasarım aşamasındadır; bütün oyun yenilenmiş değildir.

Son geri bildirim: kullanıcı tıklanabilir buton/hareket taslağını beğendi; patili altın jeton yerine üretilen krem/turkuaz kedi başını da “tamam bu olur” diyerek onayladı. Kapsamı yalnız UI tasarımı ve UI animasyonları olarak netleştirdi. Kedi hareketleri, modeller ve oda/dünya görünümü değişmeyecek.

**Güncel görev sınırı:** [Ana menü referansı](06-main-menu-reference.png) sonrası verilen devam izniyle yalnız ana menü uygulandı. [Gerçek Unity görüntüsü](../../QA/MAIN_MENU_STORYBOOK_2026-09-22/screens-final/main-menu-final.png). Yeni uygulamanın görünümü kullanıcı değerlendirmesine hazır; henüz son görsel onay alınmadı. Başka ekranlara yeni talep olmadan geçilmeyecek. [Üretim komutu ve uygulama sınırları](PROMPT_MAIN_MENU.md) referans üretiminin tarihsel kaydıdır.

## Kullanıcının istediği sonuç

Mevcut UI görünümü yeterince premium bulunmuyor. Kullanıcı canlı, masalsı, güçlü renkli ve belirgin çizgi film tarzını seçti. Uzun vadeli istek ana ekranlar, butonlar ve UI animasyonlarıdır; bu tur yalnız ana menü uygulandı. Önceki ilk-oturum/günlük-görev geliştirmesi bu görsel işin arkasına alındı.

Animasyon kapsamı netleşti: kullanıcı “kedi hareketi değişmemeli ... sadece UI tasarımı” dedi. Kedi yürüyüş/sıçrama/eşya hareketleri kapsam dışıdır.

## Taslaklar

- [Açılış, salon, mağaza ve buton ailesi](01-direction-draft.png)
- [Yeniden tasarlanan oyun içi arayüz](02-home-direction.png)
- [Buton ve HUD işçiliği revizyonu](03-ui-craft.png)
- [İkon ailesi çalışması](04-icon-study.png)
- [Modern kedi simgesi alternatifi](05-modern-cat-icon.png)
- [Ana menü referansı — uygulamaya geçiş onaylandı](06-main-menu-reference.png)
- [Oyun içi HUD referansı — değerlendirme bekliyor](07-hud-reference.png)

Üretim komutları: [ilk pano](PROMPTS.md), [oyun içi yön](PROMPT_HOME.md), [buton/HUD revizyonu](PROMPT_CRAFT.md), [ikon ailesi](PROMPT_ICONS.md).

Yeni simge: [üretim komutu](PROMPT_MODERN_CAT.md). Önceki görsellerdeki patili altın jeton tarihseldir ve kullanıcı tarafından beğenilmemiştir. Güncel tıklanabilir örnekte Devam et, Kedim ve açılan panelde yeni kedi simgesi kullanılır. Bu değişiklik oyunun para birimi tasarımına uygulanmaz.

Görseller yerleşik image_gen ile üretilmiş tasarım örnekleridir. Çalışır Unity ekranı, uygulanmış karakter modeli veya cihaz performansı kanıtı değildir. İkinci taslak ilk panodaki oyun HUD'ından daha belirgin bir değişim hedefler. Üçüncü taslak, beğenilen oda ve ışık yönünü koruyarak buton/HUD biçimini geliştirir.

Sohbetteki tıklanabilir örnek; sabit basış alanı üzerinde yüzeyin çökmesini, tek kısa bırakma tepkisini, kalıcı sekme seçimini, panel açılmasını/kapanmasını ve azaltılmış hareketi gösterir. Gerçek oyuna veya kayda bağlı değildir. İkon çalışması kaynak PNG olarak saklanır; önizleme için boyutları ve saydamlığı koruyan WebP kopyası kullanılır. Bu çalışma kanonik jeton veya mevcut ürün modellerini otomatik olarak değiştirmez.

Görsellerdeki yuvarlatılmış kedi yüzü/kenarları mevcut modelin birebir karşılığı değildir. Mağaza kategori adları ve bazı simgeler temsilîdir; mevcut gerçek işlevler kullanılır. İhtiyaç çubuğu dolulukları gerçek oyun durumu olarak alınmaz. UI örneği otomatik olarak yeni kamera, kedi ölçeği, mobilya düzeni veya fiyat kararı oluşturmaz.

## Görsel kararlar

1. Koyu indigo, turkuaz ve mercan ana renk ailesi. Sıcak krem içerik yüzeyi, altın yalnız ödül/jeton gibi seçili vurgularda.
2. Açılışta kedi sahnesi ve güçlü başlık; tek belirgin Devam et eylemi, ikincil seçenekler daha sakin.
3. Oyunda kimlik/ihtiyaçlar tek okunaklı küme, cüzdan ayrı; alt gezinme oyunla uyumlu boyutlu bir yüzey. Gerçek dokunma alanları ve SafeArea ölçülür.
4. Mağazada ürün görseli baskın, renkli sergileme alanı ve belirgin kategori seçimi. Ürün kimlikleri ve gerçek satın alma durumları korunur.
5. UI renkleri, yüzeyleri ve ikonlarında çizgi film hissi. Oda ışığı, malzemeler veya bloom değiştirilmez.

## Buton kalite revizyonu

- Ortak üst-sol ışığı, okunaklı yan yüz ve kısa temas gölgesi; her yüzeye aynı parıltı uygulanmaz.
- Indigo ikincil yüzey, mercan ana eylem/seçim, turkuaz küçük seçim işareti. Bilgi yüzeyleri eylemlerden daha sakin kalır.
- İkonlar ortak perspektif ve ışıkta; yazıların optik merkezi, kenar boşluğu ve hizası tutarlı.
- Seçili durum, fare üzerine gelme ve klavye odağından bağımsız. Kullanılamayan buton okunaklı fakat hareketsizdir.
- Basış hedefi sabit kalır; içerideki görünen yüzey yaklaşık 70 ms'de çöker, 130–180 ms'de tek küçük toparlanma yapar. Sürekli sekme/parlama yoktur.
- Genel kalite, tüm ekranların aynı renk/biçim/ikon/hareket kurallarını paylaşmasıyla doğrulanır. Tek güzel ekran bütün oyunun tamamlandığı anlamına gelmez.

## Ekran kapsamı

| Aile | Kapsanan yüzeyler |
|---|---|
| Açılış | Ana menü, yükleme, isim, tanışma, geri dönüş |
| Ev | HUD, bağlamsal eylemler, konuşma/düşünce, joystick, alt gezinme |
| İçerik | Mağaza, ürün ayrıntısı, oda seçimi, kedim, ırklar, komutlar, rehber |
| İlerleme | Bölüm/günlük görev, seviye, koleksiyon, başarı ve bildirim |
| Oyunlar | Oyun merkezi, Runner/Catch giriş, HUD, duraklatma, sonuç, can durumları |
| Sistem | Ayarlar, hesap, gizlilik, sıralama, elmas, onay ve hata durumları |

## Arayüz hareket hedefi

Bu değerler tasarım hedefidir; uygulanmış sonuç değildir.

| Eylem | Hedef |
|---|---|
| Butona basma | Yaklaşık 70–90 ms kontrollü çökme, 130–180 ms kısa toparlanma |
| Panel açılma | 200–260 ms küçük mesafe + saydamlık; içerik okunmadan tekrar zıplamaz |
| Panel kapanma | 140–180 ms; giriş kilidi doğru anda bırakılır |
| Sekme seçme | 120–160 ms net yüzey/konum tepkisi |
| Küçük ödül | Kısa yükselme, tek vurgu, düzenli kaybolma |
| Büyük kutlama | 500–700 ms giriş; sonra sakin okunaklı durum |
| Azaltılmış hareket | Mekânsal hareket ve parçacıklar azaltılır; işlemler aynı çalışır |

## Teknik bulgular

- Arayüz üretiminde ReferenceArt, Joyful, Modern ve Playful katmanları art arda uygulanıyor. Modern bazı motifleri kapatıp soğuk beyaz/mavi düzeni dayatıyor; Playful belirli ekranlara yeniden renk veriyor. Yeni görünümün son stil kuralları tek yerde belirlenmeli; yalnız sahne üstünden boyamak yeniden üretimde kaybolabilir.
- Çalışırken oluşturulan kutlamalar ve ürün/oda durumları da aynı görsel kuralları kullanmalı. Sadece editörde duran ekranların yenilenmesi bütün kapsamı karşılamaz.
- Panel hareketleri farklı sınıflarda. Ortak süre/eğri kararları mevcut açılma/kapanma ve girdi kilitleriyle uyumlu uygulanmalı.
- Işık denetçisi güncel ışık renklerini çalışma sırasında sabit değerlere geri yazıyor. Kalıcı görsel iyileştirme bu kaynakla tutarlı olmalı.
- Mobil render ölçeği .85, gölge çözünürlüğü 1024; ek ışık gölgeleri kapalı. Taslak görüntü telefon kalitesi garantisi değildir.
- Mevcut model/iskelet biçimini değiştirmek; malzeme, renk ve ışık işinden ayrı bir uygulama ve temas doğrulaması gerektirir.

## Uygulama sırası ve süre

İlk inceleme başlangıcı 08:27:45 UTC / 11:27:45 Türkiye. Aynı çalışma için mutlak üst sınır 11:27:45 UTC / 14:27:45 Türkiye. İnceleme ve taslak süresi bu toplamın içindedir; kendiliğinden yeni tur açılmaz.

1. Yalnız ana menü referansını kullanıcıya sun; onay bekle.
2. Kullanıcı onaylarsa yalnız ana menüde renk/yüzey/buton/yerleşim uygulaması yap ve gerçek süreyi ölç.
3. Gerçek kayıt ve tercih koruması, dar/geniş ekranlar, girdi ve ilgili ana menü akışlarını kontrol et.
4. Ana menü sonucunu teslim et. Diğer ekranlar sonraki ayrı kullanıcı talimatına bağlıdır.

Önceki bütün UI için 8–12 saat tahmini ölçülmüş bir süre değildir; kullanıcıyla görüşmede fazla temkinli olduğu açıklandı. Toplam süreye yeni kesin söz verilmez; önce onaylı ana menünün gerçek süresi ölçülür. Üç saat çalışma sınırı sürer. Bu inceleme sonunda oyun dosyaları, sahneler, kayıtlar veya tercihler değiştirilmemiştir.
