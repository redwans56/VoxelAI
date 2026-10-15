using System;
using VoxelAI.Core;
using VoxelAI.Gfx;

namespace VoxelAI.World
{
	public static class MeshBuilder
	{
		public const int Stride = 8;

		private static readonly float[] LightLut = BuildLightLut();

		public static readonly int[][] Normal = new int[6][]
		{
			new int[3]
			{
				0,
				-1,
				0
			},
			new int[3]
			{
				0,
				1,
				0
			},
			new int[3]
			{
				0,
				0,
				1
			},
			new int[3]
			{
				0,
				0,
				-1
			},
			new int[3]
			{
				1,
				0,
				0
			},
			new int[3]
			{
				-1,
				0,
				0
			}
		};

		private static readonly float[,] CX = new float[6, 4]
		{
			{
				0f,
				1f,
				1f,
				0f
			},
			{
				0f,
				0f,
				1f,
				1f
			},
			{
				0f,
				1f,
				1f,
				0f
			},
			{
				1f,
				0f,
				0f,
				1f
			},
			{
				1f,
				1f,
				1f,
				1f
			},
			{
				0f,
				0f,
				0f,
				0f
			}
		};

		private static readonly float[,] CY = new float[6, 4]
		{
			{
				0f,
				0f,
				0f,
				0f
			},
			{
				1f,
				1f,
				1f,
				1f
			},
			{
				0f,
				0f,
				1f,
				1f
			},
			{
				0f,
				0f,
				1f,
				1f
			},
			{
				0f,
				0f,
				1f,
				1f
			},
			{
				0f,
				0f,
				1f,
				1f
			}
		};

		private static readonly float[,] CZ = new float[6, 4]
		{
			{
				0f,
				0f,
				1f,
				1f
			},
			{
				0f,
				1f,
				1f,
				0f
			},
			{
				1f,
				1f,
				1f,
				1f
			},
			{
				0f,
				0f,
				0f,
				0f
			},
			{
				1f,
				0f,
				0f,
				1f
			},
			{
				0f,
				1f,
				1f,
				0f
			}
		};

		private static readonly int[,] UV = new int[6, 4]
		{
			{
				3,
				2,
				1,
				0
			},
			{
				3,
				2,
				1,
				0
			},
			{
				3,
				2,
				1,
				0
			},
			{
				3,
				2,
				1,
				0
			},
			{
				3,
				2,
				1,
				0
			},
			{
				3,
				2,
				1,
				0
			}
		};

		private static readonly int[,] TangentComponent = new int[6, 2]
		{
			{
				0,
				2
			},
			{
				0,
				2
			},
			{
				0,
				1
			},
			{
				0,
				1
			},
			{
				2,
				1
			},
			{
				2,
				1
			}
		};

		private static readonly int[] Order = new int[6]
		{
			0,
			1,
			2,
			0,
			2,
			3
		};

		private static readonly int[] EdgeA = new int[12]
		{
			0,
			1,
			2,
			3,
			4,
			5,
			6,
			7,
			0,
			1,
			2,
			3
		};

		private static readonly int[] EdgeB = new int[12]
		{
			1,
			3,
			3,
			2,
			5,
			7,
			7,
			6,
			4,
			5,
			6,
			7
		};

		private static float[] BuildLightLut()
		{
			float[] array = new float[16];
			for (int i = 0; i < 16; i++)
			{
				float num = (float)i / 15f;
				array[i] = 0.14f + 0.86f * (float)Math.Pow(num, 1.35);
			}
			return array;
		}

