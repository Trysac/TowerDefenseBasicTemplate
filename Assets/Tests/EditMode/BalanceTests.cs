using System.Collections.Generic;
using NUnit.Framework;

namespace SaucerSiege.Core.Tests
{
    /// <summary>
    /// Plays every level with a simple bot to prove each one can be won with sensible play, and
    /// that it is not a walkover: a thin defence of ballistas alone loses.
    /// </summary>
    public class BalanceTests
    {
        static readonly TowerKind[] BuildOrder =
        {
            TowerKind.Ballista, TowerKind.Ballista, TowerKind.Cannon, TowerKind.Frost, TowerKind.Ballista,
            TowerKind.Mortar, TowerKind.Cannon, TowerKind.Frost, TowerKind.Mortar, TowerKind.Cannon,
        };

        /// <summary>The free cell whose range covers the most road.</summary>
        static Cell BestCell(Battle battle, float range)
        {
            Cell best = new Cell(-1, -1);
            int bestCover = -1;
            for (int r = 0; r < battle.Map.Rows; r++)
            {
                for (int c = 0; c < battle.Map.Cols; c++)
                {
                    var cell = new Cell(c, r);
                    if (!battle.Map.IsBuildable(cell) || battle.TowerAt(cell) != null) continue;
                    Vec2 centre = battle.Map.Center(cell);
                    int cover = 0;
                    for (float d = 0f; d < battle.Path.Length; d += 0.25f)
                    {
                        if ((battle.Path.PositionAt(d) - centre).LengthSquared <= range * range) cover++;
                    }
                    if (cover > bestCover)
                    {
                        bestCover = cover;
                        best = cell;
                    }
                }
            }
            return best;
        }

        /// <summary>Builds in a fixed order on the best cells, then upgrades; calls only the first wave.</summary>
        static Battle PlayWithBot(LevelDefinition level, TowerKind[] order, bool upgrade, int maxTowers = 99)
        {
            var battle = new Battle(level);
            int next = 0;
            bool called = false;
            for (int step = 0; step < 60 * 60 * 30 && !battle.IsOver; step++)
            {
                if (step % 30 == 0)
                {
                    TowerKind kind = order[next % order.Length];
                    int cost = Catalog.Tower(kind).Tiers[0].Cost;
                    TowerAgent cheapest = null;
                    if (upgrade)
                    {
                        foreach (TowerAgent tower in battle.Towers)
                        {
                            if (tower.CanUpgrade && (cheapest == null || tower.UpgradeCost < cheapest.UpgradeCost)) cheapest = tower;
                        }
                    }
                    bool preferUpgrade = cheapest != null && battle.Towers.Count >= order.Length / 2 && next % 2 == 1;
                    if (preferUpgrade && battle.Upgrade(cheapest))
                    {
                        next++;
                    }
                    else if (battle.Economy.CanAfford(cost) && battle.Towers.Count < maxTowers && (next < order.Length || !upgrade))
                    {
                        Cell cell = BestCell(battle, Catalog.Tower(kind).Tiers[0].Range);
                        if (battle.Build(kind, cell) != null) next++;
                    }
                    else if (cheapest != null && next >= order.Length)
                    {
                        battle.Upgrade(cheapest);
                    }
                    if (!called)
                    {
                        called = battle.CallNextWave();
                    }
                }
                battle.Advance(Battle.Step);
                battle.ClearEvents();
            }
            return battle;
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ASensibleDefenceWinsTheLevel(int index)
        {
            Battle battle = PlayWithBot(Levels.All[index], BuildOrder, upgrade: true);
            TestContext.WriteLine($"{Levels.All[index].Name}: {battle.State}, lives {battle.Economy.Lives}, towers {battle.Towers.Count}, score {battle.Score}");
            Assert.That(battle.State, Is.EqualTo(BattleState.Won), $"lost on wave {battle.WavesStarted} with {battle.Towers.Count} towers");
        }

        [Test]
        public void TwoBallistasAloneDoNotHoldTheLastLevel()
        {
            Battle battle = PlayWithBot(Levels.All[2], new[] { TowerKind.Ballista }, upgrade: false, maxTowers: 2);
            Assert.That(battle.State, Is.EqualTo(BattleState.Lost));
        }
    }
}
