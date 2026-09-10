# Cat Home — Hesap, Bulut Kayıt ve Rekabet Planı

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

**Tarih:** 21 Ağustos 2026
**Durum:** Uygulandı ve Unity Cloud production ortamına dağıtıldı (24 Ağustos 2026)
**Yerel kayıt şeması:** v11 (değişmedi)

## Ürün kararı

- İlk açılış Main Menu v4'tür. İlk `PLAY` ve onaylı `NEW GAME`, Google/misafir
  seçimini açar; `MİSAFİR OLARAK DEVAM ET` ağ beklemeden oyunu başlattığı için
  bu yüzey hesap duvarına dönüşmez.
- Oyuncu ilk seçimde veya daha sonra Settings içinden Google hesabını bağlayabilir.
- Bu bağlantı Gmail kutusuna erişmez; yalnız Google kimliğiyle oturum açar.
- Sıralamada e-posta adresi ve Google'daki gerçek ad gösterilmez. Ayrı bir takma ad
  istenmez; yalnız onboarding/CAT JOURNAL'da seçilen denetimli kedi adı görünür.
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

Tablo Google arkadaşlarıyla sınırlı değildir; skor göndermeye uygun bütün Cat Home
oyuncuları aynı global havuzdadır. Ekran altın/gümüş/bronz podyumda ilk üçü, kaydırmalı
listede 4–50'yi, toplam global oyuncu sayısını ve oyuncunun kendi sıra kartını gösterir.
İnternet yoksa son başarılı snapshot `OFFLINE • LAST UPDATED ...` etiketiyle salt
okunur gösterilebilir.

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

## Bulut kayıt ve kesintisiz devam

- Yerel `cat-home-save.json` oynanışın hızlı kaynağı olarak kalır.
- Her başarılı yerel yazım `cat-home-save.json.recovery` kopyasını yeniler.
- Bağlı hesapta save dosyası Cloud Save Player Files'a yüklenir; `writeLock` ile başka
  cihazın kaydını sessizce ezmek engellenir.
- Cihaz ve bulut birbirinden farklıysa oyuncuya seçim ekranı gösterilmez. Geçerli
  kayıtların zamanı karşılaştırılır, en yeni kayıt otomatik uygulanır; tam eşitlikte
  bulut kazanır ve `CONTINUE` oyunu kesmeden açar.
- Bulut uygulanmadan önce yerel kopya `.before-cloud-*`; cihaz buluta yazılmadan önce
  eski bulut kopyası `.cloud-shadow-*` adıyla yerel destek yedeğine alınır.
- Online sıralama ödülleri ve doğrulanmış IAP sunucu otoritesinde olduğundan bulut
  save seçimi bunları çoğaltamaz.

### Uygulandı — 21 Ağustos 2026

- `com.unity.services.cloudsave` **3.4.0** kuruldu. Tek Player File anahtarı
  `cat_home_save_v11`; içerik yerel v11 JSON'un tamamıdır ve hesap kimliği/token içermez.
- `CloudSaveSnapshotCodec` cihaz/bulut özetini HOME LV, Coin, Diamond, koleksiyon,
  oda ve tarihle üretir; JSON SHA-256 özeti ile son güvenilen yerel kopyıyı tanır.
- Son yerel hash + Cloud Save `writeLock` hangi tarafların değiştiğini tanır. Tek veya
  iki taraf değişmiş olsa da en yeni geçerli kayıt otomatik uygulanır; tam zaman
  eşitliğinde bulut kazanır ve oyuncu hiçbir seçim yüzeyi görmez.
- Bulut seçilirse gelecekteki şema reddedilir, `.before-cloud-*` güvenlik kopyası
  alınır, atomik yerel yazım/migrasyon yapılır ve oda yeniden bağlanır. Cihaz seçilirse
  eski bulut JSON'u `.cloud-shadow-*` destek yedeğine alınır ve güncel write lock ile
  yüklenir.
