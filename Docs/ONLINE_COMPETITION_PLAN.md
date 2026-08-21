# Cat Home — Hesap, Bulut Kayıt ve Rekabet Planı

**Tarih:** 21 Ağustos 2026
**Durum:** Ürün kararı + uygulama sırası
**Yerel kayıt şeması:** v11 (değişmedi)

## Ürün kararı

- İlk açılış hesap duvarına dönüşmez. Oyuncu anonim/misafir kimlikle hemen oynar.
- Oyuncu daha sonra `GOOGLE İLE BAĞLA` ile evini bir Google hesabına bağlar.
- Bu bağlantı Gmail kutusuna erişmez; yalnız Google kimliğiyle oturum açar.
- Sıralamada e-posta adresi ve Google'daki gerçek ad gösterilmez. Yalnız denetlenen
  oyun içi takma ad görünür.
- Android için Google Play Games, çoklu platform için web tabanlı Unity Player
  Accounts adaptörü aynı hesap sözleşmesinin iki sağlayıcısı olabilir.
- Hesap bağlamak isteğe bağlıdır; çevrimdışı ev, bakım ve mini oyun akışları çalışmaya
  devam eder. Çevrimiçi sıralama ve bulut kurtarma bağlantı gerektirir.

## Sıralama yüzeyi

Games hub içine `LEADERBOARDS` açılır. İki oyun ayrı tutulur; Runner ve Catch'in
ham skor ölçekleri birbirine karıştırılmaz.

Her oyun için üç sekme vardır:

| Sekme | Kural | Sıfırlama |
| --- | --- | --- |
| DAILY | Günün en iyi skoru | Her gün 00:00 UTC; önceki sürüm arşivlenir |
| WEEKLY | Haftanın en iyi skoru | Pazartesi 00:00 UTC; önceki sürüm arşivlenir |
| ALL-TIME | Oyuncunun tarihî en iyi skoru | Sıfırlanmaz |

Ekran ilk 50 oyuncuyu, oyuncunun kendi sırasını ve kendi çevresindeki birkaç sırayı
gösterir. İnternet yoksa son başarılı snapshot `OFFLINE • LAST UPDATED ...` etiketiyle
salt okunur gösterilebilir.

İleride birleşik bir Games ligi eklenirse ham skor toplamı kullanılmaz. Her oyundaki
haftalık derece lig puanına çevrilir; böylece Runner'ın yüksek sayısal skoru Catch'i
ezmez.

## Ödül ilkesi

- Günlük ödül küçük ve katılımı teşvik edicidir.
- Haftalık ödül daha değerlidir; üst sıralarda sınırlı Diamond ve görünür kupa/rozet
  bulunabilir.
- ALL-TIME tablosu tekrar eden para ödülü vermez; unvan ve prestij yüzeyidir. Aksi
  hâlde aynı oyunculara sürekli ekonomi basar.
- Ödül istemciden doğrudan verilmez. Dönem kapandığında sunucu arşivlenmiş tabloyu
  okur ve ödülü tam bir kez verir.
- İşlem kimliği `leaderboard:{boardId}:{versionId}:{playerId}` biçimindedir. Aynı
  ödül yeniden denense bile ikinci ödeme oluşmaz.
- İlk denge önerisi:

| Derece | Günlük | Haftalık |
| --- | ---: | ---: |
| 1 | 200 Coin | 1.000 Coin + 10 Diamond + Gold kupa |
| 2–3 | 150 Coin | 700 Coin + 6 Diamond + Silver kupa |
| 4–10 | 100 Coin | 400 Coin + 3 Diamond + Bronze kupa |
| İlk %10 | 60 Coin | 250 Coin |
| Geçerli skor | 20 Coin | 100 Coin |

Bu sayılar Remote Config/Cloud Code tarafında tutulmalı; istemci güncellemesi olmadan
dengelenebilmelidir.

## Skor güvenliği

- Sonuç ekranı yalnız ham `score` göndermez. Runner süre, mesafe, coin, çarpışma,
  bakım bonusu ve koşu kimliğini; Catch süre, yakalama, combo/strike özeti ve av
  kimliğini yollar.
