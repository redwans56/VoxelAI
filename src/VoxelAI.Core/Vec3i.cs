using System;

namespace VoxelAI.Core
{
	public struct Vec3i : IEquatable<Vec3i>
	{
		public int X;

		public int Y;

		public int Z;

		public static readonly Vec3i Zero = new Vec3i(0, 0, 0);

		public Vec3i(int x, int y, int z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public static Vec3i operator +(Vec3i a, Vec3i b)
		{
			return new Vec3i(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		public static Vec3i operator -(Vec3i a, Vec3i b)
		{
			return new Vec3i(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		}

		public static bool operator ==(Vec3i a, Vec3i b)
		{
			return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
		}

		public static bool operator !=(Vec3i a, Vec3i b)
		{
			return !(a == b);
		}

		public bool Equals(Vec3i o)
		{
			return this == o;
		}

		public override bool Equals(object o)
		{
			return o is Vec3i && Equals((Vec3i)o);
		}

		public override int GetHashCode()
		{
			return (X * 73856093) ^ (Y * 19349663) ^ (Z * 83492791);
		}

		public Vec3 ToVec3()
		{
			return new Vec3(X, Y, Z);
		}

		public float DistanceTo(Vec3i o)
		{
			int num = X - o.X;
			int num2 = Y - o.Y;
			int num3 = Z - o.Z;
			return (float)Math.Sqrt(num * num + num2 * num2 + num3 * num3);
		}

		public float DistanceToXZ(Vec3 v)
		{
			float num = (float)X + 0.5f - v.X;
			float num2 = (float)Z + 0.5f - v.Z;
			return (float)Math.Sqrt(num * num + num2 * num2);
		}

		public override string ToString()
		{
			return "(" + X + "," + Y + "," + Z + ")";
		}
	}
}
