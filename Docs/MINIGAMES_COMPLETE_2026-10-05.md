# Dört mini oyun — teslim, 5 Ekim 2026

Mevcut iki oyun geliştirildi; iki yeni oyun entegre edildi. Oyun menüsü dört kartlı düzene geçti. Sahne fotoğrafları gerçek Unity kameralarından üretildi. Petrol cam yüzeyler, mercan eylem düğmeleri, doğal ahşap, nane kumaşlar ve sıcak taş detayları dört oyunda ortak kullanılır.

## Oynanış

- **Eve Dönüş:** Cat Runner'ın varsayılan modu 75 saniyede biter. Bahçe yolu, pazar ve ev sokağı bölümleri; rota ilerlemesi ve üç sonuç hedefi; isteğe bağlı sonsuz mod. Engeller arasında nefes aralıkları ve son yaklaşmada temiz rota. Mevcut koşu kontrolleri, canlar ve ödül akışı korunur.
- **Pati Avı:** Fare seçilir, kedi takip eder; doğru mesafe ve yönde oyuncu Pati at düğmesine basar. Otomatik sıçrama kaldırıldı. Üç dalgada 2/3/4 fare ve üç hareket karakteri; evle uyumlu oyun odası. Gerçek pati inişi yakalamayı belirler.
- **Yumak Rotası:** Altı sabit fizik bulmacası, bölüm başına üç pati hakkı. Yumağı geriye çekip bırakınca kedi ona yaklaşır ve vurur; duvar/minder sekmeleri, inci bonusu ve yıldızlar. Çözülen bölüm başına 12 jeton, tur sonunda tek ödeme.
- **Gölet Keyfi:** 75 saniye, altı balık ve kalıcı keşif albümü. Seçilen balık kıyıdaki kediye gelir; daralan halka nane rengine geçtiğinde pati hamlesi yapılır. Erken/geç hamle kaçırır. Yakalama başına 8 jeton, tur başına en çok 120.

Yeni oyunlarda can tüketimi yok. İlerlemeleri aynı kaydın v12 şemasına eklendi; eski kayıt göçü, derin kopya ve bozuk değer sınırları doğrulandı. Seçili kedi, TR/EN, duraklatma, tekrar, oyunlara/evine dönüş ve tek etkin kamera/ses dinleyicisi akışları bağlandı.

## Görsel kaynaklar

Blender'da 11 yeni model üretildi: yumak, sepet, minder, altı balık, halka ve bitki. Kaynak ve üretim betiği `ArtSource/Blender/CozyGames/` içinde; önce açık olan Blender belgesi `Blender_Before.blend` olarak korundu. Unity FBX/prefab/malzemeleri `Assets/Art/CozyGames/`, yeni sahneler `Assets/Scenes/Cozy/`. Gölet suyu için hafif hareketli desen shader'ı var. Mevcut ev dekorları ve malzeme dokuları yeniden kullanıldı. Bu turda Affinity veya ImageGen çıktısı üretilmedi.

İnceleme: [Görüntü galerisi](QA/MINIGAMES_COMPLETE_2026-10-05/gallery.html). Galeri 2 dil × 3 oran × (4 oyun × 4 durum + oyun menüsü) = **102 gerçek GameView** sunar. Sayılar ayrı QA kaydından gelir; oyuncunun gerçek ilerlemesi değildir. Sonuç panelleri sunum için açıldı; bu ekranlar tamamlanmış bir turun kanıtı sayılmaz.

## Doğrulama

Sonuçlar test adı bazında son koşuyla birleştirildi: **47 EditMode + 19 PlayMode = 66 benzersiz PASS**, son sonucu FAIL olan seçili test yok. Tüm proje testlerinin çalıştırıldığı iddia edilmez. Eski başarısız koşular XML dosyalarında tarihsel olarak korunur.

