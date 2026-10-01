# Cat Home — son 60 dakika, 17 Eylül 2026

**Görev bütünü tamamlanmadı.** Başlangıç 22:54:25, kapanış 23:52:30 Türkiye saati; 58.1 dakika. 23:54:25 kesin sınırı aşılmadı. Yeni çalışma turu veya bilgisayar kapatma başlatılmadı.

Kalan dört destek yüzeyi incelendi. Son rapor karşılaştırması, küvette daha önce gözden kaçırılmış bir bulguyu da açık listeye ekledi. Ortak aşağı pati hedefi kontrolü, mobilyanın geniş kutusu yerine aynı karedeki gerçek ağırlıklı pati derisini kullanacak şekilde düzeltildi. Diğer engellerde geniş kutu kontrolü, 2 mm çalışma eşiği, 5 mm son deri kabulü, kemik boyları ve eklem sınırları korunur. Bu değişiklik tek başına dört yüzeyi kapatmadı.

Daha geniş gövde/pati çözümü, geometri işlem optimizasyonu, hamak ekseni ve puf yüksekliği/çıkış denemeleri kabul testlerini geçmedi. Bunlar geri alındı. Son oyun içeriğinde model, eşya yerleşimi, prefablar, kaynak klipler ve önceki asılı koltuk düzeltmesi başlangıçla aynıdır. Yeni testler, kaynak duruş ile gerçek deri/pati desteğinin uyuşmadığı kareleri kayıt altına alır.

| Yüzey | Tamamlama | En ağır gerçek deri kesişimi | Pati destek ihlali örneği |
|---|---:|---:|---:|
| Küvet | 1/1 | 5.683 mm | 18 |
| Hamak | 1/1 | 15.658 mm | 24 |
| Asılı koltuk | 1/1 | 0.000 mm | 0 |
| Puf | 1/1 | 15.108 mm | 1 |
| Şezlong | 1/1 | 26.975 mm | 2 |
| Yer minderleri | 1/1 | 22.660 mm | 5 |

Deri sütunu, tam çevrim ölçümü ve zemin dahil bağımsız ikinci örneklemenin en büyüğüdür. Yalnız ilk CSV'deki daha küçük sayıya bakılarak geçti denmedi. Pati sütunu benzersiz kusur sayısı değil, kontrolün örneklendiği ihlal sayısıdır. **Açık kalanlar: Küvet; Hamak; Puf; Şezlong; Yer minderleri; Ağır destek hesabında 50 ms sınırı.**

Küvetin bağımsız ölçümündeki 5,683 mm kesişim ve 18 pati desteği ihlal örneği önceki 90 dakika turunun ham kanıtında da aynıdır. Önceki rapor yalnız ilk ölçümdeki 1,860 mm değerini esas almıştı; küveti başarılı sayan değerlendirme düzeltilmiştir. Karşılaştırma: [küvet rapor düzeltmesi](QA/INTERACTION_POLISH_SUPPORT60_2026-09-17/tub-report-correction.json).

Asılı koltuk son turda 10/10 ırk geçti. Son sürüme ait seçili Play Mode sonuçları 13 geçti/2 kaldı; EditMode 20 geçti/0 kaldı. Puf adayı geri alındıktan sonra altı yüzey ve puf tekil kontrolü tekrarlandı. Önceki adayla yapılan 39 rutin turu son sürüm kabulüne dahil edilmedi. Değişmeyen koltuk, soğuk yükleme ve güncel/eski temas yollarının sonuçları manifestte kendi kaynaklarıyla korunur. İçerik denetçisi ve son C# kontrolü 0 hata / 0 uyarı.

Destek çözümü 1240 örnek: ortalama 18.514 ms, en ağır 97.440 ms. Soğuk geometri yüklemesi 18.111 ms; önceki turdaki ikili geometri yükleme düzeltmesi korunur. Bu ölçüm fiziksel telefon performansı değildir.

2142 başlangıç dosyası: 2140 aynı,2 değişmiş. Üç gerçek kayıt ve 16/16 tercih aynı. Sahne/prefab/FBX/WAV ve 8,2 MB geometri verisi aynı. Üç temiz normal sahne, tek kedi/kamera/dinleyici; Play/QA/çekim/derleme kapalı. Unity ve bilgisayar açık. Commit/push/video/APK/yayın/teslim arşivi yok; QA Git dışında.

Esas kanıt: [manifest](QA/INTERACTION_POLISH_SUPPORT60_2026-09-17/current-native-manifest.json), [son yüzey tekrarı](QA/INTERACTION_POLISH_SUPPORT60_2026-09-17/native-release.xml), [diğer son kontroller](QA/INTERACTION_POLISH_SUPPORT60_2026-09-17/native-final.xml), [EditMode](QA/INTERACTION_POLISH_SUPPORT60_2026-09-17/edit-final.xml), [ölçümler](QA/INTERACTION_POLISH_SUPPORT60_2026-09-17/final-metrics.json), [koruma](QA/INTERACTION_POLISH_SUPPORT60_2026-09-17/preservation-final.json), [editör](QA/INTERACTION_POLISH_SUPPORT60_2026-09-17/editor-final.json). Adayların başarısız sonuçları silinmedi; kapsamları ve geri alma nedenleri run-exclusions.json içinde.
