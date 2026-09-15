# Genel sıçrama ve son dört oda — 12 Eylül 2026

İstenen sıçrama düzeltmesi ve bahçe, balkon, avlu, üst kat çalışmaları tamamlandı; kullanıcı incelemesine hazır. Son kayıt: 2026-09-12T02:56:04+00:00. Başlangıç 11 Eylül 20:45 UTC; ilk ortak sıçrama düzeltmesi yaklaşık iki saat içinde ölçüldü. Oda düzenleri sırayla tamamlandı.

## Sıçrama

Eski çıkış/iniş kliplerinde kalan iskelet taşıması destek düzeltmesi bırakılınca görünür gövdeyi tek karede yaklaşık 0,668 m kaydırıyordu. Bazı mobilyalarda inişten uykuya doğrudan geçiş de ikinci bir sıçrama oluşturuyordu. Boştaki kuş bakışı, öne kaydırılmış hareket kapsülünü gövdeyle birlikte çevirerek inişten sonra ek yer değişimine yol açıyordu.

`CatJumpMotion` özgün `|Jump` klibini korur. Hazırlık, yüksekliğe uyarlanan uçuş, basış ve toparlanma tek zaman çizgisindedir. Kedi gerçek uçuş yönüne döner; varış yönünü havada alır. İnişte kök ve yatay görsel düzeltme sabit kalır. `CatActivityAnimation` o karenin kaynak pozunu fazladan ilerletmeden örnekler. Mobilya dinlenmelerinde oturma, yatma, uyanma ve ayağa kalkma geçişleri destek üstünde tamamlanır. Kuşlara boşta bakış yalnız başı çevirir.

On ırkın kullandığı yedi kaynak FBX dosyası başlangıçla birebir aynı. Kemik uzunluğu ve kedi ölçeği korunur. Eski TowelJumpUp/Down durum kimlikleri uyumluluk için durur; güncel mobilya uçuşu özgün Jump kaynağını kullanır. Runner/Catch kendi hareket sistemini korur; ilgili mini oyun kontrolleri ayrıca çalıştırıldı.

Yemliğin `poleStandDistance=0.66` değeri prefab ve balkon sahnesine açıkça kaydedildi. Yalnız C# alan başlangıcını değiştirmek, Unity'nin bellekte tuttuğu eski seri değeri güncellemez. Son duruş 5° çapraz ve sağ pati vuruşudur; kaynak baş hareketi korunur. Önceki uzatma denemeleri son kanıt değildir.

## Dört oda

| Oda | Son düzen ve davranış | Yalnız izleme |
|---|---|---|
| Bahçe | Bitkiler sol/arka kenarda, hamak ve şezlong solda, bistro ve mangal sağda. Çeşme ile tek top ön yan bölümlerde. İki eski ek top istasyonu kaldırıldı. Yüksek saksıya gerçek sıçrama, hafif eşeleme ve iniş var. | Kuşlar |
| Balkon | Askılı koltuk ve raf arka tarafta, bank sağda; saksılar ve küçük masa solda. Tente dekor. Yemliğe gerçek direğin açık yanından pati atılır; gövde/baş için 0,66 m kök mesafesi kullanılır. Kupa gerçek temas sonrasında düşer. | Fenerler |
| Avlu | Salıncak sağda, kemer arkada, yemek takımı sağ ön bölümde; şemsiye, bitkiler, ateş ve çeşme sol çevrede. Dekor köşe saksıları çitin arkasına alındı. Saksıya sıçrama, salıncakta destekli dinlenme ve ateş yanında doğal yatış var. Işık dizisi dekor. | Eğrelti otu |
| Üst kat | Divan ve plak solda, kitaplık/minderler arkada, masa sağda. Kitap yığını merdiven korkuluğunu kapatmaz. Divanın başlığı arkada; kedi açık tarafta yatar. Kitap/plak ancak üç gerçek pati temasından sonra hareket eder. | Duvar galerisi |

Yürüyüş alanı açık; merkezde yalnız alçak halı/paspas gibi zemin vurguları kalır. Bahçenin mevcut mimari oyun parkuru korunur. Dört odanın paspaslarında yana devrilme ve gövde esnetme yerine hafif yoğurma, doğal oturma/yatma/kalkma vardır. Yüksek üç saksıda zeminden yukarı kayarak yürüme kaldırıldı. Oda koleksiyonları ve satın alma kimlikleri değişmez; 40 ürün, 37 etkin eşya rutini ve bahçede bir ücretsiz kuş izleme rutini bulunur.

