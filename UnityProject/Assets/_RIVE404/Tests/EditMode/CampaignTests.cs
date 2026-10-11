using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rive404.Domain;

namespace Rive404.Tests
{
    public sealed class CampaignTests
    {
        [Test]
        public void CampaignContainsNineMainAndFiveSideQuestsWithValidObjectives()
        {
            var definitions = Campaign.CreateDefinitions();
            Assert.That(definitions.Count(q => q.isMain), Is.EqualTo(9));
            Assert.That(definitions.Count(q => !q.isMain), Is.EqualTo(5));
            Assert.That(StateRules.ValidateDefinitions(definitions), Is.Empty);
        }

        [Test]
        public void EntireMainCampaignCanBeCompletedOnlyByItsRequiredActions()
        {
            var state = new GameState();
            var engine = new QuestEngine(state, Campaign.CreateDefinitions());
            int completed = 0;
            int guard = 0;
            while (engine.CurrentMain != null)
            {
                Assert.That(++guard, Is.LessThan(150), "La progression est bloquée.");
                var current = engine.CurrentMain;
                var objective = engine.Objective(current);
                Assert.That(objective, Is.Not.Null, current.id);
                var before = engine.GetProgress(current.id).stepIndex;
                engine.Record("unrelated:noise");
                Assert.That(engine.GetProgress(current.id).stepIndex, Is.EqualTo(before));
                completed += engine.Record(objective.eventId).Count(id => id == current.id);
            }
            Assert.That(completed, Is.EqualTo(9));
            foreach (var quest in engine.Definitions.Where(q => q.isMain))
                Assert.That(engine.GetProgress(quest.id).status, Is.EqualTo(QuestStatus.Completed));
            Assert.That(state.upgrades, Is.InRange(0, 3));
        }

        [Test]
        public void EarlierKillRemainsAvailableWhenItsQuestBecomesActive()
        {
            var definitions = new List<QuestDefinition>
            {
                Quest("first", 0, new QuestStep("talk:guide", "Parler", "guide")),
                Quest("second", 1, new QuestStep("kill:drone", "Vaincre", "drone"))
            };
            var engine = new QuestEngine(new GameState(), definitions);
            engine.Record("kill:drone");
            Assert.That(engine.GetProgress("first").status, Is.EqualTo(QuestStatus.Active));
            engine.Record("talk:guide");
            Assert.That(engine.GetProgress("second").status, Is.EqualTo(QuestStatus.Completed));
            Assert.That(engine.Record("kill:drone"), Is.Empty);
        }

        [Test]
        public void OneConversationCannotConsumeTwoConsecutiveConversationObjectives()
        {
            var definition = Quest("conversation", 0,
                new QuestStep("talk:guide", "Demander", "guide"),
                new QuestStep("talk:guide", "Répondre", "guide"));
            var engine = new QuestEngine(new GameState(), new List<QuestDefinition> { definition });
            engine.Record("talk:guide");
            Assert.That(engine.GetProgress("conversation").stepIndex, Is.EqualTo(1));
            Assert.That(engine.GetProgress("conversation").status, Is.EqualTo(QuestStatus.Active));
            engine.Record("talk:guide");
            Assert.That(engine.GetProgress("conversation").status, Is.EqualTo(QuestStatus.Completed));
        }

        [Test]
        public void EndingNeedsEveryFragmentBossAndOverriddenCore()
        {
            var requirements = new[] { "interact:fragment_port", "interact:fragment_neon", "interact:fragment_cliffs", "override:final_core", "kill:archivist" };
            foreach (string missing in requirements)
            {
                var state = new GameState { flags = requirements.Where(flag => flag != missing).ToList() };
                Assert.That(StateRules.CanChooseEnding(state), Is.False, missing);
            }
            Assert.That(StateRules.CanChooseEnding(new GameState { flags = requirements.ToList() }), Is.True);
        }

        private static QuestDefinition Quest(string id, int order, params QuestStep[] steps)
        {
            return new QuestDefinition { id = id, title = id, description = id, isMain = true, order = order, steps = new List<QuestStep>(steps) };
        }

        [Test]
        public void ThreeFragmentsCannotSkipCaptainsFarewellOrBoss()
        {
            var state = new GameState();
            var engine = new QuestEngine(state, Campaign.CreateDefinitions());
            int guard = 0;
            while (engine.CurrentMain.id != "sacrifice_sillage")
            {
                Assert.That(++guard, Is.LessThan(150));
                engine.Record(engine.Objective(engine.CurrentMain).eventId);
            }
            Assert.That(state.upgrades, Is.EqualTo(3));
            Assert.That(StoryRules.CanUseTarget(state, engine, "fort_anchor"), Is.False);
            Assert.That(StoryRules.CanUseTarget(state, engine, "final_core"), Is.False);
            engine.Record("talk:sillage");
            engine.Record("scan:fort_relay");
            engine.Record("override:fort_relay");
            Assert.That(StoryRules.CanUseTarget(state, engine, "fort_anchor"), Is.True);
            engine.Record("interact:fort_anchor");
            Assert.That(StoryRules.CanUseTarget(state, engine, "final_core"), Is.True);
            Assert.That(StoryRules.CanUseTarget(state, engine, "ending_restore"), Is.False);
            engine.Record("override:final_core");
            engine.Record("kill:archivist");
            Assert.That(StoryRules.CanUseTarget(state, engine, "ending_restore"), Is.True);
            Assert.That(StoryRules.CanUseTarget(state, engine, "ending_liberate"), Is.True);
        }
    }
}
