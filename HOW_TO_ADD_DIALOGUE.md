# VN Talks: complete guide (for humans and AI assistants)

Read this whole file before editing anything. It contains everything needed. Do not invent fields, file names or syntax that are not written here.

---

## 1. What this project is

A visual-novel style story system for Stardew Valley. Three villagers (Robin, Sam, Caroline) each have:

- a **menu** that opens when you talk to them (chat, topics, tone game, time together, memories),
- **three story chapters** that unlock by hearts + number of chats,
- **letters** sent the morning after a chapter,
- a villager who **runs to you** when a chapter is waiting.

Two parts work together:

| Part | Made of | Job |
|---|---|---|
| **Add-on** `VN_Talks` | JSON files only | All dialogue, choices, chapters. Loaded by the mod **Character Interact**. |
| **Mod** `VNApproach` | C# code + JSON in `assets/` | Makes villagers run to you, adds the letters to the game. One single mod. **No Content Patcher needed.** |

Requirements: SMAPI, Character Interact (mod id `Fouky.CharacterInteract`). 

---

## 2. File tree (what is where)

### 2.1 The zip / source layout

```
VN_Talks_Merged/
├── README.md                      install + build summary
├── HOW_TO_ADD_DIALOGUE.md         THIS FILE
├── tools/
│   └── validate.py                consistency checker (python tools/validate.py)
├── Addons/
│   └── VN_Talks/                  ← the ADD-ON (all dialogue)
│       ├── interact.json          list of villagers + files to load + daily greeting
│       ├── Robin.json             Robin: chat + chapters 1-3
│       ├── Robin_Daily.json       Robin: topics, tone game, time together, memories
│       ├── Sam.json
│       ├── Sam_Daily.json
│       ├── Caroline.json
│       └── Caroline_Daily.json
└── Mods/
    └── VNApproach/                ← the MOD (code + letters + run-to-you rules)
        ├── manifest.json          mod id Fouky.VNApproach, entry DLL
        ├── VNApproach.csproj      build file
        ├── ModEntry.cs            main code (do not edit for dialogue)
        ├── ModConfig.cs           settings class (do not edit for dialogue)
        ├── ApproachRule.cs        rule class (do not edit for dialogue)
        └── assets/
            ├── mail.json          the letters
            └── rules.json         when a villager runs to you
```

### 2.2 The layout after installing

```
Stardew Valley/
└── Mods/
    ├── character_interact/        (the Character Interact mod, installed separately)
    │   └── Addons/
    │       └── VN_Talks/          ← copy of Addons/VN_Talks from the zip
    │           ├── interact.json
    │           ├── Robin.json, Robin_Daily.json
    │           ├── Sam.json, Sam_Daily.json
    │           └── Caroline.json, Caroline_Daily.json
    └── VNApproach/                ← built from Mods/VNApproach
        ├── manifest.json
        ├── VNApproach.dll
        ├── config.json            (created on first run)
        └── assets/
            ├── mail.json
            └── rules.json
```

Install steps:
1. Copy `Addons/VN_Talks` into `Stardew Valley/Mods/character_interact/Addons/`.
2. Install .NET 6 SDK, open a terminal in `Mods/VNApproach`, run `dotnet build -c Release`. The result is copied to `Stardew Valley/Mods/VNApproach` automatically.
3. Delete any old `[CP] Visual Novel Talks` folder and any old VNApproach mod so nothing loads twice.

Reloading:
- Edited a file in `Addons/VN_Talks` while playing: type `ci_reload` in the SMAPI console.
- Edited `assets/mail.json` or `assets/rules.json`: restart the game.
- Edited a `.cs` file: rebuild, restart the game.

### 2.3 How the pieces connect (data flow)

```
 player talks to villager
          │
          ▼
 Character Interact opens menu ◄──── Addons/VN_Talks/<Name>.json + <Name>_Daily.json
          │                              (Conditions decide what is shown)
          ├── "Have a proper chat"  → counter  Talks_<Name> +1
          ├── chapter button        → sets flag Ch1_<Name> ... and sends letter
          │                                        │
          │                                        ▼
          │                         "AddMail Current VNTalks_<Name>_Letter1 Tomorrow"
          │                                        │
          │                                        ▼
          │                         Mods/VNApproach/assets/mail.json  (same key)
          │
 every 2 seconds, VNApproach checks Mods/VNApproach/assets/rules.json:
   if the Conditions of Chapter1/2/3 are true for a villager in your room
   → villager runs to you → opens the Character Interact menu
   (rules.json Conditions MUST equal the chapter Conditions in the villager file)
```

