# Ana menü referansı — 22 Eylül 2026

Durum: yalnız referans görseli. Kullanıcı açıkça uygulamadan önce bu görsel için onay istedi; henüz bu ana menü onayı alınmadı. Unity kodu, sahnesi, görsel varlıkları, oyun kayıtları veya ayarları değiştirilmedi.

Çıktı: [06-main-menu-reference.png](06-main-menu-reference.png). Yerleşik ImageGen kullanıldı, CLI/API yolu kullanılmadı.

Kapsam yalnız UI tasarımı ve UI animasyonlarıdır. Kedi hareketleri, modelleri ve oda/dünya görünümü kapsam dışıdır. Krem/turkuaz kedi UI simgesi kullanıcı tarafından onaylandı. Önceki altın patili jeton reddedildi.

Bu, yatay telefon görünümünde mevcut ilerlemesi olan oyuncunun ana menüsüdür. Seviye 30 temsilîdir; gerçek kayıttan okunmalıdır. Yeni oyuncuda ana eylem OYNA olur, Yeni oyun koşullu görünür. Çıkış telefonda gizlidir; masaüstü işlevi korunur. Üç kısayol Kedim, Odalar, Oyunlar; mevcut erişim koşulları korunur. Cüzdan veya yeni menü işlevi eklenmez.

Görsel AI tarafından hazırlanmış tasarım referansıdır, Unity ekran görüntüsü değildir. Mevcut başlık sahnesi referans alınmıştır; üretimde arka planın çiziminde oluşmuş küçük farklar uygulama talimatı sayılmaz. Yalnız arayüz uygulanacaktır. Sonraki adım, kullanıcı bu referansı onaylarsa yalnız ana menüyü uygulamak ve gerçek süreyi ölçmektir.

## Üretim komutu

```text
Use case: ui-mockup.
Asset type: ONE high-fidelity landscape mobile game MAIN MENU visual reference for Cat Home, 16:9, full bleed, no device frame, no presentation board or surrounding captions.
Input image 1 is the EDIT TARGET: an actual existing Cat Home Unity title screenshot.
Input image 2 is the approved UI STYLE reference only: coral primary button, indigo secondary surfaces, clean dimensional edges, soft upper-left highlights, refined tactile buttons.
Input image 3 is the approved CAT HEAD UI ICON; use this exact cream-and-mint cat-head design for UI identity and the main CTA. It is only a UI symbol, not a replacement for any in-world cat.

Primary request: redesign ONLY the main-menu UI overlay in image 1 to look cohesive, premium, vibrant, playful and modern, based on image 2. Preserve the original visible 3D background from image 1: same three low-poly cats with their exact forms, proportions, coat colors, poses and positions; same arched window, floor, turquoise rug, props, camera framing and room lighting. The user is approving UI only. Do not beautify, round off, remodel, restage or illustrate the 3D cats or room. Keep the original game rendering recognizable and unchanged underneath the redesigned UI.

UI composition:
- Left approximately 36%: a refined deep-indigo to muted-teal opaque reading surface with gently curved right edge, controlled depth, very subtle texture, clean safe margins. It smoothly meets the original room without covering the foreground orange cat. No white washed-out reading veil.
- Top left: small approved cream/mint cat-head icon next to a confident, rounded custom-looking CAT HOME wordmark, cream with restrained warm coral/mint accents. Clean, legible letter shapes, no coin.
- Below the wordmark, a quiet small line reading exactly "Kedin seni bekliyor".
- Clear spacious hero heading exactly "Yuvana hoş geldin." on two lines, soft cream, friendly rounded type, balanced size. Below it, smaller readable text exactly "Birlikte güzel bir gün daha."
- Under that, one large horizontal coral primary button with the approved small cat-head icon on the left, exact centered text "Devam et", and simple ivory right arrow. Match image 2's well-made beveled shape: warm coral face, restrained top highlight, darker short side wall and contact shadow. Elegant controlled depth, no ornate borders, no gold.
- Under the CTA, a shorter indigo secondary button, exact text "Yeni oyun", small simple curved-arrow icon.
- Near bottom of left section, a small quiet indigo/teal information pill reading exactly "Yuva seviyesi 30". This is sample saved-progress content. Keep abundant breathing room; remove the old extra 3-column explanatory strip to avoid duplication.
- Top right over the unchanged room: two small well-aligned indigo utility buttons "Ayarlar" and "Yapımcılar", refined simple gear and information/person icons, cream labels, same surface family. This is a mobile reference, so do not show Exit/Çıkış.
- Bottom right over the floor, below the cats and never covering their bodies: one compact indigo navigation dock holding exactly three generous equal shortcuts, with refined dimensional icons above readable cream labels: the approved cream/mint cat head with "Kedim", a coral sofa with "Odalar", and an ivory/teal game controller with "Oyunlar". Match the approved component style in image 2. All shortcuts are idle indigo; primary coral emphasis belongs to Devam et. No store/shopping shortcut.
- Align all baselines, margins and corner radii carefully. Typography must be crisp and coherent, Turkish diacritics correct, UI controls convincingly implementable in Unity. Respect phone safe margins at least 4% left/right; no clipping.

Constraints: preserve all three actual game cats and background scene; UI-only redesign. No paw prints, no paw coins, no gold coins, no wallet counters, no gems, no currencies, no shop offers, no additional cats or furniture, no new gameplay features, no star confetti, no lens flare, no global bloom. Do not copy the technical preview caption from image 2. No watermarks. Output a single polished finished proposed menu screen, not a before/after grid.
```
