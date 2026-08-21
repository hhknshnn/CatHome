# Cat Home Roadmap

Son güncelleme: 21 Ağustos 2026

## Ürün yönü

Cat Home yalnızca bir sanal kedi bakım oyunu değildir. Bakım, kısa süreli arcade oynanış ve ev geliştirme tek bir tekrar oynanabilir döngü oluşturur.

Ana döngü:

> Kediye bak → Cat Runner oyna → Coin kazan → Evi geliştir → Home XP kazan → Yeni alan ve içerik aç → Tekrar oyna

İlk planlanan mini oyun Cat Runner'dır. MVP kapsamı sonradan ikinci bir mini oyunla (Cat Catch) genişletildi; ikisi de Games hub üzerinden açılır. Yeni mini oyunlar bundan sonra ayrıca değerlendirilecektir.

## Kalıcı progression kararları

| Kaynak | Rolü | MVP durumu |
| --- | --- | --- |
| Coin | Cat Runner'dan kazanılır; mobilya, oyuncak ve dekorasyon için harcanır. | Aktif |
| Bond | Kediyle ilişkinin gücünü temsil eder; etkileşimler, davranışlar ve bakım bonusları açar. | Aktif |
| Home XP | Ev geliştirmelerinden kazanılır; Home Level ve yeni oda/alan açılımlarını belirler. Harcanmaz. | Aktif |
| Diamond | Nadir/premium kaynak. Runner'ın normal ödülü değildir. 7. giriş günü, Home Level 5 ve Bond 250 achievement'larından 1 gelir; IAP paketleri yalnız platform doğrulaması sonrası ödül verir. | Aktif (nadir + IAP seam) |
| Runner Enerji | Koşuya girişte harcanır; en fazla 5'tir ve 10 dakikada 1 yenilenir. | Aktif |
| Player Level | Ayrı bir progression kaynağı değildir. Save alanı yalnız quest chapter migration içindir. | Emekli |

### Player Level emeklilik kuralı

- Yeni özellikler Player Level'a bağlanmayacaktır.
- Mevcut görev grupları oyuncu seviyesi yerine görev bölümü/chapter olarak ele alınacaktır.
- Ev içi etkileşimler Bond, satın alma veya Home Level koşulu kullanacaktır.
- Oda açılımları yalnızca Home Level tarafından yönetilecektir.
- Eski kayıtları bozmamak için `playerLevel` kayıt alanı geçici olarak legacy/migration alanı şeklinde tutulabilir; gameplay tarafından okunmayacaktır.
- Geçiş tamamlandıktan ve eski kayıt testi yazıldıktan sonra alan tamamen kaldırılabilir.

## Sistemlerin oyundaki görevleri

- Bakım: Kedinin ihtiyaçlarını ve mood durumunu iyileştirir; Runner kazancına pozitif bonus verir.
- Ev içi aktiviteler: Kediyle kısa etkileşim sağlar ve Bond progression üretir.
- Cat Runner: Coin'in ana ve tekrarlanabilir kaynağıdır.
- Ev geliştirme: Coin'in ana harcama alanıdır ve Home XP üretir.
- Home Level: Yeni oda, katalog ve büyük içerik açılımlarını yönetir.

Kedi kötü durumdayken Runner kilitlenmez. İyi bakım cezayı kaldırmak yerine ödül kazandırır.

## Mevcut durum

