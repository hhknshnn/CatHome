# Cat Home — Görsel tasarım referansları
6 Eylül 2026 · Önerilen ortak yön: sıcak, modern, içerik odaklı.

Bu set, mevcut oyun kedileri, ürün ikonları ve oda görüntüleri referans alınarak **yerleşik image_gen** ile oluşturuldu. Oyun kodu, sahneler ve modeller değiştirilmedi. Görseller tasarım yönünü anlatır; Unity'den alınmış uygulanmış ekran görüntüleri değildir. Uygulamada mevcut model, materyal, animasyon ve kanonik para ikonları doğrudan kullanılacak.

[Görsel galeriyi aç](index.html)

## Altı referans

| Görsel | Ana karar | Önerilen hareket ve etkileşim |
|---|---|---|
| 01 — Ana menü | Kediler merkezde; tek baskın Devam et; yardımcı yollar küçük. | Kedilerin mevcut idle/oturma animasyonları, sakin kuyruk ve baş hareketleri; düğmede kısa basılma geri bildirimi. UI sürekli parlamaz. |
| 02 — Ev | Oda ve kedi için açık merkez; kompakt ihtiyaçlar ve para; tek bağlamsal bakım eylemi. | Joystick dokunuşta tepki verir; uygun eşyaya yaklaşınca eylem görünür. İhtiyaç güncellemesi kısa ve okunur olur. |
| 03 — Mağaza | Büyük gerçek ürün sunumu, açık kategori seçimi, ayrı jeton/elmas fiyatları. | Seçili kart ince vurgu alır; satın alma ayrıntısı aynı ürünle açılır; kaydırma konumu korunur. |
| 04 — Kedim | Büyük ve önden kadrajlanmış kedi; ırk, isim ve renk tek ailede. | Kedi döndürülebilir; seçim kısa geçişle önizlenir; uygulandığında evde ve oyunlarda aynı kimlik taşınır. |
| 05 — Satın alma | Ürün → açıklama → iki net ödeme seçeneği; arka plan ikincil. | Onay aşaması ayrı ve açık; satın alma sonrası eşya hazır yerine eklenir, kısa yerleşim geri bildirimi verilir. |
| 06 — Odalar | Oda görseli temel içerik; mevcut, açık ve kilitli durumları belirgin. | Karttan ziyaret/açma; geçişte seçilen oda korunur; yüklenme ve geri dönüş okunur. |

## Görsel kurallar

- **Krem içerik yüzeyi:** #FFF9EF; hafif mint yardımcı yüzey: #DDEDE3.
- **Turkuaz:** #218F87, seçili durumlar ve yardımcı eylemler.
- **Mercan:** #F5786C, ana eylemler.
- **Koyu mürekkep:** #293A3B, güçlü ve okunur metin. Büyük koyu paneller kullanılmaz.
- Altın, lilac ve pembe yalnız kontrollü vurgu. Her yüzey bütün paleti aynı anda kullanmaz.
- Bir ana çerçeve, ince iç ışık ve ölçülü derinlik. Kalın çok renkli çerçeve yığınları yok.
- Fredoka karakteri korunur; uzun açıklamalarda cümle düzeni, kısa eylemlerde gerektiğinde büyük harf.
- Bir ana eylem, daha sakin yardımcı eylemler. Jeton ve elmas iki açık alternatif oluşturur.
- Portre ve oda görsellerinin doğruluğu, arayüzün bütünlüğünün parçasıdır.
- Hareket azaltıldığında aynı son yerleşim ve işlevler korunur.

## Uygulamaya aktarım

Bu taslaklardaki bakiye, ihtiyaç, koleksiyon ve kilit değerleri örnektir; kullanıcı kaydını temsil etmek zorunda değildir ve kayıt değiştirilmedi. Mağaza fiyatları mevcut katalogdaki örnek ürünlerden alındı. Görseller yayın deneyimini gösterir; mevcut FREE TEST ekonomi kararı uygulamada ayrıca korunur.

Üretken görseller model ayrıntılarını, ikon çizimini ve kart oranlarını yaklaşık yorumlayabilir. Bunlar yeni kedi/eşya üretme talimatı değildir. Özellikle oda görselleri Unity uygulamasında kesin 16:9 kalacak; taslaktaki kadrajdan oran kopyalanmayacak. Sphynx'in gerçek modeli ve portresi, kanonik pati jetonu ve elmas varlıkları doğrudan bağlanacak. Eşya modelleri bu konsepte uyması için yeniden biçimlendirilmeyecek.

Evdeki Besle örneği bağlamsal eylemin görünümünü anlatır; gerçek oyunda yalnız ilgili etkileşim koşulu sağlandığında açılır. Ana menüdeki kısayollar ve oda filtreleri öneridir. Mevcut SHOP, ROOMS, görev ve mini oyun yolları ortak gezinme planıyla bağlanır; taslakta görünmemeleri bir işlevin kaldırılması anlamına gelmez.

Sabit eşya yerleşimi, satın alınanların daima görünmesi, iki para seçeneği, ürün ön koşulları ve doğrulanmış reklam/IAP ödülleri korunacak. Ayarlar, görevler, kutlamalar ve mini oyun ekranları da bu altı referansın düğme, kart, metin ve durum dilini kullanmalı.

## Dosyalar ve üretim bilgisi

Altı seçilmiş PNG `images/` altında. Galeri yalnız son sürümleri gösterir; `Draft` dosyaları ilk varyantlardır. Native çıktı **1672×941 piksel** (HD); 4K olarak sunulmuyor. Unity'deki son UI, ekran çözünürlüğünde çizilen metin ve bileşenlerle kurulacak; bu PNG'ler oyun arayüzü olarak esnetilip kullanılmayacak.

Tam üretim ve düzeltme istemleri [prompts.json](prompts.json) içinde. Yöntem: yerleşik image_gen. CLI/API yedeği kullanılmadı.