- Her başarılı yerel save üç saniye debounce ile eşitlenir. Unity Services henüz
  başlamadıysa çevrimdışı açılış hata üretmez; Google bağlantısı tamamlanınca ilk
  eşitleme otomatik çalışır.
- Settings `PRIVACY & DATA / GİZLİLİK VE VERİ` yüzeyi manuel sync, politika/silme/veri
  sayfaları ve iki dokunuşlu hesap silme sunar. Silme önce Cloud Save Player File'ını
  write lock ile, sonra Unity Authentication hesabını siler; yerel misafir yolculuğu
  cihazda korunur.
- Gerçek Editor Google dönüşünde UGS Authentication + Player Accounts bağlı ve Cloud
  Save `Synced` doğrulandı. Android development APK'da `unitydl` intent-filter gerçek
  proje kimliğiyle doğrulandı; fiziksel cihaz dönüşü USB/ADB cihazı bağlanınca yapılacak.

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
- Sıralamadaki kedi adları çevrilmez; sistem etiketleri ve ödül açıklamaları çevrilir.

## Uygulama sırası

1. **Yerel kurtarma:** kalıcı recovery kopyası + bozuk/eksik ana dosya testleri.
2. **NEW GAME + dil:** yerel, çevrimdışı ve tamamen test edilebilir ürün yüzeyleri.
3. **Hesap çekirdeği:** UGS anonim kimlik, Dashboard Unity Player Accounts/client ID
   ve gerçek Editor Google bağlantısı tamamlandı; Android cihaz dönüş testi açık.
4. **Bulut kayıt:** upload/download ve write-lock tabanlı otomatik en-yeni-kayıt
   çözümü tamamlandı; oyuncuya cihaz/bulut seçim yüzeyi gösterilmez. Bağlı hesapta
   NEW GAME için yeni profil nesli sıralama sunucu dilimiyle birlikte kalır.
5. **Salt okunur tablolar — yerelde tamamlandı:** Games hub paneli,
   Runner/Catch günlük/haftalık/all-time, ilk 50, kendi sıra kartı ve çevrimdışı cache.
6. **Doğrulanmış gönderim — yerelde tamamlandı:** Cloud Code skor hesabı,
   access-control kuralları ve tekrar kullanılan oyun kimliği koruması.
7. **Dönem ödülleri — tamamlandı:** reset/arşiv tanımları, idempotent günlük/
   haftalık ödül ve otomatik claim sunumu production ortamına dağıtıldı.

## Dış kurulum gereksinimi

Authentication, Unity Player Accounts ve Cloud Save proje bağlantısı tamamlandı.
Cloud Code modülü, altı Leaderboards tanımı ve access-control politikası Unity Cloud
production ortamına dağıtıldı. Player Accounts
istemci kimliği yalnız Unity'nin servis ayarında tutulur; istemci sırrı, erişim anahtarı
ve token repoya yazılmaz. Doğrudan Google Play Games otomatik girişi daha sonra ayrıca
istenirse Google Play Console kurulumu gerektirir; mevcut akış Player Accounts'un
çapraz platform güvenli Google sosyal girişidir.

## Uygulanan hesap çekirdeği — 21 Ağustos 2026

- `com.unity.services.authentication` **3.5.2** kuruldu; proje mevcut Unity Cloud
  Project ID'sine bağlıdır.
- `AccountIdentityService` hesap seçimini save v11 dışında tutar. Misafir kimliği
  rastgele GUID'dir; cihaz kimliği, e-posta, Google adı veya parola saklanmaz.
- Misafir seçiminde oyun anında açılır; UGS anonim oturum arka planda kurulur.
  İnternet/servis yoksa yalnız çevrimiçi özellikler bekler.
