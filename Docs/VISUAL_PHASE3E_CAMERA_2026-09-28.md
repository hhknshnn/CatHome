# Phase 3E — Living Room Camera & Composition

Başlangıç: 28 Eylül 2026, 11:01:41 UTC. Üç kamera iterasyonu; ikinci seçildi. Kapanış süresi QA/closure.json içinde. Phase 3F başlatılmadı.

## CAMERA CHANGES

| Parametre | Önce | Son |
|---|---|---|
| Position | (0, 3.6, -6.5) | (0, 2.7, -6.5) |
| Rotation | (23°, 0°, 0°) | (18°, 0°, 0°) |
| Dikey FOV | 38° | 36° |
| Projection | Perspective | Perspective |
| 1920×1080 dünya viewport'u | (0,80,1920,1000) | Aynı |

Referans `C:\Users\HAKAN\Desktop\ChatGPT Görseli 28 Eyl 2026 13_34_40.png`, kamera kararlarından önce Computer Use ile Photos içinde açılıp incelendi. Mevcut ve sonraki Game View görüntüleri aynı 1920×1080 seçimiyle karşılaştırıldı.

Sadece sahne kamerasını değiştirmek yeterli değildi: oda etkinleşmesi ve LevelLoader ortak profili yeniden uyguluyor. HomeRoomCameraProfile artık yalnız LivingRoom_Level01 sahne yolunda yeni değerleri seçiyor. Diğer odalar ve gelecekteki listelenmemiş odalar eski (0,3.6,-6.5), 23°, 38° profilini koruyor. Kamera üretici referansları, validator, mevcut ekran yakalama kontrolü ve mevcut iki test dosyasının kamera beklentileri buna uyarlandı. Oynanış, hareket veya UI kodu değiştirilmedi.

Salon sahnesinde yalnız Camera FOV, kamera Transform pozisyon/rotasyon ve HomeWorldViewport referenceFieldOfView değişti. Unity sahne kaydının eklediği ilgisiz serileştirme farkları güncel başlangıç kopyası kullanılarak çıkarıldı; kalan YAML farkı dört kamera alanıdır.

## BEFORE → AFTER

1. Kamera 0,9 m alçaldı ve aşağı bakış 5° azaldı: daha az tepeden bakış ve daha doğal mobilya cepheleri.
2. Kayıtlı kedi görünümünün izdüşüm kutusu genişliği 136,1 → 153,4 px (%12,7); yüksekliği 209,2 → 218,3 px (%4,4). Karakter ölçeği değişmedi.
3. Koltuğun izdüşüm genişliği 327,8 → 373,3 px (%13,9); sehpanınki 201,1 → 228,4 px (%13,6). Mobilya grubu daha güçlü okunuyor.

Bu sayılar aynı nesnelerin dünya eksenli sınır kutusu köşelerinin projeksiyonudur; gerçek siluet piksel alanı veya bütün kedi ırkları ölçümü değildir. Geometrik ölçümler `composition-metrics.json` içindedir.

İterasyon 1: (0,2.65,-5.8), 17°; istenen FOV 40° editör önizlemesi tarafından 38°'ye döndürüldü ve görsel yargı gerçek 38° görüntüde yapıldı. Sağ koltuk/ön hareket alanı fazla sıkıştı. İterasyon 2: son ayarlar. İterasyon 3: (0,2.25,-6.5), 14°, 36°; daha düşük görüş, ön mimari kenarı fazla açığa çıkardığı için seçilmedi. Dördüncü tuning yapılmadı.

## REFERENCE MATCH

- Viewing angle ve room immersion iyileşti; oda hâlâ genel bakış kamerasıyla okunuyor.
- Cat/furniture presence arttı, fakat referanstaki çok güçlü mobilya kütlesine ulaşmadı.
- Empty-space control sınırlı kaldı. Boş zemin ve üst duvar hâlâ belirgin; zemin yüzdesinde büyük azalma iddia edilmiyor.
- Ön mimari kenar son kadrajda ince bir şerit olarak görünür. Daha alçak üçüncü denemedeki belirgin kenar görünümünden kaçınıldı.

## GAMEPLAY / UI RISKS