---

## 3. The three golden rules

1. **Two places must always agree.** Chapter unlock Conditions exist in the villager file (`<Name>_Ch1/2/3`) AND in `assets/rules.json` (`Chapter1/2/3`). Letter keys exist in the villager file (`AddMail ...`) AND in `assets/mail.json`. Flags are written in `SetFlags` and read in `Conditions`; spelling must be identical.
2. **Edit JSON carefully.** Comma after every item except the last. Double quotes only. A quote inside text is `\"`. `// comments` are allowed.
3. **Run `python tools/validate.py` after every edit.** It must print `OK: everything agrees.`

Never edit `.cs` files to change dialogue. Only edit `interact.json` to add a villager or change the daily greeting.

---

## 4. What do I edit? (decision table)

| I want to... | Edit this file | Section |
|---|---|---|
| add random chat lines | `Addons/VN_Talks/<Name>.json` → action `<Name>_Chat` | 6 |
| change a story chapter | `Addons/VN_Talks/<Name>.json` → `<Name>_Ch1/2/3` | 7 |
| add a topic to "Talk about..." | `Addons/VN_Talks/<Name>_Daily.json` → `<Name>_Topics` → `Children` | 8.1 |
| add a "Say something" situation | `<Name>_Daily.json` → `<Name>_Moment` | 8.2 |
| add a time-together scene | `<Name>_Daily.json` → `<Name>_Together` | 8.3 |
| add a "Remember when..." entry | `<Name>_Daily.json` → `<Name>_Memories` → `Children` | 8.4 |
| change the daily greeting | `Addons/VN_Talks/interact.json` → `Greeting` | 8.5 |
| add a letter | `Mods/VNApproach/assets/mail.json` | 9 |
| add a new villager | copy 2 files + edit `interact.json` + `mail.json` | 10 |
| change unlock hearts/chats or add Chapter 4 | villager files + `Mods/VNApproach/assets/rules.json` | 11 |
| change how villagers run, speed, cooldown | `rules.json` / `config.json` | 12 |

---

## 5. Syntax reference (everything an add-on file can contain)

### 5.1 File skeleton of a villager file

```
{
  "Npcs": [ "Robin" ],          which villager(s) this file is for (internal name)
  "Actions": [                  each action = one entry in the menu
    { ...action... },
    { ...action... }
  ]
}
```

### 5.2 Fields of an action (a menu entry)

Only `Id` and `Label` are required.

| Field | Meaning |
|---|---|
| `Id` | Unique name across ALL files of the add-on. Start with the villager name: `Robin_Chat`. |
| `Label` | Text on the menu button. |
| `Order` | Lower number = higher in the menu. Default 100. Used here: chapter 5, Talk about 40, Say something 45, Time together 48, Chat 50, Remember when 60. |
| `Conditions` | Entry is shown only when true (section 5.5). |
| `Limit` | `Day` once per day, `Ever` once per save, `None` unlimited. Per villager. |
| `Prompt` + `Children` | Makes the entry a sub-menu. `Prompt` is the title; `Children` is a list of actions. |
| `Steps` | A scene: list of steps played in order (5.3). |
| `Say` | Shortcut for a single spoken line (string, or a list: one random line is picked). |
| `Friendship` | Points added (250 = 1 heart). |
| `AddCounters` | `{ "Talks_{{NpcName}}": 1 }` adds to a counter. |
| `SetFlags` / `ClearFlags` | `[ "Ch1_{{NpcName}}" ]` sets / removes a flag. |
| `Reward` | `{ "Items": [ { "Id": "(O)388", "Count": 100 } ], "Gold": 0 }` |
| `Cost` | Same shape as Reward; the player pays. If they cannot, nothing happens. Add `FailText`. |
| `TriggerActions` | List of game actions, e.g. `"AddMail Current VNTalks_Robin_Letter1 Tomorrow"`. |
| `Emote`, `Sound` | Bubble over the villager; sound cue. |

### 5.3 Step types (one step = one box on screen)

