# Dönüşte bacak anatomisi — 14 Eylül 2026

Kullanıcı çadır ve üst kat kitaplığında kedinin arka bacaklarının kırılmış gibi göründüğünü bildirdi; düzeltme hedefi 30 dakika, tamamlanma süresi yaklaşık 60 dakika. Ek süre, İran kedisinde yeni basış aralığının ön patiyi dar koltukta geride tutmasının da giderilmesi ve son doğrulamalar için gerekti. Ortak basılı dönüş düzeltildi; son görünüm 14 Eylül 2026'da kullanıcı tarafından onaylandı ve bu düzeltme işi kapatıldı.

## Kullanıcı onayı — 14 Eylül 2026

Kullanıcı “tamam çok güzel. md güncelle ve checkpoint ver.” diyerek son sonucu onayladı. Çadır/kitaplıktaki kırık gibi bacak görünümü ve son ortak dönüş düzeltmesi için yeniden onay beklenmez. Onaylı hareketler sonraki işlerde korunur. Önceki raporlardaki bu sonuca ilişkin onay bekleme ifadeleri tarihseldir.

Bu adım yalnız Markdown ve devam notlarını günceller. Aşağıdaki ölçümler son geliştirme turuna aittir; oyun kodu, sahneler veya kayıtlar bu adımda değiştirilmedi, testler yeniden çalıştırılmadı. Devam noktası: [güncel checkpoint](CatHome_Checkpoint_2026-09-14.md).

## Neden ve düzeltme

Önceki dönüş hesabı, kaynak uyluğu doğrudan yeni kalça–pati eksenine yansıtıyordu. Sabit pati yana kaydığında dizin bükülme düzlemi de yana dönüyordu. Ayrıca dar yüzeyin en yakın noktası iki pati için aynı seçilebiliyordu. Önceki testler temas ve kaymayı ölçüyordu; diz anatomisi ve sağ/sol pati sırası eksikti.

`CatSurfaceTurnMotion` artık eklem düzlemini önce özgün duruşun kendi bacak ekseninden çıkarır, sonra hedef eksene taşır. Ön dirsek ve arka diz kaynak bükülme yönünü korur. Dönüş çevrimi başına açı 28° yerine 18°; tek pati sırası ve akıcı gövde dönüşü korunur. Yüzey üzerinde üç basılı patiye yer ayrılır, arka patilerde sağ/sol sıra korunur; ön patiye ayrı basış yeriyle doğal çapraz adım serbestliği verilir; boş destek bulunmazsa mevcut basış tutulur. Erişim hesabı denge kaymasını da içerir; en fazla 15 cm, asılı koltukta 20 cm çömelme payı vardır. Kemik boyları değişmez.

İlk canlı karşılaştırmada dizin yana yönelme bileşeni çadırda 0,944, kitaplıkta 0,882 idi; yeni 0,45 sınırını aşan 100 ve 65 arka bacak örneği yakalandı. Ham başlangıç ve ara karşılaştırmalar `before-after-diagnosis.json` içinde; son sürümün doğrulaması aşağıdaki kaynaklardır.

## Son doğrulama

- Altı benzersiz native testin son sonucu **6/6**. Kaynaklar `native-final-manifest.json`; asılı koltuğun son ön pati düzeltmesi sonrası `final-release/native.xml` geçerlidir. `native-first`, `native-final`, `final-acceptance` ve `final-confirmed` içindeki ara başarısız sonuçlar final başarı diye sunulmaz.
- Sekiz oda, **39 sıçrama rutini**: anatomik diz düzlemi, sağ/sol arka pati sırası, tek pati hareketi, destek, özgün sıçrama ve kontrol iadesi. Tüm odaların her eşyasını on ırkla test ettiğimiz anlamına gelmez.
- Bildirilen çadır ve kitaplıkta **2 eşya × 10 ırk**; dar koltuk/çadır/asılı koltukta **3 × 10**; raf/çadır gerçek pati desteği **2 × 10**. On ırkta 15/30/60 fps kaynak sıçrama ve kemik kontrolü. Duraklatma/iptal de geçti.
- **26/26 EditMode**, validator **0 hata / 0 uyarı**.
- Çadır/asılı koltukta **20/20** ek canlı gövde çevrimi. Geniş küre probunun uyarısı gerçek deri ağı örneklemesiyle ayrılır; ölçülen en büyük deri girintisi 1.17 mm. Bu örnekleme bütün deri üçgenlerinin sürekli çarpışma ispatı değildir.
- Gerçek oyun düğmeleriyle **3/3**: çadır, kitaplık, asılı koltuk; tek tamamlanma, açık çıkış, kontrol iadesi. [Galeri](QA/TURN_LEG_ANATOMY_2026-09-14/index.html) 34 yeni gerçek PNG içerir; video yok. Fiziksel telefonda performans ölçülmedi.

## Korunanlar ve kapanış

`CatJumpMotion`, `CatActivityAnimation`, yedi kaynak FBX, 14 sahne ve üç gerçek kayıt başlangıçla aynı. Sadece ortak dönüş sınıfı ve `JumpContinuityTests` değişti. Ana/recovery SHA256: `97590845CADE70487CB0458DF1CA6EE932C21ACB1A91A16A4626B091F4A9F458`. Diğer kayıt hashleri `integrity-final.json` içinde. 16 tercih geri yüklendi; geçici font/editör dosyaları bu turun taze kopyasından geri alındı.

Son teknik teslimde QA/Play/çekim/derleme kapalıydı; üç temiz normal sahne, tek kedi/kamera/dinleyici ve salt okunur ön izleme vardı. APK/arşiv/commit/push/yayın yapılmadı; QA Git dışında. Önceki salon tablo yakın giriş konusu kapsam dışı ve açık kalır. Son düzeltme kullanıcı onayıyla kapandı; yeni iş kullanıcı talebine göre planlanır.
