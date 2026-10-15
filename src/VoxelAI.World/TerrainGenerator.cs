using System;
using VoxelAI.Core;

namespace VoxelAI.World
{
	public class TerrainGenerator
	{
		public const int SeaLevel = 62;

		public readonly int Seed;

		private readonly Noise nCont;

		private readonly Noise nHill;

		private readonly Noise nRidge;

		private readonly Noise nTemp;

		private readonly Noise nCave;

		private readonly Noise nOre;

		private readonly Noise nDetail;

		private readonly Noise nStruct;

		public TerrainGenerator(int seed)
		{
			Seed = seed;
			nCont = new Noise(seed + 1);
			nHill = new Noise(seed + 2);
			nRidge = new Noise(seed + 3);
			nTemp = new Noise(seed + 4);
			nCave = new Noise(seed + 5);
			nOre = new Noise(seed + 6);
			nDetail = new Noise(seed + 7);
			nStruct = new Noise(seed + 8);
		}

		public int HeightAt(int x, int z)
		{
			float num = nCont.Fbm2((float)x * 0.0031f, (float)z * 0.0031f, 4, 2f, 0.5f);
			float num2 = nHill.Fbm2((float)x * 0.015f, (float)z * 0.015f, 3, 2f, 0.5f);
			float num3 = nDetail.Perlin2((float)x * 0.06f, (float)z * 0.06f);
			float num4 = 1f - Math.Abs(nRidge.Perlin2((float)x * 0.0055f + 40f, (float)z * 0.0055f - 70f));
			float num5 = M.Clamp((num - 0.1f) / 0.9f, 0f, 1f);
			float num6 = 62f + num * 16f + num2 * 5f + num3 * 1.5f + num5 * num5 * num4 * num4 * 36f;
			return M.Clamp((int)num6, 4, 122);
		}

		public float Temperature(int x, int z, int height)
		{
			float num = nTemp.Fbm2((float)x * 0.0017f, (float)z * 0.0017f, 3, 2f, 0.5f);
			return num - (float)Math.Max(0, height - 62) * 0.011f;
		}

		public Biome BiomeAt(int x, int z)
		{
			int h = HeightAt(x, z);
			return BiomeAt(x, z, h);
		}

		public Biome BiomeAt(int x, int z, int h)
		{
			float num = Temperature(x, z, h);
			if (h < 61)
			{
				return Biome.Ocean;
			}
			if (h <= 63)
			{
				return Biome.Beach;
			}
			if (h > 86)
			{
				return Biome.Mountains;
			}
			if (num < -0.28f)
			{
				return Biome.Snowy;
			}
			if (num > 0.3f)
			{
				return Biome.Desert;
			}
			float num2 = nDetail.Perlin2((float)x * 0.004f + 300f, (float)z * 0.004f - 900f);
			return (!(num2 > 0.05f)) ? Biome.Plains : Biome.Forest;
		}

