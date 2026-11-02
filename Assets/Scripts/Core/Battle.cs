using System;
using System.Collections.Generic;

namespace SaucerSiege.Core
{
    public enum BattleState
    {
        /// <summary>Before the first wave: build, then call the wave when ready.</summary>
        Preparing,
        Fighting,
        Won,
        Lost,
    }

    public enum BattleEventType
    {
        WaveStarted,
        WaveCleared,
        EnemySpawned,
        EnemyHit,
        EnemyKilled,
        EnemyLeaked,
        TowerFired,
        ShotLanded,
        TowerBuilt,
        TowerUpgraded,
        TowerSold,
        Won,
        Lost,
    }

    /// <summary>Something that happened during a step, for the view to animate and play sounds.</summary>
    public readonly struct BattleEvent
    {
        public BattleEvent(BattleEventType type, Vec2 position, int amount = 0, int enemyId = 0,
            EnemyKind enemy = EnemyKind.Scout, TowerKind tower = TowerKind.Ballista, float radius = 0f)
        {
            Type = type;
            Position = position;
            Amount = amount;
            EnemyId = enemyId;
            Enemy = enemy;
            Tower = tower;
            Radius = radius;
        }

        public BattleEventType Type { get; }
        public Vec2 Position { get; }
        /// <summary>Damage, money, lives or wave number, depending on the type.</summary>
        public int Amount { get; }
        public int EnemyId { get; }
        public EnemyKind Enemy { get; }
        public TowerKind Tower { get; }
        public float Radius { get; }
    }

    /// <summary>
    /// One level being played: the waves, the saucers on the road, the towers and their shots,
    /// the money and the base. It advances in fixed steps, so a speed-up or a slow frame plays
    /// out exactly like normal speed.
    /// </summary>
    public sealed class Battle
    {
        public const float Step = 1f / 60f;
        /// <summary>At most this many steps run per call; the rest carries over to the next call.</summary>
        const int MaxStepsPerAdvance = 90;

        sealed class ActiveWave
        {
            public int Number;
            public WaveDefinition Definition;
            public WaveRun Run;
            public bool Cleared;
        }

        readonly List<ActiveWave> waves = new List<ActiveWave>();
        readonly List<EnemyAgent> enemies = new List<EnemyAgent>();
        readonly List<TowerAgent> towers = new List<TowerAgent>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<BattleEvent> events = new List<BattleEvent>();
        readonly List<WaveSpawn> spawns = new List<WaveSpawn>();
        readonly List<TargetCandidate> candidates = new List<TargetCandidate>();
        readonly List<EnemyAgent> victims = new List<EnemyAgent>();
        float accumulator;
        int nextEnemyId = 1;
        int nextShotId = 1;

        public Battle(LevelDefinition level)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            Map = new LevelMap(level.Rows);
            Path = Map.BuildPath();
            Economy = new Economy(level.StartingMoney, Catalog.StartingLives);
            Countdown = -1f;
        }

        public LevelDefinition Level { get; }
        public LevelMap Map { get; }
        public PathLine Path { get; }
        public Economy Economy { get; }
        public BattleState State { get; private set; }
        public bool IsOver => State == BattleState.Won || State == BattleState.Lost;

