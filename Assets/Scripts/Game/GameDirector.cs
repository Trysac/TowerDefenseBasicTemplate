using System.Collections.Generic;
using SaucerSiege.Core;
using UnityEngine;
using Screen = SaucerSiege.Core.Screen;

namespace SaucerSiege.Game
{
    /// <summary>
    /// Runs the game: reads input, drives the <see cref="Session"/>, keeps the 3D views in step
    /// with the battle, turns battle events into effects and sounds, and feeds the HUD.
    /// All rules live in SaucerSiege.Core; this class only presents them.
    /// </summary>
    public sealed class GameDirector : MonoBehaviour
    {
        static readonly Color[] KindColors =
        {
            new Color(0.5f, 0.9f, 0.35f), new Color(1f, 0.42f, 0.36f), new Color(0.7f, 0.5f, 1f), new Color(1f, 0.82f, 0.25f),
        };

        [SerializeField] GameArt art;
        [SerializeField] Hud hud;
        [SerializeField] CameraRig cameraRig;

        readonly GameInput input = new GameInput();
        readonly Dictionary<TowerAgent, TowerView> towerViews = new Dictionary<TowerAgent, TowerView>();
        readonly Dictionary<int, EnemyView> enemyViews = new Dictionary<int, EnemyView>();
        readonly Dictionary<int, Transform> shotViews = new Dictionary<int, Transform>();
        readonly HashSet<int> seen = new HashSet<int>();
        readonly List<int> stale = new List<int>();
        readonly List<TowerAgent> staleTowers = new List<TowerAgent>();
        MaterialPropertyBlock block;

        Session session;
        AudioDirector audioDirector;
        Board board;
        LevelDefinition boardLevel;
        Battle shownBattle;
        Screen? shownScreen;
        Transform actors;
        Transform highlight;
        Renderer highlightRenderer;
        Transform rangeRing;
        Renderer rangeRenderer;
        Material frostMaterial;
        Mesh ringMesh;
        Cell cursor;
        bool cursorOnBoard;
        int levelHighlight;

        public Session Session => session;
        public Hud Hud => hud;
        public Board Board => board;
        public TowerKind? Placing { get; private set; }
        public TowerAgent Selected { get; private set; }
        public Cell Cursor => cursor;
        public int EnemyViewCount => enemyViews.Count;
        public int ShotViewCount => shotViews.Count;
        public int TowerViewCount => towerViews.Count;
        public int LevelHighlight => levelHighlight;

        /// <summary>When true, real devices are ignored (tests and the screenshot hook drive the game).</summary>
        public bool ScriptedInput { get; set; }

        public EnemyView ViewOf(int enemyId) => enemyViews.TryGetValue(enemyId, out EnemyView view) ? view : null;

        void Awake()
        {
            Application.targetFrameRate = 60;
            block = new MaterialPropertyBlock();
            audioDirector = gameObject.AddComponent<AudioDirector>();
            audioDirector.Init();
            session = new Session(new PlayerPrefsRecordStore());
            hud.Init(cameraRig.View);
            Wire();
            actors = new GameObject("Actors").transform;
            ringMesh = Shapes.Ring(0.92f);
            highlight = Shapes.Part("Cursor", actors, art.QuadMesh, art.Overlay, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.94f, 0.94f, 1f), shadows: false);
            highlightRenderer = highlight.GetComponent<Renderer>();
            rangeRing = Shapes.Part("Range", actors, ringMesh, art.Overlay, Vector3.zero, Vector3.zero, Vector3.one, shadows: false);
            rangeRenderer = rangeRing.GetComponent<Renderer>();
            frostMaterial = art.ParticleAlpha;
        }

