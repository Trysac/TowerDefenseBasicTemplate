using System;
using System.Collections;
using System.IO;
using SaucerSiege.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Screen = SaucerSiege.Core.Screen;

namespace SaucerSiege.Game
{
    /// <summary>
    /// Opt-in capture of README screenshots: <c>SaucerSiege.exe -screenshot &lt;folder&gt; -scenario title,levels,build,battle,upgrade,snow,victory</c>.
    /// Without the -screenshot argument nothing is created, so normal runs and WebGL builds are
    /// unaffected. The same hook runs inside the editor (ProjectBuilder.CaptureScreenshots), which
    /// renders the camera and the HUD off screen and blends them, for machines that cannot run the player.
    /// Adding <c>-frames N [-fps F]</c> records N numbered frames per scenario at a fixed timestep
    /// (into &lt;folder&gt;/&lt;scenario&gt;/) instead of one still, e.g. <c>-scenario gameplay -frames 90 -fps 15</c>.
    /// </summary>
    public sealed class ScreenshotScenarios : MonoBehaviour
    {
        const string DefaultScenarios = "title,levels,build,battle,upgrade,snow,victory";
        const int Width = 1280;
        const int Height = 720;

        string folder;
        string[] scenarios;
        int frames;
        int fps;
        RenderTexture sceneTarget;
        RenderTexture uiTarget;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartWhenRequested()
        {
            string target = Argument("-screenshot");
            if (string.IsNullOrEmpty(target))
            {
                return;
            }
            var host = new GameObject(nameof(ScreenshotScenarios)).AddComponent<ScreenshotScenarios>();
            host.folder = target;
            host.scenarios = (Argument("-scenario") ?? DefaultScenarios).Split(',');
            host.frames = int.TryParse(Argument("-frames"), out int count) ? count : 0;
            host.fps = int.TryParse(Argument("-fps"), out int rate) ? rate : 15;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            var director = FindAnyObjectByType<GameDirector>();
            director.ScriptedInput = true;
            if (Application.isEditor)
            {
                RenderOffscreen(director);
            }
            yield return new WaitForSecondsRealtime(0.5f);
            foreach (string raw in scenarios)
            {
                string scenario = raw.Trim();
                UnityEngine.Random.InitState(scenario.Length * 97);
                if (frames > 0)
                {
                    yield return Record(director, scenario);
                    continue;
                }
                yield return Stage(director, scenario);
                Texture2D shot;
                if (Application.isEditor)
                {
                    yield return null;
                    shot = CaptureOffscreen();
                }
                else
                {
                    yield return new WaitForEndOfFrame();
                    shot = ScreenCapture.CaptureScreenshotAsTexture();
                }
                File.WriteAllBytes(Path.Combine(folder, scenario + ".png"), shot.EncodeToPNG());
                Destroy(shot);
                Debug.Log($"Saved screenshot {scenario}.png");
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Stages <paramref name="scenario"/>, then saves <see cref="frames"/> frames while game time
        /// advances exactly 1/<see cref="fps"/> s per frame, however long each capture takes.
        /// </summary>
        IEnumerator Record(GameDirector director, string scenario)
        {
            string target = Path.Combine(folder, scenario);
            Directory.CreateDirectory(target);
            yield return Stage(director, scenario);
            if (scenario == "gameplay")
            {
                StartCoroutine(BuildDuringBattle(director));
            }
            Time.captureFramerate = fps;
            for (int i = 0; i < frames; i++)
            {
                Texture2D shot;
                if (Application.isEditor)
                {
                    yield return null;
                    shot = CaptureOffscreen();
                }
                else
                {
                    yield return new WaitForEndOfFrame();
                    shot = ScreenCapture.CaptureScreenshotAsTexture();
                }
                File.WriteAllBytes(Path.Combine(target, $"frame_{i:0000}.png"), shot.EncodeToPNG());
                Destroy(shot);
            }
            Time.captureFramerate = 0;
            Debug.Log($"Saved {frames} frames of {scenario} at {fps} fps.");
        }

        /// <summary>While the waves roll in, the player places a mortar on free grass and upgrades a cannon.</summary>
        static IEnumerator BuildDuringBattle(GameDirector director)
        {
            Battle battle = director.Session.Battle;
            Cell spot = new Cell(9, 5);
            for (int col = 3; col < battle.Map.Cols && !battle.CanBuild(TowerKind.Mortar, spot); col++)
            {
                for (int row = 1; row < battle.Map.Rows; row++)
                {
                    spot = new Cell(col, row);
                    if (battle.CanBuild(TowerKind.Mortar, spot))
                    {
                        break;
                    }
                }
            }
            yield return new WaitForSeconds(0.8f);
            director.ToggleBuild(TowerKind.Mortar);
            director.SetCursor(spot);
            yield return new WaitForSeconds(1.2f);
            director.ActAt(spot);
            yield return new WaitForSeconds(1.4f);
            TowerAgent cannon = battle.TowerAt(new Cell(5, 5));
            if (cannon != null)
            {
                director.ActAt(cannon.Cell);
                yield return new WaitForSeconds(0.8f);
                battle.Upgrade(cannon);
            }
        }

        static MemoryRecordStore Records(int cleared)
        {
            var store = new MemoryRecordStore();
            if (cleared >= 1) store.Save(0, new LevelRecord(9860, 3, true));
            if (cleared >= 2) store.Save(1, new LevelRecord(11720, 2, true));
            return store;
        }

        static void Build(Battle battle, TowerKind kind, int col, int row, int tier = 0)
        {
            TowerAgent tower = battle.Build(kind, new Cell(col, row));
            for (int i = 0; tower != null && i < tier; i++) battle.Upgrade(tower);
        }

        static IEnumerator Stage(GameDirector director, string scenario)
        {
            switch (scenario)
            {
                case "title":
                    director.UseMemoryStore(Records(1));
                    director.Session.ToTitle();
                    yield return new WaitForSecondsRealtime(1.5f);
                    break;
                case "levels":
                    director.UseMemoryStore(Records(2));
                    director.Play();
                    yield return new WaitForSecondsRealtime(1.5f);
                    break;
                case "build":
                {
                    director.UseMemoryStore(Records(0));
                    director.TryStartLevel(0);
                    yield return null;
                    Battle battle = director.Session.Battle;
                    Build(battle, TowerKind.Ballista, 5, 2);
                    Build(battle, TowerKind.Cannon, 7, 4);
                    battle.Economy.Earn(40);
                    director.ToggleBuild(TowerKind.Frost);
                    director.SetCursor(new Cell(10, 4));
                    yield return new WaitForSecondsRealtime(3f);
                    break;
                }
                case "battle":
                case "upgrade":
                {
                    director.UseMemoryStore(Records(1));
                    director.TryStartLevel(1);
                    yield return null;
                    Battle battle = director.Session.Battle;
                    battle.Economy.Earn(1100);
                    Build(battle, TowerKind.Frost, 4, 4, 0);
                    Build(battle, TowerKind.Cannon, 5, 5, 1);
                    Build(battle, TowerKind.Mortar, 9, 5, 1);
                    Build(battle, TowerKind.Cannon, 12, 4, 2);
                    Build(battle, TowerKind.Ballista, 8, 2, 1);
                    Build(battle, TowerKind.Frost, 10, 6, 0);
                    Build(battle, TowerKind.Ballista, 6, 2, 0);
                    for (int i = 0; i < 5; i++) director.CallWave();
                    director.CycleSpeed();
                    yield return new WaitForSecondsRealtime(7f);
                    director.CycleSpeed();
                    director.CycleSpeed();
                    yield return new WaitForSecondsRealtime(0.5f);
                    if (scenario == "upgrade")
                    {
                        director.Select(battle.TowerAt(new Cell(5, 5)));
                        director.SetCursor(new Cell(5, 5));
                        yield return new WaitForSecondsRealtime(0.3f);
                    }
                    break;
                }
                case "gameplay":
                {
                    director.UseMemoryStore(Records(1));
                    director.TryStartLevel(1);
                    yield return null;
                    Battle battle = director.Session.Battle;
                    battle.Economy.Earn(1100);
                    Build(battle, TowerKind.Frost, 4, 4, 0);
                    Build(battle, TowerKind.Cannon, 5, 5, 1);
                    Build(battle, TowerKind.Cannon, 12, 4, 2);
                    Build(battle, TowerKind.Ballista, 8, 2, 1);
                    Build(battle, TowerKind.Frost, 10, 6, 0);
                    Build(battle, TowerKind.Ballista, 6, 2, 0);
                    for (int i = 0; i < 4; i++) director.CallWave();
                    yield return new WaitForSecondsRealtime(4f);
                    break;
                }
                case "snow":
                {
                    director.UseMemoryStore(Records(2));
                    director.TryStartLevel(2);
                    yield return null;
                    Battle battle = director.Session.Battle;
                    battle.Economy.Earn(4000);
                    Build(battle, TowerKind.Mortar, 6, 3, 2);
                    Build(battle, TowerKind.Frost, 4, 3, 2);
                    Build(battle, TowerKind.Cannon, 8, 3, 2);
                    Build(battle, TowerKind.Ballista, 3, 5, 2);
                    Build(battle, TowerKind.Cannon, 11, 5, 2);
                    Build(battle, TowerKind.Mortar, 9, 5, 1);
                    Build(battle, TowerKind.Frost, 11, 3, 1);
                    Build(battle, TowerKind.Ballista, 5, 5, 2);
                    Build(battle, TowerKind.Ballista, 13, 3, 2);
                    for (int i = 0; i < battle.TotalWaves; i++) director.CallWave();
                    director.CycleSpeed();
                    director.CycleSpeed();
                    float waited = 0f;
                    while (waited < 40f && !HasMothershipInView(battle))
                    {
                        waited += Time.unscaledDeltaTime;
                        yield return null;
                    }
                    director.CycleSpeed();
                    yield return new WaitForSecondsRealtime(0.8f);
                    break;
                }
                case "victory":
                {
                    director.UseMemoryStore(Records(0));
                    var waves = new WaveDefinition[8];
                    for (int i = 0; i < waves.Length; i++)
                    {
                        waves[i] = new WaveDefinition(new[] { new WaveGroup((int)EnemyKind.Scout, 1, 0f, 1f) }, 1f);
                    }
                    LevelDefinition hollow = Levels.All[0];
                    director.StartTrial(new LevelDefinition(hollow.Name, hollow.Tagline, hollow.Theme, 400, hollow.Rows, waves));
                    yield return null;
                    Battle battle = director.Session.Battle;
                    Build(battle, TowerKind.Ballista, 2, 2, 1);
                    Build(battle, TowerKind.Cannon, 4, 2);
                    Build(battle, TowerKind.Frost, 5, 2);
                    for (int i = 0; i < waves.Length; i++) director.CallWave();
                    float waited = 0f;
                    while (waited < 30f && director.Session.Screen != Screen.Result)
                    {
                        waited += Time.unscaledDeltaTime;
                        yield return null;
                    }
                    yield return new WaitForSecondsRealtime(0.6f);
                    break;
                }
                default:
                    Debug.LogWarning($"Unknown screenshot scenario '{scenario}'.");
                    break;
            }
        }

        static bool HasMothershipInView(Battle battle)
        {
            foreach (EnemyAgent enemy in battle.Enemies)
            {
                if (enemy.Kind == EnemyKind.Mothership && enemy.Distance > 6f) return true;
            }
            return false;
        }

        /// <summary>Points the camera and the HUD panel at render textures of a fixed size.</summary>
        void RenderOffscreen(GameDirector director)
        {
            sceneTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            uiTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            Camera.main.targetTexture = sceneTarget;
            var document = director.Hud.GetComponent<UIDocument>();
            PanelSettings panel = Instantiate(document.panelSettings);
            panel.targetTexture = uiTarget;
            panel.clearColor = true;
            panel.colorClearValue = Color.clear;
            document.panelSettings = panel;
            director.Hud.Init(Camera.main);
        }

        /// <summary>Blends the HUD over the 3D view, like the screen would show them.</summary>
        Texture2D CaptureOffscreen()
        {
            Color[] scene = Read(sceneTarget);
            Color[] ui = Read(uiTarget);
            for (int i = 0; i < scene.Length; i++)
            {
                Color over = ui[i];
                Color result = Color.Lerp(scene[i], new Color(over.r, over.g, over.b), over.a);
                result.a = 1f;
                scene[i] = result;
            }
            var shot = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            shot.SetPixels(scene);
            shot.Apply();
            return shot;
        }

        static Color[] Read(RenderTexture target)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            Color[] pixels = texture.GetPixels();
            Destroy(texture);
            return pixels;
        }

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
