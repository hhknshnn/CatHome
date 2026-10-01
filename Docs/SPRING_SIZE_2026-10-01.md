# Spring toy size-aware polish — 1 October 2026

Started 13:13:15 UTC; hard stop 13:33:15 UTC. Spring toy only.

CatSpringGeometry measures the live rig, source skinned body/head vertices, shoulder height, two-segment foreleg reach, paw size and root scale. Stand distance and target height are normalized to these measurements; contact uses actual moving mesh triangles. No breed-specific placement constants. CatSpringToyMotion retains existing ToyBatLeft/ToyBatRight/ToySniff rotations while preserving each breed's native bone translations, grounding its visual and applying small support/shoulder weight transfer. Existing natural root turn is unchanged. No Blender or new rig.

Evidence: Docs/QA/SPRING_SIZE_2026-10-01. native3.xml is the earlier three-size pass; final.xml records two failed old grid searches, not a successful all-breed acceptance. final-three.xml is the final normalized-placement test. Read its actual result for final acceptance. results.txt contains historical and final measurements in chronological order.

Video: CatHome_Yayli_Oyuncak_3_Boyut.mp4, native Unity Game View captured through ScreenCapture and MediaEncoder at 24 FPS. Intended sequence: Persian (small), Domestic Shorthair (medium), Maine Coon (large), four seconds each. Labels and close camera are QA-only. Each full interaction is tested separately beyond the captured excerpt. QA uses isolated player data. Head clearance checks sample actual skinned head vertices; this is not an exhaustive mesh-intersection guarantee. All-breed validation remains incomplete.

No APK, device deployment, commit or publishing. Preservation and editor-final files contain closure status.
