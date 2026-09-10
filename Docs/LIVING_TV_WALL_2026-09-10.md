# Salon — TV yan duvarı ve minder/kitaplık değişimi, 10 Eylül 2026

Son kullanıcı düzeltmesi: kaplar TV yanında dikine, beyaz arkalık TV'nin bulunduğu yan duvara yaslı; uyku minderiyle kitaplığın yerleri değişti. Önceki arka duvar istasyonu bu istek için geçersizdir. [Güncel galeri](QA/LIVING_TV_WALL_2026-09-10/index.html) dokuz PNG ve iki gerçek24fps tam rutin videosu içerir.

## Son yerleşim

- `LivingRoomReferenceLayout.CareStationPosition=(-3.375,0,1.40)`, yaw270. Beyaz arkalık sol panele yaklaşık3.5mm aralıkla paralel; uzun eksen TV duvarı boyunca dikine uzanır. TV'nin arka ucuyla platform arasında yaklaşık.19m, platformla arka duvar arasında yaklaşık.25m açıklık vardır.
- Taban genişliği ve gerçek destek yüksekliği aynı: Y.06. Asimetrik uzun taban ve yürüyüş engeli birlikte aynalandı. Yerel merkez X+.18; boyut1.775×.20×.871. Food/Water yerel X−.28/+ .28, Y.06, Z−.36. Girişler yerel X.185746/.745746, Z−.92; beslenme ofseti(.275746,0,.048621), gerçek .28m yarıçap. Baş teması ve açık çıkış yeni yönle birlikte taşındı.
- İçerik yüksekliği ve önceki baş/çene düzeltmesi korundu: mama+.030m, su+.028m; hedef gerçek üst üçgen. Kedi kök ölçeği, kemik bağları ve uzunlukları aynı. Son çalışma pozu oyuncu kamerasından görünür; en kısa açıyla dönüş, pause/iptal ve kontrol iadesi korunur.
- Kitaplık ve kitap seti `(-.24,0,2.36)`, minder `(-1.35,0,2.4387)` yaw0. Genişlikleri farklı olduğundan aralarında açık pay bırakılarak yerleri değiştirildi. Minderin gerçek arka kenarı Z2.72 panele sıfırdır. Ana yatak X1.00 ve üstündeki tablo aynı kaldı. Diğer dört sergilenen CAT ürününün geçerli kayıtlı konumu korunur.

## Bu değişikliğin kontrolü

**4/4 native:** `LivingBowlContactTests` üç testi ve `ProductionCareTests.PairedCareTray_BlocksNarrowPocketsAndRestoresOldSaves`. On ırkta iki kap20/20, gerçek ağız/baş kenarı/pati ölçümü, sekiz yakın yön senaryosu, pause/iptal ve eski sıkışmış konumdan güvenli çıkış geçti. Ağız max **0.02487m**, baş kenarı açıklığı min **0.02970m**, son gövde dot min **0.98459**.

**4/4 yerleşim EditMode:** `LivingRoomArrangementTests`; 4.147 izinli beşli koleksiyon, minderin tam duvar konumu ve açık girişi dahil. Önceki tam511/511 sonucu önceki geçişe aittir; bu konum düzeltmesi sonrasında bütün511 tekrar çalıştırılmadı.

Son birleşik yerleşimde gerçek mama/su düğmeleriyle normal hızlı tam videolar çekildi; her biri tek tamamlanma ve gerçek24fps/kare sayısıyla doğrulandı. Minderin giriş → tutulan dinlenme → Kalk → açık zemine dönüşü başarılı. Seçili Russian Blue dinlenirken son mesh ile duvar arasında **0.03570m** açıklık var. 103 kart ve sekiz oda ön izlemesi güncel; validator0 hata/0 uyarı. Diğer yedi oda sahnesinin hash'i aynı.

## Bırakılan durum

QA/Play kapalı; GameScene/CatHome_UI/LivingRoom_Level01 temiz, tek kamera/ses dinleyici ve gerçek kaydı yalnız okuyan editör ön izlemesi açık. 16 tercih ve varlık bayrakları tam geri yüklendi. Gerçek kayıt/recovery başlangıç–son **C9E982B6C3066948FB8AF16D0E2559578ECE9B808A59D83F39671905A305AF76**; CP2 de aynı. Eski hash geri yüklenmedi. APK, arşiv, commit/push, yayın veya kapatma yapılmadı.

Sonraki kullanıcı konusu yine kısa sıralı plan ve süre verilerek tek tek ele alınır.
