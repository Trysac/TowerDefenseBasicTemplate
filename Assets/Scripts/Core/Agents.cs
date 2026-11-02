namespace SaucerSiege.Core
{
    /// <summary>A saucer on the field.</summary>
    public sealed class EnemyAgent
    {
        internal EnemyAgent(int id, EnemyKind kind, int health, int waveNumber, object wave)
        {
            Id = id;
            Kind = kind;
            Stats = Catalog.Enemy(kind);
            Health = new EnemyHealth(health);
            WaveNumber = waveNumber;
            Wave = wave;
        }

        public int Id { get; }
        public EnemyKind Kind { get; }
        public EnemyStats Stats { get; }
        public EnemyHealth Health { get; }
        public int WaveNumber { get; }
        internal object Wave { get; }

        /// <summary>Distance travelled along the path.</summary>
        public float Distance { get; internal set; }
        public Vec2 Position { get; internal set; }
        /// <summary>Unit direction of travel.</summary>
        public Vec2 Heading { get; internal set; }

        public float SlowTimer { get; internal set; }
        public float SlowFactor { get; internal set; } = 1f;
        public bool IsSlowed => SlowTimer > 0f;

        public float CurrentSpeed => Stats.Speed * (IsSlowed ? SlowFactor : 1f);

        /// <summary>Applies a chill: the strongest slow wins and the longest duration is kept.</summary>
        internal void Chill(float factor, float seconds)
        {
            if (seconds <= 0f || factor >= 1f)
            {
                return;
            }
            SlowFactor = IsSlowed ? System.Math.Min(SlowFactor, factor) : factor;
            SlowTimer = System.Math.Max(SlowTimer, seconds);
        }
    }

    /// <summary>A tower standing on a cell.</summary>
    public sealed class TowerAgent
    {
        internal TowerAgent(TowerKind kind, Cell cell, Vec2 position)
        {
            Kind = kind;
            Cell = cell;
            Position = position;
            Stats = Catalog.Tower(kind);
            Invested = Stats.Tiers[0].Cost;
            Aim = new Vec2(0f, -1f);
            SinceShot = 99f;
        }

        public TowerKind Kind { get; }
        public Cell Cell { get; }
        public Vec2 Position { get; }
        public TowerStats Stats { get; }

        /// <summary>0-based level; upgrades raise it up to <see cref="TowerStats.MaxTier"/>.</summary>
        public int Tier { get; internal set; }
        public TargetPriority Priority { get; internal set; } = TargetPriority.First;
        public int Invested { get; internal set; }
        public int Kills { get; internal set; }

        /// <summary>Direction the weapon faces.</summary>
        public Vec2 Aim { get; internal set; }
        public float Cooldown { get; internal set; }
        /// <summary>Seconds since the last shot (for recoil).</summary>
        public float SinceShot { get; internal set; }

        public TowerTier Current => Stats.Tiers[Tier];
        public bool CanUpgrade => Tier < Stats.MaxTier;
        public TowerTier Next => CanUpgrade ? Stats.Tiers[Tier + 1] : null;
        public int UpgradeCost => CanUpgrade ? Stats.Tiers[Tier + 1].Cost : 0;
        public int SellValue => Catalog.SellValue(Invested);
    }

    /// <summary>A projectile in flight.</summary>
    public sealed class Shot
    {
        internal Shot(int id, TowerAgent tower, EnemyAgent target, Vec2 aimPoint)
        {
            Id = id;
            Tower = tower;
            Kind = tower.Kind;
            TowerTier tier = tower.Current;
            Damage = tier.Damage;
            Splash = tier.Splash;
            SlowFactor = tier.SlowFactor;
            SlowSeconds = tier.SlowSeconds;
            Speed = tier.ShotSpeed;
            Homing = tower.Stats.Homing;
            TargetId = target.Id;
            Origin = tower.Position;
            Position = tower.Position;
            Target = aimPoint;
        }

        public int Id { get; }
        public TowerKind Kind { get; }
        internal TowerAgent Tower { get; }
        public int Damage { get; }
        public float Splash { get; }
        public float SlowFactor { get; }
        public float SlowSeconds { get; }
        public float Speed { get; }
        public bool Homing { get; }
        public int TargetId { get; }
        public Vec2 Origin { get; }
        public Vec2 Position { get; internal set; }
        /// <summary>Where the shot is heading: the target itself, or where it will be for lobbed shots.</summary>
        public Vec2 Target { get; internal set; }
        public float Travelled { get; internal set; }

        /// <summary>0 at launch, 1 on impact (lobbed shots use it for their arc).</summary>
        public float Progress
        {
            get
            {
                float total = Travelled + Vec2.Distance(Position, Target);
                return total > 1e-5f ? Travelled / total : 1f;
            }
        }
    }
}
