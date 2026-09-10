# Eşyalar arasında boşluk ve doğru eylem — 7 Eylül 2026

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

Kullanıcı, top sepetinden uzakta top eyleminin göründüğünü ve yatağın önündeki “İzle” eyleminin tabloyu başlattığını bildirdi. Oda eşyaları boş alana rağmen merkezde yığılıyordu.

## Değişiklik

- Bütün `CatActivity` eylemleri HUD için ayrı bir yakınlık kuralı kullanır: gerçek rutin girişinin en fazla **.46 m** çevresi ve o noktaya engelsiz, kedi gövdesi genişliğinde doğrudan geçiş. Eski 1–1.4 m etkileşim yarıçapı HUD'u açmaz. Otomatik rutin yaklaşımı değişmez.
- Tünel aynı kontrolü iki ucunda ayrı yapar; yan kumaş bir giriş sayılmaz. Uzaktaki nesneyi seçmek mesafeyi atlamaz. Düğmeye basıldığı anda mesafe/engel yeniden ölçülür.
- Eylem düğmesi eşyanın yerelleştirilmiş adını ve eylemini birlikte gösterir. Yakındaki iki eşya arasında eski .28 m seçim toleransı .06 m'ye indi. Oyuncunun açıkça seçtiği nesne ancak yakındaysa önceliklidir.
- Tablo izleme noktası `(-.4,0,.65)` oldu. Ana yatağın ön girişi `(-.6,.05,≈1.416)` ile ayrıdır. Prefab, salon sahnesi ve `RoomProductInteractionBuilder` aynı noktayı taşır.
- CAT yerleşiminin yatay alanı ön sağ bölgeye açıldı; iki CAT gövdesi arasında .40 m pay bırakılır. Ön adaylar z≈-1.50…-1.85 ile kadraj içinde tutulur. Küçük eşya direğin arkasından ayrılır; top sepetinin özel `(-1.65,0,-1.75)` bölmesi içeri bakan girişiyle analog kontrolü kapatmaz. Oyuncakların en fazla 5, yatakların en fazla 1 olması; otomatik yerleşim; sahiplik ve depolama korunur.
- Sehpa zıplama girişi ön oyuncak alanından solundaki açık koridora, `(.50,0,-.60)` noktasına taşındı. Sofa/table model ve kamera ölçeği değişmedi.
- Yerleşim yalnız yakın bir boş ızgara hücresini bulmakla yetinmez: giriş ve tünel çıkışının gerçek .27 m kapsülü duvarlara ve gelecekte satın alınabilecek ROOM mobilyalarına karşı ölçülür. Karton yuvanın ön duvara çok yakın giriş verebildiği durum böyle elendi.

## Doğrulama ve gerçek görüntüler

Kanıt klasörü: `Docs/QA/ACTIVITY_SPACE_2026-09-07`.

- Tam EditMode: **442/442**. İçinde 4.147 izinli beş CAT birleşiminin erişilebilir yerleşim taraması bulunur.
- PlayMode: **6/6** odaklı native test. Son yerleşimde giriş/engel, 17 CAT ve on ırk top rutinini kapsayan **3/3** test ayrıca tekrar geçti (`PlayMode-final-layout.xml`).
- `ActivityProximityTests`: sekiz odadaki 80 ROOM ürününe bağlı 81 girişte yakın/uzak/engel taraması; 17 CAT ürünü; iki tünel ucu; yatak/tablo ayrımı; eski düğme tıklaması; gerçek beş eşyalı sahnede gövde boşlukları ve kamera sınırı; on ırkta üç gerçek pati temasıyla top rutini.
- `LivingFurnitureTests` on ırkta koltuk ve sehpanın 20 zıplama/temas/çıkış rutinini çalıştırır. TV seçimi ve çift uçlu tünel regresyonları ayrıca çalışır.
- Tüm oyun/test oturumları `UiQaTestSession` kayıt kopyasındadır. Gerçek kullanıcı kaydı ve çevrimiçi ödüller QA için değiştirilmez.

Gerçek 1920×1080 dört kare `index.html` galerisindedir. `live-actions.txt`: koridorda aday/düğme yok; sepette “Top sepeti / Topla oyna”; yatakta yalnız “Uyu”; ayrı izleme noktasında “Modern tablo / İzle”. Dört karenin gerçek UI dikdörtgen taramasında çakışma, ekran dışı alan veya engellenmiş düğme yoktur. Kamera/listener/EventSystem 1/1/1, mimari doğrulama 0 hata/0 uyarıdır. Salon/HOME ön izleme fotoğrafı yeni düzenle yenilendi.

Unity, QA kapalı ve Play durmuş olarak `GameScene + CatHome_UI + LivingRoom_Level01` düzenine döndürüldü; aktif salon, tek kamera, `DisableSceneReload`, boş `playModeStartScene` korunur. İlk geçiş dosyası düzeltme öncesi yakalanan karton yuva girişini saklar. Git commit/push kullanıcıya aittir.
