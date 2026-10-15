using System.Collections.Generic;

namespace VoxelAI.World
{
	public class Chunk
	{
		public const int SX = 16;

		public const int SY = 128;

		public const int SZ = 16;

		public const int BlockCount = 32768;

		public readonly int Cx;

		public readonly int Cz;

		public readonly int BaseX;

		public readonly int BaseZ;

		public byte[] Blocks = new byte[32768];

		public byte[] SkyLight = new byte[32768];

		public byte[] BlockLight = new byte[32768];

		private volatile bool _generated;

		private volatile bool _lit;

		public volatile bool Dirty = true;

		public volatile bool Generating;

		public volatile bool InGenQueue;

		public volatile bool InLightQueue;

		public volatile bool PendingVboDelete;

		public List<int> BoundarySeeds = new List<int>(256);

		public float[] Mesh = new float[4096];

		public float[] LiquidMesh = new float[1024];

		public int VertexCount;

		public int LiquidVertexCount;

		public int MeshVersion;

		public int UploadedVersion = -1;

		public int Vbo;

		public int LiquidVbo;

		public bool MeshIsStatic = true;

		public int SurfaceY;

		public bool Generated
		{
			get
			{
				return _generated;
			}
			set
			{
				_generated = value;
			}
		}

		public bool Lit
		{
			get
			{
				return _lit;
			}
			set
			{
				_lit = value;
			}
		}

		public Chunk(int cx, int cz)
		{
			Cx = cx;
			Cz = cz;
			BaseX = cx * 16;
			BaseZ = cz * 16;
		}

		public static int Index(int x, int y, int z)
		{
			return (y * 16 + z) * 16 + x;
		}

		public byte Get(int x, int y, int z)
		{
			if (y < 0 || y >= 128 || x < 0 || x >= 16 || z < 0 || z >= 16)
			{
				return 0;
			}
			return Blocks[Index(x, y, z)];
		}

		public void ComputeSurface()
		{
			SurfaceY = 0;
			for (int num = 127; num >= 0; num--)
			{
				for (int i = 0; i < 16; i++)
				{
					for (int j = 0; j < 16; j++)
					{
						if (Blocks[Index(j, num, i)] != 0)
						{
							SurfaceY = num;
							return;
						}
					}
				}
			}
		}

		public int ColumnTop(int x, int z)
		{
			for (int num = 127; num >= 0; num--)
			{
				byte b = Blocks[Index(x, num, z)];
				if (b != 0 && b != 12 && b != 8)
				{
					return num;
				}
			}
			return 0;
		}

		public void DisposeMesh()
		{
			VertexCount = 0;
			LiquidVertexCount = 0;
			UploadedVersion = -1;
		}
	}
}
