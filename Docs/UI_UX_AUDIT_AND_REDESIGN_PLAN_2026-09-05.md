# Cat Home — UI/UX incelemesi ve yenileme planı

> Tarihsel inceleme. Onaylanan tasarım 6 Eylül'de uygulandı: [uygulama kaydı](UIUX_IMPLEMENTATION_2026-09-06.md), [yeni gerçek oyun görüntüleri](QA/UIUX_2026-09-06/index.html). Aşağıdaki gözlemler önceki arayüze aittir.

5 Eylül 2026 · Durum: inceleme ve tasarım önerisi; uygulama yapılmadı.

## Tasarım değerlendirmesi

Mevcut arayüz, tamamlanmış bir premium oyunun tasarım bütünlüğüne henüz ulaşmıyor. İşlevler ve içerik bakımından zengin; ancak ekranların çoğu aynı renkli panel ve kapsül şablonunun tekrarından oluşuyor. Bu yüzden bazı yerler çok süslü olduğu hâlde genel deneyim basit ve hazır şablon hissi veriyor.

Önceki HD çalışması teknik netliği artırdı. Ekrandaki bilginin önceliğini, karakter kimliğini, ekranlar arasındaki tutarlılığı ve kullanıcı akışlarını çözmüş sayılmaz. Daha yüksek çözünürlük, yanlış kadrajı veya zayıf kompozisyonu kendi başına iyileştirmiyor.

**Önerilen yön:** Oyunun gerçek kedilerini ve odalarını merkeze alan; sıcak, renkli, neşeli ama kontrollü bir arayüz. Az sayıda belirgin vurgu, rahat okunan metinler, iyi boşluk dağılımı ve her ekranda anlaşılır bir ana eylem. İşlenmiş düğme yüzeyleri korunmalı; her bilgi satırının ayrı bir parlak düğmeye benzemesi sona ermeli.

## İnceleme kapsamı ve kanıtın sınırı

Ana menü, ev, gezinme, görevler, kedi kişiselleştirme, odalar, mağaza ve satın alma katmanları, hesap/ayarlar/gizlilik, liderlik tablosu, iki mini oyunun giriş/HUD/öğretici/duraklatma/sonuç/can bitmesi durumları, ilk tanışma ve kutlamalar incelendi. **41 kullanılabilir GameView karesi** 1920×1080 olarak kaydedildi. [Görsel galeri](QA/UIUX_2026-09-05/index.html) her kareyi açıklamasıyla gösterir.

- Normal menüler mevcut kayıtla açıldı. Canlı görülen ekranlar aşağıda **C** ile işaretlidir.
- İlerlemeyi sıfırlamak, can harcamak, ürün almak veya ödül toplamak gerektiren durumların mevcut UI bileşenleri geçici örnek verilerle gösterildi: **Ö**. Bunlar yeniden tasarım görselleri değil, mevcut bileşenlerin önizlemeleridir.
- Mini oyun HUD ve sonuç kareleri gerçek bir koşu/avın kaydı değildir. Arka plandaki boş parkur/arena, oyun dünyasının içerik yoğunluğuna dair kanıt sayılmadı. Örnek skorlar ve seviye ödülleri kullanıcı kaydına uygulanmadı.
- Ödüllü reklam, gerçek platform ödemesi, Google giriş/çıkış/silme, ağ kesintisi ve sıfırdan oyuncu yolculuğu uçtan uca denenmedi. Bu durumlar için kaynak kod ve tasarım ihtiyaçları ayrıca değerlendirildi.
- Gerçek telefon dokunma alanları, klavye açılması, fiziksel ekran okunaklılığı ve uzun oturum performansı bu masaüstü incelemesinde ölçülmedi.
- `41_Cat_Speech_Preview.png` denemesinde konuşma balonu görünmedi; bu kare galeriye ve başarılı inceleme sayısına alınmadı. Balon/progress/aydınlatma gibi bağlama bağlı alt durumlarda ayrıca canlı kontrol gerekir. Dosya numaralarındaki boşluk bu nedenle vardır.

Oyunun üretim kodu, UI prefabları ve sahne tasarımı bu incelemede değiştirilmedi. `FREE TEST / GET` mevcut kullanıcı kararıyla açık olan içerik QA modudur; bunun varlığı yanlışlıkla ücretsiz ekonomi açıldığı anlamına gelmez. Yayına hazırlanırken geliştirme metinlerinin ayrılması planlanmalıdır; ekonomi bu incelemede açılmadı.

## Ortak sorunlar

### 1. Görsel öncelik zayıf

