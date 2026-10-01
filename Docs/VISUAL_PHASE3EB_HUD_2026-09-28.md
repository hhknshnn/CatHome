# Phase 3E-B — HUD compaction, 28 Eylül 2026

Tek görsel uygulama. Yalnız StorybookHudLayout.cs, StorybookHudDetails.cs ve StorybookHudBottomPresentation.cs değişti. Üst panel 88→72 (%18,2); merkez üstten 84→64; alt sınır 128→100. Yazılar aynı boyutta; ihtiyaç ikonları 62→56. Portre runtime çerçevesi 100→72, kedi resmi 77→60; editör başlangıç çerçevesi 72 idi. Para paneli genişlikleri ve 48×48 artı hedefleri aynı. Menü 88×72. Bildirimler mevcut TopPanelBottom sabitini izler.

Joystick yalnız PremiumBase ve PremiumHandle görselleri 0,88 ölçeğe alındı: 188→165,44 ve 100→88. 220×220 giriş alanı, 0,65 handleRange ve 71,5 hareket yarıçapı aynı. Runtime güvenli alan/Y220 davranışı korunur. Alt dock 1080×64, düğmeler248×60, ayrılmış şerit80 aynı; kamera viewportuna dokunulmadı.

1920×1080 minimum ve 10 ROOM + 5 CAT gerçek otomatik düzeninin önce/sonra Game View görüntüleri Computer Use ile incelendi. Referans Photos ile açıldı. Kamera/ışık/malzeme/ROOM konumları aynı; sahiplik yazılmadı. Tam önizleme başlangıç minimum görünümüne geri alındı.

Derleme tamamlandı, Console hata0. Mevcut metinlerde taşma0; gerçek MobileJoystick yöntemlerinde8yön ve bırakma doğru. HUD'ın gerçek GraphicRaycaster bileşenleriyle8düğme×5nokta=40/40. Bu testte editörün kapalı giriş CanvasGroup kilitleri geçici açılıp finally ile döndü. Görünmez TitleScreenCanvas ayrı tutuldu: ilk bütün-sahne editör denemeleri onun SettingsButton/Visual nesnesini yakaladı; bunlar runtime hata veya son kabul sayılmaz. Esas raycast-hud-final.json. Play Mode/modal döngüsü/fiziksel telefon testi yapılmadı; uzun dinamik ad ve tutarlar ayrıca denenmedi. SafeAreaRect formülü aynı; gerçek Game View safe area1920×1080.

Üst HUD'ın altında28piksel ek düşey alan açıldı. Tablo hâlâ kısmen örtülüyor. Joystick görüntü alanı yaklaşık%22,6 azaldı; dokunma alanı aynı. Dünya yeniden tasarlanmadı.

576 başlangıç dosyası (sahneler, ürün varlıkları, ProjectSettings ve dört save/adlı dosya) hash aynı; sahiplik,33tercih,kamera/transform,ışık ve dünya malzemeleri aynı. Üç sahne temiz; Play kapalı. APK/commit/push/yayın yok. Kanıt Docs/QA/VISUAL_PHASE3EB_HUD_2026-09-28/closure.json; before-minimum.png,after-minimum.png,before-full.png,after-full.png. Kaynak değişiklikleri diskte; sahne/prefab kaydedilmedi.
