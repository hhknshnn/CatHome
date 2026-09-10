# TV eylem mesafesi ve duvara dayalı ana yatak

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

7 Eylül 2026, kullanıcının yeni geri bildirimi üzerine.

## TV

Seçilmiş ürün `FindNearestCandidate` içinde mesafe kontrolünü atlıyordu. TV'ye bir kez dokunulduktan sonra kedi uzaklaşsa bile "İzle" ve seçim çerçevesi kalabiliyordu.

Seçilmiş ürün, yakındaki adayın korunması ve düğmeye basılması artık aynı sahne + gerçek etkileşim yarıçapını denetler. Mesafe dışındaki seçim temizlenir. Düğmenin son çizildiği kareden sonra uzaklaşılmışsa tıklama da aktiviteyi başlatamaz veya enerji harcayamaz. Ürün etiketi artık katalogdaki çevrilmiş başlığı kullanır; `ModernTelevision` gibi iç adlar gösterilmez.

## Yeni ana yatak

Önceki sert çanak yerine mint minderli, krem döşemeli küçük bir kedi kanepesi üretildi. Yuvarlak yan destekler, yumuşak sırt panelleri, minder dikişi, ince altın alt kenar, küçük ayaklar ve pati detayı içerir. Gövde yaklaşık 1.10 × .80 m ve .438 m yüksektir; kediler yeniden ölçeklenmez.

Kaynak `ArtSource/Blender/PremiumFurniture/build_main_cat_bed.py`; yalnız headless Blender 5.2 kullanıldı. Toplu `build_reference_living.py` de aynı kaynağı çağırır. Son model `Assets/Art/PremiumFurniture/Models/MainCatBed_Premium.fbx`.

Yatak `(-.60,0,2.33)` konumunda. Görünen arka yüz duvar kaplamasına z=2.72'de oturur. Fizik engeli yapısal duvara z=2.80'e kadar devam eder: önünden elle yatağın içine girilmez, yanından arkasına dolanılamaz. Uyku komutu minderi kullanır; uyanış önü z≈1.416'daki açık noktadır. Minderin .242 m üst yüzeyi gerçek mesh üçgenlerinden ölçülür.

`CatBedObstacle` eski uyanık kayıtta yeni yatağa veya arka köşeye çakışan konumu yatağın önüne düzeltir. Geçerli kayıt konumunu değiştirmez. Uyku kaydı ayrı uyku akışıyla mindere döner; koleksiyon düzenlemesi uyuyan kediyi dışarı atmaz. Ana kayıt sürümü ve sahiplik verisi değişmedi. Canlı QA yalnız `UiQaTestSession` kayıt kopyasında yapıldı.

## Doğrulama

Kanıt klasörü: `Docs/QA/TV_BED_2026-09-07`.

- `PlayMode.xml`: 6/6 hedefli test. Uzak/yakın TV seçimi, tıklama anında mesafe, eski kayıt konumundan analogla ayrılma, yatak ön/arka giriş engeli, uyku kaydı ve uyanış, on ırkın minder teması, bakım erişimi ve ürün seçme ışını.
- `main-bed-contact.csv`: on ırkta görünür gövde minderden 5 mm yukarıda; en iri gövde yatayda yaklaşık ±.361/±.147 m, yeni minderin kullanılabilir sınırlarında.
- `EditMode.xml`: 442/442 geçti; 4.147 izinli CAT birleşimi dahil. Unity'nin doğrudan test raporu tamamlandı; MCP iş izleyicisi bu koşuda başlangıç callback'ini kaçırıp zaman aşımı verdi. `test-runner-state.txt` yerel test çalışmasının sona erdiğini doğrular.
- `level-validation.txt`: 0 hata, 0 uyarı.
- `art/`: Blender gerçek modelinin ön, perspektif ve silüet görüntüleri.
- Oda seçimi ve HOME mağaza salon fotoğrafı yeni modelle yeniden üretildi ve incelendi. Ana bakım yatağı ayrı satılan bir ürün olmadığı için diğer ürün fotoğrafları değişmedi.
- `01-new-bed-far-tv.png`, `02-tv-near.png`, `03-new-bed-sleeping.png`: gerçek 1920×1080 oyun kareleri. Kayıt kopyası yatağın önünde açıldı; TV 2.66 m uzaktayken seçim temizlendi, yakınında çevrilmiş başlıkla eylem göründü. HUD taramalarında çakışma/taşma yok. Son durumda QA kapalı, üç ana sahne açık, LivingRoom_Level01 aktif ve tek etkin kamera var.

Önceki `ROOM_COLLISION_FIXES_2026-09-07.md` içindeki yatağın arkasını açık bırakma çözümü, kullanıcının son kararıyla geçersizdir. İkili koltuğun .76 ölçeği ve komutla iki yönlü tünel davranışı korunur.
