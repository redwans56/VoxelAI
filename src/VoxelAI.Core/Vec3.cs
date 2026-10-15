using System;
using System.Globalization;

namespace VoxelAI.Core
{
	public struct Vec3
	{
		public float X;

		public float Y;

		public float Z;

		public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);

		public static readonly Vec3 One = new Vec3(1f, 1f, 1f);

		public static readonly Vec3 Up = new Vec3(0f, 1f, 0f);

		public float Length
		{
			get
			{
				return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
			}
		}

		public float LengthSq
		{
			get
			{
				return X * X + Y * Y + Z * Z;
			}
		}

		public float LengthXZ
		{
			get
			{
				return (float)Math.Sqrt(X * X + Z * Z);
			}
		}

		public Vec3 Normalized
		{
			get
			{
				float length = Length;
				if (length < 1E-06f)
				{
					return Zero;
				}
				return new Vec3(X / length, Y / length, Z / length);
			}
		}

		public Vec3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public static Vec3 operator +(Vec3 a, Vec3 b)
		{
			return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		public static Vec3 operator -(Vec3 a, Vec3 b)
		{
			return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		}

		public static Vec3 operator -(Vec3 a)
		{
			return new Vec3(0f - a.X, 0f - a.Y, 0f - a.Z);
		}

		public static Vec3 operator *(Vec3 a, float s)
		{
			return new Vec3(a.X * s, a.Y * s, a.Z * s);
		}

		public static Vec3 operator *(float s, Vec3 a)
		{
			return new Vec3(a.X * s, a.Y * s, a.Z * s);
		}

		public static Vec3 operator /(Vec3 a, float s)
		{
			return new Vec3(a.X / s, a.Y / s, a.Z / s);
		}

		public static float Dot(Vec3 a, Vec3 b)
		{
			return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
		}

		public static Vec3 Cross(Vec3 a, Vec3 b)
		{
			return new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
		}

		public static float Distance(Vec3 a, Vec3 b)
		{
			return (a - b).Length;
		}

		public static float DistanceSq(Vec3 a, Vec3 b)
		{
			return (a - b).LengthSq;
		}

		public static Vec3 Lerp(Vec3 a, Vec3 b, float t)
		{
			if (t < 0f)
			{
				t = 0f;
			}
			if (t > 1f)
			{
				t = 1f;
			}
			return new Vec3(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);
		}

		public Vec3 Floor()
		{
			return new Vec3((float)Math.Floor(X), (float)Math.Floor(Y), (float)Math.Floor(Z));
		}

		public Vec3i ToVec3i()
		{
			return new Vec3i((int)Math.Floor(X), (int)Math.Floor(Y), (int)Math.Floor(Z));
		}

		public Vec3 Add(float x, float y, float z)
		{
			return new Vec3(X + x, Y + y, Z + z);
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "({0:0.0},{1:0.0},{2:0.0})", X, Y, Z);
		}
	}
}
