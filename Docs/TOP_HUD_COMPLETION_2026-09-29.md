# Top HUD — portre ve altın çerçeve devamı

Kullanıcının yeni isteğiyle 29 Eylül 2026 13:26:30 UTC'de başlandı. Önceki tur süreleri bu yeni devam turundan ayrıdır. Kesin kapanış zamanı QA closure.json içindedir.

Sol üstte düşük poligonlu portre yerine Blender'da üretilmiş on ırk varyantı kullanılıyor. Yuvarlatılmış baş/kulak, gövde ve patiler, göz katmanları ve mat tüy malzemesi eklendi. Uzun tüylü varyantların ayrı yanak küreleri ikinci geçişte tek baş formuna yedirildi. Portreler stilize yorumlardır; gerçek kedi modelleri veya diğer ekranların görselleri değiştirilmedi.

Altın çerçeveler gerçek yuvarlak kesitli dış kenar, ince iç oluk ve parlak iç dudakla yeniden modellendi. Üst geniş beyaz yansıma azaltıldı, champagne-gold gövde korundu. Elmas yeni crown/pavilion geometrisi ve ince ışıklı fasetlerle yeniden yapıldı. Hilalin açı, açıklık ve uçları değişti; yıldız yeri ayarlandı. Mama kabının alt gövde hacmi ve ekrandaki boy oranı referansa yaklaştırıldı. Son PNG alpha ölçümü ile ilk loose-bounds hesabı arasındaki fark, final-framing.py ile son düzeltme içinde giderildi.

İki sanat geçişi ve bir son karşılaştırma düzeltmesi; son düzeltmede bir ek oran renderı. Sonrasında yeni sanat geçişi yok. Sekiz mevcut PNG ve metası değişti: food, energy, diamond, profile, currency, portrait-ring, menu, plus. On yeni portrait-<breed> PNG ve meta eklendi.

Tek sunum kodu değişikliği StorybookPortraitCrop.cs: asıl Image.sprite/SelectedCatPortrait bağlantısı değişmeden Image.overrideSprite ile HUD art eşlemesi yapılır. preWillRenderCanvases öncesi yenilenir. Bilinmeyen/boş kaynak ve disable durumunda özgün görsele döner. Yeni sprite'ların şeffaf güvenli boşluğu kullanılır; eski tight-quad daire kırpmasının kulak/pati kesmesi giderildi. Eski görsellerde daire kırpması devam eder. MainPanelController, SelectedCatPortrait, CatBreedService ve katalog değiştirilmedi.

Doğrulama: Gerçek kedi seçimi/kayıt değiştirilmeden geçici gizli Image üzerinde 10/10 ırkta kaynak sprite korunması, art bulunması/eşleşmesi ve kulakların yeniden kırpılmaması geçti. Disable/enable, null ve bilinmeyen kaynak için dört ek kontrol geçti. Son gerçek Unity Game View 1920×1080 kaydedildi; referans ve final Blender Image Editor'da yan yana ve top crop olarak incelendi.

71 aktif üst HUD yerleşimi aynı. 4072 ortak RectTransform, 274 ortak düğmenin seri bağlantısı ve 1350 ortak metin aynı. Domain reload ile yeniden oluşan 7 editör dock öğesi ID dışı yerleşim karşılaştırmasında aynı; CompanionShortcut ve Label geçici öğelerdir. Mevcut sprite'larda 69 GUID/spriteID/internalID kontrolü aynı. Güncel gerçek kayıtlar ve sahne dosyaları aynı. Scripts ağacında yalnız StorybookPortraitCrop.cs değişti. Bu bütün proje varlıklarının hash denetimi değildir.

Son Play/derleme kapalı, üç normal sahne temiz, Unity açık. Son Console 0/0; ilk yeniden derlemede mevcut PhoneOnboardingGuidanceTests.cs içinden sekiz obsolete API uyarısı görüldü. Fiziksel cihaz/PlayMode oynanış kabulü yok. APK, commit, push veya yayın yapılmadı.

Kaynak `ArtSource/Blender/TopHudCompletion/Top-HUD-Completion.blend`. Final sprite'lar `Assets/Resources/TopHudExact`. Kanıtlar `Docs/QA/TOP_HUD_COMPLETION_2026-09-29`; son final-game-view.png, reference-vs-final-top.png, reference-vs-final-side-by-side.png ve ten-breed-portraits.png esas. first-pass-game-view.png tarihsel ara görüntüdür.

Kalan görsel farklılıklar: referans beyaz kedi içerirken gerçek HUD seçili ırkın varyantını gösterir; örnekte turuncu Persian. Portre çizim dili, hilal yansıması ve elmas faset dağılımı referansın piksel kopyası değildir. İstenen başlıkların tümü bu turda işlendi; eşit AAA sanat kalitesi nesnel olarak doğrulanmış diye sunulmaz. Yeni tur kendiliğinden başlamaz.
