using System;
using System.IO;
using NUnit.Framework;
using Rive404.Domain;
using Rive404.Runtime;
using UnityEngine;

namespace Rive404.Tests
{
    public sealed class SaveStoreTests
    {
        private string folder;
        private SaveStore store;

        [SetUp]
        public void Setup()
        {
            folder = Path.Combine(Path.GetTempPath(), "rive404-tests-" + Guid.NewGuid().ToString("N"));
            store = new SaveStore(folder, state => JsonUtility.ToJson(state), json => JsonUtility.FromJson<GameState>(json),
                envelope => JsonUtility.ToJson(envelope), json => JsonUtility.FromJson<SaveEnvelope>(json));
        }

        [TearDown]
        public void Cleanup() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }

        [Test]
        public void SaveAndReloadKeepsPositionCheckpointAndProgress()
        {
            var state = new GameState { position = new PositionData(40, 3, 70), checkpoint = new PositionData(-20, 1, 12), energy = 43 };
            state.flags.Add("interact:fragment_port");
            var quests = new QuestEngine(state, Campaign.CreateDefinitions());
            quests.Record(quests.Objective(quests.CurrentMain).eventId);
            store.Save(state);
            Assert.That(store.TryLoad(out GameState restored, out string message), Is.True, message);
            Assert.That(restored.position.x, Is.EqualTo(40));
            Assert.That(restored.checkpoint.z, Is.EqualTo(12));
            Assert.That(restored.energy, Is.EqualTo(43));
            Assert.That(restored.flags, Contains.Item("interact:fragment_port"));
            Assert.That(restored.quests[0].stepIndex, Is.EqualTo(state.quests[0].stepIndex));
        }

        [Test]
        public void CorruptPrimaryRestoresPreviousValidBackup()
        {
            store.Save(new GameState { energy = 71 });
            store.Save(new GameState { energy = 24 });
            File.WriteAllText(store.Path, "{broken-json");
            Assert.That(store.TryLoad(out GameState restored, out string message), Is.True, message);
            Assert.That(restored.energy, Is.EqualTo(71));
            Assert.That(message, Is.Not.Empty);
        }

        [Test]
        public void FutureSaveVersionIsRejected()
        {
            Assert.Throws<InvalidDataException>(() => store.Save(new GameState { version = StateRules.CurrentVersion + 1 }));
        }
    }
}
