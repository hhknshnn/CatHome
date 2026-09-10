# Salon — izleme gecikmesi ve Window Watch kaldırma, 10 Eylül 2026

Kullanıcı kitaplık, salon bitkisi ve lambader İzle düğmelerindeki takılma/başlamama sorununu düzeltmeyi ve Window Watch'ı tamamen kaldırmayı istedi. Bu adım bu kapsamı kapatır; sıradaki kullanıcı konusu UI'dır.

## Değişiklik

- Window Watch sahne nesnesi, bağ hediyesi ve yeniden üretim yolu kaldırıldı. Eski enum/quest kimlikleri kayıt uyumluluğu için korunur; emekli eylem prompt veya TryStart ile başlayamaz. Eski authoring giriş noktası yalnız kaldırır. Pencere dekoru aynı.
- `level5_window` görevinin kimliği, tipi8, hedef1,25 jeton/15 bağ ödülü ve kayıt ilerlemesi aynı. Metinler Türkçe/İngilizce mevcut kitaplık/bitki/lambader gözlemine yöneltildi; bu eylemler zaten aynı görev türünü ilerletir. Artık kaldırılmış pencere eylemi istenmez.
- Önce her gerçek bakış yüzeyi için ayrı,144 duruşa kadar ve tekrar başlangıçta yapılan yol araması vardı. Mevcut salon ölçümünde kitaplık tek çözümü1554.36ms, lambader2461.25ms; lambader çözümü başarısızdı.
- Kitaplık/kitap seti, salon bitkileri ve lambaderde yakından uzağa ortak .2m yerel zemin noktaları denenir. Kamera/denetleyici ölçümü tek alınır; her hedef için oda ızgarası/BFS kurulmaz. Yakın açık rota aynı başlangıçta tekrar hesaplanmaz; kedi girişte gerçekten hareket ettiyse yenilenir. Kutupsal örneklemenin atladığı lambader yanındaki dar geçiş oda ızgarasıyla bulunur.
- .27m süpürülen gövde açıklığı, bitişte tam denetleyici dönme hacmi, oda sınırı, gerçek eşya yüzeyine engelsiz görüş ve sınırlı baş yönü korunur. Diğer odaların SitLook araması aynı. Mobilyalar, mama/su kapları ve yeme animasyonu değişmedi.

## Doğrulama

Aynı gerçek başlangıçlarda son yol çözümü: kitaplık5.48ms, lambader2.70ms, bitki0.94ms; üçü başarılı. Bunlar bu bilgisayardaki Unity ölçümüdür, fiziksel cihaz performansı iddiası değildir.

**4/4 native** ve **5/5 hedefli EditMode** başarılı. Mevcut koleksiyonun eşyalı salon kopyasında üç gerçek görünür düğme × üç tekrar **9/9** tamamlandı. Düğme çağrısı **0.60–1.94ms**; her eylem üç bakış evresi/tek tamamlanma, gerçek baş hareketi ve kameraya dönük gövdeyle doğrulandı. Engel nedeniyle reddetme enerjiyi harcamaz; engel kalkınca başlar; pause/iptal kontrolcü ve bakışı temizler. Gerçek yüzey/görüş testleri de başarılı. İlk fixture denemeleri ve tanı amaçlı sonuçlar final XML yerine kullanılmaz. Tam tüm oda/ırk taraması tekrarlanmadı.

[Üç gerçek Unity görüntüsü](QA/LIVING_WATCH_2026-09-10/index.html) · [Kontrol özeti](QA/LIVING_WATCH_2026-09-10/verification-summary.json)

Validator0/0; diğer yedi oda sahnesi aynı. Ürün geometrisi/konumu değişmediğinden önceki103 kart/sekiz oda görseli korunur. Gerçek kayıt/recovery başlangıç–son **34862B668A547BE56D72F98FD27D2C873B5FC5E54AD1DFB51B78BF6293937FE1**, CP2 aynı. 16 tercih/varlık bayrağı geri yüklendi. QA/Play/derleme kapalı, üç temiz sahne, tek kamera/ses dinleyici ve salt-okunur ön izleme açık. APK/arşiv/commit/push/yayın/kapatma yapılmadı.
