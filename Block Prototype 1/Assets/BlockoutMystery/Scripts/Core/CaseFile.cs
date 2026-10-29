using UnityEngine;

namespace BlockoutMystery
{
    public enum HerbColor { Green, Orange, Purple }

    public enum ItemId { Key, Antidote }

    /// <summary>How a piece of evidence is filed in the journal's Evidence tab.</summary>
    public enum EvidenceKind { Physical, Discovered, Statement }

    /// <summary>How well the evidence handed in with the report backs it up. Graded apart from who was accused.</summary>
    public enum CaseStrength { Insufficient, Plausible, Strong }

    /// <summary>
    /// The fixed case: cast, dialogue, the herb book and the solution. Nothing here is random; the only random element
    /// is which statuette hides the key (picked by <see cref="GameFlow"/> on every start).
    /// </summary>
    public static class CaseFile
    {
        public readonly struct Person
        {
            public readonly string id, name, line;
            /// <summary>The fact this line adds to the journal's Evidence tab once heard (null: not noteworthy).</summary>
            public readonly Evidence? noteworthy;
            public Person(string id, string name, string line, Evidence? noteworthy = null)
            {
                this.id = id;
                this.name = name;
                this.line = line;
                this.noteworthy = noteworthy;
            }
        }

        public readonly struct Page
        {
            public readonly string title, body;
            public readonly bool showsGreenHerb;
            /// <summary>Evidence this page adds to the journal when it is read (null: none).</summary>
            public readonly Evidence? evidence;
            public Page(string title, string body, bool showsGreenHerb = false, Evidence? evidence = null)
            {
                this.title = title;
                this.body = body;
                this.showsGreenHerb = showsGreenHerb;
                this.evidence = evidence;
            }
        }

        /// <summary>
        /// Something important enough for the journal's Evidence tab: a physical clue, a discovered fact or a noteworthy
        /// statement. <see cref="source"/> says where it came from ("the closet drawer", "Guard 1").
        /// </summary>
        public readonly struct Evidence
        {
            public readonly string id, title, description, source;
            public readonly EvidenceKind kind;

            public Evidence(string id, EvidenceKind kind, string title, string source, string description)
            {
                this.id = id;
                this.kind = kind;
                this.title = title;
                this.source = source;
                this.description = description;
            }
        }

        static Evidence Statement(string id, string speaker, string title, string description) =>
            new Evidence(id, EvidenceKind.Statement, title, speaker, description);

        public const string Butler = "butler", Guard1 = "guard1", Guard2 = "guard2", King = "king", Queen = "queen",
            Prince = "prince", Waiter = "waiter", Chef = "chef", Cook1 = "cook1", Cook2 = "cook2";

        public const string ButlerIntro =
            "I suspect there will be a murder taking place. Please prevent the murder and tell me who is involved.";
        public const string ButlerReminder =
            "Investigate the murder plot, prevent it and identify both the culprit and victim. Finally, report back to me.";

        public readonly struct Objective
        {
            public readonly string title, description;
            public Objective(string title, string description) { this.title = title; this.description = description; }
        }

        /// <summary>The journal's Objectives tab. Static: the game never ticks them off.</summary>
        public static readonly Objective[] Objectives =
        {
            new Objective("Prevent the assassination", "Identify the assassination method and neutralize the threat."),
            new Objective("Report back to the butler with your findings",
                "Determine the intended target, identify the culprit, and gather evidence to support your conclusions."),
        };

