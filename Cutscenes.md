\---

name: stardew-cutscenes

description: >

&#x20; Practical guide for authoring Stardew Valley 1.6+ cutscenes and events (Data/Events/<Location>)

&#x20; in Content Patcher. Covers four-beat pacing, tile coordinates and actor movement, emotes and

&#x20; dialogue, music/sound/ambient light, single-map and multi-location transitions, branching forks

&#x20; and state flags, and Content Patcher pack structure. Trigger on "cutscene", "cut scenes",

&#x20; "event scripting", "Data/Events", "Content Patcher event", or "/cutscenes".

\---



\# Stardew Valley Cutscenes Skill



Stardew Valley 1.6+ Cutscene \& Event Authoring Guide

===



A practical blueprint for writing cutscenes (`Data/Events/<Location>`) in \*\*Content Patcher\*\* that feel like authentic Pelican Town moments—warm, grounded, well-paced, and free of awkward dead air or broken choreography.



\---



\## 1. Cutscene Anatomy \& ConcernedApe Pacing



A great Stardew Valley cutscene rarely relies on walls of exposition. Most memorable events follow a four-beat arc:



1\. \*\*Atmospheric Hook (0–4s):\*\* Set the music, ambient sound, or weather/time tone (`ambientLight`, `playMusic`, `playSound`), establish the camera focus (`viewport`), and show a quiet small-town detail before anyone speaks.

2\. \*\*Inciting Beat / Action:\*\* Someone enters, notices something, drops or picks up an item, or reacts to the environment (`addTemporaryActor`, `move`, `emote`, `jump`).

3\. \*\*Human Exchange \& Choice:\*\* Short, natural lines (1–3 sentences per bubble) with real emotional subtext—followed at key moments by a branching choice (`question fork` / `fork`) that subtly shifts tone or friendship without punishing the player.

4\. \*\*Quiet Resolution \& Fade:\*\* A gesture, a lingering look, a music shift or fade (`playMusic none` / `fade`), and a clean return to gameplay (`end`).



\### Pacing Rules of Thumb



\* \*\*Don't rush text:\*\* Give the player time to read dialogue and see reactions (`pause 500` to `pause 1200` ms between beats).

\* \*\*Don't stack simultaneous chaos:\*\* Have one primary focal action per beat so the eye knows where to look.

\* \*\*Match emotion to rhythm:\*\* Comedic or bustling scenes use brisker pauses (`300–500` ms) and quick hops (`jump`); quiet, melancholic, or reflective scenes use longer pauses (`900–1500` ms), slower walks, and gentle music.



\---



\## 2. Core Choreography: Walking, Facing, and Moving Around



\### Coordinate \& Facing Conventions



\* \*\*Tile Coordinates (`X Y`):\*\* Measured in map tiles from the top-left corner (`0 0`) of the target location. Always check tile passability (no water, walls, or furniture blocking paths) in Tiled or in-game.

\* \*\*Facing Directions:\*\*



&#x20; \* `0` = Up (North)

&#x20; \* `1` = Right (East)

&#x20; \* `2` = Down (South)

&#x20; \* `3` = Left (West)



\### Movement Techniques



\* \*\*Point-to-Point Walking (`move <Actor> <X> <Y> <Facing> \[Speed]`):\*\*



&#x20; \* Use `farmer` for the player character or an internal character name (e.g., `Robin`, `Lewis`, or a custom NPC) for villagers.

&#x20; \* Ending `<Facing>` sets which way the character looks once they arrive at `<X> <Y>`.

&#x20; \* \*Tip:\* If two characters walk near each other, stagger their start times or offset their paths by at least 1 tile so they don't awkwardly clip or block one another.

\* \*\*Turning in Place (`faceDirection <Actor> <Facing>`):\*\*



&#x20; \* Great for a character turning to look at the farmer, turning back toward a workbench or window, or reacting to a sound behind them before walking.

\* \*\*Temporary / Extra Actors (`addTemporaryActor` / `removeTemporaryActor`):\*\*



&#x20; \* Useful for props, animals, or characters who only appear during the scene (or stand in a specific spot without disrupting normal schedules). Always clean up temporary actors or ensure the `end` command resets state cleanly.

\* \*\*Camera Framing (`viewport`):\*\*



&#x20; \* Lock the camera (`viewport <X> <Y> true`) onto the action center so the player doesn't see empty void or off-center clutter.

&#x20; \* Pan smoothly (`viewport move <dX> <dY> <DurationMs>`) when following a character walking across a room or revealing a new part of the scene.



\---



\## 3. Character Reactions: Emotes, Body Language, and Dialogue



\### Non-Verbal Reactions First



Before a character speaks, show how they feel:



