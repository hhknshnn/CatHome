# Cat Home Low-Poly Cat + Rigify Design

## Amaç

Cat Home için sıfırdan, mobil oyuna uygun, sıcak ve sevimli bir low-poly kedi üretmek. Aynı geometri ve iskelet; ileride farklı tüy renkleri, desenler, göz renkleri ve aksesuarlarla tekrar kullanılacak. Blender çalışma rig'i Rigify tabanlı olacak; Unity dışa aktarımında yalnız deform kemikleri ve bake edilmiş animasyonlar kullanılacak.

## Değişmez tasarım kararları

- Stil: yuvarlatılmış low-poly, büyük ama dengeli kafa ve gözler, belirgin patiler, hafif tombul gövde.
- Yön: kedi Blender'da `+Y` yönüne bakar; zemin `Z=0` düzlemidir.
- Ölçek: ayakta omuz yüksekliği yaklaşık `1.25 m`; `CatRoot` Unity tarafında mevcut `0.5` ölçeğini koruyabilir.
- Tek ana topoloji, tek UV düzeni ve tek rig kullanılacak. Renk/desen varyantları yeni iskelet veya yeni animasyon üretmeyecek.
- Tüy partikülü kullanılmayacak. Siluet geometriyle, tüy hissi renk/desen ve yüzey tepkisiyle verilecek.
- Hedef bütçe: bitmiş gövde ve yüz toplamı yaklaşık `6–10 bin üçgen`; aksesuarlar bu bütçeden ayrı izlenecek.
- Rigify kontrol rig'i Blender kaynak dosyasında kalacak. Unity'ye yalnız `DEF-*` deform kemikleri, skinned mesh, blendshape'ler ve bake edilmiş klipler gidecek.

## Model mimarisi

### Ana gövde

- Gövde, göğüs, kalça, boyun ve kafa nihai aşamada kesintisiz ve deformasyona uygun ortak topolojiye dönüştürülecek.
- Omuz, dirsek, bilek, kalça, diz ve arka ayak bileği çevresinde en az iki destek halkası bulunacak.
- Patiler gerçek bir kedi patisi gibi geniş, yere oturan ve parmak yönü okunabilir olacak. Zemin penetrasyonu kabul edilmeyecek.
- Kuyruk tabanında yeterli deformasyon halkası ve en az 6 deform kemiği bulunacak.

### Yüz ve ağız

- Göz küreleri ayrı objeler olacak.
- Üst ve alt göz kapakları bağımsız kontrol edilebilir olacak; `Blink_L`, `Blink_R` ve birlikte kırpma hedefleri hazırlanacak.
- Alt çene ayrı hareket edecek. Üst dişler kafaya, alt dişler çeneye bağlı olacak.
- Dil ayrı geometri olacak ve en az iki küçük deform kemiğiyle dışarı çıkma/kıvrılma hareketine hazırlanacak.
- Ağız köşeleri ve dudak hattı daha sonra `Mouth_Open`, `Mouth_Smile`, `Mouth_Mew` gibi yüz hedeflerini destekleyecek.

## Rig planı

- Gövde: Rigify `cat` metarig.
- Yüz ekleri: metarig üzerinde çene, dil ve göz kapağı için özel kemikler; uygun yerde Rigify `basic.super_copy` yaklaşımı.
- İlk rig kontrolü dört ayak temasını, omurga yüksekliğini, kuyruk zincirini ve kafa yönünü kapsayacak.
- İlk animasyon testleri: ayakta bekleme, çömelme, oturma, loaf, göz kırpma ve ağız açma.
- Loaf sırasında ön gövde yukarı kalkmayacak; hareket kalça ve arka bacakların aşağı/toplanma hareketinden başlayacak. Patiler zemini delmeyecek.

## Renk ve desen sistemi

- Ortak UV atlası değişmeyecek.
- Materyal parametreleri: `Base Fur`, `Pattern A`, `Pattern B`, `Muzzle/Chest`, `Nose/Pads`, `Iris`.
- Desenler maske/texture setleriyle değişecek: tekir, calico, tuxedo, solid, colorpoint vb.
- Her varyant yalnız materyal profili + desen maskesi + isteğe bağlı göz rengi tanımlayacak.
- İlk onaylanan görünüm nötr sıcak krem/karamel prototip olacak; desen üretimine siluet ve rig onayından sonra geçilecek.

## Kaynak düzeni

