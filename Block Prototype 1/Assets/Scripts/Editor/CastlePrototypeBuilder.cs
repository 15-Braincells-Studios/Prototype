using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PreventTheMurder.EditorTools
{
    /// <summary>
    /// Builds the greybox scene Castle_Prototype from primitives: three rooms, furniture with clue spots,
    /// NPCs with waypoint routes, the player, managers and the full UI Canvas.
    /// Re-run from Tools > Prevent the Murder to regenerate (the scene file is overwritten; data assets are kept).
    /// Extension point: add rooms by extending BuildShell + a Room() call, NPCs with another BuildNPC() call
    /// and a matching CastEntry in the scenario.
    /// </summary>
    public static class CastlePrototypeBuilder
    {
        const string ScenePath = "Assets/Scenes/Castle_Prototype.unity";
        const string MaterialFolder = "Assets/Materials/Greybox";

        const float WallHeight = 4f;
        const float WallThickness = 0.3f;
        const float ArchHalfWidth = 1.5f;
        const float ArchHeight = 3f;

        static readonly Color Ink = new Color(0.17f, 0.12f, 0.08f);
        static readonly Color Paper = new Color(0.96f, 0.92f, 0.8f);
        static readonly Color Gold = new Color(1f, 0.82f, 0.45f);
        static readonly Color PanelDark = new Color(0.08f, 0.07f, 0.06f, 0.94f);
        static readonly Color ButtonColor = new Color(0.86f, 0.78f, 0.6f);

        static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
        static TMP_FontAsset font;
        static ScenarioData scenario;

        [MenuItem("Tools/First Person - First Test/Build Castle Prototype Scene")]
        static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        public static string Build()
        {
            MaterialCache.Clear();
            font = TMP_Settings.defaultFontAsset;
            if (!font) throw new InvalidOperationException("Import TMP Essential Resources first (Window > TextMeshPro > Import TMP Essential Resources).");

            scenario = ScenarioAuthoring.GetOrCreate();
            ScenarioAuthoring.EnsureFolder(MaterialFolder);
            ScenarioAuthoring.EnsureFolder("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            var world = new GameObject("World").transform;
            BuildShell(world);
            BuildGreatHall(world);
            BuildSolar(world);
            BuildKitchens(world);
            BuildNPCs(world);
            var player = BuildPlayer();
            BuildSystemsAndUI(player.controller, player.interactor);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Prevent the Murder] Built {ScenePath}");
            return ScenePath;
        }

        // =====================================================================================  World

        static void BuildLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.44f, 0.47f);
        }

        static void BuildShell(Transform world)
        {
            var shell = Group("Shell", world, Vector3.zero);
            var stone = Mat("Stone_Wall", new Color(0.56f, 0.54f, 0.5f));
            var trim = Mat("Stone_Trim", new Color(0.4f, 0.38f, 0.35f));

            Box("Floor_RoyalSolar", shell, new Vector3(-9f, -0.05f, 0f), new Vector3(8f, 0.1f, 8f), Mat("Floor_RoyalSolar", new Color(0.46f, 0.17f, 0.17f)));
            Box("Floor_GreatHall", shell, new Vector3(0f, -0.05f, 0f), new Vector3(10f, 0.1f, 8f), Mat("Floor_GreatHall", new Color(0.64f, 0.57f, 0.43f)));
            Box("Floor_Kitchens", shell, new Vector3(9f, -0.05f, 0f), new Vector3(8f, 0.1f, 8f), Mat("Floor_Kitchens", new Color(0.3f, 0.42f, 0.38f)));

            float t = WallThickness, h = WallHeight, y = h * 0.5f;
            Box("Wall_North", shell, new Vector3(0f, y, 4f + t * 0.5f), new Vector3(26f + 2f * t, h, t), stone);
            Box("Wall_South", shell, new Vector3(0f, y, -4f - t * 0.5f), new Vector3(26f + 2f * t, h, t), stone);
            Box("Wall_West", shell, new Vector3(-13f - t * 0.5f, y, 0f), new Vector3(t, h, 8f), stone);
            Box("Wall_East", shell, new Vector3(13f + t * 0.5f, y, 0f), new Vector3(t, h, 8f), stone);

            ArchWall(shell, "Archway_Solar", -5f, stone, trim);
            ArchWall(shell, "Archway_Kitchens", 5f, stone, trim);

            Room(world, "Royal Solar", new Vector3(-9f, 0f, 0f), new Vector3(8f, 4f, 8f));
            Room(world, "Great Hall", Vector3.zero, new Vector3(10f, 4f, 8f));
            Room(world, "Kitchens", new Vector3(9f, 0f, 0f), new Vector3(8f, 4f, 8f));
        }

        static void ArchWall(Transform parent, string name, float x, Material stone, Material trim)
        {
            var arch = Group(name, parent, Vector3.zero);
            float segment = 4f - ArchHalfWidth;
            float t = WallThickness;
            Box("Wall_N", arch, new Vector3(x, WallHeight * 0.5f, ArchHalfWidth + segment * 0.5f), new Vector3(t, WallHeight, segment), stone);
            Box("Wall_S", arch, new Vector3(x, WallHeight * 0.5f, -ArchHalfWidth - segment * 0.5f), new Vector3(t, WallHeight, segment), stone);
            Box("Lintel", arch, new Vector3(x, (ArchHeight + WallHeight) * 0.5f, 0f), new Vector3(t + 0.12f, WallHeight - ArchHeight, ArchHalfWidth * 2f + 0.2f), trim);
            Box("Jamb_N", arch, new Vector3(x, ArchHeight * 0.5f, ArchHalfWidth + 0.06f), new Vector3(t + 0.12f, ArchHeight, 0.12f), trim);
            Box("Jamb_S", arch, new Vector3(x, ArchHeight * 0.5f, -ArchHalfWidth - 0.06f), new Vector3(t + 0.12f, ArchHeight, 0.12f), trim);
        }

        static void Room(Transform world, string roomName, Vector3 center, Vector3 size)
        {
            var go = new GameObject($"Room_{roomName.Replace(" ", "")}");
            go.transform.SetParent(world, false);
            go.transform.position = center;
            var volume = go.AddComponent<RoomVolume>();
            SetField(volume, "roomName", roomName);
            SetField(volume, "size", size);

            var sign = WorldText("RoomSign", go.transform, new Vector3(0f, 3.1f, 0f), roomName, 4.5f, new Color(1f, 0.93f, 0.75f));
            sign.gameObject.AddComponent<Billboard>();
        }

        static void BuildGreatHall(Transform world)
        {
            var root = Group("GreatHall", world, Vector3.zero);
            var wood = Mat("Wood", new Color(0.42f, 0.28f, 0.16f));
            var stone = Mat("Stone_Pillar", new Color(0.62f, 0.6f, 0.56f));

            // Long table with the torn memo.
            var table = Group("LongTable", root, new Vector3(0f, 0f, -2f));
            Box("Top", table, new Vector3(0f, 0.78f, 0f), new Vector3(4f, 0.08f, 1.2f), wood);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Box("Leg", table, new Vector3(1.85f * sx, 0.37f, 0.5f * sz), new Vector3(0.1f, 0.74f, 0.1f), wood);
            Prim(PrimitiveType.Cylinder, "Candle", table, new Vector3(-1.3f, 0.9f, 0.1f), new Vector3(0.07f, 0.06f, 0.07f), Mat("Wax", new Color(0.95f, 0.92f, 0.82f)), false);
            Prim(PrimitiveType.Cylinder, "Plate", table, new Vector3(-0.4f, 0.825f, 0.25f), new Vector3(0.3f, 0.01f, 0.3f), Mat("Pewter", new Color(0.6f, 0.62f, 0.64f), 0.5f), false);
            Prim(PrimitiveType.Cylinder, "Plate", table, new Vector3(0.3f, 0.825f, 0.3f), new Vector3(0.3f, 0.01f, 0.3f), Mat("Pewter", Color.grey), false);

            var note = new GameObject("TornMemo");
            note.transform.SetParent(table, false);
            note.transform.localPosition = new Vector3(0.9f, 0.83f, -0.15f);
            note.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            Box("Paper", note.transform, Vector3.zero, new Vector3(0.26f, 0.01f, 0.2f), Mat("Parchment", new Color(0.95f, 0.9f, 0.72f)), false);
            var noteCollider = note.AddComponent<BoxCollider>();
            noteCollider.size = new Vector3(0.4f, 0.1f, 0.34f);
            var readable = note.AddComponent<ReadableNote>();
            SetField(readable, "spotId", ScenarioAuthoring.SpotHallNote);
            SetField(readable, "displayName", "torn memo");

            Box("Bench", root, new Vector3(0f, 0.225f, -1.05f), new Vector3(3.6f, 0.45f, 0.35f), wood);

            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Prim(PrimitiveType.Cylinder, "Pillar", root, new Vector3(3.6f * sx, 2f, 2.8f * sz), new Vector3(0.7f, 2f, 0.7f), stone);

            Box("Banner", root, new Vector3(-2.6f, 2.4f, 3.83f), new Vector3(0.9f, 1.6f, 0.04f), Mat("Banner_Red", new Color(0.6f, 0.08f, 0.1f)), false);
            Box("Banner", root, new Vector3(3.0f, 2.4f, 3.83f), new Vector3(0.9f, 1.6f, 0.04f), Mat("Banner_Red", Color.red), false);

            // Guard post with Captain Rowan (the report point).
            var post = Group("GuardPost", root, new Vector3(0f, 0f, 3.2f));
            Box("Rug", post, new Vector3(0f, 0.015f, 0f), new Vector3(2.2f, 0.03f, 1.3f), Mat("GuardPost_Rug", new Color(0.45f, 0.1f, 0.12f)), false);
            Box("RackPost", post, new Vector3(-1.05f, 0.8f, 0.45f), new Vector3(0.08f, 1.6f, 0.08f), wood);
            Box("RackPost", post, new Vector3(-0.55f, 0.8f, 0.45f), new Vector3(0.08f, 1.6f, 0.08f), wood);
            Box("RackBar", post, new Vector3(-0.8f, 1.35f, 0.45f), new Vector3(0.58f, 0.06f, 0.06f), wood);
            WorldText("GuardPostSign", root, new Vector3(0f, 2.75f, 3.8f), "Guard Post", 3f, Gold);
            BuildCaptain(root, new Vector3(0f, 0f, 3.15f));

            BuildHourglass(root, new Vector3(1.9f, 0f, 3.4f));
        }

        static void BuildHourglass(Transform parent, Vector3 position)
        {
            var wood = Mat("Wood", Color.black);
            var glass = Mat("Hourglass_Sand", new Color(0.9f, 0.8f, 0.55f), 0.6f);
            var g = Group("VesperHourglass", parent, position);
            Box("Pedestal", g, new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 1f, 0.6f), Mat("Stone_Trim", Color.grey));
            Prim(PrimitiveType.Cylinder, "BaseDisc", g, new Vector3(0f, 1.03f, 0f), new Vector3(0.5f, 0.03f, 0.5f), wood, false);
            Prim(PrimitiveType.Sphere, "LowerBulb", g, new Vector3(0f, 1.23f, 0f), new Vector3(0.34f, 0.36f, 0.34f), glass, false);
            Prim(PrimitiveType.Cylinder, "Neck", g, new Vector3(0f, 1.42f, 0f), new Vector3(0.07f, 0.05f, 0.07f), glass, false);
            Prim(PrimitiveType.Sphere, "UpperBulb", g, new Vector3(0f, 1.61f, 0f), new Vector3(0.34f, 0.36f, 0.34f), glass, false);
            Prim(PrimitiveType.Cylinder, "TopDisc", g, new Vector3(0f, 1.81f, 0f), new Vector3(0.5f, 0.03f, 0.5f), wood, false);
            Box("Post", g, new Vector3(-0.21f, 1.42f, 0f), new Vector3(0.04f, 0.78f, 0.04f), wood, false);
            Box("Post", g, new Vector3(0.21f, 1.42f, 0f), new Vector3(0.04f, 0.78f, 0.04f), wood, false);

            var label = WorldText("ClockLabel", g, new Vector3(0f, 2.42f, 0f), scenario.clockLabel, 1.5f, Gold);
            label.gameObject.AddComponent<Billboard>();
            var time = WorldText("ClockTime", g, new Vector3(0f, 2.15f, 0f), ClockFormat.Format(scenario.timeLimitSeconds), 3.4f, Color.white);
            time.gameObject.AddComponent<Billboard>();
            var display = g.gameObject.AddComponent<WorldClockDisplay>();
            SetField(display, "timeText", time);
        }

        static void BuildSolar(Transform world)
        {
            var root = Group("RoyalSolar", world, new Vector3(-9f, 0f, 0f));
            var wood = Mat("Wood", Color.black);
            var drawerWood = Mat("Wood_Light", new Color(0.62f, 0.45f, 0.28f));

            Box("Rug", root, new Vector3(0.3f, 0.01f, 0.2f), new Vector3(3.2f, 0.02f, 2.2f), Mat("Rug_Blue", new Color(0.16f, 0.22f, 0.45f)), false);

            // Writing desk against the north wall, two drawers (left: decree, right: empty).
            var desk = Group("WritingDesk", root, new Vector3(-1f, 0f, 3.55f));
            Box("Top", desk, new Vector3(0f, 0.78f, 0f), new Vector3(1.8f, 0.06f, 0.8f), wood);
            foreach (var (side, spot) in new[] { (-1f, ScenarioAuthoring.SpotDeskDrawerLeft), (1f, ScenarioAuthoring.SpotDeskDrawerRight) })
            {
                var pedestal = Group(side < 0 ? "Pedestal_Left" : "Pedestal_Right", desk, new Vector3(0.6f * side, 0f, 0f));
                Box("Body", pedestal, new Vector3(0f, 0.36f, 0f), new Vector3(0.55f, 0.72f, 0.75f), wood);
                BuildDrawer(pedestal, "Drawer", new Vector3(0f, 0.52f, -0.095f), new Vector3(0.47f, 0.16f, 0.6f), 0.45f, spot, "desk drawer", drawerWood);
            }
            Box("ChairSeat", desk, new Vector3(0f, 0.45f, -0.75f), new Vector3(0.5f, 0.06f, 0.5f), wood);
            Box("ChairBack", desk, new Vector3(0f, 0.75f, -0.98f), new Vector3(0.5f, 0.6f, 0.05f), wood);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -0.53f, -0.97f })
                    Box("ChairLeg", desk, new Vector3(0.22f * sx, 0.21f, sz), new Vector3(0.05f, 0.42f, 0.05f), wood, false);

            // Chest against the west wall, lid opens upward.
            var chest = Group("Chest", root, new Vector3(-3.6f, 0f, 0.6f), -90f);
            var chestWood = Mat("Wood_Chest", new Color(0.35f, 0.22f, 0.12f));
            HollowBox(chest, new Vector3(1f, 0.5f, 0.6f), chestWood, openFront: false, openTop: true);
            var lidHinge = Group("LidHinge", chest, new Vector3(0f, 0.5f, 0.3f));
            Box("Lid", lidHinge, new Vector3(0f, 0.03f, -0.32f), new Vector3(1.04f, 0.06f, 0.64f), chestWood);
            Box("Band", lidHinge, new Vector3(0f, 0.035f, -0.32f), new Vector3(1.06f, 0.07f, 0.06f), Mat("Iron", new Color(0.25f, 0.25f, 0.27f), 0.4f), false);
            var vial = Prim(PrimitiveType.Cylinder, "ClueItem_Vial", chest, new Vector3(0.15f, 0.13f, 0f), new Vector3(0.07f, 0.08f, 0.07f), ClueItemMaterial());
            MakeContainer(lidHinge.gameObject, ScenarioAuthoring.SpotChest, "chest", lidHinge,
                OpenableContainer.MotionType.Rotate, Vector3.zero, new Vector3(95f, 0f, 0f), vial.AddComponent<ClueItem>());

            // Wine cabinet against the south wall, door swings open; the King's goblet inside, flagon on top.
            var cabinet = Group("WineCabinet", root, new Vector3(0.5f, 0f, -3.62f), 180f);
            var cabinetWood = Mat("Wood_Dark", new Color(0.28f, 0.17f, 0.1f));
            HollowBox(cabinet, new Vector3(0.9f, 1.6f, 0.5f), cabinetWood, openFront: true, openTop: false);
            Box("Shelf", cabinet, new Vector3(0f, 0.8f, 0f), new Vector3(0.84f, 0.04f, 0.44f), cabinetWood);
            var doorHinge = Group("DoorHinge", cabinet, new Vector3(-0.45f, 0.8f, -0.25f));
            Box("Door", doorHinge, new Vector3(0.45f, 0f, -0.02f), new Vector3(0.9f, 1.58f, 0.04f), Mat("Wood_Light", Color.black));
            Box("Knob", doorHinge, new Vector3(0.8f, 0f, -0.06f), new Vector3(0.04f, 0.04f, 0.04f), Mat("Brass", new Color(0.8f, 0.62f, 0.25f), 0.6f), false);
            var goblet = new GameObject("ClueItem_Goblet");
            goblet.transform.SetParent(cabinet, false);
            goblet.transform.localPosition = new Vector3(0.12f, 0.82f, 0.02f);
            var silver = Mat("Silver", new Color(0.8f, 0.82f, 0.85f), 0.8f);
            Prim(PrimitiveType.Cylinder, "Base", goblet.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.1f, 0.01f, 0.1f), silver, false);
            Prim(PrimitiveType.Cylinder, "Stem", goblet.transform, new Vector3(0f, 0.07f, 0f), new Vector3(0.03f, 0.06f, 0.03f), silver, false);
            Prim(PrimitiveType.Cylinder, "Cup", goblet.transform, new Vector3(0f, 0.17f, 0f), new Vector3(0.12f, 0.05f, 0.12f), silver, false);
            var gobletCollider = goblet.AddComponent<BoxCollider>();
            gobletCollider.center = new Vector3(0f, 0.12f, 0f);
            gobletCollider.size = new Vector3(0.18f, 0.26f, 0.18f);
            MakeContainer(doorHinge.gameObject, ScenarioAuthoring.SpotWineCabinet, "wine cabinet", doorHinge,
                OpenableContainer.MotionType.Rotate, Vector3.zero, new Vector3(0f, 100f, 0f), goblet.AddComponent<ClueItem>());
            Prim(PrimitiveType.Cylinder, "Flagon", cabinet, new Vector3(-0.2f, 1.75f, 0f), new Vector3(0.16f, 0.15f, 0.16f), Mat("Pewter", Color.grey), false);

            // Bookshelf for orientation.
            var shelf = Group("Bookshelf", root, new Vector3(2.5f, 0f, 3.75f));
            Box("Frame", shelf, new Vector3(0f, 1.1f, 0f), new Vector3(1.6f, 2.2f, 0.4f), wood);
            var bookColors = new[] { new Color(0.5f, 0.1f, 0.1f), new Color(0.1f, 0.3f, 0.15f), new Color(0.15f, 0.2f, 0.45f) };
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 6; i++)
                    Box("Book", shelf, new Vector3(-0.6f + i * 0.24f, 0.55f + row * 0.6f, -0.2f), new Vector3(0.16f, 0.36f, 0.04f), Mat($"Book_{(i + row) % 3}", bookColors[(i + row) % 3]), false);
        }

        static void BuildKitchens(Transform world)
        {
            var root = Group("Kitchens", world, new Vector3(9f, 0f, 0f));
            var wood = Mat("Wood", Color.black);
            var drawerWood = Mat("Wood_Light", Color.black);
            var stoneDark = Mat("Stone_Dark", new Color(0.28f, 0.27f, 0.26f));

            // Cupboard against the north wall: top drawer (nightshade), bottom drawer (debt note).
            var cupboard = Group("Cupboard", root, new Vector3(0f, 0f, 3.65f));
            Box("Body", cupboard, new Vector3(0f, 0.6f, 0f), new Vector3(1.4f, 1.2f, 0.6f), wood);
            BuildDrawer(cupboard, "Drawer_Top", new Vector3(0f, 0.95f, -0.095f), new Vector3(1.1f, 0.22f, 0.45f), 0.36f, ScenarioAuthoring.SpotCupboardTop, "cupboard drawer", drawerWood);
            BuildDrawer(cupboard, "Drawer_Bottom", new Vector3(0f, 0.6f, -0.095f), new Vector3(1.1f, 0.22f, 0.45f), 0.36f, ScenarioAuthoring.SpotCupboardBottom, "cupboard drawer", drawerWood);
            var jar = Mat("Clay", new Color(0.7f, 0.45f, 0.3f));
            for (int i = 0; i < 4; i++)
                Prim(PrimitiveType.Cylinder, "Jar", cupboard, new Vector3(-0.5f + i * 0.33f, 1.32f, 0f), new Vector3(0.18f, 0.12f, 0.18f), jar, false);

            // Work counter along the south wall.
            var counter = Group("WorkCounter", root, new Vector3(0f, 0f, -3.6f));
            Box("Body", counter, new Vector3(0f, 0.45f, 0f), new Vector3(3f, 0.9f, 0.7f), Mat("Stone_Counter", new Color(0.7f, 0.68f, 0.62f)));
            Box("Loaf", counter, new Vector3(-0.9f, 0.96f, 0f), new Vector3(0.3f, 0.12f, 0.18f), Mat("Bread", new Color(0.8f, 0.58f, 0.3f)), false);
            Box("Loaf", counter, new Vector3(-0.5f, 0.96f, 0.05f), new Vector3(0.3f, 0.12f, 0.18f), Mat("Bread", Color.black), false);
            Box("Board", counter, new Vector3(0.6f, 0.915f, 0f), new Vector3(0.6f, 0.03f, 0.4f), Mat("Wood_Light", Color.black), false);
            Prim(PrimitiveType.Cylinder, "Pot", counter, new Vector3(1.2f, 1.05f, 0f), new Vector3(0.35f, 0.15f, 0.35f), Mat("Iron", Color.black), false);

            // Hearth against the east wall.
            var hearth = Group("Hearth", root, new Vector3(3.5f, 0f, 0f));
            Box("Back", hearth, new Vector3(0.2f, 0.9f, 0f), new Vector3(0.6f, 1.8f, 2.2f), stoneDark);
            Box("Side", hearth, new Vector3(-0.3f, 0.45f, 0.925f), new Vector3(0.4f, 0.9f, 0.35f), stoneDark);
            Box("Side", hearth, new Vector3(-0.3f, 0.45f, -0.925f), new Vector3(0.4f, 0.9f, 0.35f), stoneDark);
            Box("Mantle", hearth, new Vector3(-0.3f, 1.25f, 0f), new Vector3(0.45f, 0.7f, 2.2f), stoneDark);
            Box("Fire", hearth, new Vector3(-0.2f, 0.15f, 0f), new Vector3(0.35f, 0.28f, 0.9f), Mat("Fire", new Color(1f, 0.45f, 0.1f), 0f, new Color(2.5f, 0.9f, 0.2f)), false);
            var fireLight = new GameObject("FireLight").AddComponent<Light>();
            fireLight.transform.SetParent(hearth, false);
            fireLight.transform.localPosition = new Vector3(-0.8f, 0.6f, 0f);
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.6f, 0.3f);
            fireLight.range = 5f;
            fireLight.intensity = 2f;

            var barrel = Mat("Wood_Barrel", new Color(0.45f, 0.3f, 0.17f));
            Prim(PrimitiveType.Cylinder, "Barrel", root, new Vector3(3.3f, 0.45f, 3.3f), new Vector3(0.6f, 0.45f, 0.6f), barrel);
            Prim(PrimitiveType.Cylinder, "Barrel", root, new Vector3(-3.3f, 0.45f, -3.3f), new Vector3(0.6f, 0.45f, 0.6f), barrel);
        }

        /// <summary>Drawer that slides out along local -z of its parent; its clue item rides on top.</summary>
        static OpenableContainer BuildDrawer(Transform parent, string name, Vector3 closedCenter, Vector3 size, float openDistance,
            string spotId, string label, Material drawerMaterial)
        {
            var drawer = Group(name, parent, closedCenter);
            Box("Visual", drawer, Vector3.zero, size, drawerMaterial, false);
            Box("Handle", drawer, new Vector3(0f, 0f, -size.z * 0.5f - 0.02f), new Vector3(0.14f, 0.03f, 0.03f), Mat("Brass", Color.black), false);
            var collider = drawer.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0f, -0.015f);
            collider.size = size + new Vector3(0f, 0f, 0.03f);

            const float itemSize = 0.09f;
            var item = Box("ClueItem", drawer, new Vector3(0f, size.y * 0.5f + itemSize * 0.5f, -size.z * 0.22f), Vector3.one * itemSize, ClueItemMaterial());

            return MakeContainer(drawer.gameObject, spotId, label, drawer, OpenableContainer.MotionType.Slide,
                new Vector3(0f, 0f, -openDistance), Vector3.zero, item.AddComponent<ClueItem>());
        }

        static OpenableContainer MakeContainer(GameObject host, string spotId, string label, Transform movingPart,
            OpenableContainer.MotionType motion, Vector3 openOffset, Vector3 openEuler, ClueItem item)
        {
            var container = host.AddComponent<OpenableContainer>();
            SetField(container, "spotId", spotId);
            SetField(container, "displayName", label);
            SetField(container, "movingPart", movingPart);
            SetField(container, "motion", (int)motion);
            SetField(container, "openOffset", openOffset);
            SetField(container, "openEuler", openEuler);
            SetField(container, "contentItem", item);
            return container;
        }

        /// <summary>Box made of panels, standing on the floor, front = local -z.</summary>
        static void HollowBox(Transform parent, Vector3 size, Material material, bool openFront, bool openTop)
        {
            const float t = 0.04f;
            Box("Bottom", parent, new Vector3(0f, t * 0.5f, 0f), new Vector3(size.x, t, size.z), material);
            Box("Back", parent, new Vector3(0f, size.y * 0.5f, size.z * 0.5f - t * 0.5f), new Vector3(size.x, size.y, t), material);
            Box("Left", parent, new Vector3(-size.x * 0.5f + t * 0.5f, size.y * 0.5f, 0f), new Vector3(t, size.y, size.z), material);
            Box("Right", parent, new Vector3(size.x * 0.5f - t * 0.5f, size.y * 0.5f, 0f), new Vector3(t, size.y, size.z), material);
            if (!openFront) Box("Front", parent, new Vector3(0f, size.y * 0.5f, -size.z * 0.5f + t * 0.5f), new Vector3(size.x, size.y, t), material);
            if (!openTop) Box("Top", parent, new Vector3(0f, size.y - t * 0.5f, 0f), new Vector3(size.x, t, size.z), material);
        }

        static Material ClueItemMaterial() => Mat("ClueItem", new Color(1f, 0.82f, 0.3f), 0.3f, new Color(0.35f, 0.25f, 0.05f));

        // =====================================================================================  Characters

        static void BuildNPCs(Transform world)
        {
            var npcs = Group("NPCs", world, Vector3.zero);
            var routes = Group("Waypoints", world, Vector3.zero);

            // Routes are in world space. Straight lines between points stay clear of furniture and walls;
            // pass-through points (false) sit in the archways.
            BuildNPC(npcs, routes, "king_aldric", false, (-8f, 1.6f, true), (-10.8f, 0.4f, true), (-9.5f, -2f, true), (-7f, -0.8f, true));
            BuildNPC(npcs, routes, "queen_isolde", false, (-2.5f, 0f, true), (-1f, 1.9f, true), (1.5f, 1.6f, true), (2.8f, -0.3f, true));
            BuildNPC(npcs, routes, "brother_pruitt", true, (-2.6f, 1.2f, true), (-4.2f, 0f, false), (-6f, 0f, false), (-7.8f, -1.6f, true), (-10.4f, -0.6f, true));
            // In the Kitchens, Colin keeps to the north half (by the cupboard) and Dalton mostly to the south half.
            BuildNPC(npcs, routes, "duke_colin", true, (2.6f, 1f, true), (4.2f, 0f, false), (6f, 0f, false), (7.4f, 2f, true), (11.2f, 1.8f, true));
            BuildNPC(npcs, routes, "mistress_dalton", false, (7f, -2f, true), (9.8f, 0.6f, true), (11f, -1.8f, true), (7.8f, -0.4f, true));
        }

        static CharacterData Character(string id)
        {
            var entry = scenario.FindCast(id);
            if (entry == null || !entry.character) throw new InvalidOperationException($"Scenario has no character '{id}'.");
            return entry.character;
        }

        static GameObject CharacterBody(Transform parent, CharacterData character, Vector3 position, Quaternion rotation, out Renderer bodyRenderer)
        {
            var root = new GameObject(character.displayName);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);

            var body = Prim(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.8f, 0.9f, 0.8f), Mat($"NPC_{character.id}", character.color));
            bodyRenderer = body.GetComponent<Renderer>();
            Box("Face", root.transform, new Vector3(0f, 1.5f, 0.33f), new Vector3(0.32f, 0.1f, 0.12f), Mat("NPC_Face", new Color(0.12f, 0.1f, 0.1f)), false);
            return root;
        }

        static void BuildNPC(Transform parent, Transform routeParent, string id, bool pingPong, params (float x, float z, bool pause)[] route)
        {
            var character = Character(id);
            Vector3 start = new Vector3(route[0].x, 0f, route[0].z);
            Vector3 next = new Vector3(route[1].x, 0f, route[1].z);
            var root = CharacterBody(parent, character, start, Quaternion.LookRotation(next - start), out var bodyRenderer);

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var nameLabel = WorldText("NameLabel", root.transform, new Vector3(0f, 2.15f, 0f), character.displayName, 2.6f, Color.white);
            nameLabel.gameObject.AddComponent<Billboard>();
            var bubble = WorldText("SpeechBubble", root.transform, new Vector3(0f, 2.5f, 0f), "...", 5f, new Color(1f, 0.9f, 0.4f));
            bubble.gameObject.AddComponent<Billboard>();

            var routeRoot = Group(character.displayName, routeParent, Vector3.zero);
            var waypoints = new List<Object>();
            for (int i = 0; i < route.Length; i++)
            {
                var wp = new GameObject($"WP_{i}").AddComponent<Waypoint>();
                wp.transform.SetParent(routeRoot, false);
                wp.transform.position = new Vector3(route[i].x, 0f, route[i].z);
                wp.pauseHere = route[i].pause;
                waypoints.Add(wp);
            }

            var mover = root.AddComponent<NPCWaypointMover>();
            SetList(mover, "waypoints", waypoints);
            SetField(mover, "pingPong", pingPong);

            var npc = root.AddComponent<NPCController>();
            SetField(npc, "characterId", id);
            SetField(npc, "mover", mover);
            SetField(npc, "nameLabel", nameLabel);
            SetField(npc, "bodyRenderer", bodyRenderer);

            var speech = root.AddComponent<SpeechBubble>();
            SetField(speech, "bubble", bubble.gameObject);
            SetField(speech, "npc", npc);
        }

        static void BuildCaptain(Transform parent, Vector3 position)
        {
            var character = Character("captain_rowan");
            var root = CharacterBody(parent, character, position, Quaternion.Euler(0f, 180f, 0f), out _);
            var steel = Mat("Steel", new Color(0.62f, 0.64f, 0.68f), 0.6f);
            Prim(PrimitiveType.Sphere, "Helmet", root.transform, new Vector3(0f, 1.7f, 0f), new Vector3(0.62f, 0.38f, 0.62f), steel, false);
            Prim(PrimitiveType.Cylinder, "HalberdShaft", root.transform, new Vector3(0.5f, 1.15f, 0.1f), new Vector3(0.05f, 1.15f, 0.05f), Mat("Wood", Color.black), false);
            Box("HalberdBlade", root.transform, new Vector3(0.58f, 2.15f, 0.1f), new Vector3(0.18f, 0.3f, 0.03f), steel, false);

            // Sits higher than other NPC labels so the halberd blade doesn't cover it.
            var nameLabel = WorldText("NameLabel", root.transform, new Vector3(0f, 2.5f, 0f), character.displayName, 2.6f, Color.white);
            nameLabel.gameObject.AddComponent<Billboard>();
            var bubble = WorldText("SpeechBubble", root.transform, new Vector3(0f, 2.85f, 0f), "...", 5f, new Color(1f, 0.9f, 0.4f));
            bubble.gameObject.AddComponent<Billboard>();

            var report = root.AddComponent<ReportPoint>();
            SetField(report, "reporterId", character.id);
            SetField(report, "nameLabel", nameLabel);

            var speech = root.AddComponent<SpeechBubble>();
            SetField(speech, "bubble", bubble.gameObject);
        }

        static (FirstPersonController controller, PlayerInteractor interactor) BuildPlayer()
        {
            var player = new GameObject("Player") { tag = "Player", layer = LayerMask.NameToLayer("Ignore Raycast") };
            player.transform.position = new Vector3(0f, 0.05f, -3.35f);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;

            var pivot = Group("CameraPivot", player.transform, new Vector3(0f, 1.65f, 0f));
            var camGo = new GameObject("PlayerCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();

            var controller = player.AddComponent<FirstPersonController>();
            SetField(controller, "cameraPivot", pivot);
            var interactor = player.AddComponent<PlayerInteractor>();
            SetField(interactor, "viewCamera", cam);
            return (controller, interactor);
        }

        // =====================================================================================  Systems + UI

        static void BuildSystemsAndUI(FirstPersonController player, PlayerInteractor interactor)
        {
            var systems = new GameObject("Systems");
            var gameManager = systems.AddComponent<GameManager>();
            SetField(gameManager, "scenario", scenario);
            systems.AddComponent<ClueManager>();
            var dialogueManager = systems.AddComponent<DialogueManager>();

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("UI", typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var c = canvasGo.transform;

            // ---- HUD
            var hud = Stretch(Rect("HUD", c));

            var crosshair = Panel("Crosshair", hud, new Color(1f, 1f, 1f, 0.9f), Sprite("UI/Skin/Knob.psd"), sliced: false);
            Place(crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));

            var prompt = Panel("Prompt", hud, new Color(0f, 0f, 0f, 0.6f));
            Place(prompt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(10f, 10f));
            FitToContent(prompt.gameObject, new RectOffset(18, 18, 8, 8));
            var promptText = Label("Text", prompt.transform, "[E] Interact", 28f, Color.white, TextAlignmentOptions.Center);
            promptText.textWrappingMode = TextWrappingModes.NoWrap;

            var clock = Panel("Clock", hud, new Color(0.05f, 0.04f, 0.03f, 0.65f));
            Place(clock.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(340f, 112f));
            var clockLabel = Label("Label", clock.transform, scenario.clockLabel, 22f, Gold, TextAlignmentOptions.Top);
            Stretch(clockLabel.rectTransform, 8f, 8f, 10f, 70f);
            var clockText = Label("Time", clock.transform, "15:00", 58f, Color.white, TextAlignmentOptions.Center);
            Stretch(clockText.rectTransform, 8f, 8f, 34f, 2f);
            clockText.fontStyle = FontStyles.Bold;

            var toastBg = Panel("Toast", hud, new Color(0.16f, 0.12f, 0.05f, 0.88f));
            Place(toastBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(10f, 10f));
            FitToContent(toastBg.gameObject, new RectOffset(22, 22, 10, 10));
            var toastGroup = toastBg.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;
            var toastText = Label("Text", toastBg.transform, "New clue", 27f, Gold, TextAlignmentOptions.Center);
            toastText.textWrappingMode = TextWrappingModes.NoWrap;
            var toast = toastBg.gameObject.AddComponent<ToastUI>();
            SetField(toast, "group", toastGroup);
            SetField(toast, "messageText", toastText);

            var icon = Panel("NotepadIcon", hud, Paper);
            Place(icon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -24f), new Vector2(64f, 80f));
            for (int i = 0; i < 3; i++)
            {
                var line = Panel("Line", icon.transform, new Color(Ink.r, Ink.g, Ink.b, 0.45f), null);
                Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f - i * 13f), new Vector2(40f, 3f));
            }
            var iconKey = Label("Key", icon.transform, "J", 22f, Ink, TextAlignmentOptions.Bottom);
            Stretch(iconKey.rectTransform, 0f, 0f, 0f, 4f);
            iconKey.fontStyle = FontStyles.Bold;
            var dot = Panel("NewEntryDot", icon.transform, new Color(0.9f, 0.15f, 0.12f), Sprite("UI/Skin/Knob.psd"), sliced: false);
            Place(dot.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-2f, -2f), new Vector2(24f, 24f));

            // ---- Dialogue
            var dialogueHolder = Stretch(Rect("DialoguePanel", c));
            var dialogueRoot = Panel("Root", dialogueHolder, PanelDark);
            Place(dialogueRoot.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1320f, 330f));
            var speaker = Label("Speaker", dialogueRoot.transform, "Speaker", 32f, Gold, TextAlignmentOptions.TopLeft);
            Place(speaker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -22f), new Vector2(760f, 44f));
            speaker.fontStyle = FontStyles.Bold;
            var body = Label("Body", dialogueRoot.transform, "", 30f, Color.white, TextAlignmentOptions.TopLeft);
            Place(body.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -76f), new Vector2(780f, 170f));
            var continueButton = MakeButton("ContinueButton", dialogueRoot.transform, "[E] Continue", new Vector2(240f, 56f));
            Place((RectTransform)continueButton.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 24f), new Vector2(240f, 56f));
            var topics = Rect("Topics", dialogueRoot.transform);
            Place(topics, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -22f), new Vector2(450f, 290f));
            var topicLayout = topics.gameObject.AddComponent<VerticalLayoutGroup>();
            topicLayout.spacing = 8f;
            topicLayout.childControlWidth = true;
            topicLayout.childControlHeight = false;
            topicLayout.childForceExpandWidth = true;
            topicLayout.childForceExpandHeight = false;
            var topicTemplate = MakeButton("TopicButtonTemplate", topics, "Topic", new Vector2(450f, 48f), 24f);
            var topicLabel = topicTemplate.GetComponentInChildren<TextMeshProUGUI>();
            topicLabel.alignment = TextAlignmentOptions.MidlineLeft;
            topicLabel.margin = new Vector4(16f, 0f, 8f, 0f);
            var dialogueUI = dialogueHolder.gameObject.AddComponent<DialoguePanelUI>();
            SetField(dialogueUI, "root", dialogueRoot.gameObject);
            SetField(dialogueUI, "speakerText", speaker);
            SetField(dialogueUI, "bodyText", body);
            SetField(dialogueUI, "continueButton", continueButton);
            SetField(dialogueUI, "topicContainer", topics);
            SetField(dialogueUI, "topicButtonTemplate", topicTemplate);
            SetField(dialogueManager, "panel", dialogueUI);

            // ---- Notepad
            var notepadHolder = Stretch(Rect("Notepad", c));
            var notepadRoot = Panel("Root", notepadHolder, Paper);
            Place(notepadRoot.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(580f, 820f));
            var notepadTitle = Label("Title", notepadRoot.transform, "Notepad", 40f, Ink, TextAlignmentOptions.TopLeft);
            Place(notepadTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(320f, 52f));
            notepadTitle.fontStyle = FontStyles.Bold;
            var notepadHint = Label("Hint", notepadRoot.transform, "[J] Close", 22f, new Color(Ink.r, Ink.g, Ink.b, 0.6f), TextAlignmentOptions.TopRight);
            Place(notepadHint.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -30f), new Vector2(200f, 30f));
            var rule = Panel("Rule", notepadRoot.transform, new Color(0.7f, 0.2f, 0.2f, 0.6f), null);
            Place(rule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(530f, 2f));

            var scrollGo = DefaultControls.CreateScrollView(UguiResources());
            scrollGo.name = "Entries";
            scrollGo.transform.SetParent(notepadRoot.transform, false);
            Stretch((RectTransform)scrollGo.transform, 20f, 16f, 86f, 20f);
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            Object.DestroyImmediate(scroll.horizontalScrollbar.gameObject);
            scroll.horizontalScrollbar = null;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            var content = scroll.content;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10f;
            contentLayout.padding = new RectOffset(4, 8, 4, 4);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var emptyHint = Label("EmptyHint", content, "Nothing noted yet.\nSearch the castle and question its people.", 24f, new Color(Ink.r, Ink.g, Ink.b, 0.55f), TextAlignmentOptions.TopLeft);
            emptyHint.fontStyle = FontStyles.Italic;

            var entry = Panel("EntryTemplate", content, new Color(0.9f, 0.85f, 0.7f));
            var entryLayout = entry.gameObject.AddComponent<VerticalLayoutGroup>();
            entryLayout.padding = new RectOffset(14, 14, 10, 10);
            entryLayout.spacing = 4f;
            entryLayout.childControlWidth = true;
            entryLayout.childControlHeight = true;
            entryLayout.childForceExpandWidth = true;
            entryLayout.childForceExpandHeight = false;
            var entrySummary = Label("Summary", entry.transform, "Summary", 26f, Ink, TextAlignmentOptions.TopLeft);
            var entryTag = Label("Tag", entry.transform, "[Motive]", 19f, new Color(0.45f, 0.3f, 0.18f), TextAlignmentOptions.TopLeft);
            var entryUI = entry.gameObject.AddComponent<NotepadEntryUI>();
            SetField(entryUI, "background", entry);
            SetField(entryUI, "summaryText", entrySummary);
            SetField(entryUI, "tagText", entryTag);

            var notepadUI = notepadHolder.gameObject.AddComponent<NotepadUI>();
            SetField(notepadUI, "root", notepadRoot.gameObject);
            SetField(notepadUI, "scroll", scroll);
            SetField(notepadUI, "entryContainer", content);
            SetField(notepadUI, "entryTemplate", entryUI);
            SetField(notepadUI, "emptyHint", emptyHint.gameObject);
            SetField(notepadUI, "newEntryDot", dot.gameObject);

            // ---- Clue card (also used for the intro)
            var cardHolder = Stretch(Rect("ClueCard", c));
            var cardRoot = Panel("Root", cardHolder, new Color(0f, 0f, 0f, 0.45f), null);
            Stretch(cardRoot.rectTransform);
            var card = Panel("Card", cardRoot.transform, Paper);
            Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 520f));
            var cardTitle = Label("Title", card.transform, "Title", 36f, Ink, TextAlignmentOptions.Top);
            Place(cardTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(760f, 52f));
            cardTitle.fontStyle = FontStyles.Bold;
            var cardBody = Label("Body", card.transform, "Body", 27f, Ink, TextAlignmentOptions.TopLeft);
            Place(cardBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(760f, 320f));
            cardBody.enableAutoSizing = true;
            cardBody.fontSizeMin = 18f;
            cardBody.fontSizeMax = 27f;
            var cardButton = MakeButton("ContinueButton", card.transform, "[E] Continue", new Vector2(260f, 58f));
            Place((RectTransform)cardButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(260f, 58f));
            var cardUI = cardHolder.gameObject.AddComponent<ClueCardUI>();
            SetField(cardUI, "root", cardRoot.gameObject);
            SetField(cardUI, "titleText", cardTitle);
            SetField(cardUI, "bodyText", cardBody);
            SetField(cardUI, "continueButton", cardButton);

            // ---- Report
            var reportHolder = Stretch(Rect("ReportPanel", c));
            var reportRoot = Panel("Root", reportHolder, new Color(0f, 0f, 0f, 0.5f), null);
            Stretch(reportRoot.rectTransform);
            var reportCard = Panel("Card", reportRoot.transform, new Color(0.13f, 0.12f, 0.14f, 0.97f));
            Place(reportCard.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 660f));
            var reportTitle = Label("Title", reportCard.transform, "Report to the Guard", 38f, Gold, TextAlignmentOptions.Top);
            Place(reportTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(800f, 52f));
            reportTitle.fontStyle = FontStyles.Bold;
            var reportHeader = Label("Header", reportCard.transform, "", 26f, new Color(0.92f, 0.9f, 0.85f), TextAlignmentOptions.Top);
            Place(reportHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(800f, 80f));
            reportHeader.fontStyle = FontStyles.Italic;
            var dropdowns = new TMP_Dropdown[3];
            string[] rowLabels = { "Victim", "Culprit", "Method" };
            for (int i = 0; i < 3; i++)
            {
                float y = -200f - i * 84f;
                var rowLabel = Label($"{rowLabels[i]}Label", reportCard.transform, rowLabels[i], 30f, Color.white, TextAlignmentOptions.MidlineLeft);
                Place(rowLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-260f, y), new Vector2(240f, 60f));
                dropdowns[i] = MakeDropdown($"{rowLabels[i]}Dropdown", reportCard.transform);
                Place((RectTransform)dropdowns[i].transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(110f, y), new Vector2(480f, 60f));
            }
            var reportMessage = Label("Message", reportCard.transform, "", 25f, new Color(1f, 0.88f, 0.6f), TextAlignmentOptions.Top);
            Place(reportMessage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -452f), new Vector2(800f, 100f));
            var reportButton = MakeButton("ReportButton", reportCard.transform, "Report", new Vector2(240f, 62f), 28f);
            Place((RectTransform)reportButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-140f, 30f), new Vector2(240f, 62f));
            var cancelButton = MakeButton("CancelButton", reportCard.transform, "Cancel", new Vector2(240f, 62f), 28f);
            Place((RectTransform)cancelButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(140f, 30f), new Vector2(240f, 62f));
            var reportUI = reportHolder.gameObject.AddComponent<ReportPanelUI>();
            SetField(reportUI, "root", reportRoot.gameObject);
            SetField(reportUI, "headerText", reportHeader);
            SetField(reportUI, "victimDropdown", dropdowns[0]);
            SetField(reportUI, "culpritDropdown", dropdowns[1]);
            SetField(reportUI, "methodDropdown", dropdowns[2]);
            SetField(reportUI, "messageText", reportMessage);
            SetField(reportUI, "reportButton", reportButton);
            SetField(reportUI, "cancelButton", cancelButton);

            // ---- Pause
            var pauseRoot = Panel("PausePanel", c, new Color(0f, 0f, 0f, 0.72f), null);
            Stretch(pauseRoot.rectTransform);
            var pauseTitle = Label("Title", pauseRoot.transform, "Paused", 64f, Gold, TextAlignmentOptions.Center);
            Place(pauseTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(800f, 90f));
            var pauseHint = Label("Hint", pauseRoot.transform, "The clock is stopped. [Esc] Resume", 26f, Color.white, TextAlignmentOptions.Center);
            Place(pauseHint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(800f, 40f));
            var resumeButton = MakeButton("ResumeButton", pauseRoot.transform, "Resume", new Vector2(300f, 66f), 28f);
            Place((RectTransform)resumeButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(300f, 66f));
            var pauseRestart = MakeButton("RestartButton", pauseRoot.transform, "Restart", new Vector2(300f, 66f), 28f);
            Place((RectTransform)pauseRestart.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -96f), new Vector2(300f, 66f));

            // ---- Win / lose
            var winPanel = BuildEndPanel("WinPanel", c, new Color(0.55f, 0.9f, 0.55f));
            var losePanel = BuildEndPanel("LosePanel", c, new Color(1f, 0.4f, 0.35f));

            var ui = canvasGo.AddComponent<UIManager>();
            SetField(ui, "player", player);
            SetField(ui, "interactor", interactor);
            SetField(ui, "promptRoot", prompt.gameObject);
            SetField(ui, "promptText", promptText);
            SetField(ui, "clockLabel", clockLabel);
            SetField(ui, "clockText", clockText);
            SetField(ui, "toast", toast);
            SetField(ui, "dialoguePanel", dialogueUI);
            SetField(ui, "notepad", notepadUI);
            SetField(ui, "clueCard", cardUI);
            SetField(ui, "reportPanel", reportUI);
            SetField(ui, "winPanel", winPanel);
            SetField(ui, "losePanel", losePanel);
            SetField(ui, "pauseRoot", pauseRoot.gameObject);
            SetField(ui, "resumeButton", resumeButton);
            SetField(ui, "pauseRestartButton", pauseRestart);
        }

        static EndPanelUI BuildEndPanel(string name, Transform canvas, Color accent)
        {
            var holder = Stretch(Rect(name, canvas));
            var root = Panel("Root", holder, new Color(0.02f, 0.02f, 0.03f, 0.92f), null);
            Stretch(root.rectTransform);
            var title = Label("Title", root.transform, "Title", 64f, accent, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1400f, 90f));
            title.fontStyle = FontStyles.Bold;
            var body = Label("Body", root.transform, "Body", 32f, Color.white, TextAlignmentOptions.Center);
            Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1200f, 340f));
            body.enableAutoSizing = true;
            body.fontSizeMin = 22f;
            body.fontSizeMax = 32f;
            var restart = MakeButton("RestartButton", root.transform, "Restart", new Vector2(300f, 70f), 30f);
            Place((RectTransform)restart.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -250f), new Vector2(300f, 70f));
            var panel = holder.gameObject.AddComponent<EndPanelUI>();
            SetField(panel, "root", root.gameObject);
            SetField(panel, "titleText", title);
            SetField(panel, "bodyText", body);
            SetField(panel, "restartButton", restart);
            return panel;
        }

        // =====================================================================================  Helpers: world

        static Transform Group(string name, Transform parent, Vector3 localPosition, float yaw = 0f)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return t;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material, bool collider = true) =>
            Prim(PrimitiveType.Cube, name, parent, localPosition, size, material, collider);

        /// <summary>Flat-colored URP Lit material saved as an asset. The first call per name sets the color.</summary>
        static Material Mat(string name, Color color, float smoothness = 0.1f, Color? emission = null)
        {
            if (MaterialCache.TryGetValue(name, out var cached)) return cached;
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(material);
            MaterialCache[name] = material;
            return material;
        }

        static TextMeshPro WorldText(string name, Transform parent, Vector3 localPosition, string text, float fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(6f, 1.5f);
            return tmp;
        }

        // =====================================================================================  Helpers: UI

        static Sprite Sprite(string path) => AssetDatabase.GetBuiltinExtraResource<Sprite>(path);

        static TMP_DefaultControls.Resources TmpResources() => new TMP_DefaultControls.Resources
        {
            standard = Sprite("UI/Skin/UISprite.psd"),
            background = Sprite("UI/Skin/Background.psd"),
            inputField = Sprite("UI/Skin/InputFieldBackground.psd"),
            knob = Sprite("UI/Skin/Knob.psd"),
            checkmark = Sprite("UI/Skin/Checkmark.psd"),
            dropdown = Sprite("UI/Skin/DropdownArrow.psd"),
            mask = Sprite("UI/Skin/UIMask.psd"),
        };

        static DefaultControls.Resources UguiResources() => new DefaultControls.Resources
        {
            standard = Sprite("UI/Skin/UISprite.psd"),
            background = Sprite("UI/Skin/Background.psd"),
            inputField = Sprite("UI/Skin/InputFieldBackground.psd"),
            knob = Sprite("UI/Skin/Knob.psd"),
            checkmark = Sprite("UI/Skin/Checkmark.psd"),
            dropdown = Sprite("UI/Skin/DropdownArrow.psd"),
            mask = Sprite("UI/Skin/UIMask.psd"),
        };

        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        static Image Panel(string name, Transform parent, Color color) => Panel(name, parent, color, Sprite("UI/Skin/UISprite.psd"));

        static Image Panel(string name, Transform parent, Color color, Sprite sprite, bool sliced = true)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            if (sprite)
            {
                image.sprite = sprite;
                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
            return image;
        }

        static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
        {
            var label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        static void FitToContent(GameObject go, RectOffset padding)
        {
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = padding;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static Button MakeButton(string name, Transform parent, string label, Vector2 size, float fontSize = 26f)
        {
            var go = TMP_DefaultControls.CreateButton(TmpResources());
            go.name = name;
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = size;
            go.GetComponent<Image>().color = ButtonColor;
            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            text.font = font;
            text.text = label;
            text.fontSize = fontSize;
            text.color = Ink;
            return go.GetComponent<Button>();
        }

        static TMP_Dropdown MakeDropdown(string name, Transform parent)
        {
            var go = TMP_DefaultControls.CreateDropdown(TmpResources());
            go.name = name;
            go.transform.SetParent(parent, false);
            var dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.captionText.font = font;
            dropdown.captionText.fontSize = 26f;
            dropdown.captionText.alignment = TextAlignmentOptions.MidlineLeft;
            dropdown.itemText.font = font;
            dropdown.itemText.fontSize = 24f;

            var template = dropdown.template;
            template.sizeDelta = new Vector2(template.sizeDelta.x, 280f);
            var item = (RectTransform)dropdown.itemText.transform.parent;
            item.sizeDelta = new Vector2(item.sizeDelta.x, 46f);
            var content = (RectTransform)item.parent;
            content.sizeDelta = new Vector2(content.sizeDelta.x, 50f);
            var arrow = go.transform.Find("Arrow") as RectTransform;
            if (arrow) arrow.sizeDelta = new Vector2(28f, 28f);

            dropdown.ClearOptions();
            return dropdown;
        }

        // =====================================================================================  Helpers: serialization

        static void SetField(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null) throw new ArgumentException($"{target.GetType().Name} has no serialized field '{field}'.");

            switch (value)
            {
                case Object reference: property.objectReferenceValue = reference; break;
                case string s: property.stringValue = s; break;
                case bool b: property.boolValue = b; break;
                case int i: property.intValue = i; break;
                case float f: property.floatValue = f; break;
                case Vector3 v: property.vector3Value = v; break;
                case Color color: property.colorValue = color; break;
                default: throw new ArgumentException($"Unsupported value type for '{field}'.");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetList(Object target, string field, IList<Object> values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddSceneToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
