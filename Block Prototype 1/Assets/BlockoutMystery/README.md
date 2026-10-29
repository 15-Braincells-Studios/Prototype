# Murder Mystery – Blockout Prototype

A blockout built only from primitive shapes and flat placeholder materials. Your job is to stop a poisoning by
putting the antidote on the right plate, then tell the Butler who the culprit and the victim are.

This prototype is fully separate from the other prototypes in this project. All of it lives in this folder: its own
namespace (`BlockoutMystery`), its own assemblies (`BlockoutMystery`, `BlockoutMystery.Editor`) and its own scenes. It
uses no code, data or scenes from the other prototypes.

## Variants
There are five variants. They share the same level, puzzle, dialogue text and code; only the camera and the way
dialogue is shown differ. Each one has its own scene in `Scenes/`:

| # | Scene: Blockout Murder Mystery - ... | Camera | Dialogue | While someone talks |
|---|---|---|---|---|
| 1 | **Speech Bubbles** | Third person, V for first person | Speech bubble above the speaker; long lines are split into short bubbles | You keep control |
| 2 | **Subtitles** | Third person, V for first person | Standard subtitle box at the bottom of the screen | You keep control |
| 3 | **Visual Novel** | Third person, V for first person | A big text box with a name tab at the bottom, with a live portrait of the speaker standing on it at the left; the world stays visible | You stand still |
| 4 | **Close-up Subtitles** | Third person, V for first person | The camera moves in on the speaker's face, letterbox bars slide in, and the line shows as a subtitle | You stand still |
| 5 | **Isometric Speech Bubbles** | Top-down isometric (orthographic, fixed angle, wheel to zoom), no ceilings, walls cut down to 1.3 m | Speech bubbles (as in 1) | You keep control |

**Lines:**
- When you keep control, a line stays up until you press **E** again (or Esc). Long bubble lines show the next part
  each time you press E. A speech bubble also closes when you walk more than 6 m away from the speaker
  (`DialogueSystem > Bubble Walk Away Distance`).
- If you aim at someone or something else and press E, you interact with that instead, and their line replaces the
  current one.
- In the visual novel and close-up variants, E, Space, Enter or a click continues.
- The Butler's intro always holds the player still, in every variant.

## Run it
1. Open a scene from `Scenes/` and press Play. The bottom-left corner of the screen shows which variant you are
   playing.
2. To rebuild the scenes, use **Tools > Blockout Murder Mystery > Build All Variants**, or one of the
   **Build Scene - ...** items. This overwrites the scene files, so make changes in `Editor/BlockoutSceneBuilder.cs`,
   not in the scenes. The variant list is `BlockoutSceneBuilder.Variants`.

The scenes are not in Build Settings, which keeps them separate from the other prototypes. Restart still works in the
Editor. For a player build, pass a scene to the build directly, or add it to a Build Profile.

