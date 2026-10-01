# CAT HOME — Phase 3C: Material Identity & Surface Polish

28 Eylül 2026. Phase 3A ve 3B kullanıcı tarafından kabul edilmiş başlangıçtır. Bu görevde bütün ışıklar, taban renkleri ve mimari palet sabit tutuldu. Başlangıç 10:16:55 UTC; iki malzeme iterasyonu, kapanış ve süre `QA/VISUAL_PHASE3C_MATERIALS_2026-09-28/closure.json` içinde.

## MATERIALS CHANGED

1. **Carpet_1:** Plaster atamasından salonun Rug yüzeyine geçti. Mevcut Modern Surface shader korunarak mevcut Linen detay dokusu düşük kontrastla kullanıldı. Metalik 0, smoothness 0,10.
2. **Dört halı dikişi:** Ceramic/metal tepkisinden mat Rug yüzeyine geçti. Metalik 0,64 → 0; smoothness 0,57 → 0,10. Altın renk aynı; artık metal malzeme olarak davranmaz.
3. **Pencerenin Lit camı:** mevcut URP/Lit üzerinde metallic 0,731 → 0, smoothness 0,924 → 0,65. Mevcut Unlit GlassPanel, doku/alpha, blend, render queue, shader keywords ve saydam katman sayısı aynı.
4. **Sehpa üst tabla/alt raf:** Plaster yerine PaintedWood etiketi ve daha temiz kaplama tepkisi; smoothness 0,22 → 0,32, çok düşük detay kontrastı/relief.
5. **Sehpanın aqua raf sepeti ve aynı slottaki kitap kapağı:** Plaster yerine Plastic/kaplı sert yüzey tepkisi; smoothness 0,22 → 0,30, düşük detay kontrastı/relief.

Sehpanın parça rolleri mevcut `ArtSource/Blender/PremiumFurniture/build_living_coffee_table.py` kaynağından doğrulandı. Modelde çekmece yok; coral kitap slotu değiştirilmedi. Temsilci iki sert yüzey grubu seçildi, bütün ürünler taranmadı.

Koltuk/minder/yatak kumaşları mevcut metallic 0 / smoothness 0,12 ile tutarlı bulundu ve korundu. Ahşap zemin, mobilya ve pencere çerçevesinin 0,35 smoothness tepkisinde bu açıdan bariz uyumsuzluk görülmedi; üçüncü iterasyon yapılmadı. Gerçek küçük metal ayak/yaka/donanımlar ve seramik kaplar korundu.

## BEFORE → AFTER

1. Halı dikişlerinin metalik parıltısı kalktı; renkli ip/kenar hissi daha uygun. Bu, normal Game View mesafesinde en okunur fark.
2. Halı yüzeyi daha mat ve sakin kaldı. Mevcut dokuma örneği ince kullanıldı; renk alanı küçülmedi ve mint baskısı tamamen çözülmedi.
3. Sehpa kaplama tepkisi daha temiz; cam açıklığı saydam ve okunur kaldı. Sehpa/cam farkı sabit oda kamerasında incedir, dramatik bir görsel dönüşüm iddia edilmez.

Computer Use ile gerçek Unity Game View'da aynı 1920×1080 çözünürlük/kamera: `before.png`, `iteration-1.png`, `iteration-2.png`, yeniden açılan sahne `final-reloaded.png`. Son ekranın alt editör bölümünde Windows masaüstü seçici görünür; oyun görüntüsünü örtmez. Kabul yalnız sayısal değerlere dayanmamıştır.

## FINAL MATERIAL VALUES

| Yüzey / yerel aile | Metallic | Smoothness | Shader |
|---|---:|---:|---|
| Halı — Rug | 0 | 0,10 | CatHome/Modern Surface |
| Dikiş — Rug | 0 | 0,10 | CatHome/Modern Surface |
| Lit cam | 0 | 0,65 | Universal Render Pipeline/Lit |
| Sehpa üstü/rafı — PaintedWood | 0 | 0,32 | CatHome/Modern Surface |
| Aqua raf kaplaması — Plastic | 0 | 0,30 | CatHome/Modern Surface |

Halı/dikiş: mevcut `Modern_Linen_Surface.png`, detay tiling 2×2; kontrast sırasıyla 0,14 / 0,08, relief 0,00012 / 0,00008. Boyalı/kaplı yüzeylerde mevcut detay dokusu korunur; kontrast 0,04 ve relief 0,00002. Bunlar mevcut shader alanlarıdır; yeni shader özelliği, normal map veya texture üretilmedi. Rug/PaintedWood/Plastic yerel metadata etiketleridir; shader içinde yeni branch veya genel üreticiye yeni aile tablosu eklenmedi.

## CLASSIFICATION

**ModernWorldArtBuilder değiştirilmedi.** Kaynak doğrulandı: `Family` malzeme adındaki carpet/rug sözcüklerini tanıyor, fakat `Carpet_1` context'i soft listesindeki eksiklik nedeniyle Plaster'a düşüyor. Ayrıca `Derive` içindeki gold/brass/champagne isim kuralı, dikişin gerçek kumaş rolünden bağımsız metallic 0,64 atıyor. Cream/gold gibi adların tür belirlemesi başka ortak kurallarda da bulunuyor.