- Güncel Blender kaynak: `ArtSource/Blender/Cat/LowPolyRigify/CatHome_LowPolyCat_Rigify_v2.blend`
- Önceki kontrol kopyası: `ArtSource/Blender/Cat/LowPolyRigify/CatHome_LowPolyCat_Rigify_v1.blend`
- Kurulum betiği: `ArtSource/Blender/Cat/LowPolyRigify/build_lowpoly_cat_foundation_v1.py`
- QA çıktıları: `Temp/CatRigQA/LowPolyRigify_v1/`
- Unity dışa aktarımı, model ve rig onayından sonra ayrı bir sürümlü klasöre alınacak.

## Küçük aşamalı çalışma planı

| Aşama | Çıktı | Durum |
|---|---|---|
| 0 | Boş Blender dosyası, Rigify doğrulaması, belge ve klasörler | Tamamlandı |
| 1 | Çok-görünümlü karakter paftası ve gerçek 3D temel mesh | Hyper3D temel mesh v2 hazır; görsel onay bekliyor |
| 2 | Siluet düzeltmesi ve birleşik deformasyon topolojisi | İçe aktarma ekseni düzeltildi; yüz oranı revizyonu bekliyor |
| 3 | Rigify cat metarig'in doğru ölçü/yerleşimi | Bekliyor |
| 4 | İlk generate rig + dört pati zemin testi | Bekliyor |
| 5 | Göz kapağı, çene, diş ve dil kontrolleri | Bekliyor |
| 6 | Kısa idle/çömelme/oturma testleri | Bekliyor |
| 7 | Referansa göre kontrollü loaf animasyonu | Bekliyor |
| 8 | Unity FBX ve animasyon klipleri doğrulaması | Bekliyor |
| 9 | Renk/desen varyant sistemi ve ilk varyant seti | Bekliyor |

Her aşama görsel ve kısa video kontrolüyle kapatılacak. Bir aşama onay almadan sonraki büyük adıma geçilmeyecek.

## QA kapıları

- Ön, yan ve üç çeyrek görünüşte kedi silueti okunmalı.
- Dört pati ayakta ve ana pozlarda `Z=0` üstünde kalmalı.
- Diz/dirsek bükülmelerinde hacim çökmesi veya keskin kırılma olmamalı.
- Göz kırpma sırasında göz küresi görünür biçimde kapak dışına taşmamalı.
- Ağız açıldığında diş ve dil doğru ebeveynle hareket etmeli.
- Renk/desen değişimi rig'i, ağırlıkları ve animasyonları değiştirmemeli.
- Unity dışa aktarımında kontrol kemikleri bulunmamalı; yalnız deform iskeleti bulunmalı.

## Karar günlüğü

- 2026-09-03: Mevcut kedi rig'ini düzeltmek yerine sıfırdan Rigify uyumlu low-poly kediye geçilmesi kararlaştırıldı.
- 2026-09-03: Çalışmanın 20–30 dakikalık görsel/video kontrol noktalarıyla ilerlemesi kararlaştırıldı.
- 2026-09-03: Bilgisayarın süreç sonunda kapatılmaması kesinleştirildi.
- 2026-09-03: İlk temel model `4.194` üçgen ve `41` sahne nesnesiyle üretildi. Rigify cat metarig operatörü doğrulandı ve gizli `RIGIFY_Cat_Meta` nesnesi yeni dosyada oluşturuldu.

## Kontrol noktası 01 — Temel siluet

- Blender: `ArtSource/Blender/Cat/LowPolyRigify/CatHome_LowPolyCat_Rigify_v1.blend`
- Görsel: `Temp/CatRigQA/LowPolyRigify_v1/CatHome_LowPolyCat_Foundation_v1.png`
- Dönüş videosu: `Temp/CatRigQA/LowPolyRigify_v1/CatHome_LowPolyCat_Foundation_Turntable_v1.mp4`
- Bu aşamada model parçalı bir bloklamadır; ağırlık, deformasyon ve animasyon yoktur.
- Yüz parçaları şimdiden ayrı tutuldu: gözler, üst göz kapakları, çene, üst/alt diş ve dil.
- Rigify metarig yalnız doğrulama amacıyla dosyadadır; henüz modele oturtulmadı ve generate rig yapılmadı.
- Bir sonraki küçük revizyonda arka bacak/hock anatomisi, kuyruk kıvrımı, baş-gövde oranı ve ağız silueti düzeltilecek.

## Kontrol noktası 02 — Referans oran revizyonu

