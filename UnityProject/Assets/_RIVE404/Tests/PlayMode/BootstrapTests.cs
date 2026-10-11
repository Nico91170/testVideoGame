using System.Collections;
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rive404.Runtime;
using Rive404.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rive404.Tests
{
    public sealed class BootstrapTests
    {
        private string saveFolder;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            saveFolder = Path.Combine(Path.GetTempPath(), "rive404-playmode-" + Guid.NewGuid().ToString("N"));
            GameRuntime.SaveDirectoryOverride = saveFolder;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (GameRuntime.Instance != null)
            {
                GameRuntime.Instance.ReturnToMenu();
                UnityEngine.Object.Destroy(GameRuntime.Instance.gameObject);
            }
            yield return null;
            GameRuntime.SaveDirectoryOverride = null;
            if (Directory.Exists(saveFolder)) Directory.Delete(saveFolder, true);
        }

        [UnityTest]
        public IEnumerator BootstrapStartsTheMenuAndCreatesAWorldOnNewGame()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return null;
            var runtime = GameRuntime.Instance;
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.IsPlaying, Is.False);
            runtime.StartNewGame();
            yield return null;
            Assert.That(runtime.IsPlaying, Is.True);
            Assert.That(runtime.Player, Is.Not.Null);
            Assert.That(runtime.World, Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(runtime.Quests.CurrentMain, Is.Not.Null);
            Assert.That(runtime.World.Entries.Count, Is.GreaterThanOrEqualTo(14));
            Assert.That(runtime.Player.GetComponent<CharacterController>(), Is.Not.Null);
            foreach (var definition in runtime.Quests.Definitions)
            foreach (var objective in definition.steps)
                Assert.That(runtime.World.Entries.Any(entry => entry.id == objective.targetId), Is.True, "Cible sans position : " + objective.targetId);
            var checkpoint = new Vector3(-325, 2, -290);
            runtime.Player.RecoverAt(checkpoint);
            Assert.That(runtime.Player.transform.position, Is.EqualTo(checkpoint));
            Assert.That(runtime.Player.Velocity, Is.EqualTo(Vector3.zero));
            Assert.That(runtime.Player.IsInvulnerable, Is.True);
            Assert.That(runtime.Player.GetComponent<CharacterController>().enabled, Is.True);

            var bossObject = new GameObject("Test_Boss");
            bossObject.transform.position = new Vector3(420, 2, -420);
            var boss = bossObject.AddComponent<EnemyAgent>();
            boss.Configure(runtime, "test_archivist", EnemyKind.Boss);
            float health = boss.Health;
            boss.TakeDamage(100);
            Assert.That(boss.Health, Is.EqualTo(health), "Boss protégé sans OVERRIDE du noyau.");
            int guard = 0;
            while (runtime.Quests.CurrentMain != null && runtime.Quests.CurrentMain.id != "dernier_commit")
            {
                Assert.That(++guard, Is.LessThan(150), "Campagne bloquée avant le noyau.");
                runtime.Quests.Record(runtime.Quests.Objective(runtime.Quests.CurrentMain).eventId);
            }
            Assert.That(runtime.Quests.CurrentMain.id, Is.EqualTo("dernier_commit"));
            if (!runtime.State.flags.Contains("tool:unlocked")) runtime.State.flags.Add("tool:unlocked");
            runtime.CloseDialogue();
            var core = runtime.World.GetComponentsInChildren<WorldInteractable>(true).First(target => target.id == "final_core");
            runtime.Player.RecoverAt(core.transform.position + Vector3.back * 2f);
            runtime.TryDebug(DebugOperation.Override, core);
            Assert.That(runtime.IsDebugActive("final_core"), Is.True);
            Assert.That(boss.IsShielded, Is.False, "OVERRIDE réel du noyau requis.");
            boss.TakeDamage(health * 0.4f);
            Assert.That(boss.Phase, Is.EqualTo(2));
            boss.TakeDamage(health * 0.3f);
            Assert.That(boss.Phase, Is.EqualTo(3));
            boss.TakeDamage(health);
            Assert.That(boss.IsDead, Is.True);
            Assert.That(runtime.State.flags, Contains.Item("kill:test_archivist"));
            int flagCount = runtime.State.flags.Count;
            boss.TakeDamage(health);
            Assert.That(runtime.State.flags.Count, Is.EqualTo(flagCount), "Kill répété idempotent.");
            UnityEngine.Object.Destroy(bossObject);
            runtime.ReturnToMenu();
            yield return null;
            Assert.That(runtime.IsPlaying, Is.False);
        }
    }
}
