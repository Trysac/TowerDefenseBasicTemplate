using System;
using System.Collections.Generic;
using System.IO;
using SaucerSiege.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace SaucerSiege.EditorTools
{
    /// <summary>
    /// Gathers the Kenney models into the GameArt asset, generates the materials, icon, scene and
    /// player settings from code, and builds the players, so every generated asset can be
    /// recreated from text. Batch mode:
    /// <code>
    /// Unity -batchmode -quit -projectPath . -executeMethod SaucerSiege.EditorTools.ProjectBuilder.SetUpProject
    /// Unity -batchmode -quit -projectPath . -executeMethod SaucerSiege.EditorTools.ProjectBuilder.BuildWindows -buildOutput &lt;folder&gt;
    /// Unity -batchmode -quit -projectPath . -executeMethod SaucerSiege.EditorTools.ProjectBuilder.BuildWebGL -buildOutput &lt;folder&gt;
    /// </code>
    /// </summary>
    public static class ProjectBuilder
    {
        const string ScenePath = "Assets/Scenes/SaucerSiege.unity";
        const string Kit = "Assets/Kenney/TowerDefenseKit/Models/";
        const string MaterialFolder = "Assets/Art/Materials";
        const string ArtPath = "Assets/Art/GameArt.asset";
        const string SoftDotPath = "Assets/Art/SoftDot.png";
        const string IconPath = "Assets/Art/AppIcon.png";
        const string AudioFolder = "Assets/Audio";
        const string ThemePath = "Assets/UI/HudTheme.tss";
        const string PanelSettingsPath = "Assets/UI/HudPanelSettings.asset";

        [MenuItem("Saucer Siege/Regenerate Art, Scene and Player Settings")]
        public static void SetUpProject()
        {
            GameArt art = CreateArt();
            PanelSettings panel = CreatePanelSettings();
            CreateScene(art, panel);
            ConfigurePlayer(CreateIcon());
            AssetDatabase.SaveAssets();
            Debug.Log("Saucer Siege project set up.");
        }

        /// <summary>
        /// Enters play mode and lets ScreenshotScenarios capture the README screenshots, then exits.
        /// Run in batch mode without -quit: <c>-executeMethod ...CaptureScreenshots -screenshot &lt;folder&gt;</c>.
        /// </summary>
        public static void CaptureScreenshots()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "SaucerSiege.exe");

        public static void BuildWebGL() => Build(BuildTarget.WebGL, string.Empty);

        // ---- Art ----

        static GameObject Model(string name)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + name + ".fbx");
            if (model == null)
            {
                throw new FileNotFoundException($"Missing Kenney model {name}.fbx under {Kit}.");
            }
            return model;
        }

        static GameObject[] Models(params string[] names) => Array.ConvertAll(names, Model);

        static GameArt CreateArt()
        {
            Directory.CreateDirectory(MaterialFolder);
            var art = AssetDatabase.LoadAssetAtPath<GameArt>(ArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<GameArt>();
                AssetDatabase.CreateAsset(art, ArtPath);
            }

            Bounds tile = MeasureBounds(Model("tile"));
            art.TileSize = tile.size.x;
            art.TileTop = tile.max.y;
            art.Meadow = Theme("", "tile", "detail_tree", "detail_treeLarge", "detail_rocks", "detail_rocksLarge", "detail_crystal", "detail_crystalLarge");
            art.Snow = Theme("snow_", "snow_tile", "snow_detail_tree", "snow_detail_treeLarge", "snow_detail_rocks", "snow_detail_rocksLarge", "snow_detail_crystal", "snow_detail_crystalLarge");
            art.Castle = Model("towerRound_sampleC");
            art.Towers = new[]
            {
                Parts("towerSquare_bottomA", "towerSquare_middleA", "towerSquare_topA", "weapon_ballista"),
                Parts("towerRound_bottomB", "towerRound_middleB", "towerRound_topB", "weapon_cannon"),
                Parts("towerRound_bottomC", "towerRound_middleC", "towerRound_topC", "weapon_blaster"),
                Parts("towerSquare_bottomC", "towerSquare_middleC", "towerSquare_topC", "weapon_catapult"),
            };
            art.Saucers = Models("enemy_ufoGreen", "enemy_ufoRed", "enemy_ufoPurpleWeapon", "enemy_ufoYellowWeapon");

            Texture2D dot = CreateSoftDot();
            art.Overlay = Particle("Overlay", null, additive: false, tint: new Color(1f, 1f, 1f, 0.5f));
            art.ParticleAdditive = Particle("ParticleAdditive", dot, additive: true, tint: new Color(0.5f, 0.5f, 0.5f, 0.5f));
            art.ParticleAlpha = Particle("ParticleAlpha", dot, additive: false, tint: new Color(0.6f, 0.9f, 1f, 0.55f));
            art.Bolt = Lit("Bolt", "#7a5232", 0.2f);
            art.Shell = Lit("Shell", "#2e3138", 0.6f, metallic: 0.6f);
            art.Boulder = Lit("Boulder", "#8b8478", 0.1f);
            art.FrostOrb = Unlit("FrostOrb", "#b8f2ff");
            art.BarBack = Unlit("BarBack", "#1d2433");
            art.BarFill = Unlit("BarFill", "#ffffff");

            art.CubeMesh = PrimitiveMesh(PrimitiveType.Cube);
            art.SphereMesh = PrimitiveMesh(PrimitiveType.Sphere);
            art.QuadMesh = PrimitiveMesh(PrimitiveType.Quad);

            art.TitleMusic = Clip("title_theme", music: true);
            art.BattleMusic = Clip("battle_theme", music: true);
            art.Click = Clip("click");
            art.Denied = Clip("denied");
            art.Build = Clip("build");
            art.Upgrade = Clip("upgrade");
            art.Sell = Clip("sell");
            art.BallistaShot = Clip("ballista");
            art.CannonShot = Clip("cannon");
            art.FrostShot = Clip("frost");
            art.MortarShot = Clip("mortar");
            art.Explosion = Clip("explosion");
            art.Hit = Clip("hit");
            art.SaucerDown = Clip("saucer_down");
            art.MothershipDown = Clip("mothership_down");
            art.Leak = Clip("leak");
            art.WaveStart = Clip("wave_start");
            art.WaveClear = Clip("wave_clear");
            art.Victory = Clip("victory");
            art.Defeat = Clip("defeat");

            EditorUtility.SetDirty(art);
            return art;
        }

        static TowerParts Parts(string bottom, string middle, string top, string weapon) => new TowerParts
        {
            Bottom = Model(bottom),
            Middle = Model(middle),
            Top = Model(top),
            Weapon = Model(weapon),
        };

        static ThemeTiles Theme(string prefix, string ground, string tree, string treeLarge, string rocks, string rocksLarge, string crystal, string crystalLarge)
        {
            var tiles = new ThemeTiles
            {
                Ground = Model(ground),
                Straight = Model(prefix + "tile_straight"),
                Corner = Model(prefix + "tile_cornerRound"),
                Spawn = Model(prefix + "tile_endRoundSpawn"),
                End = Model(prefix + "tile_endRound"),
                Trees = Models(tree, treeLarge),
                Rocks = Models(rocks, rocksLarge),
                Crystals = Models(crystal, crystalLarge),
            };
            tiles.StraightOpen = RoadSides(tiles.Straight, Shape.Straight);
            tiles.CornerOpen = RoadSides(tiles.Corner, Shape.Corner);
            tiles.SpawnOpen = RoadSides(tiles.Spawn, Shape.End);
            tiles.EndOpen = RoadSides(tiles.End, Shape.End);
            Debug.Log($"Road sides {prefix}: straight {tiles.StraightOpen}, corner {tiles.CornerOpen}, spawn {tiles.SpawnOpen}, end {tiles.EndOpen}");
            return tiles;
        }

        enum Shape
        {
            Straight,
            Corner,
            End,
        }

        static Bounds MeasureBounds(GameObject prefab)
        {
            GameObject probe = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Renderer[] renderers = probe.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                return bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }
        }

        /// <summary>
        /// Finds which sides of an unrotated road tile the road leaves through, from where the
        /// road's vertices (its dark dirt or snow-road materials) sit relative to the tile centre.
        /// </summary>
        static int RoadSides(GameObject prefab, Shape shape)
        {
            GameObject probe = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Bounds whole = MeasureBounds(prefab);
                var points = new List<Vector3>();
                foreach (MeshFilter filter in probe.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer = filter.GetComponent<MeshRenderer>();
                    Mesh mesh = filter.sharedMesh;
                    if (renderer == null || mesh == null) continue;
                    Vector3[] vertices = mesh.vertices;
                    Material[] materials = renderer.sharedMaterials;
                    for (int sub = 0; sub < mesh.subMeshCount && sub < materials.Length; sub++)
                    {
                        string name = materials[sub] != null ? materials[sub].name : "";
                        if (!name.Contains("dirtDark") && !name.Contains("snowRoad")) continue;
                        foreach (int index in mesh.GetTriangles(sub))
                        {
                            points.Add(filter.transform.TransformPoint(vertices[index]));
                        }
                    }
                }
                if (points.Count == 0)
                {
                    throw new InvalidOperationException($"{prefab.name} has no road material.");
                }
                Vector3 sum = Vector3.zero;
                Vector3 min = points[0];
                Vector3 max = points[0];
                foreach (Vector3 p in points)
                {
                    sum += p;
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
                Vector3 offset = sum / points.Count - whole.center;
                Vector3 spread = max - min;
                int east = offset.x >= 0f ? Side.East : Side.West;
                int north = offset.z >= 0f ? Side.North : Side.South;
                switch (shape)
                {
                    case Shape.Straight:
                        return spread.x > spread.z ? Side.East | Side.West : Side.North | Side.South;
                    case Shape.Corner:
                        return east | north;
                    default:
                        return Mathf.Abs(offset.x) > Mathf.Abs(offset.z) ? east : north;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }
        }

        /// <summary>The exact mesh GameObject.CreatePrimitive uses for a shape (the probe's collider is discarded).</summary>
        static Mesh PrimitiveMesh(PrimitiveType type)
        {
            GameObject probe = GameObject.CreatePrimitive(type);
            Mesh mesh = probe.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(probe);
            return mesh;
        }

        static Material Lit(string name, string color, float smoothness, float metallic = 0f)
        {
            Material material = MaterialAt(name, Shader.Find("Standard"));
            material.color = Hex(color);
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Unlit(string name, string color)
        {
            Material material = MaterialAt(name, Shader.Find("Unlit/Color"));
            material.color = Hex(color);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Particle(string name, Texture2D texture, bool additive, Color tint)
        {
            Material material = MaterialAt(name, Shader.Find("Legacy Shaders/Particles/" + (additive ? "Additive" : "Alpha Blended")));
            material.mainTexture = texture;
            material.SetColor("_TintColor", tint);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material MaterialAt(string name, Shader shader)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            return material;
        }

        /// <summary>A small white disc with a soft edge for every particle and the frost marker.</summary>
        static Texture2D CreateSoftDot()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha * (3f - 2f * alpha)));
                }
            }
            texture.Apply();
            File.WriteAllBytes(SoftDotPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SoftDotPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SoftDotPath);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(SoftDotPath);
        }

        static AudioClip Clip(string name, bool music = false)
        {
            string path = $"{AudioFolder}/{name}.ogg";
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Missing {path}: run python tools/audio/build.py first.");
                return null;
            }
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ---- UI ----

        static PanelSettings CreatePanelSettings()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.clearColor = false;
            EditorUtility.SetDirty(panel);
            return panel;
        }

        // ---- Scene ----

        static void CreateScene(GameArt art, PanelSettings panel)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#8cc7ed");
            camera.fieldOfView = 34f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 80f;
            cameraObject.AddComponent<AudioListener>();
            CameraRig rig = cameraObject.AddComponent<CameraRig>();

            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Hex("#fff1d6");
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            sunObject.transform.rotation = Quaternion.Euler(48f, -38f, 0f);

            RenderSettings.skybox = null;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#b9d8f2");
            RenderSettings.ambientEquatorColor = Hex("#8c9eaa");
            RenderSettings.ambientGroundColor = Hex("#5a5a52");
            RenderSettings.fog = false;

            var ui = new GameObject("HUD");
            var document = ui.AddComponent<UIDocument>();
            document.panelSettings = panel;
            Hud hud = ui.AddComponent<Hud>();

            var game = new GameObject("Game");
            var director = game.AddComponent<GameDirector>();
            var serialized = new SerializedObject(director);
            serialized.FindProperty("art").objectReferenceValue = art;
            serialized.FindProperty("hud").objectReferenceValue = hud;
            serialized.FindProperty("cameraRig").objectReferenceValue = rig;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer(Texture2D icon)
        {
            PlayerSettings.companyName = "Trysac";
            PlayerSettings.productName = "Saucer Siege";
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            int[] sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Any);
            var standaloneIcons = new Texture2D[sizes.Length];
            Array.Fill(standaloneIcons, icon);
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, standaloneIcons, IconKind.Any);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            // Static hosts such as GitHub Pages do not send Content-Encoding for .br/.gz files.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
        }

        // ---- Icon: a saucer beaming down over a castle tower on a green hill ----

        static Texture2D CreateIcon()
        {
            const int size = 1024;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = IconPixel(x + 0.5f - size / 2f, y + 0.5f - size / 2f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(IconPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        }

        static Color IconPixel(float x, float y)
        {
            const float aa = 1.5f;
            float tile = RoundedBox(x, y, 470f, 470f, 180f);
            float inside = Coverage(-tile, aa);
            Color color = Color.Lerp(Hex("#8fd3ff"), Hex("#3f8fd6"), Mathf.Clamp01((y + 470f) / 940f));
            color.a = inside;

            // The tractor beam.
            float beamHalf = Mathf.Lerp(150f, 60f, Mathf.Clamp01((y - -220f) / 400f));
            float beam = Mathf.Max(Mathf.Abs(x) - beamHalf, Mathf.Max(y - 160f, -320f - y));
            color = Over(color, Hex("#fff3a6"), Coverage(-beam, aa) * 0.45f * inside);

            // A green hill.
            float hill = Length(x, y + 900f) - 620f;
            color = Over(color, Hex("#57b347"), Coverage(-hill, aa) * inside);

            // The tower with battlements.
            float body = RoundedBox(x, y + 200f, 110f, 150f, 10f);
            float teeth = Mathf.Min(Mathf.Min(RoundedBox(x + 80f, y - -40f, 30f, 32f, 4f), RoundedBox(x, y - -40f, 30f, 32f, 4f)), RoundedBox(x - 80f, y - -40f, 30f, 32f, 4f));
            float tower = Mathf.Min(body, teeth);
            color = Over(color, Hex("#1d2433"), Coverage(-(tower - 14f), aa) * inside);
            color = Over(color, Hex("#c7ccd6"), Coverage(-tower, aa));
            float door = Mathf.Min(RoundedBox(x, y + 300f, 40f, 50f, 0f), Length(x, y + 250f) - 40f);
            color = Over(color, Hex("#5b4a3a"), Coverage(-door, aa));

            // The saucer: a disc with a glass dome and lights.
            float dome = Length((x) / 1.0f, (y - 250f)) - 95f;
            dome = Mathf.Max(dome, 230f - y);
            float disc = Length(x / 2.4f, (y - 225f)) - 70f;
            float saucer = Mathf.Min(disc, dome);
            color = Over(color, Hex("#1d2433"), Coverage(-(saucer - 14f), aa) * inside);
            color = Over(color, Hex("#e84a3c"), Coverage(-disc, aa));
            color = Over(color, Hex("#bdefff"), Coverage(-dome, aa));
            for (int i = -2; i <= 2; i++)
            {
                float light = Length(x - i * 62f, y - 215f) - 14f;
                color = Over(color, Hex("#ffd23f"), Coverage(-light, aa));
            }
            return color;
        }

        static float Length(float x, float y) => Mathf.Sqrt(x * x + y * y);

        static float RoundedBox(float x, float y, float halfWidth, float halfHeight, float radius)
        {
            float qx = Mathf.Abs(x) - (halfWidth - radius);
            float qy = Mathf.Abs(y) - (halfHeight - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        static float Coverage(float inside, float width) => Mathf.Clamp01(inside / width + 0.5f);

        static Color Over(Color below, Color above, float amount)
        {
            Color mixed = Color.Lerp(below, above, amount);
            mixed.a = below.a;
            return mixed;
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.magenta;

        // ---- Builds ----

        static void Build(BuildTarget target, string fileName)
        {
            string folder = Argument("-buildOutput");
            if (string.IsNullOrEmpty(folder))
            {
                throw new ArgumentException("Pass -buildOutput <folder> outside the repository.");
            }
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = target,
                locationPathName = string.IsNullOrEmpty(fileName) ? folder : Path.Combine(folder, fileName),
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"Build {target}: {summary.result}, {summary.totalSize / (1024 * 1024f):0.0} MB, {summary.totalErrors} errors.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
