using System;
using System.Collections.Generic;

namespace Rive404.Domain
{
    /// <summary>
    /// Deterministic quest progression. One-shot world events survive saves and early collection.
    /// Conversations require a fresh conversation at the currently displayed step.
    /// </summary>
    public sealed class QuestEngine
    {
        private readonly GameState state;
        private readonly Dictionary<string, QuestDefinition> definitionsById;
        private readonly Dictionary<string, QuestProgress> progressById;
        private readonly HashSet<string> recordedEvents;
        private readonly List<QuestDefinition> mainQuests;

        public List<QuestDefinition> Definitions { get; private set; }

        public QuestDefinition CurrentMain
        {
            get
            {
                foreach (QuestDefinition definition in mainQuests)
                    if (progressById[definition.id].status == QuestStatus.Active) return definition;
                return null;
            }
        }

        public QuestEngine(GameState state, List<QuestDefinition> definitions)
        {
            if (state == null) throw new ArgumentNullException("state");
            var errors = StateRules.ValidateDefinitions(definitions);
            if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors.ToArray()), "definitions");
            this.state = StateRules.Normalize(state);
            Definitions = definitions;
            definitionsById = new Dictionary<string, QuestDefinition>(StringComparer.Ordinal);
            progressById = new Dictionary<string, QuestProgress>(StringComparer.Ordinal);
            recordedEvents = new HashSet<string>(this.state.flags, StringComparer.Ordinal);
            mainQuests = new List<QuestDefinition>();

            foreach (QuestProgress saved in this.state.quests) progressById[saved.id] = saved;
            foreach (QuestDefinition definition in Definitions)
            {
                definitionsById.Add(definition.id, definition);
                if (definition.isMain) mainQuests.Add(definition);
                QuestProgress progress;
                if (!progressById.TryGetValue(definition.id, out progress))
                {
                    progress = new QuestProgress { id = definition.id, status = QuestStatus.NotStarted, stepIndex = 0 };
                    progressById.Add(definition.id, progress);
                    this.state.quests.Add(progress);
                }
                progress.stepIndex = Math.Min(definition.steps.Count, progress.stepIndex);
                if (progress.status == QuestStatus.Completed || progress.stepIndex == definition.steps.Count)
                {
                    progress.status = QuestStatus.Completed;
                    progress.stepIndex = definition.steps.Count;
                    AddFlag("quest:" + definition.id);
                }
                else if (progress.stepIndex > 0 && progress.status == QuestStatus.NotStarted)
                    progress.status = QuestStatus.Active;
                if (definition.isMain && progress.status == QuestStatus.Failed)
                    progress.status = QuestStatus.NotStarted;
            }
            mainQuests.Sort(delegate(QuestDefinition left, QuestDefinition right) { return left.order.CompareTo(right.order); });
            // Repair inconsistent active-main state without skipping an unfinished chapter.
            bool selectedMain = false;
            foreach (QuestDefinition definition in mainQuests)
            {
                var progress = progressById[definition.id];
                if (progress.status == QuestStatus.Completed) continue;
                progress.status = selectedMain ? QuestStatus.NotStarted : QuestStatus.Active;
                selectedMain = true;
            }
            ReconcileUpgradeRewards();
            AdvanceAll(null, new List<string>());
        }

        public QuestProgress GetProgress(string id)
        {
            QuestProgress progress;
            return id != null && progressById.TryGetValue(id, out progress) ? progress : null;
        }

        public QuestStep Objective(QuestDefinition definition)
        {
            if (definition == null) return null;
            QuestProgress progress = GetProgress(definition.id);
            if (progress == null || progress.status != QuestStatus.Active || progress.stepIndex >= definition.steps.Count) return null;
            return definition.steps[progress.stepIndex];
        }

        public bool Activate(string id)
        {
            QuestDefinition definition;
            if (id == null || !definitionsById.TryGetValue(id, out definition)) return false;
            QuestProgress progress = progressById[id];
            if (progress.status == QuestStatus.Active || progress.status == QuestStatus.Completed) return false;
            if (definition.isMain)
            {
                foreach (QuestDefinition main in mainQuests)
                {
                    if (progressById[main.id].status == QuestStatus.Completed) continue;
                    if (main.id != id) return false;
                    break;
                }
            }
            progress.status = QuestStatus.Active;
            AdvanceAll(null, new List<string>());
            return true;
        }

        public List<string> Record(string eventId)
        {
            var completed = new List<string>();
            if (string.IsNullOrWhiteSpace(eventId)) return completed;
            eventId = eventId.Trim();
            AddFlag(eventId);
            AdvanceAll(eventId, completed);
            return completed;
        }

        private void AdvanceAll(string directEvent, List<string> completed)
        {
            // The direct event is offered once to each already-active quest. It is never offered
            // again to a chapter activated by the same action (notably another talk:sillage).
            var active = new List<QuestDefinition>();
            foreach (QuestDefinition definition in Definitions)
                if (progressById[definition.id].status == QuestStatus.Active) active.Add(definition);
            foreach (QuestDefinition definition in active) Advance(definition, directEvent, completed);

            while (CurrentMain == null)
            {
                QuestDefinition next = null;
                foreach (QuestDefinition definition in mainQuests)
                    if (progressById[definition.id].status != QuestStatus.Completed) { next = definition; break; }
                if (next == null) break;
                progressById[next.id].status = QuestStatus.Active;
                Advance(next, null, completed);
                if (progressById[next.id].status == QuestStatus.Active) break;
            }
        }

        private void Advance(QuestDefinition definition, string directEvent, List<string> completed)
        {
            QuestProgress progress = progressById[definition.id];
            bool directConsumed = false;
            while (progress.status == QuestStatus.Active && progress.stepIndex < definition.steps.Count)
            {
                string required = definition.steps[progress.stepIndex].eventId;
                bool matchesDirect = !directConsumed && directEvent != null && required == directEvent;
                bool matchesMemory = IsPersistentEvent(required) && recordedEvents.Contains(required);
                if (!matchesDirect && !matchesMemory) break;
                progress.stepIndex++;
                if (matchesDirect) directConsumed = true;
            }
            if (progress.status == QuestStatus.Active && progress.stepIndex == definition.steps.Count)
            {
                progress.status = QuestStatus.Completed;
                AddFlag("quest:" + definition.id);
                ReconcileUpgradeRewards();
                completed.Add(definition.id);
            }
        }

        private static bool IsPersistentEvent(string eventId)
        {
            // Return conversations and chapter introductions must still be experienced.
            // All collectible/combat/debug milestones are facts that remain true after respawn.
            return !eventId.StartsWith("talk:", StringComparison.Ordinal);
        }

        private void AddFlag(string flag)
        {
            if (recordedEvents.Add(flag) && !state.flags.Contains(flag)) state.flags.Add(flag);
        }

        private void ReconcileUpgradeRewards()
        {
            int unlocked = 0;
            string[] rewardQuests = { "premier_fragment", "laboratoire", "heritage_rouge" };
            foreach (string id in rewardQuests)
            {
                QuestProgress progress;
                if (!progressById.TryGetValue(id, out progress) || progress.status != QuestStatus.Completed) continue;
                AddFlag("upgrade:" + id);
                unlocked++;
            }
            state.upgrades = Math.Min(StateRules.MaximumUpgrades, Math.Max(state.upgrades, unlocked));
        }
    }
}
