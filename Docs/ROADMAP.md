# Cat Home Roadmap

Son güncelleme: 18 Ağustos 2026

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
- Performans, mobil cihaz, kayıt kurtarma ve uzun oturum testleri.
- İçerik dengesi, onboarding güncellemesi ve genel polish.

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

Balkon, Garden Patio ve Second Floor katalogda Coming Soon (Home Level 9 / 10 / 12).

11. ~~Aşama 5 retention çekirdeği.~~ ✅ Tamam (18 Ağustos 2026)
    - UTC giriş serisi: 15–80 Coin, her 7. günde 1 Diamond.
    - 3 günlük ev görevi (Eat/Drink/Pet/Ball/Runner); Quest paneli chapter bitince de gösterir.
    - Achievement: first run, first shop (oturum başı), Home LV 3/5, Bond 80/250, 7-gün streak.
    - Runner x2 Coin reklamı mevcut ödülü değiştirmez; ayrı `cat-runner:{id}:double` txn.
    - Enerji / Catch can / sınırsız geçişe dokunulmadı.

Sıradaki çalışma: Aşama 5 kalanı (mobil performans, uzun oturum, onboarding polish).

Runner → Coin → Home Store → odada kalıcı açılım döngüsü çalışır durumda ve Home XP artık bu döngünün satın alma adımından besleniyor.
