# Redmi Note 9 Pro — fiziksel önce/sonra kontrolü, 27 Eylül 2026

Kullanıcı telefonu bağladı; ardından yeni APK'nın kayıtlar silinmeden güncellenmesini ve karşılaştırılmasını açıkça onayladı. Başlangıç 26 Eylül 21:06:37 UTC; tahmin 10–15 dakika. Kanıt: `Docs/QA/REDMI_DEVICE_CHECK_2026-09-27`.

## Sonuç

Redmi Note 9 Pro / Android 12 / 2400×1080 üzerinde, gerçek Cat Home SurfaceView sunum zamanlarından yaklaşık 30'ar saniyelik ölçümler:

| Ekran | Eski FPS | Yeni FPS | Eski p95 kare aralığı | Yeni p95 kare aralığı |
|---|---:|---:|---:|---:|
| Ana menü | 12,63 | 19,92 | 83,64 ms | 50,17 ms |
| Salon, normal HUD / kedinin bekleme animasyonu | 9,58 | 15,04 | 117,05 ms | 66,90 ms |

Yaklaşık %58 / %57 artış var; **30 FPS hedefi sağlanmadı, telefon performansı hâlâ açık**. Bu kısa ölçüm bütün odalar, yürüyüş/etkileşimler, mini oyunlar veya uzun süreli ısınma kabulü değildir. Eski salon ölçümü HUD girişinin son kısmını da kapsar; yeni salon HUD hazırken ölçülmüştür. Kare dağılımı ve bağımsız timestats sonuçları farkı destekler; tamamen eşlenmiş deterministik benchmark değildir.

Geçerli dört ölçümde kare zaman kapsamı 29,42–29,84 sn, örnek halkası boşluğu 0, son zaman damgaları ilerliyor. Aynı oyun katmanının timestats ortalamaları sırasıyla 12,384 / 19,974 ve 9,448 / 15,217 FPS. İlk `old-menu` ölçümü ekran uyuduğu için yalnız 16,62 sn kapsar; kabul dışıdır. Betik bu nedenle gerçek kare kapsamını ve son verinin ilerlemesini doğrular. Toplam SurfaceFlinger veya Android Java arayüz FPS'i oyun FPS'i olarak kullanılmadı.

Telefon şarjdaydı; güç tasarrufu 0. Yeni menü öncesi batarya 30,9°C, kapanışta 32,0°C. Thermal HAL hazır olmadığı için termal kısıtlama olmadığı iddia edilmez. CPU/GPU süre ayrımı ölçülmedi; kalan dar boğazın kesin kaynağı bu verilerden çıkarılamaz.

## Kurulum ve kayıtlar

Eski paket SHA256 `E35AB92CA0054CCFF6A4D91BE2F8E00FC713BACC00651B388EA3079A0804B28A`, 301451507 bayt. Sürüm adı/code her iki pakette 0.1.0/1 olduğundan yalnız sürüm etiketiyle karar verilmedi.

`CatHome_Test_0.1.0_PerfTutorial_20260926.apk` mevcut uygulamanın üzerine başarıyla kuruldu. Telefondaki yeni base.apk SHA256 `46BCD27F65B59B8DFE81FEFC55B4E3B8A9CC96EFD472921C1606CA535AB7973E`; hazırlanan APK ile birebir aynı. Uygulama kaldırılmadı, veri temizlenmedi.

Kurulumdan önce telefondaki ana/recovery kayıtları yerel QA'ya kopyalandı. Kurulumdan hemen sonra her ikisinin hash'i aynı kaldı: `7B183F7C5C0DAE5D5591C6A4983F7F378C0FBEFE2F2EEB8AE205D653A63DE402`. Son gerçek açılış/ölçüm normal ihtiyaç tüketimi ve zaman damgalarını güncelledi; jeton 215, elmas 0, seviye 1 ve diğer ilerleme alanları korundu. Eski kayıt geri yazılmadı. Mevcut kayıtla açıldığı için ilk kurulum/tutorial telefonda yeniden denenmedi.

## Kapanış

Ölçüm için geçici `stay_on_while_plugged_in` ayarı önce USB, sonra telefon AC şarj olarak bildirdiği için tüm şarj türlerinde etkinleştirildi; özgün **0** değerine geri döndüğü doğrulandı. SurfaceFlinger timestats kapatıldı. Cat Home normal arka plana alındı, yeni sürüm kurulu kaldı. Uygulama log filtresi boş döndü; bu bir kapsamlı hata yokluğu kanıtı değildir.

Bu tur oyun kaynaklarına, Unity editörüne veya başka projelere dokunulmadı. Yalnız test kanıtı ve bu teslim notu eklendi. Kalan iş, bu cihazda 30 FPS için CPU/GPU ayrımını ölçerek hedefli optimizasyondur; genel yeni geliştirme turu başlatılmadı.