- [x] Bootstrap, ortak UI ve level sahneleri ayrıldı.
- [x] Sahne referans bağlama ve mimari doğrulama kuruldu.
- [x] Bakım, tutorial, görev ve kayıt sistemlerinin temel akışı çalışıyor.
- [x] Coin/Diamond cüzdan altyapısı ve güvenli ödül işlemleri mevcut.
- [x] Top, tırmalama ve fare ev içi aktiviteleri eklendi.
- [x] Kilitli ev içeriği açılana kadar tamamen gizleniyor.
- [x] EditMode ve PlayMode regresyon testleri mevcut.
- [x] Player Level gameplay bağımlılıkları kaldırıldı; eski kayıt alanı yalnızca migration için korunuyor.
- [x] Cat Runner'ın ilk oynanabilir dikey kesiti üretildi ve ana eve bağlandı.
- [x] Runner Enerji sistemi eklendi: 5 maksimum, 10 dakika yenilenme ve çevrimdışı dolum.
- [x] Koşu başına üç çarpışma hakkı ve üçüncü çarpışmada Run Over akışı eklendi.
- [x] Ödüllü reklam için +2 Enerji ve yedi günlük sınırsız Enerji entegrasyon noktaları hazırlandı.
- [x] Home Store eklendi; top sepeti (300 Coin) ve tırmalama tahtası (400 Coin) satın alındığında odada kalıcı olarak açılıyor.
- [x] Satın alınan ev ürünleri odanın geçerli zemin alanında sürüklenebiliyor; duvar, koltuk, masa, mama alanı ve diğer eşyalarla çakışan konumlar engelleniyor; konum ve yön kaydediliyor.
- [x] Top aktivitesi sevme animasyonu/kalplerden ayrıldı; top sekmesi ve hedefe bakış iyileştirildi; mevcut kedi iskeletine özel pati vurma, pounce ve döngülü tırmalama animasyonları eklendi.
- [x] Home Store beş ziyaret edilebilir alana genişledi: Living Room, Bathroom, Kitchen, Bedroom, Garden (her biri 10 parçalık ROOM koleksiyonu) ve `RoomSelectorPanel` oda navigasyonu.
- [x] İkinci mini oyun Cat Catch ve iki oyunu barındıran Games hub eklendi.
- [x] IAP elmas paket kataloğu (10/20/50/100/500/1000) ve doğrulanmış satın alma seam'i hazırlandı; gerçek tahsilat yalnız platform doğrulaması sonrası ödül verir.
- [x] Living Room penceresi + gün/gece `WindowSystem`; premium pastel duvar/ışık geçişi.
- [x] Home XP ve Home Level eklendi: Home XP ev geliştirmelerinden (Home Store satın alımı) kazanılır, Home Level XP'den türetilir. Save şema **v11** (Home XP + daily + achievement). Oda ve ürün açma koşulları Home Level ile yönetiliyor.
- [x] HOME LV. rozeti HUD + SHOP'ta; level-up kutlaması (blur, 500 coin, reklam ×2).
- [x] Cat Runner 9 scenery silüeti + anti-tekrar; Garden avlusu oda açılınca havuza girer.
- [x] CAT kataloğu 5 yeni yerleştirilebilir ürünle genişledi.
- [x] Garden açık hava avlusu: çim, çit, açık kapı, uzak yol, ağaç, 2 ziyaretçi kuş, kenar çiçek + arı/kelebek; Home Level 5 / 6000 Coin.
- [x] Bond milestone oyunları: Window Watch (80), Feather Play (150), Bird Watch (250); fare 35 Bond.
- [x] Görevler 5 chapter: bakım → ev oyunu → Home Loop (Runner/Store/Tunnel) → Garden Bond.
- [x] Aşama 5 çekirdeği: günlük giriş, 3 günlük görev, achievement + nadir Diamond, koşu x2 reklam seam (save v11).
- [x] Garden polish (19 Ağustos): pergola kısaldı + post-only collider (altına ürün konur); doğal arka plan (katmanlı tepeler, uzak ağaç sırası, kıvrımlı patika, gölet, çayır çiçekleri); iki kendinden sürüşlü chase oyunu — PLAY YARN (hep açık) + PLAY BALL (Sunny Yarn Ball ürününe kilitli).
- [x] Yayın kalitesi paketi (19 Ağustos): home audio, kedi isim+kürk (`CAT JOURNAL`), `SETTINGS` paneli, title overlay. Save v11 değişmedi (PlayerPrefs / self-bootstrap).
- [x] Kedi idle kişiliği (19 Ağustos): boşta tımar/bakış/esneme/pounce; Bond+needs tonu; uzun boşlukta mırıltı + speech bubble. Hareket kilidi yok.
- [x] Aşama #4 yaşam katmanı (19 Ağustos): koleksiyon tamamlama ödülü (COLLECT-only) + `X OF 84 • COLLECTED`; local notification seam; SFX cilası; title → isim → tur + `SKIP TOUR`.
- [x] Premium title v2 (19 Ağustos): iki sütunlu kart, canlı 3D kedi, HOME LV / jeton / koleksiyon hapları, PLAY/CONTINUE, SETTINGS, CREDITS, masaüstü QUIT.
- [x] Coming Soon oda zinciri oynanabilir hale geldi (20 Ağustos): Balcony LV9, Garden Patio LV10, Second Floor LV12; katalog **84 → 114**, save v11 sabit.
- [x] Rooms QA düzeltmesi (20 Ağustos): oda geçişinden sonra `X`/scrim yeniden etkinleşir; Second Floor gerçek 1280×720 preview kullanır; Fredoka Ellipsis/`✦` Console uyarıları temizlendi.
- [x] Yerel kayıt kurtarma temeli (21 Ağustos): her başarılı save yanında kalıcı
  `.recovery` kopyası; eksik/bozuk ana dosyada otomatik dönüş; daha yeni save
  şemasını eski kopyayla sessizce ezmeme; 4 EditMode testi.
- [x] Onaylı `NEW GAME` + Türkçe/İngilizce yerel dilim (21 Ağustos): ilerleme
  sıfırlanırken Diamonds, satın alma işlem kimlikleri, sınırsız geçiş hakları,
  ses/erişilebilirlik tercihleri ve dil korunur; Main Menu/Settings/onay penceresi
  canlı dil değiştirir.

## Altı aylık geliştirme sırası

### Aşama 0 — Progression sadeleştirme ve Runner sözleşmesi

Hedef: Cat Runner geliştirilmeden önce çakışan progression kurallarını temizlemek.

- Player Level'ı görev, sahne ve aktivite kilitlerinden çıkarmak.
- Mevcut görev seviyelerini `Quest Chapter` mantığına dönüştürmek.
- Living Room'u başlangıçta doğrudan yüklenen ana ev sahnesi yapmak.
- Top sepeti ve tırmalama tahtasını Coin ile satın alınan içerik, fareyi Bond tabanlı içerik yapmak.
- Eski kayıtlar için migration ve geriye uyumluluk testi eklemek.
- Runner sonucu için veri sözleşmesini tanımlamak: süre, mesafe, toplanan coin, çarpışma, bakım bonusu ve toplam ödül.
- Runner ödülünün cüzdana yalnızca bir kez aktarılmasını güvenceye almak.

Tamamlanma ölçütü: Oyunda veya yeni kodda Player Level'a bağlı hiçbir görünür karar kalmaması; eski kayıtların açılabilmesi; mevcut gameplay testlerinin geçmesi.

### Aşama 1 — Cat Runner oynanabilir prototip

Hedef: Runner'ın eğlenceli olup olmadığını en küçük kapsamla kanıtlamak.

