# Salon interaction + UI master pass — 1 Ekim 2026

Başlangıç **14:39:17 UTC**, kesin sınır **17:09:17 UTC**. Kullanıcının yaklaşık 2,5 saatlik, yalnız salon görevi. Gerçek kapanış zamanı ve koruma sayıları `QA/LIVING_MASTER_PASS_2026-10-01/closure.json` içindedir. Esas kabul listesi aynı klasörde `native-final-manifest.json`; ara başarısızlıklar nihai kabul değildir.

## Etkileşimler

**Tırmalama direği:** Mevcut ToyScratch kaynağı ve gerçek uzuv/gövde kabulü kullanılır. Omuz yüksekliği, kol erişimi ve pati açıklığından temas yüksekliği, kısa aşağı tarama ve yan hedefler türetilir; ırk adına göre ofset yoktur. Kısa bakış, iki farklı üç-vuruş ritmi, göğüs katılımı ve sakin bitiş eklendi. Her vuruşta gerçek el kemiği ile gerçek yüzey arasında 25 mm altı temas gerekir. Kök sabittir; kaynak pelvis/arka destek koruması sürer. Sallanan oyuncak ve direk modeli değişmedi.

**Sınır:** İki pati her duruşta erişemediği için güvenli yakın pati alternatifi kaldırılmadı. Son küçük Persian döngüsü 2 sol + 1 sağ; orta Domestic Shorthair ve büyük Maine Coon döngüleri 3 sol + 0 sağdır. Dolayısıyla bütün boyutlarda sağ-sol dönüşümlü scratching hedefi tam karşılanmış değildir. Erişim veya fizik toleransları bunu zorlamak için gevşetilmedi.

**Tünel:** Gerçek deri silueti, uzuv ölçüsü ve tünelin gerçek MeshCollider açıklığı ölçülür. Başın gereken 0–32 derece alçalması ölçülmüş uygun adaydan seçilir. Giriş/çıkış gövde ön/arka sınırına göre dışarı alınır; giriş merkezine kısa gerçek adım ve mevcut doğal dönüş kullanılır. İçeride kök tünel eksenini izler; mevcut Walk/Crawl kaynağının fazı gerçek kat edilen metreye bağlanır. Çıkışta baş yumuşakça yükselir. Genel fizik toleransları değişmedi; tünelin dolu navigasyon kutusu yerine geçidin gerçek kumaş açıklığı ayrıca doğrulanır.

| Boyut örneği | İki tırmalama ritmi | Tünelin iki yönü | Yeni engelde başlamama | Kök kayması / tünel deri aşımı |
|---|---|---|---|---|
| Small — Persian | PASS, üçer temas | PASS | PASS | 0 / 0 |
| Medium — Domestic Shorthair | PASS, üçer temas | PASS | PASS | 0 / 0 |
| Large — Maine Coon | PASS, üçer temas | PASS | PASS | 0 / 0 |

Tünel kontrolü altı geçişte 712 örneklenen kare, 2.403.778 gövde/uzuv noktası içerir; bu maske kuyruk dışındadır. Tam kareler arası sürekli çarpışmasızlık veya bütün on ırkın kabulü diye genellenmez. Her olağan geçiş gerçek HUD ile başladı; engel deneyi başlamadan önceki taze kontrolü sınar.

## Diğer salon eşyaları

28 tam etkileşim geçti: mama, su, temel yatak; sehpa, berjer, kitaplık, kitaplar, lambader, tablo, bitki, TV, koltuk; 16 CAT ürünü. Yaylı oyuncak ve fare hareketleri yeniden tasarlanmadı, yalnız regresyon ve salon konuşmaları uygulandı.

Mevcut farklı profiller korundu: top sepeti/top pisti/çıngıraklı tekerde yuvarlama-itme; kurdele minderinde takip/çekme; iki mama bulmacasında koklama-itme-yeme; kedi çiminde koklama/dokunma; karton kutuda girme/saklanma; üç satın alınan yatakta girme/dinlenme/çıkma. Temas, sahiplik, ödül ve ekonomi sahipleri aynı kaldı.

## Konuşma ve görsel dil

**15 bağlam**, her dilde bağlam başına **3 öncesi + 3 sonrası**: tırmalama, tünel, yay, fare, mama, su, uyku, dinlenme, kurdele, top, çim, genel oyun, izleme, saklanma, bulmaca. Türkçe 90 + İngilizce 90 metin; toplam 180. Havuz sıralı döner ve art arda aynı cümleyi seçmez. Başlama öncesi metin yalnız gerçekten başlayan eylemden sonra gösterilir; başarısız tıklamaya sahte başarı verilmez.

ImageGen ile tek transparan pearl/cream/navy/gold/cyan atlas üretildi. Affinity'de gerçek alfa ve görünüm incelendi, düzenlenebilir yerel kaynak `ArtSource/Affinity/LivingMasterPass/Living_Pearl_Atlas_Source.af` olarak kaydedildi. Kaynak PNG ve Unity PNG pikselleri aynıdır. Balon, popup ve ay/yıldız bu aileyi paylaşır. Baş üstü balonun mevcut ölçüsü/hit davranışı korunur; Nunito gövde yazısı, ince kenar, yumuşak giriş/çıkış ve hafif salınım kullanılır. Modal açıldığında dünya balonu gizlenir. Diğer odalarda eski yüzey/font/renkler geri gelir.

