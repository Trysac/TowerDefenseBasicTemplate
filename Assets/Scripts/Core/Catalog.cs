using System;

namespace SaucerSiege.Core
{
    public enum EnemyKind
    {
        Scout,
        Raider,
        Tank,
        Mothership,
    }

    public enum TowerKind
    {
        Ballista,
        Cannon,
        Frost,
        Mortar,
    }

    /// <summary>What an enemy type is like. Speeds are in tiles per second.</summary>
    public sealed class EnemyStats
    {
        public EnemyStats(string name, int health, float speed, int armor, int bounty, int leakDamage, float radius)
        {
            Name = name;
            Health = health;
            Speed = speed;
            Armor = armor;
            Bounty = bounty;
            LeakDamage = leakDamage;
            Radius = radius;
        }

        public string Name { get; }
        public int Health { get; }
        public float Speed { get; }
        /// <summary>Taken off every hit, but a hit always deals at least 1.</summary>
        public int Armor { get; }
        public int Bounty { get; }
        /// <summary>Lives the base loses when this enemy reaches it.</summary>
        public int LeakDamage { get; }
        public float Radius { get; }
    }

    /// <summary>One level of a tower. Costs are what that level costs to buy or upgrade to.</summary>
    public sealed class TowerTier
    {
        public TowerTier(int cost, int damage, float range, float cooldown, float shotSpeed,
            float splash = 0f, float slowFactor = 1f, float slowSeconds = 0f)
        {
            Cost = cost;
            Damage = damage;
            Range = range;
            Cooldown = cooldown;
            ShotSpeed = shotSpeed;
            Splash = splash;
            SlowFactor = slowFactor;
            SlowSeconds = slowSeconds;
        }

        public int Cost { get; }
        public int Damage { get; }
        public float Range { get; }
        public float Cooldown { get; }
        public float ShotSpeed { get; }
        /// <summary>Radius of the blast; 0 means the shot hits only its target.</summary>
        public float Splash { get; }
        /// <summary>Speed multiplier applied to enemies it hits (1 = no slow).</summary>
        public float SlowFactor { get; }
        public float SlowSeconds { get; }

        /// <summary>Damage per second against an unarmoured enemy (for the HUD).</summary>
        public float DamagePerSecond => Damage / Cooldown;
    }

    public sealed class TowerStats
    {
        public TowerStats(string name, string role, bool homing, TowerTier[] tiers)
        {
            Name = name;
            Role = role;
            Homing = homing;
            Tiers = tiers;
        }

        public string Name { get; }
        public string Role { get; }
        /// <summary>Homing shots follow their target; the others fly to where it will be.</summary>
        public bool Homing { get; }
        public TowerTier[] Tiers { get; }
        public int MaxTier => Tiers.Length - 1;
    }

    /// <summary>Every number that shapes the game, in one place.</summary>
    public static class Catalog
    {
        public const int StartingLives = 20;
        /// <summary>Percentage of everything spent on a tower that selling it gives back.</summary>
        public const int SellRefundPercent = 70;
        /// <summary>Seconds between the end of a wave's spawning and the next wave starting by itself.</summary>
        public const float WaveCountdown = 14f;
        /// <summary>Money for each whole second left on the countdown when a wave is called early.</summary>
        public const int EarlyCallBonusPerSecond = 2;
        public const int ScorePerBounty = 10;
        public const int ScorePerLifeLeft = 100;

        static readonly EnemyStats[] Enemies =
        {
            new EnemyStats("Scout", health: 6, speed: 2.1f, armor: 0, bounty: 3, leakDamage: 1, radius: 0.22f),
            new EnemyStats("Raider", health: 15, speed: 1.35f, armor: 1, bounty: 5, leakDamage: 1, radius: 0.27f),
            new EnemyStats("Tank", health: 42, speed: 0.85f, armor: 4, bounty: 11, leakDamage: 2, radius: 0.33f),
            new EnemyStats("Mothership", health: 420, speed: 0.55f, armor: 3, bounty: 80, leakDamage: 10, radius: 0.5f),
        };

        static readonly TowerStats[] Towers =
        {
            new TowerStats("Ballista", "Fast bolts, long reach", homing: true, new[]
            {
                new TowerTier(cost: 45, damage: 3, range: 2.6f, cooldown: 0.55f, shotSpeed: 14f),
                new TowerTier(cost: 50, damage: 5, range: 2.9f, cooldown: 0.47f, shotSpeed: 15f),
                new TowerTier(cost: 85, damage: 8, range: 3.3f, cooldown: 0.38f, shotSpeed: 16f),
            }),
            new TowerStats("Cannon", "Shells, small blast", homing: false, new[]
            {
                new TowerTier(cost: 75, damage: 7, range: 2.3f, cooldown: 1.15f, shotSpeed: 8f, splash: 0.7f),
                new TowerTier(cost: 70, damage: 11, range: 2.5f, cooldown: 1.05f, shotSpeed: 8.5f, splash: 0.85f),
                new TowerTier(cost: 125, damage: 18, range: 2.8f, cooldown: 0.95f, shotSpeed: 9f, splash: 1f),
            }),
            new TowerStats("Frost", "Chills and slows", homing: true, new[]
            {
                new TowerTier(cost: 60, damage: 1, range: 2.2f, cooldown: 0.8f, shotSpeed: 12f, slowFactor: 0.55f, slowSeconds: 1.4f),
                new TowerTier(cost: 55, damage: 2, range: 2.45f, cooldown: 0.7f, shotSpeed: 12f, splash: 0.45f, slowFactor: 0.45f, slowSeconds: 1.8f),
                new TowerTier(cost: 90, damage: 3, range: 2.7f, cooldown: 0.6f, shotSpeed: 13f, splash: 0.65f, slowFactor: 0.35f, slowSeconds: 2.2f),
            }),
            new TowerStats("Mortar", "Huge blast, long reach", homing: false, new[]
            {
                new TowerTier(cost: 120, damage: 12, range: 3.6f, cooldown: 2.3f, shotSpeed: 5f, splash: 1.2f),
                new TowerTier(cost: 100, damage: 19, range: 4f, cooldown: 2.1f, shotSpeed: 5.3f, splash: 1.4f),
                new TowerTier(cost: 165, damage: 30, range: 4.5f, cooldown: 1.9f, shotSpeed: 5.6f, splash: 1.6f),
            }),
        };

        public static EnemyStats Enemy(EnemyKind kind) => Enemies[(int)kind];

        public static TowerStats Tower(TowerKind kind) => Towers[(int)kind];

        public static int TowerKindCount => Towers.Length;

        /// <summary>Damage a hit deals through armour: never less than 1.</summary>
        public static int ArmoredDamage(int damage, int armor) => Math.Max(1, damage - Math.Max(0, armor));

        /// <summary>What selling gives back for a tower that cost <paramref name="invested"/> in total.</summary>
        public static int SellValue(int invested) => invested * SellRefundPercent / 100;

        /// <summary>Money for clearing wave <paramref name="waveNumber"/> (1-based).</summary>
        public static int WaveClearBonus(int waveNumber) => 10 + 3 * waveNumber;

        /// <summary>Stars for a won level: 3 with no lives lost, 2 with at least half, otherwise 1.</summary>
        public static int Stars(int livesLeft, int startingLives)
        {
            if (livesLeft <= 0) return 0;
            if (livesLeft >= startingLives) return 3;
            return livesLeft * 2 >= startingLives ? 2 : 1;
        }
    }
}