\* \*\*Emotes (`emote <Actor> <EmoteId>`):\*\* Use thought/emotion bubbles above the head to telegraph shock (`!`), curiosity (`?`), affection (`heart`), annoyance/anger (`angry`), or music/joy (`musicNote`). Let the emote sit for a beat (`pause 600`) before the dialogue box pops.

\* \*\*Physical Beats (`jump <Actor> \[Velocity]` / `shake` / temporary animations):\*\* A small hop (`jump`) works well for surprise, excitement, or a playful nudge—keep it subtle so it doesn't feel cartoonish unless the character is being silly.

\* \*\*Eye Contact \& Turn Beats:\*\* Have a character pause mid-task, `faceDirection` toward the speaker or the player, hold a brief beat, and \*then\* deliver the line.



\### Writing Authentic Dialogue



\* \*\*Keep lines tight:\*\* 1–3 sentences max per speech bubble. Break long thoughts across multiple sequential `speak` commands rather than one giant block.

\* \*\*Use natural contractions \& small-town voice:\*\* Characters should sound like real neighbors with daily routines, worries, and dry warmth—not exposition machines.

\* \*\*Earn emotional turns:\*\* If a character opens up about burnout, loneliness, or an old memory, lead into it with ordinary details (tea cooling, tools put down, rain on glass).



\---



\## 4. Music, Ambience, and Sound Effects



Audio is half the emotional weight of a Stardew cutscene:



\* \*\*Music Shifts (`playMusic <TrackName>`):\*\*



&#x20; \* Introduce a track when the emotional tone shifts (e.g., warm acoustic for a heart-to-heart, gentle/melancholic piano for quiet reflection, upbeat/lively for a festival or town gag).

&#x20; \* Fade or stop music (`playMusic none`) when transitioning into silence, a dramatic reveal, or an ending beat.

\* \*\*Sound Effects (`playSound <SoundCue>`):\*\*



&#x20; \* Layer small foley cues onto physical actions: footsteps, doors, tools, chimes, or UI/reaction sounds.

&#x20; \* Keep sound effects purposeful—one crisp cue on a key action beats spamming sounds every line.

\* \*\*Lighting \& Mood (`ambientLight <R> <G> <B>`):\*\*



&#x20; \* Shift ambient tint when a scene takes place at dusk, in a dimly lit cellar/shed, or during a tense/quiet moment, then restore normal lighting before `end`.



\---



\## 5. Scene Transitions \& Multi-Place Cutscenes



Cutscenes don't have to stay frozen in one room. You can move the scene across spaces using two main patterns:



\### Pattern A: Camera Pan Within One Map



\* Best when moving from a doorway to a table, or from a garden path to a porch.

\* Keep all actors placed cleanly along the path, pan `viewport`, walk actors into frame, and continue the conversation.



\### Pattern B: Location Change (`changeLocation <LocationName> <X> <Y> <Facing>` or Map Warp)



\* Best when characters walk out a door into town/woods, travel to a new room, or continue the event in another map (`Farm`, `Town`, `Forest`, `Beach`, or a custom location).

\* \*\*Checklist when changing locations:\*\*



&#x20; 1. Fade out (`fade` / screen transition) so the cut feels intentional rather than jarring.

&#x20; 2. Call the location change command (`changeLocation <MapName> <FarmerX> <FarmerY> <FarmerFacing>`).

&#x20; 3. Re-position or re-add any NPCs/actors at valid tiles on the \*\*new\*\* map (tile `(12, 18)` in a cabin is \*not\* the same spot outside in `Town`!).

&#x20; 4. Re-set `viewport` for the new map and fade back in (`viewport ... true`).



\---



\## 6. Branching Choices \& State Flags



\* \*\*Player Choices (`question fork <Prompt>` / `fork <BranchId>`):\*\*



&#x20; \* Offer 2–3 distinct, natural responses (e.g., supportive, pragmatic/dry, or playful) rather than one obvious "good" answer and two rude ones.

&#x20; \* Make sure every branch rejoins cleanly or reaches its own `end` without leaving dangling actors or unclosed forks.

\* \*\*Tracking Outcomes (`addMailReceived`, `addEventSeen`, or `Data/TriggerActions`):\*\*



&#x20; \* If a cutscene's outcome should be remembered later (in dialogue, mail, or follow-up events), record a clean `{{ModId}}\_`-prefixed flag or mail/event state so future content can check it.



\---



\## 7. Content Patcher (`1.6+`) Authoring Pattern



Structure your Content Patcher pack cleanly with namespaced IDs and `i18n` localization:



```text

MyCutscenePack/

├── manifest.json

├── content.json

└── i18n/

&#x20;   └── default.json

