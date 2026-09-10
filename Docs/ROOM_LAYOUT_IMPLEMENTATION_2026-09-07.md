# Odalarda ortak sabit yerleşim — 7 Eylül 2026

Sonraki mimari kalite ve etkileşim geçişi, bu yerleşim ve ölçek sözleşmesini korur: [oda kalitesi ve etkileşim raporu](ROOM_POLISH_INTERACTIVITY_2026-09-07.md). Bu belgedeki test sonuçları yerleşim çalışmasına aittir; en yeni çalışma sonuçları bağlantıdaki rapordadır.

Kullanıcının kararı: her oda kendi mobilya ve oyun eşyalarını kullanır; 17 ürünlük CAT koleksiyonu yalnız salonda kalır. Salonun onaylı düzeni ve beş görünür CAT ürünü / en fazla bir yatak sınırı korunur. Diğer yedi odanın 70 ürünü satın alma sırasından bağımsız olarak tam koleksiyon birlikte düşünülerek yerleştirilir.

## Uygulama

`HomeRoomLayoutPlanner` bütün koleksiyonu birlikte çözer. Büyük silüetler arka veya yan duvarlara, alçak etkileşimler görülebilir yan/ön alanlara, zemin örtüleri açık orta alana gelir. Orta yürüyüş hattı en az 1 m genişlikte korunur. Ürün hacimleri arasında 0.35 m pay, girişlerde 0.30 m yarıçap ve ayrı girişler arasında en az 0.62 m hedeflenir. Girişler ve animasyon destek noktaları ortak oda kamerasından başka ürünün gövdesiyle kapanmamalıdır. Kadraj kontrolü yalnız pivotu değil kedinin gövdesi için yatay/dikey payı da içerir. Ulaşılabilir zemin noktaları seçildikten sonra gerçek rutin girişleri yeniden ayrılır; ücretsiz oda aktiviteleri de bu denetime dahildir.

Kapı/pencere hacimleri ve görünürlük noktaları ayrıca korunur. `HomeRoomLayoutObstacle` işaretli sabit mimari de çözümün parçasıdır; avlu saksıları, üst kat merdiven korkuluğu ve bahçe ağacı bu kapsamdadır. Bahçedeki sabit ağaç arka sol köşeye alındı ve oranlı küçültüldü; kuş yuvası ile kuş izleme hedefi birlikte taşındı. Ön sol/sağ bahçe çitinin çizimi, balkon ön korkuluğundaki gibi kesit olarak açıldı; yan/arka çitler ve fizik sınırları korunur. Böylece ön sıradaki ızgara ve kuş banyosu etkileşimleri çitin arkasında kalmaz.

Eşyalar tekdüze ölçüye zorlanmaz: modelin oranı korunarak kategoriye uygun bir ölçek uygulanır. Yeni katalog ürünleri de en fazla 2.45 m taban uzunluğu / 2.12 m silüet yüksekliği sınırına uyar. Görsel, katı collider, hareketli parça ve temas noktaları beraber ölçeklenir; ürün kökü 1 kalır; kedinin mevcut ölçeği korunur. Gerçek üst üçgenlerden destek yüzeyleri yeniden ölçülür. `RoomProductScaleStamp` aynı düzenleme tekrar çalıştırıldığında ikinci kez küçülmeyi engeller.

| Oda | Yerleşim yönü |
| --- | --- |
| Banyo | Duş ve depolama arkada; lavabo/küvet yanlarda, kapı açık; küçük bakım oyunları önde. |
| Mutfak | Pencere altında alçak tezgâh; yüksek depolama kenarlarda; tabure, mama alanı ve küçük oyunlar ayrı. |
| Yatak odası | Pencere ve kapı açık; büyük yatak/şezlong kenarlarda; salıncak, minder ve sepetin kendi girişleri var. |
| Bahçe | Yüksek pergola/ağaç arka alanda; çim üzerinde açık yürüyüş hattı, alçak oyunlar yanlarda. |
| Balkon | Arka kapı açık; raf ve asılı ürünler kenarlarda; oturma/ekim alanları orta geçişin iki yanında. |
| Avlu | Şemsiye masadan ayrı; kemer ve salıncak arkada; masa, ateş çukuru ve çeşme farklı alanlarda. |
| Üst kat | Kitaplık pencereyi örtmez; çalışma ve dinlenme alanları karşılıklı; ön merdiven görüşü hesaba katılır. |

