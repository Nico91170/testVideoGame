using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Rive404.Domain;
using Rive404.Runtime;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { IncludeFields = true };
    private static int passed;
    private static int failed;

    private static int Main()
    {
        Run("catalogue : 9 principales, 5 secondaires, objectifs valides", Catalog);
        Run("campagne complète : progression réelle et 3 améliorations", CampaignCompletion);
        Run("événements sans rapport : aucun avancement", UnrelatedEvents);
        Run("combat précoce : aucune mission irrémédiablement bloquée", EarlyCombat);
        Run("dialogue : une action ne consomme pas deux réponses", FreshConversation);
        Run("secondaire : collecte préalable puis deux conversations", SideQuestEarlyCollection);
        Run("récompenses : événements répétés idempotents", IdempotentRewards);
        Run("reprise : objectifs et principale active conservés", RestoreQuestProgress);
        Run("normalisation : valeurs invalides et doublons assainis", NormalizeCorruption);
        Run("checkpoint : migration v0 copie la position sans alias", CheckpointMigration);
        Run("checkpoint : position sous le sol corrigée avant reprise", CheckpointBelowGround);
        Run("sauvegarde : version future refusée", FutureVersion);
        Run("énergie : coût réel, refus sans mutation et recharge plafonnée", Energy);
        Run("fins : trois fragments, noyau et boss obligatoires", EndingRequirements);
        Run("récit : sacrifice du capitaine obligatoire avant ancre et noyau", StoryPrerequisites);
        Run("récit : drapeaux de fin ne permettent pas de sauter la campagne", EndingRequiresFinalObjective);
        Run("récit : fragments uniquement à l'objectif attendu", FragmentRequiresCurrentObjective);
        Run("définitions : identifiants et ordres en double détectés", InvalidDefinitions);
        Run("dialogues : 12 PNJ, variantes et retours avec sorties accessibles", DialogueGraphs);
        Run("disque : sauvegarde/reprise position, checkpoint et quêtes", SaveRoundTrip);
        Run("disque : JSON UTF-8 et choix final conservés", SaveEndingRoundTrip);
        Run("disque : sauvegarde initialement absente signalée", MissingSave);
        Run("disque : fichier principal corrompu, secours restauré", BackupRecovery);
        Run("disque : corruption sans secours refusée explicitement", CorruptionWithoutBackup);
        Run("disque : altération du contenu détectée par checksum", ChecksumRejectsTampering);
        Run("disque : version future ne remplace pas un état valide", InvalidSavePreservesPrevious);
        Console.WriteLine("Résultat : " + passed + " réussis, " + failed + " échoués.");
        return failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failed++; Console.Error.WriteLine("FAIL " + name + ": " + ex.GetType().Name + " " + ex.Message); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Catalog()
    {
        var definitions = Campaign.CreateDefinitions();
        Check(definitions.Count(q => q.isMain) == 9, "9 principales requises.");
        Check(definitions.Count(q => !q.isMain) == 5, "5 secondaires requises.");
        Check(StateRules.ValidateDefinitions(definitions).Count == 0, "Catalogue invalide.");
        Check(definitions.SelectMany(q => q.steps).All(s => !string.IsNullOrWhiteSpace(s.description)), "Objectif non décrit.");
    }

    private static void CampaignCompletion()
    {
        var state = new GameState();
        var engine = new QuestEngine(state, Campaign.CreateDefinitions());
        int actions = 0;
        var completed = new HashSet<string>();
        while (engine.CurrentMain != null)
        {
            Check(++actions < 150, "Boucle ou mission bloquée.");
            var step = engine.Objective(engine.CurrentMain);
            Check(step != null, "Mission active sans objectif.");
            foreach (string id in engine.Record(step.eventId)) completed.Add(id);
        }
        Check(completed.Count == 9, "Campagne incomplète.");
        Check(state.upgrades == 3, "Les 3 améliorations doivent être accessibles.");
        Check(state.quests.Count(q => q.status == QuestStatus.Completed) == 9, "Nombre de missions terminées incorrect.");
        Check(StateRules.CanChooseEnding(state), "Campagne achevée sans prérequis de fin.");
    }

    private static void UnrelatedEvents()
    {
        var state = new GameState();
        var engine = new QuestEngine(state, Campaign.CreateDefinitions());
        var id = engine.CurrentMain.id;
        for (int i = 0; i < 20; i++) Check(engine.Record("unrelated:noise").Count == 0, "Récompense inattendue.");
        Check(engine.CurrentMain.id == id && engine.GetProgress(id).stepIndex == 0, "Événement inconnu a fait progresser la mission.");
    }

    private static void EarlyCombat()
    {
        var state = new GameState();
        var engine = new QuestEngine(state, new List<QuestDefinition>
        {
            Quest("first", 0, "talk:guide"), Quest("second", 1, "kill:drone")
        });
        engine.Record("kill:drone");
        Check(engine.CurrentMain.id == "first", "Le combat a sauté un dialogue.");
        engine.Record("talk:guide");
        Check(engine.GetProgress("second").status == QuestStatus.Completed, "Combat précoce oublié.");
        Check(engine.Record("kill:drone").Count == 0, "Combat dupliqué récompensé.");
    }

    private static void FreshConversation()
    {
        var engine = new QuestEngine(new GameState(), new List<QuestDefinition> { Quest("talk", 0, "talk:guide", "talk:guide") });
        engine.Record("talk:guide");
        Check(engine.GetProgress("talk").stepIndex == 1, "Une seule conversation a consommé plusieurs réponses.");
        Check(engine.GetProgress("talk").status == QuestStatus.Active, "Mission terminée trop tôt.");
        engine.Record("talk:guide");
        Check(engine.GetProgress("talk").status == QuestStatus.Completed, "Conversation finale inaccessible.");
    }

    private static void SideQuestEarlyCollection()
    {
        var engine = new QuestEngine(new GameState(), Campaign.CreateDefinitions());
        engine.Record("interact:lost_cargo");
        Check(engine.Activate("side_lost_cargo"), "Secondaire non activable.");
        Check(!engine.Activate("side_lost_cargo"), "Activation répétée acceptée.");
        engine.Record("talk:amir");
        Check(engine.GetProgress("side_lost_cargo").status == QuestStatus.Active, "Dialogue de départ a consommé le retour.");
        engine.Record("talk:amir");
        Check(engine.GetProgress("side_lost_cargo").status == QuestStatus.Completed, "Collecte anticipée a bloqué la secondaire.");
    }

    private static void IdempotentRewards()
    {
        var state = new GameState();
        var engine = new QuestEngine(state, Campaign.CreateDefinitions());
        CompleteMain(engine);
        int upgrades = state.upgrades;
        foreach (var definition in engine.Definitions.Where(q => q.isMain))
        foreach (var step in definition.steps)
            Check(engine.Record(step.eventId).Count == 0, "Mission récompensée deux fois.");
        Check(state.upgrades == upgrades, "Amélioration dupliquée.");
    }

    private static void RestoreQuestProgress()
    {
        var state = new GameState();
        var engine = new QuestEngine(state, Campaign.CreateDefinitions());
        engine.Record(engine.Objective(engine.CurrentMain).eventId);
        string id = engine.CurrentMain.id;
        int index = engine.GetProgress(id).stepIndex;
        var copy = Deserialize<GameState>(Serialize(state));
        var restored = new QuestEngine(copy, Campaign.CreateDefinitions());
        Check(restored.CurrentMain.id == id && restored.GetProgress(id).stepIndex == index, "Progression changée à la reprise.");
        CompleteMain(restored);
        Check(restored.CurrentMain == null, "Reprise non terminable.");
    }

    private static void NormalizeCorruption()
    {
        var state = new GameState
        {
            health = float.NaN, energy = float.PositiveInfinity, upgrades = 999, playSeconds = double.NaN,
            position = new PositionData(float.NaN, 1, 1), checkpoint = null, ending = "unexpected",
            flags = new List<string> { " flag ", "flag", null, "" },
            quests = new List<QuestProgress>
            {
                new QuestProgress { id = " q ", status = (QuestStatus)200, stepIndex = -7 },
                new QuestProgress { id = "q", status = QuestStatus.Completed, stepIndex = 4 }, null
            }
        };
        Check(StateRules.Validate(state).Count > 0, "Corruption non signalée.");
        state = StateRules.Normalize(state);
        Check(StateRules.Validate(state).Count == 0, "État normalisé encore invalide.");
        Check(state.flags.SequenceEqual(new[] { "flag" }), "Drapeaux dupliqués.");
        Check(state.quests.Count == 1 && state.quests[0].status == QuestStatus.Completed, "Complétion perdue lors de fusion.");
        Check(state.position != null && state.checkpoint != null, "Position/checkpoint absent.");
    }

    private static void CheckpointMigration()
    {
        var state = new GameState { version = 0, position = new PositionData(12, 3, -15), checkpoint = null };
        state = StateRules.Normalize(state);
        Check(state.version == StateRules.CurrentVersion, "Version non migrée.");
        Check(state.checkpoint.x == 12 && state.checkpoint.z == -15, "Checkpoint migré ailleurs que joueur.");
        state.position.x = 100;
        Check(state.checkpoint.x == 12, "Checkpoint partage la référence de la position.");
    }

    private static void CheckpointBelowGround()
    {
        var state = new GameState { position = new PositionData(30, -30, 50), checkpoint = new PositionData(32, -5, 54) };
        Check(StateRules.Validate(state).Count > 0, "Position sous le sol non signalée.");
        state = StateRules.Normalize(state);
        Check(state.position.y >= 0.1f && state.checkpoint.y >= 0.1f, "Reprise resterait sous le sol.");
        Check(state.position.x == 30 && state.checkpoint.z == 54, "Correction sol déplace vers un autre checkpoint.");
    }

    private static void FutureVersion()
    {
        bool rejected = false;
        try { StateRules.Normalize(new GameState { version = StateRules.CurrentVersion + 1 }); }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "Version future silently accepted.");
    }

    private static void Energy()
    {
        var state = new GameState { energy = 25 };
        Check(StateRules.TrySpendEnergy(state, 20) && state.energy == 5, "Coût non appliqué.");
        Check(!StateRules.TrySpendEnergy(state, 30) && state.energy == 5, "Échec de dépense a modifié énergie.");
        Check(!StateRules.TrySpendEnergy(state, -10) && !StateRules.TrySpendEnergy(state, float.NaN), "Coût invalide accepté.");
        StateRules.RestoreEnergy(state, 500);
        Check(state.energy == 100, "Recharge dépasse maximum.");
        StateRules.RestoreEnergy(state, -1);
        Check(state.energy == 100, "Recharge négative réduit énergie.");
    }

    private static void EndingRequirements()
    {
        string[] required = { "interact:fragment_port", "interact:fragment_neon", "interact:fragment_cliffs", "override:final_core", "kill:archivist" };
        foreach (string missing in required)
            Check(!StateRules.CanChooseEnding(new GameState { flags = required.Where(f => f != missing).ToList() }), "Fin accessible sans " + missing);
        Check(StateRules.CanChooseEnding(new GameState { flags = required.ToList() }), "Fin inaccessible même avec tous prérequis.");
    }

    private static void InvalidDefinitions()
    {
        var definitions = new List<QuestDefinition> { Quest("same", 1, "talk:x"), Quest("same", 1, "talk:y") };
        Check(StateRules.ValidateDefinitions(definitions).Count >= 2, "Doublons de mission/ordre ignorés.");
    }

    private static void StoryPrerequisites()
    {
        var state = new GameState();
        var engine = new QuestEngine(state, Campaign.CreateDefinitions());
        AdvanceTo(engine, "sacrifice_sillage");
        Check(state.upgrades == 3, "3 améliorations attendues avant sacrifice.");
        Check(!StoryRules.CanUseTarget(state, engine, "fort_anchor"), "Ancre acceptée avant dialogue Sillage.");
        Check(!StoryRules.CanUseTarget(state, engine, "final_core"), "Noyau autorisé avant sacrifice.");
        Check(!StoryRules.CanUseTarget(state, engine, "ending_restore") && !StoryRules.CanUseTarget(state, engine, "ending_liberate"), "Fin autorisée avant sacrifice.");
        engine.Record("talk:sillage");
        Check(!StoryRules.CanUseTarget(state, engine, "fort_anchor"), "Ancre autorisée sans scanner le relais.");
        engine.Record("scan:fort_relay");
        Check(!StoryRules.CanUseTarget(state, engine, "fort_anchor"), "Ancre autorisée sans neutraliser la sécurité.");
        engine.Record("override:fort_relay");
        Check(StoryRules.CanUseTarget(state, engine, "fort_anchor"), "Ancre bloquée après toutes les actions requises.");
        Check(!StoryRules.CanUseTarget(state, engine, "final_core"), "Noyau autorisé avant accepter ancre.");
        engine.Record("interact:fort_anchor");
        Check(StoryRules.CanUseTarget(state, engine, "final_core"), "Noyau bloqué après sacrifice.");
        Check(!StoryRules.CanUseTarget(state, engine, "ending_restore"), "Fin autorisée avant le boss.");
        engine.Record("override:final_core");
        Check(!StoryRules.CanUseTarget(state, engine, "ending_restore"), "Fin autorisée avant victoire boss.");
        engine.Record("kill:archivist");
        Check(StoryRules.CanUseTarget(state, engine, "ending_restore") && StoryRules.CanUseTarget(state, engine, "ending_liberate"), "Un choix final est inaccessible.");
    }

    private static void EndingRequiresFinalObjective()
    {
        var state = new GameState
        {
            upgrades = 3,
            flags = new List<string> { "interact:fragment_port", "interact:fragment_neon", "interact:fragment_cliffs", "override:final_core", "kill:archivist" }
        };
        var engine = new QuestEngine(state, Campaign.CreateDefinitions());
        Check(StateRules.CanChooseEnding(state), "Préconditions matérielles inattendues.");
        Check(!StoryRules.CanUseTarget(state, engine, "ending_restore"), "Drapeaux seuls ont contourné l'histoire.");
        Check(!StoryRules.CanUseTarget(state, engine, "ending_liberate"), "Drapeaux seuls ont contourné l'histoire.");
    }

    private static void FragmentRequiresCurrentObjective()
    {
        var state = new GameState();
        var engine = new QuestEngine(state, Campaign.CreateDefinitions());
        foreach (string id in new[] { "fragment_port", "fragment_neon", "fragment_cliffs" })
            Check(!StoryRules.CanUseTarget(state, engine, id), "Fragment accepté pendant le prologue : " + id);
        AdvanceTo(engine, "premier_fragment");
        Check(!StoryRules.CanUseTarget(state, engine, "fragment_port"), "Fragment accepté sans indications de Mila.");
        while (engine.Objective(engine.CurrentMain).targetId != "fragment_port")
            engine.Record(engine.Objective(engine.CurrentMain).eventId);
        Check(StoryRules.CanUseTarget(state, engine, "fragment_port"), "Fragment courant inaccessible.");
        Check(!StoryRules.CanUseTarget(state, engine, "fragment_neon"), "Fragment suivant accepté trop tôt.");
    }

    private static void AdvanceTo(QuestEngine engine, string mainId)
    {
        int guard = 0;
        while (engine.CurrentMain != null && engine.CurrentMain.id != mainId)
        {
            Check(++guard < 150, "Campagne bloquée avant " + mainId);
            engine.Record(engine.Objective(engine.CurrentMain).eventId);
        }
        Check(engine.CurrentMain != null && engine.CurrentMain.id == mainId, "Mission introuvable : " + mainId);
    }

    private static void DialogueGraphs()
    {
        string[] npcs = { "brune", "sillage", "mila", "jo", "ines", "iris", "orson", "amir", "lucie", "teo", "alma", "gaston" };
        var scripts = npcs.Select(id => DialogueCatalog.Find(id, null)).ToList();
        scripts.Add(DialogueCatalog.Find("sillage", "sacrifice_sillage"));
        scripts.Add(DialogueCatalog.Find("mila", "premier_fragment"));
        foreach (string id in new[] { "amir", "lucie", "teo", "alma", "gaston" })
            scripts.Add(DialogueCatalog.Find(id, "side:return"));
        Check(scripts.Select(s => s.speakerId).Distinct().Count() == 12, "Tous les PNJ n'ont pas de dialogue.");
        foreach (var script in scripts)
        {
            Check(!string.IsNullOrWhiteSpace(script.speakerName) && script.pages.Count > 0, "Script vide : " + script.speakerId);
            for (int page = 0; page < script.pages.Count; page++)
            {
                var value = script.pages[page];
                Check(!string.IsNullOrWhiteSpace(value.text), "Page vide : " + script.speakerId);
                Check(value.answers.Count >= 2 && value.answers.Count <= 3, "Choix hors 2–3 : " + script.speakerId);
                foreach (var answer in value.answers)
                {
                    Check(!string.IsNullOrWhiteSpace(answer.label), "Choix sans texte.");
                    Check(answer.completesConversation ? answer.nextPage == -1 : answer.nextPage >= 0 && answer.nextPage < script.pages.Count,
                        "Lien invalide : " + script.speakerId + " page " + page);
                }
                Check(CanReachExit(script, page, new HashSet<int>()), "Conversation sans sortie : " + script.speakerId + " page " + page);
            }
        }
    }

    private static bool CanReachExit(DialogueScript script, int page, HashSet<int> visited)
    {
        if (!visited.Add(page)) return false;
        foreach (var answer in script.pages[page].answers)
            if (answer.completesConversation || CanReachExit(script, answer.nextPage, visited)) return true;
        return false;
    }

    private static void SaveRoundTrip()
    {
        WithStore(store =>
        {
            var state = new GameState { position = new PositionData(70, 4, 31), checkpoint = new PositionData(-30, 1, 14), energy = 43 };
            var engine = new QuestEngine(state, Campaign.CreateDefinitions());
            engine.Record(engine.Objective(engine.CurrentMain).eventId);
            state.flags.Add("interact:fragment_port");
            store.Save(state);
            Check(store.HasSave, "HasSave faux après écriture.");
            Check(store.TryLoad(out GameState loaded, out string message), message);
            Check(loaded.position.x == 70 && loaded.checkpoint.z == 14 && loaded.energy == 43, "Coordonnées/checkpoint/énergie perdues.");
            Check(loaded.flags.Contains("interact:fragment_port") && loaded.quests[0].stepIndex == state.quests[0].stepIndex, "Progression perdue.");
            Check(!Directory.GetFiles(Path.GetDirectoryName(store.Path)).Any(path => path.EndsWith(".tmp", StringComparison.Ordinal)), "Fichier temporaire laissé après sauvegarde.");
        });
    }

    private static void SaveEndingRoundTrip()
    {
        WithStore(store =>
        {
            var state = new GameState { ending = "liberate", playSeconds = 381, flags = new List<string> { "mémoire:réparée" } };
            store.Save(state);
            Check(store.TryLoad(out GameState loaded, out string message), message);
            Check(loaded.ending == "liberate" && loaded.playSeconds == 381 && loaded.flags.Contains("mémoire:réparée"), "UTF-8 ou fin perdue.");
        });
    }

    private static void MissingSave()
    {
        WithStore(store =>
        {
            Check(!store.HasSave, "Sauvegarde annoncée avant écriture.");
            Check(!store.TryLoad(out GameState loaded, out string message), "Chargement d'un fichier inexistant réussi.");
            Check(loaded == null && !string.IsNullOrWhiteSpace(message), "Absence de sauvegarde non expliquée.");
        });
    }

    private static void BackupRecovery()
    {
        WithStore(store =>
        {
            store.Save(new GameState { energy = 71 });
            store.Save(new GameState { energy = 24 });
            File.WriteAllText(store.Path, "{broken-json");
            Check(store.TryLoad(out GameState loaded, out string message), message);
            Check(loaded.energy == 71, "État précédent non restauré depuis secours.");
            Check(!string.IsNullOrWhiteSpace(message), "Récupération non annoncée.");
        });
    }

    private static void CorruptionWithoutBackup()
    {
        WithStore(store =>
        {
            store.Save(new GameState());
            File.WriteAllText(store.Path, "{broken-json");
            Check(!store.TryLoad(out GameState loaded, out string message), "Corruption acceptée.");
            Check(loaded == null && !string.IsNullOrWhiteSpace(message), "Corruption non expliquée.");
        });
    }

    private static void ChecksumRejectsTampering()
    {
        WithStore(store =>
        {
            store.Save(new GameState());
            var envelope = Deserialize<SaveEnvelope>(File.ReadAllText(store.Path));
            envelope.payload = Serialize(new GameState { energy = 7 });
            File.WriteAllText(store.Path, Serialize(envelope));
            Check(!store.TryLoad(out GameState loaded, out string message), "Contenu modifié accepté sans checksum valide.");
            Check(loaded == null && !string.IsNullOrWhiteSpace(message), "Échec checksum non expliqué.");
        });
    }

    private static void InvalidSavePreservesPrevious()
    {
        WithStore(store =>
        {
            store.Save(new GameState { energy = 64 });
            bool rejected = false;
            try { store.Save(new GameState { version = StateRules.CurrentVersion + 1 }); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Version future sauvegardée.");
            Check(store.TryLoad(out GameState loaded, out string message) && loaded.energy == 64, "Sauvegarde valide écrasée par état invalide : " + message);
        });
    }

    private static QuestDefinition Quest(string id, int order, params string[] events)
    {
        return new QuestDefinition
        {
            id = id, title = id, description = id, isMain = true, order = order,
            steps = events.Select(e => new QuestStep(e, e, e.Substring(e.IndexOf(':') + 1))).ToList()
        };
    }

    private static void CompleteMain(QuestEngine engine)
    {
        int guard = 0;
        while (engine.CurrentMain != null)
        {
            Check(++guard < 150, "Campagne bloquée.");
            engine.Record(engine.Objective(engine.CurrentMain).eventId);
        }
    }

    private static void WithStore(Action<SaveStore> test)
    {
        string folder = Path.Combine(Path.GetTempPath(), "rive404-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SaveStore(folder, Serialize, Deserialize<GameState>, Serialize, Deserialize<SaveEnvelope>);
            test(store);
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    private static string Serialize<T>(T value) { return JsonSerializer.Serialize(value, JsonOptions); }
    private static T Deserialize<T>(string value) { return JsonSerializer.Deserialize<T>(value, JsonOptions); }
}
