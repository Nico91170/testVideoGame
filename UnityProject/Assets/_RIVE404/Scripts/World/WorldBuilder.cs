using System;
using System.Collections.Generic;
using System.Reflection;
using Rive404.Domain;
using Rive404.Player;
using Rive404.Runtime;
using UnityEngine;

namespace Rive404.World
{
    /// <summary>
    /// Builds the complete campaign space from reusable low-poly modules. Gameplay objects
    /// remain resident; only decoration chunks sleep, so streaming never resets quest state.
    /// Ground bounds are [-500,500] metres on each axis. Buildings subtract a small footprint
    /// from that gross surface; the exact playable surface needs measurement in Unity.
    /// </summary>
    public sealed class WorldBuilder : MonoBehaviour
    {
        public Vector3 SpawnPoint => new Vector3(-340f, 1.1f, -300f);
        public List<MapEntry> Entries { get; } = new List<MapEntry>();
        public bool tryBakeNavigation = true;
        public float decorationDistance = 275f;

        private readonly Dictionary<string, Vector3> positions = new Dictionary<string, Vector3>();
        private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        private readonly Dictionary<Vector2Int, Transform> chunks = new Dictionary<Vector2Int, Transform>();
        private readonly List<Vector3> protectedSpots = new List<Vector3>();
        private readonly List<WorldInteractable> interactables = new List<WorldInteractable>();
        private readonly List<GameObject> finaleChoices = new List<GameObject>();
        private GameRuntime runtime;
        private Transform content;
        private Transform persistent;
        private GameObject captain;
        private float nextStreamingUpdate;
        private bool built;
        private readonly Color port = new Color(.83f, .65f, .40f);
        private readonly Color neon = new Color(.10f, .24f, .35f);
        private readonly Color cliffs = new Color(.62f, .27f, .20f);
        private readonly Color cyan = new Color(.16f, .90f, .96f);
        private readonly Color gold = new Color(1f, .75f, .20f);

        public void Build(GameRuntime gameRuntime)
        {
            ResetWorld();
            runtime = gameRuntime;
            content = new GameObject("Santa Riva — 1000 m × 1000 m").transform;
            content.SetParent(transform, false);
            persistent = new GameObject("Campaign objects (persistent)").transform;
            persistent.SetParent(content, false);
            BuildTerrainAndRoads();
            BuildInteriors();
            BuildCitizens();
            BuildObjectives();
            BuildEnemies();
            BuildDecorations();
            built = true;
            RestoreCollectedObjects();
            UpdateStreaming(true);
            BakeNavigationIfAvailable();
        }

        public void ResetWorld()
        {
            built = false;
            if (content != null)
            {
                content.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(content.gameObject);
                else DestroyImmediate(content.gameObject);
            }
            content = null;
            persistent = null;
            Entries.Clear();
            positions.Clear();
            chunks.Clear();
            protectedSpots.Clear();
            interactables.Clear();
            finaleChoices.Clear();
            captain = null;
            nextStreamingUpdate = 0f;
        }

        public Vector3 PositionOf(string id)
        {
            return id != null && positions.TryGetValue(id, out Vector3 position) ? position : SpawnPoint;
        }

        private void Update()
        {
            if (!built || runtime == null || Time.unscaledTime < nextStreamingUpdate) return;
            nextStreamingUpdate = Time.unscaledTime + .5f;
            UpdateStreaming(false);
        }

