# Oyun içi HUD referansı — 22 Eylül 2026

Yerleşik ImageGen ile tek görsel tasarım referansı. Başlangıç 12:02:14 UTC; tahmin 5–10 dakika, bu tur genel üst sınır 30 dakika. Kullanıcının “devam et” yanıtı önce referans hazırlama adımı içindir; Unity uygulamasına görselden sonra geçilir. Bu dosya oyun kaynağı değildir.

Sonuç: `07-hud-reference.png`. Yerleşik araç, dört yerel referansla tek üretim. Ana menüyle ortak indigo/mercan/krem yüzeyler, modern kedi simgesi, üç ihtiyaç, ayrı bağ puanı ve mevcut gezinme/bağlamsal eylem. Pembe 160 Bond XP/bağ puanıdır, üçüncü para birimi değildir. Uzun metin ve düşük enerji durumunda gerçek buton iki satıra sığmalıdır. Gerçek HomeWorldViewport alt navigasyon için 80 birim ayırır; uygulamada dünya kamerası ve oda alanı korunmalıdır. Taslakta yüzen alt grup bu teknik sınırın test edildiği anlamına gelmez. Referans görsel incelendi; kod, Unity durumu ve gerçek kayıtlar değiştirilmedi.

Kaynaklar: 18 Eylül gerçek oyun ekranı (`Docs/QA/INTERACTION_RECOVERY_5H_2026-09-18/manual-final-gameplay.png`), uygulanmış ana menü (`Docs/QA/MAIN_MENU_STORYBOOK_2026-09-22/screens-final/main-menu-final.png`), onaylı `05-modern-cat-icon.png`, `04-icon-study.png` içinden çanta/koltuk/kumanda. Eski patili altın jeton kullanılmaz. Pembe pati kaynağı ayrı oyun kaynağı olarak korunur.

İstem:

Use case: ui-mockup, compositing.
Create ONE polished landscape 16:9 reference mockup for the in-game HUD of CAT HOME. This is a UI DESIGN PROPOSAL, not an implemented Unity screenshot.
INPUT 1 is the edit target, an actual gameplay screenshot. Preserve its room, floor, furniture geometry and placements, camera, shadows, lighting, wall colours, and the real low-poly dark cat at the lower right. Do NOT beautify or rebuild the environment or cat. Replace ONLY the screen-space interface. The open central gameplay area must stay open.
INPUT 2 is the approved implemented main menu, a UI STYLE reference only: use its deep indigo surfaces, cream rounded typography, mint accents, restrained bevels, short dark bottom edges and coral action button. Do NOT copy the title screen composition, large left panel, logo, three cats, or room.
INPUT 3 is the approved modern cream-and-mint cat-head icon. Use this as a small emblem in the coin balance and cat-command navigation icon; no golden paw coin medallions.
INPUT 4 is an icon sheet: use the teal shopping bag (top left), coral sofa (top right), and cream gamepad (bottom right) as supporting icons. Ignore the gold paw coin entirely.

Design target: premium colourful storybook casual mobile game HUD with crisp, practical, consistent controls, at the same finish level as the main menu. No white generic rectangular cards, no photographic gloss, no excessive gold trim, glitter, particle effects or neon. UI highlights from upper left, one subtle rim, a 5-7 pixel lower sidewall, soft tight shadow, deliberate spacing. All typography clear and rounded, correct Turkish letters.

LAYOUT:
Top left, inset about 28 px: one integrated indigo cat identity and needs cluster, about half of the screen width, not many unrelated white cards. At far left show the CURRENT DARK CAT portrait from input1 in a round mint ring, then exact text "lokiş" and "Seviye 30". To its right arrange three slim integrated need sections with a sculpted food bowl, aqua water drop, lavender moon; exact labels "Tokluk", "Su", "Enerji". Each says "0%" with an EMPTY dark recessed meter, because these are the state values in input1. Do not show filled meters at 0%.
Below the profile's left edge retain a small rose resource pill with its existing pink paw-token symbol, exact value "160", and compact mint plus button. It is a separate resource, not coins.
Top right retain two compact cream/indigo currency pills, first with the approved cream cat-head emblem and "5.964", second with a faceted turquoise-blue diamond and "2". Keep distinct small plus buttons for both. Far right a rounded indigo menu button with three cream horizontal lines. Leave adequate gutters, no overlapping text.
Bottom left: preserve the joystick location and approximate footprint; replace its flat white/blue look with a softly dimensional indigo rim, recessed mint inner disc and a simple central knob with four small direction ticks. No coin decoration.
Bottom centre: one floating indigo rounded dock with four generous readable navigation buttons. Exact labels "Mağaza", "Oda", "Kedi komutları", "Oyunlar". Shopping bag / coral sofa / approved cat-head / cream controller icons respectively. The room tab uses the coral active surface. Under "Oda" show smaller "Salon · 10/10". Ensure the longer Kedi komutları label fits cleanly, on two short lines if needed. Dock should be substantial yet occupy less than 15 percent of screen height; it must not cover the lower-right cat.
At lower right, ABOVE the cat and to the right of the dock, demonstrate the existing contextual action: a single coral rounded button with a small mint tunnel icon and exact label "Tünelden geç". This is the visual example of the existing context-sensitive button, not a new permanent feature; no extra action buttons or price.
Keep the centre and most of the room clear. Use safe margins around all edges. No phone frame, no web page, no split screen, no large branding or marketing captions. Tiny unobtrusive top-centre annotation "ARAYÜZ TASLAĞI".
Output one beautiful cohesive interface concept, sharp at full resolution. Repeat invariants: keep the screenshot's existing room and real cat visually unchanged; only restyle the HUD.
