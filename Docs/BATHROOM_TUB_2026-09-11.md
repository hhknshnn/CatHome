# Banyo — küvet kenarında denge / adım 6, 11 Eylül 2026

Kullanıcı yalnız adım6'yı 25 dakika hedefiyle onayladı. Kapsam küvet kenarında havada/dışarıda kalan patiler, hafif denge hareketi ve komik balon. Model, küvetin konumu, diğer eşyalar ve önceki beş adım değiştirilmedi.

Eski yol gerçek modele oturmuyordu: kök X2.399/Y.61008 ile Z−.34366…1.04366 boyunca ilerliyordu; orta kenarın gerçek üst yüzeyi yaklaşık Y.55–.56, kıvrılan uçların bir kısmı bu düz yolun dışındaydı. Üstelik kök her karede 2 cm zıplatılıp 8° yana yatırılıyor, sabit Walk temposuyla hızlanan/yavaşlayan yol eşleşmiyordu.

TubEdgeWalkActivity mevcut işaretleri uyumlu tutar; çalışma anında uçları .40 m içeriden seçer, suya doğru 4.5 cm hizalar ve kendi MeshCollider'ının gerçek düşük kenarını ölçer. Yaklaşık .59 m yürüyüş 1.8 sn sürer; mevcut on ırklı adım kataloğu temposunu gerçek hıza bağlar. Hafif 4.5 cm çömelme dar pati yerleşiminde doğal erişim bırakır; kökte sekme/yalpalama yok.

CatTubRimMotion yalnız bu eylemde çalışır. Gerçek kenardan üst yüzey örnekleri alır; kaynak yürüyüşün gerçek pati tabanlarını ölçer, basan ve havaya kalkan patiyi ayırır. Dört bacakta yalnız eklem dönüşleriyle temas çözülür; kemik uzunlukları, kök/model ölçeği ve özgün adım korunur. Gövde en fazla 3° sallanır; baş/kuyruk küçük karşı hareket yapar. Duraklatma donar; bitiş, iptal ve devre dışı kalma bütün düzeltmeleri temizler. Ulaşılan uçtan açık zemine iniş ve kontrol/ödül sahipliği korunur.

Yürüyüşte Türkçe “Düşmedim… bilerek sallandım!”, İngilizce karşılığıyla birlikte kullanılır. Tekrar aralığı 30 sn; bitişin mevcut “Patilerim hâlâ kuru.” balonu korunur.

Son doğrulama **5/5 benzersiz native**, **validator 0 hata / 0 uyarı**. Kanıt `Docs/QA/BATHROOM_TUB_2026-09-11/test-summary.json`: son üç mevcut küvet testi `tub-final.xml`, iki son temas/duraklatma testi `tub-sole-final.xml`. İlk başarısız ara sonuçlar son sonuç değildir. On ırkta gerçek dört pati altında küvet üst yüzeyi, çözülmüş erişim, kemik uzunluğu, gerçek basan pati tabanında en fazla 2 cm açıklık, tek tamamlanma ve açık çıkış denetlenir. Adım sırasında doğal havaya kalkan pati tabana zorla yapıştırılmaz; ayak bileği yüksekliği taban açıklığı olarak raporlanmaz. Tam EditMode/telefon performansı bu dar adımda ölçülmedi.

Gerçek üç kayıt dosyası ve sekiz oda sahnesi başlangıçla aynı (`save-after.json`, `scenes-after.json`). QA öncesi 16 tercih geri yüklendi; geçici font/CurrencyHud/EditorSettings çıktıları temizlendi. Son manuel deneme yeni ayrı kayıt kopyasındadır. Video, APK, arşiv, commit/push, yayın ve kapatma yok. Sonuç kullanıcı incelemesini bekler; başka odaya kendiliğinden geçilmez.

Son gerçek düğme turları Russian Blue ve tamamen siyah Oriental Shorthair ile başarılı: her biri **4.833 sn / 1 tamamlanma / açık çıkış / bırakılmış kontrol**. Duruş geçişinde toplam .0417 sn Walk ölçüldü. Siyah kedi raporu `Docs/QA/ROOM_INTERACTIONS_2026-09-11/tub-step6-black/report.json`. Unity siyah kediyle küvet düğmesinde hazır bırakıldı; tek kedi/tek görünür deri, normal hız ve video kaydı kapalı (`editor-ready.json`).

Tek teslim görseli: [gerçek Unity denge yürüyüşü](QA/BATHROOM_TUB_2026-09-11/after-oriental-shorthair.png). Yakın çekimde kamera ayarları çekimden hemen sonra geri yüklendi. Diğer PNG'ler yerel inceleme kanıtıdır.
