# CAT koleksiyonu — yerleşim ve oyun düzeltmesi

6 Eylül 2026. Kullanıcı kararı: **aynı anda en fazla 5 CAT eşyası, en fazla 1 yatak**.

## Yerleşim

Sahiplik sınırsızdır. Satın alınan CAT ürünü koleksiyona gelir; mağazadaki “Odaya ekle” yerleştirme görünümünü açar. Görünür ürünün kartındaki “Kaldır” ürünü koleksiyona alır. Konum ve açı korunur; tekrar eklerken değiştirilebilir. ROOM satın alımları sabit yerine otomatik gelir.

`CatCollectionPolicy` yalnız düğmeyi değil `HomeStoreService.TrySetStored` yolunu da sınırlar. Store alt şeması v8, ana kayıt v11. Eski kayıtlar katalog sırasıyla en fazla beş eşya ve bir yatağı görünür tutar. Fazlasının sahipliği veya kaydedilmiş konumu silinmez. Kapasite doluyken de ürün alınabilir.

`HomeProductPlacement` görünür olmayan ROOM modellerinin zemin alanlarını da rezerve eder. Döndürülmüş ayak izleri ve yürünebilir CAT trigger ürünleri hesaba katılır. Duvar resimleri ve alçak oda halıları zemini kapatmaz. Kedinin yaklaşma noktası ve tünelin iki ağzı açık kalmalıdır. Kayıttan gelen çakışan CAT ürünü koleksiyona kaldırılır. Yeni ROOM satın alımı aynı denetimi tetikler.

## Boyutlar

Blender kaynakları, gerçek modeller, katalog ölçüleri, tetikleyiciler ve mağaza fotoğrafları birlikte güncellendi. Kedinin kendisi küçültülmez. Yataklar ve tünelin iç açıklığı korunur.

| Ürün | Önce genişlik × derinlik | Sonra | Zemin alanı değişimi |
|---|---|---|---|
| Fare oyuncağı | 0.55 × 1.20 | 0.44 × 0.78 | −48% |
| Seramik mama kabı | 0.65 × 0.55 | 0.50 × 0.42 | −41% |
| Kedi çimi | 0.70 × 0.70 | 0.52 × 0.52 | −45% |
| Mama dağıtıcısı | 0.62 × 0.52 | 0.50 × 0.44 | −32% |

## Hareket ve dinlenme

`CatToyAnimationBuilder` eski PolyOne yerine bütün ırkların paylaştığı Polyperfect iskeletine sekiz proje klibi üretir. Farede yaklaşma/sıçrama, farklı sağ-sol pati, tekerde itme, kurdelede çekme, beslenmede koklama/yeme kullanılır. İskeletin uzunlukları ve kedi kök ölçeği korunur. `CatToyContactMotion` pati erişimini gerçek ürün temasına uyarlar; dönen veya esneyen parça ancak temas yaklaşınca tepki verir. Etkileşim bitince/iptalde eklem düzeltmeleri ve oyuncak pozu temizlenir.

Tünelde ayakta pati oynatılmaz. Çıkış sonradan kapanırsa kedi girişe alçak pozla döner; kumaşın içinden ayakta geçmez.

Yataklarda giriş maliyeti **0**. Gerçek dinlenme sırasında enerji saniyede **0.75 puan**, yaklaşık bir kısa uykuda **2.4 puan** artar. Aynı sırada uyanık enerji düşüşü uygulanmaz. İptal gelecekteki artışı durdurur; tamamlamada eski 14 puanlık toplu ödül yoktur. Enerji 100 sınırında kalır.

## Kanıt

`Docs/QA/CAT_2026-09-06_Refinement/` içinde XML sonuçları, temas ölçümleri ve gerçek Unity kareleri bulunur. Canlı testler yalnız `UiQaTestSession` kayıt kopyasında yapılır. Önceki 800 ROOM rutini bu çalışmanın sonucu sayılmaz. Git commit/push kullanıcıya aittir.

Son tam EditMode: **431/431**. Native Test Runner'da **6 farklı PlayMode testi** başarılı: `PlayMode_Second.xml` içindeki beş geçen test ve son ankraj düzeltmesinden sonra `PlayMode_LegacyFinal.xml` içindeki top sepeti/tırmalama testi. Bu iki sonuç birlikte **17 ürün × 10 ırk = 170 rutin**, iptal/temizlik, gerçek pati teması, sıfır enerjide dinlenme ve on ırkın tünel içindeki bütün pozlanmış mesh taramasını kapsar. İlk başarısız denemeler teşhis kaydı olarak korunur; tek bir 6/6 dosya gibi sunulmaz.

Canlı kayıt kopyasında salonun bütün mobilyalarıyla beş CAT ürününün aynı anda geçerli konumları ölçüldü. Altıncı eşya ve ikinci yatak engellendi; Kaldır, Odaya ekle ve yerleştirme onayı gerçek düğme bağlantıları üzerinden doğrulandı. Henüz satın alınmamış TV mobilyasının alanına yerleştirme de reddedildi. Mağaza rozetleri depodakinde “Koleksiyonda”, görünür üründe “Odada” gösterir; taşma yoktur. Yerleştirme düğmeleri arasında en az 16 px boşluk korunur. HUD, mağaza ve yerleştirme çubuğunda `GetWorldCorners`/raycast taraması çakışma, ekran dışı veya yanlış düğme alıcısı bulmadı.

Yeniden görünür olan yerleştirme çubuğu da ortak ivory/mint/coral yüzeylerine ve gerçek Fredoka/Nunito fontlarına taşındı. “Yerleştir” / “Vazgeç” dil değişimini izler; açıklama Türkçedir. Basılma hareketi sabit dokunma alanının içindeki görsel çocukta çalışır. `ShopPanelBuilder.PolishPlacementToolbar` yeniden üretimde aynı görünümü korur.

Gerçek Unity görüntülerinin galerisi: [HD görseller](QA/CAT_2026-09-06_Refinement/index.html). Oda ve mağaza kareleri 1920×1080, ürün fotoğrafları 1024×1024'tür. Galerideki sahiplik düzeni test örneğidir; oyuncunun asıl kaydı değiştirilmedi. Güncellenen tüm katalog kartları birlikte gözden geçirildi; gardırobun arkadan çekilen mağaza fotoğrafı da düzeltildi.

Sınır: otomasyon penceresinde Unity `Application.isFocused=false` ve Mouse cihazı devre dışı olduğu için fiziksel fare tıklamasının sonucu doğrulanamadı. Düğmelerin EventSystem alıcısı ve bağlı eylemleri doğrulandı. Gerçek telefon performansı ve dokunma denemesi bu turda ölçülmedi.
