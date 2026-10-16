# Lab 2 — “Snow Boarder” (PRU213)

**Student:** QE190149
**Project:** `LAB2_PRU213_QE190149` (Unity 6000.6.0f1, 2D URP)
**Genre:** Sports / Arcade — 2D side-scrolling snowboarding

---

## 1. Overview

*Snow Boarder* is a 2D downhill snowboarding arcade game built around Unity's 2D
physics. The player rides a pre-built mountain slope, steers through an obstacle
course, collects snowflakes, performs airborne tricks and tries to reach the
finish line with the highest score.

The game is made of three scenes:

| Build index | Scene | Purpose |
|---|---|---|
| 0 | `Assets/Scenes/MainMenu.unity` | Main menu (Start / Options / Quit) |
| 1 | `Assets/Scenes/Level1.unity`  | The downhill course + in-game HUD |
| 2 | `Assets/Scenes/EndGame.unity` | End screen (level complete / game over) |

### Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Steer left / right | `←` / `→` (or `A` / `D`) | Left stick |
| Jump (hop over obstacles) | `Space`, `↑` or `W` | A / south button |
| Tuck & boost | `Left Shift` | Right trigger |
| Brake | `↓` or `S` | Left trigger |
| UI navigation | Mouse | — |

---

## 2. Feature checklist (against the brief)

### Snowboarding Adventure — 20 pts
* `PlayerController.cs` steers with **torque** (`Rigidbody2D.AddTorque`) so turning
  feels responsive and physical rather than teleported.
* **Speed control**: holding Shift / right trigger raises the slope's surface speed
  from `baseSpeed` (20) to `boostSpeed` (40); holding Down / S brakes to `brakeSpeed`.
* **Jumping**: `Space` / `↑` / `W` gives an upward impulse while the board is on the
  snow, so the player can hop over obstacles and use the air time for a trick.

### Unity's Physics Engine — Slope Physics — 15 pts
* The mountain is a **Sprite Shape** with an `EdgeCollider2D` driven by a
  **`SurfaceEffector2D`** (`speed = 20`, `forceScale = 0.1`, friction enabled). This
  is what actually accelerates the boarder downhill.
* The boarder is a dynamic `Rigidbody2D` (`gravityScale = 2`, linear drag `0.5`,
  angular drag `15`, continuous collision detection). Gravity, momentum, friction
  and the slope inclination therefore all come from Unity's physics engine; the
  code only adds torque and speed changes.
* The physics material, drag and gravity are tuned per **difficulty**
  (Easy / Normal / Hard) — see `GameSettings.cs`.

### Obstacle Course and Challenges — 20 pts
* The level is procedurally dressed by `Assets/Editor/Lab2SceneBuilder.cs`, which
  reconstructs the slope surface from the level's collider and places content on it
  along the whole mountain. The supplied mountain is **stretched horizontally and
  squashed vertically**, which makes the course about **twice as long** (~400 units)
  and turns the steep drops into gentle rolling hills. A typical run has ~40
  snowflakes, 6 power-ups, **6 obstacles** and 6 jump ramps, and the first stretch is
  deliberately clear of obstacles.
* The finish is marked with a **black-and-white checkered banner** on two posts.
* **Obstacle rules**: *trees* are small (≈3.5 units) and fatal — they must be
  **jumped over**; *ice patches* are pass-through triggers that scrub the speed, break
  the combo and blink the boarder, so the board can never get wedged on them.
* **Mountain**: the supplied slope is squashed vertically by ~25 % so the course reads
  as gentle rolling hills instead of steep drops.
* **Steering**: the rotation is driven by the angular velocity directly (with an
  acceleration limit), which is completely smooth and immediate — on the snow A/D
  (or ←/→) **spins** the board at up to 600 °/s and releasing self-rights it to the
  slope; in the air the same keys spin at up to 800 °/s, so a 360° trick takes about
  half a second.
  *Every one of these numbers is exposed on the `PlayerController` component, so it
  can be tuned live in the inspector while playing.*
* **Jump feel**: a strong impulse (about 8 units of height) and a buffered jump (a
  press slightly before landing still counts) with coyote time, so the input is never
  dropped.
