# The Vesper Toast: Medieval Assassin Mystery (prototype)

A self-contained third-person detective prototype. It shares no code, data or scenes with the
"Prevent the Murder" prototype in `Assets/Scripts`: everything lives in this folder, under the
`AssassinMystery` namespace, in its own assemblies (`AssassinMystery`, `AssassinMystery.Editor`).

## Play
Open `Scenes/AssassinMystery.unity` and press Play.

Controls: **WASD** move, **Mouse** look, **Shift** hurry, **E** talk / search, **Tab** (or N) notebook,
**Esc** close panel / pause.

## Regenerate
- **Tools > Assassin Mystery > Build Scene** rebuilds the whole scene (overwrites the .unity file; data assets are kept).
- **Tools > Assassin Mystery > Regenerate Mystery Data** resets characters, clues and the mystery to the defaults
  in `Editor/MysteryContent.cs`. After that, the assets in `Data/` are the source of truth; edit them in the Inspector.

No manual Inspector wiring is needed: the builder sets every reference, and the UI is built in code at runtime.

## Tuning (Inspector, on the `Systems` object)
- `GameTimer` > Duration Override: seconds (0 = the mystery's 600). Use e.g. 30 to test the failure ending.
- `AccusationManager` > penalty seconds (60), max failed accusations (3), key evidence required to convict (3).
- `MysteryData` asset > duration, key evidence list, solution text.

## Structure
| Folder | Contents |
|---|---|
| `Scripts/Data` | `SuspectData` (identity, looks, dialogue topics), `ClueData`, `MysteryData` (cast, assassin, key evidence) |
| `Scripts/Core` | `GameManager` (flow, pause, restart, endings), `GameTimer`, `GameInput` |
| `Scripts/Player` | `ThirdPersonController`, `ThirdPersonCamera`, `PlayerInteractor` |
| `Scripts/Interaction` | `Interactable` base, `SearchableObject` |
| `Scripts/NPC` | `NPCDialogue` |
| `Scripts/Investigation` | `InvestigationManager` (clues, conversations, marks), `DialogueManager`, `AccusationManager` |
| `Scripts/Sequence` | `AssassinationSequence` (both ending cutscenes) |
| `Scripts/UI` | `UIManager` + views (dialogue, inspect, notebook, accusation, menus) |
| `Editor` | `MysteryContent` (the case), `CastleSceneBuilder` (the castle) |

## How an accusation is judged
The right suspect **and** at least 3 of the mystery's key evidence clues must be presented. Every failure gives the
same message (so it never reveals whether the suspect was right), costs time and one of 3 attempts. The solution
and a spoiler-free list of red herrings are documented at the top of `Editor/MysteryContent.cs`.

## Known limitations
- Characters are primitive figures with procedural bob / turn animation, not rigged models.
- No audio.
- The scene is not in Build Settings (kept separate on purpose). Restart works in the Editor without it; add the
  scene to Build Settings before making a player build.
