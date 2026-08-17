# Cat Home Roadmap

Son güncelleme: 17 Ağustos 2026

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
| Home XP | Ev geliştirmelerinden kazanılır; Home Level ve yeni oda/alan açılımlarını belirler. Harcanmaz. | Home progression aşamasında |
| Diamond | Nadir/premium kaynak olarak korunur; Runner'ın normal ödülü değildir. | MVP sonrası |
| Runner Enerji | Koşuya girişte harcanır; en fazla 5'tir ve 10 dakikada 1 yenilenir. | Aktif |
| Player Level | Ayrı bir progression kaynağı değildir ve oyun kararlarında kullanılmayacaktır. | Emekli edilecek |

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
- [x] Home Store eklendi; top sepeti (120 Coin) ve tırmalama tahtası (280 Coin) satın alındığında odada kalıcı olarak açılıyor.
- [x] Satın alınan ev ürünleri odanın geçerli zemin alanında sürüklenebiliyor; duvar, koltuk, masa, mama alanı ve diğer eşyalarla çakışan konumlar engelleniyor; konum ve yön kaydediliyor.
- [x] Top aktivitesi sevme animasyonu/kalplerden ayrıldı; top sekmesi ve hedefe bakış iyileştirildi; mevcut kedi iskeletine özel pati vurma, pounce ve döngülü tırmalama animasyonları eklendi.
- [x] Home Store dört tam odaya genişledi: Living Room, Bathroom, Kitchen, Bedroom (her biri 10 parçalık koleksiyon) ve `RoomSelectorPanel` oda navigasyonu.
- [x] İkinci mini oyun Cat Catch ve iki oyunu barındıran Games hub eklendi.
- [x] IAP elmas paket kataloğu (10/20/50/100/500/1000) ve doğrulanmış satın alma seam'i hazırlandı; gerçek tahsilat yalnız platform doğrulaması sonrası ödül verir.
- [x] Living Room penceresi + gün/gece `WindowSystem`; premium pastel duvar/ışık geçişi.
- [~] Home XP ve Home Level **sözleşme fazı** eklendi: Home XP ev geliştirmelerinden (Home Store satın alımı) kazanılır, Home Level XP'den türetilir, save şema v10. Şu an ek/görünür progression; oda kilidini henüz yönetmez (UI gösterimi ve gating bekliyor).

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

- İlk yeni oda veya alan.
- Açılan odanın Runner segment havuzuna eklenmesi.
- Bond milestone içerikleri.
- Daha fazla ev geliştirmesi ve kedi etkileşimi.
- Görevlerin Coin, Bond ve Home XP döngüsüne göre yeniden dengelenmesi.

### Aşama 5 — Retention, monetization hazırlığı ve yayın kalitesi

Hedef: Ana döngü kanıtlandıktan sonra uzun süreli kullanım ve yayın hazırlığı.

- Günlük görev ve giriş ödülü.
- Achievement ve milestone ödülleri.
- Nadir Diamond kaynakları.
- İstenirse koşu sonunda ödüllü reklam ile x2 Coin.
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
- İlk ev harcamaları: 120 Coin top sepeti ve 280 Coin tırmalama tahtası; satın alınana kadar tamamen gizli kalırlar.

## Kapsam koruma kuralları

- Cat Runner eğlenceli bulunmadan kapsamlı ev ekonomisi kurulmayacak.
- MVP'de ikinci mini oyun geliştirilmeyecek veya Coming Soon ekranlarıyla sergilenmeyecek.
- Diamond, reklam ve IAP ana döngü kanıtlanmadan aktive edilmeyecek.
- İlk prototip için bütün oda listesi modellenmeyecek.
- Yeni sistemler mevcut save, bakım ve ev içi gameplay'i bozmayacak; her aşamada EditMode, PlayMode ve mimari doğrulama çalıştırılacak.

## Sıradaki çalışma paketi

1. ~~İlk Home XP kazanımı ve Home Level sözleşmesi.~~ ✅ Tamam (17 Ağustos 2026): `HomeProgressionService`, save v10, Home Store satın alımından XP.
2. Home Level'ı arayüzde göstermek (HUD/mağaza rozeti) — "ilk Home XP kazanımı"nın oyuncuya görünür ayağı.
3. Home Level'ın içerik açılımlarını yönetmesi (Aşama 4): oda/ürün kilitlerini `ProgressionService` quest chapter yerine Home Level'a bağlamak.
4. Runner parkur segmentlerinin görsel çeşitlendirilmesi.
5. Yeni mağaza ürünleri için veri odaklı katalog genişletmesi.

Runner → Coin → Home Store → odada kalıcı açılım döngüsü çalışır durumda ve Home XP artık bu döngünün satın alma adımından besleniyor.
