# Cat Home — ek 60 dakika teslimi, 17 Eylül 2026

**Bu tur kaydedildi; özgün görevin tamamı bitmedi.** Teknik çalışma 18:59:17–19:54:07 Türkiye saatleri arasında kapandı. Belgeler ve kaynak bağlantıları 19:56:02'de doğrulandı; toplam **56,8 dakika**. Kesin 19:59:17 sınırı aşılmadı. Bu kayıt yeni çalışma turu başlatmaz.

Bu turdaki kalıcı değişiklik, ortak geometri verisini oda ve kedi hazırlığına almak oldu. Yaklaşık 28 MB geometri kataloğunun yüklemesi artık ortak ön yükleme ile başlıyor; normal oda ve ırk hazır olma koşulu bu veriyi de bekliyor. Ön yükleme yolunu kullanmayan bağımsız sahne ve erken çağrılarda önceki eşzamanlı yükleme korunuyor; geçici boş katalog kalıcı boş temas hedefi oluşturamıyor. İki üretim dosyası ve bir soğuk yükleme testi değişti. Önceki pati araması ve destek iyileştirmeleri korundu.

İlk altı yüzey ölçümünde küvetin ilk destek hesabı önceki turun 176,906 ms değerinden 13,785 ms'ye indi. Son sürümde aynı ilk örnek **11,533 ms**. Bu ölçümler farklı test akışlarından geliyor; genel hızlanma oranı değildir. Son soğuk geometri yükleme karesi **174,098 ms** ile 50 ms sınırını aşıyor. Bekleme hazırlık aşamasına taşındı, ortadan kalkmadı. Son 1.205 destek örneğinde ortalama **12,834 ms**, en ağır **170,434 ms**. Genel performans kabulü açık; telefon performansı ölçülmedi.

Asılı koltuk ve ortak destek için denenen yüzey yönü, gövde dengelemesi, pati hassaslaştırması, oturma noktası ve sıçrama yayı adayları kabulü geçmedi; tamamı bu turun başlangıç kaynaklarına geri alındı. Geçici 28 cm nokta değişikliği ve alçak giriş yayı çıkışı tamamlattı fakat deri kesişimi 7,256 mm oldu; 5 mm sınırını geçtiği için ürüne uygulanmadı. Model, sahne, prefab, kemik uzunluğu veya kabul toleransı değiştirilmedi. İlk üç denemenin eski derlemeyle çalıştığı saptandı; bunlar aday kanıtı sayılmadı. Son derleme ve kaynaklar ayrıca doğrulandı.

Son sürüm Play Mode kontrollerinde **4 geçti / 3 kaldı**. Küçük pati kataloglarının 50 ms sınırı, 10 ırkın gerçek değiştirilmesi ve mutfağa geçiş, 10 ırkta çiçek teması ve iki ırkta gerçek plak düğmesi akışı geçti. Soğuk büyük geometri yüklemesi, altı yüzeyin deri kabulü ve 39 rutinin güvenli çevrim kabulü kaldı. EditMode **12/12**. Manifest toplamı **16 geçti / 3 kaldı**; geçmiş adaylar bu sayıya eklenmez. İçerik denetçisi ve C# Console: **0 hata / 0 uyarı**. Çiçek: 10/10 ırk × 3 temas, en büyük örneklenen deri kesişimi 1,782 mm. Bu, bütün eşyaların on ırkta veya telefonda tarandığı anlamına gelmez.

| Yüzey | Tamamlama | Son altı-yüzey deri kesişimi |
|---|---:|---:|
| Küvet | 1/1 | 7.223 mm |
| Hamak | 1/1 | 14.622 mm |
| Asılı koltuk | 0/1 | 34.579 mm |
| Armut koltuk | 1/1 | 15.807 mm |
| Şezlong | 1/1 | 31.752 mm |
| Yer minderleri | 1/1 | 23.313 mm |

Sekiz odada **38/39 tamamlama**; asılı koltuk çıkışı hâlâ tamamlanmıyor. 38 tamamlama, 38 güvenli deri kabulü değildir. Kalan üç başlık: asılı koltukta güvenli tam çıkış; yukarıdaki altı yüzeyde 5 mm gerçek deri kabulü; ilk hazırlık ve ağır destek karelerinde performans. Özgün 2, 3, 4 ve 6. maddelerin önceki kabulü korunur; bu tur yeniden çalıştırılmadı. Kapsam tablosu [önceki raporda](INTERACTION_POLISH_FINAL60_2026-09-17.md).

2.141 başlangıç dosyasından **2.138'i aynı**, yalnız belirtilen üç C# dosyası değişti. 87 sahne, 320 prefab, 396 FBX, 145 WAV ve **üç gerçek kayıt aynı**. 16/16 tercih, editör sesi ve EditorSettings korundu. Üç temiz normal sahne ve tek kedi/kamera/dinleyici var; Play, QA, çekim ve derleme kapalı. Unity açık ve yanıtlıyor; bilgisayarı kapatma iptali korundu. Commit, push, video, APK, yayın veya teslim arşivi yok; QA Git dışında.

Kanıtlar: [son manifest](QA/INTERACTION_POLISH_MORE60_2026-09-17/current-native-manifest.json), [son Play Mode](QA/INTERACTION_POLISH_MORE60_2026-09-17/native-final-play.xml), [EditMode](QA/INTERACTION_POLISH_MORE60_2026-09-17/native-final-edit-scope.xml), [ölçümler](QA/INTERACTION_POLISH_MORE60_2026-09-17/final-metrics.json), [kayıt/dosya koruması](QA/INTERACTION_POLISH_MORE60_2026-09-17/preservation-final.json), [editör](QA/INTERACTION_POLISH_MORE60_2026-09-17/editor-final.json), [kaynak farkları](QA/INTERACTION_POLISH_MORE60_2026-09-17/runtime-diff.txt). Başarısız/eski derleme XML'leri silinmedi; run-exclusions.json nedenleri saklar. Hariç tutulmaları hataların düzeldiği anlamına gelmez.
