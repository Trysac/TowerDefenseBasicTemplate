using System;
using System.Collections.Generic;

namespace SaucerSiege.Core
{
    /// <summary>The best a player has done on one level.</summary>
    public readonly struct LevelRecord : IEquatable<LevelRecord>
    {
        public LevelRecord(int bestScore, int stars, bool cleared)
        {
            BestScore = Math.Max(0, bestScore);
            Stars = Math.Max(0, Math.Min(3, stars));
            Cleared = cleared;
        }

        public int BestScore { get; }
        public int Stars { get; }
        public bool Cleared { get; }

        public bool Equals(LevelRecord other) => BestScore == other.BestScore && Stars == other.Stars && Cleared == other.Cleared;
        public override bool Equals(object obj) => obj is LevelRecord other && Equals(other);
        public override int GetHashCode() => (BestScore * 397) ^ (Stars * 7) ^ (Cleared ? 1 : 0);
        public override string ToString() => $"best {BestScore}, {Stars} stars, cleared {Cleared}";
    }

    /// <summary>Where records are kept (PlayerPrefs in the game, memory in tests).</summary>
    public interface IRecordStore
    {
        LevelRecord Load(int level);
        void Save(int level, LevelRecord record);
    }

    public sealed class MemoryRecordStore : IRecordStore
    {
        readonly Dictionary<int, LevelRecord> records = new Dictionary<int, LevelRecord>();

        public LevelRecord Load(int level) => records.TryGetValue(level, out LevelRecord record) ? record : default;

        public void Save(int level, LevelRecord record) => records[level] = record;
    }

    /// <summary>What a finished battle earned.</summary>
    public readonly struct BattleResult
    {
        public BattleResult(int level, bool won, int score, int stars, int wavesReached, bool newBest)
        {
            Level = level;
            Won = won;
            Score = score;
            Stars = stars;
            WavesReached = wavesReached;
            NewBest = newBest;
        }

        public int Level { get; }
        public bool Won { get; }
        public int Score { get; }
        public int Stars { get; }
        public int WavesReached { get; }
        /// <summary>True when the score beat the level's previous best.</summary>
        public bool NewBest { get; }
    }

    /// <summary>Records per level and which levels are open: the first always, each next one once the previous is cleared.</summary>
    public sealed class Progress
    {
        readonly IRecordStore store;

        public Progress(IRecordStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public LevelRecord Record(int level) => store.Load(level);

        public bool IsUnlocked(int level) => level == 0 || (level > 0 && level < Levels.All.Count && store.Load(level - 1).Cleared);

        public int TotalStars
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Levels.All.Count; i++) total += store.Load(i).Stars;
                return total;
            }
        }

        /// <summary>Keeps the best score and stars ever reached; a loss can still set a best score.</summary>
        public BattleResult Submit(int level, Battle battle)
        {
            bool won = battle.State == BattleState.Won;
            int stars = battle.Stars;
            LevelRecord old = store.Load(level);
            bool newBest = battle.Score > old.BestScore;
            var updated = new LevelRecord(Math.Max(old.BestScore, battle.Score), Math.Max(old.Stars, stars), old.Cleared || won);
            if (!updated.Equals(old))
            {
                store.Save(level, updated);
            }
            return new BattleResult(level, won, battle.Score, stars, battle.WavesStarted, newBest);
        }
    }
}