* **Feedback**: hitting an obstacle only makes the boarder **blink red**; the crash
  particle burst is deliberately small so it never covers the screen.
* **Challenges**: collect the snowflakes (some are lifted into arcs so they must be
  jumped for), land 180/360/540/720° tricks, and keep the combo alive.

### User Interface Design — 10 pts
* **Main menu** with **Start**, **Options** and **Quit**, a title, the best score
  and a help line. The options panel offers difficulty selection and a volume
  slider.
* **In-game HUD**: score, combo timer, multiplier, speed (number + bar), lives,
  elapsed time, snowflake/trick counters and a transient “banner” that announces
  tricks and power-ups.

### Handling Collisions and Triggers — 15 pts
* `CrashDetector.cs` uses `OnCollisionEnter2D` / `OnTriggerEnter2D` and reacts to
  anything carrying an `Obstacle` component:
  * **Fatal obstacle (tree)** → the crash effect and the crash SFX play, the boarder
    **blinks red** (the “you lost health” feedback) while a life is removed and the
    level restarts.
  * **Soft obstacle (ice patch)** → keeps 45 % of the speed, breaks the combo and
    blinks the boarder briefly.
  * **Landing on the head** → if the board is rotated past ±110° when it touches the
    snow (a wipeout), a life is lost just like hitting a tree.
* The obstacle itself is **never destroyed**: while invincible it merely lets the board
  through for a moment and then comes back, and collected snowflakes / power-ups
  **respawn** after a few seconds so the course never empties out.
* `Collectible.cs` and `PowerUp.cs` are **triggers**: snowflakes give points and
  extend the combo; power-ups give a **speed boost**, **invincibility** (obstacles
  shatter instead of hurting) or an **extra life**.
* `FinishLine.cs` is a trigger that plays the finish effect/sound, awards a finish
  bonus and completes the level.

### Game Logic and Scoring Systems — 20 pts
`ScoreManager.cs` owns the whole scoring model:

| Source | Points |
|---|---|
| Snowflake | 50 × multiplier |
| Trick 180° / 360° / 540° / 720° | 100 / 250 / 350 / 600 × multiplier |
| Speed | small trickle, `0.35 pts` per second per unit of speed |
| Finish bonus | 500 × multiplier |

* **Combo system**: every successful move increments the combo; the combo has a
  3.5 s window. If nothing happens in that window (or the player crashes), the
  combo is broken.
* **Score multiplier**: `multiplier = clamp(1 + combo / 3, 1, 5)` — chaining moves
  up to x5.
* The score manager is a persistent singleton, so a crash + restart keeps the
  score (lives), while starting a new run from the menu resets everything.

### Visuals and Aesthetics
* Crisp 2D winter sprites supplied with the lab (snow tiles, trees, rocks, clouds,
  boarder split into top/bottom for the tilt animation).
* A **winter sky gradient** with **distant hill silhouettes** drawn behind the level,
  and the existing clouds get a **parallax** offset so the scenery has depth.
* A designed **HUD**: translucent top / bottom bars, outlined text, a
  colour-coded combo multiplier, heart-shaped lives and a speed meter bar.
* **Dynamic weather**: `SnowfallController.cs` keeps a snow particle system around
  the camera and, every 6–14 s, smoothly changes the snowfall density and the
  “visibility” (camera background fades from clear sky to storm white).
* Speed-reactive snow spray from the board (`DustTrail.cs`) and crash/finish
  particle effects.
* The invincibility power-up makes the boarder **blink** so the state is readable.

---

## 3. Difficulty variants

The level exists in **three versions**, all generated from one master scene by
`Lab2SceneBuilder.BuildLevelVariants()`; the main menu selection decides which one
is loaded:

| Difficulty | Scene | Course | Terrain | Obstacles | Ramps | Speed |
|---|---|---|---|---|---|---|
| **Easy** | `Level1_Easy` | ≈405 units, ends *before* the big drop | stretched ×2.15, squashed to 0.5 (very flat) | 4 (1 tree) | 4 | ×0.8 |
| **Normal** | `Level1_Normal` | ≈600 units, includes the hills | ×2.3, 0.85 | 8 (2 trees) | 6 | ×1.0 |
| **Hard** | `Level1_Hard` | ≈635 units, includes the big drop | ×2.3, 1.15 (steep) | 15 (5 trees) | 8 | ×1.3 |

