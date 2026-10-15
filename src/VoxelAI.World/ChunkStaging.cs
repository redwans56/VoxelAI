using System;
using System.Collections.Generic;

namespace VoxelAI.World
{
	public class ChunkStaging : ILightVolume
	{
		public const int PX = 18;

		public const int PZ = 18;

		public byte[] Sky = new byte[41472];

		public byte[] Blk = new byte[41472];

		public byte[] Blocks = new byte[41472];

		public int MaxY
		{
			get
			{
				return 128;
			}
		}

		private static int I(int x, int y, int z)
		{
			return (y * 18 + z) * 18 + x;
		}

		public void Pull(World world, Chunk c)
		{
			for (int i = -1; i <= 16; i++)
			{
				for (int j = -1; j <= 16; j++)
				{
					int x = c.BaseX + j;
					int z = c.BaseZ + i;
					bool flag = j >= 0 && j < 16 && i >= 0 && i < 16;
					for (int k = 0; k < 128; k++)
					{
						int num = I(j + 1, k, i + 1);
						if (flag)
						{
							Blocks[num] = c.Blocks[Chunk.Index(j, k, i)];
							continue;
						}
						Blocks[num] = world.GetBlock(x, k, z);
						Sky[num] = world.GetSkyLight(x, k, z);
						Blk[num] = world.GetBlockLight(x, k, z);
					}
				}
			}
		}

		private void ClearInnerLights()
		{
			for (int i = 0; i < 128; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					for (int k = 0; k < 16; k++)
					{
						int num = I(k + 1, i, j + 1);
						Sky[num] = 0;
						Blk[num] = 0;
					}
				}
			}
		}