Mağaza fotoğrafları 1024×1024 olarak son prefablardan yenilendi. Lavabo, duş, pencere divanı ve komodinin arka yüzü gösteren eski fotoğraf yönleri de düzeltildi; bu dört ürünün açık/işlenmiş yüzü artık kartta görünür.

Duşun gerçek ön camı prefab +X tarafındadır. Eski +X giriş ve duruş, kediyi camın içinden geçiriyor ve oturuşu gizliyordu; giriş -.33, duruş -.30 ham X değerleriyle açık -X yarısına alındı. Ölçek bu noktalara birlikte uygulanır; banyo matrisi bu son değişiklikten sonra ayrıca çalıştırıldı ve geçti.

## Mevcut kayıtlar ve kalıcı kural

`Assets/Resources/Home/RoomLayoutCatalog.asset` sonuçları saklar. `StoreCatalogAssets` güncel konum/yön/footprint/yükseklik için bu kaynağı okur; `WithoutRoomLayout()` modelin ham üretim ölçülerini korur. `HomeProductPlacement` mevcut sahiplik kaydını değiştirmeden satın alınan ROOM ürününü oda planına döndürür. Satın alınmamış ürün için ayrılmış alan başka ürün tarafından kullanılamaz. Fiyatlar, satın alma ön koşulları, ürün kimlikleri ve save şeması değişmez.

`StoreProductContentBuilder.BuildRoomSceneProducts` yeni bir koleksiyonu görünce `EnsureRoomPlan` ile aynı kurala göre planlar, prefab ölçeğini uygular ve ulaşılabilir giriş/çıkışları pişirir. Yeni oda için rastgele koordinat listesi yazılmaz. Fiziksel olarak sığmayan koleksiyon işlem hatası verir; eşyaları üst üste koyan sessiz bir yedek yol yoktur.

## Yeni oda ekleme sözleşmesi

1. Oda `HomeRoomShellMetrics`, `HomeRoomBoundary`, `HomeRoomService.Rooms` ve ortak `HomeRoomCameraProfile` sözleşmesini kullanır. Kamera ve alt gezinme ayrımı [kamera raporundadır](SHARED_ROOM_CAMERA_2026-09-07.md).
2. ROOM ürünlerinin kimlikleri, doğru ham ölçüleri, duvar/zemin türü, inşa edilmiş prefabı ve gerçek `CatActivity` giriş/destek noktaları katalogda tanımlanır. Modelin gerçek ön yüzü prefab üzerinde ölçülür.
3. Kapı/pencere grupları isimleriyle tanınır. Yürüyüşü veya görünürlüğü etkileyen diğer sabit mimari köklerine `HomeRoomLayoutObstacle` eklenir; bütün odayı kapsayan duvar/zemin köküne eklenmez.
4. `BuildRoomSceneProducts` eksik oda planını otomatik oluşturur. Koleksiyon veya mimari sonradan değiştiğinde `HomeRoomArrangementBuilder.PlanAll()` ve `ApplyAll(false)` yeniden çalıştırılır. Önce bütün koleksiyon için uygun çözüm bulunur, sonra değişiklikler uygulanır.
5. `LevelContentValidator` oda kimliklerini, plan konumlarını, ölçek damgasını, yürüyüş hattını, aralıkları ve CAT ayrımını denetler. Yeni oda özel bir isme bağlı istisna gerektirmez.
6. `StoreCatalogPreviewBuilder.BuildAll()` ile mağaza fotoğrafları, `RoomPreviewCaptureBuilder.CaptureSilently()` ile oda fotoğrafları yenilenir. Ürünler fotoğrafta kaynak FBX'ten değil oyuncuya verilen son prefablardan gösterilir.
7. EditMode, tüm ırklarla native etkileşim testleri, gerçek kamera görünürlüğü ve farklı ekran oranlarında HUD kontrolü tamamlanır. Testler `UiQaTestSession` kayıt kopyasını kullanır; gerçek sahiplik/ödüller değiştirilmez.

