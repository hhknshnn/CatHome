# Açılış müziği — Minik Kaşif, 14 Eylül 2026

Kullanıcının low poly denemesini uzatma, detone hissi veren yerlerini düzeltme ve oyuna ekleme isteği tamamlandı. **60 saniyelik açılış döngüsü Unity'de ayrı deneme kaydıyla açık; müzikal sonuç kullanıcı incelemesinde.**

## Müzik

- Önceki 32 saniyelik v4, 40 ölçülük 6/8 Do majör parçaya dönüştürüldü. İkinci yarıda yeni melodik cevaplar ve eşlik değişimleri var; dosya yalnız tekrar edilerek uzatılmadı.
- Fa diyez içeren D7 geçişi D minöre, gergin yedili akorlar üçlü akorlara alındı. C/E altında yanlış seçilen Si bası düzeltildi; bas artık ilgili akorun seslerini kullanır. Uzun melodi notaları armoniye bağlı; kısa geçiş notaları korunur.
- Mallet, üflemeli ve tel tınılarında akort kaydırması/vibrato kaldırıldı, melodik üst sesler tam armonik oranlara getirildi. Tahta ritim perdesiz sentezlendi. Önceki neşeli, kısa vuruşlu low poly karakter korundu.
- Son notaların ve yansımanın kuyruğu döngünün başına sarılır. Oyundaki döngü dosyasında baş/son sessizlik yok; bağımsız dinleme kopyasında kısa giriş/çıkış yumuşatması var.
- 48 kHz, stereo, 16 bit WAV; 60 saniye. Tepe −2,00 dBFS, RMS −17,76 dBFS, taşan örnek 0. 33 ayrı enstrüman/notanın frekans ölçümünde en büyük hata yaklaşık 0,00033 cent. Bu sayısal ölçüm fiziksel dinleme değerlendirmesi değildir.

Oyundaki dosya: `Assets/Resources/Music/CatHomeMenu.wav`. SHA256: `63f264b0eea4fde5c3f29384b4969c3d9c073682e9e0fa706bf4240955e43434`.

[Dinleme kopyası](QA/MENU_MUSIC_2026-09-14/CatHome_Minik_Kasif_Dinleme.wav) · [Döngü WAV](QA/MENU_MUSIC_2026-09-14/CatHome_Minik_Kasif_Menu_Loop.wav) · [Açılış ekranı](QA/MENU_MUSIC_2026-09-14/title-music-ready.png)

Kaynak, düzenlenebilir nota/MIDI ve üretim/kullanım açıklaması `ArtSource/Audio/CatHomeMenu/` altında takip edilir. Tınılar yerel matematiksel sentezdir; üçüncü taraf kayıt, SoundFont veya müzik hizmeti kullanılmadı. Hak ve kullanım dayanakları [kaynak notunda](../ArtSource/Audio/CatHomeMenu/README.md); benzersizlik garantisi verilmez. Önceki v1–v4 demoları korunur.

## Oyun davranışı

`TitleScreen.Awake` tek `TitleMusicController` ekler. Kaynak yalnız açılış ekranını takip eder; ses 0,25 saniyede yumuşakça açılır/kapanır. Mevcut **Müzik** tercihi kullanılır; **Ses efektleri** tercihi müziği kapatmaz. Müzik kapatılınca veya uygulama odak kaybedip duraklayınca oynatma duraklar, yeniden açılınca aynı konumdan devam eder. **Devam et** ile açılış kapanırken müzik söner; menü tekrar açılınca tek kaynak bulunur.

Dosya Streaming/Vorbis 0,85 kaliteyle alınır; kaynak pitch 1, stereo, döngü açık. Bu adım açılış müziğidir; ev içi arka plan bestesi sonraki ayrı adımdır. Mevcut ev/mini oyun ses sistemi ve onaylı animasyonlar değiştirilmedi.

## Doğrulama

