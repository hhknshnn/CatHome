# Sıçrama ve pati etkileşimleri — inceleme ve plan

13 Eylül 2026, Türkiye saati. Kullanıcının 15 dakikalık inceleme hedefi kapsamında; düzeltme uygulanmadı. Sekiz oda tarandı. İnceleme, mevcut siyah Oriental Shorthair ve ayrı QA kayıt kopyasıyla yapıldı.

## Doğrulanan bulgular

**Çıkışta havada dönme ortak sistemden geliyor.** `CatJumpMotion.Play`, kalkış yönünden eşyanın üzerinde kullanılacak yöne uçuş sırasında döndürüyor. Bugünkü 39 gerçek rutin kaydında, 38 yükselen sıçramanın 33'ünde yatay kalça–omuz yönü uçuşta 30° üstü değişti. Bazı örnekler yaklaşık 179°: bahçe saksısı/şezlongu, balkon askılı koltuğu/saksısı, üst kat divanı. Bu ölçüm gerçek animasyon sonrası gövde yönüdür; yalnız kök dönüşü değildir.

**Uçuyor hissinin belirgin nedeni çıkışın yatay zamanlaması.** Yükselen sıçramalarda yatay yol `SmoothStep(0,1,t*t)` ile ilerliyor. Uçuş süresinin tam yarısında yatay mesafenin yalnız %15,625'i alınır. Kedi önce yukarı çıkıp sonra öne hızlanıyor. Ayrıca görsel merkezleme uçuşta değişiyor ve klibin havadaki bölümü yeni uçuş süresine yayılıyor. Özgün klip dosyası korunmuş olsa da bu ek konum/yön işlemleri özgün görünümü korumaya yetmemiş.

**İniş kontrolü hâlâ başarılı.** Sekiz odadaki 39 rutin bugünkü iniş/kontrol iadesi testini geçti. Bu test havadaki yön sabitliğini veya yatay hızın doğallığını kontrol etmiyor. Önceki başarı sonuçları bu iki konuda kabul kanıtı sayılamaz.

**Vuruş ve düşüş yönü birbirine bağlı değil.** `KnockOffActivity.WorldFallDirection`, seçilen yaklaşma tarafından bağımsız, eşyada kayıtlı yönü kullanıyor. Canlı vuruş evresinde kökten eşyaya uzanma yönü ile düşüş yönü arasındaki açı:

- Üst kat kitap yığını: **142,5°**; kediye doğru/ters yönde düşme doğrulandı. Prefabdaki ilk duruşta yönler tam ters.
- Yatak odası komodin bardağı: **73,4°**; yana belirgin sapma var, tam ters düşme değil.
- Balkon sehpa kupası: bu başlangıçta **3°**; ters düşme tekrarlanmadı. Başka yaklaşma tarafı seçilince aynı sabit yön sorunu oluşabilir.

Bu açı ölçümü gerçek pati hızının ölçümü değildir; kedinin uzandığı taraf ile nesnenin kayıtlı hareketini karşılaştırır. Düzeltme gerçek temas ve vuruş yönünü de ölçmelidir. Mutfak sepeti/yemek masası ve salon sehpası da önceden belirlenmiş kenar/iniş hedeflerine gider; yön uyumu ayrıca denetlenmelidir.

**Düzeltme aşamasındaki yöntem kontrolü (13 Eylül): aşağıdaki probun negatif sonucu geçersiz.** Kapalı SphereCollider ile `Physics.ComputePenetration`, bilerek iç içe konmuş kontrol cisimlerinde de false dönüyordu. Bu nedenle ilk taramanın “0 kesişme” ölçümü açıklık kanıtı değildir. Etkin, tetikleyici prob ve pozitif/negatif kontrol ile sekiz oda yeniden taranıyor; sonuçlar `JUMP_CONTACT_FIX_2026-09-13.md` içinde. Sıçrama/yön ölçümleri bu probu kullanmadığından geçerliliğini korur.

**İlk kısa tarama kaydı (yukarıdaki nedenle negatif kanıt sayılmaz):** Sekiz odada 26 pati, eşeleme, itme ve benzeri temas rutini çalıştırıldı; hepsi bir kez tamamlandı. Kalça/göğüs çevresindeki küçük fiziksel problar, bu başlangıç noktalarında 5 cm üstü penetrasyon bildirmedi. Bu, kullanıcının gözlemini geçersiz kılmaz: tarama tüm gövde yüzeyini, tüm kol hareketini, bütün yaklaşma yönlerini veya bütün ırkları kapsamıyor.