- Yumak: gerçek Unity pointer down/drag/up olaylarıyla altı bölümde fiziksel sepet tamamlama; 18 yıldız, tek 72 jeton ödeme ve tekrar sıfırlaması.
- Gölet: erken hamle reddi, duraklatılan saat, altı keşif, kıyıya yaklaşma, çift tıklamada tek yakalama, doğal 75 saniye sonu ve tek 48 jeton ödeme. Testte saat 3× hızda ilerletildi.
- Koşu: 75 saniye bitişi ile sonsuz modun ayrımı; mevcut çarpışma/havada bitiş/tekrar/ödül testleri. Yeni bitiş testi saati 74,99'a getirir ve parkur üreticisini kapatır; tam 75 saniyelik engelli koşu kabulü değildir.
- Av: otomatik sıçrama olmaması, yön değiştirme, gerçek iki yakalama, zeminde hareket ve hareketsizken yakalamama. Son görsel turda yakalanan Awake sırası kaynaklı `Hash 0` hatası düzeltildi; beş av testi yeniden **5/5 PASS**.
- Geçişler: eşzamanlı açılış reddi, duraklatma/iptal, mevcut iki oyunda sekiz odaya dönüş ve bakım eylemlerini bırakma. Bu fixture'ın duruş araması güncel gerçek hazır koşulunu kullanır; ev hareket/temas kuralları değiştirilmedi.
- 102 son layout raporunda etkin düğme çakışması, ekran dışına taşma veya merkezde tıklanamazlık bulgusu yok. Son 30 menü/karşılama görünümünde ayrıca görünür TMP metin taşması yok. İngilizce koşu hedef metni bu kontrolde kısaltıldı.
- Computer Use: Oyunlar → Yumak → Başla → Duraklat → Devam → Duraklat → Oyunlar geçti. Masaüstü otomasyonunun sürüklemesi güvenilir olay sırası üretmedi; fiziksel mouse/telefon sürükleme kabulü iddia edilmez. Altı bölümün native pointer olay testleri ayrı kanıttır.
- LevelContentValidator: **0 hata / 0 uyarı**. Son açılışlar ve masaüstü akışından sonra Console: **0 hata / 0 uyarı**.

Fiziksel telefon, FPS/ısınma, dokunmatik kullanım ve yeni tasarımın kullanıcı görsel kabulü henüz yok. Son native av koşusundan sonra yalnız İngilizce açıklama kısaltıldı; bu değişiklik görsel taşma kontrolünden geçti.

## Koruma ve kapanış

Başlangıç `2026-10-05T17:28:55.727419Z`; gerçek kapanış `QA/MINIGAMES_COMPLETE_2026-10-05/closure.json` içindedir. 8337 okunabilir başlangıç dosyasından **8317 aynı, 20 kapsam içi değişik**, 126 yeni kaynak/meta, eksik dosya yok. Önceden bir özgün Eat klibi okunamadı; tüm dosyalar için eksiksiz hash iddiası yok.

Beş gerçek kayıt dosyası başlangıçla birebir aynı. Testler ayrı kayıt kopyasında çalıştı, QA oturumu kapandı. **Tercih başlangıç dosyası yanlış registry yolu nedeniyle boştu; 16 tercihin tamamının eşit olduğu iddia edilemez.** Dil Türkçeye geri alındı; tarihsel tercih yedeği yüklenmedi. İki dinamik font, üç banyo malzemesinin geçici ondalık değişimleri ve EditorSettings yalnız bu turun hash doğrulanmış başlangıcına döndürüldü; ara dosyalar QA'da tutuldu.

Ev sahneleri/prefabları/modelleri/klipleri ve kabul edilmiş tırmalama kaynakları değişmedi. Son durumda üç temiz normal sahne, bir etkin AudioListener, Unity açık; Play/QA/derleme/build/profiler kapalı. APK, commit, push veya yayın yapılmadı.

Esas kanıtlar: `native-final-manifest.json`, `preservation-final.json`, `visual-final.json`, `editor-final.txt`, `architecture-final.txt`, `computer-use-final.txt`, `closure.json`. Yeni çalışma kendiliğinden başlatılmaz.