Başlık, açıklama, para, ana düğme, ikincil düğme ve sekmeler çok benzer parlaklık ve derinlik kullanıyor. Kullanıcı önce içeriği değil çerçeveleri görüyor. Ana menüde oynama eylemi; mağazada ürün; sonuç ekranında kazanılan ödül açıkça baskın olmalı.

### 2. Birden fazla karakter kimliği var

Ana menüde gerçek oyun modelleri, kedi günlüğünde farklı bir çizim, mini oyunlarda büyük gözlü ve farklı oranlı illüstrasyonlar kullanılıyor. Bir görsel tek başına kaliteli olsa da bütün oyunla aynı karakteri anlatmıyor. Aynı seçili kedinin yüzü, rengi ve karakteri menüden oyuna taşınmalı. Gerçek modelin iyi ışıklandırılmış, önden üç çeyrek kadrajı bütün portrelerin ortak kaynağı olabilir.

### 3. Düğme dili her yüzeye yayılmış

Tıklanamayan başlıklar ve bilgi alanları da büyük, kabarık düğmeler gibi. Seçili sekmeler bazı ekranlarda daha soluk, seçilmemiş olanlar daha dikkat çekici. Renkler çoğu yerde durum anlatmak yerine rastgele kategori rengi gibi çalışıyor. Ana eylem, yardımcı eylem, seçili durum, kilit, hata ve tehlikeli işlem için ayrı kurallar gerekli.

### 4. Metin ve dil tutarsız

Türkçe seçiliyken `HUNGER`, `QUESTS`, `COLLECT`, `EXIT TO MAIN MENU` gibi birçok İngilizce metin kalıyor. Uzun açıklamalar da büyük harfle yazılıyor. `CAT`, `CAT SHOP`, `CAT JOURNAL` farklı amaçları kolay anlatmıyor. `LIVE POLYPERFECT MODEL` gibi üretim ayrıntıları oyuncu arayüzüne taşmış durumda. Metinlerin bir sözlük ve tutarlı ton üzerinden yeniden ele alınması gerekiyor.

### 5. Yer kullanımı dengesiz

Bazı kartlar büyük boşluklar taşırken asıl görsel küçük kalıyor. Mağaza üst bölümü çok yer kaplıyor ve aynı anda az ürün gösteriyor. Birkaç popup, küçük içerik kartının etrafında ekranı kaplayan çok büyük dekorlar kullanıyor. Boşluk gerekli, fakat içeriğe göre düzenlenmeli; rastgele geniş paneller profesyonel bir kompozisyon oluşturmuyor.

### 6. Durumların tamamı aynı olgunlukta değil

Normal giriş ekranı daha derli toplu olsa da can bittiği, veri olmadığı, ödül kazanıldığı veya hesap durumu belirsizleştiği ekranlar aynı kaliteye ulaşmamış. Yenileme yalnızca en güzel ana ekranı üretmekle bitmemeli; bekleme, hata, boş, kilitli, yetersiz bakiye ve başarı durumları da aynı bileşenin parçası olmalı.

## Ekran bazında yorumlar

Öncelikler: **P0** anlaşılabilirlik/işlem doğruluğu kusuru; **P1** genel kaliteyi ve temel akışları doğrudan etkileyen yenileme; **P2** sonraki sunum ve içerik iyileştirmesi. Bunlar uygulama sırasını destekler, kullanıcı testiyle ölçülmüş puanlar değildir.

