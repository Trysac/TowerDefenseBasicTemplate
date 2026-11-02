using System.Collections.Generic;
using NUnit.Framework;

namespace SaucerSiege.Core.Tests
{
    public class WaveRunTests
    {
        static List<WaveSpawn> Step(WaveRun run, float seconds)
        {
            var spawns = new List<WaveSpawn>();
            run.Advance(seconds, spawns);
            return spawns;
        }

        [Test]
        public void GroupsSpawnAfterTheirDelayAndAtTheirInterval()
        {
            var run = new WaveRun(new[]
            {
                new WaveGroup(kind: 7, count: 3, delay: 1f, interval: 0.5f),
                new WaveGroup(kind: 9, count: 1, delay: 2f, interval: 0.5f),
            });
            Assert.That(run.Planned, Is.EqualTo(4));
            Assert.That(Step(run, 0.99f), Is.Empty);
            List<WaveSpawn> first = Step(run, 0.01f);
            Assert.That(first.Count, Is.EqualTo(1));
            Assert.That(first[0].Kind, Is.EqualTo(7));
            Assert.That(first[0].Group, Is.EqualTo(0));
            Assert.That(Step(run, 1f).Count, Is.EqualTo(2), "spawns at 1.5 s and 2.0 s");
            // The second group starts 2 s after the first group's last interval (2.5 s): at 4.5 s.
            Assert.That(Step(run, 2.49f), Is.Empty);
            List<WaveSpawn> second = Step(run, 0.01f);
            Assert.That(second.Count, Is.EqualTo(1));
            Assert.That(second[0].Kind, Is.EqualTo(9));
            Assert.That(run.Spawned, Is.EqualTo(4));
            Assert.That(run.SpawningDone, Is.True);
        }

        [Test]
        public void ALongFrameSpawnsEverythingThatWasDue()
        {
            var run = new WaveRun(new[] { new WaveGroup(0, 5, 0f, 0.1f) });
            Assert.That(Step(run, 10f).Count, Is.EqualTo(5));
            Assert.That(run.Alive, Is.EqualTo(5));
        }

        [Test]
        public void EmptyGroupsOnlyAddTheirDelay()
        {
            var run = new WaveRun(new[] { new WaveGroup(0, 0, 1f, 0.5f), new WaveGroup(1, 1, 1f, 0.5f) });
            Assert.That(Step(run, 1.99f), Is.Empty);
            Assert.That(Step(run, 0.01f).Count, Is.EqualTo(1));
        }

        [Test]
        public void TheWaveIsClearedWhenEveryEnemyIsGone()
        {
            var run = new WaveRun(new[] { new WaveGroup(0, 2, 0f, 0.1f) });
            Step(run, 1f);
            Assert.That(run.IsCleared, Is.False);
            run.Remove();
            Assert.That(run.IsCleared, Is.False);
            run.Remove();
            Assert.That(run.Alive, Is.EqualTo(0));
            Assert.That(run.IsCleared, Is.True);
        }

        [Test]
        public void TheWaveIsNotClearedWhileGroupsAreStillToCome()
        {
            // Killing the first group before the second one arrived used to end the wave
            // (and the last wave counted as a win) while enemies were still due.
            var run = new WaveRun(new[] { new WaveGroup(0, 1, 0f, 0.5f), new WaveGroup(1, 1, 3f, 0.5f) });
            Step(run, 0.1f);
            run.Remove();
            Assert.That(run.Alive, Is.EqualTo(0));
            Assert.That(run.SpawningDone, Is.False);
            Assert.That(run.IsCleared, Is.False);
            Step(run, 3.4f);
            run.Remove();
            Assert.That(run.IsCleared, Is.True);
        }

        [Test]
        public void RemovingMoreEnemiesThanAliveNeverGoesBelowZero()
        {
            var run = new WaveRun(new[] { new WaveGroup(0, 1, 0f, 0.5f) });
            Step(run, 0.1f);
            run.Remove();
            run.Remove();
            Assert.That(run.Alive, Is.EqualTo(0));
            Assert.That(run.IsCleared, Is.True);
        }
    }
}
