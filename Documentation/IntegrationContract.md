# Contrat d'intégration de RIVE 404

État du code livré le 11 octobre 2026. Projet Unity 6 **6000.0.40f1**, C#, URP. Ce document décrit les interfaces implémentées ; les validations exécutées et restantes sont indiquées dans `Tests.md`.

## Assemblages

| Assemblage | Rôle et références |
|---|---|
| `Rive404.Domain` | Règles et contenu C# sans Unity ; `noEngineReferences: true` |
| `Rive404.Game` | Runtime, joueur, IA, monde, interface et audio ; références `Rive404.Domain` et `Unity.InputSystem` |
| `Rive404.Editor` | Préparation, import FBX, validation et build ; éditeur uniquement, références Game/Domain/Core URP/Universal URP |
| `Rive404.EditModeTests` | Tests NUnit de campagne, récit et stockage avec JsonUtility ; éditeur uniquement |
| `Rive404.PlayModeTests` | Test Bootstrap, monde, checkpoint et boss avec sauvegardes temporaires |

Les espaces de noms `Rive404.Runtime`, `Rive404.Player`, `Rive404.World` et `Rive404.UI` partagent `Rive404.Game`. Ils ne correspondent pas à quatre assemblages distincts : cette organisation évite leurs dépendances circulaires.

## Domaine et progression

Les données publiques sérialisables utilisent des champs compatibles avec `JsonUtility` :

- `PositionData` : `float x, y, z`, constructeurs vide/coordonnées et `Copy()`.
- `GameState` : `int version=1`, `PositionData position, checkpoint`, `float health=100, energy=100`, `int upgrades`, `double playSeconds`, `string ending`, `List<string> flags`, `List<QuestProgress> quests`.
- `QuestProgress` : `string id`, `QuestStatus status`, `int stepIndex`.
- `QuestStatus` : `NotStarted`, `Active`, `Completed`, `Failed`.
- `QuestDefinition` : `string id, title, description`, `bool isMain`, `int order`, `List<QuestStep> steps`.
- `QuestStep` : `string eventId, description, targetId`.

`Campaign.CreateDefinitions()` fournit neuf missions principales et cinq secondaires. `QuestEngine(GameState, List<QuestDefinition>)` expose `Definitions`, `CurrentMain`, `GetProgress(string)`, `Objective(QuestDefinition)`, `Activate(string)` et `Record(string)`. `Record` renvoie les identifiants des missions nouvellement terminées. Une principale suivante s'active automatiquement ; une secondaire démarre par interaction avec son PNJ. Les récompenses sont idempotentes et les trois améliorations suivent `premier_fragment`, `laboratoire` et `heritage_rouge`.

Les événements persistants de collecte, interaction, combat et Debug Tool sont rejoués lorsque leur objectif devient atteignable. Les événements `talk:<npcId>` nécessitent une conversation fraîche : un même appel ne consomme pas deux conversations successives ni un chapitre activé par cet appel. Aucun timer ne termine une mission ni un combat.

`StateRules` fournit `Migrate(GameState)`, `Normalize(GameState)` (`Normalise` est un alias), `Validate(GameState)`, `ValidateDefinitions(IList<QuestDefinition>)`, `TrySpendEnergy(GameState,float)`, `RestoreEnergy(GameState,float)`, `CanChooseEnding(GameState)` et `IsFinite(float)`. Les versions de sauvegarde futures sont refusées. Les positions sous le sol sont assainies avant reprise.

`StoryRules.CanUseTarget(GameState,QuestEngine,string)` garde les actions irréversibles : fragments seulement à l'objectif courant ; ancre après conversation de Sillage, SCAN et OVERRIDE du relais ; noyau seulement dans `dernier_commit` avec trois améliorations ; fins seulement à l'objectif final avec fragments, OVERRIDE enregistré et boss vaincu.

`DialogueCatalog.Find(string npcId,string context)` fournit les conversations écrites des 12 PNJ. `DialogueScript` contient `speakerId`, `speakerName`, `questId` et `pages`; chaque `DialoguePage` contient `text` et `answers`; chaque `DialogueAnswer` contient `label`, `nextPage`, `completesConversation` et `flag`. Les contextes `sacrifice_sillage`, `premier_fragment` et suffixes `:return`/`:completed` sélectionnent les variantes et retours narratifs.

## Session, interactions et sauvegarde

`GameRuntime.Instance` possède la session. Ses propriétés sont `State`, `Quests`, `Player`, `World`, `UI`, `CurrentTarget`, `CurrentDialogue`, `IsPlaying`, `IsPaused`, `IsDialogueOpen`, `CanSkipDialogue`, `CanAcceptInput`, `HasSave` et `ErrorMessage`.

API publique :

