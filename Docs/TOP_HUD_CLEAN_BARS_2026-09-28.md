# Top HUD — bar alt çizgileri ve ikon dengesi

28 Eylül 2026, başlangıç 20:44:06 UTC. Kullanıcının bu turdaki iki isteği uygulandı; başka alan açılmadı.

Profil/XP, Tokluk, Su ve Enerji panellerinde progress barın altında görünen bağımsız beyaz yatay şerit, Blender kaynağındaki `LowerRim` nesnesiydi. Dört panel için bu nesne kaldırılarak Cycles renderları yenilendi. Gerçek progress track/fill korunur. Dış altın panel kenarı ve üst yansıma aynı. Currency kaynağındaki çizgiye dokunulmadı. Kaynak `ArtSource/Blender/TopHudCleanBars/build-panels.py`, düzenlenebilir sahne `panels.blend`.

`StorybookHudDetails.cs` yalnız üç ihtiyaç ikonunu 82×84 yerine 72×74 kutuya yerleştirir. 96×96 medalyon, paneller ve genel layout aynı. Kaynak alfa kütlesi ve gerçek kullanım boyutu kontrol edilerek mama Y−0,5, su Y+1, hilal X+2,5 optik düzeltmesi verildi. İkon artwork'ü, renkler, metinler, binding ve işlevler değişmedi.

## Doğrulama

- Gerçek Unity Game View 1920×1080 Edit Mode önizlemesi: `QA/TOP_HUD_CLEAN_BARS_2026-09-28/before.png` ve `after.png`. Play açılmadı; canlı oynanış/telefon testi iddiası yok.
- `before-after.png` gerçek ekranların üst HUD kırpımı; `difference.png` tam kare farkı.
- 20 ölçülen RectTransform/metin kaydından yalnız üç ihtiyaç `V2Icon` boyutu/konumu farklı. Diğer 17 kayıt aynı, metin taşması 0.
- Dört PNG'nin çözünürlüğü ve alfa sınırları byte aynı. Sprite metaları değişmedi; dış panel şekli/boyutu korundu.
- Coin/diamond/plus/menu ve oda/alt HUD bölgelerinde yalnız en fazla 3/255 render yuvarlama farkı; kaynak dosyaları aynı.
- Unity derleme/Console sorgusu hata ve uyarı 0.

Güncel başlangıçtaki 9.295 okunabilen dosyadan yalnız 4 panel PNG + 1 sunum C# farklı; 9.290 aynı, eksik dosya/son okuma hatası 0. Başlangıçta bir eski animasyon dosyası okunamadı; onun için hash iddiası yok. Gerçek kayıtlar, 33 tercih, sahneler, fontlar, ekonomi ve oynanış kaynakları aynı. Üç normal sahne temiz, Play/derleme kapalı, Unity açık. APK, commit, push veya yayın yok. Önce/sonra görselleri kaydedildikten sonra duruldu.