        /// <summary>
        /// Everyone in the castle, in the order they appear in the report table. The butler's line is his reminder.
        /// Noteworthy lines (red herrings included, so the journal doesn't give the answer away) also become evidence.
        /// </summary>
        public static readonly Person[] Cast =
        {
            new Person(Butler, "Butler", ButlerReminder),
            new Person(Guard1, "Guard 1", "I saw the chef getting herbs from the garden last night, he said they were for today's special dinner. " +
                    "I thought that was extra strange because normally he has the cooks do that kind of work.",
                Statement(GuardTestimony, "Guard 1", "The Chef picked herbs last night",
                    "Guard 1 saw the Chef getting herbs from the garden last night. The Chef said they were for today's special dinner, " +
                    "which the guard found strange: normally the Chef has the cooks do that kind of work.")),
            new Person(Guard2, "Guard 2", "Me and the waiter have something planned for the king, even though he secretly hates him...",
                Statement("statement-guard2", "Guard 2", "A plan for the King",
                    "Guard 2 says he and the Waiter have something planned for the King, \"even though he secretly hates him\".")),
            new Person(King, "King", "I saw the waiter talk to the guards, he normally never does that..."),
            new Person(Queen, "Queen", "Cook 1 was rude today, I might have him executed for that!",
                Statement("statement-queen", "Queen", "The Queen is furious with Cook 1",
                    "Cook 1 was rude to the Queen today. She says she might have him executed for it.")),
            new Person(Prince, "Prince", "Who are you?! Don't talk to me. You're just as disrespectful as all those cooks.",
                Statement("statement-prince", "Prince", "The Prince looks down on the cooks",
                    "The Prince snapped at you and called all the cooks disrespectful.")),
            new Person(Waiter, "Waiter", "(whispers) The king's birthday is coming up, me and the guards have a special surprise planned...",
                Statement("statement-waiter", "Waiter", "A surprise for the King's birthday",
                    "The Waiter whispers that the King's birthday is coming up and that he and the guards have a surprise planned.")),
            new Person(Chef, "Chef", "Cook 1 is acting weird today, I heard he cussed out the queen."),
            new Person(Cook1, "Cook 1", "I'm tired man, I work my ass off for these people."),
            new Person(Cook2, "Cook 2", "The chef always complains about the prince, I don't blame him...",
                Statement(CookTestimony, "Cook 2", "The Chef complains about the Prince",
                    "Cook 2 says the Chef always complains about the Prince, and doesn't blame him.")),
        };

        // ---- solution (fixed) ----
        public const string SolutionSuspect = Chef;
        public const string SolutionVictim = Prince;
        public const HerbColor PoisonedPlate = HerbColor.Green;

        // ---- report quality (simple prototype rules, see JudgeEvidence) ----
        public const string GuardTestimony = "statement-guard1", CookTestimony = "statement-cook2";

        /// <summary>
        /// The evidence that backs up the case against the Chef: the herb book (weak: it doesn't say who poisoned the meal),
        /// the guard's testimony (moderate: gathering herbs could be normal kitchen work), the herbs in the closet drawer
        /// (moderate) and the cook's testimony (weak). Everything else is irrelevant.
        /// </summary>
        public static readonly string[] SupportingEvidence = { NightleafPage, GuardTestimony, DrawerHerbs, CookTestimony };

        public static bool Supports(string evidenceId) => System.Array.IndexOf(SupportingEvidence, evidenceId) >= 0;

        /// <summary>
        /// Grades the evidence handed in (evidence ids; null for something that isn't evidence, like an unremarkable line).
        /// Strong: at least three supporting pieces, including the guard's testimony and the herbs in the drawer.
        /// Plausible: at least two supporting pieces, one of them a testimony that points at the Chef.
        /// Insufficient: anything less. One irrelevant pick is ignored; two or more drop the grade one step.
        /// </summary>
        public static CaseStrength JudgeEvidence(System.Collections.Generic.IEnumerable<string> evidenceIds,
            out int supporting, out int irrelevant)
        {
            var found = new System.Collections.Generic.HashSet<string>();
            irrelevant = 0;
            foreach (var id in evidenceIds)
            {
                if (id != null && Supports(id)) found.Add(id); // the same piece twice (entry and line) counts once
                else irrelevant++;
            }
            supporting = found.Count;

            bool pointsAtChef = found.Contains(GuardTestimony) || found.Contains(CookTestimony);
            var strength = supporting >= 3 && found.Contains(GuardTestimony) && found.Contains(DrawerHerbs) ? CaseStrength.Strong
                : supporting >= 2 && pointsAtChef ? CaseStrength.Plausible
                : CaseStrength.Insufficient;
            if (irrelevant >= 2 && strength > CaseStrength.Insufficient) strength--;
            return strength;
        }

