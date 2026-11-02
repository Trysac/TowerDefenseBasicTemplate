using System;
using System.Collections.Generic;

namespace SaucerSiege.Core
{
    public enum TileType
    {
        /// <summary>Open ground where towers can be built.</summary>
        Ground,
        Path,
        Spawn,
        Base,
        Tree,
        Rock,
        Crystal,
    }

    /// <summary>A grid cell; column 0 is the left edge and row 0 the far (top) edge.</summary>
    public readonly struct Cell : IEquatable<Cell>
    {
        public Cell(int col, int row)
        {
            Col = col;
            Row = row;
        }

        public int Col { get; }
        public int Row { get; }

        public Cell Offset(int dc, int dr) => new Cell(Col + dc, Row + dr);

        public bool Equals(Cell other) => Col == other.Col && Row == other.Row;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => Col * 397 ^ Row;
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
        public override string ToString() => $"({Col}, {Row})";
    }

    /// <summary>
    /// A level's ground, parsed from rows of characters:
    /// <c>.</c> buildable ground, <c>#</c> path, <c>S</c> where the saucers land, <c>B</c> the base,
    /// <c>T</c> tree, <c>R</c> rock, <c>C</c> crystal. The path must run from S to B without
    /// branching or touching itself. Cells are one unit wide; world Y grows towards row 0.
    /// </summary>
    public sealed class LevelMap
    {
        readonly TileType[,] tiles;
        readonly List<Cell> route = new List<Cell>();

        public LevelMap(IReadOnlyList<string> rows)
        {
            if (rows == null || rows.Count == 0) throw new ArgumentException("A map needs rows.", nameof(rows));
            Rows = rows.Count;
            Cols = rows[0].Length;
            tiles = new TileType[Cols, Rows];
            Cell? spawn = null;
            for (int r = 0; r < Rows; r++)
            {
                if (rows[r].Length != Cols) throw new ArgumentException($"Row {r} is {rows[r].Length} wide, expected {Cols}.");
                for (int c = 0; c < Cols; c++)
                {
                    TileType type = Parse(rows[r][c], c, r);
                    tiles[c, r] = type;
                    if (type == TileType.Spawn)
                    {
                        if (spawn.HasValue) throw new ArgumentException("A map has exactly one spawn (S).");
                        spawn = new Cell(c, r);
                    }
                }
            }
            if (!spawn.HasValue) throw new ArgumentException("A map needs a spawn (S).");
            TraceRoute(spawn.Value);
        }

        public int Cols { get; }
        public int Rows { get; }

        /// <summary>The cells enemies walk through, from the spawn to the base.</summary>
        public IReadOnlyList<Cell> Route => route;

        public Cell Spawn => route[0];
        public Cell BaseCell => route[route.Count - 1];

        public bool Contains(Cell cell) => cell.Col >= 0 && cell.Row >= 0 && cell.Col < Cols && cell.Row < Rows;

        public TileType this[Cell cell] => tiles[cell.Col, cell.Row];

        public bool IsBuildable(Cell cell) => Contains(cell) && this[cell] == TileType.Ground;

        /// <summary>Centre of a cell on the ground plane.</summary>
        public Vec2 Center(Cell cell) => new Vec2(cell.Col, Rows - 1 - cell.Row);

        /// <summary>The cell under a ground-plane point, which may be outside the map.</summary>
        public Cell CellAt(Vec2 point) =>
            new Cell((int)Math.Floor(point.X + 0.5f), Rows - 1 - (int)Math.Floor(point.Y + 0.5f));

        /// <summary>The walking line through the centre of every route cell.</summary>
        public PathLine BuildPath()
        {
            var points = new List<Vec2>(route.Count);
            foreach (Cell cell in route)
            {
                points.Add(Center(cell));
            }
            return new PathLine(points);
        }

        static TileType Parse(char symbol, int col, int row)
        {
            switch (symbol)
            {
                case '.': return TileType.Ground;
                case '#': return TileType.Path;
                case 'S': return TileType.Spawn;
                case 'B': return TileType.Base;
                case 'T': return TileType.Tree;
                case 'R': return TileType.Rock;
                case 'C': return TileType.Crystal;
                default: throw new ArgumentException($"Unknown map symbol '{symbol}' at ({col}, {row}).");
            }
        }

        bool IsWalkable(Cell cell) => Contains(cell) && (this[cell] == TileType.Path || this[cell] == TileType.Base);

        void TraceRoute(Cell spawn)
        {
            var visited = new HashSet<Cell> { spawn };
            route.Add(spawn);
            Cell current = spawn;
            while (this[current] != TileType.Base)
            {
                Cell? next = null;
                foreach (Cell neighbour in Neighbours(current))
                {
                    if (!IsWalkable(neighbour) || visited.Contains(neighbour)) continue;
                    if (next.HasValue) throw new ArgumentException($"The path branches at {current}.");
                    next = neighbour;
                }
                if (!next.HasValue) throw new ArgumentException($"The path stops at {current} before reaching the base.");
                current = next.Value;
                visited.Add(current);
                route.Add(current);
            }
        }

        static IEnumerable<Cell> Neighbours(Cell cell)
        {
            yield return cell.Offset(1, 0);
            yield return cell.Offset(-1, 0);
            yield return cell.Offset(0, 1);
            yield return cell.Offset(0, -1);
        }
    }

    /// <summary>A polyline enemies follow, measured by distance from its start.</summary>
    public sealed class PathLine
    {
        readonly Vec2[] points;
        readonly float[] distances;

        public PathLine(IReadOnlyList<Vec2> points)
        {
            if (points == null || points.Count < 2) throw new ArgumentException("A path needs at least two points.", nameof(points));
            this.points = new Vec2[points.Count];
            distances = new float[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                this.points[i] = points[i];
                distances[i] = i == 0 ? 0f : distances[i - 1] + Vec2.Distance(points[i - 1], points[i]);
            }
        }

        public float Length => distances[distances.Length - 1];

        public IReadOnlyList<Vec2> Points => points;

        /// <summary>The point <paramref name="distance"/> along the path, clamped to its ends.</summary>
        public Vec2 PositionAt(float distance)
        {
            int segment = Segment(distance, out float t);
            return Vec2.Lerp(points[segment], points[segment + 1], t);
        }

        /// <summary>The unit direction of travel at <paramref name="distance"/>.</summary>
        public Vec2 DirectionAt(float distance)
        {
            int segment = Segment(distance, out _);
            return (points[segment + 1] - points[segment]).Normalized;
        }

        int Segment(float distance, out float t)
        {
            if (distance <= 0f)
            {
                t = 0f;
                return 0;
            }
            for (int i = 1; i < distances.Length; i++)
            {
                if (distance <= distances[i])
                {
                    float span = distances[i] - distances[i - 1];
                    t = span > 1e-6f ? (distance - distances[i - 1]) / span : 1f;
                    return i - 1;
                }
            }
            t = 1f;
            return distances.Length - 2;
        }
    }
}