- Ana ev ekranında doğrudan Cat Runner'a götüren `PLAY` butonu.
- Ayrı Cat Runner sahnesi.
- 60 saniyelik koşu.
- Arkadan kamera ve otomatik ileri hareket.
- Üç şeritli sağ/sol hareket.
- Zıplama.
- Mobil swipe ve editörde klavye kontrolü.
- Temel koşu ve sendeleme tepkisi.
- Dört okunaklı ev temalı engel: oyuncak blokları, yastık, mama kabı ve kutu.
- Basit coin dizilimleri ve coin toplama geri bildirimi.
- Engeller ve coinler, görsel zemin akışından daha yüksek kapanma hızıyla oyuncuya yaklaşır.
- İlk iki çarpışmada kısa sendeleme/yavaşlama; üçüncü çarpışmada Run Over.
- Çarpışmalar arasında 1,5 saniye dokunulmazlık.
- Koşuya girişte harcanan 5 kapasiteli Runner Enerjisi.
- Run başlangıcı, süre sonu ve tekrar oynama.

Bu aşamada yapılmayacaklar: kayma hareketi, power-up, reklam, bütün odalar, procedural karmaşıklık, premium ekonomi.

Tamamlanma ölçütü: Oyuncunun 60 saniyelik koşuyu bitirip hemen yeniden denemek istemesi; kontrolün mobilde okunaklı ve tepkisel olması.

### Aşama 2 — Ana döngü bağlantısı

Hedef: Runner'ı Cat Home'dan kopuk bir mini oyun olmaktan çıkarmak.

- Koşu sonunda Distance, Coins, Happy Cat Bonus ve Total sonucu.
- Kazanılan coinlerin mevcut cüzdana güvenli aktarımı.
- Eve dönüş akışı.
- İhtiyaç ve mood durumundan basit pozitif Runner bonusu.
- Coin ile alınabilen ilk iki etkileşimli ev ürünü.
- Satın alma sonrası odada kalıcı görsel ve oynanış değişimi.
- Ses, coin parçacığı, hafif kamera hareketi ve hız hissi.

Tamamlanma ölçütü: Bakım → Runner → ödül → evde görsel gelişme döngüsünün baştan sona çalışması.

### Aşama 3 — Runner içeriği ve Home progression

Hedef: Kanıtlanmış ana döngüye derinlik eklemek.

- Coin Magnet, Shield ve x2 Coin power-up'ları.
- Kayma hareketi ve ona uygun engeller.
- Segment tabanlı salon/koridor parkuru ve varyasyonlar.
- Home XP ve Home Level sistemi.
- Mobilya/dekorasyon kataloğu.
- Bir odanın aşamalı görsel dönüşümü.
- Yeni oda açılış kutlaması ve kamera sunumu.

### Aşama 4 — Yeni alanlar ve içerik ölçekleme

Hedef: İçeriği aynı sistemlerle büyütmek.

- [x] İlk yeni oda veya alan (Garden açık avlu).
- [x] Açılan odanın Runner segment havuzuna eklenmesi (`Variant_GardenCourtyard`, Garden kilidi).
- [x] Bond milestone içerikleri (35 fare, 80 pencere, 150 tüy, 250 kuş).
- [x] Daha fazla ev geliştirmesi ve kedi etkileşimi (Window Watch, Feather Play, Bird Watch).
- [x] Görevlerin Coin, Bond ve Home XP döngüsüne göre yeniden dengelenmesi (Home Loop + Garden Bond).

### Aşama 5 — Retention, monetization hazırlığı ve yayın kalitesi

Hedef: Ana döngü kanıtlandıktan sonra uzun süreli kullanım ve yayın hazırlığı.

- [x] Günlük görev ve giriş ödülü (`DailyRetentionService`, save v11).
- [x] Achievement ve milestone ödülleri (`AchievementService`).
- [x] Nadir Diamond kaynakları (7. giriş günü, Home Level 5, Bond 250).
- [x] Koşu sonunda ödüllü reklam ile x2 Coin seam (`GrantDouble`, ayrı transaction id).
- [x] Yayın kalitesi 1–3 (19 Ağustos): ses/ambiyans, kedi kişiselleştirme, ayarlar + başlık ekranı.
- [x] Kedi idle kişiliği (`CatIdleBehavior`, hareket kilidi yok).
- [x] Yaşam katmanı (19 Ağustos): koleksiyon ödülü + bildirim seam + SFX + onboarding skip; premium title v2.
- Performans, mobil cihaz ve uzun oturum testleri. Yerel kayıt kurtarma tamamlandı.
- İçerik dengesi, onboarding güncellemesi ve genel polish.
- [x] Coming Soon odalar: Balkon (Home Level 9), Garden Patio (10), Second Floor (12) — üçü de oynanabilir.

### Aşama 6 — Hesap, bulut kayıt ve rekabet

Hedef: çevrimdışı ana döngüyü hesap duvarına çevirmeden cihazlar arası güvenli kayıt,
Google hesabı bağlantısı ve adil mini oyun rekabeti eklemek.

- [x] Kalıcı yerel kurtarma kopyası ve bozuk/eksik save testleri.
- [x] Onaylı `NEW GAME`; Diamonds, satın alma işlem kimlikleri, ayarlar ve
  doğrulanmış sınırsız geçiş haklarını koruyan sıfırlama sınırı.
- [x] Türkçe + İngilizce dil çekirdeği ve Ayarlar seçimi; Main Menu, Settings ve
  `NEW GAME` yüzeyleri canlı çevrilir.
