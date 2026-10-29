using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PreventTheMurder.EditorTools
{
    /// <summary>
    /// Generates the "Vesper Bell" scenario assets (characters, clues, dialogue, scenario).
    /// After generation the assets are the source of truth: edit them in the Inspector.
    /// Re-running overwrites them in place (GUIDs and scene references are kept).
    /// To add another story, duplicate the Scenario asset (or write a sibling of this class) and assign it on GameManager.
    /// </summary>
    public static class ScenarioAuthoring
    {
        public const string Root = "Assets/Data/Scenarios/VesperBell";
        public const string ScenarioPath = Root + "/Scenario_VesperBell.asset";

        // Spot ids shared with CastlePrototypeBuilder.
        public const string SpotDeskDrawerLeft = "solar_desk_drawer_left";
        public const string SpotDeskDrawerRight = "solar_desk_drawer_right";
        public const string SpotChest = "solar_chest";
        public const string SpotWineCabinet = "solar_wine_cabinet";
        public const string SpotCupboardTop = "kitchen_cupboard_drawer_top";
        public const string SpotCupboardBottom = "kitchen_cupboard_drawer_bottom";
        public const string SpotHallNote = "hall_table_note";

        public static ScenarioData GetOrCreate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ScenarioData>(ScenarioPath);
            return existing ? existing : Generate();
        }

        [MenuItem("Tools/First Person - First Test/Regenerate Vesper Bell Scenario Data")]
        static void RegenerateMenu()
        {
            if (EditorUtility.DisplayDialog("Regenerate scenario data",
                    "Overwrite the Vesper Bell character, clue, dialogue and scenario assets with the generated defaults?",
                    "Overwrite", "Cancel"))
                Generate();
        }

        public static ScenarioData Generate()
        {
            EnsureFolder(Root + "/Characters");
            EnsureFolder(Root + "/Clues");
            EnsureFolder(Root + "/Dialogue");

            // ---------------- Characters
            var king = Character("king_aldric", "King Aldric", "The King", new Color(0.95f, 0.78f, 0.2f));
            var queen = Character("queen_isolde", "Queen Isolde", "The Queen", new Color(0.58f, 0.32f, 0.78f));
            var pruitt = Character("brother_pruitt", "Brother Pruitt", "Court Physician", new Color(0.55f, 0.38f, 0.22f));
            var colin = Character("duke_colin", "Duke Colin", "The King's Nephew", new Color(0.18f, 0.55f, 0.3f));
            var dalton = Character("mistress_dalton", "Mistress Dalton", "Head Cook", new Color(0.92f, 0.52f, 0.28f));
            var rowan = Character("captain_rowan", "Captain Rowan", "Captain of the King's Guard", new Color(0.28f, 0.45f, 0.8f), suspect: false);

            // ---------------- Clues: objects
            var decree = Clue("clue_decree", "Draft decree of succession", ClueCategory.Motive, colin,
                "The King's new decree strikes Duke Colin from the succession.",
                "A draft decree in the King's own hand, naming a new heir. The name \"Duke Colin\" is struck through with a hard line. The space for the royal seal is still blank.");
            var nightshade = Clue("clue_nightshade", "Pouch of nightshade powder", ClueCategory.Method, null,
                "Nightshade pouch in the kitchen cupboard, recently half emptied.",
                "A leather pouch marked \"For the rats\". Grey nightshade powder inside, but much of it is gone, and the dust on the pouch is freshly smudged.");
            var debt = Clue("clue_debt_note", "Moneylender's debt note", ClueCategory.Motive, colin,
                "Duke Colin owes 3,000 crowns he cannot pay.",
                "\"Duke Colin owes three thousand crowns to Silas Vane, moneylender. Due by the new moon, or his lands are forfeit.\" Signed in Colin's hand.");
            var goblet = Clue("clue_goblet", "The King's goblet", ClueCategory.Method, king,
                "The King's goblet has a bitter grey residue.",
                "The King's silver goblet, set out for the Vesper toast. A faint grey film clings to the bottom, and it smells bitter, like crushed leaves.");
            var memo = Clue("clue_torn_memo", "Torn memo", ClueCategory.Timeline, king,
                "Someone noted the King drinks his wine at Vesper.",
                "A torn scrap of parchment in a hurried hand: \"The King takes his wine at Vesper, always. The cup waits in the Solar until then.\"");
            var draught = Clue("clue_sleeping_draught", "Brother Pruitt's sleeping draught", ClueCategory.Method, pruitt,
                "Pruitt's sleeping draught for the King, seal unbroken.",
                "A small vial labelled \"For His Majesty: one drop for sleep. Br. Pruitt.\" The wax seal has never been broken.");

            // ---------------- Clues: dialogue
            var daltonSawColin = Clue("clue_dalton_saw_colin", "Colin at the cupboard", ClueCategory.Opportunity, colin,
                "Dalton saw Colin alone at the kitchen cupboard.",
                "Mistress Dalton found Duke Colin alone in the kitchens this afternoon, with her spice cupboard standing open.");
            var pruittSuccession = Clue("clue_pruitt_succession", "The King's confession", ClueCategory.Motive, king,
                "Pruitt: the King will change the succession tonight.",
                "Brother Pruitt says the King confided that he will change the succession tonight.");
            var vesperWine = Clue("clue_vesper_wine", "Wine at Vesper", ClueCategory.Timeline, king,
                "Pruitt: the King drinks his wine at Vesper, sharp.",
                "Brother Pruitt confirms the King drinks one goblet of wine at the Vesper bell, without fail.");
            var queenArgument = Clue("clue_queen_argument", "Overheard argument", ClueCategory.Motive, colin,
                "Isolde heard Colin promise a stranger money by the new moon.",
                "Queen Isolde overheard Duke Colin arguing about money with a stranger: \"You will have it all by the new moon.\"");
            var kingSigning = Clue("clue_king_signing", "The King's plan", ClueCategory.Motive, king,
                "The King signs his new decree at the Vesper toast.",
                "King Aldric will sign the new decree at the Vesper toast and name his new heir before the court.");
            var colinBlames = Clue("clue_colin_blames_pruitt", "Colin points at Pruitt", ClueCategory.Opportunity, pruitt,
                "Colin claims Pruitt mixes strange potions for the King.",
                "Duke Colin was quick to say Brother Pruitt is always mixing potions for the King.");
            var colinDebtReply = Clue("clue_colin_debt_reply", "Colin's slip", ClueCategory.Motive, colin,
                "Colin says his debts will be settled 'very soon'.",
                "Confronted with the debt note, Duke Colin snapped that his debts will be settled \"very soon\".");

            // ---------------- Dialogue
            var kingDialogue = Dialogue("Dialogue_KingAldric",
                new[] { L("Ah, a visitor. Be brief, I have a decree to finish before Vesper.") },
                T("Ask about the decree",
                    L("I name a new heir tonight. My nephew Colin has wasted his last chance."),
                    L("I sign it at the Vesper toast, before the whole court.", kingSigning)),
                T("Ask if he feels safe",
                    L("Safe? I am the King, and Captain Rowan guards the hall."),
                    L("Do not trouble me with shadows. Go and enjoy the feast.")),
                T("Ask about the Vesper toast",
                    L("One goblet of my good red at the Vesper bell. Twenty years, never missed.")));

            var queenDialogue = Dialogue("Dialogue_QueenIsolde",
                new[] { L("Good evening. The hall feels restless tonight.") },
                T("Ask about last night",
                    L("I slept poorly. Aldric sat up late, writing by candlelight.")),
                T("Ask about Duke Colin",
                    L("My husband's nephew is charming, when he wants something."),
                    L("This morning I heard him in the courtyard, arguing with a stranger about money."),
                    L("He said, \"You will have it all by the new moon.\" I did not like his tone.", queenArgument)),
                T("Ask about the King",
                    L("He is stubborn, but he means well. He has big news for the court tonight.")));

            var pruittDialogue = Dialogue("Dialogue_BrotherPruitt",
                new[] { L("Peace be with you. Are you unwell?") },
                T("Ask about the King",
                    L("His Majesty confided in me. He means to change the succession tonight.", pruittSuccession),
                    L("I fear it will make him enemies.")),
                T("Ask about the King's wine",
                    L("One goblet at the Vesper bell, sharp. I advised it for his heart.", vesperWine)),
                TR("Ask about the sleeping draught", draught,
                    L("My sleeping draught? I gave it to His Majesty a week ago."),
                    L("The seal is unbroken, you say? Then he has not touched it. Good.")));

            var colinDialogue = Dialogue("Dialogue_DukeColin",
                new[] { L("What do you want? I am a busy man.") },
                T("Ask about last night",
                    L("I was at cards until late. Ask anyone.")),
                T("Ask about the King",
                    L("My dear uncle? We are very close."),
                    L("If you want someone to watch, watch Brother Pruitt. He is forever mixing potions for the King.", colinBlames)),
                T("Ask about the kitchens",
                    L("I went down for a bite to eat. Is that a crime now?")),
                TR("Confront him with the debt note", debt,
                    L("Where did you get that? Give it here!"),
                    L("My debts will be settled soon enough. Very soon.", colinDebtReply)));

            var daltonDialogue = Dialogue("Dialogue_MistressDalton",
                new[] { L("Mind the hearth, it's hot. What is it?") },
                T("Ask about today",
                    L("Busy as ever. The Vesper feast won't cook itself."),
                    L("Odd thing, though. I came back from the cellar and found Duke Colin alone in here."),
                    L("He had my cupboard open. Said he was looking for honey cakes.", daltonSawColin)),
                T("Ask about the cupboard",
                    L("Spices, salt, and the rat bane. I keep it all in there."),
                    L("You found the rat bane half gone? I filled that pouch only yesterday!", null, nightshade)),
                T("Ask about the King's wine",
                    L("I don't serve the King's wine. His goblet stays up in the Solar till Vesper.")));

            // ---------------- Scenario
            var scenario = ScriptableObject.CreateInstance<ScenarioData>();
            scenario.title = "The Vesper Bell";
            scenario.intro = "Whispers in the castle say blood will be spilled when the Vesper Bell tolls.\n\nFind out who is in danger, who means them harm, and how. Then tell Captain Rowan at the guard post before the bell rings.\n\n<size=80%>[WASD] Move   [E] Interact   [J] Notepad   [Esc] Pause</size>";
            scenario.timeLimitSeconds = 15 * 60;
            scenario.clockLabel = "Until the Vesper Bell";
            scenario.cast = new List<CastEntry>
            {
                Cast(king, kingDialogue),
                Cast(queen, queenDialogue),
                Cast(pruitt, pruittDialogue),
                Cast(colin, colinDialogue),
                Cast(dalton, daltonDialogue),
                Cast(rowan, null),
            };
            scenario.victim = king;
            scenario.culprit = colin;
            scenario.methodOptions = new List<string>
            {
                "Stabbed in the kitchens",
                "Poisoned wine",
                "Pushed from the tower stairs",
                "Smothered in his sleep",
            };
            scenario.correctMethodIndex = 1;
            scenario.cluePlacements = new List<CluePlacement>
            {
                Place(SpotDeskDrawerLeft, decree),
                // SpotDeskDrawerRight intentionally empty (red herring).
                Place(SpotChest, draught),
                Place(SpotWineCabinet, goblet),
                Place(SpotCupboardTop, nightshade),
                Place(SpotCupboardBottom, debt),
                Place(SpotHallNote, memo),
            };
            scenario.allClues = new List<ClueData>
            {
                decree, nightshade, debt, goblet, memo, draught,
                daltonSawColin, pruittSuccession, vesperWine, queenArgument, kingSigning, colinBlames, colinDebtReply,
            };
            scenario.reportPrompt = "\"Speak plainly. Who is in danger, who means them harm, and how?\"";
            scenario.partialMessage = "The Captain frowns. \"Your story does not add up. Bring me facts.\"";
            scenario.wrongMessage = "The Captain scowls. \"Idle accusations waste my time. Bring me proof.\"";
            scenario.winTitle = "The murder is prevented";
            scenario.winText = "The guards reach the King in time. {victim} lives, and {culprit} is seized.";
            scenario.loseTitle = "The Vesper Bell tolls";
            scenario.loseText = "King Aldric raises his goblet for the toast and drinks deep. The wine is laced with nightshade.\n\nThe King falls dead before the bell stops ringing, and Duke Colin is nowhere to be found.\n\n<b>Victim:</b> King Aldric    <b>Culprit:</b> Duke Colin    <b>Method:</b> Poisoned wine";

            scenario = Save(scenario, ScenarioPath);
            AssetDatabase.SaveAssets();
            return scenario;
        }

        // ---------------- Helpers

        static CharacterData Character(string id, string displayName, string role, Color color, bool suspect = true)
        {
            var c = ScriptableObject.CreateInstance<CharacterData>();
            c.id = id;
            c.displayName = displayName;
            c.role = role;
            c.color = color;
            c.isSuspect = suspect;
            return Save(c, $"{Root}/Characters/{displayName.Replace(" ", "")}.asset");
        }

        static ClueData Clue(string id, string title, ClueCategory category, CharacterData pointsTo, string summary, string description)
        {
            var c = ScriptableObject.CreateInstance<ClueData>();
            c.id = id;
            c.title = title;
            c.category = category;
            c.pointsTo = pointsTo;
            c.summary = summary;
            c.description = description;
            return Save(c, $"{Root}/Clues/{id}.asset");
        }

        static DialogueLine L(string text, ClueData unlocks = null, ClueData requires = null) =>
            new DialogueLine { text = text, clueToUnlock = unlocks, requiredClue = requires };

        static DialogueTopic T(string title, params DialogueLine[] lines) => TR(title, null, lines);

        static DialogueTopic TR(string title, ClueData requiredClue, params DialogueLine[] lines) =>
            new DialogueTopic { title = title, requiredClue = requiredClue, lines = new List<DialogueLine>(lines) };

        static DialogueData Dialogue(string fileName, DialogueLine[] greeting, params DialogueTopic[] topics)
        {
            var d = ScriptableObject.CreateInstance<DialogueData>();
            d.greeting = new List<DialogueLine>(greeting);
            d.topics = new List<DialogueTopic>(topics);
            return Save(d, $"{Root}/Dialogue/{fileName}.asset");
        }

        static CastEntry Cast(CharacterData character, DialogueData dialogue) => new CastEntry { character = character, dialogue = dialogue };

        static CluePlacement Place(string spotId, ClueData clue) => new CluePlacement { spotId = spotId, clue = clue };

        /// <summary>Creates the asset, or overwrites an existing one in place so references stay valid.</summary>
        static T Save<T>(T asset, string path) where T : ScriptableObject
        {
            asset.name = Path.GetFileNameWithoutExtension(path);
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing)
            {
                EditorUtility.CopySerialized(asset, existing);
                Object.DestroyImmediate(asset);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