| Step | Shows |
|---|---|
| `{ "Say": "Text.$h" }` | Villager speaks (portrait + mood). |
| `{ "Narrate": "Text." }` | Plain text box, no portrait (scene setting). |
| `{ "As": "Sebastian", "Say": "Text." }` | Another villager speaks. `"As": "Narrator"` = plain box. |
| `{ "Emote": "Happy" }` | Emote bubble. Names: Question, Angry, Exclamation, Heart, Sleep, Sad, Happy, Music, Blush, or a number. |
| `{ "Pause": 800 }` | Wait 800 milliseconds. |
| `{ "Ask": { "Prompt": "Robin", "Options": [ ... ] } }` | Question with buttons. |
| `{ "If": "<cond>", "Then": [ steps ], "Else": [ steps ] }` | Branch on a condition (`Else` optional). |
| `{ "Random": [ branches ] }` | Weighted random branch. |
| `{ "End": true }` | Stop the scene here. |

A step can also carry any effect field from 5.2 (`Friendship`, `SetFlags`, `Reward`, `TriggerActions`, `AddCounters`, `Conditions`, `Once`). Inside one step the order is: Conditions → Cost → effects → Say/Narrate → If/Random/Ask → Pause/End. If you need another order, use two steps. Only ONE of `If` / `Random` / `Ask` per step.

**Ask option shape:**
```json
{ "Label": "Button text",
  "Conditions": "optional, option hidden if false",
  "Steps": [ { "Say": "Reply.$h", "Friendship": 20 } ] }
```
An option's steps may contain another `Ask`. After the chosen option finishes, the scene continues with the step after the `Ask`.

**Random branch shape:**
```json
{ "Random": [
  { "Weight": 3, "Conditions": "WEATHER Here Rain", "Steps": [ { "Say": "Rain line." } ] },
  { "Weight": 2, "Conditions": "TIME 1800 2600",   "Steps": [ { "Say": "Evening line." } ] },
  { "Weight": 1,                                      "Steps": [ { "Say": "Fallback, no Conditions." } ] }
] }
```
A branch with a false Condition is skipped. Always keep one branch without `Conditions` as fallback.

### 5.4 Text codes (written inside the text)

| Code | Meaning | Code | Meaning |
|---|---|---|---|
| `@` | player's name | `$h` | happy portrait |
| `#$b#` | start a new text box inside the same Say | `$s` | sad |
| `$u` | unamused | `$a` | angry |
| `$l` | love | `$neutral` | neutral |

Put the mood code at the END of a box, before `#$b#` if another box follows: `"First box.$h#$b#Second box.$s"`.

Placeholders usable in text, labels and conditions: `{{NpcName}}` (villager), `{{PlayerName}}`, `{{Hearts}}`, `{{Gold}}`, `{{Day}}`, `{{Season}}`, `{{Counter:Name}}`.

### 5.5 Conditions

Several conditions separated by commas must ALL be true. A leading `!` negates one.

| Write | True when |
|---|---|
| `PLAYER_HAS_MET Current {{NpcName}}` | player has met the villager |
| `PLAYER_HEARTS Current {{NpcName}} 4` | 4 or more hearts |
| `PLAYER_HEARTS Current {{NpcName}} 2 5` | between 2 and 5 hearts |
| `PLAYER_NPC_RELATIONSHIP Current {{NpcName}} Dating` | dating (also Married, Friendly) |
| `PLAYER_HAS_ITEM Current (O)66` | player carries the item |
| `Fouky.CharacterInteract_HAS_FLAG Ch1_{{NpcName}}` | flag is set |
| `Fouky.CharacterInteract_FLAG_DAYS Name 0 3` | flag set 0 to 3 days ago |
| `Fouky.CharacterInteract_COUNTER Talks_{{NpcName}} 6` | counter is 6 or more |
| `WEATHER Here Rain` / `WEATHER Here Snow` | weather |
| `TIME 600 1200` | between 6:00 and 12:00 (evening: `TIME 1800 2600`) |
| `DAY_OF_WEEK Friday` | weekday |
| `LOCATION_SEASON Here winter` | season |
| `RANDOM 0.5` | half the time |

Example: `"Conditions": "PLAYER_HEARTS Current {{NpcName}} 4, !WEATHER Here Rain"`.

### 5.6 Reusable scene (optional)

