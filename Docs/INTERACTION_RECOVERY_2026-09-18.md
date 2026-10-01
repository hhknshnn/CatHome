# Cat Home — oynanış ve etkileşim düzeltmesi, 18 Eylül 2026

Çalışma başlangıcı 00:13:08, kesin bitiş sınırı 05:13:08 Türkiye saati. Bu görev için verilen beş saat yeniden başlatılmadı. Teknik kapanış 04:58:39 TR; geçen süre 285.5 dakika. Son seçili Play Mode 44/44, EditMode 30/30; içerik doğrulayıcı 0 hata / 0 uyarı. Beş saat sınırı korunmuştur.

## Oynanışın takılmasının nedenleri

İlk etkileşim sorgusu, büyük sıçrama kataloğunu ve değişmeyen ağırlıklı kaynak geometrisini o anda hazırlıyordu. Başlangıçtaki ilk koltuk sorgusu 410,93 ms sürdü. Katalog artık eşzamansız yükleniyor; 63 kaynak uç duruşun dört gövde bölgesi, oda/ırk hazır olma aşamasında zaman bütçeli küçük gruplarla hazırlanıyor. Sahnedeki güncel çarpışma sonucu saklanmıyor. İlk sorgunun bu 252 bölgeyi yeniden üretmediği ayrıca doğrulanıyor.

Hareketi engelleyen dönüş penceresinin açık olup olmadığı, her sorguda bütün yüklenmiş nesneler aranarak bulunuyordu. `WhileYouWereAwayPopup` etkin örnekleri ortak listeden sorguluyor. Pencerenin açıldığı karede kontrolü alması, gerçek Devam düğmesinde bırakması ve yok edilen nesnelerin listeden çıkması korunuyor.

Son tam oyun arayüzü + gerçek joystick testi: 1.127 kare; ortalama 7,117 ms, yüzde 95 değeri 10,376 ms, en uzun kare 14,488 ms. Önceki tam arayüz Profiler örneğinde ana iş parçacığı ortalama 27,013 ms idi; pencere sorgusu düzeltmesinden sonraki ayrı örnekte 9,874 ms oldu. Bunlar farklı ölçüm yöntemleridir; tek bir karşılaştırma yüzdesine çevrilmedi.

Son ilk soğuk sorgu 19,414 ms; kaynak hazırlama çağrısı en yüksek 5,086 ms, 67 karede hazır. İlk denemede eklenen 16 ms ek hedefi sağlanmadı. Son kabul, daha önce projede kullanılan 50 ms ilk etkileşim sınırını ve kaynak geometrisinin sorgu sırasında üretilmemesi koşulunu kullanır. Önceki 16 ms başarısız XML'leri korunur; 60 FPS için bütün ilk kullanım kareleri garantisi verilmez.

## Ortak temas ve çıkış çözümü

Gerçek ağırlıklı pati/uzuv derisi, özgün yüzey üçgenleri ve kaynak kemik erişimi birlikte değerlendirilir. Pati hedefi ve önceki duruş yalnız arama ipucudur; yeni karede tekrar ölçülür. Kemik boyları, eklem yerel konumları/ölçekleri ve karakterin sabit kökü korunur. Hedefler 2 mm sayısal temas, altı sorunlu yüzeyde 5 mm gerçek deri kabulü ve kaynakta 15 mm içinde temas eden patide en fazla 18 mm son açıklıktır.

Küvette gerçek pati tabanı ve aşağı/yukarı basış sırası; hamakta desteğin uzun ekseni; puf, şezlong ve yer minderlerinde ortak gövde/pati uyumu düzeltildi. Sıçrama hazırlığı gerçek destek yüksekliğini kullanır. İnişin dört uzvu da kaynak veriyle denetlenir. İlk koltuk düzeltmesinin açık çıkış yolu korunur. Dinlenmede büyük bir eğimin geçerli kaldığı sürece sabit tutulup bir anda bırakılması azaltıldı; küçük dönüş adayları önce denenir.