        private void UpdateStreaming(bool force)
        {
            Vector3 viewer = SpawnPoint;
            if (runtime != null && runtime.IsPlaying && runtime.Player != null)
                viewer = runtime.Player.transform.position;
            else if (runtime != null && runtime.State != null && runtime.State.position != null)
                viewer = new Vector3(runtime.State.position.x, runtime.State.position.y, runtime.State.position.z);
            // A 160 m cell's half diagonal is 114 m. Keep every local collider alive well
            // before a player can reach it, and avoid thrashing on a chunk boundary.
            float radius = Mathf.Max(180f, decorationDistance) + 114f;
            foreach (KeyValuePair<Vector2Int, Transform> pair in chunks)
            {
                Vector3 center = new Vector3((pair.Key.x + .5f) * 160f, viewer.y, (pair.Key.y + .5f) * 160f);
                float margin = pair.Value.gameObject.activeSelf ? 35f : 0f;
                bool visible = (center - viewer).sqrMagnitude < (radius + margin) * (radius + margin);
                if (force || visible != pair.Value.gameObject.activeSelf) pair.Value.gameObject.SetActive(visible);
            }
            bool endingAvailable = HasFlag("kill:archivist");
            foreach (GameObject choice in finaleChoices)
                if (choice != null && choice.activeSelf != endingAvailable) choice.SetActive(endingAvailable);

            // Sillage's sacrifice is a persistent world change. Keep him available for his
            // last conversation in an older save that collected the anchor prematurely.
            if (captain != null && runtime.Quests != null)
            {
                var sacrifice = runtime.Quests.GetProgress("sacrifice_sillage");
                if (sacrifice != null && sacrifice.status != QuestStatus.NotStarted)
                    MoveCaptain(new Vector3(300f, 0f, 233f));
                if (HasFlag("interact:fort_anchor") && sacrifice != null && sacrifice.stepIndex >= 1)
                    captain.SetActive(false);
            }

            int district = viewer.x < -120f ? 0 : viewer.x < 220f ? 1 : 2;
            string visitedFlag = "visited:" + district;
            if (runtime.State != null && runtime.State.flags != null && !runtime.State.flags.Contains(visitedFlag))
                runtime.State.flags.Add(visitedFlag);
        }

        private void BuildTerrainAndRoads()
        {
            Cube(content, "Accessible ground (gross 1 km²)", new Vector3(0f, -.5f, 0f),
                new Vector3(1000f, 1f, 1000f), new Color(.33f, .38f, .31f));
            // Border walls are beyond the measured square. Four entrances to each interior
            // and generous roads make the whole continuous square traversable on foot.
            Cube(content, "North world boundary", new Vector3(0f, 4f, 503f), new Vector3(1012f, 9f, 5f), cliffs);
            Cube(content, "South world boundary", new Vector3(0f, 4f, -503f), new Vector3(1012f, 9f, 5f), port);
            Cube(content, "East world boundary", new Vector3(503f, 4f, 0f), new Vector3(5f, 9f, 1012f), cliffs);
            Cube(content, "West world boundary", new Vector3(-503f, 4f, 0f), new Vector3(5f, 9f, 1012f), port);
            Road(new Vector3(-480, 0, -420), new Vector3(-130, 0, -200), 18f, new Color(.65f, .57f, .43f));
            Road(new Vector3(-130, 0, -200), new Vector3(150, 0, 80), 18f, new Color(.20f, .28f, .32f));
            Road(new Vector3(150, 0, 80), new Vector3(375, 0, 380), 18f, new Color(.49f, .31f, .24f));
            Road(new Vector3(-450, 0, -280), new Vector3(-105, 0, -210), 13f, new Color(.57f, .49f, .37f));
            Road(new Vector3(-110, 0, -50), new Vector3(190, 0, 70), 14f, new Color(.18f, .24f, .30f));
            Road(new Vector3(250, 0, 140), new Vector3(395, 0, 355), 14f, new Color(.57f, .34f, .25f));
            Road(new Vector3(-420, 0, 280), new Vector3(420, 0, 280), 11f, new Color(.38f, .40f, .32f));
            Road(new Vector3(-360, 0, -450), new Vector3(-360, 0, 450), 11f, new Color(.51f, .47f, .36f));
            Road(new Vector3(60, 0, -440), new Vector3(60, 0, 440), 11f, new Color(.27f, .33f, .34f));

            Landmark("port", "Vieux-Port", new Vector3(-335, 0, -305), 0, port);
            Landmark("neon", "Quartier Néon", new Vector3(0, 0, -50), 1, neon);
            Landmark("cliffs", "Falaises Rouges", new Vector3(320, 0, 230), 2, cliffs);

            // Sea and cliff skyline belong to the backdrop; they are excluded from the
            // accessible area and never cover the ground square or quest paths.
            Cube(content, "Sea backdrop (outside playable square)", new Vector3(-720, -2f, -80),
                new Vector3(420, .3f, 1440), new Color(.06f, .28f, .39f), false);
            for (int i = 0; i < 15; i++)
            {
                GameObject rock = Primitive(content, PrimitiveType.Cylinder, "Red cliff skyline",
                    new Vector3(530f + i % 3 * 18f, 16f + i % 4 * 8f, -430f + i * 65f),
                    new Vector3(48f, 25f + i % 4 * 8f, 48f), cliffs, false);
                rock.transform.rotation = Quaternion.Euler(0, i * 31, 14);
            }
        }