## Doğrulama durumu

Tamamlandı. [Gerçek HD görüntüler ve test sonuçları galerisi](QA/ROOM_LAYOUT_2026-09-07/index.html) bütün odaların geniş/dar ekranlarını, 80 ürünün gerçek oda kamerasından etkileşim karelerini, yakın temas incelemelerini ve son mağaza/oda seçici görüntülerini içerir. Bunlar Unity çıktısıdır; tasarım maketi değildir.

- Tam EditMode: **450/450 geçti** (`EditMode.xml`).
- PlayMode: **14/14 ayrı test geçti** (`PlayMode.xml`). Sekiz oda × on ürün × on ırk = **800/800 rutin**; başlangıç/bitiş, poz/iskelet hareketi, temas ve açık çıkış kontrol edildi. Son duş düzeltmesinden sonra banyonun 100 rutini ve bütün odaların yakın/uzak/engelli giriş testi ayrıca **2/2** geçti (`PlayMode-bathroom-final.xml`); bu iki tekrar ayrı 16 test olarak sayılmaz. Galerideki sekiz CSV son sonuçları taşır.
- Yakınlık taraması: **80 ürünün 81 girişi**; yakında doğru eylem, uzakta ve engelli yaklaşımda kapalı eylem (`room-proximity.csv`).
- Normal oda geçişleriyle **sekiz oda × iki ekran oranı = 16 canlı kare**; her karede tek kamera / AudioListener / EventSystem. 1920×1080 ve 1440×1080 taramaları ile mağaza ve oda seçicinin iki ek taramasında görünür düğmeler için **0 çakışma, 0 ekran dışına taşma, 0 merkezden tıklama engeli**.
- `LevelContentValidator`: **0 hata / 0 uyarı**. 103 mağaza fotoğrafı 1024×1024 olarak, sekiz oda fotoğrafı da son yerleşimle yenilendi. Kartlar ve gerçek oda etkileşim kareleri gözle incelendi.
- Canlı QA, `Library/UiQaSession` altındaki ayrı kopyada tam ROOM koleksiyonları açık örnek sahiplikle yürütüldü. Asıl kayıt dosyasının SHA256 özeti tur öncesiyle Play çıkışı sonrasında aynı kaldı. QA sona erdirildi; `GameScene` + `CatHome_UI` + etkin `LivingRoom_Level01` düzenleme görünümü geri açıldı.

Projeye ait 32 mevcut Markdown dosyasına güncel karar ve bu raporun bağlantısı eklendi; tarihsel kayıtlar silinmedi. Bu yeni rapor ve dış çalışma alanının `AGENTS.md` kurallarıyla birlikte **34 belge** güncellendi/oluşturuldu. Üçüncü taraf paket/lisans belgeleri bu sayıya dahil değildir. Gerçek cihaz performansı bu çalışmada ölçülmedi; görsel ve etkileşim doğrulaması Unity Editor üzerinde yapıldı.

Kanıt dizini: `Docs/QA/ROOM_LAYOUT_2026-09-07`. Önceki raporlardaki test sayıları kendi tarihli çalışmalarına aittir; bu geçişin doğrulaması yerine sayılmaz. Git commit/push kullanıcı tarafından yapılır.