		public static void Build(World w, Chunk chunk, bool liquidPass, ChunkNeighborhood nb)
		{
			nb.Load(w, chunk);
			float[] array = ((!liquidPass) ? chunk.Mesh : chunk.LiquidMesh);
			int v = 0;
			byte[] blocks = chunk.Blocks;
			for (int i = 0; i < 128; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					int num = (i * 16 + j) * 16;
					for (int k = 0; k < 16; k++)
					{
						byte b = blocks[num + k];
						if (b == 0)
						{
							continue;
						}
						BlockInfo blockInfo = Blocks.Info((B)b);
						if (blockInfo.Liquid != liquidPass)
						{
							continue;
						}
						bool flag = b == 12;
						if (liquidPass && !flag)
						{
							continue;
						}
						int num2 = chunk.BaseX + k;
						int num3 = chunk.BaseZ + j;
						bool loweredTop = false;
						if (flag)
						{
							byte b2 = nb.Block(num2, i + 1, num3);
							loweredTop = b2 != 12;
						}
						for (int l = 0; l < 6; l++)
						{
							int x = num2 + Normal[l][0];
							int y = i + Normal[l][1];
							int z = num3 + Normal[l][2];
							byte b3 = nb.Block(x, y, z);
							if (b3 != 0)
							{
								BlockInfo blockInfo2 = Blocks.Info((B)b3);
								if (blockInfo2.Opaque || (b3 == b && (blockInfo.Cutout || blockInfo.Liquid)) || (flag && blockInfo2.Liquid))
								{
									continue;
								}
							}
							int num4;
							switch (l)
							{
							case 1:
								num4 = blockInfo.TileTop;
								break;
							case 0:
								num4 = blockInfo.TileBottom;
								break;
							default:
								num4 = blockInfo.TileSide;
								break;
							}
							int tile = num4;
							EmitFace(nb, ref array, ref v, num2, i, num3, l, tile, flag, loweredTop);
						}
					}
				}
			}
			if (array.Length > v + 1024)
			{
				Array.Resize(ref array, Math.Max(v, 8));
			}
			if (liquidPass)
			{
				chunk.LiquidMesh = array;
				chunk.LiquidVertexCount = v / 8;
			}
			else
			{
				chunk.Mesh = array;
				chunk.VertexCount = v / 8;
			}
			chunk.MeshVersion++;
		}

		private static void Ensure(ref float[] buf, int v, int need)
		{
			if (v + need > buf.Length)
			{
				int num = Math.Max(buf.Length * 2, v + need + 1024);
				float[] array = new float[num];
				Array.Copy(buf, array, v);
				buf = array;
			}
		}

		private static void EmitFace(ChunkNeighborhood nb, ref float[] buf, ref int v, int wx, int y, int wz, int f, int tile, bool liquid, bool loweredTop)
		{
			float u;
			float v2;
			float u2;
			float v3;
			TextureAtlas.GetUv(tile, out u, out v2, out u2, out v3);
			float num = u2 - u;
			float num2 = v3 - v2;
			float num3 = BlockTint.FaceShade[f];
			int num4 = wx + Normal[f][0];
			int num5 = y + Normal[f][1];
			int num6 = wz + Normal[f][2];
			int num7 = TangentComponent[f, 0];
			int num8 = TangentComponent[f, 1];
			float[] array = new float[4];
			for (int i = 0; i < 4; i++)
			{
				float num9;
				switch (num7)
				{
				case 0:
					num9 = CX[f, i];
					break;
				case 1:
					num9 = CY[f, i];
					break;
				default:
					num9 = CZ[f, i];
					break;
				}
				float num10 = num9;
				float num11;
				switch (num8)
				{
				case 0:
					num11 = CX[f, i];
					break;
				case 1:
					num11 = CY[f, i];
					break;
				default:
					num11 = CZ[f, i];
					break;
				}
				float num12 = num11;
				int num13 = ((num10 > 0.5f) ? 1 : (-1));
				int num14 = ((num12 > 0.5f) ? 1 : (-1));
				int num15 = ((num7 == 0) ? num13 : 0);
				int num16 = ((num7 == 1) ? num13 : 0);
				int num17 = ((num7 == 2) ? num13 : 0);
				int num18 = ((num8 == 0) ? num14 : 0);
				int num19 = ((num8 == 1) ? num14 : 0);
				int num20 = ((num8 == 2) ? num14 : 0);
				int num21 = nb.Light(num4, num5, num6);
				int num22 = nb.Light(num4 + num15, num5 + num16, num6 + num17);
				int num23 = nb.Light(num4 + num18, num5 + num19, num6 + num20);
				int num24 = nb.Light(num4 + num15 + num18, num5 + num16 + num19, num6 + num17 + num20);
				int num25 = num21 + num22 + num23 + num24 >> 2;
				int num26 = 0;
				if (nb.Opaque(num4 + num15, num5 + num16, num6 + num17))
				{
					num26++;
				}
				if (nb.Opaque(num4 + num18, num5 + num19, num6 + num20))
				{
					num26++;
				}
				if (num26 < 2 && nb.Opaque(num4 + num15 + num18, num5 + num16 + num19, num6 + num17 + num20))
				{
					num26++;
				}
				float num27 = 1f - (float)num26 * 0.15f;
				array[i] = LightLut[(num25 >= 0) ? ((num25 <= 15) ? num25 : 15) : 0] * num27 * num3;
			}
			Ensure(ref buf, v, 48);
			int[] order = Order;
			for (int j = 0; j < 6; j++)
			{
				int num28 = order[j];
				float num29 = (float)wx + CX[f, num28];
				float num30 = (float)y + CY[f, num28];
				float num31 = (float)wz + CZ[f, num28];
				if (liquid && loweredTop)
				{
					num30 = ((!(CY[f, num28] > 0.5f)) ? ((float)y) : ((float)y + 0.875f));
				}
				int num32 = UV[f, num28];
				float num33 = ((num32 != 0 && num32 != 3) ? (u + num) : u);
				float num34 = ((num32 != 0 && num32 != 1) ? (v2 + num2) : v2);
				buf[v] = num29;
				buf[v + 1] = num30;
				buf[v + 2] = num31;
				buf[v + 3] = num33;
				buf[v + 4] = num34;
				float num35 = array[num28];
				buf[v + 5] = num35;
				buf[v + 6] = num35;
				buf[v + 7] = num35;
				v += 8;
			}
		}