        private void BuildInteriors()
        {
            Interior("captain_house", "Repaire du capitaine", new Vector3(-254, 0, -247), 24, 20, 0, port);
            Interior("port_depot", "Entrepôt des fragments", new Vector3(-128, 0, -204), 34, 28, 0, port);
            Interior("arcade", "Arcade Mémoire Vive", new Vector3(0, 0, -40), 30, 30, 1, neon);
            Interior("laboratory", "Laboratoire des anomalies", new Vector3(93, 0, 25), 36, 40, 1, neon);
            Interior("archive", "Archives publiques", new Vector3(137, 0, 52), 28, 28, 1, neon);
            Interior("fort", "Cour du fort rouge", new Vector3(350, 0, 325), 70, 85, 2, cliffs, 10f);
            // Furniture is deliberately sparse: a beginner can navigate each real interior
            // without camera roof occlusion, and each has at least two safe exits.
            Cube(persistent, "Capitaine desk", new Vector3(-259, .6f, -250), new Vector3(4, 1.2f, 1.5f), port);
            Cube(persistent, "Arcade cabinet row", new Vector3(-10, 1.2f, -36), new Vector3(2, 2.4f, 6), neon);
            Cube(persistent, "Laboratory workbench", new Vector3(105, .65f, 16), new Vector3(2, 1.3f, 6), cyan);
            Cube(persistent, "Archive shelf", new Vector3(128, 1.5f, 54), new Vector3(1.5f, 3, 10), port);
            Cube(persistent, "Fort monument", new Vector3(350, 2f, 285), new Vector3(9, 4, 4), cliffs);
        }

        private void BuildCitizens()
        {
            Citizen("brune", "Brune — mécanicienne", -340, -280, 0);
            Citizen("sillage", "Capitaine Sillage", -263, -241, 0, null, "Sillage");
            Citizen("mila", "Mila — cartographe", -215, -270, 0);
            Citizen("jo", "Jo — mémoire de l'arcade", -26, -55, 1);
            Citizen("ines", "Inès — ingénieure", 73, -9, 1);
            Citizen("iris", "Iris — archiviste", 128, 34, 1);
            Citizen("orson", "Orson — gardien des falaises", 280, 160, 2);
            Citizen("amir", "Amir — docker", -390, -330, 0, "side_lost_cargo");
            Citizen("lucie", "Lucie — radio du port", -180, -180, 0, "side_radio");
            Citizen("teo", "Téo — chasseur de lucioles", -90, 20, 1, "side_fireflies");
            Citizen("alma", "Alma — conservatrice", 180, 60, 1, "side_archive");
            Citizen("gaston", "Gaston — veilleur", 360, 190, 2, "side_watchtower");
        }

