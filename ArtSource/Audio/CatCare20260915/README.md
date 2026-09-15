# Kedi bakım sesleri — 15 Eylül 2026

Oyundaki `Eat_1.wav` ve `Drink_1.wav` artık gerçek kedi kayıtlarından hazırlanır. Önceki kum/damla sentezi bu iki dosyada emeklidir. Sesler mono 48 kHz / 16 bit olarak temizlendi; özgün ritim ve perde korundu. Mama 14,4 sn, içme 6,1 sn. Ortam uğultusu/hışırtısı hafif azaltıldı; yüksek darbeler yumuşatıldı ve döngü birleşimi sessiz uçlara alındı.

| Oyun sesi | Kaynak sahibi ve kayıt | Lisans | Kullanılan bölüm |
|---|---|---|---|
| Mama | [indieground — cat eating.mp3](https://freesound.org/people/indieground/sounds/238297/) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 10,2–24,5 sn |
| Su | [16GPanskaZlochova_Eliska — 4_Cat, drinking milk.wav](https://freesound.org/people/16GPanskaZlochova_Eliska/sounds/496277/) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 18,7–24,7 sn |

Kaynak sahipleri kayıtları CC0 ile sunar; ticari oyunda işleyip kullanmaya izin verir. Orijinal sayfanın halka açık yüksek kaliteli MP3 ön izlemesi kullanıldı; yeni hesap veya ücretli servis yok. İndirilen dosyaların hashleri, kaynak ve lisans bağlantıları `*-source.json`, kontrol edilen sayfalar `*-source.html` içindedir. `drink.mp3` diğer incelenen adaydır; oyuna giren kayıt `drink_alt.mp3`tir. İçme kaydının adı süt içmeyi belirtir; oyunda yalnız dilin sıvıyla temas sesi kullanılır.

Arşivlenen HTML sayfalarındaki, ses kaydı ve lisansıyla ilgisiz üçüncü taraf Mapbox erişim anahtarları `[REDACTED_MAPBOX_TOKEN]` ile değiştirilmiştir. `fetch_sources.py` yeni indirmelerde de bu temizliği uygular; kaynak, lisans, ön izleme bağlantıları ve ses dosyalarının hashleri korunur.

Ahşap/fayans/çim/kumaş için toplam 12 pati varyasyonu `prepare_audio.py paws` ile yerel üretildi. Eski sinüsle üretilen davul gövdesi yerine kısa, yumuşak ve perdesiz temas/hışırtı kullanılır. Yürüme ve koşma zamanlamasını mevcut gerçek pati gözlemcileri yönetir; animasyon, model ve kök hareketi değiştirilmedi.

`prepare_audio.py analyze` kaynakları ölçer; `care` iki bakım döngüsünü, `paws` pati varyasyonlarını üretir. Kayıtları açmak için bilgisayarda zaten kurulu FFmpeg kullanılır; diğer işlem NumPy ile yereldir. `GameAudio/generate_audio.py` güncel manifestteki bu 14 revizyonu korur.

`fill_title_tails`, önceki açılışın 11,55–12,33 ve 41,55–42,33 saniyelerine düşük seviyeli Do notası kuyruğu ekler. Do→Fa geçişindeki ortak ses kullanılır; melodi, tempo, toplam 60 sn süre ve döngü sınırı korunur. Asıl menü üreticisi de aynı işlemi uygular. `title` komutu mevcut kabul edilmiş mastera bir kez uygulamak içindir; başlangıç masterı yerel QA altında tutulur.

Bu revizyonun ölçümleri `Docs/QA/AUDIO_REFINEMENT_2026-09-15/` altındadır. Önceki raporlardaki “hiç üçüncü taraf kayıt kullanılmadı” ifadesi yalnız önceki sürümü anlatır; yeni iki bakım sesinin kaynağı yukarıdaki CC0 kayıtlardır.