        public static string StrengthName(CaseStrength s) => s switch
        {
            CaseStrength.Strong => "Strong case",
            CaseStrength.Plausible => "Plausible but incomplete case",
            _ => "Insufficient evidence",
        };

        // ---- evidence ----
        public const string NightleafPage = "nightleaf-page", DrawerHerbs = "drawer-herbs", AntidoteVial = "antidote", HiddenKey = "hidden-key";

        public const string AntidoteClue =
            "When mixed with the tainted food before it is served, silverleaf tonic neutralizes the poison, rendering the meal harmless.";

        public static readonly Evidence NightleafEvidence = new Evidence(NightleafPage, EvidenceKind.Discovered, "Herb book: Green Nightleaf", "the herb book",
            "A page from the herb book in the closet. Green Nightleaf has bright green, jagged leaves that look like parsley " +
            "and is deadly poisonous: a single sprig on a plate kills a grown man before dessert. " + AntidoteClue);
        public static readonly Evidence DrawerHerbsEvidence = new Evidence(DrawerHerbs, EvidenceKind.Physical, "Green herbs in the closet drawer", "the closet drawer",
            "A bundle of bright green, jagged herbs, hidden in the drawer of the locked kitchen closet, right next to the antidote.");
        public static readonly Evidence AntidoteEvidence = new Evidence(AntidoteVial, EvidenceKind.Physical, "Silverleaf tonic", "the closet drawer",
            "A single portion of antidote from the closet drawer. Enough for one plate.");
        public static readonly Evidence KeyEvidence = new Evidence(HiddenKey, EvidenceKind.Physical, "A hidden key", "a statuette",
            "A key someone hid inside one of the statuettes. It opens the kitchen closet.");

        /// <summary>The herb book in the closet. One page gives away the green herb; the rest is filler.</summary>
        public static readonly Page[] BookPages =
        {
            new Page("A Cook's Herbal", "A short guide to the herbs of the castle garden: their looks, their uses at the table, and their dangers.\n\nWritten for the kitchen staff. Do not remove from the closet."),
            new Page("Rosemary", "A woody shrub with needle-like leaves and a strong scent of pine.\n\nRoasted with lamb at feasts. Keeps well when hung up to dry."),
            new Page("Chamomile", "Small white flowers with yellow hearts.\n\nSteeped in hot water it makes a calming tea, popular with restless nobles before bed."),
            new Page("Green Nightleaf", "Bright green, jagged leaves that look a lot like parsley.\n\n<b>VERY DEADLY POISONOUS. NEVER EAT.</b>\n\nA single sprig on a plate is enough to kill a grown man before dessert is served.\n\n<i>Remedy:</i> " + AntidoteClue, true, NightleafEvidence),
            new Page("Garlic", "A white bulb that grows under the soil, split into cloves.\n\nGood for flavour and, the servants say, for keeping away colds."),
            new Page("Fennel", "Tall, feathery fronds with a faint taste of anise.\n\nThe seeds are chewed after a heavy meal to settle the stomach."),
        };

        public static string NameOf(string id)
        {
            foreach (var p in Cast) if (p.id == id) return p.name;
            return string.IsNullOrEmpty(id) ? "nobody" : id;
        }

        /// <summary>The evidence a line adds to the journal when heard, if the game thinks it is noteworthy.</summary>
        public static Evidence? NoteworthyIn(string speakerId, string line)
        {
            foreach (var p in Cast) if (p.id == speakerId && p.line == line) return p.noteworthy;
            return null;
        }

        public static string LineOf(string id)
        {
            foreach (var p in Cast) if (p.id == id) return p.line;
            return "";
        }

        public static string HerbName(HerbColor c) => c switch
        {
            HerbColor.Green => "green",
            HerbColor.Orange => "orange",
            _ => "purple",
        };

        /// <summary>Herb colour used by the plates, the drawer herbs and the picture in the book.</summary>
        public static Color ColorOf(HerbColor c) => c switch
        {
            HerbColor.Green => new Color(0.18f, 0.72f, 0.16f),
            HerbColor.Orange => new Color(0.98f, 0.5f, 0.08f),
            _ => new Color(0.56f, 0.2f, 0.78f),
        };
    }
}