All three keep the **first 90–120 units clear** so the start is never unfair, and the
speed multiplier is applied on top of the boarder's inspector tuning (so the designer's
values are respected).

The camera is pulled back (`orthographicSize = 12.5`) so more of the track is visible.

---

## 4. Implemented scenes

### MainMenu
A dark “mountain arcade” title screen: a navy-to-steel sky gradient, a dark mountain
silhouette with snowy trees, the boarder posing on the snow slope, floating
snowflakes, the big italic **SNOW / BOARDER** logo with a cyan rule and the tagline
*“DROP IN. RIDE YOUR LINE.”*, the **BEST RUN** score in the corner and rounded
buttons: **PLAY >** (orange), **HOW TO PLAY**, **OPTIONS** and **QUIT**. The options
panel selects Easy / Normal / Hard (changing base speed, boost speed and gravity) and
the master volume; the how-to-play panel lists all the controls.

### Level1
The supplied downhill scene is kept as-is (terrain, camera rig, boarder “Barry”,
finish line) and enriched with:
* a HUD canvas,
* a `Gameplay` root containing the generated snowflakes, power-ups, obstacles and
  ramps,
* a `Weather` object with the snow particle system,
* `Game Manager` / `Score Manager` objects,
* a `TrickController` on the boarder.

### EndGame
Shows whether the run was completed, the final score, snowflakes, tricks, best
combo and the all-time best score, with Retry / Main Menu / Quit — in the same dark
“mountain arcade” theme as the menu.

---

## 4. Script map

| File | Responsibility |
|---|---|
| `Scripts/PlayerController.cs` | Steering torque, speed control, boost |
| `Scripts/CrashDetector.cs` | Collision reactions, crash sequence |
| `Scripts/DustTrail.cs` | Speed-based snow spray |
| `Scripts/FinishLine.cs` | Trigger that completes the level |
| `Scripts/Collectible.cs` | Snowflake pickups |
| `Scripts/PowerUp.cs` | Speed boost / invincibility / extra life |
| `Scripts/Ramp.cs` | Jump ramps (trigger launch) |
| `Scripts/Obstacle.cs` | Fatal vs. soft obstacles |
| `Scripts/TrickController.cs` | Airborne rotation detection, tricks |
| `Scripts/ScoreManager.cs` | Score, combo, multiplier, statistics |
| `Scripts/GameManager.cs` | Lives, game over, scene flow |
| `Scripts/GameSettings.cs` | Difficulty presets, volume, high score |
| `Scripts/HUDController.cs` | In-game HUD |
| `Scripts/MainMenuController.cs` | Main menu + options |
| `Scripts/EndGameController.cs` | End screen |
| `Scripts/SnowfallController.cs` | Dynamic weather |
| `Scripts/Parallax.cs` | Depth for the background clouds |
| `Scripts/InvincibilityBlink.cs` | Blinks the boarder while invincible |
| `Editor/Lab2SceneBuilder.cs` | Generates menu/HUD/course/end screen |
| `Editor/Lab2SmokeTest.cs` | Head-less play-through test |
| `Editor/Lab2Screenshot.cs` | Captures menu / level screenshots |
| `Editor/PhysicsSpy.cs` | Logs physics spikes (debug helper) |

---

## 5. Design decisions

1. **Keep the provided level, extend it with a builder.** The lab ships a finished
   downhill scene (Sprite Shape terrain, camera rig, finish line) and an empty
   `Scripts` folder. Instead of hand-editing the scene YAML, all the additions are
   produced by an editor script (`Lab2 Scene → Build All Scenes`). The scene stays
   reproducible and the “why” of every object is in code.
2. **Placement follows the real collision surface.** The builder reads the
   `EdgeCollider2D` points of the mountain and takes the highest Y for a given X.
   Collectibles, obstacles and ramps are therefore guaranteed to sit on the slope
   instead of floating.
3. **New Input System.** The project is configured with *Input System Package
   (New) only*, so every script talks to `Keyboard.current` / `Gamepad.current`
   instead of the legacy `Input` class.
