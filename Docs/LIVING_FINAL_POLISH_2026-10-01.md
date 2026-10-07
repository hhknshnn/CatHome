# Salon final polish — 1 Ekim 2026

Başlangıç 16:54:00 UTC; kesin sınır 17:44:00 UTC. Kapanış zamanı ve son doğrulama `QA/LIVING_FINAL_POLISH_2026-10-01/closure.json` içindedir.

## Uygulama

Yalnız salon tırmalama direği, konuşma balonu yüzeyi ve uyku göstergesi değiştirildi. Mevcut üç runtime dosya değişik; bir görsel yardımcı, bir test dosyası ve ayrı PNG atlas eklendi. Ortak popup atlası, 180 konuşma metni, yan yatış/uyku pozu, natural turning, diğer oyuncaklar, yerleşim, HUD, kamera, ışık, ekonomi ve kayıt sistemi korunur. Başlama sırasında otomatik yaklaşma veya kök ışınlaması eklenmedi.

Tırmalama: her el için gerçek MeshCollider yüzeyine ışın, canlı rig omuz/kol/pati ölçüleri ve mevcut kaynak gövde/uzuv kabulü kullanılır. İki elin de erişebildiği duruş gerekir. Gerçek distal deri üçgen noktaları mevcut eklem çözümüne bağlanır. Pati kalınlığına göre yüzey normalinde küçük güvenlik payı hesaplanır; world-space sabit hedef veya ırka özel ofset yoktur. Hareket kaynak omuz/dirsek ile göğüs dönüşünü, kavisli dışa toparlanmayı ve tahtaya bakışı birleştirir. Sol-sağ-sol-sağ dört vuruş vardır; hafif ritim 0,78/0,91 sn, kısa tarama; enerjik ritim 0,59 sn ve son 0,82 sn, tam tarama. Mevcut 25 mm temas kabul eşiği değiştirilmedi.

Konuşma: Affinity'de yeni vektör tabanlı cream/pearl yüzey, ince navy/gold kenar, küçük cyan vurgu, hafif derinlik ve temiz kuyruk. Mevcut kısa ölçek/fade ve salınım korunur. Üç bağlam (tırmalama, mama, uyku), iki dil, üçer varyasyon kontrol edilir. Metin mantığı aynıdır.

Uyku: daha okunur ay, iki küçük yıldız ve z; yavaş yukarı hareket, düşük genlikli pulse ve zaman farkları. Giriş 0,55 sn, çıkış 0,35 sn fade; yan yatış ve gerçek uyanma akışı korunur. Azaltılmış hareket desteği sürer.

## Kaynaklar ve kanıt

Affinity kaynakları `ArtSource/Affinity/LivingFinalPolish/Living_Final_Visuals.af`, `.svg` ve `.png`. Unity ayrı `Assets/Resources/LivingRoom/FinalVisuals.png` dosyasını kullanır. Computer Use ile gerçek Affinity belge/kayıt/dışa aktarım ve Unity Game View gözden geçirildi. ImageGen veya Blender gerekmemiştir.

Gerçek Game View videosu `QA/LIVING_FINAL_POLISH_2026-10-01/CatHome_Salon_FinalPolish.mp4`: 14 saniye, 1920×1080, 24 FPS, sessiz. Tırmalama → üç konuşma → uyku göstergesi. Hazırlık yerleştirmeleri kesilir; sayaçlar ayrı QA kopyasının test değerleridir. Unity VideoPlayer son video oynatım kanıtı `video-playback.json` içindedir.

Nihai test seçimi, sonuçlar ve ölçümler `native-final-manifest.json` içindedir. Önceki XML'ler tanısaldır: erişim taraması, bilek yerine deri temasına geçiş, katı 5 mm deri ölçümü, fazla dar başlangıç açısı ve yanlış taşınan ReviewOnly filtresi. Başarısız denemeler son PASS diye sunulmaz. Kök kayması ve örneklenmiş deri noktaları ayrı raporlanır; 5 mm sınırını geçmek sıfır çakışma garantisi değildir. Tüm 10 ırk, tüm duruşlar ve fiziksel telefon/FPS kabulü yapılmamıştır.

Gerçek kayıtlar ve tercihler güncel başlangıçla karşılaştırılır; tarihsel kayıt geri yüklenmez. Koruma karşılaştırması `preservation-final.json`, son editör durumu `editor-final.json`. APK/commit/push/yayın yapılmadı. Süre sınırında kaydet, Play/QA kapat ve dur kuralı geçerlidir.

## Nihai sonuç ve açık noktalar

Nihai seçilen beş native kontrolün **4'ü PASS, 1'i FAIL**. Medium ve Large; gerçek düğmeyle iki ritim, sol/sağ ikişer temas, tamamlama ve kilit bırakma geçti. Small'da aynı işlevsel döngüler tamamlandı; ek sıkı deri testi **5,1296 mm** ölçtü ve 5 mm sınırından geçmedi. Bu açık küçük çakışma tamamen giderilmiş diye sunulmaz. Medium 4,9321 mm, Large 4,9222 mm; üçünde kök kayması 0. Her kedide 167 etkin kare, toplam 501 kare; her dört karede deri örneklenir, toplam 496.592 nokta sorgusu. Temas yaması–yüzey aralığı 25 mm'lik mevcut kabul içinde; sıfır mesafe iddiası yoktur.

Üç bağlam × iki dil × üç farklı konuşma, taşmama, gerçek uyku/uyanma ve fade çıkışı PASS. Son videoda 336 kare; Unity oynatımı 336 decoded / highest335 / end=true. Video güncel son uygulamayı içerir.

Hızlı regresyonda Domestic Shorthair ile fare ve tünel gerçek tam döngü PASS. Yaylı oyuncakta aynı kopya fixture uygun başlangıç duruşu bulamadı; bu yüzden genel regresyon PASS denmez. Yaylı oyuncak ve başlangıç arayıcısı dosyaları değişmedi; bu turun değişikliğinden kaynaklandığı gösterilmedi. Kapsam dışı düzeltme yapılmadı.

Güncel başlangıçtaki 7.928 okunabilen dosyanın 7.925'i aynı; yalnız 3 mevcut runtime C# değişti, 6 yeni dosya, eksik 0. Bir eski Eat klibi başlangıçta okunamadı. Dört gerçek kayıt, ayrıca başlangıçta bulunan eski CP2 dosyası ve 16 tercih aynı. İki font önbelleği ve EditorSettings yalnız bu turun hash doğrulanmış başlangıcına döndü. Başlangıçta zaten bulunan test sahnesi korunur; yalnız bu tur üretilen test sahneleri QA'ya alındı.

Son editör: Play/QA/derleme/profiler kapalı, captureFramerate0, üç temiz normal sahne, tek etkin ses dinleyicisi; Unity DX11 açık. Test sahnesi geçişlerinde dinleyici uyarıları vardı; son normal sahnede dinleyici doğrulandı. Kaydedildi ve duruldu. Yeni tur kendiliğinden başlamaz.