        private void BuildObjectives()
        {
            Terminal("wake_terminal", "Terminal d'éveil", -318, -276, 0, DebugOperation.Scan);
            WorldInteractable camera = Terminal("port_camera", "Caméra du quai", -256, -262, 0, DebugOperation.Override);
            camera.controlledObject = Signal("Caméra du quai active", new Vector3(-256, 3.5f, -264), Color.red);
            Terminal("port_ledger", "Registre du capitaine", -254, -247, 0, DebugOperation.Scan).debugTarget = false;
            Bridge("port_bridge", "Passerelle du dépôt", new Vector3(-200, 0, -215), 0);
            Collectible("fragment_port", "Fragment du Vieux-Port", -128, -206, 0);

            WorldInteractable power = Terminal("arcade_power", "Alimentation de l'arcade", -9, -51, 1, DebugOperation.Patch);
            power.controlledObject = Signal("Enseigne de l'arcade restaurée", new Vector3(0, 5f, -55), cyan);
            power.controlledObject.SetActive(false);
            Terminal("arcade_memory", "Mémoire vive de l'arcade", 0, -36, 1, DebugOperation.Scan).debugTarget = false;
            WorldInteractable gate = Terminal("lab_gate", "Verrou du laboratoire", 88, 0, 1, DebugOperation.Override);
            gate.controlledObject = Cube(persistent, "Laboratory entrance barrier (alternate exit east)",
                new Vector3(93, 1.4f, 5), new Vector3(7, 2.8f, .6f), new Color(.96f, .27f, .23f));
            Terminal("lab_archive", "Archives expérimentales", 85, 30, 1, DebugOperation.Scan);
            Collectible("fragment_neon", "Fragment du Quartier Néon", 99, 32, 1);
            Terminal("memory_console", "Console des souvenirs", 137, 52, 1, DebugOperation.Scan).debugTarget = false;
            Terminal("neon_beacon", "Balise des trois fragments", 160, 95, 1, DebugOperation.Scan).debugTarget = false;

            Bridge("cliff_bridge", "Passerelle des falaises", new Vector3(252, 0, 150), 2);
            Collectible("fragment_cliffs", "Fragment des Falaises Rouges", 287, 208, 2);
            WorldInteractable relay = Terminal("fort_relay", "Relais du fort", 309, 240, 2, DebugOperation.Override);
            relay.controlledObject = Signal("Relais du fort actif", new Vector3(309, 3.8f, 242), Color.red);
            Terminal("fort_anchor", "Ancrage du capitaine", 336, 277, 2, DebugOperation.Scan).debugTarget = false;
            WorldInteractable core = Terminal("final_core", "Noyau du programme", 347, 303, 2, DebugOperation.Override);
            core.controlledObject = Signal("Bouclier du noyau", new Vector3(347, 3.4f, 304), new Color(.88f, .1f, .50f));
            Ending("ending_restore", "Restaurer Santa Riva", new Vector3(340, 0, 360), InteractableKind.EndingRestore);
            Ending("ending_liberate", "Libérer Santa Riva", new Vector3(370, 0, 360), InteractableKind.EndingLiberate);

            Collectible("lost_cargo", "Caisse égarée d'Amir", -425, -355, 0);
            WorldInteractable antenna = Terminal("radio_antenna", "Antenne de Lucie", -175, -180, 0, DebugOperation.Patch);
            antenna.controlledObject = Signal("Transmission radio", new Vector3(-175, 4, -183), cyan);
            antenna.controlledObject.SetActive(false);
            Terminal("radio_console", "Console de diffusion", -162, -176, 0, DebugOperation.Scan).debugTarget = false;
            Collectible("neon_cache", "Cache des lucioles", -78, 59, 1);
            Collectible("archive_shard", "Éclat d'archive", 184, 93, 1, true);
            WorldInteractable watch = Terminal("watch_camera", "Caméra de la tour", 385, 210, 2, DebugOperation.Override);
            watch.controlledObject = Signal("Caméra de veille", new Vector3(385, 4, 212), Color.red);
            WorldInteractable generator = Terminal("watch_generator", "Générateur du veilleur", 395, 228, 2, DebugOperation.Patch);
            generator.controlledObject = Signal("Tour restaurée", new Vector3(395, 6, 231), gold);
            generator.controlledObject.SetActive(false);

            Checkpoint("checkpoint_port", "Refuge du port — sauvegarde et recharge", -343, -302, 0);
            Checkpoint("checkpoint_neon", "Refuge Néon — sauvegarde et recharge", 36, -80, 1);
            Checkpoint("checkpoint_cliffs", "Refuge des falaises — sauvegarde et recharge", 282, 230, 2);
        }

        private void BuildEnemies()
        {
            Enemy("tutorial_drone", "Drone d'éveil", -298, -274, 0, EnemyKind.Drone, "Drone");
            Enemy("depot_sentinel", "Sentinelle du dépôt", -146, -227, 0, EnemyKind.Sentinel, "Sentinel");
            Enemy("arcade_corrupted", "Corrompu de l'arcade", 18, -35, 1, EnemyKind.Corrupted, "Corrupted");
            Enemy("neon_sentinel", "Sentinelle des archives", 148, 76, 1, EnemyKind.Sentinel, "Sentinel");
            Enemy("cliff_guardian", "Gardien du fragment rouge", 268, 190, 2, EnemyKind.Corrupted, "Corrupted");
            Enemy("alley_drone", "Drone des lucioles", -80, 42, 1, EnemyKind.Drone, "Drone");
            Enemy("archivist", "L'Archiviste — boss", 356, 332, 2, EnemyKind.Boss, "Boss");
        }

        private void Citizen(string id, string title, float x, float z, int district, string side = null, string model = "NPC")
        {
            GameObject root = Root(id, title, new Vector3(x, 0, z), district);
            Color color = district == 0 ? port : district == 1 ? cyan : cliffs;
            if (ModelFactory.Attach(root.transform, "Models/" + model, Vector3.one) == null)
                Primitive(root.transform, PrimitiveType.Capsule, "Citizen visual", new Vector3(0, 1, 0),
                    new Vector3(.8f, 1f, .8f), color, false, true);
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = Vector3.up;
            collider.height = 2f;
            collider.radius = .45f;
            WorldInteractable item = root.AddComponent<WorldInteractable>();
            item.id = id; item.title = title; item.npcId = id; item.eventId = "talk:" + id;
            item.sideQuestId = side; item.kind = InteractableKind.Dialogue;
            interactables.Add(item);
            ExcludeActorFromNavigation(root);
            if (id == "sillage") captain = root;
        }

