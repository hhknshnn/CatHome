# Salon — kapların eski şekli ve hacmi, 10 Eylül 2026

Kullanıcı mama/su kaplarının değiştirilmiş şeklini ve hacmini eski haline getirmeyi istedi. Önceki adımın kapları basıklaştırma kararı kaldırıldı.

- `PremiumCareStationBuilder.cs`, sığlaştırma öncesi kaynakla bayt bayt aynı hale getirildi. Kap ve içerikteki ×.3 yükseklik ile +7.2 mm ek kaldırma kaldırıldı; özgün ölçek, dolgu yüksekliği ve gerçek içerik temas noktası geri geldi.
- Sahnedeki iki kap da özgün dünya ölçeği **(1,1,1)** ve görünür boyut **0.28079 × 0.09609 × 0.27357 m** ile doğrulandı. Taban Y0.03100, üst kenar Y0.12709; önceki özgün ölçülerle aynı.
- Kap konumları, 78 cm aralık ve ince platform korundu. Orijinal Eating klibi ve son animasyon kodu değişmedi; ek baş/boyun bükmesi geri eklenmedi.

103 ürün kartı ve sekiz oda ön izlemesi yenilendi; gerçek Unity salon görüntüsü gözle kontrol edildi. Validator **0 hata / 0 uyarı**. Diğer yedi oda sahnesi aynı. Bu dar görsel geri alma için yeni PlayMode/tam animasyon taraması çalıştırılmadı; önceki sığ kaplarla alınmış 20/20 sonucu bu ölçülere yeni doğrulama olarak sunulmaz.

[Güncel salon görüntüsü](QA/BOWL_SHAPE_RESTORED_2026-09-10/screens/living-room.png) · [Ölçüler](QA/BOWL_SHAPE_RESTORED_2026-09-10/geometry-restored.json) · [Kontrol özeti](QA/BOWL_SHAPE_RESTORED_2026-09-10/verification-summary.json)

Gerçek kayıt/recovery başlangıç–son **2E25417DE5D9DEFF66995F24E2F5AA2D10EA1ABB1478C906A8D89BD186D2FD42**; CP2 değişmedi. 16 tercih/varlık bayrağı geri yüklendi. QA/Play/derleme kapalı, üç temiz sahne, tek kamera/ses dinleyici; salt-okunur ön izleme açık. APK, arşiv, commit/push, yayın veya kapatma yapılmadı. Sonraki konu yine tek tek, işlem öncesi kısa plan ve süreyle ele alınır.
