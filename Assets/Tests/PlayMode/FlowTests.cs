using System.Collections;
using NUnit.Framework;
using SaucerSiege.Core;
using SaucerSiege.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Screen = SaucerSiege.Core.Screen;

namespace SaucerSiege.Tests
{
    /// <summary>
    /// The game's flow in the real scene: menus, building through the director, winning, losing,
    /// restarting during the result beat, and pausing.
    /// </summary>
    public class FlowTests
    {
        GameDirector director;

        static LevelDefinition Lane(int money, int scouts, float interval = 1f) =>
            new LevelDefinition("Lane", "A test road.", Theme.Meadow, money, new[]
            {
                "...........",
                "S#########B",
                "...........",
            }, new[] { new WaveDefinition(new[] { new WaveGroup((int)EnemyKind.Scout, scouts, 0f, interval) }, 1f) });

        IEnumerator Load()
        {
            SceneManager.LoadScene("SaucerSiege");
            yield return null;
            yield return null;
            director = Object.FindAnyObjectByType<GameDirector>();
            Assert.That(director, Is.Not.Null);
            director.UseMemoryStore();
            director.ScriptedInput = true;
            yield return null;
            Assert.That(director.Session.Screen, Is.EqualTo(Screen.Title));
            Assert.That(director.Hud.Showing, Is.EqualTo("Title"));
        }

        IEnumerator WaitFor(System.Func<bool> condition, float timeout, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition() && Time.realtimeSinceStartup - start < timeout)
            {
                yield return null;
            }
            Assert.That(condition(), Is.True, $"timed out waiting for {what}");
        }

        [UnityTest]
        public IEnumerator TheMenusLeadToTheFirstBattlefieldOnly()
        {
            yield return Load();
            director.Play();
            yield return null;
            Assert.That(director.Session.Screen, Is.EqualTo(Screen.LevelSelect));
            Assert.That(director.Hud.Showing, Is.EqualTo("LevelSelect"));
            Assert.That(director.TryStartLevel(1), Is.False, "the second battlefield is locked");
            Assert.That(director.Session.Screen, Is.EqualTo(Screen.LevelSelect));

            Assert.That(director.TryStartLevel(0), Is.True);
            yield return null;
            Assert.That(director.Session.Screen, Is.EqualTo(Screen.Playing));
            Assert.That(director.Hud.Showing, Is.EqualTo("Playing"));
            Assert.That(director.Session.Battle.Level.Name, Is.EqualTo("Green Hollow"));
            Assert.That(director.Board, Is.Not.Null);
            Assert.That(director.Board.Root.childCount, Is.GreaterThanOrEqualTo((15 + 2) * (9 + 2)), "a tile on every cell and border cell");
        }

        [UnityTest]
        public IEnumerator BuildingAndHoldingTheRoadWins()
        {
            yield return Load();
            director.StartTrial(Lane(90, 2));
            yield return null;
            director.ToggleBuild(TowerKind.Ballista);
            Assert.That(director.Placing, Is.EqualTo(TowerKind.Ballista));
            director.ActAt(new Cell(1, 2));
            Assert.That(director.Placing, Is.Null, "building ends the placement");
            director.ToggleBuild(TowerKind.Ballista);
            director.ActAt(new Cell(1, 1));
            Assert.That(director.Session.Battle.TowerAt(new Cell(1, 1)), Is.Null, "not on the road");
            director.ActAt(new Cell(2, 0));
            yield return null;
            Assert.That(director.TowerViewCount, Is.EqualTo(2));
            Assert.That(director.Session.Battle.Economy.Money, Is.EqualTo(0));

            director.ActAt(new Cell(1, 2));
            Assert.That(director.Selected, Is.SameAs(director.Session.Battle.TowerAt(new Cell(1, 2))), "clicking a tower selects it");

            Assert.That(director.CallWave(), Is.True);
            yield return WaitFor(() => director.EnemyViewCount > 0, 2f, "a saucer view");
            yield return WaitFor(() => director.Session.Screen == Screen.Result, 15f, "the result screen");
            Assert.That(director.Session.LastResult.Won, Is.True);
            Assert.That(director.Session.LastResult.Stars, Is.EqualTo(3));
            Assert.That(director.Hud.Showing, Is.EqualTo("Result"));
            Assert.That(director.EnemyViewCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator LettingTheSaucersThroughLoses()
        {
            yield return Load();
            director.StartTrial(Lane(0, 20, 0.05f));
            yield return null;
            director.CycleSpeed();
            director.CycleSpeed();
            Assert.That(director.Session.Speed, Is.EqualTo(3));
            director.CallWave();
            yield return WaitFor(() => director.Session.Screen == Screen.Result, 15f, "the result screen");
            Assert.That(director.Session.LastResult.Won, Is.False);
            Assert.That(director.Session.Battle.Economy.Lives, Is.EqualTo(0));
            Assert.That(director.Hud.Showing, Is.EqualTo("Result"));
        }

        [UnityTest]
        public IEnumerator RestartingDuringTheResultBeatStartsACleanBattle()
        {
            yield return Load();
            director.StartTrial(Lane(0, 20, 0.05f));
            yield return null;
            director.CycleSpeed();
            director.CycleSpeed();
            director.CallWave();
            yield return WaitFor(() => director.Session.Battle.IsOver, 10f, "the defeat");
            Assert.That(director.Session.Screen, Is.EqualTo(Screen.Playing), "still in the result beat");
            director.Restart();
            yield return new WaitForSecondsRealtime(Session.ResultDelay + 0.5f);
            Assert.That(director.Session.Screen, Is.EqualTo(Screen.Playing), "the old result popped over the new battle");
            Assert.That(director.Session.Battle.State, Is.EqualTo(BattleState.Preparing));
            Assert.That(director.Session.Battle.Economy.Lives, Is.EqualTo(Catalog.StartingLives));
            Assert.That(director.Session.Speed, Is.EqualTo(1));
            Assert.That(director.EnemyViewCount, Is.EqualTo(0));
            Assert.That(director.Hud.Showing, Is.EqualTo("Playing"));
        }

        [UnityTest]
        public IEnumerator PauseFreezesTheSaucersAndResumeContinues()
        {
            yield return Load();
            director.StartTrial(Lane(0, 1));
            yield return null;
            director.CallWave();
            yield return WaitFor(() => director.EnemyViewCount > 0, 2f, "a saucer view");
            yield return new WaitForSecondsRealtime(0.3f);
            EnemyAgent saucer = director.Session.Battle.Enemies[0];
            director.TogglePause();
            yield return null;
            Assert.That(director.Session.Screen, Is.EqualTo(Screen.Paused));
            Assert.That(director.Hud.Showing, Is.EqualTo("Paused"));
            float time = director.Session.Battle.Time;
            Vector3 before = director.ViewOf(saucer.Id).Root.position;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(director.Session.Battle.Time, Is.EqualTo(time));
            Assert.That(Vector3.Distance(director.ViewOf(saucer.Id).Root.position, before), Is.LessThan(0.1f), "only the hover bob moves");

            director.TogglePause();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(director.Session.Screen, Is.EqualTo(Screen.Playing));
            Assert.That(director.Session.Battle.Time, Is.GreaterThan(time + 0.2f));
            Assert.That(saucer.Distance, Is.GreaterThan(0.6f));
        }
    }
}