At the top level of a villager file: `"Scripts": { "Blush": [ { "Emote": "Blush" }, { "Say": "Um." } ] }`, then in a step: `{ "Call": "Blush" }`. Script names are case sensitive.

### 5.7 Friendship and fairness

- 250 points = 1 heart. Normal talking gives 20 points/day, a loved gift 80.
- Anything repeatable daily: **10 points or less**.
- Bigger numbers only for one-time story steps (this project uses 20 for a choice, 50 for a chapter end, 100 for chapter 3).

### 5.8 Item ids

Always the form `(O)id`: `(O)388` Wood, `(O)206` Pizza, `(O)614` Green Tea, `(O)66` Amethyst, `(O)276` Pumpkin.

---

## 6. Add chat lines

File: `Addons/VN_Talks/<Name>.json`, action `<Name>_Chat`. `Say` is a list; one random line per chat. Add a string and mind the commas:

```json
"Say": [
  "Existing line.$h",
  "Your new line, @.$h",
  "Two boxes in one line.#$b#This is the second box.$s"
]
```

Only "Have a proper chat" increases the counter `Talks_<Name>`, which gates the chapters. It works once per day per villager.

---

## 7. Story chapters

Each villager file has `<Name>_Ch1`, `_Ch2`, `_Ch3`. Layout of one chapter:

```
{
  "Id": "Robin_Ch1",
  "Label": "Come see the chair",
  "Order": 5,
  "Conditions": "<unlock rule, must equal rules.json>",
  "Steps": [
    { "Narrate": "..." },          opening scene
    { "Say": "..." },              dialogue ...
    { "Ask": { ... } },            player choice
    { "Narrate": "..." },
    { LAST STEP: SetFlags + Friendship + TriggerActions + Say }
  ]
}
```

You may freely add, remove, reorder the `Narrate`, `Say` and `Ask` steps in the middle.

