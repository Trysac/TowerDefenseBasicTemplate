using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SaucerSiege.Core.Tests
{
    public class BattleTests
    {
        /// <summary>A straight ten-tile road along the top row with open ground below it.</summary>
        static Battle Lane(int money, params WaveDefinition[] waves) =>
            new Battle(new LevelDefinition("Lane", "", Theme.Meadow, money, new[]
            {
                "S#########B",
                "...........",
            }, waves));

        static WaveDefinition Wave(EnemyKind kind, int count, float interval = 1f) =>
            new WaveDefinition(new[] { new WaveGroup((int)kind, count, 0f, interval) }, 1f);

        static readonly Cell BelowRoad = new Cell(2, 1);

        static List<BattleEvent> RunUntil(Battle battle, Func<Battle, bool> done, float maxSeconds)
        {
            var seen = new List<BattleEvent>();
            for (float t = 0f; t < maxSeconds && !done(battle); t += Battle.Step)
            {
                battle.Advance(Battle.Step);
                seen.AddRange(battle.Events);
                battle.ClearEvents();
            }
            return seen;
        }

        static int CountOf(List<BattleEvent> events, BattleEventType type) => events.FindAll(e => e.Type == type).Count;

        [Test]
        public void BuildingSpendsTheCostAndTakesTheCell()
        {
            Battle battle = Lane(100, Wave(EnemyKind.Scout, 1));
            TowerAgent tower = battle.Build(TowerKind.Ballista, BelowRoad);
            Assert.That(tower, Is.Not.Null);
            Assert.That(battle.Economy.Money, Is.EqualTo(55));
            Assert.That(battle.TowerAt(BelowRoad), Is.SameAs(tower));
            Assert.That(tower.Position, Is.EqualTo(new Vec2(2f, 0f)));
            Assert.That(battle.Events[0].Type, Is.EqualTo(BattleEventType.TowerBuilt));
            Assert.That(battle.Build(TowerKind.Ballista, BelowRoad), Is.Null, "the cell is taken");
            Assert.That(battle.Economy.Money, Is.EqualTo(55));
            Assert.That(battle.Towers.Count, Is.EqualTo(1));
        }

        [Test]
        public void TowersCannotStandOnTheRoadOrBeBoughtWithoutTheMoney()
        {
            Battle battle = Lane(44, Wave(EnemyKind.Scout, 1));
            Assert.That(battle.CanBuild(TowerKind.Ballista, new Cell(2, 0)), Is.False);
            Assert.That(battle.CanBuild(TowerKind.Ballista, BelowRoad), Is.False, "costs 45");
            Assert.That(battle.Build(TowerKind.Ballista, BelowRoad), Is.Null);
            Assert.That(battle.Economy.Money, Is.EqualTo(44));
            Assert.That(battle.Towers, Is.Empty);
        }

        [Test]
        public void UpgradesRaiseTheTierAndSellingRefundsSeventyPercent()
        {
            Battle battle = Lane(1000, Wave(EnemyKind.Scout, 1));
            TowerAgent tower = battle.Build(TowerKind.Ballista, BelowRoad);
            Assert.That(tower.UpgradeCost, Is.EqualTo(50));
            Assert.That(battle.Upgrade(tower), Is.True);
            Assert.That(battle.Upgrade(tower), Is.True);
            Assert.That(tower.Tier, Is.EqualTo(2));
            Assert.That(tower.Current.Damage, Is.EqualTo(8));
            Assert.That(tower.Invested, Is.EqualTo(45 + 50 + 85));
            Assert.That(tower.CanUpgrade, Is.False);
            Assert.That(battle.Upgrade(tower), Is.False, "already at the top tier");
            Assert.That(battle.Economy.Money, Is.EqualTo(1000 - 180));

            Assert.That(battle.Sell(tower), Is.EqualTo(126));
            Assert.That(battle.Economy.Money, Is.EqualTo(1000 - 180 + 126));
            Assert.That(battle.TowerAt(BelowRoad), Is.Null);
            Assert.That(battle.Sell(tower), Is.EqualTo(0), "it is already gone");
        }

        [Test]
        public void UpgradesNeedTheMoney()
        {
            Battle battle = Lane(60, Wave(EnemyKind.Scout, 1));
            TowerAgent tower = battle.Build(TowerKind.Ballista, BelowRoad);
            Assert.That(battle.CanUpgrade(tower), Is.False);
            Assert.That(battle.Upgrade(tower), Is.False);
            Assert.That(tower.Tier, Is.EqualTo(0));
            Assert.That(battle.Economy.Money, Is.EqualTo(15));
        }

        [Test]
        public void ASaucerThatReachesTheBaseCostsLives()
        {
            Battle battle = Lane(0, Wave(EnemyKind.Raider, 1));
            Assert.That(battle.State, Is.EqualTo(BattleState.Preparing));
            Assert.That(battle.CallNextWave(), Is.True);
            Assert.That(battle.State, Is.EqualTo(BattleState.Fighting));
            List<BattleEvent> events = RunUntil(battle, b => b.IsOver, 20f);
            Assert.That(CountOf(events, BattleEventType.EnemySpawned), Is.EqualTo(1));
            Assert.That(CountOf(events, BattleEventType.EnemyLeaked), Is.EqualTo(1));
            Assert.That(battle.Economy.Lives, Is.EqualTo(Catalog.StartingLives - 1));
            Assert.That(battle.Leaks, Is.EqualTo(1));
            Assert.That(battle.Time, Is.EqualTo(10f / 1.35f).Within(0.05f), "ten tiles at 1.35 tiles per second");
            Assert.That(battle.State, Is.EqualTo(BattleState.Won), "surviving the last wave wins");
            Assert.That(battle.Stars, Is.EqualTo(2));
        }

        [Test]
        public void AnIdlePlayerLosesGreenHollow()
        {
            var battle = new Battle(Levels.All[0]);
            battle.CallNextWave();
            List<BattleEvent> events = RunUntil(battle, b => b.IsOver, 3000f);
            Assert.That(battle.State, Is.EqualTo(BattleState.Lost));
            Assert.That(battle.Economy.Lives, Is.EqualTo(0));
            Assert.That(battle.Kills, Is.EqualTo(0));
            Assert.That(CountOf(events, BattleEventType.Lost), Is.EqualTo(1));
            Assert.That(CountOf(events, BattleEventType.Won), Is.EqualTo(0));
            Assert.That(battle.WavesStarted, Is.GreaterThan(1), "later waves start by themselves");
        }

        [Test]
        public void NothingCanBeDoneOnceTheBattleIsLost()
        {
            Battle battle = Lane(500, Wave(EnemyKind.Mothership, 2, 0.1f), Wave(EnemyKind.Scout, 1));
            battle.CallNextWave();
            RunUntil(battle, b => b.IsOver, 60f);
            Assert.That(battle.State, Is.EqualTo(BattleState.Lost));
            Assert.That(battle.Build(TowerKind.Ballista, BelowRoad), Is.Null);
            Assert.That(battle.CallNextWave(), Is.False);
            Assert.That(battle.Economy.Money, Is.EqualTo(500));
        }

        [Test]
        public void TheNextWaveCountsDownOnceTheLastOneHasSpawnedAndCallingItEarlyPays()
        {
            Battle battle = Lane(0, Wave(EnemyKind.Raider, 1), Wave(EnemyKind.Scout, 1));
            Assert.That(battle.Countdown, Is.EqualTo(-1f), "the first wave waits for the player");
            Assert.That(battle.EarlyCallBonus, Is.EqualTo(0));
            battle.CallNextWave();
            RunUntil(battle, b => false, 4f);
            Assert.That(battle.Countdown, Is.EqualTo(Catalog.WaveCountdown - 4f).Within(0.05f));
            int bonus = (int)Math.Ceiling(battle.Countdown) * Catalog.EarlyCallBonusPerSecond;
            Assert.That(bonus, Is.GreaterThanOrEqualTo(20));
            Assert.That(battle.EarlyCallBonus, Is.EqualTo(bonus));
            Assert.That(battle.CallNextWave(), Is.True);
            Assert.That(battle.Economy.Money, Is.EqualTo(bonus));
            Assert.That(battle.WavesStarted, Is.EqualTo(2));
            Assert.That(battle.Countdown, Is.EqualTo(-1f));
            Assert.That(battle.NextWave, Is.Null);
            Assert.That(battle.CanCallWave, Is.False);
        }

        [Test]
        public void TheNextWaveStartsByItselfWhenTheCountdownEnds()
        {
            Battle battle = Lane(0, Wave(EnemyKind.Mothership, 1), Wave(EnemyKind.Scout, 1));
            battle.CallNextWave();
            battle.ClearEvents();
            List<BattleEvent> events = RunUntil(battle, b => b.WavesStarted == 2, Catalog.WaveCountdown + 1f);
            Assert.That(battle.WavesStarted, Is.EqualTo(2));
            Assert.That(battle.Time, Is.EqualTo(Catalog.WaveCountdown).Within(0.05f));
            Assert.That(battle.Economy.Money, Is.EqualTo(0), "no bonus when the countdown runs out");
            Assert.That(CountOf(events, BattleEventType.WaveStarted), Is.EqualTo(1));
        }

        [Test]
        public void KillsPayTheBountyAndClearingAWavePaysItsBonus()
        {
            Battle battle = Lane(45, Wave(EnemyKind.Scout, 1));
            TowerAgent tower = battle.Build(TowerKind.Ballista, new Cell(1, 1));
            battle.CallNextWave();
            List<BattleEvent> events = RunUntil(battle, b => b.IsOver, 20f);
            Assert.That(CountOf(events, BattleEventType.EnemyKilled), Is.EqualTo(1));
            Assert.That(CountOf(events, BattleEventType.WaveCleared), Is.EqualTo(1));
            Assert.That(battle.Economy.Money, Is.EqualTo(Catalog.Enemy(EnemyKind.Scout).Bounty + Catalog.WaveClearBonus(1)));
            Assert.That(tower.Kills, Is.EqualTo(1));
            Assert.That(battle.Kills, Is.EqualTo(1));
            Assert.That(battle.State, Is.EqualTo(BattleState.Won));
            Assert.That(battle.Stars, Is.EqualTo(3));
            Assert.That(battle.Score, Is.EqualTo(3 * Catalog.ScorePerBounty + Catalog.StartingLives * Catalog.ScorePerLifeLeft));
            Assert.That(CountOf(events, BattleEventType.Won), Is.EqualTo(1));
        }

        [Test]
        public void ArmourTakesFromEveryHitButNeverAllOfIt()
        {
            Assert.That(Catalog.ArmoredDamage(7, 4), Is.EqualTo(3));
            Assert.That(Catalog.ArmoredDamage(3, 4), Is.EqualTo(1));
            Assert.That(Catalog.ArmoredDamage(5, 0), Is.EqualTo(5));
        }

        [Test]
        public void TanksShrugOffBallistaBolts()
        {
            Battle battle = Lane(45, Wave(EnemyKind.Tank, 1));
            battle.Build(TowerKind.Ballista, new Cell(1, 1));
            battle.CallNextWave();
            List<BattleEvent> events = RunUntil(battle, b => b.IsOver, 30f);
            List<BattleEvent> hits = events.FindAll(e => e.Type == BattleEventType.EnemyHit);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits.TrueForAll(h => h.Amount == 1), Is.True, "3 damage against 4 armour");
        }

        [Test]
        public void FrostSlowsWhatItHits()
        {
            Battle battle = Lane(60, Wave(EnemyKind.Raider, 1));
            battle.Build(TowerKind.Frost, new Cell(1, 1));
            battle.CallNextWave();
            RunUntil(battle, b => b.Enemies.Count > 0 && b.Enemies[0].IsSlowed, 5f);
            EnemyAgent raider = battle.Enemies[0];
            Assert.That(raider.IsSlowed, Is.True);
            Assert.That(raider.CurrentSpeed, Is.EqualTo(1.35f * 0.55f).Within(1e-4f));
            RunUntil(battle, b => !raider.IsSlowed, 10f);
            Assert.That(raider.CurrentSpeed, Is.EqualTo(1.35f).Within(1e-4f), "the chill wears off");
        }

        [Test]
        public void ACannonBlastHitsEverySaucerInIt()
        {
            Battle battle = Lane(75, new WaveDefinition(new[] { new WaveGroup((int)EnemyKind.Tank, 3, 0f, 0.3f) }, 1f));
            battle.Build(TowerKind.Cannon, new Cell(3, 1));
            battle.CallNextWave();
            List<BattleEvent> events = RunUntil(battle, b => b.IsOver, 40f);
            BattleEvent blast = events.Find(e => e.Type == BattleEventType.ShotLanded && e.Amount >= 2);
            Assert.That(blast.Type, Is.EqualTo(BattleEventType.ShotLanded), "a shell hit two or more tanks at once");
            Assert.That(blast.Radius, Is.EqualTo(0.7f));
        }

        [Test]
        public void LobbedShotsAimWhereTheTargetWillBe()
        {
            Battle battle = Lane(0, Wave(EnemyKind.Scout, 1));
            battle.CallNextWave();
            RunUntil(battle, b => false, 1f);
            EnemyAgent scout = battle.Enemies[0];
            Vec2 lead = battle.Lead(scout, new Vec2(5f, 0f), 5f);
            Assert.That(lead.Y, Is.EqualTo(1f).Within(1e-4f), "on the road");
            float flight = Vec2.Distance(new Vec2(5f, 0f), lead) / 5f;
            Assert.That(lead.X, Is.EqualTo(scout.Position.X + 2.1f * flight).Within(0.05f));
        }

        [Test]
        public void TheTargetPriorityCyclesThroughAllFour()
        {
            Battle battle = Lane(45, Wave(EnemyKind.Scout, 1));
            TowerAgent tower = battle.Build(TowerKind.Ballista, BelowRoad);
            var seen = new List<TargetPriority> { tower.Priority };
            for (int i = 0; i < 4; i++)
            {
                battle.CyclePriority(tower);
                seen.Add(tower.Priority);
            }
            Assert.That(seen, Is.EqualTo(new[]
            {
                TargetPriority.First, TargetPriority.Close, TargetPriority.Strong, TargetPriority.Last, TargetPriority.First,
            }));
        }

        [Test]
        public void ALongFramePlaysOutLikeManyShortOnes()
        {
            Battle a = Lane(0, Wave(EnemyKind.Scout, 3, 0.4f));
            Battle b = Lane(0, Wave(EnemyKind.Scout, 3, 0.4f));
            a.CallNextWave();
            b.CallNextWave();
            a.Advance(1.5f + Battle.Step * 0.5f);
            for (int i = 0; i < 90; i++) b.Advance(Battle.Step);
            Assert.That(a.Enemies.Count, Is.EqualTo(b.Enemies.Count));
            for (int i = 0; i < a.Enemies.Count; i++)
            {
                Assert.That(a.Enemies[i].Distance, Is.EqualTo(b.Enemies[i].Distance).Within(1e-4f));
            }
        }
    }
}
