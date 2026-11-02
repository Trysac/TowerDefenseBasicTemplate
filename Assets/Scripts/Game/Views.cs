using SaucerSiege.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace SaucerSiege.Game
{
    /// <summary>A tower stacked from Kenney pieces: one middle section per tier, and a turning weapon.</summary>
    public sealed class TowerView
    {
        /// <summary>Kenney's weapons face -Z; this turns them to face +Z at yaw 0.</summary>
        public const float WeaponYawOffset = 180f;
        const float PieceScale = 0.86f;

        readonly GameArt art;
        readonly Transform root;
        Transform body;
        Transform weapon;
        Vector3 weaponRest;
        int builtTier = -1;
        float pop;

        public TowerView(GameArt art, TowerAgent agent, Vector3 position, Transform parent)
        {
            this.art = art;
            Agent = agent;
            root = new GameObject($"{agent.Kind} tower").transform;
            root.SetParent(parent, false);
            root.position = position;
            Rebuild();
        }

        public TowerAgent Agent { get; }
        public Transform Root => root;
        /// <summary>World height of the weapon, where shots leave from.</summary>
        public float MuzzleHeight { get; private set; }

        public void Destroy() => Object.Destroy(root.gameObject);

        void Rebuild()
        {
            if (body != null)
            {
                Object.Destroy(body.gameObject);
            }
            body = new GameObject("Body").transform;
            body.SetParent(root, false);
            TowerParts parts = art.Towers[(int)Agent.Kind];
            float top = root.position.y;
            Stack(parts.Bottom, ref top);
            for (int i = 0; i < Agent.Tier; i++)
            {
                Stack(parts.Middle, ref top);
            }
            Stack(parts.Top, ref top);
            weapon = Stack(parts.Weapon, ref top);
            weaponRest = weapon != null ? weapon.localPosition : Vector3.zero;
            MuzzleHeight = top - 0.12f;
            if (builtTier >= 0)
            {
                pop = 1f;
            }
            builtTier = Agent.Tier;
        }

        /// <summary>Places a piece so its bottom sits on <paramref name="top"/>, then raises the top.</summary>
        Transform Stack(GameObject prefab, ref float top)
        {
            if (prefab == null)
            {
                return null;
            }
            Transform piece = Shapes.Model(prefab, body, art.TileSize, Vector3.zero, 0f, PieceScale);
            Bounds bounds = WorldBounds(piece);
            float lift = top - bounds.min.y;
            piece.position += new Vector3(0f, lift, 0f);
            top = bounds.max.y + lift;
            return piece;
        }

        static Bounds WorldBounds(Transform model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(model.position, Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        public void Sync(float dt)
        {
            if (Agent.Tier != builtTier)
            {
                Rebuild();
            }
            if (weapon != null)
            {
                float yaw = Mathf.Atan2(Agent.Aim.X, Agent.Aim.Y) * Mathf.Rad2Deg + WeaponYawOffset;
                weapon.localRotation = Quaternion.Slerp(weapon.localRotation, Quaternion.Euler(0f, yaw, 0f), 1f - Mathf.Exp(-dt * 18f));
                float kick = Mathf.Clamp01(1f - Agent.SinceShot / 0.18f) * 0.07f;
                weapon.localPosition = weaponRest - new Vector3(Agent.Aim.X, 0f, Agent.Aim.Y) * kick;
            }
            pop = Mathf.MoveTowards(pop, 0f, dt * 4f);
            float squash = 1f + Mathf.Sin(pop * Mathf.PI) * 0.18f;
            root.localScale = new Vector3(squash, 1f + (squash - 1f) * 0.5f, squash);
        }
    }

    /// <summary>A Kenney saucer that hovers along the road, flashes when hit and shows its health.</summary>
    public sealed class EnemyView
    {
        static readonly float[] Scales = { 0.55f, 0.68f, 0.82f, 1.35f };
        /// <summary>Hull colours that tell the kinds apart (Kenney's hulls are all the same pale blue).</summary>
        static readonly Color[] Hulls =
        {
            new Color(0.62f, 0.9f, 0.55f), new Color(1f, 0.6f, 0.52f), new Color(0.74f, 0.62f, 1f), new Color(1f, 0.84f, 0.38f),
        };
        static readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();

        readonly Transform root;
        readonly Transform model;
        readonly Renderer[] renderers;
        readonly Color[][] colors;
        readonly Vector3 baseScale;
        readonly Transform bar;
        readonly Transform barFill;
        readonly Renderer barFillRenderer;
        readonly Transform frost;
        readonly float surface;
        readonly float phase;
        float flash;
        float spin;
        bool tinted;

        public EnemyView(GameArt art, EnemyAgent agent, float surface, Transform parent, Material frostMaterial)
        {
            Agent = agent;
            this.surface = surface;
            phase = agent.Id * 1.37f;
            root = new GameObject($"{agent.Kind} {agent.Id}").transform;
            root.SetParent(parent, false);
            float scale = Scales[(int)agent.Kind];
            model = Shapes.Model(art.Saucers[(int)agent.Kind], root, art.TileSize, Vector3.zero, 0f, scale);
            renderers = model.GetComponentsInChildren<Renderer>();
            colors = new Color[renderers.Length][];
            for (int r = 0; r < renderers.Length; r++)
            {
                Material[] materials = renderers[r].sharedMaterials;
                colors[r] = new Color[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    Color color = material != null && material.HasProperty("_Color") ? material.color : Color.white;
                    if (material != null && material.name.StartsWith("snowRoad"))
                    {
                        color = Hulls[(int)agent.Kind];
                    }
                    colors[r][i] = color;
                }
            }
            baseScale = model.localScale;
            tinted = true;

            bar = new GameObject("Health").transform;
            bar.SetParent(root, false);
            bar.localPosition = new Vector3(0f, 0.32f + scale * 0.45f, 0f);
            float width = 0.28f + scale * 0.3f;
            Shapes.Part("Back", bar, art.QuadMesh, art.BarBack, Vector3.zero, Vector3.zero, new Vector3(width + 0.04f, 0.09f, 1f), shadows: false);
            barFill = Shapes.Part("Fill", bar, art.QuadMesh, art.BarFill, new Vector3(0f, 0f, -0.01f), Vector3.zero, new Vector3(width, 0.055f, 1f), shadows: false);
            barFillRenderer = barFill.GetComponent<Renderer>();
            BarWidth = width;
            bar.gameObject.SetActive(false);

            frost = Shapes.Part("Frost", root, art.QuadMesh, frostMaterial, new Vector3(0f, -0.2f, 0f), new Vector3(90f, 0f, 0f), Vector3.one * scale * 1.1f, shadows: false);
            frost.gameObject.SetActive(false);
            Sync(0f, null);
        }

        public EnemyAgent Agent { get; }
        public Transform Root => root;
        float BarWidth { get; }

        public void Flash() => flash = 1f;

        /// <summary>Whitens every material of the model by <paramref name="amount"/> (0 restores it).</summary>
        void Tint(float amount)
        {
            for (int r = 0; r < renderers.Length; r++)
            {
                for (int i = 0; i < colors[r].Length; i++)
                {
                    Block.Clear();
                    Block.SetColor("_Color", Color.Lerp(colors[r][i], Color.white, amount * 0.85f));
                    renderers[r].SetPropertyBlock(Block, i);
                }
            }
        }

        public void Destroy() => Object.Destroy(root.gameObject);

        public void Sync(float dt, Camera view)
        {
            spin += dt * (Agent.Kind == EnemyKind.Mothership ? 40f : 120f) * (Agent.IsSlowed ? 0.4f : 1f);
            float bob = Mathf.Sin(Time.time * 3f + phase) * 0.04f;
            root.position = Shapes.World(Agent.Position, surface + 0.28f + bob);
            model.localRotation = Quaternion.Euler(Agent.Heading.Y * 6f, spin, -Agent.Heading.X * 6f);

            if (flash > 0f)
            {
                flash = Mathf.Max(0f, flash - dt * 9f);
            }
            model.localScale = baseScale * (1f + flash * 0.16f);
            if (flash > 0f || tinted)
            {
                Tint(flash);
                tinted = flash > 0f;
            }

            float fraction = Agent.Health.Fraction;
            bool hurt = fraction < 0.999f;
            bar.gameObject.SetActive(hurt);
            if (hurt && view != null)
            {
                bar.rotation = view.transform.rotation;
                barFill.localScale = new Vector3(BarWidth * fraction, barFill.localScale.y, 1f);
                barFill.localPosition = new Vector3(-(1f - fraction) * BarWidth * 0.5f, 0f, -0.01f);
                Color color = Color.Lerp(new Color(0.95f, 0.25f, 0.2f), new Color(0.45f, 0.95f, 0.35f), fraction);
                Shapes.Tint(barFillRenderer, Block, "_Color", color);
            }
            frost.gameObject.SetActive(Agent.IsSlowed);
        }
    }
}
