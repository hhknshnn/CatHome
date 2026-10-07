# Tırmalama düğmesi ve karşılama tasarımı — 2 Ekim 2026

Kullanıcı önceki tırmalama hareketini kabul etti. Bu yeni tur yalnız kayıp eylem düğmesi, konuşma balonları, ana açılış ve welcome-back görünümü içindir. Yaklaşık başlangıç 07:59 UTC; kesin kapanış `QA/ACTION_UI_REFRESH_2026-10-02/closure.json` içindedir. Önceki 30 dakika yetkisi bu turun süresi değildir; bu tur yaklaşık 65 dakika sürdü, test akışındaki tekrarlar süreyi uzattı.

## Değişiklik

- Salon direğinde mevcut fiziksel noktadan en yakın geçerli tahta yönü bulunur. Dönüş yolu kontrol edilir; gerçek temas planı doğrulanınca sağ altta kısa **Tırmala / Scratch** düğmesi çıkar. Tıklama doğal dönüşle mevcut çift pati hareketine geçer. Kök yaklaşması/ışınlama eklenmedi.
- Direğin kaba gövde zarfının yanlış ret verdiği durumlarda mevcut ağırlıklı deri verisi ve gerçek direk mesh'i ile sınır daraltılır. Diğer engeller ve controller kontrolü korunur. Dönüşte yeni engel veya deri çakışması olursa eylem iptal edilir. 2,9 mm deri sınırı gevşetilmedi. Plan yönü en fazla 14° tahta ekseninden sapar; en fazla 90° güvenli başlangıç dönüşü aranır.
- Kabul edilmiş tırmalama döngüsü/rig/eklem ve pati temas hesabı korunur; başlangıç yönü için aynı çözücünün öngörülen dönüş sorgusu eklenir. Kamera, direk, oda yerleşimi, ışık ve HUD yerleşimi aynı.
- Ana menü ve welcome-back: krem, petrol yeşili, nane ve mercan paleti; daha temiz kartlar, okunaklı yazı ve düğme hiyerarşisi. Konuşma balonu: Affinity kaynaklı yeni yüzey, ince kenar, küçük pati rozeti ve sade tipografi. Konuşma içerikleri/timing ve oyun ilerlemesi aynı.
- Affinity'de düzenlenip dışa aktarılan `ArtSource/UI/WelcomeRefresh_20261002/Conversation.af` ve Blender'da hazırlanmış `PawEmblem.blend` proje dış kaynak kökünde saklandı. Unity'de iki PNG kullanılır. Computer Use ile uygulamalar ve gerçek Game View incelendi.

## Son seçili kontroller

Esas sonuçlar aynı QA dizinindeki `PlayMode-final-action-13.xml`, `PlayMode-final-regression-14.xml`, `EditMode-final-15.xml`: **8 PlayMode + 1 EditMode, 9/9 PASS**.

- SMALL Persian, MEDIUM Domestic Shorthair, LARGE Maine Coon: görünür düğmeye gerçek UI raycast/tıklaması, açılı duruştan doğal dönüş, iki sol/iki sağ vuruş ve tamamlanma. Üçünde eylem kök kayması 0.
- MEDIUM gerçek MobileJoystick pointer girişi ile yaklaşık 18,47 cm yaklaşma, düğmenin kendiliğinden görünmesi ve tıklamayla tamamlanma PASS. Eylem başlangıcı/sonu konum farkı 0,436 mm; sıfır diye raporlanmaz. Bu tur bütün ırkların joystick yaklaşmasını veya tüm açılardan erişimi kanıtlamaz.
- Uyku + yeni balon + ay/Zzz ayrımı, uyanma; fare ve tünel; balon hiyerarşi onarımı PASS.
- 18 saniyelik gerçek Game View kaydı ve tam oynatımı PASS: 1920×1080, 24 FPS, 432 kare, son kare 431, ended=true. `CatHome_Action_UI_Refresh.mp4`: açılış 0–4 s, welcome-back 4–8 s, dönüş 8–9 s, balon/düğme 9–11 s, tırmalama 11–18 s. Ses yok. WindowsMediaFoundation renk primaries metadata uyarısı verdi; karelerin tamamı çözüldü, fiziksel cihazda renk karşılaştırması yapılmadı.
- Video ayrı QA kayıt kopyasında çekildi. Seviye 30/10 oda gibi sayaçlar oyuncunun gerçek ilerlemesi değildir; dönüş özeti gösterim için örneklenmiştir. Kareler Unity'den alınmıştır, üretilmiş oyun görüntüsü değildir.

Ara başarısız/iptal edilmiş native sonuçlar korunur; son PASS olarak yeniden etiketlenmez. İlk birleşik üç-boyut fixture'ı tekrar sahne yükleme/ırk aramasında uzadı; native runner iptal edilip bağımsız boyut kontrollerine ayrıldı. Son seçili sonuçlar yukarıdaki XML dosyalarıdır. Tüm telefonlar/FPS/genel regresyon veya yeni görünüm kullanıcı onayı iddiası yok.

## Koruma ve kapanış

`preservation-final.json`: 8240 okunabilen başlangıç dosyasından 8230 aynı; 8 mevcut C# kapsam içi değişik ve Affinity'nin iki eski `~lock~` dosyası farklı. Eksik dosya yok. Yeni runtime yardımcı, test, iki PNG ve metaları eklendi. Bir eski Eat klibi başlangıçta okunamadı; eksiksiz tüm-varlık hash iddiası yok. Dört gerçek kayıt dosyası ve mevcut CP2 yedeği byte aynı. 16 tercih başlangıçla aynı; testlerin eklediği ırk tercihi özgün yokluk durumuna döndü. İki font önbelleği, CurrencyHud prefabının canlı sayaç önizlemesi ve EditorSettings yalnız bu turun hashli başlangıç baytlarına döndü.

Baseline yardımcısındaki kopyalanmış eski zaman damgası fark edilip bu turdaki baseline klasörünün gerçek 08:05:44 UTC oluşturma zamanına düzeltildi; intake yaklaşık 07:59 UTC'dir, bu fark gizlenmez.

Üç normal sahne temiz; tek etkin ses dinleyicisi. Unity açık, Play/QA/derleme/build/profiler kapalı. Test geçişlerinde ses dinleyicisi uyarıları oluştu; normal kapanışta tek dinleyici doğrulandı. APK, commit, push, yayın yok. Kaydedildi ve duruldu; yeni iş kendiliğinden başlamaz.
