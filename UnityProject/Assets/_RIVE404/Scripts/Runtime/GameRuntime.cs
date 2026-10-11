using System;
using System.Collections.Generic;
using Rive404.Domain;
using Rive404.Player;
using Rive404.UI;
using Rive404.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rive404.Runtime
{
    /// <summary>Owns the session and dispatches interactions. The UI and components remain replaceable.</summary>
    public sealed class GameRuntime : MonoBehaviour
    {
        public static GameRuntime Instance { get; private set; }
        public static string SaveDirectoryOverride;
        public GameState State { get; private set; }
        public QuestEngine Quests { get; private set; }
        public PlayerController Player { get; private set; }
        public WorldBuilder World { get; private set; }
        public GameUI UI { get; private set; }
        public WorldInteractable CurrentTarget { get; private set; }
        public DialogueView CurrentDialogue { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsDialogueOpen { get { return CurrentDialogue != null; } }
        public bool CanSkipDialogue { get { return dialogueScript != null; } }
        public bool CanAcceptInput { get { return IsPlaying && !IsPaused && !IsDialogueOpen && string.IsNullOrEmpty(State.ending); } }
        public bool HasSave { get { return saves != null && saves.HasSave; } }
        public string ErrorMessage { get; private set; }

        private SaveStore saves;
        private DebugToolSettings debugSettings;
        private AudioDirector audioDirector;
        private Camera gameCamera;
        private WorldInteractable[] targets = Array.Empty<WorldInteractable>();
        private float nextTargetUpdate;
        private float nextAutoSave;
        private float lastScan;
        private DialogueScript dialogueScript;
        private string dialogueNpc;
        private readonly Dictionary<string, float> debugEffects = new Dictionary<string, float>();
        private readonly Dictionary<AudioSource, float> baseSoundVolumes = new Dictionary<AudioSource, float>();
        private float musicLevel = .7f;
        private float effectsLevel = .8f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            State = new GameState();
            ErrorMessage = string.Empty;
            saves = new SaveStore(SaveDirectoryOverride ?? Application.persistentDataPath,
                value => JsonUtility.ToJson(value, true), value => JsonUtility.FromJson<GameState>(value),
                value => JsonUtility.ToJson(value, true), value => JsonUtility.FromJson<SaveEnvelope>(value));
            debugSettings = Resources.Load<DebugToolSettings>("DebugToolSettings");
            if (debugSettings == null) debugSettings = ScriptableObject.CreateInstance<DebugToolSettings>();
            audioDirector = gameObject.AddComponent<AudioDirector>();
            audioDirector.Initialize();
            CreateCameraAndLight();
            UI = new GameObject("Interface française").AddComponent<GameUI>();
            UI.transform.SetParent(transform);
            UI.Bind(this);
            SetCursor(false);
        }

        private void CreateCameraAndLight()
        {
            gameCamera = Camera.main;
            if (gameCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                gameCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            gameCamera.farClipPlane = 750;
            gameCamera.nearClipPlane = .15f;
            gameCamera.backgroundColor = new Color(.12f, .19f, .25f);
            gameCamera.transform.position = new Vector3(-330, 26, -330);
            gameCamera.transform.LookAt(new Vector3(-300, 0, -290));
            var sun = new GameObject("Crépuscule").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1, .78f, .57f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(32, -38, 0);
            RenderSettings.ambientLight = new Color(.46f, .51f, .62f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = gameCamera.backgroundColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .0014f;
        }

        public void StartNewGame()
        {
            BeginSession(new GameState());
            ShowMessage("Santa Riva attend. Parlez à Brune près du quai. H : commandes ; J : journal ; M : carte.");
        }

        public void ContinueGame()
        {
            if (saves.TryLoad(out GameState state, out string message))
            {
                BeginSession(state);
                if (!string.IsNullOrEmpty(message)) ShowMessage(message);
                else ShowMessage("Progression chargée.");
            }
            else { ErrorMessage = message; ShowMessage(message); }
        }

        public bool LoadGame()
        {
            bool available = HasSave;
            if (available) ContinueGame();
            return available && IsPlaying;
        }

        private void BeginSession(GameState state)
        {
            IsPlaying = false;
            IsPaused = false;
            CurrentDialogue = null;
            dialogueScript = null;
            dialogueNpc = null;
            CurrentTarget = null;
            debugEffects.Clear();
            baseSoundVolumes.Clear();
            Time.timeScale = 1;
            if (World != null) { World.gameObject.SetActive(false); Destroy(World.gameObject); }
            if (Player != null) { Player.gameObject.SetActive(false); Destroy(Player.gameObject); }
            State = StateRules.Normalize(state);
            var catalog = Resources.Load<QuestCatalogAsset>("Campaign");
            var definitions = catalog != null && catalog.definitions != null && catalog.definitions.Count > 0
                ? catalog.definitions : Campaign.CreateDefinitions();
            Quests = new QuestEngine(State, definitions);
            ErrorMessage = string.Empty;

            var worldObject = new GameObject("Santa Riva — trois quartiers");
            World = worldObject.AddComponent<WorldBuilder>();
            World.Build(this);
            var playerObject = new GameObject("Nico");
            playerObject.transform.position = ToVector(State.position);
            var controller = playerObject.AddComponent<CharacterController>();
            controller.height = 1.85f;
            controller.radius = .34f;
            controller.center = new Vector3(0, .93f, 0);
            controller.stepOffset = .35f;
            controller.slopeLimit = 48;
            if (ModelFactory.Attach(playerObject.transform, "Models/Nico", Vector3.one) == null)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Nico — visuel de secours";
                body.transform.SetParent(playerObject.transform, false);
                body.transform.localPosition = Vector3.up * .95f;
                body.transform.localScale = new Vector3(.65f, .92f, .65f);
                body.GetComponent<Collider>().enabled = false;
                body.GetComponent<Renderer>().material.color = new Color(.2f, .32f, .29f);
            }
            Player = playerObject.AddComponent<PlayerController>();
            Player.Init(this);
            var follow = gameCamera.GetComponent<ThirdPersonCamera>();
            if (follow == null) follow = gameCamera.gameObject.AddComponent<ThirdPersonCamera>();
            follow.Init(Player.transform);
            follow.enabled = true;
            targets = World.GetComponentsInChildren<WorldInteractable>(true);
            foreach (var enemy in World.GetComponentsInChildren<EnemyAgent>(true))
            {
                var target = enemy.GetComponent<WorldInteractable>();
                if (target == null) target = enemy.gameObject.AddComponent<WorldInteractable>();
                target.id = enemy.Id;
                target.title = enemy.Kind == EnemyKind.Drone ? "Drone" : enemy.Kind == EnemyKind.Sentinel
                    ? "Sentinelle" : enemy.Kind == EnemyKind.Corrupted ? "Agent corrompu" : "L'Archiviste";
                target.debugTarget = enemy.Kind != EnemyKind.Boss;
                target.operation = DebugOperation.Override;
            }
            targets = World.GetComponentsInChildren<WorldInteractable>(true);
            IsPlaying = true;
            nextAutoSave = Time.time + 60;
            nextTargetUpdate = 0;
            foreach (var target in targets) target.RefreshFromState();
            if (State.health <= 0) Respawn();
            ApplyAudioSettings(musicLevel, effectsLevel);
            RefreshControlState();
        }

        private void Update()
        {
            if (!IsPlaying || Player == null) return;
            audioDirector.SetDistrict(Player.transform.position.x < -120 ? 0 : Player.transform.position.x < 220 ? 1 : 2);
            if (CanAcceptInput)
            {
                State.playSeconds += Time.deltaTime;
                State.position = ToPosition(Player.transform.position);
                if (Player.transform.position.y < -10) Respawn();
                if (Time.time >= nextTargetUpdate)
                {
                    CurrentTarget = FindTarget();
                    nextTargetUpdate = Time.time + .08f;
                }
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.eKey.wasPressedThisFrame && CurrentTarget != null &&
                        Vector3.Distance(Player.transform.position, CurrentTarget.transform.position) <= 4.5f)
                        CurrentTarget.Interact();
                    if (keyboard.tabKey.wasPressedThisFrame) TryDebug(DebugOperation.Scan, CurrentTarget);
                    if (keyboard.digit1Key.wasPressedThisFrame) TryDebug(DebugOperation.Patch, CurrentTarget);
                    if (keyboard.digit2Key.wasPressedThisFrame) TryDebug(DebugOperation.Override, CurrentTarget);
                    if (keyboard.f5Key.wasPressedThisFrame) SaveGame();
                }
                if (Time.time >= nextAutoSave)
                { SaveGame(false); nextAutoSave = Time.time + 60; }
            }
        }

        private WorldInteractable FindTarget()
        {
            WorldInteractable best = null;
            float bestScore = float.MaxValue;
            float range = debugSettings.range + State.upgrades * 3;
            foreach (var target in targets)
            {
                if (target == null || !target.gameObject.activeInHierarchy || target.collected) continue;
                Vector3 delta = target.transform.position - Player.transform.position;
                float distance = delta.magnitude;
                if (distance > Mathf.Max(4.5f, range)) continue;
                if (!target.debugTarget && distance > 4.5f) continue;
                Vector3 look = target.transform.position + Vector3.up - gameCamera.transform.position;
                float dot = Vector3.Dot(gameCamera.transform.forward, look.normalized);
                if (dot < .68f) continue;
                float score = distance * .18f + (1 - dot) * 20;
                if (score < bestScore) { best = target; bestScore = score; }
            }
            return best;
        }

        public bool CanUseTarget(WorldInteractable target)
        {
            if (target == null || State == null || target.collected) return false;
            if (target.GetComponent<EnemyAgent>() != null) return false;
            return StoryRules.CanUseTarget(State, Quests, target.id);
        }

        public void Interact(WorldInteractable target)
        {
            if (!CanAcceptInput || !CanUseTarget(target) || Player == null ||
                Vector3.Distance(Player.transform.position, target.transform.position) > 4.5f) return;
            switch (target.kind)
            {
                case InteractableKind.Dialogue:
                    if (!string.IsNullOrEmpty(target.sideQuestId)) Quests.Activate(target.sideQuestId);
                    OpenDialogue(string.IsNullOrEmpty(target.npcId) ? target.id : target.npcId);
                    break;
                case InteractableKind.Checkpoint:
                    State.checkpoint = ToPosition(Player.transform.position);
                    State.health = 100;
                    State.energy = 100;
                    SaveGame(false);
                    audioDirector.Play("Energy");
                    ShowMessage("Borne de reprise enregistrée. Santé et énergie restaurées.");
                    break;
                case InteractableKind.EndingRestore:
                case InteractableKind.EndingLiberate:
                    PresentFinalChoice();
                    break;
                case InteractableKind.Collectible:
                    State.flags.Add("collected:" + target.id);
                    target.collected = true;
                    Record(string.IsNullOrEmpty(target.eventId) ? "interact:" + target.id : target.eventId);
                    target.gameObject.SetActive(false);
                    audioDirector.Play("Quest");
                    ShowMessage(target.title + " récupéré.");
                    SaveGame(false);
                    break;
                default:
                    OpenTerminal(target);
                    break;
            }
        }

        private void OpenTerminal(WorldInteractable target)
        {
            dialogueScript = null;
            dialogueNpc = null;
            CurrentDialogue = new DialogueView { speaker = target.title, text = TerminalNarrative.Text(target.id) };
            CurrentDialogue.choices.Add(new DialogueOption("Enregistrer cette découverte", () =>
            {
                Record(string.IsNullOrEmpty(target.eventId) ? "interact:" + target.id : target.eventId);
                CloseDialogue();
            }));
            CurrentDialogue.choices.Add(new DialogueOption("Fermer", CloseDialogue));
            RefreshControlState();
        }

        public void OpenDialogue(string npcId)
        {
            dialogueNpc = npcId;
            string context = Quests.CurrentMain == null ? "" : Quests.CurrentMain.id;
            var npc = Array.Find(targets, value => value != null && value.npcId == npcId);
            if (npc != null && !string.IsNullOrEmpty(npc.sideQuestId))
            {
                var side = Quests.Definitions.Find(value => value.id == npc.sideQuestId);
                var progress = Quests.GetProgress(npc.sideQuestId);
                if (side != null && progress != null)
                {
                    if (progress.status == QuestStatus.Completed) context = side.id + ":completed";
                    else if (progress.status == QuestStatus.Active && progress.stepIndex == side.steps.Count - 1)
                        context = side.id + ":return";
                    else context = side.id;
                }
            }
            dialogueScript = DialogueCatalog.Find(npcId, context);
            if (dialogueScript == null || dialogueScript.pages.Count == 0)
            {
                CurrentDialogue = new DialogueView { speaker = npcId, text = "Le vent du port emporte les mots. Santa Riva n'a pas encore dit son dernier mot." };
                CurrentDialogue.choices.Add(new DialogueOption("À bientôt", () => { CompleteConversation(); }));
                RefreshControlState();
                return;
            }
            ShowDialoguePage(0);
        }

        private void ShowDialoguePage(int index)
        {
            if (dialogueScript == null || index < 0 || index >= dialogueScript.pages.Count)
            { CompleteConversation(); return; }
            var page = dialogueScript.pages[index];
            CurrentDialogue = new DialogueView { speaker = dialogueScript.speakerName, text = page.text };
            foreach (var answer in page.answers)
            {
                var captured = answer;
                CurrentDialogue.choices.Add(new DialogueOption(answer.label, () =>
                {
                    if (!string.IsNullOrEmpty(captured.flag) && !State.flags.Contains(captured.flag)) State.flags.Add(captured.flag);
                    if (captured.nextPage >= 0) ShowDialoguePage(captured.nextPage);
                    else if (captured.completesConversation) CompleteConversation();
                    else CloseDialogue();
                }));
            }
            if (CurrentDialogue.choices.Count == 0)
                CurrentDialogue.choices.Add(new DialogueOption("Continuer", CompleteConversation));
            RefreshControlState();
        }

        private void CompleteConversation()
        {
            if (dialogueNpc == "brune" && !State.flags.Contains("tool:unlocked"))
            { State.flags.Add("tool:unlocked"); ShowMessage("Debug Tool reçu. Tab : SCAN, 1 : PATCH, 2 : OVERRIDE."); }
            Record("talk:" + dialogueNpc);
            CloseDialogue();
            SaveGame(false);
        }

        public void ChooseDialogue(int index)
        {
            if (CurrentDialogue == null || index < 0 || index >= CurrentDialogue.choices.Count) return;
            var choice = CurrentDialogue.choices[index];
            audioDirector.Play("UI");
            choice.action?.Invoke();
        }

        public void CloseDialogue()
        { CurrentDialogue = null; dialogueScript = null; dialogueNpc = null; RefreshControlState(); }

        public void SkipDialogue() { if (CanSkipDialogue) CompleteConversation(); else CloseDialogue(); }

        private void PresentFinalChoice()
        {
            if (!StateRules.CanChooseEnding(State)) return;
            dialogueScript = null;
            dialogueNpc = null;
            CurrentDialogue = new DialogueView
            {
                speaker = "L'Archiviste — dernier protocole",
                text = "Vous avez brisé mon verrou, Nico. Je peux restaurer une ville stable : chaque souvenir sera conservé, chaque avenir validé. Ou vous pouvez libérer le système. Des erreurs reviendront. Des vies aussi. Sillage m'a donné son ancre ; il vous a confié la décision. Quel avenir choisissez-vous ?"
            };
            CurrentDialogue.choices.Add(new DialogueOption("Restaurer Santa Riva sous le contrôle de l'Archiviste", () => SetEnding("restore")));
            CurrentDialogue.choices.Add(new DialogueOption("Libérer Santa Riva et accepter l'inconnu", () => SetEnding("liberate")));
            CurrentDialogue.choices.Add(new DialogueOption("Je veux encore réfléchir", CloseDialogue));
            RefreshControlState();
        }

        public void SetEnding(string ending)
        {
            if ((ending != "restore" && ending != "liberate") || !StateRules.CanChooseEnding(State) ||
                !StoryRules.CanUseTarget(State, Quests, "ending_restore")) return;
            State.ending = ending;
            Record("interact:ending");
            CurrentDialogue = null;
            dialogueScript = null;
            dialogueNpc = null;
            SaveGame(false);
            audioDirector.Play("Quest");
            RefreshControlState();
        }

        public void Record(string eventId)
        {
            if (Quests == null || string.IsNullOrEmpty(eventId)) return;
            var completed = Quests.Record(eventId);
            foreach (string id in completed)
            {
                var definition = Quests.Definitions.Find(value => value.id == id);
                ShowMessage("Mission terminée : " + (definition == null ? id : definition.title));
                audioDirector.Play("Quest");
            }
            if (eventId == "interact:fort_anchor")
            {
                var captain = Array.Find(targets, value => value != null && value.npcId == "sillage");
                if (captain != null) captain.gameObject.SetActive(false);
                ShowMessage("L'ancre de Sillage ouvre le noyau. Sa promesse vous accompagne.");
            }
            if (completed.Count > 0 && Player != null) SaveGame(false);
        }

        public bool TrySpendEnergy(float amount) { return StateRules.TrySpendEnergy(State, amount); }

        public bool IsDebugActive(string id)
        { return debugEffects.TryGetValue(id, out float until) && Time.time < until; }

        public bool TryDebug(DebugOperation operation, WorldInteractable target)
        {
            if (!CanAcceptInput) return false;
            if (!State.flags.Contains("tool:unlocked"))
            { ShowMessage("Parlez à Brune pour recevoir le Debug Tool."); return false; }
            if (operation == DebugOperation.Scan)
            {
                if (Time.time - lastScan < .8f) return false;
                if (!TrySpendEnergy(debugSettings.scanCost)) { ShowMessage("Énergie insuffisante. Rejoignez une borne de reprise."); return false; }
                lastScan = Time.time;
                int revealed = 0;
                foreach (var item in targets)
                {
                    if (item == null || !item.gameObject.activeInHierarchy || !item.debugTarget) continue;
                    if (Vector3.Distance(Player.transform.position, item.transform.position) > debugSettings.range + State.upgrades * 3) continue;
                    item.Reveal(debugSettings.scanDuration);
                    Record("scan:" + item.id);
                    revealed++;
                }
                audioDirector.Play("Scan");
                ShowMessage(revealed + " cible(s) identifiée(s). Visez une cible puis 1 : PATCH ou 2 : OVERRIDE.");
                return true;
            }
            if (target == null || !target.Accepts(operation))
            { ShowMessage("Visez une cible compatible avec " + operation.ToString().ToUpperInvariant() + "."); return false; }
            if (Vector3.Distance(Player.transform.position, target.transform.position) > debugSettings.range + State.upgrades * 3)
            { ShowMessage("Cible hors de portée."); return false; }
            if (target.id == "final_core" && !StoryRules.CanUseTarget(State, Quests, target.id))
            { ShowMessage("Rassemblez les fragments et accomplissez la promesse de Sillage pour accéder au noyau."); return false; }
            if (IsDebugActive(target.id)) { ShowMessage("Cette opération est déjà active."); return false; }
            float cost = (operation == DebugOperation.Patch ? debugSettings.patchCost : debugSettings.overrideCost) * (1 - .08f * State.upgrades);
            if (!TrySpendEnergy(cost)) { ShowMessage("Énergie insuffisante. Une borne restaure votre réserve."); return false; }
            float seconds = debugSettings.effectDuration + State.upgrades * 4;
            target.ApplyEffect(operation, seconds);
            debugEffects[target.id] = Time.time + seconds;
            var enemy = target.GetComponent<EnemyAgent>();
            if (enemy != null) enemy.SetDisabled(seconds);
            Record((operation == DebugOperation.Patch ? "patch:" : "override:") + target.id);
            audioDirector.Play(operation == DebugOperation.Patch ? "Patch" : "Override");
            ShowMessage(target.title + " : " + operation.ToString().ToUpperInvariant() + " pendant " + Mathf.RoundToInt(seconds) + " s.");
            return true;
        }

        public void DamagePlayer(float amount)
        {
            if (!CanAcceptInput || Player == null || Player.IsInvulnerable || amount <= 0) return;
            State.health = Mathf.Max(0, State.health - amount);
            Player.NotifyDamaged();
            audioDirector.Play("Impact");
            if (State.health <= 0) { audioDirector.Play("Death"); Respawn(); }
        }

        public void Respawn()
        {
            State.health = 100;
            State.energy = 100;
            State.position = State.checkpoint.Copy();
            if (Player != null) Player.RecoverAt(ToVector(State.position));
            ShowMessage("Reprise à la dernière borne. Votre progression est conservée.");
        }

        public void SaveGame() { SaveGame(true); }

        private void SaveGame(bool notify)
        {
            if (!IsPlaying || State == null) return;
            try
            {
                if (Player != null) State.position = ToPosition(Player.transform.position);
                StateRules.Normalize(State);
                saves.Save(State);
                ErrorMessage = string.Empty;
                if (notify) ShowMessage("Progression sauvegardée.");
            }
            catch (Exception error)
            { ErrorMessage = "Sauvegarde impossible : " + error.Message; ShowMessage(ErrorMessage); Debug.LogException(error); }
        }

        public void ReturnToMenu()
        {
            if (IsPlaying) SaveGame(false);
            IsPlaying = false;
            IsPaused = false;
            CurrentDialogue = null;
            dialogueScript = null;
            dialogueNpc = null;
            if (World != null) World.gameObject.SetActive(false);
            if (Player != null) Player.gameObject.SetActive(false);
            var follow = gameCamera.GetComponent<ThirdPersonCamera>();
            if (follow != null) follow.enabled = false;
            RefreshControlState();
        }

        public void Pause(bool paused)
        { IsPaused = paused; RefreshControlState(); }

        private void RefreshControlState()
        {
            bool controls = IsPlaying && !IsPaused && !IsDialogueOpen && string.IsNullOrEmpty(State.ending);
            Time.timeScale = controls || !IsPlaying ? 1 : 0;
            SetCursor(controls);
        }

        private static void SetCursor(bool locked)
        { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }

        public void ShowMessage(string message)
        { if (UI != null) UI.Notify(message); }

        public void PlaySound(string name) { if (audioDirector != null) audioDirector.Play(name); }

        public void ApplyAudioSettings(float music, float effects)
        {
            musicLevel = Mathf.Clamp01(music);
            effectsLevel = Mathf.Clamp01(effects);
            if (audioDirector == null) return;
            audioDirector.SetLevels(musicLevel, effectsLevel);
            foreach (var source in FindObjectsByType<AudioSource>())
            {
                if (audioDirector.Owns(source)) continue;
                if (!baseSoundVolumes.TryGetValue(source, out float original))
                { original = source.volume; baseSoundVolumes[source] = original; }
                source.volume = original * effectsLevel;
            }
        }

        private void OnApplicationPause(bool paused) { if (paused && IsPlaying) SaveGame(false); }
        private void OnApplicationFocus(bool focused)
        {
            if (!Application.isBatchMode && !focused && CanAcceptInput)
            { Pause(true); ShowMessage("Partie mise en pause lorsque la fenêtre perd le focus."); }
        }
        private void OnApplicationQuit() { if (IsPlaying) SaveGame(false); }
        private void OnDestroy() { if (Instance == this) { Instance = null; Time.timeScale = 1; SetCursor(false); } }
        private static Vector3 ToVector(PositionData p) { return new Vector3(p.x, p.y, p.z); }
        private static PositionData ToPosition(Vector3 p) { return new PositionData(p.x, p.y, p.z); }
    }
}
