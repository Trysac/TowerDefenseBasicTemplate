using System;
using System.Collections.Generic;
using SaucerSiege.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Screen = SaucerSiege.Core.Screen;

namespace SaucerSiege.Game
{
    /// <summary>
    /// The UI Toolkit HUD and menus, built in code and styled by Hud.uss. Buttons never take
    /// keyboard focus: the director reads keys itself.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class Hud : MonoBehaviour
    {
        static readonly string[] KindColors = { "#7ed957", "#ff6b5b", "#b07cff", "#ffd23f" };
        static readonly string[] TowerColors = { "#c08a52", "#9aa3b5", "#7fdcff", "#e0b25b" };
        const float FloaterLifetime = 0.9f;

        sealed class FloatingText
        {
            public Label Label;
            public Vector3 World;
            public float Age;
        }

        VisualElement root;
        VisualElement floaters;
        VisualElement play;
        VisualElement wavePanel;
        VisualElement buildBar;
        Label lives;
        Label money;
        Label wave;
        Button speed;
        Label waveTitle;
        VisualElement waveKinds;
        Button callWave;
        Label callNote;
        readonly Button[] towerCards = new Button[4];
        readonly Label[] towerCosts = new Label[4];
        VisualElement towerPanel;
        Label towerTitle;
        Label towerStats;
        Button upgrade;
        Button sell;
        Button target;
        Label banner;
        Label bannerSub;
        Label hint;
        float bannerTime;

        VisualElement title;
        Label titleStars;
        VisualElement levels;
        readonly Button[] levelCards = new Button[3];
        Label levelsStars;
        VisualElement pause;
        Slider musicSlider;
        Slider effectsSlider;
        Button muteButton;
        VisualElement result;
        Label resultTitle;
        Label resultLevel;
        VisualElement resultStars;
        Label resultScore;
        Label resultBest;
        Label resultDetails;
        Button resultNext;

        readonly List<FloatingText> active = new List<FloatingText>();
        Camera view;

        public event Action PlayClicked;
        public event Action BackClicked;
        public event Action<int> LevelClicked;
        public event Action PauseClicked;
        public event Action ResumeClicked;
        public event Action RestartClicked;
        public event Action LevelsClicked;
        public event Action NextClicked;
        public event Action CallWaveClicked;
        public event Action SpeedClicked;
        public event Action<int> TowerClicked;
        public event Action UpgradeClicked;
        public event Action SellClicked;
        public event Action TargetClicked;
        public event Action MuteClicked;
        public event Action<float> MusicChanged;
        public event Action<float> EffectsChanged;

        /// <summary>Which full-screen layer is showing, for tests.</summary>
        public string Showing { get; private set; } = "";

        public void Init(Camera camera)
        {
            view = camera;
            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.AddToClassList("hud");
            root.pickingMode = PickingMode.Ignore;
            floaters = Layer("floaters");
            BuildPlay();
            BuildTitle();
            BuildLevels();
            BuildPause();
            BuildResult();
        }

        // ---- Building ----

        static VisualElement Box(string classes, VisualElement parent, bool picks = false)
        {
            var element = new VisualElement { pickingMode = picks ? PickingMode.Position : PickingMode.Ignore };
            foreach (string name in classes.Split(' ')) if (name.Length > 0) element.AddToClassList(name);
            parent?.Add(element);
            return element;
        }

        static Label Text(string text, string classes, VisualElement parent)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            foreach (string name in classes.Split(' ')) if (name.Length > 0) label.AddToClassList(name);
            parent.Add(label);
            return label;
        }

        static Button Click(string text, string classes, VisualElement parent, Action onClick)
        {
            var button = new Button(() => onClick?.Invoke()) { text = text, focusable = false };
            button.AddToClassList("button");
            foreach (string name in classes.Split(' ')) if (name.Length > 0) button.AddToClassList(name);
            parent.Add(button);
            return button;
        }