        private WorldInteractable Terminal(string id, string title, float x, float z, int district, DebugOperation operation)
        {
            GameObject root = Root(id, title, new Vector3(x, 0, z), district);
            if (ModelFactory.Attach(root.transform, "Models/Terminal", Vector3.one) == null)
                Cube(root.transform, "Terminal pedestal", new Vector3(0, .75f, 0), new Vector3(1.4f, 1.5f, 1), neon, false, true);
            Cube(root.transform, "Screen", new Vector3(0, 1.25f, -.58f), new Vector3(1.1f, .6f, .1f), cyan, false, true);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, .8f, 0); collider.size = new Vector3(1.7f, 1.6f, 1.3f);
            WorldInteractable item = root.AddComponent<WorldInteractable>();
            item.id = id; item.title = title; item.eventId = "interact:" + id;
            item.kind = InteractableKind.Terminal; item.debugTarget = true; item.operation = operation;
            interactables.Add(item);
            return item;
        }

        private void Collectible(string id, string title, float x, float z, int district, bool scan = false)
        {
            GameObject root = Root(id, title, new Vector3(x, 0, z), district);
            Primitive(root.transform, PrimitiveType.Cube, "Memory fragment", new Vector3(0, 1f, 0),
                new Vector3(1.2f, 1.2f, 1.2f), gold, false, true).transform.rotation = Quaternion.Euler(0, 45, 0);
            Primitive(root.transform, PrimitiveType.Cylinder, "Fragment plinth", new Vector3(0, .25f, 0),
                new Vector3(1.8f, .25f, 1.8f), neon, false, true);
            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.center = Vector3.up; collider.radius = .9f;
            WorldInteractable item = root.AddComponent<WorldInteractable>();
            item.id = id; item.title = title; item.eventId = "interact:" + id;
            item.kind = InteractableKind.Collectible; item.debugTarget = scan; item.operation = DebugOperation.Scan;
            interactables.Add(item);
        }

        private void Checkpoint(string id, string title, float x, float z, int district)
        {
            WorldInteractable item = Terminal(id, title, x, z, district, DebugOperation.Scan);
            item.kind = InteractableKind.Checkpoint; item.debugTarget = false;
            Primitive(item.transform, PrimitiveType.Cylinder, "Refuge light", new Vector3(0, .1f, 0),
                new Vector3(5, .1f, 5), new Color(.24f, .74f, .37f), false, true);
        }

        private void Ending(string id, string title, Vector3 position, InteractableKind kind)
        {
            WorldInteractable item = Terminal(id, title, position.x, position.z, 2, DebugOperation.Scan);
            item.kind = kind; item.debugTarget = false;
            finaleChoices.Add(item.gameObject);
        }

        private void Bridge(string id, string title, Vector3 position, int district)
        {
            WorldInteractable terminal = Terminal(id, title + " (chemin de secours à côté)", position.x, position.z, district, DebugOperation.Patch);
            GameObject bridge = Cube(persistent, title + " — PATCH platform", position + new Vector3(0, .18f, 8),
                new Vector3(10, .3f, 16), cyan);
            terminal.controlledObject = bridge;
            bridge.SetActive(false);
            // Broken planks and dark paint explain the anomaly, while the dry service path
            // stays walkable beside it even when a temporary PATCH expires.
            Cube(persistent, title + " anomaly bed", position + new Vector3(0, .015f, 8),
                new Vector3(10, .02f, 16), new Color(.10f, .16f, .19f), false);
            Road(position + new Vector3(9, 0, -3), position + new Vector3(9, 0, 18), 6, port);
        }

        private void Enemy(string id, string label, float x, float z, int district, EnemyKind kind, string model)
        {
            GameObject root = Root(id, label, new Vector3(x, 1.05f, z), district);
            GameObject visual = ModelFactory.Attach(root.transform, "Models/" + model, Vector3.one);
            if (visual != null) visual.transform.localPosition = Vector3.down;
            if (visual == null)
                Primitive(root.transform, kind == EnemyKind.Drone ? PrimitiveType.Sphere : PrimitiveType.Capsule,
                    "Enemy visual", Vector3.zero, kind == EnemyKind.Boss ? new Vector3(1.8f, 1.6f, 1.8f) : Vector3.one,
                    new Color(.72f, .13f, .15f), false, true);
            EnemyAgent enemy = root.AddComponent<EnemyAgent>();
            enemy.Configure(runtime, id, kind);
            ExcludeActorFromNavigation(root);
            if (HasFlag("kill:" + id)) root.SetActive(false);
        }

