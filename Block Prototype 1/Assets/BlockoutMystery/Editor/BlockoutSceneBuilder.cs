using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlockoutMystery.EditorTools
{
    /// <summary>
    /// Builds the variant scenes "Assets/BlockoutMystery/Scenes/Blockout Murder Mystery - [variant].unity" from
    /// primitives: four rooms, the cast, statuettes, plates, the locked closet, the player, camera and systems. Every
    /// variant has the same level and puzzle; only the camera and the dialogue presentation differ (see
    /// <see cref="Variants"/>). Re-run from Tools > Blockout Murder Mystery; scene files are overwritten.
    ///
    /// Layout (metres, y up, +z = north). Walk: Entrance -> north into the Dining Hall -> west into the Kitchen -> the
    /// locked closet south of the Kitchen.
    ///   Kitchen  x -24..-8, z 0..16   |  Dining Hall  x -8..8, z 0..16
    ///   Closet   x -22..-16, z -5..0  |  Entrance     x -8..8, z -16..0  (start)
    /// The isometric variant drops the ceilings and cuts the walls down so you can see into the rooms.
    /// </summary>
    public static class BlockoutSceneBuilder
    {
        public const string Root = "Assets/BlockoutMystery";
        const string MaterialFolder = Root + "/Materials";

        const float H = 4f;    // ceiling height
        const float CutawayHeight = 1.3f; // wall height in the isometric variant
        const float T = 0.3f;  // wall thickness
        const float In = T * 0.5f; // from a wall's centre line to its inner face
        const int IgnoreRaycast = 2;

        enum Look { Butler, Guard, King, Queen, Prince, Waiter, Chef, Cook }

        /// <summary>One playable variant: its name, how dialogue is shown and how the player sees the world.</summary>
        public readonly struct Variant
        {
            public readonly string title;
            public readonly DialogueStyle dialogue;
            public readonly ViewStyle view;
            public Variant(string title, DialogueStyle dialogue, ViewStyle view) { this.title = title; this.dialogue = dialogue; this.view = view; }
            public string ScenePath => $"{Root}/Scenes/Blockout Murder Mystery - {title}.unity";
        }

        public static readonly Variant[] Variants =
        {
            new Variant("Speech Bubbles", DialogueStyle.SpeechBubbles, ViewStyle.ThirdPersonSwitchable),
            new Variant("Subtitles", DialogueStyle.Subtitles, ViewStyle.ThirdPersonSwitchable),
            new Variant("Visual Novel", DialogueStyle.VisualNovel, ViewStyle.ThirdPersonSwitchable),
            new Variant("Close-up Subtitles", DialogueStyle.CloseUp, ViewStyle.ThirdPersonSwitchable),
            new Variant("Isometric Speech Bubbles", DialogueStyle.SpeechBubbles, ViewStyle.Isometric),
        };

        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Transform env;
        static int statuetteCount;
        static bool iso;      // building the isometric variant
        static float wallH;   // wall height for the variant being built

        static Material floorEntrance, floorDining, floorKitchen, floorCloset, wall, ceiling, pillar, wood, woodDark, cloth, rug,
            metal, white, black, skin, gold, statue, keyGold, lockRed, stove, counter, plate, roast, potato, antidote, paper,
            bookCover, jar;

        [MenuItem("Tools/Blockout Murder Mystery/Build All Variants", priority = 0)]
        static void BuildAllMenu() => Menu(-1);

        [MenuItem("Tools/Blockout Murder Mystery/Build Scene - Speech Bubbles", priority = 20)]
        static void BuildSpeechBubblesMenu() => Menu(0);

        [MenuItem("Tools/Blockout Murder Mystery/Build Scene - Subtitles", priority = 21)]
        static void BuildSubtitlesMenu() => Menu(1);

        [MenuItem("Tools/Blockout Murder Mystery/Build Scene - Visual Novel", priority = 22)]
        static void BuildVisualNovelMenu() => Menu(2);

        [MenuItem("Tools/Blockout Murder Mystery/Build Scene - Close-up Subtitles", priority = 23)]
        static void BuildCloseUpMenu() => Menu(3);

        [MenuItem("Tools/Blockout Murder Mystery/Build Scene - Isometric Speech Bubbles", priority = 24)]
        static void BuildIsometricMenu() => Menu(4);

        static void Menu(int index)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (index < 0) BuildAll();
            else Build(Variants[index]);
        }

        /// <summary>Builds every variant scene. The last one built stays open.</summary>
        public static void BuildAll()
        {
            foreach (var v in Variants) Build(v);
        }

        public static void Build(Variant variant)
        {
            cache.Clear();
            statuetteCount = 0;
            iso = variant.view == ViewStyle.Isometric;
            wallH = iso ? CutawayHeight : H;
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMaterials(); // after NewScene, which unloads unreferenced assets
            env = new GameObject("Level").transform;

            SetupLighting();
            BuildShell();
            var statuettes = new List<Statuette>();
            BuildEntrance(statuettes);
            BuildDiningHall(statuettes);
            BuildKitchen(statuettes);
            BuildCloset();
            BuildCast();

            // ---------- camera ----------
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.11f, 0.13f);
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 60f;
            camGo.AddComponent<AudioListener>();

            var inspectLight = new GameObject("Inspect Light").AddComponent<Light>();
            inspectLight.transform.SetParent(camGo.transform, false);
            inspectLight.transform.localPosition = new Vector3(0.25f, 0.35f, 0.05f);
            inspectLight.type = LightType.Point;
            inspectLight.range = 2.5f;
            inspectLight.intensity = 0.8f;
            inspectLight.color = new Color(1f, 0.96f, 0.9f);
            inspectLight.shadows = LightShadows.None;
            inspectLight.enabled = false;

            // ---------- player ----------
            var player = new GameObject("Player") { layer = IgnoreRaycast, tag = "Player" };
            player.transform.position = new Vector3(0f, 0.02f, -12f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.3f;
            var bodyMat = ColorMat(new Color(0.18f, 0.55f, 0.6f));
            Part(player.transform, PrimitiveType.Capsule, new Vector3(0, 0.9f, 0), new Vector3(0.62f, 0.9f, 0.62f), bodyMat);
            Part(player.transform, PrimitiveType.Cube, new Vector3(0, 1.5f, 0.27f), new Vector3(0.4f, 0.12f, 0.08f), black); // visor shows facing
            Part(player.transform, PrimitiveType.Cylinder, new Vector3(0, 1.82f, 0), new Vector3(0.62f, 0.015f, 0.62f), woodDark);
            Part(player.transform, PrimitiveType.Cylinder, new Vector3(0, 1.9f, 0), new Vector3(0.38f, 0.08f, 0.38f), woodDark);
            player.AddComponent<PlayerMotor>().Configure(camGo.transform);
            player.AddComponent<PlayerInteractor>().Configure(camGo.transform);
            if (iso)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6.5f;
                camGo.AddComponent<IsoCamera>().Configure(player.transform);
                camGo.transform.rotation = Quaternion.Euler(35f, 45f, 0f);
                camGo.transform.position = player.transform.position + Vector3.up - camGo.transform.forward * 40f;
            }
            else
            {
                camGo.AddComponent<FollowCamera>().Configure(player.transform);
                camGo.transform.position = player.transform.position + new Vector3(0, 2.6f, -3.4f);
                camGo.transform.LookAt(player.transform.position + Vector3.up * 1.6f);
            }

            // ---------- systems ----------
            var systems = new GameObject("Systems");
            systems.AddComponent<PrototypeVariant>().Configure(variant.title, variant.dialogue, variant.view);
            systems.AddComponent<GameFlow>().Configure(statuettes.ToArray());
            systems.AddComponent<GameTimer>();
            systems.AddComponent<Inventory>();
            systems.AddComponent<Notebook>();
            systems.AddComponent<DialogueSystem>();
            systems.AddComponent<InspectSystem>().Configure(cam, inspectLight);
            systems.AddComponent<ReportSystem>();
            systems.AddComponent<UIRoot>();

            EditorSceneManager.SaveScene(scene, variant.ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BlockoutMystery] Built {variant.ScenePath} ({statuettes.Count} statuettes). Press Play.");
        }

        // =====================================================================
        // Rooms
        // =====================================================================

        static void BuildShell()
        {
            var g = Group("Shell");
            Floor(g, "Entrance Floor", -8, 8, -16, 0, floorEntrance);
            Floor(g, "Dining Hall Floor", -8, 8, 0, 16, floorDining);
            Floor(g, "Kitchen Floor", -24, -8, 0, 16, floorKitchen);
            Floor(g, "Closet Floor", -22, -16, -5, 0, floorCloset);
            if (!iso)
            {
                Ceiling(g, -8, 8, -16, 16);
                Ceiling(g, -24, -8, 0, 16);
                Ceiling(g, -22, -16, -5, 0);
            }
            else Box(g, "Ground", new Vector3(-8f, -0.35f, 0f), new Vector3(90f, 0.1f, 90f), ColorMat(new Color(0.16f, 0.17f, 0.2f)), false);

            WallX(g, -16, -8, 8);                                  // Entrance, south
            WallZ(g, 8, -16, 16);                                  // east side
            WallZ(g, -8, -16, 0);                                  // Entrance, west
            WallX(g, 16, -24, 8);                                  // north side
            WallZ(g, -24, 0, 16);                                  // Kitchen, west
            WallX(g, 0, -8, 8, new Vector3(0, 3f, 3f));            // Entrance | Dining Hall (archway)
            WallZ(g, -8, 0, 16, new Vector3(8, 3f, 3f));           // Dining Hall | Kitchen (archway)
            WallX(g, 0, -24, -8, new Vector3(-19, 1.3f, 2.4f));    // Kitchen | Closet (door)
            WallZ(g, -22, -5, 0);
            WallZ(g, -16, -5, 0);
            WallX(g, -5, -22, -16);

            if (iso)
            {
                // No walls to hang signs on: room names are painted on the floor instead.
                FloorLabel(g, "Entrance", new Vector3(3.6f, 0.02f, -10.8f));
                FloorLabel(g, "Dining Hall", new Vector3(-4.5f, 0.02f, 13.8f));
                FloorLabel(g, "Kitchen", new Vector3(-19.5f, 0.02f, 13.2f));
                FloorLabel(g, "Closet", new Vector3(-20.6f, 0.02f, -1.4f), 4f);
                return;
            }

            // Signs over the doorways, readable from the side you walk in from.
            Sign(g, "Dining Hall", new Vector3(0, 3.45f, -In - 0.02f), 0f);
            Sign(g, "Entrance", new Vector3(0, 3.45f, In + 0.02f), 180f);
            Sign(g, "Kitchen", new Vector3(-8 + In + 0.02f, 3.45f, 8), -90f);
            Sign(g, "Dining Hall", new Vector3(-8 - In - 0.02f, 3.45f, 8), 90f);
            Sign(g, "Closet", new Vector3(-19, 2.85f, In + 0.02f), 180f, 2.4f);
        }

        static void BuildEntrance(List<Statuette> statuettes)
        {
            var g = Group("Entrance");
            Box(g, "Rug", new Vector3(0, 0.01f, -8f), new Vector3(3.2f, 0.02f, 13f), rug, false);
            foreach (float x in new[] { -4.2f, 4.2f })
            foreach (float z in new[] { -12.5f, -4.5f })
                Cyl(g, "Pillar", new Vector3(x, wallH * 0.5f, z), 0.7f, wallH, pillar);
            Box(g, "Bench", new Vector3(-7.4f, 0.25f, -13.2f), new Vector3(0.8f, 0.5f, 2.4f), wood);
            Box(g, "Bench", new Vector3(7.4f, 0.25f, -13.2f), new Vector3(0.8f, 0.5f, 2.4f), wood);
            Box(g, "Side Table", new Vector3(-7.45f, 0.4f, -6f), new Vector3(0.7f, 0.8f, 1.4f), woodDark);
            Cyl(g, "Vase", new Vector3(-7.45f, 1.0f, -6f), 0.25f, 0.4f, jar, false);

            statuettes.Add(ShelfWithStatuette(g, new Vector3(-8 + In, 0, -10f), 90f));
            statuettes.Add(CabinetWithStatuette(g, new Vector3(8 - In, 0, -8.5f), -90f));
            statuettes.Add(CabinetWithStatuette(g, new Vector3(-8 + In, 0, -2.4f), 90f));

            Light(g, new Vector3(0, 3.6f, -12f));
            Light(g, new Vector3(0, 3.6f, -4f));
        }

        static void BuildDiningHall(List<Statuette> statuettes)
        {
            var g = Group("Dining Hall");
            Box(g, "Table Top", new Vector3(0, 0.78f, 9f), new Vector3(6f, 0.1f, 2.2f), wood);
            foreach (float x in new[] { -2.7f, 2.7f })
            foreach (float z in new[] { 8.1f, 9.9f })
                Box(g, "Table Leg", new Vector3(x, 0.365f, z), new Vector3(0.14f, 0.73f, 0.14f), woodDark);
            Box(g, "Table Runner", new Vector3(0, 0.836f, 9f), new Vector3(5.4f, 0.01f, 0.7f), cloth, false);
            foreach (float x in new[] { -1.8f, 0f, 1.8f })
            {
                Cyl(g, "Candle Holder", new Vector3(x, 0.85f, 9f), 0.14f, 0.03f, gold, false);
                Cyl(g, "Candle", new Vector3(x, 0.97f, 9f), 0.07f, 0.22f, white, false);
            }

            Chair(g, new Vector3(1.4f, 0, 7.5f), 0f);
            Chair(g, new Vector3(-2.2f, 0, 7.5f), 0f);
            Chair(g, new Vector3(-1.2f, 0, 10.5f), 180f);
            Chair(g, new Vector3(2.2f, 0, 10.5f), 180f);
            Chair(g, new Vector3(-3.6f, 0, 9f), 90f);

            Box(g, "Sideboard", new Vector3(3.5f, 0.45f, 16 - In - 0.3f), new Vector3(2.4f, 0.9f, 0.6f), woodDark);
            if (!iso) // they would float above the cut-away walls
            {
                Box(g, "Banner", new Vector3(-1.5f, 2.6f, 16 - In - 0.03f), new Vector3(1.1f, 2f, 0.04f), cloth, false);
                Box(g, "Banner", new Vector3(1.5f, 2.6f, 16 - In - 0.03f), new Vector3(1.1f, 2f, 0.04f), cloth, false);
            }

            statuettes.Add(CabinetWithStatuette(g, new Vector3(8 - In, 0, 13.5f), -90f));
            statuettes.Add(ShelfWithStatuette(g, new Vector3(-4.5f, 0, 16 - In), 180f));

            Light(g, new Vector3(0, 3.6f, 5f));
            Light(g, new Vector3(0, 3.6f, 12f));
        }

        static void BuildKitchen(List<Statuette> statuettes)
        {
            var g = Group("Kitchen");

            // The big island, with the three plates on the side facing the room.
            Box(g, "Island", new Vector3(-16f, 0.45f, 8.5f), new Vector3(4.4f, 0.9f, 1.8f), woodDark);
            Box(g, "Island Top", new Vector3(-16f, 0.925f, 8.5f), new Vector3(4.6f, 0.05f, 2f), counter);
            MakePlate(g, new Vector3(-17.3f, 0.95f, 7.95f), HerbColor.Orange);
            MakePlate(g, new Vector3(-16f, 0.95f, 7.95f), HerbColor.Green);
            MakePlate(g, new Vector3(-14.7f, 0.95f, 7.95f), HerbColor.Purple);
            // Work the cooks are busy with.
            Box(g, "Cutting Board", new Vector3(-17.7f, 0.96f, 9.0f), new Vector3(0.5f, 0.03f, 0.35f), wood, false);
            Box(g, "Cutting Board", new Vector3(-14.3f, 0.96f, 9.0f), new Vector3(0.5f, 0.03f, 0.35f), wood, false);
            Sphere(g, "Cabbage", new Vector3(-17.6f, 1.04f, 9.0f), Vector3.one * 0.16f, ColorMat(new Color(0.55f, 0.75f, 0.4f)));
            Sphere(g, "Onion", new Vector3(-14.25f, 1.02f, 9.0f), Vector3.one * 0.12f, ColorMat(new Color(0.85f, 0.7f, 0.5f)));
            Cyl(g, "Mixing Bowl", new Vector3(-16f, 1.0f, 9.15f), 0.4f, 0.12f, metal, false);

            // Stove and hood along the north wall, a counter along the west wall.
            Box(g, "Stove", new Vector3(-12.5f, 0.5f, 16 - In - 0.45f), new Vector3(3.2f, 1f, 0.9f), stove);
            if (!iso) Box(g, "Hood", new Vector3(-12.5f, 3.2f, 16 - In - 0.4f), new Vector3(2.4f, 1.6f, 0.8f), stove);
            Cyl(g, "Pot", new Vector3(-13.3f, 1.15f, 15.3f), 0.5f, 0.3f, metal, false);
            Cyl(g, "Pan", new Vector3(-11.7f, 1.04f, 15.3f), 0.45f, 0.06f, black, false);
            Box(g, "Counter", new Vector3(-24 + In + 0.45f, 0.45f, 8.5f), new Vector3(0.9f, 0.9f, 5f), woodDark);
            Box(g, "Counter Top", new Vector3(-24 + In + 0.45f, 0.925f, 8.5f), new Vector3(1f, 0.05f, 5.1f), counter);
            Cyl(g, "Barrel", new Vector3(-9.0f, 0.5f, 14.8f), 0.8f, 1f, wood);
            Cyl(g, "Barrel", new Vector3(-9.1f, 0.5f, 13.7f), 0.8f, 1f, wood);

            statuettes.Add(ShelfWithStatuette(g, new Vector3(-20.5f, 0, 16 - In), 180f));
            statuettes.Add(CabinetWithStatuette(g, new Vector3(-24 + In, 0, 3.2f), 90f));

            // The locked closet door in the south wall. It swings into the closet.
            var hinge = new GameObject("Closet Door").transform;
            hinge.SetParent(g, false);
            hinge.position = new Vector3(-19.65f, 0f, 0f);
            float doorH = Mathf.Min(2.36f, wallH - 0.05f); // cut down with the walls in the isometric variant
            LBox(hinge, "Panel", new Vector3(0.65f, doorH * 0.5f + 0.01f, 0f), new Vector3(1.28f, doorH, 0.08f), woodDark, true);
            LBox(hinge, "Lock", new Vector3(1.1f, 1.05f, 0.06f), new Vector3(0.1f, 0.16f, 0.04f), lockRed);
            LBox(hinge, "Keyhole", new Vector3(1.1f, 1.03f, 0.085f), new Vector3(0.025f, 0.06f, 0.01f), black);
            LBox(hinge, "Handle", new Vector3(1.1f, 1.22f, 0.08f), new Vector3(0.14f, 0.03f, 0.04f), metal);
            var door = hinge.gameObject.AddComponent<ClosetDoor>();
            door.Configure(hinge);
            door.ConfigureInteraction(Point(g, "Closet Door Point", new Vector3(-19f, 1.1f, 0.4f)), 2.2f);

            Light(g, new Vector3(-20f, 3.6f, 8f));
            Light(g, new Vector3(-12f, 3.6f, 8f));
        }

        static void BuildCloset()
        {
            var g = Group("Closet");

            // Chest of drawers against the south wall, front facing north.
            var chest = Anchor(g, "Chest of Drawers", new Vector3(-19f, 0f, -5 + In), 0f);
            LBox(chest, "Body", new Vector3(0, 0.33f, 0.28f), new Vector3(1.4f, 0.66f, 0.56f), woodDark, true);
            LBox(chest, "Top", new Vector3(0, 0.98f, 0.28f), new Vector3(1.44f, 0.04f, 0.58f), wood, true);
            LBox(chest, "Side", new Vector3(-0.68f, 0.81f, 0.28f), new Vector3(0.04f, 0.3f, 0.56f), woodDark, true);
            LBox(chest, "Side", new Vector3(0.68f, 0.81f, 0.28f), new Vector3(0.04f, 0.3f, 0.56f), woodDark, true);
            LBox(chest, "Back", new Vector3(0, 0.81f, 0.02f), new Vector3(1.36f, 0.3f, 0.04f), woodDark, true);
            foreach (float y in new[] { 0.48f, 0.17f })
            {
                LBox(chest, "Drawer Front", new Vector3(0, y, 0.565f), new Vector3(1.3f, 0.26f, 0.02f), wood);
                LBox(chest, "Knob", new Vector3(0, y, 0.585f), new Vector3(0.08f, 0.04f, 0.03f), metal);
            }

            var tray = new GameObject("Top Drawer").transform;
            tray.SetParent(chest, false);
            tray.localPosition = new Vector3(0, 0.67f, 0.28f);
            LBox(tray, "Bottom", new Vector3(0, 0, 0), new Vector3(1.3f, 0.02f, 0.5f), wood);
            LBox(tray, "Front", new Vector3(0, 0.14f, 0.27f), new Vector3(1.34f, 0.28f, 0.04f), wood, true);
            LBox(tray, "Knob", new Vector3(0, 0.15f, 0.305f), new Vector3(0.08f, 0.04f, 0.03f), metal);
            LBox(tray, "Back", new Vector3(0, 0.1f, -0.24f), new Vector3(1.3f, 0.2f, 0.02f), wood);
            LBox(tray, "Side", new Vector3(-0.64f, 0.1f, 0), new Vector3(0.02f, 0.2f, 0.5f), wood);
            LBox(tray, "Side", new Vector3(0.64f, 0.1f, 0), new Vector3(0.02f, 0.2f, 0.5f), wood);

            // The green herbs (decoration) and the antidote.
            var herbs = new GameObject("Green Herbs").transform;
            herbs.SetParent(tray, false);
            herbs.localPosition = new Vector3(-0.3f, 0.02f, 0f);
            var green = HerbMat(HerbColor.Green);
            for (int i = 0; i < 6; i++)
                LBox(herbs, "Sprig", new Vector3(i * 0.045f - 0.11f, 0.015f + (i % 2) * 0.01f, (i % 3) * 0.03f - 0.03f),
                    new Vector3(0.035f, 0.02f, 0.18f), green, false, new Vector3(0, i * 22f - 55f, 0));
            LBox(herbs, "Twine", new Vector3(0, 0.03f, 0.05f), new Vector3(0.24f, 0.025f, 0.02f), white);

            var vial = new GameObject("Antidote").transform;
            vial.SetParent(tray, false);
            vial.localPosition = new Vector3(0.3f, 0.01f, 0f);
            LCyl(vial, "Vial", new Vector3(0, 0.07f, 0), 0.08f, 0.12f, antidote);
            LCyl(vial, "Neck", new Vector3(0, 0.15f, 0), 0.04f, 0.04f, antidote);
            LCyl(vial, "Cork", new Vector3(0, 0.185f, 0), 0.045f, 0.03f, wood);

            var drawer = chest.gameObject.AddComponent<Drawer>();
            drawer.Configure(tray);
            drawer.ConfigureInteraction(LocalPoint(chest, "Drawer Point", new Vector3(0, 0.81f, 0.6f)), 2.0f);
            var pickup = vial.gameObject.AddComponent<AntidotePickup>();
            pickup.Configure(drawer);
            pickup.ConfigureInteraction(LocalPoint(vial, "Point", new Vector3(0, 0.1f, 0)), 2.0f);

            // The herb book on top.
            var book = new GameObject("Herb Book").transform;
            book.SetParent(chest, false);
            book.localPosition = new Vector3(-0.4f, 1.0f, 0.28f);
            book.localRotation = Quaternion.Euler(0, 12f, 0);
            LBox(book, "Cover", new Vector3(0, 0.03f, 0), new Vector3(0.3f, 0.06f, 0.4f), bookCover);
            LBox(book, "Pages", new Vector3(0.012f, 0.03f, 0), new Vector3(0.28f, 0.046f, 0.38f), paper);
            book.gameObject.AddComponent<HerbBook>().ConfigureInteraction(LocalPoint(book, "Point", new Vector3(0, 0.06f, 0)), 2.0f);

            // Clutter.
            float shelfY = iso ? 1.0f : 1.5f;
            Box(g, "Shelf", new Vector3(-22 + In + 0.2f, shelfY, -2.5f), new Vector3(0.4f, 0.05f, 2f), wood);
            for (int i = 0; i < 4; i++)
                Cyl(g, "Jar", new Vector3(-22 + In + 0.2f, shelfY + 0.15f, -3.2f + i * 0.45f), 0.18f, 0.26f, jar, false);
            Cyl(g, "Broom", new Vector3(-16 - In - 0.15f, 0.8f, -1.8f), 0.05f, 1.6f, wood, false);

            Light(g, new Vector3(-19f, 3.3f, -2.5f), 2.6f, 8f);
        }

        // =====================================================================
        // Props
        // =====================================================================

        /// <summary>A wall shelf with a statuette. <paramref name="wallPoint"/> is on the wall's inner face at floor level; yaw faces into the room.</summary>
        static Statuette ShelfWithStatuette(Transform parent, Vector3 wallPoint, float yaw)
        {
            var a = Anchor(parent, "Wall Shelf", wallPoint, yaw);
            LBox(a, "Board", new Vector3(0, 1.25f, 0.22f), new Vector3(1.2f, 0.06f, 0.44f), wood, true);
            LBox(a, "Bracket", new Vector3(-0.45f, 1.1f, 0.12f), new Vector3(0.06f, 0.25f, 0.24f), woodDark);
            LBox(a, "Bracket", new Vector3(0.45f, 1.1f, 0.12f), new Vector3(0.06f, 0.25f, 0.24f), woodDark);
            LCyl(a, "Jar", new Vector3(0.4f, 1.4f, 0.22f), 0.14f, 0.24f, jar);
            LBox(a, "Books", new Vector3(-0.42f, 1.31f, 0.2f), new Vector3(0.24f, 0.06f, 0.18f), bookCover);
            return MakeStatuette(a, new Vector3(0, 1.28f, 0.22f));
        }

        /// <summary>A small cabinet with a statuette on top. Same placement rules as <see cref="ShelfWithStatuette"/>.</summary>
        static Statuette CabinetWithStatuette(Transform parent, Vector3 wallPoint, float yaw)
        {
            var a = Anchor(parent, "Small Cabinet", wallPoint, yaw);
            LBox(a, "Body", new Vector3(0, 0.5f, 0.3f), new Vector3(1f, 1f, 0.6f), woodDark, true);
            LBox(a, "Top", new Vector3(0, 1.015f, 0.3f), new Vector3(1.06f, 0.03f, 0.64f), wood);
            LBox(a, "Door", new Vector3(-0.24f, 0.5f, 0.605f), new Vector3(0.44f, 0.8f, 0.02f), wood);
            LBox(a, "Door", new Vector3(0.24f, 0.5f, 0.605f), new Vector3(0.44f, 0.8f, 0.02f), wood);
            LCyl(a, "Candle", new Vector3(0.36f, 1.13f, 0.3f), 0.06f, 0.2f, white);
            return MakeStatuette(a, new Vector3(0, 1.03f, 0.3f));
        }

        /// <summary>A hollow figure: four thin walls (open at the bottom), shoulders and a head. Holds an inactive key.</summary>
        static Statuette MakeStatuette(Transform parent, Vector3 localPos)
        {
            var root = new GameObject($"Statuette {++statuetteCount}").transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;

            const float w = 0.2f, h = 0.34f, t = 0.02f;
            LBox(root, "Robe", new Vector3(-w / 2 + t / 2, h / 2, 0), new Vector3(t, h, w), statue, true);
            LBox(root, "Robe", new Vector3(w / 2 - t / 2, h / 2, 0), new Vector3(t, h, w), statue, true);
            LBox(root, "Robe", new Vector3(0, h / 2, w / 2 - t / 2), new Vector3(w - 2 * t, h, t), statue, true);
            LBox(root, "Robe", new Vector3(0, h / 2, -w / 2 + t / 2), new Vector3(w - 2 * t, h, t), statue, true);
            LBox(root, "Shoulders", new Vector3(0, h + 0.02f, 0), new Vector3(w + 0.03f, 0.04f, w + 0.03f), statue, true);
            LSphere(root, "Head", new Vector3(0, h + 0.12f, 0), Vector3.one * 0.15f, statue, true);
            LBox(root, "Crown", new Vector3(0, h + 0.2f, 0), new Vector3(0.09f, 0.05f, 0.09f), statue, true);

            var key = new GameObject("Hidden Key").transform;
            key.SetParent(root, false);
            key.localPosition = new Vector3(0, h * 0.55f, 0);
            key.localRotation = Quaternion.Euler(0, 35f, 0);
            LBox(key, "Shaft", new Vector3(0.01f, 0, 0), new Vector3(0.11f, 0.016f, 0.016f), keyGold);
            LBox(key, "Bow", new Vector3(-0.06f, 0, 0), new Vector3(0.04f, 0.04f, 0.014f), keyGold);
            LBox(key, "Tooth", new Vector3(0.05f, -0.016f, 0), new Vector3(0.012f, 0.022f, 0.014f), keyGold);
            var bc = key.gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(-0.005f, 0, 0);
            bc.size = new Vector3(0.15f, 0.06f, 0.06f);
            key.gameObject.AddComponent<HiddenKey>();
            key.gameObject.SetActive(false);

            var s = root.gameObject.AddComponent<Statuette>();
            s.Configure(key.gameObject);
            s.ConfigureInspect("Statuette", new Vector3(-20f, 30f, 0f), 0.85f);
            s.ConfigureInteraction(LocalPoint(root, "Point", new Vector3(0, 0.2f, 0)), 2.0f);
            return s;
        }

        static void MakePlate(Transform parent, Vector3 worldPos, HerbColor herbs)
        {
            string herbName = CaseFile.HerbName(herbs);
            var root = new GameObject($"Plate ({herbName} herbs)").transform;
            root.SetParent(parent, false);
            root.position = worldPos;

            LCyl(root, "Plate", new Vector3(0, 0.012f, 0), 0.32f, 0.024f, plate);
            LSphere(root, "Roast", new Vector3(-0.02f, 0.05f, 0.01f), new Vector3(0.15f, 0.07f, 0.11f), roast);
            LSphere(root, "Potato", new Vector3(0.08f, 0.04f, -0.05f), new Vector3(0.06f, 0.05f, 0.06f), potato);
            LSphere(root, "Potato", new Vector3(0.07f, 0.04f, 0.06f), new Vector3(0.06f, 0.05f, 0.06f), potato);

            var herbMat = HerbMat(herbs);
            Vector3[] spots =
            {
                new Vector3(-0.05f, 0.085f, 0f), new Vector3(0f, 0.087f, 0.03f), new Vector3(0.02f, 0.085f, -0.02f),
                new Vector3(-0.04f, 0.08f, -0.035f), new Vector3(0.08f, 0.068f, -0.05f), new Vector3(0.07f, 0.068f, 0.06f),
            };
            for (int i = 0; i < spots.Length; i++)
                LBox(root, "Herb", spots[i], new Vector3(0.018f, 0.008f, 0.045f), herbMat, false, new Vector3(0, i * 37f, 0));

            var drops = new GameObject("Antidote Drops").transform;
            drops.SetParent(root, false);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                LSphere(drops, "Drop", new Vector3(Mathf.Cos(a) * 0.06f, 0.09f - (i % 2) * 0.02f, Mathf.Sin(a) * 0.05f), Vector3.one * 0.022f, antidote);
            }
            drops.gameObject.SetActive(false);

            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 0.045f, 0);
            box.size = new Vector3(0.33f, 0.09f, 0.33f);

            var p = root.gameObject.AddComponent<Plate>();
            p.Configure(herbs, drops.gameObject);
            p.ConfigureInspect($"Plate with {herbName} herbs", new Vector3(-55f, 0f, 0f), 0.6f);
            p.ConfigureInteraction(LocalPoint(root, "Point", new Vector3(0, 0.05f, 0)), 1.9f);
        }

        static void Chair(Transform parent, Vector3 pos, float yaw)
        {
            var a = Anchor(parent, "Chair", pos, yaw);
            LBox(a, "Seat", new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.08f, 0.5f), wood, true);
            LBox(a, "Back", new Vector3(0, 0.8f, -0.22f), new Vector3(0.5f, 0.65f, 0.06f), wood, true);
            foreach (float x in new[] { -0.21f, 0.21f })
            foreach (float z in new[] { -0.21f, 0.21f })
                LBox(a, "Leg", new Vector3(x, 0.21f, z), new Vector3(0.05f, 0.42f, 0.05f), woodDark);
        }

        // =====================================================================
        // Cast
        // =====================================================================

        static void BuildCast()
        {
            var g = new GameObject("Cast").transform;
            MakeNpc(g, CaseFile.Butler, new Vector3(-1.5f, 0, -7.5f), 162f, new Color(0.12f, 0.12f, 0.14f), Look.Butler);
            MakeNpc(g, CaseFile.Guard1, new Vector3(-5.4f, 0, -9f), 90f, new Color(0.62f, 0.18f, 0.16f), Look.Guard);
            MakeNpc(g, CaseFile.Guard2, new Vector3(5.4f, 0, -3.2f), -120f, new Color(0.62f, 0.18f, 0.16f), Look.Guard);
            MakeNpc(g, CaseFile.King, new Vector3(3.75f, 0, 9f), -90f, new Color(0.5f, 0.12f, 0.45f), Look.King);
            MakeNpc(g, CaseFile.Queen, new Vector3(0.9f, 0, 10.5f), 180f, new Color(0.2f, 0.3f, 0.75f), Look.Queen);
            MakeNpc(g, CaseFile.Prince, new Vector3(-0.9f, 0, 7.5f), 0f, new Color(0.2f, 0.6f, 0.35f), Look.Prince);
            MakeNpc(g, CaseFile.Waiter, new Vector3(-5f, 0, 3.8f), 45f, new Color(0.15f, 0.15f, 0.18f), Look.Waiter);
            MakeNpc(g, CaseFile.Chef, new Vector3(-16f, 0, 10.15f), 180f, new Color(0.92f, 0.92f, 0.9f), Look.Chef);
            MakeNpc(g, CaseFile.Cook1, new Vector3(-18.95f, 0, 8.5f), 90f, new Color(0.72f, 0.6f, 0.45f), Look.Cook);
            MakeNpc(g, CaseFile.Cook2, new Vector3(-13.05f, 0, 8.5f), -90f, new Color(0.5f, 0.6f, 0.68f), Look.Cook);
        }

        static void MakeNpc(Transform parent, string id, Vector3 pos, float yaw, Color bodyColor, Look look)
        {
            string name = CaseFile.NameOf(id);
            var root = new GameObject(name) { layer = IgnoreRaycast };
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.9f, 0);
            col.height = 1.8f;
            col.radius = 0.32f;

            var t = root.transform;
            var body = ColorMat(bodyColor);
            Part(t, PrimitiveType.Capsule, new Vector3(0, 0.75f, 0), new Vector3(0.6f, 0.75f, 0.6f), body);
            Part(t, PrimitiveType.Sphere, new Vector3(0, 1.62f, 0), Vector3.one * 0.36f, skin);
            Part(t, PrimitiveType.Cube, new Vector3(0, 1.6f, 0.18f), new Vector3(0.06f, 0.07f, 0.08f), skin);
            Part(t, PrimitiveType.Cube, new Vector3(-0.07f, 1.67f, 0.16f), new Vector3(0.04f, 0.04f, 0.02f), black);
            Part(t, PrimitiveType.Cube, new Vector3(0.07f, 1.67f, 0.16f), new Vector3(0.04f, 0.04f, 0.02f), black);

            float tagHeight = 2.3f;
            switch (look)
            {
                case Look.Butler:
                    Part(t, PrimitiveType.Cube, new Vector3(0, 1.15f, 0.27f), new Vector3(0.2f, 0.35f, 0.06f), white);
                    Part(t, PrimitiveType.Cube, new Vector3(0, 1.33f, 0.3f), new Vector3(0.16f, 0.06f, 0.04f), black);
                    break;
                case Look.Guard:
                    Part(t, PrimitiveType.Sphere, new Vector3(0, 1.7f, 0), new Vector3(0.4f, 0.3f, 0.4f), metal);
                    Part(t, PrimitiveType.Cylinder, new Vector3(0.42f, 1.1f, 0.1f), new Vector3(0.05f, 1.1f, 0.05f), wood);
                    Part(t, PrimitiveType.Cube, new Vector3(0.42f, 2.25f, 0.1f), new Vector3(0.08f, 0.2f, 0.03f), metal);
                    tagHeight = 2.55f;
                    break;
                case Look.King:
                    Crown(t, 0.28f, 1.86f);
                    Part(t, PrimitiveType.Cube, new Vector3(0, 1.0f, -0.28f), new Vector3(0.62f, 1.2f, 0.05f), cloth);
                    tagHeight = 2.4f;
                    break;
                case Look.Queen:
                    Crown(t, 0.24f, 1.85f);
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.35f, 0), new Vector3(0.78f, 0.35f, 0.78f), body);
                    tagHeight = 2.4f;
                    break;
                case Look.Prince:
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.82f, 0), new Vector3(0.3f, 0.025f, 0.3f), gold);
                    break;
                case Look.Waiter:
                    Part(t, PrimitiveType.Cylinder, new Vector3(0.4f, 1.12f, 0.2f), new Vector3(0.42f, 0.012f, 0.42f), metal);
                    Part(t, PrimitiveType.Cube, new Vector3(-0.33f, 1.0f, 0.1f), new Vector3(0.06f, 0.4f, 0.16f), white);
                    break;
                case Look.Chef:
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.94f, 0), new Vector3(0.3f, 0.14f, 0.3f), white);
                    Part(t, PrimitiveType.Sphere, new Vector3(0, 2.1f, 0), new Vector3(0.38f, 0.16f, 0.38f), white);
                    Part(t, PrimitiveType.Cube, new Vector3(0, 0.85f, 0.28f), new Vector3(0.4f, 0.7f, 0.04f), ColorMat(new Color(0.8f, 0.8f, 0.78f)));
                    tagHeight = 2.55f;
                    break;
                case Look.Cook:
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.82f, 0), new Vector3(0.34f, 0.04f, 0.34f), white);
                    Part(t, PrimitiveType.Cube, new Vector3(0, 0.85f, 0.28f), new Vector3(0.4f, 0.7f, 0.04f), white);
                    Part(t, PrimitiveType.Cube, new Vector3(0.3f, 1.0f, 0.32f), new Vector3(0.03f, 0.03f, 0.26f), metal);
                    break;
            }

            var tag = new GameObject("Name Tag");
            tag.transform.SetParent(t, false);
            tag.transform.localPosition = new Vector3(0, tagHeight, 0);
            if (iso) tag.transform.localScale = Vector3.one * 1.6f; // seen from further away
            var text = tag.AddComponent<TextMeshPro>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = name;
            text.fontSize = 2.2f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
            tag.AddComponent<Billboard>();

            var npc = root.AddComponent<Npc>();
            npc.Configure(id, name, CaseFile.LineOf(id));
            npc.ConfigureInteraction(LocalPoint(t, "Point", new Vector3(0, 1.3f, 0)), 2.6f);
        }

        static void Crown(Transform t, float size, float y)
        {
            Part(t, PrimitiveType.Cylinder, new Vector3(0, y, 0), new Vector3(size, 0.06f, size), gold);
            float r = size * 0.4f;
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f * Mathf.Deg2Rad;
                Part(t, PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * r, y + 0.09f, Mathf.Sin(a) * r), new Vector3(0.05f, 0.09f, 0.05f), gold);
            }
        }

        // =====================================================================
        // Building blocks
        // =====================================================================

        static Transform Group(string name)
        {
            var g = new GameObject(name).transform;
            g.SetParent(env, false);
            return g;
        }

        static Transform Anchor(Transform parent, string name, Vector3 pos, float yaw)
        {
            var a = new GameObject(name).transform;
            a.SetParent(parent, false);
            a.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            return a;
        }

        static Transform Point(Transform parent, string name, Vector3 worldPos)
        {
            var p = new GameObject(name).transform;
            p.SetParent(parent, false);
            p.position = worldPos;
            return p;
        }

        static Transform LocalPoint(Transform parent, string name, Vector3 localPos)
        {
            var p = new GameObject(name).transform;
            p.SetParent(parent, false);
            p.localPosition = localPos;
            return p;
        }

        /// <summary>Static level geometry in world space.</summary>
        static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 worldPos, Vector3 scale, Material m, bool collide)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (!collide) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true;
            return go;
        }

        static GameObject Box(Transform p, string n, Vector3 pos, Vector3 size, Material m, bool collide = true) =>
            Prim(PrimitiveType.Cube, p, n, pos, size, m, collide);

        static GameObject Cyl(Transform p, string n, Vector3 pos, float diameter, float height, Material m, bool collide = true) =>
            Prim(PrimitiveType.Cylinder, p, n, pos, new Vector3(diameter, height * 0.5f, diameter), m, collide);

        static GameObject Sphere(Transform p, string n, Vector3 pos, Vector3 scale, Material m) =>
            Prim(PrimitiveType.Sphere, p, n, pos, scale, m, false);

        /// <summary>Movable geometry in local space (not static: it can be picked up, opened or swung).</summary>
        static GameObject Local(PrimitiveType type, Transform p, string n, Vector3 localPos, Vector3 scale, Material m, bool collide, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = n;
            go.transform.SetParent(p, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (!collide) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject LBox(Transform p, string n, Vector3 localPos, Vector3 size, Material m, bool collide = false, Vector3 euler = default) =>
            Local(PrimitiveType.Cube, p, n, localPos, size, m, collide, euler);

        static GameObject LCyl(Transform p, string n, Vector3 localPos, float diameter, float height, Material m, bool collide = false) =>
            Local(PrimitiveType.Cylinder, p, n, localPos, new Vector3(diameter, height * 0.5f, diameter), m, collide, Vector3.zero);

        static GameObject LSphere(Transform p, string n, Vector3 localPos, Vector3 scale, Material m, bool collide = false) =>
            Local(PrimitiveType.Sphere, p, n, localPos, scale, m, collide, Vector3.zero);

        /// <summary>A character part: no collider, on Ignore Raycast so the camera does not bump into it.</summary>
        static void Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material m)
        {
            var go = Local(type, parent, type.ToString(), pos, scale, m, false, Vector3.zero);
            go.layer = IgnoreRaycast;
        }

        static void Floor(Transform p, string n, float x0, float x1, float z0, float z1, Material m) =>
            Box(p, n, new Vector3((x0 + x1) * 0.5f, -0.1f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.2f, z1 - z0), m);

        static void Ceiling(Transform p, float x0, float x1, float z0, float z1) =>
            Box(p, "Ceiling", new Vector3((x0 + x1) * 0.5f, H + 0.1f, (z0 + z1) * 0.5f), new Vector3(x1 - x0 + T, 0.2f, z1 - z0 + T), ceiling);

        /// <summary>Wall along X at depth z. Gaps are (centre x, width, height).</summary>
        static void WallX(Transform p, float z, float x0, float x1, params Vector3[] gaps)
        {
            float a = x0 - T * 0.5f, end = x1 + T * 0.5f;
            foreach (var gap in gaps)
            {
                float g0 = gap.x - gap.y * 0.5f, g1 = gap.x + gap.y * 0.5f;
                if (g0 > a) Box(p, "Wall", new Vector3((a + g0) * 0.5f, wallH * 0.5f, z), new Vector3(g0 - a, wallH, T), wall);
                if (gap.z < wallH) Box(p, "Lintel", new Vector3(gap.x, (gap.z + wallH) * 0.5f, z), new Vector3(gap.y, wallH - gap.z, T), wall);
                a = g1;
            }
            if (end > a) Box(p, "Wall", new Vector3((a + end) * 0.5f, wallH * 0.5f, z), new Vector3(end - a, wallH, T), wall);
        }

        /// <summary>Wall along Z at x. Gaps are (centre z, width, height).</summary>
        static void WallZ(Transform p, float x, float z0, float z1, params Vector3[] gaps)
        {
            float a = z0 - T * 0.5f, end = z1 + T * 0.5f;
            foreach (var gap in gaps)
            {
                float g0 = gap.x - gap.y * 0.5f, g1 = gap.x + gap.y * 0.5f;
                if (g0 > a) Box(p, "Wall", new Vector3(x, wallH * 0.5f, (a + g0) * 0.5f), new Vector3(T, wallH, g0 - a), wall);
                if (gap.z < wallH) Box(p, "Lintel", new Vector3(x, (gap.z + wallH) * 0.5f, gap.x), new Vector3(T, wallH - gap.z, gap.y), wall);
                a = g1;
            }
            if (end > a) Box(p, "Wall", new Vector3(x, wallH * 0.5f, (a + end) * 0.5f), new Vector3(T, wallH, end - a), wall);
        }

        /// <summary>A room name lying on the floor, turned to read the right way up from the isometric camera.</summary>
        static void FloorLabel(Transform p, string text, Vector3 pos, float size = 7f)
        {
            var go = new GameObject("Floor Label " + text);
            go.transform.SetParent(p, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(90f, 45f, 0f));
            var t = go.AddComponent<TextMeshPro>();
            t.font = TMP_Settings.defaultFontAsset;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(0.15f, 0.13f, 0.1f, 0.45f);
            t.rectTransform.sizeDelta = new Vector2(10f, 1.6f);
        }

        /// <summary>Flat text on a wall. TextMeshPro reads correctly when the viewer looks along the sign's +Z.</summary>
        static void Sign(Transform p, string text, Vector3 pos, float yaw, float size = 3f)
        {
            var go = new GameObject("Sign " + text);
            go.transform.SetParent(p, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var t = go.AddComponent<TextMeshPro>();
            t.font = TMP_Settings.defaultFontAsset;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(0.25f, 0.2f, 0.15f);
            t.rectTransform.sizeDelta = new Vector2(6f, 0.8f);
        }

        static void Light(Transform p, Vector3 pos, float intensity = 3.2f, float range = 13f)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(p, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.95f, 0.86f);
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
        }

        // =====================================================================
        // Lighting and materials
        // =====================================================================

        static void SetupLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.46f, 0.5f);
            RenderSettings.fog = false;

            var sun = new GameObject("Fill Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            sun.intensity = 0.55f;
            sun.color = new Color(1f, 0.97f, 0.92f);
            sun.shadows = LightShadows.None;
        }

        static void CreateMaterials()
        {
            floorEntrance = Mat("Floor Entrance", new Color(0.56f, 0.5f, 0.44f));
            floorDining = Mat("Floor Dining Hall", new Color(0.42f, 0.46f, 0.56f));
            floorKitchen = Mat("Floor Kitchen", new Color(0.5f, 0.56f, 0.47f));
            floorCloset = Mat("Floor Closet", new Color(0.46f, 0.38f, 0.3f));
            wall = Mat("Wall", new Color(0.8f, 0.79f, 0.76f));
            ceiling = Mat("Ceiling", new Color(0.62f, 0.62f, 0.62f));
            pillar = Mat("Pillar", new Color(0.7f, 0.68f, 0.64f));
            wood = Mat("Wood", new Color(0.62f, 0.43f, 0.26f));
            woodDark = Mat("Wood Dark", new Color(0.36f, 0.24f, 0.15f));
            cloth = Mat("Cloth Red", new Color(0.62f, 0.13f, 0.13f));
            rug = Mat("Rug", new Color(0.5f, 0.16f, 0.16f));
            metal = Mat("Metal", new Color(0.62f, 0.64f, 0.68f), 0.6f, 0.6f);
            white = Mat("White", new Color(0.94f, 0.94f, 0.91f));
            black = Mat("Black", new Color(0.08f, 0.08f, 0.09f));
            skin = Mat("Skin", new Color(0.92f, 0.76f, 0.62f));
            gold = Mat("Gold", new Color(0.95f, 0.75f, 0.25f), 0.7f, 0.8f);
            statue = Mat("Statuette", new Color(0.56f, 0.62f, 0.78f), 0.45f);
            keyGold = Mat("Key", new Color(1f, 0.8f, 0.3f), 0.6f, 0.8f, new Color(0.6f, 0.45f, 0.1f));
            lockRed = Mat("Lock", new Color(0.8f, 0.15f, 0.1f));
            stove = Mat("Stove", new Color(0.2f, 0.2f, 0.22f));
            counter = Mat("Counter Top", new Color(0.86f, 0.85f, 0.8f));
            plate = Mat("Plate", new Color(0.97f, 0.97f, 0.95f), 0.6f);
            roast = Mat("Roast", new Color(0.5f, 0.28f, 0.15f));
            potato = Mat("Potato", new Color(0.9f, 0.8f, 0.45f));
            antidote = Mat("Antidote", new Color(0.35f, 0.75f, 1f), 0.8f, 0f, new Color(0.1f, 0.3f, 0.5f));
            paper = Mat("Paper", new Color(0.95f, 0.92f, 0.83f));
            bookCover = Mat("Book Cover", new Color(0.25f, 0.38f, 0.25f));
            jar = Mat("Jar", new Color(0.7f, 0.55f, 0.4f));
        }

        static Material HerbMat(HerbColor c) => Mat("Herb " + CaseFile.HerbName(c), CaseFile.ColorOf(c), 0.3f);

        static Material ColorMat(Color c) => Mat("C_" + ColorUtility.ToHtmlStringRGB(c), c);

        static Material Mat(string name, Color color, float smoothness = 0.15f, float metallic = 0f, Color? emission = null)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            if (cache.TryGetValue(path, out var cached)) return cached;
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(m);
            cache[path] = m;
            return m;
        }
    }
}