		public void Generate(Chunk c)
		{
			Rng rng = new Rng(Seed * 486187739 + c.Cx * 73856093 + c.Cz * 19349663);
			byte[] blocks = c.Blocks;
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					int num = c.BaseX + j;
					int num2 = c.BaseZ + i;
					int num3 = HeightAt(num, num2);
					Biome biome = BiomeAt(num, num2, num3);
					bool snowy = biome == Biome.Snowy || num3 > 92;
					for (int k = 0; k <= num3; k++)
					{
						byte b = (byte)((k == 0 || (k <= 3 && nOre.Perlin3((float)num * 0.3f, (float)k * 0.5f, (float)num2 * 0.3f) > -0.2f)) ? 20 : ((k == num3) ? TopBlock(num3, biome, snowy) : ((k <= num3 - 4) ? 3 : SubBlock(num3, biome, snowy))));
						if (k > 3 && k < num3 - 1 && b != 20)
						{
							float num4 = nCave.Fbm3((float)num * 0.027f, (float)k * 0.055f, (float)num2 * 0.027f, 3, 2f, 0.5f);
							float num5 = nCave.Fbm3((float)num * 0.052f + 100f, (float)k * 0.085f, (float)num2 * 0.052f - 60f, 2, 2f, 0.5f);
							float num6 = ((k >= 9) ? 0.4f : 0.28f);
							if (num4 > num6 && num5 > -0.25f)
							{
								b = (byte)((k <= 8) ? 13 : 0);
							}
						}
						blocks[Chunk.Index(j, k, i)] = b;
					}
					for (int l = num3 + 1; l <= 62; l++)
					{
						if (blocks[Chunk.Index(j, l, i)] == 0)
						{
							blocks[Chunk.Index(j, l, i)] = 12;
						}
					}
				}
			}
			ScatterOres(c, rng);
			ScatterStructures(c);
			c.ComputeSurface();
		}

		private static byte TopBlock(int h, Biome bio, bool snowy)
		{
			if (h <= 63)
			{
				return 5;
			}
			if (bio == Biome.Mountains || h > 96)
			{
				return 3;
			}
			if (snowy)
			{
				return 14;
			}
			if (bio == Biome.Desert)
			{
				return 5;
			}
			return 1;
		}

		private static byte SubBlock(int h, Biome bio, bool snowy)
		{
			if (h <= 63)
			{
				return 5;
			}
			if (bio == Biome.Mountains || h > 96)
			{
				return 3;
			}
			if (bio == Biome.Desert)
			{
				return 5;
			}
			return 2;
		}

		private void ScatterOres(Chunk c, Rng rng)
		{
			AddVeins(c, rng, B.CoalOre, 14, 4, 90, 5, 12);
			AddVeins(c, rng, B.IronOre, 9, 3, 62, 4, 9);
			AddVeins(c, rng, B.GoldOre, 3, 3, 34, 3, 7);
			AddVeins(c, rng, B.DiamondOre, 2, 2, 18, 3, 6);
		}

		private void AddVeins(Chunk c, Rng rng, B ore, int veins, int minY, int maxY, int minSize, int maxSize)
		{
			for (int i = 0; i < veins; i++)
			{
				int num = rng.Next(16);
				int num2 = rng.Next(16);
				int num3 = rng.Range(minY, maxY);
				int num4 = rng.Range(minSize, maxSize + 1);
				for (int j = 0; j < num4; j++)
				{
					if (num >= 0 && num < 16 && num2 >= 0 && num2 < 16 && num3 > 4 && num3 < 128)
					{
						int num5 = Chunk.Index(num, num3, num2);
						if (c.Blocks[num5] == 3)
						{
							c.Blocks[num5] = (byte)ore;
						}
					}
					num += rng.Range(-1, 2);
					num3 += rng.Range(-1, 2);
					num2 += rng.Range(-1, 2);
				}
			}
		}

		public bool TreeAt(int x, int z, out int trunkHeight)
		{
			trunkHeight = 0;
			int num = HeightAt(x, z);
			if (num <= 63)
			{
				return false;
			}
			float num2;
			switch (BiomeAt(x, z, num))
			{
			case Biome.Forest:
				num2 = 0.13f;
				break;
			case Biome.Plains:
				num2 = 0.028f;
				break;
			case Biome.Snowy:
				num2 = 0.075f;
				break;
			default:
				return false;
			}
			float num3 = nStruct.Perlin2((float)x * 0.11f, (float)z * 0.11f);
			if (num3 < -0.15f)
			{
				return false;
			}
			Rng rng = new Rng(Seed * 486187739 + x * 668265263 + z * 374761393);
			if (!rng.Chance(num2 * (0.6f + num3)))
			{
				return false;
			}
			trunkHeight = rng.Range(4, 7);
			return true;
		}

		public bool CactusAt(int x, int z, out int height)
		{
			height = 0;
			int num = HeightAt(x, z);
			if (num <= 63)
			{
				return false;
			}
			if (BiomeAt(x, z, num) != Biome.Desert)
			{
				return false;
			}
			Rng rng = new Rng(Seed * 40503 + x * 92821 + z * 68917);
			if (!rng.Chance(0.02f))
			{
				return false;
			}
			height = rng.Range(1, 4);
			return true;
		}

		private void ScatterStructures(Chunk c)
		{
			for (int i = -3; i < 20; i++)
			{
				for (int j = -3; j < 20; j++)
				{
					int x = c.BaseX + j;
					int z = c.BaseZ + i;
					int trunkHeight;
					if (TreeAt(x, z, out trunkHeight))
					{
						int num = HeightAt(x, z) + 1;
						byte or = GetOr(c, j, num - 1, i, 0);
						if (or == 1 || or == 2 || or == 14)
						{
							BuildTree(c, j, i, num, trunkHeight);
						}
					}
					int height;
					if (CactusAt(x, z, out height))
					{
						int num2 = HeightAt(x, z) + 1;
						for (int k = 0; k < height; k++)
						{
							PutIfAir(c, j, num2 + k, i, 15);
						}
					}
				}
			}
		}

		private byte GetOr(Chunk c, int x, int y, int z, byte def)
		{
			if (x >= 0 && x < 16 && z >= 0 && z < 16 && y >= 0 && y < 128)
			{
				return c.Blocks[Chunk.Index(x, y, z)];
			}
			return def;
		}

		private static void PutIfAir(Chunk c, int x, int y, int z, byte b)
		{
			if (x >= 0 && x < 16 && z >= 0 && z < 16 && y >= 0 && y < 128)
			{
				int num = Chunk.Index(x, y, z);
				if (c.Blocks[num] == 0)
				{
					c.Blocks[num] = b;
				}
			}
		}

		private static void PutIfLeaves(Chunk c, int x, int y, int z, byte b)
		{
			if (x >= 0 && x < 16 && z >= 0 && z < 16 && y >= 0 && y < 128)
			{
				int num = Chunk.Index(x, y, z);
				if (c.Blocks[num] == 0 || c.Blocks[num] == 8)
				{
					c.Blocks[num] = b;
				}
			}
		}

		private void BuildTree(Chunk c, int x, int z, int treeBase, int trunkHeight)
		{
			Rng rng = new Rng(x * 341873128 + z * 132897987 + Seed);
			bool flag = BiomeAt(c.BaseX + x, c.BaseZ + z) == Biome.Snowy;
			for (int i = 0; i < trunkHeight; i++)
			{
				PutIfAir(c, x, treeBase + i, z, 7);
			}
			int num = treeBase + trunkHeight;
			for (int j = -2; j <= 1; j++)
			{
				int num2 = ((j > -1) ? 1 : 2);
				for (int k = -num2; k <= num2; k++)
				{
					for (int l = -num2; l <= num2; l++)
					{
						if ((l != 0 || k != 0 || j >= 1) && (Math.Abs(l) != num2 || Math.Abs(k) != num2 || !rng.Chance(0.6f)))
						{
							bool flag2 = flag && j == 1;
							PutIfLeaves(c, x + l, num + j, z + k, (byte)((!flag2) ? 8 : 14));
						}
					}
				}
			}
			PutIfLeaves(c, x, num + 1, z, (byte)((!flag) ? 8 : 14));
		}
	}
}
