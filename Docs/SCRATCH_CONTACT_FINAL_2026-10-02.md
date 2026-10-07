# Tırmalama temas ve geçiş düzeltmesi — 2 Ekim 2026

Başlangıç 04:59:16 UTC / 07:59:16 İstanbul. Kullanıcının ek 30 dakika sınırı 05:29:16 UTC. Kapanış: 2026-10-02T05:29:20.210780+00:00.

Pati hedefleri göğüs altında daha alçak konuma alındı; kameraya uygun yan çapraz duruş kabul ediliyor. Kök, direk ve kamera taşınmadı. İki pati dönüşümlü kısa tarama yapıyor. Gerçek son deri pozunda yalnız yüzey normalinde temas düzeltiliyor. Bu düzeltmenin ağırlığı yükselişle birlikte artıyor. Dirsek yönü ve hedef birlikte geçiş yapıyor; çözülmüş kolu tekrar kaynak poza karıştırıp direğin içine sokan son dönüş kaldırıldı.

Kaynak animasyonun başlangıcı zaten dik olduğu için mevcut ayakta iskelet pozu yakalanıp tırmalama pozuna 0,8 saniyede geçiliyor; iniş de 0,8 saniye. İniş tamamlandığında bekleme animasyonuna geçiş kaynak klibin dik pozunu bir kareliğine yeniden göstermiyor. Bilek kaynağı, kemik uzunlukları ve ölçek korunur. Aktif vuruşta dirsek iç açısı40–165 derece; geçişte doğal ayakta pozundan bu aralığa yumuşakça gidilir.

Ölçüm yalnız en yakın üçgenin normaline dayanmaz: eski ham imzalı derinlik ayrıca saklanır; gerçek içeride/dışarıda altı yönlü ışın sınıflaması ve kesin yüzey uzaklığı kullanılır. Gerçek direk içinde/dışında pozitif/negatif kontrol kaydedilir. 5 mm deri ve 3 mm vuruş teması sınırları gevşetilmedi. Yükseliş/iniş/çıkış dahil yeni eklem sürekliliği kontrolü eklendi; 60 FPS örneklerinde10 derece karelik sınır korunur.

Nihai seçili native sonuçlar:
- ScratchReworkTests.Large: Failed — Rise/settle or exit popped a joint between 60 FPS frames   Expected: less than 10.0f   But was:  18.414917f
- ScratchReworkTests.Medium: Passed
- ScratchReworkTests.Small: Failed — Rise/settle or exit popped a joint between 60 FPS frames   Expected: less than 10.0f   But was:  11.6213751f
- ScratchReworkTests.PauseCancelAndBlockedStarts: Passed
- ScratchReworkTests.OtherToys: Passed

Son iki regresyon, son yalnız geçiş ağırlığı değişikliğinden önceki aynı tur sürümünde çalıştı; XMLleri manifestte ayrı belirtilir. Tam cihaz/FPS/bütün ırklar/bütün konumlar kabulü değildir.

persian: 558 kare, 574070 deri noktası; gerçek en derin giriş 0.0 mm; ham 0.0 mm; kök 0.0; tersdirsek 0; açıaşımı 0; bütün geçişlerde en büyük eklem adımı 11.6214 derece; direğe göre görünürlük 100.0%; video 558/558 kare çözüldü.
domestic-shorthair: 558 kare, 504431 deri noktası; gerçek en derin giriş 0.0 mm; ham 0.0 mm; kök 0.0; tersdirsek 0; açıaşımı 0; bütün geçişlerde en büyük eklem adımı 8.9162 derece; direğe göre görünürlük 100.0%; video 558/558 kare çözüldü.
maine-coon: 558 kare, 605067 deri noktası; gerçek en derin giriş 0.0 mm; ham 0.0 mm; kök 0.0; tersdirsek 0; açıaşımı 0; bütün geçişlerde en büyük eklem adımı 18.4149 derece; direğe göre görünürlük 100.0%; video 558/558 kare çözüldü.

Videolar gerçek Game View1080p60FPS, iki ritim. Yerleştirme hazırlıkları dışında yalnız gerçek etkinlik kareleri kaydedildi; sayaçlar QA kopyasıdır. Kare dizileri ve önceki adayların geçişleri bağımsız gözle incelendi. Bu turda son videoların Unity VideoPlayer üzerinden tam oynatımı yapılmış sayılmaz; piksel kareleri baştan sona çözüldü. Post görünürlük metriği kedinin kendi gövdesiyle örtüşmeyi ölçmez; yan açı yakın patiyi belirginleştirir, uzak pati bazı karelerde kısmen örtüşebilir. Kullanıcı görsel kabulü henüz yoktur.

7932 başlangıç dosyası aynı; değişenler: Assets/Scripts/Activities/CatActivityAnimation.cs, Assets/Scripts/Activities/CatPawReachMotion.cs, Assets/Scripts/Activities/CatToyContactMotion.cs, Assets/Scripts/Activities/ScratchPostActivity.cs, Assets/Tests/PlayMode/LivingFinalPolishTests.cs, Assets/Tests/PlayMode/ScratchReworkTests.cs. Yeni/eksik: 0/0. Dört gerçek kayıt ve CP2 yedeği aynı; 16/16 tercih aynı. Bir eski Eat klibi başlangıçta okunamadı. İki font önbelleği ve EditorSettings yalnız bu turun güncel başlangıcına döndü. Üç temiz normal sahne/tek dinleyici; Play/QA/derleme/build/profiler kapalı, Unity açık. Önceki native Access version uyarısı çözülmüş sayılmaz. APK/commit/push/yayın yok. UI işi yeniden başlatılmadı. Süre sınırında duruldu.
