using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssassinMystery.EditorTools
{
    /// <summary>
    /// Writes the "Vesper Toast" case: 11 characters, 17 clues and the solution. After generation the assets are
    /// the source of truth and can be edited in the Inspector; re-running overwrites them in place (GUIDs kept).
    ///
    /// The case, for whoever edits it:
    ///   Target   Queen Isolde.
    ///   Assassin Master Corwin Vale, Castle Treasurer.
    ///   Motive   He has embezzled from the treasury and borrowed 400 crowns from Lady Seraphine to hide it. The Queen
    ///            announces an audit at tonight's toast (Lord Halvard drafted the decree).
    ///   Means    Wolfsbane, taken from Master Ilric's unlocked reagent cabinet, poured into the Queen's private cask.
    ///   Opportunity  He left the library after the third bell (Ilric), signed out the cellar key in green ink
    ///            (Gunnar's ledger) while the cellar patrol was pulled for the feast (patrol roster), and came back
    ///            dusty (Agnes). His green-ink order slip makes sure only the Queen's cask is poured.
    ///   False alibi  "In the library all afternoon, never left."
    ///   Red herrings Duke Edric (heir, angry note: about his marriage; alibi with the Captain), Dr Mordaunt
    ///            (dismissed, knows poisons; his stock is complete; alibi via Agnes), Ilric (his poison; he is the
    ///            witness), Lady Seraphine (rival; wants Vale exposed, not the Queen dead), Lord Halvard (pulled the
    ///            patrol; secret marriage talks with Seraphine), Chef Bertrand (handles the wine; sells bottles on the
    ///            side), Gunnar (forging daggers: the Captain's order).
    /// </summary>
    public static class MysteryContent
    {
        public const string Root = "Assets/AssassinMystery/Data";
        public const string MysteryPath = Root + "/Mystery_VesperToast.asset";

        [MenuItem("Tools/Third Person - First Test/Regenerate Mystery Data")]
        static void RegenerateMenu()
        {
            if (EditorUtility.DisplayDialog("Regenerate mystery data",
                    "Overwrite the Vesper Toast characters, clues and mystery with the generated defaults?", "Overwrite", "Cancel"))
                Generate();
        }

        public static MysteryData GetOrCreate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MysteryData>(MysteryPath);
            return existing ? existing : Generate();
        }

        public static MysteryData Generate()
        {
            Directory.CreateDirectory(Root + "/Suspects");
            Directory.CreateDirectory(Root + "/Clues");
            AssetDatabase.Refresh();

            // ---------- characters (identity first, dialogue after the clues exist) ----------
            var queen = Person("queen", "Queen Isolde", "Queen of Aldmere",
                "The intended target? She refuses to cancel the feast or her toast.",
                "#E9E1CF", "#C9A13B", "#E8C7A8", Headwear.Crown, 1.0f, false);
            var advisor = Person("advisor", "Lord Halvard Crane", "Royal Advisor",
                "The Queen's oldest counsellor. Knows every royal plan before it is announced.",
                "#26385E", "#D4AF37", "#D9B28F", Headwear.Skullcap, 1.02f, true);
            var captain = Person("captain", "Captain Brienne Hale", "Captain of the Guard",
                "Commands the castle guard and knows every patrol. She summoned you after the warning note.",
                "#7A1F1F", "#9DA3A8", "#C99872", Headwear.Helmet, 1.05f, false);
            var chef = Person("chef", "Bertrand Moule", "Royal Chef",
                "Rules the kitchens and the stores beneath them. Loud, proud and protective of his wine.",
                "#E6DFD0", "#8B5A2B", "#E3B596", Headwear.ChefHat, 0.95f, false);
            var physician = Person("physician", "Doctor Aldous Mordaunt", "Royal Physician",
                "Treated the royal family for twenty years, until the Queen dismissed him from her service last month.",
                "#1E1E22", "#3F5E3A", "#D8BFA4", Headwear.FlatCap, 1.0f, true);
            var servant = Person("servant", "Mistress Agnes Fell", "Head Servant",
                "Runs the household. Knows every routine, every door and every rumour.",
                "#5B5F66", "#F2EEE4", "#E6C2A4", Headwear.Bonnet, 0.92f, false);
            var blacksmith = Person("blacksmith", "Gunnar Holt", "Royal Blacksmith",
                "Keeps the castle's locks, hinges and blades, and the ledger for the cellar key.",
                "#4A3324", "#2B2B2B", "#B9805C", Headwear.None, 1.12f, true);
            var scholar = Person("scholar", "Master Ilric Thorne", "Court Scholar",
                "Studies old texts and stranger substances. His reagent cabinet holds things best kept locked.",
                "#4B2C6B", "#C9B458", "#E2C6AE", Headwear.WizardHat, 0.98f, true);
            var rival = Person("rival", "Lady Seraphine Dunmore", "Noble Rival",
                "Head of House Dunmore, the Crown's oldest rivals. Wealthy, sharp and openly ambitious.",
                "#1F5E43", "#D9C27A", "#EBCDB5", Headwear.Circlet, 1.0f, false);
            var sibling = Person("sibling", "Duke Edric of Aldmere", "Royal Sibling",
                "The Queen's younger brother and her heir while she has no child. Royal blood, too.",
                "#6B1E46", "#D4AF37", "#E2BFA0", Headwear.Circlet, 1.03f, false);
            var treasurer = Person("treasurer", "Master Corwin Vale", "Castle Treasurer",
                "Keeps the royal accounts. Precise, polite, and always counting.",
                "#3A2A1E", "#2F6B3A", "#DCC0A3", Headwear.FlatCap, 0.97f, false);

            // ---------- clues ----------
            var auditDecree = Clue("audit_decree", "Draft Royal Decree", ClueCategory.Motive,
                "A draft in Lord Halvard's hand, to be read at tonight's toast: \"By order of Queen Isolde, a full audit of the royal " +
                "treasury shall begin at dawn. The Treasurer shall surrender every ledger.\"", treasurer, advisor);
            var debtLetter = Clue("debt_letter", "Letter to the Treasurer", ClueCategory.Motive,
                "\"Corwin. The four hundred crowns. You swore the Crown's coffers would cover it. Repay me before the Queen's audit " +
                "opens your books, or I will tell her myself. S.D.\"", treasurer, rival);
            var keyLedger = Clue("key_ledger", "Cellar Key Ledger", ClueCategory.Opportunity,
                "The book for the cellar key. Today, third bell: \"C. Vale, stores count\", signed in neat green ink. Returned at the fourth bell.",
                treasurer, blacksmith);
            var reagentList = Clue("reagent_list", "Scholar's Reagent Inventory", ClueCategory.Means,
                "Master Ilric's list, pinned inside the cabinet. \"Wolfsbane tincture, 1 vial.\" Its slot is empty, and the cabinet lock " +
                "was never latched.", scholar);
            var emptyVial = Clue("empty_vial", "Empty Vial in a Ledger Page", ClueCategory.Means,
                "Hidden behind the barrels near the Queen's cask: a small empty vial that smells bitter, wrapped in a page torn " +
                "from an account book. The columns of figures are written in green ink.");
            var toastOrder = Clue("toast_order", "Order for the Vesper Toast", ClueCategory.Means,
                "\"By order of the Treasury: the Queen's private cask is to be brought up and poured alone for the vesper toast. " +
                "No other wine is to be served to Her Majesty.\" Green ink, Treasury seal.", treasurer, chef);
            var resealedCask = Clue("resealed_cask", "The Queen's Resealed Cask", ClueCategory.Means,
                "The cask of Aldmere Red kept for the Queen alone. Its wax seal is fresh and still soft. It was opened and resealed " +
                "today, and a drip of wine has dried beside the bung.", chef);
            var poisonInventory = Clue("poison_inventory", "Physician's Poison Case", ClueCategory.Alibi,
                "Doctor Mordaunt's sealed case and its log, checked this morning. Every vial is present and every seal unbroken.", physician);
            var edricNote = Clue("edric_note", "Half-Burnt Note", ClueCategory.Motive,
                "Pulled from the edge of the fire, scorched: \"...she will not rule my life forever. E.\"", sibling);
            var patrolRoster = Clue("patrol_roster", "Guard Patrol Roster", ClueCategory.Opportunity,
                "Today's patrols. The cellar round between the third and fourth bell is struck out: \"Men to the hall doors for the " +
                "feast, by order of Lord Halvard.\" Nobody watched the cellars.", advisor, captain);
            var daggerOrder = Clue("dagger_order", "Blacksmith's Work Order", ClueCategory.Alibi,
                "\"Six daggers for the Castle Guard. Capt. B. Hale.\" Six new blades lie wrapped in oilcloth, all accounted for.",
                blacksmith, captain);
            var wineTally = Clue("wine_tally", "Chef's Secret Tally", ClueCategory.Relationship,
                "A tally of \"spare\" bottles of castle wine sold to a merchant in town, initialled B.M.", chef);
            var succession = Clue("succession_roll", "Line of Succession", ClueCategory.Motive,
                "The royal succession roll, recently consulted. While Queen Isolde has no child, Duke Edric is her heir. " +
                "A margin note in a young hand: \"Can an heir renounce?\"", sibling);
            var ilricTestimony = Clue("testimony_vale_left", "Ilric's Account", ClueCategory.Testimony,
                "Master Ilric says the Treasurer left the library just after the third bell and only came back near the fourth.",
                treasurer, scholar);
            var cobwebs = Clue("testimony_cobwebs", "Cobwebs on the Treasurer", ClueCategory.Testimony,
                "Agnes saw Master Vale cross the hall after the fourth bell with cobwebs on his sleeve and dust on his hem.", treasurer, servant);
            var meetings = Clue("testimony_meetings", "Secret Meetings", ClueCategory.Relationship,
                "Agnes has seen Lord Halvard and Lady Seraphine meet privately in the gallery twice this week.", advisor, rival);
            var greenInk = Clue("testimony_green_ink", "The Green Ink", ClueCategory.Testimony,
                "Agnes says only one person in the castle writes in green ink: Master Corwin Vale.", treasurer, servant);

            // ---------- dialogue ----------
            Talk(queen,
                "So you are the Captain's detective. I will not cancel my feast over an unsigned note. Find your villain before the " +
                "vesper bell, or stand beside me while I drink.",
                T("Please, do not drink the toast.",
                    "Two hundred years of Aldmere queens have raised that cup at midwinter. I will not be the first to be frightened from it."),
                T("Who would want you dead?",
                    "A queen has enemies the way a dog has fleas. My brother sulks, Lady Seraphine schemes, and Doctor Mordaunt has glared " +
                    "at me since I dismissed him. And after tonight's announcement, I expect a few more."),
                T("What announcement?",
                    "That is Lord Halvard's to read, not mine to spoil. Ask him, if you must."));

            Talk(advisor,
                "Detective. The Captain told me she had sent for you. I pray this alarm proves to be nothing.",
                T("What happens at the vesper bell?",
                    "The midwinter toast. Her Majesty drinks the first cup from her own cask, as Aldmere queens always have. After it, " +
                    "she makes an announcement. I am not at liberty to say what."),
                T("What is the announcement?",
                    "...You will pry it out of someone. Very well. A full audit of the royal treasury, starting at dawn. The numbers have " +
                    "not added up for a year. I drafted the decree myself. Few people know."),
                T("Where were you this afternoon?",
                    "Here in the Great Hall all day, arranging the seating and the speech. Mistress Agnes will tell you I was under her feet."),
                T("Do you know Lady Seraphine well?",
                    "Barely. We have exchanged perhaps three words at court, and two of them were \"good evening\"."),
                T("You pulled the guards off the cellar patrol.",
                    "I did. The Captain's men were needed at the hall doors for the feast. The cellars hold barrels and turnips. Who would " +
                    "want barrels? ...Is that a problem?", patrolRoster),
                T("Agnes saw you meeting Lady Seraphine in secret.",
                    "...Discreet woman, Agnes, until she isn't. Yes. Lady Seraphine wants her son married to the Queen's niece, and I have " +
                    "been negotiating terms. It is politics, detective, not murder. I would rather the court did not hear of it.", meetings));

            Talk(captain,
                "Good, you're here. I found the note pinned to the guardroom door at noon: \"Royal blood will be spilled at the vesper " +
                "bell.\" I have no idea who wrote it.",
                T("Who could be in danger?",
                    "Two people of royal blood sleep in this castle: Queen Isolde and her brother, Duke Edric. Either could be the target. " +
                    "I have doubled the guard on the hall doors."),
                T("Where were you this afternoon?",
                    "At the archery butts in the courtyard with Duke Edric, from the third bell to the fourth. He shot poorly. His mind " +
                    "was somewhere else."),
                T("Who can get into the cellars?",
                    "Anyone with the cellar key, which hangs at Gunnar's bench. You sign his ledger to take it. My men usually walk the " +
                    "cellars as well."),
                T("What will you need to make an arrest?",
                    "A name, and proof: motive, means and opportunity. Accuse the wrong person and I lose men to the uproar and you lose " +
                    "time. Do it too often and the real killer walks free."),
                T("Why was the cellar left unguarded?",
                    "Lord Halvard pulled my cellar round for the feast. Between the third and fourth bell nobody walked down there. " +
                    "I argued. I lost.", patrolRoster),
                T("Gunnar is forging daggers for you?",
                    "Six new blades for the guard, yes. Gunnar is slow, but honest. Every one is accounted for.", daggerOrder));

            Talk(chef,
                "Out of my way! The feast is in minutes and the sauce will not reduce itself!",
                T("What will the Queen eat and drink tonight?",
                    "Seven courses, every one tasted by my own boy. But the toast is not mine. Her Majesty drinks only from her private " +
                    "cask of Aldmere Red. Nobody pours that but by order."),
                T("Who ordered the Queen's cask brought up?",
                    "An order slip came down this afternoon. Very official, very fussy: the Queen's cask, poured alone. I left it on my " +
                    "pantry shelf."),
                T("Where were you this afternoon?",
                    "Here! In the kitchen and the stores all day. Ask anyone. Ask the pots."),
                T("Someone has been selling castle wine.",
                    "Ah. Ahem. A few spare bottles to a merchant in town. Spare! Nobody misses them. That is not murder, detective. That " +
                    "is... business.", wineTally),
                T("The Queen's cask was opened and resealed today.",
                    "Resealed? Nobody touches that cask but me, and I have not opened it since the harvest. If the wax is fresh, someone " +
                    "else has been down here.", resealedCask),
                T("The toast order is in green ink.",
                    "Green, black, purple, what do I care? It had the Treasury seal. When the Treasury says jump, the kitchen jumps.", toastOrder));

            Talk(physician,
                "Hm. A detective. There is a stable boy with a fever, and I am being kept here to answer questions.",
                T("Why did the Queen dismiss you?",
                    "She prefers a younger physician from the capital. Twenty years of service, and now I tend stable boys. I am bitter, " +
                    "yes. Bitter is not murderous."),
                T("You know a great deal about poisons.",
                    "Every physician does. My stores are sealed, counted each morning and kept in my satchel. Look for yourself if you must."),
                T("Where were you this afternoon?",
                    "In the stables with the sick boy, from the third bell until the fourth. Mistress Agnes brought me hot water, twice."),
                T("Who else could get hold of poison here?",
                    "Besides me? Master Ilric keeps all manner of reagents for his studies. Foxglove, nightshade, wolfsbane. He is careless " +
                    "with his keys."),
                T("What would wolfsbane do to someone?",
                    "Aconite. A few drops in strong red wine and you would never taste it. Numbness, sweating, then the heart stops within " +
                    "the hour. A cruel thing.", reagentList));

            Talk(servant,
                "Detective. If you need anything, ask. If you need gossip, ask twice.",
                T("What is the Queen's routine for the toast?",
                    "At the vesper bell she rises, her own cask is opened before the hall, and she drinks the first cup from her own goblet. " +
                    "Nobody else drinks from that cask. Ever."),
                T("Where was everyone this afternoon?",
                    "I took hot water to Doctor Mordaunt in the stables, twice. The Duke was at the butts with the Captain. Lord Halvard " +
                    "was under my feet in here all day. Master Vale said he would be in the library with his ledgers."),
                T("Did you notice anything odd today?",
                    "Master Vale came through the hall after the fourth bell with cobwebs on his sleeve and dust on his hem. The library is " +
                    "dusty, I suppose. Not that dusty.", null, cobwebs),
                T("Is anyone at court keeping secrets?",
                    "Everyone, dear. But Lord Halvard and Lady Seraphine think nobody notices them meeting in the gallery. Twice this week, " +
                    "heads together like thieves.", null, meetings),
                T("Who in the castle writes in green ink?",
                    "Green ink? Only Master Vale. He has it sent from the capital. Says it makes his ledgers \"distinctive\".", emptyVial, greenInk));

            Talk(blacksmith,
                "Hrm. Mind the anvil. It's hot.",
                T("Who took the cellar key today?",
                    "They sign the ledger on my bench. I don't read so well. Look yourself."),
                T("What are you forging?",
                    "Daggers. Six of them. Sharp ones. Captain's order. The paper's in the crate."),
                T("Where were you this afternoon?",
                    "Here. Hammering. The forge is loud and the smoke is thick. Someone came for the key after the third bell. Robes. " +
                    "I didn't look up."),
                T("Does anyone down here have secrets?",
                    "Bertrand's little wine trade? Everyone below stairs knows about it but him. His tally's in the old cabinet."),
                T("Master Vale signed for the cellar key.",
                    "Did he now. Odd. The Treasurer counting turnips himself? He always sends a clerk.", keyLedger));

            Talk(scholar,
                "Ah, a visitor! Mind the scrolls, some are older than this castle.",
                T("What do you study?",
                    "Alchemy, herbs, the deeds of old kings. Mostly herbs, lately. Fascinating what a single leaf can do."),
                T("Where were you this afternoon?",
                    "Here, the whole afternoon. Master Vale shared the long table with his ledgers... until just after the third bell, " +
                    "when he went out. He came back near the fourth, rather flushed.", null, ilricTestimony),
                T("Is anything missing from your cabinet?",
                    "Missing? No... well, I have not looked today. Look if you like, the inventory is pinned inside the door."),
                T("Has anyone shown interest in the royal family lately?",
                    "Duke Edric came yesterday to read the succession rolls. An odd thing, reading about your own sister's crown."),
                T("A vial of wolfsbane is missing from your cabinet.",
                    "Gone? I... I went to fetch candles after the third bell, and the cabinet was unlocked. Anyone in the library could " +
                    "have taken it. I swear I gave it to no one!", reagentList));

            Talk(rival,
                "So the Captain has hired a hound. How quaint.",
                T("What is your quarrel with the Crown?",
                    "My grandfather should have been king. Everyone knows it. But I prefer to win my battles with gold, detective, not daggers."),
                T("Where were you this afternoon?",
                    "Dressing. It takes hours. My maid will confirm it, though you will find her as dull as I do."),
                T("Do you know Master Vale?",
                    "The Treasurer? We have... dealings. He is a man who likes money far more than he has it."),
                T("Your letter to Master Vale.",
                    "You read my letters? ...Yes. He borrowed four hundred crowns from me, to cover Crown money he had already spent, I " +
                    "suspect. I want it back before the audit exposes him. Why would I want the Queen dead? Her audit is what makes him pay me.",
                    debtLetter),
                T("Your meetings with Lord Halvard.",
                    "A matter of marriage, not murder. My son and the Queen's niece. Ask Halvard if you doubt me.", meetings));

            Talk(sibling,
                "Detective. Is it me they want dead, or my sister? Either way, I would prefer you hurried.",
                T("How do you get on with the Queen?",
                    "We quarrel. She has forbidden my marriage to a merchant's daughter, says it shames the crown. I love her, but she rules " +
                    "my life like one of her provinces."),
                T("Where were you this afternoon?",
                    "At the archery butts with Captain Hale, from the third bell to the fourth. I shot terribly."),
                T("You inherit the throne if she dies.",
                    "Yes, and I would make a dreadful king. I read the rolls to learn whether I can renounce my claim and marry as I please. " +
                    "Ask Ilric, I asked him how.", succession),
                T("This burnt note: \"She will not rule my life forever.\"",
                    "...That is mine. I wrote it in a temper and threw it in the fire. It is about my marriage. I meant I would leave, " +
                    "detective. Not that I would kill her.", edricNote));

            Talk(treasurer,
                "Detective. A terrible business, this warning. If figures or records can help you, I am at your service.",
                T("What is your work here?",
                    "The Crown's accounts. Every coin in, every coin out. The books are in perfect order, I assure you."),
                T("Where were you this afternoon?",
                    "Here in the library, all afternoon, with my ledgers. I never left the table. Ask Master Ilric."),
                T("Have you heard of an audit?",
                    "An audit? No. No, I... I would welcome one, of course. The books are perfect."),
                T("Ilric says you left the library after the third bell.",
                    "Did I? Perhaps to stretch my legs. A few minutes, no more. Ilric is old, he loses track of the hours.", ilricTestimony),
                T("You signed for the cellar key at the third bell.",
                    "Ah. Yes, I had forgotten. A stores count. Routine. The Crown must know how many barrels of turnips it owns.", keyLedger),
                T("Lady Seraphine wants her four hundred crowns.",
                    "A private loan. Entirely private. It has nothing whatever to do with the Crown's money.", debtLetter),
                T("Agnes saw you covered in cobwebs.",
                    "The library is very dusty. Ask anyone.", cobwebs),
                T("The vial was wrapped in a ledger page, in green ink.",
                    "Anyone can buy green ink. Anyone can tear a page. I... I believe I have answered enough questions, detective.", emptyVial));

            // ---------- the case ----------
            var mystery = Load<MysteryData>(MysteryPath);
            mystery.title = "The Vesper Toast";
            mystery.subtitle = "A Medieval Assassination Mystery";
            mystery.briefing =
                "Castle Aldmere, midwinter. At noon a note was pinned to the guardroom door: <i>\"Royal blood will be spilled at the " +
                "vesper bell.\"</i>\n\nCaptain Brienne Hale has summoned you in secret. Two royals are in the castle tonight: Queen " +
                "Isolde and her brother, Duke Edric. The feast has begun, and the vesper bell rings in ten minutes.\n\n" +
                "Question the household, search the Great Hall, the Library and the Basement, and compare what people say with what " +
                "you find. When you can prove who the assassin is (motive, means and opportunity), make your accusation from the " +
                "notebook. A false accusation costs precious time.";
            mystery.target = queen;
            mystery.suspects = new List<SuspectData> { advisor, captain, chef, physician, servant, blacksmith, scholar, rival, sibling, treasurer };
            mystery.assassin = treasurer;
            mystery.keyEvidence = new List<ClueData>
            {
                auditDecree, debtLetter, keyLedger, reagentList, emptyVial, toastOrder, resealedCask, ilricTestimony, cobwebs, greenInk
            };
            mystery.solution =
                "Vale had been stealing from the royal treasury for a year, and borrowed four hundred crowns from Lady Seraphine to hide " +
                "the gap. When Lord Halvard drafted the Queen's decree ordering an audit at dawn, Vale knew he was finished. After the " +
                "third bell he slipped out of the library, took Master Ilric's wolfsbane from the unlocked cabinet, signed out the cellar " +
                "key in his own green ink, and poisoned the Queen's private cask while the cellar patrol was away. His order slip made " +
                "sure that cask, and only that cask, would be poured for the vesper toast.";
            mystery.durationSeconds = 600f;
            EditorUtility.SetDirty(mystery);

            AssetDatabase.SaveAssets();
            Debug.Log("[AssassinMystery] Generated the Vesper Toast mystery data.");
            return mystery;
        }

        // ---------- helpers ----------

        static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        static SuspectData Person(string id, string name, string role, string profile, string coat, string accent, string skin,
            Headwear hat, float height, bool beard)
        {
            var s = Load<SuspectData>($"{Root}/Suspects/{id}.asset");
            s.id = id;
            s.displayName = name;
            s.role = role;
            s.profile = profile;
            s.coatColor = Hex(coat);
            s.accentColor = Hex(accent);
            s.skinColor = Hex(skin);
            s.headwear = hat;
            s.heightScale = height;
            s.beard = beard;
            EditorUtility.SetDirty(s);
            return s;
        }

        static ClueData Clue(string id, string title, ClueCategory category, string description, params SuspectData[] related)
        {
            var c = Load<ClueData>($"{Root}/Clues/{id}.asset");
            c.id = id;
            c.title = title;
            c.category = category;
            c.description = description;
            c.relatedSuspects = new List<SuspectData>(related);
            EditorUtility.SetDirty(c);
            return c;
        }

        static DialogueTopic T(string question, string response, ClueData requires = null, ClueData reveals = null) =>
            new DialogueTopic { question = question, response = response, requiresClue = requires, revealsClue = reveals, recordInNotebook = true };

        static void Talk(SuspectData s, string greeting, params DialogueTopic[] topics)
        {
            s.greeting = greeting;
            s.topics = new List<DialogueTopic>(topics);
            EditorUtility.SetDirty(s);
        }
    }
}