| Ekran / kanıt | Bugünkü değerlendirme | Önerilen düzenleme | Öncelik |
|---|---|---|---|
| Ana menü · 01 C | Büyük logo, durum kapsülleri ve çok sayıda güçlü düğme kedinin önüne geçiyor. Yardımcı seçenekler ana eylemle yarışıyor. | Kedilere geniş odak alanı; tek güçlü `Devam et`/`Oyna`; ikincil gezinme daha küçük; ayarlar, hesap ve yapımcılar yardımcı bölgede. | P1 |
| Yapımcılar · 02 C | Geniş karttaki genel tanıtım metni, ekranın vaat ettiği yapımcı bilgisini yeterince karşılamıyor. | Gerçek katkılar, kullanılan varlıkların gerekli atıfları ve sürüm bilgisi; kısa ve okunabilir düzen. Bilinmeyen isim uydurulmaz. | P2 |
| Yeni oyun / sıfırlama · 03 C | Sonuçları açıklayan metin iyi; fakat iptal ve sıfırlama benzer güçte, dekoratif kenarları birbirine çok yakın. | `İlerlemeyi sıfırla` gibi kesin fiil; ayrı tehlikeli işlem stili, güvenli iptal, belirgin boşluk. Ana menüde ikincil konum. | P0/P1 |
| Hesap seçimi · 04 C | Büyük seçenekler ve uzun açıklamalar ilk etkileşimi ağırlaştırıyor. | Kayıt koruma faydasını bir cümleyle anlat; Google ve misafir yollarını anlaşılır kıl; yükleniyor/iptal/hata durumlarını aynı kartta tasarla. | P1 |
| Ayarlar · 05 ve 15 C | Tüm satırlar aynı ağırlıkta; ses seçeneklerinin farkı yeterince açık değil. Bağlı hesap metni düğmede sıkışıyor. | Ses, erişilebilirlik, dil ve hesap grupları; açık anahtarlar; ses seviyesi için uygun kontrol; hesap durumunu eylemden ayır. | P1 |
| Gizlilik / oyuncu verisi · 06 C | Ayarlarda Google bağlı görünürken burada giriş gerekli mesajı görülüyor. Bağlantı ve senkronizasyon anlamları karışıyor. | Hesap bağlantısı, son eşitleme ve eşitleme hatasını ayrı göster. Silme için ayrı onay ekranı ve net sonuç açıklaması. | P0 |
| Geri dönüş özeti · 07 Ö | İhtiyaçların azalması, büyük bir ödül kutlaması görseliyle anlatılıyor. Üç sayı karşılaştırması tekrar oyuna girmeyi geciktiriyor. | Sıcak kısa karşılama, en önemli bakım ihtiyacı ve açık devam eylemi; ayrıntılı değişimler ikincil. | P1 |
| Ev HUD · 08 C | Üstte para, üç büyük ihtiyaç, ek ikonlar ve seviye; altta dock ve joystick. Oda görülebiliyor ama birçok gösterge eşit baskınlıkta. | Bakım ihtiyaçları daha kompakt; kritik ihtiyaç tek belirgin öneri; para ve koleksiyon ikincil; kedinin hareket alanı açık. | P1 |
| Hamburger menü · 09 C | Altı benzer satır, karışık dil ve mağazaya birden fazla giriş. Günlük adının neyi açacağı açık değil. | Gezinme haritası sadeleşsin; `Kedim`, `Görevler`, `Odalar`, `Ayarlar` tutarlı olsun. Alt dock genel mağaza işlevini korusun. | P1 |
| Görevler · 10 C | Geniş tekrar eden satırlar, küçük içerik alanı ve uzun alt durum metni. Günlük/bölüm hedefleri kolay ayrışmıyor. | Günlük ve bölüm grupları; görev ikonu, kısa amaç, okunur ilerleme ve gerçek ödül; tamamlanan görev daha sakin. Açıklama ile hedef sayısı birlikte doğrulansın. | P1 |
| Kedi günlüğü · 11 C | Gerçek seçili kediyle uyuşmayan çizim, yinelenen isim alanı ve ayrı bir kişiselleştirme dili var. | Irk seçimiyle aynı `Kedim` ailesi: gerçek kedi önizlemesi, isim, renk ve görünüm sekmeleri. Renk işlevi korunur. | P1 |
| Kedi/ırk seçimi · 12 C | Canlı model iyi bir temel; geniş boş alan, küçük alt portreler ve geliştirme metinleri zayıflatıyor. Dönen kedi arkadan görülebiliyor. | Tutarlı önden üç çeyrek ilk kadraj; büyük seçili kedi; okunur ırk listesi; sahiplik/seçim durumları ve kullanıcı dili. | P1 |
| Odalar · 13 C | Gerçek oda görselleri güçlü. Kalın dış çerçeve, tekrar eden durum metinleri ve mağazadaki ikinci oda sunumu fazlalık yaratıyor. | Aynı oda kartı iki yerde ortak kullanılsın; 16:9 fotoğraf, mevcut/kilitli durumu, koleksiyon ilerlemesi, tek ziyaret/açma eylemi. | P1 |
| Mini oyun seçimi · 14 C | Görsellerdeki kedi tarzı oyundan farklı; dar görsel alanları ve tekrar eden RUN/HUNT açıklamaları var. | Gerçek oyun sahnesi ve bizim kedilerle iki güçlü kart; süre, can, rekor ve tek oynama eylemi. Sıralama ikincil giriş. | P1 |
| Mağaza CAT · 16 C | Kategori adının ırk mağazasından farkı belirsiz. Kartlar ürünlerden çok metin ve çerçeveleri öne çıkarıyor. | `Kedi eşyaları` gibi açık yerelleştirilmiş ad; ürün görseli büyük; fiyat ve sahiplik kolay taranır. | P1 |
| Mağaza ROOM · 17 C | Geniş üst bölümden sonra çok az ürün görünüyor. Fotoğraf küçük, satır geniş ve metin yoğun. | Daha kısa başlık; dengeli kart ızgarası; oda bağlamı; ürünün otomatik ekleneceği yer hakkında kısa görsel bilgi. | P1 |
| Mağaza HOME · 18 C | Oda seçim ekranıyla benzer amaç için farklı bir sunum var. Oyuncu ziyaret mi satın alma mı yaptığını ayırmak zorunda. | Ortak oda görseli ve durum bileşeni; kilitliyse açma, sahipse ziyaret. Mağaza ve gezinme amaçları aynı kelime sözlüğünü kullansın. | P1 |
| Satın alma / yeterli jeton · 19 Ö | Küçük ürün kartının etrafındaki dev şeritler asıl içeriği bastırıyor; kur açıklaması yineleniyor. | Ürünü büyüt; iki para seçeneğini koru; dekoratif şeritleri dengeli kenar vurgusuna dönüştür; kur bilgisini bir yerde göster. | P1 |
| Eksik ön koşul · 20 Ö | Kitap seti isteyen kullanıcı bir anda `REQUIRED: BOOKSHELF` satın alma kartında. Neden açıklanıyor ama ilk ürün görsel odağını kaybediyor. | İstenen ürün → gerekli ürün ilişkisini birlikte göster. Hangi ürünün satın alınacağı düğmede ve görselde açık olsun. | P0/P1 |
| Elmas paketleri · 21 C | Aynı kartların tekrarı ve genel `GET PACK` metni tamamlanmamış mağaza hissi veriyor. | Gerçek platformdan gelen fiyat; en küçük uygun paket vurgusu; fiyat yükleniyor/kullanılamıyor/iptal/hata durumları. Sahte para fiyatı veya indirim eklenmez. | P1 |
| Elmas harcama onayı · 22 Ö | İki katmanlı dev arka plan içinde küçük metin odaklı bir onay. | Ürün küçük görseli, tam elmas fiyatı, açık iptal/onay; arkadaki işlem kartının odağını doğru bastıran ortak modal. | P1 |
| Liderlik tablosu · 23 C | Veri yokken büyük boş podyum, tekrar eden boş metinler ve çok sayıda renkli filtre hakim. | Dolu, boş, yükleniyor ve hata için bilinçli düzenler; dönem seçimi ortak segment kontrolü; kendi sıra kartı; ödül kuralları yardım alanında. | P1 |
| Runner giriş · 24 C | İki sütunlu düzen diğerlerinden daha iyi; illüstrasyon farklı oyun vaat ediyor, kurallar birkaç yerde tekrarlanıyor. | Aynı kedi/sanat dili; rekor, can ve tek başlangıç eylemi; kısa görsel kontrol özeti. Eve dönen düğmenin metni `Eve dön` olmalı. | P1 |
| Runner HUD · 25 Ö | Skor, mesafe, süre, jeton, şans, aşama ve kombo kapsülleri benzer güçte. | Ana skor, ikincil jeton, anlaşılır can simgeleri; aşama/kombo geçici geri bildirim. Oyun alanı açık kalır. | P1 |
| Runner öğretici · 26 Ö | Büyük metin kutusu ve atla düğmesi; hareketi okumak gerekiyor. | Kısa hareket gösterimi, hedef bölge, adım ilerlemesi ve doğru hareket sonrası geri bildirim. | P1 |
| Runner duraklatma · 27 Ö | Dev düğmeler ve boşluklar; tercih değiştirme ile oyuna dönme aynı önemde. | Bir güçlü devam düğmesi; ortak küçük ayar kontrolleri; ayrı çıkış. | P1 |
| Runner sonuç · 28 Ö | Sonuçlar tek metin bloğu gibi; kazanılan jeton ve yeni rekor yeterince özel hissettirmiyor. | Skor/ödül/rekor ayrı hiyerarşi; gerektiğinde yeni rekor kutlaması; tekrar oyna ve eve dön; doğrulanmış x2 ödül ayrı seçenek. | P1 |
| Runner can yok · 29 Ö | Reklam düğmesi ile pasif başlat düğmesinin yazıları aynı konumda üst üste. Kod iki nesneyi de görünür bırakıyor. | Aynı alanda yalnız uygun eylem görünür; bekleme süresi, reklam varsa kazanılacak can ve reklam yoksa açıklama ayrı durumlar. | P0 |
| Catch giriş · 30 C | Runner ile benzer şablon; büyük resim iyi alan kaplıyor ama gerçek kediden farklı. Kurallar ve alt satır tekrar ediyor. | Runner ile ortak giriş sistemi; gerçek Catch sahnesi, 60 saniye özeti, can ve başlangıç. | P1 |
| Catch HUD · 31 Ö | Runner'dan daha sade. `MICE • COINS` içindeki iki sayı hızlı bakışta kolay ayrışmıyor. | Süreyi temel al; fare ve jetonu ayrı okunabilir ikonlarla anlat; kombo ve süre uyarısını aynı geri bildirim diline bağla. | P1 |
| Catch öğretici · 32 Ö | Üst yönerge ile alttaki sürekli açıklama aynı bilgiyi yineliyor. | İşaretlenen fare, bir kısa yönerge, yakalama ile kapanan öğretim. | P1 |
| Catch duraklatma · 33 Ö | Runner'ın duraklatma ekranından başka düzen ve daha az seçenek. | İki oyunda aynı duraklatma bileşeni; devam/ayarlar/eve dön yerleri sabit. | P1 |
| Catch sonuç · 34 Ö | Sayılar tek blok hâlinde; en önemli kazanım belirgin değil. | Runner ile ortak sonuç ailesi; yakalama sayısı oyuna özel; jeton aktarımı ve rekor açık. | P1 |
| Catch can yok · 35 Ö | Runner ile aynı üst üste başlat/reklam düğmesi kusuru. | Ortak can durumu bileşeniyle birlikte çözülmeli. | P0 |
| İlk isim verme · 36 Ö | Portre yerine yakınlaştırılmış kahverengi bir doku parçası çıkıyor; giriş alanı çok geniş. | Gerçek kedi yüzünü çeken ortak portre; kısa isim sorusu; boş/geçersiz/uzun isim ve mobil klavye durumları. | P0/P1 |
| Tanışma konuşması · 37 Ö | Aynı bozuk portre; uzun alt panel ve düşük belirginlikte devam ipucu. | Daha kısa konuşma, anlaşılır devam, gerçek kediyle ilişki; hedeflenen HUD bölümünü gerektiğinde işaretle. | P0/P1 |
| Bakım öğreticisi tamamlandı · 38 Ö | Gerçek kedi önizlemesi ışınların ortasında çok küçük kalıyor. | Kediyi kutlamanın ana görseli yap; kazanılan ödül ve açılan oyunu ayrı kısa bilgilerle sun. | P1 |
| Ev seviyesi · 39 Ö | Seviye madalyası anlamlı; başlık ve alt başlık aynı bilgiyi tekrar ediyor. | Büyük seviye, belirgin gerçek ödül ve varsa yeni açılan içerik; tek toplama eylemi. Reklam alternatifi yalnız mevcutsa. | P1 |
| Koleksiyon tamamlandı · 40 Ö | Tamamlanan odanın görseli yok; geniş kartta genel koleksiyon sayısı ve iki metin satırı. | Odanın gerçek fotoğrafı veya kısa kutlama animasyonu; oda 10/10 ile genel koleksiyon ayrı; sonraki oda hedefi. | P1 |
| Bağlama göre aktivite düğmesi · 42 Ö | Sağdaki büyük eylem okunur; ilgili eşyanın nerede olduğu düğmeden anlaşılmıyor. | Yakındaki eşyaya hafif hedef vurgusu, kısa eylem adı; enerji engeli ve devam eden etkinlik için açık geri bildirim. | P1 |

