# Top HUD — selected cat and baked sparkle, 29 September 2026

The HUD portrait again shows the currently selected cat's original catalog portrait. StorybookPortraitCrop no longer substitutes the artificial rounded portraits. The existing SelectedCatPortrait selection service and binding remain unchanged; the original circular crop remains.

The cat token was rebuilt with a cut champagne-gold perimeter and embossed face. The diamond was rebuilt with distinct bright crown and deep-blue pavilion facets, tapered seams and localized glints. Small glint cores, transparent emission halos and restrained compositor glow are baked into PNGs. Panel body saturation is retained. Twenty top-HUD PNGs were rendered, including icon wells, surface accents, gold frames and fills. Unity adds no new shader effects.

Blender source: `ArtSource/Blender/TopHudSparkle/Top-HUD-Sparkle.blend` at the workspace root. Reproduction scripts and README are adjacent. Final sprites: `Assets/Resources/TopHudExact/` in the Unity project. Unused artificial portrait files are retained but no longer substituted into the HUD.

Evidence: `Docs/QA/TOP_HUD_SPARKLE_2026-09-29/`. Actual 1920×1080 Edit Mode Game View, top comparison, full side-by-side comparison and six detail crops. Reference and final were opened side by side in Blender. The final correction was checked again in Unity at actual HUD size.

Validation: all 20 sprites imported with one preserved sprite subasset each; 99 GUID/spriteID/internalID checks match. All 71 active top-HUD rectangles and 4,072 common scene rectangles match. Seven transient dock rectangles were recreated with identical geometry. Common 274 buttons and 1,350 text items match. Selected portrait checks passed for ten catalog entries and the currently selected Persian cat. These are binding checks without a new PlayMode gameplay session. Console has no errors; eight existing obsolete-API warnings remain.

Current-start saved games and scenes are unchanged. The only changed runtime C# file is the portrait presentation component. The audit scope is recorded in preservation-final.json and is not a whole-project audit. Three clean normal scenes; Play and compilation stopped, Unity open. No APK, commit, push or publication.

Remaining differences: the reference token has rounder cheeks and softer facial modeling; its diamond pavilion is brighter. The selected cat portrait retains the actual game's low-poly rendering, as requested, instead of artificial replacement artwork. Exact reference equivalence is not claimed. No further art pass is started.

Turn started 14:03:49 UTC. Exact closure time and duration are in QA closure.json.
