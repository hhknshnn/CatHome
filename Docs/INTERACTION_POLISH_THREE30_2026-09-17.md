# Cat Home — üç maddelik 90 dakika turu, 17 Eylül 2026

**Kaydedildi; görev bütünü tamamlanmadı.** Başlangıç 21:21:44, kapanış 22:49:43 Türkiye saati; toplam **88.0 dakika**. Kesin 22:51:44 sınırı aşılmadı. Kapsam asılı koltuk çıkışı, altı yüzeyin çarpışması ve performanstı. Bu kapanış yeni bir çalışma turu başlatmaz.

Asılı koltuğun destek noktası açıklığa taşındı (yerel Z −0,18 → −0,53 m). Kaynak sıçrama klibi, model ve mobilyanın oda içindeki yeri aynı. Girişteki gereksiz yükseliş azaltıldı; aşağı çıkıştaki fazladan 15 cm yükseliş kaldırıldı ve yatay çıkış eğrisi açıklığı daha erken geçiyor. Prefab ve iki üretici aynı yeni Z değerini kullanıyor. Son sürümde **10/10 ırk**, ayrı tam çevrim testleriyle ölçüldü. Bu yalnız çıkış sayacı değildir: gerçek deri, kontrol iadesi ve mevcut eklem/kök koşulları da testte kalır. Yeni görünüm için kullanıcı görsel onayı alınmış değildir.

Ortak destek hesabı, eğimli yüzeyin özgün üçgen yönünü ve patinin yalnız en alçak köşesi yerine bütün temas alanını kullanıyor. Destek ışınının yüzeyin içinde başlaması düzeltildi. Mobilyaya ait geniş kutunun reddi, aynı karede gerçek pati derisiyle hassaslaştırılıyor; diğer sahne engelleri ve son güncel fizik kontrolü korunuyor. Aynı poz değişmeden kaldığında aynı hesap ve deri örneklemesi tekrarlanmıyor. Kedi ölçeği, kemik uzunlukları, eklem sınırları ve 5 mm kabul toleransı değiştirilmedi.

| Yüzey | Tamamlama | Son gerçek deri kesişimi |
|---|---:|---:|
| Küvet | 1/1 | 1.860 mm |
| Hamak | 1/1 | 15.658 mm |
| Asılı koltuk | 1/1 | 0.000 mm |
| Puf | 1/1 | 15.108 mm |
| Şezlong | 1/1 | 25.839 mm |
| Yer minderleri | 1/1 | 22.569 mm |

Bu tabloda sınır altındaki bir ölçüm, bütün yüzey testinin geçtiği anlamına gelmez. **Açık kalanlar: 5 mm deri kabulü: Hamak, Puf, Şezlong, Yer minderleri; Ağır destek karelerinde genel performans.** Son altı-yüzey testi bu eksikleri görünür tutar; başarısız sonuçlar silinmedi.

Geometri kataloğu 30 modelin aynı verisini koruyan 8.237.228 baytlık ikili dosyaya taşındı; ana katalog yaklaşık 28 MB yerine 3 KB. Bütün koordinat, üçgen ve arama ağacı sayıları ikili dönüşümde birebir karşılaştırıldı. Ana iş parçacığı yalnız kaynak yüklemeyi yönetiyor; sayısal çözümleme arka planda çalışıyor. Oda/ırk hazır olma koşulu bu işlemi bekliyor; bağımsız erken çağrılarda eşzamanlı güvenli yol sürüyor. Üretici de aynı dosya biçimini üretir.

Soğuk geometri yüklemesindeki en uzun kare **174,098 → 24.984 ms**. Son altı-yüzey destek ölçümü **1241 örnek, ortalama 16.509 ms, en ağır 69.144 ms** (önceki tur: 12,834 / 170,434 ms). Soğuk yükleme başarısı, bütün oyun/telefon performansının geçtiği iddiası değildir; fiziksel telefon ölçülmedi.

Son Play Mode: **16 geçti / 2 kaldı**. EditMode: **20 geçti / 0 kaldı**. Sekiz odada **39/39 rutin tamamlandı**; bu 39 rutinin hepsinin deri çarpışmasının kapandığı anlamına gelmez. On ırk çiçek teması, güncel pati teması ve eski temastan yanlış puan verilmemesi son turda kontrol edildi. İçerik denetçisi ve C# derlemesi 0 hata / 0 uyarı. Son destek durdurma korumasından sonra etkilenen 12 kontrol tekrarlandı. Değişmeyen yükleme, çiçek ve güncel pati yollarının 6 sonucu önceki son turdan korunur; manifest her sonucu kendi dosyasına bağlar. Bu hedefli tekrarın ardından kaynak değiştirilmedi.

2141 başlangıç dosyasından 2130 aynı, 11 değişmiş. Değişiklikler kapsam içindeki C# dosyaları, koltuk prefabı ve geometri kataloğu; yeni ikili veri ve okuyucu eklendi. Sahne, FBX ve WAV dosyaları ile **üç gerçek kayıt aynı**. 16/16 tercih ve editör sesi korundu. Üç temiz normal sahne, tek kedi/kamera/dinleyici; Play, QA, çekim ve derleme kapalı. Unity ve bilgisayar açık. Commit, push, video, APK, yayın veya teslim arşivi oluşturulmadı; QA Git dışında.

Kanıtlar: [son manifest](QA/INTERACTION_POLISH_THREE30_2026-09-17/current-native-manifest.json), [Play Mode](QA/INTERACTION_POLISH_THREE30_2026-09-17/native-final.xml), [EditMode](QA/INTERACTION_POLISH_THREE30_2026-09-17/edit-final.xml), [ölçümler](QA/INTERACTION_POLISH_THREE30_2026-09-17/final-metrics.json), [kayıt/dosya denetimi](QA/INTERACTION_POLISH_THREE30_2026-09-17/preservation-final.json), [editör durumu](QA/INTERACTION_POLISH_THREE30_2026-09-17/editor-final.json), [kaynak farkları](QA/INTERACTION_POLISH_THREE30_2026-09-17/runtime-diff.txt). Ara adayların son sürümden ayrılma nedenleri run-exclusions.json içinde; geçmiş sonuçlar korunur.
