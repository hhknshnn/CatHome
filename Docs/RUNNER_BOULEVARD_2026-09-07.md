# Runner Boulevard, coin ve top çarpışması — 7 Eylül 2026

Kullanıcının ikinci görsel düzeltme isteği: Runner'ın sokak/engel kalitesi, yeniden tasarlanan coin, coinlerin eşya içine girmemesi, on ırkta eğilme/zıplama ve evde topun mobilyadan geçmemesi.

## Görsel uygulama

Runner artık taş döşeli bir yaya sokağıdır. Üç cephe tipi; kiremitli çatı, bölmeli vitrin, panjur, çizgili tente, bitki ve balkon detayları taşır. Yakın ve arka cepheler, fenerler ve dokuz ayrı kedi temalı köşe kullanılır. Garden kilidi ve son iki çevreyi tekrarlamama kuralı korunur. Eski dekor grupları görünmezdir. Yol üzerinde eski neon şeritler ve hız çizgileri yoktur.

Sekiz yer engeli yeniden modellendi: dokuma yumak sepeti, sisal direk, seramik mama kabı, robot süpürge, dikişli yatak, ödül kutuları, içi açık taşıma kafesi ve biyeli minderler. Eski renkli rampalar, aynı yumuşak yükseklik fonksiyonunu izleyen ahşap ve pirinç detaylı bir parkurla değiştirildi. İki üst engel destekli çizgili tentedir; kumaşın gerçek alt açıklığı .50 m'dir. `build_runner_boulevard.py` toplam 25 FBX üretir. Kamera (0,1.50,-3.10), FOV 46–50, SMAA yüksek kalite; ölçülü kontrast/doygunluk ve güncel yol eğimi takibi kullanılır.

Coin aynı kanonik dosyalarda yeniden üretildi: yivli kenar, sürekli altın çerçeve ve kabartma pati. 1536 px şeffaf ikon doğrudan aynı Blender modelinden çekilir. HUD/mağaza/oyun ortak sanatı kullanır. Runner coin modeli dik kalır ve pati kameraya bakar; ağır ışık yayılımı ve eski halo katmanları kaldırılmıştır.

## Temas ve hareket

- Sahne üreticisi son ölçeklenmiş coin ve engel mesh sınırlarını kaydeder. Coin doğarken bütün hacmi, salınım ve güvenlik boşluğu ile kontrol edilir. Gerektiğinde boş şeride veya sonraki boşluğa alınır. Sonradan doğan engel de var olan coinlerin içine yerleşemez.
- Mıknatıs çekimi boyunca genişletilmiş engel hacmi taranır. Platform doğarken var olan jeton/engeller için yer ayrılır; sonraki coinler gerçek platform yüksekliğini kullanır.
- Eğilme ve zıplama gerçek iskelet pozlarıdır; model ölçeği değişmez. On ırkta görünür deri son karede ölçülür: zıplama tepesindeki ayak/gövde açıklığı, tentenin altındaki baş/kuyruk ve zemine batmama kontrol edilir. Her zıplama tek iniş üretir. Temas eğrisi saniyede 240 örnek kullanır ve yoğun konum eğrisi eklendikten sonra Unity'nin yeniden örneklediği SON klip üzerinde ikinci kez düzeltilir. Yalnız kaynak/anahtar karelerde ölçmek Oriental Shorthair'ın ara eğilme pozunda zemine batmayı kaçırıyordu.
- `CatToyBallCollision`, top rutini için ayrı ve görünmez bir fizik sorgu sahnesi oluşturur. Gerçek ürün geometrisini, kedinin yürüyebildiği alçak trigger oyuncakları da dahil ederek örnekler. Sürekli küre taraması hızlı kare adımında bile engelin içinden atlamaz. Odanın/kedinin collider davranışı değiştirilmez. Rutin bitince, iptalde ve kapatmada sorgu sahnesi bırakılır.
- `ResultMissions` içindeki fontta olmayan onay işareti, Türkçe/İngilizce okunabilir tamamlanma sözcükleriyle değiştirildi.

## Doğrulama

Tam EditMode **459/459**, son Runner native PlayMode **4/4**, top native PlayMode **2/2**, LevelContentValidator **0 hata / 0 uyarı**. Son sonuçlar ve gerçek HD kareler [galeride](QA/RUNNER_BOULEVARD_2026-09-07/index.html) tutulur. İlk coin testi 725. sırada salınım payının yetersiz kaldığını yakaladı; `Runner-PlayMode-Initial.xml` bu bulguyu saklar. Düzeltilen 800 sıralı test `Coin-PlayMode.xml` içindedir. Son dört Runner testi, ek platform/mıknatıs kontrolleri ve düzeltilmiş on ırk hareket taramasıyla Runner-PlayMode-Final.xml içinde geçmiştir. Runner-PlayMode-GaitReview.xml ara eğilme pozunda bulunan batmayı kaydeder; son sonuç değildir.

Topun alçak oyuncak ve yüksek mobilyaya çarpması, üstten açık yol ve hızlı adım kontrolü; ayrıca tam beş oyuncaklı salonda on ırkın üç vuruşluk rutini `Ball-PlayMode.xml` içinde geçmiştir.

QA yalnız `UiQaTestSession` kayıt kopyasında yapılır. Görüntüler yerel test oyunudur; kaynak Blender renderı ayrıca etiketlenir. Mobil cihaz GPU maliyeti bu masaüstü kontrolüne dahil değildir. Git commit/push kullanıcıya aittir.

Son geniş/dar ekran alanı taramalarında çakışma, SafeArea taşması veya tıklanamayan düğme bulunmadı. Güncel Runner ön izlemesi ve hero fotoğrafı son Play çekiminden tekrar import edildi; oyun seçimi, karşılama, duraklatma, sonuç, ev HUD'u ve mağaza görüntüleri gözle incelendi. Runner kaydı 432 kare / 24 fps (18 saniye); salon top kaydı 121 kare / 24 fps (5,04 saniye), üç temas ve 1,95 m ilerlemedir. Kayıt sırasında test komutları kullanılmıştır; kaçırılan engeller videoda korunur.

Oturum sonunda asıl kayıt dosyası başlangıçla aynı SHA256 özetine sahiptir. QA, Play, native test ve No Throttling kapalıdır. Üç sahneli ev düzeni geri açıldı; aktif sahne LivingRoom_Level01, açık sahnelerde kaydedilmemiş değişiklik yoktur. Normal evde kamera / listener / EventSystem 1/1/1; `playModeStartScene=null` ve `DisableSceneReload` korunur. Ayrıntı `FinalSession.txt` içindedir.
