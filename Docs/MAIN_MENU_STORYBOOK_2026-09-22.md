# Ana menü görsel denemesi — 22 Eylül 2026

Kullanıcı ana menü referansından sonra “devam et” dedi; uygulama yalnız ana menüyle sınırlandı. Son talimat kesin 30 dakika: başlangıç 11:22:51 UTC / 14:22:51 TR, son teslim sınırı 11:52:51 UTC / 14:52:51 TR. İlk 90–150 dakika tahmini geçersizdir. Kesin kapanış ve süre QA klasöründeki `closure-summary.json` içinde kayıtlıdır.

## Uygulanan sonuç

- Kavisli indigo/turkuaz okuma yüzeyi, krem başlıklar, mercan Devam et, daha belirgin buton derinliği ve kısa basış tepkisi.
- Kullanıcının onayladığı modern kedi başı; Kedim/Odalar/Oyunlar için ortak alt grup ve resimli simgeler. Patili altın jeton kullanılmaz; oyun para birimi değişmedi.
- Ayarlar/Yapımcılar ve masaüstündeki Çıkış korunur. Ekran oranı ve güvenli alan uyarlaması; mevcut Türkçe/İngilizce bağları ve kilitli kısayol işleyişi korunur.
- Var olan canlı kedi sahnesi, modeller, oda, ışık, kedi hareketleri, diğer ekranların görünümü ve kayıt mantığı değişmedi.

Yalnız TitleScreen prefabı ve UI sahnesindeki karşılığı güncellendi. Yeni StorybookTitleBuilder son stil adımıdır; diğer ekranlara uygulanmaz. LowPolyPanelGraphic içindeki yeni yüzey tercihi varsayılan olarak kapalıdır. Yeni StorybookTitleBackdrop ve StorybookTitleGlyph yalnız bu menünün çizimidir. İki PNG, daha önce onaylanan tasarım kaynaklarından alındı.

Referans `DesignProposals/2026-09-22_Storybook/06-main-menu-reference.png`; gerçek Unity sonucu `QA/MAIN_MENU_STORYBOOK_2026-09-22/screens-final/main-menu-final.png`. Referans bir tasarım görselidir; mevcut kedi/oda modellerinin aynısı veya birebir uygulanmış ekran olarak sunulmaz. Yeni uygulamanın kullanıcı görsel değerlendirmesi henüz alınmadı.

## Kontrol ve sınırlar

- Seçili EditMode testleri **6/6 geçti** (`editmode-title.xml`): canlı kedi/bağlantılar, yeni ikonlar, iki dil, yeni oyun onayı, Türkçe karakterler ve güvenli alan arka planı. PlayMode test paketi çalıştırıldığı iddia edilmez.
- 1920×1080 Türkçe, 2340×1080 Türkçe ve 1440×1080 İngilizce görsel kontrolü. Son ekranlar `screens-final` içindedir.
- Sekiz ana menü düğmesinin 1920×1080 merkez raycast ve güvenli alan kontrolü geçti; tablette sekizinin sınırları içeride. `title-raycasts-1920.json`, `title-bounds-tablet.json`.
- Devam et Unity pointer-click olayıyla açıldı; `TitleScreen.RequestShow()` ile menü geri getirildi, tek TitleScreen bulundu (`continue-return.json`). Yapımcılar aç/kapat ve yeni oyun onayı aç/iptal gözlendi; kayıt sıfırlanmadı.
- Fiziksel telefon, dokunmatik cihaz performansı ve bütün oyunun UI taraması bu denemenin kabulü değildir. Derleme/konsol hata kontrolü temiz.

## Koruma ve kapanış

Esas kanıt `Docs/QA/MAIN_MENU_STORYBOOK_2026-09-22`: `source-baseline.json`, `preservation-final.json`, `editor-final.json`, `closure-summary.json`. Başlangıçta 2.811 dosya hash'i alındı; bir üçüncü taraf Cat Eat animasyonunda okuma izni yoktu, `baseline-unreadable.json` içinde açıkça kayıtlı. Bu dosya değiştirilmedi; tüm kaynakların eksiksiz hash denetimi iddia edilmez.

Başlangıca göre değişen mevcut dosyalar yedi: PremiumUiFactory, LowPolyPanelGraphic, TitleScreenLayout, TitleScreen.prefab, CatHome_UI.unity ve iki mevcut EditMode test dosyası. Yeni menü sınıfları ve iki PNG ayrıca eklendi. Test/serileştirme kaynaklı CurrencyHud ve iki font atlası değişikliği, **bu görevin başlangıç hash'iyle birebir doğrulanan baytlara** döndürüldü; öncesi QA içinde saklandı. Eski tarihsel kayıt yüklenmedi.

Üç gerçek kayıt aynı, 16 tercih geri geldi. Play/QA kapalı; GameScene, CatHome_UI ve LivingRoom_Level01 temiz. Unity/PC açık. QA Git dışında; commit/push/APK/video/yayın yapılmadı. Başka ekranlara kendiliğinden devam edilmez.
