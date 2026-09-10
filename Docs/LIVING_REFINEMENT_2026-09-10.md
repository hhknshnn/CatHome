# Salon — duvar hizası ve baş/kap açıklığı, 10 Eylül 2026

Kullanıcının son salon isteği uygulandı. Önceki `LIVING_CARE` teslimindeki yalnız ağız uzaklığı kontrolü, başın kabın içinden geçmesini yakalamamıştı. Bu adımda baş ve çene yüzeyinin kap kenarı bandındaki gerçek köşeleri de ölçülür. Önceki sol duvar yerleşimi güncel değildir.

[Güncel galeri](QA/LIVING_REFINEMENT_2026-09-10/index.html): dokuz gerçek Unity PNG'si ve iki normal hızlı, 24 fps tam rutin videosu. Yakın çekimlerde yaklaşma, yeme/içme ve açık zemine dönüş birlikte görünür.

## Yerleşim

- Mama/su istasyonu arka duvarda `(-2.645,0,2.375)`, yaw0. Beyaz arkalık arka panele yaklaşık 3.5 mm açıklıkla paralel. Katı taban ve yürüyüş engeli aynı gerçek genişletilmiş hacme oturur; taban Y.06.
- Kapların yerel merkezleri X±.28, Y.06, Z−.36. Kedi Y.06 taban üzerinde, kaba .28 m uzakta ve yaw100 ile yandan görünür. Girişler açık ön zemindedir; en kısa dönüş ve sahipli iptal/çıkış korunur.
- İçerik yüzeyleri gerçek görünür geometriyle yükseltildi: mama +.030 m, su +.028 m. Temas hedefi gerçek üst üçgenden alınır. Kedi ölçeği, kemik bağları ve uzunlukları değişmedi; dört pati desteği korunur.
- Kitaplık/kitap seti `(-1.17,0,2.36)`; uyku minderi `cat.nap-pillow` `(-.06,0,2.4387)`, yaw0. Minderin gerçek arka kenarı Z2.72 paneline sıfırdır. Yalnız bu ölçülmüş yerleşimin arkasındaki boş doğrulama payı çıkarılır; ön/yan, diğer mobilya ve giriş güvenlik payları korunur.
- Ana kedi yatağı X1.00 ile berjerin yanındadır; tablo aynı X1.00 merkezine hizalıdır. Yatağın arka panel teması ve tablo yüksekliği korunur.
- Otomatik CAT yerleşimi mevcut gösterilen kümenin geçerli kayıtlı diğer dört konumunu önce dener. Uyku minderi kendi duvar konumunu önceler. Geniş kapalı yatak alternatifleri bakım koridorunu açık tutan ön-sol noktayı önceler. Salon 5 CAT / en fazla1 CAT yatağı; sahiplik/depo listeleri aynı.

## Doğrulama

Son tam EditMode **511/511**, bunun içinde **4.147 izinli beşli eşya kombinasyonu** ve minderin gerçek duvar kenarı/girişi. Son benzersiz native sonuçlar **11/11**: on ırk × iki kap **20/20**, baş/çene kenar açıklığı, gerçek ağız ve pati desteği, kemik/kök ölçüleri, yakın yönlerden başlama, pause/iptal, ihtiyaç sahipliği/tek tamamlanma, ana yatak ve bakım istasyonu geçiş güvenliği.

Ölçülen ağız hedefi uzaklığı en fazla **0.02463 m**; kap kenarı bandındaki baş yüzeyi açıklığı en az **0.00778 m**; son gövde görünürlük dot değeri en az **0.42949**. Kök görünürlük güven payı .30 korunur. Başın o banda girmediği örneklerde açıklık Infinity olarak yazılır; ağız örneği ve gerçek beslenme süresi ayrıca zorunludur. Bu ölçüm bütün kafaya ait sürekli bir fizik çarpışma çözümü olarak sunulmaz.

Gerçek Play'de minder giriş → tutulan dinlenme → Kalk → kontrolün bırakılması ayrıca başarılıdır. Seçili Russian Blue dinlenirken bütün son mesh ile duvar arasında en az .03469 m açıklık ölçüldü. 103 ürün kartı ve sekiz oda ön izlemesi yenilendi; validator **0 hata / 0 uyarı**. Diğer yedi oda sahnesinin hash'i aynı. Ara başarısız XML'ler tarihsel hata ayıklama kaydıdır; sonuç kaynağı `EditMode-verified.xml` ve `native-test-summary.json`.

## Bırakılan durum

QA/Play kapalı, derleme yok. GameScene / CatHome_UI / LivingRoom_Level01 temiz; tek kamera/ses dinleyici, gerçek kaydı yalnız okuyan editör ön izlemesi açık. 16 tercih ve varlık bayrakları birebir geri yüklendi. Gerçek kayıt/recovery başlangıç ve bitiş **C9E982B6C3066948FB8AF16D0E2559578ECE9B808A59D83F39671905A305AF76**; CP2 de başlangıçla aynı. Eski hash geri yüklenmedi. APK, arşiv, commit/push, canlı yayın veya kapatma yapılmadı; cihaz performansı ölçülmedi.

Her sonraki iş yine kısa sıralı plan ve tahmini süreyle, tek salon konusu üzerinden ele alınır.
