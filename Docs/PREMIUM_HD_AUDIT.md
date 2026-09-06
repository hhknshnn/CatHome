# Premium HD ve canlı açılış — 5 Eylül 2026

Kullanıcı kararı: genel görüntü daha net/premium olacak; açılıştaki fotoğraf yerine oyunun kendi kedileri animasyonla gösterilecek. Önceki statik title görseli kararı bu istekle değişti.

## Uygulama

- `TitleCatShowcase`: seçili ırk önde, iki farklı oyun kedisi yanında. Ortak gerçek iskelet/klipler; uyuma, oturma/ilgilenme, pati hareketi ve tımar. Dünya kedisinden bağımsız görseller; kayıt, satın alma veya aktivite bileşeni yok.
- 1920×1080 alt sınır, ekran oranını koruyan ve 3840×2160 ile sınırlanan çizim. 30 Hz sunum saati oyun zamanından bağımsızdır. Hareket azaltmada sabit poz; odak kaybında son kare tutulur. İlk açılışta daima dolu kare; kapanışta set, ışıklar, geçici meshler ve RenderTexture bırakılır. Başlangıç odası yüklenirken set kaybolmaz.
- Set, oyunun gerçek premium koltuğu/lambası/bitkisi/banki/minderlerini yalnız mesh ve materyal olarak kullanır. Kamera otomatik render etmez; yalnız 30. katmanı açık render isteğiyle çizer. Işıklar yalnız o istek boyunca açılır. Gameplay kameralarında bu katman kapalıdır.
- `TitleScreenLayout` sol dock ve sağ kısayolları SafeArea içinde ölçekler. Fredoka, neon logo, premium düğmeler ve hesap/yeni oyun/onboarding/mağaza/odalar/oyunlar akışı korunur.
- `PremiumHdVisualBuilder`: Mobile/PC native renderScale 1, 4× MSAA, 4 kemik ağırlığı, anisotropic filtreleme. Mobile 2048 / 2 cascade; PC 4096 / 4 cascade yumuşak gölge. Sekiz oda + Runner + Catch kamerasında yüksek SMAA, dithering, HDR; dinamik çözünürlük kapalı. Ortak Bloom 0.24, exposure 0, contrast 7, saturation 12 korunur.
- Oda seçici ve HOME mağaza görselleri sekiz gerçek odadan 1920×1080 / 4× AA ile yeniden çekilir. Doku ithalatı NPOT boyutlarını korur. Dış mekân önizlemelerinin eski piksel parlaklık yükseltmesi kaldırıldı, stüdyo ışığı dengelendi. Kart UV'leri yeni gerçek 16:9 fotoğraflara göre yeniden kuruldu.
- Mağaza başlık/açıklama sütunu fiyat ve eylem sütunundan 20 px ayrılır. Uzun HOME açıklamaları artık fiyatın arkasına uzanmak yerine kendi alanında satır kırar; prefab ve canlı ekran ölçümüyle kilitlidir.

## Tekrarlanabilir üretim

Edit Mode'da, diyalogsuz: `PremiumHdVisualBuilder.BuildSilently()`, `TitleScreenBuilder.BuildSilently()`, `RoomPreviewCaptureBuilder.CaptureSilently()`. Görsel boyut/oran sözleşmesi değiştiğinde `RoomSelectorPanelBuilder.BuildSilently()` ve `ShopPanelBuilder.BuildSilently()` ile kart kırpımları güncellenir. Title seti `Assets/Art/Title/LiveCats` içindedir; poster de bu setin gerçek render'ıdır.

## Doğrulama

Yeni kontroller: `PremiumHdTests`, `TitleCatShowcaseTests`; mevcut premium title/overlap testleri yeni davranışa güncellendi. PlayMode yalnız Unity Test Runner penceresinden başlatıldı.

- Son EditMode: **402/402**, 9.48 sn; mağaza metin boşluğu düzeltmesinden sonra tam paket yeniden geçti. Kanıt: `QA/PremiumHD/EditMode.xml`.
- Tam PlayMode: **56/56**, yaklaşık 9 dakika 11 saniye; 80 eşya × 10 ırkın **800/800** etkileşim matrisi dahil. Kanıt: `QA/PremiumHD/PlayMode.xml`.
- Son açılış değişikliğinden sonra odaklı PlayMode: **4/4**, 9.29 sn. On ırkın gerçek kemik animasyonu, ilk kare/odak/kapanış, reduced-motion ve oda ışığının tam geri yüklenmesi. Kanıt: `QA/PremiumHD/PlayMode-TitleFinal.xml`. Ek ışık testiyle toplam 57 farklı PlayMode testi bu iki koşuda kapsandı; tek bir 57-testlik tam koşu olarak raporlanmaz.
- `LevelContentValidator`: **0 hata / 0 uyarı**. Normal Play Console: **0 hata / 0 uyarı**. Üç ana sahne, Living Room aktif; kamera / AudioListener / EventSystem **1 / 1 / 1**. Sphynx seçimi ve oyuncu kaydı korundu. Açılış kapanınca sunum seti ve render kaynakları bırakıldı; ortam ışığı katsayısı 0.202662 kaldı.
- Canlı `GetWorldCorners`: 1920×1080, 1440×1080 (4:3) ve 2400×1080 (20:9) açılışında sekiz düğme, **0 taşma / 0 çakışma**. HD çıktı sırasıyla 1920×1080, 1920×1440 ve 2400×1080. Evde sekiz, HOME mağazasında altı, oda seçicide sekiz görünür etkileşim alanı: **0 çakışma**. Scroll maskesinin dışındaki öğeler ve modal arka planı bu ölçüme dahil edilmez. HOME başlık/açıklamalarında minimum fiyat boşluğu **20 px**.
- Görsel inceleme: `Assets/QA/PremiumVisuals/PremiumHD/Title_1920.png`, `Title_AnimationA.png`, `Title_AnimationB.png`, `Title_Tablet_4x3.png`, `Title_Wide_20x9.png`, `Home_HD.png`, `Shop_HOME_HD.png`, `Rooms_HD.png`. Bunlar gerçek GameView kareleridir; kamera ekran görüntüsü aracındaki gamma dönüşümü kullanılmadı.
- Test oturumu/No Throttling kalıntıları temiz. Son düzenleme görünümü `GameScene` + `CatHome_UI` + `LivingRoom_Level01`; Living Room aktif, `playModeStartScene = null`, yalnız `DisableSceneReload`, domain reload açık.

Gerçek Android cihazında FPS, termal ve uzun oturum ölçümü bu masaüstü doğrulamasının kapsamında değildir. HD sunumun cihazdaki maliyeti yayın öncesi gerçek cihazda ölçülmelidir. Git commit/push kullanıcıya aittir; bu çalışma commit/push yapmaz.

### Işık yaşam döngüsü için ek kilit

Odanın güneşinin sunum katmanına ikinci ana ışık olarak eklenmesi canlı QA'da bulundu. Açık render isteği boyunca dış ışıkların yalnız sunum katmanı maskesi kapatılır; güneş ve sis, aynı çağrının `finally` bloğunda aynen geri yüklenir. **`RenderSettings.ambientProbe` her kare okunup geri yazılmaz.** Unity bu değeri yoğunlukla ölçekleyebildiği için böyle bir “geri yükleme” tekrarlandıkça katsayıları büyütüp ev görüntüsünü karartabilir. Son kod ortam ışığına/probe'a hiç yazmaz; `TitleRenderingRestoresTheRoomsLightingExactly` ardışık render'larda katsayıların da sabit kaldığını kontrol eder.
