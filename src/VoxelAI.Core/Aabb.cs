using System;

namespace VoxelAI.Core
{
	public struct Aabb
	{
		public Vec3 Min;

		public Vec3 Max;

		public Vec3 Center
		{
			get
			{
				return (Min + Max) * 0.5f;
			}
		}

		public Vec3 Size
		{
			get
			{
				return Max - Min;
			}
		}

		public Aabb(Vec3 min, Vec3 max)
		{
			Min = min;
			Max = max;
		}

		public static Aabb FromCenter(Vec3 c, Vec3 size)
		{
			Vec3 b = size * 0.5f;
			return new Aabb(c - b, c + b);
		}

		public void Offset(Vec3 d)
		{
			Min += d;
			Max += d;
		}

		public Aabb Translated(Vec3 d)
		{
			return new Aabb(Min + d, Max + d);
		}

		public bool Intersects(Aabb o)
		{
			return !(o.Min.X >= Max.X) && !(o.Max.X <= Min.X) && !(o.Min.Y >= Max.Y) && !(o.Max.Y <= Min.Y) && !(o.Min.Z >= Max.Z) && !(o.Max.Z <= Min.Z);
		}

		public bool Contains(Vec3 p)
		{
			return p.X >= Min.X && p.X <= Max.X && p.Y >= Min.Y && p.Y <= Max.Y && p.Z >= Min.Z && p.Z <= Max.Z;
		}

		public bool ContainsXZ(Vec3 p)
		{
			return p.X >= Min.X && p.X <= Max.X && p.Z >= Min.Z && p.Z <= Max.Z;
		}

		public bool Raycast(Vec3 origin, Vec3 dir, float maxDist, out float hitDist)
		{
			hitDist = 0f;
			float num = 0f;
			float num2 = maxDist;
			for (int i = 0; i < 3; i++)
			{
				float num3;
				switch (i)
				{
				case 0:
					num3 = origin.X;
					break;
				case 1:
					num3 = origin.Y;
					break;
				default:
					num3 = origin.Z;
					break;
				}
				float num4 = num3;
				float num5;
				switch (i)
				{
				case 0:
					num5 = dir.X;
					break;
				case 1:
					num5 = dir.Y;
					break;
				default:
					num5 = dir.Z;
					break;
				}
				float num6 = num5;
				float num7;
				switch (i)
				{
				case 0:
					num7 = Min.X;
					break;
				case 1:
					num7 = Min.Y;
					break;
				default:
					num7 = Min.Z;
					break;
				}
				float num8 = num7;
				float num9;
				switch (i)
				{
				case 0:
					num9 = Max.X;
					break;
				case 1:
					num9 = Max.Y;
					break;
				default:
					num9 = Max.Z;
					break;
				}
				float num10 = num9;
				if (Math.Abs(num6) < 1E-07f)
				{
					if (num4 < num8 || num4 > num10)
					{
						return false;
					}
					continue;
				}
				float num11 = (num8 - num4) / num6;
				float num12 = (num10 - num4) / num6;
				if (num11 > num12)
				{
					float num13 = num11;
					num11 = num12;
					num12 = num13;
				}
				if (num11 > num)
				{
					num = num11;
				}
				if (num12 < num2)
				{
					num2 = num12;
				}
				if (num > num2)
				{
					return false;
				}
			}
			hitDist = num;
			return true;
		}
	}
}
