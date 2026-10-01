# Kedi adının ilk harfi ve Türkçe yazı tutarlılığı — 28 Eylül 2026

Kullanıcı artık açıkça yeniden istemedikçe APK oluşturulmamasını istedi. Bu kalıcı kural kök AGENTS.md başına kaydedildi. Bu görev yalnız isim biçimi ve isim alanlarının yazı tutarlılığıdır; APK/cihaz kurulumu yapılmadı.

İlk kayıtlı başlangıç 07:22:38 UTC. Tahmin 20–30 dakika; genel kesin üst sınır 10:22:38 UTC. Sayaç sıfırlanmadı; gerçek kapanış QA/closure.json içindedir.

## Son davranış

- İlk harf Türkçe kurallarla büyür: `pamuk → Pamuk`, `ipek → İpek`, `ışık → Işık`, `çağrı → Çağrı`. Cihazın/oyunun dilinden bağımsızdır. Baştaki emoji/rakam harf sayılmaz; ilk harf bulunur. Adın kalan harflerinin büyük/küçük yazımı korunur; `pAMUK → PAMUK`.
- Klavyeden ayrık gelen işaretler Unicode NFC ile birleştirilir: `u + ◌̈ → ü`, `s + ◌̧ → ş`. İsim yazılırken de uygulanır; boşluklar ve harf büyüklüğü onay öncesinde korunur. Seçim konumları birleşmiş metne göre eşlenir. Eksik Unicode giriş dizisi tamamlanana kadar beklenir.
- İsim girişlerinin alan-font ve metin-font referansları aynı, Türkçe glifleri hazır NunitoBody'ye bağlandı. Kedim alanında önce alan `Fredoka-SemiBold SDF`, metin `NunitoBody` idi. Konuşmanın isim etiketi ortak font ve gerçek ağırlığı kullanır; yapay kalınlaştırma kaldırıldı. HUD'ın mevcut FredokaEmphasis görünümü korunur.
- Önceki anlık isim bildirimi/tek kimlik kaydı düzeltmesi korunur. Eski küçük harfli kayıt da yeni kuralla görüntülenir; yeniden onaylanınca standart yazım kaydedilir. Mevcut 14 metin öğesi sınırı ve baş/son boşluk temizliği sürer.

## Doğrulama

19/19 EditMode: ad örnekleri, Türkçe i/ı, ayrık Unicode işaretleri, emoji, metin öğesi sınırı, en-US/tr-TR kültürleri ve hazır Türkçe font kapsamı.

3/3 PlayMode: gerçek isim onayı ve aynı-kare HUD güncellemesi, Kedim üzerinden küçük harfle yeniden isim verme, yeni sahne/HUD yüklemesi, gerçek ilk misafir/öğretici akışı, Türkçe isim girişinin NFC gösterimi, bütün `ÇçĞğİıÖöŞşÜü` biçimlerinin HUD'da aynı fonttan gelmesi ve eksik glif yerine başka karakter konmaması. Eski küçük harfli kaydın tekrar onaylanarak standart biçimde saklanması da geçti.

Başlangıç `baseline-font-native.xml`, ayrık `gümüş` girdisinin `Gümüş` olmasını beklerken FAIL üretti. Son kabul yalnız `case-editmode-native.xml` ve `case-font-playmode-native.xml`; toplam **22/22**. İlk başarısız test bütün Türkçe harflerin başka fonta düştüğünü kanıtlamaz; başlangıç atlaslarında Türkçe karakterler zaten vardı. Düzeltme fontları değiştirmek yerine alan bağlarını, yapay kalınlığı ve metin biçimini düzeltir.

1920×1080 Türkçe gerçek Unity görüntüleri gözle kontrol edildi: `turkish-name-input-...png`, `turkish-name-confirmed-...png`, `turkish-hud-cagri-sukru-...png`. İsim/font fixture'ında ihtiyaç öğreticisi arka planda kalır; bu görüntü bütün normal oynanışın kabulü değildir. Fiziksel telefon/IME klavyesi veya tüm çözünürlükler test edilmedi. APK oluşturulmadı.

## Koruma ve kapanış

Beş runtime dosyası ve iki mevcut test dosyası değişti; yeni runtime/test dosyası yok. Gerçek üç PC kaydı byte aynı; 16 tercih bağımsız tekrar okunarak aynı doğrulandı. Diskteki gerçek ad `hako` korunur, yeni gösterim `Hako` olur. Tarihsel kayıt yüklenmedi.

Test sırasında değişen dört font asset önbelleği ve EditorSettings yalnız bu turun başlangıç SHA256 değerleriyle eşleşen yedeklere döndü. Kaynak TTF/font/sahne/prefab değişikliği teslim edilmedi. Başlangıçta bir özgün animasyon dosyası kabuk tarafından okunamadı; o dosya için hash koruma iddiası yok.

Play/QA/derleme kapalı, üç normal sahne temiz, Unity açık. Commit/push/yayın yok. Kanıt kökü `QA/CAT_NAME_CASE_FONT_2026-09-28`; `native-final-manifest.json`, `preservation-editor-final.json`, `files-final.json`, `fonts-restored-check.json`, `closure.json` esas. Önceki telefon kapanışı hâlâ bu görevin dışındadır. Yeni genel geliştirme kendiliğinden başlamaz.
