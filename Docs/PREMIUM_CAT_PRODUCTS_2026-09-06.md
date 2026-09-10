# Premium kedi eşyaları — 6 Eylül 2026

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

CAT mağazasındaki 17 ürün yeniden modellendi. Kaynak: `ArtSource/Blender/PetProducts/build_pet_collection.py`; Blender dosyaları ve ölçüm kayıtları aynı klasörde. Unity modelleri `Assets/Art/PremiumPet/Models`, gerçek ürün prefablari `Assets/Art/StoreProducts/Prefabs` içinde.

- Top sepeti: gerçek top kovalamaca; atış hedefi doluysa erişilebilir zemine gider.
- Tırmalama direği: ölçülmüş yaklaşım, iskelet tırmalama pozu ve hareketli askı oyuncağı.
- Dört yatak/minder: alçak giriş, minderi patileme, uyuma, zemine geri çıkma.
- Fare, tüy, top pisti, kurdele ve çıngırak: ayrı hareketli parça ve kedi tepkisi.
- Mama kabı, ödül/mama bulmacaları, kedi çimi: ürüne yaklaşma ve uygun etkileşim.
- Tünel ve kutu: gerçekten açık giriş; içinden geçme veya saklanıp çıkma.

Tasma, gezdirme kayışı ve çıngıraklı tasma yerine top pisti, kurdele minderi ve çıngıraklı teker kullanılır. Eski ürün kimlikleri, fiyatları ve satın alma kayıtları korunur. CAT ürünlerinin kullanıcı tarafından kaydedilmiş taşıma/depolama verileri korunur. ROOM yerleşimi bu çalışma kapsamında değiştirilmez.

Eski CAT varsayılanları rastgele hash konumları ile eski sahne noktalarını karıştırıyordu. Tam koleksiyonda kitaplık, sehpa, lamba ve oyuncaklar birbirine giriyordu. `StoreCatalogAssets.TryGetCatPose` artık 17 ürün için bilinçli varsayılan konum ve açı verir. Eski iki aktivitenin ilk yerleşim yuvaları da bu konumları kullanır. Kullanıcının açıkça kaydettiği dünya konumu ayrıca korunur.

`CatEnrichmentActivity` 15 ürüne on davranış sağlar. BallChase ve ScratchPost kendi oyun akışlarını korur. Katı yüzeyler gerçek mesh geometrisini kullanır; düşük oyuncak tabanları ve minderler yürünebilir. Temas yüksekliği gerçek üst üçgenlerden ölçülür; tünelin zemini 0'dır, çatısı zemin kabul edilmez. Irklar doğal ölçekte kalır; kontrolcü, oyuncak pozu ve hareket kilidi tamamlanma/iptalde geri verilir.

Canlı yakın plan, eski Walk tabanlı geçişte kuyruğun tünel kumaşını deldiğini gösterdi. Gövde kesiti de ölçüldü: Sphynx'in gövdesi 0.425'e, kuyruğu 0.609'a ulaşıyordu. Tünelin yükseklik sözleşmesi bilinçli olarak 0.44'ten 0.60'a çıkarıldı; zemin footprint'i aynı kaldı. Son ek mesafe Maine Coon'un gerçek pozlanmış başından ölçüldü. Tünel sırasında üç kuyruk eklemi geriye ve hafif aşağı uzanır; her kare önce kaynak iskelet pozu geri konur, çıkış/iptal sonrasında bu düzeltme kalmaz. Tünelin ortasında ayakta pati savurma oynatılmaz. `Tunnel_AllBreeds_KeepThePosedBodyAndTailInsideTheCloth` gerçek pozlanmış gövde ve kuyruk noktalarını iç kemerle karşılaştırır.

CAT satın alımları mevcut depolama akışını korur. Depodaki ürün ne görüntü ne aktivite sunar; odaya çıkarıldığında ikisi birlikte açılır. Eski satın alma testi, CAT ürünlerinin depoya girme kararından önce kalmış görünürlük beklentisini taşıyordu; artık satın alma ve odaya çıkarma adımlarını ayrı doğrular. ROOM ürünleri hâlâ satın alındığı anda tasarlanan yerinde açılır.

Ön izlemeler 1024×1024 çözünürlükte son ürün prefabından yeniden çekildi. İlk URP ön izleme karesi boş dönebildiğinden ilk ürünün karesi ısıtılır ve tekrar alınır. Toplu fotoğraflar gözle kontrol edildi. Canlı oyun kareleri 1920×1080 çözünürlüktedir.

Salonun HOME/oda seçici fotoğrafı da yeni eşyalarla güncellendi. Ortak çekim aracı kapalı odaların mevcut ışık düzeninin üstüne ikinci güçlü stüdyo düzeni eklediği için renkler beyaza kaçıyordu; kapalı oda çekimleri artık kendi ölçülmüş ışıklarını korur. Açık hava fotoğraflarının gündüz çekimi aynı kalır.

## Doğrulama

Son test sonuçları ve gerçek görüntüler: `Docs/QA/CAT_2026-09-06`. `index.html` 17 ürünün fotoğraf galerisidir. Testler kullanıcının kaydından ayrı QA kopyasında çalışır; çevrimiçi skor ve cloud sync kapalıdır.

- Son tam EditMode: **425/425** (`EditMode.xml`).
- Native PlayMode: 15 ürün × 10 ırk matrisi **150/150**, top sepeti/tırmalama × 10 ırk **20/20**, iptal/sahiplik/depolama testi başarılı. `PlayMode_ClearanceFirst.xml` bu üç testi başarılı kaydeder; aynı koşuda bulunan tünel temas hatası sonraki ayrı koşuda kapatıldı.
- Son tünel temas koşusu: **10/10 ırk**, `PlayMode_TunnelFinal.xml` **1/1**. Gövde ve kuyruk birlikte, kare sonunda pozlanmış mesh üzerinden ölçülür. En yüksek normalize iç kemer oranı Maine Coon'da **0.914902**, sınır **1.0**; toleransla dışarı taşma kabul edilmez. CSV bütün ırkları içerir.
- Bu geçişte **4 farklı PlayMode testi** başarıya ulaştı. Önceki 800 ROOM kombinasyonu bu çalışma kapsamında tekrar koşulmuş gibi sayılmaz.
- Beş CAT mağaza satırındaki 17 kart ve tenteli yatağın satın alma fotoğrafı gözle kontrol edildi. `GetWorldCorners`/EventSystem ölçümü: çakışma, ekran dışına taşma ve tıklama engeli **0**. Ürün penceresi ve mağaza gerçek masaüstü fare tıklamasıyla kapandı; normal oyunda hareket ve giriş kilidi serbest kaldı.
- Son normal Play: GameScene + CatHome_UI + LivingRoom_Level01, kamera/listener/EventSystem **1/1/1**, Console **0 hata / 0 uyarı**. Test ve görüntü kopyası gerçek kayıtla birleştirilmez.
- Kapanış: QA kapalı, üçlü authoring görünümü açık, LivingRoom_Level01 aktif; `LevelContentValidator` **0 hata / 0 uyarı**.

Git commit/push yok; kullanıcı manuel yapar.
