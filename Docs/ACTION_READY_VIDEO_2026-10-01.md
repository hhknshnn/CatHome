# Hazır eylem düğmeleri ve salon videosu — 1 Ekim 2026

Kullanıcı, sağ altta etkin görünen bir düğmenin tıklanınca yaklaşma/dönme önerisi vermesini istemedi. Düğmenin sunulması ile gerçek başlangıç aynı kabul koşullarına bağlandı. Başlangıç 07:04 UTC; kesin kapanış `QA/ACTION_READY_VIDEO_2026-10-01/closure.json`.

## Davranış

Mama/su için gerçek erişim, yön, ihtiyaç, doluluk ve temas; uyku için gerçek başlangıç veya yatakta yerleşmiş uyanma durumu gerekir. Ortak eşya düğmesi sahiplik, oda, enerji, ihtiyaç, hareket/kontrol kilidi, gerçek başlangıç ve yaklaşma yolu kontrollerini kullanır. Koltuğun geniş yakınlık istisnası kaldırıldı. Tıklama yeniden doğrulanır; araya engel veya ihtiyaç değişikliği girdiyse eski düğme sessizce çekilir. “Kaba dön/yaklaş” geri bildirimi bu düğmelerin alternatifi değildir. Güvenlik sınırları gevşetilmedi; hazır olmayan duruşu zorla başlatma veya ışınlama eklenmedi.

Hazır olma sorgusu fiziksel duruşu aynı çağrı içinde yeniden çözerek tüketmez; bir HUD yenilemesinde aynı eşyanın sorgusu paylaşılır. Tıklamalar yeni kontrol yapar. Mama/su pahalı temas kontrolü yalnız ucuz yön/mesafe koşullarını geçen adaylarda 0,15 sn aralıkla; ucuz kontroller her karede, tıklama kontrolü daima taze çalışır.

## Bulunan ek engeller

Berjerin ön iniş hedefi 10 cm yerine 26 cm öne alındı. Persian dahil on ırk tam çıkış–dinlenme–iniş döngüsünü tamamladı.

Tırmalama direğinin görünür tabanı 9,4 cm yerine 2,8 cm kalınlıkta üretildi; gerçek model ve çarpışma yüzeyi birlikte değişti. Üst kısım/sallanan oyuncak özgün yerleşiminde; sallanan oyuncak FBX'i güncel başlangıçla byte aynı. Ana modelin Blender kaynağı, üreticisi ve metrikleri güncellendi; paketli temas geometrisi yeniden üretildi. İki pati süpürmesi mümkün değilse mevcut kaynak gövde/uzuv kontrollerinden geçen yan duruşla yakın pati kullanılır. Üç hareketin her birinde gerçek el kemiği ile ışının vurduğu gerçek yüzey arasında 25 mm altı temas gerekir; 5 mm dışarı taşınan IK hedefi temas kanıtı sayılmaz. On ırkta üçer temas ve tek tamamlanma geçti.

## Nihai kanıt

Esas dosyalar `QA/ACTION_READY_VIDEO_2026-10-01/native-final-manifest.json` içindedir. Nihai 10 EditMode + 12 benzersiz PlayMode testi geçti (tekrarlarla 24 test çalıştırması). Ara tanı başarısızlıkları ve kesilen koşular nihai kabul değildir.

- Dört bakım/dinlenme/koltuk/sehpa testi: iki ırk, gerçek joystick ve HUD işaretçi olayları, TR/EN.
- Dört düğme koruması: yanlış yön/mesafe, boş kap/dolu ihtiyaç, aynı karede yeni engel, tıklamadan önce dolan ihtiyaç ve ucuz sorgu bütçesi.
- Sekiz odada 74 etkinlik tarandı: 64 hazır eylem gerçek HUD ile başladı ve iptal sonrası kontrolü bıraktı. Örneklenen alanda hazır duruş bulunmayan diğer 10 etkinliğin düğmesi sunulmadı. Bu kontrol 74 tam animasyon döngüsü veya her odada tüm eşyaların erişilebilirlik kabulü değildir.
- Berjer on ırk: 67 geniş temas adayı, 11.866 gerçek deri noktası kontrolü, ölçülen deri penetrasyonu 0. Tırmalama on ırk: geniş gövde/kol problarında eşik üstü temas adayı 0; dolayısıyla ek deri nokta incelemesi tetiklenmedi. Üçer gerçek pati temasının kontrolü ayrıca yapıldı.
- Tam salon videosu: 28/28 hazır, başladı, tam bir kez tamamlandı ve kontrol serbest bırakıldı. 3 bakım + 9 salon etkinliği + 16 kedi eşyası.

## Video

`QA/ACTION_READY_VIDEO_2026-10-01/CatHome_Salon_Etkilesimleri.mp4`: 1920×1080 H.264, 24 kare/sn, 4.608 kare, 3 dakika 12 saniye, sessiz. Unity GameView görüntüsünden üretildi; yapay video değildir. 10 salon eşyası açık; 16 kedi eşyası mevcut beş eşya sergileme sınırı içinde sırayla gösterildi. Her bölümde gerçek HUD düğmesi ve üretim hareketleri kullanıldı. Bölümler arasındaki test yerleştirmeleri kaydedilmedi; videoda bu kesmeler açıkça belirtilir. HUD sayaçları QA kopyasının test değerleridir, gerçek oyuncu ilerlemesi değildir.

`video-chapters.json`, `video-contact-sheet.jpg` ve yerel `index.html` bölüm listesini sağlar. MP4 dosyası tarayıcıda baştan sona oynatılarak ve kayıt kareleri görsel olarak incelenerek doğrulandı. Yerel geçici HTTP sunucusu yalnız kontrol içindi; teslim edilen dosya bağımsızdır.

## Koruma ve kapanış

Güncel başlangıçtaki 7.893 okunabilen dosyanın 7.881'i aynı; 12 değişik dosya (8 runtime C#, 2 mevcut test, ana direk FBX'i ve paketli temas verisi), 2 yeni dosya (video/kapsama testi + meta), eksik 0. Dört gerçek kayıt ve 16 tercih aynı. Bir özgün Eat klibi başlangıçta kabuktan okunamadı; eksiksiz tüm-varlık hash iddiası yok. ArtSource üretici/model kaynakları için ayrıca işlem öncesi kopyalar tutuldu; bu klasör 7.893 dosyalık karşılaştırmanın kapsamında değildir.

İki font önbelleği ve EditorSettings yalnız bu turun hash doğrulanmış başlangıcından döndü. Test sırasında iki editör takılması yaşandı; günlükler ve kurtarma sahneleri QA altında korundu. DX11 editörü yeniden açıldı. Seramik kap çekiminde geçici test yerleştirmesi fiziksel olarak yerleşmeden hazır sayılıyordu; test, yerleşme sonrası yeniden doğrulamayla düzeltildi ve nihai 28 bölüm geçti. Kurtarma/test sahneleri Assets dışındaki QA klasörüne korumalı olarak taşındı.

Son durumda üç temiz normal sahne, Unity açık (DX11), Play/QA/derleme kapalı, normal sahnede tek ses dinleyicisi. Test sahneleri arası geçişlerde ses dinleyicisi bulunmama uyarıları vardır; normal sahnede eksik dinleyici yok. Fiziksel telefon/FPS kabulü yok. APK, commit, push veya yayın yapılmadı.