        private void Interior(string id, string label, Vector3 center, float width, float depth, int district, Color color, float height = 5f)
        {
            GameObject root = Root(id, label, center, district);
            // Door openings in all four walls, no roof: genuine interiors with safe exits and
            // camera access. The doorway is 8 m wide, enough for controller and third-person camera.
            float opening = 8f;
            float sideWidth = (width - opening) * .5f;
            float sideDepth = (depth - opening) * .5f;
            float xOffset = opening * .5f + sideWidth * .5f;
            float zOffset = opening * .5f + sideDepth * .5f;
            Cube(root.transform, "Interior floor", new Vector3(0, .04f, 0), new Vector3(width, .08f, depth), color, true, true);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int half = -1; half <= 1; half += 2)
                {
                    Cube(root.transform, "North/south wall module", new Vector3(half * xOffset, height * .5f, side * depth * .5f),
                        new Vector3(sideWidth, height, .8f), color, true, true);
                    Cube(root.transform, "East/west wall module", new Vector3(side * width * .5f, height * .5f, half * zOffset),
                        new Vector3(.8f, height, sideDepth), color, true, true);
                }
                Cube(root.transform, "Door lintel", new Vector3(0, height - .5f, side * depth * .5f),
                    new Vector3(opening, 1, .8f), color, false, true);
            }
            protectedSpots.Add(center + new Vector3(-width * .5f, 0, -depth * .5f));
            protectedSpots.Add(center + new Vector3(width * .5f, 0, depth * .5f));
        }

        private void Landmark(string id, string label, Vector3 center, int district, Color color)
        {
            Register(id, label, center, district);
            Primitive(content, PrimitiveType.Cylinder, label + " landmark base", center + new Vector3(0, .04f, 0),
                new Vector3(18, .04f, 18), color, false);
            // Only the marker is a collider; the flat district marker cannot impede travel.
            Cube(content, label + " orientation pillar", center + new Vector3(8, 4, 8), new Vector3(2, 8, 2), color);
            Signal(label + " beacon", center + new Vector3(8, 8.5f, 8), district == 1 ? cyan : gold);
        }

        private void BuildDecorations()
        {
            System.Random random = new System.Random(404);
            // Population deliberately avoids campaign targets and the circulation spine.
            for (int z = -430; z <= 430; z += 62)
            {
                for (int x = -430; x <= 430; x += 62)
                {
                    Vector3 point = new Vector3(x + random.Next(-9, 10), 0, z + random.Next(-9, 10));
                    if (NearProtectedSpot(point, 28f) || NearRoad(point, 22f)) continue;
                    int district = point.x < -120 ? 0 : point.x < 220 ? 1 : 2;
                    Transform chunk = DecorationChunk(point);
                    if ((x + z) % 3 == 0 || district == 2)
                    {
                        VisualDecoration(chunk, "Tree", "Wind-shaped tree", point, Vector3.one * (district == 2 ? 1.4f : 1f),
                            new Vector3(3, 6, 3), new Color(.28f, .43f, .29f), false);
                    }
                    else
                    {
                        Color color = district == 0 ? port : neon;
                        float height = district == 1 ? random.Next(8, 17) : random.Next(5, 10);
                        VisualDecoration(chunk, "House", district == 0 ? "Port house module" : "Neon residence module", point,
                            Vector3.one, new Vector3(14, height, 14), color, true);
                        if (district == 1)
                            Cube(chunk, "Neon façade strip", point + new Vector3(0, height * .75f, -7.1f),
                                new Vector3(11, .35f, .15f), cyan, false);
                    }
                }
            }
            for (int i = 0; i < 32; i++)
            {
                float t = i / 31f;
                Vector3 point = Vector3.Lerp(new Vector3(-400, 0, -355), new Vector3(370, 0, 365), t);
                point += new Vector3(i % 2 == 0 ? -15 : 15, 0, 15);
                if (NearProtectedSpot(point, 7f)) continue;
                VisualDecoration(DecorationChunk(point), "Lamp", "Street light", point, Vector3.one,
                    new Vector3(.55f, 5, .55f), port, false);
            }
            for (int i = 0; i < 12; i++)
            {
                Vector3 point = new Vector3(-460, 0, -410 + i * 55);
                VisualDecoration(DecorationChunk(point), "Boat", "Moored boat (not driveable)", point,
                    Vector3.one, new Vector3(10, 2, 5), port, true);
                Cube(DecorationChunk(point), "Quay bollard", point + new Vector3(8, .65f, 0), new Vector3(1.3f, 1.3f, 1.3f), neon);
            }
            VisualDecoration(DecorationChunk(new Vector3(400, 0, 300)), "Tower", "Watchtower landmark", new Vector3(400, 0, 300),
                Vector3.one, new Vector3(10, 18, 10), cliffs, true);
            VisualDecoration(DecorationChunk(new Vector3(-180, 0, -320)), "Warehouse", "Outer warehouse", new Vector3(-180, 0, -320),
                Vector3.one, new Vector3(22, 7, 15), port, true);
        }

        private bool NearProtectedSpot(Vector3 position, float radius)
        {
            foreach (Vector3 spot in protectedSpots)
                if ((new Vector2(spot.x - position.x, spot.z - position.z)).sqrMagnitude < radius * radius) return true;
            return false;
        }

        private static bool NearRoad(Vector3 position, float radius)
        {
            Vector3[] spine = { new Vector3(-480, 0, -420), new Vector3(-130, 0, -200), new Vector3(150, 0, 80), new Vector3(375, 0, 380) };
            for (int i = 0; i < spine.Length - 1; i++)
            {
                Vector3 delta = spine[i + 1] - spine[i];
                float t = Mathf.Clamp01(Vector3.Dot(position - spine[i], delta) / delta.sqrMagnitude);
                if ((position - (spine[i] + delta * t)).sqrMagnitude < radius * radius) return true;
            }
            return Mathf.Abs(position.x + 360) < radius || Mathf.Abs(position.x - 60) < radius || Mathf.Abs(position.z - 280) < radius;
        }

        private Transform DecorationChunk(Vector3 position)
        {
            Vector2Int key = new Vector2Int(Mathf.FloorToInt(position.x / 160f), Mathf.FloorToInt(position.z / 160f));
            if (chunks.TryGetValue(key, out Transform chunk)) return chunk;
            chunk = new GameObject("Decoration chunk " + key.x + "," + key.y).transform;
            chunk.SetParent(content, false);
            chunks.Add(key, chunk);
            return chunk;
        }

        private void VisualDecoration(Transform parent, string resource, string label, Vector3 position,
            Vector3 modelScale, Vector3 fallbackSize, Color color, bool solid)
        {
            GameObject root = new GameObject(label);
            root.transform.SetParent(parent, false); root.transform.position = position;
            GameObject visual = ModelFactory.Attach(root.transform, "Models/" + resource, modelScale);
            if (visual == null)
                Cube(root.transform, "Fallback module", new Vector3(0, fallbackSize.y * .5f, 0), fallbackSize, color, false, true);
            else if (solid) FitDecorationBounds(visual, fallbackSize);
            if (solid)
            {
                // Stable simplified physics avoids relying on any exported mesh colliders.
                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(0, fallbackSize.y * .5f, 0);
                collider.size = fallbackSize;
            }
        }

        private GameObject Root(string id, string label, Vector3 position, int district)
        {
            GameObject root = new GameObject(id + " — " + label);
            root.transform.SetParent(persistent, false);
            root.transform.position = position;
            Register(id, label, position, district);
            return root;
        }

        private void Register(string id, string label, Vector3 position, int district)
        {
            if (positions.ContainsKey(id)) throw new InvalidOperationException("Duplicate world id: " + id);
            positions.Add(id, position);
            Entries.Add(new MapEntry(id, label, position, district));
            protectedSpots.Add(position);
        }

        private void Road(Vector3 from, Vector3 to, float width, Color color)
        {
            Vector3 delta = to - from;
            GameObject road = Cube(content, "Walkable circulation route", (from + to) * .5f + new Vector3(0, .013f, 0),
                new Vector3(width, .024f, delta.magnitude), color, false);
            road.transform.rotation = Quaternion.LookRotation(delta);
        }

        private GameObject Signal(string label, Vector3 position, Color color)
        {
            GameObject signal = Primitive(persistent, PrimitiveType.Sphere, label, position, new Vector3(1.2f, 1.2f, 1.2f), color, false);
            // Emission reads clearly under the standard directional light without adding a
            // point light for every objective, keeping the lighting cost bounded.
            Renderer renderer = signal.GetComponent<Renderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_EmissionColor", color * 1.5f);
            renderer.SetPropertyBlock(block);
            return signal;
        }

        private GameObject Cube(Transform parent, string label, Vector3 position, Vector3 scale, Color color, bool collider = true, bool local = false)
        {
            return Primitive(parent, PrimitiveType.Cube, label, position, scale, color, collider, local);
        }

        private GameObject Primitive(Transform parent, PrimitiveType type, string label, Vector3 position,
            Vector3 scale, Color color, bool collider = true, bool local = false)
        {
            GameObject value = GameObject.CreatePrimitive(type);
            value.name = label;
            value.transform.SetParent(parent, false);
            if (local) value.transform.localPosition = position; else value.transform.position = position;
            value.transform.localScale = scale;
            value.GetComponent<Renderer>().sharedMaterial = Material(color);
            Collider shape = value.GetComponent<Collider>();
            if (!collider && shape != null)
            {
                shape.enabled = false;
                if (Application.isPlaying) Destroy(shape); else DestroyImmediate(shape);
            }
            // Dynamic characters and temporary Debug Tool modules must not enter static
            // batching. All decorative geometry is stable inside its streaming chunk.
            value.isStatic = parent != null && !parent.IsChildOf(persistent) && parent != persistent;
            return value;
        }

        private Material Material(Color color)
        {
            if (materials.TryGetValue(color, out Material material)) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = "Santa Riva " + color, color = color };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .15f);
            materials.Add(color, material);
            return material;
        }

        private bool HasFlag(string flag)
        {
            return runtime != null && runtime.State != null && runtime.State.flags != null && runtime.State.flags.Contains(flag);
        }

        private void RestoreCollectedObjects()
        {
            foreach (WorldInteractable item in interactables)
            {
                if (item.kind != InteractableKind.Collectible || !HasFlag("collected:" + item.id)) continue;
                item.collected = true;
                item.gameObject.SetActive(false);
            }
        }

        private void BakeNavigationIfAvailable()
        {
            if (!tryBakeNavigation || content == null) return;
            Type surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (surfaceType == null) return; // EnemyAgent provides a collision-aware fallback.
            try
            {
                Component surface = content.gameObject.AddComponent(surfaceType);
                SetProperty(surface, "overrideVoxelSize", true);
                SetProperty(surface, "voxelSize", .75f);
                SetProperty(surface, "overrideTileSize", true);
                SetProperty(surface, "tileSize", 128);
                // Bake all decorations once while active, then restore distance streaming.
                foreach (Transform chunk in chunks.Values) chunk.gameObject.SetActive(true);
                MethodInfo build = surfaceType.GetMethod("BuildNavMesh", BindingFlags.Instance | BindingFlags.Public);
                build?.Invoke(surface, null);
                UpdateStreaming(true);
            }
            catch (Exception error)
            {
                Debug.LogWarning("RIVE404: NavMesh unavailable; enemies use collision-aware movement. " + error.GetBaseException().Message);
                UpdateStreaming(true);
            }
        }

        private static void SetProperty(Component component, string name, object value)
        {
            PropertyInfo property = component.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite) property.SetValue(component, value);
        }

        private static void ExcludeActorFromNavigation(GameObject actor)
        {
            Type modifierType = Type.GetType("Unity.AI.Navigation.NavMeshModifier, Unity.AI.Navigation");
            if (modifierType == null) return;
            Component modifier = actor.AddComponent(modifierType);
            SetProperty(modifier, "ignoreFromBuild", true);
        }

        private void MoveCaptain(Vector3 position)
        {
            if ((captain.transform.position - position).sqrMagnitude < .01f) return;
            captain.transform.position = position;
            positions["sillage"] = position;
            foreach (MapEntry entry in Entries)
            {
                if (entry.id != "sillage") continue;
                entry.position = position;
                entry.district = 2;
                break;
            }
        }

        private static void FitDecorationBounds(GameObject visual, Vector3 targetSize)
        {
            // Imported metric modules can differ in width. Fit the visible silhouette to its
            // simplified collider, so there are no large invisible walls around buildings.
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 size = bounds.size;
            if (size.x < .01f || size.y < .01f || size.z < .01f) return;
            visual.transform.localScale = Vector3.Scale(visual.transform.localScale,
                new Vector3(targetSize.x / size.x, targetSize.y / size.y, targetSize.z / size.z));
        }

        private void OnDestroy()
        {
            foreach (Material material in materials.Values)
                if (material != null) Destroy(material);
            materials.Clear();
        }
    }
}
