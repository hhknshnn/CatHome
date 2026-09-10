# Git QA çıktıları temizliği — 10 Eylül 2026

Kullanıcı, büyük Git gönderiminin nedenini incelememizi ve QA çıktılarını yerelde koruyarak Git takibinden ve ilgili commit'ten çıkarmamızı istedi.

- İlk yerel commit `83a6687836923b2f9ff6a6d76350fce87f81c4dd`; uzak `main` kontrolünde hâlâ `ca9483cbab023292027d3a4181430db55209f447` vardı. Bu projeye ait devam eden büyük gönderim durduruldu. Paylaşılmış geçmiş değiştirilmedi; yalnız gönderilmemiş son yerel commit temizlendi.
- `Docs/QA`, `Assets/QA`, `Assets/Screenshots`, son iki klasörün üst `.meta` dosyaları ve `Docs` altındaki teslim ZIP'leri Git takibinden çıkarıldı. Toplam **7.176 dosya / 5.170.597.210 bayt** yerelde korunur. Oyun sahneleri ve ürünlerin çıkarılan görsellere dış GUID bağı bulunmadığı kontrol edildi.
- `.gitignore` bu klasörleri ve Docs altındaki ZIP/RAR/7z teslim arşivlerini dışlar. Asıl oyun görselleri, modeller, sesler, kod, sahneler, proje ayarları ve Docs kökündeki Markdown notları sürüm kontrolünde kalır.
- Yalnız yeni bir silme commit'i eklemek büyük dosyaları gönderilecek önceki commit'te bırakacağından, gönderilmemiş son commit aynı mesaj ve yazarla düzenlendi. Önceki kimlik Git reflog'unda kurtarma için bulunur; yedek dal oluşturulmadı.
- Unity veya gerçek oyun kaydı değiştirilmedi. APK/arşiv üretimi, yeni push, force-push ve paylaşılan geçmiş temizliği yapılmadı. Sonraki commit/push işlemleri yine kullanıcı tarafından yapılır.

Galeri ve QA kanıtlarının bağlantıları bu bilgisayardaki yerel dosyalara gider. Yeni bir clone bu yerel kanıt dosyalarını içermez. Git'in yerel nesne deposu eski commit'i reflog üzerinden bir süre tutabilir; bu boyut, sonraki push'ın aktarım boyutu değildir.
