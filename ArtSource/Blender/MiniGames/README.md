# Mini oyunların Blender kaynağı

## 7 Eylül ikinci geçiş: Boulevard

Runner'ın güncel üretimi `build_runner_boulevard.py`dır. Temel koleksiyonun ardından çalıştırılır; 15 sokak/cephe/çevre/ahşap parkur modeli ve yeniden tasarlanmış 10 engel olmak üzere 25 FBX üretir. Kaynak ölçüler `boulevard_metrics.json` içindedir. `-- --hazards-only` yalnız sekiz zemin engelini yeniler; tam ölçü manifesti için tam üretimi kullanın.

```
blender --background --factory-startup --python ArtSource/Blender/MiniGames/build_runner_boulevard.py
blender --background --factory-startup --python ArtSource/Blender/Currency/build_runner_coin.py
```

Kanonik coin aynı `PawCoin.fbx` / `PawCoin_Icon.png` yollarında kalır; ikinci komut gerçek metal kabartmalı modeli ve aynı modelden şeffaf 1536 px ikonu üretir. Coin yüzü XY'dedir; Unity görselinde yalnız Y=180 yönü kullanılır. Tam dönüş veya yatay disk yapılmaz. Eski halo/sparkle katmanları Runner'da kapalıdır.

`RunnerBoulevardBuilder`, taze Runner sahnesinin son sanat adımıdır. Kediye yakın 46–50 FOV kamera, taş döşeme, dört cephe/segment ve sahiplik kapıları korunmuş dokuz çevre varyasyonu kullanır. Eski mimari dekorları kapatır. Son modeller uygulandıktan sonra bütün coin/engel görsel sınırlarını sahneye kaydeder; modelleri değiştirdikten sonra sahneyi yeniden inşa etmek zorunludur.

Yeni QA klasörü `Docs/QA/RUNNER_BOULEVARD_2026-09-07`dır. `MiniGameVisualQa.Folder`, `FramesFolder` ve `FollowObstacles` yeni kayıtları eski kanıtlardan ayırır. `encode_motion.py -- Library/RunnerBoulevardMotion Docs/QA/RUNNER_BOULEVARD_2026-09-07/motion` yalnız gerçek 24 fps kareleri kodlar.

## İlk mini oyun geçişi

`build_minigame_collection.py`, mevcut `premium_kit.py` ile Y-up/metre ölçülerinde 15 FBX ve iki 2048 px yüzey dokusu üretir. Blender yalnız arka planda çalıştırılır:

```
blender --background --factory-startup --python ArtSource/Blender/MiniGames/build_minigame_collection.py
```

- Sekiz zemin engeli: gerçek dokuma sepet/yumak, sisal direk, seramik mama kabı, robot süpürge, destekli yatak, ödül paketleri, taşıma çantası ve dikişli minder.
- İki üst geçiş: alt yüzeyleri .50 m olan kumaş kemer/tente. Kedinin eğilme pozu bu gerçek boşluğa göre ölçülür.
- Dikişli oyuncak fare; dört ayak ayrı geometri, vücut ve kuyruk tek görsel.
- Üç pencere/kapı/saçak/balkon siluetiyle pet kasabası; var olan dokuz çevre varyasyonu ve Garden kilidi korunur.
- Parke, minder, pencere, perde ve bitkili Cat Catch oyun alanı.

`collection_metrics.json` üretim ölçülerini kaydeder. `MiniGameArtBuilder` malzemeleri oyunlara özel `Assets/Art/MiniGames/Materials` kopyalarına bağlar; ev mobilyasının materyalini yeniden boyamaz. Geçiş/engel kökleri sabittir; çarpışma yüksekliği inşa edilen görselden alınır.

Blender tamamlandıktan sonra Unity'de `MiniGameAnimationBuilder.Build()`, `CatRunnerContentBuilder.BuildSilently()` ve `CatCatchContentBuilder.BuildSilently()` kullanılır. Animasyonlar mevcut kedi iskeletinden üretilir; global model squash kullanılmaz. Her ırkın poz/zemin düzeltmesi ayrı kaydedilir.

Menü fotoğrafları yalnız izole Play oturumunda `MiniGamePreviewBuilder.CaptureLive` ile gerçek kameralardan alınır. Canlı sahne hazır olmadan editörde anlık kamera çekimi yapılmaz. `MiniGameVisualQa` bu adımı ve ekran kontrollerini otomatikleştirir.

`encode_motion.py`, gerçek Unity karelerini 24 fps MP4'e dönüştürür. Hızlandırma, yavaşlatma veya kare üretme yoktur. Test oturumu sonunda kayıt kopyası kapatılır ve GameScene + CatHome_UI + LivingRoom_Level01 düzeni geri açılır.