        /// <summary>Seconds of play so far.</summary>
        public float Time { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int Leaks { get; private set; }

        public int WavesStarted { get; private set; }
        public int TotalWaves => Level.Waves.Count;

        /// <summary>The wave that comes next, or null once every wave has started.</summary>
        public WaveDefinition NextWave => WavesStarted < TotalWaves ? Level.Waves[WavesStarted] : null;

        /// <summary>Seconds until the next wave starts by itself, or -1 while nothing is counting down.</summary>
        public float Countdown { get; private set; }

        public bool CanCallWave => !IsOver && WavesStarted < TotalWaves;

        /// <summary>Money paid for calling the next wave now, before its countdown ends.</summary>
        public int EarlyCallBonus => Countdown > 0f ? (int)Math.Ceiling(Countdown) * Catalog.EarlyCallBonusPerSecond : 0;

        public IReadOnlyList<EnemyAgent> Enemies => enemies;
        public IReadOnlyList<TowerAgent> Towers => towers;
        public IReadOnlyList<Shot> Shots => shots;
        public IReadOnlyList<BattleEvent> Events => events;

        /// <summary>Stars earned so far (0 until the battle is won).</summary>
        public int Stars => State == BattleState.Won ? Catalog.Stars(Economy.Lives, Economy.StartingLives) : 0;

        public void ClearEvents() => events.Clear();

        public EnemyAgent FindEnemy(int id)
        {
            foreach (EnemyAgent enemy in enemies)
            {
                if (enemy.Id == id) return enemy;
            }
            return null;
        }

        public TowerAgent TowerAt(Cell cell)
        {
            foreach (TowerAgent tower in towers)
            {
                if (tower.Cell == cell) return tower;
            }
            return null;
        }

        // ---- Player actions ----

        /// <summary>Starts the next wave now; during a countdown this pays <see cref="EarlyCallBonus"/>.</summary>
        public bool CallNextWave()
        {
            if (!CanCallWave)
            {
                return false;
            }
            int bonus = EarlyCallBonus;
            if (bonus > 0)
            {
                Economy.Earn(bonus);
            }
            StartWave();
            return true;
        }

        public bool CanBuild(TowerKind kind, Cell cell) =>
            !IsOver && Map.IsBuildable(cell) && TowerAt(cell) == null && Economy.CanAfford(Catalog.Tower(kind).Tiers[0].Cost);

        /// <summary>Buys and places a tower, or returns null when the cell is taken, blocked or too expensive.</summary>
        public TowerAgent Build(TowerKind kind, Cell cell)
        {
            if (!CanBuild(kind, cell) || !Economy.TrySpend(Catalog.Tower(kind).Tiers[0].Cost))
            {
                return null;
            }
            var tower = new TowerAgent(kind, cell, Map.Center(cell));
            towers.Add(tower);
            events.Add(new BattleEvent(BattleEventType.TowerBuilt, tower.Position, Catalog.Tower(kind).Tiers[0].Cost, tower: kind));
            return tower;
        }

        public bool CanUpgrade(TowerAgent tower) =>
            !IsOver && tower != null && towers.Contains(tower) && tower.CanUpgrade && Economy.CanAfford(tower.UpgradeCost);

        public bool Upgrade(TowerAgent tower)
        {
            if (!CanUpgrade(tower))
            {
                return false;
            }
            int cost = tower.UpgradeCost;
            Economy.TrySpend(cost);
            tower.Tier++;
            tower.Invested += cost;
            events.Add(new BattleEvent(BattleEventType.TowerUpgraded, tower.Position, tower.Tier, tower: tower.Kind));
            return true;
        }

        /// <summary>Removes the tower and refunds part of what it cost. Returns the refund (0 if nothing was sold).</summary>
        public int Sell(TowerAgent tower)
        {
            if (IsOver || tower == null || !towers.Remove(tower))
            {
                return 0;
            }
            int refund = tower.SellValue;
            Economy.Earn(refund);
            events.Add(new BattleEvent(BattleEventType.TowerSold, tower.Position, refund, tower: tower.Kind));
            return refund;
        }

        public void CyclePriority(TowerAgent tower)
        {
            if (tower != null)
            {
                tower.Priority = (TargetPriority)(((int)tower.Priority + 1) % 4);
            }
        }

        // ---- Simulation ----

        /// <summary>Plays <paramref name="seconds"/> of the battle in fixed steps.</summary>
        public void Advance(float seconds)
        {
            if (IsOver || seconds <= 0f)
            {
                return;
            }
            accumulator += seconds;
            int steps = 0;
            while (accumulator >= Step && steps < MaxStepsPerAdvance)
            {
                accumulator -= Step;
                steps++;
                Tick(Step);
                if (IsOver)
                {
                    accumulator = 0f;
                    return;
                }
            }
        }

        void Tick(float dt)
        {
            Time += dt;
            if (Countdown > 0f)
            {
                Countdown -= dt;
                if (Countdown <= 0f)
                {
                    StartWave();
                }
            }
            SpawnDue(dt);
            MoveEnemies(dt);
            if (IsOver)
            {
                return;
            }
            UpdateTowers(dt);
            MoveShots(dt);
            CheckWaves();
        }

        void StartWave()
        {
            var wave = new ActiveWave
            {
                Number = WavesStarted + 1,
                Definition = Level.Waves[WavesStarted],
            };
            wave.Run = new WaveRun(wave.Definition.Groups);
            waves.Add(wave);
            WavesStarted++;
            State = BattleState.Fighting;
            Countdown = -1f;
            events.Add(new BattleEvent(BattleEventType.WaveStarted, Map.Center(Map.Spawn), wave.Number));
        }

        void SpawnDue(float dt)
        {
            foreach (ActiveWave wave in waves)
            {
                if (wave.Run.SpawningDone)
                {
                    continue;
                }
                spawns.Clear();
                wave.Run.Advance(dt, spawns);
                foreach (WaveSpawn spawn in spawns)
                {
                    var kind = (EnemyKind)spawn.Kind;
                    var enemy = new EnemyAgent(nextEnemyId++, kind, wave.Definition.HealthOf(kind), wave.Number, wave)
                    {
                        Position = Path.PositionAt(0f),
                        Heading = Path.DirectionAt(0f),
                    };
                    enemies.Add(enemy);
                    events.Add(new BattleEvent(BattleEventType.EnemySpawned, enemy.Position, enemyId: enemy.Id, enemy: kind));
                }
            }
            ActiveWave latest = waves.Count > 0 ? waves[waves.Count - 1] : null;
            if (latest != null && latest.Run.SpawningDone && WavesStarted < TotalWaves && Countdown < 0f)
            {
                Countdown = Catalog.WaveCountdown;
            }
        }

        void MoveEnemies(float dt)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyAgent enemy = enemies[i];
                enemy.Distance += enemy.CurrentSpeed * dt;
                if (enemy.IsSlowed)
                {
                    enemy.SlowTimer = Math.Max(0f, enemy.SlowTimer - dt);
                }
                enemy.Position = Path.PositionAt(enemy.Distance);
                enemy.Heading = Path.DirectionAt(enemy.Distance);
                if (enemy.Distance < Path.Length)
                {
                    continue;
                }
                enemies.RemoveAt(i);
                ((ActiveWave)enemy.Wave).Run.Remove();
                Leaks++;
                bool fell = Economy.Leak(enemy.Stats.LeakDamage);
                events.Add(new BattleEvent(BattleEventType.EnemyLeaked, enemy.Position, enemy.Stats.LeakDamage, enemy.Id, enemy.Kind));
                if (fell)
                {
                    State = BattleState.Lost;
                    shots.Clear();
                    events.Add(new BattleEvent(BattleEventType.Lost, enemy.Position));
                    return;
                }
            }
        }

        void UpdateTowers(float dt)
        {
            candidates.Clear();
            foreach (EnemyAgent enemy in enemies)
            {
                candidates.Add(new TargetCandidate(enemy.Position, enemy.Distance, enemy.Health.Current));
            }
            foreach (TowerAgent tower in towers)
            {
                tower.Cooldown = Math.Max(0f, tower.Cooldown - dt);
                tower.SinceShot += dt;
                int index = Targeting.Pick(candidates, tower.Position, tower.Current.Range, tower.Priority);
                if (index < 0)
                {
                    continue;
                }
                EnemyAgent target = enemies[index];
                Vec2 aimPoint = tower.Stats.Homing ? target.Position : Lead(target, tower.Position, tower.Current.ShotSpeed);
                Vec2 toTarget = aimPoint - tower.Position;
                if (toTarget.LengthSquared > 1e-6f)
                {
                    tower.Aim = toTarget.Normalized;
                }
                if (tower.Cooldown > 0f)
                {
                    continue;
                }
                tower.Cooldown = tower.Current.Cooldown;
                tower.SinceShot = 0f;
                shots.Add(new Shot(nextShotId++, tower, target, aimPoint));
                events.Add(new BattleEvent(BattleEventType.TowerFired, tower.Position, enemyId: target.Id, tower: tower.Kind));
            }
        }

        /// <summary>Where <paramref name="enemy"/> will be when a shot from <paramref name="origin"/> reaches it.</summary>
        public Vec2 Lead(EnemyAgent enemy, Vec2 origin, float shotSpeed)
        {
            Vec2 point = enemy.Position;
            for (int i = 0; i < 5; i++)
            {
                float flight = Vec2.Distance(origin, point) / shotSpeed;
                point = Path.PositionAt(enemy.Distance + enemy.CurrentSpeed * flight);
            }
            return point;
        }

        void MoveShots(float dt)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                Shot shot = shots[i];
                if (shot.Homing)
                {
                    EnemyAgent target = FindEnemy(shot.TargetId);
                    if (target != null)
                    {
                        shot.Target = target.Position;
                    }
                }
                Vec2 toTarget = shot.Target - shot.Position;
                float distance = toTarget.Length;
                float travel = shot.Speed * dt;
                if (distance > travel)
                {
                    shot.Position += toTarget / distance * travel;
                    shot.Travelled += travel;
                    continue;
                }
                shot.Travelled += distance;
                shot.Position = shot.Target;
                shots.RemoveAt(i);
                Land(shot);
            }
        }

        void Land(Shot shot)
        {
            victims.Clear();
            if (shot.Splash > 0f)
            {
                foreach (EnemyAgent enemy in enemies)
                {
                    float reach = shot.Splash + enemy.Stats.Radius;
                    if ((enemy.Position - shot.Position).LengthSquared <= reach * reach)
                    {
                        victims.Add(enemy);
                    }
                }
            }
            else
            {
                EnemyAgent target = FindEnemy(shot.TargetId);
                if (target != null)
                {
                    victims.Add(target);
                }
            }
            events.Add(new BattleEvent(BattleEventType.ShotLanded, shot.Position, victims.Count, tower: shot.Kind, radius: shot.Splash));
            foreach (EnemyAgent enemy in victims)
            {
                Hit(enemy, shot);
            }
        }

        void Hit(EnemyAgent enemy, Shot shot)
        {
            int dealt = Catalog.ArmoredDamage(shot.Damage, enemy.Stats.Armor);
            bool killed = enemy.Health.Hit(dealt);
            enemy.Chill(shot.SlowFactor, shot.SlowSeconds);
            events.Add(new BattleEvent(BattleEventType.EnemyHit, enemy.Position, dealt, enemy.Id, enemy.Kind, shot.Kind));
            if (!killed)
            {
                return;
            }
            enemies.Remove(enemy);
            ((ActiveWave)enemy.Wave).Run.Remove();
            Kills++;
            shot.Tower.Kills++;
            Economy.Earn(enemy.Stats.Bounty);
            Score += enemy.Stats.Bounty * Catalog.ScorePerBounty;
            events.Add(new BattleEvent(BattleEventType.EnemyKilled, enemy.Position, enemy.Stats.Bounty, enemy.Id, enemy.Kind, shot.Kind));
        }

        void CheckWaves()
        {
            bool allCleared = true;
            foreach (ActiveWave wave in waves)
            {
                if (!wave.Cleared && wave.Run.IsCleared)
                {
                    wave.Cleared = true;
                    int bonus = Catalog.WaveClearBonus(wave.Number);
                    Economy.Earn(bonus);
                    events.Add(new BattleEvent(BattleEventType.WaveCleared, Map.Center(Map.BaseCell), bonus));
                }
                allCleared &= wave.Cleared;
            }
            if (allCleared && WavesStarted == TotalWaves && enemies.Count == 0)
            {
                State = BattleState.Won;
                Countdown = -1f;
                shots.Clear();
                Score += Economy.Lives * Catalog.ScorePerLifeLeft;
                events.Add(new BattleEvent(BattleEventType.Won, Map.Center(Map.BaseCell), Score));
            }
        }
    }
}