        VisualElement Layer(string name)
        {
            VisualElement layer = Box("layer", root);
            layer.name = name;
            if (name != "play" && name != "floaters") layer.AddToClassList("layer--center");
            return layer;
        }

        void BuildPlay()
        {
            play = Layer("play");
            VisualElement bar = Box("topbar", play);
            VisualElement stats = Box("chips", bar, picks: true);
            lives = Text("20", "chip chip--lives", stats);
            money = Text("$0", "chip chip--money", stats);
            wave = Text("WAVE 0/0", "chip chip--wave", stats);
            VisualElement controls = Box("row", bar);
            speed = Click("1x", "button--square", controls, () => SpeedClicked?.Invoke());
            Click("II", "button--square", controls, () => PauseClicked?.Invoke());

            wavePanel = Box("panel wave-panel", play, picks: true);
            waveTitle = Text("NEXT: WAVE 1", "panel__title", wavePanel);
            waveKinds = Box("row kinds", wavePanel);
            callWave = Click("CALL WAVE", "button--call", wavePanel, () => CallWaveClicked?.Invoke());
            callNote = Text("", "note", wavePanel);

            buildBar = Box("buildbar", play, picks: true);
            VisualElement build = buildBar;
            for (int i = 0; i < towerCards.Length; i++)
            {
                int index = i;
                TowerStats stats2 = Catalog.Tower((TowerKind)i);
                Button card = Click("", "card", build, () => TowerClicked?.Invoke(index));
                VisualElement swatch = Box("card__swatch", card);
                swatch.style.backgroundColor = Color(TowerColors[i]);
                Text($"{i + 1}  {stats2.Name.ToUpperInvariant()}", "card__name", card);
                Text(stats2.Role, "card__role", card);
                towerCosts[i] = Text($"${stats2.Tiers[0].Cost}", "card__cost", card);
                towerCards[i] = card;
            }

            towerPanel = Box("panel tower-panel hidden", play, picks: true);
            towerTitle = Text("", "panel__title", towerPanel);
            towerStats = Text("", "stats", towerPanel);
            upgrade = Click("UPGRADE", "button--wide", towerPanel, () => UpgradeClicked?.Invoke());
            target = Click("TARGET: FIRST", "button--wide", towerPanel, () => TargetClicked?.Invoke());
            sell = Click("SELL", "button--wide button--danger", towerPanel, () => SellClicked?.Invoke());

            VisualElement center = Box("banner", play);
            banner = Text("", "banner__title", center);
            bannerSub = Text("", "banner__sub", center);
            hint = Text("", "hint", play);
        }

        void BuildTitle()
        {
            title = Layer("title");
            VisualElement panel = Box("menu", title, picks: true);
            Text("SAUCER SIEGE", "menu__logo", panel);
            Text("Hold the road. Build, upgrade and sell towers to stop the saucer waves.", "menu__tagline", panel);
            Click("PLAY", "button--big", panel, () => PlayClicked?.Invoke());
            titleStars = Text("", "menu__note", panel);
            Text("Mouse, keyboard or gamepad  |  Enter / A to start", "menu__note", panel);
            Text("3D models: Kenney Tower Defense Kit (CC0)  |  Music and sound: synthesized for this game", "footer", title);
        }

        void BuildLevels()
        {
            levels = Layer("levels");
            VisualElement panel = Box("menu menu--wide", levels, picks: true);
            Text("CHOOSE A BATTLEFIELD", "menu__heading", panel);
            VisualElement row = Box("row row--center", panel);
            for (int i = 0; i < levelCards.Length; i++)
            {
                int index = i;
                levelCards[i] = Click("", "level", row, () => LevelClicked?.Invoke(index));
            }
            levelsStars = Text("", "menu__note", panel);
            Click("BACK", "button--wide", panel, () => BackClicked?.Invoke());
        }