- Session : `StartNewGame()`, `ContinueGame()`, `LoadGame()` (`bool`), `SaveGame()`, `ReturnToMenu()`, `Pause(bool)`.
- Progression : `Record(string)`, `CanUseTarget(WorldInteractable)` (`bool`), `Interact(WorldInteractable)`, `SetEnding(string)`.
- Dialogue : `OpenDialogue(string)`, `ChooseDialogue(int)`, `CloseDialogue()`, `SkipDialogue()`.
- Gameplay : `TryDebug(DebugOperation,WorldInteractable)` (`bool`), `IsDebugActive(string)` (`bool`), `TrySpendEnergy(float)` (`bool`), `DamagePlayer(float)`, `Respawn()`.
- Retours : `ShowMessage(string)`, `PlaySound(string)`, `ApplyAudioSettings(float music,float effects)`.

Le monde et le joueur sont recréés lors d'une session ; l'interface observe le nouvel état et remet ses panneaux à zéro. Pause, dialogue et fin suspendent le contrôle. La perte de focus en mode interactif met la partie en pause. Le champ statique `SaveDirectoryOverride` permet aux tests PlayMode d'isoler leur sauvegarde ; par défaut le stockage utilise `Application.persistentDataPath`.

`SaveStore` reste indépendant de Unity. Son constructeur reçoit le dossier et quatre délégués de sérialisation/désérialisation de `GameState` et `SaveEnvelope`. Il expose `Path`, `HasSave`, `Save(GameState)` et `TryLoad(out GameState,out string message)`. L'enveloppe contient `format=1`, `payload` et `sha256`. Écriture temporaire, flush disque, remplacement et copie `.bak` préservent une version valide ; le chargement contrôle version et checksum, et annonce une restauration depuis le secours. Le runtime injecte `JsonUtility`; les tests console injectent `System.Text.Json`.

`DialogueView` contient `speaker`, `text` et `List<DialogueOption> choices`. Chaque option contient `label` et `System.Action action`.

`WorldInteractable : MonoBehaviour, IInteractable, IDebugTarget` porte `id`, `title`, `eventId`, `npcId`, `sideQuestId`, `kind`, `debugTarget`, `operation`, `controlledObject`, `controlledEnabledOnPatch=true`, `duration=24` et `collected`. Il expose `CanInteract`, `Interact()`, `Accepts(DebugOperation)`, `RefreshFromState()`, `Reveal(float)` et `ApplyEffect(DebugOperation,float)`.

`InteractableKind` : `Dialogue`, `Terminal`, `Collectible`, `Checkpoint`, `EndingRestore`, `EndingLiberate`. `DebugOperation` : `Scan`, `Patch`, `Override`. PATCH/OVERRIDE expirent et restaurent l'état initial de l'objet contrôlé. Le champ `duration` de la cible n'est pas la source du délai runtime : `DebugToolSettings.effectDuration` et les améliorations le déterminent.

Les flags persistants comprennent `collected:<id>`, `scan:<id>`, `patch:<id>`, `override:<id>`, `interact:<id>`, `kill:<enemyId>`, `upgrade:<questId>`, `tool:unlocked` et `visited:<district>`. Un flag d'OVERRIDE enregistré prouve une action passée ; il ne prouve pas qu'un effet temporaire est encore actif.

## Configurations et rendu

`QuestCatalogAsset : ScriptableObject` contient `List<QuestDefinition> definitions`; le runtime charge `Resources/Campaign` avec fallback vers `Campaign.CreateDefinitions()`.

`DebugToolSettings : ScriptableObject` contient `scanCost=8`, `patchCost=20`, `overrideCost=30`, `range=18`, `scanDuration=8` et `effectDuration=24`. Le runtime charge `Resources/DebugToolSettings`, avec valeurs par défaut si l'asset manque.

`ModelFactory.Attach(Transform,string resourceName,Vector3 localScale)` charge un FBX sous `Resources/Models`, instancie son visuel et convertit ses matériaux en URP/Lit en conservant la couleur. Le retour est un `GameObject` ou `null`; les composants utilisent alors leurs primitives de secours.

Les quatre modèles `Nico`, `Sillage`, `NPC` et `Corrupted` utilisent un import **Generic** et les clips Idle/Walk. `LocomotionAnimation.Attach(Transform,string)` crée un graphe Playables et son mélange Idle/Walk sans AnimatorController ; `SetSpeed(float)` adapte ce mélange. Les clips Legacy sont incompatibles avec ce chemin et sont ignorés.

## Joueur, caméra et IA

`PlayerController.Init(GameRuntime)` reçoit le runtime après création du CharacterController et du visuel. API : `TryAttack(bool heavy)`, `NotifyDamaged(float invulnerabilitySeconds=0.7f)`, `RecoverAt(Vector3)`, `Velocity`, `AimPosition`, `IsInvulnerable`, `IsAttacking`, `LastAttackHeavy`. Un seul CharacterController possède le déplacement. Touches physiques Input System : WASD/ZQSD, flèches, Maj sprint, Espace saut, C accroupissement, Alt gauche esquive, boutons souris attaques.

`ThirdPersonCamera.Init(Transform)` configure le suivi de `Camera.main`, la rotation souris et la collision. Le champ public `Sensitivity` est appliqué par les préférences ; la caméra se repositionne immédiatement après téléportation.

