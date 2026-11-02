using SaucerSiege.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace SaucerSiege.Game
{
    /// <summary>Sides of a tile as bits, for fitting road tiles to the route.</summary>
    public static class Side
    {
        public const int North = 1; // +Z, towards row 0
        public const int East = 2;  // +X
        public const int South = 4; // -Z
        public const int West = 8;  // -X

        /// <summary>The side of <paramref name="from"/> that faces its neighbour <paramref name="to"/>.</summary>
        public static int Towards(Cell from, Cell to)
        {
            if (to.Col > from.Col) return East;
            if (to.Col < from.Col) return West;
            return to.Row < from.Row ? North : South;
        }

        /// <summary>The sides after turning a tile 90 degrees clockwise (seen from above).</summary>
        public static int TurnRight(int sides) => ((sides << 1) | (sides >> 3)) & 15;

        /// <summary>The yaw (0, 90, 180 or 270) that turns a tile open on <paramref name="model"/> to open on <paramref name="wanted"/>.</summary>
        public static float YawFor(int model, int wanted)
        {
            int sides = model;
            for (int k = 0; k < 4; k++)
            {
                if (sides == wanted) return k * 90f;
                sides = TurnRight(sides);
            }
            return 0f;
        }
    }

    /// <summary>Small helpers for placing models and simple meshes. Nothing gets a collider.</summary>
    public static class Shapes
    {
        public static Vector3 World(Vec2 ground, float height = 0f) => new Vector3(ground.X, height, ground.Y);

        public static Vec2 Ground(Vector3 world) => new Vec2(world.x, world.z);

        /// <summary>
        /// A Kenney model scaled from model units to one unit per tile, inside a holder placed at
        /// <paramref name="position"/>. Kenney's pivots sit on a corner of each piece, so the model
        /// is shifted to put the middle of its footprint on the holder: turning the holder then
        /// spins the model in place.
        /// </summary>
        public static Transform Model(GameObject prefab, Transform parent, float tileSize, Vector3 position, float yaw = 0f, float scale = 1f)
        {
            var holder = new GameObject(prefab.name).transform;
            holder.SetParent(parent, false);
            holder.localPosition = position;
            GameObject model = Object.Instantiate(prefab, holder, false);
            model.transform.localScale = Vector3.one * (scale / tileSize);
            foreach (Collider collider in model.GetComponentsInChildren<Collider>())
            {
                Object.Destroy(collider);
            }
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                Vector3 offset = holder.InverseTransformPoint(bounds.center);
                model.transform.localPosition = new Vector3(-offset.x, 0f, -offset.z);
            }
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return holder;
        }

        public static Transform Part(string name, Transform parent, Mesh mesh, Material material,
            Vector3 position, Vector3 euler, Vector3 scale, bool shadows = true)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localEulerAngles = euler;
            part.transform.localScale = scale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = shadows;
            return part.transform;
        }

        /// <summary>A flat ring on the ground (inner radius as a fraction of the outer), facing up.</summary>
        public static Mesh Ring(float inner, int segments = 72)
        {
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * inner;
                vertices[i * 2 + 1] = direction;
                int next = (i + 1) % segments;
                int t = i * 6;
                triangles[t] = i * 2;
                triangles[t + 1] = next * 2;
                triangles[t + 2] = i * 2 + 1;
                triangles[t + 3] = i * 2 + 1;
                triangles[t + 4] = next * 2;
                triangles[t + 5] = next * 2 + 1;
            }
            var mesh = new Mesh { name = "Ring", vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Total height of a model's renderers, measured in its parent's space.</summary>
        public static float Height(Transform model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return 0f;
            }
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            float scale = model.parent != null ? model.parent.lossyScale.y : 1f;
            return bounds.size.y / Mathf.Max(scale, 1e-5f);
        }

        public static void Tint(Renderer renderer, MaterialPropertyBlock block, string property, Color color)
        {
            renderer.GetPropertyBlock(block);
            block.SetColor(property, color);
            renderer.SetPropertyBlock(block);
        }
    }
}