Yeniden üretim kaynakları `OutdoorArrangementProfile`, `OutdoorPolishBuilder`, `HomeRoomArrangementBuilder` ve `RoomActivityLayoutBuilder`. Son üst kat divanı (-3,23;0;0,22), yaw90; plak (-3,05;0;-1,40), yaw270. Avlu bitkisinin açık giriş noktası yerel (0,203;0;-0,626). Yerleşim, giriş, engel ve temas noktaları birlikte korunmalıdır. Dört oda seçici/mağaza ön izlemesi yenilendi. Onaylı salon, banyo, mutfak ve yatak odası sahneleri değiştirilmedi.

## Son doğrulama

- Sekiz odada **39/39 sıçrayan rutin, 76 sıçrama**: gerçek kaynak klip, inişte kök/yatay görsel sabitliği, gövde geçişi, açık çıkış ve kontrol iadesi. Ölçülen iniş kök adımı, yatay görsel adımı ve bırakılan kontrolün kök adımı **0 m**. En büyük doğal geçiş kalça adımı 0,069351 m; kabul sınırı 0,09 m.
- On ırk × 15/30/60 fps = **30 çevrim**: kaynak eklem açıları ve kemik konumlarıyla karşılaştırma. Açı toleransı 0,06°, yerel konum toleransı 0,1 mm. Üç yüksek saksı × on ırk = **30 çevrim** ayrıca geçti.
- Dört oda eşya/ırk matrisi **370/370**: bahçe90, balkon90, avlu90, üst kat100. Başlama/tamamlama, hareket, destek düzlemi, görünür tepki, açık çıkış, normal ölçek ve kontrol iadesi.
- Son dört oda kamera denetimi **37/37 etkin eşya rutini**. Gerçek kalça–omuz yönü, tamamlanma ve açık çıkış ölçüldü. Bahçedeki ücretsiz kuş eylemi ayrıca gerçek düğmeyle denendi.
- Balkon yemlik/kupa ve üst kat kitap/plak için on ırkta **40 fiziksel temas çevrimi**; üç temas, 25 mm yakınlık, sabit kök ve iptal/duraklatma temizliği. Yemlikte direk her ölçülen vuruşta omzun en az 8 cm önünde kaldı.
- Gerçek oyun düğmeleri **38/38**: bahçe10, balkon9, avlu9, üst kat10. Tek tamamlanma, açık çıkış ve kontrol iadesi. Balkon yemliği son duruşla yeniden çekildi; eski yemlik karesi son kanıt sayılmaz.
- Son hedefli EditMode **70/70**, içerik doğrulayıcı **0 hata / 0 uyarı**. Catch dört test geçti. Runner on ırk sıçrama/iniş/eğilme ve salon oyuncak faresinin on ırk sıçraması önceki hedefli paketlerde geçti.

Kanıt: `final-native/native.xml` içindeki sıçrama/ırk/üst kat kontrolleri; `last-verified2/native.xml` son dört oda görünürlüğü ve balkon; `release-verified/native.xml` (bahce/avlu/ust kat matrisleri); `planter-jumps-verified/native.xml`; `final-edit-verified/edit.xml`; `actual-buttons-summary.json`; `validator.json`. İlk veya ara başarısız XML dosyaları son başarı toplamına dahil edilmez. Galeri `QA/GLOBAL_JUMP_ROOMS_2026-09-11/index.html`: **55 gerçek Unity PNG**; video veya indirme arşivi yok. Telefon performansı ölçülmedi.

## Ayrı bulgu: salon tablosu

Ek sekiz oda eşya taraması **11/12 testlik pakette tek hata** verdi: `room.modern-painting` için açık ve 0,80 m yakınlık koşulunu sağlayan giriş bulunamadı. Normal oyun düğmesi hazırlığında da seçilebilir yakın zemin bulunamadı. Sıçrama testleri veya yeni dört oda bu hata nedeniyle başarısız değildir. Tabloya ait izleme yolu bu turda değiştirilmedi; ortak yön yardımcısının izleme kolu da aynı davranışı koruyor. Onaylı salon yerleşimi bu ayrı bulgu için değiştirilmedi. Sekiz odanın bütün eşya eylemleri tamamen geçti şeklinde raporlanmamalı. Ayrıntı `final-native/room-default.json` ve `painting-entry-check` altındadır.

## Kayıt ve devam durumu

Üç gerçek kayıt başlangıçla aynı: ana/recovery SHA-256 `42291458FBAC457CF9C505417B278E97CA3B81F9210498EA4270AA3ACE38A93F`; CP2 `03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D`. Tarihsel kayıt geri yüklenmedi. Yedi kaynak FBX aynı; 14 sahnenin yalnız dört hedef oda sahnesi değişti. 16 tercih ve çekim ayarları geri yüklendi. QA/Play/derleme kapalı; GameScene, CatHome_UI, LivingRoom temiz; bir kedi, bir kamera ve bir ses dinleyici. Önceden var olan çalışma değişiklikleri korundu. APK, paket arşivi, commit/push veya yayın yapılmadı. QA dizinleri yerel ve Git dışında kalır.