`EnemyKind` : `Drone`, `Sentinel`, `Corrupted`, `Boss`. `EnemyAgent` expose `Id`, `Kind`, `Configure(GameRuntime,string,EnemyKind)`, `TakeDamage(float)`, `SetDisabled(float)`, `MaximumHealth`, `Health`, `HealthRatio`, `IsDead`, `Phase`, `State`, `StateLabel` et `IsShielded`.

L'IA utilise NavMesh lorsqu'une surface est disponible, sinon son CharacterController avec collisions. États : patrouille, détection, poursuite, préparation d'attaque, récupération, retour, neutralisation et mort. Les attaques ont un délai lisible, une portée et une vérification d'obstacle. La mort enregistre `kill:<id>` et une sauvegarde restaure les ennemis déjà vaincus comme inactifs.

**Le bouclier du boss dépend de `GameRuntime.IsDebugActive("final_core")`, pas du flag historique `override:final_core`.** Il revient après expiration ; le joueur peut relancer OVERRIDE. Les phases suivent la santé restante.

## Monde et carte

`WorldBuilder.Build(GameRuntime)` construit la carte ; `ResetWorld()` nettoie ses données. `SpawnPoint` vaut `(-340,1.1,-300)`. `PositionOf(string)` retourne une position connue ou le spawn. `Entries` est une `List<MapEntry>` ; chaque entrée contient `id`, `label`, `Vector3 position` et `int district`.

Le sol brut mesure 1000 × 1000 m ; les bâtiments et obstacles doivent être déduits lors de la mesure réelle de surface explorable. Trois quartiers reliés contiennent 12 PNJ, neuf missions principales, cinq secondaires, six intérieurs, sept ennemis dont un boss, et trois checkpoints. Les décors dorment par cellules de distance ; les objets de campagne restent résidents. Le runtime tente un bake `NavMeshSurface` par réflexion et conserve le déplacement de secours si indisponible.

Les IDs des cibles sont ceux de `Campaign`. Les modèles utilisés sont `Nico`, `Sillage`, `NPC`, `Drone`, `Sentinel`, `Corrupted`, `Boss`, `House`, `Warehouse`, `Tower`, `Tree`, `Lamp`, `Terminal`, `Crate`, `Boat` et `Fort`. Le déplacement de Sillage vers le fort puis sa disparition après l'ancre sont restaurés depuis la progression.

`WorldBuilder` persiste `visited:0`, `visited:1`, `visited:2` à l'entrée dans les quartiers. La carte masque les lieux et noms des quartiers non visités ; l'objectif de mission reste repérable pour guider la découverte, sans révéler leurs autres points d'intérêt.

## Interface et audio

`GameUI.Bind(GameRuntime)` et `Notify(string)` relient les menus français, HUD, journal, carte, dialogues, pause, options, sauvegarde locale, crédits et deux fins. L'interface OnGUI ne crée pas le monde et ne modifie pas directement la progression. Journal et carte utilisent `Pause(bool)` ; les marqueurs passent par `World.PositionOf(targetId)`. Le bouton Passer appelle `SkipDialogue()` pour terminer la conversation courante.

`GameSettings` stocke séparément dans PlayerPrefs volume général, musique/ambiance, effets, sensibilité, qualité, résolution, plein écran, VSync, limite FPS et profil d'aide AZERTY/QWERTY. API : `Load()`, `Save()`, `Apply(bool applyDisplay)`, `ApplyCamera()` et `HasSavedDisplay`. Le profil modifie l'aide ; les déplacements utilisent la position physique des touches.

`AudioDirector.Initialize()`, `SetDistrict(int)`, `SetLevels(float,float)`, `Owns(AudioSource)` et `Play(string)` contrôlent thème, ambiances et effets depuis `Resources/Audio`. `GameRuntime.ApplyAudioSettings` applique aussi le volume des effets aux sources du joueur et des ennemis.

## Éditeur, outils et limites de validation

`Rive404ProjectSetup.Prepare()` génère renderer, assets URP Bas/Standard/Élevé, Global Settings et ressources, catalogues de configuration, inclusion des shaders procéduraux et scène de build. `ValidateProject()` contrôle le catalogue et Bootstrap. `BuildWindows()` prépare puis construit `Builds/Windows/RIVE404.exe` avec le module Windows Mono.

Les GUID des assets existants sont déterministes : 32 premiers caractères du SHA-256 du chemin relatif à `UnityProject`, commençant par `Assets/`. Bootstrap référence `Assets/_RIVE404/Scripts/Runtime/GameRuntime.cs` avec le GUID `676c417c318a6954b28d1e5aa45b0a83`. Unity génère lui-même les fichiers de configuration et leur verrouillage lors d'un véritable import.

Les outils .NET compilent les règles et `SaveStore` sans package NuGet ; Roslyn vérifie la syntaxe C# 9 sans prétendre compiler Unity. Les `.meta`, assemblages et pins sont contrôlés séparément. L'import Unity, les tests NUnit/PlayMode, la build Windows, les trois heures de campagne et les performances 1080p/60 FPS restent à valider dans les conditions décrites dans `Tests.md`.