        void BuildPause()
        {
            pause = Layer("pause");
            VisualElement panel = Box("menu", pause, picks: true);
            Text("PAUSED", "menu__heading", panel);
            Click("RESUME", "button--big", panel, () => ResumeClicked?.Invoke());
            Click("RESTART LEVEL", "button--wide", panel, () => RestartClicked?.Invoke());
            Click("LEVEL SELECT", "button--wide", panel, () => LevelsClicked?.Invoke());
            musicSlider = new Slider("Music", 0f, 1f) { focusable = false };
            musicSlider.AddToClassList("slider");
            musicSlider.RegisterValueChangedCallback(e => MusicChanged?.Invoke(e.newValue));
            panel.Add(musicSlider);
            effectsSlider = new Slider("Effects", 0f, 1f) { focusable = false };
            effectsSlider.AddToClassList("slider");
            effectsSlider.RegisterValueChangedCallback(e => EffectsChanged?.Invoke(e.newValue));
            panel.Add(effectsSlider);
            muteButton = Click("SOUND: ON", "button--wide", panel, () => MuteClicked?.Invoke());
            Text("Esc / Start resume  |  R restart  |  Q level select  |  M mute", "menu__note", panel);
        }

        void BuildResult()
        {
            result = Layer("result");
            VisualElement panel = Box("menu", result, picks: true);
            resultTitle = Text("VICTORY", "menu__logo", panel);
            resultLevel = Text("", "menu__tagline", panel);
            resultStars = Box("row row--center stars", panel);
            resultScore = Text("", "result__score", panel);
            resultBest = Text("", "result__best", panel);
            resultDetails = Text("", "menu__note", panel);
            resultNext = Click("NEXT LEVEL", "button--big", panel, () => NextClicked?.Invoke());
            Click("RETRY", "button--wide", panel, () => RestartClicked?.Invoke());
            Click("LEVEL SELECT", "button--wide", panel, () => LevelsClicked?.Invoke());
        }

        static Color Color(string hex) => ColorUtility.TryParseHtmlString(hex, out Color color) ? color : UnityEngine.Color.magenta;

        static void Show(VisualElement element, bool visible) => element.EnableInClassList("hidden", !visible);

        static void Stars(VisualElement row, int count, int of = 3)
        {
            row.Clear();
            for (int i = 0; i < of; i++)
            {
                VisualElement star = Box(i < count ? "star" : "star star--empty", row);
                star.name = "star";
            }
        }

        // ---- Screens ----

        public void ShowScreen(Screen screen)
        {
            Show(play, screen == Screen.Playing || screen == Screen.Paused || screen == Screen.Result);
            Show(buildBar, screen != Screen.Result);
            Show(wavePanel, screen != Screen.Result);
            Show(title, screen == Screen.Title);
            Show(levels, screen == Screen.LevelSelect);
            Show(pause, screen == Screen.Paused);
            Show(result, screen == Screen.Result);
            Showing = screen.ToString();
        }

        public void RefreshTitle(Progress progress)
        {
            int stars = progress.TotalStars;
            titleStars.text = stars > 0 ? $"Stars earned: {stars} / {Levels.All.Count * 3}" : "Three battlefields, from a quiet valley to a frozen ridge.";
        }

        public void RefreshLevels(Progress progress, int highlighted)
        {
            for (int i = 0; i < levelCards.Length; i++)
            {
                Button card = levelCards[i];
                card.Clear();
                LevelDefinition level = Levels.All[i];
                LevelRecord record = progress.Record(i);
                bool open = progress.IsUnlocked(i);
                card.EnableInClassList("level--locked", !open);
                card.EnableInClassList("level--snow", level.Theme == Theme.Snow);
                card.EnableInClassList("level--highlight", i == highlighted);
                Text($"{i + 1}", "level__number", card);
                Text(level.Name.ToUpperInvariant(), "level__name", card);
                Text(level.Tagline, "level__tagline", card);
                Text($"{level.Waves.Count} waves  |  ${level.StartingMoney}", "level__meta", card);
                VisualElement stars = Box("row row--center stars stars--small", card);
                Stars(stars, record.Stars);
                Text(open ? (record.BestScore > 0 ? $"Best {record.BestScore}" : "Not played yet") : "LOCKED", "level__best", card);
            }
            levelsStars.text = $"Stars: {progress.TotalStars} / {Levels.All.Count * 3}   |   Clear a battlefield to open the next";
        }

