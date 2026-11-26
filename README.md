# VN Talks (merged)

Two folders:

| Folder | Goes to |
|---|---|
| `Addons/VN_Talks` | `Stardew Valley/Mods/character_interact/Addons/` |
| `Mods/VNApproach` | `Stardew Valley/Mods/` after building |

`Mods/VNApproach` is ONE mod: the C# code (ModEntry.cs, ModConfig.cs, ApproachRule.cs), the letters (`assets/mail.json`) and the chapter rules (`assets/rules.json`). It no longer needs Content Patcher.

## Build
1. Install the .NET 6 SDK.
2. In `Mods/VNApproach` run `dotnet build -c Release`.
3. ModBuildConfig copies the finished mod (DLL + manifest.json + assets) into `Stardew Valley/Mods/VNApproach`.
   Add `<EnableModZip>true</EnableModZip>` if you want a zip instead.

Then delete the old `[CP] Visual Novel Talks` folder and the old VNApproach mod, so nothing loads twice.

Dialogue guide (also meant to be handed to ChatGPT or another AI): `HOW_TO_ADD_DIALOGUE.md`.

Consistency check after any edit: `python tools/validate.py`.
"# fluffy-dollop" 