- [ ] **Yayın öncesi metin dondurma + tam yerelleştirme paketi** — oyun içeriği,
  ekonomi adları, görevler ve bütün oyuncu metinleri kesinleştikten sonra yapılır;
  geliştirme sırasında parça parça çeviriyle zaman kaybedilmez.
  - [ ] Kalan gameplay/HUD/SHOP/Rooms/Runner/Catch/onboarding metinlerini dil
    tablolarına taşı; kod içinde oyuncuya görünen sabit metin bırakma.
  - [ ] Öncelikli Asya paketi: Japonca (`ja-JP`), Korece (`ko-KR`) ve Geleneksel
    Çince (`zh-Hant`, Tayvan/Hong Kong).
  - [ ] Büyüme paketi: Brezilya Portekizcesi (`pt-BR`) ve Latin Amerika
    İspanyolcası (`es-419`).
  - [ ] Oyuncu/pazar verisine göre ikinci dalga: Almanca ve Fransızca; ardından
    gerekirse Tayca, Endonezce ve Vietnamca.
  - [ ] CJK font/fallback atlasları, satır kırma, auto-size, çoğul, sayı/para/tarih
    biçimleri ve her desteklenen dilde 1920×1080 + mobil taşma/çakışma QA.
  - [ ] Her dil için oyun içi metinlerle birlikte Google Play/App Store başlık,
    kısa açıklama, açıklama ve ekran görüntüsü metinlerini yerelleştir.
  - [ ] Basitleştirilmiş Çince/Çin ana karası yayınını yalnız yerel dağıtım,
    işletmeci ve oyun onayı planı ayrıca kabul edilirse ayrı proje olarak ele al.
- [ ] Anonim misafir kimliği; isteğe bağlı Google hesabı/Unity Player Accounts bağı.
- [ ] Cloud Save Player Files + write-lock çakışma seçimi.
- [ ] Runner ve Catch için DAILY / WEEKLY / ALL-TIME tabloları.
- [ ] Cloud Code içinde kanonik skor hesabı ve tekrar kullanılan run/hunt kimliği koruması.
- [ ] Arşivlenmiş dönem sonuçlarından idempotent günlük/haftalık ödül.
- [ ] E-posta/gerçek ad göstermeyen, denetlenen oyun içi takma ad.

Ayrıntılı sözleşme: `Docs/ONLINE_COMPETITION_PLAN.md`.

## İlk dikey kesitin kesin tasarımı

- Süre: 60 saniye.
- Kontroller: sağ şerit, sol şerit ve zıplama.
- Tema: sıcak Cat Home salonu ve kısa koridor varyasyonları.
- Ödül: Coin.
- Bakım bağlantısı: Happy Cat Bonus.
- Çarpışma: ilk iki hatada sendeleme ve yavaşlama, üçüncü hatada koşunun bitmesi.
- Sonuç: Run Complete ekranı ve güvenli Coin aktarımı.
- İlk ev harcamaları: 300 Coin top sepeti ve 400 Coin tırmalama tahtası; satın alınana kadar tamamen gizli kalırlar.

## Kapsam koruma kuralları

- Cat Runner eğlenceli bulunmadan kapsamlı ev ekonomisi kurulmayacak.
- İkinci mini oyun Cat Catch olarak eklendi; üçüncü bir mini oyun Coming Soon ile sergilenmez.
- Diamond, reklam ve IAP ana döngü kanıtlandıktan sonra seam olarak durur; doğrulanmamış reklam/IAP cüzdana yazmaz.
- İlk prototip için bütün oda listesi modellenmeyecek.
- Yeni sistemler mevcut save, bakım ve ev içi gameplay'i bozmayacak; her aşamada EditMode, PlayMode ve mimari doğrulama çalıştırılacak.

## Sıradaki çalışma paketi

1. ~~İlk Home XP kazanımı ve Home Level sözleşmesi.~~ ✅ Tamam (17 Ağustos 2026): `HomeProgressionService`, save v10, Home Store satın alımından XP.
2. ~~Home Level'ı arayüzde göstermek (HUD/mağaza rozeti)~~ ✅ Tamam (17 Ağustos 2026): `CurrencyHudController` ve `ShopPanelController` tarafından `HomeProgressionService.HomeLevel` ile canlı yenileme.
3. ~~Home Level'ın içerik açılımlarını yönetmesi (Aşama 4): oda/ürün kilitlerini `ProgressionService` quest chapter yerine Home Level'a bağlamak~~ ✅ Tamam (17 Ağustos 2026)
   - Home XP / Home Level service: ✅ Tamam
   - Home Level backend oda/ürün kilitleme: ✅ Tamam
   - Oda Home Level eşikleri uygulanmış: Living=1, Bathroom=2, Kitchen=3, Bedroom=4, Garden=5 ✅ Tamam
   - Home Level görünür HUD/mağaza rozeti: ✅ Tamam (17 Ağustos 2026)
   - Unity doğrulama: EditMode **230/230** + `LevelContentValidator` 0/0 (18 Ağustos 2026). PlayMode bu turda çalıştırılmadı.
4. ~~Runner parkur segmentlerinin görsel çeşitlendirilmesi.~~ ✅ Tamam (17 Ağustos 2026)
   - 9 silüet sahnede: Pet Shop, Toy Corner, Cozy Market, Cat Cafe, Window Garden, Toy Parade, Cozy Reading, Paw Park, Garden Courtyard.
   - Anti-tekrar son iki varyantı dışlar; koşu reset'i history'yi uygulamadan önce sıfırlar, böylece ilk geri dönüşen ufuk parçası son görünenle çakışmaz.
   - `LevelContentValidator` ve `CatRunnerSceneryVariantTests` 9 isimli varyantı kilitler; Garden kilitliyken 8’li havuz kullanılır.