        public void RefreshAudio(AudioDirector audio)
        {
            musicSlider.SetValueWithoutNotify(audio.MusicVolume);
            effectsSlider.SetValueWithoutNotify(audio.EffectsVolume);
            muteButton.text = audio.Muted ? "SOUND: OFF (M)" : "SOUND: ON (M)";
        }

        public void ShowResult(BattleResult outcome, Battle battle, bool hasNext)
        {
            resultTitle.text = outcome.Won ? "VICTORY" : "DEFEAT";
            resultTitle.EnableInClassList("menu__logo--defeat", !outcome.Won);
            resultLevel.text = battle.Level.Name;
            Stars(resultStars, outcome.Stars);
            resultScore.text = $"Score {outcome.Score}";
            resultBest.text = outcome.NewBest ? "NEW BEST!" : "";
            resultDetails.text = $"Waves {outcome.WavesReached}/{battle.TotalWaves}   |   Saucers downed {battle.Kills}   |   Lives left {battle.Economy.Lives}";
            Show(resultNext, outcome.Won && hasNext);
        }

        // ---- Battle HUD ----

        public void RefreshBattle(Battle battle, int gameSpeed, TowerKind? placing, TowerAgent selected, bool gamepad)
        {
            lives.text = $"LIVES {battle.Economy.Lives}";
            money.text = $"${battle.Economy.Money}";
            wave.text = $"WAVE {battle.WavesStarted}/{battle.TotalWaves}";
            speed.text = $"{gameSpeed}x";
            speed.EnableInClassList("button--active", gameSpeed > 1);

            WaveDefinition next = battle.NextWave;
            waveKinds.Clear();
            if (next != null)
            {
                waveTitle.text = $"NEXT: WAVE {battle.WavesStarted + 1}";
                for (int k = 0; k < 4; k++)
                {
                    int count = next.Count((EnemyKind)k);
                    if (count == 0) continue;
                    VisualElement chip = Box("kind", waveKinds);
                    Box("kind__dot", chip).style.backgroundColor = Color(KindColors[k]);
                    Text($"{Catalog.Enemy((EnemyKind)k).Name} x{count}", "kind__text", chip);
                }
                callWave.text = gamepad ? "CALL WAVE (Y)" : "CALL WAVE (SPACE)";
                Show(callWave, true);
                callNote.text = battle.State == BattleState.Preparing
                    ? "Build first, then call the wave"
                    : battle.Countdown > 0f ? $"Starts in {Mathf.CeilToInt(battle.Countdown)}s  |  call now for +${battle.EarlyCallBonus}" : "Arrives once this wave has landed";
            }
            else
            {
                waveTitle.text = battle.IsOver ? (battle.State == BattleState.Won ? "ALL WAVES BEATEN" : "THE BASE HAS FALLEN") : "FINAL WAVE";
                Show(callWave, false);
                int left = battle.Enemies.Count;
                callNote.text = battle.IsOver ? "" : left == 1 ? "1 saucer left" : $"{left} saucers left";
            }

            for (int i = 0; i < towerCards.Length; i++)
            {
                int cost = Catalog.Tower((TowerKind)i).Tiers[0].Cost;
                towerCards[i].EnableInClassList("card--selected", placing.HasValue && (int)placing.Value == i);
                towerCards[i].EnableInClassList("card--poor", !battle.Economy.CanAfford(cost));
            }

            Show(towerPanel, selected != null);
            if (selected != null)
            {
                TowerTier now = selected.Current;
                TowerTier after = selected.Next;
                towerTitle.text = $"{selected.Stats.Name.ToUpperInvariant()}   LV {selected.Tier + 1}/{selected.Stats.MaxTier + 1}";
                string Line(string name, float a, float? b, string format) =>
                    b.HasValue && Math.Abs(b.Value - a) > 1e-4f ? $"{name}  {a.ToString(format)}  >  {b.Value.ToString(format)}\n" : $"{name}  {a.ToString(format)}\n";
                string text = Line("Damage", now.Damage, after?.Damage, "0");
                text += Line("Range", now.Range, after?.Range, "0.0");
                text += Line("Shots/s", 1f / now.Cooldown, after != null ? 1f / after.Cooldown : (float?)null, "0.0");
                if (now.Splash > 0f || (after != null && after.Splash > 0f)) text += Line("Blast", now.Splash, after?.Splash, "0.00");
                if (now.SlowFactor < 1f) text += Line("Slow", (1f - now.SlowFactor) * 100f, after != null ? (1f - after.SlowFactor) * 100f : (float?)null, "0'%'");
                text += $"Kills  {selected.Kills}";
                towerStats.text = text;
                upgrade.text = selected.CanUpgrade ? $"UPGRADE  ${selected.UpgradeCost}  ({(gamepad ? "X" : "U")})" : "MAX LEVEL";
                upgrade.SetEnabled(battle.CanUpgrade(selected));
                sell.text = $"SELL  +${selected.SellValue}  ({(gamepad ? "LT" : "X")})";
                target.text = $"TARGET: {selected.Priority.ToString().ToUpperInvariant()}  ({(gamepad ? "RT" : "T")})";
            }

            if (bannerTime > 0f)
            {
                bannerTime -= Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(bannerTime / 0.4f);
                banner.style.opacity = alpha;
                bannerSub.style.opacity = alpha;
            }
        }