1920×1080 editör görünümünde kayıtlı kedi, mama/su, yatak, koltuk ve sehpa görünür. Koltuk sınır kutusu sağ kenardan yaklaşık 58 px içeride; alt sınırı 348 px, üst sınırı 661 px. Kayıtlı kedinin alt sınırı 393 px; kaynak başlangıç kedisinin alt sınırı 152 px ve alt şerit 80 px. Bunlar mevcut pozlardır; bütün yürüyüş alanı veya hareketli siluet garantisi değildir.

Mevcut en-boy oranı uyarlaması korundu. Bu tur diğer telefon oranları, fiziksel cihaz, yürüyüş rotaları ve tüm etkileşim animasyonları Play Mode'da denenmedi. Joystick üzerindeki ön-sol bölge ve ön kenardaki hareket için fiziksel oynanış kabulü yok.

## UNRESOLVED

Oda yoğunluğu, mobilya yerleşimi, dekor eksikliği ve karakterin fiziksel ölçeği kamera kapsamı dışında korundu. Referansın doluluk hissi yalnız kamerayla tamamen karşılanmadı. Kullanıcının yeni kadraj onayı henüz yok.

Test raporu yan etkisi: eski UiQaTestSession SessionState hedefi nedeniyle önceki `CAT_NAME_CASE_FONT_2026-09-28/case-font-playmode-native.xml` dosyasının üzerine bu turun tek kamera testi yazıldı. Özgün XML kurtarılamadı; eski manifestte bu dosya açıkça geçersiz kanıt olarak işaretlendi. Önceki 22/22 tarihsel sonuç iddiasını bu yeni XML ile doğrulamayın. Bu turun XML'i kendi klasörüne kopyalandı ve oturum rapor hedefi bu klasöre alındı. Oyun kayıtları etkilenmedi.

## RESULT

Kamera açısı ve nesne okunabilirliği hedeflerinde güvenli, ölçülü ilerleme sağlandı. Referansın boş alan/doluluk hedefi kısmen karşılandı; tam görsel eşleşme iddiası yok.

Mevcut `EveryRoomScene_KeepsItsApprovedCameraAndSharedCatScale` EditMode testi 1/1 geçti. Salon yeniden açılışı ve runtime Apply metodunun iki kez uygulanması yeni profili korudu; ayrı geçici sahnede listelenmemiş kamera eski profili aldı. Bu kontroller Play Mode testi değildir. PlayMode test beklentileri güncellendi ancak çalıştırılmadı.

Son Console sayaçları 0 hata, 18 uyarı, 6 bilgi; test aracı hazırlık/temizlik ve rapor mesajları mevcut. 0 eksik script / 0 eksik malzeme. Üç temiz normal sahne; Unity açık, Play/QA/derleme kapalı.

8738 okunabilen başlangıç dosyasından 7 dosya değişti: salon sahnesi, bir runtime kamera profili, üç kamera authoring/validation dosyası ve iki mevcut test dosyası. Diğer 8731 dosya aynı. Bir özgün animasyon başlangıç erişim hatası nedeniyle hash kapsamı dışında. Işık, palet, malzeme, zemin dokusu, kedi/mobilya/geometri, diğer sahneler ve ProjectSettings korundu. Native testin UI sahnesi serileştirme yan etkisi, aday baytlar BU TURUN başlangıç SHA256 değeriyle birebir eşleştirilerek geri alındı; tarihsel sürüm körlemesine yüklenmedi. Dört gerçek kayıt byte aynı.

Kanıt: `QA/VISUAL_PHASE3E_CAMERA_2026-09-28`. Gerçek Game View dosyaları `before.png` ve `final.png` (1920×1080); Computer Use karşılaştırmaları `reference-inspected.png`, `before-window.png`, `iteration-1-window.png`, `iteration-2-window.png`, `iteration-3-window.png`, `final-window.png`. Esas kapanış dosyaları `preservation-final.json`, `editor-final.json`, `ui-restored.json`, `runtime-profile-check.json`, `console-counts.json`, `closure.json`.

APK, commit, push ve yayın yapılmadı. Bu aşama burada durdu.