        void Wire()
        {
            hud.PlayClicked += Play;
            hud.BackClicked += ToTitle;
            hud.LevelClicked += index => TryStartLevel(index);
            hud.PauseClicked += TogglePause;
            hud.ResumeClicked += TogglePause;
            hud.RestartClicked += Restart;
            hud.LevelsClicked += ToLevelSelect;
            hud.NextClicked += NextLevel;
            hud.CallWaveClicked += () => CallWave();
            hud.SpeedClicked += CycleSpeed;
            hud.TowerClicked += index => ToggleBuild((TowerKind)index);
            hud.UpgradeClicked += () => UpgradeSelected();
            hud.SellClicked += () => SellSelected();
            hud.TargetClicked += CycleTargetSelected;
            hud.MuteClicked += ToggleMute;
            hud.MusicChanged += value => audioDirector.SetMusicVolume(value);
            hud.EffectsChanged += value => audioDirector.SetEffectsVolume(value);
        }

        /// <summary>Swaps the saved records for an in-memory store (tests and screenshots never touch PlayerPrefs).</summary>
        public void UseMemoryStore(IRecordStore store = null)
        {
            session = new Session(store ?? new MemoryRecordStore());
            shownScreen = null;
            levelHighlight = 0;
        }

        // ---- Screen actions (also used by the HUD buttons, the tests and the screenshot hook) ----

        public void Play()
        {
            Click();
            session.ToLevelSelect();
            levelHighlight = 0;
            for (int i = Levels.All.Count - 1; i >= 0; i--)
            {
                if (session.Progress.IsUnlocked(i) && !session.Progress.Record(i).Cleared)
                {
                    levelHighlight = i;
                    break;
                }
            }
        }

        public void ToTitle()
        {
            Click();
            session.ToTitle();
        }

        public void ToLevelSelect()
        {
            Click();
            int current = session.LevelIndex;
            session.ToLevelSelect();
            levelHighlight = Mathf.Clamp(current, 0, Levels.All.Count - 1);
        }

        public bool TryStartLevel(int index)
        {
            levelHighlight = Mathf.Clamp(index, 0, Levels.All.Count - 1);
            if (!session.StartLevel(index))
            {
                audioDirector.Play(art.Denied);
                return false;
            }
            Click();
            return true;
        }

        public void StartTrial(LevelDefinition level) => session.StartTrial(level);

        public void Restart()
        {
            Click();
            session.Restart();
        }

        public void NextLevel()
        {
            Click();
            if (!session.StartNextLevel())
            {
                session.Restart();
            }
        }

        public void TogglePause()
        {
            Click();
            session.TogglePause();
        }

        public void CycleSpeed()
        {
            Click();
            session.CycleSpeed();
        }

        void ToggleMute()
        {
            audioDirector.ToggleMute();
            Click();
        }

        void Click() => audioDirector?.Play(art.Click, 0.6f, 0.02f);

        // ---- Battle actions ----

        Battle Active => session.Screen == Screen.Playing && session.Battle != null && !session.Battle.IsOver ? session.Battle : null;

        public void ToggleBuild(TowerKind kind)
        {
            if (Active == null)
            {
                return;
            }
            Placing = Placing == kind ? (TowerKind?)null : kind;
            Selected = null;
            Click();
        }

        public void CancelBuildOrSelection()
        {
            Placing = null;
            Selected = null;
        }

        public void SetCursor(Cell cell)
        {
            cursor = cell;
            cursorOnBoard = shownBattle != null && shownBattle.Map.Contains(cell);
        }

        /// <summary>Builds the chosen tower on <paramref name="cell"/>, or selects the tower standing there.</summary>
        public void ActAt(Cell cell)
        {
            Battle battle = Active;
            if (battle == null)
            {
                return;
            }
            SetCursor(cell);
            if (Placing.HasValue)
            {
                TowerAgent built = battle.Build(Placing.Value, cell);
                if (built == null)
                {
                    audioDirector.Play(art.Denied);
                    hud.Banner("", !battle.Map.IsBuildable(cell) || battle.TowerAt(cell) != null ? "Towers go on free grass beside the road" : "Not enough money", 1.2f);
                    return;
                }
                Placing = null;
                return;
            }
            TowerAgent tower = battle.TowerAt(cell);
            Selected = tower;
            if (tower != null)
            {
                Click();
            }
        }

