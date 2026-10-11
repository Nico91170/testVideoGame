using System;
using System.Collections.Generic;

namespace Rive404.Domain
{
    /// <summary>Save-data safety rules; contains no engine or filesystem dependencies.</summary>
    public static class StateRules
    {
        public const int CurrentVersion = 1;
        public const float MaximumHealth = 100f;
        public const float MaximumEnergy = 100f;
        public const int MaximumUpgrades = 3;

        public static GameState Migrate(GameState state)
        {
            if (state == null) return new GameState();
            if (state.version > CurrentVersion)
                throw new InvalidOperationException("Cette sauvegarde provient d'une version plus récente du jeu.");
            if (state.version < 0)
                throw new InvalidOperationException("Version de sauvegarde invalide.");
            // Version zero was the unversioned prototype. All added fields have safe defaults.
            if (state.version == 0)
            {
                state.version = 1;
                if (state.checkpoint == null && state.position != null)
                    state.checkpoint = state.position.Copy();
            }
            return state;
        }

        public static GameState Normalize(GameState state)
        {
            state = Migrate(state);
            state.position = NormalizePosition(state.position);
            state.checkpoint = NormalizePosition(state.checkpoint);
            state.health = IsFinite(state.health) ? Clamp(state.health, 0f, MaximumHealth) : MaximumHealth;
            state.energy = IsFinite(state.energy) ? Clamp(state.energy, 0f, MaximumEnergy) : MaximumEnergy;
            state.upgrades = Math.Max(0, Math.Min(MaximumUpgrades, state.upgrades));
            if (double.IsNaN(state.playSeconds) || double.IsInfinity(state.playSeconds) || state.playSeconds < 0)
                state.playSeconds = 0;
            if (state.ending != "restore" && state.ending != "liberate") state.ending = string.Empty;
            if (state.flags == null) state.flags = new List<string>();
            var flags = new HashSet<string>(StringComparer.Ordinal);
            var normalizedFlags = new List<string>();
            foreach (string flag in state.flags)
            {
                if (string.IsNullOrWhiteSpace(flag)) continue;
                string clean = flag.Trim();
                if (flags.Add(clean)) normalizedFlags.Add(clean);
            }
            state.flags = normalizedFlags;
            if (state.quests == null) state.quests = new List<QuestProgress>();
            var unique = new Dictionary<string, QuestProgress>(StringComparer.Ordinal);
            var normalizedQuests = new List<QuestProgress>();
            foreach (QuestProgress progress in state.quests)
            {
                if (progress == null || string.IsNullOrWhiteSpace(progress.id)) continue;
                progress.id = progress.id.Trim();
                if (!Enum.IsDefined(typeof(QuestStatus), progress.status)) progress.status = QuestStatus.NotStarted;
                progress.stepIndex = Math.Max(0, progress.stepIndex);
                QuestProgress prior;
                if (!unique.TryGetValue(progress.id, out prior))
                {
                    unique.Add(progress.id, progress);
                    normalizedQuests.Add(progress);
                }
                else if (progress.status == QuestStatus.Completed ||
                         (prior.status != QuestStatus.Completed && progress.stepIndex > prior.stepIndex))
                {
                    prior.status = progress.status;
                    prior.stepIndex = progress.stepIndex;
                }
            }
            state.quests = normalizedQuests;
            return state;
        }

        public static GameState Normalise(GameState state) { return Normalize(state); }

