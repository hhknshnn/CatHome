# Cat Home Fredoka

Upstream: [Google Fonts Fredoka](https://github.com/google/fonts/tree/main/ofl/fredoka), `Fredoka[wdth,wght].ttf`, downloaded 6 September 2026. Copyright and SIL Open Font License are retained in `Fredoka-OFL.txt` and `Assets/Fonts/Fredoka-OFL.txt`.

The upstream font contains breve, cedilla and dotaccent geometry but lacks the Turkish Gbreve/gbreve, Scedilla/scedilla and Idotaccent Unicode mappings. TMP's fallback made these letters visibly thinner than the rest of a word. `build_fredoka_tr.py` composes the missing characters from the same font's base letters and accents, centers them from real glyph bounds, and emits static width=100, weight=500/600 instances. The modified family is named **Cat Home Fredoka**. No foreign glyph, synthesized bold or stretch is used.

Run the script with Python and fontTools. Its two outputs keep the existing Unity source paths so references survive. After regenerating them, reimport the TTF files, clear/repopulate the `FredokaDisplay` and `FredokaEmphasis` TMP atlas assets through `ClearFontAssetData` / `TryAddCharacters`, then save assets. `PremiumPresentationTests.TurkishHeadings_UseTheirOwnGlyphsWithoutFallback` verifies the full Turkish alphabet with fallback search disabled.