- Unity Player Accounts güvenli sistem tarayıcısını açar ve Google sosyal girişini
  sunar. Var olan anonim oyuncu önce geri yüklenir, sonra `LinkWithUnityAsync` ile
  aynı Player ID'ye bağlanır; böylece ilerleme sessizce başka hesaba taşınmaz.
  Android/iOS'ta tarayıcının açılması tamamlanmış giriş sayılmaz; gerçek
  `SignedIn`/`SignInFailed` dönüşü beklenir.
- İlk PLAY ve onaylı NEW GAME aynı TR/EN hesap kartını kullanır. NEW GAME verisi,
  seçim tamamlanmadan yazılmaz; Google iptal/hatasında mevcut save değişmez.
- Settings içindeki `ACCOUNT / HESAP` satırı seçim/misafir/bağlı durumunu gösterir
  ve sonradan Google bağlantısını başlatır.
- Giriş düğmesi Google'ın resmî ön onaylı renkli `G` varlığını, değiştirmeden beyaz
  karesi içinde kullanır; Cat Home'un aqua halo/pearl yüzeyi simgenin dışındadır.
- Dashboard'da `CatHome` Unity Player Accounts sağlayıcısı PC + Android/iOS için
  etkinleştirildi; gerçek OAuth client ID servis ayarına senkronlandı. Editor'da
  Google seçimi, localhost callback, Player Accounts ve UGS Authentication bağlantısı
  başarıyla doğrulandı.
- Player Care sitesi herkese açık yayımlandı:
  `https://cathome-player-care.hhknshnn.chatgpt.site` (`/privacy`,
  `/delete-account`, `/data`). Eksik dış kurulum: Android gerçek cihaz deep-link
  oturum testi (kullanıcı tarafından ertelendi), bu URL'lerin Dashboard/mağaza
  alanlarına bağlanması ve UGS DSA bildirim akışı.

## Uygulanan rekabet dilimi — 21 Ağustos 2026

- `CompetitionService` Runner/Catch için DAILY/WEEKLY/ALL-TIME tablolarını okur;
  ilk 50 ile oyuncunun kendi sırasını gösterir ve servis çevrimdışıyken son güvenli
  snapshot'a düşer.
- Sıralama adı onboarding/CAT JOURNAL kanonik kedi adından otomatik alınır; ayrı
  `PLAYER NAME` veya `SAVE NAME` yüzeyi yoktur. Leaderboard doğrulaması 3–16
  karakterde harf, rakam, boşluk, `_` ve `-` kabul eder. Google gerçek adı, e-posta
  adresi, token veya Player Accounts profili tablo metadatasına yazılmaz.
- Runner/Catch istemcisi doğrudan skor yazmaz. Cloud Code ham oyun ölçümlerini
  doğrular, kanonik skoru sunucuda hesaplar ve run/hunt kimliğinin ikinci kullanımını
  reddeder.
- Günlük tablolar 00:00 UTC, haftalık tablolar Pazartesi 00:00 UTC sıfırlanır ve
  arşivlenir. Günlük/haftalık ödül arşiv sürümü+sıra ile hesaplanır; idempotent işlem
  kimliği aynı dönemin iki kez ödenmesini engeller.
- Yerel modül Release build **0 hata / 0 uyarı**; Unity QA EditMode **295/295**,
  PlayMode **10/10**, validator **0/0**, Console temiz ve kanonik 1/1/1 düzenindedir.
- Production dağıtımı 24 Ağustos 2026'da tamamlandı: altı tablo, canlı
  `CatHomeCompetition.ccm` modülü ve doğrudan Player skor yazımını engelleyen
  access-control politikası uzak servis üzerinde doğrulandı. Günlük ilk reset dönemi
  `2026-08-25T00:00:00Z`, haftalık ilk reset dönemi `2026-08-31T00:00:00Z` başlar.
- Dağıtım sonrası Unity QA: EditMode **296/296**, PlayMode **10/10**, validator
  **0/0**, Console temiz; kanonik sahne yığını ve Camera/AudioListener/EventSystem
  **1/1/1**.