### Özellikle doğrulanması gereken kusurlar

1. **Can bitmesi çakışması:** Her iki mini oyunun builder'ı başlangıç ve ödüllü reklam düğmesini aynı `430×94` alana koyuyor. `RefreshWelcome` başlangıcı yalnızca pasif yapıyor; reklam düğmesini ayrıca açıyor. 29/35 önizlemelerindeki çakışma kaynak kodla destekleniyor. Gerçek sıfır-can kayıt ve reklam sağlayıcısı kombinasyonunda da regresyon testi eklenmeli.
2. **Tanışma portresi:** `CatDialogueView.FindCatTexture()` kedi materyalinin dokusunu buluyor; `CatFaceUv` ile sabit bir parçasını yüz görseli gibi kesiyor. Yeni ırkların doku düzeni için bu gerçek bir portre yöntemi değil. 36/37'de sonuç görülüyor. Bütün ırklar ortak portre kameralarıyla doğrulanmalı.
3. **Hesap mesajı:** 05/15'te bağlı hesap, 06'da giriş gerekliliği görülüyor. Bu tek başına girişin bozuk olduğunu kanıtlamaz; eski veya ayrı bir bulut senkronizasyon mesajının yanlış bağlamda gösterilmesi de olabilir. Görsel durum modeli ve mesaj kaynağı birlikte düzeltilmeli.
4. **Yanlış dönüş adı:** Mini oyun girişindeki `EXIT TO MAIN MENU`, ev dönüş yolunu kullanıyor. Oyuncuya söylenen hedef ile açılan ekran aynı olmalı.

