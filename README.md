# Isle of the Ancients (3D Android Action-Adventure)

A complete 3D Action-Adventure game built in **Unity 6 (`6000.5.9f1`)** for **Android**, engineered with modern PBR rendering, touchscreen mobile controls, enemy AI, stamina sprinting, audio feedback, quest progression, and persistent saves.

---

## 🎮 Game Architecture & Features

### 1. Scenes & Flow
- **Scene 0: [`MainMenu.unity`](file:///Users/fabrianivan/Project-3D/Assets/AdventureGame/Scenes/MainMenu.unity)**
  - Interactive 3D Title screen with dynamic camera framing an ancient altar and floating relic crystal.
  - **Start Adventure**: Seamless transition to the gameplay world.
  - **How to Play**: Full in-game controls and objectives manual.
  - **Settings Modal**: Master audio volume slider and 30/60 Target FPS toggle.
  - **Persistent Stats Display**: Shows best score and mastery stars (`⭐ ⭐ ⭐`) loaded from `PlayerPrefs`.
- **Scene 1: [`AdventureIsland.unity`](file:///Users/fabrianivan/Project-3D/Assets/AdventureGame/Scenes/AdventureIsland.unity)**
  - The main open 3D adventure world.

### 2. Player Mechanics
- **Health System ([`PlayerHealth.cs`](file:///Users/fabrianivan/Project-3D/Assets/AdventureGame/Scripts/PlayerHealth.cs))**:
  - 3-Heart visual system (`❤️ ❤️ ❤️`).
  - Invulnerability flashing window (1.5s) upon taking damage.
  - Knockback physics and damage audio.
- **Stamina & Sprinting ([`PlayerStamina.cs`](file:///Users/fabrianivan/Project-3D/Assets/AdventureGame/Scripts/PlayerStamina.cs))**:
  - Depleting/recharging stamina bar.
  - Boosts movement speed by 1.6x.
  - Activated via `Left Shift` on desktop or on-screen `SPRINT` button on mobile.
- **Audio Feedback ([`PlayerAudioEffects.cs`](file:///Users/fabrianivan/Project-3D/Assets/AdventureGame/Scripts/PlayerAudioEffects.cs))**:
  - Cycling surface footsteps (4 variations) with dynamic pitch shifting and frequency based on walk/sprint pace.
  - Custom jump and ground impact landing sounds.

### 3. Combat & Enemy AI ([`PatrolEnemy.cs`](file:///Users/fabrianivan/Project-3D/Assets/AdventureGame/Scripts/PatrolEnemy.cs))
- **Patrol Behavior**: Red guardian automations patrolling between stone ruin waypoints.
- **Aggro & Chase**: Senses the player within 8 meters, turns, and charges.
- **Combat**: Deals damage and knocks back player upon collision.
- **Stomp Defeat**: Jumping on the enemy from above squashes the enemy, launches Ethan into a high bounce, awards 250 points, and plays particle bursts.

### 4. Quest & Shrine Portal
- **Ancient Relic Keys ([`CollectibleKey.cs`](file:///Users/fabrianivan/Project-3D/Assets/AdventureGame/Scripts/CollectibleKey.cs))**:
  - 🗝️ **Ruby Key**: Guarded in the ancient stone ruins.
  - 🗝️ **Sapphire Key**: Hidden across stepping stones on the remote outpost island.
  - 🗝️ **Emerald Key**: Located on the floating sky island across the moving elevator.
- **Summit Shrine Portal ([`ShrinePortal.cs`](file:///Users/fabrianivan/Project-3D/Assets/AdventureGame/Scripts/ShrinePortal.cs))**:
  - Requires all 3 keys to activate.
  - Unlocks a celestial beacon light beam and gate aura.
  - Stepping into the activated portal triggers the victory sequence!

### 5. Mobile Touch HUD ([`CompleteGameHUD.cs`](file:///Users/fabrianivan/Project-3D/Assets/AdventureGame/Scripts/CompleteGameHUD.cs))
- **Top Bar**: Live Hearts (`❤️ ❤️ ❤️`), Stamina bar, Timer (`00:00`), Gems (`💎 0/10`), Score, and Key inventory icons (Ruby/Sapphire/Emerald).
- **Controls**: Virtual Joystick (movement), Jump button, Sprint button, Pause button.
- **Modals**:
  - **Pause Modal**: Resume, Restart, Main Menu.
  - **Game Over Modal**: Retry from Checkpoint, Main Menu.
  - **Victory Modal**: Mastery Star Rating (1 to 3 Stars), completion time, total score, Play Again, Main Menu.

---

## 📱 Controls

| Action | Android Touch | Desktop / Play Mode |
| :--- | :--- | :--- |
| **Move** | Virtual Joystick (Bottom-Left) | `W`, `A`, `S`, `D` / Arrow Keys |
| **Jump** | `JUMP` Button (Bottom-Right) | `Spacebar` |
| **Sprint** | `SPRINT` Button (Right Side) | `Left Shift` (Hold) |
| **Look / Orbit** | Dynamic Follow Camera | Mouse (Auto-aligns behind movement) |
| **Pause** | `❚❚` Pause Button (Top-Right) | `Escape` or `P` |

---

## 🚀 How to Run & Play

1. In Unity Editor, open [`Assets/AdventureGame/Scenes/MainMenu.unity`](file:///Users/fabrianivan/Project-3D/Assets/AdventureGame/Scenes/MainMenu.unity).
2. Press the **Play** button (`▶`).
3. Click **START ADVENTURE** to begin exploring the island!