- Referans: iri amber gözlü, kısa ağızlı, yuvarlak kalçalı low-poly tekir kedi illüstrasyonu.
- Blender: `ArtSource/Blender/Cat/LowPolyRigify/CatHome_LowPolyCat_Rigify_v2.blend`
- Görsel: `Temp/CatRigQA/LowPolyRigify_v1/CatHome_LowPolyCat_Foundation_v2.png`
- Dönüş videosu: `Temp/CatRigQA/LowPolyRigify_v1/CatHome_LowPolyCat_Foundation_Turntable_v2.mp4`
- Model bütçesi: `7.498` üçgen. Bu artışın çoğu üç parçalı pati uçları, bıyıklar, ağız hattı ve okunabilir arka bacak bloklarından gelir.
- Değişiklikler: daha kısa kafa/ağız, daha dar gövde, yüksek yuvarlak kalça, kalça–diz–hock zincirli arka bacak, üç parmaklı geniş patiler, düzgün kavisli kuyruk, amber gözler, ince yanak çizgileri ve bıyıklar.
- Göz kapağı kontrol geometrisi dosyada korunur ancak nötr poz renderında gizlidir; yüz rig'i aşamasında gerçek kırpma testine alınacaktır.
- Desen parçaları yalnız stil önizlemesidir. Onaydan sonra kabarık geometriler yerine ortak UV üzerindeki varyant maskelerine taşınacaktır.

## Kontrol noktası 03 — Yeni anatomik gri temel

- Kontrol noktası 01 ve 02 görsel kalite bakımından reddedildi; yeni model bunların geometrisini kullanmaz.
- Yeni Blender kaynak: `ArtSource/Blender/Cat/LowPolyRigify/CatHome_CatSculpt_Blockout_v3.blend`
- Kurulum betiği: `ArtSource/Blender/Cat/LowPolyRigify/build_cat_silhouette_v3.py`
- QA klasörü: `Temp/CatRigQA/LowPolyRigify_v3/`
- Ana gövde; pelvis, göğüs, omuz, boyun, kafa, kulaklar, ağız hacmi, dört bacak ve patileri kapsayan tek birleşik bloktur.
- Kuyruk ve göz parçaları ileride bağımsız kontrol edilebilmeleri için ayrı tutulmuştur.
- Gri ve desensiz sunum, yalnız siluet/anatomi kararına ayrılmıştır. Renk, tekir deseni, bıyık, diş ve dil bu aşamada değerlendirme dışıdır.
- Rigify cat metarig operatörü doğrulanmış ve gizli bir metarig oluşturulmuştur; henüz modele oturtulmamıştır.
- Ana gövde `9.218` üçgendir. Bu geçici sculpt blok, onaydan sonra animasyona uygun retopolojiye çevrilecektir.

## Kontrol noktası 04 — Yöntem değişikliği ve karakter paftası

- Gri sculpt v3 de görsel kalite bakımından reddedildi. Prosedürel primitive/voxel-remesh yöntemi tamamen bırakıldı.
- Yeni yöntem: tutarlı çok-görünümlü karakter paftası → gerçek image-to-3D temel mesh → Blender temizliği ve retopoloji → Rigify cat metarig yerleşimi.
- Pafta: `ArtSource/Blender/Cat/LowPolyRigify/References/CatHome_CatTurnaround_v1.png`
- Pafta aynı kediyi ön, tam yan, arka ve üç çeyrek görünüşte; nötr, dört pati yerde ve rig uyumlu biçimde gösterir.
- Meshy ortam kontrolünde API anahtarı bulunmadı. Hunyuan3D ve Hyper3D Rodin BlenderMCP bağlantıları da kapalı durumda.
- Gerçek 3D üretimi, bu üç yoldan biri etkinleştirilmeden başlatılmayacak; eski prosedürel yönteme geri dönülmeyecek.

## Kontrol noktası 05 — Hunyuan 3D giriş görseli

- BlenderMCP içindeki Tencent Hunyuan3D entegrasyonu etkinleştirildi ve bağlantı doğrulandı.
- Tek 3D giriş görseli: `ArtSource/Blender/Cat/LowPolyRigify/References/CatHome_CatThreeQuarter_Input_v1.png`
- Giriş görseli, aynı karakter paftasındaki kediyi nötr dört ayaklı duruşta ve temiz stüdyo arka planında gösterir.
- Görselin Tencent Hunyuan3D hizmetine gönderilmesi dış veri aktarımıdır; görev açık kullanıcı izni beklemektedir.

## Kontrol noktası 06 — Gerçek 3D üretim bağlantısı

- Kullanıcı, referans görselinin Tencent Hunyuan3D'ye gönderilmesini açıkça onayladı.
- Hunyuan üretim çağrısı denendi ancak eklenti `LOCAL_API` modunda `http://localhost:8081` adresine bağlı ve bu adreste çalışan servis bulunmuyor (`WinError 10061`).
- Görev kimliği oluşmadı; görsel yüklenmedi ve harici üretim başlamadı.
- Blender içindeki manuel v4 blok, referansla görsel olarak eşleşmediği için reddedildi ve rig temeli olarak kullanılmayacak.
- Devam koşulu: yerel Hunyuan API sunucusunun `localhost:8081` üzerinde çalıştırılması veya BlenderMCP'nin `OFFICIAL_API` modunda kullanıcı tarafından yerel olarak girilmiş geçerli Tencent Cloud kimlik bilgileriyle yapılandırılması.

