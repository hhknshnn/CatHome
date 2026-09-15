# Banyo — eski patiyle bakım ve ikinci kedi, 11 Eylül 2026

Kullanıcı önceki yürüyerek sürtünme çözümünü değiştirdi: yalnız mevcut patiyle bakım animasyonu, düğmeden sonra dolaşma yok. `GroomBrushActivity` yakındaki açık zeminde sabit kalır; mevcut `Groom / ActivityScratch / Itching` klibini oynatır. Yalnız görünür tarafa kısa yerinde dönüş olabilir. Fırça boyunca üç geçiş, dönüş turu ve çıkış yürüyüşü kaldırıldı. Önceki adıma eklenen ve artık kullanılmayan `SetWalkSpeed` değişikliği geri alındı. Kaplar, diğer eşya animasyonları ve yerleşim aynı.

İkinci kedi gerçek oyuncu değildi: `Editor · saved home preview` altında, artık geçerli sahnesi olmayan açık bir skinned renderer idi. Eski `CatHomeEditPreview.Clear()` geçersiz sahneleri atladığı için kopya kalıyordu. Temizlik artık kalıcı varlık olmayan tüm ön izleme sahiplerini kaldırır ve Play'e girildiğinde tekrar çalışır. `EditorHomePreviewState` de Play'de açık kalırsa kendini kapatır. Gerçek oyuncu/ırk modeli silinmez.

Kanıt kökü `Docs/QA/BATHROOM_PAW_GROOM_2026-09-11`. İlk XML'ler tanı kayıtlarıdır; kısa animasyon geçişi sırasında klip listesi boş olabilir. Son test kararlı evreyi ölçer; kökün sabit kalması bütün eylem boyunca ayrıca denetlenir. Gerçek kayıt/recovery/CP2 ve sekiz oda sahnesi önceki adımla aynı (`preservation.json`). Video yok; gerçek Unity görüntüsü kullanılır. Adım3 ve başka işler onay bekler.

Son üç benzersiz native test başarılı (`test-summary.json`): on ırkta özgün klip, ırk başına 416 örnek, kök kayması 0; gerçek pati hareketi yaklaşık .19 m. Duraklatma/başka giriş sahibiyle iptal ve eski ürün sahipliği/tamamlanma kontrolü başarılı. Son normal Play yüklemesinde **1 CatMovement, 1 görünür kedi derisi, 0 editör ön izleme sahibi** doğrulandı. [Gerçek patiyle bakım görüntüsü](QA/BATHROOM_PAW_GROOM_2026-09-11/paw-groom.png). Önceki adımın yürüyüş görüntüsü yeni davranışı temsil etmez.

Unity ayrı QA kopyasında Banyo/Bakım arabasıyla kullanıcıya hazır bırakılır; canlı son durum `editor-ready.json`. Kullanıcı bitirmeden kapatmayın. Test sonrası tercihler ve geçici font/CurrencyHud/EditorSettings çıktıları geri yüklendi. APK/arşiv/commit/push/yayın/kapatma yok.