## Kaynak kod üzerinden incelenen ek durumlar

- Günlük görevler mevcut görev listesine ekleniyor. Günlük ve bölüm hedeflerinin görsel ayrımı güçlendirilmeli; var olan günlük sistemi sıfırdan yapılacak bir özellik gibi ele alınmamalı.
- Günlük giriş serisi ve ödül özeti mevcut; bu incelemede bunları anlaşılır biçimde sunan ayrı bir oyuncu ekranı bulunmadı. Başarı servisi için ses geri bildirimi var, fakat görünür bir başarı listesi/ödül geçmişi bulunmadı. Küçük bildirim ve ilerleme alanı öneriliyor; yeni zorunlu popup zinciri eklenmemeli.
- Veri silme mevcutta aynı düğmeye belirli süre içinde ikinci kez basma yaklaşımını kullanıyor. Sonuçları gösteren açık onay katmanı daha anlaşılır olur.
- `BrightnessPanelView` kaynakta bulunuyor, bu oturumun canlı evinde bileşen bulunmadı. Aktif kullanıcı yoluna bağlılığı doğrulanmadan yeni bir ana gezinme ekranı sayılmamalı.
- Konuşma balonu, ihtiyaç baloncukları, aktivite ilerlemesi, enerji engeli ve oda değişiminin yükleme anları, yenilemenin bağlama bağlı QA listesine dahil. Konuşma balonunu bu oturumda görünür olarak doğrulayamadım; buradan bir üretim kusuru sonucu çıkarılmadı.
- İşletim sisteminin Google hesap seçici, ödeme ve klavye ekranları uygulama içi tasarımın dışında; oyunun bu ekranlara giriş ve dönüş durumları ayrıca tasarlanmalı.