Yeni sıkı denetimde avlu fıskiyesi ve saksının özgün katı geometrisi katalogda eksikti. Ortak üretici artık bütün mağaza prefablarının etkin katı meshlerini toplar. Katalog 30'dan 99 meshe çıktı; eski 30 kaydın ikili geometri verisi birebir aynı. Yeni veri 40.312.156 bayt. Sahne, model veya prefab yerleşimleri bu turda değiştirilmedi.

## Özgün dokuz madde

| İstek | Son kanıt |
|---|---|
| Balkon çiçeğine pati | On ırkta gerçek temas, sabit kök ve temasın bırakılması geçti. |
| Açlık/susuzluk balonları | Ortak eşik ve ret, bütün oda/dil adaptörleri, gerçek tok/susamamış düğmeleri; iki ırkta normal kap ve mutfak öğünü geçti. |
| Fırın–halı seçimi | Altı yaklaşma yönü, Türkçe/İngilizce gerçek Isın/Kalk düğmeleri ve halı ayrımı geçti. |
| Ortak ısınma | Türler arası animasyon, yerelleştirilmiş balon, hafif görsel; duraklatma/iptal/doğal bitiş ve balon sahipliği geçti. |
| Genel çarpışma | Sekiz odada gerçek yürüyüş; on ırk × 15/30/60 FPS duvar/dönüş; köşeden kaçış geçti. Altı destek yüzeyi de geçti. |
| Avlu saksısı | “Saksıları incele”; gerçek davranışa uygun Türkçe/İngilizce geri bildirim, ilk/tekrar düğme ve eski düğmenin reddi geçti. |
| Yerelleştirme | Metin/katalog/format/fallback denetimleri ve gerçek TR/EN arayüz kontrolleri. Her ekranın elle tarandığı iddia edilmez. |
| Plak çalar | İki ırk, gerçek düğmeyle tekrar ON/OFF; tek patiyle tek değişim, odak/müzik tercihi/duraklatma/oda çıkışı geçti. Özgün yerel müzik korunur. |
| Ortak başlangıç/hizalama | Makul mevcut duruşta başlama, güncel fizik/enerji/ihtiyaç/yön tekrar kontrolü; eski düğmenin taşıma/engel sonrası reddi. Son turda 39/39 rutin tam çevrimi geçti. |

Metin ve plak müziği önceki turlarda eklenmişti; bu tur yeniden yapılmış gibi sayılmadı. Müzik kökeni `ArtSource/Audio/RecordPlayer20260916/PROVENANCE.md`. Yeni kullanıcı metni veya ses eklenmedi.

## Doğrulamanın sınırları

Altı sorunlu destek için 5 mm kabulü ile genel 39 rutin taramasının mevcut 25 mm kabulü farklıdır. Fıskiyedeki yaklaşık 9,008 mm eski içme duruşu ve saksıdaki yaklaşık 6,964 mm ölçüm altı yüzey testine ait değildir; bütün 39 eşya 5 mm altında diye sunulmaz. Bu tür kesişimler bu beş saatlik turdan önceki testlerde de vardı.

Minder geçişlerindeki en büyük ardışık gövde noktası farkı, ek yumuşatma öncesi yaklaşık 14,4 cm iken son ölçümde yaklaşık 10,5 cm oldu. Bu ölçüm özgün animasyon hareketini de içerir ve bütün duruş karelerinin görsel olarak kusursuz olduğunu kanıtlamaz. Kök kayması ve native uçuş sürekliliği ayrı test edilir.

Unity Profiler ilk DX12 oturumunda grafik sürücüsü hatasıyla çöktü; Unity DX11 ile tekrar açıldı. Bu çöküşün kullanıcının videosundaki takılmanın nedeni olduğu iddia edilmez. Başlangıçtaki sahne/kayıt baytları korunarak çalışıldı. Fiziksel Android cihaz performansı ölçülmedi.

## Son sonuçlar

