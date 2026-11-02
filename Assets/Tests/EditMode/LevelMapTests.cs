using System;
using NUnit.Framework;

namespace SaucerSiege.Core.Tests
{
    public class LevelMapTests
    {
        static readonly string[] Small =
        {
            "S##.",
            "..#.",
            "T.#.",
            "..##",
            "...B",
        };

        [Test]
        public void TheRouteRunsFromTheSpawnToTheBaseInOrder()
        {
            var map = new LevelMap(Small);
            Assert.That(map.Cols, Is.EqualTo(4));
            Assert.That(map.Rows, Is.EqualTo(5));
            Assert.That(map.Route, Is.EqualTo(new[]
            {
                new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(2, 1), new Cell(2, 2),
                new Cell(2, 3), new Cell(3, 3), new Cell(3, 4),
            }));
            Assert.That(map.Spawn, Is.EqualTo(new Cell(0, 0)));
            Assert.That(map.BaseCell, Is.EqualTo(new Cell(3, 4)));
        }

        [Test]
        public void OnlyOpenGroundIsBuildable()
        {
            var map = new LevelMap(Small);
            Assert.That(map.IsBuildable(new Cell(0, 1)), Is.True);
            Assert.That(map.IsBuildable(new Cell(1, 0)), Is.False, "path");
            Assert.That(map.IsBuildable(new Cell(0, 2)), Is.False, "tree");
            Assert.That(map.IsBuildable(new Cell(3, 4)), Is.False, "base");
            Assert.That(map.IsBuildable(new Cell(-1, 0)), Is.False, "outside");
            Assert.That(map[new Cell(0, 2)], Is.EqualTo(TileType.Tree));
        }

        [Test]
        public void CellsAndGroundPointsConvertBothWays()
        {
            var map = new LevelMap(Small);
            Assert.That(map.Center(new Cell(0, 0)), Is.EqualTo(new Vec2(0f, 4f)), "row 0 is the far edge");
            Assert.That(map.CellAt(new Vec2(1.4f, 0.6f)), Is.EqualTo(new Cell(1, 3)));
            Assert.That(map.CellAt(map.Center(new Cell(3, 1))), Is.EqualTo(new Cell(3, 1)));
        }

        [Test]
        public void ThePathFollowsTheRouteCentres()
        {
            PathLine path = new LevelMap(Small).BuildPath();
            Assert.That(path.Length, Is.EqualTo(7f).Within(1e-4f));
            Assert.That(path.PositionAt(0f), Is.EqualTo(new Vec2(0f, 4f)));
            Assert.That(path.PositionAt(2.5f), Is.EqualTo(new Vec2(2f, 3.5f)));
            Assert.That(path.DirectionAt(2.5f), Is.EqualTo(new Vec2(0f, -1f)));
            Assert.That(path.PositionAt(99f), Is.EqualTo(new Vec2(3f, 0f)), "clamped to the base");
        }

        [Test]
        public void BrokenMapsAreRejectedWithAReason()
        {
            Assert.That(() => new LevelMap(new[] { "S#.", "##B" }), Throws.ArgumentException.With.Message.Contains("branches"));
            Assert.That(() => new LevelMap(new[] { "S#.", "..B" }), Throws.ArgumentException.With.Message.Contains("stops"));
            Assert.That(() => new LevelMap(new[] { "..#B" }), Throws.ArgumentException.With.Message.Contains("spawn"));
            Assert.That(() => new LevelMap(new[] { "S#B", "." }), Throws.ArgumentException.With.Message.Contains("wide"));
            Assert.That(() => new LevelMap(new[] { "S#X" }), Throws.ArgumentException.With.Message.Contains("'X'"));
        }

        [Test]
        public void EveryShippedLevelParsesAndHasRoomToBuild()
        {
            foreach (LevelDefinition level in Levels.All)
            {
                var map = new LevelMap(level.Rows);
                int buildable = 0;
                for (int r = 0; r < map.Rows; r++)
                {
                    for (int c = 0; c < map.Cols; c++)
                    {
                        if (map.IsBuildable(new Cell(c, r))) buildable++;
                    }
                }
                Assert.That(map.Route.Count, Is.GreaterThan(20), level.Name);
                Assert.That(buildable, Is.GreaterThan(40), level.Name);
                Assert.That(level.Waves.Count, Is.GreaterThanOrEqualTo(8), level.Name);
                Assert.That(level.Waves[level.Waves.Count - 1].Count(EnemyKind.Mothership), Is.EqualTo(1), level.Name);
            }
        }

        [Test]
        public void WavesGrowAndBringNewEnemiesInOrder()
        {
            WaveDefinition[] waves = Levels.Plan(8, 0.1f);
            Assert.That(waves[0].Count(EnemyKind.Scout), Is.EqualTo(6));
            Assert.That(waves[0].Count(EnemyKind.Raider), Is.EqualTo(0));
            Assert.That(waves[1].Count(EnemyKind.Raider), Is.EqualTo(3));
            Assert.That(waves[2].Count(EnemyKind.Tank), Is.EqualTo(0));
            Assert.That(waves[3].Count(EnemyKind.Tank), Is.EqualTo(1));
            Assert.That(waves[2].Count(EnemyKind.Scout), Is.EqualTo(10 + 9), "the third wave adds a scout rush");
            Assert.That(waves[7].HealthScale, Is.EqualTo(1.7f).Within(1e-4f));
            Assert.That(waves[7].HealthOf(EnemyKind.Raider), Is.EqualTo((int)Math.Round(15 * 1.7)));
        }
    }
}
