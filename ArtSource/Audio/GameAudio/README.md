# Cat Home — oyun sesleri

14–15 Eylül 2026'da kullanıcının bütün oyun seslerini tamamlama isteğiyle üretilen yerel ses paketi. `generate_audio.py`, Python ve NumPy kullanarak `Assets/Resources/GameAudio` altındaki dosyaları üretir. Önceki onaylı açılış müziğini değiştirmez; o parçanın enstrüman fonksiyonlarını kullanır.

Beş yeni döngü: evde **80 sn**, dış mekânda **76,8 sn**, dinlenmede **87,27 sn**, koşuda **60 sn**, av oyununda **68,57 sn**. Müzikler Do majörde, sabit akortlu ve iki yarısında farklı cümlelerle düzenlendi. Başlangıçtaki 60 saniyelik Minik Kaşif açılış teması ayrıca korunur.

119 efekt dosyası: arayüz ve ödüller; kedi sesi/mırlama/yeme/içme; farklı yüzeylerde pati ve inişler; ahşap, ip, kumaş, kâğıt, toprak, seramik ve cam; oyuncaklar, tekerlek, yemlik; duş, fıskiye, sıcak ateş, televizyon ve dış mekân havası. Tekrarlanan fiziksel vuruşlar **ayrı sentezlenmiş varyasyon** kullanır. **15 Eylül revizyonu:** yeme/içme iki gerçek CC0 kedi kaydıyla, 12 pati varyasyonu perdesiz yumuşak temaslarla yenilendi. Miyav/mırlama önceki sentezdir. Kaynaklar ve hazırlama adımları [kedi bakım sesleri notunda](../CatCare20260915/README.md). Genel üretici bu 14 revizyonu manifest/hash ile korur; bakım sesleri özel hazırlayıcıyla yeniden üretilir.

Müzikler stereo Streaming/Vorbis; efektler mono, önceden açılan Vorbis. Her kaynak 48 kHz/16 bit WAV'dır. Dosyaların hashleri, süreleri ve tepe/RMS ölçümleri `Assets/Resources/GameAudio/manifest.json` içindedir. Ham dosya ve Unity'de açılmış ses kontrolleri ayrı değerlendirilir.

Runtime bağlantıları `Assets/Scripts/Audio/` altındadır. `GameAudio` en çok 12 efekt kaynağı ve tekrar aralıkları kullanır. `GameSoundscape` müzikleri ekran/oda/dinlenme durumuna göre geçirir. `CatFoley` ve `MiniGameFoley` animasyonları değiştirmeden pati/temas durumunu izler; bazı düşen eşyaların ve av oyununun kesin temas noktasına yalnız ses çağrıları eklendi. `CatVoice` yeme/içme/okşanma/dinlenme durumunu takip eder. Arayüzde yalnız geçerli düğme/toggle eylemi ses çıkarır.

Müzikler ve diğer sentezler üçüncü taraf şarkı/SoundFont veya ücretli üretim hizmeti içermez. İki yeni bakım kaydının kullanım dayanağı kaynak sahiplerinin CC0 lisansıdır; kayıtlar yerel sentez diye sunulmaz. Önceki kaynaktaki [kullanım açıklaması](../CatHomeMenu/README.md) yerel üretimlere uygulanır. Kaynak/lisans ayrımı [kedi bakım sesleri notunda](../CatCare20260915/README.md) tutulur.

QA kanıtları `Docs/QA/FULL_AUDIO_2026-09-14/` altında yereldir; Git takibine zorla eklenmez. Çalışma 14 Eylül'de başladı, İstanbul saatiyle 15 Eylül'e devam etti.