- Son seçili benzersiz sonuçlar: **44/44 Play Mode, 30/30 EditMode**. Ara başarısız sonuçlar silinmedi. Esas dosya [native-final-manifest.json](QA/INTERACTION_RECOVERY_5H_2026-09-18/native-final-manifest.json).
- Sekiz odada 39/39 tam rutin, asılı koltuk 10/10 ırk; altı bildirilen yüzeyin her birinde tek tamamlanma, açık iniş ve kontrol iadesi. Bütün 39 rutin × on ırk matrisi değildir.
- Altı yüzeyde en büyük gerçek deri kesişimi 2,000 mm; desteklenen patide temas ihlali 0; en ağır destek çözümü 43,369 ms. [Ölçümler](QA/INTERACTION_RECOVERY_5H_2026-09-18/final-metrics.json).
- Asenkron ırk değişiminde eski testlerin sabit iki kare beklemesi yeni rig için yeterli değildi. Testler gerçek hazır olma durumunu bekler; çarpışma eşikleri gevşetilmedi. Çiçek kare bütçesi testi de aynı nedenle gerçek seçilen rigi bekler.
- İçerik doğrulayıcı 0 hata / 0 uyarı; C# derleme kontrolü temiz. Son gerçek arayüz denemesi ayrı QA kaydında yapıldı. [Editör kapanışı](QA/INTERACTION_RECOVERY_5H_2026-09-18/editor-final.json).

Son ekran kontrolünde otomasyonun Windows tıklamalarının hedefi doğru düğme olmasına rağmen Unity odağı etkin kalmadı; bu yüzden fiziksel tıklama başarı iddiası yok. Mevcut Devam et ve Kedime dön düğmelerinin olayları çağrıldı; gerçek joystick bileşeninin pointer olayıyla, kopyalanan kaydın özgün başlangıç konumundan duvar yanından çıkış görüldü. Konum, kamera ve ihtiyaçlar değiştirilmedi. İlk üç saniye dar yerde dönüş/çıkış, ardından beş saniyede 1,788 metre yürüyüş; kontrol kilidi yok. [Ham deneme](QA/INTERACTION_RECOVERY_5H_2026-09-18/manual-final-continued-walk.json), [gerçek oyun görüntüsü](QA/INTERACTION_RECOVERY_5H_2026-09-18/manual-final-gameplay.png). Bu kısa kontrol uzun süreli elle oynama veya cihaz testi değildir.

## Koruma ve kaydetme

2142 başlangıç dosyası karşılaştırıldı. Değişen kaynaklar, yeni kaynaklar ve üretilen katalog verisi [koruma raporunda](QA/INTERACTION_RECOVERY_5H_2026-09-18/preservation-final.json) tek tek listelidir. Oyun sahneleri, 320 prefab, 396 FBX ve 145 WAV başlangıç baytlarıyla aynıdır. Unity test koşucusunun geçici InitTestScene dosyaları oyun sahnesi sayılmaz; eksikler raporda ayrıca görünür.

- `cat-home-save.json`: `7ad76d3c0027326282b8b01c9070384faca00387b195ef13585f8bdd75a989a4` — başlangıçla aynı.
- `cat-home-save.json.recovery`: `7ad76d3c0027326282b8b01c9070384faca00387b195ef13585f8bdd75a989a4` — başlangıçla aynı.
- `cat-home-save-BACKUP-before-CP2-test.json`: `03d4fd1a6420ff0d6cd6213fe08ea57598038ec589ba7ca02475692036ea9a8d` — başlangıçla aynı.

16/16 kullanıcı tercihi ve editör ses tercihi geri geldi. EditorSettings ve testin değiştirebildiği üç ilgisiz varlık güncel başlangıç baytlarıyla korundu. Tarihsel kayıt yüklenmedi; QA kapanışındaki canlı kayıt referansı koruması korunuyor. Kaynak değişiklikleri diskte kayıtlıdır; ilgisiz canlı varlıkları yazmamak için toplu SaveAssets çalıştırılmadı.

Üç temiz normal sahne, tek kedi/kamera/ses dinleyicisi; Play/QA/çekim/derleme kapalı. Unity ve bilgisayar açık. Commit/push, APK, yayın, yeni video ve teslim arşivi oluşturulmadı. Yerel QA Git dışında kalır.
