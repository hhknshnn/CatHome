# Berjer: doğrudan çıkış, dönüş ve iniş — 15 Eylül 2026

Kullanıcı berjerdeki pati adımlarıyla dönmeyi, fazla yön değiştirmeyi ve ileri–geri hareketi istemedi. **Berjer için önceki tek tek patiyle dönüş tercihi geçersizdir.** Yeni sonuç Unity'de kullanıcı denemesine hazır; nihai görsel kullanıcı onayı henüz alınmadı.

## Değişiklik

- `room.armchair` önündeki mevcut girişten doğrudan sıçranır. Yaklaşma tamamlandıktan sonraki 84 cm yan dolaşma kaldırıldı.
- Özgün Jump klibi ve basış sonrası toparlanma tamamlanır; sonra sabit noktada bir defa 90° dönülür. Pati adımı/IK çevrimi, minderden 10 cm dışarı açılıp geri gelme yok. Ölçülen dönüş 0,375–0,400 sn; eski dönüş yaklaşık 3,542 sn ve 36 pati adımıydı.
- Kalkışta aynı kısa 90° dönüşle berjerin açık ön yüzüne bakılır. Giriş noktasına doğrudan inilir; eski 120° havada yön değiştirme kalktı.
- Minderde kullanılan sabit kedi merkezi 10 cm öne alındı. Başın arkalığa yaklaşması önlendi; sıçrama sonu, dönüş, oturma/uyku/uyanma ve iniş hazırlığı aynı noktayı kullanır. Berjer modeli, yerleşimi, sahne/prefab noktaları değiştirilmedi.
- `CatJumpMotion.TurnDirectly` mevcut destekli pozu ve kaynak eklem açılarını koruyan kısa yön geçişidir. Duraklama karesinde önceki delta ile ilerlemez. Berjer `CatSurfaceTurnMotion` adımlarını kullanmaz. Diğer ürünlerin dönüş yolları ve beğenilen bütün sesler aynı.
- Runtime değişen dört dosya: `CatFurnitureJumpClearance`, `CatJumpMotion`, `PerchNapActivity`, `CatSupportedFurnitureMotion`. Özgün sıçrama örneklemesi, model/kemik boyları ve ortak anatomi sınıfları aynı.

## Doğrulama

Esas [native manifest](QA/ARMCHAIR_DIRECT_TURN_2026-09-15/native-final-manifest.json) ve `native-chair-release.xml`: **4/4 başarılı**.

1. On ırk × 15/30/60 FPS: 30 tam berjer çevrimi; ikişer tek yönlü 90° dönüş, en fazla 0,4 sn, dönüşte kök yol uzunluğu **0 m**, pati adımı karesi **0**. Yaklaşma sonrasında ek dolaşma ve havada dönüş yok; özgün sıçrama/basış sürekliliği, diz düzlemi ve açık zeminde kontrol iadesi geçti.
2. Hem çıkış sonrası hem iniş öncesi dönüşte duraklatma/iptal: gövde ve patiler dururken sabit; denetleyici/ölçek/görsel konumu geri gelir. İptal sonrası tekrar tam çevrim geçer.
3. On ırk gövde/baş/kol açıklığı: ilk doğrudan yaklaşımda baş arkalığa yaklaşıyordu; sabit merkez öne alınınca düzeldi. Kalan 15 cm kalça probu uyarıları gerçek oturan gövdenin altına taşıyordu. Bu adaylar kuyruk hariç gerçek çizilen deri ağı ve 3 mm temas probuyla doğrulandı: **155 aday, 21.247 gerçek deri örneği, ölçülen en büyük iç içe geçme 0 m**. Geniş probu tek başına gerçek deri çakışması saymayın. Diğer eşyalardaki mevcut test eşiği aynı.
4. Sehpa dönüşü ve özgün sıçrama sürekliliği kontrolü geçti.

Validator: **0 hata / 0 uyarı**. Gerçek oyun **Berjer kestir** düğmesiyle Russian Blue: **1/1** tamamlanma, tek olay, açık çıkış ve kontrol iadesi; [rapor](QA/ARMCHAIR_DIRECT_TURN_2026-09-15/manual-chair/report.json). Normal zaman ölçeği, kanıt örneklemesi 24 FPS. [Gerçek Unity kareleri](QA/ARMCHAIR_DIRECT_TURN_2026-09-15/index.html); video yok. Bu tur tüm eşyalar/on ırk veya fiziksel telefon taraması değildir.

`before-motion` eski davranışın ölçümüdür. `native-candidate-front.xml` ve `native-candidate-support-probe.xml` ara denemelerdir; son sonuç olarak sunulmaz. `chair-visuals` sabit merkezin öne alındığı son runtime ile alınan ayrıntı kareleridir; `manual-chair` gerçek oyun düğmesi kanıtıdır.

## Koruma ve devam

- Yeni başlangıca göre **71 sahne dosyası (14 oyun sahnesi dahil), 396 FBX, 144 WAV ve üç gerçek kayıt birebir aynı**. Altmış etkinlik kaynak dosyasının 56'sı aynı; yalnız yukarıdaki dört runtime dosyası değişti. [Hash karşılaştırması](QA/ARMCHAIR_DIRECT_TURN_2026-09-15/preservation-summary.json).
- Gerçek ana/recovery SHA256: `72EBFF5B1C95A86931060F7B54A1B7612884D3209CFC2604CBC8877017DB9873`; CP2: `03D4FD1A6420FF0D6CD6213FE08EA57598038EC589BA7CA02475692036EA9A8D`. Eski tarihsel hash geri yüklenmedi.
- Testlerin ardından **16/16 tercih ve editör sessizliği geri yüklendi**; EditorSettings başlangıç byte dizisi aynı. Son kullanıcı denemesi ayrı `Library/UiQaSession/20260915-115309` kopyasında Play/QA açık. Normal GameScene + CatHome_UI + LivingRoom_Level01 temiz; tek kedi/ses sistemi/dinleyici; çekim/derleme kapalı. Son durum `editor-final.json`.
- Kullanıcı için berjer düğmesi hazır bırakılır. Kullanıcı bitirmeden deneme kapatılmaz. **Tools > Cat Home > Tüm Sesler Denemesi > Bitir** veya Play durdurma, mevcut güvenli ön izleme sistemiyle 16 tercihi geri getirir. Odak dışı ses susması aynı davranıştır.
- Unity/PC kapatılmadı. APK, arşiv, video, commit, push, yayın yok. QA Git dışında. Önceki salon tablo giriş bulgusu kapsam dışında; plan/süre ve tek tek doğrulama kuralı sürer.