## Yeni ortak tasarım sistemi

| Alan | Önerilen kural |
|---|---|
| Renk | Cream/mint gibi sakin içerik yüzeyleri; aqua/coral ana eylemler; berry/lilac yardımcı vurgu. Her ekrana bütün paleti aynı güçte yayma. Navy metin ve kontrollü iç kontrastta kalsın. |
| Yüzey | Bir ana kart, ölçülü iç derinlik ve parlaklık. Aynı kartın içinde her satıra çok katlı rim ekleme. Başlık bilgi olarak, düğme eylem olarak görünsün. |
| Tipografi | Fredoka kimliğini koru. Başlık, gövde, yardımcı metin, sayı ve düğme için az sayıda tutarlı boyut. Uzun metinlerde cümle düzeni; büyük harf kısa vurgu ve eylemlerde. |
| İkon | Kanonik pati jetonu/elmas aynen korunsun. Yardımcı gezinme ikonları aynı optik kalınlık ve detay düzeyinde tasarlansın; emoji/rasgele glif karışımı olmasın. |
| Görseller | Gerçek ürün prefabı, gerçek oda ve gerçek seçili kedi. Kedi portrelerinde ortak ışık, kadraj, arka plan ve ölçek; materyal dokusundan portre kesme yok. |
| Eylemler | Bir baskın eylem; ikincil geri/iptal daha sakin. Jeton ve elmas gibi eşdeğer satın alma seçenekleri açık bir çift olarak sunulabilir. Silme/reset özel ve tutarlı. |
| Modal | Küçük onay, orta ayar/bilgi ve büyük koleksiyon/mağaza aileleri. Kapatma/geri davranışı tutarlı; alttaki etkileşim doğru engellensin. |
| Hareket | Geçiş, basılma, seçim, ödül aktarımı ve başarı için amaçlı hareket. Birden fazla yüzeyin sürekli parlaması/sallanması azaltılsın. Azaltılmış hareket aynı son düzeni korusun. |
| Durum | Her bileşen normal, seçili, pasif, yükleniyor, boş, hata ve başarı durumlarıyla tasarlansın. Sadece renk değişimiyle anlam taşınmasın. |