5. ~~Yeni mağaza ürünleri için veri odaklı katalog genişletmesi.~~ ✅ Tamam (17 Ağustos 2026)
   - CAT sekmesine 5 yeni yerleştirilebilir ürün: Bell Collar, Cat Food Tin, Nap Pillow, Cat Grass Pot, Cardboard Hideout.
   - Ekonomi `HomeStoreService` satırı + sanat/placement `StoreCatalogAssets` satırı; oda 10'luk koleksiyonları ve sofa/sehpa/halı yasağı değişmedi.
   - Prefab, ikon ve SHOP kartları üretildi. Katalog 69 → 74, sonra Garden ile **84**. `HomeStoreServiceTests` eşlemeyi kilitler.
6. ~~Aşama 4 ilk yeni alan: Garden avlusu.~~ ✅ Tamam (17 Ağustos 2026)
   - Açık hava avlu: sürekli çim (kare taş yok), yan çitler, açık kapı, uzakta yol, gökyüzü, güneş ve bulutlar.
   - Bahçe ağacı; 2 kuş ağaca gelip gidiyor. Kenar çiçekleri + arı/kelebek.
   - 10 ROOM ürünü: yarn ball, flower pots, daisy bed, sapling, bird bath, sun lounger, balcony set, paw grill, hammock, sun pergola.
   - Unlock: Bedroom koleksiyonu + Home Level 5 + 6000 Coin / 60 elmas. MY ROOMS 5. kart, 3. satıra düşer.

Numaralı paket 1–6 ve Aşama 4 kuyruğu kapandı (17 Ağustos 2026).

7. ~~Garden Runner scenery.~~ ✅ `Variant_GardenCourtyard` 9. silüet; Garden odası kilitliyken havuza girmez.
8. ~~Bond milestone içerikleri.~~ ✅ `BondMilestoneService`: 35 Mouse Hunt, 80 Window Watch, 150 Feather Play, 250 Bird Watch. Quest paneli sonraki hediyeyi gösterir.
9. ~~Ev geliştirmesi ve kedi etkileşimi.~~ ✅ Pencere nöbeti, tüy oyuncağı ve bahçe kuş izleme `SitLookActivity` ile.
10. ~~Görev dengeleme.~~ ✅ Chapter 4 Home Loop (Runner / Store / Tunnel) ve Chapter 5 Garden Bond. Satın alma Home XP, bakım Bond, koşu Coin döngüsünü görevlere bağlar.

Balkon, Garden Patio ve Second Floor oynanabilir (Home Level 9 / 10 / 12); her biri 10 ürünlük koleksiyona sahiptir. Katalog toplamı **114**.

11. ~~Aşama 5 retention çekirdeği.~~ ✅ Tamam (18 Ağustos 2026)
    - UTC giriş serisi: 15–80 Coin, her 7. günde 1 Diamond.
    - 3 günlük ev görevi (Eat/Drink/Pet/Ball/Runner); Quest paneli chapter bitince de gösterir.
    - Achievement: first run, first shop (oturum başı), Home LV 3/5, Bond 80/250, 7-gün streak.
    - Runner x2 Coin reklamı mevcut ödülü değiştirmez; ayrı `cat-runner:{id}:double` txn.
    - Enerji / Catch can / sınırsız geçişe dokunulmadı.

12. ~~Aşama #4 yaşam katmanı + premium title v2.~~ ✅ Tamam (19 Ağustos 2026)
    - Koleksiyon 10'luk + katalog ödülü yalnız COLLECT (`CollectionMilestoneTests`).
    - Local notification seam, SFX cilası, `SKIP TOUR`.
    - Title v2: 3D kedi, HOME LV / jeton / `X OF 84 • COLLECTED`, CONTINUE, CREDITS.
    - EditMode **243/243**, PlayMode **10/10**, validator 0/0.

13. ~~Coming Soon oda zinciri.~~ ✅ Tamam (20 Ağustos 2026)
    - Balcony LV9 / 7000 Coin, Garden Patio LV10 / 8000 Coin, Second Floor LV12 / 9000 Coin.
    - Her oda 10 ROOM ürünü, koleksiyon ödülü, Room Selector ve HOME kartı içerir.
    - Katalog **84 → 114**; save şeması **v11** değişmedi.
    - Second Floor gerçek preview + Rooms `X` kapanış regresyonu + TMP warning temizliği tamamlandı.
    - EditMode **247/247**, PlayMode **10/10**, validator **0/0**, normal Play Console temiz.

## Yayın kalitesi / "tam paket" işleri (19 Ağustos 2026 başladı)

Oyun çekirdek döngüsü tam ama yayın hissi için eksik katmanlar sırayla ekleniyor:

1. [x] **Ana oyun ses/ambiyans** — `HomeAudioService` + `HomeAudioController` (prosedürel müzik + ambiyans + UI/olay SFX; mini-oyunda duck).
2. [x] **Kedi kişiselleştirme** — isim + 8 kürk rengi (kalıcı, kediye canlı tint); `CatJournalPanel` hamburger CAT JOURNAL satırından açılır.
3. Ana menü + ayarlar:
   - [x] **Ayarlar paneli** — `SettingsPanel` (SETTINGS satırı): MUSIC / SOUND / GAME SOUND / VIBRATION / REDUCED MOTION toggle'ları mevcut pref servislerine bağlı.
   - [x] **Başlık / ana menü ekranı** — `TitleScreen` v4: açık Blender CAT HOME amblemi, portresiz ve çakışmasız marka dock'u, HOME LV / jeton / `X OF 114 • COLLECTED`, katmanlı PLAY/CONTINUE, SETTINGS, CREDITS, masaüstü QUIT ve aynı Blender diorama ailesindeki işlevsel SHOP/ROOMS/GAMES kartları. Boot değişikliği yok.
