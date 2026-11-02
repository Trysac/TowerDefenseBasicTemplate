using NUnit.Framework;

namespace SaucerSiege.Core.Tests
{
    public class SessionTests
    {
        static void Play(Session session, float realSeconds)
        {
            for (float t = 0f; t < realSeconds; t += 0.05f)
            {
                session.Tick(0.05f);
            }
        }

        [Test]
        public void OnlyTheFirstLevelIsOpenAtFirst()
        {
            var progress = new Progress(new MemoryRecordStore());
            Assert.That(progress.IsUnlocked(0), Is.True);
            Assert.That(progress.IsUnlocked(1), Is.False);
            Assert.That(progress.IsUnlocked(2), Is.False);
            Assert.That(progress.IsUnlocked(3), Is.False, "there is no fourth level");
            Assert.That(new Session(new MemoryRecordStore()).StartLevel(1), Is.False);
        }

        [Test]
        public void ClearingALevelOpensTheNextAndKeepsTheBest()
        {
            var store = new MemoryRecordStore();
            store.Save(0, new LevelRecord(4000, 2, true));
            var progress = new Progress(store);
            Assert.That(progress.IsUnlocked(1), Is.True);
            Assert.That(progress.IsUnlocked(2), Is.False);
            Assert.That(progress.TotalStars, Is.EqualTo(2));
        }

        [Test]
        public void ALossStillRecordsABestScoreButNoStars()
        {
            var store = new MemoryRecordStore();
            var session = new Session(store);
            Assert.That(session.StartLevel(0), Is.True);
            session.Battle.Build(TowerKind.Ballista, new Cell(5, 2));
            session.Battle.CallNextWave();
            for (int i = 0; i < 20000 && !session.Battle.IsOver; i++)
            {
                session.Tick(0.1f);
            }
            Assert.That(session.Battle.State, Is.EqualTo(BattleState.Lost));
            Assert.That(session.LastResult.Won, Is.False);
            Assert.That(session.LastResult.Score, Is.GreaterThan(0));
            Assert.That(session.LastResult.NewBest, Is.True);
            Assert.That(store.Load(0), Is.EqualTo(new LevelRecord(session.LastResult.Score, 0, false)));
            Assert.That(session.Progress.IsUnlocked(1), Is.False);
        }

        [Test]
        public void ABetterRunReplacesTheRecordAndAWorseOneDoesNot()
        {
            var store = new MemoryRecordStore();
            store.Save(0, new LevelRecord(999999, 1, true));
            var session = new Session(store);
            session.StartLevel(0);
            session.Battle.CallNextWave();
            for (int i = 0; i < 20000 && !session.Battle.IsOver; i++)
            {
                session.Tick(0.1f);
            }
            Assert.That(session.LastResult.NewBest, Is.False);
            Assert.That(store.Load(0), Is.EqualTo(new LevelRecord(999999, 1, true)));
        }

        [Test]
        public void PauseFreezesTheBattleAndSpeedMultipliesIt()
        {
            var session = new Session(new MemoryRecordStore());
            session.StartLevel(0);
            session.Battle.CallNextWave();
            Play(session, 1f);
            float before = session.Battle.Time;
            Assert.That(before, Is.EqualTo(1f).Within(0.06f));

            session.TogglePause();
            Assert.That(session.Screen, Is.EqualTo(Screen.Paused));
            Play(session, 1f);
            Assert.That(session.Battle.Time, Is.EqualTo(before), "nothing moves while paused");

            session.TogglePause();
            session.CycleSpeed();
            session.CycleSpeed();
            Assert.That(session.Speed, Is.EqualTo(3));
            Play(session, 1f);
            Assert.That(session.Battle.Time - before, Is.EqualTo(3f).Within(0.1f));
            session.CycleSpeed();
            Assert.That(session.Speed, Is.EqualTo(1));
        }

        [Test]
        public void TheResultScreenFollowsTheEndOfTheBattleAfterABeat()
        {
            var session = new Session(new MemoryRecordStore());
            session.StartLevel(0);
            session.Battle.CallNextWave();
            while (!session.Battle.IsOver)
            {
                session.Tick(0.1f);
            }
            Assert.That(session.Screen, Is.EqualTo(Screen.Playing));
            Assert.That(session.ResultPending, Is.True);
            Play(session, Session.ResultDelay - 0.2f);
            Assert.That(session.Screen, Is.EqualTo(Screen.Playing));
            Play(session, 0.4f);
            Assert.That(session.Screen, Is.EqualTo(Screen.Result));
            Assert.That(session.ResultPending, Is.False);
        }

        [Test]
        public void RestartingOrLeavingDuringTheBeatCancelsTheOldResult()
        {
            var session = new Session(new MemoryRecordStore());
            session.StartLevel(0);
            session.Battle.CallNextWave();
            while (!session.Battle.IsOver)
            {
                session.Tick(0.1f);
            }
            Assert.That(session.Restart(), Is.True);
            Play(session, Session.ResultDelay + 1f);
            Assert.That(session.Screen, Is.EqualTo(Screen.Playing), "the old result popped over the new battle");
            Assert.That(session.Battle.IsOver, Is.False);
            Assert.That(session.Battle.State, Is.EqualTo(BattleState.Preparing));

            session.Battle.CallNextWave();
            while (!session.Battle.IsOver)
            {
                session.Tick(0.1f);
            }
            session.ToTitle();
            Play(session, Session.ResultDelay + 1f);
            Assert.That(session.Screen, Is.EqualTo(Screen.Title));
            Assert.That(session.Battle, Is.Null);
        }

        [Test]
        public void ATrialLevelIsPlayedButNeverRecorded()
        {
            var store = new MemoryRecordStore();
            var session = new Session(store);
            var lane = new LevelDefinition("Lane", "", Theme.Meadow, 45, new[] { "S#####B", "......." },
                new[] { new WaveDefinition(new[] { new WaveGroup((int)EnemyKind.Scout, 1, 0f, 1f) }, 1f) });
            session.StartTrial(lane);
            Assert.That(session.IsTrial, Is.True);
            Assert.That(session.HasNextLevel, Is.False);
            session.Battle.Build(TowerKind.Ballista, new Cell(1, 1));
            session.Battle.CallNextWave();
            for (int i = 0; i < 400 && session.Screen != Screen.Result; i++)
            {
                session.Tick(0.05f);
            }
            Assert.That(session.Screen, Is.EqualTo(Screen.Result));
            Assert.That(session.LastResult.Won, Is.True);
            Assert.That(session.LastResult.Stars, Is.EqualTo(3));
            Assert.That(store.Load(0), Is.EqualTo(default(LevelRecord)));
            Assert.That(session.Restart(), Is.True);
            Assert.That(session.Battle.Level, Is.SameAs(lane));
        }

        [Test]
        public void StartingALevelResetsTheSpeed()
        {
            var session = new Session(new MemoryRecordStore());
            session.StartLevel(0);
            session.CycleSpeed();
            Assert.That(session.Speed, Is.EqualTo(2));
            session.Restart();
            Assert.That(session.Speed, Is.EqualTo(1));
            Assert.That(session.HasNextLevel, Is.True);
            Assert.That(session.StartNextLevel(), Is.False, "the second level is still locked");
        }
    }
}
