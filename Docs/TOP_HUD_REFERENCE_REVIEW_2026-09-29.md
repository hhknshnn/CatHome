# Top HUD individual reference review

Follow-up start 2026-09-29 15:29:21 UTC. Final visual capture approximately 15:58 UTC. Art stopped at the 30-minute limit. User visual approval and full AAA reference parity are not claimed.

20 PNGs rebuilt/refined in Blender: profile, currency, portrait-ring, menu, plus, panel-food/water/energy, well-food/water/energy, fill-food/water/energy, food, water, energy, coin, badge, diamond. Corresponding sprite rectangles follow rendered alpha bounds; original GUIDs and sprite identities remain unchanged. Geometry, lighting and surface gradients are baked in transparent PNGs. Unity used for import and visual validation.

Source: `ArtSource/Blender/TopHudReferenceReview/Reference-Review.blend` at workspace root. Sprites: `Assets/Resources/TopHudExact`. Evidence: `Docs/QA/TOP_HUD_REFERENCE_REVIEW_2026-09-29`.

Individual review:

| Area | Result and remaining difference |
| --- | --- |
| Profile and gold | Clean continuous thin border; metal separation weaker than reference. Existing selected cat portrait remains low-poly. |
| Food | Ceramic body, ivory lip and individual kibble; food detail softer than reference at display size. |
| Water | Pointed volume, bright cyan body and modeled reflections; reflection shape differs. |
| Energy | Smooth pearl crescent and gold star; star remains smaller and crescent more upright than reference. |
| Cat token | Rounded raised gold head, copper ears and clear face; no longer buried in background. Less expressive than reference. |
| Small badge | Three reference rays and raised central pad; field remains darker and relief less nuanced. |
| Diamond | Solid depth and controlled broad facets; still too graphic/flat relative to reference. |
| Currency gold | Thin continuous clean border; weaker gold highlight than reference. |
| Menu and plus | Polished blue/green surfaces and narrow gold edge; specular shaping differs. |

Actual Unity 1920×1080 Edit Mode Game View: `final-game-view.png`. Full top comparison: `reference-vs-final-top.png`. Individual comparison: `nine-item-final-comparison.png`. Reference is HUD-Ref.png; crops only resize actual images, no repainting of evidence.

Current-start preservation: saved games, runtime C# scripts, scene files, 4079 layout records, button/text binding snapshots and 99 sprite identity comparisons unchanged. Scope is recorded in preservation-final.json; this is not a whole-project hash audit. Three scenes remain clean, Play and compilation off, Unity open. No APK, phone testing, commit, push or publication. Existing 8 deprecated API warnings; no observed console errors.

Acceptance remains incomplete because diamond, badge and gold material response do not yet equal the reference. No additional work starts automatically after this timebox.