        public void Select(TowerAgent tower)
        {
            Placing = null;
            Selected = tower;
        }

        public bool CallWave()
        {
            Battle battle = Active;
            if (battle == null || !battle.CallNextWave())
            {
                audioDirector.Play(art.Denied);
                return false;
            }
            return true;
        }

        public bool UpgradeSelected()
        {
            Battle battle = Active;
            if (battle == null || Selected == null || !battle.Upgrade(Selected))
            {
                audioDirector.Play(art.Denied);
                return false;
            }
            return true;
        }

        public bool SellSelected()
        {
            Battle battle = Active;
            if (battle == null || Selected == null || battle.Sell(Selected) == 0)
            {
                return false;
            }
            Selected = null;
            return true;
        }

        public void CycleTargetSelected()
        {
            if (Active != null && Selected != null)
            {
                session.Battle.CyclePriority(Selected);
                Click();
            }
        }

        // ---- Frame ----

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            input.Poll(dt);
            if (!ScriptedInput)
            {
                ReadInput();
            }
            session.Tick(dt);
            SyncScreen();
            SyncBoard();
            SyncBattle(dt);
            RefreshHud();
        }

        void ReadInput()
        {
            if (input.Mute)
            {
                ToggleMute();
            }
            switch (session.Screen)
            {
                case Screen.Title:
                    if (input.Confirm) Play();
                    break;
                case Screen.LevelSelect:
                    int step = input.MenuStep();
                    if (step != 0)
                    {
                        levelHighlight = Mathf.Clamp(levelHighlight + step, 0, Levels.All.Count - 1);
                        Click();
                    }
                    if (input.Confirm) TryStartLevel(levelHighlight);
                    else if (input.Back) ToTitle();
                    break;
                case Screen.Paused:
                    if (input.Pause) TogglePause();
                    else if (input.Restart) Restart();
                    else if (input.ToMenu) ToLevelSelect();
                    break;
                case Screen.Result:
                    if (input.Confirm) NextLevel();
                    else if (input.Restart) Restart();
                    else if (input.Back || input.ToMenu) ToLevelSelect();
                    break;
                case Screen.Playing:
                    ReadBattleInput();
                    break;
            }
        }

        void ReadBattleInput()
        {
            if (input.Cancel && (Placing.HasValue || Selected != null))
            {
                CancelBuildOrSelection();
                return;
            }
            if (input.Pause)
            {
                TogglePause();
                return;
            }
            if (input.Speed) CycleSpeed();
            Battle battle = Active;
            if (battle == null)
            {
                return;
            }
            int key = input.TowerKey;
            if (key >= 0) ToggleBuild((TowerKind)key);
            int cycle = input.TowerCycle;
            if (cycle != 0)
            {
                int current = Placing.HasValue ? (int)Placing.Value : (cycle > 0 ? -1 : 0);
                Placing = (TowerKind)(((current + cycle) % Catalog.TowerKindCount + Catalog.TowerKindCount) % Catalog.TowerKindCount);
                Selected = null;
                Click();
            }
            if (input.CallWave) CallWave();
            if (input.Upgrade) UpgradeSelected();
            if (input.Sell) SellSelected();
            if (input.CycleTarget) CycleTargetSelected();

            Vector2Int move = input.CursorStep();
            if (move != Vector2Int.zero)
            {
                if (!cursorOnBoard) cursor = battle.Map.Spawn;
                var moved = new Cell(Mathf.Clamp(cursor.Col + move.x, 0, battle.Map.Cols - 1), Mathf.Clamp(cursor.Row + move.y, 0, battle.Map.Rows - 1));
                SetCursor(moved);
            }
            else if (input.PointerActive)
            {
                bool overUi = hud.IsPointerOverUi(input.PointerPosition);
                if (overUi)
                {
                    cursorOnBoard = false;
                }
                else if (TryPointerCell(out Cell pointed))
                {
                    SetCursor(pointed);
                }
                else
                {
                    cursorOnBoard = false;
                }
                if (!overUi && input.Click)
                {
                    if (cursorOnBoard) ActAt(cursor);
                    else CancelBuildOrSelection();
                }
            }
            if (input.Act && cursorOnBoard) ActAt(cursor);
            if (input.RightClick) CancelBuildOrSelection();
        }

        bool TryPointerCell(out Cell cell)
        {
            cell = default;
            if (board == null || shownBattle == null)
            {
                return false;
            }
            var ground = new Plane(Vector3.up, new Vector3(0f, board.Surface, 0f));
            Ray ray = cameraRig.View.ScreenPointToRay(input.PointerPosition);
            if (!ground.Raycast(ray, out float enter))
            {
                return false;
            }
            cell = shownBattle.Map.CellAt(Shapes.Ground(ray.GetPoint(enter)));
            return shownBattle.Map.Contains(cell);
        }

        // ---- Syncing the views ----

        void SyncScreen()
        {
            if (shownScreen == session.Screen)
            {
                return;
            }
            Screen previous = shownScreen ?? Screen.Title;
            shownScreen = session.Screen;
            hud.ShowScreen(session.Screen);
            switch (session.Screen)
            {
                case Screen.Title:
                    hud.RefreshTitle(session.Progress);
                    audioDirector.PlayMusic(art.TitleMusic);
                    break;
                case Screen.LevelSelect:
                    audioDirector.PlayMusic(art.TitleMusic);
                    break;
                case Screen.Paused:
                    hud.RefreshAudio(audioDirector);
                    break;
                case Screen.Result:
                    hud.ShowResult(session.LastResult, session.Battle, session.HasNextLevel);
                    hud.ClearBanner();
                    break;
                case Screen.Playing:
                    if (previous != Screen.Paused) audioDirector.PlayMusic(art.BattleMusic);
                    break;
            }
            audioDirector.SetDucked(session.Screen == Screen.Paused || session.Screen == Screen.Result);
        }

        void SyncBoard()
        {
            LevelDefinition wanted = session.Battle != null ? session.Battle.Level : Levels.All[levelHighlight];
            if (session.Battle != shownBattle)
            {
                ClearBattleViews();
                shownBattle = session.Battle;
                Placing = null;
                Selected = null;
                if (shownBattle != null)
                {
                    SetCursor(shownBattle.Map.Spawn);
                    cursorOnBoard = false;
                    hud.Banner(shownBattle.Level.Name.ToUpperInvariant(), shownBattle.Level.Tagline, 2.6f);
                }
            }
            if (wanted != boardLevel)
            {
                board?.Destroy();
                boardLevel = wanted;
                var map = shownBattle != null ? shownBattle.Map : new LevelMap(wanted.Rows);
                board = Board.Build(art, map, wanted.Theme, transform.parent);
                cameraRig.Frame(map.Cols, map.Rows);
                cameraRig.View.backgroundColor = wanted.Theme == Theme.Snow ? new Color(0.72f, 0.82f, 0.92f) : new Color(0.55f, 0.78f, 0.93f);
            }
            if (session.Screen == Screen.LevelSelect)
            {
                hud.RefreshLevels(session.Progress, levelHighlight);
            }
        }

        void ClearBattleViews()
        {
            foreach (TowerView view in towerViews.Values) view.Destroy();
            towerViews.Clear();
            foreach (EnemyView view in enemyViews.Values) view.Destroy();
            enemyViews.Clear();
            foreach (Transform shot in shotViews.Values) Destroy(shot.gameObject);
            shotViews.Clear();
            hud.ClearFloaters();
            hud.ClearBanner();
        }

        void SyncBattle(float dt)
        {
            Battle battle = shownBattle;
            bool showCursor = battle != null && session.Screen == Screen.Playing && !battle.IsOver;
            if (battle == null)
            {
                highlight.gameObject.SetActive(false);
                rangeRing.gameObject.SetActive(false);
                return;
            }
            foreach (BattleEvent e in battle.Events)
            {
                Present(battle, e);
            }
            battle.ClearEvents();
            float viewDt = session.Screen == Screen.Playing ? dt * session.Speed : 0f;

            foreach (TowerAgent tower in battle.Towers)
            {
                if (!towerViews.ContainsKey(tower))
                {
                    towerViews[tower] = new TowerView(art, tower, board.CellCenter(tower.Cell), actors);
                }
            }
            staleTowers.Clear();
            foreach (KeyValuePair<TowerAgent, TowerView> pair in towerViews)
            {
                if (!Contains(battle.Towers, pair.Key)) staleTowers.Add(pair.Key);
                else pair.Value.Sync(viewDt);
            }
            foreach (TowerAgent gone in staleTowers)
            {
                towerViews[gone].Destroy();
                towerViews.Remove(gone);
                if (Selected == gone) Selected = null;
            }

            seen.Clear();
            foreach (EnemyAgent enemy in battle.Enemies)
            {
                seen.Add(enemy.Id);
                if (!enemyViews.TryGetValue(enemy.Id, out EnemyView view))
                {
                    view = new EnemyView(art, enemy, board.Surface, actors, frostMaterial);
                    enemyViews[enemy.Id] = view;
                }
                view.Sync(viewDt, cameraRig.View);
            }
            RemoveUnseen(enemyViews, id => enemyViews[id].Destroy());

            seen.Clear();
            foreach (Shot shot in battle.Shots)
            {
                seen.Add(shot.Id);
                if (!shotViews.TryGetValue(shot.Id, out Transform view))
                {
                    view = MakeShot(shot.Kind);
                    shotViews[shot.Id] = view;
                }
                PlaceShot(battle, shot, view);
            }
            RemoveUnseen(shotViews, id => Destroy(shotViews[id].gameObject));

            SyncCursor(battle, showCursor);
        }

        static bool Contains(IReadOnlyList<TowerAgent> towers, TowerAgent tower)
        {
            foreach (TowerAgent t in towers) if (t == tower) return true;
            return false;
        }

        void RemoveUnseen<T>(Dictionary<int, T> views, System.Action<int> destroy)
        {
            stale.Clear();
            foreach (int id in views.Keys) if (!seen.Contains(id)) stale.Add(id);
            foreach (int id in stale)
            {
                destroy(id);
                views.Remove(id);
            }
        }

        Transform MakeShot(TowerKind kind)
        {
            switch (kind)
            {
                case TowerKind.Ballista:
                    return Shapes.Part("Bolt", actors, art.CubeMesh, art.Bolt, Vector3.zero, Vector3.zero, new Vector3(0.04f, 0.04f, 0.32f), shadows: false);
                case TowerKind.Cannon:
                    return Shapes.Part("Shell", actors, art.SphereMesh, art.Shell, Vector3.zero, Vector3.zero, Vector3.one * 0.13f);
                case TowerKind.Frost:
                    return Shapes.Part("Frost orb", actors, art.SphereMesh, art.FrostOrb, Vector3.zero, Vector3.zero, Vector3.one * 0.12f, shadows: false);
                default:
                    return Shapes.Part("Boulder", actors, art.SphereMesh, art.Boulder, Vector3.zero, Vector3.zero, Vector3.one * 0.2f);
            }
        }

        void PlaceShot(Battle battle, Shot shot, Transform view)
        {
            float t = shot.Progress;
            float start = MuzzleHeightOf(shot);
            float end = board.Surface + 0.3f;
            float height = Mathf.Lerp(start, end, t);
            if (shot.Kind == TowerKind.Mortar)
            {
                height += Mathf.Sin(t * Mathf.PI) * (1.2f + Vec2.Distance(shot.Origin, shot.Target) * 0.25f);
            }
            else if (shot.Kind == TowerKind.Cannon)
            {
                height += Mathf.Sin(t * Mathf.PI) * 0.35f;
            }
            Vector3 position = Shapes.World(shot.Position, height);
            Vector3 heading = Shapes.World(shot.Target - shot.Position);
            if (heading.sqrMagnitude > 1e-5f)
            {
                view.rotation = Quaternion.LookRotation(heading);
            }
            view.position = position;
        }

        float MuzzleHeightOf(Shot shot)
        {
            foreach (KeyValuePair<TowerAgent, TowerView> pair in towerViews)
            {
                if (pair.Key.Position.Equals(shot.Origin)) return pair.Value.MuzzleHeight;
            }
            return board.Surface + 0.8f;
        }

        void SyncCursor(Battle battle, bool show)
        {
            bool onBoard = show && cursorOnBoard;
            highlight.gameObject.SetActive(onBoard);
            TowerAgent hovered = onBoard ? battle.TowerAt(cursor) : null;
            TowerAgent ringed = Selected ?? (!Placing.HasValue ? hovered : null);
            float range = 0f;
            Vector3 ringAt = Vector3.zero;
            Color ringColor = new Color(1f, 1f, 1f, 0.35f);
            if (onBoard)
            {
                highlight.position = board.CellCenter(cursor, 0.03f);
                Color color = new Color(1f, 1f, 1f, 0.35f);
                if (Placing.HasValue)
                {
                    bool can = battle.CanBuild(Placing.Value, cursor);
                    color = can ? new Color(0.35f, 1f, 0.45f, 0.6f) : new Color(1f, 0.3f, 0.25f, 0.6f);
                    range = Catalog.Tower(Placing.Value).Tiers[0].Range;
                    ringAt = board.CellCenter(cursor, 0.04f);
                    ringColor = can ? new Color(0.55f, 1f, 0.6f, 0.55f) : new Color(1f, 0.45f, 0.4f, 0.45f);
                }
                Shapes.Tint(highlightRenderer, block, "_TintColor", color);
            }
            if (show && ringed != null)
            {
                range = ringed.Current.Range;
                ringAt = board.CellCenter(ringed.Cell, 0.04f);
                ringColor = new Color(1f, 0.9f, 0.4f, 0.6f);
            }
            bool showRing = show && range > 0f;
            rangeRing.gameObject.SetActive(showRing);
            if (showRing)
            {
                rangeRing.position = ringAt;
                rangeRing.localScale = new Vector3(range, 1f, range);
                Shapes.Tint(rangeRenderer, block, "_TintColor", ringColor);
            }
        }

        // ---- Events into effects and sound ----

        Vector3 At(Vec2 ground, float lift = 0.3f) => Shapes.World(ground, board.Surface + lift);

        void Present(Battle battle, BattleEvent e)
        {
            switch (e.Type)
            {
                case BattleEventType.WaveStarted:
                    WaveDefinition wave = battle.Level.Waves[e.Amount - 1];
                    bool boss = wave.Count(EnemyKind.Mothership) > 0;
                    hud.Banner($"WAVE {e.Amount}", boss ? "The mothership is coming!" : $"{TotalOf(wave)} saucers incoming", 2f);
                    audioDirector.Play(boss ? art.Leak : art.WaveStart, 0.8f, 0f);
                    break;
                case BattleEventType.WaveCleared:
                    if (!battle.IsOver)
                    {
                        hud.Floater(At(e.Position, 1.2f), $"+${e.Amount}", "floater--gold");
                        audioDirector.Play(art.WaveClear, 0.7f, 0f);
                    }
                    break;
                case BattleEventType.EnemyHit:
                    ViewOf(e.EnemyId)?.Flash();
                    if (e.Tower == TowerKind.Ballista) audioDirector.Play(art.Hit, 0.35f, 0.15f, 0.06f);
                    break;
                case BattleEventType.EnemyKilled:
                    Color color = KindColors[(int)e.Enemy];
                    bool mothership = e.Enemy == EnemyKind.Mothership;
                    Effects.Burst(At(e.Position), art.ParticleAdditive, color, Color.white, mothership ? 70 : 18, mothership ? 4f : 2.6f, mothership ? 0.35f : 0.18f, 0.6f, 0.6f);
                    Effects.Burst(At(e.Position), art.ParticleAlpha, new Color(0.3f, 0.3f, 0.35f, 0.8f), new Color(0.15f, 0.15f, 0.2f, 0.6f), mothership ? 30 : 8, 1.2f, 0.25f, 0.8f, -0.1f);
                    hud.Floater(At(e.Position, 0.6f), $"+${e.Amount}", "floater--gold");
                    audioDirector.Play(mothership ? art.MothershipDown : art.SaucerDown, mothership ? 1f : 0.55f, 0.12f, 0.05f);
                    if (mothership) cameraRig.Shake(0.7f);
                    break;
                case BattleEventType.EnemyLeaked:
                    Effects.Burst(At(e.Position), art.ParticleAdditive, new Color(1f, 0.3f, 0.2f), new Color(1f, 0.8f, 0.3f), 24, 3f, 0.25f, 0.6f);
                    hud.Floater(At(e.Position, 1f), $"-{e.Amount}", "floater--red");
                    audioDirector.Play(art.Leak, 0.8f);
                    cameraRig.Shake(e.Amount >= 5 ? 0.8f : 0.35f);
                    break;
                case BattleEventType.TowerFired:
                    AudioClip shot = e.Tower == TowerKind.Ballista ? art.BallistaShot : e.Tower == TowerKind.Cannon ? art.CannonShot : e.Tower == TowerKind.Frost ? art.FrostShot : art.MortarShot;
                    audioDirector.Play(shot, e.Tower == TowerKind.Ballista ? 0.4f : 0.6f, 0.08f, 0.07f);
                    break;
                case BattleEventType.ShotLanded:
                    Landed(e);
                    break;
                case BattleEventType.TowerBuilt:
                    Effects.Burst(At(e.Position, 0.1f), art.ParticleAlpha, new Color(0.85f, 0.75f, 0.6f, 0.9f), new Color(1f, 1f, 1f, 0.7f), 16, 1.6f, 0.25f, 0.6f, 0.2f);
                    hud.Floater(At(e.Position, 1f), $"-${e.Amount}", "floater--red");
                    audioDirector.Play(art.Build, 0.8f);
                    break;
                case BattleEventType.TowerUpgraded:
                    Effects.Burst(At(e.Position, 0.8f), art.ParticleAdditive, new Color(1f, 0.85f, 0.3f), Color.white, 26, 2f, 0.16f, 0.7f, -0.3f);
                    audioDirector.Play(art.Upgrade, 0.8f, 0f);
                    break;
                case BattleEventType.TowerSold:
                    Effects.Burst(At(e.Position, 0.1f), art.ParticleAlpha, new Color(0.85f, 0.75f, 0.6f, 0.9f), new Color(1f, 1f, 1f, 0.7f), 20, 2f, 0.3f, 0.6f, 0.2f);
                    hud.Floater(At(e.Position, 1f), $"+${e.Amount}", "floater--gold");
                    audioDirector.Play(art.Sell, 0.8f);
                    break;
                case BattleEventType.Won:
                    hud.Banner("VICTORY", "Every wave beaten", 3f);
                    audioDirector.Play(art.Victory, 1f, 0f);
                    break;
                case BattleEventType.Lost:
                    hud.Banner("THE BASE HAS FALLEN", "", 3f);
                    audioDirector.Play(art.Defeat, 1f, 0f);
                    cameraRig.Shake(1f);
                    break;
            }
        }

        void Landed(BattleEvent e)
        {
            Vector3 point = At(e.Position, 0.15f);
            switch (e.Tower)
            {
                case TowerKind.Frost:
                    Effects.Burst(point, art.ParticleAdditive, new Color(0.6f, 0.9f, 1f), Color.white, 12, 1.6f, 0.16f, 0.45f);
                    if (e.Radius > 0f) Effects.Shockwave(At(e.Position, 0.05f), e.Radius, new Color(0.6f, 0.9f, 1f, 0.7f), ringMesh, art.Overlay);
                    break;
                case TowerKind.Ballista:
                    Effects.Burst(point, art.ParticleAdditive, new Color(1f, 0.9f, 0.6f), Color.white, 5, 1.4f, 0.1f, 0.25f);
                    break;
                default:
                    float size = e.Radius;
                    Effects.Burst(point, art.ParticleAdditive, new Color(1f, 0.6f, 0.15f), new Color(1f, 0.95f, 0.5f), (int)(22 * size) + 8, 2.4f * size + 0.8f, 0.3f * size + 0.1f, 0.5f, 0.3f);
                    Effects.Burst(point, art.ParticleAlpha, new Color(0.35f, 0.3f, 0.28f, 0.8f), new Color(0.6f, 0.55f, 0.5f, 0.6f), (int)(10 * size) + 4, 1.2f * size, 0.4f * size + 0.1f, 0.9f, -0.15f);
                    Effects.Shockwave(At(e.Position, 0.05f), e.Radius, new Color(1f, 0.75f, 0.35f, 0.8f), ringMesh, art.Overlay);
                    audioDirector.Play(art.Explosion, e.Tower == TowerKind.Mortar ? 0.9f : 0.55f, 0.1f, 0.06f);
                    if (e.Tower == TowerKind.Mortar) cameraRig.Shake(0.15f);
                    break;
            }
        }

        static int TotalOf(WaveDefinition wave)
        {
            int total = 0;
            foreach (WaveGroup group in wave.Groups) total += group.Count;
            return total;
        }

        // ---- HUD ----

        void RefreshHud()
        {
            if (shownBattle == null || (session.Screen != Screen.Playing && session.Screen != Screen.Paused && session.Screen != Screen.Result))
            {
                return;
            }
            hud.RefreshBattle(shownBattle, session.Speed, Placing, Selected, input.UsingGamepad);
            hud.SetHint(Hint(shownBattle));
        }

        string Hint(Battle battle)
        {
            if (battle.IsOver || session.Screen != Screen.Playing)
            {
                return "";
            }
            bool pad = input.UsingGamepad;
            if (Placing.HasValue)
            {
                TowerStats stats = Catalog.Tower(Placing.Value);
                return pad
                    ? $"Move to free grass and press A to build the {stats.Name} (${stats.Tiers[0].Cost})  |  B cancels"
                    : $"Click free grass to build the {stats.Name} (${stats.Tiers[0].Cost})  |  right click or Esc cancels";
            }
            if (Selected != null)
            {
                return pad ? "X upgrade  |  LT sell  |  RT change target  |  B close" : "U upgrade  |  X sell  |  T change target  |  Esc close";
            }
            if (battle.State == BattleState.Preparing)
            {
                return pad
                    ? "LB / RB pick a tower and place it beside the road, then press Y to call the first wave"
                    : "Pick a tower (1-4) and place it beside the road, then press Space to call the first wave";
            }
            return pad ? "LB / RB build  |  A on a tower to select  |  Select changes speed  |  Start pauses"
                : "1-4 build  |  click a tower to upgrade or sell  |  F speed  |  Esc pause";
        }
    }
}
