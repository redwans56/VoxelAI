using System.Collections.Generic;
using VoxelAI.Core;

namespace VoxelAI.AI
{
	public static class BuildPlan
	{
		public static List<Placement> CreateSmallHut(Vec3i origin, byte[] materials)
		{
			List<Placement> list = new List<Placement>(64);
			Rng r = new Rng((origin.X * 73856093) ^ (origin.Z * 19349663) ^ origin.Y);
			int num = 3;
			for (int i = -2; i <= 2; i++)
			{
				for (int j = -2; j <= 2; j++)
				{
					list.Add(new Placement
					{
						Pos = new Vec3i(origin.X + i, origin.Y - 1, origin.Z + j),
						Block = Pick(materials, r)
					});
				}
			}
			for (int k = 0; k < num; k++)
			{
				for (int l = -2; l <= 2; l++)
				{
					bool flag = k < 2 && l == 0;
					Vec3i[] array = new Vec3i[4]
					{
						new Vec3i(origin.X + l, origin.Y + k, origin.Z - 2),
						new Vec3i(origin.X + l, origin.Y + k, origin.Z + 2),
						new Vec3i(origin.X - 2, origin.Y + k, origin.Z + l),
						new Vec3i(origin.X + 2, origin.Y + k, origin.Z + l)
					};
					for (int m = 0; m < array.Length; m++)
					{
						bool flag2 = m == 0 && l == 0;
						if (!flag || !flag2)
						{
							list.Add(new Placement
							{
								Pos = array[m],
								Block = Pick(materials, r)
							});
						}
					}
				}
			}
			for (int n = -2; n <= 2; n++)
			{
				for (int num2 = -2; num2 <= 2; num2++)
				{
					list.Add(new Placement
					{
						Pos = new Vec3i(origin.X + n, origin.Y + num, origin.Z + num2),
						Block = Pick(materials, r)
					});
				}
			}
			return list;
		}

		public static List<Placement> CreateNook(Vec3i origin, byte[] materials)
		{
			List<Placement> list = new List<Placement>(24);
			Rng r = new Rng(origin.X * 789221 + origin.Z * 223092827 + origin.Y);
			for (int i = 0; i < 2; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					Vec3i[] array = new Vec3i[3]
					{
						new Vec3i(origin.X + j, origin.Y + i, origin.Z - 1),
						new Vec3i(origin.X - 1, origin.Y + i, origin.Z + j),
						new Vec3i(origin.X + 1, origin.Y + i, origin.Z + j)
					};
					for (int k = 0; k < array.Length; k++)
					{
						list.Add(new Placement
						{
							Pos = array[k],
							Block = Pick(materials, r)
						});
					}
				}
			}
			for (int l = -1; l <= 1; l++)
			{
				for (int m = -1; m <= 1; m++)
				{
					list.Add(new Placement
					{
						Pos = new Vec3i(origin.X + l, origin.Y + 2, origin.Z + m),
						Block = Pick(materials, r)
					});
				}
			}
			return list;
		}

		private static byte Pick(byte[] materials, Rng r)
		{
			if (materials == null || materials.Length == 0)
			{
				return 2;
			}
			return materials[r.Next(materials.Length)];
		}
	}
}
