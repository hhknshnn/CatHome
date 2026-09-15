# Banyo — kum tuvaleti / adım 3, 11 Eylül 2026

Kullanıcı adım 3'ü 35 dakika hedefiyle onayladı. Kapsam yalnız kum tuvaleti. Kazmada göğüs en az 9° öne yönelir (erişim gerektiğinde eski 14° üst sınırı korunur); baş 35° kuma bakar. Gerçek dönüşümlü pati teması, .038 m deforme çukur, kemik boyları ve kedi ölçeği korunur.

Kedi kazdıktan sonra ölçülen kalça–çukur mesafesini kısa yürüyüşle kapatır. Kök ve geçici destek birlikte hareket eder; çömelmede özgün SitDown pozu bütün olarak yatay hizalanır. Kalça kazılan çukurun üstünde kalır; çukur başka yere taşınmaz. Çiş daha alçak (.62), kaka daha yüksek (.38) SitDown evresini 2.8 saniyelik geçiş içinde kullanır. Örtmek için kumun içinde .20 m yarıçaplı kısa yayla eski pati noktasına döner; aynı çukur örtülür. İlk yerinde yarım dönüşün arka patileri kum dışına taşıdığı canlı incelemede bulundu; son test yeni hareket evrelerinin dört patisini de ölçer.

İlk tamamlanan kullanım kaka, sonraki iki kullanım çiş; üçlü düzen oda içindeki tamamlanan kullanımlarla ilerler. İptal sayacı ilerletmez. Kaka üç küçük kahverengi parça, çiş küçük koyu ıslak iz gösterir. Tek geçici mesh/renderer/malzeme kullanılır; örtülürken gömülür, çıkış veya iptalde temizlenir. Reduced motion seçeneğinde parçalar yere doğrudan yerleşir. Aralıklı “Off, koktu!” / “Çiş yaptım, hehe!” balonları en az 30 saniye arayla görünür; İngilizce karşılıkları da vardır.

Değişen oyun kodu: LitterDigActivity, CatLitterRoutineMotion, yeni CatLitterWasteFx, iki GameContentCopy satırı. CatActivityAnimation.SetWalkSpeed yalnız kumdaki kısa adımların hızını mevcut ırk yürüme ölçüleriyle eşler; diğer eylemler normal SetPose yolunu kullanır. Patiyle taranma çözümü ve ikinci kedi temizliği korunur. Kum kabı, klozet, sepet ve diğer oda yerleşimleri değiştirilmez.

Kanıt kökü: Docs/QA/BATHROOM_TOILET_2026-09-11. Güncel sonuç test-summary.json dosyasında; ilk lean/alignment/native ve return diagnostic XML'leri ara sonuçlardır. Son native testler: on ırkta tam rutin, çukur–kalça hizası, yeni hareket evrelerinde dört pati açıklığı, iki tuvalet çeşidi/yükseklik farkı, parça sayısı ve gömme, beş evrede duraklatma/iptal ve özgün mesh/sahiplik koruması. Gerçek oyun düğmesiyle tek tamamlanma, tam kapsülle açık çıkış ve bırakılan kontrol ayrıca denetlenir.

Sekiz oda sahnesi ve üç gerçek kayıt dosyası SHA-256 karşılaştırması preservation.json içinde. Gerçek ana kayıt/recovery: 3558D75B36B1710960624B629C8F0C2C51A1E1B231E2572B12984F825F896B9B. CP2 değişmedi. Tercihler ve geçici Unity üretim çıktıları test sonunda geri yüklenir; kullanıcı denemesi ayrı QA kopyasında açılır. Canlı durum editor-ready.json içindedir.

[Tek teslim görseli](QA/BATHROOM_TOILET_2026-09-11/toilet.png) gerçek Unity kamerasından alınır. Kazma görüntüsü ve ara teknik ölçüler aynı yerel kanıt dizinindedir. Video, APK, arşiv, commit/push, yayın veya kapatma yapılmadı. Telefon performansı ölçülmedi. Adım 4 ve sonraki işler kullanıcı onayı bekler.

Son doğrulama: **4/4 native, validator 0 hata / 0 uyarı**. Son dosya `toilet-final.xml`; önceki dönüş XML'indeki dar sınır ihlali son yay ayarıyla giderildi. Tam EditMode paketi yeniden çalıştırılmadı.

Son gerçek düğme turu: **18.33 saniye, 1 tamamlanma, açık çıkış ve bırakılan kontrol**. Siyah kedinin 440 örneğinde kum dışına taşan pati karesi 0; “Off, koktu!” gerçek balon metni doğrulandı (`visual-check.json`). Son düğme raporu `Docs/QA/ROOM_INTERACTIONS_2026-09-11/toilet-step3-verified/report.json`. Kullanıcının ilk denemesinde kaka çeşidini görebilmesi için yalnız QA'daki oda içi çeşit sayacı ve balon süresi başlangıca alındı.