Yenilenen salon yüzeyleri: **Welcome Back**, **seviye ödülü**, **koleksiyon tamamlama**, **küçük başarı/ödül bildirimi**. Mevcut modal akışı, ödül toplama sahipliği ve düğme hit alanları korunur. Bildirim metni Türkçe glifleri hazır Nunito ile sunulur. Test metninde yakalanan kodlama bozukluğu ayrıca düzeltildi; bu ara görüntüler nihai değildir.

848×392, 1920×1080 ve 2400×1080 gerçek Game View kontrolleri; balon TR/EN üç varyasyon, tüm 15 havuz, TR popup/toast, modal balon gizleme, gerçek kapatma/toplama tıklamaları. Nihai seçilen XML'ler manifesttedir.

## Uyku ve yerleşim

Uyku **mevcut Polyperfect Sleeping döngüsü + yeni eklem dönüşü katmanı**dır; yeni FBX/animasyon klibi üretilmedi. Gövde yana dinlenir, boyun/baş patilere doğru gevşer; mevcut gerçek yüzey oturtması sürer. Temel yatak ve uygun sürekli uyku yüzeyleri kullanır; oturma olarak tanımlı berjer pozu zorla uykuya çevrilmez. Uyanmadan önce yan poz kısa bir geçişle çözülür. Yeni ay/yıldız, küçük z, sakin salınım/pulse ve fade kullanır; azaltılmış hareket tercihi korunur.

Bonus seramik mama kabı, generic ön alan yerine gerçek FoodBowl konumuna bağlı bakım ceplerinde planlanır. Örnek konum **(-0,45; 0; 2,23)**, giriş **(-0,45; 0; 1,57)**. Otomatik satın alma/yerleştirme korunur; sürükleme eklenmedi. Mevcut **en fazla beş CAT / bir satın alınan yatak** kuralı doğrulandı. 3.432 izin verilen beşli koleksiyonun tamamı; ayak izi aralığı, bakım girişleri ve bağlantılı yürüme alanı kontrollerinden geçti. Sahne, model, prefab, kamera, ışık ve HUD yerleşim dosyaları değiştirilmedi.

## Video ve sınırlar

`QA/LIVING_MASTER_PASS_2026-10-01/CatHome_Salon_MasterPass.mp4`: **28 saniye, 1920×1080, 24 FPS, 672 kare, sessiz**. Gerçek Unity Game View'dan kaydedildi. Tırmalama → tünel → iki konuşma → yatağa giriş/uyku → Welcome Back → seviye ödülü. Hazırlık yerleştirmeleri kaydedilmedi; kesmeler videoda açıklanır. Sayaçlar ayrı QA kopyasının test değerleridir.

Unity VideoPlayer dosyayı baştan sona oynattı: 672 frame-ready olayı, en yüksek kare 671, end olayı. MP4 atomları ayrıca 672 örnek ve 27,999958 saniye gösterir. Windows Media Foundation belirtilmemiş renk primarileri için varsayılan renk dönüşümü uyarısı verdi; görüntü incelendi, renk ölçümü yapılmadı.

Kalan ana sınır orta/büyük kedide her duruşta çift-pati scratching sağlanmamasıdır. Telefon/FPS/uzun ısınma veya bütün ırklarda uyku derisi kabulü yoktur. Bu tur diğer odaları genişletme yetkisi değildir.

## Koruma

Güncel başlangıçtaki 7.911 okunabilen dosyanın **7.898'i aynı, 13 mevcut runtime C# değişik, 17 yeni dosya, eksik 0**. Yeni dosyalar altı runtime C#, bir test, atlas ve metalarıdır. Bir eski Eat klibi başlangıçta kabuktan okunamadı. **Dört gerçek kayıt ve 16 tercih aynı**; tarihsel kayıt yüklenmedi. İki font önbelleği yalnız bu turun hash doğrulanmış başlangıcına döndü. Tam dosya karşılaştırması `preservation-final.json`, tercihler `preferences-final.json`, son editör durumu `editor-final.json` içindedir.

Nihai manifestte **11 EditMode + 8 benzersiz PlayMode** kontrolü, oran tekrarlarıyla **21 seçili PASS çalıştırması** vardır. Son editör durumunda üç temiz normal sahne, tek etkin ses dinleyicisi, Play/QA/derleme/profiler kapalıdır; Unity DX11 açık bırakıldı.

İlk on-ırk testinde editör takıldı; yalnız doğrulanmış CatHome editörü yeniden açıldı. Log ve bu tur oluşan kurtarma/test sahneleri QA altında saklandı. Tünel ölçümü, test fontu/kodlaması, ilk kayıt duruşu ve video frameCount yuvarlama tanıları nihai başarısızlık sayılmaz; geçerli son koşular manifestte açıkça seçilir.

APK, telefon kurulumu, commit, push veya yayın yapılmadı. Son durum ve durulan zaman için `closure.json` esas alınır.