4. [x] **Yaşam katmanı** (19 Ağustos 2026):
   - [x] Kedi idle kişiliği: `CatIdleBehavior` + `CatIdlePersonality`; mevcut clip'ler; kilit almaz; mırıltı + bubble.
   - [x] Koleksiyon tamamlama ödülü + `X OF 84 • COLLECTED`; oda 10'luk bitince kutlama; ödül yalnız COLLECT (`TryClaim`).
   - [x] Bildirimler (local notification seam): enerji dolu, dailies, çevrimdışı ödül. Editörde simülasyon.
   - [x] SFX cilası: yeme/içme, sevme, aktivite, oda değişimi, achievement (`HomeAudioController`).
   - [x] Onboarding polish: title → PLAY → isim → tur; `SKIP TOUR`; title açıkken tutorial gizli.

### Checkpoint — 19 Ağustos 2026 (#4 yaşam katmanı + title v2)

- Koleksiyon: `CollectionMilestoneService` + kutlama overlay (sorting 290). Grant yalnız COLLECT; satın alma cüzdan testlerini bozmaz.
- Bildirim seam: `LocalNotificationService` (enerji / dailies / +4 saat çevrimdışı); editör log, provider yoksa sessiz.
- SFX: yeme/içme/sevme/aktivite/oda/kutlama. Onboarding: `SKIP TOUR` (visual v12).
- Title v2: 3D kedi RT, HOME LV / jeton / `X OF 84 • COLLECTED`, CONTINUE/PLAY, CREDITS, QUIT. Smoke testler `targetTexture == null` kameraları sayar.
- EditMode **243/243**, PlayMode **10/10**, `LevelContentValidator` 0/0, 1 ekran kamerası. Canonical 3-sahne. Git commit/push yok.

### Checkpoint — 20 Ağustos 2026 (Coming Soon odalar + Rooms QA)

- Balcony, Garden Patio ve Second Floor oynanabilir; katalog 114, save v11.
- `SecondFloorPreview.png` ve HOME mağaza kopyası 1280×720 gerçek oda kamerasından üretildi.
- Room Selector `X`, oda geçişinde kapatıldıktan sonra sonraki açılışta yeniden etkinleşir; scrim aynı lifecycle'ı izler.
- Fredoka'nın desteklemediği Ellipsis/`✦` kullanımları warning üretmeyen biçime çevrildi.
- EditMode **247/247**, PlayMode **10/10**, `LevelContentValidator` 0/0; normal Play Console 0 error / 0 warning; kanonik üç sahne ve 1 kamera/listener/EventSystem.

### Checkpoint — 20 Ağustos 2026 (Second Floor SHOP ikonları + simetrik Rooms X)

- Second Floor ROOM koleksiyonundaki 10 mobilyanın tamamı için 512×512 gerçek prefab renderı üretildi; kartlar fallback mobilya glifi yerine kendi ikonlarına bağlandı.
- Alt dock oda etiketi tek satır, merkez hizalı TMP auto-size kullanır (19→11); `SECOND FLOOR • LEVEL 1` dahil 8 kanonik oda adı taşmadan sığar.
- Rooms kapatma işareti font glifi olmaktan çıkarıldı; aynı merkezde ±45° dönen eş boyutlu iki çubukla geometrik olarak simetrik kuruldu.
- Kilit testleri ikon dosyalarını/kart bağlarını, 8 oda adının tek satırda sığmasını ve X geometrisini doğrular.
- EditMode **250/250**, PlayMode **10/10**, `LevelContentValidator` 0/0; normal Play Console 0 error / 0 warning. 1920×1080 home/dock, Rooms ve Second Floor SHOP QA görüntüleri alındı.

### Checkpoint — 20 Ağustos 2026 (Aşama 5 içerik dengesi sözleşmesi)

- 114 ürün, 8×10 oda koleksiyonu, 7 ücretli oda preview'su, Home XP kapıları ve koleksiyon ödülleri rota düzeyinde denetlendi.
- Kanonik tam oda rotası **145.000 Coin**. Preview fiyatları 3000→9000 düzenli artıyor; her preview Home Level kapısı önceki zorunlu yatırımla erişilebilir.
- Oda tamamlama ödülü 500 Coin, oda koleksiyon maliyetinin yaklaşık %3,4–%4,1'i; hedef %3–%5 bandında. Fiyat/ödül değişikliği gerekmedi.
- `HomeEconomyBalanceTests`: rota bütçesi, gate erişilebilirliği ve tamamlama ödülü oranını kilitler.
- EditMode **253/253**, PlayMode **10/10**, validator 0/0.

Bu checkpointten sonraki çalışma Premium v2 görsel dönüşümdü; aşağıdaki checkpointlerde
tamamlandı. Title `NEW GAME` ve yerel dil altyapısı 21 Ağustos devam diliminde kapandı.

### Checkpoint — 20 Ağustos 2026 (Premium v2 görsel dönüşüm — dalga 1)

