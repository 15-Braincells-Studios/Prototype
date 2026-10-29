using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AssassinMystery.EditorTools
{
    /// <summary>
    /// Builds Assets/AssassinMystery/Scenes/AssassinMystery.unity from scratch: the Great Hall, Library and
    /// Basement (joined by a door and a stairwell), the cast, searchable furniture, lighting, post-processing, the
    /// player, and the systems object. Re-run from Tools > Assassin Mystery > Build Scene; the scene file is
    /// overwritten, data assets are kept (run Regenerate Mystery Data to reset those).
    ///
    /// Layout (metres, y up):  Basement x -32..-18 (floor -3)  |  stairs x -18..-10  |  Great Hall x -10..10  |  Library x 10..24
    /// </summary>
    public static class CastleSceneBuilder
    {
        public const string ScenePath = "Assets/AssassinMystery/Scenes/AssassinMystery.unity";
        const string MaterialFolder = "Assets/AssassinMystery/Materials";
        const string CharacterMaterialFolder = MaterialFolder + "/Characters";
        const string VolumePath = MaterialFolder + "/AssassinMysteryVolume.asset";

        const float T = 0.4f; // wall thickness
        const int IgnoreRaycast = 2;

        const string Hall = "Great Hall";
        const string Library = "Library";
        const string Basement = "Basement";

        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Transform env;
        static int seed;

        // Materials
        static Material stoneFloor, stoneWall, darkStone, woodDark, woodLight, iron, gold, redCloth, blueCloth, carpet, wax,
            flame, fire, moon, parchment, barrelWood, wine, forgeGlow, sack, whiteCloth, glassGreen, glassBlue, glassPurple, black;

        [MenuItem("Tools/Third Person - First Test/Build Scene")]
        static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        public static void Build()
        {
            cache.Clear();
            seed = 7;
            Directory.CreateDirectory("Assets/AssassinMystery/Scenes");
            Directory.CreateDirectory(CharacterMaterialFolder);
            AssetDatabase.Refresh();
            MysteryContent.GetOrCreate();

            // NewScene unloads unreferenced assets, so load the mystery and materials only after it.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var mystery = AssetDatabase.LoadAssetAtPath<MysteryData>(MysteryContent.MysteryPath);
            CreateMaterials();
            env = new GameObject("Castle").transform;

            SetupLighting();
            BuildGreatHall();
            BuildStairs();
            BuildLibrary();
            BuildBasement();

            // ---------- cast ----------
            var s = new Dictionary<string, SuspectData>();
            foreach (var p in mystery.suspects) s[p.id] = p;
            var cast = new GameObject("Cast").transform;

            var queen = NPC(cast, mystery.target, new Vector3(0, 0.25f, 6.0f), 180f);
            NPC(cast, s["advisor"], new Vector3(6.4f, 0, 4.4f), Yaw(new Vector3(6.4f, 0, 4.4f), Vector3.zero));
            NPC(cast, s["captain"], new Vector3(6.6f, 0, -4.6f), Yaw(new Vector3(6.6f, 0, -4.6f), Vector3.zero));
            NPC(cast, s["servant"], new Vector3(-7.6f, 0, 3.0f), 100f);
            NPC(cast, s["rival"], new Vector3(-2.9f, 0, -6.0f), Yaw(new Vector3(-2.9f, 0, -6.0f), new Vector3(2, 0, -3)));
            NPC(cast, s["sibling"], new Vector3(7.8f, 0, 1.9f), -110f);
            NPC(cast, s["treasurer"], new Vector3(13.0f, 0, 2.4f), Yaw(new Vector3(13f, 0, 2.4f), new Vector3(17, 0, 0)));
            NPC(cast, s["scholar"], new Vector3(20.6f, 0, -2.0f), Yaw(new Vector3(20.6f, 0, -2f), new Vector3(16, 0, 0)));
            NPC(cast, s["physician"], new Vector3(14.2f, 0, -3.4f), Yaw(new Vector3(14.2f, 0, -3.4f), new Vector3(17, 0, 1)));
            NPC(cast, s["chef"], new Vector3(-26.2f, -3, -4.9f), Yaw(new Vector3(-26.2f, 0, -4.9f), new Vector3(-24, 0, 0)));
            NPC(cast, s["blacksmith"], new Vector3(-22.0f, -3, 4.2f), Yaw(new Vector3(-22f, 0, 4.2f), new Vector3(-24, 0, 0)));

            // ---------- player + camera ----------
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.08f;
            cam.fieldOfView = 60f;
            camGo.AddComponent<AudioListener>();
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var orbit = camGo.AddComponent<ThirdPersonCamera>();

            var player = new GameObject("Detective") { layer = IgnoreRaycast };
            player.tag = "Player";
            player.transform.position = new Vector3(4.5f, 0.02f, -4.2f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;
            var playerModel = Figure(player.transform, new Color(0.32f, 0.24f, 0.16f), new Color(0.75f, 0.62f, 0.38f),
                new Color(0.86f, 0.68f, 0.54f), Headwear.None, 1.0f, false, new Color(0.25f, 0.18f, 0.12f), false, true);
            var controller = player.AddComponent<ThirdPersonController>();
            controller.Configure(camGo.transform, playerModel);
            player.AddComponent<PlayerInteractor>();
            orbit.Configure(player.transform);
            camGo.transform.position = player.transform.position + new Vector3(0, 2.2f, -3.5f);

            // ---------- cutscene markers ----------
            var marks = new GameObject("Cutscene Markers").transform;
            var markers = new AssassinationSequence.Markers
            {
                assassinStart = Marker(marks, "Assassin Start", new Vector3(-6.5f, 0, 3.0f), new Vector3(0, 0.25f, 5)),
                assassinStrike = Marker(marks, "Assassin Strike", new Vector3(0, 0.25f, 4.9f), new Vector3(0, 0.25f, 7)),
                detectiveStart = Marker(marks, "Detective Start", new Vector3(6.0f, 0, 3.2f), new Vector3(0, 0, 5)),
                detectiveIntercept = Marker(marks, "Detective Intercept", new Vector3(0.85f, 0.25f, 4.65f), new Vector3(0, 0.25f, 4.9f)),
                captainHold = Marker(marks, "Captain Hold", new Vector3(-0.9f, 0.25f, 4.2f), new Vector3(0, 0.25f, 4.1f)),
                wideShot = Marker(marks, "Shot Wide", new Vector3(-7.2f, 3.6f, -1.6f), new Vector3(0, 1.3f, 5.0f)),
                closeShot = Marker(marks, "Shot Close", new Vector3(4.2f, 1.9f, 2.5f), new Vector3(0, 1.4f, 5.1f)),
                queenShot = Marker(marks, "Shot Queen", new Vector3(1.4f, 1.8f, 3.1f), new Vector3(0, 1.85f, 6.0f)),
            };

            // ---------- systems ----------
            var systems = new GameObject("Systems");
            var gm = systems.AddComponent<GameManager>();
            systems.AddComponent<GameTimer>();
            systems.AddComponent<InvestigationManager>();
            var dm = systems.AddComponent<DialogueManager>();
            systems.AddComponent<AccusationManager>();
            systems.AddComponent<UIManager>();
            var seq = systems.AddComponent<AssassinationSequence>();
            dm.Configure(player.transform);
            seq.Configure(queen, s["captain"], controller, orbit, gold, wine, markers);
            SetRef(gm, "mystery", mystery);
            SetRef(gm, "sequence", seq);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AssassinMystery] Built {ScenePath}. Press Play to investigate.");
        }

        // =====================================================================
        // Rooms
        // =====================================================================

        static void BuildGreatHall()
        {
            var room = Group(Hall);
            Floor(room, -10, 10, -8, 8, 0, stoneFloor);
            Ceiling(room, -10, 10, -8, 8, 7, woodDark);
            WallX(room, 8, -10, 10, 0, 7, stoneWall);
            WallX(room, -8, -10, 10, 0, 7, stoneWall);
            WallZ(room, 10, -8, 8, 0, 7, stoneWall, new Vector3(0, 3, 3.5f));
            WallZ(room, -10, -8, 8, 0, 7, stoneWall, new Vector3(0, 3, 3.5f));

            // Beams.
            for (float z = -6; z <= 6; z += 4) Box(room, "Beam", new Vector3(0, 6.7f, z), new Vector3(20, 0.35f, 0.35f), woodDark, false);

            // Pillars.
            foreach (var x in new[] { -7f, 7f })
            foreach (var z in new[] { -4.5f, 4.5f })
            {
                Cyl(room, "Pillar", new Vector3(x, 3.5f, z), new Vector3(0.8f, 3.5f, 0.8f), stoneWall);
                Box(room, "Pillar Base", new Vector3(x, 0.15f, z), new Vector3(1.1f, 0.3f, 1.1f), darkStone);
            }

            // Carpet and dais.
            Box(room, "Carpet", new Vector3(0, 0.01f, -2.0f), new Vector3(2.4f, 0.02f, 12f), carpet, false);
            Box(room, "Dais", new Vector3(0, 0.125f, 6.25f), new Vector3(8, 0.25f, 3.5f), darkStone);
            Box(room, "Dais Carpet", new Vector3(0, 0.255f, 6.4f), new Vector3(6, 0.01f, 3f), carpet, false);

            // Throne.
            var throne = Group("Throne", room);
            Box(throne, "Seat", new Vector3(0, 0.6f, 7.2f), new Vector3(1.2f, 0.7f, 1.0f), woodDark);
            Box(throne, "Back", new Vector3(0, 1.7f, 7.65f), new Vector3(1.3f, 2.6f, 0.2f), redCloth);
            Box(throne, "Back Frame", new Vector3(0, 3.05f, 7.62f), new Vector3(1.5f, 0.2f, 0.3f), gold);
            Box(throne, "Arm L", new Vector3(-0.65f, 0.95f, 7.2f), new Vector3(0.15f, 0.2f, 1f), gold);
            Box(throne, "Arm R", new Vector3(0.65f, 0.95f, 7.2f), new Vector3(0.15f, 0.2f, 1f), gold);

            // Queen's goblet on a small table.
            var side = Group("Queen's Side Table", room);
            Cyl(side, "Top", new Vector3(-1.5f, 1.0f, 6.6f), new Vector3(0.7f, 0.04f, 0.7f), woodDark);
            Cyl(side, "Leg", new Vector3(-1.5f, 0.62f, 6.6f), new Vector3(0.12f, 0.36f, 0.12f), woodDark);
            Cyl(side, "Goblet", new Vector3(-1.5f, 1.12f, 6.6f), new Vector3(0.12f, 0.08f, 0.12f), gold, false);
            Searchable(side.gameObject, "Queen's Goblet", "Examine", Hall,
                "The Queen's own goblet, polished gold, empty and perfectly clean. It is filled at the toast straight from her private cask.",
                null, "Nothing has touched this cup yet. If there is poison, it is waiting somewhere else.", new Vector3(-1.5f, 0.25f, 5.8f), 0.3f, 1.3f);

            // Royal chest beside the throne.
            var chest = Group("Royal Chest", room);
            Box(chest, "Body", new Vector3(2.6f, 0.55f, 7.3f), new Vector3(1.3f, 0.6f, 0.75f), woodLight);
            Box(chest, "Band", new Vector3(2.6f, 0.55f, 7.3f), new Vector3(1.32f, 0.08f, 0.77f), gold, false);
            var lidHinge = Group("Lid Hinge", chest);
            lidHinge.position = new Vector3(2.6f, 0.85f, 7.67f);
            Box(lidHinge, "Lid", new Vector3(2.6f, 0.9f, 7.3f), new Vector3(1.34f, 0.1f, 0.77f), woodLight, false);
            Searchable(chest.gameObject, "Royal Chest", "Search", Hall, "A carved chest holding the Queen's ceremonial things.",
                null, "Spare gloves, a prayer book and a velvet crown cushion. Nothing amiss.", new Vector3(2.6f, 0.25f, 6.5f), 0.3f, 1.2f,
                lidHinge, Vector3.zero, new Vector3(-75, 0, 0));

            // Banquet table and benches.
            var table = Group("Banquet Table", room);
            Box(table, "Top", new Vector3(0, 0.8f, 0), new Vector3(10, 0.12f, 1.8f), woodDark);
            Box(table, "Runner", new Vector3(0, 0.865f, 0), new Vector3(9.6f, 0.01f, 0.6f), whiteCloth, false);
            foreach (var x in new[] { -4.6f, 0f, 4.6f }) Box(table, "Trestle", new Vector3(x, 0.38f, 0), new Vector3(0.2f, 0.74f, 1.4f), woodDark);
            foreach (var z in new[] { -1.55f, 1.55f })
            {
                Box(table, "Bench", new Vector3(0, 0.45f, z), new Vector3(9.5f, 0.1f, 0.45f), woodLight);
                foreach (var x in new[] { -4.2f, 0f, 4.2f }) Box(table, "Bench Leg", new Vector3(x, 0.2f, z), new Vector3(0.12f, 0.4f, 0.4f), woodLight);
            }
            for (int i = 0; i < 8; i++)
            {
                float x = -4.2f + i * 1.2f;
                foreach (var z in new[] { -0.55f, 0.55f })
                {
                    Cyl(table, "Plate", new Vector3(x, 0.87f, z), new Vector3(0.36f, 0.01f, 0.36f), iron, false);
                    Cyl(table, "Cup", new Vector3(x + 0.3f, 0.93f, z * 0.7f), new Vector3(0.09f, 0.06f, 0.09f), iron, false);
                }
            }
            foreach (var x in new[] { -3.5f, 0f, 3.5f }) Candle(table, new Vector3(x, 0.86f, 0), x == 0f);
            // Food.
            Sphere(table, "Roast", new Vector3(-1.6f, 0.95f, 0), new Vector3(0.6f, 0.3f, 0.4f), Mat("Roast", new Color(0.45f, 0.22f, 0.08f)), false);
            Sphere(table, "Bread", new Vector3(1.8f, 0.92f, 0.1f), new Vector3(0.35f, 0.18f, 0.25f), Mat("Bread", new Color(0.78f, 0.56f, 0.3f)), false);
            Sphere(table, "Apples", new Vector3(2.5f, 0.92f, -0.15f), new Vector3(0.18f, 0.18f, 0.18f), redCloth, false);

            // Chandeliers.
            foreach (var x in new[] { -3.5f, 3.5f }) Chandelier(room, new Vector3(x, 5.0f, 0), x < 0f);

            // Fireplace on the south wall.
            var fp = Group("Fireplace", room);
            Box(fp, "Hearth L", new Vector3(-6.6f, 1.3f, -7.45f), new Vector3(0.6f, 2.6f, 1.0f), darkStone);
            Box(fp, "Hearth R", new Vector3(-3.4f, 1.3f, -7.45f), new Vector3(0.6f, 2.6f, 1.0f), darkStone);
            Box(fp, "Mantel", new Vector3(-5f, 2.75f, -7.35f), new Vector3(4.0f, 0.3f, 1.2f), darkStone);
            Box(fp, "Chimney", new Vector3(-5f, 4.9f, -7.6f), new Vector3(3.0f, 4.0f, 0.7f), darkStone);
            Box(fp, "Firebox", new Vector3(-5f, 1.2f, -7.75f), new Vector3(2.6f, 2.4f, 0.3f), black, false);
            Box(fp, "Hearthstone", new Vector3(-5f, 0.05f, -6.9f), new Vector3(3.6f, 0.1f, 0.6f), darkStone, false);
            for (int i = 0; i < 4; i++)
                Box(fp, "Log", new Vector3(-5.6f + i * 0.4f, 0.25f, -7.4f), new Vector3(0.9f, 0.18f, 0.18f), woodDark, false, new Vector3(0, 20 + i * 35, 0));
            Box(fp, "Flames", new Vector3(-5f, 0.6f, -7.4f), new Vector3(1.2f, 0.6f, 0.4f), fire, false);
            Box(fp, "Flames High", new Vector3(-4.9f, 0.95f, -7.45f), new Vector3(0.6f, 0.5f, 0.3f), fire, false);
            Light(fp, "Fire Light", new Vector3(-5f, 1.2f, -6.6f), new Color(1f, 0.55f, 0.25f), 3.5f, 13f, true, 0.3f);
            Searchable(fp.gameObject, "Fireplace", "Search", Hall, "A roaring fire. Ash and scraps of paper have collected at the edge of the hearth.",
                ClueByID("edric_note"), "", new Vector3(-5f, 0, -6.3f), 0.6f, 1.6f);

            // Decorative main doors.
            var doors = Group("Main Doors", room);
            Box(doors, "Door L", new Vector3(3.85f, 1.9f, -7.75f), new Vector3(1.3f, 3.8f, 0.15f), woodDark);
            Box(doors, "Door R", new Vector3(5.15f, 1.9f, -7.75f), new Vector3(1.3f, 3.8f, 0.15f), woodDark);
            Box(doors, "Arch", new Vector3(4.5f, 3.95f, -7.75f), new Vector3(3.0f, 0.3f, 0.25f), darkStone);
            Cyl(doors, "Ring L", new Vector3(4.2f, 1.5f, -7.65f), new Vector3(0.18f, 0.02f, 0.18f), iron, false, new Vector3(90, 0, 0));
            Cyl(doors, "Ring R", new Vector3(4.8f, 1.5f, -7.65f), new Vector3(0.18f, 0.02f, 0.18f), iron, false, new Vector3(90, 0, 0));

            // Banners and windows.
            Banner(room, new Vector3(-2.8f, 4.0f, 7.75f), 0, redCloth);
            Banner(room, new Vector3(2.8f, 4.0f, 7.75f), 0, redCloth);
            Banner(room, new Vector3(9.75f, 4.2f, 4.5f), 90, blueCloth);
            Banner(room, new Vector3(9.75f, 4.2f, -4.5f), 90, blueCloth);
            Banner(room, new Vector3(-9.75f, 4.2f, 4.5f), -90, blueCloth);
            Banner(room, new Vector3(-9.75f, 4.2f, -4.5f), -90, redCloth);
            foreach (var x in new[] { -6.5f, 6.5f })
            {
                Box(room, "Window", new Vector3(x, 4.2f, 7.78f), new Vector3(1.0f, 2.6f, 0.05f), moon, false);
                Box(room, "Window Bar", new Vector3(x, 4.2f, 7.74f), new Vector3(0.06f, 2.6f, 0.04f), iron, false);
            }
            Light(room, "Moonlight", new Vector3(0, 5.5f, 6.5f), new Color(0.5f, 0.6f, 0.9f), 1.0f, 12f, false, 0f);

            // Wall sconces.
            Sconce(room, new Vector3(-9.7f, 2.6f, -1.2f), 90);
            Sconce(room, new Vector3(-9.7f, 2.6f, 1.2f), 90);
            Sconce(room, new Vector3(9.7f, 2.6f, -1.2f), -90);
            Sconce(room, new Vector3(9.7f, 2.6f, 1.2f), -90);

            // Lord Halvard's writing desk against the east wall.
            var desk = Group("Writing Desk", room);
            Box(desk, "Top", new Vector3(9.0f, 0.78f, 5.6f), new Vector3(0.9f, 0.08f, 1.8f), woodLight);
            Box(desk, "Body", new Vector3(9.15f, 0.4f, 5.6f), new Vector3(0.6f, 0.72f, 1.7f), woodDark);
            Box(desk, "Papers", new Vector3(8.95f, 0.83f, 5.4f), new Vector3(0.4f, 0.01f, 0.5f), parchment, false, new Vector3(0, 12, 0));
            Cyl(desk, "Inkwell", new Vector3(9.1f, 0.86f, 6.1f), new Vector3(0.08f, 0.05f, 0.08f), black, false);
            var drawer = Box(desk, "Drawer", new Vector3(8.82f, 0.6f, 5.6f), new Vector3(0.1f, 0.18f, 0.7f), woodLight, false).transform;
            Searchable(desk.gameObject, "Writing Desk", "Search", Hall, "Lord Halvard's desk. Seating plans, a speech for tonight, and a locked-looking drawer that isn't locked.",
                ClueByID("audit_decree"), "", new Vector3(8.0f, 0, 5.6f), 0.4f, 1.3f, drawer, new Vector3(-0.35f, 0, 0), Vector3.zero);

            // Guard cabinet.
            var cab = Group("Guard Cabinet", room);
            Box(cab, "Body", new Vector3(9.5f, 1.1f, -5.0f), new Vector3(0.6f, 2.2f, 1.2f), woodDark);
            var hinge = Group("Door Hinge", cab);
            hinge.position = new Vector3(9.18f, 1.1f, -4.42f);
            Box(hinge, "Door", new Vector3(9.18f, 1.1f, -5.0f), new Vector3(0.06f, 2.0f, 1.12f), woodLight, false);
            Box(cab, "Shield", new Vector3(9.15f, 1.5f, -5.0f), new Vector3(0.04f, 0.6f, 0.5f), redCloth, false);
            Searchable(cab.gameObject, "Guard Cabinet", "Search", Hall, "The guard's cabinet: spare helmets, a ring of minor keys, and today's duty sheets.",
                ClueByID("patrol_roster"), "", new Vector3(8.5f, 0, -5.0f), 0.4f, 1.5f, hinge, Vector3.zero, new Vector3(0, -110, 0));

            // Sideboard.
            var sb = Group("Sideboard", room);
            Box(sb, "Body", new Vector3(-9.45f, 0.5f, 4.2f), new Vector3(0.6f, 1.0f, 2.0f), woodDark);
            Box(sb, "Top", new Vector3(-9.45f, 1.02f, 4.2f), new Vector3(0.7f, 0.05f, 2.1f), woodLight);
            for (int i = 0; i < 3; i++) Cyl(sb, "Jug", new Vector3(-9.45f, 1.2f, 3.6f + i * 0.6f), new Vector3(0.18f, 0.15f, 0.18f), iron, false);
            Searchable(sb.gameObject, "Sideboard", "Search", Hall, "The servants' sideboard, set out for the feast.",
                null, "Polished pewter jugs, spare candles and folded linen. Nothing out of place.", new Vector3(-8.5f, 0, 4.2f), 0.4f, 1.3f);
        }

        static void BuildStairs()
        {
            var room = Group("Stairwell");
            // Corridor walls, ceiling and the header above the basement opening.
            WallX(room, 1.5f, -18f, -10f, -3f, 6.5f, darkStone);
            WallX(room, -1.5f, -18f, -10f, -3f, 6.5f, darkStone);
            Box(room, "Ceiling", new Vector3(-14f, 3.6f, 0), new Vector3(8.4f, 0.2f, 3.4f), woodDark);
            Box(room, "Header", new Vector3(-18f, 2.15f, 0), new Vector3(T, 2.7f, 3f), darkStone);

            // Visual steps; a hidden ramp does the walking.
            const int steps = 12;
            float run = 8f / steps;
            for (int i = 0; i < steps; i++)
            {
                float x0 = -10f - run * i;
                float top = -(i + 1) * 0.25f;
                Box(room, "Step", new Vector3(x0 - run * 0.5f, (top - 3.2f) * 0.5f, 0), new Vector3(run, top + 3.2f, 3f), darkStone, false);
            }
            float len = Mathf.Sqrt(64f + 9f) + 0.6f;
            float angle = Mathf.Atan2(3f, 8f) * Mathf.Rad2Deg;
            var ramp = Box(room, "Ramp (walk surface)", new Vector3(-14f, -1.5f, 0) - new Vector3(-Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad), 0) * 0.05f,
                new Vector3(len, 0.1f, 3f), darkStone, true, new Vector3(0, 0, angle));
            ramp.GetComponent<MeshRenderer>().enabled = false;

            Sconce(room, new Vector3(-12.5f, 1.2f, 1.3f), 180, 1.6f);
            Sconce(room, new Vector3(-16.5f, -0.6f, -1.3f), 0, 1.6f);
        }

        static void BuildLibrary()
        {
            var room = Group(Library);
            Floor(room, 10, 24, -6, 6, 0.004f, woodLight);
            Ceiling(room, 10, 24, -6, 6, 5, woodDark);
            WallX(room, 6, 10, 24, 0, 5, stoneWall);
            WallX(room, -6, 10, 24, 0, 5, stoneWall);
            WallZ(room, 24, -6, 6, 0, 5, stoneWall);
            Box(room, "Rug", new Vector3(17.5f, 0.01f, 0), new Vector3(7f, 0.02f, 5f), blueCloth, false);

            // Bookshelves lining the walls.
            foreach (var x in new[] { 11.6f, 14.2f, 16.8f, 22.4f }) Bookshelf(room, new Vector3(x, 0, 5.6f), 180);
            foreach (var x in new[] { 11.6f, 14.2f, 19.8f, 22.4f }) Bookshelf(room, new Vector3(x, 0, -5.6f), 0);
            Bookshelf(room, new Vector3(23.6f, 0, 2.2f), -90);

            // One shelf is searchable.
            var shelf = Bookshelf(room, new Vector3(17.0f, 0, -5.6f), 0);
            Searchable(shelf, "Bookshelf", "Search", Library, "Rows of heavy books, some of them recently moved.",
                null, "Histories of the Aldmere kings, a bestiary and a surprisingly greasy cookbook. Dust, mostly.", new Vector3(17.0f, 0, -4.6f), 0.4f, 1.6f);

            // Treasurer's desk.
            var desk = Group("Treasurer's Desk", room);
            Box(desk, "Top", new Vector3(13.8f, 0.8f, 4.0f), new Vector3(1.9f, 0.08f, 0.9f), woodDark);
            Box(desk, "Body", new Vector3(13.8f, 0.4f, 4.15f), new Vector3(1.8f, 0.72f, 0.6f), woodDark);
            for (int i = 0; i < 4; i++)
                Box(desk, "Ledger", new Vector3(13.2f + i * 0.08f, 0.88f + i * 0.07f, 4.1f), new Vector3(0.45f, 0.07f, 0.32f),
                    Mat("Ledger " + i, Color.Lerp(new Color(0.2f, 0.32f, 0.18f), new Color(0.35f, 0.2f, 0.12f), i / 3f)), false, new Vector3(0, i * 6, 0));
            Cyl(desk, "Green Ink", new Vector3(14.4f, 0.88f, 4.1f), new Vector3(0.08f, 0.05f, 0.08f), glassGreen, false);
            var dr = Box(desk, "Drawer", new Vector3(13.8f, 0.6f, 3.82f), new Vector3(0.8f, 0.18f, 0.1f), woodLight, false).transform;
            Candle(desk, new Vector3(14.6f, 0.84f, 4.25f), true);
            Searchable(desk.gameObject, "Treasurer's Desk", "Search", Library, "Master Vale's desk. Ledgers stacked with fussy precision, a pot of green ink, and a drawer.",
                ClueByID("debt_letter"), "", new Vector3(13.8f, 0, 3.0f), 0.4f, 1.3f, dr, new Vector3(0, 0, -0.4f), Vector3.zero);

            // Long reading table.
            var rt = Group("Reading Table", room);
            Box(rt, "Top", new Vector3(18f, 0.8f, 0.5f), new Vector3(3.6f, 0.1f, 1.3f), woodDark);
            foreach (var x in new[] { 16.5f, 19.5f }) Box(rt, "Leg", new Vector3(x, 0.38f, 0.5f), new Vector3(0.18f, 0.75f, 1.0f), woodDark);
            Box(rt, "Open Book", new Vector3(17.4f, 0.87f, 0.4f), new Vector3(0.6f, 0.03f, 0.42f), parchment, false, new Vector3(0, -8, 0));
            Box(rt, "Ledger", new Vector3(18.8f, 0.88f, 0.6f), new Vector3(0.45f, 0.05f, 0.32f), Mat("Ledger 0", new Color(0.2f, 0.32f, 0.18f)), false, new Vector3(0, 15, 0));
            for (int i = 0; i < 3; i++) Cyl(rt, "Scroll", new Vector3(16.8f + i * 0.25f, 0.9f, 0.9f), new Vector3(0.08f, 0.25f, 0.08f), parchment, false, new Vector3(0, 0, 90));
            Candle(rt, new Vector3(18.0f, 0.85f, 0.85f), true);
            Searchable(rt.gameObject, "Reading Table", "Examine", Library, "Master Ilric's notes on herbs lie open beside one of Master Vale's ledgers.",
                null, "The ledger's last entry stops halfway down a column. The green ink on the final figure has long since dried.",
                new Vector3(18.0f, 0, -0.5f), 0.6f, 1.3f);

            // Reagent cabinet.
            var cab = Group("Reagent Cabinet", room);
            Box(cab, "Body", new Vector3(23.5f, 1.1f, -3.0f), new Vector3(0.6f, 2.2f, 1.4f), woodDark);
            for (int r = 0; r < 3; r++)
            for (int i = 0; i < 4; i++)
            {
                if (r == 1 && i == 2) continue; // the empty slot
                var glass = (r + i) % 3 == 0 ? glassGreen : (r + i) % 3 == 1 ? glassBlue : glassPurple;
                Cyl(cab, "Bottle", new Vector3(23.15f, 0.75f + r * 0.5f, -3.5f + i * 0.33f), new Vector3(0.12f, 0.12f, 0.12f), glass, false);
            }
            var cabHinge = Group("Door Hinge", cab);
            cabHinge.position = new Vector3(23.16f, 1.1f, -2.32f);
            Box(cabHinge, "Glass Door", new Vector3(23.16f, 1.1f, -3.0f), new Vector3(0.04f, 2.0f, 1.32f), Mat("Cabinet Glass", new Color(0.6f, 0.7f, 0.75f, 1f), 0.9f), false);
            Searchable(cab.gameObject, "Reagent Cabinet", "Search", Library, "Master Ilric's cabinet of tinctures and powders, each in its labelled slot. The lock hangs open.",
                ClueByID("reagent_list"), "", new Vector3(22.4f, 0, -3.0f), 0.4f, 1.5f, cabHinge, Vector3.zero, new Vector3(0, 100, 0));

            // Physician's satchel on a bench.
            var bench = Group("Physician's Satchel", room);
            Box(bench, "Bench", new Vector3(12.6f, 0.45f, -5.0f), new Vector3(1.8f, 0.1f, 0.5f), woodLight);
            foreach (var x in new[] { 11.9f, 13.3f }) Box(bench, "Leg", new Vector3(x, 0.2f, -5.0f), new Vector3(0.1f, 0.4f, 0.4f), woodLight);
            Box(bench, "Satchel", new Vector3(12.4f, 0.68f, -5.0f), new Vector3(0.55f, 0.35f, 0.3f), Mat("Leather", new Color(0.3f, 0.17f, 0.09f)), false);
            Box(bench, "Strap", new Vector3(12.4f, 0.78f, -5.0f), new Vector3(0.57f, 0.04f, 0.32f), black, false);
            Searchable(bench.gameObject, "Physician's Satchel", "Search", Library, "Doctor Mordaunt's leather satchel: bandages, lancets and a small locked case.",
                ClueByID("poison_inventory"), "", new Vector3(12.6f, 0, -4.2f), 0.3f, 1.2f);

            // Scroll rack.
            var rack = Group("Scroll Rack", room);
            Box(rack, "Frame", new Vector3(19.4f, 1.2f, 5.65f), new Vector3(1.6f, 2.4f, 0.5f), woodDark);
            for (int r = 0; r < 4; r++)
            for (int i = 0; i < 5; i++)
                Cyl(rack, "Scroll", new Vector3(18.85f + i * 0.28f, 0.5f + r * 0.5f, 5.45f), new Vector3(0.14f, 0.22f, 0.14f), parchment, false, new Vector3(90, 0, 0));
            Searchable(rack.gameObject, "Scroll Rack", "Search", Library, "Rolled charters, deeds and genealogies. One roll has been put back crooked.",
                ClueByID("succession_roll"), "", new Vector3(19.4f, 0, 4.6f), 0.4f, 1.5f);

            // Globe.
            Cyl(room, "Globe Stand", new Vector3(21.5f, 0.45f, 3.4f), new Vector3(0.2f, 0.45f, 0.2f), woodDark);
            Sphere(room, "Globe", new Vector3(21.5f, 1.15f, 3.4f), Vector3.one * 0.55f, Mat("Globe", new Color(0.55f, 0.48f, 0.3f)), false);

            Light(room, "Library Light A", new Vector3(14.0f, 3.6f, 2.0f), new Color(1f, 0.75f, 0.45f), 2.2f, 10f, false, 0.12f);
            Light(room, "Library Light B", new Vector3(20.5f, 3.6f, -1.5f), new Color(1f, 0.75f, 0.45f), 2.2f, 10f, false, 0.12f);
        }

        static void BuildBasement()
        {
            var room = Group(Basement);
            Floor(room, -32, -18, -7, 7, -3, darkStone);
            Ceiling(room, -32, -18, -7, 7, 0.8f, woodDark);
            WallX(room, 7, -32, -18, -3, 3.8f, darkStone);
            WallX(room, -7, -32, -18, -3, 3.8f, darkStone);
            WallZ(room, -32, -7, 7, -3, 3.8f, darkStone);
            WallZ(room, -18, -7, 7, -3, 3.8f, darkStone, new Vector3(0, 3, 3.8f));

            for (float x = -29; x <= -21; x += 4)
            {
                Box(room, "Post", new Vector3(x, -1.1f, -2.2f), new Vector3(0.3f, 3.8f, 0.3f), woodDark);
                Box(room, "Post", new Vector3(x, -1.1f, 2.6f), new Vector3(0.3f, 3.8f, 0.3f), woodDark);
                Box(room, "Joist", new Vector3(x, 0.55f, 0), new Vector3(0.3f, 0.3f, 14f), woodDark, false);
            }

            // Barrels by the north wall; the searchable spot is behind them.
            var barrels = Group("Barrels", room);
            for (int i = 0; i < 6; i++)
            {
                float x = -31.0f + (i % 3) * 0.95f;
                float z = 5.9f - (i / 3) * 0.95f;
                Barrel(barrels, new Vector3(x, -3f, z), false);
            }
            Barrel(barrels, new Vector3(-30.05f, -3f + 0.92f, 5.9f), false);
            Searchable(barrels.gameObject, "Behind the Barrels", "Search", Basement, "A wall of barrels, close to the Queen's cask. Something glints in the gap behind them.",
                ClueByID("empty_vial"), "", new Vector3(-29.0f, -3f, 4.3f), 0.6f, 1.4f);

            // The Queen's cask on a cradle by the west wall.
            var cask = Group("Queen's Cask", room);
            Box(cask, "Cradle", new Vector3(-31.1f, -2.75f, 1.5f), new Vector3(1.0f, 0.5f, 1.6f), woodDark);
            var caskBody = Cyl(cask, "Cask", new Vector3(-31.1f, -1.95f, 1.5f), new Vector3(1.15f, 0.8f, 1.15f), barrelWood, true, new Vector3(0, 0, 0));
            caskBody.transform.localRotation = Quaternion.Euler(0, 0, 90);
            caskBody.transform.localScale = new Vector3(1.15f, 0.8f, 1.15f);
            Cyl(cask, "Gold Band", new Vector3(-31.1f, -1.95f, 1.5f), new Vector3(1.2f, 0.06f, 1.2f), gold, false, new Vector3(0, 0, 90));
            Cyl(cask, "Seal", new Vector3(-30.3f, -1.95f, 1.5f), new Vector3(0.18f, 0.03f, 0.18f), redCloth, false, new Vector3(0, 0, 90));
            Searchable(cask.gameObject, "Queen's Cask", "Examine", Basement, "A great cask bound with a gold band and stamped with the royal arms. This is the Queen's own Aldmere Red.",
                ClueByID("resealed_cask"), "", new Vector3(-29.8f, -3f, 1.5f), 0.4f, 1.4f);

            // Wine racks.
            for (int i = 0; i < 2; i++)
            {
                var wr = Group("Wine Rack", room);
                Box(wr, "Frame", new Vector3(-31.5f, -2.0f, -1.2f - i * 1.6f), new Vector3(0.6f, 2f, 1.4f), woodDark);
                for (int r = 0; r < 4; r++)
                for (int b = 0; b < 4; b++)
                    Cyl(wr, "Bottle", new Vector3(-31.15f, -2.75f + r * 0.45f, -1.7f - i * 1.6f + b * 0.33f), new Vector3(0.1f, 0.12f, 0.1f), glassGreen, false, new Vector3(0, 0, 90));
            }

            // Old cabinet.
            var old = Group("Old Cabinet", room);
            Box(old, "Body", new Vector3(-31.55f, -1.9f, -5.0f), new Vector3(0.6f, 2.2f, 1.3f), Mat("Old Wood", new Color(0.33f, 0.25f, 0.18f)));
            var oldHinge = Group("Door Hinge", old);
            oldHinge.position = new Vector3(-31.22f, -1.9f, -5.62f);
            Box(oldHinge, "Door", new Vector3(-31.22f, -1.9f, -5.0f), new Vector3(0.05f, 2.0f, 1.22f), woodLight, false);
            Searchable(old.gameObject, "Old Cabinet", "Search", Basement, "A warped cabinet that smells of mould. Rags, a broken lantern, and a folded paper tucked under them.",
                ClueByID("wine_tally"), "", new Vector3(-30.5f, -3f, -5.0f), 0.4f, 1.5f, oldHinge, Vector3.zero, new Vector3(0, 105, 0));

            // Pantry shelf by the south wall.
            var pantry = Group("Pantry Shelf", room);
            Box(pantry, "Frame", new Vector3(-27f, -1.9f, -6.6f), new Vector3(2.4f, 2.2f, 0.5f), woodDark);
            for (int r = 0; r < 3; r++)
            {
                Box(pantry, "Board", new Vector3(-27f, -2.6f + r * 0.6f, -6.4f), new Vector3(2.3f, 0.05f, 0.45f), woodLight, false);
                for (int i = 0; i < 5; i++)
                    Cyl(pantry, "Jar", new Vector3(-27.9f + i * 0.45f, -2.45f + r * 0.6f, -6.35f), new Vector3(0.2f, 0.12f, 0.2f),
                        i % 2 == 0 ? Mat("Clay", new Color(0.6f, 0.36f, 0.22f)) : sack, false);
            }
            Box(pantry, "Order Slip", new Vector3(-26.4f, -1.39f, -6.35f), new Vector3(0.3f, 0.01f, 0.2f), parchment, false);
            Searchable(pantry.gameObject, "Pantry Shelf", "Search", Basement, "Jars of preserves, salt and honey for the kitchens. A slip of paper is pinned to the top board.",
                ClueByID("toast_order"), "", new Vector3(-27f, -3f, -5.7f), 0.4f, 1.5f);

            // Crates.
            var crates = Group("Crates", room);
            Box(crates, "Crate", new Vector3(-23.8f, -2.6f, -6.0f), new Vector3(0.9f, 0.8f, 0.9f), woodLight, true, new Vector3(0, 8, 0));
            Box(crates, "Crate", new Vector3(-23.8f, -1.8f, -6.0f), new Vector3(0.75f, 0.7f, 0.75f), woodLight, true, new Vector3(0, -12, 0));
            Box(crates, "Crate", new Vector3(-24.9f, -2.6f, -5.9f), new Vector3(0.9f, 0.8f, 0.9f), woodLight, true, new Vector3(0, -5, 0));
            var supply = Group("Supply Crate", room);
            Box(supply, "Body", new Vector3(-21.2f, -2.65f, -5.8f), new Vector3(1.4f, 0.7f, 0.8f), woodLight);
            var lid = Group("Lid Hinge", supply);
            lid.position = new Vector3(-21.2f, -2.28f, -6.2f);
            Box(lid, "Lid", new Vector3(-21.2f, -2.27f, -5.8f), new Vector3(1.44f, 0.06f, 0.84f), woodLight, false);
            Searchable(supply.gameObject, "Supply Crate", "Search", Basement, "A long crate, freshly nailed, with sawdust on the floor around it.",
                ClueByID("dagger_order"), "", new Vector3(-21.2f, -3f, -4.7f), 0.4f, 1.2f, lid, Vector3.zero, new Vector3(-80, 0, 0));

            // Grain sacks.
            var sacks = Group("Grain Sacks", room);
            for (int i = 0; i < 5; i++)
                Sphere(sacks, "Sack", new Vector3(-25.6f + (i % 3) * 0.6f, -2.72f + (i / 3) * 0.45f, 0.6f + (i % 2) * 0.3f), new Vector3(0.65f, 0.55f, 0.5f), sack, i < 3);
            Searchable(sacks.gameObject, "Grain Sacks", "Search", Basement, "Sacks of grain, stacked for the winter.",
                null, "Grain, more grain, and one very surprised mouse.", new Vector3(-25.0f, -3f, -0.4f), 0.5f, 1.2f);

            // The forge and Gunnar's workbench.
            var forge = Group("Forge", room);
            Box(forge, "Hearth", new Vector3(-19.0f, -2.4f, 6.2f), new Vector3(1.6f, 1.2f, 1.2f), Mat("Brick", new Color(0.42f, 0.2f, 0.14f)));
            Box(forge, "Coals", new Vector3(-19.0f, -1.78f, 6.2f), new Vector3(1.2f, 0.06f, 0.8f), forgeGlow, false);
            Box(forge, "Hood", new Vector3(-19.0f, -0.3f, 6.4f), new Vector3(1.4f, 1.6f, 0.9f), darkStone);
            Light(forge, "Forge Glow", new Vector3(-19.2f, -1.2f, 5.3f), new Color(1f, 0.45f, 0.15f), 2.8f, 8f, true, 0.2f);
            var anvil = Group("Anvil", room);
            Box(anvil, "Block", new Vector3(-21.0f, -2.7f, 3.4f), new Vector3(0.5f, 0.6f, 0.5f), woodDark);
            Box(anvil, "Anvil", new Vector3(-21.0f, -2.28f, 3.4f), new Vector3(0.75f, 0.25f, 0.32f), iron);

            var bench = Group("Blacksmith's Workbench", room);
            Box(bench, "Top", new Vector3(-21.6f, -2.15f, 6.35f), new Vector3(2.0f, 0.1f, 0.8f), woodDark);
            foreach (var x in new[] { -22.45f, -20.75f }) Box(bench, "Leg", new Vector3(x, -2.6f, 6.35f), new Vector3(0.12f, 0.85f, 0.7f), woodDark);
            Box(bench, "Hammer", new Vector3(-22.0f, -2.06f, 6.3f), new Vector3(0.45f, 0.05f, 0.06f), iron, false, new Vector3(0, 30, 0));
            Box(bench, "Tongs", new Vector3(-21.2f, -2.07f, 6.5f), new Vector3(0.5f, 0.03f, 0.05f), iron, false, new Vector3(0, -15, 0));
            Box(bench, "Ledger", new Vector3(-21.0f, -2.07f, 6.15f), new Vector3(0.42f, 0.05f, 0.3f), parchment, false, new Vector3(0, 10, 0));
            Box(bench, "Key Hook", new Vector3(-21.6f, -1.3f, 6.78f), new Vector3(0.6f, 0.08f, 0.05f), iron, false);
            Searchable(bench.gameObject, "Blacksmith's Workbench", "Search", Basement, "Gunnar's bench: tongs, hammers, an empty key hook and a battered ledger.",
                ClueByID("key_ledger"), "", new Vector3(-21.6f, -3f, 5.3f), 0.4f, 1.3f);

            Torch(room, new Vector3(-24.0f, -1.4f, 6.8f), 180);
            Torch(room, new Vector3(-31.8f, -1.4f, -1.0f), 90);
            Torch(room, new Vector3(-25.0f, -1.4f, -6.8f), 0);
            Torch(room, new Vector3(-18.2f, -1.4f, -3.5f), -90);
        }

        // =====================================================================
        // Furniture and props
        // =====================================================================

        static GameObject Bookshelf(Transform parent, Vector3 pos, float yaw)
        {
            var shelf = new GameObject("Bookshelf");
            shelf.transform.SetParent(parent, false);
            shelf.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var t = shelf.transform;
            LocalBox(t, "Frame", new Vector3(0, 1.6f, 0), new Vector3(2.4f, 3.2f, 0.5f), woodDark, true);
            for (int r = 0; r < 4; r++)
            {
                float y = 0.35f + r * 0.75f;
                LocalBox(t, "Board", new Vector3(0, y - 0.05f, 0.05f), new Vector3(2.3f, 0.05f, 0.45f), woodLight, false);
                float x = -1.05f;
                while (x < 1.0f)
                {
                    float w = 0.12f + Rand() * 0.18f;
                    float h = 0.4f + Rand() * 0.25f;
                    var book = BookMaterial();
                    LocalBox(t, "Books", new Vector3(x + w * 0.5f, y + h * 0.5f, 0.12f), new Vector3(w * 0.92f, h, 0.32f), book, false);
                    x += w;
                }
            }
            return shelf;
        }

        static void Barrel(Transform parent, Vector3 basePos, bool lying)
        {
            var b = Cyl(parent, "Barrel", basePos + new Vector3(0, 0.45f, 0), new Vector3(0.85f, 0.45f, 0.85f), barrelWood);
            if (lying) b.transform.localRotation = Quaternion.Euler(0, 0, 90);
            Cyl(parent, "Hoop", basePos + new Vector3(0, 0.2f, 0), new Vector3(0.88f, 0.03f, 0.88f), iron, false);
            Cyl(parent, "Hoop", basePos + new Vector3(0, 0.7f, 0), new Vector3(0.88f, 0.03f, 0.88f), iron, false);
        }

        static void Candle(Transform parent, Vector3 pos, bool withLight)
        {
            Cyl(parent, "Candle", pos + new Vector3(0, 0.12f, 0), new Vector3(0.07f, 0.12f, 0.07f), wax, false);
            Sphere(parent, "Flame", pos + new Vector3(0, 0.29f, 0), new Vector3(0.05f, 0.09f, 0.05f), flame, false);
            if (withLight) Light(parent, "Candle Light", pos + new Vector3(0, 0.45f, 0), new Color(1f, 0.7f, 0.4f), 1.2f, 4.5f, false, 0.2f);
        }

        static void Chandelier(Transform parent, Vector3 pos, bool shadows)
        {
            var g = Group("Chandelier", parent);
            Cyl(g, "Chain", pos + new Vector3(0, 1.0f, 0), new Vector3(0.05f, 1.0f, 0.05f), iron, false);
            Cyl(g, "Ring", pos, new Vector3(2.0f, 0.05f, 2.0f), iron, false);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Candle(g, pos + new Vector3(Mathf.Cos(a) * 0.95f, 0.04f, Mathf.Sin(a) * 0.95f), false);
            }
            Light(g, "Chandelier Light", pos + new Vector3(0, -0.3f, 0), new Color(1f, 0.72f, 0.42f), 3.2f, 14f, shadows, 0.1f);
        }

        static void Sconce(Transform parent, Vector3 pos, float yaw, float intensity = 1.8f)
        {
            var g = Group("Sconce", parent);
            Vector3 fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            Box(g, "Bracket", pos, new Vector3(0.12f, 0.35f, 0.12f), iron, false, new Vector3(0, yaw, 0));
            Cyl(g, "Torch", pos + fwd * 0.12f + new Vector3(0, 0.25f, 0), new Vector3(0.08f, 0.2f, 0.08f), woodDark, false, new Vector3(0, yaw, 0) + new Vector3(-15, 0, 0));
            Sphere(g, "Flame", pos + fwd * 0.16f + new Vector3(0, 0.5f, 0), new Vector3(0.14f, 0.22f, 0.14f), flame, false);
            Light(g, "Sconce Light", pos + fwd * 0.45f + new Vector3(0, 0.5f, 0), new Color(1f, 0.6f, 0.3f), intensity, 7f, false, 0.25f);
        }

        static void Torch(Transform parent, Vector3 pos, float yaw) => Sconce(parent, pos, yaw, 2.4f);

        static void Banner(Transform parent, Vector3 pos, float yaw, Material cloth)
        {
            var g = Group("Banner", parent);
            g.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            LocalBox(g, "Cloth", Vector3.zero, new Vector3(1.4f, 3.2f, 0.04f), cloth, false);
            LocalBox(g, "Stripe", new Vector3(0, 0, -0.03f), new Vector3(0.3f, 3.2f, 0.02f), gold, false);
            LocalBox(g, "Crest", new Vector3(0, 0.5f, -0.04f), new Vector3(0.6f, 0.6f, 0.02f), gold, false, new Vector3(0, 0, 45));
            LocalBox(g, "Rod", new Vector3(0, 1.65f, 0), new Vector3(1.7f, 0.08f, 0.08f), iron, false);
        }

        // =====================================================================
        // Characters
        // =====================================================================

        static NPCDialogue NPC(Transform parent, SuspectData s, Vector3 pos, float yaw)
        {
            var go = new GameObject(s.displayName) { layer = IgnoreRaycast };
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.9f, 0);
            col.height = 1.8f;
            col.radius = 0.35f;

            bool gown = s.id == "queen" || s.id == "servant" || s.id == "rival";
            Color beardColor = s.id == "scholar" ? new Color(0.88f, 0.88f, 0.86f)
                : s.id == "advisor" ? new Color(0.6f, 0.6f, 0.6f)
                : s.id == "blacksmith" ? new Color(0.45f, 0.2f, 0.1f)
                : new Color(0.25f, 0.17f, 0.1f);
            var model = Figure(go.transform, s.coatColor, s.accentColor, s.skinColor, s.headwear, s.heightScale, s.beard, beardColor, gown, false);

            var npc = go.AddComponent<NPCDialogue>();
            npc.Configure(s, model, 2.2f * s.heightScale);
            return npc;
        }

        /// <summary>A primitive figure; the root stays at the feet. Returns the model child that gets animated.</summary>
        static Transform Figure(Transform root, Color coat, Color accent, Color skin, Headwear hat, float height, bool beard, Color beardColor,
            bool gown, bool detective)
        {
            var model = new GameObject("Model") { layer = IgnoreRaycast }.transform;
            model.SetParent(root, false);
            var body = new GameObject("Body") { layer = IgnoreRaycast }.transform;
            body.SetParent(model, false);
            body.localScale = Vector3.one * height;
            var t = body;

            Material mCoat = CharMat(coat), mAccent = CharMat(accent), mSkin = CharMat(skin), mDark = CharMat(new Color(0.08f, 0.06f, 0.05f));

            if (gown) Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.45f, 0), new Vector3(0.82f, 0.45f, 0.72f), mCoat);
            else
            {
                Part(t, PrimitiveType.Capsule, new Vector3(-0.13f, 0.42f, 0), new Vector3(0.22f, 0.42f, 0.22f), CharMat(coat * 0.6f + Color.black * 0.4f));
                Part(t, PrimitiveType.Capsule, new Vector3(0.13f, 0.42f, 0), new Vector3(0.22f, 0.42f, 0.22f), CharMat(coat * 0.6f + Color.black * 0.4f));
            }
            Part(t, PrimitiveType.Capsule, new Vector3(0, 1.08f, 0), new Vector3(0.6f, 0.5f, 0.45f), mCoat);
            if (detective) Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.8f, -0.02f), new Vector3(0.66f, 0.32f, 0.52f), mCoat); // long coat
            Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.88f, 0), new Vector3(0.62f, 0.035f, 0.47f), mAccent); // belt
            Part(t, PrimitiveType.Cube, new Vector3(0, 1.46f, -0.02f), new Vector3(0.62f, 0.1f, 0.42f), mAccent); // collar
            // Arms and hands.
            Part(t, PrimitiveType.Capsule, new Vector3(-0.36f, 1.08f, 0), new Vector3(0.16f, 0.34f, 0.16f), mCoat, new Vector3(0, 0, -8));
            Part(t, PrimitiveType.Capsule, new Vector3(0.36f, 1.08f, 0), new Vector3(0.16f, 0.34f, 0.16f), mCoat, new Vector3(0, 0, 8));
            Part(t, PrimitiveType.Sphere, new Vector3(-0.41f, 0.76f, 0.02f), Vector3.one * 0.12f, mSkin);
            Part(t, PrimitiveType.Sphere, new Vector3(0.41f, 0.76f, 0.02f), Vector3.one * 0.12f, mSkin);
            // Head.
            Part(t, PrimitiveType.Sphere, new Vector3(0, 1.68f, 0), Vector3.one * 0.36f, mSkin);
            Part(t, PrimitiveType.Sphere, new Vector3(-0.07f, 1.71f, 0.16f), Vector3.one * 0.05f, mDark);
            Part(t, PrimitiveType.Sphere, new Vector3(0.07f, 1.71f, 0.16f), Vector3.one * 0.05f, mDark);
            Part(t, PrimitiveType.Sphere, new Vector3(0, 1.65f, 0.18f), new Vector3(0.05f, 0.07f, 0.05f), CharMat(skin * 0.92f));
            if (beard) Part(t, PrimitiveType.Sphere, new Vector3(0, 1.56f, 0.11f), new Vector3(0.24f, 0.22f, 0.16f), CharMat(beardColor));

            var mGold = CharMat(new Color(0.9f, 0.72f, 0.28f));
            var mSteel = CharMat(new Color(0.62f, 0.64f, 0.67f));
            var mWhite = CharMat(new Color(0.96f, 0.95f, 0.92f));
            switch (hat)
            {
                case Headwear.Crown:
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.88f, 0), new Vector3(0.3f, 0.06f, 0.3f), mGold);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * Mathf.PI * 2f / 5f;
                        Part(t, PrimitiveType.Cube, new Vector3(Mathf.Sin(a) * 0.13f, 1.97f, Mathf.Cos(a) * 0.13f), new Vector3(0.05f, 0.1f, 0.05f), mGold, new Vector3(0, a * Mathf.Rad2Deg, 0));
                    }
                    Part(t, PrimitiveType.Sphere, new Vector3(0, 1.62f, -0.12f), new Vector3(0.34f, 0.4f, 0.2f), CharMat(new Color(0.45f, 0.28f, 0.12f))); // hair
                    break;
                case Headwear.Circlet:
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.79f, 0), new Vector3(0.38f, 0.015f, 0.38f), mGold);
                    Part(t, PrimitiveType.Sphere, new Vector3(0, 1.74f, -0.06f), new Vector3(0.38f, 0.32f, 0.34f), CharMat(new Color(0.18f, 0.12f, 0.08f)));
                    break;
                case Headwear.Helmet:
                    Part(t, PrimitiveType.Sphere, new Vector3(0, 1.75f, 0), new Vector3(0.4f, 0.34f, 0.4f), mSteel);
                    Part(t, PrimitiveType.Cube, new Vector3(0, 1.67f, 0.19f), new Vector3(0.04f, 0.16f, 0.03f), mSteel);
                    break;
                case Headwear.ChefHat:
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.96f, 0), new Vector3(0.3f, 0.15f, 0.3f), mWhite);
                    Part(t, PrimitiveType.Sphere, new Vector3(0, 2.12f, 0), new Vector3(0.4f, 0.2f, 0.4f), mWhite);
                    break;
                case Headwear.Bonnet:
                    Part(t, PrimitiveType.Sphere, new Vector3(0, 1.74f, -0.04f), new Vector3(0.42f, 0.36f, 0.4f), mWhite);
                    break;
                case Headwear.WizardHat:
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.82f, 0), new Vector3(0.52f, 0.015f, 0.52f), mCoat);
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.93f, 0), new Vector3(0.3f, 0.1f, 0.3f), mCoat);
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.88f, 0), new Vector3(0.31f, 0.025f, 0.31f), mAccent);
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 2.1f, -0.02f), new Vector3(0.2f, 0.09f, 0.2f), mCoat, new Vector3(-6, 0, 0));
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 2.25f, -0.05f), new Vector3(0.1f, 0.07f, 0.1f), mCoat, new Vector3(-12, 0, 0));
                    break;
                case Headwear.FlatCap:
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.84f, 0), new Vector3(0.38f, 0.05f, 0.38f), CharMat(coat * 0.7f));
                    break;
                case Headwear.Skullcap:
                    Part(t, PrimitiveType.Sphere, new Vector3(0, 1.77f, -0.02f), new Vector3(0.34f, 0.2f, 0.34f), mAccent);
                    break;
                case Headwear.Hood:
                    Part(t, PrimitiveType.Sphere, new Vector3(0, 1.7f, -0.05f), new Vector3(0.44f, 0.42f, 0.44f), mCoat);
                    break;
                case Headwear.None:
                    if (detective)
                    {
                        var mHat = CharMat(new Color(0.12f, 0.1f, 0.09f));
                        Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.83f, 0), new Vector3(0.6f, 0.015f, 0.6f), mHat);
                        Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.92f, 0), new Vector3(0.32f, 0.09f, 0.32f), mHat);
                        Part(t, PrimitiveType.Cylinder, new Vector3(0, 1.87f, 0), new Vector3(0.33f, 0.02f, 0.33f), mAccent);
                    }
                    break;
            }
            return model;
        }

        static void Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.layer = IgnoreRaycast;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
        }

        // =====================================================================
        // Building blocks
        // =====================================================================

        static Transform Group(string name, Transform parent = null)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent ? parent : env, false);
            return g;
        }

        static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 worldPos, Vector3 scale, Material m, bool collide, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (!collide) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = collide;
            return go;
        }

        static GameObject Box(Transform p, string n, Vector3 pos, Vector3 size, Material m, bool collide = true, Vector3 euler = default) =>
            Prim(PrimitiveType.Cube, p, n, pos, size, m, collide, euler);

        static GameObject Cyl(Transform p, string n, Vector3 pos, Vector3 scale, Material m, bool collide = true, Vector3 euler = default) =>
            Prim(PrimitiveType.Cylinder, p, n, pos, scale, m, collide, euler);

        static GameObject Sphere(Transform p, string n, Vector3 pos, Vector3 scale, Material m, bool collide = true) =>
            Prim(PrimitiveType.Sphere, p, n, pos, scale, m, collide, Vector3.zero);

        static GameObject LocalBox(Transform p, string n, Vector3 localPos, Vector3 size, Material m, bool collide, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = n;
            go.transform.SetParent(p, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (!collide) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void Floor(Transform p, float x0, float x1, float z0, float z1, float y, Material m) =>
            Box(p, "Floor", new Vector3((x0 + x1) * 0.5f, y - 0.1f, (z0 + z1) * 0.5f), new Vector3(x1 - x0 + T, 0.2f, z1 - z0 + T), m);

        static void Ceiling(Transform p, float x0, float x1, float z0, float z1, float y, Material m) =>
            Box(p, "Ceiling", new Vector3((x0 + x1) * 0.5f, y + 0.1f, (z0 + z1) * 0.5f), new Vector3(x1 - x0 + T, 0.2f, z1 - z0 + T), m);

        /// <summary>Wall running along X at depth z. Gaps are (centre x, width, height).</summary>
        static void WallX(Transform p, float z, float x0, float x1, float y0, float h, Material m, params Vector3[] gaps)
        {
            float a = x0 - T * 0.5f, end = x1 + T * 0.5f;
            foreach (var g in gaps)
            {
                float g0 = g.x - g.y * 0.5f, g1 = g.x + g.y * 0.5f;
                if (g0 > a) Box(p, "Wall", new Vector3((a + g0) * 0.5f, y0 + h * 0.5f, z), new Vector3(g0 - a, h, T), m);
                if (g.z < h) Box(p, "Lintel", new Vector3(g.x, y0 + (g.z + h) * 0.5f, z), new Vector3(g.y, h - g.z, T), m);
                a = g1;
            }
            if (end > a) Box(p, "Wall", new Vector3((a + end) * 0.5f, y0 + h * 0.5f, z), new Vector3(end - a, h, T), m);
        }

        /// <summary>Wall running along Z at x. Gaps are (centre z, width, height).</summary>
        static void WallZ(Transform p, float x, float z0, float z1, float y0, float h, Material m, params Vector3[] gaps)
        {
            float a = z0 - T * 0.5f, end = z1 + T * 0.5f;
            foreach (var g in gaps)
            {
                float g0 = g.x - g.y * 0.5f, g1 = g.x + g.y * 0.5f;
                if (g0 > a) Box(p, "Wall", new Vector3(x, y0 + h * 0.5f, (a + g0) * 0.5f), new Vector3(T, h, g0 - a), m);
                if (g.z < h) Box(p, "Lintel", new Vector3(x, y0 + (g.z + h) * 0.5f, g.x), new Vector3(T, h - g.z, g.y), m);
                a = g1;
            }
            if (end > a) Box(p, "Wall", new Vector3(x, y0 + h * 0.5f, (a + end) * 0.5f), new Vector3(T, h, end - a), m);
        }

        static void Light(Transform p, string n, Vector3 pos, Color c, float intensity, float range, bool shadows, float flicker)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.intensity = intensity * 1.6f;
            l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            if (flicker > 0f)
            {
                var f = go.AddComponent<FlickerLight>();
                var so = new SerializedObject(f);
                so.FindProperty("amount").floatValue = flicker;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static Transform Marker(Transform parent, string n, Vector3 pos, Vector3 lookAt)
        {
            var t = new GameObject(n).transform;
            t.SetParent(parent, false);
            t.position = pos;
            Vector3 dir = lookAt - pos;
            t.rotation = Quaternion.LookRotation(dir.sqrMagnitude > 0.001f ? dir : Vector3.forward);
            if (n.StartsWith("Assassin") || n.StartsWith("Detective") || n.StartsWith("Captain"))
                t.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z).sqrMagnitude > 0.001f ? new Vector3(dir.x, 0, dir.z) : Vector3.forward);
            return t;
        }

        static void Searchable(GameObject furniture, string name, string verb, string room, string description, ClueData clue, string nothing,
            Vector3 point, float extraRange, float promptHeight, Transform openPart = null, Vector3 openOffset = default, Vector3 openEuler = default)
        {
            var s = furniture.AddComponent<SearchableObject>();
            var p = new GameObject("Interaction Point").transform;
            p.SetParent(furniture.transform, false);
            p.position = point;
            s.Configure(name, verb, room, description, clue, string.IsNullOrEmpty(nothing) ? "Nothing else of interest." : nothing, p, extraRange, promptHeight);
            if (openPart) s.ConfigureOpening(openPart, openOffset, openEuler);
        }

        static ClueData ClueByID(string id)
        {
            var c = AssetDatabase.LoadAssetAtPath<ClueData>($"{MysteryContent.Root}/Clues/{id}.asset");
            if (!c) Debug.LogError($"[AssassinMystery] Missing clue asset '{id}'. Run Tools > Assassin Mystery > Regenerate Mystery Data.");
            return c;
        }

        static float Yaw(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static float Rand()
        {
            seed = (seed * 1103515245 + 12345) & 0x7fffffff;
            return (seed % 10000) / 10000f;
        }

        // =====================================================================
        // Lighting and materials
        // =====================================================================

        static void SetupLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.29f, 0.24f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.05f, 0.04f, 0.035f);
            RenderSettings.fogDensity = 0.012f;

            // Post-processing.
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
            if (existing) AssetDatabase.DeleteAsset(VolumePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumePath);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(0.95f);
            bloom.scatter.Override(0.65f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.5f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.9f);
            color.saturation.Override(-8f);
            color.colorFilter.Override(new Color(1f, 0.93f, 0.84f));
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);

            var vol = new GameObject("Post Processing").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;
        }

        static void CreateMaterials()
        {
            stoneFloor = Mat("Stone Floor", new Color(0.36f, 0.33f, 0.3f), 0.25f);
            stoneWall = Mat("Stone Wall", new Color(0.46f, 0.42f, 0.37f), 0.1f);
            darkStone = Mat("Dark Stone", new Color(0.27f, 0.25f, 0.23f), 0.1f);
            woodDark = Mat("Wood Dark", new Color(0.26f, 0.16f, 0.09f), 0.3f);
            woodLight = Mat("Wood Light", new Color(0.5f, 0.33f, 0.19f), 0.3f);
            iron = Mat("Iron", new Color(0.2f, 0.2f, 0.22f), 0.5f, 0.7f);
            gold = Mat("Gold", new Color(0.86f, 0.66f, 0.26f), 0.7f, 0.9f);
            redCloth = Mat("Red Cloth", new Color(0.5f, 0.06f, 0.07f), 0.15f);
            blueCloth = Mat("Blue Cloth", new Color(0.12f, 0.17f, 0.4f), 0.15f);
            carpet = Mat("Carpet", new Color(0.42f, 0.05f, 0.07f), 0.05f);
            wax = Mat("Wax", new Color(0.95f, 0.92f, 0.82f), 0.4f);
            flame = Mat("Flame", new Color(1f, 0.7f, 0.3f), 0f, 0f, new Color(1f, 0.6f, 0.2f) * 6f);
            fire = Mat("Fire", new Color(1f, 0.45f, 0.1f), 0f, 0f, new Color(1f, 0.4f, 0.08f) * 7f);
            moon = Mat("Moonlit Glass", new Color(0.4f, 0.5f, 0.8f), 0.6f, 0f, new Color(0.35f, 0.45f, 0.8f) * 1.5f);
            parchment = Mat("Parchment", new Color(0.88f, 0.82f, 0.66f), 0.1f);
            barrelWood = Mat("Barrel Wood", new Color(0.42f, 0.27f, 0.15f), 0.25f);
            wine = Mat("Wine", new Color(0.3f, 0.02f, 0.05f), 0.95f);
            forgeGlow = Mat("Forge Coals", new Color(1f, 0.35f, 0.05f), 0f, 0f, new Color(1f, 0.3f, 0.04f) * 5f);
            sack = Mat("Sackcloth", new Color(0.6f, 0.52f, 0.38f), 0.05f);
            whiteCloth = Mat("Linen", new Color(0.9f, 0.88f, 0.82f), 0.1f);
            glassGreen = Mat("Glass Green", new Color(0.15f, 0.4f, 0.2f), 0.9f);
            glassBlue = Mat("Glass Blue", new Color(0.15f, 0.25f, 0.5f), 0.9f);
            glassPurple = Mat("Glass Purple", new Color(0.35f, 0.15f, 0.45f), 0.9f);
            black = Mat("Soot", new Color(0.03f, 0.025f, 0.02f), 0f);
        }

        static Material BookMaterial()
        {
            Color[] spines =
            {
                new Color(0.45f, 0.1f, 0.08f), new Color(0.15f, 0.25f, 0.4f), new Color(0.2f, 0.35f, 0.18f),
                new Color(0.4f, 0.28f, 0.12f), new Color(0.3f, 0.12f, 0.3f), new Color(0.55f, 0.45f, 0.25f)
            };
            int i = Mathf.Min(spines.Length - 1, (int)(Rand() * spines.Length));
            return Mat("Book " + i, spines[i], 0.2f);
        }

        static Material Mat(string name, Color color, float smoothness = 0.2f, float metallic = 0f, Color? emission = null) =>
            MatAt(MaterialFolder, name, color, smoothness, metallic, emission);

        static Material CharMat(Color c)
        {
            c.a = 1f;
            return MatAt(CharacterMaterialFolder, "C_" + ColorUtility.ToHtmlStringRGB(c), c, 0.2f, 0f, null);
        }

        static Material MatAt(string folder, string name, Color color, float smoothness, float metallic, Color? emission)
        {
            string path = $"{folder}/{name}.mat";
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
