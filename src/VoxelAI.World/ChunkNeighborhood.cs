namespace VoxelAI.World
{
	public class ChunkNeighborhood
	{
		public const int Stride = 8;

		public readonly Chunk[] C = new Chunk[9];

		public Chunk Center;

		public int BaseX;

		public int BaseZ;

		public int BakeLevel = 8;

		public void Load(World w, Chunk c)
		{
			Center = c;
			BaseX = c.BaseX;
			BaseZ = c.BaseZ;
			BakeLevel = w.BakeLevel;
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					C[(i + 1) * 3 + (j + 1)] = w.GetChunk(c.Cx + j, c.Cz + i);
				}
			}
		}

		private Chunk Pick(int x, int z, out int lx, out int lz)
		{
			int num = x - BaseX;
			int num2 = z - BaseZ;
			int num3 = 0;
			int num4 = 0;
			if (num < 0)
			{
				num3 = -1;
				lx = num + 16;
			}
			else if (num >= 16)
			{
				num3 = 1;
				lx = num - 16;
			}
			else
			{
				lx = num;
			}
			if (num2 < 0)
			{
				num4 = -1;
				lz = num2 + 16;
			}
			else if (num2 >= 16)
			{
				num4 = 1;
				lz = num2 - 16;
			}
			else
			{
				lz = num2;
			}
			return C[(num4 + 1) * 3 + (num3 + 1)];
		}

		public byte Block(int x, int y, int z)
		{
			if (y < 0 || y >= 128)
			{
				return 0;
			}
			int lx;
			int lz;
			Chunk chunk = Pick(x, z, out lx, out lz);
			if (chunk == null)
			{
				return 0;
			}
			return chunk.Blocks[Chunk.Index(lx, y, lz)];
		}

		public bool Opaque(int x, int y, int z)
		{
			byte b = Block(x, y, z);
			return b != 0 && Blocks.Info((B)b).Opaque;
		}

		public int Light(int x, int y, int z)
		{
			if (y < 0)
			{
				return 0;
			}
			if (y >= 128)
			{
				return 15 * BakeLevel / 8;
			}
			int lx;
			int lz;
			Chunk chunk = Pick(x, z, out lx, out lz);
			if (chunk == null)
			{
				return 15 * BakeLevel / 8;
			}
			int num = Chunk.Index(lx, y, lz);
			int num2 = chunk.SkyLight[num] * BakeLevel / 8;
			int num3 = chunk.BlockLight[num];
			return (num2 <= num3) ? num3 : num2;
		}
	}
}