		public static int BuildBlockCube(float[] buf, int v, Vec3 origin, float scale, int blockId, float tint)
		{
			BlockInfo blockInfo = Blocks.Info((B)blockId);
			for (int i = 0; i < 6; i++)
			{
				int num;
				switch (i)
				{
				case 1:
					num = blockInfo.TileTop;
					break;
				case 0:
					num = blockInfo.TileBottom;
					break;
				default:
					num = blockInfo.TileSide;
					break;
				}
				int tile = num;
				float u;
				float v2;
				float u2;
				float v3;
				TextureAtlas.GetUv(tile, out u, out v2, out u2, out v3);
				float num2 = BlockTint.FaceShade[i] * tint;
				for (int j = 0; j < 6; j++)
				{
					int num3 = Order[j];
					int num4 = UV[i, num3];
					float num5 = ((num4 != 0 && num4 != 3) ? u2 : u);
					float num6 = ((num4 != 0 && num4 != 1) ? v3 : v2);
					buf[v] = origin.X + CX[i, num3] * scale;
					buf[v + 1] = origin.Y + CY[i, num3] * scale;
					buf[v + 2] = origin.Z + CZ[i, num3] * scale;
					buf[v + 3] = num5;
					buf[v + 4] = num6;
					buf[v + 5] = num2;
					buf[v + 6] = num2;
					buf[v + 7] = num2;
					v += 8;
				}
			}
			return v;
		}

		public static int BuildColoredBox(float[] buf, int v, Vec3 min, Vec3 max, float r, float g, float b, float shadeMul)
		{
			for (int i = 0; i < 6; i++)
			{
				float num = BlockTint.FaceShade[i] * shadeMul;
				for (int j = 0; j < 6; j++)
				{
					int num2 = Order[j];
					buf[v] = min.X + CX[i, num2] * (max.X - min.X);
					buf[v + 1] = min.Y + CY[i, num2] * (max.Y - min.Y);
					buf[v + 2] = min.Z + CZ[i, num2] * (max.Z - min.Z);
					buf[v + 3] = 0f;
					buf[v + 4] = 0f;
					buf[v + 5] = r * num;
					buf[v + 6] = g * num;
					buf[v + 7] = b * num;
					v += 8;
				}
			}
			return v;
		}

		public static void EmitWireBox(Vec3 min, Vec3 max, float expand)
		{
			Vec3 vec = min - new Vec3(expand, expand, expand);
			Vec3 vec2 = max + new Vec3(expand, expand, expand);
			GL.Begin(1u);
			for (int i = 0; i < 12; i++)
			{
				int num = EdgeA[i];
				int num2 = EdgeB[i];
				GL.Vertex3f(vec.X + (vec2.X - vec.X) * (float)(num & 1), vec.Y + (vec2.Y - vec.Y) * (float)((num >> 1) & 1), vec.Z + (vec2.Z - vec.Z) * (float)((num >> 2) & 1));
				GL.Vertex3f(vec.X + (vec2.X - vec.X) * (float)(num2 & 1), vec.Y + (vec2.Y - vec.Y) * (float)((num2 >> 1) & 1), vec.Z + (vec2.Z - vec.Z) * (float)((num2 >> 2) & 1));
			}
			GL.End();
		}
	}
}