**The LAST step must stay last and keep these four fields** (flag, friendship, letter, final line; the reward goes in chapter 3's last step):

```json
{ "SetFlags": [ "Ch1_{{NpcName}}" ],
  "Friendship": 50,
  "TriggerActions": [ "AddMail Current VNTalks_Robin_Letter1 Tomorrow" ],
  "Say": "Closing line.$h" }
```

If the flag is never set, the chapter can be replayed forever and the next chapter never unlocks.

**Do not change a chapter's `Conditions`** unless you change the matching rule in `rules.json` (section 11).

Current ladder (identical for every villager):

| Chapter | Hearts | Chats (`Talks_<Name>`) | Needs flag | Sets flag | Letter |
|---|---|---|---|---|---|
| 1 | 2 | 2 | none | `Ch1_<Name>` | `VNTalks_<Name>_Letter1` |
| 2 | 4 | 6 | `Ch1_<Name>` | `Ch2_<Name>` | `VNTalks_<Name>_Letter2` |
| 3 | 6 | 10 | `Ch2_<Name>` | `Ch3_<Name>` | none (gives an item) |

Chapter 3 rewards: Robin 100 Wood, Sam 2 Pizza, Caroline 3 Green Tea.

---

## 8. Everyday interactions (`<Name>_Daily.json`)

Menu order top to bottom: chapter (5) → **Talk about...** (40) → **Say something** (45) → **Spend some time together** (48) → **Have a proper chat** (50, in `<Name>.json`) → **Remember when...** (60).

| Entry | Unlocks | Repeats |
|---|---|---|
| Talk about... (sub-menu) | met them | each topic once a day |
| Say something | met them | once a day |
| Spend some time together | 3 hearts | once a day |
| Remember when... | after each chapter | each once a day |

Topics have their own counter `Topics_<Name>`; the "Ask about dreams" topic needs 3 of them and 4 hearts.

### 8.1 Add a topic

Inside `Children` of `<Name>_Topics`, add a block (comma between blocks). `Id` must be unique.

```json
{
  "Id": "Robin_Weather",
  "Label": "Ask about the weather",
  "Limit": "Day",
  "Friendship": 5,
  "AddCounters": { "Topics_{{NpcName}}": 1 },
  "Steps": [
    { "Say": "Weather? Ask me after I've checked the roof.$h" }
  ]
}
```

Show it later in the story: add `"Conditions": "PLAYER_HEARTS Current {{NpcName}} 6"` or `"Conditions": "Fouky.CharacterInteract_HAS_FLAG Ch2_{{NpcName}}"`. Topic with a question: use an `Ask` step (see Caroline_Daily.json `Caroline_Garden`). Answer depending on weather/time: use a `Random` step (see `<Name>_Day`).

### 8.2 Add a "Say something" situation

Inside `<Name>_Moment`, the `Random` list holds blocks like this. Copy one and edit:

```json
{ "Steps": [
  { "Say": "Situation line from the villager." },
  { "Ask": { "Prompt": "Robin", "Options": [
    { "Label": "Honest answer.",   "Steps": [ { "Say": "Warm reaction.$h", "Friendship": 10 } ] },
    { "Label": "Kind answer.",     "Steps": [ { "Say": "Okay reaction.$h", "Friendship": 5 } ] },
    { "Label": "Playful answer.",  "Steps": [ { "Say": "Okay reaction.$h", "Friendship": 5 } ] }
  ] } }
] }
```

Each villager has ONE favourite tone: **Robin honest, Sam playful, Caroline kind**. That option gets `Friendship` 10 and a clearly warmer reply; the other two get 5 or less. The favourite is also written in a comment above the action.

### 8.3 Add a time-together scene

Inside `<Name>_Together`, add a branch to its `Random` list. A scene is made of `Narrate`, `Pause`, `Emote`, `Say`:

```json
{ "Weight": 2, "Conditions": "TIME 1700 2600",
  "Steps": [
    { "Narrate": "The shop lamps come on." },
    { "Say": "Look at that light.$h" }
  ] }
```

### 8.4 Add a "Remember when..." entry

Inside `Children` of `<Name>_Memories`:

```json
{ "Id": "Robin_Mem4", "Label": "Remember the cabin?", "Limit": "Day", "Friendship": 5,
  "Conditions": "Fouky.CharacterInteract_HAS_FLAG Ch4_{{NpcName}}",
  "Say": "Memory line.$h" }
```

The flag must be one that is actually set (`Ch1_`, `Ch2_`, `Ch3_`, or your own).

### 8.5 Daily greeting

`Addons/VN_Talks/interact.json` → `Greeting` plays one short narrated line the first time a villager's menu opens each day. It uses `{{NpcName}}` so it works for everyone. Add a branch to its `Random` list for a new mood. A greeting cannot differ per villager. `Greeting` and `Menu` only work inside `interact.json`.

---

## 9. Add a letter

File: `Mods/VNApproach/assets/mail.json`. A flat object, one line per letter, comma after every line except the last:

```json
"VNTalks_Robin_Letter3": "Dear @,^First line.^Second line.^   -Robin[#]A note from Robin"
```

- Key naming: `VNTalks_<Name>_Letter<N>`.
- `^` = line break, `@` = player name, `[#]Title` = title shown in the collections tab.
- The key must match the chapter's `AddMail` line **exactly**: `"AddMail Current VNTalks_Robin_Letter3 Tomorrow"`.
- The letter arrives the morning after the chapter. Restart the game after editing.
- No letter for a chapter: delete its `AddMail ...` string from `TriggerActions` (do not leave an unknown key).

---

## 10. Add a new villager (example: Abigail)

1. In `Addons/VN_Talks` copy `Robin.json` → `Abigail.json` and `Robin_Daily.json` → `Abigail_Daily.json`.
2. In **both** copies use Find & Replace All: `Robin` → `Abigail`. This fixes `Npcs`, all `Id`s, `Prompt`s, flags and letter keys.
3. **Rewrite all dialogue.** Replace Robin's family, shop, places and story with the new villager's. Search the copy for leftover names (Maru, Demetrius, Sebastian, shop, carpentry). Change the Chapter 3 reward item if wanted.
4. In `interact.json`: add `"Abigail"` to `Npcs`, and `"Abigail.json"`, `"Abigail_Daily.json"` to `Include`.
5. In `Mods/VNApproach/assets/mail.json`: add `VNTalks_Abigail_Letter1` and `VNTalks_Abigail_Letter2`.
6. Update the "favourite tone" comment and reactions in `Abigail_Daily.json` → `Abigail_Moment`.
7. Run `python tools/validate.py`.

Use the villager's **internal name**: the English name for vanilla villagers, or the key in the mod's `Data/Characters` for custom ones. Nothing in `rules.json` needs changing: Chapter 1-3 rules cover every villager automatically.

---

## 11. Unlock ladder, changing numbers, Chapter 4

The unlock rule exists in TWO places that must be identical (except brace style):

| Place | Braces | Effect |
|---|---|---|
| `Conditions` of `<Name>_Ch1/2/3` in the villager file | `{{NpcName}}` | chapter appears in the menu |
| `Chapter1/2/3` → `Conditions` in `assets/rules.json` | `{NpcName}` | villager runs to you |

Example, Chapter 2.

Villager file:
```
"Conditions": "Fouky.CharacterInteract_HAS_FLAG Ch1_{{NpcName}}, PLAYER_HEARTS Current {{NpcName}} 4, Fouky.CharacterInteract_COUNTER Talks_{{NpcName}} 6, !Fouky.CharacterInteract_HAS_FLAG Ch2_{{NpcName}}"
```
rules.json:
```
"Conditions": "Fouky.CharacterInteract_HAS_FLAG Ch1_{NpcName}, PLAYER_HEARTS Current {NpcName} 4, Fouky.CharacterInteract_COUNTER Talks_{NpcName} 6, !Fouky.CharacterInteract_HAS_FLAG Ch2_{NpcName}"
```

**To change a number:** change it in every villager's chapter AND in rules.json, then run the validator (it compares Chapters 1-3).

**To add Chapter 4 for chosen villagers:**
1. In the villager file copy `<Name>_Ch3` → `<Name>_Ch4`. New `Id`, `Label`.
2. `Conditions`: `Fouky.CharacterInteract_HAS_FLAG Ch3_{{NpcName}}, PLAYER_HEARTS Current {{NpcName}} <hearts>, Fouky.CharacterInteract_COUNTER Talks_{{NpcName}} <chats>, !Fouky.CharacterInteract_HAS_FLAG Ch4_{{NpcName}}`.
3. Last step: `"SetFlags": [ "Ch4_{{NpcName}}" ]`, add `TriggerActions` with a new letter key, add the letter to `mail.json`.
4. In `rules.json` copy `Chapter3` → `Chapter4` (higher `Priority`, same Conditions with single braces) and set `"Npcs": [ "Robin" ]` listing **only** villagers that have a Chapter 4. Using `"*"` would make villagers without one run to you for nothing.
5. The validator does not check Chapter 4: compare both Conditions by eye.

---

## 12. The run-to-you rules (`assets/rules.json`) and mod settings

### 12.1 What a rule looks like

`rules.json` is an object: key = rule name, value = rule.

```json
"Chapter1": {
  "Npcs": [ "*" ],
  "Required": true,
  "Priority": 10,
  "Conditions": "PLAYER_HEARTS Current {NpcName} 2, ...",
  "StartLines": [ "@! Wait up, I wanted to ask you something!" ],
  "StartEmote": "Exclamation",
  "ArriveEmote": "Happy",
  "OpenMenu": true
}
```

### 12.2 All rule fields

| Field | Default | Meaning |
|---|---|---|
| `Npcs` | everyone | internal names, or `"*"` for everyone |
| `ExcludeNpcs` | none | skipped when Npcs is `"*"` |
| `Conditions` | always true | game state query; `{NpcName}` = villager's internal name |
| `Required` | false | true: villager comes as soon as Conditions are true. false: optional, rolled by `Chance` |
| `Chance` | 0.1 | optional rules only: chance per real-time MINUTE |
| `Cooldown` | Day | `Day` once per day per villager, `Ever` once per save, `None` |
| `Priority` | 0 | higher goes first; required rules always beat optional |
| `Locations` | anywhere | only in these locations (internal names, or `Outdoors` / `Indoors`) |
| `ExcludeLocations` | none | never in these |
| `TimeoutSeconds` | 25 | gives up after this many real seconds |
| `StartLines` | none | speech bubbles when starting to run (random one; `@` = player name) |
| `StartEmote` | Exclamation | Question, Angry, Exclamation, Heart, Sleep, Sad, Happy, Music, Blush or number |
| `ArriveEmote` / `ArriveLines` | none | on arrival |
| `OpenMenu` | true | on arrival, open the Character Interact menu |

Rules in this project: `Chapter1`, `Chapter2`, `Chapter3` (required) and `DropBy` (optional flavour: friends at 4+ hearts sometimes run over to say hi, 9:00 to 19:00, no menu).

### 12.3 When a villager will NOT run (by design)

Player is not in the same location, villager sleeping or invisible, villager walking a schedule path, festival or event running, another approach active, less than 90 seconds since the last approach, or the villager's next schedule stop is within 40 in-game minutes. After the talk the villager walks back to the exact tile and facing it had before.

### 12.4 `config.json` (created in `Stardew Valley/Mods/VNApproach` on first run)

| Setting | Default | Meaning |
|---|---|---|
| `Enabled` | true | master switch |
| `CheckEverySeconds` | 2 | how often rules are evaluated |
| `RunSpeed` | 4 | running speed (normal NPC speed is 2) |
| `MaxPathLength` | 60 | longer routes are ignored |
| `MinSecondsBetweenApproaches` | 90 | pause between two approaches |
| `ReturnToPlace` | true | walk back afterwards |
| `ReturnSpeed` | 3 | speed on the way back |
| `ReturnTimeoutSeconds` | 30 | after this the villager is placed on its tile |
| `ScheduleGuardMinutes` | 40 | do not start if a schedule stop is this close; 0 = off |
| `Verbose` | false | log every decision |

### 12.5 Console commands (SMAPI console)

- `ci_reload`: re-read all add-on files.
- `vn_approach <npc> [openMenu true|false] [force]`: make a villager in your room run to you now.
- `vn_approach_rules`: shows for each villager and rule why it does or does not fire (cooldown, conditions false, schedule too close, READY).

---

## 13. Validate your work

From the folder that contains `tools/`: `python tools/validate.py`

It reads every JSON file (comments allowed) and reports:
- broken JSON,
- duplicate action `Id`s,
- a villager file not listed in `interact.json`,
- a flag that is checked in a Condition but never set,
- a letter used by `AddMail` that is missing from `mail.json`, or a letter in `mail.json` no chapter sends,
- a missing `Ch1/2/3` action for a listed villager,
- `rules.json` Chapter 1-3 Conditions that differ from the villager files.

It prints `OK: everything agrees.` when everything is fine.

---

## 14. Troubleshooting

| Symptom | Likely cause |
|---|---|
| Nothing from the add-on loads | missing or extra comma/quote; SMAPI prints the file name in red |
| A menu entry never shows | a Condition is false, `Limit` used up today, wrong name in `Npcs`, typo in a flag name |
| Chapter never shows | not enough hearts or chats, or previous chapter's flag was never set |
| Chapter replays forever | the last step lost its `SetFlags` |
| Villager never runs to you | VNApproach not built/installed, or `rules.json` Conditions differ from the chapter's, or a guard in 12.3 applies. Run `vn_approach_rules` |
| Letter never arrives | `AddMail` key differs from `mail.json`, game not restarted, or chapter not finished |
| `[missing text]` | a broken `i18n:` key (this add-on uses none) |
| Reward missing | wrong item id; use the `(O)66` form |
| Menu entry vanished | `Limit` working; `Day` returns tomorrow |
| Two `Ask`/`Random`/`If` in one step | only the first runs; put each in its own step |
| `Call` does nothing | script name does not match exactly (case sensitive) |

---

## 15. Rules for an AI assistant editing these files

1. Return the COMPLETE edited file, never fragments, so nothing is lost.
2. Keep valid JSON. Count commas. Escape inner quotes as `\"`.
3. Keep every existing `Id`, flag name, counter name and `AddMail` key unless the user asks to change it. If you rename something, rename it in every file listed in section 3, rule 1.
4. Never delete or reorder the final step of a chapter.
5. Do not change chapter `Conditions` without making the identical change in `assets/rules.json`.
6. Daily repeatable `Friendship` must be 10 or less.
7. Do not invent step types, fields or condition names. Use only what is written in section 5.
8. Do not touch `.cs` files for dialogue work.
9. Tell the user to run `python tools/validate.py` and, for `Addons` edits, `ci_reload` (for `assets/` edits: restart the game).

### Prompt to paste with this file

> I am attaching `HOW_TO_ADD_DIALOGUE.md` and the file(s) I want changed. Follow the guide exactly. Return the COMPLETE edited file(s), not fragments. Keep every `Id`, flag name and `AddMail` key unless I ask otherwise. Keep valid JSON (commas!). Keep daily Friendship at 10 or less. My request: ...
