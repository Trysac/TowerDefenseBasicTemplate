using NUnit.Framework;

namespace SaucerSiege.Core.Tests
{
    public class TargetingTests
    {
        static readonly TargetCandidate[] Pack =
        {
            new TargetCandidate(new Vec2(3f, 0f), progress: 4f, health: 5),
            new TargetCandidate(new Vec2(1f, 0f), progress: 9f, health: 2),
            new TargetCandidate(new Vec2(0f, 2f), progress: 1f, health: 8),
        };

        [TestCase(TargetPriority.First, 1)]
        [TestCase(TargetPriority.Last, 2)]
        [TestCase(TargetPriority.Close, 1)]
        [TestCase(TargetPriority.Strong, 2)]
        public void EachPriorityPicksItsEnemy(TargetPriority priority, int expected)
        {
            Assert.That(Targeting.Pick(Pack, Vec2.Zero, 5f, priority), Is.EqualTo(expected));
        }

        [Test]
        public void EnemiesOutOfRangeAreIgnored()
        {
            Assert.That(Targeting.Pick(Pack, Vec2.Zero, 2.5f, TargetPriority.Strong), Is.EqualTo(2));
            Assert.That(Targeting.Pick(Pack, Vec2.Zero, 1.5f, TargetPriority.Strong), Is.EqualTo(1));
            Assert.That(Targeting.Pick(Pack, Vec2.Zero, 0.5f, TargetPriority.First), Is.EqualTo(-1));
        }

        [Test]
        public void NoCandidatesMeansNoTarget()
        {
            Assert.That(Targeting.Pick(new TargetCandidate[0], Vec2.Zero, 5f, TargetPriority.Close), Is.EqualTo(-1));
        }

        [Test]
        public void CloseFindsTheNearestEnemyAtAnyDistance()
        {
            // The nearest search started at 99 (squared metres), so a long-range tower ignored
            // every enemy more than about 10 m away.
            var far = new[] { new TargetCandidate(new Vec2(14f, 0f), 1f, 1), new TargetCandidate(new Vec2(12f, 0f), 1f, 1) };
            Assert.That(Targeting.Pick(far, Vec2.Zero, 20f, TargetPriority.Close), Is.EqualTo(1));
        }
    }
}
