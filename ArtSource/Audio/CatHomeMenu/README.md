# Cat Home — Minik Kaşif

14 Eylül 2026'da bu proje için Codex ile oluşturulan açılış müziği. Kaynak önceki low poly denemelerinin kullanıcı geri bildirimine göre geliştirilmiş hâlidir.

- 60 saniye / 40 ölçü / 6/8 / dörtlük 120 BPM / Do majör.
- Kısa geçiş notaları dışında melodi, eşlik ve bas aynı akorun seslerine bağlanır. Önceki Fa diyez geçişleri ve C/E altında yanlış seçilen Si bası giderildi.
- Enstrümanlarda frekans kaydırması ve vibrato kaldırıldı; melodik bileşenler eşit aralıklı akorda sabittir. Tahta ritim sesi perdesizdir.
- Son notaların ve oda yansımasının kuyrukları başa taşınarak 60 saniyelik döngü hazırlanır. Oyunda başlangıç/bitiş ses seviyesi geçişi ayrıca uygulanır.

`generate_menu_music.py` Python ve NumPy ile çalışır; düzenlenebilir notalar `CatHomeMenu.mid` ve `score.json` içindedir. Üretici doğrulama çıktısını proje içindeki `Docs/QA/MENU_MUSIC_2026-09-14` klasörüne yazar. Oyundaki kaynak `Assets/Resources/Music/CatHomeMenu.wav` dosyasıdır; üretici bu dosyayı otomatik değiştirmez.

Tüm tınılar matematiksel sentezle yerelde oluşturuldu. Üçüncü taraf şarkı, ses kaydı, SoundFont, ses örnekleme kütüphanesi veya müzik üretim hizmeti kullanılmadı. Gerçek enstrüman kaydı değildir. Bu müzik için ek üretim, indirme veya abonelik ücreti doğmadı; mevcut Codex kullanımı bunun dışındadır.

Kullanıcı ve OpenAI arasında çıktı hakları, hukukun izin verdiği ölçüde kullanıcıya bırakılır. Koşullara uygun ticari Android/iOS oyun kullanımına ilişkin dayanak: [OpenAI Terms of Use](https://openai.com/policies/row-terms-of-use/) ve [Service Terms](https://openai.com/policies/service-terms/), 14 Eylül 2026'da kontrol edildi. Bu not benzersizlik veya münhasır telif garantisi değildir. Kapsamlı dış müzik benzerliği taraması yapılmadı.
