# CAT HOME — Phase 3A: Living Room Lighting Pass

28 Eylül 2026. Başlangıç 08:30:33 UTC. İki anlamlı ışık iterasyonu; süre ve kapanış `QA/VISUAL_PHASE3A_LIGHTING_2026-09-28/closure.json` içinde.

## CHANGES MADE

Yalnız `Assets/Scenes/Levels/LivingRoom_Level01.unity` içindeki dört mevcut ışık düzenlendi. Ana ışık tepeden gelen 88° yerine 52° eğim ve 315° yatay yönde. Soft shadow korunarak gölge gücü 1 → 0,65 indirildi. ReferenceSoftFill karşı yöndeki duvarları nötr, düşük güçlü ışıkla okunur tutar. Peach ve cyan dolguların rengi nötrleştirildi ve yoğunluğu düşürüldü.

WindowLight ve CeilingLight aynen korundu. RoomLightingController'ın gün/gece eğrileri ana ışık ve tavan ışığının parlaklık/rengini yönetmeye devam eder. Bu alanlar kalıcı olarak değiştirilmedi. Yeni ışık eklenmedi; altı mevcut ışık korunur.

## BEFORE → AFTER

1. Kedinin ve mobilyaların gölgeleri tepede sıkışmış lekelerden okunur bir yan yöne geçti; kedi, sehpa ve koltuk zeminden daha iyi ayrılıyor.
2. Sol duvardaki yoğun sıcak leke ve cyan dolgu katkısı azaldı. Sağ duvar nötr dolgu sayesinde okunur kaldı.
3. Zemin daha az yıkanmış görünüyor; gölgelerin koyuluğu azaltıldı, sıcak ve aydınlık atmosfer korundu.

Gerçek Unity Game View, aynı 1920×1080 çözünürlük ve aynı kamera ile Computer Use üzerinden değerlendirildi. İlk ve ikinci iterasyon görüntüleri sohbet araç çıktılarında; kaydedilmiş sahnenin son görüntüsü `QA/VISUAL_PHASE3A_LIGHTING_2026-09-28/pc-final-reloaded.png`.

## VALUES

RGB değerleri Unity Light.color alanının 0–1 değerleridir. Dönüşler Euler X/Y/Z derecedir.

| Işık | Değişen değerler |
|---|---|
| Directional Light | Dönüş (88, 0, 0) → (52, 315, 0); Soft gölge gücü 1 → 0,65 |
| ReferenceSoftFill | Dönüş (22, 12, 0) → (28, 70, 0); RGB (1, 0,96, 0,88) → (0,96, 0,98, 1). Yoğunluk 0,38 korunur. |
| PremiumPeachFill | Yoğunluk 0,60 → 0,22; RGB (1, 0,6353, 0,4941) → (1, 0,94, 0,88) |
| PremiumWindowBounce | Yoğunluk 0,90 → 0,35; RGB (0,4392, 0,8824, 1) → (0,88, 0,95, 1) |

## MOBILE / DAY-NIGHT RISKS

- Mobile Forward profilinin yalnız bellekteki geçici kopyasında SSAO kapalı, ek ışık gölgeleri kapalı, özgün 0,85 render scale ile aynı Game View incelendi. Kedi ve mobilya hacmi ile temas gölgeleri okunur. Kaynak mobil profil/renderer dosyaları değişmedi; geçici kopyalar silindi ve özgün PC profili geri geldi.
- Bu bir editör önizlemesidir: gerçek cihaz FPS/ısınma/görsel kabulü değildir. Mobil çalışma anındaki çözünürlük sınırı ve kamera AA geçişinin tamamı taklit edilmedi.
- Mevcut controller eğrilerinden 13:00 ve 00:00 ışık örnekleri alındı. Ana/tavan/pencere ışığı ve ortam yoğunluğu geçici uygulanıp görsel olarak incelendi; sonra başlangıç değerleri aynen geri getirildi. Örnekler `mobile-day-light-sample.png` ve `mobile-night-light-sample.png`.
- Oyun saati, pencere gökyüzü, materyaller ve güneş huzmesi değiştirilmedi. Bu nedenle gece görüntüsü ışık ilişkisini sınar; tam gece penceresi/geçişi veya Play Mode gün/gece kabulü değildir. Temsilci ışık örneklerinde temel uyumsuzluk görülmedi.

## UNRESOLVED VISUAL ISSUES

Zeminin geniş tek renkli yüzeyi ve halının belirgin mint rengi hâlâ kompozisyonda baskın. Sol duvarda küçük açık bir vurgu kalır. PC'de yönlü gölgelerin şekli hâlâ nettir; Soft gölge filtresi ve kalite bütçesi yükseltilmedi. Materyal, mimari palet, dekor ve post-processing bu aşamanın dışında; Phase 3B uygulanmadı.

## VALIDATION / PRESERVATION

- Son Console: 0 error / 0 warning. Sahne yeniden açıldı; altı ışık, doğru ana güneş referansı, 0 eksik script. Üç sahne temiz; Play, QA, derleme kapalı; Unity açık.
- Unity SaveScene eski alanları da yeniden serileştirdi. Bu yan değişiklikler güncel görev başlangıcının tam sahne kopyasından ayıklandı. Nihai fark yalnız dört Light ve iki Transform bloğunda, toplam sekiz değişen satırdır; diğer bütün sahne blokları birebir aynı. Sahne yeniden açılarak son değerler doğrulandı.
- Assets/ProjectSettings/Packages içindeki 8.702 okunabilen başlangıç dosyasından yalnız salon sahnesi değişti; diğer 8.701 dosya aynı. Yeni/silinen proje dosyası yok. Bir özgün animasyonun başlangıç hash'i erişim engeli nedeniyle alınamadı: `A_CartoonAnimal_Cat_Eat.anim`. Tam tüm-varlık hash doğrulaması iddia edilmez.
- Ana kayıt, recovery, iki mevcut kayıt yedeği ve mevcut TestResults.xml byte olarak aynı. Play açılmadı, gerçek kayıt yükleme/yazma/geri yükleme yapılmadı. Tercih setter'ları çağrılmadı; ayrı tercih hash karşılaştırması yapılmadı.
- Kamera, UI, gameplay, collider, navigation, materyal, Volume, shader, prefab, kod, mobil performans ayarları değişmedi. APK, commit, push, yayın yok.
- Kanıt: `lights-before.json`, `allowed-scene-blocks.json`, `preservation-final.json`, `editor-final.json`, `scope-verification.json`, `closure.json` ve PNG'ler aynı QA klasöründe.

## RESULT

Bu kapsamda hedefe ulaşıldı: daha yönlü, okunur ve düşük renk lekeli; aydınlık/cozy salon. Kullanıcının görsel onayı henüz alınmadı. İki iterasyondan sonra duruldu; Phase 3B başlatılmadı.