Bu plan, mevcut `LowPolyPanelGraphic`, `PremiumUiFactory`, `PremiumButtonFx` ve `PremiumAmbientSparkle` altyapısını geliştirerek uygulanabilir. Sabit oda yerleşimi, sahip olunan eşyaların görünürlüğü, iki para seçeneği, ön koşullar, güvenli ödül işlemleri ve mevcut premium varlıklar korunur. Yeni görsel kararlar kabul edilip uygulandığında eski görünümü zorlayan belge maddeleri kod ve testlerle birlikte güncellenmeli.

## Gezinme ve temel akış önerisi

Oyuncunun temel soruları: **Kedim nasıl? Ne yapabilirim? Ne alabilirim? Hangi odaya veya oyuna gidebilirim?** Menü bu sorular üzerinden kurulmalı.

| Yol | Önerilen deneyim |
|---|---|
| Açılış → ev | Kedilerle kısa canlı karşılama → kayıt varsa Devam et → gerekliyse kısa geri dönüş bilgisi → ev. Aynı bilgiyi art arda birkaç kartla anlatma. |
| Yeni oyuncu | Kediyle tanış → isim ver → bir bakım davranışını yaparak öğren → kısa kutlama → serbest ev. Hesap koruma isteği ilerlemeyi kesmeyecek uygun bir anda sunulur. |
| Ev → kişiselleştirme | Tek `Kedim` girişi → aynı gerçek kedi üzerinde isim, ırk ve renk. Ayrı özellikler tutarlı bir ailede birleşir. |
| Ev → ürün | SHOP → doğru kategori/oda → ürün → gerekiyorsa ön koşul → para seçimi/onay → tasarlanmış yerine eklenme → kısa ürün/kedi tepkisi. Sürükleme veya depolama adımı geri gelmez. |
| Ev → oda | ROOMS → mevcut/kilitli durum → ziyaret veya açık satın alma yolu. Satın alınan oda ile mevcut oda karışmaz. |
| Ev → mini oyun | Oyun seç → rekor/can ve açık başlat → öğretici gerekiyorsa bağlam içinde → oyun → sonuç → tekrar veya eve dön. Giriş ekranını açmak can harcamaz. |
| Ödül/bildirim | Küçük kazanç için kısa bildirim; seviye/oda tamamlama için kutlama. Aynı anda çok sayıda popup sıraya yığılmaz. |

## Uygulama planı

| Aşama | Somut teslimat | Tamamlanma ölçütü | Bağımlılık / kapsam |
|---|---|---|---|
| 0 — Durum kusurları | Can yok düğme çakışması, portre kaynağı, hesap mesajı ve dönüş metni düzeltme listesi | Her kusurun önce/sonra karesi ve gerçek durum testi | Küçük/orta; mevcut görünüm içinde çözülebilir |
| 1 — Tasarım temeli | Renk/yazı/boşluk/ikon/düğme/modal kuralları; ekran ve durum haritası; ana menü, ev ve mağaza için yüksek doğrulukta üç tasarım | Aynı oyuna ait görünen üç referans ekran; ana eylem ilk bakışta belli; gerçek kediler/ürünler kullanılmış | Orta; sonraki bütün ekranları yönlendirir |
| 2 — Ana deneyim | Ana menü, ev HUD, dock, hamburger, ayarlar girişi ve bağlama göre bakım eylemleri | Açılıştan bakıma ve gezinmeye tutarlı yol; oda/kedi için yeterli görsel alan | Büyük; 1 tamamlanmalı |
| 3 — Keşif ve mağaza | SHOP üç kategorisi, ürün kartı, oda kartı, ön koşul, satın alma/onay, elmas paket durumları; Kedim ailesi | Ürün doğru anlaşılır; seçili/owned/kilitli açık; otomatik yerleşim sonucu görünür; fiyatlar ve ödeme sözleşmeleri korunur | Büyük; ortak kart/modal sistemi |
| 4 — İlerleme ve hesap | Görevler, günlük/başarı geri bildirimi, geri dönüş, tanışma, isim, üç kutlama, gizlilik ve sıfırlama | Kısa ve amaca uygun pencereler; ödülün ne olduğu açık; hesap işlemleri tutarlı; popup kuyruğu kontrollü | Büyük; ortak modal/ödül sistemi |
| 5 — Mini oyun ailesi | Oyun seçimi, Runner/Catch giriş, HUD, tutorial, pause, sonuç, can yok ve liderlik durumları | İki oyun aynı tasarım ailesinde; oyun içeriği HUD'dan baskın; boş/hata/can yok dahil tüm durumlar temiz | Büyük; 1–4 bileşenleri tekrar kullanılır |
| 6 — Son kalite kontrolü | Türkçe/İngilizce tam metin taraması, oran/telefon/tablet, klavye, SafeArea, hareket, kontrast, gerçek cihaz akışları | Aşağıdaki kabul kapıları geçer; ana menüden en derin popup'a aynı kalite | Tüm aşamalardan sonra |

