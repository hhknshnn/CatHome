# Cat Home Fredoka

<!-- ROOM_LAYOUT_2026_09_07 -->
> **7 Eylül 2026 yerleşim güncellemesi:** Diğer yedi oda kendi 70 ROOM ürününü ortak kamera, sabit alan, oranlı ölçek ve açık etkileşim girişleriyle kullanır; CAT koleksiyonu salonda kalır. Güncel uygulama, yeni oda ekleme sözleşmesi ve doğrulama durumu [ortak oda yerleşimi raporundadır](../../Docs/ROOM_LAYOUT_IMPLEMENTATION_2026-09-07.md). Bu belgedeki önceki koordinat/ölçek/kamera kararları yeni raporla çelişirse güncel rapor geçerlidir; tarihsel test sonuçları kendi çalışmasına aittir.
<!-- /ROOM_LAYOUT_2026_09_07 -->

Upstream: [Google Fonts Fredoka](https://github.com/google/fonts/tree/main/ofl/fredoka), `Fredoka[wdth,wght].ttf`, downloaded 6 September 2026. Copyright and SIL Open Font License are retained in `Fredoka-OFL.txt` and `Assets/Fonts/Fredoka-OFL.txt`.

The upstream font contains breve, cedilla and dotaccent geometry but lacks the Turkish Gbreve/gbreve, Scedilla/scedilla and Idotaccent Unicode mappings. TMP's fallback made these letters visibly thinner than the rest of a word. `build_fredoka_tr.py` composes the missing characters from the same font's base letters and accents, centers them from real glyph bounds, and emits static width=100, weight=500/600 instances. The modified family is named **Cat Home Fredoka**. No foreign glyph, synthesized bold or stretch is used.

Run the script with Python and fontTools. Its two outputs keep the existing Unity source paths so references survive. After regenerating them, reimport the TTF files, clear/repopulate the `FredokaDisplay` and `FredokaEmphasis` TMP atlas assets through `ClearFontAssetData` / `TryAddCharacters`, then save assets. `PremiumPresentationTests.TurkishHeadings_UseTheirOwnGlyphsWithoutFallback` verifies the full Turkish alphabet with fallback search disabled.