Kodda somut açıklar var: pek çok eylemde CharacterController kapanıyor; gövde doğrudan taşınıyor. Ortak temas araması varsayılan olarak yalnız 0,27 m kök çevresini doğruluyor. `CatToyContactMotion` hedefe erişimi çözüyor, kolun aradaki eşya/gövde içinden geçmesini engellemiyor. Mutfak servis arabası ek olarak gövdeyi 7 cm ileri ve 15° öne taşıyor; araba hareketi gerçek pati temasına bağlı değil. Bunlar düzeltme öncesi farklı giriş tarafları ve gerçek model yüzeyiyle araştırılacak öncelikli noktalar.

## Oda kapsamı

| Oda | Çıkış sayısı / 30° üstü dönüş | Pati ve temas incelemesi |
|---|---:|---|
| Salon | 3 / 3 | Sehpa oyuncağı; sabit kenar ve iniş yolu |
| Banyo | 4 / 2 | Paspas, kum, kâğıt; gerçek kol/yüzey açıklığı korunmalı |
| Mutfak | 6 / 5 | Servis arabası, meyve sepeti, yemek masası, paspas |
| Yatak odası | 5 / 5 | Komodin bardağında 73,4° yön sapması; yumak, dolap, paspas |
| Bahçe | 6 / 5 | Top, saksı, ağaç, papatya minderi |
| Balkon | 4 / 4 | Yemlik, kupa, korkuluk çiçekleri, saksı; kupa bu girişte yönle uyumlu |
| Avlu | 5 / 4 | Saksı, şemsiye, halı; aynı ortak gövde/kol güvenliği açığı |
| Üst kat | 5 / 5 | Kitapta 142,5° ters yön; plak ve paspas |

Bu çalışma tüm oda türlerini kapsar; 17 CAT ürün varyantının veya on ırkın tam temas matrisi değildir. Kamera ve yaklaşma çeşitleri sınırlıdır. Salon tablosunun önceki ayrı giriş bulgusu bu turda değiştirilmedi.

## Önerilen düzeltme sırası

1. **Çıkış sıçraması — 60–90 dakika.** Doğru kalkış noktası ve yönünü zeminde hazırla. Havada varış yönüne zorunlu dönüşü kaldır; gerekiyorsa tam basıştan sonra destekli küçük adımlarla yön al. Yatay hareketi havada hızlanıp süzülmeyecek biçimde dikey uçuş ve özgün kliple eşleştir. Uçuş içindeki görsel merkezleme etkisini ölç. Mevcut iniş/toparlanma davranışını ve özgün kaynak klibi koru. Önce alçak divan, yüksek havlu rafı ve saksı ile doğrula.
2. **Gövde ve kol açıklığı — 60–90 dakika.** Sekiz odada temas duruşu, yaklaşma, vuruş ve geri çekilmeyi birlikte kontrol et. Gerçek göğüs/baş/ön kol ve eşya yüzeylerini esas al; yalnız kök noktası yeterli değil. Özellikle servis arabası, kitap, kupa, komodin, yemek masası ve korkuluk için farklı giriş taraflarını dene. Güvenli erişim yoksa yaklaşma tarafını/temas noktasını düzelt; eşyanın içinden geçirerek erişim üretme.
3. **Vuruş ve nesne yönü — 30–45 dakika.** Önce üst kat kitabı ve komodin bardağı. Temas anındaki pati hareketi, yüzey normali ve açık kenar birlikte nesnenin hareketini belirlesin. Kupa, salon oyuncağı, sepet/meyve ve masa parçalarına aynı kuralı uygula. Düşüşün duvara, kediye veya başka eşyaya ters yönde gitmesini engelle.
4. **Son doğrulama — 45–60 dakika.** Sekiz odada ilgili rutinleri tekrar çalıştır. Sıçramada 15/30/60 fps, kritik temaslarda on ırk ve farklı yaklaşma tarafları; gövde/kol kesişmesi, vuruş yönü, açık çıkış, iptal ve kontrol iadesi. Yalnız tamamlanmayı değil yeni görsel kabul koşullarını ölç. Gerçek PNG kanıtları ve kısa sonuç raporu.

Toplam tahmin **3 saat 15 dakika – 4 saat 45 dakika**. Eşya yüzeylerinde ek geometri sorunu çıkarsa süre güncellenir. Kullanıcı onayı gelmeden uygulamaya geçilmez.

## Kanıt ve sınırlar

Yerel kanıt kökü `QA/JUMP_CONTACT_REVIEW_2026-09-13`: `jump-native.xml`, 39 `*-motion.json`, `jump-review-metrics.json`, sekiz `runtime-inventory-*.json`, `prefab-inventory.json`, `contact-live.json`. Temas gözlemcisi yalnız bellekte çalıştırıldı; oyun kaynaklarına eklenmedi. Telefon performansı ölçülmedi. Yeni video veya paket oluşturulmadı. Kayıt/varlık karşılaştırması ve tercih iadesi `integrity-summary.json` ve `editor-restored.json` içinde.