Takvim, tasarım referansları ve hedef cihazlar netleştikten sonra çıkarılmalı. Bugün gün/saat tahmini vermek, ekranların durum çeşitliliğini ve gerçek cihaz doğrulamasını olduğundan küçük gösterir. İlk uygulama paketi olarak **üç referans ekran + ortak bileşen sistemi** öneriyorum; bütün ekranları birden yeniden boyamak yeterli olmaz.

## Kabul ölçütleri

- Bir bakışta ana eylem, seçili durum ve ekranın amacı anlaşılır. Tıklanmayan etiketler düğme gibi görünmez.
- 1920×1080 referansta bağımsız etkileşim yüzeyleri arasında en az 16 px görünür boşluk; dekor dışındaki gerçek çakışma sıfır. Can yok, onay üstüne onay ve uzun metinler de taranır.
- Android dokunma hedefi en az 48×48 dp; normal küçük metinde en az 4.5:1, büyük metinde 3:1 kontrast hedeflenir. Bunlar bu incelemede ölçülmüş sonuçlar değil, uygulama kabul hedefleridir. [Android erişilebilirlik rehberi](https://developer.android.com/guide/topics/ui/accessibility/apps)
- 16:9, 20:9 ve 4:3; gerçek telefon/tablet, çentik/SafeArea ve ekran klavyesi. Piksel boyutu fiziksel dokunma boyutu yerine kullanılamaz.
- Türkçe ve İngilizce tam akışlarda dil karışması, kesilme ve üretim metni kalmaz. Fiyat/sayı/oda adı/can terimi tutarlı olur.
- Gerçek ürün ve oda görselleri yüksek çözünürlüklü, doğru oranlı ve doğru yöne bakar. Bütün 10 kedi ırkında portre ve kutlama kadrajı ayrıca kontrol edilir.
- Yeni oyuncu, geri dönen oyuncu, boş koleksiyon, tamamlanmış koleksiyon, sıfır can, yetersiz para, eksik ön koşul, bağlantı hatası ve reklam/ödeme sağlayıcısı yok durumları kapsanır.
- Satın alma ve ödül sonuçları bir kez uygulanır. Onay vermeden harcama, doğrulanmadan reklam/IAP ödülü veya sırf giriş ekranı açıldığında can harcaması oluşmaz.
- Azaltılmış hareket, ses ve titreşim tercihleri korunur. Hareketi kapatmak düzeni veya anlaşılabilirliği bozmaz.
- Düzenleme sonrası `PremiumUiOverlapTests`, ilgili EditMode/PlayMode kontrolleri, `LevelContentValidator` ve gerçek `GetWorldCorners` taraması çalışır. Geçen testler görsel onayın yerine kullanılmaz; her ekran gözle incelenir.
- Uygulama sonunda küçük bir kullanıcı denemesinde “kediyi besle, istediğin ürünü bul, odaya git, mini oyundan eve dön, sesi kapat” görevleri yardım almadan denenir. Gözlenen takılmalar raporlanır; başarı oranı test yapılmadan varsayılmaz.

## Bu incelemede korumaya değer bulunanlar

Gerçek oda fotoğrafları ve doğru ürün önizlemeleri, kaliteli pati jetonu/elmas varlıkları, canlı kedi gösteriminin temeli, renkli çocuk oyunu kimliği, iki para seçeneği ve mini oyun girişinde can harcamadan inceleme olanağı iyi bir altyapı oluşturuyor. Yenilemenin işi bu içeriği daha görünür ve anlamlı hâle getirmek.

## İnceleme kapanışı

Galeri 41 kullanılabilir kare içeriyor; bütün görüntü bağlantıları mevcut. Unity Play kapatıldı ve tam ev önizlemesi geri açıldı: `GameScene` + `CatHome_UI` + `LivingRoom_Level01`, aktif Living Room, bir etkin kamera, `playModeStartScene = null`, `DisableSceneReload`. `LevelContentValidator`: 0 hata, 0 uyarı. Bu sonuç içerik bütünlüğü kontrolüdür; bu raporda bulunan tasarım ve koşullu ekran kusurlarını geçersiz kılmaz. İnceleme kapsamında yeni test paketi çalıştırılmadı, üretim kodu değişmedi. Git commit/push yapılmadı.
