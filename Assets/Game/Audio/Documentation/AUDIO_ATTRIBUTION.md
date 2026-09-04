# Super Racing Audio Attribution

## Kenney Interface Sounds

- Author: Kenney
- Source: https://kenney.nl/assets/interface-sounds
- License: Creative Commons CC0 1.0
- Used for: race cues and UI feedback. Imported clips are renamed with `_CHOSEN`.

## Kenney Impact Sounds

- Author: Kenney
- Source: https://kenney.nl/assets/impact-sounds
- License: Creative Commons CC0 1.0
- Used for: vehicle collision, landing, and tire prototype sounds.

## Race GO voice

- Clip: `Go.wav` by owly-bee
- Source: https://freesound.org/people/owly-bee/sounds/415341/
- License: Creative Commons Attribution 4.0
- Imported from Freesound's high-quality preview as `EVT_Race_StartedGo_VOICE_CHOSEN.mp3`.
- Runtime file: `EVT_Race_StartedGo_VOICE_NORMALIZED_CHOSEN.wav`; silence was trimmed and loudness normalized, with no change to the spoken content.

## Realistic vehicle collisions

- Light collision — `qubodup-crash.ogg` by qubodup — https://opengameart.org/content/crash-collision — Creative Commons CC0 1.0. The source page states that only the author's own recordings were used.
- Heavy collision — `dragon-studio-car-crash-sound-effect-376874.mp3` by DRAGON-STUDIO — https://pixabay.com/sound-effects/film-special-effects-car-crash-sound-effect-376874/ — Pixabay Content License.
- Imported as `_REALISTIC_CHOSEN` for vehicle light/medium/heavy collision testing. The older Kenney collision clips remain in the project as rollback candidates but are no longer referenced by `AudioCatalog`.

## Mechanical gear shifts and medium collisions

- Author: BMacZero
- Source: https://opengameart.org/content/mechanical-sounds
- License: Creative Commons CC0 1.0
- Files used: `clank1.wav`, `lightclunk1.wav`, `lightclunk2.wav`, and `mechanical1.wav`.
- Used for: two gear-shift variants and two medium collision variants.

## Vehicle surface recordings

- Asphalt skid — audible-edge/Tom Haigh — https://opengameart.org/content/car-tire-squeal-skid-loop — CC-BY 3.0.
- Asphalt roll — Yaroslav_Novikov — https://opengameart.org/content/car-1 — CC0.
- Sand roll — Peludo — https://opengameart.org/content/water-splash-and-sand-footsteps — CC0. The requested source-page credit is included here.
- Sand skid — Fantozzi (submitted by qubodup) — https://lpc.opengameart.org/content/fantozzis-footsteps-grasssand-stone — CC0.
- Grass roll/skid — Augmentality / Brandon Morris (submitted by HaelDB) — https://opengameart.org/content/random-sounds-samples — CC0 / OGA-BY 3.0; attribution is provided here.
- Used as texture layers for tire roll/skid; runtime pitch, volume, slip threshold, and crossfade are applied by `SurfaceAudioProfile`.

## Racing Car Engine Sound Loops

- Author: domasx2
- Source: https://opengameart.org/content/racing-car-engine-sound-loops
- License: Creative Commons CC0 1.0
- Used for: engine start, idle, drive, and acceleration layers.

## MintoDog racing music

- Author: MintoDog
- Sources: https://opengameart.org/content/hot-roadway, https://opengameart.org/content/racing-game-menu, https://opengameart.org/content/racing-game-result
- License: Creative Commons CC0 1.0
- Used for: race, menu and result music.

## Environmental ambience

- Beach Ocean Waves — jasinski/qubodup — https://opengameart.org/content/beach-ocean-waves — CC0.
- Wind — IgnasD — https://opengameart.org/content/wind — CC0.
- Short Wind Sound — remaxim — https://opengameart.org/content/short-wind-sound — CC0.
- Used for: Beach waves/wind and Desert wind/gust.

## Prototype archive

The old project-generated ambience/music is retained under `Clips/Prototype` for comparison only. No catalog or map profile references it.

No CC-BY-NC content or audio extracted from another game is included.
# Backfire

- `EVT_Vehicle_Backfire_REAL_CHOSEN.mp3`
- Source: “BACKFIRE.ogg” by CeebFrack — https://freesound.org/people/CeebFrack/sounds/105351/
- License: CC0 1.0
- Imported from the official Freesound high-quality preview and used as an exhaust-release one-shot.