## Kontrol noktası 07 — Hyper3D temel mesh ve yüz sistemi

- Kullanıcının açık onayıyla `CatHome_CatThreeQuarter_Input_v1.png` görseli Hyper3D Rodin'e gönderildi ve gerçek 3D temel mesh üretildi.
- Temiz normalize edilmiş kaynak: `ArtSource/Blender/Cat/LowPolyRigify/CatHome_Cat_Hyper3D_v2_Normalized.blend`.
- Güncel yüz kontrol noktası: `ArtSource/Blender/Cat/LowPolyRigify/CatHome_Cat_Hyper3D_v3_Face.blend`.
- Hyper3D nesnesi `17.259` vertex ve `23.332` poligondur. İçe aktarılan gizli quaternion dönüşü sıfırlandı, model Z-up yapıldı ve patiler `Z=0` düzlemine oturtuldu.
- Önceki manuel/sculpt kedi nesneleri ve eski Rigify referansı güncel sahneden silindi. Güncel sahnede yalnız Hyper3D kedi, yeni yüz parçaları, gizli referans görselleri ve sunum nesneleri bulunur.
- Üretilmiş tekir dokunun turuncu/siyah kontrastı kaplan gibi okunduğu için görünür rig tabanı temiz `CH_Cat_Base_Caramel` + `CH_Cat_Marking_Cream` + `CH_Cat_EarInner_Pink` malzeme yuvalarına geçirildi. Eski doku `CH_Cat_WarmBrownTabby_v1` olarak ilerideki desen varyantı için dosyada saklanır.
- `CAT_FACE_RIG_PARTS` altında iki ayrı amber göz, pupil, parıltı, üst/alt kapak kılavuzları, pembe burun, iki ağız yastığı, bıyıklar, hareketli çene pivotu ve ağız içi oluşturuldu. Parçalar gelecekteki Rigify baş kontrolüne bağlanmak üzere `FACE_Root` altında düzenlendi.
- Statik yüz kontrolü: `Temp/CatRigQA/Hyper3D_v3_Face/CatHome_Hyper3D_v3l_FaceReview.png`.
- Çene aç/kapat kareleri: `Temp/CatRigQA/Hyper3D_v3_Face/JawFrames/`; hareketli önizleme: `Temp/CatRigQA/Hyper3D_v3_Face/CatHome_Hyper3D_v3_JawTest.webp`.
- Çene testi 24 fps'te 24 karedir; `FACE_JawPivot` yaklaşık 10 derece açılır, ağız içi yalnız açık aralıkta görünür ve dosya tekrar kapalı nötr kare 1'de bırakılır.
- Rigify, gövde ağırlıkları ve locomotion animasyonları henüz eklenmedi. Yüz görünümü kullanıcı onayı almadan Rigify aşamasına geçilmeyecek.

## Kontrol noktası 07 — Hyper3D temel mesh ve eksen düzeltmesi

- Kullanıcı, `CatHome_CatThreeQuarter_Input_v1.png` görselinin Hyper3D Rodin internet servisine gönderilmesini açıkça onayladı.
- Hyper3D görevi tamamlandı ve dokulu temel mesh Blender'a `CatHome_Cat_Hyper3D_v1` adıyla aktarıldı.
- Güncel Blender kaynak: `ArtSource/Blender/Cat/LowPolyRigify/CatHome_Cat_Hyper3D_v2_Normalized.blend`
- Temel mesh `17.259` köşe ve `23.332` poligondur; tek mesh ve tek `model` malzemesi olarak gelmiştir.
- Önceki manuel kedi, yüz kılavuzları ve eski Rigify referans nesneleri güncel sahneden silindi. Referans görseller, sunum zemini, kamera ve ışıklar korundu.
- Hyper3D nesnesinin Euler değerleri sıfır görünmesine rağmen gizli quaternion dönüşü bütün kediyi eğik tutuyordu. Dönüş sıfırlandı, model Z-up yapıldı ve alt sınırı `Z=0` olacak şekilde zemine yerleştirildi.
- QA görselleri: `Temp/CatRigQA/Hyper3D_v2_Normalized/`. Ana üç çeyrek kontrolü `CatHome_Hyper3D_v2_ThreeQuarter.png` dosyasıdır.
- Henüz Rigify, ağırlık veya animasyon eklenmedi. Sonraki küçük adım ayrı ve animasyona uygun gözler, göz kapakları, kısa ağız-burun hacmi ve çene tasarımıdır.
