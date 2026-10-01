# Top HUD — doygun renk ve ikon hacmi, 29 Eylül 2026

Kullanıcı önceki Vivid görüntüsünün fazla açık/soluk olduğunu belirtti; kedi token'ı ve diğer ikonlar için ek kalite artışı istedi. Bu devam turunun başlangıcı 12:59:39 UTC. Önceki turların süreleri bu süreye dahil değildir ve sıfırlandıkları iddia edilmez.

Yalnız üst HUD artwork ve import ayarları değişti. Mercan, cyan-blue ve mor panel gövdelerinin doygun orta tonları derinleştirildi; geniş beyaz yansımalar ve aşırı emission azaltıldı. Dolgu çubukları renklendirildi. Kedi token'ı yumuşak yüz/kulak kabartmasıyla yeniden üretildi; ayrı yanak parçaları yüz formuna yedirildi. Mama parçaları yeniden modellendi; seramik malzeme koyulaştırıldı. Elmasın faset pigmentleri/ışığı ve hafif eğimi düzenlendi. Hilal/damla ışığı, yıldız boyutu/konumu ve gerçek torus kenarlı altın pati rozeti yenilendi. Efektler PNG içine Blender renderıyla işlendi.

İki artwork geçişi + bir son karşılaştırma düzeltmesi yapıldı. Tarihsel olarak `final-correction.py` adlı dosya bu turun ikinci artwork geçişidir; son karşılaştırma düzeltmesi `finish.py` dosyasıdır. Bunun ardından başka sanat geçişi yapılmadı. 20 sprite için mipmap/Kaiser ve trilinear küçültme filtresi açılarak küçük HUD boyutundaki tırtıklar azaltıldı; shader değişmedi.

Blender kaynak: `ArtSource/Blender/TopHudTone/Top-HUD-Tone.blend`, sahne `Top HUD Final Match`; yeniden üretim dosyaları aynı klasörde. Önceki `TopHudVivid` kaynağı korundu.

Sprite dizini: `CatHome/Assets/Resources/TopHudExact/`. Değişen 20 PNG: food, water, energy, coin, diamond, badge, panel-food, panel-water, panel-energy, well-food, well-water, well-energy, profile, currency, portrait-ring, plus, menu, fill-food, fill-water, fill-energy. Aynı 20 meta yalnız import/rect verileri bakımından değişti; GUID/spriteID/internalID kimlikleri korundu. bar-track, xp-track, fill-xp aynı.

Kanıt: `Docs/QA/TOP_HUD_TONE_2026-09-29`. `final-game-view.png` gerçek 1920×1080 Unity Edit Mode Game View GPU okumasıdır. Ham GPU görüntüsü yalnız dikey çevrilerek kaydedildi; sanat rötuşu yapılmadı. `reference-vs-final-top.png`, `reference-vs-final-side-by-side.png`, `reference-before-final-top.png` ve `six-detail-comparison.png` aynı görüntüden üretildi. Referans ve final Blender Image Editor içinde yan yana ve yakın planda açılarak incelendi.

Koruma: 71 aktif üst HUD RectTransform aynı. 4072 ortak RectTransform aynı; domain reload sırasında yeniden oluşan 7 editör dock öğesinin yerleşimleri kimliksiz karşılaştırmada aynı. 274 ortak düğmenin seri bağlantıları ve 1350 ortak metin aynı; bir geçici CompanionShortcut/Label yeniden oluştu. 69 sprite kimlik kontrolü aynı. Güncel başlangıçtaki gerçek kayıtlar, Scripts C# ve Scenes unity dosyaları aynı. Bu kapsamlı bir bütün-proje hash taraması değildir. Play açılmadı; etkileşim testi veya fiziksel cihaz testi yapıldığı iddia edilmez.

Son durum: Unity açık, Play/derleme kapalı, üç temiz normal sahne. Console 0 error; önceki turdan kalan tek RenderTexture.active uyarısı silinmedi. APK/commit/push/yayın yok.

Kalan görsel farklar: Seçili kedi portresi hâlâ mevcut düşük poligonlu ırk görselidir; binding korunmuştur. Referanstaki boyanmış portre hissi sağlanmadı. Elmasın fasetleri hâlâ referanstan daha düz; hilal ve mama kabının optik oranları birebir eşleşmez. Panel renkleri ve yüzey temizliği önceki Vivid durumuna göre yaklaştı, fakat referansla eşit AAA kalite iddia edilmez. Geçiş sınırında duruldu; yeni tur kendiliğinden başlamaz.
