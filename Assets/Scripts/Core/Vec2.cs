using System;

namespace SaucerSiege.Core
{
    /// <summary>
    /// A 2D vector on the ground plane (X right, Y forward, Unity's Z). The core never touches the engine,
    /// so the game converts it to and from Unity's Vector3 at the boundary.
    /// </summary>
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        public readonly float X;
        public readonly float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vec2 Zero => new Vec2(0f, 0f);

        public float LengthSquared => X * X + Y * Y;

        public float Length => (float)Math.Sqrt(LengthSquared);

        /// <summary>Angle in radians measured from +X towards +Y.</summary>
        public float Angle => (float)Math.Atan2(Y, X);

        /// <summary>The unit vector in the same direction, or zero for a zero vector.</summary>
        public Vec2 Normalized
        {
            get
            {
                float length = Length;
                return length > 1e-6f ? new Vec2(X / length, Y / length) : Zero;
            }
        }

        public static Vec2 FromAngle(float radians) => new Vec2((float)Math.Cos(radians), (float)Math.Sin(radians));

        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;

        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;

        /// <summary>Linear blend from <paramref name="a"/> (t = 0) to <paramref name="b"/> (t = 1).</summary>
        public static Vec2 Lerp(Vec2 a, Vec2 b, float t) => new Vec2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

        public Vec2 Rotated(float radians)
        {
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);
            return new Vec2(X * cos - Y * sin, X * sin + Y * cos);
        }

        /// <summary>The same direction with at most the given length.</summary>
        public Vec2 ClampLength(float max)
        {
            float length = Length;
            return length > max && length > 1e-6f ? this * (max / length) : this;
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);

        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);

        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Y);

        public static Vec2 operator *(Vec2 a, float k) => new Vec2(a.X * k, a.Y * k);

        public static Vec2 operator *(float k, Vec2 a) => new Vec2(a.X * k, a.Y * k);

        public static Vec2 operator /(Vec2 a, float k) => new Vec2(a.X / k, a.Y / k);

        public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);

        public override bool Equals(object obj) => obj is Vec2 other && Equals(other);

        public override int GetHashCode() => (X.GetHashCode() * 397) ^ Y.GetHashCode();

        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }
}