4. **Singletons with a safety net.** `GameManager`/`ScoreManager` are persistent
   and expose `EnsureExists()`, so the level can also be played directly from the
   editor without going through the menu.
5. **Procedural art for the new elements.** The snowflake, power-up badges and the
   ramp sprite are generated as PNGs by the builder, so the project has no missing
   references and no external dependencies.
6. **Component-based obstacle detection.** Instead of tags (which are not defined
   in the project) the player detects an obstacle by looking for the `Obstacle`
   component, which avoids the “tag is not defined” runtime exception.

---

## 6. Challenges faced

* **Unity 6 API changes** — `Rigidbody2D.velocity` became `linearVelocity`, and the
  project uses the new Input System only, so the classic tutorial code could not be
  reused verbatim.
* **Different sprite densities** — the supplied sprites use different
  pixels-per-unit values (trees 20 PPU, rocks 100 PPU, tiles 200 PPU). The builder
  normalises each obstacle to a target world height instead of assuming one scale.
* **Camera rig** — the provided scene uses Cinemachine; the package had to be added
  to `Packages/manifest.json` so the virtual camera keeps working.
* **Surface reconstruction** — the mountain is a curved Sprite Shape, so placing
  items required reading the generated `EdgeCollider2D` and solving for the top
  surface at a given X rather than assuming a straight slope.
* **Airborne detection** — a downward raycast hits the player's own colliders, so
  the trick controller filters out its own transform and all trigger colliders.
* **Falling through the slope** — the supplied boarder collider was a very thin
  capsule, and at boost speed (~32 units/second) it could slip through the
  tessellated `EdgeCollider2D` of the mountain. Fixed with four measures: a wide,
  thick “snowboard” collider, continuous collision detection with interpolation, a
  100 Hz fixed physics step, and a safety net that puts the board back on the last
  safe spot if it ever leaves the level (without costing a life).
* **Getting stuck on obstacles** — because the game scrolls on one axis the player
  cannot dodge sideways, so obstacles were reworked: trees are solid and must be
  jumped, while low “powder mounds” are pass-through triggers that only scrub speed
  (and the ramps are triggers that launch the board, so a slow player cannot wedge
  against them).

---

## 7. Optional features included

* **Three difficulty levels** (Easy / Normal / Hard) changing base speed, boost
  speed and gravity, selectable from the options menu.
* **Time display** and a personal **high-score** saved with `PlayerPrefs`.
* **Combo / multiplier** system with an on-screen combo timer.
* **Dynamic weather and visibility** changes.
* **Invincibility** power-up that lets the player smash through obstacles.

---

## 8. How to run

1. Open the project in Unity 6000.6.0f1.
2. Open `Assets/Scenes/MainMenu.unity` and press **Play**.
3. (Scenes can be regenerated at any time from the menu **Lab 2 → Build All
   Scenes**, or head-less with
   `Unity.exe -batchmode -quit -executeMethod Lab2SceneBuilder.BuildAll`.)

The build settings already contain the three scenes in the correct order.

## 9. Validation

The project ships with a head-less smoke test (`Assets/Editor/Lab2SmokeTest.cs`)
that boots the game from the main menu, presses START, plays the level until the
end screen appears and fails if any error / exception was logged:

```
Unity.exe -batchmode -nographics -projectPath "<project>" -executeMethod Lab2SmokeTest.Run
```

It is also wired to the menu entry **Lab 2 → Build All Scenes then Smoke Test**.
The three scenes report **0 missing scripts**.

There is also a screenshot helper (`Assets/Editor/Lab2Screenshot.cs`) that renders
the main menu and the level off-screen (into 1920×1080 PNGs) so the layout can be
reviewed without opening the editor, and a `PhysicsSpy` component that logs any
abrupt velocity change (used to track down the “launched into orbit” physics bug).

| Editor menu entry | What it does |
|---|---|
| Lab 2 → Build All Scenes | Regenerates art + all three scenes |
| Lab 2 → Rebuild Main Menu / Level / End Screen | Rebuilds one scene |
| Lab 2 → Build All Scenes then Smoke Test | Build + automated play-through |
| Lab 2 → Build All Scenes then Screenshot | Build + render menu/level PNGs |
