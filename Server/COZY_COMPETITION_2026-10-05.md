# Four-game competition contract — 2026-10-05

The client now exposes four game tabs. Cloud Code source adds SubmitCozyScore and reward-board support for yarn-route and pond-play. No service deployment or remote board creation was performed in this task.

Before enabling worldwide results for the new games, deploy the updated CatHomeCompetition module and provision these boards using the existing Runner/Catch configuration conventions:

| Board | Ordering/update | Period |
| --- | --- | --- |
| yarn-route-daily | descending / best score | daily, 00:00 UTC |
| yarn-route-weekly | descending / best score | Monday, 00:00 UTC |
| yarn-route-all-time | descending / best score | no reset |
| pond-play-daily | descending / best score | daily, 00:00 UTC |
| pond-play-weekly | descending / best score | Monday, 00:00 UTC |
| pond-play-all-time | descending / best score | no reset |

Retain the existing deny-direct-player-writes policy. Score submissions use the Cloud Code module and nickname metadata. Missing/offline boards fall back to the device's actual dated personal best, explicitly labelled as personal results.

Yarn canonical score: for each of the 24 solved levels, 100 + zero-based level * 25 + stars * 25 + (pearl ? 25 : 0). Stars must be 0..3. A continued attempt appends "-c" to its original 32-character hexadecimal run ID and submits the cumulative score to a best-score board.

Pond canonical score: basePoints + comboSteps * 25 + perfect * 50 + bonus * 75. The module validates catch-count and score-envelope bounds. These are metric plausibility checks, not a server-authoritative replay or proof against a modified client.

Local QA compiled the full module against the already cached Unity SDK packages (0 errors/0 warnings) and ran seven positive/rejection score-contract checks. Live authentication, board provisioning, rewards and deployment remain unverified.