## Controls
| Key | Action |
|---|---|
| WASD | Walk (relative to the camera) |
| Mouse | Look around while walking (variants 1–4) |
| V | Switch between third and first person (variants 1–4) |
| Mouse wheel | Zoom (isometric variant) |
| E | Interact with what the cursor dot is on: talk, inspect, open / close, pick up. Also continues or closes a line, and puts the inspected object back or closes the book |
| Hold mouse + drag | Rotate the inspected object |
| Click | Take something inside the inspected object (the key) |
| Esc | Put the inspected object back, close a panel or a line (in the report's evidence picker: back to the report) |
| Q | Open or close the journal (works at any time) |
| A / D | Turn pages in the book, switch tabs in the journal |

## The loop
1. At the start the Butler gives his intro. You can't move during it.
2. The timer starts when the intro ends.
3. Talk to people (their lines go into the journal) and inspect the 7 statuettes. One of them, picked at random on
   every start, hides the key. Turn the statuette over to see inside it, then click the key.
4. Open the closet in the Kitchen. The door says "Locked" until you have the key, and the key is used up when you
   open it. Read the book, open the drawer and take the antidote. Evidence you find goes into the journal too: the
   Green Nightleaf page of the book (which also says that silverleaf tonic, the antidote, neutralises the poison when
   mixed into the food), the herbs in the drawer, the antidote and the key. The drawer can be closed again.
5. Inspect a plate on the kitchen island and drag the antidote from the inventory bar onto it. There is one portion,
   so this choice is final.
6. Talk to the Butler. Before the antidote is used he gives his reminder ("Investigate the murder plot, prevent it and
   identify both the culprit and victim. Finally, report back to me."). After that, the report table opens: tick one
   suspect and one victim, press **Select evidence**, press **Select** on every journal entry that backs it up, then
   **Done** and submit.
7. A popup tells you whether you won or lost, and grades your evidence (see *Report quality*). Both versions have a Restart button, which reloads the scene with a new
   key statuette, a fresh timer and an empty inventory and journal.

Solution (fixed): suspect **Chef**, victim **Prince**, poisoned plate **green herbs**. Strongest evidence: the
**guard's testimony** and the **herbs in the closet drawer**, plus the **herb book** or the **cook's testimony**.

## The journal (Q)
Everything of note is written down automatically, in four tabs:

- **Objectives:** the Butler's two tasks, "Prevent the assassination" and "Report back to the butler with your
  findings", each with a short description. They are static: the game never ticks them off.

- **Evidence:** what the game thinks is important, sorted into *physical evidence* (the key, the antidote, the herbs in
  the drawer), *discovered facts* (the Green Nightleaf page) and *noteworthy statements* (a summary of the lines that
  matter, red herrings included). Each entry says where it came from.
- **Conversations:** one transcript per person ("Talked to the Chef"). Click it to expand it and see every line they
  said, as "Chef: line".
- **Bookmarks:** press **Mark** next to a piece of evidence or a single line to copy it here; **Unmark** (or pressing
  Marked again) removes it.

Which lines count as noteworthy, and their summaries, are set per person in `CaseFile.Cast`.

## Report quality
The evidence handed in with the report is graded on its own, apart from whether the suspect and victim are right
(winning still depends only on the plate, the suspect and the victim). Simple prototype rules, in
`CaseFile.JudgeEvidence`:

| Evidence that supports the case | Strength |
|---|---|
| Herb book (Green Nightleaf page) | Weak: it doesn't say who poisoned the meal |
| Guard 1's testimony (the Chef gathered herbs himself) | Moderate: gathering herbs could be normal kitchen work |
| The green herbs in the closet drawer | Moderate |
| Cook 2's testimony (the Chef complains about the Prince) | Weak |

- **Strong case:** at least three supporting pieces, including the guard's testimony and the herbs in the drawer.
- **Plausible but incomplete case:** at least two supporting pieces, one of them a testimony that points at the Chef
  (the guard's or the cook's), but not a strong case.
- **Insufficient evidence:** anything less, for example the herb book and the herbs without a testimony.
- Everything else is irrelevant and never strengthens the case. One irrelevant pick is ignored; two or more drop the
  grade one step. A statement and the line it came from count as the same piece.

## Where each system lives (`Scripts/`)
| System | File(s) |
|---|---|
| Which variant this scene is (dialogue style, camera) | `Core/PrototypeVariant.cs` |
| Game state, modes, Q and Esc routing, key randomizing, ending, restart | `Core/GameFlow.cs` |
| Case data: cast, lines, book pages, solution, herb colours | `Core/CaseFile.cs` |
| Timer (counts up, no limit) | `Core/GameTimer.cs` |
| Input | `Core/GameInput.cs` |
| Interaction base and the E prompt | `Interaction/Interactable.cs`, `Player/PlayerInteractor.cs` |
| Interactables | `Interaction/Npc.cs`, `Statuette.cs`, `HiddenKey.cs`, `Plate.cs`, `ClosetDoor.cs`, `Drawer.cs`, `AntidotePickup.cs`, `HerbBook.cs` |
| Inspect view (rotate, click, drop target) | `Systems/InspectSystem.cs`, `Interaction/Inspectable.cs` |
| Inventory | `Systems/Inventory.cs` + `UI/InventoryBar.cs` (floating slots, drag and drop) |
| Dialogue flow (pages, free vs. blocking lines) | `Systems/DialogueSystem.cs` (with the `IDialoguePresenter` interface) |
| Dialogue presenters | `UI/DialogueBox.cs` (subtitles), `UI/SpeechBubbleView.cs`, `UI/VisualNovelView.cs`, `UI/CloseUpPresenter.cs`; typing effect in `UI/Typewriter.cs` |
| Journal (the class is still called `Notebook`, which the scenes reference) | `Systems/Notebook.cs` + `UI/NotebookPanel.cs`; noteworthy lines are marked in `Core/CaseFile.cs` |
| Reporting to the Butler | `Systems/ReportSystem.cs` + `UI/ReportPanel.cs` |
| End screen | `UI/EndPopup.cs` |
| HUD (timer, prompt, messages), UI setup | `UI/HudView.cs`, `UI/UIRoot.cs`, `UI/UIKit.cs` |
| Player and cameras | `Player/PlayerMotor.cs`, `Player/FollowCamera.cs` (third/first person, close-up shots), `Player/IsoCamera.cs` |

All UI is built in code at runtime, so nothing needs to be wired up in the Inspector. `UIRoot` picks the dialogue
presenter from the scene's `PrototypeVariant`.

## Testing
- For testing, set `GameFlow > Force Key Statuette` on the `Systems` object to put the key in a fixed statuette.
- Dialogue lines, book pages, objectives, the solution and the report-quality rules are in `Core/CaseFile.cs`. NPCs read
  their line from there at runtime, so changing a line doesn't need a scene rebuild.
- To try another combination, change `Dialogue` or `View` on the `Systems > PrototypeVariant` component in a scene.
  To make it a permanent variant, add it to `BlockoutSceneBuilder.Variants`.

## Assumptions (where the spec was silent)
- **Camera:** the mouse moves the third-person camera while you walk, with the cursor locked. In the inspect view and
  in panels the system cursor is free. The cursor state is re-applied every frame, so a click in the Game view or
  Escape doesn't bring the system cursor back while you walk.
- **Cursor dot:** while you walk, a small dot in the centre of the screen is the cursor. You interact with the first
  thing under it (walls block), if you are within reach; a ring appears around the dot when there is something to
  use. Interactables built without a collider (the book, the antidote) get a trigger box at runtime so the dot can hit
  them.
- **Isometric interaction:** the system cursor is hidden and the dot follows the mouse instead; you interact with what
  it is on, if the character is within reach.
- **Inspect view:** the held object and its dim backdrop are drawn by an overlay camera (URP camera stack) on the
  built-in Water layer. Walls close to you in first person can never cut into the object, and it looks the same in
  every camera mode.
- **Isometric inspect view:** the camera switches to perspective while you hold something, then switches back.
- **Visual novel portrait:** it is the blockout NPC itself, rendered live by a second camera that only sees the
  speaker. The speaker is moved to the TransparentFX layer while they talk.
- **Timer:** it only counts up and has no time limit. The end popups show the time taken.
- **Report table:** it lists the 9 people other than the Butler. The same person may be ticked as both suspect and
  victim; that is simply a wrong answer.
- **Restart:** it reloads the scene, which resets everything.
- **Evidence:** the picker is a copy of the journal without Objectives (it opens on Bookmarks when there are any). Any
  number of pieces of evidence and single lines can be selected; at least one is needed to submit.
- **Losing:** you lose if the plate, the suspect or the victim is wrong. The evidence grade is shown either way. The popup explains what went wrong and shows
  the solution.
- **Closet door:** it shows "Locked" without the key. The key is removed from the inventory when the door opens.
- **Key:** it is the only clickable thing in the inspect view, and it can only be clicked when you can see it,
  through the open bottom.
- **Antidote drop:** you can drop it anywhere on the plate's area on screen.
- **Extras:** name tags above the NPCs and signs over the doorways (room names on the floor in the isometric variant),
  so you can find your way in the blockout.
- **NPCs:** they stand still and do not turn to face you.

## Known limitations
- Characters are capsules with primitive hats and props; nothing is animated.
- There is no audio.
- The full loop has been tested in the Editor by calling the game's methods directly (including the real UI drag
  events). It has not been played by hand with a keyboard and mouse. That includes the E-press rules for lines you
  can walk away from.
