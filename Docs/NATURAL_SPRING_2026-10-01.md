# Yaylı oyuncak — doğal pati etkileşimi, 1 Ekim 2026

Başlangıç 12:07:50 UTC; kesin sınır 12:32:50 UTC. Yalnız `cat.feather-toy` / `CatEnrichmentMode.Spring` değiştirildi. Computer Use ile mevcut Play Mode eylemi gerçek düğmeyle başlatıldı; bileşen ve Animator incelemesi CatHome Unity MCP örneğinde yapıldı.

## Değişiklik

Mevcut doğal dönüşten sonra baş hedefi takip eder. Tek ön patiyle yavaş yoklama ve kısa ikinci vuruş, farklı bekleme/uzanma/geri çekilme zamanlamaları kullanılır. Göğüs destek tarafına hafifçe eğilir; omuz ve bilek küçük katkı yapar, karşı ön pati destek noktasına tutulur. Mevcut dirsek/pati IK zinciri ve kemik boyları korunur. Hareketli oyuncağın yerel temas noktası izlenir; tepki yalnız gerçek el kemiği hedefe yaklaştıktan sonra başlar. Buradaki mesafe el kemiği–IK hedefi ölçüsüdür, tüm deri yüzeyinin penetrasyon ölçümü değildir.

Yakın çekimde saptanan baş/tüy iç içe geçmesini azaltmak için yalnız yaylı oyuncak başlangıç mesafesi .30→.40 m, duruş kabul yarıçapı .12→.035 m oldu. Kedi otomatik yaklaştırılmaz veya ışınlanmaz; hazır olmayan yerde eylem düğmesi sunulmaz. Diğer oyuncakların akışı ve doğal dönüş sistemi değiştirilmedi.

## Kullanılan kaynaklar

Mevcut Polyperfect `DEF-*` iskeleti, `CatHome_Polyperfect` Animator, `ToyBatLeft` / `ToyBatRight` (yakın tek pati), `ToySniff`; `CatToyContactMotion` ve `CatFurnitureGaze`. Yeni rig veya klip oluşturulmadı; Blender kullanılmadı. Yeni `CatSpringToyMotion` yalnız bu oyuncakta etkin, her kare geri alınan kemik katkısıdır.

## Kontrol ve video

Nihai davranış kanıtı `QA/NATURAL_SPRING_2026-10-01/behavior-final.xml`; son video koşusu `final-video.xml`. Tek test iki ırkı (domestic-shorthair/Persian) kapsar: her birinde iki temas, yaklaşık 20° dönüş, 13.57° oyuncak tepkisi, bir tamamlanma, serbest bırakılan kontrol, 2 mm altı kök kayması ve ön bacak yerel konum kontrolü. En yakın el–hedef mesafesi yaklaşık 3.3 mm. İlk iki ürün denemesi ve tek mesafe düzeltmesi yapıldı; aradaki test kuyruğu/başlangıç yönü düzeltmeleri ile çekim açısı tekrarları ayrıca QA XML'lerinde korunur. Ara kuyruk/yön fixture FAIL'leri nihai kabul değildir.

Video: `QA/NATURAL_SPRING_2026-10-01/CatHome_Yayli_Oyuncak.mp4`; gerçek Unity Game View, 9 saniye, 1920×1080, 24 FPS, sessiz. Yakın yan kamera yalnız QA çekimine aittir; normal oyun kamerası değiştirilmedi. Videodaki seviye/mağaza durumu kopya QA verisidir.

Kalan sorun: Persian'ın başı ile tüyler arasında yakın çekimde hafif örtüşme kalıyor; bütün ırklar ve tüm deri yüzeyi için sıfır clipping kabulü yapılmadı.

Güncel başlangıç yedeği `baseline.json`; koruma sonucu `preservation-final.json`, tercihler ve editör durumu `editor-final.json`, son zaman `closure.json`. Gerçek oyuncu kaydı yerine kopya QA kaydı kullanıldı. APK, telefon kurulumu, commit, push veya yayın yapılmadı.