- Proje öncesi güvenli arşiv: `Backups/CatHome_before_premium_v2_20260820.zip`.
- Kanonik sanat yönü: `Assets/DesignReferences/CatHome_PremiumV2_Reference.png`; krem/pearl taban, canlı candy pastel, kontrollü altın, koyu renk yalnız metin ve küçük iç kontrast.
- İlk giriş ekranı prosedürel düz fondan sinematik ev/kedi kahramanına geçti: `Assets/Art/Title/CatHome_TitleHero_v1.png`; kişisel canlı kedi rozeti, HOME LV/coin/koleksiyon, PLAY/CONTINUE ve ayarlar/credits/quit korunuyor.
- Mağaza koyu veya düz mavi büyük gövde yerine krem showroom yüzeyi, pembe başlık ve okunaklı koyu açıklamalar kullanıyor. Rooms ve While Away aynı parlak premium yüzey ailesinde kaldı.
- Games hub kartları Runner/Catch kahraman sanatlarını taşır; kart → karşılama ekranı görsel sürekliliği sağlandı.
- Blender kaynak kiti: `ArtSource/Blender/PremiumFurniture/PremiumFurnitureKit_Source.blend`. 12 görünür ürün gerçek yuvarlatılmış premium modele geçirildi: 6 Second Floor, 2 Balcony, 4 Patio. Prefab/placement/rotation/save/economy sözleşmeleri değişmedi; SHOP ikonları gerçek modellerden yeniden üretildi.
- `PremiumPresentationTests` title hero, ortak premium buton davranışı ve 12 Blender model bağlantısını kilitler.
- EditMode **256/256**, PlayMode **10/10**, `LevelContentValidator` **0 error / 0 warning**; kanonik `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room ve 1 kamera geri yüklendi.
- Premium dönüşüm devam ediyor: kalan oda ürünlerinin ikinci Blender dalgası, Runner/Catch welcome/gameplay görsel QA ve bütün panel/akışların 1920×1080 son karşılaştırması henüz kapanmadı.

### Checkpoint — 20 Ağustos 2026 (Premium v2 görsel dönüşüm — tamamlandı)

- İlk giriş, ana ev, hamburger, Quest, Cat Journal, Settings, Rooms, SHOP CAT/ROOM/HOME, satın alma, Diamond Treasure, Games hub, Runner ve Catch karşılama/oynanış akışları aynı krem + candy pastel + kontrollü altın premium dilinde ekran ekran doğrulandı.
- Cat Journal büyük canlı kürk önizlemesi, sekiz kompakt renk seçeneği ve ad/kürk sürekliliği açıklaması aldı. Quest paneli candy başlık, okunaklı görev kartları, premium claim butonları ve simetrik kapatma kontrolüyle yenilendi.
- Blender premium mobilya kiti ikinci dalgada 14 görünür ürünü daha kapsadı: Bathroom tub/vanity/toilet; Kitchen island/refrigerator/stove/pantry; Bedroom queen bed/wardrobe/window daybed; Garden pergola/sun lounger/bistro set/hammock. Premium model bağlantısı toplam **26** ürüne çıktı; prefab, placement, fiyat ve save sözleşmeleri korunarak ikonlar gerçek modellerden yeniden üretildi.
- Outdoor Room Selector/HOME kartları gerçek gün/gece sistemini değiştirmeden sabit showroom pozlamasıyla bake edilir. Garden, Balcony ve Patio kartları akşam editör saatinde dahi parlak, okunaklı ve birbirleriyle tutarlı kalır; Second Floor da aynı 16:9 kamera zincirindedir.
- Runner gameplay canlı pet-town paleti ve okunaklı paw coin hattını; Catch gameplay candy arena, lila fareler ve açık HUD kapsüllerini korur. İki welcome kartı kahraman sanatı, skor/can bilgisi ve ana eylemlerle görsel olarak Games hub'a bağlandı.
- Final QA görselleri `Assets/Screenshots/PremiumV2/Final1920_*.png` altında 1920×1080 teslim boyutunda saklandı. Görsel referans `Assets/DesignReferences/CatHome_PremiumV2_Reference.png`, title hero `Assets/Art/Title/CatHome_TitleHero_v1.png`.
- Son doğrulama: EditMode **256/256**, PlayMode **10/10**, `LevelContentValidator` **0 error / 0 warning**, temiz Console; kanonik `GameScene + CatHome_UI + LivingRoom_Level01`, aktif Living Room ve tam **1** etkin kamera. Git commit/push yok.

### Checkpoint — 20 Ağustos 2026 (Referans uyumlu Main Menu v3)

- Ana menü referans panosundaki marka vitrini yapısına geçti: solda 3D CAT HOME amblemi + canlı kedi/statlar + baskın CONTINUE; ortada sinematik kedi sahnesi; sağda görsel SHOP / ROOMS / GAMES kartları.
- Blender kaynağı `ArtSource/Blender/Title/CatHome_MainMenuLogo_Source.blend`; Unity çıktıları `CatHome_MainMenuLogo_v1.png` ve `Models/CatHome_MainMenuLogo.fbx`. Amblem kedi kulaklı pearl plaka, sky/mint katmanlar ve altın kabartma kullanır.
- Üç sağ kart dekor değildir: title fade tamamlandıktan sonra sırasıyla gerçek mağaza, Room Selector ve Games hub açılır. Yavaş additive yüklemede canlı kedi portresinin boş kalmaması için bağlanma yeniden-deneme davranışı kalıcı hale getirildi.
- Final görsel `Assets/Screenshots/PremiumV2/Final1920_MainMenuV3.png`. EditMode **256/256**, PlayMode **10/10**, validator **0/0**; kanonik üç sahne geri yüklendi. Git commit/push yok.

### Checkpoint — 20 Ağustos 2026 (Main Menu v4 — tutarlı premium sanat ailesi)

- Boş kalabilen canlı kedi portresi ve ona ait RenderTexture/yeniden-bağlanma akışı menüden tamamen kaldırıldı. Sol marka dock'u sabit optik ritimle yeniden ölçülendi; logo, karşılama, isim, iki stat, koleksiyon, CONTINUE, yardımcı eylemler ve alt slogan arasında otomatik dikdörtgen taramasında **0 çakışma** var.
- CAT HOME Blender logosu daha açık pearl, aqua/mint ve şampanya altınıyla yeniden render edildi; şeffaf kenar boşluğu Unity'de kırpılarak amblem gerçek alanı dolduruyor. Kaynak: `ArtSource/Blender/Title/CatHome_MainMenuLogo_Source.blend`.
- SHOP / ROOMS / GAMES artık karışık ekran görüntüsü/illüstrasyon kullanmıyor. Üçü aynı ortografik kamera, üç noktalı stüdyo ışığı, candy malzeme ve yuvarlatılmış low-poly diorama setinden üretildi: `ArtSource/Blender/Title/CatHome_MainMenuCards_Source.blend` ve `Assets/Art/Title/MainMenu_*Card_v1.png`.
- Ana ve yardımcı butonlar aynı merkezli dış aura + şampanya rim + dört duraklı candy yüz + üst cam bandı + iç derinlik bandı kullanır; dış Shadow/Outline yoktur. `ColorTint` kapatıldı, `PremiumButtonFx` etkileşimi korunuyor. Üç sağ kart aynı kart/rim/görsel kuyu/etiket hiyerarşisini kullanır.
- SHOP, ROOMS ve GAMES canlı sahnede tek tek doğru hedeflerini açtı. Final görsel `Assets/Screenshots/PremiumV2/MainMenuV4_Final1920.png` (**1920×1080**). `PremiumPresentationTests` **3/3**, tüm EditMode **256/256**, PlayMode **10/10**, validator **0/0**, Console temiz; kanonik üç sahne ve tam 1 kamera/listener/EventSystem geri yüklendi. Git commit/push yok.

### Checkpoint — 21 Ağustos 2026 (Main Menu v4 neon + ilk açılış)

- CAT HOME Blender amblemi daha aydınlık pearl/candy malzemelerle yeniden render edildi; ölçülü neon aura, dört köşe parıltısı ve reduced-motion uyumlu nefes animasyonu `TitleLogoNeonFx` ile eklendi.
- İlk kez açan oyuncu aynı premium menüyü `WELCOME HOME`, `PLAY`, HOME LV. 1, 0 Coin ve `0 OF 114 • COLLECTED` durumuyla görür. SHOP / ROOMS / GAMES kartları teaser olarak görünür ancak onboarding tamamlanana kadar `AFTER TOUR` rozetiyle pasiftir.
- Mobilde QUIT gizlenirken SETTINGS ve CREDITS optik merkeze yeniden hizalanır. QA görselleri: `Assets/Screenshots/PremiumV2/MainMenuV4_NeonFinal1920.png`, `MainMenuV4_FirstLaunchNeonFinal1920.png` ve `MainMenuV4_FirstLaunchMobilePreview.png`.
- EditMode **256/256**, PlayMode **10/10**, `LevelContentValidator` **0 error / 0 warning**; kanonik üç sahne ve 1 kamera/listener/EventSystem geri yüklendi. Git commit/push yok.

**Güncel devralma:** Premium v2, Main Menu v4, yerel kayıt kurtarma, onaylı
`NEW GAME` ve Türkçe/İngilizce çekirdek altyapısı tamamlandı. Sıradaki Aşama 6
ürün işi anonim/Google hesabı + bulut kayıt veya kalan ekranların çevirisidir. Ayrıntılı devir:
`Docs/CatHome_Checkpoint_2026-08-21.md`.

### Checkpoint — 21 Ağustos 2026 (onaylı NEW GAME + TR/EN altyapısı)

- Ana menüde yalnız mevcut yolculuk varsa görünen `NEW GAME` eylemi ve ayrı
  `VAZGEÇ` / `EVET, YENİ OYUN` onay katmanı eklendi.
- Sıfırlama: Coin, Home/Bond ilerlemesi, görevler, achievement/daily, oda ve ürün
  sahipliği, yerleştirmeler, Runner/Catch skor/tutorial/misyonları ve kedi adı/kürkü
  temizlenir; başlangıç ihtiyaçları 100/100/100, oda Living Room olur.
- Diamonds, idempotent satın alma işlem kimlikleri, Runner/Catch doğrulanmış
  sınırsız geçiş tarihleri, ses/haptics/reduced-motion tercihleri ve dil korunur.
  Yazmadan önce timestamp'li `.before-new-game-*` destek kopyası alınır.
- `GameLanguageService` paket gerektirmeyen TR/EN çekirdeğidir. Sistem dili Türkçe
  ise ilk açılış Türkçe, diğer diller İngilizce; Ayarlar'daki LANGUAGE/DİL satırı
  seçimi kalıcı ve canlı uygular. Main Menu, Settings ve NEW GAME çevrilmiştir.
- QA: 1920×1080 TR Main Menu, TR/EN Settings ve TR onay görselleri
  `Assets/QA/PremiumVisuals/2026-08-21_*.png`.
- EditMode **268/268**, PlayMode **10/10**, validator **0/0**, Console temiz;
  kanonik üç sahne ve Camera/AudioListener/EventSystem **1/1/1**. Save **v11**.
- Git commit/push yok.

> Kullanıcı kararı (20 Ağustos 2026): mobil performans ve uzun oturum testleri ara çalışma önerilerine alınmaz. Yalnız kullanıcı “her şey bitti, deneyelim” veya “her şey bitti, yayınlayalım” dediğinde yapılmadıkları yayın öncesi eksik kontrol olarak hatırlatılır.

> Kullanıcı kararı (21 Ağustos 2026): yeni diller oyun bitmeye yakın, bütün oyuncu
> metinleri kesinleşip **metin dondurma** yapıldıktan sonra topluca eklenir. Bu iş
> yayın öncesi zorunlu listeden çıkarılmaz; öncelik `ja-JP → ko-KR → zh-Hant →
> pt-BR → es-419`, sonra veriye göre Almanca/Fransızcadır.

Runner → Coin → Home Store → odada kalıcı açılım döngüsü çalışır durumda ve Home XP artık bu döngünün satın alma adımından besleniyor.