Güvenli genel çözüm Family, metal override ve ortak cache/bağlam davranışının birlikte ele alınmasını gerektirir. Diğer odalara yanlış eşleşme riski taşımayan küçük bir genel değişiklik bu kısa salon turunda doğrulanmadığından kullanıcı talimatındaki erteleme yolu seçildi. Sahne atamaları düzeltildi; ortak kaynak ve üretici değişmedi. Üretici tekrar topluca çalıştırılırsa yerel varyantların yeniden türetilebilmesi bilinen risktir.

Sonraki sınıflandırıcı işi için: carpet/rug/floor_mat/doormat/bathmat/sunmat gerçek yüzey bağlamı → halı; stitch/soft trim → adındaki gold'dan bağımsız metal olmayan yüzey; renk adı tek başına tür seçmemeli. Bu kurallar bu turda genel koda uygulanmadı.

## SUNDAY CITY QUALITY TARGET CHECK

Kullanıcının verdiği prensipler temelinde: yanlış metal tepkileri kaldırıldı, yumuşak/kaplı yüzey rolleri daha doğru, highlights kontrollü, görsel gürültü düşük. Yüzey okunurluğu/polish yönünde sınırlı ama anlamlı ilerleme var. Sabit oda kamerasındaki ayrım ince; bütün malzeme türleri uzaktan tamamen ayrı okunuyor iddiası yok. Referans varlık, UI veya tasarım kopyalanmadı.

## VALIDATION / PRESERVATION

- Bağımlılıklar kontrol edildi: cam ortak pencere prefabından; sehpa kaynakları birçok ürün/prefabdan; halı ve dikişler salonun geçmiş recovery sahnelerinden de referanslı. Beş salon varyantı kullanıldı, ortak materyaller ve prefab dosyaları değiştirilmedi.
- 5/5 varyantta tüm renk alanları, shader ve keywords aynı. İzinli yüzey alanları/etiketleri dışında bütün seri materyal özellikleri kaynakla eşit. `material-scope-check.json` son kabul; ilk kontrolün JSON tag-map yapısını yanlış ele alan ara false çıktısı gerçek kapsam hatası değildir.
- Nihai sahne farkı dört dikiş Renderer referansı ve üç prefab instance materyal override bloğuyla sınırlı. Cam için bir materyal slot override'ı eklendi; nesne/katman eklenmedi. Diğer bloklar başlangıçla aynı. Unity SaveScene'in 19 eski script bloğundaki otomatik serileştirme yan değişiklikleri güncel başlangıç kopyasından ayıklandı.
- Kaydedilmiş sahne yeniden açıldı: 7 hedef renderer / 8 slot / 5 materyal, shader supported true, tüm sahnede 0 eksik materyal yuvası, 0 eksik script. Console 0 error / 0 warning.
- 8.726 okunabilen başlangıç dosyasından yalnız salon sahnesi değişti; 8.725 aynı. 5 yeni `.mat` + 5 `.meta`. Bir özgün `A_CartoonAnimal_Cat_Eat.anim` başlangıç hash'i erişim engeli nedeniyle alınamadı; eksiksiz tüm-varlık hash iddiası yok.
- Phase 3A ışık/transform/shadow alanları ve Phase 3B'nin 12 materyali aynı. Camera/UI/gameplay/collider/navigation/geometry/model/kedi kürkü/Volume/diğer sahneler aynı.
- Ana kayıt, recovery ve iki mevcut yedek byte aynı. Play açılmadı; gerçek kayıt yazma veya geri yükleme yapılmadı. Tercih setter'ları çağrılmadı, ayrıca tercih hash karşılaştırması yapılmadı.
- Modern Surface'ın mevcut tek detay örneklemesi ve aynı shader keywords korunur. Yeni reflection, parallax, saydam katman, screen-space effect veya paket yok. Telefon FPS/ısınma ölçülmedi; performans artışı iddia edilmez.
- Üç temiz normal sahne; Play/QA/derleme kapalı, Unity açık. APK/commit/push/yayın yok.

## UNRESOLVED

Zeminin ahşap/plank kimliği Phase 3D'ye kalır; floor materyali, doku ve UV değiştirilmedi. Mint halı hâlâ güçlü büyük renk alanı. Düşük detayların tamamı bu kamera mesafesinde seçilmiyor. Genel sınıflandırıcı düzeltmesi ve bütün odalar/kürk varyantları/fiziksel telefon kabulü bu turun kapsamı değildir.

## RESULT

Öncelikli yanlış malzeme atamaları düzeltildi ve yüzey tepkileri kontrollü hale geldi. Görsel sonuç ince; ana ışık ve palet korunarak iki iterasyonla duruldu. Phase 3D başlatılmadı. Bu yeni yüzey düzeninin kullanıcı görsel onayı henüz alınmadı.
