# Android paketleme engeli — 23 Eylül 2026

## Sonuç — kullanıcı onayıyla kapandı

Kullanıcı, Akıllı Uygulama Denetimi'ni kapatıp derlemeyi yeniden denemeyi açıkça onayladı. Onay sonrası başlangıç 17:30:40 UTC, son sınır 18:00:40 UTC. Microsoft'un belgelediği `VerifiedAndReputablePolicyState=0` ve `CiTool.exe -r` yöntemi standart Windows yönetici onayıyla uygulandı. Son WMI durumu **SmartAppControlState=Off, AntivirusEnabled=True, RealTimeProtectionEnabled=True**. Başka güvenlik koruması/istisnası değiştirilmedi. Önceki aşağıdaki onay bekleme notları tarihseldir.

**Tam Android APK derlemesi başarılı: 0 hata, 1 uyarı.** 17:38:00–17:44:32 UTC, 392,39 saniye; IL2CPP Release, ARM64, LZ4, StrictMode, mevcut 12 etkin sahne. Yeni dosya `Builds/Android/CatHome_Test_0.1.0_20260923.apk`; 301.459.547 bayt (287,49 MiB). SHA256 `154ED49159947E2F75C769801DE38173269761D9D56DB4B9D942E86A0C603344`. APK v2 imzası ve manifest doğrulandı; `com.vexorialabs.cathome`, 0.1.0/code1, arm64-v8a. Mevcut Android Debug sertifikasıyla test paketi; mağaza yayını veya telefona kurulum yapılmadı. Eski masaüstü APK'sının üzerine yazılmadı. BuildReport'taki 1596,84 MB bütün çıktı raporudur, APK boyutu değildir.

CiTool işlemi başarılı yenilemeden sonra `Operation Successful / Press Enter to Continue` isteminde bekledi. Etkin Off durumu ve tam APK başarısından sonra yalnız bu göreve ait PID/ebeveyn/başlangıç zamanı doğrulanmış yardımcı kapatıldı; üst PowerShell raporunu yazıp çıktı. Bu nedenle `approved-security-result.json` içinde komut çıkışı -1/success=false, fakat etkin ayar başarısı ayrı `security-state-confirmed.json` ile doğruludur. Güvenlik servisleri sonlandırılmadı; görev yardımcıları açık bırakılmadı.

Üç gerçek kayıt ve 25 ProjectSettings dosyası güncel başlangıçla aynı. Üretim kodu/oyun varlıkları elle değiştirilmedi; Unity'nin standart build temizliği önceden oluşmuş dört izlenmeyen PerformanceTestRunInfo/Settings JSON/meta dosyasını kaldırdı. Kaynak değişikliği yapılmadı; Play açılmadı, Unity açık/idle ve Android hedefinde kaldı. Esas kanıtlar aynı QA klasöründe `final-build.json`, `apk-verification.json`, `apk-signature.txt`, `apk-badging.txt`, `security-state-confirmed.json`, `helper-cleanup.json`, `preservation-after-build-stage.json`, `approved-retry-closure.json`. Commit/push/yayın yok.

## İlk tanı

Kullanıcının 20:25 TR ekranındaki `Internal build system error / BuildProgram exited with code 1` hatası incelendi. Başlangıç 17:27:00 UTC; bu inceleme için en geç 17:52:00 UTC. Önceki SurfaceQueryMeasurement C# düzeltmesi korunur; yeni hata farklı bir aşamadadır.

Unity Editor.log ve Windows CodeIntegrity olayları aynı dosyayı doğruladı: `C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Data\Tools\BuildPipeline\PlayerBuildProgramLibrary.dll`. `netcorerun.exe` bu dosyayı yüklerken `0x800711C7` alıyor. 17:25:25 UTC olay 3077/3033 ve 3118, etkin Smart App Control engelini gösteriyor; `VerifiedAndReputablePolicyState=1`. Dosya 88.576 bayt, Authenticode NotSigned; SHA256 `167FA6760FDDDC753DE168080A9C421B7C13627AEEEDF6DB198234F53C5478B9`.

Zone.Identifier yok; dolayısıyla Unblock-File ile kaldırılacak indirme işareti bulunmuyor. Proje önbelleğinin bozuk olduğuna ilişkin kanıt yok; engellenen dosya Unity kurulumunda. Diğer kurulu 6000.4.0f1 sürümündeki aynı adlı DLL de imzasız ve farklı hash taşıyor; sürümler arası DLL kopyalama yapılmadı. Bu inceleme dosyanın resmi dağıtımla birebir eşleştiğini veya zararsızlığını doğrulamaz.

[Microsoft Smart App Control açıklaması](https://support.microsoft.com/en-us/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions) tek uygulamalık izin istisnası bulunmadığını belirtir. Güvenliği koruyan yol yayıncı tarafından güvenilir/imzalı dağıtım sağlanmasıdır; kurulum yenilemesinin tek başına çözüm olacağı doğrulanmadı. Smart App Control'ü kapatmak bilgisayar genelindeki ek korumayı azaltır; mevcut Unity kullanma yetkisi bu değişiklik için açık izin sayılmadı. Güvenlik ayarı değiştirilmedi, istisna/bypass uygulanmadı. Böyle bir değişiklik için kullanıcı kararı gerekir.

Yalnız tanı ve bu not/checkpoint güncellemesi yapıldı. Üretim kodu, oyun varlığı, kayıt, ProjectSettings veya Unity kurulumu değiştirilmedi. Play/derleme başlatılmadı; Unity açık ve son okunan durum idle/Play kapalı. Tam APK başarısı yok, hata henüz çözülmedi. Kanıtlar Git dışındaki `QA/BUILD_APP_CONTROL_2026-09-23` klasöründe `diagnosis.json`, `code-integrity.json`, `build-error.txt` dosyalarıdır. Önceki C# testinin 52 assembly/0 hata sonucu tam APK kabulü değildir.
