using System;

namespace Rive404.Domain
{
    /// <summary>Prevents permanent story actions before their prerequisite conversations.</summary>
    public static class StoryRules
    {
        public static bool CanUseTarget(GameState state, QuestEngine quests, string targetId)
        {
            if (state == null || quests == null || string.IsNullOrEmpty(targetId)) return false;
            QuestStep objective = quests.Objective(quests.CurrentMain);
            if (targetId == "ending_restore" || targetId == "ending_liberate")
                return StateRules.CanChooseEnding(state) && objective != null && objective.eventId == "interact:ending";
            if (targetId == "final_core")
                return state.upgrades >= 3 && quests.CurrentMain != null && quests.CurrentMain.id == "dernier_commit";
            if (targetId == "fort_anchor")
                return state.upgrades >= 3 && objective != null && objective.targetId == "fort_anchor";
            if (targetId.StartsWith("fragment_", StringComparison.Ordinal))
                return objective != null && objective.targetId == targetId;
            return true;
        }
    }
}
