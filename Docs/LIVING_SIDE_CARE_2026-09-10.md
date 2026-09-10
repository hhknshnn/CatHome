# Salon — ince platform ve zeminde yandan bakım, 10 Eylül 2026

Kullanıcı geniş platformu küçültmemizi, kapları ayırmamızı, aralarındaki koyu çizgileri kaldırmamızı ve kedinin standın üstüne çıkmadan kameraya yandan görünerek yemesini/içmesini istedi. [Güncel galeri](QA/LIVING_SIDE_CARE_2026-09-10/index.html): 9 gerçek Unity PNG'si ve iki normal hızda24fps tam rutin videosu.

## Uygulanan düzen

- Platform **1.40×.55m**, önceki1.775×.871m'ye göre yüzey alanı yaklaşık**%50 küçük**. Taban ve inset toplam yüksekliği .060→.028m, arkalık .30→.17m. Tek yüzey korundu; iki kap modelindeki `MintMat` parçaları kaynaktan kaldırıldı. Siyah/yeşil görünen altlık kenarları yok.
- İstasyon **(-3.445,0,2.02)** yaw270. Gerçek arka uç Z2.72 karşı panele sıfır, uzun arkalık TV yan duvarına paralel. Kap merkezleri **78 cm**, önceki56 cm'den22 cm daha açık. Mama(-3.325,.018,1.50), su(-3.325,.018,2.28). Kaynak Blender dosyaları ve FBX'ler birlikte güncellendi; ayrı Blender arayüzündeki çalışma değiştirilmedi.
- Beslenme kökü **zeminY0**, kap çevresinde .28m ve dünya yaw245. Gövdenin son kalça→omuz yönü kameraya yaklaşık**82.1–84.3°**: yandan okunur. Dört gerçek pati teması standın önündeki odanın zemininde; stand destek yüzeyi sayılmaz. En küçük yatay pati–stand açıklığı **0.00408m**. Yürüme engeli dar görsel hacme uyar; içine tırmanma ve dar cep güvenliği korunur.
- Baş, çene ve gerçek içerik teması yeni yön/yükseklikle birlikte ayarlandı. Suda rastgele üçgen merkezi yerine ağız doğrultusunun gerçek yüzey üçgeni üzerindeki izdüşümü kullanılır; hedef hâlâ suyun gerçek yüzeyindedir. Kararlı yüzey seçimi, kısa dönüş, ihtiyaç sahipliği, pause/iptal ve açık girişe dönüş korunur. Diğer oda ve salon eşyaları, minder/kitaplık/yatak aralıkları, CAT5/1 aynı.

## Bu adımın doğrulaması

**4/4 native:** `LivingBowlContactTests` üç test ve `ProductionCareTests.PairedCareTray_BlocksNarrowPocketsAndRestoresOldSaves`. On ırk×iki kap20/20. Gerçek ağız max**0.01802m**, baş–kenar min**0.00555m**; dört pati gerçek zemin desteğinde, tam kedi arka duvarın önünde. Son hip→shoulder yönü her örnekte yandan görünür; yalnız kök dönüşü ölçülmez. Bu istek için ana kapların eski .30 kök görünürlük payı yerine .05–.5 aralığı ve gerçek gövde0–.5 yan görünüm aralığı denetlenir; diğer eylemlerin ortak kamera kuralları değişmedi. Ölçek, kemik bağları/uzunlukları korunur. Ara başarısız yarıçap denemeleri güncel sonuç değildir; son sonuç `native-verified.xml`.

**4/4 yerleşim EditMode / 4.147 koleksiyon**, son gerçek beşli CAT planı, minderin açık girişi ve yerleşimi başarılı. 103 ürün kartı ve sekiz oda ön izlemesi yenilendi; validator0/0. Diğer yedi oda sahnesinin hash'i aynı. İki gerçek video normal hızda, tam giriş–bakım–çıkış ve tek tamamlanmayla doğrulandı; 16:9/4:3 ve yakın görünüm kontrol edildi. Önceki511 tam test bu adım için tekrar çalıştırılmadı.

## Bırakılan durum

QA/Play/derleme kapalı; GameScene/CatHome_UI/LivingRoom_Level01 temiz, tek kamera/ses dinleyici ve salt-okunur editör ön izlemesi açık. 16 tercih ve varlık bayrağı geri yüklendi. Kullanıcı arada oynadı; bu adımın gerçek kayıt/recovery başlangıç–son hash'i **415E4935FCBBEA277178D3BAB6FC84C2AF631550767E836EFE49F855084396B7**. CP2 başlangıçla aynı. Eski C9E982 kaydı geri yüklenmedi. APK/arşiv/commit/push/yayın/kapatma yok.

Sonraki salon konusu yine önce kısa sıralı plan ve süreyle tek tek ele alınır.