		public void Compute(Chunk c)
		{
			ClearInnerLights();
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					int num = 15;
					for (int num2 = 127; num2 >= 0; num2--)
					{
						byte b = Blocks[I(j + 1, num2, i + 1)];
						if (b != 0)
						{
							BlockInfo blockInfo = VoxelAI.World.Blocks.Info((B)b);
							if (blockInfo.Opaque)
							{
								num = 0;
							}
							else if (blockInfo.Liquid)
							{
								num = Math.Max(0, num - 1);
							}
							else if (blockInfo.Cutout)
							{
								num = Math.Max(0, num - 2);
							}
						}
						Sky[I(j + 1, num2, i + 1)] = (byte)num;
					}
				}
			}
			LightEngine lightEngine = new LightEngine();
			for (int k = 0; k < 16; k++)
			{
				for (int l = 0; l < 16; l++)
				{
					int x = l + 1;
					int z = k + 1;
					for (int m = 0; m < 128; m++)
					{
						int num3 = Sky[I(x, m, z)];
						if (num3 > 0)
						{
							lightEngine.Push(x, m, z, num3);
						}
					}
				}
			}
			lightEngine.Solve(this, true);
			lightEngine.ResetQueue();
			for (int n = 0; n < 128; n++)
			{
				for (int num4 = 0; num4 < 16; num4++)
				{
					for (int num5 = 0; num5 < 16; num5++)
					{
						byte b2 = Blocks[I(num5 + 1, n, num4 + 1)];
						if (b2 != 0)
						{
							int light = VoxelAI.World.Blocks.Info((B)b2).Light;
							if (light > 0)
							{
								int num6 = I(num5 + 1, n, num4 + 1);
								Blk[num6] = (byte)light;
								lightEngine.Push(num5 + 1, n, num4 + 1, light);
							}
						}
					}
				}
				for (int num7 = 0; num7 < 18; num7++)
				{
					int num8 = I(num7, n, 0);
					int num9 = I(num7, n, 17);
					if (Blk[num8] > 0)
					{
						lightEngine.Push(num7, n, 0, Blk[num8]);
					}
					if (Blk[num9] > 0)
					{
						lightEngine.Push(num7, n, 17, Blk[num9]);
					}
				}
				for (int num10 = 0; num10 < 18; num10++)
				{
					int num11 = I(0, n, num10);
					int num12 = I(17, n, num10);
					if (Blk[num11] > 0)
					{
						lightEngine.Push(0, n, num10, Blk[num11]);
					}
					if (Blk[num12] > 0)
					{
						lightEngine.Push(17, n, num10, Blk[num12]);
					}
				}
			}
			lightEngine.Solve(this, false);
			lightEngine.ResetQueue();
		}

		public void CopyTo(Chunk c)
		{
			for (int i = 0; i < 128; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					for (int k = 0; k < 16; k++)
					{
						int num = I(k + 1, i, j + 1);
						int num2 = Chunk.Index(k, i, j);
						c.SkyLight[num2] = Sky[num];
						c.BlockLight[num2] = Blk[num];
					}
				}
			}
		}

		public void CollectBoundarySeeds(Chunk c, List<int> outSeeds)
		{
			for (int i = 0; i < 128; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					AddSeed(c.BaseX - 1, i, c.BaseZ + j, Sky[I(1, i, j + 1)], Blk[I(1, i, j + 1)], outSeeds);
					AddSeed(c.BaseX + 16, i, c.BaseZ + j, Sky[I(16, i, j + 1)], Blk[I(16, i, j + 1)], outSeeds);
					AddSeed(c.BaseX + j, i, c.BaseZ - 1, Sky[I(j + 1, i, 1)], Blk[I(j + 1, i, 1)], outSeeds);
					AddSeed(c.BaseX + j, i, c.BaseZ + 16, Sky[I(j + 1, i, 16)], Blk[I(j + 1, i, 16)], outSeeds);
				}
			}
		}

		private static void AddSeed(int wx, int wy, int wz, int sky, int blk, List<int> outSeeds)
		{
			if (sky > 1)
			{
				outSeeds.Add(wx);
				outSeeds.Add(wy);
				outSeeds.Add(wz);
				outSeeds.Add(sky - 1);
				outSeeds.Add(0);
			}
			if (blk > 1)
			{
				outSeeds.Add(wx);
				outSeeds.Add(wy);
				outSeeds.Add(wz);
				outSeeds.Add(blk - 1);
				outSeeds.Add(1);
			}
		}

		public byte LightBlock(int x, int y, int z)
		{
			if (y < 0 || y >= 128 || x < 0 || x >= 18 || z < 0 || z >= 18)
			{
				return 0;
			}
			return Blocks[I(x, y, z)];
		}

		public byte LightSky(int x, int y, int z)
		{
			if (y < 0 || y >= 128 || x < 0 || x >= 18 || z < 0 || z >= 18)
			{
				return 0;
			}
			return Sky[I(x, y, z)];
		}

		public byte LightBlockValue(int x, int y, int z)
		{
			if (y < 0 || y >= 128 || x < 0 || x >= 18 || z < 0 || z >= 18)
			{
				return 0;
			}
			return Blk[I(x, y, z)];
		}

		public void LightSetSky(int x, int y, int z, byte v)
		{
			if (y >= 0 && y < 128 && x >= 0 && x < 18 && z >= 0 && z < 18)
			{
				Sky[I(x, y, z)] = v;
			}
		}

		public void LightSetBlockValue(int x, int y, int z, byte v)
		{
			if (y >= 0 && y < 128 && x >= 0 && x < 18 && z >= 0 && z < 18)
			{
				Blk[I(x, y, z)] = v;
			}
		}

		public bool LightIsOpaque(int x, int y, int z)
		{
			if (y < 0 || y >= 128)
			{
				return true;
			}
			if (x < 0 || x >= 18 || z < 0 || z >= 18)
			{
				return false;
			}
			byte b = Blocks[I(x, y, z)];
			if (b == 0)
			{
				return false;
			}
			return VoxelAI.World.Blocks.Info((B)b).Opaque;
		}
	}
}