- Cloud Code aynı kanonik kurallarla skoru yeniden hesaplar, negatif/NaN/aşırı hız,
  imkânsız yakalama sıklığı ve tekrar kullanılan run/hunt kimliğini reddeder.
- İstemcinin Leaderboards ve Economy yazma yetkisi kapatılır; yalnız izin verilen
  Cloud Code uçları yazabilir.
- Bu doğrulama sıradan kayıt düzenlemeyi ve basit sahte istekleri engeller. Tam
  hile dayanımı gerekirse sonraki sürümde imzalı olay özeti veya server-side replay
  doğrulaması eklenir.

## Bulut kayıt ve çakışma

- Yerel `cat-home-save.json` oynanışın hızlı kaynağı olarak kalır.
- Her başarılı yerel yazım `cat-home-save.json.recovery` kopyasını yeniler.
- Bağlı hesapta save dosyası Cloud Save Player Files'a yüklenir; `writeLock` ile başka
  cihazın kaydını sessizce ezmek engellenir.
- Çakışmada otomatik `last write wins` kullanılmaz. Oyuncuya iki kart gösterilir:
  cihaz/bulut tarihi, HOME LV, Coin, Diamond, koleksiyon ve son oynanan oda.
- `USE THIS DEVICE` veya `USE CLOUD SAVE` seçimi yeni write lock ile kaydedilir.
- Online sıralama ödülleri ve doğrulanmış IAP sunucu otoritesinde olduğundan bulut
  save seçimi bunları çoğaltamaz.

## NEW GAME sınırı

- İki aşamalı onay ister; yanlış dokunuşla çalışmaz.
- Oyun içi ev/progression, tutorial, görevler ve yerel skor geçmişi sıfırlanır.
- Dil, erişilebilirlik, ses ayarları ve hesap bağlantısı korunur.
- Gerçek parayla alınmış doğrulanmış haklar silinmez.
- Bağlı hesapta `START NEW HOME EVERYWHERE` ayrı bir sunucu işlemi olmalıdır. Profil
  nesli artırılır; eski cihazın bekleyen kaydı yeni evi geri getiremez.
- NEW GAME, hesap silme değildir. Hesap silme ve kişisel veri talebi ayrı ayar
  yüzeyidir.

## Dil

- İlk paket Türkçe ve İngilizce olur.
- Unity Localization tabloları kullanılır; sahne/prefab içine yeni sabit kullanıcı
  metni yazılmaz.
- İlk çalıştırmada cihaz dili TR ise Türkçe, diğer dillerde İngilizce seçilir; oyuncu
  Settings içinden her zaman değiştirebilir.
- Sıralama takma adları çevrilmez; sistem etiketleri ve ödül açıklamaları çevrilir.

## Uygulama sırası

1. **Yerel kurtarma:** kalıcı recovery kopyası + bozuk/eksik ana dosya testleri.
2. **NEW GAME + dil:** yerel, çevrimdışı ve tamamen test edilebilir ürün yüzeyleri.
3. **Hesap çekirdeği:** UGS anonim kimlik, Google/Player Accounts bağlama ve güvenli
   takma ad.
4. **Bulut kayıt:** upload/download, write-lock çatışma paneli, yeni profil nesli.
5. **Salt okunur tablolar:** Games hub paneli, Runner/Catch günlük/haftalık/all-time.
6. **Doğrulanmış gönderim:** Cloud Code skor hesabı, access-control kuralları ve
   tekrar kullanılan oyun kimliği koruması.
7. **Dönem ödülleri:** reset trigger, arşiv, idempotent ödül ve inbox/claim sunumu.

## Dış kurulum gereksinimi

3. adımdan itibaren Unity Dashboard projesinde Authentication, Cloud Save, Cloud
Code ve Leaderboards açılmalı; Android için Google Play Console istemci kimlikleri
tanımlanmalıdır. Bu gizli değerler repoya yazılmaz.
