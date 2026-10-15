using System;
using VoxelAI.Core;
using VoxelAI.World;

namespace VoxelAI.Entities
{
	public static class Physics
	{
		public const float Gravity = -26f;

		public const float TerminalVelocity = -58f;

		public static void Move(VoxelAI.World.World w, ref Vec3 pos, ref Vec3 vel, Vec3 size, float dt, out bool onGround, out bool hitWall)
		{
			onGround = false;
			hitWall = false;
			Vec3 half = size * 0.5f;
			Vec3 vec = vel * dt;
			if (vec.Y != 0f)
			{
				float num = ((vec.Y > 0f) ? 1 : (-1));
				pos.Y += vec.Y;
				if (Collides(w, pos, half))
				{
					float num2 = BackOff(w, pos, half, 1, num, Math.Abs(vec.Y));
					pos.Y -= num * num2;
					if (num < 0f)
					{
						onGround = true;
					}
					vel.Y = 0f;
				}
			}
			if (vec.X != 0f)
			{
				float num3 = ((vec.X > 0f) ? 1 : (-1));
				pos.X += vec.X;
				if (Collides(w, pos, half))
				{
					float num4 = BackOff(w, pos, half, 0, num3, Math.Abs(vec.X));
					pos.X -= num3 * num4;
					vel.X = 0f;
					hitWall = true;
				}
			}
			if (vec.Z != 0f)
			{
				float num5 = ((vec.Z > 0f) ? 1 : (-1));
				pos.Z += vec.Z;
				if (Collides(w, pos, half))
				{
					float num6 = BackOff(w, pos, half, 2, num5, Math.Abs(vec.Z));
					pos.Z -= num5 * num6;
					vel.Z = 0f;
					hitWall = true;
				}
			}
			if (!onGround && Collides(w, pos.Add(0f, -0.1f, 0f), half))
			{
				onGround = true;
			}
			if (pos.Y < -40f)
			{
				pos.Y = -40f;
				vel.Y = 0f;
			}
		}

		private static Vec3 Offset(Vec3 p, int axis, float amount)
		{
			switch (axis)
			{
			case 0:
				return p.Add(amount, 0f, 0f);
			case 1:
				return p.Add(0f, amount, 0f);
			default:
				return p.Add(0f, 0f, amount);
			}
		}

		private static float BackOff(VoxelAI.World.World w, Vec3 pos, Vec3 half, int axis, float sign, float maxDistance)
		{
			float num = 0f;
			float num2 = maxDistance + 0.05f;
			for (int i = 0; i < 10; i++)
			{
				float num3 = (num + num2) * 0.5f;
				if (!Collides(w, Offset(pos, axis, (0f - sign) * num3), half))
				{
					num2 = num3;
				}
				else
				{
					num = num3;
				}
			}
			return num2;
		}

		public static bool Collides(VoxelAI.World.World w, Vec3 pos, Vec3 half)
		{
			int num = (int)Math.Floor(pos.X - half.X);
			int num2 = (int)Math.Floor(pos.X + half.X);
			int num3 = (int)Math.Floor(pos.Y - half.Y);
			int num4 = (int)Math.Floor(pos.Y + half.Y);
			int num5 = (int)Math.Floor(pos.Z - half.Z);
			int num6 = (int)Math.Floor(pos.Z + half.Z);
			for (int i = num3; i <= num4; i++)
			{
				for (int j = num5; j <= num6; j++)
				{
					for (int k = num; k <= num2; k++)
					{
						byte block = w.GetBlock(k, i, j);
						if (block != 0 && Blocks.Info((B)block).Solid)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public static bool InLiquid(VoxelAI.World.World w, Vec3 pos, Vec3 half, out bool lava)
		{
			lava = false;
			int num = (int)Math.Floor(pos.X - half.X);
			int num2 = (int)Math.Floor(pos.X + half.X);
			int num3 = (int)Math.Floor(pos.Y - half.Y);
			int num4 = (int)Math.Floor(pos.Y + half.Y);
			int num5 = (int)Math.Floor(pos.Z - half.Z);
			int num6 = (int)Math.Floor(pos.Z + half.Z);
			bool result = false;
			for (int i = num3; i <= num4; i++)
			{
				for (int j = num5; j <= num6; j++)
				{
					for (int k = num; k <= num2; k++)
					{
						switch (w.GetBlock(k, i, j))
						{
						case 12:
							result = true;
							break;
						case 13:
							lava = true;
							result = true;
							break;
						}
					}
				}
			}
			return result;
		}
	}
}