- Son native PlayMode sonucu **4/4 başarılı**: 60 saniyelik ithal klibin gerçek ses çıktısı/döngüsü; bağımsız müzik tercihi ve aynı konumdan devam; gerçek Devam düğmesi ve üç menü açılışında tek kaynak; duraklama/odak callback'leri.
- Esas sonuç [native-title-music-release.xml](QA/MENU_MUSIC_2026-09-14/native-title-music-release.xml), test işi `3eb7ddd2a1654bada9d2449a4ce1c674`, 20:21:36–20:21:45 UTC. Adında `initial` veya `final` geçen önceki XML'ler ara 3/4 sonuçlarıdır; son teslim sayılmaz. İlk başlatma denemesi test ortamı hazırlığında kaldı. Ses örneklemesi normal dinleyicinin yüklenmesini ve çıkış tamponunun dolmasını bekleyecek şekilde düzeltildi; sıfır olmayan ses çıktısı kabul koşulu korunur.
- Manuel denemede Müzik ve Ses tercihleri değiştirildi; gerçek Play durdurulmasıyla **16 tercih**, varlık bayrakları ve editörün önceki sessiz durumu otomatik geri geldi, QA kapandı. Sonra yeni ayrı kullanıcı denemesi açıldı. Kanıt: `manual-stop-verified.json`.
- Yeni kod derlendi; konsolda derleme hatası yok. Bir gerçek Unity ekran görüntüsü incelendi; video yok. Tam oyun/animasyon testi ve fiziksel telefon/ses dinleme testi yapılmadı.

## Kullanıcı denemesi ve kayıt güvenliği

Teslimde Play/QA açık, açılış ekranı görünür, müzik açık, pitch 1 / seviye 0,36 / döngü açık. Tek müzik denetleyicisi, tek oyun kedisi ve tek etkin ses dinleyicisi var; üç sahne temiz, çekim kapalı. Açılıştaki üç kedi mevcut arka plan görselinin parçasıdır. Kanıt `editor-ready.json`. Bu dosyadaki tek anlık sıfır çıkış örneğinden sonra yapılan iki saniyelik canlı ölçümde 135 örnekleme ve 0,2158374 tepe çıkışı görüldü (`live-output-check.json`); kaynak gerçek ses sinyali üretiyor. Bu, fiziksel hoparlör dinlemesi değildir.

Son ayrı QA kaydı: `Library/UiQaSession/20260914-202736`. Bulut eşitlemesi bu QA oturumunda kapalı. **Kullanıcı bitirmeden Play kapatılmaz.** Kullanıcı Play'i durdurunca veya **Tools > Cat Home > Müzik Denemesi > Bitir** seçince `TitleMusicReview` önceki tercihleri ve editör ses durumunu geri yükler, QA'yı kapatır. Yeniden deneme için aynı menüde **Başlat** bulunur. Unity odak dışındaysa uygulama odağına bağlı müzik duraklayabilir; denemek için Game penceresine geçilir.

Yeni başlangıç ve son karşılaştırmada 299 mevcut dosyadan yalnız `TitleScreen.cs` değişti; yeni dosyalar bu karşılaştırmanın dışındadır. 14 sahne, başlık prefabı, bu taramanın kapsadığı bir kedi FBX'i ve üç gerçek kayıt aynı. Bu tur yedi FBX'in tamamı yeniden ölçüldü denmez. Yeni başlangıç hashleri esas alınır:

- Ana/recovery: `4ed7ae846f6049cd35ce90447f4e5bf935ab41a86ac504fd0d2eef7799a597da`.
- CP2: `03d4fd1a6420ff0d6cd6213fe08ea57598038ec589ba7ca02475692036ea9a8d`.

Editörün test için geçici değiştirilen Play seçenekleri geri alındı; `ProjectSettings/EditorSettings.asset` farkı yok. QA çıktıları Git dışında kalır. APK, arşiv, commit, push veya yayın yapılmadı. Onaylı sıçrama/dönüş/eklem düzeltmeleri korunur; önceki ayrı salon tablo giriş bulgusu bu işin kapsamına alınmadı.