        public void SetHint(string text) => hint.text = text;

        public void Banner(string text, string sub = "", float seconds = 2f)
        {
            banner.text = text;
            bannerSub.text = sub;
            bannerTime = seconds;
            banner.style.opacity = 1f;
            bannerSub.style.opacity = 1f;
        }

        public void ClearBanner()
        {
            bannerTime = 0f;
            banner.style.opacity = 0f;
            bannerSub.style.opacity = 0f;
        }

        /// <summary>Text that rises from a point in the world and fades.</summary>
        public void Floater(Vector3 world, string text, string colorClass)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("floater");
            label.AddToClassList(colorClass);
            floaters.Add(label);
            active.Add(new FloatingText { Label = label, World = world });
        }

        public void ClearFloaters()
        {
            foreach (FloatingText floater in active) floater.Label.RemoveFromHierarchy();
            active.Clear();
        }

        void Update()
        {
            if (root == null || root.panel == null || view == null)
            {
                return;
            }
            for (int i = active.Count - 1; i >= 0; i--)
            {
                FloatingText floater = active[i];
                floater.Age += Time.unscaledDeltaTime;
                if (floater.Age >= FloaterLifetime)
                {
                    floater.Label.RemoveFromHierarchy();
                    active.RemoveAt(i);
                    continue;
                }
                // Viewport maths instead of screen pixels, so it also works when the camera and
                // the panel render to textures (the screenshot hook).
                Vector3 viewport = view.WorldToViewportPoint(floater.World + Vector3.up * floater.Age * 0.7f);
                floater.Label.style.left = viewport.x * root.layout.width - 30f;
                floater.Label.style.top = (1f - viewport.y) * root.layout.height - 12f;
                floater.Label.style.opacity = 1f - floater.Age / FloaterLifetime;
            }
        }

        /// <summary>True when the pointer is over a panel or button, so a click there does not reach the board.</summary>
        public bool IsPointerOverUi(Vector2 screenPosition)
        {
            if (root?.panel == null)
            {
                return false;
            }
            Vector2 panelPoint = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screenPosition.x, UnityEngine.Screen.height - screenPosition.y));
            VisualElement picked = root.panel.Pick(panelPoint);
            return picked != null && picked != root;
        }
    }
}
