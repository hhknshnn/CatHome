# Oyuncunun verdiği kedi adı — 28 Eylül 2026

Kullanıcı, verilen ad yerine sol üstte başka ad gösterilmesini düzeltmemizi istedi. İlk kayıtlı çalışma saati 07:07 UTC; başlangıçtan gelen süre sıfırlanmadı. Yalnız isim gösterimi kapsamı; yeni APK, telefon kurulumu veya genel geliştirme yapılmadı.

## Düzeltme

İlk isim onayı `PlayerPrefs` değerini doğrudan yazdığı için `CatIdentityService.Changed` bildirimi atlanıyordu. Etkin HUD bu yüzden eski/varsayılan `MELO` metninde kalıyordu. İsim onayı ve editör isim temizleme yolu artık ortak `CatIdentityService.CatName` setter'ını kullanır. Kaydedilen ad, zaten açık olan etikete aynı karede ulaşır.

HUD ve konuşmadaki ad etiketi oyuncu girdisini düz metin olarak gösterir; `<b>Pati</b>` gibi bir ad biçimlendirme komutu sayılmaz. Türkçe ve büyük/küçük harfler korunur. Mevcut 14 metin öğesi sınırı ve baş/son boşluk temizliği değişmedi; ırka bağlı ayrı isim veya yeni kayıt şeması eklenmedi.

Üç runtime dosyası değişti: `PetTutorialHint.cs`, `Home/CatIdentityLabel.cs`, `CatDialogueView.cs`. Mevcut `PhoneOnboardingGuidanceTests.cs` içine gerçek düğmeyle isim, yeniden adlandırma ve yeniden yükleme kontrolü eklendi; ilk misafir testine anlık HUD adı doğrulaması eklendi.

## Kanıt

- Düzeltme öncesi native test, `Pamuk` onaylandıktan sonra HUD'da `MELO` görerek beklenen hatayı üretti: `baseline-name-native.xml`.
- Son native sonuç **2/2 PASS**: `name-fix-final-native.xml`. Pamuk, Şaşkın Çıtır, mİşKeT, literal etiket içeren ad; gerçek Kedim onay düğmesiyle Zeytin; yeni sahne/HUD yüklemesi; gerçek ilk misafir/isim/öğretici akışı.
- Ara `name-fix-native.xml` 1/2 idi: yeniden yükleme kontrolü henüz ana menü arkasında etkinleşmemiş HUD'ı okuyordu. Test artık ana menüden çıkıp oyun HUD'ının etkinleşmesini bekler. Bu adımda ürün kodu değişmedi.
- 1920×1080 Türkçe gerçek Unity görüntüsü `QA/CAT_NAME_HUD_2026-09-28/name-hud-zeytin-1920x1080-Turkish.png` gözle kontrol edildi. Fiziksel telefon/klavye, tüm çözünürlükler veya gerçek uygulama sürecini yeniden başlatma testi değildir; kaydın yeniden okunması yeni oyun sahnesi yüklenerek sınandı.

## Koruma ve kapanış

Üç gerçek PC kaydı başlangıç baytlarıyla aynı. 16 tercih bağımsız tekrar okunarak doğrulandı; gerçek oyuncu adı `hako` korundu. Testler `UiQaTestSession` kopyasında çalıştı. Tarihsel oyuncu kaydı yüklenmedi. Play/QA/derleme kapalı, üç normal sahne temiz, Unity açık.

Test kaynaklı iki font önbelleği ve EditorSettings, bu turun başlangıç SHA256 değerleriyle birebir eşleşen baytlara geri döndü. Fallback font adayı Git'ten okundu ancak yalnız güncel başlangıç hash'iyle birebir eşleşmesi doğrulandıktan sonra kullanıldı. Bir özgün animasyon dosyası başlangıçta kabuk tarafından okunamadı; tüm varlıkların eksiksiz hash doğrulaması iddia edilmez. Ayrıntı `files-final.json`, `snapshot-note.txt`, `editor-and-saves-final.json`, `preferences-verified.json` ve `closure.json`.

APK üretilmedi, telefona kurulmadı, commit/push/yayın yapılmadı. Önceki telefon aday APK/stayon kapanışı bu görevde ele alınmadı. Sonuç kaynak projededir; mevcut Android paketleri bu düzeltmeyi içermez.