        /// <summary>Reports raw invalid data without changing it, before normalization.</summary>
        public static List<string> Validate(GameState state)
        {
            var errors = new List<string>();
            if (state == null) { errors.Add("Sauvegarde absente."); return errors; }
            if (state.version < 0 || state.version > CurrentVersion) errors.Add("Version de sauvegarde incompatible.");
            if (!IsValidPosition(state.position)) errors.Add("Position du joueur invalide.");
            if (!IsValidPosition(state.checkpoint)) errors.Add("Point de reprise invalide.");
            if (!IsFinite(state.health) || state.health < 0 || state.health > MaximumHealth) errors.Add("Santé invalide.");
            if (!IsFinite(state.energy) || state.energy < 0 || state.energy > MaximumEnergy) errors.Add("Énergie invalide.");
            if (state.upgrades < 0 || state.upgrades > MaximumUpgrades) errors.Add("Améliorations invalides.");
            if (double.IsNaN(state.playSeconds) || double.IsInfinity(state.playSeconds) || state.playSeconds < 0)
                errors.Add("Temps de jeu invalide.");
            if (!string.IsNullOrEmpty(state.ending) && state.ending != "restore" && state.ending != "liberate")
                errors.Add("Conclusion inconnue.");
            if (state.flags == null) errors.Add("Drapeaux absents.");
            if (state.quests == null) errors.Add("Progression des missions absente.");
            else
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (QuestProgress progress in state.quests)
                {
                    if (progress == null || string.IsNullOrWhiteSpace(progress.id))
                    { errors.Add("Mission de sauvegarde sans identifiant."); continue; }
                    if (!ids.Add(progress.id)) errors.Add("Mission de sauvegarde en double : " + progress.id);
                    if (!Enum.IsDefined(typeof(QuestStatus), progress.status) || progress.stepIndex < 0)
                        errors.Add("Progression de mission invalide : " + progress.id);
                }
            }
            return errors;
        }

        public static List<string> ValidateDefinitions(IList<QuestDefinition> definitions)
        {
            var errors = new List<string>();
            if (definitions == null) { errors.Add("Campagne absente."); return errors; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var mainOrders = new HashSet<int>();
            foreach (QuestDefinition quest in definitions)
            {
                if (quest == null) { errors.Add("Définition de mission absente."); continue; }
                if (string.IsNullOrWhiteSpace(quest.id) || !ids.Add(quest.id)) errors.Add("Identifiant de mission absent ou en double.");
                if (quest.isMain && !mainOrders.Add(quest.order)) errors.Add("Ordre principal en double : " + quest.order);
                if (quest.steps == null || quest.steps.Count == 0)
                { errors.Add("Mission sans objectif : " + quest.id); continue; }
                foreach (QuestStep step in quest.steps)
                    if (step == null || string.IsNullOrWhiteSpace(step.eventId) || string.IsNullOrWhiteSpace(step.targetId))
                        errors.Add("Objectif sans événement ou cible : " + quest.id);
            }
            return errors;
        }

        public static bool TrySpendEnergy(GameState state, float cost)
        {
            if (state == null || !IsFinite(cost) || cost < 0 || !IsFinite(state.energy) || state.energy < cost) return false;
            state.energy = Clamp(state.energy - cost, 0, MaximumEnergy);
            return true;
        }

        public static void RestoreEnergy(GameState state, float amount)
        {
            if (state == null || !IsFinite(amount) || amount <= 0) return;
            float energy = IsFinite(state.energy) ? state.energy : 0f;
            state.energy = Clamp(energy + amount, 0, MaximumEnergy);
        }

        public static bool CanChooseEnding(GameState state)
        {
            return state != null && state.flags != null &&
                   state.flags.Contains("override:final_core") && state.flags.Contains("kill:archivist") &&
                   state.flags.Contains("interact:fragment_port") && state.flags.Contains("interact:fragment_neon") &&
                   state.flags.Contains("interact:fragment_cliffs");
        }

        public static bool IsFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }

        private static bool IsValidPosition(PositionData position)
        {
            return position != null && IsFinite(position.x) && IsFinite(position.y) && IsFinite(position.z) &&
                   position.x >= -500f && position.x <= 500f && position.z >= -500f && position.z <= 500f &&
                   position.y >= 0f && position.y <= 250f;
        }

        private static PositionData NormalizePosition(PositionData position)
        {
            if (position == null || !IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
                return new PositionData(-340f, 1f, -300f);
            // This map's walkable ground starts at y=0. A saved falling checkpoint must
            // never produce an endless below-ground respawn loop.
            return new PositionData(Clamp(position.x, -490f, 490f), Clamp(position.y, .1f, 250f), Clamp(position.z, -490f, 490f));
        }

        private static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); }
    }
}
