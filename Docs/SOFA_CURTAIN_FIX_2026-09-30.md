# Koltuk yönlendirmesi ve perde derinliği — 30 Eylül 2026

Kullanıcının ekran görüntüsü ve güncel kayıtlı duruşu incelendi. Başlangıç yaklaşık 17:34 UTC; kesin kapanış QA/SOFA_CURTAIN_FIX_2026-09-30/closure.json.

## Koltuk

Kayıtlı konum (2.2578938, .05, -.0647035), yaw159.8°; girişe mesafe .168m, gerçek iniş yönüne fark yaklaşık61.87°. Yakınlık düğmesi görünüyordu, ancak mevcut35° güvenli yön kontrolü ve o çapraz yöndeki iniş temas kontrolü reddediyordu. Ret mesajı çeviri tablosunda eksik olduğu için “Biraz sonra yeniden deneyelim” çıkıyordu.

Eksik TR/EN mesaj kaydı tamamlandı. Etkinlik konuşmaları kendi etkinliğine bağlandı; başarılı başlangıç kendi eski yönlendirmesini kapatır, başka kaynağın mesajını silmez. Sürekli dinlenmeden çıkış tek satır Kalk / Get up gösterir.

Koltuk hareketi,35° sınır, .22m başlangıç bölgesi ve temas kontrolleri gevşetilmedi. Kullanıcının tam konumundan gerçek joystick'i koltuğa yöneltmekle hem Persian hem Oriental çıkabiliyor; tıklamada ani yer değiştirme yok. Dinlenme ve gerçek Kalk tıklamasıyla yere dönüş geçti. İlk ek testte koltuktan uzağa doğru sabit vektörle sehpanın içinden yürütme denemesi engelde durdu; bu rota nihai test değildir. Nihai test aynı konumdan koltuğa doğru kısa joystick yönlendirmesidir.

## Perde

Başlangıçta kullanıcının Scene değişikliği kaydedilmemişti. Bu değişiklik önce user-unsaved-scene.unity kopyasına ve gerçek sahneye kaydedildi; başlangıç koruması bunun ardından alındı. İlk “diskten eski konum yükleniyor olabilir” hipotezi doğrulanmadı: normal Play girişinde perde konumu Scene ile birebir aynıydı. Perdeyi yeniden konumlandıran runtime kodu bulunmadı. Scene'de önden düzgün görünen perdenin farklı kamera açısından kayık görünmesinin asıl nedeni pencere yüzeyinden öne taşan derinliğiydi.

Perde x3.558513→3.630000 ile7.15cmduvara doğru alındı; z.604→.648384 ile gerçek çerçeve merkezine getirildi. Pencerenin kendisi ve perde yüksekliği/ölçeği aynı. Tekrar kullanılan Editor hizalama yardımcısı da bu derinlikle güncellendi. İki ayrı oda yüklemesinde aynı transform doğrulandı. Play açıkken yapılan değişikliklerin Unity tarafından çıkışta geri alınması normaldir; kalıcı düzenleme Play kapalıyken yapılmalı ve sahne kaydedilmelidir.

## Kanıt ve koruma

5/5 EditMode yerelleştirme, 2/2 nihai PlayMode (iki ırkta aynı kullanıcı konumu→joystick→koltuk→Kalk; sehpa regresyonu) geçti. Computer Use ile Unity incelendi. Son gerçek1920GameView [final.png](QA/SOFA_CURTAIN_FIX_2026-09-30/final.png); görüntü ayrı kayıt kopyasıyla QA'dır, sayaçlar gerçek ilerleme değildir. Önceki denemeler tarihsel; final XML ve native-final-manifest esas.

Kamera/HUD/ışık korunur. Test/önizlemenin serileştirdiği UI, kamera viewport ve font önbelleği değişiklikleri yalnız bu turun hash doğrulanmış başlangıcına geri alındı. Dört gerçek kayıt ve16tercih korunur. Bir özgün Eat klibi başlangıçta okunamadı. Üç temiz normal sahne,Unityaçık,Play/QA/derlemekapalı. APK/commit/push/yayın/fiziksel telefon testi yok. Yeni iş kendiliğinden başlamaz.
