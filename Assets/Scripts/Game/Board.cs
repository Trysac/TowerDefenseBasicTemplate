using SaucerSiege.Core;
using UnityEngine;

namespace SaucerSiege.Game
{
    /// <summary>
    /// The level's ground built from Kenney tiles: grass or snow, road tiles turned to follow the
    /// route, props on blocked cells, the landing pad and the castle, and a decorated border.
    /// </summary>
    public sealed class Board
    {
        readonly GameArt art;
        readonly LevelMap map;

        Board(GameArt art, LevelMap map, Transform root)
        {
            this.art = art;
            this.map = map;
            Root = root;
            Surface = art.TileTop / art.TileSize;
        }

        public Transform Root { get; }

        /// <summary>Height of the tiles' top surface in world units.</summary>
        public float Surface { get; }

        public Vector3 CellCenter(Cell cell, float lift = 0f) => Shapes.World(map.Center(cell), Surface + lift);

        public static Board Build(GameArt art, LevelMap map, Theme theme, Transform parent)
        {
            var root = new GameObject("Board").transform;
            root.SetParent(parent, false);
            var board = new Board(art, map, root);
            board.Lay(art.For(theme));
            return board;
        }

        public void Destroy()
        {
            Object.Destroy(Root.gameObject);
        }

        void Lay(ThemeTiles tiles)
        {
            var random = new System.Random(map.Cols * 31 + map.Rows * 7 + map.Route.Count);
            for (int r = -1; r <= map.Rows; r++)
            {
                for (int c = -1; c <= map.Cols; c++)
                {
                    var cell = new Cell(c, r);
                    if (!map.Contains(cell))
                    {
                        Border(tiles, cell, random);
                        continue;
                    }
                    switch (map[cell])
                    {
                        case TileType.Path:
                        case TileType.Spawn:
                        case TileType.Base:
                            Road(tiles, cell);
                            break;
                        case TileType.Tree:
                            Prop(tiles.Ground, cell, 0f);
                            Prop(Pick(tiles.Trees, random), cell, Surface, random.Next(4) * 90f);
                            break;
                        case TileType.Rock:
                            Prop(tiles.Ground, cell, 0f);
                            Prop(Pick(tiles.Rocks, random), cell, Surface, random.Next(4) * 90f);
                            break;
                        case TileType.Crystal:
                            Prop(tiles.Ground, cell, 0f);
                            Prop(Pick(tiles.Crystals, random), cell, Surface, random.Next(4) * 90f);
                            break;
                        default:
                            Prop(tiles.Ground, cell, 0f);
                            break;
                    }
                }
            }
        }

        void Road(ThemeTiles tiles, Cell cell)
        {
            int index = IndexOnRoute(cell);
            int open = 0;
            if (index > 0) open |= Side.Towards(cell, map.Route[index - 1]);
            if (index < map.Route.Count - 1) open |= Side.Towards(cell, map.Route[index + 1]);
            GameObject tile;
            int model;
            if (index == 0)
            {
                tile = tiles.Spawn;
                model = tiles.SpawnOpen;
            }
            else if (index == map.Route.Count - 1)
            {
                tile = tiles.End;
                model = tiles.EndOpen;
            }
            else if (open == (Side.North | Side.South) || open == (Side.East | Side.West))
            {
                tile = tiles.Straight;
                model = tiles.StraightOpen;
            }
            else
            {
                tile = tiles.Corner;
                model = tiles.CornerOpen;
            }
            Prop(tile, cell, 0f, Side.YawFor(model, open));
            if (index == map.Route.Count - 1 && art.Castle != null)
            {
                Prop(art.Castle, cell, Surface, Side.YawFor(Side.South, open), 0.9f);
            }
        }

        void Border(ThemeTiles tiles, Cell cell, System.Random random)
        {
            Prop(tiles.Ground, cell, 0f);
            int roll = random.Next(10);
            float yaw = random.Next(4) * 90f;
            if (roll < 4) Prop(Pick(tiles.Trees, random), cell, Surface, yaw);
            else if (roll < 6) Prop(Pick(tiles.Rocks, random), cell, Surface, yaw);
            else if (roll < 7) Prop(Pick(tiles.Crystals, random), cell, Surface, yaw);
        }

        int IndexOnRoute(Cell cell)
        {
            for (int i = 0; i < map.Route.Count; i++)
            {
                if (map.Route[i] == cell) return i;
            }
            return -1;
        }

        static GameObject Pick(GameObject[] options, System.Random random) =>
            options == null || options.Length == 0 ? null : options[random.Next(options.Length)];

        void Prop(GameObject prefab, Cell cell, float height, float yaw = 0f, float scale = 1f)
        {
            if (prefab == null)
            {
                return;
            }
            Shapes.Model(prefab, Root, art.TileSize, Shapes.World(map.Center(cell), height), yaw, scale);
        }
    }
}
